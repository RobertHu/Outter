using Core.TransactionServer.Agent.AccountClass.AccountUtil;
using Core.TransactionServer.Agent.Interact;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Service;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Agent.Util.TypeExtension;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.BLL.PreCheck;
using Protocal;
using Core.TransactionServer.Agent.BinaryOption.Factory;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness.Factory;
using Core.TransactionServer.Agent.BLL.AccountBusiness.TypeExtensions;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal sealed class TransactionPlacer
    {
        public static readonly TransactionPlacer Default = new TransactionPlacer();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionPlacer));

        static TransactionPlacer() { }
        private TransactionPlacer() { }

        internal void Place(Account owner, Protocal.TransactionData tranData, out string tranCode)
        {
            if (this.IsPriceOutOfDate(owner, tranData))
            {
                throw new TransactionServerException(TransactionError.PriceIsOutOfDate);
            }
            var tran = this.CreateTran(owner, tranData, out tranCode);
            Logger.InfoFormat("place tranid = {0}, subType = {1}", tran.Id, tran.SubType);
            this.PreCheck(owner, tran);
            this.Verify(owner, tran, tranData.PlaceByRiskMonitor, tranData.AppType, PlaceContext.Empty);
            if (!tran.CanAutoAcceptPlace())
            {
                tran.ChangePhaseToPlacing();
                TransactionExpireChecker.Default.Add(tran);
                tran.Owner.InvalidateInstrumentCacheAndBroadcastChanges(tran);
                return;
            }
            if (tran.ShouldAutoFill() && tran.IsPriceInRangeOfAutoFill())
            {
                foreach (var eachOrder in tran.Orders)
                {
                    eachOrder.IsAutoFill = true;
                }
            }
            owner.InvalidateInstrumentCacheAndBroadcastChanges(tran);
            Logger.InfoFormat("begin place to engine accountId = {0}, tranId = {1}", owner.Id, tran.Id);
            this.PlaceToEngine(owner, tran);
        }

        private void PlaceToEngine(Account account, Transaction tran)
        {
            tran.PlacePhase = PlacePhase.PlaceToEngine;
            InteractFacade.Default.TradingEngine.Place(tran);
        }

        private Transaction CreateTran(Account account, Protocal.TransactionData tranData, out string tranCode)
        {
            var tran = account.CreateTransaction(tranData);
            tranCode = tran.Code;
            tran.ChangePhaseToPlaced();
            tran.PlacePhase = PlacePhase.Received;
            account.InvalidateInstrumentCache(tran);
            return tran;
        }

        private void PreCheck(Account account, Transaction tran)
        {
            Debug.Assert(account == tran.Owner);
            if (this.ShouldPreCheck(tran.FreePlacingPreCheck, tran.OrderType) && !account.HasEnoughMoneyToPlace(tran))
            {
                tran.PlacePhase = PlacePhase.PreCheckFailed;
                throw new TransactionServerException(TransactionError.MarginIsNotEnough, "Place precheck is not passed");
            }
            tran.PlacePhase = PlacePhase.PreCheckSuccess;
        }

        private bool ShouldPreCheck(bool freePlacingPreCheck, OrderType orderType)
        {
            var systemParameter = Settings.Setting.Default.SystemParameter;
            return !freePlacingPreCheck && (systemParameter.NeedsPlaceCheck() || systemParameter.NeedsOldPlaceCheck(orderType));
        }


        internal void Verify(Account account, Transaction tran, bool placeByRiskMonitor, AppType appType, PlaceContext context)
        {
            TransactionVerifier.VerifyForPlacing(tran, placeByRiskMonitor, appType, context);
            if (!placeByRiskMonitor && MaxOpenLotVerifier.IsExceedMaxOpenLot(tran, context))
            {
                throw new TransactionServerException(TransactionError.ExceedMaxOpenLot);
            }
            tran.PlacePhase = PlacePhase.VerifySuccess;
        }


        private bool IsPriceOutOfDate(Account owner, Protocal.TransactionData tranData)
        {
            var systemParameter = Settings.Setting.Default.SystemParameter;
            if (systemParameter.MaxPriceDelayForSpotOrder == null) return false;
            if (tranData.OrderType == OrderType.SpotTrade)
            {
                foreach (var eachOrderNode in tranData.Orders)
                {
                    if (this.IsOrderPriceOutOfDate(owner, tranData, eachOrderNode, systemParameter)) return true;
                }
            }
            return false;
        }

        private bool IsOrderPriceOutOfDate(Account owner, Protocal.TransactionData tranData, Protocal.OrderData orderNode, SystemParameter systemParameter)
        {
            if (orderNode.PriceIsQuote != null && !orderNode.PriceIsQuote.Value)
            {
                DateTime timestamp = orderNode.PriceTimestamp.Value;
                var instrument = owner.GetOrCreateInstrument(tranData.InstrumentId);
                var submitor = Settings.Setting.Default.GetCustomer(tranData.SubmitorId);
                Quotation quotation = instrument.GetQuotation(submitor);
                if (quotation != null)
                {
                    TimeSpan diff = (quotation.Timestamp - timestamp);
                    if (diff >= systemParameter.MaxPriceDelayForSpotOrder)
                    {
                        string message = string.Format("Pirce is out of date, instrument id = {0}, last price timestamp = {1}, setprice timestamp = {2}, diff = {3}{4}{5}", tranData.InstrumentId, quotation.Timestamp, timestamp, diff, Environment.NewLine, tranData.ToString());
                        Logger.Warn(message);
                        return true;
                    }
                }
            }
            return false;
        }
    }

}
