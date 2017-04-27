using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Service
{
    internal sealed class BroadcasterProxy : Protocal.Communication.CommunicationServiceByEndPointName<Protocal.IBroadcastService>, Protocal.IBroadcastService
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(BroadcasterProxy));

        internal BroadcasterProxy(string url, string endpointName)
            : base(url, endpointName) { }

        protected override log4net.ILog Logger
        {
            get { return _Logger; }
        }

        public void Broadcast(Protocal.Command command)
        {
            this.Call(() => this.Service.Broadcast(command));
        }

        public bool Test()
        {
            throw new NotImplementedException();
        }
    }
}
