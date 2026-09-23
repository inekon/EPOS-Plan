using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Seiten.Berichte;
using EPOS.UI.Seiten.Simulation;
using SpeicherEngine;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was der Verlauf von der Seite wissen muss, um zu rechnen und zu zeichnen: die im
    /// Vergleich angehakten Stände, die Sicht der Sitzung und die Erklärzeile der Referenz.
    /// Die Hülle der Seite liefert ihn bei jedem Aufruf frisch.
    /// </summary>
    internal sealed class VerlaufKontext
    {
        /// <summary>Die angehakten Stände samt Stamm (<c>Vergleichsauswahl.Gewaehlte</c>).</summary>
        public List<int> Gewaehlt { get; set; } = new List<int>();

        /// <summary>Die Sicht der Sitzung (Kopie); <c>null</c> oder Sicht 1 = gegen die Referenz der Gruppe.</summary>
        public Vergleichssicht Sicht { get; set; }

        /// <summary>Die Erklärzeile der Referenz, wie sie die Seite zeigt — die zweite Zeile des Excel-Blatts.</summary>
        public string Referenzzeile { get; set; } = "";
    }

    /// <summary>
    /// ETAPPE E6 (Konzept Wirtschaftlichkeit § 2.13 (5)) — die PLATTFORMFREIE Hülle des
    /// Abschnitts „Verlauf" der Wirtschaftlichkeitsseite: der Kapitalwert-Verlauf mit allen
    /// drei Szenarien, im Abschnitt und nicht mehr hinter dem Knopf „Verlauf…".
    ///
    /// <para><b>Aus der Hülle des Dialogs wird die Hülle des Abschnitts.</b> Bis E6 baute
    /// diese Klasse den Parametersatz des Dialogs <c>KapitalwertVerlaufDialog</c>, der ein
    /// Szenario je Lauf rechnete. Seit E6 hält sie den Verlauf der Seite: die gesammelten
    /// Eingangsdaten (aus dem Rechenlauf der Seite oder eigens gesammelt), die drei Läufe
    /// (<see cref="WirtschaftlichkeitCtrl.BerechneVerlaufSzenarien"/>, ohne Speichern) und
    /// daraus je Wahl das Zeichenmodell (<see cref="ChartRenderer.KapitalwertSzenarienModell"/>),
    /// die Zeilen darunter und das Excel-Blatt (<see cref="VerlaufExcel"/>).</para>
    ///
    /// <para><b>Drei Wege.</b> <see cref="Zeichnen"/> ist billig: Er baut aus dem gerechneten
    /// Verlauf die Ansicht zur Wahl und rechnet nur dann aus DENSELBEN Eingangsdaten nach, wenn
    /// sich Sicht, Referenz oder Horizont geändert haben. <see cref="Berechnen"/> sammelt, wo
    /// Eingangsdaten fehlen (der Sammler simuliert dann, wie im abgelösten Dialog), auf einem
    /// eigenen Faden über <c>Kulturweitergabe</c> und ist abbrechbar. <see cref="NachExcel"/>
    /// fragt über <c>Dienste.Datei</c> nach dem Ziel und schreibt das Blatt „Verlauf".</para>
    ///
    /// <para><b>Die Persistenzwerte der Szenarien kennt nur diese Hülle</b>: Die Seite sieht
    /// die Nummern 0 (Ungünstig), 1 (Erwartet), 2 (Günstig).</para>
    /// </summary>
    internal sealed class KapitalwertVerlaufHuelle
    {
        private readonly int _idStamm;
        private readonly string _stammName;
        private readonly Func<VerlaufKontext> _kontext;
        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();

        /// <summary>Die Eingangsdaten des Verlaufs — aus dem Rechenlauf der Seite oder eigens gesammelt.</summary>
        private BerichtsDaten _daten;

        /// <summary>Der gerechnete Verlauf; <c>null</c> = noch keiner oder verworfen.</summary>
        private WirtschaftlichkeitVerlaufSzenarien _verlauf;

        /// <summary>Wofür <see cref="_verlauf"/> gilt: Horizont, Sicht, Referenz, Eingangsdaten.</summary>
        private string _schluessel;

        /// <summary>Der Horizont des Verlaufs [a]; 0 = der Betrachtungszeitraum.</summary>
        private int _jahre;

        /// <summary>Der Betrachtungszeitraum T beim letzten Rechnen [a].</summary>
        private int _t;

        /// <summary>
        /// ETAPPE E8a — die drei Läufe über den Betrachtungszeitraum, falls der Verlauf auf einem
        /// anderen Horizont steht; <c>null</c> = keine (der Verlauf selbst steht auf T).
        /// </summary>
        private WirtschaftlichkeitVerlaufSzenarien _ueberT;

        /// <summary>Wofür <see cref="_ueberT"/> gilt: Horizont, Sicht, Referenz, Eingangsdaten.</summary>
        private string _ueberTSchluessel;

        /// <summary>Rechnet „Aktualisieren" gerade auf dem Arbeitsfaden? Dann rechnet
        /// <see cref="GliederungenUeberT"/> nichts nach.</summary>
        private volatile bool _rechnet;

        /// <param name="kontext">Liefert die Wahl der Seite frisch (Vergleich, Sicht, Referenzzeile).</param>
        internal KapitalwertVerlaufHuelle(int idStamm, string stammName, Func<VerlaufKontext> kontext)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
            _kontext = kontext ?? (() => new VerlaufKontext());
        }

        /// <summary>Ist ein Verlauf gerechnet? (Prüfhilfe)</summary>
        internal bool Gerechnet => _verlauf != null;

        /// <summary>Die Datenseite des Abschnitts für die Seite.</summary>
        internal VerlaufDienste Seitenwege()
        {
            return new VerlaufDienste
            {
                Zeichnen = Zeichnen,
                Berechnen = Berechnen,
                NachExcel = NachExcel,
                JahreVorgabe = JahreVorgabe()
            };
        }

        /// <summary>
        /// Übernimmt die Eingangsdaten eines Rechenlaufs der Seite und rechnet den Verlauf
        /// gleich mit — auf dem Faden des Laufs, damit die Seite danach nur noch zeichnet.
        /// Ein Fehler hier lässt den Verlauf leer; der Rechenlauf der Seite bleibt gültig.
        /// </summary>
        internal void DatenUebernehmen(BerichtsDaten daten)
        {
            _daten = daten;
            _verlauf = null;
            _schluessel = null;
            _ueberT = null;
            _ueberTSchluessel = null;
            if (daten == null) return;
            try { Rechne(_kontext()); }
            catch { _verlauf = null; _schluessel = null; }
        }

        /// <summary>
        /// Nach gespeicherten Parametern: Der gerechnete Verlauf gilt nicht mehr; die
        /// Eingangsdaten bleiben, und das nächste Zeichnen rechnet mit den neuen Parametern.
        /// </summary>
        internal void Verwerfen()
        {
            _verlauf = null;
            _schluessel = null;
            _ueberT = null;
            _ueberTSchluessel = null;
        }

        /// <summary>
        /// ETAPPE E8a (Konzept § 2.11.4 V‑C; ValERI-Block 2, U41, U46, U47) — die
        /// <b>Gliederungen der drei Läufe über den Betrachtungszeitraum</b>, abgeglichen gegen
        /// die Ergebnisse, neben denen die Seite sie zeigt.
        ///
        /// <para><b>Keine eigene Rechnung.</b> Quelle ist der gerechnete Verlauf: Steht er auf
        /// T (die Vorgabe), IST er die Quelle; ist er verworfen, rechnet dieser Weg ihn aus
        /// denselben Eingangsdaten nach — genau das, was das nächste Zeichnen des Abschnitts
        /// ohnehin täte. Nur wenn der Anwender den Horizont des Verlaufs verstellt hat,
        /// entstehen die drei Läufe über T eigens, mit demselben Rechenweg, und werden gemerkt.
        /// Während „Aktualisieren" auf dem Arbeitsfaden rechnet, wird nichts nachgerechnet.</para>
        /// </summary>
        /// <param name="gespeichert">Die Ergebnisse der Seite (gespeichert bzw. in Sicht 2
        /// gegen A gerechnet); eine Gliederung, die nicht zu ihnen passt, fehlt im Satz.</param>
        /// <returns><c>null</c>, solange in dieser Sitzung keine Eingangsdaten vorliegen (kein
        /// „Berechnen", kein „Aktualisieren").</returns>
        internal Zahlungsgliederungen GliederungenUeberT(IEnumerable<WirtschaftlichkeitErgebnis> gespeichert)
        {
            if (_daten == null) return null;
            VerlaufKontext k = _kontext();
            WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
            int t = p.Betrachtungszeitraum;

            WirtschaftlichkeitVerlaufSzenarien laeufe;
            int horizont = _jahre > 0 ? _jahre : Horizont(p);
            if (horizont == t)
            {
                if (!_rechnet &&
                    (_verlauf == null || !string.Equals(_schluessel, Schluessel(k), StringComparison.Ordinal)))
                    Rechne(k);
                laeufe = _verlauf;
            }
            else
            {
                string schluessel = Schluessel(k, t);
                if (!_rechnet &&
                    (_ueberT == null || !string.Equals(_ueberTSchluessel, schluessel, StringComparison.Ordinal)))
                {
                    _daten.Sicht = k.Sicht != null && k.Sicht.IstPaar ? k.Sicht.Kopie() : null;
                    _daten.IdGruppenreferenz = p.IdReferenzprojekt;
                    _ueberT = _ctrl.BerechneVerlaufSzenarien(_daten, p, t);
                    _ueberTSchluessel = schluessel;
                }
                laeufe = _ueberT;
            }
            return laeufe == null ? null : Zahlungsgliederungen.Aus(laeufe, p, gespeichert);
        }

        // =====================================================================
        // Die drei Wege der Seite
        // =====================================================================

        /// <summary>Die Ansicht zur Wahl — aus dem gerechneten Verlauf.</summary>
        internal VerlaufAnsicht Zeichnen(VerlaufWahl wahl)
        {
            VerlaufKontext k = _kontext();
            if (_daten == null) return Leer();
            try
            {
                if (_verlauf == null || !string.Equals(_schluessel, Schluessel(k), StringComparison.Ordinal))
                    Rechne(k);
                return Ansicht(wahl, k);
            }
            catch (Exception ex)
            {
                VerlaufAnsicht fehler = Leer();
                fehler.Hinweis = string.Format(Kultur, T("WVERL_MSG_FEHLER", "Fehler beim Berechnen des Verlaufs: {0}"),
                                               ex.Message);
                return fehler;
            }
        }

        /// <summary>
        /// Rechnet den Verlauf über <paramref name="jahre"/> neu — sammelt die
        /// Simulationsdaten, wenn sie fehlen oder einen angehakten Stand nicht tragen.
        /// </summary>
        internal async Task<VerlaufAnsicht> Berechnen(int jahre, VerlaufWahl wahl, CancellationToken ct)
        {
            VerlaufKontext k = _kontext();
            _jahre = Math.Max(2, Math.Min(60, jahre));
            bool neu = false;

            // E3/6-Muster: Der Arbeitsfaden entsteht ueber Kulturweitergabe.Starten statt
            // ueber ein nacktes Task.Run (Waechter ParallelitaetWacheTests).
            // ETAPPE E8a: Solange er rechnet, rechnet GliederungenUeberT nichts nach.
            _rechnet = true;
            try
            {
                await Kulturweitergabe.Starten(() =>
                {
                    if (!Deckt(_daten, k.Gewaehlt))
                    {
                        var varianten = k.Gewaehlt.Where(id => id != _idStamm).ToList();
                        TarifParameter tarif = _ctrl.LadeTarif(_idStamm);
                        // Dieselbe EINE Regel wie im Kern und auf der Seite (BK1, SP-W1, LS-E-2);
                        // Q11 (E7b): nur ein WIRKSAMER Tarifsatz (Rollentarif) braucht die Reihen.
                        bool mitZeitreihen = tarif.Wirksam ||
                                             KwkgAktivierung.IstAktiv(_idStamm, varianten) ||
                                             KostenEmissionRechner.StromLeistungspreisGepflegt(_idStamm, varianten);
                        BerichtsDaten daten = new BerichtsDatenSammler().Sammle(
                            _idStamm, _stammName, varianten, false, mitZeitreihen, null, ct);
                        // Exaktes Kriterium: Der Sammler markiert neu simulierte Projekte selbst.
                        neu = daten.Varianten.Any(v => v.FrischSimuliert);
                        _daten = daten;
                        _ueberT = null;
                        _ueberTSchluessel = null;
                    }
                    ct.ThrowIfCancellationRequested();
                    Rechne(k);
                    return true;
                }, ct);
            }
            finally
            {
                _rechnet = false;
            }

            VerlaufAnsicht ansicht = Ansicht(wahl, k);
            ansicht.NeuSimuliert = neu;
            if (neu)
                ansicht.Meldung = T("WIRT_MELD_VERLAUF_NEU",
                    "⚠ Für den Verlauf wurde neu simuliert — gespeicherte Ergebnisse passen nicht mehr zum Simulationsstand, bitte „Berechnen“.");
            return ansicht;
        }

        /// <summary>
        /// „Verlauf nach Excel…" (U13): Ziel über <c>Dienste.Datei</c> wählen (gewartet —
        /// ein synchron geöffnetes Plattformfenster mitten im Blazor-Ereignis ist der
        /// Absturz aus Befund W13‑B‑1), dann das Blatt „Verlauf" mit der Wahl des Abschnitts
        /// schreiben.
        /// </summary>
        internal async Task<Rueckmeldung> NachExcel(VerlaufWahl wahl)
        {
            VerlaufKontext k = _kontext();
            if (_daten == null)
                return new Rueckmeldung(false, T("WIRT_VERL_EXCEL_LEER",
                    "Noch kein Verlauf gerechnet — erst „Aktualisieren“."));
            try
            {
                if (_verlauf == null || !string.Equals(_schluessel, Schluessel(k), StringComparison.Ordinal))
                    Rechne(k);
            }
            catch (Exception ex)
            {
                return new Rueckmeldung(false, string.Format(Kultur,
                    T("WVERL_MSG_FEHLER", "Fehler beim Berechnen des Verlaufs: {0}"), ex.Message));
            }

            string vorschlag = string.Format(Kultur,
                T("WIRT_VERL_EXCEL_DATEI", "Kapitalwertverlauf_{0}.xlsx"), Dateiteil(_stammName));
            string pfad = await Dienste.Datei.DateiSpeichernAsync(
                T("WIRT_VERL_EXCEL_TITEL", "Verlauf nach Excel speichern"), "Excel (*.xlsx)|*.xlsx", vorschlag);
            if (string.IsNullOrEmpty(pfad)) return Rueckmeldung.Still;

            try
            {
                List<int> staende = Staende(wahl, k);
                List<string> szenarien = Szenarien(wahl);
                WirtschaftlichkeitVerlaufSzenarien verlauf = _verlauf;
                string unterzeile = Unterzeile(k);
                await Kulturweitergabe.Starten(() =>
                {
                    VerlaufExcel.SchreibeMappe(pfad, verlauf, VerlaufBlattTexte.AusRessourcen(), unterzeile,
                                               staende, szenarien);
                    return true;
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                return new Rueckmeldung(false, string.Format(Kultur,
                    T("WIRT_VERL_EXCEL_FEHLER", "Der Verlauf konnte nicht nach Excel geschrieben werden: {0}"),
                    ex.Message));
            }
            return new Rueckmeldung(true, string.Format(Kultur,
                T("WIRT_VERL_EXCEL_OK", "Verlauf nach Excel geschrieben: {0}"), pfad));
        }

        // =====================================================================
        // Rechnen
        // =====================================================================

        /// <summary>
        /// Die drei Läufe aus den vorhandenen Eingangsdaten — in der Sicht der Sitzung: In
        /// Sicht 2 rechnet der Kern gegen A und zeichnet allein B (Konzept § 2.15, VG‑Q5).
        /// </summary>
        private void Rechne(VerlaufKontext k)
        {
            WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
            int jahre = _jahre > 0 ? _jahre : Horizont(p);
            _daten.Sicht = k.Sicht != null && k.Sicht.IstPaar ? k.Sicht.Kopie() : null;
            _daten.IdGruppenreferenz = p.IdReferenzprojekt;
            _verlauf = _ctrl.BerechneVerlaufSzenarien(_daten, p, jahre);
            _jahre = jahre;
            _t = p.Betrachtungszeitraum;
            _schluessel = Schluessel(k);
        }

        /// <summary>Wofür ein gerechneter Verlauf gilt — ändert sich einer der Teile, wird nachgerechnet.</summary>
        private string Schluessel(VerlaufKontext k)
            => Schluessel(k, _jahre > 0 ? _jahre : 0);

        /// <summary>Derselbe Schlüssel für einen ausdrücklich genannten Horizont (ETAPPE E8a:
        /// die Läufe über T, wenn der Verlauf auf einem anderen Horizont steht).</summary>
        private string Schluessel(VerlaufKontext k, int jahre)
        {
            int referenz = 0;
            try { referenz = _ctrl.LadeParameter(_idStamm).IdReferenzprojekt; }
            catch { }
            Vergleichssicht s = k.Sicht;
            bool paar = s != null && s.IstPaar;
            return string.Join("|",
                (_jahre > 0 ? _jahre : 0).ToString(CultureInfo.InvariantCulture),
                paar ? "P" : "A",
                (paar ? s.IdA : 0).ToString(CultureInfo.InvariantCulture),
                (paar ? s.IdB : 0).ToString(CultureInfo.InvariantCulture),
                referenz.ToString(CultureInfo.InvariantCulture),
                (_daten == null ? 0 : RuntimeHelpers.GetHashCode(_daten)).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Tragen die Eingangsdaten jeden angehakten Stand?</summary>
        private static bool Deckt(BerichtsDaten daten, List<int> gewaehlt)
        {
            if (daten == null) return false;
            var vorhanden = new HashSet<int>(daten.Varianten.Select(v => v.IdProjekt));
            return gewaehlt.All(vorhanden.Contains);
        }

        /// <summary>Der Horizont ohne eigene Wahl: der Betrachtungszeitraum (2 … 60 a), sonst 20.</summary>
        private static int Horizont(WirtschaftlichkeitParameter p)
            => p != null && p.Betrachtungszeitraum >= 2 && p.Betrachtungszeitraum <= 60 ? p.Betrachtungszeitraum : 20;

        private int JahreVorgabe()
        {
            try { return Horizont(_ctrl.LadeParameter(_idStamm)); }
            catch { return 20; }
        }

        // =====================================================================
        // Die Ansicht
        // =====================================================================

        private static CultureInfo Kultur => BerichtTexte.Kultur;

        /// <summary>Die Ansicht ohne gerechneten Verlauf: kein Modell, der Grund als Hinweis.</summary>
        private static VerlaufAnsicht Leer()
        {
            return new VerlaufAnsicht
            {
                Szenarien = Szenarienliste(ChartRenderer.VerlaufSzenarienTexte.AusRessourcen()),
                GewaehlteSzenarien = new[] { 0, 1, 2 },
                Hinweis = T("WIRT_VERL_NOCH_NICHT",
                    "Der Verlauf rechnet aus den Simulationsergebnissen alle drei Szenarien — „Aktualisieren“ "
                    + "startet die Rechnung, „Berechnen“ rechnet ihn mit.")
            };
        }

        private VerlaufAnsicht Ansicht(VerlaufWahl wahl, VerlaufKontext k)
        {
            var texte = ChartRenderer.VerlaufSzenarienTexte.AusRessourcen();
            List<KeyValuePair<int, string>> sichtbar = Sichtbare(k);
            List<int> staende = Staende(wahl, k);
            List<string> szenarien = Szenarien(wahl);

            ChartRenderer.Szenarienreihen inhalt =
                ChartRenderer.VerlaufsReihenSzenarien(_verlauf, texte, staende, szenarien);
            Zeichenmodell modell = ChartRenderer.KapitalwertSzenarienModell(
                T("WIRT_VERL_BILD", "Kumulierter Barwert der Differenz zur Referenz — drei Szenarien"),
                inhalt, texte,
                T("WIRT_VERL_FUSS",
                  "Kumulierter Barwert der Differenz zur Referenz je Jahr, ohne Restwert; Farbe = Variante, "
                  + "Strichart = Szenario; der Nulldurchgang ist die dynamische Amortisation."));

            var ansicht = new VerlaufAnsicht
            {
                Modell = modell,
                Staende = sichtbar.Select(v => (v.Key, v.Value)).ToList(),
                GewaehlteStaende = staende,
                Szenarien = Szenarienliste(texte),
                GewaehlteSzenarien = szenarien.Select(s => IndexVon(s)).ToList(),
                Jahre = _jahre,
                // Dieselben Zeilen wie unter dem Dreierbild des Wortberichts (VerlaufZeilen).
                Nulldurchgangszeile = VerlaufZeilen.Nulldurchgaenge(_verlauf, staende, szenarien, Kultur),
                Restwertzeile = VerlaufZeilen.Restwerte(_verlauf, staende, szenarien, Kultur),
                Statuszeile = Statuszeile(),
                Hinweis = Hinweis(k)
            };
            return ansicht;
        }

        /// <summary>Die Stände mit Linie, die im Vergleich angehakt sind — in der Reihenfolge der Gruppe.</summary>
        private List<KeyValuePair<int, string>> Sichtbare(VerlaufKontext k)
        {
            List<KeyValuePair<int, string>> alle = _verlauf == null
                ? new List<KeyValuePair<int, string>>() : _verlauf.Versionen();
            if (k.Gewaehlt == null || k.Gewaehlt.Count == 0) return alle;
            return alle.Where(v => k.Gewaehlt.Contains(v.Key)).ToList();
        }

        /// <summary>Die gezeichneten Stände: die sichtbaren, eingeschränkt auf die Wahl des Abschnitts.</summary>
        private List<int> Staende(VerlaufWahl wahl, VerlaufKontext k)
        {
            List<int> sichtbar = Sichtbare(k).Select(v => v.Key).ToList();
            if (wahl == null || wahl.Staende == null) return sichtbar;
            return sichtbar.Where(wahl.Staende.Contains).ToList();
        }

        /// <summary>Die gezeichneten Szenarien als Persistenzwerte, in der Reihenfolge des Bildes.</summary>
        private static List<string> Szenarien(VerlaufWahl wahl)
        {
            var liste = new List<string>();
            IReadOnlyList<string> alle = WirtschaftlichkeitVerlaufSzenarien.Reihenfolge;
            for (int i = 0; i < alle.Count; i++)
                if (wahl == null || wahl.Szenarien == null || wahl.Szenarien.Contains(i)) liste.Add(alle[i]);
            return liste;
        }

        private static int IndexVon(string szenario)
        {
            IReadOnlyList<string> alle = WirtschaftlichkeitVerlaufSzenarien.Reihenfolge;
            for (int i = 0; i < alle.Count; i++)
                if (string.Equals(alle[i], szenario, StringComparison.Ordinal)) return i;
            return -1;
        }

        private static IReadOnlyList<(int Id, string Text)> Szenarienliste(ChartRenderer.VerlaufSzenarienTexte texte)
        {
            IReadOnlyList<string> alle = WirtschaftlichkeitVerlaufSzenarien.Reihenfolge;
            var liste = new List<(int, string)>();
            for (int i = 0; i < alle.Count; i++) liste.Add((i, texte.Szenarioname(alle[i])));
            return liste;
        }

        /// <summary>
        /// Der Horizont — und nur bei einem abweichenden der Satz, dass die gespeicherten
        /// Ergebnisse unverändert bleiben (bei einem längeren dazu der Satz zur
        /// Amortisationskennzahl). Die drei Bausteine bleiben einzeln übersetzbar.
        /// </summary>
        private string Statuszeile()
        {
            string jenseits = _jahre > _t
                ? T("WVERL_STATUS_JENSEITS",
                    "; Nulldurchgänge jenseits von T erscheinen nicht in der gespeicherten Amortisationskennzahl")
                : "";
            return string.Format(Kultur, T("WIRT_VERL_STATUS", "Verlauf über {0} Jahre, alle drei Szenarien"), _jahre) +
                   (_jahre != _t
                    ? string.Format(Kultur,
                        T("WVERL_STATUS_ABWEICHEND",
                          " (abweichend von T = {0} a — nur Anzeige, gespeicherte Ergebnisse unverändert{1})."),
                        _t, jenseits)
                    : ".");
        }

        /// <summary>
        /// Stände ohne Reihe (mit Grund) und angehakte Stände, die der gerechnete Verlauf nicht
        /// trägt — beides benannt, nie still übergangen.
        /// </summary>
        private string Hinweis(VerlaufKontext k)
        {
            var saetze = new List<string>();
            List<VerlaufSerie> ohne = _verlauf.OhneReihe();
            if (ohne.Count > 0)
                saetze.Add(string.Format(Kultur, T("WVERL_OHNE_REIHE", "⚠ Ohne Reihe: {0}"),
                    string.Join("; ", ohne.Select(s => s.Anzeige + " (" + s.Fehlgrund + ")"))));

            int fehlend = 0;
            if (_daten != null && k.Gewaehlt != null)
            {
                var vorhanden = new HashSet<int>(_daten.Varianten.Select(v => v.IdProjekt));
                fehlend = k.Gewaehlt.Count(id => !vorhanden.Contains(id));
            }
            if (fehlend > 0)
                saetze.Add(string.Format(Kultur, T("WIRT_VERL_FEHLEND",
                    "Für {0} angehakte Version(en) liegt noch kein Verlauf vor — „Aktualisieren“ rechnet ihn nach."),
                    fehlend));
            return string.Join(" ", saetze);
        }

        /// <summary>Die zweite Zeile des Excel-Blatts: Referenz und Horizont.</summary>
        private string Unterzeile(VerlaufKontext k)
        {
            string status = Statuszeile();
            return string.IsNullOrEmpty(k.Referenzzeile) ? status : k.Referenzzeile + " · " + status;
        }

        /// <summary>Ein Stammname als Teil eines Dateinamens — ohne die Zeichen, die kein Dateisystem nimmt.</summary>
        private static string Dateiteil(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in name ?? "")
                sb.Append(Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0 || c == ' ' ? '_' : c);
            return sb.Length == 0 ? "Projekt" : sb.ToString();
        }

        private static string T(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
