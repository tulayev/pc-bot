using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;

namespace PcTgBot.Commands
{
    internal class PCSystem
    {
        private string SizeSuffix(long value)
        {
            var sizeSuffixes = new[] { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            if (value < 0)
                return $"-{SizeSuffix(-value)}";

            if (value == 0)
                return "0.0 bytes";

            var mag = (int)Math.Log(value, 1024);
            var adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, sizeSuffixes[mag]);
        }

        private bool IsAppVisible(RegistryKey subkey)
        {
            var name = (string)subkey.GetValue("DisplayName");
            var releaseType = (string)subkey.GetValue("ReleaseType");
            var parentName = (string)subkey.GetValue("ParentDisplayName");
            var systemComponent = subkey.GetValue("SystemComponent");

            return !string.IsNullOrWhiteSpace(name) && 
                string.IsNullOrWhiteSpace(releaseType) && 
                string.IsNullOrWhiteSpace(parentName) && 
                (systemComponent == null);
        }

        private IEnumerable<string> GetInstalledAppsList()
        {
            var registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(registryKey))
            {
                foreach (var keyName in key.GetSubKeyNames())
                {
                    using (var subkey = key.OpenSubKey(keyName))
                    {
                        if (IsAppVisible(subkey))
                            yield return (string)subkey.GetValue("DisplayName");
                    }
                }
            }
            
            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(registryKey))
            {
                foreach (var keyName in key.GetSubKeyNames())
                {
                    using (var subkey = key.OpenSubKey(keyName))
                    {
                        if (IsAppVisible(subkey))
                            yield return (string)subkey.GetValue("DisplayName");
                    }
                }
            }
        }

        public string GetAppInstallationPath(string appName)
        {
            var registryKey = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32).OpenSubKey(registryKey))
            {
                foreach (var keyName in key.GetSubKeyNames())
                {
                    using (var subkey = key.OpenSubKey(keyName))
                    {
                        var appDisplayName = (string)subkey.GetValue("DisplayName");
                        if (IsAppVisible(subkey) && appDisplayName.ToLower().Contains(appName.ToLower()))
                            return (string)subkey.GetValue("InstallLocation");
                    }
                }
            }

            using (var key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64).OpenSubKey(registryKey))
            {
                foreach (var keyName in key.GetSubKeyNames())
                {
                    using (var subkey = key.OpenSubKey(keyName))
                    {
                        var appDisplayName = (string)subkey.GetValue("DisplayName");
                        if (IsAppVisible(subkey) && appDisplayName.ToLower().Contains(appName.ToLower()))
                            return (string)subkey.GetValue("InstallLocation");
                    }
                }
            }

            return string.Empty;
        }

        public string GetInstalledAppsListText()
        {
            var list = GetInstalledAppsList().ToList();
            var sb = new StringBuilder();

            foreach (var item in list)
                sb.AppendLine(item);

            return sb.ToString();
        }

        public string GetSystemInfo()
        {
            var sb = new StringBuilder();
            //OS Info
            var myOperativeSystemObject = new ManagementObjectSearcher("select * from Win32_OperatingSystem");
            
            sb.AppendLine("=====OS Info=====");
            
            foreach (var obj in myOperativeSystemObject.Get())
            {
                sb.AppendLine($"Caption: {obj["Caption"]}");
                sb.AppendLine($"Version: {obj["Version"]}");
            }

            //CPU Info
            var myProcessorObject = new ManagementObjectSearcher("select * from Win32_Processor");
            
            sb.AppendLine("=====CPU Info=====");
            
            foreach (var obj in myProcessorObject.Get())
            {
                sb.AppendLine($"Name: {obj["Name"]}");
                sb.AppendLine($"Number of cores: {obj["NumberOfCores"]}");
                sb.AppendLine($"Caption: {obj["Caption"]}");
            }

            //RAM Info
            var myRamObject = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
            
            sb.AppendLine("====RAM Info=====");
            
            foreach (var obj in myRamObject.Get())
            {
                sb.AppendLine($"Total visible memory: {SizeSuffix((long)Convert.ToDouble(obj["TotalVisibleMemorySize"]) * 1024)}");
            }

            //Drives Info
            var allDrives = DriveInfo.GetDrives();
            
            sb.AppendLine("====Drive Info=====");
            
            foreach (var drive in allDrives)
            {
                sb.AppendLine($"Drive {drive.Name}");
                if (drive.IsReady)
                {
                    sb.AppendLine($"Total available space: {SizeSuffix(drive.TotalFreeSpace)}");
                    sb.AppendLine($"Total size of drive: {SizeSuffix(drive.TotalSize)}");
                }
            }

            //GPU Info
            var myVideoObject = new ManagementObjectSearcher("select * from Win32_VideoController");
            
            sb.AppendLine("====GPU Info=====");
            
            foreach (var obj in myVideoObject.Get())
            {
                sb.AppendLine($"Name: {obj["Name"]}");
                sb.AppendLine($"Caption: {obj["Caption"]}");
                sb.AppendLine($"AdapterRAM: {SizeSuffix((long)Convert.ToDouble(obj["AdapterRAM"]))}");
            }

            return sb.ToString();
        }

        public bool IsShutdown(string messageText)
        {
            if (messageText.ToLower() != "yes" || messageText.ToLower() != "y")
                return false;

            // You can't shutdown without security privileges
            var mcWin32 = new ManagementClass("Win32_OperatingSystem");
            mcWin32.Get();
            mcWin32.Scope.Options.EnablePrivileges = true;
            
            // Flag 1 means we want to shut down the system. Use "2" to reboot.
            var mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");
            mboShutdownParams["Flags"] = "1";
            mboShutdownParams["Reserved"] = "0";

            ManagementBaseObject mboShutdown;
            foreach (var manObj in mcWin32.GetInstances())
                mboShutdown = (manObj as ManagementObject).InvokeMethod("Win32Shutdown", mboShutdownParams, null);

            return true;
        }
    }
}
