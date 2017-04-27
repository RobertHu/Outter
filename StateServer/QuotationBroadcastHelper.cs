using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;
using System.Threading;
using System.Xml;
using System.Diagnostics;
using System.ServiceModel;
using System.Configuration;
using System.Text;

namespace iExchange.StateServer
{
    internal class QuotationPair
    {
        internal OverridedQuotation OverridedQuotation { get; set; }
        internal OriginQuotation OriginQuotation { get; set; }
        internal Token Token { get; set; }

        internal bool IsEmpty { get { return this.OverridedQuotation == null; } }

        internal void Clear()
        {
            this.OverridedQuotation = null;
            this.OriginQuotation = null;
        }

        internal QuotationPair Clon()
        {
            QuotationPair clon = new QuotationPair();

            clon.OverridedQuotation = this.OverridedQuotation;
            clon.OriginQuotation = this.OriginQuotation;
            clon.Token = this.Token;

            return clon;
        }
    }

    internal class MergedQuotation
    {
        private Guid InstrumentId;
        private Guid QuotePolicyId;                

        private QuotationPair High = new QuotationPair();
        private QuotationPair Low = new QuotationPair();
        private QuotationPair Last = new QuotationPair();

        internal MergedQuotation(Guid instrumentId, Guid quotePolicyId)
        {
            this.InstrumentId = instrumentId;
            this.QuotePolicyId = quotePolicyId;
        }

        internal QuotationPair Fetch()
        {
            QuotationPair result = null;
            if (!this.High.IsEmpty)
            {
                if (this.Low.IsEmpty || this.High.OverridedQuotation.Timestamp < this.Low.OverridedQuotation.Timestamp)
                {
                    result = this.High.Clon();
                    this.High.Clear();
                }
                else
                {
                    result = this.Low.Clon();
                    this.Low.Clear();
                }
            }
            else if (!this.Low.IsEmpty)
            {
                result = this.Low.Clon();
                this.Low.Clear();
            }
            else if(!this.Last.IsEmpty)
            {
                result = this.Last.Clon();
                this.Last.Clear();
            }

            return result;
        }

        internal void Merge(Token token, OverridedQuotation overridedQuotation, OriginQuotation originQuotation)
        {
            if (this.High.IsEmpty || decimal.Parse(overridedQuotation.Bid) >= decimal.Parse(this.High.OverridedQuotation.Bid))
            {
                if (!this.High.IsEmpty && this.Low.IsEmpty)
                {
                    this.Low.OverridedQuotation = this.High.OverridedQuotation;
                    this.Low.OriginQuotation = this.High.OriginQuotation;
                    this.Low.Token = this.High.Token;
                }

                this.High.OverridedQuotation = overridedQuotation;
                this.High.OriginQuotation = originQuotation;
                this.High.Token = token;
                if (!this.Last.IsEmpty && overridedQuotation.Timestamp >= this.Last.OverridedQuotation.Timestamp)
                {
                    this.Last.Clear();
                }
            }
            else if (this.Low.IsEmpty || decimal.Parse(overridedQuotation.Bid) <= decimal.Parse(this.Low.OverridedQuotation.Bid))
            {
                this.Low.OverridedQuotation = overridedQuotation;
                this.Low.OriginQuotation = originQuotation;
                this.Low.Token = token;
                if (!this.Last.IsEmpty && overridedQuotation.Timestamp >= this.Last.OverridedQuotation.Timestamp)
                {
                    this.Last.Clear();
                }
            }
            else if (this.Last.IsEmpty || overridedQuotation.Timestamp >= this.Last.OverridedQuotation.Timestamp)
            {
                this.Last.OverridedQuotation = overridedQuotation;
                this.Last.OriginQuotation = originQuotation;
                this.Last.Token = token;
            }            
        }
    }

    internal class QuotationMerger
    {
        private Dictionary<string, MergedQuotation> mergedQuotaions = new Dictionary<string, MergedQuotation>();

