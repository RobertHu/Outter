using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using SystemController.Factory;
using log4net;
using Protocal;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;

namespace SystemController.Services
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
    public sealed class QuotationService : IQuotationService
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(QuotationService));

        public void SetQuotation(OriginQ[] originQs, OverridedQ[] overridedQs)
        {
            try
            {
                QuotationCommand command = new QuotationCommand
                {
                    OriginQs = originQs,
                    OverridedQs = overridedQs,
                    IsQuotation = true
                };
                Broadcaster.Default.AddCommand(command);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }

}
