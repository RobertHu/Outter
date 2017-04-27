using Core.TransactionServer.Agent.BinaryOption.Factory;
using Core.TransactionServer.Agent.BLL.TransactionBusiness.Factory;
using Core.TransactionServer.Agent.Periphery.TransactionBLL;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    public sealed class TransactionFacade
    {
        static TransactionFacade() { }
        public static readonly TransactionFacade Default = new TransactionFacade();

        private TransactionFacade()
        {
        }

        public AddTransactionCommandFactoryBase GetAddTransactionFactory(OrderType orderType, InstrumentCategory instrumentCategory)
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
