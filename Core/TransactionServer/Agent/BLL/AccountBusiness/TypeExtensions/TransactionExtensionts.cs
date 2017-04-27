using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.CommandFactorys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness.TypeExtensions
{
    internal static class TransactionExtensionts
    {
        internal static Transaction CreateTransaction(this Account account, Protocal.TransactionData tranData)
        {
            var factory = tranData.GetAddTranCommandFactory();
            var command = factory.Create(account, tranData);
            command.Execute();
            return command.Result;
        }


        internal static Transaction CreateTransaction(this Account account, Order originOrder, decimal lot)
        {
            var factory = TransactionFacade.CreateAddTranCommandFactory(originOrder.Owner.OrderType, originOrder.Owner.InstrumentCategory);
            var command = factory.CreateLmtQuantiryOnMaxLotChangeTransaction(account, originOrder, lot);
            command.Execute();
            return command.Result;
        }



        internal static Transaction CreateTransaction(this Account account, Protocal.TransactionBookData tranData)
        {
            var factory = tranData.GetAddTranCommandFactory();
            var command = factory.CreateBookTranWithNoCalculation(account, tranData);
            command.Execute();
            return command.Result;
        }

        private static AddTransactionCommandFactoryBase GetAddTranCommandFactory(this Protocal.TransactionCommonData tranData)
        {
            var instrument = Settings.Setting.Default.GetInstrument(tranData.InstrumentId);
            return TransactionFacade.CreateAddTranCommandFactory(tranData.OrderType, instrument.Category);
        }


    }
}
