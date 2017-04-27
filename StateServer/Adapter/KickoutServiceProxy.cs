using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;

namespace iExchange.StateServer.Adapter
{
    public sealed class KickoutServiceProxy : Protocal.Communication.HttpCommunicationService<Protocal.IKickoutService>, Protocal.IKickoutService
    {
        private static readonly ILog _Logger = LogManager.GetLogger(typeof(KickoutServiceProxy));

        internal KickoutServiceProxy(string url)
            : base(url)
        {
        }

        public void Kickout(Guid userId)
        {
            this.Call(() => this.Service.Kickout(userId));
        }

        protected override log4net.ILog Logger
        {
            get { return _Logger; }
        }
    }
}