using System;
using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Process_Sanitizer.Core
{
    /// <summary>
    /// Результат проверки обновлений на GitHub.
    /// </summary>
    public class UpdateInfo
    {
        public bool Error;
        public string ErrorMessage;
        public string CurrentVersion;
        public string LatestVersion;
        public bool HasUpdate;
        public string ReleaseUrl;     // страница релиза (html)
        public string DownloadUrl;    // прямой .exe/.zip или null
    }

    /// <summary>
    /// Проверка обновлений через GitHub API (releases/latest).
    /// Источник — официальный репозиторий проекта; HTTPS, без сторонних серверов.
    /// </summary>
    public static class GitHubUpdater
    {
        private const string Owner = "edgeplus1";
        private const string Repo = "Process-Sanitizer";
        private static readonly string ApiUrl =
            "https://api.github.com/repos/" + Owner + "/" + Repo + "/releases/latest";

        public static string CurrentVersion
        {
            get
            {
                try { return Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
                catch { return "1.0.0"; }
            }
        }

        /// <summary>
        /// Асинхронно проверить наличие новой версии.
        /// </summary>
        public static Task<UpdateInfo> CheckAsync()
        {
            return Task.Run(() =>
            {
                UpdateInfo info = new UpdateInfo();
                info.CurrentVersion = CurrentVersion;
                try
                {
                    using (WebClient wc = new WebClient())
                    {
                        wc.Headers[HttpRequestHeader.UserAgent] = "Process-Sanitizer-Update";
                        wc.Headers[HttpRequestHeader.Accept] = "application/vnd.github+json";
                        string json = wc.DownloadString(ApiUrl);
                        Parse(json, info);
                    }
                }
                catch (WebException wex)
                {
                    info.Error = true;
                    if (wex.Response != null)
                    {
                        try
                        {
                            using (var r = new System.IO.StreamReader(wex.Response.GetResponseStream()))
                            {
                                string body = r.ReadToEnd();
                                if (body.Contains("Not Found"))
                                    info.ErrorMessage = "Релизов пока нет — опубликуй первый на GitHub.";
                                else if (body.Contains("rate limit"))
                                    info.ErrorMessage = "Превышен лимит запросов GitHub API. Попробуй позже.";
                                else
                                    info.ErrorMessage = wex.Message;
                            }
                        }
                        catch { info.ErrorMessage = wex.Message; }
                    }
                    else info.ErrorMessage = wex.Message;
                }
                catch (Exception ex)
                {
                    info.Error = true;
                    info.ErrorMessage = ex.Message;
                }
                return info;
            });
        }

        // ---------- Парсинг ----------

        private static void Parse(string json, UpdateInfo info)
        {
            info.LatestVersion = Extract(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");

            info.ReleaseUrl = Extract(json, "\"html_url\"\\s*:\\s*\"([^\"]*releases[^\"]*)\"");
            if (string.IsNullOrEmpty(info.ReleaseUrl))
                info.ReleaseUrl = Extract(json, "\"html_url\"\\s*:\\s*\"([^\"]+)\"");

            string dl = Extract(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+\\.exe)\"");
            if (string.IsNullOrEmpty(dl))
                dl = Extract(json, "\"browser_download_url\"\\s*:\\s*\"([^\"]+\\.zip)\"");
            info.DownloadUrl = dl;

            Version cur, latest;
            if (Version.TryParse(Normalize(info.CurrentVersion), out cur) &&
                Version.TryParse(Normalize(info.LatestVersion), out latest))
                info.HasUpdate = latest > cur;
            else
                info.HasUpdate = !string.IsNullOrEmpty(info.LatestVersion) &&
                                 Normalize(info.LatestVersion) != Normalize(info.CurrentVersion);
        }

        private static string Normalize(string v)
        {
            if (string.IsNullOrEmpty(v)) return "0.0.0";
            v = v.Trim();
            if (v.StartsWith("v", StringComparison.OrdinalIgnoreCase)) v = v.Substring(1);
            return v;
        }

        private static string Extract(string input, string pattern)
        {
            Match m = Regex.Match(input, pattern);
            return m.Success ? m.Groups[1].Value : null;
        }

        /// <summary>
        /// Скачать файл с прогрессом. onComplete(ok, error).
        /// </summary>
        public static void Download(string url, string targetPath,
            Action<int> onProgress, Action<bool, string> onComplete)
        {
            WebClient wc = new WebClient();
            wc.Headers[HttpRequestHeader.UserAgent] = "Process-Sanitizer-Update";
            wc.DownloadProgressChanged += delegate (object s, DownloadProgressChangedEventArgs e)
            {
                if (onProgress != null) onProgress(e.ProgressPercentage);
            };
            wc.DownloadFileCompleted += delegate (object s, System.ComponentModel.AsyncCompletedEventArgs e)
            {
                bool ok = e.Error == null;
                string err = ok ? null : e.Error.Message;
                wc.Dispose();
                if (onComplete != null) onComplete(ok, err);
            };
            wc.DownloadFileAsync(new Uri(url), targetPath);
        }
    }
}