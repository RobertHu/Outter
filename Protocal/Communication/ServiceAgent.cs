using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace Protocal.Communication
{
    public abstract class ServiceAgentBase
    {
        private volatile bool _isStarted;
        private ServiceHost _host;

        protected ServiceAgentBase(ServiceHost host)
        {
            _host = host;
        }

        public void Start()
        {
            if (!_isStarted)
            {
                _host.Open();
                _isStarted = true;
            }
        }

        public void Stop()
        {
            if (_isStarted)
            {
                _host.Close();
            }
        }
    }


    public sealed class ServiceAgent<T> : ServiceAgentBase where T : class
    {
        public ServiceAgent()
            : base(new ServiceHost(typeof(T)))
        {
        }
    }

    public sealed class WebServiceAgent<T> : ServiceAgentBase where T : class
    {
        public WebServiceAgent() :
            base(new WebServiceHost(typeof(T)))
        {
        }
    }

}
