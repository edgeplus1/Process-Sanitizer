using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Один объект в карантине.
    /// </summary>
    public class QuarantineItem
    {
        public string Id;
        public string OriginalPath;
        public string Sha256;
        public long Size;
        public DateTime AddedAt;
        public string ProcessName;
        public int Pid;
        public string Risk;

        public string BinPath { get { return Path.Combine(Quarantine.Dir, Id + ".qbin"); } }
        public string MetaPath { get { return Path.Combine(Quarantine.Dir, Id + ".meta"); } }
    }

    /// <summary>
    /// Карантин: файлы лежат как <id>.qbin (не exe — не запустятся случайно),
    /// рядом метаданные <id>.meta (путь, хеш, время, риск).
    /// </summary>
    public static class Quarantine
    {
        public static string Dir
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Quarantine"); }
        }

        /// <summary>
        /// Переместить файл в карантин. Возвращает id или null при ошибке.
        /// </summary>
        public static string Add(string sourcePath, string procName, int pid, string risk, out string error)
        {
            error = null;
            try
            {
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
                {
                    error = "файл не найден (возможно, уже удалён)";
                    return null;
                }

                Directory.CreateDirectory(Dir);

                string hash = ComputeSha256(sourcePath);
                long size = new FileInfo(sourcePath).Length;
                string id = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" +
                            Guid.NewGuid().ToString("N").Substring(0, 6);
                string bin = Path.Combine(Dir, id + ".qbin");

                // Сначала пробуем перемещение (быстро, тот же том),
                // иначе копирование + удаление (другой том).
                try
                {
                    File.Move(sourcePath, bin);
                }
                catch
                {
                    File.Copy(sourcePath, bin, true);
                    File.Delete(sourcePath);
                }

                QuarantineItem item = new QuarantineItem();
                item.Id = id;
                item.OriginalPath = sourcePath;
                item.Sha256 = hash;
                item.Size = size;
                item.AddedAt = DateTime.Now;
                item.ProcessName = procName;
                item.Pid = pid;
                item.Risk = risk;
                WriteMeta(item);
                return id;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Все объекты карантина, новые сверху.
        /// </summary>
        public static List<QuarantineItem> List()
        {
            List<QuarantineItem> list = new List<QuarantineItem>();
            try
            {
                if (!Directory.Exists(Dir)) return list;
                foreach (string meta in Directory.GetFiles(Dir, "*.meta"))
                {
                    QuarantineItem item = ReadMeta(meta);
                    if (item != null) list.Add(item);
                }
            }
            catch { }
            list.Sort(delegate (QuarantineItem a, QuarantineItem b) { return b.AddedAt.CompareTo(a.AddedAt); });
            return list;
        }

        /// <summary>
        /// Вернуть файл на исходное место.
        /// </summary>
        public static bool Restore(string id, out string error)
        {
            error = null;
            try
            {
                QuarantineItem item = Find(id);
                if (item == null) { error = "объект не найден"; return false; }
                if (!File.Exists(item.BinPath)) { error = "файл карантина отсутствует"; return false; }
                if (File.Exists(item.OriginalPath)) { error = "по исходному пути уже есть файл"; return false; }

                string dir = Path.GetDirectoryName(item.OriginalPath);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                File.Move(item.BinPath, item.OriginalPath);
                File.Delete(item.MetaPath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Удалить объект карантина навсегда.
        /// </summary>
        public static bool DeletePermanent(string id, out string error)
        {
            error = null;
            try
            {
                QuarantineItem item = Find(id);
                if (item == null) { error = "объект не найден"; return false; }
                if (File.Exists(item.BinPath)) File.Delete(item.BinPath);
                if (File.Exists(item.MetaPath)) File.Delete(item.MetaPath);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        // ---------- Внутреннее ----------

        private static QuarantineItem Find(string id)
        {
            foreach (QuarantineItem it in List())
                if (it.Id == id) return it;
            return null;
        }

        private static string ComputeSha256(string path)
        {
            using (SHA256 sha = SHA256.Create())
            using (FileStream fs = File.OpenRead(path))
            {
                byte[] hash = sha.ComputeHash(fs);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static void WriteMeta(QuarantineItem it)
        {
            File.WriteAllLines(it.MetaPath, new string[]
            {
                "id=" + it.Id,
                "path=" + it.OriginalPath,
                "sha256=" + it.Sha256,
                "size=" + it.Size,
                "added=" + it.AddedAt.ToString("o"),
                "name=" + it.ProcessName,
                "pid=" + it.Pid,
                "risk=" + it.Risk
            });
        }

        private static QuarantineItem ReadMeta(string metaPath)
        {
            try
            {
                QuarantineItem it = new QuarantineItem();
                foreach (string line in File.ReadAllLines(metaPath))
                {
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq);
                    string val = line.Substring(eq + 1);
                    switch (key)
                    {
                        case "id": it.Id = val; break;
                        case "path": it.OriginalPath = val; break;
                        case "sha256": it.Sha256 = val; break;
                        case "size": it.Size = long.Parse(val); break;
                        case "added": it.AddedAt = DateTime.Parse(val, null, System.Globalization.DateTimeStyles.RoundtripKind); break;
                        case "name": it.ProcessName = val; break;
                        case "pid": it.Pid = int.Parse(val); break;
                        case "risk": it.Risk = val; break;
                    }
                }
                return string.IsNullOrEmpty(it.Id) ? null : it;
            }
            catch
            {
                return null;
            }
        }
    }
}