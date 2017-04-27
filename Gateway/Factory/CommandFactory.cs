using iExchange.Common;
using log4net;
using Newtonsoft.Json;
using Protocal;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using SystemController.InstrumentBLL;

namespace SystemController.Factory
{
    internal static class CommandFactory
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommandFactory));

        internal static Protocal.Command CreateSettingCommand(AppType appType, string updateNode)
        {
            return new Protocal.SettingCommand
            {
                Content = updateNode,
                AppType = appType
            };
        }




    }
}
