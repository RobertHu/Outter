using iExchange.Common;
using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Core.TransactionServer.Agent.Util.TypeExtension;
using System.Data;
using Core.TransactionServer.Agent.Framework;
using Protocal.TypeExtensions;
using Protocal.CommonSetting;
using Core.TransactionServer.Agent.BLL.AccountBusiness;

namespace Core.TransactionServer.Agent.Settings
{
    internal enum InstrumentUpdateType
    {
        None,
        Add,
        Update,
        Delete,
    }

    internal delegate void InstrumentUpdateHandle(Instrument instrument, InstrumentUpdateType updateType);

    internal class SettingInfo
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(SettingInfo));

        private Dictionary<DateTime, Setting> _historySettingsDict = new Dictionary<DateTime, Setting>();
        private const int CURRENCY_CAPACITY = 33;
        private const int INSTRUMENT_CAPACITY = 37;
        private const int DEFAULT_CAPACITY = 500;
        private double capacityFactory = 1;
        private SystemParameter _systemParameter;
        private Dictionary<Guid, Currency> _currencies = new Dictionary<Guid, Currency>(DEFAULT_CAPACITY);
        private Dictionary<CurrencyIdPair, CurrencyRate> _currencyRates = new Dictionary<CurrencyIdPair, CurrencyRate>(DEFAULT_CAPACITY);
        private Dictionary<QuotePolicyInstrumentIdPair, QuotePolicyDetail> _quotePolicyDetails = new Dictionary<QuotePolicyInstrumentIdPair, QuotePolicyDetail>(DEFAULT_CAPACITY);
        private Dictionary<Guid, TradePolicy> _tradePolicies = new Dictionary<Guid, TradePolicy>(DEFAULT_CAPACITY);
        private Dictionary<TradePolicyDetailKey, TradePolicyDetail> _tradePolicyDetails = new Dictionary<TradePolicyDetailKey, TradePolicyDetail>(DEFAULT_CAPACITY);
        private Dictionary<Guid, DealingPolicy> _dealingPolicies = new Dictionary<Guid, DealingPolicy>(DEFAULT_CAPACITY);
        private Dictionary<Guid, VolumeNecessary> _volumeNecessaries = new Dictionary<Guid, VolumeNecessary>(DEFAULT_CAPACITY);
        private Dictionary<Guid, InstalmentPolicy> _instalmentPolicies = new Dictionary<Guid, InstalmentPolicy>(DEFAULT_CAPACITY);
        private Dictionary<Guid, SpecialTradePolicy> _specialTradePolicies = new Dictionary<Guid, SpecialTradePolicy>(DEFAULT_CAPACITY);
        private Dictionary<Guid, Customer> _customers = new Dictionary<Guid, Customer>(10000);
        private Dictionary<Guid, Account> _accounts = new Dictionary<Guid, Account>(10000);
        private Dictionary<Guid, Instrument> _instruments = new Dictionary<Guid, Instrument>(DEFAULT_CAPACITY);
        private Dictionary<Guid, InterestPolicy> _interestPolicies = new Dictionary<Guid, InterestPolicy>(DEFAULT_CAPACITY);
        private Dictionary<Guid, PhysicalPaymentDiscountPolicy> _physicalPaymentDiscountPolicyDict = new Dictionary<Guid, PhysicalPaymentDiscountPolicy>(DEFAULT_CAPACITY);
        private Dictionary<Guid, Blotter> _blotters = new Dictionary<Guid, Blotter>(50);
        private TradeDay _tradeDay;
        public static readonly Token Token = new Token(Guid.Empty, UserType.System, AppType.TransactionServer);
        internal event InstrumentUpdateHandle InstruemntUpdated;

        internal void Clear()
        {
            _currencies.Clear();
            _currencyRates.Clear();
            _quotePolicyDetails.Clear();
            _tradePolicies.Clear();
            _tradePolicyDetails.Clear();
            _dealingPolicies.Clear();
            _volumeNecessaries.Clear();
            _instalmentPolicies.Clear();
            _specialTradePolicies.Clear();
            _customers.Clear();
            _accounts.Clear();
            _instruments.Clear();
            _interestPolicies.Clear();
            _physicalPaymentDiscountPolicyDict.Clear();
            _blotters.Clear();
        }

        internal int AccountCapacity
        {
            get { return (int)(this._accounts.Count * this.capacityFactory); }
        }

        internal int InstrumentCapacity
        {
            get { return (int)(this._instruments.Count * this.capacityFactory); }
        }

        internal TradeDay TradeDay
        {
            get { return _tradeDay; }
        }

        internal SystemParameter SystemParameter
        {
            get { return this._systemParameter; }
        }

        internal Dictionary<Guid, Instrument> Instruments
        {
            get { return _instruments; }
        }

        internal Dictionary<Guid, Currency> Currencies
        {
            get { return _currencies; }
        }

        internal Dictionary<CurrencyIdPair, CurrencyRate> CurrencyRates
        {
            get { return _currencyRates; }
        }

        internal Dictionary<QuotePolicyInstrumentIdPair, QuotePolicyDetail> QuotePolicyDetails
        {
            get { return _quotePolicyDetails; }
        }

        internal Dictionary<Guid, TradePolicy> TradePolicies
        {
            get { return _tradePolicies; }
        }

        internal Dictionary<TradePolicyDetailKey, TradePolicyDetail> TradePolicyDetails
        {
            get { return _tradePolicyDetails; }
        }

        internal Dictionary<Guid, DealingPolicy> DealingPolicies
        {
            get { return _dealingPolicies; }
        }

        internal Dictionary<Guid, VolumeNecessary> VolumeNecessaries
        {
            get { return _volumeNecessaries; }
        }

        internal Dictionary<Guid, InstalmentPolicy> InstalmentPolicies
        {
            get { return _instalmentPolicies; }
        }

        internal Dictionary<Guid, SpecialTradePolicy> SpecialTradePolicies
        {
            get { return _specialTradePolicies; }
        }

        internal Dictionary<Guid, Customer> Customers
        {
            get { return _customers; }
        }

        internal Dictionary<Guid, Account> Accounts
        {
            get { return _accounts; }
        }

        internal Dictionary<Guid, InterestPolicy> InterestPolicies
        {
            get { return _interestPolicies; }
        }

        internal Dictionary<Guid, PhysicalPaymentDiscountPolicy> PhysicalPaymentDiscountPolicyDict
        {
            get { return _physicalPaymentDiscountPolicyDict; }
        }


        public DealingPolicy GetDealingPolicy(Guid dealingPolicyId)
        {
            return _dealingPolicies[dealingPolicyId];
        }

        internal Blotter GetBlotter(Guid blotterId)
        {
            Blotter blotter = null;
            _blotters.TryGetValue(blotterId, out blotter);
            return blotter;
        }


        public TradingTime GetTradingTime(Guid instrumentId)
        {
            var instrument = _instruments[instrumentId];
            return new TradingTime(instrument.DayOpenTime, instrument.DayCloseTime, TimeSpan.FromMinutes(instrument.LastAcceptTimeSpan));
        }

        public QuotationParameter GetQuotationParameter(Guid instrumentId)
        {
            if (!_instruments.ContainsKey(instrumentId)) return QuotationParameter.Invalid;
            Instrument instrument = _instruments[instrumentId];
            return new QuotationParameter(instrument.IsNormal, instrument.NumeratorUnit, instrument.Denominator);
        }

        internal QuotePolicyDetail GetQuotePolicyDetail(IQuotePolicyProvider quotePolicyProvider, Guid instrumentId)
        {
            QuotePolicyDetail result = quotePolicyProvider.Get<QuotePolicyDetail>(delegate(Guid id, out QuotePolicyDetail qpd)
            {
                return this.QuotePolicyDetails.TryGetValue(new QuotePolicyInstrumentIdPair(id, instrumentId), out qpd);
            });
            return result;
        }

        #region Update settings

        internal void FillCurrencyRate(List<CurrencyRate> target)
        {
            target.AddRange(_currencyRates.Values);
        }


        public void Update(XElement node)
        {
            foreach (XElement eachMethod in node.Elements())
            {
                foreach (XElement eachRow in eachMethod.Elements())
                {
                    string methodName = eachMethod.Name.ToString();
                    string entityName = eachRow.Name.ToString();
                    switch (entityName)
                    {
                        case "SystemParameter":
                            if (methodName == "Modify")
                            {
                                _systemParameter.Update(eachRow.ToXmlNode());
                            }
                            break;
                        case "Currency":
                            this.UpdateCurrencies(eachRow, methodName);
                            break;

                        case "CurrencyRate": //recalculate
                            this.UpdateCurrencyRate(eachRow, methodName);
                            break;
                        case "Customer":
                        case "Employee":
                            this.UpdateCustomer(eachRow, methodName);
                            break;
                        case "Customers":
                        case "Employees":
                            this.UpdateCustomers(eachRow, methodName);
                            break;
                        case "Blotter":
                            this.UpdateBlotter(eachRow, methodName);
                            break;
                        case "Account":
                            if (eachMethod.Name == "Modify")
                            {
                                Guid id = eachRow.AttrToGuid("ID");
                                if (_accounts.ContainsKey(id))
                                {
                                    this.UpdateAccount(eachRow);
                                }
                                else
                                {
                                    this.AddAccount(eachRow);
                                }
                            }
                            else if (eachMethod.Name == "Add")
                            {
                                this.AddAccount(eachRow);
                            }
                            else if (eachMethod.Name == "Delete")
                            {
                                Guid id = eachRow.AttrToGuid("ID");
                                if (_accounts.ContainsKey(id))
                                {
                                    _accounts.Remove(id);
                                    TradingSetting.Default.RemoveAccount(id);
                                    RiskChecker.Default.Remove(id);
                                }
                            }
                            break;
                        case "Physical.SettlementPrice":
                            Guid instrumentId = eachRow.AttrToGuid("InstrumentID");
                            Instrument instrument;
                            if (_instruments.TryGetValue(instrumentId, out instrument))
                            {
                                string depositPrice = eachRow.HasAttribute("DepositPrice") ? eachRow.Attribute("DepositPrice").Value : null;
                                string deliveryPrice = eachRow.HasAttribute("DeliveryPrice") ? eachRow.Attribute("DeliveryPrice").Value : null;
                                instrument.UpdateSettlementPrice(depositPrice, deliveryPrice);
                            }
                            break;
                        case "Instruments":
                            this.UpdateInstruments(eachRow, methodName);
                            break;
                        case "Instrument":
                            this.UpdateInstrument(eachRow, methodName);
                            break;

                        case "TradePolicy":
                            this.UpdateTradePolicy(eachRow, methodName);
                            break;
                        case "TradePolicyDetail":
                            this.UpdateTradePolicyDetail(eachRow, methodName);
                            break;
                        case "TradePolicyDetails":
                            foreach (var row2 in eachRow.Elements())
                            {
                                this.UpdateTradePolicyDetail(row2, methodName);
                            }
                            break;
                        case "VolumeNecessary":
                            this.UpdateVolumeNecessary(eachRow, methodName);
                            break;
                        case "VolumeNecessaryDetail":
                            this.UpdateVolumeNecessaryDetail(eachRow, methodName);
                            break;
                        case "PhysicalPaymentDiscount":
                            this.UpdatePhysicalPaymentDiscount(eachRow, methodName);
                            break;
                        case "PhysicalPaymentDiscountDetail":
                            this.UpdatePhysicalPaymentDiscountDetail(eachRow, methodName);
                            break;
                        case "SpecialTradePolicy":
                            this.UpdateSpecialTradePolicy(eachRow, methodName);
                            break;
                        case "SpecialTradePolicyDetail":
                            this.UpdateSpecialTradePolicyDetail(eachRow, methodName);
                            break;
                        case "DealingPolicy":
                            this.UpdateDealingPolicy(eachRow, methodName);
                            break;
                        case "DealingPolicyDetails":
                            foreach (var eachChildRow in eachRow.Elements())
                            {
                                this.UpdateDealingPolicyDetail(eachChildRow, methodName);
                            }
                            break;
                        case "DealingPolicyDetail":
                            this.UpdateDealingPolicyDetail(eachRow, methodName);
                            break;
                        case "QuotePolicyDetail":
                            this.UpdateQuotePolicyDetail(eachRow, methodName);
                            break;
                        case "QuotePolicyDetails":
                            foreach (var eachChildRow in eachRow.Elements())
                            {
                                this.UpdateQuotePolicyDetail(eachChildRow, methodName);
                            }
                            break;
                        case "InstalmentPolicy":
                            this.UpdateInstalmentPolicy(eachRow, methodName);
                            break;
                        case "InstalmentPolicyDetail":
                            this.UpdateInstalmentPolicyDetail(eachRow, methodName);
                            break;
                        case "BOBetType":
                        case "BOBetTypes":
                            BinaryOption.BOBetTypeRepository.Update(eachRow, methodName);
                            break;

                        case "BOPolicy":
                        case "BOPolicies":
                            BinaryOption.BOPolicyRepository.Update(eachRow, methodName);
                            BinaryOption.BOPolicyDetailRepository.Default.Update(eachRow, methodName);
                            break;
                        case "BOPolicyDetail":
                        case "BOPolicyDetails":
                            BinaryOption.BOPolicyDetailRepository.Default.Update(eachRow, methodName);
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        private void AddAccount(XElement row)
        {
            var account = new Account(row);
            if (!_accounts.ContainsKey(account.Id))
            {
                _accounts.Add(account.Id, account);
                TradingSetting.Default.AddAccount(new Agent.Account(account.Id));
                Logger.InfoFormat("add account id = {0}", account.Id);
            }
            else
            {
                Logger.ErrorFormat("duplicate key id = {0}", account.Id);
            }
        }

        private void UpdateInstalmentPolicyDetail(XElement row, string method)
        {
            Guid instalmentPolicyId = row.AttrToGuid("InstalmentPolicyId");
            int period = row.AttrToInt32("Period");
            InstalmentFrequence frequence = (InstalmentFrequence)(row.AttrToInt32("Frequence"));
            if (method == "Add")
            {
                if (_instalmentPolicies.ContainsKey(instalmentPolicyId))
                {
                    InstalmentPolicyDetail instalmentPolicyDetail = new InstalmentPolicyDetail(row);
                    _instalmentPolicies[instalmentPolicyDetail.InstalmentPolicyId].Add(instalmentPolicyDetail);
                }
            }
            else if (method == "Modify")
            {
                if (_instalmentPolicies.ContainsKey(instalmentPolicyId))
                {
                    var detail = _instalmentPolicies[instalmentPolicyId].Get(new InstalmentPeriod(period, frequence));
                    detail.Update(row);
                }
            }
            else if (method == "Delete")
            {
                if (_instalmentPolicies.ContainsKey(instalmentPolicyId))
                {
                    _instalmentPolicies[instalmentPolicyId].Remove(period, frequence);
                }
            }
        }


        private void UpdateInstalmentPolicy(XElement row, string method)
        {
            Guid instalmentPolicyId = row.AttrToGuid("ID");
            if (method == "Add")
            {
                if (!_instalmentPolicies.ContainsKey(instalmentPolicyId))
                {
                    InstalmentPolicy instalmentPolicy = new InstalmentPolicy(row);
                    _instalmentPolicies.Add(instalmentPolicy.Id, instalmentPolicy);
                }
            }
            else if (method == "Modify")
            {
                if (_instalmentPolicies.ContainsKey(instalmentPolicyId))
                {
                    _instalmentPolicies[instalmentPolicyId].Update(row);
                }
            }
            else if (method == "Delete")
            {
                if (_instalmentPolicies.ContainsKey(instalmentPolicyId))
                {
                    _instalmentPolicies.Remove(instalmentPolicyId);
                }
            }
        }


        private void UpdateQuotePolicyDetail(XElement row, string methodName)
        {
            Guid quotePolicyID = row.AttrToGuid("QuotePolicyID");
            Guid instrumentID = row.AttrToGuid("InstrumentID");
            if (methodName == "Modify")
            {
                Guid oldQuotePolicyID, oldInstrumentID;
                if (row.Element("OldPrimaryKey") == null)
                {
                    oldQuotePolicyID = quotePolicyID;
                    oldInstrumentID = instrumentID;
                }
                else
                {
                    oldQuotePolicyID = row.Element("OldPrimaryKey").AttrToGuid("QuotePolicyID");
                    oldInstrumentID = row.Element("OldPrimaryKey").AttrToGuid("InstrumentID");
                }
                var key =new QuotePolicyInstrumentIdPair(oldQuotePolicyID, oldInstrumentID);

                if (_quotePolicyDetails.ContainsKey(key))
                {
                    _quotePolicyDetails[key].Update(row);
                }
                else
                {
                    Logger.InfoFormat("UpdateQuotePolicyDetail can't find QuotePolicyID = {0},InstrumentID = {1}", key.QuotePolicyId, key.InstrumentId);
                }

            }
            else if (methodName == "Add")
            {
                if (!_quotePolicyDetails.ContainsKey(new QuotePolicyInstrumentIdPair(quotePolicyID, instrumentID)))
                {
                    QuotePolicyDetail quotePolicyDetail = new QuotePolicyDetail(row);
                    _quotePolicyDetails.Add(new QuotePolicyInstrumentIdPair(quotePolicyID, instrumentID), quotePolicyDetail);
                }
            }
            else if (methodName == "Delete")
            {
                if (_quotePolicyDetails.ContainsKey(new QuotePolicyInstrumentIdPair(quotePolicyID, instrumentID)))
                {
                    _quotePolicyDetails.Remove(new QuotePolicyInstrumentIdPair(quotePolicyID, instrumentID));
                }
            }
        }




        private void UpdateDealingPolicyDetail(XElement row, string method)
        {
            Guid dealingPolicyID = row.AttrToGuid("DealingPolicyID");
            Guid instrumentID = row.AttrToGuid("InstrumentID");
            if (method == "Modify")
            {
                Guid oldDealingPolicyID, oldInstrumentID;
                if (row.Element("OldPrimaryKey") == null)
                {
                    oldDealingPolicyID = dealingPolicyID;
                    oldInstrumentID = instrumentID;
                }
                else
                {
                    oldDealingPolicyID = row.Element("OldPrimaryKey").AttrToGuid("DealingPolicyID");
                    oldInstrumentID = row.Element("OldPrimaryKey").AttrToGuid("InstrumentID");
                }

                var detail = _dealingPolicies[oldDealingPolicyID][oldInstrumentID];

                _dealingPolicies[oldDealingPolicyID].Remove(detail);

                detail.Update(row, _dealingPolicies[dealingPolicyID]);

            }
            else if (method == "Add")
            {
                DealingPolicy dealingPolicy = _dealingPolicies[dealingPolicyID];
                if (!_dealingPolicies.ContainsKey(instrumentID))
                {
                    new DealingPolicyDetail(row, dealingPolicy);
                }
            }
            else if (method == "Delete")
            {
                if (_dealingPolicies.ContainsKey(dealingPolicyID))
                {
                    _dealingPolicies[dealingPolicyID].Remove(_dealingPolicies[dealingPolicyID][instrumentID]);
                }
            }
        }



        private void UpdateDealingPolicy(XElement row, string method)
        {
            Guid dealingPolicyID = row.AttrToGuid("ID");
            if (method == "Modify")
            {
                if (_dealingPolicies.ContainsKey(dealingPolicyID))
                {
                    DealingPolicy dealingPolicy = _dealingPolicies[dealingPolicyID];
                    dealingPolicy.Update(row);
                }
            }
            else if (method == "Add")
            {
                if (!_dealingPolicies.ContainsKey(dealingPolicyID))
                {
                    DealingPolicy dealingPolicy = new DealingPolicy(row);
                    _dealingPolicies.Add(dealingPolicy.ID, dealingPolicy);
                }
            }
            else if (method == "Delete")
            {
                if (_dealingPolicies.ContainsKey(dealingPolicyID))
                {
                    _dealingPolicies.Remove(dealingPolicyID);
                }
            }

        }


        private void UpdateSpecialTradePolicyDetail(XElement row, string method)
        {
            Guid specialTradePolicyID = row.AttrToGuid("SpecialTradePolicyID");
            Guid instrumentID = row.AttrToGuid("InstrumentID");
            if (method == "Modify")
            {
                Guid oldSpecialTradePolicyID, oldInstrumentID;
                if (row.Element("OldPrimaryKey") == null)
                {
                    oldSpecialTradePolicyID = specialTradePolicyID;
                    oldInstrumentID = instrumentID;
                }
                else
                {
                    oldSpecialTradePolicyID = row.Element("OldPrimaryKey").AttrToGuid("SpecialTradePolicyID");
                    oldInstrumentID = row.Element("OldPrimaryKey").AttrToGuid("InstrumentID");
                }
                SpecialTradePolicy oldTradePolicy = _specialTradePolicies[oldSpecialTradePolicyID];
                SpecialTradePolicyDetail detail = oldTradePolicy[oldInstrumentID];

                if (oldSpecialTradePolicyID != specialTradePolicyID || oldInstrumentID != instrumentID)
                {
                    oldTradePolicy.Remove(detail);
                }
                detail.Update(row, _specialTradePolicies[specialTradePolicyID]);
            }
            else if (method == "Add")
            {
                SpecialTradePolicy specialTradePolicy = _specialTradePolicies[specialTradePolicyID];
                new SpecialTradePolicyDetail(row, specialTradePolicy);
            }
            else if (method == "Delete")
            {
                var detail = _specialTradePolicies[specialTradePolicyID][instrumentID];
                _specialTradePolicies[specialTradePolicyID].Remove(detail);
            }
        }


        private void UpdateSpecialTradePolicy(XElement row, string method)
        {
            Guid specialTradePolicyID = row.AttrToGuid("ID");
            if (method == "Modify")
            {
                if (_specialTradePolicies.ContainsKey(specialTradePolicyID))
                {
                    SpecialTradePolicy specialTradePolicy = _specialTradePolicies[specialTradePolicyID];
                    specialTradePolicy.Update(row);
                }
            }
            else if (method == "Add")
            {
                if (!_specialTradePolicies.ContainsKey(specialTradePolicyID))
                {
                    SpecialTradePolicy specialTradePolicy = new SpecialTradePolicy(row);
                    _specialTradePolicies.Add(specialTradePolicy.Id, specialTradePolicy);
                }
            }
            else if (method == "Delete")
            {
                if (_specialTradePolicies.ContainsKey(specialTradePolicyID))
                {
                    SpecialTradePolicy specialTradePolicy = _specialTradePolicies[specialTradePolicyID];
                    _specialTradePolicies.Remove(specialTradePolicyID);
                }
            }
        }


        private void UpdatePhysicalPaymentDiscountDetail(XElement row, string method)
        {
            Guid physicalPaymentDiscountDetailId = row.AttrToGuid("ID");
            Guid paymentDiscountId = row.AttrToGuid("PhysicalPaymentDiscountID");
            if (method == "Modify")
            {
                decimal oldFrom = _physicalPaymentDiscountPolicyDict[paymentDiscountId][physicalPaymentDiscountDetailId].From;
                _physicalPaymentDiscountPolicyDict[paymentDiscountId][physicalPaymentDiscountDetailId].Update(row);
                if (oldFrom != _physicalPaymentDiscountPolicyDict[paymentDiscountId][physicalPaymentDiscountDetailId].From)
                {
                    _physicalPaymentDiscountPolicyDict[paymentDiscountId].ResortDetails();
                }
            }
            else if (method == "Add")
            {
                PhysicalPaymentDiscountPolicyDetail physicalPaymentDiscountPolicyDetail
                    = new PhysicalPaymentDiscountPolicyDetail(row);
                _physicalPaymentDiscountPolicyDict[paymentDiscountId].Add(physicalPaymentDiscountPolicyDetail);
            }
            else if (method == "Delete")
            {
                _physicalPaymentDiscountPolicyDict[paymentDiscountId].Remove(physicalPaymentDiscountDetailId);
            }
        }

        private void UpdatePhysicalPaymentDiscount(XElement row, string method)
        {
            Guid physicalPaymentDiscountId = row.AttrToGuid("ID");
            if (method == "Modify")
            {
                if (_physicalPaymentDiscountPolicyDict.ContainsKey(physicalPaymentDiscountId))
                {
                    _physicalPaymentDiscountPolicyDict[physicalPaymentDiscountId].Update(row);
                }
            }
            else if (method == "Add")
            {
                if (!_physicalPaymentDiscountPolicyDict.ContainsKey(physicalPaymentDiscountId))
                {
                    PhysicalPaymentDiscountPolicy physicalPaymentDiscountPolicy = new PhysicalPaymentDiscountPolicy(row);
                    _physicalPaymentDiscountPolicyDict.Add(physicalPaymentDiscountPolicy.ID, physicalPaymentDiscountPolicy);
                }
            }
            else if (method == "Delete")
            {
                if (_physicalPaymentDiscountPolicyDict.ContainsKey(physicalPaymentDiscountId))
                {
                    _physicalPaymentDiscountPolicyDict.Remove(physicalPaymentDiscountId);
                }
            }

        }


        private void UpdateVolumeNecessaryDetail(XElement row, string method)
        {
            Guid volumeNecessaryDetailId = row.AttrToGuid("ID");
            Guid ownerId = row.AttrToGuid("VolumeNecessaryId");
            if (method == "Modify")
            {
                decimal oldFrom = _volumeNecessaries[ownerId][volumeNecessaryDetailId].From;
                _volumeNecessaries[ownerId][volumeNecessaryDetailId].Update(row);
                if (oldFrom != _volumeNecessaries[ownerId][volumeNecessaryDetailId].From)
                {
                    _volumeNecessaries[ownerId].ResortDetails();
                }
            }
            else if (method == "Add")
            {
                VolumeNecessaryDetail volumeNecessaryDetail = new VolumeNecessaryDetail(row);
                _volumeNecessaries[ownerId].Add(volumeNecessaryDetail);
            }
            else if (method == "Delete")
            {
                _volumeNecessaries[ownerId].Remove(volumeNecessaryDetailId);
            }
        }


        private void UpdateVolumeNecessary(XElement row, string method)
        {
            Guid volumeNecessaryId = row.AttrToGuid("ID");
            if (method == "Modify")
            {
                if (_volumeNecessaries.ContainsKey(volumeNecessaryId))
                {
                    _volumeNecessaries[volumeNecessaryId].Update(row);
                }
            }
            else if (method == "Add")
            {
                if (!_volumeNecessaries.ContainsKey(volumeNecessaryId))
                {
                    VolumeNecessary volumeNecessary = new VolumeNecessary(row);
                    _volumeNecessaries.Add(volumeNecessary.Id, volumeNecessary);
                }
            }
            else if (method == "Delete")
            {
                if (_volumeNecessaries.ContainsKey(volumeNecessaryId))
                {
                    _volumeNecessaries.Remove(volumeNecessaryId);
                }
            }
        }


        private void UpdateBlotter(XElement row, string method)
        {
            Guid id = row.AttrToGuid("ID");
            if (method == "Modify")
            {
                if (_blotters.ContainsKey(id))
                {
                    Blotter boltter = _blotters[id];
                    boltter.Update(row);
                }
            }
            else if (method == "Add")
            {
                if (!_blotters.ContainsKey(id))
                {
                    Blotter boltter = new Blotter(row);
                    _blotters.Add(boltter.ID, boltter);
                }
            }
            else if (method == "Delete")
            {
                if (_blotters.ContainsKey(id))
                {
                    _blotters.Remove(id);
                }
            }
        }


        private bool UpdateTradePolicyDetail(XElement row, string method)
        {
            Guid tradePolicyID = row.AttrToGuid("TradePolicyID");
            Guid instrumentID = row.AttrToGuid("InstrumentID");
            if (method == "Modify")
            {
                Guid oldTradePolicyID, oldInstrumentID;
                if (row.Element("OldPrimaryKey") == null)
                {
                    oldTradePolicyID = tradePolicyID;
                    oldInstrumentID = instrumentID;
                }
                else
                {
                    oldTradePolicyID = row.Element("OldPrimaryKey").AttrToGuid("TradePolicyID");
                    oldInstrumentID = row.Element("OldPrimaryKey").AttrToGuid("InstrumentID");
                }
                var oldTradePolicy = _tradePolicies[oldTradePolicyID];
                var tradePolicyDetail = oldTradePolicy[oldInstrumentID, null];
                var newTradePolicy = _tradePolicies[tradePolicyID];
                if (tradePolicyDetail != null)
                {
                    _tradePolicyDetails.Remove(new TradePolicyDetailKey(oldInstrumentID, oldTradePolicyID));
                    decimal oldCommissionCloseD = tradePolicyDetail.CommissionCloseD;
                    decimal oldLevy = tradePolicyDetail.LevyClose;
                    tradePolicyDetail.Update(row, newTradePolicy);
                    this.SetVolumnNecessaryForTradePolicyDetail(tradePolicyDetail);
                    _tradePolicyDetails.Add(new TradePolicyDetailKey(instrumentID, tradePolicyID), tradePolicyDetail);
                    this.ProcessWhenTradePolicyDetailUpdate(oldCommissionCloseD, oldLevy, tradePolicyDetail);
                }
            }
            else if (method == "Add")
            {
                if (!_tradePolicyDetails.ContainsKey(new TradePolicyDetailKey(instrumentID, tradePolicyID)))
                {
                    var tradePolicyDetail = new TradePolicyDetail(row, _tradePolicies[tradePolicyID]);
                    _tradePolicyDetails.Add(new TradePolicyDetailKey(instrumentID, tradePolicyID), tradePolicyDetail);
                    this.SetVolumnNecessaryForTradePolicyDetail(tradePolicyDetail);
                }
            }
            else if (method == "Delete")
            {
                if (_tradePolicyDetails.ContainsKey(new TradePolicyDetailKey(instrumentID, tradePolicyID)))
                {
                    _tradePolicyDetails.Remove(new TradePolicyDetailKey(instrumentID, tradePolicyID));
                }
            }
            return true;
        }

        private void ProcessWhenTradePolicyDetailUpdate(decimal oldCommissionCloseD, decimal oldLevyClose, TradePolicyDetail detail)
        {
            Logger.InfoFormat("ProcessWhenTradePolicyDetailUpdate oldCommissionCloseD = {0}, oldLevyClose  = {1}, newCommission = {2}, newLevy = {3}, instrumentId = {4}, tradePolicyId = {5}",
                oldCommissionCloseD, oldLevyClose, detail.CommissionCloseD, detail.LevyClose, detail.InstrumentId, detail.TradePolicy.ID);
            if (oldCommissionCloseD != detail.CommissionCloseD || oldLevyClose != detail.LevyClose)
            {
                TradingSetting.Default.DoParallelForAccounts(m => m.RecalculateEstimateFee(detail));
            }
        }



        private void SetVolumnNecessaryForTradePolicyDetail(TradePolicyDetail tradePolicyDetail)
        {
            tradePolicyDetail.VolumeNecessary = tradePolicyDetail.VolumeNecessaryId == null ? null : _volumeNecessaries[tradePolicyDetail.VolumeNecessaryId.Value];
        }


        private void UpdateTradePolicy(XElement row, string method)
        {
            Guid tradePolicyID = row.AttrToGuid("ID");
            if (method == "Modify")
            {
                if (_tradePolicies.ContainsKey(tradePolicyID))
                {
                    TradePolicy tradePolicy = _tradePolicies[tradePolicyID];
                    tradePolicy.Update(row);
                }
            }
            else if (method == "Add")
            {
                if (!_tradePolicies.ContainsKey(tradePolicyID))
                {
                    TradePolicy tradePolicy = new TradePolicy(row);
                    _tradePolicies.Add(tradePolicy.ID, tradePolicy);
                }
            }
            else if (method == "Delete")
            {
                if (_tradePolicies.ContainsKey(tradePolicyID))
                {
                    TradePolicy tradePolicy = _tradePolicies[tradePolicyID];
                    _tradePolicies.Remove(tradePolicyID);
                }
            }

        }

        private void UpdateCustomers(XElement row, string method)
        {
            CustomerType type = row.Name == "Customers" ? CustomerType.Customer : CustomerType.Employee;
            if (method == "Modify")
            {
                foreach (var eachChild in row.Elements())
                {
                    Guid customerID = eachChild.AttrToGuid("ID");
                    if (_customers.ContainsKey(customerID))
                    {
                        Customer customer = _customers[customerID];
                        customer.Update(eachChild, type);
                    }
                }
            }
        }

        private void UpdateCustomer(XElement row, string method)
        {
            CustomerType type = row.Name == "Customer" ? CustomerType.Customer : CustomerType.Employee;
            Guid customerID = row.AttrToGuid("ID");
            if (method == "Modify")
            {
                if (_customers.ContainsKey(customerID))
                {
                    Customer customer = _customers[customerID];
                    customer.Update(row, type);
                }
            }
            else if (method == "Add")
            {
                if (!_customers.ContainsKey(customerID))
                {
                    Logger.InfoFormat("add customer id = {0}", customerID);
                    Customer customer = new Customer(row, type);
                    _customers.Add(customer.Id, customer);
                }
                else
                {
                    Logger.Error("duplicate customer id = " + customerID);
                }
            }
        }

        private void UpdateInstruments(XElement node, string method)
        {
            foreach (var eachInstrumentNode in node.Elements())
            {
                this.UpdateInstrument(eachInstrumentNode, method);
            }
        }

        private void UpdateInstrument(XElement node, string method)
        {
            Guid id = node.AttrToGuid("ID");
            if (method == "Modify")
            {
                if (_instruments.ContainsKey(id))
                {
                    Instrument instrument = _instruments[id];
                    instrument.Update(node);
                    this.OnInstrumentUpdated(instrument, InstrumentUpdateType.Update);
                }
            }
            else if (method == "Add")
            {
                if (!_instruments.ContainsKey(id))
                {
                    Instrument instrument = new Instrument(node);
                    _instruments.Add(instrument.Id, instrument);
                    this.OnInstrumentUpdated(instrument, InstrumentUpdateType.Add);
                    Logger.InfoFormat("Add New Instrument id = {0}", instrument);
                }
                else
                {
                    Debug.WriteLine(string.Format("Duplicate instrument key id = {0}", id));
                }
            }
            else if (method == "Delete")
            {
                if (_instruments.ContainsKey(id))
                {
                    this.OnInstrumentUpdated(_instruments[id], InstrumentUpdateType.Delete);
                    _instruments.Remove(id);
                }
            }
        }

        private void UpdateCurrencyRate(XElement node, string method)
        {
            Guid sourceCurrencyID = node.AttrToGuid("SourceCurrencyID");
            Guid targetCurrencyID = node.AttrToGuid("TargetCurrencyID");
            var currencyRateID = new CurrencyIdPair(sourceCurrencyID, targetCurrencyID);
            if (method == "Add" || method == "Modify")
            {
                if (!_currencyRates.ContainsKey(currencyRateID))
                {
                    Currency sourceCurrency = _currencies[sourceCurrencyID];
                    Currency targetCurrency = _currencies[targetCurrencyID];
                    _currencyRates.Add(currencyRateID, new CurrencyRate(node, sourceCurrency, targetCurrency));
                }
                else
                {
                    CurrencyRate currencyRate = _currencyRates[currencyRateID];
                    currencyRate.Update(node);
                }
            }
        }

        private void UpdateCurrencies(XElement node, string method)
        {
            if (method == "Modify")
            {
                Guid currencyID = node.AttrToGuid("ID");
                Currency currency;
                if (!_currencies.TryGetValue(currencyID, out currency))
                {
                    throw new UpdateSettingException("Currency", method, "can't found currencyid in currencies");
                }
                int? decimals = null;
                if (node.HasAttribute("Decimals"))
                {
                    decimals = node.AttrToInt32("Decimals");
                }
                string code = node.HasAttribute("Code") ? node.Attribute("Code").Value : null;
                if (decimals != null) currency.Decimals = decimals.Value;
                if (code != null) currency.Code = code;
            }
            else if (method == "Add")
            {
                Guid currencyID = node.AttrToGuid("ID");
                string code = node.Attribute("Code").Value;
                int decimals = node.AttrToInt32("Decimals");
                if (!_currencies.ContainsKey(currencyID))
                {
                    Currency currency = new Currency(currencyID, code, decimals);
                    _currencies.Add(currencyID, currency);
                    _currencyRates.Add(new CurrencyIdPair(currencyID, currencyID), new CurrencyRate(currency, currency, 1, 1, null, false));
                }
            }

        }

        private void UpdateAccount(XElement row)
        {
            Guid id = row.AttrToGuid("ID");
            Account account;
            if (!_accounts.TryGetValue(id, out account)) return;
            decimal oldCommissionFactor = account.RateCommission;
            decimal oldLevyFactor = account.RateLevy;
            account.Update(row);
            var tradingAccount = TradingSetting.Default.GetAccount(id);
            if (account.RateCommission != oldCommissionFactor || account.RateLevy != oldLevyFactor)
            {
                tradingAccount.RecalculateEstimateFee();
            }
            tradingAccount.CheckRisk();
        }

        #endregion

        private void OnInstrumentUpdated(Instrument instrument, InstrumentUpdateType updateType)
        {
            var handle = this.InstruemntUpdated;
            if (handle != null)
            {
                handle(instrument, updateType);
            }
        }

        #region   parse db data by sqlDataReader

        internal void InitializeTradeDay(IDataReader dr)
        {
            _tradeDay = SettingInitializer.InitializeTradeDay(dr);
        }

        internal void InitializeSystemParameter(IDataReader dr)
        {
            _systemParameter = SettingInitializer.InitializeSystemParameter(dr);
        }

        internal void InitializeCurrency(IDataReader dr)
        {
            var item = new Currency(new DBReader(dr));
            _currencies.Add(item.Id, item);
        }

        internal void InitializeCurrencyRate(IDataReader dr)
        {
            var item = SettingInitializer.InitializeCurrencyRate(dr, _currencies);
            _currencyRates.Add(new CurrencyIdPair(item.SourceCurrency.Id, item.TargetCurrency.Id), item);
        }


        internal void InitializeInstrument(IDataReader dr)
        {
            var item = new Instrument(new DBReader(dr));
            _instruments.Add(item.Id, item);
            this.OnInstrumentUpdated(item, InstrumentUpdateType.Add);
        }

        internal void InitializeInstrumentSettlementPrice(IDataReader dr)
        {
            SettingInitializer.InitializeSettlementPrice(dr, _instruments);
        }

        internal void InitializeQuotePolicyDetail(IDataReader dr)
        {
            var item = new QuotePolicyDetail(new DBReader(dr));
            _quotePolicyDetails.Add(new QuotePolicyInstrumentIdPair(item.QuotePolicyId, item.InstrumentId), item);
        }

        internal void InitializeInstrumentDayQuotation(IDataReader dr)
        {
            Instrument instrument = _instruments[(Guid)dr["InstrumentID"]];
            instrument.DayQuotation = new DayQuotation(new DBReader(dr), instrument);
        }


        internal void InitializeTradePolicy(IDataReader dr)
        {
            var item = new TradePolicy(new DBReader(dr));
            this.AddCommon(_tradePolicies, item.ID, item);
        }

        internal void InitializeTradePolicyDetail(IDataReader dr)
        {
            Guid tradePolicyID = (Guid)dr["TradePolicyID"];
            if (!_tradePolicies.ContainsKey(tradePolicyID)) return;
            TradePolicy tradePolicy = _tradePolicies[tradePolicyID];
            TradePolicyDetail tradePolicyDetail = new TradePolicyDetail(new DBReader(dr), tradePolicy);
            var key = new TradePolicyDetailKey(tradePolicyDetail.InstrumentId, tradePolicyID);
            this.AddCommon(_tradePolicyDetails, key, tradePolicyDetail);
        }


        internal void InitializeInstalmentPolicy(IDataReader dr)
        {
            InstalmentPolicy instalmentPolicy = new InstalmentPolicy(new DBReader(dr));
            this.AddCommon(_instalmentPolicies, instalmentPolicy.Id, instalmentPolicy);
        }


        internal void InitializeInstalmentPolicyDetail(IDataReader dr)
        {
            InstalmentPolicyDetail instalmentPolicyDetail = new InstalmentPolicyDetail(new DBReader(dr));
            if (!_instalmentPolicies.ContainsKey(instalmentPolicyDetail.InstalmentPolicyId))
            {
                throw new InitializeSettingException(string.Format("Can't find instalment policy, id={0}, period = {1}", instalmentPolicyDetail.InstalmentPolicyId, instalmentPolicyDetail.Period.Period));
            }
            this._instalmentPolicies[instalmentPolicyDetail.InstalmentPolicyId].Add(instalmentPolicyDetail);
        }

        internal void InitializeSpecialTradePolicy(IDataReader dr)
        {
            SpecialTradePolicy specialTradePolicy = new SpecialTradePolicy(new DBReader(dr));
            this.AddCommon(_specialTradePolicies, specialTradePolicy.Id, specialTradePolicy);
        }

        internal void InitializeSpecialTradePolicyDetail(IDataReader dr)
        {
            if (_specialTradePolicies.ContainsKey((Guid)dr["SpecialTradePolicyID"]))
            {
                SpecialTradePolicy specialTradePolicy = this._specialTradePolicies[(Guid)dr["SpecialTradePolicyID"]];
                SpecialTradePolicyDetail specialTradePolicyDetail = new SpecialTradePolicyDetail(new DBReader(dr), specialTradePolicy);
            }
        }

        internal void InitializeVolumeNecessary(IDataReader dr)
        {
            VolumeNecessary volumeNecessary = new VolumeNecessary(new DBReader(dr));
            this.AddCommon(_volumeNecessaries, volumeNecessary.Id, volumeNecessary);
        }

        internal void InitializeVolumeNecessaryDetail(IDataReader dr)
        {
            VolumeNecessaryDetail volumeNecessaryDetail = new VolumeNecessaryDetail(new DBReader(dr));
            if (!_volumeNecessaries.ContainsKey(volumeNecessaryDetail.VolumeNecessaryId))
            {
                throw new InitializeSettingException(string.Format("Can't find volume necessary, id={0}", volumeNecessaryDetail.VolumeNecessaryId));
            }
            this._volumeNecessaries[volumeNecessaryDetail.VolumeNecessaryId].Add(volumeNecessaryDetail);
        }

        internal void InitializePhysicalPaymentDiscount(IDataReader dr)
        {
            PhysicalPaymentDiscountPolicy physicalPaymentDiscountPolicy = new PhysicalPaymentDiscountPolicy(new DBReader(dr));
            this.AddCommon(_physicalPaymentDiscountPolicyDict, physicalPaymentDiscountPolicy.ID, physicalPaymentDiscountPolicy);
        }

        internal void InitializePhysicalPaymentDiscountDetail(IDataReader dr)
        {
            PhysicalPaymentDiscountPolicyDetail detail = new PhysicalPaymentDiscountPolicyDetail(new DBReader(dr));
            var policy = this.GetPhysicalPaymentDiscountPolicy(detail.PhysicalPaymentDiscountID);
            policy.Update(detail);
        }

        internal void InitializeDealingPolicy(IDataReader dr)
        {
            DealingPolicy dealingPolicy = new DealingPolicy(new DBReader(dr));
            this.AddCommon(_dealingPolicies, dealingPolicy.ID, dealingPolicy);
        }

        internal void InitializeDealingPolicyDetail(IDataReader dr)
        {
            var dealingPolicyId = (Guid)dr["DealingPolicyID"];
            if (_dealingPolicies.ContainsKey(dealingPolicyId))
            {
                DealingPolicy dealingPolicy = this._dealingPolicies[dealingPolicyId];
                DealingPolicyDetail dealingPolicyDetail = new DealingPolicyDetail(new DBReader(dr), dealingPolicy);
            }
        }

        internal void InitializeInterestPolicy(IDataReader dr)
        {
            var interestPolicy = new InterestPolicy(new DBReader(dr));
            this.AddCommon(_interestPolicies, interestPolicy.Id, interestPolicy);
        }

        internal void InitializeBlotter(IDataReader dr)
        {
            Blotter blotter = new Blotter(new DBReader(dr));
            this.AddCommon(_blotters, blotter.ID, blotter);
        }

        internal void InitializeCustomer(IDataReader dr)
        {
            Customer customer = new Customer(new DBReader(dr));
            this.AddCommon(_customers, customer.Id, customer);
        }

        internal void InitializeAccount(IDataReader dr)
        {
            Account account = new Account(new DBReader(dr));
            this.AddCommon(_accounts, account.Id, account);
        }

        #endregion


        private void AddCommon<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
            }
        }


        internal PhysicalPaymentDiscountPolicy GetPhysicalPaymentDiscountPolicy(Guid id)
        {
            return _physicalPaymentDiscountPolicyDict[id];
        }

        private void InitializeCurrencies(DataSet ds)
        {
            foreach (var currency in SettingInitializer.InitializeCurrencies(ds))
            {
                if (this._currencies == null)
                {
                    this._currencies = new Dictionary<Guid, Currency>(CURRENCY_CAPACITY);
                }
                this._currencies.Add(currency.Id, currency);
            }
        }

        private void InitializeCurrencyRates(DataSet ds)
        {
            foreach (var currencyRate in SettingInitializer.InitializeCurrencyRates(ds, _currencies))
            {
                if (_currencyRates == null)
                {
                    _currencyRates = new Dictionary<CurrencyIdPair, CurrencyRate>(CURRENCY_CAPACITY);
                }
                if (currencyRate != null)
                {
                    var pair = new CurrencyIdPair(currencyRate.SourceCurrency.Id, currencyRate.TargetCurrency.Id);
                    _currencyRates.Add(pair, currencyRate);
                }
            }
        }

        internal void UpdateTradeDay(Protocal.UpdateTradeDayInfoMarketCommand command)
        {
            _tradeDay = new TradeDay(command.TradeDay, command.BeginTime, command.EndTime, command.IsTrading);
        }


        internal void UpdateVolumeNecessaryOfTradePolicyDetail()
        {
            foreach (TradePolicyDetail eachDetail in _tradePolicyDetails.Values)
            {
                if (eachDetail.VolumeNecessaryId != null && _volumeNecessaries.ContainsKey(eachDetail.VolumeNecessaryId.Value))
                {
                    eachDetail.VolumeNecessary = _volumeNecessaries[eachDetail.VolumeNecessaryId.Value];
                }
            }
        }
    }


    internal sealed class SettingInfoPool : Protocal.PoolBase<SettingInfo>
    {
        internal static readonly SettingInfoPool Default = new SettingInfoPool();

        static SettingInfoPool() { }
        private SettingInfoPool() { }

        internal SettingInfo Get()
        {
            return this.Get(() => new SettingInfo(), m => m.Clear());
        }

    }



    public sealed class UpdateSettingException : Exception
    {
        public UpdateSettingException(string nodeName, string operationMethod, string msg)
            : base(msg)
        {
            this.NodeName = nodeName;
            this.OperationMethod = operationMethod;
        }
        public string NodeName { get; private set; }
        public string OperationMethod { get; private set; }
    }

    internal sealed class InitializeSettingException : Exception
    {
        internal InitializeSettingException(string msg)
            : base(msg)
        {
        }
    }

    internal struct TradePolicyDetailKey : IEquatable<TradePolicyDetailKey>
    {
        private Guid _instrumentId;
        private Guid _tradePolicyId;

        internal TradePolicyDetailKey(Guid instrumentId, Guid tradePolicyId)
        {
            _instrumentId = instrumentId;
            _tradePolicyId = tradePolicyId;
        }

        internal Guid InstrumentId
        {
            get { return _instrumentId; }
        }

        internal Guid TradePolicyId
        {
            get { return _tradePolicyId; }
        }

        public bool Equals(TradePolicyDetailKey other)
        {
            return this.InstrumentId == other.InstrumentId && this.TradePolicyId == other.TradePolicyId;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((TradePolicyDetailKey)obj);
        }

        public override int GetHashCode()
        {
            return this.InstrumentId.GetHashCode() ^ this.TradePolicyId.GetHashCode();
        }

        public override string ToString()
        {
            return string.Format("instrumentId = {0}, tradePolicyId = {1}", this.InstrumentId, this.TradePolicyId);
        }

    }
}
