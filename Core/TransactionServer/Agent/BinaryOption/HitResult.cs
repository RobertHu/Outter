using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption
{
    public enum BetResult
    {
        Draw,
        Win,
        Lose
    }

    public enum BetDirection
    {
        Up,
        Down
    }

    internal struct BeforeHitParams
    {
        private readonly int _hitCount;
        private readonly Price _bestPrice;
        private readonly DateTime? _bestTime;
        private readonly BetResult? _betResult;

        internal BeforeHitParams(int hitCount, Price bestPrice, DateTime? bestTime, BetResult? betResult)
        {
            _hitCount = hitCount;
            _bestPrice = bestPrice;
            _bestTime = bestTime;
            _betResult = betResult;
        }

        internal int HitCount
        {
            get { return _hitCount; }
        }

        internal Price BestPrice
        {
            get { return _bestPrice; }
        }

        internal DateTime? BestTime
        {
            get { return _bestTime; }
        }

        internal BetResult? BetResult
        {
            get { return _betResult; }
        }
    }


    public struct HitResult
    {
        private readonly BetResult _betResult;
        private readonly BeforeHitParams _beforeHitParams;

        internal HitResult(BetResult betResult, BeforeHitParams beforeHitParams)
        {
            _betResult = betResult;
            _beforeHitParams = beforeHitParams;
        }

        public BetResult BetResult
        {
            get
            {
                return _betResult;
            }
        }

        internal BeforeHitParams BeforeHitParams
        {
            get
            {
                return _beforeHitParams;
            }
        }
    }
}
