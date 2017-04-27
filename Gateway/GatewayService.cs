using SystemController.Factory;
using SystemController.InstrumentBLL;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SystemController
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class GatewayService : IGatewayService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GatewayService));

        public void Register(string address, iExchange.Common.AppType appType)
        {
            try
            {
                Logger.InfoFormat("register address = {0}, appType = {1}", address, appType);
                Broadcaster.Default.AddClient(address, appType);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public Protocal.TradingInstrument.InstrumentDayOpenCloseParams GetTradingInstrumentStatus(Guid instrumentId)
        {
            try
            {
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        public void SetQuotation(iExchange.Common.OverridedQuotation[] quotations)
        {
            try
            {
                Logger.Info("Set Quotation");
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public Protocal.UpdateInstrumentTradingStatusMarketCommand GetTradingInstrumentStatusCommand()
        {
            try
            {
                return InstrumentTradingStatusKeeper.Default.InstrumentTradingStatusCommand;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        public Protocal.TradingInstrument.TradeDayInfo GetTradeDay()
        {
            try
            {
                return TradeDayManager.Default.TradeDayInfo;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        public List<Protocal.TradingInstrument.TradingSession> GetTradingSession(Guid instrumentId)
        {
            try
            {
                return TradingTimeFactory.Default.GetTradingSessions(instrumentId);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }


        public void AddCheckPointForTest(List<InstrumentCheckPoint> checkPoints)
        {
            try
            {
                //InstrumentBLL.Manager.Default.AddCheckPoint(checkPoints);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        public Protocal.TradingInstrument.TradingSession GetTradingSession(Guid instrumentId, DateTime baseTime)
        {
            try
            {
                return null;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }


        public UpdateInstrumentDayOpenCloseTimeMarketCommand GetInstrumentDayOpenCloseTimeCommand()
        {
            try
            {
                return InstrumentBLL.InstrumentDayOpenCloseTimeKeeper.Default.InstrumentDayOpenCloseTimeMarketCommand;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        public UpdateTradeDayInfoMarketCommand GetTradeDayInfoCommand()
        {
            try
            {
                return MarketCommandFactory.CreateUpdateTradeDayInfoCommand();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }

        public void UpdateLoginInfo(TraderServerType appType, string onlineXml, TimeSpan expireTime)
        {
            try
            {
                Logger.InfoFormat("UpdateLoginInfo appType = {0}", appType);
                LoginInfoManager.Default.Add(new LoginInfo(appType, onlineXml, expireTime));
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public ResetNotify GetResetNotify()
        {
            try
            {
                var result = Server.Default.ResetNotify;
                if (result == null)
                {
                    Logger.Info("GetResetNotify  failed reset notify not exist");
                    return null;
                }
                Logger.InfoFormat("GetResetNotify tradeDay = {0}", result.TradeDay);
                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return null;
            }
        }
    }

    internal sealed class LoginInfo
    {
        internal LoginInfo(TraderServerType type, string onLineXml, TimeSpan expireTime)
        {
            this.Type = type;
            this.OnLineXml = onLineXml;
            this.ExpireTime = expireTime;
        }

        internal TraderServerType Type { get; private set; }
        internal string OnLineXml { get; private set; }
        internal TimeSpan ExpireTime { get; private set; }
    }



    internal sealed class LoginInfoManager : Protocal.ThreadQueueBase<LoginInfo>
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LoginInfoManager));
        internal static readonly LoginInfoManager Default = new LoginInfoManager();
        private StateServerService.Service _stateServer;
        static LoginInfoManager() { }
        private LoginInfoManager()
            : base(100)
        {
            _stateServer = new StateServerService.Service();
        }

        public override void DoWork(LoginInfo item)
        {
            _stateServer.UpdateLoginInfo(item.Type, item.OnLineXml, item.ExpireTime);
        }

        public override void RecordLog(Exception ex)
        {
            Logger.Error(ex);
        }
    }




}
