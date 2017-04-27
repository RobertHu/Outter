using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal.TypeExtensions;
using System.Xml.Linq;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    public sealed class Currency
    {
        private Guid id;
        private int decimals;

        internal Currency(IDBRow currencyRow)
        {
            this.id = (Guid)currencyRow["ID"];
            this.Code = (string)currencyRow["Code"];
            this.decimals = (short)currencyRow["Decimals"];
            this.InterestPolicyId = currencyRow.GetColumn<Guid?>("InterestPolicyID");
            this.UInterestIn = currencyRow.GetColumn<decimal>("UInterestIn");
            this.UInterestOut = currencyRow.GetColumn<decimal>("UInterestOut");
            this.UsableInterestDayYear = currencyRow.GetColumn<int>("UsableInterestDayYear");
        }

        internal Currency(Guid id, string code, int decimals)
        {
            this.id = id;
            this.Code = code;
            this.decimals = decimals;
        }

        public Guid Id
        {
            get { return this.id; }
        }

        public int Decimals
        {
            get { return this.decimals; }
            set { this.decimals = value; }
        }

        public string Code
        {
            get;
            set;
        }

        internal Guid? InterestPolicyId { get; private set; }

        internal decimal UInterestIn { get; private set; }

        internal decimal UInterestOut { get; private set; }

        internal int UsableInterestDayYear { get; private set; }


        //internal void Update(XElement updateNode)
        //{
        //    if (updateNode.HasAttribute("Code"))
        //    {
        //        this.Code = updateNode.Attribute("Code").Value;
        //    }
        //}



    }
}