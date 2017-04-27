using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.TransactionBLL.Visitors
{
    public abstract class AddTransactionCommandVisitorBase
    {
        internal abstract void VisitAddGeneralTransactionCommand(Commands.AddTranCommandBase command);
        internal abstract void VisitAddPhysicalTransactionCommand(Commands.AddTranCommandBase command);
        internal abstract void VisitAddBOTransactionCommand(Commands.AddTranCommandBase command);
    }
}
