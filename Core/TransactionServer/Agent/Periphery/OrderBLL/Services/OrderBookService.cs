using Core.TransactionServer.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Services
{
    //public abstract class OrderBookServiceBase
    //{
    //    protected Order _order;

    //    protected OrderBookServiceBase(Order order)
    //    {
    //        _order = order;
    //    }


    //    internal void Book(ExecuteContext context)
    //    {
    //        if (_order.Phase != iExchange.Common.OrderPhase.Executed) return;
    //        if (!_order.IsOpen)
    //        {
    //            this.CalculateForCloseOrder(context);
    //        }
    //        else
    //        {
    //            this.CalculateForOpenOrder(context);
    //        }
    //        decimal deltaBalance = _order.SumBillsForBalance();
    //    }

    //    protected abstract void CalculateForOpenOrder(ExecuteContext context);

    //    private void CalculateForCloseOrder(ExecuteContext context)
    //    {
    //        _order.UpdateOpenOrder(context);
    //        _order.CalculateFee(context);
    //        _order.CalculateValuedPL(context);
    //    }


    //}

    //public sealed class OrderBookService : OrderBookServiceBase
    //{
    //    internal OrderBookService(Order order)
    //        : base(order)
    //    {
    //    }

    //    protected override void CalculateForOpenOrder(ExecuteContext context)
    //    {
    //    }

    //}

    //public sealed class PhysicalOrderBookService : OrderBookServiceBase
    //{
    //    internal PhysicalOrderBookService(Physical.PhysicalOrder order)
    //        : base(order)
    //    {
    //    }

    //    protected override void CalculateForOpenOrder(ExecuteContext context)
    //    {
    //        var physicalOrder = (Physical.PhysicalOrder)_order;
    //        physicalOrder.CalculateOriginValue(context);
    //        physicalOrder.CalculatePaidAmount();
    //    }

    //}



}
