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
    /// direkt im Berichtsdialog und ist unverändert hierher gehoben worden, damit
    /// die Seite „Bericht" des Reiters ihn einbetten kann; die Dialoghülle
    /// darum ist mit iU9-W0 entfallen (Anwenderentscheid iF29).
    /// Neu hinzugekommen ist allein der Knopf „Projektvergleich + Bericht (alt)",
    /// der beim Wegfall des Dialogs „Projektvarianten" sonst verloren gegangen wäre.</para>
    ///
    /// Aufbau immer vom Stammprojekt aus; ist eine Variante aktiv, ermittelt der
    /// Aufrufer vorher deren Stamm (VariantenCtrl.StammRefDerVariante).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Die Oberfläche steht in <c>UcBericht.Designer.cs</c>, ohne eigene
    /// <c>.resx</c>: Alle sichtbaren Texte kommen aus <c>MyResource</c> und werden
    /// in <see cref="TexteSetzen"/> gesetzt; im Designer stehen nur Platzhalter.
    /// </para>
    /// <para>
    /// <b>Aufbauhinweise, die vor der Designer-Umstellung als Kommentare im
    /// Aufbaucode standen.</b> Links die Variantenliste mit „Alle"/„Keine", rechts
    /// die Bausteinliste. <c>lblRechnen</c> ist ein Hinweis und keine Option:
    /// Simulation und Wirtschaftlichkeit laufen vor JEDER Ausgabe neu
    /// (Nutzeranforderung 15.08.2026) — der frühere Schalter „Vor Ausgabe neu
    /// rechnen" entfällt bewusst. <c>btnVergleichAlt</c> ist der Bestandsweg
    /// „Projektvergleich + Bericht (alt)"; er stand bislang im Dialog
    /// „Projektvarianten" und ist mit dessen Wegfall auf die Berichtsseite
    /// gewandert, damit die Funktion nicht verloren geht. <c>btnAbbrechen</c>
    /// startet unsichtbar — er erscheint nur während eines Laufs
    /// (<see cref="SetBusy"/>).
    /// </para>
    /// </remarks>
    public partial class UcBericht : UserControl
    {
        private readonly int _idStamm;
        private readonly string _stammName;

        private readonly BerichtCtrl _bericht = new BerichtCtrl();

        private CancellationTokenSource _cts;
        private bool _initialisiere;       // unterdrückt ItemCheck-Logik beim Befüllen

        /// <summary>Stammprojekt-ID der Vergleichsgruppe.</summary>
        public int IdStamm { get { return _idStamm; } }

        /// <summary>Der Anwender hat „Schließen" gedrückt (nur im Dialog-Wrapper belegt).</summary>
        public event EventHandler SchliessenAngefordert;

        public UcBericht(int idStamm, string stammName)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";

            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Vor der Designer-Umstellung stand hier Font
            // OHNE AutoScaleDimensions — der Skalierungsfaktor war damit 1, es fand
            // also ebenfalls keine Umrechnung der fest gerechneten Pixelpositionen
            // statt (die Anwendung läuft DpiUnaware, siehe app.manifest und
            // Program.SetHighDpiMode). None hält genau dieses Verhalten fest und
            // verhindert, dass ein späteres Designer-Speichern die Skalierung über
            // nachgetragene AutoScaleDimensions erstmals scharf schaltet.
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            TexteSetzen();
        }

        /// <summary>Titelzeile für den Dialog-Wrapper bzw. die Seitenüberschrift.</summary>
        public string Titel
        {
            get { return string.Format(MyResource.Resource.BK_BER_TITEL, _stammName); }
        }

        // ==================================================================
        // Texte
        // ==================================================================

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            // --- Varianten (links) ----------------------------------------
            lblVarianten.Text = MyResource.Resource.BK_BER_LBL_VARIANTEN;
            colArt.Text = MyResource.Resource.BK_SP_ART;
            colBez.Text = MyResource.Resource.BK_SP_BEZEICHNER;
            colName.Text = MyResource.Resource.BK_SP_PROJEKTNAME;
            colSim.Text = MyResource.Resource.BK_BER_SP_SIMULATION;
            btnAlle.Text = MyResource.Resource.BK_BER_BTN_ALLE;
            btnKeine.Text = MyResource.Resource.BK_BER_BTN_KEINE;

            // --- Bausteine (rechts) ---------------------------------------
            lblBausteine.Text = MyResource.Resource.BK_BER_LBL_BAUSTEINE;
            lblRechnen.Text = MyResource.Resource.BK_BER_LBL_RECHNEN;

            // --- Ausgabe --------------------------------------------------
            lblAusgabe.Text = MyResource.Resource.BK_BER_LBL_AUSGABE;
            rbWord.Text = MyResource.Resource.BK_BER_RB_WORD;
            rbExcel.Text = MyResource.Resource.BK_BER_RB_EXCEL;
            rbBeide.Text = MyResource.Resource.BK_BER_RB_BEIDE;

            // --- Zielordner -----------------------------------------------
            lblZiel.Text = MyResource.Resource.BK_BER_LBL_ZIEL;
            btnDurchsuchen.Text = MyResource.Resource.BK_BER_BTN_DURCHSUCHEN;

            // --- Schaltflächen --------------------------------------------
            btnVergleichAlt.Text = MyResource.Resource.BK_BTN_VERGLEICH_ALT;
            btnErstellen.Text = MyResource.Resource.BK_BER_BTN_ERSTELLEN;
            btnAbbrechen.Text = MyResource.Resource.BK_BER_BTN_SCHLIESSEN;
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
                        st.IstStamm ? MyResource.Resource.BK_ART_STAMM : MyResource.Resource.BK_ART_VARIANTE,
                        st.IstStamm ? MyResource.Resource.BK_ART_STAMMPROJEKT : st.Variantenname,
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
                Melde(MyResource.Resource.BK_BER_MSG_STAMM_REFERENZ);
            }
        }

        // Hinweis beim Aktivieren der Wirtschaftlichkeit: der Berichtslauf rechnet sie
        // selbst mit — ein vorheriger Besuch der Seite ist nicht nötig.
        private void clbBausteine_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_initialisiere) return;
            int idx = IndexVon(BerichtsKonfiguration.B_WIRTSCHAFT);
            if (e.Index == idx && e.NewValue == CheckState.Checked)
                Melde(MyResource.Resource.BK_BER_MSG_WIRTSCHAFT_HINWEIS);
        }

        private static int IndexVon(string schluessel)
        {
            for (int i = 0; i < BerichtsKonfiguration.AlleBausteine.Length; i++)
                if (BerichtsKonfiguration.AlleBausteine[i].Schluessel == schluessel) return i;
            return -1;
        }

        /// <summary>
        /// „Alle" — vor der Designer-Umstellung ein Lambda an <c>Click</c>; der
        /// Designer verdrahtet ausschließlich Methodenverweise, deshalb steht der
        /// Aufruf jetzt hier.
        /// </summary>
        private void btnAlle_Click(object sender, EventArgs e)
        {
            SetzeAlleVarianten(true);
        }

        /// <summary>„Keine" — wie <see cref="btnAlle_Click"/>, nur umgekehrt.</summary>
        private void btnKeine_Click(object sender, EventArgs e)
        {
            SetzeAlleVarianten(false);
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
            // iU7-9: Ordnerwahl über Dienste.Datei statt über FolderBrowserDialog.
            // Titel und Startordner wie bisher; leer = abgebrochen, dann bleibt das
            // Zielfeld stehen (der Bestandsdialog verhielt sich bei Abbruch genauso).
            string start = Directory.Exists(txtZiel.Text) ? txtZiel.Text : "";
            string gewaehlt = Dienste.Datei.OrdnerWaehlen(
                MyResource.Resource.BK_BER_DLG_ZIELORDNER, start);
            if (!string.IsNullOrEmpty(gewaehlt)) txtZiel.Text = gewaehlt;
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

            // iU7-9: Speicherziel über Dienste.Datei statt über SaveFileDialog.
            // Filter wie bisher; der Dateinamensvorschlag ist ein technischer Wert wie
            // der Namensstamm „_Bericht_" in BerichtCtrl und deshalb bewusst nicht
            // lokalisiert. Leer = abgebrochen.
            string zieldatei = Dienste.Datei.DateiSpeichern(
                null,
                MyResource.Resource.BK_BER_DLG_FILTER_WORD,
                "Projektvergleich_" + _stammName + ".docx");
            if (string.IsNullOrEmpty(zieldatei)) return;

            try
            {
                Cursor = Cursors.WaitCursor;
                // Der Bericht simuliert die Gruppe selbst neu (Nutzeranforderung
                // 15.08.2026) und liefert die Meldungen der Läufe zurück.
                ProjektvergleichBericht bericht = new ProjektvergleichBericht();
                bericht.Erzeuge(zieldatei, gruppe);
                Melde(string.Format(MyResource.Resource.BK_BER_STATUS_ERSTELLT, zieldatei));

                string frage = MyResource.Resource.BK_BER_MSG_VERGLEICH_FERTIG;
                if (bericht.Laufmeldungen.Count > 0)
                    frage += "\r\n\r\n" + MyResource.Resource.BK_BER_MSG_HINWEISE + "\r\n• " +
                             string.Join("\r\n• ", bericht.Laufmeldungen);
                frage += "\r\n\r\n" + MyResource.Resource.BK_BER_FRAGE_OEFFNEN;

                if (MessageBox.Show(frage, MyResource.Resource.BK_BER_TITEL_VERGLEICH,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    Dienste.Datei.MitSystemOeffnen(zieldatei);
            }
            catch (Exception ex)
            {
                // Vollstaendige Fehlermeldung inkl. inner exceptions anzeigen (Statuszeile kuerzt ab).
                string msg = ex.Message;
                Exception inner = ex.InnerException;
                while (inner != null) { msg += "\r\n→ " + inner.Message; inner = inner.InnerException; }
                Melde(MyResource.Resource.BK_BER_STATUS_FEHLER);
                MessageBox.Show(msg, MyResource.Resource.BK_BER_TITEL_FEHLER_VERGLEICH,
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { Cursor = Cursors.Default; }
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
            // „Word" / „Excel" / „Beide" sind Persistenzwerte des Konfigurations-JSON
            // (Tabelle Berichtskonfiguration) und bleiben deutsch und eingefroren;
            // lokalisiert sind nur die Beschriftungen der drei Auswahlknöpfe.
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
                    string.Format(MyResource.Resource.BK_BER_FRAGE_START, anzahl),
                    MyResource.Resource.BK_BER_TITEL_ERSTELLEN,
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question)
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
                    Melde(MyResource.Resource.BK_BER_STATUS_WORD);
                    ct.ThrowIfCancellationRequested();
                    wordPfad = await Task.Run(() => _bericht.ErzeugeWord(daten, konfig), ct);
                }
                if (konfig.Ausgabe == "Excel" || konfig.Ausgabe == "Beide")
                {
                    Melde(MyResource.Resource.BK_BER_STATUS_EXCEL);
                    ct.ThrowIfCancellationRequested();
                    excelPfad = await Task.Run(() => _bericht.ErzeugeExcel(daten, konfig), ct);
                }

                string erster = wordPfad ?? excelPfad;
                Melde(string.Format(MyResource.Resource.BK_BER_STATUS_ERSTELLT, erster));
                string meldung = MyResource.Resource.BK_BER_MSG_ERSTELLT_KOPF;
                if (wordPfad != null) meldung += "\r\n" + wordPfad;
                if (excelPfad != null) meldung += "\r\n" + excelPfad;
                if (daten.Warnungen.Count > 0)
                    meldung += "\r\n\r\n" + MyResource.Resource.BK_BER_MSG_HINWEISE + "\r\n• " +
                               string.Join("\r\n• ", daten.Warnungen);
                meldung += "\r\n\r\n" + (wordPfad != null && excelPfad != null
                    ? MyResource.Resource.BK_BER_FRAGE_OEFFNEN_WORD
                    : MyResource.Resource.BK_BER_FRAGE_OEFFNEN_BERICHT);

                if (MessageBox.Show(meldung, MyResource.Resource.BK_BER_TITEL_ERSTELLEN,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    Dienste.Datei.MitSystemOeffnen(erster);   // iU7-9
                LadeDaten();   // Zeitstempel in der Liste auffrischen
            }
            catch (OperationCanceledException)
            {
                Melde(MyResource.Resource.BK_BER_STATUS_ABGEBROCHEN);
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(MyResource.Resource.BK_BER_MSG_LAUFFEHLER, ex.Message),
                    MyResource.Resource.BK_BER_TITEL_FEHLER,
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
            // Der Knopf dient allein dem Abbrechen; ausserhalb eines Laufs ist er weg.
            btnAbbrechen.Visible = busy;
            btnAbbrechen.Text = busy
                ? MyResource.Resource.BK_BER_BTN_ABBRECHEN
                : MyResource.Resource.BK_BER_BTN_SCHLIESSEN;
            this.UseWaitCursor = busy;
        }

        private void Melde(string text)
        { lblStatus.Text = text ?? ""; }
    }
}
