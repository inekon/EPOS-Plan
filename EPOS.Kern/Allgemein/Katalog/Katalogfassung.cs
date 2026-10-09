using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // KATALOGFASSUNG: SCHLÜSSEL UND PRÜFSUMME JE AUSGELIEFERTEM SATZ - Entscheidungsvorlage
    // Modellgrenzen KU1, Stufe 1 (Welle M6 „Katalog-Update") und Stufe 2 (die übrigen Kataloge).
    //
    // WAS. Jeder ausgelieferte Satz (ReadOnly = 1) eines Katalogs des Registers trägt einen
    // stabilen SCHLÜSSEL (über Programmfassungen gleich) und die PRÜFSUMME seiner ausgelieferten
    // Fachwerte. Ein Update erkennt daran, ob der Anwender den Satz angefasst hat: Prüfsumme der
    // Zeile = gespeicherte Prüfsumme heißt „wie ausgeliefert", also darf der neue Stand darüber.
    //
    // DAS REGISTER. Stufe 1 sind die Kataloge mit laufender Pflege: Wärmepumpen samt Kennlinien und
    // Kühlkennlinien, Heizkessel, BHKW, PV-Module, Brauchwasser- und Prozessprofile mit ihren
    // Wochenprofilen (Typen). Stufe 2 sind die übrigen Kataloge (Baustoffe, Bauteilaufbauten,
    // Brennstoffe, Tagesverteilungen, Gebäude, Konditionierungsvorlagen, Pufferspeicher und ihre
    // Auslegungsvorgaben, Solarkollektoren, Ganglinien, Stromspeicher, Stromverbraucher samt Typen,
    // Wärmebedarfsganglinien, Wechselrichter). BENANNT AUSGENOMMEN (Katalogfassung.Ausgenommen) sind
    // der Klimakatalog und der Zapfprofilkatalog - Gründe dort.
    //
    // DIE PRÜFSUMME ist SHA-256 über die Fachspalten in der festen Folge von
    // Katalogtabelle.Fachspalten, je Spalte „Name=Wert" (Zahlen invariant und rundlauffest, Text
    // unverändert, LEER trägt nichts bei - eine neue, leer angelegte Spalte verschiebt keine
    // Prüfsumme). Die Kindzeilen gehen hinter dem Kopf ein: sortiert, wenn ihre Reihenfolge nichts
    // bedeutet (Kennlinien), in ihrer Folge mit Position, wenn sie eine REIHE sind (Ganglinien,
    // Tagesverteilungen). Enkelzeilen (die Perioden eines Konditionierungskalenders) gehen sortiert
    // in die Zeile ihres Kindes ein. Ein VERWEIS (Spalte mit der ID eines Satzes einer anderen
    // Tabelle) geht mit dem stabilen Namen des Ziels ein, nie mit der ID - die ist je Datenbank
    // eine andere.
    //
    // DER SCHLÜSSEL ist Tabellenkürzel und bereinigter Name („WP:LW_12_A"); der Name ist der
    // Bezeichner oder, wo eine Tabelle einen anderen eindeutigen Namen führt, dessen Spalten
    // (Konditionierungsvorlage: Größe und Bezeichner). Belegt ein anderer Satz derselben Tabelle den
    // Schlüssel schon, kommt ein Zähler dazu („_2", „_3"). Ein Anwendersatz (ReadOnly = 0) bekommt
    // keinen.
    // ====================================================================================

    /// <summary>
    /// Ein Verweis einer Katalogspalte auf den Satz einer anderen Tabelle (etwa die Schicht eines
    /// Bauteilaufbaus auf ihren Baustoff). In Prüfsumme und Paket steht der Wert der
    /// <see cref="Zielspalte"/> des Ziels, beim Schreiben wird er wieder zur ID des Ziels.
    /// </summary>
    public sealed class Katalogverweis
    {
        internal Katalogverweis(string spalte, string zieltabelle, string zielspalte)
        {
            Spalte = spalte;
            Zieltabelle = zieltabelle;
            Zielspalte = zielspalte;
        }

        /// <summary>Die Spalte mit der ID des Ziels.</summary>
        public string Spalte { get; }

        /// <summary>Die Tabelle des Ziels.</summary>
        public string Zieltabelle { get; }

        /// <summary>Die Spalte des Ziels mit dem stabilen Namen.</summary>
        public string Zielspalte { get; }
    }

    /// <summary>Eine Kindtabelle eines Katalogsatzes (etwa die Kennlinie der Wärmepumpe).</summary>
    public sealed class Katalogkind
    {
        internal Katalogkind(string tabelle, string fremdschluessel, string[] fachspalten, string projektspalte = null)
        {
            Tabelle = tabelle;
            Fremdschluessel = fremdschluessel;
            Fachspalten = fachspalten;
            Projektspalte = projektspalte;
        }

        /// <summary>Die Kindtabelle.</summary>
        public string Tabelle { get; }

        /// <summary>Die Spalte, die auf den Kopfsatz (beim Enkel: auf die Kindzeile) zeigt.</summary>
        public string Fremdschluessel { get; }

        /// <summary>Die Fachspalten in fester Folge.</summary>
        public IReadOnlyList<string> Fachspalten { get; }

        /// <summary>
        /// Führt die Kindtabelle eine Projektspalte, gehören nur Zeilen mit leerem oder 0
        /// dazu — eine Zeile mit Projekt ist eine Projektkopie und wird nie angefasst.
        /// </summary>
        public string Projektspalte { get; }

        /// <summary>
        /// <c>true</c> = die Zeilen sind eine REIHE: Ihre Folge (nach ID) ist Fachinhalt, die Prüfsumme
        /// nimmt sie mit Position, das Paket in derselben Folge.
        /// </summary>
        public bool Geordnet { get; init; }

        /// <summary>Kindtabellen der Kindzeile (Fremdschlüssel auf die ID der Kindzeile).</summary>
        public IReadOnlyList<Katalogkind> Enkel { get; init; } = Array.Empty<Katalogkind>();

        /// <summary>Spalten mit der ID eines Satzes einer anderen Tabelle.</summary>
        public IReadOnlyList<Katalogverweis> Verweise { get; init; } = Array.Empty<Katalogverweis>();

        /// <summary>
        /// Spalten anderer Eigentümer derselben Tabelle (etwa <c>ID_Gebaeude</c> neben
        /// <c>ID_Vorlage</c>) — weder Fachwert noch Fremdschlüssel dieses Kindes; der Abgleich lässt sie leer.
        /// </summary>
        public IReadOnlyList<string> Nebenspalten { get; init; } = Array.Empty<string>();

        /// <summary>Eine eingefügte Kindzeile trägt <c>ReadOnly = 1</c> (wo die Tabelle das Kennzeichen pflegt).</summary>
        public bool GesperrtEinfuegen { get; init; }

        /// <summary>Eine einspaltige Reihe ohne Enkel und Verweis — im Paket als bloße Werteliste.</summary>
        public bool Reihe => Geordnet && Fachspalten.Count == 1 && Enkel.Count == 0 && Verweise.Count == 0;

        /// <summary>
        /// Eine Spalte, die bei den Zeilen DIESES Kindes leer ist (Schritt ZK: <c>ID_Zone_Stamm</c> — die
        /// Konditionierung einer Katalogzone trägt den Katalogbau daneben, gehört aber nicht zu seiner Ebene).
        /// Führt die Datenbank die Spalte nicht, gilt sie als leer.
        /// </summary>
        public string Leerspalte { get; init; }

        /// <summary>Die Bedingung „gehört zum Katalog" (leer ohne Projektspalte und ohne Leerspalte).</summary>
        internal string Katalogbedingung =>
            (Projektspalte == null ? "" : " AND COALESCE(\"" + Projektspalte + "\", 0) = 0") +
            (Leerspalte != null && DataRepository.SpalteVorhanden(Tabelle, Leerspalte) ? " AND \"" + Leerspalte + "\" IS NULL" : "");
    }

    /// <summary>Eine Katalogtabelle des Registers.</summary>
    public sealed class Katalogtabelle
    {
        internal Katalogtabelle(string tabelle, string kuerzel, string[] fachspalten, params Katalogkind[] kinder)
        {
            Tabelle = tabelle;
            Kuerzel = kuerzel;
            Fachspalten = fachspalten;
            Kinder = kinder ?? new Katalogkind[0];
        }

        /// <summary>Die Katalogtabelle (<c>…_STAMM</c>).</summary>
        public string Tabelle { get; }

        /// <summary>Das Kürzel vor dem Schlüssel.</summary>
        public string Kuerzel { get; }

        /// <summary>Die Fachspalten in fester Folge — alle Spalten außer ID, ReadOnly und den Katalogspalten.</summary>
        public IReadOnlyList<string> Fachspalten { get; }

        /// <summary>Die Kindtabellen, deren Zeilen zum Satz gehören.</summary>
        public IReadOnlyList<Katalogkind> Kinder { get; }

        /// <summary>Die Spalten des eindeutigen Namens (Anzeige, Schlüsselstamm, Namensprüfung).</summary>
        public IReadOnlyList<string> Namensspalten { get; init; } = new[] { Katalogfassung.SPALTE_BEZEICHNER };

        /// <summary>Spalten des Kopfes mit der ID eines Satzes einer anderen Tabelle.</summary>
        public IReadOnlyList<Katalogverweis> Verweise { get; init; } = Array.Empty<Katalogverweis>();

        /// <summary>Die Stufe der Entscheidungsvorlage (1 laufend gepflegt, 2 übrige Kataloge).</summary>
        public int Stufe { get; init; } = 1;

        /// <summary>Der Ressourcenschlüssel des Katalognamens (Dialog, Bericht).</summary>
        public string Anzeigeschluessel { get; init; } = "";

        public override string ToString() => Tabelle;
    }

    /// <summary>
    /// <b>Die Katalogfassung</b> — das Register der Kataloge (Stufe 1 und 2), Schlüsselbildung und
    /// Prüfsumme. Regeln im Kopf der Datei.
    /// </summary>
    public static class Katalogfassung
    {
        /// <summary>Die stabile Textkennung eines ausgelieferten Satzes.</summary>
        public const string SPALTE_SCHLUESSEL = "Katalog_Schluessel";

        /// <summary>Die Prüfsumme des ausgelieferten Stands (SHA-256, 64 Hexzeichen).</summary>
        public const string SPALTE_PRUEFSUMME = "Katalog_Pruefsumme";

        /// <summary>1 = der Satz ist in einer späteren Auslieferung entfallen; er bleibt stehen.</summary>
        public const string SPALTE_AUSGELAUFEN = "Katalog_Ausgelaufen";

        /// <summary>Die Spalte der Programmfassung an <c>Tab_Applikation</c>; leer = noch nie abgeglichen.</summary>
        public const string SPALTE_FASSUNG = "Katalogfassung";

        /// <summary>Die Statustabelle mit der Katalogfassung.</summary>
        public const string TAB_APPLIKATION = "Tab_Applikation";

        /// <summary>Die übliche Namensspalte (die Stufe 1 führt nur sie).</summary>
        public const string SPALTE_BEZEICHNER = "Bezeichner";

        /// <summary>Das Auslieferungskennzeichen.</summary>
        public const string SPALTE_READONLY = "ReadOnly";

        /// <summary>Höchstlänge des Schlüssels (Prüfklausel der Spalte).</summary>
        public const int SCHLUESSEL_MAX = 120;

        /// <summary>Trennzeichen der Namensspalten im Anzeigenamen.</summary>
        public const string NAMENSTRENNER = " / ";

        /// <summary>
        /// Die Spalten, die KEIN Fachwert sind: Kennung, Auslieferungskennzeichen und die drei
        /// Katalogspalten. Eine Katalogkopie, die Dublettenprüfung und die Prüfsumme lassen sie aus.
        /// <para>Dazu die Kostenvorlage des Satzes (<see cref="KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE"/>,
        /// Schritt 208): ein Verweis auf eine Anwendervorlage, die kein Paket führt — sie verändert weder Prüfsumme noch
        /// Paket, und eine Katalogkopie beginnt mit der Standardvorlage des Gewerks.</para>
        /// </summary>
        public static readonly IReadOnlyCollection<string> Metaspalten =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ID", SPALTE_READONLY, SPALTE_SCHLUESSEL, SPALTE_PRUEFSUMME, SPALTE_AUSGELAUFEN,
                KatalogkostenUrsprungSchema.SPALTE_ID_KOSTENVORLAGE
            };

        /// <summary>Ist <paramref name="spalte"/> eine der drei Katalogspalten?</summary>
        public static bool IstKatalogspalte(string spalte) =>
            string.Equals(spalte, SPALTE_SCHLUESSEL, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(spalte, SPALTE_PRUEFSUMME, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(spalte, SPALTE_AUSGELAUFEN, StringComparison.OrdinalIgnoreCase);

        private static string[] Stunden168()
        {
            var s = new string[168];
            for (int i = 0; i < 168; i++) s[i] = (i + 1).ToString(CultureInfo.InvariantCulture);
            return s;
        }

        private static string[] Monate12()
        {
            var s = new string[12];
            for (int i = 0; i < 12; i++) s[i] = "Monat_" + (i + 1).ToString(CultureInfo.InvariantCulture);
            return s;
        }

        private static string[] Verbinden(params string[][] teile) => teile.SelectMany(t => t).ToArray();

        private static readonly Katalogtabelle[] STUFE1 =
        {
            new Katalogtabelle("Tab_WP_STAMM", "WP",
                new[]
                {
                    "Bezeichner", "Firma", "Beschreibung", "Typ", "Baujahr", "Aufstellung", "Nennleistung",
                    "maxPtherm", "Heizung", "Regelung", "Modulkosten", "Laenge", "Breite", "Hoehe", "Gewicht",
                    "Raum", "Kuehlleistung", "Bauart", "Kuehlbetrieb", "Kuehl_Vorlauf", "Kuehl_Hilfsstromanteil",
                    "Mindestleistung_kW", "Taktverlustfaktor_Cd",
                    // UebergabegrenzeSchema (Schritt 205): die acht Geraetespalten; leer tragen sie nichts zur Pruefsumme bei.
                    "Spreizung_Auslegung_K", "Spreizung_Max_K", "Spreizung_Min_K", "Mindestvolumenstrom_Prozent",
                    "Ruecklauf_Max", "Ruecklauf_Bezug", "Ruecklauf_Abwertung_ProzentJeK", "Kaeltemittel"
                },
                new Katalogkind("Tab_Kenndaten_STAMM", "ID_WP", new[] { "Vorlauf", "Temperatur", "COP", "Ptherm" }),
                new Katalogkind("Tab_Kenndaten_Kuehlung_STAMM", "ID_WP",
                                new[] { "Vorlauf", "Temperatur", "COP", "Pkuehl", "Last" }, "ID_Projekt"))
            { Anzeigeschluessel = "KABG_KATALOG_WP" },
            new Katalogtabelle("Tab_Heizkessel_STAMM", "KES",
                new[]
                {
                    "Bezeichner", "Firma", "Beschreibung", "Ptherm", "Brennstoff", "Wirkungsgrad_Gas",
                    "Wirkungsgrad_Öl", "Investitionskosten", "Raumbedarf", "Wartungskosten", "Nutzungsdauer",
                    "CO2", "SO2", "NOx", "CO", "Staub", "Betriebsbereitschaftverlust", "Brennwert", "Vorlauf",
                    "Ruecklauf", "Wartungskosten_Einheit", "Wirkungsgrad_Teillast30", "Kennlinie_Brennwert",
                    "Mindestleistung", "Anfahrverlust_kWh", "Mindestlaufzeit_min", "Bereitschaft_Einheit"
                })
            { Anzeigeschluessel = "KABG_KATALOG_KESSEL" },
            new Katalogtabelle("Tab_BHKW_STAMM", "BHKW",
                new[]
                {
                    "Bezeichner", "Firma", "Beschreibung", "Ptherm", "Pel", "Brennstoff", "Wirkungsgrad",
                    "Investition_kwel", "Raumbedarf", "Wartungskosten_kwhel", "Nutzungsdauer", "NOX", "SO2", "CO",
                    "CO2", "Staub", "Motortyp", "Grenzleistung", "Kosten_Modul", "Kosten_Montage",
                    "Kosten_Lieferung", "Kosten_Schallschutzhaube", "Kosten_Abgasreinigung", "Vorlauf",
                    "Ruecklauf", "Wirkungsgrad_el", "Wirkungsgrad_th", "Wirkungsgrad_el_Teillast50",
                    "Wirkungsgrad_th_Teillast50", "Anfahrverlust_kWh", "Mindestlaufzeit_min",
                    // UebergabegrenzeSchema (Schritt 205): die Ruecklaufgrenze; leer traegt sie nichts zur Pruefsumme bei.
                    "Ruecklauf_Max"
                })
            { Anzeigeschluessel = "KABG_KATALOG_BHKW" },
            new Katalogtabelle("Tab_PV_STAMM", "PV",
                new[]
                {
                    "Bezeichner", "Firma", "Beschreibung", "Leistung", "Wirkungsgrad", "U_Mpp", "U_Leerlauf",
                    "I_Mpp", "I_Kurzschluss", "alpha_SC", "beta_OC", "gamma_PMP", "T_NOCT", "Laenge", "Breite",
                    "Modulkosten", "Technologie"
                })
            { Anzeigeschluessel = "KABG_KATALOG_PV" },
            new Katalogtabelle("Tab_Brauchwasser_STAMM", "BW",
                Verbinden(new[] { "Bezeichner", "Typ", "Beschreibung" }, Monate12()))
            { Anzeigeschluessel = "KABG_KATALOG_BW" },
            new Katalogtabelle("Tab_Brauchwassertyp_STAMM", "BWT",
                Verbinden(new[] { "Bezeichner", "Beschreibung" }, Stunden168()))
            { Anzeigeschluessel = "KABG_KATALOG_BWT" },
            new Katalogtabelle("Tab_Prozesswaerme_STAMM", "PW",
                Verbinden(new[] { "Bezeichner", "Typ", "Beschreibung" }, Monate12(), new[] { "Vorlauf", "Ruecklauf" }))
            { Anzeigeschluessel = "KABG_KATALOG_PW" },
            new Katalogtabelle("Tab_Prozesstyp_STAMM", "PWT",
                Verbinden(new[] { "Bezeichner", "Beschreibung" }, Stunden168()))
            { Anzeigeschluessel = "KABG_KATALOG_PWT" },
        };

        /// <summary>
        /// Die Kinder einer Konditionierung (Vorgaben und Kalender samt Perioden), die an einem Eigentümer
        /// <paramref name="fremdschluessel"/> hängen. Die Tabellen teilen sich Katalogbau, Vorlage und
        /// Projektgebäude; die übrigen Eigentümerspalten bleiben leer.
        /// </summary>
        private static Katalogkind[] Konditionierung(string fremdschluessel, string andererEigentuemer)
        {
            // ID_Zone_Stamm (Schritt ZK): die Zeilen einer Katalogzone gehören nicht zur Ebene des Katalogbaus.
            string[] neben = { "ID_Gebaeude", "ID_Zone", andererEigentuemer, ZonenKatalogSchema.SPALTE_ID_ZONE_STAMM };
            var periode = new Katalogkind("Tab_Konditionierungsperiode", "ID_Kalender",
                new[] { "Rang", "Art", "Bezeichner", "Beginn", "Ende", "Feiertagsregel", "Wert", "Aus", "Woche", "WieWochentag", "Gilt_Fuer" })
            {
                // Schritt K2: der Verweis auf eine benannte Woche ist ein Schluessel des Ziels, kein Fachwert.
                Nebenspalten = new[] { KalenderbedienungSchema.SPALTE_ID_WOCHE },
            };
            return new[]
            {
                new Katalogkind("Tab_Konditionierungsvorgabe", fremdschluessel,
                    new[] { "Groesse", "Zeile", "Wert", "Aus", "Von", "Bis", "Bedingt_K" })
                { Nebenspalten = neben, Leerspalte = ZonenKatalogSchema.SPALTE_ID_ZONE_STAMM },
                // Nutzung (Schemaschritt 176) ist die Kopie der Vorlagen-Nutzung am Projektkalender:
                // keine Fachspalte des Katalogs (die Nutzung steht am Vorlagenkopf), nur Nebenspalte.
                new Katalogkind("Tab_Konditionierungskalender", fremdschluessel,
                    new[] { "Groesse", "Wert", "Aus", "Woche", "Nennwert", "Bemerkung" })
                {
                    Nebenspalten = new[] { "ID_Gebaeude", "ID_Zone", andererEigentuemer, ZonenKatalogSchema.SPALTE_ID_ZONE_STAMM, "Nutzung" },
                    Enkel = new[] { periode }, Leerspalte = ZonenKatalogSchema.SPALTE_ID_ZONE_STAMM,
                },
            };
        }

        /// <summary>Eine Reihe (Ganglinie, Verteilung) als Kind: eine Wertspalte in fester Folge.</summary>
        private static Katalogkind Reihe(string tabelle, string fremdschluessel, string wertspalte) =>
            new Katalogkind(tabelle, fremdschluessel, new[] { wertspalte }) { Geordnet = true };

        private static readonly Katalogtabelle[] STUFE2 =
        {
            new Katalogtabelle("Tab_Baustoff_STAMM", "BST",
                new[] { "Bezeichner", "Gruppe", "Hersteller", "Lambda", "Rho", "cp", "Quelle", "Herkunft", "Quellkennung" },
                new Katalogkind("Tab_Baustoffsynonym_STAMM", "ID_Baustoff", new[] { "Materialname", "Sprache", "Quelle" })
                { GesperrtEinfuegen = true })
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_BAUSTOFF" },
            new Katalogtabelle("Tab_Bauteilaufbau_STAMM", "BTA",
                new[] { "Bezeichner", "Beschreibung", "Bauteilart", "Quelle", "Herkunft", "Quellkennung", "Typaufbau" },
                new Katalogkind("Tab_Bauteilschicht_STAMM", "ID_Aufbau",
                    new[] { "Reihenfolge", "ID_Baustoff", "Dicke", "IstLuftschicht", "Lambda", "Rho", "cp" })
                { Verweise = new[] { new Katalogverweis("ID_Baustoff", "Tab_Baustoff_STAMM", SPALTE_SCHLUESSEL) } })
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_BAUTEILAUFBAU" },
            new Katalogtabelle("Tab_Brennstoff_Stamm", "BRS",
                new[]
                {
                    "ID_Kategorie", "Bezeichner", "Einheit", "PreisEinheit", "Hi", "Hs", "CO2", "SO2", "NOx", "Staub",
                    "PE_Faktor", "Standard_Grundpreis", "Standard_Arbeitspreis", "Standard_Leistungspreis"
                })
            {
                Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_BRENNSTOFF",
                Verweise = new[] { new Katalogverweis("ID_Kategorie", "Tab_BrennstoffKategorien", "Gruppe") },
            },
            new Katalogtabelle("Tab_DBTagV_STAMM", "TAGV",
                new[] { "Bezeichner", "Beschreibung", "Veraenderbar" },
                Reihe("Tab_DBTagVDaten_STAMM", "ID_TagV", "Verteilung"))
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_TAGESVERTEILUNG" },
            new Katalogtabelle("Tab_Gebaeude_STAMM", "GEB",
                new[]
                {
                    "Bezeichner", "Typ", "Beschreibung", "Wohnflaeche_gesamt", "Bewohner", "Flaeche_Nutzer",
                    "Interne_Waermegewinne", "Bauweise", "Fensterflaeche_Sued", "Fensterflaeche_Ost_West",
                    "Fensterflaeche_Nord", "Fensterdurchlassgrad", "Raumsolltemperatur_Nachtabsenkung",
                    "Raumsolltemperatur_Tag", "Raumsolltemperatur_Wochenende", "Raumsolltemperatur_Ferien",
                    "Maximaleraumtemperatur", "k_Wert_Außenwand", "k_Wert_Fenster", "k_Wert_Dachflaeche",
                    "k_Wert_Grundflaeche", "k_Wert_Sonstiges", "Flaeche_Außenwand", "gesamte_Fensterflaeche",
                    "Dachflaeche", "Grundflaeche", "Sonstige_Flaechen", "Nutzflaeche", "Raumhoehe",
                    "WBVK_Anschluß_Fenster_Wand", "WBVK_Anschluß_Wand_Dach", "WBVK_Anschluß_Außenwand_Kellerdecke",
                    "Abmessung_Anschluß_Fenster_Wand", "Abmessung_Anschluß_Wand_Dach",
                    "Abmessung_Anschluß_Außenwand_Kellerdecke", "Luftwechselrate", "Wochenende", "Ferien",
                    "Ferienbeginn_1", "Ferienende_1", "Ferienbeginn_2", "Ferienende_2", "Ferienbeginn_3",
                    "Ferienende_3", "Ferienbeginn_4", "Ferienende_4", "WW_Bedarf", "spez_Waermeverbrauch",
                    "Waermebedarf", "Baualtersklasse", "Gebaeudeart", "Wohngebaeude_Nicht_Wohngebaeude",
                    "Gebaeude_Modell", "Fensterflaeche_Ost", "Fensterflaeche_West", "Rahmenanteil",
                    "Verschattungsfaktor", "Grundflaeche_Randbedingung", "Kellertemperatur", "Masseanteil_Aussen",
                    "Innenflaechenfaktor", "Heizung_Strahlungsanteil", "Heizleistung_Max",
                    "Aussenbauteile_Strahlung", "Luftwechsel_Infiltration", "Luftwechsel_Nutzer", "Sommerlueftung",
                    "Kuehl_Sollwert", "Kuehlleistung_Max", "Kuehlung_Aktiv", "Kuehl_Sollwert_Nacht",
                    "Heizkreis_Aktiv", "Uebergabe_Art", "Uebergabe_Exponent", "Uebergabe_Leistung_Nenn",
                    "Auslegung_Vorlauf", "Auslegung_Ruecklauf", "Auslegung_Raumtemperatur",
                    "Auslegung_Aussentemperatur", "Heizkurve_Aktiv", "Heizkurve_Niveau", "Heizkurve_Steilheit",
                    "Regler_Proportionalband", "Sollwertprofil", "Kuehluebergabe_Aktiv", "Kuehl_Uebergabe_Art",
                    "Kuehl_Uebergabe_Exponent", "Kuehl_Uebergabe_Leistung_Nenn", "Kuehl_Auslegung_Vorlauf",
                    "Kuehl_Auslegung_Ruecklauf", "Kuehl_Auslegung_Raumtemperatur", "Kuehl_Vorlaufgrenze",
                    "Baujahr", "Nachtabsenkung_Beginn", "Nachtabsenkung_Ende", "Energiestandard", "Erdreich_U_Wirksam",
                    "Heizkurve_Raumeinfluss", "Kuehlkurve_Aktiv", "Kuehlkurve_Fusspunkt", "Kuehlkurve_Raumeinfluss",
                    "Kuehlkurve_Auslegung_Weg", "Kuehlkurve_Auslegung_Aussen", "Wochenendtage", "Feiertagsland"
                },
                Konditionierung("ID_Gebaeude_Stamm", "ID_Vorlage"))
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_GEBAEUDE" },
            new Katalogtabelle("Tab_Konditionierungsvorlage_STAMM", "KV",
                new[] { "Groesse", "Bezeichner", "Beschreibung", "Nutzung" },
                Konditionierung("ID_Vorlage", "ID_Gebaeude_Stamm"))
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_KONDITIONIERUNGSVORLAGE", Namensspalten = new[] { "Groesse", "Bezeichner" } },
            new Katalogtabelle("Tab_Pufferspeicher_STAMM", "PS",
                new[] { "Bezeichner", "Hersteller", "Speichertyp", "Bereitschaftsverluste", "Gesamtvolumen", "Investitionskosten" })
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_PUFFER" },
            new Katalogtabelle("Tab_PufferAuslegungParameter_STAMM", "PAP",
                new[] { "Schluessel", "Wert", "Einheit", "Quelle", "Herkunftsart" })
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_PUFFERVORGABE", Namensspalten = new[] { "Schluessel" } },
            new Katalogtabelle("Tab_Solarkollektoren_STAMM", "SK",
                new[]
                {
                    "Bezeichner", "Firma", "Beschreibung", "Kollektortyp", "Modulflaeche", "Aperturflaeche", "h0",
                    "k1", "k2", "Kdir", "Kdfu", "Investitionskosten", "Bezugsflaeche"
                })
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_SOLARKOLLEKTOR" },
            new Katalogtabelle("Tab_Solarganglinie_STAMM", "SOLGL",
                new[] { "Bezeichner", "Beschreibung" },
                Reihe("Tab_SolarganglinieDaten_STAMM", "ID_Ganglinie", "Wert"))
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_SOLARGANGLINIE" },
            new Katalogtabelle("Tab_PvGanglinie_STAMM", "PVG",
                new[] { "Bezeichner", "Beschreibung", "Raster_Minuten", "Nennleistung_kWp", "Jahresarbeit_kWh", "Spitze_kW" },
                Reihe("Tab_PvGanglinieDaten_STAMM", "ID_Ganglinie", "Wert"))
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_PVGANGLINIE" },
            new Katalogtabelle("Tab_Stromspeicher_STAMM", "SSP",
                new[]
                {
                    "Bezeichner", "Typ", "Leistung", "Energie", "Degradation", "Ladezustand", "Modulkosten",
                    "Wirkungsgrad_RT", "Zyklen_Zugesichert", "Verschleisskosten", "Leistungskosten",
                    "Investition_Fix", "Standby_Verbrauch", "Firma", "Selbstentladung_Prozent_Monat"
                })
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_STROMSPEICHER" },
            new Katalogtabelle("Tab_Stromverbrauchertyp_STAMM", "SVT",
                Verbinden(new[] { "Typname", "Beschreibung" }, Stunden168()))
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_STROMVERBRAUCHERTYP", Namensspalten = new[] { "Typname" } },
            new Katalogtabelle("Tab_Stromverbraucher_STAMM", "SV",
                Verbinden(new[] { "Bezeichner", "Typ", "Beschreibung" }, Monate12()))
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_STROMVERBRAUCHER" },
            new Katalogtabelle("Tab_Stromganglinie_STAMM", "STRGL",
                new[] { "Bezeichner", "Zeitinterval" },
                Reihe("Tab_StromganglinieDaten_STAMM", "ID_Ganglinie", "Wert"))
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_STROMGANGLINIE" },
            new Katalogtabelle("Tab_Waermebedarf_STAMM", "WBGL",
                new[] { "Bezeichner" },
                Reihe("Tab_WaermebedarfDaten_STAMM", "ID_Ganglinie", "Wert"))
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_WAERMEBEDARF" },
            new Katalogtabelle("Tab_Wechselrichter_STAMM", "WR",
                new[]
                {
                    "Bezeichner", "Firma", "Beschreibung", "P_AC_Nenn", "S_AC_Max", "P_DC_Max", "U_Mpp_Min",
                    "U_Mpp_Max", "U_Dc_Max", "U_Start", "I_Dc_Max", "Anzahl_Mppt", "Straenge_Je_Mppt", "Eta05",
                    "Eta10", "Eta20", "Eta30", "Eta50", "Eta100", "Eta_Euro", "Eta_Max", "P_Standby", "P_Nacht",
                    "Kosten", "Sandia_Pdco", "Sandia_Vdco", "Sandia_Pso", "Sandia_C0", "Sandia_C1", "Sandia_C2",
                    "Sandia_C3", "Herkunft", "I_Sc_Max"
                })
            { Stufe = 2, Anzeigeschluessel = "KABG_KATALOG_WECHSELRICHTER" },
        };

        // Stufe 3: Kataloge, die NACH den Schritten der Katalogfassung entstehen. Sie stehen nicht in
        // Stufe 1 oder 2, weil deren Schritte ihre Tabellen sonst als „unvollständig“ fänden, solange
        // eine ältere Datenbank den späteren Schritt noch nicht gelaufen hat; ihr eigener Schemaschritt
        // legt Katalogspalten und Schlüssel an (KaeltemaschineSchema, KU3-1).
        private static readonly Katalogtabelle[] STUFE3 =
        {
            new Katalogtabelle(KaeltemaschineSchema.TAB_STAMM, "KM",
                KaeltemaschineSchema.Fachspalten,
                new Katalogkind(KaeltemaschineSchema.TAB_KENNDATEN_STAMM, KaeltemaschineSchema.SPALTE_ID_KAELTEMASCHINE,
                                KaeltemaschineSchema.KennlinienSpalten))
            { Stufe = 3, Anzeigeschluessel = "KABG_KATALOG_KAELTEMASCHINE" },
        };

        private static readonly Katalogtabelle[] ALLE = STUFE1.Concat(STUFE2).Concat(STUFE3).ToArray();

        /// <summary>Die Katalogtabellen der Stufe 1 in fester Folge.</summary>
        public static IReadOnlyList<Katalogtabelle> Stufe1 => STUFE1;

        /// <summary>Die Katalogtabellen der Stufe 2 in fester Folge (Verweisziele vor ihren Verweisern).</summary>
        public static IReadOnlyList<Katalogtabelle> Stufe2 => STUFE2;

        /// <summary>Die Katalogtabellen der Stufe 3 — eigener Schemaschritt nach der Katalogfassung.</summary>
        public static IReadOnlyList<Katalogtabelle> Stufe3 => STUFE3;

        /// <summary>Das ganze Register: Stufe 1, dann Stufe 2, dann Stufe 3.</summary>
        public static IReadOnlyList<Katalogtabelle> Alle => ALLE;

        /// <summary>
        /// <b>Die benannt ausgenommenen Katalogtabellen</b> mit Grund — jede <c>_STAMM</c>-Tabelle steht
        /// entweder im Register (als Kopf, Kind oder Enkel) oder hier (Wache in den Tests).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> Ausgenommen = AusnahmenBauen();

        /// <summary>Grund der Ausnahme des Klimakatalogs.</summary>
        public const string GRUND_KLIMA =
            "Klimakatalog: Stunden- und Tagesreihen je Region (Tab_Solar_STAMM 8760 Stunden x 13 Spalten) - das " +
            "Paket wuerde dreistellige Megabyte gross; der Pflegeweg ist der Klimaimport (DWD-TRY) mit Quelle, " +
            "Importdatum, Szenario und Bezugsjahr; Primaerschluessel ID_Klimaregion statt ID.";

        /// <summary>Grund der Ausnahme des Zapfprofilkatalogs.</summary>
        public const string GRUND_NUTZUNGSPROFIL =
            "Nutzungsprofile der Pufferauslegung: eine feste Aufzaehlung (Kennungen im Code), die der Schemaschritt " +
            "ProzessNutzungSchema anlegt; ohne Katalogfassung und ohne Paketweg - es gibt nichts abzugleichen.";

        public const string GRUND_ZAPFPROFIL =
            "Zapfprofilkatalog: fuehrt eine eigene Katalogversion und einen eigenen Paketweg (TwwPaketteilCtrl, " +
            "Katalogimport mit Konfliktregeln und Provenienz je Zeile); seine Tabellen verweisen ueber IDs " +
            "aufeinander (ID_Tagesgangsatz, ID_Vorlage, ID_Nutzungsart, ID_Bedarfstag). Ein zweiter Weg " +
            "daneben ergaebe zwei Wahrheiten ueber denselben Stand.";

        /// <summary>Grund der Ausnahme der Katalogzonen (Schritt ZK).</summary>
        public const string GRUND_KATALOGZONEN =
            "Zonen im Gebaeudekatalog (Schritt ZK): Anwenderdaten am Katalogsatz, die gesperrten Auslieferungssaetze tragen " +
            "keine; Bauteile und Luftstroeme verweisen ueber IDs auf Geschwisterzonen desselben Satzes (ID_Nachbarzone, " +
            "ID_ZoneA/ID_ZoneB), die der Paketweg (Verweis auf einen Katalogschluessel) nicht abbildet. Pruefsumme und " +
            "Paket des Gebaeudes bleiben ohne Zonen; die Konditionierung der Katalogzonen faellt ueber die Leerspalte " +
            "ID_Zone_Stamm aus seinen Kindern.";

        private static IReadOnlyDictionary<string, string> AusnahmenBauen()
        {
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string t in new[] { "Tab_Klimaregion_STAMM", "Tab_Klimadaten_STAMM", "Tab_Solar_STAMM" })
                d[t] = GRUND_KLIMA;
            foreach (string t in new[]
                     {
                         "Tab_TwwNutzungsart_STAMM", "Tab_TwwZapfkategorie_STAMM", "Tab_TwwTagesgangsatz_STAMM",
                         "Tab_TwwTagesgang_STAMM", "Tab_TwwBedarfstag_STAMM", "Tab_TwwBedarfstagEreignis_STAMM",
                         "Tab_TwwDin4708Wert_STAMM", "Tab_TwwParameter_STAMM"
                     })
                d[t] = GRUND_ZAPFPROFIL;
            d[ProzessNutzungSchema.TAB_PROFIL] = GRUND_NUTZUNGSPROFIL;
            foreach (string t in new[] { ZonenKatalogSchema.TAB_ZONE, ZonenKatalogSchema.TAB_BAUTEIL, ZonenKatalogSchema.TAB_LUFTSTROM })
                d[t] = GRUND_KATALOGZONEN;
            return d;
        }

        /// <summary>Die Tabelle des Registers mit diesem Namen oder <c>null</c>.</summary>
        public static Katalogtabelle Tabelle(string name) =>
            ALLE.FirstOrDefault(t => string.Equals(t.Tabelle, name, StringComparison.OrdinalIgnoreCase));

        /// <summary>Der Katalog in Worten (Dialog, Bericht); der Tabellenname, wenn er nicht im Register steht.</summary>
        public static string Anzeigename(string tabelle)
        {
            Katalogtabelle t = Tabelle(tabelle);
            if (t == null || string.IsNullOrEmpty(t.Anzeigeschluessel)) return tabelle ?? "";
            string text = MyResource.Resource.ResourceManager.GetString(t.Anzeigeschluessel, MyResource.Resource.Culture);
            return string.IsNullOrEmpty(text) ? t.Tabelle : text;
        }

        /// <summary>Alle Kind- und Enkeltabellen einer Katalogtabelle (Tiefe zuerst).</summary>
        public static IEnumerable<Katalogkind> KinderUndEnkel(Katalogtabelle t)
        {
            foreach (Katalogkind k in t.Kinder)
                foreach (Katalogkind x in MitEnkeln(k)) yield return x;
        }

        private static IEnumerable<Katalogkind> MitEnkeln(Katalogkind k)
        {
            yield return k;
            foreach (Katalogkind e in k.Enkel)
                foreach (Katalogkind x in MitEnkeln(e)) yield return x;
        }

        // =================================================================================
        // Name
        // =================================================================================

        /// <summary>Der Name eines Satzes aus seinen Namensspalten (mehrere mit „ / " verbunden).</summary>
        public static string Name(Katalogtabelle t, IReadOnlyDictionary<string, object> werte) =>
            string.Join(NAMENSTRENNER, t.Namensspalten.Select(s =>
                werte != null && werte.TryGetValue(s, out object w) && w != null && w != DBNull.Value
                    ? Convert.ToString(w, CultureInfo.InvariantCulture) : ""));

        /// <summary>Der Name eines gelesenen Satzes.</summary>
        internal static string Name(Katalogtabelle t, DataRow r) =>
            string.Join(NAMENSTRENNER, t.Namensspalten.Select(s =>
                r.Table.Columns.Contains(s) && r[s] != DBNull.Value ? Convert.ToString(r[s], CultureInfo.InvariantCulture) : ""));

        // =================================================================================
        // Prüfsumme
        // =================================================================================

        /// <summary>
        /// Ein Wert in seiner Prüfsummenform: Zahlen als „n:" mit invarianter, rundlauffester
        /// Darstellung (ganzzahlige Gleitkommazahlen wie Ganzzahlen, Wahrheitswerte als 1/0), Text
        /// als „s:" unverändert. <c>null</c> = leer (trägt nichts bei).
        /// </summary>
        public static string Normiert(object wert)
        {
            if (wert == null || wert == DBNull.Value) return null;
            switch (wert)
            {
                case string s: return "s:" + s;
                case bool b: return "n:" + (b ? "1" : "0");
                case byte or sbyte or short or ushort or int or uint or long:
                    return "n:" + Convert.ToInt64(wert, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
                case ulong u: return "n:" + u.ToString(CultureInfo.InvariantCulture);
                case float f: return Zahl(f);
                case double d: return Zahl(d);
                case decimal m: return Zahl((double)m);
                case DateTime t: return "s:" + t.ToString("o", CultureInfo.InvariantCulture);
                case byte[] bytes: return "b:" + Convert.ToBase64String(bytes);
                default: return "s:" + Convert.ToString(wert, CultureInfo.InvariantCulture);
            }
        }

        private static string Zahl(double d)
        {
            if (!double.IsNaN(d) && !double.IsInfinity(d) && Math.Floor(d) == d && Math.Abs(d) < 9.0e15)
                return "n:" + ((long)d).ToString(CultureInfo.InvariantCulture);
            return "n:" + d.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>Die Zeile „Name=Wert" je belegter Spalte, in der Folge von <paramref name="spalten"/>.</summary>
        private static void Zeilen(StringBuilder sb, IEnumerable<string> spalten, IReadOnlyDictionary<string, object> werte)
        {
            foreach (string s in spalten)
            {
                if (werte == null || !werte.TryGetValue(s, out object w)) continue;
                string n = Normiert(w);
                if (n == null) continue;
                sb.Append(s).Append('=').Append(n.Replace("\\", "\\\\").Replace("\n", "\\n").Replace("\r", "\\r")).Append('\n');
            }
        }

        /// <summary>Die Enkelzeilen einer Kindzeile (unter dem Namen der Enkeltabelle abgelegt).</summary>
        internal static IReadOnlyList<IReadOnlyDictionary<string, object>> Enkelzeilen(
            IReadOnlyDictionary<string, object> kindzeile, string enkeltabelle)
        {
            var liste = new List<IReadOnlyDictionary<string, object>>();
            if (kindzeile == null || !kindzeile.TryGetValue(enkeltabelle, out object o) || o == null || o is string) return liste;
            if (o is IEnumerable e)
                foreach (object z in e)
                    if (z is IReadOnlyDictionary<string, object> d) liste.Add(d);
            return liste;
        }

        /// <summary>Eine Kindzeile als eine Textzeile (Spalten mit „;" getrennt, Enkel in geschweiften Klammern).</summary>
        private static string Kindzeile(Katalogkind k, IReadOnlyDictionary<string, object> werte)
        {
            var sb = new StringBuilder();
            Zeilen(sb, k.Fachspalten, werte);
            string text = sb.ToString().Replace("\n", ";");
            foreach (Katalogkind e in k.Enkel)
                text += "{#" + e.Tabelle + ":" + string.Join("|", Kindtexte(e, Enkelzeilen(werte, e.Tabelle))) + "}";
            return text;
        }

        /// <summary>Die Textzeilen der Kindzeilen: sortiert, bei einer Reihe in ihrer Folge mit Position.</summary>
        internal static List<string> Kindtexte(Katalogkind k, IEnumerable<IReadOnlyDictionary<string, object>> zeilen)
        {
            var texte = (zeilen ?? Enumerable.Empty<IReadOnlyDictionary<string, object>>()).Select(z => Kindzeile(k, z)).ToList();
            if (k.Geordnet)
            {
                for (int i = 0; i < texte.Count; i++)
                    texte[i] = "@" + i.ToString(CultureInfo.InvariantCulture) + ":" + texte[i];
            }
            else texte.Sort(StringComparer.Ordinal);
            return texte;
        }

        /// <summary>
        /// <b>Die Prüfsumme eines Satzes</b> — SHA-256 (Hex, klein) über die Fachwerte in der festen
        /// Spaltenfolge und die Kindzeilen (sortiert oder, bei einer Reihe, in ihrer Folge). Unabhängig
        /// von der Reihenfolge, in der <paramref name="werte"/> geliefert werden.
        /// </summary>
        /// <param name="t">Die Katalogtabelle.</param>
        /// <param name="werte">Spaltenname → Wert (Groß-/Kleinschreibung wie im Schema); Verweise als Name des Ziels.</param>
        /// <param name="kinder">Kindtabelle → Zeilen; fehlt eine Kindtabelle, zählt sie als leer.</param>
        public static string Pruefsumme(Katalogtabelle t, IReadOnlyDictionary<string, object> werte,
                                        IReadOnlyDictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>> kinder = null)
        {
            if (t == null) throw new ArgumentNullException(nameof(t));
            var sb = new StringBuilder();
            sb.Append("#").Append(t.Tabelle).Append('\n');
            Zeilen(sb, t.Fachspalten, werte);
            foreach (Katalogkind k in t.Kinder)
            {
                sb.Append("#").Append(k.Tabelle).Append('\n');
                IReadOnlyList<IReadOnlyDictionary<string, object>> zeilen = null;
                kinder?.TryGetValue(k.Tabelle, out zeilen);
                foreach (string z in Kindtexte(k, zeilen)) sb.Append(z).Append('\n');
            }
            return Hex(sb.ToString());
        }

        private static string Hex(string text)
        {
            using (SHA256 sha = SHA256.Create())
            {
                byte[] h = sha.ComputeHash(new UTF8Encoding(false).GetBytes(text));
                var sb = new StringBuilder(64);
                foreach (byte b in h) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        // =================================================================================
        // Schlüssel
        // =================================================================================

        /// <summary>
        /// Der Schlüsselstamm aus Kürzel und bereinigtem Namen: Umlaute ausgeschrieben, alles
        /// außer Buchstaben A–Z und Ziffern als „_", Folgen zusammengezogen, groß geschrieben, höchstens
        /// <see cref="SCHLUESSEL_MAX"/> − 8 Zeichen (Platz für den Zähler).
        /// </summary>
        public static string Schluesselstamm(Katalogtabelle t, string bezeichner)
        {
            string b = (bezeichner ?? "").Trim()
                .Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue")
                .Replace("Ä", "Ae").Replace("Ö", "Oe").Replace("Ü", "Ue").Replace("ß", "ss")
                .ToUpperInvariant();
            var sb = new StringBuilder();
            bool strich = false;
            foreach (char c in b)
            {
                bool gut = (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
                if (gut) { sb.Append(c); strich = false; }
                else if (!strich && sb.Length > 0) { sb.Append('_'); strich = true; }
            }
            string rumpf = sb.ToString().TrimEnd('_');
            if (rumpf.Length == 0) rumpf = "SATZ";
            string stamm = t.Kuerzel + ":" + rumpf;
            int max = SCHLUESSEL_MAX - 8;
            return stamm.Length > max ? stamm.Substring(0, max).TrimEnd('_') : stamm;
        }

        /// <summary>
        /// Der Schlüssel eines Satzes: der Stamm, ist er belegt, mit Zähler „_2", „_3" … — der erste
        /// freie nach <paramref name="belegt"/>.
        /// </summary>
        public static string Schluessel(Katalogtabelle t, string bezeichner, Func<string, bool> belegt)
        {
            string stamm = Schluesselstamm(t, bezeichner);
            if (belegt == null || !belegt(stamm)) return stamm;
            for (int i = 2; ; i++)
            {
                string k = stamm + "_" + i.ToString(CultureInfo.InvariantCulture);
                if (!belegt(k)) return k;
            }
        }

        // =================================================================================
        // Lesen aus der Datenbank
        // =================================================================================

        /// <summary>Sind die drei Katalogspalten an <paramref name="tabelle"/> angelegt?</summary>
        public static bool SpaltenVorhanden(string tabelle) =>
            DataRepository.SpalteVorhanden(tabelle, SPALTE_SCHLUESSEL) &&
            DataRepository.SpalteVorhanden(tabelle, SPALTE_PRUEFSUMME) &&
            DataRepository.SpalteVorhanden(tabelle, SPALTE_AUSGELAUFEN);

        /// <summary>Die Fachspalten von <paramref name="t"/>, die die geöffnete Datenbank führt.</summary>
        internal static List<string> VorhandeneFachspalten(Katalogtabelle t) =>
            t.Fachspalten.Where(s => DataRepository.SpalteVorhanden(t.Tabelle, s)).ToList();

        /// <summary>Die Fachwerte einer gelesenen Zeile (roh, Verweise als ID).</summary>
        internal static Dictionary<string, object> Werte(IEnumerable<string> spalten, DataRow r)
        {
            var d = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (string s in spalten)
            {
                if (!r.Table.Columns.Contains(s)) continue;
                object w = r[s];
                d[s] = w == DBNull.Value ? null : w;
            }
            return d;
        }

        /// <summary>Die Fachwerte eines gelesenen Kopfsatzes in der Form von Prüfsumme und Paket (Verweise als Name).</summary>
        internal static Dictionary<string, object> Fachwerte(Katalogtabelle t, IEnumerable<string> spalten, DataRow r,
                                                             Func<string, DbParam[], DataTable> lese) =>
            Uebersetzen(t.Verweise, Werte(spalten, r), lese);

        /// <summary>Ersetzt die IDs der Verweise durch den Namen des Ziels (leer, wenn das Ziel keinen hat).</summary>
        private static Dictionary<string, object> Uebersetzen(IReadOnlyList<Katalogverweis> verweise,
                                                              Dictionary<string, object> d,
                                                              Func<string, DbParam[], DataTable> lese)
        {
            foreach (Katalogverweis v in verweise)
            {
                if (!d.TryGetValue(v.Spalte, out object w) || w == null) continue;
                object name = null;
                if (DataRepository.TabelleVorhanden(v.Zieltabelle) && DataRepository.SpalteVorhanden(v.Zieltabelle, v.Zielspalte))
                {
                    DataTable dt = lese("SELECT \"" + v.Zielspalte + "\" FROM \"" + v.Zieltabelle + "\" WHERE ID = ?",
                                        new[] { new DbParam("@id", w) });
                    if (dt != null && dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
                        name = Convert.ToString(dt.Rows[0][0], CultureInfo.InvariantCulture);
                }
                d[v.Spalte] = name;
            }
            return d;
        }

        /// <summary>
        /// Der Schreibwert einer Spalte: bei einem Verweis die ID des Ziels mit diesem Namen (leer, wenn
        /// es das Ziel nicht gibt), sonst der Wert.
        /// </summary>
        internal static object Schreibwert(IReadOnlyList<Katalogverweis> verweise, IReadOnlyDictionary<string, object> werte,
                                           string spalte, Func<string, DbParam[], DataTable> lese)
        {
            object w = werte != null && werte.TryGetValue(spalte, out object x) && x != null ? x : null;
            Katalogverweis v = verweise.FirstOrDefault(z => string.Equals(z.Spalte, spalte, StringComparison.OrdinalIgnoreCase));
            if (v == null || w == null) return w ?? DBNull.Value;
            if (!DataRepository.TabelleVorhanden(v.Zieltabelle) || !DataRepository.SpalteVorhanden(v.Zieltabelle, v.Zielspalte))
                return DBNull.Value;
            DataTable dt = lese("SELECT ID FROM \"" + v.Zieltabelle + "\" WHERE \"" + v.Zielspalte + "\" = ? ORDER BY ID LIMIT 1",
                                new[] { new DbParam("@n", w) });
            return dt != null && dt.Rows.Count > 0 ? dt.Rows[0][0] : DBNull.Value;
        }

        /// <summary>Das SELECT der Spalten (Namen in Anführungszeichen, auch mit Umlaut).</summary>
        internal static string Spaltentext(IEnumerable<string> spalten) =>
            string.Join(", ", spalten.Select(s => "\"" + s + "\""));

        /// <summary>Die Fachspalten eines Kindes, die die Datenbank führt.</summary>
        internal static List<string> VorhandeneFachspalten(Katalogkind k) =>
            k.Fachspalten.Where(s => DataRepository.SpalteVorhanden(k.Tabelle, s)).ToList();

        /// <summary>
        /// Die Kindzeilen eines Satzes je Kindtabelle — nur Katalogzeilen, nie Projektkopien. Eine
        /// Kindtabelle, die die Datenbank nicht führt, fehlt im Ergebnis.
        /// </summary>
        internal static Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>> Kinder(
            Katalogtabelle t, long id, Func<string, DbParam[], DataTable> lese)
        {
            var d = new Dictionary<string, IReadOnlyList<IReadOnlyDictionary<string, object>>>(StringComparer.Ordinal);
            foreach (Katalogkind k in t.Kinder)
            {
                List<IReadOnlyDictionary<string, object>> zeilen = Kindzeilen(k, id, lese);
                if (zeilen != null) d[k.Tabelle] = zeilen;
            }
            return d;
        }

        /// <summary>Die Zeilen eines Kindes zu einer Eigentümer-ID (samt Enkeln); <c>null</c>, wenn die Tabelle fehlt.</summary>
        private static List<IReadOnlyDictionary<string, object>> Kindzeilen(Katalogkind k, long id,
                                                                             Func<string, DbParam[], DataTable> lese)
        {
            if (!DataRepository.TabelleVorhanden(k.Tabelle)) return null;
            List<string> spalten = VorhandeneFachspalten(k);
            if (spalten.Count == 0) return null;
            DataTable dt = lese("SELECT ID, " + Spaltentext(spalten) + " FROM \"" + k.Tabelle + "\" WHERE \"" +
                                k.Fremdschluessel + "\" = ?" + k.Katalogbedingung + " ORDER BY ID",
                                new[] { new DbParam("@id", id) });
            var zeilen = new List<IReadOnlyDictionary<string, object>>();
            if (dt == null) return zeilen;
            foreach (DataRow r in dt.Rows)
            {
                Dictionary<string, object> w = Uebersetzen(k.Verweise, Werte(spalten, r), lese);
                long kindId = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
                foreach (Katalogkind e in k.Enkel)
                {
                    List<IReadOnlyDictionary<string, object>> enkel = Kindzeilen(e, kindId, lese);
                    if (enkel != null) w[e.Tabelle] = enkel;
                }
                zeilen.Add(w);
            }
            return zeilen;
        }

        /// <summary>Die Prüfsumme eines gelesenen Kopfsatzes samt seiner Kindzeilen.</summary>
        internal static string PruefsummeDerZeile(Katalogtabelle t, List<string> spalten, DataRow r,
                                                  Func<string, DbParam[], DataTable> lese)
        {
            long id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture);
            return Pruefsumme(t, Fachwerte(t, spalten, r, lese), Kinder(t, id, lese));
        }

        /// <summary>Lesen über die Zugriffsschicht ohne Vorgang.</summary>
        internal static DataTable LeseOhneVorgang(string sql, DbParam[] p) => DataRepository.GetDataTable(sql, p);
    }
}
