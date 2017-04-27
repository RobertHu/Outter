using iExchange.Common;
using log4net;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using System.Diagnostics;
using Protocal;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.PriceAlert
{
    public interface IQuotePolicySetter
    {
        Guid PublicQuotePolicyID { get; }
        Guid? PrivateQuotePolicyID { get; }
    }

    public interface IQuotePolicySetterProvider
    {
        IQuotePolicySetter Get(Guid customerId);
    }

    public interface IQuotationSetter
    {
        int Denominator { get; }
        int NumeratorUnit { get; }
    }

    public interface IQuotationSetterProvider
    {
        IQuotationSetter Get(Guid instrumentId);
    }

    public sealed class AlertInstrument
    {
        private Dictionary<Guid, AlertOverridedQuotation> _overridedQuotationDict = new Dictionary<Guid, AlertOverridedQuotation>(50);
        private Settings.Instrument _instrument;

        internal AlertInstrument(Settings.Instrument instrument)
        {
            _instrument = instrument;
        }

        public Guid Id
        {
            get { return _instrument.Id; }
        }

        public bool IsNormal
        {
            get { return _instrument.IsNormal; }
        }

        public Dictionary<Guid, AlertOverridedQuotation> OverridedQuotations
        {
            get { return _overridedQuotationDict; }
        }

        public Price CreatePriceFromDataRowItem(object dataRowItem)
        {
            if (dataRowItem == DBNull.Value)
            {
                return null;
            }
            else
            {
                return this.CreatePriceFromString((string)dataRowItem);
            }
        }

        public Price CreatePriceFromString(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            else
            {
                return Price.CreateInstance(value, _instrument.NumeratorUnit, _instrument.Denominator);
            }
        }

    }


    public class AlertOverridedQuotation
    {
        private AlertInstrument instrument;
        private Guid quotePolicyID;
        private DateTime timestamp;
        private Price ask;
        private Price bid;
        private Price high;
        private Price low;

        #region Common public properties definition
        public AlertInstrument Instrument
        {
            get { return this.instrument; }
        }
        public Guid QuotePolicyID
        {
            get { return this.quotePolicyID; }
        }
        public DateTime Timestamp
        {
            get { return this.timestamp; }
        }
        public Price Ask
        {
            get { return this.ask; }
        }
        public Price Bid
        {
            get { return this.bid; }
        }

        public Price High
        {
            get { return this.high; }
        }
        public Price Low
        {
            get { return this.low; }
        }

        /// <summary>
        /// NOTE: Buy and Sell is on the side of company
        /// </summary>
        public Price Buy
        {
            get { return (this.instrument.IsNormal ? this.bid : this.ask); }
        }
        public Price Sell
        {
            get { return (this.instrument.IsNormal ? this.ask : this.bid); }
        }
        #endregion Common public properties definition

        public AlertOverridedQuotation(DataRow overridedQuotationRow, AlertInstrument instrument)
        {
            this.instrument = instrument;
            this.quotePolicyID = (Guid)overridedQuotationRow["QuotePolicyID"];
            this.timestamp = (DateTime)overridedQuotationRow["Timestamp"];

            this.ask = instrument.CreatePriceFromDataRowItem(overridedQuotationRow["Ask"]);
            this.bid = instrument.CreatePriceFromDataRowItem(overridedQuotationRow["Bid"]);
            this.high = instrument.CreatePriceFromDataRowItem(overridedQuotationRow["High"]);
            this.low = instrument.CreatePriceFromDataRowItem(overridedQuotationRow["Low"]);
        }

        public AlertOverridedQuotation(OverridedQ overridedQ, AlertInstrument instrument)
        {
            this.instrument = instrument;
            this.quotePolicyID = overridedQ.QuotePolicyID;
            this.timestamp = overridedQ.Timestamp;

            this.ask = instrument.CreatePriceFromString(overridedQ.Ask);
            this.bid = instrument.CreatePriceFromString(overridedQ.Bid);
            this.high = instrument.CreatePriceFromString(overridedQ.High);
            this.low = instrument.CreatePriceFromString(overridedQ.Low);
        }



        public void Merge(AlertOverridedQuotation overridedQ)
        {
            if (this.timestamp <= overridedQ.timestamp)
            {
                this.timestamp = overridedQ.timestamp;
                if (overridedQ.ask != null) this.ask = overridedQ.ask;
                if (overridedQ.bid != null) this.bid = overridedQ.bid;
                if (overridedQ.high != null) this.high = overridedQ.high;
                if (overridedQ.low != null) this.low = overridedQ.low;
            }
            else
            {
                if (this.ask == null) this.ask = overridedQ.ask;
                if (this.bid == null) this.bid = overridedQ.bid;
                if (this.high == null) this.high = overridedQ.high;
                if (this.low == null) this.low = overridedQ.low;
            }
        }
    }


    internal sealed class Quotation
    {
        internal Guid QuotePolicyId { get; private set; }
        internal Guid InstrumentId { get; private set; }
        internal DateTime Timestamp { get; private set; }
        internal Price Ask { get; private set; }
        internal Price Bid { get; private set; }

        internal Quotation(Guid instrumentId, Guid quotePolicyId, DateTime timestamp, Price ask, Price bid)
        {
            this.InstrumentId = instrumentId;
            this.QuotePolicyId = quotePolicyId;
            this.Timestamp = timestamp;
            this.Ask = ask;
            this.Bid = bid;
        }

        internal static Quotation From(AlertOverridedQuotation overridedQuotation)
        {
            return new Quotation(overridedQuotation.Instrument.Id, overridedQuotation.QuotePolicyID,
                overridedQuotation.Timestamp, overridedQuotation.Ask, overridedQuotation.Bid);
        }
    }

    public class PriceAlertHitEventArgs : EventArgs
    {
        public IEnumerable<Alert> HitAlerts { get; private set; }

        public PriceAlertHitEventArgs(IEnumerable<Alert> hitAlerts)
        {
            this.HitAlerts = hitAlerts;
        }
    }

    public class PriceAlertExpiredEventArgs : EventArgs
    {
        public IEnumerable<Alert> ExpiredAlerts { get; private set; }

        public PriceAlertExpiredEventArgs(IEnumerable<Alert> expiredAlerts)
        {
            this.ExpiredAlerts = expiredAlerts;
        }
    }

    public sealed class Manager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Manager));

        public delegate void PriceAlertHitEventHandler(object sender, PriceAlertHitEventArgs fe);
        public delegate void PriceAlertExpiredEventHandler(object sender, PriceAlertExpiredEventArgs fe);

        public event PriceAlertHitEventHandler PriceAlertHit;
        public event PriceAlertExpiredEventHandler PriceAlertExpired;

        private IQuotationSetterProvider quotationSetterProvider;

        private object lockObj = new object();

        private Dictionary<Guid, ICollection<Alert>> instrument2Alerts = new Dictionary<Guid, ICollection<Alert>>();
        private Dictionary<Guid, Alert> alerts = new Dictionary<Guid, Alert>();

        private Dictionary<Guid, Dictionary<Guid, Quotation>> instrument2Quotations = new Dictionary<Guid, Dictionary<Guid, Quotation>>();

        private Dictionary<Guid, AlertInstrument> _alertInstrumentDict = new Dictionary<Guid, AlertInstrument>(50);

        private Queue<List<AlertOverridedQuotation>> marketQuotations = new Queue<List<AlertOverridedQuotation>>(100);
        private AutoResetEvent marketQuotationsEvent = new AutoResetEvent(false);
        private Thread hitThread;
        private Timer expireCheckTimer;

        internal static readonly Manager Default = new Manager();

        static Manager() { }
        private Manager() { }

        public void Initialize()
        {
            this.quotationSetterProvider = QuoteProvider.Default;
            this.InitializeThreadAndTimer();
        }



        internal void OnInstrumentUpdated(Settings.Instrument instrument, Settings.InstrumentUpdateType updateType)
        {
            lock (this.lockObj)
            {
                Logger.InfoFormat("update instrument, updateType = {0}, instrument = {1}", updateType, instrument);
                if (updateType == Settings.InstrumentUpdateType.Add)
                {
                    Debug.Assert(!_alertInstrumentDict.ContainsKey(instrument.Id));
                    _alertInstrumentDict.Add(instrument.Id, new AlertInstrument(instrument));
                }
                else if (updateType == Settings.InstrumentUpdateType.Delete)
                {
                    Debug.Assert(_alertInstrumentDict.ContainsKey(instrument.Id));
                    _alertInstrumentDict.Remove(instrument.Id);
                }
            }
        }



        private void InitializeThreadAndTimer()
        {
            this.hitThread = new Thread(this.HitAlerts);
            this.hitThread.IsBackground = true;
            this.hitThread.Start();

            this.expireCheckTimer = new Timer(this.DoExpireCheck, null, 60 * 1000, 60 * 1000);
        }

        internal void InitializePriceAlert(IDBRow dr)
        {
            Alert alert = Alert.Create(dr, this.quotationSetterProvider);
            Logger.Info(string.Format("PriceAlertManager.Initialize add alert {0}", alert));
            this.Add(alert);
        }



        private void DoExpireCheck(object state)
        {
            try
            {
                this.expireCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);

                List<Alert> expiredAlerts = new List<Alert>();

                lock (this.lockObj)
                {
                    DateTime now = DateTime.Now;
                    foreach (Alert alert in this.alerts.Values)
                    {
                        if (alert.State == AlertState.Pending && alert.Expire(now))
                        {
                            expiredAlerts.Add(alert);
                        }
                    }

                    foreach (Alert alert in expiredAlerts)
                    {
                        Logger.Info(string.Format("PriceAlertManager.DoExpireCheck remove expired alert {0}", alert));
                        this.Remove(alert.Id);
                    }
                }

                if (expiredAlerts.Count > 0)
                {
                    this.SaveAlerts(expiredAlerts);
                    this.FireAlertExpried(expiredAlerts);
                }
            }
            catch (Exception exception)
            {
                Logger.Error("PriceAlertManager.DoExpireCheck error", exception);
            }
            finally
            {
                this.expireCheckTimer.Change(60 * 1000, 60 * 1000);
            }
        }

        private void HitAlerts(object state)
        {
            while (true)
            {
                this.marketQuotationsEvent.WaitOne();

                try
                {
                    while (true)
                    {
                        List<Alert> hitedAlerts = new List<Alert>();
                        List<Alert> expiredAlerts = new List<Alert>();

                        lock (this.lockObj)
                        {
                            if (this.marketQuotations.Count == 0) break;

                            DateTime now = DateTime.Now;
                            List<AlertOverridedQuotation> overridedQs = this.marketQuotations.Dequeue();
                            List<Guid> quotationChangedInstruments = this.UpdateQuotation(overridedQs);
                            foreach (Guid instrumentId in quotationChangedInstruments)
                            {
                                ICollection<Alert> alerts = null;
                                if (this.instrument2Alerts.TryGetValue(instrumentId, out alerts) && alerts.Count > 0)
                                {
                                    foreach (Alert alert in alerts)
                                    {
                                        if (alert.QuotePolicyId == null) continue;
                                        Quotation quotation = this.GetQuotation(alert.InstrumentId, alert.QuotePolicyId.Value);
                                        if (quotation == null) continue;
                                        if (alert.State == AlertState.Pending)
                                        {
                                            if (alert.Expire(now))
                                            {
                                                Logger.InfoFormat("alert expire {0}", alert);
                                                expiredAlerts.Add(alert);
                                            }
                                            else if (alert.Hit(quotation))
                                            {
                                                Logger.InfoFormat("alert hit {0}", alert);
                                                hitedAlerts.Add(alert);
                                            }
                                        }
                                    }
                                }
                            }

                            foreach (Alert alert in hitedAlerts)
                            {
                                Logger.Info(string.Format("PriceAlertManager.HitAlerts remove hit alert {0}", alert));
                                this.Remove(alert.Id);
                            }

                            foreach (Alert alert in expiredAlerts)
                            {
                                Logger.Info(string.Format("PriceAlertManager.HitAlerts remove expired alert {0}", alert));
                                this.Remove(alert.Id);
                            }
                        }

                        if (hitedAlerts.Count > 0)
                        {
                            this.SaveAlerts(hitedAlerts);
                            this.FireAlertHit(hitedAlerts);
                        }

                        if (expiredAlerts.Count > 0)
                        {
                            this.SaveAlerts(expiredAlerts);
                            this.FireAlertExpried(expiredAlerts);
                        }

                        if (this.marketQuotations.Count > 0)
                        {
                            Thread.Sleep(10);
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception exception)
                {
                    Logger.Error("PriceAlertManager.HitAlerts error", exception);
                }
            }
        }

        private Quotation GetQuotation(Guid instrumentId, Guid quotePolicyId)
        {
            Dictionary<Guid, Quotation> quotations = null;
            Quotation quotation = null;
            if (this.instrument2Quotations.TryGetValue(instrumentId, out quotations))
            {
                quotations.TryGetValue(quotePolicyId, out quotation);
            }
            return quotation;
        }

        private List<Guid> UpdateQuotation(List<AlertOverridedQuotation> overridedQs)
        {
            List<Guid> quotationChangedInstruments = new List<Guid>();
            foreach (AlertOverridedQuotation quotaion in overridedQs)
            {
                if (!quotationChangedInstruments.Contains(quotaion.Instrument.Id))
                {
                    quotationChangedInstruments.Add(quotaion.Instrument.Id);
                }

                Dictionary<Guid, Quotation> quotations = null;
                if (!this.instrument2Quotations.TryGetValue(quotaion.Instrument.Id, out quotations))
                {
                    quotations = new Dictionary<Guid, Quotation>();
                    this.instrument2Quotations.Add(quotaion.Instrument.Id, quotations);
                }
                quotations[quotaion.QuotePolicyID] = Quotation.From(quotaion);
            }
            return quotationChangedInstruments;
        }

        private void SaveAlerts(List<Alert> alerts)
        {
            StringBuilder builder = new StringBuilder("<PriceAlerts>");

            foreach (Alert alert in alerts)
            {
                alert.BuildUpdateXmlString(builder);
            }

            builder.Append("</PriceAlerts>");

            this.SaveAlerts(builder.ToString());
        }

        private bool SaveAlerts(string alertsInXml, Guid? submitorId = null)
        {
            try
            {
                using (SqlConnection sqlConnection = new SqlConnection(Settings.ExternalSettings.Default.DBConnectionString))
                {
                    SqlCommand command = sqlConnection.CreateCommand();
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "P_UpdatePriceAlert";

                    command.Parameters.Add("@xml", SqlDbType.NText);
                    command.Parameters["@xml"].Value = alertsInXml;

                    if (submitorId != null)
                    {
                        command.Parameters.Add("@userID", SqlDbType.UniqueIdentifier);
                        command.Parameters["@userID"].Value = submitorId.Value;
                    }

                    sqlConnection.Open();
                    command.ExecuteNonQuery();

                    return true;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception);
                return false;
            }
        }

        private void FireAlertHit(IEnumerable<Alert> hitedAlerts)
        {
            if (this.PriceAlertHit != null)
            {
                this.PriceAlertHit(this, new PriceAlertHitEventArgs(hitedAlerts));
            }
        }

        private void FireAlertExpried(IEnumerable<Alert> expiredAlerts)
        {
            if (this.PriceAlertExpired != null)
            {
                this.PriceAlertExpired(this, new PriceAlertExpiredEventArgs(expiredAlerts));
            }
        }

        public bool Set(Guid submitorId, XmlNode alertsNode)
        {
            Logger.Info(string.Format("PriceAlertManager.Set {0}", alertsNode.OuterXml));
            if (this.SaveAlerts(alertsNode.OuterXml, submitorId))
            {
                lock (this.lockObj)
                {
                    foreach (XmlNode alertNode in alertsNode.ChildNodes)
                    {
                        if (string.Compare(alertNode.Name, "PriceAlert", true) == 0)
                        {
                            ModifyFlag flag = (ModifyFlag)(int.Parse(alertNode.Attributes["ModifyFlag"].Value));
                            if (flag == ModifyFlag.Add)
                            {
                                Alert alert = Alert.Create(submitorId, alertNode, quotationSetterProvider);

                                Logger.Info(string.Format("PriceAlertManager.Set add alert {0}", alert));
                                this.Add(alert);
                            }
                            else if (flag == ModifyFlag.Update)
                            {
                                Guid alertId = Guid.Parse(alertNode.Attributes["ID"].Value);
                                Alert alert = null;
                                if (this.alerts.TryGetValue(alertId, out alert))
                                {
                                    Guid oldInstrumentId = alert.InstrumentId;
                                    Logger.Info(string.Format("PriceAlertManager.Set update alert {0}", alert));
                                    alert.Update(alertNode, quotationSetterProvider);

                                    if (oldInstrumentId != alert.InstrumentId)
                                    {
                                        // remove from old list
                                        ICollection<Alert> oldList;
                                        if (this.instrument2Alerts.TryGetValue(oldInstrumentId, out oldList))
                                        {
                                            oldList.Remove(alert);
                                        }

                                        // add to new list
                                        List<Alert> newList;
                                        if (!this.instrument2Alerts.ContainsKey(alert.InstrumentId))
                                        {
                                            newList = new List<Alert>();
                                            newList.Add(alert);
                                            this.instrument2Alerts.Add(alert.InstrumentId, newList);
                                        }
                                        else
                                        {
                                            newList = this.instrument2Alerts[alert.InstrumentId] as List<Alert>;
                                            newList.Add(alert);
                                        }
                                    }
                                }

                            }
                            else if (flag == ModifyFlag.Remove)
                            {
                                Guid alertId = Guid.Parse(alertNode.Attributes["ID"].Value);
                                this.Remove(alertId);
                                Logger.Info(string.Format("PriceAlertManager.Set remove alert {0}", alertId));
                            }
                        }
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetQuotation(OverridedQ[] overridedQs)
        {
            if (overridedQs == null) return;
            lock (this.lockObj)
            {
                try
                {
                    this.SetQuotation(this.UpdateInstrumentQuotation(overridedQs));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }

        private List<AlertOverridedQuotation> UpdateInstrumentQuotation(OverridedQ[] overridedQs)
        {
            List<AlertOverridedQuotation> result = new List<AlertOverridedQuotation>();
            foreach (OverridedQ eachOverridedQ in overridedQs)
            {
                AlertInstrument instrument;
                if (!_alertInstrumentDict.TryGetValue(eachOverridedQ.InstrumentID, out instrument)) continue;
                AlertOverridedQuotation alertoverridedQ = new AlertOverridedQuotation(eachOverridedQ, instrument);

                if (!instrument.OverridedQuotations.ContainsKey(alertoverridedQ.QuotePolicyID))
                {
                    instrument.OverridedQuotations.Add(alertoverridedQ.QuotePolicyID, alertoverridedQ);
                }
                else
                {
                    AlertOverridedQuotation overridedQuotation = instrument.OverridedQuotations[alertoverridedQ.QuotePolicyID];

                    if (alertoverridedQ.Timestamp < overridedQuotation.Timestamp)
                    {
                        continue;
                    }
                    else
                    {
                        overridedQuotation.Merge(alertoverridedQ);
                    }
                }

                //Guids key = new Guids(overridedQ2.Instrument.Id, overridedQ2.QuotePolicyID);
                result.Add(alertoverridedQ);
            }
            return result;
        }


        private void SetQuotation(List<AlertOverridedQuotation> overridedQs)
        {
            if (overridedQs != null && overridedQs.Count > 0)
            {
                this.marketQuotations.Enqueue(overridedQs);
                this.marketQuotationsEvent.Set();
            }
        }

        private void Add(Alert alert)
        {
            ICollection<Alert> alerts = null;
            if (!this.instrument2Alerts.TryGetValue(alert.InstrumentId, out alerts))
            {
                alerts = new List<Alert>();
                this.instrument2Alerts.Add(alert.InstrumentId, alerts);
            }
            alerts.Add(alert);
            this.alerts.Add(alert.Id, alert);
        }

        private void Remove(Guid alertId)
        {
            Alert alert = null;
            if (this.alerts.TryGetValue(alertId, out alert))
            {
                this.instrument2Alerts[alert.InstrumentId].Remove(alert);
                this.alerts.Remove(alert.Id);
            }
        }
    }
}
