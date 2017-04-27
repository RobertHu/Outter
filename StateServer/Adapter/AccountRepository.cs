using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace iExchange.StateServer.Adapter
{
    internal class AccountRepository : Protocal.Commands.AccountRepositoryBase
    {
        internal static readonly AccountRepository Default = new AccountRepository();

        static AccountRepository() { }
        private AccountRepository() { }


        internal int Count
        {
            get
            {
                lock (_mutex)
                {
                    return _accounts.Count;
                }
            }
        }

        public override bool Remove(Guid accountId)
        {
            lock (_mutex)
            {
                if (!_accounts.ContainsKey(accountId)) return false;
                return _accounts.Remove(accountId);
            }
        }

        internal void FillAccountIds(Guid[] target, int index)
        {
            lock (_mutex)
            {
                _accounts.Keys.CopyTo(target, index);
            }
        }


        public override Protocal.Commands.Account CreateAccount(Guid accountId)
        {
            return new Account(accountId);
        }

        internal Account CreateAndFillAccount(Guid accountId, string xml)
        {
            var account = (Account)this.GetOrAdd(accountId);
            account.Initialize(XElement.Parse(xml).Element("Account"));
            return account;
        }

    }

}