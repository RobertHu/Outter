using iExchange.Common;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.BLL.InstrumentBusiness
{
    public class InstrumentPriceStatusChangedEventArgs : EventArgs
    {
        public string OriginCode { get; private set; }
        public InstrumentPriceStatus Status { get; private set; }

        public InstrumentPriceStatusChangedEventArgs(string originCode, InstrumentPriceStatus status)
        {
            this.OriginCode = originCode;
            this.Status = status;
        }
    }

    public sealed class InstrumentPriceStatusManager
    {
        public event EventHandler<InstrumentPriceStatusChangedEventArgs> InstrumentPriceStatusChanged;
        public static readonly InstrumentPriceStatusManager Default = new InstrumentPriceStatusManager();

        private Dictionary<string, InstrumentPriceStatus> statusStore = new Dictionary<string, InstrumentPriceStatus>(30);

        static InstrumentPriceStatusManager() { }
        private InstrumentPriceStatusManager() { }

        public void Initialize(DataSet dataSet)
        {
            DataTable table = dataSet.Tables["Instrument"];
            if (table == null || !table.Columns.Contains("OriginCode") || !table.Columns.Contains("PriceStatus")) return;

            this.statusStore.Clear();
            foreach (DataRow row in table.Rows)
            {
                this.Initialize(new DBRow(row));
            }
        }

        internal void Initialize(IDBRow dr)
        {
            string originCode = (string)dr["OriginCode"];
            if (dr["PriceStatus"] != DBNull.Value)
            {
                InstrumentPriceStatus status = (InstrumentPriceStatus)((int)dr["PriceStatus"]);
                this.statusStore[originCode] = status;
            }
        }



        public void Update(XElement updateNode)
        {
            foreach (XElement method in updateNode.Elements())
            {
                foreach (XElement row in method.Elements())
                {
                    switch (row.Name.ToString())
                    {
                        case "OriginInstrument":
                            if (method.Name == "Modify")
                            {
                                string originCode = row.Attribute("OriginCode").Value;
                                InstrumentPriceStatus status = (InstrumentPriceStatus)(int.Parse(row.Attribute("Status").Value));
                                this.UpdateStatus(originCode, status);
                            }
                            break;
                    }
                }
            }
        }

        public bool IsOnBPoint(string originCode)
        {
            InstrumentPriceStatus status;
            if (this.statusStore.TryGetValue(originCode, out status))
            {
                return status == InstrumentPriceStatus.OnLowBPoint || status == InstrumentPriceStatus.OnHighBPoint;
            }
            return false;
        }

        public bool IsOnPriceLimit(string originCode)
        {
            InstrumentPriceStatus status;
            if (this.statusStore.TryGetValue(originCode, out status))
            {
                return status == InstrumentPriceStatus.OnHighLimit || status == InstrumentPriceStatus.OnLowLimit;
            }
            return false;
        }

        private void UpdateStatus(string originCode, InstrumentPriceStatus status)
        {
            if (!this.statusStore.ContainsKey(originCode) || this.statusStore[originCode] != status)
            {
                this.statusStore[originCode] = status;
                if (this.InstrumentPriceStatusChanged != null)
                {
                    this.InstrumentPriceStatusChanged(this,
                        new InstrumentPriceStatusChangedEventArgs(originCode, status));
                }
            }
        }
    }
}
