using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Validator
{
    internal static class PlacePriceVerifier
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PlacePriceVerifier));

        internal static void VerifySportOrderPrice(this Order order, Price buy, Price sell, Price comparePrice, DateTime priceTimestamp)
        {
            int sign = (order.Instrument().IsNormal ^ order.IsBuy) ? 1 : -1;
            Price setPrice = order.SetPrice - order.DQMaxMove * sign;
            var dealingPolicyDetail = order.Owner.DealingPolicyPayload();
            int points = (setPrice - comparePrice) * sign;
            string errorDetail = string.Format("comparePrice={0}, setPrice={1}, acceptDQVariation={2}, sign={3}, comparePriceTime={4}, points = {5}",
                comparePrice, setPrice, dealingPolicyDetail.AcceptDQVariation, sign, priceTimestamp, points);
            Logger.Warn(errorDetail);
            if (points > dealingPolicyDetail.AcceptDQVariation)
            {
                order.JudgePrice = comparePrice;
                order.JudgePriceTimestamp = priceTimestamp;
                throw new TransactionServerException(TransactionError.OutOfAcceptDQVariation, errorDetail);
            }
        }


        private static string GetExceedLmtVariationMsg(Price setPrice, Price comparePrice, bool isNetOpen, Settings.DealingPolicyPayload dealingPolicyDetail)
        {
            int pricePoints = Math.Abs(setPrice - comparePrice);
            return string.Format("setPrice = {0}, comparePrice = {1}, ,AcceptLmtVariation = {2}, AcceptCloseLmtVariation= {3}, isNetOpen = {4}, pricePoints = {5}", setPrice, comparePrice, dealingPolicyDetail.AcceptLmtVariation,
                dealingPolicyDetail.AcceptCloseLmtVariation, isNetOpen, pricePoints);
        }



        internal static void VerifyLimitAndOCOOrderPrice(this Order order, Price buy, Price sell, Price comparePrice, DateTime priceTimestamp, AppType appType, OrderNetVerifier netVerifier)
        {
            bool isNetOpen = appType == AppType.RiskMonitor ? false : netVerifier.IsNetOpen(order);//Order placed by RiskMonitor use CloseVariation temporarily
            var dealingPolicyDetail = order.Owner.DealingPolicyPayload();
            if (!order.Owner.DisableAcceptLmtVariation
                && Math.Abs(order.SetPrice - comparePrice) < (isNetOpen ? dealingPolicyDetail.AcceptLmtVariation : dealingPolicyDetail.AcceptCloseLmtVariation))
            {
                order.JudgePrice = comparePrice;
                order.JudgePriceTimestamp = priceTimestamp;
                throw new TransactionServerException(TransactionError.SetPriceTooCloseToMarket, GetExceedLmtVariationMsg(order.SetPrice, comparePrice, isNetOpen, dealingPolicyDetail));
            }

            bool isBuyAsNormal = order.Instrument().IsNormal ? order.IsBuy : !order.IsBuy;
            string message = string.Format("IsBuyAsNormal={0},this.setPrice={1},comparePrice={2},setPrice={3},comparePriceTime={4}", isBuyAsNormal, order.SetPrice, comparePrice, order.SetPrice, priceTimestamp);
            if ((!isBuyAsNormal && order.SetPrice > comparePrice) || (isBuyAsNormal && order.SetPrice <= comparePrice))
            {
                if (order.TradeOption != TradeOption.Better)
                {
                    order.JudgePrice = comparePrice;
                    order.JudgePriceTimestamp = priceTimestamp;

                    throw new TransactionServerException(TransactionError.InvalidPrice, message);
                }
            }
            else
            {
                if (order.TradeOption != TradeOption.Stop)
                {
                    order.JudgePrice = comparePrice;
                    order.JudgePriceTimestamp = priceTimestamp;
                    throw new TransactionServerException(TransactionError.InvalidPrice, message);
                }
            }
        }
    }
}
