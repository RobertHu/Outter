using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using iExchange.Common;
using System.Threading.Tasks;
using System.Data;

using CachingAssistant = iExchange.Common.Caching.Transaction.Assistant;
using System.Threading;
using System.Diagnostics;
using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.AccountClass;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Agent.Market;
using System.Xml.Linq;
using log4net;
using Core.TransactionServer.Agent.BinaryOption;
using Core.TransactionServer.Agent.Framework;
using Core.TransactionServer.Agent.Periphery;
using System.IO;
using Core.TransactionServer.Agent.Reset;
using Core.TransactionServer.Agent.Interact;
using System.ServiceModel;
using Core.TransactionServer.Engine.iExchange;
using Core.TransactionServer.Agent.Service;
using iExchange.Common.Caching.Transaction;
using Core.TransactionServer.Agent.BLL.TransactionBusiness;
using Protocal;
using Core.TransactionServer.Agent.BLL.Transfers;
using Core.TransactionServer.Agent.Physical.Delivery;
using Core.TransactionServer.Agent.BLL.InstrumentBusiness;
using Core.TransactionServer.Agent.BLL;
using Core.TransactionServer.Agent.BLL.AccountBusiness;

namespace Core.TransactionServer.Agent
{
    public sealed class Server
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Server));
        private ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private string _id;
        private string _cachePath;


        public Server(string id, string cacheName)
        {
            _id = id;
            var currentDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            _cachePath = Path.Combine(currentDir, cacheName);
            if (!Directory.Exists(_cachePath))
            {
                Directory.CreateDirectory(_cachePath);
            }
        }

        public void Start()
        {
            _readWriteLock.EnterWriteLock();
            try
            {
                InteractFacade.Default.Initialize(iExchangeEngine.Default);
                TransactionExpireChecker.Default.Initialize(InteractFacade.Default.TradingEngine);
                Caching.CacheCenter.Default.Initialize(_cachePath, ExternalSettings.Default.DBConnectionString);
                if (!Caching.CacheCenter.Default.FlushToDB())
                {
                    Logger.Error("Cache files flush to db failed");
                    throw new FlushToDBException();
                }
                InstrumentTradingStateManager.Default.Start();
                Settings.Setting.Default.SettingInfo.InstruemntUpdated += PriceAlert.Manager.Default.OnInstrumentUpdated;
                Settings.Setting.Default.SettingInfo.InstruemntUpdated += Market.MarketManager.Default.OnInstrumentUpdated;
                Settings.Setting.Default.SettingInfo.InstruemntUpdated += TradingSetting.Default.OnInstrumentUpdated;
                Thread thread = new Thread(() => ServerRunner.Run(_id));
                thread.IsBackground = true;
                thread.Start();
            }
            catch (FlushToDBException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }


        public void Update(AppType appType, XElement updateNode)
        {
            _readWriteLock.EnterWriteLock();
            try
            {
                InstrumentPriceStatusManager.Default.Update(updateNode);
                Settings.Setting.Default.SettingInfo.Update(updateNode);
                TradingSetting.Default.Update(appType, updateNode);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }

        public TransactionError Place(Protocal.TransactionData tranData)
        {
            string tranCode;
            return PlaceCommon(tranData, out tranCode);
        }

        internal TransactionError Place(Protocal.TransactionData tranData, out string tranCode)
        {
            return PlaceCommon(tranData, out tranCode);
        }


        internal TransactionError DeleteOrder(Guid accountId, Guid orderId, bool isPayForInstalmentDebitInterest, Guid? deliveryRequestId)
        {
            return this.CallByRead(() =>
            {
                var account = GetAccount(accountId);
                Debug.Assert(account != null);
                return account.DeleteOrder(orderId, isPayForInstalmentDebitInterest, deliveryRequestId);
            },
            () => TransactionError.RuntimeError);
        }


        internal long PlaceBatchOrders(List<TransactionData> trans)
        {
            return this.CallByRead(() =>
             {
                 Stopwatch stopwatch = new Stopwatch();
                 stopwatch.Start();
                 Parallel.ForEach(trans, tran =>
                 {
                     var account = this.GetAccount(tran.AccountId);
                     string tranCode = string.Empty;
                     TransactionError error = account.Place(tran, out tranCode);
                 });
                 stopwatch.Stop();
                 return stopwatch.ElapsedTicks;
             }, () => 0);
        }


        private TransactionError PlaceCommon(Protocal.TransactionData tranData, out string tranCode)
        {
            _readWriteLock.EnterReadLock();
            tranCode = null;
            try
            {
                Logger.Warn("Place order");
                var account = GetAccount(tranData.AccountId);
                TransactionError tranError = account.Place(tranData, out tranCode);
                return tranError;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return TransactionError.RuntimeError;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        public TransactionError Cancel(Token token, Guid accountId, Guid tranId, CancelReason cancelReason)
        {
            return this.CallByRead(() =>
            {
                var account = GetAccount(accountId);
                return account.Cancel(token, tranId, cancelReason);
            },
            () => TransactionError.RuntimeError);
        }


        public string GetInitializeData(List<Guid> accountIds)
        {
            return this.CallByRead(() =>
                {
                    if (accountIds == null || accountIds.Count == 0) return string.Empty;

                    StringBuilder sb = StringBuilderPool.Default.Get();
                    sb.Append("<Accounts>");
                    foreach (var eachAccountId in accountIds)
                    {
                        Stopwatch watch = Stopwatch.StartNew();
                        var account = TradingSetting.Default.GetAccount(eachAccountId);
                        if (account == null)
                        {
                            Logger.InfoFormat("GetInitializeData id = {0} not exists", eachAccountId);
                            continue;
                        }
                        account.GetInitializeData(sb);
                        watch.Stop();
                        Logger.InfoFormat("GetInitializeData cost time = {0}ms, id = {1}", watch.ElapsedMilliseconds, eachAccountId);
                    }
                    sb.Append("</Accounts>");
                    string result = sb.ToString();
                    StringBuilderPool.Default.Add(sb);
                    return result;
                },
                () => string.Empty);
        }

        public string GetAllAccountsInitData()
        {
            return this.CallByRead(() =>
            {
                StringBuilder sb = Protocal.StringBuilderCache.Acquire(100000);
                sb.Append("<Accounts>");
                TradingSetting.Default.DoWorkForAccounts(m => m.GetInitializeData(sb));
                sb.Append("</Accounts>");
                return Protocal.StringBuilderCache.GetStringAndRelease(sb);
            }, () => string.Empty);
        }

        private void CheckAccountRisk(IEnumerable<Protocal.InstrumentStatusInfo> instruments)
        {
            foreach (var eachInstrumentInfo in instruments)
            {
                var settingInstrument = Settings.Setting.Default.GetInstrument(eachInstrumentInfo.Id);
                settingInstrument.MustUseNightNecessaryWhenTrading = true;
            }

#if INSTRUMENTCLOSE
            var account = this.GetAccount(Guid.Parse("FBEE5652-0F6A-4F2A-A6B1-A477D4DF6420"));
            foreach (var eachInstrumentInfo in instruments)
            {
                if (account.ExistInstrument(eachInstrumentInfo.Id))
                {
                    Debug.Assert(false);
                    account.CheckRisk();
                    break;
                }
            }
#else
            TradingSetting.Default.CheckAccountsRisk(instruments);
#endif

            foreach (var eachInstrumentInfo in instruments)
            {
                var settingInstrument = Settings.Setting.Default.GetInstrument(eachInstrumentInfo.Id);
                settingInstrument.MustUseNightNecessaryWhenTrading = false;
            }
        }


        internal void SetDailyClosePrice(Guid instrumentId, DateTime tradeDay, List<TradingDailyQuotation> closeQuotations)
        {
            this.CallByRead(() =>
            {
                Logger.InfoFormat("SetDailyClosePrice instrumentId = {0}, tradeDay = {1}", instrumentId, tradeDay);
#if NOBROADCAST
                var account = this.GetAccount(Guid.Parse("1b8d3e62-bd6d-40d0-b8fa-00c0d1565c15"));
                Debug.Assert(false);
                account.DoInstrumentReset(instrumentId, tradeDay, closeQuotations);
#else
                TradingSetting.Default.DoParallelForAccounts(account =>
                    {
                        if (account.ExistInstrument(instrumentId))
                        {
                            account.DoInstrumentReset(instrumentId, tradeDay, closeQuotations);
                        }
                    });
#endif
            });
        }


        internal void DoSystemReset(DateTime tradeDay)
        {
            this.CallByRead(() =>
            {
                TradingSetting.Default.DoParallelForAccounts(account => account.DoSystemReset(tradeDay));
                ResetManager.Default.Clear();
            });
        }

        internal void UpdateTradeDayInfo(Protocal.UpdateTradeDayInfoMarketCommand command)
        {
            this.CallByWrite(() => Settings.Setting.Default.SettingInfo.UpdateTradeDay(command));
        }

        public void UpdateInstrumentsTradingStatus(Dictionary<Protocal.TradingInstrument.InstrumentStatus, List<Protocal.InstrumentStatusInfo>> instrumentStatusDict)
        {
            this.CallByWrite(() =>
            {
                foreach (var eachStatus in instrumentStatusDict.Keys)
                {
                    if (eachStatus == Protocal.TradingInstrument.InstrumentStatus.DayClose)
                    {
                        this.CheckAccountRisk(instrumentStatusDict[eachStatus]);
                    }
                    foreach (var eachInstrumentInfo in instrumentStatusDict[eachStatus])
                    {
                        this.UpdateInstrumentTradingStatus(eachInstrumentInfo.Id, eachStatus);
                    }
                }
            });
        }


        private void UpdateInstrumentTradingStatus(Guid instrumentId, Protocal.TradingInstrument.InstrumentStatus status)
        {
            if (TradingSetting.Default.ExistsInstrument(instrumentId))
            {
                var instrument = TradingSetting.Default.GetInstrument(instrumentId);
                instrument.TradingStatus.UpdateStatus(status);
            }
        }


        public void UpdateInstrumentDayOpenCloseTime(Protocal.UpdateInstrumentDayOpenCloseTimeMarketCommand command)
        {
            this.CallByWrite(() =>
                {
                    foreach (var eachDayCloseInfo in command.Records)
                    {
                        var instrument = TradingSetting.Default.GetInstrument(eachDayCloseInfo.Id);
                        Logger.InfoFormat("UpdateInstrumentDayOpenCloseTime id={0} tradeDay= {1},dayOpenTime = {2}, dayCloseTime= {3}, valueDate = {4}, nextDayOpenTime = {5}, realValueDate = {6}",
                            eachDayCloseInfo.Id, eachDayCloseInfo.TradeDay, eachDayCloseInfo.DayOpenTime, eachDayCloseInfo.DayCloseTime, eachDayCloseInfo.ValueDate, eachDayCloseInfo.NextDayOpenTime, eachDayCloseInfo.RealValueDate);
                        instrument.UpdateTradingTime(eachDayCloseInfo.DayOpenTime, eachDayCloseInfo.DayCloseTime, eachDayCloseInfo.ValueDate, eachDayCloseInfo.NextDayOpenTime);
                        ResetManager.Default.Add(eachDayCloseInfo.Id, eachDayCloseInfo.TradeDay, eachDayCloseInfo.DayOpenTime, eachDayCloseInfo.DayCloseTime, eachDayCloseInfo.ValueDate, eachDayCloseInfo.RealValueDate);
                    }
                });
        }

        internal TransactionError ApplyDelivery(Protocal.Physical.DeliveryRequestData requestData, out string code, out string balance, out string usableMargin)
        {
            _readWriteLock.EnterReadLock();
            code = balance = usableMargin = null;
            try
            {
                var account = GetAccount(requestData.AccountId);
                var transactionError = account.ApplyDelivery(requestData, out code, out balance, out usableMargin);
                if (transactionError != TransactionError.OK)
                {
                    account.RejectChanges();
                }
                return transactionError;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return TransactionError.RuntimeError;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        internal TransactionError ApplyDelivery(Protocal.Physical.DeliveryRequestData requestData)
        {
            _readWriteLock.EnterReadLock();
            try
            {
                var account = GetAccount(requestData.AccountId);
                var transactionError = account.ApplyDelivery(requestData);
                if (transactionError != TransactionError.OK)
                {
                    account.RejectChanges();
                }
                return transactionError;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return TransactionError.RuntimeError;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        internal bool CancelDelivery(Guid userId, Guid deliveryRequestId, out Guid accountId, out int status)
        {
            _readWriteLock.EnterReadLock();
            status = (int)DeliveryRequestStatus.Cancelled;
            accountId = Guid.Empty;
            try
            {
                DeliveryRequest deliveryRequest = DeliveryRequestManager.Default[deliveryRequestId];
                if (deliveryRequest == null
                    || deliveryRequest.DeliveryRequestStatus == DeliveryRequestStatus.Delivered
                    || deliveryRequest.DeliveryRequestStatus == DeliveryRequestStatus.OrderCreated)
                {
                    return false;
                }

                if (deliveryRequest.DeliveryRequestStatus == DeliveryRequestStatus.Cancelled
                    || deliveryRequest.DeliveryRequestStatus == DeliveryRequestStatus.Hedge)
                {
                    return true;
                }
                Account account = this.GetAccount(deliveryRequest.AccountId);
                accountId = account.Id;
                return account.CancelDelivery(deliveryRequestId, out status);

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        internal bool NotifyDelivery(Guid deliveryRequestId, DateTime availableDeliveryTime, string title, string notifyMessage, out Guid accountId)
        {
            _readWriteLock.EnterReadLock();
            accountId = Guid.Empty;
            try
            {
                DeliveryRequest deliveryRequest = DeliveryRequestManager.Default[deliveryRequestId];
                if (deliveryRequest == null || deliveryRequest.DeliveryRequestStatus == DeliveryRequestStatus.Delivered)
                {
                    return false;
                }
                accountId = deliveryRequest.AccountId;
                var account = GetAccount(accountId);
                account.NotifyDelivery(deliveryRequest, availableDeliveryTime);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(string.Format("NotifyDelivery {0} availableDeliveryTime={1}, title={2}, notifyMessage={3}", deliveryRequestId, availableDeliveryTime, title, notifyMessage), ex);
                return false;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }


        public void NotifyDeliveryApproved(Guid accountId, Guid deliveryRequestId, Guid approvedId, DateTime approvedTime, DateTime deliveryTime)
        {
            this.CallByRead(() =>
            {
                var account = this.GetAccount(accountId);
                account.NotifyDeliveryApproved(deliveryRequestId, approvedId, approvedTime, deliveryTime);
            });
        }

        public bool NotifyDeliveried(Guid accountId, Guid deliveryRequestId)
        {
            return this.CallByRead(() =>
            {
                var account = this.GetAccount(accountId);
                return account.NotifyDeliveried(deliveryRequestId);
            }, () => false);
        }


        internal TransactionError ApplyTransfer(Guid userID, Guid sourceAccountId, Guid sourceCurrencyID, decimal sourceAmount, Guid targetAccountID, Guid targetCurrencyID, decimal targetAmount, decimal rate, DateTime expireDate)
        {
            return this.CallByRead(() =>
            {
                var account = GetAccount(sourceAccountId);
                return account.ApplyTransfer(userID, sourceCurrencyID, sourceAmount, targetAccountID, targetCurrencyID, targetAmount, rate, expireDate);
            },
            () => TransactionError.RuntimeError);
        }

        internal bool ChangeLeverage(Guid accountId, int leverage, out decimal necessary)
        {
            _readWriteLock.EnterReadLock();
            necessary = 0m;
            try
            {
                var account = this.GetAccount(accountId);
                return account.ChangeLeverage(leverage, out necessary);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }


        internal TransactionError AcceptTransfer(Guid userId, Guid transferID)
        {
            return this.CallByRead(() => this.AcceptOrDeclineTransfer(userId, transferID, TransferAction.Accept), () => TransactionError.RuntimeError);
        }


        internal TransactionError DeclineTransfer(Guid userId, Guid transferID)
        {
            return this.CallByRead(() => this.AcceptOrDeclineTransfer(userId, transferID, TransferAction.Decline), () => TransactionError.RuntimeError);
        }

        private TransactionError AcceptOrDeclineTransfer(Guid userId, Guid transferID, TransferAction action)
        {
            Guid accountId, currencyId;
            decimal amount;
            TransactionError error = TransferManager.AcceptTransfer(userId, transferID, action, out accountId, out currencyId, out amount);
            var account = GetAccount(accountId);
            account.AddDeposit(currencyId,  amount, true);
            account.SaveAndBroadcastChanges();
            Broadcaster.Default.Add(BroadcastBLL.CommandFactory.CreateUpdateBalanceCommand(accountId, currencyId, amount, ModifyType.Add));
            return error;
        }


        internal List<Protocal.Physical.OrderInstalmentData> GetOrderInstalments(Guid orderId)
        {
            return this.CallByRead(() => TradingSetting.Default.GetInstalments(orderId), () => null);
        }

        internal TransactionError PrePayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, Protocal.Physical.TerminateData terminateData)
        {
            return this.CallByRead(() =>
                {
                    var account = GetAccount(accountId);
                    return account.PrePayForInstalment(submitorId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, terminateData);
                }, () => TransactionError.RuntimeError);
        }

        public TransactionError InstalmentPayoff(Guid submitorId, Guid accountId, Guid currencyId, decimal sumSourcePaymentAmount, decimal sumSourceTerminateFee, List<Protocal.Physical.InstalmentData> instalments)
        {
            return this.CallByRead(() =>
                {
                    var account = GetAccount(accountId);
                    return account.InstalmentPayoff(submitorId, currencyId, sumSourcePaymentAmount, sumSourceTerminateFee, instalments);
                }, () => TransactionError.RuntimeError);
        }


        public TransactionError Execute(Guid accountId, Guid tranID, string buyPrice, string sellPrice, string lot, Guid executedOrderID)
        {
            return this.CallByRead(() =>
                {
                    var account = GetAccount(accountId);
                    if (account == null) return TransactionError.IsNotAccountOwner;
                    return account.Execute(tranID, buyPrice, sellPrice, lot, executedOrderID);
                },
                () => TransactionError.RuntimeError);
        }

        public long ExecuteBatchOrders(List<Protocal.Test.ExecuteInfo> executeInfos)
        {
            return this.CallByRead(() =>
            {
                Stopwatch watch = Stopwatch.StartNew();
                Parallel.ForEach(executeInfos, info =>
                    {
                        var account = this.GetAccount(info.AccountId);
                        if (account != null)
                        {
                            account.Execute(info.TranId, info.BuyPrice, info.SellPrice, info.lot, info.ExecuteOrderId);
                        }
                    });
                watch.Stop();
                return watch.ElapsedTicks;
            }, () => 0);
        }


        public TransactionError MultipleClose(Guid accountId, Guid[] orderIds)
        {
            return this.CallByRead(() =>
            {
                var account = GetAccount(accountId);
                if (account == null) return TransactionError.IsNotAccountOwner;
                return account.MultipleClose(orderIds);
            },
            () => TransactionError.RuntimeError);
        }

        public void ResetHit(Dictionary<Guid, List<Guid>> accountPerOrders)
        {
            this.CallByRead(() =>
            {
                foreach (var eachPair in accountPerOrders)
                {
                    Guid accountId = eachPair.Key;
                    List<Guid> orderIds = eachPair.Value;
                    var account = this.GetAccount(accountId);
                    account.ResetHit(orderIds);
                }
            });
        }

        public Guid[] Rehit(Guid[] orderIds, Guid[] accountIds)
        {
            return this.CallByRead(() => ReHitter.Default.Hit(orderIds, accountIds), () => null);
        }

        internal AccountFloatingStatus GetAccountFloatingStatus(Guid accountId)
        {
            return this.CallByRead(() =>
                  {
                      var account = this.GetAccount(accountId);
                      if (account == null) return null;
                      return account.GetAccountFloatingStatus();
                  }, () => null);
        }


        public iExchange.Common.AlertLevel[] ResetAlertLevel(Guid[] accountIDs)
        {
            return this.CallByRead(() =>
            {
                AlertLevel[] result = new AlertLevel[accountIDs.Length];
                for (int i = 0; i < accountIDs.Length; i++)
                {
                    Account account = GetAccount(accountIDs[i]);
                    if (account == null)
                    {
                        result[i] = AlertLevel.Unknown;
                        continue;
                    }
                    account.ResetAlertLevel();
                    result[i] = account.AlertLevel;
                }
                return result;
            },
            () => null);
        }

        public bool SetPriceAlerts(Guid submiterId, XmlNode priceAlertsNode)
        {
            return this.CallByRead(() => PriceAlert.Manager.Default.Set(submiterId, priceAlertsNode), () => false);
        }

        public TransactionError Book(Token token, TransactionBookData tranData, bool preserveCalculation)
        {
            return this.CallByRead(() =>
                {
                    var account = GetAccount(tranData.AccountId);
                    if (preserveCalculation)
                    {
                        Logger.Error("Book, preserveCalculation = true");
                        return TransactionError.RuntimeError;
                    }
                    else
                    {
                        return account.Book(token, tranData);
                    }
                }, () => TransactionError.RuntimeError);
        }

        public TransactionError AcceptPlace(Guid accountID, Guid tranID)
        {
            return this.CallByRead(() =>
            {
                var account = this.GetAccount(accountID);
                return account.AcceptPlace(tranID);
            },
            () => TransactionError.RuntimeError);
        }


        public void GetAccountInstrumentPrice(Guid accountId, Guid instrumentId, out string buyPrice, out string sellPrice)
        {
            buyPrice = string.Empty;
            sellPrice = string.Empty;
            _readWriteLock.EnterReadLock();
            try
            {
                var account = this.GetAccount(accountId);
                account.GetInstrumentPrice(instrumentId, out buyPrice, out sellPrice);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

        public string GetAccountsProfitWithin(decimal? minProfit, bool includeMinProfit, decimal? maxProfit, bool includeMaxProfit)
        {
            return this.CallByRead(() =>
             {
                 Logger.InfoFormat("GetAccountsProfitWithin minProfit = {0}, includeMinProfit = {1}, maxProfit = {2}, includeMaxProfit = {3}", minProfit, includeMinProfit, maxProfit, includeMaxProfit);
                 return TradingSetting.Default.GetAccountsProfitWithin(minProfit, includeMinProfit, maxProfit, includeMaxProfit);
             }, () => string.Empty);
        }


        private T CallByRead<T>(Func<T> func, Func<T> errorHandle)
        {
            return this.CallGenericCommon(LockType.Read, func, errorHandle);
        }

        private void CallByRead(Action action, Action<Exception> errorHandle = null)
        {
            this.CallCommon(LockType.Read, action, errorHandle);
        }


        private T CallByWrite<T>(Func<T> func, Func<T> errorHandle)
        {
            return this.CallGenericCommon(LockType.Write, func, errorHandle);
        }

        private void CallByWrite(Action action, Action<Exception> errorHandle = null)
        {
            this.CallCommon(LockType.Write, action, errorHandle);
        }


        private void CallCommon(LockType lockType, Action action, Action<Exception> errorHandle)
        {
            if (lockType == LockType.Read) _readWriteLock.EnterReadLock();
            else if (lockType == LockType.Write) _readWriteLock.EnterWriteLock();
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (errorHandle != null) errorHandle(ex);
                else Logger.Error(ex);
            }
            finally
            {
                if (lockType == LockType.Read) _readWriteLock.ExitReadLock();
                else if (lockType == LockType.Write) _readWriteLock.ExitWriteLock();
            }
        }


        private T CallGenericCommon<T>(LockType lockType, Func<T> func, Func<T> errorHandle)
        {
            if (lockType == LockType.Read) _readWriteLock.EnterReadLock();
            else if (lockType == LockType.Write) _readWriteLock.EnterWriteLock();
            try
            {
                return func();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return errorHandle();
            }
            finally
            {
                if (lockType == LockType.Read) _readWriteLock.ExitReadLock();
                else if (lockType == LockType.Write) _readWriteLock.ExitWriteLock();
            }
        }


        private Account GetAccount(Guid accountId)
        {
            return TradingSetting.Default.GetAccount(accountId);
        }

        private enum LockType
        {
            None,
            Read,
            Write
        }

    }

    internal static class ServerRunner
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(ServerRunner));

        internal static void Run(string transactionServerId)
        {
            try
            {
                Initializer.Init(transactionServerId);
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                Logger.Info("begin calculate init");
                TradingSetting.Default.DoParallelForAccounts(account =>
                {
                    account.CalculateInit();
                });
                stopWatch.Stop();
                Logger.InfoFormat("CalculateInit cost time = {0}", stopWatch.ElapsedMilliseconds);
                TradingSetting.Default.CheckAllPlacingAndPlacedTransactions();
                Logger.Info("Begin do reset");
                stopWatch.Start();
                DoReset();
                stopWatch.Stop();
                Logger.InfoFormat("Reset cost time = {0}ms", stopWatch.ElapsedMilliseconds);
                CurrencyRateCaculator.Default.Start();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        private static void DoReset()
        {
            DateTime tradeDay = Setting.Default.GetTradeDay().Day.AddDays(-1);
            ResetManager.Default.LoadInstrumentDayOpenCloseHistorys();
            ResetManager.Default.LoadOrderDayHistorys();
            InstrumentTradingStateManager.Default.UpdateLastResetDay(tradeDay);
            TradingSetting.Default.DoReset(tradeDay);
            ResetManager.Default.Clear();
        }

    }

    public sealed class FlushToDBException : Exception
    {
    }

    internal struct InstrumentDayCloseKey : IEquatable<InstrumentDayCloseKey>
    {
        private Guid _id;
        private DateTime _tradeDay;

        internal InstrumentDayCloseKey(Guid id, DateTime tradeDay)
        {
            _id = id;
            _tradeDay = tradeDay;
        }

        internal Guid Id
        {
            get { return _id; }
        }

        internal DateTime TradeDay
        {
            get { return _tradeDay; }
        }

        public bool Equals(InstrumentDayCloseKey other)
        {
            return this.Id == other.Id && this.TradeDay == other.TradeDay;
        }

        public override bool Equals(object obj)
        {
            return this.Equals((InstrumentDayCloseKey)obj);
        }

        public override int GetHashCode()
        {
            return _id.GetHashCode() ^ _tradeDay.GetHashCode();
        }

    }

}