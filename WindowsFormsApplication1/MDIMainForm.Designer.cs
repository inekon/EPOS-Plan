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
            this.MenuItem_Gebaeude = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_GebBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_GebTypen = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Kessel = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Klimadaten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Prozesswaerme = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Stromspeicher = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Stromverbraucher = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Stromganglinie = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WP = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WPBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MeniItem_VDI3805 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WaermebedarfExtern = new System.Windows.Forms.ToolStripMenuItem();
            this.testToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_BHKW = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.AliceBlue;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Projekte,
            this.Administration,
            this.testToolStripMenuItem});
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
            this.MenuItem_BHKW,
            this.MenuItem_Gebaeude,
            this.MenuItem_Kessel,
            this.MenuItem_Klimadaten,
            this.MenuItem_Prozesswaerme,
            this.MenuItem_Stromspeicher,
            this.MenuItem_Stromverbraucher,
            this.MenuItem_Stromganglinie,
            this.MenuItem_WP,
            this.MenuItem_WaermebedarfExtern});
            this.Administration.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Administration.Name = "Administration";
            this.Administration.Size = new System.Drawing.Size(125, 25);
            this.Administration.Text = "Administration";
            // 
            // MenuItem_Gebaeude
            // 
            this.MenuItem_Gebaeude.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_GebBearbeiten,
            this.MenuItem_GebTypen});
            this.MenuItem_Gebaeude.Name = "MenuItem_Gebaeude";
            this.MenuItem_Gebaeude.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_Gebaeude.Text = "Gebäude";
            this.MenuItem_Gebaeude.Click += new System.EventHandler(this.MenuItem_Gebaeude_Click);
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
            // MenuItem_Kessel
            // 
            this.MenuItem_Kessel.Name = "MenuItem_Kessel";
            this.MenuItem_Kessel.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_Kessel.Text = "Heizkessel";
            this.MenuItem_Kessel.Click += new System.EventHandler(this.MenuItem_Kessel_Click);
            // 
            // MenuItem_Klimadaten
            // 
            this.MenuItem_Klimadaten.Name = "MenuItem_Klimadaten";
            this.MenuItem_Klimadaten.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_Klimadaten.Text = "Klimadaten";
            this.MenuItem_Klimadaten.Click += new System.EventHandler(this.MenuItem_Klimadaten_Click);
            // 
            // MenuItem_Prozesswaerme
            // 
            this.MenuItem_Prozesswaerme.Name = "MenuItem_Prozesswaerme";
            this.MenuItem_Prozesswaerme.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_Prozesswaerme.Text = "Prozesswärme";
            this.MenuItem_Prozesswaerme.Click += new System.EventHandler(this.MenuItem_Prozesswaerme_Click);
            // 
            // MenuItem_Stromspeicher
            // 
            this.MenuItem_Stromspeicher.Name = "MenuItem_Stromspeicher";
            this.MenuItem_Stromspeicher.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_Stromspeicher.Text = "Stromspeicher";
            this.MenuItem_Stromspeicher.Click += new System.EventHandler(this.MenuItem_Stromspeicher_Click);
            // 
            // MenuItem_Stromverbraucher
            // 
            this.MenuItem_Stromverbraucher.Name = "MenuItem_Stromverbraucher";
            this.MenuItem_Stromverbraucher.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_Stromverbraucher.Text = "Stromverbraucher";
            this.MenuItem_Stromverbraucher.Click += new System.EventHandler(this.MenuItem_Stromverbraucher_Click);
            // 
            // MenuItem_Stromganglinie
            // 
            this.MenuItem_Stromganglinie.Name = "MenuItem_Stromganglinie";
            this.MenuItem_Stromganglinie.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_Stromganglinie.Text = "Stromganglinie";
            this.MenuItem_Stromganglinie.Click += new System.EventHandler(this.MenuItem_Stromganglinie_Click);
            // 
            // MenuItem_WP
            // 
            this.MenuItem_WP.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_WPBearbeiten,
            this.MeniItem_VDI3805});
            this.MenuItem_WP.Name = "MenuItem_WP";
            this.MenuItem_WP.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_WP.Text = "Wärmepumpen";
            // 
            // MenuItem_WPBearbeiten
            // 
            this.MenuItem_WPBearbeiten.Name = "MenuItem_WPBearbeiten";
            this.MenuItem_WPBearbeiten.Size = new System.Drawing.Size(231, 26);
            this.MenuItem_WPBearbeiten.Text = "Bearbeiten";
            this.MenuItem_WPBearbeiten.Click += new System.EventHandler(this.MenuItem_WPBearbeiten_Click_1);
            // 
            // MeniItem_VDI3805
            // 
            this.MeniItem_VDI3805.Name = "MeniItem_VDI3805";
            this.MeniItem_VDI3805.Size = new System.Drawing.Size(231, 26);
            this.MeniItem_VDI3805.Text = "Importieren VDI 3805";
            this.MeniItem_VDI3805.Click += new System.EventHandler(this.MeniItem_VDI3805_Click);
            // 
            // MenuItem_WaermebedarfExtern
            // 
            this.MenuItem_WaermebedarfExtern.Name = "MenuItem_WaermebedarfExtern";
            this.MenuItem_WaermebedarfExtern.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_WaermebedarfExtern.Text = "Wärmebedarf Lastgang";
            this.MenuItem_WaermebedarfExtern.Click += new System.EventHandler(this.MenuItem_WaermebedarfExtern_Click);
            // 
            // testToolStripMenuItem
            // 
            this.testToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.testToolStripMenuItem.Name = "testToolStripMenuItem";
            this.testToolStripMenuItem.Size = new System.Drawing.Size(48, 25);
            this.testToolStripMenuItem.Text = "Test";
            this.testToolStripMenuItem.Click += new System.EventHandler(this.testToolStripMenuItem_Click);
            // 
            // MenuItem_BHKW
            // 
            this.MenuItem_BHKW.Name = "MenuItem_BHKW";
            this.MenuItem_BHKW.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_BHKW.Text = "BHKW";
            this.MenuItem_BHKW.Click += new System.EventHandler(this.MenuItem_BHKW_Click);
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
        private System.Windows.Forms.ToolStripMenuItem MeniItem_VDI3805;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Gebaeude;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_GebBearbeiten;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_GebTypen;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_WaermebedarfExtern;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Prozesswaerme;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Stromverbraucher;
        private System.Windows.Forms.ToolStripMenuItem testToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Stromganglinie;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Kessel;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_BHKW;
    }
}

