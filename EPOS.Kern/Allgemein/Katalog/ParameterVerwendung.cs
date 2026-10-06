using System;
using System.Linq;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Welche der acht Anlagenarten mit eigener Katalogverwaltung gemeint ist
    /// (Anwenderwunsch W14a-E-8, 06.09.2026; die achte kam am selben Tag mit dem
    /// Wechselrichterkatalog, W6-E-2).
    ///
    /// <para>Sie sind genau die Punkte unter „Administration", hinter denen
    /// ein Geraetekatalog steht: Heizkessel, BHKW, Waermepumpe, Solarkollektoren,
    /// PV-Module, Stromspeicher, Pufferspeicher und Wechselrichter. Ein AUFZAEHLUNGSTYP und keine
    /// Zeichenkette — dieselbe Begruendung wie bei <see cref="KatalogBrowserArt"/>
    /// und <see cref="ModulKatalogArt"/>.</para>
    /// </summary>
    public enum Anlagenart
    {
        /// <summary><c>Tab_Heizkessel_STAMM</c>.</summary>
        Heizkessel,

        /// <summary><c>Tab_BHKW_STAMM</c>.</summary>
        Bhkw,

        /// <summary><c>Tab_WP_STAMM</c> (die Kennfelder stehen in <c>Tab_Kenndaten_STAMM</c>).</summary>
        Waermepumpe,

        /// <summary><c>Tab_Solarkollektoren_STAMM</c>.</summary>
        Solarkollektoren,

        /// <summary><c>Tab_PV_STAMM</c>.</summary>
        Photovoltaik,

        /// <summary><c>Tab_Stromspeicher_STAMM</c>.</summary>
        Stromspeicher,

        /// <summary><c>Tab_Pufferspeicher_STAMM</c>.</summary>
        Pufferspeicher,

        /// <summary>
        /// <c>Tab_Wechselrichter_STAMM</c> — der ACHTE Katalog, seit dem
        /// Anwenderentscheid W6-E-2 vom 06.09.2026 (Stufe S1 des
        /// Konzept_Wechselrichter_EPOS-Plan.md).
        /// </summary>
        Wechselrichter,

        /// <summary>
        /// <c>Tab_Kaeltemaschine_STAMM</c> (die Kennlinie steht in <c>Tab_Kenndaten_Kaeltemaschine_STAMM</c>) —
        /// der NEUNTE Katalog (KU3-1). Er hat eine Verwaltung und ein Filterprofil; im
        /// <see cref="ParameterVerwendung.AlleArten">Verwendungskatalog</see> steht er erst, wenn der
        /// Rechenweg ihn liest — ohne gerechnete Spalte waere die Einstufung eine Behauptung.
        /// </summary>
        Kaeltemaschine
    }

    /// <summary>
    /// <b>Wofuer ein Katalogparameter im Programm gebraucht wird</b> (W14a-E-8).
    ///
    /// <para>Die fuenf Stufen sind AUSSCHLIESSEND gemeint und werden von aussen nach
    /// innen geprueft: Wer im Rechenweg gelesen wird, ist <see cref="Simulation"/> —
    /// auch wenn er daneben im Bericht steht. Ein Parameter kann mehrere Stufen
    /// tragen; <see cref="Keine"/> steht immer allein.</para>
    /// </summary>
    public enum Verwendung
    {
        /// <summary>
        /// Der Rechenweg liest ihn: <c>EPOS.Kern/Allgemein/Simulation/**</c>,
        /// <c>SpeicherEngine</c> oder <c>Controller/StromspeicherSimCtrl</c>.
        /// </summary>
        Simulation,

        /// <summary>
        /// Kosten, Erloese oder Emissionsbilanz lesen ihn:
        /// <c>Allgemein/Wirtschaftlichkeit/**</c>, <c>Allgemein/Bericht/KostenEmissionRechner</c>
        /// oder <c>Controller/TechnikPlanwertCtrl</c> (die Kostenplanwerte).
        /// </summary>
        Wirtschaftlichkeit,

        /// <summary>
        /// Er steht in einer Berichtsausgabe — heute durchweg in
        /// <c>Allgemein/Bericht/AbweichungsErmittler.Felder</c>, dem einzigen Ort, der
        /// Geraetespalten NAMENTLICH in den Bericht traegt.
        /// </summary>
        Bericht,

        /// <summary>
        /// Er wird nur angezeigt oder gepflegt — eine Maske schreibt und liest ihn,
        /// weiter kommt er nicht.
        /// </summary>
        Dialog,

        /// <summary>
        /// Niemand liest ihn: kein Rechenweg, kein Bericht, keine Maske. Er entsteht
        /// beim Herstellerimport oder beim Kopieren ins Projekt und bleibt liegen.
        /// </summary>
        Keine
    }

    /// <summary>
    /// Ein Parameter eines Anlagenkatalogs samt seiner Verwendung (W14a-E-8).
    /// </summary>
    /// <param name="Spalte">
    /// Der Spaltenname in der Stammtabelle — sprachneutral und zugleich der Schluessel,
    /// unter dem die Huelle den Wert liefert.
    /// </param>
    /// <param name="Anzeigetext">
    /// Die Beschriftung, bereits uebersetzt. Sie kommt aus DEMSELBEN Ressourcenschluessel
    /// wie im Katalogdialog — es gibt fuer einen Parameter genau einen Text im Haus.
    /// </param>
    /// <param name="Einheit">Die Einheit hinter dem Wert, sprachneutral; leer, wo es keine gibt.</param>
    /// <param name="Verwendung">
    /// Eine oder mehrere Stufen; <see cref="WindowsFormsApplication1.Verwendung.Keine"/>
    /// steht allein.
    /// </param>
    /// <param name="Fundstelle">
    /// Der BELEG: Datei und Symbol („Datei.Methode“, keine Zeilennummer), an dem der Wert gelesen wird — leer nur bei
    /// <see cref="WindowsFormsApplication1.Verwendung.Keine"/>. Die Kern-Probe
    /// <c>ParameterVerwendungTests</c> faellt rot aus, sobald eine als
    /// <see cref="WindowsFormsApplication1.Verwendung.Simulation"/> eingestufte Spalte
    /// keine Fundstelle nennt. Der Variantenvergleich wird über sein Merkmal belegt
    /// („AbweichungsErmittler.Felder (Tabelle.Spalte)“), nicht über eine Zeilennummer;
    /// <c>ParameterVerwendungTests</c> prüft, dass der Ermittler dieses Merkmal führt
    /// und dass jedes genannte Symbol in seiner Datei vorkommt.
    /// </param>
    public sealed record ParameterEintrag(string Spalte, string Anzeigetext, string Einheit,
                                          Verwendung[] Verwendung, string Fundstelle)
    {
        /// <summary>Traegt der Eintrag diese Stufe?</summary>
        public bool Hat(Verwendung stufe)
        {
            if (Verwendung == null) return false;
            for (int i = 0; i < Verwendung.Length; i++)
                if (Verwendung[i] == stufe) return true;
            return false;
        }

        /// <summary>
        /// Wird der Parameter GERECHNET — Simulation oder Wirtschaftlichkeit? Genau
        /// diese Menge prueft Teil 3 des Anwenderwunsches gegen das Bearbeiten-Formular.
        /// </summary>
        public bool Gerechnet =>
            Hat(WindowsFormsApplication1.Verwendung.Simulation) ||
            Hat(WindowsFormsApplication1.Verwendung.Wirtschaftlichkeit);
    }

    /// <summary>
    /// <b>Der Verwendungskatalog der sieben Anlagenarten</b> (Anwenderwunsch W14a-E-8
    /// vom 06.09.2026: „Für alle Menüs mit Anlagendaten: … 1. alle verfügbaren
    /// Parameter und Eigenschaften angezeigt werden und 2. alle verwendeten Parameter
    /// gekennzeichnet sind").
    ///
    /// <para><b>Eine Wahrheit, kein zweiter Text.</b> Der Katalog nennt fuer JEDE
    /// Spalte der sieben Stammtabellen, wofuer sie gebraucht wird, und belegt jede
    /// Einstufung mit Datei und Zeile. Die Beschriftungen holt er ueber denselben
    /// Uebersetzer und dieselben Schluessel wie <see cref="KatalogBrowserProfil"/> und
    /// <see cref="ModulKatalogProfil"/> — waeren es zweite Texte, liefen sie beim
    /// ersten Fachwechsel auseinander.</para>
    ///
    /// <para><b>Warum im Kern und nicht in der Oberflaeche.</b> Die Aussage „dieser
    /// Wert geht in die Simulation" ist eine FACHaussage ueber den Rechenweg, keine
    /// Anzeigeentscheidung. Windows und iOS zeigen dieselbe Liste; die Razor-Komponente
    /// <c>Parameteruebersicht</c> malt nur, was hier steht.</para>
    ///
    /// <para><b>Der Stand ist der vom 06.09.2026</b> und wird von
    /// <c>EPOS.Kern.Tests/ParameterVerwendungTests</c> gegen <c>pragma table_info</c>
    /// der Testdatenbank gehalten: keine vergessene, keine erfundene Spalte. Wer eine
    /// Spalte anlegt, traegt sie hier ein — sonst faellt die Probe rot aus.</para>
    /// </summary>
    public static class ParameterVerwendung
    {
        /// <summary>Was ein nicht gepflegter Wert in der Uebersicht anzeigt.</summary>
        /// <remarks>
        /// Derselbe Strich wie <see cref="PhotovoltaikStammCtrl.PARAMETER_LEER"/> aus
        /// dem Anwenderwunsch W6-E-1 — ein Halbgeviertstrich, kein Bindestrich und
        /// keine 0: NULL ist etwas anderes als eine gemessene Null.
        /// </remarks>
        public const string LEER = "–";

        // =================================================================
        // Die Stammtabellen
        // =================================================================

        /// <summary>Die Stammtabelle einer Anlagenart.</summary>
        public static string Stammtabelle(Anlagenart art)
        {
            switch (art)
            {
                case Anlagenart.Heizkessel: return HeizkesselStammCtrl.TABLE;
                case Anlagenart.Bhkw: return BHKWStammCtrl.TABLE;
                case Anlagenart.Waermepumpe: return WPStammCtrl.TABLE;
                case Anlagenart.Solarkollektoren: return SolarkollektorenStammCtrl.TABLE;
                case Anlagenart.Photovoltaik: return PhotovoltaikStammCtrl.TABLE;
                case Anlagenart.Stromspeicher: return StromspeicherStammCtrl.TABLE;
                case Anlagenart.Pufferspeicher: return PufferSpStammCtrl.TABLE;
                case Anlagenart.Wechselrichter: return WechselrichterStammCtrl.TABLE;
                case Anlagenart.Kaeltemaschine: return KaeltemaschineStammCtrl.TABLE;
            }
            throw new ArgumentOutOfRangeException(nameof(art));
        }

        /// <summary>Alle acht Auspraegungen — fuer Stapelpruefungen und die Doku.</summary>
        public static IEnumerable<Anlagenart> AlleArten
        {
            get
            {
                yield return Anlagenart.Heizkessel;
                yield return Anlagenart.Bhkw;
                yield return Anlagenart.Waermepumpe;
                yield return Anlagenart.Solarkollektoren;
                yield return Anlagenart.Photovoltaik;
                yield return Anlagenart.Stromspeicher;
                yield return Anlagenart.Pufferspeicher;
                yield return Anlagenart.Wechselrichter;
                yield return Anlagenart.Kaeltemaschine;
            }
        }

        // =================================================================
        // Der Katalog
        // =================================================================

        /// <summary>
        /// Alle Parameter einer Anlagenart in der Reihenfolge der Stammtabelle.
        /// <paramref name="text"/> uebersetzt einen Beschriftungsschluessel;
        /// <c>null</c> liefert den Schluessel selbst zurueck (fuer Tests und fuer eine
        /// Umgebung ohne Katalog) — dasselbe Vorgehen wie
        /// <see cref="KatalogBrowserProfil.Finde"/>.
        /// </summary>
        public static IReadOnlyList<ParameterEintrag> Katalog(Anlagenart art, Func<string, string> text = null)
        {
            Func<string, string> t = text ?? (s => s);

            switch (art)
            {
                case Anlagenart.Heizkessel: return Heizkessel(t);
                case Anlagenart.Bhkw: return Bhkw(t);
                case Anlagenart.Waermepumpe: return Waermepumpe(t);
                case Anlagenart.Solarkollektoren: return Solarkollektoren(t);
                case Anlagenart.Photovoltaik: return Photovoltaik(t);
                case Anlagenart.Stromspeicher: return Stromspeicher(t);
                case Anlagenart.Pufferspeicher: return Pufferspeicher(t);
                case Anlagenart.Wechselrichter: return Wechselrichter(t);
                case Anlagenart.Kaeltemaschine: return Kaeltemaschine(t);
            }
            throw new ArgumentOutOfRangeException(nameof(art));
        }

        // --- Abkuerzungen, damit die Tabellen lesbar bleiben ---------------

        private static readonly Verwendung[] SIM = { Verwendung.Simulation };
        private static readonly Verwendung[] WIRT = { Verwendung.Wirtschaftlichkeit };
        private static readonly Verwendung[] BER = { Verwendung.Bericht };
        private static readonly Verwendung[] DLG = { Verwendung.Dialog };
        private static readonly Verwendung[] NIX = { Verwendung.Keine };
        private static readonly Verwendung[] SIM_BER = { Verwendung.Simulation, Verwendung.Bericht };
        private static readonly Verwendung[] SIM_WIRT = { Verwendung.Simulation, Verwendung.Wirtschaftlichkeit };
        private static readonly Verwendung[] SIM_WIRT_BER =
            { Verwendung.Simulation, Verwendung.Wirtschaftlichkeit, Verwendung.Bericht };

        /// <summary>
        /// KU1 Stufe 1 und 2 (Schemaschritte <see cref="KatalogfassungSchema"/> und
        /// <see cref="KatalogfassungStufe2Schema"/>): die drei Katalogspalten der Kataloge des Registers,
        /// in der Reihenfolge der Tabelle die letzten. Kein Fachwert — die
        /// Kennung des Auslieferungssatzes, die der Katalogabgleich liest und schreibt.
        /// </summary>
        private static IEnumerable<ParameterEintrag> Katalogspalten(Func<string, string> t) => new[]
        {
            E(Katalogfassung.SPALTE_SCHLUESSEL, t("PARV_LBL_KATALOG_SCHLUESSEL"), "", NIX,
              "Katalogabgleich (stabile Kennung des Auslieferungssatzes; Anwendersatz leer)"),
            E(Katalogfassung.SPALTE_PRUEFSUMME, t("PARV_LBL_KATALOG_PRUEFSUMME"), "", NIX,
              "Katalogabgleich (Pruefsumme des ausgelieferten Stands: gleich = nicht angepasst)"),
            E(Katalogfassung.SPALTE_AUSGELAUFEN, t("PARV_LBL_KATALOG_AUSGELAUFEN"), "", NIX,
              "Katalogabgleich (Satz in einer spaeteren Auslieferung entfallen, bleibt stehen)"),
        };

        private static ParameterEintrag E(string spalte, string anzeige, string einheit,
                                          Verwendung[] verwendung, string fundstelle = "")
        {
            return new ParameterEintrag(spalte, anzeige, einheit ?? "", verwendung, fundstelle ?? "");
        }

        // =================================================================
        // 1. Heizkessel — Tab_Heizkessel_STAMM (29 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Der Befund dieser Tabelle</b> (W14a-E-8-B1, entschieden 07.09.2026): Die
        /// fuenf Emissionsspalten des Kessels (<c>CO2</c> … <c>Staub</c>) werden
        /// GEPFLEGT, aber nicht gerechnet. Der Rechenweg holt seine Faktoren seit dem
        /// Entscheid aus DER EINEN Quelle — dem Emissionskatalog des Energietraegers
        /// (<c>Emissionsquelle</c> ueber <c>EmissionsFaktorLader</c>) —, und die fuenf
        /// Katalogspalten sind ausdruecklich <b>nur Anzeige</b>: Sie stehen in g/MWh,
        /// haben kein CO2-Aequivalent und keine Herkunft. Sie bleiben als
        /// Herstellerangabe erhalten.
        ///
        /// <para><b>Die Pflegestelle ist seit dem 15.09.2026 der AUFKLAPPER</b> „Alle
        /// Daten" im Modulbereich des <c>HeizkesselDialog</c>, nicht mehr der
        /// Katalogeditor: Kosten, Wartung, Raumbedarf, Nutzungsdauer und die fuenf
        /// Emissionsspalten sind aus dem Bearbeiten-Dialog gefallen, und der Aufklapper
        /// zeigt statt ihrer ALLE editierbaren Spalten des
        /// <see cref="KatalogBrowserProfil"/>. Die Fundstellen unten nennen deshalb das
        /// PROFIL — es ist die eine Stelle, die entscheidet, was der Aufklapper
        /// zeigt.</para>
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Heizkessel(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM,
                  "Tab_Energieanlagen.ID_Kessel → SimulationControl.KesselTemperaturpaar"),
                E("Bezeichner", t("HZKK_LBL_NAME"), "", SIM_BER,
                  "SimulationSPK.Kesseldaten_Einlesen; AbweichungsErmittler.Felder (Tab_Heizkessel.Bezeichner)"),
                E("Firma", t("HZKK_LBL_HERSTELLER"), "", BER,
                  "AbweichungsErmittler.Felder (Tab_Heizkessel.Firma)"),
                E("Beschreibung", t("HZKK_LBL_BESCHREIBUNG"), "", SIM,
                  "HeizkesselKatalogDialog.razor (Feld Beschreibung); SimulationSPK.Kesseldaten_Einlesen " +
                  "(Kesselkennlinie.Bauart: VDI-Bauart „Standard…“ fuer die Normvorgabe von eta30)"),
                E("Ptherm", t("HZKK_LBL_PTHERM"), "kW", SIM_WIRT_BER,
                  "SimulationSPK.Kesseldaten_Einlesen; WirtschaftlichkeitCtrl.LiesReferenzkessel; AbweichungsErmittler.Felder (Tab_Heizkessel.Ptherm)"),
                E("Brennstoff", t("HZKK_LBL_ENERGIETRAEGER"), "", SIM_WIRT,
                  "SimulationSPK.Kesseldaten_Einlesen; WirtschaftlichkeitCtrl.LiesReferenzkessel"),
                E("Wirkungsgrad_Gas", t("HZKK_LBL_WG_GAS"), "", SIM_WIRT_BER,
                  "SimulationSPK.Kesseldaten_Einlesen; WirtschaftlichkeitCtrl.LiesReferenzkessel; AbweichungsErmittler.Felder (Tab_Heizkessel.Wirkungsgrad_Gas)"),
                E("Wirkungsgrad_Öl", t("HZKK_LBL_WG_OEL"), "", SIM_WIRT_BER,
                  "SimulationSPK.Kesseldaten_Einlesen; WirtschaftlichkeitCtrl.LiesReferenzkessel; AbweichungsErmittler.Felder (Tab_Heizkessel.Wirkungsgrad_Öl)"),
                E("Investitionskosten", t("HZKK_LBL_INVEST"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen (ERZEUGER_HEIZKESSEL)"),
                E("Raumbedarf", t("HZKK_LBL_RAUMBEDARF"), "m³", DLG,
                  "KatalogBrowserProfil (Heizkessel) - Aufklapper „Alle Daten“ des HeizkesselDialog"),
                E("Wartungskosten", t("KESSEL_WARTUNG_LBL"), "", WIRT,
                  "TechnikPlanwertCtrl.KesselPlanwert (Betriebskosten-Planwert)"),
                E("Nutzungsdauer", t("HZKK_LBL_NUTZUNGSDAUER"), t("HZKK_EINHEIT_JAHRE"), DLG,
                  "KatalogBrowserProfil (Heizkessel) - Aufklapper „Alle Daten“ des HeizkesselDialog" +
                  " — Geraetedaten, nicht rechenwirksam; massgeblich ist die Nutzungsdauertabelle (A8, E10)"),
                E("CO2", "CO2:", "g / MWh", DLG,
                  "KatalogBrowserProfil (Heizkessel) - Aufklapper „Alle Daten“ — nur Anzeige (W14a-E-8-B1); der Lauf nimmt den Emissionskatalog des Energietraegers"),
                E("SO2", "SO2:", "g / MWh", DLG,
                  "KatalogBrowserProfil (Heizkessel) - Aufklapper „Alle Daten“ — nur Anzeige (W14a-E-8-B1); der Lauf nimmt den Emissionskatalog des Energietraegers"),
                E("NOx", "NOx:", "g / MWh", DLG,
                  "KatalogBrowserProfil (Heizkessel) - Aufklapper „Alle Daten“ — nur Anzeige (W14a-E-8-B1); der Lauf nimmt den Emissionskatalog des Energietraegers"),
                E("CO", "CO:", "g / MWh", DLG,
                  "KatalogBrowserProfil (Heizkessel) - Aufklapper „Alle Daten“ — nur Anzeige (W14a-E-8-B1); der Artenkatalog fuehrt kein CO"),
                E("Staub", t("HZKK_LBL_STAUB"), "g / MWh", DLG,
                  "KatalogBrowserProfil (Heizkessel) - Aufklapper „Alle Daten“ — nur Anzeige (W14a-E-8-B1); der Lauf nimmt den Emissionskatalog des Energietraegers"),
                E("Betriebsbereitschaftverlust", t("HZKK_LBL_BBVERLUST"), "kW / %", SIM,
                  "SimulationSPK.cs (BereitschaftsleistungKw, Stunde_Abschluss) - Leistung je Stillstandsstunde, " +
                  "in der Einheit Bereitschaft_Einheit (KesselBereitschaft.LeistungKw)"),
                E("Brennwert", t("HZKK_LBL_BRENNWERT"), "", SIM_BER,
                  "AbweichungsErmittler.Felder (Tab_Heizkessel.Brennwert); SimulationSPK.Kesseldaten_Einlesen " +
                  "(Kesselkennlinie.Bauart: Brennwertkessel fuer die Normvorgabe von eta30)"),
                E("Vorlauf", t("HZKK_LBL_VORLAUF"), "°C", SIM,
                  "SimulationControl.KesselTemperaturpaarGepflegt; Warnkriterien.KesselVorlauf"),
                E("Ruecklauf", t("HZKK_LBL_RUECKLAUF"), "°C", SIM,
                  "SimulationControl.KesselTemperaturpaarGepflegt; Warnkriterien.KesselVorlauf"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "HeizkesselStammCtrl.Ueberschreiben (Schreibschutz der Auslieferung)"),
                E("Wartungskosten_Einheit", t("KESSEL_WARTUNG_EINHEIT_LBL"), "", WIRT,
                  "TechnikPlanwertCtrl.KesselPlanwert (Bezugsgroesse der Wartungskosten)"),

                // Die Kennlinie (Konzept Kesselkennlinie 3.1): gepflegt in Editor und Aufklapper,
                // gelesen vom Import (Satz 710.01). eta30 rechnet mit der Teillastkennlinie (E2), der
                // Schalter der Brennwertkennlinie mit dem Ruecklauf (E3), Mindestleistung,
                // Anfahrverlust und Mindestlaufzeit mit dem Takten (E4) - leer je die Normvorgabe.
                E("Wirkungsgrad_Teillast30", t("HZKK_LBL_TEILLAST30"), "", SIM,
                  "SimulationSPK.Stunde_Abschluss (Kesselkennlinie.Eta30Wirksam, leer = Normvorgabe nach Bauart); " +
                  "HeizkesselKatalogDialog.razor (Gruppe Kennlinie); KatalogBrowserProfil (Heizkessel)"),
                E("Kennlinie_Brennwert", t("HZKK_LBL_KENNLINIE_BRENNWERT"), "", SIM,
                  "SimulationSPK.Kesseldaten_Einlesen (Kesselkennlinie.RechnetMitBrennwertkennlinie) und " +
                  "Stunde_Abschluss (Kesselkennlinie.EtaBrennwert, Ruecklaufkette); " +
                  "HeizkesselKatalogDialog.razor (Gruppe Kennlinie); KatalogBrowserProfil (Heizkessel)"),
                E("Mindestleistung", t("HZKK_LBL_MINDESTLEISTUNG"), "kW", SIM,
                  "SimulationSPK.TaktwerteBilden und Stunde_Abschluss (Kesselkennlinie.MindestleistungWirksam, Taktet; " +
                  "leer = Normvorgabe); HeizkesselKatalogDialog.razor (Gruppe Kennlinie); KatalogBrowserProfil (Heizkessel)"),
                E("Anfahrverlust_kWh", t("HZKK_LBL_ANFAHRVERLUST"), "kWh", SIM,
                  "SimulationSPK.TaktwerteBilden und Stunde_Abschluss (Kesselkennlinie.AnfahrverlustWirksam; " +
                  "leer = Normvorgabe); HeizkesselKatalogDialog.razor (Gruppe Kennlinie); KatalogBrowserProfil (Heizkessel)"),
                E("Mindestlaufzeit_min", t("HZKK_LBL_MINDESTLAUFZEIT"), "min", SIM,
                  "SimulationSPK.TaktwerteBilden und Stunde_Abschluss (Kesselkennlinie.MindestlaufzeitWirksam, StartsImTakt; " +
                  "leer = Normvorgabe); HeizkesselKatalogDialog.razor (Gruppe Kennlinie); KatalogBrowserProfil (Heizkessel)"),

                // Die Einheit des Bereitschaftsverlusts (Anwenderentscheid 02.10.2026, Schemaschritt
                // KesselBereitschaftEinheitSchema.SCHRITT) - in der Reihenfolge der Tabelle die letzte Spalte.
                E(KesselBereitschaftEinheitSchema.SPALTE, t("HZKK_LBL_BB_EINHEIT"), "", SIM,
                  "KesselBereitschaft.LeistungKw (kW oder % der Nennleistung); HeizkesselKatalogDialog.razor; " +
                  "KatalogBrowserProfil (Heizkessel)")
            }.Concat(Katalogspalten(t)).ToList();
        }

        // =================================================================
        // 2. BHKW — Tab_BHKW_STAMM (33 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Der Befund dieser Tabelle:</b> <c>Investition_kwel</c> ist seit dem
        /// Nutzerentscheid vom 22.08.2026 ABGELEITET (<c>BHKWKosten.JeKWel</c> aus den
        /// fuenf Einzelposten) und hat im Rechenweg keinen Leser — die Kostenplanung
        /// rechnet mit <c>Kosten_Modul</c> und den vier Nebenposten
        /// (<c>TechnikPlanwertCtrl.BasenFuellen</c>). Seit dem Anwenderentscheid
        /// <b>W14a-E-8-B3</b> vom 07.09.2026 ist er aber keine Dublette mehr, sondern
        /// die AUSGERECHNETE ANZEIGE der fuenf Posten; gespeichert wird immer nur, was
        /// in den Posten steht. Seit dem Anwenderentscheid vom 15.09.2026 steht er im
        /// AUFKLAPPER „Alle Daten" und ist dort ausdruecklich NICHT editierbar — zwei
        /// Eingabewege fuer dasselbe Geld liessen sich widersprechen. Er bleibt
        /// <c>Dialog</c>.
        ///
        /// <para><b>Die Pflegestelle ist seit dem 15.09.2026 der AUFKLAPPER</b> „Alle
        /// Daten" im Modulbereich des <c>BhkwDialog</c>: Aus dem Katalogeditor sind die
        /// Gruppen Kosten, BEHG und Emissionsfaktoren ersatzlos gefallen; der
        /// Aufklapper zeigt dafuer ALLE editierbaren Spalten des
        /// <see cref="KatalogBrowserProfil"/>. Die Fundstellen unten nennen deshalb das
        /// PROFIL.</para>
        ///
        /// <para><b>Der zweite Befund, seit W14a-E-8-B1 (07.09.2026):</b> Die fuenf
        /// Emissionsspalten (<c>CO2</c>, <c>SO2</c>, <c>NOX</c>, <c>CO</c>,
        /// <c>Staub</c>) sind von <c>Simulation</c> auf <c>Dialog</c> gefallen. Bis
        /// dahin war das BHKW der EINZIGE Erzeuger, dessen Lauf Geraetespalten als
        /// Emissionsfaktoren las (<c>SimulationBHKW.Moduldaten_Einlesen</c>) — waehrend
        /// die Emissionsbilanz desselben Projekts mit dem Katalogwert des
        /// Energietraegers rechnete. Der Anwender hat das aufgeloest: EIN Faktor, der
        /// des Energietraegers; die Geraetespalten bleiben als Herstellerangabe und
        /// sind „nur Anzeige".</para>
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Bhkw(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM_WIRT,
                  "Tab_Energieanlagen.ID_BHKW → WirtschaftlichkeitCtrl.AnlagenTabelle; TechnikPlanwertCtrl.Plaene"),
                E("Bezeichner", t("BHKWK_LBL_NAME"), "", SIM_BER,
                  "SimulationBHKW.Moduldaten_Einlesen (ReadSingle); AbweichungsErmittler.Felder (Tab_BHKW.Bezeichner)"),
                E("Firma", t("BHKWK_LBL_HERSTELLER"), "", BER,
                  "AbweichungsErmittler.Felder (Tab_BHKW.Firma)"),
                E("Beschreibung", t("BHKWK_LBL_BESCHREIBUNG"), "", DLG,
                  "BhkwKatalogDialog.razor (Feld Beschreibung)"),
                E("Ptherm", t("BHKWK_LBL_PTHERM"), "kW", SIM_WIRT_BER,
                  "SimulationBHKW.Moduldaten_Einlesen; KostenEmissionRechner.AnschlussleistungKW; AbweichungsErmittler.Felder (Tab_BHKW.Ptherm)"),
                E("Pel", t("BHKWK_LBL_PEL"), "kW", SIM_WIRT_BER,
                  "SimulationBHKW.Moduldaten_Einlesen; WirtschaftlichkeitCtrl.LiesBhkwLeistungKW (KWKG-Deckel); AbweichungsErmittler.Felder (Tab_BHKW.Pel)"),
                E("Brennstoff", t("BHKWK_LBL_ENERGIETRAEGER"), "", SIM_WIRT,
                  "SimulationBHKW.Moduldaten_Einlesen; WirtschaftlichkeitCtrl.AnlagenTabelle"),
                E("Wirkungsgrad", t("BHKWK_LBL_WIRKUNGSGRAD"), "", SIM_WIRT_BER,
                  "SimulationBHKW.Moduldaten_Einlesen; KostenEmissionRechner.AnschlussleistungKW; AbweichungsErmittler.Felder (Tab_BHKW.Wirkungsgrad)"),
                E("Investition_kwel", t("BHKWK_LBL_INVEST"), "€ / kWel", DLG,
                  "KatalogBrowserProfil (BHKW) - Aufklapper „Alle Daten“ — nur Anzeige " +
                  "(W14a-E-8-B3): abgeleitet aus den fuenf Posten (BHKWKosten.JeKWel) und " +
                  "beim Schreiben nachgerechnet; kein Leser im Rechenweg"),
                E("Raumbedarf", t("BHKWK_LBL_RAUMBEDARF"), "m³", DLG,
                  "KatalogBrowserProfil (BHKW) - Aufklapper „Alle Daten“ des BhkwDialog"),
                E("Wartungskosten_kwhel", t("BHKWK_LBL_WARTUNG"), "€ / kWhel", WIRT,
                  "TechnikPlanwertCtrl.LiesBetriebsplanwert (Betriebskosten-Planwert)"),
                E("Nutzungsdauer", t("BHKWK_LBL_NUTZUNGSDAUER"), t("HZKK_EINHEIT_JAHRE"), DLG,
                  "KatalogBrowserProfil (BHKW) - Aufklapper „Alle Daten“ des BhkwDialog" +
                  " — Geraetedaten, nicht rechenwirksam; massgeblich ist die Nutzungsdauertabelle (A8, E10)"),
                E("NOX", "NOx:", "g / MWh", DLG,
                  "KatalogBrowserProfil (BHKW) - Aufklapper „Alle Daten“ — nur Anzeige (W14a-E-8-B1); der Lauf nimmt den Emissionskatalog des Energietraegers"),
                E("SO2", "SO2:", "g / MWh", DLG,
                  "KatalogBrowserProfil (BHKW) - Aufklapper „Alle Daten“ — nur Anzeige (W14a-E-8-B1); der Lauf nimmt den Emissionskatalog des Energietraegers"),
                E("CO", "CO:", "g / MWh", DLG,
                  "KatalogBrowserProfil (BHKW) - Aufklapper „Alle Daten“ — nur Anzeige (W14a-E-8-B1); der Artenkatalog fuehrt kein CO"),
                E("CO2", "CO2:", "g / MWh", DLG,
                  "KatalogBrowserProfil (BHKW) - Aufklapper „Alle Daten“ — nur Anzeige (W14a-E-8-B1); der Lauf nimmt den Emissionskatalog des Energietraegers"),
                E("Staub", t("HZKK_LBL_STAUB"), "g / MWh", DLG,
                  "KatalogBrowserProfil (BHKW) - Aufklapper „Alle Daten“ — nur Anzeige (W14a-E-8-B1); der Lauf nimmt den Emissionskatalog des Energietraegers"),
                E("Motortyp", t("BHKWK_LBL_MOTORTYP"), "", BER,
                  "AbweichungsErmittler.Felder (Tab_BHKW.Motortyp)"),
                E("Grenzleistung", t("BHKWK_LBL_GRENZLEISTUNG"), "%", SIM,
                  "SimulationBHKW.Moduldaten_Einlesen (Teillastgrenze, Prozent → Faktor)"),
                E("Kosten_Modul", t("BHKWK_LBL_MODUL"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen"),
                E("Kosten_Montage", t("BHKWK_LBL_MONTAGE"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen"),
                E("Kosten_Lieferung", t("BHKWK_LBL_LIEFERUNG"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen"),
                E("Kosten_Schallschutzhaube", t("BHKWK_LBL_SCHALLSCHUTZ"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen"),
                E("Kosten_Abgasreinigung", t("BHKWK_LBL_ABGASREINIGUNG"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen"),
                E("Vorlauf", t("BHKWK_LBL_VORLAUF"), "°C", BER,
                  "AbweichungsErmittler.Felder (Tab_BHKW.Vorlauf) — der Lauf nimmt Tab_Energieanlagen.Vorlauf"),
                E("Ruecklauf", t("BHKWK_LBL_RUECKLAUF"), "°C", BER,
                  "AbweichungsErmittler.Felder (Tab_BHKW.Ruecklauf) — der Lauf nimmt Tab_Energieanlagen.[Rücklauf]"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "BHKWStammCtrl.IstSchreibgeschuetzt (Rueckfrage beim Ueberschreiben)"),
                // SCHEMASCHRITT 99, und deshalb ZULETZT: Die Reihenfolge dieser Liste
                // folgt der SPALTENFOLGE der Tabelle (Waechter
                // ParameterVerwendungTests), und ein ADD COLUMN haengt hinten an. Der
                // Gesamtwirkungsgrad weiter oben ist ihre SUMME und wird beim Speichern
                // nachgezogen (BhkwWirkungsgrad.Gesamt); gerechnet wird mit ihm - diese
                // zwei Spalten sind die PFLEGESTELLE.
                E(BhkwWirkungsgrad.SPALTE_EL, t("BHKWK_LBL_WIRKUNGSGRAD_EL"), "", DLG,
                  "BhkwKatalogDialog (Katalogeditor) - Pflegestelle des Gesamtwirkungsgrads"),
                E(BhkwWirkungsgrad.SPALTE_TH, t("BHKWK_LBL_WIRKUNGSGRAD_TH"), "", DLG,
                  "BhkwKatalogDialog (Katalogeditor) - Pflegestelle des Gesamtwirkungsgrads"),

                // Welle M4 (Schemaschritt ErzeugerTeillastSchema.SCHRITT), in der Spaltenfolge der
                // Tabelle: Teillastkennlinie (BH1) und Takten (BH2) - leer rechnet das Modul wie zuvor.
                E(ErzeugerTeillastSchema.SPALTE_BHKW_ETA_EL50, t("BHKWK_LBL_ETA_EL50"), "", SIM,
                  "SimulationBHKW.Moduldaten_Einlesen und TeillastStundeAbschliessen (BhkwTeillast.EtaEl, " +
                  "Stromkennzahl der Motorlaeufe; leer = wie Volllast); BhkwKatalogDialog (Gruppe Teillast und Takten)"),
                E(ErzeugerTeillastSchema.SPALTE_BHKW_ETA_TH50, t("BHKWK_LBL_ETA_TH50"), "", SIM,
                  "SimulationBHKW.Moduldaten_Einlesen und Motorlaeufe (BhkwTeillast.EtaTh, WaermeAusStrom/" +
                  "StromAusWaerme; leer = wie Volllast); BhkwKatalogDialog (Gruppe Teillast und Takten)"),
                E(ErzeugerTeillastSchema.SPALTE_BHKW_ANFAHRVERLUST, t("BHKWK_LBL_ANFAHRVERLUST"), "kWh", SIM,
                  "SimulationBHKW.TeillastStundeAbschliessen (Starts mal Anfahrverlust, Takten unter der " +
                  "Untergrenze); BhkwKatalogDialog (Gruppe Teillast und Takten)"),
                E(ErzeugerTeillastSchema.SPALTE_BHKW_MINDESTLAUFZEIT, t("BHKWK_LBL_MINDESTLAUFZEIT"), "min", SIM,
                  "SimulationBHKW.TeillastStundeAbschliessen (BhkwTeillast.StartsImTakt, Kesselregel; mit " +
                  "Anfahrverlust Schalter des Taktens); BhkwKatalogDialog (Gruppe Teillast und Takten)")
            }.Concat(Katalogspalten(t)).ToList();
        }

        // =================================================================
        // 3. Waermepumpe — Tab_WP_STAMM (25 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Die Kennfelder stehen nicht hier.</b> COP und Leistung je Vorlauf und
        /// Quelltemperatur fuehrt <c>Tab_Kenndaten_STAMM</c> (Kopie
        /// <c>Tab_Kenndaten</c>); der Lauf liest sie in
        /// <c>SimulationWaermepumpe.KennlinienwahlLaden</c>, gepflegt werden sie im
        /// <c>KennlinienEditorDialog</c> (W7.2). Sie sind KEINE Spalten dieser Tabelle
        /// und stehen deshalb nicht in diesem Katalog.
        ///
        /// <para><b>Der Befund dieser Tabelle:</b> <c>Modulkosten</c> ist der einzige
        /// Kostenwert der Waermepumpe, den die Kostenplanung liest
        /// (<c>TechnikPlanwertCtrl.BasenFuellen</c>) — und der einzige gerechnete Parameter
        /// aller sieben Kataloge, den seine Verwaltung nicht zur PFLEGE fuehrt.
        /// <b>Seit dem Anwenderentscheid W14a-O-1 vom 06.09.2026 zeigt sie ihn
        /// wenigstens</b>: als Lesewert mit Herleitungszeile im Stammdialog. Ä19
        /// bleibt damit gewahrt — hier tippt niemand Kosten ein, gepflegt werden
        /// Geraetekosten in der Kostenverwaltung. <b>Befund W14a-O-2 (06.09.2026):</b>
        /// Der VDI-3805-Import fuellt die Spalte NICHT — <c>KatalogImportSatz.NachStamm</c>
        /// setzt sie nie, und <c>WPStammCtrl.UpdateImport</c> laesst sie beim
        /// Ueberschreiben ausdruecklich stehen („vom Anwender gepflegte Felder").
        /// Ein neu importiertes Geraet steht damit dauerhaft auf 0. Fuenf weitere
        /// Spalten (<c>Laenge</c> … <c>Raum</c>) hat
        /// ueberhaupt kein Leser: sie kommen aus dem VDI-3805-Import und bleiben
        /// liegen.</para>
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Waermepumpe(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM,
                  "Tab_Energieanlagen.ID_WP → SimulationWaermepumpe.ModuleAufbauen; Hydraulikbild.Lesen"),
                E("Bezeichner", t("WPS_LBL_NAME"), "", SIM_BER,
                  "SimulationWaermepumpe.ModuleAufbauen (ID_WP der Anlage); AbweichungsErmittler.Felder (Tab_WP.Bezeichner)"),
                E("Firma", t("WPS_LBL_HERSTELLER"), "", BER,
                  "AbweichungsErmittler.Felder (Tab_WP.Firma)"),
                E("Beschreibung", t("WPS_LBL_BESCHREIBUNG"), "", DLG,
                  "WaermepumpeStammDialog.razor (Feld Beschreibung)"),
                E("Typ", t("WPS_LBL_TYP"), "", SIM_BER,
                  "SimulationWaermepumpe.ModuleAufbauen (Bauart → Quellenwahl); Warnkriterien.SoleOhneQuellePruefen; AbweichungsErmittler.Felder (Tab_WP.Typ)"),
                E("Baujahr", t("WPS_LBL_BAUJAHR"), "", DLG,
                  "WaermepumpeStammDialog.razor (Feld Baujahr)"),
                E("Aufstellung", t("WPS_LBL_AUFSTELLUNG"), "", DLG,
                  "WaermepumpenKatalogFilter.Anwenden (Katalogfilter W7.1)"),
                E("Nennleistung", t("WPS_LBL_NENNLEISTUNG"), "kW", SIM_BER,
                  "SimulationWaermepumpe.ModuleAufbauen (Grenzleistung des Moduls); AbweichungsErmittler.Felder (Tab_WP.Nennleistung)"),
                // Kein Leser mehr: Der Bericht fuehrt die Spalte nicht (Anwenderentscheid
                // 29.09.2026); im Stammdialog nicht sichtbar, laeuft verborgen mit.
                E("maxPtherm", t("PARV_LBL_MAXPTHERM"), "kW", NIX),
                E("Heizung", t("WPS_LBL_HEIZSTAB"), "kW", SIM,
                  "SimulationWaermepumpe.ModuleAufbauen (WP_Heizung, Heizstabphase)"),
                E("Regelung", t("WPS_LBL_REGELUNG"), "", BER,
                  "AbweichungsErmittler.Felder (Tab_WP.Regelung); WaermepumpenKatalogFilter.Anwenden"),
                E("Modulkosten", t("MODK_LBL_MODULKOSTEN"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen (ERZEUGER_WAERMEPUMPE) — " +
                  "im Stammdialog nur lesend (W14a-O-1); geschrieben wird die Spalte " +
                  "heute von KEINEM Importweg (Befund W14a-O-2)"),
                E("Laenge", t("MODK_LBL_LAENGE"), "mm", NIX),
                E("Breite", t("MODK_LBL_BREITE"), "mm", NIX),
                E("Hoehe", t("PARV_LBL_HOEHE"), "mm", NIX),
                E("Gewicht", t("PARV_LBL_GEWICHT"), "kg", NIX),
                E("Raum", t("HZKK_LBL_RAUMBEDARF"), "m³", NIX),
                E("Kuehlleistung", t("WPS_LBL_KUEHLLEISTUNG"), "kW", BER,
                  "AbweichungsErmittler.Felder (Tab_WP.Kuehlleistung)"),
                E("Bauart", t("WPK_LBL_BAUART"), "", BER,
                  "AbweichungsErmittler.Felder (Tab_WP.Bauart); WaermepumpenKatalogFilter.Anwenden"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "WPStammCtrl.Speichern (Auslieferungssatz, Liste gedimmt)"),
                // KU-S3 (Schemaschritt 114; Kuehlkonzept 7.3, E33): der Kuehlbetrieb am
                // Erzeuger. Noch KEIN Leser - die reversible Waermepumpe rechnet ab KU2 Welle 2;
                // bis dahin reisen die Werte nur mit (Katalogkopie ins Projekt, Uebernahme in
                // den Katalog).
                E(KuehlungSchema.SPALTE_ERZEUGER_KUEHLBETRIEB, t("WPS_LBL_KUEHLBETRIEB"), "", NIX),
                E(KuehlungSchema.SPALTE_KUEHL_VORLAUF, t("WPS_LBL_KUEHL_VORLAUF"), "°C", NIX),
                E(KuehlungSchema.SPALTE_KUEHL_HILFSSTROMANTEIL, t("WPS_LBL_KUEHL_HILFSSTROM"), "", NIX),

                // Welle M4 (Schemaschritt ErzeugerTeillastSchema.SCHRITT): der Taktverlust nach EN 14825
                // (WP1) - leer = keine Taktrechnung bzw. C_d = 0,9.
                E(ErzeugerTeillastSchema.SPALTE_WP_MINDESTLEISTUNG, t("WPS_LBL_MINDESTLEISTUNG"), "kW", SIM,
                  "SimulationWaermepumpe.TaktwerteLesen und TaktStundeAbschliessen (Waermepumpentakt.Taktet, " +
                  "Mehrstrom, StartsImTakt); Kaeltekaskade (Mindestanteil im Kuehlbetrieb); WaermepumpeStammFelder"),
                E(ErzeugerTeillastSchema.SPALTE_WP_CD, t("WPS_LBL_TAKTVERLUST_CD"), "", SIM,
                  "SimulationWaermepumpe.TaktStundeAbschliessen (Waermepumpentakt.Teillastfaktor, leer = 0,9); " +
                  "Kaeltekaskade; WaermepumpeStammFelder")
            }.Concat(Katalogspalten(t)).ToList();
        }

        // =================================================================
        // 4. Solarkollektoren — Tab_Solarkollektoren_STAMM (15 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Der Befund dieser Tabelle:</b> Der Kollektorwirkungsgrad rechnet mit <c>h0</c>,
        /// <c>k1</c>, <c>k2</c>, <c>Kdir</c> und — für die Diffus- und Bodenreflexstrahlung —
        /// <c>Kdfu</c> (im Editor „Kdiff", Welle M2 ST5; 0 = Faktor der Direktstrahlung). Und <c>Modulflaeche</c> ist die
        /// Bruttoflaeche EINES Moduls; gerechnet wird mit der Flaeche, auf die die Kennwerte
        /// bezogen sind (<c>Bezugsflaeche</c>: Apertur als Vorgabe oder Brutto, Welle M2 ST6), mal
        /// <c>Tab_Energieanlagen.Kollektormodulanzahl</c>.
        ///
        /// <para><b>Die Investitionskosten pflegt seit dem 15.09.2026 der AUFKLAPPER</b>
        /// „Alle Daten" im Modulbereich des <c>SolarkollektorenDialog</c>: Aus dem
        /// Katalogeditor ist das Feld gefallen (Pflichtfelder 8 → 7), der Aufklapper
        /// zeigt dafuer ALLE editierbaren Spalten des
        /// <see cref="KatalogBrowserProfil"/>. Die Fundstelle bleibt die des RECHNERS —
        /// sie belegt die Einstufung, nicht die Eingabe.</para>
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Solarkollektoren(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM_WIRT,
                  "Tab_Energieanlagen.ID_Solar → SimulationSolarthermie.Kollektorfelder_Lesen; TechnikPlanwertCtrl.Plaene"),
                E("Bezeichner", t("SKK_LBL_NAME"), "", SIM_BER,
                  "SimulationSolarthermie.Kollektorfelder_Lesen; AbweichungsErmittler.Felder (Tab_Solarkollektoren.Bezeichner)"),
                E("Firma", t("SKK_LBL_HERSTELLER"), "", DLG,
                  "SolarkollektorKatalogDialog.razor (Feld Hersteller)"),
                E("Beschreibung", t("SKK_LBL_BESCHREIBUNG"), "", DLG,
                  "SolarkollektorKatalogDialog.razor (Feld Beschreibung)"),
                E("Kollektortyp", t("SKK_LBL_TYP"), "", BER,
                  "AbweichungsErmittler.Felder (Tab_Solarkollektoren.Kollektortyp)"),
                E("Modulflaeche", t("SKK_LBL_MODULFLAECHE"), "m²", SIM,
                  "Solarkreis.Modulbezugsflaeche (Bruttoflaeche, rechnet bei Bezugsflaeche = brutto); " +
                  "SolarkollektorKatalogDialog.razor (Feld Kollektorflaeche)"),
                E("Aperturflaeche", t("SKK_LBL_APERTURFLAECHE"), "m²", SIM_BER,
                  "Solarkreis.Modulbezugsflaeche (rechnet bei Bezugsflaeche = apertur, der Vorgabe); " +
                  "AbweichungsErmittler.Felder (Tab_Solarkollektoren.Aperturflaeche)"),
                E("h0", "h0:", "", SIM, "SimulationSolarthermie.Kollektorfelder_Lesen (Konversionsfaktor)"),
                E("k1", "k1:", "W/(m²*K)", SIM, "SimulationSolarthermie.Kollektorfelder_Lesen"),
                E("k2", "k2:", "W/(m²*K²)", SIM, "SimulationSolarthermie.Kollektorfelder_Lesen"),
                E("Kdir", "Kdir:", "", SIM, "SimulationSolarthermie.Kollektorfelder_Lesen (IAM, direkt)"),
                E("Kdfu", "Kdiff:", "", SIM,
                  "Solarkreis.LeistungJeQm (K_d der Diffus- und Bodenreflexstrahlung, ST5; 0 = K_b(θ)); " +
                  "SolarkollektorKatalogDialog.razor (Feld Kdiff)"),
                E("Investitionskosten", t("SKK_LBL_KOSTEN"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen (Stueckpreis, ERZEUGER_SOLARTHERMIE)"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "SolarkollektorenStammCtrl (Auslieferungssatz)"),

                // Welle M2 ST6 (Schemaschritt SolarthermieFelderSchema.SCHRITT) - in der Reihenfolge
                // der Tabelle die letzte Spalte.
                E(SolarthermieFelderSchema.SPALTE_BEZUGSFLAECHE, t("SKK_LBL_BEZUGSFLAECHE"), "", SIM,
                  "SimulationSolarthermie.Kollektorfelder_Lesen → Solarkreis.Modulbezugsflaeche " +
                  "(apertur oder brutto); SolarkollektorKatalogDialog.razor; KatalogBrowserProfil (Solarkollektoren)")
            }.Concat(Katalogspalten(t)).ToList();
        }

        // =================================================================
        // 5. Photovoltaik — Tab_PV_STAMM (19 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Der Befund dieser Tabelle:</b> Sechs elektrische Kenngroessen
        /// (<c>U_Mpp</c>, <c>U_Leerlauf</c>, <c>I_Mpp</c>, <c>I_Kurzschluss</c>,
        /// <c>alpha_SC</c>, <c>beta_OC</c>) stehen im Katalog und im Aufklapper des
        /// Projektdialogs (W6-E-1), gehen aber in keine Rechnung ein: Das erweiterte
        /// Modell nach Huld braucht <c>Leistung</c>, <c>gamma_PMP</c>, <c>T_NOCT</c>
        /// und <c>Technologie</c>. <c>alpha_SC</c> und <c>beta_OC</c> sind ausserdem
        /// die einzigen zwei Spalten, die der Katalogdialog nicht pflegen kann — sie
        /// kommen nur aus dem CEC-/PAN-Import.
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Photovoltaik(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM_WIRT,
                  "Tab_Energieanlagen.ID_PV → SimulationPV.Berechnung; TechnikPlanwertCtrl.Plaene"),
                E("Bezeichner", t("MODK_LBL_BEZEICHNER_PV"), "", SIM_BER,
                  "SimulationPV.Berechnung (Modulname der Meldungen); AbweichungsErmittler.Felder (Tab_PV.Bezeichner)"),
                E("Firma", t("MODK_LBL_FIRMA"), "", BER,
                  "AbweichungsErmittler.Felder (Tab_PV.Firma)"),
                E("Beschreibung", t("MODK_LBL_BESCHREIBUNG"), "", DLG,
                  "ModulKatalogDialog.razor (Feld Beschreibung)"),
                E("Leistung", t("MODK_LBL_PMAX"), "W", SIM_BER,
                  "SimulationPV.Berechnung (P_STC der Anlage); AbweichungsErmittler.Felder (Tab_PV.Leistung)"),
                E("Wirkungsgrad", t("MODK_LBL_WIRKUNGSGRAD"), "%", SIM_BER,
                  "SimulationPV.Berechnung; AbweichungsErmittler.Felder (Tab_PV.Wirkungsgrad)"),
                E("U_Mpp", t("MODK_LBL_UMPP"), "V", DLG,
                  "PhotovoltaikStammCtrl.Parameterzeilen (W6-E-1); ModulKatalogDialog"),
                E("U_Leerlauf", t("MODK_LBL_ULEERLAUF"), "V", DLG,
                  "PhotovoltaikStammCtrl.Parameterzeilen (W6-E-1); ModulKatalogDialog"),
                E("I_Mpp", t("MODK_LBL_IMPP"), "A", DLG,
                  "PhotovoltaikStammCtrl.Parameterzeilen (W6-E-1); ModulKatalogDialog"),
                E("I_Kurzschluss", t("MODK_LBL_IKURZSCHLUSS"), "A", DLG,
                  "PhotovoltaikStammCtrl.Parameterzeilen (W6-E-1); ModulKatalogDialog"),
                E("alpha_SC", t("PVIMP_LBL_ALPHA_ISC"), "", DLG,
                  "PhotovoltaikStammCtrl.Parameterzeilen (W6-E-1) — nur Anzeige, Quelle CEC/PAN-Import"),
                E("beta_OC", t("PVIMP_LBL_BETA_VOC"), "", DLG,
                  "PhotovoltaikStammCtrl.Parameterzeilen (W6-E-1) — nur Anzeige, Quelle CEC/PAN-Import"),
                E("gamma_PMP", t("MODK_LBL_TEMPKOEFF"), "%/K", SIM,
                  "SimulationPV.Berechnung (Temperaturkoeffizient des Huld-Modells)"),
                E("T_NOCT", t("PV_MODUL_LABEL_TNOCT"), "°C", SIM,
                  "SimulationPV.NoctDesModuls (Zelltemperatur)"),
                E("Laenge", t("MODK_LBL_LAENGE"), "m", SIM,
                  "SimulationPV.Berechnung (Modulflaeche)"),
                E("Breite", t("MODK_LBL_BREITE"), "m", SIM,
                  "SimulationPV.Berechnung (Modulflaeche)"),
                E("Modulkosten", t("MODK_LBL_MODULKOSTEN_PV"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen (Stueckpreis, ERZEUGER_PHOTOVOLTAIK)"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "PhotovoltaikStammCtrl.SpeichernAus (Auslieferungssatz)"),
                E("Technologie", t("PVM_MODUL_LABEL_TECHNOLOGIE"), "", SIM_BER,
                  "SimulationPV.HuldSatzDerAnlage (Huld-Satz je Zelltechnologie); AbweichungsErmittler.Felder (Tab_PV.Technologie)")
            }.Concat(Katalogspalten(t)).ToList();
        }

        // =================================================================
        // 6. Stromspeicher — Tab_Stromspeicher_STAMM (16 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Der Befund dieser Tabelle:</b> Sie ist die einzige der sieben, in der
        /// JEDE Fachspalte gerechnet wird — <c>StromspeicherSimCtrl.LeseParameter</c>
        /// liest alle elf und reicht sie als <c>SpeicherParameter</c> an die
        /// <c>SpeicherEngine</c>. Vier davon sind Kostengroessen und gehen ausserdem in
        /// den Kostenplanwert (<c>TechnikPlanwertCtrl.BasenFuellen</c>).
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Stromspeicher(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM_WIRT,
                  "Tab_Energieanlagen.ID_SP → StromspeicherSimCtrl.Speicheranlagen; TechnikPlanwertCtrl.Plaene"),
                E("Bezeichner", t("MODK_LBL_BEZEICHNER"), "", SIM_BER,
                  "StromspeicherSimCtrl.LeseParameter; AbweichungsErmittler.Felder (Tab_Stromspeicher.Bezeichner)"),
                E("Typ", t("MODK_LBL_TYP"), "", BER,
                  "AbweichungsErmittler.Felder (Tab_Stromspeicher.Typ)"),
                E("Leistung", t("MODK_LBL_LEISTUNG"), "kW", SIM_WIRT_BER,
                  "StromspeicherSimCtrl.LeseParameter; TechnikPlanwertCtrl.BasenFuellen; AbweichungsErmittler.Felder (Tab_Stromspeicher.Leistung)"),
                E("Energie", t("SP_LABEL_ENERGIE_KURZ"), "kWh", SIM_WIRT_BER,
                  "StromspeicherSimCtrl.LeseParameter; TechnikPlanwertCtrl.BasenFuellen; AbweichungsErmittler.Felder (Tab_Stromspeicher.Energie)"),
                E("Degradation", t("MODK_LBL_DEGRADATION"), "%", SIM,
                  "StromspeicherSimCtrl.LeseParameter (SpeicherParameter.DegradationProA)"),
                E("Ladezustand", t("MODK_LBL_LADEZUSTAND"), "%", SIM,
                  "StromspeicherSimCtrl.LeseParameter (nutzbarer Hub)"),
                E("Modulkosten", t("MODK_LBL_MODULKOSTEN"), "€/kWh", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen (spezifischer Kapazitaetspreis)"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "StromspeicherStammCtrl.SpeichernAus (Auslieferungssatz)"),
                E("Wirkungsgrad_RT", t("SP_LABEL_WIRKUNGSGRAD_RT"), "-", SIM,
                  "StromspeicherSimCtrl.LeseParameter (Umlaufwirkungsgrad der SpeicherEngine)"),
                E("Zyklen_Zugesichert", t("SP_LABEL_ZYKLEN"), "-", SIM_WIRT,
                  "StromspeicherSimCtrl.LeseParameter; ArbitragePlaner.Lauf (Zyklenbudget)"),
                E("Verschleisskosten", t("SP_LABEL_VERSCHLEISSKOSTEN"), t("SP_EINHEIT_ZYKLUSKOSTEN"), WIRT,
                  "StromspeicherSimCtrl.LeseParameter; ArbitrageOptionen.VerschleissCtKwh"),
                E("Leistungskosten", t("SP_LABEL_LEISTUNGSKOSTEN"), "€/kW", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen"),
                E("Investition_Fix", t("SP_LABEL_INVESTITION_FIX"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen; StromspeicherSimCtrl.LeseParameter"),
                E("Standby_Verbrauch", t("SP_LABEL_STANDBY"), "W", SIM,
                  "StromspeicherSimCtrl.LeseParameter (SpeicherParameter.StandbyKw); " +
                  "SpeicherEngine/Speichersystem.Standby (aus PV-Überschuss, sonst Netz; Welle M5, SP1)"),

                // Migrationsschritt 68 (Anwenderentscheid W14a-E-10-Q7 vom 07.09.2026).
                // Die Spalte steht am ENDE der Tabelle, weil ALTER TABLE ADD COLUMN sie
                // dort anhaengt - die Reihenfolge dieses Katalogs ist die der Tabelle.
                // Verwendung DLG wie beim Wechselrichter: Kein Rechenweg liest den
                // Hersteller, er sortiert und filtert die Katalogliste.
                E("Firma", t("MODK_LBL_FIRMA"), "", DLG,
                  "StromspeicherStammCtrl.Hersteller (Spalte \"Hersteller\" der Katalogliste); " +
                  "StromspeicherCtrl.CopyFromStamm (Quelle der Projektkopie)"),

                // Welle M5 (SP1; Schemaschritt StromViertelstundenSchema.SCHRITT) - am Ende der Tabelle.
                E(StromViertelstundenSchema.SPALTE_SELBSTENTLADUNG, t("SP_LABEL_SELBSTENTLADUNG"), "%/Monat", SIM,
                  "StromspeicherSimCtrl.LeseParameter (SpeicherParameter.SelbstentladungProzentMonat); " +
                  "SpeicherEngine/Speichersystem.Selbstentladung; SpeicherFlottenStudieCtrl.EinheitAusKatalogsatz")
            }.Concat(Katalogspalten(t)).ToList();
        }

        // =================================================================
        // 7. Pufferspeicher — Tab_Pufferspeicher_STAMM (8 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Die Betriebswerte stehen nicht hier.</b> Vor-/Ruecklauf, Schwellen,
        /// Schichten, Entnahmehoehen und Ladeleistungen fuehrt erst die PROJEKTKOPIE
        /// <c>Tab_Pufferspeicher</c> (28 Spalten, angelegt von <c>ProjektPuffer</c>);
        /// gepflegt werden sie im <c>PufferSpProjektDialog</c> (W10a.4). Der KATALOG
        /// traegt nur die sechs Geraetewerte — deshalb ist dies die kuerzeste der
        /// sieben Tabellen.
        ///
        /// <para><b>Die Investitionskosten pflegt seit dem 15.09.2026 der AUFKLAPPER</b>
        /// „Alle Daten" im Modulbereich des <c>PufferspeicherDialog</c>: Aus dem
        /// Katalogeditor ist die Kostengruppe gefallen, der Aufklapper zeigt dafuer ALLE
        /// editierbaren Spalten des <see cref="KatalogBrowserProfil"/>. Die Fundstelle
        /// bleibt die des RECHNERS — sie belegt die Einstufung, nicht die Eingabe.</para>
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Pufferspeicher(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM_WIRT,
                  "Tab_Energieanlagen.ID_PUFFER → WaermesenkeClass.PufferLesen; TechnikPlanwertCtrl.Plaene"),
                E("Bezeichner", t("PSPK_LBL_NAME"), "", SIM_BER,
                  "WaermesenkeClass.ProjektPufferListe; AbweichungsErmittler.Felder (Tab_Pufferspeicher.Bezeichner)"),
                E("Hersteller", t("PSPK_LBL_HERSTELLER"), "", DLG,
                  "PufferSpKatalogDialog.razor (Feld Hersteller); Herstellerfilter des Browsers"),
                E("Speichertyp", t("PSPK_LBL_SPEICHERTYP"), "", SIM_BER,
                  "Warnkriterien.BauformAnzeige, Warnkriterien.SpeicherPruefen (Kriterium W4, Kombi- und Solarspeicher); AbweichungsErmittler.Felder (Tab_Pufferspeicher.Speichertyp)"),
                E("Bereitschaftsverluste", t("PSPK_LBL_VERLUSTE"), "kWh/d", SIM,
                  "SimulationControl.SpeicherRegistryAufbauen (SimulationPufferspeicher.Init); WaermequelleClass.Quellspeicher"),
                E("Gesamtvolumen", t("PSPK_LBL_VOLUMEN"), "l", SIM_BER,
                  "SimulationControl.SpeicherRegistryAufbauen; WaermequelleClass.Quellspeicher; AbweichungsErmittler.Felder (Tab_Pufferspeicher.Gesamtvolumen)"),
                E("Investitionskosten", t("PSPK_LBL_INVEST"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen (KOSTEN_KOMPONENTE_PUFFERSPEICHER)"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "PufferSpStammCtrl.Ueberschreiben (Auslieferungssatz)")
            }.Concat(Katalogspalten(t)).ToList();
        }

        // =================================================================
        // 8. Wechselrichter — Tab_Wechselrichter_STAMM (34 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Seit Stufe S3 (06.09.2026) RECHNET dieser Katalog.</b> Bis dahin galt die
        /// Zusage des Anwenderentscheids W6-E-2 „Katalog, Verwaltung und Import ohne
        /// Rechenwirkung"; S1 und S2 haben sie gehalten, S3 loest sie ein. Zehn Spalten
        /// stehen deshalb jetzt auf <c>Simulation</c> und eine auf
        /// <c>Wirtschaftlichkeit</c>.
        ///
        /// <para><b>Was wohin gewandert ist.</b> Die SECHS Stuetzstellen und
        /// <c>P_AC_Nenn</c>, <c>P_Standby</c> und <c>P_Nacht</c> rechnen in
        /// <c>PvStrangModell</c> (Kennlinie, Clipping, Nachtverbrauch, Konzept 4.1
        /// Schritt 4); <c>Kosten</c> rechnet in <c>TechnikPlanwertCtrl</c> (Q8).
        /// <c>Anzahl_Mppt</c> und <c>Straenge_Je_Mppt</c> bleiben <c>Dialog</c>:
        /// Entscheidungsfrage Q7 laesst den MPP-Tracker ausdruecklich als reine
        /// PRUEFgroesse - er hat keine eigene Leistungsgrenze im Rechenweg.</para>
        ///
        /// <para><b>Die Spannungs- und Stromgrenzen bleiben ebenfalls <c>Dialog</c>.</b>
        /// <c>U_Mpp_Min</c>, <c>U_Mpp_Max</c>, <c>U_Dc_Max</c>, <c>U_Start</c> und
        /// <c>I_Dc_Max</c> lesen die Auslegungspruefungen P1 bis P5 beim BEARBEITEN einer
        /// Strangzeile und faerben eine Ampel; ohne Ein-Dioden-Modell (Stufe E3 des
        /// PV-Ertragsmodells, zurueckgestellt) gibt es keine Strangspannung je Stunde,
        /// die sie im Lauf begrenzen koennte.</para>
        ///
        /// Der Fall <c>Der_Wechselrichter_rechnet_ab_S3</c> haelt den neuen Zustand fest -
        /// er ist der Zeuge dafuer, dass die Einstufungen dem Rechenweg gefolgt sind.
        ///
        /// <para><b>Die sieben Sandia-Spalten stehen weiter als <c>Keine</c> da</b> - sie
        /// sind mitgeschriebenes Katalogwissen (Konzept 3.3.3): Der CEC-Import schreibt
        /// sie verlustfrei mit, damit ein spannungsabhaengiges Modell (Stufe E3 des
        /// PV-Ertragsmodells) sie spaeter ohne Neuimport vorfindet. Heute liest sie
        /// nichts - auch die Verwaltung zeigt sie nicht, weil sie kein Anwender von Hand
        /// pflegen kann.</para>
        /// </remarks>
        /// <summary>
        /// <b>Die Kältemaschine</b> (KU3-2): Der Rechenweg liest die Projektkopie
        /// (<c>Tab_Kaeltemaschine</c>) über <c>Kaeltemaschine.AusModell</c> — Nennkälteleistung,
        /// Rückkühlart, Mindestteillast, Hilfsstrom der Rückkühlung und den kleinsten Kaltwasservorlauf;
        /// Leistung und EER der Stunde kommen aus der Kennlinie. Der Nenn-EER ist eine Angabe des
        /// Datenblatts, das Kältemittel eine Beschreibung (F-Gase rechnet EPOS-Plan nicht, Kühlkonzept
        /// 6.3); die Modulkosten liest die Wirtschaftlichkeit (KU3-4, Gerätepreis × Anzahl der Anlagenzeile).
        /// </summary>
        private static IReadOnlyList<ParameterEintrag> Kaeltemaschine(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", DLG,
                  "KaeltemaschineCtrl.AusKatalogUebernehmen (Quelle der Projektkopie, ID_Stamm)"),
                E("Bezeichner", t("KM_LBL_BEZEICHNER"), "", SIM,
                  "Kaeltemaschine.AusModell (Name des Erzeugers in Kältekaskade und Laufprotokoll)"),
                E("Firma", t("KM_LBL_FIRMA"), "", DLG,
                  "KaeltemaschineKatalogDialog.razor (Feld Firma)"),
                E("Typ", t("KM_LBL_TYP"), "", DLG,
                  "KaeltemaschineKatalogDialog.razor (Feld Typ)"),
                E("Beschreibung", t("KM_LBL_BESCHREIBUNG"), "", DLG,
                  "KaeltemaschineKatalogDialog.razor (Feld Beschreibung)"),
                E(KaeltemaschineSchema.SPALTE_NENNKAELTELEISTUNG, t("KM_LBL_NENNKAELTELEISTUNG"), "kW", SIM,
                  "Kaeltemaschine.Stunde (Bezug der Mindestteillast, Grenze der freien Kuehlung)"),
                E(KaeltemaschineSchema.SPALTE_NENN_EER, t("KM_LBL_NENN_EER"), "-", DLG,
                  "KaeltemaschineKatalogDialog.razor (Datenblattangabe; gerechnet wird der EER der Kennlinie)"),
                E(KaeltemaschineSchema.SPALTE_KAELTEMITTEL, t("KM_LBL_KAELTEMITTEL"), "", DLG,
                  "KaeltemaschineKatalogDialog.razor (Beschreibung; F-Gase ausgeschlossen, Kuehlkonzept 6.3)"),
                E(KaeltemaschineSchema.SPALTE_RUECKKUEHLART, t("KM_LBL_RUECKKUEHLART"), "", SIM,
                  "Kaeltemaschine.Rueckkuehltemperatur; Kaeltemaschine.FreieKuehlungMoeglich"),
                E(KaeltemaschineSchema.SPALTE_MINDESTTEILLAST, t("KM_LBL_MINDESTTEILLAST"), "%", SIM,
                  "Kaeltemaschine.Stunde (Takt unter der Mindestteillast)"),
                E(KaeltemaschineSchema.SPALTE_HILFSSTROM_RUECKKUEHLUNG, t("KM_LBL_HILFSSTROM"), "kW", SIM,
                  "Kaeltemaschine.Stunde (Hilfsstrom der Rueckkuehlung x Laufanteil)"),
                E(KaeltemaschineSchema.SPALTE_KALTWASSER_VORLAUF_MIN, t("KM_LBL_KALTWASSER_MIN"), "°C", SIM,
                  "Kaeltemaschine.AusModell (untere Grenze der Kaltwassertemperatur)"),
                E(KaeltemaschineSchema.SPALTE_MODULKOSTEN, t("KM_LBL_MODULKOSTEN"), "€", WIRT,
                  "TechnikPlanwertCtrl.BasenFuellen (ERZEUGER_KAELTEMASCHINE: Geraetepreis x Anzahl der Anlagenzeile)"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "KaeltemaschineStammCtrl.Speichern (Auslieferungssatz)"),
            }.Concat(Katalogspalten(t)).ToList();
        }

        private static IReadOnlyList<ParameterEintrag> Wechselrichter(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", DLG,
                  "WechselrichterCtrl.CopyFromStamm (Quelle der Projektkopie)"),
                E("Bezeichner", t("WRK_LBL_BEZEICHNER"), "", DLG,
                  "ModulKatalogDialog (Liste und WHERE-Schluessel); WechselrichterCtrl.CopyFromStamm"),
                E("Firma", t("WRK_LBL_FIRMA"), "", DLG,
                  "WechselrichterStammCtrl.Hersteller (Herstellerfilter der Verwaltung)"),
                E("Beschreibung", t("WRK_LBL_BESCHREIBUNG"), "", DLG,
                  "ModulKatalogDialog (Feld Beschreibung)"),
                E("P_AC_Nenn", t("WRK_LBL_P_AC_NENN"), "kW", SIM,
                  "PvStrangModell.Stunde (Auslastung und CLIPPING je Geraet, Konzept 4.1 Schritt 4); " +
                  "SimulationPV.DcAcDerAnlage; ModulKatalogProfil (Pflichtfeld); " +
                  "WechselrichterPlausibilitaet.PruefeLeistungen; StrangPlausibilitaet.DcAcPruefen (P6)"),
                E("S_AC_Max", t("WRK_LBL_S_AC_MAX"), "kVA", DLG,
                  "ModulKatalogProfil (Gruppe Geraet); WechselrichterPlausibilitaet"),
                E("P_DC_Max", t("WRK_LBL_P_DC_MAX"), "kW", DLG,
                  "ModulKatalogProfil (Gruppe Geraet); WechselrichterPlausibilitaet; " +
                  "StrangPlausibilitaet.DcAcPruefen (P7)"),
                E("U_Mpp_Min", t("WRK_LBL_U_MPP_MIN"), "V", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeSpannungen; " +
                  "StrangPlausibilitaet.StrangPruefen (P2)"),
                E("U_Mpp_Max", t("WRK_LBL_U_MPP_MAX"), "V", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeSpannungen; " +
                  "StrangPlausibilitaet.StrangPruefen (P3)"),
                E("U_Dc_Max", t("WRK_LBL_U_DC_MAX"), "V", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeSpannungen; " +
                  "StrangPlausibilitaet.StrangPruefen (P1)"),
                E("U_Start", t("WRK_LBL_U_START"), "V", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeSpannungen"),
                E("I_Dc_Max", t("WRK_LBL_I_DC_MAX"), "A", DLG,
                  "ModulKatalogProfil (Gruppe Eingang) - JE MPPT, siehe WechselrichterSchema; " +
                  "StrangPlausibilitaet.MpptPruefen (P4)"),
                E("Anzahl_Mppt", t("WRK_LBL_ANZAHL_MPPT"), "", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeMppt; " +
                  "StrangPlausibilitaet.MpptPruefen (NULL = ein Tracker, W6-O-2); " +
                  "WechselrichterVorschlag (NULL = bedingt, Katalogwerte unvollstaendig); OND-Import NbMPPT"),
                E("Straenge_Je_Mppt", t("WRK_LBL_STRAENGE_JE_MPPT"), "", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeMppt; " +
                  "StrangPlausibilitaet.MpptPruefen (P5); StrangAuslegung.ParallelJeMppt (Deckel); " +
                  "OND-Import NbInputs/NbMPPT bei glatter Teilung"),
                E("Eta05", t("WRK_LBL_ETA05"), "-", SIM,
                  "PvStrangModell.Kennlinie (Stuetzstelle 5 %, SimulationPV.StraengeRechnen); " +
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta10", t("WRK_LBL_ETA10"), "-", SIM,
                  "PvStrangModell.Kennlinie (Stuetzstelle 10 %, SimulationPV.StraengeRechnen); " +
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta20", t("WRK_LBL_ETA20"), "-", SIM,
                  "PvStrangModell.Kennlinie (Stuetzstelle 20 %, SimulationPV.StraengeRechnen); " +
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta30", t("WRK_LBL_ETA30"), "-", SIM,
                  "PvStrangModell.Kennlinie (Stuetzstelle 30 %, SimulationPV.StraengeRechnen); " +
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta50", t("WRK_LBL_ETA50"), "-", SIM,
                  "PvStrangModell.Kennlinie (Stuetzstelle 50 %, SimulationPV.StraengeRechnen); " +
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta100", t("WRK_LBL_ETA100"), "-", SIM,
                  "PvStrangModell.Kennlinie (Stuetzstelle 100 %, SimulationPV.StraengeRechnen); " +
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta_Euro", t("WRK_LBL_ETA_EURO"), "-", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad) - Ausweis des Datenblatts"),
                E("Eta_Max", t("WRK_LBL_ETA_MAX"), "-", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad) - Ausweis des Datenblatts"),
                E("P_Standby", t("WRK_LBL_P_STANDBY"), "W", SIM,
                  "PvStrangModell.Stunde (Einschaltschwelle, Konzept 4.1 Schritt 4); " +
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); CEC-Import (Pso)"),
                E("P_Nacht", t("WRK_LBL_P_NACHT"), "W", SIM,
                  "PvStrangModell.Stunde (NACHTVERBRAUCH, negative Erzeugung); " +
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); CEC-Import (Pnt)"),
                E("Kosten", t("WRK_LBL_KOSTEN"), "€", WIRT,
                  "TechnikPlanwertCtrl.Wechselrichteranlagen (Investition je Geraet x Geraetezahl, Q8); " +
                  "ModulKatalogProfil (Gruppe Geraet)"),
                E("Sandia_Pdco", "Sandia Pdco:", "W", NIX),
                E("Sandia_Vdco", "Sandia Vdco:", "V", NIX),
                E("Sandia_Pso", "Sandia Pso:", "W", NIX),
                E("Sandia_C0", "Sandia C0:", "1/W", NIX),
                E("Sandia_C1", "Sandia C1:", "1/V", NIX),
                E("Sandia_C2", "Sandia C2:", "1/V", NIX),
                E("Sandia_C3", "Sandia C3:", "1/V", NIX),
                E("Herkunft", t("WRK_LBL_HERKUNFT"), "", DLG,
                  "CecWechselrichter.NachModell (CEC); ModulKatalogProfil (gesperrtes Feld)"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "WechselrichterStammCtrl.Update/Delete (Auslieferungssatz)"),
                // W6-B-10 (09.09.2026), Migrationsschritt 70: der KURZSCHLUSSstrom je
                // MPPT. Er steht hinter ReadOnly, weil die Spalte im Bestand ueber
                // ADD COLUMN ans Ende der Tabelle kommt und dieser Katalog der
                // TABELLENREIHENFOLGE folgt. Kein Import fuellt ihn - weder die
                // CEC-Liste noch das OND-Format fuehren einen Kurzschlussstrom je
                // Eingang.
                E("I_Sc_Max", t("WRK_LBL_I_SC_MAX"), "A", DLG,
                  "ModulKatalogProfil (Gruppe Eingang) - JE MPPT, Handpflege; " +
                  "StrangPlausibilitaet.MpptPruefen (P4 rot); " +
                  "StrangAuslegung.ParallelJeMppt (Grenze)")
            }.Concat(Katalogspalten(t)).ToList();
        }
    }
}
