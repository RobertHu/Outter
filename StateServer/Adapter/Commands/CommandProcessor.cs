using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iExchange.Common;
using Protocal.Commands;
using iExchange.StateServer.Adapter.FaxEmailServices;
using System.Xml;

namespace iExchange.StateServer.Adapter.Commands
{
    internal interface ICommandProcessor
    {
        Command[] Process();
    }

    internal abstract class CommandProcessor<T> : ICommandProcessor where T : Protocal.TradingCommand
    {
        protected T _command;

        protected CommandProcessor(T command)
        {
            _command = command;
        }
        public abstract Command[] Process();
    }

    internal sealed class UpdateBalanceCommandProcessor : CommandProcessor<TradingUpdateBalanceCommand>
    {
        internal UpdateBalanceCommandProcessor(TradingUpdateBalanceCommand command)
            : base(command)
        {
        }

        public override Command[] Process()
        {
            var tradingUpdateBalanceCommand = _command;
            var account = AccountRepository.Default.Get(tradingUpdateBalanceCommand.AccountId);
            var fund = account.GetFund(tradingUpdateBalanceCommand.CurrencyId);
            FaxEmailEngine.Default.NotifyBalanceChanged((Account)account, DateTime.Now, fund.CurrencyCode, tradingUpdateBalanceCommand.Balance);
            var result = this.CreateUpdateCommand(tradingUpdateBalanceCommand.AccountId, tradingUpdateBalanceCommand.CurrencyId, tradingUpdateBalanceCommand.Balance);
            return new Command[] { result };
        }

        private Command CreateUpdateCommand(Guid accountID, Guid currencyID, decimal amount)
        {
            XmlDocument document = new XmlDocument();
            XmlElement updateNode = document.CreateElement("Update");

            XmlElement modifyNode = document.CreateElement("Add");
            updateNode.AppendChild(modifyNode);

            XmlElement accountBalanceNode = document.CreateElement("AccountBalance");
            modifyNode.AppendChild(accountBalanceNode);

            accountBalanceNode.SetAttribute("AccountID", XmlConvert.ToString(accountID));
            accountBalanceNode.SetAttribute("CurrencyID", XmlConvert.ToString(currencyID));
            accountBalanceNode.SetAttribute("Balance", XmlConvert.ToString(amount));

            UpdateCommand updateCommand = new UpdateCommand();
            updateCommand.Content = updateNode;
            return updateCommand;
        }
    }

    internal sealed class TransferCommandProcessor : CommandProcessor<TradingTransferCommand>
    {
        internal TransferCommandProcessor(TradingTransferCommand command)
            : base(command) { }

        public override Command[] Process()
        {
            TradingTransferCommand tradingTransferCommand = _command;
            TransferCommand transferCommand = new TransferCommand();
            transferCommand.TransferId = tradingTransferCommand.TransferId;
            transferCommand.Action = tradingTransferCommand.Action;
            transferCommand.RemitterId = tradingTransferCommand.RemitterId;
            transferCommand.PayeeId = tradingTransferCommand.PayeeId;
            return new Command[] { transferCommand };
        }
    }

    internal sealed class ExecuteCommandProcessor : CommandProcessor<TradingExecuteCommand>
    {
        internal ExecuteCommandProcessor(TradingExecuteCommand command)
            : base(command) { }

        public override Command[] Process()
        {
            var command = _command;
            Account account = (Account)AccountRepository.Default.Get(command.AccountId);
            Transaction tran = (Transaction)account.GetTran(command.TransactionId);
            ExecuteCommand executeCommand = new ExecuteCommand();
            executeCommand.AccountID = command.AccountId;
            executeCommand.InstrumentID = command.InstrumentId;
            executeCommand.TranID = command.TransactionId;

            XmlDocument xmlDoc = new XmlDocument();
            XmlElement content = xmlDoc.CreateElement("Content");
            executeCommand.Content = content;
            content.AppendChild(xmlDoc.ImportNode(account.ToXmlNode(), true));
            content.AppendChild(xmlDoc.ImportNode(tran.GetExecuteXmlElement(), true));
            return new Command[] { executeCommand };
        }
    }


    internal sealed class AcceptPlaceCommandProcessor : CommandProcessor<TradingAcceptPlaceCommand>
    {
        internal AcceptPlaceCommandProcessor(TradingAcceptPlaceCommand command)
            : base(command) { }

        public override Command[] Process()
        {
            var command = _command;
            AcceptPlaceCommand acceptPlaceCommand = new AcceptPlaceCommand
            {
                InstrumentID = command.InstrumentId,
                AccountID = command.AccountId,
                TransactionID = command.TransactionId
            };
            return new Command[] { acceptPlaceCommand };
        }
    }

    internal sealed class CancelCommandProccessor : CommandProcessor<TradingCancelByManagerCommand>
    {
        internal CancelCommandProccessor(TradingCancelByManagerCommand command)
            : base(command) { }

        public override Command[] Process()
        {
            CancelCommand cancelCommand = new CancelCommand();
            cancelCommand.InstrumentID = _command.InstrumentId;
            cancelCommand.AccountID = _command.AccountId;
            cancelCommand.TransactionID = _command.TransactionId;
            cancelCommand.CancelReason = _command.Reason;
            cancelCommand.ErrorCode = _command.ErrorCode;
            return new Command[] { cancelCommand };
        }
    }


    internal sealed class PrePaymentCommandProcessor : CommandProcessor<TradingPrePaymentCommand>
    {
        internal PrePaymentCommandProcessor(TradingPrePaymentCommand command)
            : base(command) { }

        public override Command[] Process()
        {
            XmlDocument document = new XmlDocument();
            XmlNode updateContent = document.CreateElement("Update");
            XmlNode modifyChild = document.CreateElement("Modify");
            updateContent.AppendChild(modifyChild);
            XmlElement accountBalanceChild = document.CreateElement("AccountBalance");
            modifyChild.AppendChild(accountBalanceChild);

            accountBalanceChild.SetAttribute("AccountID", _command.AccountId.ToString());
            accountBalanceChild.SetAttribute("CurrencyID", _command.CurrencyId.ToString());
            accountBalanceChild.SetAttribute("Balance", XmlConvert.ToString(_command.Balance));
            accountBalanceChild.SetAttribute("TotalPaidAmount", XmlConvert.ToString(_command.TotalPaidAmount));

            UpdateCommand command = new UpdateCommand();
            command.Content = updateContent;
            return new Command[] { command };
        }
    }

}