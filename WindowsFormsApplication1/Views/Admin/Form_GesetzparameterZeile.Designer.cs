namespace WindowsFormsApplication1
{
    partial class Form_GesetzparameterZeile
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
            this.lblSchluessel = new System.Windows.Forms.Label();
            this.tbSchluessel = new System.Windows.Forms.TextBox();
            this.lblKlasse = new System.Windows.Forms.Label();
            this.cbKlasse = new System.Windows.Forms.ComboBox();
            this.lblJahr = new System.Windows.Forms.Label();
            this.tbJahr = new System.Windows.Forms.TextBox();
            this.lblWert = new System.Windows.Forms.Label();
            this.tbWert = new System.Windows.Forms.TextBox();
            this.lblWertLeer = new System.Windows.Forms.Label();
            this.lblEinheit = new System.Windows.Forms.Label();
            this.cbEinheit = new System.Windows.Forms.ComboBox();
            this.lblStatus = new System.Windows.Forms.Label();
            this.cbStatus = new System.Windows.Forms.ComboBox();
            this.lblQuelle = new System.Windows.Forms.Label();
            this.tbQuelle = new System.Windows.Forms.TextBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnAbbruch = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblSchluessel
            //
            this.lblSchluessel.AutoSize = true;
            this.lblSchluessel.Location = new System.Drawing.Point(12, 14);
            this.lblSchluessel.Name = "lblSchluessel";
            this.lblSchluessel.Text = "Schlüssel";
            //
            // tbSchluessel
            //
            this.tbSchluessel.CharacterCasing = System.Windows.Forms.CharacterCasing.Upper;
            this.tbSchluessel.Location = new System.Drawing.Point(160, 11);
            this.tbSchluessel.Name = "tbSchluessel";
            this.tbSchluessel.Width = 440;
            //
            // lblKlasse
            //
            this.lblKlasse.AutoSize = true;
            this.lblKlasse.Location = new System.Drawing.Point(12, 46);
            this.lblKlasse.Name = "lblKlasse";
            this.lblKlasse.Text = "Bereich";
            //
            // cbKlasse
            //
            this.cbKlasse.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbKlasse.Location = new System.Drawing.Point(160, 43);
            this.cbKlasse.Name = "cbKlasse";
            this.cbKlasse.Width = 260;
            //
            // lblJahr
            //
            this.lblJahr.AutoSize = true;
            this.lblJahr.Location = new System.Drawing.Point(12, 78);
            this.lblJahr.Name = "lblJahr";
            this.lblJahr.Text = "Gültig ab";
            //
            // tbJahr
            //
            this.tbJahr.Location = new System.Drawing.Point(160, 75);
            this.tbJahr.Name = "tbJahr";
            this.tbJahr.Width = 80;
            //
            // lblWert
            //
            this.lblWert.AutoSize = true;
            this.lblWert.Location = new System.Drawing.Point(12, 110);
            this.lblWert.Name = "lblWert";
            this.lblWert.Text = "Wert";
            //
            // tbWert
            //
            this.tbWert.Location = new System.Drawing.Point(160, 107);
            this.tbWert.Name = "tbWert";
            this.tbWert.Width = 120;
            //
            // lblWertLeer
            //
            this.lblWertLeer.AutoSize = true;
            this.lblWertLeer.ForeColor = System.Drawing.SystemColors.GrayText;
            this.lblWertLeer.Location = new System.Drawing.Point(290, 110);
            this.lblWertLeer.Name = "lblWertLeer";
            this.lblWertLeer.Text = "leer = der Satz ist entfallen (nicht 0)";
            //
            // lblEinheit
            //
            this.lblEinheit.AutoSize = true;
            this.lblEinheit.Location = new System.Drawing.Point(12, 142);
            this.lblEinheit.Name = "lblEinheit";
            this.lblEinheit.Text = "Einheit";
            //
            // cbEinheit
            //
            this.cbEinheit.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbEinheit.Location = new System.Drawing.Point(160, 139);
            this.cbEinheit.Name = "cbEinheit";
            this.cbEinheit.Width = 140;
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(330, 142);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Text = "Status";
            //
            // cbStatus
            //
            this.cbStatus.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbStatus.Location = new System.Drawing.Point(400, 139);
            this.cbStatus.Name = "cbStatus";
            this.cbStatus.Width = 200;
            //
            // lblQuelle
            //
            this.lblQuelle.AutoSize = true;
            this.lblQuelle.Location = new System.Drawing.Point(12, 174);
            this.lblQuelle.Name = "lblQuelle";
            this.lblQuelle.Text = "Quelle";
            //
            // tbQuelle
            //
            this.tbQuelle.Location = new System.Drawing.Point(160, 171);
            this.tbQuelle.MaxLength = 120;
            this.tbQuelle.Name = "tbQuelle";
            this.tbQuelle.Width = 440;
            //
            // btnOk
            //
            this.btnOk.Location = new System.Drawing.Point(370, 210);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(110, 30);
            this.btnOk.Text = "OK";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            //
            // btnAbbruch
            //
            this.btnAbbruch.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAbbruch.Location = new System.Drawing.Point(490, 210);
            this.btnAbbruch.Name = "btnAbbruch";
            this.btnAbbruch.Size = new System.Drawing.Size(110, 30);
            this.btnAbbruch.Text = "Abbrechen";
            //
            // Form_GesetzparameterZeile
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnAbbruch;
            this.ClientSize = new System.Drawing.Size(620, 260);
            this.Controls.Add(this.lblSchluessel);
            this.Controls.Add(this.tbSchluessel);
            this.Controls.Add(this.lblKlasse);
            this.Controls.Add(this.cbKlasse);
            this.Controls.Add(this.lblJahr);
            this.Controls.Add(this.tbJahr);
            this.Controls.Add(this.lblWert);
            this.Controls.Add(this.tbWert);
            this.Controls.Add(this.lblWertLeer);
            this.Controls.Add(this.lblEinheit);
            this.Controls.Add(this.cbEinheit);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.cbStatus);
            this.Controls.Add(this.lblQuelle);
            this.Controls.Add(this.tbQuelle);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnAbbruch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_GesetzparameterZeile";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Gesetzlichen Parameter ändern";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblSchluessel;
        private System.Windows.Forms.TextBox tbSchluessel;
        private System.Windows.Forms.Label lblKlasse;
        private System.Windows.Forms.ComboBox cbKlasse;
        private System.Windows.Forms.Label lblJahr;
        private System.Windows.Forms.TextBox tbJahr;
        private System.Windows.Forms.Label lblWert;
        private System.Windows.Forms.TextBox tbWert;
        private System.Windows.Forms.Label lblWertLeer;
        private System.Windows.Forms.Label lblEinheit;
        private System.Windows.Forms.ComboBox cbEinheit;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ComboBox cbStatus;
        private System.Windows.Forms.Label lblQuelle;
        private System.Windows.Forms.TextBox tbQuelle;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnAbbruch;
    }
}
