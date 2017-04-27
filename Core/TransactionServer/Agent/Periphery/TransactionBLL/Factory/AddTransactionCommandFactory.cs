using Core.TransactionServer.Agent.AccountClass;
using iExchange.Common;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.CommandFactorys
{
    public abstract class AddTransactionCommandFactoryBase
    {
        internal abstract Commands.AddTranCommandBase Create(Account account, IDBRow dataRowTran, Framework.OperationType operationType);

        internal abstract Commands.AddTranCommandBase Create(Account account, Protocal.TransactionData tranData);

        internal abstract Commands.AddTranCommandBase CreateByAutoClose(Account account, Order openOrder, Price closePrice, OrderType orderType);

        internal abstract Commands.AddTranCommandBase CreateDoneTransaction(Account account, Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice);

        internal abstract Commands.AddTranCommandBase CreateCutTransaction(Account account, Instrument instrument, decimal lotBalanceSum, Price setPrice, bool isBuy);

        internal abstract Commands.AddTranCommandBase CreateBookTranWithNoCalculation(Account account, Protocal.TransactionBookData bookData);

        internal abstract Commands.AddTranCommandBase CreateBookTran(Account account, Protocal.TransactionBookData bookData);

        internal abstract Commands.AddTranCommandBase CreateMultipleCloseTran(Account account, Guid instrumentId, decimal contractSize, Guid submitorId);

        internal abstract Commands.AddTranCommandBase CreateCancelDeliveryWithShortSellTTran(Account account, Guid instrumentId, decimal contractSize);

        internal abstract Commands.AddTranCommandBase CreateLmtQuantiryOnMaxLotChangeTransaction(Account account, Order originOrder, decimal lot);


    }

    public sealed class AddGeneralTransactionCommandFactory : AddTransactionCommandFactoryBase
    {
        internal static readonly AddGeneralTransactionCommandFactory Default = new AddGeneralTransactionCommandFactory();

        static AddGeneralTransactionCommandFactory() { }
        private AddGeneralTransactionCommandFactory() { }


        internal override Commands.AddTranCommandBase Create(Account account, IDBRow dataRowTran, Framework.OperationType operationType)
        {
            return new Commands.AddDataRowTransactionCommand(account, dataRowTran, operationType);
        }

        internal override Commands.AddTranCommandBase Create(Account account, Protocal.TransactionData tranData)
        {
            return new Commands.AddCommunicationTransactionCommand(account, tranData);
        }

        internal override Commands.AddTranCommandBase CreateByAutoClose(Account account, Order openOrder, Price closePrice, OrderType orderType)
        {
            return new Commands.AddAutoCloseTransactionCommand(account, openOrder, closePrice, orderType);
        }

        internal override Commands.AddTranCommandBase CreateDoneTransaction(Account account, Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice)
        {
            return new Commands.AddDoneTransactionCommand(account, ifTran, sourceOrderId, limitPrice, stopPrice);
        }

        internal override Commands.AddTranCommandBase CreateCutTransaction(Account account, Instrument instrument, decimal lotBalanceSum, Price setPrice, bool isBuy)
        {
            return new Commands.AddCutTransactionCommand(account, instrument, lotBalanceSum, setPrice, isBuy);
        }

        internal override Commands.AddTranCommandBase CreateBookTranWithNoCalculation(Account account, Protocal.TransactionBookData bookData)
        {
            return new Commands.AddBookWithNoCalculationTransactionCommand(account, bookData);
        }

        internal override Commands.AddTranCommandBase CreateMultipleCloseTran(Account account, Guid instrumentId, decimal contractSize, Guid submitorId)
        {
            return new Commands.AddMultipleCloseTranCommand(account, instrumentId, contractSize, submitorId);
        }

        internal override Commands.AddTranCommandBase CreateCancelDeliveryWithShortSellTTran(Account account, Guid instrumentId, decimal contractSize)
        {
            throw new NotImplementedException();
        }

        internal override Commands.AddTranCommandBase CreateBookTran(Account account, Protocal.TransactionBookData bookData)
        {
            return new Commands.AddBookTransactionCommand(account, bookData);
        }

        internal override Commands.AddTranCommandBase CreateLmtQuantiryOnMaxLotChangeTransaction(Account account, Order originOrder, decimal lot)
        {
            return new Commands.AddLmtQuantiryOnMaxLotChangeTransactionCommand(account, originOrder, lot);
        }
    }


    public sealed class AddPhysicalTransactionCommandFactory : AddTransactionCommandFactoryBase
    {
        internal static readonly AddPhysicalTransactionCommandFactory Default = new AddPhysicalTransactionCommandFactory();

        static AddPhysicalTransactionCommandFactory() { }
        private AddPhysicalTransactionCommandFactory() { }

        internal override Commands.AddTranCommandBase Create(Account account, IDBRow dataRowTran, Framework.OperationType operationType)
        {
            return new Commands.AddDataRowPhysicalTransactionCommand(account, dataRowTran, operationType);
        }

        internal override Commands.AddTranCommandBase Create(Account account, Protocal.TransactionData tranData)
        {
            return new Commands.AddCommunicationPhysicalTransactionCommand(account, tranData);
        }

        internal override Commands.AddTranCommandBase CreateByAutoClose(Account account, Order openOrder, Price closePrice, OrderType orderType)
        {
            return new Commands.AddAutoClosePhysicalTransactionCommand(account, openOrder, closePrice, orderType);
        }

        internal override Commands.AddTranCommandBase CreateDoneTransaction(Account account, Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice)
        {
            return new Commands.AddDonePhysicalTransactionCommand(account, ifTran, sourceOrderId, limitPrice, stopPrice);
        }

        internal override Commands.AddTranCommandBase CreateCutTransaction(Account account, Instrument instrument, decimal lotBalanceSum, Price setPrice, bool isBuy)
        {
            return new Commands.AddCutPhysicalTransactionCommand(account, instrument, lotBalanceSum, setPrice, isBuy);
        }

        internal Commands.AddTranCommandBase CreateInstalmentTransaction(Account account, Transaction oldTran, Guid sourceOrderId, Physical.PhysicalOrder oldOrder, bool isBuy, bool isOpen, decimal lot)
        {
            return new Commands.AddPhysicalInstalmentTransactionCommand(account, oldTran, sourceOrderId, oldOrder, isBuy, isOpen, lot);
        }

        internal override Commands.AddTranCommandBase CreateBookTranWithNoCalculation(Account account, Protocal.TransactionBookData bookData)
        {
            return new Commands.AddBookWithNoCalculationPhysicalTransactionCommand(account, bookData);
        }

        internal override Commands.AddTranCommandBase CreateMultipleCloseTran(Account account, Guid instrumentId, decimal contractSize, Guid submitorId)
        {
            return new Commands.AddMultipleClosePhysicalTranCommand(account, instrumentId, contractSize, submitorId);
        }

        internal override Commands.AddTranCommandBase CreateCancelDeliveryWithShortSellTTran(Account account, Guid instrumentId, decimal contractSize)
        {
            return new Commands.AddCancelDeliveryWithShortSellTranCommand(account, instrumentId, contractSize);
        }

        internal override Commands.AddTranCommandBase CreateBookTran(Account account, Protocal.TransactionBookData bookData)
        {
            return new Commands.AddBookPhysicalTransactionCommand(account, bookData);
        }

        internal override Commands.AddTranCommandBase CreateLmtQuantiryOnMaxLotChangeTransaction(Account account, Order originOrder, decimal lot)
        {
            return new Commands.AddPhysicalLmtQuantiryOnMaxLotChangeTransactionCommand(account, originOrder, lot);
        }
    }

    public sealed class AddBOTransactionCommandFactory : AddTransactionCommandFactoryBase
    {
        internal static readonly AddBOTransactionCommandFactory Default = new AddBOTransactionCommandFactory();

        static AddBOTransactionCommandFactory() { }
        private AddBOTransactionCommandFactory() { }

        internal override Commands.AddTranCommandBase Create(Account account, IDBRow dataRowTran, Framework.OperationType operationType)
        {
            throw new NotImplementedException();
        }

        internal override Commands.AddTranCommandBase Create(Account account, Protocal.TransactionData tranData)
        {
            return new Commands.AddCommunicationBOTransactionCommand(account, tranData);
        }

        internal override Commands.AddTranCommandBase CreateByAutoClose(Account account, Order openOrder, Price closePrice, OrderType orderType)
        {
            throw new NotImplementedException();
        }

        internal Commands.AddTranCommandBase CreateByClose(Account account, Order openOrder)
        {
            return new Commands.AddCloseBOTransactionCommand(account, (BinaryOption.Order)openOrder);
        }

        internal override Commands.AddTranCommandBase CreateDoneTransaction(Account account, Transaction ifTran, Guid sourceOrderId, Price limitPrice, Price stopPrice)
        {
            throw new NotImplementedException();
        }

        internal override Commands.AddTranCommandBase CreateCutTransaction(Account account, Instrument instrument, decimal lotBalanceSum, Price setPrice, bool isBuy)
        {
            throw new NotImplementedException();
        }

        internal override Commands.AddTranCommandBase CreateBookTranWithNoCalculation(Account account, Protocal.TransactionBookData bookData)
        {
            throw new NotImplementedException();
        }

        internal override Commands.AddTranCommandBase CreateMultipleCloseTran(Account account, Guid instrumentId, decimal contractSize, Guid submitorId)
        {
            throw new NotImplementedException();
        }

        internal override Commands.AddTranCommandBase CreateCancelDeliveryWithShortSellTTran(Account account, Guid instrumentId, decimal contractSize)
        {
            throw new NotImplementedException();
        }

        internal override Commands.AddTranCommandBase CreateBookTran(Account account, Protocal.TransactionBookData bookData)
        {
            throw new NotImplementedException();
        }

        internal override Commands.AddTranCommandBase CreateLmtQuantiryOnMaxLotChangeTransaction(Account account, Order originOrder, decimal lot)
        {
            throw new NotImplementedException();
        }
    }


}
