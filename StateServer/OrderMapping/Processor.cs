using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Collections;
using System.Threading;
using System.Text;
using System.IO;
using iExchange.Common;

namespace iExchange.StateServer.OrderMapping
{
    internal delegate TransactionError DoBook(MappedTran tran);

    internal class Processor
    {
        private string cachePath;
        private LinkedList<MappedTran> mappedTrans = new LinkedList<MappedTran>();
        private object lockObj = new object();
        private AutoResetEvent tranNeedToBookEvent = new AutoResetEvent(false);
        private Thread bookTranThread = null;
        private bool isStarted = false;
        private long lastSequence = 1;
        private DoBook booker;

        public Processor(string cachePath, DoBook booker)
        {
            this.cachePath = cachePath;
            this.booker = booker;
        }

        public void Start()
        {
            lock (this.lockObj)
            {
                if (!this.isStarted)
                {
                    this.CreateCachePath();

                    this.Load();
                    if (this.mappedTrans.Count > 0)
                    {
                        this.StartBookTranThread();
                        this.tranNeedToBookEvent.Set();
                    }
                    this.isStarted = true;
                }
            }
        }   

        public void Add(Guid linkedAccountID, bool needSaveMappedOrderId, bool isLocal, XmlNode tran, Hashtable orderIdToMappedOrderId)
        {
            lock (lockObj)
            {
                if (!this.isStarted) throw new InvalidOperationException("Please call function Start first");

                MappedTran mappedTran 
                    = new MappedTran(this.lastSequence++, linkedAccountID, needSaveMappedOrderId, isLocal, tran, orderIdToMappedOrderId);
                if (bookTranThread == null) this.StartBookTranThread();

                int retryCount = 0;
                SaveMappedTranResult result = this.Save(mappedTran);                
                while (result == SaveMappedTranResult.FileAlreadyExist)
                {
                    AppDebug.LogEvent("StateServer", string.Format("File alrady exist while save map order {0}", mappedTran.ToXmlString()), System.Diagnostics.EventLogEntryType.Warning);

                    Thread.Sleep(10);
                    this.mappedTrans.Clear();
                    this.Load();
                    mappedTran.Sequence = this.lastSequence++;
                    result = this.Save(mappedTran);

                    if (retryCount++ > 3)
                    {
                        AppDebug.LogEvent("StateServer", string.Format("Save map order in backup file failed: {0}", mappedTran.ToXmlString()), System.Diagnostics.EventLogEntryType.Warning);
                        break;
                    }
                }

                mappedTrans.AddLast(mappedTran);
                this.tranNeedToBookEvent.Set();
            }
        }

