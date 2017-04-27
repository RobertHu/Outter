using Core.TransactionServer;
using Core.TransactionServer.Agent;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Validator;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Engine.iExchange.BLL;
using Core.TransactionServer.Engine.iExchange.BLL.OrderBLL;
using Core.TransactionServer.Engine.iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using iExchange.Common;
using Protocal;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness;

namespace Core.TransactionServer.Engine.iExchange
{
    internal sealed class PlaceService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PlaceService));

        internal void Place(Transaction tran)
        {
            Stopwatch stopWatch = new Stopwatch();
            try
            {
                iExchangeEngine.Default.AcceptPlace(tran.Owner.Id, tran.Id);
                this.ProcessForPostPlaced(tran);
            }
            catch (TransactionServerException tranEx)
            {
                iExchangeEngine.Default.RejectPlace(tran.Owner.Id, tran.Id, tranEx.ErrorCode, tranEx.ErrorDetail);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                iExchangeEngine.Default.RejectPlace(tran.Owner.Id, tran.Id, TransactionError.RuntimeError, ex.ToString());
            }
        }

        internal void AcceptPlace(Transaction tran)
        {
            try
            {
                Logger.InfoFormat("accept place {0}", tran.ToString());
                iExchangeEngine.Default.AcceptPlace(tran.Owner.Id, tran.Id);
                this.ProcessForPostPlaced(tran);
            }
            catch (TransactionServerException tranEx)
            {
                iExchangeEngine.Default.RejectPlace(tran.Owner.Id, tran.Id, tranEx.ErrorCode, tranEx.ErrorDetail);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                iExchangeEngine.Default.RejectPlace(tran.Owner.Id, tran.Id, TransactionError.RuntimeError, ex.ToString());
            }
        }

        private void ProcessForPostPlaced(Transaction tran)
        {
            if (tran.ShouldAutoFill())
            {
                if (!tran.IsPriceInRangeOfAutoFill())
                {
                    iExchangeEngine.Default.Cancel(tran, CancelReason.InvalidPrice);
                }
                else
                {
                    Logger.InfoFormat("begin do auto fill, tranId = {0}", tran.Id);
                    this.DoAutoFill(tran);
                }
            }
            else if (tran.EndTime <= tran.SettingInstrument().DayCloseTime || tran.SettingInstrument().DayCloseTime == default(DateTime))
            {
                TransactionExpireChecker.Default.Add(tran);
                if (tran.ShouldTryHit)
                {
                    Logger.Info("Begin try hit");
                    this.TryHitPriceAndExecute(tran.FirstOrder);
                }
            }
        }


        private void DoAutoFill(Transaction tran)
        {
            if (tran.OrderCount == 0)
            {
                throw new TransactionServerException(TransactionError.RuntimeError, string.Format("FillBOBestPrice tranId = {0} does not  has any order", tran.Id));
            }
            if (tran.Type == TransactionType.Single)
            {
                if (tran.FirstOrder.DQMaxMove > 0)
                {
                    Logger.InfoFormat("try hit, tranId = {0}", tran.Id);
                    this.TryHitPrice(tran.FirstOrder);
                }

                if (tran.OrderType == OrderType.BinaryOption && (tran.FirstOrder.SetPrice == null))
                {
                    this.FillBOBestPrice(tran);
                }
            }

            if (tran.DeferredToFill)
            {
                Logger.InfoFormat("in deferred to fill, tranId = {0}", tran.Id);
                DeferredAutoFillManager.Default.Add(tran);
            }
            else
            {
                this.Execute(tran);
            }
        }

        private void FillBOBestPrice(Transaction tran)
        {
            var quotation = tran.AccountInstrument.GetQuotation(tran.SubmitorQuotePolicyProvider);
            var order = (Agent.BinaryOption.Order)tran.FirstOrder;
            order.UpdateBestPrice(Agent.Market.MarketManager.Now, quotation.BuyPrice, quotation.SellPrice, tran.SettingInstrument());
        }


        private void Execute(Transaction tran)
        {
            Logger.InfoFormat("in auto fill, tranId = {0}", tran.Id);
            List<OrderPriceInfo> infoList = tran.CreateOrderPriceInfo();
            var executeContext = new ExecuteContext(tran.Owner.Id, tran.Id, ExecuteStatus.Filled, infoList);
            var executeRequest = new OrderExecuteEventArgs(executeContext);
            Logger.InfoFormat("Engine ready to execute transaction, tranId = {0}", tran.Id);
            iExchangeEngine.Default.Execute(executeRequest);
        }

        private void TryHitPriceAndExecute(Order order)
        {
            var status = this.TryHitPrice(order);
            if (status == OrderHitStatus.ToAutoFill)
            {
                Price buy, sell;
                order.GetBuyAndSellPrice(out buy, out sell);
                var executeRequest = new OrderExecuteEventArgs(new ExecuteContext(order.Owner.Owner.Id, order.Owner.Id, order.Id, ExecuteStatus.Filled, new List<OrderPriceInfo> { new OrderPriceInfo(order.Id, buy, sell) }));
                iExchangeEngine.Default.Execute(executeRequest);
            }
        }

        private OrderHitStatus TryHitPrice(Order order)
        {
            var tran = order.Owner;
            var account = tran.Owner;
            var quotation = tran.AccountInstrument.GetQuotation(tran.SubmitorQuotePolicyProvider);
            if (!order.IsValid(quotation)) return OrderHitStatus.None;
            return order.HitSetPrice(quotation, false);
        }
    }
}
