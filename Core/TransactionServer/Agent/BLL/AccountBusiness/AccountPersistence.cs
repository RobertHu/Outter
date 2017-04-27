using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.Framework;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal sealed class AccountPersistence
    {
        private BusinessItem<decimal?> _leverage;
        private BusinessItem<decimal?> _rateMarginD;
        private BusinessItem<decimal?> _rateMarginO;
        private BusinessItem<decimal?> _rateMarginLockD;
        private BusinessItem<decimal?> _rateMarginLockO;

        internal AccountPersistence(Account owner)
        {
            _leverage = this.CreateSoundItem<decimal?>("Leverage", owner);
            _rateMarginD = this.CreateSoundItem<decimal?>("RateMarginD", owner);
            _rateMarginO = this.CreateSoundItem<decimal?>("RateMarginO", owner);
            _rateMarginLockD = this.CreateSoundItem<decimal?>("RateMarginLockD", owner);
            _rateMarginLockO = this.CreateSoundItem<decimal?>("RateMarginLockO", owner);
            this.ClearStatus();
        }

        private void ClearStatus()
        {
            _leverage.Status = ChangeStatus.None;
            _rateMarginD.Status = ChangeStatus.None;
            _rateMarginO.Status = ChangeStatus.None;
            _rateMarginLockD.Status = ChangeStatus.None;
            _rateMarginLockO.Status = ChangeStatus.None;
        }

        internal decimal? Leverage
        {
            get { return _leverage.Value; }
            set { _leverage.SetValue(value); }
        }

        internal decimal? RateMarginD
        {
            get { return _rateMarginD.Value; }
            set { _rateMarginD.SetValue(value); }
        }

        internal decimal? RateMarginO
        {
            get { return _rateMarginO.Value; }
            set { _rateMarginO.SetValue(value); }
        }

        internal decimal? RateMarginLockD
        {
            get { return _rateMarginLockD.Value; }
            set { _rateMarginLockD.SetValue(value); }
        }

        internal decimal? RateMarginLockO
        {
            get { return _rateMarginLockO.Value; }
            set { _rateMarginLockO.SetValue(value); }
        }

        internal BusinessItem<T> CreateSoundItem<T>(string name, BusinessRecord parent)
        {
            return BusinessItemFactory.Create<T>(name, default(T), PermissionFeature.Sound, parent);
        }

    }
}
