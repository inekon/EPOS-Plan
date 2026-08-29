namespace WindowsFormsApplication1
{
    partial class ucBrennstoffBestandteile
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
            this._gbBestandteile = new System.Windows.Forms.GroupBox();
            this._rbAufgeschluesselt = new System.Windows.Forms.RadioButton();
            this._rbGesamtwert = new System.Windows.Forms.RadioButton();
            this._chkEnergiesteuer = new System.Windows.Forms.CheckBox();
            this._tbEnergiesteuer = new System.Windows.Forms.TextBox();
            this._lblEinheitEnergiesteuer = new System.Windows.Forms.Label();
            this._lblSchnellwahl = new System.Windows.Forms.Label();
            this._btnSatzRegel = new System.Windows.Forms.Button();
            this._btnSatz53a = new System.Windows.Forms.Button();
            this._btnSatz54 = new System.Windows.Forms.Button();
            this._chkCo2 = new System.Windows.Forms.CheckBox();
            this._tbCo2 = new System.Windows.Forms.TextBox();
            this._lblEinheitCo2 = new System.Windows.Forms.Label();
            this._btnCo2 = new System.Windows.Forms.Button();
            this._chkNetzentgelt = new System.Windows.Forms.CheckBox();
            this._tbNetzentgelt = new System.Windows.Forms.TextBox();
            this._lblEinheitNetzentgelt = new System.Windows.Forms.Label();
            this._chkVertrieb = new System.Windows.Forms.CheckBox();
            this._tbVertrieb = new System.Windows.Forms.TextBox();
            this._lblEinheitVertrieb = new System.Windows.Forms.Label();
            this._lblSumme = new System.Windows.Forms.Label();
            this._lblArbeitspreis = new System.Windows.Forms.Label();
            this._lblArbeitspreisWert = new System.Windows.Forms.Label();
            this._lblEinheitArbeitspreis = new System.Windows.Forms.Label();
            this._lblRest = new System.Windows.Forms.Label();
            this._btnInArbeitspreis = new System.Windows.Forms.Button();
            this._lblQuelle = new System.Windows.Forms.Label();
            this._gbBestandteile.SuspendLayout();
            this.SuspendLayout();
            //
            // _gbBestandteile
            //
            this._gbBestandteile.Controls.Add(this._rbAufgeschluesselt);
            this._gbBestandteile.Controls.Add(this._rbGesamtwert);
            this._gbBestandteile.Controls.Add(this._chkEnergiesteuer);
            this._gbBestandteile.Controls.Add(this._tbEnergiesteuer);
            this._gbBestandteile.Controls.Add(this._lblEinheitEnergiesteuer);
            this._gbBestandteile.Controls.Add(this._lblSchnellwahl);
            this._gbBestandteile.Controls.Add(this._btnSatzRegel);
            this._gbBestandteile.Controls.Add(this._btnSatz53a);
            this._gbBestandteile.Controls.Add(this._btnSatz54);
            this._gbBestandteile.Controls.Add(this._chkCo2);
            this._gbBestandteile.Controls.Add(this._tbCo2);
            this._gbBestandteile.Controls.Add(this._lblEinheitCo2);
            this._gbBestandteile.Controls.Add(this._btnCo2);
            this._gbBestandteile.Controls.Add(this._chkNetzentgelt);
            this._gbBestandteile.Controls.Add(this._tbNetzentgelt);
            this._gbBestandteile.Controls.Add(this._lblEinheitNetzentgelt);
            this._gbBestandteile.Controls.Add(this._chkVertrieb);
            this._gbBestandteile.Controls.Add(this._tbVertrieb);
            this._gbBestandteile.Controls.Add(this._lblEinheitVertrieb);
            this._gbBestandteile.Controls.Add(this._lblSumme);
            this._gbBestandteile.Controls.Add(this._lblArbeitspreis);
            this._gbBestandteile.Controls.Add(this._lblArbeitspreisWert);
            this._gbBestandteile.Controls.Add(this._lblEinheitArbeitspreis);
            this._gbBestandteile.Controls.Add(this._lblRest);
            this._gbBestandteile.Controls.Add(this._btnInArbeitspreis);
            this._gbBestandteile.Controls.Add(this._lblQuelle);
            this._gbBestandteile.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._gbBestandteile.Location = new System.Drawing.Point(0, 0);
            this._gbBestandteile.Name = "_gbBestandteile";
            this._gbBestandteile.Size = new System.Drawing.Size(548, 300);
            this._gbBestandteile.Text = "Preisbestandteile des Brennstoffs";
            //
            // _rbAufgeschluesselt
            //
            this._rbAufgeschluesselt.AutoSize = true;
            this._rbAufgeschluesselt.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._rbAufgeschluesselt.Location = new System.Drawing.Point(14, 22);
            this._rbAufgeschluesselt.Name = "_rbAufgeschluesselt";
            this._rbAufgeschluesselt.Text = "aufgeschluesselt";
            this._rbAufgeschluesselt.CheckedChanged += new System.EventHandler(this.rbAufgeschluesselt_CheckedChanged);
            //
            // _rbGesamtwert
            //
            this._rbGesamtwert.AutoSize = true;
            this._rbGesamtwert.Checked = true;
            this._rbGesamtwert.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._rbGesamtwert.Location = new System.Drawing.Point(224, 22);
            this._rbGesamtwert.Name = "_rbGesamtwert";
            this._rbGesamtwert.TabStop = true;
            this._rbGesamtwert.Text = "Gesamtwert (Arbeitspreis gilt)";
            //
            // _chkEnergiesteuer
            //
            this._chkEnergiesteuer.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._chkEnergiesteuer.Location = new System.Drawing.Point(14, 50);
            this._chkEnergiesteuer.Name = "_chkEnergiesteuer";
            this._chkEnergiesteuer.Size = new System.Drawing.Size(228, 21);
            this._chkEnergiesteuer.Text = "Energiesteuer";
            this._chkEnergiesteuer.CheckedChanged += new System.EventHandler(this.KomponenteSchalter_CheckedChanged);
            //
            // _tbEnergiesteuer
            //
            this._tbEnergiesteuer.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbEnergiesteuer.Location = new System.Drawing.Point(250, 48);
            this._tbEnergiesteuer.Name = "_tbEnergiesteuer";
            this._tbEnergiesteuer.Size = new System.Drawing.Size(92, 23);
            this._tbEnergiesteuer.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbEnergiesteuer.TextChanged += new System.EventHandler(this.Zahlenfeld_TextChanged);
            //
            // _lblEinheitEnergiesteuer
            //
            this._lblEinheitEnergiesteuer.AutoSize = true;
            this._lblEinheitEnergiesteuer.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitEnergiesteuer.Location = new System.Drawing.Point(350, 51);
            this._lblEinheitEnergiesteuer.Name = "_lblEinheitEnergiesteuer";
            this._lblEinheitEnergiesteuer.Text = "ct/kWh";
            //
            // _lblSchnellwahl
            //
            this._lblSchnellwahl.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular);
            this._lblSchnellwahl.Location = new System.Drawing.Point(14, 77);
            this._lblSchnellwahl.Name = "_lblSchnellwahl";
            this._lblSchnellwahl.Size = new System.Drawing.Size(150, 21);
            this._lblSchnellwahl.Text = "Schnellwahl (Katalog):";
            //
            // _btnSatzRegel
            //
            this._btnSatzRegel.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            this._btnSatzRegel.Location = new System.Drawing.Point(168, 73);
            this._btnSatzRegel.Name = "_btnSatzRegel";
            this._btnSatzRegel.Size = new System.Drawing.Size(118, 24);
            this._btnSatzRegel.Text = "Regelsatz";
            this._btnSatzRegel.Click += new System.EventHandler(this.btnSatzRegel_Click);
            //
            // _btnSatz53a
            //
            this._btnSatz53a.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            this._btnSatz53a.Location = new System.Drawing.Point(290, 73);
            this._btnSatz53a.Name = "_btnSatz53a";
            this._btnSatz53a.Size = new System.Drawing.Size(118, 24);
            this._btnSatz53a.Text = "53a Abs. 5";
            this._btnSatz53a.Click += new System.EventHandler(this.btnSatz53a_Click);
            //
            // _btnSatz54
            //
            this._btnSatz54.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            this._btnSatz54.Location = new System.Drawing.Point(412, 73);
            this._btnSatz54.Name = "_btnSatz54";
            this._btnSatz54.Size = new System.Drawing.Size(118, 24);
            this._btnSatz54.Text = "54";
            this._btnSatz54.Click += new System.EventHandler(this.btnSatz54_Click);
            //
            // _chkCo2
            //
            this._chkCo2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._chkCo2.Location = new System.Drawing.Point(14, 104);
            this._chkCo2.Name = "_chkCo2";
            this._chkCo2.Size = new System.Drawing.Size(228, 21);
            this._chkCo2.Text = "CO2-Anteil (BEHG)";
            this._chkCo2.CheckedChanged += new System.EventHandler(this.KomponenteSchalter_CheckedChanged);
            //
            // _tbCo2
            //
            this._tbCo2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbCo2.Location = new System.Drawing.Point(250, 102);
            this._tbCo2.Name = "_tbCo2";
            this._tbCo2.Size = new System.Drawing.Size(92, 23);
            this._tbCo2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbCo2.TextChanged += new System.EventHandler(this.Zahlenfeld_TextChanged);
            //
            // _lblEinheitCo2
            //
            this._lblEinheitCo2.AutoSize = true;
            this._lblEinheitCo2.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitCo2.Location = new System.Drawing.Point(350, 105);
            this._lblEinheitCo2.Name = "_lblEinheitCo2";
            this._lblEinheitCo2.Text = "ct/kWh";
            //
            // _btnCo2
            //
            this._btnCo2.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            this._btnCo2.Location = new System.Drawing.Point(402, 101);
            this._btnCo2.Name = "_btnCo2";
            this._btnCo2.Size = new System.Drawing.Size(128, 24);
            this._btnCo2.Text = "CO2-Preis";
            this._btnCo2.Click += new System.EventHandler(this.btnCo2_Click);
            //
            // _chkNetzentgelt
            //
            this._chkNetzentgelt.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._chkNetzentgelt.Location = new System.Drawing.Point(14, 131);
            this._chkNetzentgelt.Name = "_chkNetzentgelt";
            this._chkNetzentgelt.Size = new System.Drawing.Size(228, 21);
            this._chkNetzentgelt.Text = "Netz-/Messentgelt";
            this._chkNetzentgelt.CheckedChanged += new System.EventHandler(this.KomponenteSchalter_CheckedChanged);
            //
            // _tbNetzentgelt
            //
            this._tbNetzentgelt.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbNetzentgelt.Location = new System.Drawing.Point(250, 129);
            this._tbNetzentgelt.Name = "_tbNetzentgelt";
            this._tbNetzentgelt.Size = new System.Drawing.Size(92, 23);
            this._tbNetzentgelt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbNetzentgelt.TextChanged += new System.EventHandler(this.Zahlenfeld_TextChanged);
            //
            // _lblEinheitNetzentgelt
            //
            this._lblEinheitNetzentgelt.AutoSize = true;
            this._lblEinheitNetzentgelt.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitNetzentgelt.Location = new System.Drawing.Point(350, 132);
            this._lblEinheitNetzentgelt.Name = "_lblEinheitNetzentgelt";
            this._lblEinheitNetzentgelt.Text = "ct/kWh";
            //
            // _chkVertrieb
            //
            this._chkVertrieb.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._chkVertrieb.Location = new System.Drawing.Point(14, 158);
            this._chkVertrieb.Name = "_chkVertrieb";
            this._chkVertrieb.Size = new System.Drawing.Size(228, 21);
            this._chkVertrieb.Text = "Vertrieb";
            this._chkVertrieb.CheckedChanged += new System.EventHandler(this.KomponenteSchalter_CheckedChanged);
            //
            // _tbVertrieb
            //
            this._tbVertrieb.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbVertrieb.Location = new System.Drawing.Point(250, 156);
            this._tbVertrieb.Name = "_tbVertrieb";
            this._tbVertrieb.Size = new System.Drawing.Size(92, 23);
            this._tbVertrieb.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbVertrieb.TextChanged += new System.EventHandler(this.Zahlenfeld_TextChanged);
            //
            // _lblEinheitVertrieb
            //
            this._lblEinheitVertrieb.AutoSize = true;
            this._lblEinheitVertrieb.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitVertrieb.Location = new System.Drawing.Point(350, 159);
            this._lblEinheitVertrieb.Name = "_lblEinheitVertrieb";
            this._lblEinheitVertrieb.Text = "ct/kWh";
            //
            // _lblSumme
            //
            this._lblSumme.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this._lblSumme.Location = new System.Drawing.Point(14, 189);
            this._lblSumme.Name = "_lblSumme";
            this._lblSumme.Size = new System.Drawing.Size(520, 20);
            //
            // _lblArbeitspreis
            //
            this._lblArbeitspreis.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblArbeitspreis.Location = new System.Drawing.Point(14, 216);
            this._lblArbeitspreis.Name = "_lblArbeitspreis";
            this._lblArbeitspreis.Size = new System.Drawing.Size(228, 21);
            this._lblArbeitspreis.Text = "Arbeitspreis";
            //
            // _lblArbeitspreisWert
            //
            this._lblArbeitspreisWert.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblArbeitspreisWert.Location = new System.Drawing.Point(250, 216);
            this._lblArbeitspreisWert.Name = "_lblArbeitspreisWert";
            this._lblArbeitspreisWert.Size = new System.Drawing.Size(92, 21);
            this._lblArbeitspreisWert.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            //
            // _lblEinheitArbeitspreis
            //
            this._lblEinheitArbeitspreis.AutoSize = true;
            this._lblEinheitArbeitspreis.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitArbeitspreis.Location = new System.Drawing.Point(350, 216);
            this._lblEinheitArbeitspreis.Name = "_lblEinheitArbeitspreis";
            this._lblEinheitArbeitspreis.Text = "ct/kWh";
            //
            // _lblRest
            //
            this._lblRest.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular);
            this._lblRest.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this._lblRest.Location = new System.Drawing.Point(14, 241);
            this._lblRest.Name = "_lblRest";
            this._lblRest.Size = new System.Drawing.Size(520, 20);
            //
            // _btnInArbeitspreis
            //
            this._btnInArbeitspreis.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._btnInArbeitspreis.Location = new System.Drawing.Point(14, 266);
            this._btnInArbeitspreis.Name = "_btnInArbeitspreis";
            this._btnInArbeitspreis.Size = new System.Drawing.Size(240, 25);
            this._btnInArbeitspreis.Text = "In Arbeitspreis uebernehmen";
            this._btnInArbeitspreis.Click += new System.EventHandler(this.btnInArbeitspreis_Click);
            //
            // _lblQuelle
            //
            this._lblQuelle.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            this._lblQuelle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this._lblQuelle.Location = new System.Drawing.Point(262, 270);
            this._lblQuelle.Name = "_lblQuelle";
            this._lblQuelle.Size = new System.Drawing.Size(272, 18);
            //
            // ucBrennstoffBestandteile
            //
            this.Controls.Add(this._gbBestandteile);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.Name = "ucBrennstoffBestandteile";
            this.Size = new System.Drawing.Size(548, 304);
            this._gbBestandteile.ResumeLayout(false);
            this._gbBestandteile.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox _gbBestandteile;
        private System.Windows.Forms.RadioButton _rbAufgeschluesselt;
        private System.Windows.Forms.RadioButton _rbGesamtwert;
        private System.Windows.Forms.CheckBox _chkEnergiesteuer;
        private System.Windows.Forms.TextBox _tbEnergiesteuer;
        private System.Windows.Forms.Label _lblEinheitEnergiesteuer;
        private System.Windows.Forms.Label _lblSchnellwahl;
        private System.Windows.Forms.Button _btnSatzRegel;
        private System.Windows.Forms.Button _btnSatz53a;
        private System.Windows.Forms.Button _btnSatz54;
        private System.Windows.Forms.CheckBox _chkCo2;
        private System.Windows.Forms.TextBox _tbCo2;
        private System.Windows.Forms.Label _lblEinheitCo2;
        private System.Windows.Forms.Button _btnCo2;
        private System.Windows.Forms.CheckBox _chkNetzentgelt;
        private System.Windows.Forms.TextBox _tbNetzentgelt;
        private System.Windows.Forms.Label _lblEinheitNetzentgelt;
        private System.Windows.Forms.CheckBox _chkVertrieb;
        private System.Windows.Forms.TextBox _tbVertrieb;
        private System.Windows.Forms.Label _lblEinheitVertrieb;
        private System.Windows.Forms.Label _lblSumme;
        private System.Windows.Forms.Label _lblArbeitspreis;
        private System.Windows.Forms.Label _lblArbeitspreisWert;
        private System.Windows.Forms.Label _lblEinheitArbeitspreis;
        private System.Windows.Forms.Label _lblRest;
        private System.Windows.Forms.Button _btnInArbeitspreis;
        private System.Windows.Forms.Label _lblQuelle;
    }
}
