using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Engine.iExchange;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    internal sealed class IfDoneService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(IfDoneService));
        private Transaction _tran;
        private TransactionSettings _settings;
        private Instrument _settingInstrument;

        internal IfDoneService(Transaction tran, TransactionSettings settings)
        {
            _tran = tran;
            _settings = settings;
            _settingInstrument = tran.SettingInstrument();
        }


        internal bool DoneCondition
        {
            get
            {
                return _tran.SubType == TransactionSubType.IfDone || _tran.SubType == TransactionSubType.Amend;
            }
        }

        /// <summary>
        /// 用于OCO单成交时，获取 the other order所对应的 done transactions
        /// </summary>
        /// <param name="executeOrderId"></param>
        /// <returns></returns>
        internal List<Transaction> GetDoneTransactionsForOCO(Guid executeOrderId)
        {
            List<Transaction> result = new List<Transaction>();
            foreach (Order ifOrder in _tran.Orders)
            {
                if (executeOrderId == ifOrder.Id) continue;
                var doneTran = this.GetDoneTransaction(ifOrder.Id);
                if (doneTran == null) continue;
                if (doneTran.Phase == TransactionPhase.Placing)
                {
                    result.Add(doneTran);
                }
                else
                {
                    Logger.Info(string.Format("DoneTran has been canceled or amended \r\nIf={0}\r\nDone={1} ", _tran, doneTran));
                }
            }
            return result;
        }

        internal List<Transaction> GetDoneTransactions()
        {
            List<Transaction> result = new List<Transaction>();
            foreach (var eachOrder in _tran.Orders)
            {
                Transaction tran = this.GetDoneTransaction(eachOrder.Id);
                if (tran != null)
                {
                    result.Add(tran);
                }
            }
            return result;
        }

        internal Transaction GetDoneTransaction(Guid ifOrderId)
        {
            if (!this.DoneCondition) return null;
            Transaction result = null;
            foreach (Transaction tran in _tran.Owner.Transactions)
            {
                if (this.IsDoneTran(tran, ifOrderId))
                {
                    result = tran;
                    break;
                }
            }
            return result;
        }

        private bool IsDoneTran(Transaction tran, Guid ifOrderId)
        {
            return _tran != tran
                 && tran.Phase != TransactionPhase.Canceled
                 && tran.SubType == TransactionSubType.IfDone
                 && tran.SourceOrderId == ifOrderId;
        }


        internal void RemoveDoneTrans()
        {
            var doneTrans = _tran.GetDoneTransactions();
            if (doneTrans == null) return;
            var account = _tran.Owner;
            foreach (var eachTran in doneTrans)
            {
                account.RemoveTransaction(eachTran);
            }
        }

        internal bool IsValidDoneOrderPrice(Price basePrice)
        {
            foreach (Order eachOrder in _tran.Orders)
            {
                if (!this.IsPriceValidFor(eachOrder, basePrice)) return false;
            }
            return true;
        }

        private bool IsPriceValidFor(Order order, Price basePrice)
        {
            bool isBuy = order.IsBuy;
            Price donePrice = order.SetPrice;
            if (order.TradeOption == TradeOption.Better)
            {
                return this.IsValidForBetterOption(order, basePrice);
            }
            else if (order.TradeOption == TradeOption.Stop)
            {
                return this.IsValidForStopOption(order, basePrice);
            }
            else
            {
                return true;
            }
        }

        private bool IsValidForStopOption(Order order, Price basePrice)
        {
            var comparePrice = this.CalculateComparePriceForStop(order, basePrice);
            double comparePriceValue = (double)comparePrice;
            var donePrice = order.SetPrice;
            if (((_settingInstrument.IsNormal != order.IsBuy) ? donePrice > comparePrice : donePrice < comparePrice)
                || Math.Abs((double)donePrice - comparePriceValue) > comparePriceValue * 0.2)
            {
                return false;
            }
            return true;
        }

        private Price CalculateComparePriceForStop(Order order, Price basePrice)
        {
            var account = _tran.Owner;
            var quotePolicyDetail = Settings.Setting.Default.GetQuotePolicyDetail(account, _settingInstrument.Id);
            int spread = quotePolicyDetail.SpreadPoints + _settingInstrument.NumeratorUnit;
            spread = Math.Max(spread, _settingInstrument.AcceptIfDoneVariation);
            return order.IsBuy ? (basePrice + spread) : (basePrice - spread);
        }


        private bool IsValidForBetterOption(Order order, Price basePrice)
        {
            var comparePrice = this.CalculateComparePriceForBetter(order, basePrice);
            double comparePriceValue = (double)comparePrice;
            var donePrice = order.SetPrice;
            if (((_settingInstrument.IsNormal != order.IsBuy) ? donePrice < comparePrice : donePrice > comparePrice)
                || Math.Abs((double)donePrice - comparePriceValue) > comparePriceValue * 0.2)
            {
                return false;
            }
            return true;
        }

        private Price CalculateComparePriceForBetter(Order order, Price basePrice)
        {
            return _settingInstrument.IsNormal != order.IsBuy ?
                (basePrice + _settingInstrument.AcceptIfDoneVariation) :
                (basePrice - _settingInstrument.AcceptIfDoneVariation);
        }

    }
}
