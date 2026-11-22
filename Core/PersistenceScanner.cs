using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Одна запись автозагрузки/закрепления.
    /// </summary>
    public class PersistenceEntry
    {
        public string Location;
        public string Name;
        public string Command;
        public bool Suspicious;
        public string Reason;
    }

    /// <summary>
    /// Сканер закреплений: реестр Run/RunOnce, папки автозагрузки, службы, задачи планировщика.
    /// </summary>
    public static class PersistenceScanner
    {
        public static List<PersistenceEntry> Scan()
        {
            List<PersistenceEntry> list = new List<PersistenceEntry>();
            ScanRegistry(list, Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU\\Run");
            ScanRegistry(list, Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKCU\\RunOnce");
            ScanRegistry(list, Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKLM\\Run");
            ScanRegistry(list, Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", "HKLM\\RunOnce");
            ScanRegistry(list, Registry.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", "HKLM\\Run (x86)");
            ScanStartupFolders(list);
            ScanServices(list);
            ScanTasks(list);
            return list;
        }

        // ---------- Реестр ----------

        private static void ScanRegistry(List<PersistenceEntry> list, RegistryKey root, string subKey, string label)
        {
            try
            {
                using (RegistryKey key = root.OpenSubKey(subKey))
                {
                    if (key == null) return;
                    foreach (string name in key.GetValueNames())
                    {
                        string cmd = Convert.ToString(key.GetValue(name));
                        AddEntry(list, label, name, cmd);
                    }
                }
            }
            catch { }
        }

        // ---------- Папки автозагрузки ----------

        private static void ScanStartupFolders(List<PersistenceEntry> list)
        {
            string[] folders =
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup)
            };
            foreach (string folder in folders)
            {
                try
                {
                    if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) continue;
                    foreach (string file in Directory.GetFiles(folder))
                    {
                        string name = Path.GetFileName(file);
                        if (name.Equals("desktop.ini", StringComparison.OrdinalIgnoreCase)) continue;
                        AddEntry(list, "Папка автозагрузки", name, file);
                    }
                }
                catch { }
            }
        }

        // ---------- Службы (WMI) ----------

        private static void ScanServices(List<PersistenceEntry> list)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                    "SELECT Name, PathName, StartName, State FROM Win32_Service"))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject mo in results)
                    {
                        using (mo)
                        {
                            string name = mo["Name"] as string ?? "";
                            string path = mo["PathName"] as string ?? "";
                            if (path.Length == 0) continue;
                            AddEntry(list, "Служба (" + (mo["StartName"] as string ?? "") + ")", name, path);
                        }
                    }
                }
            }
            catch { }
        }

        // ---------- Планировщик задач (COM, без доп. ссылок) ----------

        private static void ScanTasks(List<PersistenceEntry> list)
        {
            object service = null;
            try
            {
                Type t = Type.GetTypeFromProgID("Schedule.Service");
                if (t == null) return;
                service = Activator.CreateInstance(t);
                t.InvokeMember("Connect", BindingFlags.InvokeMethod, null, service, null);
                object root = t.InvokeMember("GetFolder", BindingFlags.InvokeMethod, null, service, new object[] { "\\" });
                WalkFolder(root, list);
            }
            catch { }
            finally
            {
                if (service != null) Marshal.ReleaseComObject(service);
            }
        }

        private static void WalkFolder(object folder, List<PersistenceEntry> list)
        {
            try
            {
                Type ft = folder.GetType();
                string folderPath = (string)ft.InvokeMember("Path", BindingFlags.GetProperty, null, folder, null);
                // системные задачи Windows не показываем — их сотни и они доверенные
                if (folderPath.StartsWith(@"\Microsoft\Windows\", StringComparison.OrdinalIgnoreCase)) return;

                object tasks = ft.InvokeMember("GetTasks", BindingFlags.InvokeMethod, null, folder, new object[] { 1 });
                foreach (object task in (System.Collections.IEnumerable)tasks)
                {
                    try
                    {
                        Type tt = task.GetType();
                        string name = (string)tt.InvokeMember("Name", BindingFlags.GetProperty, null, task, null);
                        object def = tt.InvokeMember("Definition", BindingFlags.GetProperty, null, task, null);
                        object actions = def.GetType().InvokeMember("Actions", BindingFlags.GetProperty, null, def, null);
                        int count = (int)actions.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, actions, null);
                        for (int i = 1; i <= count; i++)
                        {
                            object action = actions.GetType().InvokeMember("Item", BindingFlags.GetProperty, null, actions, new object[] { i });
                            int type = (int)action.GetType().InvokeMember("Type", BindingFlags.GetProperty, null, action, null);
                            if (type != 0) continue; // только Exec-действия
                            string exe = (string)action.GetType().InvokeMember("Path", BindingFlags.GetProperty, null, action, null);
                            string args = (string)action.GetType().InvokeMember("Arguments", BindingFlags.GetProperty, null, action, null);
                            AddEntry(list, "Задача: " + folderPath, name, (exe + " " + args).Trim());
                        }
                    }
                    catch { }
                }

                object subFolders = ft.InvokeMember("GetFolders", BindingFlags.InvokeMethod, null, folder, new object[] { 1 });
                foreach (object sub in (System.Collections.IEnumerable)subFolders)
                    WalkFolder(sub, list);
            }
            catch { }
        }

        // ---------- Общее ----------

        private static void AddEntry(List<PersistenceEntry> list, string location, string name, string command)
        {
            PersistenceEntry e = new PersistenceEntry();
            e.Location = location;
            e.Name = name;
            e.Command = command ?? "";
            e.Reason = AnalyzeCommand(e.Command);
            e.Suspicious = e.Reason != null;
            if (e.Reason == null) e.Reason = "";
            list.Add(e);
        }

        private static string AnalyzeCommand(string cmd)
        {
            string c = (cmd ?? "").ToLowerInvariant();
            if (c.Contains(@"\temp\")) return "запуск из Temp";
            if (c.Contains(@"\downloads\")) return "запуск из Downloads";
            if (c.Contains(@"\programdata\")) return "запуск из ProgramData";
            if (c.Contains(@"\appdata\")) return "запуск из AppData (проверь вручную)";
            if (c.Contains("-encodedcommand") || c.Contains(" -enc ") || c.Contains(" -ec ")) return "PowerShell с закодированной командой";
            if (c.Contains("mshta http") || c.Contains("regsvr32 /i:http") || c.Contains("rundll32 javascript")) return "скриптовый запуск из сети";
            return null;
        }
    }
}