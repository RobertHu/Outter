using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ServiceModel;
using iExchange.Common;
using System.Diagnostics;

namespace iExchange.StateServer.Adapter
{
    internal sealed class Hoster : Protocal.Communication.HosterBase
    {
        internal static readonly Hoster Default = new Hoster();

        static Hoster() { }
        private Hoster() { }

        protected override List<Protocal.Communication.ServiceAgentBase> InitializeServiceAgents()
        {
            return new List<Protocal.Communication.ServiceAgentBase>
            {
                new Protocal.Communication.ServiceAgent<CommandCollector>()
            };
        }
    }
}