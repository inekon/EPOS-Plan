using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Wirtschaftlichkeit;
using EPOS.UI.Seiten.Berichte;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die DATENSEITE der Wirtschaftlichkeitsseite (iU9-W5.3/W5.6) — Nachfolge
    /// von <c>Views/Wirtschaftlichkeit/UcWirtschaftlichkeit.cs</c> (831 Z.), seit
    /// Etappe E3 Schritt 3 plattformfrei in <c>EPOS.UI.Daten</c>: keine
    /// WinForms-Anweisung, kein Fenster. Vier der fünf Unterdialoge zeigen
    /// daneben noch ein eigenes Fenster und kommen deshalb über
    /// <see cref="Wirtschaftlichkeitswege"/> herein, bis sie mit E3 Schritt 6
    /// selbst wandern.
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

        /// <summary>
        /// KONZEPT § 2.15 — die zuletzt GESAMMELTE Vergleichsgruppe. Die Sicht „Zwei
        /// Staende" rechnet gegen A, und dafuer braucht sie dieselben Eingangsdaten wie
        /// der letzte Lauf; gesammelt wird nicht noch einmal. <c>null</c> = in dieser
        /// Sitzung wurde nicht gerechnet — dann sagt die Statuszeile, dass die Sicht
        /// einen Lauf braucht, statt Zahlen gegen die falsche Referenz zu zeigen.
        /// </summary>
        private BerichtsDaten _letzteDaten;

        private CancellationTokenSource _cts;

        /// <summary>
        /// ETAPPE E6 (Konzept § 2.13 (5)) — die Hülle des Abschnitts „Verlauf": der
        /// Kapitalwert-Verlauf mit allen drei Szenarien. Sie entsteht beim ersten Zugriff und
        /// bekommt die Eingangsdaten jedes Rechenlaufs dieser Seite (<see cref="Berechnen"/>).
        /// </summary>
        private KapitalwertVerlaufHuelle _verlaufHuelle;

        /// <summary>
        /// Die Erklärzeile der Referenz, wie sie <see cref="Laden"/> zuletzt gebildet hat — die
        /// zweite Zeile des Excel-Blatts „Verlauf".
        /// </summary>
        private string _referenzzeile = "";

        /// <summary>Die wirksame Referenz der Gruppe beim letzten <see cref="Laden"/> (0 = keine).</summary>
        private int _referenzWirksam;

        /// <summary>Die Verlaufshülle der Seite (ETAPPE E6).</summary>
        private KapitalwertVerlaufHuelle Verlauf
        {
            get
            {
                if (_verlaufHuelle == null)
                    _verlaufHuelle = new KapitalwertVerlaufHuelle(_idStamm, _stammName, Verlaufskontext);
                return _verlaufHuelle;
            }
        }

        /// <summary>
        /// ETAPPE E6 — was der Verlauf von der Seite wissen muss: die angehakten Stände (samt
        /// der wirksamen Referenz, die nicht aus dem Vergleich fallen kann), die Sicht der
        /// Sitzung und die Erklärzeile der Referenz.
        /// </summary>
        private VerlaufKontext Verlaufskontext()
        {
            List<int> gewaehlt = Vergleich.Gewaehlte(_gruppe, _idStamm);
            if (_referenzWirksam > 0 && !gewaehlt.Contains(_referenzWirksam)) gewaehlt.Add(_referenzWirksam);
            return new VerlaufKontext
            {
                Gewaehlt = gewaehlt,
                Sicht = Vergleich.Sicht != null ? Vergleich.Sicht.Kopie() : null,
                Referenzzeile = _referenzzeile ?? ""
            };
        }

        /// <summary>
        /// ETAPPE E5 (V‑A, V‑G6) — die Sensitivitätszeilen zu den gezeigten Ergebnissen:
        /// die gespeicherten des Gruppenlaufs, in Sicht 2 die des Laufs gegen A (ohne
        /// Persistenz, <see cref="PaarErgebnisse"/>). Die Seite zeigt sie seither mit der
        /// Steigungsspalte, bis E5 standen sie nur im Bericht.
        /// </summary>
        private List<SensitivitaetZeile> _sens = new List<SensitivitaetZeile>();

        /// <summary>
        /// ETAPPE E5 (U44, Entscheid Q18) — der BESTEHENDE Berichtsweg
        /// (<c>BerichtSeiteGaben.ErzeugeFuerVergleich</c>), gesetzt von der Rahmenhülle:
        /// Er erzeugt den Bericht mit der gespeicherten Konfiguration, den übergebenen
        /// Versionen und der Sicht der Sitzung. <c>null</c> = kein Knopf „Bericht
        /// erzeugen" (kein Delegat, kein Knopf) — ein zweiter Berichtsgenerator entsteht
        /// hier nicht.
        /// </summary>
        internal Func<IReadOnlyList<int>, Action<Laufschritt>, Task<LaufErgebnis>> Berichtsweg { get; set; }

        /// <summary>
        /// ETAPPE E5 (U44): bricht einen über <see cref="Berichtsweg"/> gestarteten
        /// Berichtslauf ab — derselbe Abbrechen-Knopf der Seite wie beim Rechenlauf.
        /// </summary>
        internal Action BerichtAbbrechen { get; set; }

        /// <summary>
        /// BV-E2 (Konzept Berichtsvorlagen 9.5): die Stellen der Anhang-E-Checkliste in der gewählten
        /// Word-Vorlage — gesetzt von der Rahmenhülle (<c>BerichtsvorlagenGaben.AnhangEStellenDerVorlage</c>
        /// der Berichtshülle derselben Gruppe). <c>null</c> = die Überlagerung nennt die Stellen der
        /// Standardvorlage.
        /// </summary>
        internal Func<AnhangEStellen> AnhangEStellenLaden { get; set; }

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

        internal WirtschaftlichkeitSeiteGaben(int idStamm, string stammName)
        {
            _idStamm = idStamm;
            _stammName = stammName ?? "";
        }

        /// <summary>Läuft gerade eine Berechnung?</summary>
        internal bool Beschaeftigt { get { return _cts != null; } }

        /// <summary>Der Parametersatz der Seite.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            var gaben = new Dictionary<string, object>
            {
                ["Laden"] = new Func<WirtschaftlichkeitStand>(Laden),

                // ETAPPE E5 (U2, V‑1/K8): der Umschalter "Kennzahlen / ValERI-Bewertung"
                // als Sitzungswahl - dieselbe geteilte Instanz wie Haekchen und Sicht.
                ["DarstellungGewaehlt"] = new Action<int>(DarstellungSetzen),
                ["Anzeigen"] = new Func<int, ErgebnisAnsicht>(Ansicht),
                ["Berechnen"] = new Func<IReadOnlyList<int>, Action<Laufschritt>, Task<LaufErgebnis>>(Berechnen),
                ["Abbrechen"] = new Action(Abbrechen),
                ["VergleichGewaehlt"] = new Action<IReadOnlyList<int>>(VergleichSetzen),
                ["Gaben"] = new Func<WirtschaftlichkeitSeite.Unterdialog,
                                     IReadOnlyDictionary<string, object>>(Unterdialog),
                ["Nachlauf"] = new Func<WirtschaftlichkeitSeite.Unterdialog, bool, string>(Nachlauf),

                // AUFTRAG #325 (Anwenderentscheid 17.09.2026): Die nicht monetären
                // Wirkungen werden auf der SEITE gepflegt. ETAPPE E17 (V‑G11): als Liste
                // je Wirkung (Tab_ProjektWirkung) statt als Freitext.
                ["WirkungSpeichern"] = new Func<IReadOnlyList<ProjektWirkung>, bool>(WirkungSpeichern),
                ["Speicherfehlerzeile"] = new Func<string>(Speicherfehlerzeile),

                // KONZEPT § 2.9 und § 2.15: die waehlbare Referenz und die zwei
                // Sichten. Jeder Rueckruf gibt den NEUEN Stand zurueck - die Seite
                // uebernimmt ihn, statt ein zweites Mal zu laden.
                ["ReferenzGewaehlt"] = new Func<int, WirtschaftlichkeitStand>(ReferenzSetzen),
                ["SichtGewaehlt"] = new Func<int, WirtschaftlichkeitStand>(SichtSetzen),
                ["PaarGewaehlt"] = new Func<int, int, WirtschaftlichkeitStand>(PaarSetzen),
                ["PaarTauschen"] = new Func<WirtschaftlichkeitStand>(PaarTauschen),

                ["TitelText"] = T("WIRT_TITEL", "Wirtschaftlichkeit (Kapitalwertmethode DIN EN 17463)")
                                + " — " + T("WIRT_STAMM", "Stamm:") + " " + _stammName,
                ["LabelVarianten"] = T("WIRT_LBL_GRUPPE", "Vergleichsgruppe:"),
                // ETAPPE E5 (U4): Die Klappliste steuert nur die Tafeln darunter und sagt
                // das in ihrer Beschriftung.
                ["LabelSzenario"] = T("WIRT_LBL_EINZELHEITEN", "Einzelheiten anzeigen für Szenario:"),
                ["LabelFortschritt"] = T("BKS_LBL_FORTSCHRITT", "Fortschritt"),
                ["SpalteArt"] = MyResource.Resource.BK_SP_ART,
                ["SpalteBezeichner"] = MyResource.Resource.BK_SP_BEZEICHNER,
                ["SpalteProjektname"] = MyResource.Resource.BK_SP_PROJEKTNAME,
                ["SpalteSpeicher"] = MyResource.Resource.WIRT_ZEILE_SPEICHER,
                ["SpalteSimulation"] = MyResource.Resource.BK_BER_SP_SIMULATION,
                ["PhotovoltaikText"] = T("PVW_KNOPF", "Photovoltaik…"),
                ["BhkwText"] = T("BHW_KNOPF", "BHKW-Wirtschaftlichkeit…"),

                // E3/7: die Titel der Tarif-Ueberlagerung, wenn sie als ZIEL
                // eines Sprungs aufgeht. Sie kommen aus demselben Textbuendel,
                // das der Dialog selbst fuehrt - kein neuer Schluessel.
                ["TarifBhkwText"] = TarifstrukturHuelle.Titel(TarifSicht.Bhkw),
                ["TarifPvText"] = TarifstrukturHuelle.Titel(TarifSicht.Photovoltaik),
                ["ParameterText"] = T("WIRT_BTN_PARAMETER", "Parameter…"),
                // ETAPPE E6 (K8, U3): Der Verlauf steht als eigener Abschnitt in „Wie sicher
                // ist das?" - der Knopf „Verlauf…" und sein Dialog sind entfallen. Der
                // Abschnitt bekommt seine Datenseite aus der Verlaufshülle.
                ["Verlauf"] = Verlauf.Seitenwege(),
                ["BerechnenText"] = T("WIRT_BTN_BERECHNEN", "Berechnen"),
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,
                ["MeldungStammReferenz"] = MyResource.Resource.BK_BER_MSG_STAMM_REFERENZ,
                ["StatusAbgebrochen"] = MyResource.Resource.BK_BER_STATUS_ABGEBROCHEN,
                ["HilfeSchluessel"] = "UcWirtschaftlichkeit.btn_Help"
            };

            // ETAPPE E5 (U44): "Bericht erzeugen" nur mit dem bestehenden Berichtsweg -
            // ohne Rahmenhuelle (Proben, eigenstaendige Einbettung) gibt es den Knopf nicht.
            if (Berichtsweg != null)
            {
                gaben["BerichtErzeugen"] = Berichtsweg;
                gaben["DateiOeffnen"] = new Func<string, Task>(DateiOeffnen);
            }

            // BV-E2 (Konzept 9.5): die Anhang-E-Stellen der gewaehlten Vorlage - nur mit Rahmenhuelle.
            if (AnhangEStellenLaden != null) gaben["AnhangEStellenLaden"] = AnhangEStellenLaden;
            return gaben;
        }

        /// <summary>ETAPPE E5 (U2): die Darstellung der Seite in die Sitzungswahl.</summary>
        private void DarstellungSetzen(int darstellung)
        {
            Vergleich.DarstellungWaehlen(darstellung);
        }

        /// <summary>
        /// ETAPPE E5 (U44): einen erzeugten Bericht öffnen — derselbe Weg wie auf der
        /// Berichtsseite (<c>Dienste.Datei</c>).
        /// </summary>
        private static Task DateiOeffnen(string pfad)
        {
            try { Dienste.Datei.MitSystemOeffnen(pfad); } catch { }
            return Task.CompletedTask;
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
                        Auffaellig = !st.SimStand.HasValue || st.Veraltet,
                        Veraltet = st.Veraltet
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

            // KONZEPT § 2.9 und § 2.15: die waehlbaren Staende, die wirksame Referenz
            // und die Sicht. Die Referenz kommt aus dem Parametersatz und wird gegen
            // die Gruppe aufgeloest - eine geloeschte faellt BENANNT auf den Stamm
            // zurueck. Die Sicht liegt in der Sitzung; steht A oder B nicht mehr im
            // Vergleich, faellt auch sie benannt auf Sicht 1 zurueck.
            var staende = new List<ValueTuple<int, string>>();
            foreach (int id in _gruppe)
                staende.Add(new ValueTuple<int, string>(
                    id, _namen.ContainsKey(id) ? _namen[id] : id.ToString(CultureInfo.InvariantCulture)));
            stand.Staende = staende;

            Referenzwahl wahl = Referenzwahl.Bestimme(
                _gruppe, _idStamm, Gruppenreferenz(),
                id => _namen.ContainsKey(id) ? _namen[id] : "");
            stand.IdReferenz = wahl.IdReferenz;

            // Die Referenz ist immer im Vergleich - sie ist die Unterlassensalternative
            // und kann nicht aus dem Vergleich fallen, gegen den sie gehalten wird.
            if (wahl.IdReferenz > 0 && !gewaehlt.Contains(wahl.IdReferenz))
            {
                gewaehlt.Add(wahl.IdReferenz);
                stand.GewaehlteVarianten = gewaehlt;
            }

            string sichtWarnung = Vergleich.Nachziehen(gewaehlt);
            stand.PaarMoeglich = Vergleichsauswahl.PaarMoeglich(gewaehlt);
            stand.Sicht = Vergleich.Sicht.Sicht;
            stand.SichtA = Vergleich.Sicht.IdA;
            stand.SichtB = Vergleich.Sicht.IdB;
            stand.Referenzzeile = Vergleich.Sicht.IstPaar
                ? Referenzwahl.Nachweiszeile(Name(Vergleich.Sicht.IdA), wahl.Anzeige)
                : Referenzwahl.Nachweiszeile(wahl.Anzeige);
            // ETAPPE E6: Der Verlauf nennt dieselbe Referenz im Excel-Blatt und hält sie im
            // Vergleich, auch wenn sie nicht angehakt ist.
            _referenzzeile = stand.Referenzzeile ?? "";
            _referenzWirksam = wahl.IdReferenz;

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
            // ETAPPE E13 (E7c3‑Q6 a): Der Grund eines abgebrochenen Ladens (Ladefehler des
            // Kerns, sonst der gefangene Fehler) geht in die Statuszeile, statt dass die
            // Zeilen still fehlen.
            var ladefehler = new List<string>();
            try
            {
                _ergebnisse = _ctrl.LadeErgebnisse(new List<int>(_gruppe));
                ladefehler.Add(_ctrl.Ladefehler);
            }
            catch (Exception ex)
            {
                _ergebnisse = new List<WirtschaftlichkeitErgebnis>();
                ladefehler.Add(Fehlergrund.Text(ex));
            }

            // ETAPPE E5 (V‑A): die gespeicherten Sensitivitaetszeilen des Gruppenlaufs -
            // in Sicht 2 ersetzt sie gleich der Lauf gegen A (PaarErgebnisse).
            try
            {
                _sens = _ctrl.LadeSensitivitaet(new List<int>(_gruppe)) ?? new List<SensitivitaetZeile>();
                ladefehler.Add(_ctrl.Ladefehler);
            }
            catch (Exception ex)
            {
                _sens = new List<SensitivitaetZeile>();
                ladefehler.Add(Fehlergrund.Text(ex));
            }

            // ETAPPE E17 (V‑G11): die nicht monetarisierbaren Wirkungen des Stammprojekts -
            // vor den Deklarationen, deren Risikozeile an ihnen haengt. Ein Lesefehler geht
            // wie die uebrigen in die Statuszeile.
            _wirkungen = _wirkungCtrl.Laden(_idStamm);
            ladefehler.Add(_wirkungCtrl.Ladefehler);

            // KONZEPT § 2.15: In Sicht 2 rechnen die Differenzkennzahlen gegen A. Es ist
            // DERSELBE Rechenweg (WirtschaftlichkeitCtrl.Berechne) mit anderer Referenz -
            // nur ohne zu persistieren: Die Paarwahl ist ein Erkundungswerkzeug, der
            // gespeicherte Lauf bleibt der gegen die Unterlassensalternative der Gruppe.
            bool paarLaufFehlt = Vergleich.Sicht.IstPaar && !PaarErgebnisse();

            // AUCH EINE ZEILE MIT FEHLGRUND VERALTET (Anwenderbefund 22.09.2026): Die
            // Frage „passt das Ergebnis zum Simulationsstand“ hat mit der Frage „steht
            // eine Kennzahl“ nichts zu tun. Solange beide hier zusammenhingen, blieb die
            // Statuszeile bei genau den Zeilen stumm, bei denen der Anwender am ehesten
            // neu rechnen muss.
            bool veraltet = _ergebnisse.Count > 0 &&
                            _ergebnisse.Any(x => !_ctrl.ErgebnisAktuell(x));
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

            // KONZEPT § 2.9 und § 2.15: Beide Rueckfaelle werden BENANNT, nie still
            // vollzogen - und der fehlende Lauf der Paarsicht ebenso.
            if (wahl.Warnung != null) stand.Statuszeile += " " + wahl.Warnung;
            if (sichtWarnung != null) stand.Statuszeile += " " + sichtWarnung;
            if (paarLaufFehlt) stand.Statuszeile += " " + MyResource.Resource.WIRT_SICHT_LAUF_NOETIG;

            // ETAPPE E13 (E7c3‑Q6 a): Ladefehler, Speicherfehler und Vorsorgewarnung des
            // Kerns — jeder Grund einmal, mit dem Text des Kerns. Der Speicherfehler stammt
            // aus dem Schreibweg, der dieses Laden auslöste (Referenzwahl), und gilt einmal.
            foreach (string zeile in Fehlergrund.Anzeigezeilen(ladefehler, _speicherfehler,
                                                                WirtschaftlichkeitCtrl.Vorsorgewarnung))
                stand.Statuszeile += " " + zeile;
            _speicherfehler = null;

            WirtschaftlichkeitCtrl.ErzeugerFlags flags = null;
            try { flags = _ctrl.ErzeugerDerGruppe(_idStamm); }
            catch { }

            stand.MitPhotovoltaik = flags != null && flags.Photovoltaik;
            stand.MitBhkw = flags != null && flags.Bhkw;

            // ETAPPE W5-B-11 (Anwenderentscheid 09.09.2026): die zwei VALERI-Ausweise
            // des Nachweisblocks. Sie haengen am Projekt, nicht an der Wahl - deshalb
            // stehen sie am Stand und nicht an der Ansicht.
            // ETAPPE E5 (U39): Die Positionen sammelt seither der KERN
            // (NutzungsdauerHinweisCtrl) - dieselbe Stelle liefert dem Bericht seine
            // Zeile, und sie bildet dazu die Hinweiszeilen „k von n Positionen ohne
            // Nutzungsdauer" (Konzept § 2.13 (3)).
            NutzungsdauerHinweise nutzungsdauer = Nutzungsdauerhinweise();
            stand.Zeitraumzeile = nutzungsdauer.Zeitraumzeile;
            stand.Nutzungsdauerhinweise = nutzungsdauer.Zeilen;
            stand.Vereinfachungszeile = Vereinfachungszeile(stand.MitPhotovoltaik);

            // ETAPPE E5 (U10, V‑A) und E9b (E9b-Q3): unter der Annahmentafel der Ausweis
            // "n von m Parametern szenariert" (an der Stelle des Hinweistexts) und die
            // Deklarationszeilen der Bewertung - beide an der Gruppe, nicht an der Wahl.
            stand.Szenarioabdeckung = Szenarioabdeckung();
            stand.Deklarationen = Deklarationen();

            // ETAPPE E5 Teil b (U2): die Annahmentafel ueber dem Ausweis und der
            // Umschalter als Sitzungswahl.
            stand.Annahmen = Annahmentafel();
            stand.Darstellung = Vergleich.Darstellung;

            // ETAPPE W5-B-12 (Anwenderentscheid 09.09.2026, VALERI-Luecke G6), Form
            // aus AUFTRAG #328: der Freitext "Nicht monetaere Wirkungen" geht ROH
            // hinueber und nur einmal. Die Seite weist ihn im Kopf ihres
            // Bewertungsblocks aus und pflegt ihn dort; eine zweite, hier fertig
            // formulierte Zeile fuer den Nachweisblock gibt es nicht mehr. Er haengt
            // wie die zwei Zeilen darueber am PROJEKT und nicht an der Szenario-
            // oder Vergleichswahl.
            stand.NichtMonetaer = NichtMonetaer();

            // ETAPPE E17 (V‑G11): die Liste der Wirkungen - eine KOPIE je Zeile, damit der
            // Arbeitsstand der Seite den geladenen Stand nicht mitverändert.
            stand.Wirkungen = _wirkungen.Select(w => w.Kopie()).ToList();

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

        // =====================================================================
        // KONZEPT § 2.9 und § 2.15 — Referenz und Vergleichssicht
        // =====================================================================

        /// <summary>Die gespeicherte Referenz der Gruppe; 0 = Stamm.</summary>
        private int Gruppenreferenz()
        {
            try { return _ctrl.LadeParameter(_idStamm).IdReferenzprojekt; }
            catch { return 0; }
        }

        /// <summary>Der Anzeigename eines Standes der Gruppe.</summary>
        private string Name(int idProjekt)
        {
            return _namen.ContainsKey(idProjekt) ? _namen[idProjekt] : "";
        }

        /// <summary>
        /// KONZEPT § 2.15: die Ergebnisse der Sicht 2 — DERSELBE Rechenweg mit A als
        /// Referenz, auf den Eingangsdaten des letzten Laufs und <b>ohne zu
        /// persistieren</b>. Ohne Lauf in dieser Sitzung gibt es sie nicht; dann sagt
        /// die Statuszeile es, statt die Zahlen des Gruppenlaufs als Paarvergleich
        /// auszugeben.
        /// </summary>
        /// <returns>true, wenn die Paarsicht gerechnet werden konnte.</returns>
        private bool PaarErgebnisse()
        {
            if (_letzteDaten == null) return false;
            try
            {
                _letzteDaten.Sicht = Vergleich.Sicht.Kopie();
                // ETAPPE E5 (V‑A): mit den Sensitivitaetszeilen DIESES Laufs - gegen A,
                // wie die Differenzen daneben.
                List<SensitivitaetZeile> sens;
                _ergebnisse = _ctrl.Berechne(_letzteDaten, _ctrl.LadeParameter(_idStamm),
                                             Vergleich.Sicht.Referenz, false, out sens);
                _sens = sens ?? new List<SensitivitaetZeile>();
                return true;
            }
            catch { return false; }
        }

        /// <summary>
        /// KONZEPT § 2.9: Die Referenz der Gruppe wechselt — sie wird GESPEICHERT
        /// (<c>ID_Referenzprojekt</c>), weil sie wie Zins und Zeitraum zur Gruppe
        /// gehört und der Bericht aus der Datenbank reproduzierbar bleiben soll.
        /// Danach liest die Seite neu; die Differenzkennzahlen folgen dem nächsten Lauf.
        /// </summary>
        private WirtschaftlichkeitStand ReferenzSetzen(int idReferenz)
        {
            try
            {
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                p.IdReferenzprojekt = idReferenz == _idStamm ? 0 : idReferenz;
                // ETAPPE E13 (E7c3‑Q6 a): Scheitert das Speichern, nennt die Statuszeile
                // des folgenden Ladens den Grund.
                if (!_ctrl.SpeichereParameter(p)) _speicherfehler = _ctrl.Speicherfehler;
                _parameterCache = null;
            }
            catch (Exception ex) { _speicherfehler = Fehlergrund.Text(ex); }
            return Laden();
        }

        /// <summary>KONZEPT § 2.15: die Sicht wechseln (0 = alle, 1 = zwei Stände).</summary>
        private WirtschaftlichkeitStand SichtSetzen(int sicht)
        {
            Vergleich.SichtWaehlen(sicht, Vergleich.Gewaehlte(_gruppe, _idStamm),
                                   Gruppenreferenz(), _idStamm);
            return Laden();
        }

        /// <summary>KONZEPT § 2.15: A und B der Sicht 2 setzen (A ≠ B ist gesichert).</summary>
        private WirtschaftlichkeitStand PaarSetzen(int idA, int idB)
        {
            Vergleich.PaarWaehlen(idA, idB);
            return Laden();
        }

        /// <summary>KONZEPT § 2.15 (VG‑Q7): A und B tauschen — das dreht das Vorzeichen.</summary>
        private WirtschaftlichkeitStand PaarTauschen()
        {
            Vergleich.Tauschen();
            return Laden();
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (VALERI-Lücke G7): der Betrachtungszeitraum gegen die
        /// Nutzungsdauern der Investitionspositionen — gelesen im Szenario ERWARTET,
        /// also aus derselben Liste, mit der der Kapitalwert rechnet. Ein Lesefehler
        /// lässt die Zeile still entfallen; sie ist Ausweis, kein Ergebnis.
        ///
        /// <para><b>ETAPPE E5 (U39):</b> Das Einsammeln der Positionen stand bis hierher
        /// in dieser Hülle; es liegt seither im Kern (<see cref="NutzungsdauerHinweisCtrl"/>),
        /// der zugleich die Hinweiszeilen „k von n Positionen ohne Nutzungsdauer" bildet —
        /// derselbe Aufruf, den der Berichtsdatensammler für Wort- und Tabellenbericht
        /// nimmt.</para>
        /// </summary>
        private NutzungsdauerHinweise Nutzungsdauerhinweise()
        {
            try
            {
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                var staende = new List<KeyValuePair<int, string>>();
                foreach (int id in _gruppe)
                    staende.Add(new KeyValuePair<int, string>(id, Name(id)));
                return NutzungsdauerHinweisCtrl.Bilde(p.Betrachtungszeitraum, staende,
                                                      BerichtTexte.Kultur);
            }
            catch { return new NutzungsdauerHinweise(); }
        }

        /// <summary>
        /// ETAPPE E9b (U10, Konzept § 2.11.5 und § 2.11.7; E9b‑Q2, E9b‑Q3): der Ausweis
        /// „n von m Parametern szenariert" unter der Annahmentafel — an der Stelle des
        /// Hinweistexts, den die Pflege in den Dialogen überflüssig macht. Gezählt wird
        /// über die GANZE Vergleichsgruppe, nicht über die Wahl — dieselben Stände wie die
        /// Nutzungsdauer-Hinweise darüber; die Regel steht im Kern
        /// (<see cref="SzenarioAbdeckung.Lesen"/>). Ein Lesefehler kostet die Zeile.
        /// </summary>
        private string Szenarioabdeckung()
        {
            try
            {
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                var staende = new List<KeyValuePair<int, string>>();
                foreach (int id in _gruppe)
                    staende.Add(new KeyValuePair<int, string>(id, Name(id)));
                return SzenarioAbdeckung.Lesen(p, staende).Satz(BerichtTexte.Kultur);
            }
            catch { return ""; }
        }

        /// <summary>
        /// ETAPPE E5 (V‑A): die Deklarationszeilen der Bewertung als Texte. Die Risikozeile
        /// folgt dem gepflegten Text der nicht monetären Wirkungen (Empfehlung Q5): ohne
        /// Text „keine benannt".
        /// </summary>
        private List<string> Deklarationen()
        {
            var texte = new List<string>();
            try
            {
                // ETAPPE E15 (V‑G7): mit dem Parametersatz, damit die Risikozeile ein
                // gepflegtes Risiko nennt. ETAPPE E17 (V‑G11): „benannt" heißt: mindestens
                // eine Wirkung der Liste trägt eine Beschreibung — das Altfeld zählt nicht mit.
                WirtschaftlichkeitParameter p = null;
                try { p = _ctrl.LadeParameter(_idStamm); } catch { p = null; }
                foreach (ValeriDeklaration d in ValeriAusweis.Deklarationen(
                             NichtMonetaereWirkungen.Kurztext(_wirkungen), p))
                    texte.Add(d.Text);
            }
            catch { }
            return texte;
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
        /// ETAPPE E17 (V‑G11): der Controller der nicht monetarisierbaren Wirkungen und die
        /// zuletzt geladene Liste des Stammprojekts (Quelle der Deklaration „benannt").
        /// </summary>
        private readonly ProjektWirkungCtrl _wirkungCtrl = new ProjektWirkungCtrl();
        private List<ProjektWirkung> _wirkungen = new List<ProjektWirkung>();

        /// <summary>
        /// ETAPPE E17 (V‑G11): Schreibt die Liste der nicht monetarisierbaren Wirkungen des
        /// Stammprojekts (<see cref="ProjektWirkungCtrl.Speichern"/> ersetzt sie in einem
        /// Vorgang). Das Freitextfeld (Altfeld) bleibt unberührt. Der Grund eines
        /// Scheiterns — auch ein Prüfbefund wie „Jede Wirkung braucht eine Beschreibung" —
        /// geht über <see cref="Speicherfehlerzeile"/> in die Statuszeile.
        /// </summary>
        private bool WirkungSpeichern(IReadOnlyList<ProjektWirkung> liste)
        {
            try
            {
                if (!_wirkungCtrl.Speichern(_idStamm, liste))
                {
                    _speicherfehler = _wirkungCtrl.Speicherfehler;
                    return false;
                }
                _wirkungen = _wirkungCtrl.Laden(_idStamm);
                return true;
            }
            catch (Exception ex) { _speicherfehler = Fehlergrund.Text(ex); return false; }
        }

        /// <summary>
        /// ETAPPE E13 (E7c3‑Q6 a) — der Grund des zuletzt gescheiterten Speicherns
        /// (<see cref="WirtschaftlichkeitCtrl.Speicherfehler"/>, sonst der gefangene Fehler);
        /// <c>null</c> = keiner. Die Statuszeile zeigt ihn einmal.
        /// </summary>
        private string _speicherfehler;

        /// <summary>
        /// ETAPPE E13 (E7c3‑Q6 a) — die Statuszeile nach einem gescheiterten
        /// <see cref="WirkungSpeichern"/>: der Grund des Kerns („Speichern gescheitert: …"),
        /// leer ohne bekannten Grund. Gelesen wird er einmal.
        /// </summary>
        private string Speicherfehlerzeile()
        {
            List<string> zeilen = Fehlergrund.Anzeigezeilen(null, _speicherfehler, null);
            _speicherfehler = null;
            return zeilen.Count > 0 ? zeilen[0] : "";
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
        /// <para>W3: Rollentarif und KWKG-Split brauchen Stundenreihen; dann wird je
        /// Projekt frisch in-memory simuliert. SP-W1: derselbe Grund für den
        /// Leistungspreis des Stromträgers samt Staffel — seine Basis ist die
        /// Bezugsspitze aus der Viertelstundenreihe, und die gibt es nur aus dem
        /// frischen Lauf. LS-E-2: Der
        /// Leistungspreis zählt für die GANZE Gruppe, nicht nur für den Stamm — sonst
        /// fiele er einer Variante still weg, die ihn als Einzige führt.</para>
        /// </summary>
        private bool MitZeitreihen(WirtschaftlichkeitParameter p, TarifParameter tarif)
        {
            // BK1: dieselbe EINE Regel wie im Kern und in der Verlaufshülle; Q11 (E7b):
            // nur ein WIRKSAMER Tarifsatz (Rollentarif) braucht die Reihen.
            return tarif.Wirksam || KwkgAktivierung.IstAktiv(_idStamm, _gruppe) ||
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
            // KONZEPT § 2.15: In Sicht 2 sind es genau zwei Spalten, A und B.
            List<int> gewaehlt = Vergleich.Sicht.Spalten(Vergleich.Gewaehlte(_gruppe, _idStamm));
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

            // KONZEPT § 2.9 und § 2.15: die REFERENZ dieser Ansicht - in Sicht 2 der
            // Stand A, sonst die Referenz der Gruppe. Tafel, Bandbreite, Karten und
            // Vorschlag rechnen alle gegen sie; sie steht deshalb vor allen vieren.
            int idReferenz = Vergleich.Sicht.IstPaar
                           ? Vergleich.Sicht.IdA
                           : Referenzwahl.Bestimme(_gruppe, _idStamm, Gruppenreferenz(), null).IdReferenz;

            // ETAPPE E5 (U4, U5): die Bandbreite dreier Szenarien und die Einstufung je
            // Version - EIN Modell des Kerns (WirtschaftlichkeitBandbreite), aus dem auch
            // der Vorschlagssatz entsteht. Es urteilt ueber ALLE drei Szenarien der
            // GEWAEHLTEN Versionen - nicht nur ueber das angezeigte (W5-B-11).
            WirtschaftlichkeitBandbreite bandbreite = Bandbreite(gewaehlt, idReferenz);

            // ETAPPE E5 Teil b (U2): Karten und Kennzahltafel stehen in "Lohnt es sich?"
            // UEBER der Szenario-Klappliste - sie zeigen deshalb den Erwartungsfall, gleich
            // welches Szenario die Klappliste gerade zeigt ("Die Klappliste steuert nur die
            // Tafeln darunter").
            // Die Spalten folgen der Wahl, nicht dem Szenario: Eine Spalte ohne Ergebnis im
            // gezeigten Szenario kann trotzdem eines im Erwartungsfall tragen. Ohne
            // Gruppenliste (Laden fehlgeschlagen) sind es die Staende der gezeigten Spalten.
            var spaltenIds = new List<int>();
            if (gewaehlt.Count > 0) spaltenIds.AddRange(gewaehlt);
            else foreach (WirtschaftlichkeitErgebnis e in spaltenErg) spaltenIds.Add(e.IdProjekt);
            var spaltenErwartet = new List<WirtschaftlichkeitErgebnis>();
            var zeilenErwartet = new List<WirtschaftlichkeitErgebnis>();
            foreach (int id in spaltenIds)
            {
                WirtschaftlichkeitErgebnis erw = _ergebnisse.FirstOrDefault(
                    x => x.IdProjekt == id && x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                spaltenErwartet.Add(erw);
                if (erw != null) zeilenErwartet.Add(erw);
            }

            var staendeDerAnsicht = new List<KeyValuePair<int, string>>();
            foreach (int id in gewaehlt) staendeDerAnsicht.Add(new KeyValuePair<int, string>(id, Name(id)));

            var ansicht = new ErgebnisAnsicht
            {
                // BV-E3: Welchen Stand die Karten zeigen, wählt die Regel des Kerns
                // (BesteVariante) über die Spalten im Erwartungsfall — dieselbe Regel füllt
                // im Bericht wirtschaft.beste.*.
                Kacheln = Kacheln(BesteVariante.Waehle(_ergebnisse, _idStamm, spaltenIds), kultur),
                Szenariozeile = Szenariozeile(szenario, kultur),   // W5-B-9
                // ETAPPE E5 (U5): Die Empfehlungszeile wird aus DEMSELBEN Modell
                // gespeist wie die Karten - mit der Referenz beim Namen, wie im Bericht.
                Empfehlungszeile = Empfehlungszeile(bandbreite, kultur),
                Empfehlungen = Karten(bandbreite, kultur),
                Bandbreite = BandbreitenTafel(bandbreite, kultur),
                // ETAPPE E5 Teil b: der Fusstext der Bandbreite (wie im Bericht), die
                // Sensitivitaet mit Steigung, die Nr.-31-Zeile und der Rahmen der
                // ValERI-Ansicht (Block 1).
                Bandbreitenfuss = bandbreite == null || bandbreite.Leer ? ""
                    : string.Format(kultur, MyResource.Resource.WIRT_SZ_DELTA_FUSS, bandbreite.Referenzname),
                // ETAPPE E6 (Nachtrag E5b, Frage (4)): das Spannenbild neben der Tafel.
                Spannenbild = Spannenbild(bandbreite),
                Sensitivitaet = SensitivitaetTafel(staendeDerAnsicht, idReferenz, kultur),
                Nachweiszeile = WirtschaftlichkeitBewertung.Nachweiszeile(
                    WirtschaftlichkeitBewertung.StaendeOhneNachweis(staendeDerAnsicht, _ergebnisse)),
                Rahmen = Rahmentafel(gewaehlt, idReferenz, kultur),
                // BV-E6: die Gliederung im gezeigten Szenario — der Bericht führt sie als
                // tabelle.wirtschaft.kennzahlen je Szenario.
                GliederungVorlagenfeld = "tabelle.wirtschaft.kennzahlen" + GliederungsAnhang(szenario)
            };

            // ETAPPE E8a (Konzept § 2.11.4 V‑C, Mockup Kategorie 8): die ZAHLUNGSREIHEN der
            // gezeigten Stände (ValERI-Block 2) — aus den Zahlungsbildern des Laufs dieser
            // Sitzung (der Verlauf rechnet sie mit „Berechnen" ohnehin mit), nur wo sie zum
            // gespeicherten Ergebnis passen. Gerechnet wird hier nichts; die Leitversion ist
            // die Regel des Kerns. U42: Jede Tafel trägt ihr Zahlungsstrombild.
            var staendeSpalten = new List<KeyValuePair<int, string>>();
            foreach (int id in spaltenIds) staendeSpalten.Add(new KeyValuePair<int, string>(id, Name(id)));
            Zahlungsgliederungen gliederungen = Gliederungen();
            ansicht.Leitversion = Zahlungsgliederungen.Leitversion(_ergebnisse, spaltenIds, idReferenz);
            var szenarionamen = new List<string>();
            for (int i = 0; i < SZENARIEN.Length; i++) szenarionamen.Add(SzenarioAnzeige(i));
            ansicht.Zahlungsreihen = ZahlungsreihenAnsicht.Jahrestafeln(gliederungen, staendeSpalten, SZENARIEN, kultur,
                                                                        szenarionamen);
            ansicht.Zahlungsstaende = ZahlungsreihenAnsicht.Staende(ansicht.Zahlungsreihen, staendeSpalten);
            ansicht.Zahlungshinweis = ZahlungsreihenAnsicht.Hinweis(gliederungen, staendeSpalten, kultur);

            // ETAPPE E8a (U46): die Gliederung des Kapitalwerts im GEWÄHLTEN Szenario — Barwert
            // und Nominalsumme je Bestandteil, die Differenzspalte Leitversion − Referenz. Sie
            // steht unter der Szenario-Klappliste und folgt ihr (SzenarioTeileUebernehmen).
            ansicht.Bestandteile = ZahlungsreihenAnsicht.Bestandteile(gliederungen, szenario, staendeSpalten,
                                                                      idReferenz, ansicht.Leitversion, kultur);
            string szenarioname = SzenarioAnzeige(Math.Max(0, Array.IndexOf(SZENARIEN, szenario)));
            if (ansicht.Bestandteile.Zeilen.Count > 0)
            {
                ansicht.BestandteileTitel = string.Format(kultur, MyResource.Resource.WIRT_GL_TITEL, szenarioname);
                ansicht.BestandteileUnterzeile = ZahlungsreihenAnsicht.Unterzeile(gliederungen, szenario,
                                                                                  staendeSpalten, kultur);
            }

            // ETAPPE E8a (U41): das Brückenbild — dieselben Schritte wie die Differenzspalte,
            // gezeichnet vom Renderer des Kerns; es folgt wie sie der Szenario-Klappliste.
            try
            {
                ansicht.Bruecke = ZahlungsreihenAnsicht.Bruecke(gliederungen, szenario, ansicht.Leitversion,
                                                                idReferenz, staendeSpalten, szenarioname, kultur);
            }
            catch { ansicht.Bruecke = null; }

            // ETAPPE E8a (U47): „Was daraus im Lauf wird" — die drei Szenarioläufe der
            // Bandbreite, ihre Wirkung auf die Leitversion.
            ansicht.Laufwirkung = ZahlungsreihenAnsicht.Laufwirkung(gliederungen, ansicht.Leitversion,
                                                                    Name(ansicht.Leitversion), kultur);

            // ETAPPE E8a (U48): die Fußzeile von „Was ist angenommen?" — wie viele Szenarien der
            // gezeigten Stände gerechnet sind und woher ihre Annahmen kommen (Regel des Kerns).
            ansicht.Szenariofuss = Szenariofuss(spaltenIds, kultur);

            var spalten = new List<string> { T("WIRT_SP_KENNZAHL", "Kennzahl") };
            for (int i = 0; i < spaltenErg.Count; i++)
            {
                WirtschaftlichkeitErgebnis erg = spaltenErg[i];
                int id = erg != null ? erg.IdProjekt : gewaehlt[i];
                spalten.Add(_namen.ContainsKey(id) ? _namen[id]
                            : erg == null ? id.ToString(CultureInfo.InvariantCulture)
                            : (erg.IstStamm ? MyResource.Resource.BK_ART_STAMM : erg.Anzeige));
            }

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
            // KONZEPT § 2.9 und § 2.15: Die Zeilendefinition kennzeichnet die REFERENZ -
            // in Sicht 2 den Stand A, sonst die Referenz der Gruppe (idReferenz oben).
            List<WirtZeile> definition = WirtschaftlichkeitZeilen.Sichtbare(
                WirtschaftlichkeitZeilen.Kennzahlen(_ergebnisse, _tarifCache, idReferenz), _ergebnisse);

            // ETAPPE E5 Teil b (U2, V‑A): die KENNZAHLTAFEL im Erwartungsfall - dieselben
            // Zeilen der Definition, mit Label "nachrichtlich" und Zellwarnung.
            if (zeilenErwartet.Count > 0)
            {
                var kennzahlen = new List<MatrixZeile>();
                foreach (WirtZeile z in definition)
                    if (WirtschaftlichkeitZeilen.IstKennzahl(z.Schluessel))
                        kennzahlen.Add(Matrixzeile(z, spaltenErwartet, kultur, MatrixZeile.ABSCHNITT_KENNZAHL));
                ansicht.Kennzahltafel = new ErgebnisMatrix { Spalten = spalten, Zeilen = kennzahlen };
            }
            if (zeilen.Count == 0) return ansicht;

            var matrixzeilen = new List<MatrixZeile>();

            // ETAPPE E5 Teil b (U2): Jede Zeile traegt ihren ABSCHNITT - die Kennzahlen
            // stehen in "Lohnt es sich?", alles uebrige gliedert den Kapitalwert ("Woraus
            // entsteht die Zahl?"); der Nettobarwert ist beides und bleibt als Summe der
            // Gliederung stehen.
            foreach (WirtZeile z in definition)
            {
                string abschnitt = WirtschaftlichkeitZeilen.IstKennzahl(z.Schluessel) &&
                                   !string.Equals(z.Schluessel, WirtschaftlichkeitZeilen.GLIEDERUNGSSUMME,
                                                  StringComparison.Ordinal)
                                 ? MatrixZeile.ABSCHNITT_KENNZAHL
                                 : MatrixZeile.ABSCHNITT_GLIEDERUNG;
                matrixzeilen.Add(Matrixzeile(z, spaltenErg, kultur, abschnitt));
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

            // Hinweiszeilen (nicht-fatal W3 / unvollstaendige Rechnungen). ETAPPE E5 Teil b:
            // Sie stehen unter der Gliederung und speisen das Warnband (Abschnitt HINWEIS).
            string hinweis = T("WIRT_ZEILE_HINWEIS", "Hinweis");
            if (zeilen.Any(x => x.Hinweis != null))
                matrixzeilen.Add(Hinweiszeile(Zeile(hinweis, spaltenErg, x => x.Hinweis != null ? "⚠ " + x.Hinweis : "")));
            if (zeilen.Any(x => x.Fehlgrund != null))
                matrixzeilen.Add(Hinweiszeile(Zeile(hinweis, spaltenErg, x => x.Fehlgrund != null ? "⚠ " + x.Fehlgrund : "")));

            // W5-B-5: die gewaehlte Version ohne Ergebnis sagt es in ihrer Spalte.
            if (spaltenErg.Any(e => e == null))
            {
                string nichtBerechnet = "⚠ " + T("WIRT_MSG_NICHT_BERECHNET", "nicht berechnet — bitte „Berechnen“");
                var zellen = new List<string>();
                foreach (WirtschaftlichkeitErgebnis e in spaltenErg) zellen.Add(e == null ? nichtBerechnet : "");
                matrixzeilen.Add(Hinweiszeile(new MatrixZeile { Titel = hinweis, Zellen = zellen }));
            }

            // ETAPPE E5 (Konzept § 6.3 Nr. 31, entschieden 22.09.2026): Ergebniszeilen OHNE
            // Nachweisumschlag - kein Nachziehlauf, keine Warnung: Der Umschlag entsteht mit
            // der naechsten Rechnung. Seit Teil b steht das als EINE Zeile unter den
            // Annahmen (ErgebnisAnsicht.Nachweiszeile, oben), nicht mehr als Hinweiszeile
            // der Tabelle - dieselbe Aussage zweimal auf einer Seite waere eine zu viel.

            // ETAPPE E2 (Befund R6): Die Kohärenzzeilen stehen seither im EINEN
            // Zeilenkatalog des Kerns (WirtschaftlichkeitZeilen) und kommen oben mit
            // allen anderen Zeilen herein — damit zeigen Reiter, Wortbericht und
            // Excelblatt dieselben Zeilen. Der zweite Aufbau an dieser Stelle war die
            // Stelle, an der die drei Ausgaben auseinanderliefen.

            ansicht.Matrix = new ErgebnisMatrix { Spalten = spalten, Zeilen = matrixzeilen };
            return ansicht;
        }

        /// <summary>
        /// ETAPPE E8a: die Gliederungen der drei Läufe über den Betrachtungszeitraum — aus der
        /// Verlaufshülle, abgeglichen gegen die Ergebnisse der Seite
        /// (<see cref="KapitalwertVerlaufHuelle.GliederungenUeberT"/>). <c>null</c> = in dieser
        /// Sitzung ist nichts gerechnet, oder das Lesen scheiterte — dann stehen die
        /// Zahlungsreihen nicht da, und die Seite sagt es; eine Kennzahl hängt nie daran.
        /// </summary>
        private Zahlungsgliederungen Gliederungen()
        {
            try { return Verlauf.GliederungenUeberT(_ergebnisse); }
            catch { return null; }
        }

        /// <summary>
        /// ETAPPE E8a (U48): die Fußzeile „Drei Szenarien gerechnet · Annahmen aus Vorgaben,
        /// nichts gepflegt" — gezählt werden die Szenarien, für die einer der gezeigten Stände
        /// ein Ergebnis trägt; die Herkunft der Annahmen nennt der Kern
        /// (<see cref="ValeriAusweis.Szenarienfuss"/>). Ein Lesefehler lässt die Herkunft weg.
        /// </summary>
        private string Szenariofuss(List<int> staende, CultureInfo kultur)
        {
            int gerechnet = 0;
            foreach (string s in SZENARIEN)
                if (_ergebnisse.Any(e => e.Szenario == s && staende.Contains(e.IdProjekt))) gerechnet++;
            WirtschaftlichkeitParameter p = null;
            try { p = _ctrl.LadeParameter(_idStamm); }
            catch { }
            try { return ValeriAusweis.Szenarienfuss(gerechnet, p, kultur); }
            catch { return ""; }
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
        /// ETAPPE E5 (U4, U5): die Bandbreite der GEWÄHLTEN Versionen über alle drei
        /// Szenarien — das Modell des Kerns (<see cref="WirtschaftlichkeitBandbreite"/>),
        /// gebildet aus dem Lauf, der schon im Speicher liegt (gespeichert oder, in
        /// Sicht 2, gegen A ohne Persistenz gerechnet). Gerechnet wird hier nichts.
        /// </summary>
        private WirtschaftlichkeitBandbreite Bandbreite(List<int> gewaehlt, int idReferenz)
        {
            try
            {
                List<int> ids = gewaehlt != null && gewaehlt.Count > 0
                    ? gewaehlt : new List<int>(_gruppe);
                var staende = new List<KeyValuePair<int, string>>();
                foreach (int id in ids)
                    staende.Add(new KeyValuePair<int, string>(id, Name(id)));
                return WirtschaftlichkeitBandbreite.Bilde(staende, _ergebnisse, idReferenz, Name(idReferenz));
            }
            catch { return new WirtschaftlichkeitBandbreite(); }
        }

        /// <summary>
        /// ETAPPE E6 (Nachtrag E5b, Anwenderentscheid 22.09.2026 zu Frage (4)): das
        /// <b>Spannenbild</b> aus DERSELBEN Bandbreite wie die Tafel — je Version ein Balken,
        /// der Erwartungsfall als Punkt, die Referenz als Nulllinie. Gezeichnet vom Renderer
        /// des Kerns (<see cref="ChartRenderer.KapitalwertSpanneModell"/>); ohne Bandbreite
        /// oder bei einem Fehler kein Bild — die Tafel bleibt.
        /// </summary>
        private static WindowsFormsApplication1.Zeichnung.Zeichenmodell Spannenbild(
            WirtschaftlichkeitBandbreite bandbreite)
        {
            try
            {
                if (bandbreite == null || bandbreite.Leer) return null;
                return ChartRenderer.KapitalwertSpanneModell(
                    ChartRenderer.Spannenbalken.Aus(bandbreite), bandbreite.Referenzname,
                    ChartRenderer.SpannenTexte.AusRessourcen());
            }
            catch { return null; }
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026, VALERI-Lücke G9): der
        /// Vorschlag zur Entscheidung über die GEWÄHLTEN Versionen.
        ///
        /// <para>Die Regel steht im Kern (<see cref="WirtschaftlichkeitEmpfehlung"/>) —
        /// Seite, Word und Excel sollen denselben Satz zeigen. Leer heißt „keine
        /// Variante mit Erwartet-Ergebnis"; dann zeichnet die Seite die Zeile nicht.</para>
        ///
        /// <para><b>ETAPPE E5 (U5):</b> Der Satz entsteht aus den Einstufungen der
        /// Bandbreite — denselben, die die Empfehlungskarten tragen — und nennt die
        /// Referenz beim Namen, wie der Bericht seit E2.</para>
        /// </summary>
        private static string Empfehlungszeile(WirtschaftlichkeitBandbreite bandbreite, CultureInfo kultur)
        {
            try
            {
                if (bandbreite == null) return "";
                return WirtschaftlichkeitEmpfehlung.Vorschlagstext(
                    bandbreite.Urteile, kultur, bandbreite.Referenzname);
            }
            catch { return ""; }
        }

        /// <summary>
        /// ETAPPE E5 (U5): die Empfehlungskarten — je Version mit Erwartet-Ergebnis ihre
        /// Stufe und ihre Kapitalwertdifferenz zur Referenz im Szenario Erwartet, in der
        /// Reihenfolge der Gruppe.
        /// </summary>
        private static List<EmpfehlungKarte> Karten(WirtschaftlichkeitBandbreite bandbreite,
                                                    CultureInfo kultur)
        {
            var karten = new List<EmpfehlungKarte>();
            if (bandbreite == null) return karten;
            foreach (BandbreitenZeile z in bandbreite.Zeilen)
            {
                VariantenEmpfehlung u = z.Urteil;
                if (u == null) continue;
                karten.Add(new EmpfehlungKarte
                {
                    Name = z.Anzeige,
                    Stufe = u.Stufe == EmpfehlungStufe.Empfohlen ? EmpfehlungKarte.STUFE_JA
                          : u.Stufe == EmpfehlungStufe.Bedingt ? EmpfehlungKarte.STUFE_BEDINGT
                          : EmpfehlungKarte.STUFE_NEIN,
                    StufeText = u.StufeText,
                    Differenz = WirtschaftlichkeitEmpfehlung.Geld(u.DiffErwartet, kultur),
                    BandbreiteFehlt = u.BandbreiteFehlt
                });
            }
            return karten;
        }

        /// <summary>
        /// ETAPPE E5 (U4): die Bandbreite als Tafel der Seite — Version, Ungünstig,
        /// Erwartet, Günstig, Spanne, Einstufung (Mockup Kategorie 8, „Wie sicher ist
        /// das?"); darüber die Referenzzeile. Die Werte bleiben die des Modells, formatiert
        /// wie die Kennzahltafel; ohne Zahl steht „—".
        /// </summary>
        private static ErgebnisMatrix BandbreitenTafel(WirtschaftlichkeitBandbreite bandbreite,
                                                       CultureInfo kultur)
        {
            var tafel = new ErgebnisMatrix();
            if (bandbreite == null || bandbreite.Leer) return tafel;

            tafel.Spalten = new List<string>
            {
                MyResource.Resource.WIRT_SZ_SP_VARIANTE,
                MyResource.Resource.WIRT_SZEN_WORST,
                MyResource.Resource.WIRT_SZEN_ERWARTET,
                MyResource.Resource.WIRT_SZEN_BEST,
                MyResource.Resource.WIRT_SZ_SP_SPANNE,
                MyResource.Resource.WIRT_EMPF_SPALTE
            };

            var zeilen = new List<MatrixZeile>();
            string referenz = MyResource.Resource.WIRT_ZEILE_STAMM_REFERENZ;
            zeilen.Add(new MatrixZeile
            {
                Titel = bandbreite.Referenzname,
                Zellen = new List<string> { referenz, referenz, referenz, "—", "—" }
            });
            foreach (BandbreitenZeile z in bandbreite.Zeilen)
                zeilen.Add(new MatrixZeile
                {
                    Titel = z.Anzeige,
                    Zellen = new List<string>
                    {
                        W(z.Worst, "N0", kultur),
                        W(z.Erwartet, "N0", kultur),
                        W(z.Best, "N0", kultur),
                        W(z.Spanne, "N0", kultur),
                        z.Urteil != null ? z.Urteil.StufeText : "—"
                    }
                });
            tafel.Zeilen = zeilen;
            return tafel;
        }

        private static MatrixZeile Zeile(string titel, List<WirtschaftlichkeitErgebnis> zeilen,
                                         Func<WirtschaftlichkeitErgebnis, string> wert)
        {
            var zellen = new List<string>();
            foreach (WirtschaftlichkeitErgebnis erg in zeilen) zellen.Add(erg == null ? "—" : wert(erg));
            return new MatrixZeile { Titel = titel, Zellen = zellen };
        }

        /// <summary>
        /// ETAPPE E5 Teil b: eine Zeile der Definition als Matrixzeile — Titel (mit
        /// Einzug), je Spalte die Anzeige („— ‹Grund›" ohne Wert, Q16), der Abschnitt, das
        /// Label „nachrichtlich" (V‑3) und je Zelle die Warnung (V‑A, mehrdeutiger
        /// Zinsfuß). Eine Überschrift trägt leere Zellen, eine Spalte ohne Ergebnis „—".
        /// </summary>
        private static MatrixZeile Matrixzeile(WirtZeile z, List<WirtschaftlichkeitErgebnis> spalten,
                                               CultureInfo kultur, string abschnitt)
        {
            var zellen = new List<string>();
            var warnungen = new List<string>();
            foreach (WirtschaftlichkeitErgebnis e in spalten)
            {
                zellen.Add(e == null ? "—" : z.IstUeberschrift ? "" : z.Anzeige(e, kultur));
                warnungen.Add(e == null || z.IstUeberschrift ? "" : z.Warnung(e));
            }
            return new MatrixZeile
            {
                Titel = (z.Einzug > 0 ? "    " : "") + z.Titel,
                Zellen = zellen,
                Abschnitt = abschnitt,
                Kennzeichen = z.Nachrichtlich ? ValeriAusweis.NachrichtlichLabel() : "",
                Zellwarnungen = warnungen
            };
        }

        /// <summary>ETAPPE E5 Teil b: eine Hinweiszeile der Tafel (Abschnitt HINWEIS).</summary>
        private static MatrixZeile Hinweiszeile(MatrixZeile z)
        {
            z.Abschnitt = MatrixZeile.ABSCHNITT_HINWEIS;
            return z;
        }

        /// <summary>
        /// ETAPPE E5 (V‑A, V‑G6): die Sensitivitätstafel der Seite — je Stand außer der
        /// Referenz seine Zeilen (Szenario Erwartet), der Name an der ersten, dazu die
        /// Steigung mit ihrer Einheit. Die Zeilen ordnet der Kern
        /// (<see cref="WirtschaftlichkeitBewertung.Sensitivitaetszeilen"/>) — dieselbe
        /// Auswahl wie im Bericht.
        /// </summary>
        private ErgebnisMatrix SensitivitaetTafel(List<KeyValuePair<int, string>> staende, int idReferenz,
                                                  CultureInfo kultur)
        {
            var tafel = new ErgebnisMatrix();
            List<SensitivitaetZeile> zeilen;
            try
            {
                zeilen = WirtschaftlichkeitBewertung.Sensitivitaetszeilen(
                    staende, _sens ?? new List<SensitivitaetZeile>(), idReferenz);
            }
            catch { return tafel; }
            if (zeilen.Count == 0) return tafel;

            tafel.Spalten = new List<string>
            {
                MyResource.Resource.WIRT_SZ_SP_VARIANTE,
                MyResource.Resource.WIRT_SENS_SP_PARAMETER,
                MyResource.Resource.WIRT_SENS_SP_MINUS,
                MyResource.Resource.WIRT_SENS_SP_BASIS,
                MyResource.Resource.WIRT_SENS_SP_PLUS,
                MyResource.Resource.WIRT_SENS_SP_STEIGUNG
            };
            var matrix = new List<MatrixZeile>();
            int vorher = int.MinValue;
            foreach (SensitivitaetZeile z in zeilen)
            {
                matrix.Add(new MatrixZeile
                {
                    Titel = z.IdProjekt != vorher ? Name(z.IdProjekt) : "",
                    Zellen = new List<string>
                    {
                        z.Parameter ?? "",
                        W(z.KwMinus, "N0", kultur),
                        W(z.KwBasis, "N0", kultur),
                        W(z.KwPlus, "N0", kultur),
                        z.Steigung.HasValue
                            ? z.Steigung.Value.ToString("N2", kultur) + " " + z.SteigungEinheit
                            : "—"
                    }
                });
                vorher = z.IdProjekt;
            }
            tafel.Zeilen = matrix;
            return tafel;
        }

        /// <summary>
        /// ETAPPE E5 (V‑1, ValERI-Block 1 „Gegenstand und Rahmen"): Maßnahme (die
        /// Versionen im Vergleich), Referenz, Betrachtungszeitraum und Kalkulationszins —
        /// aus dem Parametersatz der Gruppe. Die Rechnungsart („nominal") steht als Zeile
        /// darunter und kommt aus dem Textbündel der Seite.
        /// </summary>
        private ErgebnisMatrix Rahmentafel(List<int> gewaehlt, int idReferenz, CultureInfo kultur)
        {
            WirtschaftlichkeitParameter p = null;
            try { p = _ctrl.LadeParameter(_idStamm); }
            catch { }

            var versionen = new List<string>();
            foreach (int id in gewaehlt)
            {
                if (id == idReferenz) continue;
                string n = Name(id);
                if (n.Length > 0) versionen.Add(n);
            }
            string massnahme = versionen.Count == 0
                ? _stammName
                : (_stammName.Length > 0 ? _stammName + ": " : "") + string.Join(", ", versionen.ToArray());

            return new ErgebnisMatrix
            {
                Zeilen = new List<MatrixZeile>
                {
                    new MatrixZeile { Titel = MyResource.Resource.WIRT_VALERI_MASSNAHME,
                                      Zellen = new List<string> { massnahme } },
                    new MatrixZeile { Titel = MyResource.Resource.WIRT_SICHT_REF_SPALTE,
                                      Zellen = new List<string> { idReferenz > 0 ? Name(idReferenz) : "—" } },
                    new MatrixZeile { Titel = MyResource.Resource.WIRT_ANN_ZEITRAUM,
                                      Zellen = new List<string>
                                      {
                                          p != null ? p.Betrachtungszeitraum.ToString(CultureInfo.InvariantCulture) + " a" : "—"
                                      } },
                    new MatrixZeile { Titel = MyResource.Resource.WPAR_SZ_ZINS,
                                      Zellen = new List<string>
                                      {
                                          p != null ? p.Zinssatz.ToString("N2", kultur) + " %" : "—"
                                      } }
                }
            };
        }

        /// <summary>
        /// ETAPPE E5 Teil b (U2, Mockup „Was ist angenommen?"): die Annahmentafel aus dem
        /// Parametersatz — Größe · Ungünstig · Erwartet · Günstig · Herkunft
        /// (<see cref="ValeriAusweis.Annahmen"/>). Ein Lesefehler lässt sie leer; der
        /// Ausweis der Szenarioabdeckung darunter steht trotzdem.
        /// </summary>
        private ErgebnisMatrix Annahmentafel()
        {
            var tafel = new ErgebnisMatrix();
            List<AnnahmeZeile> zeilen;
            try { zeilen = ValeriAusweis.Annahmen(_ctrl.LadeParameter(_idStamm), BerichtTexte.Kultur); }
            catch { return tafel; }
            if (zeilen == null || zeilen.Count == 0) return tafel;

            tafel.Spalten = new List<string>
            {
                MyResource.Resource.WPAR_SZ_SPALTE_GROESSE,
                MyResource.Resource.WIRT_SZEN_WORST,
                MyResource.Resource.WIRT_SZEN_ERWARTET,
                MyResource.Resource.WIRT_SZEN_BEST,
                MyResource.Resource.WIRT_ANN_SP_HERKUNFT
            };
            var matrix = new List<MatrixZeile>();
            foreach (AnnahmeZeile z in zeilen)
                matrix.Add(new MatrixZeile
                {
                    Titel = z.Groesse,
                    Zellen = new List<string> { z.Unguenstig, z.Erwartet, z.Guenstig, z.Herkunft }
                });
            tafel.Zeilen = matrix;
            return tafel;
        }

        /// <summary>
        /// KD6a: die vier Kennzahl-Karten — beste Variante gegenüber Stamm. Reine ANZEIGE
        /// der bereits berechneten Werte. ETAPPE E5 Teil b: Die Karten stehen über der
        /// Szenario-Klappliste und zeigen den Erwartungsfall. BV-E3: WELCHEN Stand sie
        /// zeigen, wählt der Kern (<see cref="BesteVariante.Waehle"/>) — hier steht nur,
        /// wie die Auswahl auf den Karten aussieht.
        /// </summary>
        private List<KachelZeile> Kacheln(BesteVariante.Auswahl auswahl, CultureInfo kultur)
        {
            var kw = new KachelZeile { Titel = T("WIRT_KACHEL_KW", "Kapitalwert ggue. Stamm") };
            var an = new KachelZeile { Titel = T("WIRT_KACHEL_ANNUITAET", "Annuität") };
            var am = new KachelZeile { Titel = T("WIRT_KACHEL_AMORTISATION", "Amortisation") };
            var irr = new KachelZeile { Titel = T("WIRT_KACHEL_IRR", "Interner Zinsfuß") };

            // ETAPPE E5 (V‑A, Entscheid V‑3): Annuität, Amortisation und Zinsfuß bleiben
            // auf den Kacheln, tragen aber das Label „nachrichtlich" — die Regel, welche
            // Kennzahlen das sind, steht EINMAL in der Zeilendefinition des Kerns.
            string nachrichtlich = ValeriAusweis.NachrichtlichLabel();
            if (WirtschaftlichkeitZeilen.IstNachrichtlich("ANNUITAET")) an.Kennzeichen = nachrichtlich;
            if (WirtschaftlichkeitZeilen.IstNachrichtlich("AMORTISATION")) am.Kennzeichen = nachrichtlich;
            if (WirtschaftlichkeitZeilen.IstNachrichtlich("IRR")) irr.Kennzeichen = nachrichtlich;

            if (auswahl.Grund == BesteVariante.Auswahlgrund.BestesKriterium)
            {
                WirtschaftlichkeitErgebnis beste = auswahl.Ergebnis;
                string name = _namen.ContainsKey(beste.IdProjekt) ? _namen[beste.IdProjekt] : beste.Anzeige;
                string quelle = string.Format(T("WIRT_KACHEL_BESTE", "beste Variante: {0}"), name);

                kw.Wert = beste.KapitalwertDiff.Value.ToString("N0", kultur) + " €";
                kw.Quelle = quelle;
                an.Wert = beste.AnnuitaetKW.HasValue
                    ? beste.AnnuitaetKW.Value.ToString("N0", kultur) + " €/a" : "—";
                an.Quelle = quelle;
                // ETAPPE E5 (Q16): ohne Wert „— ‹Grund›" — derselbe Grund wie in der
                // Tafel (ValeriAusweis), nicht mehr das nackte „keine".
                am.Wert = beste.AmortisationJahre.HasValue
                    ? beste.AmortisationJahre.Value.ToString("N1", kultur) + " a"
                    : Strich(ValeriAusweis.AmortisationGrund(beste));
                am.Quelle = quelle;
                irr.Wert = beste.IRR.HasValue
                    ? beste.IRR.Value.ToString("N1", kultur) + " %"
                    : Strich(ValeriAusweis.IzfGrund(beste));
                irr.Quelle = quelle;
                // ETAPPE E5 (V‑A, Befund A2): mehr als ein Vorzeichenwechsel — die
                // Kachel warnt, der Wert bleibt stehen.
                irr.Warnung = ValeriAusweis.IzfWarnung(beste);
            }
            else
            {
                // Ohne Variante mit Differenz der Stamm; ohne sein Ergebnis bleibt der Strich.
                WirtschaftlichkeitErgebnis stamm = auswahl.Ergebnis;
                string q = T("WIRT_KACHEL_NUR_STAMM", "nur Stammprojekt gerechnet");
                kw.Wert = stamm != null && stamm.Kapitalwert.HasValue
                    ? stamm.Kapitalwert.Value.ToString("N0", kultur) + " €" : "—";
                kw.Quelle = stamm != null
                    ? T("WIRT_KACHEL_STAMM_KW", "Nettobarwert des Stammprojekts") : "";
                an.Wert = "—"; an.Quelle = q;
                am.Wert = "—"; am.Quelle = q;
                irr.Wert = "—"; irr.Quelle = q;
            }

            // BV-E6 (Konzept Berichtsvorlagen 9.5): Die Karten zeigen, was der Bericht unter
            // wirtschaft.beste.* auflöst — dieselbe Regel des Kerns wählt den Stand. Die
            // Kapitalwertkarte ist wirtschaft.beste.kapitalwert: bei einer Variante ihre
            // Differenz, im Stammfall der Nettobarwert des Stamms.
            kw.Vorlagenfeld = "wirtschaft.beste.kapitalwert";
            an.Vorlagenfeld = "wirtschaft.beste.annuitaet";
            am.Vorlagenfeld = "wirtschaft.beste.amortisation";
            irr.Vorlagenfeld = "wirtschaft.beste.irr";

            return new List<KachelZeile> { kw, an, am, irr };
        }

        /// <summary>BV-E6: der Anhang eines Szenarios an den Tabellenschlüsseln (Erwartet ohne).</summary>
        private static string GliederungsAnhang(string szenario)
        {
            if (szenario == WirtschaftlichkeitSzenario.BEST) return EPOS.UI.Dienste.Vorlagenfeldorte.Szenarioanhang("guenstig");
            if (szenario == WirtschaftlichkeitSzenario.WORST) return EPOS.UI.Dienste.Vorlagenfeldorte.Szenarioanhang("unguenstig");
            return "";
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

                // E3/3: In EPOS.UI.Daten gilt der Wächter ParallelitaetWache — ein
                // nackter Task.Run liest den VERAENDERLICHEN prozessweiten
                // Kulturvorgabewert. Derselbe Arbeitsfaden, dieselbe Abbruchmarke,
                // nur mit weitergereichter Kultur (Muster der Hüllen aus #428).
                _ergebnisse = await Kulturweitergabe.Starten(() =>
                {
                    BerichtsDaten daten = new BerichtsDatenSammler().Sammle(
                        _idStamm, _stammName, varianten, false, mitZeitreihen, melde, ct);
                    // KONZEPT § 2.9: Der GESPEICHERTE Lauf rechnet immer gegen die
                    // Referenz der GRUPPE - er ist der Lauf, aus dem der Bericht
                    // reproduzierbar sein soll. Die Sicht bleibt hier bewusst leer;
                    // die Paarsicht setzt sie in PaarErgebnisse und persistiert nicht.
                    // Die gesammelte Gruppe bleibt stehen, damit die Paarsicht gegen A
                    // rechnen kann, ohne noch einmal zu sammeln.
                    daten.IdGruppenreferenz = p.IdReferenzprojekt;
                    daten.Sicht = null;
                    _letzteDaten = daten;
                    List<WirtschaftlichkeitErgebnis> ergebnisse = _ctrl.Berechne(daten, p, p.IdReferenzprojekt);

                    // ETAPPE E6: Der Verlauf mit drei Szenarien rechnet aus DENSELBEN
                    // Eingangsdaten gleich mit - auf diesem Faden, ohne zu speichern. Die
                    // Seite zeichnet ihn danach nur noch.
                    Verlauf.DatenUebernehmen(daten);
                    return ergebnisse;
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

            // ETAPPE E5 (U44): Derselbe Knopf bricht auch einen Berichtslauf ab, den die
            // Seite über den Berichtsweg gestartet hat.
            Action bericht = BerichtAbbrechen;
            if (bericht != null) bericht();
        }

        // =====================================================================
        // Die Unterdialoge
        // =====================================================================

        private IReadOnlyDictionary<string, object> Unterdialog(
            WirtschaftlichkeitSeite.Unterdialog art)
        {
            try
            {
                // E3/6: Alle fuenf Huellen liegen jetzt selbst in EPOS.UI.Daten;
                // die Naht der Schale (Wirtschaftlichkeitswege) ist weg, und
                // jede Ueberlagerung erscheint auf JEDER Plattform. null heisst
                // hier nur noch das, was es immer hiess: Der Satz liess sich
                // nicht bauen (kein Stammprojekt, keine Anlagen).
                switch (art)
                {
                    case WirtschaftlichkeitSeite.Unterdialog.Photovoltaik:
                        return PhotovoltaikVerguetungHuelle.Gaben(_idStamm);

                    case WirtschaftlichkeitSeite.Unterdialog.Bhkw:
                        // Der Titel gehoert zum FENSTER; als Ueberlagerung traegt
                        // ihn der Wirt, deshalb verfaellt er hier.
                        string titel;
                        return BhkwWirtschaftlichkeitHuelle.Gaben(_idStamm, _ergebnisse, out titel);

                    // E3/7: Dieselbe Huelle, zwei Sichten - die Sprungziele aus
                    // dem BHKW- und dem PV-Dialog. Die Ueberlagerung ist EINE.
                    // Die Sicht "Strombezug" mit eigenem Knopf ist mit Q11 entfallen.
                    case WirtschaftlichkeitSeite.Unterdialog.TarifBhkw:
                        return TarifstrukturHuelle.Gaben(_idStamm, TarifSicht.Bhkw);

                    case WirtschaftlichkeitSeite.Unterdialog.TarifPv:
                        return TarifstrukturHuelle.Gaben(_idStamm, TarifSicht.Photovoltaik);

                    case WirtschaftlichkeitSeite.Unterdialog.Parameter:
                        return WirtschaftlichkeitParameterHuelle.Gaben(_idStamm);
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

            if (!gespeichert) return "";

            // ETAPPE E6: Gespeicherte Parameter, Tarife oder Vergütungen gelten auch für den
            // Verlauf - der gerechnete gilt nicht mehr; das nächste Zeichnen rechnet ihn aus
            // denselben Eingangsdaten mit den neuen Werten.
            if (_verlaufHuelle != null) _verlaufHuelle.Verwerfen();

            switch (art)
            {
                case WirtschaftlichkeitSeite.Unterdialog.Photovoltaik:
                    return T("PVW_MELD_GESPEICHERT", "PV-Vergütung gespeichert — bitte neu berechnen.");
                case WirtschaftlichkeitSeite.Unterdialog.Bhkw:
                    return T("BHW_MELD_GESPEICHERT",
                             "BHKW-Wirtschaftlichkeit gespeichert — bitte neu berechnen.");
                // E3/7: Dieselbe Meldung fuer beide Sichten - gespeichert
                // wurde dieselbe Tarifstruktur, egal auf welchem Weg sie aufging.
                case WirtschaftlichkeitSeite.Unterdialog.TarifBhkw:
                case WirtschaftlichkeitSeite.Unterdialog.TarifPv:
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

        /// <summary>ETAPPE E5 (Q16): „— ‹Grund›", ohne Grund der bloße Strich.</summary>
        private static string Strich(string grund)
        {
            return string.IsNullOrEmpty(grund) ? "—" : "— " + grund;
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
