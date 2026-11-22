using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.Win32;
using Process_Sanitizer.Models;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Действия над процессами: завершение, открытие папки, защита критических,
    /// принудительное безвозвратное уничтожение (force).
    /// </summary>
    public static class ProcessActions
    {
        private static readonly HashSet<string> CriticalNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "system", "idle", "registry", "smss", "csrss", "wininit", "winlogon",
            "services", "lsass", "svchost", "explorer", "dwm", "fontdrvhost",
            "audiodg", "spoolsv"
        };

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string lpExistingFileName, string lpNewFileName, uint dwFlags);

        private const uint MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        public static bool IsProtected(ProcessInfo pi)
        {
            if (pi.Pid == Process.GetCurrentProcess().Id) return true;
            if (!CriticalNames.Contains(pi.Name)) return false;
            string path = (pi.Path ?? string.Empty).ToLowerInvariant();
            return path.Length == 0 || path.StartsWith(@"c:\windows\");
        }

        public static bool Kill(int pid, out string error)
        {
            error = null;
            try
            {
                using (Process p = Process.GetProcessById(pid))
                {
                    p.Kill();
                    if (!p.WaitForExit(3000))
                    {
                        error = "процесс не завершился за 3 секунды";
                        return false;
                    }
                }
                return true;
            }
            catch (ArgumentException)
            {
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public static void OpenFolder(string path)
        {
            Process.Start("explorer.exe", "/select,\"" + path + "\"");
        }

        /// <summary>
        /// FORCE: принудительно завершить дерево процесса, удалить файл НАВСЕГДА
        /// (без карантина), убрать Run-записи автозагрузки с этим путём.
        /// Если файл занят — удаление планируется при перезагрузке.
        /// </summary>
        public static bool ForceDestroy(ProcessInfo pi, out string report, out bool rebootScheduled)
        {
            StringBuilder sb = new StringBuilder();
            rebootScheduled = false;

            // 1. Принудительное завершение дерева
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("taskkill", "/F /T /PID " + pi.Pid);
                psi.CreateNoWindow = true;
                psi.UseShellExecute = false;
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit(5000);
                }
                sb.AppendLine("taskkill /F /T /PID " + pi.Pid + " выполнен");
            }
            catch (Exception ex)
            {
                sb.AppendLine("taskkill: " + ex.Message);
            }

            try
            {
                using (Process p = Process.GetProcessById(pi.Pid))
                {
                    p.WaitForExit(5000);
                }
            }
            catch { }

            // 2. Безвозвратное удаление файла
            string path = pi.Path;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                bool deleted = false;
                for (int i = 0; i < 5 && !deleted; i++)
                {
                    try
                    {
                        File.Delete(path);
                        deleted = true;
                    }
                    catch
                    {
                        Thread.Sleep(300);
                    }
                }

                if (deleted)
                {
                    sb.AppendLine("Файл удалён навсегда: " + path);
                }
                else if (MoveFileEx(path, null, MOVEFILE_DELAY_UNTIL_REBOOT))
                {
                    rebootScheduled = true;
                    sb.AppendLine("Файл занят — удаление запланировано при перезагрузке: " + path);
                }
                else
                {
                    sb.AppendLine("Не удалось удалить файл: " + path);
                }
            }
            else
            {
                sb.AppendLine("Файл не найден (уже удалён или путь недоступен)");
            }

            // 3. Чистка Run-записей, ссылающихся на этот путь
            int removed = RemoveRunEntries(path);
            sb.AppendLine("Записей автозагрузки Run удалено: " + removed);

            report = sb.ToString();
            return true;
        }

        private static int RemoveRunEntries(string path)
        {
            if (string.IsNullOrEmpty(path)) return 0;
            string needle = path.ToLowerInvariant();
            int removed = 0;

            removed += CleanRun(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", needle);
            removed += CleanRun(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", needle);
            removed += CleanRun(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", needle);
            removed += CleanRun(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\RunOnce", needle);
            removed += CleanRun(Registry.LocalMachine, @"Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Run", needle);
            return removed;
        }

        private static int CleanRun(RegistryKey root, string subKey, string needle)
        {
            int removed = 0;
            try
            {
                using (RegistryKey key = root.OpenSubKey(subKey, true))
                {
                    if (key == null) return 0;
                    List<string> toDelete = new List<string>();
                    foreach (string name in key.GetValueNames())
                    {
                        string cmd = Convert.ToString(key.GetValue(name)).ToLowerInvariant();
                        if (cmd.Contains(needle)) toDelete.Add(name);
                    }
                    foreach (string name in toDelete)
                    {
                        try { key.DeleteValue(name); removed++; }
                        catch { }
                    }
                }
            }
            catch { }
            return removed;
        }
    }
}