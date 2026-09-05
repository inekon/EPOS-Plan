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
            menuToolbar = new System.Windows.Forms.MenuStrip();
            Projekte = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_ProjektNeu = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            MenuItem_ProjektOeffnen = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            MenuItem_ProjektBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            MenuItem_zuletztGeöffnet = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            MenuItem_ProjektLöschen = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            MenuItem_ExportImport = new System.Windows.Forms.ToolStripMenuItem();
            Administration = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_WBundHeizung = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Brauchwasser = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Kessel = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Prozesswaerme = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_PufferSp = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_WaermebedarfExtern = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_WP = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_StromBedarfundSp = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Stromverbraucher = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Stromganglinie = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Stromspeicher = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Energiesysteme = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_PV = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_PC_Bearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Solarkollektoren = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_ST_Bearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_SolThermGanglinie = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_BHKW = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Klima = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Klimadaten = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_DatImport = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Import_Heizkessel = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_PufferSp_VDI3805 = new System.Windows.Forms.ToolStripMenuItem();
            MeniItem_VDI3805 = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_PV_Import_CEC = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_ST_Import = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_KostenVerwaltung = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Gebaeude = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_GebBearbeiten = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_GebTypen = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Einstellungen = new System.Windows.Forms.ToolStripMenuItem();
            Help = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Version = new System.Windows.Forms.ToolStripMenuItem();
            toolStripSeparator7 = new System.Windows.Forms.ToolStripSeparator();
            MenuItem_Lizenz = new System.Windows.Forms.ToolStripMenuItem();
            MenuItem_Dokumentation = new System.Windows.Forms.ToolStripMenuItem();
            Deutsch = new System.Windows.Forms.ToolStripMenuItem();
            Englisch = new System.Windows.Forms.ToolStripMenuItem();
            label_OnlineDoku = new System.Windows.Forms.Label();
            menuToolbar.SuspendLayout();
            SuspendLayout();
            // 
            // menuToolbar
            // 
            menuToolbar.BackColor = System.Drawing.Color.AliceBlue;
            menuToolbar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { Projekte, Administration, Help, Deutsch, Englisch });
            resources.ApplyResources(menuToolbar, "menuToolbar");
            menuToolbar.Name = "menuToolbar";
            // 
            // Projekte
            // 
            Projekte.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_ProjektNeu, toolStripSeparator1, MenuItem_ProjektOeffnen, toolStripSeparator2, MenuItem_ProjektBearbeiten, toolStripSeparator3, MenuItem_zuletztGeöffnet, toolStripSeparator4, MenuItem_ProjektLöschen, toolStripSeparator5, MenuItem_ExportImport });
            resources.ApplyResources(Projekte, "Projekte");
            Projekte.Name = "Projekte";
            Projekte.Tag = "Projekte";
            // 
            // MenuItem_ProjektNeu
            // 
            MenuItem_ProjektNeu.Name = "MenuItem_ProjektNeu";
            resources.ApplyResources(MenuItem_ProjektNeu, "MenuItem_ProjektNeu");
            MenuItem_ProjektNeu.Click += MenuItem_Neu_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            resources.ApplyResources(toolStripSeparator1, "toolStripSeparator1");
            // 
            // MenuItem_ProjektOeffnen
            // 
            MenuItem_ProjektOeffnen.Name = "MenuItem_ProjektOeffnen";
            resources.ApplyResources(MenuItem_ProjektOeffnen, "MenuItem_ProjektOeffnen");
            MenuItem_ProjektOeffnen.Click += MenuItem_ProjektOeffnen_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            resources.ApplyResources(toolStripSeparator2, "toolStripSeparator2");
            // 
            // MenuItem_ProjektBearbeiten
            // 
            MenuItem_ProjektBearbeiten.Name = "MenuItem_ProjektBearbeiten";
            resources.ApplyResources(MenuItem_ProjektBearbeiten, "MenuItem_ProjektBearbeiten");
            MenuItem_ProjektBearbeiten.Click += MenuItem_ProjektBearbeiten_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            resources.ApplyResources(toolStripSeparator3, "toolStripSeparator3");
            // 
            // MenuItem_zuletztGeöffnet
            // 
            MenuItem_zuletztGeöffnet.Name = "MenuItem_zuletztGeöffnet";
            resources.ApplyResources(MenuItem_zuletztGeöffnet, "MenuItem_zuletztGeöffnet");
            MenuItem_zuletztGeöffnet.Click += MenuItem_zuletztGeöffnet_Click;
            // 
            // toolStripSeparator4
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            resources.ApplyResources(toolStripSeparator4, "toolStripSeparator4");
            // 
            // MenuItem_ProjektLöschen
            // 
            MenuItem_ProjektLöschen.Name = "MenuItem_ProjektLöschen";
            resources.ApplyResources(MenuItem_ProjektLöschen, "MenuItem_ProjektLöschen");
            MenuItem_ProjektLöschen.Click += MenuItem_ProjektLöschen_Click;
            // 
            // toolStripSeparator5
            // 
            toolStripSeparator5.Name = "toolStripSeparator5";
            resources.ApplyResources(toolStripSeparator5, "toolStripSeparator5");
            // 
            // MenuItem_ExportImport
            // 
            MenuItem_ExportImport.Name = "MenuItem_ExportImport";
            resources.ApplyResources(MenuItem_ExportImport, "MenuItem_ExportImport");
            MenuItem_ExportImport.Click += MenuItem_ExportImport_Click;
            // 
            // Administration
            // 
            Administration.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            Administration.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_WBundHeizung, MenuItem_StromBedarfundSp, MenuItem_Energiesysteme, MenuItem_Klima, MenuItem_DatImport, MenuItem_KostenVerwaltung, MenuItem_Gebaeude, MenuItem_Einstellungen });
            resources.ApplyResources(Administration, "Administration");
            Administration.Name = "Administration";
            // 
            // MenuItem_WBundHeizung
            // 
            MenuItem_WBundHeizung.BackColor = System.Drawing.SystemColors.Control;
            MenuItem_WBundHeizung.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_Brauchwasser, MenuItem_Kessel, MenuItem_Prozesswaerme, MenuItem_PufferSp, MenuItem_WaermebedarfExtern, MenuItem_WP });
            MenuItem_WBundHeizung.Image = Properties.Resources.Menu1;
            resources.ApplyResources(MenuItem_WBundHeizung, "MenuItem_WBundHeizung");
            MenuItem_WBundHeizung.Name = "MenuItem_WBundHeizung";
            // 
            // MenuItem_Brauchwasser
            // 
            MenuItem_Brauchwasser.Name = "MenuItem_Brauchwasser";
            resources.ApplyResources(MenuItem_Brauchwasser, "MenuItem_Brauchwasser");
            MenuItem_Brauchwasser.Click += MenuItem_Brauchwasser_Click;
            // 
            // MenuItem_Kessel
            // 
            MenuItem_Kessel.Name = "MenuItem_Kessel";
            resources.ApplyResources(MenuItem_Kessel, "MenuItem_Kessel");
            MenuItem_Kessel.Click += MenuItem_Kessel_Click;
            // 
            // MenuItem_Prozesswaerme
            // 
            MenuItem_Prozesswaerme.Name = "MenuItem_Prozesswaerme";
            resources.ApplyResources(MenuItem_Prozesswaerme, "MenuItem_Prozesswaerme");
            MenuItem_Prozesswaerme.Click += MenuItem_Prozesswaerme_Click;
            // 
            // MenuItem_PufferSp
            // 
            MenuItem_PufferSp.Name = "MenuItem_PufferSp";
            resources.ApplyResources(MenuItem_PufferSp, "MenuItem_PufferSp");
            MenuItem_PufferSp.Click += MenuItem_PufferSp_Click;
            // 
            // MenuItem_WaermebedarfExtern
            // 
            MenuItem_WaermebedarfExtern.Name = "MenuItem_WaermebedarfExtern";
            resources.ApplyResources(MenuItem_WaermebedarfExtern, "MenuItem_WaermebedarfExtern");
            MenuItem_WaermebedarfExtern.Click += MenuItem_WaermebedarfExtern_Click;
            // 
            // MenuItem_WP
            // 
            MenuItem_WP.Name = "MenuItem_WP";
            resources.ApplyResources(MenuItem_WP, "MenuItem_WP");
            MenuItem_WP.Click += MenuItem_WP_Click;
            // 
            // MenuItem_StromBedarfundSp
            // 
            MenuItem_StromBedarfundSp.BackColor = System.Drawing.SystemColors.Control;
            MenuItem_StromBedarfundSp.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_Stromverbraucher, MenuItem_Stromganglinie, MenuItem_Stromspeicher });
            MenuItem_StromBedarfundSp.Image = Properties.Resources.Menue2;
            resources.ApplyResources(MenuItem_StromBedarfundSp, "MenuItem_StromBedarfundSp");
            MenuItem_StromBedarfundSp.Name = "MenuItem_StromBedarfundSp";
            // 
            // MenuItem_Stromverbraucher
            // 
            MenuItem_Stromverbraucher.Name = "MenuItem_Stromverbraucher";
            resources.ApplyResources(MenuItem_Stromverbraucher, "MenuItem_Stromverbraucher");
            MenuItem_Stromverbraucher.Click += MenuItem_Stromverbraucher_Click;
            // 
            // MenuItem_Stromganglinie
            // 
            MenuItem_Stromganglinie.Name = "MenuItem_Stromganglinie";
            resources.ApplyResources(MenuItem_Stromganglinie, "MenuItem_Stromganglinie");
            MenuItem_Stromganglinie.Click += MenuItem_Stromganglinie_Click;
            // 
            // MenuItem_Stromspeicher
            // 
            MenuItem_Stromspeicher.Name = "MenuItem_Stromspeicher";
            resources.ApplyResources(MenuItem_Stromspeicher, "MenuItem_Stromspeicher");
            MenuItem_Stromspeicher.Click += MenuItem_Stromspeicher_Click;
            // 
            // MenuItem_Energiesysteme
            // 
            MenuItem_Energiesysteme.BackColor = System.Drawing.SystemColors.Control;
            MenuItem_Energiesysteme.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_PV, MenuItem_Solarkollektoren, MenuItem_SolThermGanglinie, MenuItem_BHKW });
            MenuItem_Energiesysteme.Image = Properties.Resources.Menu3;
            resources.ApplyResources(MenuItem_Energiesysteme, "MenuItem_Energiesysteme");
            MenuItem_Energiesysteme.Name = "MenuItem_Energiesysteme";
            // 
            // MenuItem_PV
            // 
            MenuItem_PV.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_PC_Bearbeiten });
            MenuItem_PV.Name = "MenuItem_PV";
            resources.ApplyResources(MenuItem_PV, "MenuItem_PV");
            // 
            // MenuItem_PC_Bearbeiten
            // 
            MenuItem_PC_Bearbeiten.Name = "MenuItem_PC_Bearbeiten";
            resources.ApplyResources(MenuItem_PC_Bearbeiten, "MenuItem_PC_Bearbeiten");
            MenuItem_PC_Bearbeiten.Click += MenuItem_PV_Bearbeiten_Click;
            // 
            // MenuItem_Solarkollektoren
            // 
            MenuItem_Solarkollektoren.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_ST_Bearbeiten });
            MenuItem_Solarkollektoren.Name = "MenuItem_Solarkollektoren";
            resources.ApplyResources(MenuItem_Solarkollektoren, "MenuItem_Solarkollektoren");
            // 
            // MenuItem_ST_Bearbeiten
            // 
            MenuItem_ST_Bearbeiten.Name = "MenuItem_ST_Bearbeiten";
            resources.ApplyResources(MenuItem_ST_Bearbeiten, "MenuItem_ST_Bearbeiten");
            MenuItem_ST_Bearbeiten.Click += MenuItem_ST_Bearbeiten_Click;
            // 
            // MenuItem_SolThermGanglinie
            // 
            MenuItem_SolThermGanglinie.Name = "MenuItem_SolThermGanglinie";
            resources.ApplyResources(MenuItem_SolThermGanglinie, "MenuItem_SolThermGanglinie");
            MenuItem_SolThermGanglinie.Click += MenuItem_SolThermGanglinie_Click;
            // 
            // MenuItem_BHKW
            // 
            MenuItem_BHKW.Name = "MenuItem_BHKW";
            resources.ApplyResources(MenuItem_BHKW, "MenuItem_BHKW");
            MenuItem_BHKW.Click += MenuItem_BHKW_Click;
            // 
            // MenuItem_Klima
            // 
            MenuItem_Klima.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_Klimadaten });
            MenuItem_Klima.Image = Properties.Resources.Menu4;
            resources.ApplyResources(MenuItem_Klima, "MenuItem_Klima");
            MenuItem_Klima.Name = "MenuItem_Klima";
            // 
            // MenuItem_Klimadaten
            // 
            MenuItem_Klimadaten.Name = "MenuItem_Klimadaten";
            resources.ApplyResources(MenuItem_Klimadaten, "MenuItem_Klimadaten");
            MenuItem_Klimadaten.Click += MenuItem_Klimadaten_Click;
            // 
            // MenuItem_DatImport
            // 
            MenuItem_DatImport.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_Import_Heizkessel, MenuItem_PufferSp_VDI3805, MeniItem_VDI3805, MenuItem_PV_Import_CEC, MenuItem_ST_Import });
            MenuItem_DatImport.Image = Properties.Resources.Menue5;
            resources.ApplyResources(MenuItem_DatImport, "MenuItem_DatImport");
            MenuItem_DatImport.Name = "MenuItem_DatImport";
            // 
            // MenuItem_Import_Heizkessel
            // 
            MenuItem_Import_Heizkessel.Name = "MenuItem_Import_Heizkessel";
            resources.ApplyResources(MenuItem_Import_Heizkessel, "MenuItem_Import_Heizkessel");
            MenuItem_Import_Heizkessel.Click += MenuItem_Import_Heizkessel_Click;
            // 
            // MenuItem_PufferSp_VDI3805
            // 
            MenuItem_PufferSp_VDI3805.Name = "MenuItem_PufferSp_VDI3805";
            resources.ApplyResources(MenuItem_PufferSp_VDI3805, "MenuItem_PufferSp_VDI3805");
            MenuItem_PufferSp_VDI3805.Click += MeniItem_PufferSp_VDI3805_Click;
            // 
            // MeniItem_VDI3805
            // 
            MeniItem_VDI3805.Name = "MeniItem_VDI3805";
            resources.ApplyResources(MeniItem_VDI3805, "MeniItem_VDI3805");
            MeniItem_VDI3805.Click += MenuItem_WP_VDI3805_Click;
            // 
            // MenuItem_PV_Import_CEC
            // 
            MenuItem_PV_Import_CEC.Name = "MenuItem_PV_Import_CEC";
            resources.ApplyResources(MenuItem_PV_Import_CEC, "MenuItem_PV_Import_CEC");
            MenuItem_PV_Import_CEC.Click += MenuItem_PV_Import_CEC_Click;
            // 
            // MenuItem_ST_Import
            // 
            MenuItem_ST_Import.Name = "MenuItem_ST_Import";
            resources.ApplyResources(MenuItem_ST_Import, "MenuItem_ST_Import");
            MenuItem_ST_Import.Click += MenuItem_ST_Import_Click;
            // 
            // MenuItem_KostenVerwaltung
            // 
            resources.ApplyResources(MenuItem_KostenVerwaltung, "MenuItem_KostenVerwaltung");
            MenuItem_KostenVerwaltung.Name = "MenuItem_KostenVerwaltung";
            // 
            // MenuItem_Gebaeude
            // 
            MenuItem_Gebaeude.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_GebBearbeiten, MenuItem_GebTypen });
            MenuItem_Gebaeude.Image = Properties.Resources.Menue6;
            resources.ApplyResources(MenuItem_Gebaeude, "MenuItem_Gebaeude");
            MenuItem_Gebaeude.Name = "MenuItem_Gebaeude";
            // 
            // MenuItem_GebBearbeiten
            // 
            MenuItem_GebBearbeiten.Name = "MenuItem_GebBearbeiten";
            resources.ApplyResources(MenuItem_GebBearbeiten, "MenuItem_GebBearbeiten");
            MenuItem_GebBearbeiten.Click += MenuItem_GebBearbeiten_Click;
            // 
            // MenuItem_GebTypen
            // 
            MenuItem_GebTypen.Name = "MenuItem_GebTypen";
            resources.ApplyResources(MenuItem_GebTypen, "MenuItem_GebTypen");
            MenuItem_GebTypen.Click += MenuItem_GebTypen_Click;
            // 
            // MenuItem_Einstellungen
            // 
            MenuItem_Einstellungen.Image = Properties.Resources.einstellungen_32;
            resources.ApplyResources(MenuItem_Einstellungen, "MenuItem_Einstellungen");
            MenuItem_Einstellungen.Name = "MenuItem_Einstellungen";
            MenuItem_Einstellungen.Click += MenuItem_Einstellungen_Click;
            // 
            // Help
            // 
            Help.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] { MenuItem_Version, toolStripSeparator7, MenuItem_Lizenz, MenuItem_Dokumentation });
            resources.ApplyResources(Help, "Help");
            Help.Name = "Help";
            // 
            // MenuItem_Version
            // 
            MenuItem_Version.BackColor = System.Drawing.SystemColors.Control;
            MenuItem_Version.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            MenuItem_Version.Name = "MenuItem_Version";
            resources.ApplyResources(MenuItem_Version, "MenuItem_Version");
            MenuItem_Version.Click += MenuItem_Version_Click;
            // 
            // toolStripSeparator7
            // 
            toolStripSeparator7.Name = "toolStripSeparator7";
            resources.ApplyResources(toolStripSeparator7, "toolStripSeparator7");
            // 
            // MenuItem_Lizenz
            // 
            MenuItem_Lizenz.Name = "MenuItem_Lizenz";
            resources.ApplyResources(MenuItem_Lizenz, "MenuItem_Lizenz");
            MenuItem_Lizenz.Click += MenuItem_Lizenz_Click;
            // 
            // MenuItem_Dokumentation
            // 
            MenuItem_Dokumentation.Name = "MenuItem_Dokumentation";
            resources.ApplyResources(MenuItem_Dokumentation, "MenuItem_Dokumentation");
            MenuItem_Dokumentation.Click += MenuItem_Dokumentation_Click;
            // 
            // Deutsch
            // 
            Deutsch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            resources.ApplyResources(Deutsch, "Deutsch");
            Deutsch.Image = Properties.Resources.germany;
            Deutsch.Name = "Deutsch";
            Deutsch.Click += Deutsch_Click;
            // 
            // Englisch
            // 
            Englisch.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            resources.ApplyResources(Englisch, "Englisch");
            Englisch.Image = Properties.Resources.usa;
            Englisch.Name = "Englisch";
            Englisch.Click += Englisch_Click;
            // 
            // label_OnlineDoku
            // 
            resources.ApplyResources(label_OnlineDoku, "label_OnlineDoku");
            label_OnlineDoku.BackColor = System.Drawing.Color.Transparent;
            label_OnlineDoku.Name = "label_OnlineDoku";
            // 
            // MDIMainForm
            // 
            resources.ApplyResources(this, "$this");
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            Controls.Add(label_OnlineDoku);
            Controls.Add(menuToolbar);
            IsMdiContainer = true;
            MainMenuStrip = menuToolbar;
            Name = "MDIMainForm";
            WindowState = System.Windows.Forms.FormWindowState.Maximized;
            Load += MDIMainForm_Load;
            menuToolbar.ResumeLayout(false);
            menuToolbar.PerformLayout();
            ResumeLayout(false);
            PerformLayout();

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
        private System.Windows.Forms.Label label_OnlineDoku;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Einstellungen;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_Dokumentation;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_KostenVerwaltung;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_ExportImport;
    }
}

