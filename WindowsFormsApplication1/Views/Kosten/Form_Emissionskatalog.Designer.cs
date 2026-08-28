namespace WindowsFormsApplication1
{
    partial class Form_Emissionskatalog
    {
        /// <summary>Required designer variable.</summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>Clean up any resources being used.</summary>
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
        /// Aufbau des Dialogs — HANDGESCHRIEBEN, nicht vom VS-Designer erzeugt
        /// (Hausregel: Designer-Dateien werden im Projekt nicht im Designer
        /// geöffnet; die AutoScale-Basis 7×15 / <c>AutoScaleMode.Font</c> ist von
        /// <c>Form_LeistungspreisReihe</c> übernommen, damit der Dialog auf einem
        /// 150-%-Monitor dieselbe Skalierung erfährt wie seine Nachbarn).
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlKopf = new System.Windows.Forms.Panel();
            this.lblKopfTitel = new System.Windows.Forms.Label();
            this.lblKontext = new System.Windows.Forms.Label();
            this.pnlModus = new System.Windows.Forms.Panel();
            this.lblModus = new System.Windows.Forms.Label();
            this.rbModusCo2 = new System.Windows.Forms.RadioButton();
            this.rbModusCo2e = new System.Windows.Forms.RadioButton();
            this.lblModusOrt = new System.Windows.Forms.Label();
            this.grpArten = new System.Windows.Forms.GroupBox();
            this.dgvArten = new System.Windows.Forms.DataGridView();
            this.btnArtNeu = new System.Windows.Forms.Button();
            this.btnArtBearbeiten = new System.Windows.Forms.Button();
            this.btnArtLoeschen = new System.Windows.Forms.Button();
            this.grpWerte = new System.Windows.Forms.GroupBox();
            this.dgvWerte = new System.Windows.Forms.DataGridView();
            this.btnUebernehmen = new System.Windows.Forms.Button();
            this.btnWertNeu = new System.Windows.Forms.Button();
            this.btnWertBearbeiten = new System.Windows.Forms.Button();
            this.btnWertLoeschen = new System.Windows.Forms.Button();
            this.lblHinweis = new System.Windows.Forms.Label();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnAbbrechen = new System.Windows.Forms.Button();
            this.pnlKopf.SuspendLayout();
            this.pnlModus.SuspendLayout();
            this.grpArten.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvArten)).BeginInit();
            this.grpWerte.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvWerte)).BeginInit();
            this.SuspendLayout();
            //
            // pnlKopf
            //
            this.pnlKopf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(50)))), ((int)(((byte)(97)))));
            this.pnlKopf.Controls.Add(this.lblKontext);
            this.pnlKopf.Controls.Add(this.lblKopfTitel);
            this.pnlKopf.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlKopf.Location = new System.Drawing.Point(0, 0);
            this.pnlKopf.Name = "pnlKopf";
            this.pnlKopf.Size = new System.Drawing.Size(920, 58);
            this.pnlKopf.TabIndex = 0;
            //
            // lblKopfTitel
            //
            this.lblKopfTitel.AutoSize = true;
            this.lblKopfTitel.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold);
            this.lblKopfTitel.ForeColor = System.Drawing.Color.White;
            this.lblKopfTitel.Location = new System.Drawing.Point(12, 9);
            this.lblKopfTitel.Name = "lblKopfTitel";
            this.lblKopfTitel.Size = new System.Drawing.Size(163, 20);
            this.lblKopfTitel.TabIndex = 0;
            this.lblKopfTitel.Text = "Emissionsfaktor-Katalog";
            //
            // lblKontext
            //
            this.lblKontext.AutoSize = true;
            this.lblKontext.ForeColor = System.Drawing.Color.Gainsboro;
            this.lblKontext.Location = new System.Drawing.Point(14, 34);
            this.lblKontext.Name = "lblKontext";
            this.lblKontext.Size = new System.Drawing.Size(50, 15);
            this.lblKontext.TabIndex = 1;
            this.lblKontext.Text = "Kontext";
            //
            // pnlModus
            //
            this.pnlModus.Controls.Add(this.lblModusOrt);
            this.pnlModus.Controls.Add(this.rbModusCo2e);
            this.pnlModus.Controls.Add(this.rbModusCo2);
            this.pnlModus.Controls.Add(this.lblModus);
            this.pnlModus.Location = new System.Drawing.Point(12, 66);
            this.pnlModus.Name = "pnlModus";
            this.pnlModus.Size = new System.Drawing.Size(896, 30);
            this.pnlModus.TabIndex = 1;
            //
            // lblModus
            //
            this.lblModus.AutoSize = true;
            this.lblModus.Location = new System.Drawing.Point(3, 7);
            this.lblModus.Name = "lblModus";
            this.lblModus.Size = new System.Drawing.Size(106, 15);
            this.lblModus.TabIndex = 0;
            this.lblModus.Text = "CO2-Berechnung:";
            //
            // rbModusCo2
            //
            this.rbModusCo2.AutoSize = true;
            this.rbModusCo2.Location = new System.Drawing.Point(125, 5);
            this.rbModusCo2.Name = "rbModusCo2";
            this.rbModusCo2.Size = new System.Drawing.Size(52, 19);
            this.rbModusCo2.TabIndex = 1;
            this.rbModusCo2.TabStop = true;
            this.rbModusCo2.Text = "CO2";
            this.rbModusCo2.UseVisualStyleBackColor = true;
            //
            // rbModusCo2e
            //
            this.rbModusCo2e.AutoSize = true;
            this.rbModusCo2e.Location = new System.Drawing.Point(195, 5);
            this.rbModusCo2e.Name = "rbModusCo2e";
            this.rbModusCo2e.Size = new System.Drawing.Size(190, 19);
            this.rbModusCo2e.TabIndex = 2;
            this.rbModusCo2e.Text = "CO2-Äquivalent (GWP100)";
            this.rbModusCo2e.UseVisualStyleBackColor = true;
            //
            // lblModusOrt
            //
            this.lblModusOrt.AutoSize = true;
            this.lblModusOrt.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblModusOrt.Location = new System.Drawing.Point(400, 7);
            this.lblModusOrt.Name = "lblModusOrt";
            this.lblModusOrt.Size = new System.Drawing.Size(100, 15);
            this.lblModusOrt.TabIndex = 3;
            this.lblModusOrt.Text = "[globale Vorgabe]";
            //
            // grpArten
            //
            this.grpArten.Controls.Add(this.btnArtLoeschen);
            this.grpArten.Controls.Add(this.btnArtBearbeiten);
            this.grpArten.Controls.Add(this.btnArtNeu);
            this.grpArten.Controls.Add(this.dgvArten);
            this.grpArten.Location = new System.Drawing.Point(12, 102);
            this.grpArten.Name = "grpArten";
            this.grpArten.Size = new System.Drawing.Size(400, 360);
            this.grpArten.TabIndex = 2;
            this.grpArten.TabStop = false;
            this.grpArten.Text = "Emissionsarten";
            //
            // dgvArten
            //
            this.dgvArten.AllowUserToAddRows = false;
            this.dgvArten.AllowUserToDeleteRows = false;
            this.dgvArten.AllowUserToResizeRows = false;
            this.dgvArten.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvArten.Location = new System.Drawing.Point(8, 22);
            this.dgvArten.MultiSelect = false;
            this.dgvArten.Name = "dgvArten";
            this.dgvArten.RowHeadersVisible = false;
            this.dgvArten.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvArten.Size = new System.Drawing.Size(384, 292);
            this.dgvArten.TabIndex = 0;
            //
            // btnArtNeu
            //
            this.btnArtNeu.Location = new System.Drawing.Point(8, 322);
            this.btnArtNeu.Name = "btnArtNeu";
            this.btnArtNeu.Size = new System.Drawing.Size(80, 27);
            this.btnArtNeu.TabIndex = 1;
            this.btnArtNeu.Text = "Neu…";
            this.btnArtNeu.UseVisualStyleBackColor = true;
            //
            // btnArtBearbeiten
            //
            this.btnArtBearbeiten.Location = new System.Drawing.Point(94, 322);
            this.btnArtBearbeiten.Name = "btnArtBearbeiten";
            this.btnArtBearbeiten.Size = new System.Drawing.Size(110, 27);
            this.btnArtBearbeiten.TabIndex = 2;
            this.btnArtBearbeiten.Text = "Bearbeiten…";
            this.btnArtBearbeiten.UseVisualStyleBackColor = true;
            //
            // btnArtLoeschen
            //
            this.btnArtLoeschen.Location = new System.Drawing.Point(210, 322);
            this.btnArtLoeschen.Name = "btnArtLoeschen";
            this.btnArtLoeschen.Size = new System.Drawing.Size(90, 27);
            this.btnArtLoeschen.TabIndex = 3;
            this.btnArtLoeschen.Text = "Löschen";
            this.btnArtLoeschen.UseVisualStyleBackColor = true;
            //
            // grpWerte
            //
            this.grpWerte.Controls.Add(this.btnWertLoeschen);
            this.grpWerte.Controls.Add(this.btnWertBearbeiten);
            this.grpWerte.Controls.Add(this.btnWertNeu);
            this.grpWerte.Controls.Add(this.btnUebernehmen);
            this.grpWerte.Controls.Add(this.dgvWerte);
            this.grpWerte.Location = new System.Drawing.Point(420, 102);
            this.grpWerte.Name = "grpWerte";
            this.grpWerte.Size = new System.Drawing.Size(488, 360);
            this.grpWerte.TabIndex = 3;
            this.grpWerte.TabStop = false;
            this.grpWerte.Text = "Werte";
            //
            // dgvWerte
            //
            this.dgvWerte.AllowUserToAddRows = false;
            this.dgvWerte.AllowUserToDeleteRows = false;
            this.dgvWerte.AllowUserToResizeRows = false;
            this.dgvWerte.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvWerte.Location = new System.Drawing.Point(8, 22);
            this.dgvWerte.MultiSelect = false;
            this.dgvWerte.Name = "dgvWerte";
            this.dgvWerte.ReadOnly = true;
            this.dgvWerte.RowHeadersVisible = false;
            this.dgvWerte.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvWerte.Size = new System.Drawing.Size(472, 292);
            this.dgvWerte.TabIndex = 0;
            //
            // btnUebernehmen
            //
            this.btnUebernehmen.Location = new System.Drawing.Point(8, 322);
            this.btnUebernehmen.Name = "btnUebernehmen";
            this.btnUebernehmen.Size = new System.Drawing.Size(120, 27);
            this.btnUebernehmen.TabIndex = 1;
            this.btnUebernehmen.Text = "Übernehmen";
            this.btnUebernehmen.UseVisualStyleBackColor = true;
            //
            // btnWertNeu
            //
            this.btnWertNeu.Location = new System.Drawing.Point(196, 322);
            this.btnWertNeu.Name = "btnWertNeu";
            this.btnWertNeu.Size = new System.Drawing.Size(80, 27);
            this.btnWertNeu.TabIndex = 2;
            this.btnWertNeu.Text = "Neu…";
            this.btnWertNeu.UseVisualStyleBackColor = true;
            //
            // btnWertBearbeiten
            //
            this.btnWertBearbeiten.Location = new System.Drawing.Point(282, 322);
            this.btnWertBearbeiten.Name = "btnWertBearbeiten";
            this.btnWertBearbeiten.Size = new System.Drawing.Size(110, 27);
            this.btnWertBearbeiten.TabIndex = 3;
            this.btnWertBearbeiten.Text = "Bearbeiten…";
            this.btnWertBearbeiten.UseVisualStyleBackColor = true;
            //
            // btnWertLoeschen
            //
            this.btnWertLoeschen.Location = new System.Drawing.Point(398, 322);
            this.btnWertLoeschen.Name = "btnWertLoeschen";
            this.btnWertLoeschen.Size = new System.Drawing.Size(82, 27);
            this.btnWertLoeschen.TabIndex = 4;
            this.btnWertLoeschen.Text = "Löschen";
            this.btnWertLoeschen.UseVisualStyleBackColor = true;
            //
            // lblHinweis
            //
            this.lblHinweis.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(90)))), ((int)(((byte)(90)))), ((int)(((byte)(90)))));
            this.lblHinweis.Location = new System.Drawing.Point(12, 468);
            this.lblHinweis.Name = "lblHinweis";
            this.lblHinweis.Size = new System.Drawing.Size(680, 44);
            this.lblHinweis.TabIndex = 4;
            //
            // btnOk
            //
            this.btnOk.Location = new System.Drawing.Point(700, 478);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(95, 27);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "OK";
            this.btnOk.UseVisualStyleBackColor = true;
            //
            // btnAbbrechen
            //
            this.btnAbbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAbbrechen.Location = new System.Drawing.Point(805, 478);
            this.btnAbbrechen.Name = "btnAbbrechen";
            this.btnAbbrechen.Size = new System.Drawing.Size(103, 27);
            this.btnAbbrechen.TabIndex = 6;
            this.btnAbbrechen.Text = "Abbrechen";
            this.btnAbbrechen.UseVisualStyleBackColor = true;
            //
            // Form_Emissionskatalog
            //
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnAbbrechen;
            this.ClientSize = new System.Drawing.Size(920, 519);
            this.Controls.Add(this.btnAbbrechen);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.lblHinweis);
            this.Controls.Add(this.grpWerte);
            this.Controls.Add(this.grpArten);
            this.Controls.Add(this.pnlModus);
            this.Controls.Add(this.pnlKopf);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_Emissionskatalog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Emissionsfaktor-Katalog";
            this.pnlKopf.ResumeLayout(false);
            this.pnlKopf.PerformLayout();
            this.pnlModus.ResumeLayout(false);
            this.pnlModus.PerformLayout();
            this.grpArten.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvArten)).EndInit();
            this.grpWerte.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvWerte)).EndInit();
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlKopf;
        private System.Windows.Forms.Label lblKopfTitel;
        private System.Windows.Forms.Label lblKontext;
        private System.Windows.Forms.Panel pnlModus;
        private System.Windows.Forms.Label lblModus;
        private System.Windows.Forms.RadioButton rbModusCo2;
        private System.Windows.Forms.RadioButton rbModusCo2e;
        private System.Windows.Forms.Label lblModusOrt;
        private System.Windows.Forms.GroupBox grpArten;
        private System.Windows.Forms.DataGridView dgvArten;
        private System.Windows.Forms.Button btnArtNeu;
        private System.Windows.Forms.Button btnArtBearbeiten;
        private System.Windows.Forms.Button btnArtLoeschen;
        private System.Windows.Forms.GroupBox grpWerte;
        private System.Windows.Forms.DataGridView dgvWerte;
        private System.Windows.Forms.Button btnUebernehmen;
        private System.Windows.Forms.Button btnWertNeu;
        private System.Windows.Forms.Button btnWertBearbeiten;
        private System.Windows.Forms.Button btnWertLoeschen;
        private System.Windows.Forms.Label lblHinweis;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnAbbrechen;
    }
}
