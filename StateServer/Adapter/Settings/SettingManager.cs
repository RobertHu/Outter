using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Protocal.CommonSetting;
using System.Xml;
using log4net;
using System.Data;

namespace iExchange.StateServer.Adapter.Settings
{
    internal sealed class SettingManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SettingManager));
        internal static readonly SettingManager Default = new SettingManager();

        static SettingManager() { }
        private SettingManager() { }

        internal SystemParameter SystemParameter { get; private set; }

        internal void Initialize(DataSet ds)
        {
            this.SystemParameter = new SystemParameter(new DBRow(ds.Tables["SystemParameter"].Rows[0]));
        }


        internal void Update(XmlNode updateNode)
        {
            try
            {
                foreach (XmlNode eachMethodNode in updateNode.ChildNodes)
                {
                    foreach (XmlNode eachModelNode in eachMethodNode.ChildNodes)
                    {
                        switch (eachModelNode.Name)
                        {
                            case "AccountBalance":
                                this.ProcessAccountBalance(eachMethodNode.Name, eachModelNode);
                                break;
                            case "SystemParameter":
                                if (eachMethodNode.Name == "Modify")
                                {
                                    this.SystemParameter.Update(eachModelNode);
                                }
                                break;
                            case "Instruments":
                                foreach (XmlNode eachSubModelNode in eachModelNode.ChildNodes)
                                {
                                    this.ProcessInstruments(eachMethodNode.Name, eachSubModelNode);
                                }
                                break;
                            case "Instrument":
                                this.ProcessInstruments(eachMethodNode.Name, eachModelNode);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void ProcessInstruments(string methodName, XmlNode instrumentNode)
        {
            if (methodName == "Modify")
            {
                Guid id = XmlConvert.ToGuid(instrumentNode.Attributes["ID"].Value);
                Instrument instrument = InstrumentManager.Default.Get(id);
                if (instrument != null)
                {
                    instrument.Update(instrumentNode);
                }
            }
            else if (methodName == "Add")
            {
                Guid id = XmlConvert.ToGuid(instrumentNode.Attributes["ID"].Value);
                Instrument instrument = new Instrument(id, instrumentNode);
                InstrumentManager.Default.Add(instrument);
            }
            if (methodName == "Delete")
            {
                Guid id = XmlConvert.ToGuid(instrumentNode.Attributes["ID"].Value);
                InstrumentManager.Default.Remove(id);
            }

        }



        private void ProcessAccountBalance(string methodName, XmlNode node)
        {
            Guid accountID = XmlConvert.ToGuid(node.Attributes["AccountID"].Value);
            Guid currencyID = XmlConvert.ToGuid(node.Attributes["CurrencyID"].Value);
            if (methodName == "Modify" || methodName == "Add")
            {
                decimal balance = XmlConvert.ToDecimal(node.Attributes["Balance"].Value);
                if (Settings.SettingManager.Default.SystemParameter.EnableEmailNotify)
                {
                    if (!AccountRepository.Default.Contains(accountID)) return;
                    Account account = (Account)AccountRepository.Default.Get(accountID);
                    Fund fund = (Fund)account.GetFund(currencyID);
                    FaxEmailServices.FaxEmailEngine.Default.NotifyBalanceChanged(account, DateTime.Now, fund.CurrencyCode, balance);
                }
            }
        }

    }
}