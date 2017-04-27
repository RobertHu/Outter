using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using System.Collections.Concurrent;
using iExchange.Common;
using System.Xml.Linq;
using System.Diagnostics;
using Protocal.TypeExtensions;
using iExchange.StateServer.Adapter.FaxEmailServices;
using Protocal.Commands;
using iExchange.StateServer.Adapter.Commands;
using log4net;
using System.Data.SqlClient;

namespace iExchange.StateServer.Adapter
{
    internal static class XElementExtension
    {
        internal static bool ExistAndHasChildrenElments(this XElement node, string name)
        {
            return node.Element(name) != null && node.Element(name).HasElements;
        }
    }

    internal sealed class CommandManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(CommandManager));
        internal static readonly CommandManager Default = new CommandManager();
        private ICommandBroadcast _broadcast;
        private string _connectionString;
        private readonly Token _token = new Token(Guid.Empty, UserType.System, AppType.TransactionServer);
        private ChangedContentCommandProcessor _changedContentCommandProcessor;
        private TransactionAdaptor _transactionAdaptor;

        static CommandManager() { }
        private CommandManager() { }

        internal void Initialize(ICommandBroadcast broadcast, string connectionString, TransactionAdaptor transactionAdaptor)
        {
            _broadcast = broadcast;
            _connectionString = connectionString;
            _transactionAdaptor = transactionAdaptor;
            _changedContentCommandProcessor = new ChangedContentCommandProcessor(_token, _broadcast);
        }

        internal void ProcessTradingCommand(Protocal.TradingCommand tradingCommand)
        {
            try
            {
                Logger.InfoFormat("ProcessTradingCommand, content= {0}", tradingCommand.Content);
                this.GetInitDataForAccount(tradingCommand);
                this.ProcessNormalTradingCommand(tradingCommand);

            }
            catch (Exception ex)
            {
                Logger.Error(tradingCommand.Content, ex);
            }
        }

        private void GetInitDataForAccount(Protocal.TradingCommand command)
        {
            if (string.IsNullOrEmpty(command.Content)) return;
            if (!AccountRepository.Default.Contains(command.AccountId))
            {
                _transactionAdaptor.GetAccountsForInit(new Guid[] { command.AccountId });
            }
        }

        private void ProcessNormalTradingCommand(Protocal.TradingCommand tradingCommand)
        {
            if (!string.IsNullOrEmpty(tradingCommand.Content))
            {
                _changedContentCommandProcessor.Process(tradingCommand);
            }
            else
            {
                var commandProcessor = this.GetCommandProcessor(tradingCommand);
                if (commandProcessor != null)
                {
                    _broadcast.BroadcastCommands(_token, commandProcessor.Process());
                }
            }
        }


        internal void BroadcastSettingCommand(string content)
        {
            if (string.IsNullOrEmpty(content)) return;
            UpdateCommand command = new UpdateCommand();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(content);
            command.Content = doc.DocumentElement;
            _broadcast.BroadcastCommands(_token, new Command[] { command });
        }


        internal ICommandProcessor GetCommandProcessor(Protocal.TradingCommand tradingCommand)
        {
            if (tradingCommand is TradingUpdateBalanceCommand)
            {
                return new UpdateBalanceCommandProcessor((TradingUpdateBalanceCommand)tradingCommand);
            }
            else if (tradingCommand is TradingTransferCommand)
            {
                return new TransferCommandProcessor((TradingTransferCommand)tradingCommand);
            }
            else if (tradingCommand is TradingExecuteCommand)
            {
                return new ExecuteCommandProcessor((TradingExecuteCommand)tradingCommand);
            }
            else if (tradingCommand is TradingAcceptPlaceCommand)
            {
                return new AcceptPlaceCommandProcessor((TradingAcceptPlaceCommand)tradingCommand);
            }
            else if (tradingCommand is TradingPrePaymentCommand)
            {
                return new PrePaymentCommandProcessor((TradingPrePaymentCommand)tradingCommand);
            }
            else if (tradingCommand is TradingCancelByManagerCommand)
            {
                return new CancelCommandProccessor((TradingCancelByManagerCommand)tradingCommand);
            }
            else
            {
                return null;
            }
        }


        internal XmlNode FillAndGetInitData(string initData)
        {
            XElement result = new XElement("Accounts");
            var nodes = this.CreateAccountNodes(initData);
            foreach (var eachNode in nodes)
            {
                result.Add(eachNode);
            }
            return result.ToXmlNode();
        }

        private List<XElement> CreateAccountNodes(string initData)
        {
            try
            {
                List<XElement> result = new List<XElement>();
                XElement sourceElement = XElement.Parse(initData);
                foreach (XElement eachAccountElement in sourceElement.Elements("Account"))
                {
                    result.Add(this.CreateAccountNode(eachAccountElement));
                }
                return result;
            }
            catch
            {
                Logger.ErrorFormat("CreateAccountNodes initData = {0}", initData);
                throw;
            }
        }

        private XElement CreateAccountNode(XElement accountElement)
        {
            Guid accountId = Guid.Parse(accountElement.Attribute("ID").Value);
            if (AccountRepository.Default.Contains(accountId))
            {
                AccountRepository.Default.Remove(accountId);
            }
            var account = (Account)AccountRepository.Default.GetOrAdd(accountId);
            XElement initResult = account.InitializeAndGetXml(accountElement);
            TransactionMapping.Default.Initialize(account);
            return initResult;
        }

        internal void ProcessMarketCommand(Protocal.MarketCommand marketCommand)
        {
            try
            {
                Logger.InfoFormat("ProcessMarketCommand, type= {0}", marketCommand.GetType().Name);
                if (marketCommand is Protocal.UpdateInstrumentTradingStatusMarketCommand)
                {
                    List<InstrumentUpdateStatusCommand> broadcastTask = new List<InstrumentUpdateStatusCommand>();
                    Protocal.UpdateInstrumentTradingStatusMarketCommand updateMarketCommand = marketCommand as Protocal.UpdateInstrumentTradingStatusMarketCommand;
                    Logger.InfoFormat("ProcessMarketCommand, content= {0}", updateMarketCommand);

                    foreach (var instrumentStatus in updateMarketCommand.InstrumentStatus)
                    {
                        foreach (var instrument in instrumentStatus.Value)
                        {
                            InstrumentStatus sendInstrumentStatus = InstrumentStatus.None;
                            switch (instrumentStatus.Key)
                            {
                                case Protocal.TradingInstrument.InstrumentStatus.DayOpen:
                                    sendInstrumentStatus = InstrumentStatus.DayOpen;
                                    break;

                                case Protocal.TradingInstrument.InstrumentStatus.DayClose:
                                    sendInstrumentStatus = InstrumentStatus.DayClose;
                                    break;

                                case Protocal.TradingInstrument.InstrumentStatus.SessionOpen:
                                    sendInstrumentStatus = InstrumentStatus.SessionOpen;
                                    break;

                                case Protocal.TradingInstrument.InstrumentStatus.SessionClose:
                                    sendInstrumentStatus = InstrumentStatus.SessionClose;
                                    break;

                                default:
                                    continue;
                            }

                            broadcastTask.Add(new InstrumentUpdateStatusCommand
                            {
                                InstrumentId = instrument.Id,
                                TradeDay = instrument.TradeDay == null ? DateTime.MaxValue : instrument.TradeDay.Value,
                                Status = sendInstrumentStatus
                            });
                        }
                    }

                    this._broadcast.BroadcastCommands(this._token, new InstrumentsUpdateStatusCommand[] { new InstrumentsUpdateStatusCommand() { commandCollection = broadcastTask } });

                    //Protocal.UpdateInstrumentTradingStatusMarketCommand
                    //    marketCloseCommand = marketCommand as Protocal.UpdateInstrumentTradingStatusMarketCommand;

                    //Logger.InfoFormat("ProcessMarketCommand, content= {0}", marketCloseCommand);

                    //if (marketCloseCommand.InstrumentStatus.ContainsKey(Protocal.TradingInstrument.InstrumentStatus.DayClose))
                    //{
                    //    List<InstrumentTradeDayCloseCommand> broadcastTask = new List<InstrumentTradeDayCloseCommand>();

                    //    foreach (var instrumentInfo in marketCloseCommand.InstrumentStatus[Protocal.TradingInstrument.InstrumentStatus.DayClose])
                    //    {
                    //        broadcastTask.Add(new InstrumentTradeDayCloseCommand
                    //        {
                    //            InstrumentId = instrumentInfo.Id,
                    //            TradeDay = instrumentInfo.TradeDay
                    //        });
                    //    }

                    //    Logger.InfoFormat("ProcessMarketCommand, callBrodcast.");
                    //    this._broadcast.BroadcastCommands(this._token, broadcastTask.ToArray());
                    //}
                }
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("ProcessMarketCommand failed at\r\n{0}", ex));
            }
        }
    }
}