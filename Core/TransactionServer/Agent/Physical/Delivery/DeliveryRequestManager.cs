using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Core.TransactionServer.Agent.Physical.Delivery
{
    internal sealed class DeliveryRequestManager
    {
        private Dictionary<Guid, DeliveryRequest> _deliveryRequestDict = new Dictionary<Guid, DeliveryRequest>();

        private static Lazy<DeliveryRequestManager> defaultManager = new Lazy<DeliveryRequestManager>(() => new DeliveryRequestManager(), true);

        private object _mutex = new object();

        private DeliveryRequestManager()
        {
        }

        public static DeliveryRequestManager Default
        {
            get { return DeliveryRequestManager.defaultManager.Value; }
        }

        internal DeliveryRequest this[Guid deliveryRequestId]
        {
            get
            {
                lock (_mutex)
                {
                    DeliveryRequest result;
                    _deliveryRequestDict.TryGetValue(deliveryRequestId, out result);
                    return result;
                }
            }
        }

        internal IEnumerable<DeliveryRequest> DeliveryRequests
        {
            get
            {
                lock (_mutex)
                {
                    return _deliveryRequestDict.Values;
                }
            }
        }


        internal bool TryGet(Guid deliveryRequestId, out DeliveryRequest deliveryRequest)
        {
            lock (_mutex)
            {
                return this._deliveryRequestDict.TryGetValue(deliveryRequestId, out deliveryRequest);
            }
        }

        internal DeliveryRequest Create(Account account, Protocal.Physical.DeliveryRequestData requestNode)
        {
            DeliveryRequest deliveryRequest = new DeliveryRequest(account, requestNode);
            if (requestNode.OrderRelations != null)
            {
                foreach (var eachOrderRelation in requestNode.OrderRelations)
                {
                    new DeliveryRequestOrderRelation(deliveryRequest, eachOrderRelation);
                }
            }

            if (requestNode.Specifications != null)
            {
                foreach (var eachSpecification in requestNode.Specifications)
                {
                    new DeliveryRequestSpecification(deliveryRequest, eachSpecification);
                }
            }
            return deliveryRequest;
        }

        public void Add(DeliveryRequest deliveryRequest)
        {
            lock (_mutex)
            {
                this._deliveryRequestDict.Add(deliveryRequest.Id, deliveryRequest);
            }
        }

        internal void Remove(DeliveryRequest deliveryRequest)
        {
            lock (_mutex)
            {
                this._deliveryRequestDict.Remove(deliveryRequest.Id);
            }
        }

        internal void Clear()
        {
            lock (_mutex)
            {
                this._deliveryRequestDict.Clear();
            }
        }
    }
}
