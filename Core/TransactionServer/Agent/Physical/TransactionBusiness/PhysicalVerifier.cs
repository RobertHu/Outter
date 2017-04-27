﻿using Core.TransactionServer.Agent.BLL.TransactionBusiness.PreCheck;
using Core.TransactionServer.Agent.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.TransactionBusiness
{
    internal sealed class PhysicalVerifier : Verifier
    {
        internal PhysicalVerifier(PhysicalTransaction tran)
            : base(tran) { }

        protected override BuySellLot CalculateSameDirectionLotsOfPendingOrders()
        {
            var result = base.CalculateSameDirectionLotsOfPendingOrders();
            var buyDirectionLotBalance = this.CalculateBuyDirectionLotBalance();
            result += new BuySellLot(buyDirectionLotBalance, 0m);
            return result;
        }

        private decimal CalculateBuyDirectionLotBalance()
        {
            decimal result = 0m;
            foreach (var tran in _instrument.GetTransactions())
            {
                if (object.ReferenceEquals(tran, _tran)) continue;
                foreach (var eachOrder in tran.Orders)
                {
                    if (eachOrder.IsBuy && eachOrder.IsExecuted && eachOrder.LotBalance > 0)
                    {
                        result += eachOrder.LotBalance;
                    }
                }
            }
            return result;
        }

        protected override bool ShouldCalculateTranLotBalanceAsRisk()
        {
            return true;
        }


    }
}
