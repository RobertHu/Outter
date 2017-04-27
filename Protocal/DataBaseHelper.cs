using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Protocal
{
    public class DBParameter
    {
        public DBParameter(string key, object value, ParameterDirection direction = ParameterDirection.Input)
        {
            this.Key = key;
            this.Value = value;
            this.Direction = direction;
        }

        public string Key { get; private set; }
        public object Value { get; private set; }
        public ParameterDirection Direction { get; private set; }
        public object OutPutValue { get; set; }
        public SqlDbType DbType { get; set; }
    }


    public static class DataBaseHelper
    {
        public static DataSet GetData(string sql, string connectionString, string[] tableNames, Dictionary<string, object> sqlParams)
        {
            var paramList = Convert(sqlParams);
            return GetData(sql, connectionString, tableNames, paramList);
        }

        public static DataSet GetData(string sql, string connectionString, string[] tableNames, List<DBParameter> sqlParams)
        {
            DataSet result = GetDataHelper(sql, connectionString, sqlParams);
            if (result.Tables.Count == 0) return result;
            CheckTableNames(result, tableNames);
            SetTableNames(result, tableNames);
            return result;
        }

        public static SqlDataReader GetReader(string sql, string connectionString, Dictionary<string, object> sqlParams)
        {
            var paramList = Convert(sqlParams);
            return GetReader(sql, connectionString, paramList);
        }

        public static SqlDataReader GetReader(string sql, string connectionString, List<DBParameter> sqlParams)
        {
            SqlCommand command = CreateCommand(sql, connectionString, sqlParams);
            return command.ExecuteReader();
        }

        public static bool ExecuteNonQuery(string sql, string connectionString, Dictionary<string, object> sqlParams)
        {
            var dbParameters = Convert(sqlParams);
            return ExecuteNonQuery(sql, connectionString, dbParameters);
        }

        public static bool ExecuteNonQuery(string sql, string connectionString, List<DBParameter> dbParameters)
        {
            var command = CreateCommand(sql, connectionString, dbParameters);
            int affectedRows = command.ExecuteNonQuery();
            FillOutputSqlParameter(dbParameters, command);
            command.Connection.Close();
            return affectedRows != -1;
        }



        private static void SetTableNames(DataSet data, string[] tableNames)
        {
            if (tableNames == null) return;
            for (int i = 0; i < data.Tables.Count; i++)
            {
                data.Tables[i].TableName = tableNames[i];
            }
        }

        private static List<DBParameter> Convert(Dictionary<string, object> sqlParams)
        {
            if (sqlParams == null) return null;
            List<DBParameter> paramList = new List<DBParameter>();
            foreach (var eachParamPair in sqlParams)
            {
                Debug.WriteLine(string.Format("key = {0}, value={1}", eachParamPair.Key, eachParamPair.Value));
                DBParameter item = new DBParameter(eachParamPair.Key, eachParamPair.Value, ParameterDirection.Input);
                paramList.Add(item);
            }
            return paramList;
        }

        private static DataSet GetDataHelper(string sql, string connectionString, List<DBParameter> sqlParams)
        {
            var command = CreateCommand(sql, connectionString, sqlParams);
            var adapter = new SqlDataAdapter(command);
            var result = new DataSet();
            adapter.Fill(result);
            FillOutputSqlParameter(sqlParams, command);
            command.Connection.Close();
            return result;
        }

        private static SqlCommand CreateCommand(string sql, string connectionString, List<DBParameter> sqlParams)
        {
            SqlCommand command = new SqlCommand(sql);
            if (sqlParams != null)
            {
                FillSqlParameter(sqlParams, command);
            }
            command.Connection = new SqlConnection(connectionString);
            command.Connection.Open();
            command.CommandTimeout = (int)TimeSpan.FromMinutes(20).TotalMilliseconds;
            return command;
        }


        private static void FillOutputSqlParameter(List<DBParameter> sqlParams, SqlCommand command)
        {
            if (sqlParams == null) return;
            foreach (var eachParam in sqlParams)
            {
                if (eachParam.Direction == ParameterDirection.Output)
                {
                    Debug.WriteLine(string.Format("fill output value, key={0}", eachParam.Key));
                    eachParam.OutPutValue = command.Parameters[eachParam.Key].Value;
                }
            }
        }

        private static void FillSqlParameter(List<DBParameter> sqlParams, SqlCommand command)
        {
            command.CommandType = System.Data.CommandType.StoredProcedure;
            foreach (var eachParam in sqlParams)
            {
                Debug.WriteLine(string.Format("key = {0}, value={1}", eachParam.Key, eachParam.Value));
                if (eachParam.Direction == ParameterDirection.Output)
                {
                    SqlParameter sqlParameter = new SqlParameter(eachParam.Key, eachParam.DbType)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(sqlParameter);
                }
                else
                {
                    command.Parameters.AddWithValue(eachParam.Key, eachParam.Value);
                }
            }
        }


        private static void CheckTableNames(DataSet data, string[] tableNames)
        {
            if (tableNames == null) return;
            if (data.Tables.Count < tableNames.Length)
            {
                throw new TransactionServerException(TransactionError.DatabaseDataIntegralityViolated, string.Format("data's table count = {0}, tableNames.count={1}", data.Tables.Count, tableNames.Length));
            }
        }

    }

    public class TransactionServerException : Exception
    {
        public TransactionServerException(iExchange.Common.TransactionError errorCode, string errorDetail = "")
            : base(string.Format("errorCode= {0}, errorDetails={1}", errorCode, errorDetail))
        {
            this.ErrorCode = errorCode;
            this.ErrorDetail = errorDetail;
        }

        public iExchange.Common.TransactionError ErrorCode { get; private set; }
        public string ErrorDetail { get; private set; }


    }

}
