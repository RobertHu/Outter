using Core.TransactionServer.Agent.BLL.OrderBusiness;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery.OrderBLL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal sealed class BOOrderSettings : OrderSettings
    {
        private BusinessItem<Guid> _betTypeId;
        private BusinessItem<int> _frequency;
        private BusinessItem<long> _betOption;
        private BusinessItem<decimal> _odds;
        private BusinessItem<decimal> _paidPledge;
        private BusinessItem<decimal> _paidPledgeBalance;
        private BusinessItem<DateTime?> _settleTime;
        internal BOOrderSettings(Order order, BOOrderConstructParams boOrderConstructParams)
            : base(order, boOrderConstructParams)
        {
        }

        protected override void Parse(OrderConstructParams constructParams)
        {
            base.Parse(constructParams);
            var boOrderConstructParams = (BOOrderConstructParams)constructParams;
            _betTypeId = this.CreateReadonlyItem("BOBetTypeID", boOrderConstructParams.BetTypeId);
            _frequency = this.CreateReadonlyItem("BOFrequency", boOrderConstructParams.Frequency);
            _betOption = this.CreateReadonlyItem("BOBetOption", boOrderConstructParams.BetOption);
            _settleTime = this.CreateSoundItem("BOSettleTime", boOrderConstructParams.SettleTime);
            _odds = this.CreateReadonlyItem("BOOdds", boOrderConstructParams.Odds);
            _paidPledge = this.CreateSoundItem("PaidPledge", boOrderConstructParams.PaidPledge);
            _paidPledgeBalance = this.CreateSoundItem("PaidPledgeBalance", boOrderConstructParams.PaidPledgeBalance);
        }

        internal Guid BetTypeId
        {
            get
            {
                return _betTypeId.Value;
            }
        }

        internal int Frequency
        {
            get
            {
                return _frequency.Value;
            }
        }

        internal long BetOption
        {
            get
            {
                return _betOption.Value;
            }
        }

        internal decimal Odds
        {
            get
            {
                return _odds.Value;
            }
        }

        internal DateTime? SettleTime
        {
            get { return _settleTime.Value; }
            set { _settleTime.SetValue(value); }
        }

        internal decimal PaidPledge
        {
            get { return _paidPledge.Value; }
            set { _paidPledge.SetValue(value); }
        }

        internal decimal PaidPledgeBalance
        {
            get { return _paidPledgeBalance.Value; }
            set { _paidPledgeBalance.SetValue(value); }
        }

    }

}
