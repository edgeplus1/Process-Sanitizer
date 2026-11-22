using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Проверка цифровой подписи файла через WinVerifyTrust.
    /// Результат: VALID / UNSIGNED / INVALID: причина / UNKNOWN.
    /// </summary>
    public static class SignatureChecker
    {
        private static readonly Dictionary<string, string> _cache = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _signerCache = new Dictionary<string, string>();
        private static readonly Guid ActionVerifyV2 = new Guid("00AAC56B-CD44-11d0-8CC2-00C04FC295EE");

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_FILE_INFO
        {
            public uint cbStruct;
            public IntPtr pcwszFilePath;
            public IntPtr hFile;
            public IntPtr pgKnownSubject;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WINTRUST_DATA
        {
            public uint cbStruct;
            public IntPtr pPolicyCallbackData;
            public IntPtr pSIPClientData;
            public uint dwUIChoice;
            public uint fdwRevocationChecks;
            public uint dwUnionChoice;
            public IntPtr pFile;
            public uint dwStateAction;
            public IntPtr hWVTStateData;
            public IntPtr pwszURLReference;
            public uint dwProvFlags;
            public uint dwUIContext;
        }

        [DllImport("wintrust.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint WinVerifyTrust(IntPtr hwnd, ref Guid pgActionID, ref WINTRUST_DATA pWVTData);

        /// <summary>
        /// Проверить подпись. Кэш по пути+размеру+времени изменения файла.
        /// </summary>
        public static string Verify(string path)
        {
            if (string.IsNullOrEmpty(path)) return "UNKNOWN";

            string key;
            try
            {
                FileInfo fi = new FileInfo(path);
                key = path.ToLowerInvariant() + "|" + fi.Length + "|" + fi.LastWriteTimeUtc.Ticks;
            }
            catch { return "UNKNOWN"; }

            string cached;
            if (_cache.TryGetValue(key, out cached)) return cached;

            string result = VerifyInner(path);
            _cache[key] = result;
            return result;
        }

        /// <summary>
        /// Имя подписанта (CN из Subject), если подпись есть.
        /// </summary>
        public static string GetSigner(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            string key = path.ToLowerInvariant();
            string cached;
            if (_signerCache.TryGetValue(key, out cached)) return cached;

            string signer = "";
            try
            {
                using (X509Certificate2 cert = new X509Certificate2(X509Certificate.CreateFromSignedFile(path)))
                {
                    foreach (string part in cert.Subject.Split(','))
                    {
                        string t = part.Trim();
                        if (t.StartsWith("CN=", StringComparison.OrdinalIgnoreCase))
                        {
                            signer = t.Substring(3);
                            break;
                        }
                    }
                    if (signer.Length == 0) signer = cert.Subject;
                }
            }
            catch { signer = ""; }

            _signerCache[key] = signer;
            return signer;
        }

        // ---------- Внутреннее ----------

        private static string VerifyInner(string path)
        {
            IntPtr pPath = Marshal.StringToCoTaskMemUni(path);
            IntPtr pFileInfo = IntPtr.Zero;
            try
            {
                WINTRUST_FILE_INFO fileInfo = new WINTRUST_FILE_INFO();
                fileInfo.cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
                fileInfo.pcwszFilePath = pPath;
                fileInfo.hFile = IntPtr.Zero;
                fileInfo.pgKnownSubject = IntPtr.Zero;

                pFileInfo = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
                Marshal.StructureToPtr(fileInfo, pFileInfo, false);

                WINTRUST_DATA data = new WINTRUST_DATA();
                data.cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_DATA));
                data.dwUIChoice = 2;          // WTD_UI_NONE — без диалогов
                data.fdwRevocationChecks = 0; // WTD_REVOKE_NONE — без сети
                data.dwUnionChoice = 1;       // WTD_CHOICE_FILE
                data.pFile = pFileInfo;
                data.dwStateAction = 1;       // WTD_STATEACTION_VERIFY

                Guid action = ActionVerifyV2;
                uint res = WinVerifyTrust(IntPtr.Zero, ref action, ref data);

                data.dwStateAction = 2;       // WTD_STATEACTION_CLOSE
                WinVerifyTrust(IntPtr.Zero, ref action, ref data);

                if (res == 0) return "VALID";
                if (res == 0x800B0100) return "UNSIGNED";
                return "INVALID: " + Reason(res);
            }
            catch
            {
                return "UNKNOWN";
            }
            finally
            {
                if (pFileInfo != IntPtr.Zero) Marshal.FreeCoTaskMem(pFileInfo);
                Marshal.FreeCoTaskMem(pPath);
            }
        }

        private static string Reason(uint code)
        {
            switch (code)
            {
                case 0x800B0101: return "сертификат просрочен";
                case 0x800B0109: return "недоверенный корневой сертификат";
                case 0x800B010A: return "не построена цепочка сертификатов";
                case 0x800B010C: return "сертификат отозван";
                case 0x800B010E: return "проверка отзыва не удалась";
                case 0x80096001: return "системная ошибка доверия";
                case 0x80096002: return "нет сертификата подписанта";
                case 0x80096004: return "неверная подпись сертификата";
                case 0x80096005: return "проблема с меткой времени";
                case 0x80096010: return "хеш файла не совпадает с подписью (файл изменён)";
                case 0x800B010B: return "общая ошибка проверки доверия";
                default: return "код 0x" + code.ToString("X8");
            }
        }
    }
}