using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;
using iExchange.Common;
using System.Data.SqlClient;
using System.Data;
using System.Collections;
using System.Xml.Linq;
using log4net;

namespace iExchange.StateServer.Adapter.OuterService
{
    public sealed class GroupNetPositionManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(GroupNetPositionManager));

        private class AccountGroupGNP
        {
            private Guid id;
            private string code;

            public Hashtable accountGNPs = new Hashtable();

            private AccountGroupGNP()
            { }

            public AccountGroupGNP(Guid id, string code)
            {
                this.id = id;
                this.code = code;
            }

            public XmlElement ToXmlNode(XmlDocument xmlDoc)
            {
                XmlElement groupNode = xmlDoc.CreateElement("Group");

                groupNode.SetAttribute("ID", XmlConvert.ToString(this.id));
                groupNode.SetAttribute("Code", this.code);

                XmlElement accountsNode = xmlDoc.CreateElement("Accounts");
                groupNode.AppendChild(accountsNode);
                foreach (AccountGNP accountGNP in this.accountGNPs.Values)
                {
                    accountsNode.AppendChild(accountGNP.ToXmlNode(xmlDoc));
                }

                return groupNode;
            }
        }

        private class AccountGNP
        {
            private Guid id;
            private string code;
            private AccountType type;

            public Dictionary<Guid, InstrumentGNP> instrumentGNPs = new Dictionary<Guid, InstrumentGNP>();

            private AccountGNP()
            { }

            public AccountGNP(Guid id, string code, AccountType type)
            {
                this.id = id;
                this.code = code;
                this.type = type;
            }

            public XmlElement ToXmlNode(XmlDocument xmlDoc)
            {
                XmlElement accountNode = xmlDoc.CreateElement("Account");

                accountNode.SetAttribute("ID", XmlConvert.ToString(this.id));
                accountNode.SetAttribute("Code", this.code);
                accountNode.SetAttribute("Type", XmlConvert.ToString((int)this.type));

                XmlElement instrumentsNode = xmlDoc.CreateElement("Instruments");
                accountNode.AppendChild(instrumentsNode);
                foreach (InstrumentGNP instrumentGNP in this.instrumentGNPs.Values)
                {
                    instrumentsNode.AppendChild(instrumentGNP.ToXmlNode(xmlDoc));
                }

                return accountNode;
            }
        }

        private class InstrumentGNP
        {
            private Guid id;
            private int numeratorUnit;
            private int denominator;
            private decimal lotBalance = decimal.Zero;
            private decimal quantity = decimal.Zero;
            private decimal buyQuantity = decimal.Zero;
            private string buyAveragePrice = string.Empty;
            private decimal buyMultiplyValue = decimal.Zero;
            private decimal sellQuantity = decimal.Zero;
            private string sellAveragePrice = string.Empty;
            private decimal sellMultiplyValue = decimal.Zero;
            private decimal _BuyLot = decimal.Zero;
            private decimal _SellLot = decimal.Zero;
            private decimal _BuySumEl = decimal.Zero;
            private decimal _SellSumEl = decimal.Zero;

            private InstrumentGNP()
            { }

            public InstrumentGNP(Guid id, int numeratorUnit, int denominator)
            {
                this.id = id;
                this.numeratorUnit = numeratorUnit;
                this.denominator = denominator;
            }
            public decimal LotBalance
            {
                get { return this.lotBalance; }
                set { this.lotBalance = value; }
            }

            public decimal Quantity
            {
                get { return this.quantity; }
            }

            public decimal BuyQuantity
            {
                get { return this.buyQuantity; }
                set { this.buyQuantity = value; }
            }

            public string BuyAveragePrice
            {
                get
                {
                    if (this.BuyMultiplyValue == decimal.Zero || this.BuyQuantity == decimal.Zero)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return (string)Price.CreateInstance((double)(this.BuyMultiplyValue / this.BuyQuantity), this.numeratorUnit, this.denominator);
                    }
                }
            }

            public decimal BuyMultiplyValue
            {
                get { return this.buyMultiplyValue; }
                set { this.buyMultiplyValue = value; }
            }

            public decimal SellQuantity
            {
                get { return this.sellQuantity; }
                set { this.sellQuantity = value; }
            }

            public string SellAveragePrice
            {
                get
                {
                    if (this.SellMultiplyValue == decimal.Zero || this.SellQuantity == decimal.Zero)
                    {
                        return string.Empty;
                    }
                    else
                    {
                        return (string)Price.CreateInstance((double)(this.SellMultiplyValue / this.SellQuantity), this.numeratorUnit, this.denominator);
                    }
                }
            }

            public decimal SellMultiplyValue
            {
                get { return this.sellMultiplyValue; }
                set { this.sellMultiplyValue = value; }
            }

            public decimal BuyLot
            {
                get { return this._BuyLot; }
                set { this._BuyLot = value; }
            }
            public decimal SellLot
            {
                get { return this._SellLot; }
                set { this._SellLot = value; }
            }
            public decimal BuySumEl
            {
                get { return this._BuySumEl; }
                set { this._BuySumEl = value; }
            }
            public decimal SellSumEl
            {
                get { return this._SellSumEl; }
                set { this._SellSumEl = value; }
            }

            public void AddQuotation(decimal value)
            {
                this.quantity += value;
            }
            public void AddLotBalance(decimal value)
            {
                this.lotBalance += value;
            }

            //Modify by Erric add attribute LotBalance
            public XmlElement ToXmlNode(XmlDocument xmlDoc)
            {
                XmlElement instrumentNode = xmlDoc.CreateElement("Instrument");
                instrumentNode.SetAttribute("ID", XmlConvert.ToString(this.id));
                instrumentNode.SetAttribute("LotBalance", XmlConvert.ToString(this.lotBalance));
                instrumentNode.SetAttribute("Quantity", XmlConvert.ToString(this.Quantity));
                instrumentNode.SetAttribute("BuyQuantity", XmlConvert.ToString(this.BuyQuantity));
                instrumentNode.SetAttribute("BuyAveragePrice", this.BuyAveragePrice);
                instrumentNode.SetAttribute("BuyMultiplyValue", XmlConvert.ToString(this.BuyMultiplyValue));
                instrumentNode.SetAttribute("SellQuantity", XmlConvert.ToString(this.SellQuantity));
                instrumentNode.SetAttribute("SellAveragePrice", this.SellAveragePrice);
                instrumentNode.SetAttribute("SellMultiplyValue", XmlConvert.ToString(this.SellMultiplyValue));
                instrumentNode.SetAttribute("SellLot", XmlConvert.ToString(this.SellLot));
                instrumentNode.SetAttribute("BuyLot", XmlConvert.ToString(this.BuyLot));
                instrumentNode.SetAttribute("SellSumEl", XmlConvert.ToString(this.SellSumEl));
                instrumentNode.SetAttribute("BuySumEl", XmlConvert.ToString(this.BuySumEl));

                return instrumentNode;
            }
        }

        private string _connnectionString;
        private SystemControllerProxy _proxy;

        internal GroupNetPositionManager(string connectionString, SystemControllerProxy proxy)
        {
            _connnectionString = connectionString;
            _proxy = proxy;
        }

        public XmlNode GetGroupNetPositionForManager(Token token, string permissionName, Guid[] accountGroupIds, Guid[] instrumentGroupIDs, bool showActualQuantity, string[] blotterCodeSelecteds)
        {
            try
            {
                DataSet dataSet = this.LoadDBData(token, permissionName, accountGroupIds, instrumentGroupIDs);
                if (dataSet == null || dataSet.Tables.Count <= 0 || dataSet.Tables[0].Rows.Count <= 0) return null;
                Guid[] instrumentIDs = this.BuildInstrumentIds(dataSet);
                Dictionary<Guid, AccountGroupGNP> accountGroupDict = this.BuildAccountGroupDict(dataSet, instrumentIDs, blotterCodeSelecteds);
                XmlDocument xmlDoc = new XmlDocument();
                XmlElement groupsNode = xmlDoc.CreateElement("Groups");
                xmlDoc.AppendChild(groupsNode);
                foreach (AccountGroupGNP accountGroupGNP in accountGroupDict.Values)
                {
                    if (accountGroupGNP.accountGNPs.Count > 0)
                    {
                        groupsNode.AppendChild(accountGroupGNP.ToXmlNode(xmlDoc));
                    }
                }
                if (!groupsNode.HasChildNodes) groupsNode = null;

                return groupsNode;

            }
            catch (Exception ex)
            {
                //Logger.Error(string.Format("userId = {0}, permissionName = {1}, accountGroupIds= {2}, instrumentGroupIds={3}, showActualQuantity = {4}, blotterCodeSelecteds = {}5",token.UserID, permissionName, accountGroupIds.Join(,,ex);
                Logger.Error(ex);
                return null;
            }
        }


        private DataSet LoadDBData(Token token, string permissionName, Guid[] accountGroupIds, Guid[] instrumentGroupIDs)
        {
            var groupXmlItems = this.BuildAccountGroupXmlAndInstrumentGroupXml(accountGroupIds, instrumentGroupIDs);
            var accountGroupIDsXml = groupXmlItems.Item1;
            var instrumentGroupXml = groupXmlItems.Item2;
            SqlCommand command = new SqlCommand();
            command.Connection = new SqlConnection(_connnectionString);
            command.CommandTimeout = 600;

            command.CommandType = System.Data.CommandType.StoredProcedure;
            command.CommandText = "dbo.P_GetGroupNetPositionInfoForManager";
            command.Parameters.AddWithValue("@userID", token.UserID);
            command.Parameters.AddWithValue("@permissionName", permissionName);
            command.Parameters.AddWithValue("@userType", 0);
            command.Parameters.Add("@accountGroupXml", SqlDbType.NText);
            command.Parameters["@accountGroupXml"].Value = accountGroupIDsXml;
            command.Parameters.Add("@InstrumentGroupXml", SqlDbType.NText);
            command.Parameters["@InstrumentGroupXml"].Value = instrumentGroupXml;

            SqlDataAdapter dataAdapter = new SqlDataAdapter();
            dataAdapter.SelectCommand = command;
            DataSet dataSet = new DataSet();
            dataAdapter.Fill(dataSet);
            return dataSet;
        }

        private Tuple<string, string> BuildAccountGroupXmlAndInstrumentGroupXml(Guid[] accountGroupIds, Guid[] instrumentGroupIDs)
        {
            string accountGroupIDsXml = null;

            string[] accountGroupIDsArray = new string[accountGroupIds.Length];
            //accountIDs.CopyTo(accountIDsArray, 0);
            for (int i = 0, count = accountGroupIds.Length; i < count; i++)
            {
                accountGroupIDsArray[i] = accountGroupIds[i].ToString();
            }
            accountGroupIDsXml = XmlTransform.Transform(accountGroupIDsArray, "AccountGroups", "AccountGroup", "ID");

            string[] instrumentGroupArray = new string[instrumentGroupIDs.Length];
            for (int i = 0; i < instrumentGroupIDs.Length; i++)
            {
                instrumentGroupArray[i] = instrumentGroupIDs[i].ToString();
            }
            string instrumentGroupXml = XmlTransform.Transform(instrumentGroupArray, "InstrumentGroups", "InstrumentGroup", "ID");
            return Tuple.Create(accountGroupIDsXml, instrumentGroupXml);
        }

        private Dictionary<Guid, AccountGroupGNP> BuildAccountGroupDict(DataSet dataSet, Guid[] instrumentIDs, string[] blotterCodeSelecteds)
        {
            DataRowCollection accountDataRowCollection = dataSet.Tables[0].Rows;
            Dictionary<Guid, AccountGroupGNP> accountgroupDict = new Dictionary<Guid, AccountGroupGNP>(accountDataRowCollection.Count);
            foreach (DataRow dataRow in accountDataRowCollection)
            {
                Guid accountID = (Guid)dataRow["AccountID"];
                string accountCode = dataRow["AccountCode"].ToString();
                Guid groupID = (Guid)dataRow["GroupID"];
                string groupCode = dataRow["GroupCode"].ToString();
                this.InitAccount(accountID);
                AccountGroupGNP group;
                if (accountgroupDict.ContainsKey(groupID))
                {
                    group = accountgroupDict[groupID];
                }
                else
                {
                    group = new AccountGroupGNP(groupID, groupCode);
                    accountgroupDict.Add(groupID, group);
                }

                var account = AccountRepository.Default.Get(accountID);
                var trans = account.GetTrans(instrumentIDs);
                if (trans.Count > 0)
                {
                    Dictionary<Guid, InstrumentGNP> instrumentGNPDict = this.BuildInstrumentGNPDict((Account)account, trans, blotterCodeSelecteds);
                    if (instrumentGNPDict.Count > 0)
                    {
                        AccountGNP accountGNP = new AccountGNP(account.Id, account.Code, account.Type);
                        accountGNP.instrumentGNPs = instrumentGNPDict;
                        group.accountGNPs.Add(account.Id, accountGNP);
                    }
                }
            }
            return accountgroupDict;
        }

        private Dictionary<Guid, InstrumentGNP> BuildInstrumentGNPDict(Account account, List<Protocal.Commands.Transaction> trans, string[] blotterCodeSelecteds)
        {
            Dictionary<Guid, InstrumentGNP> instrumentGNPs = new Dictionary<Guid, InstrumentGNP>();
            foreach (Transaction tran in trans)
            {
                bool isExistsOpenOrder;
                decimal buyQuantity, buyMultiplyValue, sellQuantity, sellMultiplyValue, sellLot, buyLot, sellSumEl, buySumEl;

                tran.GroupNetPositionForManager(blotterCodeSelecteds, out isExistsOpenOrder, out buyQuantity, out buyMultiplyValue, out sellQuantity, out sellMultiplyValue, out buyLot, out sellLot, out buySumEl, out sellSumEl);
                if (!isExistsOpenOrder) continue;

                InstrumentGNP instrumentGNP;
                if (instrumentGNPs.ContainsKey(tran.InstrumentId))
                {
                    instrumentGNP = instrumentGNPs[tran.InstrumentId];
                }
                else
                {
                    instrumentGNP = new InstrumentGNP(tran.InstrumentId, tran.Instrument.NumeratorUnit, tran.Instrument.Denominator);
                    instrumentGNPs.Add(tran.InstrumentId, instrumentGNP);
                }
                instrumentGNP.BuyQuantity += buyQuantity;
                instrumentGNP.BuyMultiplyValue += buyMultiplyValue;
                instrumentGNP.SellQuantity += sellQuantity;
                instrumentGNP.SellMultiplyValue += sellMultiplyValue;
                instrumentGNP.SellLot += sellLot;
                instrumentGNP.BuyLot += buyLot;
                instrumentGNP.SellSumEl += sellSumEl;
                instrumentGNP.BuySumEl += buySumEl;
                if (account.Type == AccountType.Company)
                {
                    instrumentGNP.AddQuotation(sellQuantity - buyQuantity);
                    instrumentGNP.AddLotBalance(sellLot - buyLot);
                }
                else
                {
                    instrumentGNP.AddQuotation(buyQuantity - sellQuantity);
                    instrumentGNP.AddLotBalance(buyLot - sellLot);
                }
            }
            return instrumentGNPs;
        }

        private Guid[] BuildInstrumentIds(DataSet dataSet)
        {
            DataRowCollection instrumentDataRowCollection = dataSet.Tables[1].Rows;
            Guid[] instrumentIDs = new Guid[instrumentDataRowCollection.Count];
            int index = 0;
            foreach (DataRow dataRow in instrumentDataRowCollection)
            {
                Guid instrumentID = (Guid)dataRow["InstrumentID"];
                instrumentIDs.SetValue(instrumentID, index);
                index++;
            }
            return instrumentIDs;
        }

        private void InitAccount(Guid accountID)
        {
            try
            {
                if (!AccountRepository.Default.Contains(accountID))
                {
                    var initData = _proxy.GetInitializeData(new List<Guid> { accountID });
                    var account = AccountRepository.Default.GetOrAdd(accountID);
                    if (string.IsNullOrEmpty(initData)) return;
                    var root = XElement.Parse(initData);
                    if (root.HasElements)
                    {
                        account.Initialize(root.Element("Account"));
                    }
                }
            }
            catch
            {
                Logger.ErrorFormat("init account id = {0}", accountID);
                throw;
            }

        }

    }
}