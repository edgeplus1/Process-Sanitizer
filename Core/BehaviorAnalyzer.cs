using System;
using System.Collections.Generic;
using System.Diagnostics;
using Process_Sanitizer.Models;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Поведенческий анализ с памятью:
    /// мгновенный burn после старта, sustained-аномалия CPU, sticky-память,
    /// кросс-проверка "интерпретатор с параметрами из автозагрузки".
    /// </summary>
    public static class BehaviorAnalyzer
    {
        private class History
        {
            public int Hot60;     // подряд сканов с CPU >= 60
            public int Hot85;     // подряд сканов с CPU >= 85
            public bool Sticky;   // память: аномалия уже фиксировалась
        }

        private static readonly Dictionary<int, History> _hist = new Dictionary<int, History>();
        private static List<PersistenceEntry> _pers;
        private static DateTime _persAt = DateTime.MinValue;

        // Интерпретаторы/LOLBins, опасные в автозагрузке с параметрами
        private static readonly HashSet<string> Interpreters =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cmd", "powershell", "pwsh", "wscript", "cscript", "mshta",
            "rundll32", "regsvr32", "certutil", "bitsadmin", "curl", "wget", "ftp"
        };

        // Легитимные "тяжёлые" инструменты — не штрафуем за нагрузку
        private static readonly HashSet<string> HeavyAllowed =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "msbuild", "cl", "link", "dotnet", "devenv", "vbc", "csc", "vbcscompiler",
            "blender", "ffmpeg", "handbrake", "7z", "winrar", "rar", "unrar",
            "msiexec", "trustedinstaller", "tiworker", "searchindexer",
            "searchprotocolhost", "msmpeng", "mrt", "ngen", "ngentask"
        };

        /// <summary>
        /// Применить поведенческие правила ко всему списку процессов.
        /// </summary>
        public static void ApplyAll(List<ProcessInfo> list)
        {
            List<PersistenceEntry> pers = GetPersistence();

            HashSet<int> alive = new HashSet<int>();
            foreach (ProcessInfo pi in list)
            {
                alive.Add(pi.Pid);
                Apply(pi, pers);
            }

            // Чистим память о завершившихся процессах
            List<int> dead = new List<int>();
            foreach (int pid in _hist.Keys)
                if (!alive.Contains(pid)) dead.Add(pid);
            foreach (int pid in dead) _hist.Remove(pid);
        }

        // ---------- Внутреннее ----------

        private static void Apply(ProcessInfo pi, List<PersistenceEntry> pers)
        {
            if (pi.Pid == Process.GetCurrentProcess().Id) return;
            if (Whitelist.Contains(pi) || HashDatabase.IsWhitelisted(pi.Sha256)) return;

            ThreatRecord good;
            if (pi.Sha256.Length > 0 && ThreatDatabase.TryGetGood(pi.Sha256, out good)) return;

            string path = (pi.Path ?? string.Empty).ToLowerInvariant();
            string name = (pi.Name ?? string.Empty).ToLowerInvariant();
            string cmd = (pi.CommandLine ?? string.Empty).ToLowerInvariant();

            // Системные процессы в своих папках — не трогаем (система "своя")
            if (path.StartsWith(@"c:\windows\") && IsSystemSid(pi.UserSid)) return;
            // Легитимные тяжёлые инструменты — не трогаем
            if (HeavyAllowed.Contains(name)) return;

            History h;
            if (!_hist.TryGetValue(pi.Pid, out h))
            {
                h = new History();
                _hist[pi.Pid] = h;
            }

            int add = 0;
            List<string> reasons = new List<string>();

            // --- B1: мгновенная нагрузка сразу после запуска ---
            if (pi.StartTime != DateTime.MinValue)
            {
                double age = (DateTime.Now - pi.StartTime).TotalSeconds;
                if (age >= 0 && age < 60 && pi.CpuPercent >= 50)
                {
                    add += 70;
                    reasons.Add("+70  Мгновенная высокая нагрузка сразу после запуска (возраст " + (int)age + " с)");
                }
            }

            // --- B2: sustained-аномалия ---
            if (pi.CpuPercent >= 60) h.Hot60++; else h.Hot60 = 0;
            if (pi.CpuPercent >= 85) h.Hot85++; else h.Hot85 = 0;

            if (h.Hot85 >= 2)
            {
                add += 80;
                reasons.Add("+80  Аномальная нагрузка >=85% держится " + h.Hot85 + " сканов подряд");
                h.Sticky = true;
            }
            else if (h.Hot60 >= 3)
            {
                add += 60;
                reasons.Add("+60  Высокая нагрузка >=60% держится " + h.Hot60 + " сканов подряд");
                h.Sticky = true;
            }

            // --- B3: память поведения (уклонение не спасает) ---
            if (h.Sticky && h.Hot85 < 2 && h.Hot60 < 3)
            {
                add += 80;
                reasons.Add("+80  Ранее зафиксирована аномальная нагрузка (память поведения)");
            }

            // --- B4: интерпретатор с параметрами из автозагрузки ---
            string normCmd = Norm(cmd);
            if (Interpreters.Contains(name) && normCmd.Length > 0)
            {
                foreach (PersistenceEntry en in pers)
                {
                    string ne = Norm(en.Command);
                    if (ne.Length < 8) continue;
                    if (ne == normCmd || ne.Contains(normCmd) || normCmd.Contains(ne))
                    {
                        if (HasArgs(en.Command))
                        {
                            add += 100;
                            reasons.Add("+100  Вредоносный автозапуск: " + name +
                                        " с параметрами из автозагрузки (" + en.Location + ")");
                        }
                        break;
                    }
                }
            }

            // --- B5: скриптовые загрузчики / скрытое выполнение ---
            if (Interpreters.Contains(name))
            {
                if (cmd.Contains("-encodedcommand") || cmd.Contains(" -enc ") || cmd.Contains(" -ec ") ||
                    cmd.Contains("-w hidden") || cmd.Contains("-windowstyle hidden") ||
                    cmd.Contains("-nop") || cmd.Contains("downloadstring") ||
                    cmd.Contains("downloadfile") || cmd.Contains("invoke-expression") ||
                    cmd.Contains("iex(") || cmd.Contains("javascript:") ||
                    cmd.Contains("/i:http") || cmd.Contains("certutil -urlcache") ||
                    cmd.Contains("bitsadmin /transfer"))
                {
                    add += 70;
                    reasons.Add("+70  Скриптовый загрузчик / скрытое выполнение в аргументах");
                }
            }

            if (add > 0)
            {
                pi.RiskScore += add;
                pi.RiskReasons.AddRange(reasons);
                pi.Risk = RiskAnalyzer.LevelFor(pi.RiskScore);
            }
        }

        /// <summary>
        /// Кэш автозагрузок: пересканировать не чаще раза в 30 секунд.
        /// </summary>
        private static List<PersistenceEntry> GetPersistence()
        {
            if (_pers == null || (DateTime.Now - _persAt).TotalSeconds > 30)
            {
                try { _pers = PersistenceScanner.Scan(); }
                catch { _pers = new List<PersistenceEntry>(); }
                _persAt = DateTime.Now;
            }
            return _pers;
        }

        private static bool IsSystemSid(string sid)
        {
            return sid == "S-1-5-18" || sid == "S-1-5-19" || sid == "S-1-5-20";
        }

        private static string Norm(string s)
        {
            string t = (s ?? string.Empty).ToLowerInvariant().Replace("\"", "").Trim();
            while (t.Contains("  ")) t = t.Replace("  ", " ");
            return t;
        }

        private static bool HasArgs(string command)
        {
            string t = (command ?? string.Empty).Trim();
            if (t.StartsWith("\""))
            {
                int close = t.IndexOf('"', 1);
                if (close >= 0) return t.Substring(close + 1).Trim().Length > 0;
                return false;
            }
            int sp = t.IndexOf(' ');
            return sp >= 0 && t.Substring(sp + 1).Trim().Length > 0;
        }
    }
}