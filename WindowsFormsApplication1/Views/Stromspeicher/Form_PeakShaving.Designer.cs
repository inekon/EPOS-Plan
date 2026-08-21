namespace WindowsFormsApplication1
{
    partial class Form_PeakShaving
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
            this.grpQuelle = new System.Windows.Forms.GroupBox();
            this.rad_Ganglinie = new System.Windows.Forms.RadioButton();
            this.cbo_Ganglinie = new System.Windows.Forms.ComboBox();
            this.rad_Datei = new System.Windows.Forms.RadioButton();
            this.btn_Datei = new System.Windows.Forms.Button();
            this.lbl_Reihe = new System.Windows.Forms.Label();
            this.grpParameter = new System.Windows.Forms.GroupBox();
            this.lbl_P = new System.Windows.Forms.Label();
            this.tb_P = new System.Windows.Forms.TextBox();
            this.lbl_Kapazitaet = new System.Windows.Forms.Label();
            this.tb_Kapazitaet = new System.Windows.Forms.TextBox();
            this.lbl_Eta = new System.Windows.Forms.Label();
            this.tb_Eta = new System.Windows.Forms.TextBox();
            this.lbl_SoCMin = new System.Windows.Forms.Label();
            this.tb_SoCMin = new System.Windows.Forms.TextBox();
            this.lbl_SoCMax = new System.Windows.Forms.Label();
            this.tb_SoCMax = new System.Windows.Forms.TextBox();
            this.lbl_StartSoC = new System.Windows.Forms.Label();
            this.tb_StartSoC = new System.Windows.Forms.TextBox();
            this.lbl_Ziel = new System.Windows.Forms.Label();
            this.tb_Ziel = new System.Windows.Forms.TextBox();
            this.chk_Adaptiv = new System.Windows.Forms.CheckBox();
            this.btn_Minimal = new System.Windows.Forms.Button();
            this.lbl_Lp = new System.Windows.Forms.Label();
            this.tb_Lp = new System.Windows.Forms.TextBox();
            this.lbl_Bezugspreis = new System.Windows.Forms.Label();
            this.tb_Bezugspreis = new System.Windows.Forms.TextBox();
            this.chk_Kompatibel = new System.Windows.Forms.CheckBox();
            this.lbl_CCap = new System.Windows.Forms.Label();
            this.tb_CCap = new System.Windows.Forms.TextBox();
            this.lbl_CPow = new System.Windows.Forms.Label();
            this.tb_CPow = new System.Windows.Forms.TextBox();
            this.lbl_IFix = new System.Windows.Forms.Label();
            this.tb_IFix = new System.Windows.Forms.TextBox();
            this.lbl_Zins = new System.Windows.Forms.Label();
            this.tb_Zins = new System.Windows.Forms.TextBox();
            this.lbl_Nutzungsdauer = new System.Windows.Forms.Label();
            this.tb_Nutzungsdauer = new System.Windows.Forms.TextBox();
            this.lbl_Herkunft = new System.Windows.Forms.Label();
            this.btn_Rechnen = new System.Windows.Forms.Button();
            this.chk_SoC = new System.Windows.Forms.CheckBox();
            this.btn_Csv = new System.Windows.Forms.Button();
            this.tab_Ergebnis = new System.Windows.Forms.TabControl();
            this.tabKennzahlen = new System.Windows.Forms.TabPage();
            this.list_Kennzahlen = new System.Windows.Forms.ListView();
            this.col_KennzahlGroesse = new System.Windows.Forms.ColumnHeader();
            this.col_KennzahlWert = new System.Windows.Forms.ColumnHeader();
            this.col_KennzahlEinheit = new System.Windows.Forms.ColumnHeader();
            this.tabChart = new System.Windows.Forms.TabPage();
            this.tabMonate = new System.Windows.Forms.TabPage();
            this.list_Monate = new System.Windows.Forms.ListView();
            this.col_MonatName = new System.Windows.Forms.ColumnHeader();
            this.col_MonatAlt = new System.Windows.Forms.ColumnHeader();
            this.col_MonatNeu = new System.Windows.Forms.ColumnHeader();
            this.col_MonatKappung = new System.Windows.Forms.ColumnHeader();
            this.lbl_Hinweis = new System.Windows.Forms.Label();
            this.btn_Schliessen = new System.Windows.Forms.Button();
            this.grpQuelle.SuspendLayout();
            this.grpParameter.SuspendLayout();
            this.tab_Ergebnis.SuspendLayout();
            this.tabKennzahlen.SuspendLayout();
            this.tabMonate.SuspendLayout();
            this.SuspendLayout();
            //
            // rad_Ganglinie
            //
            this.rad_Ganglinie.Checked = true;
            this.rad_Ganglinie.Location = new System.Drawing.Point(14, 24);
            this.rad_Ganglinie.Name = "rad_Ganglinie";
            this.rad_Ganglinie.Size = new System.Drawing.Size(210, 22);
            this.rad_Ganglinie.Text = "rad_Ganglinie";
            this.rad_Ganglinie.CheckedChanged += new System.EventHandler(this.QuelleGeaendert);
            //
            // cbo_Ganglinie
            //
            this.cbo_Ganglinie.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cbo_Ganglinie.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbo_Ganglinie.Location = new System.Drawing.Point(230, 23);
            this.cbo_Ganglinie.Name = "cbo_Ganglinie";
            this.cbo_Ganglinie.Size = new System.Drawing.Size(480, 22);
            this.cbo_Ganglinie.SelectedIndexChanged += new System.EventHandler(this.QuelleGeaendert);
            //
            // rad_Datei
            //
            this.rad_Datei.Location = new System.Drawing.Point(14, 54);
            this.rad_Datei.Name = "rad_Datei";
            this.rad_Datei.Size = new System.Drawing.Size(210, 22);
            this.rad_Datei.Text = "rad_Datei";
            this.rad_Datei.CheckedChanged += new System.EventHandler(this.QuelleGeaendert);
            //
            // btn_Datei
            //
            this.btn_Datei.Location = new System.Drawing.Point(230, 52);
            this.btn_Datei.Name = "btn_Datei";
            this.btn_Datei.Size = new System.Drawing.Size(160, 26);
            this.btn_Datei.Text = "btn_Datei";
            this.btn_Datei.Click += new System.EventHandler(this.Datei_Click);
            //
            // lbl_Reihe
            //
            this.lbl_Reihe.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_Reihe.AutoEllipsis = true;
            this.lbl_Reihe.Location = new System.Drawing.Point(400, 57);
            this.lbl_Reihe.Name = "lbl_Reihe";
            this.lbl_Reihe.Size = new System.Drawing.Size(620, 18);
            //
            // grpQuelle
            //
            this.grpQuelle.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpQuelle.Location = new System.Drawing.Point(12, 10);
            this.grpQuelle.Name = "grpQuelle";
            this.grpQuelle.Size = new System.Drawing.Size(1036, 96);
            this.grpQuelle.TabStop = false;
            this.grpQuelle.Text = "grpQuelle";
            this.grpQuelle.Controls.Add(this.rad_Ganglinie);
            this.grpQuelle.Controls.Add(this.cbo_Ganglinie);
            this.grpQuelle.Controls.Add(this.rad_Datei);
            this.grpQuelle.Controls.Add(this.btn_Datei);
            this.grpQuelle.Controls.Add(this.lbl_Reihe);
            //
            // lbl_P
            //
            this.lbl_P.Location = new System.Drawing.Point(14, 28);
            this.lbl_P.Name = "lbl_P";
            this.lbl_P.Size = new System.Drawing.Size(194, 18);
            this.lbl_P.Text = "lbl_P";
            //
            // tb_P
            //
            this.tb_P.Location = new System.Drawing.Point(210, 24);
            this.tb_P.Name = "tb_P";
            this.tb_P.Size = new System.Drawing.Size(120, 22);
            this.tb_P.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_P.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_Kapazitaet
            //
            this.lbl_Kapazitaet.Location = new System.Drawing.Point(350, 28);
            this.lbl_Kapazitaet.Name = "lbl_Kapazitaet";
            this.lbl_Kapazitaet.Size = new System.Drawing.Size(194, 18);
            this.lbl_Kapazitaet.Text = "lbl_Kapazitaet";
            //
            // tb_Kapazitaet
            //
            this.tb_Kapazitaet.Location = new System.Drawing.Point(546, 24);
            this.tb_Kapazitaet.Name = "tb_Kapazitaet";
            this.tb_Kapazitaet.Size = new System.Drawing.Size(120, 22);
            this.tb_Kapazitaet.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_Kapazitaet.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_Eta
            //
            this.lbl_Eta.Location = new System.Drawing.Point(686, 28);
            this.lbl_Eta.Name = "lbl_Eta";
            this.lbl_Eta.Size = new System.Drawing.Size(194, 18);
            this.lbl_Eta.Text = "lbl_Eta";
            //
            // tb_Eta
            //
            this.tb_Eta.Location = new System.Drawing.Point(882, 24);
            this.tb_Eta.Name = "tb_Eta";
            this.tb_Eta.Size = new System.Drawing.Size(120, 22);
            this.tb_Eta.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_Eta.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_SoCMin
            //
            this.lbl_SoCMin.Location = new System.Drawing.Point(14, 56);
            this.lbl_SoCMin.Name = "lbl_SoCMin";
            this.lbl_SoCMin.Size = new System.Drawing.Size(194, 18);
            this.lbl_SoCMin.Text = "lbl_SoCMin";
            //
            // tb_SoCMin
            //
            this.tb_SoCMin.Location = new System.Drawing.Point(210, 52);
            this.tb_SoCMin.Name = "tb_SoCMin";
            this.tb_SoCMin.Size = new System.Drawing.Size(120, 22);
            this.tb_SoCMin.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_SoCMin.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_SoCMax
            //
            this.lbl_SoCMax.Location = new System.Drawing.Point(350, 56);
            this.lbl_SoCMax.Name = "lbl_SoCMax";
            this.lbl_SoCMax.Size = new System.Drawing.Size(194, 18);
            this.lbl_SoCMax.Text = "lbl_SoCMax";
            //
            // tb_SoCMax
            //
            this.tb_SoCMax.Location = new System.Drawing.Point(546, 52);
            this.tb_SoCMax.Name = "tb_SoCMax";
            this.tb_SoCMax.Size = new System.Drawing.Size(120, 22);
            this.tb_SoCMax.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_SoCMax.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_StartSoC
            //
            this.lbl_StartSoC.Location = new System.Drawing.Point(686, 56);
            this.lbl_StartSoC.Name = "lbl_StartSoC";
            this.lbl_StartSoC.Size = new System.Drawing.Size(194, 18);
            this.lbl_StartSoC.Text = "lbl_StartSoC";
            //
            // tb_StartSoC
            //
            this.tb_StartSoC.Location = new System.Drawing.Point(882, 52);
            this.tb_StartSoC.Name = "tb_StartSoC";
            this.tb_StartSoC.Size = new System.Drawing.Size(120, 22);
            this.tb_StartSoC.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_StartSoC.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_Ziel
            //
            this.lbl_Ziel.Location = new System.Drawing.Point(14, 84);
            this.lbl_Ziel.Name = "lbl_Ziel";
            this.lbl_Ziel.Size = new System.Drawing.Size(194, 18);
            this.lbl_Ziel.Text = "lbl_Ziel";
            //
            // tb_Ziel
            //
            this.tb_Ziel.Location = new System.Drawing.Point(210, 80);
            this.tb_Ziel.Name = "tb_Ziel";
            this.tb_Ziel.Size = new System.Drawing.Size(120, 22);
            this.tb_Ziel.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_Ziel.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // chk_Adaptiv
            //
            this.chk_Adaptiv.Location = new System.Drawing.Point(350, 80);
            this.chk_Adaptiv.Name = "chk_Adaptiv";
            this.chk_Adaptiv.Size = new System.Drawing.Size(194, 22);
            this.chk_Adaptiv.Text = "chk_Adaptiv";
            this.chk_Adaptiv.CheckedChanged += new System.EventHandler(this.AdaptivGeaendert);
            //
            // btn_Minimal
            //
            this.btn_Minimal.Location = new System.Drawing.Point(546, 78);
            this.btn_Minimal.Name = "btn_Minimal";
            this.btn_Minimal.Size = new System.Drawing.Size(336, 26);
            this.btn_Minimal.Text = "btn_Minimal";
            this.btn_Minimal.Click += new System.EventHandler(this.Minimal_Click);
            //
            // lbl_Lp
            //
            this.lbl_Lp.Location = new System.Drawing.Point(14, 112);
            this.lbl_Lp.Name = "lbl_Lp";
            this.lbl_Lp.Size = new System.Drawing.Size(194, 18);
            this.lbl_Lp.Text = "lbl_Lp";
            //
            // tb_Lp
            //
            this.tb_Lp.Location = new System.Drawing.Point(210, 108);
            this.tb_Lp.Name = "tb_Lp";
            this.tb_Lp.Size = new System.Drawing.Size(120, 22);
            this.tb_Lp.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_Lp.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_Bezugspreis
            //
            this.lbl_Bezugspreis.Location = new System.Drawing.Point(350, 112);
            this.lbl_Bezugspreis.Name = "lbl_Bezugspreis";
            this.lbl_Bezugspreis.Size = new System.Drawing.Size(194, 18);
            this.lbl_Bezugspreis.Text = "lbl_Bezugspreis";
            //
            // tb_Bezugspreis
            //
            this.tb_Bezugspreis.Location = new System.Drawing.Point(546, 108);
            this.tb_Bezugspreis.Name = "tb_Bezugspreis";
            this.tb_Bezugspreis.Size = new System.Drawing.Size(120, 22);
            this.tb_Bezugspreis.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_Bezugspreis.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // chk_Kompatibel
            //
            this.chk_Kompatibel.Location = new System.Drawing.Point(686, 108);
            this.chk_Kompatibel.Name = "chk_Kompatibel";
            this.chk_Kompatibel.Size = new System.Drawing.Size(336, 22);
            this.chk_Kompatibel.Text = "chk_Kompatibel";
            //
            // lbl_CCap
            //
            this.lbl_CCap.Location = new System.Drawing.Point(14, 140);
            this.lbl_CCap.Name = "lbl_CCap";
            this.lbl_CCap.Size = new System.Drawing.Size(194, 18);
            this.lbl_CCap.Text = "lbl_CCap";
            //
            // tb_CCap
            //
            this.tb_CCap.Location = new System.Drawing.Point(210, 136);
            this.tb_CCap.Name = "tb_CCap";
            this.tb_CCap.Size = new System.Drawing.Size(120, 22);
            this.tb_CCap.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_CCap.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_CPow
            //
            this.lbl_CPow.Location = new System.Drawing.Point(350, 140);
            this.lbl_CPow.Name = "lbl_CPow";
            this.lbl_CPow.Size = new System.Drawing.Size(194, 18);
            this.lbl_CPow.Text = "lbl_CPow";
            //
            // tb_CPow
            //
            this.tb_CPow.Location = new System.Drawing.Point(546, 136);
            this.tb_CPow.Name = "tb_CPow";
            this.tb_CPow.Size = new System.Drawing.Size(120, 22);
            this.tb_CPow.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_CPow.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_IFix
            //
            this.lbl_IFix.Location = new System.Drawing.Point(686, 140);
            this.lbl_IFix.Name = "lbl_IFix";
            this.lbl_IFix.Size = new System.Drawing.Size(194, 18);
            this.lbl_IFix.Text = "lbl_IFix";
            //
            // tb_IFix
            //
            this.tb_IFix.Location = new System.Drawing.Point(882, 136);
            this.tb_IFix.Name = "tb_IFix";
            this.tb_IFix.Size = new System.Drawing.Size(120, 22);
            this.tb_IFix.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_IFix.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_Zins
            //
            this.lbl_Zins.Location = new System.Drawing.Point(14, 168);
            this.lbl_Zins.Name = "lbl_Zins";
            this.lbl_Zins.Size = new System.Drawing.Size(194, 18);
            this.lbl_Zins.Text = "lbl_Zins";
            //
            // tb_Zins
            //
            this.tb_Zins.Location = new System.Drawing.Point(210, 164);
            this.tb_Zins.Name = "tb_Zins";
            this.tb_Zins.Size = new System.Drawing.Size(120, 22);
            this.tb_Zins.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_Zins.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_Nutzungsdauer
            //
            this.lbl_Nutzungsdauer.Location = new System.Drawing.Point(350, 168);
            this.lbl_Nutzungsdauer.Name = "lbl_Nutzungsdauer";
            this.lbl_Nutzungsdauer.Size = new System.Drawing.Size(194, 18);
            this.lbl_Nutzungsdauer.Text = "lbl_Nutzungsdauer";
            //
            // tb_Nutzungsdauer
            //
            this.tb_Nutzungsdauer.Location = new System.Drawing.Point(546, 164);
            this.tb_Nutzungsdauer.Name = "tb_Nutzungsdauer";
            this.tb_Nutzungsdauer.Size = new System.Drawing.Size(120, 22);
            this.tb_Nutzungsdauer.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.tb_Nutzungsdauer.TextChanged += new System.EventHandler(this.Zahlfeld_TextChanged);
            //
            // lbl_Herkunft
            //
            this.lbl_Herkunft.AutoEllipsis = true;
            this.lbl_Herkunft.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lbl_Herkunft.Location = new System.Drawing.Point(686, 168);
            this.lbl_Herkunft.Name = "lbl_Herkunft";
            this.lbl_Herkunft.Size = new System.Drawing.Size(336, 18);
            //
            // grpParameter
            //
            this.grpParameter.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpParameter.Location = new System.Drawing.Point(12, 112);
            this.grpParameter.Name = "grpParameter";
            this.grpParameter.Size = new System.Drawing.Size(1036, 196);
            this.grpParameter.TabStop = false;
            this.grpParameter.Text = "grpParameter";
            this.grpParameter.Controls.Add(this.lbl_P);
            this.grpParameter.Controls.Add(this.tb_P);
            this.grpParameter.Controls.Add(this.lbl_Kapazitaet);
            this.grpParameter.Controls.Add(this.tb_Kapazitaet);
            this.grpParameter.Controls.Add(this.lbl_Eta);
            this.grpParameter.Controls.Add(this.tb_Eta);
            this.grpParameter.Controls.Add(this.lbl_SoCMin);
            this.grpParameter.Controls.Add(this.tb_SoCMin);
            this.grpParameter.Controls.Add(this.lbl_SoCMax);
            this.grpParameter.Controls.Add(this.tb_SoCMax);
            this.grpParameter.Controls.Add(this.lbl_StartSoC);
            this.grpParameter.Controls.Add(this.tb_StartSoC);
            this.grpParameter.Controls.Add(this.lbl_Ziel);
            this.grpParameter.Controls.Add(this.tb_Ziel);
            this.grpParameter.Controls.Add(this.chk_Adaptiv);
            this.grpParameter.Controls.Add(this.btn_Minimal);
            this.grpParameter.Controls.Add(this.lbl_Lp);
            this.grpParameter.Controls.Add(this.tb_Lp);
            this.grpParameter.Controls.Add(this.lbl_Bezugspreis);
            this.grpParameter.Controls.Add(this.tb_Bezugspreis);
            this.grpParameter.Controls.Add(this.chk_Kompatibel);
            this.grpParameter.Controls.Add(this.lbl_CCap);
            this.grpParameter.Controls.Add(this.tb_CCap);
            this.grpParameter.Controls.Add(this.lbl_CPow);
            this.grpParameter.Controls.Add(this.tb_CPow);
            this.grpParameter.Controls.Add(this.lbl_IFix);
            this.grpParameter.Controls.Add(this.tb_IFix);
            this.grpParameter.Controls.Add(this.lbl_Zins);
            this.grpParameter.Controls.Add(this.tb_Zins);
            this.grpParameter.Controls.Add(this.lbl_Nutzungsdauer);
            this.grpParameter.Controls.Add(this.tb_Nutzungsdauer);
            this.grpParameter.Controls.Add(this.lbl_Herkunft);
            //
            // btn_Rechnen
            //
            this.btn_Rechnen.Location = new System.Drawing.Point(12, 316);
            this.btn_Rechnen.Name = "btn_Rechnen";
            this.btn_Rechnen.Size = new System.Drawing.Size(190, 30);
            this.btn_Rechnen.Text = "btn_Rechnen";
            this.btn_Rechnen.Click += new System.EventHandler(this.Rechnen_Click);
            //
            // chk_SoC
            //
            this.chk_SoC.Checked = true;
            this.chk_SoC.Location = new System.Drawing.Point(216, 321);
            this.chk_SoC.Name = "chk_SoC";
            this.chk_SoC.Size = new System.Drawing.Size(260, 22);
            this.chk_SoC.Text = "chk_SoC";
            this.chk_SoC.CheckedChanged += new System.EventHandler(this.SoCAnzeigeGeaendert);
            //
            // btn_Csv
            //
            this.btn_Csv.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Csv.Location = new System.Drawing.Point(858, 316);
            this.btn_Csv.Name = "btn_Csv";
            this.btn_Csv.Size = new System.Drawing.Size(190, 30);
            this.btn_Csv.Text = "btn_Csv";
            this.btn_Csv.Click += new System.EventHandler(this.Csv_Click);
            //
            // list_Kennzahlen
            //
            this.list_Kennzahlen.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.col_KennzahlGroesse,
            this.col_KennzahlWert,
            this.col_KennzahlEinheit});
            this.list_Kennzahlen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.list_Kennzahlen.FullRowSelect = true;
            this.list_Kennzahlen.GridLines = true;
            this.list_Kennzahlen.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.list_Kennzahlen.MultiSelect = false;
            this.list_Kennzahlen.Name = "list_Kennzahlen";
            this.list_Kennzahlen.View = System.Windows.Forms.View.Details;
            //
            // col_KennzahlGroesse
            //
            this.col_KennzahlGroesse.Name = "col_KennzahlGroesse";
            this.col_KennzahlGroesse.Text = "col_KennzahlGroesse";
            this.col_KennzahlGroesse.Width = 420;
            //
            // col_KennzahlWert
            //
            this.col_KennzahlWert.Name = "col_KennzahlWert";
            this.col_KennzahlWert.Text = "col_KennzahlWert";
            this.col_KennzahlWert.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.col_KennzahlWert.Width = 170;
            //
            // col_KennzahlEinheit
            //
            this.col_KennzahlEinheit.Name = "col_KennzahlEinheit";
            this.col_KennzahlEinheit.Text = "col_KennzahlEinheit";
            this.col_KennzahlEinheit.Width = 150;
            //
            // tabKennzahlen
            //
            this.tabKennzahlen.Name = "tabKennzahlen";
            this.tabKennzahlen.Text = "tabKennzahlen";
            this.tabKennzahlen.Controls.Add(this.list_Kennzahlen);
            //
            // tabChart
            //
            // Das Chart wird bewusst NICHT vom Designer erzeugt (Projektregel: keine
            // Chart-Steuerelemente in der Designer-Serialisierung). Die leere Huelle
            // baut Form_PeakShaving.ChartAufbauen() im Konstruktor-Nachlauf und haengt
            // sie hier ein; diese Seite bleibt deshalb ohne Kinder.
            this.tabChart.Name = "tabChart";
            this.tabChart.Text = "tabChart";
            //
            // list_Monate
            //
            this.list_Monate.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.col_MonatName,
            this.col_MonatAlt,
            this.col_MonatNeu,
            this.col_MonatKappung});
            this.list_Monate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.list_Monate.FullRowSelect = true;
            this.list_Monate.GridLines = true;
            this.list_Monate.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this.list_Monate.MultiSelect = false;
            this.list_Monate.Name = "list_Monate";
            this.list_Monate.View = System.Windows.Forms.View.Details;
            //
            // col_MonatName
            //
            this.col_MonatName.Name = "col_MonatName";
            this.col_MonatName.Text = "col_MonatName";
            this.col_MonatName.Width = 220;
            //
            // col_MonatAlt
            //
            this.col_MonatAlt.Name = "col_MonatAlt";
            this.col_MonatAlt.Text = "col_MonatAlt";
            this.col_MonatAlt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.col_MonatAlt.Width = 170;
            //
            // col_MonatNeu
            //
            this.col_MonatNeu.Name = "col_MonatNeu";
            this.col_MonatNeu.Text = "col_MonatNeu";
            this.col_MonatNeu.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.col_MonatNeu.Width = 170;
            //
            // col_MonatKappung
            //
            this.col_MonatKappung.Name = "col_MonatKappung";
            this.col_MonatKappung.Text = "col_MonatKappung";
            this.col_MonatKappung.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.col_MonatKappung.Width = 170;
            //
            // tabMonate
            //
            this.tabMonate.Name = "tabMonate";
            this.tabMonate.Text = "tabMonate";
            this.tabMonate.Controls.Add(this.list_Monate);
            //
            // tab_Ergebnis
            //
            this.tab_Ergebnis.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tab_Ergebnis.Location = new System.Drawing.Point(12, 354);
            this.tab_Ergebnis.Name = "tab_Ergebnis";
            this.tab_Ergebnis.Size = new System.Drawing.Size(1036, 402);
            this.tab_Ergebnis.Controls.Add(this.tabKennzahlen);
            this.tab_Ergebnis.Controls.Add(this.tabChart);
            this.tab_Ergebnis.Controls.Add(this.tabMonate);
            //
            // lbl_Hinweis
            //
            this.lbl_Hinweis.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lbl_Hinweis.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lbl_Hinweis.Location = new System.Drawing.Point(12, 764);
            this.lbl_Hinweis.Name = "lbl_Hinweis";
            this.lbl_Hinweis.Size = new System.Drawing.Size(900, 52);
            this.lbl_Hinweis.Text = "lbl_Hinweis";
            //
            // btn_Schliessen
            //
            this.btn_Schliessen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Schliessen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btn_Schliessen.Location = new System.Drawing.Point(954, 790);
            this.btn_Schliessen.Name = "btn_Schliessen";
            this.btn_Schliessen.Size = new System.Drawing.Size(94, 28);
            this.btn_Schliessen.Text = "btn_Schliessen";
            //
            // Form_PeakShaving
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btn_Schliessen;
            this.ClientSize = new System.Drawing.Size(1060, 830);
            this.Controls.Add(this.grpQuelle);
            this.Controls.Add(this.grpParameter);
            this.Controls.Add(this.btn_Rechnen);
            this.Controls.Add(this.chk_SoC);
            this.Controls.Add(this.btn_Csv);
            this.Controls.Add(this.tab_Ergebnis);
            this.Controls.Add(this.lbl_Hinweis);
            this.Controls.Add(this.btn_Schliessen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(900, 700);
            this.Name = "Form_PeakShaving";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Form_PeakShaving";
            this.grpQuelle.ResumeLayout(false);
            this.grpParameter.ResumeLayout(false);
            this.tab_Ergebnis.ResumeLayout(false);
            this.tabKennzahlen.ResumeLayout(false);
            this.tabMonate.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox grpQuelle;
        private System.Windows.Forms.RadioButton rad_Ganglinie;
        private System.Windows.Forms.ComboBox cbo_Ganglinie;
        private System.Windows.Forms.RadioButton rad_Datei;
        private System.Windows.Forms.Button btn_Datei;
        private System.Windows.Forms.Label lbl_Reihe;
        private System.Windows.Forms.GroupBox grpParameter;
        private System.Windows.Forms.Label lbl_P;
        private System.Windows.Forms.TextBox tb_P;
        private System.Windows.Forms.Label lbl_Kapazitaet;
        private System.Windows.Forms.TextBox tb_Kapazitaet;
        private System.Windows.Forms.Label lbl_Eta;
        private System.Windows.Forms.TextBox tb_Eta;
        private System.Windows.Forms.Label lbl_SoCMin;
        private System.Windows.Forms.TextBox tb_SoCMin;
        private System.Windows.Forms.Label lbl_SoCMax;
        private System.Windows.Forms.TextBox tb_SoCMax;
        private System.Windows.Forms.Label lbl_StartSoC;
        private System.Windows.Forms.TextBox tb_StartSoC;
        private System.Windows.Forms.Label lbl_Ziel;
        private System.Windows.Forms.TextBox tb_Ziel;
        private System.Windows.Forms.CheckBox chk_Adaptiv;
        private System.Windows.Forms.Button btn_Minimal;
        private System.Windows.Forms.Label lbl_Lp;
        private System.Windows.Forms.TextBox tb_Lp;
        private System.Windows.Forms.Label lbl_Bezugspreis;
        private System.Windows.Forms.TextBox tb_Bezugspreis;
        private System.Windows.Forms.CheckBox chk_Kompatibel;
        private System.Windows.Forms.Label lbl_CCap;
        private System.Windows.Forms.TextBox tb_CCap;
        private System.Windows.Forms.Label lbl_CPow;
        private System.Windows.Forms.TextBox tb_CPow;
        private System.Windows.Forms.Label lbl_IFix;
        private System.Windows.Forms.TextBox tb_IFix;
        private System.Windows.Forms.Label lbl_Zins;
        private System.Windows.Forms.TextBox tb_Zins;
        private System.Windows.Forms.Label lbl_Nutzungsdauer;
        private System.Windows.Forms.TextBox tb_Nutzungsdauer;
        private System.Windows.Forms.Label lbl_Herkunft;
        private System.Windows.Forms.Button btn_Rechnen;
        private System.Windows.Forms.CheckBox chk_SoC;
        private System.Windows.Forms.Button btn_Csv;
        private System.Windows.Forms.TabControl tab_Ergebnis;
        private System.Windows.Forms.TabPage tabKennzahlen;
        private System.Windows.Forms.ListView list_Kennzahlen;
        private System.Windows.Forms.ColumnHeader col_KennzahlGroesse;
        private System.Windows.Forms.ColumnHeader col_KennzahlWert;
        private System.Windows.Forms.ColumnHeader col_KennzahlEinheit;
        private System.Windows.Forms.TabPage tabChart;
        private System.Windows.Forms.TabPage tabMonate;
        private System.Windows.Forms.ListView list_Monate;
        private System.Windows.Forms.ColumnHeader col_MonatName;
        private System.Windows.Forms.ColumnHeader col_MonatAlt;
        private System.Windows.Forms.ColumnHeader col_MonatNeu;
        private System.Windows.Forms.ColumnHeader col_MonatKappung;
        private System.Windows.Forms.Label lbl_Hinweis;
        private System.Windows.Forms.Button btn_Schliessen;
    }
}
