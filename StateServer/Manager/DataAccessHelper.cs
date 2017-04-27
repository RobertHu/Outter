using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Data.SqlClient;

namespace iExchange.StateServer.Manager
{
    public class DataAccessHelper
    {
        public static SqlConnection GetSqlConnection()
        {
            string connectionString = System.Configuration.ConfigurationManager.AppSettings["ConnectionString"];
            SqlConnection sqlConnection = new SqlConnection(connectionString);
            sqlConnection.Open();
            return sqlConnection;
        }

        public static object ExecuteScalar(string sql, CommandType commandType, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = DataAccessHelper.GetSqlConnection())
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    foreach (SqlParameter parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                    command.CommandType = commandType;
                    return command.ExecuteScalar();
                }
            }
        }

        public static int ExecuteNonQuery(string sql, CommandType commandType, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = DataAccessHelper.GetSqlConnection())
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    foreach (SqlParameter parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                    command.CommandType = commandType;
                    return command.ExecuteNonQuery();
                }
            }
        }

        public static void ExecuteReader(string sql, CommandType commandType, Action<SqlDataReader> processData, params SqlParameter[] parameters)
        {
            using (SqlConnection connection = DataAccessHelper.GetSqlConnection())
            {
                using (SqlCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;
                    foreach (SqlParameter parameter in parameters)
                    {
                        command.Parameters.Add(parameter);
                    }
                    command.CommandType = commandType;
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        processData(reader);
                    }
                }
            }
        }
    }
}