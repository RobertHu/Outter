using Protocal;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Protocal
{
    [ServiceContract]
    public interface IGatewayService
    {
        [OperationContract]
        void Register(string address, iExchange.Common.AppType appType);

        [OperationContract]
        InstrumentDayOpenCloseParams GetTradingInstrumentStatus(Guid instrumentId);

        [OperationContract]
        Protocal.UpdateInstrumentTradingStatusMarketCommand GetTradingInstrumentStatusCommand();

        [OperationContract]
        Protocal.UpdateInstrumentDayOpenCloseTimeMarketCommand GetInstrumentDayOpenCloseTimeCommand();

        [OperationContract]
        Protocal.UpdateTradeDayInfoMarketCommand GetTradeDayInfoCommand();

        [OperationContract]
        void SetQuotation(iExchange.Common.OverridedQuotation[] quotations);

        [OperationContract]
        Protocal.TradingInstrument.TradeDayInfo GetTradeDay();

        [OperationContract]
        List<Protocal.TradingInstrument.TradingSession> GetTradingSession(Guid instrumentId);

        [OperationContract]
        void AddCheckPointForTest(List<InstrumentCheckPoint> checkPoints);

        [OperationContract(Name = "GetSingleTradingSession")]
        TradingSession GetTradingSession(Guid instrumentId, DateTime baseTime);

        [OperationContract]
        void UpdateLoginInfo(iExchange.Common.TraderServerType appType, string onlineXml, TimeSpan expireTime);

        [OperationContract]
        ResetNotify GetResetNotify();
    }



    [DataContract]
    public class InstrumentCheckPoint
    {
        [DataMember]
        public Guid InstrumentId { get; set; }

        [DataMember]
        public DateTime CheckTime { get; set; }

        [DataMember]
        public InstrumentStatus Status { get; set; }
    }
}
