using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// SHA256 файла с кэшем по пути+размеру+времени изменения.
    /// Первый скан медленнее, дальше — мгновенно из кэша.
    /// </summary>
    public static class FileHashCache
    {
        private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();

        public static string Get(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";

            string key;
            try
            {
                FileInfo fi = new FileInfo(path);
                key = path.ToLowerInvariant() + "|" + fi.Length + "|" + fi.LastWriteTimeUtc.Ticks;
            }
            catch { return ""; }

            string cached;
            if (_cache.TryGetValue(key, out cached)) return cached;

            string hash = Compute(path);
            _cache[key] = hash;
            return hash;
        }

        private static string Compute(string path)
        {
            try
            {
                // ReadWrite|Delete — чтобы читать даже запущенные exe
                using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read,
                    FileShare.ReadWrite | FileShare.Delete))
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(fs);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
            catch { return ""; }
        }
    }
}