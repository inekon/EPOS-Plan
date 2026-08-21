namespace WindowsFormsApplication1
{
    partial class Form_Gesetzesparameter
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
            this.lblHinweis = new System.Windows.Forms.Label();
            this.lblKlasse = new System.Windows.Forms.Label();
            this.cbKlasse = new System.Windows.Forms.ComboBox();
            this.lvZeilen = new System.Windows.Forms.ListView();
            this.colSchluessel = new System.Windows.Forms.ColumnHeader();
            this.colJahrVon = new System.Windows.Forms.ColumnHeader();
            this.colWert = new System.Windows.Forms.ColumnHeader();
            this.colEinheit = new System.Windows.Forms.ColumnHeader();
            this.colStatus = new System.Windows.Forms.ColumnHeader();
            this.colQuelle = new System.Windows.Forms.ColumnHeader();
            this.btnNeu = new System.Windows.Forms.Button();
            this.btnAendern = new System.Windows.Forms.Button();
            this.btnLoeschen = new System.Windows.Forms.Button();
            this.btnSchliessen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblHinweis
            //
            this.lblHinweis.AutoSize = false;
            this.lblHinweis.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(90)))), ((int)(((byte)(160)))));
            this.lblHinweis.Location = new System.Drawing.Point(12, 10);
            this.lblHinweis.Name = "lblHinweis";
            this.lblHinweis.Size = new System.Drawing.Size(916, 34);
            this.lblHinweis.Text = "Eine Gesetzesänderung ist eine neue Jahreszeile, kein Ändern der alten. Nur so lie" +
    "fert eine heute gerechnete Variante in einigen Jahren noch dieselben Zahlen.";
            //
            // lblKlasse
            //
            this.lblKlasse.AutoSize = true;
            this.lblKlasse.Location = new System.Drawing.Point(12, 54);
            this.lblKlasse.Name = "lblKlasse";
            this.lblKlasse.Text = "Bereich";
            //
            // cbKlasse
            //
            this.cbKlasse.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbKlasse.Location = new System.Drawing.Point(90, 51);
            this.cbKlasse.Name = "cbKlasse";
            this.cbKlasse.Width = 320;
            this.cbKlasse.SelectedIndexChanged += new System.EventHandler(this.cbKlasse_SelectedIndexChanged);
            //
            // lvZeilen
            //
            this.lvZeilen.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lvZeilen.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colSchluessel,
            this.colJahrVon,
            this.colWert,
            this.colEinheit,
            this.colStatus,
            this.colQuelle});
            this.lvZeilen.FullRowSelect = true;
            this.lvZeilen.HideSelection = false;
            this.lvZeilen.Location = new System.Drawing.Point(12, 84);
            this.lvZeilen.MultiSelect = false;
            this.lvZeilen.Name = "lvZeilen";
            this.lvZeilen.Size = new System.Drawing.Size(916, 424);
            this.lvZeilen.View = System.Windows.Forms.View.Details;
            this.lvZeilen.DoubleClick += new System.EventHandler(this.btnAendern_Click);
            //
            // colSchluessel
            //
            this.colSchluessel.Name = "colSchluessel";
            this.colSchluessel.Text = "Schlüssel";
            this.colSchluessel.Width = 300;
            //
            // colJahrVon
            //
            this.colJahrVon.Name = "colJahrVon";
            this.colJahrVon.Text = "Gültig ab";
            this.colJahrVon.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colJahrVon.Width = 70;
            //
            // colWert
            //
            this.colWert.Name = "colWert";
            this.colWert.Text = "Wert";
            this.colWert.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.colWert.Width = 90;
            //
            // colEinheit
            //
            this.colEinheit.Name = "colEinheit";
            this.colEinheit.Text = "Einheit";
            this.colEinheit.Width = 80;
            //
            // colStatus
            //
            this.colStatus.Name = "colStatus";
            this.colStatus.Text = "Status";
            this.colStatus.Width = 90;
            //
            // colQuelle
            //
            this.colQuelle.Name = "colQuelle";
            this.colQuelle.Text = "Quelle";
            this.colQuelle.Width = 270;
            //
            // btnNeu
            //
            this.btnNeu.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnNeu.Location = new System.Drawing.Point(12, 520);
            this.btnNeu.Name = "btnNeu";
            this.btnNeu.Size = new System.Drawing.Size(110, 30);
            this.btnNeu.Text = "Neu…";
            this.btnNeu.Click += new System.EventHandler(this.btnNeu_Click);
            //
            // btnAendern
            //
            this.btnAendern.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAendern.Location = new System.Drawing.Point(132, 520);
            this.btnAendern.Name = "btnAendern";
            this.btnAendern.Size = new System.Drawing.Size(110, 30);
            this.btnAendern.Text = "Ändern…";
            this.btnAendern.Click += new System.EventHandler(this.btnAendern_Click);
            //
            // btnLoeschen
            //
            this.btnLoeschen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnLoeschen.Location = new System.Drawing.Point(252, 520);
            this.btnLoeschen.Name = "btnLoeschen";
            this.btnLoeschen.Size = new System.Drawing.Size(110, 30);
            this.btnLoeschen.Text = "Löschen";
            this.btnLoeschen.Click += new System.EventHandler(this.btnLoeschen_Click);
            //
            // btnSchliessen
            //
            this.btnSchliessen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSchliessen.Location = new System.Drawing.Point(818, 520);
            this.btnSchliessen.Name = "btnSchliessen";
            this.btnSchliessen.Size = new System.Drawing.Size(110, 30);
            this.btnSchliessen.Text = "Schließen";
            this.btnSchliessen.Click += new System.EventHandler(this.btnSchliessen_Click);
            //
            // Form_Gesetzesparameter
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnSchliessen;
            this.ClientSize = new System.Drawing.Size(940, 560);
            this.Controls.Add(this.lblHinweis);
            this.Controls.Add(this.lblKlasse);
            this.Controls.Add(this.cbKlasse);
            this.Controls.Add(this.lvZeilen);
            this.Controls.Add(this.btnNeu);
            this.Controls.Add(this.btnAendern);
            this.Controls.Add(this.btnLoeschen);
            this.Controls.Add(this.btnSchliessen);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(760, 420);
            this.Name = "Form_Gesetzesparameter";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Gesetzliche Parameter";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblHinweis;
        private System.Windows.Forms.Label lblKlasse;
        private System.Windows.Forms.ComboBox cbKlasse;
        private System.Windows.Forms.ListView lvZeilen;
        private System.Windows.Forms.ColumnHeader colSchluessel;
        private System.Windows.Forms.ColumnHeader colJahrVon;
        private System.Windows.Forms.ColumnHeader colWert;
        private System.Windows.Forms.ColumnHeader colEinheit;
        private System.Windows.Forms.ColumnHeader colStatus;
        private System.Windows.Forms.ColumnHeader colQuelle;
        private System.Windows.Forms.Button btnNeu;
        private System.Windows.Forms.Button btnAendern;
        private System.Windows.Forms.Button btnLoeschen;
        private System.Windows.Forms.Button btnSchliessen;
    }
}
