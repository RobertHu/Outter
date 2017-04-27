using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using log4net;

namespace Core.TransactionServer.Agent.Caching
{
    internal static class CacheFileManager
    {
        private enum FileAction
        {
            Copy,
            Move
        }


        private static readonly ILog Logger = LogManager.GetLogger(typeof(CacheFileManager));

        private static readonly string RootDirectory;
        private static readonly string BackUpDirPrex;


        static CacheFileManager()
        {
            RootDirectory = Path.Combine(GetRootDir(), "BackupData");
            BackUpDirPrex = Path.Combine(RootDirectory, "TradingData");
        }


        internal static void Copy(string sourcePath)
        {
            ActionCommon(sourcePath, FileAction.Copy);
        }

        internal static string GetRootDir()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }


        internal static void Move(string sourcePath)
        {
            ActionCommon(sourcePath, FileAction.Move);
        }

        private static void ActionCommon(string sourcePath, FileAction action)
        {
            try
            {
                string targetPath = DoCommon(sourcePath);
                if (!File.Exists(targetPath))
                {
                    if (action == FileAction.Copy)
                    {
                        File.Copy(sourcePath, targetPath);
                    }
                    else if (action == FileAction.Move)
                    {
                        File.Move(sourcePath, targetPath);
                    }
                }
                else
                {
                    Logger.WarnFormat("file path = {0} already exists", targetPath);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

        }


        private static string DoCommon(string sourcePath)
        {
            string dirName = GetDirectoryName();
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            string fileName = Path.GetFileName(sourcePath);
            return Path.Combine(dirName, fileName);
        }

        private static string GetDirectoryName()
        {
            if (!Directory.Exists(RootDirectory))
            {
                Directory.CreateDirectory(RootDirectory);
            }
            return string.Format("{0}{1}", BackUpDirPrex, DateTime.Now.ToString("yyyy-MM-dd"));
        }
    }
}
