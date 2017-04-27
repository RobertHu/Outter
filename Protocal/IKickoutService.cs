using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace Protocal
{
    [ServiceContract]
    public interface IKickoutService
    {
        [OperationContract]
        void Kickout(Guid userId);
    }
}
