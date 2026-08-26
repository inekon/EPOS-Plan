namespace WindowsFormsApplication1
{
    partial class Form_BkUebernahme
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

        // ==================================================================
        // Design-Politur 21.08.2026 — Echttexte und Geometrie
        // ==================================================================
        // Im Entwurf stehen jetzt die deutschen Echttexte aus MyResource
        // (BK_UEB_*) statt der Feldnamen. Zur Laufzeit überschreibt
        // TexteSetzen() sie unverändert; die Literale hier dienen allein dem
        // Entwurfsbild und der Maßprüfung.
        //
        // Mit den Echttexten nachgemessen (Segoe UI 9 pt, 96 dpi, DpiUnaware):
        //
        //  - Spalte 0 (Absolute 132 px) trägt den längsten Titel „Wert der
        //    Quelle:" (94 px) mit Reserve — keine Änderung nötig.
        //  - Die Zeilenhöhen tragen die Echttexte: Zeile 1 (30 px) fasst die
        //    ComboBox (21 px + 8 px Rand), die Zeilen 2–5 und 7 (24/22 px) die
        //    einzeiligen Werte (21 px). Zeile 0 ist AutoSize und wächst mit
        //    dem Gegenstand selbst. Auch hier keine Änderung.
        //  - Geändert wurden nur die Fußknöpfe: MinimumSize 110 × 28 → 110 × 30
        //    (einheitliche Knopfhöhe) und Margin links 6 → 10 px, damit
        //    zwischen „Übernehmen" und „Abbrechen" mindestens 10 px liegen.
        //    Die rechte Kante bleibt durch den RightToLeft-Fluss konstant bei
        //    12 px Abstand zum Fensterrand — dieselbe Kante wie ComboBox und
        //    Klartextfeld. In Zeile 8 (40 px, davon 8 px Rand oben) bleiben
        //    unter den 30 px hohen Knöpfen 2 px, zusammen mit der unteren
        //    Innenkante (10 px) also 12 px — ClientSize unverändert.

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            tl = new System.Windows.Forms.TableLayoutPanel();
            lblGegenstand = new System.Windows.Forms.Label();
            lblQuelle = new System.Windows.Forms.Label();
            cbQuelle = new System.Windows.Forms.ComboBox();
            lblWertQuelleTitel = new System.Windows.Forms.Label();
            lblQuelleWert = new System.Windows.Forms.Label();
            lblZielTitel = new System.Windows.Forms.Label();
            lblZiel = new System.Windows.Forms.Label();
            lblWertZielTitel = new System.Windows.Forms.Label();
            lblZielWert = new System.Windows.Forms.Label();
            lblKomponenten = new System.Windows.Forms.Label();
            txtKlartext = new System.Windows.Forms.TextBox();
            lblGrund = new System.Windows.Forms.Label();
            pnlKnoepfe = new System.Windows.Forms.FlowLayoutPanel();
            btnAbbrechen = new System.Windows.Forms.Button();
            btnOk = new System.Windows.Forms.Button();
            tl.SuspendLayout();
            pnlKnoepfe.SuspendLayout();
            SuspendLayout();
            // 
            // tl
            // 
            tl.ColumnCount = 2;
            tl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 132F));
            tl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tl.Controls.Add(lblGegenstand, 0, 0);
            tl.Controls.Add(lblQuelle, 0, 1);
            tl.Controls.Add(cbQuelle, 1, 1);
            tl.Controls.Add(lblWertQuelleTitel, 0, 2);
            tl.Controls.Add(lblQuelleWert, 1, 2);
            tl.Controls.Add(lblZielTitel, 0, 3);
            tl.Controls.Add(lblZiel, 1, 3);
            tl.Controls.Add(lblWertZielTitel, 0, 4);
            tl.Controls.Add(lblZielWert, 1, 4);
            tl.Controls.Add(lblKomponenten, 0, 5);
            tl.Controls.Add(txtKlartext, 0, 6);
            tl.Controls.Add(lblGrund, 0, 7);
            tl.Controls.Add(pnlKnoepfe, 0, 8);
            tl.Dock = System.Windows.Forms.DockStyle.Fill;
            tl.Location = new System.Drawing.Point(0, 0);
            tl.Name = "tl";
            tl.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
            tl.RowCount = 9;
            tl.RowStyles.Add(new System.Windows.Forms.RowStyle());
            tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));
            tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));
            tl.Size = new System.Drawing.Size(520, 380);
            tl.TabIndex = 0;
            // 
            // lblGegenstand
            // 
            tl.SetColumnSpan(lblGegenstand, 2);
            lblGegenstand.Dock = System.Windows.Forms.DockStyle.Fill;
            lblGegenstand.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            lblGegenstand.ForeColor = System.Drawing.Color.FromArgb(31, 78, 121);
            lblGegenstand.Location = new System.Drawing.Point(12, 10);
            lblGegenstand.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            lblGegenstand.Name = "lblGegenstand";
            lblGegenstand.Size = new System.Drawing.Size(496, 23);
            lblGegenstand.TabIndex = 0;
            // 
            // lblQuelle
            // 
            lblQuelle.Dock = System.Windows.Forms.DockStyle.Fill;
            lblQuelle.Location = new System.Drawing.Point(15, 41);
            lblQuelle.Name = "lblQuelle";
            lblQuelle.Size = new System.Drawing.Size(126, 30);
            lblQuelle.TabIndex = 1;
            lblQuelle.Text = "Quelle:";
            lblQuelle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbQuelle
            // 
            cbQuelle.Dock = System.Windows.Forms.DockStyle.Fill;
            cbQuelle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            cbQuelle.Location = new System.Drawing.Point(144, 43);
            cbQuelle.Margin = new System.Windows.Forms.Padding(0, 2, 0, 6);
            cbQuelle.Name = "cbQuelle";
            cbQuelle.Size = new System.Drawing.Size(364, 33);
            cbQuelle.TabIndex = 2;
            cbQuelle.SelectedIndexChanged += cbQuelle_SelectedIndexChanged;
            // 
            // lblWertQuelleTitel
            // 
            lblWertQuelleTitel.Dock = System.Windows.Forms.DockStyle.Fill;
            lblWertQuelleTitel.Location = new System.Drawing.Point(15, 71);
            lblWertQuelleTitel.Name = "lblWertQuelleTitel";
            lblWertQuelleTitel.Size = new System.Drawing.Size(126, 24);
            lblWertQuelleTitel.TabIndex = 3;
            lblWertQuelleTitel.Text = "Wert der Quelle:";
            lblWertQuelleTitel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblQuelleWert
            // 
            lblQuelleWert.Dock = System.Windows.Forms.DockStyle.Fill;
            lblQuelleWert.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            lblQuelleWert.Location = new System.Drawing.Point(147, 71);
            lblQuelleWert.Name = "lblQuelleWert";
            lblQuelleWert.Size = new System.Drawing.Size(358, 24);
            lblQuelleWert.TabIndex = 4;
            lblQuelleWert.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblZielTitel
            // 
            lblZielTitel.Dock = System.Windows.Forms.DockStyle.Fill;
            lblZielTitel.Location = new System.Drawing.Point(15, 95);
            lblZielTitel.Name = "lblZielTitel";
            lblZielTitel.Size = new System.Drawing.Size(126, 24);
            lblZielTitel.TabIndex = 5;
            lblZielTitel.Text = "Ziel:";
            lblZielTitel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblZiel
            // 
            lblZiel.AutoEllipsis = true;
            lblZiel.Dock = System.Windows.Forms.DockStyle.Fill;
            lblZiel.Location = new System.Drawing.Point(147, 95);
            lblZiel.Name = "lblZiel";
            lblZiel.Size = new System.Drawing.Size(358, 24);
            lblZiel.TabIndex = 6;
            lblZiel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWertZielTitel
            // 
            lblWertZielTitel.Dock = System.Windows.Forms.DockStyle.Fill;
            lblWertZielTitel.Location = new System.Drawing.Point(15, 119);
            lblWertZielTitel.Name = "lblWertZielTitel";
            lblWertZielTitel.Size = new System.Drawing.Size(126, 24);
            lblWertZielTitel.TabIndex = 7;
            lblWertZielTitel.Text = "Wert des Ziels:";
            lblWertZielTitel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblZielWert
            // 
            lblZielWert.Dock = System.Windows.Forms.DockStyle.Fill;
            lblZielWert.Location = new System.Drawing.Point(147, 119);
            lblZielWert.Name = "lblZielWert";
            lblZielWert.Size = new System.Drawing.Size(358, 24);
            lblZielWert.TabIndex = 8;
            lblZielWert.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblKomponenten
            // 
            lblKomponenten.AutoEllipsis = true;
            tl.SetColumnSpan(lblKomponenten, 2);
            lblKomponenten.Dock = System.Windows.Forms.DockStyle.Fill;
            lblKomponenten.ForeColor = System.Drawing.Color.DimGray;
            lblKomponenten.Location = new System.Drawing.Point(15, 143);
            lblKomponenten.Name = "lblKomponenten";
            lblKomponenten.Size = new System.Drawing.Size(490, 22);
            lblKomponenten.TabIndex = 9;
            lblKomponenten.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtKlartext
            // 
            txtKlartext.BackColor = System.Drawing.SystemColors.Window;
            tl.SetColumnSpan(txtKlartext, 2);
            txtKlartext.Dock = System.Windows.Forms.DockStyle.Fill;
            txtKlartext.Font = new System.Drawing.Font("Segoe UI", 9F);
            txtKlartext.Location = new System.Drawing.Point(15, 168);
            txtKlartext.Multiline = true;
            txtKlartext.Name = "txtKlartext";
            txtKlartext.ReadOnly = true;
            txtKlartext.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            txtKlartext.Size = new System.Drawing.Size(490, 135);
            txtKlartext.TabIndex = 10;
            // 
            // lblGrund
            // 
            lblGrund.AutoEllipsis = true;
            tl.SetColumnSpan(lblGrund, 2);
            lblGrund.Dock = System.Windows.Forms.DockStyle.Fill;
            lblGrund.ForeColor = System.Drawing.Color.Firebrick;
            lblGrund.Location = new System.Drawing.Point(15, 306);
            lblGrund.Name = "lblGrund";
            lblGrund.Size = new System.Drawing.Size(490, 24);
            lblGrund.TabIndex = 11;
            lblGrund.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // pnlKnoepfe
            // 
            tl.SetColumnSpan(pnlKnoepfe, 2);
            pnlKnoepfe.Controls.Add(btnAbbrechen);
            pnlKnoepfe.Controls.Add(btnOk);
            pnlKnoepfe.Dock = System.Windows.Forms.DockStyle.Fill;
            pnlKnoepfe.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            pnlKnoepfe.Location = new System.Drawing.Point(12, 338);
            pnlKnoepfe.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            pnlKnoepfe.Name = "pnlKnoepfe";
            pnlKnoepfe.Size = new System.Drawing.Size(496, 32);
            pnlKnoepfe.TabIndex = 12;
            // 
            // btnAbbrechen
            // 
            btnAbbrechen.AutoSize = true;
            btnAbbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            btnAbbrechen.Location = new System.Drawing.Point(386, 0);
            btnAbbrechen.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            btnAbbrechen.MinimumSize = new System.Drawing.Size(110, 30);
            btnAbbrechen.Name = "btnAbbrechen";
            btnAbbrechen.Size = new System.Drawing.Size(110, 35);
            btnAbbrechen.TabIndex = 0;
            btnAbbrechen.Text = "Abbrechen";
            // 
            // btnOk
            // 
            btnOk.AutoSize = true;
            btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            btnOk.Location = new System.Drawing.Point(252, 0);
            btnOk.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            btnOk.MinimumSize = new System.Drawing.Size(110, 30);
            btnOk.Name = "btnOk";
            btnOk.Size = new System.Drawing.Size(124, 35);
            btnOk.TabIndex = 1;
            btnOk.Text = "Übernehmen";
            // 
            // Form_BkUebernahme
            // 
            AcceptButton = btnOk;
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            CancelButton = btnAbbrechen;
            ClientSize = new System.Drawing.Size(520, 380);
            Controls.Add(tl);
            Font = new System.Drawing.Font("Segoe UI", 9F);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form_BkUebernahme";
            ShowInTaskbar = false;
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            tl.ResumeLayout(false);
            tl.PerformLayout();
            pnlKnoepfe.ResumeLayout(false);
            pnlKnoepfe.PerformLayout();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tl;
        private System.Windows.Forms.Label lblGegenstand;
        private System.Windows.Forms.Label lblQuelle;
        private System.Windows.Forms.ComboBox cbQuelle;
        private System.Windows.Forms.Label lblWertQuelleTitel;
        private System.Windows.Forms.Label lblQuelleWert;
        private System.Windows.Forms.Label lblZielTitel;
        private System.Windows.Forms.Label lblZiel;
        private System.Windows.Forms.Label lblWertZielTitel;
        private System.Windows.Forms.Label lblZielWert;
        private System.Windows.Forms.Label lblKomponenten;
        private System.Windows.Forms.TextBox txtKlartext;
        private System.Windows.Forms.Label lblGrund;
        private System.Windows.Forms.FlowLayoutPanel pnlKnoepfe;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnAbbrechen;
    }
}
