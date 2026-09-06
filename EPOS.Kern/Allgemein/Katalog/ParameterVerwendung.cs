using System;
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
        Wechselrichter
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
    /// Der BELEG: Datei und Zeile, an der der Wert gelesen wird — leer nur bei
    /// <see cref="WindowsFormsApplication1.Verwendung.Keine"/>. Die Kern-Probe
    /// <c>ParameterVerwendungTests</c> faellt rot aus, sobald eine als
    /// <see cref="WindowsFormsApplication1.Verwendung.Simulation"/> eingestufte Spalte
    /// keine Fundstelle nennt.
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

        private static ParameterEintrag E(string spalte, string anzeige, string einheit,
                                          Verwendung[] verwendung, string fundstelle = "")
        {
            return new ParameterEintrag(spalte, anzeige, einheit ?? "", verwendung, fundstelle ?? "");
        }

        // =================================================================
        // 1. Heizkessel — Tab_Heizkessel_STAMM (23 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Der Befund dieser Tabelle:</b> Die fuenf Emissionsspalten des Kessels
        /// (<c>CO2</c> … <c>Staub</c>) werden GEPFLEGT, aber nicht gerechnet — der
        /// Rechenweg holt die Emissionsfaktoren aus <c>Tab_Brennstoff_Stamm</c>
        /// (<c>SimulationSPK.cs:151-158</c>). Sie sind deshalb <c>Dialog</c> und nicht
        /// <c>Simulation</c>.
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Heizkessel(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM,
                  "Tab_Energieanlagen.ID_Kessel → SimulationControl.cs:3886"),
                E("Bezeichner", t("HZKK_LBL_NAME"), "", SIM_BER,
                  "SimulationSPK.cs:131; AbweichungsErmittler.cs:95"),
                E("Firma", t("HZKK_LBL_HERSTELLER"), "", BER,
                  "AbweichungsErmittler.cs:96"),
                E("Beschreibung", t("HZKK_LBL_BESCHREIBUNG"), "", DLG,
                  "HeizkesselKatalogDialog.razor:69"),
                E("Ptherm", t("HZKK_LBL_PTHERM"), "kW", SIM_WIRT_BER,
                  "SimulationSPK.cs:148; WirtschaftlichkeitCtrl.cs:723; AbweichungsErmittler.cs:97"),
                E("Brennstoff", t("HZKK_LBL_ENERGIETRAEGER"), "", SIM_WIRT,
                  "SimulationSPK.cs:175; WirtschaftlichkeitCtrl.cs:721"),
                E("Wirkungsgrad_Gas", t("HZKK_LBL_WG_GAS"), "", SIM_WIRT_BER,
                  "SimulationSPK.cs:162; WirtschaftlichkeitCtrl.cs:736; AbweichungsErmittler.cs:98"),
                E("Wirkungsgrad_Öl", t("HZKK_LBL_WG_OEL"), "", SIM_WIRT_BER,
                  "SimulationSPK.cs:163; WirtschaftlichkeitCtrl.cs:737; AbweichungsErmittler.cs:99"),
                E("Investitionskosten", t("HZKK_LBL_INVEST"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:357 (BasenFuellen, ERZEUGER_HEIZKESSEL)"),
                E("Raumbedarf", t("HZKK_LBL_RAUMBEDARF"), "m³", DLG,
                  "HeizkesselKatalogDialog.razor:134"),
                E("Wartungskosten", t("KESSEL_WARTUNG_LBL"), "", WIRT,
                  "TechnikPlanwertCtrl.cs:823 (Betriebskosten-Planwert)"),
                E("Nutzungsdauer", t("HZKK_LBL_NUTZUNGSDAUER"), t("HZKK_EINHEIT_JAHRE"), DLG,
                  "HeizkesselKatalogDialog.razor:137"),
                E("CO2", "CO2:", "g / MWh", DLG,
                  "HeizkesselKatalogDialog.razor:164 — der Lauf nimmt Tab_Brennstoff_Stamm (SimulationSPK.cs:155)"),
                E("SO2", "SO2:", "g / MWh", DLG,
                  "HeizkesselKatalogDialog.razor:167 — der Lauf nimmt Tab_Brennstoff_Stamm (SimulationSPK.cs:156)"),
                E("NOx", "NOx:", "g / MWh", DLG,
                  "HeizkesselKatalogDialog.razor:170 — der Lauf nimmt Tab_Brennstoff_Stamm (SimulationSPK.cs:157)"),
                E("CO", "CO:", "g / MWh", DLG,
                  "HeizkesselKatalogDialog.razor:173 — Tab_Brennstoff_Stamm fuehrt kein CO"),
                E("Staub", t("HZKK_LBL_STAUB"), "g / MWh", DLG,
                  "HeizkesselKatalogDialog.razor:176 — der Lauf nimmt Tab_Brennstoff_Stamm (SimulationSPK.cs:158)"),
                E("Betriebsbereitschaftverlust", t("HZKK_LBL_BBVERLUST"), "%", SIM,
                  "SimulationSPK.cs:178"),
                E("Brennwert", t("HZKK_LBL_BRENNWERT"), "", BER,
                  "AbweichungsErmittler.cs:100"),
                E("Vorlauf", t("HZKK_LBL_VORLAUF"), "°C", SIM,
                  "SimulationControl.cs:3890 (KesselTemperaturpaarGepflegt); Warnkriterien.cs:1258"),
                E("Ruecklauf", t("HZKK_LBL_RUECKLAUF"), "°C", SIM,
                  "SimulationControl.cs:3890 (KesselTemperaturpaarGepflegt); Warnkriterien.cs:1258"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "HeizkesselStammCtrl.Ueberschreiben (Schreibschutz der Auslieferung)"),
                E("Wartungskosten_Einheit", t("KESSEL_WARTUNG_EINHEIT_LBL"), "", WIRT,
                  "TechnikPlanwertCtrl.cs:823 (Bezugsgroesse der Wartungskosten)")
            };
        }

        // =================================================================
        // 2. BHKW — Tab_BHKW_STAMM (27 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Der Befund dieser Tabelle:</b> <c>Investition_kwel</c> ist seit dem
        /// Nutzerentscheid vom 22.08.2026 ABGELEITET (<c>BHKWKosten.JeKWel</c> aus den
        /// fuenf Einzelposten) und hat im ganzen Bestand keinen Leser mehr — die
        /// Kostenplanung rechnet mit <c>Kosten_Modul</c> und den vier Nebenposten
        /// (<c>TechnikPlanwertCtrl.cs:317-325</c>). Er bleibt <c>Dialog</c>.
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Bhkw(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM_WIRT,
                  "Tab_Energieanlagen.ID_BHKW → WirtschaftlichkeitCtrl.cs:4265; TechnikPlanwertCtrl.cs:160"),
                E("Bezeichner", t("BHKWK_LBL_NAME"), "", SIM_BER,
                  "SimulationBHKW.cs:281 (ReadSingle); AbweichungsErmittler.cs:85"),
                E("Firma", t("BHKWK_LBL_HERSTELLER"), "", BER,
                  "AbweichungsErmittler.cs:86"),
                E("Beschreibung", t("BHKWK_LBL_BESCHREIBUNG"), "", DLG,
                  "BhkwKatalogDialog.razor:64"),
                E("Ptherm", t("BHKWK_LBL_PTHERM"), "kW", SIM_WIRT_BER,
                  "SimulationBHKW.cs:282; KostenEmissionRechner.cs:352; AbweichungsErmittler.cs:88"),
                E("Pel", t("BHKWK_LBL_PEL"), "kW", SIM_WIRT_BER,
                  "SimulationBHKW.cs:283; WirtschaftlichkeitCtrl.cs:3585 (KWKG-Deckel); AbweichungsErmittler.cs:89"),
                E("Brennstoff", t("BHKWK_LBL_ENERGIETRAEGER"), "", SIM_WIRT,
                  "SimulationBHKW.cs:286; WirtschaftlichkeitCtrl.cs:4263"),
                E("Wirkungsgrad", t("BHKWK_LBL_WIRKUNGSGRAD"), "", SIM_WIRT_BER,
                  "SimulationBHKW.cs:287; KostenEmissionRechner.cs:352; AbweichungsErmittler.cs:90"),
                E("Investition_kwel", t("BHKWK_LBL_INVEST"), "", DLG,
                  "BhkwKatalogDialog.razor:127 — abgeleitet (BHKWKosten.JeKWel), kein Leser im Rechenweg"),
                E("Raumbedarf", t("BHKWK_LBL_RAUMBEDARF"), "m³", DLG,
                  "BhkwKatalogDialog.razor:130"),
                E("Wartungskosten_kwhel", t("BHKWK_LBL_WARTUNG"), "€ / kWhel", WIRT,
                  "TechnikPlanwertCtrl.cs:706 (Betriebskosten-Planwert)"),
                E("Nutzungsdauer", t("BHKWK_LBL_NUTZUNGSDAUER"), t("HZKK_EINHEIT_JAHRE"), DLG,
                  "BhkwKatalogDialog.razor:137"),
                E("NOX", "NOx:", "g / MWh", SIM, "SimulationBHKW.cs:317"),
                E("SO2", "SO2:", "g / MWh", SIM, "SimulationBHKW.cs:316"),
                E("CO", "CO:", "g / MWh", SIM, "SimulationBHKW.cs:318"),
                E("CO2", "CO2:", "g / MWh", SIM, "SimulationBHKW.cs:315"),
                E("Staub", t("HZKK_LBL_STAUB"), "g / MWh", SIM, "SimulationBHKW.cs:319"),
                E("Motortyp", t("BHKWK_LBL_MOTORTYP"), "", BER,
                  "AbweichungsErmittler.cs:87"),
                E("Grenzleistung", t("BHKWK_LBL_GRENZLEISTUNG"), "%", SIM,
                  "SimulationBHKW.cs:312 (Teillastgrenze, Prozent → Faktor)"),
                E("Kosten_Modul", t("BHKWK_LBL_MODUL"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:317"),
                E("Kosten_Montage", t("BHKWK_LBL_MONTAGE"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:322"),
                E("Kosten_Lieferung", t("BHKWK_LBL_LIEFERUNG"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:323"),
                E("Kosten_Schallschutzhaube", t("BHKWK_LBL_SCHALLSCHUTZ"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:324"),
                E("Kosten_Abgasreinigung", t("BHKWK_LBL_ABGASREINIGUNG"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:325"),
                E("Vorlauf", t("BHKWK_LBL_VORLAUF"), "°C", BER,
                  "AbweichungsErmittler.cs:91 — der Lauf nimmt Tab_Energieanlagen.Vorlauf"),
                E("Ruecklauf", t("BHKWK_LBL_RUECKLAUF"), "°C", BER,
                  "AbweichungsErmittler.cs:92 — der Lauf nimmt Tab_Energieanlagen.[Rücklauf]"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "BHKWStammCtrl.IstSchreibgeschuetzt (Rueckfrage beim Ueberschreiben)")
            };
        }

        // =================================================================
        // 3. Waermepumpe — Tab_WP_STAMM (20 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Die Kennfelder stehen nicht hier.</b> COP und Leistung je Vorlauf und
        /// Quelltemperatur fuehrt <c>Tab_Kenndaten_STAMM</c> (Kopie
        /// <c>Tab_Kenndaten</c>); der Lauf liest sie in
        /// <c>SimulationWaermepumpe.cs:611</c>, gepflegt werden sie im
        /// <c>KennlinienEditorDialog</c> (W7.2). Sie sind KEINE Spalten dieser Tabelle
        /// und stehen deshalb nicht in diesem Katalog.
        ///
        /// <para><b>Der Befund dieser Tabelle:</b> <c>Modulkosten</c> ist der einzige
        /// Kostenwert der Waermepumpe, den die Kostenplanung liest
        /// (<c>TechnikPlanwertCtrl.cs:345</c>) — und der einzige gerechnete Parameter
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
                  "Tab_Energieanlagen.ID_WP → SimulationWaermepumpe.cs:538; Hydraulikbild.cs:175"),
                E("Bezeichner", t("WPS_LBL_NAME"), "", SIM_BER,
                  "SimulationWaermepumpe.cs:538 (ID_WP der Anlage); AbweichungsErmittler.cs:75"),
                E("Firma", t("WPS_LBL_HERSTELLER"), "", BER,
                  "AbweichungsErmittler.cs:76"),
                E("Beschreibung", t("WPS_LBL_BESCHREIBUNG"), "", DLG,
                  "WaermepumpeStammDialog.razor (Feld Beschreibung)"),
                E("Typ", t("WPS_LBL_TYP"), "", SIM_BER,
                  "SimulationWaermepumpe.cs:543 (Bauart → Quellenwahl); Warnkriterien.cs:669; AbweichungsErmittler.cs:77"),
                E("Baujahr", t("WPS_LBL_BAUJAHR"), "", DLG,
                  "WaermepumpeStammDialog.razor (Feld Baujahr)"),
                E("Aufstellung", t("WPS_LBL_AUFSTELLUNG"), "", DLG,
                  "WaermepumpenKatalogFilter.cs:98 (Katalogfilter W7.1)"),
                E("Nennleistung", t("WPS_LBL_NENNLEISTUNG"), "kW", SIM_BER,
                  "SimulationWaermepumpe.cs:541 (Grenzleistung des Moduls); AbweichungsErmittler.cs:79"),
                E("maxPtherm", t("PARV_LBL_MAXPTHERM"), "kW", BER,
                  "AbweichungsErmittler.cs:80 — im Stammdialog nicht sichtbar, laeuft verborgen mit"),
                E("Heizung", t("WPS_LBL_HEIZSTAB"), "kW", SIM,
                  "SimulationWaermepumpe.cs:542 (WP_Heizung, Heizstabphase :1553)"),
                E("Regelung", t("WPS_LBL_REGELUNG"), "", BER,
                  "AbweichungsErmittler.cs:82; WaermepumpenKatalogFilter.cs:96"),
                E("Modulkosten", t("MODK_LBL_MODULKOSTEN"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:345 (BasenFuellen, ERZEUGER_WAERMEPUMPE) — " +
                  "im Stammdialog nur lesend (W14a-O-1); geschrieben wird die Spalte " +
                  "heute von KEINEM Importweg (Befund W14a-O-2)"),
                E("Laenge", t("MODK_LBL_LAENGE"), "mm", NIX),
                E("Breite", t("MODK_LBL_BREITE"), "mm", NIX),
                E("Hoehe", t("PARV_LBL_HOEHE"), "mm", NIX),
                E("Gewicht", t("PARV_LBL_GEWICHT"), "kg", NIX),
                E("Raum", t("HZKK_LBL_RAUMBEDARF"), "m³", NIX),
                E("Kuehlleistung", t("WPS_LBL_KUEHLLEISTUNG"), "kW", BER,
                  "AbweichungsErmittler.cs:81"),
                E("Bauart", t("WPK_LBL_BAUART"), "", BER,
                  "AbweichungsErmittler.cs:78; WaermepumpenKatalogFilter.cs:98"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "WPStammCtrl.Speichern (Auslieferungssatz, Liste gedimmt)")
            };
        }

        // =================================================================
        // 4. Solarkollektoren — Tab_Solarkollektoren_STAMM (16 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Der Befund dieser Tabelle:</b> <c>Kdfu</c> (im Editor „Kdiff") wird
        /// gepflegt, aber nirgends gerechnet — der Kollektorwirkungsgrad benutzt nur
        /// <c>h0</c>, <c>k1</c>, <c>k2</c> und <c>Kdir</c>
        /// (<c>SimulationSolarthermie.cs:242-245</c>). Und <c>Modulflaeche</c> ist die
        /// Flaeche EINES Moduls, gerechnet wird mit <c>Aperturflaeche</c> mal
        /// <c>Tab_Energieanlagen.Kollektormodulanzahl</c>.
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Solarkollektoren(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM_WIRT,
                  "Tab_Energieanlagen.ID_Solar → SimulationSolarthermie.cs:230; TechnikPlanwertCtrl.cs:163"),
                E("Bezeichner", t("SKK_LBL_NAME"), "", SIM_BER,
                  "SimulationSolarthermie.cs:251; AbweichungsErmittler.cs:103"),
                E("Firma", t("SKK_LBL_HERSTELLER"), "", DLG,
                  "SolarkollektorKatalogDialog.razor:53"),
                E("Beschreibung", t("SKK_LBL_BESCHREIBUNG"), "", DLG,
                  "SolarkollektorKatalogDialog.razor:55"),
                E("Kollektortyp", t("SKK_LBL_TYP"), "", BER,
                  "AbweichungsErmittler.cs:104"),
                E("Modulflaeche", t("SKK_LBL_MODULFLAECHE"), "m²", DLG,
                  "SolarkollektorKatalogDialog.razor:71 — gerechnet wird mit Aperturflaeche"),
                E("Aperturflaeche", t("SKK_LBL_APERTURFLAECHE"), "m²", SIM_BER,
                  "SimulationSolarthermie.cs:232; AbweichungsErmittler.cs:105"),
                E("h0", "h0:", "", SIM, "SimulationSolarthermie.cs:242 (Konversionsfaktor)"),
                E("k1", "k1:", "W/(m²*K)", SIM, "SimulationSolarthermie.cs:243"),
                E("k2", "k2:", "W/(m²*K²)", SIM, "SimulationSolarthermie.cs:244"),
                E("Kdir", "Kdir:", "", SIM, "SimulationSolarthermie.cs:245 (IAM, direkt)"),
                E("Kdfu", "Kdiff:", "50°", DLG,
                  "SolarkollektorKatalogDialog.razor:87 — kein Leser im Rechenweg"),
                E("Investitionskosten", t("SKK_LBL_KOSTEN"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:341 (Stueckpreis, ERZEUGER_SOLARTHERMIE)"),
                E("Vorlauf", t("SKK_LBL_VORLAUF"), "°C", DLG,
                  "SolarkollektorKatalogDialog.razor:93 — der Lauf nimmt Tab_Energieanlagen.Vorlauf"),
                E("Ruecklauf", t("SKK_LBL_RUECKLAUF"), "°C", DLG,
                  "SolarkollektorKatalogDialog.razor:96 — der Lauf nimmt Tab_Energieanlagen.[Rücklauf]"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "SolarkollektorenStammCtrl (Auslieferungssatz)")
            };
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
                  "Tab_Energieanlagen.ID_PV → SimulationPV.cs:179; TechnikPlanwertCtrl.cs:162"),
                E("Bezeichner", t("MODK_LBL_BEZEICHNER_PV"), "", SIM_BER,
                  "SimulationPV.cs:467 (Modulname der Meldungen); AbweichungsErmittler.cs:108"),
                E("Firma", t("MODK_LBL_FIRMA"), "", BER,
                  "AbweichungsErmittler.cs:109"),
                E("Beschreibung", t("MODK_LBL_BESCHREIBUNG"), "", DLG,
                  "ModulKatalogDialog.razor (Feld Beschreibung)"),
                E("Leistung", t("MODK_LBL_PMAX"), "W", SIM_BER,
                  "SimulationPV.cs:497 (P_STC der Anlage); AbweichungsErmittler.cs:110"),
                E("Wirkungsgrad", t("MODK_LBL_WIRKUNGSGRAD"), "%", SIM_BER,
                  "SimulationPV.cs:184/480; AbweichungsErmittler.cs:111"),
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
                  "SimulationPV.cs:535 (Temperaturkoeffizient des Huld-Modells)"),
                E("T_NOCT", t("PV_MODUL_LABEL_TNOCT"), "°C", SIM,
                  "SimulationPV.cs:508 (Zelltemperatur)"),
                E("Laenge", t("MODK_LBL_LAENGE"), "m", SIM,
                  "SimulationPV.cs:183/480 (Modulflaeche)"),
                E("Breite", t("MODK_LBL_BREITE"), "m", SIM,
                  "SimulationPV.cs:183/480 (Modulflaeche)"),
                E("Modulkosten", t("MODK_LBL_MODULKOSTEN_PV"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:349 (Stueckpreis, ERZEUGER_PHOTOVOLTAIK)"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "PhotovoltaikStammCtrl.SpeichernAus (Auslieferungssatz)"),
                E("Technologie", t("PVM_MODUL_LABEL_TECHNOLOGIE"), "", SIM_BER,
                  "SimulationPV.cs:590 (Huld-Satz je Zelltechnologie); AbweichungsErmittler.cs:112")
            };
        }

        // =================================================================
        // 6. Stromspeicher — Tab_Stromspeicher_STAMM (15 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Der Befund dieser Tabelle:</b> Sie ist die einzige der sieben, in der
        /// JEDE Fachspalte gerechnet wird — <c>StromspeicherSimCtrl.cs:1109-1122</c>
        /// liest alle elf und reicht sie als <c>SpeicherParameter</c> an die
        /// <c>SpeicherEngine</c>. Vier davon sind Kostengroessen und gehen ausserdem in
        /// den Kostenplanwert (<c>TechnikPlanwertCtrl.cs:330-340</c>).
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Stromspeicher(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM_WIRT,
                  "Tab_Energieanlagen.ID_SP → StromspeicherSimCtrl.cs:1055; TechnikPlanwertCtrl.cs:164"),
                E("Bezeichner", t("MODK_LBL_BEZEICHNER"), "", SIM_BER,
                  "StromspeicherSimCtrl.cs:1054; AbweichungsErmittler.cs:120"),
                E("Typ", t("MODK_LBL_TYP"), "", BER,
                  "AbweichungsErmittler.cs:121"),
                E("Leistung", t("MODK_LBL_LEISTUNG"), "kW", SIM_WIRT_BER,
                  "StromspeicherSimCtrl.cs:1110; TechnikPlanwertCtrl.cs:333; AbweichungsErmittler.cs:122"),
                E("Energie", t("SP_LABEL_ENERGIE_KURZ"), "kWh", SIM_WIRT_BER,
                  "StromspeicherSimCtrl.cs:1109; TechnikPlanwertCtrl.cs:331; AbweichungsErmittler.cs:123"),
                E("Degradation", t("MODK_LBL_DEGRADATION"), "%", SIM,
                  "StromspeicherSimCtrl.cs:1118 (SpeicherParameter.DegradationProA)"),
                E("Ladezustand", t("MODK_LBL_LADEZUSTAND"), "%", SIM,
                  "StromspeicherSimCtrl.cs:1117 (nutzbarer Hub)"),
                E("Modulkosten", t("MODK_LBL_MODULKOSTEN"), "€/kWh", WIRT,
                  "TechnikPlanwertCtrl.cs:330 (spezifischer Kapazitaetspreis)"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "StromspeicherStammCtrl.SpeichernAus (Auslieferungssatz)"),
                E("Wirkungsgrad_RT", t("SP_LABEL_WIRKUNGSGRAD_RT"), "-", SIM,
                  "StromspeicherSimCtrl.cs:1120 (Umlaufwirkungsgrad der SpeicherEngine)"),
                E("Zyklen_Zugesichert", t("SP_LABEL_ZYKLEN"), "-", SIM_WIRT,
                  "StromspeicherSimCtrl.cs:1122; SpeicherEngine/ArbitragePlaner.cs:198 (Zyklenbudget)"),
                E("Verschleisskosten", t("SP_LABEL_VERSCHLEISSKOSTEN"), t("SP_EINHEIT_ZYKLUSKOSTEN"), WIRT,
                  "StromspeicherSimCtrl.cs:1121; SpeicherEngine/ArbitrageOptionen.cs:178"),
                E("Leistungskosten", t("SP_LABEL_LEISTUNGSKOSTEN"), "€/kW", WIRT,
                  "TechnikPlanwertCtrl.cs:332"),
                E("Investition_Fix", t("SP_LABEL_INVESTITION_FIX"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:334; StromspeicherSimCtrl.cs:1114"),
                E("Standby_Verbrauch", t("SP_LABEL_STANDBY"), "W", SIM,
                  "StromspeicherSimCtrl.cs:1115 (Eigenverbrauch der Leistungselektronik)")
            };
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
        /// </remarks>
        private static IReadOnlyList<ParameterEintrag> Pufferspeicher(Func<string, string> t)
        {
            return new[]
            {
                E("ID", "ID:", "", SIM_WIRT,
                  "Tab_Energieanlagen.ID_PUFFER → WaermesenkeClass.cs:574; TechnikPlanwertCtrl.cs:165"),
                E("Bezeichner", t("PSPK_LBL_NAME"), "", SIM_BER,
                  "WaermesenkeClass.cs:1284/1561; AbweichungsErmittler.cs:115"),
                E("Hersteller", t("PSPK_LBL_HERSTELLER"), "", DLG,
                  "PufferSpKatalogDialog.razor:61; Herstellerfilter des Browsers"),
                E("Speichertyp", t("PSPK_LBL_SPEICHERTYP"), "", SIM_BER,
                  "Warnkriterien.cs:1307/984 (Kriterium W4, Kombi- und Solarspeicher); AbweichungsErmittler.cs:116"),
                E("Bereitschaftsverluste", t("PSPK_LBL_VERLUSTE"), "kWh/d", SIM,
                  "SimulationControl.cs:1681 (SimulationPufferspeicher.Init); WaermequelleClass.cs:805"),
                E("Gesamtvolumen", t("PSPK_LBL_VOLUMEN"), "l", SIM_BER,
                  "SimulationControl.cs:1681; WaermequelleClass.cs:803; AbweichungsErmittler.cs:117"),
                E("Investitionskosten", t("PSPK_LBL_INVEST"), "€", WIRT,
                  "TechnikPlanwertCtrl.cs:357 (KOSTEN_KOMPONENTE_PUFFERSPEICHER)"),
                E("ReadOnly", t("PARV_LBL_READONLY"), "", DLG,
                  "PufferSpStammCtrl.Ueberschreiben (Auslieferungssatz)")
            };
        }

        // =================================================================
        // 8. Wechselrichter — Tab_Wechselrichter_STAMM (34 Spalten)
        // =================================================================

        /// <remarks>
        /// <b>Der Befund dieser Tabelle in Stufe S1: KEINE Spalte wird gerechnet.</b>
        /// Das ist kein Versaeumnis, sondern die Zusage des Anwenderentscheids W6-E-2
        /// vom 06.09.2026: „S1 Katalog, Verwaltung und Import sofort und OHNE
        /// Rechenwirkung". Der Katalog wird gepflegt und importiert, gelesen wird er
        /// erst mit der Strangzuordnung (S2) und dem Rechenweg (S3) — dann wandern die
        /// Kennlinienspalten auf <c>Simulation</c> und <c>Kosten</c> auf
        /// <c>Wirtschaftlichkeit</c> (Konzept 4.1 und Entscheidungsfrage Q8).
        /// Der Fall <c>Der_Wechselrichter_rechnet_in_S1_noch_nicht</c> haelt diesen
        /// Zustand fest: Faellt er rot aus, ist S3 gelaufen und die Einstufung muss mit.
        ///
        /// <para><b>Die sieben Sandia-Spalten stehen als <c>Keine</c> da</b> — sie sind
        /// mitgeschriebenes Katalogwissen (Konzept 3.3.3): Der CEC-Import schreibt sie
        /// verlustfrei mit, damit ein spannungsabhaengiges Modell (Stufe E3 des
        /// PV-Ertragsmodells) sie spaeter ohne Neuimport vorfindet. Heute liest sie
        /// nichts — auch die Verwaltung zeigt sie nicht, weil sie kein Anwender von
        /// Hand pflegen kann.</para>
        /// </remarks>
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
                E("P_AC_Nenn", t("WRK_LBL_P_AC_NENN"), "kW", DLG,
                  "ModulKatalogProfil (Pflichtfeld); WechselrichterPlausibilitaet.PruefeLeistungen"),
                E("S_AC_Max", t("WRK_LBL_S_AC_MAX"), "kVA", DLG,
                  "ModulKatalogProfil (Gruppe Geraet); WechselrichterPlausibilitaet"),
                E("P_DC_Max", t("WRK_LBL_P_DC_MAX"), "kW", DLG,
                  "ModulKatalogProfil (Gruppe Geraet); WechselrichterPlausibilitaet"),
                E("U_Mpp_Min", t("WRK_LBL_U_MPP_MIN"), "V", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeSpannungen"),
                E("U_Mpp_Max", t("WRK_LBL_U_MPP_MAX"), "V", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeSpannungen"),
                E("U_Dc_Max", t("WRK_LBL_U_DC_MAX"), "V", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeSpannungen"),
                E("U_Start", t("WRK_LBL_U_START"), "V", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeSpannungen"),
                E("I_Dc_Max", t("WRK_LBL_I_DC_MAX"), "A", DLG,
                  "ModulKatalogProfil (Gruppe Eingang) - JE MPPT, siehe WechselrichterSchema"),
                E("Anzahl_Mppt", t("WRK_LBL_ANZAHL_MPPT"), "", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeMppt"),
                E("Straenge_Je_Mppt", t("WRK_LBL_STRAENGE_JE_MPPT"), "", DLG,
                  "ModulKatalogProfil (Gruppe Eingang); WechselrichterPlausibilitaet.PruefeMppt"),
                E("Eta05", t("WRK_LBL_ETA05"), "-", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta10", t("WRK_LBL_ETA10"), "-", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta20", t("WRK_LBL_ETA20"), "-", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta30", t("WRK_LBL_ETA30"), "-", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta50", t("WRK_LBL_ETA50"), "-", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta100", t("WRK_LBL_ETA100"), "-", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); WechselrichterKennlinie.EuroWirkungsgrad"),
                E("Eta_Euro", t("WRK_LBL_ETA_EURO"), "-", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad) - Ausweis des Datenblatts"),
                E("Eta_Max", t("WRK_LBL_ETA_MAX"), "-", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad) - Ausweis des Datenblatts"),
                E("P_Standby", t("WRK_LBL_P_STANDBY"), "W", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); CEC-Import (Pso)"),
                E("P_Nacht", t("WRK_LBL_P_NACHT"), "W", DLG,
                  "ModulKatalogProfil (Gruppe Wirkungsgrad); CEC-Import (Pnt)"),
                E("Kosten", t("WRK_LBL_KOSTEN"), "€", DLG,
                  "ModulKatalogProfil (Gruppe Geraet) - in S3 die Investition je Geraet (Q8)"),
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
                  "WechselrichterStammCtrl.Update/Delete (Auslieferungssatz)")
            };
        }
    }
}
