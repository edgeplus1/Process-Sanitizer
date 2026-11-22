using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using Process_Sanitizer.Core;

namespace Process_Sanitizer.UI
{
    /// <summary>
    /// Окно проверки и установки обновлений через GitHub (стандартная тема).
    /// </summary>
    public partial class UpdateForm : Form
    {
        private bool _busy;
        private string _downloadUrl;

        public UpdateForm()
        {
            InitializeComponent();

            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }

            lblCurrent.Text = "Текущая версия: " + GitHubUpdater.CurrentVersion;
        }

        // ---------- Логика ----------

        private async void btnCheck_Click(object sender, EventArgs e)
        {
            if (_busy) return;
            _busy = true;
            btnCheck.Enabled = false;
            btnDownload.Enabled = false;
            btnOpenPage.Enabled = false;
            lblStatus.ForeColor = SystemColors.ControlText;
            lblStatus.Text = "Запрос к GitHub...";

            UpdateInfo info = await GitHubUpdater.CheckAsync();
            _busy = false;
            btnCheck.Enabled = true;

            if (info.Error)
            {
                lblStatus.Text = "Не удалось проверить: " + info.ErrorMessage;
                lblStatus.ForeColor = Color.Red;
                return;
            }

            lblLatest.Text = "Последняя версия: " + (info.LatestVersion ?? "—");

            if (info.HasUpdate)
            {
                lblStatus.Text = "Доступно обновление!";
                lblStatus.ForeColor = Color.Green;
                btnOpenPage.Enabled = true;
                if (!string.IsNullOrEmpty(info.DownloadUrl))
                {
                    _downloadUrl = info.DownloadUrl;
                    btnDownload.Enabled = true;
                }
            }
            else
            {
                lblStatus.Text = "У вас самая свежая версия. Всё обновлено";
                lblStatus.ForeColor = Color.Green;
                btnOpenPage.Enabled = true;
            }
        }

        private void btnOpenPage_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://github.com/edgeplus1/Process-Sanitizer/releases");
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Не удалось открыть браузер: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            if (_busy || string.IsNullOrEmpty(_downloadUrl)) return;
            _busy = true;
            btnDownload.Enabled = false;
            btnCheck.Enabled = false;
            bar.Visible = true;
            bar.Value = 0;
            lblStatus.ForeColor = SystemColors.ControlText;
            lblStatus.Text = "Скачивание...";

            string dir = GetDownloadsFolder();
            string name = "Process-Sanitizer-" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".exe";
            string target = Path.Combine(dir, name);

            GitHubUpdater.Download(_downloadUrl, target,
                delegate (int percent) { bar.Value = percent; },
                delegate (bool ok, string err)
                {
                    bar.Visible = false;
                    _busy = false;
                    btnCheck.Enabled = true;
                    if (ok)
                    {
                        lblStatus.Text = "Скачано в «Загрузки»: " + name + "\nЗакрой программу и запусти новый exe.";
                        lblStatus.ForeColor = Color.Green;
                        DialogResult dr = MessageBox.Show(this,
                            "Обновление скачано в папку «Загрузки».\nОткрыть папку сейчас?",
                            "Готово", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                        if (dr == DialogResult.Yes)
                            Process.Start("explorer.exe", "/select,\"" + target + "\"");
                    }
                    else
                    {
                        lblStatus.Text = "Ошибка скачивания: " + err;
                        lblStatus.ForeColor = Color.Red;
                        btnDownload.Enabled = true;
                    }
                });
        }

        /// <summary>
        /// Реальный путь к папке «Загрузки» (через реестр, т.к. в .NET Framework
        /// нет SpecialFolder.Downloads). Запасной вариант — UserProfile\Downloads.
        /// </summary>
        private static string GetDownloadsFolder()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("{374DE290-123F-4565-9164-39C4925E467B}");
                        if (val != null)
                        {
                            string path = Environment.ExpandEnvironmentVariables(val.ToString());
                            if (Directory.Exists(path)) return path;
                        }
                    }
                }
            }
            catch { }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }
    }
}