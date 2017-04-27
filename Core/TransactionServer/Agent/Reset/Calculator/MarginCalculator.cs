using iExchange.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.TransactionServer.Agent.Reset.Calculator
{
    internal static class MarginCalculator
    {
        internal static decimal CalculateMargin(int marginFormula, decimal lotBalance, decimal contractSize, Price price, Price livePrice, decimal rateIn, decimal rateOut, int decimals, int sourceDecimals)
        {
            decimal result = 0m;
            Price targetPrice = null;
            decimal newRateIn = rateIn;
            decimal newRateOut = rateOut;
            int newSourceDecimals = sourceDecimals;
            if (marginFormula == 6 || marginFormula == 7)
            {
                if (livePrice == null)
                {
                    return result;
                }
                else
                {
                    targetPrice = livePrice;
                }
            }
            else
            {
                targetPrice = price;
            }
            if (marginFormula == 0 || marginFormula == 4 || marginFormula == 5)
            {
                result = lotBalance;
                newRateIn = 1;
                newRateOut = 1;
                newSourceDecimals = decimals;
            }
            else if (marginFormula == 1)
            {
                result = lotBalance * contractSize;
            }
            else if (marginFormula == 2 || marginFormula == 6)
            {
                if (targetPrice != null)
                {
                    result = lotBalance * contractSize / (decimal)targetPrice;
                }
            }
            else if (marginFormula == 3 || marginFormula == 7)
            {
                if (targetPrice != null)
                {
                    result = lotBalance * contractSize * (decimal)targetPrice;
                }
            }
            result = result.Exchange(newRateIn, newRateOut, decimals, newSourceDecimals);
            return result;
        }


        internal static decimal CalculateRptMargin(int marginFormula, decimal lotBalance, decimal contractSize, Price price, decimal? rateIn, decimal? rateout, int decimals, Price refPrice)
        {
            decimal margin = 0m;
            decimal refPriceValue = refPrice == null ? 0m : (decimal)refPrice;
            if (marginFormula == 0)
            {
                margin = lotBalance;
                rateIn = rateout = 1;
            }
            else if (marginFormula == 1)
            {
                margin = lotBalance * contractSize;
            }
            else if (marginFormula == 2)
            {
                margin = lotBalance * contractSize / (decimal)price;
            }
            else if (marginFormula == 3)
            {
                margin = lotBalance * contractSize * (decimal)price;
            }
            else if (marginFormula == 6)
            {
                if (refPriceValue == 0)
                {
                    margin = 0;
                }
                else
                {
                    margin = lotBalance * contractSize / refPriceValue;
                }
            }
            else if (marginFormula == 7)
            {
                margin = lotBalance * contractSize * refPriceValue;
            }
            else if (marginFormula == 4 || marginFormula == 5)
            {
                margin = lotBalance;
            }
            return GetRatedValue(margin, rateIn, rateout, decimals);
        }

        internal static decimal GetRatedValue(decimal value, decimal? rateIn, decimal? rateOut, int decimals)
        {
            decimal result = 0m;
            result = value * (value > 0 ? rateIn ?? 1 : rateOut ?? 1);
            return result.MathRound(decimals);
        }

        internal static decimal CalculateNecessaryMargin(Settings.Setting setting, Guid volumeNecessaryId, decimal netLot, decimal tradePolicyMargin, bool isNight)
        {
            var volumeNecessary = setting.GetVolumeNecessary(volumeNecessaryId, null);
            decimal necessary = 0m;

            if (volumeNecessary.Option == Settings.VolumeNecessaryOption.Progessive)
            {
                necessary = volumeNecessary.CalclateNecessaryWhenExistsVolumeNecessaryDetails(netLot, tradePolicyMargin, isNight);
                if (volumeNecessary.VolumeNecessaryDetails.Count == 0)
                {
                    necessary = netLot * tradePolicyMargin;
                }
            }
            else
            {
                necessary = volumeNecessary.CalculateNecessaryForFlat(netLot, tradePolicyMargin, isNight);
            }
            return necessary;
        }


        private static decimal CalculateNecessaryForFlat(this Settings.VolumeNecessary volumeNecessary, decimal netLot, decimal tradePolicyMargin, bool isNight)
        {
            var detail = volumeNecessary.GetMaxFromDetail(netLot);
            if (detail == null)
            {
                return netLot * tradePolicyMargin;
            }
            else
            {
                return netLot * detail.GetMargin(isNight);
            }
        }

        private static decimal GetMargin(this Settings.VolumeNecessaryDetail volumeNecessaryDetail, bool isNight)
        {
            return isNight ? volumeNecessaryDetail.MarginO : volumeNecessaryDetail.MarginD;
        }

        private static Settings.VolumeNecessaryDetail GetMaxFromDetail(this Settings.VolumeNecessary volumeNecessary, decimal netLot)
        {
            if (volumeNecessary.VolumeNecessaryDetails.Count == 0) return null;
            Settings.VolumeNecessaryDetail result = null;
            decimal maxFrom = decimal.MinValue;
            foreach (var eachDetail in volumeNecessary.VolumeNecessaryDetails)
            {
                if (eachDetail.From < netLot && maxFrom < eachDetail.From)
                {
                    maxFrom = eachDetail.From;
                    result = eachDetail;
                }
            }
            return result;
        }


        private static decimal CalclateNecessaryWhenExistsVolumeNecessaryDetails(this Settings.VolumeNecessary volumeNecessary, decimal netLot, decimal tradePolicyMargin, bool isNight)
        {
            decimal necessary = 0m;
            int detailCount = volumeNecessary.VolumeNecessaryDetails.Count;
            int i = 0;
            while (i < volumeNecessary.VolumeNecessaryDetails.Count)
            {
                var volumeNecessaryDetail = volumeNecessary.VolumeNecessaryDetails[i];
                if (i == 0)
                {
                    necessary = Math.Min(netLot, volumeNecessaryDetail.From) * tradePolicyMargin;
                }
                ++i;
                if (netLot > volumeNecessaryDetail.From)
                {
                    decimal margin = volumeNecessaryDetail.GetMargin(isNight);
                    if (i < detailCount)
                    {
                        var nextDetail = volumeNecessary.VolumeNecessaryDetails[i];
                        decimal lot1 = netLot - volumeNecessaryDetail.From;
                        decimal lot2 = nextDetail.From - volumeNecessaryDetail.From;
                        decimal lot = Math.Min(lot1, lot2);
                        necessary += lot * margin;
                    }
                    else
                    {
                        necessary += (netLot - volumeNecessaryDetail.From) * margin;
                    }
                }
            }
            return necessary;
        }


    }
}
