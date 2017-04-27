using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SystemController.Broadcast
{
    internal sealed class ClientFactory
    {
        internal static ClientBase Create(ICommandCollectService service, string url, iExchange.Common.AppType appType)
        {
            if (appType == iExchange.Common.AppType.TransactionServer)
            {
                return new TransactionClient(service, url, appType);
            }
            return new Client(service, url, appType);
        }

    }
}
