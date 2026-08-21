namespace WindowsFormsApplication1
{
    partial class Form_KiEinstellungen
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
        // (KI_EINST_*, KI_AKT_WEGB_EINSTELLUNG) statt der Feldnamen; die
        // Formatzeichenfolgen mit {0} stehen wörtlich da. Zur Laufzeit
        // überschreiben TexteSetzen() und WerteUebernehmen() sie unverändert —
        // die Literale hier dienen allein dem Entwurfsbild und der Maßprüfung.
        //
        // Mit den Echttexten nachgemessen (Segoe UI 9 pt, 96 dpi, DpiUnaware)
        // und angepasst:
        //
        //  - _hinweis: Der Text besteht aus DREI Absätzen (Modellzeile,
        //    Datenschutz, Kontingent), die HinweisSetzen() mit Leerzeilen
        //    verbindet. Bei 470 px Breite ergibt das 133 px, in der Fassung
        //    nach „Modell neu erkennen" (längere Modellzeile) 149 px. Der
        //    Bereich war 88 px hoch, der Text also um gut die Hälfte
        //    abgeschnitten → 154 px.
        //  - _modellNeu: „Modell neu erkennen" braucht 131 px, der Knopf war
        //    94 px breit (abgeschnitten) → 140 px; er rückt von x = 390 auf
        //    x = 344, damit die rechte Kante wie bisher mit dem Eingabefeld
        //    _schluessel bei 484 abschließt. Abstand zu _limitWert
        //    („{0} (fest vorgegeben)", 118 px breit) 26 px.
        //  - _limitLabel („Tageslimit je Arbeitsplatz:", 144 px) endet bei
        //    x = 158 und lässt bis _limitWert (x = 200) 42 px — unverändert.
        //  - Fußknöpfe: 80/84 × 23 → einheitlich 110 × 30, Abstand
        //    zueinander 10 px, rechte Kante konstant 484 (OK links,
        //    Abbrechen rechts — Windows-Reihenfolge unverändert).
        //  - _wegB und die Knopfzeile rücken um die gewonnene Hinweishöhe
        //    nach unten (208 → 280 bzw. 236 → 314); ClientSize wächst dafür
        //    von 500 × 276 auf 500 × 358 (unterer Rand 14 px).
        //  - Fenstertitel: Platzhalter „Form_KiEinstellungen" → KI_EINST_TITEL.

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this._schluesselLabel = new System.Windows.Forms.Label();
            this._schluessel = new System.Windows.Forms.TextBox();
            this._limitLabel = new System.Windows.Forms.Label();
            this._limitWert = new System.Windows.Forms.Label();
            this._modellNeu = new System.Windows.Forms.Button();
            this._hinweis = new System.Windows.Forms.Label();
            this._wegB = new System.Windows.Forms.CheckBox();
            this._ok = new System.Windows.Forms.Button();
            this._abbrechen = new System.Windows.Forms.Button();
            this._tip = new System.Windows.Forms.ToolTip(this.components);
            this.SuspendLayout();
            //
            // _schluesselLabel
            //
            this._schluesselLabel.AutoSize = true;
            this._schluesselLabel.Location = new System.Drawing.Point(14, 18);
            this._schluesselLabel.Name = "_schluesselLabel";
            this._schluesselLabel.Text = "API-Schlüssel (Google AI Studio):";
            //
            // _schluessel
            //
            this._schluessel.Location = new System.Drawing.Point(14, 42);
            this._schluessel.Name = "_schluessel";
            this._schluessel.UseSystemPasswordChar = true;
            this._schluessel.Width = 470;
            //
            // _limitLabel
            //
            this._limitLabel.AutoSize = true;
            this._limitLabel.Location = new System.Drawing.Point(14, 82);
            this._limitLabel.Name = "_limitLabel";
            this._limitLabel.Text = "Tageslimit je Arbeitsplatz:";
            //
            // _limitWert
            //
            this._limitWert.AutoSize = true;
            this._limitWert.ForeColor = System.Drawing.Color.DimGray;
            this._limitWert.Location = new System.Drawing.Point(200, 82);
            this._limitWert.Name = "_limitWert";
            this._limitWert.Text = "{0} (fest vorgegeben)";
            this._tip.SetToolTip(this._limitWert, "Fest im Programm hinterlegt und nicht änderbar - weder hier noch über eine Einstel" +
                "lung. Eine Änderung erfordert einen neuen Programmstand.");
            //
            // _modellNeu
            //
            this._modellNeu.Location = new System.Drawing.Point(344, 78);
            this._modellNeu.Name = "_modellNeu";
            this._modellNeu.Size = new System.Drawing.Size(140, 24);
            this._modellNeu.Text = "Modell neu erkennen";
            this._modellNeu.Click += new System.EventHandler(this.ModellNeu_Click);
            //
            // _hinweis
            //
            this._hinweis.AutoSize = false;
            this._hinweis.ForeColor = System.Drawing.Color.DimGray;
            this._hinweis.Location = new System.Drawing.Point(14, 118);
            this._hinweis.Name = "_hinweis";
            this._hinweis.Size = new System.Drawing.Size(470, 154);
            this._hinweis.Text = "Modell: {0} (kostengünstige Klasse).\r\n\r\nEs werden ausschließlich Hilfetexte, Ihre " +
                "Frage und der Bereichsname übertragen - keine Projekt-, Kunden- oder Simulationsda" +
                "ten.\r\n\r\nHinweis: Im kostenlosen Kontingent verwendet der Anbieter die Inhalte zur " +
                "Produktverbesserung. Für den produktiven Einsatz einen kostenpflichtigen Zugang nu" +
                "tzen.";
            //
            // _wegB
            //
            this._wegB.AutoSize = true;
            this._wegB.Location = new System.Drawing.Point(14, 280);
            this._wegB.Name = "_wegB";
            this._wegB.Text = "Rückfallweg B erzwingen (Modell ohne Werkzeuge)";
            //
            // _ok
            //
            this._ok.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._ok.Location = new System.Drawing.Point(254, 314);
            this._ok.Name = "_ok";
            this._ok.Size = new System.Drawing.Size(110, 30);
            this._ok.Text = "OK";
            //
            // _abbrechen
            //
            this._abbrechen.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._abbrechen.Location = new System.Drawing.Point(374, 314);
            this._abbrechen.Name = "_abbrechen";
            this._abbrechen.Size = new System.Drawing.Size(110, 30);
            this._abbrechen.Text = "Abbrechen";
            //
            // Form_KiEinstellungen
            //
            this.AcceptButton = this._ok;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.CancelButton = this._abbrechen;
            this.ClientSize = new System.Drawing.Size(500, 358);
            this.Controls.Add(this._schluesselLabel);
            this.Controls.Add(this._schluessel);
            this.Controls.Add(this._limitLabel);
            this.Controls.Add(this._limitWert);
            this.Controls.Add(this._modellNeu);
            this.Controls.Add(this._hinweis);
            this.Controls.Add(this._wegB);
            this.Controls.Add(this._ok);
            this.Controls.Add(this._abbrechen);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Form_KiEinstellungen";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "KI-Assistent - Einstellungen";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label _schluesselLabel;
        private System.Windows.Forms.TextBox _schluessel;
        private System.Windows.Forms.Label _limitLabel;
        private System.Windows.Forms.Label _limitWert;
        private System.Windows.Forms.Button _modellNeu;
        private System.Windows.Forms.Label _hinweis;
        private System.Windows.Forms.CheckBox _wegB;
        private System.Windows.Forms.Button _ok;
        private System.Windows.Forms.Button _abbrechen;
        private System.Windows.Forms.ToolTip _tip;
    }
}
