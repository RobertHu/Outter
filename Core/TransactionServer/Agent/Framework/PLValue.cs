using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Framework
{
    public struct PLValue
    {
        public static readonly PLValue Empty = new PLValue(0m, 0m, 0m);
        private decimal _interestPL;
        private decimal _storagePL;
        private decimal _tradePL;

        public PLValue(decimal interestPL, decimal storagePL, decimal tradePL)
        {
            _interestPL = interestPL;
            _storagePL = storagePL;
            _tradePL = tradePL;
        }

        public decimal InterestPL
        {
            get { return _interestPL; }
        }
        public decimal StoragePL
        {
            get { return _storagePL; }

        }
        public decimal TradePL
        {
            get { return _tradePL; }
        }

        public static PLValue operator +(PLValue left, PLValue right)
        {
            return new PLValue(left.InterestPL + right.InterestPL, left.StoragePL + right.StoragePL, left.TradePL + right.TradePL);
        }

        public static PLValue operator -(PLValue left, PLValue right)
        {
            return new PLValue(left.InterestPL - right.InterestPL, left.StoragePL - right.StoragePL, left.TradePL - right.TradePL);
        }

    }

}
