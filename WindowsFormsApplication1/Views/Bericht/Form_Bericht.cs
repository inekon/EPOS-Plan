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
    /// Berichtsdialog (Konzept_Berichtserstellung_EPOS-Plan.md, Kap. 3.1).
    ///
    /// Aufruf immer vom Stammprojekt aus: new Form_Bericht(idStamm, stammName).ShowDialog();
    /// ist eine Variante aktiv, ermittelt der Aufrufer vorher deren Stamm
    /// (VariantenCtrl.StammRefDerVariante).
    ///
    /// Phase 1: Variantencheckliste mit Simulationszeitstempeln, Baustein-Checkliste,
    /// Optionen, DB-gespeicherte Konfiguration, Datensammlung inkl. optionaler
    /// headless Simulation mit Fortschritt/Abbruch. Die Word-/Excel-Erzeugung folgt
    /// in Phase 2/4 — "Erstellen" liefert bis dahin die Zusammenfassung der Datenlage.
    ///
    /// Die Form ist komplett im Code aufgebaut (kein Designer/.resx nötig);
    /// Lokalisierung de/en folgt in Phase 5 über Satelliten-Ressourcen.
    /// </summary>
    public class Form_Bericht : Form
    {
        private readonly int _idStamm;
        private readonly string _stammName;

        private readonly VariantenCtrl _varianten = new VariantenCtrl();
        private readonly BerichtCtrl _bericht = new BerichtCtrl();

        private CancellationTokenSource _cts;
        private bool _initialisiere;       // unterdrückt ItemCheck-Logik beim Befüllen
        private int _idxWirtschaft = -1;   // Index des (gesperrten) Wirtschaftlichkeits-Bausteins

        // Steuerelemente
        private Label lblVarianten;
        private ListView lvVarianten;
        private ColumnHeader colArt, colBez, colName, colSim;
        private Button btnAlle, btnKeine;
        private Label lblBausteine;
        private CheckedListBox clbBausteine;
        private CheckBox chkNeuRechnen;
        private Label lblAusgabe;
        private RadioButton rbWord, rbExcel, rbBeide;
        private Label lblZiel;
        private TextBox txtZiel;
        private Button btnDurchsuchen;
        private Label lblStatus;
        private ProgressBar progress;
        private Button btnErstellen, btnAbbrechen;

        public Form_Bericht(int idStamm, string stammName)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
            InitializeComponent();
        }

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
            this.chkNeuRechnen = new CheckBox();
            this.lblAusgabe = new Label();
            this.rbWord = new RadioButton();
            this.rbExcel = new RadioButton();
            this.rbBeide = new RadioButton();
            this.lblZiel = new Label();
            this.txtZiel = new TextBox();
            this.btnDurchsuchen = new Button();
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

            // Optionen
            this.chkNeuRechnen.AutoSize = true;
            this.chkNeuRechnen.Location = new Point(498, 232);
            this.chkNeuRechnen.Text = "Vor Ausgabe neu rechnen";
            this.chkNeuRechnen.Checked = true;

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
            this.btnAbbrechen.Click += new EventHandler(this.btnAbbrechen_Click);

            // Form
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new Size(730, 436);
            this.MinimumSize = new Size(700, 420);
            this.Font = new Font("Segoe UI", 9f);
            this.StartPosition = FormStartPosition.CenterParent;
            this.Name = "Form_Bericht";
            this.Text = "Bericht erstellen — Projekt: " + _stammName;
            this.Controls.Add(this.lblVarianten);
            this.Controls.Add(this.lvVarianten);
            this.Controls.Add(this.btnAlle);
            this.Controls.Add(this.btnKeine);
            this.Controls.Add(this.lblBausteine);
            this.Controls.Add(this.clbBausteine);
            this.Controls.Add(this.chkNeuRechnen);
            this.Controls.Add(this.lblAusgabe);
            this.Controls.Add(this.rbWord);
            this.Controls.Add(this.rbExcel);
            this.Controls.Add(this.rbBeide);
            this.Controls.Add(this.lblZiel);
            this.Controls.Add(this.txtZiel);
            this.Controls.Add(this.btnDurchsuchen);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.btnErstellen);
            this.Controls.Add(this.btnAbbrechen);
            this.Load += new EventHandler(this.Form_Bericht_Load);
            this.FormClosing += new FormClosingEventHandler(this.Form_Bericht_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        // ------------------------------------------------------------- Laden

        private void Form_Bericht_Load(object sender, EventArgs e)
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

                // Bausteine.
                clbBausteine.Items.Clear();
                _idxWirtschaft = -1;
                foreach (BerichtsKonfiguration.BausteinDef b in BerichtsKonfiguration.AlleBausteine)
                {
                    bool aktiv = konfig.AktiveBausteine.Count > 0
                        ? konfig.IstAktiv(b.Schluessel)
                        : b.Standard;
                    string titel = b.Titel;
                    if (b.Schluessel == BerichtsKonfiguration.B_WIRTSCHAFT)
                    {
                        titel += "  (noch ohne Berechnung)";
                        aktiv = false;
                        _idxWirtschaft = clbBausteine.Items.Count;
                    }
                    clbBausteine.Items.Add(titel, aktiv);
                }

                chkNeuRechnen.Checked = konfig.NeuRechnen;
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

        // Wirtschaftlichkeit bleibt gesperrt, bis der Provider verfügbar ist (Konzept Kap. 7).
        private void clbBausteine_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_initialisiere) return;
            if (e.Index == _idxWirtschaft && e.NewValue == CheckState.Checked)
            {
                e.NewValue = CheckState.Unchecked;
                Melde("Wirtschaftlichkeit: Berechnung noch nicht verfügbar (Konzept_Wirtschaftlichkeit.md).");
            }
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
                if (dlg.ShowDialog(this) == DialogResult.OK) txtZiel.Text = dlg.SelectedPath;
            }
        }

        private void btnAbbrechen_Click(object sender, EventArgs e)
        {
            if (_cts != null) { _cts.Cancel(); return; }   // laufenden Vorgang abbrechen
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Form_Bericht_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_cts != null) { _cts.Cancel(); e.Cancel = true; }  // erst Lauf beenden, dann schließen
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
            k.NeuRechnen = chkNeuRechnen.Checked;
            k.Ausgabe = rbBeide.Checked ? "Beide" : (rbExcel.Checked ? "Excel" : "Word");
            k.ZielOrdner = txtZiel.Text ?? "";
            return k;
        }

        private async void btnErstellen_Click(object sender, EventArgs e)
        {
            if (_cts != null) return;   // läuft bereits

            BerichtsKonfiguration konfig = LeseKonfigurationAusUi();
            _bericht.Speichere(_idStamm, konfig);   // Auswahl merken (Konzept Kap. 8.4)

            // Hinweis auf fehlende Ergebnisse, wenn nicht neu gerechnet werden soll.
            if (!konfig.NeuRechnen)
            {
                var fehlend = new List<string>();
                foreach (ListViewItem it in lvVarianten.Items)
                {
                    var st = it.Tag as BerichtsDatenSammler.VariantenStatus;
                    if (st != null && it.Checked && !st.SimStand.HasValue) fehlend.Add(st.Projektname);
                }
                if (fehlend.Count > 0 && MessageBox.Show(
                        "Für folgende Projekte liegt kein Simulationsergebnis vor — sie werden vor dem " +
                        "Bericht simuliert:\r\n\r\n" + string.Join("\r\n", fehlend) + "\r\n\r\nFortfahren?",
                        "Bericht erstellen", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                    return;
            }

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
                BerichtsDaten daten = await Task.Run(() =>
                    new BerichtsDatenSammler().Sammle(_idStamm, _stammName, konfig.VariantenIds,
                                                      konfig.NeuRechnen, progressMelder, ct), ct);

                // Word-Erzeugung (Phase 2). Excel folgt in Phase 4.
                string wordPfad = null;
                if (konfig.Ausgabe == "Word" || konfig.Ausgabe == "Beide")
                {
                    Melde("Erzeuge Word-Bericht…");
                    ct.ThrowIfCancellationRequested();
                    wordPfad = await Task.Run(() => _bericht.ErzeugeWord(daten, konfig), ct);
                }

                if (wordPfad != null)
                {
                    Melde("Bericht erstellt: " + wordPfad);
                    string meldung = "Bericht erstellt:\r\n" + wordPfad;
                    if (konfig.Ausgabe == "Beide")
                        meldung += "\r\n\r\nHinweis: Die Excel-Ausgabe folgt in Phase 4 des Berichtsmoduls.";
                    if (daten.Warnungen.Count > 0)
                        meldung += "\r\n\r\nHinweise:\r\n• " + string.Join("\r\n• ", daten.Warnungen);
                    meldung += "\r\n\r\nBericht jetzt öffnen?";

                    if (MessageBox.Show(meldung, "Bericht erstellen",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                        System.Diagnostics.Process.Start(
                            new System.Diagnostics.ProcessStartInfo(wordPfad) { UseShellExecute = true });
                }
                else
                {
                    Melde("Datensammlung abgeschlossen.");
                    MessageBox.Show(BaueZusammenfassung(daten, konfig) +
                        "\r\nDie Excel-Ausgabe folgt in Phase 4 des Berichtsmoduls.",
                        "Bericht — Datenlage", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                Form_Bericht_Load(this, EventArgs.Empty);   // Zeitstempel in der Liste auffrischen
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

        private static string BaueZusammenfassung(BerichtsDaten daten, BerichtsKonfiguration konfig)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Datensammlung für den Bericht (" + konfig.Ausgabe + "):");
            sb.AppendLine();
            foreach (VariantenDaten v in daten.Varianten)
            {
                string kopf = (v.IstStamm ? "Stamm    " : "Variante ") + v.Projektname;
                if (v.Fehler != null)
                { sb.AppendLine(kopf + "  →  FEHLER: " + v.Fehler); continue; }

                int belegt = 0;
                foreach (KeyValuePair<string, double?> kv in v.Kennzahlen)
                    if (kv.Value.HasValue) belegt++;

                sb.AppendLine(kopf +
                    "  →  Simulation " + (v.SimulationsStand.HasValue ? v.SimulationsStand.Value.ToString("dd.MM.yyyy HH:mm") : "—") +
                    (v.FrischSimuliert ? " (neu gerechnet)" : "") +
                    ", Kennzahlen: " + belegt + "/" + v.Kennzahlen.Count);
            }
            if (daten.Warnungen.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Hinweise:");
                foreach (string w in daten.Warnungen) sb.AppendLine("• " + w);
            }
            return sb.ToString();
        }

        private void SetBusy(bool busy)
        {
            progress.Visible = busy;
            if (!busy) progress.Value = 0;
            lvVarianten.Enabled = !busy;
            clbBausteine.Enabled = !busy;
            btnAlle.Enabled = !busy;
            btnKeine.Enabled = !busy;
            chkNeuRechnen.Enabled = !busy;
            rbWord.Enabled = !busy; rbExcel.Enabled = !busy; rbBeide.Enabled = !busy;
            txtZiel.Enabled = !busy;
            btnDurchsuchen.Enabled = !busy;
            btnErstellen.Enabled = !busy;
            btnAbbrechen.Text = busy ? "Abbrechen" : "Schließen";
            this.UseWaitCursor = busy;
        }

        private void Melde(string text)
        { lblStatus.Text = text ?? ""; }
    }
}
