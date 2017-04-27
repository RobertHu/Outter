using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Protocal.Commands.Notify
{
    [DataContract]
    public sealed class NewsNotifyCommand : NotifyCommand
    {
        [DataMember]
        public List<News> News { get; set; }

        protected override void CollectCustomerIds()
        {
            throw new NotImplementedException();
        }
    }

    [DataContract]
    public sealed class News
    {
        [DataMember]
        public Guid CategoryId { get; set; }

        [DataMember]
        public string Contents { get; set; }

        [DataMember]
        public DateTime ExpireTime { get; set; }

        [DataMember]
        public Guid Id { get; set; }

        [DataMember]
        public bool IsExpired { get; set;}

        [DataMember]
        public string Language { get; set; }

        [DataMember]
        public DateTime ModifyTime { get; set; }

        [DataMember]
        public Guid PublisherId { get; set; }

        [DataMember]
        public DateTime PublishTime { get; set; }

        [DataMember]
        public string Title { get; set; }
    }
}
