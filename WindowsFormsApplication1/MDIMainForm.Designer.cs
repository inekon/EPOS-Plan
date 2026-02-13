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
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WaermebedarfExtern = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Prozesswaerme = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WP = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_WPBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MeniItem_VDI3805 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Kessel = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_SPKBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            this.MeniItem_SPK_VDI3805 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_PufferSp = new System.Windows.Forms.ToolStripMenuItem();
            this.MeniItem_PufferSp_VDI3805 = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_PufferSpBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
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
            this.Help = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Version = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_Lizenz = new System.Windows.Forms.ToolStripMenuItem();
            this.Deutsch = new System.Windows.Forms.ToolStripMenuItem();
            this.Englisch = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_Brauchwasser = new System.Windows.Forms.ToolStripMenuItem();
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
            this.toolStripMenuItem1,
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem6,
            this.toolStripMenuItem7,
            this.MenuItem_Gebaeude});
            resources.ApplyResources(this.Administration, "Administration");
            this.Administration.Name = "Administration";
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.BackColor = System.Drawing.SystemColors.Control;
            this.toolStripMenuItem1.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Brauchwasser,
            this.MenuItem_Kessel,
            this.MenuItem_Prozesswaerme,
            this.MenuItem_PufferSp,
            this.MenuItem_WaermebedarfExtern,
            this.MenuItem_WP});
            this.toolStripMenuItem1.Image = global::WindowsFormsApplication1.Properties.Resources.Menu1;
            resources.ApplyResources(this.toolStripMenuItem1, "toolStripMenuItem1");
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            // 
            // MenuItem_WaermebedarfExtern
            // 
            this.MenuItem_WaermebedarfExtern.Name = "MenuItem_WaermebedarfExtern";
            resources.ApplyResources(this.MenuItem_WaermebedarfExtern, "MenuItem_WaermebedarfExtern");
            this.MenuItem_WaermebedarfExtern.Click += new System.EventHandler(this.MenuItem_WaermebedarfExtern_Click);
            // 
            // MenuItem_Prozesswaerme
            // 
            this.MenuItem_Prozesswaerme.Name = "MenuItem_Prozesswaerme";
            resources.ApplyResources(this.MenuItem_Prozesswaerme, "MenuItem_Prozesswaerme");
            this.MenuItem_Prozesswaerme.Click += new System.EventHandler(this.MenuItem_Prozesswaerme_Click);
            // 
            // MenuItem_WP
            // 
            this.MenuItem_WP.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_WPBearbeiten,
            this.MeniItem_VDI3805});
            this.MenuItem_WP.Name = "MenuItem_WP";
            resources.ApplyResources(this.MenuItem_WP, "MenuItem_WP");
            // 
            // MenuItem_WPBearbeiten
            // 
            this.MenuItem_WPBearbeiten.Name = "MenuItem_WPBearbeiten";
            resources.ApplyResources(this.MenuItem_WPBearbeiten, "MenuItem_WPBearbeiten");
            this.MenuItem_WPBearbeiten.Click += new System.EventHandler(this.MenuItem_WPBearbeiten_Click_1);
            // 
            // MeniItem_VDI3805
            // 
            this.MeniItem_VDI3805.Name = "MeniItem_VDI3805";
            resources.ApplyResources(this.MeniItem_VDI3805, "MeniItem_VDI3805");
            this.MeniItem_VDI3805.Click += new System.EventHandler(this.MeniItem_VDI3805_Click);
            // 
            // MenuItem_Kessel
            // 
            this.MenuItem_Kessel.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_SPKBearbeiten,
            this.MeniItem_SPK_VDI3805});
            this.MenuItem_Kessel.Name = "MenuItem_Kessel";
            resources.ApplyResources(this.MenuItem_Kessel, "MenuItem_Kessel");
            // 
            // MenuItem_SPKBearbeiten
            // 
            this.MenuItem_SPKBearbeiten.Name = "MenuItem_SPKBearbeiten";
            resources.ApplyResources(this.MenuItem_SPKBearbeiten, "MenuItem_SPKBearbeiten");
            this.MenuItem_SPKBearbeiten.Click += new System.EventHandler(this.MenuItem_SPKBearbeiten_Click);
            // 
            // MeniItem_SPK_VDI3805
            // 
            this.MeniItem_SPK_VDI3805.Name = "MeniItem_SPK_VDI3805";
            resources.ApplyResources(this.MeniItem_SPK_VDI3805, "MeniItem_SPK_VDI3805");
            this.MeniItem_SPK_VDI3805.Click += new System.EventHandler(this.MeniItem_SPK_VDI3805_Click);
            // 
            // MenuItem_PufferSp
            // 
            this.MenuItem_PufferSp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MeniItem_PufferSp_VDI3805,
            this.MenuItem_PufferSpBearbeiten});
            this.MenuItem_PufferSp.Name = "MenuItem_PufferSp";
            resources.ApplyResources(this.MenuItem_PufferSp, "MenuItem_PufferSp");
            // 
            // MeniItem_PufferSp_VDI3805
            // 
            this.MeniItem_PufferSp_VDI3805.Name = "MeniItem_PufferSp_VDI3805";
            resources.ApplyResources(this.MeniItem_PufferSp_VDI3805, "MeniItem_PufferSp_VDI3805");
            this.MeniItem_PufferSp_VDI3805.Click += new System.EventHandler(this.MeniItem_PufferSp_VDI3805_Click);
            // 
            // MenuItem_PufferSpBearbeiten
            // 
            this.MenuItem_PufferSpBearbeiten.Name = "MenuItem_PufferSpBearbeiten";
            resources.ApplyResources(this.MenuItem_PufferSpBearbeiten, "MenuItem_PufferSpBearbeiten");
            this.MenuItem_PufferSpBearbeiten.Click += new System.EventHandler(this.MenuItem_PufferSpBearbeiten_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.BackColor = System.Drawing.SystemColors.Control;
            this.toolStripMenuItem2.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem5,
            this.toolStripMenuItem4,
            this.toolStripMenuItem8});
            this.toolStripMenuItem2.Image = global::WindowsFormsApplication1.Properties.Resources.Menue2;
            resources.ApplyResources(this.toolStripMenuItem2, "toolStripMenuItem2");
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            resources.ApplyResources(this.toolStripMenuItem5, "toolStripMenuItem5");
            this.toolStripMenuItem5.Click += new System.EventHandler(this.MenuItem_Stromverbraucher_Click);
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            resources.ApplyResources(this.toolStripMenuItem4, "toolStripMenuItem4");
            this.toolStripMenuItem4.Click += new System.EventHandler(this.MenuItem_Stromganglinie_Click);
            // 
            // toolStripMenuItem8
            // 
            this.toolStripMenuItem8.Name = "toolStripMenuItem8";
            resources.ApplyResources(this.toolStripMenuItem8, "toolStripMenuItem8");
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
            resources.ApplyResources(this.toolStripMenuItem3, "toolStripMenuItem3");
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            // 
            // MenuItem_PV
            // 
            this.MenuItem_PV.Name = "MenuItem_PV";
            resources.ApplyResources(this.MenuItem_PV, "MenuItem_PV");
            this.MenuItem_PV.Click += new System.EventHandler(this.MenuItem_PV_Click);
            // 
            // MenuItem_Solarkollektoren
            // 
            this.MenuItem_Solarkollektoren.Name = "MenuItem_Solarkollektoren";
            resources.ApplyResources(this.MenuItem_Solarkollektoren, "MenuItem_Solarkollektoren");
            this.MenuItem_Solarkollektoren.Click += new System.EventHandler(this.MenuItem_Solarkollektoren_Click);
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
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Klimadaten});
            this.toolStripMenuItem6.Image = global::WindowsFormsApplication1.Properties.Resources.Menu4;
            resources.ApplyResources(this.toolStripMenuItem6, "toolStripMenuItem6");
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            // 
            // MenuItem_Klimadaten
            // 
            this.MenuItem_Klimadaten.Name = "MenuItem_Klimadaten";
            resources.ApplyResources(this.MenuItem_Klimadaten, "MenuItem_Klimadaten");
            this.MenuItem_Klimadaten.Click += new System.EventHandler(this.MenuItem_Klimadaten_Click);
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_Update});
            this.toolStripMenuItem7.Image = global::WindowsFormsApplication1.Properties.Resources.Menue5;
            resources.ApplyResources(this.toolStripMenuItem7, "toolStripMenuItem7");
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            // 
            // MenuItem_Update
            // 
            this.MenuItem_Update.Name = "MenuItem_Update";
            resources.ApplyResources(this.MenuItem_Update, "MenuItem_Update");
            this.MenuItem_Update.Click += new System.EventHandler(this.MenuItem_Update_Click);
            // 
            // MenuItem_Gebaeude
            // 
            this.MenuItem_Gebaeude.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_GebBearbeiten,
            this.MenuItem_GebTypen});
            this.MenuItem_Gebaeude.Image = global::WindowsFormsApplication1.Properties.Resources.Menue6;
            resources.ApplyResources(this.MenuItem_Gebaeude, "MenuItem_Gebaeude");
            this.MenuItem_Gebaeude.Name = "MenuItem_Gebaeude";
            this.MenuItem_Gebaeude.Click += new System.EventHandler(this.MenuItem_Gebaeude_Click);
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
            // MenuItem_Brauchwasser
            // 
            this.MenuItem_Brauchwasser.Name = "MenuItem_Brauchwasser";
            resources.ApplyResources(this.MenuItem_Brauchwasser, "MenuItem_Brauchwasser");
            this.MenuItem_Brauchwasser.Click += new System.EventHandler(this.MenuItem_Brauchwasser_Click);
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
        private System.Windows.Forms.ToolStripMenuItem Deutsch;
        private System.Windows.Forms.ToolStripMenuItem Englisch;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_SPKBearbeiten;
        private System.Windows.Forms.ToolStripMenuItem MeniItem_SPK_VDI3805;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_PufferSp;
        private System.Windows.Forms.ToolStripMenuItem MeniItem_PufferSp_VDI3805;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_PufferSpBearbeiten;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Brauchwasser;
    }
}

