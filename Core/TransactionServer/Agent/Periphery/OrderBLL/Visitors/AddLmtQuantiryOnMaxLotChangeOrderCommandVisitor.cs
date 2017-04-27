using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors
{
    internal sealed class AddLmtQuantiryOnMaxLotChangeOrderCommandVisitor : AddOrderCommandVisitorBase
    {
        internal static readonly AddLmtQuantiryOnMaxLotChangeOrderCommandVisitor Default = new AddLmtQuantiryOnMaxLotChangeOrderCommandVisitor();

        static AddLmtQuantiryOnMaxLotChangeOrderCommandVisitor() { }
        private AddLmtQuantiryOnMaxLotChangeOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.ParseCommon((Commands.AddLmtQuantiryOnMaxLotChangeOrderCommandBase)command);
            this.CreateOrder(command);
        }

        public override void VisitAddPhysicalOrderCommand(Commands.AddOrderCommandBase command)
        {
            this.ParsePhysical((Commands.AddLmtQuantiryOnMaxLotChangeOrderCommandBase)command);
            this.CreateOrder(command);
        }

        public override void VisitAddBOOrderCommand(Commands.AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }

        protected override void CreateOrderRelation(Order closeOrder, Commands.AddOrderCommandBase command)
        {
            var lmtQuantiryOnMaxLotChangeOrderCommand = (Commands.AddLmtQuantiryOnMaxLotChangeOrderCommandBase)command;
            List<OrderRelation> orderRelations = new List<OrderRelation>(lmtQuantiryOnMaxLotChangeOrderCommand.OriginOrder.OrderRelations);
            orderRelations.Sort(OrderRelation.AutoCloseComparer);
            decimal totalCloseLot = lmtQuantiryOnMaxLotChangeOrderCommand.Lot;
            foreach (OrderRelation sourceOrderRelation in orderRelations)
            {
                if (sourceOrderRelation.ClosedLot < totalCloseLot)
                {
                    var orderRelationCommand = command.AddOrderRelationFactory.Create(closeOrder, sourceOrderRelation.OpenOrder, sourceOrderRelation.ClosedLot);
                    orderRelationCommand.Execute();
                    totalCloseLot -= sourceOrderRelation.ClosedLot;
                }
                else
                {

                    var orderRelationCommand = command.AddOrderRelationFactory.Create(closeOrder, sourceOrderRelation.OpenOrder, totalCloseLot);
                    orderRelationCommand.Execute();
                    break;
                }
            }
        }

        private void ParsePhysical(Commands.AddLmtQuantiryOnMaxLotChangeOrderCommandBase command)
        {
            this.ParseCommon(command);
            var originPhysicalOrder = (Physical.PhysicalOrder)command.OriginOrder;
            var constructParams = (PhysicalOrderConstructParams)command.ConstructParams;
            constructParams.PhysicalSettings = new PhysicalConstructParams();
            constructParams.PhysicalSettings.PhysicalTradeSide = originPhysicalOrder.PhysicalTradeSide;
            constructParams.PhysicalSettings.PhysicalRequestId = originPhysicalOrder.PhysicalRequestId;
            constructParams.PhysicalSettings.PhysicalType = originPhysicalOrder.PhysicalType;
            if (originPhysicalOrder.Instalment != null)
            {
                constructParams.Instalment = new InstalmentConstructParams();
                constructParams.Instalment.InstalmentPolicyId = originPhysicalOrder.Instalment.InstalmentPolicyId;
                constructParams.Instalment.InstalmentType = originPhysicalOrder.Instalment.InstalmentType;
                constructParams.Instalment.RecalculateRateType = originPhysicalOrder.Instalment.RecalculateRateType;
                constructParams.Instalment.Period = originPhysicalOrder.Instalment.Period;
                constructParams.Instalment.Frequence = originPhysicalOrder.Instalment.Frequence;
                constructParams.Instalment.DownPayment = originPhysicalOrder.Instalment.DownPayment;
                constructParams.Instalment.DownPaymentBasis = originPhysicalOrder.Instalment.DownPaymentBasis;
            }

        }

        private void ParseCommon(Commands.AddLmtQuantiryOnMaxLotChangeOrderCommandBase command)
        {
            var constructParams = command.ConstructParams;
            constructParams.Id = Guid.NewGuid();
            constructParams.Phase = iExchange.Common.OrderPhase.Placed;
            constructParams.TradeOption = command.OriginOrder.TradeOption;
            constructParams.IsOpen = command.OriginOrder.IsOpen;
            constructParams.IsBuy = command.OriginOrder.IsBuy;
            constructParams.SetPrice = command.OriginOrder.SetPrice;
            constructParams.Lot = command.Lot;
            constructParams.OriginalLot = command.Lot;
            constructParams.Code = command.GenerateOrderCode();
            constructParams.OriginCode = constructParams.Code;
        }

    }
}
