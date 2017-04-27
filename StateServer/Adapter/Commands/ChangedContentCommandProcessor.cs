using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using iExchange.StateServer.Adapter.FaxEmailServices;
using log4net;

namespace iExchange.StateServer.Adapter.Commands
{
    public sealed class ChangedContentCommandProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ChangedContentCommandProcessor));

        private ICommandBroadcast _broadcast;
        private Token _token;
        private Mapper _mapper;

        internal ChangedContentCommandProcessor(Token token, ICommandBroadcast broadcast)
        {
            _broadcast = broadcast;
            _token = token;
            _mapper = new Mapper(broadcast);
            _mapper.Start();
        }

        internal void Process(Protocal.TradingCommand tradingCommand)
        {
            this.ProcessChangedContentForFaxEmail(tradingCommand.Content);
            this.UpdateAccount(tradingCommand);
        }


        private void ProcessChangedContentForFaxEmail(string content)
        {
            XElement contentNode = XElement.Parse(content);
            if (contentNode.ExistAndHasChildrenElments("DeliveryRequests"))
            {
                this.ProcessDeliveryReqeust(contentNode);
            }
        }

        private void ProcessDeliveryReqeust(XElement contentNode)
        {
            var deliverRequestElements = contentNode.Element("DeliveryRequests").Elements("DeliveryRequest");
            if (deliverRequestElements != null)
            {
                foreach (var eachDeliveryRequest in deliverRequestElements)
                {
                    Guid deliverRequestId = eachDeliveryRequest.Attribute("Id").Value.XmlToGuid();
                    FaxEmailServices.FaxEmailEngine.Default.NotifyApplyDelivery(deliverRequestId);
                }
            }
        }

        private void UpdateAccount(Protocal.TradingCommand tradingCommand)
        {
            var commands = this.CreateCommands(tradingCommand);
            Logger.InfoFormat("UpdateAccount commands count = {0}", commands.Count);
            if (commands.Count > 0)
            {
                _broadcast.BroadcastCommands(_token, commands.ToArray());
            }
        }


        internal List<Command> CreateCommands(Protocal.TradingCommand tradingCommand)
        {
            List<Command> commands = new List<Command>();
            XElement accountNode = XElement.Parse(tradingCommand.Content);
            Protocal.Commands.Account account = null;
            if (AccountRepository.Default.TryGet(tradingCommand.AccountId, out account))
            {
                List<Protocal.Commands.OrderPhaseChange> orderChanges = null;
                commands = ((Account)account).UpdateAndCreateCommand(accountNode, out orderChanges);
                Protocal.Commands.TransactionMapping.Default.Update((Account)account, orderChanges);
                this.NotifyFaxEmalEngine(orderChanges);
                this.ProcessExecutedChanges(orderChanges);
            }
            return commands;
        }

        private void ProcessExecutedChanges(List<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            try
            {
                Dictionary<Guid, Transaction> trans = new Dictionary<Guid, Transaction>();
                foreach (var eachChange in orderChanges)
                {
                    if (eachChange.ChangeType == Protocal.Commands.OrderChangeType.Executed)
                    {
                        var tran = eachChange.Source.Owner;
                        if (!trans.ContainsKey(tran.Id))
                        {
                            trans.Add(tran.Id, (Transaction)tran);
                        }
                    }
                }
                foreach (var eachTran in trans.Values)
                {
                   _mapper.Add(eachTran.ToXml().ToXmlNode());
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }


        private void NotifyFaxEmalEngine(IEnumerable<Protocal.Commands.OrderPhaseChange> orderChanges)
        {
            try
            {
                if (orderChanges == null) return;
                foreach (var eachChange in orderChanges)
                {
                    if (eachChange.ChangeType == Protocal.Commands.OrderChangeType.Executed)
                    {
                        Logger.InfoFormat("Notify execute orderID = {0}", eachChange.Source.Id);
                        FaxEmailEngine.Default.NotifyExecution(eachChange.Source.Id);
                    }
                    else if (eachChange.ChangeType == Protocal.Commands.OrderChangeType.Deleted)
                    {
                        Logger.InfoFormat("Notify Deleted orderID = {0}", eachChange.Source.Id);
                        FaxEmailEngine.Default.NotifyOrderDeleted(eachChange.Source.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


    }
}