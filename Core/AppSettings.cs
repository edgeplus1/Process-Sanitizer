using System;
using System.Drawing;
using System.IO;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Настройки программы в settings.txt (key=value, строки с # — комментарии).
    /// </summary>
    public static class AppSettings
    {
        public static int RefreshIntervalMs = 3000;
        public static bool AutoRefresh = false;
        public static bool MinimizeToTray = true;
        public static bool NotifyOnThreat = true;
        public static Rectangle WindowBounds = Rectangle.Empty;
        public static bool Maximized = false;

        public static string FilePath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.txt"); }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                foreach (string line in File.ReadAllLines(FilePath))
                {
                    string t = line.Trim();
                    if (t.Length == 0 || t.StartsWith("#")) continue;
                    int eq = t.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = t.Substring(0, eq);
                    string val = t.Substring(eq + 1);
                    switch (key)
                    {
                        case "interval": RefreshIntervalMs = ParseInt(val, 3000); break;
                        case "autorefresh": AutoRefresh = ParseBool(val, false); break;
                        case "tray": MinimizeToTray = ParseBool(val, true); break;
                        case "notify": NotifyOnThreat = ParseBool(val, true); break;
                        case "maximized": Maximized = ParseBool(val, false); break;
                        case "bounds":
                            string[] p = val.Split(',');
                            if (p.Length == 4)
                                WindowBounds = new Rectangle(
                                    ParseInt(p[0], 0), ParseInt(p[1], 0),
                                    ParseInt(p[2], 980), ParseInt(p[3], 660));
                            break;
                    }
                }
            }
            catch { }
        }

        public static void Save()
        {
            try
            {
                File.WriteAllLines(FilePath, new string[]
                {
                    "# Process Sanitizer settings",
                    "interval=" + RefreshIntervalMs,
                    "autorefresh=" + AutoRefresh,
                    "tray=" + MinimizeToTray,
                    "notify=" + NotifyOnThreat,
                    "maximized=" + Maximized,
                    "bounds=" + WindowBounds.X + "," + WindowBounds.Y + "," + WindowBounds.Width + "," + WindowBounds.Height
                });
            }
            catch { }
        }

        private static int ParseInt(string s, int def)
        {
            int v;
            return int.TryParse(s, out v) ? v : def;
        }

        private static bool ParseBool(string s, bool def)
        {
            if (s == "true" || s == "1") return true;
            if (s == "false" || s == "0") return false;
            return def;
        }
    }
}