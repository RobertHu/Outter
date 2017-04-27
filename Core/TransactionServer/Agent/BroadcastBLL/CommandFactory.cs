using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BroadcastBLL
{
    internal static class CommandFactory
    {
        internal static Protocal.TradingCommand CreateBookCommand(Guid accountId, string content)
        {
            return new Protocal.TradingCommand
            {
                Content = content,
                AccountId = accountId,
                IsBook = true
            };
        }


        internal static Protocal.TradingCommand CreateUpdateBalanceCommand(Guid accountId, Guid currencyId, decimal balance, Protocal.ModifyType type)
        {
            return new Protocal.Commands.TradingUpdateBalanceCommand
            {
                AccountId = accountId,
                ModifyType = type,
                CurrencyId = currencyId,
                Balance = balance
            };
        }

        internal static Protocal.TradingCommand CreatePrePaymenCommand(Guid accountId, Guid currencyId, decimal balance, decimal totalPaidAmount)
        {
            return new Protocal.Commands.TradingPrePaymentCommand
            {
                AccountId = accountId,
                CurrencyId = currencyId,
                Balance = balance,
                TotalPaidAmount = totalPaidAmount
            };
        }

        internal static Protocal.TradingCommand CreateTradingTransferCommand(Guid transferId, Guid remitterId, Guid payeeId, TransferAction action)
        {
            return new Protocal.Commands.TradingTransferCommand
            {
                TransferId = transferId,
                RemitterId = remitterId,
                PayeeId = payeeId,
                Action = action
            };
        }

        internal static Protocal.TradingCommand CreateExecuteCommand(Guid accountId, Guid instrumentId, Guid tranId)
        {
            return new Protocal.Commands.TradingExecuteCommand
            {
                AccountId = accountId,
                InstrumentId = instrumentId,
                TransactionId = tranId
            };
        }

        internal static Protocal.TradingCommand CreateContentChangedCommand(Guid accountId, string content)
        {
            Protocal.TradingCommand command = new Protocal.TradingCommand();
            command.Content = content;
            command.AccountId = accountId;
            return command;
        }

        internal static Protocal.TradingCommand CreateHitCommand(Guid accountId, Guid orderId)
        {
            return new Protocal.Commands.TradingHitCommand
            {
                AccountId = accountId,
                OrderId = orderId
            };
        }


        internal static Protocal.TradingCommand CreateAcceptPlaceCommand(Guid accountId, Guid tranId, Guid instrumentId)
        {
            return new Protocal.Commands.TradingAcceptPlaceCommand
            {
                AccountId = accountId,
                TransactionId = tranId,
                InstrumentId = instrumentId
            };
        }


        internal static Protocal.TradingCommand CreatePriceAlertCommand(IEnumerable<PriceAlert.Alert> alerts, Protocal.Commands.AlertType alertType)
        {
            Dictionary<Guid, Protocal.Commands.UserPriceAlertData> alertDict = new Dictionary<Guid, Protocal.Commands.UserPriceAlertData>();
            foreach (var eachAlert in alerts)
            {
                eachAlert.Process(alertDict, alertType);
            }
            return new Protocal.Commands.TradingPriceAlertCommand
            {
                Type = alertType,
                UserPriceAlerts = alertDict.Values.ToList()
            };
        }


        private static void Process(this PriceAlert.Alert alert, Dictionary<Guid, Protocal.Commands.UserPriceAlertData> alertDict, Protocal.Commands.AlertType alertType)
        {
            Protocal.Commands.UserPriceAlertData userPriceAlerts;
            if (!alertDict.TryGetValue(alert.UserId, out userPriceAlerts))
            {
                userPriceAlerts = new Protocal.Commands.UserPriceAlertData
                {
                    UserId = alert.UserId,
                    PriceAlerts = new List<Protocal.Commands.PriceAlertData>()
                };
                alertDict.Add(userPriceAlerts.UserId, userPriceAlerts);
            }
            userPriceAlerts.PriceAlerts.Add(alert.CreatePriceAlertData(alertType));
        }




        private static Protocal.Commands.PriceAlertData CreatePriceAlertData(this PriceAlert.Alert alert, Protocal.Commands.AlertType alertType)
        {
            return new Protocal.Commands.PriceAlertData
            {
                Id = alert.Id,
                State = alert.State,
                HitPrice = alertType == Protocal.Commands.AlertType.Hit ? (string)alert.HitPrice : null,
                HitTime = alertType == Protocal.Commands.AlertType.Hit ? alert.HitPriceTimestamp : (DateTime?)null
            };
        }


        internal static Protocal.SettingCommand CreateCurrencyRateUpdateCommand(string content)
        {
            return new Protocal.SettingCommand
            {
                Content = content,
                SourceType = AppType.TransactionServer
            };
        }


    }
}
