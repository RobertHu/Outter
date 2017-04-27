using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using Core.TransactionServer.Agent.Util.TypeExtension;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors
{
    //internal sealed class AddResetOrderCommandVisitor : AddOrderCommandVisitorBase
    //{
    //    private sealed class DataRowParameter
    //    {
    //        internal DataRowParameter(AddOrderCommandBase command)
    //        {
    //            var parameter = (AddResetOrderParamter)command.Param;
    //            this.OrderDataRow = parameter.DataRow;
    //            this.TradeDay = this.OrderDataRow.GetColumn<DateTime>("TradeDay");
    //            this.AccountId = this.OrderDataRow.GetColumn<Guid>("AccountID");
    //            this.InstrumentId = this.OrderDataRow.GetColumn<Guid>("InstrumentID");
    //        }

    //        internal Guid AccountId { get; private set; }
    //        internal Guid InstrumentId { get; private set; }
    //        internal DataRow OrderDataRow { get; private set; }
    //        internal DateTime TradeDay { get; private set; }
    //    }

    //    internal static readonly AddResetOrderCommandVisitor Default = new AddResetOrderCommandVisitor();
    //    static AddResetOrderCommandVisitor() { }
    //    private AddResetOrderCommandVisitor() { }

    //    public override void VisitAddGeneralOrderCommand(AddGeneralOrderCommand command)
    //    {
    //        this.VisitCommon<OrderConstructParams>(command, DataRowOrderParser.ParseForGeneral);
    //    }

    //    public override void VisitAddPhysicalOrderCommand(AddPhysicalOrderCommand command)
    //    {
    //        this.VisitCommon<PhysicalOrderConstructParams>(command, DataRowOrderParser.ParseForPhysical);
    //    }

    //    public override void VisitAddBOOrderCommand(AddBOOrderCommand command)
    //    {
    //        this.VisitCommon<BOOrderConstructParams>(command, DataRowOrderParser.ParseForBO);
    //    }

    //    private void VisitCommon<T>(AddOrderCommandBase command, Action<T, DataRow, Guid, Guid, DateTime?> parseAction) where T : OrderConstructParams
    //    {
    //        DataRowParameter param = new DataRowParameter(command);
    //        parseAction((T)command.ConstructParams, param.OrderDataRow, param.InstrumentId, param.AccountId, param.TradeDay);
    //        this.CreateOrder(command);
    //    }
    //}

}
