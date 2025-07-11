namespace WindowsFormsApplication1
{
    partial class MDIMainForm
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.Projekte = new System.Windows.Forms.ToolStripMenuItem();
            this.verwaltungToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.öffnenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.Fenster = new System.Windows.Forms.ToolStripMenuItem();
            this.WP = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.AliceBlue;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Projekte,
            this.Fenster,
            this.WP});
            this.menuStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(762, 29);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "File";
            // 
            // Projekte
            // 
            this.Projekte.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.verwaltungToolStripMenuItem,
            this.öffnenToolStripMenuItem});
            this.Projekte.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Projekte.Name = "Projekte";
            this.Projekte.Size = new System.Drawing.Size(79, 25);
            this.Projekte.Tag = "Projekte";
            this.Projekte.Text = "Projekte";
            // 
            // verwaltungToolStripMenuItem
            // 
            this.verwaltungToolStripMenuItem.Name = "verwaltungToolStripMenuItem";
            this.verwaltungToolStripMenuItem.Size = new System.Drawing.Size(159, 26);
            this.verwaltungToolStripMenuItem.Text = "Verwaltung";
            this.verwaltungToolStripMenuItem.Click += new System.EventHandler(this.verwaltungToolStripMenuItem_Click);
            // 
            // öffnenToolStripMenuItem
            // 
            this.öffnenToolStripMenuItem.Name = "öffnenToolStripMenuItem";
            this.öffnenToolStripMenuItem.Size = new System.Drawing.Size(159, 26);
            this.öffnenToolStripMenuItem.Text = "Öffnen";
            this.öffnenToolStripMenuItem.Click += new System.EventHandler(this.öffnenToolStripMenuItem_Click);
            // 
            // Fenster
            // 
            this.Fenster.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Fenster.Name = "Fenster";
            this.Fenster.Size = new System.Drawing.Size(84, 25);
            this.Fenster.Text = "Gebäude";
            // 
            // WP
            // 
            this.WP.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.WP.Name = "WP";
            this.WP.Size = new System.Drawing.Size(46, 25);
            this.WP.Text = "WP";
            this.WP.Click += new System.EventHandler(this.WP_Click);
            // 
            // MDIMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(762, 569);
            this.Controls.Add(this.menuStrip1);
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MDIMainForm";
            this.Text = "WP-Wärmeplan";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MDIMainForm_FormClosing);
            this.Load += new System.EventHandler(this.MDIMainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem Projekte;
        private System.Windows.Forms.ToolStripMenuItem Fenster;
        private System.Windows.Forms.ToolStripMenuItem WP;
        private System.Windows.Forms.ToolStripMenuItem verwaltungToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem öffnenToolStripMenuItem;
    }
}

