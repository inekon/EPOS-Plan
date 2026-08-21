namespace WindowsFormsApplication1
{
    partial class Form_Betriebsmodus
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
            this._lblKopf = new System.Windows.Forms.Label();
            this._rbLaufzeit = new System.Windows.Forms.RadioButton();
            this._lblLaufzeit = new System.Windows.Forms.Label();
            this._rbLeistung = new System.Windows.Forms.RadioButton();
            this._lblLeistung = new System.Windows.Forms.Label();
            this._rbPV = new System.Windows.Forms.RadioButton();
            this._lblPV = new System.Windows.Forms.Label();
            this._btnOk = new System.Windows.Forms.Button();
            this._btnAbbrechen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // _lblKopf
            //
            this._lblKopf.AutoSize = true;
            this._lblKopf.Location = new System.Drawing.Point(14, 14);
            this._lblKopf.Name = "_lblKopf";
            this._lblKopf.Text = "Leistungssteuerung der Wärmepumpe:";
            //
            // _rbLaufzeit
            //
            this._rbLaufzeit.AutoSize = true;
            this._rbLaufzeit.Location = new System.Drawing.Point(24, 48);
            this._rbLaufzeit.Name = "_rbLaufzeit";
            this._rbLaufzeit.Text = "Laufzeitoptimiert - maximale Leistung";
            //
            // _lblLaufzeit
            //
            this._lblLaufzeit.AutoSize = false;
            this._lblLaufzeit.Location = new System.Drawing.Point(46, 70);
            this._lblLaufzeit.Name = "_lblLaufzeit";
            this._lblLaufzeit.Size = new System.Drawing.Size(460, 34);
            this._lblLaufzeit.Text = "Die Wärmepumpe fährt volle Leistung; die über den Bedarf hinaus\r\nerzeugte Wärme l" +
    "ädt den Pufferspeicher. Lange Laufzeiten, wenig Takten.";
            //
            // _rbLeistung
            //
            this._rbLeistung.AutoSize = true;
            this._rbLeistung.Location = new System.Drawing.Point(24, 112);
            this._rbLeistung.Name = "_rbLeistung";
            this._rbLeistung.Text = "Leistungsoptimiert - nur den Bedarf decken";
            //
            // _lblLeistung
            //
            this._lblLeistung.AutoSize = false;
            this._lblLeistung.Location = new System.Drawing.Point(46, 134);
            this._lblLeistung.Name = "_lblLeistung";
            this._lblLeistung.Size = new System.Drawing.Size(460, 34);
            this._lblLeistung.Text = "Die Wärmepumpe moduliert exakt auf den Wärmebedarf und erzeugt\r\nkeinen Überschuss." +
    " Der Speicher wird nicht gezielt beladen.";
            //
            // _rbPV
            //
            this._rbPV.AutoSize = true;
            this._rbPV.Location = new System.Drawing.Point(24, 176);
            this._rbPV.Name = "_rbPV";
            this._rbPV.Text = "PV-optimiert - Überschuss nur mit PV-Strom";
            //
            // _lblPV
            //
            this._lblPV.AutoSize = false;
            this._lblPV.Location = new System.Drawing.Point(46, 198);
            this._lblPV.Name = "_lblPV";
            this._lblPV.Size = new System.Drawing.Size(460, 48);
            this._lblPV.Text = "Bei verfügbarem PV-Strom fährt die Wärmepumpe erhöhte Leistung\r\n(begrenzt auf den " +
    "PV-Überschuss) und lädt den Speicher; sonst\r\narbeitet sie leistungsoptimiert.";
            //
            // _btnOk
            //
            this._btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._btnOk.Location = new System.Drawing.Point(278, 258);
            this._btnOk.Name = "_btnOk";
            this._btnOk.Size = new System.Drawing.Size(110, 30);
            this._btnOk.Text = "OK";
            //
            // _btnAbbrechen
            //
            this._btnAbbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._btnAbbrechen.Location = new System.Drawing.Point(398, 258);
            this._btnAbbrechen.Name = "_btnAbbrechen";
            this._btnAbbrechen.Size = new System.Drawing.Size(110, 30);
            this._btnAbbrechen.Text = "Abbrechen";
            //
            // Form_Betriebsmodus
            //
            this.AcceptButton = this._btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this._btnAbbrechen;
            this.ClientSize = new System.Drawing.Size(520, 300);
            this.Controls.Add(this._lblKopf);
            this.Controls.Add(this._rbLaufzeit);
            this.Controls.Add(this._lblLaufzeit);
            this.Controls.Add(this._rbLeistung);
            this.Controls.Add(this._lblLeistung);
            this.Controls.Add(this._rbPV);
            this.Controls.Add(this._lblPV);
            this.Controls.Add(this._btnOk);
            this.Controls.Add(this._btnAbbrechen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_Betriebsmodus";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Betriebsmodus - {0}";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _lblKopf;
        private System.Windows.Forms.RadioButton _rbLaufzeit;
        private System.Windows.Forms.Label _lblLaufzeit;
        private System.Windows.Forms.RadioButton _rbLeistung;
        private System.Windows.Forms.Label _lblLeistung;
        private System.Windows.Forms.RadioButton _rbPV;
        private System.Windows.Forms.Label _lblPV;
        private System.Windows.Forms.Button _btnOk;
        private System.Windows.Forms.Button _btnAbbrechen;
    }
}
