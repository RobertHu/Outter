using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Commands
{
    internal abstract class AddBookTransactionCommandBase : AddTranCommandBase
    {
        protected AddBookTransactionCommandBase(Account account, IAddTranCommandService commandService, Visitors.AddTransactionCommandVisitorBase visitor, Protocal.TransactionBookData tranData)
            : base(account, visitor, commandService)
        {
            this.TranData = tranData;
        }

        internal Protocal.TransactionBookData TranData { get; private set; }
    }



    internal sealed class AddBookWithNoCalculationTransactionCommand : AddBookTransactionCommandBase
    {
        internal AddBookWithNoCalculationTransactionCommand(Account account, Protocal.TransactionBookData tranData)
            : base(account, AddTransactionCommandService.Default, Visitors.AddBookWithNoCalculationTransactionCommandVistor.Default, tranData)
        {
        }
    }


    internal sealed class AddBookTransactionCommand : AddBookTransactionCommandBase
    {
        internal AddBookTransactionCommand(Account account, Protocal.TransactionBookData tranData)
            : base(account, AddTransactionCommandService.Default, Visitors.AddBookTransactionCommandVisitor.Default, tranData)
        {
        }
    }


    internal sealed class AddBookWithNoCalculationPhysicalTransactionCommand : AddBookTransactionCommandBase
    {
        internal AddBookWithNoCalculationPhysicalTransactionCommand(Account account, Protocal.TransactionBookData tranData)
            : base(account, AddPhysicalTransactionCommandService.Default, Visitors.AddBookWithNoCalculationTransactionCommandVistor.Default, tranData)
        {
        }
    }

    internal sealed class AddBookPhysicalTransactionCommand : AddBookTransactionCommandBase
    {
        internal AddBookPhysicalTransactionCommand (Account account, Protocal.TransactionBookData tranData)
            : base(account, AddPhysicalTransactionCommandService.Default, Visitors.AddBookTransactionCommandVisitor.Default, tranData)
        {
        }
    }


    internal abstract class AddCommunicationTransactionCommandBase : AddTranCommandBase
    {
        protected AddCommunicationTransactionCommandBase(Account account, IAddTranCommandService addTranCommandService, Protocal.TransactionData tranData)
            : base(account, Visitors.AddCommunicationTransactionCommandVisitor.Default, addTranCommandService)
        {
            this.TranData = tranData;
        }

        internal Protocal.TransactionData TranData { get; private set; }
    }


    internal sealed class AddCommunicationTransactionCommand : AddCommunicationTransactionCommandBase
    {
        internal AddCommunicationTransactionCommand(Account account, Protocal.TransactionData tranData)
            : base(account, AddTransactionCommandService.Default, tranData)
        {
        }
    }


    internal sealed class AddCommunicationPhysicalTransactionCommand : AddCommunicationTransactionCommandBase
    {
        internal AddCommunicationPhysicalTransactionCommand(Account account, Protocal.TransactionData tranData)
            : base(account, AddPhysicalTransactionCommandService.Default, tranData)
        {
        }
    }

    internal sealed class AddCommunicationBOTransactionCommand : AddCommunicationTransactionCommandBase
    {
        internal AddCommunicationBOTransactionCommand(Account account, Protocal.TransactionData tranData)
            : base(account, AddBOTransactionCommandService.Default, tranData)
        {
        }
    }

}
