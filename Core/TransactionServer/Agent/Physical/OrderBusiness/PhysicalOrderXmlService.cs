using Core.TransactionServer.Agent.BLL.OrderBusiness;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    public sealed class PhysicalOrderXmlService : OrderXmlServiceBase
    {
        internal PhysicalOrderXmlService(PhysicalOrder order)
            : base(order) { }


        public override XmlElement ToXmlNode(XmlDocument xmlTran, XmlElement tranNode)
        {
            var order = (PhysicalOrder)_order;
            var orderNode = base.ToXmlNode(xmlTran, tranNode);
            orderNode.SetAttribute("PhysicalTradeSide", XmlConvert.ToString((int)order.PhysicalTradeSide));
            orderNode.SetAttribute("PhysicalOriginValue", XmlConvert.ToString(order.PhysicalOriginValue));
            orderNode.SetAttribute("PhysicalOriginValueBalance", XmlConvert.ToString(order.PhysicalOriginValueBalance));
            orderNode.SetAttribute("PhysicalValueMatureDay", XmlConvert.ToString(order.PhysicalValueMatureDay));
            orderNode.SetAttribute("PaidPledge", XmlConvert.ToString(order.PaidPledge));
            orderNode.SetAttribute("PaidPledgeBalance", XmlConvert.ToString(order.PaidPledgeBalance));
            if (order.PhysicalRequestId != null) orderNode.SetAttribute("PhysicalRequestId", XmlConvert.ToString(order.PhysicalRequestId.Value));
            if (order.IsInstalment)
            {
                //order.Instalment.ToXmlNode(orderNode);
            }
            orderNode.SetAttribute("DeliveryLockLot", XmlConvert.ToString(order.DeliveryLockLot));
            return orderNode;
        }


        protected override void InnerGetExecuteXmlString(StringBuilder stringBuilder, DateTime executeTradeDay)
        {
            var order = (PhysicalOrder)_order;
            var _instalment = order.Instalment;
            base.InnerGetExecuteXmlString(stringBuilder, executeTradeDay);
            stringBuilder.AppendFormat(" PhysicalTradeSide='{0}'", XmlConvert.ToString((int)order.PhysicalTradeSide));
            if (order.PhysicalRequestId != null) stringBuilder.AppendFormat(" PhysicalRequestId='{0}'", XmlConvert.ToString(order.PhysicalRequestId.Value));
            if (order.PhysicalValueMatureDay != 0) stringBuilder.AppendFormat(" PhysicalValueMatureDay='{0}'", XmlConvert.ToString(order.PhysicalValueMatureDay));
            stringBuilder.AppendFormat(" PhysicalOriginValue='{0}'", XmlConvert.ToString(order.PhysicalOriginValue));
            stringBuilder.AppendFormat(" PhysicalOriginValueBalance='{0}'", XmlConvert.ToString(order.PhysicalOriginValueBalance));
            stringBuilder.AppendFormat(" ValueAsMargin='{0}'", XmlConvert.ToString(order.ValueAsMargin));
            stringBuilder.AppendFormat(" PaidPledge='{0}'", XmlConvert.ToString(order.PaidPledge));
            stringBuilder.AppendFormat(" PaidPledgeBalance='{0}'", XmlConvert.ToString(order.PaidPledgeBalance));
            if (order.IsInstalment)
            {
                stringBuilder.AppendFormat(" InstalmentPolicyId='{0}'", XmlConvert.ToString(_instalment.InstalmentPolicyId));
                stringBuilder.AppendFormat(" PhysicalInstalmentType='{0}'", XmlConvert.ToString((int)_instalment.InstalmentType));
                stringBuilder.AppendFormat(" Period='{0}'", XmlConvert.ToString(_instalment.Period.Period));
                stringBuilder.AppendFormat(" InstalmentFrequence='{0}'", XmlConvert.ToString((int)_instalment.Period.Frequence));
                stringBuilder.AppendFormat(" DownPayment='{0}'", XmlConvert.ToString(_instalment.DownPayment));
                stringBuilder.AppendFormat(" DownPaymentBasis='{0}'", XmlConvert.ToString((int)_instalment.DownPaymentBasis));
                stringBuilder.AppendFormat(" RecalculateRateType='{0}'", XmlConvert.ToString((int)_instalment.RecalculateRateType));
                stringBuilder.AppendFormat(" InstalmentAdministrationFee='{0}'", XmlConvert.ToString(order.InstalmentAdministrationFee));
            }
        }
    }
}
