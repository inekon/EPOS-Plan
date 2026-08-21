namespace WindowsFormsApplication1
{
    partial class Form_PufferSp_Projekt
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
            this._gbListe = new System.Windows.Forms.GroupBox();
            this._lbProjekt = new System.Windows.Forms.ListBox();
            this._btnNeu = new System.Windows.Forms.Button();
            this._btnEntfernen = new System.Windows.Forms.Button();
            this._btnKatalog = new System.Windows.Forms.Button();
            this._gbDaten = new System.Windows.Forms.GroupBox();
            this._lblAusKatalog = new System.Windows.Forms.Label();
            this._cbKatalog = new System.Windows.Forms.ComboBox();
            this._lblBezeichner = new System.Windows.Forms.Label();
            this._tbBezeichner = new System.Windows.Forms.TextBox();
            this._lblVerwendung = new System.Windows.Forms.Label();
            this._cbVerwendung = new System.Windows.Forms.ComboBox();
            this._lblGesamtvolumen = new System.Windows.Forms.Label();
            this._tbVolumen = new System.Windows.Forms.TextBox();
            this._lblBereitschaftsverluste = new System.Windows.Forms.Label();
            this._tbVerluste = new System.Windows.Forms.TextBox();
            this._lblVorlauf = new System.Windows.Forms.Label();
            this._tbVorlauf = new System.Windows.Forms.TextBox();
            this._lblRuecklauf = new System.Windows.Forms.Label();
            this._tbRuecklauf = new System.Windows.Forms.TextBox();
            this._lblQmax = new System.Windows.Forms.Label();
            this._lblEinschaltschwelle = new System.Windows.Forms.Label();
            this._tbSchwelleEin = new System.Windows.Forms.TextBox();
            this._lblAbschaltschwelle = new System.Windows.Forms.Label();
            this._tbSchwelleAus = new System.Windows.Forms.TextBox();
            this._lblSchwelleNachrangig = new System.Windows.Forms.Label();
            this._tbSchwelleNachrang = new System.Windows.Forms.TextBox();
            this._lblMindestfuellstand = new System.Windows.Forms.Label();
            this._tbSchwelleReserve = new System.Windows.Forms.TextBox();
            this._gbLaden = new System.Windows.Forms.GroupBox();
            this._lvLaden = new System.Windows.Forms.ListView();
            this._colNr = new System.Windows.Forms.ColumnHeader();
            this._colAnlage = new System.Windows.Forms.ColumnHeader();
            this._colErzeuger = new System.Windows.Forms.ColumnHeader();
            this._colSenke = new System.Windows.Forms.ColumnHeader();
            this._colLadeprio = new System.Windows.Forms.ColumnHeader();
            this._colLaedtBis = new System.Windows.Forms.ColumnHeader();
            this._lblEntladeprio = new System.Windows.Forms.Label();
            this._cbEntladeprio = new System.Windows.Forms.ComboBox();
            this._lblEntladeInfo = new System.Windows.Forms.Label();
            this._lblStatus = new System.Windows.Forms.Label();
            this._btnUebernehmen = new System.Windows.Forms.Button();
            this._btnSchliessen = new System.Windows.Forms.Button();
            this._gbListe.SuspendLayout();
            this._gbDaten.SuspendLayout();
            this._gbLaden.SuspendLayout();
            this.SuspendLayout();
            //
            // _lbProjekt
            //
            this._lbProjekt.Location = new System.Drawing.Point(14, 22);
            this._lbProjekt.Name = "_lbProjekt";
            this._lbProjekt.Size = new System.Drawing.Size(420, 102);
            this._lbProjekt.SelectedIndexChanged += new System.EventHandler(this.lbProjekt_SelectedIndexChanged);
            //
            // _btnNeu
            //
            this._btnNeu.Location = new System.Drawing.Point(446, 22);
            this._btnNeu.Name = "_btnNeu";
            this._btnNeu.Size = new System.Drawing.Size(214, 30);
            this._btnNeu.Text = "Neuer Pufferspeicher";
            this._btnNeu.Click += new System.EventHandler(this.btnNeu_Click);
            //
            // _btnEntfernen
            //
            this._btnEntfernen.Location = new System.Drawing.Point(446, 58);
            this._btnEntfernen.Name = "_btnEntfernen";
            this._btnEntfernen.Size = new System.Drawing.Size(214, 30);
            this._btnEntfernen.Text = "Entfernen";
            this._btnEntfernen.Click += new System.EventHandler(this.btnEntfernen_Click);
            //
            // _btnKatalog
            //
            this._btnKatalog.Location = new System.Drawing.Point(446, 94);
            this._btnKatalog.Name = "_btnKatalog";
            this._btnKatalog.Size = new System.Drawing.Size(214, 30);
            this._btnKatalog.Text = "Katalog ansehen…";
            this._btnKatalog.Click += new System.EventHandler(this.btnKatalog_Click);
            //
            // _gbListe
            //
            this._gbListe.Controls.Add(this._lbProjekt);
            this._gbListe.Controls.Add(this._btnNeu);
            this._gbListe.Controls.Add(this._btnEntfernen);
            this._gbListe.Controls.Add(this._btnKatalog);
            this._gbListe.Location = new System.Drawing.Point(12, 8);
            this._gbListe.Name = "_gbListe";
            this._gbListe.Size = new System.Drawing.Size(676, 134);
            this._gbListe.Text = "Pufferspeicher im Projekt";
            //
            // _lblAusKatalog
            //
            this._lblAusKatalog.AutoSize = true;
            this._lblAusKatalog.Location = new System.Drawing.Point(16, 26);
            this._lblAusKatalog.Name = "_lblAusKatalog";
            this._lblAusKatalog.Text = "Aus Katalog:";
            //
            // _cbKatalog
            //
            this._cbKatalog.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbKatalog.Location = new System.Drawing.Point(180, 22);
            this._cbKatalog.Name = "_cbKatalog";
            this._cbKatalog.Width = 300;
            this._cbKatalog.SelectedIndexChanged += new System.EventHandler(this.cbKatalog_SelectedIndexChanged);
            //
            // _lblBezeichner
            //
            this._lblBezeichner.AutoSize = true;
            this._lblBezeichner.Location = new System.Drawing.Point(16, 58);
            this._lblBezeichner.Name = "_lblBezeichner";
            this._lblBezeichner.Text = "Bezeichner:";
            //
            // _tbBezeichner
            //
            this._tbBezeichner.Location = new System.Drawing.Point(180, 55);
            this._tbBezeichner.Name = "_tbBezeichner";
            this._tbBezeichner.Width = 200;
            //
            // _lblVerwendung
            //
            this._lblVerwendung.AutoSize = true;
            this._lblVerwendung.Location = new System.Drawing.Point(16, 90);
            this._lblVerwendung.Name = "_lblVerwendung";
            this._lblVerwendung.Text = "Verwendung:";
            //
            // _cbVerwendung
            //
            this._cbVerwendung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbVerwendung.Location = new System.Drawing.Point(180, 86);
            this._cbVerwendung.Name = "_cbVerwendung";
            this._cbVerwendung.Width = 180;
            this._cbVerwendung.SelectedIndexChanged += new System.EventHandler(this.Daten_Geaendert);
            //
            // _lblGesamtvolumen
            //
            this._lblGesamtvolumen.AutoSize = true;
            this._lblGesamtvolumen.Location = new System.Drawing.Point(388, 58);
            this._lblGesamtvolumen.Name = "_lblGesamtvolumen";
            this._lblGesamtvolumen.Text = "Gesamtvolumen [l]:";
            //
            // _tbVolumen
            //
            this._tbVolumen.Location = new System.Drawing.Point(556, 55);
            this._tbVolumen.Name = "_tbVolumen";
            this._tbVolumen.Width = 110;
            this._tbVolumen.TextChanged += new System.EventHandler(this.Kapazitaet_Geaendert);
            //
            // _lblBereitschaftsverluste
            //
            this._lblBereitschaftsverluste.AutoSize = true;
            this._lblBereitschaftsverluste.Location = new System.Drawing.Point(388, 90);
            this._lblBereitschaftsverluste.Name = "_lblBereitschaftsverluste";
            this._lblBereitschaftsverluste.Text = "Bereitschaftsverl. [kWh/24h]:";
            //
            // _tbVerluste
            //
            this._tbVerluste.Location = new System.Drawing.Point(556, 87);
            this._tbVerluste.Name = "_tbVerluste";
            this._tbVerluste.Width = 110;
            //
            // _lblVorlauf
            //
            this._lblVorlauf.AutoSize = true;
            this._lblVorlauf.Location = new System.Drawing.Point(16, 124);
            this._lblVorlauf.Name = "_lblVorlauf";
            this._lblVorlauf.Text = "Vorlauf [°C]:";
            //
            // _tbVorlauf
            //
            this._tbVorlauf.Location = new System.Drawing.Point(180, 121);
            this._tbVorlauf.Name = "_tbVorlauf";
            this._tbVorlauf.Width = 60;
            this._tbVorlauf.TextChanged += new System.EventHandler(this.Kapazitaet_Geaendert);
            //
            // _lblRuecklauf
            //
            this._lblRuecklauf.AutoSize = true;
            this._lblRuecklauf.Location = new System.Drawing.Point(260, 124);
            this._lblRuecklauf.Name = "_lblRuecklauf";
            this._lblRuecklauf.Text = "Rücklauf [°C]:";
            //
            // _tbRuecklauf
            //
            this._tbRuecklauf.Location = new System.Drawing.Point(360, 121);
            this._tbRuecklauf.Name = "_tbRuecklauf";
            this._tbRuecklauf.Width = 60;
            this._tbRuecklauf.TextChanged += new System.EventHandler(this.Kapazitaet_Geaendert);
            //
            // _lblQmax
            //
            this._lblQmax.AutoSize = false;
            this._lblQmax.Location = new System.Drawing.Point(436, 124);
            this._lblQmax.Name = "_lblQmax";
            this._lblQmax.Size = new System.Drawing.Size(220, 18);
            this._lblQmax.Text = "→  Q_max {0} kWh";
            //
            // _lblEinschaltschwelle
            //
            this._lblEinschaltschwelle.AutoSize = true;
            this._lblEinschaltschwelle.Location = new System.Drawing.Point(16, 160);
            this._lblEinschaltschwelle.Name = "_lblEinschaltschwelle";
            this._lblEinschaltschwelle.Text = "Einschaltschwelle [%]:";
            //
            // _tbSchwelleEin
            //
            this._tbSchwelleEin.Location = new System.Drawing.Point(180, 157);
            this._tbSchwelleEin.Name = "_tbSchwelleEin";
            this._tbSchwelleEin.Width = 60;
            //
            // _lblAbschaltschwelle
            //
            this._lblAbschaltschwelle.AutoSize = true;
            this._lblAbschaltschwelle.Location = new System.Drawing.Point(260, 160);
            this._lblAbschaltschwelle.Name = "_lblAbschaltschwelle";
            this._lblAbschaltschwelle.Text = "Abschaltschwelle [%]:";
            //
            // _tbSchwelleAus
            //
            this._tbSchwelleAus.Location = new System.Drawing.Point(400, 157);
            this._tbSchwelleAus.Name = "_tbSchwelleAus";
            this._tbSchwelleAus.Width = 60;
            //
            // _lblSchwelleNachrangig
            //
            this._lblSchwelleNachrangig.AutoSize = true;
            this._lblSchwelleNachrangig.Location = new System.Drawing.Point(480, 160);
            this._lblSchwelleNachrangig.Name = "_lblSchwelleNachrangig";
            this._lblSchwelleNachrangig.Text = "… nachrangig [%]:";
            //
            // _tbSchwelleNachrang
            //
            this._tbSchwelleNachrang.Location = new System.Drawing.Point(600, 157);
            this._tbSchwelleNachrang.Name = "_tbSchwelleNachrang";
            this._tbSchwelleNachrang.Width = 56;
            //
            // _lblMindestfuellstand
            //
            this._lblMindestfuellstand.AutoSize = true;
            this._lblMindestfuellstand.Location = new System.Drawing.Point(16, 193);
            this._lblMindestfuellstand.Name = "_lblMindestfuellstand";
            this._lblMindestfuellstand.Text = "Mindestfüllstand/Notreserve [%]:";
            //
            // _tbSchwelleReserve
            //
            this._tbSchwelleReserve.Location = new System.Drawing.Point(284, 190);
            this._tbSchwelleReserve.Name = "_tbSchwelleReserve";
            this._tbSchwelleReserve.Width = 60;
            //
            // _gbDaten
            //
            this._gbDaten.Controls.Add(this._lblAusKatalog);
            this._gbDaten.Controls.Add(this._cbKatalog);
            this._gbDaten.Controls.Add(this._lblBezeichner);
            this._gbDaten.Controls.Add(this._tbBezeichner);
            this._gbDaten.Controls.Add(this._lblVerwendung);
            this._gbDaten.Controls.Add(this._cbVerwendung);
            this._gbDaten.Controls.Add(this._lblGesamtvolumen);
            this._gbDaten.Controls.Add(this._tbVolumen);
            this._gbDaten.Controls.Add(this._lblBereitschaftsverluste);
            this._gbDaten.Controls.Add(this._tbVerluste);
            this._gbDaten.Controls.Add(this._lblVorlauf);
            this._gbDaten.Controls.Add(this._tbVorlauf);
            this._gbDaten.Controls.Add(this._lblRuecklauf);
            this._gbDaten.Controls.Add(this._tbRuecklauf);
            this._gbDaten.Controls.Add(this._lblQmax);
            this._gbDaten.Controls.Add(this._lblEinschaltschwelle);
            this._gbDaten.Controls.Add(this._tbSchwelleEin);
            this._gbDaten.Controls.Add(this._lblAbschaltschwelle);
            this._gbDaten.Controls.Add(this._tbSchwelleAus);
            this._gbDaten.Controls.Add(this._lblSchwelleNachrangig);
            this._gbDaten.Controls.Add(this._tbSchwelleNachrang);
            this._gbDaten.Controls.Add(this._lblMindestfuellstand);
            this._gbDaten.Controls.Add(this._tbSchwelleReserve);
            this._gbDaten.Location = new System.Drawing.Point(12, 148);
            this._gbDaten.Name = "_gbDaten";
            this._gbDaten.Size = new System.Drawing.Size(676, 232);
            this._gbDaten.Text = "Eigenschaften";
            //
            // _colNr
            //
            this._colNr.Name = "_colNr";
            this._colNr.Text = "#";
            this._colNr.Width = 30;
            //
            // _colAnlage
            //
            this._colAnlage.Name = "_colAnlage";
            this._colAnlage.Text = "Anlage";
            this._colAnlage.Width = 220;
            //
            // _colErzeuger
            //
            this._colErzeuger.Name = "_colErzeuger";
            this._colErzeuger.Text = "Erzeuger";
            this._colErzeuger.Width = 120;
            //
            // _colSenke
            //
            this._colSenke.Name = "_colSenke";
            this._colSenke.Text = "Senke";
            this._colSenke.Width = 90;
            //
            // _colLadeprio
            //
            this._colLadeprio.Name = "_colLadeprio";
            this._colLadeprio.Text = "Ladeprio";
            this._colLadeprio.Width = 80;
            //
            // _colLaedtBis
            //
            this._colLaedtBis.Name = "_colLaedtBis";
            this._colLaedtBis.Text = "lädt bis";
            this._colLaedtBis.Width = 90;
            //
            // _lvLaden
            //
            this._lvLaden.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this._colNr,
            this._colAnlage,
            this._colErzeuger,
            this._colSenke,
            this._colLadeprio,
            this._colLaedtBis});
            this._lvLaden.FullRowSelect = true;
            this._lvLaden.GridLines = true;
            this._lvLaden.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
            this._lvLaden.Location = new System.Drawing.Point(14, 22);
            this._lvLaden.MultiSelect = false;
            this._lvLaden.Name = "_lvLaden";
            this._lvLaden.Size = new System.Drawing.Size(646, 118);
            this._lvLaden.View = System.Windows.Forms.View.Details;
            //
            // _gbLaden
            //
            this._gbLaden.Controls.Add(this._lvLaden);
            this._gbLaden.Location = new System.Drawing.Point(12, 386);
            this._gbLaden.Name = "_gbLaden";
            this._gbLaden.Size = new System.Drawing.Size(676, 152);
            this._gbLaden.Text = "Ladereihenfolge dieses Speichers (aus den Erzeugerzuordnungen)";
            //
            // _lblEntladeprio
            //
            this._lblEntladeprio.AutoSize = true;
            this._lblEntladeprio.Location = new System.Drawing.Point(16, 550);
            this._lblEntladeprio.Name = "_lblEntladeprio";
            this._lblEntladeprio.Text = "Entladepriorität:";
            //
            // _cbEntladeprio
            //
            this._cbEntladeprio.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cbEntladeprio.Location = new System.Drawing.Point(180, 546);
            this._cbEntladeprio.Name = "_cbEntladeprio";
            this._cbEntladeprio.Width = 210;
            //
            // _lblEntladeInfo
            //
            this._lblEntladeInfo.AutoSize = false;
            this._lblEntladeInfo.Location = new System.Drawing.Point(400, 550);
            this._lblEntladeInfo.Name = "_lblEntladeInfo";
            this._lblEntladeInfo.Size = new System.Drawing.Size(288, 32);
            this._lblEntladeInfo.Text = "Wird als {0}. von {1} {2} entladen.";
            //
            // _lblStatus
            //
            this._lblStatus.AutoSize = false;
            this._lblStatus.Location = new System.Drawing.Point(14, 590);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(380, 32);
            //
            // _btnUebernehmen
            //
            this._btnUebernehmen.Location = new System.Drawing.Point(400, 622);
            this._btnUebernehmen.Name = "_btnUebernehmen";
            this._btnUebernehmen.Size = new System.Drawing.Size(130, 30);
            this._btnUebernehmen.Text = "Übernehmen";
            this._btnUebernehmen.Click += new System.EventHandler(this.btnUebernehmen_Click);
            //
            // _btnSchliessen
            //
            this._btnSchliessen.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnSchliessen.Location = new System.Drawing.Point(550, 622);
            this._btnSchliessen.Name = "_btnSchliessen";
            this._btnSchliessen.Size = new System.Drawing.Size(130, 30);
            this._btnSchliessen.Text = "Schließen";
            //
            // Form_PufferSp_Projekt
            //
            this.AcceptButton = this._btnUebernehmen;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this._btnSchliessen;
            this.ClientSize = new System.Drawing.Size(700, 662);
            this.Controls.Add(this._gbListe);
            this.Controls.Add(this._gbDaten);
            this.Controls.Add(this._gbLaden);
            this.Controls.Add(this._lblEntladeprio);
            this.Controls.Add(this._cbEntladeprio);
            this.Controls.Add(this._lblEntladeInfo);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._btnUebernehmen);
            this.Controls.Add(this._btnSchliessen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_PufferSp_Projekt";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Pufferspeicher im Projekt";
            this._gbListe.ResumeLayout(false);
            this._gbDaten.ResumeLayout(false);
            this._gbDaten.PerformLayout();
            this._gbLaden.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox _gbListe;
        private System.Windows.Forms.ListBox _lbProjekt;
        private System.Windows.Forms.Button _btnNeu;
        private System.Windows.Forms.Button _btnEntfernen;
        private System.Windows.Forms.Button _btnKatalog;
        private System.Windows.Forms.GroupBox _gbDaten;
        private System.Windows.Forms.Label _lblAusKatalog;
        private System.Windows.Forms.ComboBox _cbKatalog;
        private System.Windows.Forms.Label _lblBezeichner;
        private System.Windows.Forms.TextBox _tbBezeichner;
        private System.Windows.Forms.Label _lblVerwendung;
        private System.Windows.Forms.ComboBox _cbVerwendung;
        private System.Windows.Forms.Label _lblGesamtvolumen;
        private System.Windows.Forms.TextBox _tbVolumen;
        private System.Windows.Forms.Label _lblBereitschaftsverluste;
        private System.Windows.Forms.TextBox _tbVerluste;
        private System.Windows.Forms.Label _lblVorlauf;
        private System.Windows.Forms.TextBox _tbVorlauf;
        private System.Windows.Forms.Label _lblRuecklauf;
        private System.Windows.Forms.TextBox _tbRuecklauf;
        private System.Windows.Forms.Label _lblQmax;
        private System.Windows.Forms.Label _lblEinschaltschwelle;
        private System.Windows.Forms.TextBox _tbSchwelleEin;
        private System.Windows.Forms.Label _lblAbschaltschwelle;
        private System.Windows.Forms.TextBox _tbSchwelleAus;
        private System.Windows.Forms.Label _lblSchwelleNachrangig;
        private System.Windows.Forms.TextBox _tbSchwelleNachrang;
        private System.Windows.Forms.Label _lblMindestfuellstand;
        private System.Windows.Forms.TextBox _tbSchwelleReserve;
        private System.Windows.Forms.GroupBox _gbLaden;
        private System.Windows.Forms.ListView _lvLaden;
        private System.Windows.Forms.ColumnHeader _colNr;
        private System.Windows.Forms.ColumnHeader _colAnlage;
        private System.Windows.Forms.ColumnHeader _colErzeuger;
        private System.Windows.Forms.ColumnHeader _colSenke;
        private System.Windows.Forms.ColumnHeader _colLadeprio;
        private System.Windows.Forms.ColumnHeader _colLaedtBis;
        private System.Windows.Forms.Label _lblEntladeprio;
        private System.Windows.Forms.ComboBox _cbEntladeprio;
        private System.Windows.Forms.Label _lblEntladeInfo;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.Button _btnUebernehmen;
        private System.Windows.Forms.Button _btnSchliessen;
    }
}
