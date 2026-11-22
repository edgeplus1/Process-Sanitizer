using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Process_Sanitizer.Core;

namespace Process_Sanitizer.UI
{
    /// <summary>
    /// Окно управления карантином. Строится кодом, без дизайнера.
    /// </summary>
    public class QuarantineForm : Form
    {
        private readonly ListView lv = new ListView();
        private readonly Label lblStatus = new Label();

        public QuarantineForm()
        {
            Text = "Карантин — Process Sanitizer";
            Size = new Size(960, 520);
            MinimumSize = new Size(720, 380);
            StartPosition = FormStartPosition.CenterParent;

            lv.Dock = DockStyle.Fill;
            lv.View = View.Details;
            lv.FullRowSelect = true;
            lv.GridLines = true;
            lv.HideSelection = false;
            lv.MultiSelect = false;
            lv.Columns.Add("ID", 200);
            lv.Columns.Add("Исходный путь", 330);
            lv.Columns.Add("SHA256", 130);
            lv.Columns.Add("Помещён", 140);
            lv.Columns.Add("Размер, КБ", 90);
            lv.Columns.Add("Риск", 80);

            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Bottom;
            panel.Height = 46;
            panel.Padding = new Padding(8, 8, 8, 4);

            Button btnRefresh = new Button();
            btnRefresh.Text = "Обновить";
            btnRefresh.AutoSize = true;
            btnRefresh.Click += delegate { RefreshList(); };

            Button btnRestore = new Button();
            btnRestore.Text = "Восстановить";
            btnRestore.AutoSize = true;
            btnRestore.Click += BtnRestore_Click;

            Button btnDelete = new Button();
            btnDelete.Text = "Удалить навсегда";
            btnDelete.AutoSize = true;
            btnDelete.Click += BtnDelete_Click;

            Button btnClose = new Button();
            btnClose.Text = "Закрыть";
            btnClose.AutoSize = true;
            btnClose.DialogResult = DialogResult.Cancel;

            lblStatus.AutoSize = true;
            lblStatus.Padding = new Padding(12, 9, 0, 0);

            panel.Controls.Add(btnRefresh);
            panel.Controls.Add(btnRestore);
            panel.Controls.Add(btnDelete);
            panel.Controls.Add(btnClose);
            panel.Controls.Add(lblStatus);
            CancelButton = btnClose;

            Controls.Add(lv);
            Controls.Add(panel);
            RefreshList();
        }

        private string SelectedId()
        {
            if (lv.SelectedItems.Count == 0) return null;
            return lv.SelectedItems[0].Tag as string;
        }

        private void RefreshList()
        {
            List<QuarantineItem> items = Quarantine.List();
            lv.BeginUpdate();
            lv.Items.Clear();
            foreach (QuarantineItem it in items)
            {
                ListViewItem row = new ListViewItem(it.Id);
                row.SubItems.Add(it.OriginalPath);
                row.SubItems.Add(it.Sha256.Substring(0, Math.Min(16, it.Sha256.Length)) + "…");
                row.SubItems.Add(it.AddedAt.ToString("yyyy-MM-dd HH:mm:ss"));
                row.SubItems.Add((it.Size / 1024).ToString());
                row.SubItems.Add(it.Risk);
                row.Tag = it.Id;
                lv.Items.Add(row);
            }
            lv.EndUpdate();
            lblStatus.Text = "Объектов: " + items.Count + "   (" + Quarantine.Dir + ")";
        }

        private void BtnRestore_Click(object sender, EventArgs e)
        {
            string id = SelectedId();
            if (id == null) { MessageBox.Show("Выбери объект в списке."); return; }
            string error;
            if (Quarantine.Restore(id, out error))
            {
                MessageBox.Show("Файл восстановлен по исходному пути.", "Карантин",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshList();
            }
            else
            {
                MessageBox.Show("Не удалось восстановить: " + error, "Карантин",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDelete_Click(object sender, EventArgs e)
        {
            string id = SelectedId();
            if (id == null) { MessageBox.Show("Выбери объект в списке."); return; }
            DialogResult dr = MessageBox.Show(
                "Удалить объект карантина НАВСЕГДА?\n\n" + id + "\n\nОтменить это действие будет нельзя.",
                "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (dr != DialogResult.Yes) return;

            string error;
            if (Quarantine.DeletePermanent(id, out error))
                RefreshList();
            else
                MessageBox.Show("Не удалось удалить: " + error, "Карантин",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(QuarantineForm));
            this.SuspendLayout();
            // 
            // QuarantineForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "QuarantineForm";
            this.ResumeLayout(false);

        }
    }
}