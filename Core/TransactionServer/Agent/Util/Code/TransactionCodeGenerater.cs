using Core.TransactionServer.Agent.Settings;
using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using Core.TransactionServer.Agent.Periphery;
using Core.TransactionServer.Agent.DB;
using log4net;

namespace Core.TransactionServer.Agent.Util.Code
{
    internal enum CodeType
    {
        None,
        Order,
        Transaction
    }

    public sealed class TransactionCodeGenerater
    {
        public static readonly TransactionCodeGenerater Default = new TransactionCodeGenerater();
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionCodeGenerater));
#if PLACETEST
        private const int CODE_LENGTH = 18;
        private const int SEQENCE_PART_LENGTH = 7;
#else
        private const int CODE_LENGTH = 16;
        private const int SEQENCE_PART_LENGTH = 5;
#endif
        private Dictionary<string, int> _orderPrefixPerSequnceDict = new Dictionary<string, int>();
        private Dictionary<string, int> _tranPrefixPerSequenceDict = new Dictionary<string, int>();
        private object _mutex = new object();

        static TransactionCodeGenerater() { }
        private TransactionCodeGenerater() { }

        internal void FillTranAndOrderCode(Transaction tran)
        {
            tran.Code = this.GenerateTranCode(tran.Owner.Setting().OrganizationId, tran.OrderType);
            foreach (var eachOrder in tran.Orders)
            {
                eachOrder.Code = this.GenerateOrderCode(tran.Owner.Setting().OrganizationId);
            }
        }

        internal string GenerateTranCode(Guid organizationId, OrderType orderType)
        {
            var organizationCode = OrganizationAndOrderTypeRepository.Default.GetOrganizationCode(organizationId);
            var orderTypeCode = OrganizationAndOrderTypeRepository.Default.GetOrderTypeCode((int)orderType);
            var tradeDay = Settings.Setting.Default.GetTradeDay();
            return this.GenerateTranCode(organizationCode, tradeDay, orderTypeCode);
        }

        internal string GenerateOrderCode(Guid organizationId)
        {
            var organizationCode = OrganizationAndOrderTypeRepository.Default.GetOrganizationCode(organizationId);
            var tradeDay = Settings.Setting.Default.GetTradeDay();
            string prefix = this.GenerateOrderCodePrefix(organizationCode, tradeDay);
            string suffix = this.GenerateCodeSuffix(this.GetOrderSequence(prefix));
            return prefix + suffix;
        }

        private string GenerateTranCode(string orgCode, TradeDay tradeDay, string orderTypeCode)
        {
            string prefix = this.GenerateTranCodePrefix(orgCode, tradeDay, orderTypeCode);
            string suffix = this.GenerateCodeSuffix(this.GetTranSequence(prefix));
            return prefix + suffix;
        }

        private string GenerateTranCodePrefix(string orgCode, TradeDay tradeDay, string orderTypeCode)
        {
            return orgCode + tradeDay.Day.ToString("yyMMdd") + orderTypeCode;
        }

        private string GenerateOrderCodePrefix(string orgCode, TradeDay tradeDay)
        {
            return orgCode + tradeDay.Day.ToString("yyyyMMdd");
        }

        private string GenerateCodeSuffix(long sequence)
        {
            string sequenceString = sequence.ToString();
            try
            {
                return new string('0', SEQENCE_PART_LENGTH - sequenceString.Length) + sequenceString;
            }
            catch (System.ArgumentOutOfRangeException)
            {
                Logger.ErrorFormat("GenerateCodeSuffix, sequence = {0},  sequenceLength = {1}, SEQENCE_PART_LENGTH = {2}", sequence, sequenceString.Length, SEQENCE_PART_LENGTH);
                throw;
            }
        }

        private int GetTranSequence(string tranPrefix)
        {
            return this.GetSequence(CodeType.Transaction, tranPrefix, _tranPrefixPerSequenceDict);
        }

        private int GetOrderSequence(string orderPrefix)
        {
            return this.GetSequence(CodeType.Order, orderPrefix, _orderPrefixPerSequnceDict);
        }


        private int GetSequence(CodeType codeType, string codePrefix, Dictionary<string, int> sequencePerPrefixDict)
        {
            lock (_mutex)
            {
                int sequence;
                if (!sequencePerPrefixDict.TryGetValue(codePrefix, out sequence))
                {
                    sequence = DBRepository.Default.GetCodeSequence(CodeType.Transaction, codePrefix);
                    sequencePerPrefixDict.Add(codePrefix, sequence);
                }
                else
                {
                    sequence = sequencePerPrefixDict[codePrefix] + 1;
                    sequencePerPrefixDict[codePrefix] = sequence;
                }
                return sequence;
            }
        }

    }
}
