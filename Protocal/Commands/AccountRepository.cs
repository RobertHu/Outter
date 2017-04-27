using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocal.Commands
{
    public abstract class AccountRepositoryBase
    {
        protected Dictionary<Guid, Account> _accounts = new Dictionary<Guid, Account>(500);
        protected object _mutex = new object();

        public IEnumerable<Account> Accounts
        {
            get
            {
                lock (_mutex)
                {
                    return _accounts.Values;
                }
            }
        }

        public abstract bool Remove(Guid accountId);

        public abstract Account CreateAccount(Guid accountId);

        public Transaction GetTran(Guid tranId)
        {
            lock (_mutex)
            {
                Guid accountId;
                if (!TransactionMapping.Default.GetAccountId(tranId, out accountId)) return null;
                Account account;
                if (!_accounts.TryGetValue(accountId, out account)) return null;
                return account.GetTran(tranId);
            }
        }

        public bool Contains(Guid accountId)
        {
            lock (_mutex)
            {
                return _accounts.ContainsKey(accountId);
            }
        }

        public Account GetOrAdd(Guid accountId)
        {
            lock (_mutex)
            {
                Account result;
                if (!_accounts.TryGetValue(accountId, out result))
                {
                    result = this.CreateAccount(accountId);
                    _accounts.Add(accountId, result);
                }
                result.IncrementReference();
                return result;
            }
        }

        public Account Get(Guid accountId)
        {
            lock (_mutex)
            {
                Account result = null;
                _accounts.TryGetValue(accountId, out result);
                return result;
            }
        }

        public Account GetByTranId(Guid tranId)
        {
            lock (_mutex)
            {
                Guid accountId;
                if (!TransactionMapping.Default.GetAccountId(tranId, out accountId)) return null;
                return this.Get(accountId);
            }
        }


        public bool TryGet(Guid accountId, out Account account)
        {
            lock (_mutex)
            {
                return _accounts.TryGetValue(accountId, out account);
            }
        }
    }


    public sealed class AccountRepository : AccountRepositoryBase
    {

        public static readonly AccountRepository Default = new AccountRepository();

        static AccountRepository() { }
        private AccountRepository() { }

        public override Account CreateAccount(Guid accountId)
        {
            return new Account(accountId);
        }

        public override bool Remove(Guid accountId)
        {
            lock (_mutex)
            {
                Account result = null;
                if (_accounts.TryGetValue(accountId, out result))
                {
                    if (result.DecrementReference() == 0)
                    {
                        _accounts.Remove(accountId);
                        TransactionMapping.Default.Clear(accountId);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
    }

}
