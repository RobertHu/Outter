using Core.TransactionServer.Agent.Periphery;
using Core.TransactionServer.Agent.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Test
{
    public static class ExternalInitializer
    {
        public static void InitializeDBConnectionString(string connectionString)
        {
            ExternalSettings.Default.DBConnectionString = connectionString;
        }


        private static void LoadOrgnizationAndOrderType(XElement node)
        {
            foreach (XElement methodNode in node.Elements())
            {
                if (methodNode.Name == "Add")
                {
                    foreach (XElement eachEntityNode in methodNode.Elements())
                    {
                        if (eachEntityNode.Name == "Organization")
                        {
                            Guid orgId = Guid.Parse(eachEntityNode.Attribute("ID").Value);
                            string orgCode = eachEntityNode.Attribute("Code").Value;
                            OrganizationAndOrderTypeRepository.Default.AddOrganization(orgId, orgCode);
                        }
                        else if (eachEntityNode.Name == "OrderType")
                        {
                            int id = int.Parse(eachEntityNode.Attribute("ID").Value);
                            string code = eachEntityNode.Attribute("Code").Value;
                            OrganizationAndOrderTypeRepository.Default.AddOrderType(id, code);
                        }
                    }
                }
            }
        }


    }
}
