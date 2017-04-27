using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iExchange.StateServer.Adapter.Util
{
    internal static class PathHelper
    {
        private const string _styleSheetPathFormat = "Adapter/Stylesheet/{0}";

        internal static string GetAccountCurrencyStylesheetPath()
        {
            return GetCurrentDirectory() + GetStyleSheetPath("AccountCurrency.xslt");
        }

        internal static string GetAlertRiskStylesheetPath()
        {
            return GetCurrentDirectory() + GetStyleSheetPath("AlertRiskMonitor.xslt");
        }

        private static string GetStyleSheetPath(string styleSheetName)
        {
            return string.Format(_styleSheetPathFormat, styleSheetName);
        }

        private static string GetCurrentDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }



    }
}