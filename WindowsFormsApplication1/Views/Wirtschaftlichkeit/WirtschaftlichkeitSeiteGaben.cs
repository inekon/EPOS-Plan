using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Seiten.Berichte;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Wirtschaftlichkeitsseite (iU9-W5.3/W5.6) — Nachfolge
    /// von <c>Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs</c> (831 Z.).
    ///
    /// <para><b>Was hier liegt.</b> Laden und Rechnen über
    /// <see cref="WirtschaftlichkeitCtrl"/> und
    /// <see cref="BerichtsDatenSammler"/>, der Parameternachweis (L12/L13),
    /// die vier Kennzahl-Karten (KD6a) und die Zeilen der Vergleichstabelle
    /// (<c>WirtschaftlichkeitZeilen.Kennzahlen</c> — EINE Zeilendefinition für
    /// Seite, Word und Excel). Dazu die Parametersätze der fünf Unterdialoge.
    /// Die Komponente <see cref="WirtschaftlichkeitSeite"/> zeigt nur an.</para>
    ///
    /// <para><b>Zwei Zwischenspeicher wie im Vorläufer.</b> Der Tarif wird
    /// EINMAL je Sitzung gelesen (E7: die Beschriftung der Stromkostenzeile
    /// hängt daran, und <c>ZeigeErgebnisse</c> läuft bei jedem
    /// Szenariowechsel), die Emissionsbilanzen einmal je Datenstand (Review
    /// Phase 8 — nicht bei jedem Wechsel im Oberflächenfaden rechnen).</para>
    /// </summary>
    internal sealed class WirtschaftlichkeitSeiteGaben
    {
        private readonly int _idStamm;
        private readonly string _stammName;
        private readonly Func<Form> _besitzer;

        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();

        private List<WirtschaftlichkeitErgebnis> _ergebnisse = new List<WirtschaftlichkeitErgebnis>();
        private readonly Dictionary<int, string> _namen = new Dictionary<int, string>();
        private readonly Dictionary<int, EmissionsBilanz> _bilanzen = new Dictionary<int, EmissionsBilanz>();

        /// <summary>Die Ids der Gruppe in Listenreihenfolge (Stamm zuerst) — für die Vergleichswahl (W5‑B‑5).</summary>
        private readonly List<int> _gruppe = new List<int>();

        /// <summary>
        /// Die gespeicherten Simulationsstände der Gruppe (VF-1, Teil C) — Grundlage des
        /// Satzes „Ergebnisse aus gespeicherten Läufen vom …". Genommen wird der
        /// ÄLTESTE: Er begrenzt, wie frisch die Tabelle insgesamt ist.
        /// </summary>
        private readonly List<DateTime> _simStaende = new List<DateTime>();

        /// <summary>Die geteilte Vergleichswahl der drei Seiten (W5‑B‑5); die Rahmenhülle setzt sie.</summary>
        internal Vergleichsauswahl Vergleich { get; set; } = new Vergleichsauswahl();

        private WirtschaftlichkeitParameter _parameterCache;
        private TarifParameter _tarifCache;

        private CancellationTokenSource _cts;

        /// <summary>
        /// Die Szenarien als Nummer. Die PERSISTENZWERTE
        /// (<c>Tab_ErgebnisWirtschaftlichkeit.Szenario</c>) kennt nur diese
        /// Hülle — sie dürfen weder in die Komponente noch in eine <c>.resx</c>.
        /// </summary>
        private static readonly string[] SZENARIEN =
        {
            WirtschaftlichkeitSzenario.ERWARTET,
            WirtschaftlichkeitSzenario.BEST,
            WirtschaftlichkeitSzenario.WORST
        };

        internal WirtschaftlichkeitSeiteGaben(int idStamm, string stammName, Func<Form> besitzer)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
            _besitzer = besitzer;
        }

        /// <summary>Läuft gerade eine Berechnung?</summary>
        internal bool Beschaeftigt { get { return _cts != null; } }

        /// <summary>Der Parametersatz der Seite.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Laden"] = new Func<WirtschaftlichkeitStand>(Laden),
                ["Anzeigen"] = new Func<int, ErgebnisAnsicht>(Ansicht),
                ["Berechnen"] = new Func<IReadOnlyList<int>, Action<Laufschritt>, Task<LaufErgebnis>>(Berechnen),
                ["Abbrechen"] = new Action(Abbrechen),
                ["VergleichGewaehlt"] = new Action<IReadOnlyList<int>>(VergleichSetzen),
                ["Gaben"] = new Func<WirtschaftlichkeitSeite.Unterdialog,
                                     IReadOnlyDictionary<string, object>>(Unterdialog),
                ["Nachlauf"] = new Func<WirtschaftlichkeitSeite.Unterdialog, bool, string>(Nachlauf),

                // AUFTRAG #325 (Anwenderentscheid 17.09.2026): Der Freitext der nicht
                // monetären Wirkungen wird auf der SEITE gepflegt, nicht mehr im
                // Parameterdialog. Der Schreibweg ist derselbe, den der Dialog nahm.
                ["WirkungSpeichern"] = new Func<string, bool>(WirkungSpeichern),

                ["TitelText"] = T("WIRT_TITEL", "Wirtschaftlichkeit (Kapitalwertmethode DIN EN 17463)")
                                + " — " + T("WIRT_STAMM", "Stamm:") + " " + _stammName,
                ["LabelVarianten"] = T("WIRT_LBL_GRUPPE",
                    "Vergleichsgruppe (Referenz: Stamm, fest gewählt):"),
                ["LabelSzenario"] = T("WIRT_LBL_SZENARIO", "Szenario:"),
                ["LabelFortschritt"] = T("BKS_LBL_FORTSCHRITT", "Fortschritt"),
                ["SpalteArt"] = MyResource.Resource.BK_SP_ART,
                ["SpalteBezeichner"] = MyResource.Resource.BK_SP_BEZEICHNER,
                ["SpalteProjektname"] = MyResource.Resource.BK_SP_PROJEKTNAME,
                ["SpalteSpeicher"] = MyResource.Resource.WIRT_ZEILE_SPEICHER,
                ["SpalteSimulation"] = MyResource.Resource.BK_BER_SP_SIMULATION,
                ["PhotovoltaikText"] = T("PVW_KNOPF", "Photovoltaik…"),
                ["BhkwText"] = T("BHW_KNOPF", "BHKW-Wirtschaftlichkeit…"),
                ["StrombezugText"] = T("WIRT_BTN_STROM_TARIF", "Strombezug…"),
                ["ParameterText"] = T("WIRT_BTN_PARAMETER", "Parameter…"),
                ["VerlaufText"] = T("WIRT_BTN_VERLAUF", "Verlauf…"),
                ["BerechnenText"] = T("WIRT_BTN_BERECHNEN", "Berechnen"),
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["MeldungStammReferenz"] = MyResource.Resource.BK_BER_MSG_STAMM_REFERENZ,
                ["StatusAbgebrochen"] = MyResource.Resource.BK_BER_STATUS_ABGEBROCHEN,
                ["HilfeSchluessel"] = "UcWirtschaftlichkeit.btn_Help"
            };
        }

        // =====================================================================
        // Laden (Vorbild LadeDaten + AktualisiereListe + ZeigeParameterzeile)
        // =====================================================================

        private WirtschaftlichkeitStand Laden()
        {
            var stand = new WirtschaftlichkeitStand();

            var zeilen = new List<VarianteZeile>();
            _namen.Clear();
            _gruppe.Clear();
            _simStaende.Clear();
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
                        // VF-1: Womit rechnet diese Version ihren Stromspeicher? Der Text
                        // kommt aus der EINEN Kernmethode, die auch der Simulationsreiter
                        // und die Vergleichstabelle nehmen.
                        Speicher = SpeicherAnzeigeCtrl.SpeicherKontextText(st.IdProjekt),
                        SimStand = st.SimStandText,
                        IstStamm = st.IstStamm,
                        Auffaellig = !st.SimStand.HasValue || st.Veraltet
                    });
                    if (st.SimStand.HasValue) _simStaende.Add(st.SimStand.Value);
                    _gruppe.Add(st.IdProjekt);
                    _namen[st.IdProjekt] = st.IstStamm
                        ? MyResource.Resource.BK_ART_STAMM
                        : (string.IsNullOrEmpty(st.Variantenname) ? st.Projektname : st.Variantenname);
                }
            }
            catch { }
            stand.Varianten = zeilen;

            // Die Vergleichswahl ist die GETEILTE der drei Seiten (W5-B-5, 08.09.2026):
            // Vorgabe alle Versionen der Gruppe (Vorbild AktualisiereListe), abgewaehlt
            // bleibt abgewaehlt - auch ueber Uebersicht und Kosten hinweg.
            List<int> gewaehlt = Vergleich.Gewaehlte(_gruppe, _idStamm);
            stand.GewaehlteVarianten = gewaehlt;

            var szenarien = new List<ValueTuple<int, string>>();
            for (int i = 0; i < SZENARIEN.Length; i++)
                szenarien.Add(new ValueTuple<int, string>(i, SzenarioAnzeige(i)));
            stand.Szenarien = szenarien;
            stand.SzenarioId = 0;

            stand.Parameterzeile = Parameterzeile();

            // Persistierte Ergebnisse anzeigen, solange sie zum Simulationsstand
            // passen (Vorbild LadeDaten).
            // Geladen werden die Ergebnisse ALLER Versionen der Gruppe; die Wahl
            // filtert erst in Ansicht() - so folgt die Tabelle einem Haken sofort,
            // ohne die Datenbank erneut zu lesen.
            try { _ergebnisse = _ctrl.LadeErgebnisse(new List<int>(_gruppe)); }
            catch { _ergebnisse = new List<WirtschaftlichkeitErgebnis>(); }

            bool veraltet = _ergebnisse.Count > 0 &&
                            _ergebnisse.Any(x => x.Fehlgrund == null && !_ctrl.ErgebnisAktuell(x));
            BilanzenAuffrischen();
            stand.Ansicht = Ansicht(0);

            stand.Statuszeile = _ergebnisse.Count == 0
                ? T("WIRT_STATUS_KEINE", "Noch keine Wirtschaftlichkeitsberechnung gespeichert — bitte „Berechnen“.")
                : veraltet
                    ? T("WIRT_STATUS_VERALTET", "⚠ Gespeicherte Ergebnisse passen nicht mehr zum Simulationsstand — bitte „Berechnen“.")
                    : string.Format(T("WIRT_STATUS_STAND", "Gespeicherte Ergebnisse vom {0}."),
                                    _ergebnisse[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm"));

            // W5-B-5: Eine gewaehlte Version OHNE gespeichertes Ergebnis fehlte bis
            // hierher stumm in der Tabelle ("Andere WP" in der Windows-Abnahme vom
            // 08.09.2026). Jetzt sagt die Statuszeile, wie viele es sind.
            int fehlend = 0;
            foreach (int id in gewaehlt)
                if (!_ergebnisse.Any(x => x.IdProjekt == id && x.Szenario == SZENARIEN[0])) fehlend++;
            if (fehlend > 0 && _ergebnisse.Count > 0)
                stand.Statuszeile += " " + string.Format(
                    T("WIRT_STATUS_FEHLEND", "Für {0} gewählte Version(en) liegt kein gespeichertes Ergebnis vor — bitte „Berechnen“."),
                    fehlend);

            WirtschaftlichkeitCtrl.ErzeugerFlags flags = null;
            try { flags = _ctrl.ErzeugerDerGruppe(_idStamm); }
            catch { }
            bool tarifAktiv = false;
            try { tarifAktiv = _ctrl.LadeTarif(_idStamm).Aktiv; }
            catch { }

            stand.MitPhotovoltaik = flags != null && flags.Photovoltaik;
            stand.MitBhkw = flags != null && flags.Bhkw;

            // Der Einstieg "Strombezug..." haengt allein am TARIFSATZ des Projekts:
            // Er pflegt die Sicht "Strombezug" von Tab_ProjektTarif, und die wirkt
            // nur, solange der Satz aktiv ist (ohne ihn gelten die Flat-Preise der
            // Kostenmaske). Die Erzeugerlage der Gruppe entscheidet nicht mit.
            stand.MitStrombezug = tarifAktiv;

            // ETAPPE W5-B-11 (Anwenderentscheid 09.09.2026): die zwei VALERI-Ausweise
            // des Nachweisblocks. Sie haengen am Projekt, nicht an der Wahl - deshalb
            // stehen sie am Stand und nicht an der Ansicht.
            stand.Zeitraumzeile = Zeitraumzeile();
            stand.Vereinfachungszeile = Vereinfachungszeile(stand.MitPhotovoltaik);

            // ETAPPE W5-B-12 (Anwenderentscheid 09.09.2026, VALERI-Luecke G6), Form
            // aus AUFTRAG #328: der Freitext "Nicht monetaere Wirkungen" geht ROH
            // hinueber und nur einmal. Die Seite weist ihn im Kopf ihres
            // Bewertungsblocks aus und pflegt ihn dort; eine zweite, hier fertig
            // formulierte Zeile fuer den Nachweisblock gibt es nicht mehr. Er haengt
            // wie die zwei Zeilen darueber am PROJEKT und nicht an der Szenario-
            // oder Vergleichswahl.
            stand.NichtMonetaer = NichtMonetaer();

            return stand;
        }

        /// <summary>
        /// Der AUSWEIS der Bilanzierungsregeln steht NEBEN dem Parameternachweis,
        /// nicht in ihm (L12/L13): eigene Herkunft, eigene Lokalisierung.
        /// </summary>
        /// <summary>Die Vergleichswahl der Seite (W5‑B‑5) in die geteilte Auswahl.</summary>
        private void VergleichSetzen(IReadOnlyList<int> gewaehlt)
        {
            Vergleich.Setzen(gewaehlt, _gruppe, _idStamm);
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (VALERI-Lücke G7): der Betrachtungszeitraum gegen die
        /// Nutzungsdauern der Investitionspositionen — gelesen im Szenario ERWARTET,
        /// also aus derselben Liste, mit der der Kapitalwert rechnet. Ein Lesefehler
        /// lässt die Zeile still entfallen; sie ist Ausweis, kein Ergebnis.
        /// </summary>
        private string Zeitraumzeile()
        {
            try
            {
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                var positionen = new List<KapitalwertRechner.InvestPosition>();
                foreach (int id in _gruppe)
                    positionen.AddRange(WirtschaftlichkeitCtrl.LiesInvestitionen(
                        id, WirtschaftlichkeitSzenario.ERWARTET));
                return NutzungsdauerAbgleich.Hinweis(p.Betrachtungszeitraum, positionen,
                                                     BerichtTexte.Kultur);
            }
            catch { return ""; }
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (VALERI-Lücken G10 und G1/G3/G5): die Herleitung der
        /// Eigennutzung — nur mit Photovoltaik in der Gruppe, denn ohne PV gibt es
        /// keine Eigenverbrauchsquote — und die offengelegten Vereinfachungen.
        /// </summary>
        private static string Vereinfachungszeile(bool mitPhotovoltaik)
        {
            try
            {
                string s = ValeriAusweis.Vereinfachungen();
                return mitPhotovoltaik
                     ? ValeriAusweis.EigennutzungHerleitung() + " " + s
                     : s;
            }
            catch { return ""; }
        }

        /// <summary>
        /// AUFTRAG #325 (Anwenderentscheid 17.09.2026): der GEPFLEGTE Freitext, roh —
        /// das, was der Bewertungsblock der Seite zum Bearbeiten bekommt und seit
        /// AUFTRAG #328 in seinem Kopf auch ausweist. Ein Lesefehler liefert den
        /// leeren Text; der Block bleibt dann leer und überschreibt nichts, solange
        /// der Anwender nicht selbst speichert.
        ///
        /// <para>Der Bericht liest diesen Weg NICHT: Word (<c>BausteineWirtschaftlichkeit</c>)
        /// und Excel (<c>ExcelBerichtGenerator</c>) holen sich
        /// <c>WirtschaftlichkeitParameter.NichtMonetaer</c> selbst und formulieren mit
        /// <c>WIRT_NM_ZEILE</c> bzw. <c>WIRT_NM_TITEL</c> ihre eigene Ausgabe.</para>
        /// </summary>
        private string NichtMonetaer()
        {
            try
            {
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                return p != null && p.NichtMonetaer != null ? p.NichtMonetaer : "";
            }
            catch { return ""; }
        }

        /// <summary>
        /// AUFTRAG #325: Schreibt den Freitext fort — <b>derselbe Weg, den der
        /// Parameterdialog nahm</b>: den vollständigen Parametersatz frisch lesen, das
        /// eine Feld setzen, <c>SpeichereParameter</c>. So bleibt alles Übrige
        /// unverändert stehen, auch wenn es eine andere Maske zwischenzeitlich
        /// geändert hat.
        /// </summary>
        private bool WirkungSpeichern(string text)
        {
            try
            {
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                if (p == null) return false;
                p.NichtMonetaer = text ?? "";
                if (!_ctrl.SpeichereParameter(p)) return false;
                _parameterCache = p;
                return true;
            }
            catch { return false; }
        }

        private string Parameterzeile()
        {
            try
            {
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                TarifParameter t = _ctrl.LadeTarif(_idStamm);
                string zeile = T("WIRT_PARAM_KOPF", "Parameter:") + " " + p.Nachweis(BerichtTexte.Kultur) +
                       " · " + T("WIRT_PARAM_REFERENZ", "Referenz: Stammprojekt · Restwert linear") +
                       " · " + t.Nachweis(BerichtTexte.Kultur) + " · " +
                       BilanzKonvention.Bestimme(p, new GesetzKatalog()).Ausweis(BerichtTexte.Kultur);

                string gespeichert = GespeicherteLaeufeZeile(p, t);
                return gespeichert.Length == 0 ? zeile : zeile + " · " + gespeichert;
            }
            catch { return ""; }
        }

        /// <summary>
        /// AUFTRAG VF-1, Teil C (Anwenderbefund 17.09.2026): <b>Der Sammler rechnet nicht
        /// immer neu — und das gehört gesagt.</b>
        ///
        /// <para>Frisch simuliert wird jedes Projekt der Gruppe nur, wenn der Lauf
        /// Stundenreihen braucht (<see cref="MitZeitreihen"/>). Sonst nimmt der Sammler
        /// ein VORHANDENES Simulationsergebnis — auch ein älteres. Bis hierher stand
        /// darüber nirgends etwas: Der Anwender sah Zahlen, die aussahen wie eben
        /// gerechnet.</para>
        ///
        /// <para><b>Der ÄLTESTE Stand der Gruppe</b> steht im Satz, nicht der neueste: Er
        /// sagt, wie alt die Tabelle im schlechtesten Fall ist. Ohne einen einzigen
        /// gespeicherten Lauf entfällt die Zeile — dann simuliert der Sammler ohnehin
        /// jedes Projekt, weil ihm das Ergebnis fehlt.</para>
        /// </summary>
        private string GespeicherteLaeufeZeile(WirtschaftlichkeitParameter p, TarifParameter t)
        {
            if (MitZeitreihen(p, t) || _simStaende.Count == 0) return "";
            DateTime aeltester = _simStaende[0];
            foreach (DateTime d in _simStaende) if (d < aeltester) aeltester = d;
            return string.Format(BerichtTexte.Kultur,
                T("WIRT_PARAM_GESPEICHERT",
                  "Ergebnisse aus gespeicherten Läufen vom {0}; nicht neu gerechnet"),
                aeltester.ToString("dd.MM.yyyy HH:mm", BerichtTexte.Kultur));
        }

        /// <summary>
        /// <b>Braucht der Lauf dieser Gruppe Stundenreihen?</b> — die EINE Herleitung für
        /// den Rechenlauf und für den Ausweis in der Parameterzeile (VF-1).
        ///
        /// <para>W3: Tarifmatrix und KWKG-Split brauchen Stundenreihen; dann wird je
        /// Projekt frisch in-memory simuliert. SP-W1: derselbe Grund für den
        /// Leistungspreis des Stromträgers — seine Basis ist die Bezugsspitze aus der
        /// Viertelstundenreihe, und die gibt es nur aus dem frischen Lauf. LS-E-2: Der
        /// Leistungspreis zählt für die GANZE Gruppe, nicht nur für den Stamm — sonst
        /// fiele er einer Variante still weg, die ihn als Einzige führt.</para>
        /// </summary>
        private bool MitZeitreihen(WirtschaftlichkeitParameter p, TarifParameter tarif)
        {
            // BK1: dieselbe EINE Regel wie im Kern und in der Verlaufshülle.
            return tarif.Aktiv || KwkgAktivierung.IstAktiv(_idStamm, _gruppe) ||
                   KostenEmissionRechner.StromLeistungspreisGepflegt(_idStamm, _gruppe);
        }

        // =====================================================================
        // Anzeigen (Vorbild ZeigeErgebnisse + KachelnAktualisieren)
        // =====================================================================

        private ErgebnisAnsicht Ansicht(int szenarioId)
        {
            string szenario = SZENARIEN[Math.Max(0, Math.Min(SZENARIEN.Length - 1, szenarioId))];
            CultureInfo kultur = BerichtTexte.Kultur;

            // W5-B-5 (08.09.2026): EINE Spalte je GEWAEHLTER Version - auch ohne
            // gespeichertes Ergebnis. Bis hierher stand nur da, wofuer ein Ergebnis
            // lag; eine nie gerechnete Variante fehlte stumm ("Andere WP" in der
            // Windows-Abnahme). Jetzt steht sie mit "—" und dem Hinweis "nicht
            // berechnet" in der Tabelle, und eine abgewaehlte Version verschwindet.
            List<int> gewaehlt = Vergleich.Gewaehlte(_gruppe, _idStamm);
            var spaltenErg = new List<WirtschaftlichkeitErgebnis>();   // je Spalte; null = kein Ergebnis
            var zeilen = new List<WirtschaftlichkeitErgebnis>();       // die vorhandenen Ergebnisse
            foreach (int id in gewaehlt)
            {
                WirtschaftlichkeitErgebnis e = _ergebnisse
                    .FirstOrDefault(x => x.Szenario == szenario && x.IdProjekt == id);
                spaltenErg.Add(e);
                if (e != null) zeilen.Add(e);
            }
            if (gewaehlt.Count == 0)
            {
                // Ohne Gruppenliste (Laden fehlgeschlagen): wie bisher alles, was da ist.
                foreach (WirtschaftlichkeitErgebnis e in _ergebnisse
                             .Where(x => x.Szenario == szenario).OrderByDescending(x => x.IstStamm))
                {
                    spaltenErg.Add(e);
                    zeilen.Add(e);
                }
            }

            var ansicht = new ErgebnisAnsicht
            {
                Kacheln = Kacheln(zeilen, kultur),
                Szenariozeile = Szenariozeile(szenario, kultur),   // W5-B-9
                // W5-B-11 (G9): Der Vorschlag urteilt ueber ALLE drei Szenarien der
                // GEWAEHLTEN Versionen - nicht nur ueber das angezeigte. Deshalb geht
                // hier _ergebnisse hinein, gefiltert auf die Wahl, und nicht die
                // Zeilenliste des einen Szenarios.
                Empfehlungszeile = Empfehlungszeile(gewaehlt, kultur)
            };
            if (zeilen.Count == 0) return ansicht;

            var spalten = new List<string> { T("WIRT_SP_KENNZAHL", "Kennzahl") };
            for (int i = 0; i < spaltenErg.Count; i++)
            {
                WirtschaftlichkeitErgebnis erg = spaltenErg[i];
                int id = erg != null ? erg.IdProjekt : gewaehlt[i];
                spalten.Add(_namen.ContainsKey(id) ? _namen[id]
                            : erg == null ? id.ToString(CultureInfo.InvariantCulture)
                            : (erg.IstStamm ? MyResource.Resource.BK_ART_STAMM : erg.Anzeige));
            }

            var matrixzeilen = new List<MatrixZeile>();

            // ETAPPE E7: EINE Zeilendefinition fuer Seite, Word und Excel. Die
            // SICHTBARKEIT entscheidet sich ueber ALLE Ergebnisse der Gruppe,
            // nicht ueber das gerade angezeigte Szenario - sonst zeigten Seite
            // und Bericht verschiedene Tabellen.
            if (_tarifCache == null)
            {
                try { _tarifCache = _ctrl.LadeTarif(_idStamm); }
                catch { _tarifCache = new TarifParameter(); }
            }
            // ETAPPE B7: Hier stand eine ZWEITE Sichtbarkeitspruefung ueber die gerade
            // gewaehlten Spalten - der Reiter konnte damit eine andere Tabelle zeigen
            // als Word und Excel, entgegen dem Versprechen von E7. Es gibt seither EINE
            // Regel, und sie steht in WirtschaftlichkeitZeilen.Sichtbare.
            foreach (WirtZeile z in WirtschaftlichkeitZeilen.Sichtbare(
                         WirtschaftlichkeitZeilen.Kennzahlen(_ergebnisse, _tarifCache),
                         _ergebnisse))
            {
                string titel = (z.Einzug > 0 ? "    " : "") + z.Titel;
                matrixzeilen.Add(z.IstUeberschrift
                    ? Zeile(titel, spaltenErg, x => "")
                    : Zeile(titel, spaltenErg, x => z.Anzeige(x, kultur)));
            }

            // W3: CO₂-Vermeidung gegenueber getrennter Erzeugung (aus dem Cache;
            // nur fuer Projekte, deren Ergebnis zum Simulationslauf passt).
            if (_bilanzen.Values.Any(x => x != null && x.CO2VermeidungT.HasValue))
                matrixzeilen.Add(Zeile(
                    EmissionsAusweis.BilanzVermeidung(EmissionsAusweis.ModusAusBilanzen(_bilanzen.Values)),
                    spaltenErg, x =>
                    {
                        EmissionsBilanz b = _bilanzen.ContainsKey(x.IdProjekt) ? _bilanzen[x.IdProjekt] : null;
                        return b == null ? "—" : W(b.CO2VermeidungT, "N1", kultur);
                    }));

            // Hinweiszeilen (nicht-fatal W3 / unvollstaendige Rechnungen).
            string hinweis = T("WIRT_ZEILE_HINWEIS", "Hinweis");
            if (zeilen.Any(x => x.Hinweis != null))
                matrixzeilen.Add(Zeile(hinweis, spaltenErg, x => x.Hinweis != null ? "⚠ " + x.Hinweis : ""));
            if (zeilen.Any(x => x.Fehlgrund != null))
                matrixzeilen.Add(Zeile(hinweis, spaltenErg, x => x.Fehlgrund != null ? "⚠ " + x.Fehlgrund : ""));

            // W5-B-5: die gewaehlte Version ohne Ergebnis sagt es in ihrer Spalte.
            if (spaltenErg.Any(e => e == null))
            {
                string nichtBerechnet = "⚠ " + T("WIRT_MSG_NICHT_BERECHNET", "nicht berechnet — bitte „Berechnen“");
                var zellen = new List<string>();
                foreach (WirtschaftlichkeitErgebnis e in spaltenErg) zellen.Add(e == null ? nichtBerechnet : "");
                matrixzeilen.Add(new MatrixZeile { Titel = hinweis, Zellen = zellen });
            }

            KohaerenzZeilen(spaltenErg, matrixzeilen);

            ansicht.Matrix = new ErgebnisMatrix { Spalten = spalten, Zeilen = matrixzeilen };
            return ansicht;
        }

        /// <summary>
        /// ETAPPE W5‑B‑9 (Anwenderentscheid 09.09.2026): die Statuszeile des gewählten
        /// Szenarios.
        ///
        /// <para>Für <b>Erwartet</b> ein Satz: Es rechnet unverändert mit den
        /// Projektparametern — die stehen ohnehin in der Zeile darunter. Für Best und
        /// Worst der WIRKSAME Parametersatz und seine Herkunft: „Vorgaben“, solange
        /// niemand ein Feld gepflegt hat, sonst „gepflegte Werte“.</para>
        ///
        /// <para>Die Anzeigetexte kommen aus denselben Ressourcenschlüsseln wie im
        /// Parameterdialog — Seite und Dialog sollen dieselbe Auskunft geben.</para>
        /// </summary>
        private string Szenariozeile(string szenario, CultureInfo kultur)
        {
            try
            {
                if (string.Equals(szenario, WirtschaftlichkeitSzenario.ERWARTET,
                                  StringComparison.Ordinal))
                    return T("WIRT_SZ_KOPF", "Szenario:") + " " +
                           T("WIRT_SZ_ERWARTET",
                             "Szenario Erwartet: die Projektparameter unverändert.");

                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                SzenarioSatz satz = p.SatzFuer(szenario);
                if (satz == null) return "";

                string name = SzenarioAnzeige(Array.IndexOf(SZENARIEN, szenario));
                string muster = satz.NurVorgaben
                    ? T("WPAR_SZ_HERKUNFT_VORGABE", "{0}: Vorgaben — {1}")
                    : T("WPAR_SZ_HERKUNFT_GEPFLEGT", "{0}: gepflegte Werte — {1}");
                return T("WIRT_SZ_KOPF", "Szenario:") + " " +
                       string.Format(kultur, muster, name, satz.Nachweis(p, kultur));
            }
            catch { return ""; }
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026, VALERI-Lücke G9): der
        /// Vorschlag zur Entscheidung über die GEWÄHLTEN Versionen.
        ///
        /// <para>Die Regel steht im Kern (<see cref="WirtschaftlichkeitEmpfehlung"/>) —
        /// Seite, Word und Excel sollen denselben Satz zeigen. Leer heißt „keine
        /// Variante mit Erwartet-Ergebnis"; dann zeichnet die Seite die Zeile nicht.</para>
        /// </summary>
        private string Empfehlungszeile(List<int> gewaehlt, CultureInfo kultur)
        {
            try
            {
                List<WirtschaftlichkeitErgebnis> quelle = gewaehlt != null && gewaehlt.Count > 0
                    ? _ergebnisse.Where(x => gewaehlt.Contains(x.IdProjekt)).ToList()
                    : _ergebnisse;
                return WirtschaftlichkeitEmpfehlung.Vorschlagstext(quelle, kultur);
            }
            catch { return ""; }
        }

        private static MatrixZeile Zeile(string titel, List<WirtschaftlichkeitErgebnis> zeilen,
                                         Func<WirtschaftlichkeitErgebnis, string> wert)
        {
            var zellen = new List<string>();
            foreach (WirtschaftlichkeitErgebnis erg in zeilen) zellen.Add(erg == null ? "—" : wert(erg));
            return new MatrixZeile { Titel = titel, Zellen = zellen };
        }

        /// <summary>
        /// ETAPPE B2 (Konzept BHKW-Wirtschaftlichkeit § 4.1): die Zeilen der
        /// Kohärenzprüfung, je Hinweis eine Zeile. Sie sind nicht persistiert;
        /// ein aus der Datenbank geladener Stand zeigt sie deshalb nicht.
        /// </summary>
        private void KohaerenzZeilen(List<WirtschaftlichkeitErgebnis> zeilen, List<MatrixZeile> ziel)
        {
            int hoechste = 0;
            foreach (WirtschaftlichkeitErgebnis x in zeilen)
                if (x != null && x.KohaerenzHinweise != null && x.KohaerenzHinweise.Count > hoechste)
                    hoechste = x.KohaerenzHinweise.Count;
            if (hoechste == 0) return;

            string titel = T("KOH_ZEILE_TITEL", "Kohärenzprüfung");
            for (int i = 0; i < hoechste; i++)
            {
                int index = i;
                ziel.Add(Zeile(titel, zeilen, x =>
                {
                    List<KohaerenzHinweis> l = x.KohaerenzHinweise;
                    if (l == null || index >= l.Count) return "";
                    KohaerenzHinweis h = l[index];
                    // ETAPPE B6: drei Schweren, drei Marken - die positive Nennung
                    // (Fall 1) bekommt den Haken, den die Anwendung sonst fuer
                    // "hat geklappt" nimmt.
                    string marke = string.Equals(h.Schwere, KohaerenzSchwere.WARNUNG,
                                                 StringComparison.Ordinal) ? "⚠ "
                                 : string.Equals(h.Schwere, KohaerenzSchwere.BESTAETIGUNG,
                                                 StringComparison.Ordinal) ? "✓ " : "· ";
                    return marke + h.Text;
                }));
            }
        }

        /// <summary>
        /// KD6a: die vier Kennzahl-Karten — beste Variante gegenüber Stamm im
        /// gewählten Szenario. Reine ANZEIGE der bereits berechneten Werte.
        /// </summary>
        private List<KachelZeile> Kacheln(List<WirtschaftlichkeitErgebnis> zeilen, CultureInfo kultur)
        {
            var kw = new KachelZeile { Titel = T("WIRT_KACHEL_KW", "Kapitalwert ggue. Stamm") };
            var an = new KachelZeile { Titel = T("WIRT_KACHEL_ANNUITAET", "Annuität") };
            var am = new KachelZeile { Titel = T("WIRT_KACHEL_AMORTISATION", "Amortisation") };
            var irr = new KachelZeile { Titel = T("WIRT_KACHEL_IRR", "Interner Zinsfuß") };

            WirtschaftlichkeitErgebnis beste = null;
            foreach (WirtschaftlichkeitErgebnis x in zeilen)
                if (!x.IstStamm && x.KapitalwertDiff.HasValue &&
                    (beste == null || x.KapitalwertDiff.Value > beste.KapitalwertDiff.Value))
                    beste = x;

            if (beste != null)
            {
                string name = _namen.ContainsKey(beste.IdProjekt) ? _namen[beste.IdProjekt] : beste.Anzeige;
                string quelle = string.Format(T("WIRT_KACHEL_BESTE", "beste Variante: {0}"), name);

                kw.Wert = beste.KapitalwertDiff.Value.ToString("N0", kultur) + " €";
                kw.Quelle = quelle;
                an.Wert = beste.AnnuitaetKW.HasValue
                    ? beste.AnnuitaetKW.Value.ToString("N0", kultur) + " €/a" : "—";
                an.Quelle = quelle;
                am.Wert = beste.AmortisationJahre.HasValue
                    ? beste.AmortisationJahre.Value.ToString("N1", kultur) + " a"
                    : T("WIRT_KACHEL_KEINE", "keine");
                am.Quelle = quelle;
                irr.Wert = beste.IRR.HasValue ? beste.IRR.Value.ToString("N1", kultur) + " %" : "—";
                irr.Quelle = quelle;
            }
            else
            {
                WirtschaftlichkeitErgebnis stamm = zeilen.Find(x => x.IstStamm);
                string q = T("WIRT_KACHEL_NUR_STAMM", "nur Stammprojekt gerechnet");
                kw.Wert = stamm != null && stamm.Kapitalwert.HasValue
                    ? stamm.Kapitalwert.Value.ToString("N0", kultur) + " €" : "—";
                kw.Quelle = stamm != null
                    ? T("WIRT_KACHEL_STAMM_KW", "Nettobarwert des Stammprojekts") : "";
                an.Wert = "—"; an.Quelle = q;
                am.Wert = "—"; am.Quelle = q;
                irr.Wert = "—"; irr.Quelle = q;
            }

            return new List<KachelZeile> { kw, an, am, irr };
        }

        /// <summary>Emissionsbilanz-Cache neu füllen (nur aktuelle Ergebnisse, W3).</summary>
        private void BilanzenAuffrischen()
        {
            _bilanzen.Clear();
            try
            {
                _parameterCache = _ctrl.LadeParameter(_idStamm);
                if (_parameterCache.IdKraftwerkspark <= 0) return;
                foreach (WirtschaftlichkeitErgebnis erg in _ergebnisse
                         .Where(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET))
                {
                    if (_bilanzen.ContainsKey(erg.IdProjekt)) continue;
                    _bilanzen[erg.IdProjekt] = _ctrl.ErgebnisAktuell(erg)
                        ? EmissionsBilanzRechner.Berechne(erg.IdProjekt, _parameterCache)
                        : null;
                }
            }
            catch { }
        }

        // =====================================================================
        // Berechnen (Vorbild btnBerechnen_Click)
        // =====================================================================

        private async Task<LaufErgebnis> Berechnen(IReadOnlyList<int> variantenIds,
                                                   Action<Laufschritt> melder)
        {
            if (_cts != null) return new LaufErgebnis { Abgebrochen = true };

            var varianten = new List<int>(variantenIds ?? new List<int>());
            _cts = new CancellationTokenSource();
            var melde = new Progress<BerichtsDatenSammler.Fortschritt>(
                f => melder(new Laufschritt(f.Aktuell, f.Gesamt, f.Text)));

            try
            {
                CancellationToken ct = _cts.Token;
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                TarifParameter tarif = _ctrl.LadeTarif(_idStamm);

                // Die Herleitung steht in MitZeitreihen — dieselbe, die auch die
                // Parameterzeile ausweist (VF-1). Zwei Fassungen wären zwei Antworten:
                // eine Zeile, die „nicht neu gerechnet" sagt, während gerechnet wird.
                bool mitZeitreihen = MitZeitreihen(p, tarif);

                _ergebnisse = await Task.Run(() =>
                {
                    BerichtsDaten daten = new BerichtsDatenSammler().Sammle(
                        _idStamm, _stammName, varianten, false, mitZeitreihen, melde, ct);
                    return _ctrl.Berechne(daten, p);
                }, ct);

                _tarifCache = null;
                BilanzenAuffrischen();

                return new LaufErgebnis
                {
                    Erfolg = true,
                    Statuszeile = string.Format(
                        T("WIRT_STATUS_BERECHNET",
                          "Berechnet am {0} — Ergebnisse gespeichert (Basis für den Berichts-Baustein Wirtschaftlichkeit)."),
                        DateTime.Now.ToString("dd.MM.yyyy HH:mm"))
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
                    Fehler = string.Format(
                        T("WIRT_MSG_RECHENFEHLER", "Fehler bei der Wirtschaftlichkeitsberechnung: {0}"),
                        ex.Message)
                };
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
            }
        }

        private void Abbrechen()
        {
            if (_cts != null) _cts.Cancel();
        }

        // =====================================================================
        // Die fünf Unterdialoge
        // =====================================================================

        private Func<bool> _verlaufNeuGesammelt;

        private IReadOnlyDictionary<string, object> Unterdialog(
            WirtschaftlichkeitSeite.Unterdialog art)
        {
            try
            {
                switch (art)
                {
                    case WirtschaftlichkeitSeite.Unterdialog.Photovoltaik:
                        return PhotovoltaikVerguetungHuelle.Gaben(_idStamm, _besitzer);

                    case WirtschaftlichkeitSeite.Unterdialog.Bhkw:
                        string titel;
                        return BhkwWirtschaftlichkeitHuelle.Gaben(_idStamm, _ergebnisse, out titel);

                    case WirtschaftlichkeitSeite.Unterdialog.Strombezug:
                        return TarifstrukturHuelle.Gaben(_idStamm, TarifSicht.Strombezug);

                    case WirtschaftlichkeitSeite.Unterdialog.Parameter:
                        return WirtschaftlichkeitParameterHuelle.Gaben(_idStamm);

                    case WirtschaftlichkeitSeite.Unterdialog.Verlauf:
                        var varianten = new List<int>();
                        foreach (WirtschaftlichkeitErgebnis e in _ergebnisse)
                            if (!e.IstStamm && !varianten.Contains(e.IdProjekt)) varianten.Add(e.IdProjekt);
                        return KapitalwertVerlaufHuelle.Gaben(
                            _idStamm, _stammName, varianten, out _verlaufNeuGesammelt);
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// Was nach einem Unterdialog zu melden ist — wortgleich die Sätze, die
        /// der Vorläufer in seine Statuszeile schrieb.
        /// </summary>
        private string Nachlauf(WirtschaftlichkeitSeite.Unterdialog art, bool gespeichert)
        {
            _tarifCache = null;   // E7: Beschriftung der Stromkostenzeile neu holen

            if (art == WirtschaftlichkeitSeite.Unterdialog.Verlauf)
            {
                // Der Verlaufsdialog kann neu simuliert haben (Review Phase 11):
                // Dann passen die persistierten Ergebnisse nicht mehr zum
                // Simulationsstand — und das gehoert gesagt.
                bool neu = _verlaufNeuGesammelt != null && _verlaufNeuGesammelt();
                _verlaufNeuGesammelt = null;
                if (!neu) return "";

                return _ergebnisse.Any(x => x.Fehlgrund == null && !_ctrl.ErgebnisAktuell(x))
                    ? T("WIRT_MELD_VERLAUF_NEU",
                        "⚠ Für den Verlauf wurde neu simuliert — gespeicherte Ergebnisse passen nicht mehr zum Simulationsstand, bitte „Berechnen“.")
                    : "";
            }

            if (!gespeichert) return "";

            switch (art)
            {
                case WirtschaftlichkeitSeite.Unterdialog.Photovoltaik:
                    return T("PVW_MELD_GESPEICHERT", "PV-Vergütung gespeichert — bitte neu berechnen.");
                case WirtschaftlichkeitSeite.Unterdialog.Bhkw:
                    return T("BHW_MELD_GESPEICHERT",
                             "BHKW-Wirtschaftlichkeit gespeichert — bitte neu berechnen.");
                case WirtschaftlichkeitSeite.Unterdialog.Strombezug:
                    return T("WIRT_MELD_TARIF", "Tarifstruktur gespeichert — bitte neu berechnen.");
                case WirtschaftlichkeitSeite.Unterdialog.Parameter:
                    return T("WIRT_MELD_PARAMETER", "Parameter gespeichert — bitte neu berechnen.");
            }
            return "";
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        /// <summary>
        /// Der Anzeigetext eines Szenarios. Bis eine Übersetzung vorliegt, ist
        /// es der Persistenzwert selbst (unverändert zum Vorläufer, W1-O6).
        /// </summary>
        private static string SzenarioAnzeige(int nummer)
        {
            string schluessel = nummer == 1 ? "WIRT_SZEN_BEST"
                              : nummer == 2 ? "WIRT_SZEN_WORST" : "WIRT_SZEN_ERWARTET";
            return T(schluessel, SZENARIEN[nummer]);
        }

        private static string W(double? v, string format, CultureInfo kultur)
        {
            return v.HasValue ? v.Value.ToString(format, kultur) : "—";
        }

        private static string T(string schluessel, string rueckfall)
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
