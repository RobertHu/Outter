using System;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using System.Configuration;
using System.Diagnostics;

using iExchange.Common;

namespace iExchange.StateServer
{
	/// <summary>
	/// Summary description for Verify.
	/// </summary>
	public class Checker
	{
		public Checker()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public static bool Hash()
		{
			try
			{
				byte[] hashValue = Checker.hashNames();

				string hashValue2="0x";
				foreach(byte b in hashValue)
				{
					hashValue2+=b.ToString("X2");
				}

				//save to db
				string connectionString=ConfigurationSettings.AppSettings["ConnectionString"];
				string sql="UPDATE SystemParameter SET SystemSign ="+hashValue2;
				SqlCommand command=new SqlCommand(sql,new SqlConnection(connectionString));
				command.Connection.Open();
				command.ExecuteNonQuery();
				command.Connection.Close();

				return true;
			}
			catch(Exception ex)
			{
				AppDebug.LogEvent("StateServer",ex.ToString(),EventLogEntryType.Error);
				throw ex;
			}
		}

		public static bool VerifyHash()
		{
			try
			{
				byte[] hashValue = Checker.hashNames();

				//get hash
				string connectionString=ConfigurationSettings.AppSettings["ConnectionString"];
				string sql="SELECT TOP 1 SystemSign,SystemExpireTime FROM SystemParameter";

				SqlDataAdapter sqlDataAdapter=new SqlDataAdapter(sql,connectionString);
				DataTable systemParameter=new DataTable();
				sqlDataAdapter.Fill(systemParameter);

				byte[] systemSign = (byte[])systemParameter.Rows[0]["SystemSign"];
				DateTime systemExpireTime = (DateTime)systemParameter.Rows[0]["SystemExpireTime"];

				//Compare byte to byte
				if(hashValue.Length!=systemSign.Length) return false;
				for(int i=0;i<hashValue.Length;i++)
				{
					if(hashValue[i]!=systemSign[i]) return false;
				}

				if(DateTime.Now > systemExpireTime) return false;

				return true;
			}
			catch(Exception ex)
			{
				AppDebug.LogEvent("StateServer",ex.ToString(),EventLogEntryType.Error);
				throw ex;
			}
		}

		private static byte[] hashNames()
		{
            string sql = "SELECT TOP 1 SystemExpireTime FROM SystemParameter;SELECT Name FROM Organization;SELECT Name FROM OrganizationName";
			string connectionString=ConfigurationSettings.AppSettings["ConnectionString"];

			//get all names of Organization
			SqlDataAdapter sqlDataAdapter=new SqlDataAdapter(sql,connectionString);
			sqlDataAdapter.TableMappings.Add("Table","SystemParameter");
			sqlDataAdapter.TableMappings.Add("Table1","Orgnization");
            sqlDataAdapter.TableMappings.Add("Table2", "OrgnizationName");
			
			DataSet hashData=new DataSet();
			sqlDataAdapter.Fill(hashData);
			
			string[] mix={
							 "213","derew","ewre","fegdf","23d5","dfgd","dfgdf","dfiin5",
							 "dfdfd","ghfg","56gt","hjl","89087","!#$%","(**&&","#$%&*",
							 "refvgb","43r","67yh","9088u","0--0[p","=-n","``````","/,m"
						 };

			DateTime SystemExpireTime=(DateTime)hashData.Tables["SystemParameter"].Rows[0]["SystemExpireTime"];

			string names=SystemExpireTime.ToString();
			
            int indexInMix = 0;
			foreach(DataRow dr in hashData.Tables["Orgnization"].Rows)
			{
                names += (string)dr["Name"] + mix[indexInMix++];
                if (indexInMix == mix.Length) indexInMix = 0;
			}

            indexInMix = 0;
            foreach (DataRow dr in hashData.Tables["OrgnizationName"].Rows)
            {
                if (dr["Name"] == DBNull.Value) continue;

                names += (string)dr["Name"] + mix[indexInMix++];
                if (indexInMix == mix.Length) indexInMix = 0;
            }

			//hash all names
			UnicodeEncoding ue = new UnicodeEncoding();
			byte[] messageBytes = ue.GetBytes(names);
			SHA1Managed hasher = new SHA1Managed();
			byte[] hashValue = hasher.ComputeHash(messageBytes);

			return hashValue;
		}


        private static bool _Initialized;
        private static object _InitailizeLock = new object();

        private static void InitializeSystemParameter()
        {
            if (_Initialized) return;

            lock (_InitailizeLock)
            {
                if (_Initialized) return;

                try
                {
                    string connectionString = ConfigurationSettings.AppSettings["ConnectionString"];

                    using (SqlConnection connection = new SqlConnection(connectionString))
                    {
                        SqlCommand sqlCommand = connection.CreateCommand();
                        sqlCommand.CommandType = CommandType.Text;
                        sqlCommand.CommandText = "SELECT AllowMultipleLogin FROM [SystemParameter]";
                        connection.Open();
                        SqlDataReader reader = sqlCommand.ExecuteReader();
                        reader.Read();

                        Checker.AllowMultipleLogin = (bool)reader["AllowMultipleLogin"];
                        reader.Close();
                    }
                    _Initialized = true;
                }
                catch (Exception exception)
                {
                    AppDebug.LogEvent("Checker", exception.ToString(), System.Diagnostics.EventLogEntryType.Error);
                }
            }
        }


        private static bool _AllowMultipleLogin = false;
        public static bool AllowMultipleLogin
        {
            get
            {
                InitializeSystemParameter();
                return _AllowMultipleLogin;
            }
            private set { _AllowMultipleLogin = value; }
        }


	}
}
