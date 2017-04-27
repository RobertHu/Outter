using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace SystemController.Manager
{
    [ServiceContract]
    public interface IResetService
    {
        [OperationContract]
        void DoReset(IList<Guid> instrumentIds);
    }

    public sealed class ResetService: IResetService
    {
        public void DoReset(IList<Guid> instrumentIds)
        {
            throw new NotImplementedException();
        }
    }
}
