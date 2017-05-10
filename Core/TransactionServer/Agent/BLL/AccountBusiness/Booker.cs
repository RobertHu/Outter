using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iExchange.Common;
using Protocal;
using Core.TransactionServer.Agent.BLL.AccountBusiness.TypeExtensions;
using Core.TransactionServer.Agent.BLL.PreCheck;
using log4net;
using Core.TransactionServer.Agent.Reset;
using Protocal.Physical;
using Core.TransactionServer.Agent.Physical.Delivery;
using Core.TransactionServer.Agent.Util;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Agent.Interact;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal sealed class PlaceContext
    {
        internal static readonly PlaceContext Empty = new PlaceContext(false, null);

        internal PlaceContext(bool isBook, DateTime? executeTime)
        {
            this.IsBook = isBook;
            this.ExecuteTime = executeTime;
        }

        internal bool IsBook { get; private set; }

        internal DateTime? ExecuteTime { get; private set; }

        internal DateTime? TradeDay
        {
            get
            {
                if (this.ExecuteTime == null) return null;
                return this.ExecuteTime.Value.Date;
            }
        }
    }


    internal static class Booker
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Booker));

        internal static bool Book(this Account account, Token token, Protocal.TransactionBookData tranData)
        {
            bool useCurrentSetting;
            Settings.Instrument instrument = Settings.Setting.Default.GetInstrument(tranData.InstrumentId);
            TransactionError error = account.BookForPhysicalTran(tranData, out useCurrentSetting);
            if (error != TransactionError.OK) throw new TransactionServerException(error);
            if (!useCurrentSetting)
            {
                ResetManager.Default.LoadHistorySetting(tranData.TradeDay, "Book");
            }

            var settingAccount = Settings.Setting.Default.GetAccount(account.Id, tranData.TradeDay);
            if (settingAccount == null || tranData.ExecuteTime < settingAccount.BeginTime)
            {
                throw new TransactionServerException(TransactionError.AccountIsNotTrading);
            }

            if (token.AppType == AppType.PhysicalTerminal && instrument.Category == InstrumentCategory.Physical)
            {
                if (!tranData.TryFillExecutePrice(instrument, account))
                {
                    Logger.Error("TransactionServer.Book Can't get live price to book \r\n");
                    throw new TransactionServerException(TransactionError.InvalidPrice);
                }
            }
            Transaction tran = Periphery.TransactionBLL.TransactionFacade.CreateBookTran(instrument.Category, account, tranData);
            TransactionPlacer.Default.Verify(account, tran, false, tranData.AppType, PlaceContext.Empty);
            if (tranData.CheckMargin && !account.HasEnoughMoneyToPlace(tran))
            {
                throw new TransactionServerException(TransactionError.MarginIsNotEnough);
            }
            if (account.ExecuteBookTran(tran, tranData))
            {
                if (tran.IsPhysical)
                {
                    UpdatePhysicalRequestStatus(tran);
                }
                return true;
            }
            return false;
        }


        private static void UpdatePhysicalRequestStatus(Transaction tran)
        {
            foreach (Physical.PhysicalOrder eachOrder in tran.Orders)
            {
                if (eachOrder.PhysicalTradeSide == PhysicalTradeSide.Delivery && eachOrder.PhysicalRequestId != null)
                {
                    DeliveryRequest request;
                    if (DeliveryRequestManager.Default.TryGet(eachOrder.PhysicalRequestId.Value, out request))
                    {
                        request.DeliveryRequestStatus = DeliveryRequestStatus.Approved;
                    }
                }
            }
        }




        private static bool ExecuteBookTran(this Account account, Transaction tran, Protocal.TransactionBookData tranData)
        {
            var orderPriceInfos = tranData.CreateOrderPriceInfo(tran);
            ExecuteContext context = new ExecuteContext(account.Id, tran.Id, null, true, tranData.CheckMargin, ExecuteStatus.Filled, orderPriceInfos)
            {
                BookInfo = new BookInfo(tranData.ExecuteTime, tranData.TradeDay, tranData.CheckMargin)
            };
            return TransactionExecutor.Default.Execute(context);
        }


        private static List<OrderPriceInfo> CreateOrderPriceInfo(this Protocal.TransactionBookData tranData, Transaction tran)
        {
            var buySellPrice = tranData.ParseBuyAnSellPrice();
            Price buyPrice = buySellPrice.Item1;
            Price sellPrice = buySellPrice.Item2;
            List<OrderPriceInfo> result = new List<OrderPriceInfo>();
            foreach (var eachOrder in tran.Orders)
            {
                result.Add(new OrderPriceInfo(eachOrder.Id, buyPrice, sellPrice));
            }
            return result;
        }



        private static Tuple<Price, Price> ParseBuyAnSellPrice(this Protocal.TransactionBookData tranData)
        {
            Price buy = null;
            Price sell = null;
            foreach (var eachOrder in tranData.Orders)
            {
                if (eachOrder.IsBuy)
                {
                    if (buy == null) buy = eachOrder.ExecutePrice.CreatePrice(tranData.InstrumentId, null);
                }
                else
                {
                    if (sell == null) sell = eachOrder.ExecutePrice.CreatePrice(tranData.InstrumentId, null);
                }
            }

            if (buy == null) buy = sell;
            if (sell == null) sell = buy;
            return Tuple.Create(buy, sell);
        }


        private static bool TryFillExecutePrice(this Protocal.TransactionBookData bookData, Settings.Instrument instrument, Account account)
        {
            foreach (OrderBookData eachOrderBookData in bookData.Orders)
            {
                PhysicalOrderBookData eachPhysicalOrderBookData = (PhysicalOrderBookData)eachOrderBookData;
                if (eachPhysicalOrderBookData.PhysicalTradeSide == PhysicalTradeSide.Deposit || eachPhysicalOrderBookData.PhysicalTradeSide == PhysicalTradeSide.Delivery)
                {
                    if (eachOrderBookData.ExecutePrice != null) continue;

                    string executePrice = null;
                    if (eachPhysicalOrderBookData.PhysicalTradeSide == PhysicalTradeSide.Deposit)
                    {
                        executePrice = instrument.DepositPrice;
                    }
                    else if (eachPhysicalOrderBookData.PhysicalTradeSide == PhysicalTradeSide.Delivery)
                    {
                        Guid physicalRequestId = eachPhysicalOrderBookData.PhysicalRequestId.Value;
                        DeliveryRequest deliveryRequest = null;
                        if (DeliveryRequestManager.Default.TryGet(physicalRequestId, out deliveryRequest))
                        {
                            executePrice = deliveryRequest.Ask;
                        }
                    }

                    if (string.IsNullOrEmpty(executePrice)) return false;
                    eachPhysicalOrderBookData.ExecutePrice = executePrice;
                }
            }
            return true;
        }


        private static TransactionError BookForPhysicalTran(this Account account, Protocal.TransactionBookData tranData, out bool useCurrentSetting)
        {
            useCurrentSetting = false;
            var instrument = account.GetOrCreateInstrument(tranData.InstrumentId);
            if (instrument.IsPhysical)
            {
                bool isDeposit;
                if (account.MayCoexistBuySellOrders(tranData, out isDeposit))
                {
                    if (isDeposit)
                    {
                        tranData.ExecuteTime = DateTime.Now;
                        useCurrentSetting = true;
                    }
                    else
                    {
                        return TransactionError.BuySellCoExistNotAllow;
                    }
                }
            }
            return TransactionError.OK;
        }

        private static bool MayCoexistBuySellOrders(this Account account, Protocal.TransactionBookData tranData, out bool isDeposit)
        {
            Protocal.Physical.PhysicalOrderBookData orderData = (Protocal.Physical.PhysicalOrderBookData)tranData.Orders[0];
            decimal lot = orderData.Lot;
            bool isBuy = orderData.IsBuy;
            bool isOpen = orderData.IsOpen;
            DateTime executeTime = tranData.ExecuteTime;
            isDeposit = orderData.PhysicalTradeSide == PhysicalTradeSide.Deposit;
            if (isOpen) return false;
            decimal oppositeSumLotBeforeExecuteTime = 0, oppositeSumLotAfterExecuteTime = 0;
            foreach (Transaction eachTran in account.Transactions)
            {
                if (eachTran.InstrumentId != tranData.InstrumentId) continue;
                foreach (Order eachOrder in eachTran.Orders)
                {
                    if (eachOrder.Phase == OrderPhase.Executed && eachOrder.LotBalance > 0 && eachOrder.IsBuy != isBuy)
                    {
                        if (eachTran.ExecuteTime < executeTime) oppositeSumLotBeforeExecuteTime += eachOrder.LotBalance;
                        else oppositeSumLotAfterExecuteTime += eachOrder.LotBalance;
                    }
                }
            }
            Logger.InfoFormat("lot = {0}, oppositeSumLotBeforeExecuteTime = {1}, oppositeSumLotAfterExecuteTime  = {2}, executeTime = {3}, instrumentId = {4}, accountId = {5}, isBuy = {6}", lot, oppositeSumLotBeforeExecuteTime, oppositeSumLotAfterExecuteTime, executeTime, tranData.InstrumentId, account.Id, isBuy);
            return lot > oppositeSumLotBeforeExecuteTime && oppositeSumLotAfterExecuteTime > 0;
        }


        private static void VerifyAmentedOrder(this Account account, TransactionBookData tranData)
        {
            Transaction tran = account.GetTran(tranData.Id);
            if (tran == null) return;
            if (tran.CanBeAmendedBy(tranData))
            {
                CancelExecute(tran);
                account.RemoveTransaction(tran);
            }
            else
            {
                throw new TransactionServerException(TransactionError.TransactionAlreadyExists);
            }
        }

        private static void CancelExecute(Transaction tran)
        {
            tran.Phase = TransactionPhase.Canceled;
            foreach (var eachOrder in tran.Orders)
            {
                eachOrder.CancelExecute();
            }
        }


        private static bool CanBeAmendedBy(this Transaction sourceTran, TransactionBookData tranData)
        {
            TransactionSubType tranSubType = tranData.SubType;
            if (tranSubType == TransactionSubType.Amend)
            {
                Order order = sourceTran.FirstOrder;
                return sourceTran.OrderCount == 1
                    && order.IsOpen
                    && tranData.SourceOrderId != null
                    && order.Id == tranData.SourceOrderId
                    && sourceTran.AccountId == tranData.AccountId
                    && sourceTran.InstrumentId == tranData.InstrumentId
                    && sourceTran.Type == tranData.Type
                    && sourceTran.OrderType == tranData.OrderType
                    && sourceTran.ContractSize(null) == tranData.ContractSize
                    && sourceTran.BeginTime == tranData.BeginTime
                    && sourceTran.EndTime == tranData.EndTime
                    && sourceTran.SubmitTime == tranData.SubmitTime
                    && sourceTran.ExecuteTime == tranData.ExecuteTime
                    && sourceTran.SubmitorId == tranData.SubmitorId
                    && sourceTran.ApproverId == tranData.ApproverId;
            }
            else
            {
                return false;
            }
        }

    }
}
