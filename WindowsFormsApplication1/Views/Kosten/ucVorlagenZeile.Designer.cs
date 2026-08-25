namespace WindowsFormsApplication1
{
    partial class ucVorlagenZeile
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.btnEditor = new System.Windows.Forms.Button();
            this.btnLoeschen = new System.Windows.Forms.Button();
            this.txtBezeichnung = new System.Windows.Forms.TextBox();
            this.cmbBemessung = new System.Windows.Forms.ComboBox();
            this.txtSatz = new System.Windows.Forms.TextBox();
            this.lblEinheit = new System.Windows.Forms.Label();
            this.lblKette = new System.Windows.Forms.Label();
            this.txtBetrag = new System.Windows.Forms.TextBox();
            this.txtNutzung = new System.Windows.Forms.TextBox();
            this.btnWorstBest = new System.Windows.Forms.Button();
            this.tip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // btnEditor
            //
            this.btnEditor.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnEditor.FlatAppearance.BorderSize = 0;
            this.btnEditor.Location = new System.Drawing.Point(4, 4);
            this.btnEditor.Name = "btnEditor";
            this.btnEditor.Size = new System.Drawing.Size(28, 26);
            this.btnEditor.TabIndex = 0;
            this.btnEditor.Text = "✏️";
            this.btnEditor.UseVisualStyleBackColor = true;
            this.btnEditor.Click += new System.EventHandler(this.btnEditor_Click);
            //
            // btnLoeschen
            //
            this.btnLoeschen.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnLoeschen.FlatAppearance.BorderSize = 0;
            this.btnLoeschen.Location = new System.Drawing.Point(34, 4);
            this.btnLoeschen.Name = "btnLoeschen";
            this.btnLoeschen.Size = new System.Drawing.Size(28, 26);
            this.btnLoeschen.TabIndex = 1;
            this.btnLoeschen.Text = "🗑️";
            this.btnLoeschen.UseVisualStyleBackColor = true;
            this.btnLoeschen.Click += new System.EventHandler(this.btnLoeschen_Click);
            //
            // txtBezeichnung
            //
            this.txtBezeichnung.Location = new System.Drawing.Point(72, 6);
            this.txtBezeichnung.Name = "txtBezeichnung";
            this.txtBezeichnung.Size = new System.Drawing.Size(252, 23);
            this.txtBezeichnung.TabIndex = 2;
            this.txtBezeichnung.Leave += new System.EventHandler(this.Feld_Leave);
            //
            // cmbBemessung
            //
            this.cmbBemessung.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbBemessung.Location = new System.Drawing.Point(334, 6);
            this.cmbBemessung.Name = "cmbBemessung";
            this.cmbBemessung.Size = new System.Drawing.Size(172, 23);
            this.cmbBemessung.TabIndex = 3;
            this.cmbBemessung.SelectedIndexChanged += new System.EventHandler(this.cmbBemessung_SelectedIndexChanged);
            //
            // txtSatz
            //
            this.txtSatz.Location = new System.Drawing.Point(516, 6);
            this.txtSatz.Name = "txtSatz";
            this.txtSatz.Size = new System.Drawing.Size(88, 23);
            this.txtSatz.TabIndex = 4;
            this.txtSatz.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtSatz.TextChanged += new System.EventHandler(this.Zahl_TextChanged);
            this.txtSatz.Leave += new System.EventHandler(this.Feld_Leave);
            //
            // lblEinheit
            //
            this.lblEinheit.Location = new System.Drawing.Point(606, 9);
            this.lblEinheit.Name = "lblEinheit";
            this.lblEinheit.Size = new System.Drawing.Size(50, 17);
            this.lblEinheit.TabIndex = 5;
            this.lblEinheit.Text = "€/kW";
            //
            // lblKette
            //
            this.lblKette.Location = new System.Drawing.Point(658, 9);
            this.lblKette.Name = "lblKette";
            this.lblKette.Size = new System.Drawing.Size(24, 17);
            this.lblKette.TabIndex = 6;
            this.lblKette.Text = "🔗";
            this.lblKette.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // txtBetrag
            //
            this.txtBetrag.Location = new System.Drawing.Point(686, 6);
            this.txtBetrag.Name = "txtBetrag";
            this.txtBetrag.Size = new System.Drawing.Size(108, 23);
            this.txtBetrag.TabIndex = 7;
            this.txtBetrag.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtBetrag.TextChanged += new System.EventHandler(this.Zahl_TextChanged);
            this.txtBetrag.Leave += new System.EventHandler(this.Feld_Leave);
            //
            // txtNutzung
            //
            this.txtNutzung.Location = new System.Drawing.Point(802, 6);
            this.txtNutzung.Name = "txtNutzung";
            this.txtNutzung.Size = new System.Drawing.Size(60, 23);
            this.txtNutzung.TabIndex = 8;
            this.txtNutzung.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtNutzung.TextChanged += new System.EventHandler(this.Zahl_TextChanged);
            this.txtNutzung.Leave += new System.EventHandler(this.Feld_Leave);
            //
            // btnWorstBest
            //
            this.btnWorstBest.Enabled = false;
            this.btnWorstBest.Location = new System.Drawing.Point(872, 5);
            this.btnWorstBest.Name = "btnWorstBest";
            this.btnWorstBest.Size = new System.Drawing.Size(48, 25);
            this.btnWorstBest.TabIndex = 9;
            this.btnWorstBest.Text = "+/-";
            this.btnWorstBest.UseVisualStyleBackColor = true;
            //
            // ucVorlagenZeile
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.btnEditor);
            this.Controls.Add(this.btnLoeschen);
            this.Controls.Add(this.txtBezeichnung);
            this.Controls.Add(this.cmbBemessung);
            this.Controls.Add(this.txtSatz);
            this.Controls.Add(this.lblEinheit);
            this.Controls.Add(this.lblKette);
            this.Controls.Add(this.txtBetrag);
            this.Controls.Add(this.txtNutzung);
            this.Controls.Add(this.btnWorstBest);
            this.Name = "ucVorlagenZeile";
            this.Size = new System.Drawing.Size(928, 34);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Button btnEditor;
        private System.Windows.Forms.Button btnLoeschen;
        private System.Windows.Forms.TextBox txtBezeichnung;
        private System.Windows.Forms.ComboBox cmbBemessung;
        private System.Windows.Forms.TextBox txtSatz;
        private System.Windows.Forms.Label lblEinheit;
        private System.Windows.Forms.Label lblKette;
        private System.Windows.Forms.TextBox txtBetrag;
        private System.Windows.Forms.TextBox txtNutzung;
        private System.Windows.Forms.Button btnWorstBest;
        private System.Windows.Forms.ToolTip tip;
    }
}
