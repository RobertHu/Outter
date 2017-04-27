using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using Core.TransactionServer.Agent.Physical;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.OrderBusiness.Commands
{
    internal class AddOrderParameterBase
    {
        protected AddOrderParameterBase(AddOrderCommandVisitorBase visitor)
        {
            this.Visitor = visitor;
        }
        internal AddOrderCommandVisitorBase Visitor { get; private set; }
    }

    internal sealed class AddResetOrderParamter : AddOrderParameterBase
    {
        internal AddResetOrderParamter(DataRow dataRow)
            : base(AddResetOrderCommandVisitor.Default)
        {
            this.DataRow = dataRow;
        }

        internal DataRow DataRow { get; private set; }
    }


    internal sealed class AddDataRowOrderParameter : AddOrderParameterBase
    {
        internal AddDataRowOrderParameter(DataRow dataRow)
            : base(AddDataRowFormatOrderCommandVisitor.Default)
        {
            this.DataRow = dataRow;
        }
        internal DataRow DataRow { get; private set; }
    }


    internal sealed class AddCommunicationOrderParameter : AddOrderParameterBase
    {
        internal AddCommunicationOrderParameter(Protocal.OrderData orderData, DateTime? tradeDay)
            : base(AddCommunicationOrderCommandVisitor.Default)
        {
            this.OrderData = orderData;
            this.TradeDay = tradeDay;
        }

        internal Protocal.OrderData OrderData { get; private set; }
        internal DateTime? TradeDay { get; private set; }
    }


    internal sealed class AddAutoCloseOrderParameter : AddOrderParameterBase
    {
        internal AddAutoCloseOrderParameter(Order openOrder, Price closePrice, TradeOption tradeOption)
            : base(AddAutoCloseOrderCommandVisitor.Default)
        {
            this.CloseInfo = new CloseInfo(openOrder, closePrice, tradeOption);
        }

        internal CloseInfo CloseInfo { get; private set; }
    }


    internal sealed class AddCloseOrderParameter : AddOrderParameterBase
    {
        internal AddCloseOrderParameter(Order openOrder)
            : base(AddCloseOrderCommandVisitor.Default)
        {
            this.OpenOrder = openOrder;
        }

        internal Order OpenOrder { get; private set; }
    }

    internal sealed class AddDoneOrderParameter : AddOrderParameterBase
    {
        internal AddDoneOrderParameter(Order openOrder, Price closePrice, TradeOption tradeOption)
            : base(AddDoneOrderCommandVisitor.Default)
        {
            this.CloseInfo = new CloseInfo(openOrder, closePrice, tradeOption);
        }

        internal CloseInfo CloseInfo { get; private set; }

    }


    internal sealed class CloseInfo
    {
        internal CloseInfo(Order openOrder, Price closePrice, TradeOption tradeOption)
        {
            this.OpenOrder = openOrder;
            this.ClosePrice = closePrice;
            this.TradeOption = tradeOption;
        }

        internal Order OpenOrder { get; private set; }

        internal Price ClosePrice { get; private set; }

        internal TradeOption TradeOption { get; private set; }
    }

    internal sealed class AddCutOrderParameter : AddOrderParameterBase
    {
        internal AddCutOrderParameter(bool isBuy, decimal lotBalance, Price setPrice)
            : base(AddCutOrderCommandVisitor.Default)
        {
            this.IsBuy = isBuy;
            this.LotBalance = lotBalance;
            this.SetPrice = setPrice;
        }

        internal bool IsBuy { get; private set; }
        internal decimal LotBalance { get; private set; }
        internal Price SetPrice { get; private set; }
    }

    internal sealed class AddInstalmentOrderParameter : AddOrderParameterBase
    {
        internal AddInstalmentOrderParameter(PhysicalOrder oldOrder, decimal lot, bool isOpen, bool isBuy)
            : base(AddPhysicalInstalmentOrderCommandVisitor.Default)
        {
            this.OldOrder = oldOrder;
            this.Lot = lot;
            this.IsOpen = isOpen;
            this.IsBuy = isBuy;
        }

        internal PhysicalOrder OldOrder { get; private set; }
        internal decimal Lot { get; private set; }
        internal bool IsOpen { get; private set; }
        internal bool IsBuy { get; private set; }
    }


}
