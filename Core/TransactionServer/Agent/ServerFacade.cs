using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Interact;
using Core.TransactionServer.Agent.Service;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine.iExchange;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace Core.TransactionServer.Agent
{
    public sealed class ServerFacade
    {
        public static readonly ServerFacade Default = new ServerFacade();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerFacade));
        private Server _server;

        static ServerFacade() { }
        private ServerFacade()
        {
            InstrumentPriceStatusManager.Default.InstrumentPriceStatusChanged += Default_InstrumentPriceStatusChanged;
        }

        void Default_InstrumentPriceStatusChanged(object sender, InstrumentPriceStatusChangedEventArgs e)
        {

        }

        internal Server Server
        {
            get
            {
                return _server;
            }
        }

        internal GatewayProxy GatewayProxy { get; private set; }


        public void Start()
        {
            try
            {
                Logger.Info("load external setting");
                ExternalSettings.Default.LoadSettings();
                this.GatewayProxy = new GatewayProxy(ExternalSettings.Default.GatewayServiceUrl);
                _server = new Server("TransactionServer", "CacheFiles");
                this.InitializePriceAlertManager();
                _server.Start();
                Logger.Info("Start up");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void InitializePriceAlertManager()
        {
            PriceAlert.Manager.Default.PriceAlertHit += (s, e) =>
            {
                this.ProcessPriceAlertEvent(Protocal.Commands.AlertType.Hit, e.HitAlerts);
            };

            PriceAlert.Manager.Default.PriceAlertExpired += (s, e) =>
            {
                this.ProcessPriceAlertEvent(Protocal.Commands.AlertType.Expired, e.ExpiredAlerts);
            };
        }

        private void ProcessPriceAlertEvent(Protocal.Commands.AlertType alertType, IEnumerable<PriceAlert.Alert> alerts)
        {
            try
            {
                Broadcaster.Default.Add(BroadcastBLL.CommandFactory.CreatePriceAlertCommand(alerts, alertType));
                Logger.InfoFormat("broadcast price alert command alertType = {0}", alertType);
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("ProcessPriceAlertEvent alertType = {0}", alertType), ex);
            }
        }


        public void Stop()
        {
            try
            {
                Hoster.Default.Stop();
                TransactionExpireChecker.Default.Stop();
                CurrencyRateCaculator.Default.Stop();
                Logger.Info("Stop");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }



    }
}
