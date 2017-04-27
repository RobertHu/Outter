using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.Settings
{
    public sealed class LoadExternalSettingException : Exception
    {
        public LoadExternalSettingException(string key, string msg)
            : this(key, msg, null)
        {
        }

        public LoadExternalSettingException(string key, string msg, Exception innerException)
            : base(msg, innerException)
        {
            this.Key = key;
        }

        public string Key { get; private set; }
    }

    public sealed class ExternalSettings
    {
        public static readonly ExternalSettings Default = new ExternalSettings();

        static ExternalSettings() { }

        private ExternalSettings()
        {
        }

        public bool AutoCloseFirstInFirstOut { get; private set; }
        public string BugFix_OrderPriceMaxMovePercent { get; private set; }
        public bool CheckHighLowForAutoFill { get; private set; }
        internal string DBConnectionString { get; set; }

        internal string GatewayServiceUrl { get; private set; }

        internal string CommandCollectServiceUrl { get; private set; }

        internal string BroadcastServiceUrl { get; private set; }

        public void LoadSettings()
        {
            this.DBConnectionString = this.LoadSettingItem("DBConnectionString");
            this.GatewayServiceUrl = this.LoadSettingItem("GateWayServiceUrl");
            this.CommandCollectServiceUrl = this.LoadSettingItem("CommandCollectServiceUrl");
            this.BroadcastServiceUrl = this.LoadSettingItem("BroadcastServiceUrl");
            string autoCloseFirstInFirstOut = this.LoadSettingItem("AutoCloseFirstInFirstOut");
            if (!string.IsNullOrEmpty(autoCloseFirstInFirstOut))
            {
                this.AutoCloseFirstInFirstOut = XmlConvert.ToBoolean(autoCloseFirstInFirstOut);
            }
        }

        private string LoadSettingItem(string name)
        {
            try
            {
                return ConfigurationManager.AppSettings[name];
            }
            catch (Exception ex)
            {
                throw new LoadExternalSettingException(name, string.Format("key can'n be found", name), ex);
            }
        }




    }
}
