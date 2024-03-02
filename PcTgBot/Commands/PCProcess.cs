using System.Diagnostics;
using System.Text;
using System.IO;
using System.Windows.Forms;
using System;
using System.Linq;
using NLog;

namespace PcTgBot.Commands
{
    internal class PCProcess
    {
        private readonly PCSystem _system;
        public BotState BotState { get; set; } = BotState.Wait;

        public PCProcess(PCSystem system)
        {
            _system = system;
        }

        private bool ProcessStarted(string appName)
        {
            var exeFolder = _system.GetAppInstallationPath(appName);

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

                    foreach (var file in files)
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

        private void SearchWeb(string query)
        {
            Process.Start($"microsoft-edge:https://www.google.com/search?q={query}");
        }

        private void SendMessage(string text)
        {
            var messageBoxTitle = "Message by Bot";
            MessageBox.Show(text, messageBoxTitle, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);
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
                    BotState = BotState.Wait;
                    if (ProcessStarted(messageText))
                    {
                        response = $"{messageText} started";
                        LogManager.GetCurrentClassLogger().Info($"Launching app: {messageText}");
                    }
                    else
                    {
                        response = $"No such app: {messageText}";
                    }
                    break;
                case BotState.KillProcess:
                    BotState = BotState.Wait;
                    if (ProcessKilled(messageText))
                    {
                        response = $"{messageText} is terminated";
                        LogManager.GetCurrentClassLogger().Info($"Process terminated: {messageText}");
                    }
                    else
                    {
                        response = $"No such process running: {messageText}";
                    }
                    break;
                case BotState.SearchWeb:
                    BotState = BotState.Wait;
                    SearchWeb(messageText);
                    response = "Opened MS Edge for Google search";
                    break;
                case BotState.SendMessage:
                    BotState = BotState.Wait;
                    SendMessage(messageText);
                    response = "Message sent!";
                    break;
                case BotState.Shutdown:
                    BotState = BotState.Wait;
                    var isShutdown = _system.IsShutdown(messageText);
                    response = isShutdown ? "PC has been switched off!" : "You canceled the shutdown operation.";
                    break;
            }

            return response;
        }
    }
}
