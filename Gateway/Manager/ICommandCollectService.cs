using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace SystemController.Manager
{
    [ServiceContract(SessionMode = SessionMode.Allowed)]
    public interface ICommandCollectService
    {
        [OperationContract]
        [XmlSerializerFormat]
        void AddCommand(Token token, Command command);


        [OperationContract]
        [XmlSerializerFormat]
        void KickoutPredecessor(Guid userId);

    }

    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Single)]
    public class CommandCollectService : ICommandCollectService
    {
        public void AddCommand(iExchange.Common.Token token, iExchange.Common.Command command)
        {
            QuotationCommand quotationCommand = command as QuotationCommand;
            if (quotationCommand != null)
            {
                var targetCommand = SystemController.Factory.CommandFactory.CreateQuotationCommand(quotationCommand.OriginQs, quotationCommand.OverridedQs);
                Broadcaster.Default.AddCommand(targetCommand);
            }
        }

        public void KickoutPredecessor(Guid userId)
        {
        }
    }


}
