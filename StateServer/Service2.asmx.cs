using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Web;
using System.Web.Services;
using System.Xml;

using iExchange.Common;
using iExchange.StateServer.Manager;

namespace iExchange.StateServer
{
    /// <summary>
    /// Summary description for Service2.
    /// </summary>
    [WebService(Namespace = "http://www.omnicare.com/StateServer/")]
    public class Service2 : System.Web.Services.WebService
    {
        protected StateServer StateServer
        {
            get { return (StateServer)Application["StateServer"]; }
        }

        public Service2()
        {
            //CODEGEN: This call is required by the ASP.NET Web Services Designer
            InitializeComponent();
        }

        #region Component Designer generated code

        //Required by the Web Services Designer 
        private IContainer components = null;

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null)
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        [WebMethod]
        public string HelloWorld()
        {
            return "Hello World";
        }

        [WebMethod]
        public int UpdateInstrument(string instrumentCode, bool priceEnable, string sourceName, out string errorMsg)
        {
            return this.StateServer.Update(new Token(Guid.Empty, UserType.System, AppType.QuotationServer), instrumentCode, priceEnable, sourceName, out errorMsg);
        }


        [WebMethod]
        public void Update(Token token, XmlNode update)
        {
            this.StateServer.Update(token, update);
        }

        [WebMethod]
        public void BroadcastQuotation(Token token, OriginQuotation[] originQs, OverridedQuotation[] overridedQs)
        {
            this.StateServer.Broadcast(token, originQs, overridedQs);
        }

        [WebMethod]
        public void Reset(Token token, XmlNode resetNode, OverridedQuotation[] overridedQs)
        {
            this.StateServer.Reset(token, resetNode, overridedQs);
        }

        [WebMethod(Description = "Flush all quotations in quotationServer memory to database")]
        public bool FlushQuotations(Token token, string quotation)
        {
            return this.StateServer.FlushQuotations(token);
        }

        [WebMethod(Description = "Get last cycle minute chart data")]
        public MinuteChartData[] GetMinuteChartData(Token token, Guid? instrumentId, Guid? quotePolicyId)
        {
            return this.StateServer.GetMinuteChartData(instrumentId, quotePolicyId);
        }

        [WebMethod]
        public void Alert(Token token, XmlNode alertNode)
        {
            this.StateServer.Alert(token, alertNode);
        }

        [WebMethod]
        public void Cut(Token token, XmlNode cutNode)
        {
            this.StateServer.Cut(token, cutNode);
        }

        [WebMethod]
        public void BroadcastExecuteResult(Token token, Guid tranId, TransactionError error, XmlNode xmlTran, XmlNode xmlAccount)
        {
            this.StateServer.BroadcastExecuteResult(token, tranId, error, xmlTran, xmlAccount);
        }

        [WebMethod]
        public void BroadcastCommand(Token token, Command[] commands)
        {
            this.StateServer.BroadcastCommand(token, commands);
        }

        [WebMethod]
        public bool BroadcastTradeCommand(Token token, iExchange.Common.External.CME.TradeCommand tradeCommand)
        {
            return this.StateServer.BroadcastTradeCommand(token, tradeCommand);
        }

        [WebMethod]
        public bool BroadcastOpenInterestCommand(Token token, iExchange.Common.External.CME.OpenInterestCommand openInterestCommand)
        {
            return this.StateServer.BroadcastOpenInterestCommand(token, openInterestCommand);
        }

        /*
                [WebMethod]
                public string ExecuteToLinkedServer(string xmlTran)
                {
                    XmlDocument xmlDoc=new XmlDocument();
                    XmlNode tran=xmlDoc.CreateElement("Dummy");
                    tran.InnerXml=xmlTran;

                    StateServer stateServer=(StateServer)Application["StateServer"];
                    XmlNode account= stateServer.ExecuteToLinkedServer(tran.FirstChild);

                    return account.OuterXml;
                }
        */

        [WebMethod]
        public void NotifyManagerStarted(string managerAddress, string exchangeCode)
        {
            try
            {
                if (this.StateServer.IsUseManager)
                {
                    ManagerClient.Start(managerAddress, exchangeCode);
                }
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("StateServer.Service2.NotifyManagerStarted", ex.ToString(), EventLogEntryType.Error);
            }
        }
    }
}
