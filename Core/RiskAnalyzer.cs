using System;
using System.Collections.Generic;
using Process_Sanitizer.Models;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Результат оценки риска одного процесса.
    /// </summary>
    public class RiskAssessment
    {
        public int Score;
        public string Level = "SAFE";
        public List<string> Reasons = new List<string>();
    }

    /// <summary>
    /// Эвристический анализатор риска (v4: встроенная БД угроз + эталоны).
    /// Порядок: БД BAD -> локальный чёрный -> БД GOOD/белые -> подмена -> каталог -> эвристики.
    /// Поверх результата работает BehaviorAnalyzer (ы 11-12, уровень DEAD).
    /// </summary>
    public static class RiskAnalyzer
    {
        // Где ДОЛЖНЫ лежать системные процессы.
        // Часть компонентов Windows живёт в SystemApps (UWP-оболочка),
        // "Параметры" — в ImmersiveControlPanel, 32-битные двойники — в SysWOW64.
        private static readonly Dictionary<string, string> SystemDirs =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "svchost",   @"\windows\system32\" },
            { "csrss",     @"\windows\system32\" },
            { "lsass",     @"\windows\system32\" },
            { "services",  @"\windows\system32\" },
            { "winlogon",  @"\windows\system32\" },
            { "wininit",   @"\windows\system32\" },
            { "smss",      @"\windows\system32\" },
            { "spoolsv",   @"\windows\system32\" },
            { "dllhost",   @"\windows\system32\" },
            { "taskhost",  @"\windows\system32\" },
            { "taskhostw", @"\windows\system32\" },
            { "conhost",   @"\windows\system32\" },
            { "sihost",    @"\windows\system32\" },
            { "dwm",       @"\windows\system32\" },
            { "fontdrvhost", @"\windows\system32\" },
            { "audiodg",   @"\windows\system32\" },
            { "ctfmon",    @"\windows\system32\" },
            { "runtimebroker", @"\windows\system32\" },
            { "securityhealthservice", @"\windows\system32\" },
            { "applicationframehost", @"\windows\system32\" },
            { "explorer",  @"\windows\" },
            { "systemsettings", @"\windows\immersivecontrolpanel\" },
            // UWP-компоненты оболочки — SystemApps
            { "shellexperiencehost", @"\windows\systemapps\" },
            { "startmenuexperiencehost", @"\windows\systemapps\" },
            { "searchapp", @"\windows\systemapps\" },
            { "searchui", @"\windows\systemapps\" },
            { "textinputhost", @"\windows\systemapps\" },
            { "lockapp", @"\windows\systemapps\" },
            { "sechealthui", @"\windows\systemapps\" },
            { "peopleexperiencehost", @"\windows\systemapps\" }
        };

        // Эти имена ДОЛЖНЫ работать от системных учётных записей (кроме per-user svchost)
        private static readonly HashSet<string> SystemOnlyAccounts =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "csrss", "lsass", "services", "winlogon", "wininit", "smss", "spoolsv"
        };

        // SID системных учётных записей — не зависят от языка Windows
        private static readonly HashSet<string> SystemSids =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "S-1-5-18", // LOCAL SYSTEM
            "S-1-5-19", // LOCAL SERVICE
            "S-1-5-20"  // NETWORK SERVICE
        };

        // Имена, которым разрешён также SysWOW64 (32-битные двойники на x64 Windows)
        private static readonly HashSet<string> Wow64Allowed =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "dllhost", "conhost", "taskhost", "taskhostw"
        };

        // Эти файлы Windows подписаны ВСТРОЕННОЙ подписью; отсутствие VALID = признак подмены
        private static readonly HashSet<string> EmbeddedSignedExpected =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "explorer", "userinit", "taskmgr", "regedit", "notepad", "cmd",
            "mmc", "control", "msconfig", "eventvwr"
        };

        // Эти файлы Windows подписаны КАТАЛОГОМ; без встроенной подписи в своей папке — норма
        private static readonly HashSet<string> CatalogSignedNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "svchost", "csrss", "lsass", "services", "winlogon", "wininit", "smss",
            "spoolsv", "dllhost", "taskhost", "taskhostw", "conhost", "sihost",
            "dwm", "fontdrvhost", "audiodg", "ctfmon"
        };

        // Признаки майнинга в командной строке
        private static readonly string[] MinerKeywords =
        {
            "stratum", "xmrig", "minerd", "nicehash", "ethash",
            "cryptonight", "coinhive", "--donate-level", "miningpool", "pool.minexmr"
        };

        /// <summary>
        /// Оценить процесс и записать результат прямо в модель.
        /// </summary>
        public static RiskAssessment Assess(ProcessInfo pi)
        {
            RiskAssessment a = new RiskAssessment();

            // --- Хеш файла ---
            pi.Sha256 = FileHashCache.Get(pi.Path);

            // --- 1. Встроенная БД: известный вредонос ---
            ThreatRecord bad;
            if (pi.Sha256.Length > 0 && ThreatDatabase.TryGetBad(pi.Sha256, out bad))
            {
                a.Score = 100;
                a.Level = "CRITICAL";
                a.Reasons.Add("+100  БД угроз: " + bad.Label +
                              (bad.Comment.Length > 0 ? " (" + bad.Comment + ")" : ""));
                pi.Risk = a.Level;
                pi.RiskScore = a.Score;
                pi.RiskReasons = a.Reasons;
                pi.Signature = SignatureChecker.Verify(pi.Path);
                pi.Signer = pi.Signature == "VALID" ? SignatureChecker.GetSigner(pi.Path) : "";
                return a;
            }

            // --- 2. Локальный чёрный список хешей ---
            if (pi.Sha256.Length > 0 && HashDatabase.IsBlacklisted(pi.Sha256))
            {
                a.Score = 100;
                a.Level = "CRITICAL";
                a.Reasons.Add("+100  Хеш в локальном чёрном списке");
                pi.Risk = a.Level;
                pi.RiskScore = a.Score;
                pi.RiskReasons = a.Reasons;
                pi.Signature = SignatureChecker.Verify(pi.Path);
                pi.Signer = pi.Signature == "VALID" ? SignatureChecker.GetSigner(pi.Path) : "";
                return a;
            }

            // --- 3. Эталон БД / белые списки ---
            ThreatRecord good = null;
            if (pi.Sha256.Length > 0) ThreatDatabase.TryGetGood(pi.Sha256, out good);
            bool goodAnchor = good != null;
            if (goodAnchor || Whitelist.Contains(pi) ||
                (pi.Sha256.Length > 0 && HashDatabase.IsWhitelisted(pi.Sha256)))
            {
                a.Reasons.Add(goodAnchor
                    ? "Хеш совпадает с эталоном БД: " + good.Label + " — доверен"
                    : "Объект в белом списке (путь или хеш) — риск обнулён");
                pi.Risk = "SAFE";
                pi.RiskScore = 0;
                pi.RiskReasons = a.Reasons;
                pi.Signature = SignatureChecker.Verify(pi.Path);
                pi.Signer = pi.Signature == "VALID" ? SignatureChecker.GetSigner(pi.Path) : "";
                return a;
            }

            string path = (pi.Path ?? string.Empty).ToLowerInvariant();
            string name = (pi.Name ?? string.Empty).ToLowerInvariant();
            string user = pi.User ?? string.Empty;
            string cmd = (pi.CommandLine ?? string.Empty).ToLowerInvariant();

            bool inSystemApps = path.StartsWith(@"c:\windows\systemapps\");
            bool inTrustedDir = path.StartsWith(@"c:\windows\")
                             || path.Contains(@"\program files\")
                             || path.Contains(@"\program files (x86)\");

            // --- Подпись: наличие + валидность ---
            string sig = SignatureChecker.Verify(pi.Path);
            pi.Signature = sig;
            bool signed = sig == "VALID";
            pi.Signer = signed ? SignatureChecker.GetSigner(pi.Path) : "";

            string expectedDir;
            bool knownSystemName = SystemDirs.TryGetValue(name, out expectedDir);

            // --- 4. Подмена системного файла (поддельный explorer и т.п.) ---
            if (knownSystemName && path.Length > 0 && InExpectedDir(path, expectedDir, name) &&
                EmbeddedSignedExpected.Contains(name) && sig != "VALID")
            {
                Add(a, 45, "Системный файл без действующей встроенной подписи — возможна подмена: " + pi.Name);
            }

            // --- 5. Каталожное доверие Windows ---
            if (knownSystemName && InExpectedDir(path, expectedDir, name) &&
                (sig == "UNSIGNED" || sig == "UNKNOWN") &&
                ((CatalogSignedNames.Contains(name) && IsSystemAccount(pi)) || inSystemApps))
            {
                a.Reasons.Add("Системный файл Windows в своей папке (каталожная подпись) — доверен");
                pi.Risk = "SAFE";
                pi.RiskScore = 0;
                pi.RiskReasons = a.Reasons;
                return a;
            }

            // --- 6. Маскировка под системный процесс ---
            if (knownSystemName)
            {
                if (path.Length > 0 && !InExpectedDir(path, expectedDir, name))
                    Add(a, 40, "Имя системного процесса вне своей папки: " + pi.Path);

                if (SystemOnlyAccounts.Contains(name) && user.Length > 0 && !IsSystemAccount(pi))
                {
                    // svchost от пользователя — норма для per-user служб (ключ -k),
                    // но только если он лежит в своей папке
                    bool perUserService = (name == "svchost") && cmd.Contains("-k") && InExpectedDir(path, expectedDir, name);
                    if (!perUserService)
                        Add(a, 30, "Системный процесс запущен от обычного пользователя: " + user);
                }
            }
            else
            {
                // Похожее имя с опечаткой: svch0st, scvhost, explorers...
                foreach (KeyValuePair<string, string> kv in SystemDirs)
                {
                    if (name != kv.Key &&
                        Math.Abs(name.Length - kv.Key.Length) <= 1 &&
                        Distance(name, kv.Key) <= 2)
                    {
                        Add(a, 35, "Имя мимикрирует под системный процесс: " + pi.Name);
                        break;
                    }
                }
            }

            // --- 7. Подозрительные папки ---
            if (path.Contains(@"\temp\")) Add(a, 25, "Запуск из временной папки (Temp)");
            if (path.Contains(@"\appdata\")) Add(a, 15, "Запуск из AppData");
            if (path.Contains(@"\downloads\")) Add(a, 20, "Запуск из папки загрузок");
            if (path.Contains(@"\programdata\") && !path.Contains(@"\windows defender\platform\"))
                Add(a, 10, "Запуск из ProgramData");
            if (path.Contains(@"\$recycle.bin\") || path.Contains(@"\recycler\"))
                Add(a, 35, "Запуск из корзины");

            // --- 8. Подпись (SystemApps подписан каталогом пакета — не штрафуем) ---
            if (path.Length > 0 && sig == "UNSIGNED" && !inSystemApps)
                Add(a, 10, "Файл не имеет цифровой подписи");
            if (path.Length > 0 && sig.StartsWith("INVALID"))
                Add(a, 25, "Подпись недействительна: " + sig);

            // --- 9. SYSTEM + без валидной подписи вне доверенных папок ---
            if (IsSystemAccount(pi) && path.Length > 0 && !signed && !inTrustedDir)
                Add(a, 25, "Работает от SYSTEM без валидной подписи вне доверенных папок");

            // --- 10. Майнинг в командной строке ---
            foreach (string kw in MinerKeywords)
            {
                if (cmd.Contains(kw))
                {
                    Add(a, 60, "Командная строка содержит признак майнинга: " + kw);
                    break;
                }
            }

            // --- 11. Высокая нагрузка CPU (мгновенный снимок; sustained/DEAD — в BehaviorAnalyzer) ---
            if (pi.CpuPercent >= 60) Add(a, 30, "Очень высокая загрузка CPU: " + pi.CpuPercent.ToString("0") + "%");
            else if (pi.CpuPercent >= 25) Add(a, 15, "Высокая загрузка CPU: " + pi.CpuPercent.ToString("0") + "%");

            // --- Уровень ---
            a.Level = LevelFor(a.Score);
            pi.Risk = a.Level;
            pi.RiskScore = a.Score;
            pi.RiskReasons = a.Reasons;
            return a;
        }

        // ---------- Вспомогательное ----------

        /// <summary>
        /// Уровень риска по сумме баллов (используется и поведенческим анализом).
        /// DEAD — экстремальная аномальная нагрузка ( 12).
        /// </summary>
        public static string LevelFor(int score)
        {
            if (score >= 150) return "DEAD";
            if (score >= 80) return "CRITICAL";
            if (score >= 50) return "HIGH";
            if (score >= 25) return "MEDIUM";
            if (score > 0) return "LOW";
            return "SAFE";
        }

        /// <summary>
        /// Лежит ли файл в своей папке (System32 или разрешённый SysWOW64).
        /// </summary>
        private static bool InExpectedDir(string path, string expectedDir, string name)
        {
            if (path.Contains(expectedDir)) return true;
            return expectedDir == @"\windows\system32\"
                && path.Contains(@"\windows\syswow64\")
                && Wow64Allowed.Contains(name);
        }

        private static void Add(RiskAssessment a, int points, string reason)
        {
            a.Score += points;
            a.Reasons.Add("+" + points + "  " + reason);
        }

        /// <summary>
        /// Системная учётная запись: сначала по SID (надёжно, любой язык),
        /// имя — только как запасной вариант.
        /// </summary>
        private static bool IsSystemAccount(ProcessInfo pi)
        {
            if (!string.IsNullOrEmpty(pi.UserSid) && SystemSids.Contains(pi.UserSid)) return true;
            if (string.IsNullOrEmpty(pi.User)) return false;
            string u = pi.User.ToUpperInvariant();
            return u.EndsWith(@"\SYSTEM")
                || u.EndsWith(@"\LOCAL SERVICE")
                || u.EndsWith(@"\NETWORK SERVICE")
                || u.EndsWith(@"\СИСТЕМА")
                || u.EndsWith(@"\СЕТЕВАЯ СЛУЖБА")
                || u.EndsWith(@"\ЛОКАЛЬНАЯ СЛУЖБА");
        }

        /// <summary>
        /// Расстояние Левенштейна: насколько имя похоже на системное.
        /// </summary>
        private static int Distance(string a, string b)
        {
            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                }
            }
            return d[a.Length, b.Length];
        }
    }
}