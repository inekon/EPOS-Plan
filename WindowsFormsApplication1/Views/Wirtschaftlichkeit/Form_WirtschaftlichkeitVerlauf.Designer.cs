namespace WindowsFormsApplication1
{
    partial class Form_WirtschaftlichkeitVerlauf
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

        // ---------------------------------------------------------------------------
        // Design-Politur 21.08.2026 — Geometrie
        //
        // Die Feldnamen-Platzhalter sind durch die ECHTEN deutschen Texte aus
        // Form_WirtschaftlichkeitVerlauf.TexteSetzen() ersetzt, damit die
        // Entwurfsansicht die Maske zeigt. TexteSetzen() bleibt die einzige Quelle und
        // überschreibt sie beim Start erneut; im Fenstertitel steht hier nur der feste
        // Teil — den Stammnamen hängt TexteSetzen() an.
        //
        // Am echten Text nachgemessen (Segoe UI 9 pt, 96 dpi): „Zeitraum [Jahre]:“
        // 96 px (x = 12 -> rechte Kante 108, numJahre bei 118: 10 px Abstand),
        // „Szenario:“ 54 px (x = 206 -> 260, cbSzenario bei 272: 12 px Abstand),
        // „Aktualisieren“ 75 px und „Schließen“/„Abbrechen“ 65 px (beide passen).
        // Die Beschriftungen und die Auswahlliste blieben deshalb unverändert.
        //
        // Geändert wurden allein die Knopfgrößen auf das Hausmaß 110 x 30:
        //   * btnZeichnen 110x27 -> 110x30 (Unterkante 40, picDiff bei 46: 6 px Abstand).
        //   * btnSchliessen 100x27 -> 110x30; er ist Top|Right verankert, x geht
        //     786 -> 776, damit die rechte Kante bei 886 = ClientSize 898 − 12 bleibt.
        // ClientSize bleibt 898 x 744 — auf diesen Entwurfswert bezieht sich
        // Form_WirtschaftlichkeitVerlauf.GroesseAufArbeitsflaecheDeckeln().
        // ---------------------------------------------------------------------------

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.lblZeitraum = new System.Windows.Forms.Label();
            this.numJahre = new System.Windows.Forms.NumericUpDown();
            this.lblSzenario = new System.Windows.Forms.Label();
            this.cbSzenario = new System.Windows.Forms.ComboBox();
            this.btnZeichnen = new System.Windows.Forms.Button();
            this.btnSchliessen = new System.Windows.Forms.Button();
            this.picDiff = new System.Windows.Forms.PictureBox();
            this.picAbsolut = new System.Windows.Forms.PictureBox();
            this.lblRestwert = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progress = new System.Windows.Forms.ProgressBar();
            this._tooltip = new System.Windows.Forms.ToolTip(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.numJahre)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDiff)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picAbsolut)).BeginInit();
            this.SuspendLayout();
            //
            // lblZeitraum
            //
            this.lblZeitraum.AutoSize = true;
            this.lblZeitraum.Location = new System.Drawing.Point(12, 16);
            this.lblZeitraum.Name = "lblZeitraum";
            this.lblZeitraum.Text = "Zeitraum [Jahre]:";
            //
            // numJahre
            //
            this.numJahre.Location = new System.Drawing.Point(118, 12);
            this.numJahre.Maximum = new decimal(new int[] { 60, 0, 0, 0});
            this.numJahre.Minimum = new decimal(new int[] { 2, 0, 0, 0});
            this.numJahre.Name = "numJahre";
            this.numJahre.Size = new System.Drawing.Size(70, 23);
            this.numJahre.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numJahre.Value = new decimal(new int[] { 20, 0, 0, 0});
            //
            // lblSzenario
            //
            this.lblSzenario.AutoSize = true;
            this.lblSzenario.Location = new System.Drawing.Point(206, 16);
            this.lblSzenario.Name = "lblSzenario";
            this.lblSzenario.Text = "Szenario:";
            //
            // cbSzenario
            //
            this.cbSzenario.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSzenario.Location = new System.Drawing.Point(272, 12);
            this.cbSzenario.Name = "cbSzenario";
            this.cbSzenario.Width = 130;
            //
            // btnZeichnen
            //
            this.btnZeichnen.Location = new System.Drawing.Point(418, 10);
            this.btnZeichnen.Name = "btnZeichnen";
            this.btnZeichnen.Size = new System.Drawing.Size(110, 30);
            this.btnZeichnen.Text = "Aktualisieren";
            this.btnZeichnen.Click += new System.EventHandler(this.btnZeichnen_Click);
            //
            // btnSchliessen
            //
            this.btnSchliessen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSchliessen.Location = new System.Drawing.Point(776, 10);
            this.btnSchliessen.Name = "btnSchliessen";
            this.btnSchliessen.Size = new System.Drawing.Size(110, 30);
            this.btnSchliessen.Text = "Schließen";
            this.btnSchliessen.Click += new System.EventHandler(this.btnSchliessen_Click);
            //
            // picDiff
            //
            this.picDiff.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.picDiff.BackColor = System.Drawing.Color.White;
            this.picDiff.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picDiff.Location = new System.Drawing.Point(12, 46);
            this.picDiff.Name = "picDiff";
            this.picDiff.Size = new System.Drawing.Size(874, 320);
            this.picDiff.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            //
            // picAbsolut
            //
            this.picAbsolut.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.picAbsolut.BackColor = System.Drawing.Color.White;
            this.picAbsolut.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picAbsolut.Location = new System.Drawing.Point(12, 372);
            this.picAbsolut.Name = "picAbsolut";
            this.picAbsolut.Size = new System.Drawing.Size(874, 316);
            this.picAbsolut.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            //
            // lblRestwert
            //
            this.lblRestwert.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblRestwert.AutoEllipsis = true;
            this.lblRestwert.ForeColor = System.Drawing.Color.DimGray;
            this.lblRestwert.Location = new System.Drawing.Point(12, 694);
            this.lblRestwert.Name = "lblRestwert";
            this.lblRestwert.Size = new System.Drawing.Size(874, 30);
            //
            // lblStatus
            //
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblStatus.Location = new System.Drawing.Point(12, 726);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(600, 18);
            //
            // progress
            //
            this.progress.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.progress.Location = new System.Drawing.Point(636, 728);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(250, 14);
            this.progress.Visible = false;
            //
            // Form_WirtschaftlichkeitVerlauf
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(898, 744);
            this.Controls.Add(this.lblZeitraum);
            this.Controls.Add(this.numJahre);
            this.Controls.Add(this.lblSzenario);
            this.Controls.Add(this.cbSzenario);
            this.Controls.Add(this.btnZeichnen);
            this.Controls.Add(this.btnSchliessen);
            this.Controls.Add(this.picDiff);
            this.Controls.Add(this.picAbsolut);
            this.Controls.Add(this.lblRestwert);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progress);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(760, 560);
            this.Name = "Form_WirtschaftlichkeitVerlauf";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Kapitalwert-Verlauf über den Nutzungszeitraum — Stamm: ";
            this.Load += new System.EventHandler(this.Form_WirtschaftlichkeitVerlauf_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form_WirtschaftlichkeitVerlauf_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Form_WirtschaftlichkeitVerlauf_FormClosed);
            ((System.ComponentModel.ISupportInitialize)(this.numJahre)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picDiff)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picAbsolut)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblZeitraum;
        private System.Windows.Forms.NumericUpDown numJahre;
        private System.Windows.Forms.Label lblSzenario;
        private System.Windows.Forms.ComboBox cbSzenario;
        private System.Windows.Forms.Button btnZeichnen;
        private System.Windows.Forms.Button btnSchliessen;
        private System.Windows.Forms.PictureBox picDiff;
        private System.Windows.Forms.PictureBox picAbsolut;
        private System.Windows.Forms.Label lblRestwert;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.ToolTip _tooltip;
    }
}
