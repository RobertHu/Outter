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
    internal static class BOBetTypeRepository
    {
        private static Dictionary<Guid, BOBetType> binaryOptionBetTypes = new Dictionary<Guid, BOBetType>(30);

        internal static BOBetType Get(Guid binaryOptionTypeId)
        {
            return binaryOptionBetTypes[binaryOptionTypeId];
        }

        internal static void Read(IDBRow row)
        {
            BOBetType item = new BOBetType(row);
            binaryOptionBetTypes.Add(item.ID, item);
        }

        internal static void Update(XElement node, string methodName)
        {
            if (node.Name == "BOBetType")
            {
                UpdateBOBetType(node, methodName);
            }
            else if (node.Name == "BOBetTypes")
            {
                foreach (XElement child in node.Elements("BOBetType"))
                {
                    UpdateBOBetType(child, methodName);
                }
            }
        }

        private static void UpdateBOBetType(XElement node, string methodName)
        {
            Guid id = XmlConvert.ToGuid(node.Attribute("ID").Value);
            if (methodName == "Add")
            {
                BOBetType item = new BOBetType(id, node);
                binaryOptionBetTypes[id] = item;
            }
            else if (methodName == "Delete")
            {
                binaryOptionBetTypes.Remove(id);
            }
            else if (methodName == "Modify")
            {
                binaryOptionBetTypes[id].Update(node);
            }
        }
    }
}
