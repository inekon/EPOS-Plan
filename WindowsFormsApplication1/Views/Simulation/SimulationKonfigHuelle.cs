using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using EPOS.UI.Bausteine;
using EPOS.UI.Seiten.Simulation;
using Microsoft.AspNetCore.Components;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die WINDOWS-HÜLLE der Simulationskonfiguration (iU9-W10b.1) — der Ersatz für
    /// <c>Form_Simulation_Config</c> mit ihren vier Teildateien (4 558 Z.).
    ///
    /// <para><b>Entscheid R-W10b-1.</b> Die Komponente ist eine SEITE
    /// (<c>EPOS.UI/Seiten/Simulation/SimulationKonfigSeite.razor</c>) — mit
    /// <c>SeitenZustand</c>, Eintrag in <c>Seitenschluessel</c> und einem Zweig in
    /// <c>AppWurzel</c>, damit iOS sie als Seite zeigt. Unter Windows erscheint sie
    /// <b>bis W16</b> in der modalen Dialoghülle, weil ihre beiden Aufrufer die modale
    /// Rückkehr brauchen: <c>Form_Start.btn_SimKonfig_Click</c> und
    /// <c>Form_Simulation_Detail.btn_Konfiguration_Click</c> — Letzterer springt danach
    /// auf seinen gemerkten Reiter zurück. Ein späteres Umhängen in einen Reiter oder in
    /// ein nicht-modales Fenster ändert nur diese Datei.</para>
    ///
    /// <para><b>Hier steht die ganze Datenseite.</b> Die Komponente kennt weder
    /// Controller noch <c>MyResource</c>-Schlüssel; sie bekommt fertige Kacheln,
    /// fertige Chips und ein fertig angeordnetes Schema. Was der Vorläufer in
    /// <c>Karten.cs</c> und <c>Uebersicht.cs</c> zwischen Anzeige und Datenbank
    /// vermischte, ist damit getrennt.</para>
    ///
    /// <para><b>Der Zwischenspeicher bleibt.</b> Warnbefunde, Booster-Anlagen,
    /// Quellnutzer, geladene Puffer, Systemtemperaturen, Schichtenzahlen und die
    /// Ergebnistemperatur werden EINMAL je Auffrischung geholt (nicht je Karte) — genau
    /// wie im Vorläufer, aus demselben Grund: Projekt 1023 der Arbeitskopie führt
    /// 79 Puffer-Zeilen.</para>
    /// </summary>
    internal sealed class SimulationKonfigHuelle
    {
        // =================================================================
        // iU9-W16b.4 (Entscheid E-5): KEIN MODALES FENSTER MEHR
        //
        // Hier stand "Oeffnen(besitzer, idProjekt)" - ein BlazorDialogForm mit der
        // Seite darin, 1120 x 620. Es war der Zwischenstand aus W10b (Entscheid
        // R-W10b-1: "die Huelle zeigt sie bis W16 in einem modalen Fenster, weil
        // ihre beiden Aufrufer die modale Rueckkehr brauchen").
        //
        // Beide Aufrufer sind seither Razor: die STARTSEITE zeigt die Seite als
        // freie Ansicht in derselben WebView, die ERGEBNISSEITE als Ueberlagerung.
        // Der Grund fuer die Modalitaet ist damit weg - und ein zweites Fenster
        // mit einer zweiten WebView darin waere Risiko R2 und R5 zugleich.
        //
        // Was bleibt, ist der PARAMETERSATZ (Gaben) - er hat sich nicht geaendert.
        // Den Bereich fuer den KI-Hilfe-Assistenten meldet jetzt die Huelle beim
        // Bauen des Satzes statt das Fenster beim Aktivieren.
        // =================================================================

        // =================================================================
        // Zustand
        // =================================================================

        private int m_ID_Projekt;

        /// <summary>Der Einstellungssatz des Projekts — das Persistenzmodell von Tool_1..6.</summary>
        private KonfigurationModel _konfiguration = new KonfigurationModel();

        private bool _gesperrt;
        private string _sperrgrund = "";

        private readonly EPOS.UI.Dienste.SeitenZustand _zustand =
            new EPOS.UI.Dienste.SeitenZustand();

        /// <summary>Anlagen-ID → Befundtexte (nur Befunde MIT Anlagenbezug).</summary>
        private Dictionary<int, List<string>> _warnbefunde = new Dictionary<int, List<string>>();

        /// <summary>Anlagen-ID → geteilter Quellpuffer (die Booster-Anzeigeregel, F9).</summary>
        private Dictionary<int, int> _boosterAnlagen = new Dictionary<int, int>();

        /// <summary>Quellpuffer-ID → Anlagen, die ihn als Wärmequelle nutzen.</summary>
        private Dictionary<int, List<string>> _quellnutzer = new Dictionary<int, List<string>>();

        /// <summary>Puffer, die überhaupt geladen werden — der Vorfilter der Ladeordnung.</summary>
        private HashSet<int> _geladenePuffer = new HashSet<int>();

        private int? _systemVorlauf;
        private int? _systemRuecklauf;

        private Dictionary<int, int> _schichtenJePuffer = new Dictionary<int, int>();
        private Dictionary<int, double> _tObenJePuffer = new Dictionary<int, double>();

        /// <summary>Anlagen-ID → Anlage, aus der laufenden Auffrischung (für die Editoren).</summary>
        private readonly Dictionary<int, AnlagenInfo> _anlagen = new Dictionary<int, AnlagenInfo>();

        // Außentemperatur der Klimaregion (8760 Stundenwerte) für die Vorschau des
        // Erdreichdialogs. Einmal je Sitzung geladen und gecacht (Konzept 4.5).
        private float[] _aussentempCache;
        private bool _aussentempGeladen;

        private SimulationKonfigHuelle(int idProjekt)
        {
            ProjektSetzen(idProjekt);
            _zustand.ProjektSetzen(idProjekt, "");
        }

        /// <summary>
        /// Der Ersatz für <c>SetControls</c>:292-345 — Sperrprüfung, Schemapflege und
        /// die Vorwahl der verbauten Anlagen (Ä15).
        /// </summary>
        private void ProjektSetzen(int idProjekt)
        {
            m_ID_Projekt = idProjekt;

            // Blockade bei nicht abgeschlossener Schema-Migration (ADR-001, Aufgabe 6):
            // auf halb migriertem Schema zu konfigurieren, führt zu stillen Datenfehlern.
            string grund;
            _gesperrt = SchemaMigration.SimulationGesperrt(out grund);
            _sperrgrund = _gesperrt ? grund : "";
            if (_gesperrt) return;

            // Neue Spalten (Prioritaet, Wärmequelle) bei Bedarf anlegen.
            WaermequelleClass.SchemaSicherstellen();

            _konfiguration = KonfigurationCtrl.LiesProjekt(idProjekt) ?? new KonfigurationModel();

            VerbauteAnlagenVorwaehlen();
        }

        /// <summary>
        /// Ä15 (Nutzerabnahme 26.08.2026): Im Projekt ANGELEGTE Anlagen erscheinen von
        /// selbst als gewählte Komponenten — auch ohne je gespeicherte Konfiguration.
        /// Eine gespeicherte Auswahl bleibt unangetastet, ergänzt wird nur Fehlendes.
        ///
        /// <para>Genommen wird der ERSTE freie Platz in der Reihenfolge 1…4 — wörtlich
        /// wie <c>VerbauteAnlagenVorwaehlen</c>:349-380 und damit ausdrücklich anders als
        /// <c>Kaskade.Aufnehmen</c> (dort der erste freie Platz HINTER dem letzten
        /// belegten, weil das die Bedienhandlung „+ aufnehmen" ist).</para>
        ///
        /// <para><c>catch { }</c> wie im Vorläufer: Die Vorwahl ist Komfort — sie darf das
        /// Öffnen nie verhindern.</para>
        /// </summary>
        private void VerbauteAnlagenVorwaehlen()
        {
            try
            {
                List<string> plaetze = Kaskade.Lesen(_konfiguration);

                foreach (string erzeuger in ErzeugerKatalog.WAERMEERZEUGER)
                {
                    if (!TechnikPlanwertCtrl.Verbaut(m_ID_Projekt, erzeuger)) continue;
                    if (plaetze.Contains(erzeuger)) continue;

                    for (int i = 0; i < plaetze.Count; i++)
                        if (string.IsNullOrEmpty(plaetze[i])) { plaetze[i] = erzeuger; break; }
                }
                Kaskade.Schreiben(_konfiguration, plaetze);

                if (TechnikPlanwertCtrl.Verbaut(m_ID_Projekt, DbWerte.ERZEUGER_PHOTOVOLTAIK) &&
                    Kaskade.StromWert(_konfiguration, Kaskade.PLATZ_STROMERZEUGER) !=
                        DbWerte.ERZEUGER_PHOTOVOLTAIK)
                    Kaskade.StromAuswahl(_konfiguration, Kaskade.PLATZ_STROMERZEUGER,
                                         DbWerte.ERZEUGER_PHOTOVOLTAIK);

                if (TechnikPlanwertCtrl.Verbaut(m_ID_Projekt, DbWerte.ERZEUGER_STROMSPEICHER) &&
                    Kaskade.StromWert(_konfiguration, Kaskade.PLATZ_ENERGIESPEICHER) !=
                        DbWerte.ERZEUGER_STROMSPEICHER)
                    Kaskade.StromAuswahl(_konfiguration, Kaskade.PLATZ_ENERGIESPEICHER,
                                         DbWerte.ERZEUGER_STROMSPEICHER);
            }
            catch { /* Vorwahl ist Komfort — sie darf das Öffnen nie verhindern */ }
        }

        // =================================================================
        // Der Parametersatz der Seite
        // =================================================================

        /// <summary>
        /// Der PARAMETERSATZ der Seite zu einem Projekt — ohne Fenster (iU9-W11b.13).
        ///
        /// <para>Die Ergebnisseite zeigt die Konfiguration als ÜBERLAGERUNG (Risiko
        /// R-W11-6): Blazor über Blazor gehört in dasselbe Fenster, nicht in eine
        /// zweite WebView (Regel seit W4.0). Sie braucht dafür genau diesen Satz und
        /// setzt <c>Geschlossen</c> selbst.</para>
        /// </summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int idProjekt)
        {
            // Bereich fuer den KI-Hilfe-Assistenten melden (nur Bedien-Kontext,
            // keine Projekt- oder Kundendaten) - woertlich wie im Vorlaeufer :114,
            // nur nicht mehr am Activated-Ereignis eines Fensters.
            HilfeKontext.SetzeBereich(
                "Simulation Konfiguration (Erzeuger definieren, Pufferspeicher zuordnen)");

            return new SimulationKonfigHuelle(idProjekt).Gaben();
        }

        /// <summary>Der PARAMETERSATZ der Seite — ohne <c>Geschlossen</c>.</summary>
        internal IReadOnlyDictionary<string, object> Gaben()
        {
            return new Dictionary<string, object>
            {
                ["Dienste"] = DiensteSatz(),
                ["Zustand"] = _zustand,

                ["KopfText"] = MyResource.Resource.SIM_KONFIG_KOPF,
                ["HilfeSchluessel"] = "Form_Simulation_Config.btn_Help",

                ["AnsichtLabel"] = MyResource.Resource.SIM_ANSICHT_LABEL,
                ["AnsichtListe"] = MyResource.Resource.SIM_ANSICHT_LISTE,
                ["AnsichtSchema"] = MyResource.Resource.SIM_ANSICHT_SCHEMA,

                ["KopfErzeuger"] = MyResource.Resource.SIM_KARTEN_KOPF_ERZEUGER,
                ["KopfSpeicher"] = MyResource.Resource.PSP_KARTEN_KOPF_SPEICHER,

                ["AnlageHinzuText"] = MyResource.Resource.SIM_KARTE_ANLAGE_HINZU,
                ["VerfuegbarEinblenden"] = MyResource.Resource.SIM_KARTE_VERFUEGBAR_EINBLENDEN,
                ["VerfuegbarAusblenden"] = MyResource.Resource.SIM_KARTE_VERFUEGBAR_AUSBLENDEN,

                ["AufnehmenText"] = MyResource.Resource.SIM_KARTE_AUFNEHMEN,
                ["TipHoch"] = MyResource.Resource.SIM_KARTE_TIP_HOCH,
                ["TipRunter"] = MyResource.Resource.SIM_KARTE_TIP_RUNTER,
                ["TipBearbeiten"] = MyResource.Resource.SIM_KARTE_TIP_BEARBEITEN,
                ["TipEntfernen"] = MyResource.Resource.SIM_KARTE_TIP_ENTFERNEN,
                ["TipAufnehmen"] = MyResource.Resource.SIM_KARTE_TIP_AUFNEHMEN,
                ["TipAufklappen"] = MyResource.Resource.SIM_KARTE_TIP_AUFKLAPPEN,

                ["BilanzText"] = MyResource.Resource.PSP_KARTE_BILANZ,
                ["TipSpeicherBearbeiten"] = MyResource.Resource.PSP_KARTE_TIP_BEARBEITEN,
                ["TipSpeicherAufklappen"] = MyResource.Resource.SIM_KARTE_TIP_AUFKLAPPEN,
                ["BtnPufferVerwalten"] = MyResource.Resource.PSP_BTN_PUFFER_VERWALTEN,

                ["ExtrapolationText"] = MyResource.Resource.SIM_EXTRAPOLATION_SCHALTER,
                ["LesepunktText"] = MyResource.Resource.SIM_BOOSTER_LESEPUNKT_SCHALTER,

                ["BtnSpeichern"] = MyResource.Resource.SIM_KONFIG_BTN_SPEICHERN,
                ["BtnBeenden"] = MyResource.Resource.SIM_KONFIG_BTN_BEENDEN,

                ["StatusGespeichert"] = MyResource.Resource.SIM_STATUS_KONFIG_GESPEICHERT,
                ["StatusExtrapolationEin"] = MyResource.Resource.SIM_STATUS_EXTRAPOLATION_EIN,
                ["StatusExtrapolationAus"] = MyResource.Resource.SIM_STATUS_EXTRAPOLATION_AUS,
                ["StatusLesepunktDavor"] = MyResource.Resource.SIM_STATUS_LESEPUNKT_DAVOR,
                ["StatusLesepunktDanach"] = MyResource.Resource.SIM_STATUS_LESEPUNKT_DANACH,
                ["StatusEinstellungFehler"] = MyResource.Resource.SIM_STATUS_EINSTELLUNG_FEHLER,

                ["SchemaLeerText"] = MyResource.Resource.SIM_SCHEMA_LEER,
                ["SchemaWarnungText"] = MyResource.Resource.SIM_SCHEMA_WARNUNG,
                ["SchemaKetteKopfText"] = MyResource.Resource.SIM_SCHEMA_KETTE_KOPF,
                ["SchemaKeineKetteText"] = MyResource.Resource.SIM_SCHEMA_KEINE_KETTE,

                ["ModusTitel"] = MyResource.Resource.SIM_TITEL_BETRIEBSMODUS,
                ["MsgModusNurWp"] = Zeilenumbruch.Normalisieren(
                                        MyResource.Resource.SIM_MSG_MODUS_NUR_WP),
                ["MsgPvAuswahl"] = Zeilenumbruch.Normalisieren(
                                        MyResource.Resource.SIM_MSG_PV_AUSWAHL),
                ["SteuerwertPv"] = WaermequelleClass.MODUS_PV,

                ["PrioTitel"] = MyResource.Resource.SIM_WPPRIO_DIALOG_TITEL,
                ["PrioFrage"] = MyResource.Resource.SIM_WPPRIO_DIALOG_TEXT,
                ["MsgWpPrioNurWp"] = Zeilenumbruch.Normalisieren(
                                        MyResource.Resource.SIM_MSG_WPPRIO_NUR_WP),

                ["QuellenwahlTitel"] = MyResource.Resource.SIMQ_TITEL_WAERMEQUELLE,
                ["MsgQuelleArt"] = Zeilenumbruch.Normalisieren(
                                        MyResource.Resource.SIMQ_MSG_QUELLE_ART),
                ["MsgLuftWasser"] = Zeilenumbruch.Normalisieren(
                                        MyResource.Resource.SIMQ_MSG_LUFT_WASSER),

                ["KonstantTitel"] = MyResource.Resource.SIMQ_KONSTANT_DIALOG_TITEL,
                ["KonstantFrage"] = MyResource.Resource.SIMQ_KONSTANT_DIALOG_TEXT,

                ["CsvTitel"] = MyResource.Resource.SIMQ_CSV_TITEL,
                ["CsvFrage"] = Zeilenumbruch.Normalisieren(
                                        MyResource.Resource.SIMQ_CSV_FRAGE_DATEI),
                ["CsvFormatHinweis"] = WaermequelleClass.CSV_FORMAT_HINWEIS,
                ["CsvJaText"] = MyResource.Resource.SIM_BTN_OK,
                ["CsvNeinText"] = MyResource.Resource.SIM_BTN_ABBRECHEN,

                // Die Steuerwerte der sechs Quellenzweige - nie Anzeigetexte.
                ["SteuerwertQuelleKonstant"] = WaermequelleClass.TYP_KONSTANT,
                ["SteuerwertQuellePuffer"] = WaermequelleClass.TYP_PUFFER,
                ["SteuerwertQuellprofil"] = WaermequelleClass.TYP_PROFIL,
                ["SteuerwertQuelleCsv"] = WaermequelleClass.TYP_CSV,
                ["SteuerwertQuelleErdreich"] = WaermequelleClass.TYP_ERDREICH,

                ["QuellePufferTitel"] = MyResource.Resource.SIMQ_PUFFER_TITEL,
                ["QuellprofilTitel"] = MyResource.Resource.SIMQ_QUELLPROFIL_TITEL,
                ["ErdreichTitel"] = MyResource.Resource.SIMQ_ERDREICH_TITEL,
                ["SenkeTitel"] = MyResource.Resource.SIM_SENKE_TITEL,
                ["VerwaltungTitel"] = MyResource.Resource.PSP_PROJEKT_FENSTERTITEL
            };
        }

        // =================================================================
        // Die Datenseite
        // =================================================================

        private SimulationKonfigDienste DiensteSatz()
        {
            return new SimulationKonfigDienste
            {
                Laden = Laden,
                SchemaLaden = SchemaLaden,

                Verschieben = (dbWert, richtung) =>
                    Kaskade.Verschieben(_konfiguration, dbWert, richtung),
                Aufnehmen = dbWert => Kaskade.Aufnehmen(_konfiguration, dbWert),
                Entfernen = dbWert => Kaskade.Entfernen(_konfiguration, dbWert),
                StromAuswahl = (platz, dbWert) =>
                    Kaskade.StromAuswahl(_konfiguration, platz, dbWert),

                Speichern = Speichern,
                ExtrapolationSchreiben = wert =>
                    m_ID_Projekt > 0 &&
                    KonfigurationCtrl.ExtrapolationErlaubtSchreiben(m_ID_Projekt, wert),
                LesepunktSchreiben = LesepunktSchreiben,

                BetriebsmodusGaben = idAnlage =>
                {
                    AnlagenInfo a = Anlage(idAnlage);
                    return a == null ? null
                        : BetriebsmodusHuelle.Gaben(a.Bezeichner, a.BM_Typ);
                },
                BetriebsmodusSchreiben = (idAnlage, modus) =>
                    WaermequelleClass.WertSchreiben(idAnlage, "BM_Typ", modus),
                PrioritaetSchreiben = (idAnlage, prio) =>
                    WaermequelleClass.WertSchreiben(idAnlage, "Prioritaet", prio),

                Quellentypen = Quellentypen,
                QuelleTyp = idAnlage =>
                {
                    AnlagenInfo a = Anlage(idAnlage);
                    if (a == null) return "";

                    // Vorauswahl: der gespeicherte Typ. Beim Heizkessel ist die leere
                    // Angabe ein REGULÄRER Eintrag („Systemrücklauf"), bei der
                    // Wärmepumpe steht sie wie bisher für Außenluft (:841-849).
                    string typ = a.WQ_Typ ?? "";
                    return a.IstWaermepumpe && typ.Length == 0
                        ? WaermequelleClass.TYP_AUSSENLUFT : typ;
                },
                QuelleTemperatur = idAnlage =>
                {
                    AnlagenInfo a = Anlage(idAnlage);
                    return a != null && a.WQ_Temp != 0 ? a.WQ_Temp : 10.0;
                },
                QuelleEinfachSchreiben = QuelleEinfachSchreiben,

                QuellePufferGaben = QuellePufferGaben,
                QuellePufferSchreiben = QuellePufferSchreiben,
                QuellprofilGaben = QuellprofilGaben,
                QuellprofilSchreiben = (idAnlage, idProfil) =>
                    WaermequelleClass.QuelleSchreiben(idAnlage, new QuelleErgebnis
                    {
                        Typ = WaermequelleClass.TYP_PROFIL,
                        IdQuellprofil = idProfil
                    }),
                QuelleErdreichGaben = QuelleErdreichGaben,
                QuelleErdreichSchreiben = QuelleErdreichSchreiben,
                QuelleCsvWaehlen = QuelleCsvWaehlen,

                WaermesenkeGaben = WaermesenkeGaben,
                WaermesenkeFertig = WaermesenkeFertig,

                PufferVerwaltungGaben = idPuffer =>
                    PufferSpProjektHuelle.Gaben(m_ID_Projekt, null, idPuffer)
            };
        }

        private AnlagenInfo Anlage(int idAnlage)
        {
            AnlagenInfo a;
            return _anlagen.TryGetValue(idAnlage, out a) ? a : null;
        }

        // =================================================================
        // Laden — der eine Auffrischungsweg
        // =================================================================

        private SimulationKonfigDaten Laden(int idProjekt)
        {
            if (idProjekt != m_ID_Projekt) ProjektSetzen(idProjekt);

            SimulationKonfigDaten d = new SimulationKonfigDaten
            {
                IdProjekt = m_ID_Projekt,
                Gesperrt = _gesperrt,
                Sperrgrund = _sperrgrund
            };
            if (_gesperrt) return d;

            // EINMAL je Auffrischung, nicht je Karte (Karten.cs:665-671 und
            // :1820-1830) - der Katalog liest Anlagen, Senkenlisten und
            // Speicherzeilen.
            _warnbefunde = WarnbefundeSammeln();
            _boosterAnlagen = m_ID_Projekt > 0
                ? Warnkriterien.BoosterAnlagen(m_ID_Projekt) : new Dictionary<int, int>();
            _quellnutzer = QuellnutzerSammeln();
            _geladenePuffer = GeladenePufferSammeln();
            _systemVorlauf = PufferSpCtrl.SystemVorlauf(m_ID_Projekt);
            _systemRuecklauf = PufferSpCtrl.SystemRuecklauf(m_ID_Projekt);
            _schichtenJePuffer = PufferSpCtrl.SchichtenJeProjekt(m_ID_Projekt);
            _tObenJePuffer = TObenSammeln();
            _anlagen.Clear();

            d.Gruppen = new List<KachelGruppe>
            {
                WaermeerzeugerGruppe(),
                StromGruppe(MyResource.Resource.SIM_KARTE_GRUPPE_STROM,
                            Kaskade.PLATZ_STROMERZEUGER, ErzeugerKatalog.STROMERZEUGER,
                            WizardItemClass.PV_TYP),
                StromGruppe(MyResource.Resource.SIM_KARTE_GRUPPE_SPEICHER,
                            Kaskade.PLATZ_ENERGIESPEICHER, ErzeugerKatalog.ENERGIESPEICHER,
                            WizardItemClass.SP_TYP)
            };

            d.Speicher = SpeicherSpalte();
            d.SpeicherLeerText = m_ID_Projekt > 0
                ? MyResource.Resource.PSP_KARTE_KEIN_SPEICHER
                : MyResource.Resource.PSP_FUSSZEILE_OHNE_PROJEKT;

            d.ExtrapolationMoeglich = m_ID_Projekt > 0;
            // Ohne Projekt bleibt die Vorbelegung stehen - nicht „aus", denn das wäre
            // die Aussage „Extrapolation verboten", und die trifft nicht zu (:189-192).
            d.ExtrapolationErlaubt = m_ID_Projekt <= 0 ||
                                     KonfigurationCtrl.ExtrapolationErlaubtLesen(m_ID_Projekt);

            d.BoosterSichtbar = m_ID_Projekt > 0 && _boosterAnlagen.Count > 0;
            d.BoosterDavor = !d.BoosterSichtbar ||
                !string.Equals(KonfigurationCtrl.BoosterLesepunktLesen(m_ID_Projekt),
                               DbWerte.BOOSTER_LESEPUNKT_DANACH, StringComparison.Ordinal);

            d.PvGewaehlt = Kaskade.StromWert(_konfiguration, Kaskade.PLATZ_STROMERZEUGER).Length > 0;

            return d;
        }

        // =================================================================
        // Die Erzeugerspalte
        // =================================================================

        /// <summary>
        /// Gruppe „Wärmeerzeuger": erst die aufgenommenen in Kaskadenreihenfolge, dann
        /// die verfügbaren (wörtlich <c>WaermeerzeugerGruppe</c>:715-790).
        /// </summary>
        private KachelGruppe WaermeerzeugerGruppe()
        {
            List<ErzeugerZeile> zeilen = new List<ErzeugerZeile>();
            List<string> kaskade = Kaskade.Belegt(_konfiguration);

            for (int i = 0; i < kaskade.Count; i++)
            {
                string dbWert = kaskade[i];
                string erzeuger = ErzeugerKatalog.Anzeige(dbWert);
                int idType = Kaskade.TypZuAnlagentyp(dbWert);
                List<AnlagenInfo> anlagen = m_ID_Projekt > 0 && idType > 0
                    ? WErzeugerCtrl.AnlagenMitWp(m_ID_Projekt, idType)
                    : new List<AnlagenInfo>();
                string rang = (i + 1).ToString(CultureInfo.CurrentCulture);

                if (anlagen.Count == 0)
                {
                    // Aufgenommen, aber im Projekt gibt es keine Anlage dazu.
                    zeilen.Add(new ErzeugerZeile
                    {
                        DbWert = dbWert,
                        IdType = idType,
                        Kachel = new ErzeugerKachelDaten
                        {
                            Schluessel = dbWert,
                            Rang = rang,
                            Titel = erzeuger,
                            Chips = new List<ChipDaten>
                            {
                                new ChipDaten(MyResource.Resource.SIM_KARTE_OHNE_ANLAGE,
                                              ChipStil.Flaeche)
                            },
                            Reihenfolge = true,
                            AufMoeglich = i > 0,
                            AbMoeglich = i < kaskade.Count - 1,
                            Umschaltbar = true
                        }
                    });
                    continue;
                }

                for (int a = 0; a < anlagen.Count; a++)
                {
                    AnlagenInfo info = anlagen[a];
                    _anlagen[info.ID] = info;

                    zeilen.Add(new ErzeugerZeile
                    {
                        DbWert = dbWert,
                        IdAnlage = info.ID,
                        IdType = info.ID_Type,
                        Bezeichner = info.Bezeichner,
                        IstWaermepumpe = info.IstWaermepumpe,
                        QuellenwahlMoeglich = WaermequelleClass.QuellenwahlMoeglich(info.ID_Type),
                        BauartGebunden = string.IsNullOrEmpty(info.WpTyp) ||
                                         info.WpTyp == DbWerte.WP_BAUART_LUFT_WASSER,
                        WpTypAnzeige = string.IsNullOrEmpty(info.WpTyp)
                            ? MyResource.Resource.SIMQ_WPTYP_NICHT_GEPFLEGT : info.WpTyp,
                        Prioritaet = info.Prioritaet,
                        Kachel = new ErzeugerKachelDaten
                        {
                            Schluessel = dbWert,
                            Rang = rang,
                            Titel = string.Format(MyResource.Resource.SIM_KARTE_TITEL,
                                                  erzeuger, info.Bezeichner),
                            // F2: n = Platz in der Anzeigereihenfolge dieser Gruppe,
                            // m = Anlagen des Typs.
                            Chips = ErzeugerChips(info, a + 1, anlagen.Count),
                            Reihenfolge = a == 0,
                            AufMoeglich = i > 0,
                            AbMoeglich = i < kaskade.Count - 1,
                            Umschaltbar = a == 0,
                            Editierbar = true
                        }
                    });
                }
            }

            foreach (string dbWert in ErzeugerKatalog.WAERMEERZEUGER)
            {
                if (kaskade.Contains(dbWert)) continue;
                zeilen.Add(VerfuegbarZeile(dbWert, Kaskade.TypZuAnlagentyp(dbWert), false, 0));
            }

            return new KachelGruppe
            {
                Titel = MyResource.Resource.SIM_KARTE_GRUPPE_WAERME,
                Zeilen = zeilen,
                // ABNAHMEBEFUND 1: Seit die Platzhalter ausgeblendet sind, kann diese
                // Gruppe LEER sein. Eine Überschrift ohne alles darunter sagt nichts.
                LeerText = kaskade.Count == 0
                    ? MyResource.Resource.SIM_KARTE_KEINE_ERZEUGER : ""
            };
        }

        /// <summary>
        /// Gruppe „Stromerzeuger" bzw. „Energiespeicher" — je ein Auswahlplatz
        /// (<c>Tool_5</c>/<c>Tool_6</c>) mit genau einem Katalogeintrag. Keine
        /// Kaskadenposition, kein Senkendialog (wörtlich :801-859).
        /// </summary>
        private KachelGruppe StromGruppe(string titel, int platz, string[] katalog, int idType)
        {
            List<ErzeugerZeile> zeilen = new List<ErzeugerZeile>();
            string gewaehlt = Kaskade.StromWert(_konfiguration, platz);

            foreach (string dbWert in katalog)
            {
                if (string.Equals(dbWert, gewaehlt, StringComparison.Ordinal))
                {
                    List<string> namen = WErzeugerCtrl.AnlagenNamen(m_ID_Projekt, idType);

                    List<ChipDaten> chips = new List<ChipDaten>();
                    if (namen.Count == 0)
                        chips.Add(new ChipDaten(MyResource.Resource.SIM_KARTE_OHNE_ANLAGE,
                                                ChipStil.Flaeche));

                    zeilen.Add(new ErzeugerZeile
                    {
                        DbWert = dbWert,
                        IdType = idType,
                        IstStrom = true,
                        StromPlatz = platz,
                        Kachel = new ErzeugerKachelDaten
                        {
                            Schluessel = dbWert,
                            Titel = namen.Count > 0
                                ? string.Format(MyResource.Resource.SIM_KARTE_TITEL,
                                                ErzeugerKatalog.Anzeige(dbWert),
                                                string.Join(" · ", namen.ToArray()))
                                : ErzeugerKatalog.Anzeige(dbWert),
                            Chips = chips,
                            Detailchips = StromDetailchips(idType),
                            Umschaltbar = true
                        }
                    });
                    continue;
                }

                zeilen.Add(VerfuegbarZeile(dbWert, idType, true, platz));
            }

            return new KachelGruppe { Titel = titel, Zeilen = zeilen };
        }

        /// <summary>
        /// Eine gestrichelte Zeile „im Katalog wählbar, nicht aufgenommen"
        /// (wörtlich <c>VerfuegbarKarte</c>:1107-1146). Ob sie gezeigt wird, entscheidet
        /// die SEITE — sie führt den Schalter am Spaltenende.
        /// </summary>
        private ErzeugerZeile VerfuegbarZeile(string dbWert, int idType, bool strom, int platz)
        {
            List<string> namen = idType > 0
                ? WErzeugerCtrl.AnlagenNamen(m_ID_Projekt, idType) : new List<string>();

            List<ChipDaten> chips = new List<ChipDaten>
            {
                new ChipDaten(MyResource.Resource.SIM_KARTE_VERFUEGBAR, ChipStil.Flaeche)
            };
            if (namen.Count == 0)
                chips.Add(new ChipDaten(MyResource.Resource.SIM_KARTE_OHNE_ANLAGE,
                                        ChipStil.Flaeche));

            return new ErzeugerZeile
            {
                DbWert = dbWert,
                IdType = idType,
                IstStrom = strom,
                StromPlatz = platz,
                Verfuegbar = true,
                Kachel = new ErzeugerKachelDaten
                {
                    Schluessel = dbWert,
                    Titel = namen.Count > 0
                        ? string.Format(MyResource.Resource.SIM_KARTE_TITEL,
                                        ErzeugerKatalog.Anzeige(dbWert),
                                        string.Join(" · ", namen.ToArray()))
                        : ErzeugerKatalog.Anzeige(dbWert),
                    Chips = chips,
                    Zustand = Kachelzustand.Verfuegbar,
                    Umschaltbar = true
                }
            };
        }

        // =================================================================
        // Die Chips einer Erzeugerkachel (Konzept 3, Mockup 4)
        // =================================================================

        /// <summary>
        /// Die Chipfolge einer Kachel in FESTER Reihenfolge (wörtlich
        /// <c>ErzeugerChips</c>:1340-1371).
        /// </summary>
        private List<ChipDaten> ErzeugerChips(AnlagenInfo info, int modulNr, int modulAnzahl)
        {
            List<ChipDaten> chips = new List<ChipDaten>();

            ModulChip(chips, modulNr, modulAnzahl);
            QuellenChip(info, chips);
            BoosterChip(info, chips);
            SenkenChips(info, chips);
            TemperaturChip(info, chips);
            WarnChip(info, chips);

            if (info.IstWaermepumpe)
            {
                chips.Add(new ChipDaten(
                    string.Format(MyResource.Resource.SIM_KARTE_WPPRIO,
                                  info.Prioritaet > 0
                                      ? info.Prioritaet.ToString(CultureInfo.CurrentCulture) : "–"),
                    ChipStil.Neutral, MyResource.Resource.SIM_TIP_WPPRIO, ChipZiel.Prioritaet));

                chips.Add(new ChipDaten(BetriebsmodusAnzeige(info), ChipStil.Neutral,
                                        MyResource.Resource.SIM_TIP_BETRIEBSMODUS, ChipZiel.Modus));
            }

            return chips;
        }

        /// <summary>
        /// Der MODUL-AUSWEIS (Anwenderentscheid F2): „Modul n von m", nur bei m &gt; 1.
        ///
        /// <para><b>Befund W10-B36 erledigt.</b> Der Vorläufer holte den Text über ein
        /// <c>T(schluessel, rueckfall)</c>-Muster mit deutschem Rückfall; beide Schlüssel
        /// stehen längst in beiden <c>.resx</c> UND in <c>Resource.Designer.cs</c>, der
        /// Rückfall war tot. Hier stehen sie direkt.</para>
        /// </summary>
        private static void ModulChip(List<ChipDaten> chips, int nr, int anzahl)
        {
            if (anzahl < 2 || nr < 1) return;

            chips.Add(new ChipDaten(
                string.Format(MyResource.Resource.SIM_KARTE_MODUL, nr, anzahl),
                ChipStil.Flaeche,
                string.Format(MyResource.Resource.SIM_KARTE_TIP_MODUL, anzahl)));
        }

        /// <summary>Der Quellen-Chip (wörtlich <c>QuellenChip</c>:1561-1637).</summary>
        private void QuellenChip(AnlagenInfo info, List<ChipDaten> chips)
        {
            bool waehlbar = WaermequelleClass.QuellenwahlMoeglich(info.ID_Type);

            // E0: WQ_ID_Puffer ist die führende Identität des Quellpuffers.
            int idQuellPuffer = WaermesenkeClass.QuellPufferDerAnlage(m_ID_Projekt, info.ID);

            if (idQuellPuffer > 0)
            {
                string name = WaermesenkeClass.PufferName(idQuellPuffer);
                if (name.Length == 0) name = MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER;

                chips.Add(new ChipDaten(
                    string.Format(MyResource.Resource.SIM_KARTE_QUELLE_KASKADE, name),
                    ChipStil.QuelleKaskade, MyResource.Resource.SIM_KARTE_TIP_KASKADE,
                    waehlbar ? ChipZiel.Quelle : ChipZiel.Keines));
                return;
            }

            // Solarthermie und BHKW haben keine wählbare Wärmequelle - für sie entsteht
            // gar kein Chip, statt einen anzubieten und den Klick abzuweisen.
            if (!waehlbar) return;

            if (info.IstWaermepumpe)
            {
                // PAKET Q1: BAUART-BINDUNG SICHTBAR. Eine Luft-Wasser-Wärmepumpe hat
                // keine Wahl - ihre Quelle IST die Außenluft. Flächenstil statt
                // Quellrahmen, kein Ziel, und der Hinweis nennt den Grund.
                bool bauartGebunden = string.IsNullOrEmpty(info.WpTyp) ||
                                      info.WpTyp == DbWerte.WP_BAUART_LUFT_WASSER;

                chips.Add(new ChipDaten(
                    string.Format(MyResource.Resource.SIM_KARTE_QUELLE,
                                  WaermequelleClass.QuelleAnzeige(m_ID_Projekt, info.ID,
                                                                  info.WpTyp, info.WQ_Typ,
                                                                  info.WQ_Temp)),
                    bauartGebunden ? ChipStil.Flaeche : ChipStil.Quelle,
                    bauartGebunden
                        ? string.Format(MyResource.Resource.SIMQ_TIP_QUELLE_BAUART,
                                        string.IsNullOrEmpty(info.WpTyp)
                                            ? MyResource.Resource.SIMQ_WPTYP_NICHT_GEPFLEGT
                                            : info.WpTyp)
                        : MyResource.Resource.SIMQ_TIP_QUELLE,
                    bauartGebunden ? ChipZiel.Keines : ChipZiel.Quelle));
                return;
            }

            // Heizkessel ohne Quellpuffer: Er rechnet mit dem Systemrücklauf als
            // Eintrittstemperatur - der Normalfall und keine Fehlstelle.
            chips.Add(new ChipDaten(
                string.Format(MyResource.Resource.SIM_KARTE_QUELLE,
                              MyResource.Resource.SIMQ_QUELLE_SYSTEMRUECKLAUF),
                ChipStil.Quelle, MyResource.Resource.SIMQ_TIP_QUELLE_KESSEL, ChipZiel.Quelle));
        }

        /// <summary>Das BOOSTER-BADGE (Konzept 8.2, Entscheidung F9; :1478-1493).</summary>
        private void BoosterChip(AnlagenInfo info, List<ChipDaten> chips)
        {
            int idPuffer;
            if (!_boosterAnlagen.TryGetValue(info.ID, out idPuffer) || idPuffer <= 0) return;

            string name = WaermesenkeClass.PufferName(idPuffer);
            if (name.Length == 0) name = MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER;

            chips.Add(new ChipDaten(MyResource.Resource.SIM_KARTE_BOOSTER,
                                    ChipStil.QuelleKaskade,
                                    string.Format(MyResource.Resource.SIM_KARTE_TIP_BOOSTER, name),
                                    ChipZiel.Quelle));
        }

        /// <summary>Die SENKENKETTE (Konzept 5.3; wörtlich :1652-1754).</summary>
        private void SenkenChips(AnlagenInfo info, List<ChipDaten> chips)
        {
            List<Z_AnlageSenkeModel> kette = info.Senken;

            Z_AnlageSenkeModel rang1 = info.SenkeAufRang(0);
            bool pufferSenke = rang1 != null &&
                               WaermesenkeClass.IstPufferZiel(rang1.Ziel) && rang1.ID_Puffer > 0;

            string text = WaermesenkeClass.SenkeAnzeige(rang1);
            string hinweis = MyResource.Resource.SIM_TIP_SENKE;

            if (pufferSenke)
            {
                // PARALLELVERBUND: Lädt der Erzeuger einen gemeinsamen Vorrat aus
                // mehreren Speichern, muss die Kachel das zeigen - sonst stünde dort der
                // Name EINES Behälters, während der Lauf mit der Summe rechnet.
                int zusatz = WaermesenkeClass.VerbundLesen(info.ID).Count;
                if (zusatz > 0)
                {
                    text += " " + string.Format(MyResource.Resource.SIM_KARTE_VERBUND_ZUSATZ, zusatz);
                    hinweis = string.Format(MyResource.Resource.SIM_TIP_VERBUND, zusatz + 1) +
                              Environment.NewLine + hinweis;
                }

                List<Ladeordnung.LadeEintrag> ordnung =
                    Ladeordnung.Ladereihenfolge(m_ID_Projekt, rang1.ID_Puffer);
                int position = Ladeordnung.Position(ordnung, info.ID, false);
                if (position > 0)
                {
                    text += " " + KartenStil.Kreisziffer(position);
                    hinweis = string.Format(MyResource.Resource.SIM_POSITION_LAEDT_ALS,
                                            position, ordnung.Count) +
                              Environment.NewLine + hinweis;
                }
            }

            chips.Add(new ChipDaten(string.Format(MyResource.Resource.SIM_KARTE_SENKE, text),
                                    pufferSenke ? ChipStil.Senke : ChipStil.Neutral,
                                    hinweis, ChipZiel.Senke));

            Z_AnlageSenkeModel rang2 = info.SenkeAufRang(1);
            string zweit = WaermesenkeClass.SenkeAnzeige(rang2);
            bool zweitPuffer = rang2 != null &&
                               WaermesenkeClass.IstPufferZiel(rang2.Ziel) && rang2.ID_Puffer > 0;

            if (zweitPuffer)
            {
                List<Ladeordnung.LadeEintrag> ordnung2 =
                    Ladeordnung.Ladereihenfolge(m_ID_Projekt, rang2.ID_Puffer);
                int position2 = Ladeordnung.Position(ordnung2, info.ID, true);
                if (position2 > 0) zweit += " " + KartenStil.Kreisziffer(position2);
            }

            chips.Add(new ChipDaten(string.Format(MyResource.Resource.SIM_KARTE_ZWEITSENKE, zweit),
                                    zweitPuffer ? ChipStil.Senke : ChipStil.Neutral,
                                    MyResource.Resource.SIM_TIP_ZWEITSENKE, ChipZiel.Zweitsenke));

            // --- Ränge ab 3 (Paket S1) ------------------------------------------
            for (int i = 2; i < kette.Count; i++)
            {
                Z_AnlageSenkeModel z = kette[i];
                if (z == null) continue;

                string weiter = WaermesenkeClass.SenkeAnzeige(z);
                bool weiterPuffer = WaermesenkeClass.IstPufferZiel(z.Ziel) && z.ID_Puffer > 0;

                if (weiterPuffer)
                {
                    List<Ladeordnung.LadeEintrag> ordnungN =
                        Ladeordnung.Ladereihenfolge(m_ID_Projekt, z.ID_Puffer);
                    int positionN = Ladeordnung.Position(ordnungN, info.ID, true);
                    if (positionN > 0) weiter += " " + KartenStil.Kreisziffer(positionN);
                }

                chips.Add(new ChipDaten(
                    string.Format(MyResource.Resource.SIM_KARTE_SENKE_WEITER, weiter),
                    weiterPuffer ? ChipStil.Senke : ChipStil.Neutral,
                    MyResource.Resource.SIM_TIP_ZWEITSENKE, ChipZiel.Zweitsenke));
            }
        }

        /// <summary>
        /// Temperaturchip mit der WARNREGEL aus Konzept Abschnitt 5 (wörtlich
        /// :1773-1803): Gezeigt wird das Paar des ZUGEORDNETEN Puffers, sobald der
        /// Erzeuger einen lädt; ohne gepflegtes Paar entsteht KEIN Chip.
        /// </summary>
        private static void TemperaturChip(AnlagenInfo info, List<ChipDaten> chips)
        {
            WaermesenkeClass.PufferInfo puffer = null;
            Z_AnlageSenkeModel rang1 = info.SenkeAufRang(0);
            if (rang1 != null && WaermesenkeClass.IstPufferZiel(rang1.Ziel) && rang1.ID_Puffer > 0)
                puffer = WaermesenkeClass.PufferLesen(rang1.ID_Puffer);

            bool pufferPaar = puffer != null && puffer.Vorlauf > 0 && puffer.Ruecklauf > 0;

            string text;
            if (pufferPaar)
                text = string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR,
                                     puffer.Vorlauf, puffer.Ruecklauf);
            else if (info.Vorlauf > 0 && info.Ruecklauf > 0)
                text = string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR,
                                     info.Vorlauf, info.Ruecklauf);
            else
                return;   // nichts Gepflegtes - lieber kein Chip als eine erfundene Zahl

            bool warnung = pufferPaar && info.Vorlauf > 0 && info.Vorlauf < puffer.Vorlauf;

            chips.Add(new ChipDaten(text,
                warnung ? ChipStil.Warnung : ChipStil.Flaeche,
                warnung
                    ? string.Format(MyResource.Resource.SIM_KARTE_TIP_TEMPERATUR_WARNUNG,
                                    info.Vorlauf, puffer.Bezeichner, puffer.Vorlauf)
                    : ""));
        }

        /// <summary>
        /// Der WARN-Chip (Konzept 6.2; :1530-1543): ein dezentes Amber-Chip mit den
        /// Befunden im Hinweis, sonst gar nichts — <b>kein Modaldialog</b>.
        /// </summary>
        private void WarnChip(AnlagenInfo info, List<ChipDaten> chips)
        {
            List<string> texte;
            if (!_warnbefunde.TryGetValue(info.ID, out texte) || texte.Count == 0) return;

            chips.Add(new ChipDaten(MyResource.Resource.SIMWARN_KARTE_CHIP, ChipStil.Warnung,
                MyResource.Resource.SIMWARN_KARTE_CHIP_TIP + Environment.NewLine +
                "• " + string.Join(Environment.NewLine + "• ", texte.ToArray()),
                ChipZiel.Senke));
        }

        private Dictionary<int, List<string>> WarnbefundeSammeln()
        {
            Dictionary<int, List<string>> map = new Dictionary<int, List<string>>();
            if (m_ID_Projekt <= 0) return map;

            foreach (Warnbefund b in Warnkriterien.PruefeProjekt(m_ID_Projekt))
            {
                if (b == null || b.ID_Anlage <= 0 || string.IsNullOrEmpty(b.Text)) continue;

                List<string> texte;
                if (!map.TryGetValue(b.ID_Anlage, out texte))
                {
                    texte = new List<string>();
                    map[b.ID_Anlage] = texte;
                }

                string zeile = Zeilenumbruch.Einzeilig(b.Text);
                if (!texte.Contains(zeile)) texte.Add(zeile);
            }

            return map;
        }

        /// <summary>Anzeigetext des Betriebsmodus (:587-594).</summary>
        private static string BetriebsmodusAnzeige(AnlagenInfo a)
        {
            switch (a.BM_Typ)
            {
                case WaermequelleClass.MODUS_LEISTUNG: return MyResource.Resource.SIM_MODUS_LEISTUNG;
                case WaermequelleClass.MODUS_PV: return MyResource.Resource.SIM_MODUS_PV;
                default: return MyResource.Resource.SIM_MODUS_LAUFZEIT;
            }
        }

        // =================================================================
        // Die Detailchips der Strom- und Speicherkachel (Abnahmebefund 3)
        // =================================================================

        private List<ChipDaten> StromDetailchips(int idType)
        {
            if (idType == WizardItemClass.SP_TYP) return SpeicherDetailchips();
            if (idType == WizardItemClass.PV_TYP) return PvDetailchips();
            return new List<ChipDaten>();
        }

        /// <summary>
        /// Gerätedaten aller Speicheranlagen plus die Betriebsführung der AKTIVEN
        /// Variante — die Einheit, die die Gesamtsimulation rechnet (:928-978).
        /// </summary>
        private List<ChipDaten> SpeicherDetailchips()
        {
            List<ChipDaten> chips = new List<ChipDaten>();
            if (m_ID_Projekt <= 0) return chips;

            CultureInfo kultur = CultureInfo.CurrentCulture;

            WErzeugerCtrl anlagen = new WErzeugerCtrl();
            anlagen.ReadAllFilter("ID_Projekt=" + m_ID_Projekt +
                                  " and ID_Type=" + WizardItemClass.SP_TYP);

            List<int> gezeigt = new List<int>();
            for (int i = 0; i < anlagen.rows; i++)
            {
                int idGeraet = anlagen.items[i].ID_SP;
                if (idGeraet <= 0 || gezeigt.Contains(idGeraet)) continue;
                gezeigt.Add(idGeraet);

                StromspeicherCtrl geraet = new StromspeicherCtrl();
                geraet.ReadSingle(idGeraet);
                if (geraet.m_ID <= 0) continue;

                Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_KAPAZITAET,
                                          geraet.m_Energie.ToString("N2", kultur)), ChipStil.Senke);
                Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_LEISTUNG,
                                          geraet.m_Leistung.ToString("N2", kultur)));

                double etaRt = geraet.m_WirkungsgradRT > 0.0
                    ? geraet.m_WirkungsgradRT : StromspeicherModel.WIRKUNGSGRAD_RT_VORGABE;
                Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_WIRKUNGSGRAD,
                                          etaRt.ToString("N2", kultur)));

                if (!string.IsNullOrEmpty(geraet.m_szTyp))
                    Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_TYP, geraet.m_szTyp),
                         ChipStil.Flaeche);

                if (geraet.m_ZyklenZugesichert > 0)
                    Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_ZYKLEN,
                                              geraet.m_ZyklenZugesichert.ToString("N0", kultur)));
            }

            if (chips.Count == 0)
            {
                Chip(chips, MyResource.Resource.SIM_KARTE_OHNE_GERAET, ChipStil.Flaeche);
                return chips;
            }

            SpeicherVariantenchips(chips, kultur);
            return chips;
        }

        /// <summary>Die Betriebsführung der aktiven Variante (:986-1020).</summary>
        private void SpeicherVariantenchips(List<ChipDaten> chips, CultureInfo kultur)
        {
            StromspeicherVarianteModel variante = null;
            try
            {
                variante = new StromspeicherVarianteCtrl().ReadAktiveVariante(m_ID_Projekt);
            }
            catch (Exception ex)
            {
                // Die Kachel ist Beiwerk - sie darf die Seite nicht kippen, wenn die
                // Variantentabelle (Migrationsschritt 11b) noch fehlt.
                Console.WriteLine("Die aktive Speichervariante konnte nicht gelesen werden: " +
                                  ex.Message);
            }

            if (variante == null)
            {
                Chip(chips, MyResource.Resource.SIM_KARTE_SP_OHNE_VARIANTE, ChipStil.Warnung);
                return;
            }

            Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_VARIANTE,
                                      BetriebsartAnzeige(variante.Betriebsart)), ChipStil.Quelle);
            Chip(chips, BerechnungsartAnzeige(variante.Berechnungsart), ChipStil.Quelle);
            Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_SP_BAND,
                                      variante.SoC_Min_Prozent.ToString("N0", kultur),
                                      variante.SoC_Max_Prozent.ToString("N0", kultur)));
            Chip(chips, variante.Netzentladung
                     ? MyResource.Resource.SIM_KARTE_SP_NETZENTLADUNG_AN
                     : MyResource.Resource.SIM_KARTE_SP_NETZENTLADUNG_AUS, ChipStil.Flaeche);
        }

        /// <summary>
        /// Gerätedaten aller PV-Anlagen (:1033-1073).
        /// <c>Tab_Energieanlagen.PV_Leistung</c> ist trotz seines Namens die MODULANZAHL.
        ///
        /// <para><b>Stufe S3 des Wechselrichterkonzepts:</b> Steht eine Anlage auf „mit
        /// Wechselrichter" und führt sie Stränge mit Gerät, kommen zwei Chips dazu —
        /// die Zahl der Geräte und Stränge und das DC/AC-Verhältnis der Anlage. Beides
        /// ist eine Zahl aus den STAMMDATEN und braucht keinen Lauf; die Kennzahlen des
        /// Laufs (Clipping, Jahresnutzungsgrad) stehen im Simulationsprotokoll und im
        /// Ergebnisreiter. <b>Ohne Zuordnung ändert sich an der Karte nichts.</b></para>
        /// </summary>
        private List<ChipDaten> PvDetailchips()
        {
            List<ChipDaten> chips = new List<ChipDaten>();
            if (m_ID_Projekt <= 0) return chips;

            CultureInfo kultur = CultureInfo.CurrentCulture;

            // Die Strangzeilen und Geräte des PROJEKTS in je einer Abfrage - und nur,
            // wenn überhaupt eine Anlage den Schalter trägt (dieselbe Zurückhaltung wie
            // in SimulationPV.Berechnung).
            List<AnlageStrangModel> alleStraenge = null;
            Dictionary<int, WechselrichterModel> alleGeraete = null;

            WErzeugerCtrl anlagen = new WErzeugerCtrl();
            anlagen.ReadAllFilter("ID_Projekt=" + m_ID_Projekt +
                                  " and ID_Type=" + WizardItemClass.PV_TYP);

            for (int i = 0; i < anlagen.rows; i++)
                if (SimulationPV.IstKatalogweg(anlagen.items[i]))
                {
                    alleStraenge = new AnlageStrangCtrl().LesenJeProjekt(m_ID_Projekt);

                    WechselrichterCtrl wr = new WechselrichterCtrl();
                    wr.ReadAll(m_ID_Projekt);
                    alleGeraete = new Dictionary<int, WechselrichterModel>();
                    for (int g = 0; g < wr.rows; g++) alleGeraete[wr.items[g].m_ID] = wr.items[g];
                    break;
                }

            for (int i = 0; i < anlagen.rows; i++)
            {
                WErzeugerModel anlage = anlagen.items[i];

                PhotovoltaikCtrl modul = new PhotovoltaikCtrl();
                if (anlage.ID_PV > 0) modul.ReadSingle(anlage.ID_PV);

                if (!string.IsNullOrEmpty(modul.m_szName))
                    Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_PV_MODUL, modul.m_szName),
                         ChipStil.Flaeche);

                long anzahl = (long)anlage.PV_Leistung;
                Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_PV_ANZAHL,
                                          anzahl.ToString("N0", kultur)), ChipStil.Quelle);

                Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_PV_AUSRICHTUNG,
                                          anlage.m_Neigung.ToString(kultur),
                                          anlage.m_Azimut.ToString(kultur)));

                if (modul.m_Leistung > 0.0 && anzahl > 0)
                    Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_PV_KWP,
                                              (modul.m_Leistung * anzahl / 1000.0)
                                                  .ToString("N2", kultur)));

                // PAKET B (Stufe E2, mit Merge 5 aus Form_Simulation_Config.Karten nachgezogen):
                // Das Rechenmodell steht nur dann auf der Karte, wenn es vom Bestand ABWEICHT -
                // das vereinfachte Modell ist der Regelfall und braucht keinen Chip. Das DC/AC-
                // Verhaeltnis kommt dazu, sobald eine AC-Nennleistung gepflegt ist; ohne sie
                // rechnet der Lauf ohne Clipping und sagt es im Protokoll.
                if (SimulationPV.IstErweitert(anlage))
                {
                    double kwp = modul.m_Leistung * anzahl / 1000.0;
                    double? nenn = anlage.PV_WrNennleistungKw;
                    Chip(chips, (nenn.HasValue && nenn.Value > 0.0 && kwp > 0.0)
                            ? string.Format(MyResource.Resource.SIM_KARTE_PV_MODELL_DCAC,
                                            (kwp / nenn.Value).ToString("N2", kultur))
                            : MyResource.Resource.SIM_KARTE_PV_MODELL_ERWEITERT,
                         ChipStil.Quelle);
                }

                WechselrichterChips(chips, kultur, anlage, modul, alleStraenge, alleGeraete);
            }

            if (chips.Count == 0)
                Chip(chips, MyResource.Resource.SIM_KARTE_OHNE_GERAET, ChipStil.Flaeche);

            return chips;
        }

        // iU9-W11a (Befund W11-B42, offener Punkt W11a-O-4): Diese beiden Methoden
        // waren die VIERTE Fassung derselben Uebersetzung - neben
        // Form_SpeicherVariantenVergleich, Form_SpeicherOptimierung und der
        // Ergebnisseite. Sie war zugleich die vollstaendigste: nur sie kannte die
        // Preissteuerung. Ihr Wissen steht jetzt in SpeicherAnzeigeCtrl (Kern), und
        // alle vier Aufrufer bekommen denselben Text.
        //
        // EIN UNTERSCHIED, bewusst: Ein unbekannter Wert kam hier als "Gruenstrom" bzw.
        // "Dauernutzung" zurueck; der Kern gibt ihn unveraendert weiter. Das ist eine
        // Behauptung weniger ueber Daten, die man nicht kennt - und unerreichbar,
        // solange alle Schreiber DbWerte.SP_* setzen.

        /// <summary>
        /// Die zwei Wechselrichter-Chips einer Anlage, die auf der STRANGEBENE rechnet
        /// (Stufe S3, <c>Konzept_Wechselrichter_EPOS-Plan.md</c> 4.4): wieviele Geräte
        /// und Stränge, und das DC/AC-Verhältnis.
        ///
        /// <para><b>Dieselbe Vorrangregel wie im Rechenkern</b> — geprüft wird über
        /// <c>SimulationPV.IstKatalogweg</c> und <c>PvStrangModell.Gruppieren</c>, damit
        /// die Karte nichts anzeigt, was der Lauf nicht rechnet. Ohne Zuordnung entsteht
        /// kein Chip.</para>
        /// </summary>
        private static void WechselrichterChips(List<ChipDaten> chips, CultureInfo kultur,
                                                WErzeugerModel anlage, PhotovoltaikCtrl modul,
                                                List<AnlageStrangModel> alleStraenge,
                                                Dictionary<int, WechselrichterModel> alleGeraete)
        {
            if (!SimulationPV.IstKatalogweg(anlage) || alleStraenge == null) return;

            List<AnlageStrangModel> eigene = new List<AnlageStrangModel>();
            foreach (AnlageStrangModel s in alleStraenge)
                if (s != null && s.ID_Anlage == anlage.ID) eigene.Add(s);
            if (eigene.Count == 0) return;

            int ohneGeraet;
            List<PvStrangModell.Geraetegruppe> geraete =
                PvStrangModell.Gruppieren(eigene, alleGeraete, out ohneGeraet);
            if (geraete.Count == 0) return;

            int straenge = 0;
            foreach (PvStrangModell.Geraetegruppe g in geraete)
            {
                straenge += g.Straenge.Count;

                double kwp = 0.0;
                foreach (AnlageStrangModel s in g.Straenge)
                    kwp += modul.m_Leistung / 1000.0 * s.Modulzahl;
                g.KwpDc = kwp;
            }

            Chip(chips, string.Format(MyResource.Resource.SIM_KARTE_PV_WR_ANZAHL,
                                      geraete.Count.ToString("N0", kultur),
                                      straenge.ToString("N0", kultur)),
                 ChipStil.Quelle);

            double dcAc = SimulationPV.DcAcDerAnlage(geraete);
            Chip(chips, dcAc > 0.0
                     ? string.Format(MyResource.Resource.SIM_KARTE_PV_WR_DCAC,
                                     dcAc.ToString("N2", kultur))
                     : MyResource.Resource.SIM_KARTE_PV_WR_OHNE_NENN,
                 ChipStil.Quelle);
        }

        private static string BetriebsartAnzeige(string dbWert)
        {
            return SpeicherAnzeigeCtrl.BetriebsartText(dbWert);
        }

        private static string BerechnungsartAnzeige(string dbWert)
        {
            return SpeicherAnzeigeCtrl.BerechnungsartText(dbWert);
        }

        private static void Chip(List<ChipDaten> chips, string text,
                                 ChipStil stil = ChipStil.Neutral)
        {
            if (string.IsNullOrEmpty(text)) return;
            chips.Add(new ChipDaten(text, stil));
        }

        // =================================================================
        // Die Speicherspalte (Konzept 3a)
        // =================================================================

        private List<SpeicherKachelDaten> SpeicherSpalte()
        {
            List<SpeicherKachelDaten> liste = new List<SpeicherKachelDaten>();
            if (m_ID_Projekt <= 0) return liste;

            foreach (WaermesenkeClass.PufferInfo p in
                     WaermesenkeClass.ProjektPufferListe(m_ID_Projekt, null))
                liste.Add(SpeicherKarteDaten(p));

            return liste;
        }

        /// <summary>Füllt eine Speicherkachel aus den Projektdaten (wörtlich :1923-2057).</summary>
        private SpeicherKachelDaten SpeicherKarteDaten(WaermesenkeClass.PufferInfo p)
        {
            SpeicherKachelDaten d = new SpeicherKachelDaten();
            d.IdPuffer = p.ID;
            d.Bezeichner = p.Bezeichner.Length > 0
                ? p.Bezeichner : MyResource.Resource.PSP_BEZEICHNER_ERSATZ;

            string kanal = WaermesenkeClass.WirksameVerwendung(p);
            d.Verwendung = WaermesenkeClass.VerwendungAnzeige(kanal);

            // PAKET P1: Schicht-Badge „N Schichten". Nur bei N > 1 - das Verzeichnis
            // führt Ein-Zonen-Speicher gar nicht erst.
            int schichten;
            if (_schichtenJePuffer.TryGetValue(p.ID, out schichten))
                d.Schichtung = string.Format(MyResource.Resource.PSP_KARTE_SCHICHTEN, schichten);

            if (p.Gesamtvolumen > 0)
                d.Volumen = string.Format(MyResource.Resource.PSP_KARTE_VOLUMEN, p.Gesamtvolumen);

            int vorlauf, ruecklauf;
            string herkunft = TemperaturHerkunft(p, out vorlauf, out ruecklauf);
            if (vorlauf > 0 && ruecklauf > 0)
                d.Temperaturpaar = string.Format(MyResource.Resource.SIM_KARTE_TEMPERATURPAAR,
                                                 vorlauf, ruecklauf);

            List<string> zeilen = new List<string>();

            // --- Lader in wirksamer Reihenfolge (Ladeordnung 3.4) ------------------
            //
            // VORFILTER: Für einen Speicher, den niemand lädt, ist das Ergebnis
            // garantiert leer - die Abfrage also verschenkt (Projekt 1023: 79 Puffer,
            // einer davon geladen).
            List<Ladeordnung.LadeEintrag> lader = _geladenePuffer.Contains(p.ID)
                ? Ladeordnung.Ladereihenfolge(m_ID_Projekt, p.ID)
                : new List<Ladeordnung.LadeEintrag>();
            d.LaderAnzahl = lader.Count;

            if (lader.Count > 0)
            {
                List<string> teile = new List<string>();
                for (int i = 0; i < lader.Count; i++)
                {
                    Ladeordnung.LadeEintrag e = lader[i];
                    string name = e.Bezeichner.Length > 0 ? e.Bezeichner : e.Erzeuger;

                    string zeile = (i + 1) + ". " + name + " (" +
                                   string.Format(MyResource.Resource.SIM_POSITION_BIS,
                                                 e.Obergrenze.ToString("0.#")) + ")";

                    if (e.Zweitsenke) zeile += " " + MyResource.Resource.SIM_ROLLE_ZWEITSENKE;
                    if (e.LadeprioPV > 0)
                        zeile += " " + string.Format(MyResource.Resource.PSP_KARTE_PV_RANG,
                                                     e.LadeprioPV);

                    teile.Add(zeile);
                }
                zeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_LADER,
                                         string.Join(" · ", teile.ToArray())));
            }
            else
            {
                zeilen.Add(MyResource.Resource.PSP_KARTE_LADER_KEINE);
            }

            // --- Versorgt: der Kanal, aus dem entladen wird ------------------------
            zeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_VERSORGT, d.Verwendung));

            // --- PARALLELVERBUND ---------------------------------------------------
            //
            // Ein MITGLIED hat im Lauf keinen eigenen Füllstand: Seine Kapazität steckt
            // im Leitspeicher, und in Tab_ErgebnisPufferspeicher steht keine Zeile für
            // ihn. Ohne diese Zeile suchte der Anwender im Ergebnis nach einem Speicher,
            // den es dort nicht gibt.
            int idLeit = AnlagePufferVerbundCtrl.LeitspeicherFuerMitglied(p.ID);
            if (idLeit > 0 && idLeit != p.ID)
                zeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_IM_VERBUND,
                                         WaermesenkeClass.PufferName(idLeit)));

            // --- Quelle für: NUR Erzeuger (Invariante S-1) -------------------------
            List<string> quelleFuer = QuelleFuerAnlagen(p);
            if (quelleFuer.Count > 0)
                zeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_QUELLE_FUER,
                                         string.Join(" · ", quelleFuer.ToArray())));

            // Ein Abnehmer ist der eigene Kanal; jede Kaskadenentnahme kommt hinzu.
            d.AbnehmerAnzahl = 1 + quelleFuer.Count;

            // --- Entladepriorität ---------------------------------------------------
            bool manuell = p.Entladeprio >= Ladeordnung.PRIO_MIN &&
                           p.Entladeprio <= Ladeordnung.PRIO_MAX;

            // Der Automatikwert ist die BESTE Ladepriorität am Speicher (Konzept 3.6) -
            // also der erste Eintrag der bereits geholten Ladereihenfolge.
            int automatik = lader.Count > 0 ? lader[0].Ladeprio : Ladeordnung.PRIO_SONSTIGE;

            string prio = manuell
                ? string.Format(MyResource.Resource.PSP_LADEPRIO_MANUELL, p.Entladeprio)
                : string.Format(MyResource.Resource.PSP_PRIO_AUTOMATISCH_WERT, automatik);
            zeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_ENTLADEPRIO, prio));

            // --- Temperaturherkunft --------------------------------------------------
            zeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_TEMP_HERKUNFT, herkunft));

            // --- PAKET P1: Ergebnistemperatur der obersten Schicht --------------------
            double tOben;
            if (_tObenJePuffer.TryGetValue(p.ID, out tOben))
                zeilen.Add(string.Format(MyResource.Resource.PSP_KARTE_T_OBEN,
                                         tOben.ToString("0.#")));

            d.Detailzeilen = zeilen;

            // --- Schwellenband -------------------------------------------------------
            d.SchwelleEin = p.SchwelleEin;
            d.SchwelleAusNachrang = p.SchwelleAusNachrang;
            d.SchwelleAus = p.SchwelleAus;
            d.Schwellentext = string.Format(MyResource.Resource.PSP_KARTE_SCHWELLEN,
                                            p.SchwelleEin.ToString("0.#"),
                                            p.SchwelleAusNachrang.ToString("0.#"),
                                            p.SchwelleAus.ToString("0.#"));
            return d;
        }

        /// <summary>
        /// Abbildung Quellpuffer → Anlagen, die ihn als WÄRMEQUELLE nutzen. EINMAL je
        /// Auffrischung (Befund: über 300 zusätzliche Abfragen bei Projekt 1023).
        /// </summary>
        private Dictionary<int, List<string>> QuellnutzerSammeln()
        {
            Dictionary<int, List<string>> map = new Dictionary<int, List<string>>();
            if (m_ID_Projekt <= 0) return map;

            foreach (AnlagenKurz a in WErzeugerCtrl.Quellnutzer(m_ID_Projekt))
            {
                int idPuffer = WaermesenkeClass.QuellPufferDerAnlage(m_ID_Projekt, a.ID);
                if (idPuffer <= 0 || a.Bezeichner.Length == 0) continue;

                if (!map.ContainsKey(idPuffer)) map[idPuffer] = new List<string>();
                map[idPuffer].Add(a.Bezeichner + " " + MyResource.Resource.PSP_KARTE_KASKADE);
            }

            return map;
        }

        /// <summary>Alle Puffer, die überhaupt von einer Anlage GELADEN werden.</summary>
        private HashSet<int> GeladenePufferSammeln()
        {
            HashSet<int> geladen = new HashSet<int>();
            if (m_ID_Projekt <= 0) return geladen;

            foreach (Senkenliste liste in WaermesenkeClass.SenkenlistenLadenStill(m_ID_Projekt))
            {
                if (liste == null) continue;

                foreach (Senkenzeile z in liste.Zeilen)
                    if (z != null && z.IstPuffersenke && z.IDPuffer > 0) geladen.Add(z.IDPuffer);
            }

            return geladen;
        }

        /// <summary>
        /// PAKET P1 — <c>T_oben_Mittel</c> je Speicher aus dem JÜNGSTEN Ergebnis.
        /// Zwei Abfragen statt einer Unterabfrage; die Kopfabfrage steht seit
        /// iU9-W10b.0b als <c>ErgebnisCtrl.LetzteErgebnisId</c> im Kern.
        /// </summary>
        private Dictionary<int, double> TObenSammeln()
        {
            Dictionary<int, double> werte = new Dictionary<int, double>();
            if (m_ID_Projekt <= 0) return werte;

            int idErgebnis = ErgebnisCtrl.LetzteErgebnisId(m_ID_Projekt);
            if (idErgebnis <= 0) return werte;

            System.Data.DataTable dt = ErgebnisCtrl.PufferZeilenLesenStill(idErgebnis);
            if (dt == null || !dt.Columns.Contains(SchemaKatalog.SPALTE_PUFFER_T_OBEN_MITTEL))
                return werte;

            foreach (System.Data.DataRow r in dt.Rows)
            {
                int id = StilleDb.Zahl(StilleDb.Feld(r, "ID_Pufferspeicher"));
                object v = StilleDb.Feld(r, SchemaKatalog.SPALTE_PUFFER_T_OBEN_MITTEL);
                if (id <= 0 || v == null || v == DBNull.Value || werte.ContainsKey(id)) continue;

                try { werte[id] = Convert.ToDouble(v); }
                catch { /* unlesbarer Wert - dann bleibt die Zeile weg */ }
            }

            return werte;
        }

        private List<string> QuelleFuerAnlagen(WaermesenkeClass.PufferInfo p)
        {
            List<string> namen;
            if (p != null && _quellnutzer.TryGetValue(p.ID, out namen)) return namen;

            return new List<string>();
        }

        /// <summary>
        /// Herkunft der Betriebstemperaturen — die Vorrangkette aus Paket 1/4
        /// (eigene Werte, dann die Systemvorgabe; wörtlich :2225-2247).
        /// </summary>
        private string TemperaturHerkunft(WaermesenkeClass.PufferInfo p,
                                          out int vorlauf, out int ruecklauf)
        {
            vorlauf = 0;
            ruecklauf = 0;
            if (p == null) return MyResource.Resource.PSP_KARTE_TEMP_KEINE;

            if (p.Vorlauf > 0 && p.Ruecklauf > 0)
            {
                vorlauf = p.Vorlauf;
                ruecklauf = p.Ruecklauf;
                return MyResource.Resource.PSP_KARTE_TEMP_EIGEN;
            }

            if (ProjektPuffer.IstTemperaturpaar(_systemVorlauf, _systemRuecklauf))
            {
                vorlauf = _systemVorlauf.Value;
                ruecklauf = _systemRuecklauf.Value;
                return MyResource.Resource.PSP_KARTE_TEMP_SYSTEM;
            }

            return MyResource.Resource.PSP_KARTE_TEMP_KEINE;
        }

        // =================================================================
        // Das Schema
        // =================================================================

        /// <summary>
        /// Baut das Schemamodell, ersetzt seine Hinweise durch die KURZINFO DER KACHEL
        /// (Aufgabe D4-2) und ordnet es an (<c>SchemaLayout</c>, iU9-W10b.0a).
        /// </summary>
        private SchemaBild SchemaLaden(int idProjekt)
        {
            if (idProjekt != m_ID_Projekt) ProjektSetzen(idProjekt);
            if (_gesperrt) return SchemaBild.Leer;

            SchemaModell modell = SchemaModell.Aufbauen(m_ID_Projekt, Kaskade.Belegt(_konfiguration));
            SchemaHinweiseSetzen(modell);

            return SchemaAbbilden(SchemaLayout.Anordnen(modell, 0));
        }

        /// <summary>
        /// Ersetzt die Modell-Hinweise durch die Kartenkurzinfo. Das Modell baut sich
        /// einen eigenen, knappen Hinweis — es soll ohne Oberfläche tragfähig bleiben.
        /// Sobald es aber IN dieser Seite gezeigt wird, ist die Kachelkurzinfo die
        /// bessere Auskunft (wörtlich <c>SchemaHinweiseSetzen</c>:224-285).
        /// </summary>
        private void SchemaHinweiseSetzen(SchemaModell modell)
        {
            if (modell == null || m_ID_Projekt <= 0) return;

            // Warn-Chip und Booster-Badge sind Teil der Kartenkurzinfo und müssen
            // deshalb auch hier frisch sein - die Schema-Ansicht kann aufgefrischt
            // werden, ohne dass die Kachelspalte neu gebaut wurde (Umschalter).
            _warnbefunde = WarnbefundeSammeln();
            _boosterAnlagen = Warnkriterien.BoosterAnlagen(m_ID_Projekt);

            Dictionary<int, string> chips = new Dictionary<int, string>();
            foreach (string dbWert in Kaskade.Belegt(_konfiguration))
            {
                int idType = Kaskade.TypZuAnlagentyp(dbWert);
                if (idType <= 0) continue;

                List<AnlagenInfo> anlagen = WErzeugerCtrl.AnlagenMitWp(m_ID_Projekt, idType);
                for (int a = 0; a < anlagen.Count; a++)
                {
                    AnlagenInfo info = anlagen[a];
                    _anlagen[info.ID] = info;

                    List<string> zeilen = new List<string>();
                    foreach (ChipDaten c in ErzeugerChips(info, a + 1, anlagen.Count))
                        if (c != null && !string.IsNullOrEmpty(c.Text)) zeilen.Add(c.Text);

                    chips[info.ID] = string.Join(Environment.NewLine, zeilen.ToArray());
                }
            }

            Dictionary<int, string> speicher = new Dictionary<int, string>();
            _quellnutzer = QuellnutzerSammeln();
            _geladenePuffer = GeladenePufferSammeln();
            _systemVorlauf = PufferSpCtrl.SystemVorlauf(m_ID_Projekt);
            _systemRuecklauf = PufferSpCtrl.SystemRuecklauf(m_ID_Projekt);

            foreach (WaermesenkeClass.PufferInfo p in
                     WaermesenkeClass.ProjektPufferListe(m_ID_Projekt, null))
            {
                if (p == null ||
                    modell.Finden(SchemaModell.PRAEFIX_SPEICHER + p.ID) == null) continue;

                SpeicherKachelDaten d = SpeicherKarteDaten(p);
                List<string> zeilen = new List<string>(d.Detailzeilen);
                if (!string.IsNullOrEmpty(d.Schwellentext)) zeilen.Add(d.Schwellentext);
                speicher[p.ID] = string.Join(Environment.NewLine, zeilen.ToArray());
            }

            foreach (SchemaModell.Knoten k in modell.Knotenliste)
            {
                string text;
                if (k.Art == SchemaModell.Knotenart.Erzeuger && chips.TryGetValue(k.ID, out text))
                    k.Hinweis = text;
                else if (k.Art == SchemaModell.Knotenart.Speicher &&
                         speicher.TryGetValue(k.ID, out text))
                    k.Hinweis = text;
            }
        }

        /// <summary>
        /// Bildet das angeordnete Layout des Kerns auf den Satz ab, den der Baustein
        /// <c>Schema</c> zeichnet — eine Zuordnung ohne Rechnung.
        /// </summary>
        private static SchemaBild SchemaAbbilden(SchemaLayout l)
        {
            if (l == null || l.IstLeer) return SchemaBild.Leer;

            List<SchemaKnoten> knoten = new List<SchemaKnoten>();
            foreach (SchemaLayout.Knotenflaeche k in l.Knoten)
                knoten.Add(new SchemaKnoten(
                    k.Schluessel, (SchemaKnotenart)(int)k.Knoten.Art,
                    k.Flaeche.X, k.Flaeche.Y, k.Flaeche.Breite, k.Flaeche.Hoehe,
                    k.Knoten.Rang, k.Knoten.Titel,
                    k.Knoten.Zeilen, k.Knoten.Badges,
                    k.Knoten.Hinweis, k.Knoten.Warnung, k.Knoten.Warntext, k.Knoten.Kaskade,
                    k.Knoten.ID_Type == ProjektPuffer.TYP_WP));

            List<SchemaKante> kanten = new List<SchemaKante>();
            foreach (SchemaLayout.Kantenzug z in l.Kanten)
                kanten.Add(new SchemaKante(
                    z.Kante.Von, z.Kante.Nach, (SchemaKantenart)(int)z.Art, z.Prioritaet,
                    Streckenzug(z), z.Mitte.X, z.Mitte.Y));

            List<SchemaBandglied> band = new List<SchemaBandglied>();
            foreach (SchemaLayout.Bandflaeche b in l.Band)
                band.Add(new SchemaBandglied(
                    b.Schluessel, b.Glied.Text, (SchemaKnotenart)(int)b.Glied.Art,
                    (SchemaKantenart)(int)b.Glied.PfeilDavor,
                    b.Flaeche.X, b.Flaeche.Y, b.Flaeche.Breite, b.Flaeche.Hoehe,
                    b.Kettenanfang));

            // Fünf Einträge, Farben index-gekoppelt, Strichelung an einem eigenen Feld
            // (Paket E1 - ein eingeschobener Eintrag hätte sie sonst verschoben).
            List<SchemaLegendeeintrag> legende = new List<SchemaLegendeeintrag>
            {
                new SchemaLegendeeintrag(MyResource.Resource.SIM_SCHEMA_LEGENDE_LADUNG,
                                         SchemaKantenart.Ladung, false),
                new SchemaLegendeeintrag(MyResource.Resource.SIM_SCHEMA_LEGENDE_VERSORGUNG,
                                         SchemaKantenart.Versorgung, false),
                new SchemaLegendeeintrag(MyResource.Resource.SIM_SCHEMA_LEGENDE_PROZESS,
                                         SchemaKantenart.Prozess, false),
                new SchemaLegendeeintrag(MyResource.Resource.SIM_SCHEMA_LEGENDE_QUELLE,
                                         SchemaKantenart.Quelle, false),
                new SchemaLegendeeintrag(MyResource.Resource.SIM_SCHEMA_LEGENDE_KASKADE,
                                         SchemaKantenart.Kaskade, true)
            };

            return new SchemaBild(
                knoten, kanten, band, legende,
                new List<string>
                {
                    MyResource.Resource.SIM_SCHEMA_SPALTE_QUELLE,
                    MyResource.Resource.SIM_SCHEMA_SPALTE_ERZEUGER,
                    MyResource.Resource.SIM_SCHEMA_SPALTE_SPEICHER,
                    MyResource.Resource.SIM_SCHEMA_SPALTE_ABNEHMER
                },
                new List<int>(l.SpaltenX),
                new List<int>(SchemaLayout.SPALTEN_BREITE),
                l.InhaltBreite, l.Gesamthoehe, SchemaLayout.RAND, SchemaLayout.KOPF_HOEHE,
                l.BandOben, l.LegendeOben,
                SchemaLayout.LINIE_BREITE, SchemaLayout.LINIE_BREITE_HERVOR,
                l.Modell != null && l.Modell.HatKaskade, false);
        }

        /// <summary>
        /// Der Streckenzug einer Kante als SVG-Pfadangabe („M … L … L …").
        ///
        /// <para>Anwenderbefund W10b-B-1 (05.09.2026): Bis hierher stand hier ein
        /// kubischer Bezierbogen („M … C …"). Er lief bei einer übersprungenen Spalte
        /// quer durch die Kästen; der Kern legt die Leitung seither in Spaltenbahnen aus
        /// lauter waagerechten und senkrechten Stücken.</para>
        /// </summary>
        private static string Streckenzug(SchemaLayout.Kantenzug z)
        {
            CultureInfo k = CultureInfo.InvariantCulture;
            StringBuilder b = new StringBuilder();

            for (int i = 0; i < z.Punkte.Count; i++)
            {
                b.Append(i == 0 ? "M" : " L");
                b.Append(z.Punkte[i].X.ToString(k));
                b.Append(',');
                b.Append(z.Punkte[i].Y.ToString(k));
            }

            return b.ToString();
        }

        // =================================================================
        // Fußzeile
        // =================================================================

        /// <summary>
        /// Schreibt <c>Tool_1..6</c> weg (wörtlich <c>btn_Speichern_Click</c>:396-454).
        ///
        /// <para>FRAGE 23: <c>Extrapolation_erlaubt</c> steht nicht in der Spaltenliste
        /// von <c>Insert</c>; dort zieht ein stilles UPDATE die Vorbelegung WAHR nach.
        /// Der Lesezugriff VOR dem Delete unterscheidet „neues Projekt" von „bewusste
        /// Abwahl"; zurückgeschrieben wird deshalb nur die ABWAHL.</para>
        /// </summary>
        private bool Speichern()
        {
            if (_gesperrt) return false;

            KonfigurationCtrl ctrl = new KonfigurationCtrl();
            bool extrapolationErlaubt = KonfigurationCtrl.ExtrapolationErlaubtLesen(m_ID_Projekt);

            ctrl.model = _konfiguration;
            if (!ctrl.Delete(m_ID_Projekt)) return false;
            if (!ctrl.Insert(m_ID_Projekt)) return false;

            if (!extrapolationErlaubt)
                KonfigurationCtrl.ExtrapolationErlaubtSchreiben(m_ID_Projekt, false);

            return true;
        }

        private bool LesepunktSchreiben(bool davor)
        {
            if (m_ID_Projekt <= 0) return false;

            return KonfigurationCtrl.BoosterLesepunktSchreiben(
                m_ID_Projekt,
                davor ? DbWerte.BOOSTER_LESEPUNKT_DAVOR : DbWerte.BOOSTER_LESEPUNKT_DANACH);
        }

        // =================================================================
        // Die Editoren
        // =================================================================

        /// <summary>
        /// Die wählbaren Quellentypen einer Erzeugerart — sie hängen seit Etappe D5b am
        /// <c>ID_Type</c>: Die Wärmepumpe bekommt die sechs bekannten Typen, der
        /// Heizkessel genau zwei („Systemrücklauf", „Pufferspeicher").
        /// </summary>
        private static IReadOnlyList<Quellentyp> Quellentypen(int idType)
        {
            string[] werte = WaermequelleClass.TypWerteFuer(idType);
            string[] anzeige = WaermequelleClass.TypAnzeigeFuer(idType);

            List<Quellentyp> liste = new List<Quellentyp>();
            for (int i = 0; i < werte.Length; i++)
                liste.Add(new Quellentyp(werte[i], i < anzeige.Length ? anzeige[i] : werte[i]));

            return liste;
        }

        /// <summary>Die drei Zweige ohne Unterdialog (:858-885).</summary>
        private void QuelleEinfachSchreiben(int idAnlage, string typ, double wert)
        {
            WaermequelleClass.QuelleSchreiben(idAnlage, new QuelleErgebnis
            {
                Typ = typ,
                Temperatur = wert
            });
        }

        private IReadOnlyDictionary<string, object> QuellePufferGaben(int idAnlage)
        {
            AnlagenInfo info = Anlage(idAnlage);
            if (info == null) return null;

            object oIdPuffer = WaermequelleClass.WertLesen(idAnlage, "WQ_ID_Puffer");
            object oTemp = WaermequelleClass.WertLesen(idAnlage, "WQ_Temp");
            object oSpreiz = WaermequelleClass.WertLesen(idAnlage, "WQ_Spreizung");
            object oReg = WaermequelleClass.WertLesen(idAnlage, "WQ_Regeneration");
            object oUnb = WaermequelleClass.WertLesen(idAnlage, "WQ_Unbegrenzt");
            object oHoehe = WaermequelleClass.WertLesen(idAnlage, "WQ_Anschlusshoehe");

            var daten = new EPOS.UI.Dialoge.Simulation.QuellePufferspeicherDaten
            {
                WPName = info.Bezeichner,
                IdProjekt = m_ID_Projekt,
                IstKessel = !info.IstWaermepumpe,
                IdPuffer = oIdPuffer != null ? Convert.ToInt32(oIdPuffer) : 0,
                Pufferspeicher = WaermequelleClass.WertLesen(idAnlage, "WQ_Puffer") as string ?? "",
                Quelltemperatur = oTemp != null ? Convert.ToDouble(oTemp) : 10.0,
                Spreizung = (oSpreiz != null && Convert.ToDouble(oSpreiz) > 0)
                    ? Convert.ToDouble(oSpreiz) : 5.0,
                Regeneration = oReg != null ? Convert.ToDouble(oReg) : 0.0,
                Unbegrenzt = oUnb != null && Convert.ToBoolean(oUnb),
                Anschlusshoehe = oHoehe != null ? Convert.ToDouble(oHoehe) : (double?)null,

                TemperaturModus = DbWerte.TemperaturModusOderDefault(
                    WaermequelleClass.WertLesen(idAnlage,
                        SchemaKatalog.SPALTE_ANLAGE_WQ_TEMPERATURMODUS)),
                VorlaufAnlage = info.Vorlauf,
                RuecklaufAnlage = info.Ruecklauf
            };

            return QuellePufferspeicherHuelle.Gaben(daten);
        }

        /// <summary>
        /// Die beiden Dialogprüfungen aus Konzept Abschnitt 7, BEVOR irgendetwas
        /// geschrieben wird (Kurzschluss, Kaskadenzyklus; wörtlich :959-973). Die
        /// Engine-Guards bleiben als zweite Verteidigungslinie.
        /// </summary>
        private Rueckmeldung QuellePufferSchreiben(
            int idAnlage, EPOS.UI.Dialoge.Simulation.QuellePufferspeicherDaten q)
        {
            AnlagenInfo info = Anlage(idAnlage);
            if (info == null || q == null) return Rueckmeldung.Still;

            WaermesenkeClass.QuellPruefErgebnis pruef =
                WaermesenkeClass.QuellePruefen(m_ID_Projekt, idAnlage, q.IdPuffer);
            if (!pruef.Ok)
                return new Rueckmeldung(false, Zeilenumbruch.Einzeilig(pruef.Fehler));

            WaermequelleClass.QuelleSchreiben(idAnlage, new QuelleErgebnis
            {
                Typ = WaermequelleClass.TYP_PUFFER,
                IstWaermepumpe = info.IstWaermepumpe,
                IdPuffer = q.IdPuffer,
                Pufferspeicher = q.Pufferspeicher,
                Quelltemperatur = q.Quelltemperatur,
                Spreizung = q.Spreizung,
                Regeneration = q.Regeneration,
                Unbegrenzt = q.Unbegrenzt,
                Anschlusshoehe = q.Anschlusshoehe,
                TemperaturModus = q.TemperaturModus,
                VorlaufAnlage = q.VorlaufAnlage,
                RuecklaufAnlage = q.RuecklaufAnlage
            });

            return Rueckmeldung.Still;
        }

        private IReadOnlyDictionary<string, object> QuellprofilGaben(int idAnlage)
        {
            AnlagenInfo info = Anlage(idAnlage);
            if (info == null) return null;

            object oIdProfil = WaermequelleClass.WertLesen(idAnlage, "WQ_ID_Quellprofil");

            var daten = new EPOS.UI.Dialoge.Simulation.QuellprofilDaten
            {
                WPName = info.Bezeichner,
                IdProjekt = m_ID_Projekt,
                IdQuellprofil = oIdProfil != null ? Convert.ToInt32(oIdProfil) : 0,
                Monatswerte = QuellprofilCtrl.MonatswerteParsen(
                    WaermequelleClass.WertLesen(idAnlage, "WQ_Monatswerte") as string),
                Wochenwerte = QuellprofilCtrl.WochenwerteParsen(
                    WaermequelleClass.WertLesen(idAnlage, "WQ_Wochenwerte") as string)
            };

            return QuellprofilHuelle.Gaben(daten);
        }

        private IReadOnlyDictionary<string, object> QuelleErdreichGaben(int idAnlage)
        {
            AnlagenInfo info = Anlage(idAnlage);
            if (info == null) return null;

            string quellsystem = WaermequelleClass.WertLesen(idAnlage, "WQ_Quellsystem") as string;
            object oTiefe = WaermequelleClass.WertLesen(idAnlage, "WQ_Tiefe");
            object oFlaeche = WaermequelleClass.WertLesen(idAnlage, "WQ_Flaeche");
            object oAnzahl = WaermequelleClass.WertLesen(idAnlage, "WQ_Anzahl");
            string bodentyp = WaermequelleClass.WertLesen(idAnlage, "WQ_Bodentyp") as string;
            object oSpreiz = WaermequelleClass.WertLesen(idAnlage, "WQ_Spreizung");

            var daten = new EPOS.UI.Dialoge.Simulation.QuelleErdreichDaten
            {
                WPName = info.Bezeichner,
                IdProjekt = m_ID_Projekt,
                IdAnlage = idAnlage,
                Quellsystem = string.IsNullOrEmpty(quellsystem) ? "" : quellsystem,
                Tiefe = (oTiefe != null && Convert.ToDouble(oTiefe) > 0)
                    ? Convert.ToDouble(oTiefe) : 0.0,
                Flaeche = oFlaeche != null ? Convert.ToDouble(oFlaeche) : 0.0,
                Anzahl = (oAnzahl != null && Convert.ToInt32(oAnzahl) > 0)
                    ? Convert.ToInt32(oAnzahl) : 0,
                Bodentyp = string.IsNullOrEmpty(bodentyp) ? "" : bodentyp,
                Klimazone = KlimaregionCtrl.KlimazoneJeProjekt(m_ID_Projekt),
                Spreizung = (oSpreiz != null && Convert.ToDouble(oSpreiz) > 0)
                    ? Convert.ToDouble(oSpreiz) : 0.0,
                Aussentemperatur = AussentemperaturLaden()
            };

            return QuelleErdreichHuelle.Gaben(daten);
        }

        /// <summary>
        /// Die Klimazone ist eine Eigenschaft der REGION, nicht der Anlage
        /// (Konzept 13.1) — eine Änderung geht deshalb an die Region zurück.
        /// </summary>
        private void QuelleErdreichSchreiben(
            int idAnlage, EPOS.UI.Dialoge.Simulation.QuelleErdreichDaten e)
        {
            if (e == null) return;

            if (e.Klimazone != KlimaregionCtrl.KlimazoneJeProjekt(m_ID_Projekt))
                KlimaregionCtrl.KlimazoneJeProjektSchreiben(m_ID_Projekt, e.Klimazone);

            WaermequelleClass.QuelleSchreiben(idAnlage, new QuelleErgebnis
            {
                Typ = WaermequelleClass.TYP_ERDREICH,
                Quellsystem = e.Quellsystem,
                Tiefe = e.Tiefe,
                Flaeche = e.Flaeche,
                Anzahl = e.Anzahl,
                Bodentyp = e.Bodentyp,
                SpreizungErdreich = e.Spreizung
            });
        }

        /// <summary>
        /// Der CSV-Zweig (:1064-1089): Datei wählen, Profil prüfen, Pfad schreiben.
        /// Die Rückfrage davor stellt die SEITE.
        /// </summary>
        private async System.Threading.Tasks.Task<Rueckmeldung> QuelleCsvWaehlen(int idAnlage)
        {
            // Der Wähler läuft HINTER dem Blazor-Ereignis (Befund W13‑B‑1,
            // siehe IDateiDienst) - deshalb await statt eines synchronen Aufrufs.
            string pfad = await Dienste.Datei.DateiOeffnenAsync(
                MyResource.Resource.SIMQ_CSV_DATEIDIALOG_TITEL,
                MyResource.Resource.SIMQ_CSV_DATEIFILTER, "");

            if (string.IsNullOrEmpty(pfad))
                return Rueckmeldung.Still;

            if (WaermequelleClass.ProfilAusCsv(pfad) == null)
                return new Rueckmeldung(false,
                    Zeilenumbruch.Einzeilig(
                        string.Format(MyResource.Resource.SIMQ_CSV_FEHLER,
                                      WaermequelleClass.CSV_FORMAT_HINWEIS)));

            WaermequelleClass.QuelleSchreiben(idAnlage, new QuelleErgebnis
            {
                Typ = WaermequelleClass.TYP_CSV,
                CsvPfad = pfad
            });

            return Rueckmeldung.Still;
        }

        private IReadOnlyDictionary<string, object> WaermesenkeGaben(int idAnlage)
        {
            AnlagenInfo info = Anlage(idAnlage);
            if (info == null) return null;

            var daten = new EPOS.UI.Dialoge.Simulation.WaermesenkeDaten
            {
                IdProjekt = m_ID_Projekt,
                IdAnlage = idAnlage,
                IdType = info.ID_Type,
                AnlagenName = info.Bezeichner,
                PvModus = string.Equals(info.BM_Typ, WaermequelleClass.MODUS_PV,
                                        StringComparison.Ordinal),
                VerbundMitglieder = WaermesenkeClass.VerbundLesen(idAnlage)
            };

            return WaermesenkeHuelle.Gaben(daten);
        }

        /// <summary>
        /// Der Senkendialog SPEICHERT selbst; hier bleibt nur die Statusmeldung
        /// (wörtlich :756-784). <c>null</c> = abgebrochen, dann wurde nichts geschrieben.
        /// </summary>
        private Rueckmeldung WaermesenkeFertig(
            int idAnlage, EPOS.UI.Dialoge.Simulation.WaermesenkeErgebnis ergebnis)
        {
            if (ergebnis == null) return Rueckmeldung.Still;

            if (!ergebnis.SpeichernOk)
                return new Rueckmeldung(false, MyResource.Resource.SIM_STATUS_SENKE_FEHLER);

            // Die Kurzform der Rang-1-Senke für die Statuszeile. Die Komponente kennt
            // Z_AnlageSenkeModel nicht; das Modell entsteht deshalb hier.
            Z_AnlageSenkeModel rang1 = null;
            if (ergebnis.Zeilen.Count > 0)
            {
                EPOS.UI.Dialoge.Simulation.SenkenzeileDaten z = ergebnis.Zeilen[0];
                rang1 = new Z_AnlageSenkeModel
                {
                    ID_Anlage = idAnlage,
                    Rang = 1,
                    Ziel = z.Ziel,
                    ID_Puffer = z.IdPuffer,
                    Bedarfsart = z.Bedarfsart
                };
            }

            return new Rueckmeldung(true,
                string.Format(MyResource.Resource.SIM_STATUS_SENKE_GESPEICHERT,
                              WaermesenkeClass.SenkeAnzeige(rang1)));
        }

        /// <summary>
        /// Die Außentemperatur der Projekt-Klimaregion (8760 Stundenwerte) für die
        /// Vorschau des Erdreichdialogs — einmal je Sitzung geladen und gecacht.
        /// </summary>
        private float[] AussentemperaturLaden()
        {
            if (_aussentempGeladen) return _aussentempCache;

            _aussentempGeladen = true;
            _aussentempCache = KlimaregionCtrl.Aussentemperatur(m_ID_Projekt);
            return _aussentempCache;
        }
    }
}
