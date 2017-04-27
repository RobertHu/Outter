using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal abstract class AutoFillServiceBase
    {
        protected AutoFillServiceBase() { }

        internal abstract ILog Logger { get; }

        internal abstract bool ShouldAutoFill(Transaction tran);

        internal abstract bool IsPriceInRangeOfAutoFill(Transaction tran);
        //{
        //    return true;
        //    //var instrument = _owner.TradingInstrument;
        //    //return instrument.IsPriceInRangeOfAutoFill(order.IsBuy, order.SetPrice, _owner.Owner);
        //}

    }


    internal sealed class AutoFillService : AutoFillServiceBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(AutoFillService));

        internal static readonly AutoFillService Default = new AutoFillService();

        static AutoFillService() { }
        private AutoFillService() { }

        internal override bool ShouldAutoFill(Transaction tran)
        {
            if (tran.OrderType == OrderType.SpotTrade && tran.SettingInstrument().IsAutoFill)
            {
                return this.ShouldOrdersAutoFill(tran);
            }
            else
            {
                return false;
            }
        }


        private bool ShouldOrdersAutoFill(Transaction tran)
        {
            DealingPolicyPayload dealingPolicyDetail = tran.DealingPolicyPayload();
            foreach (Order eachOrder in tran.Orders)
            {
                if (eachOrder.Phase != OrderPhase.Placed || eachOrder.Lot > dealingPolicyDetail.AutoDQMaxLot)
                {
                    Logger.WarnFormat("check CanOrderAutoFill failed, order.phase = {0}, order.lot = {1}, autoDQMaxLot = {2}, tranId = {3}", eachOrder.Phase, eachOrder.Lot, dealingPolicyDetail.AutoDQMaxLot, tran.Id);
                    return false;
                }
            }
            return true;
        }

        internal override ILog Logger
        {
            get { return _Logger; }
        }

        internal override bool IsPriceInRangeOfAutoFill(Transaction tran)
        {
            if (!ExternalSettings.Default.CheckHighLowForAutoFill) return true;
            foreach (Order eachOrder in tran.Orders)
            {
                var instrument = tran.AccountInstrument;
                if (!instrument.IsPriceInRangeOfAutoFill(eachOrder.IsBuy, eachOrder.SetPrice,instrument.GetQuotation(tran.SubmitorQuotePolicyProvider), tran.SubmitorQuotePolicyProvider))
                {
                    Logger.Warn(string.Format("Order:(id = {0}; instrument = {1}; lot = {2}) can not be auto filled for the price is out of range", eachOrder.Id, tran.InstrumentId, eachOrder.Lot));
                    return false;
                }
            }
            return true;
        }
    }

    internal sealed class BOAutoFillService : AutoFillServiceBase
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(BOAutoFillService));

        internal static readonly BOAutoFillService Default = new BOAutoFillService();

        static BOAutoFillService() { }
        private BOAutoFillService() { }

        internal override ILog Logger
        {
            get { return _Logger; }
        }

        internal override bool ShouldAutoFill(Transaction tran)
        {
            return true;
        }

        internal override bool IsPriceInRangeOfAutoFill(Transaction tran)
        {
            return true;
        }
    }
}
