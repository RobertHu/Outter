using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;
using System.Configuration;

namespace iExchange.StateServer.Adapter
{
    internal sealed class TradeDayManager
    {
        internal static readonly TradeDayManager Default = new TradeDayManager();

        private GatewayServiceProxy _proxy;
        static TradeDayManager() { }
        private TradeDayManager()
        {
            _proxy = new GatewayServiceProxy(ConfigurationManager.AppSettings["GatewayUrl"]);
        }

        internal DateTime GetTradeDay()
        {
            var tradeDayInfo = _proxy.GetTradeDay();
            return tradeDayInfo == null ? DateTime.MinValue : tradeDayInfo.TradeDay;
        }

    }

    internal sealed class GatewayServiceProxy : Protocal.Communication.HttpCommunicationService<Protocal.IGatewayService>, Protocal.IGatewayService
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(GatewayServiceProxy));

        internal GatewayServiceProxy(string url)
            : base(url) { }

        protected override log4net.ILog Logger
        {
            get { throw new NotImplementedException(); }
        }

        public void AddCheckPointForTest(List<Protocal.InstrumentCheckPoint> checkPoints)
        {
            throw new NotImplementedException();
        }

        public Protocal.UpdateInstrumentDayOpenCloseTimeMarketCommand GetInstrumentDayOpenCloseTimeCommand()
        {
            throw new NotImplementedException();
        }

        public Protocal.TradingInstrument.TradeDayInfo GetTradeDay()
        {
            return this.Call(() => this.Service.GetTradeDay());
        }

        public Protocal.UpdateTradeDayInfoMarketCommand GetTradeDayInfoCommand()
        {
            throw new NotImplementedException();
        }

        public Protocal.TradingInstrument.InstrumentDayOpenCloseParams GetTradingInstrumentStatus(Guid instrumentId)
        {
            throw new NotImplementedException();
        }

        public Protocal.UpdateInstrumentTradingStatusMarketCommand GetTradingInstrumentStatusCommand()
        {
            throw new NotImplementedException();
        }

        public Protocal.TradingInstrument.TradingSession GetTradingSession(Guid instrumentId, DateTime baseTime)
        {
            throw new NotImplementedException();
        }

        public List<Protocal.TradingInstrument.TradingSession> GetTradingSession(Guid instrumentId)
        {
            throw new NotImplementedException();
        }

        public void Register(string address, Common.AppType appType)
        {
            throw new NotImplementedException();
        }

        public void SetQuotation(Common.OverridedQuotation[] quotations)
        {
            throw new NotImplementedException();
        }

        public void UpdateLoginInfo(Common.TraderServerType appType, string onlineXml, TimeSpan expireTime)
        {
            throw new NotImplementedException();
        }


        public Protocal.ResetNotify GetResetNotify()
        {
            throw new NotImplementedException();
        }
    }


}