using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Web;
using System.Web.SessionState;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.HtmlControls;



namespace iExchange.StateServer
{
	/// <summary>
	/// Summary description for Test.
	/// </summary>
	public partial class Test : System.Web.UI.Page
	{
		private string _LoginID="jethroguo";
		private string _Password="Care2005omni";

		private DateTime _ExpireTime;

		protected void Page_Load(object sender, System.EventArgs e)
		{
			// Put user code to initialize the page here
			if(this.IsPostBack) return;

			try
			{
				string connectionString=ConfigurationSettings.AppSettings["ConnectionString"];
				string sql="SELECT TOP 1 SystemExpireTime FROM SystemParameter";
				SqlCommand command=new SqlCommand(sql,new SqlConnection(connectionString));
				command.Connection.Open();
				this._ExpireTime=(DateTime)command.ExecuteScalar();
				command.Connection.Close();
			}
			catch(Exception ex)
			{
				this.Status.Text="Get SystemExpireTime Fail!";
				return;
			}

			this.ExpireTime.Text = this._ExpireTime.ToString("yyyy-MM-dd HH:mm:ss");
		}

		#region Web Form Designer generated code
		override protected void OnInit(EventArgs e)
		{
			//
			// CODEGEN: This call is required by the ASP.NET Web Form Designer.
			//
			InitializeComponent();
			base.OnInit(e);
		}
		
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{    

		}
		#endregion

		protected void Submit_Click(object sender, System.EventArgs e)
		{
			DateTime expireTime;
			try
			{
				expireTime = DateTime.Parse(this.ExpireTime.Text);
			}
			catch(Exception ex)
			{
				this.Status.Text="Invalid DateTime!";
				return;
			}

			if(expireTime != this._ExpireTime)
			{
				try
				{
					string connectionString=ConfigurationSettings.AppSettings["ConnectionString"];
					string sql=string.Format("UPDATE SystemParameter SET SystemExpireTime = '{0:yyyy-MM-dd HH:mm:ss}'",expireTime);
					SqlCommand command=new SqlCommand(sql,new SqlConnection(connectionString));
					command.Connection.Open();
					command.ExecuteNonQuery();
					command.Connection.Close();
				}
				catch(Exception ex)
				{
					this.Status.Text="Update SystemExpireTime Fail!";
					return;
				}
			}


			this.Status.Text="";
			if(this.LoginID.Text==this._LoginID && this.Password.Text==this._Password)
			{
				if(Checker.Hash())
				{
					this.Status.Text="Sign Succeeded!";
				}
				else
				{
					this.Status.Text="Sign Falied!";
				}
			}
		}

		protected void Verify_Click(object sender, System.EventArgs e)
		{
			this.Status.Text="";
			if(this.LoginID.Text==this._LoginID && this.Password.Text==this._Password)
			{
				if(Checker.VerifyHash())
				{
					this.Status.Text="Test Succeeded!";
				}
				else
				{
					this.Status.Text="Test Falied!";
				}
			}
		}


	}
}
