using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;

namespace Protocal.DB
{
    public static class DBRetryHelper
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(DBRetryHelper));

        public static void Save(Action action)
        {
            int tryCount = 3;
            while (!Call(action))
            {
                tryCount--;
                Thread.Sleep((int)Math.Pow(2, tryCount) * 500);
                if (tryCount <= 0) break;
            }
        }

        public static void RetryForever(Action action)
        {
            while (!Call(action))
            {
                Thread.Sleep(1000);
            }
        }

        private static bool Call(Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

    }
}
