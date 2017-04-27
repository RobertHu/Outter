using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Xml;
using System.Xml.Linq;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    internal enum VolumeNecessaryOption
    {
        Progessive = 0,
        Flat
    }

    public sealed class VolumeNecessary
    {
        internal VolumeNecessary(IDBRow  dataRow)
        {
            this.VolumeNecessaryDetails = new List<VolumeNecessaryDetail>();
            this.Update(dataRow);
        }

        internal VolumeNecessary(XElement row)
        {
            this.VolumeNecessaryDetails = new List<VolumeNecessaryDetail>();
            this.Update(row);
        }

        internal Guid Id
        {
            get;
            private set;
        }

        internal VolumeNecessaryOption Option
        {
            get;
            private set;
        }

        internal void Update(IDBRow  volumeNecessaryRow)
        {
            this.Id = (Guid)volumeNecessaryRow["ID"];
            this.Option = (VolumeNecessaryOption)((byte)volumeNecessaryRow["Option"]);
        }

        internal void Update(XElement tradePolicyDetailNode)
        {
            foreach (XAttribute attribute in tradePolicyDetailNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "ID":
                        this.Id = new Guid(attribute.Value);
                        break;
                    case "Option":
                        this.Option = (VolumeNecessaryOption)int.Parse(attribute.Value);
                        break;
                    default:
                        break;
                }
            }
        }

        internal VolumeNecessaryDetail this[Guid volumeNecessaryDetailId]
        {
            get
            {
                int index = this.VolumeNecessaryDetails.FindIndex(delegate(VolumeNecessaryDetail item) { return item.Id == volumeNecessaryDetailId; });
                return index == -1 ? null : this.VolumeNecessaryDetails[index];
            }
        }

        internal List<VolumeNecessaryDetail> VolumeNecessaryDetails { get; private set; }

        internal void Add(VolumeNecessaryDetail volumeNecessaryDetail)
        {
            int index = this.VolumeNecessaryDetails.FindIndex(delegate(VolumeNecessaryDetail item) { return item.From > volumeNecessaryDetail.From; });
            if (index == -1)
            {
                this.VolumeNecessaryDetails.Add(volumeNecessaryDetail);
            }
            else
            {
                this.VolumeNecessaryDetails.Insert(index, volumeNecessaryDetail);
            }
        }

        internal void Remove(VolumeNecessaryDetail volumeNecessaryDetail)
        {
            this.VolumeNecessaryDetails.Remove(volumeNecessaryDetail);
        }

        internal void Update(VolumeNecessaryDetail volumeNecessaryDetail)
        {
            int index = this.VolumeNecessaryDetails.FindIndex(delegate(VolumeNecessaryDetail item) { return item.Id == volumeNecessaryDetail.Id; });
            if (index == -1)
            {
                this.Add(volumeNecessaryDetail);
            }
            else
            {
                this.VolumeNecessaryDetails[index] = volumeNecessaryDetail;
            }
        }

        internal decimal CalculateNecessary(decimal marginRate, decimal defaultMargin, decimal netLot, bool useDayMargin)
        {
            if (this.VolumeNecessaryDetails.Count == 0) return marginRate * defaultMargin * netLot;

            if (this.Option == VolumeNecessaryOption.Flat)
            {
                VolumeNecessaryDetail volumeNecessaryDetail = this.VolumeNecessaryDetails.FindLast(delegate(VolumeNecessaryDetail item) { return netLot > item.From; });
                decimal margin = volumeNecessaryDetail == null ? defaultMargin : (useDayMargin ? volumeNecessaryDetail.MarginD : volumeNecessaryDetail.MarginO);
                return marginRate * margin * netLot;
            }
            else if (this.Option == VolumeNecessaryOption.Progessive)
            {
                decimal necessary = marginRate * defaultMargin * Math.Min(netLot, this.VolumeNecessaryDetails[0].From);

                int index = 0;
                while (index < this.VolumeNecessaryDetails.Count && netLot > this.VolumeNecessaryDetails[index].From)
                {
                    decimal margin = useDayMargin ? this.VolumeNecessaryDetails[index].MarginD : this.VolumeNecessaryDetails[index].MarginO;
                    decimal lot = netLot - this.VolumeNecessaryDetails[index].From;
                    if (index < this.VolumeNecessaryDetails.Count - 1)
                    {
                        lot = Math.Min(lot, (this.VolumeNecessaryDetails[index + 1].From - this.VolumeNecessaryDetails[index].From));
                    }
                    necessary += marginRate * margin * lot;

                    index++;
                }

                return necessary;
            }
            else
            {
                throw new NotSupportedException(string.Format("Option = {0} is not supported", this.Option));
            }
        }

        internal void Remove(Guid volumeNecessaryDetailId)
        {
            int index = this.VolumeNecessaryDetails.FindIndex(delegate(VolumeNecessaryDetail item) { return item.Id == volumeNecessaryDetailId; });
            if (index >= 0)
            {
                this.VolumeNecessaryDetails.RemoveAt(index);
            }
        }

        internal void ResortDetails()
        {
            this.VolumeNecessaryDetails.Sort(VolumeNecessaryDetailComparer.Default);
        }
    }

    internal sealed class VolumeNecessaryDetailComparer : IComparer<VolumeNecessaryDetail>
    {
        internal static readonly VolumeNecessaryDetailComparer Default = new VolumeNecessaryDetailComparer();

        private VolumeNecessaryDetailComparer()
        {
        }

        int IComparer<VolumeNecessaryDetail>.Compare(VolumeNecessaryDetail x, VolumeNecessaryDetail y)
        {
            return x.From.CompareTo(y.From);
        }
    }

    internal sealed class VolumeNecessaryDetail
    {
        internal VolumeNecessaryDetail(IDBRow  dataRow)
        {
            this.Update(dataRow);
        }

        internal VolumeNecessaryDetail(XElement row)
        {
            this.Update(row);
        }

        internal Guid Id
        {
            get;
            private set;
        }

        internal Guid VolumeNecessaryId
        {
            get;
            private set;
        }

        internal decimal From
        {
            get;
            private set;
        }

        internal decimal MarginD
        {
            get;
            private set;
        }

        internal decimal MarginO
        {
            get;
            private set;
        }

        internal void Update(IDBRow  volumeNecessaryDetailRow)
        {
            this.Id = (Guid)volumeNecessaryDetailRow["Id"];
            this.VolumeNecessaryId = (Guid)volumeNecessaryDetailRow["VolumeNecessaryId"];
            this.From = (decimal)volumeNecessaryDetailRow["From"];
            this.MarginD = (decimal)volumeNecessaryDetailRow["MarginD"];
            this.MarginO = (decimal)volumeNecessaryDetailRow["MarginO"];
        }

        internal void Update(XElement tradePolicyDetailNode)
        {
            foreach (XAttribute attribute in tradePolicyDetailNode.Attributes())
            {
                switch (attribute.Name.ToString())
                {
                    case "ID":
                        this.Id = new Guid(attribute.Value);
                        break;
                    case "From":
                        this.From = decimal.Parse(attribute.Value);
                        break;
                    case "MarginD":
                        this.MarginD = decimal.Parse(attribute.Value);
                        break;
                    case "MarginO":
                        this.MarginO = decimal.Parse(attribute.Value);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}