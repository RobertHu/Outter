using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.BroadcastBLL;
using Core.TransactionServer.Agent.Caching;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Interact;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using iExchange.Common;
using log4net;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.AccountClass
{
    internal class AccountRisk : BusinessItemBuilder
    {
        private sealed class ConstructParams
        {
            public AlertLevel AlertLevel { get; set; }
            public DateTime? AlertTime { get; set; }
            public AlertLevel AlertLevelAfterCut { get; set; }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountRisk));

        private const int CUT_TRANSACTIONS_CAPACITY = 55;
        private const int CUT_ALL_TRANSACTION_CAPACITY = 2;
        private BusinessItem<AlertLevel> _alertLevel;
        private BusinessItem<DateTime?> _alertTime;
        private BusinessItem<AlertLevel> _alertLevelAfterCut;
        private Account _account;
        private RiskDataCalculator _riskDataCalculator;
        private InstrumentManager _instrumentManager;

        public AccountRisk(Account account, IDBRow dr, InstrumentManager manager)
            : base(account)
        {
            _account = account;
            _instrumentManager = manager;
            _riskDataCalculator = new RiskDataCalculator(account, manager);
            ConstructParams constructParams = new AccountRisk.ConstructParams();
            constructParams.AlertLevel = (AlertLevel)dr["AlertLevel"];
            constructParams.AlertTime = null;
            constructParams.AlertLevelAfterCut = constructParams.AlertLevel;
            this.Parse(constructParams);
        }

        /// <summary>
        /// </summary>
        /// <param name="account"></param>
        public AccountRisk(Account account, InstrumentManager manager)
            : base(account)
        {
            _account = account;
            _instrumentManager = manager;
            _riskDataCalculator = new RiskDataCalculator(account, manager);
            ConstructParams constructParams = new ConstructParams();
            constructParams.AlertLevel = iExchange.Common.AlertLevel.Normal;
            constructParams.AlertLevelAfterCut = constructParams.AlertLevel;
            constructParams.AlertTime = null;
            this.Parse(constructParams);
        }

        private void Parse(ConstructParams constructParams)
        {
            _alertLevel = this.CreateSoundItem("AlertLevel", constructParams.AlertLevel);
            _alertTime = this.CreateSoundItem<DateTime?>("AlertTime", constructParams.AlertTime);
            _alertLevelAfterCut = this.CreateSoundItem("CutAlertLevel", constructParams.AlertLevelAfterCut);
        }

        public AlertLevel AlertLevel
        {
            get { return this._alertLevel.Value; }
            private set { this._alertLevel.SetValue(value); }
        }

        public AlertLevel AlertLevelAfterCut
        {
            get { return this._alertLevelAfterCut.Value; }
            private set { this._alertLevelAfterCut.SetValue(value); }
        }

        public DateTime? AlertTime
        {
            get { return this._alertTime.Value; }
            private set { this._alertTime.SetValue(value); }
        }

        private bool IsFreeOfRiskCheck
        {
            get
            {
                return _account.IsResetFailed || _account.Setting().Type == AccountType.Agent || _account.Setting().Type == AccountType.Transit;
            }
        }

        internal void ResetAlertLevel()
        {
            Logger.InfoFormat("before ResetAlertLevel alertLevel = {0}, ResetAlertLevel = {1}", this.AlertLevel, this.AlertLevelAfterCut);
            this.AlertLevel = this.CalculateAlertLevel(false);
            this.AlertLevelAfterCut = this.CalculateAlertLevel(true);
            if (this.AlertLevel != iExchange.Common.AlertLevel.Normal)
            {
                this.AlertTime = DateTime.Now;
            }
            Logger.InfoFormat("End ResetAlertLevel alertLevel = {0}, ResetAlertLevel = {1}, alertTime = {2}", this.AlertLevel, this.AlertLevelAfterCut, this.AlertTime);
        }


        public void CheckRisk(DateTime baseTime, CalculateType calculateType, IQuotePolicyProvider quotePolicyProvider = null)
        {
            try
            {
                if (this.IsFreeOfRiskCheck) return;
                this.CalculateRiskData(baseTime, calculateType, quotePolicyProvider ?? _account);
                _account.AcceptChanges();
                this.ExecutePendingConfirmLimitOrders(baseTime);
                this.CheckAlertLevelAndCut(baseTime);
                RiskChecker.Default.Add(_account.Id, baseTime);
                _account.SaveAndBroadcastChanges();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void ExecutePendingConfirmLimitOrders(DateTime baseTime)
        {
            var systemParameter = Settings.Setting.Default.SystemParameter;

            if (systemParameter.RiskActionOnPendingConfirmLimit != RiskActionOnPendingConfirmLimit.Normal
                && this.CalculateAlertLevel(true) == AlertLevel.Cut && _account.HasPendingConfirmLimitOrder())
            {
                if (systemParameter.RiskActionOnPendingConfirmLimit == RiskActionOnPendingConfirmLimit.StopCheckRisk)
                {
                    return;
                }
                else if (systemParameter.RiskActionOnPendingConfirmLimit == RiskActionOnPendingConfirmLimit.ExecuteFirst)
                {
                    foreach (Order order in _account.GetPendingConfirmLimitOrders())
                    {
                        if (order.Owner.CanExecute && order.Owner.TradingInstrument.CanTrade(baseTime, PlaceContext.Empty) && order.Owner.AccountInstrument.HasTradePrice(_account))
                        {
                            Price buy = order.IsBuy ? order.SetPrice : null;
                            Price sell = order.IsBuy ? null : order.SetPrice;
                            _account.Execute(order.Owner.Id, (string)buy, (string)sell, null, order.Id, false);
                        }
                    }
                }
            }
        }



        private void CheckAlertLevelAndCut(DateTime baseTime)
        {
            AlertLevel liveAlertLevelAfterLock;
            this.CheckAlertLevel(baseTime, out liveAlertLevelAfterLock);
            if (this.CutTransactions(liveAlertLevelAfterLock, baseTime))
            {
                this.CheckAlertLevel(Market.MarketManager.Now, out liveAlertLevelAfterLock);
            }
        }

        private void CheckAlertLevel(DateTime baseTime, out AlertLevel liveAlertLevelAfterLock)
        {
            AlertLevel liveAlertLevel;
            liveAlertLevelAfterLock = this.CalculateAlertLevel(true);
            liveAlertLevel = liveAlertLevelAfterLock == iExchange.Common.AlertLevel.Cut ? liveAlertLevelAfterLock : this.CalculateAlertLevel(false);
            if (this.AlertLevel < liveAlertLevel)
            {
                this.AlertLevel = liveAlertLevel;
                this.AlertTime = baseTime;

            }
            else if (this.ShouldAutoReduceAlertLevel(liveAlertLevel))
            {
                Logger.InfoFormat("alert level droped, liveAlertLevel = {0}, oldAlertLevel = {1}, accountId = {2}", liveAlertLevel, this.AlertLevel, _account.Id);
                this.AlertLevel = liveAlertLevel;
                this.AlertTime = baseTime;
            }
        }

        private bool ShouldAutoReduceAlertLevel(AlertLevel liveAlertLevel)
        {
            return Settings.Setting.Default.SystemParameter.EnableAutoResetAlertLevel && this.AlertLevel > liveAlertLevel; ;
        }

        private bool CutTransactions(AlertLevel liveAlertLevelAfterLock, DateTime baseTime)
        {
            var settingAccount = _account.Setting();

            if ((settingAccount.IsAutoCut && liveAlertLevelAfterLock == AlertLevel.Cut)
                        && ((settingAccount.RiskActionMode == RiskActionMode.WhenAlertLevelRaising && this.AlertLevelAfterCut < AlertLevel.Cut)
                            || settingAccount.RiskActionMode == RiskActionMode.LiveAlertLevel))
            {
                Logger.InfoFormat("begin CutTransactions, isAutoCut = {0}, liveAlertLevelAfterLock = {1}, RiskActionMode  = {2}, AlertLevelAfterCut  = {3}, accountId = {4}",
                    settingAccount.IsAutoCut, liveAlertLevelAfterLock, settingAccount.RiskActionMode, this.AlertLevelAfterCut, _account.Id);
                this.AlertLevelAfterCut = liveAlertLevelAfterLock;
                bool result = this.Cut(baseTime);
                if (result)
                {
                    _account.RecalculateEstimateFee();
                }
                return result;
            }
            return false;
        }



        private bool Cut(DateTime baseTime)
        {
            List<Transaction> cutTrans = this.CreateCutTransactions(baseTime);
            if (cutTrans.Count == 0) return false;
            this.ExecuteCutTrans(cutTrans);
            return true;
        }


        private void ExecuteCutTrans(List<Transaction> cutTrans)
        {
            foreach (var eachTran in cutTrans)
            {
                if (!_account.ExecuteCutTran(eachTran)) break; ;
            }
        }


        private List<Transaction> CreateCutTransactions(DateTime baseTime)
        {
            var cutTrans = new List<Transaction>(CUT_TRANSACTIONS_CAPACITY);
            foreach (var instrument in _instrumentManager.Instruments)
            {
                if (this.CanCut(instrument, baseTime))
                {
                    var trans = this.InnerCut(instrument);
                    if (trans == null) continue;
                    cutTrans.AddRange(trans);
                }
            }
            return cutTrans;
        }


        private List<Transaction> InnerCut(Instrument instrument)
        {
            if (_account.Setting().RiskLevelAction == RiskLevelAction.CloseAll)
            {
                return this.CutAll(instrument);
            }
            else
            {
                return this.CutNet(instrument);
            }
        }

        private List<Transaction> CutAll(Instrument instrument)
        {
            List<Transaction> result = new List<Transaction>(CUT_ALL_TRANSACTION_CAPACITY);
            Quotation quotation = instrument.GetQuotation();
            if (quotation == null) return result;
            Dictionary<Guid, decimal> closedLotPerOpenOrderDict = new Dictionary<Guid, decimal>(); ;
            if (instrument.TotalSellLotBalance > 0)
            {
                var tran = this.CutCommon(closedLotPerOpenOrderDict, instrument, true, instrument.TotalSellLotBalance, quotation.SellPrice);
                result.Add(tran);
            }

            if (instrument.TotalBuyLotBalance > 0)
            {
                var tran = this.CutCommon(closedLotPerOpenOrderDict, instrument, false, instrument.TotalBuyLotBalance, quotation.BuyPrice);
                result.Add(tran);
            }
            return result;
        }

        private List<Transaction> CutNet(Instrument instrument)
        {
            bool isBuy = (instrument.TotalBuyLotBalance < instrument.TotalSellLotBalance);
            decimal lotBalanceSum = Math.Abs(instrument.TotalBuyLotBalance - instrument.TotalSellLotBalance);
            if (lotBalanceSum == 0) return null;
            var quotation = instrument.GetQuotation();
            var price = isBuy ? quotation.BuyPrice : quotation.SellPrice;
            Dictionary<Guid, decimal> closedLotPerOpenOrderDict = new Dictionary<Guid, decimal>(); ;
            var tran = this.CutCommon(closedLotPerOpenOrderDict, instrument, isBuy, lotBalanceSum, price);
            var result = new List<Transaction>();
            result.Add(tran);
            return result;
        }

        private Transaction CutCommon(Dictionary<Guid, decimal> closedLotPerOpenOrderDict, Instrument instrument, bool cutAsBuy, decimal lotBalanceSum, Price setPrice)
        {
            DateTime baseTime = Market.MarketManager.Now;
            var account = instrument.Owner;
            var factory = TransactionFacade.CreateAddTranCommandFactory(OrderType.Risk, instrument.Setting.Category);
            var command = factory.CreateCutTransaction(account, instrument, lotBalanceSum, setPrice, cutAsBuy);
            command.Execute();
            Transaction tran = command.Result;
            if (tran.InstrumentCategory == InstrumentCategory.Physical
                   || _account.Setting().RiskLevelAction == RiskLevelAction.CloseNetPosition || _account.Setting().RiskLevelAction == RiskLevelAction.CloseAll)
            {
                tran.FirstOrder.SplitOrder(closedLotPerOpenOrderDict, true);
            }
            return tran;
        }


        private bool CanCut(Instrument instrument, DateTime baseTime)
        {
            return instrument.Trading.CanTrade(baseTime, PlaceContext.Empty) && instrument.HasTradePrice(_account);
        }

        internal void CalculateRiskData(DateTime baseTime, CalculateType calculateType, IQuotePolicyProvider quotePolicyProvider)
        {
            try
            {
                _riskDataCalculator.CalculateRiskData(baseTime, calculateType, quotePolicyProvider);
            }
            catch (TradePolicyDetailNotFountException tradePolicyDetailNotFountException)
            {
                Logger.ErrorFormat("instrumentId={0}, tradePolicyId={1}, accountId={2},error={3}", tradePolicyDetailNotFountException.InstrumentId, tradePolicyDetailNotFountException.TradePolicyId, _account.Id, tradePolicyDetailNotFountException);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }




        private AlertLevel CalculateAlertLevel(bool afterLock)
        {
            AlertLevel result = iExchange.Common.AlertLevel.Normal;
            if (this.HasPosition())
            {
                result = this.CalculateAlertLevelHelper(afterLock);
            }
            return result;
        }

        private AlertLevel CalculateAlertLevelHelper(bool afterLock)
        {
            RiskData riskRawData = _account.SumFund.RiskRawData;
            decimal equity = _account.SumFund.Equity + _account.Setting().CreditAmount + riskRawData.RiskCredit;
            if (afterLock)
            {
                equity += riskRawData.LockOrderTradePLFloat;
            }
            AlertLevel alertLevel = AlertLevel.Normal;
            decimal? riskActionMinimumEquity = _account.Setting().RiskActionMinimumEquity;
            if (riskActionMinimumEquity != null)
            {
                if (equity < riskActionMinimumEquity.Value)
                {
                    alertLevel = AlertLevel.Cut;
                }
            }
            else
            {
                var settingAccount = _account.Setting();
                if (equity < (riskRawData.MinEquityAvoidRiskLevel3 * settingAccount.RateRiskNecessary))
                {
                    alertLevel = AlertLevel.Cut;
                }
                else if (equity < (riskRawData.MinEquityAvoidRiskLevel2 * settingAccount.RateRiskNecessary))
                {
                    alertLevel = AlertLevel.Notify;
                }
                else if (equity < (riskRawData.MinEquityAvoidRiskLevel1 * settingAccount.RateRiskNecessary))
                {
                    alertLevel = AlertLevel.Call;
                }
            }

            return alertLevel;
        }


        private bool HasPosition()
        {
            foreach (var eachTran in _account.Transactions)
            {
                foreach (var eachOrder in eachTran.Orders)
                {
                    if (eachOrder.IsRisky && eachOrder.Phase == OrderPhase.Executed && eachOrder.LotBalance > 0) return true;
                }
            }
            return false;
        }

        private void ClearFeeForCutting()
        {
            _account.SumFund.ClearFeeForCutting();
            foreach (var eachFund in _account.Funds)
            {
                eachFund.ClearFeeForCutting();
            }
        }


        private void CalculateFeeForCutting()
        {
            foreach (var eachInstrument in _instrumentManager.Instruments)
            {
                eachInstrument.CalcuateFeeForCutting();
                var fund = _account.GetOrCreateFund(eachInstrument.CurrencyId);
                fund.AddFeeForCutting(eachInstrument.RiskRawData.FeeForCutting);
            }

            foreach (var eachFund in _account.Funds)
            {
                _account.SumFund.AddFeeForCutting(eachFund);
            }
        }


        internal bool IsInAlerting(Instrument instrument, BuySellLot oldLotBalance)
        {
            //0-Unrestricted|1-AlertLevel1|2-AlertLevel2|3-AlertLevel3|4-AlertLevel3 Reduce Position
            if (_account.Setting().ForbiddenAlert < 1
                || _account.Setting().ForbiddenAlert < 4 && (int)this.AlertLevel < _account.Setting().ForbiddenAlert
                || _account.Setting().ForbiddenAlert == 4 && this.AlertLevel < AlertLevel.Cut)
            {
                return false;
            }
            var currentLotBalance = instrument.GetBuySellLotBalance();
            if (_account.Setting().ForbiddenAlert == 4 && this.AlertLevel == AlertLevel.Cut)
            {
                return Math.Abs(oldLotBalance.NetPosition) != Math.Abs(currentLotBalance.NetPosition) || oldLotBalance.IsNetPosWithDiffDirection(currentLotBalance);
            }
            else
            {
                return oldLotBalance.IsAbsNetPosLessThan(currentLotBalance) || oldLotBalance.IsNetPosWithDiffDirection(currentLotBalance);
            }
        }


        private sealed class RiskDataCalculator
        {
            private Dictionary<Guid, FundData> _funds;
            private Account _account;
            private InstrumentManager _instrumentManager;

            internal RiskDataCalculator(Account account, InstrumentManager instrumentManager)
            {
                _account = account;
                _instrumentManager = instrumentManager;
                _funds = new Dictionary<Guid, FundData>();
            }


            internal void CalculateRiskData(DateTime baseTime, CalculateType calculateType, IQuotePolicyProvider quotePolicyProvider)
            {
                this.InitializeSubFunds();
                this.CalculateSubFunds(baseTime, calculateType, quotePolicyProvider);
                this.CalculateSumFund();
                foreach (var eachFund in _funds.Values)
                {
                    FundDataPool.Default.Add(eachFund);
                }
                _funds.Clear();
            }

            private void CalculateSumFund()
            {
                FundData sumFund = FundDataPool.Default.Get(_account, _account.Setting().CurrencyId);
                bool hasPosition = _account.HasPosition();
                foreach (var eachFund in _funds.Values)
                {
                    var fund = _account.GetOrCreateFund(eachFund.CurrencyId);
                    fund.ResetRiskData(eachFund);
                    sumFund.Add(eachFund);
                }
                _account.SumFund.Reset(sumFund);
            }


            private void CalculateSubFunds(DateTime baseTime, CalculateType calculateType, IQuotePolicyProvider quotePolicyProvider)
            {
                if (_instrumentManager.Count > 0)
                {
                    foreach (var instrument in _instrumentManager.Instruments)
                    {
                        if (instrument.GetTransactions().Count == 0) continue;
                        instrument.Calculate(baseTime, calculateType, instrument.GetQuotation(quotePolicyProvider));
                        FundData fund = this.GetOrCreateFund(instrument.CurrencyId);
                        fund.Add(instrument.RiskRawData, null);
                    }

                }
            }

            private void InitializeSubFunds()
            {
                foreach (var eachFund in _account.Funds)
                {
                    FundData fund;
                    if (!_funds.TryGetValue(eachFund.CurrencyId, out fund))
                    {
                        fund = FundDataPool.Default.Get(_account, eachFund.CurrencyId);
                        _funds.Add(eachFund.CurrencyId, fund);
                        fund.InitializeBalanceAndFrozenFund(eachFund.Balance, eachFund.FrozenFund, eachFund.TotalDeposit);
                    }
                }
            }


            private FundData GetOrCreateFund(Guid currencyId)
            {
                FundData fund = null;
                if (_account.IsMultiCurrency)
                {
                    if (!_funds.TryGetValue(currencyId, out fund))
                    {
                        fund = FundDataPool.Default.Get(_account, currencyId);
                    }
                }
                else
                {
                    if (!_funds.TryGetValue(_account.Setting().CurrencyId, out fund))
                    {
                        fund = FundDataPool.Default.Get(_account, _account.Setting().CurrencyId);
                    }
                }
                return fund;
            }

        }
    }

}
