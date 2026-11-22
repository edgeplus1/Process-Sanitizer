using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Process_Sanitizer.Core;
using Process_Sanitizer.Models;
using Process_Sanitizer.UI;

namespace Process_Sanitizer
{
    public partial class Form1 : Form
    {
        private List<ProcessInfo> _all = new List<ProcessInfo>();
        private bool _scanning;
        private readonly HashSet<string> _reportedAlerts = new HashSet<string>();
        private int _lastThreats = -1;

        private NotifyIcon _tray;
        private ToolStripMenuItem _miTray;
        private ToolStripMenuItem _miNotify;
        private ToolStripMenuItem _miAutoTray;
        private ToolStripMenuItem[] _intervalItems;
        private readonly ListViewSorter _sorter = new ListViewSorter();

        // График ( 13)
        private PerformanceGraph _graph;
        private ToolStripButton _tsbGraph;
        private bool _graphVisible = true;

        public Form1()
        {
            InitializeComponent();
            Whitelist.Load();
            HashDatabase.Load();
            ThreatDatabase.Load();
            AppSettings.Load();

            BuildToolsMenu();
            BuildSettingsMenu();
            BuildTray();
            BuildListContextMenu();
            BuildGraph();

            ToolStripMenuItem miDestroy = new ToolStripMenuItem("Уничтожить навсегда (force)...");
            miDestroy.Click += DestroyClick;
            menuActions.DropDownItems.Add(new ToolStripSeparator());
            menuActions.DropDownItems.Add(miDestroy);

            lvProcesses.ListViewItemSorter = _sorter;
            lvProcesses.ColumnClick += LvProcesses_ColumnClick;

            tmrRefresh.Interval = AppSettings.RefreshIntervalMs;
            foreach (ToolStripMenuItem mi in _intervalItems)
                mi.Checked = ((int)mi.Tag == AppSettings.RefreshIntervalMs);
            _miTray.Checked = AppSettings.MinimizeToTray;
            _miNotify.Checked = AppSettings.NotifyOnThreat;
            chkAutoRefresh.Checked = AppSettings.AutoRefresh;

            if (AppSettings.WindowBounds != Rectangle.Empty)
            {
                StartPosition = FormStartPosition.Manual;
                Bounds = AppSettings.WindowBounds;
            }
            if (AppSettings.Maximized) WindowState = FormWindowState.Maximized;

            Log(" 13: живой график нагрузки CPU/RAM/GPU/Disk.");
            Log("БД угроз: BAD " + ThreatDatabase.BadCount + ", GOOD " + ThreatDatabase.GoodCount);
            Log("Белый список: " + Whitelist.Count + " записей.");
        }

        // ---------- График ----------

        private void BuildGraph()
        {
            _graph = new PerformanceGraph();
            _graph.Dock = DockStyle.Top;
            _graph.Height = 140; // компактный, чтобы таблица не сжималась

            _tsbGraph = new ToolStripButton("График");
            _tsbGraph.DisplayStyle = ToolStripItemDisplayStyle.Text;
            _tsbGraph.Click += delegate { ToggleGraph(); };
            toolStrip1.Items.Insert(0, _tsbGraph);

            this.Controls.Add(_graph);
            int splitIndex = this.Controls.IndexOf(splitMain);
            if (splitIndex >= 0)
                this.Controls.SetChildIndex(_graph, splitIndex);

            splitMain.BringToFront();
            _graph.Visible = _graphVisible;
        }

        private void ToggleGraph()
        {
            _graphVisible = !_graphVisible;
            _graph.Visible = _graphVisible;
            Log("График: " + (_graphVisible ? "показан" : "скрыт"));
        }

        private void UpdateGraph(List<ProcessInfo> list)
        {
            double totalCpu = 0;
            double totalRamMb = 0;
            double maxGpu = 0;
            double totalDisk = 0;
            bool anyGpu = false;

            foreach (ProcessInfo pi in list)
            {
                if (pi.CpuPercent > 0) totalCpu += pi.CpuPercent;
                if (pi.MemoryMb > 0) totalRamMb += pi.MemoryMb;
                if (pi.GpuPercent >= 0) { anyGpu = true; if (pi.GpuPercent > maxGpu) maxGpu = pi.GpuPercent; }
                if (pi.IoMbPerSec > 0) totalDisk += pi.IoMbPerSec;
            }

            double cpuPercent = totalCpu / Math.Max(1, Environment.ProcessorCount);
            if (cpuPercent > 100) cpuPercent = 100;

            ulong totalPhys = ProcessCollector.TotalPhysBytes;
            double ramPercent = totalPhys > 0 ? totalRamMb * 1048576.0 / totalPhys * 100.0 : 0;
            if (ramPercent > 100) ramPercent = 100;

            PerfSample s = new PerfSample
            {
                Time = DateTime.Now,
                Cpu = cpuPercent,
                Ram = ramPercent,
                Gpu = anyGpu ? maxGpu : -1,
                Disk = totalDisk
            };
            _graph.Add(s);
        }

