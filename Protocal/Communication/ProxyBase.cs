using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Protocal.Communication
{
    public abstract class ProxyBase<T> where T : class
    {
        protected static readonly ILog Logger = LogManager.GetLogger(typeof(ProxyBase<T>));
        protected bool _isChannelBroken = true;
        protected T _proxy;

        public virtual void ServiceDiscoveryHandle(List<EndpointAddress> addresses)
        {
            Logger.WarnFormat("In ServiceDiscoveryHandle , address = {0}", addresses[0]);
            if (addresses.Count == 0 || !_isChannelBroken) return;
            _proxy = Protocal.ChannelFactory.CreateTcpChannel<T>(addresses[0]);
            Logger.WarnFormat("create channel , address = {0}", addresses[0]);
            _isChannelBroken = false;
        }

        protected bool IsProxyInitialized()
        {
            if (_proxy == null)
            {
                Logger.Error("Proxy  has not initialized");
                return false;
            }
            return true;
        }
    }
}
