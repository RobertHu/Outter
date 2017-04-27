using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using Core.TransactionServer.Agent.Physical;
using iExchange.Common;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Factory
{
    internal abstract class AddOrderCommandFactoryBase
    {
        internal abstract AddOrderCommandBase CreateByCommunication(Transaction tran, Protocal.OrderData orderData);
        internal abstract AddOrderCommandBase CreateByDataRow(Transaction tran, IDBRow dr);
        internal abstract AddOrderCommandBase CreateByAutoClose(Transaction tran, Order openOrder, Price closePrice, TradeOption tradeOption);
        internal abstract AddOrderCommandBase CreateDoneOrder(Transaction tran, Order openOrder, Price closePrice, TradeOption tradeOption);
        internal abstract AddOrderCommandBase CreateCutOrder(Transaction tran, bool isBuy, decimal lotBalance, Price setPrice);
        internal abstract AddOrderCommandBase CreateBookOrderWithNoCalculation(Transaction tran, Protocal.OrderBookData orderData, DateTime tradeDay);
        internal abstract AddOrderCommandBase CreateBookOrder(Transaction tran, Protocal.OrderBookData orderData, DateTime tradeDay);
        internal abstract AddOrderCommandBase CreateMultipleCloseOrder(Transaction tran, decimal closedLot, Price executePrice, bool isBuy, List<OrderRelationRecord> orderRelations);
        internal abstract AddOrderCommandBase CreateCancelDeliveryWithShortSellOrder(Transaction tran, Core.TransactionServer.Agent.Periphery.Facades.CancelDeliveryWithShortSellOrderParam param);
        internal abstract AddOrderCommandBase CreateAddLmtQuantiryOnMaxLotChangeOrderCommand(Transaction tran, Order originOrder, decimal lot);
    }


    internal sealed class AddGeneralOrderCommandFactory : AddOrderCommandFactoryBase
    {
        internal static readonly AddGeneralOrderCommandFactory Default = new AddGeneralOrderCommandFactory();

        static AddGeneralOrderCommandFactory() { }
        private AddGeneralOrderCommandFactory() { }


        internal override AddOrderCommandBase CreateByCommunication(Transaction tran, Protocal.OrderData orderData)
        {
            return new AddCommunicationOrderCommand(tran, orderData);
        }

        internal override AddOrderCommandBase CreateByDataRow(Transaction tran, IDBRow dr)
        {
            return new AddDataRowOrderCommand(tran, dr);
        }

        internal override AddOrderCommandBase CreateByAutoClose(Transaction tran, Order openOrder, Price closePrice, TradeOption tradeOption)
        {
            return new AddAutoCloseOrderCommand(tran, openOrder, closePrice, tradeOption);
        }

        internal override AddOrderCommandBase CreateDoneOrder(Transaction tran, Order openOrder, Price closePrice, TradeOption tradeOption)
        {
            return new AddDoneOrderCommand(tran, openOrder, closePrice, tradeOption);
        }

        internal override AddOrderCommandBase CreateCutOrder(Transaction tran, bool isBuy, decimal lotBalance, Price setPrice)
        {
            return new AddCutOrderCommand(tran, isBuy, lotBalance, setPrice);
        }

        internal override AddOrderCommandBase CreateBookOrderWithNoCalculation(Transaction tran, Protocal.OrderBookData orderData, DateTime tradeDay)
        {
            return new AddBookWithNoCalculationOrderCommand(tran, orderData, tradeDay);
        }

        internal override AddOrderCommandBase CreateMultipleCloseOrder(Transaction tran, decimal closedLot, Price executePrice, bool isBuy, List<OrderRelationRecord> orderRelations)
        {
            return new AddMultipleCloseOrderCommand(tran, closedLot, executePrice, isBuy, orderRelations);
        }

        internal override AddOrderCommandBase CreateCancelDeliveryWithShortSellOrder(Transaction tran, Core.TransactionServer.Agent.Periphery.Facades.CancelDeliveryWithShortSellOrderParam param)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateBookOrder(Transaction tran, Protocal.OrderBookData orderData, DateTime tradeDay)
        {
            return new Commands.AddBookOrderCommand(tran, orderData, tradeDay);
        }

        internal override AddOrderCommandBase CreateAddLmtQuantiryOnMaxLotChangeOrderCommand(Transaction tran, Order originOrder, decimal lot)
        {
            return new Commands.AddLmtQuantiryOnMaxLotChangeOrderCommand(tran, originOrder, lot);
        }
    }

    internal sealed class AddPhysicalOrderCommandFactory : AddOrderCommandFactoryBase
    {
        internal static readonly AddPhysicalOrderCommandFactory Default = new AddPhysicalOrderCommandFactory();

        static AddPhysicalOrderCommandFactory() { }
        private AddPhysicalOrderCommandFactory() { }


        internal AddOrderCommandBase CreateInstalmentOrder(PhysicalTransaction tran, PhysicalOrder oldOrder, decimal lot, bool isOpen, bool isBuy)
        {
            return new AddInstalmentOrderOrderCommand(tran, oldOrder, lot, isOpen, isBuy);
        }

        internal override AddOrderCommandBase CreateByCommunication(Transaction tran, Protocal.OrderData orderData)
        {
            return new AddCommunicationPhysicalOrderCommand(tran, (Protocal.Physical.PhysicalOrderData)orderData);
        }

        internal override AddOrderCommandBase CreateByDataRow(Transaction tran, IDBRow dr)
        {
            return new AddDataRowPhysicalOrderCommand(tran, dr);
        }

        internal override AddOrderCommandBase CreateByAutoClose(Transaction tran, Order openOrder, Price closePrice, TradeOption tradeOption)
        {
            return new AddAutoClosePhysicalOrderCommand(tran, (Physical.PhysicalOrder)openOrder, closePrice, tradeOption);
        }

        internal override AddOrderCommandBase CreateDoneOrder(Transaction tran, Order openOrder, Price closePrice, TradeOption tradeOption)
        {
            return new AddPhysicalDoneOrderCommand(tran, (Physical.PhysicalOrder)openOrder, closePrice, tradeOption);
        }

        internal override AddOrderCommandBase CreateCutOrder(Transaction tran, bool isBuy, decimal lotBalance, Price setPrice)
        {
            return new AddPhysicalCutOrderCommand(tran, isBuy, lotBalance, setPrice);
        }

        internal override AddOrderCommandBase CreateBookOrderWithNoCalculation(Transaction tran, Protocal.OrderBookData orderData, DateTime tradeDay)
        {
            return new AddBookWithNoCalculationPhysicalOrderCommand(tran, (Protocal.Physical.PhysicalOrderBookData)orderData, tradeDay);
        }

        internal override AddOrderCommandBase CreateMultipleCloseOrder(Transaction tran, decimal closedLot, Price executePrice, bool isBuy, List<OrderRelationRecord> orderRelations)
        {
            return new AddMultipleClosePhysicalOrderCommand(tran, closedLot, executePrice, isBuy, orderRelations);
        }

        internal override AddOrderCommandBase CreateCancelDeliveryWithShortSellOrder(Transaction tran, Core.TransactionServer.Agent.Periphery.Facades.CancelDeliveryWithShortSellOrderParam param)
        {
            return new Commands.AddCancelDeliveryWithShortSellOrderCommand(tran, param);
        }

        internal override AddOrderCommandBase CreateBookOrder(Transaction tran, Protocal.OrderBookData orderData, DateTime tradeDay)
        {
            return new Commands.AddBookPhysicalOrderCommand(tran, (Protocal.Physical.PhysicalOrderBookData)orderData, tradeDay);
        }

        internal override AddOrderCommandBase CreateAddLmtQuantiryOnMaxLotChangeOrderCommand(Transaction tran, Order originOrder, decimal lot)
        {
            return new Commands.AddPhysicalLmtQuantiryOnMaxLotChangeOrderCommand(tran, originOrder, lot);
        }
    }


    internal sealed class AddBOOrderCommandFactory : AddOrderCommandFactoryBase
    {
        internal static readonly AddBOOrderCommandFactory Default = new AddBOOrderCommandFactory();

        static AddBOOrderCommandFactory() { }
        private AddBOOrderCommandFactory() { }

        internal AddOrderCommandBase CreateByClose(Transaction tran, BinaryOption.Order openOrder)
        {
            return new AddBOCloseOrdeCommand(tran, openOrder);
        }

        internal override AddOrderCommandBase CreateByCommunication(Transaction tran, Protocal.OrderData orderData)
        {
            return new AddCommunicationBOOrderCommand(tran, (Protocal.BOOrderData)orderData);
        }

        internal override AddOrderCommandBase CreateByDataRow(Transaction tran, IDBRow  dr)
        {
            return new AddDataRowBOOrderCommand(tran, dr);
        }

        internal override AddOrderCommandBase CreateByAutoClose(Transaction tran, Order openOrder, Price closePrice, TradeOption tradeOption)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateDoneOrder(Transaction tran, Order openOrder, Price closePrice, TradeOption tradeOption)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateCutOrder(Transaction tran, bool isBuy, decimal lotBalance, Price setPrice)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateBookOrderWithNoCalculation(Transaction tran, Protocal.OrderBookData orderData, DateTime tradeDay)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateMultipleCloseOrder(Transaction tran, decimal closedLot, Price executePrice, bool isBuy, List<OrderRelationRecord> orderRelations)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateCancelDeliveryWithShortSellOrder(Transaction tran, Core.TransactionServer.Agent.Periphery.Facades.CancelDeliveryWithShortSellOrderParam param)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateBookOrder(Transaction tran, Protocal.OrderBookData orderData, DateTime tradeDay)
        {
            throw new NotImplementedException();
        }

        internal override AddOrderCommandBase CreateAddLmtQuantiryOnMaxLotChangeOrderCommand(Transaction tran, Order originOrder, decimal lot)
        {
            throw new NotImplementedException();
        }
    }



}
