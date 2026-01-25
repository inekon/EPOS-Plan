namespace WindowsFormsApplication1
{
    partial class FormMain
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMain));
            this.tabControl_Main = new System.Windows.Forms.TabControl();
            this.tabPage_Komponenten = new System.Windows.Forms.TabPage();
            this.comboBox_Klima = new System.Windows.Forms.ComboBox();
            this.button1 = new System.Windows.Forms.Button();
            this.btn_Speichern = new System.Windows.Forms.Button();
            this.btn_DragDestination = new System.Windows.Forms.Button();
            this.tabControl_Komponenten = new System.Windows.Forms.TabControl();
            this.tabPage5 = new System.Windows.Forms.TabPage();
            this.listView_Gebaeude = new System.Windows.Forms.ListView();
            this.tabPage9 = new System.Windows.Forms.TabPage();
            this.listView_WaermebedarfExtern = new System.Windows.Forms.ListView();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.listView_BHKW = new System.Windows.Forms.ListView();
            this.tabPage3 = new System.Windows.Forms.TabPage();
            this.listView_WP = new System.Windows.Forms.ListView();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.listView_SP = new System.Windows.Forms.ListView();
            this.tabPage6 = new System.Windows.Forms.TabPage();
            this.listView_Solar = new System.Windows.Forms.ListView();
            this.tabPage10 = new System.Windows.Forms.TabPage();
            this.listView_PV = new System.Windows.Forms.ListView();
            this.tabPage11 = new System.Windows.Forms.TabPage();
            this.listView_Heizkessel = new System.Windows.Forms.ListView();
            this.tabPage13 = new System.Windows.Forms.TabPage();
            this.listView_Prozesswaerme = new System.Windows.Forms.ListView();
            this.tabPage14 = new System.Windows.Forms.TabPage();
            this.listView_Strombedarf = new System.Windows.Forms.ListView();
            this.tabPage12 = new System.Windows.Forms.TabPage();
            this.listView_Stromganglinie = new System.Windows.Forms.ListView();
            this.textBox_Beschreibung = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.textBox_Datum = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.textBox_Kunde = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox_Bearbeiter = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox_Projekt = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button_Beenden = new System.Windows.Forms.Button();
            this.tabControl_Main.SuspendLayout();
            this.tabPage_Komponenten.SuspendLayout();
            this.tabControl_Komponenten.SuspendLayout();
            this.tabPage5.SuspendLayout();
            this.tabPage9.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage3.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.tabPage6.SuspendLayout();
            this.tabPage10.SuspendLayout();
            this.tabPage11.SuspendLayout();
            this.tabPage13.SuspendLayout();
            this.tabPage14.SuspendLayout();
            this.tabPage12.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl_Main
            // 
            resources.ApplyResources(this.tabControl_Main, "tabControl_Main");
            this.tabControl_Main.Controls.Add(this.tabPage_Komponenten);
            this.tabControl_Main.Name = "tabControl_Main";
            this.tabControl_Main.SelectedIndex = 0;
            // 
            // tabPage_Komponenten
            // 
            resources.ApplyResources(this.tabPage_Komponenten, "tabPage_Komponenten");
            this.tabPage_Komponenten.Controls.Add(this.comboBox_Klima);
            this.tabPage_Komponenten.Controls.Add(this.button1);
            this.tabPage_Komponenten.Controls.Add(this.btn_Speichern);
            this.tabPage_Komponenten.Controls.Add(this.btn_DragDestination);
            this.tabPage_Komponenten.Controls.Add(this.tabControl_Komponenten);
            this.tabPage_Komponenten.Controls.Add(this.textBox_Beschreibung);
            this.tabPage_Komponenten.Controls.Add(this.label9);
            this.tabPage_Komponenten.Controls.Add(this.textBox_Datum);
            this.tabPage_Komponenten.Controls.Add(this.label8);
            this.tabPage_Komponenten.Controls.Add(this.textBox_Kunde);
            this.tabPage_Komponenten.Controls.Add(this.label6);
            this.tabPage_Komponenten.Controls.Add(this.textBox_Bearbeiter);
            this.tabPage_Komponenten.Controls.Add(this.label5);
            this.tabPage_Komponenten.Controls.Add(this.textBox_Projekt);
            this.tabPage_Komponenten.Controls.Add(this.label4);
            this.tabPage_Komponenten.Controls.Add(this.label1);
            this.tabPage_Komponenten.Controls.Add(this.label3);
            this.tabPage_Komponenten.Controls.Add(this.label2);
            this.tabPage_Komponenten.Name = "tabPage_Komponenten";
            this.tabPage_Komponenten.UseVisualStyleBackColor = true;
            // 
            // comboBox_Klima
            // 
            resources.ApplyResources(this.comboBox_Klima, "comboBox_Klima");
            this.comboBox_Klima.FormattingEnabled = true;
            this.comboBox_Klima.Name = "comboBox_Klima";
            this.comboBox_Klima.SelectedIndexChanged += new System.EventHandler(this.comboBox_Klima_SelectedIndexChanged);
            // 
            // button1
            // 
            resources.ApplyResources(this.button1, "button1");
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.button1.ForeColor = System.Drawing.Color.Black;
            this.button1.Name = "button1";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // btn_Speichern
            // 
            resources.ApplyResources(this.btn_Speichern, "btn_Speichern");
            this.btn_Speichern.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btn_Speichern.ForeColor = System.Drawing.Color.Black;
            this.btn_Speichern.Image = global::WindowsFormsApplication1.Properties.Resources.speichern;
            this.btn_Speichern.Name = "btn_Speichern";
            this.btn_Speichern.UseVisualStyleBackColor = false;
            this.btn_Speichern.Click += new System.EventHandler(this.btn_Speichern_Click);
            // 
            // btn_DragDestination
            // 
            resources.ApplyResources(this.btn_DragDestination, "btn_DragDestination");
            this.btn_DragDestination.AllowDrop = true;
            this.btn_DragDestination.ForeColor = System.Drawing.Color.Black;
            this.btn_DragDestination.Name = "btn_DragDestination";
            this.btn_DragDestination.UseVisualStyleBackColor = true;
            this.btn_DragDestination.DragDrop += new System.Windows.Forms.DragEventHandler(this.button1_DragDrop);
            this.btn_DragDestination.DragEnter += new System.Windows.Forms.DragEventHandler(this.button1_DragEnter);
            this.btn_DragDestination.DragOver += new System.Windows.Forms.DragEventHandler(this.button1_DragOver);
            this.btn_DragDestination.MouseHover += new System.EventHandler(this.btn_DragDestination_MouseHover);
            // 
            // tabControl_Komponenten
            // 
            resources.ApplyResources(this.tabControl_Komponenten, "tabControl_Komponenten");
            this.tabControl_Komponenten.Controls.Add(this.tabPage5);
            this.tabControl_Komponenten.Controls.Add(this.tabPage9);
            this.tabControl_Komponenten.Controls.Add(this.tabPage1);
            this.tabControl_Komponenten.Controls.Add(this.tabPage3);
            this.tabControl_Komponenten.Controls.Add(this.tabPage4);
            this.tabControl_Komponenten.Controls.Add(this.tabPage6);
            this.tabControl_Komponenten.Controls.Add(this.tabPage10);
            this.tabControl_Komponenten.Controls.Add(this.tabPage11);
            this.tabControl_Komponenten.Controls.Add(this.tabPage13);
            this.tabControl_Komponenten.Controls.Add(this.tabPage14);
            this.tabControl_Komponenten.Controls.Add(this.tabPage12);
            this.tabControl_Komponenten.Multiline = true;
            this.tabControl_Komponenten.Name = "tabControl_Komponenten";
            this.tabControl_Komponenten.SelectedIndex = 0;
            // 
            // tabPage5
            // 
            resources.ApplyResources(this.tabPage5, "tabPage5");
            this.tabPage5.Controls.Add(this.listView_Gebaeude);
            this.tabPage5.Name = "tabPage5";
            this.tabPage5.UseVisualStyleBackColor = true;
            // 
            // listView_Gebaeude
            // 
            resources.ApplyResources(this.listView_Gebaeude, "listView_Gebaeude");
            this.listView_Gebaeude.BackColor = System.Drawing.Color.White;
            this.listView_Gebaeude.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_Gebaeude.FullRowSelect = true;
            this.listView_Gebaeude.GridLines = true;
            this.listView_Gebaeude.HideSelection = false;
            this.listView_Gebaeude.MultiSelect = false;
            this.listView_Gebaeude.Name = "listView_Gebaeude";
            this.listView_Gebaeude.UseCompatibleStateImageBehavior = false;
            this.listView_Gebaeude.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_Gebaeude_MouseDown);
            this.listView_Gebaeude.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_Gebaeude_MouseMove);
            this.listView_Gebaeude.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_Gebaeude_MouseUp);
            // 
            // tabPage9
            // 
            resources.ApplyResources(this.tabPage9, "tabPage9");
            this.tabPage9.Controls.Add(this.listView_WaermebedarfExtern);
            this.tabPage9.Name = "tabPage9";
            this.tabPage9.UseVisualStyleBackColor = true;
            // 
            // listView_WaermebedarfExtern
            // 
            resources.ApplyResources(this.listView_WaermebedarfExtern, "listView_WaermebedarfExtern");
            this.listView_WaermebedarfExtern.BackColor = System.Drawing.Color.White;
            this.listView_WaermebedarfExtern.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_WaermebedarfExtern.FullRowSelect = true;
            this.listView_WaermebedarfExtern.GridLines = true;
            this.listView_WaermebedarfExtern.HideSelection = false;
            this.listView_WaermebedarfExtern.MultiSelect = false;
            this.listView_WaermebedarfExtern.Name = "listView_WaermebedarfExtern";
            this.listView_WaermebedarfExtern.UseCompatibleStateImageBehavior = false;
            this.listView_WaermebedarfExtern.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_WaermebedarfExtern_MouseDown);
            this.listView_WaermebedarfExtern.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_WaermebedarfExtern_MouseMove);
            this.listView_WaermebedarfExtern.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_WaermebedarfExtern_MouseUp);
            // 
            // tabPage1
            // 
            resources.ApplyResources(this.tabPage1, "tabPage1");
            this.tabPage1.Controls.Add(this.listView_BHKW);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // listView_BHKW
            // 
            resources.ApplyResources(this.listView_BHKW, "listView_BHKW");
            this.listView_BHKW.BackColor = System.Drawing.Color.White;
            this.listView_BHKW.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_BHKW.FullRowSelect = true;
            this.listView_BHKW.GridLines = true;
            this.listView_BHKW.HideSelection = false;
            this.listView_BHKW.MultiSelect = false;
            this.listView_BHKW.Name = "listView_BHKW";
            this.listView_BHKW.UseCompatibleStateImageBehavior = false;
            this.listView_BHKW.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_BHKW_MouseDown);
            this.listView_BHKW.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_BHKW_MouseMove);
            this.listView_BHKW.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_BHKW_MouseUp);
            // 
            // tabPage3
            // 
            resources.ApplyResources(this.tabPage3, "tabPage3");
            this.tabPage3.Controls.Add(this.listView_WP);
            this.tabPage3.Name = "tabPage3";
            this.tabPage3.UseVisualStyleBackColor = true;
            // 
            // listView_WP
            // 
            resources.ApplyResources(this.listView_WP, "listView_WP");
            this.listView_WP.BackColor = System.Drawing.Color.White;
            this.listView_WP.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_WP.FullRowSelect = true;
            this.listView_WP.GridLines = true;
            this.listView_WP.HideSelection = false;
            this.listView_WP.MultiSelect = false;
            this.listView_WP.Name = "listView_WP";
            this.listView_WP.UseCompatibleStateImageBehavior = false;
            this.listView_WP.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_WP_MouseDown);
            this.listView_WP.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_WP_MouseMove);
            this.listView_WP.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_WP_MouseUp);
            // 
            // tabPage4
            // 
            resources.ApplyResources(this.tabPage4, "tabPage4");
            this.tabPage4.Controls.Add(this.listView_SP);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // listView_SP
            // 
            resources.ApplyResources(this.listView_SP, "listView_SP");
            this.listView_SP.BackColor = System.Drawing.Color.White;
            this.listView_SP.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_SP.FullRowSelect = true;
            this.listView_SP.GridLines = true;
            this.listView_SP.HideSelection = false;
            this.listView_SP.MultiSelect = false;
            this.listView_SP.Name = "listView_SP";
            this.listView_SP.UseCompatibleStateImageBehavior = false;
            this.listView_SP.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_SP_MouseDown);
            this.listView_SP.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_SP_MouseMove);
            this.listView_SP.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_SP_MouseUp);
            // 
            // tabPage6
            // 
            resources.ApplyResources(this.tabPage6, "tabPage6");
            this.tabPage6.Controls.Add(this.listView_Solar);
            this.tabPage6.Name = "tabPage6";
            this.tabPage6.UseVisualStyleBackColor = true;
            // 
            // listView_Solar
            // 
            resources.ApplyResources(this.listView_Solar, "listView_Solar");
            this.listView_Solar.BackColor = System.Drawing.Color.White;
            this.listView_Solar.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_Solar.FullRowSelect = true;
            this.listView_Solar.GridLines = true;
            this.listView_Solar.HideSelection = false;
            this.listView_Solar.MultiSelect = false;
            this.listView_Solar.Name = "listView_Solar";
            this.listView_Solar.UseCompatibleStateImageBehavior = false;
            this.listView_Solar.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_Solar_MouseDown);
            this.listView_Solar.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_Solar_MouseMove);
            this.listView_Solar.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_Solar_MouseUp);
            // 
            // tabPage10
            // 
            resources.ApplyResources(this.tabPage10, "tabPage10");
            this.tabPage10.Controls.Add(this.listView_PV);
            this.tabPage10.Name = "tabPage10";
            this.tabPage10.UseVisualStyleBackColor = true;
            // 
            // listView_PV
            // 
            resources.ApplyResources(this.listView_PV, "listView_PV");
            this.listView_PV.BackColor = System.Drawing.Color.White;
            this.listView_PV.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_PV.FullRowSelect = true;
            this.listView_PV.GridLines = true;
            this.listView_PV.HideSelection = false;
            this.listView_PV.MultiSelect = false;
            this.listView_PV.Name = "listView_PV";
            this.listView_PV.UseCompatibleStateImageBehavior = false;
            this.listView_PV.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_PV_MouseDown);
            this.listView_PV.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_PV_MouseMove);
            this.listView_PV.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_PV_MouseUp);
            // 
            // tabPage11
            // 
            resources.ApplyResources(this.tabPage11, "tabPage11");
            this.tabPage11.Controls.Add(this.listView_Heizkessel);
            this.tabPage11.Name = "tabPage11";
            this.tabPage11.UseVisualStyleBackColor = true;
            // 
            // listView_Heizkessel
            // 
            resources.ApplyResources(this.listView_Heizkessel, "listView_Heizkessel");
            this.listView_Heizkessel.BackColor = System.Drawing.Color.White;
            this.listView_Heizkessel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_Heizkessel.FullRowSelect = true;
            this.listView_Heizkessel.GridLines = true;
            this.listView_Heizkessel.HideSelection = false;
            this.listView_Heizkessel.MultiSelect = false;
            this.listView_Heizkessel.Name = "listView_Heizkessel";
            this.listView_Heizkessel.UseCompatibleStateImageBehavior = false;
            this.listView_Heizkessel.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_Heizkessel_MouseDown);
            this.listView_Heizkessel.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_Heizkessel_MouseMove);
            this.listView_Heizkessel.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_Heizkessel_MouseUp);
            // 
            // tabPage13
            // 
            resources.ApplyResources(this.tabPage13, "tabPage13");
            this.tabPage13.Controls.Add(this.listView_Prozesswaerme);
            this.tabPage13.Name = "tabPage13";
            this.tabPage13.UseVisualStyleBackColor = true;
            // 
            // listView_Prozesswaerme
            // 
            resources.ApplyResources(this.listView_Prozesswaerme, "listView_Prozesswaerme");
            this.listView_Prozesswaerme.BackColor = System.Drawing.Color.White;
            this.listView_Prozesswaerme.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_Prozesswaerme.FullRowSelect = true;
            this.listView_Prozesswaerme.GridLines = true;
            this.listView_Prozesswaerme.HideSelection = false;
            this.listView_Prozesswaerme.MultiSelect = false;
            this.listView_Prozesswaerme.Name = "listView_Prozesswaerme";
            this.listView_Prozesswaerme.UseCompatibleStateImageBehavior = false;
            this.listView_Prozesswaerme.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_Prozesswaerme_MouseDown);
            this.listView_Prozesswaerme.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_Prozesswaerme_MouseMove);
            this.listView_Prozesswaerme.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_Prozesswaerme_MouseUp);
            // 
            // tabPage14
            // 
            resources.ApplyResources(this.tabPage14, "tabPage14");
            this.tabPage14.Controls.Add(this.listView_Strombedarf);
            this.tabPage14.Name = "tabPage14";
            this.tabPage14.UseVisualStyleBackColor = true;
            // 
            // listView_Strombedarf
            // 
            resources.ApplyResources(this.listView_Strombedarf, "listView_Strombedarf");
            this.listView_Strombedarf.BackColor = System.Drawing.Color.White;
            this.listView_Strombedarf.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_Strombedarf.FullRowSelect = true;
            this.listView_Strombedarf.GridLines = true;
            this.listView_Strombedarf.HideSelection = false;
            this.listView_Strombedarf.MultiSelect = false;
            this.listView_Strombedarf.Name = "listView_Strombedarf";
            this.listView_Strombedarf.UseCompatibleStateImageBehavior = false;
            this.listView_Strombedarf.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_Strombedarf_MouseDown);
            this.listView_Strombedarf.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_Strombedarf_MouseMove);
            this.listView_Strombedarf.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_Strombedarf_MouseUp);
            // 
            // tabPage12
            // 
            resources.ApplyResources(this.tabPage12, "tabPage12");
            this.tabPage12.Controls.Add(this.listView_Stromganglinie);
            this.tabPage12.Name = "tabPage12";
            this.tabPage12.UseVisualStyleBackColor = true;
            // 
            // listView_Stromganglinie
            // 
            resources.ApplyResources(this.listView_Stromganglinie, "listView_Stromganglinie");
            this.listView_Stromganglinie.BackColor = System.Drawing.Color.White;
            this.listView_Stromganglinie.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.listView_Stromganglinie.FullRowSelect = true;
            this.listView_Stromganglinie.GridLines = true;
            this.listView_Stromganglinie.HideSelection = false;
            this.listView_Stromganglinie.MultiSelect = false;
            this.listView_Stromganglinie.Name = "listView_Stromganglinie";
            this.listView_Stromganglinie.UseCompatibleStateImageBehavior = false;
            this.listView_Stromganglinie.MouseDown += new System.Windows.Forms.MouseEventHandler(this.listView_Stromganglinie_MouseDown);
            this.listView_Stromganglinie.MouseMove += new System.Windows.Forms.MouseEventHandler(this.listView_Stromganglinie_MouseMove);
            this.listView_Stromganglinie.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_Stromganglinie_MouseUp);
            // 
            // textBox_Beschreibung
            // 
            resources.ApplyResources(this.textBox_Beschreibung, "textBox_Beschreibung");
            this.textBox_Beschreibung.BackColor = System.Drawing.Color.White;
            this.textBox_Beschreibung.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Beschreibung.ForeColor = System.Drawing.Color.Black;
            this.textBox_Beschreibung.Name = "textBox_Beschreibung";
            // 
            // label9
            // 
            resources.ApplyResources(this.label9, "label9");
            this.label9.BackColor = System.Drawing.Color.White;
            this.label9.ForeColor = System.Drawing.Color.Black;
            this.label9.Name = "label9";
            // 
            // textBox_Datum
            // 
            resources.ApplyResources(this.textBox_Datum, "textBox_Datum");
            this.textBox_Datum.BackColor = System.Drawing.Color.White;
            this.textBox_Datum.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Datum.ForeColor = System.Drawing.Color.Black;
            this.textBox_Datum.Name = "textBox_Datum";
            this.textBox_Datum.ReadOnly = true;
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.BackColor = System.Drawing.Color.White;
            this.label8.ForeColor = System.Drawing.Color.Black;
            this.label8.Name = "label8";
            // 
            // textBox_Kunde
            // 
            resources.ApplyResources(this.textBox_Kunde, "textBox_Kunde");
            this.textBox_Kunde.BackColor = System.Drawing.Color.White;
            this.textBox_Kunde.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Kunde.ForeColor = System.Drawing.Color.Black;
            this.textBox_Kunde.Name = "textBox_Kunde";
            // 
            // label6
            // 
            resources.ApplyResources(this.label6, "label6");
            this.label6.BackColor = System.Drawing.Color.White;
            this.label6.ForeColor = System.Drawing.Color.Black;
            this.label6.Name = "label6";
            // 
            // textBox_Bearbeiter
            // 
            resources.ApplyResources(this.textBox_Bearbeiter, "textBox_Bearbeiter");
            this.textBox_Bearbeiter.BackColor = System.Drawing.Color.White;
            this.textBox_Bearbeiter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Bearbeiter.ForeColor = System.Drawing.Color.Black;
            this.textBox_Bearbeiter.Name = "textBox_Bearbeiter";
            // 
            // label5
            // 
            resources.ApplyResources(this.label5, "label5");
            this.label5.BackColor = System.Drawing.Color.White;
            this.label5.ForeColor = System.Drawing.Color.Black;
            this.label5.Name = "label5";
            // 
            // textBox_Projekt
            // 
            resources.ApplyResources(this.textBox_Projekt, "textBox_Projekt");
            this.textBox_Projekt.BackColor = System.Drawing.Color.White;
            this.textBox_Projekt.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox_Projekt.Name = "textBox_Projekt";
            this.textBox_Projekt.ReadOnly = true;
            // 
            // label4
            // 
            resources.ApplyResources(this.label4, "label4");
            this.label4.BackColor = System.Drawing.Color.SteelBlue;
            this.label4.ForeColor = System.Drawing.Color.White;
            this.label4.Name = "label4";
            // 
            // label1
            // 
            resources.ApplyResources(this.label1, "label1");
            this.label1.BackColor = System.Drawing.Color.SteelBlue;
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Name = "label1";
            // 
            // label3
            // 
            resources.ApplyResources(this.label3, "label3");
            this.label3.BackColor = System.Drawing.Color.White;
            this.label3.ForeColor = System.Drawing.Color.Black;
            this.label3.Name = "label3";
            // 
            // label2
            // 
            resources.ApplyResources(this.label2, "label2");
            this.label2.BackColor = System.Drawing.Color.White;
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Name = "label2";
            // 
            // button_Beenden
            // 
            resources.ApplyResources(this.button_Beenden, "button_Beenden");
            this.button_Beenden.ForeColor = System.Drawing.Color.Black;
            this.button_Beenden.Name = "button_Beenden";
            this.button_Beenden.UseVisualStyleBackColor = true;
            this.button_Beenden.Click += new System.EventHandler(this.button_Beenden_Click);
            // 
            // FormMain
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ControlBox = false;
            this.Controls.Add(this.tabControl_Main);
            this.Controls.Add(this.button_Beenden);
            this.ForeColor = System.Drawing.Color.White;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "FormMain";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.tabControl_Main.ResumeLayout(false);
            this.tabPage_Komponenten.ResumeLayout(false);
            this.tabPage_Komponenten.PerformLayout();
            this.tabControl_Komponenten.ResumeLayout(false);
            this.tabPage5.ResumeLayout(false);
            this.tabPage9.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage3.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.tabPage6.ResumeLayout(false);
            this.tabPage10.ResumeLayout(false);
            this.tabPage11.ResumeLayout(false);
            this.tabPage13.ResumeLayout(false);
            this.tabPage14.ResumeLayout(false);
            this.tabPage12.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl_Main;
        private System.Windows.Forms.TabPage tabPage_Komponenten;
        private System.Windows.Forms.ComboBox comboBox_Klima;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button btn_Speichern;
        private System.Windows.Forms.Button btn_DragDestination;
        private System.Windows.Forms.TabControl tabControl_Komponenten;
        private System.Windows.Forms.TabPage tabPage5;
        private System.Windows.Forms.ListView listView_Gebaeude;
        private System.Windows.Forms.TabPage tabPage9;
        private System.Windows.Forms.ListView listView_WaermebedarfExtern;
        private System.Windows.Forms.TabPage tabPage3;
        private System.Windows.Forms.ListView listView_WP;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.ListView listView_SP;
        private System.Windows.Forms.TabPage tabPage6;
        private System.Windows.Forms.TabPage tabPage10;
        private System.Windows.Forms.TabPage tabPage11;
        private System.Windows.Forms.ListView listView_Heizkessel;
        private System.Windows.Forms.TabPage tabPage13;
        private System.Windows.Forms.ListView listView_Prozesswaerme;
        private System.Windows.Forms.TabPage tabPage14;
        private System.Windows.Forms.ListView listView_Strombedarf;
        private System.Windows.Forms.TabPage tabPage12;
        private System.Windows.Forms.ListView listView_Stromganglinie;
        private System.Windows.Forms.TextBox textBox_Beschreibung;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox textBox_Datum;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox textBox_Kunde;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox_Bearbeiter;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox_Projekt;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button_Beenden;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ListView listView_BHKW;
        private System.Windows.Forms.ListView listView_Solar;
        private System.Windows.Forms.ListView listView_PV;
    }
}