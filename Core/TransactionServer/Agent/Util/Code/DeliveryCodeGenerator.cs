using Core.TransactionServer.Agent.DB;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;

namespace Core.TransactionServer.Agent.Util.Code
{

    internal sealed class DeliveryCodeInfo
    {
        internal DeliveryCodeInfo(string codePrefix, string printingCodePrefix, long sequence, int numberLength)
        {
            this.CodePrefix = codePrefix;
            this.PrintingCodePrefix = printingCodePrefix;
            this.Sequence = sequence;
            this.NumberLength = numberLength;
        }

        internal string CodePrefix { get; private set; }
        internal string PrintingCodePrefix { get; private set; }
        internal long Sequence { get; set; }
        internal int NumberLength { get; private set; }
    }

    public sealed class DeliveryCodeGenerator
    {
        public static readonly DeliveryCodeGenerator Default = new DeliveryCodeGenerator();
        private Dictionary<DateTime, DeliveryCodeInfo> _codeDict = new Dictionary<DateTime, DeliveryCodeInfo>();
        private object _mutex = new object();

        static DeliveryCodeGenerator() { }
        private DeliveryCodeGenerator() { }

        public Tuple<string, string> Create()
        {
            lock (_mutex)
            {
                DeliveryCodeInfo codeInfo;
                DateTime tradeDay = DateTime.Now.Date;
                if (!_codeDict.TryGetValue(tradeDay, out codeInfo))
                {
                    DataRow dataRow = DBRepository.Default.GetPhysicalCode(2, tradeDay);
                    codeInfo = this.CreateDeliveryCodeInfo(dataRow);
                    _codeDict.Add(tradeDay, codeInfo);
                }
                codeInfo.Sequence += 1;
                string nextSequence = this.GenerateSequence(codeInfo);
                return Tuple.Create(codeInfo.CodePrefix + nextSequence, codeInfo.PrintingCodePrefix + nextSequence);
            }
        }

        private DeliveryCodeInfo CreateDeliveryCodeInfo(DataRow dataRow)
        {
            string codePrefix = (string)dataRow["CodePrefix"];
            string printingCodePrefix = (string)dataRow["PrintingCodePrefix"];
            long sequence = (long)dataRow["Sequence"];
            int numberLength = (int)dataRow["NumberLength"];
            return new DeliveryCodeInfo(codePrefix, printingCodePrefix, sequence, numberLength);
        }


        private string GenerateSequence(DeliveryCodeInfo codeInfo)
        {
            int sequenceLength = codeInfo.Sequence.ToString().Length;
            return new string('0', codeInfo.NumberLength - sequenceLength) + codeInfo.Sequence.ToString();
        }

    }
}
