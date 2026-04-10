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
            this.menuToolbar = new System.Windows.Forms.MenuStrip();
            this.Projekte = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_ProjektNeu = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_ProjektOeffnen = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_ProjektBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_zuletztGeöffnet = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_ProjektLöschen = new System.Windows.Forms.ToolStripMenuItem();
            this.Administration = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WBundHeizung = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Brauchwasser = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Kessel = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Prozesswaerme = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_PufferSp = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WaermebedarfExtern = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WP = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_StromBedarfundSp = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Stromverbraucher = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Stromganglinie = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Stromspeicher = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Energiesysteme = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_PV = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_PC_Bearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Solarkollektoren = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_ST_Bearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_SolThermGanglinie = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_BHKW = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Klima = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Klimadaten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_DatImport = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Update = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Import_Heizkessel = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_PufferSp_VDI3805 = new System.Windows.Forms.ToolStripMenuItem();
            this.MeniItem_VDI3805 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_PV_Import_CEC = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_ST_Import = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Gebaeude = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_GebBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_GebTypen = new System.Windows.Forms.ToolStripMenuItem();
            this.Help = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Version = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_Lizenz = new System.Windows.Forms.ToolStripMenuItem();
            this.Deutsch = new System.Windows.Forms.ToolStripMenuItem();
            this.Englisch = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Kosten = new System.Windows.Forms.ToolStripMenuItem();
            this.menuToolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuToolbar
            // 
            this.menuToolbar.BackColor = System.Drawing.Color.AliceBlue;
            this.menuToolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.Projekte,
            this.Administration,
            this.Help,
            this.Deutsch,
            this.Englisch});
            resources.ApplyResources(this.menuToolbar, "menuToolbar");
            this.menuToolbar.Name = "menuToolbar";
            // 
            // Projekte
            // 
            this.Projekte.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_ProjektNeu,
            this.toolStripSeparator1,
            this.MenuItem_ProjektOeffnen,
            this.toolStripSeparator2,
            this.MenuItem_ProjektBearbeiten,
            this.toolStripSeparator3,
            this.MenuItem_zuletztGeöffnet,
            this.toolStripSeparator4,
            this.MenuItem_ProjektLöschen});
            resources.ApplyResources(this.Projekte, "Projekte");
            this.Projekte.Name = "Projekte";
            this.Projekte.Tag = "Projekte";
            // 
            // MenuItem_ProjektNeu
            // 
            this.MenuItem_ProjektNeu.Name = "MenuItem_ProjektNeu";
            resources.ApplyResources(this.MenuItem_ProjektNeu, "MenuItem_ProjektNeu");
            this.MenuItem_ProjektNeu.Click += new System.EventHandler(this.MenuItem_Neu_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
            // 
            // MenuItem_ProjektOeffnen
            // 
            this.MenuItem_ProjektOeffnen.Name = "MenuItem_ProjektOeffnen";
            resources.ApplyResources(this.MenuItem_ProjektOeffnen, "MenuItem_ProjektOeffnen");
            this.MenuItem_ProjektOeffnen.Click += new System.EventHandler(this.MenuItem_ProjektOeffnen_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
            // 
            // MenuItem_ProjektBearbeiten
            // 
            this.MenuItem_ProjektBearbeiten.Name = "MenuItem_ProjektBearbeiten";
            resources.ApplyResources(this.MenuItem_ProjektBearbeiten, "MenuItem_ProjektBearbeiten");
            this.MenuItem_ProjektBearbeiten.Click += new System.EventHandler(this.MenuItem_ProjektBearbeiten_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
            // 
            // MenuItem_zuletztGeöffnet
            // 
            this.MenuItem_zuletztGeöffnet.Name = "MenuItem_zuletztGeöffnet";
            resources.ApplyResources(this.MenuItem_zuletztGeöffnet, "MenuItem_zuletztGeöffnet");
            this.MenuItem_zuletztGeöffnet.Click += new System.EventHandler(this.MenuItem_zuletztGeöffnet_Click);
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
            // 
            // MenuItem_ProjektLöschen
            // 
            this.MenuItem_ProjektLöschen.Name = "MenuItem_ProjektLöschen";
            resources.ApplyResources(this.MenuItem_ProjektLöschen, "MenuItem_ProjektLöschen");
            this.MenuItem_ProjektLöschen.Click += new System.EventHandler(this.MenuItem_ProjektLöschen_Click);
            // 
            // Administration
            // 
            this.Administration.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.Administration.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_WBundHeizung,
            this.MenuItem_StromBedarfundSp,
            this.MenuItem_Energiesysteme,
            this.MenuItem_Klima,
            this.MenuItem_DatImport,
            this.MenuItem_Gebaeude});
            resources.ApplyResources(this.Administration, "Administration");
            this.Administration.Name = "Administration";
            // 
            // MenuItem_WBundHeizung
            // 
            this.MenuItem_WBundHeizung.BackColor = System.Drawing.SystemColors.Control;
            this.MenuItem_WBundHeizung.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Brauchwasser,
            this.MenuItem_Kessel,
            this.MenuItem_Prozesswaerme,
            this.MenuItem_PufferSp,
            this.MenuItem_WaermebedarfExtern,
            this.MenuItem_WP});
            this.MenuItem_WBundHeizung.Image = global::WindowsFormsApplication1.Properties.Resources.Menu1;
            resources.ApplyResources(this.MenuItem_WBundHeizung, "MenuItem_WBundHeizung");
            this.MenuItem_WBundHeizung.Name = "MenuItem_WBundHeizung";
            // 
            // MenuItem_Brauchwasser
            // 
            this.MenuItem_Brauchwasser.Name = "MenuItem_Brauchwasser";
            resources.ApplyResources(this.MenuItem_Brauchwasser, "MenuItem_Brauchwasser");
            this.MenuItem_Brauchwasser.Click += new System.EventHandler(this.MenuItem_Brauchwasser_Click);
            // 
            // MenuItem_Kessel
            // 
            this.MenuItem_Kessel.Name = "MenuItem_Kessel";
            resources.ApplyResources(this.MenuItem_Kessel, "MenuItem_Kessel");
            this.MenuItem_Kessel.Click += new System.EventHandler(this.MenuItem_Kessel_Click);
            // 
            // MenuItem_Prozesswaerme
            // 
            this.MenuItem_Prozesswaerme.Name = "MenuItem_Prozesswaerme";
            resources.ApplyResources(this.MenuItem_Prozesswaerme, "MenuItem_Prozesswaerme");
            this.MenuItem_Prozesswaerme.Click += new System.EventHandler(this.MenuItem_Prozesswaerme_Click);
            // 
            // MenuItem_PufferSp
            // 
            this.MenuItem_PufferSp.Name = "MenuItem_PufferSp";
            resources.ApplyResources(this.MenuItem_PufferSp, "MenuItem_PufferSp");
            this.MenuItem_PufferSp.Click += new System.EventHandler(this.MenuItem_PufferSp_Click);
            // 
            // MenuItem_WaermebedarfExtern
            // 
            this.MenuItem_WaermebedarfExtern.Name = "MenuItem_WaermebedarfExtern";
            resources.ApplyResources(this.MenuItem_WaermebedarfExtern, "MenuItem_WaermebedarfExtern");
            this.MenuItem_WaermebedarfExtern.Click += new System.EventHandler(this.MenuItem_WaermebedarfExtern_Click);
            // 
            // MenuItem_WP
            // 
            this.MenuItem_WP.Name = "MenuItem_WP";
            resources.ApplyResources(this.MenuItem_WP, "MenuItem_WP");
            this.MenuItem_WP.Click += new System.EventHandler(this.MenuItem_WP_Click);
            // 
            // MenuItem_StromBedarfundSp
            // 
            this.MenuItem_StromBedarfundSp.BackColor = System.Drawing.SystemColors.Control;
            this.MenuItem_StromBedarfundSp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Stromverbraucher,
            this.MenuItem_Stromganglinie,
            this.MenuItem_Stromspeicher});
            this.MenuItem_StromBedarfundSp.Image = global::WindowsFormsApplication1.Properties.Resources.Menue2;
            resources.ApplyResources(this.MenuItem_StromBedarfundSp, "MenuItem_StromBedarfundSp");
            this.MenuItem_StromBedarfundSp.Name = "MenuItem_StromBedarfundSp";
            // 
            // MenuItem_Stromverbraucher
            // 
            this.MenuItem_Stromverbraucher.Name = "MenuItem_Stromverbraucher";
            resources.ApplyResources(this.MenuItem_Stromverbraucher, "MenuItem_Stromverbraucher");
            this.MenuItem_Stromverbraucher.Click += new System.EventHandler(this.MenuItem_Stromverbraucher_Click);
            // 
            // MenuItem_Stromganglinie
            // 
            this.MenuItem_Stromganglinie.Name = "MenuItem_Stromganglinie";
            resources.ApplyResources(this.MenuItem_Stromganglinie, "MenuItem_Stromganglinie");
            this.MenuItem_Stromganglinie.Click += new System.EventHandler(this.MenuItem_Stromganglinie_Click);
            // 
            // MenuItem_Stromspeicher
            // 
            this.MenuItem_Stromspeicher.Name = "MenuItem_Stromspeicher";
            resources.ApplyResources(this.MenuItem_Stromspeicher, "MenuItem_Stromspeicher");
            this.MenuItem_Stromspeicher.Click += new System.EventHandler(this.MenuItem_Stromspeicher_Click);
            // 
            // MenuItem_Energiesysteme
            // 
            this.MenuItem_Energiesysteme.BackColor = System.Drawing.SystemColors.Control;
            this.MenuItem_Energiesysteme.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_PV,
            this.MenuItem_Solarkollektoren,
            this.MenuItem_SolThermGanglinie,
            this.MenuItem_BHKW});
            this.MenuItem_Energiesysteme.Image = global::WindowsFormsApplication1.Properties.Resources.Menu3;
            resources.ApplyResources(this.MenuItem_Energiesysteme, "MenuItem_Energiesysteme");
            this.MenuItem_Energiesysteme.Name = "MenuItem_Energiesysteme";
            // 
            // MenuItem_PV
            // 
            this.MenuItem_PV.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_PC_Bearbeiten});
            this.MenuItem_PV.Name = "MenuItem_PV";
            resources.ApplyResources(this.MenuItem_PV, "MenuItem_PV");
            // 
            // MenuItem_PC_Bearbeiten
            // 
            this.MenuItem_PC_Bearbeiten.Name = "MenuItem_PC_Bearbeiten";
            resources.ApplyResources(this.MenuItem_PC_Bearbeiten, "MenuItem_PC_Bearbeiten");
            this.MenuItem_PC_Bearbeiten.Click += new System.EventHandler(this.MenuItem_PV_Bearbeiten_Click);
            // 
            // MenuItem_Solarkollektoren
            // 
            this.MenuItem_Solarkollektoren.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_ST_Bearbeiten});
            this.MenuItem_Solarkollektoren.Name = "MenuItem_Solarkollektoren";
            resources.ApplyResources(this.MenuItem_Solarkollektoren, "MenuItem_Solarkollektoren");
            // 
            // MenuItem_ST_Bearbeiten
            // 
            this.MenuItem_ST_Bearbeiten.Name = "MenuItem_ST_Bearbeiten";
            resources.ApplyResources(this.MenuItem_ST_Bearbeiten, "MenuItem_ST_Bearbeiten");
            this.MenuItem_ST_Bearbeiten.Click += new System.EventHandler(this.MenuItem_ST_Bearbeiten_Click);
            // 
            // MenuItem_SolThermGanglinie
            // 
            this.MenuItem_SolThermGanglinie.Name = "MenuItem_SolThermGanglinie";
            resources.ApplyResources(this.MenuItem_SolThermGanglinie, "MenuItem_SolThermGanglinie");
            this.MenuItem_SolThermGanglinie.Click += new System.EventHandler(this.MenuItem_SolThermGanglinie_Click);
            // 
            // MenuItem_BHKW
            // 
            this.MenuItem_BHKW.Name = "MenuItem_BHKW";
            resources.ApplyResources(this.MenuItem_BHKW, "MenuItem_BHKW");
            this.MenuItem_BHKW.Click += new System.EventHandler(this.MenuItem_BHKW_Click);
            // 
            // MenuItem_Klima
            // 
            this.MenuItem_Klima.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Klimadaten});
            this.MenuItem_Klima.Image = global::WindowsFormsApplication1.Properties.Resources.Menu4;
            resources.ApplyResources(this.MenuItem_Klima, "MenuItem_Klima");
            this.MenuItem_Klima.Name = "MenuItem_Klima";
            // 
            // MenuItem_Klimadaten
            // 
            this.MenuItem_Klimadaten.Name = "MenuItem_Klimadaten";
            resources.ApplyResources(this.MenuItem_Klimadaten, "MenuItem_Klimadaten");
            this.MenuItem_Klimadaten.Click += new System.EventHandler(this.MenuItem_Klimadaten_Click);
            // 
            // MenuItem_DatImport
            // 
            this.MenuItem_DatImport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Update,
            this.MenuItem_Import_Heizkessel,
            this.MenuItem_PufferSp_VDI3805,
            this.MeniItem_VDI3805,
            this.MenuItem_PV_Import_CEC,
            this.MenuItem_ST_Import,
            this.MenuItem_Kosten});
            this.MenuItem_DatImport.Image = global::WindowsFormsApplication1.Properties.Resources.Menue5;
            resources.ApplyResources(this.MenuItem_DatImport, "MenuItem_DatImport");
            this.MenuItem_DatImport.Name = "MenuItem_DatImport";
            // 
            // MenuItem_Update
            // 
            this.MenuItem_Update.Name = "MenuItem_Update";
            resources.ApplyResources(this.MenuItem_Update, "MenuItem_Update");
            this.MenuItem_Update.Click += new System.EventHandler(this.MenuItem_Update_Click);
            // 
            // MenuItem_Import_Heizkessel
            // 
            this.MenuItem_Import_Heizkessel.Name = "MenuItem_Import_Heizkessel";
            resources.ApplyResources(this.MenuItem_Import_Heizkessel, "MenuItem_Import_Heizkessel");
            this.MenuItem_Import_Heizkessel.Click += new System.EventHandler(this.MenuItem_Import_Heizkessel_Click);
            // 
            // MenuItem_PufferSp_VDI3805
            // 
            this.MenuItem_PufferSp_VDI3805.Name = "MenuItem_PufferSp_VDI3805";
            resources.ApplyResources(this.MenuItem_PufferSp_VDI3805, "MenuItem_PufferSp_VDI3805");
            this.MenuItem_PufferSp_VDI3805.Click += new System.EventHandler(this.MeniItem_PufferSp_VDI3805_Click);
            // 
            // MeniItem_VDI3805
            // 
            this.MeniItem_VDI3805.Name = "MeniItem_VDI3805";
            resources.ApplyResources(this.MeniItem_VDI3805, "MeniItem_VDI3805");
            this.MeniItem_VDI3805.Click += new System.EventHandler(this.MenuItem_WP_VDI3805_Click);
            // 
            // MenuItem_PV_Import_CEC
            // 
            this.MenuItem_PV_Import_CEC.Name = "MenuItem_PV_Import_CEC";
            resources.ApplyResources(this.MenuItem_PV_Import_CEC, "MenuItem_PV_Import_CEC");
            this.MenuItem_PV_Import_CEC.Click += new System.EventHandler(this.MenuItem_PV_Import_CEC_Click);
            // 
            // MenuItem_ST_Import
            // 
            this.MenuItem_ST_Import.Name = "MenuItem_ST_Import";
            resources.ApplyResources(this.MenuItem_ST_Import, "MenuItem_ST_Import");
            this.MenuItem_ST_Import.Click += new System.EventHandler(this.MenuItem_ST_Import_Click);
            // 
            // MenuItem_Gebaeude
            // 
            this.MenuItem_Gebaeude.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_GebBearbeiten,
            this.MenuItem_GebTypen});
            this.MenuItem_Gebaeude.Image = global::WindowsFormsApplication1.Properties.Resources.Menue6;
            resources.ApplyResources(this.MenuItem_Gebaeude, "MenuItem_Gebaeude");
            this.MenuItem_Gebaeude.Name = "MenuItem_Gebaeude";
            // 
            // MenuItem_GebBearbeiten
            // 
            this.MenuItem_GebBearbeiten.Name = "MenuItem_GebBearbeiten";
            resources.ApplyResources(this.MenuItem_GebBearbeiten, "MenuItem_GebBearbeiten");
            this.MenuItem_GebBearbeiten.Click += new System.EventHandler(this.MenuItem_GebBearbeiten_Click);
            // 
            // MenuItem_GebTypen
            // 
            this.MenuItem_GebTypen.Name = "MenuItem_GebTypen";
            resources.ApplyResources(this.MenuItem_GebTypen, "MenuItem_GebTypen");
            this.MenuItem_GebTypen.Click += new System.EventHandler(this.MenuItem_GebTypen_Click);
            // 
            // Help
            // 
            this.Help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Version,
            this.toolStripSeparator7,
            this.MenuItem_Lizenz});
            resources.ApplyResources(this.Help, "Help");
            this.Help.Name = "Help";
            // 
            // MenuItem_Version
            // 
            this.MenuItem_Version.BackColor = System.Drawing.SystemColors.Control;
            this.MenuItem_Version.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.MenuItem_Version.Name = "MenuItem_Version";
            resources.ApplyResources(this.MenuItem_Version, "MenuItem_Version");
            this.MenuItem_Version.Click += new System.EventHandler(this.MenuItem_Version_Click);
            // 
            // toolStripSeparator7
            // 
            this.toolStripSeparator7.Name = "toolStripSeparator7";
            resources.ApplyResources(this.toolStripSeparator7, "toolStripSeparator7");
            // 
            // MenuItem_Lizenz
            // 
            this.MenuItem_Lizenz.Name = "MenuItem_Lizenz";
            resources.ApplyResources(this.MenuItem_Lizenz, "MenuItem_Lizenz");
            this.MenuItem_Lizenz.Click += new System.EventHandler(this.MenuItem_Lizenz_Click);
            // 
            // Deutsch
            // 
            this.Deutsch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            resources.ApplyResources(this.Deutsch, "Deutsch");
            this.Deutsch.Image = global::WindowsFormsApplication1.Properties.Resources.germany;
            this.Deutsch.Name = "Deutsch";
            this.Deutsch.Click += new System.EventHandler(this.Deutsch_Click);
            // 
            // Englisch
            // 
            this.Englisch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            resources.ApplyResources(this.Englisch, "Englisch");
            this.Englisch.Image = global::WindowsFormsApplication1.Properties.Resources.usa;
            this.Englisch.Name = "Englisch";
            this.Englisch.Click += new System.EventHandler(this.Englisch_Click);
            // 
            // MenuItem_Kosten
            // 
            this.MenuItem_Kosten.Name = "MenuItem_Kosten";
            resources.ApplyResources(this.MenuItem_Kosten, "MenuItem_Kosten");
            this.MenuItem_Kosten.Click += new System.EventHandler(this.MenuItem_Kosten_Click);
            // 
            // MDIMainForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.menuToolbar);
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.menuToolbar;
            this.Name = "MDIMainForm";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.Load += new System.EventHandler(this.MDIMainForm_Load);
            this.menuToolbar.ResumeLayout(false);
            this.menuToolbar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuToolbar;
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
        private System.Windows.Forms.ToolStripMenuItem MenuItem_WBundHeizung;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_WaermebedarfExtern;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Prozesswaerme;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_WP;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Kessel;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_StromBedarfundSp;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Stromverbraucher;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Stromganglinie;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Stromspeicher;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Energiesysteme;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_PV;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Solarkollektoren;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_SolThermGanglinie;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_BHKW;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Klima;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Klimadaten;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_DatImport;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Update;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator7;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Lizenz;
        private System.Windows.Forms.ToolStripMenuItem Deutsch;
        private System.Windows.Forms.ToolStripMenuItem Englisch;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_PufferSp;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Brauchwasser;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_PC_Bearbeiten;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_ST_Bearbeiten;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Import_Heizkessel;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_PufferSp_VDI3805;
        private System.Windows.Forms.ToolStripMenuItem MeniItem_VDI3805;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_PV_Import_CEC;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_ST_Import;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Kosten;
    }
}

