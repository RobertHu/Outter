using Core.TransactionServer.Agent.Quotations;
using Core.TransactionServer.Agent.Settings;
using Core.TransactionServer.Engine;
using Core.TransactionServer.Engine.iExchange;
using iExchange.Common;
using log4net;
using Protocal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Core.TransactionServer.Agent.BLL.TransactionBusiness
{
    //internal sealed class TryExecuteService
    //{
    //    private static readonly ILog Logger = LogManager.GetLogger(typeof(TryExecuteService));
    //    private Transaction _owner;
    //    private TransactionSettings _settings;
    //    internal TryExecuteService(Transaction tran, TransactionSettings settings)
    //    {
    //        _owner = tran;
    //        _settings = settings;
    //    }

    //    internal bool TryExecute()
    //    {
    //        return true;
    //        //if (!SettingFacade.Default.SettingManager.SystemParameter.NeedsOldPlaceCheck(_owner.OrderType)) return true;
    //        //Logger.Info("in try execute");
    //        //Price buyPrice, sellPrice;
    //        //this.GetTryExecutePrice(out buyPrice, out sellPrice);
    //        //var executeRequest = new ExecuteEventArgs(_owner.Owner, _owner, buyPrice, sellPrice);
    //        //tradingEngine.Execute(executeRequest);
    //        //tradingEngine.CancelExecute(new CancelExecuteEventArgs(_owner.Owner.Id, _owner.Id));
    //        //return executeRequest.IsSuccess;
    //    }

    //    private void GetTryExecutePrice(out Price buy, out Price sell)
    //    {
    //        buy = sell = null;
    //        if (_owner.OrderType == OrderType.SpotTrade)
    //        {
    //            this.TryExecutePriceForSportOrder(out buy, out sell);
    //        }
    //        else if (_owner.OrderType == OrderType.Market)
    //        {
    //            this.TryExecutePriceForMarketOrder(out buy, out sell);
    //        }
    //    }

    //    private void TryExecutePriceForMarketOrder(out Price buy, out Price sell)
    //    {
    //        buy = sell = null;
    //        var quotation = _owner.AccountInstrument.GetQuotation(_owner.SubmitorQuotePolicyProvider);
    //        buy = quotation.BuyPrice;
    //        sell = quotation.SellPrice;
    //        if (buy == null || sell == null)
    //        {
    //            throw new TransactionServerException(TransactionError.HasNoQuotationExists);
    //        }
    //    }

    //    private void TryExecutePriceForSportOrder(out Price buy, out Price sell)
    //    {
    //        buy = sell = null;
    //        foreach (Order eachOrder in _owner.Orders)
    //        {
    //            eachOrder.GetBuyAndSellSetPrice(out buy, out sell);
    //        }
    //    }

    //}

}
