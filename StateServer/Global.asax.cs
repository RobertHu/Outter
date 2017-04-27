using System;
using System.Net;
using System.Configuration;
using System.Collections;
using System.ComponentModel;
using System.Web;
using System.Diagnostics;
using System.Web.SessionState;

using iExchange.Common;
using iExchange.Common.Log;
using iExchange.StateServer.Adapter;
using System.Threading.Tasks;
using System.Threading;
[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace iExchange.StateServer
{
    /// <summary>
    /// Summary description for Global.
    /// </summary>
    public class Global : System.Web.HttpApplication
    {
        private static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(typeof(Global));
        public static StateServer StateServer;
        public static LogManager LogManager;
        public Global()
        {
            InitializeComponent();
        }

        protected void Application_Start(Object sender, EventArgs e)
        {
            try
            {
                //init State object
                if (this.IsTransactionAdapterServiceConfiged())
                {
                    log4net.Config.XmlConfigurator.Configure();
                    Task.Factory.StartNew(() =>
                        {
                            this.StartHost();
                        });
                }
                string stateServerID = ConfigurationManager.AppSettings["StateServerID"];
                string connectionString = ConfigurationManager.AppSettings["ConnectionString"];
                string securityConnectionString = ConfigurationManager.AppSettings["SecurityConnectionString"];
                string serverName = ConfigurationManager.AppSettings["ServerName"];
                if (string.IsNullOrEmpty(serverName)) serverName = "StateServer";
                //Check if data is tempered
                if (!Checker.VerifyHash()) throw new ApplicationException("Data is tempered, please contact the vendor!");

                string serviceUrl = this.GetLocalServiceUrl();

                Application["StateServer"] = Global.StateServer = new StateServer(stateServerID, connectionString, serviceUrl, securityConnectionString);

                //added by adam on 2009-02-05
                string workMode = ConfigurationManager.AppSettings["workMode"];
                if (String.Equals(workMode, "Filiale", StringComparison.CurrentCultureIgnoreCase))
                {
                    //create FilialeTransactionSwitcher instance, and begin send the failed transaction(if have) in new thread.
                    FilialeTransactionSwitcher.GetInstance();
                }

                AppDebug.LogEvent("StateServer", stateServerID + " started", EventLogEntryType.SuccessAudit);
                Application["LogManager"] = Global.LogManager = new LogManager(AppType.StateServer, connectionString, 60, serverName);
                Global.LogManager.LogServiceAction(Common.Log.ServiceAction.Start, DateTime.Now);

            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer", ex.ToString(), EventLogEntryType.Error);
                throw;
            }
        }

        private void StartHost()
        {
            try
            {
                Logger.Info("start host");
                Hoster.Default.Start();
                Logger.Info("host success");
            }
            catch (System.ServiceModel.AddressAlreadyInUseException ex)
            {
                Logger.ErrorFormat("host failed  , ex = {0}", ex);
                Thread.Sleep(60000);
                this.StartHost();
            }
        }


        private bool IsTransactionAdapterServiceConfiged()
        {
            return !string.IsNullOrEmpty(ConfigurationManager.AppSettings["TransactionAdaptorServiceUrl"]);
        }

        protected void Session_Start(Object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(Object sender, EventArgs e)
        {

        }

        protected void Application_EndRequest(Object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(Object sender, EventArgs e)
        {

        }

        protected void Application_Error(Object sender, EventArgs e)
        {

        }

        protected void Session_End(Object sender, EventArgs e)
        {

        }

        protected void Application_End(Object sender, EventArgs e)
        {
            //			try
            //			{
            //				StateServer stateServer=(StateServer)this.Application["StateServer"];
            //				stateServer.UnRegister(new Token(Guid.Empty,UserType.System,AppType.StateServer),null);
            //			}
            //			catch(Exception ee)
            //			{
            //				AppDebug.LogEvent("StateServer",ee.ToString(),EventLogEntryType.Error);
            //			}
            string stateServerID = ConfigurationSettings.AppSettings["StateServerID"];
            AppDebug.LogEvent("StateServer", stateServerID + " stopped", EventLogEntryType.SuccessAudit);
            Global.LogManager.LogServiceAction(Common.Log.ServiceAction.Stop, DateTime.Now);
            if (this.IsTransactionAdapterServiceConfiged())
            {
                Hoster.Default.Stop();
            }
        }

        private string GetLocalServiceUrl()
        {
            string serviceUrl = ConfigurationSettings.AppSettings["ServiceUrl"];
            if (serviceUrl == null || serviceUrl.Trim().Length == 0)
            {
                string authority = this.Context.Request.Url.GetLeftPart(UriPartial.Authority);
                string host = Dns.GetHostName();
                authority = authority.Replace("localhost", host);

                serviceUrl = authority + this.Context.Request.ApplicationPath + "/Service2.asmx";
            }
            return serviceUrl;
        }

        #region Web Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
        }
        #endregion
    }
}

