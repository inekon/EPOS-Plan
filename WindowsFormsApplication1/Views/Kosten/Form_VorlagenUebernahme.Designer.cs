namespace WindowsFormsApplication1
{
    partial class Form_VorlagenUebernahme
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
            this.lblKopfTitel = new System.Windows.Forms.Label();
            this.lblKontext = new System.Windows.Forms.Label();
            this.lblZiel = new System.Windows.Forms.Label();
            this.cmbZielProjekt = new System.Windows.Forms.ComboBox();
            this.rbQuelleVorlage = new System.Windows.Forms.RadioButton();
            this.rbQuelleProjekt = new System.Windows.Forms.RadioButton();
            this.cmbQuellProjekt = new System.Windows.Forms.ComboBox();
            this.lblVorschau = new System.Windows.Forms.Label();
            this.btnUebernehmen = new System.Windows.Forms.Button();
            this.btnAbbrechen = new System.Windows.Forms.Button();
            this.pnlKopf.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlKopf
            //
            this.pnlKopf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(31)))), ((int)(((byte)(61)))));
            this.pnlKopf.Controls.Add(this.lblKopfTitel);
            this.pnlKopf.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlKopf.Location = new System.Drawing.Point(0, 0);
            this.pnlKopf.Name = "pnlKopf";
            this.pnlKopf.Size = new System.Drawing.Size(544, 40);
            this.pnlKopf.TabIndex = 0;
            //
            // lblKopfTitel
            //
            this.lblKopfTitel.AutoSize = true;
            this.lblKopfTitel.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold);
            this.lblKopfTitel.ForeColor = System.Drawing.Color.White;
            this.lblKopfTitel.Location = new System.Drawing.Point(12, 9);
            this.lblKopfTitel.Name = "lblKopfTitel";
            this.lblKopfTitel.Size = new System.Drawing.Size(180, 20);
            this.lblKopfTitel.TabIndex = 0;
            this.lblKopfTitel.Text = "Übernahme ins Projekt";
            //
            // lblKontext
            //
            this.lblKontext.AutoSize = true;
            this.lblKontext.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblKontext.Location = new System.Drawing.Point(14, 52);
            this.lblKontext.Name = "lblKontext";
            this.lblKontext.Size = new System.Drawing.Size(160, 15);
            this.lblKontext.TabIndex = 1;
            this.lblKontext.Text = "BHKW · Betriebskosten";
            //
            // lblZiel
            //
            this.lblZiel.AutoSize = true;
            this.lblZiel.Location = new System.Drawing.Point(14, 86);
            this.lblZiel.Name = "lblZiel";
            this.lblZiel.Size = new System.Drawing.Size(70, 15);
            this.lblZiel.TabIndex = 2;
            this.lblZiel.Text = "Zielprojekt:";
            //
            // cmbZielProjekt
            //
            this.cmbZielProjekt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbZielProjekt.Location = new System.Drawing.Point(190, 83);
            this.cmbZielProjekt.Name = "cmbZielProjekt";
            this.cmbZielProjekt.Size = new System.Drawing.Size(336, 23);
            this.cmbZielProjekt.TabIndex = 3;
            this.cmbZielProjekt.SelectedIndexChanged += new System.EventHandler(this.Auswahl_Geaendert);
            //
            // rbQuelleVorlage
            //
            this.rbQuelleVorlage.AutoSize = true;
            this.rbQuelleVorlage.Checked = true;
            this.rbQuelleVorlage.Location = new System.Drawing.Point(17, 122);
            this.rbQuelleVorlage.Name = "rbQuelleVorlage";
            this.rbQuelleVorlage.Size = new System.Drawing.Size(220, 19);
            this.rbQuelleVorlage.TabIndex = 4;
            this.rbQuelleVorlage.TabStop = true;
            this.rbQuelleVorlage.Text = "Aus der aktuellen Vorlage/Variante";
            this.rbQuelleVorlage.UseVisualStyleBackColor = true;
            this.rbQuelleVorlage.CheckedChanged += new System.EventHandler(this.Auswahl_Geaendert);
            //
            // rbQuelleProjekt
            //
            this.rbQuelleProjekt.AutoSize = true;
            this.rbQuelleProjekt.Location = new System.Drawing.Point(17, 150);
            this.rbQuelleProjekt.Name = "rbQuelleProjekt";
            this.rbQuelleProjekt.Size = new System.Drawing.Size(150, 19);
            this.rbQuelleProjekt.TabIndex = 5;
            this.rbQuelleProjekt.Text = "Aus anderem Projekt:";
            this.rbQuelleProjekt.UseVisualStyleBackColor = true;
            //
            // cmbQuellProjekt
            //
            this.cmbQuellProjekt.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbQuellProjekt.Enabled = false;
            this.cmbQuellProjekt.Location = new System.Drawing.Point(190, 148);
            this.cmbQuellProjekt.Name = "cmbQuellProjekt";
            this.cmbQuellProjekt.Size = new System.Drawing.Size(336, 23);
            this.cmbQuellProjekt.TabIndex = 6;
            this.cmbQuellProjekt.SelectedIndexChanged += new System.EventHandler(this.Auswahl_Geaendert);
            //
            // lblVorschau
            //
            this.lblVorschau.BackColor = System.Drawing.Color.LemonChiffon;
            this.lblVorschau.Location = new System.Drawing.Point(14, 186);
            this.lblVorschau.Name = "lblVorschau";
            this.lblVorschau.Padding = new System.Windows.Forms.Padding(6);
            this.lblVorschau.Size = new System.Drawing.Size(512, 78);
            this.lblVorschau.TabIndex = 7;
            this.lblVorschau.Text = "Vorschau";
            //
            // btnUebernehmen
            //
            this.btnUebernehmen.Location = new System.Drawing.Point(320, 278);
            this.btnUebernehmen.Name = "btnUebernehmen";
            this.btnUebernehmen.Size = new System.Drawing.Size(110, 27);
            this.btnUebernehmen.TabIndex = 8;
            this.btnUebernehmen.Text = "Übernehmen";
            this.btnUebernehmen.UseVisualStyleBackColor = true;
            this.btnUebernehmen.Click += new System.EventHandler(this.btnUebernehmen_Click);
            //
            // btnAbbrechen
            //
            this.btnAbbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAbbrechen.Location = new System.Drawing.Point(438, 278);
            this.btnAbbrechen.Name = "btnAbbrechen";
            this.btnAbbrechen.Size = new System.Drawing.Size(88, 27);
            this.btnAbbrechen.TabIndex = 9;
            this.btnAbbrechen.Text = "Abbrechen";
            this.btnAbbrechen.UseVisualStyleBackColor = true;
            //
            // Form_VorlagenUebernahme
            //
            this.AcceptButton = this.btnUebernehmen;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnAbbrechen;
            this.ClientSize = new System.Drawing.Size(544, 319);
            this.Controls.Add(this.pnlKopf);
            this.Controls.Add(this.lblKontext);
            this.Controls.Add(this.lblZiel);
            this.Controls.Add(this.cmbZielProjekt);
            this.Controls.Add(this.rbQuelleVorlage);
            this.Controls.Add(this.rbQuelleProjekt);
            this.Controls.Add(this.cmbQuellProjekt);
            this.Controls.Add(this.lblVorschau);
            this.Controls.Add(this.btnUebernehmen);
            this.Controls.Add(this.btnAbbrechen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_VorlagenUebernahme";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Übernahme ins Projekt";
            this.pnlKopf.ResumeLayout(false);
            this.pnlKopf.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel pnlKopf;
        private System.Windows.Forms.Label lblKopfTitel;
        private System.Windows.Forms.Label lblKontext;
        private System.Windows.Forms.Label lblZiel;
        private System.Windows.Forms.ComboBox cmbZielProjekt;
        private System.Windows.Forms.RadioButton rbQuelleVorlage;
        private System.Windows.Forms.RadioButton rbQuelleProjekt;
        private System.Windows.Forms.ComboBox cmbQuellProjekt;
        private System.Windows.Forms.Label lblVorschau;
        private System.Windows.Forms.Button btnUebernehmen;
        private System.Windows.Forms.Button btnAbbrechen;
    }
}
