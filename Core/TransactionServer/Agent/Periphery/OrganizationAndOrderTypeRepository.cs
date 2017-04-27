using Core.TransactionServer.Agent.DB;
using log4net;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Periphery
{
    public sealed class OrganizationAndOrderTypeRepository
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(OrganizationAndOrderTypeRepository));
        private Dictionary<Guid, string> _organizationRepository = new Dictionary<Guid, string>();
        private Dictionary<int, string> _orderTypeRepository = new Dictionary<int, string>();
        private object _mutex = new object();

        public static readonly OrganizationAndOrderTypeRepository Default = new OrganizationAndOrderTypeRepository();
        private OrganizationAndOrderTypeRepository() { }

        public string GetOrganizationCode(Guid id)
        {
            lock (_mutex)
            {
                if (!_organizationRepository.ContainsKey(id))
                {
                    Logger.InfoFormat("GetOrganizationCode orgId = {0} not found", id);
                    this.LoadOrgsFromDB();
                    if (!_organizationRepository.ContainsKey(id))
                    {
                        throw new KeyNotFoundException(string.Format("organizationId = {0} not found", id));
                    }
                    else
                    {
                        return _organizationRepository[id];
                    }
                }
                return _organizationRepository[id];
            }
        }

        private void LoadOrgsFromDB()
        {
            Logger.Info("begin LoadOrgsFromDB");
            var orgs = DBRepository.Default.LoadOrganizations();
            if (orgs != null && orgs.Count() > 0)
            {
                Logger.Info("LoadOrgsFromDB success");
                _organizationRepository.Clear();
                foreach (var eachOrg in orgs)
                {
                    _organizationRepository.Add(eachOrg.ID, eachOrg.Code);
                }
            }
        }


        public string GetOrderTypeCode(int id)
        {
            if (!_orderTypeRepository.ContainsKey(id))
            {
                throw new KeyNotFoundException(string.Format("orderType = {0} not found", id));
            }
            return _orderTypeRepository[id];
        }

        internal void InitializeOrganization(IDBRow dr)
        {
            var id = (Guid)dr["ID"];
            var code = (string)dr["Code"];
            this.AddOrganization(id, code);
        }

        internal void InitializeOrderType(IDBRow dr)
        {
            int orderTypeId = (int)dr["ID"];
            string orderTypeCode = (string)dr["Code"];
            this.AddOrderType(orderTypeId, orderTypeCode);
        }


        private void LoadDataCommon(DataSet ds, string tableName)
        {
            var table = ds.Tables[tableName];
            foreach (DataRow eachRow in table.Rows)
            {
                if (tableName == "OrderType")
                {
                    this.InitializeOrderType(new DBRow(eachRow));
                }
                else
                {
                    this.InitializeOrganization(new DBRow(eachRow));
                }
            }
        }

        public void AddOrderType(int orderTypeId, string orderTypeCode)
        {
            if (!_orderTypeRepository.ContainsKey(orderTypeId))
            {
                _orderTypeRepository.Add(orderTypeId, new string(orderTypeCode.ToCharArray(), 0, 2));
            }

        }

        public void AddOrganization(Guid organizationId, string organizationCode)
        {
            if (!_organizationRepository.ContainsKey(organizationId))
            {
                _organizationRepository.Add(organizationId, organizationCode);
            }
        }

    }
}