        private void CreateCachePath()
        {
            try
            {
                if (!Directory.Exists(this.cachePath))
                {
                    Directory.CreateDirectory(this.cachePath);                    
                }
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer", ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }
        }

        private void StartBookTranThread()
        {
            this.bookTranThread = new Thread(this.BookTran);
            this.bookTranThread.IsBackground = true;
            this.bookTranThread.Start();
        }

        private void BookTran()
        {
            while (true)
            {
                this.tranNeedToBookEvent.WaitOne();
                while (true)
                {
                    MappedTran? tran = null;
                    lock (this.lockObj)
                    {
                        if (this.mappedTrans.Count > 0)
                        {
                            tran = this.mappedTrans.First.Value;
                        }
                    }

                    if (tran != null)
                    {
                        int tryCount = 0;
                        while (true)
                        {
                            if (this.Book(tran.Value))
                            {
                                lock (this.lockObj)
                                {
                                    this.mappedTrans.RemoveFirst();
                                }
                                break;
                            }
                            else
                            {
                                if (tryCount < 5) tryCount++;
                                Thread.Sleep(1000 * tryCount);
                            }
                        }
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private bool Book(MappedTran tran)
        {
            TransactionError error = this.booker.Invoke(tran);

            if (error == TransactionError.OK || error == TransactionError.TransactionAlreadyExists
                || error == TransactionError.OpenOrderNotExists || error == TransactionError.InvalidOrderRelation
                || error == TransactionError.ExceedOpenLotBalance)
            {
                if (error != TransactionError.OK)
                {
                    AppDebug.LogEvent("StateServer", string.Format("Book failed, error = {0} \n {1}", error, tran.ToXmlString()), System.Diagnostics.EventLogEntryType.Warning);
                }

                try
                {
                    string fileName = Path.Combine(this.cachePath, string.Format("{0}.tan", tran.Sequence));
                    File.Delete(fileName);
                }
                catch (Exception ex)
                {
                    AppDebug.LogEvent("StateServer", ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
                }
                
                return true;
            }
            else
            {
                AppDebug.LogEvent("StateServer", string.Format("Book failed, error = {0} \n {1}", error, tran.ToXmlString()), System.Diagnostics.EventLogEntryType.Warning);
                return false;
            }
        }

        private SaveMappedTranResult Save(MappedTran tran)
        {
            SaveMappedTranResult result = SaveMappedTranResult.OK;
            try
            {
                string fileName = Path.Combine(this.cachePath, string.Format("{0}.tan", tran.Sequence));
                using (FileStream stream = new FileStream(fileName, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.WriteThrough))
                {
                    using (StreamWriter writer = new StreamWriter(stream))
                    {
                        writer.Write(tran.ToXmlString());
                    }                    
                }
            }
            catch(Exception ex)
            {
                result = (ex is IOException && ex.ToString().Contains("already exists")) ? SaveMappedTranResult.FileAlreadyExist : SaveMappedTranResult.Error;
                AppDebug.LogEvent("StateServer", ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
            }

            return result;
        }

        private void Load()
        {
            string fileExtension = ".tan";
            List<string> files = new List<string>(Directory.EnumerateFiles(this.cachePath, "*.tan"));
            if (files.Count == 0) return;

            files.Sort((string left, string right) => 
            {
                string leftFileName = Path.GetFileName(left);
                string rightFileName = Path.GetFileName(right);
                long leftFileSequence = long.Parse(leftFileName.Substring(0, leftFileName.Length - fileExtension.Length));
                long rightFileSequence = long.Parse(rightFileName.Substring(0, rightFileName.Length - fileExtension.Length));
                return leftFileSequence.CompareTo(rightFileSequence);
            });

            string maxSequenceFileName = Path.GetFileName(files[files.Count - 1]);
            this.lastSequence = long.Parse(maxSequenceFileName.Substring(0, maxSequenceFileName.Length - fileExtension.Length));

            foreach (string fileName in files)
            {
                using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        string xmlString = reader.ReadToEnd();
                        MappedTran tran;
                        if (MappedTran.TryFromXmlString(xmlString, out tran))
                        {
                            this.mappedTrans.AddLast(tran);
                        }
                    }
                }
            }
        }
    }

    internal enum SaveMappedTranResult
    {
        OK,
        Error,
        FileAlreadyExist
    }

    internal struct MappedTran
    {
        private long sequence;
        private Guid linkedAccountID;
        private bool needSaveLinkedOrderId;
        private bool isLocal;
        private XmlNode tran;
        private Hashtable orderIdToMappedOrderId;
        private string xmlString;

        public bool IsLocal
        {
            get { return this.isLocal; }
        }

        public Guid LinkedAccountID
        {
            get { return this.linkedAccountID; }
        }

        public bool NeedSaveLinkedOrderId
        {
            get { return this.needSaveLinkedOrderId; }
        }

        public XmlNode XmlNode
        {
            get { return this.tran; }
        }

        public long Sequence
        {
            get { return this.sequence; }
            internal set { this.sequence = value; }
        }

        public Hashtable LinkedOrders
        {
            get { return this.orderIdToMappedOrderId; }
        }

        public MappedTran(long sequence, Guid linkedAccountID, bool needSaveMappedOrderId,
            bool isLocal, XmlNode tran, Hashtable orderIdToMappedOrderId)
        {
            this.sequence = sequence;
            this.linkedAccountID = linkedAccountID;
            this.needSaveLinkedOrderId = needSaveMappedOrderId;
            this.isLocal = isLocal;
            this.tran = tran;
            this.orderIdToMappedOrderId = orderIdToMappedOrderId;
            this.xmlString = null;
        }

        public string ToXmlString()
        {
            if (this.xmlString == null)
            {
                StringBuilder builder = new StringBuilder();
                builder.AppendFormat("<MappedTran LinkedAccountID='{0}' NeedSaveMappedOrderId='{1}' IsLocal='{2}' Sequence='{3}'>",
                    XmlConvert.ToString(this.linkedAccountID), XmlConvert.ToString(this.needSaveLinkedOrderId), 
                    XmlConvert.ToString(this.isLocal), XmlConvert.ToString(this.sequence));
                
                foreach (Guid orderId in this.orderIdToMappedOrderId.Keys)
                {
                    Guid mappedOrderId = (Guid)this.orderIdToMappedOrderId[orderId];
                    builder.AppendFormat("<OrderIdToMappedOrderId OrderId='{0}' MappedOrderId='{1}'/>",
                        XmlConvert.ToString(orderId), XmlConvert.ToString(mappedOrderId));
                }

                builder.Append(this.tran.OuterXml);
                builder.Append("</MappedTran>");

                this.xmlString = builder.ToString();
            }

            return this.xmlString;
        }

        public static bool TryFromXmlString(string xmlString, out MappedTran mappedTran)
        {
            try
            {
                XmlDocument document = new XmlDocument();
                document.LoadXml(xmlString);
                Guid linkedAccountID = XmlConvert.ToGuid(document.DocumentElement.Attributes["LinkedAccountID"].Value);
                bool isLocal = XmlConvert.ToBoolean(document.DocumentElement.Attributes["IsLocal"].Value);
                bool needSaveMappedOrderId = XmlConvert.ToBoolean(document.DocumentElement.Attributes["NeedSaveMappedOrderId"].Value);
                long sequence = XmlConvert.ToInt64(document.DocumentElement.Attributes["Sequence"].Value);
                Hashtable orderIdToMappedOrderId = new Hashtable();
                XmlNode tran = null;

                foreach (XmlNode childNode in document.DocumentElement.ChildNodes)
                {
                    if (childNode.Name == "OrderIdToMappedOrderId")
                    {
                        Guid orderId = XmlConvert.ToGuid(childNode.Attributes["OrderId"].Value);
                        Guid mappedOrderId = XmlConvert.ToGuid(childNode.Attributes["MappedOrderId"].Value);
                        orderIdToMappedOrderId.Add(orderId, mappedOrderId);
                    }
                    else if (childNode.Name == "Transaction")
                    {
                        tran = childNode;
                    }
                }

                mappedTran = new MappedTran(sequence, linkedAccountID, needSaveMappedOrderId, isLocal, tran, orderIdToMappedOrderId);
                mappedTran.xmlString = xmlString;
                return true;
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer", ex.ToString(), System.Diagnostics.EventLogEntryType.Error);
                mappedTran = default(MappedTran);
                return false;
            }
        }
    }
}