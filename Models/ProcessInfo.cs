using System;
using System.Collections.Generic;

namespace Process_Sanitizer.Models
{
    /// <summary>
    /// Модель процесса: всё, что мы о нём знаем.
    /// </summary>
    public class ProcessInfo
    {
        public int Pid { get; set; }
        public string Name { get; set; }
        public string User { get; set; }
        public string UserSid { get; set; }
        public string Path { get; set; }
        public string CommandLine { get; set; }
        public double CpuPercent { get; set; }    // -1 = нет данных
        public double MemoryMb { get; set; }      // -1 = нет данных
        public double GpuPercent { get; set; }    // -1 = нет данных ( 12)
        public double RamPercent { get; set; }    // % от физической памяти ( 12)
        public double IoMbPerSec { get; set; }    // дисковый IO, МБ/с ( 12)
        public DateTime StartTime { get; set; }

        // --- Риск ---
        public string Risk { get; set; }
        public int RiskScore { get; set; }
        public List<string> RiskReasons { get; set; }

        // --- Подпись ---
        public string Signature { get; set; }
        public string Signer { get; set; }

        // --- Хеш ---
        public string Sha256 { get; set; }

        public ProcessInfo()
        {
            Name = string.Empty;
            User = string.Empty;
            UserSid = string.Empty;
            Path = string.Empty;
            CommandLine = string.Empty;
            CpuPercent = -1;
            MemoryMb = -1;
            GpuPercent = -1;
            RamPercent = -1;
            IoMbPerSec = -1;
            StartTime = DateTime.MinValue;
            Risk = "SAFE";
            RiskScore = 0;
            RiskReasons = new List<string>();
            Signature = "UNKNOWN";
            Signer = string.Empty;
            Sha256 = string.Empty;
        }
    }
}