        // ---------- Построение UI кодом ----------

        private void BuildToolsMenu()
        {
            ToolStripMenuItem menuTools = new ToolStripMenuItem("Инструменты");

            ToolStripMenuItem miPersist = new ToolStripMenuItem("Проверка автозагрузок...");
            miPersist.Click += delegate { using (PersistenceForm f = new PersistenceForm()) f.ShowDialog(this); };

            ToolStripMenuItem miQuarView = new ToolStripMenuItem("Карантин (хранилище)...");
            miQuarView.Click += delegate { using (QuarantineForm f = new QuarantineForm()) f.ShowDialog(this); };

            ToolStripMenuItem miHashDb = new ToolStripMenuItem("База хешей...");
            miHashDb.Click += delegate { using (HashDatabaseForm f = new HashDatabaseForm()) f.ShowDialog(this); };

            ToolStripMenuItem miThreatDb = new ToolStripMenuItem("База угроз (БД)...");
            miThreatDb.Click += delegate { using (ThreatDbForm f = new ThreatDbForm()) f.ShowDialog(this); };

            ToolStripMenuItem miExportTxt = new ToolStripMenuItem("Экспорт отчёта (TXT)...");
            miExportTxt.Click += delegate { ExportReport(false); };

            ToolStripMenuItem miExportHtml = new ToolStripMenuItem("Экспорт отчёта (HTML)...");
            miExportHtml.Click += delegate { ExportReport(true); };

            menuTools.DropDownItems.Add(miPersist);
            menuTools.DropDownItems.Add(miQuarView);
            menuTools.DropDownItems.Add(miHashDb);
            menuTools.DropDownItems.Add(miThreatDb);
            menuTools.DropDownItems.Add(new ToolStripSeparator());
            menuTools.DropDownItems.Add(miExportTxt);
            menuTools.DropDownItems.Add(miExportHtml);
            menuStrip1.Items.Add(menuTools);
        }

        private void BuildSettingsMenu()
        {
            menuSettings.DropDownItems.Add(new ToolStripSeparator());

            _miTray = new ToolStripMenuItem("Сворачивать в трей");
            _miTray.CheckOnClick = true;
            menuSettings.DropDownItems.Add(_miTray);

            _miNotify = new ToolStripMenuItem("Уведомления об угрозах");
            _miNotify.CheckOnClick = true;
            menuSettings.DropDownItems.Add(_miNotify);

            menuSettings.DropDownItems.Add(new ToolStripSeparator());

            _intervalItems = new ToolStripMenuItem[]
            {
                new ToolStripMenuItem("Интервал: 3 секунды"),
                new ToolStripMenuItem("Интервал: 5 секунд"),
                new ToolStripMenuItem("Интервал: 10 секунд")
            };
            _intervalItems[0].Tag = 3000;
            _intervalItems[1].Tag = 5000;
            _intervalItems[2].Tag = 10000;
            foreach (ToolStripMenuItem mi in _intervalItems)
            {
                mi.Click += IntervalClick;
                menuSettings.DropDownItems.Add(mi);
            }
        }

        private void BuildTray()
        {
            Icon appIcon = null;
            try { appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }
            if (appIcon == null) appIcon = SystemIcons.Application;
            Icon = appIcon;

            ContextMenuStrip trayMenu = new ContextMenuStrip();
            ToolStripMenuItem miOpen = new ToolStripMenuItem("Открыть");
            miOpen.Click += delegate { RestoreFromTray(); };
            ToolStripMenuItem miScan = new ToolStripMenuItem("Сканировать");
            miScan.Click += async delegate { await RunScanAsync(); };
            _miAutoTray = new ToolStripMenuItem("Автообновление");
            _miAutoTray.CheckOnClick = true;
            _miAutoTray.Click += delegate { chkAutoRefresh.Checked = _miAutoTray.Checked; };
            ToolStripMenuItem miExit = new ToolStripMenuItem("Выход");
            miExit.Click += delegate { Close(); };
            trayMenu.Items.Add(miOpen);
            trayMenu.Items.Add(miScan);
            trayMenu.Items.Add(_miAutoTray);
            trayMenu.Items.Add(new ToolStripSeparator());
            trayMenu.Items.Add(miExit);

            _tray = new NotifyIcon();
            _tray.Icon = appIcon;
            _tray.Text = "Process Sanitizer";
            _tray.ContextMenuStrip = trayMenu;
            _tray.Visible = true;
            _tray.DoubleClick += delegate { RestoreFromTray(); };
        }

