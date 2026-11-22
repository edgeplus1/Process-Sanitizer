using System;
using System.Collections.Generic;
using System.IO;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Локальная база хешей: чёрный список (CRITICAL) и белый (SAFE).
    /// Формат строки: "sha256 комментарий..." (# в начале строки — комментарий).
    /// </summary>
    public static class HashDatabase
    {
        private static readonly Dictionary<string, string> _black =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string> _white =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static string BlackPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hash_blacklist.txt"); }
        }

        public static string WhitePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "hash_whitelist.txt"); }
        }

        public static IEnumerable<KeyValuePair<string, string>> Black { get { return _black; } }
        public static IEnumerable<KeyValuePair<string, string>> White { get { return _white; } }

        public static void Load()
        {
            _black.Clear();
            _white.Clear();
            ReadFile(BlackPath, _black);
            ReadFile(WhitePath, _white);
        }

        public static void Save()
        {
            WriteFile(BlackPath, _black);
            WriteFile(WhitePath, _white);
        }

        public static bool IsBlacklisted(string hash)
        {
            return !string.IsNullOrEmpty(hash) && _black.ContainsKey(hash);
        }

        public static bool IsWhitelisted(string hash)
        {
            return !string.IsNullOrEmpty(hash) && _white.ContainsKey(hash);
        }

        public static void AddBlack(string hash, string comment)
        {
            _black[Normalize(hash)] = comment ?? "";
            _white.Remove(Normalize(hash));
            Save();
        }

        public static void AddWhite(string hash, string comment)
        {
            _white[Normalize(hash)] = comment ?? "";
            _black.Remove(Normalize(hash));
            Save();
        }

        public static void RemoveBlack(string hash) { _black.Remove(Normalize(hash)); Save(); }
        public static void RemoveWhite(string hash) { _white.Remove(Normalize(hash)); Save(); }

        public static bool IsValidHash(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length != 64) return false;
            foreach (char c in s)
            {
                bool ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!ok) return false;
            }
            return true;
        }

        private static string Normalize(string hash)
        {
            return (hash ?? "").Trim().ToLowerInvariant();
        }

        private static void ReadFile(string path, Dictionary<string, string> dict)
        {
            try
            {
                if (!File.Exists(path)) return;
                foreach (string line in File.ReadAllLines(path))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) continue;
                    int sp = t.IndexOf(' ');
                    string hash = sp < 0 ? t : t.Substring(0, sp);
                    string comment = sp < 0 ? "" : t.Substring(sp + 1).Trim();
                    if (IsValidHash(hash)) dict[Normalize(hash)] = comment;
                }
            }
            catch { }
        }

        private static void WriteFile(string path, Dictionary<string, string> dict)
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("# Process Sanitizer hash database");
                lines.Add("# формат: sha256 комментарий");
                foreach (KeyValuePair<string, string> kv in dict)
                    lines.Add(kv.Key + (kv.Value.Length > 0 ? " " + kv.Value : ""));
                File.WriteAllLines(path, lines.ToArray());
            }
            catch { }
        }
    }
}