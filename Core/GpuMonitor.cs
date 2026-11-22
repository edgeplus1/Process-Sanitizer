using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Загрузка GPU по процессам через счётчики "GPU Engine" (Windows 10+).
    /// Если категории нет (Win7/старые драйверы) — возвращает -1.
    /// </summary>
    public static class GpuMonitor
    {
        private static readonly Dictionary<string, PerformanceCounter> _counters =
            new Dictionary<string, PerformanceCounter>();
        private static DateTime _refreshAt = DateTime.MinValue;
        private static bool _available = true;

        public static double Get(int pid)
        {
            if (!_available) return -1;
            try
            {
                if (_counters.Count == 0 || (DateTime.Now - _refreshAt).TotalSeconds > 5)
                    Refresh();
                if (!_available) return -1;

                string prefix = "pid_" + pid + "_";
                double sum = 0;
                bool any = false;
                foreach (KeyValuePair<string, PerformanceCounter> kv in _counters)
                {
                    if (!kv.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                    any = true;
                    sum += kv.Value.NextValue();
                }
                return any ? Math.Min(100.0, sum) : 0.0;
            }
            catch
            {
                _available = false;
                return -1;
            }
        }

        private static void Refresh()
        {
            foreach (PerformanceCounter c in _counters.Values)
            {
                try { c.Dispose(); } catch { }
            }
            _counters.Clear();
            _refreshAt = DateTime.Now;

            try
            {
                if (!PerformanceCounterCategory.Exists("GPU Engine"))
                {
                    _available = false;
                    return;
                }
                PerformanceCounterCategory cat = new PerformanceCounterCategory("GPU Engine");
                foreach (string inst in cat.GetInstanceNames())
                {
                    try
                    {
                        _counters[inst] = new PerformanceCounter("GPU Engine", "Utilization Percentage", inst, true);
                    }
                    catch { }
                }
            }
            catch
            {
                _available = false;
            }
        }
    }
}