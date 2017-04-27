using log4net;
using Protocal.TradingInstrument;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SystemController.Broadcast;
using SystemController.Factory;
using System.Threading;

namespace SystemController.InstrumentBLL
{
    internal sealed class InstrumentTradingStatusKeeper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InstrumentTradingStatusKeeper));
        internal static readonly InstrumentTradingStatusKeeper Default = new InstrumentTradingStatusKeeper();
        private ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        private InstrumentTradingStatusBuilder _builder = new InstrumentTradingStatusBuilder();

        internal event DayCloseQuotationReceivedHandle DayCloseQuotationReceived;

        static InstrumentTradingStatusKeeper() { }
        private InstrumentTradingStatusKeeper() { }


        internal Protocal.UpdateInstrumentTradingStatusMarketCommand InstrumentTradingStatusCommand
        {
            get
            {
                _readWriteLock.EnterReadLock();
                try
                {
                    Logger.Info("Get InstrumentTradingStatusCommand");
                    return MarketCommandFactory.CreateUpdateInstrumentTradingStatusCommand(_builder.StatusDict);
                }
                finally
                {
                    _readWriteLock.ExitReadLock();
                }
            }
        }


        internal void AddInstrumentStatus(Guid instrumentId, InstrumentStatus status, DateTime checkTime, DateTime tradeDay)
        {
            _readWriteLock.EnterWriteLock();
            try
            {
                Logger.InfoFormat("AddInstrumentStatus instrumentId = {0}, status = {1}, checkTime = {2}, tradeDay = {3}", instrumentId, status, checkTime, tradeDay);
                _builder.Add(instrumentId, status, checkTime, tradeDay);
                if (status == InstrumentStatus.DayCloseQuotationReceived)
                {
                    this.OnDayCloseQuotationReceived(instrumentId, tradeDay);
                }
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }

        private void OnDayCloseQuotationReceived(Guid instrumentId, DateTime tradeDay)
        {
            var handle = this.DayCloseQuotationReceived;
            if (handle != null)
            {
                handle(instrumentId, tradeDay);
            }
        }

        internal void ClientConnectedHanlde(ClientBase client)
        {
            _readWriteLock.EnterReadLock();
            try
            {
                Logger.InfoFormat("client connect, send back instrument trading status command client url = {0}, appType = {1}", client.ServiceUrl, client.AppType);
                if (!_builder.ExistsStatus()) return;
                client.Send(MarketCommandFactory.CreateUpdateInstrumentTradingStatusCommand(_builder.StatusDict));
            }
            finally
            {
                _readWriteLock.ExitReadLock();
            }
        }

    }

    internal sealed class InstrumentDayOpenCloseTimeKeeper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(InstrumentDayOpenCloseTimeKeeper));
        private List<Protocal.UpdateInstrumentDayOpenCloseTimeMarketCommand> _dayOpenCloseCommands = new List<Protocal.UpdateInstrumentDayOpenCloseTimeMarketCommand>(20);
        private Dictionary<Guid, Dictionary<DateTime, Protocal.InstrumentDayOpenCloseTimeRecord>> _instrumentDayOpenCloseDict = new Dictionary<Guid, Dictionary<DateTime, Protocal.InstrumentDayOpenCloseTimeRecord>>(20);
        private ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();

        internal static readonly InstrumentDayOpenCloseTimeKeeper Default = new InstrumentDayOpenCloseTimeKeeper();

        static InstrumentDayOpenCloseTimeKeeper() { }
        private InstrumentDayOpenCloseTimeKeeper() { }


        internal Protocal.UpdateInstrumentDayOpenCloseTimeMarketCommand InstrumentDayOpenCloseTimeMarketCommand
        {
            get
            {
                try
                {
                    _readWriteLock.EnterReadLock();
                    Logger.Info("Get InstrumentDayOpenCloseTimeMarketCommand");
                    List<Protocal.InstrumentDayOpenCloseTimeRecord> result = new List<Protocal.InstrumentDayOpenCloseTimeRecord>(10);
                    foreach (var eachRecordDict in _instrumentDayOpenCloseDict.Values)
                    {
                        foreach (var eachRecord in eachRecordDict.Values)
                        {
                            result.Add(eachRecord);
                        }
                    }
                    return new Protocal.UpdateInstrumentDayOpenCloseTimeMarketCommand
                    {
                        Records = result,
                        SourceType = iExchange.Common.AppType.SystemController
                    };
                }
                finally
                {
                    _readWriteLock.ExitReadLock();
                }
            }
        }

        internal void AddInstrumentDayOpenCloseTimeRecords(List<Protocal.InstrumentDayOpenCloseTimeRecord> records)
        {
            try
            {
                _readWriteLock.EnterWriteLock();
                Logger.Info("AddInstrumentDayOpenCloseTimeRecords");
                foreach (var eachRecord in records)
                {
                    this.AddInstrumentDayOpenCloseTimeRecordCommon(eachRecord);
                }
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }

        internal void AddInstrumentDayOpenCloseTimeRecordByDB(Protocal.InstrumentDayOpenCloseTimeRecord record)
        {
            try
            {
                _readWriteLock.EnterWriteLock();
                Logger.Info("AddInstrumentDayOpenCloseTimeRecordByDB");
                this.AddInstrumentDayOpenCloseTimeRecordCommon(record);
            }
            finally
            {
                _readWriteLock.ExitWriteLock();
            }
        }

        private void AddInstrumentDayOpenCloseTimeRecordCommon(Protocal.InstrumentDayOpenCloseTimeRecord record)
        {
            Dictionary<DateTime, Protocal.InstrumentDayOpenCloseTimeRecord> recordDict;
            if (!_instrumentDayOpenCloseDict.TryGetValue(record.Id, out recordDict))
            {
                recordDict = new Dictionary<DateTime, Protocal.InstrumentDayOpenCloseTimeRecord>();
                _instrumentDayOpenCloseDict.Add(record.Id, recordDict);
            }
            if (recordDict.Count > 0)
            {
                this.RemoveLessThenTradeDayRecords(record.TradeDay, recordDict);
            }
            if (!recordDict.ContainsKey(record.TradeDay))
            {
                recordDict.Add(record.TradeDay, record);
            }
        }


        private void RemoveLessThenTradeDayRecords(DateTime tradeDay, Dictionary<DateTime, Protocal.InstrumentDayOpenCloseTimeRecord> existsRecords)
        {
            List<DateTime> toBeRemovedDays = new List<DateTime>();
            foreach (var eachTradeDay in existsRecords.Keys)
            {
                if (eachTradeDay < tradeDay)
                {
                    toBeRemovedDays.Add(eachTradeDay);
                }
            }

            foreach (var eachTradeDay in toBeRemovedDays)
            {
                existsRecords.Remove(eachTradeDay);
            }
        }

    }



}