        private void BuildListContextMenu()
        {
            ContextMenuStrip ctx = new ContextMenuStrip();
            ToolStripMenuItem miKill = new ToolStripMenuItem("Завершить");
            miKill.Click += KillClick;
            ToolStripMenuItem miQuar = new ToolStripMenuItem("В карантин");
            miQuar.Click += QuarantineClick;
            ToolStripMenuItem miDestroyCtx = new ToolStripMenuItem("Уничтожить навсегда (force)");
            miDestroyCtx.Click += DestroyClick;
            ToolStripMenuItem miOpen = new ToolStripMenuItem("Открыть папку");
            miOpen.Click += OpenPathClick;
            ToolStripMenuItem miWhite = new ToolStripMenuItem("Белый список (добавить/убрать)");
            miWhite.Click += WhitelistClick;
            ToolStripMenuItem miCopy = new ToolStripMenuItem("Копировать путь");
            miCopy.Click += CopyPathClick;
            ToolStripMenuItem miHashBlack = new ToolStripMenuItem("Хеш → в чёрный список");
            miHashBlack.Click += HashBlackClick;
            ToolStripMenuItem miHashWhite = new ToolStripMenuItem("Хеш → в белый список (добавить/убрать)");
            miHashWhite.Click += HashWhiteClick;
            ToolStripMenuItem miDbBad = new ToolStripMenuItem("Хеш → в БД угроз (BAD)");
            miDbBad.Click += DbBadClick;
            ToolStripMenuItem miDbGood = new ToolStripMenuItem("Хеш → эталон (GOOD)");
            miDbGood.Click += DbGoodClick;
            ctx.Items.Add(miKill);
            ctx.Items.Add(miQuar);
            ctx.Items.Add(miDestroyCtx);
            ctx.Items.Add(miOpen);
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add(miWhite);
            ctx.Items.Add(miHashBlack);
            ctx.Items.Add(miHashWhite);
            ctx.Items.Add(miDbBad);
            ctx.Items.Add(miDbGood);
            ctx.Items.Add(new ToolStripSeparator());
            ctx.Items.Add(miCopy);
            lvProcesses.ContextMenuStrip = ctx;
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (WindowState == FormWindowState.Minimized && _miTray != null && _miTray.Checked)
                Hide();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            AppSettings.RefreshIntervalMs = tmrRefresh.Interval;
            AppSettings.AutoRefresh = chkAutoRefresh.Checked;
            AppSettings.MinimizeToTray = _miTray.Checked;
            AppSettings.NotifyOnThreat = _miNotify.Checked;
            AppSettings.Maximized = (WindowState == FormWindowState.Maximized);
            AppSettings.WindowBounds = (WindowState == FormWindowState.Normal) ? Bounds : RestoreBounds;
            AppSettings.Save();

            if (_tray != null)
            {
                _tray.Visible = false;
                _tray.Dispose();
                _tray = null;
            }
            base.OnFormClosing(e);
        }

        // ---------- Служебное ----------

        private void Log(string message)
        {
            txtLog.AppendText(string.Format("[{0:HH:mm:ss}] {1}{2}", DateTime.Now, message, Environment.NewLine));
        }

        private ProcessInfo SelectedProcess()
        {
            if (lvProcesses.SelectedItems.Count == 0) return null;
            int pid = (int)lvProcesses.SelectedItems[0].Tag;
            return _all.Find(x => x.Pid == pid);
        }

        private static bool IsThreat(string risk)
        {
            return risk == "HIGH" || risk == "CRITICAL" || risk == "DEAD";
        }

        // ---------- Сканирование ----------

