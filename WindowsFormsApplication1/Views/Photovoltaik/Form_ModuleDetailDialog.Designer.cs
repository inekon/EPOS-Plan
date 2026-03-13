using System.Drawing;

namespace WindowsFormsApplication1
{
    partial class Form_ModuleDetailDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.header = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.scroll = new System.Windows.Forms.Panel(); // Standard statt ScrollablePanel
            this.inner = new System.Windows.Forms.FlowLayoutPanel();
            this.footer = new System.Windows.Forms.Panel();
            this.btnOk = new System.Windows.Forms.Button();

            this.SuspendLayout();

            // header
            this.header.Dock = System.Windows.Forms.DockStyle.Top;
            this.header.Height = 72;
            this.header.BackColor = Color.FromArgb(30, 87, 153);
            this.header.Paint += new System.Windows.Forms.PaintEventHandler(this.Header_Paint);

            // btnClose
            this.btnClose.Text = "✕";
            this.btnClose.Size = new System.Drawing.Size(32, 32);
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Font = new System.Drawing.Font("Segoe UI", 10f, System.Drawing.FontStyle.Bold);
            this.btnClose.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnClose.FlatAppearance.BorderSize = 0;

            // scroll
            this.scroll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.scroll.BackColor = Color.FromArgb(236, 242, 250);
            this.scroll.Padding = new System.Windows.Forms.Padding(14, 12, 14, 14);
            this.scroll.AutoScroll = true;

            // inner
            this.inner.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.inner.WrapContents = false;
            this.inner.AutoSize = true;
            this.inner.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;

            // footer
            this.footer.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.footer.Height = 44;
            this.footer.BackColor = System.Drawing.Color.FromArgb(225, 232, 242);
            this.footer.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);

            // btnOk
            this.btnOk.Text = "Schließen";
            this.btnOk.Size = new System.Drawing.Size(100, 30);
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOk.BackColor = Color.FromArgb(30, 87, 153);
            this.btnOk.ForeColor = System.Drawing.Color.White;
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Cursor = System.Windows.Forms.Cursors.Hand;

            // Form
            this.ClientSize = new System.Drawing.Size(920, 700);
            this.MinimumSize = new System.Drawing.Size(800, 600);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.BackColor = Color.FromArgb(236, 242, 250);
            this.Text = "Moduldetails";
            this.KeyPreview = true;

            this.Controls.Add(this.scroll);
            this.Controls.Add(this.header);
            this.Controls.Add(this.footer);

            this.header.Controls.Add(this.btnClose);
            this.scroll.Controls.Add(this.inner);
            this.footer.Controls.Add(this.btnOk);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Panel header;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Panel scroll;
        private System.Windows.Forms.FlowLayoutPanel inner;
        private System.Windows.Forms.Panel footer;
        private System.Windows.Forms.Button btnOk;
    }
}