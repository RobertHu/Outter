using System;
using System.Collections.Generic;
using System.Web;

namespace iExchange.StateServer
{
    /// <summary>
    /// Represents the executed transaction data in filiale. 
    /// </summary>
    [Serializable]
    public class FilialeTransaction
    {
        private Guid _Id;
        private string _XmlTran;

        public Guid Id
        {
            get { return _Id; }
            set { _Id = value; }
        }

        public string XmlTran
        {
            get { return _XmlTran; }
            set { _XmlTran = value; }
        }

        public override string ToString()
        {
            return String.Format("Id:{0}, XmlTran:{1}", this.Id, this.XmlTran);
        }
    }
}
