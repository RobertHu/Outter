using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using System.Diagnostics;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Util.TypeExtension;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;
using log4net;

namespace Core.TransactionServer.Agent.Settings
{
    internal enum CustomerType
    {
        Customer = 0,
        Employee = 1
    }

    internal enum EmployeeType
    {
        Sales = 1,
        Manager = 2
    }

    public class Customer : IQuotePolicyProvider, PriceAlert.IQuotePolicySetter
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Customer));

        private Guid id;
        private string name;
        private Guid? privateQuotePolicyId;
        private Guid? dealingPolicyId = null;
        private CustomerType customerType;
        private EmployeeType employeeType;

        #region internal Properties
        public Guid Id
        {
            get { return this.id; }
        }

        public string Name
        {
            get { return this.name; }
        }

        public Guid PublicQuotePolicyId
        {
            get { return Settings.Setting.Default.SystemParameter.DefaultQuotePolicyId.Value; }
        }

        public Guid? PrivateQuotePolicyId
        {
            get { return this.privateQuotePolicyId; }
        }

        internal Guid? DealingPolicyId
        {
            get { return this.dealingPolicyId; }
        }

        internal CustomerType Type
        {
            get { return this.customerType; }
        }

        internal EmployeeType EmployeeType
        {
            get { return this.employeeType; }
        }

        internal DealingPolicy DealingPolicy
        {
            get
            {
                if (this.dealingPolicyId == null)
                {
                    return null;
                }
                else
                {
                    return Settings.Setting.Default.GetDealingPolicy(this.dealingPolicyId.Value);
                }
            }
        }
        #endregion

        internal Customer(IDBRow customerRow)
        {
            this.Update(customerRow);
        }

        internal Customer(XElement customerNode, CustomerType type)
        {
            this.id = customerNode.AttrToGuid("ID");
            this.Update(customerNode, type);
        }

        internal void Update(IDBRow customerRow)
        {
            this.id = (Guid)customerRow["ID"];
            this.name = (string)customerRow["Name"];
            this.dealingPolicyId = customerRow.GetColumn<Guid?>("DealingPolicyID");
            this.privateQuotePolicyId = customerRow.GetColumn<Guid?>("PrivateQuotePolicyID");
            if (customerRow.Contains("Type"))
            {
                this.customerType = (CustomerType)customerRow["Type"];
            }
            if (customerRow.Contains("EmployeeType"))
            {
                this.employeeType = (EmployeeType)customerRow["EmployeeType"];
            }
        }

        internal bool Update(XElement customerNode, CustomerType type)
        {
            Guid? dealingPolicyId = null;
            if (customerNode.Attribute("DealingPolicyID") != null && !string.IsNullOrEmpty(customerNode.Attribute("DealingPolicyID").Value))
            {
                dealingPolicyId = XmlConvert.ToGuid(customerNode.Attribute("DealingPolicyID").Value);
            }

            if (dealingPolicyId == null
                && (customerNode.Attribute("EmployeeDealingPolicyID") != null && !string.IsNullOrEmpty(customerNode.Attribute("EmployeeDealingPolicyID").Value)))
            {
                dealingPolicyId = XmlConvert.ToGuid(customerNode.Attribute("EmployeeDealingPolicyID").Value);
            }
            this.dealingPolicyId = dealingPolicyId;

            foreach (XAttribute attribute in customerNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "PublicQuotePolicyID":
                        break;
                    case "Name":
                        this.name = attribute.Value;
                        break;
                    case "PrivateQuotePolicyID":
                    case "EmployeeQuotePolicyID":
                        if (string.IsNullOrEmpty(attribute.Value))
                        {
                            this.privateQuotePolicyId = null;
                        }
                        else
                        {
                            this.privateQuotePolicyId = XmlConvert.ToGuid(attribute.Value);
                        }
                        break;
                }
            }

            this.customerType = type;

            return true;
        }

        Guid PriceAlert.IQuotePolicySetter.PublicQuotePolicyID
        {
            get { return this.PublicQuotePolicyId; }
        }

        Guid? PriceAlert.IQuotePolicySetter.PrivateQuotePolicyID
        {
            get { return this.PrivateQuotePolicyId; }
        }
    }
}