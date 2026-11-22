using System;
using System.Collections.Generic;
using System.IO;
using Process_Sanitizer.Models;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Белый список доверенных объектов.
    /// Хранится построчно в whitelist.txt рядом с exe (строки с # — комментарии).
    /// Ключ: полный путь в нижнем регистре, либо "name:имя" если путь недоступен.
    /// </summary>
    public static class Whitelist
    {
        private static readonly HashSet<string> _items = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static string FilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "whitelist.txt"); }
        }

        public static int Count { get { return _items.Count; } }

        public static string Key(ProcessInfo pi)
        {
            return string.IsNullOrEmpty(pi.Path)
                ? "name:" + (pi.Name ?? string.Empty).ToLowerInvariant()
                : pi.Path.ToLowerInvariant();
        }

        public static void Load()
        {
            _items.Clear();
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) continue;
                    _items.Add(t);
                }
            }
            catch
            {
                // файл повреждён/занят — работаем с пустым списком
            }
        }

        public static void Save()
        {
            try
            {
                List<string> lines = new List<string>();
                lines.Add("# Process Sanitizer whitelist");
                lines.Add("# Одна строка = один доверенный путь (или name:имя)");
                List<string> sorted = new List<string>(_items);
                sorted.Sort(StringComparer.OrdinalIgnoreCase);
                lines.AddRange(sorted);
                File.WriteAllLines(FilePath, lines.ToArray());
            }
            catch
            {
                // нет прав на запись — список останется в памяти до выхода
            }
        }

        public static bool Contains(ProcessInfo pi)
        {
            return _items.Contains(Key(pi));
        }

        public static void Add(ProcessInfo pi)
        {
            _items.Add(Key(pi));
            Save();
        }

        public static void Remove(ProcessInfo pi)
        {
            _items.Remove(Key(pi));
            Save();
        }
    }
}