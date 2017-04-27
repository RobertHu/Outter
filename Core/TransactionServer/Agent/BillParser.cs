using Core.TransactionServer.Agent.Framework;
using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Protocal.TypeExtensions;
using System.Collections;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent
{
    internal sealed class BillParser
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BillParser));

        internal static readonly BillParser Default = new BillParser();

        static BillParser() { }
        private BillParser() { }

        internal void Initialize(IDataReader dr)
        {
            try
            {
                Bill bill;
                if (dr["IsValued"] != DBNull.Value)
                {
                    bill = new PLBill(new DBReader(dr));
                }
                else
                {
                    bill = new Bill(new DBReader(dr));
                }
                var account = TradingSetting.Default.GetAccount(bill.AccountId);
                if (account == null)
                {
                    throw new NullReferenceException(string.Format("account id = {0} not exist", bill.AccountId));
                }
                if (bill.OwnerType == BillOwnerType.Order)
                {
                    if (dr["DependenceID"] == DBNull.Value)
                    {
                        Logger.ErrorFormat("Parse bill error, dependenceid is null, billId = {0}", bill.Id);
                        return;
                    }
                    Guid dependenceId = (Guid)dr["DependenceID"];
                    account.AddOrderBill(dependenceId, bill, OperationType.None);
                }
                else if (bill.OwnerType == BillOwnerType.Account)
                {
                    account.AddBill(bill, OperationType.None);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        internal void Parse(DataSet ds)
        {
            try
            {
                DataTable billTable = ds.Tables["Bill"];
                Logger.WarnFormat("bills row count = {0}", billTable.Rows.Count);
                if (billTable.Rows.Count == 0) return;
                foreach (DataRow eachBillRow in billTable.Rows)
                {
                    Bill bill;
                    if (eachBillRow["IsValued"] != DBNull.Value)
                    {
                        bill = new PLBill(new DBRow(eachBillRow));
                    }
                    else
                    {
                        bill = new Bill(new DBRow(eachBillRow));
                    }
                    var account = TradingSetting.Default.GetAccount(bill.AccountId);
                    if (bill.OwnerType == BillOwnerType.Order)
                    {
                        Guid dependenceId = (Guid)eachBillRow["DependenceID"];
                        account.AddOrderBill(dependenceId, bill, OperationType.AsNewRecord);
                    }
                    else if (bill.OwnerType == BillOwnerType.Account)
                    {
                        account.AddBill(bill, OperationType.AsNewRecord);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }

        }

        private Account GetAccount(Guid accountId)
        {
            return TradingSetting.Default.GetAccount(accountId);
        }

    }
}
