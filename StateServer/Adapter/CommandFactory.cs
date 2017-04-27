using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;
using System.Xml;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using System.Data.SqlTypes;
using log4net;

namespace iExchange.StateServer.Adapter
{
    internal static class CommandFactory
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommandFactory));
        internal static Command CreateAlertLevelRisedCommand(this Account account)
        {
            AlertCommand alertCommand = new AlertCommand();
            alertCommand.AccountID = account.Id;
            alertCommand.Content = account.CreateXmlNodeForAlertLevelRised().ToXmlNode();
            return alertCommand;
        }

        private static XElement CreateXmlNodeForAlertLevelRised(this Account account)
        {
            XElement result = new XElement("Alert");
            var accountNode = account.ToXml();
            result.Add(accountNode);
            XElement transNode = new XElement("Transactions");
            foreach (Fund eachFund in account.SubFunds)
            {
                foreach (Transaction eachTran in eachFund.Trans)
                {
                    transNode.Add(eachTran.ToXml());
                }
            }
            accountNode.Add(transNode);
            return result;
        }

        internal static Command CreateAccountUpdateCommand(this Account account)
        {
            AccountUpdateCommand accountUpdateCommand = new AccountUpdateCommand();
            accountUpdateCommand.AccountID = account.Id;
            accountUpdateCommand.Content = account.ToXml(new XmlParameter(false, false, false)).ToXmlNode();
            return accountUpdateCommand;
        }

        internal static Command CreateAccountBalanceCommand(this Account account, Fund subFundBalanceChanged)
        {
            XmlDocument document = new XmlDocument();
            XmlNode updateContent = document.CreateElement("Update");
            XmlNode modifyChild = document.CreateElement("Modify");
            updateContent.AppendChild(modifyChild);
            XmlElement accountBalanceChild = document.CreateElement("AccountBalance");
            modifyChild.AppendChild(accountBalanceChild);

            accountBalanceChild.SetAttribute("AccountID", account.Id.ToString());
            accountBalanceChild.SetAttribute("CurrencyID", subFundBalanceChanged.CurrencyId.ToString());
            accountBalanceChild.SetAttribute("Balance", XmlConvert.ToString(subFundBalanceChanged.Balance));
            accountBalanceChild.SetAttribute("TotalPaidAmount", XmlConvert.ToString(subFundBalanceChanged.TotalPaidAmount));

            UpdateCommand command = new UpdateCommand();
            command.Content = updateContent;
            Logger.InfoFormat("CreateAccountBalanceCommand  {0}", updateContent.OuterXml);
            return command;
        }


        internal static List<Command> ToCommand(this OrderChange orderChange)
        {
            List<Command> result = new List<Command>();
            if (orderChange.ChangeType == Protocal.Commands.OrderChangeType.Placed || orderChange.ChangeType == Protocal.Commands.OrderChangeType.Placing)
            {
                PlaceCommand placeCommand = new PlaceCommand();
                placeCommand.InstrumentID = orderChange.InstrumentId;
                placeCommand.AccountID = orderChange.AccountId;
                placeCommand.IsAutoFill = orderChange.Source.IsAutoFill;

                XElement placeNode = new XElement("Place");
                placeNode.Add(((Transaction)orderChange.Source.Owner).ToXml());
                placeCommand.Content = placeNode.ToXmlNode();
                result.Add(placeCommand);
                Logger.InfoFormat("create place command {0}", placeCommand.Content.OuterXml);
            }
            else if (orderChange.ChangeType == Protocal.Commands.OrderChangeType.Canceled)
            {
                CancelCommand cancelCommand = new CancelCommand();
                cancelCommand.InstrumentID = orderChange.InstrumentId;
                cancelCommand.AccountID = orderChange.AccountId;
                cancelCommand.TransactionID = orderChange.TranId;
                if (orderChange.Source.CancelReason != null)
                {
                    cancelCommand.CancelReason = orderChange.Source.CancelReason.Value;
                    Logger.InfoFormat("Create cancel command, accountId = {0}, tranId={1}, cancelReason = {2}", cancelCommand.AccountID, cancelCommand.TransactionID, cancelCommand.CancelReason );
                }
                cancelCommand.ErrorCode = TransactionError.OK;

                result.Add(cancelCommand);
            }
            else if (orderChange.ChangeType == Protocal.Commands.OrderChangeType.Deleted)
            {
                //DeleteCommand deleteCommand = new DeleteCommand();
                //deleteCommand.AccountID = orderChange.Source.Owner.Owner.Id;
                //deleteCommand.InstrumentID = orderChange.Source.Owner.InstrumentId;
                //deleteCommand.Content = orderChange.CreateDeleteXml().ToXmlNode();
                //result.Add(deleteCommand);
                //Logger.InfoFormat("delete command content1 = {0}", deleteCommand.Content.OuterXml);

                DeleteCommand deleteCommand2 = new DeleteCommand();
                deleteCommand2.AccountID = orderChange.Source.Owner.Owner.Id;
                deleteCommand2.InstrumentID = orderChange.Source.Owner.InstrumentId;
                var root = orderChange.CreateDeleteXmlForTrader();
                XElement affectedOrderNode = new XElement("AffectedOrders");
                foreach (var eachTran in orderChange.AffectedTrans)
                {
                    affectedOrderNode.Add(eachTran.ToXml());
                }
                root.Add(affectedOrderNode);
                deleteCommand2.Content = root.ToXmlNode();
                result.Add(deleteCommand2);
                Logger.InfoFormat("delete command content2 = {0}", deleteCommand2.Content.OuterXml);
                //此处实现有问题，新系统如何通知删单时受影响的单，现在还不清楚
            }
            else if (orderChange.ChangeType == Protocal.Commands.OrderChangeType.Executed)
            {
                ExecuteCommand executeCommand = new ExecuteCommand();
                executeCommand.AccountID = orderChange.AccountId;
                executeCommand.InstrumentID = orderChange.InstrumentId;
                executeCommand.TranID = orderChange.TranId;
                executeCommand.Content = orderChange.CreateExecuteXml().ToXmlNode();
                result.Add(executeCommand);
                Logger.InfoFormat("create execute command {0}", executeCommand.Content.OuterXml);
            }
            else if (orderChange.ChangeType == Protocal.Commands.OrderChangeType.Cut)
            {
                result.Add(orderChange.CreateOrderCutCommand());
            }
            else if (orderChange.ChangeType == Protocal.Commands.OrderChangeType.Changed)
            {
                result.Add(orderChange.CreateOrderChangedCommand());
            }
            else if (orderChange.ChangeType == Protocal.Commands.OrderChangeType.Hit)
            {
                result.Add(orderChange.CreateHitCommand());
            }
            else
            {
                throw new NotSupportedException(string.Format("Change Type {0} is not supported yet", orderChange.ChangeType));
            }
            return result;
        }

        private static XElement CreateExecuteXml(this OrderChange orderChange)
        {
            XElement result = new XElement("Content");
            result.Add(orderChange.Account.ToXml());
            result.Add(orderChange.Tran.ToXml());
            return result;
        }


        internal static Command CreateHitCommand(this OrderChange orderChange)
        {
            HitCommand hitCommand = new HitCommand();
            hitCommand.Content = orderChange.CreateHitXml().ToXmlNode();
            Logger.WarnFormat("CreateHitCommand content = {0}", hitCommand.Content);
            return hitCommand;
        }

        internal static XElement CreateHitXml(this OrderChange orderChange)
        {
            XElement root = new XElement("Hit");
            Order order = (Order)orderChange.Source;
            XElement orderElement = new XElement("Order");
            root.Add(orderElement);
            orderElement.SetAttributeValue("ID", order.Id);
            orderElement.SetAttributeValue("AccountID", orderChange.AccountId);
            orderElement.SetAttributeValue("InstrumentID", orderChange.InstrumentId);
            orderElement.SetAttributeValue("HitCount", order.HitCount);
            orderElement.SetAttributeValue("BestPrice", order.BestPrice);
            orderElement.SetAttributeValue("BestTime", XmlConvert.ToString((new SqlDateTime(order.BestTime ?? DateTime.MinValue)).Value, DateTimeFormat.Xml));
            return root;
        }



        internal static XElement CreateDeleteXml(this OrderChange orderChange)
        {

            //此处实现有问题，新系统如何通知删单时受影响的单，现在还不清楚
            //XElement affectedOrdersNode = new XElement("AffectedOrders");
            //affectedOrdersNode.Add(orderChange.Tran.ToXml());
            //result.Add(affectedOrdersNode);
            return orderChange.CreateDeleteXMLCommon();
        }

        internal static XElement CreateDeleteXmlForTrader(this OrderChange orderChange)
        {
            var result = orderChange.CreateDeleteXMLCommon();
            result.Add(orderChange.Account.ToXml());
            return result;
        }

        private static XElement CreateDeleteXMLCommon(this OrderChange orderChange)
        {
            XElement result = new XElement("Delete");
            XElement deletedOrderNode = new XElement("DeletedOrder");
            result.Add(deletedOrderNode);
            deletedOrderNode.SetAttributeValue("ID", XmlConvert.ToString(orderChange.Source.Id));
            deletedOrderNode.SetAttributeValue("AccountID", XmlConvert.ToString(orderChange.AccountId));
            deletedOrderNode.SetAttributeValue("InstrumentID", XmlConvert.ToString(orderChange.InstrumentId));
            return result;
        }


        private static Command CreateOrderCutCommand(this OrderChange orderChange)
        {
            CutCommand cutCommand = new CutCommand();
            cutCommand.AccountID = orderChange.AccountId;
            cutCommand.Content = orderChange.CreateOrderCutXml().ToXmlNode();
            return cutCommand;
        }

        private static XElement CreateOrderCutXml(this OrderChange orderChange)
        {
            XElement result = new XElement("Cut");
            var accountNode = orderChange.Account.ToXml();
            result.Add(accountNode);

            XElement transNode = new XElement("Transactions");
            accountNode.Add(transNode);
            transNode.Add(orderChange.Tran.ToXml());
            return result;
        }

        private static Command CreateOrderChangedCommand(this OrderChange orderChange)
        {
            AccountUpdateCommand accountUpdateCommand = null;
            Order order = (Order)orderChange.Source;
            if ((order.Owner.InstrumentCategory == InstrumentCategory.Physical || order.LotBalance > 0) && order.Phase == OrderPhase.Executed && orderChange.PropertyType != PropertyChangeType.None)
            {
                XmlDocument xmlDoc = new XmlDocument();
                if (accountUpdateCommand == null)
                {
                    accountUpdateCommand = new AccountUpdateCommand();
                    accountUpdateCommand.AccountID = order.Account.Id;
                    accountUpdateCommand.Content = xmlDoc.CreateElement("Orders");
                }

                XmlElement orderNode = xmlDoc.CreateElement("Order");

                orderNode.SetAttribute("ID", XmlConvert.ToString(order.Id));
                if (orderChange.PropertyType.Contains(PropertyChangeType.AutoLimitPrice))
                {
                    string autoLimitPrice = (order.AutoLimitPrice == null ? "" : (string)order.AutoLimitPrice);
                    orderNode.SetAttribute("AutoLimitPrice", autoLimitPrice);
                }
                if (orderChange.PropertyType.Contains(PropertyChangeType.AutoStopPrice))
                {
                    string autoStopPrice = (order.AutoStopPrice == null ? "" : (string)order.AutoStopPrice);
                    orderNode.SetAttribute("AutoStopPrice", autoStopPrice);
                }
                if (orderChange.PropertyType.Contains(PropertyChangeType.PaidPledgeBalance))
                {
                    orderNode.SetAttribute("PaidPledgeBalance", XmlConvert.ToString(order.PaidPledgeBalance));
                }
                if (orderChange.PropertyType.Contains(PropertyChangeType.PaidPledge))
                {
                    orderNode.SetAttribute("PaidPledge", XmlConvert.ToString(order.PaidPledge));
                }
                if (orderChange.PropertyType.Contains(PropertyChangeType.HasOverdue))
                {
                    orderNode.SetAttribute("IsInstalmentOverdue", XmlConvert.ToString(order.IsInstalmentOverdue));
                }
                accountUpdateCommand.Content.AppendChild(orderNode);
            }
            return accountUpdateCommand;
        }
    }

}