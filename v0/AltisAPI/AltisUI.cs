using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace AltisAPI
{
    public class AltisUI : System.Web.UI.Page
    {
        public string GetConfig(string name)
        {
            try
            {
                return WebConfigurationManager.AppSettings[name];
            }
            catch (Exception ex)
            {
                // to do logging
                return null;
            }
        }
     private string LogPath()
        {
            string logPath = GetConfig("LogPath");
            DirectoryInfo directoryInfo = new DirectoryInfo(logPath);
            if (!directoryInfo.Exists)
                directoryInfo.Create();
            return logPath;
        }
        public bool CheckLogPath()
        {
            try
            {
                return Directory.Exists(LogPath());
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public void Log(string content)
        {
            string logPath = LogPath() + @"\log_"+DateTime.Now.ToString("yyyyMMdd")+".log";
            File.AppendAllText(logPath,DateTime.Now + " : "+ content + Environment.NewLine);
        }
    }
}