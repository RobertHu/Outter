using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.OrderBLL;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.Physical.OrderBusiness
{
    internal sealed class PhysicalOrderSettings : OrderSettings
    {
        private BusinessItem<PhysicalTradeSide> _physicalTradeSide;
        private BusinessItem<int> _physicalValueMatureDay;
        private BusinessItem<Guid?> _physicalRequestId;
        private BusinessItem<decimal> _physicalOriginValue;
        private BusinessItem<decimal> _physicalOriginValueBalance;
        private BusinessItem<decimal> _paidPledgeBalance;
        private BusinessItem<decimal> _paidPledgeForDB;
        private BusinessItem<decimal> _advanceAmount;
        private BusinessItem<Protocal.Physical.PhysicalType> _physicalType;
        private Instalment _instalment;
        private Delivery.DeliveryModel _deliveryModel;

        private PhysicalOrder _owner;

        internal PhysicalOrderSettings(PhysicalOrder order, PhysicalOrderConstructParams constructParams)
            : base(order, constructParams)
        {
            _owner = order;
            _deliveryModel = new Delivery.DeliveryModel(order);
            _instalment = Instalment.Create(order, constructParams.Instalment);
        }

        protected override void Parse(OrderConstructParams constructParams)
        {
            base.Parse(constructParams);
            var physicalOrderConstructParams = (PhysicalOrderConstructParams)constructParams;
            this.ParsePhysicalSettings(physicalOrderConstructParams.PhysicalSettings);
        }


        private void ParsePhysicalSettings(PhysicalConstructParams constructParams)
        {
            _physicalTradeSide = this.CreateSoundItem(OrderBusinessItemNames.PhysicalTradeSide, constructParams.PhysicalTradeSide);
            _physicalValueMatureDay = this.CreateSoundItem(OrderBusinessItemNames.PhysicalValueMatureDay, constructParams.PhysicalValueMatureDay);
            _physicalRequestId = this.CreateSoundItem(OrderBusinessItemNames.PhysicalRequestId, constructParams.PhysicalRequestId);
            _physicalType = this.CreateReadonlyItem("PhysicalType", constructParams.PhysicalType);
            _physicalOriginValue = this.CreateSoundItem(OrderBusinessItemNames.PhysicalOriginValue, constructParams.PhysicalOriginValue);
            _physicalOriginValueBalance = this.CreateSoundItem(OrderBusinessItemNames.PhysicalOriginValueBalance, constructParams.PhysicalOriginValueBalance);
            _paidPledgeBalance = this.CreateSoundItem(OrderBusinessItemNames.PaidPledgeBalance, constructParams.PaidPledgeBalance);
            _paidPledgeForDB = this.CreateSoundItem(OrderBusinessItemNames.PaidPledge, constructParams.PaidPledge);
            _advanceAmount = this.CreateReadonlyItem("AdvanceAmount", constructParams.AdvanceAmount);
        }

        internal Instalment Instalment
        {
            get
            {
                return _instalment;
            }
        }

        internal Delivery.DeliveryModel DeliveryModel
        {
            get { return _deliveryModel; }
        }

        internal PhysicalTradeSide PhysicalTradeSide
        {
            get { return _physicalTradeSide.Value; }
            set { _physicalTradeSide.SetValue(value); }
        }


        internal int PhysicalValueMatureDay
        {
            get { return _physicalValueMatureDay.Value; }
        }

        internal decimal PhysicalOriginValue
        {
            get { return _physicalOriginValue.Value; }
            set { _physicalOriginValue.SetValue(value); }
        }

        internal decimal PhysicalOriginValueBalance
        {
            get { return _physicalOriginValueBalance.Value; }
            set { _physicalOriginValueBalance.SetValue(value); }
        }

        internal decimal PaidPledgeBalance
        {
            get { return _paidPledgeBalance.Value; }
            set { _paidPledgeBalance.SetValue(value); }
        }

        internal decimal PaidPledgeForDB
        {
            get { return _paidPledgeForDB.Value; }
            set { _paidPledgeForDB.SetValue(value); }
        }


        internal Protocal.Physical.PhysicalType PhysicalType
        {
            get { return _physicalType.Value; }
        }

        internal Guid? PhysicalRequestId
        {
            get { return _physicalRequestId.Value; }
            set { _physicalRequestId.SetValue(value); }
        }

    }
}
