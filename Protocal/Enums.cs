using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Protocal
{
    public enum DownPaymentBasis
    {
        PercentageOfAmount = 0,
        FixedAmountPerLot = 1
    }

    public enum BillType
    {
        None = 0,
        Commission = 1,
        Levy = 2,
        PaidPledge = 6,
        PayBackPledge = 7,
        OverdueCutPenalty = 8,
        ClosePenalty = 9,
        FrozenFund = 10,
        InstalmentAdministrationFee = 11,
        InterestPL = 21,
        StoragePL = 22,
        TradePL = 23,
        PhysicalPaymentDiscount = 37,
        OtherFee = 40,
        Deposit = 41,
        Adjustment = 42,
        DeltaBalance = 44,
        DebitInterest = 45,
        PrePay = 46,
        Instalment = 47
    }


    public enum AdjustmentType
    {
        None = 0,
        Other = 1,
        DepositInterest = 2,
        Interest = 3,
        Commission = 4,
        TradePL = 5,
        Storage = 6,
        Offset = 7,
        CashSettlement = 8,
        PledgeOrPhysicalValue = 9,
        FrozenCapital = 10,
        AssayFee = 11,
        DeliveryCommission = 12,
        CancellationCharge = 13,
        AdministrativeExpenses = 14,
        InstalmentFee = 15,
        InstalmentPayment = 16,
        PrepaymentFee = 17
    }


    public enum DepositType
    {
        None,
        Cash = 1,
        Cheque = 2,
        VISA = 3,
        Credit = 4,
        Temp = 5,
        Offset = 6,
        Transfer = 7
    }



    public enum AlertState
    {
        Pending,
        Hit,
        Processed,
        Expired
    }

}
