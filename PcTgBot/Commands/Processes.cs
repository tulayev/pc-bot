using System.Diagnostics;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System;
using System.Linq;
using NLog;

namespace PcTgBot.Commands
{
    enum BotState { StartProcess, KillProcess, SearchWeb, SendMessage, Wait }

    internal class Processes
    {
        private readonly PCSystem _system;
        public BotState BotState { get; set; } = BotState.Wait;

        public Processes(PCSystem system)
        {
            _system = system;
        }

        private bool ProcessStarted(string appName)
        {
            var exeFolder = _system.GetApplictionInstallPath(appName);
            BotState = BotState.Wait;

            if (!string.IsNullOrWhiteSpace(exeFolder))
            {
                try
                {
                    var directory = new DirectoryInfo(exeFolder);
                    var files = directory.GetFiles("*.exe");
                    var exeFile = string.Empty;
                    var exePath = string.Empty;
                    var fileSize = files[0].Length;
                    
                    for (var i = 1; i < files.Length; i++)
                    {
                        if (fileSize < files[i].Length)
                            fileSize = files[i].Length;
                    }

                    foreach (FileInfo file in files)
                    {
                        if (file.Length == fileSize)
                            exeFile = file.Name;
                    }

                    exePath = Path.Combine(exeFolder, exeFile);
                    
                    if (File.Exists(exePath))
                    {
                        Process.Start(exePath);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Error(ex);
                }
            }

            return false;
        }

        private bool ProcessKilled(string processName)
        {
            BotState = BotState.Wait;
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                if (process.ProcessName == processName)
                {
                    Process.GetProcessesByName(processName).ToList().ForEach(x => x.Kill());
                    return true;
                }
            }

            return false;
        }

        private string SearchWeb(string query)
        {
            BotState = BotState.Wait;
            Process.Start($"microsoft-edge:https://www.google.com/search?q={query}");
            return "Opened MS Edge for Google search";
        }

        private void SendMessage(string text)
        {
            BotState = BotState.Wait;
            MessageBox.Show(text, "Message by Bot", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public string GetRunningProcessesList()
        {
            var processes = Process.GetProcesses();
            var sb = new StringBuilder();

            sb.AppendLine("Processes' list");
            sb.AppendLine(new string('=', 25));

            foreach (var process in processes)
            {
                if (!string.IsNullOrWhiteSpace(process.MainWindowTitle))
                    sb.AppendLine($"{process.StartTime}: {process.ProcessName} - {process.MainWindowTitle}");
            }

            sb.AppendLine(new string('=', 25));
            return sb.ToString();
        }

        public string ManageBotState(string messageText)
        {
            var response = string.Empty;

            switch (BotState)
            {
                case BotState.StartProcess:
                    if (ProcessStarted(messageText))
                    {
                        response = $"{messageText} started";
                        Console.WriteLine($"Launching app: {messageText}");
                    }
                    else
                    {
                        response = $"No such app: {messageText}";
                    }
                    break;
                case BotState.KillProcess:
                    if (ProcessKilled(messageText))
                    {
                        response = $"{messageText} is terminated";
                        Console.WriteLine($"Process terminated: {messageText}");
                    }
                    else
                    {
                        response = $"No such process running: {messageText}";
                    }
                    break;
                case BotState.SearchWeb:
                    SearchWeb(messageText);
                    break;
                case BotState.SendMessage:
                    SendMessage(messageText);
                    break;
            }

            return response;
        }
    }
}
