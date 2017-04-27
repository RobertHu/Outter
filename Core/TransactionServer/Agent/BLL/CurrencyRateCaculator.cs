using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Settings;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.BLL
{
    internal sealed class CurrencyRateCaculator
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CurrencyRateCaculator));

        internal static readonly CurrencyRateCaculator Default = new CurrencyRateCaculator();

        private sealed class DefaultQuotePolicyProvider : IQuotePolicyProvider
        {
            public Guid PublicQuotePolicyId
            {
                get { return Settings.Setting.Default.SystemParameter.DefaultQuotePolicyId.Value; }
            }

            public Guid? PrivateQuotePolicyId
            {
                get { return null; }
            }
        }

        private List<CurrencyRate> _currencyRates = new List<CurrencyRate>();
        private Dictionary<string, DateTime> _lastCaculateTimestamps = new Dictionary<string, DateTime>();
        private volatile bool _stopped = true;
        private IQuotePolicyProvider _quotePolicyProvider = new DefaultQuotePolicyProvider();

        private object _mutex = new object();


        static CurrencyRateCaculator() { }

        private CurrencyRateCaculator()
        {
        }

        internal void Start()
        {
            if (!_stopped) return;
            if (this.Setting.SystemParameter.CurrencyRateUpdateDuration > 0)
            {
                _stopped = false;
                new Thread(this.DoWorkHandle) { IsBackground = true }.Start();
            }
        }

        private void DoWorkHandle()
        {
            while (!_stopped)
            {
                Thread.Sleep(this.Duration);
                try
                {
                    this.FillCurrencyRates();
                    foreach (CurrencyRate currencyRate in _currencyRates)
                    {
                        this.Caculate(currencyRate);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            }
        }


        internal void Stop()
        {
            _stopped = true;
        }


        private void FillCurrencyRates()
        {
            _currencyRates.Clear();
            this.Setting.FillCurrencyRate(_currencyRates);
        }

        private bool TryGetLastUpdateTime(string key, out DateTime lastTime)
        {
            lock (_mutex)
            {
                return _lastCaculateTimestamps.TryGetValue(key, out lastTime);
            }
        }


        private int Duration
        {
            get
            {
                return this.Setting.SystemParameter.CurrencyRateUpdateDuration * 1000;
            }
        }

        private string ConnectionString
        {
            get
            {
                return ExternalSettings.Default.DBConnectionString;
            }
        }

        private Setting Setting
        {
            get
            {
                return Settings.Setting.Default;
            }
        }

        public void Caculate(CurrencyRate currencyRate, Guid? accountId = null)
        {
            try
            {
                if (this.Setting.SystemParameter.DefaultQuotePolicyId != null && currencyRate.DependingInstrumentId != null)
                {

                    var instrumentId = currencyRate.DependingInstrumentId.Value;
                    var instrument = Market.MarketManager.Default[currencyRate.DependingInstrumentId.Value];
                    if (instrument == null) return;
                    var defaultOverridedQuotation = instrument.GetQuotation(_quotePolicyProvider);
                    if (defaultOverridedQuotation == null || defaultOverridedQuotation.Ask == null || defaultOverridedQuotation.Bid == null) return;//maybe closed

                    string key = string.Format("{0}-{1}", currencyRate.SourceCurrency.Id, currencyRate.TargetCurrency.Id);
                    DateTime lastTime;
                    if (!this.TryGetLastUpdateTime(key, out lastTime) || defaultOverridedQuotation.Timestamp > lastTime)
                    {
                        decimal price = ((decimal)defaultOverridedQuotation.Ask + (decimal)defaultOverridedQuotation.Bid) / 2;

                        int decimals = 8;
                        if (!currencyRate.Inverted)
                        {
                            var settingInstrument = this.Setting.GetInstrument(instrumentId);
                            if (settingInstrument == null) return;
                            decimals = (int)Math.Log10(settingInstrument.Denominator);
                            if (settingInstrument.Denominator != Math.Pow(10, decimals))
                            {
                                decimals = 8;
                            }
                        }

                        Logger.InfoFormat("calcualte currency rate, accountId = {0}", accountId);
                        double rate = Math.Round((double)(currencyRate.Inverted ? 1 / price : price), decimals, MidpointRounding.AwayFromZero);
                        if (rate != (double)currencyRate.RateIn || rate != (double)currencyRate.RateOut)
                        {
                            if (this.SaveCurrencyRate(currencyRate, rate))
                            {
                                currencyRate.Update((decimal)rate, (decimal)rate);
                                this.Boardcast(currencyRate, rate, accountId);
                            }
                            lock (_mutex)
                            {
                                this._lastCaculateTimestamps[key] = defaultOverridedQuotation.Timestamp;
                            }
                            Logger.InfoFormat("recalcualte currency rate success, accountId = {0}", accountId);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error("TransactionServer.CurrencyRateCaculaotr.Caculate", exception);
            }
        }

        private void Boardcast(CurrencyRate currencyRate, double rate, Guid? accountId)
        {
            try
            {
                var updateNode = this.CreateUpdateContent(currencyRate, rate);
                string content = updateNode.ToString();
                Logger.InfoFormat("Boardcast content = {0}, accountId = {1}", content, accountId);
                Broadcaster.Default.Add(BroadcastBLL.CommandFactory.CreateCurrencyRateUpdateCommand(content));
                Setting.Default.SettingInfo.Update(updateNode);
                this.RecalculateForAccounts(accountId);
            }
            catch (Exception exception)
            {
                Logger.Error("TransactionServer.CurrencyRateCaculaotr.Boardcast", exception);
            }
        }

        private void RecalculateForAccounts(Guid? accountId)
        {
            if (accountId != null)
            {
                var account = TradingSetting.Default.GetAccount(accountId.Value);
                account.CalculateForCurrencyRateChanged();
            }

            TradingSetting.Default.DoParallelForAccounts(m =>
            {
                if (accountId != null && accountId == m.Id) return;
                m.CalculateForCurrencyRateChanged();
            });
        }


        private XElement CreateUpdateContent(CurrencyRate currencyRate, double rate)
        {
            XElement updateNode = new XElement("Update");

            XElement modifyNode = new XElement("Modify");
            updateNode.Add(modifyNode);

            XElement currencyRateNode = new XElement("CurrencyRate");
            modifyNode.Add(currencyRateNode);

            currencyRateNode.SetAttributeValue("SourceCurrencyID", currencyRate.SourceCurrency.Id);
            currencyRateNode.SetAttributeValue("TargetCurrencyID", currencyRate.TargetCurrency.Id);
            currencyRateNode.SetAttributeValue("RateIn", rate);
            currencyRateNode.SetAttributeValue("RateOut", rate);
            return updateNode;
        }


        private bool SaveCurrencyRate(CurrencyRate currencyRate, double rate)
        {
            using (SqlConnection connection = new SqlConnection(this.ConnectionString))
            {
                SqlCommand sqlCommand = connection.CreateCommand();
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure;
                sqlCommand.CommandText = "CurrencyRate_Set";
                sqlCommand.Parameters.Add(new SqlParameter("@sourceCurrencyId", currencyRate.SourceCurrency.Id));
                sqlCommand.Parameters.Add(new SqlParameter("@targetCurrencyId", currencyRate.TargetCurrency.Id));
                sqlCommand.Parameters.Add(new SqlParameter("@rateIn", rate));
                sqlCommand.Parameters.Add(new SqlParameter("@rateOut", rate));

                SqlParameter returnParameter = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
                returnParameter.Direction = System.Data.ParameterDirection.ReturnValue;
                sqlCommand.Parameters.Add(returnParameter);

                connection.Open();
                sqlCommand.ExecuteNonQuery();

                int returnValue = (int)returnParameter.Value;
                return returnValue == 0;
            }
        }
    }


}
