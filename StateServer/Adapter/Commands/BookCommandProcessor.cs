using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using log4net;
using System.Xml.Linq;
using Protocal.TypeExtensions;
using System.Xml;

namespace iExchange.StateServer.Adapter.Commands
{
    internal sealed class BookCommandProcessor
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(BookCommandProcessor));

        internal static readonly BookCommandProcessor Default = new BookCommandProcessor();

        static BookCommandProcessor() { }
        private BookCommandProcessor() { }

        internal void Process(Protocal.TradingCommand bookCommand, ICommandBroadcast broadcast)
        {
            try
            {
                if (string.IsNullOrEmpty(bookCommand.Content)) return;
                XElement accountElement = XElement.Parse(bookCommand.Content);
                Guid accountId = accountElement.AttrToGuid("ID");
                var account = (Account)AccountRepository.Default.Get(accountId);
                if (account == null) return;
                Protocal.Commands.ChangedFund changedFund;
                var changes = account.Update(accountElement, out changedFund);
                var tran = (Transaction)changes.Single().Tran;
                Protocal.Commands.TransactionMapping.Default.Update(account, changes);
                var affectedOrders = this.CreateAffectedOrders(tran);
                broadcast.BoardcastBookResult(StateServer.Token, tran.ToXml().ToXmlNode(), account.ToXml().ToXmlNode(), affectedOrders);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


        private XmlNode CreateAffectedOrders(Transaction tran)
        {
            XElement result = new XElement("AffectedOrders");
            foreach (var eachOrder in tran.Orders)
            {
                if (eachOrder.IsOpen) continue;
                foreach (OrderRelation eachOrderRelation in eachOrder.OrderRelations)
                {
                    result.Add(((Transaction)eachOrderRelation.OpenOrder.Owner).ToXml());
                }
            }
            if (result.HasElements) return result.ToXmlNode();
            return null;
        }


    }
}