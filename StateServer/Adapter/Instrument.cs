using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data;
using System.Xml;
using iExchange.Common;
using log4net;

namespace iExchange.StateServer.Adapter
{
    internal sealed class InstrumentManager
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InstrumentManager));
        private object _mutex = new object();

        internal static readonly InstrumentManager Default = new InstrumentManager();

        private Dictionary<Guid, Instrument> _instruments = new Dictionary<Guid, Instrument>();

        static InstrumentManager() { }
        private InstrumentManager() { }

        internal void Initialize(DataTable instruments)
        {
            lock (_mutex)
            {
                foreach (DataRow row in instruments.Rows)
                {
                    Instrument instrument = new Instrument(row);
                    this._instruments.Add(instrument.Id, instrument);
                }
            }
        }

        internal void Add(Instrument instrument)
        {
            lock (_mutex)
            {
                if (!_instruments.ContainsKey(instrument.Id))
                {
                    Logger.InfoFormat("add instrument = {0}", instrument.Id);
                    _instruments.Add(instrument.Id, instrument);
                }
            }
        }

        internal void Remove(Guid id)
        {
            lock (_mutex)
            {
                if (_instruments.ContainsKey(id))
                {
                    Logger.InfoFormat("remove instrument = {0}", id);
                    _instruments.Remove(id);
                }
            }
        }

        internal Instrument Get(Guid id)
        {
            lock (_mutex)
            {
                Instrument result = null;
                if (!_instruments.TryGetValue(id, out result))
                {
                    Logger.InfoFormat("get, instrument = {0} not exists", id);
                }
                return result;
            }
        }

    }

    internal sealed class Instrument
    {
        internal Guid Id { get; private set; }
        internal string Code { get; private set; }
        internal int NumeratorUnit { get; private set; }
        internal int Denominator { get; private set; }
        internal Guid CurrencyId { get; private set; }
        internal InstrumentCategory Category { get; private set; }

        internal Instrument(DataRow row)
        {
            this.Id = (Guid)(row["ID"]);
            this.NumeratorUnit = (int)(row["NumeratorUnit"]);
            this.Denominator = (int)(row["Denominator"]);
            this.CurrencyId = (Guid)row["CurrencyID"];
            this.Category = (InstrumentCategory)((int)row["Category"]);
        }

        internal Instrument(Guid id, XmlNode instrumentNode)
        {
            this.Id = id;
            this.Update(instrumentNode);
        }


        internal void Update(XmlNode instrumentNode)
        {
            foreach (XmlAttribute attribute in instrumentNode.Attributes)
            {
                switch (attribute.Name)
                {
                    case "NumeratorUnit":
                        this.NumeratorUnit = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "Denominator":
                        this.Denominator = XmlConvert.ToInt32(attribute.Value);
                        break;
                    case "CurrencyID":
                        this.CurrencyId = XmlConvert.ToGuid(attribute.Value);
                        break;
                    case "Category":
                        this.Category = (InstrumentCategory)XmlConvert.ToInt32(attribute.Value);
                        break;
                }
            }
        }
    }
}