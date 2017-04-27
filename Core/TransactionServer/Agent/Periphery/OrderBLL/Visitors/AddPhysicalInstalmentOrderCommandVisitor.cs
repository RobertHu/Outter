using Core.TransactionServer.Agent.Periphery.OrderBLL.Commands;
using Core.TransactionServer.Agent.Physical;
using Core.TransactionServer.Agent.Physical.OrderBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery.OrderBLL.Visitors
{
    internal sealed class AddPhysicalInstalmentOrderCommandVisitor : AddOrderCommandVisitorBase
    {
        internal static readonly AddPhysicalInstalmentOrderCommandVisitor Default = new AddPhysicalInstalmentOrderCommandVisitor();

        private AddPhysicalInstalmentOrderCommandVisitor() { }

        public override void VisitAddGeneralOrderCommand(AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }

        public override void VisitAddPhysicalOrderCommand(AddOrderCommandBase command)
        {
            var instalmentOrderCommand = (AddInstalmentOrderOrderCommand)command;
            this.ParseCommon(instalmentOrderCommand);
             command.CreateOrder();
            command.Result.CopyHitInfoFrom(instalmentOrderCommand.OldOrder);
        }

        public override void VisitAddBOOrderCommand(AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }

        private void ParseCommon(AddInstalmentOrderOrderCommand command)
        {
            var lotBalance = command.IsOpen ? command.Lot : 0;
            PhysicalOrderConstructParams constructParams = (PhysicalOrderConstructParams)command.ConstructParams;
            constructParams.Id = Guid.NewGuid();
            constructParams.Phase = OrderPhase.Placed;
            constructParams.IsOpen = command.IsOpen;
            constructParams.IsBuy = command.IsBuy;
            constructParams.SetPrice = command.OldOrder.SetPrice;
            constructParams.SetPrice2 = command.OldOrder.SetPrice2;
            constructParams.ExecutePrice = command.OldOrder.ExecutePrice;
            constructParams.Lot = command.Lot;
            constructParams.OriginalLot = command.Lot;
            constructParams.LotBalance = lotBalance;
            constructParams.TradeOption = command.OldOrder.TradeOption;
            constructParams.PhysicalSettings = new PhysicalConstructParams
            {
                PhysicalTradeSide = command.OldOrder.PhysicalTradeSide,
                PhysicalType = command.OldOrder.PhysicalType
            };
            constructParams.Instalment = this.CreateInstalmentConstructParams(command.OldOrder);
        }

        private InstalmentConstructParams CreateInstalmentConstructParams(PhysicalOrder order)
        {
            if (order.Instalment == null) return null;
            InstalmentConstructParams result = new InstalmentConstructParams
            {
                InstalmentPolicyId = order.Instalment.InstalmentPolicyId,
                DownPayment = order.Instalment.DownPayment,
                DownPaymentBasis = order.Instalment.DownPaymentBasis,
                InstalmentOverdueDay = order.Instalment.InstalmentOverdueDay,
                Period = order.Period,
                Frequence = order.Frequence,
                InstalmentType = order.Instalment.InstalmentType,
                IsInstalmentOverdue = order.Instalment.IsInstalmentOverdue,
                RecalculateRateType = order.Instalment.RecalculateRateType
            };
            return result;
        }


        protected override void CreateOrderRelation(Order closeOrder, AddOrderCommandBase command)
        {
            throw new NotImplementedException();
        }
    }
}
