using Core.TransactionServer.Agent.BinaryOption.Factory;
using Core.TransactionServer.Agent.BLL.TransactionBusiness.Factory;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.CommandFactorys;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL
{
    internal static class TransactionFacade
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionFacade));


        internal static Transaction CreateMultipleCloseTran(InstrumentCategory instrumentCategory, Account account, Guid instrumentId, decimal contractSize, Guid submitorId)
        {
            var addTranCommandFactory = TransactionFacade.CreateAddTranCommandFactory(OrderType.MultipleClose, instrumentCategory);
            var command = addTranCommandFactory.CreateMultipleCloseTran(account, instrumentId, contractSize, submitorId);
            return command.ExecuteAndGetResult();
        }

        internal static Transaction CreateBookTran(InstrumentCategory instrumentCategory, Account account, Protocal.TransactionBookData tranData)
        {
            var factory = CreateAddTranCommandFactory(tranData.OrderType, instrumentCategory);
            var command = factory.CreateBookTran(account, tranData);
            return command.ExecuteAndGetResult();
        }


        internal static Transaction CreateCancelDeliveryWithShortSellTran(Account account, Guid instrumentId, decimal contractSize)
        {
            var addCommand = AddPhysicalTransactionCommandFactory.Default.CreateCancelDeliveryWithShortSellTTran(account, instrumentId, contractSize);
            return addCommand.ExecuteAndGetResult();
        }

        private static Transaction ExecuteAndGetResult(this Periphery.TransactionBLL.Commands.AddTranCommandBase command)
        {
            command.Execute();
            return command.Result;
        }



        internal static AddTransactionCommandFactoryBase CreateAddTranCommandFactory(OrderType orderType, InstrumentCategory instrumentCategory)
        {
            if (orderType == OrderType.BinaryOption)
            {
                return AddBOTransactionCommandFactory.Default;
            }
            else if (instrumentCategory == InstrumentCategory.Physical)
            {
                return AddPhysicalTransactionCommandFactory.Default;
            }
            else
            {
                return AddGeneralTransactionCommandFactory.Default;
            }
        }
    }
}
