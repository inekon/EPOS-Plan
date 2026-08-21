namespace WindowsFormsApplication1
{
    partial class Form_SpotpreisImport
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
            this._lblInfo = new System.Windows.Forms.Label();
            this._lblDatei = new System.Windows.Forms.Label();
            this._tbPfad = new System.Windows.Forms.TextBox();
            this._btnWaehlen = new System.Windows.Forms.Button();
            this._lblBezeichner = new System.Windows.Forms.Label();
            this._tbBezeichner = new System.Windows.Forms.TextBox();
            this._chkStamm = new System.Windows.Forms.CheckBox();
            this._lblProtokoll = new System.Windows.Forms.Label();
            this._tbProtokoll = new System.Windows.Forms.TextBox();
            this._lblStatus = new System.Windows.Forms.Label();
            this._btnUebernehmen = new System.Windows.Forms.Button();
            this._btnSchliessen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // _lblInfo
            // 
            this._lblInfo.AutoSize = false;
            this._lblInfo.Location = new System.Drawing.Point(12, 10);
            this._lblInfo.Name = "_lblInfo";
            this._lblInfo.Size = new System.Drawing.Size(696, 46);
            this._lblInfo.Text = "Erwartet wird eine CSV-Datei mit Semikolon und Dezimalkomma: Datum; von; Zeitzone " +
    "von; bis; Zeitzone bis; Preis in ct/kWh. Zeitumstellung und Schaltjahr werden aus" +
    "gewertet und protokolliert.";
            // 
            // _lblDatei
            // 
            this._lblDatei.AutoSize = true;
            this._lblDatei.Location = new System.Drawing.Point(12, 66);
            this._lblDatei.Name = "_lblDatei";
            this._lblDatei.Text = "Datei:";
            // 
            // _tbPfad
            // 
            this._tbPfad.BackColor = System.Drawing.SystemColors.Control;
            this._tbPfad.Location = new System.Drawing.Point(110, 63);
            this._tbPfad.Name = "_tbPfad";
            this._tbPfad.ReadOnly = true;
            this._tbPfad.Width = 480;
            // 
            // _btnWaehlen
            // 
            this._btnWaehlen.Location = new System.Drawing.Point(600, 63);
            this._btnWaehlen.Name = "_btnWaehlen";
            this._btnWaehlen.Text = "Datei waehlen ...";
            this._btnWaehlen.Width = 108;
            this._btnWaehlen.Click += new System.EventHandler(this.btnWaehlen_Click);
            // 
            // _lblBezeichner
            // 
            this._lblBezeichner.AutoSize = true;
            this._lblBezeichner.Location = new System.Drawing.Point(12, 98);
            this._lblBezeichner.Name = "_lblBezeichner";
            this._lblBezeichner.Text = "Bezeichnung:";
            // 
            // _tbBezeichner
            // 
            this._tbBezeichner.Location = new System.Drawing.Point(110, 95);
            this._tbBezeichner.Name = "_tbBezeichner";
            this._tbBezeichner.Width = 300;
            // 
            // _chkStamm
            // 
            this._chkStamm.AutoSize = true;
            this._chkStamm.Checked = true;
            this._chkStamm.Location = new System.Drawing.Point(430, 96);
            this._chkStamm.Name = "_chkStamm";
            this._chkStamm.Text = "allen Projekten zur Verfuegung stellen";
            // 
            // _lblProtokoll
            // 
            this._lblProtokoll.AutoSize = true;
            this._lblProtokoll.Location = new System.Drawing.Point(12, 128);
            this._lblProtokoll.Name = "_lblProtokoll";
            this._lblProtokoll.Text = "Validierungsprotokoll:";
            // 
            // _tbProtokoll
            // 
            this._tbProtokoll.BackColor = System.Drawing.SystemColors.Window;
            this._tbProtokoll.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular);
            this._tbProtokoll.Location = new System.Drawing.Point(12, 150);
            this._tbProtokoll.Multiline = true;
            this._tbProtokoll.Name = "_tbProtokoll";
            this._tbProtokoll.ReadOnly = true;
            this._tbProtokoll.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this._tbProtokoll.Size = new System.Drawing.Size(696, 300);
            // 
            // _lblStatus
            // 
            this._lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblStatus.Location = new System.Drawing.Point(12, 458);
            this._lblStatus.Name = "_lblStatus";
            this._lblStatus.Size = new System.Drawing.Size(480, 20);
            // 
            // _btnUebernehmen
            // 
            this._btnUebernehmen.Enabled = false;
            this._btnUebernehmen.Location = new System.Drawing.Point(468, 486);
            this._btnUebernehmen.Name = "_btnUebernehmen";
            this._btnUebernehmen.Size = new System.Drawing.Size(120, 30);
            this._btnUebernehmen.Text = "Uebernehmen";
            this._btnUebernehmen.Click += new System.EventHandler(this.btnUebernehmen_Click);
            // 
            // _btnSchliessen
            // 
            this._btnSchliessen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnSchliessen.Location = new System.Drawing.Point(598, 486);
            this._btnSchliessen.Name = "_btnSchliessen";
            this._btnSchliessen.Size = new System.Drawing.Size(110, 30);
            this._btnSchliessen.Text = "Abbrechen";
            // 
            // Form_SpotpreisImport
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this._btnSchliessen;
            this.ClientSize = new System.Drawing.Size(720, 528);
            this.Controls.Add(this._lblInfo);
            this.Controls.Add(this._lblDatei);
            this.Controls.Add(this._tbPfad);
            this.Controls.Add(this._btnWaehlen);
            this.Controls.Add(this._lblBezeichner);
            this.Controls.Add(this._tbBezeichner);
            this.Controls.Add(this._chkStamm);
            this.Controls.Add(this._lblProtokoll);
            this.Controls.Add(this._tbProtokoll);
            this.Controls.Add(this._lblStatus);
            this.Controls.Add(this._btnUebernehmen);
            this.Controls.Add(this._btnSchliessen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_SpotpreisImport";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Spotmarktpreise importieren";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _lblInfo;
        private System.Windows.Forms.Label _lblDatei;
        private System.Windows.Forms.TextBox _tbPfad;
        private System.Windows.Forms.Button _btnWaehlen;
        private System.Windows.Forms.Label _lblBezeichner;
        private System.Windows.Forms.TextBox _tbBezeichner;
        private System.Windows.Forms.CheckBox _chkStamm;
        private System.Windows.Forms.Label _lblProtokoll;
        private System.Windows.Forms.TextBox _tbProtokoll;
        private System.Windows.Forms.Label _lblStatus;
        private System.Windows.Forms.Button _btnUebernehmen;
        private System.Windows.Forms.Button _btnSchliessen;
    }
}
