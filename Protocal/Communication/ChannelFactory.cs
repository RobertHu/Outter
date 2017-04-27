using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Text;

namespace Protocal
{
    public static class ChannelFactory
    {
        public static T CreateTcpChannel<T>(string address)
        {
            return CreateTcpChannel<T>(new EndpointAddress(address));
        }

        public static T CreateTcpChannel<T>(EndpointAddress address)
        {
            var binding = new NetTcpBinding(SecurityMode.None);
            binding.SendTimeout = TimeSpan.FromMinutes(5);
            binding.OpenTimeout = TimeSpan.FromMinutes(5);
            binding.CloseTimeout = TimeSpan.FromMinutes(5);
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.MaxBufferSize = int.MaxValue;
            return ChannelFactory<T>.CreateChannel(binding, address);
        }

        public static T CreateChannelByName<T>(string endpointName, EndpointAddress address)
        {
            System.ServiceModel.ChannelFactory<T> factory = new System.ServiceModel.ChannelFactory<T>(endpointName, address);
            return factory.CreateChannel();
        }

        public static T CreateHttpChannel<T>(string address)
        {
            var binding = new BasicHttpBinding(BasicHttpSecurityMode.None);
            binding.SendTimeout = TimeSpan.FromMinutes(10);
            binding.OpenTimeout = TimeSpan.FromMinutes(10);
            binding.CloseTimeout = TimeSpan.FromSeconds(5);
            binding.MaxReceivedMessageSize = int.MaxValue;
            binding.MaxBufferSize = int.MaxValue;
            binding.ReaderQuotas = new System.Xml.XmlDictionaryReaderQuotas()
            {
                MaxStringContentLength = 2147483647,
                MaxDepth = 2147483647,
                MaxArrayLength = 2147483647,
                MaxBytesPerRead = 2147483647,
                MaxNameTableCharCount = 2147483647
            };
            return ChannelFactory<T>.CreateChannel(binding, new EndpointAddress(address));
        }


    }
}
