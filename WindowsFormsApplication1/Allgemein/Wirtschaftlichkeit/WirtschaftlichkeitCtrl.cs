using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Steuerung der Wirtschaftlichkeitsberechnung (Konzept_Wirtschaftlichkeit.md
    /// Kap. 5.7; Phase 6 = Ausbaustufe W1).
    ///
    /// Liest ausschließlich Tab_ProjektWerte, Tab_Ergebnis*, energy_* und
    /// Tab_ProjektWirtschaftlichkeit; schreibt Tab_ErgebnisWirtschaftlichkeit —
    /// keine UI-Abhängigkeit. Der UI-Reiter (Form_Wirtschaftlichkeit) und der
    /// Berichts-Baustein lesen dieselben persistierten Ergebnisse.
    ///
    /// Zahlungsgerüst W1 je Projekt und Szenario (Worst/Erwartet/Best):
    ///  - I₀ = Σ Tab_ProjektWerte Kategorie 1 (Szenariospalten Best/WorstCase;
    ///    0/leer → Erwartungswert), Nutzungsdauer analog → Ersatz + Restwert.
    ///  - Betriebskosten p. a. = Σ Kategorie 2 (Szenariowert).
    ///  - Energiekosten p. a. aus dem KostenEmissionRechner (Preise der Kosten-
    ///    maske; Entscheidung 11.08.2026 — keine Doppelpflege), alle Szenarien
    ///    identisch (Preisszenarien folgen mit W2).
    ///  - Erlöse = PV-Überschuss × Einspeisevergütung (Parameter).
    /// Referenz = Stammprojekt: KapitalwertDiff/Annuität/Amortisation der Variante
    /// entstehen aus der Differenz-Zahlungsreihe Variante − Stamm.
    /// </summary>
    public class WirtschaftlichkeitCtrl : IWirtschaftlichkeitProvider
    {
        // Caches EINES Berechne-Laufs (szenariounabhängige DB-Werte; Review Phase 9).
        private List<KeyValuePair<int, double>> _staffelCache;
        private readonly Dictionary<int, double> _pelCache = new Dictionary<int, double>();
        private readonly Dictionary<int, bool> _oelCache = new Dictionary<int, bool>();
        private readonly Dictionary<int, ReferenzkesselInfo> _refKesselCache =
            new Dictionary<int, ReferenzkesselInfo>();   // Review 11: LadeParameter wird oft gerufen

        /// <summary>
        /// ETAPPE K2: Projekte, deren Einheiten-Konsistenz in diesem Ctrl-Leben bereits
        /// geprüft wurde. Aus demselben Grund wie <see cref="_refKesselCache"/> —
        /// <see cref="LadeParameter"/> wird oft gerufen (Parameterdialog, Reiter,
        /// Verlaufsfenster, Bericht, KI-Leseaktion), der Befund hängt aber allein am
        /// Datenbankstand und ändert sich innerhalb eines Laufs nicht.
        /// </summary>
        private readonly HashSet<int> _einheitenGeprueft = new HashSet<int>();

        /// <summary>Anlagenzeilen der BHKW je Projekt (Nachtrag zu E2: Prüfung je Anlage).</summary>
        private readonly Dictionary<int, List<BhkwAnlage>> _anlagenCache =
            new Dictionary<int, List<BhkwAnlage>>();

        /// <summary>ETAPPE B3 Paket a — dasselbe für die HEIZKESSEL-Anlagenzeilen
        /// (§ 54 EnergieStG trifft auch sie, Entscheidung BF5).</summary>
        private readonly Dictionary<int, List<BhkwAnlage>> _kesselCache =
            new Dictionary<int, List<BhkwAnlage>>();

        /// <summary>
        /// Die beiden Nachschlagewerke des Heizöl-Ausschlusses (Nachtrag 2 zu E2), je
        /// Berechne-Lauf einmal gelesen — sie sind projektunabhängige Katalogtabellen:
        /// <c>Tab_Brennstoff_Stamm.ID → ID_Kategorie</c> und
        /// <c>energy_carrier.id → ID_Brennstoff</c>. <c>null</c> = noch nicht gelesen.
        /// </summary>
        private Dictionary<int, int> _brennstoffKategorie;

        /// <inheritdoc cref="_brennstoffKategorie"/>
        private Dictionary<int, int> _carrierBrennstoff;

        /// <summary>Lesefassade auf Tab_Gesetzesparameter (E1); eine Instanz je Berechne-Lauf.</summary>
        private GesetzKatalog _gesetze;

        public const string TAB_PARAMETER = "Tab_ProjektWirtschaftlichkeit";
        public const string TAB_ERGEBNIS = "Tab_ErgebnisWirtschaftlichkeit";
        public const string TAB_SENS = "Tab_ErgebnisWirtSensitivitaet";
        public const string TAB_TARIF = "Tab_ProjektTarif";
        public const string TAB_MATRIX = "Tab_ErgebnisStromMatrix";

        /// <summary>ETAPPE E2 (L6): Spalte der erreichten elektrischen
        /// Vollbenutzungsstunden in <see cref="TAB_ERGEBNIS"/>. EINE Wahrheit für
        /// Anlage, Schreib- und Leseweg.</summary>
        public const string SPALTE_KWKG_VBH_EL = "KWKGVbhElektrisch";

        /// <summary>
        /// ETAPPE E4: die drei Steuergutschriften des ersten Betrachtungsjahres und die
        /// Herkunft der verwendeten Sätze in <see cref="TAB_ERGEBNIS"/>.
        ///
        /// <para><b>Warum über <c>SpalteSicher</c> und nicht über einen
        /// Migrationsschritt.</b> Dieses Modul führt seine ERGEBNIStabelle seit W1 selbst
        /// und hat sie so schon zwanzigmal additiv nachgerüstet — zuletzt in E2 mit
        /// <see cref="SPALTE_KWKG_VBH_EL"/>. Ein Migrationsschritt dafür wäre der dritte
        /// Mechanismus für EINE Tabelle. Die PARAMETERtabelle geht denselben Weg
        /// zusätzlich über Migrationsschritt 20 — dort verlangt der Auftrag den
        /// Schemastand, und dort ist er auch fachlich richtig: Es sind Eingabedaten des
        /// Anwenders, keine wiederherstellbaren Rechenergebnisse.</para>
        /// </summary>
        public const string SPALTE_ENERGIESTEUER = "EnergiesteuerErloes";

        /// <inheritdoc cref="SPALTE_ENERGIESTEUER"/>
        public const string SPALTE_STROMST_BEFREIUNG = "StromsteuerBefreiung";

        /// <inheritdoc cref="SPALTE_ENERGIESTEUER"/>
        public const string SPALTE_STROMST_ENTLASTUNG = "StromsteuerEntlastung";

        /// <inheritdoc cref="SPALTE_ENERGIESTEUER"/>
        public const string SPALTE_STEUER_HERKUNFT = "SteuerHerkunft";

        /// <summary>
        /// ETAPPE E5: vermiedene Kosten (Arbeit, Leistung, Summe) und der Betrag der
        /// berücksichtigten Aufschläge in <see cref="TAB_ERGEBNIS"/>. Über
        /// <c>SpalteSicher</c> — dieselbe Begründung wie bei
        /// <see cref="SPALTE_ENERGIESTEUER"/>.
        /// </summary>
        public const string SPALTE_VERMIEDEN_ARBEIT = "VermiedenArbeit";

        /// <inheritdoc cref="SPALTE_VERMIEDEN_ARBEIT"/>
        public const string SPALTE_VERMIEDEN_LEISTUNG = "VermiedenLeistung";

        /// <inheritdoc cref="SPALTE_VERMIEDEN_ARBEIT"/>
        public const string SPALTE_VERMIEDEN_GESAMT = "VermiedenGesamt";

        /// <inheritdoc cref="SPALTE_VERMIEDEN_ARBEIT"/>
        public const string SPALTE_AUFSCHLAG_BETRAG = "AufschlagBetrag";

        /// <summary>
        /// ETAPPE E7: Aufschlüsselung des Einspeiseerlöses in PV-Überschuss und
        /// KWK-Einspeisung in <see cref="TAB_ERGEBNIS"/>. Über <c>SpalteSicher</c> —
        /// dieselbe Begründung wie bei <see cref="SPALTE_ENERGIESTEUER"/>. Die Summe der
        /// beiden Spalten ist der bereits vorhandene <c>Einspeiseerloes</c>; sie sind
        /// Zerlegung, keine zusätzliche Zahlung.
        /// </summary>
        public const string SPALTE_EINSPEISUNG_PV = "EinspeiseerloesPV";

        /// <inheritdoc cref="SPALTE_EINSPEISUNG_PV"/>
        public const string SPALTE_EINSPEISUNG_KWK = "EinspeiseerloesKWK";

        /// <summary>
        /// ETAPPE K5 (Konzept § 7.4, L7): der angesetzte Investitionszuschuss in
        /// <see cref="TAB_ERGEBNIS"/> [€], positiv. Über <c>SpalteSicher</c> — dieselbe
        /// Begründung wie bei <see cref="SPALTE_ENERGIESTEUER"/>.
        ///
        /// <para><b>Warum eine eigene Spalte und nicht die Differenz.</b> Ohne sie
        /// stünde in <c>Investition</c> entweder der Bruttobetrag (dann fehlte der
        /// Zuschuss im Ausweis) oder der Nettobetrag (dann wäre die Bezugsgröße der
        /// prozentualen Betriebskosten aus dem Ergebnis nicht mehr rekonstruierbar).
        /// Beide Zahlen werden gebraucht, also stehen beide da.</para>
        /// </summary>
        public const string SPALTE_ZUSCHUSS = "Zuschuss";

        /// <summary>
        /// ETAPPE P6 (PV-Konzept § 6.4): Ausweis des PV-Vergütungsdialogs in
        /// <see cref="TAB_ERGEBNIS"/>. Über <c>SpalteSicher</c> — dieselbe
        /// Begründung wie bei <see cref="SPALTE_ENERGIESTEUER"/>. Gefüllt nur bei
        /// aktivem Dialog (Form leer = Bestandsweg ohne Dialog).
        /// </summary>
        public const string SPALTE_PV_FORM = "PvVerguetungsform";
        /// <inheritdoc cref="SPALTE_PV_FORM"/>
        public const string SPALTE_PV_AW = "PvAnzulegenderWert";
        /// <inheritdoc cref="SPALTE_PV_FORM"/>
        public const string SPALTE_PV_MARKTPRAEMIE = "PvMarktpraemie";
        /// <inheritdoc cref="SPALTE_PV_FORM"/>
        public const string SPALTE_PV_AUSFALL_KWH = "PvVerguetungsausfallKwh";
        /// <inheritdoc cref="SPALTE_PV_FORM"/>
        public const string SPALTE_PV_AUSFALL_EUR = "PvVerguetungsausfall";
        /// <inheritdoc cref="SPALTE_PV_FORM"/>
        public const string SPALTE_PV_51A = "PvKompensation51a";
        /// <inheritdoc cref="SPALTE_PV_FORM"/>
        public const string SPALTE_PV_KAPPUNG_KWH = "PvKappungsverlustKwh";
        /// <inheritdoc cref="SPALTE_PV_FORM"/>
        public const string SPALTE_PV_VERMIEDEN = "PvVermiedenerBezug";

        /// <summary>Fristen des § 6 KWKG 2025 (Konzept Kap. 8.2, Phase 9).</summary>
        public static readonly DateTime KWKG_STICHTAG_ENDE = new DateTime(2026, 12, 31);
        public const int KWKG_REALISIERUNG_JAHRE = 4;
        /// <summary>
        /// Ausschreibungsgrenze des § 8a KWKG / der KWKAusV [kW el] — <b>je Anlage</b>,
        /// nicht je Projektsumme (Nutzerentscheidung 19.08.2026, Nachtrag zu Etappe E2).
        ///
        /// <para><b>Nur noch Rückfallebene.</b> Maßgeblich ist der Katalogschlüssel
        /// <c>KWKG_AUSSCHREIBUNG_GRENZE_KW</c> (<see cref="GesetzKatalog"/>, Etappe E1).
        /// Eine Bestandsdatenbank, deren Katalog vor diesem Nachtrag eingesät wurde,
        /// kennt den Schlüssel noch nicht — dann gilt dieser Wert.</para>
        /// </summary>
        public const double KWKG_MAX_LEISTUNG_KW = 500;

        /// <summary>
        /// Kategorie „Öl" des Brennstoffkatalogs — <c>Tab_BrennstoffKategorien.ID</c> = 2,
        /// die Kategorie der neun Heizöl-Zeilen in <c>Tab_Brennstoff_Stamm</c> (Heizöl S/M/L/EL,
        /// EL schwefelarm, Bio 5/10/15/20).
        ///
        /// <para><b>Warum diese Kategorie und nicht <c>pricing_model</c>.</b> Der Code kennt mit
        /// <c>energy_carrier.pricing_model = 'LIQUID_FUEL'</c> ein zweites, gröberes Merkmal für
        /// „flüssig". Es umfasst neben der Kategorie 2 auch die Kategorie 8 <b>Rapsöl</b> — ein
        /// biogener Brennstoff, für den der Ausschluss fossiler flüssiger Brennstoffe gerade nicht
        /// gilt. Maßgeblich ist deshalb die Kategorie; sie ist zugleich das Merkmal, das der
        /// Ausschluss schon vor diesem Nachtrag geprüft hat (siehe <see cref="BhkwMitHeizoel"/>).</para>
        ///
        /// <para><b>Persistenzwert</b> im Sinne der Drei-Schichten-Regel: ein in SQL verglichener
        /// Katalogschlüssel, eingefroren. Er steht nicht in <c>DbWerte</c>, weil dort ausschließlich
        /// die deutschen Zeichen<i>ketten</i> der Datenbank gesammelt sind.</para>
        /// </summary>
        public const int BRENNSTOFF_KATEGORIE_OEL = 2;

        // Feste Ausschläge der Sensitivitätsanalyse (W2; im Bericht ausgewiesen).
        public const double SENS_DELTA_ZINS = 1.0;      // ± Prozentpunkte
        public const double SENS_DELTA_PREIS = 1.0;     // ± Prozentpunkte Energiepreissteigerung
        public const double SENS_DELTA_INVEST = 10.0;   // ± % Investition der Variante
        public const double SENS_DELTA_ENERGIE = 10.0;  // ± % Energiekosten der Variante

        // ------------------------------------------------------------- Tabellen

        /// <summary>Legt Parameter- und Ergebnistabelle an, falls sie fehlen
        /// (Muster BerichtCtrl.StelleKonfigTabelleSicher).</summary>
        /// <remarks>
        /// ARBEITSPAKET S4b: eigene Verbindung -> Zugriffsschicht (still, damit die
        /// leeren <c>catch</c>-Zweige weiter halten, was sie zusagen); Schemaproben statt
        /// <c>GetOleDbSchemaTable</c> (S4c vorgezogen); SQLite-DDL statt Access-DDL
        /// (S4d vorgezogen).
        ///
        /// <para>Die Spaltenlisten sind unveraendert - uebersetzt sind nur die TYPEN
        /// (LONG->INTEGER, DOUBLE->REAL, TEXT(n)/LONGTEXT->TEXT, YESNO->INTEGER 0/1,
        /// DATETIME->TEXT) und die beiden Access-Inline-Nebenbedingungen: SQLite kennt
        /// kein <c>CONSTRAINT … UNIQUE</c> in der Spaltenzeile, der eindeutige Index auf
        /// ID_Projekt wird deshalb - wie im Grundschema - getrennt angelegt.</para>
        /// </remarks>
        public void StelleTabellenSicher()
        {
            try
            {
                // Der Block hielt bis S4b die eigene OleDbConnection; er bleibt als
                // Klammer stehen, damit der Diff auf den Umbau beschraenkt bleibt.
                {
                    // Jeder CREATE einzeln abgesichert: ein Fehlschlag (z. B. reserviertes
                    // Wort) darf weder die anderen Tabellen noch die Spalten-Nachrüstung
                    // darunter verhindern (Review-Befund Phase 7).
                    try
                    {
                    if (!TabelleVorhanden(TAB_PARAMETER))
                    {
                        Ddl("CREATE TABLE IF NOT EXISTS [" + TAB_PARAMETER + "] (" +
                                  "\"ID\" INTEGER PRIMARY KEY, " +
                                  "\"ID_Projekt\" INTEGER, " +
                                  "\"Zinssatz\" REAL, " +
                                  "\"Betrachtungszeitraum\" INTEGER, " +
                                  "\"Preissteigerung_Energie\" REAL, " +
                                  "\"Preissteigerung_Betrieb\" REAL, " +
                                  "\"Einspeiseverguetung\" REAL, " +
                                  "\"CO2_Preis\" REAL, " +
                                  "\"KWKG_Bonus\" REAL, " +
                                  "\"KWKG_Vbh_Jahresdeckel\" REAL, " +
                                  "\"KWKG_Vbh_Kontingent\" REAL, " +
                                  // ETAPPE K6 (HF6/M-D): die vier KWKG-Projektangaben auch im
                                  // CREATE — sonst hätte eine frisch angelegte Tabelle sie erst
                                  // nach dem SpalteSicher-Nachzug weiter unten.
                                  "\"KWKG_Tatbestand\" TEXT CHECK (length(\"KWKG_Tatbestand\") <= 30), " +
                                  "\"KWKG_Anlagenart\" TEXT CHECK (length(\"KWKG_Anlagenart\") <= 20), " +
                                  "\"KWKG_Kostenanteil\" REAL, " +
                                  "\"KWKG_Pauschalmodus\" INTEGER NOT NULL DEFAULT 0 CHECK (\"KWKG_Pauschalmodus\" IN (0,1)), " +
                                  "\"GeaendertAm\" TEXT)");
                        Ddl("CREATE UNIQUE INDEX IF NOT EXISTS \"UQ_ProjWirtProj\" " +
                            "ON [" + TAB_PARAMETER + "] (\"ID_Projekt\")");
                    }
                    }
                    catch { }
                    try
                    {
                    if (!TabelleVorhanden(TAB_ERGEBNIS))
                        Ddl("CREATE TABLE IF NOT EXISTS [" + TAB_ERGEBNIS + "] (" +
                                  "\"ID\" INTEGER PRIMARY KEY, " +
                                  "\"ID_Projekt\" INTEGER, " +
                                  "\"ID_Ergebnis\" INTEGER, " +          // FK auf Tab_Ergebnis.ID (Simulationslauf)
                                  "\"Szenario\" TEXT CHECK (length(\"Szenario\") <= 20), " +
                                  "\"IstStamm\" INTEGER NOT NULL DEFAULT 0 CHECK (\"IstStamm\" IN (0,1)), " +
                                  "\"Anzeige\" TEXT CHECK (length(\"Anzeige\") <= 255), " +
                                  "\"Zeitstempel\" TEXT, " +
                                  "\"Zinssatz\" REAL, " +
                                  "\"Betrachtungszeitraum\" INTEGER, " +
                                  "\"Preissteigerung_Energie\" REAL, " +
                                  "\"Preissteigerung_Betrieb\" REAL, " +
                                  "\"Einspeiseverguetung\" REAL, " +
                                  "\"Investition\" REAL, " +
                                  "\"Betriebskosten\" REAL, " +
                                  "\"Energiekosten\" REAL, " +
                                  "\"Einspeiseerloes\" REAL, " +
                                  "\"BarwertAusgaben\" REAL, " +
                                  "\"BarwertEinnahmen\" REAL, " +
                                  "\"Restwert\" REAL, " +
                                  "\"Kapitalwert\" REAL, " +
                                  "\"KapitalwertDiff\" REAL, " +
                                  "\"AnnuitaetKW\" REAL, " +
                                  "\"AmortisationJahre\" REAL, " +
                                  "\"Gestehungskosten\" REAL, " +
                                  "\"IRR\" REAL, " +
                                  "\"CO2Abgabe\" REAL, " +
                                  "\"KWKGErloes\" REAL, " +
                                  "\"Fehlgrund\" TEXT)");
                    }
                    catch { }
                    try
                    {
                    if (!TabelleVorhanden(TAB_SENS))
                        Ddl("CREATE TABLE IF NOT EXISTS [" + TAB_SENS + "] (" +
                                  "\"ID\" INTEGER PRIMARY KEY, " +
                                  "\"ID_Projekt\" INTEGER, " +
                                  "\"Parameter\" TEXT CHECK (length(\"Parameter\") <= 60), " +
                                  "\"KwMinus\" REAL, " +
                                  "\"KwBasis\" REAL, " +
                                  "\"KwPlus\" REAL, " +
                                  "\"Zeitstempel\" TEXT)");
                    }
                    catch { }
                    try
                    {
                    if (!TabelleVorhanden(TAB_TARIF))
                    {
                        Ddl("CREATE TABLE IF NOT EXISTS [" + TAB_TARIF + "] (" +
                                  "\"ID\" INTEGER PRIMARY KEY, " +
                                  "\"ID_Projekt\" INTEGER, " +
                                  "\"Aktiv\" INTEGER NOT NULL DEFAULT 0 CHECK (\"Aktiv\" IN (0,1)), " +
                                  "\"Winter_Von\" INTEGER, " +
                                  "\"Winter_Bis\" INTEGER, " +
                                  "\"HT_Von\" INTEGER, " +
                                  "\"HT_Bis\" INTEGER, " +
                                  "\"Bezug_W_HT\" REAL, \"Bezug_W_NT\" REAL, \"Bezug_S_HT\" REAL, \"Bezug_S_NT\" REAL, " +
                                  "\"Einsp_W_HT\" REAL, \"Einsp_W_NT\" REAL, \"Einsp_S_HT\" REAL, \"Einsp_S_NT\" REAL, " +
                                  "\"Staffel_Grenze\" REAL, \"Staffel_Preis1\" REAL, \"Staffel_Preis2\" REAL, " +
                                  "\"GeaendertAm\" TEXT)");
                        Ddl("CREATE UNIQUE INDEX IF NOT EXISTS \"UQ_ProjTarifProj\" " +
                            "ON [" + TAB_TARIF + "] (\"ID_Projekt\")");
                    }
                    }
                    catch { }
                    try
                    {
                    if (!TabelleVorhanden(TAB_MATRIX))
                        Ddl("CREATE TABLE IF NOT EXISTS [" + TAB_MATRIX + "] (" +
                                  "\"ID\" INTEGER PRIMARY KEY, " +
                                  "\"ID_Projekt\" INTEGER, " +
                                  "\"Zone\" TEXT CHECK (length(\"Zone\") <= 20), " +
                                  "\"BezugMWh\" REAL, " +
                                  "\"EinspPvMWh\" REAL, " +
                                  "\"KwkEigenMWh\" REAL, " +
                                  "\"KwkEinspMWh\" REAL, " +
                                  "\"MaxBezugKW\" REAL, " +
                                  "\"Zeitstempel\" TEXT)");
                    }
                    catch { }

                    // Ältere Tabellenstände additiv nachrüsten (Muster
                    // ErgebnisCtrl.StelleModulSpaltenSicher) — CREATE erfasst nur Neuanlagen.
                    SpalteSicher(TAB_ERGEBNIS, "IstStamm", "YESNO");
                    SpalteSicher(TAB_ERGEBNIS, "Anzeige", "TEXT(255)");
                    SpalteSicher(TAB_ERGEBNIS, "IRR", "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, "CO2Abgabe", "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, "KWKGErloes", "DOUBLE");
                    SpalteSicher(TAB_PARAMETER, "CO2_Preis", "DOUBLE");
                    SpalteSicher(TAB_PARAMETER, "KWKG_Bonus", "DOUBLE");
                    SpalteSicher(TAB_PARAMETER, "KWKG_Vbh_Jahresdeckel", "DOUBLE");
                    SpalteSicher(TAB_PARAMETER, "KWKG_Vbh_Kontingent", "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, "StromkostenTarif", "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, "HinweisText", "LONGTEXT");
                    // ETAPPE E2 (L6): die Bemessungsgrundlage der KWKG-Deckelung wird
                    // mitgeschrieben, damit ein gespeichertes Ergebnis nachvollziehbar
                    // bleibt. Additiv über denselben Weg wie die Spalten darüber — dieses
                    // Modul führt seine Tabellen seit jeher selbst (bekannte doppelte
                    // Wahrheit gegenüber SchemaMigration, W4-Umsetzungsstand Abschnitt 6);
                    // ein Migrationsschritt dafür wäre der dritte Mechanismus.
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_KWKG_VBH_EL, "DOUBLE");
                    SpalteSicher(TAB_PARAMETER, "KWKG_Bonus_Einspeisung", "DOUBLE");
                    SpalteSicher(TAB_PARAMETER, "ID_Kraftwerkspark", "LONG");
                    SpalteSicher(TAB_PARAMETER, "RefKessel_Wirkungsgrad", "DOUBLE");
                    SpalteSicher(TAB_PARAMETER, "RefKessel_ID_Brennstoff", "LONG");
                    bool phase9Neu = SpalteSicher(TAB_PARAMETER, "KWKG_Stichtag", "DATETIME");
                    SpalteSicher(TAB_PARAMETER, "KWKG_Inbetriebnahme", "DATETIME");
                    SpalteSicher(TAB_PARAMETER, "KWKG_Abschlag_Negativ", "DOUBLE");

                    // Einmalige Migration (Phase 9): der bisherige Vorgabewert 3500 des
                    // Deckels bedeutete „KWKG-2020-Standard" — in der neuen Override-
                    // Semantik (0 = degressive Staffel) würde er die Staffel dauerhaft
                    // aushebeln. Beim ersten Phase-9-Start auf 0 umstellen.
                    if (phase9Neu)
                        try { Ddl("UPDATE " + TAB_PARAMETER +
                                        " SET KWKG_Vbh_Jahresdeckel = 0 WHERE KWKG_Vbh_Jahresdeckel = 3500"); }
                        catch { }

                    // ETAPPE E4 — die drei Steuergutschriften und die Herkunft ihrer
                    // Sätze im ERGEBNIS. Additiv über denselben Weg wie die Spalten
                    // darüber (Begründung bei SPALTE_ENERGIESTEUER).
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_ENERGIESTEUER, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_STROMST_BEFREIUNG, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_STROMST_ENTLASTUNG, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_STEUER_HERKUNFT, "LONGTEXT");

                    // ETAPPE E4 — die sechs Projektangaben der Steuerprüfung. Sie
                    // entstehen regulär über Migrationsschritt 20; das hier ist die
                    // tolerante VORSORGE unmittelbar vor dem Zugriff, damit eine nie
                    // migrierte Datenbank nicht an einer fehlenden Spalte scheitert —
                    // dasselbe Muster wie KostenPositionCtrl.StelleSpaltenSicher (E3).
                    // Die WERTE-Vorbelegung bleibt allein bei Schritt 20b: Die Leseseite
                    // behandelt leer/NULL ohnehin wie „keine Gutschrift", und ein zweiter
                    // schreibender Weg auf Anwenderdaten wäre eine Wahrheit zu viel.
                    SpalteSicher(TAB_PARAMETER, SchemaKatalog.SPALTE_PW_UNTERNEHMENSART, "TEXT(24)");
                    SpalteSicher(TAB_PARAMETER, SchemaKatalog.SPALTE_PW_RAEUMLICH, "YESNO");
                    SpalteSicher(TAB_PARAMETER, SchemaKatalog.SPALTE_PW_HOCHEFFIZIENZ, "YESNO");
                    SpalteSicher(TAB_PARAMETER, SchemaKatalog.SPALTE_PW_NUTZUNGSGRAD, "DOUBLE");
                    SpalteSicher(TAB_PARAMETER, SchemaKatalog.SPALTE_PW_ENERGIESTEUER_WAHL, "TEXT(20)");
                    SpalteSicher(TAB_PARAMETER, SchemaKatalog.SPALTE_PW_AUFTEILUNG, "TEXT(30)");

                    // ETAPPE K6 (HF6/M-D) — die vier KWKG-Projektangaben. Regulär legt sie
                    // Migrationsschritt 28 an; das hier ist die tolerante VORSORGE
                    // unmittelbar vor dem Zugriff (doppelte Schema-Wahrheit dieses Moduls,
                    // Konzept § 9 Punkt 2). WERTE werden auch hier nicht vorbelegt: leer
                    // heißt „nicht angegeben", und genau das hält den Bestand unverändert.
                    SpalteSicher(TAB_PARAMETER, SchemaKatalog.SPALTE_PW_KWKG_TATBESTAND, "TEXT(30)");
                    SpalteSicher(TAB_PARAMETER, SchemaKatalog.SPALTE_PW_KWKG_ANLAGENART, "TEXT(20)");
                    SpalteSicher(TAB_PARAMETER, SchemaKatalog.SPALTE_PW_KWKG_KOSTENANTEIL, "DOUBLE");
                    SpalteSicher(TAB_PARAMETER, SchemaKatalog.SPALTE_PW_KWKG_PAUSCHALMODUS, "YESNO");

                    // ETAPPE E5 — der Bedarf OHNE Anlage je Zone: die Bezugsgröße der
                    // Differenzmethode. Sie fehlte im Modell vollständig.
                    SpalteSicher(TAB_MATRIX, "BedarfMWh", "DOUBLE");

                    // ETAPPE E5 — vermiedene Kosten und Aufschlagsbetrag im ERGEBNIS.
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_VERMIEDEN_ARBEIT, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_VERMIEDEN_LEISTUNG, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_VERMIEDEN_GESAMT, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_AUFSCHLAG_BETRAG, "DOUBLE");

                    // ETAPPE E7 — Zerlegung des Einspeiseerlöses. Additiv wie oben; die
                    // Summe der beiden Spalten ist der bereits vorhandene Gesamtbetrag.
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_EINSPEISUNG_PV, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_EINSPEISUNG_KWK, "DOUBLE");

                    // ETAPPE K5 — der angesetzte Investitionszuschuss. Additiv über
                    // denselben Weg; die doppelte Schema-Wahrheit dieses Moduls (§ 9.2
                    // des Konzepts) wird damit nicht um einen dritten Mechanismus
                    // erweitert: Ergebnisspalten führt der Controller, Eingabespalten
                    // der Migrationskatalog.
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_ZUSCHUSS, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_PV_FORM, "TEXT(50)");   // P6
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_PV_AW, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_PV_MARKTPRAEMIE, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_PV_AUSFALL_KWH, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_PV_AUSFALL_EUR, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_PV_51A, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_PV_KAPPUNG_KWH, "DOUBLE");
                    SpalteSicher(TAB_ERGEBNIS, SPALTE_PV_VERMIEDEN, "DOUBLE");

                    // ETAPPE E5 — die Spalten des Tarif-Rollenmodells und die zwei
                    // Projektangaben. Sie entstehen regulär über Migrationsschritt 21;
                    // das hier ist die tolerante VORSORGE unmittelbar vor dem Zugriff,
                    // damit eine nie migrierte Datenbank nicht an einer fehlenden Spalte
                    // scheitert — dasselbe Muster wie bei den E4-Spalten darüber. Die
                    // WERTE-Vorbelegung bleibt allein bei Schritt 21b: Die Leseseite
                    // behandelt leer/NULL ohnehin wie ZONEN.
                    foreach (SchemaSpalte s in SchemaKatalog.Schritt21_Tarifmodell)
                        SpalteSicher(s.Tabelle, s.Name, s.TypDefinition);

                    // ETAPPE E6 — die acht KWKG-Spalten JE ANLAGE an Tab_Energieanlagen.
                    // Sie entstehen regulär über Migrationsschritt 22; das hier ist die
                    // tolerante VORSORGE unmittelbar vor dem Zugriff — dasselbe Muster
                    // wie bei E4 und E5. Eine WERTE-Vorbelegung gibt es weder hier noch
                    // in Schritt 22: NULL heißt „kein eigener Wert", und dann gilt der
                    // Projektwert.
                    foreach (SchemaSpalte s in SchemaKatalog.Schritt22_KwkgJeAnlage)
                        SpalteSicher(s.Tabelle, s.Name, s.TypDefinition);

                    // ETAPPE B3 Paket a — die drei Angaben JE ANLAGE (Steuerwahl,
                    // Aufteilungsmethode, Hilfsenergieanteil) an Tab_Energieanlagen. Sie
                    // entstehen regulär über Migrationsschritt 61a; das hier ist die
                    // tolerante VORSORGE unmittelbar vor dem Zugriff — dasselbe Muster
                    // wie bei E4, E5 und E6. Eine WERTE-Vorbelegung gibt es weder hier
                    // noch in Schritt 61: NULL heißt „kein eigener Wert", und dann gilt
                    // der Projektwert bzw. „keine Hilfsenergie".
                    foreach (SchemaSpalte s in SchemaKatalog.Schritt61_SteuerJeAnlage)
                        SpalteSicher(s.Tabelle, s.Name, s.TypDefinition);

                    // LEITENTSCHEIDUNGEN L12/L13 — die vier Bilanzierungsangaben. Sie
                    // entstehen regulär über Migrationsschritt 23; das hier ist die
                    // tolerante VORSORGE unmittelbar vor dem Zugriff — dasselbe Muster
                    // wie bei E4, E5 und E6. Die WERTE-Vorbelegung bleibt allein bei
                    // Schritt 23b; die Leseseite behandelt leer/NULL ohnehin wie den
                    // Vorgabewert, und ein leeres Bilanzjahr wie 2026.
                    foreach (SchemaSpalte s in SchemaKatalog.Schritt23_Bilanzkonvention)
                        SpalteSicher(s.Tabelle, s.Name, s.TypDefinition);
                }
            }
            catch { /* ohne Tabellen laufen Laden/Speichern in ihre eigenen Fänge */ }

            // Katalog gesetzlicher Parameter (Etappe E1, Leitentscheidung L2). Eigene
            // Verbindung, eigener Fang: Ein Fehlschlag darf die Tabellen oben nicht
            // gefährden, und umgekehrt.
            GesetzKatalog.StelleKatalogSicher();
        }

        /// <summary>Fügt eine fehlende Spalte per ALTER TABLE hinzu (still, additiv).
        /// Liefert true, wenn die Spalte JETZT neu angelegt wurde (Migrations-Anker).
        ///
        /// ARBEITSPAKET S4b: Schemaprobe über <see cref="StilleDb.SpaltenNamen"/> statt
        /// <c>GetOleDbSchemaTable(Columns, …)</c>; die Access-Typangabe wird beim
        /// Verbrauch nach SQLite übersetzt.</summary>
        private static bool SpalteSicher(string tabelle, string spalte, string typ)
        {
            try
            {
                HashSet<string> vorhanden = StilleDb.SpaltenNamen(tabelle);

                // Wie bisher: Nur ein NACHWEISLICHES Fehlen loest das ALTER aus. null
                // hiess frueher "Schema nicht lesbar / Tabelle fehlt" - dann meldete
                // GetOleDbSchemaTable keine Zeile und das ALTER lief in seinen catch.
                if (vorhanden != null && vorhanden.Contains(spalte)) return false;

                Ddl(StilleDb.AlterTableAddColumn(tabelle, spalte, typ));
                return true;
            }
            catch { return false; }
        }

        private static bool TabelleVorhanden(string name)
        {
            return StilleDb.TabelleVorhanden(name);
        }

        /// <summary>
        /// Eine DDL-/Verwaltungsanweisung, still. ARBEITSPAKET S4b, Verhaltenstreue:
        /// Der frühere Weg WARF bei einem Fehlschlag, und genau darauf bauen die
        /// umschliessenden <c>try/catch</c>-Klammern (ein misslungenes CREATE darf den
        /// zugehörigen Index nicht mehr anlegen, ein misslungenes ALTER meldet "nicht
        /// neu angelegt"). <see cref="StilleDb"/> wirft nicht, also wird hier von Hand
        /// geworfen. Nach aussen bleibt alles gleich - gefangen wurde die Ausnahme
        /// schon bisher wortlos.
        /// </summary>
        private static void Ddl(string sql)
        {
            if (StilleDb.NonQuery(sql) < 0)
                throw new InvalidOperationException("Anweisung fehlgeschlagen: " + sql);
        }

        // ------------------------------------------------------------- Parameter

        public WirtschaftlichkeitParameter LadeParameter(int idStamm)
        {
            StelleTabellenSicher();
            var p = new WirtschaftlichkeitParameter { IdStamm = idStamm };
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_PARAMETER + " WHERE ID_Projekt = ?",
                    new DbParam("@p", idStamm));
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    p.Zinssatz = D(r, "Zinssatz") ?? p.Zinssatz;
                    p.Betrachtungszeitraum = (int)(D(r, "Betrachtungszeitraum") ?? p.Betrachtungszeitraum);
                    p.PreissteigerungEnergie = D(r, "Preissteigerung_Energie") ?? 0;
                    p.PreissteigerungBetrieb = D(r, "Preissteigerung_Betrieb") ?? 0;
                    p.Einspeiseverguetung = D(r, "Einspeiseverguetung") ?? 0;
                    p.CO2Preis = D(r, "CO2_Preis") ?? 0;
                    p.KwkgBonus = D(r, "KWKG_Bonus") ?? 0;
                    p.KwkgVbhJahresdeckel = D(r, "KWKG_Vbh_Jahresdeckel") ?? p.KwkgVbhJahresdeckel;
                    p.KwkgVbhKontingent = D(r, "KWKG_Vbh_Kontingent") ?? p.KwkgVbhKontingent;
                    p.KwkgBonusEinspeisung = D(r, "KWKG_Bonus_Einspeisung") ?? 0;
                    p.IdKraftwerkspark = (int)(D(r, "ID_Kraftwerkspark") ?? 0);
                    p.RefKesselWirkungsgrad = D(r, "RefKessel_Wirkungsgrad") ?? p.RefKesselWirkungsgrad;
                    p.RefKesselIdBrennstoff = (int)(D(r, "RefKessel_ID_Brennstoff") ?? p.RefKesselIdBrennstoff);
                    if (r.Table.Columns.Contains("KWKG_Stichtag") && r["KWKG_Stichtag"] != DBNull.Value)
                        p.KwkgStichtag = Convert.ToDateTime(r["KWKG_Stichtag"]);
                    if (r.Table.Columns.Contains("KWKG_Inbetriebnahme") && r["KWKG_Inbetriebnahme"] != DBNull.Value)
                        p.KwkgInbetriebnahme = Convert.ToDateTime(r["KWKG_Inbetriebnahme"]);
                    p.KwkgAbschlagNegativ = D(r, "KWKG_Abschlag_Negativ") ?? 0;

                    // ETAPPE K6 — KWKG-Tatbestand, Anlagenart, Kostenanteil, Pauschale.
                    // Ein LEERER Steuerwert heißt hier „nicht angegeben" und ist NICHT
                    // gleichbedeutend mit KEINER bzw. NEUANLAGE: Ohne Erfassung rechnet
                    // die Anwendung wie bisher und weist das aus (Begründung an
                    // WirtschaftlichkeitParameter.KwkgTatbestand).
                    p.KwkgTatbestand = Text(r, SchemaKatalog.SPALTE_PW_KWKG_TATBESTAND);
                    p.KwkgAnlagenart = Text(r, SchemaKatalog.SPALTE_PW_KWKG_ANLAGENART);
                    p.KwkgKostenanteil = D(r, SchemaKatalog.SPALTE_PW_KWKG_KOSTENANTEIL) ?? 0;
                    p.KwkgPauschalmodus = B(r, SchemaKatalog.SPALTE_PW_KWKG_PAUSCHALMODUS);

                    // ETAPPE E4 — Steuerangaben. Ein LEERER Steuerwert bedeutet genau
                    // dasselbe wie der Vorgabewert: keine Gutschrift. Eine nicht
                    // migrierte Datenbank verhält sich dadurch wie eine migrierte.
                    string art = Text(r, SchemaKatalog.SPALTE_PW_UNTERNEHMENSART);
                    if (art.Length > 0) p.Unternehmensart = art;
                    p.RaeumlicherZusammenhang = B(r, SchemaKatalog.SPALTE_PW_RAEUMLICH);
                    p.HocheffizienzNachweis = B(r, SchemaKatalog.SPALTE_PW_HOCHEFFIZIENZ);
                    p.Jahresnutzungsgrad = D(r, SchemaKatalog.SPALTE_PW_NUTZUNGSGRAD);
                    string wahl = Text(r, SchemaKatalog.SPALTE_PW_ENERGIESTEUER_WAHL);
                    if (wahl.Length > 0) p.EnergiesteuerWahl = wahl;
                    string auf = Text(r, SchemaKatalog.SPALTE_PW_AUFTEILUNG);
                    if (auf.Length > 0) p.AufteilungMethode = auf;

                    // ETAPPE E5 — Aufschlagsschalter und KWK-Einspeisevergütung. Beide
                    // sind ohne ausdrückliche Angabe wirkungslos: YESNO liegt bei jeder
                    // Bestandszeile auf False, DOUBLE bleibt NULL.
                    p.AufschlaegeAnwenden = B(r, SchemaKatalog.SPALTE_PW_AUFSCHLAEGE);
                    p.EinspeiseverguetungKWK = D(r, SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK);

                    // LEITENTSCHEIDUNGEN L12/L13 — Bilanzierungsangaben. Ein LEERER
                    // Steuerwert bedeutet genau dasselbe wie der Vorgabewert; ein
                    // leeres Bilanzjahr heißt „Rechtsstand bis 31.12.2026". Eine nicht
                    // migrierte Datenbank verhält sich dadurch wie eine migrierte.
                    p.BilanzJahr = (int)(D(r, SchemaKatalog.SPALTE_PW_BILANZJAHR) ?? 0);
                    string meth = Text(r, SchemaKatalog.SPALTE_PW_EMISSIONSMETHODE);
                    if (meth.Length > 0) p.EmissionsMethode = meth;
                    string bkon = Text(r, SchemaKatalog.SPALTE_PW_BIOMASSE_KONVENTION);
                    if (bkon.Length > 0) p.BiomasseKonvention = bkon;
                    string bnw = Text(r, SchemaKatalog.SPALTE_PW_BIOMASSE_NACHWEIS);
                    // Nur der ausdrückliche Wert NACHWEIS_NEIN entzieht den Nachweis —
                    // leer, NULL und jeder unbekannte Bestandswert bedeuten JA und damit
                    // die unveränderte BEHG-Abgabe.
                    p.NachhaltigkeitsnachweisBiomasse =
                        !string.Equals(bnw, DbWerte.BIOMASSE_NACHWEIS_NEIN, StringComparison.Ordinal);

                    if (r["GeaendertAm"] != DBNull.Value) p.GeaendertAm = Convert.ToDateTime(r["GeaendertAm"]);
                }
            }
            catch { }
            if (p.Betrachtungszeitraum <= 0) p.Betrachtungszeitraum = 20;

            // Referenzkessel seit Phase 11 aus der DB (Heizkessel des Stammprojekts) —
            // nicht mehr im Dialog gepflegt; die gespeicherten Werte bleiben Fallback,
            // falls das Stammprojekt (noch) keinen Kessel hat.
            ReferenzkesselInfo rk = LiesReferenzkessel(idStamm);
            if (rk != null && rk.Gefunden)
            {
                p.RefKesselWirkungsgrad = rk.WirkungsgradProzent;
                if (rk.IdBrennstoff > 0)             // ohne Träger-FK: nur η übernehmen
                    p.RefKesselIdBrennstoff = rk.IdBrennstoff;
            }

            MeldeEinheitenBefunde(idStamm);
            return p;
        }

        /// <summary>
        /// ETAPPE K2 (Konzept Kosten/Energieträger, HF2 / L2): die Befunde des
        /// Einheitenprüfers als PROTOKOLLWARNUNG in den Lauf geben.
        ///
        /// <para><b>Nicht blockierend, und das ist die ganze Absicht.</b> Keine
        /// MessageBox, kein Abbruch, kein veränderter Rückgabewert — die Rechnung läuft
        /// unverändert weiter. Ein Träger, der kWh nicht erreicht, ist ein Mangel der
        /// STAMMDATEN; ihn mitten im Wirtschaftlichkeitslauf zur Fehlerlage zu erklären
        /// hieße, ein gespeichertes Projekt unbenutzbar zu machen, das gestern noch
        /// gerechnet hat. Die blockierende Prüfung gehört an die Stelle, an der die
        /// Daten ENTSTEHEN — beim Speichern in <c>ucFuelSettings</c>, Etappe K3.</para>
        ///
        /// <para><b>Warum <c>SimulationProtokoll</c>.</b> Das ist der EINE nicht
        /// blockierende Meldekanal dieser Anwendung: prozessweit erreichbar, nie
        /// <c>null</c>, im unbeaufsichtigten Lauf dialogfrei, und ausdrücklich
        /// ergebnisneutral („Diese Klasse rechnet nichts. Sie sammelt Text."). Auch
        /// <c>DataRepository</c> meldet dorthin, ist also kein Simulationsmonopol. Die
        /// Stufe <b>Warnung</b> trifft die Lage nach der Definition der Klasse selbst:
        /// „gerechnet wurde, aber mit einer Ersatzannahme" — die Mengenrechnung greift
        /// bei fehlender Regelkette unmittelbar auf <c>eff_hi</c> zurück.</para>
        ///
        /// <para><b>Kein Einfluss auf die Referenzläufe.</b> <c>Referenzlauf</c> zählt
        /// Warnungen über das Konsolen-Token „Simulation Warnung:" — und ruft
        /// <see cref="LadeParameter"/> nirgends auf (die Suite rechnet Simulationen,
        /// keine Wirtschaftlichkeit). Diese Meldungen können dort also weder auftauchen
        /// noch eine Zählung verschieben.</para>
        ///
        /// <para><c>WarnungEinmal</c> statt <c>Warnung</c>: <see cref="LadeParameter"/>
        /// wird je Sitzung vielfach gerufen, der Befund ist aber immer derselbe.</para>
        /// </summary>
        private void MeldeEinheitenBefunde(int idStamm)
        {
            if (idStamm <= 0) return;
            if (!_einheitenGeprueft.Add(idStamm)) return;

            try
            {
                List<EinheitenBefund> befunde = EnergieEinheitenPruefung.PruefeProjekt(idStamm);
                if (befunde == null || befunde.Count == 0) return;

                foreach (EinheitenBefund b in befunde)
                    SimulationProtokoll.Aktuell.WarnungEinmal(
                        "K2/EINHEITEN/" + idStamm + "/" + b.CarrierId + "/" + b.Code,
                        "Energieträger-Einheiten (Projekt " + idStamm + "): " + b);
            }
            catch
            {
                // Eine gescheiterte PRÜFUNG darf niemals eine gelingende RECHNUNG
                // verhindern. Der Prüfer fängt selbst schon alles ab; dieser Block ist
                // die zweite Sicherung an der Nahtstelle zum Rechenweg.
            }
        }

        /// <summary>Referenzkessel der getrennten Erzeugung aus dem Stammprojekt
        /// (Phase 11): größter VERBAUTER Kessel; Wirkungsgrad je nach
        /// Brennstoff-Kategorie (Öl → Wirkungsgrad_Öl, sonst _Gas; 0 → der andere).
        /// Kein Kessel/kein brauchbarer Wirkungsgrad → Gefunden = false
        /// (die gespeicherten Parameter-Vorgaben gelten weiter).
        ///
        /// <para><b>NACHTRAG ZU E2 (22.08.2026) — Bezugsmenge wie bei
        /// <see cref="LiesBhkwLeistungKW"/> korrigiert.</b> Bis dahin las die Abfrage
        /// <c>WHERE ID_Projekt = ?</c> direkt auf <c>Tab_Heizkessel</c> — der Tabelle der
        /// PROJEKTKOPIEN, in der auch Kessel stehen, deren Anlagenzeile nie entstand oder
        /// längst gelöscht ist. <c>ORDER BY Ptherm DESC</c> kürte dann ausgerechnet den
        /// größten dieser Altbestände zum Referenzkessel, dessen Bezeichner, Brennstoff
        /// und Wirkungsgrad in die getrennte Erzeugung einflossen (Projekt 1023 führte am
        /// 22.08.2026 16 verwaiste von 18 Kesselzeilen). Maßgeblich ist der Verbund über
        /// <c>Tab_Energieanlagen.ID_Kessel</c> — BEWUSST OHNE Typfilter: Plan- (Typ 10)
        /// wie Referenzliste (Typ 5) führen ihre Kessel absichtlich im Projekt, genau wie
        /// die alte Abfrage beide sah.</para>
        ///
        /// <para><b>Rückfall auf die Gerätezeilen</b>, wenn der Verbund keine Zeile
        /// liefert (Anlagenzeile ohne <c>ID_Kessel</c>, Datenbank ohne Anlagenzeilen) —
        /// dieselbe Begründung wie beim BHKW: Dann ist die Gerätetabelle die einzige
        /// verfügbare Aussage, und seit dem Aufräumlauf (<c>GeraeteWaisen</c>,
        /// Migrationsschritt 34) führt sie ohnehin nur noch Verbautes.</para></summary>
        public ReferenzkesselInfo LiesReferenzkessel(int idStamm)
        {
            var info = new ReferenzkesselInfo();
            if (idStamm <= 0) return info;
            ReferenzkesselInfo cache;
            if (_refKesselCache.TryGetValue(idStamm, out cache)) return cache;   // Review 11
            try
            {
                // 1. Größter Kessel über die ANLAGENZEILEN — dieselbe Menge, die die
                //    Engine rechnet und die Verwaltungsdialoge anzeigen.
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT g.Bezeichner, g.Brennstoff, g.Wirkungsgrad_Gas, g.[Wirkungsgrad_Öl] " +
                    "FROM Tab_Heizkessel AS g INNER JOIN Tab_Energieanlagen AS a ON g.ID = a.ID_Kessel " +
                    "WHERE a.ID_Projekt = ? ORDER BY g.Ptherm DESC, g.ID LIMIT 1",
                    new DbParam("@p", idStamm));

                // 2. Rückfall: die Gerätezeilen (der Weg bis zu diesem Nachtrag).
                if (dt == null || dt.Rows.Count == 0)
                    dt = DataRepository.GetDataTable(
                        "SELECT Bezeichner, Brennstoff, Wirkungsgrad_Gas, [Wirkungsgrad_Öl] " +
                        "FROM Tab_Heizkessel WHERE ID_Projekt = ? ORDER BY Ptherm DESC, ID LIMIT 1",
                        new DbParam("@p", idStamm));

                if (dt == null || dt.Rows.Count == 0) { _refKesselCache[idStamm] = info; return info; }
                DataRow r = dt.Rows[0];

                double wGas = r["Wirkungsgrad_Gas"] != DBNull.Value ? Convert.ToDouble(r["Wirkungsgrad_Gas"]) : 0;
                double wOel = r["Wirkungsgrad_Öl"] != DBNull.Value ? Convert.ToDouble(r["Wirkungsgrad_Öl"]) : 0;
                int idBrennstoff = r["Brennstoff"] != DBNull.Value ? Convert.ToInt32(r["Brennstoff"]) : 0;

                bool oel = false;
                if (idBrennstoff > 0)
                {
                    DataTable bs = DataRepository.GetDataTable(
                        "SELECT ID_Kategorie, Bezeichner FROM Tab_Brennstoff_Stamm WHERE ID = ?",
                        new DbParam("@b", idBrennstoff));
                    if (bs == null || bs.Rows.Count == 0)
                    {
                        // FK zeigt ins Leere (Träger gelöscht) — kein stiller Gas-Default
                        // (Review 11): gespeicherte Vorgaben gelten weiter.
                        _refKesselCache[idStamm] = info;
                        return info;
                    }
                    oel = bs.Rows[0]["ID_Kategorie"] != DBNull.Value &&
                          Convert.ToInt32(bs.Rows[0]["ID_Kategorie"]) == 2;   // Kategorie 2 = Öl
                    info.BrennstoffName = bs.Rows[0]["Bezeichner"] != DBNull.Value
                        ? bs.Rows[0]["Bezeichner"].ToString() : "";
                }

                double eta = oel ? wOel : wGas;
                if (eta <= 0) eta = oel ? wGas : wOel;   // gepflegt ist nur der andere Wert
                if (eta <= 1.5) eta *= 100.0;            // Faktor-Schreibweise (0,9) → Prozent

                // Plausibilitätsband (Review 11): unsinnige DB-Werte (z. B. 9,5 statt
                // 0,95) dürfen die gepflegte Vorgabe nicht still ersetzen.
                if (eta < 50.0 || eta > 115.0) { _refKesselCache[idStamm] = info; return info; }

                info.Gefunden = true;
                info.Bezeichner = r["Bezeichner"] != DBNull.Value ? r["Bezeichner"].ToString() : "";
                info.WirkungsgradProzent = eta;
                info.IdBrennstoff = idBrennstoff;   // 0 = kein Träger-FK → nur η übernehmen
            }
            catch { }
            _refKesselCache[idStamm] = info;
            return info;
        }

        public bool SpeichereParameter(WirtschaftlichkeitParameter p)
        {
            if (p == null || p.IdStamm <= 0) return false;
            StelleTabellenSicher();
            try
            {
                int rows = DataRepository.ExecuteNonQuery(
                    "UPDATE " + TAB_PARAMETER + " SET Zinssatz = ?, Betrachtungszeitraum = ?, " +
                    "Preissteigerung_Energie = ?, Preissteigerung_Betrieb = ?, " +
                    "Einspeiseverguetung = ?, CO2_Preis = ?, KWKG_Bonus = ?, " +
                    "KWKG_Vbh_Jahresdeckel = ?, KWKG_Vbh_Kontingent = ?, " +
                    "KWKG_Bonus_Einspeisung = ?, ID_Kraftwerkspark = ?, " +
                    "RefKessel_Wirkungsgrad = ?, RefKessel_ID_Brennstoff = ?, " +
                    "KWKG_Stichtag = ?, KWKG_Inbetriebnahme = ?, KWKG_Abschlag_Negativ = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_UNTERNEHMENSART + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_RAEUMLICH + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_HOCHEFFIZIENZ + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_NUTZUNGSGRAD + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_ENERGIESTEUER_WAHL + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_AUFTEILUNG + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_AUFSCHLAEGE + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_BILANZJAHR + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_EMISSIONSMETHODE + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_BIOMASSE_KONVENTION + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_BIOMASSE_NACHWEIS + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_KWKG_TATBESTAND + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_KWKG_ANLAGENART + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_KWKG_KOSTENANTEIL + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_KWKG_PAUSCHALMODUS + "] = ?, " +
                    "GeaendertAm = ? WHERE ID_Projekt = ?",
                    new DbParam("@z", p.Zinssatz),
                    new DbParam("@t", p.Betrachtungszeitraum),
                    new DbParam("@pe", p.PreissteigerungEnergie),
                    new DbParam("@pb", p.PreissteigerungBetrieb),
                    new DbParam("@ev", p.Einspeiseverguetung),
                    new DbParam("@co2", p.CO2Preis),
                    new DbParam("@kwkg", p.KwkgBonus),
                    new DbParam("@vbhj", p.KwkgVbhJahresdeckel),
                    new DbParam("@vbhk", p.KwkgVbhKontingent),
                    new DbParam("@kwkgE", p.KwkgBonusEinspeisung),
                    new DbParam("@park", p.IdKraftwerkspark),
                    new DbParam("@refEta", p.RefKesselWirkungsgrad),
                    new DbParam("@refBs", p.RefKesselIdBrennstoff),
                    new DbParam("@st", DbParamTyp.Date) { Wert = (object)p.KwkgStichtag ?? DBNull.Value },
                    new DbParam("@ibn", DbParamTyp.Date) { Wert = (object)p.KwkgInbetriebnahme ?? DBNull.Value },
                    new DbParam("@neg", p.KwkgAbschlagNegativ),
                    new DbParam("@art", DbParamTyp.VarWChar, 24)
                    { Wert = Steuerwert(p.Unternehmensart, DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE) },
                    new DbParam("@raum", DbParamTyp.Boolean) { Wert = p.RaeumlicherZusammenhang },
                    new DbParam("@heff", DbParamTyp.Boolean) { Wert = p.HocheffizienzNachweis },
                    new DbParam("@ng", DbParamTyp.Double)
                    { Wert = p.Jahresnutzungsgrad.HasValue ? (object)p.Jahresnutzungsgrad.Value : DBNull.Value },
                    new DbParam("@wahl", DbParamTyp.VarWChar, 20)
                    { Wert = Steuerwert(p.EnergiesteuerWahl, DbWerte.ENERGIESTEUER_WAHL_KEINE) },
                    new DbParam("@auf", DbParamTyp.VarWChar, 30)
                    { Wert = Steuerwert(p.AufteilungMethode, DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF) },
                    new DbParam("@aufs", DbParamTyp.Boolean) { Wert = p.AufschlaegeAnwenden },
                    new DbParam("@vkwk", DbParamTyp.Double)
                    { Wert = p.EinspeiseverguetungKWK.HasValue
                              ? (object)p.EinspeiseverguetungKWK.Value : DBNull.Value },
                    new DbParam("@bjahr", DbParamTyp.Integer)
                    { Wert = p.BilanzJahr > 0 ? (object)p.BilanzJahr : DBNull.Value },
                    new DbParam("@meth", DbParamTyp.VarWChar, 30)
                    { Wert = Steuerwert(p.EmissionsMethode, DbWerte.EMISSIONSMETHODE_KATALOG) },
                    new DbParam("@bkon", DbParamTyp.VarWChar, 30)
                    { Wert = Steuerwert(p.BiomasseKonvention, DbWerte.BIOMASSE_KONVENTION_NULL) },
                    new DbParam("@bnw", DbParamTyp.VarWChar, 30)
                    { Wert = p.NachhaltigkeitsnachweisBiomasse
                              ? DbWerte.BIOMASSE_NACHWEIS_JA : DbWerte.BIOMASSE_NACHWEIS_NEIN },
                    // ETAPPE K6 — die leere Angabe muss LEER in die Datenbank: Sie ist die
                    // Aussage „nicht angegeben" und damit etwas anderes als KEINER bzw.
                    // NEUANLAGE. Deshalb hier bewusst KEIN Steuerwert(...)-Rückfall.
                    new DbParam("@ktb", DbParamTyp.VarWChar, 30)
                    { Wert = LeerAlsNull(p.KwkgTatbestand) },
                    new DbParam("@kart", DbParamTyp.VarWChar, 20)
                    { Wert = LeerAlsNull(p.KwkgAnlagenart) },
                    new DbParam("@kant", DbParamTyp.Double)
                    { Wert = p.KwkgKostenanteil > 0 ? (object)p.KwkgKostenanteil : DBNull.Value },
                    new DbParam("@kpau", DbParamTyp.Boolean) { Wert = p.KwkgPauschalmodus },
                    new DbParam("@am", DbParamTyp.Date) { Wert = DateTime.Now },
                    new DbParam("@p", p.IdStamm));
                if (rows > 0) return true;

                int id = DataRepository.GetMaxID(TAB_PARAMETER, "ID") + 1;
                return DataRepository.ExecuteSQL(
                    "INSERT INTO " + TAB_PARAMETER + " (ID, ID_Projekt, Zinssatz, Betrachtungszeitraum, " +
                    "Preissteigerung_Energie, Preissteigerung_Betrieb, Einspeiseverguetung, " +
                    "CO2_Preis, KWKG_Bonus, KWKG_Vbh_Jahresdeckel, KWKG_Vbh_Kontingent, " +
                    "KWKG_Bonus_Einspeisung, ID_Kraftwerkspark, RefKessel_Wirkungsgrad, " +
                    "RefKessel_ID_Brennstoff, KWKG_Stichtag, KWKG_Inbetriebnahme, " +
                    "KWKG_Abschlag_Negativ, " +
                    "[" + SchemaKatalog.SPALTE_PW_UNTERNEHMENSART + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_RAEUMLICH + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_HOCHEFFIZIENZ + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_NUTZUNGSGRAD + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_ENERGIESTEUER_WAHL + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_AUFTEILUNG + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_AUFSCHLAEGE + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_BILANZJAHR + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_EMISSIONSMETHODE + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_BIOMASSE_KONVENTION + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_BIOMASSE_NACHWEIS + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_KWKG_TATBESTAND + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_KWKG_ANLAGENART + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_KWKG_KOSTENANTEIL + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_KWKG_PAUSCHALMODUS + "], " +
                    "GeaendertAm) " +
                    "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                    new DbParam("@id", id),
                    new DbParam("@p", p.IdStamm),
                    new DbParam("@z", p.Zinssatz),
                    new DbParam("@t", p.Betrachtungszeitraum),
                    new DbParam("@pe", p.PreissteigerungEnergie),
                    new DbParam("@pb", p.PreissteigerungBetrieb),
                    new DbParam("@ev", p.Einspeiseverguetung),
                    new DbParam("@co2", p.CO2Preis),
                    new DbParam("@kwkg", p.KwkgBonus),
                    new DbParam("@vbhj", p.KwkgVbhJahresdeckel),
                    new DbParam("@vbhk", p.KwkgVbhKontingent),
                    new DbParam("@kwkgE", p.KwkgBonusEinspeisung),
                    new DbParam("@park", p.IdKraftwerkspark),
                    new DbParam("@refEta", p.RefKesselWirkungsgrad),
                    new DbParam("@refBs", p.RefKesselIdBrennstoff),
                    new DbParam("@st", DbParamTyp.Date) { Wert = (object)p.KwkgStichtag ?? DBNull.Value },
                    new DbParam("@ibn", DbParamTyp.Date) { Wert = (object)p.KwkgInbetriebnahme ?? DBNull.Value },
                    new DbParam("@neg", p.KwkgAbschlagNegativ),
                    new DbParam("@art", DbParamTyp.VarWChar, 24)
                    { Wert = Steuerwert(p.Unternehmensart, DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE) },
                    new DbParam("@raum", DbParamTyp.Boolean) { Wert = p.RaeumlicherZusammenhang },
                    new DbParam("@heff", DbParamTyp.Boolean) { Wert = p.HocheffizienzNachweis },
                    new DbParam("@ng", DbParamTyp.Double)
                    { Wert = p.Jahresnutzungsgrad.HasValue ? (object)p.Jahresnutzungsgrad.Value : DBNull.Value },
                    new DbParam("@wahl", DbParamTyp.VarWChar, 20)
                    { Wert = Steuerwert(p.EnergiesteuerWahl, DbWerte.ENERGIESTEUER_WAHL_KEINE) },
                    new DbParam("@auf", DbParamTyp.VarWChar, 30)
                    { Wert = Steuerwert(p.AufteilungMethode, DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF) },
                    new DbParam("@aufs", DbParamTyp.Boolean) { Wert = p.AufschlaegeAnwenden },
                    new DbParam("@vkwk", DbParamTyp.Double)
                    { Wert = p.EinspeiseverguetungKWK.HasValue
                              ? (object)p.EinspeiseverguetungKWK.Value : DBNull.Value },
                    new DbParam("@bjahr", DbParamTyp.Integer)
                    { Wert = p.BilanzJahr > 0 ? (object)p.BilanzJahr : DBNull.Value },
                    new DbParam("@meth", DbParamTyp.VarWChar, 30)
                    { Wert = Steuerwert(p.EmissionsMethode, DbWerte.EMISSIONSMETHODE_KATALOG) },
                    new DbParam("@bkon", DbParamTyp.VarWChar, 30)
                    { Wert = Steuerwert(p.BiomasseKonvention, DbWerte.BIOMASSE_KONVENTION_NULL) },
                    new DbParam("@bnw", DbParamTyp.VarWChar, 30)
                    { Wert = p.NachhaltigkeitsnachweisBiomasse
                              ? DbWerte.BIOMASSE_NACHWEIS_JA : DbWerte.BIOMASSE_NACHWEIS_NEIN },
                    new DbParam("@ktb", DbParamTyp.VarWChar, 30)
                    { Wert = LeerAlsNull(p.KwkgTatbestand) },
                    new DbParam("@kart", DbParamTyp.VarWChar, 20)
                    { Wert = LeerAlsNull(p.KwkgAnlagenart) },
                    new DbParam("@kant", DbParamTyp.Double)
                    { Wert = p.KwkgKostenanteil > 0 ? (object)p.KwkgKostenanteil : DBNull.Value },
                    new DbParam("@kpau", DbParamTyp.Boolean) { Wert = p.KwkgPauschalmodus },
                    new DbParam("@am", DbParamTyp.Date) { Wert = DateTime.Now });
            }
            catch { return false; }
        }

        /// <summary>
        /// ETAPPE K6 — eine leere Angabe geht als <c>NULL</c> in die Datenbank, nicht als
        /// Leerstring. Gegenstück zu <see cref="Steuerwert"/>: Dort ist „leer" ein
        /// Fehler und wird durch die Vorgabe ersetzt, hier ist „leer" die Aussage
        /// „nicht angegeben" und muss erhalten bleiben.
        /// </summary>
        private static object LeerAlsNull(string wert)
        {
            if (wert == null) return DBNull.Value;
            wert = wert.Trim();
            return wert.Length == 0 ? (object)DBNull.Value : wert;
        }

        /// <summary>Steuerwert oder Vorgabe — ein leeres Feld darf nie in die Datenbank
        /// geraten (Etappe E4; leer und Vorgabe bedeuten dasselbe, aber der geschriebene
        /// Wert soll lesbar sein).</summary>
        private static string Steuerwert(string wert, string vorgabe)
        {
            return string.IsNullOrEmpty(wert) ? vorgabe : wert.Trim();
        }

        // ------------------------------------------------------------- Erzeuger der Gruppe

        /// <summary>Welche Erzeugertypen kommen in der Vergleichsgruppe vor?
        /// (Stamm + alle Varianten; Basis der kategorisierten Parameter-Anzeige.)</summary>
        public class ErzeugerFlags
        {
            public bool Bhkw;
            public bool Photovoltaik;
            public bool Heizkessel;
            /// <summary>Ä18: Wärmepumpe in der Gruppe — Anker des
            /// Strombezug-Tarifeinstiegs auf der Wirtschaftlichkeitsseite.</summary>
            public bool Waermepumpe;
            /// <summary>Brennstoff-Erzeuger vorhanden (BHKW oder Kessel) — Emissionsbilanz sinnvoll.</summary>
            public bool Brennstoff { get { return Bhkw || Heizkessel; } }
        }

        /// <summary>Erzeugertypen der Vergleichsgruppe des Stamms ermitteln
        /// (Eingabetabellen Tab_BHKW / Tab_PV / Tab_Heizkessel je Projekt).</summary>
        public ErzeugerFlags ErzeugerDerGruppe(int idStamm)
        {
            var f = new ErzeugerFlags();
            if (idStamm <= 0) return f;
            var ids = new List<int> { idStamm };
            try
            {
                new VariantenCtrl().StelleVariantentabelleSicher();
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT ID_Projekt FROM " + VariantenCtrl.TAB_VARIANTE + " WHERE ID_ProjektRef = ?",
                    new DbParam("@p", idStamm));
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        if (r["ID_Projekt"] != DBNull.Value) ids.Add(Convert.ToInt32(r["ID_Projekt"]));
            }
            catch { }

            f.Bhkw = ErzeugerVorhanden("Tab_BHKW", ids);
            f.Photovoltaik = ErzeugerVorhanden("Tab_PV", ids);
            f.Heizkessel = ErzeugerVorhanden("Tab_Heizkessel", ids);
            f.Waermepumpe = ErzeugerVorhanden("Tab_WP", ids);
            return f;
        }

        private static bool ErzeugerVorhanden(string tabelle, List<int> projektIds)
        {
            if (projektIds == null || projektIds.Count == 0) return false;
            try
            {
                // Eine Abfrage je Tabelle statt N Einzelabfragen; die IDs stammen
                // aus der DB (int) — Inline-IN ist hier unkritisch.
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + tabelle +
                    " WHERE ID_Projekt IN (" + string.Join(",", projektIds) + ")");
                return o != null && o != DBNull.Value && Convert.ToInt32(o) > 0;
            }
            catch
            {
                // Fail-open: im Zweifel Gruppe EINBLENDEN, damit die Parameter
                // auch bei DB-Störungen editierbar bleiben (Review Phase 10).
                return true;
            }
        }

        // ------------------------------------------------------------- Tarif (W3)

        public TarifParameter LadeTarif(int idStamm)
        {
            StelleTabellenSicher();
            var t = new TarifParameter { IdStamm = idStamm };
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM " + TAB_TARIF + " WHERE ID_Projekt = ?",
                    new DbParam("@p", idStamm));
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    t.Aktiv = B(r, "Aktiv");
                    t.WinterVonMonat = (int)(D(r, "Winter_Von") ?? t.WinterVonMonat);
                    t.WinterBisMonat = (int)(D(r, "Winter_Bis") ?? t.WinterBisMonat);
                    t.HtVonStunde = (int)(D(r, "HT_Von") ?? t.HtVonStunde);
                    t.HtBisStunde = (int)(D(r, "HT_Bis") ?? t.HtBisStunde);
                    t.PreisBezugWinterHT = D(r, "Bezug_W_HT") ?? 0;
                    t.PreisBezugWinterNT = D(r, "Bezug_W_NT") ?? 0;
                    t.PreisBezugSommerHT = D(r, "Bezug_S_HT") ?? 0;
                    t.PreisBezugSommerNT = D(r, "Bezug_S_NT") ?? 0;
                    t.PreisEinspWinterHT = D(r, "Einsp_W_HT") ?? 0;
                    t.PreisEinspWinterNT = D(r, "Einsp_W_NT") ?? 0;
                    t.PreisEinspSommerHT = D(r, "Einsp_S_HT") ?? 0;
                    t.PreisEinspSommerNT = D(r, "Einsp_S_NT") ?? 0;
                    t.StaffelGrenzeKW = D(r, "Staffel_Grenze") ?? 0;
                    t.StaffelPreis1EurKW = D(r, "Staffel_Preis1") ?? 0;
                    t.StaffelPreis2EurKW = D(r, "Staffel_Preis2") ?? 0;

                    // ETAPPE E5 — Rollenmodell. Ein LEERER Modus bedeutet genau
                    // dasselbe wie der Vorgabewert ZONEN: der Bestandsrechenweg. Eine
                    // nicht migrierte Datenbank verhält sich dadurch wie eine migrierte.
                    string modus = Text(r, SchemaKatalog.SPALTE_TARIF_MODUS);
                    if (modus.Length > 0) t.Modus = modus;
                    if (r.Table.Columns.Contains(SchemaKatalog.SPALTE_TARIF_GUELTIGAB) &&
                        r[SchemaKatalog.SPALTE_TARIF_GUELTIGAB] != DBNull.Value)
                        t.GueltigAb = Convert.ToDateTime(r[SchemaKatalog.SPALTE_TARIF_GUELTIGAB]);

                    LiesRolle(r, "Bezug_", t.Bezug);
                    LiesRolle(r, "Rest_", t.Reststrom);
                    t.Einspeisung.ArbeitspreisEurKWh = D(r, "Einsp_Arbeit") ?? 0;
                    t.Einspeisung.GrundpreisEurJahr = D(r, "Einsp_Grundpreis") ?? 0;
                }
            }
            catch { }
            return t;
        }

        /// <summary>ETAPPE E5 — eine Tarifrolle (Bezug/Reststrom) aus der Zeile lesen.</summary>
        private static void LiesRolle(DataRow r, string prefix, TarifRolle rolle)
        {
            rolle.ArbeitspreisEurKWh = D(r, prefix + "Arbeit") ?? 0;
            rolle.GrundpreisEurJahr = D(r, prefix + "Grundpreis") ?? 0;
            rolle.MonatspreisEurKWMonat = D(r, prefix + "Monatspreis") ?? 0;
            string modell = Text(r, prefix + "Leistungsmodell");
            if (modell.Length > 0) rolle.Leistungsmodell = modell;
            for (int i = 0; i < rolle.Stufen.Count && i < 4; i++)
            {
                string s = prefix + "Stufe" + (i + 1) + "_";
                rolle.Stufen[i].ObergrenzeKW = D(r, s + "KW") ?? 0;
                rolle.Stufen[i].PreisSommer = D(r, s + "Sommer") ?? 0;
                rolle.Stufen[i].PreisWinter = D(r, s + "Winter") ?? 0;
            }
        }

        public bool SpeichereTarif(TarifParameter t)
        {
            if (t == null || t.IdStamm <= 0) return false;
            StelleTabellenSicher();
            try
            {
                // Spaltenliste und Werte entstehen aus EINER Quelle — bei 52 Spalten
                // wäre eine von Hand gepflegte Fragezeichenkette die klassische
                // Fehlerquelle (ETAPPE E5; bis dahin waren es 17 Spalten).
                // DbParam dürfen nur EINER Parameters-Collection angehören,
                // deshalb je Kommando ein frischer Satz.
                List<string> spalten = TarifSpalten();
                Func<List<DbParam>> werte = () => TarifWerte(t);

                var setzt = new StringBuilder();
                foreach (string s in spalten)
                {
                    if (setzt.Length > 0) setzt.Append(", ");
                    setzt.Append('[').Append(s).Append("] = ?");
                }

                List<DbParam> update = werte();
                update.Add(new DbParam("@p", t.IdStamm));
                int rows = DataRepository.ExecuteNonQuery(
                    "UPDATE " + TAB_TARIF + " SET " + setzt + " WHERE ID_Projekt = ?",
                    update.ToArray());
                if (rows > 0) return true;

                int id = DataRepository.GetMaxID(TAB_TARIF, "ID") + 1;
                var insert = new List<DbParam>
                {
                    new DbParam("@id", id),
                    new DbParam("@p", t.IdStamm)
                };
                insert.AddRange(werte());

                var namen = new StringBuilder("ID, ID_Projekt");
                var frage = new StringBuilder("?,?");
                foreach (string s in spalten)
                {
                    namen.Append(", [").Append(s).Append(']');
                    frage.Append(",?");
                }
                return DataRepository.ExecuteSQL(
                    "INSERT INTO " + TAB_TARIF + " (" + namen + ") VALUES (" + frage + ")",
                    insert.ToArray());
            }
            catch { return false; }
        }

        /// <summary>Spaltenreihenfolge des Tarifsatzes — EINE Wahrheit für UPDATE und INSERT.</summary>
        private static List<string> TarifSpalten()
        {
            var s = new List<string>
            {
                "Aktiv", "Winter_Von", "Winter_Bis", "HT_Von", "HT_Bis",
                "Bezug_W_HT", "Bezug_W_NT", "Bezug_S_HT", "Bezug_S_NT",
                "Einsp_W_HT", "Einsp_W_NT", "Einsp_S_HT", "Einsp_S_NT",
                "Staffel_Grenze", "Staffel_Preis1", "Staffel_Preis2",
                // ETAPPE E5
                SchemaKatalog.SPALTE_TARIF_MODUS, SchemaKatalog.SPALTE_TARIF_GUELTIGAB
            };
            foreach (string p in new[] { "Bezug_", "Rest_" })
            {
                s.Add(p + "Arbeit"); s.Add(p + "Grundpreis");
                s.Add(p + "Leistungsmodell"); s.Add(p + "Monatspreis");
                for (int i = 1; i <= 4; i++)
                { s.Add(p + "Stufe" + i + "_KW"); s.Add(p + "Stufe" + i + "_Sommer"); s.Add(p + "Stufe" + i + "_Winter"); }
            }
            s.Add("Einsp_Arbeit"); s.Add("Einsp_Grundpreis");
            s.Add("GeaendertAm");
            return s;
        }

        /// <summary>Werte in der Reihenfolge von <see cref="TarifSpalten"/>.</summary>
        private static List<DbParam> TarifWerte(TarifParameter t)
        {
            var w = new List<DbParam>
            {
                new DbParam("@a", DbParamTyp.Boolean) { Wert = t.Aktiv },
                new DbParam("@wv", t.WinterVonMonat),
                new DbParam("@wb", t.WinterBisMonat),
                new DbParam("@hv", t.HtVonStunde),
                new DbParam("@hb", t.HtBisStunde),
                new DbParam("@b1", t.PreisBezugWinterHT),
                new DbParam("@b2", t.PreisBezugWinterNT),
                new DbParam("@b3", t.PreisBezugSommerHT),
                new DbParam("@b4", t.PreisBezugSommerNT),
                new DbParam("@e1", t.PreisEinspWinterHT),
                new DbParam("@e2", t.PreisEinspWinterNT),
                new DbParam("@e3", t.PreisEinspSommerHT),
                new DbParam("@e4", t.PreisEinspSommerNT),
                new DbParam("@sg", t.StaffelGrenzeKW),
                new DbParam("@s1", t.StaffelPreis1EurKW),
                new DbParam("@s2", t.StaffelPreis2EurKW),
                // ETAPPE E5: TEXT(12) — der längste Steuerwert ROLLEN hat 6 Zeichen.
                new DbParam("@mod", DbParamTyp.VarWChar, 12)
                { Wert = Steuerwert(t.Modus, DbWerte.TARIF_MODUS_ZONEN) },
                new DbParam("@gab", DbParamTyp.Date)
                { Wert = (object)t.GueltigAb ?? DBNull.Value }
            };
            RolleWerte(w, t.Bezug);
            RolleWerte(w, t.Reststrom);
            w.Add(new DbParam("@ea", t.Einspeisung.ArbeitspreisEurKWh));
            w.Add(new DbParam("@eg", t.Einspeisung.GrundpreisEurJahr));
            w.Add(new DbParam("@am", DbParamTyp.Date) { Wert = DateTime.Now });
            return w;
        }

        /// <summary>Die 16 Werte einer Rolle in der Reihenfolge von <see cref="TarifSpalten"/>.</summary>
        private static void RolleWerte(List<DbParam> w, TarifRolle r)
        {
            w.Add(new DbParam("@ra", r.ArbeitspreisEurKWh));
            w.Add(new DbParam("@rg", r.GrundpreisEurJahr));
            // TEXT(24): der längste Steuerwert JAHRESHOECHSTLAST hat 17 Zeichen. Ein zu
            // kurzes Feld ließe das UPDATE STILL scheitern (Lehre aus Etappe E3).
            w.Add(new DbParam("@rm", DbParamTyp.VarWChar, 24)
            { Wert = Steuerwert(r.Leistungsmodell, DbWerte.LEISTUNGSMODELL_MONATLICH) });
            w.Add(new DbParam("@rp", r.MonatspreisEurKWMonat));
            for (int i = 0; i < 4; i++)
            {
                LeistungsStufe s = i < r.Stufen.Count ? r.Stufen[i] : new LeistungsStufe();
                w.Add(new DbParam("@sk" + i, s.ObergrenzeKW));
                w.Add(new DbParam("@ss" + i, s.PreisSommer));
                w.Add(new DbParam("@sw" + i, s.PreisWinter));
            }
        }

        // ------------------------------------------------------------- Berechnung

        /// <summary>
        /// Rechnet alle Szenarien für die gesammelte Vergleichsgruppe und
        /// persistiert die Ergebnisse. daten stammt aus BerichtsDatenSammler.Sammle
        /// (dort ist die Vorbedingung „Simulation vorhanden/aktuell" bereits
        /// erledigt, inkl. automatischem Rechnen fehlender Ergebnisse).
        /// </summary>
        public List<WirtschaftlichkeitErgebnis> Berechne(BerichtsDaten daten, WirtschaftlichkeitParameter p)
        {
            var alle = new List<WirtschaftlichkeitErgebnis>();
            var sens = new List<SensitivitaetZeile>();
            var matrizen = new Dictionary<int, StromMatrix>();   // W3: je Projekt (szenariounabhängig)
            if (daten == null || daten.Varianten.Count == 0 || p == null) return alle;
            StelleTabellenSicher();
            TarifParameter tarif = LadeTarif(daten.IdStamm);      // W3: gilt für die ganze Gruppe
            _staffelCache = null; _pelCache.Clear(); _oelCache.Clear();
            _refKesselCache.Clear();                                       // frischer Lauf
            _anlagenCache.Clear(); _gesetze = null;                        // Nachtrag zu E2
            _kesselCache.Clear();                                          // Etappe B3 Paket a
            _brennstoffKategorie = null; _carrierBrennstoff = null;        // Nachtrag 2 zu E2
            _traegerCache.Clear();                                         // Etappe E4

            foreach (string szenario in WirtschaftlichkeitSzenario.Alle)
            {
                ProjektEingabe stammEingabe = null;
                KapitalwertRechner.Zahlungsbild stammBild = null;
                WirtschaftlichkeitErgebnis stammErg = null;

                foreach (VariantenDaten v in daten.Varianten)
                {
                    ProjektEingabe eingabe = BaueEingabe(v, p, tarif, szenario);
                    if (eingabe.Matrix != null && !matrizen.ContainsKey(v.IdProjekt))
                        matrizen[v.IdProjekt] = eingabe.Matrix;
                    WirtschaftlichkeitErgebnis erg = RechneProjekt(v, p, eingabe,
                        szenario, out KapitalwertRechner.Zahlungsbild bild);
                    alle.Add(erg);

                    if (v.IstStamm) { stammEingabe = eingabe; stammBild = bild; stammErg = erg; continue; }

                    // Referenz = Stamm (Entscheidung 11.08.2026): Differenzkennzahlen.
                    if (bild != null && stammBild != null &&
                        erg.Kapitalwert.HasValue && stammErg != null && stammErg.Kapitalwert.HasValue)
                    {
                        erg.KapitalwertDiff = erg.Kapitalwert.Value - stammErg.Kapitalwert.Value;
                        erg.AnnuitaetKW = erg.KapitalwertDiff.Value *
                            KapitalwertRechner.Annuitaet(p.Zinssatz / 100.0, p.Betrachtungszeitraum);
                        erg.AmortisationJahre = KapitalwertRechner.AmortisationDifferenz(bild, stammBild);
                        erg.IRR = KapitalwertRechner.InternerZinsfuss(bild, stammBild);   // W2

                        // Sensitivitätsanalyse (W2): nur Szenario Erwartet.
                        if (szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                            stammEingabe != null && eingabe.Energie.HasValue && stammEingabe.Energie.HasValue)
                            sens.AddRange(BaueSensitivitaet(v.IdProjekt, eingabe, stammEingabe, p,
                                                            erg.KapitalwertDiff.Value));
                    }
                }
            }

            Persistiere(alle, sens, matrizen, p);
            return alle;
        }

        // ------------------------------------------------------------- Verlauf (Phase 11)

        /// <summary>
        /// Kapitalwert-VERLAUF über einen frei wählbaren Horizont (Phase 11):
        /// kumulierte diskontierte Zahlungsströme je Projekt und Jahr 0…N —
        /// absolut und als Differenz zur Stamm-Referenz (Nulldurchgang der
        /// Differenzlinie = dynamische Amortisation). Der Horizont darf vom
        /// Betrachtungszeitraum T abweichen (auch &gt; T): gerechnet wird dann mit
        /// verlängertem/verkürztem Zeitraum (Ersatzbeschaffungen, KWKG-Reihe und
        /// Restwert folgen dem Horizont); die gespeicherten Parameter und die
        /// persistierten Ergebnisse bleiben unverändert. Die Reihen sind OHNE
        /// Restwert — Kapitalwert = Endwert + Restwert-Barwert (ausgewiesen).
        /// </summary>
        public WirtschaftlichkeitVerlauf BerechneVerlauf(BerichtsDaten daten,
            WirtschaftlichkeitParameter p, int jahre, string szenario)
        {
            var verlauf = new WirtschaftlichkeitVerlauf
            {
                Jahre = Math.Max(1, jahre),
                Szenario = szenario ?? WirtschaftlichkeitSzenario.ERWARTET
            };
            if (daten == null || daten.Varianten.Count == 0 || p == null) return verlauf;

            WirtschaftlichkeitParameter ph = p.Kopie();
            ph.Betrachtungszeitraum = verlauf.Jahre;

            TarifParameter tarif = LadeTarif(daten.IdStamm);
            _staffelCache = null; _pelCache.Clear(); _oelCache.Clear();   // frischer Lauf
            _anlagenCache.Clear(); _gesetze = null;                       // wie in Berechne
            _kesselCache.Clear();                                         // Etappe B3 Paket a
            _brennstoffKategorie = null; _carrierBrennstoff = null;
            _traegerCache.Clear();

            VerlaufSerie stamm = null;
            foreach (VariantenDaten v in daten.Varianten)
            {
                var serie = new VerlaufSerie
                {
                    IdProjekt = v.IdProjekt,
                    Anzeige = v.IstStamm ? "Stamm" : v.Anzeige,
                    IstStamm = v.IstStamm
                };

                ProjektEingabe eingabe = BaueEingabe(v, ph, tarif, verlauf.Szenario);
                if (v.Fehler != null || v.Ergebnis == null)
                    serie.Fehlgrund = v.Fehler ?? "Kein Simulationsergebnis vorhanden.";
                else if (!eingabe.Energie.HasValue)
                    serie.Fehlgrund = "Energiekosten nicht bestimmbar.";
                else
                {
                    KapitalwertRechner.Zahlungsbild bild =
                        RechneBild(eingabe, ph, ph.Zinssatz, ph.PreissteigerungEnergie, 1.0, 1.0);
                    var kum = new double[verlauf.Jahre + 1];
                    double summe = 0;
                    for (int t = 0; t <= verlauf.Jahre; t++)
                    {
                        summe += bild.BarwertReihe[t];
                        kum[t] = summe;
                    }
                    serie.Kumuliert = kum;
                    serie.RestwertBarwert = bild.RestwertBarwert;
                    // ETAPPE E7: Das ganze Zahlungsbild wandert mit — es trägt seit E7
                    // die Jahresreihen der Einzelpositionen, und genau die braucht die
                    // Mehrjahrestabelle des Berichts. Bisher wurde hier alles außer der
                    // kumulierten Summe verworfen.
                    serie.Bild = bild;
                }

                verlauf.Absolut.Add(serie);
                if (v.IstStamm) stamm = serie;
            }

            // Differenzlinien Variante − Stamm (nur wenn beide Reihen vorliegen).
            if (stamm != null && stamm.Kumuliert != null)
                foreach (VerlaufSerie s in verlauf.Absolut)
                {
                    if (s.IstStamm || s.Kumuliert == null) continue;
                    var d = new double[verlauf.Jahre + 1];
                    for (int t = 0; t <= verlauf.Jahre; t++)
                        d[t] = s.Kumuliert[t] - stamm.Kumuliert[t];
                    verlauf.Differenz.Add(new VerlaufSerie
                    {
                        IdProjekt = s.IdProjekt,
                        Anzeige = s.Anzeige,
                        Kumuliert = d,
                        RestwertBarwert = s.RestwertBarwert - stamm.RestwertBarwert
                    });
                }
            return verlauf;
        }

        // ------------------------------------------------------------- Eingaben (W2)

        /// <summary>Zahlungsgerüst-Eingaben eines Projekts (Basis für Rechnung + Sensitivität).</summary>
        private class ProjektEingabe
        {
            public List<KapitalwertRechner.InvestPosition> Investitionen =
                new List<KapitalwertRechner.InvestPosition>();
            /// <summary>ETAPPE K5: Investitionszuschuss [€], positiv (0 = keiner).
            /// Mindert I₀ einmalig; siehe <see cref="LiesInvestitionen(int,string,out double)"/>.</summary>
            public double Zuschuss;

            public double Betrieb;          // €/a (Kategorie 2, Szenariowert) — Topf p_B

            /// <summary>
            /// PAKET FX3 (Anwenderentscheid R-2): der Endenergie-Topf der
            /// Betriebskosten [€/a] — Positionen mit
            /// <c>PROZENT_ENDENERGIEKOSTEN</c>/<c>PROZENT_ENDENERGIEBEDARF</c>, seit
            /// PAKET FX4-b auch <c>PROZENT_BRENNSTOFFKOSTEN</c>/<c>PROZENT_STROMKOSTEN</c>
            /// (<see cref="IstEnergiepreisArt"/>). Er eskaliert in der Jahresreihe mit
            /// p_E statt mit p_B (Begründung an <see cref="BetriebsTopfe"/>) und wird
            /// seit FX4-c vom Sensitivitäts-Energiefaktor mitskaliert
            /// (<see cref="RechneBild"/>). <see cref="Betrieb"/> trägt ihn NICHT
            /// mehr mit; die Summe beider ist die ausgewiesene Betriebskostenzahl.
            /// </summary>
            public double Endenergie;

            /// <summary>PAKET FX3 (R-2) × KD6: Endenergie-Positionen mit Startjahr ≥ 2.</summary>
            public List<KeyValuePair<double, int>> EndenergieAbJahr =
                new List<KeyValuePair<double, int>>();

            /// <summary>
            /// PAKET FX5-a (Anwenderentscheid 03.09.2026, offener Punkt FX4-1): der in
            /// <see cref="Betrieb"/> ENTHALTENE investitionsgekoppelte Anteil [€/a] —
            /// Positionen mit <c>PROZENT_INVESTITION</c>
            /// (<see cref="BetriebsTopfe.InvestGekoppeltSofort"/>). Nur die Sensitivität
            /// „Investition Variante ±10 %" liest ihn und skaliert ihn mit dem
            /// Investitionsfaktor mit (<see cref="RechneBild"/>); jede andere Rechnung
            /// sieht ihn nicht, weil er in <see cref="Betrieb"/> längst steckt.
            /// </summary>
            public double InvestGekoppelt;

            /// <summary>PAKET FX5-a × KD6: derselbe Ausweis für Positionen mit
            /// Startjahr ≥ 2 — Teilmenge von <see cref="BetriebAbJahr"/>.</summary>
            public List<KeyValuePair<double, int>> InvestGekoppeltAbJahr =
                new List<KeyValuePair<double, int>>();

            public double? Energie;         // €/a (null = nicht bestimmbar)
            public double Erloes;           // €/a Einspeisevergütung (konstant)
            public double Behg;             // €/a BEHG-Abgabe Jahr 1 (steigt mit p_E)

            /// <summary>
            /// ETAPPE K6 (Konzept § 8.3, E5): die CO₂-Abgabe <b>jahresscharf</b> [€],
            /// Index 1…T, aus dem Preispfad des Gesetzeskatalogs. <c>null</c> = kein
            /// Pfad — dann gilt der konstante Projektwert <c>CO2_Preis</c> als Override
            /// und <see cref="Behg"/> wird wie bisher mit p_E fortgeschrieben.
            /// </summary>
            public double[] BehgJeJahr;

            /// <summary>
            /// ETAPPE E4 (L1): alle jahresscharfen Erlösreihen des Projekts, benannt —
            /// KWK-Zuschlag und die drei Steuergutschriften. Bis E4 stand hier ein
            /// einzelnes <c>double[] KwkgReihe</c>.
            /// </summary>
            public List<KapitalwertRechner.ErloesReihe> ErloesReihen =
                new List<KapitalwertRechner.ErloesReihe>();

            public double KwkgJahr1;

            // ETAPPE E4 — Jahr-1-Beträge der drei Steuergutschriften [€/a] und die
            // Herkunft der verwendeten Sätze (0 bzw. null = keine Gutschrift; der Grund
            // steht in Hinweis).
            public double EnergiesteuerJahr1;
            public double StromsteuerBefreiungJahr1;
            public double StromsteuerEntlastungJahr1;
            public string SteuerHerkunft;
            /// <summary>ETAPPE E2 (L6): erreichte ELEKTRISCHE Vbh [h/a] — die Größe, an
            /// der die KWKG-Deckelung hängt (0 = kein BHKW / nicht bestimmbar).</summary>
            public double VbhElektrisch;
            public double WaermeMWh;

            // Stufe W3 (Phase 8)
            public StromMatrix Matrix;      // null = keine Stundenreihen im Lauf
            public double? StromkostenTarif;
            public string Hinweis;

            // ETAPPE E5 — Differenzmethode und Aufschläge (reiner Ausweis; der
            // Kapitalwert rechnet mit den tatsächlichen Reststromkosten, in denen die
            // Einsparung bereits steckt — eine zusätzliche Erlöszeile wäre doppelt).
            public double VermiedenArbeit;
            public double VermiedenLeistung;
            public double VermiedenGesamt;
            public double AufschlagBetrag;

            // ETAPPE E7 — Aufschlüsselungen und Nachweise (reine Ausgabe).
            /// <summary>Anteil des PV-Überschusses am Einspeiseerlös [€/a].</summary>
            public double ErloesPv;
            /// <summary>Anteil der KWK-Einspeisung am Einspeiseerlös [€/a].</summary>
            public double ErloesKwk;
            /// <summary>Nachweis je BHKW-Modul der KWKG-Rechnung (E6 → E7).</summary>
            public List<KwkgModulNachweis> KwkgModule = new List<KwkgModulNachweis>();

            /// <summary>ETAPPE P4: Ergebnis des PV-Vergütungsdialogs (null =
            /// Dialog inaktiv — dann gilt exakt der Bestandsrechenweg).</summary>
            public PvErloesErgebnis PvVerguetung;

            /// <summary>ETAPPE KD6 (§ 11, FK10): Betriebskostenpositionen mit
            /// Startjahr ≥ 2 als (Betrag €/a, Startjahr) — sie laufen in der
            /// Kapitalwertreihe erst ab ihrem Jahr; <see cref="Betrieb"/> trägt
            /// nur noch den Sofort-Anteil.</summary>
            public List<KeyValuePair<double, int>> BetriebAbJahr =
                new List<KeyValuePair<double, int>>();
            /// <summary>Betriebskostenpositionen mit Kostenart und Herleitung (E3 → E7).</summary>
            public List<KostenPositionNachweis> Betriebskosten = new List<KostenPositionNachweis>();

            /// <summary>
            /// ETAPPE B2 — die Eingabe der Steuerrechnung dieses Laufs (Anlagen mit
            /// Träger, Heizwerten und Katalogschlüsseln, gewählte Norm, Netzbezug).
            /// <c>null</c> = kein Steuerpfad im Lauf (kein BHKW und kein produzierendes
            /// Gewerbe). <b>Reine Ausgabe:</b> Sie wird festgehalten, damit die
            /// Kohärenzprüfung dieselbe Grundlage liest, mit der gerechnet wurde, statt
            /// die Anlagen ein zweites Mal aufzulösen.
            /// </summary>
            public SteuerEingabe SteuerEingabe;
        }

        private ProjektEingabe BaueEingabe(VariantenDaten v, WirtschaftlichkeitParameter p,
                                           TarifParameter tarif, string szenario)
        {
            var e = new ProjektEingabe();
            if (v.Fehler != null || v.Ergebnis == null) return e;

            // ETAPPE K5: Zuschusszeilen kommen aus derselben Abfrage, gehen aber nicht in
            // die Positionsliste — sie mindern I₀ einmalig (Konzept § 7.4).
            double zuschuss;
            e.Investitionen = LiesInvestitionen(v.IdProjekt, szenario, out zuschuss);
            e.Zuschuss = zuschuss;
            // PAKET FX3 (R-2): zwei Töpfe statt einem — der Endenergie-Anteil wächst in
            // der Jahresreihe mit p_E (Begründung an BetriebsTopfe).
            BetriebsTopfe topfe = LiesBetriebskostenTopfe(v.IdProjekt, szenario);
            e.Betrieb = topfe.BetriebSofort;
            e.BetriebAbJahr = topfe.BetriebAbJahr;
            e.Endenergie = topfe.EndenergieSofort;
            e.EndenergieAbJahr = topfe.EndenergieAbJahr;
            // PAKET FX5-a: der investgekoppelte Ausweis wandert mit — er ändert an den
            // Beträgen nichts (er ist Teilmenge von Betrieb/BetriebAbJahr) und wird nur
            // in der Sensitivität gelesen.
            e.InvestGekoppelt = topfe.InvestGekoppeltSofort;
            e.InvestGekoppeltAbJahr = topfe.InvestGekoppeltAbJahr;
            List<KeyValuePair<double, int>> betriebAbJahr = topfe.BetriebAbJahr;

            // ETAPPE KD6 (§ 11, FK10): Sind Startjahre gesetzt, laufen Investition
            // (samt Ersatz/Restwert) und Betriebskosten der Position erst ab ihrem
            // Jahr. Die ENERGIEKOSTEN bleiben die Gesamtrechnung des Simulationslaufs
            // — die Simulation kennt keine Startjahre je Komponente; der Hinweis
            // macht die dokumentierte Vereinfachung sichtbar statt still.
            bool startjahre = betriebAbJahr.Count > 0 || e.EndenergieAbJahr.Count > 0;
            if (!startjahre)
                foreach (KapitalwertRechner.InvestPosition ip in e.Investitionen)
                    if (ip.StartJahr > 1) { startjahre = true; break; }
            if (startjahre)
                e.Hinweis = Anhaengen(e.Hinweis,
                    "Startjahre gesetzt (FK10): Investition und Betriebskosten der " +
                    "Positionen laufen ab ihrem Jahr; die Energiekosten bleiben die " +
                    "Gesamtrechnung des Simulationslaufs.");
            // ETAPPE E7: dieselben Positionen ein zweites Mal, diesmal mit ihrer
            // Herleitung. Der SUMMENweg oben bleibt unangetastet — der Bericht liest
            // eine zweite, ausschließlich beschreibende Sicht, statt die Rechnung auf
            // einen neuen Leseweg umzustellen.
            e.Betriebskosten = LiesBetriebskostenPositionen(v.IdProjekt, szenario);
            e.Energie = v.Energiekosten;   // KostenEmissionRechner (Phase 5)

            double pvUeberschussMWh = v.Ergebnis.Photovoltaik != null ? v.Ergebnis.Photovoltaik.Ueberschuss : 0;
            e.Erloes = pvUeberschussMWh * 1000.0 * p.Einspeiseverguetung;
            e.ErloesPv = e.Erloes;         // E7: Aufschlüsselung, siehe unten
            e.WaermeMWh = v.Ergebnis.Energiebedarf != null ? v.Ergebnis.Energiebedarf.Waermebedarf_Gesamt : 0;

            // ---------------- Strommengen-Matrix (W3) ----------------
            // Aus den Stundenreihen des Laufs; auch bei inaktivem Tarif gebaut,
            // sobald Reihen vorliegen (Basis des KWKG-Splits und des Berichts).
            e.Matrix = StromMatrix.Baue(v.Zeitreihen, tarif);

            // ---------------- Eingespeister KWK-Strom (ETAPPE E5) ----------------
            //
            // BESTANDSMANGEL, hier behoben: Bis E5 bewertete der Flat-Pfad
            // ausschließlich den PV-Überschuss. Eingespeister BHKW-Strom bekam gar
            // keinen Strompreis, sondern nur den KWK-Zuschlag — und das Feld dafür war
            // ohne Photovoltaik im Projekt im Parameterdialog nicht einmal sichtbar
            // (Form_WirtschaftlichkeitParameter, PV-Gruppe). Ökonomisch ist das grob
            // falsch: Der eingespeiste Strom wird vergütet, der Zuschlag kommt obendrauf.
            //
            // ERGEBNISNEUTRAL: Ohne gepflegte KWK-Vergütung (NULL) bleibt der Beitrag 0.
            double kwkEinspeisungMWh = e.Matrix != null ? e.Matrix.KwkEinspeisungGesamtMWh : 0;
            if (p.EinspeiseverguetungKWK.HasValue && p.EinspeiseverguetungKWK.Value != 0 &&
                kwkEinspeisungMWh > 0)
            {
                e.ErloesKwk = kwkEinspeisungMWh * 1000.0 * p.EinspeiseverguetungKWK.Value;
                e.Erloes += e.ErloesKwk;   // ETAPPE E7: derselbe Betrag, zusätzlich benannt
            }

            // ---------------- Tarif-Rollenmodell (ETAPPE E5) ----------------
            bool rollen = tarif != null && tarif.Aktiv && tarif.RollenModus;
            if (rollen) RechneRollentarif(v, tarif, e);

            // Tarifkosten ersetzen die Flat-Stromkosten NUR, wenn beide Seiten
            // bestimmbar sind (Energiekosten und Flat-Netzanteil aus Phase 5) UND
            // Zonenpreise gepflegt wurden (Review Phase 8: Aktiv + Nullpreise würde
            // den Strom sonst still kostenlos machen). Der Tarifersatz umfasst
            // Arbeits-, Grund- UND Leistungspreis der Kostenmaske.
            if (tarif != null && tarif.Aktiv && !rollen)
            {
                bool preiseGepflegt = tarif.PreisBezugWinterHT > 0 || tarif.PreisBezugWinterNT > 0 ||
                                      tarif.PreisBezugSommerHT > 0 || tarif.PreisBezugSommerNT > 0;
                if (!preiseGepflegt)
                    e.Hinweis = "Tarifstruktur aktiv, aber keine Bezugspreise gepflegt — " +
                                "Flat-Preise der Kostenmaske verwendet.";
                else if (e.Matrix != null && v.Energiekosten.HasValue && v.StromkostenNetz.HasValue)
                {
                    double stromTarif = e.Matrix.Bezugskosten(tarif);
                    e.StromkostenTarif = stromTarif;
                    e.Energie = v.Energiekosten.Value - v.StromkostenNetz.Value + stromTarif;
                    e.Erloes = e.Matrix.Einspeiseerloes(tarif);   // ersetzt PV × Flat-Vergütung
                    // ETAPPE E7: Der Zonenerlös trägt PV-Überschuss und KWK-Einspeisung
                    // zusammen. Der PV-Anteil wird eigens gerechnet, der KWK-Anteil als
                    // REST gebildet — so ist die Summe der beiden Teile ohne
                    // Rundungsrest der ausgewiesene Gesamtbetrag.
                    e.ErloesPv = e.Matrix.EinspeiseerloesPv(tarif);
                    e.ErloesKwk = e.Erloes - e.ErloesPv;

                    // Mengenabgleich Flat-Basis (Jahressumme) vs. Stundenreihe.
                    double flatMWh = v.Ergebnis.Energiebedarf != null
                                     ? v.Ergebnis.Energiebedarf.Stromrestbedarf : 0;
                    double reiheMWh = e.Matrix.BezugGesamtMWh;
                    if (flatMWh > 0 && Math.Abs(reiheMWh - flatMWh) / flatMWh > 0.05)
                        e.Hinweis = "Netzbezug der Stundenreihe (" + reiheMWh.ToString("N0") +
                                    " MWh) weicht > 5 % vom Jahresergebnis (" + flatMWh.ToString("N0") +
                                    " MWh) ab — Tarifkosten bitte prüfen.";
                    else if (e.Matrix.StrombedarfFehlt)
                        e.Hinweis = "KWK-Split ohne Strombedarfs-Reihe — gesamte BHKW-Erzeugung " +
                                    "als Eigenstrom gewertet.";
                }
                else if (e.Matrix == null)
                    e.Hinweis = "Tarifstruktur aktiv, aber keine (vollständigen) Stundenreihen im " +
                                "Lauf — Flat-Preise der Kostenmaske verwendet.";
                else
                    e.Hinweis = "Tarifstruktur aktiv, aber Flat-Energiekosten unvollständig — " +
                                "Tarifersatz nicht möglich.";
            }

            // ---------------- PV-Vergütung (PV-Konzept, ETAPPE P4) ----------------
            // Ist der Vergütungsdialog AKTIV, ersetzt seine jahresscharfe Reihe
            // (ErloesReihe.PV_VERGUETUNG) die PV-Bewertung des gerade aktiven Pfades
            // (Flat/Rollen/Tarif) — EINE Vergütungswahrheit (Befund V4, F7). Der Platz
            // NACH allen drei Pfaden ist Absicht: Jeder von ihnen führt e.ErloesPv,
            // also wird genau dieser Anteil aus dem konstanten Erlös herausgelöst.
            // Inaktiv (Aktiv = false) ändert sich NICHTS — Abnahmekriterium P4.
            RechnePvVerguetung(v, p, e);

            // BEHG (W2): nur Brennstoff-CO₂ ist abgabepflichtig; ohne vollständige
            // Faktoren (CO2Brennstoff = null) bleibt die Abgabe 0 und ist im
            // Ergebnis als 0 sichtbar (Faktoren in der Kostenmaske pflegen).
            //
            // LEITENTSCHEIDUNG L13: Fehlt der Nachhaltigkeitsnachweis nach § 8 EBeV 2030,
            // gilt für die FLÜSSIGE Biomasse (Rapsöl, Tierische Fette) der volle fossile
            // Standardwert der EBeV — sie wird abgabepflichtig, statt mit null anzusetzen.
            // Feste Biomasse, Biogas und Klärgas sind keine BEHG-Brennstoffe und bleiben
            // in jedem Fall außen vor (Grundlagen 7.7). Vorgabe ist „Nachweis liegt vor";
            // dann ist dieser Zweig wirkungslos und die Abgabe bleibt die bisherige.
            double behgBasisT = v.CO2Brennstoff ?? 0;
            BilanzKonvention konv = Bilanzregeln(p);
            double efOhneNachweis = konv.BehgOhneNachweisGJeKWh;
            if (efOhneNachweis > 0 && v.BiogenBehgMengeMWh > 0)
            {
                behgBasisT += v.BiogenBehgMengeMWh * efOhneNachweis / 1000.0;   // MWh × g/kWh → t
                e.Hinweis = Anhaengen(e.Hinweis,
                    "Ohne Nachhaltigkeitsnachweis (§ 8 EBeV 2030): " +
                    v.BiogenBehgMengeMWh.ToString("N0") + " MWh flüssige Biomasse mit " +
                    efOhneNachweis.ToString("N1") + " g CO₂/kWh abgabepflichtig.");
            }
            // ETAPPE K6 (Konzept § 8.3, Entscheidung E5): Der CO₂-Preis kommt
            // JAHRESGENAU aus dem Gesetzeskatalog (Klasse CO₂-Preispfad), der
            // Projektwert CO2_Preis ist nur noch der Override „konstanter Preis".
            //
            // ACHTUNG, DIE EINE GEWOLLTE ERGEBNISÄNDERUNG DER ETAPPE: Bis K6 bedeutete
            // CO2_Preis = 0 „CO₂-Abgabe aus"; ab K6 bedeutet es „Pfad aus dem Katalog".
            // Jedes Bestandsprojekt mit 0 bekommt damit eine BEHG-Abgabe, die es vorher
            // nicht hatte. Das ist die im Konzept § 10 angekündigte Änderung; sie steht
            // im Hinweisfeld des Ergebnisses, damit sie niemanden überrascht.
            string co2Hinweis;
            e.BehgJeJahr = BaueCo2Reihe(p, behgBasisT, out co2Hinweis);
            e.Behg = e.BehgJeJahr != null ? e.BehgJeJahr[1]
                                          : (p.CO2Preis > 0 ? behgBasisT * p.CO2Preis : 0);
            if (co2Hinweis != null && behgBasisT > 0)
                e.Hinweis = Anhaengen(e.Hinweis, co2Hinweis);

            // ETAPPE E2 (L6): die erreichten ELEKTRISCHEN Vollbenutzungsstunden — die
            // Bezugsgröße der KWKG-Deckelung. Sie wird UNABHÄNGIG davon geführt, ob ein
            // KWKG-Satz gepflegt ist, damit Reiter und Bericht sie auch dann zeigen
            // können; der zugehörige Hinweis entsteht ausschließlich in BaueKwkgReihe,
            // also nur dort, wo die Größe tatsächlich gebraucht wird.
            string vbhHinweisUnbenutzt;
            e.VbhElektrisch = VbhElektrisch(v, out vbhHinweisUnbenutzt);

            // ETAPPE K6 — Pauschale § 9 KWKG VOR der laufenden Reihe: Greift sie, gibt
            // es keinen laufenden Zuschlag mehr („damit entfällt die Einzelabrechnung").
            double[] pauschalReihe;
            string pauschalHinweis;
            bool pauschalGreift = PauschaleReihe(v, p, out pauschalReihe, out pauschalHinweis);
            if (pauschalReihe != null)
                e.ErloesReihen.Add(new KapitalwertRechner.ErloesReihe(
                    KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE, pauschalReihe));
            if (pauschalHinweis != null)
                e.Hinweis = Anhaengen(e.Hinweis, pauschalHinweis);

            double kwkgJahr1 = 0;
            string kwkgHinweis = null;
            double[] kwkgReihe = pauschalGreift
                ? null
                : BaueKwkgReihe(v, p, e.Matrix, e.KwkgModule, out kwkgJahr1, out kwkgHinweis);
            e.KwkgJahr1 = kwkgJahr1;
            if (kwkgReihe != null)
                e.ErloesReihen.Add(new KapitalwertRechner.ErloesReihe(
                    KapitalwertRechner.ErloesReihe.KWKG, kwkgReihe));
            if (kwkgHinweis != null)
                e.Hinweis = e.Hinweis == null ? kwkgHinweis : e.Hinweis + " | " + kwkgHinweis;

            // ETAPPE E4: die drei Steuergutschriften, jahresscharf nach denselben Regeln
            // wie die KWKG-Reihe (Förderbeginn + t − 1 als Stichtagsjahr).
            string steuerHinweis;
            BaueSteuerReihen(v, p, e, out steuerHinweis);
            if (steuerHinweis != null)
                e.Hinweis = e.Hinweis == null ? steuerHinweis : e.Hinweis + " | " + steuerHinweis;

            // ETAPPE E5: die Aufschläge auf den Strombezug — NACH den Steuerreihen,
            // damit der Abgleich mit der § 9b-Entlastung beide Größen kennt.
            RechneAufschlaege(v, p, e);
            return e;
        }

        // =====================================================================
        // ETAPPE E5 — Tarif-Rollenmodell und Aufschläge
        // =====================================================================

        /// <summary>
        /// Rechnet den Strom nach dem <b>Rollenmodell</b> (Tarifmodus <c>ROLLEN</c>):
        /// Bezugstarif ohne BHKW, Reststromtarif mit BHKW, Einspeisetarif — und daraus
        /// die vermiedenen Kosten nach der <b>Differenzmethode</b> (Konzept 4.3).
        ///
        /// <para><b>Was in den Kapitalwert geht, ist der Reststrom.</b> Die vermiedenen
        /// Kosten sind eine AUSSAGE, kein zweiter Zahlungsstrom: Die Einsparung steckt
        /// bereits darin, dass die Anlage die Bezugsmenge senkt. Wer sie zusätzlich als
        /// Erlös bucht, zählt sie doppelt. Deshalb ersetzt hier — wie im Zonenmodell —
        /// der Tarifbetrag den Flat-Netzanteil der Energiekosten, und die drei
        /// Differenzzeilen werden nur ausgewiesen.</para>
        ///
        /// <para><b>Ohne Strombedarfsreihe keine Referenz.</b> „Bedarf ohne Anlage" lässt
        /// sich nur aus der Stundenreihe bilden. Fehlt sie, bleiben die vermiedenen
        /// Kosten 0 und der Hinweis sagt warum — statt eine Einsparung in Höhe der
        /// gesamten Reststromkosten zu behaupten.</para>
        /// </summary>
        private void RechneRollentarif(VariantenDaten v, TarifParameter tarif, ProjektEingabe e)
        {
            if (e.Matrix == null)
            {
                Melde(e, "Tarif-Rollenmodell aktiv, aber keine (vollständigen) Stundenreihen im " +
                         "Lauf — Flat-Preise der Kostenmaske verwendet.");
                return;
            }
            bool preiseGepflegt = tarif.Reststrom.ArbeitspreisEurKWh > 0 ||
                                  tarif.Bezug.ArbeitspreisEurKWh > 0;
            if (!preiseGepflegt)
            {
                Melde(e, "Tarif-Rollenmodell aktiv, aber kein Arbeitspreis für Bezug oder " +
                         "Reststrom gepflegt — Flat-Preise der Kostenmaske verwendet.");
                return;
            }
            if (!v.Energiekosten.HasValue || !v.StromkostenNetz.HasValue)
            {
                Melde(e, "Tarif-Rollenmodell aktiv, aber Flat-Energiekosten unvollständig — " +
                         "Tarifersatz nicht möglich.");
                return;
            }

            var eingabe = new StromErloesEingabe
            {
                BedarfMWh = e.Matrix.BedarfGesamtMWh,
                RestbezugMWh = e.Matrix.BezugGesamtMWh,
                EinspeisungMWh = e.Matrix.EinspeisungPvGesamtMWh + e.Matrix.KwkEinspeisungGesamtMWh,
                LastBedarf = e.Matrix.LastBedarf,
                LastRestbezug = e.Matrix.LastBezug
            };
            StromErloesErgebnis r = StromTarifRechner.Rechne(
                eingabe, tarif.Bezug, tarif.Reststrom, tarif.Einspeisung, BerichtTexte.Kultur);

            e.StromkostenTarif = r.Reststrom.SummeEur;
            e.Energie = v.Energiekosten.Value - v.StromkostenNetz.Value + r.Reststrom.SummeEur;
            e.Erloes = r.EinspeiseerloesEur;   // ersetzt PV-/KWK-Bewertung über die Parameter

            // ETAPPE E7: Das Rollenmodell kennt EINEN Einspeisetarif für beide Mengen —
            // die Aufteilung kann deshalb nur MENGENPROPORTIONAL sein, und sie wird als
            // solche benannt. Der KWK-Anteil entsteht als Rest, damit die Summe stimmt.
            double pvMWh = e.Matrix.EinspeisungPvGesamtMWh;
            double kwkMWh = e.Matrix.KwkEinspeisungGesamtMWh;
            double gesamtMWh = pvMWh + kwkMWh;
            if (gesamtMWh > 0)
            {
                e.ErloesPv = e.Erloes * (pvMWh / gesamtMWh);
                e.ErloesKwk = e.Erloes - e.ErloesPv;
            }
            else { e.ErloesPv = 0; e.ErloesKwk = 0; }

            if (e.Matrix.StrombedarfFehlt)
                Melde(e, "Vermiedene Kosten nicht bestimmbar: Die Strombedarfs-Reihe fehlt im " +
                         "Lauf, damit gibt es keine Bezugsgröße „Bedarf ohne Anlage\".");
            else
            {
                e.VermiedenArbeit = r.VermiedenArbeitEur;
                e.VermiedenLeistung = r.VermiedenLeistungEur;
                e.VermiedenGesamt = r.VermiedenGesamtEur;
                foreach (string h in r.Herleitung) Melde(e, h);
            }
        }

        /// <summary>
        /// Schlägt Netzentgelt, Umlagen, Stromsteuer, Konzessionsabgabe und Vertrieb auf
        /// den Strombezug auf — <b>nur</b>, wenn das Projekt es ausdrücklich verlangt
        /// (<see cref="WirtschaftlichkeitParameter.AufschlaegeAnwenden"/>, Vorgabe aus).
        ///
        /// <para><b>Eine Wahrheit für den wirksamen Aufschlag.</b> Gelesen wird derselbe
        /// Block, mit dem die Speichersimulation rechnet
        /// (<c>StromAufschlagCtrl.ReadStrom</c> ⇒ <c>Aufschlagssatz.WirksamCtKwh</c>:
        /// Override im Modus Gesamtwert, sonst Summe der AKTIVEN Komponenten). Ein
        /// eigener Rechenweg hier wäre die zweite Wahrheit, die das Fachkonzept des
        /// Aufschlagsblocks gerade vermeidet.</para>
        ///
        /// <para><b>Der Betrag wird ausgewiesen, nicht versteckt.</b> Er steht als eigene
        /// Ergebnisgröße und als Hinweiszeile mit Satz, Menge und Herkunft — sonst wäre
        /// ein Drittel der Energiekosten eine stille Zahl. Zu beachten ist dabei: Ein
        /// Trägersatz ohne gepflegte Werte liefert die VORSCHLAGSWERTE des Fachkonzepts
        /// (in Summe 11,746 ct/kWh), nicht 0 — deshalb nennt der Hinweis den Satz.</para>
        ///
        /// <para><b>Abgleich mit der Stromsteuer aus E4.</b> Der Aufschlagsblock enthält
        /// die Stromsteuer als BELASTUNG, § 9b StromStG die Entlastung als GUTSCHRIFT.
        /// Zusammen sind sie kein Doppelansatz, sondern die zwei Seiten derselben
        /// Vorschrift. Steht der Schalter dagegen auf AUS, während § 9b greift, enthält
        /// der Kapitalwert eine Entlastung ohne die zugehörige Belastung — genau darauf
        /// weist der Hinweis dann hin.</para>
        /// </summary>
        /// <summary>
        /// ETAPPE P4 (PV-Konzept § 4.4/§ 4.6): rechnet bei AKTIVEM Vergütungsdialog
        /// die jahresscharfe PV-Erlösreihe und ersetzt damit den PV-Anteil des
        /// konstanten Einspeiseerlöses. Stufe 2 (gemessener § 51-Ausfall, Spoterlös,
        /// Kappung) greift von selbst, wenn Stundenreihen des Laufs und eine
        /// Spot-Preisreihe des Projekts vorliegen — sonst rechnet Stufe 1 mit der
        /// Ausfall-Pauschale. Fehler kippen den Lauf nicht (Hinweis statt Absturz).
        /// </summary>
        private void RechnePvVerguetung(VariantenDaten v, WirtschaftlichkeitParameter p,
                                        ProjektEingabe e)
        {
            try
            {
                ProjektPhotovoltaikCtrl pvc = new ProjektPhotovoltaikCtrl();
                ProjektPhotovoltaikModel pv = pvc.Lies(v.IdProjekt);
                if (pv == null || !pv.Aktiv) return;

                double kwp = PhotovoltaikCtrl.KwpDesProjekts(v.IdProjekt);
                double einspMWh = v.Ergebnis != null && v.Ergebnis.Photovoltaik != null
                    ? v.Ergebnis.Photovoltaik.Ueberschuss : 0;   // nach V2 (P1)

                double[] einspStunden = null;
                if (v.Zeitreihen != null)
                {
                    double[] reihe = v.Zeitreihen.Hole(ZeitreihenSatz.PV_UEBERSCHUSS);
                    if (reihe != null && reihe.Length >= 8760) einspStunden = reihe;
                }

                // Spot-Preisreihe des Projekts — dieselbe Stichtagsregel wie die
                // Speicherwelt (eine Preiswahrheit je Projekt, F10).
                double[] spot = null;
                try
                {
                    PreisreiheCtrl prc = new PreisreiheCtrl();
                    PreisreiheModel kopf = prc.ReadZumJahr(v.IdProjekt, pv.Inbetriebnahme.Year);
                    if (kopf != null)
                    {
                        double[] werte = prc.ReadWerte(kopf.ID);
                        if (werte != null && werte.Length >= 8760)
                            spot = werte.Length >= 8760 * 4
                                ? ViertelstundenZuStundenMittel(werte)
                                : werte;
                    }
                }
                catch { }

                GesetzKatalog katalog = new GesetzKatalog();
                PvErloesErgebnis pe = PvErloesRechner.Rechne(pv, kwp, einspMWh,
                    einspStunden, spot, p.Betrachtungszeitraum, katalog.Wert,
                    jahr => pvc.Jahresmarktwert(jahr, pv), BerichtTexte.Kultur);

                // Der bisherige PV-Anteil (des jeweils aktiven Pfades) verlässt den
                // konstanten Erlös; die Dialog-Reihe übernimmt.
                e.Erloes -= e.ErloesPv;
                e.ErloesPv = pe.JeJahr != null && pe.JeJahr.Length > 1 ? pe.JeJahr[1] : 0;
                e.ErloesReihen.Add(new KapitalwertRechner.ErloesReihe(
                    KapitalwertRechner.ErloesReihe.PV_VERGUETUNG, pe.JeJahr));
                e.PvVerguetung = pe;
                e.Hinweis = Anhaengen(e.Hinweis, "PV-Vergütungsdialog aktiv: " + pe.Herleitung);
            }
            catch (Exception ex)
            {
                e.Hinweis = Anhaengen(e.Hinweis,
                    "PV-Vergütung nicht gerechnet: " + ex.Message);
            }
        }

        /// <summary>Viertelstundenpreise [ct/kWh] → Stundenmittel (8.760 Werte).</summary>
        private static double[] ViertelstundenZuStundenMittel(double[] viertel)
        {
            double[] stunden = new double[8760];
            for (int h = 0; h < 8760 && h * 4 + 3 < viertel.Length; h++)
                stunden[h] = (viertel[h * 4] + viertel[h * 4 + 1] +
                              viertel[h * 4 + 2] + viertel[h * 4 + 3]) / 4.0;
            return stunden;
        }

        private void RechneAufschlaege(VariantenDaten v, WirtschaftlichkeitParameter p,
                                       ProjektEingabe e)
        {
            // Netzbezug in der Fassung, die auch die Kosten getragen hat.
            double netzbezugMWh = e.StromkostenTarif.HasValue && e.Matrix != null
                ? e.Matrix.BezugGesamtMWh
                : (v.Ergebnis.Energiebedarf != null ? v.Ergebnis.Energiebedarf.Stromrestbedarf : 0);

            if (!p.AufschlaegeAnwenden)
            {
                // Der Widerspruch aus E4/E5 wird gemeldet, nicht verschwiegen.
                if (e.StromsteuerEntlastungJahr1 > 0)
                    Melde(e, "Hinweis: Die Stromsteuer-Entlastung nach § 9b wird gutgeschrieben, " +
                             "obwohl die Stromsteuer im Bezugspreis nicht angesetzt ist (Schalter " +
                             "„Aufschläge in der Wirtschaftlichkeit berücksichtigen\" aus). Der " +
                             "Kapitalwert enthält damit eine Entlastung ohne die zugehörige Belastung.");
                return;
            }

            if (netzbezugMWh <= 0)
            {
                Melde(e, "Aufschläge sollen berücksichtigt werden, es gibt aber keinen " +
                         "Netzbezug (Jahressaldo ≤ 0) — kein Aufschlagsbetrag.");
                return;
            }

            StromAufschlagModel m = null;
            try { m = new StromAufschlagCtrl().ReadStrom(v.IdProjekt); }
            catch { }
            if (m == null || !m.AusDatenbank)
            {
                Melde(e, "Aufschläge sollen berücksichtigt werden, dem Projekt ist aber kein " +
                         "Strom-Energieträger zugeordnet — kein Aufschlagsbetrag.");
                return;
            }

            double ctKwh;
            try { ctKwh = StromAufschlagCtrl.AlsAufschlagssatz(m).WirksamCtKwh; }
            catch { return; }
            if (ctKwh == 0)
            {
                Melde(e, "Aufschläge sollen berücksichtigt werden, der wirksame Aufschlag ist " +
                         "aber 0 ct/kWh (alle Komponenten inaktiv bzw. Gesamtwert 0).");
                return;
            }

            double betrag = netzbezugMWh * 1000.0 * ctKwh / 100.0;
            e.AufschlagBetrag = betrag;
            if (e.Energie.HasValue) e.Energie = e.Energie.Value + betrag;

            System.Globalization.CultureInfo k = BerichtTexte.Kultur;
            string zerlegung = string.Equals(m.Modus, DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT,
                                             StringComparison.Ordinal)
                ? "Gesamtwert " + m.Override.ToString("N3", k) + " ct/kWh"
                : "Netzentgelt " + Komponente(m.Netzentgelt, m.Netzentgelt_Aktiv, k) +
                  " + Umlagen " + Komponente(m.Umlagen, m.Umlagen_Aktiv, k) +
                  " + Stromsteuer " + Komponente(m.Stromsteuer, m.Stromsteuer_Aktiv, k) +
                  " + Konzession " + Komponente(m.Konzession, m.Konzession_Aktiv, k) +
                  " + Vertrieb " + Komponente(m.Vertrieb, m.Vertrieb_Aktiv, k);

            Melde(e, "Aufschläge berücksichtigt: " + ctKwh.ToString("N3", k) + " ct/kWh (" +
                     zerlegung + ") auf " + netzbezugMWh.ToString("N1", k) + " MWh Netzbezug = " +
                     betrag.ToString("N2", k) + " €/a.");

            if (e.StromsteuerEntlastungJahr1 > 0 && m.Stromsteuer_Aktiv)
                Melde(e, "Stromsteuer: Belastung " + m.Stromsteuer.ToString("N3", k) +
                         " ct/kWh im Bezugspreis und Entlastung nach § 9b als Gutschrift — " +
                         "kein Doppelansatz, sondern die zwei Seiten derselben Vorschrift.");
        }

        /// <summary>Eine Aufschlagskomponente als Text; inaktive werden als solche benannt.</summary>
        private static string Komponente(double wert, bool aktiv, System.Globalization.CultureInfo k)
        {
            return aktiv ? wert.ToString("N3", k) : "0 (inaktiv)";
        }

        /// <summary>Hängt eine Meldung an den Hinweis an, ohne vorhandene zu überschreiben.</summary>
        private static void Melde(ProjektEingabe e, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            e.Hinweis = string.IsNullOrEmpty(e.Hinweis) ? text : e.Hinweis + " | " + text;
        }

        /// <summary>
        /// KWKG-Bonusreihe nach KWKG 2025 (Phase 9, Konzept Kap. 8): Bonus [ct/kWh]
        /// auf KWK-Eigenstrom/-Einspeisung (W3-Split), je Kalenderjahr begrenzt durch
        /// die DEGRESSIVE Vbh-Staffel (§ 8, Katalog Tab_Gesetzesparameter; Override über
        /// den Parameter-Deckel), kumuliert bis zum Vbh-Kontingent (30.000 Vbh).
        /// Vorab die Förderfähigkeits-Prüfkette: Fristenlogik § 6 (Stichtag
        /// 31.12.2026 + 4 Jahre Realisierung), Ausschreibungsgrenze <b>je Anlage</b>
        /// (§ 8a KWKG / KWKAusV) und Heizöl-Ausschluss für Neuanlagen — Verstoß ⇒
        /// Bonus = 0 mit Hinweis.
        /// Negativpreis-Abschlag (§ 7 Abs. 5) als %-Näherung auf die vergüteten Vbh;
        /// die abgeschlagenen Stunden verbrauchen das Kontingent nicht.
        ///
        /// <para><b>ETAPPE E2 (L6): erreichte Vbh = ELEKTRISCHE Vollbenutzungsstunden</b>
        /// (<see cref="VbhElektrisch"/>), leistungsgewichtet über alle Module. Bis dahin
        /// stand hier <c>Betriebsstunden_Gesamt</c> — die Summe THERMISCHER Vbh, die
        /// 8.760 h überschreiten kann und den Zuschlag bei Kaskaden zu hoch ansetzte. Die
        /// Rechnung bleibt projektweit; modulscharf wird sie in Etappe E6.</para>
        ///
        /// <para><b>NACHTRAG ZU E2 (19.08.2026): Ausschreibungsgrenze je ANLAGE.</b>
        /// Anlagen über der Grenze verlieren ihren Zuschlaganteil, die übrigen behalten
        /// ihn; die projektweiten Bezugsgrößen werden dafür bereinigt
        /// (<see cref="Anlagenauswahl"/>). Vorher fiel der Zuschlag ganz weg, sobald die
        /// PROJEKTSUMME die Grenze überschritt.</para>
        ///
        /// <para><b>NACHTRAG 2 ZU E2 (19.08.2026): Heizöl-Ausschluss je ANLAGE.</b> Derselbe
        /// Weg für den zweiten Ausschlussgrund: Ein Öl-BHKW verliert seinen Zuschlaganteil,
        /// ein daneben stehendes Gas-BHKW behält ihn. Vorher genügte EINE Öl-Zeile in
        /// <c>Tab_BHKW</c>, um dem ganzen Projekt den Zuschlag zu nehmen — auch dann, wenn zu
        /// dieser Gerätezeile nie eine Anlagenzeile entstand. Beide Ausschlussgründe laufen
        /// jetzt durch <see cref="Anlagenauswahl"/> und kürzen die Bezugsgrößen zusammen um
        /// <b>jede ausgeschlossene Anlage genau einmal</b>, auch wenn beide Gründe auf
        /// dieselbe Anlage zutreffen.</para>
        ///
        /// <para><b>ETAPPE E6 (19.08.2026): eine Reihe JE ANLAGE, jahresweise summiert.</b>
        /// Zuschlagssatz, Vollbenutzungsstunden, Jahresdeckel, Kontingent, Stichtag und
        /// Inbetriebnahme gelten ab hier je Modul; die Reihen werden Jahr für Jahr addiert.
        /// Damit sind die Restbefunde 1 bis 4 der „vier Grenzen der Zwischenlösung" aus dem
        /// E2-Nachtrag aufgelöst. <b>Ergebnisneutral für Einmodulprojekte</b> — bei genau
        /// einer Anlage ist die Summe über eine Reihe die Reihe selbst, und jede
        /// Anlagenangabe fällt auf den Projektwert zurück. Bei mehreren Anlagen ändert sich
        /// das Ergebnis <b>gewollt</b>: Der Deckel greift dann je Anlage statt über eine
        /// gemeinsame, leistungsgewichtete Vbh-Zahl.</para>
        ///
        /// <para><b>Der Weg ohne zuordenbare Anlagenzeilen bleibt unverändert projektweit</b>
        /// (<see cref="ReiheProjektweit"/>) — er ist derselbe Code wie vor E6.</para>
        /// </summary>
        /// <param name="nachweise">ETAPPE E7: Wird je gerechnetem Modul um eine Zeile
        /// ergänzt (Satz, Vbh, Deckel, Kontingent, Herleitung nach § 7). Nur der Weg je
        /// Anlage füllt sie — der projektweite Ersatzweg kennt keine Module. <c>null</c>
        /// ist erlaubt.</param>
        private double[] BaueKwkgReihe(VariantenDaten v, WirtschaftlichkeitParameter p,
                                       StromMatrix matrix, List<KwkgModulNachweis> nachweise,
                                       out double jahr1, out string hinweis)
        {
            jahr1 = 0;
            hinweis = null;
            var hinweise = new List<string>();   // Meldungen kombinieren, nie überschreiben

            // ---------- ETAPPE K6: Eigenstrom-Tatbestand § 6 Abs. 3 (HF6) ----------
            // Nach § 7 Abs. 2 gibt es den Zuschlag auf SELBST GENUTZTEN Strom nicht
            // generell, sondern nur in den drei Tatbeständen des § 6 Abs. 3. Bis K6
            // rechnete der projektweite Weg den eingetragenen Satz ungeprüft.
            //
            // ERGEBNISNEUTRAL FÜR DEN BESTAND, und das ist der Grund für die drei
            // Zweige: Ein Bestandsprojekt hat die Angabe nie gemacht (Spalte NULL) —
            // dort bleibt der Satz stehen und der Hinweis sagt, dass die Voraussetzung
            // ungeprüft ist. Erst die AUSDRÜCKLICHE Wahl „keiner" nimmt den Satz weg.
            double satzEigenProjekt = p.KwkgBonus;
            string tatbestand = (p.KwkgTatbestand ?? "").Trim();
            if (satzEigenProjekt > 0)
            {
                if (string.Equals(tatbestand, DbWerte.KWKG_EIGENFALL_KEINER, StringComparison.Ordinal))
                {
                    satzEigenProjekt = 0;
                    hinweise.Add(MyResource.Resource.WIRT_KWKG_TATBESTAND_KEINER);
                }
                else if (tatbestand.Length == 0)
                    hinweise.Add(MyResource.Resource.WIRT_KWKG_TATBESTAND_OFFEN);
            }

            bool aktiv = satzEigenProjekt > 0 || p.KwkgBonusEinspeisung > 0;
            if (!aktiv || v.Ergebnis == null || v.Ergebnis.BHKW == null)
            {
                if (hinweise.Count > 0) hinweis = string.Join(" | ", hinweise);
                return null;
            }
            double stromMWh = v.Ergebnis.BHKW.Stromproduktion;
            if (stromMWh <= 0) return null;      // kein KWK-Strom -> nichts zu vergüten

            // ETAPPE E2: die maßgebliche Größe der Deckelung. Ist sie nicht bestimmbar,
            // sagt der Hinweis warum — statt still mit 0 weiterzurechnen (Befund D5-Regel).
            // Sie bleibt auch nach E6 die Größe des ERSATZWEGS und die angezeigte Kennzahl.
            string vbhHinweis;
            double vbh = VbhElektrisch(v, out vbhHinweis);
            if (vbh <= 0)
            {
                if (vbhHinweis != null) hinweis = vbhHinweis;
                return null;
            }

            // ---------------- Förderfähigkeit § 6 KWKG 2025 (Kap. 8.2) ----------------
            //
            // ETAPPE E6: Die Prüfung gilt nach § 6 der EINZELNEN Anlage. Solange KEINE
            // Anlage ein eigenes Datum trägt — der Zustand jeder Datenbank vor
            // Migrationsschritt 22 —, ist die Prüfung je Anlage für alle Anlagen dieselbe
            // wie die des Projekts. Dann läuft bewusst der BESTANDSBLOCK weiter: gleiche
            // Bedingung, gleicher früher Ausstieg, gleicher Meldungstext. Trägt dagegen
            // mindestens eine Anlage ein eigenes Datum, entscheidet die Prüfung je Anlage
            // in Anlagenauswahl, und eine ausgefallene Anlage reißt die übrigen nicht mit.
            List<BhkwAnlage> anlagen = BhkwAnlagen(v.IdProjekt);
            bool eigeneFristdaten = false;
            foreach (BhkwAnlage a in anlagen)
                if (a.Stichtag.HasValue || a.Inbetriebnahme.HasValue) { eigeneFristdaten = true; break; }

            if (!eigeneFristdaten)
            {
                if (p.KwkgStichtag.HasValue)
                {
                    if (p.KwkgStichtag.Value.Date > KWKG_STICHTAG_ENDE)
                    {
                        hinweis = "KWKG: Bestellung/Genehmigung nach dem 31.12.2026 — nach geltendem " +
                                  "Recht nicht förderfähig (Regulierungsrisiko Novelle); Bonus = 0.";
                        return null;
                    }
                    // Realisierungsfrist: Dauerbetrieb bis zum ABLAUF des 4. Jahres nach
                    // dem Stichtag (§ 6, Pfad 2 — für den Bestellungs-Pfad 3 großzügig
                    // um maximal ein Jahr; im Konzept 8.5 als Näherung dokumentiert).
                    DateTime fristende = new DateTime(
                        p.KwkgStichtag.Value.Year + KWKG_REALISIERUNG_JAHRE, 12, 31);
                    if (p.KwkgInbetriebnahme.HasValue && p.KwkgInbetriebnahme.Value.Date > fristende)
                    {
                        hinweis = "KWKG: Inbetriebnahme nach Ablauf des " + KWKG_REALISIERUNG_JAHRE +
                                  ". Jahres nach dem Stichtag (§ 6 Realisierungsfrist, bis " +
                                  fristende.ToString("dd.MM.yyyy") + "); Bonus = 0.";
                        return null;
                    }
                }
                else
                    hinweise.Add("KWKG: kein Bestell-/Genehmigungsdatum hinterlegt — " +
                                 "Förderfähigkeit ungeprüft (§ 6 KWKG 2025, Stichtag 31.12.2026).");
            }

            int foerderbeginn = Foerderbeginn(p);

            // ETAPPE K6: das Vbh-Kontingent — Override, sonst nach § 8 aus der
            // Anlagenart abgeleitet. Der Bestand trägt einen Override und bleibt
            // dadurch unverändert (Begründung an KontingentDesProjekts).
            double kontingentProjekt = KontingentDesProjekts(p, foerderbeginn, hinweise);

            // ------- Guard Kap. 8.4: Ausschreibungsgrenze und Heizöl, JE ANLAGE -------
            // NACHTRAG ZU E2 (Nutzerentscheidung 19.08.2026): Das Gesetz stellt auf die
            // EINZELNE KWK-Anlage ab — oberhalb der Grenze gibt es den Zuschlag nur über
            // eine Ausschreibung (§ 8a KWKG / KWKAusV), und dieser Weg ist hier nicht
            // bedienbar. Zwei Module zu je 300 kW sind damit ZWEI förderfähige Anlagen,
            // keine nicht förderfähige 600-kW-Anlage. Bis hierher prüfte der Guard die
            // PROJEKTSUMME und nahm einer Kaskade den Zuschlag vollständig.
            //
            // NACHTRAG 2 (19.08.2026): Der Heizöl-Ausschluss läuft denselben Weg. Er gilt
            // ebenfalls je Anlage — ein Öl-BHKW ist nicht zuschlagsberechtigt, ein daneben
            // stehendes Gas-BHKW schon. Der Ausschluss greift unverändert NUR für erkennbare
            // Neuanlagen (IBN ≥ 2025); Bestandsanlagen rechnen mit ihrem historischen Satz
            // weiter (Kap. 8.5.3), und die Rechtsgrundlage bleibt die Sekundärquelle aus
            // Grundlagen_KWKG_Energiesteuer_Stromsteuer.md Abschnitt 6 Punkt 3 — daran ändert
            // dieser Nachtrag nichts, er korrigiert ausschließlich den BEZUG.
            //
            // ETAPPE E6: Dieselbe Kette entscheidet jetzt auch über Stichtag und
            // Realisierungsfrist je Anlage, und die Ausschreibungsgrenze wird mit dem
            // Inbetriebnahmejahr DIESER Anlage im Katalog nachgeschlagen.
            double grenzeKW = AusschreibungsgrenzeKW(foerderbeginn);
            bool oelAusschluss = p.KwkgInbetriebnahme.HasValue
                              && p.KwkgInbetriebnahme.Value.Year >= 2025;
            KwkgAnlagenauswahl auswahl = Anlagenauswahl(v, p, foerderbeginn, grenzeKW);

            // ---------------- ETAPPE B3 Paket b: der EINE Netto-Ort ----------------
            //
            // § 4.3 des Konzepts: Der KWK-Zuschlag bemisst sich auf die NETTOstrom-
            // erzeugung — Erzeugung minus Hilfsstrom. Gebildet wird die Minderung genau
            // hier, EINMAL, und von hier aus in JEDEN Mengenpfad gereicht:
            //   (a) die Anteilsbildung je Anlage in ReiheJeAnlage,
            //   (b) die beiden Splitmengen aus der StromMatrix,
            //   (c) den projektweiten Ersatzweg.
            // Die StromMatrix selbst bleibt unangetastet: Ihre Stundenreihen sind die
            // BRUTTO-Welt und speisen außer dem Zuschlag auch Bezugskosten,
            // KWK-Einspeiseerlös und die Stromsteuer — eine Minderung an der Quelle
            // würde in all diese Rechnungen durchschlagen, wo sie nicht hingehört.
            //
            // ERGEBNISNEUTRAL: Ohne gepflegten Anteil ist GesamtMWh = 0, und jede
            // Netto-Größe unten ist zeilengleich ihrer Brutto-Vorgängerin.
            HilfsstromSatz hilfsstrom = HilfsstromDesProjekts(v);
            if (hilfsstrom.Gepflegt && !hilfsstrom.Zuordenbar)
                hinweise.Add(T("WIRT_KWKG_HILFSSTROM_UNKLAR",
                    "KWKG: Für mindestens eine Anlage ist ein Hilfsenergieanteil gepflegt, " +
                    "Anlagen- und Ergebniszeilen lassen sich aber nicht zuordnen — der " +
                    "Zuschlag rechnet mit der Bruttostromerzeugung."));

            // Der Split des Projekts, um den Hilfsstrom gemindert (Reihenfolge und
            // Begründung: HilfsstromRechner.NettoSplit).
            bool mitMatrix = matrix != null &&
                             matrix.KwkEigenGesamtMWh + matrix.KwkEinspeisungGesamtMWh > 0;
            double eigenNettoMWh = matrix != null ? matrix.KwkEigenGesamtMWh : 0;
            double einspNettoMWh = matrix != null ? matrix.KwkEinspeisungGesamtMWh : 0;
            HilfsstromRechner.NettoSplit(hilfsstrom.GesamtMWh, ref eigenNettoMWh, ref einspNettoMWh);

            if (!auswahl.Bestimmbar)
            {
                // Ohne zuordenbare Anlagenzeilen bleiben nur die Projektsumme und die
                // Gerätezeilen — der Weg bis zu diesem Nachtrag. Er ist konservativ und
                // wird als Ersatz ausgewiesen.
                if (auswahl.PelGesamtKW > grenzeKW)
                {
                    hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_LEISTUNG_JE_ANLAGE_UNKLAR,
                                               auswahl.PelGesamtKW.ToString("N0"), grenzeKW.ToString("N0")));
                    hinweis = string.Join(" | ", hinweise);
                    return null;
                }

                if (!_oelCache.ContainsKey(v.IdProjekt)) _oelCache[v.IdProjekt] = BhkwMitHeizoel(v.IdProjekt);
                bool oelGeraetezeile = _oelCache[v.IdProjekt];
                if (oelAusschluss && oelGeraetezeile)
                {
                    hinweise.Add(MyResource.Resource.WIRT_KWKG_HEIZOEL_JE_ANLAGE_UNKLAR);
                    hinweis = string.Join(" | ", hinweise);
                    return null;
                }
                if (!p.KwkgInbetriebnahme.HasValue && oelGeraetezeile)
                    hinweise.Add(MyResource.Resource.WIRT_KWKG_HEIZOEL_OHNE_IBN_UNKLAR);

                // ERSATZWEG: die projektweite Rechnung, Zeile für Zeile der Stand vor E6.
                //
                // ETAPPE B3 Paket b: Er zieht dieselbe Minderung wie der Weg je Anlage —
                // in der Sache greift sie hier allerdings nie, weil die fehlgeschlagene
                // Zuordnung, die diesen Zweig überhaupt auslöst, DIESELBE ist, an der
                // auch HilfsstromDesProjekts scheitert (GesamtMWh = 0). Die Netto-Größen
                // stehen trotzdem in der Signatur: Der Ersatzweg soll nicht der eine
                // Pfad sein, der beim nächsten Ausbau brutto rechnet.
                double stromNettoMWh = Math.Max(0, stromMWh - hilfsstrom.GesamtMWh);
                double[] ersatz = ReiheProjektweit(p, mitMatrix, eigenNettoMWh, einspNettoMWh,
                                                   stromNettoMWh, vbh, foerderbeginn,
                                                   satzEigenProjekt, kontingentProjekt, out jahr1);
                if (hinweise.Count > 0) hinweis = string.Join(" | ", hinweise);
                return ersatz;
            }

            if (auswahl.AnzahlAusgeschlossen > 0 && auswahl.PelFoerderfaehigKW <= 0)
            {
                // Keine Anlage bleibt übrig — Ergebnis wie bisher (Bonus = 0), aber mit den
                // Anlagen und dem jeweiligen Grund im Klartext. Drei Fälle, damit kein
                // Meldungstext eine leere Aufzählung führt.
                //
                // Die Bedingung verlangt zusätzlich einen ECHTEN Ausschluss: Ohne ihn hieße
                // eine Restleistung von 0 nur, dass in den Anlagenzeilen keine elektrische
                // Nennleistung steht. Der Altstand meldete dafür „jede Anlage über der
                // Ausschreibungsgrenze" mit LEERER Aufzählung; jetzt läuft dieser Fall
                // unbereinigt weiter — wie vor der Prüfung je Anlage, mit der Leistung aus
                // der Gerätesumme, die VbhElektrisch ohnehin schon verwendet hat.
                Ausschlussmeldungen(auswahl, grenzeKW, hinweise);
                hinweis = string.Join(" | ", hinweise);
                return null;
            }

            // Teilausschluss: je Grund eine Meldung, alle nennen die verbleibende
            // Leistung — das ist dieselbe Zahl, weil sie nach ALLEN Filtern übrig ist.
            if (auswahl.UeberGrenze.Count > 0)
                hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_ANLAGE_UEBER_GRENZE,
                                           grenzeKW.ToString("N0"), auswahl.Klartext(auswahl.UeberGrenze),
                                           auswahl.PelFoerderfaehigKW.ToString("N0")));
            if (auswahl.NurHeizoel.Count > 0)
                hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_ANLAGE_HEIZOEL,
                                           auswahl.Klartext(auswahl.NurHeizoel),
                                           auswahl.PelFoerderfaehigKW.ToString("N0")));
            foreach (string s in auswahl.Fristmeldungen) hinweise.Add(s);

            // Öl-Anlagen ohne Inbetriebnahmedatum werden NICHT ausgeschlossen (der
            // Ausschluss gilt nur für Neuanlagen) — der Anwender muss aber wissen, dass
            // das Ergebnis am fehlenden Datum hängt.
            if (auswahl.OelOhneIbn.Count > 0)
                hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_HEIZOEL_OHNE_IBN,
                                           auswahl.Klartext(auswahl.OelOhneIbn)));

            double[] reihe = ReiheJeAnlage(v, p, mitMatrix, eigenNettoMWh, einspNettoMWh,
                                           hilfsstrom, auswahl, foerderbeginn, hinweise,
                                           nachweise, satzEigenProjekt, tatbestand,
                                           kontingentProjekt, out jahr1);
            if (hinweise.Count > 0) hinweis = string.Join(" | ", hinweise);
            return reihe;
        }

        /// <summary>Die drei Meldungen für „keine Anlage bleibt übrig" — unverändert aus
        /// dem E2-Nachtrag, nur ausgelagert.</summary>
        private static void Ausschlussmeldungen(KwkgAnlagenauswahl auswahl, double grenzeKW,
                                                List<string> hinweise)
        {
            if (auswahl.UeberGrenze.Count > 0 && auswahl.NurHeizoel.Count > 0)
                hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_KEINE_FOERDERFAEHIG,
                                           grenzeKW.ToString("N0"), auswahl.Klartext(auswahl.UeberGrenze),
                                           auswahl.Klartext(auswahl.NurHeizoel)));
            else if (auswahl.NurHeizoel.Count > 0)
                hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_ALLE_HEIZOEL,
                                           auswahl.Klartext(auswahl.NurHeizoel)));
            else if (auswahl.UeberGrenze.Count > 0)
                hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_ALLE_UEBER_GRENZE,
                                           grenzeKW.ToString("N0"), auswahl.Klartext(auswahl.UeberGrenze)));
            foreach (string s in auswahl.Fristmeldungen) hinweise.Add(s);
        }

        /// <summary>
        /// Die <b>projektweite</b> Zuschlagsreihe — der Rechenweg vor Etappe E6, Zeile für
        /// Zeile unverändert. Er greift nur noch als ERSATZWEG, wenn sich Anlagen- und
        /// Ergebnismodulzeilen nicht paaren lassen
        /// (<see cref="KwkgAnlagenauswahl.Bestimmbar"/> = false): kein Anlagenbestand,
        /// keine Modulzeilen, oder Namen und Anzahl passen nicht zusammen. Dann ist die
        /// Projektsumme die einzige verfügbare Aussage.
        /// </summary>
        /// <param name="satzEigenProjekt">ETAPPE K6: der Eigenstrom-Satz NACH der Prüfung
        /// des § 6 Abs. 3 — identisch mit <c>p.KwkgBonus</c>, außer der Anwender hat den
        /// Tatbestand ausdrücklich auf „keiner" gesetzt (dann 0).</param>
        /// <param name="kontingentProjekt">ETAPPE K6: das Vbh-Kontingent — der Override
        /// aus <c>p.KwkgVbhKontingent</c>, sonst der nach § 8 abgeleitete Wert.</param>
        /// <param name="mitMatrix">true = die Stundenreihen liefern einen Eigen-/
        /// Einspeise-Split; false = Fallback „alles ist Eigenverbrauch" (W2).</param>
        /// <param name="eigenNettoMWh">ETAPPE B3 Paket b: KWK-Eigenverbrauch des
        /// Projekts NACH Abzug des Hilfsstroms [MWh/a].</param>
        /// <param name="einspNettoMWh">Ebenso die KWK-Einspeisung [MWh/a].</param>
        /// <param name="stromNettoMWh">Ebenso die Gesamterzeugung [MWh/a] — die
        /// Bezugsgröße des Fallbacks ohne Stundenreihen.</param>
        private double[] ReiheProjektweit(WirtschaftlichkeitParameter p, bool mitMatrix,
                                          double eigenNettoMWh, double einspNettoMWh,
                                          double stromNettoMWh, double vbh, int foerderbeginn,
                                          double satzEigenProjekt, double kontingentProjekt,
                                          out double jahr1)
        {
            jahr1 = 0;

            // ---------------- Bonus bei voller Vergütung [€/a] ----------------
            //  - W3-Split: getrennte Sätze auf KWK-Eigenstrom und -Einspeisung.
            //  - Fallback ohne Stundenreihen: Eigenstrom-Satz auf die Gesamtmenge (W2).
            //  - B3b: beide Mengen sind NETTO (Erzeugung minus Hilfsstrom, § 4.3);
            //    ohne gepflegten Anteil sind sie zeilengleich den Bruttomengen.
            double bonusVoll;
            if (mitMatrix)
                bonusVoll = eigenNettoMWh * 1000.0 * (satzEigenProjekt / 100.0)
                          + einspNettoMWh * 1000.0 * (p.KwkgBonusEinspeisung / 100.0);
            else
                bonusVoll = stromNettoMWh * 1000.0 * (satzEigenProjekt / 100.0);
            if (bonusVoll <= 0) return null;

            if (_staffelCache == null) _staffelCache = LadeKwkgStaffel();
            List<KeyValuePair<int, double>> staffel = _staffelCache;
            double abschlag = Math.Min(100.0, Math.Max(0.0, p.KwkgAbschlagNegativ)) / 100.0;

            int T = Math.Max(1, p.Betrachtungszeitraum);
            double[] reihe = new double[T + 1];
            double rest = kontingentProjekt;
            for (int t = 1; t <= T; t++)
            {
                if (rest <= 0) break;
                double deckel = p.KwkgVbhJahresdeckel > 0
                    ? p.KwkgVbhJahresdeckel                              // fester Override
                    : StaffelDeckel(staffel, foerderbeginn + t - 1);     // KWKG-2025-Staffel
                double verguetet = Math.Min(vbh, Math.Min(deckel, rest)) * (1.0 - abschlag);
                reihe[t] = bonusVoll * (verguetet / vbh);
                rest -= verguetet;   // Negativpreis-Stunden verbrauchen das Kontingent nicht
            }
            jahr1 = reihe[1];
            return reihe;
        }

        /// <summary>
        /// ETAPPE E6 — <b>eine Zuschlagsreihe je förderfähiger Anlage, jahresweise
        /// summiert</b>. Das ist der Kern der Etappe.
        ///
        /// <para><b>Je Anlage eigen:</b> Zuschlagssatz (Überschreibwert, sonst Projektsatz),
        /// elektrische Vollbenutzungsstunden (Modulzeile aus E2, sonst Strom × 1000 / P_el
        /// dieser Anlage), Jahresdeckel (Anlagen-Override, sonst Projekt-Override, sonst die
        /// Staffel des § 8 Abs. 4 ab dem Inbetriebnahmejahr DIESER Anlage) und Kontingent
        /// (Anlagenwert, sonst Projektwert). Der Negativpreis-Abschlag bleibt projektweit —
        /// er hängt am Strommarkt, nicht an der Anlage.</para>
        ///
        /// <para><b>Die eine dokumentierte Näherung: der Split Eigenstrom/Einspeisung.</b>
        /// <see cref="StromMatrix"/> liefert ihn nur für das GANZE Projekt; modulscharfe
        /// Stundenreihen gibt es im Modell nicht. Er wird deshalb im Verhältnis der
        /// Stromerzeugung auf die Anlagen verteilt. Bei genau einer Anlage ist das exakt,
        /// bei mehreren eine Annahme — dieselbe, die der E2-Nachtrag für die Kürzung schon
        /// getroffen hat (dort als Grenze 3 der Zwischenlösung benannt). Fehlt die
        /// Strombedarfsreihe, gilt wie bisher „alles ist Eigenverbrauch", und
        /// <see cref="StromMatrix.StrombedarfFehlt"/> weist das aus.</para>
        ///
        /// <para><b>Warum das für ein Einmodulprojekt dieselbe Zahl liefert wie vorher:</b>
        /// Die Summe über eine Reihe ist die Reihe; der Anteil dieser einen Anlage am Split
        /// ist 1; ihre Vbh sind die leistungsgewichteten Vbh des Projekts (bei einem Modul
        /// identisch); Satz, Deckel und Kontingent fallen auf den Projektwert zurück.</para>
        ///
        /// <para><b>ETAPPE B3 Paket b (§ 4.3): die Mengen sind NETTO.</b> Jede Anlage
        /// bringt ihre Erzeugung abzüglich ihres Hilfsstroms in die Anteilsbildung ein,
        /// und der projektweite Split ist bereits um den Gesamthilfsstrom gemindert
        /// (<see cref="HilfsstromRechner"/>). Zähler und Nenner des Anteils stammen aus
        /// derselben Netto-Reihe — die Anteile summieren sich deshalb weiter zu eins,
        /// und die verteilten Mengen zur Nettoerzeugung. Ohne gepflegten Anteil ist
        /// jede Netto-Größe zeilengleich ihrer Brutto-Vorgängerin.</para>
        ///
        /// <para><b>Die Vollbenutzungsstunden bleiben BRUTTO</b>
        /// (<see cref="VbhDerAnlage"/>). Sie sind eine Auslegungsgröße der Anlage und
        /// stehen in <c>reihe[t]</c> in Zähler UND Nenner desselben Bruchs; der
        /// Hilfsstrom wirkt deshalb ausschließlich über <c>bonusVoll</c>, also als
        /// proportionale Minderung des Zuschlags — genau das, was § 4.3 verlangt.</para>
        /// </summary>
        /// <param name="satzEigenProjekt">ETAPPE K6: der Eigenstrom-Satz des PROJEKTS nach
        /// der Prüfung des § 6 Abs. 3 — er greift für jede Anlage ohne eigenen Satz.</param>
        /// <param name="tatbestandProjekt">ETAPPE B3 Paket b: der Tatbestand des § 6
        /// Abs. 3 aus dem Projekt (<c>DbWerte.KWKG_EIGENFALL_*</c>, leer = keiner) —
        /// Rückfall der Prüfung je Anlage.</param>
        /// <param name="kontingentProjekt">ETAPPE K6: das Vbh-Kontingent des PROJEKTS —
        /// Override, sonst nach § 8 abgeleitet; Rückfall für jede Anlage ohne eigenen
        /// Wert.</param>
        /// <param name="mitMatrix">true = die Stundenreihen liefern einen Eigen-/
        /// Einspeise-Split; false = Fallback „alles ist Eigenverbrauch" (W2).</param>
        /// <param name="eigenNettoMWh">KWK-Eigenverbrauch des Projekts nach Abzug des
        /// Hilfsstroms [MWh/a].</param>
        /// <param name="einspNettoMWh">Ebenso die KWK-Einspeisung [MWh/a].</param>
        /// <param name="hilfsstrom">Der Hilfsstrom je Anlage — indexgleich zu
        /// <see cref="KwkgAnlagenauswahl.Anlagen"/>.</param>
        private double[] ReiheJeAnlage(VariantenDaten v, WirtschaftlichkeitParameter p,
                                       bool mitMatrix, double eigenNettoMWh, double einspNettoMWh,
                                       HilfsstromSatz hilfsstrom, KwkgAnlagenauswahl auswahl,
                                       int foerderbeginn, List<string> hinweise,
                                       List<KwkgModulNachweis> nachweise,
                                       double satzEigenProjekt, string tatbestandProjekt,
                                       double kontingentProjekt, out double jahr1)
        {
            jahr1 = 0;
            if (_staffelCache == null) _staffelCache = LadeKwkgStaffel();
            List<KeyValuePair<int, double>> staffel = _staffelCache;
            double abschlag = Math.Min(100.0, Math.Max(0.0, p.KwkgAbschlagNegativ)) / 100.0;
            int T = Math.Max(1, p.Betrachtungszeitraum);

            // ETAPPE B3 Paket b — die NETTO-Erzeugung je Anlage und ihre Summe, in EINEM
            // Durchlauf über ALLE Anlagen (auch die nicht förderfähigen): Der Nenner der
            // Anteilsbildung muss dieselbe Größe sein wie der Zähler, sonst summierten
            // sich die verteilten Mengen nicht mehr zum Split. Die Klemme bei 0 fängt den
            // absurden Fall ab, dass ein gepflegter Anteil mehr Hilfsstrom fordert, als
            // die Anlage erzeugt — eine negative Menge darf hier nie entstehen.
            var stromNettoJeAnlage = new double[auswahl.Anlagen.Count];
            double stromSumme = 0;
            for (int i = 0; i < auswahl.Anlagen.Count; i++)
            {
                double hs = hilfsstrom != null && i < hilfsstrom.JeAnlage.Length
                          ? hilfsstrom.JeAnlage[i] : 0;
                stromNettoJeAnlage[i] = Math.Max(0, StromVon(auswahl.Module[i]) - hs);
                stromSumme += stromNettoJeAnlage[i];
            }

            var reihe = new double[T + 1];
            var beschreibung = new List<string>();
            bool etwasGerechnet = false;

            for (int i = 0; i < auswahl.Anlagen.Count; i++)
            {
                if (!auswahl.Foerderfaehig[i]) continue;
                BhkwAnlage a = auswahl.Anlagen[i];
                double stromBruttoMWh = StromVon(auswahl.Module[i]);
                double hilfsstromMWh = hilfsstrom != null && i < hilfsstrom.JeAnlage.Length
                                     ? hilfsstrom.JeAnlage[i] : 0;
                double stromAnlageMWh = stromNettoJeAnlage[i];
                if (stromAnlageMWh <= 0) continue;

                double anteil = stromSumme > 0 ? stromAnlageMWh / stromSumme : 0;
                double eigenMWh = mitMatrix ? eigenNettoMWh * anteil : stromAnlageMWh;
                double einspMWh = mitMatrix ? einspNettoMWh * anteil : 0;

                double satzEigen = SatzEigenDerAnlage(a, satzEigenProjekt, tatbestandProjekt,
                                                      hinweise);
                double satzEinsp = a.SatzEinspCt ?? p.KwkgBonusEinspeisung;
                double bonusVoll = eigenMWh * 1000.0 * (satzEigen / 100.0)
                                 + einspMWh * 1000.0 * (satzEinsp / 100.0);
                if (bonusVoll <= 0) continue;

                double vbhAnlage = VbhDerAnlage(a, auswahl.Module[i], stromAnlageMWh);
                if (vbhAnlage <= 0) continue;

                int beginn = a.Inbetriebnahme.HasValue ? a.Inbetriebnahme.Value.Year : foerderbeginn;
                double kontingent = a.VbhKontingent.HasValue && a.VbhKontingent.Value > 0
                                  ? a.VbhKontingent.Value : kontingentProjekt;   // K6
                double deckelFest = a.VbhDeckel.HasValue && a.VbhDeckel.Value > 0
                                  ? a.VbhDeckel.Value : p.KwkgVbhJahresdeckel;

                double rest = kontingent;
                double jahr1Modul = 0;              // E7: Nachweis, kein Rechenweg
                int erschoepftAb = 0;
                for (int t = 1; t <= T; t++)
                {
                    if (rest <= 0) { if (erschoepftAb == 0) erschoepftAb = t; break; }
                    double deckel = deckelFest > 0 ? deckelFest
                                                   : StaffelDeckel(staffel, beginn + t - 1);
                    double verguetet = Math.Min(vbhAnlage, Math.Min(deckel, rest)) * (1.0 - abschlag);
                    reihe[t] += bonusVoll * (verguetet / vbhAnlage);
                    if (t == 1) jahr1Modul = bonusVoll * (verguetet / vbhAnlage);
                    rest -= verguetet;   // Negativpreis-Stunden verbrauchen das Kontingent nicht
                }
                etwasGerechnet = true;

                // ETAPPE E7 — dieselben Angaben strukturiert, damit der Bericht eine
                // Tabelle je Modul bauen kann statt einer immer längeren Hinweiszeile
                // (Übergabepunkt 1 aus E6). Die Herleitung nach § 7 kommt aus derselben
                // Tranchenrechnung, die auch der Dialog zeigt — sie macht den ANGESETZTEN
                // Satz nachvollziehbar und eine Abweichung vom Katalog sichtbar.
                if (nachweise != null)
                {
                    var n = new KwkgModulNachweis
                    {
                        Bezeichner = a.Bezeichner,
                        PelKW = a.PelKW,
                        VbhElektrisch = vbhAnlage,
                        SatzEigenCt = satzEigen,
                        SatzEinspeisungCt = satzEinsp,
                        SatzAusAnlage = a.SatzEigenCt.HasValue || a.SatzEinspCt.HasValue,
                        KontingentH = kontingent,
                        JahresdeckelH = deckelFest,
                        Foerderbeginn = beginn,
                        Jahr1Eur = jahr1Modul,
                        ErschoepftAbJahr = erschoepftAb,
                        // ETAPPE B3 Paket b: die Mengenherleitung, damit die
                        // Herleitungstafel (BW8) sie fertig vorfindet und niemand sie
                        // nachrechnet.
                        StromBruttoMWh = stromBruttoMWh,
                        HilfsstromMWh = hilfsstromMWh,
                        StromNettoMWh = stromAnlageMWh,
                        EigenMWh = eigenMWh,
                        EinspeisungMWh = einspMWh
                    };
                    try
                    {
                        if (_gesetze == null) _gesetze = new GesetzKatalog();
                        KwkgSatzVorschlag vs = KwkgSatzRechner.Vorschlag(
                            a.PelKW, beginn, a.Anlagenart, a.Eigenfall,
                            (s, j) => _gesetze.WertMitHerkunft(s, j), BerichtTexte.Kultur);
                        n.HerleitungEigen = vs.HerleitungEigen ?? "";
                        n.HerleitungEinspeisung = vs.HerleitungEinspeisung ?? "";
                    }
                    catch { }
                    nachweise.Add(n);
                }

                // Der Bezeichner ist ein Datenwert, die Klammer trägt nur Zahlen und
                // Einheitenzeichen — sie bleibt deshalb im Code (Drei-Schichten-Regel,
                // wie bei KwkgAnlagenauswahl.Klartext).
                beschreibung.Add(a.Bezeichner + " (" + a.PelKW.ToString("N0") + " kW, " +
                                 vbhAnlage.ToString("N0") + " h/a, " +
                                 satzEigen.ToString("N2") + "/" + satzEinsp.ToString("N2") + " ct/kWh, " +
                                 kontingent.ToString("N0") + " h)");
            }

            if (!etwasGerechnet) return null;

            // Die Herleitung je Modul erscheint nur, wenn es überhaupt etwas zu unterscheiden
            // gibt: mehr als eine Anlage oder mindestens eine eigene Angabe. Bei einem
            // Einmodulprojekt ohne eigene Angaben bleibt der Hinweistext unverändert leer —
            // sonst hätte E6 auf jedem Bestandsprojekt eine neue Meldung erzeugt.
            if (beschreibung.Count > 1 || auswahl.MitEigenerAngabe)
                hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_JE_MODUL,
                                           string.Join("; ", beschreibung.ToArray())));

            jahr1 = reihe[1];
            return reihe;
        }

        /// <summary>
        /// ETAPPE B3 Paket b (§ 4.4) — der Eigenstrom-Satz EINER Anlage [ct/kWh] nach der
        /// Prüfung des Tatbestands nach § 6 Abs. 3 KWKG.
        ///
        /// <para><b>Die Lücke, die das schließt.</b> Seit K6 prüft
        /// <see cref="BaueKwkgReihe"/> den Tatbestand — aber nur gegen den PROJEKTsatz.
        /// Trug eine Anlage einen eigenen <c>KWKG_Satz_Eigen</c>, ging dieser Satz an der
        /// Prüfung vorbei und erzeugte einen Eigenverbrauchszuschlag, obwohl kein
        /// Tatbestand vorlag. Genau dieser Weg läuft jetzt durch dieselbe Prüfung, mit
        /// dem Tatbestand DIESER Anlage (<c>Tab_Energieanlagen.KWKG_Eigenstromfall</c>)
        /// und dem Projektwert als Rückfall.</para>
        ///
        /// <para><b>Warum der Anlagenweg strenger ist als der Projektweg.</b> Am Projekt
        /// lässt ein LEERER Tatbestand den Satz stehen und meldet nur „ungeprüft" — das
        /// war die Bedingung, unter der K6 für Bestandsprojekte ergebnisneutral bleiben
        /// konnte (jede Bestandsdatenbank hat die Angabe nie gemacht). An der Anlage gilt
        /// diese Rücksicht nicht: Ein <c>KWKG_Satz_Eigen</c> ist eine ausdrückliche
        /// Eingabe, die es im Bestand nirgends gibt (geprüft: alle Anlagenzeilen NULL).
        /// Wer ihn pflegt, muss auch den Tatbestand nennen, der ihn trägt — <b>ohne
        /// Tatbestand gibt es den Zuschlag nach § 7 Abs. 2 nicht</b> (Etappendefinition:
        /// Ergebniswirkung nur bei gepflegten Anlagenangaben, und dort gewollt).</para>
        ///
        /// <para><b>Anlagen ohne eigenen Satz bleiben unberührt</b> — für sie hat die
        /// Prüfung am Projekt bereits entschieden, und <c>KWKG_Eigenstromfall</c> behält
        /// dort seine E6-Rolle als reiner Steuerwert des Katalogvorschlags.</para>
        /// </summary>
        private static double SatzEigenDerAnlage(BhkwAnlage a, double satzEigenProjekt,
                                                 string tatbestandProjekt, List<string> hinweise)
        {
            if (!a.SatzEigenCt.HasValue) return satzEigenProjekt;   // K6: geprüfter Projektsatz

            double satz = a.SatzEigenCt.Value;
            if (satz <= 0) return satz;                             // nichts zu prüfen

            string fall = (a.Eigenfall ?? "").Trim();
            if (fall.Length == 0) fall = (tatbestandProjekt ?? "").Trim();

            if (fall.Length == 0)
            {
                hinweise.Add(string.Format(T("WIRT_KWKG_TATBESTAND_ANLAGE_OFFEN",
                    "KWKG: „{0}“ trägt einen eigenen Satz auf selbst genutzten Strom, aber " +
                    "keinen Tatbestand nach § 6 Abs. 3 — ohne ihn gibt es den " +
                    "Eigenverbrauchszuschlag nicht (§ 7 Abs. 2); Satz dieser Anlage = 0."),
                    a.Bezeichner));
                return 0;
            }

            if (string.Equals(fall, DbWerte.KWKG_EIGENFALL_KEINER, StringComparison.Ordinal))
            {
                hinweise.Add(string.Format(T("WIRT_KWKG_TATBESTAND_ANLAGE_KEINER",
                    "KWKG: Für „{0}“ ist als Tatbestand nach § 6 Abs. 3 „keiner“ gewählt — " +
                    "der eigene Satz auf selbst genutzten Strom entfällt (§ 7 Abs. 2)."),
                    a.Bezeichner));
                return 0;
            }
            return satz;
        }

        /// <summary>
        /// Elektrische Vollbenutzungsstunden EINER Anlage [h/a] (Etappe E6). Vorrang hat der
        /// beim Lauf berechnete Wert aus <c>Tab_ErgebnisBHKWModul.VbhElektrisch</c>
        /// (Migrationsschritt 18) — er trägt die Leistung, die zum Zeitpunkt des Laufs
        /// installiert war. Fehlt er (Ergebniszeile vor E2), wird er aus der Stromerzeugung
        /// dieser Anlage und ihrer heutigen Nennleistung gebildet — dieselbe Formel, die der
        /// Rechenkern verwendet.
        ///
        /// <para><b>ETAPPE B3 Paket b: BRUTTO.</b> Vollbenutzungsstunden sind eine
        /// Auslegungsgröße — die Zeit, die eine Anlage rechnerisch unter Nennlast lief.
        /// Ein Hilfsstromabzug würde sie zu einer Erlösgröße machen und den Deckel des
        /// § 8 Abs. 4 verschieben, obwohl die Anlage genauso lange gelaufen ist. Der
        /// Hilfsstrom wirkt ausschließlich über die Mengen (siehe
        /// <see cref="ReiheJeAnlage"/>).</para>
        /// </summary>
        private static double VbhDerAnlage(BhkwAnlage a, ErgebnisBHKWModulModel modul, double stromMWh)
        {
            if (modul != null && modul.VbhElektrisch > 0) return modul.VbhElektrisch;
            return a.PelKW > 0 ? stromMWh * 1000.0 / a.PelKW : 0;
        }

        /// <summary>
        /// Erstes Kalenderjahr der jahresscharfen Reihen: das Inbetriebnahmejahr, sonst
        /// das Folgejahr (Planungsfall). Seit Etappe E4 an EINER Stelle — die KWKG-Reihe
        /// und die drei Steuerreihen müssen dasselbe Jahr zugrunde legen, sonst zeigen
        /// zwei Zeilen desselben Ergebnisses verschiedene Rechtsstände.
        /// </summary>
        private static int Foerderbeginn(WirtschaftlichkeitParameter p)
        {
            return p.KwkgInbetriebnahme.HasValue ? p.KwkgInbetriebnahme.Value.Year
                                                 : DateTime.Now.Year + 1;
        }

        // =====================================================================
        // ETAPPE K6 — CO₂-Preispfad (Konzept § 8.3, Entscheidung E5)
        // =====================================================================

        /// <summary>
        /// Die CO₂-Abgabe <b>jahresscharf</b> [€], Index 1…T — Bemessungsmenge mal dem
        /// Preis DIESES Kalenderjahres aus der Katalogklasse CO₂-Preispfad.
        /// <c>null</c> = kein Pfad, weil der Projektwert <c>CO2_Preis</c> als Override
        /// gesetzt ist; dann gilt der Bestandsweg (konstanter Preis, mit p_E
        /// fortgeschrieben).
        ///
        /// <para><b>Die Umkehr der Bedeutung von 0.</b> Bis K6 hieß <c>CO2_Preis = 0</c>
        /// „CO₂-Abgabe aus". Ab K6 heißt es „Pfad aus dem Gesetzeskatalog" — so hat es
        /// das Konzept in § 8.3 entschieden (E5). Für Bestandsprojekte ist das die eine
        /// gewollte Ergebnisänderung dieser Etappe: Sie bekommen eine Abgabe, die sie
        /// vorher nicht hatten. Wer den alten Zustand will, trägt einen Preis ein oder
        /// löscht die Stützstellen der Klasse.</para>
        ///
        /// <para><b>Warum <see cref="Foerderbeginn"/> und nicht das Bilanzjahr.</b> Die
        /// Abgabe ist ein ZAHLUNGSstrom der Betriebsjahre und gehört damit auf dieselbe
        /// Zeitachse wie die KWKG- und die drei Steuerreihen (Regel aus E4: alle
        /// jahresscharfen Reihen legen dasselbe Jahr zugrunde). Das Bilanzjahr aus L12
        /// wählt dagegen eine METHODE der Emissionsbilanz und darf gerade nicht am
        /// Förderbeginn hängen — die beiden Größen beantworten verschiedene Fragen.</para>
        /// </summary>
        private double[] BaueCo2Reihe(WirtschaftlichkeitParameter p, double behgBasisT,
                                      out string hinweis)
        {
            hinweis = null;
            System.Globalization.CultureInfo kultur = BerichtTexte.Kultur;

            if (p.CO2Preis > 0)
            {
                hinweis = string.Format(kultur, MyResource.Resource.WIRT_CO2_KONSTANT,
                                        p.CO2Preis.ToString("N0", kultur));
                return null;
            }

            if (_gesetze == null) _gesetze = new GesetzKatalog();
            int T = Math.Max(1, p.Betrachtungszeitraum);
            int beginn = Foerderbeginn(p);

            var reihe = new double[T + 1];
            var luecken = new List<int>();
            int prognoseAb = 0;
            bool etwas = false;

            for (int t = 1; t <= T; t++)
            {
                int jahr = beginn + t - 1;
                GesetzParameter g = _gesetze.WertMitHerkunft(DbWerte.GESETZ_CO2_PREIS_NEHS, jahr);
                if (g == null || !g.Wert.HasValue) { luecken.Add(jahr); continue; }
                if (prognoseAb == 0 &&
                    string.Equals(g.Status, DbWerte.GESETZ_STATUS_PROGNOSE, StringComparison.Ordinal))
                    prognoseAb = jahr;
                reihe[t] = behgBasisT * g.Wert.Value;
                etwas = true;
            }

            // Kein einziges Jahr im Katalog: dann gibt es keinen Pfad, und der
            // Bestandsweg (Override = 0 ⇒ keine Abgabe) bleibt — Befund-D5-Regel, ein
            // ungepflegter Satz darf sich nicht als Wert durch die Rechnung schleichen.
            if (!etwas)
            {
                hinweis = string.Format(kultur, MyResource.Resource.WIRT_CO2_PFAD_LUECKE,
                                        beginn.ToString(System.Globalization.CultureInfo.InvariantCulture));
                return null;
            }

            double preisErstes = Preis(beginn);
            int letztes = beginn + T - 1;
            hinweis = string.Format(kultur, MyResource.Resource.WIRT_CO2_PFAD,
                                    preisErstes.ToString("N0", kultur),
                                    beginn.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                    letztes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                                    Preis(letztes).ToString("N0", kultur),
                                    (prognoseAb > 0 ? prognoseAb : letztes)
                                        .ToString(System.Globalization.CultureInfo.InvariantCulture));

            if (luecken.Count > 0)
                hinweis += " | " + string.Format(kultur, MyResource.Resource.WIRT_CO2_PFAD_LUECKE,
                                                 string.Join(", ", luecken.ConvertAll(
                                                     j => j.ToString(System.Globalization.CultureInfo.InvariantCulture))
                                                     .ToArray()));
            return reihe;
        }

        /// <summary>Der CO₂-Preis eines Kalenderjahres [€/t]; 0 = keine Stützstelle.</summary>
        private double Preis(int jahr)
        {
            if (_gesetze == null) _gesetze = new GesetzKatalog();
            double? w = _gesetze.Wert(DbWerte.GESETZ_CO2_PREIS_NEHS, jahr);
            return w.HasValue ? w.Value : 0;
        }

        // =====================================================================
        // ETAPPE K6 — Vbh-Kontingent nach § 8 und Pauschale nach § 9 KWKG
        // =====================================================================

        /// <summary>
        /// Das Vbh-Kontingent des Projekts [h]. <b>Der Override gewinnt:</b> Steht in
        /// <c>KWKG_Vbh_Kontingent</c> ein Wert größer 0, gilt er unverändert — das ist
        /// jede Bestandsdatenbank, und deshalb ändert Etappe K6 hier nichts. Erst ein
        /// Kontingent von 0 <b>und</b> eine ausdrücklich erfasste Anlagenart lassen den
        /// Wert nach § 8 KWKG ableiten (<see cref="KwkgKontingentRechner"/>).
        /// </summary>
        private double KontingentDesProjekts(WirtschaftlichkeitParameter p, int jahr,
                                             List<string> hinweise)
        {
            if (p.KwkgVbhKontingent > 0) return p.KwkgVbhKontingent;
            if (string.IsNullOrEmpty(p.KwkgAnlagenart)) return p.KwkgVbhKontingent;

            if (_gesetze == null) _gesetze = new GesetzKatalog();
            System.Globalization.CultureInfo kultur = BerichtTexte.Kultur;
            KwkgKontingentVorschlag v = KwkgKontingentRechner.Ableiten(
                p.KwkgAnlagenart, p.KwkgKostenanteil, jahr,
                (s, j) => _gesetze.WertMitHerkunft(s, j), kultur);

            if (hinweise != null)
                hinweise.Add(string.Format(kultur, MyResource.Resource.WIRT_KWKG_KONTINGENT_ABGELEITET,
                                           v.KontingentH.ToString("N0", kultur), v.Herleitung));
            return v.KontingentH;
        }

        /// <summary>
        /// ETAPPE K6 — die pauschale Vorauszahlung nach § 9 KWKG (Anlagen bis
        /// 2 kW<sub>el</sub>): <c>0,04 €/kWh × 60.000 Vbh × P_el[kW]</c>, einmalig im
        /// Jahr 0. Rückgabe <c>true</c> = die Pauschale greift, der <b>laufende</b>
        /// Zuschlag entfällt dafür vollständig (§ 9: „damit entfällt die
        /// Einzelabrechnung").
        ///
        /// <para>Über der Leistungsgrenze bleibt der Schalter ohne Wirkung, und der
        /// Hinweis sagt warum — statt ihn still zu übergehen oder eine Anlage zu
        /// begünstigen, der die Norm nicht gilt. Alle drei Zahlen (Satz, Vbh, Grenze)
        /// stehen im Gesetzeskatalog; fehlt eine, gibt es keine Vorauszahlung und eine
        /// Begründung.</para>
        /// </summary>
        private bool PauschaleReihe(VariantenDaten v, WirtschaftlichkeitParameter p,
                                    out double[] reihe, out string hinweis)
        {
            reihe = null;
            hinweis = null;
            if (!p.KwkgPauschalmodus) return false;
            if (v == null || v.Ergebnis == null || v.Ergebnis.BHKW == null) return false;

            System.Globalization.CultureInfo kultur = BerichtTexte.Kultur;
            if (_gesetze == null) _gesetze = new GesetzKatalog();
            int jahr = Foerderbeginn(p);

            double? grenze = _gesetze.Wert(DbWerte.GESETZ_KWKG_PAUSCHALE_GRENZE, jahr);
            double? satzCt = _gesetze.Wert(DbWerte.GESETZ_KWKG_PAUSCHALE_BIS2KW, jahr);
            double? vbh = _gesetze.Wert(DbWerte.GESETZ_KWKG_PAUSCHALE_BIS2KW_VBH, jahr);
            if (!grenze.HasValue || !satzCt.HasValue || !vbh.HasValue)
            {
                hinweis = string.Format(MyResource.Resource.WIRT_KWKG_PAUSCHALE_SATZ_FEHLT,
                                        DbWerte.GESETZ_KWKG_PAUSCHALE_BIS2KW);
                return false;
            }

            double pelKW = PelKW(v.IdProjekt);
            if (pelKW <= 0) return false;      // ohne Nennleistung nichts zu rechnen

            if (pelKW > grenze.Value)
            {
                hinweis = string.Format(kultur, MyResource.Resource.WIRT_KWKG_PAUSCHALE_ZU_GROSS,
                                        pelKW.ToString("N1", kultur),
                                        grenze.Value.ToString("N0", kultur));
                return false;                  // Schalter ignoriert, laufender Zuschlag bleibt
            }

            double betrag = satzCt.Value / 100.0 * vbh.Value * pelKW;   // €/kWh × h × kW
            int T = Math.Max(1, p.Betrachtungszeitraum);
            reihe = new double[T + 1];
            reihe[0] = betrag;                 // EINMALIG im Jahr 0, nicht abgezinst

            hinweis = string.Format(kultur, MyResource.Resource.WIRT_KWKG_PAUSCHALE,
                                    betrag.ToString("N0", kultur),
                                    satzCt.Value.ToString("N2", kultur),
                                    vbh.Value.ToString("N0", kultur),
                                    pelKW.ToString("N1", kultur));
            return true;
        }

        /// <summary>
        /// Die aufgelösten Bilanzierungsregeln dieses Laufs (Leitentscheidungen L12/L13),
        /// gegen den geladenen Gesetzeskatalog bestimmt.
        ///
        /// <para><b>Bewusst NICHT über <see cref="Foerderbeginn"/>.</b> Das Förderjahr
        /// fällt ohne gepflegte Inbetriebnahme auf „aktuelles Jahr + 1" zurück — heute
        /// also auf 2027. Daran den Wegfall des Verdrängungsstrommix zu hängen, hätte
        /// jedes Bestandsprojekt ohne Inbetriebnahmedatum sofort auf den neuen
        /// Rechtsstand gezogen. Das Bilanzjahr ist deshalb eine eigene Projektangabe
        /// mit festem Rückfall auf 2026 (<c>BilanzKonvention.BILANZJAHR_RUECKFALL</c>).</para>
        /// </summary>
        private BilanzKonvention Bilanzregeln(WirtschaftlichkeitParameter p)
        {
            if (_gesetze == null) _gesetze = new GesetzKatalog();
            return BilanzKonvention.Bestimme(p, _gesetze);
        }

        /// <summary>Hinweistexte verketten (dieselbe Trennung wie in <c>Berechne</c>).</summary>
        private static string Anhaengen(string bisher, string neu)
        {
            return string.IsNullOrEmpty(bisher) ? neu : bisher + " | " + neu;
        }

        /// <summary>
        /// ETAPPE B3 Paket b — MyResource mit deutschem Rückfall (Drei-Schichten-Regel),
        /// dasselbe Muster wie <c>SteuerGutschriftRechner.T</c> und
        /// <c>KohaerenzPruefung.T</c>. Der Rückfall greift auf einer Ressourcendatei
        /// ohne die neuen Einträge; die Schlüssel werden mit dem nächsten
        /// resx-Sammelnachtrag ergänzt.
        /// </summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }

        // =====================================================================
        // ETAPPE E4 — Energiesteuer- und Stromsteuergutschriften
        // =====================================================================

        /// <summary>
        /// Baut die drei jahresscharfen Gutschriftreihen (Energiesteuer,
        /// Stromsteuer-Befreiung, Stromsteuer-Entlastung) und hängt sie an
        /// <see cref="ProjektEingabe.ErloesReihen"/>.
        ///
        /// <para><b>Jahresscharf wie die KWKG-Reihe (L1).</b> Die Sätze des Katalogs
        /// tragen ein Gültigkeitsjahr; für jedes Betrachtungsjahr wird deshalb mit dem
        /// Satz dieses Jahres gerechnet (<c>Förderbeginn + t − 1</c>). Auf dem heutigen
        /// Rechtsstand sind die Sätze ab 2026 konstant, die Reihen also flach — die
        /// Mechanik trägt aber jede künftige Novelle, ohne dass eine Altrechnung ihre
        /// Zahlen ändert.</para>
        ///
        /// <para><b>Die Begründungen entstehen nur EINMAL</b>, aus dem ersten Jahr. Sonst
        /// stünde derselbe Satz zwanzigmal im Hinweisfeld.</para>
        ///
        /// <para><b>Seit B3: BHKW UND Heizkessel.</b> Bis dahin enthielt die Anlagenliste
        /// ausschließlich BHKW-Anlagen — richtig für § 53 und § 53a, die den Brennstoff
        /// der Stromerzeugung entlasten, aber falsch für § 54: Der hängt an keiner
        /// KWK-Anlage, sondern am produzierenden Gewerbe (Entscheidung BF5). Die
        /// Abgrenzung liegt seither dort, wo sie hingehört — je Anlage im
        /// <c>SteuerGutschriftRechner</c> über
        /// <see cref="SteuerAnlage.Stromerzeuger"/>, nicht in der Zuführung.</para>
        /// </summary>
        private void BaueSteuerReihen(VariantenDaten v, WirtschaftlichkeitParameter p,
                                      ProjektEingabe e, out string hinweis)
        {
            hinweis = null;
            if (v == null || v.Ergebnis == null) return;

            SteuerEingabe eingabe = BaueSteuerEingabe(v, p, e.Matrix);
            if (eingabe == null) return;
            e.SteuerEingabe = eingabe;      // B2: Grundlage der Kohärenzprüfung, reine Ausgabe

            if (_gesetze == null) _gesetze = new GesetzKatalog();
            System.Globalization.CultureInfo kultur = BerichtTexte.Kultur;

            int T = Math.Max(1, p.Betrachtungszeitraum);
            int beginn = Foerderbeginn(p);

            double[] energie = new double[T + 1];
            double[] befreiung = new double[T + 1];
            double[] entlastung = new double[T + 1];
            var herkunft = new List<string>();
            var begruendungen = new List<string>();

            for (int t = 1; t <= T; t++)
            {
                int jahr = beginn + t - 1;
                SteuerErgebnis r = SteuerGutschriftRechner.Rechne(
                    eingabe, jahr, s => _gesetze.WertMitHerkunft(s, jahr), kultur);

                energie[t] = r.EnergiesteuerEur;
                befreiung[t] = r.StromsteuerBefreiungEur;
                entlastung[t] = r.StromsteuerEntlastungEur;

                if (t != 1) continue;                       // Texte nur aus dem ersten Jahr
                foreach (string s in r.Begruendungen) if (!begruendungen.Contains(s)) begruendungen.Add(s);
                foreach (string s in r.Herkunft) if (!herkunft.Contains(s)) herkunft.Add(s);
            }

            e.EnergiesteuerJahr1 = energie[1];
            e.StromsteuerBefreiungJahr1 = befreiung[1];
            e.StromsteuerEntlastungJahr1 = entlastung[1];
            if (herkunft.Count > 0) e.SteuerHerkunft = string.Join(" | ", herkunft.ToArray());

            // Eine Reihe ohne jeden Betrag wird gar nicht erst angehängt — sie hätte im
            // Kapitalwert keine Wirkung und im Bericht (E7) keinen Aussagewert.
            Reihe(e, KapitalwertRechner.ErloesReihe.ENERGIESTEUER, energie);
            Reihe(e, KapitalwertRechner.ErloesReihe.STROMSTEUER_BEFREIUNG, befreiung);
            Reihe(e, KapitalwertRechner.ErloesReihe.STROMSTEUER_ENTLASTUNG, entlastung);

            if (begruendungen.Count > 0) hinweis = string.Join(" | ", begruendungen.ToArray());
        }

        /// <summary>Hängt eine Reihe an, sofern sie überhaupt einen Betrag führt.</summary>
        private static void Reihe(ProjektEingabe e, string name, double[] werte)
        {
            for (int t = 1; t < werte.Length; t++)
                if (werte[t] != 0)
                {
                    e.ErloesReihen.Add(new KapitalwertRechner.ErloesReihe(name, werte));
                    return;
                }
        }

        /// <summary>
        /// Sammelt Mengen und Projektangaben für die Steuerprüfung.
        /// <c>null</c> = kein BHKW im Lauf (dann gibt es nichts zu prüfen und nichts zu
        /// melden).
        ///
        /// <para><b>Die Anlagenliste ist dieselbe wie beim KWKG-Guard</b>
        /// (<c>Tab_Energieanlagen</c> ⋈ <c>Tab_BHKW</c>, gepaart mit den
        /// Ergebnis-Modulzeilen). Nur so ist die 2-MW-Grenze des § 9 Abs. 1 Nr. 3
        /// StromStG <b>je Anlage</b> prüfbar — die Grenze ist eine Anlagen-Nennleistung,
        /// nicht die Projektsumme (Restbefund 3 aus dem E2-Protokoll).</para>
        ///
        /// <para><b>Ersatzweg ohne Anlagenzeilen.</b> Lassen sich Anlagen und Modulzeilen
        /// nicht paaren, wird je Modulzeile eine Anlage gebildet und ihr die
        /// PROJEKTSUMME der elektrischen Leistung zugeschrieben. Das ist konservativ: Es
        /// schließt eher zu viel von der Befreiung aus als zu wenig — derselbe Zweig wie
        /// beim KWKG-Guard.</para>
        /// </summary>
        private SteuerEingabe BaueSteuerEingabe(VariantenDaten v, WirtschaftlichkeitParameter p,
                                                StromMatrix matrix)
        {
            List<ErgebnisBHKWModulModel> module = v.Ergebnis.BHKW != null ? v.Ergebnis.BHKW.Module : null;
            bool hatBhkw = module != null && module.Count > 0;

            // ETAPPE E5 — § 9b StromStG hängt an KEINER KWK-Anlage. Er entlastet den
            // Netzbezug JEDES Unternehmens des produzierenden Gewerbes (und jedes
            // Betriebs der Land- und Forstwirtschaft). Bis E4 fiel er mit der
            // BHKW-Prüfung weg — der offene Punkt 1 des E4-Protokolls.
            //
            // ERGEBNISNEUTRAL: Die Erweiterung greift NUR, wenn die Unternehmensart
            // ausdrücklich auf produzierendes Gewerbe bzw. Land- und Forstwirtschaft
            // steht. Die Vorbelegung aus Migrationsschritt 20b ist KEIN_PROD_GEWERBE —
            // ein Bestandsprojekt ohne BHKW liefert deshalb weiterhin null und meldet
            // auch nichts (sonst stünde an jedem Wärmepumpenprojekt eine Begründung,
            // warum es keine Entlastung gibt, die niemand beantragt hat).
            bool prodGewerbe =
                string.Equals(p.Unternehmensart, DbWerte.UNTERNEHMENSART_PROD_GEWERBE, StringComparison.Ordinal) ||
                string.Equals(p.Unternehmensart, DbWerte.UNTERNEHMENSART_LAND_FORST, StringComparison.Ordinal);
            if (!hatBhkw && !prodGewerbe) return null;

            var eingabe = new SteuerEingabe
            {
                Unternehmensart = p.Unternehmensart,
                RaeumlicherZusammenhang = p.RaeumlicherZusammenhang,
                HocheffizienzNachweis = p.HocheffizienzNachweis,
                JahresnutzungsgradProzent = p.Jahresnutzungsgrad,
                EnergiesteuerWahl = p.EnergiesteuerWahl,
                AufteilungMethode = p.AufteilungMethode
            };

            // Ohne BHKW bleibt die Anlagenliste leer — Energiesteuer und
            // Stromsteuerbefreiung hängen an ihr und schweigen dann (E5); § 9b rechnet
            // allein mit dem Netzbezug weiter unten.
            if (hatBhkw)
            {
                List<BhkwAnlage> anlagen = BhkwAnlagen(v.IdProjekt);
                ErgebnisBHKWModulModel[] zuordnung = anlagen.Count > 0
                    ? ModulJeAnlage(anlagen, module) : null;

                if (zuordnung != null)
                    for (int i = 0; i < anlagen.Count; i++)
                        eingabe.Anlagen.Add(BaueSteuerAnlage(v.IdProjekt, anlagen[i].Bezeichner,
                            anlagen[i].PelKW, zuordnung[i], anlagen[i].IdCarrier, anlagen[i].IdBrennstoff,
                            anlagen[i].EnergiesteuerWahl, anlagen[i].AufteilungMethode));
                else
                {
                    double pelProjekt = PelKW(v.IdProjekt);
                    foreach (ErgebnisBHKWModulModel m in module)
                        eingabe.Anlagen.Add(BaueSteuerAnlage(v.IdProjekt,
                            m.Modul ?? "", pelProjekt, m, 0, 0, null, null));
                }
            }

            // ETAPPE B3 Paket a (BF5) — die HEIZKESSEL. § 54 EnergieStG hängt an keiner
            // KWK-Anlage; er entlastet den Brennstoff eines Unternehmens des
            // produzierenden Gewerbes, gleich ob er in einem BHKW oder in einem Kessel
            // verbrannt wird.
            KesselAnlagenErgaenzen(v, eingabe);

            // Eigenverbrauch: NUR aus der Stundenreihe. Ohne sie bleibt der Wert null,
            // und die Befreiung entfällt mit Begründung (siehe SteuerGutschriftRechner).
            //
            // ETAPPE B3 Paket b — BRUTTO, und das ist keine Auslassung, sondern die
            // Vorschrift. § 4.3 des Konzepts sagt zum Hilfsstrom ausdrücklich:
            // „steuerlich Teil des KWK-Eigenverbrauchs, sofern die Anlage die Bedingungen
            // des § 9 Abs. 1 Nr. 3 erfüllt". Der Hilfsstrom wird in der Anlage selbst
            // verbraucht — er ist der Musterfall des begünstigten Eigenverbrauchs und
            // gehört deshalb in die Bemessungsgrundlage der Befreiung hinein. Gemindert
            // wird von ihm allein die ZUSCHLAGSfähige Nettoerzeugung des KWKG
            // (BaueKwkgReihe); die beiden Vorschriften stellen auf verschiedene Größen
            // ab, und genau diese Trennung wird hier festgehalten.
            if (matrix != null && !matrix.StrombedarfFehlt)
                eingabe.KwkEigenMWh = matrix.KwkEigenGesamtMWh;

            // Netzbezug: die Stundenreihe, sonst die Jahressumme des Laufs — beides sind
            // gerechnete Größen desselben Laufs, keine Näherung.
            eingabe.NetzbezugMWh = matrix != null ? matrix.BezugGesamtMWh
                : (v.Ergebnis.Energiebedarf != null ? v.Ergebnis.Energiebedarf.Stromrestbedarf : 0);

            return eingabe;
        }

        /// <summary>Eine Anlagenzeile der Steuerprüfung aus Anlagen- und Modulangaben.</summary>
        private SteuerAnlage BaueSteuerAnlage(int idProjekt, string bezeichner, double pelKW,
                                              ErgebnisBHKWModulModel modul,
                                              int idCarrierAnlage, int idBrennstoffAnlage,
                                              string wahl, string methode)
        {
            var a = new SteuerAnlage { Bezeichner = bezeichner, PelKW = pelKW };
            if (modul != null)
            {
                a.BrennstoffMWh = modul.Verbrauch;
                // ETAPPE B3 Paket b — BRUTTO, bewusst. Die Stromerzeugung geht im
                // SteuerGutschriftRechner in zwei Größen ein, und beide meinen die
                // ERZEUGUNG, nicht die zuschlagsfähige Menge: die Anteilsbereinigung des
                // § 9 Abs. 1 Nr. 3 (welcher Teil der Erzeugung stammt aus Anlagen, die
                // die Bedingungen erfüllen) und der CO₂-Grenzwert des § 2 StromStG
                // (Brennstoff-CO₂ je kWh Energieertrag Strom + Wärme). Ein Abzug des
                // Hilfsstroms würde hier den Energieertrag kleinrechnen und damit die
                // spezifischen Emissionen künstlich erhöhen — die Anlage fiele unter
                // Umständen aus der Befreiung, weil sie eine Pumpe betreibt.
                a.StromMWh = modul.Stromproduktion;
                a.WaermeMWh = modul.Waermeproduktion;
            }

            // Der Träger der ERGEBNISZEILE hat Vorrang: Er ist der, mit dem der Lauf
            // gerechnet hat. Erst wenn er fehlt, gilt der Träger der Anlagenzeile.
            int carrier = modul != null && modul.CarrierId > 0 ? modul.CarrierId : idCarrierAnlage;
            int brennstoff = carrier > 0 ? BrennstoffId(carrier, idBrennstoffAnlage)
                                         : idBrennstoffAnlage;

            SteuerschluesselSetzen(idProjekt, a, carrier, brennstoff);

            // ETAPPE B3 Paket a — die Wahl DIESER Anlage; leer heißt „es gilt der
            // Projektwert", und der steht bereits in der SteuerEingabe.
            a.EnergiesteuerWahl = wahl;
            a.AufteilungMethode = methode;
            return a;
        }

        /// <summary>
        /// Satzschlüssel, Heizwerte und Abrechnungseinheit einer Steuerzeile — der Teil,
        /// den BHKW und Heizkessel wörtlich teilen (B3a).
        /// </summary>
        private void SteuerschluesselSetzen(int idProjekt, SteuerAnlage a, int carrier, int brennstoff)
        {
            a.SchluesselSatzVoll = EnergiesteuerSchluessel(brennstoff, false);
            a.SchluesselSatz53a = EnergiesteuerSchluessel(brennstoff, true);
            a.SchluesselSatz54 = Energiesteuer54Schluessel(brennstoff);     // K6
            a.SchluesselCo2 = Co2Schluessel(brennstoff);
            a.Fossil = FossilerBrennstoff(brennstoff);

            TraegerEinheit t = Traeger(idProjekt, carrier);
            a.EffHi = t.EffHi;
            a.EffHs = t.EffHs;
            a.Abrechnungseinheit = t.Einheit;
            a.CarrierId = carrier;          // B2: Bezugspunkt der Kohärenzprüfung
        }

        /// <summary>
        /// ETAPPE B3 Paket a (BF5) — hängt die HEIZKESSEL des Projekts an die
        /// Steuereingabe. § 54 EnergieStG entlastet den Brennstoff eines Unternehmens des
        /// produzierenden Gewerbes; ob er in einem BHKW oder in einem Kessel verbrannt
        /// wird, ist der Vorschrift gleichgültig.
        ///
        /// <para><b>Das Tor: es muss überhaupt etwas gewählt sein.</b> Ohne eine
        /// Steuerwahl — weder im Projekt noch an einer Anlage — bleiben die Kesselzeilen
        /// draußen. Sonst bekämen 26 der 28 Bestandsprojekte mit Kessel schlagartig die
        /// Hinweiszeile „keine Entlastung gewählt", die es dort nie gab; die Etappe soll
        /// aber wirken, wenn gepflegt wird, und sonst gar nicht. Rechenwirkung hat das
        /// Tor keine: Ohne Wahl gäbe es ohnehin 0 €.</para>
        ///
        /// <para><b>Verwaiste Modulzeilen</b> (Bezeichner ohne passende Anlagenzeile —
        /// im Bestand die Projekte 1042 und 1044, wo nach dem Lauf die Anlage getauscht
        /// wurde) werden wie beim BHKW über den Ersatzweg geführt: je Modulzeile eine
        /// Rechenzeile mit der PROJEKTwahl und dem Modulnamen. Verschluckt wird keine
        /// Zeile, doppelt gezählt auch keine — <c>ModulJeAnlage</c> vergibt jede
        /// Modulzeile höchstens einmal.</para>
        /// </summary>
        private void KesselAnlagenErgaenzen(VariantenDaten v, SteuerEingabe eingabe)
        {
            List<ErgebnisHeizkesselModulModel> module =
                v.Ergebnis.Heizkessel != null ? v.Ergebnis.Heizkessel.Module : null;
            if (module == null || module.Count == 0) return;

            List<BhkwAnlage> anlagen = KesselAnlagen(v.IdProjekt);

            bool projektwahl = !string.IsNullOrEmpty(eingabe.EnergiesteuerWahl) &&
                               !string.Equals(eingabe.EnergiesteuerWahl,
                                              DbWerte.ENERGIESTEUER_WAHL_KEINE, StringComparison.Ordinal);
            bool anlagenwahl = false;
            foreach (BhkwAnlage k in anlagen) if (k.HatEigeneSteuerwahl) { anlagenwahl = true; break; }
            if (!projektwahl && !anlagenwahl) return;

            ErgebnisHeizkesselModulModel[] zuordnung = anlagen.Count > 0
                ? KesselModulJeAnlage(anlagen, module) : null;

            if (zuordnung != null)
            {
                for (int i = 0; i < anlagen.Count; i++)
                    eingabe.Anlagen.Add(BaueSteuerAnlageKessel(v.IdProjekt, anlagen[i].Bezeichner,
                        zuordnung[i], anlagen[i].IdCarrier, anlagen[i].IdBrennstoff,
                        anlagen[i].EnergiesteuerWahl, anlagen[i].AufteilungMethode));
                return;
            }

            foreach (ErgebnisHeizkesselModulModel m in module)
                eingabe.Anlagen.Add(BaueSteuerAnlageKessel(v.IdProjekt, m.Modul ?? "", m, 0, 0, null, null));
        }

        /// <summary>
        /// Eine KESSEL-Zeile der Steuerprüfung (B3a). Unterschiede zur BHKW-Zeile:
        /// <c>PelKW</c> und <c>StromMWh</c> sind 0 (ein Kessel erzeugt keinen Strom, und
        /// keine stromseitige Prüfung darf ihn meinen), die Wärme ist
        /// <c>Waerme_Gas + Waerme_Oel</c>, und der Brennstoff wird abgeleitet — siehe
        /// <see cref="KesselBrennstoffMWh"/>.
        /// </summary>
        private SteuerAnlage BaueSteuerAnlageKessel(int idProjekt, string bezeichner,
                                                    ErgebnisHeizkesselModulModel modul,
                                                    int idCarrierAnlage, int idBrennstoffAnlage,
                                                    string wahl, string methode)
        {
            var a = new SteuerAnlage { Bezeichner = bezeichner, PelKW = 0, Stromerzeuger = false };
            if (modul != null)
            {
                a.WaermeMWh = modul.Waerme_Gas + modul.Waerme_Oel;
                a.BrennstoffMWh = KesselBrennstoffMWh(modul);
            }

            int carrier = modul != null && modul.CarrierId > 0 ? modul.CarrierId : idCarrierAnlage;
            int brennstoff = carrier > 0 ? BrennstoffId(carrier, idBrennstoffAnlage)
                                         : idBrennstoffAnlage;
            SteuerschluesselSetzen(idProjekt, a, carrier, brennstoff);

            a.EnergiesteuerWahl = wahl;
            a.AufteilungMethode = methode;
            return a;
        }

        /// <summary>
        /// Bemessungsmenge des § 54 für einen Kessel [MWh/a, heizwertbezogen].
        ///
        /// <para><b>Warum sie abgeleitet und nicht gelesen wird.</b>
        /// <c>Tab_ErgebnisHeizkesselModul.Verbrauch</c> existiert seit jeher, wird vom
        /// Rechenkern aber NIE gesetzt (<c>SimulationRunner</c> füllt an der Modulzeile
        /// nur Modul, Waerme_Gas, Waerme_Oel, Jahresnutzungsgrad und carrier_id) — im
        /// ganzen Bestand steht dort 0. Gelesen wird die Spalte trotzdem zuerst: Sobald
        /// sie einmal gefüllt wird, ist sie die bessere Quelle, und diese Reihenfolge
        /// muss dann nicht noch einmal angefasst werden.</para>
        ///
        /// <para><b>Die Ableitung ist die exakte Umkehrung der Vorwärtsrechnung.</b>
        /// <c>SimulationSPK.Bilanz_und_Nutzungsgrad</c> bildet den Nutzungsgrad als
        /// <c>(Waerme_Gas + Waerme_Oel) / Brennstoffeinsatz × 100</c> — in PROZENT und
        /// über denselben Zähler. Die Rückrechnung
        /// <c>(Waerme_Gas + Waerme_Oel) / (Nutzungsgrad / 100)</c> liefert deshalb wieder
        /// den Brennstoffeinsatz des Laufs, nicht eine Näherung. Einzige Ausnahme sind
        /// die Plausibilitätsklemmen des Rechenkerns (Nutzungsgrad über 110 % wird auf
        /// 108 gesetzt, unter 1 % auf 1); in diesen Fällen weicht die Rückrechnung um
        /// genau den geklemmten Betrag ab — ein Fall, den es nur bei absurden
        /// Eingangsdaten gibt.</para>
        ///
        /// <para><b>Ohne Nutzungsgrad keine Menge:</b> 0, und die Steuerrechnung meldet
        /// „Menge unklar" mit dem Anlagennamen. Eine geratene Menge wäre hier dasselbe wie
        /// eine geratene Dichte (Leitentscheidung L3).</para>
        ///
        /// <para><b>Der Simulationspfad bleibt unberührt.</b> Die Ableitung steht
        /// bewusst hier in der Zuführung und nicht im <c>SimulationRunner</c>: Eine neu
        /// gefüllte Ergebnisspalte änderte gespeicherte Läufe und damit die
        /// Referenzlaufvergleiche, ohne dass die Wirtschaftlichkeit davon mehr hätte.</para>
        ///
        /// <para><b>ETAPPE B3 Paket b: <c>internal</c>.</b> Der Speicherweg
        /// (<see cref="ErgebnisCtrl"/>) braucht dieselbe Menge als Bemessungsgrundlage
        /// des Hilfsstroms. Eine zweite Ableitung daneben wäre die zweite Wahrheit über
        /// genau die Frage, die dieser Kommentar beantwortet.</para>
        /// </summary>
        internal static double KesselBrennstoffMWh(ErgebnisHeizkesselModulModel m)
        {
            if (m.Verbrauch > 0) return m.Verbrauch;
            double waerme = m.Waerme_Gas + m.Waerme_Oel;
            if (waerme <= 0 || m.Jahresnutzungsgrad <= 0) return 0;
            return waerme / (m.Jahresnutzungsgrad / 100.0);
        }

        /// <summary>
        /// Ordnet jeder KESSEL-Anlagenzeile ihre Ergebnis-Modulzeile zu — Zeile für Zeile
        /// dasselbe Verfahren wie <see cref="ModulJeAnlage"/> beim BHKW (Bezeichner
        /// zuerst, Reihenfolge bei gleicher Anzahl als Rückfall, sonst <c>null</c>).
        ///
        /// <para>Eine gemeinsame generische Fassung hätte beide Modultypen unter eine
        /// Schnittstelle zwingen müssen, die es im Modell nicht gibt
        /// (<c>ErgebnisBHKWModulModel</c> und <c>ErgebnisHeizkesselModulModel</c> teilen
        /// nur das Feld <c>Modul</c>) — für zwanzig Zeilen der falsche Preis.</para>
        /// </summary>
        private static ErgebnisHeizkesselModulModel[] KesselModulJeAnlage(
            List<BhkwAnlage> anlagen, List<ErgebnisHeizkesselModulModel> module)
        {
            var treffer = new ErgebnisHeizkesselModulModel[anlagen.Count];
            bool[] belegt = new bool[module.Count];
            int getroffen = 0;

            for (int i = 0; i < anlagen.Count; i++)
                for (int j = 0; j < module.Count; j++)
                {
                    if (belegt[j]) continue;
                    string name = module[j].Modul == null ? "" : module[j].Modul.Trim();
                    if (!string.Equals(name, anlagen[i].Bezeichner, StringComparison.OrdinalIgnoreCase))
                        continue;
                    belegt[j] = true;
                    treffer[i] = module[j];
                    getroffen++;
                    break;
                }
            if (getroffen == anlagen.Count) return treffer;

            if (anlagen.Count != module.Count) return null;
            for (int i = 0; i < anlagen.Count; i++) treffer[i] = module[i];
            return treffer;
        }

        /// <summary>Abrechnungseinheit und Heizwerte eines Energieträgers.</summary>
        private sealed class TraegerEinheit
        {
            public double EffHi;
            public double EffHs;
            public string Einheit = "";
        }

        /// <summary>Cache je Berechne-Lauf: (Projekt, Träger) → Einheit und Heizwerte.</summary>
        private readonly Dictionary<string, TraegerEinheit> _traegerCache =
            new Dictionary<string, TraegerEinheit>();

        /// <summary>
        /// Abrechnungseinheit, Heizwert und Brennwert eines Trägers — <b>Projektwert vor
        /// Katalogwert</b>: zuerst <c>Abfrage_Energietraeger_Effektiv</c> (dieselbe
        /// Quelle, aus der auch <c>KostenEmissionRechner</c> seine Mengen bildet),
        /// ersatzweise die Katalogzeile <c>energy_carrier</c>. Es gibt in dieser
        /// Anwendung keine zweite Wahrheit über Heizwerte.
        ///
        /// <para><b>Warum die Rückfallebene nötig ist.</b> Die gespeicherte Abfrage führt
        /// nur die Träger, die dem Projekt in <c>energy_project_settings</c> zugeordnet
        /// sind. Fährt eine Anlage einen Träger ohne solche Zuordnung, gäbe es sonst
        /// weder Abrechnungseinheit noch Heizwert — und die Steuerrechnung meldete „nicht
        /// umrechenbar", obwohl der Katalog beides führt. Der Katalogwert ist der
        /// schwächere, aber richtige Ersatz.</para>
        /// </summary>
        private TraegerEinheit Traeger(int idProjekt, int carrierId)
        {
            var leer = new TraegerEinheit();
            if (carrierId <= 0) return leer;
            string key = idProjekt + "/" + carrierId;
            TraegerEinheit gefunden;
            if (_traegerCache.TryGetValue(key, out gefunden)) return gefunden;

            var t = new TraegerEinheit();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT billing_unit, eff_hi, eff_hs FROM Abfrage_Energietraeger_Effektiv " +
                    "WHERE ID_Projekt = ? AND carrier_id = ?",
                    new DbParam("@p", idProjekt), new DbParam("@c", carrierId));
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    t.Einheit = r["billing_unit"] != DBNull.Value
                              ? Convert.ToString(r["billing_unit"]).Trim() : "";
                    t.EffHi = D(r, "eff_hi") ?? 0;
                    t.EffHs = D(r, "eff_hs") ?? 0;
                }
            }
            catch { }

            if (t.EffHi <= 0 || t.Einheit.Length == 0)
                try
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT billing_unit, hi_kwh_per_unit, hs_kwh_per_unit FROM energy_carrier WHERE id = ?",
                        new DbParam("@c", carrierId));
                    if (dt != null && dt.Rows.Count > 0)
                    {
                        DataRow r = dt.Rows[0];
                        if (t.Einheit.Length == 0 && r["billing_unit"] != DBNull.Value)
                            t.Einheit = Convert.ToString(r["billing_unit"]).Trim();
                        if (t.EffHi <= 0) t.EffHi = D(r, "hi_kwh_per_unit") ?? 0;
                        if (t.EffHs <= 0) t.EffHs = D(r, "hs_kwh_per_unit") ?? 0;
                    }
                }
                catch { }

            _traegerCache[key] = t;
            return t;
        }

        /// <summary>
        /// Katalogschlüssel des Energiesteuersatzes eines Brennstoffs
        /// (<c>Tab_Brennstoff_Stamm.ID</c>); leer = kein Satz zugeordnet, dann gibt es
        /// keine Gutschrift und eine Begründung.
        ///
        /// <para><b>Ausdrücklich unvollständig, und das ist Absicht.</b> Zugeordnet wird
        /// nur, was <c>Grundlagen_KWKG_Energiesteuer_Stromsteuer.md</c> Abschnitt 3
        /// namentlich führt. Alles Übrige — Stadtgas, Wasserstoff, Kohle und Koks nach
        /// § 2, Biogas, Holz, Pellets, Rapsöl, tierische Fette, Fernwärme — bleibt ohne
        /// Zuordnung. Eine geratene Einordnung wäre genau der Fehlertyp, den
        /// Leitentscheidung L3 verhindern soll.</para>
        ///
        /// <para><b>Heizöl L und M zählen als Schweröl</b> (§ 2 Abs. 3 Satz 1 Nr. 2, je
        /// 1.000 kg); nur Heizöl EL ist Gasöl im Sinne der Nr. 1 Buchst. a (je 1.000 l).
        /// Die Bio-Blends folgen dem Heizöl EL — dieselbe Näherung, mit der schon die
        /// BEHG-Einstufung arbeitet.</para>
        /// </summary>
        /// <param name="teilsatz">true = Teilsatz nach § 53a Abs. 5, false = voller Satz nach § 2.</param>
        /// <remarks>
        /// <b>Sichtbarkeit <c>internal</c> seit Etappe B2</b> (Konzept BHKW-Wirtschaftlichkeit
        /// § 6.2): Die Schnellwahl in <see cref="ucBrennstoffBestandteile"/> braucht dieselbe
        /// Zuordnung. Sie zu kopieren wäre genau die doppelte Wahrheit, die Befund A7 benennt —
        /// deshalb liest der Dialog diese Methode, statt eine zweite Tabelle zu führen.
        /// </remarks>
        internal static string EnergiesteuerSchluessel(int idBrennstoff, bool teilsatz)
        {
            switch (idBrennstoff)
            {
                case 2:    // Erdgas LL
                case 3:    // Erdgas E
                    return teilsatz ? DbWerte.GESETZ_ENERGIEST_53A5_ERDGAS
                                    : DbWerte.GESETZ_ENERGIEST_ERDGAS;
                case 4:    // Flüssiggas (Propan)
                case 5:    // Flüssiggas (Butan)
                    return teilsatz ? DbWerte.GESETZ_ENERGIEST_53A5_FLUESSIGGAS
                                    : DbWerte.GESETZ_ENERGIEST_FLUESSIGGAS;
                case 6:    // Heizöl S
                case 7:    // Heizöl M
                case 8:    // Heizöl L
                    return teilsatz ? DbWerte.GESETZ_ENERGIEST_53A5_SCHWEROEL
                                    : DbWerte.GESETZ_ENERGIEST_SCHWEROEL;
                case 9:    // Heizöl EL
                case 18:   // Heizöl Bio 5
                case 19:   // Heizöl Bio 10
                case 20:   // Heizöl Bio 15
                case 21:   // Heizöl Bio 20
                case 22:   // Heizöl EL schwefelarm
                    return teilsatz ? DbWerte.GESETZ_ENERGIEST_53A5_HEIZOEL_EL
                                    : DbWerte.GESETZ_ENERGIEST_HEIZOEL_EL;
                default:
                    return "";
            }
        }

        /// <summary>
        /// ETAPPE K6 — Katalogschlüssel des Entlastungssatzes nach § 54 EnergieStG.
        /// Der Paragraf führt <b>nur drei</b> Heizstoffe (Erdgas 1,38 €/MWh, Heizöl EL
        /// 15,34 €/1.000 l, Flüssiggas 15,15 €/1.000 kg); Schweröl und Kohle kommen
        /// darin nicht vor. Für sie liefert die Methode einen leeren Schlüssel — die
        /// Rechnung meldet dann „dem Energieträger ist kein Satz zugeordnet", statt
        /// einen fremden Satz zu verwenden.
        ///
        /// <para><b>Sichtbarkeit <c>internal</c> seit Etappe B2</b> — siehe
        /// <see cref="EnergiesteuerSchluessel"/>.</para>
        /// </summary>
        internal static string Energiesteuer54Schluessel(int idBrennstoff)
        {
            switch (idBrennstoff)
            {
                case 2:    // Erdgas LL
                case 3:    // Erdgas E
                    return DbWerte.GESETZ_ENERGIEST_54_ERDGAS;
                case 4:    // Flüssiggas (Propan)
                case 5:    // Flüssiggas (Butan)
                    return DbWerte.GESETZ_ENERGIEST_54_FLUESSIGGAS;
                case 9:    // Heizöl EL
                case 18:   // Heizöl Bio 5
                case 19:   // Heizöl Bio 10
                case 20:   // Heizöl Bio 15
                case 21:   // Heizöl Bio 20
                case 22:   // Heizöl EL schwefelarm
                    return DbWerte.GESETZ_ENERGIEST_54_HEIZOEL_EL;
                default:
                    return "";
            }
        }

        /// <summary>
        /// Katalogschlüssel des <b>direkten</b> CO₂-Faktors eines Brennstoffs
        /// (Klasse <c>EF_BILANZ</c>, EBeV 2030 Anlage 2 Teil 4, heizwertbezogen); leer =
        /// kein Faktor zugeordnet.
        ///
        /// <para><b>Nicht die Nachweiswerte der Anlage 9.</b> § 2 StromStG fragt nach den
        /// tatsächlichen direkten Emissionen; die Nachweisfaktoren des Gebäuderechts
        /// gehören in den Energieausweis. Leitentscheidung L11 hält die beiden Sätze
        /// getrennt, und diese Zuordnung ist die Anwendung dieser Regel.</para>
        /// </summary>
        private static string Co2Schluessel(int idBrennstoff)
        {
            switch (idBrennstoff)
            {
                case 2:
                case 3:
                    return DbWerte.GESETZ_EF_BILANZ_EBEV_ERDGAS_HI;
                case 4:
                case 5:
                    return DbWerte.GESETZ_EF_BILANZ_EBEV_FLUESSIGGAS;
                case 6:
                case 7:
                case 8:
                    return DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_S;
                case 9:
                case 18:
                case 19:
                case 20:
                case 21:
                case 22:
                    return DbWerte.GESETZ_EF_BILANZ_EBEV_HEIZOEL_EL;
                case 16:   // Rapsöl
                    return DbWerte.GESETZ_EF_BILANZ_EBEV_PFLANZENOEL;
                default:
                    return "";
            }
        }

        /// <summary>
        /// true, wenn der Brennstoff fossil ist — nur dann greift der CO₂-Grenzwert des
        /// § 2 StromStG. Maßstab sind dieselben Kategorien, nach denen
        /// <c>KostenEmissionRechner</c> die BEHG-Pflicht bestimmt (Gas, Öl, Koks, Kohle,
        /// Sonstige), abzüglich Biogas — eine zweite Einstufung derselben Frage wäre eine
        /// doppelte Wahrheit.
        /// </summary>
        private bool FossilerBrennstoff(int idBrennstoff)
        {
            if (idBrennstoff <= 0) return false;
            if (idBrennstoff == 14) return false;               // Biogas
            int k = BrennstoffKategorie(0, idBrennstoff);
            return k == 1 || k == 2 || k == 3 || k == 4 || k == 11;
        }

        /// <summary>
        /// Vbh-Staffel des § 8 Abs. 4 KWKG (JahrVon aufsteigend); Fallback = Gesetzeswerte.
        ///
        /// <para>
        /// <b>Quelle seit Etappe E1: <c>Tab_Gesetzesparameter</c></b>, Schlüssel
        /// <c>KWKG_VBH_JAHRESDECKEL</c>, gelesen über <see cref="GesetzKatalog"/>. Die
        /// Alttabelle <c>Tab_KWKG_Staffel</c> wird seit Etappe K1 (19.08.2026) auch
        /// nicht mehr ANGELEGT: Konstante und DDL/Saat sind aus
        /// <c>StelleTabellenSicher</c> entfernt (Konzept Kosten/Energieträger, HF1).
        /// Seit Etappe K6 ist sie ganz weg — Migrationsschritt 29 (M-E) droppt sie.
        /// </para>
        ///
        /// <para>
        /// <b>Ergebnisgleich.</b> Der Katalog führt die erste Stufe mit
        /// <c>JahrVon = 2021</c> (so steht es im Gesetz), die Alttabelle mit 2020. Auf
        /// den Lookup wirkt sich das nicht aus: <see cref="StaffelDeckel"/> beginnt mit
        /// dem Wert der ERSTEN Zeile und überschreibt ihn erst ab dem passenden Jahr —
        /// für 2020 und früher liefern beide Reihen 5.000 h, ab 2021 sind die Zeilen
        /// ohnehin deckungsgleich.
        /// </para>
        /// </summary>
        private static List<KeyValuePair<int, double>> LadeKwkgStaffel()
        {
            var liste = new GesetzKatalog().Reihe(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL);
            if (liste.Count == 0)
            {
                int[,] f = { { 2020, 5000 }, { 2023, 4000 }, { 2025, 3500 }, { 2026, 3300 },
                             { 2027, 3100 }, { 2028, 2900 }, { 2029, 2700 }, { 2030, 2500 } };
                for (int i = 0; i < f.GetLength(0); i++)
                    liste.Add(new KeyValuePair<int, double>(f[i, 0], f[i, 1]));
            }
            return liste;
        }

        /// <summary>Deckel des Kalenderjahres: letzte Staffelzeile mit JahrVon ≤ Jahr.</summary>
        private static double StaffelDeckel(List<KeyValuePair<int, double>> staffel, int jahr)
        {
            double deckel = staffel.Count > 0 ? staffel[0].Value : 3500;
            foreach (KeyValuePair<int, double> z in staffel)
                if (z.Key <= jahr) deckel = z.Value; else break;
            return deckel;
        }

        /// <summary>
        /// Installierte elektrische BHKW-Leistung des Projekts [kW].
        ///
        /// <para><b>ETAPPE E2 — Bezugsmenge korrigiert.</b> Bis dahin lautete die Abfrage
        /// <c>SELECT SUM(Pel) FROM Tab_BHKW WHERE ID_Projekt = ?</c> — die Summe über alle
        /// BHKW-GERÄTEZEILEN des Projekts. Das ist nicht die installierte Leistung:
        /// <c>Tab_BHKW</c> nimmt jede Katalogübernahme auf, auch wenn die zugehörige
        /// Anlagenzeile nie entstand oder später gelöscht wurde. Die Simulation baut ihre
        /// Modulliste dagegen ausschließlich aus <c>Tab_Energieanlagen</c>
        /// (<c>SimulationControl.BHKW_Liste_Laden</c>); nur diese Geräte laufen und
        /// erzeugen den Strom, an dem der Zuschlag hängt.</para>
        ///
        /// <para><b>Gemessen am Bestand (18.08.2026):</b> Projekt 1024 führt EIN
        /// BHKW-Modul mit 21 kW, <c>Tab_BHKW</c> aber fünf Gerätezeilen mit zusammen
        /// 546,4 kW. Die alte Summe überschritt damit die 500-kW-Schwelle des
        /// Ausschreibungsfensters und setzte den KWK-Zuschlag auf 0 — für eine Anlage, die
        /// nicht einmal ein Zwanzigstel dieser Leistung hat. Projekt 1023 kommt auf
        /// 1.551,2 kW aus elf Gerätezeilen und hat überhaupt kein BHKW im Anlagenbestand.</para>
        ///
        /// <para><b>Rückfall auf die alte Summe</b>, wenn der Verbund keine Zeile liefert
        /// (Anlagenzeile ohne <c>ID_BHKW</c>, Datenbank ohne Anlagenzeilen): Dann ist die
        /// Gerätesumme die einzige verfügbare Aussage, und sie ist konservativ — sie
        /// überschätzt die Leistung nie nach unten. Ein Projekt ohne jede Angabe liefert 0;
        /// die Aufrufer behandeln das ausdrücklich.</para>
        /// </summary>
        private static double LiesBhkwLeistungKW(int idProjekt)
        {
            // 1. Σ P_el über die ANLAGENZEILEN — dieselbe Menge, die die Engine rechnet.
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT SUM(b.Pel) FROM Tab_Energieanlagen AS a " +
                    "INNER JOIN Tab_BHKW AS b ON a.ID_BHKW = b.ID " +
                    "WHERE a.ID_Projekt = ? AND a.ID_Type = " + WizardItemClass.BHKW_TYP,
                    new DbParam("@p", idProjekt));
                if (o != null && o != DBNull.Value)
                {
                    double summe = Convert.ToDouble(o);
                    if (summe > 0) return summe;
                }
            }
            catch { }

            // 2. Rückfall: Σ P_el über die Gerätezeilen (der Weg bis Etappe E2).
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT SUM(Pel) FROM Tab_BHKW WHERE ID_Projekt = ?",
                    new DbParam("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToDouble(o);
            }
            catch { }
            return 0;
        }

        /// <summary>Σ P_el des Projekts [kW], einmal je Berechne-Lauf gelesen.</summary>
        private double PelKW(int idProjekt)
        {
            if (!_pelCache.ContainsKey(idProjekt)) _pelCache[idProjekt] = LiesBhkwLeistungKW(idProjekt);
            return _pelCache[idProjekt];
        }

        // =====================================================================
        // Förderfähigkeit JE ANLAGE — Ausschreibungsgrenze § 8a KWKG / KWKAusV
        // (Nachtrag zu Etappe E2) und Heizöl-Ausschluss (Nachtrag 2)
        // Nutzerentscheidungen vom 19.08.2026
        // =====================================================================

        /// <summary>
        /// Eine BHKW-Anlagenzeile des Projekts mit ihrer elektrischen Nennleistung und
        /// ihrer Brennstoffart.
        /// </summary>
        private sealed class BhkwAnlage
        {
            public string Bezeichner = "";
            public double PelKW;

            /// <summary>
            /// true, wenn diese Anlage einen Brennstoff der Kategorie „Öl" fährt
            /// (<see cref="BRENNSTOFF_KATEGORIE_OEL"/>) — ermittelt in
            /// <see cref="LiesBhkwAnlagen"/> vorrangig über den Energieträger der
            /// ANLAGE, ersatzweise über den Brennstoff der Gerätezeile.
            /// </summary>
            public bool Heizoel;

            /// <summary>
            /// ETAPPE E4: <c>Tab_Energieanlagen.ID_Carrier</c> der Anlage (0 = keiner) —
            /// über ihn kommen Abrechnungseinheit und Heizwert der Steuerrechnung.
            /// </summary>
            public int IdCarrier;

            /// <summary>
            /// ETAPPE E4: der aufgelöste <c>Tab_Brennstoff_Stamm.ID</c> dieser Anlage
            /// (0 = nicht ermittelbar) — <b>dieselbe</b> zweistufige Auflösung wie bei
            /// <see cref="Heizoel"/> (Träger vor Gerät), nur eine Ebene früher
            /// abgegriffen. Er ordnet der Anlage ihren Energiesteuersatz und ihren
            /// CO₂-Faktor zu.
            /// </summary>
            public int IdBrennstoff;

            // ---------------- ETAPPE E6 — die acht Angaben je Anlage ----------------
            //
            // ALLE sind NULL-fähig, und NULL heißt durchgehend „kein eigener Wert, es
            // gilt der Projektwert". Genau dieser Rückfall macht E6 für Bestandsprojekte
            // ergebnisneutral: In jeder Datenbank vor Migrationsschritt 22 sind alle acht
            // Felder leer, und dann rechnet die Reihe Zeile für Zeile wie vorher.

            /// <summary><c>Tab_Energieanlagen.ID</c> — die Zeile, in die der Dialog
            /// schreibt.</summary>
            public int IdAnlage;

            /// <summary><c>Tab_Energieanlagen.ID_Projekt</c> — für die Anzeige im Dialog,
            /// der die ganze Vergleichsgruppe führt.</summary>
            public int IdProjekt;

            /// <summary>Bestell-/Genehmigungsdatum DIESER Anlage (§ 6 KWKG 2025);
            /// <c>null</c> = Projektvorgabe.</summary>
            public DateTime? Stichtag;

            /// <summary>Inbetriebnahmedatum DIESER Anlage; <c>null</c> = Projektvorgabe.
            /// Es entscheidet über Realisierungsfrist, Satzstichtag, Deckelstaffel und
            /// über Neuanlage/Bestandsanlage (Heizöl-Ausschluss).</summary>
            public DateTime? Inbetriebnahme;

            /// <summary>Anlagenart, Steuerwert <c>DbWerte.KWKG_ANLAGENART_*</c>; leer =
            /// nicht erfasst (der Vorschlag rechnet dann als Neuanlage). Ohne
            /// Rechenwirkung — steuert nur den Katalogvorschlag.</summary>
            public string Anlagenart = "";

            /// <summary>Tatbestand des § 6 Abs. 3, Steuerwert
            /// <c>DbWerte.KWKG_EIGENFALL_*</c>; leer = keiner. Ohne Rechenwirkung.</summary>
            public string Eigenfall = "";

            /// <summary>Überschreibwert des Einspeisesatzes [ct/kWh]; <c>null</c> =
            /// Projektsatz <c>KwkgBonusEinspeisung</c>.</summary>
            public double? SatzEinspCt;

            /// <summary>Überschreibwert des Eigenstromsatzes [ct/kWh]; <c>null</c> =
            /// Projektsatz <c>KwkgBonus</c>.</summary>
            public double? SatzEigenCt;

            /// <summary>Vbh-Kontingent dieser Anlage [h]; <c>null</c> = Projektwert.</summary>
            public double? VbhKontingent;

            /// <summary>Jahresdeckel-Override dieser Anlage [h/a]; <c>null</c> oder 0 =
            /// Projekt-Override, sonst die Staffel des § 8 Abs. 4.</summary>
            public double? VbhDeckel;

            /// <summary>true, wenn diese Anlage überhaupt eine eigene E6-Angabe trägt —
            /// die Bedingung, unter der die Rechnung von der Projektvorgabe abweicht.</summary>
            public bool HatEigeneAngabe
            {
                get
                {
                    return Stichtag.HasValue || Inbetriebnahme.HasValue ||
                           SatzEinspCt.HasValue || SatzEigenCt.HasValue ||
                           VbhKontingent.HasValue || VbhDeckel.HasValue;
                }
            }

            // ---------------- ETAPPE B3 Paket a — Steuerwahl je Anlage ----------------
            //
            // Beide sind leer-fähig, und leer heißt „kein eigener Wert, es gilt der
            // Projektwert" — dasselbe Rückfallmuster wie bei den acht E6-Angaben darüber
            // und derselbe Grund: In jeder Datenbank vor Migrationsschritt 61 sind sie
            // leer, und dann rechnet die Steuerreihe Zeile für Zeile wie vorher.

            /// <summary><c>Tab_Energieanlagen.Energiesteuer_Wahl</c>, Steuerwert
            /// <c>DbWerte.ENERGIESTEUER_WAHL_*</c>; leer = Projektwahl (BF6).</summary>
            public string EnergiesteuerWahl = "";

            /// <summary><c>Tab_Energieanlagen.Aufteilung_Methode</c>, Steuerwert
            /// <c>DbWerte.AUFTEILUNG_*</c>; leer = Projektmethode.</summary>
            public string AufteilungMethode = "";

            /// <summary>true, wenn diese Anlage eine eigene Steuerwahl trägt — die
            /// Bedingung, unter der Kesselzeilen überhaupt in die Steuerprüfung
            /// aufgenommen werden (siehe <c>BaueSteuerEingabe</c>).</summary>
            public bool HatEigeneSteuerwahl
            {
                get { return !string.IsNullOrEmpty(EnergiesteuerWahl); }
            }

            // ---------------- ETAPPE B3 Paket b — Hilfsenergie je Anlage ----------------

            /// <summary>
            /// <c>Tab_Energieanlagen.Hilfsenergie_Anteil</c> [% des Energieeinsatzes
            /// dieser Anlage, Konzept § 4.5 Weg B]. <c>null</c> oder 0 = keine
            /// Hilfsenergie — der Wert, der nichts auslöst, und damit derselbe
            /// Rückfall wie bei allen Angaben darüber. Die MENGE bildet allein
            /// <see cref="HilfsstromRechner.MengeMWh"/>.
            /// </summary>
            public double? HilfsenergieAnteil;
        }

        /// <summary>
        /// Aufteilung der BHKW-Anlagen eines Projekts in förderfähige und ausgeschlossene —
        /// samt der bereinigten Bezugsgrößen der Zuschlagsrechnung.
        ///
        /// <para><b>Zwei Ausschlussgründe, eine Bilanz.</b> Ausgeschlossen wird eine Anlage,
        /// wenn sie über der Ausschreibungsgrenze liegt <b>oder</b> mit Heizöl läuft (und der
        /// Ölausschluss für dieses Projekt überhaupt greift). Die Gründelisten dürfen sich
        /// überschneiden — die Summen <see cref="PelFoerderfaehigKW"/> und
        /// <see cref="StromFoerderfaehigMWh"/> entstehen dagegen aus einem einzigen Durchlauf
        /// über die Anlagen, sodass eine doppelt betroffene Anlage genau einmal fehlt.</para>
        ///
        /// <para><see cref="Bestimmbar"/> = false heißt: Anlagen- und Ergebnismodulzeilen
        /// ließen sich nicht paaren (kein Anlagenbestand, keine Modulzeilen, oder Namen und
        /// Anzahl passen nicht zusammen). Dann bleibt nur die Projektsumme — der Weg bis zu
        /// diesem Nachtrag. Er ist konservativ: Er schließt eher zu viel aus als zu wenig.</para>
        /// </summary>
        private sealed class KwkgAnlagenauswahl
        {
            public bool Bestimmbar;
            public double PelGesamtKW;
            public double PelFoerderfaehigKW;
            public double StromGesamtMWh;
            public double StromFoerderfaehigMWh;

            /// <summary>Zahl der ausgeschlossenen Anlagen — je Anlage EINS, gleich wie viele
            /// Gründe auf sie zutreffen. Maßgeblich für die Bereinigung der Bezugsgrößen.</summary>
            public int AnzahlAusgeschlossen;

            /// <summary>Anlagen über der Ausschreibungsgrenze, als Klartext „Bezeichner (n kW)".</summary>
            public readonly List<string> UeberGrenze = new List<string>();

            /// <summary>
            /// <b>Alle</b> ölbetriebenen Anlagen, unabhängig davon, ob der Ausschluss greift —
            /// die Grundlage des Hinweises „Öl-BHKW ohne Inbetriebnahmedatum".
            /// </summary>
            public readonly List<string> MitHeizoel = new List<string>();

            /// <summary>
            /// Die Anlagen, die <b>wegen Heizöl</b> ausgeschlossen sind und <b>nicht schon</b>
            /// über der Ausschreibungsgrenze liegen. Genau diese Teilmenge nennt die
            /// Heizöl-Meldung — so steht keine Anlage in zwei Meldungen desselben Hinweises.
            /// </summary>
            public readonly List<string> NurHeizoel = new List<string>();

            /// <summary>Anteil der förderfähigen Anlagen an der Stromerzeugung [0…1].</summary>
            public double StromanteilFoerderfaehig
            {
                get { return StromGesamtMWh > 0 ? StromFoerderfaehigMWh / StromGesamtMWh : 0; }
            }

            /// <summary>Elektrische Vbh der förderfähigen Anlagen [h/a], leistungsgewichtet.</summary>
            public double VbhFoerderfaehig
            {
                get
                {
                    return PelFoerderfaehigKW > 0
                        ? StromFoerderfaehigMWh * 1000.0 / PelFoerderfaehigKW : 0;
                }
            }

            /// <summary>Eine Gründeliste als Aufzählung für die Meldung.</summary>
            public string Klartext(List<string> anlagen)
            {
                return string.Join(", ", anlagen.ToArray());
            }

            // ---------------- ETAPPE E6 ----------------

            /// <summary>Die Anlagenzeilen in Lesereihenfolge — Grundlage der Reihe je Modul.</summary>
            public readonly List<BhkwAnlage> Anlagen = new List<BhkwAnlage>();

            /// <summary>Die zugeordnete Ergebnis-Modulzeile je Anlage (<c>null</c> = keine).</summary>
            public ErgebnisBHKWModulModel[] Module = new ErgebnisBHKWModulModel[0];

            /// <summary>true je Anlage, wenn KEIN Ausschlussgrund auf sie zutrifft.</summary>
            public bool[] Foerderfaehig = new bool[0];

            /// <summary>
            /// Fertige Meldungen zu Anlagen, die an <b>Stichtag oder Realisierungsfrist</b>
            /// des § 6 gescheitert sind (Etappe E6). Sie stehen einzeln statt als
            /// Aufzählung, weil jede ihr eigenes Datum nennt.
            /// </summary>
            public readonly List<string> Fristmeldungen = new List<string>();

            /// <summary>
            /// Ölbetriebene Anlagen ohne wirksames Inbetriebnahmedatum — sie werden NICHT
            /// ausgeschlossen (der Ausschluss gilt nur für Neuanlagen), aber der Anwender
            /// muss wissen, dass das Ergebnis am fehlenden Datum hängt. Bis E5 hing diese
            /// Meldung am Projektdatum; seit E6 am Datum der jeweiligen Anlage.
            /// </summary>
            public readonly List<string> OelOhneIbn = new List<string>();

            /// <summary>true, wenn mindestens eine Anlage eine eigene E6-Angabe trägt.</summary>
            public bool MitEigenerAngabe;
        }

        /// <summary>
        /// Die Ausschreibungsgrenze [kW el] des Förderjahres aus dem Gesetzeskatalog
        /// (<c>KWKG_AUSSCHREIBUNG_GRENZE_KW</c>, Etappe E1). Fehlt der Schlüssel — jede
        /// Datenbank, deren Katalog vor diesem Nachtrag eingesät wurde —, gilt
        /// <see cref="KWKG_MAX_LEISTUNG_KW"/> mit demselben Wert.
        /// </summary>
        private double AusschreibungsgrenzeKW(int jahr)
        {
            try
            {
                if (_gesetze == null) _gesetze = new GesetzKatalog();
                double? katalog = _gesetze.Wert(DbWerte.GESETZ_KWKG_AUSSCHREIBUNG_GRENZE, jahr);
                if (katalog.HasValue && katalog.Value > 0) return katalog.Value;
            }
            catch { }
            return KWKG_MAX_LEISTUNG_KW;
        }

        /// <summary>
        /// Prüft JEDE BHKW-Anlage des Projekts einzeln gegen die Ausschreibungsgrenze und —
        /// wenn <paramref name="heizoelAusschliessen"/> gilt — gegen den Heizöl-Ausschluss,
        /// und bildet die um die ausgeschlossenen Anlagen bereinigten Bezugsgrößen.
        /// </summary>
        /// <param name="p">
        /// Projektparameter — Stichtag, Inbetriebnahme und Sätze wirken als <b>Vorgabe</b>
        /// für jede Anlage ohne eigenen Wert (Etappe E6).
        /// </param>
        /// <param name="foerderbeginn">
        /// Stichtagsjahr des PROJEKTS; es gilt für jede Anlage ohne eigenes
        /// Inbetriebnahmedatum.
        /// </param>
        /// <param name="grenzeKW">
        /// Ausschreibungsgrenze des Projektstichtagsjahres — sie gilt für Anlagen ohne
        /// eigenes Datum und für den Ersatzweg. Anlagen mit eigenem Datum schlagen ihre
        /// Grenze mit dem eigenen Jahr im Katalog nach.
        /// </param>
        private KwkgAnlagenauswahl Anlagenauswahl(VariantenDaten v, WirtschaftlichkeitParameter p,
                                                  int foerderbeginn, double grenzeKW)
        {
            var a = new KwkgAnlagenauswahl();
            a.PelGesamtKW = PelKW(v.IdProjekt);
            a.StromGesamtMWh = v.Ergebnis != null && v.Ergebnis.BHKW != null
                             ? v.Ergebnis.BHKW.Stromproduktion : 0;

            List<BhkwAnlage> anlagen = BhkwAnlagen(v.IdProjekt);
            List<ErgebnisBHKWModulModel> module = v.Ergebnis != null && v.Ergebnis.BHKW != null
                                                ? v.Ergebnis.BHKW.Module : null;
            if (anlagen.Count == 0 || module == null || module.Count == 0) return a;

            ErgebnisBHKWModulModel[] zuordnung = ModulJeAnlage(anlagen, module);
            if (zuordnung == null) return a;

            a.Bestimmbar = true;
            a.PelGesamtKW = 0;
            a.StromGesamtMWh = 0;
            a.Anlagen.AddRange(anlagen);
            a.Module = zuordnung;
            a.Foerderfaehig = new bool[anlagen.Count];

            for (int i = 0; i < anlagen.Count; i++)
            {
                BhkwAnlage anl = anlagen[i];
                a.PelGesamtKW += anl.PelKW;
                a.StromGesamtMWh += StromVon(zuordnung[i]);
                if (anl.HatEigeneAngabe) a.MitEigenerAngabe = true;

                // Der Bezeichner ist ein Datenwert, kein Anzeigetext; die Klammer mit dem
                // Einheitenzeichen kommt ohne Wortbestand aus und bleibt deshalb im Code
                // (Drei-Schichten-Regel, wie die typografischen Marken).
                string klartext = anl.Bezeichner + " (" + anl.PelKW.ToString("N0") + " kW)";

                // ETAPPE E6 — die wirksamen Daten DIESER Anlage: eigener Wert, sonst
                // Projektvorgabe. Genau dieser Rückfall hält Bestandsprojekte unverändert.
                DateTime? stichtag = anl.Stichtag ?? p.KwkgStichtag;
                DateTime? ibn = anl.Inbetriebnahme ?? p.KwkgInbetriebnahme;
                int jahr = ibn.HasValue ? ibn.Value.Year : foerderbeginn;
                double grenzeAnlage = anl.Inbetriebnahme.HasValue
                                    ? AusschreibungsgrenzeKW(jahr) : grenzeKW;

                bool ueberGrenze = anl.PelKW > grenzeAnlage;
                bool oel = anl.Heizoel;
                // Der Heizöl-Ausschluss gilt nur für erkennbare NEUANLAGEN. Maßgeblich ist
                // seit E6 das Inbetriebnahmedatum DIESER Anlage (mit Projektvorgabe als
                // Rückfall) — vorher entschied ein einziges Projektdatum für alle zugleich.
                bool oelAusschluss = oel && ibn.HasValue && ibn.Value.Year >= 2025;

                // § 6 KWKG je Anlage (Etappe E6): Stichtag und Realisierungsfrist.
                bool nachStichtag = stichtag.HasValue && stichtag.Value.Date > KWKG_STICHTAG_ENDE;
                bool nachFrist = false;
                DateTime fristende = DateTime.MinValue;
                if (!nachStichtag && stichtag.HasValue && ibn.HasValue)
                {
                    fristende = new DateTime(stichtag.Value.Year + KWKG_REALISIERUNG_JAHRE, 12, 31);
                    nachFrist = ibn.Value.Date > fristende;
                }

                if (ueberGrenze) a.UeberGrenze.Add(klartext);
                if (oel) a.MitHeizoel.Add(klartext);
                if (oelAusschluss && !ueberGrenze) a.NurHeizoel.Add(klartext);
                if (oel && !ibn.HasValue) a.OelOhneIbn.Add(klartext);
                if (nachStichtag)
                    a.Fristmeldungen.Add(string.Format(MyResource.Resource.WIRT_KWKG_ANLAGE_STICHTAG,
                                                       klartext, KWKG_STICHTAG_ENDE.ToString("dd.MM.yyyy")));
                else if (nachFrist)
                    a.Fristmeldungen.Add(string.Format(MyResource.Resource.WIRT_KWKG_ANLAGE_FRIST,
                                                       klartext, fristende.ToString("dd.MM.yyyy")));

                // EIN Ausschluss je Anlage, gleich wie viele Gründe zutreffen — sonst
                // fehlte eine mehrfach betroffene Anlage mehrfach in den Bezugsgrößen.
                if (ueberGrenze || oelAusschluss || nachStichtag || nachFrist)
                {
                    a.AnzahlAusgeschlossen++;
                }
                else
                {
                    a.Foerderfaehig[i] = true;
                    a.PelFoerderfaehigKW += anl.PelKW;
                    a.StromFoerderfaehigMWh += StromVon(zuordnung[i]);
                }
            }
            return a;
        }

        /// <summary>
        /// Ordnet jeder Anlagenzeile ihre Stromerzeugung aus den Ergebnis-Modulzeilen zu.
        /// Erster Weg ist der BEZEICHNER (<c>SimulationRunner</c> schreibt ihn als
        /// <c>Modul</c>), zweiter Weg die Reihenfolge bei gleicher Anzahl — Modulzeilen
        /// entstehen in der Reihenfolge von <c>SimulationControl.BHKW_Liste_Laden</c>,
        /// der Bezeichner kann sich seit dem Lauf aber geändert haben.
        /// <c>null</c> = nicht zuordenbar.
        ///
        /// <para><b>ETAPPE E4: liefert die MODULZEILE statt nur der Strommenge.</b> Die
        /// Steuerrechnung braucht aus derselben Zeile zusätzlich Brennstoffverbrauch,
        /// Wärmeproduktion und Energieträger. Das Zuordnungsverfahren ist Zeile für Zeile
        /// unverändert — es gab keinen Grund, dafür eine zweite Fassung anzulegen.</para>
        /// </summary>
        private static ErgebnisBHKWModulModel[] ModulJeAnlage(List<BhkwAnlage> anlagen,
                                                              List<ErgebnisBHKWModulModel> module)
        {
            var treffer = new ErgebnisBHKWModulModel[anlagen.Count];
            bool[] belegt = new bool[module.Count];
            int getroffen = 0;

            for (int i = 0; i < anlagen.Count; i++)
                for (int j = 0; j < module.Count; j++)
                {
                    if (belegt[j]) continue;
                    string name = module[j].Modul == null ? "" : module[j].Modul.Trim();
                    if (!string.Equals(name, anlagen[i].Bezeichner, StringComparison.OrdinalIgnoreCase))
                        continue;
                    belegt[j] = true;
                    treffer[i] = module[j];
                    getroffen++;
                    break;
                }
            if (getroffen == anlagen.Count) return treffer;

            if (anlagen.Count != module.Count) return null;
            for (int i = 0; i < anlagen.Count; i++) treffer[i] = module[i];
            return treffer;
        }

        /// <summary>Stromproduktion einer zugeordneten Modulzeile [MWh/a]; eine nicht
        /// getroffene Zeile zählt wie bisher mit 0.
        ///
        /// <para><b>BRUTTO</b> — die Erzeugung an der Klemme. Was davon nach Abzug des
        /// Hilfsstroms zuschlagsfähig bleibt, bildet
        /// <see cref="HilfsstromDesProjekts"/> (Etappe B3 Paket b).</para></summary>
        private static double StromVon(ErgebnisBHKWModulModel m)
        {
            return m == null ? 0 : m.Stromproduktion;
        }

        /// <summary>Brennstoffeinsatz einer zugeordneten BHKW-Modulzeile [MWh/a] — die
        /// ENDENERGIE dieser Anlage und damit die Bemessungsgrundlage des Hilfsstroms
        /// (Konzept § 4.5). Dieselbe Größe, die <see cref="BaueSteuerAnlage"/> als
        /// <c>SteuerAnlage.BrennstoffMWh</c> ansetzt — es gibt nur eine.</summary>
        private static double BrennstoffVon(ErgebnisBHKWModulModel m)
        {
            return m == null ? 0 : m.Verbrauch;
        }

        /// <summary>
        /// ETAPPE B3 Paket b — <b>der Hilfsstrom eines Projekts, je Anlage und in
        /// Summe</b>. Das Ergebnis dieser Klasse ist der EINE Netto-Ort: Jeder Pfad der
        /// KWKG-Rechnung bezieht seine geminderten Mengen aus ihr, keiner rechnet sie
        /// selbst.
        /// </summary>
        private sealed class HilfsstromSatz
        {
            /// <summary>Hilfsstrom je Anlagenzeile [MWh/a], in der Lesereihenfolge von
            /// <see cref="BhkwAnlagen"/> — und damit indexgleich zu
            /// <see cref="KwkgAnlagenauswahl.Anlagen"/>.</summary>
            public double[] JeAnlage = new double[0];

            /// <summary>Summe über ALLE Anlagen des Projekts [MWh/a] — auch über die
            /// nicht förderfähigen: Hilfsstrom verbraucht die Anlage unabhängig davon,
            /// ob ihr Strom einen Zuschlag bekommt.</summary>
            public double GesamtMWh;

            /// <summary>true, sobald irgendeine Anlage einen Anteil &gt; 0 trägt.</summary>
            public bool Gepflegt;

            /// <summary>false = ein Anteil ist gepflegt, aber Anlagen- und Modulzeilen
            /// ließen sich nicht paaren. Dann bleibt alles brutto, und der Anwender
            /// bekommt eine Meldung statt einer stillen Null.</summary>
            public bool Zuordenbar = true;
        }

        /// <summary>
        /// Bildet den Hilfsstrom aller BHKW-Anlagen eines Projekts (Konzept § 4.3):
        /// <c>Hilfsenergie_Anteil × Brennstoff der zugeordneten Modulzeile</c>, über
        /// <see cref="HilfsstromRechner.MengeMWh"/> — dieselbe Funktion, die beim
        /// Speichern eines Laufs die Ergebnisspalte <c>Hilfsenergie</c> füllt.
        ///
        /// <para><b>Frisch statt gelesen.</b> Die persistierte Spalte wird bewusst NICHT
        /// verwendet: Sie trägt den Stand des Laufs, an dem gespeichert wurde, und der
        /// Anteil an der Anlage kann sich seither geändert haben (Konzept § 4.5, „die
        /// Menge ist ein Ergebniswert").</para>
        ///
        /// <para><b>Die Zuordnung ist dieselbe wie überall</b>
        /// (<see cref="ModulJeAnlage"/>) — und sie scheitert unter genau denselben
        /// Bedingungen wie <see cref="Anlagenauswahl"/>. Ist sie nicht möglich, bleibt
        /// der Hilfsstrom 0 und <see cref="HilfsstromSatz.Zuordenbar"/> false; die
        /// KWKG-Rechnung läuft dann ohnehin auf ihrem projektweiten Ersatzweg, der
        /// keine Anlagen kennt.</para>
        /// </summary>
        private HilfsstromSatz HilfsstromDesProjekts(VariantenDaten v)
        {
            var h = new HilfsstromSatz();
            if (v == null) return h;

            List<BhkwAnlage> anlagen = BhkwAnlagen(v.IdProjekt);
            h.JeAnlage = new double[anlagen.Count];

            foreach (BhkwAnlage a in anlagen)
                if (a.HilfsenergieAnteil.HasValue && a.HilfsenergieAnteil.Value > 0)
                { h.Gepflegt = true; break; }
            if (!h.Gepflegt) return h;    // nichts gepflegt: alles 0, nichts zu melden

            List<ErgebnisBHKWModulModel> module = v.Ergebnis != null && v.Ergebnis.BHKW != null
                                                ? v.Ergebnis.BHKW.Module : null;
            if (anlagen.Count == 0 || module == null || module.Count == 0)
            { h.Zuordenbar = false; return h; }

            ErgebnisBHKWModulModel[] zuordnung = ModulJeAnlage(anlagen, module);
            if (zuordnung == null) { h.Zuordenbar = false; return h; }

            for (int i = 0; i < anlagen.Count; i++)
            {
                h.JeAnlage[i] = HilfsstromRechner.MengeMWh(anlagen[i].HilfsenergieAnteil,
                                                           BrennstoffVon(zuordnung[i]));
                h.GesamtMWh += h.JeAnlage[i];
            }
            return h;
        }

        /// <summary>BHKW-Anlagenzeilen des Projekts, einmal je Berechne-Lauf gelesen.</summary>
        private List<BhkwAnlage> BhkwAnlagen(int idProjekt)
        {
            List<BhkwAnlage> liste;
            if (_anlagenCache.TryGetValue(idProjekt, out liste)) return liste;
            liste = LiesAnlagen(idProjekt, WizardItemClass.BHKW_TYP);
            _anlagenCache[idProjekt] = liste;
            return liste;
        }

        /// <summary>
        /// ETAPPE B3 Paket a — HEIZKESSEL-Anlagenzeilen des Projekts, einmal je
        /// Berechne-Lauf gelesen. Eigener Cache statt eines zusammengesetzten Schlüssels:
        /// Der BHKW-Cache wird an mehreren Stellen als „die Anlagen" geleert und gelesen,
        /// und ein Dictionary, in dem zwei Anlagenarten unter einem Zahlenschlüssel
        /// liegen, wäre genau die Verwechslung, die keiner sucht.
        /// </summary>
        private List<BhkwAnlage> KesselAnlagen(int idProjekt)
        {
            List<BhkwAnlage> liste;
            if (_kesselCache.TryGetValue(idProjekt, out liste)) return liste;
            liste = LiesAnlagen(idProjekt, WizardItemClass.KESSEL_TYP);
            _kesselCache[idProjekt] = liste;
            return liste;
        }

        /// <summary>
        /// Bezeichner, elektrische Nennleistung und Brennstoffart je BHKW-ANLAGENZEILE des
        /// Projekts — dieselbe Menge, die auch <see cref="LiesBhkwLeistungKW"/> summiert und
        /// die die Engine rechnet (<c>Tab_Energieanlagen</c> ⋈ <c>Tab_BHKW</c>). Leere Liste,
        /// wenn das Projekt keine Anlagenzeile führt oder die Abfrage scheitert.
        ///
        /// <para><b>Die Brennstoffart hat zwei Quellen, in dieser Reihenfolge</b>
        /// (Nachtrag 2 zu E2):</para>
        /// <list type="number">
        ///   <item><description><c>Tab_Energieanlagen.ID_Carrier</c> → <c>energy_carrier</c>
        ///     → <c>Tab_Brennstoff_Stamm.ID_Kategorie</c>. Der Energieträger hängt an der
        ///     ANLAGE und ist seit dem Energieträger-Umbau die maßgebliche Zuordnung: Aus ihm
        ///     bildet die Anwendung Brennstoffkosten und Emissionen
        ///     (<c>SimulationControl.EnergietraegerZuordnungLesen</c>,
        ///     <c>KostenEmissionRechner</c>).</description></item>
        ///   <item><description><c>Tab_BHKW.Brennstoff</c> →
        ///     <c>Tab_Brennstoff_Stamm.ID_Kategorie</c> — der Weg des Altstands. Er greift,
        ///     wenn die Anlage keinen Energieträger trägt (<c>ID_Carrier</c> NULL oder 0),
        ///     wenn der Träger im Katalog fehlt oder wenn die Tabelle <c>energy_carrier</c>
        ///     in einer alten Datenbank gar nicht existiert. Im Bestand vom 19.08.2026 ist
        ///     das kein Randfall: Die BHKW-Anlage des Projekts 1017 führt keinen
        ///     Energieträger.</description></item>
        /// </list>
        ///
        /// <para><b>Warum nicht umgekehrt.</b> <c>Tab_BHKW</c> trägt den Brennstoff des
        /// KATALOGGERÄTS. Wechselt der Anwender den Energieträger der Anlage, bleibt die
        /// Gerätezeile stehen — der Trägerverweis ist dann die jüngere und für Kosten,
        /// Emissionen und Bericht bereits maßgebliche Aussage.</para>
        ///
        /// <para><b>ETAPPE B3 Paket a: derselbe Leser auch für HEIZKESSEL</b>
        /// (<paramref name="idType"/> = <c>WizardItemClass.KESSEL_TYP</c>). § 54 EnergieStG
        /// hängt an keiner KWK-Anlage (BF5), die Kesselzeilen brauchen deshalb dieselben
        /// Angaben: Bezeichner, Energieträger, Brennstoffart und die Steuerwahl. Was es
        /// beim Kessel nicht gibt, ist die elektrische Nennleistung — sie kommt als 0
        /// zurück, und mit ihr fällt die Anlage aus jeder stromseitigen Bezugsgröße
        /// heraus, ohne sie zu verfälschen. Ein zweiter, fast wortgleicher Leser wäre die
        /// schlechtere Lösung gewesen: Die E6- und B3a-Fähigkeitsproben müssten dann
        /// zweimal gepflegt werden.</para>
        /// </summary>
        private List<BhkwAnlage> LiesAnlagen(int idProjekt, int idType)
        {
            // ETAPPE E6: Zuerst mit den acht neuen Spalten. Fehlen sie (Datenbank vor
            // Migrationsschritt 22, in der auch StelleTabellenSicher nie lief), scheitert
            // die Abfrage — dann greift dieselbe Abfrage ohne sie, und jede E6-Angabe
            // bleibt leer. Das ist genau der Zustand, in dem überall der Projektwert gilt.
            //
            // Erkannt wird das am ERGEBNIS, nicht an einer Ausnahme: DataRepository
            // liefert bei einem SQL-Fehler eine LEERE DataTable statt zu werfen (und
            // meldet still, weil die Abfrage im Engine-Modus läuft). Ein Blick auf die
            // Spaltenliste ist deshalb die einzige verlässliche Unterscheidung zwischen
            // „Abfrage lief, Projekt hat keine solche Anlage" und „Spalten fehlen".
            //
            // ETAPPE B3 Paket a: dieselbe Treppe eine Stufe tiefer. Zuerst mit E6 UND den
            // zwei Steuerspalten, dann nur mit E6, dann ohne beides. Die Zwischenstufe ist
            // kein Papierfall: Eine Datenbank auf Schema 22..60 hat die E6-Spalten und die
            // B3a-Spalten nicht.
            DataTable dt = AnlagenTabelle(idProjekt, idType, true, true);
            bool mitB3a = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_ENERGIESTEUER_WAHL);
            bool mitE6 = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_KWKG_STICHTAG);
            if (!mitB3a)
            {
                dt = AnlagenTabelle(idProjekt, idType, true, false);
                mitE6 = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_KWKG_STICHTAG);
            }
            if (!mitE6) dt = AnlagenTabelle(idProjekt, idType, false, false);

            var liste = new List<BhkwAnlage>();
            if (dt == null) return liste;
            try
            {
                foreach (DataRow r in dt.Rows)
                {
                    var anl = new BhkwAnlage();
                    anl.Bezeichner = r["Bezeichner"] == DBNull.Value
                                   ? "" : Convert.ToString(r["Bezeichner"]).Trim();
                    anl.PelKW = r["Pel"] == DBNull.Value ? 0 : Convert.ToDouble(r["Pel"]);
                    anl.IdCarrier = Ganzzahl(r, "ID_Carrier");
                    anl.IdAnlage = Ganzzahl(r, "ID");
                    anl.IdProjekt = Ganzzahl(r, "ID_Projekt");
                    anl.IdBrennstoff = BrennstoffId(anl.IdCarrier, Ganzzahl(r, "Brennstoff"));
                    anl.Heizoel = BrennstoffKategorie(anl.IdCarrier, Ganzzahl(r, "Brennstoff"))
                                  == BRENNSTOFF_KATEGORIE_OEL;

                    if (mitE6)
                    {
                        anl.Stichtag = Datum(r, SchemaKatalog.SPALTE_EA_KWKG_STICHTAG);
                        anl.Inbetriebnahme = Datum(r, SchemaKatalog.SPALTE_EA_KWKG_INBETRIEBNAHME);
                        anl.Anlagenart = Text(r, SchemaKatalog.SPALTE_EA_KWKG_ANLAGENART) ?? "";
                        anl.Eigenfall = Text(r, SchemaKatalog.SPALTE_EA_KWKG_EIGENFALL) ?? "";
                        anl.SatzEinspCt = D(r, SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP);
                        anl.SatzEigenCt = D(r, SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN);
                        anl.VbhKontingent = D(r, SchemaKatalog.SPALTE_EA_KWKG_KONTINGENT);
                        anl.VbhDeckel = D(r, SchemaKatalog.SPALTE_EA_KWKG_DECKEL);
                    }

                    if (mitB3a)
                    {
                        // Text() liefert "" für NULL und für die fehlende Spalte — und ""
                        // heißt hier durchgehend „kein eigener Wert, es gilt der
                        // Projektwert" (Etappe B3 Paket a).
                        anl.EnergiesteuerWahl = Text(r, SchemaKatalog.SPALTE_EA_ENERGIESTEUER_WAHL) ?? "";
                        anl.AufteilungMethode = Text(r, SchemaKatalog.SPALTE_EA_AUFTEILUNG_METHODE) ?? "";

                        // ETAPPE B3 Paket b: D() liefert null für NULL und für die
                        // fehlende Spalte — und null heißt hier „keine Hilfsenergie".
                        anl.HilfsenergieAnteil = D(r, SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL);
                    }
                    liste.Add(anl);
                }
            }
            catch { liste.Clear(); }
            return liste;
        }

        /// <summary>
        /// Die Anlagenabfrage — mit oder ohne die acht E6-Spalten und mit oder ohne die
        /// zwei Steuerspalten aus B3a. <c>null</c> = die Abfrage ist gescheitert (bei
        /// gesetzten Flags in aller Regel, weil die Spalten fehlen).
        ///
        /// <para><b>Zwei Gerätewelten, eine Abfrage</b> (B3a): Beim BHKW liefert
        /// <c>Tab_BHKW</c> die elektrische Nennleistung und den Katalogbrennstoff, beim
        /// Heizkessel <c>Tab_Heizkessel</c> nur den Brennstoff — <c>Pel</c> ist dort
        /// konstant 0, weil ein Kessel keine elektrische Nennleistung hat und keine der
        /// stromseitigen Prüfungen ihn je meinen darf. Der Kessel-Join ist ein
        /// <c>LEFT JOIN</c>: Eine Anlagenzeile ohne <c>ID_Kessel</c> soll ihre Steuerwahl
        /// behalten, nicht aus der Liste fallen. Beim BHKW bleibt es beim <c>INNER
        /// JOIN</c> des Bestands — dort hängt die 2-MW-Prüfung an <c>Pel</c>, und eine
        /// Zeile ohne Gerät hätte keine.</para>
        /// </summary>
        private static DataTable AnlagenTabelle(int idProjekt, int idType, bool mitE6, bool mitB3a)
        {
            string e6 = mitE6
                ? ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_STICHTAG + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_INBETRIEBNAHME + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_ANLAGENART + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_EIGENFALL + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EINSP + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_SATZ_EIGEN + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_KONTINGENT + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_DECKEL + "]"
                : "";
            // Paket b nimmt den Hilfsenergieanteil in DIESELBE Fähigkeitsstufe: Alle drei
            // Spalten entstehen zusammen in Migrationsschritt 61a, eine Datenbank hat
            // also entweder alle drei oder keine.
            string b3a = mitB3a
                ? ", a.[" + SchemaKatalog.SPALTE_EA_ENERGIESTEUER_WAHL + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_AUFTEILUNG_METHODE + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_HILFSENERGIE_ANTEIL + "]"
                : "";

            bool bhkw = idType == WizardItemClass.BHKW_TYP;
            string geraet = bhkw ? "b.Pel, b.Brennstoff" : "0 AS Pel, b.Brennstoff";
            string join = bhkw
                ? "INNER JOIN Tab_BHKW AS b ON a.ID_BHKW = b.ID "
                : "LEFT JOIN Tab_Heizkessel AS b ON a.ID_Kessel = b.ID ";
            try
            {
                using (DataRepository.EngineModus())
                    return DataRepository.GetDataTable(
                        "SELECT a.ID, a.ID_Projekt, a.Bezeichner, a.ID_Carrier, " +
                        geraet + e6 + b3a + " " +
                        "FROM Tab_Energieanlagen AS a " + join +
                        // KEIN ORDER BY — bewusst. Die Zuordnung Anlage ↔ Ergebnismodul
                        // fällt bei nicht passenden Bezeichnern auf die REIHENFOLGE
                        // zurück, und die Modulzeilen entstehen in der Reihenfolge von
                        // SimulationControl.BHKW_Liste_Laden bzw. SPK_Liste_Laden, die
                        // beide ebenfalls ohne ORDER BY lesen. Eine Sortierung hier
                        // könnte beide auseinanderlaufen lassen.
                        "WHERE a.ID_Projekt = ? AND a.ID_Type = " +
                        idType.ToString(System.Globalization.CultureInfo.InvariantCulture),
                        new DbParam("@p", idProjekt));
            }
            catch { return null; }
        }

        /// <summary>Datumsspalte einer Zeile; NULL, fehlende Spalte und Lesefehler ergeben
        /// <c>null</c> („kein eigener Wert" — dann gilt der Projektwert).</summary>
        private static DateTime? Datum(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDateTime(r[spalte]); } catch { return null; }
        }

        /// <summary>Ganzzahlspalte einer Zeile; NULL und Lesefehler ergeben 0.</summary>
        private static int Ganzzahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte]); } catch { return 0; }
        }

        /// <summary>
        /// Brennstoffkategorie einer Anlage (<c>Tab_BrennstoffKategorien.ID</c>) —
        /// vorrangig über den Energieträger der Anlage, ersatzweise über den Brennstoff der
        /// Gerätezeile. 0 = nicht ermittelbar (dann gilt die Anlage als nicht ölbetrieben,
        /// wie im Altstand: <c>BhkwMitHeizoel</c> zählte nur Zeilen mit gültigem Verbund).
        /// </summary>
        private int BrennstoffKategorie(int idCarrier, int idBrennstoff)
        {
            int bs = BrennstoffId(idCarrier, idBrennstoff);
            int kategorie;
            return bs > 0 && _brennstoffKategorie.TryGetValue(bs, out kategorie) ? kategorie : 0;
        }

        /// <summary>
        /// Der maßgebliche <c>Tab_Brennstoff_Stamm.ID</c> einer Anlage (0 = nicht
        /// ermittelbar) — vorrangig über den Energieträger der ANLAGE, ersatzweise über
        /// den Brennstoff der Gerätezeile.
        ///
        /// <para><b>ETAPPE E4: eine Ebene früher abgegriffen, sonst unverändert.</b> Bis
        /// dahin bildete <see cref="BrennstoffKategorie"/> beide Stufen selbst. Die
        /// Bedingung der ersten Stufe ist wortgleich geblieben — der Trägerweg zählt nur,
        /// wenn er bis zu einer bekannten <b>Kategorie</b> durchläuft; sonst greift die
        /// Gerätezeile. Damit liefert <see cref="BrennstoffKategorie"/> Zeile für Zeile
        /// dasselbe wie vorher, und E4 kann zusätzlich den Brennstoff selbst verwenden.</para>
        /// </summary>
        private int BrennstoffId(int idCarrier, int idBrennstoff)
        {
            if (_brennstoffKategorie == null)
            {
                _brennstoffKategorie = LiesZuordnung("SELECT ID, ID_Kategorie FROM Tab_Brennstoff_Stamm");
                _carrierBrennstoff = LiesZuordnung("SELECT id, ID_Brennstoff FROM energy_carrier");
            }

            int kategorie;
            int brennstoffAusTraeger;
            if (idCarrier > 0 && _carrierBrennstoff.TryGetValue(idCarrier, out brennstoffAusTraeger)
                              && _brennstoffKategorie.TryGetValue(brennstoffAusTraeger, out kategorie))
                return brennstoffAusTraeger;

            if (idBrennstoff > 0 && _brennstoffKategorie.ContainsKey(idBrennstoff))
                return idBrennstoff;

            return 0;
        }

        /// <summary>
        /// Zweispaltige Katalogabfrage als Zuordnung Schlüssel → Wert; leere Zuordnung, wenn
        /// die Tabelle fehlt (alte Datenbank ohne <c>energy_carrier</c>) oder die Abfrage
        /// scheitert. Zeilen mit NULL in einer der beiden Spalten werden übergangen.
        /// </summary>
        private static Dictionary<int, int> LiesZuordnung(string sql)
        {
            var zuordnung = new Dictionary<int, int>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(sql);
                if (dt == null) return zuordnung;
                foreach (DataRow r in dt.Rows)
                {
                    if (r[0] == DBNull.Value || r[1] == DBNull.Value) continue;
                    try { zuordnung[Convert.ToInt32(r[0])] = Convert.ToInt32(r[1]); }
                    catch { }
                }
            }
            catch { zuordnung.Clear(); }
            return zuordnung;
        }

        /// <summary>
        /// ETAPPE E2 (Leitentscheidung L6) — die ELEKTRISCHEN Vollbenutzungsstunden des
        /// Projekts [h/a], leistungsgewichtet über alle BHKW-Module:
        /// <c>Σ Stromproduktion [MWh] × 1000 / Σ P_el [kW]</c>.
        ///
        /// <para><b>Warum diese Größe und nicht die bisherige.</b> Der KWK-Zuschlag wird je
        /// Kilowattstunde KWK-STROM gezahlt und über Vollbenutzungsstunden gedeckelt
        /// (KWKG 2025 § 8). Bis Etappe E2 stand an dieser Stelle
        /// <c>Ergebnis.BHKW.Betriebsstunden_Gesamt</c> — die SUMME THERMISCHER
        /// Vollbenutzungsstunden über alle Module
        /// (<c>SimulationBHKW.Laufzeiten[i] = Wärme_MWh[i] / P_therm[i] × 1000</c>,
        /// aufsummiert). Zwei Fehler in einem: falsche Energieart (thermisch statt
        /// elektrisch) und falsche Aggregation (Summe statt Gewichtung). Die Summe kann
        /// 8.760 h überschreiten; der Deckel griff dadurch bei Kaskaden nicht mehr, und der
        /// Zuschlag fiel systematisch zu hoch aus.</para>
        ///
        /// <para><b>Zwei Quellen, eine Formel.</b> Vorrang hat der beim Lauf berechnete und
        /// gespeicherte Wert (<c>Tab_ErgebnisBHKW.VbhElektrisch</c>, Migrationsschritt 18) —
        /// er trägt die Leistung, die ZUM ZEITPUNKT DES LAUFS installiert war, und ist
        /// damit dieselbe Zahl, die Ergebnisreiter und Bericht zeigen. Fehlt er
        /// (Ergebniszeile vor E2), wird er aus <c>Stromproduktion</c> und der heute
        /// installierten Leistung gebildet — nach derselben Formel, die der Rechenkern
        /// verwendet.</para>
        /// </summary>
        /// <param name="hinweis">
        /// != null, wenn die Größe NICHT bestimmbar ist und der Anwender das wissen muss
        /// (keine elektrische Leistung gepflegt). Kein Strom im Lauf ergibt dagegen still
        /// 0 — dann gibt es schlicht nichts zu vergüten.
        /// </param>
        /// <returns>Elektrische Vollbenutzungsstunden [h/a]; 0 = nicht bestimmbar.</returns>
        private double VbhElektrisch(VariantenDaten v, out string hinweis)
        {
            hinweis = null;
            if (v == null || v.Ergebnis == null || v.Ergebnis.BHKW == null) return 0;

            double stromMWh = v.Ergebnis.BHKW.Stromproduktion;
            if (stromMWh <= 0) return 0;   // kein KWK-Strom -> keine Vollbenutzungsstunden

            double gespeichert = v.Ergebnis.BHKW.VbhElektrisch;
            if (gespeichert > 0) return gespeichert;

            double pelKW = PelKW(v.IdProjekt);
            if (pelKW <= 0)
            {
                hinweis = "KWKG: keine elektrische Nennleistung der BHKW gepflegt (Tab_BHKW.Pel) — " +
                          "die elektrischen Vollbenutzungsstunden sind nicht bestimmbar; Bonus = 0.";
                return 0;
            }
            return stromMWh * 1000.0 / pelKW;
        }

        /// <summary>
        /// true, wenn eine BHKW-GERÄTEZEILE des Projekts einen Öl-Brennstoff führt
        /// (<c>Tab_BHKW.Brennstoff</c> → <c>Tab_Brennstoff_Stamm.ID_Kategorie</c> =
        /// <see cref="BRENNSTOFF_KATEGORIE_OEL"/>).
        ///
        /// <para><b>NACHTRAG 2 ZU E2 — nur noch RÜCKFALLEBENE.</b> Bis dahin war das die
        /// einzige Prüfung, und sie hatte zwei Mängel in einer Zeile: Sie galt PROJEKTWEIT
        /// (eine Öl-Zeile nahm allen Anlagen den Zuschlag) und sie zählte GERÄTEZEILEN
        /// (auch solche, zu denen nie eine Anlagenzeile entstand). Maßgeblich ist jetzt die
        /// Brennstoffart je installierter Anlage aus <see cref="LiesBhkwAnlagen"/>. Diese
        /// Abfrage greift nur noch, wenn sich die Anlagen nicht bestimmen lassen
        /// (<see cref="KwkgAnlagenauswahl.Bestimmbar"/> = false) — dann sind die
        /// Gerätezeilen die einzige verfügbare Aussage, genau wie bei
        /// <see cref="LiesBhkwLeistungKW"/>, und sie ist konservativ.</para>
        /// </summary>
        private static bool BhkwMitHeizoel(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_BHKW AS b " +
                    "INNER JOIN Tab_Brennstoff_Stamm AS bs ON b.Brennstoff = bs.ID " +
                    "WHERE b.ID_Projekt = ? AND bs.ID_Kategorie = " + BRENNSTOFF_KATEGORIE_OEL,
                    new DbParam("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o) > 0;
            }
            catch { }
            return false;
        }

        /// <summary>Kapitalwertrechnung einer Eingabe, optional mit Sensitivitäts-Ausschlägen
        /// (Invest-/Energiefaktor wirken auf DIESES Projekt; Zins/Preissteigerung global).
        /// <para><b>Beide Faktoren ziehen ihre abgeleiteten Betriebskosten mit:</b> der
        /// Energiefaktor den p_E-Topf (FX4-c), der Investitionsfaktor den
        /// investgekoppelten Anteil des p_B-Topfes (FX5-a). Bei Faktor 1,0 wird jeweils
        /// gar nichts angefasst — der Regellauf ist bitgenau der von vorher.</para></summary>
        private static KapitalwertRechner.Zahlungsbild RechneBild(ProjektEingabe e, WirtschaftlichkeitParameter p,
            double zinsProzent, double preisstEnergie, double investFaktor, double energieFaktor)
        {
            List<KapitalwertRechner.InvestPosition> invest = e.Investitionen;
            double betrieb = e.Betrieb;
            IList<KeyValuePair<double, int>> betriebAbJahr = e.BetriebAbJahr;
            if (investFaktor != 1.0)
            {
                invest = new List<KapitalwertRechner.InvestPosition>();
                foreach (KapitalwertRechner.InvestPosition pos in e.Investitionen)
                    invest.Add(new KapitalwertRechner.InvestPosition
                    { Betrag = pos.Betrag * investFaktor, Nutzungsdauer = pos.Nutzungsdauer,
                      StartJahr = pos.StartJahr });   // KD6: Startjahr wandert mit (Sensitivität)

                // PAKET FX5-a (Anwenderentscheid 03.09.2026, offener Punkt FX4-1): Der
                // Ausschlag zieht die INVESTITIONSGEKOPPELTEN BETRIEBSKOSTEN mit —
                // spiegelbildlich zu FX4-c auf der Energieseite. Eine Position
                // „x % der Investitionssumme" (Wartung, Versicherung, Verwaltung; im
                // Bestand die häufigste Kategorie-2-Bemessung) IST ein Anteil der
                // Investition; kostet die Anlage 10 % mehr, kostet ihre Wartung nach
                // dieser Bemessung 10 % mehr. Bis FX4 skalierte der Faktor nur die
                // Investitionspositionen selbst.
                //
                // MODELLANNAHME, ausdrücklich: Δ Position = (f − 1) × Jahr-1-Betrag,
                // also LINEAR im Faktor. Der Betrag entsteht in BaueEingabe einmal aus
                // Investitionssumme × Satz (H4a InvestSummeFuer, stufig
                // Anlage→Komponente→Projekt); „die Investition steigt um 10 %" heißt in
                // diesem Modell, dass diese Bemessungsbasis um 10 % steigt. Neu
                // aufgelöst wird nichts — die Kostenwelt kennt den Ausschlag nicht.
                //
                // WIE skaliert wird: als ADDITIVE Korrektur auf den fertigen
                // Betriebs-Topf, betrieb = e.Betrieb + (f − 1) × Anteil. Das ist
                // rechnerisch (e.Betrieb − Anteil) + f × Anteil, ändert aber die
                // Summationsreihenfolge des Regellaufs nicht (dort wird der Zweig gar
                // nicht betreten). Der Topf bleibt im Übrigen ein p_B-Topf: (1+p_B)^(t−1)
                // läuft im Rechenkern unverändert darüber.
                //
                // OHNE AUSSCHLAG UNVERÄNDERT: investFaktor ist im Normallauf exakt 1,0 —
                // dieselbe IEEE-754-Begründung wie bei FX4-c.
                double delta = investFaktor - 1.0;
                if (e.InvestGekoppelt != 0.0)
                    betrieb = e.Betrieb + delta * e.InvestGekoppelt;
                if (e.InvestGekoppeltAbJahr != null && e.InvestGekoppeltAbJahr.Count > 0)
                {
                    // Die Startjahr-Anteile sind ebenfalls Jahr-1-Beträge, nur später
                    // fällig. Angehängt wird je Position ein KORREKTURPAAR mit demselben
                    // Startjahr — der Rechenkern summiert die Paare ohnehin nur auf
                    // (t ≥ Startjahr), also ist das wertgleich zum Skalieren der Position
                    // und kommt ohne Zuordnung Liste↔Liste aus.
                    var mitKorrektur = new List<KeyValuePair<double, int>>(
                        betriebAbJahr ?? (IList<KeyValuePair<double, int>>)
                                         new List<KeyValuePair<double, int>>());
                    foreach (KeyValuePair<double, int> vi in e.InvestGekoppeltAbJahr)
                        mitKorrektur.Add(new KeyValuePair<double, int>(delta * vi.Key, vi.Value));
                    betriebAbJahr = mitKorrektur;
                }
            }
            // ETAPPE K5: Der Zuschuss wird vom Investitionsfaktor NICHT skaliert. Die
            // Sensitivität fragt „was, wenn die Anlage 10 % mehr kostet?" — eine
            // bewilligte Förderzusage über einen festen Betrag ändert sich dadurch nicht.
            // Sie skalieren hiesse zu behaupten, der Fördergeber zahle Kostensteigerungen
            // anteilig mit; das gibt keine Zusage her.
            // ETAPPE K6: Die jahresscharfe CO₂-Reihe wird vom Energiefaktor GENAUSO
            // skaliert wie der Skalar davor — die Sensitivität „Energiekosten ±10 %
            // (inkl. CO₂-Abgabe)" fragt nach beidem zusammen, und die Zeile im Bericht
            // sagt das ausdrücklich. Ohne Pfad bleibt die Reihe null und der Rechenweg
            // ist Zeichen für Zeichen der von vorher.
            double[] behgReihe = null;
            if (e.BehgJeJahr != null)
            {
                behgReihe = new double[e.BehgJeJahr.Length];
                for (int t = 0; t < behgReihe.Length; t++)
                    behgReihe[t] = e.BehgJeJahr[t] * energieFaktor;
            }

            // PAKET FX3 (R-2): Der Endenergie-Topf geht als EIGENER Term hinein und
            // eskaliert mit preisstEnergie.
            //
            // PAKET FX4-c (Anwenderentscheid 02.09.2026, offener Punkt FX3-5): Er wird
            // vom energieFaktor der Sensitivität jetzt MITSKALIERT — genau wie die
            // Energiekosten selbst, die CO₂-Abgabe und die jahresscharfe CO₂-Reihe
            // darüber. Fachlich: Der Topf trägt Positionen, deren Betrag ein ANTEIL der
            // Endenergie(-kosten) ist; steigen die Energiekosten der Variante um 10 %,
            // steigt ein Anteil davon mit. Bis FX3 blieb er außen vor (Begründung
            // damals: die Bezugsgröße sei ein Ergebniswert des Laufs, kein
            // Preisparameter) — der Anwender hat das am 02.09.2026 anders entschieden.
            //
            // WIE skaliert wird, ist genau die Mechanik der übrigen Energiegrößen: Der
            // Faktor greift am JAHR-1-BETRAG (bei e.Energie und e.Behg ebenso), die
            // Preissteigerung (1+p_E)^(t−1) läuft unverändert darüber. Die
            // Startjahr-Anteile (KD6) werden mitskaliert, denn auch sie sind
            // Jahr-1-Beträge, nur eben später fällig.
            //
            // OHNE AUSSCHLAG UNVERÄNDERT: energieFaktor ist im Normallauf exakt 1,0 —
            // die Multiplikation mit 1,0 ist in IEEE 754 wertgleich, und die Liste wird
            // dann gar nicht erst kopiert. Die Sensitivität „Energiepreissteigerung ±"
            // wirkt wie bisher über preisstEnergie.
            IList<KeyValuePair<double, int>> endenergieAbJahr = e.EndenergieAbJahr;
            if (energieFaktor != 1.0 && endenergieAbJahr != null && endenergieAbJahr.Count > 0)
            {
                var skaliert = new List<KeyValuePair<double, int>>(endenergieAbJahr.Count);
                foreach (KeyValuePair<double, int> ve in endenergieAbJahr)
                    skaliert.Add(new KeyValuePair<double, int>(ve.Key * energieFaktor, ve.Value));
                endenergieAbJahr = skaliert;
            }

            return KapitalwertRechner.Rechne(invest, betrieb,
                (e.Energie ?? 0) * energieFaktor, e.Erloes,
                zinsProzent, p.Betrachtungszeitraum,
                p.PreissteigerungBetrieb, preisstEnergie,
                e.Behg * energieFaktor, e.ErloesReihen, e.Zuschuss, behgReihe,
                betriebAbJahr, e.Endenergie * energieFaktor, endenergieAbJahr);
        }

        /// <summary>Sensitivitätszeilen einer Variante (W2): 4 Parameter, ±Δ → KW vs. Stamm.</summary>
        private static List<SensitivitaetZeile> BaueSensitivitaet(int idProjekt, ProjektEingabe variante,
            ProjektEingabe stamm, WirtschaftlichkeitParameter p, double kwBasis)
        {
            Func<double, double, double, double, double> diff = (zins, pE, investF, energieF) =>
            {
                KapitalwertRechner.Zahlungsbild bv = RechneBild(variante, p, zins, pE, investF, energieF);
                KapitalwertRechner.Zahlungsbild bs = RechneBild(stamm, p, zins, pE, 1.0, 1.0);
                return Math.Round(bv.Kapitalwert - bs.Kapitalwert, 2);
            };
            double z = p.Zinssatz, pe = p.PreissteigerungEnergie;

            var zeilenListe = new List<SensitivitaetZeile>
            {
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Zinssatz ±" + SENS_DELTA_ZINS.ToString("0.#") + " %-Pkt",
                    KwMinus = diff(z - SENS_DELTA_ZINS, pe, 1, 1), KwBasis = kwBasis,
                    KwPlus = diff(z + SENS_DELTA_ZINS, pe, 1, 1) },
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Energiepreissteigerung ±" + SENS_DELTA_PREIS.ToString("0.#") + " %-Pkt",
                    KwMinus = diff(z, pe - SENS_DELTA_PREIS, 1, 1), KwBasis = kwBasis,
                    KwPlus = diff(z, pe + SENS_DELTA_PREIS, 1, 1) },
                // PAKET FX5-a: Der Investitions-Ausschlag skaliert seit dem 03.09.2026
                // NICHT mehr nur die Investitionspositionen, sondern auch die davon
                // abgeleiteten Betriebskosten („x % der Investitionssumme") — die
                // Mitkopplung sitzt in RechneBild, die Zeile hier ist unverändert.
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Investition Variante ±" + SENS_DELTA_INVEST.ToString("0.#") + " %",
                    KwMinus = diff(z, pe, 1.0 - SENS_DELTA_INVEST / 100.0, 1), KwBasis = kwBasis,
                    KwPlus = diff(z, pe, 1.0 + SENS_DELTA_INVEST / 100.0, 1) },
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Energiekosten Variante ±" + SENS_DELTA_ENERGIE.ToString("0.#") + " % (inkl. CO₂-Abgabe)",
                    KwMinus = diff(z, pe, 1, 1.0 - SENS_DELTA_ENERGIE / 100.0), KwBasis = kwBasis,
                    KwPlus = diff(z, pe, 1, 1.0 + SENS_DELTA_ENERGIE / 100.0) }
            };
            // Novellen-Szenario (Kap. 8.5.7, Phase 9): KWKG-Bonus entfällt komplett
            // (−Δ) vs. Fortschreibung der heutigen Sätze (Basis = +Δ).
            // ETAPPE E4: Gestrichen wird ausschließlich die KWKG-Reihe — die
            // Steuergutschriften hängen an anderen Gesetzen und bleiben stehen.
            if (HatKwkg(variante) || HatKwkg(stamm))
            {
                KapitalwertRechner.Zahlungsbild bv = RechneBild(OhneKwkg(variante), p, z, pe, 1.0, 1.0);
                KapitalwertRechner.Zahlungsbild bs = RechneBild(OhneKwkg(stamm), p, z, pe, 1.0, 1.0);
                zeilenListe.Add(new SensitivitaetZeile
                {
                    IdProjekt = idProjekt,
                    Parameter = "KWKG-Bonus entfällt (Regulierungsrisiko Novelle)",
                    KwMinus = Math.Round(bv.Kapitalwert - bs.Kapitalwert, 2),
                    KwBasis = kwBasis,
                    KwPlus = kwBasis
                });
            }
            return zeilenListe;
        }

        /// <summary>true, wenn die Eingabe eine KWKG-Reihe führt (Etappe E4).
        /// ETAPPE K6: Die Pauschale des § 9 zählt mit — sie ist derselbe Fördertopf und
        /// fiele mit einer Novelle genauso weg.</summary>
        private static bool HatKwkg(ProjektEingabe e)
        {
            foreach (KapitalwertRechner.ErloesReihe r in e.ErloesReihen)
                if (IstKwkgReihe(r.Name)) return true;
            return false;
        }

        /// <summary>Die beiden KWKG-Reihennamen an EINER Stelle (Etappe K6).</summary>
        private static bool IstKwkgReihe(string name)
        {
            return string.Equals(name, KapitalwertRechner.ErloesReihe.KWKG, StringComparison.Ordinal) ||
                   string.Equals(name, KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE, StringComparison.Ordinal);
        }

        /// <summary>
        /// Flache Kopie einer Eingabe ohne KWKG-Erlösreihe (Novellen-Szenario).
        /// <para><b>ETAPPE E4:</b> Es fällt genau die KWKG-Reihe weg; die
        /// Steuergutschriften bleiben, denn das Szenario fragt nach dem Wegfall der
        /// KWKG-Förderung, nicht nach dem Wegfall des Energie- und Stromsteuerrechts.</para>
        /// </summary>
        private static ProjektEingabe OhneKwkg(ProjektEingabe e)
        {
            var kopie = new ProjektEingabe
            {
                Investitionen = e.Investitionen,
                // K5: Der Zuschuss MUSS mitkopiert werden. Ohne ihn rechnete das
                // Novellen-Szenario gegen ein anderes I₀ als die Basis, und die
                // ausgewiesene Differenz enthielte den Zuschuss statt nur den
                // weggefallenen KWKG-Bonus.
                Zuschuss = e.Zuschuss,
                Betrieb = e.Betrieb,
                // PAKET FX4-a (Anwenderentscheid 02.09.2026, offener Punkt FX3-2):
                // Die Betriebskosten mit STARTJAHR ≥ 2 (KD6) fehlten hier seit KD6 —
                // eine Altlücke, keine Absicht. Ohne sie rechnete das Novellen-Szenario
                // gegen andere Betriebskosten als die Basis, und die ausgewiesene
                // Differenz enthielte sie; dieselbe Begründung wie beim Zuschuss, bei
                // der CO₂-Reihe und beim Endenergie-Topf. Mit diesem Feld ist die Kopie
                // für RechneBild VOLLSTÄNDIG (Investitionen, Zuschuss, beide
                // Betriebstöpfe samt Startjahr-Anteilen, Energie, Erlös, CO₂).
                // Bestandswirkung null: 0 Zeilen mit StartJahr > 1 im gesamten Bestand.
                BetriebAbJahr = e.BetriebAbJahr,
                // PAKET FX5-a: der investgekoppelte AUSWEIS gehört zur vollständigen
                // Kopie. Rechnerisch ist er hier folgenlos — der Ohne-KWKG-Vergleich
                // läuft immer mit Investitionsfaktor 1,0 —, aber die Kopie muss jedes
                // Feld führen, das RechneBild liest; sonst wäre die oben behauptete
                // Vollständigkeit wieder eine Halbwahrheit.
                InvestGekoppelt = e.InvestGekoppelt,
                InvestGekoppeltAbJahr = e.InvestGekoppeltAbJahr,
                // PAKET FX3 (R-2): Der Endenergie-Topf MUSS mitkopiert werden —
                // dieselbe Begründung wie beim Zuschuss und bei der CO₂-Reihe: Sonst
                // rechnete das Novellen-Szenario gegen andere Betriebskosten als die
                // Basis, und die ausgewiesene Differenz enthielte sie.
                Endenergie = e.Endenergie,
                EndenergieAbJahr = e.EndenergieAbJahr,
                Energie = e.Energie,
                Erloes = e.Erloes,
                Behg = e.Behg,
                // K6: Die CO₂-Reihe MUSS mitkopiert werden — dieselbe Begründung wie beim
                // Zuschuss: Sonst rechnete das Novellen-Szenario gegen eine andere
                // CO₂-Abgabe als die Basis, und die ausgewiesene Differenz enthielte sie.
                BehgJeJahr = e.BehgJeJahr,
                KwkgJahr1 = 0,
                WaermeMWh = e.WaermeMWh,
                Matrix = e.Matrix
            };
            foreach (KapitalwertRechner.ErloesReihe r in e.ErloesReihen)
                if (!IstKwkgReihe(r.Name))          // K6: auch die Pauschale des § 9 fällt weg
                    kopie.ErloesReihen.Add(r);
            return kopie;
        }

        /// <summary>
        /// ETAPPE P6 (§ 6.4): vermiedener Netzbezug durch PV-Eigenverbrauch [€/a],
        /// INFORMATIV — Jahr-1-Sicht: (Erzeugung − Überschuss) × Strom-Arbeitspreis.
        /// Preis über dieselbe Vorrangkette wie die Energiekostenrechnung
        /// (<c>custom_price</c> des Projekts vor <c>price</c> des Katalogs,
        /// 0 = nicht gepflegt, Befund D5; Stromträger wie
        /// <c>KostenEmissionRechner.FindeStromTraeger</c>). KEIN Bestandteil des
        /// Kapitalwerts; im ROLLEN-Modus tragen die E5-Zeilen die Systemsicht.
        /// </summary>
        private static double? PvVermiedenerBezugAusweis(VariantenDaten v)
        {
            try
            {
                if (v.Ergebnis == null || v.Ergebnis.Photovoltaik == null) return null;
                double evMWh = v.Ergebnis.Photovoltaik.Stromproduktion
                             - v.Ergebnis.Photovoltaik.Ueberschuss;
                if (evMWh <= 0.0005) return null;
                double? preis = StromArbeitspreisEurJeKwh(v.IdProjekt);
                if (!preis.HasValue) return null;
                return evMWh * 1000.0 * preis.Value;
            }
            catch { return null; }
        }

        /// <summary>Arbeitspreis Strom [€/kWh] des Projekt-Stromträgers; null = keiner gepflegt.
        /// <para><c>custom_price</c> ist eine Lazy-Spalte und fehlt auf nie berührten
        /// Datenbanken (Produktiv-Befund 26.08.2026) - sie wird deshalb vor dem
        /// Zugriff still geprobt statt blind angefragt.</para></summary>
        internal static double? StromArbeitspreisEurJeKwh(int idProjekt)
        {
            try
            {
                // Bestandsspalten der Kostenwelt: custom_price_work (Projekt) vor
                // price_work (Katalog) - NICHT "custom_price"/"price" (Befund
                // 26.08.2026: diese Namen existieren nur auf der Testkopie, der
                // Produktivbestand kennt sie nicht -> ACE-Parameterfehler).
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT s.custom_price_work AS Projektpreis, ec.price_work AS price " +
                    "FROM energy_project_settings AS s " +
                    "INNER JOIN energy_carrier AS ec ON s.[ID_Energieträger] = ec.id " +
                    "WHERE s.ID_Projekt = ? AND ec.pricing_model = 'ELECTRICITY' LIMIT 1",
                    new DbParam("@p", idProjekt));
                if (dt == null || dt.Rows.Count == 0) return null;
                DataRow r = dt.Rows[0];
                double? projektwert = D2(r, "Projektpreis");
                if (projektwert.HasValue && projektwert.Value > 0) return projektwert;
                double? katalogwert = D2(r, "price");
                return katalogwert.HasValue && katalogwert.Value > 0 ? katalogwert : null;
            }
            catch { return null; }
        }


        private static double? D2(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); } catch { return null; }
        }

        /// <summary>Absolutes Zahlungsbild + Kennzahlen eines Projekts für ein Szenario.</summary>
        private WirtschaftlichkeitErgebnis RechneProjekt(VariantenDaten v, WirtschaftlichkeitParameter p,
                                                         ProjektEingabe eingabe, string szenario,
                                                         out KapitalwertRechner.Zahlungsbild bild)
        {
            bild = null;
            var erg = new WirtschaftlichkeitErgebnis
            {
                IdProjekt = v.IdProjekt,
                IdErgebnis = LiesErgebnisId(v.IdProjekt),
                Szenario = szenario,
                IstStamm = v.IstStamm,
                Anzeige = v.Anzeige
            };

            if (v.Fehler != null || v.Ergebnis == null)
            { erg.Fehlgrund = v.Fehler ?? "Kein Simulationsergebnis vorhanden."; return erg; }

            // ---------------- Zahlungsgerüst (BaueEingabe) ----------------
            // PAKET FX3 (R-2): AUSGEWIESEN werden weiterhin die Betriebskosten p. a. als
            // GANZES — beide Preissteigerungstöpfe zusammen. Die Trennung ist eine Frage
            // der Fortschreibung über die Jahre, keine Frage der Jahr-1-Zahl; sie darf
            // die Betriebskostenzeile der Berichte nicht schrumpfen lassen.
            erg.BetriebskostenJahr = eingabe.Betrieb + eingabe.Endenergie;
            erg.EnergiekostenJahr = eingabe.Energie;
            erg.EinspeiseerloesJahr = eingabe.Erloes;
            erg.CO2AbgabeJahr = eingabe.Behg;                 // W2: BEHG
            erg.KwkgErloesJahr1 = eingabe.KwkgJahr1;          // W2/W3: KWKG
            erg.KwkgVbhElektrisch = eingabe.VbhElektrisch;    // E2: Bezugsgröße der Deckelung
            erg.EnergiesteuerJahr1 = eingabe.EnergiesteuerJahr1;              // E4
            erg.StromsteuerBefreiungJahr1 = eingabe.StromsteuerBefreiungJahr1;
            erg.StromsteuerEntlastungJahr1 = eingabe.StromsteuerEntlastungJahr1;
            erg.SteuerHerkunft = eingabe.SteuerHerkunft;
            erg.StromkostenTarif = eingabe.StromkostenTarif;  // W3: Tarifmatrix
            erg.VermiedenArbeitJahr = eingabe.VermiedenArbeit;        // E5
            erg.VermiedenLeistungJahr = eingabe.VermiedenLeistung;
            erg.VermiedenGesamtJahr = eingabe.VermiedenGesamt;
            erg.AufschlagJahr = eingabe.AufschlagBetrag;
            erg.EinspeiseerloesPvJahr = eingabe.ErloesPv;             // E7
            erg.EinspeiseerloesKwkJahr = eingabe.ErloesKwk;

            // ETAPPE P6 (PV-Konzept § 6.4): Ausweis des Vergütungsdialogs — die
            // Reihe selbst steckt längst in eingabe.ErloesReihen (P4); hier wird
            // ihre Herkunft für Reiter, Bericht und Persistenz festgehalten.
            if (eingabe.PvVerguetung != null)
            {
                PvErloesErgebnis pv = eingabe.PvVerguetung;
                erg.PvVerguetungsform = pv.Vermarktungsform ?? "";
                erg.PvAnzulegenderWert = pv.AwMixCt;
                erg.PvMarktpraemie = pv.MarktpraemieEurJahr1;
                erg.PvVerguetungsausfallKwh = pv.VerguetungsausfallKwh;
                erg.PvVerguetungsausfall = pv.VerguetungsausfallEur;
                erg.PvKompensation51a = pv.Kompensation51aEur;
                erg.PvKappungsverlustKwh = pv.KappungsverlustKwh;
                erg.PvVermiedenerBezug = PvVermiedenerBezugAusweis(v);
            }
            erg.KwkgModule = eingabe.KwkgModule;
            erg.Betriebskosten = eingabe.Betriebskosten;
            erg.Hinweis = eingabe.Hinweis;

            // Trägerzuordnungs-Etappe: Fiel die Emissionsrechnung mangels zugeordnetem
            // Strom-Energieträger auf den Strommix-Vorgabewert zurück (Flag aus
            // KostenEmissionRechner), gehört das in denselben Hinweiskanal wie jede
            // andere dokumentierte Vereinfachung — sonst steht im Ergebnis eine
            // CO₂-Bilanz, deren Bezugsgröße niemand erfasst hat.
            if (v.CO2StrommixRueckfall)
                erg.Hinweis = Anhaengen(erg.Hinweis, T("WIRT_CO2_STROMMIX_RUECKFALL",
                    "CO₂-Bilanz: kein Strom-Energieträger zugeordnet — Netzbezug mit " +
                    "Strommix-Vorgabewert gerechnet."));

            // BEFUNDE B-1/N1 (Anwenderentscheid 30.08.2026): Hat ein Heizkessel Wärme
            // erzeugt, ohne dass sein Brennstoffverbrauch im Ergebnis steht, fehlt sein
            // Brennstoff still in Energiekosten, CO₂-Bilanz und BEHG-Menge (Fahne aus
            // KostenEmissionRechner). GEMELDET, nicht abgeleitet: Die Zahlen dieses
            // Ergebnisses bleiben unverändert — hier kommt allein die Hinweiszeile
            // dazu, nach demselben Muster wie der Strommix-Rückfall darüber.
            if (v.KesselVerbrauchFehlt)
                erg.Hinweis = Anhaengen(erg.Hinweis, string.Format(
                    T("WIRT_KESSELBRENNSTOFF_FEHLT",
                      "Energiekosten/CO₂-Bilanz unvollständig: Der Brennstoffverbrauch des " +
                      "Heizkessels {0} liegt im Simulationsergebnis nicht vor — Kesselbrennstoff " +
                      "fehlt in Energiekosten, CO₂-Bilanz und BEHG-Abgabe."),
                    (v.KesselOhneVerbrauch == null || v.KesselOhneVerbrauch.Count == 0)
                        ? "?" : string.Join(", ", v.KesselOhneVerbrauch)));

            // ETAPPE B2 (BW2/BF2) — Kohärenzprüfung als REINE Warnzeile. Sie liest die
            // Preiszerlegung und vergleicht sie mit den bereits gebuchten Gutschriften;
            // sie rechnet nichts nach und ändert nichts. Ein Fehlschlag darf den Lauf
            // niemals kippen — deshalb der Fangzaun: lieber keine Hinweiszeile als kein
            // Kapitalwert.
            try
            {
                erg.KohaerenzHinweise = KohaerenzPruefung.Pruefe(v.IdProjekt, new KohaerenzLauf
                {
                    Jahr = Foerderbeginn(p),
                    Steuer = eingabe.SteuerEingabe,
                    AufschlaegeAnwenden = p.AufschlaegeAnwenden,
                    EnergiesteuerEur = eingabe.EnergiesteuerJahr1,
                    StromsteuerBefreiungEur = eingabe.StromsteuerBefreiungJahr1,
                    StromsteuerEntlastungEur = eingabe.StromsteuerEntlastungJahr1
                });
            }
            catch { }
            foreach (KapitalwertRechner.InvestPosition pos in eingabe.Investitionen)
                erg.Investition += pos.Betrag;

            if (!eingabe.Energie.HasValue)
            {
                // Ohne Energiekosten fehlt der größte Posten — Kennzahlen bleiben „—".
                erg.Fehlgrund = "Energiekosten nicht bestimmbar — Arbeitspreise/Träger in der " +
                                "Kostenmaske (Energiekosten) prüfen.";
                return erg;
            }

            // ---------------- Kapitalwert ----------------
            bild = RechneBild(eingabe, p, p.Zinssatz, p.PreissteigerungEnergie, 1.0, 1.0);

            // ETAPPE K5: Der ANGESETZTE Zuschuss - nicht der erfasste. Beide fallen
            // auseinander, wenn jemand mehr Zuschuss als Investition erfasst hat; dann
            // steht I₀ auf 0, und der Überhang wird als Hinweis gemeldet statt
            // stillschweigend als Gewinn verrechnet.
            erg.Zuschuss = bild.Zuschuss;
            if (bild.ZuschussUeberhang > 0.005)
            {
                string ueberhang = string.Format(MyResource.Resource.WIRT_ZUSCHUSS_UEBERHANG,
                    (bild.Zuschuss + bild.ZuschussUeberhang).ToString("N2", BerichtTexte.Kultur),
                    bild.InvestitionBrutto.ToString("N2", BerichtTexte.Kultur));
                erg.Hinweis = string.IsNullOrEmpty(erg.Hinweis)
                    ? ueberhang : erg.Hinweis + " | " + ueberhang;
            }

            erg.BarwertAusgaben = bild.BarwertAusgaben;
            erg.BarwertEinnahmen = bild.BarwertEinnahmen;
            erg.RestwertBarwert = bild.RestwertBarwert;
            erg.Kapitalwert = bild.Kapitalwert;

            // Wärmegestehungskosten: annuisierte Nettokosten ÷ Jahreswärmebedarf.
            if (eingabe.WaermeMWh > 0)
            {
                double a = KapitalwertRechner.Annuitaet(p.Zinssatz / 100.0, p.Betrachtungszeitraum);
                erg.Gestehungskosten = (-bild.Kapitalwert * a) / (eingabe.WaermeMWh * 1000.0);
            }
            return erg;
        }

        /// <summary>Kategorie-1-Positionen (Investitionen) mit Szenariowerten.
        /// Best/WorstCase bzw. …_Nutzungsdauer: 0/leer → Erwartungswert (VALERI-Muster).
        /// <para>Sichtbarkeit <c>internal</c> statt <c>private</c>, damit die
        /// Kompaktanzeige der Seite „Kosten" (<see cref="UcBkKosten"/>) dieselbe
        /// Leselogik verwendet und keine zweite entsteht.</para></summary>
        internal static List<KapitalwertRechner.InvestPosition> LiesInvestitionen(int idProjekt, string szenario)
        {
            double zuschussEgal;
            return LiesInvestitionen(idProjekt, szenario, out zuschussEgal);
        }

        /// <summary>
        /// ETAPPE K5 (Konzept § 7.4, L7): dieselbe Leselogik, aber mit dem
        /// <b>Zuschuss getrennt</b>. Positionen mit
        /// <c>Kostenart = <see cref="DbWerte.KOSTENART_ZUSCHUSS"/></c> gehen NICHT in die
        /// Positionsliste, sondern in <paramref name="zuschuss"/> — als positive Summe.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum getrennt und nicht als negative Position.</b> Eine Position bekommt im
        /// <see cref="KapitalwertRechner"/> über ihre Nutzungsdauer eine Ersatzbeschaffung
        /// und einen Restwert. Für eine Förderzahlung ist beides sinnlos; die
        /// Altanwendung tat es trotzdem und nahm dafür die Laufvariable des letzten
        /// BHKW-Moduls als Nutzungsdauer (Konzept Anhang A(e), Fehler 1). Der Zuschuss
        /// wird deshalb I₀-seitig abgezogen.
        /// </para>
        /// <para>
        /// <b>Der Zuschuss folgt den Szenarien wie jede andere Zeile.</b> Best- und
        /// Worst-Case-Beträge gelten auch für ihn (0/leer → Erwartungswert, VALERI-Muster)
        /// — eine Förderzusage kann ausfallen oder höher ausfallen, und das ist genau die
        /// Art Unsicherheit, für die die Szenarien da sind.
        /// </para>
        /// <para>
        /// <b>Ohne die Spalten aus Schritt 19</b> (nie migrierte Datenbank) gibt es keine
        /// Kostenart und damit keinen Zuschuss: Der Rückfallweg liest dieselbe Abfrage wie
        /// vor K5, und jede Zeile bleibt eine Investitionsposition. Das ist das Verhalten
        /// des Bestands und damit richtig — eine Zuschusszeile kann in einer solchen
        /// Datenbank gar nicht entstanden sein.
        /// </para>
        /// </remarks>
        internal static List<KapitalwertRechner.InvestPosition> LiesInvestitionen(
            int idProjekt, string szenario, out double zuschuss)
        {
            var liste = new List<KapitalwertRechner.InvestPosition>();
            zuschuss = 0;

            bool mitKostenart = false;
            try { mitKostenart = KostenPositionCtrl.StelleSpaltenSicher(); }
            catch { }

            try
            {
                string felder = "w.EingegebenerWert, w.BestCase, w.WorstCase, w.Nutzungsdauer, " +
                                "w.BestCase_Nutzungsdauer, w.WorstCase_Nutzungsdauer, " +
                                "w.KomponentenID, f.IsMainComponent";
                if (mitKostenart)
                    felder += ", w.[" + SchemaKatalog.SPALTE_PW_KOSTENART + "]" +
                              ", w.[" + SchemaKatalog.SPALTE_PW_BEMESSUNG + "]" +
                              ", w.[" + SchemaKatalog.SPALTE_PW_MENGE + "]" +
                              ", w.[" + SchemaKatalog.SPALTE_PW_EINHEITPREIS + "]";
                // ETAPPE KD6 (§ 11, FK10): das Startjahr je Position — die Spalte kommt
                // mit Migrationsschritt 38. Sie wird nur ANGEFRAGT, wenn sie existiert:
                // Ein SELECT auf eine fehlende Spalte würde die GANZE Abfrage kippen
                // und die Investitionsliste still leeren (der catch unten schluckt).
                if (StartjahrSpalteVorhanden())
                    felder += ", w.[" + SchemaKatalog.SPALTE_PW_STARTJAHR + "]";
                if (AnlagenSpalteVorhanden())
                    felder += ", w.[" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "]";

                DataTable dt = DataRepository.GetDataTable(
                    "SELECT " + felder +
                    " FROM Tab_ProjektWerte AS w LEFT JOIN Tab_Kostenfaktor AS f " +
                    "ON w.StammID = f.StammID " +
                    "WHERE w.ProjektID = ? AND w.KategorieID = 1",
                    new DbParam("@p", idProjekt));
                if (dt == null) return liste;

                // ETAPPE H4b: erst puffern, dann in DREI Runden ableiten — die
                // §-5.3-Kaskade des Kostendialoge-Konzepts: Hauptposition →
                // „% der Erzeugerkosten" → Summe → „% der Investition". Zeilen mit
                // Bemessung BETRAG (der GESAMTE Bestand nach Schritt 19b) laufen in
                // Runde 1 exakt den alten Weg; abgeleitete Arten rechnen
                // Menge × Satz, die Menge notfalls frisch aus der Gerätewelt
                // (TechnikPlanwertCtrl.BaugroesseSumme) — eine gepflegte Menge und
                // gepflegte Szenariowerte behalten Vorrang (VALERI-Muster wie E3).
                var puffer = new List<InvestZeile>();
                foreach (DataRow r in dt.Rows)
                {
                    var z = new InvestZeile();
                    z.Wert = Szenariowert(r, szenario, "EingegebenerWert", "BestCase", "WorstCase");
                    z.Erwartet = D(r, "EingegebenerWert") ?? 0;
                    z.Dauer = Szenariowert(r, szenario, "Nutzungsdauer",
                                           "BestCase_Nutzungsdauer", "WorstCase_Nutzungsdauer");
                    z.Start = StartJahrDerZeile(r);
                    z.Zuschuss = mitKostenart && IstZuschuss(r);
                    z.Haupt = B(r, "IsMainComponent");
                    KomponenteUndAnlage(r, out z.Komponente, out z.Anlage);
                    if (mitKostenart)
                    {
                        z.Bem = Text(r, SchemaKatalog.SPALTE_PW_BEMESSUNG);
                        z.Satz = D(r, SchemaKatalog.SPALTE_PW_EINHEITPREIS);
                        z.Menge = D(r, SchemaKatalog.SPALTE_PW_MENGE);
                    }
                    puffer.Add(z);
                }

                // Runde 1 — alles außer den beiden %-Kaskadenarten.
                foreach (InvestZeile z in puffer)
                {
                    if (IstProzentErzeuger(z.Bem) || IstProzentInvest(z.Bem)) continue;
                    z.Betrag = InvestBetrag(z, idProjekt, null);
                    z.Abgeleitet = true;
                }

                // Runde 2 — „% der Erzeugerkosten": Basis ist der abgeleitete Betrag
                // der Hauptposition(en) derselben Komponente.
                foreach (InvestZeile z in puffer)
                {
                    if (!IstProzentErzeuger(z.Bem)) continue;
                    double basis = 0; bool da = false;
                    foreach (InvestZeile h in puffer)
                        if (h.Abgeleitet && h.Haupt && !h.Zuschuss && h.Komponente == z.Komponente)
                        { basis += h.Betrag; da = true; }
                    z.Betrag = InvestBetrag(z, idProjekt, da && basis != 0 ? basis : (double?)null);
                    z.Abgeleitet = true;
                }

                // Runde 3 — „% der Investition": Basis ist die Summe der in den Runden 1
                // und 2 abgeleiteten Beträge (ohne Zuschüsse), stufig Anlage → Komponente
                // → Projekt (dieselbe Semantik wie InvestSummeFuer der H4a).
                //
                // ANWENDERENTSCHEID I-3 (30.08.2026, Paket FX2) — ZWEI PHASEN.
                // Die Basiszeilen werden VOR der Zuweisungsschleife eingefroren. Bis
                // hierher setzte die Schleife jede fertige Zeile sofort auf
                // Abgeleitet = true; eine ZWEITE „% der Investition"-Zeile rechnete
                // deshalb die ERSTE in ihre Basis ein, und weil die Leseabfrage kein
                // ORDER BY trägt, entschied die Datenbank über das Ergebnis (Befund I-3).
                //
                // Der Entscheid: Jede Investition ist eine eigene Position mit eigener
                // Nutzungsdauer — das bleibt. %-Zeilen bemessen sich aber ausschließlich
                // an den DIREKTEN Zeilen der Runden 1 und 2 und zählen einander nie mit.
                // Damit ist das Ergebnis deterministisch und reihenfolgeunabhängig.
                var basisZeilen = new List<InvestZeile>();
                foreach (InvestZeile h in puffer)
                    if (h.Abgeleitet && !h.Zuschuss) basisZeilen.Add(h);

                foreach (InvestZeile z in puffer)
                {
                    if (z.Abgeleitet) continue;
                    double sAnlage = 0, sKomponente = 0, sProjekt = 0;
                    bool aDa = false, kDa = false;
                    foreach (InvestZeile h in basisZeilen)
                    {
                        sProjekt += h.Betrag;
                        if (z.Komponente > 0 && h.Komponente == z.Komponente)
                        {
                            sKomponente += h.Betrag; kDa = true;
                            if (z.Anlage > 0 && h.Anlage == z.Anlage) { sAnlage += h.Betrag; aDa = true; }
                        }
                    }
                    double basis = (aDa && sAnlage != 0) ? sAnlage
                                 : (kDa && sKomponente != 0) ? sKomponente : sProjekt;
                    z.Betrag = InvestBetrag(z, idProjekt, basis != 0 ? basis : (double?)null);
                    z.Abgeleitet = true;
                }

                foreach (InvestZeile z in puffer)
                {
                    if (z.Betrag == 0) continue;

                    if (z.Zuschuss)
                    {
                        // Der Betrag wird positiv erfasst; ein versehentlich negativer
                        // Wert würde die Investition ERHÖHEN. Das ist nie gemeint —
                        // deshalb der Betrag, nicht das Vorzeichen.
                        zuschuss += Math.Abs(z.Betrag);
                        continue;
                    }

                    liste.Add(new KapitalwertRechner.InvestPosition
                    {
                        Betrag = z.Betrag,
                        Nutzungsdauer = z.Dauer,
                        StartJahr = z.Start
                    });
                }
            }
            catch { }
            return liste;
        }

        /// <summary>ETAPPE H4b: eine gepufferte Kategorie-1-Zeile der Kaskade.</summary>
        private sealed class InvestZeile
        {
            public double Wert;        // Szenariowert aus EingegebenerWert/Best/Worst
            public double Erwartet;    // EingegebenerWert (VALERI-Vergleichsbasis)
            public double Dauer;
            public int Start;
            public bool Zuschuss;
            public bool Haupt;         // Tab_Kostenfaktor.IsMainComponent
            public int Komponente;
            public int Anlage;
            public string Bem = "";
            public double? Satz;
            public double? Menge;
            public double Betrag;      // wirksamer Betrag nach der Ableitung
            public bool Abgeleitet;
        }

        private static bool IstProzentErzeuger(string bem)
        {
            return string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, StringComparison.Ordinal);
        }

        /// <summary>„x % der Investitionssumme" (H4b auf der Investseite, H4a auf der
        /// Betriebsseite).
        /// <para><b>PAKET FX5-a:</b> Dasselbe Prädikat entscheidet seit dem
        /// 03.09.2026 auch, welche KATEGORIE-2-Zeile in den investgekoppelten Ausweis
        /// von <see cref="LiesBetriebskostenTopfe"/> geht — die Zeile, die der
        /// Sensitivitäts-Investitionsfaktor mitzieht.</para></summary>
        private static bool IstProzentInvest(string bem)
        {
            return string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal);
        }

        /// <summary>
        /// ETAPPE H4b: wirksamer Betrag einer Investitionszeile. BETRAG/leer = der
        /// Bestandsweg (Szenariowert unverändert); abgeleitete Arten rechnen
        /// Menge × Satz über <see cref="BetriebskostenCtrl.Betrag"/>. Gepflegte
        /// Best-/Worst-Beträge schlagen die Ableitung (VALERI).
        /// ETAPPE H2-1: Mengenreihenfolge FRISCH vor Konserve — erst
        /// <paramref name="kaskadenBasis"/> (Runden 2/3), dann die Gerätewelt,
        /// zuletzt die Menge-Spalte. Die ist nur Ausweisgröße („Stand des Laufs",
        /// Konzept BHKW-Wirtschaftlichkeit § 4.5), sonst rechnete die Kaskade nach
        /// einer Geräteänderung stillschweigend mit der alten Baugröße weiter.
        /// </summary>
        private static double InvestBetrag(InvestZeile z, int idProjekt, double? kaskadenBasis)
        {
            if (string.IsNullOrEmpty(z.Bem) ||
                string.Equals(z.Bem, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal))
                return z.Wert;

            bool szenarioGepflegt = Math.Abs(z.Wert - z.Erwartet) > 1e-9;
            if (szenarioGepflegt) return z.Wert;

            double? menge = kaskadenBasis;
            if (!menge.HasValue)
                menge = TechnikPlanwertCtrl.BaugroesseSumme(idProjekt, z.Komponente, z.Bem, z.Anlage);
            if (!menge.HasValue) menge = z.Menge;

            return BetriebskostenCtrl.Betrag(z.Bem, z.Erwartet, menge, z.Satz, false);
        }

        /// <summary>Einmal je Prozess geprüft: existiert <c>Tab_ProjektWerte.StartJahr</c>
        /// (Migrationsschritt 38)? Auf älteren Datenbanken bleibt alles t0.</summary>
        private static bool? _startjahrSpalte;

        private static bool StartjahrSpalteVorhanden()
        {
            if (_startjahrSpalte.HasValue) return _startjahrSpalte.Value;
            _startjahrSpalte = SpalteVorhanden("Tab_ProjektWerte",
                                               SchemaKatalog.SPALTE_PW_STARTJAHR);
            return _startjahrSpalte.Value;
        }

        /// <summary>ETAPPE H2: Cache der Spaltenprobe
        /// <c>Tab_ProjektWerte.ID_Anlage</c> (Schritt 45) — gleiches Muster wie
        /// <see cref="StartjahrSpalteVorhanden"/>.</summary>
        private static bool? _anlagenSpalte;

        private static bool AnlagenSpalteVorhanden()
        {
            if (_anlagenSpalte.HasValue) return _anlagenSpalte.Value;
            _anlagenSpalte = SpalteVorhanden("Tab_ProjektWerte",
                                             SchemaKatalog.SPALTE_PW_ID_ANLAGE);
            return _anlagenSpalte.Value;
        }

        /// <summary>
        /// Stille Spaltenprobe: <c>DataRepository</c> meldet Abfragefehler selbst
        /// (MessageBox) und WIRFT NICHT - eine Probe per try/catch griffe also nie
        /// und zeigte dem Anwender einen Scheinfehler. Die Probe laeuft deshalb im
        /// <c>EngineModus</c> (Meldungen wandern still in die Sammelliste) und
        /// wertet die Liste aus. Befund 26.08.2026 (Produktiv-DB ohne Lazy-Spalte).
        /// </summary>
        internal static bool SpalteVorhanden(string tabelle, string spalte)
        {
            using (DataRepository.EngineModus())
            {
                DataRepository.StilleFehlerAbholen();                  // Liste leeren
                DataRepository.ExecuteScalar(
                    "SELECT MAX([" + spalte + "]) FROM [" + tabelle + "]");
                return DataRepository.StilleFehlerAbholen().Length == 0;
            }
        }

        /// <summary>KD6 (§ 11): <c>Tab_ProjektWerte.StartJahr</c> der Zeile —
        /// 0 = t0 (NULL, fehlende Spalte oder Werte &lt; 2).</summary>
        private static int StartJahrDerZeile(DataRow r)
        {
            try
            {
                if (!r.Table.Columns.Contains(SchemaKatalog.SPALTE_PW_STARTJAHR)) return 0;
                object o = r[SchemaKatalog.SPALTE_PW_STARTJAHR];
                if (o == null || o == DBNull.Value) return 0;
                int j = Convert.ToInt32(o);
                return j > 1 ? j : 0;
            }
            catch { return 0; }
        }

        /// <summary>true, wenn die Zeile die Kostenart „Zuschuss" trägt (K5).</summary>
        private static bool IstZuschuss(DataRow r)
        {
            try
            {
                if (!r.Table.Columns.Contains(SchemaKatalog.SPALTE_PW_KOSTENART)) return false;
                object o = r[SchemaKatalog.SPALTE_PW_KOSTENART];
                if (o == null || o == DBNull.Value) return false;
                return string.Equals(Convert.ToString(o).Trim(), DbWerte.KOSTENART_ZUSCHUSS,
                                     StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        /// <summary>
        /// Summe der Zuschusspositionen eines Projekts [€], positiv (K5). 0 = keine.
        /// Für Anzeigen, die die Investitionsliste nicht ohnehin lesen.
        /// </summary>
        internal static double LiesZuschuss(int idProjekt, string szenario)
        {
            double zuschuss;
            LiesInvestitionen(idProjekt, szenario, out zuschuss);
            return zuschuss;
        }

        /// <summary>Summe der Kategorie-2-Positionen (Betriebskosten p. a., Szenariowert).
        /// <c>internal</c> aus demselben Grund wie <see cref="LiesInvestitionen"/>.</summary>
        /// <remarks>
        /// <para>
        /// <b>Etappe E3: die Bemessungsart wird ausgewertet.</b> Eine Position mit
        /// <c>Bemessung = BETRAG</c> — und das sind nach Migrationsschritt 19b ALLE
        /// Bestandszeilen — verhält sich Zeile für Zeile wie vorher: Der Szenariowert aus
        /// <c>EingegebenerWert</c>/<c>BestCase</c>/<c>WorstCase</c> gilt unverändert. Nur
        /// die vier abgeleiteten Bemessungsarten rechnen aus der persistierten Herleitung
        /// <c>Menge × Einheitpreis</c> (<see cref="BetriebskostenCtrl.Betrag"/>).
        /// </para>
        /// <para>
        /// <b>Szenarien bleiben Vorrang vor der Ableitung.</b> Ein gepflegter Best- oder
        /// Worst-Case-Betrag schlägt die Ableitung — dasselbe VALERI-Muster wie bisher
        /// („0/leer = kein Szenariowert gepflegt"). Ohne gepflegten Szenariowert gilt der
        /// abgeleitete Erwartungswert in allen drei Szenarien.
        /// </para>
        /// <para>
        /// <b>Zwei Abfragen, eine tolerante Rückfallebene.</b> Fehlen die Spalten aus
        /// Schritt 19 (Datenbank nie migriert und die Vorsorge nicht durchgekommen), läuft
        /// die alte Abfrage — also exakt der Rechenweg vor E3.
        /// </para>
        /// <para>
        /// <b>Vorzeichen.</b> Der gespeicherte Betrag ist die Zahlungswirkung in €/a:
        /// positiv = Ausgabe, negativ = Einnahme. Eine Erlösposition
        /// (<c>IstErloes = True</c>) trägt deshalb mit negativem Vorzeichen zur Summe bei
        /// und senkt die Betriebskosten, statt sie zu erhöhen;
        /// <see cref="BetriebskostenCtrl.Betrag"/> erzwingt das Vorzeichen zusätzlich.
        /// </para>
        /// </remarks>
        internal static double LiesBetriebskosten(int idProjekt, string szenario)
        {
            BetriebsTopfe t = LiesBetriebskostenTopfe(idProjekt, szenario);
            // Bestandssicht: die GESAMTsumme p. a. (Anzeigen/Berichte) — Startjahre
            // betreffen nur die zeitliche Verteilung in der Kapitalwertreihe, und die
            // Aufteilung auf die beiden Preissteigerungstöpfe (FX3) erst recht nicht.
            return t.Gesamt;
        }

        /// <summary>
        /// ETAPPE KD6 (§ 11, FK10): dieselbe Leselogik, aber Positionen mit
        /// <c>StartJahr ≥ 2</c> GETRENNT — sie gehen als (Betrag, Startjahr)-Paare
        /// in <paramref name="abJahr"/> und laufen in der Kapitalwertreihe erst ab
        /// ihrem Jahr; der Rückgabewert ist nur noch der Sofort-Anteil (t0).
        /// <para><b>PAKET FX3:</b> Diese Überladung fasst beide Preissteigerungstöpfe
        /// wieder zu EINEM zusammen (Sicht vor FX3). Wer die Jahresreihe rechnet, nimmt
        /// <see cref="LiesBetriebskostenTopfe"/> — sonst wüchse der Endenergie-Anteil
        /// wieder mit p_B statt mit p_E.</para>
        /// </summary>
        internal static double LiesBetriebskosten(int idProjekt, string szenario,
                                                  out List<KeyValuePair<double, int>> abJahr)
        {
            BetriebsTopfe t = LiesBetriebskostenTopfe(idProjekt, szenario);
            abJahr = new List<KeyValuePair<double, int>>(t.BetriebAbJahr);
            abJahr.AddRange(t.EndenergieAbJahr);
            return t.BetriebSofort + t.EndenergieSofort;
        }

        /// <summary>
        /// PAKET FX3 (Anwenderentscheid R-2, 02.09.2026) — die Kategorie-2-Positionen in
        /// ZWEI Töpfen: dem Betriebs-Topf (Preissteigerung p_B, alles Bisherige) und dem
        /// Endenergie-Topf (p_E).
        ///
        /// <para><b>Warum zwei Töpfe.</b> Eine Position mit
        /// <see cref="DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN"/> oder
        /// <see cref="DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF"/> IST ein Anteil der
        /// Energiekosten der Anlage (Hilfsenergie, Konzept § 4.5, Wege A und B). Sie mit
        /// der Betriebspreissteigerung fortzuschreiben widerspricht ihrer eigenen
        /// Bemessung; VDI 2067 und DIN EN 17463 ordnen bedarfsgebundene Kosten der
        /// Energiepreisentwicklung zu. Der Kapitalwertrechner bekommt deshalb beide
        /// Töpfe getrennt (Befund R-2 der Rechenwege-Formelkarte).</para>
        ///
        /// <para><b>Die Zuordnung entscheidet die BEMESSUNGSART, nicht der Betrag.</b>
        /// Auch eine Zeile, deren Ableitung von einem gepflegten Best-/Worst-Case-Wert
        /// geschlagen wurde (VALERI-Vorfahrt), bleibt eine Endenergie-Position und
        /// eskaliert mit p_E — sonst führe dasselbe Projekt in BEST und in ERWARTET mit
        /// verschiedenen Preisraten.</para>
        ///
        /// <para><b>PAKET FX4-b (Anwenderentscheid 02.09.2026, offener Punkt FX3-4):
        /// die zwei Alt-Arten gehören dazu.</b>
        /// <see cref="DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN"/> und
        /// <see cref="DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN"/> — die projektweiten
        /// Vorläufer von Weg A — liegen seither im Endenergie-Topf, samt ihrer
        /// Startjahr-Anteile. FX3 hatte sie ausgenommen („sie laufen aus und sollen
        /// sich nicht mehr ändern"); der Anwender hat entschieden, sie gleichzuziehen,
        /// denn sie sind derselben Sache nach ein Anteil der Energiekosten. Die
        /// Bezugsmenge holen sie unverändert aus der gepflegten Konserve — ihre
        /// MENGENermittlung ist von FX4 nicht berührt
        /// (<see cref="IstEndenergieArt"/> bleibt, was es war).</para>
        ///
        /// <para><b>Dokumentierte Grenze (Stand FX4).</b> Der „Weg C" der Hilfsenergie —
        /// der feste Jahresbetrag
        /// (<see cref="DbWerte.BEMESSUNG_JAHRESBETRAG"/>/<see cref="DbWerte.BEMESSUNG_BETRAG"/>) —
        /// bleibt im Betriebs-Topf (p_B). Das ist eine Fachentscheidung des Anwenders und
        /// keine Vergesslichkeit: Ein fester Betrag trägt keine Endenergie-Bemessung.</para>
        /// </summary>
        internal sealed class BetriebsTopfe
        {
            /// <summary>Betriebskosten p. a. mit Preissteigerung p_B, Zahlung ab t0 [€/a].</summary>
            public double BetriebSofort;

            /// <summary>Betriebs-Topf-Positionen mit Startjahr ≥ 2 (KD6).</summary>
            public List<KeyValuePair<double, int>> BetriebAbJahr =
                new List<KeyValuePair<double, int>>();

            /// <summary>Energiepreisgebundene Betriebskosten p. a. mit Preissteigerung
            /// p_E, Zahlung ab t0 [€/a] — die Arten aus
            /// <see cref="IstEnergiepreisArt"/>.</summary>
            public double EndenergieSofort;

            /// <summary>Endenergie-Topf-Positionen mit Startjahr ≥ 2 (KD6).</summary>
            public List<KeyValuePair<double, int>> EndenergieAbJahr =
                new List<KeyValuePair<double, int>>();

            /// <summary>
            /// PAKET FX5-a (Anwenderentscheid 03.09.2026, offener Punkt FX4-1) — der
            /// <b>investitionsgekoppelte ANTEIL</b> des Betriebs-Topfes [€/a],
            /// Sofort-Anteil: Positionen mit
            /// <see cref="DbWerte.BEMESSUNG_PROZENT_INVESTITION"/> („x % der
            /// Investitionssumme", H4a).
            ///
            /// <para><b>KEIN dritter Topf, sondern eine TEILMENGE.</b> Der Betrag steckt
            /// unverändert in <see cref="BetriebSofort"/> und eskaliert weiterhin mit p_B
            /// — er ist investitions-, nicht energiegebunden. Dieser Ausweis dient
            /// AUSSCHLIESSLICH der Sensitivität „Investition Variante ±10 %“
            /// (<see cref="RechneBild"/>); <see cref="Gesamt"/> zählt ihn deshalb NICHT
            /// noch einmal.</para>
            ///
            /// <para><b>Warum Teilmenge und nicht Herauslösung.</b> Würde der Anteil aus
            /// <see cref="BetriebSofort"/> herausgelöst und im Rechenweg wieder addiert,
            /// änderte sich die REIHENFOLGE der Gleitkomma-Summation der Leseschleife —
            /// der Regellauf (Faktor 1,0) wäre dann nicht mehr bitgenau der von vorher.
            /// So bleibt die Summation Zeile für Zeile, wie sie war.</para>
            /// </summary>
            public double InvestGekoppeltSofort;

            /// <summary>PAKET FX5-a × KD6: derselbe Ausweis für die investitions-
            /// gekoppelten Positionen mit Startjahr ≥ 2 — Teilmenge von
            /// <see cref="BetriebAbJahr"/>, dieselben (Betrag, Startjahr)-Paare.</summary>
            public List<KeyValuePair<double, int>> InvestGekoppeltAbJahr =
                new List<KeyValuePair<double, int>>();

            /// <summary>Betriebskosten p. a. GESAMT [€/a] — beide Töpfe, Sofort- und
            /// Startjahr-Anteil. Das ist die Zahl, die Anzeigen und Berichte als
            /// „Betriebskosten p. a." ausweisen; sie ist von FX3 unberührt.
            /// <para><b>PAKET FX5-a:</b> <see cref="InvestGekoppeltSofort"/> und
            /// <see cref="InvestGekoppeltAbJahr"/> gehen hier bewusst NICHT ein — sie
            /// sind eine Teilmenge der beiden Betriebsfelder und wären sonst doppelt
            /// gezählt.</para></summary>
            public double Gesamt
            {
                get
                {
                    double s = BetriebSofort + EndenergieSofort;
                    foreach (KeyValuePair<double, int> vb in BetriebAbJahr) s += vb.Key;
                    foreach (KeyValuePair<double, int> ve in EndenergieAbJahr) s += ve.Key;
                    return s;
                }
            }
        }

        /// <summary>
        /// PAKET FX3 (R-2): die Leseschleife der Kategorie-2-Positionen, aufgeteilt auf
        /// die beiden Preissteigerungstöpfe (Begründung an <see cref="BetriebsTopfe"/>).
        /// Ohne Endenergie-Position im Projekt bleibt der zweite Topf leer, und jede
        /// Zahl ist bitgenau die von vor FX3.
        /// <para><b>PAKET FX5-a</b> hängt einen dritten, rein beschreibenden Ausweis an:
        /// den investgekoppelten ANTEIL des Betriebs-Topfes
        /// (<see cref="BetriebsTopfe.InvestGekoppeltSofort"/>). Er ist kein Topf, ändert
        /// keine Summe und wird nur von der Sensitivität gelesen.</para>
        /// </summary>
        internal static BetriebsTopfe LiesBetriebskostenTopfe(int idProjekt, string szenario)
        {
            var topfe = new BetriebsTopfe();
            double summe = 0;
            double summeEnde = 0;
            // PAKET FX5-a: eigener Akkumulator für den investgekoppelten AUSWEIS. Er
            // läuft NEBEN summe her und fasst sie nicht an — deshalb bleibt die
            // Summationsreihenfolge des Betriebs-Topfes bitgenau die von vorher.
            double summeInvest = 0;
            List<KeyValuePair<double, int>> abJahr = topfe.BetriebAbJahr;
            bool mitBemessung = false;
            try { mitBemessung = KostenPositionCtrl.StelleSpaltenSicher(); }
            catch { }

            try
            {
                string felder = "EingegebenerWert, BestCase, WorstCase";
                if (mitBemessung)
                {
                    felder += ", [" + SchemaKatalog.SPALTE_PW_BEMESSUNG + "]" +
                              ", [" + SchemaKatalog.SPALTE_PW_IST_ERLOES + "]" +
                              ", [" + SchemaKatalog.SPALTE_PW_MENGE + "]" +
                              ", [" + SchemaKatalog.SPALTE_PW_EINHEITPREIS + "]";
                    // ETAPPE H2: Komponente und Anlage identifizieren die Basis der
                    // Endenergie-Bemessungen; ID_Anlage nur, wo Schritt 45 gelaufen ist.
                    felder += ", KomponentenID";
                    if (AnlagenSpalteVorhanden())
                        felder += ", [" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "]";
                }
                if (StartjahrSpalteVorhanden())
                    felder += ", [" + SchemaKatalog.SPALTE_PW_STARTJAHR + "]";

                DataTable dt = DataRepository.GetDataTable(
                    "SELECT " + felder +
                    " FROM Tab_ProjektWerte WHERE ProjektID = ? AND KategorieID = 2",
                    new DbParam("@p", idProjekt));
                if (dt == null) return topfe;

                // ETAPPE H2: der Endenergie-Auflöser wird je Aufruf höchstens einmal
                // gebaut — und nur, wenn eine Position ihn wirklich braucht.
                EndenergieAufloeser endenergie = null;
                bool endenergieVersucht = false;

                foreach (DataRow r in dt.Rows)
                {
                    double wert = Szenariowert(r, szenario, "EingegebenerWert", "BestCase", "WorstCase");
                    int start = StartJahrDerZeile(r);
                    double beitrag;
                    // PAKET FX3 (R-2): Diese Zeile gehört in den Endenergie-Topf (p_E),
                    // sobald ihre BEMESSUNGSART eine energiepreisgebundene Art ist —
                    // unabhängig davon, ob der Betrag abgeleitet wurde oder aus einem
                    // gepflegten Szenariowert stammt.
                    // PAKET FX4-b: dazu zählen jetzt auch die zwei Alt-Arten.
                    bool ausEnergiepreis = false;
                    // PAKET FX5-a: „Diese Zeile ist an der Investitionssumme bemessen" —
                    // eine ZWEITE, unabhängige Frage. Sie entscheidet NICHT über den
                    // Preissteigerungstopf (die Zeile bleibt p_B), sondern allein über
                    // den Ausweis für die Sensitivität „Investition Variante ±10 %".
                    bool ausInvestition = false;

                    if (!mitBemessung) beitrag = wert;
                    else
                    {
                        string bem = Text(r, SchemaKatalog.SPALTE_PW_BEMESSUNG);
                        bool erloes = B(r, SchemaKatalog.SPALTE_PW_IST_ERLOES);
                        ausEnergiepreis = IstEnergiepreisArt(bem);
                        ausInvestition = IstProzentInvest(bem);

                        if (string.IsNullOrEmpty(bem) ||
                            string.Equals(bem, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal))
                        {
                            // Der Bestandsweg. Das Vorzeichen einer Erlöszeile wird trotzdem
                            // erzwungen — ein Erlös darf nie als Kosten in die Summe geraten.
                            beitrag = erloes && wert > 0 ? -wert : wert;
                        }
                        else
                        {
                            // Ein gepflegter Szenariowert schlägt die Ableitung (VALERI-Muster).
                            double erwartet = D(r, "EingegebenerWert") ?? 0;
                            bool szenarioGepflegt = Math.Abs(wert - erwartet) > 1e-9;

                            // ETAPPE H2/H2-1: Ermittelbare Bemessungsarten holen ihre
                            // Bezugsgröße bei JEDEM Lesen frisch (Endenergie aus dem
                            // jüngsten Lauf, Investsumme aus der Kostenwelt, Baugrößen
                            // aus der Gerätewelt) — die Menge-Spalte ist Ausweisgröße
                            // („Stand des Laufs", Konzept § 4.5) und gilt nur noch als
                            // Konserve, wenn frisch nichts ermittelbar ist. Alle übrigen
                            // Arten lesen unverändert die gepflegte Herleitung.
                            double? menge = D(r, SchemaKatalog.SPALTE_PW_MENGE);
                            if (IstEndenergieArt(bem))
                                menge = EndenergieMenge(idProjekt, r, bem,
                                                        ref endenergie, ref endenergieVersucht);
                            else if (IstRueckfallErmittelbareArt(bem))
                            {
                                double? frisch = RueckfallMenge(idProjekt, r, bem,
                                                                ref endenergie, ref endenergieVersucht);
                                if (frisch.HasValue) menge = frisch;
                            }

                            beitrag = szenarioGepflegt
                                ? (erloes && wert > 0 ? -wert : wert)
                                : BetriebskostenCtrl.Betrag(bem, erwartet, menge,
                                                            D(r, SchemaKatalog.SPALTE_PW_EINHEITPREIS),
                                                            erloes);
                        }
                    }

                    // PAKET FX3 (R-2): Der Endenergie-Anteil wird in einem EIGENEN
                    // Akkumulator geführt. Ohne solche Zeile bleibt summeEnde eine echte
                    // 0 und der Betriebstopf sammelt Zeile für Zeile wie vor FX3 —
                    // deshalb bleiben Bestandsprojekte bitgenau.
                    if (ausEnergiepreis)
                    {
                        if (start > 1)
                            topfe.EndenergieAbJahr.Add(new KeyValuePair<double, int>(beitrag, start));
                        else summeEnde += beitrag;
                    }
                    else if (start > 1) abJahr.Add(new KeyValuePair<double, int>(beitrag, start));
                    else summe += beitrag;

                    // PAKET FX5-a (Anwenderentscheid 03.09.2026, offener Punkt FX4-1):
                    // Der investgekoppelte Anteil wird ZUSÄTZLICH ausgewiesen — dieselbe
                    // Zeile, derselbe Szenariowert, derselbe Startjahr-Schnitt. Sie
                    // bleibt oben im Betriebs-Topf stehen (sie eskaliert mit p_B, nicht
                    // mit p_E); dieser Ausweis ist eine TEILMENGE und existiert allein
                    // für die Sensitivität (Begründung an BetriebsTopfe).
                    // PROZENT_INVESTITION ist keine energiepreisgebundene Art — beide
                    // Zweige schließen einander aus; der Ausweis steht trotzdem
                    // absichtlich unabhängig daneben statt in einem der Zweige.
                    if (ausInvestition)
                    {
                        if (start > 1)
                            topfe.InvestGekoppeltAbJahr.Add(
                                new KeyValuePair<double, int>(beitrag, start));
                        else summeInvest += beitrag;
                    }
                }
            }
            catch { }
            topfe.BetriebSofort = summe;
            topfe.EndenergieSofort = summeEnde;
            topfe.InvestGekoppeltSofort = summeInvest;
            return topfe;
        }

        /// <summary>
        /// ETAPPE E7 — dieselben Kategorie-2-Positionen wie
        /// <see cref="LiesBetriebskosten"/>, aber EINZELN und mit ihrer Herleitung.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum eine zweite Leseschleife und keine Umstellung der ersten.</b> Die
        /// Summenschleife ist der Rechenweg und wurde in E3 gegen die Referenz gestellt;
        /// sie umzubauen, damit sie nebenbei eine Liste füllt, hieße den Rechenweg für
        /// eine Ausgabe anzufassen. Diese Methode rechnet mit <b>denselben Regeln</b>
        /// (<see cref="BetriebskostenCtrl.Betrag"/>, dieselbe Szenarienvorfahrt), liefert
        /// aber nur Beschreibung. Ihre Summe muss der Summe oben entsprechen — das ist
        /// eine Probe, die der Bericht ausweist.
        /// </para>
        /// <para>
        /// <b>Der Bezeichner kommt aus <c>Tab_Kostenfaktor</c>.</b>
        /// <c>Tab_ProjektWerte</c> trägt keinen Text; der Name der Position steht über
        /// <c>StammID</c> im Katalog. Gelesen wird direkt, nicht über
        /// <c>Abfrage_Kostenfaktoren</c> — die gespeicherte Access-Abfrage liegt außerhalb
        /// des Repos und kennt die fünf Spalten aus Schritt 19 nicht (E3-Protokoll,
        /// Restbefund 6).
        /// </para>
        /// </remarks>
        internal static List<KostenPositionNachweis> LiesBetriebskostenPositionen(
            int idProjekt, string szenario)
        {
            var liste = new List<KostenPositionNachweis>();
            bool mitBemessung = false;
            try { mitBemessung = KostenPositionCtrl.StelleSpaltenSicher(); }
            catch { }
            if (!mitBemessung) return liste;   // ohne Schritt 19 gibt es nichts zu gliedern

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT w.EingegebenerWert, w.BestCase, w.WorstCase, w.Gruppe, " +
                    "f.Bezeichnung, w.[" + SchemaKatalog.SPALTE_PW_KOSTENART + "], " +
                    "w.[" + SchemaKatalog.SPALTE_PW_BEMESSUNG + "], " +
                    "w.[" + SchemaKatalog.SPALTE_PW_IST_ERLOES + "], " +
                    "w.[" + SchemaKatalog.SPALTE_PW_MENGE + "], " +
                    "w.[" + SchemaKatalog.SPALTE_PW_EINHEITPREIS + "], w.KomponentenID" +
                    (AnlagenSpalteVorhanden()
                        ? ", w.[" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "] "
                        : " ") +
                    "FROM Tab_ProjektWerte AS w LEFT JOIN Tab_Kostenfaktor AS f " +
                    "ON w.StammID = f.StammID " +
                    "WHERE w.ProjektID = ? AND w.KategorieID = 2",
                    new DbParam("@p", idProjekt));
                if (dt == null) return liste;

                // ETAPPE H2: gleiche Frisch-Regel wie in der Summenschleife — die
                // Nachweisliste muss deren Summe treffen (E7-Probe).
                EndenergieAufloeser endenergie = null;
                bool endenergieVersucht = false;

                foreach (DataRow r in dt.Rows)
                {
                    double wert = Szenariowert(r, szenario, "EingegebenerWert", "BestCase", "WorstCase");
                    string bem = Text(r, SchemaKatalog.SPALTE_PW_BEMESSUNG);
                    if (string.IsNullOrEmpty(bem)) bem = DbWerte.BEMESSUNG_BETRAG;
                    bool erloes = B(r, SchemaKatalog.SPALTE_PW_IST_ERLOES);
                    double erwartet = D(r, "EingegebenerWert") ?? 0;
                    bool szenarioGepflegt = Math.Abs(wert - erwartet) > 1e-9;

                    // H2/H4a/H2-1: dieselbe Mengenregel wie in der Summenschleife
                    // (frisch vor Konserve) — die Nachweisliste muss deren Summe
                    // treffen (E7-Probe).
                    double? menge = D(r, SchemaKatalog.SPALTE_PW_MENGE);
                    if (IstEndenergieArt(bem))
                        menge = EndenergieMenge(idProjekt, r, bem,
                                                ref endenergie, ref endenergieVersucht);
                    else if (IstRueckfallErmittelbareArt(bem))
                    {
                        double? frisch = RueckfallMenge(idProjekt, r, bem,
                                                        ref endenergie, ref endenergieVersucht);
                        if (frisch.HasValue) menge = frisch;
                    }

                    var n = new KostenPositionNachweis
                    {
                        Bezeichnung = Text(r, "Bezeichnung"),
                        Gruppe = Text(r, "Gruppe"),
                        Kostenart = Text(r, SchemaKatalog.SPALTE_PW_KOSTENART),
                        Bemessung = bem,
                        Menge = menge,
                        Einheitpreis = D(r, SchemaKatalog.SPALTE_PW_EINHEITPREIS),
                        IstErloes = erloes,
                        SzenarioGepflegt = szenarioGepflegt
                    };
                    n.BetragJahr =
                        string.Equals(bem, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal)
                            ? (erloes && wert > 0 ? -wert : wert)
                            : (szenarioGepflegt
                                ? (erloes && wert > 0 ? -wert : wert)
                                : BetriebskostenCtrl.Betrag(bem, erwartet, n.Menge, n.Einheitpreis, erloes));
                    liste.Add(n);
                }
            }
            catch { }
            return liste;
        }

        private static double Szenariowert(DataRow r, string szenario,
                                           string spalteErwartet, string spalteBest, string spalteWorst)
        {
            double erwartet = D(r, spalteErwartet) ?? 0;
            string spalte = szenario == WirtschaftlichkeitSzenario.BEST ? spalteBest
                          : szenario == WirtschaftlichkeitSzenario.WORST ? spalteWorst : null;
            if (spalte == null) return erwartet;
            double wert = D(r, spalte) ?? 0;
            return wert != 0 ? wert : erwartet;   // 0/leer = kein Szenariowert gepflegt
        }

        /// <summary>ETAPPE H2: die beiden Endenergie-Bemessungen (Konzept § 4.5).
        /// <para><b>Das ist die MENGEN-Frage, nicht die Topf-Frage:</b> Nur diese zwei
        /// Arten holen ihre Bezugsgröße frisch aus dem Lauf
        /// (<see cref="EndenergieMenge"/>). Welcher Preissteigerungstopf zuständig ist,
        /// beantwortet seit FX4-b <see cref="IstEnergiepreisArt"/> — die beiden Fragen
        /// fallen seither auseinander und dürfen nicht zusammengelegt werden.</para></summary>
        private static bool IstEndenergieArt(string bem)
        {
            return string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, StringComparison.Ordinal);
        }

        /// <summary>
        /// PAKET FX4-b (Anwenderentscheid 02.09.2026): die Bemessungsarten, deren Betrag
        /// ein ANTEIL DER ENERGIEKOSTEN ist — sie eskalieren mit p_E statt mit p_B und
        /// gehören deshalb in den Endenergie-Topf von
        /// <see cref="LiesBetriebskostenTopfe"/>.
        ///
        /// <para>Das sind die beiden Endenergie-Arten aus H1 (Wege A und B) <b>und</b>
        /// ihre zwei projektweiten Vorläufer <c>PROZENT_BRENNSTOFFKOSTEN</c> /
        /// <c>PROZENT_STROMKOSTEN</c>. FX3 hatte die Vorläufer noch ausgenommen; der
        /// Anwender hat sie am 02.09.2026 gleichgezogen.</para>
        ///
        /// <para><b>Nicht dabei:</b> der feste Jahresbetrag („Weg C",
        /// <see cref="DbWerte.BEMESSUNG_JAHRESBETRAG"/>/<see cref="DbWerte.BEMESSUNG_BETRAG"/>)
        /// und alle mengenbezogenen Arten (€/kWh, €/kW, €/h …) — ihr Betrag ist kein
        /// Anteil eines Energiepreises.</para>
        /// </summary>
        private static bool IstEnergiepreisArt(string bem)
        {
            return IstEndenergieArt(bem) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN, StringComparison.Ordinal);
        }

        /// <summary>
        /// ETAPPE H2: frische Bezugsmenge einer Endenergie-Position aus dem jüngsten
        /// Lauf. Weg A liefert die Arbeitskosten [€/a]; Weg B den BEWERTETEN Bedarf
        /// (kWh × Strombezugspreis) — <c>Menge × Satz / 100</c> ergibt so ohne zweite
        /// Formel den Betrag (Begründung bei <see cref="BetriebskostenCtrl.Betrag"/>).
        /// null = keine Bezugsgröße (kein Lauf, Anlage nicht im Lauf, Preis fehlt) —
        /// dann gilt die dokumentierte 0.
        /// </summary>
        private static double? EndenergieMenge(int idProjekt, DataRow r, string bem,
                                               ref EndenergieAufloeser aufloeser, ref bool versucht)
        {
            if (!versucht)
            {
                versucht = true;
                aufloeser = EndenergieAufloeser.FuerProjekt(idProjekt);
            }
            if (aufloeser == null) return null;

            int komponente, idAnlage;
            KomponenteUndAnlage(r, out komponente, out idAnlage);

            EndenergieAufloeser.Groesse g = aufloeser.FuerPosition(komponente, idAnlage);
            if (g == null) return null;

            if (string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, StringComparison.Ordinal))
                return g.KostenEuro;

            double? strompreis = aufloeser.StrompreisJeKwh;
            return strompreis.HasValue ? g.BedarfKwh * strompreis.Value : (double?)null;
        }

        /// <summary>Komponente und Anlage der Positionszeile (0 = nicht gesetzt bzw.
        /// Spalte nicht mitgelesen) — gemeinsamer Helfer von H2 und H4a.</summary>
        private static void KomponenteUndAnlage(DataRow r, out int komponente, out int idAnlage)
        {
            komponente = 0;
            idAnlage = 0;
            try
            {
                if (r.Table.Columns.Contains("KomponentenID") && r["KomponentenID"] != DBNull.Value)
                    komponente = Convert.ToInt32(r["KomponentenID"]);
            }
            catch { }
            try
            {
                if (r.Table.Columns.Contains(SchemaKatalog.SPALTE_PW_ID_ANLAGE) &&
                    r[SchemaKatalog.SPALTE_PW_ID_ANLAGE] != DBNull.Value)
                    idAnlage = Convert.ToInt32(r[SchemaKatalog.SPALTE_PW_ID_ANLAGE]);
            }
            catch { }
        }

        /// <summary>ETAPPE H4a: Bemessungsarten mit Ermittlung der Bezugsgröße
        /// (Konzept Kostendialoge § 5.3) — „% der Investition" aus der Kostenwelt,
        /// die kWh-Arten aus dem jüngsten Lauf. ETAPPE H2-1: dazu die sechs
        /// Gerätewelt-Arten (Baugrößen über die Anlagen-Geräteverweise, H4b) —
        /// damit zieht z. B. „Wartung je kW" ihre kW auch auf der Betriebsseite
        /// selbst. „% der Erzeugerkosten" bleibt Kaskadenmaterie der Investseite.
        /// <para>PAKET FX2 (Anwenderentscheid B-4, 02.09.2026): dazu „je Stunde"
        /// (<see cref="DbWerte.BEMESSUNG_EUR_PRO_H"/>) — der Satz [€/h] bleibt Eingabe,
        /// die Stundenzahl kommt aus dem jüngsten Lauf
        /// (<see cref="EndenergieAufloeser.BetriebsstundenH"/>). Damit sind von den vier
        /// Arten des Befundes B-4 drei noch reine Konserve: <c>EUR_PRO_KWH</c>,
        /// <c>PROZENT_BRENNSTOFFKOSTEN</c> und <c>PROZENT_STROMKOSTEN</c>.</para></summary>
        private static bool IstRueckfallErmittelbareArt(string bem)
        {
            return string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KW_LEISTUNG, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KW_HEIZLEISTUNG, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KW_ELEKTRISCH, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWP, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_KAPAZITAET, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_M2_KOLLEKTOR, StringComparison.Ordinal);
        }

        /// <summary>
        /// ETAPPE H4a: frische Bezugsgröße einer Position. ETAPPE H2-1: nicht mehr
        /// nur Rückfall — die Frische gewinnt an den Lesestellen Vorrang vor der
        /// Menge-Spalte, die nach Konzept § 4.5 reine Ausweisgröße ist („Stand des
        /// Laufs"); die Konserve gilt nur noch, wenn hier nichts ermittelbar ist.
        /// null = keine Basis (kein Lauf, kein Gerät, keine Investsumme).
        /// </summary>
        private static double? RueckfallMenge(int idProjekt, DataRow r, string bem,
                                              ref EndenergieAufloeser aufloeser, ref bool versucht)
        {
            int komponente, idAnlage;
            KomponenteUndAnlage(r, out komponente, out idAnlage);

            if (string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal))
                return BetriebskostenCtrl.InvestSummeFuer(idProjekt, komponente, idAnlage);

            // PAKET FX2 (B-4): „je Stunde" holt seine Stundenzahl aus dem Lauf — sonst
            // wie die kWh-Arten. Die Gerätewelt kennt die Art nicht; sie darf deshalb
            // nicht in den BaugroesseSumme-Zweig unten fallen.
            bool ausDemLauf =
                string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal) ||
                string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, StringComparison.Ordinal) ||
                string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, StringComparison.Ordinal);
            if (!ausDemLauf)
                return TechnikPlanwertCtrl.BaugroesseSumme(idProjekt, komponente, bem, idAnlage);

            if (!versucht)
            {
                versucht = true;
                aufloeser = EndenergieAufloeser.FuerProjekt(idProjekt);
            }
            if (aufloeser == null) return null;

            if (string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal))
                return aufloeser.BetriebsstundenH(komponente, idAnlage);

            return string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, StringComparison.Ordinal)
                ? aufloeser.WaermeerzeugungKwh(komponente, idAnlage)
                : aufloeser.StromgroesseKwh(komponente, idAnlage);
        }

        /// <summary>
        /// ETAPPE H2-1 (Konzept BHKW-Wirtschaftlichkeit § 4.5): AUSWEIS der frischen
        /// Bezugsgröße einer Position nach <c>Tab_ProjektWerte.Menge</c> — „Stand des
        /// Laufs" beim Dialog-Speichern. Die Rechenwege lesen ohnehin frisch; der
        /// Ausweis dient dem Dialog und Fremdlesern der Spalte. Geschrieben wird auch
        /// NULL (nichts ermittelbar = ehrlich kein Stand). false = keine ermittelbare
        /// Art (die Menge bleibt Eingabewert, z. B. „je kWh") oder Zeile unauffindbar.
        /// „% der Investition" in Kategorie 1 bemisst sich an der KASKADE (H4b,
        /// Runde 3), nicht an der Kostenwelt-Summe — dort kein Einzelzeilen-Ausweis.
        /// <para>PAKET FX2 (Anwenderentscheid B-4): „je Stunde" zählt seither zu den
        /// ermittelbaren Arten und ist hier OHNE weitere Änderung mitgedeckt — die
        /// Methode fragt <see cref="IstRueckfallErmittelbareArt"/>; ausgewiesen wird
        /// die Stundenzahl des jüngsten Laufs.</para>
        /// </summary>
        internal static bool MengeAusweisen(int positionsId, out double? menge)
        {
            menge = null;
            if (positionsId <= 0) return false;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT w.ProjektID, w.KategorieID, w.KomponentenID, " +
                    "w.[" + SchemaKatalog.SPALTE_PW_BEMESSUNG + "]" +
                    (AnlagenSpalteVorhanden()
                        ? ", w.[" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "] "
                        : " ") +
                    "FROM Tab_ProjektWerte AS w WHERE w.ID = ?",
                    new DbParam("@id", positionsId));
                if (dt == null || dt.Rows.Count == 0) return false;
                DataRow r = dt.Rows[0];

                string bem = Text(r, SchemaKatalog.SPALTE_PW_BEMESSUNG);
                bool endenergie = IstEndenergieArt(bem);
                if (!endenergie && !IstRueckfallErmittelbareArt(bem)) return false;

                int idProjekt = r["ProjektID"] == DBNull.Value ? 0 : Convert.ToInt32(r["ProjektID"]);
                int kategorie = r["KategorieID"] == DBNull.Value ? 0 : Convert.ToInt32(r["KategorieID"]);
                if (kategorie == Form_Kosten.KATEGORIE_INVESTITION &&
                    string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal))
                    return false;

                EndenergieAufloeser aufloeser = null;
                bool versucht = false;
                menge = endenergie
                    ? EndenergieMenge(idProjekt, r, bem, ref aufloeser, ref versucht)
                    : RueckfallMenge(idProjekt, r, bem, ref aufloeser, ref versucht);

                var p = new DbParam("@m", DbParamTyp.Double);
                p.Wert = menge.HasValue ? (object)menge.Value : DBNull.Value;
                DataRepository.ExecuteSQL(
                    "UPDATE Tab_ProjektWerte SET [" + SchemaKatalog.SPALTE_PW_MENGE +
                    "] = ? WHERE ID = ?",
                    p, new DbParam("@id", positionsId));
                return true;
            }
            catch { menge = null; return false; }
        }

        /// <summary>ID des jüngsten Simulationslaufs (Tab_Ergebnis) des Projekts, 0 = keiner.</summary>
        private static int LiesErgebnisId(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT ID FROM " + ErgebnisCtrl.TAB_KOPF +
                    " WHERE ID_Projekt = ? ORDER BY ID DESC LIMIT 1",
                    new DbParam("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o);
            }
            catch { }
            return 0;
        }

        // ------------------------------------------------------------- Persistenz

        /// <summary>
        /// Ersetzt die gespeicherten Ergebnisse der beteiligten Projekte — in EINER
        /// Transaktion über eine eigene Verbindung: kein Teilstand bei Fehlern, und
        /// keine modalen Fehlerdialoge des DataRepository aus dem Hintergrundthread
        /// (Berechne läuft im Task). Ein Persistenzfehler kippt die Anzeige nicht.
        /// </summary>
        private void Persistiere(List<WirtschaftlichkeitErgebnis> ergebnisse,
                                 List<SensitivitaetZeile> sensitivitaet,
                                 Dictionary<int, StromMatrix> matrizen, WirtschaftlichkeitParameter p)
        {
            var projektIds = new HashSet<int>();
            foreach (WirtschaftlichkeitErgebnis e in ergebnisse) projektIds.Add(e.IdProjekt);

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    try
                    {
                        foreach (int id in projektIds)
                        {
                            {
                                List<DbParam> pl = new List<DbParam>();
                                pl.Add(new DbParam("@p", id));
                                v.Ausfuehren("DELETE FROM " + TAB_ERGEBNIS + " WHERE ID_Projekt = ?", pl.ToArray());
                            }
                            {
                                List<DbParam> pl = new List<DbParam>();
                                pl.Add(new DbParam("@p", id));
                                v.Ausfuehren("DELETE FROM " + TAB_SENS + " WHERE ID_Projekt = ?", pl.ToArray());
                            }
                            {
                                List<DbParam> pl = new List<DbParam>();
                                pl.Add(new DbParam("@p", id));
                                v.Ausfuehren("DELETE FROM " + TAB_MATRIX + " WHERE ID_Projekt = ?", pl.ToArray());
                            }
                        }

                        int naechsteId;
                        {
                            object o = v.Skalar("SELECT MAX(ID) FROM " + TAB_ERGEBNIS);
                            naechsteId = (o != null && o != DBNull.Value ? Convert.ToInt32(o) : 0) + 1;
                        }

                        foreach (WirtschaftlichkeitErgebnis e in ergebnisse)
                        {
                            {
                                List<DbParam> pl = new List<DbParam>();
                                pl.Add(new DbParam("@id", naechsteId));
                                pl.Add(new DbParam("@proj", e.IdProjekt));
                                pl.Add(new DbParam("@erg", e.IdErgebnis));
                                pl.Add(new DbParam("@sz", e.Szenario ?? ""));
                                pl.Add(new DbParam("@stamm", e.IstStamm));
                                pl.Add(new DbParam("@anz", e.Anzeige ?? ""));
                                pl.Add(new DbParam("@zeit", DbParamTyp.Date) { Wert = e.Zeitstempel });
                                pl.Add(new DbParam("@z", p.Zinssatz));
                                pl.Add(new DbParam("@t", p.Betrachtungszeitraum));
                                pl.Add(new DbParam("@pe", p.PreissteigerungEnergie));
                                pl.Add(new DbParam("@pb", p.PreissteigerungBetrieb));
                                pl.Add(new DbParam("@ev", p.Einspeiseverguetung));
                                pl.Add(new DbParam("@inv", R(e.Investition)));
                                pl.Add(DbWert(e.BetriebskostenJahr));
                                pl.Add(DbWert(e.EnergiekostenJahr));
                                pl.Add(new DbParam("@einsp", R(e.EinspeiseerloesJahr)));
                                pl.Add(DbWert(e.BarwertAusgaben));
                                pl.Add(DbWert(e.BarwertEinnahmen));
                                pl.Add(new DbParam("@rw", R(e.RestwertBarwert)));
                                pl.Add(DbWert(e.Kapitalwert));
                                pl.Add(DbWert(e.KapitalwertDiff));
                                pl.Add(DbWert(e.AnnuitaetKW));
                                pl.Add(DbWert(e.AmortisationJahre));
                                pl.Add(DbWert(e.Gestehungskosten, 6));
                                pl.Add(DbWert(e.IRR));
                                pl.Add(new DbParam("@behg", R(e.CO2AbgabeJahr)));
                                pl.Add(new DbParam("@kwkg", R(e.KwkgErloesJahr1)));
                                pl.Add(new DbParam("@vbhel", R(e.KwkgVbhElektrisch)));   // E2 (L6)
                                pl.Add(new DbParam("@enst", R(e.EnergiesteuerJahr1)));   // E4
                                pl.Add(new DbParam("@stbe", R(e.StromsteuerBefreiungJahr1)));
                                pl.Add(new DbParam("@sten", R(e.StromsteuerEntlastungJahr1)));
                                pl.Add(new DbParam("@sthk", (object)e.SteuerHerkunft ?? DBNull.Value));
                                pl.Add(new DbParam("@vmar", R(e.VermiedenArbeitJahr)));   // E5
                                pl.Add(new DbParam("@vmle", R(e.VermiedenLeistungJahr)));
                                pl.Add(new DbParam("@vmge", R(e.VermiedenGesamtJahr)));
                                pl.Add(new DbParam("@aufs", R(e.AufschlagJahr)));
                                pl.Add(new DbParam("@epv", R(e.EinspeiseerloesPvJahr)));   // E7
                                pl.Add(new DbParam("@ekwk", R(e.EinspeiseerloesKwkJahr)));
                                pl.Add(new DbParam("@zusch", R(e.Zuschuss)));              // K5
                                pl.Add(new DbParam("@pvf", e.PvVerguetungsform ?? ""));    // P6
                                pl.Add(DbWert(e.PvAnzulegenderWert));
                                pl.Add(new DbParam("@pvmp", R(e.PvMarktpraemie)));
                                pl.Add(new DbParam("@pvak", R(e.PvVerguetungsausfallKwh)));
                                pl.Add(new DbParam("@pvae", R(e.PvVerguetungsausfall)));
                                pl.Add(new DbParam("@pv51", R(e.PvKompensation51a)));
                                pl.Add(new DbParam("@pvkw", R(e.PvKappungsverlustKwh)));
                                pl.Add(DbWert(e.PvVermiedenerBezug));
                                pl.Add(DbWert(e.StromkostenTarif));
                                pl.Add(new DbParam("@hw", (object)e.Hinweis ?? DBNull.Value));
                                pl.Add(new DbParam("@fg", (object)e.Fehlgrund ?? DBNull.Value));
                                v.Ausfuehren("INSERT INTO " + TAB_ERGEBNIS + " (ID, ID_Projekt, ID_Ergebnis, Szenario, " +
                                "IstStamm, Anzeige, Zeitstempel, " +
                                "Zinssatz, Betrachtungszeitraum, Preissteigerung_Energie, Preissteigerung_Betrieb, " +
                                "Einspeiseverguetung, Investition, Betriebskosten, Energiekosten, Einspeiseerloes, " +
                                "BarwertAusgaben, BarwertEinnahmen, Restwert, Kapitalwert, KapitalwertDiff, " +
                                "AnnuitaetKW, AmortisationJahre, Gestehungskosten, " +
                                "IRR, CO2Abgabe, KWKGErloes, " + SPALTE_KWKG_VBH_EL + ", " +
                                SPALTE_ENERGIESTEUER + ", " + SPALTE_STROMST_BEFREIUNG + ", " +
                                SPALTE_STROMST_ENTLASTUNG + ", " + SPALTE_STEUER_HERKUNFT + ", " +
                                SPALTE_VERMIEDEN_ARBEIT + ", " + SPALTE_VERMIEDEN_LEISTUNG + ", " +
                                SPALTE_VERMIEDEN_GESAMT + ", " + SPALTE_AUFSCHLAG_BETRAG + ", " +
                                SPALTE_EINSPEISUNG_PV + ", " + SPALTE_EINSPEISUNG_KWK + ", " +
                                SPALTE_ZUSCHUSS + ", " +
                                SPALTE_PV_FORM + ", " + SPALTE_PV_AW + ", " +
                                SPALTE_PV_MARKTPRAEMIE + ", " + SPALTE_PV_AUSFALL_KWH + ", " +
                                SPALTE_PV_AUSFALL_EUR + ", " + SPALTE_PV_51A + ", " +
                                SPALTE_PV_KAPPUNG_KWH + ", " + SPALTE_PV_VERMIEDEN + ", " +
                                "StromkostenTarif, HinweisText, Fehlgrund) " +
                                "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", pl.ToArray());
                            }
                            naechsteId++;
                        }

                        // Sensitivitätszeilen (W2, Szenario Erwartet).
                        if (sensitivitaet != null && sensitivitaet.Count > 0)
                        {
                            int sensId;
                            {
                                object o = v.Skalar("SELECT MAX(ID) FROM " + TAB_SENS);
                                sensId = (o != null && o != DBNull.Value ? Convert.ToInt32(o) : 0) + 1;
                            }
                            foreach (SensitivitaetZeile z in sensitivitaet)
                            {
                                {
                                    List<DbParam> pl = new List<DbParam>();
                                    pl.Add(new DbParam("@id", sensId));
                                    pl.Add(new DbParam("@p", z.IdProjekt));
                                    pl.Add(new DbParam("@par", z.Parameter ?? ""));
                                    pl.Add(DbWert(z.KwMinus));
                                    pl.Add(DbWert(z.KwBasis));
                                    pl.Add(DbWert(z.KwPlus));
                                    pl.Add(new DbParam("@zeit", DbParamTyp.Date) { Wert = DateTime.Now });
                                    v.Ausfuehren("INSERT INTO " + TAB_SENS + " (ID, ID_Projekt, [Parameter], " +
                                    "KwMinus, KwBasis, KwPlus, Zeitstempel) VALUES (?,?,?,?,?,?,?)", pl.ToArray());
                                }
                                sensId++;
                            }
                        }

                        // Strommengen-Matrix (W3) — eine Zeile je Projekt und Zone.
                        if (matrizen != null && matrizen.Count > 0)
                        {
                            int mxId;
                            {
                                object o = v.Skalar("SELECT MAX(ID) FROM " + TAB_MATRIX);
                                mxId = (o != null && o != DBNull.Value ? Convert.ToInt32(o) : 0) + 1;
                            }
                            foreach (KeyValuePair<int, StromMatrix> kv in matrizen)
                            {
                                foreach (string zone in StromMatrix.Zonen)
                                {
                                    StromMatrix.Zone z = kv.Value.Hole(zone);
                                    if (z == null) continue;
                                    {
                                        List<DbParam> pl = new List<DbParam>();
                                        pl.Add(new DbParam("@id", mxId));
                                        pl.Add(new DbParam("@p", kv.Key));
                                        pl.Add(new DbParam("@z", zone));
                                        pl.Add(new DbParam("@b", Math.Round(z.BezugMWh, 3)));
                                        pl.Add(new DbParam("@pv", Math.Round(z.EinspeisungPvMWh, 3)));
                                        pl.Add(new DbParam("@ke", Math.Round(z.KwkEigenMWh, 3)));
                                        pl.Add(new DbParam("@ki", Math.Round(z.KwkEinspeisungMWh, 3)));
                                        pl.Add(new DbParam("@mx", Math.Round(kv.Value.MaxBezugKW, 1)));
                                        pl.Add(new DbParam("@bd", Math.Round(z.BedarfMWh, 3)));   // E5
                                        pl.Add(new DbParam("@zeit", DbParamTyp.Date) { Wert = DateTime.Now });
                                        v.Ausfuehren("INSERT INTO " + TAB_MATRIX + " (ID, ID_Projekt, [Zone], " +
                                        "BezugMWh, EinspPvMWh, KwkEigenMWh, KwkEinspMWh, MaxBezugKW, " +
                                        "BedarfMWh, Zeitstempel) VALUES (?,?,?,?,?,?,?,?,?,?)", pl.ToArray());
                                    }
                                    mxId++;
                                }
                            }
                        }
                        v.Commit();
                    }
                    catch
                    {
                        try { v.Rollback(); } catch { }
                        throw;
                    }
                }
            }
            catch { /* Ergebnisse bleiben im Speicher; der Reiter meldet beim nächsten Laden den alten Stand */ }
        }

        /// <summary>Persistierte Ergebnisse laden (IWirtschaftlichkeitProvider).</summary>
        public List<WirtschaftlichkeitErgebnis> LadeErgebnisse(List<int> projektIds)
        {
            var liste = new List<WirtschaftlichkeitErgebnis>();
            if (projektIds == null || projektIds.Count == 0) return liste;
            StelleTabellenSicher();
            try
            {
                foreach (int idProjekt in projektIds)
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT * FROM " + TAB_ERGEBNIS + " WHERE ID_Projekt = ?",
                        new DbParam("@p", idProjekt));
                    if (dt == null) continue;
                    foreach (DataRow r in dt.Rows)
                    {
                        var e = new WirtschaftlichkeitErgebnis
                        {
                            IdProjekt = idProjekt,
                            IdErgebnis = (int)(D(r, "ID_Ergebnis") ?? 0),
                            Szenario = r["Szenario"] != DBNull.Value ? r["Szenario"].ToString()
                                                                     : WirtschaftlichkeitSzenario.ERWARTET,
                            IstStamm = B(r, "IstStamm"),
                            Anzeige = r.Table.Columns.Contains("Anzeige") && r["Anzeige"] != DBNull.Value
                                      ? r["Anzeige"].ToString() : "",
                            Investition = D(r, "Investition") ?? 0,
                            BetriebskostenJahr = D(r, "Betriebskosten"),
                            EnergiekostenJahr = D(r, "Energiekosten"),
                            EinspeiseerloesJahr = D(r, "Einspeiseerloes") ?? 0,
                            BarwertAusgaben = D(r, "BarwertAusgaben"),
                            BarwertEinnahmen = D(r, "BarwertEinnahmen"),
                            RestwertBarwert = D(r, "Restwert") ?? 0,
                            Kapitalwert = D(r, "Kapitalwert"),
                            KapitalwertDiff = D(r, "KapitalwertDiff"),
                            AnnuitaetKW = D(r, "AnnuitaetKW"),
                            AmortisationJahre = D(r, "AmortisationJahre"),
                            Gestehungskosten = D(r, "Gestehungskosten"),
                            IRR = D(r, "IRR"),
                            CO2AbgabeJahr = D(r, "CO2Abgabe") ?? 0,
                            KwkgErloesJahr1 = D(r, "KWKGErloes") ?? 0,
                            KwkgVbhElektrisch = D(r, SPALTE_KWKG_VBH_EL) ?? 0,   // E2 (L6)
                            EnergiesteuerJahr1 = D(r, SPALTE_ENERGIESTEUER) ?? 0,          // E4
                            StromsteuerBefreiungJahr1 = D(r, SPALTE_STROMST_BEFREIUNG) ?? 0,
                            StromsteuerEntlastungJahr1 = D(r, SPALTE_STROMST_ENTLASTUNG) ?? 0,
                            SteuerHerkunft = Text(r, SPALTE_STEUER_HERKUNFT).Length > 0
                                             ? Text(r, SPALTE_STEUER_HERKUNFT) : null,
                            VermiedenArbeitJahr = D(r, SPALTE_VERMIEDEN_ARBEIT) ?? 0,        // E5
                            VermiedenLeistungJahr = D(r, SPALTE_VERMIEDEN_LEISTUNG) ?? 0,
                            VermiedenGesamtJahr = D(r, SPALTE_VERMIEDEN_GESAMT) ?? 0,
                            AufschlagJahr = D(r, SPALTE_AUFSCHLAG_BETRAG) ?? 0,
                            EinspeiseerloesPvJahr = D(r, SPALTE_EINSPEISUNG_PV) ?? 0,        // E7
                            EinspeiseerloesKwkJahr = D(r, SPALTE_EINSPEISUNG_KWK) ?? 0,
                            Zuschuss = D(r, SPALTE_ZUSCHUSS) ?? 0,
                            PvVerguetungsform = Text(r, SPALTE_PV_FORM),                  // P6
                            PvAnzulegenderWert = D(r, SPALTE_PV_AW),
                            PvMarktpraemie = D(r, SPALTE_PV_MARKTPRAEMIE) ?? 0,
                            PvVerguetungsausfallKwh = D(r, SPALTE_PV_AUSFALL_KWH) ?? 0,
                            PvVerguetungsausfall = D(r, SPALTE_PV_AUSFALL_EUR) ?? 0,
                            PvKompensation51a = D(r, SPALTE_PV_51A) ?? 0,
                            PvKappungsverlustKwh = D(r, SPALTE_PV_KAPPUNG_KWH) ?? 0,
                            PvVermiedenerBezug = D(r, SPALTE_PV_VERMIEDEN),                        // K5
                            StromkostenTarif = D(r, "StromkostenTarif"),
                            Hinweis = r.Table.Columns.Contains("HinweisText") && r["HinweisText"] != DBNull.Value
                                      ? r["HinweisText"].ToString() : null,
                            Fehlgrund = r["Fehlgrund"] != DBNull.Value ? r["Fehlgrund"].ToString() : null
                        };
                        if (r["Zeitstempel"] != DBNull.Value) e.Zeitstempel = Convert.ToDateTime(r["Zeitstempel"]);
                        liste.Add(e);
                    }
                }
            }
            catch { }
            return liste;
        }

        /// <summary>Persistierte Sensitivitätszeilen laden (IWirtschaftlichkeitProvider, W2).</summary>
        public List<SensitivitaetZeile> LadeSensitivitaet(List<int> projektIds)
        {
            var liste = new List<SensitivitaetZeile>();
            if (projektIds == null || projektIds.Count == 0) return liste;
            StelleTabellenSicher();
            try
            {
                foreach (int idProjekt in projektIds)
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT * FROM " + TAB_SENS + " WHERE ID_Projekt = ? ORDER BY ID",
                        new DbParam("@p", idProjekt));
                    if (dt == null) continue;
                    foreach (DataRow r in dt.Rows)
                        liste.Add(new SensitivitaetZeile
                        {
                            IdProjekt = idProjekt,
                            Parameter = r["Parameter"] != DBNull.Value ? r["Parameter"].ToString() : "",
                            KwMinus = D(r, "KwMinus"),
                            KwBasis = D(r, "KwBasis"),
                            KwPlus = D(r, "KwPlus")
                        });
                }
            }
            catch { }
            return liste;
        }

        /// <summary>Persistierte Strommengen-Matrizen laden (IWirtschaftlichkeitProvider, W3).</summary>
        public Dictionary<int, StromMatrix> LadeStromMatrix(List<int> projektIds)
        {
            var map = new Dictionary<int, StromMatrix>();
            if (projektIds == null || projektIds.Count == 0) return map;
            StelleTabellenSicher();
            try
            {
                foreach (int idProjekt in projektIds)
                {
                    DataTable dt = DataRepository.GetDataTable(
                        "SELECT * FROM " + TAB_MATRIX + " WHERE ID_Projekt = ? ORDER BY ID",
                        new DbParam("@p", idProjekt));
                    if (dt == null || dt.Rows.Count == 0) continue;

                    var m = new StromMatrix();
                    foreach (DataRow r in dt.Rows)
                    {
                        string zone = r["Zone"] != DBNull.Value ? r["Zone"].ToString() : "";
                        if (zone.Length == 0) continue;
                        m.ZonenWerte[zone] = new StromMatrix.Zone
                        {
                            Name = zone,
                            BezugMWh = D(r, "BezugMWh") ?? 0,
                            EinspeisungPvMWh = D(r, "EinspPvMWh") ?? 0,
                            KwkEigenMWh = D(r, "KwkEigenMWh") ?? 0,
                            KwkEinspeisungMWh = D(r, "KwkEinspMWh") ?? 0,
                            BedarfMWh = D(r, "BedarfMWh") ?? 0        // E5
                        };
                        double mx = D(r, "MaxBezugKW") ?? 0;
                        if (mx > m.MaxBezugKW) m.MaxBezugKW = mx;
                    }
                    map[idProjekt] = m;
                }
            }
            catch { }
            return map;
        }

        /// <summary>true, wenn ein gespeichertes Ergebnis zum aktuellen Simulationslauf passt.</summary>
        public bool ErgebnisAktuell(WirtschaftlichkeitErgebnis e)
        {
            return e != null && e.Fehlgrund == null &&
                   e.IdErgebnis > 0 && e.IdErgebnis == LiesErgebnisId(e.IdProjekt);
        }

        // ------------------------------------------------------------- Hilfen

        private static bool B(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return false;
            try { return Convert.ToBoolean(r[spalte]); } catch { return false; }
        }

        private static double? D(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); } catch { return null; }
        }

        /// <summary>Textspalte, tolerant gegen fehlende Spalte und NULL (Etappe E3).</summary>
        private static string Text(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "";
            try { return Convert.ToString(r[spalte]).Trim(); } catch { return ""; }
        }

        private static double R(double v, int dez = 2) { return Math.Round(v, dez); }

        private static DbParam DbWert(double? v, int dez = 2)
        {
            return new DbParam("@w", DbParamTyp.Double)
            { Wert = v.HasValue ? (object)Math.Round(v.Value, dez) : DBNull.Value };
        }
    }
}
