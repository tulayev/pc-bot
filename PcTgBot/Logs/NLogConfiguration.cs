using NLog;
using System;
using System.IO;
using System.Linq;

namespace PcTgBot.Logs
{
    internal class NLogConfiguration
    {
        private static readonly string _logPath = Path.Combine(ConfigSettings.ProjectFolder, "Logs");

        public static void Setup()
        {
            var config = new NLog.Config.LoggingConfiguration();
            // Targets where to log to: File
            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = Path.Combine(_logPath, $"{DateTime.UtcNow:dd_MM_yyyy_hh_mm_ss}.log"),
                Layout = "${longdate}|${level:uppercase=true}|${logger}|${threadid}|${message}|${exception:format=tostring}"
            };
            // Rules for mapping loggers to targets            
            config.AddRule(LogLevel.Info, LogLevel.Fatal, logfile);
            // Apply config           
            LogManager.Configuration = config;
        }

        public static string GetLatestLogs()
        {
            return string.Join(
                Environment.NewLine, 
                File.ReadAllLines(Directory.GetFiles(_logPath, "*.log").LastOrDefault())
            );
        }
    }
}
