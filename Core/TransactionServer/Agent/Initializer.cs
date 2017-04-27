using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.BLL.AccountBusiness;
using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Core.TransactionServer.Agent.DB;
using Core.TransactionServer.Agent.Market;
using Core.TransactionServer.Agent.Periphery;
using Core.TransactionServer.Agent.Physical.Delivery;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Reset;
using Core.TransactionServer.Agent.Service;
using Core.TransactionServer.Agent.Settings;
using log4net;
using Protocal.CommonSetting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent
{
    internal static class Initializer
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Initializer));

        internal static void Init(string transactionServerId)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            Logger.Info("begin load initdata and initialized");
            InitDBData(transactionServerId);
            InitializeCommunication();
            stopWatch.Stop();
            Logger.InfoFormat("InitializeDBData cost time = {0}", stopWatch.ElapsedMilliseconds);
        }

        private static void InitDBData(string transactionServerId)
        {
            Dictionary<Guid, Order> orders = new Dictionary<Guid, Order>(1000);
            InitializeByDBReader(transactionServerId, orders);
            if (orders != null)
            {
                foreach (var eachOrder in orders.Values)
                {
                    eachOrder.CalculateInit();
                }
            }
        }

        private static void InitializeByDBReader(string transactionServerId, Dictionary<Guid, Order> orders)
        {
            SqlDataReader reader = DBRepository.Default.GetInitDataByReader(transactionServerId);
            ReadTradeDay(reader);
            ReadSystemParameter(reader);
            ReadCurrency(reader);
            ReadCurrencyRate(reader);
            ReadInstrument(reader);
            MarketManager.Default.LoadInstruments();
            ReadQuotePolicyDetail(reader);
            ReadTradePolicy(reader);
            ReadTradePolicyDetail(reader);
            ReadInstalmentPolicy(reader);
            ReadInstalmentPolicyDetail(reader);
            ReadSpecialTradePolicy(reader);
            ReadSpecialTradePolicyDetail(reader);
            ReadVolumeNecessary(reader);
            ReadVolumeNecessaryDetail(reader);
            Settings.Setting.Default.SettingInfo.UpdateVolumeNecessaryOfTradePolicyDetail();
            ReadPhysicalPaymentDiscount(reader);
            ReadPhysicalPaymentDiscountDetail(reader);
            ReadDealingPolicy(reader);
            ReadDealingPolicyDetail(reader);
            ReadCustomer(reader);
            ReadAccount(reader);
            ReadUnclearDeposit(reader);
            ReadDayQuotation(reader);
            ReadOverridedQuotation(reader);
            ReadAccountEx(reader);
            ReadAccountBalance(reader);
            ReadSettlementPrice(reader);
            Dictionary<Guid, DeliveryRequest> deliveryRequests = new Dictionary<Guid, DeliveryRequest>(100);
            ReadDeliveryRequest(reader, deliveryRequests);
            ReadDeliveryRequestOrderRelation(reader, deliveryRequests);
            Dictionary<Guid, Transaction> trans = new Dictionary<Guid, Transaction>(1000);
            ReadTransaction(reader, trans);
            ReadOrder(reader, trans, orders);
            ReadOrderRelation(reader, orders);
            ReadOrderInstalment(reader, orders);
            ReadOrderPLNotValued(reader, orders);
            ReadBill(reader);
            ReadOrganization(reader);
            ReadOrderType(reader);
            TradingSetting.Default.CreateInstrumentsForAccount();
            TradingSetting.Default.BuildTradingInstruments();
            ReadInstrumentResetStatus(reader);
            ReadAccountResetStatus(reader);
            ReadInterestPolicy(reader);
            ReadBlotter(reader);
            PriceAlert.Manager.Default.Initialize();
            ReadPriceAlert(reader);
            ReadOrderDeletedReason(reader);
            ReadBOBetType(reader);
            ReadBOPolicy(reader);
            ReadBOPolicyDetail(reader);
        }

        private static void ReadTradeDay(SqlDataReader reader)
        {
            if (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeTradeDay(reader);
            }
        }

        private static void ReadSystemParameter(SqlDataReader reader)
        {
            reader.NextResult();
            if (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeSystemParameter(reader);
            }
        }

        private static void ReadCurrency(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeCurrency(reader);
            }
        }

        private static void ReadCurrencyRate(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeCurrencyRate(reader);
            }
        }

        private static void ReadInstrument(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeInstrument(reader);
                InstrumentPriceStatusManager.Default.Initialize(new DBReader(reader));
            }
        }


        private static void ReadQuotePolicyDetail(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeQuotePolicyDetail(reader);
            }
        }

        private static void ReadTradePolicy(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeTradePolicy(reader);
            }
        }

        private static void ReadTradePolicyDetail(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeTradePolicyDetail(reader);
            }
        }

        private static void ReadInstalmentPolicy(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeInstalmentPolicy(reader);
            }
        }

        private static void ReadInstalmentPolicyDetail(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeInstalmentPolicyDetail(reader);
            }
        }

        private static void ReadSpecialTradePolicy(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeSpecialTradePolicy(reader);
            }
        }

        private static void ReadSpecialTradePolicyDetail(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeSpecialTradePolicyDetail(reader);
            }
        }


        private static void ReadVolumeNecessary(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeVolumeNecessary(reader);
            }
        }

        private static void ReadVolumeNecessaryDetail(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeVolumeNecessaryDetail(reader);
            }
        }


        private static void ReadPhysicalPaymentDiscount(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializePhysicalPaymentDiscount(reader);
            }
        }


        private static void ReadPhysicalPaymentDiscountDetail(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializePhysicalPaymentDiscountDetail(reader);
            }
        }

        private static void ReadDealingPolicy(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeDealingPolicy(reader);
            }
        }

        private static void ReadDealingPolicyDetail(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeDealingPolicyDetail(reader);
            }
        }

        private static void ReadCustomer(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeCustomer(reader);
            }
        }


        private static void ReadAccount(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeAccount(reader);
            }
        }

        private static void ReadUnclearDeposit(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeUnclearDeposit(reader);
            }
        }


        private static void ReadDayQuotation(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeInstrumentDayQuotation(reader);
            }
        }

        private static void ReadOverridedQuotation(SqlDataReader reader)
        {
            reader.NextResult();
            Dictionary<Guid, QuotePolicy2QuotationDict> instrumentQuotations = new Dictionary<Guid, QuotePolicy2QuotationDict>();
            while (reader.Read())
            {
                MarketManager.Default.LoadQuotations(new DBReader(reader), instrumentQuotations);
            }
            Logger.InfoFormat("ReadOverridedQuotation count = {0}", instrumentQuotations.Count);
            MarketManager.Default.UpdateQuotation(new QuotationBulk(instrumentQuotations));
        }

        private static void ReadAccountEx(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeAccountEx(reader);
            }
        }

        private static void ReadAccountBalance(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeAccountBalance(reader);
            }
        }

        private static void ReadSettlementPrice(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeInstrumentSettlementPrice(reader);
            }
        }

        private static void ReadDeliveryRequest(SqlDataReader reader, Dictionary<Guid, DeliveryRequest> deliveryRequests)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeDeliveryRequest(reader, deliveryRequests);
            }
        }

        private static void ReadDeliveryRequestOrderRelation(SqlDataReader reader, Dictionary<Guid, DeliveryRequest> deliveryRequests)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeDeliveryRequestOrderRelation(reader, deliveryRequests);
            }
        }


        private static void ReadOrderInstalment(SqlDataReader reader, Dictionary<Guid, Order> orders)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeOrderInstalment(reader, orders);
            }
        }

        private static void ReadTransaction(SqlDataReader reader, Dictionary<Guid, Transaction> trans)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeTransaction(reader, trans);
            }
        }


        private static void ReadOrder(SqlDataReader reader, Dictionary<Guid, Transaction> trans, Dictionary<Guid, Order> orders)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeOrder(reader, trans, orders);
            }
        }

        private static void ReadOrderRelation(SqlDataReader reader, Dictionary<Guid, Order> orders)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeOrderRelation(reader, orders);
            }
        }

        private static void ReadBill(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeBill(reader);
            }
        }

        private static void ReadOrganization(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                OrganizationAndOrderTypeRepository.Default.InitializeOrganization(new DBReader(reader));
            }
        }

        private static void ReadOrderType(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                OrganizationAndOrderTypeRepository.Default.InitializeOrderType(new DBReader(reader));
            }
        }


        private static void ReadInstrumentResetStatus(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeInstrumentResetStatus(reader);
            }
        }

        private static void ReadAccountResetStatus(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeAccountResetStatus(reader);
            }
        }

        private static void ReadInterestPolicy(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeInterestPolicy(reader);
            }
        }


        private static void ReadBlotter(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                Settings.Setting.Default.SettingInfo.InitializeBlotter(reader);
            }
        }

        private static void ReadOrderPLNotValued(SqlDataReader reader, Dictionary<Guid, Order> orders)
        {
            reader.NextResult();
            while (reader.Read())
            {
                TradingSetting.Default.InitializeOrderPLNotValued(new DBReader(reader), orders);
            }
        }

        private static void ReadOrderDeletedReason(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                OrderDeletedReasonRepository.Default.InitializeOrderDeletedReason(new DBReader(reader));
            }
        }


        private static void ReadPriceAlert(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                PriceAlert.Manager.Default.InitializePriceAlert(new DBReader(reader));
            }
        }

        private static void ReadBOBetType(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                BinaryOption.BOBetTypeRepository.Read(new DBReader(reader));
            }
        }


        private static void ReadBOPolicy(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                BinaryOption.BOPolicyRepository.Read(new DBReader(reader));
            }
        }

        private static void ReadBOPolicyDetail(SqlDataReader reader)
        {
            reader.NextResult();
            while (reader.Read())
            {
                BinaryOption.BOPolicyDetailRepository.Default.Read(new DBReader(reader));
            }
        }

        private static void InitializeCommunication()
        {
#if NOBROADCAST
            Hoster.Default.Start();
#else
            Hoster.Default.Start();
            UpdateInstrumentTradingStatus();
            Task.Factory.StartNew(() => RegisterToSystemController());
#endif
        }

        private static void UpdateInstrumentTradingStatus()
        {
            var tradingStatusCommand = ServerFacade.Default.GatewayProxy.GetTradingInstrumentStatusCommand();
            if (tradingStatusCommand != null)
            {
                ServerFacade.Default.Server.UpdateInstrumentsTradingStatus(tradingStatusCommand.InstrumentStatus);
            }
        }

        private static void RegisterToSystemController()
        {
            try
            {
                ServerFacade.Default.GatewayProxy.Register(ExternalSettings.Default.CommandCollectServiceUrl, iExchange.Common.AppType.TransactionServer);
                Logger.InfoFormat("Register CommandCollectServiceUrl = {0}", ExternalSettings.Default.CommandCollectServiceUrl);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }


    }
}
