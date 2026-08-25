namespace WindowsFormsApplication1
{
    partial class Form_Energietraeger
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
            this.lblKontext = new System.Windows.Forms.Label();
            this.lblKopfTitel = new System.Windows.Forms.Label();
            this.pnlListe = new System.Windows.Forms.Panel();
            this.lstTraeger = new System.Windows.Forms.ListBox();
            this.pnlListeKopf = new System.Windows.Forms.Panel();
            this.lblListeTitel = new System.Windows.Forms.Label();
            this.pnlFuss = new System.Windows.Forms.Panel();
            this.btnSchliessen = new System.Windows.Forms.Button();
            this.pnlInhalt = new System.Windows.Forms.Panel();
            this.pnlKopf.SuspendLayout();
            this.pnlListe.SuspendLayout();
            this.pnlListeKopf.SuspendLayout();
            this.pnlFuss.SuspendLayout();
            this.SuspendLayout();
            //
            // pnlKopf
            //
            this.pnlKopf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(31)))), ((int)(((byte)(61)))));
            this.pnlKopf.Controls.Add(this.lblKontext);
            this.pnlKopf.Controls.Add(this.lblKopfTitel);
            this.pnlKopf.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlKopf.Location = new System.Drawing.Point(0, 0);
            this.pnlKopf.Name = "pnlKopf";
            this.pnlKopf.Size = new System.Drawing.Size(1084, 48);
            this.pnlKopf.TabIndex = 0;
            //
            // lblKopfTitel
            //
            this.lblKopfTitel.AutoSize = true;
            this.lblKopfTitel.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.lblKopfTitel.ForeColor = System.Drawing.Color.White;
            this.lblKopfTitel.Location = new System.Drawing.Point(14, 11);
            this.lblKopfTitel.Name = "lblKopfTitel";
            this.lblKopfTitel.Size = new System.Drawing.Size(203, 21);
            this.lblKopfTitel.TabIndex = 0;
            this.lblKopfTitel.Text = "Energieträgerverwaltung";
            //
            // lblKontext
            //
            this.lblKontext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblKontext.AutoSize = true;
            this.lblKontext.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(210)))), ((int)(((byte)(230)))));
            this.lblKontext.Location = new System.Drawing.Point(902, 16);
            this.lblKontext.Name = "lblKontext";
            this.lblKontext.Size = new System.Drawing.Size(170, 15);
            this.lblKontext.TabIndex = 1;
            this.lblKontext.Text = "Kontext: Katalog (Stammdaten)";
            this.lblKontext.TextAlign = System.Drawing.ContentAlignment.TopRight;
            //
            // pnlListe
            //
            this.pnlListe.Controls.Add(this.lstTraeger);
            this.pnlListe.Controls.Add(this.pnlListeKopf);
            this.pnlListe.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlListe.Location = new System.Drawing.Point(0, 48);
            this.pnlListe.Name = "pnlListe";
            this.pnlListe.Padding = new System.Windows.Forms.Padding(0, 0, 1, 0);
            this.pnlListe.Size = new System.Drawing.Size(300, 584);
            this.pnlListe.TabIndex = 1;
            //
            // pnlListeKopf
            //
            this.pnlListeKopf.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(26)))), ((int)(((byte)(50)))), ((int)(((byte)(97)))));
            this.pnlListeKopf.Controls.Add(this.lblListeTitel);
            this.pnlListeKopf.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlListeKopf.Location = new System.Drawing.Point(0, 0);
            this.pnlListeKopf.Name = "pnlListeKopf";
            this.pnlListeKopf.Size = new System.Drawing.Size(299, 30);
            this.pnlListeKopf.TabIndex = 0;
            //
            // lblListeTitel
            //
            this.lblListeTitel.AutoSize = true;
            this.lblListeTitel.Font = new System.Drawing.Font("Segoe UI", 9.75F, System.Drawing.FontStyle.Bold);
            this.lblListeTitel.ForeColor = System.Drawing.Color.White;
            this.lblListeTitel.Location = new System.Drawing.Point(10, 6);
            this.lblListeTitel.Name = "lblListeTitel";
            this.lblListeTitel.Size = new System.Drawing.Size(101, 17);
            this.lblListeTitel.TabIndex = 0;
            this.lblListeTitel.Text = "Energieträger";
            //
            // lstTraeger
            //
            this.lstTraeger.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstTraeger.Font = new System.Drawing.Font("Segoe UI", 9.75F);
            this.lstTraeger.FormattingEnabled = true;
            this.lstTraeger.IntegralHeight = false;
            this.lstTraeger.ItemHeight = 17;
            this.lstTraeger.Location = new System.Drawing.Point(0, 30);
            this.lstTraeger.Name = "lstTraeger";
            this.lstTraeger.Size = new System.Drawing.Size(299, 554);
            this.lstTraeger.TabIndex = 1;
            this.lstTraeger.SelectedIndexChanged += new System.EventHandler(this.lstTraeger_SelectedIndexChanged);
            //
            // pnlFuss
            //
            this.pnlFuss.Controls.Add(this.btnSchliessen);
            this.pnlFuss.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlFuss.Location = new System.Drawing.Point(300, 632);
            this.pnlFuss.Name = "pnlFuss";
            this.pnlFuss.Size = new System.Drawing.Size(784, 48);
            this.pnlFuss.TabIndex = 3;
            //
            // btnSchliessen
            //
            this.btnSchliessen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSchliessen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnSchliessen.Location = new System.Drawing.Point(662, 9);
            this.btnSchliessen.Name = "btnSchliessen";
            this.btnSchliessen.Size = new System.Drawing.Size(110, 30);
            this.btnSchliessen.TabIndex = 0;
            this.btnSchliessen.Text = "Schließen";
            this.btnSchliessen.UseVisualStyleBackColor = true;
            //
            // pnlInhalt
            //
            this.pnlInhalt.AutoScroll = true;
            this.pnlInhalt.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(252)))));
            this.pnlInhalt.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlInhalt.Location = new System.Drawing.Point(300, 48);
            this.pnlInhalt.Name = "pnlInhalt";
            this.pnlInhalt.Size = new System.Drawing.Size(784, 584);
            this.pnlInhalt.TabIndex = 2;
            //
            // Form_Energietraeger
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnSchliessen;
            this.ClientSize = new System.Drawing.Size(1084, 680);
            this.Controls.Add(this.pnlInhalt);
            this.Controls.Add(this.pnlFuss);
            this.Controls.Add(this.pnlListe);
            this.Controls.Add(this.pnlKopf);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.MinimumSize = new System.Drawing.Size(900, 560);
            this.Name = "Form_Energietraeger";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Energieträgerverwaltung";
            this.pnlKopf.ResumeLayout(false);
            this.pnlKopf.PerformLayout();
            this.pnlListe.ResumeLayout(false);
            this.pnlListeKopf.ResumeLayout(false);
            this.pnlListeKopf.PerformLayout();
            this.pnlFuss.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel pnlKopf;
        private System.Windows.Forms.Label lblKopfTitel;
        private System.Windows.Forms.Label lblKontext;
        private System.Windows.Forms.Panel pnlListe;
        private System.Windows.Forms.Panel pnlListeKopf;
        private System.Windows.Forms.Label lblListeTitel;
        private System.Windows.Forms.ListBox lstTraeger;
        private System.Windows.Forms.Panel pnlFuss;
        private System.Windows.Forms.Button btnSchliessen;
        private System.Windows.Forms.Panel pnlInhalt;
    }
}
