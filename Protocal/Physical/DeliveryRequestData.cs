using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.Physical
{
    [DataContract]
    public sealed class DeliveryRequestData
    {
        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public Guid AccountId { get; set; }

        [DataMember]
        public Guid InstrumentId { get; set; }

        [DataMember]
        public decimal RequireQuantity { get; set; }

        [DataMember]
        public decimal RequireLot { get; set; }

        [DataMember]
        public Guid ChargeCurrencyId { get; set; }

        [DataMember]
        public decimal Charge { get; set; }

        [DataMember]
        public Guid? DeliveryAddressId { get; set; }

        [DataMember]
        public Guid SubmitorId { get; set; }

        [DataMember]
        public DateTime? DeliveryTime { get; set; }

        [DataMember]
        public List<DeliveryRequestOrderRelationData> OrderRelations { get; set; }

        [DataMember]
        public List<DeliveryRequestSpecificationData> Specifications { get; set; }
    }


    [DataContract]
    public sealed class DeliveryRequestOrderRelationData
    {
        [DataMember]
        public Guid DeliveryRequestId { get; set; }

        [DataMember]
        public Guid OpenOrderId { get; set; }

        [DataMember]
        public decimal DeliveryQuantity { get; set; }

        [DataMember]
        public decimal DeliveryLot { get; set; }
    }


    [DataContract]
    public sealed class DeliveryRequestSpecificationData
    {
        [DataMember]
        public Guid UnitId { get; set; }

        [DataMember]
        public int Quantity { get; set; }

        [DataMember]
        public decimal Size { get; set; }
    }


}
