using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.BinaryOption
{
    internal static class BOPolicyRepository
    {
        private static Dictionary<Guid, BOPolicy> _binaryOptionPolicies = new Dictionary<Guid, BOPolicy>(50);

        internal static void Read(IDBRow row)
        {
            var boPolicy = new BOPolicy(row);
            _binaryOptionPolicies.Add(boPolicy.ID, boPolicy);
        }

        internal static bool TryGet(Guid binaryOptionPolicyId, out BOPolicy boPolicy)
        {
            return _binaryOptionPolicies.TryGetValue(binaryOptionPolicyId, out boPolicy);
        }

        internal static void Update(XElement node, string methodName)
        {
            if (node.Name == "BOPolicy")
            {
                UpdateBOPolicy(node, methodName);
            }
            else if (node.Name == "BOPolicys")
            {
                foreach (XElement child in node.Elements("BOPolicy"))
                {
                    UpdateBOPolicy(child, methodName);
                }
            }
        }

        private static void UpdateBOPolicy(XElement node, string methodName)
        {
            if (methodName == "Add")
            {
                BOPolicy policy = new BOPolicy(node);
                _binaryOptionPolicies[policy.ID] = policy;
            }
            else if (methodName == "Delete")
            {
                Guid binaryPolicyId = XmlConvert.ToGuid(node.Attribute("ID").Value);
                _binaryOptionPolicies.Remove(binaryPolicyId);
            }
            else if (methodName == "Modify")
            {
                Guid binaryPolicyId = XmlConvert.ToGuid(node.Attribute("ID").Value);
                _binaryOptionPolicies[binaryPolicyId].Update(node);
            }
        }
    }
}
