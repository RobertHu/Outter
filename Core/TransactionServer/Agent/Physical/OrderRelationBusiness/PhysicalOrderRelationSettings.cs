using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.BLL.OrderRelationBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.OrderRelationBLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.OrderRelationBusiness
{
    internal sealed class PhysicalOrderRelationSettings : BusinessItemBuilder
    {
        private BusinessItem<DateTime?> _physicalValueMatureDay;
        private BusinessItem<decimal> _overdueCutPenalty;
        private BusinessItem<decimal> _closePenalty;
        private BusinessItem<decimal> _payBackPledge;
        private BusinessItem<decimal> _closedPhysicalValue;
        private BusinessItem<decimal> _physicalValue;


        internal PhysicalOrderRelationSettings(PhysicalOrderRelation owner, PhysicalOrderRelationConstructParams constructParams)
            : base(owner)
        {
            this.Parse(constructParams);
        }

        private void Parse(PhysicalOrderRelationConstructParams constructParams)
        {
            _physicalValueMatureDay = this.CreateSoundItem(OrderRelationBusinessItemNames.PhysicalValueMatureDate, constructParams.PhysicalValueMatureDay);
            _overdueCutPenalty = this.CreateSoundItem("OverdueCutPenalty", constructParams.OverdueCutPenalty);
            _closePenalty = this.CreateSoundItem("ClosePenalty", constructParams.ClosePenalty);
            _payBackPledge = this.CreateSoundItem("PayBackPledge", constructParams.PayBackPledge);
            _closedPhysicalValue = this.CreateSoundItem("ClosedPhysicalValue", constructParams.ClosedPhysicalValue);
            _physicalValue = this.CreateSoundItem("physicalValue", constructParams.PhysicalValue);
        }

        internal DateTime? PhysicalValueMatureDay
        {
            get { return this._physicalValueMatureDay.Value; }
            set { _physicalValueMatureDay.SetValue(value); }
        }

        internal decimal OverdueCutPenalty
        {
            get { return _overdueCutPenalty.Value; }
            set { _overdueCutPenalty.SetValue(value); }
        }

        internal decimal ClosePenalty
        {
            get { return _closePenalty.Value; }
            set { _closePenalty.SetValue(value); }
        }


        internal decimal PayBackPledge
        {
            get { return _payBackPledge.Value; }
            set { _payBackPledge.SetValue(value); }
        }

        internal decimal ClosedPhysicalValue
        {
            get { return _closedPhysicalValue.Value; }
            set { _closedPhysicalValue.SetValue(value); }
        }

        internal decimal PhysicalValue
        {
            get { return _physicalValue.Value; }
            set { _physicalValue.SetValue(value); }
        }


    }

}
