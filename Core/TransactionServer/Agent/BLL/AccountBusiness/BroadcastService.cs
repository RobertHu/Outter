using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.TransactionServer.Agent.Util.TypeExtension;
using System.Diagnostics;
using CachingAssistant = iExchange.Common.Caching.Transaction.Assistant;
using log4net;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Caching;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal class BroadcastService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BroadcastService));
        private Account _account;

        internal BroadcastService(Account account)
        {
            _account = account;
        }

        public bool SaveAndBroadcastChanges(out string content)
        {
            content = string.Empty;
            try
            {
                content = this.SaveCommon(CacheType.Transaciton);
                Broadcaster.Default.Add(_account.Id, content);
                _account.AcceptChanges();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                _account.RejectChanges();
                return false;
            }
        }

        internal string SaveTradingContent()
        {
            try
            {
                string content = this.SaveCommon(CacheType.Transaciton);
                _account.AcceptChanges();
                return content;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                _account.RejectChanges();
                return string.Empty;
            }
        }


        internal bool SaveResetContent(CacheType cacheType, out string content)
        {
            content = string.Empty;
            try
            {
                content = this.SaveCommon(cacheType);
#if NOBROADCAST
#else
                Broadcaster.Default.Add(_account.Id, content);
#endif
                _account.AcceptChanges();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                _account.RejectChanges();
                return false;
            }
        }

        internal string SaveCommon(Caching.CacheType cacheType)
        {
            if (_account.Status == Framework.ChangeStatus.None) return string.Empty;
            var changeContent = this.GenerateChangeContent();
            CacheCenter.Default.Add(_account, changeContent, cacheType);
            return changeContent;
        }

        private string GenerateChangeContent()
        {
            StringBuilder result = new StringBuilder(500);
            _account.WriteXml(result, m => false);
            return result.ToString();
        }
    }
}
