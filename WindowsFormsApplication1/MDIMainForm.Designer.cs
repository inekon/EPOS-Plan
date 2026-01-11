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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MDIMainForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.Projekte = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_ProjektNeu = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_ProjektOeffnen = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_ProjektBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_zuletztGeöffnet = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_ProjektLöschen = new System.Windows.Forms.ToolStripMenuItem();
            this.Administration = new System.Windows.Forms.ToolStripMenuItem();
            this.Help = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Version = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_Lizenz = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WaermebedarfExtern = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Prozesswaerme = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WP = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WPBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MeniItem_VDI3805 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Kessel = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem8 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_PV = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Solarkollektoren = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_SolThermGanglinie = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_BHKW = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Klimadaten = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Update = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Gebaeude = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_GebBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_GebTypen = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackColor = System.Drawing.Color.AliceBlue;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Projekte,
            this.Administration,
            this.Help});
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
            this.Administration.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.Administration.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem6,
            this.toolStripMenuItem7,
            this.MenuItem_Gebaeude});
            this.Administration.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Administration.Name = "Administration";
            this.Administration.Size = new System.Drawing.Size(125, 25);
            this.Administration.Text = "Administration";
            // 
            // Help
            // 
            this.Help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Version,
            this.toolStripSeparator7,
            this.MenuItem_Lizenz});
            this.Help.Font = new System.Drawing.Font("Segoe UI", 12F);
            this.Help.Name = "Help";
            this.Help.Size = new System.Drawing.Size(54, 25);
            this.Help.Text = "Hilfe";
            // 
            // MenuItem_Version
            // 
            this.MenuItem_Version.BackColor = System.Drawing.SystemColors.Control;
            this.MenuItem_Version.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.MenuItem_Version.Name = "MenuItem_Version";
            this.MenuItem_Version.Size = new System.Drawing.Size(132, 26);
            this.MenuItem_Version.Text = "Version";
            this.MenuItem_Version.TextImageRelation = System.Windows.Forms.TextImageRelation.TextBeforeImage;
            this.MenuItem_Version.Click += new System.EventHandler(this.MenuItem_Version_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            this.toolStripSeparator7.Size = new System.Drawing.Size(129, 6);
            // 
            // MenuItem_Lizenz
            // 
            this.MenuItem_Lizenz.Name = "MenuItem_Lizenz";
            this.MenuItem_Lizenz.Size = new System.Drawing.Size(132, 26);
            this.MenuItem_Lizenz.Text = "Lizenz";
            this.MenuItem_Lizenz.Click += new System.EventHandler(this.MenuItem_Lizenz_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.BackColor = System.Drawing.SystemColors.Control;
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_WaermebedarfExtern,
            this.MenuItem_Prozesswaerme,
            this.MenuItem_WP,
            this.MenuItem_Kessel});
            this.toolStripMenuItem1.Image = global::WindowsFormsApplication1.Properties.Resources.Menu1;
            this.toolStripMenuItem1.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripMenuItem1.ImageTransparentColor = System.Drawing.SystemColors.Control;
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(273, 38);
            this.toolStripMenuItem1.Text = "Wärmebedarf && Heizung ";
            // 
            // MenuItem_WaermebedarfExtern
            // 
            this.MenuItem_WaermebedarfExtern.Name = "MenuItem_WaermebedarfExtern";
            this.MenuItem_WaermebedarfExtern.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_WaermebedarfExtern.Text = "Wärmebedarf Lastgang";
            this.MenuItem_WaermebedarfExtern.Click += new System.EventHandler(this.MenuItem_WaermebedarfExtern_Click);
            // 
            // MenuItem_Prozesswaerme
            // 
            this.MenuItem_Prozesswaerme.Name = "MenuItem_Prozesswaerme";
            this.MenuItem_Prozesswaerme.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_Prozesswaerme.Text = "Prozesswärme";
            this.MenuItem_Prozesswaerme.Click += new System.EventHandler(this.MenuItem_Prozesswaerme_Click);
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
            // MenuItem_Kessel
            // 
            this.MenuItem_Kessel.Name = "MenuItem_Kessel";
            this.MenuItem_Kessel.Size = new System.Drawing.Size(242, 26);
            this.MenuItem_Kessel.Text = "Heizkessel";
            this.MenuItem_Kessel.Click += new System.EventHandler(this.MenuItem_Kessel_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.BackColor = System.Drawing.SystemColors.Control;
            this.toolStripMenuItem2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem5,
            this.toolStripMenuItem4,
            this.toolStripMenuItem8});
            this.toolStripMenuItem2.Image = global::WindowsFormsApplication1.Properties.Resources.Menue2;
            this.toolStripMenuItem2.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(273, 38);
            this.toolStripMenuItem2.Text = "Strombedarf && Speicher";
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(206, 26);
            this.toolStripMenuItem5.Text = "Stromverbraucher";
            this.toolStripMenuItem5.Click += new System.EventHandler(this.MenuItem_Stromverbraucher_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(206, 26);
            this.toolStripMenuItem4.Text = "Stromganglinie";
            this.toolStripMenuItem4.Click += new System.EventHandler(this.MenuItem_Stromganglinie_Click);
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            this.toolStripMenuItem8.Size = new System.Drawing.Size(206, 26);
            this.toolStripMenuItem8.Text = "Stromspeicher";
            this.toolStripMenuItem8.Click += new System.EventHandler(this.MenuItem_Stromspeicher_Click);
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.BackColor = System.Drawing.SystemColors.Control;
            this.toolStripMenuItem3.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_PV,
            this.MenuItem_Solarkollektoren,
            this.MenuItem_SolThermGanglinie,
            this.MenuItem_BHKW});
            this.toolStripMenuItem3.Image = global::WindowsFormsApplication1.Properties.Resources.Menu3;
            this.toolStripMenuItem3.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(273, 38);
            this.toolStripMenuItem3.Text = "Energiesysteme";
            // 
            // MenuItem_PV
            // 
            this.MenuItem_PV.Name = "MenuItem_PV";
            this.MenuItem_PV.Size = new System.Drawing.Size(234, 26);
            this.MenuItem_PV.Text = "Photovoltaik";
            this.MenuItem_PV.Click += new System.EventHandler(this.MenuItem_PV_Click);
            // 
            // MenuItem_Solarkollektoren
            // 
            this.MenuItem_Solarkollektoren.Name = "MenuItem_Solarkollektoren";
            this.MenuItem_Solarkollektoren.Size = new System.Drawing.Size(234, 26);
            this.MenuItem_Solarkollektoren.Text = "Solarkollektoren";
            this.MenuItem_Solarkollektoren.Click += new System.EventHandler(this.MenuItem_Solarkollektoren_Click);
            // 
            // MenuItem_SolThermGanglinie
            // 
            this.MenuItem_SolThermGanglinie.Name = "MenuItem_SolThermGanglinie";
            this.MenuItem_SolThermGanglinie.Size = new System.Drawing.Size(234, 26);
            this.MenuItem_SolThermGanglinie.Text = "Solarthermieganglinie";
            this.MenuItem_SolThermGanglinie.Click += new System.EventHandler(this.MenuItem_SolThermGanglinie_Click);
            // 
            // MenuItem_BHKW
            // 
            this.MenuItem_BHKW.Name = "MenuItem_BHKW";
            this.MenuItem_BHKW.Size = new System.Drawing.Size(234, 26);
            this.MenuItem_BHKW.Text = "BHKW";
            this.MenuItem_BHKW.Click += new System.EventHandler(this.MenuItem_BHKW_Click);
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Klimadaten});
            this.toolStripMenuItem6.Image = global::WindowsFormsApplication1.Properties.Resources.Menu4;
            this.toolStripMenuItem6.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(273, 38);
            this.toolStripMenuItem6.Text = "Klimadaten && Umgebung";
            // 
            // MenuItem_Klimadaten
            // 
            this.MenuItem_Klimadaten.Name = "MenuItem_Klimadaten";
            this.MenuItem_Klimadaten.Size = new System.Drawing.Size(158, 26);
            this.MenuItem_Klimadaten.Text = "Klimadaten";
            this.MenuItem_Klimadaten.Click += new System.EventHandler(this.MenuItem_Klimadaten_Click);
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Update});
            this.toolStripMenuItem7.Image = global::WindowsFormsApplication1.Properties.Resources.Menue5;
            this.toolStripMenuItem7.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(273, 38);
            this.toolStripMenuItem7.Text = "Daten && Import ";
            // 
            // MenuItem_Update
            // 
            this.MenuItem_Update.Name = "MenuItem_Update";
            this.MenuItem_Update.Size = new System.Drawing.Size(241, 26);
            this.MenuItem_Update.Text = "Datenbank importieren";
            this.MenuItem_Update.Click += new System.EventHandler(this.MenuItem_Update_Click);
            // 
            // MenuItem_Gebaeude
            // 
            this.MenuItem_Gebaeude.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_GebBearbeiten,
            this.MenuItem_GebTypen});
            this.MenuItem_Gebaeude.Image = global::WindowsFormsApplication1.Properties.Resources.Menue6;
            this.MenuItem_Gebaeude.ImageScaling = System.Windows.Forms.ToolStripItemImageScaling.None;
            this.MenuItem_Gebaeude.Name = "MenuItem_Gebaeude";
            this.MenuItem_Gebaeude.Size = new System.Drawing.Size(273, 38);
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
            // MDIMainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(762, 569);
            this.Controls.Add(this.menuStrip1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
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
        private System.Windows.Forms.ToolStripMenuItem MenuItem_ProjektLöschen;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Gebaeude;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_GebBearbeiten;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_GebTypen;
        private System.Windows.Forms.ToolStripMenuItem Help;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Version;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_WaermebedarfExtern;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Prozesswaerme;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_WP;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_WPBearbeiten;
        private System.Windows.Forms.ToolStripMenuItem MeniItem_VDI3805;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Kessel;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem8;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_PV;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Solarkollektoren;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_SolThermGanglinie;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_BHKW;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Klimadaten;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Update;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Lizenz;
    }
}

