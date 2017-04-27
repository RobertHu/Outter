using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Core.TransactionServer.Agent.Service
{
    internal sealed class Hoster : Protocal.Communication.HosterBase
    {
        internal static readonly Hoster Default = new Hoster();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Hoster));

        static Hoster() { }
        private Hoster() { }

        protected override List<Protocal.Communication.ServiceAgentBase> InitializeServiceAgents()
        {
            return new List<Protocal.Communication.ServiceAgentBase>
            {
                new Protocal.Communication.ServiceAgent<ServerService>(),
                new Protocal.Communication.ServiceAgent<TransactionServerService>(),
                new Protocal.Communication.ServiceAgent<CommandCollectService>(),
                new Protocal.Communication.ServiceAgent<TransactionAdapterService>(),
            };
        }
    }

}
