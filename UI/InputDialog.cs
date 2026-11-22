using System;
using System.Drawing;
using System.Windows.Forms;

namespace Process_Sanitizer.UI
{
    /// <summary>
    /// Маленький диалог ввода строки.
    /// </summary>
    public class InputDialog : Form
    {
        private readonly TextBox txt = new TextBox();

        public string Value { get { return txt.Text; } }

        public InputDialog(string title, string prompt)
        {
            Text = title;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(560, 112);

            Label lbl = new Label();
            lbl.Text = prompt;
            lbl.Dock = DockStyle.Top;
            lbl.Height = 34;
            lbl.Padding = new Padding(8, 8, 8, 0);

            txt.Dock = DockStyle.Top;
            txt.Height = 24;

            FlowLayoutPanel panel = new FlowLayoutPanel();
            panel.Dock = DockStyle.Bottom;
            panel.Height = 42;
            panel.FlowDirection = FlowDirection.RightToLeft;
            panel.Padding = new Padding(8, 6, 8, 4);

            Button ok = new Button();
            ok.Text = "ОК";
            ok.AutoSize = true;
            ok.DialogResult = DialogResult.OK;

            Button cancel = new Button();
            cancel.Text = "Отмена";
            cancel.AutoSize = true;
            cancel.DialogResult = DialogResult.Cancel;

            panel.Controls.Add(ok);
            panel.Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;

            Controls.Add(txt);
            Controls.Add(lbl);
            Controls.Add(panel);
        }

        public static string Ask(string title, string prompt)
        {
            using (InputDialog d = new InputDialog(title, prompt))
            {
                return d.ShowDialog() == DialogResult.OK ? d.Value : null;
            }
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InputDialog));
            this.SuspendLayout();
            // 
            // InputDialog
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "InputDialog";
            this.ResumeLayout(false);

        }
    }
}