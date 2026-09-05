using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Berichte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Berichtsseite (iU9-W5.2/W5.6) — Nachfolge von
    /// <c>Views/Bericht/UcBericht.cs</c> (508 Z.).
    ///
    /// <para><b>Was hier liegt.</b> Konfiguration lesen und schreiben
    /// (<see cref="BerichtCtrl"/>), der Variantenstatus
    /// (<see cref="BerichtsDatenSammler.ErmittleStatus"/>), der Berichtslauf
    /// selbst (Sammeln, Word, Excel) und der Bestandsweg
    /// „Projektvergleich + Bericht (alt)". Dazu die Ordnerwahl und das Öffnen
    /// einer erzeugten Datei über <c>Dienste.Datei</c> (iU7-9). Die Komponente
    /// <see cref="BerichtSeite"/> zeigt nur an.</para>
    ///
    /// <para><b>Die langen Läufe laufen auf einem eigenen Faden</b>
    /// (<c>Task.Run</c>, Muster <c>KapitalwertVerlaufHuelle</c>): Jeder
    /// Berichtslauf simuliert alle gewählten Projekte neu
    /// (Nutzeranforderung 15.08.2026) und rechnet danach die
    /// Wirtschaftlichkeit.</para>
    ///
    /// <para><b>Die drei Persistenzwerte „Word", „Excel" und „Beide"</b>
    /// (Tabelle <c>Berichtskonfiguration</c>) bleiben deutsch und eingefroren;
    /// die Komponente rechnet mit der Nummer 0/1/2 (Drei-Schichten-Regel).</para>
    /// </summary>
    internal sealed class BerichtSeiteGaben
    {
        private readonly int _idStamm;
        private readonly string _stammName;
        private readonly BerichtCtrl _bericht = new BerichtCtrl();

        private CancellationTokenSource _cts;

        internal BerichtSeiteGaben(int idStamm, string stammName)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
        }

        /// <summary>Läuft gerade ein Bericht? (Der Wirt darf dann nicht schließen.)</summary>
        internal bool Beschaeftigt { get { return _cts != null; } }

        /// <summary>Der Parametersatz der Seite.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Laden"] = new Func<BerichtStand>(Laden),
                ["Erstellen"] = new Func<BerichtAuftrag, Action<Laufschritt>, Task<LaufErgebnis>>(Erstellen),
                ["VergleichAlt"] = new Func<BerichtAuftrag, Task<LaufErgebnis>>(VergleichAlt),
                ["Abbrechen"] = new Action(Abbrechen),
                ["OrdnerWaehler"] = new Func<string, Task<string>>(OrdnerWaehlen),
                ["DateiOeffnen"] = new Func<string, Task>(DateiOeffnen),

                ["TitelText"] = string.Format(MyResource.Resource.BK_BER_TITEL, _stammName),
                ["LabelVarianten"] = MyResource.Resource.BK_BER_LBL_VARIANTEN,
                ["LabelBausteine"] = MyResource.Resource.BK_BER_LBL_BAUSTEINE,
                ["LabelRechnen"] = MyResource.Resource.BK_BER_LBL_RECHNEN,
                ["LabelAusgabe"] = MyResource.Resource.BK_BER_LBL_AUSGABE,
                ["LabelZiel"] = MyResource.Resource.BK_BER_LBL_ZIEL,
                ["LabelFortschritt"] = Text("BKS_LBL_FORTSCHRITT", "Fortschritt"),
                ["SpalteArt"] = MyResource.Resource.BK_SP_ART,
                ["SpalteBezeichner"] = MyResource.Resource.BK_SP_BEZEICHNER,
                ["SpalteProjektname"] = MyResource.Resource.BK_SP_PROJEKTNAME,
                ["SpalteSimulation"] = MyResource.Resource.BK_BER_SP_SIMULATION,
                ["AlleText"] = MyResource.Resource.BK_BER_BTN_ALLE,
                ["KeineText"] = MyResource.Resource.BK_BER_BTN_KEINE,
                ["WordText"] = MyResource.Resource.BK_BER_RB_WORD,
                ["ExcelText"] = MyResource.Resource.BK_BER_RB_EXCEL,
                ["BeideText"] = MyResource.Resource.BK_BER_RB_BEIDE,
                ["DurchsuchenText"] = MyResource.Resource.BK_BER_BTN_DURCHSUCHEN,
                ["ErstellenText"] = MyResource.Resource.BK_BER_BTN_ERSTELLEN,
                ["VergleichAltText"] = MyResource.Resource.BK_BTN_VERGLEICH_ALT,
                ["AbbrechenText"] = MyResource.Resource.BK_BER_BTN_ABBRECHEN,
                ["JaText"] = Text("BKS_BTN_JA", "Ja"),
                ["NeinText"] = Text("BKS_BTN_NEIN", "Nein"),
                ["MeldungStammReferenz"] = MyResource.Resource.BK_BER_MSG_STAMM_REFERENZ,
                ["MeldungWirtschaftHinweis"] = MyResource.Resource.BK_BER_MSG_WIRTSCHAFT_HINWEIS,
                ["BausteinWirtschaft"] = BerichtsKonfiguration.B_WIRTSCHAFT,
                ["FrageStart"] = MyResource.Resource.BK_BER_FRAGE_START,
                ["TitelErstellen"] = MyResource.Resource.BK_BER_TITEL_ERSTELLEN,
                ["TitelVergleich"] = MyResource.Resource.BK_BER_TITEL_VERGLEICH,
                ["StatusAbgebrochen"] = MyResource.Resource.BK_BER_STATUS_ABGEBROCHEN,
                ["HilfeSchluessel"] = "UcBericht.btn_Help"
            };
        }

        // =====================================================================
        // Laden (Vorbild UcBericht.LadeDaten)
        // =====================================================================

        private BerichtStand Laden()
        {
            var stand = new BerichtStand();
            BerichtsKonfiguration konfig;
            try { konfig = _bericht.Lade(_idStamm); }
            catch { konfig = new BerichtsKonfiguration(); }

            var zeilen = new List<VarianteZeile>();
            var gewaehlt = new List<int>();
            try
            {
                foreach (BerichtsDatenSammler.VariantenStatus st in
                         BerichtsDatenSammler.ErmittleStatus(_idStamm, _stammName))
                {
                    zeilen.Add(new VarianteZeile
                    {
                        IdProjekt = st.IdProjekt,
                        Art = st.IstStamm ? MyResource.Resource.BK_ART_STAMM
                                          : MyResource.Resource.BK_ART_VARIANTE,
                        Bezeichner = st.IstStamm ? MyResource.Resource.BK_ART_STAMMPROJEKT
                                                 : st.Variantenname,
                        Projektname = st.Projektname,
                        SimStand = st.SimStandText,
                        IstStamm = st.IstStamm,
                        Auffaellig = !st.SimStand.HasValue || st.Veraltet
                    });

                    // Neuzustand: alles an — wortgleich zum Vorlaeufer.
                    if (st.IstStamm || konfig.VariantenIds.Contains(st.IdProjekt)
                        || konfig.VariantenIds.Count == 0)
                        gewaehlt.Add(st.IdProjekt);
                }
            }
            catch { }
            stand.Varianten = zeilen;
            stand.GewaehlteVarianten = gewaehlt;

            var bausteine = new List<BausteinZeile>();
            var aktiv = new List<string>();
            foreach (BerichtsKonfiguration.BausteinDef b in BerichtsKonfiguration.AlleBausteine)
            {
                bausteine.Add(new BausteinZeile { Schluessel = b.Schluessel, Titel = b.Titel });
                bool an = konfig.AktiveBausteine.Count > 0 ? konfig.IstAktiv(b.Schluessel) : b.Standard;
                if (an) aktiv.Add(b.Schluessel);
            }
            stand.Bausteine = bausteine;
            stand.AktiveBausteine = aktiv;

            stand.AusgabeId = AusgabeNummer(konfig.Ausgabe);
            stand.Zielordner = string.IsNullOrWhiteSpace(konfig.ZielOrdner)
                ? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                : konfig.ZielOrdner;

            return stand;
        }

        // =====================================================================
        // Erstellen (Vorbild btnErstellen_Click)
        // =====================================================================

        private async Task<LaufErgebnis> Erstellen(BerichtAuftrag auftrag, Action<Laufschritt> melder)
        {
            if (_cts != null) return new LaufErgebnis { Abgebrochen = true };

            BerichtsKonfiguration konfig = AusAuftrag(auftrag);
            try { _bericht.Speichere(_idStamm, konfig); } catch { }   // Auswahl merken (Kap. 8.4)

            _cts = new CancellationTokenSource();
            var melde = new Progress<BerichtsDatenSammler.Fortschritt>(
                f => melder(new Laufschritt(f.Aktuell, f.Gesamt, f.Text)));

            try
            {
                CancellationToken ct = _cts.Token;

                // Ganglinien (Word) und Monatswerte (Excel) brauchen Stundenreihen;
                // die sammelt der Lauf zusaetzlich ein (Konzept Kap. 6.2/9).
                bool mitZeitreihen = konfig.IstAktiv(BerichtsKonfiguration.B_ERGEBNISSE);

                BerichtsDaten daten = await Task.Run(() =>
                    new BerichtsDatenSammler().SammleFuerBericht(_idStamm, _stammName,
                                                                 konfig.VariantenIds,
                                                                 mitZeitreihen, melde, ct), ct);

                string wordPfad = null, excelPfad = null;
                if (konfig.Ausgabe == AUSGABE_WORD || konfig.Ausgabe == AUSGABE_BEIDE)
                {
                    melder(new Laufschritt(0, 0, MyResource.Resource.BK_BER_STATUS_WORD));
                    ct.ThrowIfCancellationRequested();
                    wordPfad = await Task.Run(() => _bericht.ErzeugeWord(daten, konfig), ct);
                }
                if (konfig.Ausgabe == AUSGABE_EXCEL || konfig.Ausgabe == AUSGABE_BEIDE)
                {
                    melder(new Laufschritt(0, 0, MyResource.Resource.BK_BER_STATUS_EXCEL));
                    ct.ThrowIfCancellationRequested();
                    excelPfad = await Task.Run(() => _bericht.ErzeugeExcel(daten, konfig), ct);
                }

                string erster = wordPfad ?? excelPfad;
                string meldung = MyResource.Resource.BK_BER_MSG_ERSTELLT_KOPF;
                if (wordPfad != null) meldung += "\r\n" + wordPfad;
                if (excelPfad != null) meldung += "\r\n" + excelPfad;
                if (daten.Warnungen.Count > 0)
                    meldung += "\r\n\r\n" + MyResource.Resource.BK_BER_MSG_HINWEISE + "\r\n• " +
                               string.Join("\r\n• ", daten.Warnungen);

                return new LaufErgebnis
                {
                    Erfolg = true,
                    Statuszeile = string.Format(MyResource.Resource.BK_BER_STATUS_ERSTELLT, erster),
                    Meldung = meldung,
                    Frage = wordPfad != null && excelPfad != null
                        ? MyResource.Resource.BK_BER_FRAGE_OEFFNEN_WORD
                        : MyResource.Resource.BK_BER_FRAGE_OEFFNEN_BERICHT,
                    Datei = erster ?? ""
                };
            }
            catch (OperationCanceledException)
            {
                return new LaufErgebnis { Abgebrochen = true };
            }
            catch (Exception ex)
            {
                return new LaufErgebnis
                {
                    Fehler = string.Format(MyResource.Resource.BK_BER_MSG_LAUFFEHLER, ex.Message)
                };
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        // =====================================================================
        // Bestandsweg „Projektvergleich + Bericht (alt)"
        // =====================================================================

        private async Task<LaufErgebnis> VergleichAlt(BerichtAuftrag auftrag)
        {
            var gruppe = new List<ProjektvergleichBericht.Projekt>();
            gruppe.Add(new ProjektvergleichBericht.Projekt
            {
                Id = _idStamm,
                Name = _stammName,
                Bezeichner = "",
                IstStamm = true
            });

            var namen = new Dictionary<int, VarianteZeile>();
            foreach (VarianteZeile z in Laden().Varianten) namen[z.IdProjekt] = z;
            foreach (int id in auftrag.VariantenIds)
            {
                VarianteZeile z;
                if (!namen.TryGetValue(id, out z)) continue;
                gruppe.Add(new ProjektvergleichBericht.Projekt
                {
                    Id = z.IdProjekt,
                    Name = z.Projektname,
                    Bezeichner = z.Bezeichner,
                    IstStamm = false
                });
            }

            // iU7-9: Speicherziel ueber Dienste.Datei statt SaveFileDialog. Der
            // Dateinamensvorschlag ist ein technischer Wert und deshalb bewusst
            // nicht lokalisiert. Leer = abgebrochen.
            string zieldatei = Dienste.Datei.DateiSpeichern(
                null,
                MyResource.Resource.BK_BER_DLG_FILTER_WORD,
                "Projektvergleich_" + _stammName + ".docx");
            if (string.IsNullOrEmpty(zieldatei))
                return new LaufErgebnis { Abgebrochen = true };

            try
            {
                var bericht = new ProjektvergleichBericht();
                await Task.Run(() => bericht.Erzeuge(zieldatei, gruppe));

                string meldung = MyResource.Resource.BK_BER_MSG_VERGLEICH_FERTIG;
                if (bericht.Laufmeldungen.Count > 0)
                    meldung += "\r\n\r\n" + MyResource.Resource.BK_BER_MSG_HINWEISE + "\r\n• " +
                               string.Join("\r\n• ", bericht.Laufmeldungen);

                return new LaufErgebnis
                {
                    Erfolg = true,
                    Statuszeile = string.Format(MyResource.Resource.BK_BER_STATUS_ERSTELLT, zieldatei),
                    Meldung = meldung,
                    Frage = MyResource.Resource.BK_BER_FRAGE_OEFFNEN,
                    Datei = zieldatei
                };
            }
            catch (Exception ex)
            {
                // Vollstaendige Fehlermeldung inkl. inner exceptions (die
                // Statuszeile kuerzt ab) — wortgleich zum Vorlaeufer.
                string msg = ex.Message;
                Exception inner = ex.InnerException;
                while (inner != null) { msg += "\r\n→ " + inner.Message; inner = inner.InnerException; }

                return new LaufErgebnis
                {
                    Statuszeile = MyResource.Resource.BK_BER_STATUS_FEHLER,
                    Fehler = msg
                };
            }
        }

        // =====================================================================
        // Umgebung
        // =====================================================================

        private void Abbrechen()
        {
            if (_cts != null) _cts.Cancel();
        }

        /// <summary>
        /// iU7-9: Ordnerwahl über <c>Dienste.Datei</c> statt über
        /// <c>FolderBrowserDialog</c>. Der Filter wird nicht ausgewertet — die
        /// Komponente reicht ihn nur durch.
        /// </summary>
        private Task<string> OrdnerWaehlen(string filter)
        {
            string start = "";
            try
            {
                string vorher = Laden().Zielordner;
                if (Directory.Exists(vorher)) start = vorher;
            }
            catch { }

            // Der Ordnerwaehler ist ein modales SYSTEMFENSTER und darf nicht
            // synchron im WebView-Rueckruf aufgehen (Hausregel (d), Befund W13-B-1);
            // die …Async-Form fuehrt ihn eine gepostete Nachricht spaeter hoch.
            return Dienste.Datei.OrdnerWaehlenAsync(
                MyResource.Resource.BK_BER_DLG_ZIELORDNER, start);
        }

        private Task DateiOeffnen(string pfad)
        {
            try { Dienste.Datei.MitSystemOeffnen(pfad); } catch { }
            return Task.CompletedTask;
        }

        // =====================================================================
        // Ausgabeformat — die Persistenzwerte kennt NUR diese Hülle
        // =====================================================================

        private const string AUSGABE_WORD = "Word";
        private const string AUSGABE_EXCEL = "Excel";
        private const string AUSGABE_BEIDE = "Beide";

        private static int AusgabeNummer(string persistenz)
        {
            if (string.Equals(persistenz, AUSGABE_EXCEL, StringComparison.Ordinal)) return 1;
            if (string.Equals(persistenz, AUSGABE_BEIDE, StringComparison.Ordinal)) return 2;
            return 0;
        }

        private static string AusgabeWert(int nummer)
        {
            return nummer == 2 ? AUSGABE_BEIDE : (nummer == 1 ? AUSGABE_EXCEL : AUSGABE_WORD);
        }

        private static BerichtsKonfiguration AusAuftrag(BerichtAuftrag a)
        {
            var k = new BerichtsKonfiguration();
            foreach (int id in a.VariantenIds) k.VariantenIds.Add(id);
            foreach (string s in a.Bausteine) k.AktiveBausteine.Add(s);

            // NeuRechnen bleibt nur noch fuer den JSON-Bestand stehen — der
            // Berichtslauf rechnet grundsaetzlich neu (SammleFuerBericht).
            k.NeuRechnen = true;
            k.Ausgabe = AusgabeWert(a.AusgabeId);
            k.ZielOrdner = a.Zielordner ?? "";
            return k;
        }

        private static string Text(string schluessel, string rueckfall)
        {
            try
            {
                string t = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(t) ? rueckfall : t;
            }
            catch { return rueckfall; }
        }
    }
}
