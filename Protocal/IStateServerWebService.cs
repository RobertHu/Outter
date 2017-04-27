using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Protocal
{
    [XmlSerializerFormat]
    [ServiceContract(Namespace = "http://www.omnicare.com/StateServer/")]
    public interface IStateServerWebService
    {
        [OperationContract]
        void NotifyManagerStarted(string managerAddress, string exchangeCode);
    }
}
