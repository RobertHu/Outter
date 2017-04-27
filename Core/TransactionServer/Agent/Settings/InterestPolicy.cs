using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Protocal.TypeExtensions;
using System.Diagnostics;
using Protocal.CommonSetting;

namespace Core.TransactionServer.Agent.Settings
{
    internal sealed class InterestPolicy
    {
        internal InterestPolicy(IDBRow  dr)
        {
            this.Id = dr.GetColumn<Guid>("ID");
            this.Code = dr.GetColumn<string>("Code");
            this.Mon = dr.GetColumn<Int16>("Mon");
            this.Tue = dr.GetColumn<Int16>("Tue");
            this.Wed = dr.GetColumn<Int16>("Wed");
            this.Thu = dr.GetColumn<Int16>("Thu");
            this.Fri = dr.GetColumn<Int16>("Fri");
            this.Sat = dr.GetColumn<Int16>("Sat");
            this.Sun = dr.GetColumn<Int16>("Sun");
            this.UpdateTime = dr.GetColumn<DateTime>("UpdateTime");
        }

        internal Guid Id { get; private set; }
        internal string Code { get; private set; }
        internal int Mon { get; private set; }
        internal int Tue { get; private set; }
        internal int Wed { get; private set; }
        internal int Thu { get; private set; }
        internal int Fri { get; private set; }
        internal int Sat { get; private set; }
        internal int Sun { get; private set; }
        internal DateTime UpdateTime { get; private set; }

    }
}
