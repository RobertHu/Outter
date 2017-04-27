using log4net;
using Protocal.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Text;
using System.Threading.Tasks;

namespace SystemController
{
    internal sealed class Hoster : HosterBase
    {
        internal static readonly Hoster Default = new Hoster();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Hoster));
        private List<ServiceAgentBase> _agentList;

        static Hoster() { }
        private Hoster()
        {
        }

        protected override List<ServiceAgentBase> InitializeServiceAgents()
        {
            return _agentList = new List<ServiceAgentBase>()
            {
                new ServiceAgent<GatewayService>(),
                new ServiceAgent<BroadcastService>(),
                new ServiceAgent<Services.SystemControllerService>(),
                new ServiceAgent<Services.TransactionService>(),
                new ServiceAgent<Services.QuotationService>()
            };
        }
    }
}
