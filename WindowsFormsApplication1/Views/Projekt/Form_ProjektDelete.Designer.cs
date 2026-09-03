namespace WindowsFormsApplication1
{
    partial class Form_ProjektDelete
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        // Layout im Code (kein .resx-Layout): Texte setzt TexteSetzen() aus den
        // Ressourcen — die Entwurfstexte hier sind deutsche Vorgaben (Ä6-Regel).
        private void InitializeComponent()
        {
            this.lblHinweis = new System.Windows.Forms.Label();
            this.ucAuswahl = new WindowsFormsApplication1.ProjektAuswahl();
            this.lnkAlle = new System.Windows.Forms.LinkLabel();
            this.lnkKeine = new System.Windows.Forms.LinkLabel();
            this.chkSicherung = new System.Windows.Forms.CheckBox();
            this.btn_Loeschen = new System.Windows.Forms.Button();
            this.btn_Abbrechen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblHinweis
            //
            this.lblHinweis.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHinweis.ForeColor = System.Drawing.Color.DimGray;
            this.lblHinweis.Location = new System.Drawing.Point(12, 12);
            this.lblHinweis.Name = "lblHinweis";
            this.lblHinweis.Size = new System.Drawing.Size(616, 36);
            this.lblHinweis.Text = "Wählen Sie die zu löschenden Projekte per Häkchen.";
            //
            // ucAuswahl
            //
            this.ucAuswahl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
            this.ucAuswahl.Location = new System.Drawing.Point(12, 54);
            this.ucAuswahl.Name = "ucAuswahl";
            this.ucAuswahl.Size = new System.Drawing.Size(616, 372);
            this.ucAuswahl.AuswahlGeaendert += new System.EventHandler(this.ucAuswahl_AuswahlGeaendert);
            //
            // lnkAlle
            //
            this.lnkAlle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lnkAlle.AutoSize = true;
            this.lnkAlle.Location = new System.Drawing.Point(12, 436);
            this.lnkAlle.Name = "lnkAlle";
            this.lnkAlle.Text = "Alle sichtbaren auswählen";
            this.lnkAlle.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkAlle_LinkClicked);
            //
            // lnkKeine
            //
            this.lnkKeine.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lnkKeine.AutoSize = true;
            this.lnkKeine.Location = new System.Drawing.Point(200, 436);
            this.lnkKeine.Name = "lnkKeine";
            this.lnkKeine.Text = "Auswahl aufheben";
            this.lnkKeine.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lnkKeine_LinkClicked);
            //
            // chkSicherung
            //
            this.chkSicherung.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.chkSicherung.AutoSize = true;
            this.chkSicherung.Checked = true;
            this.chkSicherung.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSicherung.Location = new System.Drawing.Point(12, 464);
            this.chkSicherung.Name = "chkSicherung";
            this.chkSicherung.Text = "Sicherungskopie der Datenbank vor dem Löschen anlegen";
            //
            // btn_Loeschen
            //
            this.btn_Loeschen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Loeschen.Enabled = false;
            this.btn_Loeschen.Location = new System.Drawing.Point(432, 498);
            this.btn_Loeschen.Name = "btn_Loeschen";
            this.btn_Loeschen.Size = new System.Drawing.Size(100, 30);
            this.btn_Loeschen.Text = "Löschen…";
            this.btn_Loeschen.UseVisualStyleBackColor = true;
            this.btn_Loeschen.Click += new System.EventHandler(this.btn_Loeschen_Click);
            //
            // btn_Abbrechen
            //
            this.btn_Abbrechen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btn_Abbrechen.Location = new System.Drawing.Point(540, 498);
            this.btn_Abbrechen.Name = "btn_Abbrechen";
            this.btn_Abbrechen.Size = new System.Drawing.Size(88, 30);
            this.btn_Abbrechen.Text = "Abbrechen";
            this.btn_Abbrechen.UseVisualStyleBackColor = true;
            this.btn_Abbrechen.Click += new System.EventHandler(this.btn_Abbrechen_Click);
            //
            // Form_ProjektDelete
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(640, 540);
            this.Controls.Add(this.lblHinweis);
            this.Controls.Add(this.ucAuswahl);
            this.Controls.Add(this.lnkAlle);
            this.Controls.Add(this.lnkKeine);
            this.Controls.Add(this.chkSicherung);
            this.Controls.Add(this.btn_Loeschen);
            this.Controls.Add(this.btn_Abbrechen);
            this.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.MinimumSize = new System.Drawing.Size(520, 420);
            this.Name = "Form_ProjektDelete";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Projekte löschen";
            this.Load += new System.EventHandler(this.Form_ProjektDelete_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblHinweis;
        private WindowsFormsApplication1.ProjektAuswahl ucAuswahl;
        private System.Windows.Forms.LinkLabel lnkAlle;
        private System.Windows.Forms.LinkLabel lnkKeine;
        private System.Windows.Forms.CheckBox chkSicherung;
        private System.Windows.Forms.Button btn_Loeschen;
        private System.Windows.Forms.Button btn_Abbrechen;
    }
}
