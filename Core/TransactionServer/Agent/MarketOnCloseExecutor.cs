using Core.TransactionServer;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Agent.BLL;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent
{
    internal sealed class MarketOnCloseExecutor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(MarketOnCloseExecutor));

        private string _transactionServerId;
        private string _connectionString;

        internal MarketOnCloseExecutor(string transactionServerId, string connectionString)
        {
            _transactionServerId = transactionServerId;
            _connectionString = connectionString;
        }

        internal TransactionError Execute(ExecuteContext context)
        {
            TransactionError error = TransactionError.OK;
            var account = TradingSetting.Default.GetAccount(context.AccountId);
            var tran = account.GetTran(context.TranId);
            try
            {
                DataSet historyData = this.LoadHistorySettings(tran);
                this.ReplaceOrder(historyData, false, account);
                //account.Execute(e);
                this.ReplaceOrder(historyData, true, account);
                //SettingFacade.Default.SettingManager.RemoveHistorySettings(e.Account.Id);
            }
            catch (TransactionServerException tranException)
            {
                Logger.Error(tranException.ToString());
                error = tranException.ErrorCode;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                error = TransactionError.RuntimeError;
            }
            if (error != TransactionError.OK)
            {
                account.CancelExecute(context, error);
            }
            return error;

        }

        private DataSet LoadHistorySettings(Transaction tran)
        {
            var accountId = tran.Owner.Id;
            DataSet data = this.GetData(_transactionServerId, _connectionString, accountId, tran.InstrumentId, tran.ExecuteTime.Value);
            this.ReplaceOrder(data, false, tran.Owner);
            // SettingFacade.Default.SettingManager.AddHistorySettings(accountId, data);
            return data;
        }

        private DataSet GetData(string transactionServerID, string connectionString, Guid accountID, Guid instrumentID, DateTime endTime)
        {
            InitCommand initCommand;
            initCommand = new InitCommand();
            initCommand.Command = new SqlCommand("dbo.P_GetInitDataForTransactionServer2");
            initCommand.Command.CommandType = System.Data.CommandType.StoredProcedure;

            initCommand.Command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
            initCommand.Command.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;

            initCommand.Command.Parameters.AddWithValue("@transactionServerID", transactionServerID);
            initCommand.Command.Parameters.AddWithValue("@accountID", accountID);
            initCommand.Command.Parameters.AddWithValue("@instrumentID", instrumentID);
            initCommand.Command.Parameters.AddWithValue("@endTime", endTime);
            initCommand.Command.Parameters.AddWithValue("@getEndTimeQuotation", 0);

            initCommand.TableNames = new string[]{
												   "TradeDay",
												   "Currency",
												   "CurrencyRate",
												   "Instrument",
												   "TradePolicy",
												   "TradePolicyDetail",
                                                   "VolumeNecessary",
												   "VolumeNecessaryDetail",
												   "Customer",
												   "Account",
												   "OriginQuotation",
												   "OverridedQuotation",
												   "Order"
											   };

            //data set
            SqlCommand command = initCommand.Command;
            command.Connection = new SqlConnection(connectionString);

            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            dataAdapter.SelectCommand = command;
            DataSet dataSet = new DataSet();
            dataAdapter.Fill(dataSet);

            int result = (int)(command.Parameters["@RETURN_VALUE"].Value);
            if (result == (int)TransactionError.ExecuteTimeMustBeInTradingTime)
            {
                throw new TransactionServerException(TransactionError.ExecuteTimeMustBeInTradingTime);
            }
            else if (result == (int)TransactionError.AccountIsNotTrading)
            {
                throw new TransactionServerException(TransactionError.AccountIsNotTrading);
            }
            else if (result == (int)TransactionError.InstrumentIsNotAccepting)
            {
                throw new TransactionServerException(TransactionError.InstrumentIsNotAccepting);
            }

            if (dataSet.Tables.Count < initCommand.TableNames.Length)
            {
                string tableName = initCommand.TableNames[dataSet.Tables.Count - 1];
                string logErrorMessage = string.Format("GetData: Failed to get parameters [{0}], please run the following sql to check !\r\n Exec dbo.P_GetInitDataForTransactionServer2 '{1}','{2}','{3}','{4}'", tableName, transactionServerID, accountID, instrumentID, endTime);
                Logger.Error(logErrorMessage);
                throw new TransactionServerException(TransactionError.DatabaseDataIntegralityViolated);
            }

            //modify table name
            string[] tableNames = initCommand.TableNames;
            for (int i = 0; i < dataSet.Tables.Count; i++)
            {
                dataSet.Tables[i].TableName = tableNames[i];
            }

            return dataSet;
        }

        private void ReplaceOrder(DataSet dataSet, bool isCurrent, Core.TransactionServer.Agent.Account account)
        {
            //?? Order
            var dataRows = dataSet.Tables["Order"].Rows;
            foreach (DataRow orderRow in dataRows)
            {
                Guid orderID = (Guid)orderRow["ID"];
                Order order = account.GetOrder(orderID);
                if (isCurrent)
                {
                    //Restore
                    //var interestPerLot = (decimal)orderRow["InterestPerLot"];
                    //var storagePerLot = (decimal)orderRow["StoragePerLot"];
                    //order.FeeSettings.Update(interestPerLot, storagePerLot);
                }
                else
                {
                    //Backup --Used to calculate OrderRelation.interestPL & OrderRelation.storagePL when it closed by the orders in current request.
                    //orderRow["InterestPerLot"] = order.FeeSettings.InterestPerLot;
                    //orderRow["StoragePerLot"] = order.FeeSettings.StoragePerLot;
                    //var interestPerLot = (decimal)orderRow["LastInterestPerLot"];
                    //var storagePerLot = (decimal)orderRow["LastStoragePerLot"];
                    //order.FeeSettings.Update(interestPerLot, storagePerLot);
                }
            }
        }

        private void AssertFilledTran(Transaction tran)
        {
            bool hasError = false;
            foreach (Order order in tran.Orders)
            {
                if ((order.Phase == OrderPhase.Executed || order.Phase == OrderPhase.Completed)
                    && (order.Code == null || tran.ExecuteTime == default(DateTime) || tran.Code == null))
                {
                    hasError = true;
                    order.Cancel(CancelReason.OtherReason);
                }
            }

            if (hasError)
            {
                //string xmlTran = tran.GetExecuteXmlString(); //tran.GetXml("Execute.xslt").OuterXml;
                //var msg = string.Format("AssertTran {0} failed", xmlTran);
                //Logger.Error(msg);
                throw new TransactionServerException(TransactionError.TransactionStateViolated);
            }
        }
    }
}
