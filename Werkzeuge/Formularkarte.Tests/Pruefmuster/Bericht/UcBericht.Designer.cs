namespace WindowsFormsApplication1
{
    partial class UcBericht
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
        // (BK_BER_*, BK_SP_*, BK_BTN_VERGLEICH_ALT) statt der Feldnamen. Zur
        // Laufzeit überschreibt TexteSetzen() sie unverändert; die Literale
        // hier dienen allein dem Entwurfsbild und der Maßprüfung.
        //
        // Mit den Echttexten nachgemessen (Segoe UI 9 pt, 96 dpi, DpiUnaware)
        // und angepasst:
        //
        //  - lblRechnen: der Hinweissatz (97 Zeichen) bricht bei 220 px auf
        //    drei Zeilen und braucht 53 px; die Höhe stand auf 30 → jetzt 56.
        //    Der Platz kommt aus clbBausteine (190 → 163); die Ausgabezeile
        //    rückt entsprechend nach.
        //  - Ausgabe-Zeile: „Ausgabe:" (55) + Word/Excel/Beide (53/50/53) plus
        //    Mindestabstände brauchen 229 px, die rechte Spalte ist 220 px
        //    breit — rbBeide stand mit rechter Kante 725 über dem 12-px-Rand
        //    (718). Die drei Auswahlknöpfe stehen deshalb jetzt in einer
        //    eigenen Zeile unter der Beschriftung (rechte Kante 667).
        //  - lblBausteine, lblAusgabe, rbWord/rbExcel/rbBeide hatten keinen
        //    Anker (Top|Left). Da clbBausteine und lblRechnen Top|Right
        //    verankert sind, zerfiel die rechte Spalte beim Verbreitern und
        //    ragte beim Verkleinern (MinimumSize 600) über den rechten Rand.
        //    Alle fünf sind jetzt Top|Right verankert wie ihre Nachbarn.
        //  - btnDurchsuchen: „Durchsuchen…" braucht 101 px, der Knopf war
        //    82 px breit (abgeschnitten) → 110 px bei gleichbleibender rechter
        //    Kante 718; txtZiel wird dafür von 545 auf 517 px gekürzt (6 px
        //    Abstand).
        //  - lvVarianten: Spaltensumme 460 px bei 453 px Client-Breite (mit
        //    senkrechter Bildlaufleiste) erzwang eine waagerechte Leiste;
        //    colBez 130 → 124 und colName 150 → 144 (Summe 448).
        //  - Fußknöpfe einheitlich 30 px hoch (btnErstellen 32 → 30,
        //    btnAbbrechen und btnVergleichAlt 26 → 30), rechte Kante von
        //    btnDurchsuchen/btnErstellen/btnAbbrechen konstant 718, senkrechter
        //    Abstand btnErstellen ↔ btnAbbrechen 10 px. Die Fußzeile wurde
        //    dafür innerhalb der unveränderten Größe 730 × 436 neu aufgeteilt
        //    (lblStatus 354 → 350, progress 376 → 374 und 16 → 14 px hoch,
        //    Knopfzeile 398 → 394); unterer Rand bleibt 12 px.
        //
        // Nicht geändert: MinimumSize 600 × 360. Sie ist für diesen Inhalt zu
        // klein — die unten verankerte Zielordner-/Fußzeile schiebt sich dann
        // in die rechte Spalte. Eine Korrektur müsste die frühere Dialoghülle
        // (MinimumSize 700 × 420) mitziehen und ist deshalb hier offen.
        // ÜBERHOLT durch die Nacharbeit vom 21.08.2026, siehe unten.
        //
        // ------------------------------------------------------------------
        // Nacharbeit 21.08.2026 — MinimumSize 600 × 360 -> 730 × 436
        // ------------------------------------------------------------------
        // Die Mindestgröße steht jetzt auf dem ENTWURFSMASS. Grund ist der
        // oben offen gelassene Befund: Die Fußzeile (lblZiel/txtZiel/
        // btnDurchsuchen, lblStatus, progress, btnErstellen/btnAbbrechen) ist
        // Bottom|Right verankert, die rechte Spalte (clbBausteine, lblRechnen,
        // Ausgabe-Auswahl) Top|Right. Unterhalb von 730 × 436 wandern beide
        // Gruppen aufeinander zu und überlagern sich — nachgemessen bei
        // 670 × 470: btnErstellen rückt von x = 560 auf 500 und liegt damit
        // unter der Bausteinliste.
        //
        // Wirkung in den beiden Wirten:
        //  - Form_Bericht (Dialog-Hülle) zieht mit: Die Hülle setzt ihre
        //    MinimumSize seit derselben Nacharbeit aus der eigenen Rahmen-
        //    differenz, die CLIENT-Fläche kann dort also nicht mehr unter
        //    730 × 436 fallen (vorher 684 × 381).
        //  - UcBerichteKosten bettet dieses Steuerelement mit Dock = Fill in
        //    pnlInhalt. Docking respektiert MinimumSize: Ist die Fläche
        //    kleiner, behält die Seite ihr Maß und wird rechts/unten
        //    abgeschnitten, statt ihr Innenraster zu verlieren. Das ist
        //    Bestandsverhalten (pnlInhalt.AutoScroll greift bei einem
        //    Dock = Fill-Kind nicht) und tritt erst unterhalb von 938 px
        //    Wirtsbreite auf (730 + NAV_BREITE 208) — im Entwurf ist
        //    UcBerichteKosten 1265 px breit, pnlInhalt also 1057 px.

        #region Vom Komponenten-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.lblVarianten = new System.Windows.Forms.Label();
            this.lvVarianten = new System.Windows.Forms.ListView();
            this.colArt = new System.Windows.Forms.ColumnHeader();
            this.colBez = new System.Windows.Forms.ColumnHeader();
            this.colName = new System.Windows.Forms.ColumnHeader();
            this.colSim = new System.Windows.Forms.ColumnHeader();
            this.btnAlle = new System.Windows.Forms.Button();
            this.btnKeine = new System.Windows.Forms.Button();
            this.lblBausteine = new System.Windows.Forms.Label();
            this.clbBausteine = new System.Windows.Forms.CheckedListBox();
            this.lblRechnen = new System.Windows.Forms.Label();
            this.lblAusgabe = new System.Windows.Forms.Label();
            this.rbWord = new System.Windows.Forms.RadioButton();
            this.rbExcel = new System.Windows.Forms.RadioButton();
            this.rbBeide = new System.Windows.Forms.RadioButton();
            this.lblZiel = new System.Windows.Forms.Label();
            this.txtZiel = new System.Windows.Forms.TextBox();
            this.btnDurchsuchen = new System.Windows.Forms.Button();
            this.btnVergleichAlt = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.progress = new System.Windows.Forms.ProgressBar();
            this.btnErstellen = new System.Windows.Forms.Button();
            this.btnAbbrechen = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblVarianten
            //
            this.lblVarianten.AutoSize = true;
            this.lblVarianten.Location = new System.Drawing.Point(12, 12);
            this.lblVarianten.Name = "lblVarianten";
            this.lblVarianten.Text = "Varianten (Referenz: Stamm, fest gewählt):";
            //
            // lvVarianten
            //
            this.lvVarianten.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lvVarianten.CheckBoxes = true;
            this.lvVarianten.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colArt,
            this.colBez,
            this.colName,
            this.colSim});
            this.lvVarianten.FullRowSelect = true;
            this.lvVarianten.HideSelection = false;
            this.lvVarianten.Location = new System.Drawing.Point(12, 32);
            this.lvVarianten.MultiSelect = false;
            this.lvVarianten.Name = "lvVarianten";
            this.lvVarianten.Size = new System.Drawing.Size(470, 250);
            this.lvVarianten.View = System.Windows.Forms.View.Details;
            this.lvVarianten.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.lvVarianten_ItemCheck);
            //
            // colArt
            //
            this.colArt.Name = "colArt";
            this.colArt.Text = "Art";
            this.colArt.Width = 70;
            //
            // colBez
            //
            this.colBez.Name = "colBez";
            this.colBez.Text = "Bezeichner";
            this.colBez.Width = 124;
            //
            // colName
            //
            this.colName.Name = "colName";
            this.colName.Text = "Projektname";
            this.colName.Width = 144;
            //
            // colSim
            //
            this.colSim.Name = "colSim";
            this.colSim.Text = "Simulation";
            this.colSim.Width = 110;
            //
            // btnAlle
            //
            this.btnAlle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAlle.Location = new System.Drawing.Point(12, 288);
            this.btnAlle.Name = "btnAlle";
            this.btnAlle.Size = new System.Drawing.Size(70, 24);
            this.btnAlle.Text = "Alle";
            this.btnAlle.Click += new System.EventHandler(this.btnAlle_Click);
            //
            // btnKeine
            //
            this.btnKeine.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnKeine.Location = new System.Drawing.Point(88, 288);
            this.btnKeine.Name = "btnKeine";
            this.btnKeine.Size = new System.Drawing.Size(70, 24);
            this.btnKeine.Text = "Keine";
            this.btnKeine.Click += new System.EventHandler(this.btnKeine_Click);
            //
            // lblBausteine
            //
            this.lblBausteine.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblBausteine.AutoSize = true;
            this.lblBausteine.Location = new System.Drawing.Point(498, 12);
            this.lblBausteine.Name = "lblBausteine";
            this.lblBausteine.Text = "Berichtsbausteine:";
            //
            // clbBausteine
            //
            this.clbBausteine.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.clbBausteine.CheckOnClick = true;
            this.clbBausteine.IntegralHeight = false;
            this.clbBausteine.Location = new System.Drawing.Point(498, 32);
            this.clbBausteine.Name = "clbBausteine";
            this.clbBausteine.Size = new System.Drawing.Size(220, 163);
            this.clbBausteine.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbBausteine_ItemCheck);
            //
            // lblRechnen
            //
            this.lblRechnen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblRechnen.ForeColor = System.Drawing.Color.DimGray;
            this.lblRechnen.Location = new System.Drawing.Point(498, 201);
            this.lblRechnen.Name = "lblRechnen";
            this.lblRechnen.Size = new System.Drawing.Size(220, 56);
            this.lblRechnen.Text = "Jeder Bericht rechnet neu: alle gewählten Varianten werden simuliert und wirtschaf" +
                "tlich bewertet.";
            //
            // lblAusgabe
            //
            this.lblAusgabe.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblAusgabe.AutoSize = true;
            this.lblAusgabe.Location = new System.Drawing.Point(498, 263);
            this.lblAusgabe.Name = "lblAusgabe";
            this.lblAusgabe.Text = "Ausgabe:";
            //
            // rbWord
            //
            this.rbWord.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rbWord.AutoSize = true;
            this.rbWord.Checked = true;
            this.rbWord.Location = new System.Drawing.Point(498, 290);
            this.rbWord.Name = "rbWord";
            this.rbWord.Text = "Word";
            //
            // rbExcel
            //
            this.rbExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rbExcel.AutoSize = true;
            this.rbExcel.Location = new System.Drawing.Point(558, 290);
            this.rbExcel.Name = "rbExcel";
            this.rbExcel.Text = "Excel";
            //
            // rbBeide
            //
            this.rbBeide.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.rbBeide.AutoSize = true;
            this.rbBeide.Location = new System.Drawing.Point(614, 290);
            this.rbBeide.Name = "rbBeide";
            this.rbBeide.Text = "Beide";
            //
            // lblZiel
            //
            this.lblZiel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblZiel.AutoSize = true;
            this.lblZiel.Location = new System.Drawing.Point(12, 324);
            this.lblZiel.Name = "lblZiel";
            this.lblZiel.Text = "Zielordner:";
            //
            // txtZiel
            //
            this.txtZiel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtZiel.Location = new System.Drawing.Point(85, 321);
            this.txtZiel.Name = "txtZiel";
            this.txtZiel.Size = new System.Drawing.Size(517, 23);
            //
            // btnDurchsuchen
            //
            this.btnDurchsuchen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDurchsuchen.Location = new System.Drawing.Point(608, 320);
            this.btnDurchsuchen.Name = "btnDurchsuchen";
            this.btnDurchsuchen.Size = new System.Drawing.Size(110, 24);
            this.btnDurchsuchen.Text = "Durchsuchen…";
            this.btnDurchsuchen.Click += new System.EventHandler(this.btnDurchsuchen_Click);
            //
            // btnVergleichAlt
            //
            this.btnVergleichAlt.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnVergleichAlt.Location = new System.Drawing.Point(12, 394);
            this.btnVergleichAlt.Name = "btnVergleichAlt";
            this.btnVergleichAlt.Size = new System.Drawing.Size(300, 30);
            this.btnVergleichAlt.Text = "Projektvergleich + Bericht (alt)";
            this.btnVergleichAlt.Click += new System.EventHandler(this.btnVergleichAlt_Click);
            //
            // lblStatus
            //
            this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblStatus.ForeColor = System.Drawing.Color.DimGray;
            this.lblStatus.Location = new System.Drawing.Point(12, 350);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(540, 18);
            //
            // progress
            //
            this.progress.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progress.Location = new System.Drawing.Point(12, 374);
            this.progress.Name = "progress";
            this.progress.Size = new System.Drawing.Size(540, 14);
            this.progress.Visible = false;
            //
            // btnErstellen
            //
            this.btnErstellen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnErstellen.Location = new System.Drawing.Point(560, 354);
            this.btnErstellen.Name = "btnErstellen";
            this.btnErstellen.Size = new System.Drawing.Size(158, 30);
            this.btnErstellen.Text = "Erstellen";
            this.btnErstellen.Click += new System.EventHandler(this.btnErstellen_Click);
            //
            // btnAbbrechen
            //
            this.btnAbbrechen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAbbrechen.Location = new System.Drawing.Point(560, 394);
            this.btnAbbrechen.Name = "btnAbbrechen";
            this.btnAbbrechen.Size = new System.Drawing.Size(158, 30);
            this.btnAbbrechen.Text = "Schließen";
            this.btnAbbrechen.Visible = false;
            this.btnAbbrechen.Click += new System.EventHandler(this.btnAbbrechen_Click);
            //
            // UcBericht
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Size = new System.Drawing.Size(730, 436);
            this.MinimumSize = new System.Drawing.Size(730, 436);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            this.Name = "UcBericht";
            this.Controls.Add(this.lblVarianten);
            this.Controls.Add(this.lvVarianten);
            this.Controls.Add(this.btnAlle);
            this.Controls.Add(this.btnKeine);
            this.Controls.Add(this.lblBausteine);
            this.Controls.Add(this.clbBausteine);
            this.Controls.Add(this.lblRechnen);
            this.Controls.Add(this.lblAusgabe);
            this.Controls.Add(this.rbWord);
            this.Controls.Add(this.rbExcel);
            this.Controls.Add(this.rbBeide);
            this.Controls.Add(this.lblZiel);
            this.Controls.Add(this.txtZiel);
            this.Controls.Add(this.btnDurchsuchen);
            this.Controls.Add(this.btnVergleichAlt);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.btnErstellen);
            this.Controls.Add(this.btnAbbrechen);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lblVarianten;
        private System.Windows.Forms.ListView lvVarianten;
        private System.Windows.Forms.ColumnHeader colArt;
        private System.Windows.Forms.ColumnHeader colBez;
        private System.Windows.Forms.ColumnHeader colName;
        private System.Windows.Forms.ColumnHeader colSim;
        private System.Windows.Forms.Button btnAlle;
        private System.Windows.Forms.Button btnKeine;
        private System.Windows.Forms.Label lblBausteine;
        private System.Windows.Forms.CheckedListBox clbBausteine;
        private System.Windows.Forms.Label lblRechnen;
        private System.Windows.Forms.Label lblAusgabe;
        private System.Windows.Forms.RadioButton rbWord;
        private System.Windows.Forms.RadioButton rbExcel;
        private System.Windows.Forms.RadioButton rbBeide;
        private System.Windows.Forms.Label lblZiel;
        private System.Windows.Forms.TextBox txtZiel;
        private System.Windows.Forms.Button btnDurchsuchen;
        private System.Windows.Forms.Button btnVergleichAlt;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.ProgressBar progress;
        private System.Windows.Forms.Button btnErstellen;
        private System.Windows.Forms.Button btnAbbrechen;
    }
}
