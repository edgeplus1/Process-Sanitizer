namespace Process_Sanitizer
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Код, сгенерированный конструктором форм Windows

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuFile = new System.Windows.Forms.ToolStripMenuItem();
            this.menuExit = new System.Windows.Forms.ToolStripMenuItem();
            this.menuActions = new System.Windows.Forms.ToolStripMenuItem();
            this.menuScan = new System.Windows.Forms.ToolStripMenuItem();
            this.menuKill = new System.Windows.Forms.ToolStripMenuItem();
            this.menuQuarantine = new System.Windows.Forms.ToolStripMenuItem();
            this.menuSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAutoRefresh = new System.Windows.Forms.ToolStripMenuItem();
            this.menuHelp = new System.Windows.Forms.ToolStripMenuItem();
            this.menuAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tsbScan = new System.Windows.Forms.ToolStripButton();
            this.tsbRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbKill = new System.Windows.Forms.ToolStripButton();
            this.tsbQuarantine = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbOpenPath = new System.Windows.Forms.ToolStripButton();
            this.tsbWhitelist = new System.Windows.Forms.ToolStripButton();
            this.panelTop = new System.Windows.Forms.Panel();
            this.chkAutoRefresh = new System.Windows.Forms.CheckBox();
            this.txtSearch = new System.Windows.Forms.TextBox();
            this.lblFilter = new System.Windows.Forms.Label();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.lvProcesses = new System.Windows.Forms.ListView();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colPid = new System.Windows.Forms.ColumnHeader();
            this.colUser = new System.Windows.Forms.ColumnHeader();
            this.colPath = new System.Windows.Forms.ColumnHeader();
            this.colCpu = new System.Windows.Forms.ColumnHeader();
            this.colMem = new System.Windows.Forms.ColumnHeader();
            this.colRisk = new System.Windows.Forms.ColumnHeader();
            this.splitBottom = new System.Windows.Forms.SplitContainer();
            this.txtDetails = new System.Windows.Forms.TextBox();
            this.lblDetailsHeader = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.lblLogHeader = new System.Windows.Forms.Label();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.tslStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.tslThreat = new System.Windows.Forms.ToolStripStatusLabel();
            this.tmrRefresh = new System.Windows.Forms.Timer(this.components);
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.panelTop.SuspendLayout();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.SuspendLayout();
            this.splitBottom.Panel1.SuspendLayout();
            this.splitBottom.Panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitBottom)).BeginInit();
            this.splitBottom.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuFile,
            this.menuActions,
            this.menuSettings,
            this.menuHelp});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(980, 24);
            this.menuStrip1.TabIndex = 0;
            // 
            // menuFile
            // 
            this.menuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuExit});
            this.menuFile.Name = "menuFile";
            this.menuFile.Size = new System.Drawing.Size(48, 20);
            this.menuFile.Text = "Файл";
            // 
            // menuExit
            // 
            this.menuExit.Name = "menuExit";
            this.menuExit.Size = new System.Drawing.Size(108, 22);
            this.menuExit.Text = "Выход";
            this.menuExit.Click += new System.EventHandler(this.MenuExit_Click);
            // 
            // menuActions
            // 
            this.menuActions.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuScan,
            this.menuKill,
            this.menuQuarantine});
            this.menuActions.Name = "menuActions";
            this.menuActions.Size = new System.Drawing.Size(64, 20);
            this.menuActions.Text = "Действия";
            // 
            // menuScan
            // 
            this.menuScan.Name = "menuScan";
            this.menuScan.Size = new System.Drawing.Size(152, 22);
            this.menuScan.Text = "Сканировать";
            this.menuScan.Click += new System.EventHandler(this.ScanClick);
            // 
            // menuKill
            // 
            this.menuKill.Name = "menuKill";
            this.menuKill.Size = new System.Drawing.Size(152, 22);
            this.menuKill.Text = "Завершить процесс";
            this.menuKill.Click += new System.EventHandler(this.KillClick);
            // 
            // menuQuarantine
            // 
            this.menuQuarantine.Name = "menuQuarantine";
            this.menuQuarantine.Size = new System.Drawing.Size(152, 22);
            this.menuQuarantine.Text = "В карантин";
            this.menuQuarantine.Click += new System.EventHandler(this.QuarantineClick);
            // 
            // menuSettings
            // 
            this.menuSettings.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAutoRefresh});
            this.menuSettings.Name = "menuSettings";
            this.menuSettings.Size = new System.Drawing.Size(79, 20);
            this.menuSettings.Text = "Настройки";
            // 
            // menuAutoRefresh
            // 
            this.menuAutoRefresh.CheckOnClick = true;
            this.menuAutoRefresh.Name = "menuAutoRefresh";
            this.menuAutoRefresh.Size = new System.Drawing.Size(168, 22);
            this.menuAutoRefresh.Text = "Автообновление";
            this.menuAutoRefresh.Click += new System.EventHandler(this.MenuAutoRefresh_Click);
            // 
            // menuHelp
            // 
            this.menuHelp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAbout});
            this.menuHelp.Name = "menuHelp";
            this.menuHelp.Size = new System.Drawing.Size(65, 20);
            this.menuHelp.Text = "Справка";
            // 
            // menuAbout
            // 
            this.menuAbout.Name = "menuAbout";
            this.menuAbout.Size = new System.Drawing.Size(106, 22);
            this.menuAbout.Text = "О программе";
            this.menuAbout.Click += new System.EventHandler(this.MenuAbout_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbScan,
            this.tsbRefresh,
            this.toolStripSeparator1,
            this.tsbKill,
            this.tsbQuarantine,
            this.toolStripSeparator2,
            this.tsbOpenPath,
            this.tsbWhitelist});
            this.toolStrip1.Location = new System.Drawing.Point(0, 24);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(980, 25);
            this.toolStrip1.TabIndex = 1;
            // 
            // tsbScan
            // 
            this.tsbScan.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbScan.Name = "tsbScan";
            this.tsbScan.Size = new System.Drawing.Size(46, 22);
            this.tsbScan.Text = "Скан";
            this.tsbScan.Click += new System.EventHandler(this.ScanClick);
            // 
            // tsbRefresh
            // 
            this.tsbRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbRefresh.Name = "tsbRefresh";
            this.tsbRefresh.Size = new System.Drawing.Size(73, 22);
            this.tsbRefresh.Text = "Обновить";
            this.tsbRefresh.Click += new System.EventHandler(this.ScanClick);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbKill
            // 
            this.tsbKill.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbKill.Name = "tsbKill";
            this.tsbKill.Size = new System.Drawing.Size(75, 22);
            this.tsbKill.Text = "Завершить";
            this.tsbKill.Click += new System.EventHandler(this.KillClick);
            // 
            // tsbQuarantine
            // 
            this.tsbQuarantine.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbQuarantine.Name = "tsbQuarantine";
            this.tsbQuarantine.Size = new System.Drawing.Size(72, 22);
            this.tsbQuarantine.Text = "Карантин";
            this.tsbQuarantine.Click += new System.EventHandler(this.QuarantineClick);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbOpenPath
            // 
            this.tsbOpenPath.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbOpenPath.Name = "tsbOpenPath";
            this.tsbOpenPath.Size = new System.Drawing.Size(92, 22);
            this.tsbOpenPath.Text = "Открыть папку";
            this.tsbOpenPath.Click += new System.EventHandler(this.OpenPathClick);
            // 
            // tsbWhitelist
            // 
            this.tsbWhitelist.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tsbWhitelist.Name = "tsbWhitelist";
            this.tsbWhitelist.Size = new System.Drawing.Size(94, 22);
            this.tsbWhitelist.Text = "Белый список";
            this.tsbWhitelist.Click += new System.EventHandler(this.WhitelistClick);
            // 
            // panelTop
            // 
            this.panelTop.Controls.Add(this.chkAutoRefresh);
            this.panelTop.Controls.Add(this.txtSearch);
            this.panelTop.Controls.Add(this.lblFilter);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 49);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(980, 42);
            this.panelTop.TabIndex = 2;
            // 
            // chkAutoRefresh
            // 
            this.chkAutoRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkAutoRefresh.AutoSize = true;
            this.chkAutoRefresh.Location = new System.Drawing.Point(868, 13);
            this.chkAutoRefresh.Name = "chkAutoRefresh";
            this.chkAutoRefresh.Size = new System.Drawing.Size(100, 17);
            this.chkAutoRefresh.TabIndex = 2;
            this.chkAutoRefresh.Text = "Автообновление";
            this.chkAutoRefresh.UseVisualStyleBackColor = true;
            this.chkAutoRefresh.CheckedChanged += new System.EventHandler(this.ChkAutoRefresh_CheckedChanged);
            // 
            // txtSearch
            // 
            this.txtSearch.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSearch.Location = new System.Drawing.Point(62, 11);
            this.txtSearch.Name = "txtSearch";
            this.txtSearch.Size = new System.Drawing.Size(780, 20);
            this.txtSearch.TabIndex = 1;
            this.txtSearch.TextChanged += new System.EventHandler(this.TxtSearch_TextChanged);
            // 
            // lblFilter
            // 
            this.lblFilter.AutoSize = true;
            this.lblFilter.Location = new System.Drawing.Point(12, 14);
            this.lblFilter.Name = "lblFilter";
            this.lblFilter.Size = new System.Drawing.Size(44, 13);
            this.lblFilter.TabIndex = 0;
            this.lblFilter.Text = "Фильтр:";
            // 
            // splitMain
            // 
            this.splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitMain.Location = new System.Drawing.Point(0, 91);
            this.splitMain.Name = "splitMain";
            this.splitMain.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.lvProcesses);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.splitBottom);
            this.splitMain.Size = new System.Drawing.Size(980, 547);
            this.splitMain.SplitterDistance = 380;
            this.splitMain.Panel1MinSize = 120;
            this.splitMain.Panel2MinSize = 100;
            this.splitMain.TabIndex = 3;
            // 
            // lvProcesses
            // 
            this.lvProcesses.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colName,
            this.colPid,
            this.colUser,
            this.colPath,
            this.colCpu,
            this.colMem,
            this.colRisk});
            this.lvProcesses.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lvProcesses.FullRowSelect = true;
            this.lvProcesses.GridLines = true;
            this.lvProcesses.HideSelection = false;
            this.lvProcesses.Location = new System.Drawing.Point(0, 0);
            this.lvProcesses.MultiSelect = false;
            this.lvProcesses.Name = "lvProcesses";
            this.lvProcesses.Size = new System.Drawing.Size(980, 380);
            this.lvProcesses.TabIndex = 0;
            this.lvProcesses.UseCompatibleStateImageBehavior = false;
            this.lvProcesses.View = System.Windows.Forms.View.Details;
            this.lvProcesses.SelectedIndexChanged += new System.EventHandler(this.LvProcesses_SelectedIndexChanged);
            // 
            // colName
            // 
            this.colName.Text = "Процесс";
            this.colName.Width = 160;
            // 
            // colPid
            // 
            this.colPid.Text = "PID";
            this.colPid.Width = 60;
            // 
            // colUser
            // 
            this.colUser.Text = "Пользователь";
            this.colUser.Width = 120;
            // 
            // colPath
            // 
            this.colPath.Text = "Путь";
            this.colPath.Width = 320;
            // 
            // colCpu
            // 
            this.colCpu.Text = "CPU, %";
            this.colCpu.Width = 70;
            // 
            // colMem
            // 
            this.colMem.Text = "Память, МБ";
            this.colMem.Width = 90;
            // 
            // colRisk
            // 
            this.colRisk.Text = "Риск";
            this.colRisk.Width = 90;
            // 
            // splitBottom
            // 
            this.splitBottom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitBottom.Location = new System.Drawing.Point(0, 0);
            this.splitBottom.Name = "splitBottom";
            // 
            // splitBottom.Panel1
            // 
            this.splitBottom.Panel1.Controls.Add(this.txtDetails);
            this.splitBottom.Panel1.Controls.Add(this.lblDetailsHeader);
            // 
            // splitBottom.Panel2
            // 
            this.splitBottom.Panel2.Controls.Add(this.txtLog);
            this.splitBottom.Panel2.Controls.Add(this.lblLogHeader);
            this.splitBottom.Size = new System.Drawing.Size(980, 163);
            this.splitBottom.SplitterDistance = 480;
            this.splitBottom.Panel1MinSize = 150;
            this.splitBottom.Panel2MinSize = 150;
            this.splitBottom.TabIndex = 0;
            // 
            // txtDetails
            // 
            this.txtDetails.BackColor = System.Drawing.SystemColors.Window;
            this.txtDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDetails.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtDetails.Location = new System.Drawing.Point(0, 22);
            this.txtDetails.Multiline = true;
            this.txtDetails.Name = "txtDetails";
            this.txtDetails.ReadOnly = true;
            this.txtDetails.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtDetails.Size = new System.Drawing.Size(480, 141);
            this.txtDetails.TabIndex = 1;
            // 
            // lblDetailsHeader
            // 
            this.lblDetailsHeader.BackColor = System.Drawing.SystemColors.ControlDark;
            this.lblDetailsHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDetailsHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblDetailsHeader.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lblDetailsHeader.Location = new System.Drawing.Point(0, 0);
            this.lblDetailsHeader.Name = "lblDetailsHeader";
            this.lblDetailsHeader.Size = new System.Drawing.Size(480, 22);
            this.lblDetailsHeader.TabIndex = 0;
            this.lblDetailsHeader.Text = " Подробности выбранного процесса";
            this.lblDetailsHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtLog
            // 
            this.txtLog.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(30)))), ((int)(((byte)(30)))));
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtLog.ForeColor = System.Drawing.Color.LightGreen;
            this.txtLog.Location = new System.Drawing.Point(0, 22);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(496, 141);
            this.txtLog.TabIndex = 1;
            // 
            // lblLogHeader
            // 
            this.lblLogHeader.BackColor = System.Drawing.SystemColors.ControlDark;
            this.lblLogHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblLogHeader.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblLogHeader.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lblLogHeader.Location = new System.Drawing.Point(0, 0);
            this.lblLogHeader.Name = "lblLogHeader";
            this.lblLogHeader.Size = new System.Drawing.Size(496, 22);
            this.lblLogHeader.TabIndex = 0;
            this.lblLogHeader.Text = " Журнал действий";
            this.lblLogHeader.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslStatus,
            this.tslCount,
            this.tslThreat});
            this.statusStrip1.Location = new System.Drawing.Point(0, 638);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(980, 22);
            this.statusStrip1.TabIndex = 4;
            // 
            // tslStatus
            // 
            this.tslStatus.Name = "tslStatus";
            this.tslStatus.Size = new System.Drawing.Size(800, 17);
            this.tslStatus.Spring = true;
            this.tslStatus.Text = "Готов";
            this.tslStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // tslCount
            // 
            this.tslCount.Name = "tslCount";
            this.tslCount.Size = new System.Drawing.Size(86, 17);
            this.tslCount.Text = "Процессов: 0";
            // 
            // tslThreat
            // 
            this.tslThreat.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.tslThreat.Name = "tslThreat";
            this.tslThreat.Size = new System.Drawing.Size(64, 17);
            this.tslThreat.Text = "Угроз: 0";
            // 
            // tmrRefresh
            // 
            this.tmrRefresh.Interval = 3000;
            this.tmrRefresh.Tick += new System.EventHandler(this.TmrRefresh_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(980, 660);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(820, 520);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Process Sanitizer";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.splitBottom.Panel1.ResumeLayout(false);
            this.splitBottom.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitBottom)).EndInit();
            this.splitBottom.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuFile;
        private System.Windows.Forms.ToolStripMenuItem menuExit;
        private System.Windows.Forms.ToolStripMenuItem menuActions;
        private System.Windows.Forms.ToolStripMenuItem menuScan;
        private System.Windows.Forms.ToolStripMenuItem menuKill;
        private System.Windows.Forms.ToolStripMenuItem menuQuarantine;
        private System.Windows.Forms.ToolStripMenuItem menuSettings;
        private System.Windows.Forms.ToolStripMenuItem menuAutoRefresh;
        private System.Windows.Forms.ToolStripMenuItem menuHelp;
        private System.Windows.Forms.ToolStripMenuItem menuAbout;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tsbScan;
        private System.Windows.Forms.ToolStripButton tsbRefresh;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton tsbKill;
        private System.Windows.Forms.ToolStripButton tsbQuarantine;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton tsbOpenPath;
        private System.Windows.Forms.ToolStripButton tsbWhitelist;
        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.CheckBox chkAutoRefresh;
        private System.Windows.Forms.TextBox txtSearch;
        private System.Windows.Forms.Label lblFilter;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.ListView lvProcesses;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colPid;
        private System.Windows.Forms.ColumnHeader colUser;
        private System.Windows.Forms.ColumnHeader colPath;
        private System.Windows.Forms.ColumnHeader colCpu;
        private System.Windows.Forms.ColumnHeader colMem;
        private System.Windows.Forms.ColumnHeader colRisk;
        private System.Windows.Forms.SplitContainer splitBottom;
        private System.Windows.Forms.TextBox txtDetails;
        private System.Windows.Forms.Label lblDetailsHeader;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Label lblLogHeader;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel tslStatus;
        private System.Windows.Forms.ToolStripStatusLabel tslCount;
        private System.Windows.Forms.ToolStripStatusLabel tslThreat;
        private System.Windows.Forms.Timer tmrRefresh;
    }
}