        private async Task RunScanAsync()
        {
            if (_scanning) return;
            _scanning = true;
            tslStatus.Text = "Сканирование...";

            Stopwatch sw = Stopwatch.StartNew();
            List<ProcessInfo> result = null;
            List<string> alerts = null;
            try
            {
                result = await Task.Run(() =>
                {
                    List<ProcessInfo> list = ProcessCollector.Collect();
                    foreach (ProcessInfo pi in list)
                        RiskAnalyzer.Assess(pi);
                    BehaviorAnalyzer.ApplyAll(list);
                    alerts = QuarantineGuard.Check(list);
                    return list;
                });
            }
            catch (Exception ex)
            {
                Log("Ошибка сканирования: " + ex.Message);
            }
            sw.Stop();

            if (result != null)
            {
                _all = result;
                UpdateGraph(result);
                RenderList();

                int threats = 0;
                foreach (ProcessInfo pi in _all)
                    if (IsThreat(pi.Risk)) threats++;

                tslCount.Text = "Процессов: " + _all.Count;
                tslThreat.Text = "Угроз: " + threats;

                Log(string.Format("Сканирование завершено: процессов {0}, угроз {1}, время {2} мс",
                    _all.Count, threats, sw.ElapsedMilliseconds));

                if (threats > 0)
                {
                    int shown = 0;
                    foreach (ProcessInfo pi in _all)
                    {
                        if (!IsThreat(pi.Risk)) continue;
                        Log("  ! " + pi.Risk + "  " + pi.Name + " (PID " + pi.Pid + ", баллов " + pi.RiskScore + ")");
                        shown++;
                        if (shown >= 10) { Log("  ... и другие, смотри таблицу"); break; }
                    }
                }

                if (_miNotify.Checked && threats > 0 && threats != _lastThreats &&
                    (!Visible || WindowState == FormWindowState.Minimized))
                {
                    _tray.ShowBalloonTip(4000, "Process Sanitizer",
                        "Обнаружены угрозы: " + threats + ". Открой программу для просмотра.",
                        ToolTipIcon.Warning);
                }
                _lastThreats = threats;

                if (alerts != null)
                {
                    foreach (string alert in alerts)
                    {
                        if (_reportedAlerts.Add(alert))
                        {
                            Log("  !! ВОЗВРАТ КАРАНТИНА: " + alert);
                            if (_miNotify.Checked)
                                _tray.ShowBalloonTip(5000, "Возврат карантина!", alert, ToolTipIcon.Error);
                        }
                    }
                }
            }

            tslStatus.Text = "Готов";
            _scanning = false;
        }

        // ---------- Отрисовка списка ----------

        private void RenderList()
        {
            string filter = txtSearch.Text.Trim();
            int selectedPid = -1;
            if (lvProcesses.SelectedItems.Count > 0)
                selectedPid = (int)lvProcesses.SelectedItems[0].Tag;

            lvProcesses.BeginUpdate();
            try
            {
                lvProcesses.Items.Clear();
                foreach (ProcessInfo pi in _all)
                {
                    if (filter.Length > 0 && !Matches(pi, filter)) continue;

                    ListViewItem item = new ListViewItem(pi.Name);
                    item.SubItems.Add(pi.Pid.ToString());
                    item.SubItems.Add(string.IsNullOrEmpty(pi.User) ? "—" : pi.User);
                    item.SubItems.Add(string.IsNullOrEmpty(pi.Path) ? "—" : pi.Path);
                    item.SubItems.Add(pi.CpuPercent < 0 ? "—" : pi.CpuPercent.ToString("0.0"));
                    item.SubItems.Add(pi.MemoryMb < 0 ? "—" : pi.MemoryMb.ToString("0"));
                    item.SubItems.Add(pi.Risk);
                    item.Tag = pi.Pid;

                    switch (pi.Risk)
                    {
                        case "DEAD":
                            item.BackColor = Color.FromArgb(15, 15, 15);
                            item.ForeColor = Color.White;
                            item.Font = new Font(lvProcesses.Font, FontStyle.Bold);
                            break;
                        case "CRITICAL":
                            item.BackColor = Color.FromArgb(255, 205, 210);
                            item.ForeColor = Color.FromArgb(140, 0, 0);
                            break;
                        case "HIGH":
                            item.BackColor = Color.FromArgb(255, 224, 200);
                            item.ForeColor = Color.FromArgb(150, 60, 0);
                            break;
                        case "MEDIUM":
                            item.BackColor = Color.FromArgb(255, 246, 214);
                            break;
                    }

                    lvProcesses.Items.Add(item);
                    if (pi.Pid == selectedPid) item.Selected = true;
                }
                lvProcesses.Sort();
            }
            finally
            {
                lvProcesses.EndUpdate();
            }
        }

