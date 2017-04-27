using Core.TransactionServer.Agent.BinaryOption.Factory;
using Core.TransactionServer.Agent.BLL.OrderBusiness.Factory;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Factory;
using Core.TransactionServer.Agent.Periphery.TransactionBLL.Factory;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Commands
{
    internal interface IAddTranCommandService
    {
        TransactionConstructParams CreateConstructParams();
        Transaction CreateTransaction(Account account, TransactionConstructParams constructParams);
        void Accept(Visitors.AddTransactionCommandVisitorBase visitor, AddTranCommandBase tranCommand);
        AddOrderCommandFactoryBase AddOrderCommandFactory { get; }
    }


    internal sealed class AddTransactionCommandService : IAddTranCommandService
    {
        internal static readonly AddTransactionCommandService Default = new AddTransactionCommandService();

        static AddTransactionCommandService() { }
        private AddTransactionCommandService() { }

        public TransactionConstructParams CreateConstructParams()
        {
            return new TransactionConstructParams();
        }

        public Transaction CreateTransaction(Account account, TransactionConstructParams constructParams)
        {
            return TransactionServiceFactory.Default.CreateTransaction(account, constructParams);
        }

        public void Accept(Visitors.AddTransactionCommandVisitorBase visitor, AddTranCommandBase tranCommand)
        {
            visitor.VisitAddGeneralTransactionCommand(tranCommand);
        }

        public AddOrderCommandFactoryBase AddOrderCommandFactory
        {
            get { return AddGeneralOrderCommandFactory.Default; }
        }
    }

    internal sealed class AddPhysicalTransactionCommandService : IAddTranCommandService
    {
        internal static readonly AddPhysicalTransactionCommandService Default = new AddPhysicalTransactionCommandService();

        static AddPhysicalTransactionCommandService() { }
        private AddPhysicalTransactionCommandService() { }

        public TransactionConstructParams CreateConstructParams()
        {
            return new TransactionConstructParams();
        }

        public Transaction CreateTransaction(Account account, TransactionConstructParams constructParams)
        {
            return PhysicalTransactionServiceFactory.Default.CreateTransaction(account, constructParams);
        }

        public void Accept(Visitors.AddTransactionCommandVisitorBase visitor, AddTranCommandBase tranCommand)
        {
            visitor.VisitAddPhysicalTransactionCommand(tranCommand);
        }

        public AddOrderCommandFactoryBase AddOrderCommandFactory
        {
            get { return AddPhysicalOrderCommandFactory.Default; }
        }
    }

    internal sealed class AddBOTransactionCommandService : IAddTranCommandService
    {
        internal static readonly AddBOTransactionCommandService Default = new AddBOTransactionCommandService();

        static AddBOTransactionCommandService() { }
        private AddBOTransactionCommandService() { }

        public TransactionConstructParams CreateConstructParams()
        {
            return new TransactionConstructParams();
        }

        public Transaction CreateTransaction(Account account, TransactionConstructParams constructParams)
        {
           return BOTransactionServiceFactory.Default.CreateTransaction(account, constructParams);
        }

        public void Accept(Visitors.AddTransactionCommandVisitorBase visitor, AddTranCommandBase tranCommand)
        {
            visitor.VisitAddBOTransactionCommand(tranCommand);
        }

        public AddOrderCommandFactoryBase AddOrderCommandFactory
        {
            get { return AddBOOrderCommandFactory.Default; }
        }
    }

}
