using System;
using System.Drawing;
using System.Windows.Forms;
using Process_Sanitizer.Core;

namespace Process_Sanitizer.UI
{
    /// <summary>
    /// Окно встроенной БД угроз: просмотр, эталоны, пополнение.
    /// </summary>
    public class ThreatDbForm : Form
    {
        private readonly ListView lv = new ListView();
        private readonly Label lblStatus = new Label();

        public ThreatDbForm()
        {
            Text = "База угроз (БД) — Process Sanitizer";
            Size = new Size(1040, 560);
            MinimumSize = new Size(800, 400);
            StartPosition = FormStartPosition.CenterParent;

            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.HideSelection = false;
            lv.MultiSelect = false;
            lv.Columns.Add("Тип", 60);
            lv.Columns.Add("SHA256", 430);
            lv.Columns.Add("Метка", 160);
            lv.Columns.Add("Комментарий", 240);
            lv.Columns.Add("Источник", 90);

            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Bottom;
            panel.Height = 46;
            panel.Padding = new Padding(8, 8, 8, 4);

            Button btnBaseline = new Button();
            btnBaseline.Text = "Снять эталоны системы (GOOD)";
            btnBaseline.AutoSize = true;
            btnBaseline.Click += BtnBaseline_Click;

            Button btnAddBad = new Button();
            btnAddBad.Text = "Добавить BAD...";
            btnAddBad.AutoSize = true;
            btnAddBad.Click += delegate { AddRecord(true); };

            Button btnAddGood = new Button();
            btnAddGood.Text = "Добавить GOOD...";
            btnAddGood.AutoSize = true;
            btnAddGood.Click += delegate { AddRecord(false); };

            Button btnRemove = new Button();
            btnRemove.Text = "Удалить локальную запись";
            btnRemove.AutoSize = true;
            btnRemove.Click += BtnRemove_Click;

            Button btnClose = new Button();
            btnClose.Text = "Закрыть";
            btnClose.AutoSize = true;
            btnClose.DialogResult = DialogResult.Cancel;

            lblStatus.AutoSize = true;
            lblStatus.Padding = new Padding(12, 9, 0, 0);

            panel.Controls.Add(btnBaseline);
            panel.Controls.Add(btnAddBad);
            panel.Controls.Add(btnAddGood);
            panel.Controls.Add(btnRemove);
            panel.Controls.Add(btnClose);
            panel.Controls.Add(lblStatus);
            CancelButton = btnClose;

            Controls.Add(lv);
            Controls.Add(panel);
            RefreshList();
        }

        private void RefreshList()
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            foreach (ThreatRecord r in ThreatDatabase.Records)
            {
                ListViewItem row = new ListViewItem(r.Kind);
                row.SubItems.Add(r.Hash);
                row.SubItems.Add(r.Label);
                row.SubItems.Add(r.Comment);
                row.SubItems.Add(r.Local ? "локально" : "встроено");
                row.Tag = r.Hash;
                if (r.Kind == "BAD")
                {
                    row.BackColor = Color.FromArgb(255, 205, 210);
                    row.ForeColor = Color.FromArgb(140, 0, 0);
                }
                else
                {
                    row.BackColor = Color.FromArgb(226, 244, 226);
                    row.ForeColor = Color.FromArgb(30, 100, 50);
                }
                lv.Items.Add(row);
            }
            lv.EndUpdate();
            lblStatus.Text = "BAD: " + ThreatDatabase.BadCount + "  |  GOOD: " + ThreatDatabase.GoodCount +
                             "  |  оверлей: " + ThreatDatabase.LocalPath;
        }

        private void BtnBaseline_Click(object sender, EventArgs e)
        {
            DialogResult dr = MessageBox.Show(
                "Снять эталонные хеши системных файлов этой машины?\n\n" +
                "ВАЖНО: делай это только на ЗАВЕДОМО ЧИСТОЙ системе.\n" +
                "Эталоны попадут в локальный оверлей и будут давать доверие по хешу.",
                "Снятие эталонов", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes) return;

            Cursor = Cursors.WaitCursor;
            int added = ThreatDatabase.CaptureBaseline();
            Cursor = Cursors.Default;
            RefreshList();
            MessageBox.Show("Добавлено эталонов: " + added, "БД угроз",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void AddRecord(bool bad)
        {
            string input = InputDialog.Ask(
                bad ? "Добавить BAD-хеш" : "Добавить GOOD-хеш",
                "Формат: sha256|метка|комментарий   (например: abc...|XMRig|майнер из инцидента)");
            if (input == null) return;
            string[] p = input.Trim().Split('|');
            if (p.Length < 2 || !HashDatabase.IsValidHash(p[0].Trim()))
            {
                MessageBox.Show("Нужен корректный SHA256 (64 hex) и метка через |.", "БД угроз",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string hash = p[0].Trim();
            string label = p[1].Trim();
            string comment = p.Length > 2 ? p[2].Trim() : "";
            if (bad) ThreatDatabase.AddLocalBad(hash, label, comment);
            else ThreatDatabase.AddLocalGood(hash, label, comment);
            RefreshList();
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (lv.SelectedItems.Count == 0) { MessageBox.Show("Выбери запись."); return; }
            string hash = (string)lv.SelectedItems[0].Tag;
            ThreatDatabase.RemoveLocal(hash);
            RefreshList();
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ThreatDbForm));
            this.SuspendLayout();
            // 
            // ThreatDbForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "ThreatDbForm";
            this.ResumeLayout(false);

        }
    }
}