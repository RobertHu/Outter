using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Settings
{
    internal sealed class SettingManager : ISettingProvider
    {
        internal static readonly SettingManager Default = new SettingManager();

        static SettingManager() { }
        private SettingManager()
        {
            this.SettingInfo = new SettingInfo();
            this.Setting = new Setting(this.SettingInfo);
        }

        internal SettingInfo SettingInfo { get; private set; }

        public Setting Setting { get; private set; }
    }
}
