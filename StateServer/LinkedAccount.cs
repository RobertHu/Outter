using System;

namespace iExchange.StateServer
{
    /// <summary>
    /// Summary description for LinkedAccount.
    /// </summary>
    public class LinkedAccount
    {
        public Guid AccountID;
        public Guid LinkedAccountID;
        public bool IsLocal;
        public bool IsOpposite;
        public double QuantityFactor;

        public LinkedAccount()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        public override string ToString()
        {
            return string.Format("AccountId={0},LinkedAccountID={1},IsLocal={2},IsOpposite={3},QuantityFactor={4}",
                this.AccountID, this.LinkedAccountID, this.IsLocal, this.IsOpposite, this.QuantityFactor);
        }
    }
}
