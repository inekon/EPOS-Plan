using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Abweichungserkennung Variante vs. Stamm (Konzept Kap. 4, Baustein 4):
    /// dreistufig — Gewerk vorhanden/nicht vorhanden, andere Komponente,
    /// geänderte Auslegung/Betriebsparameter. Die Feldliste ist deklarativ:
    /// eine Zeile je Vergleichsmerkmal; neue Merkmale kosten genau eine Zeile.
    /// Dieselbe Liste speist die Kenndaten-Tabellen des Komponenten-Bausteins.
    /// </summary>
    public static class AbweichungsErmittler
    {
        /// <summary>Ein Vergleichsmerkmal (Spalte einer Eingabetabelle).</summary>
        public class Merkmal
        {
            public string Gewerk;      // Anzeigegruppe ("Anlage", "Wärmepumpe", …)
            public string Tabelle;     // Quelltabelle (projektbezogen, siehe ProjektDetails)
            public string Spalte;      // Spaltenname (tolerant — fehlt sie, wird übersprungen)
            public string Einheit;     // "" wenn keine
            public int Dez;            // Nachkommastellen (-1 = Text, -2 = Ja/Nein)

            private readonly string _labelSchluessel;   // "" = fester deutscher Text
            private readonly string _labelRueckfall;

            /// <summary>
            /// Der Anzeigename.
            ///
            /// <para><b>Warum eine Eigenschaft und kein Feld.</b> <see cref="Felder"/> ist
            /// eine <c>static readonly</c>-Liste: Ein Ressourcentext, den der Feldinitialisierer
            /// läse, wäre in der Sprache eingefroren, die beim ERSTEN Zugriff auf die Klasse
            /// galt — ein Sprachwechsel zur Laufzeit ginge an ihm vorbei (Kulturpinnung). Die
            /// Eigenschaft liest ihn bei jedem Zugriff, mit dem deutschen Wortlaut als
            /// Rückfall. Jedes Merkmal der Feldliste trägt einen Schlüssel; wer keinen angibt,
            /// bekommt den Text, den er hineingegeben hat. Verglichen und zugeordnet wird nie
            /// über diesen Namen, sondern über <see cref="Schluessel(Merkmal)"/>.</para>
            /// </summary>
            public string Label
            {
                get
                {
                    if (string.IsNullOrEmpty(_labelSchluessel)) return _labelRueckfall;
                    return Text(_labelSchluessel, _labelRueckfall);
                }
            }

            public Merkmal(string gewerk, string tabelle, string spalte, string label,
                           string einheit, int dez, string labelSchluessel = "")
            {
                Gewerk = gewerk; Tabelle = tabelle; Spalte = spalte;
                Einheit = einheit; Dez = dez;
                _labelRueckfall = label ?? "";
                _labelSchluessel = labelSchluessel ?? "";
            }
        }

        public const int TEXT = -1;
        public const int JN = -2;

        /// <summary>
        /// <b>Die sprachfreien Schlüssel der Stufe 1</b> (<see cref="Abweichung.Schluessel"/>): Wer eine Zeile zuordnet,
        /// vergleicht diesen Schlüssel, nie den Anzeigetext — der folgt der Sprache (<see cref="MerkmalBestand"/>,
        /// <see cref="MerkmalAnzahl"/>). Eine Merkmalszeile der Feldliste trägt „Tabelle.Spalte“ (<see cref="Schluessel"/>).
        /// </summary>
        public const string SCHLUESSEL_BESTAND = "BESTAND";

        /// <inheritdoc cref="SCHLUESSEL_BESTAND"/>
        public const string SCHLUESSEL_ANZAHL = "ANZAHL";

        /// <summary>
        /// Die Texte der Stufe 1 an EINER Stelle, nicht als Literale an zweien: Die Seite „Übersicht" zeigt dieselben
        /// Bestandszeilen auch OHNE Vergleichspartner (Gegenüberstellung Stamm ↔ Varianten). Zwei Schreibweisen derselben
        /// Kennzahl wären für den Leser zwei Kennzahlen. Bei jedem Zugriff in der Anzeigesprache gelesen — im Lauf eines
        /// Berichts in dessen Sprache (<see cref="BerichtTexte.ImLauf"/>).
        /// </summary>
        public static string MerkmalBestand => Text("BV_A1D_MERKMAL_BESTAND", "Bestand");

        /// <inheritdoc cref="MerkmalBestand"/>
        public static string MerkmalAnzahl => Text("BV_A1D_MERKMAL_ANZAHL", "Anzahl Komponenten");

        /// <inheritdoc cref="MerkmalBestand"/>
        public static string BestandVorhanden => Text("BV_A1D_BESTAND_VORHANDEN", "vorhanden");

        /// <inheritdoc cref="MerkmalBestand"/>
        public static string BestandFehlt => Text("BV_A1D_BESTAND_FEHLT", "nicht vorhanden");

        /// <summary>Der sprachfreie Schlüssel eines Merkmals der Feldliste: „Tabelle.Spalte“.</summary>
        public static string Schluessel(Merkmal f) => f == null ? "" : f.Tabelle + "." + f.Spalte;

        /// <summary>
        /// <b>Der Anzeigename eines Gewerks.</b> Die Gewerknamen der Feldliste und von
        /// <see cref="ProjektDetails.GewerkTabellen"/> sind SCHLÜSSEL (Zuordnung, Übernahme, Tests) und bleiben deutsch;
        /// gezeigt wird dieser Name in der Anzeigesprache. Ein unbekannter Name kommt unverändert zurück.
        /// </summary>
        public static string Gewerkname(string gewerk)
        {
            switch (gewerk)
            {
                case "Anlage": return Text("BV_A1D_GEWERK_ANLAGE", gewerk);
                case "Wärmepumpe": return Text("KONFIG_WAERMEPUMPE", gewerk);
                case "BHKW": return Text("KONFIG_BHKW", gewerk);
                case "Spitzenkessel": return Text("BV_A1D_GEWERK_SPITZENKESSEL", gewerk);
                case "Solarthermie": return Text("KONFIG_SOLARTHERMIE", gewerk);
                case "Photovoltaik": return Text("KONFIG_PHOTOVOLTAIK", gewerk);
                case "Pufferspeicher": return Text("BV_A1D_GEWERK_PUFFERSPEICHER", gewerk);
                case "Stromspeicher": return Text("KONFIG_STROMSPEICHER", gewerk);
                case "Gebäude": return Text("BV_A1D_GEWERK_GEBAEUDE", gewerk);
                default: return gewerk ?? "";
            }
        }

        /// <summary>Ein Ressourcentext in der Anzeigesprache (im Bericht: der Laufsprache); fehlt er, der deutsche Rückfall.</summary>
        internal static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel, MyResource.Resource.Culture); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        /// <summary>
        /// Deklarative Feldliste (Spaltennamen gegen Kenndaten.accdb verifiziert, 11.08.2026).
        /// </summary>
        public static readonly List<Merkmal> Felder = new List<Merkmal>
        {
            // Anlagenkonfiguration (Tab_Energieanlagen — Anker der Konfiguration)
            new Merkmal("Anlage", "Tab_Energieanlagen", "Betriebsart",        "Betriebsart", "", TEXT, "BV_A1D_MERKMAL_BETRIEBSART"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Vorlauf",            "Vorlauftemperatur", "°C", 0, "BV_A1D_MERKMAL_VORLAUFTEMP"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Rücklauf",           "Rücklauftemperatur", "°C", 0, "BV_A1D_MERKMAL_RUECKLAUFTEMP"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Bivalenter_Betrieb", "Bivalenter Betrieb", "", JN, "BV_A1D_MERKMAL_BIVALENT"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Abschaltpunkt",      "Abschaltpunkt", "°C", 1, "BV_A1D_MERKMAL_ABSCHALTPUNKT"),
            // 16.09.2026: Der Schalter heisst „Heizstab mitrechnen" und gehoert seit
            // Schemaschritt 79 der ANLAGE - der Lauf liest ihn je Waermepumpe
            // (SimulationWaermepumpe.ModuleAufbauen), die projektweite Einstellung
            // Tab_Einstellungen.WP_Heizstab ist entfallen. Das blosse „Heizstab" las
            // sich wie die LEISTUNG des Heizstabs (Tab_WP.Heizung, weiter unten);
            // die Spalte und damit der Rechenweg bleiben unberuehrt.
            new Merkmal("Anlage", "Tab_Energieanlagen", "Heizstab",           "Heizstab mitrechnen", "", JN,
                        "ABW_MERKMAL_HEIZSTAB"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Grenzleistung",      "Grenzleistung", "kW", 1, "BV_A1D_MERKMAL_GRENZLEISTUNG"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_Leistung",        "PV-Leistung", "kWp", 1, "BV_A1D_MERKMAL_PV_LEISTUNG"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Neigung",            "Neigung", "°", 0, "BV_A1D_MERKMAL_NEIGUNG"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Azimut",             "Azimut", "°", 0, "BV_A1D_MERKMAL_AZIMUT"),
            // Paket A des PV-Ertragsmodells (Stufe E1.3), Migrationsschritt 62.
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_WrWirkungsgrad",  "Wechselrichter-Wirkungsgrad", "", 2, "BV_A1D_MERKMAL_WR_WG"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_Systemverluste",  "Systemverluste", "%", 1, "BV_A1D_MERKMAL_SYSTEMVERLUSTE"),
            // Paket B desselben Konzepts (Stufe E2), Migrationsschritt 63.
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_Modell",          "PV-Rechenmodell", "", TEXT, "BV_A1D_MERKMAL_PV_MODELL"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_WrNennleistungKw","Wechselrichter-Nennleistung", "kW", 1, "BV_A1D_MERKMAL_WR_NENNLEISTUNG"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_WrEta10",         "Wechselrichter-Wirkungsgrad bei 10 %", "", 3, "BV_A1D_MERKMAL_WR_WG10"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_WrEta50",         "Wechselrichter-Wirkungsgrad bei 50 %", "", 3, "BV_A1D_MERKMAL_WR_WG50"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "PV_WrEta100",        "Wechselrichter-Wirkungsgrad bei 100 %", "", 3, "BV_A1D_MERKMAL_WR_WG100"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Kollektormodulanzahl","Kollektormodulanzahl", "", 0, "BV_A1D_MERKMAL_KOLLEKTORMODULE"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Solaranteil",        "Solaranteil", "%", 0, "BV_A1D_MERKMAL_SOLARANTEIL"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "Volumen",            "Speichervolumen (Anlage)", "l", 0, "BV_A1D_MERKMAL_SPEICHERVOLUMEN"),
            new Merkmal("Anlage", "Tab_Energieanlagen", "WQ_Typ",             "Wärmequelle", "", TEXT, "BV_A1D_MERKMAL_WAERMEQUELLE"),

            // Wärmepumpe (Tab_WP)
            new Merkmal("Wärmepumpe", "Tab_WP", "Bezeichner",   "Komponente", "", TEXT, "BV_A1D_MERKMAL_KOMPONENTE"),
            new Merkmal("Wärmepumpe", "Tab_WP", "Firma",        "Hersteller", "", TEXT, "BV_A1D_MERKMAL_HERSTELLER"),
            new Merkmal("Wärmepumpe", "Tab_WP", "Typ",          "Typ", "", TEXT, "BV_A1D_MERKMAL_TYP"),
            new Merkmal("Wärmepumpe", "Tab_WP", "Bauart",       "Bauart", "", TEXT, "BV_A1D_MERKMAL_BAUART"),
            new Merkmal("Wärmepumpe", "Tab_WP", "Nennleistung", "Nennleistung", "kW", 0, "BV_A1D_MERKMAL_NENNLEISTUNG"),
            // Ohne maxPtherm (Anwenderentscheid 29.09.2026): Kein Dialog pflegt die Spalte,
            // der Herstellerimport setzt sie nie, die Rechnung liest sie nicht - die Tafel
            // zeigte „max. therm. Leistung 0 kW“ ohne Aussage.
            new Merkmal("Wärmepumpe", "Tab_WP", "Kuehlleistung","Kühlleistung", "kW", 1, "BV_A1D_MERKMAL_KUEHLLEISTUNG"),
            new Merkmal("Wärmepumpe", "Tab_WP", "Regelung",     "Regelung", "", TEXT, "BV_A1D_MERKMAL_REGELUNG"),

            // BHKW (Tab_BHKW)
            new Merkmal("BHKW", "Tab_BHKW", "Bezeichner", "Komponente", "", TEXT, "BV_A1D_MERKMAL_KOMPONENTE"),
            new Merkmal("BHKW", "Tab_BHKW", "Firma",      "Hersteller", "", TEXT, "BV_A1D_MERKMAL_HERSTELLER"),
            new Merkmal("BHKW", "Tab_BHKW", "Motortyp",   "Motortyp", "", TEXT, "BV_A1D_MERKMAL_MOTORTYP"),
            new Merkmal("BHKW", "Tab_BHKW", "Ptherm",     "therm. Leistung", "kW", 1, "BV_A1D_MERKMAL_LEISTUNG_TH"),
            new Merkmal("BHKW", "Tab_BHKW", "Pel",        "el. Leistung", "kW", 1, "BV_A1D_MERKMAL_LEISTUNG_EL"),
            // OHNE EINHEIT: Der BHKW-Wirkungsgrad ist ein FAKTOR (0…1), kein
            // Prozentwert - so rechnet SimulationBHKW, und seit Schemaschritt 98 steht
            // er im ganzen Katalog so. Die zwei Anteile daneben ergeben ihn
            // (Schemaschritt 99); drei Stellen, wie der Katalog sie pflegt.
            new Merkmal("BHKW", "Tab_BHKW", "Wirkungsgrad","Ges. Wirkungsgrad", "", 3, "BV_A1D_MERKMAL_WG_GESAMT"),
            new Merkmal("BHKW", "Tab_BHKW", "Wirkungsgrad_el","el. Wirkungsgrad", "", 3, "BV_A1D_MERKMAL_WG_EL"),
            new Merkmal("BHKW", "Tab_BHKW", "Wirkungsgrad_th","therm. Wirkungsgrad", "", 3, "BV_A1D_MERKMAL_WG_TH"),
            new Merkmal("BHKW", "Tab_BHKW", "Vorlauf",    "Vorlauf", "°C", 0, "BV_A1D_MERKMAL_VORLAUF"),
            new Merkmal("BHKW", "Tab_BHKW", "Ruecklauf",  "Rücklauf", "°C", 0, "BV_A1D_MERKMAL_RUECKLAUF"),

            // Spitzenkessel (Tab_Heizkessel)
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Bezeichner",       "Komponente", "", TEXT, "BV_A1D_MERKMAL_KOMPONENTE"),
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Firma",            "Hersteller", "", TEXT, "BV_A1D_MERKMAL_HERSTELLER"),
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Ptherm",           "therm. Leistung", "kW", 1, "BV_A1D_MERKMAL_LEISTUNG_TH"),
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Wirkungsgrad_Gas", "Wirkungsgrad Gas", "%", 1, "BV_A1D_MERKMAL_WG_GAS"),
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Wirkungsgrad_Öl",  "Wirkungsgrad Öl", "%", 1, "BV_A1D_MERKMAL_WG_OEL"),
            new Merkmal("Spitzenkessel", "Tab_Heizkessel", "Brennwert",        "Brennwertnutzung", "", JN, "BV_A1D_MERKMAL_BRENNWERT"),

            // Solarthermie (Tab_Solarkollektoren)
            new Merkmal("Solarthermie", "Tab_Solarkollektoren", "Bezeichner",    "Komponente", "", TEXT, "BV_A1D_MERKMAL_KOMPONENTE"),
            new Merkmal("Solarthermie", "Tab_Solarkollektoren", "Kollektortyp",  "Kollektortyp", "", TEXT, "BV_A1D_MERKMAL_KOLLEKTORTYP"),
            new Merkmal("Solarthermie", "Tab_Solarkollektoren", "Aperturflaeche","Aperturfläche", "m²", 2, "BV_A1D_MERKMAL_APERTURFLAECHE"),

            // Photovoltaik (Tab_PV)
            new Merkmal("Photovoltaik", "Tab_PV", "Bezeichner",  "Komponente", "", TEXT, "BV_A1D_MERKMAL_KOMPONENTE"),
            new Merkmal("Photovoltaik", "Tab_PV", "Firma",       "Hersteller", "", TEXT, "BV_A1D_MERKMAL_HERSTELLER"),
            new Merkmal("Photovoltaik", "Tab_PV", "Leistung",    "Modulleistung", "W", 0, "BV_A1D_MERKMAL_MODULLEISTUNG"),
            new Merkmal("Photovoltaik", "Tab_PV", "Wirkungsgrad","Wirkungsgrad", "%", 1, "BV_A1D_MERKMAL_WG"),
            new Merkmal("Photovoltaik", "Tab_PV", "Technologie", "Zelltechnologie", "", TEXT, "BV_A1D_MERKMAL_ZELLTECHNIK"),

            // Pufferspeicher (Tab_Pufferspeicher)
            new Merkmal("Pufferspeicher", "Tab_Pufferspeicher", "Bezeichner",   "Komponente", "", TEXT, "BV_A1D_MERKMAL_KOMPONENTE"),
            new Merkmal("Pufferspeicher", "Tab_Pufferspeicher", "Speichertyp",  "Speichertyp", "", TEXT, "BV_A1D_MERKMAL_SPEICHERTYP"),
            new Merkmal("Pufferspeicher", "Tab_Pufferspeicher", "Gesamtvolumen","Gesamtvolumen", "l", 0, "BV_A1D_MERKMAL_GESAMTVOLUMEN"),

            // Stromspeicher (Tab_Stromspeicher)
            new Merkmal("Stromspeicher", "Tab_Stromspeicher", "Bezeichner", "Komponente", "", TEXT, "BV_A1D_MERKMAL_KOMPONENTE"),
            new Merkmal("Stromspeicher", "Tab_Stromspeicher", "Typ",        "Typ", "", TEXT, "BV_A1D_MERKMAL_TYP"),
            new Merkmal("Stromspeicher", "Tab_Stromspeicher", "Leistung",   "Leistung", "kW", 1, "BV_A1D_MERKMAL_LEISTUNG"),
            new Merkmal("Stromspeicher", "Tab_Stromspeicher", "Energie",    "Kapazität", "kWh", 1, "BV_A1D_MERKMAL_KAPAZITAET"),

            // Gebäude (Tab_Gebaeude — erstes Gebäude)
            new Merkmal("Gebäude", "Tab_Gebaeude", "Waermebedarf",       "Wärmebedarf", "kWh/a", 0, "BV_A1D_MERKMAL_WAERMEBEDARF"),
            new Merkmal("Gebäude", "Tab_Gebaeude", "Wohnflaeche_gesamt", "Wohn-/Nutzfläche", "m²", 0, "BV_A1D_MERKMAL_NUTZFLAECHE"),
            new Merkmal("Gebäude", "Tab_Gebaeude", "WW_Bedarf",          "Warmwasserbedarf", "kWh/a", 0, "BV_A1D_MERKMAL_WW_BEDARF"),
            new Merkmal("Gebäude", "Tab_Gebaeude", "Luftwechselrate",    "Luftwechselrate", "1/h", 2, "BV_A1D_MERKMAL_LUFTWECHSEL"),

            // Anlagenkopplung AK1 (Konzept 9.4): die Kopplungsstufe des Projekts und die Wärmeübergabe
            // des ersten Gebäudes - sonst sähe ein Vergleich zweier Varianten mit verschiedener
            // Kopplung wie ein Modellwechsel aus. Gezeigt und verglichen wird der Anzeigename
            // (Anzeigenamen unten): NULL heißt „aus" bzw. „ideal".
            new Merkmal("Gebäude", "Tab_Einstellungen", "Anlagenkopplung", "Anlagenkopplung", "", TEXT,
                        "ABW_MERKMAL_ANLAGENKOPPLUNG"),
            new Merkmal("Gebäude", "Tab_Gebaeude", "Heizkreis_Aktiv",    "Übergabe rechnen", "", JN,
                        "ABW_MERKMAL_HEIZKREIS"),
            new Merkmal("Gebäude", "Tab_Gebaeude", "Uebergabe_Art",      "Übergabeart", "", TEXT,
                        "ABW_MERKMAL_UEBERGABEART"),
            // E37: die Kälteseite - Schalter und Kühlübergabeart (NULL heißt „ideal").
            new Merkmal("Gebäude", "Tab_Gebaeude", "Kuehluebergabe_Aktiv", "Kühlübergabe rechnen", "", JN,
                        "ABW_MERKMAL_KUEHLUEBERGABE"),
            new Merkmal("Gebäude", "Tab_Gebaeude", "Kuehl_Uebergabe_Art", "Kühlübergabeart", "", TEXT,
                        "ABW_MERKMAL_KUEHLUEBERGABEART"),

            // Stufe G6a: die Zonen des Projekts über ALLE Gebäude (ProjektDetails.Zonenmerkmale) -
            // sonst meldete ein Unterschied nur in den Zonen „Keine Abweichungen". Werte aus der
            // einen Formel der Zonenkennwerte; der Rechenweg der Hülle über seinen Anzeigenamen.
            new Merkmal("Gebäude", ProjektDetails.ZONENMERKMALE, "Zonenzahl", "Zahl der Zonen", "", 0,
                        "ABW_MERKMAL_ZONENZAHL"),
            new Merkmal("Gebäude", ProjektDetails.ZONENMERKMALE, "Zonenflaeche", "Σ Nutzfläche der Zonen", "m²", 0,
                        "ABW_MERKMAL_ZONENFLAECHE"),
            new Merkmal("Gebäude", ProjektDetails.ZONENMERKMALE, "Zonen_HT", "Σ H_T der Zonen", "W/K", 1,
                        "ABW_MERKMAL_ZONEN_HT"),
            new Merkmal("Gebäude", ProjektDetails.ZONENMERKMALE, "Huellrechenweg", "Rechenweg der Hülle", "", TEXT,
                        "ABW_MERKMAL_HUELLRECHENWEG"),

            // KP3 Welle O3b (B21; Festlegungen 29 und 39, E59): die Aufheizoptimierung des Projekts, ihre wirksame Art über
            // die Gebäude und die manuelle Aufheizzeit des ersten Gebäudes - sonst meldete der Vergleich mit und ohne Rampe
            // (P8) „Keine Abweichungen". NULL heißt die Vorgabe: Bemessung und Art über ihren Anzeigenamen, die Zahlen über
            // Zahlvorgaben (unten); die Reserve steht als Anteil in der Datenbank und als Prozent im Vergleich.
            new Merkmal("Gebäude", "Tab_Einstellungen", AufheizvorgabeSchema.SPALTE_SCHALTER, "Aufheizoptimierung", "", JN,
                        "ABW_MERKMAL_AUFH_SCHALTER"),
            new Merkmal("Gebäude", "Tab_Einstellungen", AufheizvorgabeSchema.SPALTE_BEMESSUNG, "Bemessung der Aufheizzeit", "", TEXT,
                        "ABW_MERKMAL_AUFH_BEMESSUNG"),
            new Merkmal("Gebäude", "Tab_Einstellungen", AufheizvorgabeSchema.SPALTE_ABZUG, "Abzug ΔT_K", "K", 1,
                        "ABW_MERKMAL_AUFH_ABZUG"),
            new Merkmal("Gebäude", "Tab_Einstellungen", AufheizvorgabeSchema.SPALTE_RESERVE, "Aufheizreserve ρ", "%", 0,
                        "ABW_MERKMAL_AUFH_RESERVE"),
            new Merkmal("Gebäude", AUFHEIZMERKMALE, SPALTE_AUFHEIZART, "Art der Aufheizzeit", "", TEXT,
                        "ABW_MERKMAL_AUFH_ART"),
            new Merkmal("Gebäude", "Tab_Einstellungen", AufheizManuellSchema.SPALTE_AUFSCHLAG_H, "Aufschlag (h)", "h", 0,
                        "ABW_MERKMAL_AUFH_AUFSCHLAG_H"),
            new Merkmal("Gebäude", "Tab_Einstellungen", AufheizManuellSchema.SPALTE_AUFSCHLAG_PROZENT, "Aufschlag (%)", "%", 1,
                        "ABW_MERKMAL_AUFH_AUFSCHLAG_PROZENT"),
            new Merkmal("Gebäude", "Tab_Gebaeude", AufheizManuellSchema.SPALTE_MANUELL, "Aufheizzeit manuell (h)", "h", 0,
                        "ABW_MERKMAL_AUFH_MANUELL"),
        };

        /// <summary>
        /// Die Pseudotabelle der Aufheizmerkmale (Muster <see cref="ProjektDetails.ZONENMERKMALE"/>): eine Zeile, aus der Projekt-
        /// einstellung und allen Gebäuden gebildet (<see cref="Aufheizmerkmale"/>). Sie ist keine Tabelle der Datenbank — eine
        /// Übernahme findet keine Zielzeile.
        /// </summary>
        public const string AUFHEIZMERKMALE = "Aufheizung";

        /// <summary>Die wirksamen Arten der Aufheizzeit über die Gebäude als Steuerwerte, kommagetrennt in der Folge der Ergebnisarten.</summary>
        public const string SPALTE_AUFHEIZART = "Art";

        /// <summary>
        /// <b>Die Zeile der Aufheizmerkmale</b> eines Projekts (Festlegung 39): je Gebäude die wirksame Art — „manuell“, wenn es
        /// eine manuelle Aufheizzeit trägt, sonst die Art der Projekteinstellung (NULL = täglich) —, über alle Gebäude die
        /// verschiedenen Arten in der Folge täglich, fest, manuell. <c>null</c> ohne Gebäude.
        /// </summary>
        public static DataRow Aufheizmerkmale(ProjektDetails d)
        {
            if (d?.Gebaeude == null || d.Gebaeude.Rows.Count == 0) return null;
            DataRow e = d.Einstellungen != null && d.Einstellungen.Rows.Count > 0 ? d.Einstellungen.Rows[0] : null;
            string projekt = ProjektDetails.S(e, AufheizvorgabeSchema.SPALTE_ART).Trim() == DbWerte.AUFHEIZ_ART_FEST
                ? DbWerte.AUFHEIZ_ART_FEST : DbWerte.AUFHEIZ_ART_TAEGLICH;
            var arten = new HashSet<string>(StringComparer.Ordinal);
            foreach (DataRow g in d.Gebaeude.Rows)
                arten.Add(ProjektDetails.D(g, AufheizManuellSchema.SPALTE_MANUELL) is double h && h > 0 ? DbWerte.AUFHEIZ_ART_MANUELL : projekt);
            var dt = new DataTable();
            dt.Columns.Add(SPALTE_AUFHEIZART, typeof(string));
            dt.Rows.Add(string.Join(",", DbWerte.AUFHEIZ_ERGEBNIS_ARTEN.Where(arten.Contains)));
            return dt.Rows[0];
        }

        /// <summary>Der Anzeigename der Bemessung: NULL und <c>STUNDE</c> heißen beide „kälteste Stunde“ (Variante (a)).</summary>
        private static string Bemessungsname(string steuerwert)
            => steuerwert == DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG ? MyResource.Resource.SIMKONF_AUFH_BEMESSUNG_ABZUG
             : string.IsNullOrEmpty(steuerwert) || steuerwert == DbWerte.AUFHEIZ_BEMESSUNG_STUNDE ? MyResource.Resource.SIMKONF_AUFH_BEMESSUNG_STUNDE
             : steuerwert;

        /// <summary>Der Anzeigename der wirksamen Arten (<see cref="SPALTE_AUFHEIZART"/>): „täglich“, „fest“, „manuell“, mehrere mit Komma.</summary>
        private static string Artname(string steuerwert)
        {
            if (string.IsNullOrEmpty(steuerwert)) return "—";
            return string.Join(", ", steuerwert.Split(',').Select(a => a switch
            {
                DbWerte.AUFHEIZ_ART_TAEGLICH => MyResource.Resource.SIMKONF_AUFH_ART_TAEGLICH,
                DbWerte.AUFHEIZ_ART_FEST => MyResource.Resource.SIMKONF_AUFH_ART_FEST,
                DbWerte.AUFHEIZ_ART_MANUELL => MyResource.Resource.ABW_MERKMAL_AUFH_WERT_MANUELL,
                _ => a,
            }));
        }

        /// <summary>
        /// <b>Zahlvorgaben</b> der Zahlmerkmale, deren NULL einen Wert bedeutet (Schlüssel „Tabelle.Spalte“): der Wert für NULL
        /// und der Faktor der Anzeige. Verglichen und gezeigt wird der wirksame Wert — NULL und die Vorgabe sind keine Abweichung.
        /// </summary>
        private static readonly Dictionary<string, (double Vorgabe, double Faktor)> Zahlvorgaben =
            new Dictionary<string, (double Vorgabe, double Faktor)>(StringComparer.OrdinalIgnoreCase)
            {
                { "Tab_Einstellungen." + AufheizvorgabeSchema.SPALTE_ABZUG, (AufheizvorgabeSchema.ABZUG_VORGABE_K, 1.0) },
                { "Tab_Einstellungen." + AufheizvorgabeSchema.SPALTE_RESERVE, (AufheizvorgabeSchema.RESERVE_VORGABE, 100.0) },
                { "Tab_Einstellungen." + AufheizManuellSchema.SPALTE_AUFSCHLAG_H, (0.0, 1.0) },
                { "Tab_Einstellungen." + AufheizManuellSchema.SPALTE_AUFSCHLAG_PROZENT, (0.0, 1.0) },
            };

        /// <summary>Der wirksame Zahlwert eines Merkmals: der Spaltenwert, NULL über die Zahlvorgabe, mal dem Anzeigefaktor.</summary>
        private static double? Zahl(DataRow r, Merkmal f)
        {
            double? d = ProjektDetails.D(r, f.Spalte);
            if (!Zahlvorgaben.TryGetValue(f.Tabelle + "." + f.Spalte, out (double Vorgabe, double Faktor) z)) return d;
            return (d ?? z.Vorgabe) * z.Faktor;
        }

        /// <summary>
        /// <b>Anzeigenamen der Steuerwerte</b> (Drei-Schichten-Regel): Die Datenbank führt deutsche,
        /// eingefrorene Schlüssel (<c>RADIATOR</c>, <c>AK1</c>); der Vergleich ZEIGT den Anzeigenamen
        /// und VERGLEICHT ihn auch — NULL und „AUS" heißen dasselbe, und ein Unterschied zwischen
        /// ihnen ist keiner. Schlüssel ist „Tabelle.Spalte".
        /// </summary>
        private static readonly Dictionary<string, Func<string, string>> Anzeigenamen =
            new Dictionary<string, Func<string, string>>(StringComparer.OrdinalIgnoreCase)
            {
                { "Tab_Einstellungen.Anlagenkopplung", Waermeuebergabevorgaben.Stufenname },
                { "Tab_Gebaeude.Uebergabe_Art", Waermeuebergabevorgaben.Anzeigename },
                { "Tab_Gebaeude.Kuehl_Uebergabe_Art", Waermeuebergabevorgaben.KuehlAnzeigename },
                { ProjektDetails.ZONENMERKMALE + ".Huellrechenweg", ProjektDetails.Huellrechenwegtext },
                { "Tab_Einstellungen." + AufheizvorgabeSchema.SPALTE_BEMESSUNG, Bemessungsname },
                { AUFHEIZMERKMALE + "." + SPALTE_AUFHEIZART, Artname },
            };

        /// <summary>Der Anzeigeweg eines Textmerkmals mit Steuerwerten; <c>null</c> = der Wert, wie er ist.</summary>
        private static Func<string, string> Anzeige(Merkmal f)
            => f != null && Anzeigenamen.TryGetValue(f.Tabelle + "." + f.Spalte, out Func<string, string> a) ? a : null;

        /// <summary>
        /// Die Kultur der Zahlen: die der Berichtssprache (<see cref="BerichtTexte.Kultur"/> — im Lauf eines Berichts
        /// dessen Sprache, sonst die Oberflächensprache), wie die übrigen Berichtsbausteine. Bei jedem Zugriff gelesen,
        /// damit ein englischer Bericht „1.5“ und ein deutscher „1,5“ zeigt.
        /// </summary>
        private static CultureInfo Zahlkultur => BerichtTexte.Kultur;

        /// <summary>
        /// Vergleicht die Konfiguration einer Variante gegen den Stamm.
        /// Hinweis: verglichen wird je Gewerk die erste Komponente des Projekts
        /// (erste Anlagenzeile — <c>ProjektDetails.LadeGewerk</c>); unterschiedliche
        /// Einträge-Anzahlen werden als eigene Abweichung gemeldet.
        /// </summary>
        public static List<Abweichung> Vergleiche(ProjektDetails stamm, ProjektDetails variante)
        {
            var liste = new List<Abweichung>();
            if (stamm == null || variante == null) return liste;

            // Stufe 1: Gewerk vorhanden / nicht vorhanden + Anzahl.
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
            {
                int nS = Anzahl(stamm, g.Key);
                int nV = Anzahl(variante, g.Key);
                if ((nS > 0) != (nV > 0))
                    liste.Add(new Abweichung
                    {
                        Gewerk = g.Key, Merkmal = MerkmalBestand, Schluessel = SCHLUESSEL_BESTAND,
                        WertStamm = nS > 0 ? BestandVorhanden : BestandFehlt,
                        WertVariante = nV > 0 ? BestandVorhanden : BestandFehlt
                    });
                else if (nS != nV)
                    liste.Add(new Abweichung
                    {
                        Gewerk = g.Key, Merkmal = MerkmalAnzahl, Schluessel = SCHLUESSEL_ANZAHL,
                        WertStamm = nS.ToString(Zahlkultur), WertVariante = nV.ToString(Zahlkultur)
                    });
            }

            // PVG: die Quelle der Photovoltaik als eigene Zeile - Modulmodell oder Ganglinie (Name, Raster,
            // Nennleistung), sobald einer der beiden Stände ueber eine Ganglinie rechnet.
            Abweichung quelle = PvQuelleVergleichen(stamm, variante);
            if (quelle != null) liste.Add(quelle);

            // Stufe 2/3: Merkmalsvergleich über die deklarative Feldliste.
            // Artefakt-Guard (Nutzerbefund 28.08.2026): Der Anlage-Block vergleicht
            // die jeweils erste ECHTE Anlagenzeile — führen Stamm und Variante dort
            // verschiedene Gewerke, entfallen seine Merkmalszeilen (siehe
            // AnlagenVergleichbar); Referenzanlagen zählen nie (ErsteEchteAnlage).
            bool anlagenVergleichbar = AnlagenVergleichbar(stamm, variante);
            foreach (Merkmal f in Felder)
            {
                if (f.Tabelle == "Tab_Energieanlagen" && !anlagenVergleichbar) continue;
                DataRow rS = ZeileFuer(stamm, f);
                DataRow rV = ZeileFuer(variante, f);
                if (rS == null && rV == null) continue;            // Gewerk in beiden nicht vorhanden
                if (rS == null || rV == null) continue;            // Bestand bereits in Stufe 1 gemeldet

                string wS = Formatiere(rS, f);
                string wV = Formatiere(rV, f);
                if (!WerteGleich(rS, rV, f))
                    liste.Add(new Abweichung
                    {
                        Gewerk = f.Gewerk, Merkmal = f.Label, Schluessel = Schluessel(f), WertStamm = wS, WertVariante = wV
                    });
            }

            return liste;
        }

        /// <summary>Der Schlüssel der Zeile „Quelle" der Photovoltaik (Modulmodell oder PV-Ganglinie).</summary>
        public const string SCHLUESSEL_PV_QUELLE = "PV_QUELLE";

        /// <summary>
        /// Die Quelle der Photovoltaik eines Stands als Text: der Ausweis der rechnenden Ganglinie
        /// (<see cref="PvGanglinieAusweis.Text"/> — Name, Raster, Nennleistung), sonst „Modulmodell", wenn der Stand
        /// Photovoltaik führt; <c>null</c> ohne Photovoltaik.
        /// </summary>
        public static string PvQuelle(ProjektDetails d)
        {
            if (d == null) return null;
            if (d.PvGanglinie != null) return d.PvGanglinie.Text(Zahlkultur);
            return Anzahl(d, "Photovoltaik") > 0 ? Text("PVG_AUSWEIS_MODULMODELL", "Modulmodell") : null;
        }

        /// <summary>
        /// <b>Die Abweichung in der Quelle der Photovoltaik</b> (Modulmodell ↔ Ganglinie, eine andere Ganglinie, ein
        /// anderes Raster oder eine andere Nennleistung) — nur, wenn mindestens einer der beiden Stände über eine
        /// Ganglinie rechnet; Modulmodell gegen keine Photovoltaik meldet schon die Bestandszeile. Ein Stand ohne
        /// Photovoltaik steht als „nicht vorhanden". <c>null</c> ohne Unterschied.
        /// </summary>
        public static Abweichung PvQuelleVergleichen(ProjektDetails stamm, ProjektDetails variante)
        {
            if (stamm == null || variante == null) return null;
            if (stamm.PvGanglinie == null && variante.PvGanglinie == null) return null;
            string qS = PvQuelle(stamm) ?? BestandFehlt;
            string qV = PvQuelle(variante) ?? BestandFehlt;
            if (string.Equals(qS, qV, StringComparison.Ordinal)) return null;
            return new Abweichung
            {
                Gewerk = "Photovoltaik", Merkmal = Text("PVG_AUSWEIS_MERKMAL_QUELLE", "Quelle"),
                Schluessel = SCHLUESSEL_PV_QUELLE, WertStamm = qS, WertVariante = qV
            };
        }

        /// <summary>
        /// Anzahl der Komponenten EINES GEWERKS im Projekt — die Kennzahl der
        /// Stufe-1-Zeile „Anzahl Komponenten". Quelle ist
        /// <see cref="ProjektDetails.KomponentenAnzahl"/>, also der über
        /// <c>Tab_Energieanlagen</c> ermittelte VERBAUTE Bestand
        /// (<c>ProjektDetails.LadeGewerk</c>) — NICHT der rohe Zeilenbestand der
        /// Gerätetabelle, der auch Altkopien führt, auf die keine Anlage mehr zeigt.
        /// Öffentlich, damit die Gegenüberstellung der Seite „Übersicht" dieselbe
        /// Kennzahl aus derselben Quelle liest wie die Unterschiedsanzeige.
        /// </summary>
        public static int Anzahl(ProjektDetails d, string gewerk)
        {
            return (d != null && gewerk != null && d.KomponentenAnzahl.ContainsKey(gewerk))
                ? d.KomponentenAnzahl[gewerk] : 0;
        }

        /// <summary>
        /// Anzeigetext der Anzahlzeile: die Zahl — bei 0 das „nicht vorhanden" der
        /// Bestandszeile. Eine „0" allein wäre in einer Gegenüberstellung ohne
        /// Vergleichspartner nicht von einer ungezählten Zelle zu unterscheiden.
        /// </summary>
        public static string AnzahlText(int n)
        {
            return n > 0 ? n.ToString(Zahlkultur) : BestandFehlt;
        }

        /// <summary>
        /// Die Datenzeile eines Projekts, aus der ein Merkmal gelesen wird
        /// (null = Gewerk im Projekt nicht vorhanden). Öffentlich, damit die Seite
        /// „Übersicht" des Reiters „Berichte &amp; Kosten" die Komponenten des
        /// Stammprojekts mit derselben Feldliste anzeigen kann wie der Bericht.
        /// </summary>
        public static DataRow ZeileFuer(ProjektDetails d, Merkmal f)
        {
            if (f.Tabelle == "Tab_Energieanlagen")
                return ErsteEchteAnlage(d);
            if (f.Tabelle == "Tab_Gebaeude")
                return (d.Gebaeude != null && d.Gebaeude.Rows.Count > 0) ? d.Gebaeude.Rows[0] : null;
            if (f.Tabelle == "Tab_Einstellungen")
                return (d.Einstellungen != null && d.Einstellungen.Rows.Count > 0) ? d.Einstellungen.Rows[0] : null;
            if (f.Tabelle == ProjektDetails.ZONENMERKMALE)
                return (d.Zonenmerkmale != null && d.Zonenmerkmale.Rows.Count > 0) ? d.Zonenmerkmale.Rows[0] : null;
            if (f.Tabelle == AUFHEIZMERKMALE)
                return Aufheizmerkmale(d);
            foreach (KeyValuePair<string, string> g in ProjektDetails.GewerkTabellen)
                if (g.Value == f.Tabelle)
                    return d.Komponenten.ContainsKey(g.Key) ? d.Komponenten[g.Key] : null;
            return null;
        }

        /// <summary>Die n-te Komponentenzeile eines Gewerks (null wenn nicht vorhanden) —
        /// Grundlage der „eine Zeile je Komponente"-Gegenüberstellung (28.08.2026).</summary>
        public static DataRow KomponenteZeile(ProjektDetails d, string gewerk, int index)
        {
            if (d == null || gewerk == null || index < 0) return null;
            DataTable dt;
            if (!d.KomponentenAlle.TryGetValue(gewerk, out dt) || dt == null) return null;
            return index < dt.Rows.Count ? dt.Rows[index] : null;
        }

        /// <summary>Das Bezeichner-Merkmal eines Gewerks (Label „Komponente" der
        /// deklarativen Feldliste) — liefert der Komponentenzeile ihren Namen.</summary>
        public static Merkmal BezeichnerMerkmal(string gewerk)
        {
            foreach (Merkmal f in Felder)
                if (f.Gewerk == gewerk && f.Spalte == "Bezeichner") return f;
            return null;
        }

        /// <summary>
        /// Merkmalstext einer Komponentenzeile über die deklarative Feldliste ihres
        /// Gewerks — je belegtem Merkmal „Label: Wert"; das Bezeichner-Merkmal bleibt
        /// außen vor (es steht bereits in der Zelle). Eine Wahrheit für Mouse-over
        /// und Auswahlanzeige der Gegenüberstellung (Nutzerauftrag 28.08.2026).
        /// </summary>
        public static string MerkmaleText(DataRow r, string gewerk, string trenner)
        {
            if (r == null) return "";
            var teile = new List<string>();
            foreach (Merkmal f in Felder)
            {
                if (f.Gewerk != gewerk || f.Spalte == "Bezeichner") continue;
                if (!r.Table.Columns.Contains(f.Spalte)) continue;
                string w = Formatiere(r, f);
                if (string.IsNullOrEmpty(w) || w == "—") continue;
                teile.Add(f.Label + ": " + w);
            }
            return string.Join(trenner, teile);
        }

        /// <summary>
        /// Die erste ECHTE Anlagenzeile eines Projekts — Referenzanlagen
        /// (<c>WizardItemClass.REF_KESSEL_TYP</c>…<c>REF_PV_TYP</c>) bleiben außen
        /// vor: Sie sind im Bereich Energieerzeuger nicht als Projektanlage
        /// angelegt und lieferten dem Anlage-Block sonst Artefaktwerte
        /// (Nutzerbefund 28.08.2026).
        /// </summary>
        public static DataRow ErsteEchteAnlage(ProjektDetails d)
        {
            if (d == null || d.Anlagen == null) return null;
            foreach (DataRow r in d.Anlagen.Rows)
            {
                int typ = (int)(ProjektDetails.D(r, "ID_Type") ?? 0);
                if (typ < WizardItemClass.REF_KESSEL_TYP || typ > WizardItemClass.REF_PV_TYP)
                    return r;
            }
            return null;
        }

        /// <summary>
        /// true, wenn die Anlage-Merkmale zweier Projekte vergleichbar sind: Beide
        /// führen eine echte Anlagenzeile DESSELBEN Gewerks (ID_Type). Ein
        /// WP-Stamm gegen eine BHKW-Variante verglich sonst Äpfel mit Birnen —
        /// die Tabelle zeigte „Anlage"-Unterschiede einer Anlage, die es im
        /// Bereich Energieerzeuger des Partners gar nicht gibt; der
        /// Systemunterschied steht bereits in den Bestandszeilen der Stufe 1.
        /// </summary>
        public static bool AnlagenVergleichbar(ProjektDetails a, ProjektDetails b)
        {
            DataRow ra = ErsteEchteAnlage(a), rb = ErsteEchteAnlage(b);
            if (ra == null || rb == null) return false;
            return (int)(ProjektDetails.D(ra, "ID_Type") ?? 0) ==
                   (int)(ProjektDetails.D(rb, "ID_Type") ?? 0);
        }

        /// <summary>
        /// true, wenn ALLE Versionen mit echter Anlagenzeile dasselbe Gewerk
        /// führen (für die Gegenüberstellung der Übersicht — sonst stünden Werte
        /// verschiedener Gewerke nebeneinander in einer „Anlage"-Zeile).
        /// </summary>
        public static bool AnlagenEinheitlich(IEnumerable<ProjektDetails> versionen)
        {
            if (versionen == null) return false;
            int typ = 0;
            foreach (ProjektDetails d in versionen)
            {
                DataRow r = ErsteEchteAnlage(d);
                if (r == null) continue;
                int t = (int)(ProjektDetails.D(r, "ID_Type") ?? 0);
                if (typ == 0) typ = t;
                else if (typ != t) return false;
            }
            return typ != 0;
        }

        private static bool WerteGleich(DataRow a, DataRow b, Merkmal f)
        {
            if (f.Dez == TEXT)
            {
                Func<string, string> anzeige = Anzeige(f);
                if (anzeige != null)
                    return string.Equals(anzeige(ProjektDetails.S(a, f.Spalte).Trim()),
                                         anzeige(ProjektDetails.S(b, f.Spalte).Trim()), StringComparison.Ordinal);
                return string.Equals(ProjektDetails.S(a, f.Spalte).Trim(),
                                     ProjektDetails.S(b, f.Spalte).Trim(),
                                     StringComparison.OrdinalIgnoreCase);
            }
            if (f.Dez == JN)
            {
                bool? x = ProjektDetails.B(a, f.Spalte), y = ProjektDetails.B(b, f.Spalte);
                return (x ?? false) == (y ?? false);
            }
            double? u = Zahl(a, f), v = Zahl(b, f);
            if (!u.HasValue && !v.HasValue) return true;
            if (!u.HasValue || !v.HasValue) return false;
            double toleranz = Math.Pow(10, -Math.Max(f.Dez, 0)) / 2.0;   // halbe Anzeigestelle
            return Math.Abs(u.Value - v.Value) <= toleranz;
        }

        /// <summary>Formatiert einen Merkmalswert für Tabellen (Abweichung/Kenndaten). null/leer → „—".</summary>
        public static string Formatiere(DataRow r, Merkmal f)
        {
            if (f.Dez == TEXT)
            {
                string s = ProjektDetails.S(r, f.Spalte).Trim();
                Func<string, string> anzeige = Anzeige(f);
                if (anzeige != null) return anzeige(s);
                return s.Length == 0 ? "—" : s;
            }
            if (f.Dez == JN)
            {
                bool? b = ProjektDetails.B(r, f.Spalte);
                // Der Wahrheitswert in der Anzeigesprache (im Bericht: der Laufsprache, BerichtTexte.ImLauf) —
                // dieselben Texte wie der Ja/Nein-Wert der Parameterübersicht und des Katalogfilters.
                return !b.HasValue ? "—" : (b.Value ? MyResource.Resource.ALLG_BTN_JA : MyResource.Resource.ALLG_BTN_NEIN);
            }
            double? d = Zahl(r, f);
            if (!d.HasValue) return "—";
            string txt = d.Value.ToString("N" + f.Dez, Zahlkultur);
            return string.IsNullOrEmpty(f.Einheit) ? txt : txt + " " + f.Einheit;
        }
    }
}
