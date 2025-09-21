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
            this.Administration = new System.Windows.Forms.ToolStripMenuItem();
            this.klimadatenEinlesenToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gebäudeToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.wärmepumpenToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem_Stromspeicher = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.White;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Projekte,
            this.Administration,
            this.toolStripMenuItem_Stromspeicher});
            this.menuStrip1.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.Flow;
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(762, 27);
            this.menuStrip1.TabIndex = 1;
            this.menuStrip1.Text = "File";
            // 
            // Projekte
            // 
            this.Projekte.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.verwaltungToolStripMenuItem,
            this.öffnenToolStripMenuItem});
            this.Projekte.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Projekte.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Projekte.Name = "Projekte";
            this.Projekte.Size = new System.Drawing.Size(84, 23);
            this.Projekte.Tag = "Projekte";
            this.Projekte.Text = "Projekte";
            // 
            // verwaltungToolStripMenuItem
            // 
            this.verwaltungToolStripMenuItem.BackColor = System.Drawing.Color.Navy;
            this.verwaltungToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            this.verwaltungToolStripMenuItem.Name = "verwaltungToolStripMenuItem";
            this.verwaltungToolStripMenuItem.Size = new System.Drawing.Size(164, 24);
            this.verwaltungToolStripMenuItem.Text = "Verwaltung";
            this.verwaltungToolStripMenuItem.Click += new System.EventHandler(this.verwaltungToolStripMenuItem_Click);
            // 
            // öffnenToolStripMenuItem
            // 
            this.öffnenToolStripMenuItem.BackColor = System.Drawing.Color.Navy;
            this.öffnenToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            this.öffnenToolStripMenuItem.Name = "öffnenToolStripMenuItem";
            this.öffnenToolStripMenuItem.Size = new System.Drawing.Size(164, 24);
            this.öffnenToolStripMenuItem.Text = "Öffnen";
            this.öffnenToolStripMenuItem.Click += new System.EventHandler(this.öffnenToolStripMenuItem_Click);
            // 
            // Administration
            // 
            this.Administration.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.klimadatenEinlesenToolStripMenuItem,
            this.gebäudeToolStripMenuItem1,
            this.wärmepumpenToolStripMenuItem1});
            this.Administration.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Administration.ForeColor = System.Drawing.SystemColors.ControlText;
            this.Administration.Name = "Administration";
            this.Administration.Size = new System.Drawing.Size(132, 23);
            this.Administration.Text = "Administration";
            // 
            // klimadatenEinlesenToolStripMenuItem
            // 
            this.klimadatenEinlesenToolStripMenuItem.BackColor = System.Drawing.Color.Navy;
            this.klimadatenEinlesenToolStripMenuItem.ForeColor = System.Drawing.SystemColors.Control;
            this.klimadatenEinlesenToolStripMenuItem.Name = "klimadatenEinlesenToolStripMenuItem";
            this.klimadatenEinlesenToolStripMenuItem.Size = new System.Drawing.Size(232, 24);
            this.klimadatenEinlesenToolStripMenuItem.Text = "Klimadaten einlesen";
            this.klimadatenEinlesenToolStripMenuItem.Click += new System.EventHandler(this.klimadatenEinlesenToolStripMenuItem_Click);
            // 
            // gebäudeToolStripMenuItem1
            // 
            this.gebäudeToolStripMenuItem1.BackColor = System.Drawing.Color.Navy;
            this.gebäudeToolStripMenuItem1.ForeColor = System.Drawing.SystemColors.Control;
            this.gebäudeToolStripMenuItem1.Name = "gebäudeToolStripMenuItem1";
            this.gebäudeToolStripMenuItem1.Size = new System.Drawing.Size(232, 24);
            this.gebäudeToolStripMenuItem1.Text = "Gebäude";
            // 
            // wärmepumpenToolStripMenuItem1
            // 
            this.wärmepumpenToolStripMenuItem1.BackColor = System.Drawing.Color.Navy;
            this.wärmepumpenToolStripMenuItem1.ForeColor = System.Drawing.SystemColors.Control;
            this.wärmepumpenToolStripMenuItem1.Name = "wärmepumpenToolStripMenuItem1";
            this.wärmepumpenToolStripMenuItem1.Size = new System.Drawing.Size(232, 24);
            this.wärmepumpenToolStripMenuItem1.Text = "Wärmepumpen";
            this.wärmepumpenToolStripMenuItem1.Click += new System.EventHandler(this.wärmepumpenToolStripMenuItem1_Click);
            // 
            // toolStripMenuItem_Stromspeicher
            // 
            this.toolStripMenuItem_Stromspeicher.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.toolStripMenuItem_Stromspeicher.Name = "toolStripMenuItem_Stromspeicher";
            this.toolStripMenuItem_Stromspeicher.Size = new System.Drawing.Size(133, 23);
            this.toolStripMenuItem_Stromspeicher.Text = "Stromspeicher";
            this.toolStripMenuItem_Stromspeicher.Click += new System.EventHandler(this.toolStripMenuItem_Stromspeicher_Click);
            // 
            // MDIMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(762, 569);
            this.Controls.Add(this.menuStrip1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F);
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
        private System.Windows.Forms.ToolStripMenuItem verwaltungToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem öffnenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem Administration;
        private System.Windows.Forms.ToolStripMenuItem klimadatenEinlesenToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem gebäudeToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem wärmepumpenToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem_Stromspeicher;
    }
}

