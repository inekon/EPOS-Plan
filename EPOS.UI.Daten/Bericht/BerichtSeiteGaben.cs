using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Berichte;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Berichtsseite (iU9-W5.2/W5.6) — Nachfolge von
    /// <c>Views/Bericht/UcBericht.cs</c> (508 Z.).
    ///
    /// <para><b>Was hier liegt.</b> Konfiguration lesen und schreiben
    /// (<see cref="BerichtCtrl"/>), der Variantenstatus
    /// (<see cref="BerichtsDatenSammler.ErmittleStatus"/>) und der Berichtslauf
    /// selbst (Sammeln, Word, Excel). Dazu die Ordnerwahl und das Öffnen
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
    ///
    /// <para><b>BV-E1 (Konzept Berichtsvorlagen 10.2): die Word-Vorlage.</b> Die Gruppe „Vorlage"
    /// der Seite belegt <see cref="BerichtsvorlagenGaben"/>. Der Lauf prüft VOR dem Sammeln vor
    /// (<see cref="BerichtCtrl.PruefeVorStart"/> — der Befund, den die Seite vor der Startrückfrage
    /// geholt hat, samt seinen Bytes), füllt nach dem Sammeln genau diese Bytes
    /// (<see cref="BerichtCtrl.ErzeugeWord(BerichtsDaten, BerichtsKonfiguration, Startbefund, Startweg)"/>)
    /// und schreibt die Laufmeldung (<see cref="BerichtCtrl.Laufmeldung"/>) in die Meldung der
    /// Seite. Die Wahl der Vorlage ist die Abweichung des Stammprojekts in seiner Konfiguration;
    /// der Lauf nimmt sie aus der gespeicherten Konfiguration mit, auch wenn er die übrige Auswahl
    /// aus dem Auftrag bildet.</para>
    ///
    /// <para><b>BV-E2 (Konzept 10.2, „Häkchen (BV-Q1 c)"; 13 „Bausteintitel nach MyResource"):</b> Die
    /// Häkchen tragen ihren Titel aus <c>MyResource</c> über den Kern (<see cref="BerichtsKonfiguration.BausteinDef.TitelIn"/>)
    /// und sagen, ob die Excel-Mappe sie führt (<see cref="BausteinZeile.InExcel"/>); welche die
    /// gewählte Vorlage führt, liefert der Kapitelstand der Gruppe (<see cref="BerichtsvorlagenGaben.Kapitel(Pruefbefund)"/>).</para>
    /// </summary>
    internal sealed class BerichtSeiteGaben
    {
        private readonly int _idStamm;
        private readonly string _stammName;
        private readonly BerichtsvorlagenCtrl _vorlagenCtrl;
        private readonly BerichtCtrl _bericht;
        private readonly BerichtsvorlagenGaben _vorlagen;

        /// <summary>
        /// KONZEPT § 2.15 (VG‑Q4): Die geteilte Vergleichswahl trägt auch die SICHT der
        /// Ergebnisansicht — der Bericht folgt ihr, so wie er den Häkchen folgt. Die
        /// Rahmenhülle setzt dieselbe Instanz, die die Wirtschaftlichkeitsseite führt;
        /// ohne sie gilt Sicht 1.
        /// </summary>
        internal Vergleichsauswahl Vergleich { get; set; } = new Vergleichsauswahl();

        private CancellationTokenSource _cts;

        /// <param name="idStamm">Das Stammprojekt der Vergleichsgruppe.</param>
        /// <param name="stammName">Sein Name (Titel, Dateiname).</param>
        /// <param name="vorlagen">Der Vorlagen-Controller; <c>null</c> = mit den Diensten der Plattform
        /// (ein Prüfstand reicht eigene Pfade und Einstellungen herein).</param>
        /// <param name="wege">Die Wege der Plattform um eine Vorlagendatei; <c>null</c> =
        /// <see cref="Berichtsvorlagenwege.Plattform"/>.</param>
        internal BerichtSeiteGaben(int idStamm, string stammName, BerichtsvorlagenCtrl vorlagen = null,
                                   Berichtsvorlagenwege wege = null)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
            _vorlagenCtrl = vorlagen ?? new BerichtsvorlagenCtrl();
            _bericht = new BerichtCtrl(_vorlagenCtrl);
            _vorlagen = new BerichtsvorlagenGaben(idStamm, _bericht, _vorlagenCtrl, wege, Sichtnummer);
        }

        /// <summary>Läuft gerade ein Bericht? (Der Wirt darf dann nicht schließen.)</summary>
        internal bool Beschaeftigt { get { return _cts != null; } }

        /// <summary>Die Hülle der Gruppe „Vorlage" (BV-E1) — dieselbe Instanz für Seite und Lauf.</summary>
        internal BerichtsvorlagenGaben Vorlagen { get { return _vorlagen; } }

        /// <summary>
        /// Das Sammeln der Berichtsdaten — im Betrieb <see cref="BerichtsDatenSammler.SammleFuerBericht(int, string, List{int}, Berichtsbedarf, IProgress{BerichtsDatenSammler.Fortschritt}, CancellationToken, Vergleichssicht)"/>;
        /// ein Prüfstand setzt einen Satz ohne Simulation ein (Konfiguration, Bedarf, Fortschritt,
        /// Abbruch, Sicht).
        /// </summary>
        internal Func<BerichtsKonfiguration, Berichtsbedarf, IProgress<BerichtsDatenSammler.Fortschritt>, CancellationToken,
                      Vergleichssicht, BerichtsDaten> Sammler { get; set; }

        /// <summary>Der Parametersatz der Seite.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            var gaben = new Dictionary<string, object>
            {
                ["Laden"] = new Func<BerichtStand>(Laden),
                ["Erstellen"] = new Func<BerichtAuftrag, Action<Laufschritt>, Task<LaufErgebnis>>(Erstellen),
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
                ["SpalteSpeicher"] = MyResource.Resource.WIRT_ZEILE_SPEICHER,
                ["SpalteSimulation"] = MyResource.Resource.BK_BER_SP_SIMULATION,
                ["AlleText"] = MyResource.Resource.BK_BER_BTN_ALLE,
                ["KeineText"] = MyResource.Resource.BK_BER_BTN_KEINE,
                ["WordText"] = MyResource.Resource.BK_BER_RB_WORD,
                ["ExcelText"] = MyResource.Resource.BK_BER_RB_EXCEL,
                ["BeideText"] = MyResource.Resource.BK_BER_RB_BEIDE,
                ["DurchsuchenText"] = MyResource.Resource.BK_BER_BTN_DURCHSUCHEN,
                ["ErstellenText"] = MyResource.Resource.BK_BER_BTN_ERSTELLEN,
                ["AbbrechenText"] = MyResource.Resource.BK_BER_BTN_ABBRECHEN,
                ["JaText"] = Text("BKS_BTN_JA", "Ja"),
                ["NeinText"] = Text("BKS_BTN_NEIN", "Nein"),
                ["MeldungStammReferenz"] = MyResource.Resource.BK_BER_MSG_STAMM_REFERENZ,
                ["MeldungWirtschaftHinweis"] = MyResource.Resource.BK_BER_MSG_WIRTSCHAFT_HINWEIS,
                ["BausteinWirtschaft"] = BerichtsKonfiguration.B_WIRTSCHAFT,
                ["FrageStart"] = MyResource.Resource.BK_BER_FRAGE_START,
                ["TitelErstellen"] = MyResource.Resource.BK_BER_TITEL_ERSTELLEN,
                ["StatusAbgebrochen"] = MyResource.Resource.BK_BER_STATUS_ABGEBROCHEN,
                ["HilfeSchluessel"] = "UcBericht.btn_Help"
            };

            // BV-E1: die Gruppe „Vorlage" — Liste, Wahl, Menü, Prüfzeile, Überlagerungen, Rückfrage.
            _vorlagen.Belegen(gaben);
            return gaben;
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
                        // AUFTRAG US-2: Womit rechnet diese Version ihren Stromspeicher?
                        // Der Text kommt aus der EINEN Kernmethode, die auch der
                        // Simulationsreiter und die Wirtschaftlichkeit nehmen (#320).
                        Speicher = SpeicherAnzeigeCtrl.SpeicherKontextText(st.IdProjekt),
                        SimStand = st.SimStandText,
                        IstStamm = st.IstStamm,
                        Auffaellig = !st.SimStand.HasValue || st.Veraltet,
                        Veraltet = st.Veraltet
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
                // BV-E2: der Titel in der Sprache der Oberfläche aus dem Kern (BK_BER_BAUSTEIN_*, Rückfall
                // der deutsche Titel des Katalogs) und ob die Excel-Mappe den Baustein führt (NurWord = nein).
                bausteine.Add(new BausteinZeile { Schluessel = b.Schluessel, Titel = b.TitelIn(BerichtTexte.Englisch), InExcel = !b.NurWord });
                bool an = konfig.AktiveBausteine.Count > 0 ? konfig.IstAktiv(b.Schluessel) : b.Standard;
                if (an) aktiv.Add(b.Schluessel);
            }
            stand.Bausteine = bausteine;
            stand.AktiveBausteine = aktiv;

            stand.AusgabeId = AusgabeNummer(konfig.Ausgabe);
            // E3/8: Der Vorgabeordner kommt ueber Dienste.Pfade statt ueber
            // Environment.SpecialFolder - das ist Windows und in EPOS.UI.Daten
            // verboten (Waechter SimulationAnsichtQuelleTests). Unter Windows
            // antwortet WindowsPfade denselben Ordner "Dokumente"; auf iOS den
            // Sandkasten-Ordner Documents, in den dort geschrieben werden darf.
            stand.Zielordner = string.IsNullOrWhiteSpace(konfig.ZielOrdner)
                ? Dienste.Pfade.Dokumente
                : konfig.ZielOrdner;

            return stand;
        }

        // =====================================================================
        // Erstellen (Vorbild btnErstellen_Click)
        // =====================================================================

        /// <summary>Der Lauf der Berichtsseite: Er merkt sich die Auswahl als
        /// Konfiguration der Gruppe (Kap. 8.4).</summary>
        private Task<LaufErgebnis> Erstellen(BerichtAuftrag auftrag, Action<Laufschritt> melder)
        {
            return Erstellen(auftrag, melder, true);
        }

        /// <param name="auswahlMerken"><c>true</c> = die Auswahl des Auftrags wird die
        /// gespeicherte Konfiguration der Gruppe (Berichtsseite); <c>false</c> = der Lauf
        /// nimmt sie nur für sich (<see cref="ErzeugeFuerVergleich"/>).</param>
        /// <param name="erzwingtWirtschaftlichkeit">Zweiter Einstieg (Wirtschaftlichkeitsseite): Die
        /// Vorprüfung fragt, ob die Vorlage Platzhalter der Wirtschaftlichkeit führt (Konzept 10.2).</param>
        private async Task<LaufErgebnis> Erstellen(BerichtAuftrag auftrag, Action<Laufschritt> melder,
                                                   bool auswahlMerken, bool erzwingtWirtschaftlichkeit = false)
        {
            if (_cts != null) return new LaufErgebnis { Abgebrochen = true };

            // BV-E1: Die Vorlagenwahl ist die Abweichung des Stammprojekts. Sie steht in der
            // GESPEICHERTEN Konfiguration, nicht im Auftrag, und geht mit - in den Lauf und ins
            // Merken; sonst löschte jeder Lauf der Berichtsseite die Wahl.
            BerichtsKonfiguration konfig = AusAuftrag(auftrag);
            VorlagenwahlUebernehmen(konfig, Lade());
            if (auswahlMerken)
                try { _bericht.Speichere(_idStamm, konfig); } catch { }   // Auswahl merken (Kap. 8.4)

            // BV-E1 (Konzept 6.8, 10.2): die Vorprüfung VOR dem Sammeln. Der Befund, den die Seite
            // vor ihrer Startrückfrage geholt hat, gilt samt seinen Bytes, wenn er zu diesem Lauf
            // passt; sonst prüft der Lauf frisch. Der Weg ist die Antwort der erweiterten
            // Rückfrage - ohne Antwort die gewählte Vorlage.
            bool englisch = BerichtTexte.Englisch;
            bool mitWord = konfig.Ausgabe == AUSGABE_WORD || konfig.Ausgabe == AUSGABE_BEIDE;
            bool mitExcel = konfig.Ausgabe == AUSGABE_EXCEL || konfig.Ausgabe == AUSGABE_BEIDE;
            Startbefund start = null;
            Startweg weg = Startweg.Gewaehlt;
            IReadOnlyList<string> ungefragt = Array.Empty<string>();
            if (mitWord)
            {
                try { start = _vorlagen.StartFuerLauf(konfig, englisch, Sichtnummer(), erzwingtWirtschaftlichkeit); }
                catch (Exception) { start = null; }   // der Lauf wählt und liest dann selbst (ErzeugeWordLauf)
                weg = Weg(auftrag.Vorlagenweg, start, erzwingtWirtschaftlichkeit, out ungefragt);
            }

            // BV-E7-3: der Befund der Excel-Vorlage aus derselben Vorprüfung und die Antwort der Rückfrage für die Mappe.
            Excelstartbefund excelStart = null;
            bool excelOhneVorlage = false;
            if (mitExcel)
            {
                try { excelStart = _vorlagen.ExcelStartFuerLauf(konfig, englisch, Sichtnummer()); }
                catch (Exception) { excelStart = null; }   // der Lauf wählt und liest dann selbst (ErzeugeExcelLauf)
                excelOhneVorlage = WegExcel(auftrag.Vorlagenweg, excelStart, out IReadOnlyList<string> excelUngefragt);
                if (excelUngefragt.Count > 0) ungefragt = System.Linq.Enumerable.ToList(System.Linq.Enumerable.Concat(ungefragt, excelUngefragt));
                if (mitWord && weg == Startweg.Standard && excelStart?.BrauchtRueckfrage == true
                    && start?.BrauchtRueckfrage != true && !erzwingtWirtschaftlichkeit)
                    weg = Startweg.Gewaehlt;   // die Rückfrage galt allein der Excel-Vorlage — Word bleibt bei seiner
            }

            _cts = new CancellationTokenSource();
            var melde = new Progress<BerichtsDatenSammler.Fortschritt>(
                f => melder(new Laufschritt(f.Aktuell, f.Gesamt, f.Text)));

            try
            {
                CancellationToken ct = _cts.Token;

                // BV-E3 (Konzept Berichtsvorlagen 5.1, 8.5): Was der Lauf über Simulation und
                // Wirtschaftlichkeit hinaus erhebt — Stundenreihen, Verlauf, Emissionsbilanz —,
                // sagt die Vorlage (Startbefund.Bedarf) samt der Mappe, die den Häkchen folgt;
                // ohne Startbefund gilt die Vorgabe der Häkchen. Die Stundenreihen, die die
                // Kostenrechnung braucht (Leistungspreis, SP-W1/LS-E-2), ergänzt der Sammler.
                Berichtsbedarf bedarf = Berichtsbedarf.FuerLauf(konfig, start, weg, mitExcel);

                // E3/8: Der Arbeitsfaden entsteht ueber Kulturweitergabe.Starten
                // statt ueber ein nacktes Task.Run - in EPOS.UI.Daten gilt der
                // Waechter ParallelitaetWacheTests, und ein Bericht, der mitten
                // im Lauf die Sprache wechselte, traegt zwei Zahlenbilder.
                // KONZEPT § 2.9 und § 2.15 (VG-Q4): DER BERICHT FOLGT DER SICHT - so wie
                // er den Haekchen folgt. Sicht 2 druckt A | B mit A als Referenz und der
                // Deklarationszeile; Sicht 1 alle Staende gegen die Referenz der Gruppe.
                // Beides ist eine SITZUNGSWAHL und wandert als Momentaufnahme mit.
                //
                // ETAPPE E5 (Empfehlung Q6, 22.09.2026): Die Sicht geht VOR dem Sammeln
                // hinein. Bis hierher setzte diese Huelle sie erst danach - gerechnet war
                // dann gegen die Referenz der Gruppe, waehrend die Tafeln A nannten. Jetzt
                // rechnet der Sammler in Sicht 2 gegen A (ohne zu speichern) und bucht die
                // Gruppenrechnung wie bisher; die Gruppenreferenz setzt er selbst.
                Vergleichssicht sicht = Vergleich.Sicht.Kopie();
                Func<BerichtsKonfiguration, Berichtsbedarf, IProgress<BerichtsDatenSammler.Fortschritt>, CancellationToken,
                     Vergleichssicht, BerichtsDaten> sammler = Sammler;
                BerichtsDaten daten = await Kulturweitergabe.Starten(() => sammler != null
                    ? sammler(konfig, bedarf, melde, ct, sicht)
                    : new BerichtsDatenSammler().SammleFuerBericht(_idStamm, _stammName,
                                                                   konfig.VariantenIds,
                                                                   bedarf, melde, ct, sicht), ct);

                // BV-E1: Word aus DENSELBEN Bytes, die die Vorprüfung gelesen hat, auf dem Weg der
                // Rückfrage; der Lauf sagt, woraus der Bericht entstand (Laufmeldung).
                string wordPfad = null, excelPfad = null;
                Berichtslauf lauf = null, excelLauf = null;
                if (mitWord)
                {
                    melder(new Laufschritt(0, 0, MyResource.Resource.BK_BER_STATUS_WORD));
                    ct.ThrowIfCancellationRequested();
                    lauf = await Kulturweitergabe.Starten(
                        () => _bericht.ErzeugeWord(daten, konfig, start, weg), ct);
                    wordPfad = lauf.Pfad;
                }
                if (mitExcel)
                {
                    melder(new Laufschritt(0, 0, MyResource.Resource.BK_BER_STATUS_EXCEL));
                    ct.ThrowIfCancellationRequested();
                    // BV-E7: die Mappe aus der Excel-Vorlage des Stammprojekts (ohne Vorlage wie bisher).
                    excelLauf = await Kulturweitergabe.Starten(
                        () => _bericht.ErzeugeExcelLauf(daten, konfig, excelStart, excelOhneVorlage), ct);
                    excelPfad = excelLauf.Pfad;
                }

                string erster = wordPfad ?? excelPfad;
                string meldung = Meldung(wordPfad, excelPfad, lauf, ungefragt, daten.Warnungen, englisch, excelLauf);
                var dateien = new List<string>();
                if (wordPfad != null) dateien.Add(wordPfad);
                if (excelPfad != null) dateien.Add(excelPfad);
                Gliedere(daten, lauf, excelLauf, ungefragt, englisch,
                         out IReadOnlyList<Laufhinweisgruppe> warnungen, out IReadOnlyList<Laufhinweisgruppe> hinweise);

                return new LaufErgebnis
                {
                    Erfolg = true,
                    Statuszeile = string.Format(MyResource.Resource.BK_BER_STATUS_ERSTELLT, erster),
                    Meldung = meldung,
                    Dateien = dateien,
                    Vorlage = lauf?.VorlageName
                              ?? (excelLauf != null && !excelLauf.IstRueckfall ? excelLauf.VorlageName : "") ?? "",
                    VorlageGrund = lauf != null ? Grund(lauf.Herkunft)
                                 : excelLauf != null && !excelLauf.IstRueckfall ? Grund(excelLauf.Herkunft) : null,
                    Warnungen = warnungen,
                    Hinweise = hinweise,
                    // Die Berichtsseite fragt nicht mehr „öffnen?" — ihre Erfolgszeile trägt „Öffnen";
                    // die Wirtschaftlichkeitsseite (zweiter Einstieg) behält ihre Rückfrage.
                    Frage = !erzwingtWirtschaftlichkeit ? ""
                        : wordPfad != null && excelPfad != null
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

        /// <summary>
        /// ETAPPE E5 (U44, Entscheid Q18): <b>„Bericht erzeugen" auf der
        /// Wirtschaftlichkeitsseite</b> — DERSELBE Berichtsweg wie der Knopf dieser Seite
        /// (<see cref="Erstellen"/>), kein zweiter Generator. Er nimmt die gespeicherte
        /// Konfiguration (Bausteine, Ausgabe, Zielordner) und die Versionen, die die
        /// Ergebnisseite gerade vergleicht; die Sicht kommt aus der geteilten
        /// Vergleichswahl wie bei jedem Berichtslauf (Q6: vor dem Sammeln).
        ///
        /// <para>Der Baustein „Wirtschaftlichkeit" ist immer dabei — ein Bericht, der von
        /// der Wirtschaftlichkeitsseite aus entsteht und sie nicht enthält, wäre ein
        /// anderer Bericht als der, um den gebeten wurde.</para>
        ///
        /// <para><b>Nur für diesen Lauf</b> (Anwenderentscheid 22.09.2026 zu Frage (3) aus
        /// E5b): Baustein und Versionen gelten für den einen Bericht; die gespeicherte
        /// Konfiguration der Gruppe bleibt, wie die Berichtsseite sie zuletzt gemerkt hat.
        /// Gemerkt wird allein beim Lauf der Berichtsseite.</para>
        ///
        /// <para><b>BV-E1 (Konzept 10.2, zweiter Einstieg):</b> Der Lauf nimmt die gespeicherte
        /// Vorlagenwahl und prüft vor, ob die Vorlage Platzhalter der Wirtschaftlichkeit führt. Die
        /// Wirtschaftlichkeitsseite hat keine erweiterte Rückfrage; führt die Vorlage Platzhalter,
        /// aber keinen der Wirtschaftlichkeit, entsteht DIESER Bericht deshalb mit der
        /// Standardvorlage, und die Laufmeldung nennt es samt den Befunden der Vorprüfung. Eine
        /// Vorlage ganz ohne Platzhalter bekommt den ganzen Bericht an ihr Ende und bleibt.</para>
        /// </summary>
        /// <param name="varianten">Die gewählten Versionen OHNE Stamm.</param>
        internal Task<LaufErgebnis> ErzeugeFuerVergleich(IReadOnlyList<int> varianten, Action<Laufschritt> melder)
        {
            BerichtsKonfiguration k;
            try { k = _bericht.Lade(_idStamm); }
            catch { k = BerichtsKonfiguration.Standard(); }
            if (k == null) k = BerichtsKonfiguration.Standard();

            List<string> bausteine = BausteineFuerVergleich(k);

            var ids = new List<int>(varianten ?? new List<int>());
            var auftrag = new BerichtAuftrag
            {
                VariantenIds = ids,
                Bausteine = bausteine,
                AusgabeId = AusgabeNummer(k.Ausgabe),
                Zielordner = string.IsNullOrWhiteSpace(k.ZielOrdner) ? Dienste.Pfade.Dokumente : k.ZielOrdner,
                AnzahlMitStamm = ids.Count + 1
            };
            return Erstellen(auftrag, melder, false, erzwingtWirtschaftlichkeit: true);
        }

        /// <summary>
        /// Die Häkchen des zweiten Einstiegs: die gespeicherten — ohne gespeicherte die des Neuzustands —
        /// und stets die Wirtschaftlichkeit. Mit ihnen fragt auch die Anhang-E-Überlagerung derselben
        /// Seite nach ihren Stellen (<see cref="BerichtsvorlagenGaben.AnhangEStellenDerVorlage"/>).
        /// </summary>
        internal static List<string> BausteineFuerVergleich(BerichtsKonfiguration k)
        {
            var bausteine = new List<string>(k?.AktiveBausteine ?? new List<string>());
            if (bausteine.Count == 0)
                foreach (BerichtsKonfiguration.BausteinDef d in BerichtsKonfiguration.AlleBausteine)
                    if (d.Standard) bausteine.Add(d.Schluessel);
            if (!bausteine.Contains(BerichtsKonfiguration.B_WIRTSCHAFT))
                bausteine.Add(BerichtsKonfiguration.B_WIRTSCHAFT);
            return bausteine;
        }

        // =====================================================================
        // BV-E1 — Vorlage, Weg und Laufmeldung
        // =====================================================================

        /// <summary>Die Sicht der Ergebnisansicht als Nummer der Vorprüfung: 2 = Paarvergleich, sonst 1.</summary>
        private int Sichtnummer()
        {
            return Vergleich?.Sicht?.IstPaar == true ? 2 : 1;
        }

        private BerichtsKonfiguration Lade()
        {
            try { return _bericht.Lade(_idStamm) ?? BerichtsKonfiguration.Standard(); }
            catch { return BerichtsKonfiguration.Standard(); }
        }

        /// <summary>Die Vorlagenwahl (Abweichung) der gespeicherten Konfiguration in die des Laufs.</summary>
        private static void VorlagenwahlUebernehmen(BerichtsKonfiguration ziel, BerichtsKonfiguration gespeichert)
        {
            ziel.VorlageWordQuelle = gespeichert?.VorlageWordQuelle;
            ziel.VorlageWordDatei = gespeichert?.VorlageWordDatei;
            ziel.VorlageExcelQuelle = gespeichert?.VorlageExcelQuelle;
            ziel.VorlageExcelDatei = gespeichert?.VorlageExcelDatei;
        }

        /// <summary>
        /// Der Weg des Laufs aus der Antwort der erweiterten Rückfrage (<see cref="BerichtAuftrag.Vorlagenweg"/>):
        /// „standard" = für diesen Lauf die Standardvorlage, „eigene" = die gewählte. Ohne Antwort und
        /// mit einem Befund, der eine Rückfrage bräuchte, hat niemand gefragt — der zweite Einstieg
        /// oder ein Befund, der erst mit der Auswahl dieses Laufs entstand: Die Befunde gehen dann in
        /// die Laufmeldung (<paramref name="ungefragt"/>); im zweiten Einstieg nimmt eine Vorlage mit
        /// Platzhaltern, aber ohne einen der Wirtschaftlichkeit, die Standardvorlage.
        /// </summary>
        internal static Startweg Weg(string vorlagenweg, Startbefund start, bool erzwingtWirtschaftlichkeit,
                                     out IReadOnlyList<string> ungefragt)
        {
            ungefragt = Array.Empty<string>();
            if (string.Equals(vorlagenweg, EPOS.UI.Seiten.Berichte.Startweg.Standard, StringComparison.Ordinal))
                return Startweg.Standard;
            if (string.Equals(vorlagenweg, EPOS.UI.Seiten.Berichte.Startweg.Eigene, StringComparison.Ordinal))
                return Startweg.Gewaehlt;
            if (start == null || !start.BrauchtRueckfrage) return Startweg.Gewaehlt;

            ungefragt = BerichtsvorlagenGaben.Punkte(start);
            bool ohneWirtschaft = erzwingtWirtschaftlichkeit && start.OhneWirtschaftlichkeit && start.StandardAngeboten
                                  && (start.Pruefbefund?.AnzahlPlatzhalter ?? 0) > 0;
            return ohneWirtschaft ? Startweg.Standard : Startweg.Gewaehlt;
        }

        /// <summary>
        /// Die Antwort der erweiterten Rückfrage für die Excel-Mappe (Anwenderentscheid BV-E7-3): Hat die Excel-Vorlage
        /// Fehler und lautet die Antwort „standard“ (der zweite Weg: Standardvorlage bzw. „Ohne Excel-Vorlage“), entsteht
        /// die Mappe für diesen Lauf ohne Vorlage (<c>true</c>). Ohne Antwort hat niemand gefragt — die Befunde gehen in
        /// die Laufmeldung (<paramref name="ungefragt"/>), und die Mappe entsteht aus der gewählten Vorlage.
        /// Ohne Fehler der Excel-Vorlage bleibt sie, wie auch die Antwort lautet.
        /// </summary>
        internal static bool WegExcel(string vorlagenweg, Excelstartbefund start, out IReadOnlyList<string> ungefragt)
        {
            ungefragt = Array.Empty<string>();
            if (start == null || !start.BrauchtRueckfrage) return false;
            if (string.Equals(vorlagenweg, EPOS.UI.Seiten.Berichte.Startweg.Standard, StringComparison.Ordinal)) return true;
            if (string.Equals(vorlagenweg, EPOS.UI.Seiten.Berichte.Startweg.Eigene, StringComparison.Ordinal)) return false;
            ungefragt = BerichtsvorlagenGaben.Punkte(start);
            return false;
        }

        /// <summary>
        /// Die Meldung eines gelungenen Laufs: die geschriebenen Dateien, die Laufmeldung des
        /// Word-Berichts (Vorlage und Grund, Rückfälle, gelbe und leere Platzhalter, Kommentare,
        /// Warnungen der Engine — <see cref="BerichtCtrl.Laufmeldung"/>), die Befunde der Vorprüfung,
        /// nach denen niemand gefragt hat, und die Hinweise des Sammlers.
        /// </summary>
        internal static string Meldung(string wordPfad, string excelPfad, Berichtslauf lauf,
                                       IReadOnlyList<string> ungefragt, IReadOnlyList<string> warnungen, bool englisch,
                                       Berichtslauf excelLauf = null)
        {
            var sb = new StringBuilder(MyResource.Resource.BK_BER_MSG_ERSTELLT_KOPF);
            if (wordPfad != null) sb.Append("\r\n").Append(wordPfad);
            if (excelPfad != null) sb.Append("\r\n").Append(excelPfad);

            string laufmeldung = lauf == null ? "" : BerichtCtrl.Laufmeldung(lauf, englisch);
            if (laufmeldung.Length > 0) sb.Append("\r\n\r\n").Append(laufmeldung);

            // BV-E7: die Laufmeldung der Mappe — nur mit Excel-Vorlage oder Rückfall.
            string excelmeldung = BerichtCtrl.LaufmeldungExcel(excelLauf, englisch);
            if (excelmeldung.Length > 0) sb.Append("\r\n\r\n").Append(excelmeldung);

            if (ungefragt != null && ungefragt.Count > 0)
                sb.Append("\r\n\r\n").Append(MyResource.Resource.BV_START_BEFUNDE)
                  .Append("\r\n• ").Append(string.Join("\r\n• ", ungefragt));

            if (warnungen != null && warnungen.Count > 0)
                sb.Append("\r\n\r\n").Append(MyResource.Resource.BK_BER_MSG_HINWEISE)
                  .Append("\r\n• ").Append(string.Join("\r\n• ", warnungen));

            return sb.ToString();
        }

        /// <summary>Die Herkunft der Vorlage (Kern) als Grund der Erfolgszeile (Oberfläche).</summary>
        internal static Vorlagengrund Grund(Vorlagenherkunft herkunft)
        {
            switch (herkunft)
            {
                case Vorlagenherkunft.Projektvorlage: return Vorlagengrund.Projektvorlage;
                case Vorlagenherkunft.Vorgabe: return Vorlagengrund.Vorgabe;
                case Vorlagenherkunft.Ersatz: return Vorlagengrund.Ersatz;
                case Vorlagenherkunft.Ersetzt: return Vorlagengrund.Ersetzt;
                case Vorlagenherkunft.Rueckfall: return Vorlagengrund.Rueckfall;
                default: return Vorlagengrund.Standardvorlage;
            }
        }

        /// <summary>
        /// Die Meldung eines gelungenen Laufs GEGLIEDERT für die Berichtsseite: Warnungen (sichtbar)
        /// und Hinweise (eingeklappt), je in Gruppen. Quellen: die Abschnitte der Laufmeldung des
        /// Word-Berichts und der Mappe (<see cref="BerichtCtrl.Laufabschnitte"/> — Rückfall, gelbe
        /// Platzhalter und Warnungen der Engine sind Warnungen; leere Platzhalter und entfernte
        /// Kommentare Hinweise), die ungefragten Befunde der Vorprüfung (Hinweise) und die Hinweise
        /// des Sammlers mit ihrer Stufe (<see cref="BerichtsDaten.Hinweisliste"/>), gegliedert nach
        /// <see cref="Berichtshinweise.Gruppiere"/> — was für alle Stände gleich lautet, einmal.
        /// Die Vorlage selbst steht in der Erfolgszeile, nicht hier.
        /// </summary>
        internal static void Gliedere(BerichtsDaten daten, Berichtslauf lauf, Berichtslauf excelLauf,
                                      IReadOnlyList<string> ungefragt, bool englisch,
                                      out IReadOnlyList<Laufhinweisgruppe> warnungen,
                                      out IReadOnlyList<Laufhinweisgruppe> hinweise)
        {
            var w = new List<Laufhinweisgruppe>();
            var h = new List<Laufhinweisgruppe>();

            // 1. Die Stände des Sammlers — Warnungen und Hinweise je für sich gegliedert.
            var staende = new List<string>();
            if (daten != null)
                foreach (VariantenDaten v in daten.Varianten) staende.Add(v.Anzeige);
            IReadOnlyList<Berichtshinweis> liste = daten?.Hinweisliste ?? new List<Berichtshinweis>();
            w.AddRange(Gruppen(liste, Berichtshinweisstufe.Warnung, staende));
            h.AddRange(Gruppen(liste, Berichtshinweisstufe.Hinweis, staende));

            // 2. Der Word-Bericht und die Mappe.
            Abschnitte(lauf, englisch, MyResource.Resource.BK_BER_HINWEIS_WORD, w, h);
            if (excelLauf != null && !(excelLauf.IstRueckfall && excelLauf.Rueckfaelle.Count == 0))
            {
                Abschnitte(excelLauf, englisch, MyResource.Resource.BK_BER_HINWEIS_EXCEL, w, h);
                if (excelLauf.Hinweise.Count > 0)
                    Anhaengen(h, MyResource.Resource.BK_BER_HINWEIS_EXCEL,
                              new Laufhinweispunkt(string.Format(MyResource.Resource.BV_XL_LAUF_HINWEISE, excelLauf.Hinweise.Count),
                                                   excelLauf.Hinweise));
            }

            // 3. Die Befunde der Vorprüfung, nach denen niemand gefragt hat.
            if (ungefragt != null && ungefragt.Count > 0)
            {
                var punkte = new List<Laufhinweispunkt>();
                foreach (string p in ungefragt) punkte.Add(new Laufhinweispunkt(p));
                h.Add(new Laufhinweisgruppe(MyResource.Resource.BK_BER_HINWEIS_VORPRUEFUNG, punkte));
            }

            warnungen = w;
            hinweise = h;
        }

        /// <summary>Die Gruppen einer Stufe der Sammlerhinweise mit ihren Anzeigetiteln.</summary>
        private static IEnumerable<Laufhinweisgruppe> Gruppen(IReadOnlyList<Berichtshinweis> liste,
                                                               Berichtshinweisstufe stufe, IReadOnlyList<string> staende)
        {
            var auswahl = new List<Berichtshinweis>();
            foreach (Berichtshinweis x in liste) if (x.Stufe == stufe) auswahl.Add(x);
            foreach (Berichtshinweisgruppe g in Berichtshinweise.Gruppiere(auswahl, staende))
            {
                string titel = g.Art switch
                {
                    Berichtshinweisgruppenart.Lauf => MyResource.Resource.BK_BER_HINWEIS_LAUF,
                    Berichtshinweisgruppenart.AlleStaende => MyResource.Resource.BK_BER_HINWEIS_ALLE,
                    _ => g.IstStamm ? MyResource.Resource.BK_BER_HINWEIS_STAMM
                                    : string.Format(MyResource.Resource.BK_BER_HINWEIS_VARIANTE, g.Stand)
                };
                var punkte = new List<Laufhinweispunkt>();
                foreach (string t in g.Texte) punkte.Add(new Laufhinweispunkt(t));
                yield return new Laufhinweisgruppe(titel, punkte);
            }
        }

        /// <summary>Die Abschnitte einer Laufmeldung ohne die Vorlagenzeile, nach Stufe verteilt.</summary>
        private static void Abschnitte(Berichtslauf lauf, bool englisch, string titel,
                                       List<Laufhinweisgruppe> w, List<Laufhinweisgruppe> h)
        {
            if (lauf == null) return;
            foreach (Berichtsmeldung m in BerichtCtrl.Laufabschnitte(lauf, englisch))
            {
                if (m.Kennung == KiMeldungskennung.BV_LAUF_VORLAGE) continue;
                bool warnung = m.Kennung == KiMeldungskennung.BV_LAUF_RUECKFALL
                            || m.Kennung == KiMeldungskennung.BV_LAUF_UNBEKANNT
                            || m.Kennung == KiMeldungskennung.BV_LAUF_WARNUNGEN;
                var punkt = new Laufhinweispunkt(m.Text, m.Punkte ?? Array.Empty<string>(),
                                                 m.Kennung == KiMeldungskennung.BV_LAUF_LEER);
                Anhaengen(warnung ? w : h, titel, punkt);
            }
        }

        /// <summary>Hängt einen Punkt an die Gruppe dieses Titels an (legt sie bei Bedarf an).</summary>
        private static void Anhaengen(List<Laufhinweisgruppe> gruppen, string titel, Laufhinweispunkt punkt)
        {
            for (int i = 0; i < gruppen.Count; i++)
            {
                if (!string.Equals(gruppen[i].Titel, titel, StringComparison.Ordinal)) continue;
                var punkte = new List<Laufhinweispunkt>(gruppen[i].Punkte) { punkt };
                gruppen[i] = new Laufhinweisgruppe(titel, punkte);
                return;
            }
            gruppen.Add(new Laufhinweisgruppe(titel, new[] { punkt }));
        }

        // =====================================================================
        // Umgebung
        // =====================================================================


        /// <summary>Bricht einen laufenden Bericht ab — ETAPPE E5 (U44): auch einen, den
        /// die Wirtschaftlichkeitsseite gestartet hat.</summary>
        internal void Abbrechen()
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
