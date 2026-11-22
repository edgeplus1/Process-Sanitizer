using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Process_Sanitizer.Models;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Сторож возврата: проверяет, не появились ли снова файлы из карантина
    /// (по исходному пути или по хешу в подозрительных папках).
    /// </summary>
    public static class QuarantineGuard
    {
        public static List<string> Check(List<ProcessInfo> procs)
        {
            List<string> alerts = new List<string>();
            List<QuarantineItem> items = Quarantine.List();
            if (items.Count == 0) return alerts;

            Dictionary<string, QuarantineItem> byPath =
                new Dictionary<string, QuarantineItem>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (QuarantineItem it in items)
            {
                byPath[it.OriginalPath] = it;
                hashes.Add(it.Sha256);
            }

            foreach (ProcessInfo pi in procs)
            {
                string path = pi.Path;
                if (string.IsNullOrEmpty(path)) continue;

                QuarantineItem item;
                if (byPath.TryGetValue(path, out item))
                {
                    string h = Hash(path);
                    if (h == item.Sha256)
                    {
                        Mark(pi, "Файл из карантина вернулся по исходному пути (хеш совпал)");
                        alerts.Add(pi.Name + " (PID " + pi.Pid + "): " + path);
                    }
                    else
                    {
                        alerts.Add("По пути карантина снова файл, но хеш другой: " + path);
                    }
                    continue;
                }

                string low = path.ToLowerInvariant();
                if (low.Contains(@"\temp\") || low.Contains(@"\appdata\") ||
                    low.Contains(@"\downloads\") || low.Contains(@"\programdata\"))
                {
                    string h = Hash(path);
                    if (h != null && hashes.Contains(h))
                    {
                        Mark(pi, "Копия карантинного файла под другим именем/путём (хеш совпал)");
                        alerts.Add(pi.Name + " (PID " + pi.Pid + "): " + path);
                    }
                }
            }
            return alerts;
        }

        private static void Mark(ProcessInfo pi, string reason)
        {
            pi.Risk = "CRITICAL";
            if (pi.RiskScore < 100) pi.RiskScore = 100;
            pi.RiskReasons.Add("+100  " + reason);
        }

        private static string Hash(string path)
        {
            try
            {
                using (SHA256 sha = SHA256.Create())
                using (FileStream fs = File.OpenRead(path))
                {
                    byte[] hash = sha.ComputeHash(fs);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch { return null; }
        }
    }
}