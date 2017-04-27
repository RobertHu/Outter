using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using iExchange.Common;
using System.ServiceModel;
using ManagerService.Exchange;
using System.Diagnostics;
using Manager.Common;
using System.Text;
using System.ServiceModel.Security;

namespace iExchange.StateServer.Manager
{
    public class ManagerClient
    {
        private static ManagerClient _Client = new ManagerClient();
        public static void Start(string managerAddress, string exchangeCode)
        {
            lock (ManagerClient._Client)
            {
                ManagerClient._Client.InternalStart(managerAddress, exchangeCode);
            }
        }

        public static void AddCommand(Command command)
        {
            if (!(command is QuotationCommand))
            {
                AppDebug.LogEvent("ManagerClient", "AddCommand :" + command.GetType().Name, EventLogEntryType.Information);
                ManagerClient._Client.InternalAddCommand(command);
            }
        }

        internal static bool FlushQuotations()
        {
            return ManagerClient._Client._ExchangeService.FlushCachedQuotations();
        }

        private RelayEngine<Command> _CommandRelayEngine = null;
        private string _ManagerAddress;
        private string _ExchangeCode;
        private IExchangeService _ExchangeService;
        private IExchangeService _ExchangeServiceQuotation;
        private object _ServiceLock = new object();

        private ManagerClient() { }

        private void InternalAddCommand(Command command)
        {
            this._CommandRelayEngine.AddItem(command);
        }

        private void InternalStart(string managerAddress, string exchangeCode)
        {
            this._ManagerAddress = managerAddress;
            this._ExchangeCode = exchangeCode;
            if (this._CommandRelayEngine == null)
            {
                this._CommandRelayEngine = new RelayEngine<Command>(null, this.HandlEngineException, this.SendCommand);
            }
            ThreadPool.QueueUserWorkItem(this.ConnectToManager);
        }

        private void HandlEngineException(Exception ex)
        {
            AppDebug.LogEvent("ManagerClient", "ManagerClient.HandlEngineException RelayEngine stopped:\r\n" + ex.ToString(), EventLogEntryType.Error);
        }

        private void ConnectToManager(object state)
        {
            try
            {
                EndpointAddress address = new EndpointAddress(this._ManagerAddress);
                NetTcpBinding binding = new NetTcpBinding(SecurityMode.None);
                binding.TransferMode = TransferMode.Buffered;
                binding.ReaderQuotas.MaxStringContentLength = int.MaxValue;
                binding.ReaderQuotas.MaxDepth = 64;
                binding.ReaderQuotas.MaxArrayLength = 16384;

                binding.OpenTimeout = TimeSpan.FromSeconds(50);
                binding.SendTimeout = TimeSpan.FromSeconds(180);
                binding.MaxReceivedMessageSize = int.MaxValue;
                binding.MaxBufferSize = int.MaxValue;
                binding.MaxBufferPoolSize = 524288;

                DuplexChannelFactory<IExchangeService> factory = new DuplexChannelFactory<IExchangeService>(new InstanceContext(new Callback()), binding, address);

                try
                {
                    foreach (var operationDescription in factory.Endpoint.Contract.Operations)
                    {
                        System.ServiceModel.Description.DataContractSerializerOperationBehavior dcsob =
                            operationDescription.Behaviors.Find<System.ServiceModel.Description.DataContractSerializerOperationBehavior>();
                        if (dcsob != null)
                        {
                            dcsob.MaxItemsInObjectGraph = int.MaxValue;
                        }
                    }
                }
                catch { }
                
                lock (this._ServiceLock)
                {
                    this._ExchangeService = factory.CreateChannel();
                    this._ExchangeService.Register(this._ExchangeCode, false);
                }
                this._CommandRelayEngine.Resume();

                // create quotation channel
                DuplexChannelFactory<IExchangeService> factory2 = new DuplexChannelFactory<IExchangeService>(new InstanceContext(new Callback()), binding, address);
                this._ExchangeServiceQuotation = factory2.CreateChannel();
                this._ExchangeServiceQuotation.Register(this._ExchangeCode, true);

                AppDebug.LogEvent("ManagerClient",
                    string.Format("StateServer connected to manager service at:{0}, ExchangeCode:{1}", this._ManagerAddress, this._ExchangeCode), EventLogEntryType.Information);
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerClient", "ConnectToManager failed:\r\n" + ex.ToString(), EventLogEntryType.Error);
            }
            finally
            {
                this.inRcorverConnection = false;
            }
        }

        private bool inRcorverConnection = false;
        private EngineResult SendCommand(Command command)
        {
            try
            {
                lock (this._ServiceLock)
                {
                    this._ExchangeService.AddCommand(command);
                }                
            }
            catch (Exception ex)
            {
                AppDebug.LogEvent("ManagerClient", "SendCommand failed:\r\n" + ex.ToString(), EventLogEntryType.Warning);

                if (ex is CommunicationObjectFaultedException)
                {
                    if (!this.inRcorverConnection)
                    {
                        this.inRcorverConnection = true;
                        ThreadPool.QueueUserWorkItem(this.ConnectToManager);
                    }
                    AppDebug.LogEvent("ManagerClient", "Try recover connection", EventLogEntryType.Warning);
                    Thread.Sleep(1000);
                    return EngineResult.FaildWithoutSuspend;
                }
            }
            return EngineResult.Success;            
        }

        internal static MinuteChartData[] GetMinuteChartData(Guid? instrumentId, Guid? quotePolicyId)
        {
            if (ManagerClient._Client._ExchangeService == null)
            {
                AppDebug.LogEvent("StateServer", "ManagerClient.GetMinuteChartData Faild : ManagerService Not Connected.", EventLogEntryType.Error);
                return null;
            }
            return ManagerClient._Client._ExchangeService.GetMinuteChartData(instrumentId, quotePolicyId);
        }


        internal static void UpdateLoginInfo(iExchange.Common.TraderServerType appType, string onlineXml, TimeSpan expireTime)
        {
            if (ManagerClient._Client._ExchangeService == null)
            {
                AppDebug.LogEvent("StateServer", "ManagerClient.UpdateLoginInfo Faild : ManagerService Not Connected.", EventLogEntryType.Warning);
                return;
            }
            ManagerClient._Client._ExchangeService.UpdateLoginInfo(appType, onlineXml, expireTime);
        }
    }
}