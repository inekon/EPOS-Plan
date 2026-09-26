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
    /// keine UI-Abhängigkeit. Die Seite „Wirtschaftlichkeit" (UcWirtschaftlichkeit) und der
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

        // =====================================================================
        // ETAPPE E7c3 (Befund B‑6) — gescheiterte Rechenstufen werden sichtbar
        // =====================================================================

        /// <summary>
        /// ETAPPE E7c3 (Befund B‑6): Rechenstufen dieses Laufs, die an einem Fehler
        /// scheiterten — je Projekt (Schlüssel = Projekt-ID) bzw. für die ganze Gruppe
        /// (Schlüssel 0). Jede Stufe rechnet weiter mit ihrem benannten Rückfall (leere
        /// Anlagenliste, Vorgabeparameter, Flat-Tarif …), aber jede Ergebniszeile des
        /// Projekts trägt eine Kohärenzzeile „Rechenstufe „X“ nicht ausführbar:
        /// &lt;Grund&gt;“ (<see cref="StufenfehlerAnhaengen"/>). Bis E7c3 fing hier
        /// <c>catch { }</c>, und der Kapitalwert sah aus wie ein vollständiger.
        /// </summary>
        private readonly Dictionary<int, List<string>> _stufenfehler = new Dictionary<int, List<string>>();

        /// <summary>Ressourcenschlüssel der Rechenstufen, die scheitern können (B‑6).</summary>
        internal const string STUFE_PARAMETER = "WIRT_STUFE_PARAMETER";
        internal const string STUFE_TARIF = "WIRT_STUFE_TARIF";
        internal const string STUFE_ANLAGEN = "WIRT_STUFE_ANLAGEN";
        internal const string STUFE_LEISTUNG = "WIRT_STUFE_LEISTUNG";
        internal const string STUFE_TRAEGER = "WIRT_STUFE_TRAEGER";
        internal const string STUFE_HEIZOEL = "WIRT_STUFE_HEIZOEL";
        internal const string STUFE_BETRIEBSKOSTEN = "WIRT_STUFE_BETRIEBSKOSTEN";
        internal const string STUFE_KATALOG = "WIRT_STUFE_KATALOG";
        internal const string STUFE_KOHAERENZ = "WIRT_STUFE_KOHAERENZ";
        internal const string STUFE_SATZHERLEITUNG = "WIRT_STUFE_SATZHERLEITUNG";
        internal const string STUFE_SPEICHERN = "WIRT_STUFE_SPEICHERN";
        internal const string STUFE_LADEN = "WIRT_STUFE_LADEN";

        /// <summary>„Rechenstufe „X“ nicht ausführbar: &lt;Grund&gt;“.</summary>
        internal static string StufeNichtAusfuehrbar(string schluessel, string grund)
        {
            return string.Format(BerichtTexte.Kultur,
                T("WIRT_STUFE_NICHT_AUSFUEHRBAR", "Rechenstufe „{0}“ nicht ausführbar: {1}"),
                T(schluessel, schluessel), grund);
        }

        /// <summary>Merkt eine gescheiterte Stufe für ein Projekt (0 = die ganze Gruppe);
        /// dieselbe Zeile steht nur einmal da.</summary>
        private void Stufenfehler(int idProjekt, string schluessel, string grund)
        {
            if (string.IsNullOrEmpty(grund)) return;
            string zeile = StufeNichtAusfuehrbar(schluessel, grund);
            if (!_stufenfehler.TryGetValue(idProjekt, out List<string> liste))
            {
                liste = new List<string>();
                _stufenfehler[idProjekt] = liste;
            }
            if (!liste.Contains(zeile)) liste.Add(zeile);
        }

        /// <summary>
        /// Hängt die gescheiterten Stufen der Gruppe und des Projekts als WARNUNG an die
        /// Kohärenzzeilen eines Ergebnisses — ohne Betrag, jede Zeile einmal. Ohne Fehler
        /// geschieht nichts (Basisprojekte: Zeile für Zeile wie vorher).
        /// </summary>
        private void StufenfehlerAnhaengen(WirtschaftlichkeitErgebnis erg)
        {
            if (erg == null || _stufenfehler.Count == 0) return;
            foreach (int schluessel in new[] { 0, erg.IdProjekt })
            {
                if (!_stufenfehler.TryGetValue(schluessel, out List<string> liste)) continue;
                if (erg.KohaerenzHinweise == null) erg.KohaerenzHinweise = new List<KohaerenzHinweis>();
                foreach (string z in liste)
                {
                    bool schon = false;
                    foreach (KohaerenzHinweis h in erg.KohaerenzHinweise)
                        if (string.Equals(h.Text, z, StringComparison.Ordinal)) { schon = true; break; }
                    if (!schon)
                        erg.KohaerenzHinweise.Add(new KohaerenzHinweis { Schwere = KohaerenzSchwere.WARNUNG, Text = z });
                }
            }
        }

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

        /// <summary>
        /// ETAPPE B6 — der Modus, in dem § 9 Abs. 1 Nr. 3 StromStG in DIESEN Lauf
        /// eingegangen ist (<c>AUSWEIS</c>/<c>ERLOES</c>, Werte aus
        /// <see cref="DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS"/>). Er steht im ERGEBNIS,
        /// nicht nur im Parametersatz: Ein gespeicherter Lauf muss auch nach einer
        /// späteren Umstellung des Projekts sagen können, wie ER gerechnet hat.
        /// <inheritdoc cref="SPALTE_ENERGIESTEUER" path="/summary/para"/>
        /// </summary>
        public const string SPALTE_STROMST_MODUS = "StromsteuerBefreiungModus";

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
        /// ETAPPE W5‑B‑10 (VALERI): Barwert der Ersatzbeschaffungen in
        /// <see cref="TAB_ERGEBNIS"/> [€]. Über <c>SpalteSicher</c> — dieselbe
        /// Begründung wie bei <see cref="SPALTE_ENERGIESTEUER"/>: Ergebnisspalten führt
        /// dieses Modul selbst, Eingabespalten der Migrationskatalog.
        /// </summary>
        public const string SPALTE_ERSATZ_BARWERT = "ErsatzBarwert";

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
        /// <summary>
        /// ETAPPE B7P — der NACHWEISUMSCHLAG des Ergebnisses in
        /// <see cref="TAB_ERGEBNIS"/>: ein JSON-Text mit Praefix <c>nw1:</c>
        /// (<see cref="ErgebnisNachweisUmschlag"/>), der die Zeilenlisten eines Laufs
        /// traegt — Modulnachweis, Energiekosten je Anlage, Betriebskostenpositionen,
        /// Kohaerenzzeilen — und die vier Skalare, die es bis B7P ebenfalls nur im
        /// frischen Lauf gab. Ueber <c>SpalteSicher</c> — dieselbe Begruendung wie bei
        /// <see cref="SPALTE_ENERGIESTEUER"/>.
        ///
        /// <para><b>Warum ein Umschlag und nicht dreissig Spalten.</b> Es sind LISTEN
        /// mit je einem Dutzend Feldern, deren Laenge an der Zahl der Anlagen und
        /// Kostenpositionen haengt. In Spalten waeren das vier weitere Tabellen mit
        /// eigenem Schema, eigenem Schreib- und Leseweg und eigener Migration — fuer
        /// Daten, die reiner AUSWEIS sind und aus denen nichts gerechnet wird. NULL
        /// heisst „kein Umschlag": Der Lauf laedt dann wie vor B7P, nur ohne
        /// Unterzeilen.</para>
        /// </summary>
        public const string SPALTE_NACHWEIS_JSON = "Nachweis_Json";

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

        /// <summary>Fristen des § 6 KWKG 2025 (Konzept Kap. 8.2, Phase 9).
        ///
        /// <para><b>ETAPPE E7c (A20, Entscheid E7‑Q3 Lesart b):</b> Die feste
        /// Realisierungsfrist von vier Jahren ab dem Stichtag ist entfallen. An ihre Stelle
        /// tritt das Katalogdatum „Ende der Frist zur Inbetriebnahme"
        /// (<c>DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE</c>, 31.12.2030), gelesen über
        /// <see cref="FristendeInbetriebnahme"/> — ohne Katalogwert keine stille Vorgabe,
        /// sondern eine Herleitungszeile.</para></summary>
        public static readonly DateTime KWKG_STICHTAG_ENDE = new DateTime(2026, 12, 31);
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

        /// <summary>
        /// Zieht die drei Ergebnisspalten nach, die kein Schemaschritt führt, und sät den
        /// Katalog gesetzlicher Parameter. <b>Die Tabellen selbst legt sie nicht an.</b>
        /// </summary>
        /// <remarks>
        /// <para><b>Warum die fünf Tabellen hier nicht entstehen.</b> <see cref="TAB_PARAMETER"/>,
        /// <see cref="TAB_ERGEBNIS"/>, <see cref="TAB_SENS"/>, <see cref="TAB_TARIF"/> und
        /// <see cref="TAB_MATRIX"/> stehen im Grundschema (<c>sql/schema/001_grundschema.sql</c>),
        /// aus dem jede Datenbank hervorgeht — unter Windows über die Auslieferungsvorlage
        /// (<see cref="Erstbereitstellung"/>), auf iOS über die Seed-Kopie, in Tests und
        /// Referenzlauf über die Testdatenbank. Alle fünf sind dort <c>STRICT</c>; Parameter-
        /// und Tariftabelle tragen ihren Fremdschlüssel auf <c>Tab_Projekt</c> mit
        /// <c>ON DELETE CASCADE</c> seit dem Grundschema, die drei Ergebnistabellen bekommen ihn
        /// in Schemaschritt 96 (<see cref="ProjektFremdschluessel"/>). Eine Anlage an dieser
        /// Stelle entstünde ohne Fremdschlüssel und ohne <c>STRICT</c>, und Schritt 96 baute eine
        /// solche Tabelle nicht um, sondern bräche an ihr ab
        /// (<see cref="ProjektFremdschluessel.Zieltext"/>) — die Simulation bliebe gesperrt.
        /// Fehlt eine Tabelle doch, nennen Laden und Speichern den Datenbankfehler
        /// (<see cref="Ladefehler"/>, <see cref="Speicherfehler"/>).</para>
        ///
        /// <para><b>Warum die Eingabespalten hier nicht nachgezogen werden.</b> Jede Spalte, die
        /// ein Schemaschritt anlegt (20, 21, 22, 23, 28, 61, 71, 72, 88, 89, 92, 93, 105, 116,
        /// 118, 125), ist auf jeder Datenbank garantiert, auf der dieser Controller läuft: Die
        /// Windows-Schale führt <c>SchemaMigration.Ausfuehren</c> bei jedem Start vor dem ersten
        /// Fenster aus und sperrt bei Fehlschlag die Simulation
        /// (<see cref="SchemaStand.SimulationGesperrt"/>); die Seed-Datenbank der iOS-Hülle und
        /// die Testdatenbank stehen auf <see cref="SchemaStand.Zielversion"/>. Ein zweiter
        /// DDL-Ort holte zudem Entferntes zurück: <see cref="SchemaKatalog.Schritt21_Tarifmodell"/>
        /// führt <c>Aufschlaege_Anwenden</c>, das Schemaschritt 85 entfernt hat — ein Nachzug aus
        /// dieser Liste legte die Spalte bei jedem Zugriff wieder an (Wache:
        /// <c>WirtschaftlichkeitCtrlTabellenTests</c>).</para>
        ///
        /// <para><b>Was bleibt, und warum.</b> Drei Ergebnisspalten führt weder das Grundschema
        /// noch ein Schemaschritt: <see cref="SPALTE_STROMST_MODUS"/> (Etappe B6),
        /// <see cref="SPALTE_ERSATZ_BARWERT"/> (W5-B-10) und <see cref="SPALTE_NACHWEIS_JSON"/>
        /// (B7P). Die Testdatenbank bekommt sie einzeln über <c>Werkzeuge/Testdatenbankschema</c>,
        /// eine Anwenderdatenbank aus einer älteren Auslieferungsvorlage nur hier. Bis ein
        /// Schemaschritt sie führt, bleibt dieser additive Nachzug (<see cref="SpalteSicher"/>:
        /// <c>ALTER TABLE … ADD COLUMN</c> nur bei nachweislichem Fehlen, kein DML); ein
        /// Fehlschlag steht in <see cref="Vorsorgewarnung"/>.</para>
        /// </remarks>
        public void StelleTabellenSicher()
        {
            // ETAPPE E13 (E7c3-Q6): Die Warnung gilt für DIESE Vorsorge — seit die
            // Oberfläche sie zeigt, darf ein längst behobener Fehler nicht stehen bleiben.
            Vorsorgewarnung = null;

            SpalteSicher(TAB_ERGEBNIS, SPALTE_STROMST_MODUS, "TEXT(20)");   // B6
            SpalteSicher(TAB_ERGEBNIS, SPALTE_ERSATZ_BARWERT, "DOUBLE");    // W5-B-10
            SpalteSicher(TAB_ERGEBNIS, SPALTE_NACHWEIS_JSON, "LONGTEXT");   // B7P

            // Katalog gesetzlicher Parameter (Etappe E1, Leitentscheidung L2). Eigene
            // Verbindung, eigener Fang: Ein Fehlschlag darf die Spalten oben nicht
            // gefährden, und umgekehrt.
            GesetzKatalog.StelleKatalogSicher();
        }

        /// <summary>
        /// ETAPPE E7c3 (Befund B‑6) — der letzte Fehler der Schemavorsorge
        /// (<see cref="StelleTabellenSicher"/>, <see cref="SpalteSicher"/>); <c>null</c> =
        /// keiner. Die Vorsorge bleibt still (kein Dialog beim Start), aber der Grund ist
        /// abrufbar statt verschluckt; die Folgen nennen Laden und Speichern selbst.
        /// ETAPPE E13: Jede Vorsorge setzt sie zu Beginn zurück; Statuszeile der
        /// Ergebnisseite und BHKW-Dialog zeigen sie (<see cref="Fehlergrund.Anzeigezeilen"/>).
        /// </summary>
        public static string Vorsorgewarnung { get; private set; }

        private static void Vorsorgefehler(Exception ex)
        {
            Vorsorgewarnung = Fehlergrund.Text(ex);
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
            catch (Exception ex)
            {
                Vorsorgefehler(ex);   // E7c3 (B‑6): benannt — der Rückgabewert ist nur Migrationsanker
                return false;
            }
        }

        /// <summary>
        /// Eine DDL-Anweisung, still. <see cref="StilleDb"/> wirft nicht; geworfen wird hier
        /// von Hand, damit der Fang in <see cref="SpalteSicher"/> greift: Ein misslungenes
        /// ALTER meldet „nicht neu angelegt" und nennt den Grund in
        /// <see cref="Vorsorgewarnung"/>.
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
                // ETAPPE E7c3 (B‑6): der strenge Leseweg (StilleDb.TabelleStreng) — ein
                // Abfragefehler erreicht den benannten Fang, statt als leere Tabelle
                // still „Vorgaben" zu heißen.
                DataTable dt = StilleDb.TabelleStreng(
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
                    p.IdKraftwerkspark = (int)(D(r, "ID_Kraftwerkspark") ?? 0);
                    p.RefKesselWirkungsgrad = D(r, "RefKessel_Wirkungsgrad") ?? p.RefKesselWirkungsgrad;
                    p.RefKesselIdBrennstoff = (int)(D(r, "RefKessel_ID_Brennstoff") ?? p.RefKesselIdBrennstoff);
                    if (r.Table.Columns.Contains("KWKG_Stichtag") && r["KWKG_Stichtag"] != DBNull.Value)
                        p.KwkgStichtag = Convert.ToDateTime(r["KWKG_Stichtag"]);
                    if (r.Table.Columns.Contains("KWKG_Inbetriebnahme") && r["KWKG_Inbetriebnahme"] != DBNull.Value)
                        p.KwkgInbetriebnahme = Convert.ToDateTime(r["KWKG_Inbetriebnahme"]);
                    p.KwkgAbschlagNegativ = D(r, "KWKG_Abschlag_Negativ") ?? 0;

                    // ETAPPE K6 — die Pauschale des Projekts. Tatbestand und Anlagenart
                    // sind mit Schemaschritt 90 entfallen, der Kostenanteil mit
                    // Schemaschritt 91; alle drei stehen seit Schritt 89 an der Anlage
                    // und werden dort gelesen.
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

                    // KONZEPT § 2.9 - die Referenz der Differenzrechnung (Schritt 92).
                    // NULL heisst Stamm, und genau dafuer steht die 0: Eine nicht
                    // migrierte Datenbank verhaelt sich wie eine migrierte und wie der
                    // Bestand. Geprueft wird die Zugehoerigkeit zur Gruppe erst beim
                    // Rechnen (Referenzwahl) - eine geloeschte Variante wird hier NICHT
                    // still bereinigt.
                    p.IdReferenzprojekt = (int)(D(r, SchemaKatalog.SPALTE_PW_REFERENZPROJEKT) ?? 0);

                    // ETAPPE B6 - der Modus des § 9 Abs. 1 Nr. 3 StromStG (Schritt 88).
                    // NUR der ausdrueckliche Wert ERLOES bucht die Befreiung als Erloes;
                    // leer, NULL und jeder unbekannte Bestandswert bedeuten AUSWEIS.
                    // Eine nicht migrierte Datenbank verhaelt sich dadurch wie eine
                    // migrierte - und wie die Vorgabe.
                    p.StromsteuerBefreiungModus =
                        string.Equals(Text(r, SchemaKatalog.SPALTE_PW_STROMST_BEFREIUNG_MODUS),
                                      DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES, StringComparison.Ordinal)
                            ? DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES
                            : DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS;

                    // ETAPPE E5 — die KWK-Einspeisevergütung; ohne ausdrückliche Angabe
                    // wirkungslos (DOUBLE bleibt NULL).
                    //
                    // SP-E-2: Der Aufschlagsschalter Aufschlaege_Anwenden wird NICHT
                    // MEHR GELESEN. Die Preisanteile zerlegen den Arbeitspreis, sie
                    // kommen nicht mehr auf ihn — es gibt nichts an- oder abzuschalten.
                    // Die Spalte ist mit Schemaschritt 85 entfallen
                    // (StrompreisAltspalten).
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

                    // ETAPPE W5-B-9 - der Szenario-Parametersatz. NULL heißt VORGABE,
                    // nicht 0: Ein nie gepflegtes Feld soll bei einer geänderten
                    // Projektangabe MITZIEHEN, und das kann nur eine leere Spalte.
                    // Deshalb hier bewusst kein "?? 0".
                    p.SatzBest = LiesSatz(r, WirtschaftlichkeitSzenario.BEST,
                        SchemaKatalog.SPALTE_PW_SZEN_BEST_ZINS,
                        SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_E,
                        SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_B,
                        SchemaKatalog.SPALTE_PW_SZEN_BEST_INVEST,
                        SchemaKatalog.SPALTE_PW_SZEN_BEST_ERTRAG,
                        SchemaKatalog.SPALTE_PW_SZEN_BEST_DAUER,
                        SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_I);
                    p.SatzWorst = LiesSatz(r, WirtschaftlichkeitSzenario.WORST,
                        SchemaKatalog.SPALTE_PW_SZEN_WORST_ZINS,
                        SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_E,
                        SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_B,
                        SchemaKatalog.SPALTE_PW_SZEN_WORST_INVEST,
                        SchemaKatalog.SPALTE_PW_SZEN_WORST_ERTRAG,
                        SchemaKatalog.SPALTE_PW_SZEN_WORST_DAUER,
                        SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_I);

                    // ETAPPE E9a (Schritte B und D, Schemaschritte 116 und 118) - die vier
                    // Rahmen- und Erloesgroessen je Szenario. NULL (und 0) heisst hier "wie
                    // Erwartet" (E9a-Q5 a); eine 0 wird deshalb schon beim Lesen leer, damit
                    // NurVorgaben und die Herkunftszeile sie nicht als Pflege zaehlen.
                    LiesRahmen(r, p.SatzBest,
                        SchemaKatalog.SPALTE_PW_SZEN_BEST_ZEITRAUM,
                        SchemaKatalog.SPALTE_PW_SZEN_BEST_MENGE,
                        SchemaKatalog.SPALTE_PW_VERGUETUNG_BEST,
                        SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK_BEST);
                    LiesRahmen(r, p.SatzWorst,
                        SchemaKatalog.SPALTE_PW_SZEN_WORST_ZEITRAUM,
                        SchemaKatalog.SPALTE_PW_SZEN_WORST_MENGE,
                        SchemaKatalog.SPALTE_PW_VERGUETUNG_WORST,
                        SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK_WORST);

                    // ETAPPE W5-B-12 - p_I und die nicht monetaeren Wirkungen. Auch hier
                    // bewusst KEIN "?? 0": NULL heisst bei p_I "wie p_B" und nicht
                    // "0 %/a", und ein leerer Freitext ist "nichts erfasst".
                    p.PreissteigerungInvestition = D(r, SchemaKatalog.SPALTE_PW_PREIS_I);
                    p.NichtMonetaer = Text(r, SchemaKatalog.SPALTE_PW_NICHT_MONETAER);

                    // ETAPPE E15 (V-G7, Schritt 125) - das Risikomodul. Vorgabe AUS: Eine
                    // leere, fehlende oder unbekannte Art heisst "kein Risiko" (normiert beim
                    // Lesen); die drei Zahlen bleiben nullbar, ohne "?? 0".
                    p.RisikoArt = Risikoart.Normiert(Text(r, SchemaKatalog.SPALTE_PW_RISIKO_ART));
                    p.RisikoZinszuschlag = D(r, SchemaKatalog.SPALTE_PW_RISIKO_ZINSZUSCHLAG);
                    p.RisikoVerlust = D(r, SchemaKatalog.SPALTE_PW_RISIKO_VERLUST);
                    p.RisikoWahrscheinlichkeit = D(r, SchemaKatalog.SPALTE_PW_RISIKO_WAHRSCHEINLICHKEIT);

                    if (r["GeaendertAm"] != DBNull.Value) p.GeaendertAm = Convert.ToDateTime(r["GeaendertAm"]);
                }
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt statt catch { } — der Lauf rechnet mit den
                // Vorgaben weiter, jede Ergebniszeile nennt den Grund (Berechne).
                p.Lesefehler = Fehlergrund.Text(ex);
            }
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
        /// ETAPPE W5‑B‑9: einen Szenario-Parametersatz aus der Parameterzeile lesen.
        /// <para>Jedes Feld bleibt <c>null</c>, wenn die Spalte fehlt oder NULL ist —
        /// und <c>null</c> heißt VORGABE. Eine nie migrierte Datenbank verhält sich
        /// dadurch wie eine frisch migrierte.</para>
        /// </summary>
        private static SzenarioSatz LiesSatz(DataRow r, string szenario, string sZins,
                                             string sPreisE, string sPreisB, string sInvest,
                                             string sErtrag, string sDauer, string sPreisI)
        {
            return new SzenarioSatz
            {
                Szenario = szenario,
                Zinssatz = D(r, sZins),
                PreissteigerungEnergie = D(r, sPreisE),
                PreissteigerungBetrieb = D(r, sPreisB),
                InvestitionAenderung = D(r, sInvest),
                ErtragAenderung = D(r, sErtrag),
                NutzungsdauerAenderung = D(r, sDauer),
                // ETAPPE W5-B-12: die siebte Groesse des Satzes (Schritt 72).
                PreissteigerungInvestition = D(r, sPreisI)
            };
        }

        /// <summary>
        /// ETAPPE E9a (Schritte B und D): die vier Rahmen- und Erlösgrößen eines Satzes aus
        /// der Parameterzeile. Jedes Feld bleibt <c>null</c>, wenn die Spalte fehlt, NULL
        /// oder 0 ist — und <c>null</c> heißt hier „wie Erwartet". Eine nie migrierte
        /// Datenbank rechnet dadurch wie eine migrierte ohne Pflege.
        /// </summary>
        private static void LiesRahmen(DataRow r, SzenarioSatz satz, string sZeitraum,
                                       string sMenge, string sVerguetung, string sVerguetungKwk)
        {
            if (satz == null) return;
            double? zeitraum = OhneNull(D(r, sZeitraum));
            satz.Zeitraum = zeitraum.HasValue ? (int?)(int)Math.Round(zeitraum.Value) : null;
            satz.Menge = OhneNull(D(r, sMenge));
            satz.Einspeiseverguetung = OhneNull(D(r, sVerguetung));
            satz.EinspeiseverguetungKwk = OhneNull(D(r, sVerguetungKwk));
        }

        /// <summary>ETAPPE E9a: „NULL/0 heißt wie Erwartet" — eine 0 wird leer.</summary>
        private static double? OhneNull(double? wert)
        {
            return wert.HasValue && wert.Value != 0 ? wert : null;
        }

        /// <summary>ETAPPE W5‑B‑9: ein nullbarer Szenariowert als Parameter — <c>null</c>
        /// muss LEER in die Datenbank, sonst ginge die Aussage „Vorgabe“ verloren.</summary>
        private static DbParam SzenParam(double? wert)
        {
            return new DbParam("@sz", DbParamTyp.Double)
            { Wert = wert.HasValue ? (object)wert.Value : DBNull.Value };
        }

        /// <summary>ETAPPE E9a: dieselbe Nullregel für einen ganzzahligen Szenariowert (der
        /// Betrachtungszeitraum je Szenario) — nicht gepflegt geht LEER in die Datenbank.</summary>
        private static DbParam SzenParam(int? wert)
        {
            return new DbParam("@szg", DbParamTyp.Integer)
            { Wert = wert.HasValue ? (object)wert.Value : DBNull.Value };
        }

        /// <summary>
        /// ETAPPE E15 (V‑G7): die Art des Risikos als Parameter — normiert
        /// (<see cref="Risikoart.Normiert"/>); „kein Risiko" geht LEER in die Datenbank.
        /// </summary>
        private static DbParam RisikoArtParam(string art)
        {
            string n = Risikoart.Normiert(art);
            return new DbParam("@risiko", DbParamTyp.VarWChar, 10)
            { Wert = n != null ? (object)n : DBNull.Value };
        }

        /// <summary>
        /// KONZEPT § 2.9 — die gewählte Referenz als Parameter. <b>0 geht als
        /// <c>NULL</c> in die Datenbank</b>: „Stamm" ist die Abwesenheit einer Wahl,
        /// und eine geschriebene 0 wäre ein Verweis auf ein Projekt, das es nicht gibt
        /// (dieselbe Regel wie bei jeder FK-Spalte dieses Schemas).
        /// </summary>
        private static DbParam RefParam(int idReferenzprojekt)
        {
            return new DbParam("@ref", DbParamTyp.Integer)
            { Wert = idReferenzprojekt > 0 ? (object)idReferenzprojekt : DBNull.Value };
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
            catch (Exception)
            {
                // Eine gescheiterte PRÜFUNG darf niemals eine gelingende RECHNUNG
                // verhindern. Der Prüfer fängt selbst schon alles ab; dieser Block ist
                // die zweite Sicherung an der Nahtstelle zum Rechenweg (ETAPPE E7c3:
                // benannt, ohne Rechenwirkung — die Prüfung ist reine Protokollwarnung).
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
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannter Rückfall — Gefunden bleibt false, und die
                // gespeicherten Vorgaben des Referenzkessels gelten weiter (derselbe Weg
                // wie ohne Kessel im Stammprojekt; die Nachweiszeile nennt die Vorgabe).
            }
            _refKesselCache[idStamm] = info;
            return info;
        }

        public bool SpeichereParameter(WirtschaftlichkeitParameter p)
        {
            Speicherfehler = null;   // ETAPPE E7c3 (B‑6)
            if (p == null || p.IdStamm <= 0) return false;
            StelleTabellenSicher();
            try
            {
                int rows = DataRepository.ExecuteNonQuery(
                    "UPDATE " + TAB_PARAMETER + " SET Zinssatz = ?, Betrachtungszeitraum = ?, " +
                    "Preissteigerung_Energie = ?, Preissteigerung_Betrieb = ?, " +
                    "Einspeiseverguetung = ?, CO2_Preis = ?, ID_Kraftwerkspark = ?, " +
                    "RefKessel_Wirkungsgrad = ?, RefKessel_ID_Brennstoff = ?, " +
                    "KWKG_Stichtag = ?, KWKG_Inbetriebnahme = ?, KWKG_Abschlag_Negativ = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_UNTERNEHMENSART + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_RAEUMLICH + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_HOCHEFFIZIENZ + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_NUTZUNGSGRAD + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_ENERGIESTEUER_WAHL + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_AUFTEILUNG + "] = ?, " +
                    // ETAPPE B6 - der Modus des § 9 Abs. 1 Nr. 3 StromStG (Schritt 88).
                    "[" + SchemaKatalog.SPALTE_PW_STROMST_BEFREIUNG_MODUS + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_BILANZJAHR + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_EMISSIONSMETHODE + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_BIOMASSE_KONVENTION + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_BIOMASSE_NACHWEIS + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_KWKG_PAUSCHALMODUS + "] = ?, " +
                    // ETAPPE W5-B-9 - die zwölf Szenariospalten. Reihenfolge wie in
                    // SchemaKatalog.Schritt71_Szenarioparameter.
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_ZINS + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_E + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_B + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_INVEST + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_ERTRAG + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_DAUER + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_ZINS + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_E + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_B + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_INVEST + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_ERTRAG + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_DAUER + "] = ?, " +
                    // ETAPPE W5-B-12 - p_I (Projekt, Best, Worst) und der Freitext.
                    // Reihenfolge wie in SchemaKatalog.Schritt72_ValeriErgaenzung.
                    "[" + SchemaKatalog.SPALTE_PW_PREIS_I + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_I + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_I + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_NICHT_MONETAER + "] = ?, " +
                    // KONZEPT § 2.9 - die Referenz der Differenzrechnung (Schritt 92).
                    "[" + SchemaKatalog.SPALTE_PW_REFERENZPROJEKT + "] = ?, " +
                    // ETAPPE E9a - Szenariorahmen (Schritt 116) und Erloessaetze der
                    // Parametertabelle (Schritt 118), Reihenfolge wie in SchemaKatalog.
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_ZEITRAUM + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_ZEITRAUM + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_MENGE + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_MENGE + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_BEST + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_WORST + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK_BEST + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK_WORST + "] = ?, " +
                    // ETAPPE E15 - das Risikomodul (Schritt 125), Reihenfolge wie in SchemaKatalog.
                    "[" + SchemaKatalog.SPALTE_PW_RISIKO_ART + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_RISIKO_ZINSZUSCHLAG + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_RISIKO_VERLUST + "] = ?, " +
                    "[" + SchemaKatalog.SPALTE_PW_RISIKO_WAHRSCHEINLICHKEIT + "] = ?, " +
                    "GeaendertAm = ? WHERE ID_Projekt = ?",
                    new DbParam("@z", p.Zinssatz),
                    new DbParam("@t", p.Betrachtungszeitraum),
                    new DbParam("@pe", p.PreissteigerungEnergie),
                    new DbParam("@pb", p.PreissteigerungBetrieb),
                    new DbParam("@ev", p.Einspeiseverguetung),
                    new DbParam("@co2", p.CO2Preis),
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
                    // ETAPPE B6: Geschrieben wird IMMER einer der beiden Steuerwerte -
                    // NULL waere zwar gleichbedeutend mit AUSWEIS, aber eine gepflegte
                    // Wahl soll auch als gepflegt dastehen.
                    new DbParam("@stmo", DbParamTyp.VarWChar, 20)
                    { Wert = Steuerwert(p.StromsteuerBefreiungModus,
                                        DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS) },
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
                    new DbParam("@kpau", DbParamTyp.Boolean) { Wert = p.KwkgPauschalmodus },
                    // ETAPPE W5-B-9: ein nicht gepflegtes Feld muss LEER in die
                    // Datenbank - es ist die Aussage „Vorgabe“ und etwas anderes als 0.
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).Zinssatz),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).PreissteigerungEnergie),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).PreissteigerungBetrieb),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).InvestitionAenderung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).ErtragAenderung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).NutzungsdauerAenderung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).Zinssatz),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).PreissteigerungEnergie),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).PreissteigerungBetrieb),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).InvestitionAenderung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).ErtragAenderung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).NutzungsdauerAenderung),
                    // ETAPPE W5-B-12: dieselbe Nullregel wie bei den zwoelf Spalten
                    // darueber - "nicht gepflegt" muss LEER in die Datenbank. Bei p_I
                    // waere eine geschriebene 0 die Aussage "Investitionsgueter werden
                    // nie teurer" statt "wie p_B".
                    SzenParam(p.PreissteigerungInvestition),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).PreissteigerungInvestition),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).PreissteigerungInvestition),
                    // LongVarWChar wie bei WQ_Wochenwerte: MEMO, nicht TEXT(n) - der
                    // Access-Rueckweg schnitte einen langen Freitext bei VarWChar ab.
                    new DbParam("@nm", DbParamTyp.LongVarWChar)
                    { Wert = LeerAlsNull(p.NichtMonetaer) },
                    // KONZEPT § 2.9: "Stamm" ist die Abwesenheit einer Wahl und muss
                    // LEER in die Datenbank - eine geschriebene 0 waere ein Verweis auf
                    // ein Projekt, das es nicht gibt.
                    RefParam(p.IdReferenzprojekt),
                    // ETAPPE E9a: dieselbe Nullregel - nicht gepflegt heisst LEER, und
                    // leer heisst hier "wie Erwartet".
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).Zeitraum),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).Zeitraum),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).Menge),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).Menge),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).Einspeiseverguetung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).Einspeiseverguetung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).EinspeiseverguetungKwk),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).EinspeiseverguetungKwk),
                    // ETAPPE E15: dieselbe Nullregel - "nicht gepflegt" geht LEER in die
                    // Datenbank, und eine leere Art heisst "kein Risiko".
                    RisikoArtParam(p.RisikoArt),
                    SzenParam(p.RisikoZinszuschlag),
                    SzenParam(p.RisikoVerlust),
                    SzenParam(p.RisikoWahrscheinlichkeit),
                    new DbParam("@am", DbParamTyp.Date) { Wert = DateTime.Now },
                    new DbParam("@p", p.IdStamm));
                if (rows > 0) return true;
                // ETAPPE E13 (E7c3‑Q6): -1 = die Zugriffsschicht hat den Fehler schon gemeldet.
                // Ein INSERT danach träfe den eindeutigen Index und meldete einen ZWEITEN,
                // falschen Grund („UNIQUE constraint failed") — der Grund erscheint einmal.
                if (rows < 0) return false;

                int id = DataRepository.GetMaxID(TAB_PARAMETER, "ID") + 1;
                return DataRepository.ExecuteSQL(
                    "INSERT INTO " + TAB_PARAMETER + " (ID, ID_Projekt, Zinssatz, Betrachtungszeitraum, " +
                    "Preissteigerung_Energie, Preissteigerung_Betrieb, Einspeiseverguetung, " +
                    "CO2_Preis, ID_Kraftwerkspark, RefKessel_Wirkungsgrad, " +
                    "RefKessel_ID_Brennstoff, KWKG_Stichtag, KWKG_Inbetriebnahme, " +
                    "KWKG_Abschlag_Negativ, " +
                    "[" + SchemaKatalog.SPALTE_PW_UNTERNEHMENSART + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_RAEUMLICH + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_HOCHEFFIZIENZ + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_NUTZUNGSGRAD + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_ENERGIESTEUER_WAHL + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_AUFTEILUNG + "], " +
                    // ETAPPE B6 - der Modus des § 9 Abs. 1 Nr. 3 StromStG (Schritt 88).
                    "[" + SchemaKatalog.SPALTE_PW_STROMST_BEFREIUNG_MODUS + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_BILANZJAHR + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_EMISSIONSMETHODE + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_BIOMASSE_KONVENTION + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_BIOMASSE_NACHWEIS + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_KWKG_PAUSCHALMODUS + "], " +
                    // ETAPPE W5-B-9 - die zwölf Szenariospalten, Reihenfolge wie im UPDATE.
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_ZINS + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_E + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_B + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_INVEST + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_ERTRAG + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_DAUER + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_ZINS + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_E + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_B + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_INVEST + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_ERTRAG + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_DAUER + "], " +
                    // ETAPPE W5-B-12 - die vier Spalten des Schritts 72, Reihenfolge
                    // wie im UPDATE darueber.
                    "[" + SchemaKatalog.SPALTE_PW_PREIS_I + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_PREIS_I + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_PREIS_I + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_NICHT_MONETAER + "], " +
                    // KONZEPT § 2.9 - die Referenz der Differenzrechnung (Schritt 92).
                    "[" + SchemaKatalog.SPALTE_PW_REFERENZPROJEKT + "], " +
                    // ETAPPE E9a - Schritte 116 und 118, Reihenfolge wie im UPDATE.
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_ZEITRAUM + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_ZEITRAUM + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_BEST_MENGE + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_SZEN_WORST_MENGE + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_BEST + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_WORST + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK_BEST + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_VERGUETUNG_KWK_WORST + "], " +
                    // ETAPPE E15 - Schritt 125, Reihenfolge wie im UPDATE.
                    "[" + SchemaKatalog.SPALTE_PW_RISIKO_ART + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_RISIKO_ZINSZUSCHLAG + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_RISIKO_VERLUST + "], " +
                    "[" + SchemaKatalog.SPALTE_PW_RISIKO_WAHRSCHEINLICHKEIT + "], " +
                    "GeaendertAm) " +
                    "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?," +
                    "?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                    new DbParam("@id", id),
                    new DbParam("@p", p.IdStamm),
                    new DbParam("@z", p.Zinssatz),
                    new DbParam("@t", p.Betrachtungszeitraum),
                    new DbParam("@pe", p.PreissteigerungEnergie),
                    new DbParam("@pb", p.PreissteigerungBetrieb),
                    new DbParam("@ev", p.Einspeiseverguetung),
                    new DbParam("@co2", p.CO2Preis),
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
                    // ETAPPE B6: Geschrieben wird IMMER einer der beiden Steuerwerte -
                    // NULL waere zwar gleichbedeutend mit AUSWEIS, aber eine gepflegte
                    // Wahl soll auch als gepflegt dastehen.
                    new DbParam("@stmo", DbParamTyp.VarWChar, 20)
                    { Wert = Steuerwert(p.StromsteuerBefreiungModus,
                                        DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS) },
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
                    new DbParam("@kpau", DbParamTyp.Boolean) { Wert = p.KwkgPauschalmodus },
                    // ETAPPE W5-B-9 - Reihenfolge wie im UPDATE darüber; nicht gepflegt
                    // heißt LEER, nicht 0.
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).Zinssatz),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).PreissteigerungEnergie),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).PreissteigerungBetrieb),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).InvestitionAenderung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).ErtragAenderung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).NutzungsdauerAenderung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).Zinssatz),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).PreissteigerungEnergie),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).PreissteigerungBetrieb),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).InvestitionAenderung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).ErtragAenderung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).NutzungsdauerAenderung),
                    // ETAPPE W5-B-12 - Reihenfolge wie im UPDATE darueber; nicht
                    // gepflegt heisst LEER, nicht 0 bzw. nicht Leerstring.
                    SzenParam(p.PreissteigerungInvestition),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).PreissteigerungInvestition),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).PreissteigerungInvestition),
                    new DbParam("@nm", DbParamTyp.LongVarWChar)
                    { Wert = LeerAlsNull(p.NichtMonetaer) },
                    // KONZEPT § 2.9 - Reihenfolge wie im UPDATE darueber; 0 heisst
                    // Stamm und geht als NULL in die Datenbank.
                    RefParam(p.IdReferenzprojekt),
                    // ETAPPE E9a - Reihenfolge wie im UPDATE darueber; nicht gepflegt
                    // heisst LEER ("wie Erwartet").
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).Zeitraum),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).Zeitraum),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).Menge),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).Menge),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).Einspeiseverguetung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).Einspeiseverguetung),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.BEST).EinspeiseverguetungKwk),
                    SzenParam(Satz(p, WirtschaftlichkeitSzenario.WORST).EinspeiseverguetungKwk),
                    // ETAPPE E15 - Reihenfolge wie im UPDATE darueber.
                    RisikoArtParam(p.RisikoArt),
                    SzenParam(p.RisikoZinszuschlag),
                    SzenParam(p.RisikoVerlust),
                    SzenParam(p.RisikoWahrscheinlichkeit),
                    new DbParam("@am", DbParamTyp.Date) { Wert = DateTime.Now });
            }
            catch (Exception ex)
            {
                Speicherfehler = Fehlergrund.Text(ex);   // ETAPPE E7c3 (B‑6): benannt
                return false;
            }
        }

        /// <summary>
        /// ETAPPE E7c3 (Befund B‑6): der Grund, aus dem das letzte Speichern von Parametern
        /// oder Tarif (<see cref="SpeichereParameter"/>, <c>SpeichereTarif</c>) scheiterte;
        /// die Methoden melden das Scheitern weiter über <c>false</c>, der Dialog kann den
        /// Grund daneben nennen. <c>null</c> = kein Fehler seit dem letzten Aufruf.
        /// ETAPPE E13: Einen Fehler der Datenbankanweisung meldet die Zugriffsschicht selbst
        /// (<c>DataRepository.FehlerMelden</c>) — dann bleibt die Eigenschaft <c>null</c>, damit
        /// derselbe Grund nicht ein zweites Mal erscheint; sie trägt die übrigen Fehler.
        /// </summary>
        public string Speicherfehler { get; private set; }

        /// <summary>ETAPPE W5‑B‑9: der Satz eines Szenarios, nie <c>null</c> — ein
        /// Parametersatz ohne Szenariosatz (etwa aus einem Test) speichert dann lauter
        /// Vorgaben, also lauter NULL.</summary>
        private static SzenarioSatz Satz(WirtschaftlichkeitParameter p, string szenario)
        {
            SzenarioSatz s = p != null ? p.SatzFuer(szenario) : null;
            return s ?? SzenarioSatz.Vorgabe(szenario);
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
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannter Rückfall — ohne Variantenliste zählt der
                // Stamm allein; ErzeugerVorhanden blendet im Zweifel ein (Fail-open).
            }

            f.Bhkw = ErzeugerVorhanden("Tab_BHKW", ids);
            f.Photovoltaik = ErzeugerVorhanden("Tab_PV", ids);
            f.Heizkessel = ErzeugerVorhanden("Tab_Heizkessel", ids);
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
            catch (Exception)
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
                // ETAPPE E7c3 (B‑6): der strenge Leseweg, damit der Fang unten greift.
                DataTable dt = StilleDb.TabelleStreng(
                    "SELECT * FROM " + TAB_TARIF + " WHERE ID_Projekt = ?",
                    new DbParam("@p", idStamm));
                if (dt != null && dt.Rows.Count > 0)
                {
                    DataRow r = dt.Rows[0];
                    t.Aktiv = B(r, "Aktiv");
                    t.WinterVonMonat = (int)(D(r, "Winter_Von") ?? t.WinterVonMonat);
                    t.WinterBisMonat = (int)(D(r, "Winter_Bis") ?? t.WinterBisMonat);

                    // Q11 (E7b, „kein HT/NT"): Die Spalten des Zonenmodells — HT-Fenster,
                    // vier Zonen-Bezugs- und vier Zonen-Einspeisepreise, die zweistufige
                    // Staffel — liest der Kern nicht mehr. Die Staffel steht seit
                    // Schemaschritt 104 am Stromträger (LeistungspreisStaffel).

                    // ETAPPE E5 — Rollenmodell. Ein LEERER Modus heißt dasselbe wie ZONEN:
                    // ein Satz des entfallenen Zonenmodells, der nicht rechnet.
                    string modus = Text(r, SchemaKatalog.SPALTE_TARIF_MODUS);
                    t.Modus = modus.Length > 0 ? modus : DbWerte.TARIF_MODUS_ZONEN;
                    if (r.Table.Columns.Contains(SchemaKatalog.SPALTE_TARIF_GUELTIGAB) &&
                        r[SchemaKatalog.SPALTE_TARIF_GUELTIGAB] != DBNull.Value)
                        t.GueltigAb = Convert.ToDateTime(r[SchemaKatalog.SPALTE_TARIF_GUELTIGAB]);

                    LiesRolle(r, "Bezug_", t.Bezug);
                    LiesRolle(r, "Rest_", t.Reststrom);
                    t.Einspeisung.ArbeitspreisEurKWh = D(r, "Einsp_Arbeit") ?? 0;
                    t.Einspeisung.GrundpreisEurJahr = D(r, "Einsp_Grundpreis") ?? 0;
                }
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt statt catch { } — ein unlesbarer Tarif gilt wie
                // bisher als nicht aktiv (Flat-Pfad), aber der Grund reist mit (Berechne).
                t.Aktiv = false;
                t.Lesefehler = Fehlergrund.Text(ex);
            }
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
            Speicherfehler = null;   // ETAPPE E7c3 (B‑6)
            if (t == null || t.IdStamm <= 0) return false;
            StelleTabellenSicher();
            try
            {
                // Spaltenliste und Werte entstehen aus EINER Quelle — bei 40 Spalten
                // wäre eine von Hand gepflegte Fragezeichenkette die klassische
                // Fehlerquelle (ETAPPE E5; ohne die dreizehn des Zonenmodells seit Q11/E7b).
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
                if (rows < 0) return false;   // ETAPPE E13: gemeldet — kein zweiter Grund per INSERT

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
            catch (Exception ex)
            {
                Speicherfehler = Fehlergrund.Text(ex);   // ETAPPE E7c3 (B‑6): benannt
                return false;
            }
        }

        /// <summary>
        /// Spaltenreihenfolge des Tarifsatzes — EINE Wahrheit für UPDATE und INSERT.
        ///
        /// <para><b>Ohne die Spalten des Zonenmodells</b> (Q11, E7b): HT-Fenster,
        /// Zonenpreise und Staffel schreibt der Kern nicht mehr — ein UPDATE lässt sie
        /// stehen, ein INSERT lässt sie leer. Die Tabelle behält sie bis zu einem
        /// späteren Aufräumschritt (kein DDL mit E7b).</para>
        /// </summary>
        private static List<string> TarifSpalten()
        {
            var s = new List<string>
            {
                "Aktiv", "Winter_Von", "Winter_Bis",
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
        ///
        /// <para>Die Referenz der Differenzrechnung ist die GRUPPENREFERENZ
        /// <see cref="WirtschaftlichkeitParameter.IdReferenzprojekt"/> (0 = Stamm,
        /// Konzept § 2.9). Wer eine andere braucht — die Vergleichssicht „Zwei Stände"
        /// übergibt A —, nimmt die Überladung mit <c>idReferenz</c>.</para>
        /// </summary>
        public List<WirtschaftlichkeitErgebnis> Berechne(BerichtsDaten daten, WirtschaftlichkeitParameter p)
        {
            return Berechne(daten, p, 0);
        }

        /// <summary>
        /// KONZEPT § 2.9 und § 2.15 — <b>welche Referenz gilt</b>, in dieser Reihenfolge:
        /// der ausdrückliche Lauf-Parameter, dann die Vergleichssicht des Laufs
        /// (Sicht 2 setzt A), zuletzt die Gruppenreferenz des Parametersatzes. Und 0
        /// heißt am Ende der Kette: Stamm.
        ///
        /// <para>Die Kette steht EINMAL hier, damit Kennzahlen, Verlauf und Bericht
        /// nicht drei verschiedene Referenzen nehmen können.</para>
        /// </summary>
        private static int Referenz(BerichtsDaten daten, WirtschaftlichkeitParameter p,
                                    int idReferenz)
        {
            if (idReferenz > 0) return idReferenz;
            if (daten != null && daten.Sicht != null && daten.Sicht.IstPaar)
                return daten.Sicht.Referenz;
            return p != null ? p.IdReferenzprojekt : 0;
        }

        /// <summary>
        /// KONZEPT § 2.9 — dieselbe Rechnung mit AUSDRÜCKLICH gewählter Referenz.
        /// </summary>
        /// <param name="idReferenz">
        /// <c>Tab_Projekt.ID</c> der Referenz dieses Laufs. <b>0 = die Gruppenreferenz</b>
        /// aus <see cref="WirtschaftlichkeitParameter.IdReferenzprojekt"/>, und die
        /// wiederum 0 = Stamm.
        ///
        /// <para>Die Sicht 2 der Ergebnisansicht (§ 2.15, VG‑Q1) übergibt hier A und
        /// schreibt die Gruppenreferenz NICHT um: Amortisation und interner Zinsfuß
        /// hängen an der Jahresreihe der Differenz und sind nicht linear — es gibt
        /// deshalb nur EINEN Rechenweg für Differenzkennzahlen, und das ist dieser.</para>
        /// </param>
        public List<WirtschaftlichkeitErgebnis> Berechne(BerichtsDaten daten,
            WirtschaftlichkeitParameter p, int idReferenz)
        {
            return Berechne(daten, p, idReferenz, true);
        }

        /// <summary>
        /// KONZEPT § 2.15 — dieselbe Rechnung, ohne zu persistieren.
        /// </summary>
        /// <param name="persistieren">
        /// <c>false</c> für die Vergleichssicht „Zwei Stände": Sie ist ein
        /// <b>Erkundungswerkzeug</b> — ihre Differenzen gelten gegen A und nicht gegen
        /// die Unterlassensalternative der Gruppe. Geschrieben in
        /// <c>Tab_ErgebnisWirtschaftlichkeit</c> wären sie eine zweite Wahrheit neben
        /// dem Lauf, aus dem der Bericht reproduzierbar sein soll. Wer einen
        /// Paarvergleich dauerhaft will, wählt A als Gruppenreferenz (§ 2.9).
        /// </param>
        public List<WirtschaftlichkeitErgebnis> Berechne(BerichtsDaten daten,
            WirtschaftlichkeitParameter p, int idReferenz, bool persistieren)
        {
            List<SensitivitaetZeile> sensitivitaet;
            return Berechne(daten, p, idReferenz, persistieren, out sensitivitaet);
        }

        /// <summary>
        /// ETAPPE E5 (V‑A) — dieselbe Rechnung, die zusätzlich ihre
        /// <b>Sensitivitätszeilen</b> herausgibt (Szenario Erwartet, gegen die Referenz
        /// DIESES Laufs). Ein Lauf ohne Persistenz (Sicht 2) schreibt sie nicht in die
        /// Datenbank; Seite und Bericht brauchen sie trotzdem — sonst stünde neben den
        /// Differenzen gegen A eine Sensitivität gegen die Gruppenreferenz.
        /// </summary>
        /// <param name="sensitivitaet">Die Zeilen dieses Laufs, mit Stufe und Steigung.</param>
        public List<WirtschaftlichkeitErgebnis> Berechne(BerichtsDaten daten,
            WirtschaftlichkeitParameter p, int idReferenz, bool persistieren,
            out List<SensitivitaetZeile> sensitivitaet)
        {
            var alle = new List<WirtschaftlichkeitErgebnis>();
            var sens = new List<SensitivitaetZeile>();
            sensitivitaet = sens;
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
            _stufenfehler.Clear();                                         // Etappe E7c3 (B‑6)
            if (p.Lesefehler != null) Stufenfehler(0, STUFE_PARAMETER, p.Lesefehler);
            if (tarif != null && tarif.Lesefehler != null) Stufenfehler(0, STUFE_TARIF, tarif.Lesefehler);

            // KONZEPT § 2.9: WOGEGEN gerechnet wird. Der Lauf-Parameter schlaegt die
            // Gruppenreferenz, die Gruppenreferenz schlaegt den Stamm. Steht die
            // gewaehlte Referenz nicht (mehr) in der Gruppe, faellt die Wahl auf den
            // Stamm zurueck - aber BENANNT, nie still (Randfall 2).
            Referenzwahl wahl = Referenzwahl.Bestimme(daten, Referenz(daten, p, idReferenz));
            if (wahl.Warnung != null && daten.Warnungen != null &&
                !daten.Warnungen.Contains(wahl.Warnung)) daten.Warnungen.Add(wahl.Warnung);

            foreach (string szenario in WirtschaftlichkeitSzenario.Alle)
            {
                // ETAPPE W5-B-9 (Anwenderentscheid 09.09.2026): der Parametersatz, mit
                // dem DIESES Szenario rechnet. Fuer ERWARTET ist es p selbst - dieselbe
                // Referenz, nicht eine wertgleiche Kopie; damit ist die
                // Zahlengleichheit des Erwartungsfalls eine Eigenschaft des Codes und
                // keine Behauptung. Fuer Best/Worst eine Kopie mit ersetztem Zins und
                // ersetzten Preissteigerungen (Begruendung an FuerSzenario).
                WirtschaftlichkeitParameter ps = p.FuerSzenario(szenario);

                // ERST ALLE STAENDE RECHNEN, DANN DIE DIFFERENZEN (Konzept § 2.9): Die
                // Referenz darf irgendwo in der Gruppe stehen. Solange sie fest der
                // Stamm war, genuegte ein Durchlauf - der Stamm steht zuerst. Eine
                // gewaehlte Variante kann hinter den Staenden stehen, deren Differenz
                // sie traegt; die Reihenfolge von alle[] und sens[] bleibt unveraendert.
                var eingaben = new List<ProjektEingabe>();
                var bilder = new List<KapitalwertRechner.Zahlungsbild>();
                var ergebnisse = new List<WirtschaftlichkeitErgebnis>();

                foreach (VariantenDaten v in daten.Varianten)
                {
                    // ETAPPE E9a (V‑E): die Mengen- und Preisbasis DIESES Szenarios —
                    // Mengenfaktor (Schritt B) und Trägerpreise (Schritt C) an einer Stelle.
                    // Ohne Pflege (und immer für ERWARTET) dieselbe Referenz wie v.
                    VariantenDaten vs = Szenariodaten(v, ps, szenario);
                    ProjektEingabe eingabe = BaueEingabe(vs, ps, tarif, szenario);
                    // ETAPPE E15 (V‑G7): der Risikoabzug je Periode — für jeden Stand außer der
                    // Referenz dieses Laufs (RisikoModul.AbzugFuerStand, die eine Regel).
                    eingabe.Risikoabzug = RisikoModul.AbzugFuerStand(ps, wahl.IstReferenz(v.IdProjekt),
                                                                     daten.Varianten.Count);
                    // Die gespeicherte Strommatrix bleibt die des ERWARTUNGSfalls: Er wird
                    // zuerst gerechnet, und nur die erste Matrix je Projekt wird gemerkt.
                    if (eingabe.Matrix != null && !matrizen.ContainsKey(v.IdProjekt))
                        matrizen[v.IdProjekt] = eingabe.Matrix;
                    WirtschaftlichkeitErgebnis erg = RechneProjekt(vs, ps, eingabe,
                        szenario, out KapitalwertRechner.Zahlungsbild bild);
                    alle.Add(erg);
                    eingaben.Add(eingabe); bilder.Add(bild); ergebnisse.Add(erg);
                }

                int refIndex = -1;
                for (int i = 0; i < ergebnisse.Count; i++)
                    if (ergebnisse[i].IdProjekt == wahl.IdReferenz) { refIndex = i; break; }

                ProjektEingabe refEingabe = refIndex >= 0 ? eingaben[refIndex] : null;
                KapitalwertRechner.Zahlungsbild refBild = refIndex >= 0 ? bilder[refIndex] : null;
                WirtschaftlichkeitErgebnis refErg = refIndex >= 0 ? ergebnisse[refIndex] : null;

                // RANDFALL (§ 2.9): Die Referenz selbst liess sich nicht rechnen. Der
                // Sammler hat sie zuvor nachgerechnet wie jede Variante; scheitert auch
                // das, gibt es keine Differenzkennzahlen - und der Grund steht da,
                // statt dass still der Stamm einspringt.
                if (refIndex >= 0 && refBild == null && szenario == WirtschaftlichkeitSzenario.ERWARTET)
                {
                    string satz = Referenzwahl.ReferenzFehlt(
                        string.IsNullOrEmpty(refErg.Anzeige) ? wahl.Anzeige : refErg.Anzeige,
                        refErg.Fehlgrund ?? refErg.Hinweis);
                    if (daten.Warnungen != null && !daten.Warnungen.Contains(satz))
                        daten.Warnungen.Add(satz);
                }

                for (int i = 0; i < ergebnisse.Count; i++)
                {
                    // Die REFERENZ selbst bekommt keine Differenzkennzahlen (§ 2.9).
                    if (i == refIndex) continue;

                    WirtschaftlichkeitErgebnis erg = ergebnisse[i];
                    KapitalwertRechner.Zahlungsbild bild = bilder[i];
                    ProjektEingabe eingabe = eingaben[i];

                    if (bild != null && refBild != null &&
                        erg.Kapitalwert.HasValue && refErg != null && refErg.Kapitalwert.HasValue)
                    {
                        erg.KapitalwertDiff = erg.Kapitalwert.Value - refErg.Kapitalwert.Value;
                        // W5-B-9: mit dem Zins DIESES Szenarios - sonst annuisierte die
                        // Best-Zeile ihren Kapitalwert mit dem Erwartungszins.
                        erg.AnnuitaetKW = erg.KapitalwertDiff.Value *
                            KapitalwertRechner.Annuitaet(ps.Zinssatz / 100.0, ps.Betrachtungszeitraum);
                        erg.AmortisationJahre = KapitalwertRechner.AmortisationDifferenz(bild, refBild);
                        erg.IRR = KapitalwertRechner.InternerZinsfuss(bild, refBild);   // W2
                        // ETAPPE E5 (V‑A, Befund A2): Wie oft wechselt DIESELBE Reihe ihr
                        // Vorzeichen? Mehr als einmal = der Zinsfuß ist mehrdeutig, keinmal =
                        // es gibt keinen. Reiner Ausweis; der Zinsfuß selbst bleibt, wie er ist.
                        erg.IrrVorzeichenwechsel = KapitalwertRechner.Vorzeichenwechsel(bild, refBild);

                        // Sensitivitätsanalyse (W2): nur Szenario Erwartet.
                        if (szenario == WirtschaftlichkeitSzenario.ERWARTET &&
                            refEingabe != null && eingabe.Energie.HasValue && refEingabe.Energie.HasValue)
                            // Der Sensitivitaetslauf haengt am Szenario ERWARTET - dort ist
                            // ps dieselbe Referenz wie p.
                            sens.AddRange(BaueSensitivitaet(erg.IdProjekt, eingabe, refEingabe, ps,
                                                            erg.KapitalwertDiff.Value));
                    }
                }
            }

            if (persistieren) Persistiere(alle, sens, matrizen, p);
            return alle;
        }

        /// <summary>
        /// ETAPPE E5 (U4) — die <b>Bandbreite dreier Szenarien</b>: DERSELBE Rechenweg wie
        /// <see cref="Berechne(BerichtsDaten, WirtschaftlichkeitParameter, int, bool)"/> —
        /// drei vollständige Läufe mit je eigenem Parametersatz —, aber <b>ohne zu
        /// persistieren</b> (Muster: Sicht 2 aus § 2.15). Sie ist eine Auskunft über die
        /// Gruppe, kein gebuchter Lauf; der gespeicherte Stand bleibt der, aus dem der
        /// Bericht reproduzierbar sein soll.
        /// </summary>
        /// <param name="idReferenz">0 = die Gruppenreferenz (bzw. A in Sicht 2), und die
        /// wiederum 0 = Stamm — dieselbe Kette wie in <c>Berechne</c>.</param>
        public WirtschaftlichkeitBandbreite BerechneBandbreite(BerichtsDaten daten,
            WirtschaftlichkeitParameter p, int idReferenz)
        {
            List<WirtschaftlichkeitErgebnis> alle = Berechne(daten, p, idReferenz, false);
            return WirtschaftlichkeitBandbreite.Bilde(daten, alle, Referenz(daten, p, idReferenz));
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
            return BerechneVerlauf(daten, p, jahre, szenario, 0);
        }

        /// <summary>
        /// KONZEPT § 2.9 und § 2.15 — derselbe Verlauf mit AUSDRÜCKLICH gewählter
        /// Referenz. Die Differenzlinien laufen gegen sie statt gegen den Stamm; in
        /// Sicht 2 ist das A, und die eine gezeichnete Linie ist B − A (VG‑Q5).
        /// </summary>
        /// <param name="idReferenz">0 = die Gruppenreferenz aus
        /// <see cref="WirtschaftlichkeitParameter.IdReferenzprojekt"/>, und die
        /// wiederum 0 = Stamm.</param>
        public WirtschaftlichkeitVerlauf BerechneVerlauf(BerichtsDaten daten,
            WirtschaftlichkeitParameter p, int jahre, string szenario, int idReferenz)
        {
            var verlauf = new WirtschaftlichkeitVerlauf
            {
                Jahre = Math.Max(1, jahre),
                Szenario = szenario ?? WirtschaftlichkeitSzenario.ERWARTET
            };
            if (daten == null || daten.Varianten.Count == 0 || p == null) return verlauf;

            // ETAPPE W5-B-9: erst das Szenario, dann der Horizont. FuerSzenario gibt
            // fuer ERWARTET p selbst zurueck - die Kopie danach ist also unverzichtbar,
            // sonst schriebe der Verlauf den Betrachtungszeitraum in den gespeicherten
            // Parametersatz.
            WirtschaftlichkeitParameter ph = p.FuerSzenario(verlauf.Szenario).Kopie();
            ph.Betrachtungszeitraum = verlauf.Jahre;

            TarifParameter tarif = LadeTarif(daten.IdStamm);
            _staffelCache = null; _pelCache.Clear(); _oelCache.Clear();   // frischer Lauf
            _anlagenCache.Clear(); _gesetze = null;                       // wie in Berechne
            _kesselCache.Clear();                                         // Etappe B3 Paket a
            _brennstoffKategorie = null; _carrierBrennstoff = null;
            _traegerCache.Clear();

            // KONZEPT § 2.9: WOGEGEN die Differenzlinien laufen. Dieselbe Auflösung wie
            // in Berechne — sonst zeigte der Verlauf eine andere Referenz als die
            // Kennzahltafel.
            Referenzwahl wahl = Referenzwahl.Bestimme(daten, Referenz(daten, p, idReferenz));
            // ETAPPE E6: Der Verlauf nennt seine Referenz — ein Kopf „Δ ‹Stand› − ‹Referenz›"
            // liest sie hier statt „Stamm" anzunehmen.
            verlauf.IdReferenz = wahl.IdReferenz;

            // KONZEPT § 2.15 (VG‑Q5): In Sicht 2 wird EINE Differenzkurve gezeichnet,
            // B − A; ihr Nulldurchgang ist die dynamische Amortisation des Paars. Mit
            // A = Gruppenreferenz wäre die A-Kurve die Nulllinie.
            int nurDieser = daten.Sicht != null && daten.Sicht.IstPaar ? daten.Sicht.IdB : 0;

            VerlaufSerie referenz = null;
            foreach (VariantenDaten v in daten.Varianten)
            {
                var serie = new VerlaufSerie
                {
                    IdProjekt = v.IdProjekt,
                    Anzeige = v.IstStamm ? "Stamm" : v.Anzeige,
                    IstStamm = v.IstStamm
                };

                // ETAPPE E9a: dieselbe Mengen- und Preisbasis des Szenarios wie im Hauptlauf —
                // sonst zeigte die Linie eines Szenarios eine andere Zahl als seine Kennzahl.
                VariantenDaten vs = Szenariodaten(v, ph, verlauf.Szenario);
                ProjektEingabe eingabe = BaueEingabe(vs, ph, tarif, verlauf.Szenario);
                // ETAPPE E15 (V‑G7): derselbe Risikoabzug wie im Hauptlauf — dieselbe Regel.
                eingabe.Risikoabzug = RisikoModul.AbzugFuerStand(ph, wahl.IstReferenz(v.IdProjekt),
                                                                 daten.Varianten.Count);
                if (vs.Fehler != null || vs.Ergebnis == null)
                    serie.Fehlgrund = vs.Fehler ?? "Kein Simulationsergebnis vorhanden.";
                else if (!eingabe.Energie.HasValue)
                    // AUFTRAG #267: derselbe benannte Grund wie in RechneProjekt —
                    // der Verlauf zeigte bisher „Energiekosten nicht bestimmbar." und
                    // ließ den Anwender damit allein.
                    serie.Fehlgrund = !string.IsNullOrEmpty(vs.EnergiekostenGrund)
                        ? vs.EnergiekostenGrund : "Energiekosten nicht bestimmbar.";
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
                if (wahl.IstReferenz(v.IdProjekt)) referenz = serie;
            }

            // Differenzlinien Variante − Referenz (nur wenn beide Reihen vorliegen).
            // Die Referenz selbst bekommt keine Linie — sie wäre die Nulllinie.
            if (referenz != null && referenz.Kumuliert != null)
                foreach (VerlaufSerie s in verlauf.Absolut)
                {
                    if (s.IdProjekt == referenz.IdProjekt || s.Kumuliert == null) continue;
                    if (nurDieser > 0 && s.IdProjekt != nurDieser) continue;
                    var d = new double[verlauf.Jahre + 1];
                    for (int t = 0; t <= verlauf.Jahre; t++)
                        d[t] = s.Kumuliert[t] - referenz.Kumuliert[t];
                    verlauf.Differenz.Add(new VerlaufSerie
                    {
                        IdProjekt = s.IdProjekt,
                        Anzeige = s.Anzeige,
                        Kumuliert = d,
                        RestwertBarwert = s.RestwertBarwert - referenz.RestwertBarwert
                    });
                }
            return verlauf;
        }

        /// <summary>
        /// ETAPPE E6 (Konzept § 2.13 (5)) — der Verlauf mit <b>allen drei Szenarien</b>: DREI
        /// vollständige Läufe von <see cref="BerechneVerlauf(BerichtsDaten, WirtschaftlichkeitParameter, int, string, int)"/>
        /// (Ungünstig, Erwartet, Günstig), jeder mit seinem Parametersatz und derselben
        /// Referenz, gesammelt in <see cref="WirtschaftlichkeitVerlaufSzenarien"/>.
        ///
        /// <para><b>Ohne Speichern</b> (Muster <see cref="BerechneBandbreite"/>): Der Verlauf
        /// ist eine Auskunft, kein gebuchter Lauf. <b>Keine eigene Rechnung:</b> Jede Linie
        /// ist Zahl für Zahl die des Einzellaufs — dieselbe Methode, dieselben Eingaben.</para>
        /// </summary>
        /// <param name="jahre">Der Horizont [a]; er darf vom Betrachtungszeitraum abweichen.</param>
        /// <param name="idReferenz">0 = die Gruppenreferenz (in Sicht 2 A), und die
        /// wiederum 0 = Stamm — dieselbe Kette wie in <see cref="BerechneVerlauf(BerichtsDaten, WirtschaftlichkeitParameter, int, string, int)"/>.</param>
        public WirtschaftlichkeitVerlaufSzenarien BerechneVerlaufSzenarien(BerichtsDaten daten,
            WirtschaftlichkeitParameter p, int jahre, int idReferenz = 0)
        {
            var modell = new WirtschaftlichkeitVerlaufSzenarien { Jahre = Math.Max(1, jahre) };
            if (daten == null || daten.Varianten.Count == 0 || p == null) return modell;
            foreach (string szenario in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                modell.Laeufe[szenario] = BerechneVerlauf(daten, p, jahre, szenario, idReferenz);
            return modell;
        }

        /// <summary>
        /// ETAPPE E9a (Schritt B, E9a‑Q4) — der Verlauf mit allen drei Szenarien <b>über den
        /// Betrachtungszeitraum JEDES Szenarios</b>: Ungünstig über T_Worst, Erwartet über T,
        /// Günstig über T_Best (<see cref="WirtschaftlichkeitParameter.FuerSzenario"/>). Jede
        /// Linie endet damit dort, wo ihr Kapitalwert steht, und ihre Gliederung
        /// (<see cref="Zahlungsgliederungen"/>) passt zum gerechneten Ergebnis — „je Szenario
        /// mit eigener Länge".
        ///
        /// <para><b>Ohne gepflegten Zeitraum ist das Zahl für Zahl
        /// <see cref="BerechneVerlaufSzenarien"/> mit <c>p.Betrachtungszeitraum</c></b>.
        /// <see cref="WirtschaftlichkeitVerlaufSzenarien.Jahre"/> ist der längste der drei
        /// Zeiträume; jeder Lauf trägt seinen eigenen. Der Verlaufsdialog mit frei gewähltem
        /// Horizont bleibt bei <see cref="BerechneVerlaufSzenarien"/>.</para>
        /// </summary>
        /// <param name="idReferenz">0 = die Gruppenreferenz (in Sicht 2 A) — dieselbe Kette
        /// wie in <see cref="BerechneVerlauf(BerichtsDaten, WirtschaftlichkeitParameter, int, string, int)"/>.</param>
        public WirtschaftlichkeitVerlaufSzenarien BerechneVerlaufSzenarienJeZeitraum(BerichtsDaten daten,
            WirtschaftlichkeitParameter p, int idReferenz = 0)
        {
            int t = p != null ? p.Betrachtungszeitraum : 1;
            var modell = new WirtschaftlichkeitVerlaufSzenarien { Jahre = Math.Max(1, t) };
            if (daten == null || daten.Varianten.Count == 0 || p == null) return modell;
            foreach (string szenario in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
            {
                int ts = Math.Max(1, p.FuerSzenario(szenario).Betrachtungszeitraum);
                modell.Laeufe[szenario] = BerechneVerlauf(daten, p, ts, szenario, idReferenz);
                if (ts > modell.Jahre) modell.Jahre = ts;
            }
            return modell;
        }

        /// <summary>
        /// ETAPPE E9a — hat ein Szenario einen eigenen Betrachtungszeitraum? Dann brauchen
        /// Gliederung und Mehrjahresreihen den Verlauf
        /// <see cref="BerechneVerlaufSzenarienJeZeitraum"/> statt eines gemeinsamen Horizonts.
        /// </summary>
        public static bool ZeitraumJeSzenario(WirtschaftlichkeitParameter p)
        {
            if (p == null) return false;
            foreach (string szenario in WirtschaftlichkeitVerlaufSzenarien.Reihenfolge)
                if (p.FuerSzenario(szenario).Betrachtungszeitraum != p.Betrachtungszeitraum) return true;
            return false;
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

            /// <summary>
            /// ETAPPE E15 (V‑G7, DIN EN 17463 Anhang F): der Risikoabzug je Periode t ≥ 1 [€/a],
            /// positiv; 0 = keiner. Gesetzt von Lauf und Verlauf über
            /// <see cref="RisikoModul.AbzugFuerStand"/> — die Referenz des Laufs trägt ihn nicht.
            /// </summary>
            public double Risikoabzug;

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

            // AUFTRAG U7 — dieselbe Zahl in zwei Beträgen: § 53/§ 53a (Brennstoff der
            // Stromerzeugung) und § 54 (Heizstoff des produzierenden Gewerbes, nach
            // Sockel). EnergiesteuerJahr1 bleibt ihre Summe — die Erlösreihe und die
            // Ergebnisspalte lesen weiter dort.
            public double Energiesteuer53Jahr1;
            public double Energiesteuer54Jahr1;
            public double Energiesteuer54SockelJahr1;
            public List<EnergiesteuerNachweis> EnergiesteuerNachweise =
                new List<EnergiesteuerNachweis>();

            /// <summary>ETAPPE E7c3 (E7c2‑Q8 b) — die Vorschau je Anlage und Wahl, aus
            /// dem ersten Jahr; reiner Ausweis.</summary>
            public List<EnergiesteuerVorschauZeile> EnergiesteuerVorschau =
                new List<EnergiesteuerVorschauZeile>();

            /// <summary>AUFTRAG 9d — die Begründung je Rubrikposition, aus dem
            /// Steuerrechner des ersten Jahres.</summary>
            public Dictionary<string, string> PositionsGruende =
                new Dictionary<string, string>(StringComparer.Ordinal);

            /// <summary>ETAPPE B6: true = § 9 Abs. 1 Nr. 3 StromStG ist als Erlösreihe
            /// angehängt (Modus <c>ERLOES</c>); false = ausgewiesen, aber nicht im
            /// Kapitalwert (Vorgabe <c>AUSWEIS</c>).</summary>
            public bool StromsteuerBefreiungAlsErloes;
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

            /// <summary>
            /// ETAPPE B7 — die vermiedene Strommenge [MWh/a] (Bedarf ohne Anlage minus
            /// Restbezug). Sie ist die Bemessungsgroesse der § 9b-Korrektur des
            /// Ausweises (Konzept § 2.6, Klarstellung 1) und sonst nirgends im Spiel:
            /// Der Kapitalwert rechnet unveraendert mit den tatsaechlichen
            /// Reststromkosten.
            /// </summary>
            public double VermiedenMengeMWh;

            // ETAPPE E7 — Aufschlüsselungen und Nachweise (reine Ausgabe).
            /// <summary>Anteil des PV-Überschusses am Einspeiseerlös [€/a].</summary>
            public double ErloesPv;
            /// <summary>Anteil der KWK-Einspeisung am Einspeiseerlös [€/a].</summary>
            public double ErloesKwk;
            /// <summary>Nachweis je BHKW-Modul der KWKG-Rechnung (E6 → E7).</summary>
            public List<KwkgModulNachweis> KwkgModule = new List<KwkgModulNachweis>();

            /// <summary>ETAPPE E7c — die Datenlücken der KWKG-Rechnung dieses Laufs, die
            /// die Kohärenzprüfung als Zeile meldet (Anlagenart fehlt, Stromkennzahl
            /// fehlt). <b>Reine Ausgabe.</b></summary>
            public KwkgLuecken KwkgLuecken = new KwkgLuecken();

            /// <summary>ETAPPE P4: Ergebnis des PV-Vergütungsdialogs (null =
            /// Dialog inaktiv — dann gilt exakt der Bestandsrechenweg).</summary>
            public PvErloesErgebnis PvVerguetung;

            /// <summary>KONZEPT § 2.16: Rechnet dieser Stand mit der Vergütung seines
            /// STAMMPROJEKTS? <c>false</c> = eigene Werte (und beim Stamm immer).</summary>
            public bool PvVerguetungUebernommen;

            /// <summary>Name des Stammprojekts, dessen Vergütung übernommen wird; leer
            /// bei eigenen Werten. Er steht im Nachweis, weil eine Id dort niemandem
            /// sagt, WOHER die Zahl kommt.</summary>
            public string PvVerguetungQuelle = "";

            /// <summary>ETAPPE KD6 (§ 11, FK10): Betriebskostenpositionen mit
            /// Startjahr ≥ 2 als (Betrag €/a, Startjahr) — sie laufen in der
            /// Kapitalwertreihe erst ab ihrem Jahr; <see cref="Betrieb"/> trägt
            /// nur noch den Sofort-Anteil.</summary>
            public List<KeyValuePair<double, int>> BetriebAbJahr =
                new List<KeyValuePair<double, int>>();

            /// <summary>ETAPPE E16 (V‑G3, DIN EN 17463 6.3.1): die Betriebspositionen
            /// „alle n Jahre" beider Töpfe (<see cref="BetriebsTopfe.Wiederholt"/>) — sie
            /// zahlen in der Kapitalwertreihe nur in ihren Zahlungsjahren; weder
            /// <see cref="Betrieb"/> noch <see cref="Endenergie"/> tragen sie. Leer = keine.</summary>
            public List<KapitalwertRechner.Wiederholposten> Wiederholt =
                new List<KapitalwertRechner.Wiederholposten>();
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

            /// <summary>ETAPPE E9a (E9a‑Q7): true, wenn das Tarif-Rollenmodell den
            /// Stromanteil und den Einspeiseerlös dieses Laufs tatsächlich ersetzt hat.</summary>
            public bool RollenGerechnet;

            /// <summary>ETAPPE E9a: die Kohärenzzeilen des Szenariolaufs — gepflegte
            /// Szenariowerte, die in diesem Lauf ohne Wirkung bleiben. <b>Reine Ausgabe.</b></summary>
            public List<string> SzenarioHinweise = new List<string>();
        }

        /// <summary>
        /// ETAPPE E9a (vollständige Szenarioabdeckung V‑E) — <b>die Mengen- und Preisbasis
        /// einer Variante in einem Szenario</b>, gebildet an EINER Stelle:
        /// <list type="bullet">
        ///   <item><description>der <b>Mengenfaktor</b> des Satzes (Schritt B, E9a‑Q2) skaliert
        ///     das Mengengerüst des Simulationsergebnisses (<see cref="SzenarioMengen"/>),
        ///     BEVOR es Preise, Sätze oder Kontingente trifft;</description></item>
        ///   <item><description>die <b>Trägerpreise</b> des Szenarios (Schritt C, E9a‑Q3)
        ///     bepreisen die Mengen: Die Energiekosten der Kopie rechnet
        ///     <see cref="KostenEmissionRechner.Berechne(VariantenDaten, string)"/> neu —
        ///     Arbeits-, Grund- und Leistungspreis je Träger ersetzt, wo gepflegt.</description></item>
        /// </list>
        /// <para><b>Für ERWARTET und ohne jede Pflege kommt <paramref name="v"/> selbst
        /// zurück</b> — dieselbe Referenz, also Zahl für Zahl der Lauf von vor E9a. Das
        /// Original wird nie verändert: Seite, Bericht und Kennzahlen lesen weiter die
        /// Erwartet-Zahlen der Variante.</para>
        /// </summary>
        private static VariantenDaten Szenariodaten(VariantenDaten v, WirtschaftlichkeitParameter ps,
                                                    string szenario)
        {
            if (v == null || v.Fehler != null || v.Ergebnis == null || ps == null) return v;
            SzenarioSatz satz = ps.SatzFuer(szenario);
            if (satz == null) return v;                                   // ERWARTET

            double faktor = satz.MengeFaktor;                             // genau 1,0 ohne Pflege
            bool preise = EnergietraegerPreisCtrl.SzenarioGepflegt(v.IdProjekt, szenario);
            if (faktor == 1.0 && !preise) return v;                       // nichts gepflegt

            VariantenDaten k = faktor == 1.0 ? v.Kopie() : SzenarioMengen.Variante(v, faktor);
            KostenEmissionRechner.Berechne(k, szenario);
            return k;
        }

        private ProjektEingabe BaueEingabe(VariantenDaten v, WirtschaftlichkeitParameter p,
                                           TarifParameter tarif, string szenario)
        {
            var e = new ProjektEingabe();
            if (v.Fehler != null || v.Ergebnis == null) return e;

            // ETAPPE K5: Zuschusszeilen kommen aus derselben Abfrage, gehen aber nicht in
            // die Positionsliste — sie mindern I₀ einmalig (Konzept § 7.4).
            // ETAPPE W5-B-9: der Szenario-Parametersatz. Fuer ERWARTET ist er null, und
            // dann geht jeder Zweig darunter den Weg von vor dieser Etappe. p ist hier
            // bereits der SZENARIOsatz (Berechne ruft FuerSzenario) - seine Zins- und
            // Preisfelder tragen die wirksamen Werte; der Satz wird ausschliesslich
            // fuer Investition, Nutzungsdauer und Ertraege gelesen, nie noch einmal
            // fuer den Zins.
            SzenarioSatz satz = p.SatzFuer(szenario);

            double zuschuss;
            e.Investitionen = LiesInvestitionen(v.IdProjekt, szenario, satz, out zuschuss);
            e.Zuschuss = zuschuss;
            // PAKET FX3 (R-2): zwei Töpfe statt einem — der Endenergie-Anteil wächst in
            // der Jahresreihe mit p_E (Begründung an BetriebsTopfe).
            // ETAPPE W5-B-11 (G11, 09.09.2026): Der Satz geht mit - die Zeilen
            // "x % der Investitionssumme" bemessen sich damit an der Investition DIESES
            // Szenarios statt immer an der des Erwartungsfalls.
            BetriebsTopfe topfe = LiesBetriebskostenTopfe(v.IdProjekt, szenario, satz);
            if (topfe.Fehler != null) Stufenfehler(v.IdProjekt, STUFE_BETRIEBSKOSTEN, topfe.Fehler);   // E7c3 (B‑6)
            e.Betrieb = topfe.BetriebSofort;
            e.BetriebAbJahr = topfe.BetriebAbJahr;
            e.Endenergie = topfe.EndenergieSofort;
            e.EndenergieAbJahr = topfe.EndenergieAbJahr;
            // PAKET FX5-a: der investgekoppelte Ausweis wandert mit — er ändert an den
            // Beträgen nichts (er ist Teilmenge von Betrieb/BetriebAbJahr) und wird nur
            // in der Sensitivität gelesen.
            e.InvestGekoppelt = topfe.InvestGekoppeltSofort;
            e.InvestGekoppeltAbJahr = topfe.InvestGekoppeltAbJahr;
            // ETAPPE E16 (V‑G3): die Positionen „alle n Jahre" — eigene Liste, leer im Bestand.
            e.Wiederholt = topfe.Wiederholt;
            List<KeyValuePair<double, int>> betriebAbJahr = topfe.BetriebAbJahr;

            // ETAPPE KD6 (§ 11, FK10): Sind Startjahre gesetzt, laufen Investition
            // (samt Ersatz/Restwert) und Betriebskosten der Position erst ab ihrem
            // Jahr. Die ENERGIEKOSTEN bleiben die Gesamtrechnung des Simulationslaufs
            // — die Simulation kennt keine Startjahre je Komponente; der Hinweis
            // macht die dokumentierte Vereinfachung sichtbar statt still.
            bool startjahre = betriebAbJahr.Count > 0 || e.EndenergieAbJahr.Count > 0;
            // ETAPPE E16: Eine Position „alle n Jahre" mit Startjahr ≥ 2 ist ebenso eine
            // Startjahr-Position — derselbe Hinweis auf die Energiekosten gilt für sie.
            if (!startjahre)
                foreach (KapitalwertRechner.Wiederholposten w in e.Wiederholt)
                    if (w.StartJahr > 1) { startjahre = true; break; }
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
            e.Betriebskosten = LiesBetriebskostenPositionen(v.IdProjekt, szenario, satz);
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

            // ANWENDERENTSCHEIDE 22.09.2026 — STROMBEDARF OHNE VERWENDUNG. Die Regel
            // (ProjektEnergietraegerCtrl.StromOhneVerwendung) hat der
            // KostenEmissionRechner EINMAL gestellt; ihr Ergebnis steht an der Variante.
            // Der Netzbezug ist dann in den Flat-Kosten nicht bepreist, und kein Tarif
            // darf ihn nachträglich bepreisen: kein Tarifersatz und keine Tarifmeldung
            // („Flat-Energiekosten unvollständig" wäre falsch — sie sind vollständig).
            // Einzuspeisen gibt es ohne stromverwendenden Erzeuger nichts.
            bool stromOhneVerwendung = v.StrombedarfOhneVerwendungMWh.HasValue;

            // ---------------- Tarif-Rollenmodell (ETAPPE E5) ----------------
            bool rollen = !stromOhneVerwendung && tarif != null && tarif.Wirksam;
            if (rollen) RechneRollentarif(v, tarif, e);

            // ---------------- Kein Zeitzonentarif (Q11, ETAPPE E7b) ----------------
            //
            // Bis E7b ersetzte ein aktiver Tarifsatz im ZONENmodell die Flat-Stromkosten
            // durch vier Zonenpreise Winter/Sommer × HT/NT samt einer zweistufigen
            // Leistungspreis-Staffel auf die höchste Stundenlast, und den Einspeiseerlös
            // durch vier Zonen-Einspeisepreise. Entscheid Q11 (Anwender 22.09.2026: „kein
            // HT/NT"): Den Zeitzonentarif gibt es nicht mehr. Der Netzbezug bleibt mit
            // den Preisen des Stromträgers aus der Kostenverwaltung bepreist — die Staffel
            // steht seither dort (KostenEmissionRechner, Viertelstundenspitze) —, der
            // Einspeiseerlös mit den Vergütungssätzen der Parameter.
            //
            // KEIN STILLER RÜCKFALL: Ein Tarifsatz, der noch aktiv auf dem Zonenmodell
            // steht (eine Datenbank vor Schemaschritt 104, der ihn löscht), rechnet
            // nicht mehr — und das steht als Hinweis am Ergebnis. Der Hinweis ist der
            // Wächter für einen nicht migrierten Stand.
            if (!stromOhneVerwendung && tarif != null && tarif.Aktiv && !rollen)
                Melde(e, T("WIRT_HINWEIS_ZEITZONENTARIF",
                    "Zeitzonentarif (HT/NT) entfällt: Der Tarifsatz des Projekts steht noch " +
                    "auf dem Zonenmodell und wird nicht mehr gerechnet — der Strom ist mit den " +
                    "Preisen des Stromträgers aus der Kostenverwaltung bepreist."));

            // ---------------- PV-Vergütung (PV-Konzept, ETAPPE P4) ----------------
            // Ist der Vergütungsdialog AKTIV, ersetzt seine jahresscharfe Reihe
            // (ErloesReihe.PV_VERGUETUNG) die PV-Bewertung des gerade aktiven Pfades
            // (Flat/Rollen) — EINE Vergütungswahrheit (Befund V4, F7). Der Platz
            // NACH beiden Pfaden ist Absicht: Jeder von ihnen führt e.ErloesPv,
            // also wird genau dieser Anteil aus dem konstanten Erlös herausgelöst.
            // Inaktiv (Aktiv = false) ändert sich NICHTS — Abnahmekriterium P4.
            // ETAPPE E9a (Schritt D): mit DV-Entgelt und PPA-Preis DIESES Szenarios.
            RechnePvVerguetung(v, p, e, szenario);

            // ETAPPE E9a (E9a‑Q7, Empfehlung) — gepflegte Szenariowerte, die dieser Lauf nicht
            // lesen kann, werden BENANNT statt still verschluckt. Das Rollenmodell ersetzt den
            // Stromanteil samt Leistungspreis durch den Reststromtarif und den Einspeiseerlös
            // durch den Einspeisetarif (RechneRollentarif) — seine Rollenpreise bleiben in
            // allen Szenarien die Erwartet-Preise. Rechnet der Vergütungsdialog die PV-Reihe,
            // trägt die flache Einspeisevergütung den PV-Anteil nicht mehr.
            if (satz != null)
            {
                bool verguetungGepflegt = satz.Einspeiseverguetung.HasValue;
                if (e.RollenGerechnet)
                {
                    if (v.SzenarioStrompreisGepflegt)
                        e.SzenarioHinweise.Add(T("WIRT_SZ_ROLLEN_STROMPREIS",
                            "Szenario-Strompreis ohne Wirkung: Das Tarif-Rollenmodell ist aktiv — " +
                            "Bezug, Reststrom und Einspeisung rechnen in allen Szenarien mit den " +
                            "Preisen des Tarifsatzes."));
                    if (verguetungGepflegt || satz.EinspeiseverguetungKwk.HasValue)
                        e.SzenarioHinweise.Add(T("WIRT_SZ_ROLLEN_EINSPEISUNG",
                            "Szenario-Einspeisevergütung ohne Wirkung: Das Tarif-Rollenmodell " +
                            "bewertet die Einspeisung mit dem Einspeisetarif des Tarifsatzes."));
                }
                else if (verguetungGepflegt && e.PvVerguetung != null)
                    e.SzenarioHinweise.Add(T("WIRT_SZ_PV_DIALOG_EINSPEISUNG",
                        "Szenario-Einspeisevergütung (PV) ohne Wirkung: Die PV-Vergütung rechnet " +
                        "der Vergütungsdialog — dort gelten DV-Entgelt und PPA-Preis je Szenario."));
            }

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
                : BaueKwkgReihe(v, p, e.Matrix, e.KwkgModule, e.KwkgLuecken, out kwkgJahr1, out kwkgHinweis);
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

            // SP-E-2: Hier stand bis zum Anwenderentscheid vom 17.09.2026 der
            // Aufschlagssummand auf die Energiekosten. Er ist entfallen — die
            // Preisanteile ZERLEGEN den Arbeitspreis des Stromträgers, sie kommen nicht
            // mehr auf ihn. Was an Netzentgelt, Steuern, Abgaben und Umlagen im Preis
            // steckt, steckt damit schon in `e.Energie`; ein zweiter Summand hier wäre
            // genau die Doppelzählung, die der E5-Restpunkt benannt hat.

            // ETAPPE W5-B-9: die Ertragsaenderung des Szenarios - GANZ ZUM SCHLUSS, wenn
            // beide Erloespfade (Flat, Rollenmodell) und der
            // PV-Verguetungsdialog ihre Zahlen gesetzt haben.
            SkaliereErtraege(e, satz);
            return e;
        }

        /// <summary>
        /// ETAPPE W5‑B‑9 (Anwenderentscheid 09.09.2026): die Ertragsänderung des Szenarios
        /// auf die ERLÖSE anwenden.
        ///
        /// <para><b>Was skaliert wird:</b> der konstante Einspeiseerlös (samt seiner
        /// beiden Ausweise PV und KWK) und die jahresscharfe PV-Vergütungsreihe. Beides
        /// hängt an der ERZEUGUNG der Anlage, und genau deren Unsicherheit meint eine
        /// Ertragsbandbreite.</para>
        ///
        /// <para><b>Was NICHT skaliert wird:</b> die gesetzlichen Erlösreihen —
        /// KWK-Zuschlag, Energiesteuer-Entlastung, Stromsteuer-Befreiung und
        /// -Entlastung. Sie hängen an Sätzen, Kontingenten und Schwellen des
        /// Gesetzeskatalogs, nicht an einer Ertragserwartung; ein pauschaler Zehnprozenter
        /// darauf wäre keine Bandbreite, sondern eine falsche Zahl. Für das
        /// Regulierungsrisiko gibt es die eigene Sensitivitätszeile „KWKG-Bonus
        /// entfällt“. Ebenso wenig skaliert werden die ENERGIEKOSTEN — sie sind die
        /// Ausgabenseite und haben mit p_E ihren eigenen Hebel.</para>
        ///
        /// <para><b>Ohne Satz (ERWARTET) und bei Faktor 1,0 wird nichts angefasst</b> —
        /// derselbe IEEE-754-Grund wie bei den Sensitivitätsfaktoren in
        /// <see cref="RechneBild"/>.</para>
        /// </summary>
        private static void SkaliereErtraege(ProjektEingabe e, SzenarioSatz satz)
        {
            if (e == null || satz == null) return;
            double f = satz.ErtragFaktor;
            if (f == 1.0) return;

            e.Erloes *= f;
            e.ErloesPv *= f;
            e.ErloesKwk *= f;

            for (int i = 0; i < e.ErloesReihen.Count; i++)
            {
                KapitalwertRechner.ErloesReihe r = e.ErloesReihen[i];
                if (r == null || r.JeJahr == null ||
                    !string.Equals(r.Name, KapitalwertRechner.ErloesReihe.PV_VERGUETUNG,
                                   StringComparison.Ordinal)) continue;
                var werte = new double[r.JeJahr.Length];
                for (int t = 0; t < werte.Length; t++) werte[t] = r.JeJahr[t] * f;
                e.ErloesReihen[i] = new KapitalwertRechner.ErloesReihe(r.Name, werte);
            }
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
        /// Erlös bucht, zählt sie doppelt. Deshalb ersetzt hier der Tarifbetrag den
        /// Flat-Netzanteil der Energiekosten (samt Leistungsanteil), und die drei
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

            // KU2 WELLE 3 (Entscheid E34, Kühlkonzept 6.1): Die Reststrommenge der Matrix ist der
            // ganze Netzbezug des Anschlusses - samt dem Anteil, den ein abweichender Kühlträger
            // trägt. Dieser Anteil steht mit dem Arbeitspreis des Kühlträgers schon in den
            // Energiekosten (KostenEmissionRechner, außerhalb von StromkostenNetz); der Tarif bepreist
            // ihn deshalb nicht ein zweites Mal. Leistungs- und Grundpreis bleiben beim Tarif des
            // Anschlusses. Ohne abweichenden Kühlträger ist der Abzug 0.
            double kuehlAbzug = v.NetzbezugKuehltraegerMWh > 0
                ? v.NetzbezugKuehltraegerMWh * 1000.0 * tarif.Reststrom.ArbeitspreisEurKWh : 0.0;
            if (kuehlAbzug != 0.0)
                Melde(e, string.Format(BerichtTexte.Kultur, T("WIRT_TARIF_KUEHLTRAEGER",
                    "Rollentarif: {0} MWh/a Netzbezug des Kältestroms tragen den Stromträger der Kühlung " +
                    "und stehen nicht im Reststromtarif ({1} €/a)."),
                    v.NetzbezugKuehltraegerMWh.ToString("N2", BerichtTexte.Kultur),
                    kuehlAbzug.ToString("N2", BerichtTexte.Kultur)));

            e.StromkostenTarif = r.Reststrom.SummeEur - kuehlAbzug;
            e.Energie = v.Energiekosten.Value - v.StromkostenNetz.Value + r.Reststrom.SummeEur - kuehlAbzug;
            e.Erloes = r.EinspeiseerloesEur;   // ersetzt PV-/KWK-Bewertung über die Parameter
            e.RollenGerechnet = true;          // E9a (E9a‑Q7): Anlass der Kohärenzzeilen

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
                e.VermiedenMengeMWh = r.VermiedenMengeMWh;   // B7
                foreach (string h in r.Herleitung) Melde(e, h);
            }
        }

        /// <summary>
        /// ETAPPE P4 (PV-Konzept § 4.4/§ 4.6): rechnet bei AKTIVEM Vergütungsdialog
        /// die jahresscharfe PV-Erlösreihe und ersetzt damit den PV-Anteil des
        /// konstanten Einspeiseerlöses. Stufe 2 (gemessener § 51-Ausfall, Spoterlös,
        /// Kappung) greift von selbst, wenn Stundenreihen des Laufs und eine
        /// Spot-Preisreihe des Projekts vorliegen — sonst rechnet Stufe 1 mit der
        /// Ausfall-Pauschale. Fehler kippen den Lauf nicht (Hinweis statt Absturz).
        ///
        /// <para><b>KONZEPT § 2.16 — gelesen wird die AUFGELÖSTE Zeile.</b> Bis dahin
        /// stand hier <c>Lies(v.IdProjekt)</c>, also die Zeile dieses einen Stands: Eine
        /// Variante bekam die Dialogangaben genau dann, wenn sie eine eigene Zeile hatte
        /// — und die hatte sie genau dann, wenn sie NACH der Pflege des Stamms angelegt
        /// wurde. <see cref="ProjektPhotovoltaikCtrl.LiesAufgeloest"/> macht daraus eine
        /// Wahl: eigene Zeile, sonst die des Stamms. Der Stamm rechnet wie bisher, und
        /// eine Gruppe ohne gepflegte Zeile ebenso (Flat-Pfad). Der flache Rahmensatz
        /// <c>Einspeiseverguetung</c> bleibt je Gruppe (VV‑Q5).</para>
        /// </summary>
        private void RechnePvVerguetung(VariantenDaten v, WirtschaftlichkeitParameter p,
                                        ProjektEingabe e, string szenario)
        {
            try
            {
                ProjektPhotovoltaikCtrl pvc = new ProjektPhotovoltaikCtrl();
                PvVerguetungStand stand = pvc.LiesAufgeloest(v.IdProjekt);
                ProjektPhotovoltaikModel pv = stand.Modell;
                if (pv == null || !pv.Aktiv) return;

                // ETAPPE E9a (Schritt D): die WIRKSAME Zeile des Szenarios — DV-Entgelt und
                // PPA-Festpreis durch ihren gepflegten Szenariowert ersetzt, sonst dieselbe
                // Referenz (ProjektPhotovoltaikCtrl.FuerSzenario, die EINE Stelle).
                pv = ProjektPhotovoltaikCtrl.FuerSzenario(pv, szenario);

                e.PvVerguetungUebernommen = stand.Uebernommen;
                e.PvVerguetungQuelle = stand.Uebernommen
                    ? StartseiteCtrl.Projektname(stand.IdQuelle) ?? "" : "";

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
                catch (Exception)
                {
                    // ETAPPE E7c3 (B‑6): benannter Rückfall — ohne Spotreihe rechnet die
                    // Vergütung in Stufe 1 (Ausfallpauschale); die Herleitung des Rechners
                    // nennt die Stufe, in der er gerechnet hat.
                }

                // ETAPPE E2.4 (Paket B des PV-Ertragsmodells): Eigenverbrauch und
                // Arbeitspreis des Basisjahres - beide NUR fuer den
                // degradationsbedingten Mehrbezug. Ohne gepflegte Degradation bleiben
                // sie wirkungslos (der Rechner prueft d > 0 zuerst), die Reihe ist dann
                // bitgleich zum P6-Stand. Dieselben zwei Groessen wie im Ausweis
                // PvVermiedenerBezugAusweis - eine Wahrheit, zwei Verwendungen.
                double evKwh = 0, strompreis = 0;
                if (v.Ergebnis != null && v.Ergebnis.Photovoltaik != null)
                    evKwh = Math.Max(0, (v.Ergebnis.Photovoltaik.Stromproduktion
                                       - v.Ergebnis.Photovoltaik.Ueberschuss) * 1000.0);
                // ETAPPE E9a (Schritt C): der Arbeitspreis des Szenarios — derselbe, mit dem
                // die Energiekosten dieses Szenarios den Mehrbezug bepreisen.
                double? arbeitspreis = StromArbeitspreisEurJeKwh(v.IdProjekt, szenario);
                if (arbeitspreis.HasValue) strompreis = arbeitspreis.Value;

                GesetzKatalog katalog = new GesetzKatalog();
                PvErloesErgebnis pe = PvErloesRechner.Rechne(pv, kwp, einspMWh,
                    einspStunden, spot, p.Betrachtungszeitraum, katalog.Wert,
                    jahr => pvc.Jahresmarktwert(jahr, pv), BerichtTexte.Kultur,
                    evKwh, strompreis);

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
        /// 31.12.2026, Inbetriebnahme bis zum Fristende des Katalogs), Ausschreibungsgrenze <b>je Anlage</b>
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
        /// <para><b>ETAPPE BK1a: auch der Weg ohne zuordenbare Anlagenzeilen rechnet aus
        /// den ANLAGEN</b> (<see cref="ReiheErsatzGewichtet"/>) — als eine
        /// leistungsgewichtete virtuelle Gesamtanlage. Damit hat der Zuschlag nur noch
        /// EINE Quelle; die elf KWKG-Spalten von <c>Tab_ProjektWirtschaftlichkeit</c>
        /// rechnen nirgends mehr mit.</para>
        /// </summary>
        /// <param name="nachweise">ETAPPE E7: Wird je gerechnetem Modul um eine Zeile
        /// ergänzt (Satz, Vbh, Deckel, Kontingent, Herleitung nach § 7). Nur der Weg je
        /// Anlage füllt sie — der projektweite Ersatzweg kennt keine Module. <c>null</c>
        /// ist erlaubt.</param>
        /// <param name="luecken">ETAPPE E7c: nimmt die Anlagen auf, deren Zuschlag an einer
        /// Datenlücke 0 wurde (Anlagenart fehlt, Stromkennzahl fehlt) — die Grundlage der
        /// Kohärenzzeilen. <c>null</c> ist erlaubt.</param>
        private double[] BaueKwkgReihe(VariantenDaten v, WirtschaftlichkeitParameter p,
                                       StromMatrix matrix, List<KwkgModulNachweis> nachweise,
                                       KwkgLuecken luecken,
                                       out double jahr1, out string hinweis)
        {
            jahr1 = 0;
            hinweis = null;
            var hinweise = new List<string>();   // Meldungen kombinieren, nie überschreiben

            // ETAPPE BK1a — DIE PRÜFUNG DES EIGENSTROM-TATBESTANDS STEHT JE ANLAGE.
            // Bis hierher prüfte K6 den PROJEKTsatz an dieser Stelle. Beide Rechenwege
            // holen Satz und Tatbestand jetzt aus der Anlage
            // (<see cref="SatzEigenDerAnlage"/>) — der Regelweg seit BK1, der Ersatzweg
            // seit BK1a. Eine zweite Prüfung am Projekt hätte nur noch Spalten gelesen,
            // aus denen niemand mehr rechnet.

            // ETAPPE BK1 — DER AKTIVIERUNGSSCHALTER FRAGT DIE ANLAGEN, nicht mehr das
            // Projekt. Gerechnet wird, was an der Anlage steht (Rückfall aufgegeben);
            // ein Schalter, der weiter die Projektvorgabe liest, spräche von etwas
            // anderem als die Rechnung darunter. Die Regel steht EINMAL in
            // KwkgAktivierung und wird an allen sechs Stellen von dort geholt.
            bool aktiv = false;
            foreach (BhkwAnlage ak in BhkwAnlagen(v.IdProjekt))
                if (KwkgAktivierung.SatzGefuehrt(ak.SatzEigenCt, ak.SatzEinspCt)) { aktiv = true; break; }
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
                if (p.KwkgStichtag.HasValue && p.KwkgStichtag.Value.Date > KWKG_STICHTAG_ENDE)
                {
                    hinweis = "KWKG: Bestellung/Genehmigung nach dem 31.12.2026 — nach geltendem " +
                              "Recht nicht förderfähig (Regulierungsrisiko Novelle); Bonus = 0.";
                    return null;
                }

                // ETAPPE E7c (A20, Entscheid E7-Q3 Lesart b): Die Frist zur
                // Inbetriebnahme endet am Katalogdatum (31.12.2030) — nicht mehr vier
                // Jahre nach dem Stichtag. Geprüft wird die Inbetriebnahme, sobald sie
                // bekannt ist, auch ohne Stichtag; ohne Katalogwert keine stille Vorgabe,
                // sondern eine Herleitungszeile („ungeprüft").
                if (p.KwkgInbetriebnahme.HasValue)
                {
                    GesetzParameter quelle;
                    DateTime? fristende = FristendeInbetriebnahme(p.KwkgInbetriebnahme.Value.Year, out quelle);
                    if (!fristende.HasValue)
                        hinweise.Add(FristendeFehltZeile(null, p.KwkgInbetriebnahme.Value.Year));
                    else if (p.KwkgInbetriebnahme.Value.Date > fristende.Value)
                    {
                        hinweis = string.Format(BerichtTexte.Kultur,
                            T("WIRT_KWKG_NACH_FRISTENDE",
                              "KWKG: Inbetriebnahme am {0} nach dem Ende der Frist zur " +
                              "Inbetriebnahme am {1} ({2}) — kein Zuschlag."),
                            p.KwkgInbetriebnahme.Value.ToString("dd.MM.yyyy", BerichtTexte.Kultur),
                            fristende.Value.ToString("dd.MM.yyyy", BerichtTexte.Kultur),
                            Herkunft(quelle));
                        return null;
                    }
                }

                if (!p.KwkgStichtag.HasValue)
                    hinweise.Add("KWKG: kein Bestell-/Genehmigungsdatum hinterlegt — " +
                                 "Förderfähigkeit ungeprüft (§ 6 KWKG 2025, Stichtag 31.12.2026).");
            }

            int foerderbeginn = Foerderbeginn(p);

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
            // Frist zur Inbetriebnahme je Anlage (seit E7c das Katalogdatum), und die
            // Ausschreibungsgrenze wird mit dem Inbetriebnahmejahr DIESER Anlage im
            // Katalog nachgeschlagen.
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

                if (!_oelCache.ContainsKey(v.IdProjekt))
                {
                    _oelCache[v.IdProjekt] = BhkwMitHeizoel(v.IdProjekt, out string oelFehler);
                    if (oelFehler != null) Stufenfehler(v.IdProjekt, STUFE_HEIZOEL, oelFehler);   // E7c3 (B‑6)
                }
                bool oelGeraetezeile = _oelCache[v.IdProjekt];
                if (oelAusschluss && oelGeraetezeile)
                {
                    hinweise.Add(MyResource.Resource.WIRT_KWKG_HEIZOEL_JE_ANLAGE_UNKLAR);
                    hinweis = string.Join(" | ", hinweise);
                    return null;
                }
                if (!p.KwkgInbetriebnahme.HasValue && oelGeraetezeile)
                    hinweise.Add(MyResource.Resource.WIRT_KWKG_HEIZOEL_OHNE_IBN_UNKLAR);

                // ERSATZWEG: eine leistungsgewichtete virtuelle Gesamtanlage
                // (Etappe BK1a) — die Rechenform des Stands vor E6, aber mit
                // Eingangsgrößen aus den ANLAGEN statt aus dem Projekt.
                //
                // ETAPPE B3 Paket b: Er zieht dieselbe Minderung wie der Weg je Anlage —
                // in der Sache greift sie hier allerdings nie, weil die fehlgeschlagene
                // Zuordnung, die diesen Zweig überhaupt auslöst, DIESELBE ist, an der
                // auch HilfsstromDesProjekts scheitert (GesamtMWh = 0). Die Netto-Größen
                // stehen trotzdem in der Signatur: Der Ersatzweg soll nicht der eine
                // Pfad sein, der beim nächsten Ausbau brutto rechnet.
                double stromNettoMWh = Math.Max(0, stromMWh - hilfsstrom.GesamtMWh);
                double[] ersatz = ReiheErsatzGewichtet(v, p, mitMatrix, eigenNettoMWh, einspNettoMWh,
                                                       stromNettoMWh, stromMWh, vbh, foerderbeginn,
                                                       hinweise, luecken, out jahr1);
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
            foreach (string s in auswahl.Fristhinweise) hinweise.Add(s);   // E7c (A20)

            // Öl-Anlagen ohne Inbetriebnahmedatum werden NICHT ausgeschlossen (der
            // Ausschluss gilt nur für Neuanlagen) — der Anwender muss aber wissen, dass
            // das Ergebnis am fehlenden Datum hängt.
            if (auswahl.OelOhneIbn.Count > 0)
                hinweise.Add(string.Format(MyResource.Resource.WIRT_KWKG_HEIZOEL_OHNE_IBN,
                                           auswahl.Klartext(auswahl.OelOhneIbn)));

            double[] reihe = ReiheJeAnlage(v, p, mitMatrix, eigenNettoMWh, einspNettoMWh,
                                           hilfsstrom, auswahl, foerderbeginn, hinweise,
                                           nachweise, luecken, out jahr1);
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
            foreach (string s in auswahl.Fristhinweise) hinweise.Add(s);   // E7c (A20)
        }

        /// <summary>
        /// <b>ETAPPE BK1a — der ERSATZWEG als leistungsgewichtete virtuelle
        /// Gesamtanlage.</b> Er greift, wenn sich Anlagen- und Ergebnismodulzeilen nicht
        /// paaren lassen (<see cref="KwkgAnlagenauswahl.Bestimmbar"/> = false): keine
        /// Modulzeilen, oder Namen und Anzahl passen nicht zusammen. Dann fehlt die
        /// Zuordnung <b>Menge → Anlage</b>, nicht aber die Anlage selbst.
        ///
        /// <para><b>Warum er nicht mehr projektweit rechnet.</b> Bis BK1a war er der
        /// Rechenweg vor E6 und las seine vier Eingangsgrößen aus
        /// <c>Tab_ProjektWirtschaftlichkeit</c>. Seit BK1 gehört der Zuschlag der Anlage
        /// (§ 7 KWKG bemisst den Satz an IHRER Leistung, § 8 das Kontingent an IHRER
        /// Anlagenart); die Projektspalten waren damit eine zweite Wahrheit, die nur noch
        /// dieser eine Zweig las — und mit Schemaschritt 90 fallen sie ganz. Der Zweig
        /// bildet deshalb aus den Anlagen EINE virtuelle Gesamtanlage: Jede der vier
        /// Größen ist das mit der elektrischen Nennleistung gewichtete Mittel über die
        /// Anlagen, <c>g_i = P_el,i</c>, <c>G = Σ g_i</c>.</para>
        ///
        /// <para><b>Die Rechenform bleibt.</b> <c>bonusVoll</c>, der Negativpreis-
        /// Abschlag, der Fallback ohne Stundenreihen (W2), die projektweiten
        /// Vollbenutzungsstunden aus <see cref="VbhElektrisch"/> und die Jahresschleife
        /// mit <c>rest</c> und <c>verguetet</c> sind Zeile für Zeile die von vorher — nur
        /// die vier Eingangsgrößen wechseln die Herkunft. Bei EINER Anlage, die ihre
        /// Vorgaben aus Schritt 89 trägt, ist das Ergebnis deshalb dieselbe Zahl wie vor
        /// BK1a; bei mehreren gleichen Anlagen ebenso, weil das gewichtete Mittel
        /// gleicher Werte dieser Wert ist.</para>
        ///
        /// <para><b>Der Jahresdeckel wird JE JAHR gemischt.</b> Anlagen mit verschiedenem
        /// Förderbeginn stehen in demselben Kalenderjahr auf verschiedenen Stufen der
        /// Staffel des § 8 Abs. 4; ein einmal gebildeter Mittelwert würde diesen Verlauf
        /// einebnen. Der gewichtete Mittelwert entsteht deshalb in der Jahresschleife
        /// neu, aus dem festen Deckel der Anlage oder aus IHRER Staffelstufe im Jahr
        /// <c>beginn_i + t − 1</c>.</para>
        ///
        /// <para><b><c>G ≤ 0</c> — keine Anlage trägt eine elektrische Nennleistung.</b>
        /// Dann gäbe eine Gewichtung mit 0 eine stille 0 für Satz, Kontingent und Deckel
        /// und damit einen Zuschlag von 0 ohne Grund. Stattdessen wird arithmetisch
        /// gemittelt und der Ersatz benannt (<c>WIRT_KWKG_ERSATZ_OHNE_LEISTUNG</c>).</para>
        ///
        /// <para><b>Die Hinweise werden LOKAL entdoppelt.</b> Satz- und Kontingentprüfung
        /// laufen je Anlage und erzeugen bei N gleichartigen Anlagen N wortgleiche
        /// Zeilen. Entdoppelt wird deshalb ordinal innerhalb dieses Weges — nicht gegen
        /// die schon gesammelten Meldungen des Aufrufers, die aus anderen Prüfungen
        /// stammen und zufällig gleich lauten könnten.</para>
        ///
        /// <para><b>ETAPPE E7c — Fall 2 auf dem Ersatzweg</b> (Entscheid E7‑Q2 (4)). Trägt
        /// eine Anlage das Kennzeichen „Vorrichtung zur Abwärmeabfuhr", fehlt hier die
        /// Zuordnung Modul → Anlage auch für die Wärme. Die Nutzwärme des PROJEKTS
        /// (Wärmeproduktion − Wärmeüberschuss) und die Nettostromerzeugung werden deshalb
        /// mit denselben Gewichten verteilt, die die Gesamtanlage mischen (P_el, bei G ≤ 0
        /// zu gleichen Teilen); je gekennzeichneter Anlage gilt
        /// <c>min(Netto_i, Nutzwärme_i × σ_i)</c>, und die Summe der Kürzungen geht zuerst
        /// von der Einspeisung ab (<see cref="KwkStromRechner.Kuerzen"/>). Die
        /// Herleitungszeile sagt „Ersatzweg".</para>
        /// </summary>
        /// <param name="mitMatrix">true = die Stundenreihen liefern einen Eigen-/
        /// Einspeise-Split; false = Fallback „alles ist Eigenverbrauch" (W2).</param>
        /// <param name="eigenNettoMWh">ETAPPE B3 Paket b: KWK-Eigenverbrauch des
        /// Projekts NACH Abzug des Hilfsstroms [MWh/a].</param>
        /// <param name="einspNettoMWh">Ebenso die KWK-Einspeisung [MWh/a].</param>
        /// <param name="stromNettoMWh">Ebenso die Gesamterzeugung [MWh/a] — die
        /// Bezugsgröße des Fallbacks ohne Stundenreihen.</param>
        /// <param name="stromBruttoMWh">ETAPPE E7c3: die Stromerzeugung des Projekts an den
        /// Klemmen [MWh/a] — die erzeugte Arbeit der Vbh-Definition; nur für die
        /// Herleitungszeile des Falls 2, gerechnet wird mit <paramref name="vbh"/>.</param>
        /// <param name="foerderbeginn">Förderbeginn des PROJEKTS; er gilt für jede
        /// Anlage ohne eigenes Inbetriebnahmedatum.</param>
        /// <param name="hinweise">Die Meldungsliste des Aufrufers — dieser Weg hängt
        /// seine entdoppelten Meldungen hinten an.</param>
        /// <param name="luecken">ETAPPE E7c: Anlagen, deren Zuschlag an einer Datenlücke 0
        /// wurde; <c>null</c> ist erlaubt.</param>
        private double[] ReiheErsatzGewichtet(VariantenDaten v, WirtschaftlichkeitParameter p,
                                              bool mitMatrix, double eigenNettoMWh,
                                              double einspNettoMWh, double stromNettoMWh,
                                              double stromBruttoMWh,
                                              double vbh, int foerderbeginn,
                                              List<string> hinweise, KwkgLuecken luecken,
                                              out double jahr1)
        {
            jahr1 = 0;
            List<BhkwAnlage> anlagen = BhkwAnlagen(v.IdProjekt);
            if (anlagen.Count == 0) return null;

            if (_staffelCache == null) _staffelCache = LadeKwkgStaffel();
            List<KeyValuePair<int, double>> staffel = _staffelCache;

            // Die Meldungen DIESES Weges, getrennt gesammelt (Begründung im Kopf).
            var eigene = new List<string>();

            double pelSumme = 0;
            foreach (BhkwAnlage a in anlagen) pelSumme += a.PelKW;

            bool nachLeistung = pelSumme > 0;
            double gewichtSumme = nachLeistung ? pelSumme : anlagen.Count;

            var gewicht = new double[anlagen.Count];
            var beginnJeAnlage = new int[anlagen.Count];
            var deckelFest = new double[anlagen.Count];

            double satzEigen = 0, satzEinsp = 0, kontingent = 0;
            for (int i = 0; i < anlagen.Count; i++)
            {
                BhkwAnlage a = anlagen[i];
                gewicht[i] = nachLeistung ? a.PelKW : 1.0;
                beginnJeAnlage[i] = a.Inbetriebnahme.HasValue ? a.Inbetriebnahme.Value.Year
                                                              : foerderbeginn;
                deckelFest[i] = a.VbhDeckel.HasValue && a.VbhDeckel.Value > 0
                              ? a.VbhDeckel.Value : 0;

                satzEigen += gewicht[i] * SatzEigenDerAnlage(a, eigene);
                satzEinsp += gewicht[i] * (a.SatzEinspCt ?? 0);
                kontingent += gewicht[i] * (a.VbhKontingent.HasValue && a.VbhKontingent.Value > 0
                                          ? a.VbhKontingent.Value
                                          : KontingentDerAnlage(a, beginnJeAnlage[i], eigene, luecken));
            }
            satzEigen /= gewichtSumme;
            satzEinsp /= gewichtSumme;
            kontingent /= gewichtSumme;

            // Zuerst die Ansage, welcher Weg gerechnet wird, dann seine Einzelheiten.
            hinweise.Add(string.Format(BerichtTexte.Kultur,
                T("WIRT_KWKG_ERSATZ_GEWICHTET",
                  "KWKG: Anlagen- und Ergebniszeilen ließen sich nicht zuordnen — " +
                  "gerechnet wird mit einer leistungsgewichteten Gesamtanlage aus {0} " +
                  "Anlagen ({1} kW_el)."),
                anlagen.Count.ToString("N0", BerichtTexte.Kultur),
                pelSumme.ToString("N0", BerichtTexte.Kultur)));
            if (!nachLeistung)
                hinweise.Add(T("WIRT_KWKG_ERSATZ_OHNE_LEISTUNG",
                    "KWKG: Keine der Anlagen führt eine elektrische Nennleistung — die " +
                    "Gesamtanlage wird deshalb arithmetisch gemittelt statt nach " +
                    "Leistung gewichtet."));
            Entdoppelt(eigene, hinweise);

            // ETAPPE E7c — Fall 2 auf dem Ersatzweg (Begründung im Kopf). Ohne Kennzeichen
            // an irgendeiner Anlage läuft nichts davon, und die Mengen bleiben Zeile für
            // Zeile die von vorher.
            string fall2Zeile = ErsatzwegFall2(v, anlagen, gewicht, gewichtSumme, nachLeistung, mitMatrix,
                                               ref eigenNettoMWh, ref einspNettoMWh, ref stromNettoMWh,
                                               luecken);
            if (fall2Zeile != null) hinweise.Add(fall2Zeile);

            // ETAPPE E7c3 — VOLLBENUTZUNGSSTUNDEN NACH DEFINITION (Anwenderentscheid vom
            // 23.09.2026): Vbh = erzeugte Arbeit ÷ P_Nenn — die elektrische Arbeit, die das
            // Modul an den Klemmen erzeugt (brutto), durch seine elektrische Nennleistung;
            // ein normierter Auslastungsindikator, in Fall 1 und Fall 2 derselbe. Auf dem
            // Ersatzweg ist das die Projektgröße aus VbhElektrisch, mit der Kontingent und
            // Jahresdeckel zählen; der KWK-Strom des Falls 2 bestimmt allein die bezahlte
            // Menge (die gekürzten Mengen oben). Trägt eine Anlage das Kennzeichen, nennt
            // eine Herleitungszeile die Rechnung mit Zahlen — ohne Kennzeichen bleibt der
            // Hinweistext Zeile für Zeile der von vorher.
            if (fall2Zeile != null && nachLeistung && vbh > 0)
            {
                double kwkMWh = mitMatrix ? eigenNettoMWh + einspNettoMWh : stromNettoMWh;
                hinweise.Add(string.Format(BerichtTexte.Kultur, T("WIRT_KWKG_FALL2_VBH_ERSATZ",
                    "KWKG § 2 Nr. 16 Fall 2 auf dem Ersatzweg: Vbh = erzeugte Arbeit ÷ P_Nenn = " +
                    "{0} MWh ÷ {1} kW = {2} h/a (brutto an den Klemmen, wie in Fall 1); " +
                    "Kontingent und Jahresdeckel zählen diese Stunden, der KWK-Strom " +
                    "{3} MWh bestimmt allein die bezahlte Menge."),
                    stromBruttoMWh.ToString("N3", BerichtTexte.Kultur),
                    pelSumme.ToString("N0", BerichtTexte.Kultur),
                    vbh.ToString("N0", BerichtTexte.Kultur),
                    Math.Max(0, kwkMWh).ToString("N3", BerichtTexte.Kultur)));
            }

            // ---------------- Bonus bei voller Vergütung [€/a] ----------------
            //  - W3-Split: getrennte Sätze auf KWK-Eigenstrom und -Einspeisung.
            //  - Fallback ohne Stundenreihen: Eigenstrom-Satz auf die Gesamtmenge (W2).
            //  - B3b: beide Mengen sind NETTO (Erzeugung minus Hilfsstrom, § 4.3);
            //    ohne gepflegten Anteil sind sie zeilengleich den Bruttomengen.
            //  - E7c2: Betrag und Stunden aus KwkgJahresbetrag — derselbe Ausdruck wie im
            //    Weg je Anlage und in der Überlagerung (ohne Einspeisung + 0,0: bitgleich).
            double bonusVoll;
            if (mitMatrix)
                bonusVoll = KwkgJahresbetrag.Voll(eigenNettoMWh, satzEigen, einspNettoMWh, satzEinsp);
            else
                bonusVoll = KwkgJahresbetrag.Voll(stromNettoMWh, satzEigen, 0, 0);
            if (bonusVoll <= 0) return null;

            double abschlag = KwkgJahresbetrag.Abschlag(p.KwkgAbschlagNegativ);

            int jahre = Math.Max(1, p.Betrachtungszeitraum);
            double[] reihe = new double[jahre + 1];
            double rest = kontingent;
            for (int t = 1; t <= jahre; t++)
            {
                if (rest <= 0) break;

                // Der Jahresdeckel der virtuellen Gesamtanlage — je Jahr neu gemischt
                // (Begründung im Kopf).
                double deckel = 0;
                for (int i = 0; i < anlagen.Count; i++)
                    deckel += gewicht[i] * (deckelFest[i] > 0
                                          ? deckelFest[i]
                                          : StaffelDeckel(staffel, beginnJeAnlage[i] + t - 1));
                deckel /= gewichtSumme;

                double verguetet = KwkgJahresbetrag.VerguetetH(vbh, deckel, rest, abschlag);
                reihe[t] = bonusVoll * (verguetet / vbh);
                rest -= verguetet;   // Negativpreis-Stunden verbrauchen das Kontingent nicht
            }
            jahr1 = reihe[1];
            return reihe;
        }

        /// <summary>
        /// Hängt <paramref name="quelle"/> an <paramref name="ziel"/> an und lässt dabei
        /// jede wortgleiche Wiederholung INNERHALB der Quelle weg (Ordinalvergleich).
        /// Gebraucht vom Ersatzweg: Seine Prüfungen laufen je Anlage und erzeugen bei N
        /// gleichartigen Anlagen N gleiche Zeilen.
        /// </summary>
        private static void Entdoppelt(List<string> quelle, List<string> ziel)
        {
            var gesehen = new List<string>();
            foreach (string s in quelle)
            {
                bool schon = false;
                foreach (string g in gesehen)
                    if (string.Equals(g, s, StringComparison.Ordinal)) { schon = true; break; }
                if (schon) continue;
                gesehen.Add(s);
                ziel.Add(s);
            }
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
                                       List<KwkgModulNachweis> nachweise, KwkgLuecken luecken,
                                       out double jahr1)
        {
            jahr1 = 0;
            if (_staffelCache == null) _staffelCache = LadeKwkgStaffel();
            List<KeyValuePair<int, double>> staffel = _staffelCache;
            // ETAPPE E7c2 (E7c1-Q7): Abschlag, voller Jahresbetrag und vergütete Stunden
            // stehen in KwkgJahresbetrag — die Überlagerung „Sätze und Herkunft" ruft
            // dieselben Ausdrücke für ihre „Wirkung Jahr 1".
            double abschlag = KwkgJahresbetrag.Abschlag(p.KwkgAbschlagNegativ);
            int T = Math.Max(1, p.Betrachtungszeitraum);

            // ETAPPE E7c (Befund K-1): die zwei Projektgrößen, die Fall 2 braucht — der
            // Wärmeüberschuss liegt nur als Projektsumme vor (gemessen mit E7a, A2) und
            // wird allein nach P_el auf die Module verteilt; die Leistungssumme über ALLE
            // Anlagen, weil der Überschuss physikalisch ist, nicht förderrechtlich.
            double ueberschussMWh = v.Ergebnis != null && v.Ergebnis.BHKW != null
                                  ? v.Ergebnis.BHKW.Waermeueberschuss : 0;
            double pelSummeKW = 0;
            foreach (BhkwAnlage an in auswahl.Anlagen) pelSummeKW += Math.Max(0, an.PelKW);

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

                // ETAPPE E7c — DER ZWEITE FALL DES § 2 Nr. 16 KWKG (Befund K-1, Entscheid
                // E7-Q2). Trägt die Anlage das Kennzeichen „Vorrichtung zur
                // Abwärmeabfuhr", ist ihr KWK-Strom nicht die Nettostromerzeugung, sondern
                // min(Netto, Nutzwärme × σ). Der ANTEIL am Split bleibt der physikalische
                // (Netto) — träte der KWK-Strom an seine Stelle, bliebe bei einer einzigen
                // Anlage alles gleich (E7-Q2 (3)). Gekürzt wird die zugeteilte Menge, und
                // zwar zuerst die Einspeisung. Ohne bestimmbare Kennzahl gibt es keinen
                // Ersatz: kein KWK-Strom nach Fall 2, der Zuschlag der Anlage ist 0.
                // OHNE KENNZEICHEN läuft dieser Block nicht — Zeile für Zeile wie vorher.
                KwkStromFall2 fall2 = null;
                string fall2Zeile = null;
                if (a.Abwaermeabfuhr)
                {
                    fall2 = Fall2DerAnlage(a, auswahl.Module[i], stromAnlageMWh, ueberschussMWh, pelSummeKW);
                    double eigenVor = eigenMWh, einspVor = einspMWh;
                    if (fall2.Stromkennzahl.Bestimmbar)
                    {
                        KwkStromRechner.Kuerzen(fall2.KuerzungMWh, ref eigenMWh, ref einspMWh);
                    }
                    else
                    {
                        eigenMWh = 0;
                        einspMWh = 0;
                        if (luecken != null) KwkgLuecken.Merke(luecken.OhneStromkennzahl, a.Bezeichner);
                    }
                    fall2Zeile = Fall2Zeile(a, fall2, einspVor - einspMWh, eigenVor - eigenMWh);
                    hinweise.Add(fall2Zeile);
                }

                // ETAPPE BK1 — KEIN RÜCKFALL MEHR AUF DAS PROJEKT. Was hier gerechnet
                // wird, steht an der Anlage; NULL heißt jetzt 0 und nicht „Projektwert".
                // Möglich wird das durch Schemaschritt 89, der die Projektvorgaben
                // einmalig in jede leere Anlagenzelle geschrieben hat.
                double satzEigen = SatzEigenDerAnlage(a, hinweise);
                double satzEinsp = a.SatzEinspCt ?? 0;
                double bonusVoll = KwkgJahresbetrag.Voll(eigenMWh, satzEigen, einspMWh, satzEinsp);
                if (bonusVoll <= 0) continue;

                // ETAPPE E7c3 — Vbh NACH DEFINITION (Anwenderentscheid vom 23.09.2026):
                // Vbh = erzeugte Arbeit ÷ P_Nenn, die Arbeit brutto an den Klemmen. Der
                // Rückfall ohne gespeicherte Modulzahl nimmt deshalb die BRUTTOerzeugung
                // dieses Moduls (bis E7c3 stand hier die Nettomenge nach Hilfsstrom — nur
                // wirksam, wo die Ergebniszeile keine Vbh führt UND ein Anteil gepflegt ist).
                double vbhAnlage = VbhDerAnlage(a, auswahl.Module[i], stromBruttoMWh);

                // In Fall 2 bleibt es bei denselben Stunden wie in Fall 1: Kontingent-
                // verbrauch und Jahresdeckel zählen die Bruttostunden, der KWK-Strom
                // bestimmt allein die bezahlte Menge (die gekürzten Mengen oben). Die
                // Herleitungszeile nennt die Rechnung mit Zahlen; ohne Kennzeichen
                // (Fall 1) wird der Zweig nicht betreten — Zeile für Zeile wie vorher.
                if (fall2 != null && a.PelKW > 0 && vbhAnlage > 0)
                {
                    // WirtschaftlichkeitCtrl.T: die Laufzeit T dieser Methode verdeckt den Namen.
                    hinweise.Add(string.Format(BerichtTexte.Kultur, WirtschaftlichkeitCtrl.T("WIRT_KWKG_FALL2_VBH",
                        "KWKG § 2 Nr. 16 Fall 2 — „{0}“: Vbh = erzeugte Arbeit ÷ P_Nenn = " +
                        "{1} MWh ÷ {2} kW = {3} h/a (brutto an den Klemmen, wie in Fall 1); " +
                        "Kontingent und Jahresdeckel zählen diese Stunden, der KWK-Strom " +
                        "{4} MWh bestimmt allein die bezahlte Menge."),
                        a.Bezeichner, stromBruttoMWh.ToString("N3", BerichtTexte.Kultur),
                        a.PelKW.ToString("N0", BerichtTexte.Kultur),
                        vbhAnlage.ToString("N0", BerichtTexte.Kultur),
                        fall2.KwkStromMWh.ToString("N3", BerichtTexte.Kultur)));
                }
                if (vbhAnlage <= 0) continue;

                int beginn = a.Inbetriebnahme.HasValue ? a.Inbetriebnahme.Value.Year : foerderbeginn;

                // BK1: Der Override der Anlage gewinnt; ohne ihn wird das Kontingent
                // NACH § 8 AUS DIESER ANLAGE abgeleitet (Anlagenart und Kostenanteil der
                // Anlage) — bis BK1 fiel es auf die projektweite Größe zurück, die für
                // eine Kaskade nur eines ihrer Module treffen konnte.
                double kontingent = a.VbhKontingent.HasValue && a.VbhKontingent.Value > 0
                                  ? a.VbhKontingent.Value
                                  : KontingentDerAnlage(a, beginn, hinweise, luecken);
                double deckelFest = a.VbhDeckel.HasValue && a.VbhDeckel.Value > 0
                                  ? a.VbhDeckel.Value : 0;

                double rest = kontingent;
                double jahr1Modul = 0;              // E7: Nachweis, kein Rechenweg
                int erschoepftAb = 0;
                for (int t = 1; t <= T; t++)
                {
                    if (rest <= 0) { if (erschoepftAb == 0) erschoepftAb = t; break; }
                    double deckel = deckelFest > 0 ? deckelFest
                                                   : StaffelDeckel(staffel, beginn + t - 1);
                    double verguetet = KwkgJahresbetrag.VerguetetH(vbhAnlage, deckel, rest, abschlag);
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
                    // ETAPPE E7c: der zweite Fall steht nur an einer Anlage mit
                    // Kennzeichen — sonst bleiben die Felder null (Fall 1).
                    if (fall2 != null)
                    {
                        n.Abwaermeabfuhr = true;
                        n.Stromkennzahl = fall2.Stromkennzahl.Wert;
                        n.StromkennzahlHerkunft = fall2.Stromkennzahl.Herkunft;
                        n.NutzwaermeMWh = fall2.NutzwaermeMWh;
                        n.KwkStromMWh = fall2.KwkStromMWh;
                        n.KuerzungMWh = fall2.KuerzungMWh;
                        // E7c3: VbhElektrisch (oben) trägt in beiden Fällen die Stunden
                        // nach Definition (erzeugte Arbeit ÷ P_Nenn); die Herleitung ist
                        // die der Menge.
                        n.HerleitungKwkStrom = fall2Zeile;
                    }
                    try
                    {
                        if (_gesetze == null) _gesetze = new GesetzKatalog();
                        KwkgSatzVorschlag vs = KwkgSatzRechner.Vorschlag(
                            a.PelKW, beginn, a.Anlagenart, a.Eigenfall,
                            (s, j) => _gesetze.WertMitHerkunft(s, j), BerichtTexte.Kultur);
                        n.HerleitungEigen = vs.HerleitungEigen ?? "";
                        n.HerleitungEinspeisung = vs.HerleitungEinspeisung ?? "";
                        // AUFTRAG #351 (U23): der Vorschlag auch als ZAHL — erst damit
                        // kann die Erlösrubrik ohne Katalog sagen, ob der angesetzte
                        // Satz ein eigener Wert ist.
                        n.VorschlagEigenCt = vs.SatzEigenCt;
                        n.VorschlagEinspeisungCt = vs.SatzEinspeisungCt;
                    }
                    catch (Exception ex)
                    {
                        // ETAPPE E7c3 (B‑6): benannt — der angesetzte Satz steht längst; es
                        // fehlt nur seine Herleitung im Nachweis, und die Ergebniszeile
                        // sagt es.
                        Stufenfehler(v.IdProjekt, STUFE_SATZHERLEITUNG, Fehlergrund.Text(ex));
                    }
                    nachweise.Add(n);
                }

                // Der Bezeichner ist ein Datenwert, die Klammer trägt nur Zahlen und
                // Einheitenzeichen — sie bleibt deshalb im Code (Drei-Schichten-Regel,
                // wie bei KwkgAnlagenauswahl.Klartext).
                beschreibung.Add(a.Bezeichner + " (" + a.PelKW.ToString("N0") + " kW, " +
                                 vbhAnlage.ToString("N0") + " h/a, " +
                                 satzEigen.ToString(KwkgSatzHerkunft.ZAHLFORMAT) + "/" +
                                 satzEinsp.ToString(KwkgSatzHerkunft.ZAHLFORMAT) + " ct/kWh, " +
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

        // =====================================================================
        // ETAPPE E7c — der zweite Fall des § 2 Nr. 16 KWKG (Befund K-1)
        // =====================================================================

        /// <summary>
        /// Der zweite Fall für EINE zugeordnete Anlage (Regelweg): σ nach Entscheid
        /// E7‑Q2 (2), die Wärme des Moduls, dessen Anteil am Wärmeüberschuss des Projekts
        /// nach P_el (E7‑Q2 (1)) — gerechnet von <see cref="KwkStromRechner"/>, derselben
        /// reinen Funktion, die der Dialog für den Vorschlag ruft.
        /// </summary>
        private static KwkStromFall2 Fall2DerAnlage(BhkwAnlage a, ErgebnisBHKWModulModel modul,
                                                    double nettoMWh, double ueberschussMWh,
                                                    double pelSummeKW)
        {
            KwkStromkennzahl sigma = KwkStromRechner.Stromkennzahl(a.Stromkennzahl, a.PelKW, a.PthKW,
                                                                  BerichtTexte.Kultur);
            double waerme = modul == null ? 0 : modul.Waermeproduktion;
            double anteil = KwkStromRechner.UeberschussAnteil(ueberschussMWh, a.PelKW, pelSummeKW);
            return KwkStromRechner.Fall2(nettoMWh, waerme, anteil, sigma);
        }

        /// <summary>
        /// Die Herleitungszeile des zweiten Falls für EINE Anlage (E7‑Q2 (5)): Fall, σ
        /// und seine Herkunft, Nutzwärme, KWK-Strom und Kürzung — aufgeteilt auf
        /// Einspeisung und Eigenverbrauch, wie sie tatsächlich abgezogen wurde. Ohne
        /// bestimmbare Kennzahl die Zeile „kein KWK-Strom nach Fall 2".
        /// </summary>
        private static string Fall2Zeile(BhkwAnlage a, KwkStromFall2 f, double vonEinspMWh,
                                         double vonEigenMWh)
        {
            System.Globalization.CultureInfo k = BerichtTexte.Kultur;
            if (!f.Stromkennzahl.Bestimmbar)
                return string.Format(k, T("WIRT_KWKG_FALL2_OHNE_SIGMA",
                    "KWKG § 2 Nr. 16 Fall 2 (Vorrichtung zur Abwärmeabfuhr) — „{0}“: keine " +
                    "Stromkennzahl ({1}) — kein KWK-Strom nach Fall 2, für diese Anlage kein " +
                    "Zuschlag."),
                    a.Bezeichner, f.Stromkennzahl.Herleitung);

            return string.Format(k, T("WIRT_KWKG_FALL2_ANLAGE",
                "KWKG § 2 Nr. 16 Fall 2 (Vorrichtung zur Abwärmeabfuhr) — „{0}“: Stromkennzahl " +
                "σ {1} ({2}); Nutzwärme {3} MWh (Wärmeproduktion {4} MWh − Anteil am " +
                "Wärmeüberschuss {5} MWh); KWK-Strom min({6} ; {3} × {1}) = {7} MWh; Kürzung " +
                "{8} MWh, davon Einspeisung {9} MWh und Eigenverbrauch {10} MWh."),
                a.Bezeichner,
                f.Stromkennzahl.Wert.Value.ToString(KwkStromRechner.FORMAT_KENNZAHL, k),
                f.Stromkennzahl.Herleitung,
                f.NutzwaermeMWh.ToString("N3", k),
                f.WaermeMWh.ToString("N3", k),
                f.UeberschussAnteilMWh.ToString("N3", k),
                f.NettoMWh.ToString("N3", k),
                f.KwkStromMWh.ToString("N3", k),
                f.KuerzungMWh.ToString("N3", k),
                Math.Max(0, vonEinspMWh).ToString("N3", k),
                Math.Max(0, vonEigenMWh).ToString("N3", k))
                + Rundungsgrund(f.KuerzungMWh);
        }

        /// <summary>
        /// ETAPPE E7c — Entscheid E7c1‑Q1 a mit Hinweis (23.09.2026): Die Formel des
        /// zweiten Falls rechnet OHNE Toleranz; eine Kürzung unter 0,01 MWh bleibt stehen.
        /// Sie entsteht aus der Rundung — σ = P_el ÷ P_th ist unrund, die Mengen des Laufs
        /// stehen auf 0,01 MWh gerundet in der Datenbank —, und die Herleitung nennt das,
        /// damit eine Kürzung von 0,002 MWh nicht wie ein Befund aussieht. Leer, wenn es
        /// keine Kürzung gibt oder sie 0,01 MWh erreicht.
        /// </summary>
        internal static string Rundungsgrund(double kuerzungMWh)
        {
            if (!(kuerzungMWh > 0) || kuerzungMWh >= 0.01) return "";
            return " " + T("WIRT_KWKG_FALL2_RUNDUNG",
                "Die Kürzung unter 0,01 MWh entsteht aus der Rundung von σ bzw. der Mengen auf 0,01 MWh.");
        }

        /// <summary>
        /// Der zweite Fall auf dem ERSATZWEG (Entscheid E7‑Q2 (4)) — Begründung im Kopf
        /// von <see cref="ReiheErsatzGewichtet"/>. Mindert die Mengen des Aufrufers um die
        /// Summe der Kürzungen (zuerst die Einspeisung) und liefert die Herleitungszeile;
        /// <c>null</c>, wenn keine Anlage das Kennzeichen trägt — dann bleibt alles, wie es
        /// war.
        /// </summary>
        private static string ErsatzwegFall2(VariantenDaten v, List<BhkwAnlage> anlagen,
                                             double[] gewicht, double gewichtSumme, bool nachLeistung,
                                             bool mitMatrix, ref double eigenNettoMWh,
                                             ref double einspNettoMWh, ref double stromNettoMWh,
                                             KwkgLuecken luecken)
        {
            bool gesetzt = false;
            foreach (BhkwAnlage a in anlagen)
                if (a.Abwaermeabfuhr) { gesetzt = true; break; }
            if (!gesetzt || gewichtSumme <= 0) return null;

            System.Globalization.CultureInfo k = BerichtTexte.Kultur;
            double waerme = v.Ergebnis != null && v.Ergebnis.BHKW != null ? v.Ergebnis.BHKW.Waermeproduktion : 0;
            double ueberschuss = v.Ergebnis != null && v.Ergebnis.BHKW != null ? v.Ergebnis.BHKW.Waermeueberschuss : 0;
            double netto = Math.Max(0, stromNettoMWh);

            var teile = new List<string>();
            double kuerzung = 0;
            for (int i = 0; i < anlagen.Count; i++)
            {
                BhkwAnlage a = anlagen[i];
                if (!a.Abwaermeabfuhr) continue;
                double anteil = gewicht[i] / gewichtSumme;
                KwkStromkennzahl sigma = KwkStromRechner.Stromkennzahl(a.Stromkennzahl, a.PelKW, a.PthKW, k);
                // Nutzwärme des PROJEKTS mit dem Gewicht der Anlage: Wärme und Überschuss
                // mit demselben Anteil — dieselbe Zahl wie (Wärme − Überschuss) × Anteil.
                KwkStromFall2 f = KwkStromRechner.Fall2(netto * anteil, waerme * anteil,
                                                        Math.Max(0, ueberschuss) * anteil, sigma);
                kuerzung += f.KuerzungMWh;

                if (!sigma.Bestimmbar)
                {
                    if (luecken != null) KwkgLuecken.Merke(luecken.OhneStromkennzahl, a.Bezeichner);
                    teile.Add(string.Format(k, T("WIRT_KWKG_FALL2_ERSATZ_OHNE_SIGMA",
                        "„{0}“ ohne Stromkennzahl — KWK-Strom 0 MWh, Kürzung {1} MWh"),
                        a.Bezeichner, f.KuerzungMWh.ToString("N3", k)));
                }
                else
                    teile.Add(string.Format(k, T("WIRT_KWKG_FALL2_ERSATZ_ANLAGE",
                        "„{0}“ σ {1} ({2}), Nutzwärme {3} MWh, KWK-Strom {4} MWh, Kürzung {5} MWh"),
                        a.Bezeichner, sigma.Wert.Value.ToString(KwkStromRechner.FORMAT_KENNZAHL, k),
                        sigma.Herleitung, f.NutzwaermeMWh.ToString("N3", k),
                        f.KwkStromMWh.ToString("N3", k), f.KuerzungMWh.ToString("N3", k))
                        // E7c1-Q1: der Rundungsgrund einer Kürzung unter 0,01 MWh.
                        + (Rundungsgrund(f.KuerzungMWh).Length > 0
                            ? " " + T("WIRT_KWKG_FALL2_RUNDUNG_KURZ",
                                      "(aus der Rundung von σ bzw. der Mengen auf 0,01 MWh)")
                            : ""));
            }

            if (mitMatrix)
                KwkStromRechner.Kuerzen(kuerzung, ref eigenNettoMWh, ref einspNettoMWh);
            else
                stromNettoMWh = Math.Max(0, stromNettoMWh - kuerzung);

            string verteilung = nachLeistung
                ? T("WIRT_KWKG_FALL2_VERTEILUNG_PEL", "nach P_el")
                : T("WIRT_KWKG_FALL2_VERTEILUNG_GLEICH", "zu gleichen Teilen (keine Anlage führt P_el)");
            return string.Format(k, T("WIRT_KWKG_FALL2_ERSATZ",
                "KWKG § 2 Nr. 16 Fall 2 auf dem Ersatzweg — Nutzwärme des Projekts {0} MWh " +
                "(Wärmeproduktion {1} MWh − Wärmeüberschuss {2} MWh) und Nettostromerzeugung {3} MWh " +
                "{4} auf die Anlagen verteilt: {5}. Kürzung zusammen {6} MWh, zuerst von der Einspeisung."),
                KwkStromRechner.Nutzwaerme(waerme, ueberschuss).ToString("N3", k),
                waerme.ToString("N3", k), Math.Max(0, ueberschuss).ToString("N3", k),
                netto.ToString("N3", k), verteilung, string.Join("; ", teile.ToArray()),
                kuerzung.ToString("N3", k));
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
        /// <para><b>ETAPPE BK1 — kein Rückfall mehr, und eine Strenge weniger.</b> Der
        /// Satz kommt ausschließlich aus <c>Tab_Energieanlagen.KWKG_Satz_Eigen</c>; ein
        /// leeres Feld heißt 0 und nicht mehr „Projektsatz". Zugleich fällt die
        /// B3b-Strenge gegen den LEEREN Tatbestand weg — sie stand auf der Annahme, ein
        /// Satz an der Anlage sei eine ausdrückliche Eingabe, die es im Bestand nirgends
        /// gibt. Diese Annahme ist mit Schemaschritt 89 hinfällig: Dort bekommt JEDE
        /// Bestandsanlage den Projektsatz eingetragen, den niemand an ihr eingegeben hat.
        /// Hätte die Strenge Bestand, nähme der Schritt jedem Bestandsprojekt ohne
        /// gepflegten Tatbestand den Eigenverbrauchszuschlag weg — eine Rechenwirkung, die
        /// nirgends entschieden wurde. Es gilt deshalb ab hier JE ANLAGE genau die Regel,
        /// die K6 am Projekt eingeführt hat: Ein leerer Tatbestand lässt den Satz stehen
        /// und meldet „ungeprüft", <b>nur die ausdrückliche Wahl „keiner" nimmt ihn
        /// weg</b> (§ 7 Abs. 2).</para>
        /// </summary>
        private static double SatzEigenDerAnlage(BhkwAnlage a, List<string> hinweise)
        {
            double satz = a.SatzEigenCt ?? 0;                       // BK1: kein Rückfall
            if (satz <= 0) return satz;                             // nichts zu prüfen

            string fall = (a.Eigenfall ?? "").Trim();

            if (fall.Length == 0)
            {
                hinweise.Add(string.Format(T("WIRT_KWKG_TATBESTAND_ANLAGE_OFFEN",
                    "KWKG: Für „{0}“ ist kein Tatbestand nach § 6 Abs. 3 erfasst — die " +
                    "Voraussetzung des Zuschlags auf selbst genutzten Strom (§ 7 Abs. 2) " +
                    "ist ungeprüft; gerechnet wird mit dem eingetragenen Satz."),
                    a.Bezeichner));
                return satz;
            }

            if (string.Equals(fall, DbWerte.KWKG_EIGENFALL_KEINER, StringComparison.Ordinal))
            {
                hinweise.Add(string.Format(T("WIRT_KWKG_TATBESTAND_ANLAGE_KEINER",
                    "KWKG: Für „{0}“ ist als Tatbestand nach § 6 Abs. 3 „keiner“ gewählt — " +
                    "der eigene Satz auf selbst genutzten Strom entfällt (§ 7 Abs. 2)."),
                    a.Bezeichner));
                // ETAPPE E7c2: dieselbe Regel, die die Überlagerung für „Wirkung Jahr 1" ruft.
                return KwkgJahresbetrag.SatzEigenWirksam(satz, fall);
            }
            return satz;
        }

        /// <summary>
        /// ETAPPE BK1 — das Vbh-Kontingent EINER Anlage [h] nach § 8 KWKG, abgeleitet aus
        /// <b>ihrer</b> Anlagenart und <b>ihrem</b> Kostenanteil
        /// (<see cref="KwkgKontingentRechner"/>).
        ///
        /// <para><b>Warum je Anlage.</b> § 8 stellt auf die einzelne KWK-Anlage ab. Bis
        /// BK1 gab es die Ableitung nur projektweit (aus Anlagenart und Kostenanteil
        /// des Projekts); eine Kaskade aus einem neuen und einem
        /// modernisierten Modul bekam damit für beide dieselbe Stufe, obwohl ihnen
        /// verschiedene zustehen. Seit BK1a ist auch der Ersatzweg hier — die
        /// projektweite Ableitung ist ersatzlos entfallen.</para>
        ///
        /// <para>Ohne erfasste Anlagenart liefert der Rechner 0 mit Begründung — das ist
        /// dieselbe Antwort wie am Projekt und kein stiller Ausfall: Eine Anlage ohne
        /// Kontingent bekommt keinen Zuschlag, und die Herleitung sagt warum.</para>
        ///
        /// <para><b>ETAPPE E7c — die Kern-Regel zu § 6.3 Nr. 30</b> (Entscheid E7‑Q1,
        /// Lesart b, 23.09.2026): „NULL ⇒ kein Zuschlag" greift genau HIER — nur dort, wo
        /// das Kontingent aus der Anlagenart abzuleiten ist. Ein gepflegtes Kontingent
        /// kommt gar nicht bis hierher (es gilt, auch ohne Anlagenart). Fehlt die
        /// Anlagenart, hält <paramref name="luecken"/> die Anlage fest — die Grundlage
        /// der Kohärenzzeile „Anlagenart fehlt" —, und die Herleitung sagt es ohne den
        /// Nachsatz „es gilt der eingetragene Wert", der ohne eingetragenen Wert nicht
        /// stimmt.</para>
        /// </summary>
        private double KontingentDerAnlage(BhkwAnlage a, int jahr, List<string> hinweise,
                                           KwkgLuecken luecken)
        {
            if (_gesetze == null) _gesetze = new GesetzKatalog();
            System.Globalization.CultureInfo kultur = BerichtTexte.Kultur;
            KwkgKontingentVorschlag v = KwkgKontingentRechner.Ableiten(
                a.Anlagenart, a.Kostenanteil ?? 0, jahr,
                (sch, j) => _gesetze.WertMitHerkunft(sch, j), kultur);

            bool ohneArt = string.IsNullOrEmpty(a.Anlagenart);
            if (ohneArt && luecken != null) KwkgLuecken.Merke(luecken.OhneAnlagenart, a.Bezeichner);

            if (hinweise != null)
                hinweise.Add(ohneArt
                    ? string.Format(kultur,
                        T("WIRT_KWKG_KONTINGENT_ANLAGE_OHNE_ART",
                          "KWKG: Für „{0}“ ist weder ein Vbh-Kontingent gepflegt noch eine " +
                          "Anlagenart erfasst — ohne Anlagenart leitet § 8 KWKG kein Kontingent " +
                          "ab; für diese Anlage kein Zuschlag."),
                        a.Bezeichner)
                    : string.Format(kultur,
                        T("WIRT_KWKG_KONTINGENT_ANLAGE",
                          "KWKG: Für „{0}“ ist kein eigenes Vbh-Kontingent gepflegt — " +
                          "abgeleitet {1} Vbh ({2})."),
                        a.Bezeichner, v.KontingentH.ToString("N0", kultur), v.Herleitung));
            return v.KontingentH;
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
        ///
        /// <para><b>ETAPPE E7c3 — die Definition des Anwenders (23.09.2026):</b>
        /// Vbh = erzeugte Arbeit ÷ P_Nenn, die elektrische Arbeit an den Klemmen durch die
        /// installierte elektrische Nennleistung — ein normierter Auslastungsindikator,
        /// in Fall 1 und Fall 2 des § 2 Nr. 16 KWKG derselbe. Der Aufrufer reicht deshalb
        /// die BRUTTOerzeugung des Moduls herein, auch für den Rückfall.</para>
        /// </summary>
        /// <param name="stromMWh">Die Stromerzeugung des Moduls an den Klemmen [MWh/a]
        /// (brutto, vor Hilfsstrom).</param>
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
        //
        // ETAPPE BK1a: Die projektweite Ableitung des Kontingents ist entfallen. § 8
        // stellt auf die einzelne Anlage ab; seit BK1 leitet KontingentDerAnlage aus
        // IHRER Anlagenart und IHREM Kostenanteil ab, und seit BK1a gilt das für beide
        // Rechenwege. Die Pauschale nach § 9 bleibt projektweit — sie hängt am Projekt,
        // nicht an einer Anlage.
        // =====================================================================

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
            catch (Exception ex) when (ex is System.Resources.MissingManifestResourceException ||
                                       ex is System.Resources.MissingSatelliteAssemblyException ||
                                       ex is InvalidOperationException)
            {
                // ETAPPE E7c3 (B‑6): benannt — nur die Fehler der Ressourcensuche; der
                // deutsche Rückfalltext ist der Zweck dieser Methode.
                return rueckfall;
            }
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

                // AUFTRAG U7 — die Aufteilung und ihre Herleitung stammen aus
                // DEMSELBEN Jahr wie die ausgewiesene Jahr-1-Zahl; eine spätere
                // Satzänderung darf die Zeilen darunter nicht verschieben.
                e.Energiesteuer53Jahr1 = r.Energiesteuer53Eur;
                e.Energiesteuer54Jahr1 = r.Energiesteuer54Eur;
                e.Energiesteuer54SockelJahr1 = r.Energiesteuer54SockelEur;
                e.EnergiesteuerNachweise = new List<EnergiesteuerNachweis>(r.EnergiesteuerNachweise);

                // ETAPPE E7c3 (E7c2‑Q8 b) — die Vorschau je Wahl aus DEMSELBEN Jahr und
                // derselben Eingabe; sie rechnet auf Kopien, der Lauf bleibt unberührt.
                e.EnergiesteuerVorschau = SteuerGutschriftRechner.Vorschau(
                    eingabe, jahr, s => _gesetze.WertMitHerkunft(s, jahr), kultur);

                // AUFTRAG 9d — die Begründungen JE POSITION, ebenfalls aus dem ersten
                // Jahr: Sie erklären die Jahr-1-Zahl, die die Rubrik zeigt.
                e.PositionsGruende = new Dictionary<string, string>(r.PositionsGruende,
                                                                    StringComparer.Ordinal);

                foreach (string s in r.Begruendungen) if (!begruendungen.Contains(s)) begruendungen.Add(s);
                foreach (string s in r.Herkunft) if (!herkunft.Contains(s)) herkunft.Add(s);
            }

            e.EnergiesteuerJahr1 = energie[1];
            e.StromsteuerBefreiungJahr1 = befreiung[1];
            e.StromsteuerEntlastungJahr1 = entlastung[1];
            if (herkunft.Count > 0) e.SteuerHerkunft = string.Join(" | ", herkunft.ToArray());

            // ETAPPE B6 — der Modus des § 9 Abs. 1 Nr. 3 StromStG wandert mit ins
            // Ergebnis: Die Vergleichstabelle beschriftet ihre Zeile danach, und der
            // Kohärenzfall zur Doppelzählung hängt an derselben Wahl.
            e.StromsteuerBefreiungAlsErloes = p != null && p.StromsteuerBefreiungAlsErloes;

            // Eine Reihe ohne jeden Betrag wird gar nicht erst angehängt — sie hätte im
            // Kapitalwert keine Wirkung und im Bericht (E7) keinen Aussagewert.
            Reihe(e, KapitalwertRechner.ErloesReihe.ENERGIESTEUER, energie);

            // ETAPPE B6 (Befund B-1) — § 9 Abs. 1 Nr. 3 StromStG ist KEINE
            // Rückerstattung: Auf selbst erzeugten und selbst verbrauchten Strom
            // entsteht gar keine Stromsteuer, der Vorteil steckt bereits in der
            // kleineren Bezugsrechnung. Im Modus AUSWEIS (Vorgabe) wird der Betrag
            // deshalb GERECHNET und in StromsteuerBefreiungJahr1 gezeigt, aber nicht
            // angehängt — er geht nicht in die Erlöse und nicht in den Kapitalwert.
            // Nur der ausdrückliche Modus ERLOES bucht die Reihe wie bis B5.
            if (e.StromsteuerBefreiungAlsErloes)
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

            eingabe.NetzbezugMWh = NetzbezugFuerStromsteuer(v, matrix);

            return eingabe;
        }

        /// <summary>
        /// <b>Die Bemessungsmenge der Entlastung nach § 9b StromStG</b> [MWh/a] — der bezogene,
        /// versteuerte Strom des Laufs.
        ///
        /// <para>Der Netzbezug des Anschlusses: die Stundenreihe, sonst die Jahressumme des Laufs —
        /// beides sind gerechnete Größen desselben Laufs, keine Näherung.</para>
        ///
        /// <para><b>Strombedarf ohne Verwendung</b> (Anwenderentscheide 22.09.2026): 0. Die
        /// Entlastung ist eine Gutschrift auf den bezogenen Strom; wo dieser Strom weder bepreist
        /// noch bewertet wird, gibt es auch nichts zu entlasten — sonst stünde eine Gutschrift auf
        /// Kosten, die nicht angesetzt sind.</para>
        ///
        /// <para><b>Der Kältestrom eines eigenen Zählers</b> (Stufe KU2 Welle 4; Kühlkonzept 6.2;
        /// Entscheide E34, E35) ist versteuerter Strom aus dem Netz, bezogen NEBEN dem Anschluss: Er
        /// steht weder in der Stundenreihe des Netzbezugs noch in <c>Stromrestbedarf</c> und kommt
        /// deshalb genau einmal hinzu, mit der Menge der <see cref="Kaeltestromabrechnung"/> — wie in
        /// Energiekosten und Autarkie. Er zählt nicht als Netzbezug des Projektträgers (dessen Menge
        /// bleibt die des Anschlusses) und nicht als vermiedener Bezug (die vermiedene Menge ist
        /// Bedarf minus Restbezug des Anschlusses). Der anteilige Kältestrom steht als Teil des
        /// Netzbezugs schon darin; ohne eigenen Zähler ist der Summand 0.</para>
        /// </summary>
        internal static double NetzbezugFuerStromsteuer(VariantenDaten v, StromMatrix matrix)
        {
            if (v == null || v.StrombedarfOhneVerwendungMWh.HasValue) return 0.0;
            double anschluss = matrix != null ? matrix.BezugGesamtMWh
                : (v.Ergebnis != null && v.Ergebnis.Energiebedarf != null ? v.Ergebnis.Energiebedarf.Stromrestbedarf : 0);
            return anschluss + Kaeltestromabrechnung.EigenerZaehlerMwh(v.Ergebnis);
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
        /// <para><b>Umsetzungskonzept iU3, Kante K5:</b> Die Ableitung selbst steht seit
        /// dem Kappen der Brücken bei <see cref="HilfsstromRechner.KesselBrennstoffMWh"/>
        /// — <see cref="ErgebnisCtrl"/> braucht sie im Speicherweg und darf dafür nicht
        /// die gesamte Wirtschaftlichkeit mitziehen. Hier steht die Weiterleitung; der
        /// begründende Text bleibt stehen, weil er zu dieser Rechnung gehört.</para>
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
            return HilfsstromRechner.KesselBrennstoffMWh(m);
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
            string lesefehler = null;   // ETAPPE E7c3 (B‑6)
            try
            {
                // ETAPPE E7c3 (B‑6): der strenge Leseweg, damit der Fang unten greift.
                DataTable dt = StilleDb.TabelleStreng(
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
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannter Rückfall auf den Katalogwert (Stufe 2);
                // der Grund bleibt stehen, falls auch der Katalog nichts liefert.
                lesefehler = Fehlergrund.Text(ex);
            }

            if (t.EffHi <= 0 || t.Einheit.Length == 0)
                try
                {
                    DataTable dt = StilleDb.TabelleStreng(
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
                catch (Exception ex)
                {
                    // ETAPPE E7c3 (B‑6): benannt — Heizwert 0 und leere Einheit hießen sonst
                    // still „nicht gepflegt"; die Ergebniszeile nennt den Grund.
                    if (lesefehler == null) lesefehler = Fehlergrund.Text(ex);
                }

            // Nur ein Fehler, der am Ende OHNE Wert dasteht, ist eine gescheiterte Stufe;
            // lieferte der Katalog, war die erste Stufe ein benannter Rückfall.
            if (lesefehler != null && (t.EffHi <= 0 || t.Einheit.Length == 0))
                Stufenfehler(idProjekt, STUFE_TRAEGER, lesefehler);

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
        ///
        /// <para><b>Heizwertbezogen ist hier nur der SCHLÜSSEL.</b> Den Grenzwert prüft
        /// <see cref="SteuerGutschriftRechner.Co2JeEnergieertrag"/> brennwertbezogen
        /// (Konzept § 6.3 Nr. 29): Zu Erdgas liest er den Ho-Schlüssel, sonst rechnet er
        /// über die Heizwerte des Trägers um.</para>
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
            // ETAPPE E7c2: die Rückfallstaffel steht einmal, in KwkgJahresbetrag — der
            // Dialog fällt auf dieselben Zahlen zurück.
            if (liste.Count == 0) liste.AddRange(KwkgJahresbetrag.STAFFEL_RUECKFALL);
            return liste;
        }

        /// <summary>Deckel des Kalenderjahres: letzte Staffelzeile mit JahrVon ≤ Jahr
        /// (<see cref="KwkgJahresbetrag.StaffelDeckel(IReadOnlyList{KeyValuePair{int, double}}, int)"/>).</summary>
        private static double StaffelDeckel(List<KeyValuePair<int, double>> staffel, int jahr)
        {
            return KwkgJahresbetrag.StaffelDeckel(staffel, jahr);
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
        private static double LiesBhkwLeistungKW(int idProjekt, out string lesefehler)
        {
            lesefehler = null;

            // 1. Σ P_el über die ANLAGENZEILEN — dieselbe Menge, die die Engine rechnet.
            try
            {
                // ETAPPE E7c3 (B‑6): der strenge Leseweg (StilleDb.ScalarStreng) für
                // beide Stufen, damit ihre Fänge greifen.
                object o = StilleDb.ScalarStreng(
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
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannter Rückfall auf Stufe 2; der Grund bleibt
                // stehen, falls auch sie nichts liefert.
                lesefehler = Fehlergrund.Text(ex);
            }

            // 2. Rückfall: Σ P_el über die Gerätezeilen (der Weg bis Etappe E2).
            try
            {
                object o = StilleDb.ScalarStreng(
                    "SELECT SUM(Pel) FROM Tab_BHKW WHERE ID_Projekt = ?",
                    new DbParam("@p", idProjekt));
                if (o != null && o != DBNull.Value) { lesefehler = null; return Convert.ToDouble(o); }
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt — 0 hieße sonst „keine Leistung gepflegt".
                if (lesefehler == null) lesefehler = Fehlergrund.Text(ex);
            }
            return 0;
        }

        /// <summary>Σ P_el des Projekts [kW], einmal je Berechne-Lauf gelesen. ETAPPE E7c3
        /// (B‑6): Scheitert das Lesen, bleibt es bei 0, und die Ergebniszeile nennt den
        /// Grund (statt „keine elektrische Nennleistung gepflegt").</summary>
        private double PelKW(int idProjekt)
        {
            if (!_pelCache.ContainsKey(idProjekt))
            {
                _pelCache[idProjekt] = LiesBhkwLeistungKW(idProjekt, out string lesefehler);
                if (lesefehler != null) Stufenfehler(idProjekt, STUFE_LEISTUNG, lesefehler);
            }
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
            /// Es entscheidet über die Frist zur Inbetriebnahme, Satzstichtag, Deckelstaffel und
            /// über Neuanlage/Bestandsanlage (Heizöl-Ausschluss).</summary>
            public DateTime? Inbetriebnahme;

            /// <summary>Anlagenart, Steuerwert <c>DbWerte.KWKG_ANLAGENART_*</c>; leer =
            /// nicht erfasst (der Vorschlag rechnet dann als Neuanlage). Ohne
            /// Rechenwirkung — steuert nur den Katalogvorschlag.</summary>
            public string Anlagenart = "";

            /// <summary>Tatbestand des § 6 Abs. 3, Steuerwert
            /// <c>DbWerte.KWKG_EIGENFALL_*</c>; leer = keiner. Ohne Rechenwirkung.</summary>
            public string Eigenfall = "";

            /// <summary>Einspeisesatz dieser Anlage [ct/kWh]; <c>null</c> oder 0 = kein
            /// Satz (Etappe BK1a: die Projektvorgabe ist entfallen).</summary>
            public double? SatzEinspCt;

            /// <summary>Eigenstromsatz dieser Anlage [ct/kWh]; <c>null</c> oder 0 = kein
            /// Satz (Etappe BK1a: die Projektvorgabe ist entfallen).</summary>
            public double? SatzEigenCt;

            /// <summary>Vbh-Kontingent dieser Anlage [h]; <c>null</c> = Projektwert.</summary>
            public double? VbhKontingent;

            /// <summary>Jahresdeckel-Override dieser Anlage [h/a]; <c>null</c> oder 0 =
            /// die Staffel des § 8 Abs. 4 (Etappe BK1: kein Rückfall auf den
            /// Projekt-Override mehr).</summary>
            public double? VbhDeckel;

            /// <summary>ETAPPE BK1 — Anteil an den Neuherstellungskosten DIESER Anlage
            /// [%] (§ 8 Abs. 2/3); <c>null</c> oder 0 = nicht gepflegt. Er wählt zusammen
            /// mit <see cref="Anlagenart"/> die Kontingentstufe der Anlage.</summary>
            public double? Kostenanteil;

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

            // ---------------- ETAPPE E7c — § 2 Nr. 16 KWKG, zweiter Fall ----------------

            /// <summary>ETAPPE E7c (Befund K‑1): <c>Tab_Energieanlagen.KWKG_Abwaermeabfuhr</c>
            /// — true = die Anlage verfügt über eine Vorrichtung zur Abwärmeabfuhr (Fall 2);
            /// false (0, fehlende Spalte) = Fall 1, die Nettostromerzeugung.</summary>
            public bool Abwaermeabfuhr;

            /// <summary>ETAPPE E7c: <c>Tab_Energieanlagen.KWKG_Stromkennzahl</c>;
            /// <c>null</c> = nicht gepflegt — dann gilt P_el ÷ P_th der Gerätezeile
            /// (<see cref="KwkStromRechner.Stromkennzahl"/>).</summary>
            public double? Stromkennzahl;

            /// <summary>ETAPPE E7c: <c>Tab_BHKW.Ptherm</c> [kW] der Gerätezeile — der Nenner
            /// des Vorschlags σ = P_el ÷ P_th; <c>null</c> = nicht erfasst.</summary>
            public double? PthKW;
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
            /// Fertige Meldungen zu Anlagen, die an <b>Stichtag oder Frist zur Inbetriebnahme</b>
            /// des § 6 gescheitert sind (Etappe E6). Sie stehen einzeln statt als
            /// Aufzählung, weil jede ihr eigenes Datum nennt.
            /// </summary>
            public readonly List<string> Fristmeldungen = new List<string>();

            /// <summary>
            /// ETAPPE E7c (A20) — Zeilen zu Anlagen, deren Frist zur Inbetriebnahme sich
            /// NICHT prüfen ließ, weil der Katalog für ihr Inbetriebnahmejahr kein
            /// Fristende führt. Die Anlage bleibt förderfähig; die Zeile sagt, dass die
            /// Prüfung fehlt, statt still eine Vorgabe anzunehmen.
            /// </summary>
            public readonly List<string> Fristhinweise = new List<string>();

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
        /// <summary>
        /// ETAPPE E7c (A20, Entscheid E7‑Q3 Lesart b) — das <b>Ende der Frist zur
        /// Inbetriebnahme</b> aus dem Gesetzeskatalog
        /// (<c>DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE</c>, als Kalenderjahr: bis zum
        /// 31.12. dieses Jahres), nachgeschlagen mit dem Inbetriebnahmejahr der Anlage.
        ///
        /// <para><b>Nullbar gelesen.</b> Fehlt die Zeile oder ihr Wert, ist das
        /// <c>null</c> — und der Aufrufer schreibt eine Herleitungszeile statt eine
        /// Vorgabe anzunehmen (keine stille Konstante wie die entfallene
        /// Realisierungsfrist von vier Jahren).</para>
        /// </summary>
        /// <param name="jahr">Stichjahr der Nachschlagung — das Inbetriebnahmejahr.</param>
        /// <param name="quelle">Die Katalogzeile (Schlüssel, Quelle) für die Herleitung;
        /// <c>null</c>, wenn es keine gibt.</param>
        private DateTime? FristendeInbetriebnahme(int jahr, out GesetzParameter quelle)
        {
            quelle = null;
            try
            {
                if (_gesetze == null) _gesetze = new GesetzKatalog();
                quelle = _gesetze.WertMitHerkunft(DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE, jahr);
                if (quelle == null || !quelle.Wert.HasValue) return null;
                int bis = (int)Math.Round(quelle.Wert.Value);
                if (bis < GesetzKatalog.JAHR_MIN || bis > GesetzKatalog.JAHR_MAX) return null;
                return new DateTime(bis, 12, 31);
            }
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannt — null heißt „kein Fristende": Der Aufrufer
                // schreibt dann die Herleitungszeile „Fristende nicht im Katalog —
                // ungeprüft" (Entscheid E7c1‑Q4 a), nie eine stille Vorgabe.
                return null;
            }
        }

        /// <summary>Die Herleitungszeile „Fristende nicht im Katalog — ungeprüft";
        /// <paramref name="anlage"/> leer = der Projektblock (keine Anlage mit eigenem
        /// Datum).</summary>
        private static string FristendeFehltZeile(string anlage, int jahr)
        {
            return string.Format(BerichtTexte.Kultur,
                T("WIRT_KWKG_FRISTENDE_FEHLT",
                  "KWKG: {0}Der Gesetzeskatalog führt für das Inbetriebnahmejahr {1} kein Ende " +
                  "der Frist zur Inbetriebnahme ({2}) — die Frist ist ungeprüft, gerechnet wird " +
                  "ohne sie."),
                string.IsNullOrEmpty(anlage) ? "" : anlage + " — ",
                jahr.ToString(System.Globalization.CultureInfo.InvariantCulture),
                DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE);
        }

        /// <summary>Die Herkunft einer Katalogzeile für eine Herleitung: Schlüssel und
        /// Fundstelle.</summary>
        private static string Herkunft(GesetzParameter p)
        {
            if (p == null) return DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE;
            return string.IsNullOrEmpty(p.Quelle) ? p.Schluessel : p.Schluessel + ", " + p.Quelle;
        }

        private double AusschreibungsgrenzeKW(int jahr)
        {
            try
            {
                if (_gesetze == null) _gesetze = new GesetzKatalog();
                double? katalog = _gesetze.Wert(DbWerte.GESETZ_KWKG_AUSSCHREIBUNG_GRENZE, jahr);
                if (katalog.HasValue && katalog.Value > 0) return katalog.Value;
            }
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannter Rückfall auf die wertgleiche Konstante
                // (500 kW, § 8a KWKG); ein Lesefehler des Katalogs steht über
                // GesetzKatalog.Lesefehler an der Ergebniszeile.
            }
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

                // § 6 KWKG je Anlage (Etappe E6): Stichtag und Frist zur Inbetriebnahme.
                // ETAPPE E7c (A20, E7-Q3 Lesart b): Das Fristende ist das Katalogdatum
                // (31.12.2030), nicht mehr vier Jahre nach dem Stichtag — und es gilt
                // für jede Anlage mit bekannter Inbetriebnahme, auch ohne Stichtag. Ohne
                // Katalogwert bleibt die Anlage förderfähig, und eine Zeile sagt, dass
                // die Frist ungeprüft ist (keine stille Vorgabe).
                bool nachStichtag = stichtag.HasValue && stichtag.Value.Date > KWKG_STICHTAG_ENDE;
                bool nachFrist = false;
                DateTime fristende = DateTime.MinValue;
                GesetzParameter fristquelle = null;
                if (!nachStichtag && ibn.HasValue)
                {
                    DateTime? ende = FristendeInbetriebnahme(ibn.Value.Year, out fristquelle);
                    if (ende.HasValue)
                    {
                        fristende = ende.Value;
                        nachFrist = ibn.Value.Date > fristende;
                    }
                    else
                        a.Fristhinweise.Add(FristendeFehltZeile(klartext, ibn.Value.Year));
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
                                                       klartext, fristende.ToString("dd.MM.yyyy"),
                                                       Herkunft(fristquelle)));

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
            // Migrationsschritt 22), scheitert
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
            // ETAPPE BK1: eine vierte Stufe ganz oben — der Kostenanteil je Anlage
            // entsteht erst mit Migrationsschritt 89, eine Datenbank auf 61..88 hat E6
            // und B3a, aber ihn nicht.
            // ETAPPE E7c (Befund K-1): eine fünfte Stufe ganz oben — Kennzeichen und
            // Stromkennzahl entstehen erst mit Migrationsschritt 105, dazu P_th der
            // Gerätezeile. Nur beim BHKW gefragt: Ein Kessel ist keine KWK-Anlage.
            bool k1Gefragt = idType == WizardItemClass.BHKW_TYP;
            DataTable dt = k1Gefragt ? AnlagenTabelle(idProjekt, idType, true, true, true, true) : null;
            bool mitK1 = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_KWKG_ABWAERMEABFUHR);
            if (!mitK1) dt = AnlagenTabelle(idProjekt, idType, true, true, true, false);
            bool mitBk1 = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL);
            if (!mitBk1) dt = AnlagenTabelle(idProjekt, idType, true, true, false, false);
            bool mitB3a = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_ENERGIESTEUER_WAHL);
            bool mitE6 = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_KWKG_STICHTAG);
            if (!mitB3a)
            {
                dt = AnlagenTabelle(idProjekt, idType, true, false, false, false);
                mitE6 = dt != null && dt.Columns.Contains(SchemaKatalog.SPALTE_EA_KWKG_STICHTAG);
            }
            var liste = new List<BhkwAnlage>();
            if (!mitE6)
            {
                // ETAPPE E7c3 (B‑6): Die schmalste Stufe liest STRENG — nach ihr gibt es
                // keine Leiter mehr. Bis E7c3 lieferte der Engine-Modus bei einem
                // Abfragefehler eine leere Tabelle, und das las sich wie „das Projekt hat
                // keine solche Anlage"; jetzt nennt die Ergebniszeile den Grund.
                try { dt = AnlagenTabelle(idProjekt, idType, false, false, false, false, true); }
                catch (Exception ex)
                {
                    Stufenfehler(idProjekt, STUFE_ANLAGEN, Fehlergrund.Text(ex));
                    return liste;
                }
            }
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

                    // ETAPPE BK1: null heißt „nicht gepflegt" — dann gibt es kein
                    // abgeleitetes Kontingent, sondern eine Begründung.
                    if (mitBk1) anl.Kostenanteil = D(r, SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL);

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

                    // ETAPPE E7c (Befund K-1): Ohne die Spalten (Datenbank vor Schritt
                    // 105) bleibt das Kennzeichen false — Fall 1, wie bisher.
                    if (mitK1)
                    {
                        anl.Abwaermeabfuhr = Ganzzahl(r, SchemaKatalog.SPALTE_EA_KWKG_ABWAERMEABFUHR) == 1;
                        anl.Stromkennzahl = D(r, SchemaKatalog.SPALTE_EA_KWKG_STROMKENNZAHL);
                        anl.PthKW = D(r, "Ptherm");
                    }
                    liste.Add(anl);
                }
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt statt catch { liste.Clear(); } — ohne
                // Anlagenliste fehlt die KWKG-Reihe (und die Steuerseite dieser Anlagen);
                // das bleibt der Rückfall, aber die Ergebniszeile nennt den Grund.
                liste.Clear();
                Stufenfehler(idProjekt, STUFE_ANLAGEN, Fehlergrund.Text(ex));
            }
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
        ///
        /// <para>ETAPPE E7c3 (B‑6): <paramref name="streng"/> liest über
        /// <see cref="StilleDb.TabelleStreng"/> und reicht einen Abfragefehler weiter —
        /// für die schmalste Stufe, nach der es keine Leiter mehr gibt.</para>
        /// </summary>
        private static DataTable AnlagenTabelle(int idProjekt, int idType, bool mitE6,
                                               bool mitB3a, bool mitBk1, bool mitK1,
                                               bool streng = false)
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
            // ETAPPE BK1: eigene Stufe, weil der Kostenanteil erst mit Schritt 89 kommt.
            string bk1 = mitBk1
                ? ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_KOSTENANTEIL + "]"
                : "";

            bool bhkw = idType == WizardItemClass.BHKW_TYP;

            // ETAPPE E7c (Befund K-1): eigene Stufe, weil die zwei Spalten erst mit
            // Schritt 105 kommen — und nur beim BHKW, das allein P_th in der
            // Gerätezeile führt (Nenner des Vorschlags σ = P_el ÷ P_th).
            string k1 = mitK1 && bhkw
                ? ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_ABWAERMEABFUHR + "]" +
                  ", a.[" + SchemaKatalog.SPALTE_EA_KWKG_STROMKENNZAHL + "]" +
                  ", b.Ptherm"
                : "";
            string geraet = bhkw ? "b.Pel, b.Brennstoff" : "0 AS Pel, b.Brennstoff";
            string join = bhkw
                ? "INNER JOIN Tab_BHKW AS b ON a.ID_BHKW = b.ID "
                : "LEFT JOIN Tab_Heizkessel AS b ON a.ID_Kessel = b.ID ";
            string sql =
                "SELECT a.ID, a.ID_Projekt, a.Bezeichner, a.ID_Carrier, " +
                geraet + e6 + b3a + bk1 + k1 + " " +
                "FROM Tab_Energieanlagen AS a " + join +
                // KEIN ORDER BY — bewusst. Die Zuordnung Anlage ↔ Ergebnismodul
                // fällt bei nicht passenden Bezeichnern auf die REIHENFOLGE
                // zurück, und die Modulzeilen entstehen in der Reihenfolge von
                // SimulationControl.BHKW_Liste_Laden bzw. SPK_Liste_Laden, die
                // beide ebenfalls ohne ORDER BY lesen. Eine Sortierung hier
                // könnte beide auseinanderlaufen lassen.
                "WHERE a.ID_Projekt = ? AND a.ID_Type = " +
                idType.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (streng) return StilleDb.TabelleStreng(sql, new DbParam("@p", idProjekt));
            try
            {
                using (DataRepository.EngineModus())
                    return DataRepository.GetDataTable(sql, new DbParam("@p", idProjekt));
            }
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannter Rückfall der STUFENLEITER — null heißt „diese
                // Fähigkeitsstufe ist nicht lesbar", LiesAnlagen versucht die nächste,
                // schmalere; die schmalste liest LiesAnlagen streng und nennt ihren Grund.
                return null;
            }
        }

        /// <summary>Die Fehler einer Umwandlung (Zahl, Datum) — ETAPPE E7c3 (B‑6): die
        /// einzigen, die die Lesehelfer dieser Klasse als „kein Wert" nehmen.</summary>
        internal static bool IstZahlfehler(Exception ex)
            => ex is FormatException || ex is InvalidCastException || ex is OverflowException;

        /// <summary>Datumsspalte einer Zeile; NULL, fehlende Spalte und Lesefehler ergeben
        /// <c>null</c> („kein eigener Wert" — dann gilt der Projektwert).</summary>
        private static DateTime? Datum(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDateTime(r[spalte]); }
            catch (Exception ex) when (IstZahlfehler(ex)) { return null; }   // E7c3 (B‑6): benannt
        }

        /// <summary>Ganzzahlspalte einer Zeile; NULL und Lesefehler ergeben 0.</summary>
        private static int Ganzzahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return 0;
            try { return Convert.ToInt32(r[spalte]); }
            catch (Exception ex) when (IstZahlfehler(ex)) { return 0; }   // E7c3 (B‑6): benannt
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
                    catch (Exception ex) when (IstZahlfehler(ex))
                    {
                        // ETAPPE E7c3 (B‑6): benannt — diese eine Zeile fällt aus der
                        // Zuordnung („0 = nicht ermittelbar, gilt als nicht ölbetrieben").
                    }
                }
            }
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannter Rückfall — leere Zuordnung (Tabelle fehlt
                // oder Abfrage scheitert): Der Heizöl-Ausschluss prüft dann über die
                // Gerätezeilen (BhkwMitHeizoel), deren Lesefehler die Ergebniszeile nennt.
                zuordnung.Clear();
            }
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
        private static bool BhkwMitHeizoel(int idProjekt, out string lesefehler)
        {
            lesefehler = null;
            try
            {
                // ETAPPE E7c3 (B‑6): der strenge Leseweg, damit der Fang unten greift.
                object o = StilleDb.ScalarStreng(
                    "SELECT COUNT(*) FROM Tab_BHKW AS b " +
                    "INNER JOIN Tab_Brennstoff_Stamm AS bs ON b.Brennstoff = bs.ID " +
                    "WHERE b.ID_Projekt = ? AND bs.ID_Kategorie = " + BRENNSTOFF_KATEGORIE_OEL,
                    new DbParam("@p", idProjekt));
                if (o != null && o != DBNull.Value) return Convert.ToInt32(o) > 0;
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt — „kein Öl" bleibt der Rückfall des
                // Ersatzwegs, der Grund steht an der Ergebniszeile.
                lesefehler = Fehlergrund.Text(ex);
            }
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
                      StartJahr = pos.StartJahr,     // KD6: Startjahr wandert mit (Sensitivität)
                      // E7c (Schritt E): die Kennzeichen der Position wandern mit.
                      ErsatzFuehren = pos.ErsatzFuehren, RestwertAnsetzen = pos.RestwertAnsetzen });

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
                // W5‑B‑8 (09.09.2026): Diese Bemessungsbasis ist seither die
                // Investitionskaskade (satzbasierte Zeilen und Prozentzeilen zählen
                // mit), nicht mehr die rohe Spaltensumme. Der Ausschlag skaliert damit
                // dieselbe Investition, die die Kachel und I₀ zeigen — die Modellannahme
                // selbst (linear im Faktor) ist unverändert.
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

            // ETAPPE E16 (V‑G3): Die Positionen „alle n Jahre" ziehen in der Sensitivität mit wie
            // ihre jährlichen Nachbarn desselben Topfes — der Energiefaktor den p_E-Anteil
            // (FX4-c), der Investitionsfaktor den investgekoppelten Anteil als additive
            // Korrektur (FX5-a). Bei Faktor 1,0 bzw. ohne Liste wird nichts kopiert.
            IList<KapitalwertRechner.Wiederholposten> wiederholt = e.Wiederholt;
            if (wiederholt != null && wiederholt.Count > 0 && (investFaktor != 1.0 || energieFaktor != 1.0))
            {
                var skaliert = new List<KapitalwertRechner.Wiederholposten>(wiederholt.Count);
                foreach (KapitalwertRechner.Wiederholposten w in wiederholt)
                {
                    double betrag = w.Betrag;
                    if (w.Endenergie && energieFaktor != 1.0) betrag = w.Betrag * energieFaktor;
                    else if (w.InvestGekoppelt && investFaktor != 1.0) betrag = w.Betrag + (investFaktor - 1.0) * w.Betrag;
                    skaliert.Add(w.MitBetrag(betrag));
                }
                wiederholt = skaliert;
            }

            // ETAPPE W5-B-12 (Anwenderentscheid 09.09.2026): der dritte Preisaenderungssatz
            // p_I. Er steht am ENDE der Signatur und kommt aus DEM Parametersatz, mit dem
            // dieser Lauf rechnet - fuer Best/Worst hat FuerSzenario ihn dort bereits
            // ersetzt, fuer Erwartet ist es der Projektwert (bzw. p_B, wenn p_I nicht
            // gepflegt ist). Die EINZIGE Aufrufstelle des Rechenkerns, also auch die
            // einzige, die den Satz durchreichen muss: Hauptlauf, Verlaufsdialog und
            // Sensitivitaet gehen alle hier durch.
            return KapitalwertRechner.Rechne(invest, betrieb,
                (e.Energie ?? 0) * energieFaktor, e.Erloes,
                zinsProzent, p.Betrachtungszeitraum,
                p.PreissteigerungBetrieb, preisstEnergie,
                e.Behg * energieFaktor, e.ErloesReihen, e.Zuschuss, behgReihe,
                betriebAbJahr, e.Endenergie * energieFaktor, endenergieAbJahr,
                p.PreisInvestWirksam,
                // ETAPPE E15 (V‑G7): der Risikoabzug dieses Standes — 0 ohne Risiko und für die
                // Referenz des Laufs; dann rechnet der Kern Zeichen für Zeichen wie vorher.
                e.Risikoabzug,
                // ETAPPE E16 (V‑G3): die Positionen „alle n Jahre" — leer im Bestand, dann
                // betritt der Kern den Zweig nicht.
                wiederholt);
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

            // ETAPPE E5 (V‑A, V‑G6): Jede stetige Zeile nennt ihren Ausschlag auch als
            // ZAHL (Schritt) — daraus bildet die Zeile ihre Steigung. Die Kapitalwerte
            // sind unverändert dieselben.
            var zeilenListe = new List<SensitivitaetZeile>
            {
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Zinssatz ±" + SENS_DELTA_ZINS.ToString("0.#") + " %-Pkt",
                    KwMinus = diff(z - SENS_DELTA_ZINS, pe, 1, 1), KwBasis = kwBasis,
                    KwPlus = diff(z + SENS_DELTA_ZINS, pe, 1, 1),
                    Schritt = SENS_DELTA_ZINS, SchrittInProzentpunkten = true },
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Energiepreissteigerung ±" + SENS_DELTA_PREIS.ToString("0.#") + " %-Pkt",
                    KwMinus = diff(z, pe - SENS_DELTA_PREIS, 1, 1), KwBasis = kwBasis,
                    KwPlus = diff(z, pe + SENS_DELTA_PREIS, 1, 1),
                    Schritt = SENS_DELTA_PREIS, SchrittInProzentpunkten = true },
                // PAKET FX5-a: Der Investitions-Ausschlag skaliert seit dem 03.09.2026
                // NICHT mehr nur die Investitionspositionen, sondern auch die davon
                // abgeleiteten Betriebskosten („x % der Investitionssumme") — die
                // Mitkopplung sitzt in RechneBild, die Zeile hier ist unverändert.
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Investition Variante ±" + SENS_DELTA_INVEST.ToString("0.#") + " %",
                    KwMinus = diff(z, pe, 1.0 - SENS_DELTA_INVEST / 100.0, 1), KwBasis = kwBasis,
                    KwPlus = diff(z, pe, 1.0 + SENS_DELTA_INVEST / 100.0, 1),
                    Schritt = SENS_DELTA_INVEST, SchrittInProzentpunkten = false },
                new SensitivitaetZeile { IdProjekt = idProjekt,
                    Parameter = "Energiekosten Variante ±" + SENS_DELTA_ENERGIE.ToString("0.#") + " % (inkl. CO₂-Abgabe)",
                    KwMinus = diff(z, pe, 1, 1.0 - SENS_DELTA_ENERGIE / 100.0), KwBasis = kwBasis,
                    KwPlus = diff(z, pe, 1, 1.0 + SENS_DELTA_ENERGIE / 100.0),
                    Schritt = SENS_DELTA_ENERGIE, SchrittInProzentpunkten = false }
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
        /// AUFTRAG U17 — der Betrag der Pauschale nach § 9 KWKG [€] dieses Laufs; 0 =
        /// sie greift nicht.
        ///
        /// <para><b>Gelesen, nicht nachgerechnet.</b> Die Zahl steht bereits im INDEX 0
        /// der Erlösreihe <c>KWKG_PAUSCHALE</c>, die <see cref="PauschaleReihe"/>
        /// gebildet und der <c>KapitalwertRechner</c> unabgezinst auf den Startwert
        /// gebucht hat. Sie hier ein zweites Mal aus Satz, Vbh und Nennleistung zu
        /// bilden, wäre eine zweite Rechenstelle, die auseinanderlaufen kann — der
        /// AUSWEIS muss zeigen, was der KAPITALWERT verwendet hat.</para>
        /// </summary>
        private static double PauschaleBetrag(ProjektEingabe e)
        {
            if (e == null || e.ErloesReihen == null) return 0;
            foreach (KapitalwertRechner.ErloesReihe r in e.ErloesReihen)
                if (r != null && string.Equals(r.Name,
                        KapitalwertRechner.ErloesReihe.KWKG_PAUSCHALE, StringComparison.Ordinal))
                    return r.Wert(0);
            return 0;
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
                // E15: der Risikoabzug MUSS mitkopiert werden — dieselbe Begründung wie beim
                // Zuschuss: Sonst enthielte die ausgewiesene Differenz den Abzug.
                Risikoabzug = e.Risikoabzug,
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
                // ETAPPE E16: die Positionen „alle n Jahre" gehören zur vollständigen Kopie —
                // dieselbe Begründung wie beim Endenergie-Topf.
                Wiederholt = e.Wiederholt,
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
        private static double? PvVermiedenerBezugAusweis(VariantenDaten v, string szenario)
        {
            try
            {
                if (v.Ergebnis == null || v.Ergebnis.Photovoltaik == null) return null;
                double evMWh = v.Ergebnis.Photovoltaik.Stromproduktion
                             - v.Ergebnis.Photovoltaik.Ueberschuss;
                if (evMWh <= 0.0005) return null;
                // ETAPPE E9a: Menge (v trägt den Mengenfaktor) und Preis des Szenarios.
                double? preis = StromArbeitspreisEurJeKwh(v.IdProjekt, szenario);
                if (!preis.HasValue) return null;
                return evMWh * 1000.0 * preis.Value;
            }
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannt — reiner Ausweis, KEIN Bestandteil des
                // Kapitalwerts; null zeigt „—" statt einer Zahl.
                return null;
            }
        }

        /// <summary>
        /// AUFTRAG U6 — der VERTEILSCHLÜSSEL der vermiedenen Stromkosten: je Komponente
        /// ihr Eigenverbrauch [MWh/a], daraus die Anteile.
        ///
        /// <para><b>Die Näherung V‑4, ausgewiesen (Entscheid A12).</b> Die Strommatrix
        /// trennt nicht nach Anlage (Befund R8); modulscharfe
        /// Stundenreihen gibt es im Modell nicht. Der Eigenverbrauch je Modul kommt
        /// deshalb aus demselben Modulnachweis, mit dem der KWKG-Rechner seine Mengen
        /// gebildet hat — bei genau einem Modul exakt, bei mehreren eine Annahme.
        /// Mehrere Module werden zu EINER Komponentenzeile zusammengefasst: Die
        /// Rubrikzeilen darüber (Zuschlag, § 53, Befreiung) sind ebenfalls Summen über
        /// alle Module, und zwei verschiedene Schnitte im selben Block wären zwei
        /// Wahrheiten.</para>
        ///
        /// <para><b>ETAPPE E7 — BEIDE ANLAGEN, BEIDE BRUTTO</b> (Konzept § 6.3 Nr. 32,
        /// Entscheid U6‑Q1 vom 22.09.2026: „der Verteilschlüssel bringt beide Anlagen
        /// ein"). Die vermiedene Menge ist seit E7 „Bedarf ohne JEDE Eigenerzeugung minus
        /// Restbezug" und trägt damit KWK- UND PV-Eigenverbrauch. Der Schlüssel nimmt
        /// beide aus der STROMMATRIX, derselben Brutto-Welt, aus der die Menge selbst
        /// entsteht: das Blockheizkraftwerk mit seinem Eigenverbrauch nach der min-Regel
        /// (<see cref="StromMatrix.KwkEigenGesamtMWh"/>), die Photovoltaik mit ihrer
        /// Eigennutzung (<see cref="StromMatrix.PvEigenGesamtMWh"/>). Der Hilfsstrom
        /// berührt diese Menge nicht — er mindert allein die KWKG-Mengen (Mockup
        /// Kategorie 7, Orchestrator-Entscheid 23.09.2026 nach Empfehlung des Mockups).
        /// Ohne Speicher ist der Schlüssel damit exakt: Jede Anlage bekommt genau ihren
        /// Eigenverbrauch. Bis E7 kam der Schlüssel des Blockheizkraftwerks aus dem
        /// Modulnachweis (NETTO, nach Hilfsstrom); allein war er gleichgültig, neben der
        /// Photovoltaik nicht mehr.</para>
        ///
        /// <para>Der Modulnachweis liefert weiter den ANLAGENNAMEN: Er steht, wenn genau
        /// ein Modul Eigenverbrauch trägt.</para>
        ///
        /// <para>Leer = kein Eigenverbrauch bestimmbar; dann bleibt die Rubrik bei der
        /// einen projektweiten Kette.</para>
        /// </summary>
        private static List<VermiedenAnlageNachweis> VermiedenAufteilung(
            ProjektEingabe eingabe, WirtschaftlichkeitErgebnis erg)
        {
            if (eingabe == null) return new List<VermiedenAnlageNachweis>();
            return VermiedenAufteilung(eingabe.KwkgModule, eingabe.Matrix, erg);
        }

        /// <summary>
        /// Der Verteilschlüssel aus Modulnachweis und Strommatrix des Laufs — die
        /// Rechnung hinter <see cref="VermiedenAufteilung(ProjektEingabe, WirtschaftlichkeitErgebnis)"/>,
        /// ohne die private Eingabe des Laufs und deshalb für den Nachweis erreichbar.
        /// </summary>
        internal static List<VermiedenAnlageNachweis> VermiedenAufteilung(
            IList<KwkgModulNachweis> kwkgModule, StromMatrix matrix, WirtschaftlichkeitErgebnis erg)
        {
            var leer = new List<VermiedenAnlageNachweis>();
            if (erg == null) return leer;
            if (erg.VermiedenMengeMWh <= 0 && erg.VermiedenArbeitJahr == 0) return leer;

            // Der Modulnachweis liefert nur noch den Namen — genau dann, wenn EIN Modul
            // Eigenverbrauch trägt; bei mehreren ist die Zeile die Technik.
            string name = "";
            int module = 0;
            if (kwkgModule != null)
                foreach (KwkgModulNachweis n in kwkgModule)
                {
                    if (n == null || n.EigenMWh <= 0) continue;
                    module++;
                    if (module == 1) name = n.Bezeichner ?? "";
                }

            // ETAPPE E7 — beide Schlüssel BRUTTO aus der Strommatrix: der
            // KWK-Eigenverbrauch nach der min-Regel und die PV-Eigennutzung, soweit sie
            // Bedarf deckt. Ohne Matrix gibt es keine vermiedene Menge und damit nichts
            // zu verteilen.
            double kwkEigenMWh = matrix != null ? matrix.KwkEigenGesamtMWh : 0;
            double pvEigenMWh = matrix != null ? matrix.PvEigenGesamtMWh : 0;

            var schluessel = new List<VermiedenAnlageNachweis>();
            if (kwkEigenMWh > 0)
                schluessel.Add(new VermiedenAnlageNachweis
                {
                    Komponente = WirtZeile.KOMPONENTE_BHKW,
                    // Der Anlagenname steht nur, wenn er EINE Anlage meint; bei mehreren
                    // Modulen ist die Zeile die Technik, nicht das Gerät.
                    Anlage = module == 1 ? name : "",
                    EigenMWh = kwkEigenMWh
                });
            if (pvEigenMWh > 0)
                schluessel.Add(new VermiedenAnlageNachweis
                {
                    Komponente = WirtZeile.KOMPONENTE_PV,
                    Anlage = "",                 // Sammelzeile der Technik
                    EigenMWh = pvEigenMWh
                });
            if (schluessel.Count == 0) return leer;

            return VermiedenAnlageNachweis.Verteile(schluessel, erg.VermiedenMengeMWh,
                                                    erg.VermiedenArbeitJahr,
                                                    erg.VermiedenEntlastung9bJahr);
        }

        /// <summary>Arbeitspreis Strom [€/kWh] des Projekt-Stromträgers — des Trägers, der in den
        /// Energiekosten den Netzbezug bepreist, nicht der abweichende Kühlträger einer Wärmepumpe
        /// (E34, E35); null = kein Träger oder kein Preis gepflegt.</summary>
        internal static double? StromArbeitspreisEurJeKwh(int idProjekt)
        {
            return StromArbeitspreisEurJeKwh(idProjekt, null);
        }

        /// <summary>
        /// ETAPPE E9a (Schritt C) — derselbe Arbeitspreis des Stromträgers im SZENARIO: Ein
        /// gepflegter Szenario-Arbeitspreis des Trägers ersetzt den Erwartet-Preis nach
        /// derselben Regel wie in den Energiekosten (<see cref="TraegerpreisSzenario.Wirksam"/>).
        /// <paramref name="szenario"/> = <c>null</c> oder ERWARTET ist der Weg von vor E9a.
        /// </summary>
        internal static double? StromArbeitspreisEurJeKwh(int idProjekt, string szenario)
        {
            try
            {
                // STUFE KU2 WELLE 4 (Kühlkonzept 6.2; Entscheide E34, E35) — KEINE EIGENE
                // PREISABFRAGE MEHR. Hier stand eine eigene Abfrage, die IRGENDEINEN dem Projekt
                // zugeordneten Stromträger las (LIMIT 1) und dessen Preis ohne Preisstand. Seit
                // ein Projekt für die Kühlung einen zweiten Stromträger führen kann (K9, E33),
                // konnte das der Kühlträger sein: Der vermiedene Bezug der Photovoltaik — die nie
                // Kältestrom eines eigenen Zählers deckt — und ihr Mehrbezug durch Degradation
                // trugen dann den Preis der Kühlung. Jetzt ist es der Stromträger, der den
                // Netzbezug in den Energiekosten bepreist (Kaeltestromabrechnung.Projekttraeger,
                // samt Rückfall auf den Auslieferungsträger), mit dem Arbeitspreis aus derselben
                // Vorrangkette (KostenEmissionRechner.ArbeitspreisJeKwh: Projektwert →
                // Preisstand → Katalog, im Szenario der wirksame Szenariopreis, E9a) — eine
                // Wahrheit statt zwei.
                int traeger = Kaeltestromabrechnung.Projekttraeger(idProjekt);
                if (traeger <= 0) return null;
                return KostenEmissionRechner.ArbeitspreisJeKwh(idProjekt, traeger, szenario);
            }
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannt — null heißt „kein Arbeitspreis": der Ausweis
                // bleibt leer; die Degradation der PV-Reihe rechnet dann ohne Mehrbezug.
                return null;
            }
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
            // ETAPPE E16 (E16‑Q3 a): Die Jahr-1-Zahl bleibt die Jahr-1-Zahl — eine Position
            // „alle n Jahre", die im ersten Jahr zahlt, gehört dazu, eine mit späterem Startjahr
            // nicht. Ohne solche Position wird nichts addiert (bitgleich).
            if (eingabe.Wiederholt != null && eingabe.Wiederholt.Count > 0)
                erg.BetriebskostenJahr += ErstesJahr(eingabe.Wiederholt);
            erg.EnergiekostenJahr = eingabe.Energie;
            erg.EinspeiseerloesJahr = eingabe.Erloes;
            erg.CO2AbgabeJahr = eingabe.Behg;                 // W2: BEHG
            erg.KwkgErloesJahr1 = eingabe.KwkgJahr1;          // W2/W3: KWKG
            erg.KwkgVbhElektrisch = eingabe.VbhElektrisch;    // E2: Bezugsgröße der Deckelung
            erg.KwkgPauschaleEur = PauschaleBetrag(eingabe);  // U17: Einmalzahlung Jahr 0
            erg.EnergiesteuerJahr1 = eingabe.EnergiesteuerJahr1;              // E4
            // AUFTRAG U7 — ein frisch gerechneter Lauf kennt die Aufteilung IMMER,
            // auch wenn beide Beträge 0 sind (kein BHKW, nichts gewählt). Der Merker
            // unterscheidet „zweimal 0 gerechnet" von „nicht aufgeteilt gebucht".
            erg.EnergiesteuerAufgeteilt = true;
            erg.Energiesteuer53Jahr1 = eingabe.Energiesteuer53Jahr1;
            erg.Energiesteuer54Jahr1 = eingabe.Energiesteuer54Jahr1;
            erg.Energiesteuer54SockelJahr1 = eingabe.Energiesteuer54SockelJahr1;
            erg.EnergiesteuerNachweise = eingabe.EnergiesteuerNachweise;
            erg.EnergiesteuerVorschau = eingabe.EnergiesteuerVorschau;         // E7c3 (Q8 b)
            erg.PositionsGruende = eingabe.PositionsGruende;                 // 9d
            erg.StromsteuerBefreiungJahr1 = eingabe.StromsteuerBefreiungJahr1;
            erg.StromsteuerBefreiungAlsErloes = eingabe.StromsteuerBefreiungAlsErloes;   // B6
            erg.StromsteuerEntlastungJahr1 = eingabe.StromsteuerEntlastungJahr1;
            erg.SteuerHerkunft = eingabe.SteuerHerkunft;
            erg.StromkostenTarif = eingabe.StromkostenTarif;  // Rollentarif (Reststrom)
            erg.BezugsspitzeKW = v.BezugsspitzeKW;            // SP-W1: Herleitungszeile
            erg.VermiedenArbeitJahr = eingabe.VermiedenArbeit;        // E5
            erg.VermiedenLeistungJahr = eingabe.VermiedenLeistung;
            erg.VermiedenGesamtJahr = eingabe.VermiedenGesamt;
            // SP-E-2: Es gibt keinen Aufschlagsbetrag mehr — die Preisanteile zerlegen
            // den Arbeitspreis und stecken damit in den Energiekosten. Die 0 bleibt
            // stehen, weil die Spalte Tab_Ergebnis.AufschlagBetrag bleibt: Der
            // Referenzexport liest die Ergebnistabellen per SELECT *, und eine Spalte
            // ohne Wert wäre dort NULL statt 0.
            erg.AufschlagJahr = 0.0;
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
                erg.PvVermiedenerBezug = PvVermiedenerBezugAusweis(v, szenario);
                // KONZEPT § 2.16: die Herkunft der Verguetung - "eigene Werte" oder
                // "uebernommen von <Stamm>". Sie reist im Nachweisumschlag mit, damit
                // der Bericht sie auch beim GEBUCHTEN Stand nennen kann.
                erg.PvVerguetungUebernommen = eingabe.PvVerguetungUebernommen;
                erg.PvVerguetungQuelle = eingabe.PvVerguetungQuelle ?? "";
            }
            erg.KwkgModule = eingabe.KwkgModule;
            erg.Betriebskosten = eingabe.Betriebskosten;
            // ETAPPE B7 (Konzept § 3.5): die Energiekosten je Anlage — gebildet vom
            // KostenEmissionRechner aus denselben Mengen und Preisen, aus denen
            // erg.EnergiekostenJahr entstanden ist. Nur Ausweis; seit B7P mit dem Lauf
            // gebucht (ErgebnisNachweisUmschlag).
            if (v.EnergiekostenJeAnlage != null)
                erg.EnergiekostenJeAnlage = v.EnergiekostenJeAnlage;

            // ETAPPE B7 (Konzept § 2.6, Klarstellung 1) — die § 9b-KORREKTUR des
            // AUSWEISES. Sie ruehrt den Kapitalwert nicht an: Angehaengt wird keine
            // Reihe, veraendert wird keine; berechnet wird allein, um wie viel der
            // ausgewiesene Vorteil zu hoch stuende, wenn die entgangene Entlastung
            // fehlte. Die Bemessungsgroesse ist die vermiedene MENGE, nicht der
            // Netzbezug — nur sie ist die Menge, die beide Seiten der Differenz
            // unterscheidet.
            //
            // Die Unternehmensart kommt aus derselben Eingabe, mit der die
            // Steuerrechnung gerechnet hat (B2), und die Pruefung ist dieselbe Funktion
            // (SteuerGutschriftRechner.ProduzierendesGewerbe) — eine zweite Fassung
            // waere eine zweite Antwort auf dieselbe Frage.
            erg.VermiedenMengeMWh = eingabe.VermiedenMengeMWh;
            if (eingabe.SteuerEingabe != null &&
                SteuerGutschriftRechner.ProduzierendesGewerbe(eingabe.SteuerEingabe))
            {
                erg.ProduzierendesGewerbe = true;
                if (eingabe.VermiedenMengeMWh > 0)
                {
                    if (_gesetze == null) _gesetze = new GesetzKatalog();
                    double? satz = _gesetze.Wert(DbWerte.GESETZ_STROMST_ENTLASTUNG_9B,
                                                 Foerderbeginn(p));
                    if (satz.HasValue && satz.Value > 0)
                        erg.VermiedenEntlastung9bJahr = satz.Value * eingabe.VermiedenMengeMWh;
                }
            }

            // AUFTRAG U6 (Anwenderentscheid Q15/A12 vom 22.09.2026) — die AUFTEILUNG
            // der vermiedenen Stromkosten auf die Anlagen. Sie entsteht HIER, im Kern,
            // einmal: Rubrik, Wort- und Excelbericht und die BHKW-Vorschau lesen sie,
            // statt sie je selbst zu bilden.
            //
            // Verteilt wird der ARBEITSanteil samt der auf ihn entfallenden entgangenen
            // § 9b-Entlastung; der LEISTUNGSanteil bleibt projektweit (Q15) — er haengt
            // an der Bezugsspitze des ganzen Projekts.
            //
            // ETAPPE E7 (Konzept § 6.3 Nr. 32, Entscheid U6-Q1): Die vermiedene Menge
            // ist „Bedarf OHNE JEDE Eigenerzeugung minus Restbezug" — StromMatrix.Baue
            // zieht die PV-Eigennutzung nicht mehr vorab ab. Die Menge traegt damit KWK-
            // UND PV-Eigenverbrauch, die § 9b-Korrektur oben greift auf beide, und der
            // Schluessel bringt beide Anlagen ein — beide brutto aus der Strommatrix.
            // Die Ausweiszeile PvVermiedenerBezug (Flat-Preis) wird weiter gerechnet und
            // gespeichert; die RUBRIK zeigt sie nur, wo die Aufteilung keinen PV-Anteil
            // traegt (WirtschaftlichkeitZeilen.PvVermiedenFlat).
            erg.VermiedenJeAnlage = VermiedenAufteilung(eingabe, erg);

            erg.Hinweis = eingabe.Hinweis;

            // Trägerzuordnungs-Etappe: Fiel die Emissionsrechnung mangels zugeordnetem
            // Strom-Energieträger auf den Strommix-Vorgabewert zurück (Flag aus
            // KostenEmissionRechner), gehört das ausgewiesen — sonst steht im Ergebnis
            // eine CO₂-Bilanz, deren Bezugsgröße niemand erfasst hat.
            //
            // ETAPPE E2 (Befund R6): Die Zeile ist keine Laufbemerkung mehr, sondern
            // eine KOHÄRENZZEILE MIT WERT (§ 3.9 des Konzepts führt sie dort). Sie wird
            // unten an KohaerenzPruefung übergeben und erreicht damit dieselben drei
            // Ausgaben wie jede andere Kohärenzzeile — Rubrik, Wort- und Excelbericht.



            // AUFTRAG #267: Dieselbe Lage auf der KOSTENseite — der Netzbezug wurde mit
            // dem Auslieferungsträger des Katalogs bepreist, weil das Projekt keinen
            // Stromträger führt. Gerechnet wird damit (sonst gäbe es gar keine Zahl),
            // gesagt wird es hier.
            if (!string.IsNullOrEmpty(v.StromTraegerRueckfall))
                erg.Hinweis = Anhaengen(erg.Hinweis, string.Format(
                    T("WIRT_STROMTRAEGER_RUECKFALL",
                      KostenEmissionRechner.HINWEIS_STROMTRAEGER_RUECKFALL),
                    v.StromTraegerRueckfall));

            // ANWENDERENTSCHEID 15.09.2026 — dieselbe Lage auf der CO₂-Seite, und
            // anders als oben MIT Zahl: Der Netzbezug wurde mit dem Emissionsfaktor
            // des Auslieferungsträgers gerechnet, weil das Projekt keinen Stromträger
            // führt. Eine geliehene Zahl sieht aus wie eine gepflegte — diese Zeile
            // macht den Unterschied sichtbar. Sie steht NEBEN der Strommix-Zeile
            // darüber, nie zugleich mit ihr: Entweder trug der Rückfall den Faktor,
            // oder es blieb beim Vorgabewert.
            if (!string.IsNullOrEmpty(v.CO2TraegerRueckfall))
                erg.Hinweis = Anhaengen(erg.Hinweis, string.Format(
                    T("WIRT_CO2_TRAEGER_RUECKFALL",
                      KostenEmissionRechner.HINWEIS_CO2_TRAEGER_RUECKFALL),
                    v.CO2TraegerRueckfall));

            // ANWENDERENTSCHEID 22.09.2026 — STROMBEDARF OHNE VERWENDUNG. Das Projekt
            // führt einen Netzbezug, aber keinen Erzeuger, der Strom verwendet: Energiekosten
            // und Emissionen sind dann OHNE diesen Strom bestimmt (KostenEmissionRechner,
            // Regel ProjektEnergietraegerCtrl.StromOhneVerwendung). Die Rückfallzeilen
            // darüber stehen in dieser Lage nie — bepreist und bewertet wurde nichts.
            // Eine WARNUNG, kein Fehlgrund — die Zahl steht, nur nicht die Stromseite.
            // Sie reist denselben Weg wie die beiden Rückfallzeilen darüber und erreicht
            // damit Warnband, Vergleichstabelle, Wort- und Excelbericht.
            if (v.StrombedarfOhneVerwendungMWh.HasValue)
                erg.Hinweis = Anhaengen(erg.Hinweis, string.Format(
                    T("WIRT_HINWEIS_STROMBEDARF_OHNE_VERWENDUNG",
                      KostenEmissionRechner.HINWEIS_STROMBEDARF_OHNE_VERWENDUNG),
                    v.StrombedarfOhneVerwendungMWh.Value.ToString("N1", BerichtTexte.Kultur)));

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
                    EnergiesteuerEur = eingabe.EnergiesteuerJahr1,
                    StromsteuerBefreiungEur = eingabe.StromsteuerBefreiungJahr1,
                    StromsteuerBefreiungAlsErloes = eingabe.StromsteuerBefreiungAlsErloes,   // B6
                    StromsteuerEntlastungEur = eingabe.StromsteuerEntlastungJahr1,
                    // ETAPPE E2 (R5): die gebuchte CO₂-Abgabe des Jahres 1 — sie ist der
                    // Betrag, der bei aktivem CO₂-Bestandteil im Arbeitspreis ZWEIMAL
                    // in den Energiekosten steht.
                    Co2AbgabeEur = eingabe.Behg,
                    // ETAPPE E2 (R6): der Strommix-Rückfall als Zeile MIT Wert.
                    StrommixRueckfallGJeKwh = v.CO2StrommixRueckfall
                        ? KostenEmissionRechner.STROMMIX_CO2_G_JE_KWH : (double?)null,
                    // ETAPPE E7c: die Datenlücken der KWKG-Rechnung (Anlagenart fehlt,
                    // Stromkennzahl fehlt) — festgehalten, wo sie den Zuschlag kosteten.
                    Kwkg = eingabe.KwkgLuecken
                });
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): Der Fangzaun bleibt — kein Kapitalwert kippt an der
                // Prüfung —, aber sie fehlt nicht mehr still: Die Zeile nennt den Grund.
                // (Die zehn Teilprüfungen fangen ihre Fehler schon selbst; hier bleibt
                // der Aufbau des Laufs.)
                Stufenfehler(v.IdProjekt, STUFE_KOHAERENZ, Fehlergrund.Text(ex));
            }
            // ETAPPE E9a (E9a‑Q3, E9a‑Q7): gepflegte Szenariowerte ohne Wirkung als HINWEIS —
            // der Szenario-Leistungspreis neben Staffel oder Saisonreihe, Strompreis und
            // Einspeisevergütung im Rollenmodell, die flache PV-Vergütung neben dem
            // Vergütungsdialog. Ohne Pflege entsteht keine Zeile.
            SzenarioHinweiseAnhaengen(erg, eingabe, v);
            // ETAPPE E7c3 (B‑6): die gescheiterten Rechenstufen dieses Laufs als WARNUNG.
            if (_gesetze != null && _gesetze.Lesefehler != null)
                Stufenfehler(0, STUFE_KATALOG, _gesetze.Lesefehler);
            StufenfehlerAnhaengen(erg);
            foreach (KapitalwertRechner.InvestPosition pos in eingabe.Investitionen)
                erg.Investition += pos.Betrag;

            if (!eingabe.Energie.HasValue)
            {
                // Ohne Energiekosten fehlt der größte Posten — Kennzahlen bleiben „—".
                //
                // AUFTRAG #267: Der GRUND kommt aus dem Rechner, der ihn kennt
                // (KostenEmissionRechner setzt v.EnergiekostenGrund an genau der
                // Stelle, an der er die Auskunft verweigert). Der pauschale Satz
                // darunter bleibt nur als Rückfall für Stände ohne Grund — er hat den
                // Anwenderbefund vom 14.09.2026 mit verursacht, weil er
                // „Arbeitspreise prüfen" sagte, während in Wahrheit der Wärmepumpe
                // kein Stromträger zugeordnet war.
                erg.Fehlgrund = !string.IsNullOrEmpty(v.EnergiekostenGrund)
                    ? v.EnergiekostenGrund
                    : "Energiekosten nicht bestimmbar — Arbeitspreise/Träger in der " +
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
            // ETAPPE W5-B-10 (VALERI): der Barwert der Ersatzbeschaffungen als eigener
            // Ausweis. Er steckt in BarwertAusgaben bereits drin und wird nirgends
            // addiert - er wird nur aus dem fertigen Zahlungsbild abgelesen.
            erg.ErsatzBarwert = ErsatzBarwert(bild, p.Zinssatz);
            erg.Kapitalwert = bild.Kapitalwert;

            // Wärmegestehungskosten: annuisierte Nettokosten ÷ Jahreswärmebedarf.
            if (eingabe.WaermeMWh > 0)
            {
                double a = KapitalwertRechner.Annuitaet(p.Zinssatz / 100.0, p.Betrachtungszeitraum);
                erg.Gestehungskosten = (-bild.Kapitalwert * a) / (eingabe.WaermeMWh * 1000.0);
            }
            return erg;
        }

        /// <summary>
        /// ETAPPE E9a — die Kohärenzzeilen des Szenariolaufs an ein Ergebnis hängen: je
        /// Träger, dessen Szenario-Leistungspreis neben einer Staffel oder Saisonreihe ohne
        /// Wirkung blieb, eine Zeile; dazu die Zeilen, die die Eingabe gesammelt hat
        /// (Rollenmodell, Vergütungsdialog). Schwere HINWEIS — gerechnet ist richtig, nur ohne
        /// den gepflegten Wert. Jede Zeile einmal.
        /// </summary>
        private static void SzenarioHinweiseAnhaengen(WirtschaftlichkeitErgebnis erg, ProjektEingabe eingabe,
                                                      VariantenDaten v)
        {
            var zeilen = new List<string>();
            if (v != null && v.SzenarioLeistungspreisOhneWirkung != null)
                foreach (string traeger in v.SzenarioLeistungspreisOhneWirkung)
                    zeilen.Add(string.Format(BerichtTexte.Kultur,
                        T("WIRT_SZ_LEISTUNGSPREIS_OHNE_WIRKUNG",
                          "Szenario-Leistungspreis des Energieträgers „{0}“ ohne Wirkung: Die " +
                          "gepflegte Leistungspreis-Staffel bzw. saisonale Leistungspreisreihe des " +
                          "Trägers gilt auch in diesem Szenario."),
                        traeger));
            if (eingabe != null && eingabe.SzenarioHinweise != null)
                zeilen.AddRange(eingabe.SzenarioHinweise);
            if (zeilen.Count == 0) return;

            if (erg.KohaerenzHinweise == null) erg.KohaerenzHinweise = new List<KohaerenzHinweis>();
            foreach (string z in zeilen)
            {
                bool schon = false;
                foreach (KohaerenzHinweis h in erg.KohaerenzHinweise)
                    if (string.Equals(h.Text, z, StringComparison.Ordinal)) { schon = true; break; }
                if (!schon)
                    erg.KohaerenzHinweise.Add(new KohaerenzHinweis { Schwere = KohaerenzSchwere.HINWEIS, Text = z });
            }
        }

        /// <summary>
        /// ETAPPE W5‑B‑10 (VALERI): Barwert der Ersatzbeschaffungen [€] aus dem fertigen
        /// Zahlungsbild. Die Reihe ist nominal und beginnt bei Index 1; Index 0 trägt in
        /// den Zahlungsreihen die Erstinvestition und gehört nicht zum Ersatz.
        /// <para><b>Reine Ableitung</b> — sie ändert am Kapitalwert nichts und wird
        /// nirgends aufsummiert.</para>
        /// </summary>
        private static double ErsatzBarwert(KapitalwertRechner.Zahlungsbild bild, double zinsProzent)
        {
            if (bild == null || bild.ErsatzJeJahr == null) return 0;
            double i = zinsProzent / 100.0;
            double summe = 0;
            for (int t = 1; t < bild.ErsatzJeJahr.Length; t++)
                if (bild.ErsatzJeJahr[t] != 0)
                    summe += bild.ErsatzJeJahr[t] / Math.Pow(1 + i, t);
            return summe;
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
            return LiesInvestitionen(idProjekt, szenario, null, out zuschuss);
        }

        /// <summary>
        /// ETAPPE W5‑B‑9 (Anwenderentscheid 09.09.2026): dieselbe Leselogik, aber mit dem
        /// <b>Szenario-Parametersatz</b>.
        ///
        /// <para><paramref name="satz"/> = <c>null</c> heißt „kein pauschaler Ausschlag“
        /// und ist der Weg des Szenarios ERWARTET sowie jeder Anzeige, die erfasste
        /// Zahlen zeigt (Kostenseite, Dialog Kostenverwaltung). Dann ist diese Fassung
        /// Zeichen für Zeichen die von vor W5‑B‑9.</para>
        ///
        /// <para><b>Vorrangregel.</b> Der Ausschlag greift <b>je Zeile</b> und nur dort,
        /// wo <see cref="InvestKaskade.Zeile.WertGepflegt"/> bzw.
        /// <c>DauerGepflegt</c> <c>false</c> ist — ein gepflegter Best-/Worst-Wert
        /// schlägt den pauschalen Satz (sonst Doppelzählung). Dass eine %-Zeile ihre
        /// Basis bereits unskaliert bezogen hat, macht keinen Unterschied: Sie wird
        /// selbst skaliert, und das ist wertgleich zum Skalieren ihrer Basis.</para>
        ///
        /// <para><b>Zuschusszeilen bleiben außen vor</b> — dieselbe Begründung wie in
        /// der Sensitivität (<c>RechneBild</c>, K5): Eine bewilligte Förderzusage über
        /// einen festen Betrag ändert sich nicht, weil die Anlage 10 % mehr kostet. Ihre
        /// eigenen Best-/Worst-Spalten gelten weiterhin.</para>
        /// </summary>
        internal static List<KapitalwertRechner.InvestPosition> LiesInvestitionen(
            int idProjekt, string szenario, SzenarioSatz satz, out double zuschuss)
        {
            var liste = new List<KapitalwertRechner.InvestPosition>();
            zuschuss = 0;

            // ANWENDERBEFUND W5-B-7 (08.09.2026): Die Kaskade selbst steht seither in
            // InvestKaskade — Wort für Wort dieselbe, nur an einem Ort. Der Dialog
            // Kostenverwaltung und die Anlagentabelle der Kostenseite lesen sie
            // ebenfalls dort; vorher hatte jede der drei Stellen ihren eigenen Weg und
            // damit ihre eigene Zahl. Hier bleibt genau das, was die Kapitalwertrechnung
            // eigen ist: der K5-Zuschussabzug und die Übersetzung in InvestPosition.
            foreach (InvestKaskade.Zeile z in InvestKaskade.Lies(idProjekt, szenario))
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

                // ETAPPE W5-B-9: der pauschale Szenarioausschlag - NUR auf Zeilen ohne
                // gepflegten Szenariowert. Ohne Satz (ERWARTET, Anzeigen) wird der Zweig
                // gar nicht betreten, und die zwei Zuweisungen sind wertgleich zum
                // Bestand.
                // ETAPPE W5-B-11 (09.09.2026): Die BETRAGSregel steht seither in
                // InvestKaskade.BetragImSzenario - dieselbe Methode, die jetzt auch die
                // Bemessungsbasis der Prozent-Betriebskosten bildet (G11). Der Rechenweg
                // ist unveraendert (z.Betrag * InvestFaktor, ohne Satz gar nichts).
                double betrag = InvestKaskade.BetragImSzenario(z, satz);
                double dauer = z.Dauer;
                if (satz != null && !z.DauerGepflegt) dauer = satz.DauerFuer(dauer);

                liste.Add(new KapitalwertRechner.InvestPosition
                {
                    Betrag = betrag,
                    Nutzungsdauer = dauer,
                    StartJahr = z.Start,
                    // ETAPPE E7c (Schritt E, Entscheid A6): die zwei Kennzeichen der
                    // Position; null (der ganze Bestand) rechnet wie bisher.
                    ErsatzFuehren = z.ErsatzFuehren,
                    RestwertAnsetzen = z.RestwertAnsetzen
                });
            }
            return liste;
        }

        /// <summary>„x % der Investitionssumme" (H4b auf der Investseite, H4a auf der
        /// Betriebsseite).
        /// <para><b>PAKET FX5-a:</b> Dasselbe Prädikat entscheidet seit dem
        /// 03.09.2026 auch, welche KATEGORIE-2-Zeile in den investgekoppelten Ausweis
        /// von <see cref="LiesBetriebskostenTopfe"/> geht — die Zeile, die der
        /// Sensitivitäts-Investitionsfaktor mitzieht.</para></summary>
        internal static bool IstProzentInvest(string bem)
        {
            return string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal);
        }

        /// <summary>Einmal je Prozess geprüft: existiert <c>Tab_ProjektWerte.StartJahr</c>
        /// (Migrationsschritt 38)? Auf älteren Datenbanken bleibt alles t0.</summary>
        private static bool? _startjahrSpalte;

        internal static bool StartjahrSpalteVorhanden()
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

        internal static bool AnlagenSpalteVorhanden()
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
        internal static int StartJahrDerZeile(DataRow r)
        {
            try
            {
                if (!r.Table.Columns.Contains(SchemaKatalog.SPALTE_PW_STARTJAHR)) return 0;
                object o = r[SchemaKatalog.SPALTE_PW_STARTJAHR];
                if (o == null || o == DBNull.Value) return 0;
                int j = Convert.ToInt32(o);
                return j > 1 ? j : 0;
            }
            catch (Exception ex) when (ex is ArgumentException || IstZahlfehler(ex))
            {
                // ETAPPE E7c3 (B‑6): benannt — fehlende Spalte oder keine Zahl heißt
                // „0 = ab t0", wie eine leere Zelle.
                return 0;
            }
        }

        /// <summary>
        /// true, wenn die Zeile die Kostenart „Zuschuss" trägt (K5).
        ///
        /// <para><b>ETAPPE E2 (Befund I-5) — ZEICHENGENAU.</b> Bis hierher verglich
        /// allein diese Stelle <c>OrdinalIgnoreCase</c>, während dieselbe Frage im
        /// Betriebskostenpfad als SQL-Gleichheit gestellt wird
        /// (<c>BetriebskostenCtrl</c>, Parameter <c>@art</c>) — und SQLite vergleicht
        /// TEXT zeichengenau. Eine Zeile mit abweichender Schreibweise wäre hier ein
        /// Zuschuss gewesen und dort keiner: zwei Antworten auf eine Frage. Steuerwerte
        /// sind eingefrorene ASCII-Schlüssel (Drei-Schichten-Regel) und werden im
        /// ganzen Kern zeichengenau verglichen — 78 von 79 Stellen taten das bereits.</para>
        /// </summary>
        internal static bool IstZuschuss(DataRow r)
        {
            try
            {
                if (!r.Table.Columns.Contains(SchemaKatalog.SPALTE_PW_KOSTENART)) return false;
                object o = r[SchemaKatalog.SPALTE_PW_KOSTENART];
                if (o == null || o == DBNull.Value) return false;
                return string.Equals(Convert.ToString(o).Trim(), DbWerte.KOSTENART_ZUSCHUSS,
                                     StringComparison.Ordinal);
            }
            catch (Exception ex) when (ex is ArgumentException || IstZahlfehler(ex))
            {
                // ETAPPE E7c3 (B‑6): benannt — ohne Spalte Kostenart ist eine Zeile kein
                // Zuschuss (Datenbank vor der Kaskade).
                return false;
            }
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
        /// <para><b>ETAPPE E16:</b> Positionen „alle n Jahre" kennt diese Sicht nicht — sie
        /// stehen allein in <see cref="BetriebsTopfe.Wiederholt"/>; ein (Betrag, Startjahr)-Paar
        /// sagte „jedes Jahr ab X" und wäre falsch.</para>
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
            /// <summary>ETAPPE E7c3 (Befund B‑6): der Grund, aus dem die Leseschleife
            /// abbrach (dann fehlen die Positionen danach) oder die Bemessungsspalten nicht
            /// sichergestellt werden konnten; <c>null</c> = vollständig gelesen.</summary>
            public string Fehler;

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

            /// <summary>
            /// ETAPPE E16 (V‑G3, DIN EN 17463 6.3.1): die Positionen <b>„alle n Jahre"</b>
            /// (Wiederholperiode n ≥ 2) beider Töpfe — je mit Betrag, Startjahr, Periode und
            /// Topf. Sie stehen NUR hier, nicht in den Sofort- und Startjahr-Anteilen; ohne
            /// gepflegte Periode bleibt die Liste leer, und jede Zahl oben ist bitgenau die
            /// von vorher.
            /// </summary>
            public List<KapitalwertRechner.Wiederholposten> Wiederholt =
                new List<KapitalwertRechner.Wiederholposten>();

            /// <summary>
            /// ETAPPE E16 (E16‑Q3 a): der Anteil der Positionen „alle n Jahre", der im
            /// ERSTEN Jahr zahlt (Startjahr ≤ 1) [€/a] — er gehört zur Jahr-1-Zahl der
            /// Betriebskosten p. a., wie jede jährliche Position ab Jahr 1.
            /// </summary>
            public double WiederholtErstesJahr
            {
                get { return ErstesJahr(Wiederholt); }
            }

            /// <summary>Betriebskosten p. a. GESAMT [€/a] — beide Töpfe, Sofort- und
            /// Startjahr-Anteil. Das ist die Zahl, die Anzeigen und Berichte als
            /// „Betriebskosten p. a." ausweisen; sie ist von FX3 unberührt.
            /// <para><b>PAKET FX5-a:</b> <see cref="InvestGekoppeltSofort"/> und
            /// <see cref="InvestGekoppeltAbJahr"/> gehen hier bewusst NICHT ein — sie
            /// sind eine Teilmenge der beiden Betriebsfelder und wären sonst doppelt
            /// gezählt.</para>
            /// <para><b>ETAPPE E16:</b> Die Positionen „alle n Jahre" zählen mit ihrem
            /// Betrag je Zahlung — wie eine Startjahr-Position mit ihrem Jahresbetrag.</para></summary>
            public double Gesamt
            {
                get
                {
                    double s = BetriebSofort + EndenergieSofort;
                    foreach (KeyValuePair<double, int> vb in BetriebAbJahr) s += vb.Key;
                    foreach (KeyValuePair<double, int> ve in EndenergieAbJahr) s += ve.Key;
                    foreach (KapitalwertRechner.Wiederholposten w in Wiederholt) s += w.Betrag;
                    return s;
                }
            }
        }

        /// <summary>
        /// ETAPPE E16 (E16‑Q3 a): die Summe der Positionen „alle n Jahre", die im ERSTEN Jahr
        /// zahlen [€/a] — dieselbe Regel wie der Rechenkern
        /// (<see cref="KapitalwertRechner.ZahltImJahr"/> für t = 1). 0 ohne Liste.
        /// </summary>
        internal static double ErstesJahr(IList<KapitalwertRechner.Wiederholposten> wiederholt)
        {
            double s = 0;
            if (wiederholt != null)
                foreach (KapitalwertRechner.Wiederholposten w in wiederholt)
                    if (w != null && KapitalwertRechner.ZahltImJahr(w.StartJahr, w.Periode, 1)) s += w.Betrag;
            return s;
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
            return LiesBetriebskostenTopfe(idProjekt, szenario, null);
        }

        /// <summary>
        /// ETAPPE W5‑B‑11 (Anwenderentscheid 09.09.2026, VALERI-Lücke G11): dieselbe
        /// Leseschleife, aber die Bemessungsbasis der Zeilen „x % der Investitionssumme"
        /// folgt dem SZENARIO-Investitionsausschlag.
        ///
        /// <para><b>Der Befund.</b> Die Bezugsgröße kam bis dahin immer aus dem
        /// Erwartungslauf der Kaskade (<c>BetriebskostenCtrl.Kaskadensummen</c> stand fest
        /// auf ERWARTET). Eine Anlage, die im Worst-Fall 10 % mehr kostet, hatte damit
        /// dieselbe Wartung wie im Erwartungsfall — während die Sensitivität denselben
        /// Ausschlag längst mitzieht (PAKET FX5‑a, additive Korrektur über
        /// <see cref="BetriebsTopfe.InvestGekoppeltSofort"/>). Der Anwender hat am
        /// 09.09.2026 entschieden, die beiden Wege gleichzuziehen.</para>
        ///
        /// <para><b>Die Vorrangregel gilt auch hier.</b> Skaliert wird die Basis je Zeile,
        /// und nur, wo kein Best-/Worst-Wert gepflegt ist: Eine gepflegte Zeile mit
        /// 7.000 € im Worst-Fall bleibt Basis 7.000 €
        /// (<see cref="InvestKaskade.BetragImSzenario"/>).</para>
        ///
        /// <para><b>Ohne Satz unverändert.</b> <paramref name="satz"/> = <c>null</c> ist
        /// der Weg des Szenarios ERWARTET und jeder Anzeige — dann wird der neue Zweig
        /// gar nicht erst betreten, und jede Zahl ist bitgenau die von vorher.</para>
        ///
        /// <para><b>Die Sensitivität bleibt, wo sie war.</b> Sie rechnet auf ERWARTET
        /// (dort ist <paramref name="satz"/> null) und korrigiert ihren eigenen Ausschlag
        /// weiterhin additiv in <see cref="RechneBild"/>. Beides zusammen wäre
        /// Doppelzählung; beides trifft aber nie zusammen.</para>
        /// </summary>
        internal static BetriebsTopfe LiesBetriebskostenTopfe(int idProjekt, string szenario,
                                                             SzenarioSatz satz)
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
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt — ohne Bemessungsspalten rechnet die Schleife
                // im Bestandsweg; der Grund reist mit (BetriebsTopfe.Fehler).
                topfe.Fehler = Fehlergrund.Text(ex);
            }

            try
            {
                string felder = "EingegebenerWert, BestCase, WorstCase";
                // ETAPPE E30/2 (#548, B4): die Zeilen-ID — sie findet die Position, die den
                // Hilfsenergieanteil ihrer Anlage als Satz trägt (HilfsenergieAusAnteil).
                HilfsenergieAusAnteil.Plan hilfsPlan = null;
                if (mitBemessung)
                {
                    felder = "ID, " + felder;
                    hilfsPlan = HilfsenergieAusAnteil.Plane(idProjekt);
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
                // ETAPPE E16 (V‑G3): die Wiederholperiode — nur, wo es die Spalte gibt.
                if (WiederholperiodeSchema.SpalteVorhanden(SchemaKatalog.TAB_PROJEKTWERTE))
                    felder += ", [" + WiederholperiodeSchema.SPALTE + "]";

                // ETAPPE E7c3 (B‑6): der strenge Leseweg — ein Abfragefehler erreicht den
                // benannten Fang (topfe.Fehler), statt als leere Tabelle „keine
                // Betriebskosten" zu heißen.
                DataTable dt = StilleDb.TabelleStreng(
                    "SELECT " + felder +
                    " FROM Tab_ProjektWerte WHERE ProjektID = ? AND KategorieID = 2",
                    new DbParam("@p", idProjekt));

                // ETAPPE H2: der Endenergie-Auflöser wird je Aufruf höchstens einmal
                // gebaut — und nur, wenn eine Position ihn wirklich braucht.
                EndenergieAufloeser endenergie = null;
                bool endenergieVersucht = false;
                // W5‑B‑8: die Investitionskaskade des Projekts — höchstens EINMAL je
                // Leseschleife, und nur, wenn eine Zeile sie wirklich braucht.
                Dictionary<KeyValuePair<int, int>, double> investSummen = null;

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

                        HilfsenergieAusAnteil.Anlage hilfsAnlage;
                        if (hilfsPlan != null && !hilfsPlan.Leer &&
                            hilfsPlan.JeZeile.TryGetValue(ZeilenId(r), out hilfsAnlage))
                        {
                            // ETAPPE E30/2 (#548, B4, E30‑Q1 a): Die Position trägt den
                            // Hilfsenergieanteil ihrer Anlage als Satz und rechnet nach Weg B
                            // — gleich, welche Bemessung gespeichert ist. Ein gepflegter
                            // Szenariowert schlägt auch hier die Ableitung (VALERI-Muster).
                            ausEnergiepreis = true;
                            ausInvestition = false;
                            double erwartetH = D(r, "EingegebenerWert") ?? 0;
                            if (Math.Abs(wert - erwartetH) > 1e-9)
                                beitrag = erloes && wert > 0 ? -wert : wert;
                            else
                                beitrag = HilfsenergieBetrag(idProjekt, hilfsAnlage,
                                                             ref endenergie, ref endenergieVersucht,
                                                             szenario, satz);
                        }
                        else if (string.IsNullOrEmpty(bem) ||
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
                            // ETAPPE E9a: Der Auflöser trägt Mengenfaktor und Trägerpreise des
                            // Szenarios — eine an Endenergie, Stunden oder kWh bemessene Zeile
                            // folgt damit den Mengen und Preisen, mit denen das Szenario rechnet.
                            if (IstEndenergieArt(bem))
                                menge = EndenergieMenge(idProjekt, r, bem,
                                                        ref endenergie, ref endenergieVersucht,
                                                        szenario, satz);
                            else if (IstRueckfallErmittelbareArt(bem))
                            {
                                double? frisch = RueckfallMenge(idProjekt, r, bem,
                                                                ref endenergie, ref endenergieVersucht,
                                                                ref investSummen, satz, szenario);
                                if (frisch.HasValue) menge = frisch;
                            }

                            beitrag = szenarioGepflegt
                                ? (erloes && wert > 0 ? -wert : wert)
                                : BetriebskostenCtrl.Betrag(bem, erwartet, menge,
                                                            D(r, SchemaKatalog.SPALTE_PW_EINHEITPREIS),
                                                            erloes);
                        }
                    }

                    // ETAPPE E16 (V‑G3, DIN EN 17463 6.3.1): Eine Position „alle n Jahre"
                    // (n ≥ 2) geht in KEINEN der Akkumulatoren darunter, sondern mit Betrag,
                    // Startjahr, Periode und Topf in die eigene Liste — der Rechenkern zählt sie
                    // nur in ihren Zahlungsjahren. Jährliche Zeilen (NULL/0/1, der ganze
                    // Bestand) laufen unverändert weiter; die Summationsreihenfolge der
                    // Akkumulatoren bleibt die von vorher.
                    int periode = Wiederholperiode.DerZeile(r);
                    if (periode >= Wiederholperiode.MIN_WIEDERHOLT)
                    {
                        topfe.Wiederholt.Add(new KapitalwertRechner.Wiederholposten
                        {
                            Betrag = beitrag,
                            StartJahr = start > 1 ? start : 1,
                            Periode = periode,
                            Endenergie = ausEnergiepreis,
                            InvestGekoppelt = ausInvestition
                        });
                        continue;
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

                // ETAPPE E30/2 (#548, B4): Anlagen mit Hilfsenergieanteil, aber ohne
                // Hilfsenergie-Kostenposition — ihre abgeleitete Zeile zahlt jährlich ab
                // Jahr 1 im Endenergie-Topf. Hinter der Schleife, damit die
                // Summationsreihenfolge der gelesenen Zeilen bleibt.
                if (hilfsPlan != null)
                    foreach (HilfsenergieAusAnteil.Anlage a in hilfsPlan.OhneZeile)
                        summeEnde += HilfsenergieBetrag(idProjekt, a, ref endenergie,
                                                        ref endenergieVersucht, szenario, satz);
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt statt catch { } — die Summe bleibt, soweit
                // sie gelesen ist, aber sie ist als unvollständig benannt.
                topfe.Fehler = Fehlergrund.Text(ex);
            }
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
            return LiesBetriebskostenPositionen(idProjekt, szenario, null);
        }

        /// <summary>ETAPPE W5‑B‑11 (G11): dieselbe Nachweisliste mit der
        /// szenariogerechten Bemessungsbasis. Sie MUSS denselben Satz bekommen wie
        /// <see cref="LiesBetriebskostenTopfe"/> — sonst wiese der Bericht im Best-/
        /// Worst-Fall eine andere Summe aus als die, mit der gerechnet wurde (die
        /// E7-Probe „Summe der Nachweisliste = Summe der Rechnung").</summary>
        internal static List<KostenPositionNachweis> LiesBetriebskostenPositionen(
            int idProjekt, string szenario, SzenarioSatz satz)
        {
            var liste = new List<KostenPositionNachweis>();
            bool mitBemessung = false;
            try { mitBemessung = KostenPositionCtrl.StelleSpaltenSicher(); }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt — ohne Bemessungsspalten keine Gliederung;
                // die Summenrechnung (LiesBetriebskostenTopfe) nennt denselben Grund.
                Vorsorgefehler(ex);
            }
            if (!mitBemessung) return liste;   // ohne Schritt 19 gibt es nichts zu gliedern

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT w.ID, w.EingegebenerWert, w.BestCase, w.WorstCase, w.Gruppe, " +
                    "f.Bezeichnung, w.[" + SchemaKatalog.SPALTE_PW_KOSTENART + "], " +
                    "w.[" + SchemaKatalog.SPALTE_PW_BEMESSUNG + "], " +
                    "w.[" + SchemaKatalog.SPALTE_PW_IST_ERLOES + "], " +
                    "w.[" + SchemaKatalog.SPALTE_PW_MENGE + "], " +
                    "w.[" + SchemaKatalog.SPALTE_PW_EINHEITPREIS + "], w.KomponentenID" +
                    (AnlagenSpalteVorhanden()
                        ? ", w.[" + SchemaKatalog.SPALTE_PW_ID_ANLAGE + "]"
                        : "") +
                    // ETAPPE E8c (E8b‑Q3): das Startjahr (KD6) — die Probe der Gliederung
                    // vergleicht nur die Positionen, die im ersten Jahr zahlen.
                    (StartjahrSpalteVorhanden()
                        ? ", w.[" + SchemaKatalog.SPALTE_PW_STARTJAHR + "]"
                        : "") +
                    // ETAPPE E16 (V‑G3): die Wiederholperiode — Herleitung „alle n Jahre ab
                    // Jahr X" und die Probe der Gliederung (nur Positionen des ersten Jahres).
                    (WiederholperiodeSchema.SpalteVorhanden(SchemaKatalog.TAB_PROJEKTWERTE)
                        ? ", w.[" + WiederholperiodeSchema.SPALTE + "]"
                        : "") +
                    " FROM Tab_ProjektWerte AS w LEFT JOIN Tab_Kostenfaktor AS f " +
                    "ON w.StammID = f.StammID " +
                    "WHERE w.ProjektID = ? AND w.KategorieID = 2",
                    new DbParam("@p", idProjekt));
                if (dt == null) return liste;

                // ETAPPE E30/2 (#548, B4): derselbe Plan wie in der Summenschleife — die
                // Position mit dem Anteil ihrer Anlage als Satz, dazu die abgeleiteten Zeilen.
                HilfsenergieAusAnteil.Plan hilfsPlan = HilfsenergieAusAnteil.Plane(idProjekt);

                // ETAPPE H2: gleiche Frisch-Regel wie in der Summenschleife — die
                // Nachweisliste muss deren Summe treffen (E7-Probe).
                EndenergieAufloeser endenergie = null;
                bool endenergieVersucht = false;
                // W5‑B‑8: dieselbe Kaskade wie in der Summenschleife, einmal je Lesepass.
                Dictionary<KeyValuePair<int, int>, double> investSummen = null;
                // ETAPPE E10 (Stufe S3): Lesestand der Nutzungsdauertabelle — nur für die
                // HERKUNFT eines gepflegten Satzes, einmal je Lesepass und nur bei Bedarf.
                NutzungsdauerSatztafel satztafel = null;

                foreach (DataRow r in dt.Rows)
                {
                    double wert = Szenariowert(r, szenario, "EingegebenerWert", "BestCase", "WorstCase");
                    string bem = Text(r, SchemaKatalog.SPALTE_PW_BEMESSUNG);
                    if (string.IsNullOrEmpty(bem)) bem = DbWerte.BEMESSUNG_BETRAG;
                    bool erloes = B(r, SchemaKatalog.SPALTE_PW_IST_ERLOES);
                    double erwartet = D(r, "EingegebenerWert") ?? 0;
                    bool szenarioGepflegt = Math.Abs(wert - erwartet) > 1e-9;

                    // ETAPPE E30/2 (#548, B4): Trägt die Position den Hilfsenergieanteil
                    // ihrer Anlage, rechnet und zeigt sie Weg B mit dem Anteil als Satz.
                    HilfsenergieAusAnteil.Anlage hilfsAnlage = null;
                    bool ausAnteil = !hilfsPlan.Leer &&
                                     hilfsPlan.JeZeile.TryGetValue(ZeilenId(r), out hilfsAnlage);
                    if (ausAnteil) bem = DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF;

                    // H2/H4a/H2-1: dieselbe Mengenregel wie in der Summenschleife
                    // (frisch vor Konserve) — die Nachweisliste muss deren Summe
                    // treffen (E7-Probe).
                    double? menge = D(r, SchemaKatalog.SPALTE_PW_MENGE);
                    if (ausAnteil)
                        menge = HilfsenergieMenge(idProjekt, hilfsAnlage,
                                                  ref endenergie, ref endenergieVersucht,
                                                  szenario, satz);
                    else if (IstEndenergieArt(bem))
                        menge = EndenergieMenge(idProjekt, r, bem,
                                                ref endenergie, ref endenergieVersucht,
                                                szenario, satz);
                    else if (IstRueckfallErmittelbareArt(bem))
                    {
                        double? frisch = RueckfallMenge(idProjekt, r, bem,
                                                        ref endenergie, ref endenergieVersucht,
                                                        ref investSummen, satz, szenario);
                        if (frisch.HasValue) menge = frisch;
                    }

                    int komponente, idAnlage;
                    KomponenteUndAnlage(r, out komponente, out idAnlage);
                    // ETAPPE E8c (E8b‑Q3): dieselbe Lesung des Startjahrs wie in der
                    // Summenschleife — 0 heißt „ab dem ersten Jahr".
                    int start = StartJahrDerZeile(r);

                    // ETAPPE E10 (Stufe S3, Fassung E10/9): gerechnet wird mit dem Satz der
                    // Zeile, wie er gepflegt ist — die Nutzungsdauertabelle rechnet nicht
                    // selbst. Ist der gepflegte Satz GENAU der der Tabelle (vorbelegt oder
                    // übernommen), trägt die Zeile ihre Herkunft; Herleitung und Formelmappe
                    // nennen sie.
                    double? satzDerZeile = ausAnteil
                        ? hilfsAnlage.AnteilProzent
                        : D(r, SchemaKatalog.SPALTE_PW_EINHEITPREIS);
                    bool satzAusTabelle = !ausAnteil &&
                                          SatzAusTabelle(r, bem, satzDerZeile, ref satztafel);

                    var n = new KostenPositionNachweis
                    {
                        // W5‑B‑7: der Schlüssel und die Zuordnung — damit Dialog und
                        // Kostenseite dieselbe Zeile derselben Rechnung wiederfinden.
                        Id = r.Table.Columns.Contains("ID") && r["ID"] != DBNull.Value
                             ? Convert.ToInt32(r["ID"]) : 0,
                        Komponente = komponente,
                        Anlage = idAnlage,
                        Bezeichnung = Text(r, "Bezeichnung"),
                        Gruppe = Text(r, "Gruppe"),
                        Kostenart = Text(r, SchemaKatalog.SPALTE_PW_KOSTENART),
                        Bemessung = bem,
                        Menge = menge,
                        Einheitpreis = satzDerZeile,
                        SatzHerkunft = ausAnteil
                            ? HilfsenergieAusAnteil.HERKUNFT_ANLAGENANTEIL
                            : (satzAusTabelle ? NutzungsdauerSatzCtrl.HERKUNFT_TABELLE : null),
                        IstErloes = erloes,
                        SzenarioGepflegt = szenarioGepflegt,
                        StartJahr = start > 1 ? start : (int?)null,
                        // ETAPPE E16 (V‑G3): dieselbe Lesung wie in der Summenschleife —
                        // null heißt jährlich.
                        Wiederholperiode = Wiederholperiode.Normiert(Wiederholperiode.DerZeile(r))
                    };
                    n.BetragJahr =
                        string.Equals(bem, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal)
                            ? (erloes && wert > 0 ? -wert : wert)
                            : (szenarioGepflegt
                                ? (erloes && wert > 0 ? -wert : wert)
                                : BetriebskostenCtrl.Betrag(bem, erwartet, n.Menge, n.Einheitpreis, erloes));
                    liste.Add(n);
                }

                // ETAPPE E30/2 (#548, B4): die abgeleiteten Zeilen der Anlagen ohne
                // Hilfsenergie-Kostenposition — dieselben Beträge wie in der Summenschleife.
                // Ohne Zeilen-ID (0): Sie stehen in keiner Tabelle, der Dialog schlägt sie
                // nicht nach (BetriebNachId).
                foreach (HilfsenergieAusAnteil.Anlage a in hilfsPlan.OhneZeile)
                {
                    var n = new KostenPositionNachweis
                    {
                        Id = 0,
                        Komponente = a.Komponente,
                        Anlage = a.IdAnlage,
                        Bezeichnung = HilfsenergieAusAnteil.NameAbgeleiteteZeile(),
                        Gruppe = DbWerte.KOSTEN_GRUPPE_BETRIEB_VDI,
                        Kostenart = DbWerte.KOSTENART_BEDARFSGEBUNDEN,
                        Bemessung = DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                        Menge = HilfsenergieMenge(idProjekt, a, ref endenergie, ref endenergieVersucht,
                                                  szenario, satz),
                        Einheitpreis = a.AnteilProzent,
                        SatzHerkunft = HilfsenergieAusAnteil.HERKUNFT_ANLAGENANTEIL
                    };
                    n.BetragJahr = BetriebskostenCtrl.Betrag(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF,
                                                             0.0, n.Menge, n.Einheitpreis, false);
                    liste.Add(n);
                }
            }
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannt — die Nachweisliste bleibt, soweit gelesen;
                // dieselbe Tabelle liest die Summe (LiesBetriebskostenTopfe) streng, und
                // deren Abbruch nennt die Ergebniszeile (Rechenstufe „Betriebskosten").
            }
            return liste;
        }

        /// <summary>
        /// ETAPPE E10 (Stufe S3, Fassung E10/9) — die HERKUNFT des Satzes einer Betriebszeile,
        /// allein für den Nachweis: Trägt eine Position „Instandhaltung …"/„Wartung …" mit
        /// „% der Investition" GENAU den Satz der Nutzungsdauertabelle ihrer Technik, stammt er
        /// aus ihr — vorbelegt über „Sätze vorbelegen…" oder übernommen mit der Vorlage
        /// (<see cref="NutzungsdauerSatzCtrl.AusTabelle"/>).
        ///
        /// <para><b>Rechnet nichts.</b> Summen- und Nachweisschleife rechnen mit dem Satz der
        /// Zeile, wie er steht; eine leere Zeile bleibt leer (Anwenderentscheid ND‑Q4: nichts
        /// ändert eine gerechnete Wirtschaftlichkeit ohne Zutun). Ohne gepflegten Satz wird
        /// die Tabelle gar nicht erst gelesen.</para>
        /// </summary>
        private static bool SatzAusTabelle(DataRow r, string bem, double? satz,
                                           ref NutzungsdauerSatztafel satztafel)
        {
            if (!satz.HasValue || !IstProzentInvest(bem)) return false;
            int komponente = r.Table.Columns.Contains("KomponentenID") && r["KomponentenID"] != DBNull.Value
                ? Convert.ToInt32(r["KomponentenID"], System.Globalization.CultureInfo.InvariantCulture) : 0;
            return NutzungsdauerSatzCtrl.AusTabelle(bem, satz, komponente, Text(r, "Bezeichnung"),
                                                    ref satztafel);
        }

        /// <summary>
        /// ANWENDERBEFUND W5‑B‑7 (08.09.2026): dieselben Kategorie-2-Positionen wie
        /// <see cref="LiesBetriebskostenPositionen"/>, geschlüsselt nach
        /// <c>Tab_ProjektWerte.ID</c> — die Form, in der der Dialog Kostenverwaltung und
        /// die Anlagentabelle der Kostenseite ihre Zeile nachschlagen.
        ///
        /// <para><b>Warum diese Liste und nicht die Summenschleife.</b> Die
        /// Summenschleife (<see cref="LiesBetriebskostenTopfe"/>) ist der Rechenweg und
        /// liefert TÖPFE, keine Zeilen; die Nachweisliste rechnet mit denselben Regeln
        /// und ihre Summe muss deren Summe treffen (E7-Probe). Auf einer Datenbank ohne
        /// Schritt 19 ist sie leer — der Aufrufer fällt dann auf den Bestandsweg
        /// zurück.</para>
        /// </summary>
        internal static Dictionary<int, KostenPositionNachweis> BetriebNachId(
            int idProjekt, string szenario)
        {
            var karte = new Dictionary<int, KostenPositionNachweis>();
            foreach (KostenPositionNachweis n in LiesBetriebskostenPositionen(idProjekt, szenario))
                if (n.Id > 0) karte[n.Id] = n;
            return karte;
        }

        internal static double Szenariowert(DataRow r, string szenario,
                                           string spalteErwartet, string spalteBest, string spalteWorst)
        {
            bool gepflegtEgal;
            return Szenariowert(r, szenario, spalteErwartet, spalteBest, spalteWorst,
                                out gepflegtEgal);
        }

        /// <summary>
        /// ETAPPE W5‑B‑9 (09.09.2026): derselbe Szenariowert, aber mit seiner HERKUNFT.
        ///
        /// <para><paramref name="gepflegt"/> ist <c>true</c>, wenn der Wert aus der
        /// Best- bzw. Worst-Spalte kam — und <c>false</c>, wenn er nach dem VALERI-Muster
        /// auf den Erwartungswert zurückgefallen ist (0/leer) oder wenn gar kein
        /// Szenario abgefragt wurde (ERWARTET). Genau daran hängt die Vorrangregel des
        /// Szenario-Parametersatzes: Der pauschale Ausschlag greift NUR auf
        /// zurückgefallene Zeilen, sonst zählte er doppelt.</para>
        ///
        /// <para><b>Der Rechenweg selbst ist unverändert</b> — die Fassung ohne den
        /// Ausgabeparameter ruft diese hier auf und wirft die Auskunft weg.</para>
        /// </summary>
        internal static double Szenariowert(DataRow r, string szenario,
                                           string spalteErwartet, string spalteBest,
                                           string spalteWorst, out bool gepflegt)
        {
            gepflegt = false;
            double erwartet = D(r, spalteErwartet) ?? 0;
            string spalte = szenario == WirtschaftlichkeitSzenario.BEST ? spalteBest
                          : szenario == WirtschaftlichkeitSzenario.WORST ? spalteWorst : null;
            if (spalte == null) return erwartet;
            double wert = D(r, spalte) ?? 0;
            gepflegt = wert != 0;                 // 0/leer = kein Szenariowert gepflegt
            return gepflegt ? wert : erwartet;
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
            return IstEndenergieArt(bem) || IstProjektkostenArt(bem);
        }

        /// <summary>
        /// ETAPPE E7c (B‑4 Rest, Konzept § 4): die zwei projektweiten Alt-Arten
        /// <c>PROZENT_BRENNSTOFFKOSTEN</c> und <c>PROZENT_STROMKOSTEN</c>. Sie
        /// eskalieren seit FX4-b mit p_E (<see cref="IstEnergiepreisArt"/>) und holen
        /// ihre Bezugsgröße seit E7c frisch aus dem jüngsten Lauf wie <c>EUR_PRO_H</c>
        /// und die <c>EUR_PRO_KWH_*</c>-Arten — die projektweiten Brennstoff- bzw.
        /// Stromkosten des Laufs (<see cref="EndenergieAufloeser.BrennstoffkostenProjektEuro"/>,
        /// <see cref="EndenergieAufloeser.StromkostenProjektEuro"/>). Die Menge-Spalte ist
        /// damit auch hier Ausweisgröße und Konserve nur, wo frisch nichts ermittelbar ist.
        /// </summary>
        private static bool IstProjektkostenArt(string bem)
        {
            return string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN, StringComparison.Ordinal) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_STROMKOSTEN, StringComparison.Ordinal);
        }

        /// <summary>
        /// ETAPPE H2: frische Bezugsmenge einer Endenergie-Position aus dem jüngsten
        /// Lauf. Weg A liefert die Arbeitskosten [€/a]; Weg B den BEWERTETEN Bedarf
        /// (kWh × Strompreis der Anlage, <c>Groesse.BewertungspreisJeKwh</c>) —
        /// <c>Menge × Satz / 100</c> ergibt so ohne zweite
        /// Formel den Betrag (Begründung bei <see cref="BetriebskostenCtrl.Betrag"/>).
        /// null = keine Bezugsgröße (kein Lauf, Anlage nicht im Lauf, Preis fehlt) —
        /// dann gilt die dokumentierte 0.
        /// </summary>
        private static double? EndenergieMenge(int idProjekt, DataRow r, string bem,
                                               ref EndenergieAufloeser aufloeser, ref bool versucht,
                                               string szenario, SzenarioSatz satz)
        {
            if (!versucht)
            {
                versucht = true;
                aufloeser = Aufloeser(idProjekt, szenario, satz);
            }
            if (aufloeser == null) return null;

            int komponente, idAnlage;
            KomponenteUndAnlage(r, out komponente, out idAnlage);
            return EndenergieMenge(aufloeser, komponente, idAnlage, bem);
        }

        /// <summary>ETAPPE E30/2 (#548): dieselbe Bezugsgröße für Komponente und Anlage
        /// ohne Positionszeile — der gemeinsame Kern der Zeilenfassung oben und der
        /// Hilfsenergie aus dem Anlagenanteil.</summary>
        private static double? EndenergieMenge(EndenergieAufloeser aufloeser, int komponente,
                                               int idAnlage, string bem)
        {
            if (aufloeser == null) return null;
            EndenergieAufloeser.Groesse g = aufloeser.FuerPosition(komponente, idAnlage);
            if (g == null) return null;

            if (string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_ENDENERGIEKOSTEN, StringComparison.Ordinal))
                return g.KostenEuro;

            // ANWENDERENTSCHEID 19.09.2026: Der Preis kommt vom Auflöser, nicht aus
            // dem Projektträger — er weiß als einziger, ob DIESE Anlage einen eigenen
            // Stromträger trägt (Wärmepumpe, Heizstab, Elektrokessel). Für eine
            // Stromanlage bewerten Weg A und Weg B damit mit demselben Preis.
            double? strompreis = g.BewertungspreisJeKwh;
            return strompreis.HasValue ? g.BedarfKwh * strompreis.Value : (double?)null;
        }

        /// <summary>
        /// ETAPPE E30/2 (#548, B4) — die Bezugsgröße der Hilfsenergie aus dem Anteil einer
        /// Anlage: Brennstoff dieser Anlage [kWh] × Arbeitspreis des Projekt-Stromträgers
        /// (Weg B, <see cref="HilfsenergieAusAnteil"/>); <c>null</c> = nicht ermittelbar
        /// (kein Lauf, Anlage nicht im Lauf, kein Strompreis).
        /// </summary>
        private static double? HilfsenergieMenge(int idProjekt, HilfsenergieAusAnteil.Anlage a,
                                                 ref EndenergieAufloeser aufloeser, ref bool versucht,
                                                 string szenario, SzenarioSatz satz)
        {
            if (!versucht)
            {
                versucht = true;
                aufloeser = Aufloeser(idProjekt, szenario, satz);
            }
            return EndenergieMenge(aufloeser, a.Komponente, a.IdAnlage,
                                   DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF);
        }

        /// <summary>
        /// ETAPPE E30/2 (#548, B4) — der Jahresbetrag der Hilfsenergie aus dem Anteil einer
        /// Anlage: <c>Bezugsgröße × Anteil / 100</c> über den einen Rechenweg
        /// (<see cref="BetriebskostenCtrl.Betrag"/>, Weg B). Ohne Bezugsgröße 0 — der
        /// erfasste Wert einer vorbereiteten Position ist 0 (Anwenderentscheid I‑2).
        /// </summary>
        private static double HilfsenergieBetrag(int idProjekt, HilfsenergieAusAnteil.Anlage a,
                                                 ref EndenergieAufloeser aufloeser, ref bool versucht,
                                                 string szenario, SzenarioSatz satz)
        {
            double? menge = HilfsenergieMenge(idProjekt, a, ref aufloeser, ref versucht, szenario, satz);
            return BetriebskostenCtrl.Betrag(DbWerte.BEMESSUNG_PROZENT_ENDENERGIEBEDARF, 0.0,
                                             menge, a.AnteilProzent, false);
        }

        /// <summary>ETAPPE E30/2: <c>Tab_ProjektWerte.ID</c> einer gelesenen Zeile; 0, wenn
        /// die Spalte nicht mitgelesen ist.</summary>
        private static int ZeilenId(DataRow r)
        {
            return r.Table.Columns.Contains("ID") && r["ID"] != DBNull.Value
                 ? Convert.ToInt32(r["ID"], System.Globalization.CultureInfo.InvariantCulture) : 0;
        }

        /// <summary>
        /// ETAPPE E9a — der Endenergie-Auflöser DIESES Szenarios: Mengen mit dem Mengenfaktor
        /// des Satzes, Preise mit den Trägerpreisen des Szenarios
        /// (<see cref="EndenergieAufloeser.FuerProjekt(int, string, double)"/>). Ohne Satz
        /// (ERWARTET, jede Anzeige) und ohne gepflegte Szenariopreise der Auflöser von vor E9a.
        /// </summary>
        private static EndenergieAufloeser Aufloeser(int idProjekt, string szenario, SzenarioSatz satz)
        {
            return EndenergieAufloeser.FuerProjekt(idProjekt, szenario,
                                                  satz != null ? satz.MengeFaktor : 1.0);
        }

        /// <summary>Komponente und Anlage der Positionszeile (0 = nicht gesetzt bzw.
        /// Spalte nicht mitgelesen) — gemeinsamer Helfer von H2 und H4a.</summary>
        internal static void KomponenteUndAnlage(DataRow r, out int komponente, out int idAnlage)
        {
            komponente = 0;
            idAnlage = 0;
            try
            {
                if (r.Table.Columns.Contains("KomponentenID") && r["KomponentenID"] != DBNull.Value)
                    komponente = Convert.ToInt32(r["KomponentenID"]);
            }
            catch (Exception ex) when (IstZahlfehler(ex))
            {
                // ETAPPE E7c3 (B‑6): benannt — keine Zahl heißt „nicht gesetzt" (0).
            }
            try
            {
                if (r.Table.Columns.Contains(SchemaKatalog.SPALTE_PW_ID_ANLAGE) &&
                    r[SchemaKatalog.SPALTE_PW_ID_ANLAGE] != DBNull.Value)
                    idAnlage = Convert.ToInt32(r[SchemaKatalog.SPALTE_PW_ID_ANLAGE]);
            }
            catch (Exception ex) when (IstZahlfehler(ex))
            {
                // ETAPPE E7c3 (B‑6): benannt — keine Zahl heißt „Anlage unbekannt" (0).
            }
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
        /// <c>PROZENT_BRENNSTOFFKOSTEN</c> und <c>PROZENT_STROMKOSTEN</c>.</para>
        /// <para><b>ETAPPE E7c (B‑4 Rest):</b> Die zwei Prozentarten holen ihre
        /// Bezugsgröße seither ebenfalls frisch aus dem jüngsten Lauf
        /// (<see cref="IstProjektkostenArt"/>) — die projektweiten Brennstoff- bzw.
        /// Stromkosten; Konserve bleibt allein <c>EUR_PRO_KWH</c>.</para></summary>
        private static bool IstRueckfallErmittelbareArt(string bem)
        {
            return IstProjektkostenArt(bem) ||
                   string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal) ||
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
        /// <para><b>ANWENDERENTSCHEID W5‑B‑8 (09.09.2026):</b> Die Investitionssumme kommt
        /// seither aus der <see cref="InvestKaskade"/> statt aus
        /// <c>SUM(EingegebenerWert)</c> — satzbasierte Zeilen und Prozentzeilen der
        /// Investseite zählen also mit. <paramref name="investSummen"/> ist der Merker
        /// dieser Kaskade für die LAUFENDE Leseschleife (null = noch nicht gelesen):
        /// Ohne ihn liefe der ganze Rechenweg der Kategorie 1 je Betriebskostenzeile
        /// erneut — dasselbe Muster wie <paramref name="aufloeser"/>/<paramref name="versucht"/>
        /// beim Endenergie-Auflöser.</para>
        /// <para><b>E20 (Anwenderentscheid 25.09.2026):</b> <paramref name="investition"/>
        /// = die Zeile gehört zu Kategorie 1. Nur dann kennt die Gerätewelt „je kW
        /// elektrisch" an der Wärmepumpe; die Summenschleifen der Betriebskosten fragen
        /// mit der Vorgabe false.</para>
        /// </summary>
        private static double? RueckfallMenge(int idProjekt, DataRow r, string bem,
                                              ref EndenergieAufloeser aufloeser, ref bool versucht,
                                              ref Dictionary<KeyValuePair<int, int>, double> investSummen,
                                              SzenarioSatz satz, string szenario,
                                              bool investition = false)
        {
            int komponente, idAnlage;
            KomponenteUndAnlage(r, out komponente, out idAnlage);

            if (string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal))
            {
                // ETAPPE W5-B-11 (G11): Die Kaskade wird im SZENARIO des Satzes gelesen -
                // mit gepflegten Zeilenwerten und pauschalem Ausschlag. satz = null ist
                // der Erwartungslauf und damit der Weg von vor dieser Etappe.
                if (investSummen == null)
                    investSummen = BetriebskostenCtrl.Kaskadensummen(idProjekt, satz);
                return BetriebskostenCtrl.InvestSummeFuer(idProjekt, komponente, idAnlage,
                                                          investSummen);
            }

            // ETAPPE E7c (B-4 Rest): „% der Brennstoffkosten" und „% der Stromkosten"
            // holen ihre Bezugsgröße frisch aus dem jüngsten Lauf — PROJEKTWEIT, denn so
            // sind die zwei Alt-Arten bemessen (der Vorläufer von Weg A, je Energieart
            // getrennt). Komponente und Anlage der Zeile spielen keine Rolle. Ohne Lauf,
            // Menge oder Preis bleibt es bei der gepflegten Menge (Konserve).
            if (IstProjektkostenArt(bem))
            {
                if (!versucht)
                {
                    versucht = true;
                    aufloeser = Aufloeser(idProjekt, szenario, satz);   // E9a: Mengen und Preise des Szenarios
                }
                if (aufloeser == null) return null;
                return string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN, StringComparison.Ordinal)
                    ? aufloeser.BrennstoffkostenProjektEuro()
                    : aufloeser.StromkostenProjektEuro();
            }

            // PAKET FX2 (B-4): „je Stunde" holt seine Stundenzahl aus dem Lauf — sonst
            // wie die kWh-Arten. Die Gerätewelt kennt die Art nicht; sie darf deshalb
            // nicht in den BaugroesseSumme-Zweig unten fallen.
            bool ausDemLauf =
                string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal) ||
                string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, StringComparison.Ordinal) ||
                string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, StringComparison.Ordinal);
            if (!ausDemLauf)
                return TechnikPlanwertCtrl.BaugroesseSumme(idProjekt, komponente, bem, idAnlage,
                                                           investition);

            if (!versucht)
            {
                versucht = true;
                aufloeser = Aufloeser(idProjekt, szenario, satz);   // E9a: Mengen und Preise des Szenarios
            }
            if (aufloeser == null) return null;

            if (string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal))
                return aufloeser.BetriebsstundenH(komponente, idAnlage);

            return string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, StringComparison.Ordinal)
                ? aufloeser.WaermeerzeugungKwh(komponente, idAnlage)
                : aufloeser.StromgroesseKwh(komponente, idAnlage);
        }

        // =====================================================================
        // ANWENDERBEFUND 10.09.2026 (H4c) — WARUM eine Zeile keine Bezugsgröße hat
        //
        // Der Befund lautete nicht nur „der Betrag ist 0", sondern „ich sehe nicht,
        // warum". Eine Zeile ohne ermittelbare Menge fällt über den Anwenderentscheid
        // I-2 auf den erfassten Betrag zurück — bei einer satzbasierten Zeile ist das
        // die 0, und die steht dann stumm im Raster. Diese Weiche benennt den Grund.
        //
        // SPRACHNEUTRALE STEUERWERTE, kein Anzeigetext: Der Kern kennt die Quelle einer
        // Bemessung, die Oberfläche kennt die Sprache (Drei-Schichten-Regel, Konzept
        // 13.6). Den Satz baut KostenKomponenteHuelle.
        // =====================================================================

        /// <summary>H4c: Die Art passt nicht zu diesem Gewerk — kein Pflegefehler.</summary>
        internal const string BASISGRUND_GEWERK = "GEWERK";

        /// <summary>H4c: Die Art passt, aber das Gerät fehlt oder führt die Größe als 0.</summary>
        internal const string BASISGRUND_GERAET = "GERAET";

        /// <summary>H4c: Die Größe kommt aus dem jüngsten Lauf — und den gibt es nicht.</summary>
        internal const string BASISGRUND_LAUF = "LAUF";

        /// <summary>H4c: „% der Investition" ohne Investitionskosten in der Kaskade.</summary>
        internal const string BASISGRUND_INVEST = "INVEST";

        /// <summary>H4c: Die Art wird nicht ermittelt — ihre Menge ist Eingabe.</summary>
        internal const string BASISGRUND_KONSERVE = "KONSERVE";

        // ---------------------------------------------------------------------
        // ANWENDERBEFUND 19.09.2026 — DREI LAGEN, DIE BIS HIERHER „KEIN
        // SIMULATIONSLAUF" HIESSEN
        //
        // BASISGRUND_LAUF stand für alles, was aus dem Lauf kommt und nicht dasteht.
        // Der Anwender las es auch dort, wo der Lauf samt Menge längst stand und
        // allein der Arbeitspreis des Energieträgers fehlte — und suchte den Fehler
        // an der falschen Stelle. Die drei Werte hier trennen die Lagen; welche
        // vorliegt, beantwortet der EndenergieAufloeser, der Lauf, Anlagen und Preise
        // ohnehin in der Hand hält (EndenergieAufloeser.GrundOhneBasis).
        // ---------------------------------------------------------------------

        /// <summary>#363: Es gibt einen Lauf, aber diese Anlage steht nicht darin —
        /// sie braucht einen Platz in der Simulationskonfiguration.</summary>
        internal const string BASISGRUND_ANLAGE = "ANLAGE";

        /// <summary>#363: Lauf und Anlage stehen, die Menge des Laufs ist 0.</summary>
        internal const string BASISGRUND_MENGE = "MENGE";

        /// <summary>#363: Die Menge steht, der Arbeitspreis des Energieträgers
        /// fehlt — die Bewertung ergäbe 0 (Weg A und Weg B, § 4.5). Auf Weg B ist
        /// damit der EIGENE Stromträger der Anlage gemeint (Anwenderentscheid
        /// 19.09.2026); für den Stromträger des Projekts gilt
        /// <see cref="BASISGRUND_STROMPREIS"/>.</summary>
        internal const string BASISGRUND_PREIS = "PREIS";

        /// <summary>
        /// ANWENDERENTSCHEID 19.09.2026: Die Menge steht, und Weg B fehlt der
        /// Arbeitspreis des PROJEKT-Stromträgers — die Lage einer Anlage mit
        /// Brennstoffträger (BHKW, Heizkessel auf Gas oder Öl), deren Hilfsstrom aus
        /// dem Netzbezug des Projekts bewertet wird.
        ///
        /// <para><b>Warum ein eigener Steuerwert.</b> Der Klartext ist die ABHILFE,
        /// und die zeigt hier auf einen anderen Eintrag der Energieträgerverwaltung
        /// als bei <see cref="BASISGRUND_PREIS"/>. Aus Steuerwert und Bemessungsart
        /// allein ist die Lage seit der anlagenscharfen Preisauflösung nicht mehr
        /// bestimmbar: Dieselbe Bemessungsart trägt an einer Stromanlage den eigenen,
        /// an einer Brennstoffanlage den Projektträger. Die Auskunft reist damit auf
        /// dem Weg, den die Zeile ohnehin führt, statt als zweite Angabe neben ihm.</para>
        /// </summary>
        internal const string BASISGRUND_STROMPREIS = "STROMPREIS";

        /// <summary>
        /// ANWENDERBEFUND 10.09.2026 (H4c): Warum trägt diese Zeile keine Bezugsgröße?
        /// Rückgabe ist einer der <c>BASISGRUND_*</c>-Steuerwerte, oder <c>""</c>, wenn
        /// die Bemessungsart überhaupt keine Bezugsgröße braucht (absolute Arten).
        ///
        /// <para>Der Aufrufer fragt NUR, wenn keine Menge ermittelt wurde — die Methode
        /// rechnet selbst nichts nach, sie liest allein die Landkarte Art↔Gewerk. Damit
        /// bleibt sie frei von Datenbankzugriffen und kann im Zeichenlauf des Dialogs
        /// stehen.</para>
        /// </summary>
        internal static string BasisGrund(string bem, int komponente)
        {
            return BasisGrund(bem, komponente, false);
        }

        /// <summary>
        /// Dieselbe Landkarte, erweitert um die GERÄTEKENNTNIS der Anlage:
        /// <paramref name="elektrokessel"/> = die Zeile hängt an einem Heizkessel, der
        /// auf Strom läuft (E1, Anwenderentscheid 18.09.2026). Er hat eine elektrische
        /// Größe — seinen Stromeinsatz —, und die kommt aus dem LAUF. Ohne diese
        /// Kenntnis hörte der Anwender an einem Elektrokessel „das Gewerk kennt die
        /// Größe nicht“, obwohl allein der Lauf fehlt.
        ///
        /// <para>Die Kenntnis wird HEREINGEREICHT, nicht hier ermittelt: Die Methode
        /// bleibt frei von Datenbankzugriffen. Wer sie hat, nimmt
        /// <see cref="BasisGrundFuerZeile"/>.</para>
        /// </summary>
        internal static string BasisGrund(string bem, int komponente, bool elektrokessel)
        {
            return BasisGrund(bem, komponente, elektrokessel, false);
        }

        /// <summary>
        /// E20 (Anwenderentscheid 25.09.2026, Konzept § 6.3 Nr. 10): dieselbe Landkarte im
        /// RASTER der Zeile — <paramref name="investition"/> = Kategorie 1. Nur die
        /// Gerätewelt hängt daran („je kW elektrisch" an der Wärmepumpe gibt es allein bei
        /// den Investitionskosten); auf der Betriebsseite bleibt die Antwort
        /// <see cref="BASISGRUND_GEWERK"/>.
        /// </summary>
        internal static string BasisGrund(string bem, int komponente, bool elektrokessel,
                                          bool investition)
        {
            if (string.IsNullOrEmpty(bem) ||
                string.Equals(bem, DbWerte.BEMESSUNG_BETRAG, StringComparison.Ordinal) ||
                string.Equals(bem, DbWerte.BEMESSUNG_JAHRESBETRAG, StringComparison.Ordinal))
                return "";

            // Beide Prozentarten der Kostenwelt: Ihre Basis ist ein EURO-Betrag der
            // Investseite — „% der Investition" die Kaskadensumme (W5‑B‑8), „% der
            // Erzeugerkosten" die Hauptposition (Kaskade, Runde 2). Fehlt sie, fehlen
            // Investitionskosten, nicht ein Lauf und nicht ein Gerät.
            if (string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal) ||
                string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_ERZEUGERKOSTEN, StringComparison.Ordinal))
                return BASISGRUND_INVEST;

            // Die Arten aus dem LAUF. Kennt das Gewerk die Größe gar nicht, ist ein
            // Simulationslauf keine Abhilfe — dann ist die ART das Problem.
            if (IstEndenergieArt(bem))
                return komponente == EndenergieAufloeser.KOMPONENTE_WAERMEPUMPE ||
                       komponente == BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL ||
                       komponente == BetriebskostenCtrl.KOMPONENTE_BHKW
                    ? BASISGRUND_LAUF : BASISGRUND_GEWERK;

            if (string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_H, StringComparison.Ordinal))
                return komponente == EndenergieAufloeser.KOMPONENTE_WAERMEPUMPE ||
                       komponente == BetriebskostenCtrl.KOMPONENTE_BHKW
                    ? BASISGRUND_LAUF : BASISGRUND_GEWERK;

            // E23 (Anwenderentscheid 25.09.2026): nicht an der Wärmepumpe — ihre
            // Betriebskosten sind ein fester Jahresbetrag oder % der Investition, nicht
            // je kWh (weder Strom noch Wärme). Bestandszeilen: siehe unten, „je kWh
            // elektrisch".
            if (string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_THERMISCH, StringComparison.Ordinal))
                return komponente == BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL ||
                       komponente == BetriebskostenCtrl.KOMPONENTE_BHKW ||
                       komponente == EndenergieAufloeser.KOMPONENTE_SOLARTHERMIE
                    ? BASISGRUND_LAUF : BASISGRUND_GEWERK;

            // E1: Am Heizkessel hängt die Antwort am GERÄT — nur der Elektrokessel
            // führt eine elektrische Größe (seinen Stromeinsatz, EndenergieAufloeser
            // .StromgroesseKwh). Am Brennstoffkessel bleibt es beim Gewerk.
            //
            // E23 (Anwenderentscheide E20‑Q6 b und 25.09.2026): An der WÄRMEPUMPE gibt es
            // die Art nicht mehr — „Strom-kWh sind Energiekosten", und die Betriebskosten
            // der WP werden überhaupt nicht je kWh bemessen (auch „je kWh thermisch"
            // oben nicht). Die Landkarte antwortet deshalb GEWERK, und die Auswahl folgt
            // ihr (BemessungKatalog.Auswahl). Eine Bestandszeile bleibt über „benutzt"
            // wählbar und RECHNET weiter: RueckfallMenge/FrischeBasis holen die Menge
            // ohne diese Landkarte (EndenergieAufloeser.StromgroesseKwh bzw.
            // .WaermeerzeugungKwh); die Herleitung nennt sie Altbestand
            // (KostenHerleitung.IstAltbestandWpKwh).
            if (string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, StringComparison.Ordinal))
                return komponente == EndenergieAufloeser.KOMPONENTE_PHOTOVOLTAIK ||
                       komponente == EndenergieAufloeser.KOMPONENTE_STROMSPEICHER ||
                       komponente == BetriebskostenCtrl.KOMPONENTE_BHKW ||
                       (elektrokessel && komponente == BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL)
                    ? BASISGRUND_LAUF : BASISGRUND_GEWERK;

            // ETAPPE E7c (B-4 Rest): „% der Brennstoff-/Stromkosten" kommen aus dem
            // Lauf — projektweit, an jedem Gewerk.
            if (IstProjektkostenArt(bem)) return BASISGRUND_LAUF;

            // Die Arten aus der GERÄTEWELT. Hier unterscheidet die Landkarte selbst,
            // ob die Art zum Gewerk passt (H4c).
            if (IstRueckfallErmittelbareArt(bem))
                return TechnikPlanwertCtrl.KenntBaugroesse(komponente, bem, investition)
                    ? BASISGRUND_GERAET : BASISGRUND_GEWERK;

            // „je kWh", „% der Erzeugerkosten": ihre Menge ist gepflegte Eingabe,
            // keine Ermittlung (FX2, Befund B-4).
            return BASISGRUND_KONSERVE;
        }

        /// <summary>
        /// E1 (Anwenderentscheid 18.09.2026): <see cref="BasisGrund(string, int)"/> für
        /// eine Zeile, die ihre ANLAGE kennt. Einzig hier kann die Gerätefrage den Grund
        /// drehen — „je kWh elektrisch" am Heizkessel —, und nur dort wird sie auch
        /// gestellt: Sie kostet eine Abfrage, und der Grundtext steht im Zeichenlauf des
        /// Dialogs. <paramref name="idAnlage"/> 0 = Anlage unbekannt; dann gilt die
        /// Landkarte allein.
        /// </summary>
        internal static string BasisGrundFuerZeile(string bem, int komponente, int idAnlage)
        {
            return BasisGrundFuerZeile(bem, komponente, idAnlage, false);
        }

        /// <summary>E20: <see cref="BasisGrundFuerZeile(string, int, int)"/> im RASTER der
        /// Zeile — <paramref name="investition"/> = Kategorie 1
        /// (<see cref="BasisGrund(string, int, bool, bool)"/>).</summary>
        internal static string BasisGrundFuerZeile(string bem, int komponente, int idAnlage,
                                                   bool investition)
        {
            bool elektrokessel =
                idAnlage > 0 &&
                komponente == BetriebskostenCtrl.KOMPONENTE_HEIZKESSEL &&
                string.Equals(bem, DbWerte.BEMESSUNG_EUR_PRO_KWH_ELEKTRISCH, StringComparison.Ordinal) &&
                IstElektrokesselAnlage(idAnlage);
            return BasisGrund(bem, komponente, elektrokessel, investition);
        }

        /// <summary>
        /// ANWENDERBEFUND 19.09.2026 (#363): derselbe Grund, aber mit dem LAUF in der
        /// Hand — die einzige Fassung, die „kein Simulationslauf" von „Anlage nicht im
        /// Lauf", „Menge 0" und „Arbeitspreis fehlt" unterscheiden kann.
        ///
        /// <para><b>Die Landkarte entscheidet zuerst.</b> Genauer wird nur, was sie als
        /// <see cref="BASISGRUND_LAUF"/> beantwortet hat; jede andere Antwort (Gewerk,
        /// Gerät, Investition, Konserve) bleibt Wort für Wort, wie sie war. Ein Projekt
        /// ohne Kennung (<paramref name="idProjekt"/> 0) bekommt ebenfalls die
        /// Landkarte allein.</para>
        ///
        /// <para><b>Der Auflöser wird höchstens EINMAL je Leseschleife gebaut</b> —
        /// <paramref name="aufloeser"/>/<paramref name="versucht"/> sind derselbe Merker
        /// wie in <see cref="EndenergieMenge"/>: <c>new ErgebnisCtrl().Load</c> liest
        /// den ganzen Lauf, und das darf nicht je Zeile geschehen. Gefragt wird ohnehin
        /// nur für Zeilen OHNE Bezugsgröße.</para>
        /// </summary>
        internal static string BasisGrundFuerZeile(string bem, int komponente, int idAnlage,
                                                   int idProjekt,
                                                   ref EndenergieAufloeser aufloeser,
                                                   ref bool versucht)
        {
            return BasisGrundFuerZeile(bem, komponente, idAnlage, idProjekt,
                                       ref aufloeser, ref versucht, false);
        }

        /// <summary>E20: dieselbe Fassung mit dem Lauf in der Hand, im RASTER der Zeile —
        /// <paramref name="investition"/> = Kategorie 1.</summary>
        internal static string BasisGrundFuerZeile(string bem, int komponente, int idAnlage,
                                                   int idProjekt,
                                                   ref EndenergieAufloeser aufloeser,
                                                   ref bool versucht,
                                                   bool investition)
        {
            string grund = BasisGrundFuerZeile(bem, komponente, idAnlage, investition);
            if (idProjekt <= 0 ||
                !string.Equals(grund, BASISGRUND_LAUF, StringComparison.Ordinal))
                return grund;

            if (!versucht)
            {
                versucht = true;
                aufloeser = EndenergieAufloeser.FuerProjekt(idProjekt);
            }
            if (aufloeser == null) return grund;

            // ETAPPE E7c (B-4 Rest): Die projektweiten Arten fragen nicht nach der
            // Anlage, sondern nach Lauf, Menge und Preis des ganzen Projekts.
            string genauer = IstProjektkostenArt(bem)
                ? aufloeser.GrundOhneProjektkosten(bem)
                : aufloeser.GrundOhneBasis(komponente, idAnlage, bem);
            return string.IsNullOrEmpty(genauer) ? grund : genauer;
        }

        /// <summary>
        /// Läuft der Heizkessel DIESER Anlagenzeile auf Strom
        /// (<see cref="SimulationSPK.BRENNSTOFF_STROM"/>)? Dieselbe eine Regel wie im
        /// <see cref="EndenergieAufloeser"/> und in der Trägerzulassung — der
        /// Brennstoff des Geräts, nicht ein Name. <c>false</c> bei jedem Zweifel:
        /// keine Anlage, kein Kessel, Abfrage gescheitert.
        /// </summary>
        internal static bool IstElektrokesselAnlage(int idAnlage)
        {
            if (idAnlage <= 0) return false;
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM Tab_Energieanlagen AS a " +
                    "INNER JOIN Tab_Heizkessel AS k ON a.ID_Kessel = k.ID " +
                    "WHERE a.ID = ? AND k.Brennstoff = ?",
                    new DbParam("@a", idAnlage),
                    new DbParam("@b", SimulationSPK.BRENNSTOFF_STROM));
                return o != null && o != DBNull.Value && Convert.ToInt32(o) > 0;
            }
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannt — der dokumentierte Zweifelsfall („false bei
                // jedem Zweifel"): Es gilt die Landkarte allein; nur der Grundtext des
                // Dialogs hängt daran, keine Zahl.
                return false;
            }
        }

        /// <summary>
        /// ANWENDERBEFUND 10.09.2026 (H4c): die frische Bezugsgröße EINER Position zu
        /// einer — auch noch ungespeicherten — Bemessungsart, samt Grund, wenn es keine
        /// gibt. Der Dialog braucht sie, sobald der Anwender die Bemessung wechselt: Bis
        /// dahin rechnete er bis zum Speichern mit der Bezugsgröße der ALTEN Art weiter.
        ///
        /// <para><b>Kein zweiter Rechenweg.</b> Gelesen wird mit denselben zwei
        /// Auflösern wie in der Summenschleife
        /// (<see cref="EndenergieMenge"/>/<see cref="RueckfallMenge"/>) und im
        /// Erwartungslauf, wie es der Ausweis in <see cref="MengeAusweisen"/> auch tut.
        /// GESCHRIEBEN wird hier nichts — die Vorschau darf die Konserve nicht
        /// anfassen, solange der Anwender nicht gespeichert hat.</para>
        /// </summary>
        internal static double? FrischeBasis(int positionsId, string bemessung, out string grund)
        {
            grund = "";
            if (positionsId <= 0) return null;
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
                if (dt == null || dt.Rows.Count == 0) return null;
                DataRow r = dt.Rows[0];

                string bem = string.IsNullOrEmpty(bemessung)
                    ? Text(r, SchemaKatalog.SPALTE_PW_BEMESSUNG) : bemessung;
                int idProjekt = r["ProjektID"] == DBNull.Value ? 0 : Convert.ToInt32(r["ProjektID"]);
                // E20: Das Raster der Zeile entscheidet mit, ob die Gerätewelt die Art
                // kennt („je kW elektrisch" an der Wärmepumpe nur in Kategorie 1).
                bool investition = r["KategorieID"] != DBNull.Value &&
                                   Convert.ToInt32(r["KategorieID"]) == DbWerte.KOSTEN_KATEGORIE_INVESTITION;

                EndenergieAufloeser aufloeser = null;
                bool versucht = false;
                Dictionary<KeyValuePair<int, int>, double> investSummen = null;
                double? menge = IstEndenergieArt(bem)
                    ? EndenergieMenge(idProjekt, r, bem, ref aufloeser, ref versucht, null, null)
                    : IstRueckfallErmittelbareArt(bem)
                        ? RueckfallMenge(idProjekt, r, bem, ref aufloeser, ref versucht,
                                         ref investSummen, null, null, investition)
                        : null;

                if (menge.HasValue) return menge;

                int komponente, idAnlage;
                KomponenteUndAnlage(r, out komponente, out idAnlage);
                // #363: Den genauen Grund — der Auflöser steht hier ohnehin schon (er
                // wurde für die Menge gebaut), also kostet die Auskunft nichts mehr.
                grund = BasisGrundFuerZeile(bem, komponente, idAnlage, idProjekt,
                                            ref aufloeser, ref versucht, investition);
                return null;
            }
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannt — reine Dialogvorschau vor dem Speichern:
                // keine Bezugsgröße und kein Grund („—"). Gerechnet wird erst beim
                // Speichern, frisch; scheitert dort das Lesen, nennt die Ergebniszeile
                // den Grund (Rechenstufe „Betriebskosten").
                grund = "";
                return null;
            }
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
        /// <para><b>W5‑B‑8 (09.09.2026):</b> In KATEGORIE 2 wird für dieselbe
        /// Bemessungsart seither die KASKADENSUMME ausgewiesen (vorher die rohe
        /// Spaltensumme <c>SUM(EingegebenerWert)</c>) — dieselbe Zahl, mit der der
        /// Rechenweg den Betrag bildet. Der Ausweis bleibt damit das, was er sein soll:
        /// die tatsächlich angesetzte Bezugsgröße, nicht eine zweite Rechnung.</para>
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
                if (kategorie == DbWerte.KOSTEN_KATEGORIE_INVESTITION &&
                    string.Equals(bem, DbWerte.BEMESSUNG_PROZENT_INVESTITION, StringComparison.Ordinal))
                    return false;

                EndenergieAufloeser aufloeser = null;
                bool versucht = false;
                // W5‑B‑8: EINE Zeile, also auch nur ein Kaskadenlesen — der Merker steht
                // hier nur, weil RueckfallMenge ihn führt.
                Dictionary<KeyValuePair<int, int>, double> investSummen = null;
                menge = endenergie
                    ? EndenergieMenge(idProjekt, r, bem, ref aufloeser, ref versucht, null, null)
                    : RueckfallMenge(idProjekt, r, bem, ref aufloeser, ref versucht,
                                     ref investSummen, null, null,     // W5-B-11: Ausweis = Erwartungslauf
                                     kategorie == DbWerte.KOSTEN_KATEGORIE_INVESTITION);   // E20

                var p = new DbParam("@m", DbParamTyp.Double);
                p.Wert = menge.HasValue ? (object)menge.Value : DBNull.Value;
                DataRepository.ExecuteSQL(
                    "UPDATE Tab_ProjektWerte SET [" + SchemaKatalog.SPALTE_PW_MENGE +
                    "] = ? WHERE ID = ?",
                    p, new DbParam("@id", positionsId));
                return true;
            }
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannt — der Ausweis ist Anzeige für Dialog und
                // Fremdleser; die Rechenwege lesen die Menge ohnehin frisch. false lässt
                // den gespeicherten Ausweis stehen (keine 0).
                menge = null;
                return false;
            }
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
            catch (Exception)
            {
                // ETAPPE E7c3 (B‑6): benannt — 0 heißt „kein Lauf bekannt": Das Ergebnis
                // gilt dann als nicht auf dem jüngsten Lauf gerechnet (Frischeprüfung),
                // es wird nichts still als aktuell ausgegeben.
            }
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
                                // ETAPPE B7P — der Nachweisumschlag dieser Zeile. Er
                                // entsteht VOR der Parameterkette, weil sein Fehlschlag
                                // den Hinweistext derselben Zeile ergaenzt: Der Anwender
                                // erfaehrt am Lauf, dass die Unterzeilen diesmal nicht
                                // gespeichert sind - nicht erst, wenn er sie vermisst.
                                // Schreiben wirft nicht; die Transaktion bleibt heil.
                                string nwGrund;
                                string nwJson = ErgebnisNachweisUmschlag.Schreiben(e, out nwGrund);
                                string hinweisText = nwGrund == null
                                                     ? e.Hinweis : Anhaengen(e.Hinweis, nwGrund);

                                List<DbParam> pl = new List<DbParam>();
                                pl.Add(new DbParam("@id", naechsteId));
                                pl.Add(new DbParam("@proj", e.IdProjekt));
                                pl.Add(new DbParam("@erg", e.IdErgebnis));
                                pl.Add(new DbParam("@sz", e.Szenario ?? ""));
                                pl.Add(new DbParam("@stamm", e.IstStamm));
                                pl.Add(new DbParam("@anz", e.Anzeige ?? ""));
                                pl.Add(new DbParam("@zeit", DbParamTyp.Date) { Wert = e.Zeitstempel });
                                // ETAPPE W5-B-9: der WIRKSAME Satz dieser Zeile. Bis dahin
                                // stand in allen drei Szenariozeilen dreimal derselbe
                                // Projektwert - seit es Szenarioparameter gibt, waere das
                                // eine Annahme, mit der gar nicht gerechnet wurde.
                                // FuerSzenario gibt fuer ERWARTET p selbst zurueck; dort
                                // ist die Zeile Wert fuer Wert die von vorher.
                                WirtschaftlichkeitParameter pz = p.FuerSzenario(e.Szenario);
                                pl.Add(new DbParam("@z", pz.Zinssatz));
                                // ETAPPE E9a: Zeitraum und Einspeisevergütung DIESES Szenarios
                                // (FuerSzenario, Schritte B und D) — ohne Pflege der Projektwert.
                                pl.Add(new DbParam("@t", pz.Betrachtungszeitraum));
                                pl.Add(new DbParam("@pe", pz.PreissteigerungEnergie));
                                pl.Add(new DbParam("@pb", pz.PreissteigerungBetrieb));
                                pl.Add(new DbParam("@ev", pz.Einspeiseverguetung));
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
                                pl.Add(new DbParam("@stmo", e.StromsteuerBefreiungAlsErloes   // B6
                                    ? DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES
                                    : DbWerte.STROMST_BEFREIUNG_MODUS_AUSWEIS));
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
                                pl.Add(new DbParam("@hw", (object)hinweisText ?? DBNull.Value));
                                pl.Add(new DbParam("@fg", (object)e.Fehlgrund ?? DBNull.Value));
                                pl.Add(new DbParam("@erb", R(e.ErsatzBarwert)));   // W5-B-10
                                pl.Add(new DbParam("@nw", (object)nwJson ?? DBNull.Value));   // B7P
                                v.Ausfuehren("INSERT INTO " + TAB_ERGEBNIS + " (ID, ID_Projekt, ID_Ergebnis, Szenario, " +
                                "IstStamm, Anzeige, Zeitstempel, " +
                                "Zinssatz, Betrachtungszeitraum, Preissteigerung_Energie, Preissteigerung_Betrieb, " +
                                "Einspeiseverguetung, Investition, Betriebskosten, Energiekosten, Einspeiseerloes, " +
                                "BarwertAusgaben, BarwertEinnahmen, Restwert, Kapitalwert, KapitalwertDiff, " +
                                "AnnuitaetKW, AmortisationJahre, Gestehungskosten, " +
                                "IRR, CO2Abgabe, KWKGErloes, " + SPALTE_KWKG_VBH_EL + ", " +
                                SPALTE_ENERGIESTEUER + ", " + SPALTE_STROMST_BEFREIUNG + ", " +
                                SPALTE_STROMST_MODUS + ", " +
                                SPALTE_STROMST_ENTLASTUNG + ", " + SPALTE_STEUER_HERKUNFT + ", " +
                                SPALTE_VERMIEDEN_ARBEIT + ", " + SPALTE_VERMIEDEN_LEISTUNG + ", " +
                                SPALTE_VERMIEDEN_GESAMT + ", " + SPALTE_AUFSCHLAG_BETRAG + ", " +
                                SPALTE_EINSPEISUNG_PV + ", " + SPALTE_EINSPEISUNG_KWK + ", " +
                                SPALTE_ZUSCHUSS + ", " +
                                SPALTE_PV_FORM + ", " + SPALTE_PV_AW + ", " +
                                SPALTE_PV_MARKTPRAEMIE + ", " + SPALTE_PV_AUSFALL_KWH + ", " +
                                SPALTE_PV_AUSFALL_EUR + ", " + SPALTE_PV_51A + ", " +
                                SPALTE_PV_KAPPUNG_KWH + ", " + SPALTE_PV_VERMIEDEN + ", " +
                                "StromkostenTarif, HinweisText, Fehlgrund, " +
                                SPALTE_ERSATZ_BARWERT + ", " + SPALTE_NACHWEIS_JSON + ") " +
                                "VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)", pl.ToArray());
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

                        // Strommengen-Matrix (W3) — EINE Jahreszeile je Projekt (Q11, E7b:
                        // keine Tarifzonen mehr; die Spalte Zone trägt StromMatrix.ZEILE_JAHR).
                        if (matrizen != null && matrizen.Count > 0)
                        {
                            int mxId;
                            {
                                object o = v.Skalar("SELECT MAX(ID) FROM " + TAB_MATRIX);
                                mxId = (o != null && o != DBNull.Value ? Convert.ToInt32(o) : 0) + 1;
                            }
                            foreach (KeyValuePair<int, StromMatrix> kv in matrizen)
                            {
                                StromMatrix m = kv.Value;
                                if (m == null) continue;
                                List<DbParam> pl = new List<DbParam>();
                                pl.Add(new DbParam("@id", mxId));
                                pl.Add(new DbParam("@p", kv.Key));
                                pl.Add(new DbParam("@z", StromMatrix.ZEILE_JAHR));
                                pl.Add(new DbParam("@b", Math.Round(m.BezugGesamtMWh, 3)));
                                pl.Add(new DbParam("@pv", Math.Round(m.EinspeisungPvGesamtMWh, 3)));
                                pl.Add(new DbParam("@ke", Math.Round(m.KwkEigenGesamtMWh, 3)));
                                pl.Add(new DbParam("@ki", Math.Round(m.KwkEinspeisungGesamtMWh, 3)));
                                pl.Add(new DbParam("@mx", Math.Round(m.MaxBezugKW, 1)));
                                pl.Add(new DbParam("@bd", Math.Round(m.BedarfGesamtMWh, 3)));   // E5
                                pl.Add(new DbParam("@zeit", DbParamTyp.Date) { Wert = DateTime.Now });
                                v.Ausfuehren("INSERT INTO " + TAB_MATRIX + " (ID, ID_Projekt, [Zone], " +
                                "BezugMWh, EinspPvMWh, KwkEigenMWh, KwkEinspMWh, MaxBezugKW, " +
                                "BedarfMWh, Zeitstempel) VALUES (?,?,?,?,?,?,?,?,?,?)", pl.ToArray());
                                mxId++;
                            }
                        }
                        v.Commit();
                    }
                    catch (Exception)
                    {
                        // Aufräumen und weiterwerfen — der äußere Fang benennt den Fehler.
                        try { v.Rollback(); }
                        catch (Exception)
                        {
                            // ETAPPE E7c3 (B‑6): ein gescheitertes Rollback ändert nichts
                            // am Ausgang; gemeldet wird der ursprüngliche Fehler (throw).
                        }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): Die Ergebnisse bleiben im Speicher und gelten für die
                // Anzeige dieses Laufs — aber jede Zeile sagt jetzt, dass sie NICHT
                // gespeichert ist; beim nächsten Laden erschiene sonst still der alte Stand.
                string zeile = StufeNichtAusfuehrbar(STUFE_SPEICHERN, Fehlergrund.Text(ex));
                foreach (WirtschaftlichkeitErgebnis e in ergebnisse ?? new List<WirtschaftlichkeitErgebnis>())
                {
                    if (e == null) continue;
                    if (e.KohaerenzHinweise == null) e.KohaerenzHinweise = new List<KohaerenzHinweis>();
                    e.KohaerenzHinweise.Add(new KohaerenzHinweis { Schwere = KohaerenzSchwere.WARNUNG, Text = zeile });
                }
            }
        }

        /// <summary>Persistierte Ergebnisse laden (IWirtschaftlichkeitProvider).</summary>
        public List<WirtschaftlichkeitErgebnis> LadeErgebnisse(List<int> projektIds)
        {
            var liste = new List<WirtschaftlichkeitErgebnis>();
            Ladefehler = null;   // ETAPPE E7c3 (B‑6)
            if (projektIds == null || projektIds.Count == 0) return liste;
            StelleTabellenSicher();
            try
            {
                foreach (int idProjekt in projektIds)
                {
                    // ETAPPE E7c3 (B‑6): der strenge Leseweg — ein Abfragefehler erreicht
                    // den benannten Fang (Ladefehler), statt „nie gerechnet" zu heißen.
                    DataTable dt = StilleDb.TabelleStreng(
                        "SELECT * FROM " + TAB_ERGEBNIS + " WHERE ID_Projekt = ?",
                        new DbParam("@p", idProjekt));
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
                            ErsatzBarwert = D(r, SPALTE_ERSATZ_BARWERT) ?? 0,   // W5-B-10
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
                            // B6: Nur der ausdrueckliche Wert ERLOES; leer und NULL
                            // bedeuten AUSWEIS - so liest sich ein vor B6 gespeicherter
                            // Lauf wie die heutige Vorgabe.
                            StromsteuerBefreiungAlsErloes =
                                string.Equals(Text(r, SPALTE_STROMST_MODUS),
                                              DbWerte.STROMST_BEFREIUNG_MODUS_ERLOES,
                                              StringComparison.Ordinal),
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

                        // ETAPPE B7P — der Nachweisumschlag. ENGER eigener Fang: Der
                        // Fang um die Projektschleife herum verschlucht alles, was aus
                        // ihm herausfliegt — mit ihm wären es ALLE Ergebniszeilen ALLER
                        // Projekte, wegen einer Zeichenkette in EINER Zeile.
                        //
                        // Die Spalte wird tolerant geprüft: Eine nie migrierte Datei hat
                        // sie nicht und lädt unverändert (dann fehlen die Unterzeilen wie
                        // vor B7P, und das ist keine Störung, sondern der alte Stand).
                        // Ein VORHANDENER, aber unlesbarer Umschlag dagegen ist eine
                        // Auskunft: genau EIN Kohärenzhinweis, kein Dialog und keine
                        // Meldung — er läuft über den Weg, den Reiter, Word und Excel
                        // ohnehin zeigen.
                        // ETAPPE E5 (Konzept § 6.3 Nr. 31, entschieden 22.09.2026): Eine
                        // Zeile OHNE Umschlag — Spalte fehlt, NULL oder leer — wird
                        // gekennzeichnet, nicht nachgerechnet. Ein vorhandener, aber
                        // unlesbarer Umschlag sagt es bereits selbst (Kohärenzzeile unten).
                        e.OhneNachweis = !r.Table.Columns.Contains(SPALTE_NACHWEIS_JSON) ||
                                         r[SPALTE_NACHWEIS_JSON] == DBNull.Value ||
                                         string.IsNullOrWhiteSpace(Convert.ToString(r[SPALTE_NACHWEIS_JSON]));

                        if (r.Table.Columns.Contains(SPALTE_NACHWEIS_JSON) &&
                            r[SPALTE_NACHWEIS_JSON] != DBNull.Value)
                        {
                            try
                            {
                                string roh = Convert.ToString(r[SPALTE_NACHWEIS_JSON]);
                                ErgebnisNachweisUmschlag u = ErgebnisNachweisUmschlag.Lesen(roh);
                                if (u != null) u.Uebernimm(e);
                                else if (!string.IsNullOrWhiteSpace(roh))
                                    e.KohaerenzHinweise.Add(new KohaerenzHinweis
                                    {
                                        Schwere = KohaerenzSchwere.HINWEIS,
                                        Text = MyResource.Resource.WIRT_NACHWEIS_UNLESBAR
                                    });
                            }
                            catch (Exception)
                            {
                                // Benannt (schon vor E7c3): der Nachweis ist unlesbar,
                                // die Kernwerte der Zeile bleiben.
                                e.KohaerenzHinweise.Add(new KohaerenzHinweis
                                {
                                    Schwere = KohaerenzSchwere.HINWEIS,
                                    Text = MyResource.Resource.WIRT_NACHWEIS_UNLESBAR
                                });
                            }
                        }
                        liste.Add(e);
                    }
                }
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt statt catch { } — was bis zum Fehler gelesen
                // ist, bleibt; jede geladene Zeile und Ladefehler nennen den Grund, statt
                // dass die übrigen Projekte still fehlen.
                Ladefehler = Fehlergrund.Text(ex);
                string zeile = StufeNichtAusfuehrbar(STUFE_LADEN, Ladefehler);
                foreach (WirtschaftlichkeitErgebnis e in liste)
                {
                    if (e.KohaerenzHinweise == null) e.KohaerenzHinweise = new List<KohaerenzHinweis>();
                    e.KohaerenzHinweise.Add(new KohaerenzHinweis { Schwere = KohaerenzSchwere.WARNUNG, Text = zeile });
                }
            }
            return liste;
        }

        /// <summary>
        /// ETAPPE E7c3 (Befund B‑6): der Grund, aus dem das letzte Laden gespeicherter
        /// Ergebnisse (<see cref="LadeErgebnisse"/>, <see cref="LadeSensitivitaet"/>,
        /// <c>LadeStromMatrix</c>) abbrach — dann fehlen die Zeilen danach; <c>null</c> =
        /// vollständig geladen. Jeder der drei Aufrufe setzt ihn neu; gelesen wird er
        /// unmittelbar nach dem Aufruf.
        /// </summary>
        public string Ladefehler { get; private set; }

        /// <summary>Persistierte Sensitivitätszeilen laden (IWirtschaftlichkeitProvider, W2).</summary>
        public List<SensitivitaetZeile> LadeSensitivitaet(List<int> projektIds)
        {
            var liste = new List<SensitivitaetZeile>();
            Ladefehler = null;   // ETAPPE E7c3 (B‑6): gilt für DIESES Laden
            if (projektIds == null || projektIds.Count == 0) return liste;
            StelleTabellenSicher();
            try
            {
                foreach (int idProjekt in projektIds)
                {
                    DataTable dt = StilleDb.TabelleStreng(   // E7c3 (B‑6): streng
                        "SELECT * FROM " + TAB_SENS + " WHERE ID_Projekt = ? ORDER BY ID",
                        new DbParam("@p", idProjekt));
                    foreach (DataRow r in dt.Rows)
                    {
                        var z = new SensitivitaetZeile
                        {
                            IdProjekt = idProjekt,
                            Parameter = r["Parameter"] != DBNull.Value ? r["Parameter"].ToString() : "",
                            KwMinus = D(r, "KwMinus"),
                            KwBasis = D(r, "KwBasis"),
                            KwPlus = D(r, "KwPlus")
                        };
                        // ETAPPE E5 (V‑A): Die Stufe steht im Text der Zeile — daraus
                        // entsteht die Steigung, ohne dass eine Spalte dazukommt.
                        double schritt;
                        bool punkte;
                        if (SensitivitaetZeile.SchrittAusParameter(z.Parameter, out schritt, out punkte))
                        {
                            z.Schritt = schritt;
                            z.SchrittInProzentpunkten = punkte;
                        }
                        liste.Add(z);
                    }
                }
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt — was gelesen ist, bleibt; der Grund steht in
                // Ladefehler statt still fehlender Sensitivitätszeilen.
                Ladefehler = Fehlergrund.Text(ex);
            }
            return liste;
        }

        /// <summary>
        /// Persistierte Strommengen-Matrizen laden (IWirtschaftlichkeitProvider, W3).
        ///
        /// <para><b>Summiert ALLE Zeilen eines Projekts</b> (Q11, E7b). Geschrieben wird
        /// eine Jahreszeile (<see cref="StromMatrix.ZEILE_JAHR"/>); ein Stand von vor E7b
        /// trägt vier Zeilen, je Tarifzone eine. Beide ergeben so dieselben
        /// Jahressummen — ein alter Stand wird weder falsch gelesen noch als Zone
        /// gezeigt, bis Schemaschritt 104 ihn zusammenfasst oder ein neuer Lauf ihn
        /// ersetzt. Die höchste Stundenlast ist das Maximum der Zeilen.</para>
        /// </summary>
        public Dictionary<int, StromMatrix> LadeStromMatrix(List<int> projektIds)
        {
            var map = new Dictionary<int, StromMatrix>();
            Ladefehler = null;   // ETAPPE E7c3 (B‑6): gilt für DIESES Laden
            if (projektIds == null || projektIds.Count == 0) return map;
            StelleTabellenSicher();
            try
            {
                foreach (int idProjekt in projektIds)
                {
                    DataTable dt = StilleDb.TabelleStreng(   // E7c3 (B‑6): streng
                        "SELECT * FROM " + TAB_MATRIX + " WHERE ID_Projekt = ? ORDER BY ID",
                        new DbParam("@p", idProjekt));
                    if (dt.Rows.Count == 0) continue;

                    var m = new StromMatrix();
                    bool gelesen = false;
                    foreach (DataRow r in dt.Rows)
                    {
                        string zeile = r["Zone"] != DBNull.Value ? r["Zone"].ToString() : "";
                        if (zeile.Length == 0) continue;
                        m.BezugGesamtMWh += D(r, "BezugMWh") ?? 0;
                        m.EinspeisungPvGesamtMWh += D(r, "EinspPvMWh") ?? 0;
                        m.KwkEigenGesamtMWh += D(r, "KwkEigenMWh") ?? 0;
                        m.KwkEinspeisungGesamtMWh += D(r, "KwkEinspMWh") ?? 0;
                        m.BedarfGesamtMWh += D(r, "BedarfMWh") ?? 0;        // E5
                        double mx = D(r, "MaxBezugKW") ?? 0;
                        if (mx > m.MaxBezugKW) m.MaxBezugKW = mx;
                        gelesen = true;
                    }
                    if (gelesen) map[idProjekt] = m;
                }
            }
            catch (Exception ex)
            {
                // ETAPPE E7c3 (B‑6): benannt — der Grund steht in Ladefehler.
                Ladefehler = Fehlergrund.Text(ex);
            }
            return map;
        }

        /// <summary>
        /// true, wenn ein gespeichertes Ergebnis zum aktuellen Simulationslauf passt.
        ///
        /// <para><b>Die Frage ist NUR der Simulationsstand</b> (Anwenderbefund
        /// 22.09.2026). Bis dahin stand hier zusätzlich <c>Fehlgrund == null</c> — eine
        /// Zeile mit Fehlgrund galt damit als „veraltet“ und wurde von jedem Aufrufer,
        /// der beides zugleich prüfte, wieder herausgefiltert: Die Statuszeile
        /// „Gespeicherte Ergebnisse passen nicht mehr zum Simulationsstand“ erschien für
        /// solche Zeilen NIE. Ob eine Kennzahl fehlt, sagt der Fehlgrund; ob das
        /// Ergebnis zum Lauf passt, sagt diese Methode. Wer beides braucht, fragt
        /// beides — die zwei Berichtsstellen tun das unverändert.</para>
        /// </summary>
        public bool ErgebnisAktuell(WirtschaftlichkeitErgebnis e)
        {
            return e != null &&
                   e.IdErgebnis > 0 && e.IdErgebnis == LiesErgebnisId(e.IdProjekt);
        }

        // ------------------------------------------------------------- Hilfen

        internal static bool B(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return false;
            try { return Convert.ToBoolean(r[spalte]); }
            catch (Exception ex) when (IstZahlfehler(ex)) { return false; }   // E7c3 (B‑6): benannt
        }

        internal static double? D(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[spalte]); }
            catch (Exception ex) when (IstZahlfehler(ex)) { return null; }    // E7c3 (B‑6): benannt
        }

        /// <summary>Textspalte, tolerant gegen fehlende Spalte und NULL (Etappe E3).</summary>
        internal static string Text(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return "";
            try { return Convert.ToString(r[spalte]).Trim(); }
            catch (Exception ex) when (IstZahlfehler(ex)) { return ""; }      // E7c3 (B‑6): benannt
        }

        private static double R(double v, int dez = 2) { return Math.Round(v, dez); }

        private static DbParam DbWert(double? v, int dez = 2)
        {
            return new DbParam("@w", DbParamTyp.Double)
            { Wert = v.HasValue ? (object)Math.Round(v.Value, dez) : DBNull.Value };
        }
    }
}
