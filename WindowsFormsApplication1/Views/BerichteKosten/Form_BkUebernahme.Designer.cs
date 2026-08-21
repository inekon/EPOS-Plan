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
            this.tl = new System.Windows.Forms.TableLayoutPanel();
            this.lblGegenstand = new System.Windows.Forms.Label();
            this.lblQuelle = new System.Windows.Forms.Label();
            this.cbQuelle = new System.Windows.Forms.ComboBox();
            this.lblWertQuelleTitel = new System.Windows.Forms.Label();
            this.lblQuelleWert = new System.Windows.Forms.Label();
            this.lblZielTitel = new System.Windows.Forms.Label();
            this.lblZiel = new System.Windows.Forms.Label();
            this.lblWertZielTitel = new System.Windows.Forms.Label();
            this.lblZielWert = new System.Windows.Forms.Label();
            this.lblKomponenten = new System.Windows.Forms.Label();
            this.txtKlartext = new System.Windows.Forms.TextBox();
            this.lblGrund = new System.Windows.Forms.Label();
            this.pnlKnoepfe = new System.Windows.Forms.FlowLayoutPanel();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnAbbrechen = new System.Windows.Forms.Button();
            this.tl.SuspendLayout();
            this.pnlKnoepfe.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblGegenstand
            // 
            this.lblGegenstand.AutoSize = false;
            this.lblGegenstand.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGegenstand.Font = new System.Drawing.Font("Segoe UI Semibold", 10F, System.Drawing.FontStyle.Bold);
            this.lblGegenstand.ForeColor = System.Drawing.Color.FromArgb(0x1F, 0x4E, 0x79);
            this.lblGegenstand.Margin = new System.Windows.Forms.Padding(0, 0, 0, 8);
            this.lblGegenstand.Name = "lblGegenstand";
            // 
            // lblQuelle
            // 
            this.lblQuelle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQuelle.Name = "lblQuelle";
            this.lblQuelle.Text = "Quelle:";
            this.lblQuelle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // cbQuelle
            // 
            this.cbQuelle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbQuelle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbQuelle.Margin = new System.Windows.Forms.Padding(0, 2, 0, 6);
            this.cbQuelle.Name = "cbQuelle";
            this.cbQuelle.SelectedIndexChanged += new System.EventHandler(this.cbQuelle_SelectedIndexChanged);
            // 
            // lblWertQuelleTitel
            // 
            this.lblWertQuelleTitel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWertQuelleTitel.Name = "lblWertQuelleTitel";
            this.lblWertQuelleTitel.Text = "Wert der Quelle:";
            this.lblWertQuelleTitel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblQuelleWert
            // 
            this.lblQuelleWert.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQuelleWert.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.lblQuelleWert.Name = "lblQuelleWert";
            this.lblQuelleWert.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblZielTitel
            // 
            this.lblZielTitel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZielTitel.Name = "lblZielTitel";
            this.lblZielTitel.Text = "Ziel:";
            this.lblZielTitel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblZiel
            // 
            this.lblZiel.AutoEllipsis = true;
            this.lblZiel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZiel.Name = "lblZiel";
            this.lblZiel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblWertZielTitel
            // 
            this.lblWertZielTitel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblWertZielTitel.Name = "lblWertZielTitel";
            this.lblWertZielTitel.Text = "Wert des Ziels:";
            this.lblWertZielTitel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblZielWert
            // 
            this.lblZielWert.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblZielWert.Name = "lblZielWert";
            this.lblZielWert.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblKomponenten
            // 
            this.lblKomponenten.AutoEllipsis = true;
            this.lblKomponenten.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblKomponenten.ForeColor = System.Drawing.Color.DimGray;
            this.lblKomponenten.Name = "lblKomponenten";
            this.lblKomponenten.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // txtKlartext
            // 
            this.txtKlartext.BackColor = System.Drawing.SystemColors.Window;
            this.txtKlartext.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtKlartext.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.txtKlartext.Multiline = true;
            this.txtKlartext.Name = "txtKlartext";
            this.txtKlartext.ReadOnly = true;
            this.txtKlartext.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            // 
            // lblGrund
            // 
            this.lblGrund.AutoEllipsis = true;
            this.lblGrund.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblGrund.ForeColor = System.Drawing.Color.Firebrick;
            this.lblGrund.Name = "lblGrund";
            this.lblGrund.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnOk
            // 
            this.btnOk.AutoSize = true;
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.btnOk.MinimumSize = new System.Drawing.Size(110, 30);
            this.btnOk.Name = "btnOk";
            this.btnOk.Text = "Übernehmen";
            // 
            // btnAbbrechen
            // 
            this.btnAbbrechen.AutoSize = true;
            this.btnAbbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnAbbrechen.Margin = new System.Windows.Forms.Padding(10, 0, 0, 0);
            this.btnAbbrechen.MinimumSize = new System.Drawing.Size(110, 30);
            this.btnAbbrechen.Name = "btnAbbrechen";
            this.btnAbbrechen.Text = "Abbrechen";
            // 
            // pnlKnoepfe
            // 
            this.pnlKnoepfe.Controls.Add(this.btnAbbrechen);
            this.pnlKnoepfe.Controls.Add(this.btnOk);
            this.pnlKnoepfe.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlKnoepfe.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.pnlKnoepfe.Margin = new System.Windows.Forms.Padding(0, 8, 0, 0);
            this.pnlKnoepfe.Name = "pnlKnoepfe";
            // 
            // tl
            // 
            // Raster: Beschriftung links, Wert rechts.
            this.tl.ColumnCount = 2;
            this.tl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 132F));
            this.tl.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tl.Controls.Add(this.lblGegenstand, 0, 0);
            this.tl.SetColumnSpan(this.lblGegenstand, 2);
            this.tl.Controls.Add(this.lblQuelle, 0, 1);
            this.tl.Controls.Add(this.cbQuelle, 1, 1);
            this.tl.Controls.Add(this.lblWertQuelleTitel, 0, 2);
            this.tl.Controls.Add(this.lblQuelleWert, 1, 2);
            this.tl.Controls.Add(this.lblZielTitel, 0, 3);
            this.tl.Controls.Add(this.lblZiel, 1, 3);
            this.tl.Controls.Add(this.lblWertZielTitel, 0, 4);
            this.tl.Controls.Add(this.lblZielWert, 1, 4);
            this.tl.Controls.Add(this.lblKomponenten, 0, 5);
            this.tl.SetColumnSpan(this.lblKomponenten, 2);
            this.tl.Controls.Add(this.txtKlartext, 0, 6);
            this.tl.SetColumnSpan(this.txtKlartext, 2);
            this.tl.Controls.Add(this.lblGrund, 0, 7);
            this.tl.SetColumnSpan(this.lblGrund, 2);
            this.tl.Controls.Add(this.pnlKnoepfe, 0, 8);
            this.tl.SetColumnSpan(this.pnlKnoepfe, 2);
            this.tl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tl.Name = "tl";
            this.tl.Padding = new System.Windows.Forms.Padding(12, 10, 12, 10);
            this.tl.RowCount = 9;
            this.tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));                 // 0 Gegenstand
            this.tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));            // 1 Quelle
            this.tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));            // 2 Quellwert
            this.tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));            // 3 Ziel
            this.tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));            // 4 Zielwert
            this.tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 22F));            // 5 Komponenten
            this.tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));            // 6 Klartext
            this.tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 24F));            // 7 Grund
            this.tl.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 40F));            // 8 Knöpfe
            // 
            // Form_BkUebernahme
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this.btnAbbrechen;
            this.ClientSize = new System.Drawing.Size(520, 380);
            this.Controls.Add(this.tl);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_BkUebernahme";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.tl.ResumeLayout(false);
            this.pnlKnoepfe.ResumeLayout(false);
            this.ResumeLayout(false);

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
