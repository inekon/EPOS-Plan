namespace WindowsFormsApplication1
{
    partial class ucStromAufschlaege
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
            this._gbAufschlag = new System.Windows.Forms.GroupBox();
            this._rbAufgeschluesselt = new System.Windows.Forms.RadioButton();
            this._rbGesamtwert = new System.Windows.Forms.RadioButton();
            this._chkNetzentgelt = new System.Windows.Forms.CheckBox();
            this._tbNetzentgelt = new System.Windows.Forms.TextBox();
            this._lblEinheitNetzentgelt = new System.Windows.Forms.Label();
            this._chkUmlagen = new System.Windows.Forms.CheckBox();
            this._tbUmlagen = new System.Windows.Forms.TextBox();
            this._lblEinheitUmlagen = new System.Windows.Forms.Label();
            this._chkStromsteuer = new System.Windows.Forms.CheckBox();
            this._tbStromsteuer = new System.Windows.Forms.TextBox();
            this._lblEinheitStromsteuer = new System.Windows.Forms.Label();
            this._chkKonzession = new System.Windows.Forms.CheckBox();
            this._tbKonzession = new System.Windows.Forms.TextBox();
            this._lblEinheitKonzession = new System.Windows.Forms.Label();
            this._chkVertrieb = new System.Windows.Forms.CheckBox();
            this._tbVertrieb = new System.Windows.Forms.TextBox();
            this._lblEinheitVertrieb = new System.Windows.Forms.Label();
            this._btnStromsteuerRegelfall = new System.Windows.Forms.Button();
            this._btnStromsteuerReduziert = new System.Windows.Forms.Button();
            this._lblSumme = new System.Windows.Forms.Label();
            this._lblGesamtaufschlag = new System.Windows.Forms.Label();
            this._tbOverride = new System.Windows.Forms.TextBox();
            this._lblEinheitOverride = new System.Windows.Forms.Label();
            this._lblRest = new System.Windows.Forms.Label();
            this._gbVerguetung = new System.Windows.Forms.GroupBox();
            this._lblVerguetungPv = new System.Windows.Forms.Label();
            this._tbVerguetungPv = new System.Windows.Forms.TextBox();
            this._lblEinheitVerguetungPv = new System.Windows.Forms.Label();
            this._lblVerguetungBhkw = new System.Windows.Forms.Label();
            this._tbVerguetungBhkw = new System.Windows.Forms.TextBox();
            this._lblEinheitVerguetungBhkw = new System.Windows.Forms.Label();
            this._gbAufschlag.SuspendLayout();
            this._gbVerguetung.SuspendLayout();
            this.SuspendLayout();
            //
            // _gbAufschlag
            //
            this._gbAufschlag.Controls.Add(this._rbAufgeschluesselt);
            this._gbAufschlag.Controls.Add(this._rbGesamtwert);
            this._gbAufschlag.Controls.Add(this._chkNetzentgelt);
            this._gbAufschlag.Controls.Add(this._tbNetzentgelt);
            this._gbAufschlag.Controls.Add(this._lblEinheitNetzentgelt);
            this._gbAufschlag.Controls.Add(this._chkUmlagen);
            this._gbAufschlag.Controls.Add(this._tbUmlagen);
            this._gbAufschlag.Controls.Add(this._lblEinheitUmlagen);
            this._gbAufschlag.Controls.Add(this._chkStromsteuer);
            this._gbAufschlag.Controls.Add(this._tbStromsteuer);
            this._gbAufschlag.Controls.Add(this._lblEinheitStromsteuer);
            this._gbAufschlag.Controls.Add(this._chkKonzession);
            this._gbAufschlag.Controls.Add(this._tbKonzession);
            this._gbAufschlag.Controls.Add(this._lblEinheitKonzession);
            this._gbAufschlag.Controls.Add(this._chkVertrieb);
            this._gbAufschlag.Controls.Add(this._tbVertrieb);
            this._gbAufschlag.Controls.Add(this._lblEinheitVertrieb);
            this._gbAufschlag.Controls.Add(this._btnStromsteuerRegelfall);
            this._gbAufschlag.Controls.Add(this._btnStromsteuerReduziert);
            this._gbAufschlag.Controls.Add(this._lblSumme);
            this._gbAufschlag.Controls.Add(this._lblGesamtaufschlag);
            this._gbAufschlag.Controls.Add(this._tbOverride);
            this._gbAufschlag.Controls.Add(this._lblEinheitOverride);
            this._gbAufschlag.Controls.Add(this._lblRest);
            this._gbAufschlag.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._gbAufschlag.Location = new System.Drawing.Point(0, 0);
            this._gbAufschlag.Name = "_gbAufschlag";
            this._gbAufschlag.Size = new System.Drawing.Size(548, 270);
            this._gbAufschlag.Text = "Aufschlaege auf den Strombezugspreis";
            //
            // _rbAufgeschluesselt
            //
            this._rbAufgeschluesselt.AutoSize = true;
            this._rbAufgeschluesselt.Checked = true;
            this._rbAufgeschluesselt.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._rbAufgeschluesselt.Location = new System.Drawing.Point(14, 22);
            this._rbAufgeschluesselt.Name = "_rbAufgeschluesselt";
            this._rbAufgeschluesselt.Text = "aufgeschluesselt";
            this._rbAufgeschluesselt.CheckedChanged += new System.EventHandler(this.rbAufgeschluesselt_CheckedChanged);
            //
            // _rbGesamtwert
            //
            this._rbGesamtwert.AutoSize = true;
            this._rbGesamtwert.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._rbGesamtwert.Location = new System.Drawing.Point(224, 22);
            this._rbGesamtwert.Name = "_rbGesamtwert";
            this._rbGesamtwert.Text = "Gesamtwert (Override)";
            //
            // _chkNetzentgelt
            //
            this._chkNetzentgelt.Checked = true;
            this._chkNetzentgelt.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._chkNetzentgelt.Location = new System.Drawing.Point(14, 50);
            this._chkNetzentgelt.Name = "_chkNetzentgelt";
            this._chkNetzentgelt.Size = new System.Drawing.Size(228, 21);
            this._chkNetzentgelt.Text = "Netzentgelt Arbeit";
            this._chkNetzentgelt.CheckedChanged += new System.EventHandler(this.KomponenteSchalter_CheckedChanged);
            //
            // _tbNetzentgelt
            //
            this._tbNetzentgelt.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbNetzentgelt.Location = new System.Drawing.Point(250, 48);
            this._tbNetzentgelt.Name = "_tbNetzentgelt";
            this._tbNetzentgelt.Size = new System.Drawing.Size(92, 23);
            this._tbNetzentgelt.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbNetzentgelt.TextChanged += new System.EventHandler(this.Zahlenfeld_TextChanged);
            //
            // _lblEinheitNetzentgelt
            //
            this._lblEinheitNetzentgelt.AutoSize = true;
            this._lblEinheitNetzentgelt.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitNetzentgelt.Location = new System.Drawing.Point(350, 51);
            this._lblEinheitNetzentgelt.Name = "_lblEinheitNetzentgelt";
            this._lblEinheitNetzentgelt.Text = "ct/kWh";
            //
            // _chkUmlagen
            //
            this._chkUmlagen.Checked = true;
            this._chkUmlagen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._chkUmlagen.Location = new System.Drawing.Point(14, 77);
            this._chkUmlagen.Name = "_chkUmlagen";
            this._chkUmlagen.Size = new System.Drawing.Size(228, 21);
            this._chkUmlagen.Text = "Umlagen (Summe)";
            this._chkUmlagen.CheckedChanged += new System.EventHandler(this.KomponenteSchalter_CheckedChanged);
            //
            // _tbUmlagen
            //
            this._tbUmlagen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbUmlagen.Location = new System.Drawing.Point(250, 75);
            this._tbUmlagen.Name = "_tbUmlagen";
            this._tbUmlagen.Size = new System.Drawing.Size(92, 23);
            this._tbUmlagen.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbUmlagen.TextChanged += new System.EventHandler(this.Zahlenfeld_TextChanged);
            //
            // _lblEinheitUmlagen
            //
            this._lblEinheitUmlagen.AutoSize = true;
            this._lblEinheitUmlagen.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitUmlagen.Location = new System.Drawing.Point(350, 78);
            this._lblEinheitUmlagen.Name = "_lblEinheitUmlagen";
            this._lblEinheitUmlagen.Text = "ct/kWh";
            //
            // _chkStromsteuer
            //
            this._chkStromsteuer.Checked = true;
            this._chkStromsteuer.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._chkStromsteuer.Location = new System.Drawing.Point(14, 104);
            this._chkStromsteuer.Name = "_chkStromsteuer";
            this._chkStromsteuer.Size = new System.Drawing.Size(228, 21);
            this._chkStromsteuer.Text = "Stromsteuer";
            this._chkStromsteuer.CheckedChanged += new System.EventHandler(this.KomponenteSchalter_CheckedChanged);
            //
            // _tbStromsteuer
            //
            this._tbStromsteuer.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbStromsteuer.Location = new System.Drawing.Point(250, 102);
            this._tbStromsteuer.Name = "_tbStromsteuer";
            this._tbStromsteuer.Size = new System.Drawing.Size(92, 23);
            this._tbStromsteuer.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbStromsteuer.TextChanged += new System.EventHandler(this.Zahlenfeld_TextChanged);
            //
            // _lblEinheitStromsteuer
            //
            this._lblEinheitStromsteuer.AutoSize = true;
            this._lblEinheitStromsteuer.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitStromsteuer.Location = new System.Drawing.Point(350, 105);
            this._lblEinheitStromsteuer.Name = "_lblEinheitStromsteuer";
            this._lblEinheitStromsteuer.Text = "ct/kWh";
            //
            // _chkKonzession
            //
            this._chkKonzession.Checked = true;
            this._chkKonzession.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._chkKonzession.Location = new System.Drawing.Point(14, 131);
            this._chkKonzession.Name = "_chkKonzession";
            this._chkKonzession.Size = new System.Drawing.Size(228, 21);
            this._chkKonzession.Text = "Konzessionsabgabe";
            this._chkKonzession.CheckedChanged += new System.EventHandler(this.KomponenteSchalter_CheckedChanged);
            //
            // _tbKonzession
            //
            this._tbKonzession.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbKonzession.Location = new System.Drawing.Point(250, 129);
            this._tbKonzession.Name = "_tbKonzession";
            this._tbKonzession.Size = new System.Drawing.Size(92, 23);
            this._tbKonzession.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbKonzession.TextChanged += new System.EventHandler(this.Zahlenfeld_TextChanged);
            //
            // _lblEinheitKonzession
            //
            this._lblEinheitKonzession.AutoSize = true;
            this._lblEinheitKonzession.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitKonzession.Location = new System.Drawing.Point(350, 132);
            this._lblEinheitKonzession.Name = "_lblEinheitKonzession";
            this._lblEinheitKonzession.Text = "ct/kWh";
            //
            // _chkVertrieb
            //
            this._chkVertrieb.Checked = true;
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
            // _btnStromsteuerRegelfall
            //
            this._btnStromsteuerRegelfall.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            this._btnStromsteuerRegelfall.Location = new System.Drawing.Point(402, 101);
            this._btnStromsteuerRegelfall.Name = "_btnStromsteuerRegelfall";
            this._btnStromsteuerRegelfall.Size = new System.Drawing.Size(62, 24);
            this._btnStromsteuerRegelfall.Text = "2,05";
            this._btnStromsteuerRegelfall.Click += new System.EventHandler(this.btnStromsteuerRegelfall_Click);
            //
            // _btnStromsteuerReduziert
            //
            this._btnStromsteuerReduziert.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            this._btnStromsteuerReduziert.Location = new System.Drawing.Point(468, 101);
            this._btnStromsteuerReduziert.Name = "_btnStromsteuerReduziert";
            this._btnStromsteuerReduziert.Size = new System.Drawing.Size(62, 24);
            this._btnStromsteuerReduziert.Text = "0,05";
            this._btnStromsteuerReduziert.Click += new System.EventHandler(this.btnStromsteuerReduziert_Click);
            //
            // _lblSumme
            //
            this._lblSumme.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this._lblSumme.Location = new System.Drawing.Point(14, 189);
            this._lblSumme.Name = "_lblSumme";
            this._lblSumme.Size = new System.Drawing.Size(520, 20);
            //
            // _lblGesamtaufschlag
            //
            this._lblGesamtaufschlag.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblGesamtaufschlag.Location = new System.Drawing.Point(14, 216);
            this._lblGesamtaufschlag.Name = "_lblGesamtaufschlag";
            this._lblGesamtaufschlag.Size = new System.Drawing.Size(228, 21);
            this._lblGesamtaufschlag.Text = "Gesamtaufschlag";
            //
            // _tbOverride
            //
            this._tbOverride.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbOverride.Location = new System.Drawing.Point(250, 213);
            this._tbOverride.Name = "_tbOverride";
            this._tbOverride.Size = new System.Drawing.Size(92, 23);
            this._tbOverride.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbOverride.TextChanged += new System.EventHandler(this.Zahlenfeld_TextChanged);
            //
            // _lblEinheitOverride
            //
            this._lblEinheitOverride.AutoSize = true;
            this._lblEinheitOverride.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitOverride.Location = new System.Drawing.Point(350, 216);
            this._lblEinheitOverride.Name = "_lblEinheitOverride";
            this._lblEinheitOverride.Text = "ct/kWh";
            //
            // _lblRest
            //
            this._lblRest.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular);
            this._lblRest.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(100)))), ((int)(((byte)(100)))), ((int)(((byte)(100)))));
            this._lblRest.Location = new System.Drawing.Point(14, 241);
            this._lblRest.Name = "_lblRest";
            this._lblRest.Size = new System.Drawing.Size(520, 20);
            //
            // _gbVerguetung
            //
            this._gbVerguetung.Controls.Add(this._lblVerguetungPv);
            this._gbVerguetung.Controls.Add(this._tbVerguetungPv);
            this._gbVerguetung.Controls.Add(this._lblEinheitVerguetungPv);
            this._gbVerguetung.Controls.Add(this._lblVerguetungBhkw);
            this._gbVerguetung.Controls.Add(this._tbVerguetungBhkw);
            this._gbVerguetung.Controls.Add(this._lblEinheitVerguetungBhkw);
            this._gbVerguetung.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._gbVerguetung.Location = new System.Drawing.Point(0, 276);
            this._gbVerguetung.Name = "_gbVerguetung";
            this._gbVerguetung.Size = new System.Drawing.Size(548, 58);
            this._gbVerguetung.Text = "Verguetung fuer eingespeisten Strom";
            //
            // _lblVerguetungPv
            //
            this._lblVerguetungPv.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblVerguetungPv.Location = new System.Drawing.Point(14, 25);
            this._lblVerguetungPv.Name = "_lblVerguetungPv";
            this._lblVerguetungPv.Size = new System.Drawing.Size(150, 21);
            this._lblVerguetungPv.Text = "Photovoltaik v_pv";
            //
            // _tbVerguetungPv
            //
            this._tbVerguetungPv.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbVerguetungPv.Location = new System.Drawing.Point(166, 22);
            this._tbVerguetungPv.Name = "_tbVerguetungPv";
            this._tbVerguetungPv.Size = new System.Drawing.Size(70, 23);
            this._tbVerguetungPv.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbVerguetungPv.TextChanged += new System.EventHandler(this.Verguetungsfeld_TextChanged);
            //
            // _lblEinheitVerguetungPv
            //
            this._lblEinheitVerguetungPv.AutoSize = true;
            this._lblEinheitVerguetungPv.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitVerguetungPv.Location = new System.Drawing.Point(240, 25);
            this._lblEinheitVerguetungPv.Name = "_lblEinheitVerguetungPv";
            this._lblEinheitVerguetungPv.Text = "ct/kWh";
            //
            // _lblVerguetungBhkw
            //
            this._lblVerguetungBhkw.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblVerguetungBhkw.Location = new System.Drawing.Point(290, 25);
            this._lblVerguetungBhkw.Name = "_lblVerguetungBhkw";
            this._lblVerguetungBhkw.Size = new System.Drawing.Size(120, 21);
            this._lblVerguetungBhkw.Text = "BHKW v_bhkw";
            //
            // _tbVerguetungBhkw
            //
            this._tbVerguetungBhkw.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._tbVerguetungBhkw.Location = new System.Drawing.Point(412, 22);
            this._tbVerguetungBhkw.Name = "_tbVerguetungBhkw";
            this._tbVerguetungBhkw.Size = new System.Drawing.Size(70, 23);
            this._tbVerguetungBhkw.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this._tbVerguetungBhkw.TextChanged += new System.EventHandler(this.Verguetungsfeld_TextChanged);
            //
            // _lblEinheitVerguetungBhkw
            //
            this._lblEinheitVerguetungBhkw.AutoSize = true;
            this._lblEinheitVerguetungBhkw.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this._lblEinheitVerguetungBhkw.Location = new System.Drawing.Point(486, 25);
            this._lblEinheitVerguetungBhkw.Name = "_lblEinheitVerguetungBhkw";
            this._lblEinheitVerguetungBhkw.Text = "ct/kWh";
            //
            // ucStromAufschlaege
            //
            this.Controls.Add(this._gbAufschlag);
            this.Controls.Add(this._gbVerguetung);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.Name = "ucStromAufschlaege";
            this.Size = new System.Drawing.Size(548, 338);
            this._gbAufschlag.ResumeLayout(false);
            this._gbAufschlag.PerformLayout();
            this._gbVerguetung.ResumeLayout(false);
            this._gbVerguetung.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox _gbAufschlag;
        private System.Windows.Forms.RadioButton _rbAufgeschluesselt;
        private System.Windows.Forms.RadioButton _rbGesamtwert;
        private System.Windows.Forms.CheckBox _chkNetzentgelt;
        private System.Windows.Forms.TextBox _tbNetzentgelt;
        private System.Windows.Forms.Label _lblEinheitNetzentgelt;
        private System.Windows.Forms.CheckBox _chkUmlagen;
        private System.Windows.Forms.TextBox _tbUmlagen;
        private System.Windows.Forms.Label _lblEinheitUmlagen;
        private System.Windows.Forms.CheckBox _chkStromsteuer;
        private System.Windows.Forms.TextBox _tbStromsteuer;
        private System.Windows.Forms.Label _lblEinheitStromsteuer;
        private System.Windows.Forms.CheckBox _chkKonzession;
        private System.Windows.Forms.TextBox _tbKonzession;
        private System.Windows.Forms.Label _lblEinheitKonzession;
        private System.Windows.Forms.CheckBox _chkVertrieb;
        private System.Windows.Forms.TextBox _tbVertrieb;
        private System.Windows.Forms.Label _lblEinheitVertrieb;
        private System.Windows.Forms.Button _btnStromsteuerRegelfall;
        private System.Windows.Forms.Button _btnStromsteuerReduziert;
        private System.Windows.Forms.Label _lblSumme;
        private System.Windows.Forms.Label _lblGesamtaufschlag;
        private System.Windows.Forms.TextBox _tbOverride;
        private System.Windows.Forms.Label _lblEinheitOverride;
        private System.Windows.Forms.Label _lblRest;
        private System.Windows.Forms.GroupBox _gbVerguetung;
        private System.Windows.Forms.Label _lblVerguetungPv;
        private System.Windows.Forms.TextBox _tbVerguetungPv;
        private System.Windows.Forms.Label _lblEinheitVerguetungPv;
        private System.Windows.Forms.Label _lblVerguetungBhkw;
        private System.Windows.Forms.TextBox _tbVerguetungBhkw;
        private System.Windows.Forms.Label _lblEinheitVerguetungBhkw;
    }
}
