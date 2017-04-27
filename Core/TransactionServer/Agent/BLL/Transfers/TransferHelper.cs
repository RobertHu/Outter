using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.Transfers
{
    internal static class TransferHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransferHelper));

        internal static bool ApplyTransferToDB(string dbConnectionString, Guid userID, Guid sourceAccountID, Guid sourceCurrencyID,
            decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount,
            decimal rate, DateTime expireDate, out Guid transferId)
        {
            transferId = Guid.NewGuid();
            try
            {
                using (SqlConnection connection = new SqlConnection(dbConnectionString))
                {
                    SqlCommand command = connection.CreateCommand();
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "P_ApplyTransfer";
                    command.Parameters.Add(new SqlParameter("@userID", userID));
                    command.Parameters.Add(new SqlParameter("@transferID", transferId));
                    command.Parameters.Add(new SqlParameter("@sourceAccountID", sourceAccountID));
                    command.Parameters.Add(new SqlParameter("@sourceCurrencyID", sourceCurrencyID));
                    command.Parameters.Add(new SqlParameter("@sourceAmount", sourceAmount));
                    command.Parameters.Add(new SqlParameter("@targetAccountID", targetAccountID));
                    command.Parameters.Add(new SqlParameter("@targetCurrencyID", targetCurrencyID));
                    command.Parameters.Add(new SqlParameter("@targetAmount", targetAmount));
                    command.Parameters.Add(new SqlParameter("@rate", rate));
                    command.Parameters.Add(new SqlParameter("@expireDate", expireDate));

                    SqlParameter parameter = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
                    parameter.Direction = System.Data.ParameterDirection.ReturnValue;
                    command.Parameters.Add(parameter);

                    connection.Open();
                    command.ExecuteNonQuery();
                    int returnValue = (int)command.Parameters["@RETURN_VALUE"].Value;

                    return returnValue == 0;
                }

            }
            catch (Exception exception)
            {
                Logger.Error(exception);
                return false;
            }
        }

        internal static bool AcceptOrDeclineTransfer(string dbConnectionString, Guid userID, Guid transferID, TransferAction action,
            out decimal amount, out Guid currencyId, out Guid accountId)
        {
            amount = 0;
            currencyId = accountId = Guid.Empty;

            try
            {
                using (SqlConnection connection = new SqlConnection(dbConnectionString))
                {
                    SqlCommand command = connection.CreateCommand();
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = "P_AcceptOrDeclineTransfer";
                    command.Parameters.Add(new SqlParameter("@userID", userID));
                    command.Parameters.Add(new SqlParameter("@transferID", transferID));
                    command.Parameters.Add(new SqlParameter("@isAccept", action == TransferAction.Accept ? true : false));

                    SqlParameter parameter = new SqlParameter("@RETURN_VALUE", SqlDbType.Int);
                    parameter.Direction = System.Data.ParameterDirection.ReturnValue;
                    command.Parameters.Add(parameter);

                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            accountId = (Guid)reader["AccountID"];
                            currencyId = (Guid)reader["CurrencyID"];
                            amount = (decimal)reader["Amount"];

                            return true;
                        }
                    }
                }

            }
            catch (Exception exception)
            {
                Logger.Error(exception);
            }

            return false;
        }

        //internal static UpdateCommand ToUpdateCommand(Guid accountID, Guid currencyID, decimal amount)
        //{
        //    XmlDocument document = new XmlDocument();
        //    XmlElement updateNode = document.CreateElement("Update");

        //    XmlElement modifyNode = document.CreateElement("Add");
        //    updateNode.AppendChild(modifyNode);

        //    XmlElement accountBalanceNode = document.CreateElement("AccountBalance");
        //    modifyNode.AppendChild(accountBalanceNode);

        //    accountBalanceNode.SetAttribute("AccountID", XmlConvert.ToString(accountID));
        //    accountBalanceNode.SetAttribute("CurrencyID", XmlConvert.ToString(currencyID));
        //    accountBalanceNode.SetAttribute("Balance", XmlConvert.ToString(amount));

        //    UpdateCommand updateCommand = new UpdateCommand();
        //    updateCommand.Content = updateNode;

        //    return updateCommand;
        //}

        //internal static TransferCommand ToTransferCommand(Guid transferId, Guid remitterId, Guid payeeId, TransferAction action)
        //{
        //    TransferCommand transferCommand = new TransferCommand();
        //    transferCommand.TransferId = transferId;
        //    transferCommand.Action = action;
        //    transferCommand.RemitterId = remitterId;
        //    transferCommand.PayeeId = payeeId;

        //    return transferCommand;
        //}

        internal static bool GetVisaAmount2(string connectionString, Guid accountID, Guid targetCurrencyID, out decimal notClearAmount)
        {
            notClearAmount = Decimal.Zero;
            try
            {
                SqlConnection sqlConnection = new SqlConnection(connectionString);
                sqlConnection.Open();

                SqlCommand sqlCommand = new SqlCommand("dbo.P_GetVisaAmount2", sqlConnection);
                sqlCommand.CommandType = CommandType.StoredProcedure;
                SqlParameter sqlParameter = sqlCommand.Parameters.Add("@accountID", SqlDbType.UniqueIdentifier);
                sqlParameter.Value = accountID;
                sqlParameter = sqlCommand.Parameters.Add("@targetCurrencyID", SqlDbType.UniqueIdentifier);
                sqlParameter.Value = targetCurrencyID;
                sqlParameter = sqlCommand.Parameters.Add("@notClearAmount", SqlDbType.Money);
                sqlParameter.Direction = ParameterDirection.Output;

                sqlCommand.ExecuteNonQuery();

                notClearAmount = (decimal)sqlCommand.Parameters["@notClearAmount"].Value;

                sqlConnection.Close();
            }
            catch (Exception exception)
            {
                Logger.ErrorFormat("error: {0} \r\n GetVisaAmount:{1},{2}", exception, accountID, targetCurrencyID);

                return false;
            }

            return true;
        }
    }
}
