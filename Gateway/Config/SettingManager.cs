using log4net;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SystemController
{
    internal sealed class SettingManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SettingManager));
        internal static readonly SettingManager Default = new SettingManager();
        private SettingManager()
        {
        }

        internal string DBConnectionString { get; set; }

        internal string TransactionServiceUrl { get; private set; }

        internal string TransactionAdapterService { get; private set; }

        internal int QuotationTimeDiffInMS { get; private set; }

        internal int ExceedTimeInMSForDisplayQuotationCostTime { get; private set; }

        internal void LoadSettings()
        {
            this.DBConnectionString = this.GetItem("DBConnectionString");
            this.TransactionServiceUrl = this.GetItem("TransactionServiceUrl");
            this.TransactionAdapterService = this.GetItem("TransactionAdapterServiceUrl");
            this.QuotationTimeDiffInMS = int.Parse(this.GetItem("QuotationTimeDiffInMS"));
            this.ExceedTimeInMSForDisplayQuotationCostTime = int.Parse(this.GetItem("ExceedTimeInMSForDisplayQuotationCostTime"));
        }

        private string GetItem(string key)
        {
            try
            {
                return ConfigurationManager.AppSettings[key];
            }
            catch
            {
                Logger.ErrorFormat("Can't load config item key = {0}", key);
                throw;
            }
        }

    }
}
