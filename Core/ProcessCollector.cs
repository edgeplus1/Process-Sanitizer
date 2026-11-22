using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using Process_Sanitizer.Models;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Сборщик информации о запущенных процессах.
    /// CPU и IO считаются дельтой между сканами (при первом скане будет "—").
    /// GPU — через счётчики "GPU Engine", RAM% — от физической памяти.
    /// </summary>
    public static class ProcessCollector
    {
        private class CpuSample
        {
            public DateTime Time;
            public TimeSpan Cpu;
        }

        private class IoSample
        {
            public DateTime Time;
            public ulong Bytes;
        }

        // Кэши предыдущих замеров (pid -> замер)
        private static readonly Dictionary<int, CpuSample> _prev = new Dictionary<int, CpuSample>();
        private static readonly Dictionary<int, IoSample> _prevIo = new Dictionary<int, IoSample>();
        private static ulong _totalPhys;
        public static ulong TotalPhysBytes { get { return _totalPhys; } }

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint TOKEN_QUERY = 0x0008;
        private const int TOKEN_USER = 1;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool GetTokenInformation(IntPtr TokenHandle, int TokenInformationClass, IntPtr TokenInformation, int TokenInformationLength, out int ReturnLength);

        [StructLayout(LayoutKind.Sequential)]
        private struct IO_COUNTERS
        {
            public ulong ReadOperationCount;
            public ulong WriteOperationCount;
            public ulong OtherOperationCount;
            public ulong ReadTransferCount;
            public ulong WriteTransferCount;
            public ulong OtherTransferCount;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetProcessIoCounters(IntPtr hProcess, out IO_COUNTERS counters);

        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX buffer);

        /// <summary>
        /// Собрать список всех процессов системы.
        /// </summary>
        public static List<ProcessInfo> Collect()
        {
            Dictionary<int, string> cmdLines = GetCommandLines();
            List<ProcessInfo> list = new List<ProcessInfo>();
            DateTime now = DateTime.Now;
            Dictionary<int, CpuSample> nextPrev = new Dictionary<int, CpuSample>();

            foreach (Process p in Process.GetProcesses())
            {
                using (p)
                {
                    ProcessInfo info = new ProcessInfo();
                    info.Pid = p.Id;
                    info.Name = SafeString(() => p.ProcessName, "???");
                    info.Path = GetPath(p.Id);
                    info.User = GetOwner(p.Id);
                    info.UserSid = GetOwnerSid(p.Id);
                    info.CommandLine = cmdLines.ContainsKey(p.Id) ? cmdLines[p.Id] : string.Empty;
                    info.MemoryMb = SafeDouble(() => p.WorkingSet64 / 1048576.0, -1);
                    info.StartTime = SafeTime(p);
                    info.CpuPercent = ComputeCpu(p, now, nextPrev);
                    info.IoMbPerSec = ComputeIo(p.Id, now);
                    info.RamPercent = ComputeRamPercent(info.MemoryMb);
                    info.GpuPercent = GpuMonitor.Get(p.Id);
                    list.Add(info);
                }
            }

            // Обновляем кэш замеров CPU
            _prev.Clear();
            foreach (KeyValuePair<int, CpuSample> kv in nextPrev)
                _prev[kv.Key] = kv.Value;

            // Чистим IO-кэш от завершившихся процессов
            List<int> deadIo = new List<int>();
            foreach (int k in _prevIo.Keys)
                if (!nextPrev.ContainsKey(k)) deadIo.Add(k);
            foreach (int k in deadIo) _prevIo.Remove(k);

            list.Sort(delegate (ProcessInfo a, ProcessInfo b)
            {
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return list;
        }

        /// <summary>
        /// Текущий % CPU: дельта процессорного времени между сканами.
        /// </summary>
        private static double ComputeCpu(Process p, DateTime now, Dictionary<int, CpuSample> nextPrev)
        {
            TimeSpan total;
            try { total = p.TotalProcessorTime; }
            catch { return -1; }

            nextPrev[p.Id] = new CpuSample { Time = now, Cpu = total };

            CpuSample prev;
            if (!_prev.TryGetValue(p.Id, out prev)) return -1; // первый замер — данных ещё нет

            double wallMs = (now - prev.Time).TotalMilliseconds;
            if (wallMs <= 0) return -1;

            double cpuMs = (total - prev.Cpu).TotalMilliseconds;
            double percent = (cpuMs / wallMs) * 100.0 / Environment.ProcessorCount;
            if (percent < 0) percent = 0;
            if (percent > 100) percent = 100;
            return percent;
        }

        /// <summary>
        /// Дисковый IO процесса: МБ/с по дельте счётчиков IO.
        /// </summary>
        private static double ComputeIo(int pid, DateTime now)
        {
            IntPtr h = OpenProcess(PROCESS_QUERY_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return -1;
            try
            {
                IO_COUNTERS c;
                if (!GetProcessIoCounters(h, out c)) return -1;
                ulong bytes = c.ReadTransferCount + c.WriteTransferCount + c.OtherTransferCount;

                IoSample prev;
                _prevIo.TryGetValue(pid, out prev);
                _prevIo[pid] = new IoSample { Time = now, Bytes = bytes };
                if (prev == null) return -1;

                double sec = (now - prev.Time).TotalSeconds;
                if (sec <= 0) return -1;
                return (bytes - prev.Bytes) / 1048576.0 / sec;
            }
            finally
            {
                CloseHandle(h);
            }
        }

        /// <summary>
        /// Доля рабочей памяти процесса от всей физической RAM, %.
        /// </summary>
        private static double ComputeRamPercent(double memMb)
        {
            if (memMb < 0) return -1;
            if (_totalPhys == 0)
            {
                MEMORYSTATUSEX m = new MEMORYSTATUSEX();
                m.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                if (GlobalMemoryStatusEx(ref m)) _totalPhys = m.ullTotalPhys;
                if (_totalPhys == 0) return -1;
            }
            return memMb * 1048576.0 / _totalPhys * 100.0;
        }

        /// <summary>
        /// Полный путь к exe через WinAPI (работает там, где MainModule бросает исключение).
        /// </summary>
        private static string GetPath(int pid)
        {
            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return string.Empty;
            try
            {
                StringBuilder sb = new StringBuilder(1024);
                int size = sb.Capacity;
                if (QueryFullProcessImageName(h, 0, sb, ref size))
                    return sb.ToString();
                return string.Empty;
            }
            finally
            {
                CloseHandle(h);
            }
        }

        /// <summary>
        /// Владелец процесса через токен доступа (быстрее, чем WMI GetOwner).
        /// </summary>
        private static string GetOwner(int pid)
        {
            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return string.Empty;
            try
            {
                IntPtr hToken;
                if (!OpenProcessToken(h, TOKEN_QUERY, out hToken)) return string.Empty;
                try
                {
                    int len;
                    bool ok = GetTokenInformation(hToken, TOKEN_USER, IntPtr.Zero, 0, out len);
                    if (!ok && Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER) return string.Empty;

                    IntPtr buf = Marshal.AllocHGlobal(len);
                    try
                    {
                        if (!GetTokenInformation(hToken, TOKEN_USER, buf, len, out len)) return string.Empty;
                        IntPtr sidPtr = Marshal.ReadIntPtr(buf);
                        SecurityIdentifier sid = new SecurityIdentifier(sidPtr);
                        try
                        {
                            return sid.Translate(typeof(NTAccount)).Value;
                        }
                        catch
                        {
                            return sid.Value; // имя не разрешилось — показываем SID
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buf);
                    }
                }
                finally
                {
                    CloseHandle(hToken);
                }
            }
            finally
            {
                CloseHandle(h);
            }
        }

        /// <summary>
        /// SID владельца процесса — не зависит от локализации Windows.
        /// Именно по нему анализатор узнаёт SYSTEM / LOCAL SERVICE / NETWORK SERVICE.
        /// </summary>
        public static string GetOwnerSid(int pid)
        {
            IntPtr h = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (h == IntPtr.Zero) return string.Empty;
            try
            {
                IntPtr hToken;
                if (!OpenProcessToken(h, TOKEN_QUERY, out hToken)) return string.Empty;
                try
                {
                    int len;
                    bool ok = GetTokenInformation(hToken, TOKEN_USER, IntPtr.Zero, 0, out len);
                    if (!ok && Marshal.GetLastWin32Error() != ERROR_INSUFFICIENT_BUFFER) return string.Empty;

                    IntPtr buf = Marshal.AllocHGlobal(len);
                    try
                    {
                        if (!GetTokenInformation(hToken, TOKEN_USER, buf, len, out len)) return string.Empty;
                        return new SecurityIdentifier(Marshal.ReadIntPtr(buf)).Value;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buf);
                    }
                }
                finally
                {
                    CloseHandle(hToken);
                }
            }
            finally
            {
                CloseHandle(h);
            }
        }

        /// <summary>
        /// Командные строки всех процессов одним WMI-запросом.
        /// </summary>
        private static Dictionary<int, string> GetCommandLines()
        {
            Dictionary<int, string> map = new Dictionary<int, string>();
            try
            {
                using (ManagementObjectSearcher searcher =
                    new ManagementObjectSearcher("SELECT ProcessId, CommandLine FROM Win32_Process"))
                using (ManagementObjectCollection results = searcher.Get())
                {
                    foreach (ManagementObject mo in results)
                    {
                        using (mo)
                        {
                            object pidObj = mo["ProcessId"];
                            if (pidObj == null) continue;
                            int pid = Convert.ToInt32(pidObj);
                            map[pid] = mo["CommandLine"] as string ?? string.Empty;
                        }
                    }
                }
            }
            catch
            {
                // WMI недоступна — работаем без командных строк
            }
            return map;
        }

        // ---------- Защитные обёртки ----------

        private static string SafeString(Func<string> f, string def)
        {
            try { return f(); }
            catch { return def; }
        }

        private static double SafeDouble(Func<double> f, double def)
        {
            try { return f(); }
            catch { return def; }
        }

        private static DateTime SafeTime(Process p)
        {
            try { return p.StartTime; }
            catch { return DateTime.MinValue; }
        }
    }
}