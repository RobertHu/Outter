using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.OrderBLL;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Services;
using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using Protocal;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Market = Core.TransactionServer.Agent.Market;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal sealed class OrderComparer : IComparer<Order>
    {
        public int Compare(Order x, Order y)
        {
            if (x == y) return 0;
            if (x.NextHitTime.CompareTo(y.NextHitTime) == 0)
            {
                return 1;
            }
            return x.NextHitTime.CompareTo(y.NextHitTime);
        }
    }


    internal sealed class Order : Core.TransactionServer.Agent.Order
    {
        internal static readonly IComparer<Order> Comparer = new OrderComparer();
        private BOBetType _bOBetType;
        private DateTime? _nextHitTime;
        private BetResult? _betResult = null;
        private bool _shouldCloseWithHistoryQuotation;
        private BOOrderSettings _boOrderSettings;
        internal Order(Transaction owner, BOOrderConstructParams constructParams, OrderServiceFactoryBase serviceFactory)
            : base(owner, constructParams, serviceFactory)
        {
            if (this.IsOpen)
            {
                this.PayBackPledge = 0m;
                _boOrderSettings = (BOOrderSettings)_orderSettings;
                _bOBetType = BOBetTypeRepository.Get(_boOrderSettings.BetTypeId);
                this.CalculateNextHitTime();
                if (this.HitCount > 0)
                {
                    _betResult = this.CalculateBetResult(this.BestPrice, this.GetBetDirection(this.HitCount - 1));
                }
            }
        }

        internal bool IsClosed
        {
            get { return this.LotBalance == 0m; }
        }

        internal decimal PayBackPledge { get; set; }

        internal decimal PaidPledge
        {
            get { return _boOrderSettings.PaidPledge; }
        }

        internal DateTime? SettleTime
        {
            get { return _boOrderSettings.SettleTime; }
        }

        internal decimal PaidPledgeBalance
        {
            get { return _boOrderSettings.PaidPledgeBalance; }
            set
            {
                Debug.Assert(this.IsOpen);
                _boOrderSettings.PaidPledgeBalance = value;
            }
        }

        internal Guid BetTypeId
        {
            get
            {
                return _boOrderSettings.BetTypeId;
            }
        }

        internal int Frequency
        {
            get
            {
                return _boOrderSettings.Frequency;
            }
        }

        internal long BetOption
        {
            get
            {
                return _boOrderSettings.BetOption;
            }
        }

        internal decimal Odds
        {
            get
            {
                return _boOrderSettings.Odds;
            }
        }

        internal DateTime NextHitTime
        {
            get
            {
                return _nextHitTime ?? DateTime.MinValue;
            }
        }

        public BetResult? BetResult
        {
            get { return _betResult; }
            private set { _betResult = value; }
        }

        public bool ShouldClose
        {
            get
            {
                return this.HitCount == _bOBetType.HitCount || (this.HitCount > 0 && _betResult != BinaryOption.BetResult.Win);
            }
        }

        internal override bool IsRisky
        {
            get
            {
                return false;
            }
        }

        internal override bool IsFreeOfNecessaryCheck
        {
            get
            {
                return true;
            }
        }

        internal void CalculatePledge()
        {
            if (this.IsOpen)
            {
                _boOrderSettings.PaidPledge = _boOrderSettings.PaidPledgeBalance = -this.Lot;
                this.AddBill(new Bill(this.AccountId, this.CurrencyId,this.PaidPledge, BillType.PaidPledge, BillOwnerType.Order));
            }
        }

        internal override bool CanBeClosed()
        {
            return this.IsOpen && this.Phase == OrderPhase.Executed && this.LotBalance > 0;
        }

        internal BetResult CalculateBetResult(Price hitPrice, BetDirection betDirection)
        {
            int priceCompareResult = hitPrice.CompareTo(this.ExecutePrice);
            if (priceCompareResult == 0)
            {
                return _bOBetType.HitCount > 1 ? BinaryOption.BetResult.Lose : BinaryOption.BetResult.Draw;
            }
            else if ((priceCompareResult > 0 && betDirection == BetDirection.Up)
                || (priceCompareResult < 0 && betDirection == BetDirection.Down))
            {
                return BinaryOption.BetResult.Win;
            }
            else
            {
                return BinaryOption.BetResult.Lose;
            }
        }


        internal void CalculateNextHitTime()
        {
            if (this.Owner.ExecuteTime == null) return;
            if (this.ShouldClose)
            {
                _nextHitTime = null;
                Debug.WriteLine("should close");
            }
            else
            {
                _nextHitTime = (this.HitCount > 0 ? this.BestTime : this.Owner.ExecuteTime) + TimeSpan.FromSeconds(this.Frequency);
                Debug.WriteLine(string.Format("bo order execute time = {0}, nextHitTime = {1}", this.Owner.ExecuteTime, _nextHitTime));
            }
            _shouldCloseWithHistoryQuotation = _nextHitTime < Market.MarketManager.Now;
        }

        internal Price GetHitPrice()
        {
            if (_shouldCloseWithHistoryQuotation)
            {
                return this.GetHistoryHitPrice(ExternalSettings.Default.DBConnectionString);
            }
            return this.GetRealTimePrice();
        }

        private Price GetRealTimePrice()
        {
            var quotation = this.Owner.AccountInstrument.GetQuotation();
            return quotation.BuyPrice;
        }

        private Price GetHistoryHitPrice(string connectionString)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                SqlCommand command = connection.CreateCommand();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = "P_GetBOOrderHistoryHitPrice";
                command.Parameters.AddWithValue("@submitorID", this.Owner.SubmitorId);
                command.Parameters.AddWithValue("@instrumentID", this.Instrument().Id);
                command.Parameters.AddWithValue("@timestamp", new SqlDateTime(_nextHitTime.Value).Value);
                SqlParameter parameter = command.Parameters.Add("@hitPrice", System.Data.SqlDbType.VarChar);
                parameter.Size = 10;
                parameter.Direction = System.Data.ParameterDirection.Output;
                connection.Open();
                command.ExecuteNonQuery();
                string hitPrice = (string)command.Parameters["@hitPrice"].Value;
                return Price.CreateInstance(hitPrice, this.Instrument().NumeratorUnit, this.Instrument().Denominator);
            }
        }

        public void Hit(DateTime hitTime)
        {
            BetDirection betDirection = this.GetBetDirection(this.HitCount);
            HitResult result = this.DoBinaryOptionHit(betDirection, hitTime);
        }

        public HitResult DoBinaryOptionHit(BetDirection betDirection, DateTime hitTime)
        {
            BeforeHitParams hitParams = new BeforeHitParams(this.HitCount, this.BestPrice, this.BestTime, this.BetResult);
            this.HitCount++;
            var hitPrice = this.GetHitPrice();
            this.BestPrice = hitPrice;
            this.BestTime = hitTime;
            this.BetResult = this.CalculateBetResult(hitPrice, betDirection);
            return new HitResult(this.BetResult.Value, hitParams);
        }

        public BetDirection GetBetDirection(int indexOfBetOption)
        {
            long buyUpMask = 0X00000001;
            return ((this.BetOption >> indexOfBetOption) & buyUpMask) == buyUpMask ? BetDirection.Up : BetDirection.Down;
        }


        internal void UpdateBestPrice(DateTime baseTime, Price marketBuy, Price marketSell, Instrument instrument)
        {
            Price marketOppositePrice = this.IsBuy ? marketSell : marketBuy;

            if (this.IsBuy == instrument.IsNormal)
            {
                if (this.BestPrice == null)
                {
                    this.BestTime = baseTime;
                    this.BestPrice = marketOppositePrice;
                }
                else if (marketOppositePrice > this.BestPrice)
                {
                    this.BestTime = baseTime;
                    this.BestPrice = marketOppositePrice;
                }
            }
            else
            {
                if (this.BestPrice == null)
                {
                    this.BestTime = baseTime;
                    this.BestPrice = marketOppositePrice;
                }
                else if (marketOppositePrice < this.BestPrice)
                {
                    this.BestTime = baseTime;
                    this.BestPrice = marketOppositePrice;
                }
            }

        }

    }
}
