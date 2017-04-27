using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using Protocal.TypeExtensions;

namespace Core.TransactionServer.Agent.BinaryOption
{
    public sealed class BOBetRepository
    {
        public static readonly BOBetRepository Default = new BOBetRepository();

        private Dictionary<Guid, BOBet> _itemDict = new Dictionary<Guid, BOBet>();

        private BOBetRepository() { }

        public BOBet Get(Guid binaryOptionTypeId)
        {
            return _itemDict[binaryOptionTypeId];
        }

        internal void Initialize(DataTable table)
        {
            _itemDict.Clear();
            foreach (DataRow row in table.Rows)
            {
                BOBet item = new BOBet(row);
                _itemDict.Add(item.ID, item);
            }
        }

        public void Update(XElement node, string methodName)
        {
            if (node.Name == "BOBetType")
            {
                this.UpdateBOBetType(node, methodName);
            }
            else if (node.Name == "BOBetTypes")
            {
                foreach (var child in node.Elements())
                {
                    this.UpdateBOBetType(child, methodName);
                }
            }
        }

        private void UpdateBOBetType(XElement node, string methodName)
        {
            Guid id = node.AttrToGuid("ID");
            if (methodName == "Add")
            {
                BOBet item = new BOBet(id, node);
                _itemDict.Add(id, item);
            }
            else if (methodName == "Delete")
            {
                _itemDict.Remove(id);
            }
            else if (methodName == "Modify")
            {
                _itemDict[id].Update(node);
            }
        }
    }

}
