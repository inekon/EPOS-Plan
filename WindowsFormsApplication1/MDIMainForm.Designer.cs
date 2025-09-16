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
            this.MenuItem_ProjektNeu = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_ProjektOeffnen = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_ProjektBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_zuletztGeöffnet = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_ProjektLöschen = new System.Windows.Forms.ToolStripMenuItem();
            this.Administration = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Klimadaten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WP = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WPBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.importierenVDI3805ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Stromspeicher = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Gebaeude = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_GebBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_GebTypen = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WaermebedarfExtern = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Prozesswaerme = new System.Windows.Forms.ToolStripMenuItem();
            this.stromTestToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.AliceBlue;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Projekte,
            this.Administration,
            this.stromTestToolStripMenuItem});
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
            this.MenuItem_ProjektNeu,
            this.MenuItem_ProjektOeffnen,
            this.MenuItem_ProjektBearbeiten,
            this.MenuItem_zuletztGeöffnet,
            this.MenuItem_ProjektLöschen});
            this.Projekte.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Projekte.Name = "Projekte";
            this.Projekte.Size = new System.Drawing.Size(71, 25);
            this.Projekte.Tag = "Projekte";
            this.Projekte.Text = "Projekt";
            // 
            // MenuItem_ProjektNeu
            // 
            this.MenuItem_ProjektNeu.Name = "MenuItem_ProjektNeu";
            this.MenuItem_ProjektNeu.Size = new System.Drawing.Size(187, 26);
            this.MenuItem_ProjektNeu.Text = "Neu...";
            this.MenuItem_ProjektNeu.Click += new System.EventHandler(this.MenuItem_Neu_Click);
            // 
            // MenuItem_ProjektOeffnen
            // 
            this.MenuItem_ProjektOeffnen.Name = "MenuItem_ProjektOeffnen";
            this.MenuItem_ProjektOeffnen.Size = new System.Drawing.Size(187, 26);
            this.MenuItem_ProjektOeffnen.Text = "Öffnen...";
            this.MenuItem_ProjektOeffnen.Click += new System.EventHandler(this.MenuItem_ProjektOeffnen_Click);
            // 
            // MenuItem_ProjektBearbeiten
            // 
            this.MenuItem_ProjektBearbeiten.Name = "MenuItem_ProjektBearbeiten";
            this.MenuItem_ProjektBearbeiten.Size = new System.Drawing.Size(187, 26);
            this.MenuItem_ProjektBearbeiten.Text = "Bearbeiten...";
            this.MenuItem_ProjektBearbeiten.Click += new System.EventHandler(this.MenuItem_ProjektBearbeiten_Click);
            // 
            // MenuItem_zuletztGeöffnet
            // 
            this.MenuItem_zuletztGeöffnet.Name = "MenuItem_zuletztGeöffnet";
            this.MenuItem_zuletztGeöffnet.Size = new System.Drawing.Size(187, 26);
            this.MenuItem_zuletztGeöffnet.Text = "zuletzt geöffnet";
            this.MenuItem_zuletztGeöffnet.Click += new System.EventHandler(this.MenuItem_zuletztGeöffnet_Click);
            // 
            // MenuItem_ProjektLöschen
            // 
            this.MenuItem_ProjektLöschen.Name = "MenuItem_ProjektLöschen";
            this.MenuItem_ProjektLöschen.Size = new System.Drawing.Size(187, 26);
            this.MenuItem_ProjektLöschen.Text = "Löschen...";
            this.MenuItem_ProjektLöschen.Click += new System.EventHandler(this.MenuItem_ProjektLöschen_Click);
            // 
            // Administration
            // 
            this.Administration.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Klimadaten,
            this.MenuItem_WP,
            this.MenuItem_Stromspeicher,
            this.MenuItem_Gebaeude,
            this.MenuItem_WaermebedarfExtern,
            this.MenuItem_Prozesswaerme});
            this.Administration.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Administration.Name = "Administration";
            this.Administration.Size = new System.Drawing.Size(125, 25);
            this.Administration.Text = "Administration";
            // 
            // MenuItem_Klimadaten
            // 
            this.MenuItem_Klimadaten.Name = "MenuItem_Klimadaten";
            this.MenuItem_Klimadaten.Size = new System.Drawing.Size(222, 26);
            this.MenuItem_Klimadaten.Text = "Klimadaten";
            this.MenuItem_Klimadaten.Click += new System.EventHandler(this.MenuItem_Klimadaten_Click);
            // 
            // MenuItem_WP
            // 
            this.MenuItem_WP.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_WPBearbeiten,
            this.importierenVDI3805ToolStripMenuItem});
            this.MenuItem_WP.Name = "MenuItem_WP";
            this.MenuItem_WP.Size = new System.Drawing.Size(222, 26);
            this.MenuItem_WP.Text = "Wärmepumpen";
            // 
            // MenuItem_WPBearbeiten
            // 
            this.MenuItem_WPBearbeiten.Name = "MenuItem_WPBearbeiten";
            this.MenuItem_WPBearbeiten.Size = new System.Drawing.Size(231, 26);
            this.MenuItem_WPBearbeiten.Text = "Bearbeiten";
            this.MenuItem_WPBearbeiten.Click += new System.EventHandler(this.MenuItem_WPBearbeiten_Click_1);
            // 
            // importierenVDI3805ToolStripMenuItem
            // 
            this.importierenVDI3805ToolStripMenuItem.Name = "importierenVDI3805ToolStripMenuItem";
            this.importierenVDI3805ToolStripMenuItem.Size = new System.Drawing.Size(231, 26);
            this.importierenVDI3805ToolStripMenuItem.Text = "Importieren VDI 3805";
            // 
            // MenuItem_Stromspeicher
            // 
            this.MenuItem_Stromspeicher.Name = "MenuItem_Stromspeicher";
            this.MenuItem_Stromspeicher.Size = new System.Drawing.Size(222, 26);
            this.MenuItem_Stromspeicher.Text = "Stromspeicher";
            this.MenuItem_Stromspeicher.Click += new System.EventHandler(this.MenuItem_Stromspeicher_Click);
            // 
            // MenuItem_Gebaeude
            // 
            this.MenuItem_Gebaeude.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_GebBearbeiten,
            this.MenuItem_GebTypen});
            this.MenuItem_Gebaeude.Name = "MenuItem_Gebaeude";
            this.MenuItem_Gebaeude.Size = new System.Drawing.Size(222, 26);
            this.MenuItem_Gebaeude.Text = "Gebäude";
            // 
            // MenuItem_GebBearbeiten
            // 
            this.MenuItem_GebBearbeiten.Name = "MenuItem_GebBearbeiten";
            this.MenuItem_GebBearbeiten.Size = new System.Drawing.Size(181, 26);
            this.MenuItem_GebBearbeiten.Text = "Bearbeiten";
            this.MenuItem_GebBearbeiten.Click += new System.EventHandler(this.MenuItem_GebBearbeiten_Click);
            // 
            // MenuItem_GebTypen
            // 
            this.MenuItem_GebTypen.Name = "MenuItem_GebTypen";
            this.MenuItem_GebTypen.Size = new System.Drawing.Size(181, 26);
            this.MenuItem_GebTypen.Text = "Gebäudetypen";
            this.MenuItem_GebTypen.Click += new System.EventHandler(this.MenuItem_GebTypen_Click);
            // 
            // MenuItem_WaermebedarfExtern
            // 
            this.MenuItem_WaermebedarfExtern.Name = "MenuItem_WaermebedarfExtern";
            this.MenuItem_WaermebedarfExtern.Size = new System.Drawing.Size(222, 26);
            this.MenuItem_WaermebedarfExtern.Text = "Wärmebedarf extern";
            this.MenuItem_WaermebedarfExtern.Click += new System.EventHandler(this.MenuItem_WaermebedarfExtern_Click);
            // 
            // MenuItem_Prozesswaerme
            // 
            this.MenuItem_Prozesswaerme.Name = "MenuItem_Prozesswaerme";
            this.MenuItem_Prozesswaerme.Size = new System.Drawing.Size(222, 26);
            this.MenuItem_Prozesswaerme.Text = "Prozesswärme";
            this.MenuItem_Prozesswaerme.Click += new System.EventHandler(this.MenuItem_Prozesswaerme_Click);
            // 
            // stromTestToolStripMenuItem
            // 
            this.stromTestToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.stromTestToolStripMenuItem.Name = "stromTestToolStripMenuItem";
            this.stromTestToolStripMenuItem.Size = new System.Drawing.Size(90, 25);
            this.stromTestToolStripMenuItem.Text = "StromTest";
            this.stromTestToolStripMenuItem.Click += new System.EventHandler(this.stromTestToolStripMenuItem_Click);
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
            this.Text = "WP-Plan";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.MDIMainForm_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem Projekte;
        private System.Windows.Forms.ToolStripMenuItem Administration;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_ProjektNeu;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_ProjektOeffnen;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_ProjektBearbeiten;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_zuletztGeöffnet;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Klimadaten;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_ProjektLöschen;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_WP;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_WPBearbeiten;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Stromspeicher;
        private System.Windows.Forms.ToolStripMenuItem importierenVDI3805ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Gebaeude;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_GebBearbeiten;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_GebTypen;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_WaermebedarfExtern;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Prozesswaerme;
        private System.Windows.Forms.ToolStripMenuItem stromTestToolStripMenuItem;
    }
}