        private bool Matches(ProcessInfo pi, string filter)
        {
            return pi.Name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || (pi.Path ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || (pi.User ?? string.Empty).IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || pi.Risk.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0
                || pi.Pid.ToString() == filter;
        }

        private void LvProcesses_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (_sorter.Column == e.Column)
                _sorter.Order = (_sorter.Order == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
            else
            {
                _sorter.Column = e.Column;
                _sorter.Order = SortOrder.Ascending;
            }
            lvProcesses.Sort();
        }

        // ---------- Экспорт отчёта ----------

        private void ExportReport(bool html)
        {
            if (_all.Count == 0)
            {
                MessageBox.Show("Сначала выполни сканирование — отчёт строится по его результатам.",
                    "Экспорт", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Title = html ? "Сохранить HTML-отчёт" : "Сохранить TXT-отчёт";
                sfd.Filter = html ? "HTML-отчёт (*.html)|*.html" : "Текстовый отчёт (*.txt)|*.txt";
                sfd.FileName = "ProcessSanitizer_report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + (html ? ".html" : ".txt");
                if (sfd.ShowDialog(this) != DialogResult.OK) return;

                Cursor = Cursors.WaitCursor;
                try
                {
                    List<QuarantineItem> quar = Quarantine.List();
                    List<PersistenceEntry> pers = PersistenceScanner.Scan();
                    string content = html
                        ? ReportExporter.BuildHtml(_all, quar, pers)
                        : ReportExporter.BuildTxt(_all, quar, pers);
                    File.WriteAllText(sfd.FileName, content, Encoding.UTF8);
                    Log("Отчёт сохранён: " + sfd.FileName);
                    MessageBox.Show("Отчёт сохранён:\n" + sfd.FileName, "Экспорт",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    Log("Ошибка экспорта: " + ex.Message);
                    MessageBox.Show("Не удалось сохранить отчёт: " + ex.Message, "Экспорт",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                finally
                {
                    Cursor = Cursors.Default;
                }
            }
        }

        // ---------- Действия ----------

        private async void KillClick(object sender, EventArgs e)
        {
            ProcessInfo pi = SelectedProcess();
            if (pi == null) { Log("Сначала выбери процесс в таблице."); return; }

            if (pi.Pid == Process.GetCurrentProcess().Id)
            {
                MessageBox.Show("Это сама программа Process Sanitizer — завершать себя из себя нельзя :)",
                    "Защита", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (ProcessActions.IsProtected(pi))
            {
                MessageBox.Show(
                    "Процесс \"" + pi.Name + "\" (PID " + pi.Pid + ") защищён:\n" +
                    "это критический системный процесс в системной папке.\n\n" +
                    "Завершение таких процессов может обрушить Windows.",
                    "Защищённый процесс", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Log("Отказ: попытка завершить защищённый процесс " + pi.Name + " (PID " + pi.Pid + ")");
                return;
            }

            DialogResult dr = MessageBox.Show(
                "Завершить процесс?\n\n" +
                "Имя:   " + pi.Name + "\n" +
                "PID:   " + pi.Pid + "\n" +
                "Путь:  " + (string.IsNullOrEmpty(pi.Path) ? "—" : pi.Path) + "\n" +
                "Риск:  " + pi.Risk + " (баллов " + pi.RiskScore + ")\n\n" +
                "Несохранённые данные этого процесса будут потеряны.",
                "Подтверждение завершения", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (dr != DialogResult.Yes) { Log("Завершение отменено пользователем: " + pi.Name); return; }

            string error;
            bool ok = ProcessActions.Kill(pi.Pid, out error);
            if (ok) Log("Процесс завершён: " + pi.Name + " (PID " + pi.Pid + ")");
            else Log("Не удалось завершить " + pi.Name + " (PID " + pi.Pid + "): " + error);

            await RunScanAsync();
        }

        private async void QuarantineClick(object sender, EventArgs e)
        {
            ProcessInfo pi = SelectedProcess();
            if (pi == null) { Log("Сначала выбери процесс в таблице."); return; }

            if (pi.Pid == Process.GetCurrentProcess().Id)
            {
                MessageBox.Show("Нельзя поместить в карантин саму программу.", "Защита",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (string.IsNullOrEmpty(pi.Path))
            {
                Log("Путь недоступен (нужны права администратора): " + pi.Name);
                return;
            }
            if (ProcessActions.IsProtected(pi))
            {
                MessageBox.Show("Это защищённый системный процесс — карантин запрещён.", "Защита",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string lower = pi.Path.ToLowerInvariant();
            if (lower.StartsWith(@"c:\windows\") || lower.Contains(@"\program files\"))
            {
                MessageBox.Show(
                    "Файл лежит в системной/доверенной папке:\n" + pi.Path + "\n\n" +
                    "Карантин таких файлов не рекомендуется — можно сломать систему или приложение.",
                    "Доверенное расположение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult dr = MessageBox.Show(
                "Завершить процесс и поместить его файл в карантин?\n\n" +
                "Имя:   " + pi.Name + "\n" +
                "PID:   " + pi.Pid + "\n" +
                "Файл:  " + pi.Path + "\n" +
                "Риск:  " + pi.Risk + " (баллов " + pi.RiskScore + ")\n\n" +
                "Файл будет перемещён в хранилище Quarantine с хешем SHA256.\n" +
                "Его можно будет восстановить или удалить навсегда.",
                "Подтверждение карантина", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes) { Log("Карантин отменён пользователем: " + pi.Name); return; }

            string error;
            bool killed = ProcessActions.Kill(pi.Pid, out error);
            Log(killed
                ? "Процесс завершён перед карантином: " + pi.Name + " (PID " + pi.Pid + ")"
                : "Не удалось завершить процесс: " + error + " (пробуем карантин файла)");

            string id = Quarantine.Add(pi.Path, pi.Name, pi.Pid, pi.Risk, out error);
            if (id != null)
                Log("В карантине: " + id + "  <-  " + pi.Path);
            else
                Log("Ошибка карантина: " + error);

            await RunScanAsync();
        }

        private async void DestroyClick(object sender, EventArgs e)
        {
            ProcessInfo pi = SelectedProcess();
            if (pi == null) { Log("Сначала выбери процесс в таблице."); return; }

            if (pi.Pid == Process.GetCurrentProcess().Id)
            {
                MessageBox.Show("Нельзя уничтожить саму программу из неё же.", "Защита",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string lower = (pi.Path ?? string.Empty).ToLowerInvariant();
            bool systemTarget = ProcessActions.IsProtected(pi)
                             || lower.StartsWith(@"c:\windows\")
                             || lower.Contains(@"\program files\");

            string warn =
                "УНИЧТОЖЕНИЕ НАВСЕГДА (force)\n\n" +
                "Процесс будет принудительно завершён вместе с деревом.\n" +
                "Файл будет удалён БЕЗ карантина и БЕЗ возможности восстановления.\n" +
                "Записи автозагрузки Run с этим путём будут удалены.\n\n" +
                "Имя:   " + pi.Name + "\n" +
                "PID:   " + pi.Pid + "\n" +
                "Файл:  " + (string.IsNullOrEmpty(pi.Path) ? "—" : pi.Path) + "\n" +
                "Риск:  " + pi.Risk + " (баллов " + pi.RiskScore + ")";

            if (systemTarget)
            {
                MessageBox.Show(warn + "\n\nВНИМАНИЕ: это системный/доверенный объект!\n" +
                    "Система может упасть или перестать загружаться.",
                    "ОПАСНАЯ ОПЕРАЦИЯ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                string typed = InputDialog.Ask("Подтверждение уничтожения",
                    "Введи слово УНИЧТОЖИТЬ для подтверждения:");
                if (typed == null || typed.Trim() != "УНИЧТОЖИТЬ")
                {
                    Log("Уничтожение отменено (не введено подтверждение): " + pi.Name);
                    return;
                }
            }
            else
            {
                DialogResult dr = MessageBox.Show(warn + "\n\nПродолжить?",
                    "Уничтожить навсегда", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dr != DialogResult.Yes) { Log("Уничтожение отменено пользователем: " + pi.Name); return; }
            }

            string report;
            bool reboot;
            ProcessActions.ForceDestroy(pi, out report, out reboot);
            Log("УНИЧТОЖЕНО (force): " + pi.Name + " (PID " + pi.Pid + ")");
            foreach (string line in report.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                Log("  " + line);

            if (reboot)
                MessageBox.Show("Файл занят — он будет удалён при перезагрузке Windows.",
                    "Уничтожение", MessageBoxButtons.OK, MessageBoxIcon.Information);

            await RunScanAsync();
        }

        private void OpenPathClick(object sender, EventArgs e)
        {
            ProcessInfo pi = SelectedProcess();
            if (pi == null) { Log("Сначала выбери процесс в таблице."); return; }
            if (string.IsNullOrEmpty(pi.Path))
            {
                Log("Путь недоступен (нужны права администратора): " + pi.Name);
                return;
            }
            try
            {
                ProcessActions.OpenFolder(pi.Path);
                Log("Открыта папка процесса: " + pi.Path);
            }
            catch (Exception ex)
            {
                Log("Не удалось открыть папку: " + ex.Message);
            }
        }

        private void CopyPathClick(object sender, EventArgs e)
        {
            ProcessInfo pi = SelectedProcess();
            if (pi == null) { Log("Сначала выбери процесс в таблице."); return; }
            if (string.IsNullOrEmpty(pi.Path)) { Log("Путь недоступен: " + pi.Name); return; }
            try
            {
                Clipboard.SetText(pi.Path);
                Log("Путь скопирован в буфер: " + pi.Path);
            }
            catch (Exception ex)
            {
                Log("Не удалось скопировать путь: " + ex.Message);
            }
        }

        private async void WhitelistClick(object sender, EventArgs e)
        {
            ProcessInfo pi = SelectedProcess();
            if (pi == null) { Log("Сначала выбери процесс в таблице."); return; }

            if (Whitelist.Contains(pi))
            {
                Whitelist.Remove(pi);
                Log("Убрано из белого списка: " + pi.Name);
            }
            else
            {
                Whitelist.Add(pi);
                Log("Добавлено в белый список: " + pi.Name + "  [" + Whitelist.Key(pi) + "]");
            }

            await RunScanAsync();
        }

        private async void HashBlackClick(object sender, EventArgs e)
        {
            ProcessInfo pi = SelectedProcess();
            if (pi == null) { Log("Сначала выбери процесс в таблице."); return; }
            if (string.IsNullOrEmpty(pi.Sha256)) { Log("Хеш недоступен (нет пути/прав): " + pi.Name); return; }

            HashDatabase.AddBlack(pi.Sha256, pi.Name + "  " + DateTime.Now.ToString("yyyy-MM-dd"));
            Log("Хеш добавлен в ЧЁРНЫЙ список: " + pi.Name + "  " + pi.Sha256);
            await RunScanAsync();
        }

        private async void HashWhiteClick(object sender, EventArgs e)
        {
            ProcessInfo pi = SelectedProcess();
            if (pi == null) { Log("Сначала выбери процесс в таблице."); return; }
            if (string.IsNullOrEmpty(pi.Sha256)) { Log("Хеш недоступен (нет пути/прав): " + pi.Name); return; }

            if (HashDatabase.IsWhitelisted(pi.Sha256))
            {
                HashDatabase.RemoveWhite(pi.Sha256);
                Log("Хеш убран из белого списка: " + pi.Name);
            }
            else
            {
                HashDatabase.AddWhite(pi.Sha256, pi.Name + "  " + DateTime.Now.ToString("yyyy-MM-dd"));
                Log("Хеш добавлен в БЕЛЫЙ список: " + pi.Name + "  " + pi.Sha256);
            }
            await RunScanAsync();
        }

        private async void DbBadClick(object sender, EventArgs e)
        {
            ProcessInfo pi = SelectedProcess();
            if (pi == null) { Log("Сначала выбери процесс в таблице."); return; }
            if (string.IsNullOrEmpty(pi.Sha256)) { Log("Хеш недоступен: " + pi.Name); return; }

            ThreatDatabase.AddLocalBad(pi.Sha256, pi.Name,
                "добавлено вручную " + DateTime.Now.ToString("yyyy-MM-dd"));
            Log("БД угроз: BAD  " + pi.Name + "  " + pi.Sha256);
            await RunScanAsync();
        }

        private async void DbGoodClick(object sender, EventArgs e)
        {
            ProcessInfo pi = SelectedProcess();
            if (pi == null) { Log("Сначала выбери процесс в таблице."); return; }
            if (string.IsNullOrEmpty(pi.Sha256)) { Log("Хеш недоступен: " + pi.Name); return; }

            ThreatDatabase.AddLocalGood(pi.Sha256, pi.Name,
                "эталон вручную " + DateTime.Now.ToString("yyyy-MM-dd"));
            Log("БД угроз: GOOD  " + pi.Name + "  " + pi.Sha256);
            await RunScanAsync();
        }

        // ---------- Обработчики ----------

        private async void ScanClick(object sender, EventArgs e)
        {
            await RunScanAsync();
        }

        private async void TmrRefresh_Tick(object sender, EventArgs e)
        {
            await RunScanAsync();
        }

        private void IntervalClick(object sender, EventArgs e)
        {
            ToolStripMenuItem mi = (ToolStripMenuItem)sender;
            int ms = (int)mi.Tag;
            tmrRefresh.Interval = ms;
            foreach (ToolStripMenuItem it in _intervalItems)
                it.Checked = (it == mi);
            Log("Интервал автообновления: " + (ms / 1000) + " с");
        }

        private void LvProcesses_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvProcesses.SelectedItems.Count == 0)
            {
                txtDetails.Text = string.Empty;
                return;
            }

            ProcessInfo pi = SelectedProcess();
            if (pi == null)
            {
                txtDetails.Text = "Процесс уже завершён.";
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Процесс:       " + pi.Name);
            sb.AppendLine("PID:           " + pi.Pid);
            sb.AppendLine("Пользователь:  " + (string.IsNullOrEmpty(pi.User) ? "—" : pi.User));
            sb.AppendLine("Путь:          " + (string.IsNullOrEmpty(pi.Path) ? "—" : pi.Path));
            sb.AppendLine("Память:        " + (pi.MemoryMb < 0 ? "—" : pi.MemoryMb.ToString("0") + " МБ"));
            sb.AppendLine("CPU:           " + (pi.CpuPercent < 0 ? "—" : pi.CpuPercent.ToString("0.0") + " %"));
            sb.AppendLine("GPU:           " + (pi.GpuPercent < 0 ? "—" : pi.GpuPercent.ToString("0.0") + " %"));
            sb.AppendLine("RAM:           " + (pi.RamPercent < 0 ? "—" : pi.RamPercent.ToString("0.0") + " % физической"));
            sb.AppendLine("Диск IO:       " + (pi.IoMbPerSec < 0 ? "—" : pi.IoMbPerSec.ToString("0.0") + " МБ/с"));
            sb.AppendLine("Запущен:       " + (pi.StartTime == DateTime.MinValue ? "—" : pi.StartTime.ToString("yyyy-MM-dd HH:mm:ss")));
            sb.AppendLine("Подпись:       " + pi.Signature +
                          (string.IsNullOrEmpty(pi.Signer) ? "" : "  (" + pi.Signer + ")"));
            sb.AppendLine("SHA256:        " + (string.IsNullOrEmpty(pi.Sha256) ? "—" : pi.Sha256));
            sb.AppendLine("Белый список:  " + (Whitelist.Contains(pi) || HashDatabase.IsWhitelisted(pi.Sha256) ? "да" : "нет"));
            sb.AppendLine();
            sb.AppendLine("Командная строка:");
            sb.AppendLine(string.IsNullOrEmpty(pi.CommandLine) ? "—" : pi.CommandLine);
            sb.AppendLine();
            sb.AppendLine("РИСК: " + pi.Risk + "  (баллов: " + pi.RiskScore + ")");
            if (pi.RiskReasons.Count > 0)
            {
                sb.AppendLine("Причины:");
                foreach (string reason in pi.RiskReasons)
                    sb.AppendLine("  " + reason);
            }
            else
            {
                sb.AppendLine("Подозрительных признаков не найдено.");
            }
            txtDetails.Text = sb.ToString();
        }

        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            if (_all.Count > 0) RenderList();
        }

        private void ChkAutoRefresh_CheckedChanged(object sender, EventArgs e)
        {
            tmrRefresh.Enabled = chkAutoRefresh.Checked;
            menuAutoRefresh.Checked = chkAutoRefresh.Checked;
            if (_miAutoTray != null) _miAutoTray.Checked = chkAutoRefresh.Checked;
            Log("Автообновление: " + (chkAutoRefresh.Checked ? "ВКЛ" : "ВЫКЛ"));
        }

        private void MenuAutoRefresh_Click(object sender, EventArgs e)
        {
            chkAutoRefresh.Checked = !chkAutoRefresh.Checked;
        }

        private void MenuExit_Click(object sender, EventArgs e)
        {
            Close();
        }

        // ---------- О программе (вызов твоей существующей формы) ----------
        private void MenuAbout_Click(object sender, EventArgs e)
        {
            using (AboutForm f = new AboutForm())
            {
                f.ShowDialog(this);
            }
        }
    }
}