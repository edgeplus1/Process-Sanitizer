using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Process_Sanitizer.Core;

namespace Process_Sanitizer.UI
{
    /// <summary>
    /// Окно управления базой хешей: чёрный и белый списки.
    /// </summary>
    public class HashDatabaseForm : Form
    {
        private readonly ListView lvBlack = new ListView();
        private readonly ListView lvWhite = new ListView();
        private readonly Label lblStatus = new Label();

        public HashDatabaseForm()
        {
            Text = "База хешей — Process Sanitizer";
            Size = new Size(1000, 520);
            MinimumSize = new Size(760, 380);
            StartPosition = FormStartPosition.CenterParent;

            SplitContainer split = new SplitContainer();
            split.Dock = DockStyle.Fill;
            split.Orientation = Orientation.Vertical;
            split.SplitterDistance = 480;

            Label hb = new Label();
            hb.Text = " Чёрный список — мгновенный CRITICAL";
            hb.Dock = DockStyle.Top;
            hb.Height = 22;
            hb.BackColor = Color.FromArgb(140, 0, 0);
            hb.ForeColor = Color.White;
            hb.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            hb.TextAlign = ContentAlignment.MiddleLeft;

            Label hw = new Label();
            hw.Text = " Белый список хешей — доверие даже после переименования";
            hw.Dock = DockStyle.Top;
            hw.Height = 22;
            hw.BackColor = Color.FromArgb(40, 110, 60);
            hw.ForeColor = Color.White;
            hw.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            hw.TextAlign = ContentAlignment.MiddleLeft;

            ConfigureList(lvBlack);
            ConfigureList(lvWhite);

            split.Panel1.Controls.Add(lvBlack);
            split.Panel1.Controls.Add(hb);
            split.Panel2.Controls.Add(lvWhite);
            split.Panel2.Controls.Add(hw);

            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Bottom;
            panel.Height = 46;
            panel.Padding = new Padding(8, 8, 8, 4);

            Button btnAddBlack = new Button();
            btnAddBlack.Text = "Добавить в чёрный...";
            btnAddBlack.AutoSize = true;
            btnAddBlack.Click += delegate { AddHash(true); };

            Button btnAddWhite = new Button();
            btnAddWhite.Text = "Добавить в белый...";
            btnAddWhite.AutoSize = true;
            btnAddWhite.Click += delegate { AddHash(false); };

            Button btnRemove = new Button();
            btnRemove.Text = "Удалить выбранное";
            btnRemove.AutoSize = true;
            btnRemove.Click += BtnRemove_Click;

            Button btnClose = new Button();
            btnClose.Text = "Закрыть";
            btnClose.AutoSize = true;
            btnClose.DialogResult = DialogResult.Cancel;

            lblStatus.AutoSize = true;
            lblStatus.Padding = new Padding(12, 9, 0, 0);

            panel.Controls.Add(btnAddBlack);
            panel.Controls.Add(btnAddWhite);
            panel.Controls.Add(btnRemove);
            panel.Controls.Add(btnClose);
            panel.Controls.Add(lblStatus);
            CancelButton = btnClose;

            Controls.Add(split);
            Controls.Add(panel);
            RefreshLists();
        }

        private static void ConfigureList(ListView lv)
        {
            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.HideSelection = false;
            lv.MultiSelect = false;
            lv.Columns.Add("SHA256", 430);
            lv.Columns.Add("Комментарий", 240);
        }

        private void RefreshLists()
        {
            Fill(lvBlack, HashDatabase.Black);
            Fill(lvWhite, HashDatabase.White);
            int b = 0, w = 0;
            foreach (KeyValuePair<string, string> kv in HashDatabase.Black) b++;
            foreach (KeyValuePair<string, string> kv in HashDatabase.White) w++;
            lblStatus.Text = "Чёрных: " + b + "  |  Белых: " + w;
        }

        private static void Fill(ListView lv, IEnumerable<KeyValuePair<string, string>> data)
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            foreach (KeyValuePair<string, string> kv in data)
            {
                ListViewItem row = new ListViewItem(kv.Key);
                row.SubItems.Add(kv.Value);
                row.Tag = kv.Key;
                lv.Items.Add(row);
            }
            lv.EndUpdate();
        }

        private void AddHash(bool black)
        {
            string input = InputDialog.Ask(
                black ? "Добавить в чёрный список" : "Добавить в белый список",
                "Введи SHA256 (64 hex-символа), затем пробел и комментарий:");
            if (input == null) return;

            string t = input.Trim();
            int sp = t.IndexOf(' ');
            string hash = sp < 0 ? t : t.Substring(0, sp);
            string comment = sp < 0 ? "" : t.Substring(sp + 1).Trim();

            if (!HashDatabase.IsValidHash(hash))
            {
                MessageBox.Show("Это не SHA256: нужно ровно 64 hex-символа.", "База хешей",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (black) HashDatabase.AddBlack(hash, comment);
            else HashDatabase.AddWhite(hash, comment);
            RefreshLists();
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (lvBlack.SelectedItems.Count > 0)
            {
                HashDatabase.RemoveBlack((string)lvBlack.SelectedItems[0].Tag);
                RefreshLists();
                return;
            }
            if (lvWhite.SelectedItems.Count > 0)
            {
                HashDatabase.RemoveWhite((string)lvWhite.SelectedItems[0].Tag);
                RefreshLists();
                return;
            }
            MessageBox.Show("Выбери запись в одном из списков.");
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HashDatabaseForm));
            this.SuspendLayout();
            // 
            // HashDatabaseForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "HashDatabaseForm";
            this.ResumeLayout(false);

        }
    }
}