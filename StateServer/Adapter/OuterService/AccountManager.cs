using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using iExchange.Common;
using Protocal.TypeExtensions;
using log4net;
using System.Xml.Linq;

namespace iExchange.StateServer.Adapter.OuterService
{
    public sealed class AccountManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(AccountManager));
        private SystemControllerProxy _proxy;

        internal AccountManager(SystemControllerProxy proxy)
        {
            _proxy = proxy;
        }

        public XmlNode GetAccounts(Token token, Guid[] accountIDs, Guid[] instrumentIDs, bool includeTransactions, bool onlyCutOrder)
        {
            try
            {
                if (accountIDs == null)
                {
                    accountIDs = new Guid[AccountRepository.Default.Count];
                    AccountRepository.Default.FillAccountIds(accountIDs, 0);
                }
                Array.Sort(accountIDs);

                XElement accountsNode = new XElement("Accounts");
                foreach (Guid accountID in accountIDs)
                {
                    if (!AccountRepository.Default.Contains(accountID))
                    {
                        string content = _proxy.GetInitializeData(new List<Guid> { accountID });
                        AccountRepository.Default.CreateAndFillAccount(accountID, content);
                    }

                    Account account = (Account)AccountRepository.Default.Get(accountID);
                    XElement accountNode;
                    Protocal.AccountFloatingStatus floatingStatus = _proxy.GetAccountFloatingStatus(accountID);
                    if (floatingStatus != null)
                    {
                        account.InitializeFloatingStatus(floatingStatus);
                    }

                    if (token != null)
                    {
                        if (token.AppType == AppType.BackOffice)
                        {
                            accountNode = account.ToXml(new XmlParameter(false, false, includeTransactions));
                        }
                        else
                        {
                            accountNode = account.ToXml();
                            if (includeTransactions)
                            {
                                var trans = account.GetTrans(instrumentIDs);
                                if (trans.Count > 0)
                                {
                                    XElement transNode = new XElement("Transactions");
                                    foreach (Transaction tran in trans)
                                    {
                                        if (onlyCutOrder)
                                        {
                                            if (tran.SubmitorId == Guid.Empty && tran.ApproverId == Guid.Empty)
                                            {
                                                transNode.Add(tran.ToXml(false, true));
                                            }
                                        }
                                        else
                                        {
                                            transNode.Add(tran.ToXml(false, true));
                                        }
                                    }
                                    accountNode.Add(transNode);
                                }
                            }
                        }
                        accountsNode.Add(accountNode);
                    }
                }
                //    else //for test
                //    {
                //        accountNode = xmlDoc.ImportNode(account.ToXmlNodeTest(), true);

                //        if (includeTransactions)
                //        {
                //            var trans = account.GetTrans(instrumentIDs);
                //            if (trans.Count > 0)
                //            {
                //                Transaction[] trans2 = new Transaction[trans.Count];
                //                trans.CopyTo(trans2);
                //                Array.Sort(trans2, Transaction.SubmitTimeComparer);

                //                XmlElement transNode = xmlDoc.CreateElement("Transactions");
                //                foreach (Transaction tran in trans2)
                //                {
                //                    if (onlyCutOrder)
                //                    {
                //                        if (tran.SubmitorId == Guid.Empty && tran.ApproverId == Guid.Empty)
                //                        {
                //                            transNode.AppendChild(xmlDoc.ImportNode(tran.ToXml(false, true).ToXmlNode(), true));
                //                        }
                //                    }
                //                    else
                //                    {
                //                        transNode.AppendChild(xmlDoc.ImportNode(tran.ToXml(false, true).ToXmlNode(), true));
                //                    }
                //                }
                //                accountNode.AppendChild(transNode);
                //            }
                //        }
                //    }

                //    accountsNode.AppendChild(xmlDoc.ImportNode(accountNode, true));
                //}
                return accountsNode.ToXmlNode();
            }
            catch (Exception e)
            {
                return null;
            }
        }

    }
}