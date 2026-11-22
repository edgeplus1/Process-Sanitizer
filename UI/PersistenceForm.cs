using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Process_Sanitizer.Core;

namespace Process_Sanitizer.UI
{
    /// <summary>
    /// Окно просмотра автозагрузок и закреплений. Строится кодом, без дизайнера.
    /// </summary>
    public class PersistenceForm : Form
    {
        private readonly ListView lv = new ListView();
        private readonly Label lblStatus = new Label();

        public PersistenceForm()
        {
            Text = "Автозагрузки и закрепления — Process Sanitizer";
            Size = new Size(1000, 560);
            MinimumSize = new Size(760, 400);
            StartPosition = FormStartPosition.CenterParent;

            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.HideSelection = false;
            lv.MultiSelect = false;
            lv.Columns.Add("Где", 230);
            lv.Columns.Add("Имя", 190);
            lv.Columns.Add("Команда", 420);
            lv.Columns.Add("Вердикт", 110);

            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Bottom;
            panel.Height = 46;
            panel.Padding = new Padding(8, 8, 8, 4);

            Button btnScan = new Button();
            btnScan.Text = "Пересканировать";
            btnScan.AutoSize = true;
            btnScan.Click += delegate { RefreshList(); };

            Button btnClose = new Button();
            btnClose.Text = "Закрыть";
            btnClose.AutoSize = true;
            btnClose.DialogResult = DialogResult.Cancel;

            lblStatus.AutoSize = true;
            lblStatus.Padding = new Padding(12, 9, 0, 0);

            panel.Controls.Add(btnScan);
            panel.Controls.Add(btnClose);
            panel.Controls.Add(lblStatus);
            CancelButton = btnClose;

            Controls.Add(lv);
            Controls.Add(panel);
            RefreshList();
        }

        private void RefreshList()
        {
            Cursor = Cursors.WaitCursor;
            List<PersistenceEntry> entries;
            try
            {
                entries = PersistenceScanner.Scan();
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                MessageBox.Show("Ошибка сканирования: " + ex.Message);
                return;
            }

            lv.BeginUpdate();
            lv.Items.Clear();
            int suspicious = 0;
            foreach (PersistenceEntry en in entries)
            {
                ListViewItem row = new ListViewItem(en.Location);
                row.SubItems.Add(en.Name);
                row.SubItems.Add(en.Command);
                row.SubItems.Add(en.Suspicious ? "ПРОВЕРИТЬ" : "ок");
                row.ToolTipText = en.Reason;
                if (en.Suspicious)
                {
                    row.BackColor = Color.FromArgb(255, 224, 200);
                    row.ForeColor = Color.FromArgb(150, 60, 0);
                    suspicious++;
                }
                lv.Items.Add(row);
            }
            lv.EndUpdate();
            lblStatus.Text = "Записей: " + entries.Count + "  |  подозрительных: " + suspicious;
            Cursor = Cursors.Default;
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PersistenceForm));
            this.SuspendLayout();
            // 
            // PersistenceForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "PersistenceForm";
            this.ResumeLayout(false);

        }
    }
}