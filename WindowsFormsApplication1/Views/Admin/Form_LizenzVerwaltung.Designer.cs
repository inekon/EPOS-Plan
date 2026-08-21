namespace WindowsFormsApplication1
{
    partial class Form_LizenzVerwaltung
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
            this._statusBox = new System.Windows.Forms.GroupBox();
            this._statusWert = new System.Windows.Forms.Label();
            this._detailWert = new System.Windows.Forms.Label();
            this._portal = new System.Windows.Forms.LinkLabel();
            this._aktivBox = new System.Windows.Forms.GroupBox();
            this._schluesselLabel = new System.Windows.Forms.Label();
            this._schluessel = new System.Windows.Forms.TextBox();
            this._licLaden = new System.Windows.Forms.Button();
            this._emailLabel = new System.Windows.Forms.Label();
            this._email = new System.Windows.Forms.TextBox();
            this._aktivieren = new System.Windows.Forms.Button();
            this._aktivHinweis = new System.Windows.Forms.Label();
            this._aktionenBox = new System.Windows.Forms.GroupBox();
            this._trial = new System.Windows.Forms.Button();
            this._freigeben = new System.Windows.Forms.Button();
            this._hinweis = new System.Windows.Forms.Label();
            this._schliessen = new System.Windows.Forms.Button();
            this._statusBox.SuspendLayout();
            this._aktivBox.SuspendLayout();
            this._aktionenBox.SuspendLayout();
            this.SuspendLayout();
            //
            // _statusBox
            //
            this._statusBox.Controls.Add(this._statusWert);
            this._statusBox.Controls.Add(this._detailWert);
            this._statusBox.Controls.Add(this._portal);
            this._statusBox.Location = new System.Drawing.Point(16, 12);
            this._statusBox.Name = "_statusBox";
            this._statusBox.Size = new System.Drawing.Size(528, 132);
            this._statusBox.Text = "Lizenzstatus auf diesem Arbeitsplatz";
            //
            // _statusWert
            //
            this._statusWert.AutoSize = false;
            this._statusWert.Font = new System.Drawing.Font("Segoe UI Semibold", 9.5F, System.Drawing.FontStyle.Bold);
            this._statusWert.Location = new System.Drawing.Point(12, 22);
            this._statusWert.Name = "_statusWert";
            this._statusWert.Size = new System.Drawing.Size(504, 40);
            this._statusWert.Text = "Nicht aktiviert — Testversion oder Lizenzschlüssel unter Administration → Lizenz.";
            //
            // _detailWert
            //
            this._detailWert.AutoSize = false;
            this._detailWert.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(96)))), ((int)(((byte)(102)))));
            this._detailWert.Location = new System.Drawing.Point(12, 64);
            this._detailWert.Name = "_detailWert";
            this._detailWert.Size = new System.Drawing.Size(504, 38);
            this._detailWert.Text = "Lizenz {0} · {1}\r\nBenutzer: {2} · Gerät: {3}";
            //
            // _portal
            //
            this._portal.AutoSize = true;
            this._portal.Location = new System.Drawing.Point(12, 105);
            this._portal.Name = "_portal";
            this._portal.Text = "Lizenzportal öffnen (Benutzer und Geräte verwalten, Schlüssel neu erzeugen)";
            this._portal.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.Portal_LinkClicked);
            //
            // _aktivBox
            //
            this._aktivBox.Controls.Add(this._schluesselLabel);
            this._aktivBox.Controls.Add(this._schluessel);
            this._aktivBox.Controls.Add(this._licLaden);
            this._aktivBox.Controls.Add(this._emailLabel);
            this._aktivBox.Controls.Add(this._email);
            this._aktivBox.Controls.Add(this._aktivieren);
            this._aktivBox.Controls.Add(this._aktivHinweis);
            this._aktivBox.Location = new System.Drawing.Point(16, 156);
            this._aktivBox.Name = "_aktivBox";
            this._aktivBox.Size = new System.Drawing.Size(528, 176);
            this._aktivBox.Text = "Aktivieren";
            //
            // _schluesselLabel
            //
            this._schluesselLabel.AutoSize = true;
            this._schluesselLabel.Location = new System.Drawing.Point(12, 28);
            this._schluesselLabel.Name = "_schluesselLabel";
            this._schluesselLabel.Text = "Lizenzschlüssel:";
            //
            // _schluessel
            //
            this._schluessel.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this._schluessel.Location = new System.Drawing.Point(130, 25);
            this._schluessel.Name = "_schluessel";
            this._schluessel.Size = new System.Drawing.Size(260, 24);
            //
            // _licLaden
            //
            this._licLaden.Location = new System.Drawing.Point(398, 22);
            this._licLaden.Name = "_licLaden";
            this._licLaden.Size = new System.Drawing.Size(118, 30);
            this._licLaden.Text = "Lizenzdatei (.lic)…";
            this._licLaden.Click += new System.EventHandler(this.LicLaden_Click);
            //
            // _emailLabel
            //
            this._emailLabel.AutoSize = true;
            this._emailLabel.Location = new System.Drawing.Point(12, 62);
            this._emailLabel.Name = "_emailLabel";
            this._emailLabel.Text = "E-Mail (Benutzer):";
            //
            // _email
            //
            this._email.Location = new System.Drawing.Point(130, 59);
            this._email.Name = "_email";
            this._email.Size = new System.Drawing.Size(260, 24);
            //
            // _aktivieren
            //
            this._aktivieren.Location = new System.Drawing.Point(130, 96);
            this._aktivieren.Name = "_aktivieren";
            this._aktivieren.Size = new System.Drawing.Size(140, 30);
            this._aktivieren.Text = "Jetzt aktivieren";
            this._aktivieren.Click += new System.EventHandler(this.Aktivieren_Click);
            //
            // _aktivHinweis
            //
            this._aktivHinweis.AutoSize = false;
            this._aktivHinweis.Font = new System.Drawing.Font("Segoe UI", 8F, System.Drawing.FontStyle.Regular);
            this._aktivHinweis.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(126)))), ((int)(((byte)(132)))));
            this._aktivHinweis.Location = new System.Drawing.Point(12, 132);
            this._aktivHinweis.Name = "_aktivHinweis";
            this._aktivHinweis.Size = new System.Drawing.Size(504, 34);
            this._aktivHinweis.Text = "Die Aktivierung benötigt einmalig eine Internetverbindung. Übertragen werden nur\r\n" +
    "Lizenzschlüssel, E-Mail und ein anonymer Geräte-Hash — keine Projekt- oder Kunden" +
    "daten.";
            //
            // _aktionenBox
            //
            this._aktionenBox.Controls.Add(this._trial);
            this._aktionenBox.Controls.Add(this._freigeben);
            this._aktionenBox.Location = new System.Drawing.Point(16, 338);
            this._aktionenBox.Name = "_aktionenBox";
            this._aktionenBox.Size = new System.Drawing.Size(528, 76);
            this._aktionenBox.Text = "Weitere Aktionen";
            //
            // _trial
            //
            this._trial.Location = new System.Drawing.Point(12, 28);
            this._trial.Name = "_trial";
            this._trial.Size = new System.Drawing.Size(170, 30);
            this._trial.Text = "Testversion anfordern…";
            this._trial.Click += new System.EventHandler(this.Trial_Click);
            //
            // _freigeben
            //
            this._freigeben.Location = new System.Drawing.Point(196, 28);
            this._freigeben.Name = "_freigeben";
            this._freigeben.Size = new System.Drawing.Size(190, 30);
            this._freigeben.Text = "Gerät von der Lizenz lösen";
            this._freigeben.Click += new System.EventHandler(this.Freigeben_Click);
            //
            // _hinweis
            //
            this._hinweis.AutoSize = false;
            this._hinweis.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(96)))), ((int)(((byte)(102)))));
            this._hinweis.Location = new System.Drawing.Point(16, 422);
            this._hinweis.Name = "_hinweis";
            this._hinweis.Size = new System.Drawing.Size(412, 52);
            //
            // _schliessen
            //
            this._schliessen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._schliessen.Location = new System.Drawing.Point(434, 444);
            this._schliessen.Name = "_schliessen";
            this._schliessen.Size = new System.Drawing.Size(110, 30);
            this._schliessen.Text = "Schließen";
            //
            // Form_LizenzVerwaltung
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this._schliessen;
            this.ClientSize = new System.Drawing.Size(560, 486);
            this.Controls.Add(this._statusBox);
            this.Controls.Add(this._aktivBox);
            this.Controls.Add(this._aktionenBox);
            this.Controls.Add(this._hinweis);
            this.Controls.Add(this._schliessen);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_LizenzVerwaltung";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Lizenz — EPOS-Plan";
            this._statusBox.ResumeLayout(false);
            this._statusBox.PerformLayout();
            this._aktivBox.ResumeLayout(false);
            this._aktivBox.PerformLayout();
            this._aktionenBox.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox _statusBox;
        private System.Windows.Forms.Label _statusWert;
        private System.Windows.Forms.Label _detailWert;
        private System.Windows.Forms.LinkLabel _portal;
        private System.Windows.Forms.GroupBox _aktivBox;
        private System.Windows.Forms.Label _schluesselLabel;
        private System.Windows.Forms.TextBox _schluessel;
        private System.Windows.Forms.Button _licLaden;
        private System.Windows.Forms.Label _emailLabel;
        private System.Windows.Forms.TextBox _email;
        private System.Windows.Forms.Button _aktivieren;
        private System.Windows.Forms.Label _aktivHinweis;
        private System.Windows.Forms.GroupBox _aktionenBox;
        private System.Windows.Forms.Button _trial;
        private System.Windows.Forms.Button _freigeben;
        private System.Windows.Forms.Label _hinweis;
        private System.Windows.Forms.Button _schliessen;
    }
}
