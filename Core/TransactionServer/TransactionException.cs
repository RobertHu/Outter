using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer
{
    public class TransactionServerException : Exception
    {
        public TransactionServerException(iExchange.Common.TransactionError errorCode, string errorDetail = "")
            : base(string.Format("errorCode= {0}, errorDetails={1}", errorCode, errorDetail))
        {
            this.ErrorCode = errorCode;
            this.ErrorDetail = errorDetail;
        }

        public iExchange.Common.TransactionError ErrorCode { get; private set; }
        public string ErrorDetail { get; private set; }
    }
}
