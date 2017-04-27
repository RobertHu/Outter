using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Discovery;
using System.Text;

namespace Protocal.Communication
{
    public static class ServiceHostExtension
    {
        public static void AddDiscoveryFunction(this ServiceHost host)
        {
            host.Description.Behaviors.Add(new ServiceDiscoveryBehavior());
            host.AddServiceEndpoint(new UdpDiscoveryEndpoint());
        }
    }
}