        internal QuotationForBroadcast MergeAndGetQuotationToBroadcast(Queue<QuotationForBroadcast> pendingQuotations)
        {
            if (pendingQuotations.Count > 0)
            {
                if (pendingQuotations.Count > 3)
                {
                    string warning = string.Format("Too much {0} quotation wait for sending to TransactionServer", pendingQuotations.Count);
                    AppDebug.LogEvent("StateServer.QuotationBroadcastHelper", warning, EventLogEntryType.Warning);
                }

                foreach (QuotationForBroadcast pendingQuotation in pendingQuotations)
                {
                    if (pendingQuotation.OverridedQuotations == null) continue;

                    foreach (OverridedQuotation overridedQuotation in pendingQuotation.OverridedQuotations)
                    {
                        if (string.IsNullOrEmpty(overridedQuotation.Ask) || string.IsNullOrEmpty(overridedQuotation.Bid)) continue;

                        string mergedQuotationKey = overridedQuotation.InstrumentID.ToString() + overridedQuotation.QuotePolicyID.ToString();

                        MergedQuotation mergedQuotation = null;
                        if (!this.mergedQuotaions.TryGetValue(mergedQuotationKey, out mergedQuotation))
                        {
                            mergedQuotation = new MergedQuotation(overridedQuotation.InstrumentID, overridedQuotation.QuotePolicyID);
                            this.mergedQuotaions.Add(mergedQuotationKey, mergedQuotation);
                        }

                        OriginQuotation originQuotation = null;
                        if (pendingQuotation.OriginQuotations != null) originQuotation = pendingQuotation.OriginQuotations.FirstOrDefault<OriginQuotation>(t => { return t.InstrumentID == overridedQuotation.InstrumentID; });
                        mergedQuotation.Merge(pendingQuotation.Token, overridedQuotation, originQuotation);
                    }
                }
            }


            List<OverridedQuotation> overridedQuotations = null;
            List<OriginQuotation> originQuotations = null;
            Token token = null;
            foreach (MergedQuotation mergedQuotation in this.mergedQuotaions.Values)
            {
                QuotationPair pair = mergedQuotation.Fetch();
                if (pair != null)
                {
                    if (overridedQuotations == null) overridedQuotations = new List<OverridedQuotation>();
                    overridedQuotations.Add(pair.OverridedQuotation);
                    if (token != null) token = pair.Token;

                    if (pair.OriginQuotation != null)
                    {
                        if (originQuotations == null) originQuotations = new List<OriginQuotation>();
                        if (!originQuotations.Contains(pair.OriginQuotation)) originQuotations.Add(pair.OriginQuotation);
                    }
                }
            }

            if (overridedQuotations != null)
            {
                QuotationForBroadcast result = new QuotationForBroadcast();
                result.OverridedQuotations = overridedQuotations.ToArray();
                result.Token = token;
                result.OriginQuotations = originQuotations == null ? null : originQuotations.ToArray();
                return result;
            }
            else
            {
                return null;
            }
        }
    }

    [ServiceContract]
    public interface IRealtimeQuotationProcessorService
    {
        [OperationContract(AsyncPattern = false)]
        [XmlSerializerFormat]
        string SetQuotation(Token token, Common.OriginQuotation[] originQs, Common.OverridedQuotation[] overridedQs, out AutoFillResultInString[] autoFillResults);
    }

    public class QuotationForBroadcast
    {
        public Token Token { get; set; }
        public OriginQuotation[] OriginQuotations { get; set; }
        public OverridedQuotation[] OverridedQuotations { get; set; }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            if (this.OriginQuotations != null)
            {
                str.AppendLine("OriginQuotations");
                foreach (OriginQuotation item in this.OriginQuotations)
                {
                    str.AppendLine(item.ToString());
                }
            }

            if (this.OverridedQuotations != null)
            {
                str.AppendLine("OverridedQuotations");
                foreach (OverridedQuotation item in this.OverridedQuotations)
                {
                    str.AppendLine(item.ToString());
                }
            }

