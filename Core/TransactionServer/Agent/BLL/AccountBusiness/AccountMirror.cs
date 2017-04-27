using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Engine.iExchange.BLL.OrderBLL;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal static class MirrorManager
    {
        internal static void Initialize(Dictionary<Guid, Account> source)
        {
            AccountMirror.Default.Initialize(source);
            HitMirror.Default.Initialize(source);
        }

        internal static void Add(Account account)
        {
            AccountMirror.Default.Add(account);
            HitMirror.Default.Add(account);
        }

        internal static void Remove(Guid accountId)
        {
            AccountMirror.Default.Remove(accountId);
            HitMirror.Default.Remove(accountId);
        }

    }


    internal sealed class AccountMirror : AccountMirrorBase
    {
        internal static readonly AccountMirror Default = new AccountMirror();

        private AccountMirror() { }
        static AccountMirror() { }

        internal void SetQuotation(QuotationBulk quotationBulk)
        {
            lock (_mutex)
            {
                Parallel.ForEach(_accounts.Values, account =>
                {
                    account.UpdateQuotation(quotationBulk);
                });
            }
        }

    }


    internal abstract class AccountMirrorBase
    {
        protected Dictionary<Guid, Account> _accounts = new Dictionary<Guid, Account>(5000);
        protected object _mutex = new object();

        internal void Initialize(Dictionary<Guid, Account> source)
        {
            lock (_mutex)
            {
                foreach (var eachPair in source)
                {
                    _accounts.Add(eachPair.Key, eachPair.Value);
                }
            }
        }

        internal void Add(Account account)
        {
            lock (_mutex)
            {
                if (!_accounts.ContainsKey(account.Id))
                {
                    _accounts.Add(account.Id, account);
                }
            }
        }

        internal void Remove(Guid accountId)
        {
            lock (_mutex)
            {
                if (_accounts.ContainsKey(accountId))
                {
                    _accounts.Remove(accountId);
                }
            }
        }
    }

}
