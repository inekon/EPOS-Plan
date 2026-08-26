namespace WindowsFormsApplication1
{
    partial class Form_KostenKomponente
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
            this.lblTitel = new System.Windows.Forms.Label();
            this.lblUntertitel = new System.Windows.Forms.Label();
            this.tabHaupt = new System.Windows.Forms.TabControl();
            this.tpKosten = new System.Windows.Forms.TabPage();
            this.pnlZeilen = new System.Windows.Forms.Panel();
            this.pnlRasterKopf = new System.Windows.Forms.Panel();
            this.lblSpAktionen = new System.Windows.Forms.Label();
            this.lblSpPosition = new System.Windows.Forms.Label();
            this.lblSpBemessung = new System.Windows.Forms.Label();
            this.lblSpSatz = new System.Windows.Forms.Label();
            this.lblSpBetrag = new System.Windows.Forms.Label();
            this.lblSpNutzung = new System.Windows.Forms.Label();
            this.lblSpWorstBest = new System.Windows.Forms.Label();
            this.lblReadOnly = new System.Windows.Forms.Label();
            this.pnlBanner = new System.Windows.Forms.Panel();
            this.lblBanner = new System.Windows.Forms.Label();
            this.btnBannerZu = new System.Windows.Forms.Button();
            this.pnlKontext = new System.Windows.Forms.Panel();
            this.lblKomponente = new System.Windows.Forms.Label();
            this.cmbKomponente = new System.Windows.Forms.ComboBox();
            this.rbInvest = new System.Windows.Forms.RadioButton();
            this.rbBetrieb = new System.Windows.Forms.RadioButton();
            this.lblVariante = new System.Windows.Forms.Label();
            this.cmbVariante = new System.Windows.Forms.ComboBox();
            this.btnVarianteNeu = new System.Windows.Forms.Button();
            this.btnSpeichernUnter = new System.Windows.Forms.Button();
            this.btnVarianteLoeschen = new System.Windows.Forms.Button();
            this.pnlFuss = new System.Windows.Forms.Panel();
            this.btnUebernahme = new System.Windows.Forms.Button();
            this.btnKatalog = new System.Windows.Forms.Button();
            this.btnPositionNeu = new System.Windows.Forms.Button();
            this.lblSummeNetto = new System.Windows.Forms.Label();
            this.lblSummeBrutto = new System.Windows.Forms.Label();
            this.tpErtrag = new System.Windows.Forms.TabPage();
            this.lblErtragHinweis = new System.Windows.Forms.Label();
            this.pnlKopf.SuspendLayout();
            this.tabHaupt.SuspendLayout();
            this.tpKosten.SuspendLayout();
            this.pnlRasterKopf.SuspendLayout();
            this.pnlBanner.SuspendLayout();
            this.pnlKontext.SuspendLayout();
            this.pnlFuss.SuspendLayout();
            this.tpErtrag.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlKopf
            //
            this.pnlKopf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(31)))), ((int)(((byte)(61)))));
            this.pnlKopf.Controls.Add(this.lblTitel);
            this.pnlKopf.Controls.Add(this.lblUntertitel);
            this.pnlKopf.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlKopf.Location = new System.Drawing.Point(0, 0);
            this.pnlKopf.Name = "pnlKopf";
            this.pnlKopf.Size = new System.Drawing.Size(1004, 74);
            this.pnlKopf.TabIndex = 0;
            //
            // lblTitel
            //
            this.lblTitel.AutoSize = true;
            this.lblTitel.Font = new System.Drawing.Font("Segoe UI", 15.75F, System.Drawing.FontStyle.Bold);
            this.lblTitel.ForeColor = System.Drawing.Color.White;
            this.lblTitel.Location = new System.Drawing.Point(14, 10);
            this.lblTitel.Name = "lblTitel";
            this.lblTitel.Size = new System.Drawing.Size(260, 30);
            this.lblTitel.TabIndex = 0;
            this.lblTitel.Text = "Kostenverwaltung";
            //
            // lblUntertitel
            //
            this.lblUntertitel.AutoSize = true;
            this.lblUntertitel.ForeColor = System.Drawing.Color.LightSteelBlue;
            this.lblUntertitel.Location = new System.Drawing.Point(16, 44);
            this.lblUntertitel.Name = "lblUntertitel";
            this.lblUntertitel.Size = new System.Drawing.Size(180, 15);
            this.lblUntertitel.TabIndex = 1;
            this.lblUntertitel.Text = "Investitionskosten nach VDI 2067";
            //
            // tabHaupt
            //
            this.tabHaupt.Controls.Add(this.tpKosten);
            this.tabHaupt.Controls.Add(this.tpErtrag);
            this.tabHaupt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabHaupt.Location = new System.Drawing.Point(0, 74);
            this.tabHaupt.Name = "tabHaupt";
            this.tabHaupt.SelectedIndex = 0;
            this.tabHaupt.Size = new System.Drawing.Size(1004, 647);
            this.tabHaupt.TabIndex = 1;
            //
            // tpKosten
            //
            this.tpKosten.Controls.Add(this.pnlZeilen);
            this.tpKosten.Controls.Add(this.pnlRasterKopf);
            this.tpKosten.Controls.Add(this.lblReadOnly);
            this.tpKosten.Controls.Add(this.pnlBanner);
            this.tpKosten.Controls.Add(this.pnlKontext);
            this.tpKosten.Controls.Add(this.pnlFuss);
            this.tpKosten.Location = new System.Drawing.Point(4, 24);
            this.tpKosten.Name = "tpKosten";
            this.tpKosten.Size = new System.Drawing.Size(996, 619);
            this.tpKosten.TabIndex = 0;
            this.tpKosten.Text = "Kosten Invest/Betrieb";
            this.tpKosten.UseVisualStyleBackColor = true;
            //
            // pnlZeilen
            //
            this.pnlZeilen.AutoScroll = true;
            this.pnlZeilen.BackColor = System.Drawing.Color.White;
            this.pnlZeilen.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlZeilen.Location = new System.Drawing.Point(0, 136);
            this.pnlZeilen.Name = "pnlZeilen";
            this.pnlZeilen.Size = new System.Drawing.Size(996, 419);
            this.pnlZeilen.TabIndex = 5;
            //
            // pnlRasterKopf
            //
            this.pnlRasterKopf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(50)))), ((int)(((byte)(97)))));
            this.pnlRasterKopf.ForeColor = System.Drawing.Color.White;
            this.pnlRasterKopf.Controls.Add(this.lblSpAktionen);
            this.pnlRasterKopf.Controls.Add(this.lblSpPosition);
            this.pnlRasterKopf.Controls.Add(this.lblSpBemessung);
            this.pnlRasterKopf.Controls.Add(this.lblSpSatz);
            this.pnlRasterKopf.Controls.Add(this.lblSpBetrag);
            this.pnlRasterKopf.Controls.Add(this.lblSpNutzung);
            this.pnlRasterKopf.Controls.Add(this.lblSpWorstBest);
            this.pnlRasterKopf.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlRasterKopf.Location = new System.Drawing.Point(0, 110);
            this.pnlRasterKopf.Name = "pnlRasterKopf";
            this.pnlRasterKopf.Size = new System.Drawing.Size(996, 26);
            this.pnlRasterKopf.TabIndex = 4;
            //
            // lblSpAktionen
            //
            this.lblSpAktionen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpAktionen.Location = new System.Drawing.Point(6, 6);
            this.lblSpAktionen.Name = "lblSpAktionen";
            this.lblSpAktionen.Size = new System.Drawing.Size(62, 15);
            this.lblSpAktionen.TabIndex = 0;
            this.lblSpAktionen.Text = "Aktionen";
            //
            // lblSpPosition
            //
            this.lblSpPosition.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpPosition.Location = new System.Drawing.Point(74, 6);
            this.lblSpPosition.Name = "lblSpPosition";
            this.lblSpPosition.Size = new System.Drawing.Size(250, 15);
            this.lblSpPosition.TabIndex = 1;
            this.lblSpPosition.Text = "Position";
            //
            // lblSpBemessung
            //
            this.lblSpBemessung.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpBemessung.Location = new System.Drawing.Point(336, 6);
            this.lblSpBemessung.Name = "lblSpBemessung";
            this.lblSpBemessung.Size = new System.Drawing.Size(170, 15);
            this.lblSpBemessung.TabIndex = 2;
            this.lblSpBemessung.Text = "Bemessung";
            //
            // lblSpSatz
            //
            this.lblSpSatz.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpSatz.Location = new System.Drawing.Point(518, 6);
            this.lblSpSatz.Name = "lblSpSatz";
            this.lblSpSatz.Size = new System.Drawing.Size(140, 15);
            this.lblSpSatz.TabIndex = 3;
            this.lblSpSatz.Text = "Satz";
            //
            // lblSpBetrag
            //
            this.lblSpBetrag.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpBetrag.Location = new System.Drawing.Point(688, 6);
            this.lblSpBetrag.Name = "lblSpBetrag";
            this.lblSpBetrag.Size = new System.Drawing.Size(108, 30);
            this.lblSpBetrag.TabIndex = 4;
            this.lblSpBetrag.Text = "Betrag netto [€]";
            //
            // lblSpNutzung
            //
            this.lblSpNutzung.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpNutzung.Location = new System.Drawing.Point(800, 6);
            this.lblSpNutzung.Name = "lblSpNutzung";
            this.lblSpNutzung.Size = new System.Drawing.Size(72, 30);
            this.lblSpNutzung.TabIndex = 5;
            this.lblSpNutzung.Text = "Nutzung [a]";
            //
            // lblSpWorstBest
            //
            this.lblSpWorstBest.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblSpWorstBest.Location = new System.Drawing.Point(872, 6);
            this.lblSpWorstBest.Name = "lblSpWorstBest";
            this.lblSpWorstBest.Size = new System.Drawing.Size(80, 15);
            this.lblSpWorstBest.TabIndex = 6;
            this.lblSpWorstBest.Text = "Worst/Best";
            //
            // lblReadOnly
            //
            this.lblReadOnly.BackColor = System.Drawing.Color.MistyRose;
            this.lblReadOnly.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblReadOnly.ForeColor = System.Drawing.Color.Firebrick;
            this.lblReadOnly.Location = new System.Drawing.Point(0, 88);
            this.lblReadOnly.Name = "lblReadOnly";
            this.lblReadOnly.Padding = new System.Windows.Forms.Padding(8, 3, 3, 3);
            this.lblReadOnly.Size = new System.Drawing.Size(996, 22);
            this.lblReadOnly.TabIndex = 3;
            this.lblReadOnly.Text = "Auslieferungsvorlage (schreibgeschützt) — zum Ändern „Speichern unter…\" verwenden.";
            this.lblReadOnly.Visible = false;
            //
            // pnlBanner
            //
            this.pnlBanner.BackColor = System.Drawing.Color.LemonChiffon;
            this.pnlBanner.Controls.Add(this.lblBanner);
            this.pnlBanner.Controls.Add(this.btnBannerZu);
            this.pnlBanner.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlBanner.Location = new System.Drawing.Point(0, 44);
            this.pnlBanner.Name = "pnlBanner";
            this.pnlBanner.Padding = new System.Windows.Forms.Padding(8, 4, 4, 4);
            this.pnlBanner.Size = new System.Drawing.Size(996, 44);
            this.pnlBanner.TabIndex = 2;
            //
            // lblBanner
            //
            this.lblBanner.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblBanner.Location = new System.Drawing.Point(8, 4);
            this.lblBanner.Name = "lblBanner";
            this.lblBanner.Size = new System.Drawing.Size(958, 36);
            this.lblBanner.TabIndex = 0;
            this.lblBanner.Text = "Alle Beträge und alle Bezugsgrößen sind NETTO.";
            //
            // btnBannerZu
            //
            this.btnBannerZu.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnBannerZu.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnBannerZu.FlatAppearance.BorderSize = 0;
            this.btnBannerZu.Location = new System.Drawing.Point(966, 4);
            this.btnBannerZu.Name = "btnBannerZu";
            this.btnBannerZu.Size = new System.Drawing.Size(26, 36);
            this.btnBannerZu.TabIndex = 1;
            this.btnBannerZu.Text = "✕";
            this.btnBannerZu.UseVisualStyleBackColor = false;
            this.btnBannerZu.Click += new System.EventHandler(this.btnBannerZu_Click);
            //
            // pnlKontext
            //
            this.pnlKontext.Controls.Add(this.lblKomponente);
            this.pnlKontext.Controls.Add(this.cmbKomponente);
            this.pnlKontext.Controls.Add(this.rbInvest);
            this.pnlKontext.Controls.Add(this.rbBetrieb);
            this.pnlKontext.Controls.Add(this.lblVariante);
            this.pnlKontext.Controls.Add(this.cmbVariante);
            this.pnlKontext.Controls.Add(this.btnVarianteNeu);
            this.pnlKontext.Controls.Add(this.btnSpeichernUnter);
            this.pnlKontext.Controls.Add(this.btnVarianteLoeschen);
            this.pnlKontext.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlKontext.Location = new System.Drawing.Point(0, 0);
            this.pnlKontext.Name = "pnlKontext";
            this.pnlKontext.Size = new System.Drawing.Size(996, 44);
            this.pnlKontext.TabIndex = 1;
            //
            // lblKomponente
            //
            this.lblKomponente.AutoSize = true;
            this.lblKomponente.Location = new System.Drawing.Point(8, 13);
            this.lblKomponente.Name = "lblKomponente";
            this.lblKomponente.Size = new System.Drawing.Size(80, 15);
            this.lblKomponente.TabIndex = 0;
            this.lblKomponente.Text = "Komponente:";
            //
            // cmbKomponente
            //
            this.cmbKomponente.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbKomponente.Location = new System.Drawing.Point(94, 10);
            this.cmbKomponente.Name = "cmbKomponente";
            this.cmbKomponente.Size = new System.Drawing.Size(170, 23);
            this.cmbKomponente.TabIndex = 1;
            this.cmbKomponente.SelectedIndexChanged += new System.EventHandler(this.Kontext_Geaendert);
            //
            // rbInvest
            //
            this.rbInvest.AutoSize = true;
            this.rbInvest.Checked = true;
            this.rbInvest.Location = new System.Drawing.Point(282, 11);
            this.rbInvest.Name = "rbInvest";
            this.rbInvest.Size = new System.Drawing.Size(125, 19);
            this.rbInvest.TabIndex = 2;
            this.rbInvest.TabStop = true;
            this.rbInvest.Text = "Investitionskosten";
            this.rbInvest.UseVisualStyleBackColor = true;
            this.rbInvest.CheckedChanged += new System.EventHandler(this.Kontext_Geaendert);
            //
            // rbBetrieb
            //
            this.rbBetrieb.AutoSize = true;
            this.rbBetrieb.Location = new System.Drawing.Point(412, 11);
            this.rbBetrieb.Name = "rbBetrieb";
            this.rbBetrieb.Size = new System.Drawing.Size(105, 19);
            this.rbBetrieb.TabIndex = 3;
            this.rbBetrieb.Text = "Betriebskosten";
            this.rbBetrieb.UseVisualStyleBackColor = true;
            //
            // lblVariante
            //
            this.lblVariante.AutoSize = true;
            this.lblVariante.Location = new System.Drawing.Point(536, 13);
            this.lblVariante.Name = "lblVariante";
            this.lblVariante.Size = new System.Drawing.Size(54, 15);
            this.lblVariante.TabIndex = 4;
            this.lblVariante.Text = "Variante:";
            //
            // cmbVariante
            //
            this.cmbVariante.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbVariante.Location = new System.Drawing.Point(596, 10);
            this.cmbVariante.Name = "cmbVariante";
            this.cmbVariante.Size = new System.Drawing.Size(170, 23);
            this.cmbVariante.TabIndex = 5;
            this.cmbVariante.SelectedIndexChanged += new System.EventHandler(this.cmbVariante_SelectedIndexChanged);
            //
            // btnVarianteNeu
            //
            this.btnVarianteNeu.Location = new System.Drawing.Point(774, 9);
            this.btnVarianteNeu.Name = "btnVarianteNeu";
            this.btnVarianteNeu.Size = new System.Drawing.Size(58, 25);
            this.btnVarianteNeu.TabIndex = 6;
            this.btnVarianteNeu.Text = "Neu…";
            this.btnVarianteNeu.UseVisualStyleBackColor = true;
            this.btnVarianteNeu.Click += new System.EventHandler(this.btnVarianteNeu_Click);
            //
            // btnSpeichernUnter
            //
            this.btnSpeichernUnter.Location = new System.Drawing.Point(836, 9);
            this.btnSpeichernUnter.Name = "btnSpeichernUnter";
            this.btnSpeichernUnter.Size = new System.Drawing.Size(112, 25);
            this.btnSpeichernUnter.TabIndex = 7;
            this.btnSpeichernUnter.Text = "Speichern unter…";
            this.btnSpeichernUnter.UseVisualStyleBackColor = true;
            this.btnSpeichernUnter.Click += new System.EventHandler(this.btnSpeichernUnter_Click);
            //
            // btnVarianteLoeschen
            //
            this.btnVarianteLoeschen.Location = new System.Drawing.Point(952, 9);
            this.btnVarianteLoeschen.Name = "btnVarianteLoeschen";
            this.btnVarianteLoeschen.Size = new System.Drawing.Size(40, 25);
            this.btnVarianteLoeschen.TabIndex = 8;
            this.btnVarianteLoeschen.Text = "🗑️";
            this.btnVarianteLoeschen.UseVisualStyleBackColor = true;
            this.btnVarianteLoeschen.Click += new System.EventHandler(this.btnVarianteLoeschen_Click);
            //
            // pnlFuss
            //
            this.pnlFuss.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(31)))), ((int)(((byte)(61)))));
            this.pnlFuss.ForeColor = System.Drawing.Color.White;
            this.pnlFuss.Controls.Add(this.btnUebernahme);
            this.pnlFuss.Controls.Add(this.btnKatalog);
            this.pnlFuss.Controls.Add(this.btnPositionNeu);
            this.pnlFuss.Controls.Add(this.lblSummeNetto);
            this.pnlFuss.Controls.Add(this.lblSummeBrutto);
            this.pnlFuss.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFuss.Location = new System.Drawing.Point(0, 555);
            this.pnlFuss.Name = "pnlFuss";
            this.pnlFuss.Size = new System.Drawing.Size(996, 64);
            this.pnlFuss.TabIndex = 0;
            //
            // btnUebernahme
            //
            this.btnUebernahme.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUebernahme.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnUebernahme.Location = new System.Drawing.Point(806, 6);
            this.btnUebernahme.Name = "btnUebernahme";
            this.btnUebernahme.Size = new System.Drawing.Size(182, 27);
            this.btnUebernahme.TabIndex = 3;
            this.btnUebernahme.Text = "In Projekt übernehmen…";
            this.btnUebernahme.UseVisualStyleBackColor = true;
            this.btnUebernahme.Click += new System.EventHandler(this.btnUebernahme_Click);
            // 
            // btnKatalog
            // 
            this.btnKatalog.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnKatalog.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnKatalog.Location = new System.Drawing.Point(806, 35);
            this.btnKatalog.Name = "btnKatalog";
            this.btnKatalog.Size = new System.Drawing.Size(182, 24);
            this.btnKatalog.TabIndex = 9;
            this.btnKatalog.Text = "Positionskatalog…";
            this.btnKatalog.UseVisualStyleBackColor = true;
            this.btnKatalog.Click += new System.EventHandler(this.btnKatalog_Click);
            //
            // btnPositionNeu
            //
            this.btnPositionNeu.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnPositionNeu.Location = new System.Drawing.Point(8, 6);
            this.btnPositionNeu.Name = "btnPositionNeu";
            this.btnPositionNeu.Size = new System.Drawing.Size(170, 27);
            this.btnPositionNeu.TabIndex = 0;
            this.btnPositionNeu.Text = "+ Position hinzufügen";
            this.btnPositionNeu.UseVisualStyleBackColor = true;
            this.btnPositionNeu.Click += new System.EventHandler(this.btnPositionNeu_Click);
            //
            // lblSummeNetto
            //
            this.lblSummeNetto.AutoSize = true;
            this.lblSummeNetto.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSummeNetto.Location = new System.Drawing.Point(300, 10);
            this.lblSummeNetto.Name = "lblSummeNetto";
            this.lblSummeNetto.Size = new System.Drawing.Size(220, 17);
            this.lblSummeNetto.TabIndex = 1;
            this.lblSummeNetto.Text = "Summe Investitionskosten netto: 0,00 €";
            //
            // lblSummeBrutto
            //
            this.lblSummeBrutto.AutoSize = true;
            this.lblSummeBrutto.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblSummeBrutto.Location = new System.Drawing.Point(300, 34);
            this.lblSummeBrutto.Name = "lblSummeBrutto";
            this.lblSummeBrutto.Size = new System.Drawing.Size(180, 17);
            this.lblSummeBrutto.TabIndex = 2;
            this.lblSummeBrutto.Text = "Summe brutto: 0,00 €";
            //
            // tpErtrag
            //
            this.tpErtrag.Controls.Add(this.lblErtragHinweis);
            this.tpErtrag.Location = new System.Drawing.Point(4, 24);
            this.tpErtrag.Name = "tpErtrag";
            this.tpErtrag.Size = new System.Drawing.Size(996, 619);
            this.tpErtrag.TabIndex = 1;
            this.tpErtrag.Text = "Ertrag/Bonus";
            this.tpErtrag.UseVisualStyleBackColor = true;
            //
            // lblErtragHinweis
            //
            this.lblErtragHinweis.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblErtragHinweis.ForeColor = System.Drawing.Color.DimGray;
            this.lblErtragHinweis.Location = new System.Drawing.Point(0, 0);
            this.lblErtragHinweis.Name = "lblErtragHinweis";
            this.lblErtragHinweis.Size = new System.Drawing.Size(996, 619);
            this.lblErtragHinweis.TabIndex = 0;
            this.lblErtragHinweis.Text = "Der Reiter „Ertrag/Bonus\" folgt in Etappe KD5.";
            this.lblErtragHinweis.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // Form_KostenKomponente
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1004, 721);
            this.Controls.Add(this.tabHaupt);
            this.Controls.Add(this.pnlKopf);
            this.MinimumSize = new System.Drawing.Size(1020, 600);
            this.Name = "Form_KostenKomponente";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Kostenverwaltung";
            this.pnlKopf.ResumeLayout(false);
            this.pnlKopf.PerformLayout();
            this.tabHaupt.ResumeLayout(false);
            this.tpKosten.ResumeLayout(false);
            this.pnlRasterKopf.ResumeLayout(false);
            this.pnlBanner.ResumeLayout(false);
            this.pnlKontext.ResumeLayout(false);
            this.pnlKontext.PerformLayout();
            this.pnlFuss.ResumeLayout(false);
            this.pnlFuss.PerformLayout();
            this.tpErtrag.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlKopf;
        private System.Windows.Forms.Label lblTitel;
        private System.Windows.Forms.Label lblUntertitel;
        private System.Windows.Forms.TabControl tabHaupt;
        private System.Windows.Forms.TabPage tpKosten;
        private System.Windows.Forms.Panel pnlZeilen;
        private System.Windows.Forms.Panel pnlRasterKopf;
        private System.Windows.Forms.Label lblSpAktionen;
        private System.Windows.Forms.Label lblSpPosition;
        private System.Windows.Forms.Label lblSpBemessung;
        private System.Windows.Forms.Label lblSpSatz;
        private System.Windows.Forms.Label lblSpBetrag;
        private System.Windows.Forms.Label lblSpNutzung;
        private System.Windows.Forms.Label lblSpWorstBest;
        private System.Windows.Forms.Label lblReadOnly;
        private System.Windows.Forms.Panel pnlBanner;
        private System.Windows.Forms.Label lblBanner;
        private System.Windows.Forms.Button btnBannerZu;
        private System.Windows.Forms.Panel pnlKontext;
        private System.Windows.Forms.Label lblKomponente;
        private System.Windows.Forms.ComboBox cmbKomponente;
        private System.Windows.Forms.RadioButton rbInvest;
        private System.Windows.Forms.RadioButton rbBetrieb;
        private System.Windows.Forms.Label lblVariante;
        private System.Windows.Forms.ComboBox cmbVariante;
        private System.Windows.Forms.Button btnVarianteNeu;
        private System.Windows.Forms.Button btnSpeichernUnter;
        private System.Windows.Forms.Button btnVarianteLoeschen;
        private System.Windows.Forms.Panel pnlFuss;
        private System.Windows.Forms.Button btnUebernahme;
        private System.Windows.Forms.Button btnKatalog;
        private System.Windows.Forms.Button btnPositionNeu;
        private System.Windows.Forms.Label lblSummeNetto;
        private System.Windows.Forms.Label lblSummeBrutto;
        private System.Windows.Forms.TabPage tpErtrag;
        private System.Windows.Forms.Label lblErtragHinweis;
    }
}
