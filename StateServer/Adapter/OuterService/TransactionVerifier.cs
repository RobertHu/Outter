using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;
using System.Xml;
using log4net;

namespace iExchange.StateServer.Adapter.OuterService
{
    internal sealed class TransactionVerifier
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionVerifier));

        private Dictionary<Guid, DateTime> _suspiciousTransDict = new Dictionary<Guid, DateTime>();

        internal static readonly TransactionVerifier Default = new TransactionVerifier();

        static TransactionVerifier() { }
        private TransactionVerifier() { }


        internal Guid[] VerifyTransaction(Token token, Guid[] transactionIDs, out XmlNode[] xmlTrans, out XmlNode[] xmlAccounts)
        {
            List<Guid> canceledTrans = new List<Guid>();
            xmlTrans = null;
            xmlAccounts = null;

            List<XmlNode> trans = new List<XmlNode>();
            List<XmlNode> accounts = new List<XmlNode>();

            foreach (var eachTranId in transactionIDs)
            {
                Transaction tran = (Transaction)AccountRepository.Default.GetTran(eachTranId);
                this.VerifyIndividualTransaction(token, tran, eachTranId, canceledTrans, trans, accounts);
            }
            xmlTrans = trans.ToArray();
            xmlAccounts = accounts.ToArray();
            return canceledTrans.ToArray();
        }

        private void VerifyIndividualTransaction(Token token, Transaction tran, Guid tranId, List<Guid> canceledTrans, List<XmlNode> trans, List<XmlNode> accounts)
        {
            if (tran == null)
            {
                this.VerifyWhenTranNotExist(token, tranId, canceledTrans);
            }
            else
            {
                this.VerifyWhenTranExist(token, tran, tranId, canceledTrans, trans, accounts);
            }
        }

        private void VerifyWhenTranNotExist(Token token, Guid tranId, List<Guid> canceledTrans)
        {
            if (_suspiciousTransDict.ContainsKey(tranId))
            {
                DateTime lastVerifyTime = (DateTime)_suspiciousTransDict[tranId];
                if (((TimeSpan)(DateTime.Now - lastVerifyTime)).TotalSeconds > 5)
                {
                    canceledTrans.Add(tranId);
                    _suspiciousTransDict.Remove(tranId);
                }
                Logger.WarnFormat("VerifyTransaction: Removed {0}\r\n{1}", token, tranId);
            }
            else
            {
                _suspiciousTransDict.Add(tranId, DateTime.Now);
            }
        }

        private void VerifyWhenTranExist(Token token, Transaction tran, Guid tranId, List<Guid> canceledTrans, List<XmlNode> trans, List<XmlNode> accounts)
        {
            if (tran.Phase == TransactionPhase.Canceled)
            {
                canceledTrans.Add(tranId);
                Logger.WarnFormat("VerifyTransaction: Canceled already {0}\r\n{1}", token, tranId);
            }
            else if (tran.Phase == TransactionPhase.Executed)
            {
                if (tran.Owner.IsMultiCurrency)
                {
                    accounts.Add(((Account)tran.Owner).ToXmlNode(tran.CurrencyId));
                }
                else
                {
                    accounts.Add(((Account)tran.Owner).ToXmlNode());
                }
                var xmlTran = tran.GetExecuteXmlElement();
                trans.Add(xmlTran);
                Logger.WarnFormat("VerifyTransaction: Executed already {0}\r\n{1}", token, tranId);
            }
        }

    }
}