using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Protocal
{
    public delegate void ServiceDiscoveryHandle(List<EndpointAddress> address);

    public sealed class ServiceFinder<T>
    {
        public event ServiceDiscoveryHandle ServiceDiscovied;
        private Thread _thread;
        private DiscoveryClient _client;
        private FindCriteria _findCriteria;
        private List<EndpointAddress> _addressList;
        private volatile bool _isStopped = false;

        public ServiceFinder(List<string> scopes = null)
        {
            _client = new DiscoveryClient(new UdpDiscoveryEndpoint());
            _findCriteria = new FindCriteria(typeof(T))
                {
                    Duration = TimeSpan.FromSeconds(2)
                };
            if (scopes != null)
            {
                foreach (var eachScope in scopes)
                {
                    _findCriteria.Scopes.Add(new Uri(eachScope));
                }
            }
            _addressList = new List<EndpointAddress>();
            _thread = new Thread(this.Discovery);
            _thread.IsBackground = true;
        }

        public void Start()
        {
            _thread.Start();
        }

        public void Stop()
        {
            _isStopped = true;
        }

        private void Discovery()
        {
            while (!_isStopped)
            {
                var result = this.FindServiceAddress();
                if (result.Count == 0) continue;
                this.OnDiscovery(result);
                if (_isStopped)
                {
                    _client.Close();
                }
                Thread.Sleep(10000);
            }
            _client.Close();
        }

        private List<EndpointAddress> FindServiceAddress()
        {
            _addressList.Clear();
            FindResponse findResponse = _client.Find(_findCriteria);
            foreach (var m in findResponse.Endpoints)
            {
                _addressList.Add(m.Address);
            }
            return _addressList;
        }

        private void OnDiscovery(List<EndpointAddress> addresses)
        {
            var handle = this.ServiceDiscovied;
            if (handle != null)
            {
                handle(addresses);
            }
        }
    }
}
