using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Settings
{
    public sealed class SettingFacade
    {
        public static readonly SettingFacade Default = new SettingFacade();
        private Setting _settingManager;
        private SettingFacade() { }

        public void Initialize(Setting settingManager)
        {
            Debug.Assert(settingManager != null);
            _settingManager = settingManager;
        }

        public Setting SettingManager
        {
            get
            {
                return _settingManager;
            }
        }


    }
}
