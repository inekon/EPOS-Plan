using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Berichtsseite (Konzept_Berichtserstellung_EPOS-Plan.md, Kap. 3.1):
    /// Variantencheckliste mit Simulationszeitstempeln, Baustein-Checkliste,
    /// Ausgabeformat, Zielordner, „Erstellen".
    ///
    /// <para><b>Herkunft.</b> Der Inhalt stand bis zum Umbau „Berichte &amp; Kosten"
    /// direkt in <see cref="Form_Bericht"/> und ist unverändert hierher gehoben
    /// worden, damit die Seite „Bericht" des Reiters ihn einbetten kann;
    /// <see cref="Form_Bericht"/> ist seither nur noch ein dünner Dialog-Wrapper.
    /// Neu hinzugekommen ist allein der Knopf „Projektvergleich + Bericht (alt)",
    /// der beim Wegfall des Dialogs „Projektvarianten" sonst verloren gegangen wäre.</para>
    ///
    /// Aufbau immer vom Stammprojekt aus; ist eine Variante aktiv, ermittelt der
    /// Aufrufer vorher deren Stamm (VariantenCtrl.StammRefDerVariante).
    /// </summary>
    public class UcBericht : UserControl
    {
        private readonly int _idStamm;
        private readonly string _stammName;

        private readonly BerichtCtrl _bericht = new BerichtCtrl();

        private CancellationTokenSource _cts;
        private bool _initialisiere;       // unterdrückt ItemCheck-Logik beim Befüllen

        // Steuerelemente
        private Label lblVarianten;
        private ListView lvVarianten;
        private ColumnHeader colArt, colBez, colName, colSim;
        private Button btnAlle, btnKeine;
        private Label lblBausteine;
        private CheckedListBox clbBausteine;
        private Label lblRechnen;
        private Label lblAusgabe;
        private RadioButton rbWord, rbExcel, rbBeide;
        private Label lblZiel;
        private TextBox txtZiel;
        private Button btnDurchsuchen;
        private Button btnVergleichAlt;
        private Label lblStatus;
        private ProgressBar progress;
        private Button btnErstellen, btnAbbrechen;

        /// <summary>Stammprojekt-ID der Vergleichsgruppe.</summary>
        public int IdStamm { get { return _idStamm; } }

        /// <summary>
        /// true = das Control sitzt im Dialog-Wrapper <see cref="Form_Bericht"/>;
        /// dann bleibt „Schließen" dauerhaft sichtbar. Eingebettet im Reiter
        /// erscheint der Knopf nur während eines Laufs — als „Abbrechen".
        /// </summary>
        public bool AlsDialog
        {
            get { return _alsDialog; }
            set { _alsDialog = value; if (btnAbbrechen != null) btnAbbrechen.Visible = value || Beschaeftigt; }
        }
        private bool _alsDialog;

        /// <summary>Der Anwender hat „Schließen" gedrückt (nur im Dialog-Wrapper belegt).</summary>
        public event EventHandler SchliessenAngefordert;

        public UcBericht(int idStamm, string stammName)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
            InitializeComponent();
        }

        /// <summary>Titelzeile für den Dialog-Wrapper bzw. die Seitenüberschrift.</summary>
        public string Titel { get { return "Bericht erstellen — Projekt: " + _stammName; } }

        // ------------------------------------------------------------- Aufbau

        private void InitializeComponent()
        {
            this.lblVarianten = new Label();
            this.lvVarianten = new ListView();
            this.colArt = new ColumnHeader();
            this.colBez = new ColumnHeader();
            this.colName = new ColumnHeader();
            this.colSim = new ColumnHeader();
            this.btnAlle = new Button();
            this.btnKeine = new Button();
            this.lblBausteine = new Label();
            this.clbBausteine = new CheckedListBox();
            this.lblRechnen = new Label();
            this.lblAusgabe = new Label();
            this.rbWord = new RadioButton();
            this.rbExcel = new RadioButton();
            this.rbBeide = new RadioButton();
            this.lblZiel = new Label();
            this.txtZiel = new TextBox();
            this.btnDurchsuchen = new Button();
            this.btnVergleichAlt = new Button();
            this.lblStatus = new Label();
            this.progress = new ProgressBar();
            this.btnErstellen = new Button();
            this.btnAbbrechen = new Button();
            this.SuspendLayout();

            // Varianten (links)
            this.lblVarianten.AutoSize = true;
            this.lblVarianten.Location = new Point(12, 12);
            this.lblVarianten.Text = "Varianten (Referenz: Stamm, fest gewählt):";

            this.lvVarianten.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            this.lvVarianten.CheckBoxes = true;
            this.lvVarianten.Columns.AddRange(new ColumnHeader[] { this.colArt, this.colBez, this.colName, this.colSim });
            this.lvVarianten.FullRowSelect = true;
            this.lvVarianten.HideSelection = false;
            this.lvVarianten.Location = new Point(12, 32);
            this.lvVarianten.MultiSelect = false;
            this.lvVarianten.Size = new Size(470, 250);
            this.lvVarianten.View = View.Details;
            this.lvVarianten.ItemCheck += new ItemCheckEventHandler(this.lvVarianten_ItemCheck);

            this.colArt.Text = "Art"; this.colArt.Width = 70;
            this.colBez.Text = "Bezeichner"; this.colBez.Width = 130;
            this.colName.Text = "Projektname"; this.colName.Width = 150;
            this.colSim.Text = "Simulation"; this.colSim.Width = 110;

            this.btnAlle.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnAlle.Location = new Point(12, 288);
            this.btnAlle.Size = new Size(70, 24);
            this.btnAlle.Text = "Alle";
            this.btnAlle.Click += (s, e) => SetzeAlleVarianten(true);

            this.btnKeine.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnKeine.Location = new Point(88, 288);
            this.btnKeine.Size = new Size(70, 24);
            this.btnKeine.Text = "Keine";
            this.btnKeine.Click += (s, e) => SetzeAlleVarianten(false);

            // Bausteine (rechts)
            this.lblBausteine.AutoSize = true;
            this.lblBausteine.Location = new Point(498, 12);
            this.lblBausteine.Text = "Berichtsbausteine:";

            this.clbBausteine.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.clbBausteine.CheckOnClick = true;
            this.clbBausteine.IntegralHeight = false;
            this.clbBausteine.Location = new Point(498, 32);
            this.clbBausteine.Size = new Size(220, 190);
            this.clbBausteine.ItemCheck += new ItemCheckEventHandler(this.clbBausteine_ItemCheck);

            // Rechenhinweis statt Option: Simulation und Wirtschaftlichkeit laufen
            // vor JEDER Ausgabe neu (Nutzeranforderung 15.08.2026) — der frühere
            // Schalter „Vor Ausgabe neu rechnen" entfällt bewusst.
            this.lblRechnen.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            this.lblRechnen.ForeColor = Color.DimGray;
            this.lblRechnen.Location = new Point(498, 228);
            this.lblRechnen.Size = new Size(220, 30);
            this.lblRechnen.Text = "Jeder Bericht rechnet neu: alle gewählten Varianten " +
                                   "werden simuliert und wirtschaftlich bewertet.";

            this.lblAusgabe.AutoSize = true;
            this.lblAusgabe.Location = new Point(498, 260);
            this.lblAusgabe.Text = "Ausgabe:";

            this.rbWord.AutoSize = true;
            this.rbWord.Location = new Point(560, 258);
            this.rbWord.Text = "Word";
            this.rbWord.Checked = true;

            this.rbExcel.AutoSize = true;
            this.rbExcel.Location = new Point(618, 258);
            this.rbExcel.Text = "Excel";

            this.rbBeide.AutoSize = true;
            this.rbBeide.Location = new Point(672, 258);
            this.rbBeide.Text = "Beide";

            // Zielordner
            this.lblZiel.AutoSize = true;
            this.lblZiel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.lblZiel.Location = new Point(12, 324);
            this.lblZiel.Text = "Zielordner:";

            this.txtZiel.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.txtZiel.Location = new Point(85, 321);
            this.txtZiel.Size = new Size(545, 23);

            this.btnDurchsuchen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnDurchsuchen.Location = new Point(636, 320);
            this.btnDurchsuchen.Size = new Size(82, 24);
            this.btnDurchsuchen.Text = "Durchsuchen…";
            this.btnDurchsuchen.Click += new EventHandler(this.btnDurchsuchen_Click);

            // Status + Fortschritt
            this.lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Location = new Point(12, 354);
            this.lblStatus.Size = new Size(540, 18);

            this.progress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.progress.Location = new Point(12, 376);
            this.progress.Size = new Size(540, 16);
            this.progress.Visible = false;

            // Bestandsweg „Projektvergleich + Bericht (alt)" — stand bislang im Dialog
            // „Projektvarianten"; mit dessen Wegfall wandert er auf die Berichtsseite,
            // damit die Funktion nicht verloren geht.
            this.btnVergleichAlt.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.btnVergleichAlt.Location = new Point(12, 398);
            this.btnVergleichAlt.Size = new Size(300, 26);
            this.btnVergleichAlt.Text = MyResource.Resource.BK_BTN_VERGLEICH_ALT;
            this.btnVergleichAlt.Click += new EventHandler(this.btnVergleichAlt_Click);

            // Schaltflächen
            this.btnErstellen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnErstellen.Location = new Point(560, 360);
            this.btnErstellen.Size = new Size(158, 32);
            this.btnErstellen.Text = "Erstellen";
            this.btnErstellen.Click += new EventHandler(this.btnErstellen_Click);

            this.btnAbbrechen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnAbbrechen.Location = new Point(560, 398);
            this.btnAbbrechen.Size = new Size(158, 26);
            this.btnAbbrechen.Text = "Schließen";
            this.btnAbbrechen.Visible = false;   // im Reiter nur während eines Laufs (SetBusy)
            this.btnAbbrechen.Click += new EventHandler(this.btnAbbrechen_Click);

            // Control
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Size = new Size(730, 436);
            this.MinimumSize = new Size(600, 360);
            this.Font = new Font("Segoe UI", 9f);
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

        /// <summary>Umgebendes Formular als Dialog-Besitzer (im Reiter das Startformular).</summary>
        private IWin32Window Besitzer
        {
            get { Form f = this.FindForm(); return f != null ? (IWin32Window)f : this; }
        }

        // ------------------------------------------------------------- Laden

        private bool _geladen;

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            LadeDatenEinmalig();
        }

        /// <summary>
        /// Erstbefüllung — gleichgültig, ob sie der Wrapper (Form.Load) oder das
        /// Erzeugen des Fensterhandles auslöst; der zweite Aufruf ist wirkungslos.
        /// </summary>
        public void LadeDatenEinmalig()
        {
            if (_geladen) return;
            _geladen = true;
            LadeDaten();
        }

        /// <summary>
        /// Liest Konfiguration, Variantenliste und Bausteine neu ein
        /// (früher Form_Bericht_Load; nach jedem Berichtslauf erneut gerufen).
        /// </summary>
        public void LadeDaten()
        {
            if (this.DesignMode) return;
            _initialisiere = true;
            try
            {
                BerichtsKonfiguration konfig = _bericht.Lade(_idStamm);

                // Varianten mit Simulationsstand.
                lvVarianten.Items.Clear();
                foreach (BerichtsDatenSammler.VariantenStatus st in
                         BerichtsDatenSammler.ErmittleStatus(_idStamm, _stammName))
                {
                    var it = new ListViewItem(new[]
                    {
                        st.IstStamm ? "Stamm" : "Variante",
                        st.IstStamm ? "(Stammprojekt)" : st.Variantenname,
                        st.Projektname,
                        st.SimStandText
                    });
                    it.Tag = st;
                    it.Checked = st.IstStamm || konfig.VariantenIds.Contains(st.IdProjekt)
                                 || konfig.VariantenIds.Count == 0;   // Neuzustand: alles an
                    if (!st.SimStand.HasValue || st.Veraltet) it.ForeColor = Color.Firebrick;
                    lvVarianten.Items.Add(it);
                }

                // Bausteine. Wirtschaftlichkeit (Phase 6) ist wählbar; die Zahlen dafür
                // rechnet der Berichtslauf selbst (SammleFuerBericht, Schritt b).
                clbBausteine.Items.Clear();
                foreach (BerichtsKonfiguration.BausteinDef b in BerichtsKonfiguration.AlleBausteine)
                {
                    bool aktiv = konfig.AktiveBausteine.Count > 0
                        ? konfig.IstAktiv(b.Schluessel)
                        : b.Standard;
                    clbBausteine.Items.Add(b.Titel, aktiv);
                }

                rbWord.Checked = konfig.Ausgabe == "Word";
                rbExcel.Checked = konfig.Ausgabe == "Excel";
                rbBeide.Checked = konfig.Ausgabe == "Beide";
                if (!rbWord.Checked && !rbExcel.Checked && !rbBeide.Checked) rbWord.Checked = true;

                txtZiel.Text = string.IsNullOrWhiteSpace(konfig.ZielOrdner)
                    ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                    : konfig.ZielOrdner;

                Melde("");
            }
            finally { _initialisiere = false; }
        }

        // ------------------------------------------------------------- Ereignisse

        // Stammzeile bleibt immer angehakt.
        private void lvVarianten_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_initialisiere) return;
            var st = lvVarianten.Items[e.Index].Tag as BerichtsDatenSammler.VariantenStatus;
            if (st != null && st.IstStamm && e.NewValue != CheckState.Checked)
            {
                e.NewValue = CheckState.Checked;
                Melde("Das Stammprojekt ist die Referenz und immer enthalten.");
            }
        }

        // Hinweis beim Aktivieren der Wirtschaftlichkeit: der Berichtslauf rechnet sie
        // selbst mit — ein vorheriger Besuch der Seite ist nicht nötig.
        private void clbBausteine_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_initialisiere) return;
            int idx = IndexVon(BerichtsKonfiguration.B_WIRTSCHAFT);
            if (e.Index == idx && e.NewValue == CheckState.Checked)
                Melde("Wirtschaftlichkeit: wird für diesen Bericht neu berechnet " +
                      "(Kapitalwertmethode, alle Szenarien) — verlängert den Lauf.");
        }

        private static int IndexVon(string schluessel)
        {
            for (int i = 0; i < BerichtsKonfiguration.AlleBausteine.Length; i++)
                if (BerichtsKonfiguration.AlleBausteine[i].Schluessel == schluessel) return i;
            return -1;
        }

        private void SetzeAlleVarianten(bool an)
        {
            foreach (ListViewItem it in lvVarianten.Items)
            {
                var st = it.Tag as BerichtsDatenSammler.VariantenStatus;
                it.Checked = an || (st != null && st.IstStamm);
            }
        }

        private void btnDurchsuchen_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Zielordner für den Bericht wählen";
                if (Directory.Exists(txtZiel.Text)) dlg.SelectedPath = txtZiel.Text;
                if (dlg.ShowDialog(Besitzer) == DialogResult.OK) txtZiel.Text = dlg.SelectedPath;
            }
        }

        private void btnAbbrechen_Click(object sender, EventArgs e)
        {
            if (_cts != null) { _cts.Cancel(); return; }   // laufenden Vorgang abbrechen
            EventHandler h = SchliessenAngefordert;
            if (h != null) h(this, EventArgs.Empty);
        }

        /// <summary>true, solange ein Berichtslauf aussteht (Wrapper darf dann nicht schließen).</summary>
        public bool Beschaeftigt { get { return _cts != null; } }

        /// <summary>Bricht einen laufenden Berichtslauf ab (Wrapper beim Schließen).</summary>
        public void Abbrechen()
        {
            if (_cts != null) _cts.Cancel();
        }

        // --------------------------------------------- Bestandsweg „Vergleich (alt)"

        /// <summary>
        /// Direktbericht Stamm + angehakte Varianten über <see cref="ProjektvergleichBericht"/>.
        /// Übernommen aus dem entfallenen Dialog „Projektvarianten"; dort war die Gruppe
        /// Stamm + die EINE markierte Variante, hier sind es die in der Liste angehakten
        /// Varianten (dieselbe Auswahl, die auch der reguläre Bericht verwendet).
        /// </summary>
        private void btnVergleichAlt_Click(object sender, EventArgs e)
        {
            var gruppe = new List<ProjektvergleichBericht.Projekt>();
            gruppe.Add(new ProjektvergleichBericht.Projekt
            {
                Id = _idStamm,
                Name = _stammName,
                Bezeichner = "",
                IstStamm = true
            });
            foreach (ListViewItem it in lvVarianten.Items)
            {
                var st = it.Tag as BerichtsDatenSammler.VariantenStatus;
                if (st == null || st.IstStamm || !it.Checked) continue;
                gruppe.Add(new ProjektvergleichBericht.Projekt
                {
                    Id = st.IdProjekt,
                    Name = st.Projektname,
                    Bezeichner = st.Variantenname,
                    IstStamm = false
                });
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Word-Dokument (*.docx)|*.docx";
                sfd.FileName = "Projektvergleich_" + _stammName + ".docx";
                if (sfd.ShowDialog(Besitzer) != DialogResult.OK) return;

                try
                {
                    Cursor = Cursors.WaitCursor;
                    // Der Bericht simuliert die Gruppe selbst neu (Nutzeranforderung
                    // 15.08.2026) und liefert die Meldungen der Läufe zurück.
                    ProjektvergleichBericht bericht = new ProjektvergleichBericht();
                    bericht.Erzeuge(sfd.FileName, gruppe);
                    Melde("Bericht erstellt: " + sfd.FileName);

                    string frage = "Bericht wurde erstellt (alle Projekte neu simuliert).";
                    if (bericht.Laufmeldungen.Count > 0)
                        frage += "\r\n\r\nHinweise:\r\n• " +
                                 string.Join("\r\n• ", bericht.Laufmeldungen);
                    frage += "\r\n\r\nJetzt öffnen?";

                    if (MessageBox.Show(frage, "Projektvergleich",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo(sfd.FileName) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    // Vollstaendige Fehlermeldung inkl. inner exceptions anzeigen (Statuszeile kuerzt ab).
                    string msg = ex.Message;
                    Exception inner = ex.InnerException;
                    while (inner != null) { msg += "\r\n→ " + inner.Message; inner = inner.InnerException; }
                    Melde("Fehler beim Erstellen des Berichts.");
                    MessageBox.Show(msg, "Fehler beim Erstellen des Berichts",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                finally { Cursor = Cursors.Default; }
            }
        }

        // ------------------------------------------------------------- Erstellen

        private BerichtsKonfiguration LeseKonfigurationAusUi()
        {
            var k = new BerichtsKonfiguration();
            foreach (ListViewItem it in lvVarianten.Items)
            {
                var st = it.Tag as BerichtsDatenSammler.VariantenStatus;
                if (st != null && !st.IstStamm && it.Checked) k.VariantenIds.Add(st.IdProjekt);
            }
            for (int i = 0; i < BerichtsKonfiguration.AlleBausteine.Length; i++)
                if (clbBausteine.GetItemChecked(i))
                    k.AktiveBausteine.Add(BerichtsKonfiguration.AlleBausteine[i].Schluessel);
            // NeuRechnen bleibt nur noch für den JSON-Bestand in der DB stehen — der
            // Berichtslauf rechnet grundsätzlich neu (siehe SammleFuerBericht).
            k.NeuRechnen = true;
            k.Ausgabe = rbBeide.Checked ? "Beide" : (rbExcel.Checked ? "Excel" : "Word");
            k.ZielOrdner = txtZiel.Text ?? "";
            return k;
        }

        private async void btnErstellen_Click(object sender, EventArgs e)
        {
            if (_cts != null) return;   // läuft bereits

            BerichtsKonfiguration konfig = LeseKonfigurationAusUi();
            _bericht.Speichere(_idStamm, konfig);   // Auswahl merken (Konzept Kap. 8.4)

            // Kein Schnellpfad mehr: jeder Berichtslauf simuliert alle gewählten
            // Projekte neu und rechnet danach die Wirtschaftlichkeit. Das kostet Zeit,
            // deshalb wird der Aufwand vor dem Start beziffert statt hinterher erklärt.
            int anzahl = 0;
            foreach (ListViewItem it in lvVarianten.Items) if (it.Checked) anzahl++;
            if (MessageBox.Show(
                    "Für diesen Bericht werden " + anzahl + " Projekt(e) neu simuliert und " +
                    "anschließend wirtschaftlich bewertet.\r\n\r\n" +
                    "Je nach Projektgröße dauert das einige Minuten. Fortfahren?",
                    "Bericht erstellen", MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                != DialogResult.Yes)
                return;

            _cts = new CancellationTokenSource();
            SetBusy(true);
            var progressMelder = new Progress<BerichtsDatenSammler.Fortschritt>(f =>
            {
                if (f.Gesamt > 0)
                {
                    progress.Maximum = f.Gesamt;
                    progress.Value = Math.Min(f.Aktuell, f.Gesamt);
                }
                Melde(string.Format("({0}/{1}) {2}", f.Aktuell, f.Gesamt, f.Text));
            });

            try
            {
                CancellationToken ct = _cts.Token;
                // Ganglinien (Word) und Monatswerte (Excel-Detailblätter) brauchen
                // Stundenreihen; die sammelt der Lauf zusätzlich ein, sobald
                // „Ergebnisse je Variante" aktiv ist (Konzept Kap. 6.2/9).
                bool mitZeitreihen = konfig.IstAktiv(BerichtsKonfiguration.B_ERGEBNISSE);

                // Ein Sammel-Einstieg für Word UND Excel: frische Simulation je Projekt,
                // danach die Wirtschaftlichkeitsrechnung derselben Gruppe.
                BerichtsDaten daten = await Task.Run(() =>
                    new BerichtsDatenSammler().SammleFuerBericht(_idStamm, _stammName,
                                                                 konfig.VariantenIds,
                                                                 mitZeitreihen, progressMelder, ct), ct);

                // Word- und/oder Excel-Erzeugung (Konzept Kap. 4/9).
                string wordPfad = null, excelPfad = null;
                if (konfig.Ausgabe == "Word" || konfig.Ausgabe == "Beide")
                {
                    Melde("Erzeuge Word-Bericht…");
                    ct.ThrowIfCancellationRequested();
                    wordPfad = await Task.Run(() => _bericht.ErzeugeWord(daten, konfig), ct);
                }
                if (konfig.Ausgabe == "Excel" || konfig.Ausgabe == "Beide")
                {
                    Melde("Erzeuge Excel-Bericht…");
                    ct.ThrowIfCancellationRequested();
                    excelPfad = await Task.Run(() => _bericht.ErzeugeExcel(daten, konfig), ct);
                }

                string erster = wordPfad ?? excelPfad;
                Melde("Bericht erstellt: " + erster);
                string meldung = "Bericht erstellt:";
                if (wordPfad != null) meldung += "\r\n" + wordPfad;
                if (excelPfad != null) meldung += "\r\n" + excelPfad;
                if (daten.Warnungen.Count > 0)
                    meldung += "\r\n\r\nHinweise:\r\n• " + string.Join("\r\n• ", daten.Warnungen);
                meldung += "\r\n\r\n" + (wordPfad != null && excelPfad != null
                    ? "Word-Bericht jetzt öffnen?" : "Bericht jetzt öffnen?");

                if (MessageBox.Show(meldung, "Bericht erstellen",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    System.Diagnostics.Process.Start(
                        new System.Diagnostics.ProcessStartInfo(erster) { UseShellExecute = true });
                LadeDaten();   // Zeitstempel in der Liste auffrischen
            }
            catch (OperationCanceledException)
            {
                Melde("Vorgang abgebrochen.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei der Berichtserstellung: " + ex.Message, "Fehler",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            progress.Visible = busy;
            if (!busy) progress.Value = 0;
            lvVarianten.Enabled = !busy;
            clbBausteine.Enabled = !busy;
            btnAlle.Enabled = !busy;
            btnKeine.Enabled = !busy;
            rbWord.Enabled = !busy; rbExcel.Enabled = !busy; rbBeide.Enabled = !busy;
            txtZiel.Enabled = !busy;
            btnDurchsuchen.Enabled = !busy;
            btnVergleichAlt.Enabled = !busy;
            btnErstellen.Enabled = !busy;
            // Eingebettet dient der Knopf allein dem Abbrechen; im Dialog bleibt er stehen.
            btnAbbrechen.Visible = AlsDialog || busy;
            btnAbbrechen.Text = busy ? "Abbrechen" : "Schließen";
            this.UseWaitCursor = busy;
        }

        private void Melde(string text)
        { lblStatus.Text = text ?? ""; }
    }
}
