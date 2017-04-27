using Core.TransactionServer.Agent.DB;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.BLL.AccountBusiness
{
    internal static class LeverageExtension
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LeverageExtension));

        internal static bool ChangeAccountLeverage(this Account account, int leverage, out decimal necessary)
        {
            Logger.Info(string.Format("accountid={0}, leverage={1}", account.Id, leverage));
            bool recoverRateIfException = false;
            decimal oldRateMarginO = 0;
            decimal oldRateMarginD = 0;
            decimal oldRateMarginLockO = 0;
            decimal oldRateMarginLockD = 0;
            var settingAccount = account.Setting();
            necessary = 0;
            try
            {
                oldRateMarginO = settingAccount.RateMarginO;
                oldRateMarginD = settingAccount.RateMarginD;
                oldRateMarginLockO = settingAccount.RateMarginLockO;
                oldRateMarginLockD = settingAccount.RateMarginLockD;

                decimal rate = (decimal)1 / (decimal)leverage;
                settingAccount.ChangeLeverage(rate, rate, rate, rate);
                recoverRateIfException = true;

                Logger.Info(string.Format("before ChangeLeverage Equity={0}, Necessary={1}", account.Equity, account.Necessary));
                account.CalculateRiskData();
                Logger.Info(string.Format("after change leverage Equity={0}, Necessary={1}", account.Equity, account.Necessary));

                necessary = account.Necessary;
                if (account.Equity > account.Necessary)
                {
                    account.SaveLeverageToDB(leverage);
                    account.Leverage = leverage;
                    account.SaveAndBroadcastChanges();
                    account.CheckRisk();
                    return true;
                }
                else
                {
                    account.RejectChanges();
                    settingAccount.ChangeLeverage(oldRateMarginO, oldRateMarginD, oldRateMarginLockO, oldRateMarginLockD);
                    account.CalculateRiskData();
                    return false;
                }
            }
            catch (Exception exception)
            {
                account.RejectChanges();
                if (recoverRateIfException)
                {
                    settingAccount.ChangeLeverage(oldRateMarginO, oldRateMarginD, oldRateMarginLockO, oldRateMarginLockD);
                    account.CalculateRiskData();
                }
                Logger.Error(exception);
                return false;
            }
        }

        private static void SaveLeverageToDB(this Account account, int leverage)
        {
            Protocal.DB.DBRetryHelper.Save(() =>
                {
                    DB.DBMapping.LeverageParameters leverageParameters = new DB.DBMapping.LeverageParameters
                    {
                        AccountId = account.Id,
                        Leverage = leverage,
                        RateMarginO = account.Setting().RateMarginO,
                        RateMarginD = account.Setting().RateMarginD,
                        RateMarginLockO = account.Setting().RateMarginLockO,
                        RateMarginLockD = account.Setting().RateMarginLockD
                    };
                    DBRepository.Default.SaveLeverage(leverageParameters);
                });
        }

    }
}