            return str.ToString();
        }
    }

    public class QuotationBroadcastHelper
    {
        private static bool EnableLog = false;
        private static List<QuotationBroadcastHelper> _QuotationBroadcastHeplers = new List<QuotationBroadcastHelper>();

        static QuotationBroadcastHelper()
        {
            string str = ConfigurationManager.AppSettings["EnableQuotationTrace"];
            bool enable = false;
            if (!string.IsNullOrEmpty(str) && bool.TryParse(str, out enable))
            {
                EnableLog = enable;
            }
        }

        public static void Add(StateServer stateServer, TransactionServer.Service transactionServerService)
        {
            QuotationBroadcastHelper._QuotationBroadcastHeplers.Add(new QuotationBroadcastHelper(stateServer, transactionServerService));
        }
        public static void AddQuotation(QuotationForBroadcast quotationForBroadcast)
        {
            if (EnableLog)
            {
                AppDebug.LogEvent("StateServer.QuotationBroadcastHelper",
                    string.Format("Quotation from QuotationServer: {0}", quotationForBroadcast), EventLogEntryType.Information);
            }

            foreach (QuotationBroadcastHelper helper in QuotationBroadcastHelper._QuotationBroadcastHeplers)
            {
                helper.Add(quotationForBroadcast);
            }
        }

        private StateServer _StateServer;
        private TransactionServer.Service _TransactionServerService;
        private IRealtimeQuotationProcessorService realtimeQuotationProcessorService;
        private string realtimeQuotationProcessorServiceUrl;
        private Queue<QuotationForBroadcast> _QuotationForBroadcastQueue;
        private AutoResetEvent _QuotationArriveEvent = new AutoResetEvent(false);
        private Thread _BroadcastThread;
        private QuotationMerger _Merger = new QuotationMerger();

        private QuotationBroadcastHelper(StateServer stateServer, TransactionServer.Service transactionServerService)
        {
            this._StateServer = stateServer;
            this._TransactionServerService = transactionServerService;

            string host = new Uri(transactionServerService.Url).Host;
            string port = ConfigurationManager.AppSettings["iExchange.StateServer.TransactionServer.RealtimeQuotationServicePort"];
            this.realtimeQuotationProcessorServiceUrl = string.Format("net.tcp://{0}:{1}/TransactionServer/RealtimeQuotationProcessService", host, string.IsNullOrEmpty(port) ? "9090" : port);
            this.realtimeQuotationProcessorService = CreateRealtimeQuotationProcessorService(this.realtimeQuotationProcessorServiceUrl);

            this._TransactionServerService.Timeout = 1800000;
            this._QuotationForBroadcastQueue = new Queue<QuotationForBroadcast>();
            this._BroadcastThread = new Thread(this.Broadcast);
            this._BroadcastThread.IsBackground = true;
            this._BroadcastThread.Start();
        }

        private void Add(QuotationForBroadcast quotationForBroadcast)
        {
            lock (this._QuotationForBroadcastQueue)
            {
                this._QuotationForBroadcastQueue.Enqueue(quotationForBroadcast);
                this._QuotationArriveEvent.Set();
            }
        }

        private void Broadcast()
        {
            while (true)
            {
                this._QuotationArriveEvent.WaitOne();
                try
                {
                    while (true)
                    {
                        QuotationForBroadcast quotation = null;
                        AutoFillResultInString[] autoFillResults;
                        lock (this._QuotationForBroadcastQueue)
                        {
                            //if (this._QuotationForBroadcastQueue.Count == 4)
                            {
                                quotation = this._Merger.MergeAndGetQuotationToBroadcast(this._QuotationForBroadcastQueue);
                                this._QuotationForBroadcastQueue.Clear();
                            }
                        }
                        if (quotation == null) break;
                        if (EnableLog)
                        {
                            AppDebug.LogEvent("StateServer.QuotationBroadcastHelper",
                                string.Format("Quotation send to TransactionServer: {0}", quotation), EventLogEntryType.Information);
                        }

                        Stopwatch watch = new Stopwatch();
                        watch.Start();
                        //XmlNode xmlHitOrders = this._TransactionServerService.SetQuotation(quotation.Token, quotation.OriginQuotations, quotation.OverridedQuotations, out autoFillResults);
                        string hitOrders = this.realtimeQuotationProcessorService.SetQuotation(quotation.Token, quotation.OriginQuotations, quotation.OverridedQuotations, out autoFillResults);
                        XmlNode xmlHitOrders = null;
                        if (!string.IsNullOrEmpty(hitOrders))
                        {
                            XmlDocument document = new XmlDocument();
                            document.LoadXml(hitOrders);
                            xmlHitOrders = document.DocumentElement;
                        }
                        watch.Stop();
                        AppDebug.LogEvent("StateServer",
                            string.Format("QuotationBroadcastHepler.Broadcast, call TransactionServer.SetQuotation consume time = {0} ms", watch.ElapsedMilliseconds), EventLogEntryType.Information);
                        AutoFillResult[] autoFillResults2 = autoFillResults == null ? null : new AutoFillResult[autoFillResults.Length];
                        if (autoFillResults != null)
                        {
                            int index = 0;
                            foreach (AutoFillResultInString source in autoFillResults)
                            {
                                autoFillResults2[index++] = source.ToAutoFillResult();
                            }
                        }
                        Token token = quotation.Token == null ? StateServer.Token: quotation.Token;
                        this._StateServer.AfterBroadcastQuotationToTransactionServer(token, xmlHitOrders, autoFillResults2, this._TransactionServerService);
                    }
                }
                catch (Exception ex)
                {
                    AppDebug.LogEvent("StateServer",
                        string.Format("QuotationBroadcastHepler.Broadcast to {0} call TransactionServer.SetQuotation error {1}", this.realtimeQuotationProcessorServiceUrl, ex), EventLogEntryType.Error);
                    this.realtimeQuotationProcessorService = CreateRealtimeQuotationProcessorService(this.realtimeQuotationProcessorServiceUrl);
                }
            }
        }

        private static IRealtimeQuotationProcessorService CreateRealtimeQuotationProcessorService(string url, int timeoutInSecond =30)
        {
            EndpointAddress address = new EndpointAddress(url);
            NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
            binding.MaxBufferPoolSize = binding.MaxReceivedMessageSize = binding.MaxBufferSize = 16 * 1024 * 1024;
            binding.SendTimeout = TimeSpan.FromSeconds(timeoutInSecond);
            binding.OpenTimeout = TimeSpan.FromSeconds(timeoutInSecond);
            ChannelFactory<IRealtimeQuotationProcessorService> factory = new ChannelFactory<IRealtimeQuotationProcessorService>(binding, address);
            IRealtimeQuotationProcessorService service = factory.CreateChannel();
            return service;
        }
    }
}