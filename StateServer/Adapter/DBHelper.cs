using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.SqlClient;
using System.Data;
using iExchange.Common;
using log4net;
using System.Xml;

namespace iExchange.StateServer.Adapter
{
    internal static class DBHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DBHelper));

        private static readonly TimeSpan _retryBaseTime = TimeSpan.FromSeconds(1);

        private static string _connectionString;

        internal static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }

        internal static void ResetDBAlertLevel(Guid userId, Guid accountId)
        {
            TryResetDBAlertLevel(userId, accountId, 0);
        }
        private static void TryResetDBAlertLevel(Guid userId, Guid accountID, int retryTime)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                SqlCommand command = sqlConnection.CreateCommand();
                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = "dbo.P_ResetAlertLevel";
                command.Parameters.Add("@RETURN_VALUE", SqlDbType.Int);
                command.Parameters["@RETURN_VALUE"].Direction = ParameterDirection.ReturnValue;
                command.Parameters.AddWithValue("@userID", userId);
                command.Parameters.AddWithValue("@accountID", accountID);

                //Execute
                try
                {
                    sqlConnection.Open();
                    command.ExecuteNonQuery();
                }
                catch (SqlException sqlException)
                {
                    if (retryTime < 3 && SqlHelper.IsRepeatalbeOperation(sqlException))
                    {
                        SqlHelper.SleepBeforeRetry(TimeSpan.FromSeconds(1), retryTime++);
                        Logger.Error(string.Format("ReTryResetDBAlertLevel {0}", retryTime));
                        TryResetDBAlertLevel(userId, accountID, retryTime);
                    }
                    else
                    {
                        Logger.Error(string.Format("TryResetDBAlertLevel {0} failed at\r\n{1}", accountID, sqlException));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(string.Format("TryResetDBAlertLevel {0} failed at\r\n{1}", accountID, e));
                }
            }
        }

        internal static void UpdateDBAlertHistory(Guid accountId, XmlNode alertDbNode)
        {
            TryUpdateDBAlertHistory(accountId, alertDbNode, 0);
        }

        private static void TryUpdateDBAlertHistory(Guid accountID, XmlNode alertDbNode, int retryTime)
        {
            using (SqlConnection sqlConnection = new SqlConnection(_connectionString))
            {
                SqlCommand command = sqlConnection.CreateCommand();

                command.CommandType = System.Data.CommandType.StoredProcedure;
                command.CommandText = "dbo.P_UpdateAlertHistory";
                command.Parameters.AddWithValue("@xmlAlert", alertDbNode.OuterXml);

                //Execute
                try
                {
                    sqlConnection.Open();
                    command.ExecuteNonQuery();
                }
                catch (SqlException sqlException)
                {
                    if (retryTime < 3 && SqlHelper.IsRepeatalbeOperation(sqlException))
                    {
                        SqlHelper.SleepBeforeRetry(_retryBaseTime, retryTime++);
                        Logger.Warn(string.Format("ReTryUpdateDBAlertHistory {0}", retryTime));
                        TryUpdateDBAlertHistory(accountID, alertDbNode, retryTime);
                    }
                    else
                    {
                        Logger.Error(string.Format("TryUpdateDBAlertHistory {0} failed at\r\n{1}", alertDbNode.ToString(), sqlException));
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(string.Format("TryUpdateDBAlertHistory {0} failed at\r\n{1}", alertDbNode.ToString(), e));
                }
            }
        }

    }
}