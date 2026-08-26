namespace WindowsFormsApplication1
{
    partial class Form_PhotovoltaikVerguetung
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlKopf = new System.Windows.Forms.Panel();
            this.chkAktiv = new System.Windows.Forms.CheckBox();
            this.lblKopfTitel = new System.Windows.Forms.Label();
            this.grpAnlage = new System.Windows.Forms.GroupBox();
            this.lblKwp = new System.Windows.Forms.Label();
            this.lblKwpWert = new System.Windows.Forms.Label();
            this.lblKwpOverride = new System.Windows.Forms.Label();
            this.numKwpOverride = new System.Windows.Forms.NumericUpDown();
            this.lblIbn = new System.Windows.Forms.Label();
            this.dtpIbn = new System.Windows.Forms.DateTimePicker();
            this.rbUeberschuss = new System.Windows.Forms.RadioButton();
            this.rbVoll = new System.Windows.Forms.RadioButton();
            this.lblAnlageWarnung = new System.Windows.Forms.Label();
            this.grpVermarktung = new System.Windows.Forms.GroupBox();
            this.rbEv = new System.Windows.Forms.RadioButton();
            this.rbMarktpraemie = new System.Windows.Forms.RadioButton();
            this.rbPpa = new System.Windows.Forms.RadioButton();
            this.rbKeine = new System.Windows.Forms.RadioButton();
            this.lblDv = new System.Windows.Forms.Label();
            this.numDv = new System.Windows.Forms.NumericUpDown();
            this.lblPpaPreis = new System.Windows.Forms.Label();
            this.numPpaPreis = new System.Windows.Forms.NumericUpDown();
            this.lblPpaAufschlag = new System.Windows.Forms.Label();
            this.numPpaAufschlag = new System.Windows.Forms.NumericUpDown();
            this.lblVermarktungHinweis = new System.Windows.Forms.Label();
            this.grpAw = new System.Windows.Forms.GroupBox();
            this.lblAwWert = new System.Windows.Forms.Label();
            this.lblAwHerkunft = new System.Windows.Forms.Label();
            this.lblAwOverride = new System.Windows.Forms.Label();
            this.numAwOverride = new System.Windows.Forms.NumericUpDown();
            this.lblEv = new System.Windows.Forms.Label();
            this.grpPar51 = new System.Windows.Forms.GroupBox();
            this.lblPar51 = new System.Windows.Forms.Label();
            this.cmbPar51 = new System.Windows.Forms.ComboBox();
            this.lblPar51Status = new System.Windows.Forms.Label();
            this.lblIMSys = new System.Windows.Forms.Label();
            this.numIMSys = new System.Windows.Forms.NumericUpDown();
            this.lblAusfall = new System.Windows.Forms.Label();
            this.numAusfall = new System.Windows.Forms.NumericUpDown();
            this.chk51a = new System.Windows.Forms.CheckBox();
            this.grpBezug = new System.Windows.Forms.GroupBox();
            this.chkBezugReihe = new System.Windows.Forms.CheckBox();
            this.lblStromsteuer = new System.Windows.Forms.Label();
            this.grpKappung = new System.Windows.Forms.GroupBox();
            this.lblKappung = new System.Windows.Forms.Label();
            this.cmbKappung = new System.Windows.Forms.ComboBox();
            this.lblKappungStatus = new System.Windows.Forms.Label();
            this.pnlVorschauKopf = new System.Windows.Forms.Panel();
            this.lblVorschauTitel = new System.Windows.Forms.Label();
            this.lblVorschau = new System.Windows.Forms.Label();
            this.lblKennzahlen = new System.Windows.Forms.Label();
            this.btnMarktwerte = new System.Windows.Forms.Button();
            this.btnUebernehmen = new System.Windows.Forms.Button();
            this.btnAbbrechen = new System.Windows.Forms.Button();
            this.btnTarifPv = new System.Windows.Forms.Button();
            this.pnlKopf.SuspendLayout();
            this.grpAnlage.SuspendLayout();
            this.grpVermarktung.SuspendLayout();
            this.grpAw.SuspendLayout();
            this.grpPar51.SuspendLayout();
            this.grpBezug.SuspendLayout();
            this.grpKappung.SuspendLayout();
            this.pnlVorschauKopf.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numKwpOverride)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDv)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPpaPreis)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPpaAufschlag)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAwOverride)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numIMSys)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAusfall)).BeginInit();
            this.SuspendLayout();
            //
            // pnlKopf
            //
            this.pnlKopf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(31)))), ((int)(((byte)(61)))));
            this.pnlKopf.Controls.Add(this.chkAktiv);
            this.pnlKopf.Controls.Add(this.lblKopfTitel);
            this.pnlKopf.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlKopf.Location = new System.Drawing.Point(0, 0);
            this.pnlKopf.Name = "pnlKopf";
            this.pnlKopf.Size = new System.Drawing.Size(914, 48);
            this.pnlKopf.TabIndex = 0;
            //
            // lblKopfTitel
            //
            this.lblKopfTitel.AutoSize = true;
            this.lblKopfTitel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblKopfTitel.ForeColor = System.Drawing.Color.White;
            this.lblKopfTitel.Location = new System.Drawing.Point(14, 11);
            this.lblKopfTitel.Name = "lblKopfTitel";
            this.lblKopfTitel.Size = new System.Drawing.Size(179, 21);
            this.lblKopfTitel.TabIndex = 0;
            this.lblKopfTitel.Text = "PV-Vergütung (EEG)";
            //
            // chkAktiv
            //
            this.chkAktiv.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.chkAktiv.AutoSize = true;
            this.chkAktiv.ForeColor = System.Drawing.Color.White;
            this.chkAktiv.Location = new System.Drawing.Point(756, 15);
            this.chkAktiv.Name = "chkAktiv";
            this.chkAktiv.Size = new System.Drawing.Size(145, 19);
            this.chkAktiv.TabIndex = 1;
            this.chkAktiv.Text = "Vergütung anwenden";
            this.chkAktiv.CheckedChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            // grpAnlage
            //
            this.grpAnlage.Controls.Add(this.lblKwp);
            this.grpAnlage.Controls.Add(this.lblKwpWert);
            this.grpAnlage.Controls.Add(this.lblKwpOverride);
            this.grpAnlage.Controls.Add(this.numKwpOverride);
            this.grpAnlage.Controls.Add(this.lblIbn);
            this.grpAnlage.Controls.Add(this.dtpIbn);
            this.grpAnlage.Controls.Add(this.rbUeberschuss);
            this.grpAnlage.Controls.Add(this.rbVoll);
            this.grpAnlage.Controls.Add(this.lblAnlageWarnung);
            this.grpAnlage.Location = new System.Drawing.Point(16, 60);
            this.grpAnlage.Name = "grpAnlage";
            this.grpAnlage.Size = new System.Drawing.Size(432, 156);
            this.grpAnlage.TabIndex = 1;
            this.grpAnlage.TabStop = false;
            this.grpAnlage.Text = "Anlage";
            //
            this.lblKwp.AutoSize = true;
            this.lblKwp.Location = new System.Drawing.Point(12, 26);
            this.lblKwp.Name = "lblKwp";
            this.lblKwp.Size = new System.Drawing.Size(122, 15);
            this.lblKwp.Text = "Installierte Leistung:";
            //
            this.lblKwpWert.AutoSize = true;
            this.lblKwpWert.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblKwpWert.Location = new System.Drawing.Point(190, 26);
            this.lblKwpWert.Name = "lblKwpWert";
            this.lblKwpWert.Size = new System.Drawing.Size(52, 15);
            this.lblKwpWert.Text = "0,0 kWp";
            //
            this.lblKwpOverride.AutoSize = true;
            this.lblKwpOverride.Location = new System.Drawing.Point(12, 56);
            this.lblKwpOverride.Name = "lblKwpOverride";
            this.lblKwpOverride.Size = new System.Drawing.Size(160, 15);
            this.lblKwpOverride.Text = "Override [kWp] (0 = keiner):";
            //
            this.numKwpOverride.DecimalPlaces = 2;
            this.numKwpOverride.Location = new System.Drawing.Point(190, 52);
            this.numKwpOverride.Maximum = new decimal(new int[] { 100000, 0, 0, 0 });
            this.numKwpOverride.Name = "numKwpOverride";
            this.numKwpOverride.Size = new System.Drawing.Size(90, 23);
            this.numKwpOverride.TabIndex = 1;
            this.numKwpOverride.ValueChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblIbn.AutoSize = true;
            this.lblIbn.Location = new System.Drawing.Point(12, 88);
            this.lblIbn.Name = "lblIbn";
            this.lblIbn.Size = new System.Drawing.Size(120, 15);
            this.lblIbn.Text = "Inbetriebnahme:";
            //
            this.dtpIbn.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtpIbn.Location = new System.Drawing.Point(190, 84);
            this.dtpIbn.Name = "dtpIbn";
            this.dtpIbn.Size = new System.Drawing.Size(110, 23);
            this.dtpIbn.TabIndex = 2;
            this.dtpIbn.ValueChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.rbUeberschuss.AutoSize = true;
            this.rbUeberschuss.Checked = true;
            this.rbUeberschuss.Location = new System.Drawing.Point(15, 118);
            this.rbUeberschuss.Name = "rbUeberschuss";
            this.rbUeberschuss.Size = new System.Drawing.Size(150, 19);
            this.rbUeberschuss.TabIndex = 3;
            this.rbUeberschuss.TabStop = true;
            this.rbUeberschuss.Text = "Überschusseinspeisung";
            this.rbUeberschuss.CheckedChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.rbVoll.AutoSize = true;
            this.rbVoll.Location = new System.Drawing.Point(190, 118);
            this.rbVoll.Name = "rbVoll";
            this.rbVoll.Size = new System.Drawing.Size(115, 19);
            this.rbVoll.TabIndex = 4;
            this.rbVoll.Text = "Volleinspeisung";
            this.rbVoll.CheckedChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblAnlageWarnung.AutoSize = true;
            this.lblAnlageWarnung.ForeColor = System.Drawing.Color.Firebrick;
            this.lblAnlageWarnung.Location = new System.Drawing.Point(310, 26);
            this.lblAnlageWarnung.MaximumSize = new System.Drawing.Size(115, 0);
            this.lblAnlageWarnung.Name = "lblAnlageWarnung";
            this.lblAnlageWarnung.Size = new System.Drawing.Size(0, 15);
            //
            // grpVermarktung
            //
            this.grpVermarktung.Controls.Add(this.rbEv);
            this.grpVermarktung.Controls.Add(this.rbMarktpraemie);
            this.grpVermarktung.Controls.Add(this.rbPpa);
            this.grpVermarktung.Controls.Add(this.rbKeine);
            this.grpVermarktung.Controls.Add(this.lblDv);
            this.grpVermarktung.Controls.Add(this.numDv);
            this.grpVermarktung.Controls.Add(this.lblPpaPreis);
            this.grpVermarktung.Controls.Add(this.numPpaPreis);
            this.grpVermarktung.Controls.Add(this.lblPpaAufschlag);
            this.grpVermarktung.Controls.Add(this.numPpaAufschlag);
            this.grpVermarktung.Controls.Add(this.lblVermarktungHinweis);
            this.grpVermarktung.Location = new System.Drawing.Point(16, 224);
            this.grpVermarktung.Name = "grpVermarktung";
            this.grpVermarktung.Size = new System.Drawing.Size(432, 216);
            this.grpVermarktung.TabIndex = 2;
            this.grpVermarktung.TabStop = false;
            this.grpVermarktung.Text = "Vermarktung";
            //
            this.rbEv.AutoSize = true;
            this.rbEv.Checked = true;
            this.rbEv.Location = new System.Drawing.Point(15, 24);
            this.rbEv.Name = "rbEv";
            this.rbEv.Size = new System.Drawing.Size(190, 19);
            this.rbEv.TabIndex = 0;
            this.rbEv.TabStop = true;
            this.rbEv.Text = "Feste Einspeisevergütung";
            this.rbEv.CheckedChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.rbMarktpraemie.AutoSize = true;
            this.rbMarktpraemie.Location = new System.Drawing.Point(15, 49);
            this.rbMarktpraemie.Name = "rbMarktpraemie";
            this.rbMarktpraemie.Size = new System.Drawing.Size(230, 19);
            this.rbMarktpraemie.TabIndex = 1;
            this.rbMarktpraemie.Text = "Direktvermarktung mit Marktprämie";
            this.rbMarktpraemie.CheckedChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.rbPpa.AutoSize = true;
            this.rbPpa.Location = new System.Drawing.Point(15, 74);
            this.rbPpa.Name = "rbPpa";
            this.rbPpa.Size = new System.Drawing.Size(220, 19);
            this.rbPpa.TabIndex = 2;
            this.rbPpa.Text = "Sonstige Direktvermarktung / PPA";
            this.rbPpa.CheckedChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.rbKeine.AutoSize = true;
            this.rbKeine.Location = new System.Drawing.Point(15, 99);
            this.rbKeine.Name = "rbKeine";
            this.rbKeine.Size = new System.Drawing.Size(230, 19);
            this.rbKeine.TabIndex = 3;
            this.rbKeine.Text = "Keine Vergütung (unentgeltlich)";
            this.rbKeine.CheckedChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblDv.AutoSize = true;
            this.lblDv.Location = new System.Drawing.Point(12, 130);
            this.lblDv.Name = "lblDv";
            this.lblDv.Size = new System.Drawing.Size(140, 15);
            this.lblDv.Text = "DV-Entgelt [ct/kWh]:";
            //
            this.numDv.DecimalPlaces = 2;
            this.numDv.Increment = new decimal(new int[] { 5, 0, 0, 131072 });
            this.numDv.Location = new System.Drawing.Point(250, 126);
            this.numDv.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numDv.Name = "numDv";
            this.numDv.Size = new System.Drawing.Size(80, 23);
            this.numDv.TabIndex = 4;
            this.numDv.ValueChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblPpaPreis.AutoSize = true;
            this.lblPpaPreis.Location = new System.Drawing.Point(12, 158);
            this.lblPpaPreis.Name = "lblPpaPreis";
            this.lblPpaPreis.Size = new System.Drawing.Size(180, 15);
            this.lblPpaPreis.Text = "PPA-Festpreis [ct/kWh] (0 = keiner):";
            //
            this.numPpaPreis.DecimalPlaces = 2;
            this.numPpaPreis.Location = new System.Drawing.Point(250, 154);
            this.numPpaPreis.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numPpaPreis.Name = "numPpaPreis";
            this.numPpaPreis.Size = new System.Drawing.Size(80, 23);
            this.numPpaPreis.TabIndex = 5;
            this.numPpaPreis.ValueChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblPpaAufschlag.AutoSize = true;
            this.lblPpaAufschlag.Location = new System.Drawing.Point(12, 186);
            this.lblPpaAufschlag.Name = "lblPpaAufschlag";
            this.lblPpaAufschlag.Size = new System.Drawing.Size(190, 15);
            this.lblPpaAufschlag.Text = "PPA-Aufschlag auf Spot [ct/kWh]:";
            //
            this.numPpaAufschlag.DecimalPlaces = 2;
            this.numPpaAufschlag.Location = new System.Drawing.Point(250, 182);
            this.numPpaAufschlag.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numPpaAufschlag.Minimum = new decimal(new int[] { 100, 0, 0, -2147483648 });
            this.numPpaAufschlag.Name = "numPpaAufschlag";
            this.numPpaAufschlag.Size = new System.Drawing.Size(80, 23);
            this.numPpaAufschlag.TabIndex = 6;
            this.numPpaAufschlag.ValueChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblVermarktungHinweis.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblVermarktungHinweis.Location = new System.Drawing.Point(345, 126);
            this.lblVermarktungHinweis.Name = "lblVermarktungHinweis";
            this.lblVermarktungHinweis.Size = new System.Drawing.Size(80, 80);
            this.lblVermarktungHinweis.Text = "";
            //
            // grpAw
            //
            this.grpAw.Controls.Add(this.lblAwWert);
            this.grpAw.Controls.Add(this.lblAwHerkunft);
            this.grpAw.Controls.Add(this.lblAwOverride);
            this.grpAw.Controls.Add(this.numAwOverride);
            this.grpAw.Controls.Add(this.lblEv);
            this.grpAw.Location = new System.Drawing.Point(464, 60);
            this.grpAw.Name = "grpAw";
            this.grpAw.Size = new System.Drawing.Size(434, 156);
            this.grpAw.TabIndex = 3;
            this.grpAw.TabStop = false;
            this.grpAw.Text = "Anzulegender Wert";
            //
            this.lblAwWert.AutoSize = true;
            this.lblAwWert.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
            this.lblAwWert.Location = new System.Drawing.Point(12, 24);
            this.lblAwWert.Name = "lblAwWert";
            this.lblAwWert.Size = new System.Drawing.Size(100, 19);
            this.lblAwWert.Text = "AW_mix: —";
            //
            this.lblAwHerkunft.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblAwHerkunft.Location = new System.Drawing.Point(12, 48);
            this.lblAwHerkunft.Name = "lblAwHerkunft";
            this.lblAwHerkunft.Size = new System.Drawing.Size(410, 48);
            this.lblAwHerkunft.Text = "";
            //
            this.lblAwOverride.AutoSize = true;
            this.lblAwOverride.Location = new System.Drawing.Point(12, 102);
            this.lblAwOverride.Name = "lblAwOverride";
            this.lblAwOverride.Size = new System.Drawing.Size(210, 15);
            this.lblAwOverride.Text = "AW-Override [ct/kWh] (0 = Katalog):";
            //
            this.numAwOverride.DecimalPlaces = 2;
            this.numAwOverride.Location = new System.Drawing.Point(250, 98);
            this.numAwOverride.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numAwOverride.Name = "numAwOverride";
            this.numAwOverride.Size = new System.Drawing.Size(80, 23);
            this.numAwOverride.TabIndex = 1;
            this.numAwOverride.ValueChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblEv.AutoSize = true;
            this.lblEv.Location = new System.Drawing.Point(12, 128);
            this.lblEv.Name = "lblEv";
            this.lblEv.Size = new System.Drawing.Size(170, 15);
            this.lblEv.Text = "Feste EV (AW − 0,40): —";
            //
            // grpPar51
            //
            this.grpPar51.Controls.Add(this.lblPar51);
            this.grpPar51.Controls.Add(this.cmbPar51);
            this.grpPar51.Controls.Add(this.lblPar51Status);
            this.grpPar51.Controls.Add(this.lblIMSys);
            this.grpPar51.Controls.Add(this.numIMSys);
            this.grpPar51.Controls.Add(this.lblAusfall);
            this.grpPar51.Controls.Add(this.numAusfall);
            this.grpPar51.Controls.Add(this.chk51a);
            this.grpPar51.Location = new System.Drawing.Point(464, 224);
            this.grpPar51.Name = "grpPar51";
            this.grpPar51.Size = new System.Drawing.Size(434, 156);
            this.grpPar51.TabIndex = 4;
            this.grpPar51.TabStop = false;
            this.grpPar51.Text = "Vergütungsausfall (§ 51 / § 51a)";
            //
            this.lblPar51.AutoSize = true;
            this.lblPar51.Location = new System.Drawing.Point(12, 26);
            this.lblPar51.Name = "lblPar51";
            this.lblPar51.Size = new System.Drawing.Size(70, 15);
            this.lblPar51.Text = "Anwenden:";
            //
            this.cmbPar51.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbPar51.Location = new System.Drawing.Point(120, 22);
            this.cmbPar51.Name = "cmbPar51";
            this.cmbPar51.Size = new System.Drawing.Size(120, 23);
            this.cmbPar51.TabIndex = 0;
            this.cmbPar51.SelectedIndexChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblPar51Status.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblPar51Status.Location = new System.Drawing.Point(250, 22);
            this.lblPar51Status.Name = "lblPar51Status";
            this.lblPar51Status.Size = new System.Drawing.Size(176, 34);
            this.lblPar51Status.Text = "";
            //
            this.lblIMSys.AutoSize = true;
            this.lblIMSys.Location = new System.Drawing.Point(12, 62);
            this.lblIMSys.Name = "lblIMSys";
            this.lblIMSys.Size = new System.Drawing.Size(200, 15);
            this.lblIMSys.Text = "iMSys-Einbaujahr (0 = keins):";
            //
            this.numIMSys.Location = new System.Drawing.Point(250, 58);
            this.numIMSys.Maximum = new decimal(new int[] { 2100, 0, 0, 0 });
            this.numIMSys.Name = "numIMSys";
            this.numIMSys.Size = new System.Drawing.Size(80, 23);
            this.numIMSys.TabIndex = 1;
            this.numIMSys.ValueChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblAusfall.AutoSize = true;
            this.lblAusfall.Location = new System.Drawing.Point(12, 92);
            this.lblAusfall.Name = "lblAusfall";
            this.lblAusfall.Size = new System.Drawing.Size(220, 15);
            this.lblAusfall.Text = "Ausfallanteil der Einspeisearbeit [%]:";
            //
            this.numAusfall.DecimalPlaces = 1;
            this.numAusfall.Location = new System.Drawing.Point(250, 88);
            this.numAusfall.Name = "numAusfall";
            this.numAusfall.Size = new System.Drawing.Size(80, 23);
            this.numAusfall.TabIndex = 2;
            this.numAusfall.ValueChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.chk51a.AutoSize = true;
            this.chk51a.Checked = true;
            this.chk51a.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chk51a.Location = new System.Drawing.Point(15, 122);
            this.chk51a.Name = "chk51a";
            this.chk51a.Size = new System.Drawing.Size(280, 19);
            this.chk51a.TabIndex = 3;
            this.chk51a.Text = "§ 51a-Kompensation (Laufzeitverlängerung)";
            this.chk51a.CheckedChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            // grpBezug
            //
            this.grpBezug.Controls.Add(this.chkBezugReihe);
            this.grpBezug.Controls.Add(this.lblStromsteuer);
            this.grpBezug.Location = new System.Drawing.Point(16, 448);
            this.grpBezug.Name = "grpBezug";
            this.grpBezug.Size = new System.Drawing.Size(432, 92);
            this.grpBezug.TabIndex = 5;
            this.grpBezug.TabStop = false;
            this.grpBezug.Text = "Strompreis / Bezugsbewertung";
            //
            this.chkBezugReihe.AutoSize = true;
            this.chkBezugReihe.Location = new System.Drawing.Point(15, 24);
            this.chkBezugReihe.Name = "chkBezugReihe";
            this.chkBezugReihe.Size = new System.Drawing.Size(330, 19);
            this.chkBezugReihe.TabIndex = 0;
            this.chkBezugReihe.Text = "Netzbezug stundenscharf aus Preiszeitreihe bewerten";
            this.chkBezugReihe.CheckedChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblStromsteuer.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblStromsteuer.Location = new System.Drawing.Point(12, 46);
            this.lblStromsteuer.Name = "lblStromsteuer";
            this.lblStromsteuer.Size = new System.Drawing.Size(410, 42);
            this.lblStromsteuer.Text = "";
            //
            // grpKappung
            //
            this.grpKappung.Controls.Add(this.lblKappung);
            this.grpKappung.Controls.Add(this.cmbKappung);
            this.grpKappung.Controls.Add(this.lblKappungStatus);
            this.grpKappung.Location = new System.Drawing.Point(464, 448);
            this.grpKappung.Name = "grpKappung";
            this.grpKappung.Size = new System.Drawing.Size(434, 92);
            this.grpKappung.TabIndex = 6;
            this.grpKappung.TabStop = false;
            this.grpKappung.Text = "60-%-Wirkleistungsbegrenzung (§ 9 Abs. 2)";
            //
            this.lblKappung.AutoSize = true;
            this.lblKappung.Location = new System.Drawing.Point(12, 26);
            this.lblKappung.Name = "lblKappung";
            this.lblKappung.Size = new System.Drawing.Size(70, 15);
            this.lblKappung.Text = "Anwenden:";
            //
            this.cmbKappung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbKappung.Location = new System.Drawing.Point(120, 22);
            this.cmbKappung.Name = "cmbKappung";
            this.cmbKappung.Size = new System.Drawing.Size(120, 23);
            this.cmbKappung.TabIndex = 0;
            this.cmbKappung.SelectedIndexChanged += new System.EventHandler(this.EingabeGeaendert);
            //
            this.lblKappungStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblKappungStatus.Location = new System.Drawing.Point(12, 52);
            this.lblKappungStatus.Name = "lblKappungStatus";
            this.lblKappungStatus.Size = new System.Drawing.Size(414, 32);
            this.lblKappungStatus.Text = "";
            //
            // pnlVorschauKopf
            //
            this.pnlVorschauKopf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(50)))), ((int)(((byte)(97)))));
            this.pnlVorschauKopf.Controls.Add(this.lblVorschauTitel);
            this.pnlVorschauKopf.Location = new System.Drawing.Point(16, 552);
            this.pnlVorschauKopf.Name = "pnlVorschauKopf";
            this.pnlVorschauKopf.Size = new System.Drawing.Size(882, 28);
            this.pnlVorschauKopf.TabIndex = 7;
            //
            this.lblVorschauTitel.AutoSize = true;
            this.lblVorschauTitel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblVorschauTitel.ForeColor = System.Drawing.Color.White;
            this.lblVorschauTitel.Location = new System.Drawing.Point(10, 5);
            this.lblVorschauTitel.Name = "lblVorschauTitel";
            this.lblVorschauTitel.Size = new System.Drawing.Size(70, 17);
            this.lblVorschauTitel.Text = "Vorschau";
            //
            // lblVorschau
            //
            this.lblVorschau.Location = new System.Drawing.Point(16, 586);
            this.lblVorschau.Name = "lblVorschau";
            this.lblVorschau.Size = new System.Drawing.Size(882, 42);
            this.lblVorschau.TabIndex = 8;
            this.lblVorschau.Text = "—";
            // 
            // lblKennzahlen
            // 
            this.lblKennzahlen.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(50)))), ((int)(((byte)(97)))));
            this.lblKennzahlen.Location = new System.Drawing.Point(16, 630);
            this.lblKennzahlen.Name = "lblKennzahlen";
            this.lblKennzahlen.Size = new System.Drawing.Size(882, 44);
            this.lblKennzahlen.TabIndex = 12;
            this.lblKennzahlen.Text = "—";
            //
            // btnUebernehmen
            //
            this.btnMarktwerte.Location = new System.Drawing.Point(16, 682);
            this.btnMarktwerte.Name = "btnMarktwerte";
            this.btnMarktwerte.Size = new System.Drawing.Size(200, 27);
            this.btnMarktwerte.TabIndex = 13;
            this.btnMarktwerte.Text = "Marktwerte importieren…";
            this.btnMarktwerte.UseVisualStyleBackColor = true;
            this.btnMarktwerte.Click += new System.EventHandler(this.btnMarktwerte_Click);
            //
            // btnTarifPv
            //
            this.btnTarifPv.Location = new System.Drawing.Point(224, 682);
            this.btnTarifPv.Name = "btnTarifPv";
            this.btnTarifPv.Size = new System.Drawing.Size(170, 27);
            this.btnTarifPv.TabIndex = 14;
            this.btnTarifPv.Text = "Einspeise-Tarif…";
            this.btnTarifPv.UseVisualStyleBackColor = true;
            this.btnTarifPv.Click += new System.EventHandler(this.btnTarifPv_Click);
            //
            // btnUebernehmen
            //
            this.btnUebernehmen.Location = new System.Drawing.Point(672, 682);
            this.btnUebernehmen.Name = "btnUebernehmen";
            this.btnUebernehmen.Size = new System.Drawing.Size(110, 30);
            this.btnUebernehmen.TabIndex = 9;
            this.btnUebernehmen.Text = "Übernehmen";
            this.btnUebernehmen.UseVisualStyleBackColor = true;
            this.btnUebernehmen.Click += new System.EventHandler(this.btnUebernehmen_Click);
            //
            // btnAbbrechen
            //
            this.btnAbbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAbbrechen.Location = new System.Drawing.Point(788, 682);
            this.btnAbbrechen.Name = "btnAbbrechen";
            this.btnAbbrechen.Size = new System.Drawing.Size(110, 30);
            this.btnAbbrechen.TabIndex = 10;
            this.btnAbbrechen.Text = "Abbrechen";
            this.btnAbbrechen.UseVisualStyleBackColor = true;
            //
            // Form_PhotovoltaikVerguetung
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnAbbrechen;
            this.ClientSize = new System.Drawing.Size(914, 724);
            this.Controls.Add(this.pnlKopf);
            this.Controls.Add(this.grpAnlage);
            this.Controls.Add(this.grpVermarktung);
            this.Controls.Add(this.grpAw);
            this.Controls.Add(this.grpPar51);
            this.Controls.Add(this.grpBezug);
            this.Controls.Add(this.grpKappung);
            this.Controls.Add(this.pnlVorschauKopf);
            this.Controls.Add(this.lblVorschau);
            this.Controls.Add(this.lblKennzahlen);
            this.Controls.Add(this.btnMarktwerte);
            this.Controls.Add(this.btnTarifPv);
            this.Controls.Add(this.btnUebernehmen);
            this.Controls.Add(this.btnAbbrechen);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_PhotovoltaikVerguetung";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "PV-Vergütung (EEG)";
            this.pnlKopf.ResumeLayout(false);
            this.pnlKopf.PerformLayout();
            this.grpAnlage.ResumeLayout(false);
            this.grpAnlage.PerformLayout();
            this.grpVermarktung.ResumeLayout(false);
            this.grpVermarktung.PerformLayout();
            this.grpAw.ResumeLayout(false);
            this.grpAw.PerformLayout();
            this.grpPar51.ResumeLayout(false);
            this.grpPar51.PerformLayout();
            this.grpBezug.ResumeLayout(false);
            this.grpBezug.PerformLayout();
            this.grpKappung.ResumeLayout(false);
            this.grpKappung.PerformLayout();
            this.pnlVorschauKopf.ResumeLayout(false);
            this.pnlVorschauKopf.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numKwpOverride)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numDv)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPpaPreis)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPpaAufschlag)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAwOverride)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numIMSys)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAusfall)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlKopf;
        private System.Windows.Forms.Label lblKopfTitel;
        private System.Windows.Forms.CheckBox chkAktiv;
        private System.Windows.Forms.GroupBox grpAnlage;
        private System.Windows.Forms.Label lblKwp;
        private System.Windows.Forms.Label lblKwpWert;
        private System.Windows.Forms.Label lblKwpOverride;
        private System.Windows.Forms.NumericUpDown numKwpOverride;
        private System.Windows.Forms.Label lblIbn;
        private System.Windows.Forms.DateTimePicker dtpIbn;
        private System.Windows.Forms.RadioButton rbUeberschuss;
        private System.Windows.Forms.RadioButton rbVoll;
        private System.Windows.Forms.Label lblAnlageWarnung;
        private System.Windows.Forms.GroupBox grpVermarktung;
        private System.Windows.Forms.RadioButton rbEv;
        private System.Windows.Forms.RadioButton rbMarktpraemie;
        private System.Windows.Forms.RadioButton rbPpa;
        private System.Windows.Forms.RadioButton rbKeine;
        private System.Windows.Forms.Label lblDv;
        private System.Windows.Forms.NumericUpDown numDv;
        private System.Windows.Forms.Label lblPpaPreis;
        private System.Windows.Forms.NumericUpDown numPpaPreis;
        private System.Windows.Forms.Label lblPpaAufschlag;
        private System.Windows.Forms.NumericUpDown numPpaAufschlag;
        private System.Windows.Forms.Label lblVermarktungHinweis;
        private System.Windows.Forms.GroupBox grpAw;
        private System.Windows.Forms.Label lblAwWert;
        private System.Windows.Forms.Label lblAwHerkunft;
        private System.Windows.Forms.Label lblAwOverride;
        private System.Windows.Forms.NumericUpDown numAwOverride;
        private System.Windows.Forms.Label lblEv;
        private System.Windows.Forms.GroupBox grpPar51;
        private System.Windows.Forms.Label lblPar51;
        private System.Windows.Forms.ComboBox cmbPar51;
        private System.Windows.Forms.Label lblPar51Status;
        private System.Windows.Forms.Label lblIMSys;
        private System.Windows.Forms.NumericUpDown numIMSys;
        private System.Windows.Forms.Label lblAusfall;
        private System.Windows.Forms.NumericUpDown numAusfall;
        private System.Windows.Forms.CheckBox chk51a;
        private System.Windows.Forms.GroupBox grpBezug;
        private System.Windows.Forms.CheckBox chkBezugReihe;
        private System.Windows.Forms.Label lblStromsteuer;
        private System.Windows.Forms.GroupBox grpKappung;
        private System.Windows.Forms.ComboBox cmbKappung;
        private System.Windows.Forms.Label lblKappung;
        private System.Windows.Forms.Label lblKappungStatus;
        private System.Windows.Forms.Panel pnlVorschauKopf;
        private System.Windows.Forms.Label lblVorschauTitel;
        private System.Windows.Forms.Label lblVorschau;
        private System.Windows.Forms.Label lblKennzahlen;
        private System.Windows.Forms.Button btnMarktwerte;
        private System.Windows.Forms.Button btnUebernehmen;
        private System.Windows.Forms.Button btnAbbrechen;
        private System.Windows.Forms.Button btnTarifPv;
    }
}
