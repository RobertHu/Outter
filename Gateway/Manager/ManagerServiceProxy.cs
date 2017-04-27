using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace SystemController
{
    internal sealed class ManagerServiceProxy
    {
        private string _managerServiceAddress;
        private string _iExchangeCode;
        private IExchangeService _service;

        internal static readonly ManagerServiceProxy Default = new ManagerServiceProxy();

        private ManagerServiceProxy() { }

        internal void Initialize(string managerServiceAddress, string iExchangeCode)
        {
            _managerServiceAddress = managerServiceAddress;
            _iExchangeCode = iExchangeCode;
            Task.Factory.StartNew(this.CreateConnection);
        }

        private void CreateConnection()
        {
            EndpointAddress address = new EndpointAddress(_managerServiceAddress);
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

            DuplexChannelFactory<IExchangeService> factory = new DuplexChannelFactory<IExchangeService>(new InstanceContext(new ManagerServiceCallback()), binding, address);
            _service = factory.CreateChannel();
            _service.Register(_iExchangeCode, true);
        }

    }
}
