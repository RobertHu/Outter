using log4net;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Service
{
    internal sealed class GatewayProxy : Protocal.Communication.HttpCommunicationService<Protocal.IGatewayService>, Protocal.IGatewayService
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(GatewayProxy));

        internal GatewayProxy(string serviceUrl)
            : base(serviceUrl) { }

        protected override ILog Logger
        {
            get { return _Logger; }
        }

        public void Register(string address, iExchange.Common.AppType appType)
        {
            this.Call(() => this.Service.Register(address, appType));
        }

        public InstrumentDayOpenCloseParams GetTradingInstrumentStatus(Guid instrumentId)
        {
            throw new NotImplementedException();
        }

        public Protocal.UpdateInstrumentTradingStatusMarketCommand GetTradingInstrumentStatusCommand()
        {
            return this.Call(() => this.Service.GetTradingInstrumentStatusCommand());
        }

        public void SetQuotation(iExchange.Common.OverridedQuotation[] quotations)
        {
            throw new NotImplementedException();
        }

        public TradeDayInfo GetTradeDay()
        {
            throw new NotImplementedException();
        }

        public List<TradingSession> GetTradingSession(Guid instrumentId)
        {
            throw new NotImplementedException();
        }

        public void AddCheckPointForTest(List<Protocal.InstrumentCheckPoint> checkPoints)
        {
            throw new NotImplementedException();
        }

        public TradingSession GetTradingSession(Guid instrumentId, DateTime baseTime)
        {
            throw new NotImplementedException();
        }


        public Protocal.UpdateInstrumentDayOpenCloseTimeMarketCommand GetInstrumentDayOpenCloseTimeCommand()
        {
            return this.Call(() => this.Service.GetInstrumentDayOpenCloseTimeCommand());
        }


        public Protocal.UpdateTradeDayInfoMarketCommand GetTradeDayInfoCommand()
        {
            return this.Call(() => this.Service.GetTradeDayInfoCommand());
        }



        public void UpdateLoginInfo(iExchange.Common.TraderServerType appType, string onlineXml, TimeSpan expireTime)
        {
            throw new NotImplementedException();
        }


        public Protocal.ResetNotify GetResetNotify()
        {
            return this.Call(() => this.Service.GetResetNotify());
        }
    }
}
