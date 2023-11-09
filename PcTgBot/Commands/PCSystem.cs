using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Text;

namespace PcTgBot.Commands
{
    internal class PCSystem
    {
        private bool IsProgramVisible(RegistryKey subkey)
        {
            var name = (string)subkey.GetValue("DisplayName");
            var releaseType = (string)subkey.GetValue("ReleaseType");
            var systemComponent = subkey.GetValue("SystemComponent");
            var parentName = (string)subkey.GetValue("ParentDisplayName");

            return !string.IsNullOrWhiteSpace(name) && 
                string.IsNullOrWhiteSpace(releaseType) && 
                string.IsNullOrWhiteSpace(parentName) && 
                (systemComponent == null);
        }

        private IEnumerable<string> GetInstalledProgramsFromRegistry(RegistryView registryView)
        {
            const string registry_key = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            var list = new List<string>();

            using (RegistryKey key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView).OpenSubKey(registry_key))
            {
                foreach (string subkey_name in key.GetSubKeyNames())
                {
                    using (RegistryKey subkey = key.OpenSubKey(subkey_name))
                    {
                        if (IsProgramVisible(subkey))
                            list.Add((string)subkey.GetValue("DisplayName"));
                    }
                }
            }

            return list;
        }

        private string ExistsInSubKey(RegistryKey root, string subKeyName, string attributeName, string nameOfAppToFind)
        {
            RegistryKey subkey;
            string displayName;

            using (RegistryKey key = root.OpenSubKey(subKeyName))
            {
                if (key != null)
                {
                    foreach (var keyName in key.GetSubKeyNames())
                    {
                        using (subkey = key.OpenSubKey(keyName))
                        {
                            displayName = subkey.GetValue(attributeName) as string;
                            
                            if (!string.IsNullOrWhiteSpace(displayName))
                            {
                                displayName = displayName.ToLower();
                                
                                if (displayName.Contains(nameOfAppToFind))
                                    return subkey.GetValue("InstallLocation") as string;
                            }
                        }
                    }
                }
            }

            return "Can't open it";
        }

        private string SizeSuffix(long value)
        {
            var sizeSuffixes = new[] { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            
            if (value < 0) 
                return "-" + SizeSuffix(-value); 
            
            if (value == 0) 
                return "0.0 bytes"; 

            var mag = (int)Math.Log(value, 1024);
            var adjustedSize = (decimal)value / (1L << (mag * 10));

            return string.Format("{0:n1} {1}", adjustedSize, sizeSuffixes[mag]);
        }

        public string GetApplictionInstallPath(string appToFind)
        {
            var keyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
            var installedPath = ExistsInSubKey(Registry.LocalMachine, keyName, "DisplayName", appToFind);

            if (!string.IsNullOrWhiteSpace(installedPath))
                return installedPath;

            return string.Empty;
        }

        public string GetInstalledAppsList()
        {
            var list = new List<string>();
            var sb = new StringBuilder();

            list.AddRange(GetInstalledProgramsFromRegistry(RegistryView.Registry32));
            list.AddRange(GetInstalledProgramsFromRegistry(RegistryView.Registry64));

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

        public void ShutDown()
        {
            ManagementBaseObject mboShutdown = null;
            
            // You can't shutdown without security privileges
            var mcWin32 = new ManagementClass("Win32_OperatingSystem");
            mcWin32.Get();
            mcWin32.Scope.Options.EnablePrivileges = true;
            
            // Flag 1 means we want to shut down the system. Use "2" to reboot.
            var mboShutdownParams = mcWin32.GetMethodParameters("Win32Shutdown");
            mboShutdownParams["Flags"] = "1";
            mboShutdownParams["Reserved"] = "0";

            foreach (var manObj in mcWin32.GetInstances())
                mboShutdown = (manObj as ManagementObject).InvokeMethod("Win32Shutdown", mboShutdownParams, null);
        }
    }
}
