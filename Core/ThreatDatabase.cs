using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Запись БД: BAD (известный вредонос) или GOOD (эталонный файл).
    /// </summary>
    public class ThreatRecord
    {
        public string Kind;      // BAD / GOOD
        public string Hash;
        public string Label;
        public string Comment;
        public bool Local;       // true = из локального оверлея, false = встроено в exe
    }

    /// <summary>
    /// Встроенная БД угроз (Embedded Resource) + локальный оверлей threatdb_local.txt.
    /// </summary>
    public static class ThreatDatabase
    {
        private static readonly Dictionary<string, ThreatRecord> _bad =
            new Dictionary<string, ThreatRecord>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, ThreatRecord> _good =
            new Dictionary<string, ThreatRecord>(StringComparer.OrdinalIgnoreCase);

        public static string LocalPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "threatdb_local.txt"); }
        }

        public static int BadCount { get { return _bad.Count; } }
        public static int GoodCount { get { return _good.Count; } }

        public static IEnumerable<ThreatRecord> Records
        {
            get
            {
                foreach (KeyValuePair<string, ThreatRecord> kv in _bad) yield return kv.Value;
                foreach (KeyValuePair<string, ThreatRecord> kv in _good) yield return kv.Value;
            }
        }

        public static void Load()
        {
            _bad.Clear();
            _good.Clear();

            // 1. Встроенная БД из ресурсов exe
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                using (Stream s = asm.GetManifestResourceStream("Process_Sanitizer.threatdb.txt"))
                {
                    if (s != null)
                        using (StreamReader r = new StreamReader(s))
                            ReadAll(r, false);
                }
            }
            catch { }

            // 2. Запасной вариант: файл threatdb.txt рядом с exe
            try
            {
                string baked = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "threatdb.txt");
                if (File.Exists(baked))
                    using (StreamReader r = File.OpenText(baked))
                        ReadAll(r, false);
            }
            catch { }

            // 3. Локальный оверлей (пополняется без пересборки)
            try
            {
                if (File.Exists(LocalPath))
                    using (StreamReader r = File.OpenText(LocalPath))
                        ReadAll(r, true);
            }
            catch { }
        }

        public static bool TryGetBad(string hash, out ThreatRecord rec)
        {
            rec = null;
            return !string.IsNullOrEmpty(hash) && _bad.TryGetValue(hash, out rec);
        }

        public static bool TryGetGood(string hash, out ThreatRecord rec)
        {
            rec = null;
            return !string.IsNullOrEmpty(hash) && _good.TryGetValue(hash, out rec);
        }

        public static void AddLocalBad(string hash, string label, string comment)
        {
            _bad[hash.ToLowerInvariant()] = new ThreatRecord
            { Kind = "BAD", Hash = hash.ToLowerInvariant(), Label = label, Comment = comment, Local = true };
            _good.Remove(hash.ToLowerInvariant());
            SaveLocal();
        }

        public static void AddLocalGood(string hash, string label, string comment)
        {
            _good[hash.ToLowerInvariant()] = new ThreatRecord
            { Kind = "GOOD", Hash = hash.ToLowerInvariant(), Label = label, Comment = comment, Local = true };
            _bad.Remove(hash.ToLowerInvariant());
            SaveLocal();
        }

        public static void RemoveLocal(string hash)
        {
            string h = hash.ToLowerInvariant();
            ThreatRecord rec;
            if (_bad.TryGetValue(h, out rec) && rec.Local) _bad.Remove(h);
            if (_good.TryGetValue(h, out rec) && rec.Local) _good.Remove(h);
            SaveLocal();
        }

        public static void SaveLocal()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("# Process Sanitizer — локальные дополнения БД угроз");
                lines.Add("# BAD|sha256|семейство|комментарий / GOOD|sha256|имя|комментарий");
                foreach (ThreatRecord r in Records)
                {
                    if (!r.Local) continue;
                    lines.Add(r.Kind + "|" + r.Hash + "|" + r.Label + "|" + r.Comment);
                }
                File.WriteAllLines(LocalPath, lines.ToArray());
            }
            catch { }
        }

        /// <summary>
        /// Снять эталоны (GOOD) с системных файлов ЭТОЙ машины.
        /// Делать только на заведомо чистой системе!
        /// </summary>
        public static int CaptureBaseline()
        {
            int added = 0;
            string win = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            string sys32 = Path.Combine(win, "System32");

            List<string> files = new List<string>();
            files.Add(Path.Combine(win, "explorer.exe"));
            files.Add(Path.Combine(win, "ImmersiveControlPanel", "SystemSettings.exe"));
            string[] sys32Names =
            {
                "svchost.exe", "csrss.exe", "lsass.exe", "services.exe", "winlogon.exe",
                "wininit.exe", "smss.exe", "spoolsv.exe", "dllhost.exe", "taskhostw.exe",
                "conhost.exe", "sihost.exe", "dwm.exe", "fontdrvhost.exe", "audiodg.exe",
                "ctfmon.exe", "RuntimeBroker.exe", "ApplicationFrameHost.exe",
                "cmd.exe", "notepad.exe", "taskmgr.exe", "regedit.exe", "mmc.exe",
                "control.exe", "userinit.exe"
            };
            foreach (string n in sys32Names) files.Add(Path.Combine(sys32, n));

            // UWP-компоненты оболочки в SystemApps
            string sysApps = Path.Combine(win, "SystemApps");
            if (Directory.Exists(sysApps))
            {
                string[] shellNames =
                {
                    "ShellExperienceHost.exe", "StartMenuExperienceHost.exe", "SearchApp.exe",
                    "SearchUI.exe", "TextInputHost.exe", "LockApp.exe", "SecHealthUI.exe",
                    "PeopleExperienceHost.exe"
                };
                foreach (string dir in Directory.GetDirectories(sysApps))
                    foreach (string n in shellNames)
                    {
                        string f = Path.Combine(dir, n);
                        if (File.Exists(f)) files.Add(f);
                    }
            }

            string stamp = "baseline " + Environment.OSVersion.Version + " " + DateTime.Now.ToString("yyyy-MM-dd");
            foreach (string f in files)
            {
                string h = FileHashCache.Get(f);
                if (h.Length == 0 || _good.ContainsKey(h)) continue;
                _good[h] = new ThreatRecord
                { Kind = "GOOD", Hash = h, Label = Path.GetFileName(f), Comment = stamp, Local = true };
                added++;
            }
            SaveLocal();
            return added;
        }

        // ---------- Внутреннее ----------

        private static void ReadAll(TextReader r, bool local)
        {
            string line;
            while ((line = r.ReadLine()) != null) Parse(line, local);
        }

        private static void Parse(string line, bool local)
        {
            string t = line.Trim();
            if (t.Length == 0 || t.StartsWith("#")) return;
            string[] p = t.Split('|');
            if (p.Length < 3) return;
            string kind = p[0].Trim().ToUpperInvariant();
            string hash = p[1].Trim().ToLowerInvariant();
            if (!HashDatabase.IsValidHash(hash)) return;

            ThreatRecord rec = new ThreatRecord();
            rec.Kind = kind;
            rec.Hash = hash;
            rec.Label = p[2].Trim();
            rec.Comment = p.Length > 3 ? p[3].Trim() : "";
            rec.Local = local;

            if (kind == "BAD") _bad[hash] = rec;
            else if (kind == "GOOD") _good[hash] = rec;
        }
    }
}