using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.Physical.TransactionBusiness;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Physical.AccountBusiness
{
    internal sealed class PhysicalExecuteStrategy : AccountExecuteStrategy
    {
        internal override void ExecuteTran(Transaction tran, Guid? executeOrderId, Price buyPrice, Price sellPrice, bool isFreeValidation)
        {
            tran.Execute(executeOrderId, buyPrice, sellPrice, isFreeValidation);
        }

        internal override void PreExecuteCheck(Transaction tran, bool checkMaxPhysicalValue)
        {
            this.VerifyIsExceedMaxPhysicalValue(tran, checkMaxPhysicalValue);
        }

        private void VerifyIsExceedMaxPhysicalValue(Transaction tran, bool checkMaxPhysicalValue)
        {
            var validateService = (PhysicalExecuteValidateService)tran.ValidateService;
            string errorDetail = string.Empty;
            if (checkMaxPhysicalValue && validateService.IsExceedMaxPhysicalValue(out errorDetail))
            {
                throw new TransactionException(TransactionError.ExceedMaxPhysicalValue, errorDetail);
            }
        }
    }
}
