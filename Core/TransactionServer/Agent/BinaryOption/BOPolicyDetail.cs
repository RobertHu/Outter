using Core.TransactionServer.Agent.Util;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using log4net;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal struct BOPolicyDetailKey : IEquatable<BOPolicyDetailKey>
    {
        private readonly Guid _boPolicyId;
        private readonly Guid _boBetTypeId;
        private readonly int _frequence;

        internal BOPolicyDetailKey(Guid binaryOptionPolicyID, Guid binaryOptionBetTypeID, int frequence)
        {
            _boPolicyId = binaryOptionPolicyID;
            _boBetTypeId = binaryOptionBetTypeID;
            _frequence = frequence;
        }

        internal Guid BOPolicyId
        {
            get { return _boPolicyId; }
        }

        internal Guid BOBetTypeId
        {
            get { return _boBetTypeId; }
        }

        internal int Frequence
        {
            get { return _frequence; }
        }

        public bool Equals(BOPolicyDetailKey other)
        {
            return this.BOPolicyId.Equals(other.BOPolicyId) && this.BOBetTypeId.Equals(other.BOBetTypeId) && this.Frequence.Equals(other.Frequence);
        }

        public override int GetHashCode()
        {
            return HashCodeGenerator.Calculate(_boPolicyId.GetHashCode(), _boBetTypeId.GetHashCode(), _frequence.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return this.Equals((BOPolicyDetailKey)obj);
        }

    }

    internal sealed class BOPolicyDetail
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BOPolicyDetail));

        internal BOPolicyDetail(IDBRow row)
        {
            Guid binaryOptionPolicyID = (Guid)row["BOPolicyID"];
            Guid binaryOptionBetTypeID = (Guid)row["BOBetTypeID"];
            int frequency = (int)row["Frequency"];

            this.Key = new BOPolicyDetailKey(binaryOptionPolicyID, binaryOptionBetTypeID, frequency);
            this.MinBet = (decimal)row["MinBet"];
            this.MaxBet = (decimal)row["MaxBet"];
            this.AutoAcceptMaxBet = (decimal)row["AutoAcceptMaxBet"];
            this.StepBet = (decimal)row["StepBet"];
            this.Odds = (decimal)row["Odds"];
            this.CommissionOpen = (decimal)row["CommissionOpen"];
            this.MinCommissionOpen = (decimal)row["MinCommissionOpen"];
            this.MaxOrderCount = row.GetColumn<int?>("MaxOrderCount");
            this.TotalBetLimit = row.GetColumn<decimal?>("TotalBetLimit");
        }

        internal BOPolicyDetail(BOPolicyDetailKey key, XElement node)
        {
            this.Key = key;
            this.MinBet = node.Get<decimal>("MinBet");
            this.MaxBet = node.Get<decimal>("MaxBet");
            this.AutoAcceptMaxBet = node.Get<decimal>("AutoAcceptMaxBet");
            this.StepBet = node.Get<decimal>("StepBet");
            this.Odds = node.Get<decimal>("Odds");
            this.CommissionOpen = node.Get<decimal>("CommissionOpen");
            this.MinCommissionOpen = node.Get<decimal>("MinCommissionOpen");
            this.MaxOrderCount = node.Get<int?>("MaxOrderCount");
            this.TotalBetLimit = node.Get<decimal?>("TotalBetLimit");
        }

        internal BOPolicyDetailKey Key { get; private set; }

        internal decimal MinBet { get; private set; }

        internal decimal MaxBet { get; private set; }

        internal decimal AutoAcceptMaxBet { get; private set; }

        internal decimal StepBet { get; private set; }

        internal decimal Odds { get; private set; }

        internal decimal CommissionOpen { get; private set; }

        internal decimal MinCommissionOpen { get; private set; }

        internal int? MaxOrderCount { get; private set; }

        internal decimal? TotalBetLimit { get; private set; }

        internal BOBetType BetType
        {
            get { return BOBetTypeRepository.Get(this.Key.BOBetTypeId); }
        }

        internal void Update(XElement node)
        {
            foreach (var attribute in node.Attributes())
            {
                string name = attribute.Name.LocalName;
                string value = attribute.Value;

                switch (name)
                {
                    case "MinBet":
                        this.MinBet = value.Get<decimal>();
                        break;
                    case "MaxBet":
                        this.MaxBet = value.XmlToDecimal();
                        break;
                    case "AutoAcceptMaxBet":
                        this.AutoAcceptMaxBet = value.XmlToDecimal();
                        break;
                    case "StepBet":
                        this.StepBet = value.XmlToDecimal();
                        break;
                    case "Odds":
                        this.Odds = value.XmlToDecimal();
                        break;
                    case "CommissionOpen":
                        this.CommissionOpen = value.XmlToDecimal();
                        break;
                    case "MinCommissionOpen":
                        this.MinCommissionOpen = value.XmlToDecimal();
                        break;
                    case "MaxOrderCount":
                        this.MaxOrderCount = value.Get<Int32>();
                        break;
                    case "TotalBetLimit":
                        this.TotalBetLimit = value.Get<decimal?>();
                        break;
                    default:
                        Logger.WarnFormat("Unknow name={0},value={1} in update BOPolciyDetail", name, value);
                        break;
                }
            }
        }
    }

    public sealed class BOPolicyDetailRepository
    {
        public static readonly BOPolicyDetailRepository Default = new BOPolicyDetailRepository();

        private Dictionary<BOPolicyDetailKey, BOPolicyDetail> _itemDict = new Dictionary<BOPolicyDetailKey, BOPolicyDetail>();
        private BOPolicyDetailRepository() { }


        internal void Read(IDBRow row)
        {
            BOPolicyDetail detail = new BOPolicyDetail(row);
            _itemDict.Add(detail.Key, detail);
        }


        internal bool TryGet(BOPolicyDetailKey key, out BOPolicyDetail detail)
        {
            return _itemDict.TryGetValue(key, out detail);
        }

        public void Update(XElement node, string methodName)
        {
            if (node.Name == "BOPolicyDetail")
            {
                this.UpdateBOPolicyDetail(node, methodName);
            }
            else if (node.Name == "BOPolicyDetails")
            {
                foreach (var child in node.Elements())
                {
                    this.UpdateBOPolicyDetail(child, methodName);
                }
            }
            else if (node.Name == "BOPolicy" && methodName == "Delete")
            {
                this.RemoveBOPolicy(node);
            }
            else if (node.Name == "BOPolicys" && methodName == "Delete")
            {
                foreach (var child in node.Elements())
                {
                    this.RemoveBOPolicy(child);
                }
            }
        }

        private void RemoveBOPolicy(XElement node)
        {
            Guid binaryPolicyId = node.AttrToGuid("ID");
            List<BOPolicyDetailKey> shouldRemoveBOPolicyDetails = new List<BOPolicyDetailKey>();
            foreach (BOPolicyDetailKey key in _itemDict.Keys)
            {
                if (key.BOPolicyId == binaryPolicyId)
                {
                    shouldRemoveBOPolicyDetails.Add(key);
                }
            }

            foreach (BOPolicyDetailKey key in shouldRemoveBOPolicyDetails)
            {
                _itemDict.Remove(key);
            }
        }

        private void UpdateBOPolicyDetail(XElement node, string methodName)
        {
            Guid binaryOptionPolicyId = node.AttrToGuid("BOPolicyID");
            Guid binaryOptionBetTypeId = node.AttrToGuid("BOBetTypeID");
            int frequency = node.AttrToInt32("Frequency");
            BOPolicyDetailKey key = new BOPolicyDetailKey(binaryOptionPolicyId, binaryOptionBetTypeId, frequency);
            if (methodName == "Add")
            {
                _itemDict.Add(key, new BOPolicyDetail(key, node));
            }
            else if (methodName == "Delete")
            {
                _itemDict.Remove(key);
            }
            else if (methodName == "Modify")
            {
                _itemDict[key].Update(node);
            }
        }
    }


}
