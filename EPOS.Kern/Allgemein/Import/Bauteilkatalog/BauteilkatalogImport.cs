using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // BAUTEILKATALOG EINLESEN - Regeln, Vorschau und Schreiben (BK-3, Entscheide E132 und E133).
    //
    // DER WEG: BauteilkatalogQuelle.Lesen (sechs CSV-Dateien) -> Planen (Regeln 1-12 des Entwurfs 2.3,
    // Zusammenfuehrung, Kennzeichen; ohne Datenbank, gegen einen Abzug des Katalogs) -> Vorschau
    // (Zahlen je Satzart) oder Uebernehmen (EIN Vorgang, Rueckweg bei jedem Fehler).
    //
    // ZUSAMMENFUEHREN (E132 F4): gleich sind zwei Baustoffe, wenn der Name nach N1/N2 aufbereitet
    // (Baustoffabgleich.Schluessel) und die Gruppe uebereinstimmen und lambda +-2 %, rho +-5 %, c +-10 %
    // zutreffen. Innerhalb der Quelle wird daraus ein Satz; gegen den Katalog behaelt der vorhandene
    // Satz seine Werte, der Import fuellt nur leere Felder und haengt seine Quelle an. Ein Satz mit
    // derselben Quellkennung wird wiedererkannt: ein Importsatz wird aktualisiert, jeder andere nur
    // ergaenzt - ein Auslieferungssatz (VORGABE) wird nie ueberschrieben.
    //
    // KENNZEICHEN setzt allein der Kern (Bauteilkatalog.MeistUngeeignet). Der Rechenweg liest keine
    // Spalte, die hier geschrieben wird, ausser lambda, rho, c, Dicke und Luftschicht der Schichten.
    // ====================================================================================

    /// <summary>Die Zahlen einer Satzart in Vorschau und Ergebnis.</summary>
    public sealed class BauteilkatalogZaehlung
    {
        /// <summary>Neu anzulegen.</summary>
        public int Neu { get; internal set; }
        /// <summary>Mit einem vorhandenen Satz des Katalogs zusammengeführt (nur leere Felder ergänzt).</summary>
        public int MitKatalog { get; internal set; }
        /// <summary>Innerhalb der Quelle mit einem gleichen Satz zusammengeführt.</summary>
        public int InQuelle { get; internal set; }
        /// <summary>Per Quellkennung wiedererkannt (Importsatz aktualisiert, sonst ergänzt).</summary>
        public int Wiedererkannt { get; internal set; }
        /// <summary>Abgelehnt, je Grund in <see cref="Gruende"/>.</summary>
        public int Abgelehnt { get; internal set; }
        /// <summary>Ablehnungsgrund → Anzahl.</summary>
        public SortedDictionary<string, int> Gruende { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);

        internal void Ablehnen(string grund)
        {
            Abgelehnt++;
            Gruende[grund] = Gruende.TryGetValue(grund, out int n) ? n + 1 : 1;
        }
    }

    /// <summary>Die Stichprobe der Richtung innen → außen (Regel 7).</summary>
    public sealed class BauteilkatalogRichtungsprobe
    {
        /// <summary>Außenwände mit einer Dämmlage und einer tragenden Schicht.</summary>
        public int Paare { get; internal set; }
        /// <summary>Davon mit der Dämmlage hinter der tragenden Schicht.</summary>
        public int DaemmungHinten { get; internal set; }
        /// <summary>Aufbauten, an denen U aus den Schichten gegen U der Quelle gerechnet wurde.</summary>
        public int UGeprueft { get; internal set; }
        /// <summary>Median von U(Schichten) / U(Quelle); <c>null</c> ohne Fall.</summary>
        public double? MedianVerhaeltnis { get; internal set; }
        /// <summary>Anteil der Fälle außerhalb ±10 % (0…1); <c>null</c> ohne Fall.</summary>
        public double? AnteilAusserhalb { get; internal set; }
    }

    /// <summary><b>Vorschau und Ergebnis</b> eines Bauteilkatalog-Imports (Kern, ohne Oberfläche).</summary>
    public sealed class BauteilkatalogVorschau
    {
        /// <summary>Grund, aus dem die ganze Quelle abgelehnt ist; <c>null</c> = lesbar.</summary>
        public string Abgelehnt { get; internal set; }
        /// <summary>Die Quellangabe der Sätze („Hottgenroth-Katalog, Datenstand JJJJ-MM“).</summary>
        public string Quelle { get; internal set; } = "";
        /// <summary>Zahlen der Baustoffe.</summary>
        public BauteilkatalogZaehlung Baustoffe { get; } = new BauteilkatalogZaehlung();
        /// <summary>Zahlen der Aufbauten.</summary>
        public BauteilkatalogZaehlung Aufbauten { get; } = new BauteilkatalogZaehlung();
        /// <summary>Zahlen der Schichten.</summary>
        public BauteilkatalogZaehlung Schichten { get; } = new BauteilkatalogZaehlung();
        /// <summary>Zahlen der Abschnitte inhomogener Schichten.</summary>
        public BauteilkatalogZaehlung Abschnitte { get; } = new BauteilkatalogZaehlung();
        /// <summary>Aufbauten mit dem Kennzeichen „für die Gebäudesimulation meist nicht geeignet“.</summary>
        public int Gekennzeichnet { get; internal set; }
        /// <summary>Schichten, die über den Namen (N1–N3) einen Baustoff derselben Gruppe treffen.</summary>
        public int SchichtenMitBaustoff { get; internal set; }
        /// <summary>Baustoffe, deren λ als Skalenfehler der Quelle durch 1 000 geteilt wurde (E134; je Zeile im Protokoll).</summary>
        public int LambdaSkaliert { get; internal set; }
        /// <summary>Baustoffe der Klasse Haustechnik (Basis-Codes 14, 19, 29, 30).</summary>
        public int HaustechnikBaustoffe { get; internal set; }
        /// <summary>Aufbauten der Klasse Haustechnik (Basis-Codes 14, 19, 29, 30).</summary>
        public int HaustechnikAufbauten { get; internal set; }
        /// <summary>Luftdurchlässe (Basis-Code 19) unter den Aufbauten — immer gekennzeichnet.</summary>
        public int Luftdurchlaesse { get; internal set; }
        /// <summary>Geplante Baustoffe je „Gruppe/Klasse“ (Codes; Klasse leer = „-“).</summary>
        public SortedDictionary<string, int> BaustoffGliederung { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);
        /// <summary>Geplante Aufbauten je „Gruppe/Klasse“ (Codes; Klasse leer = „-“).</summary>
        public SortedDictionary<string, int> AufbauGliederung { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);
        /// <summary>Codes ohne Deutung („Datei Spalte Code“ → Anzahl); sie fallen auf „Sonstige“.</summary>
        public SortedDictionary<string, int> CodesOhneDeutung { get; } = new SortedDictionary<string, int>(StringComparer.Ordinal);
        /// <summary>Die Stichprobe der Richtung (Regel 7).</summary>
        public BauteilkatalogRichtungsprobe Richtung { get; } = new BauteilkatalogRichtungsprobe();
        /// <summary>Protokollzeilen (Lücken, Widersprüche, verworfene Werte, Einheiten).</summary>
        public List<string> Protokoll { get; } = new List<string>();
        /// <summary>Nach dem Übernehmen: ob geschrieben wurde.</summary>
        public bool Geschrieben { get; internal set; }
        /// <summary>Nach dem Übernehmen: der Fehler, der den Vorgang zurückgerollt hat.</summary>
        public string Fehler { get; internal set; }
        /// <summary>Dauer von Lesen, Planen und Schreiben in Millisekunden.</summary>
        public long DauerMs { get; internal set; }

        internal void Code(string wo, string code) =>
            CodesOhneDeutung[wo + " " + code] = CodesOhneDeutung.TryGetValue(wo + " " + code, out int n) ? n + 1 : 1;
    }

    /// <summary>Ein vorhandener Katalogsatz im Abzug (Baustoff oder Aufbau).</summary>
    public sealed class BauteilkatalogBestand
    {
        /// <summary>Die Id.</summary>
        public long Id { get; set; }
        /// <summary>Der Bezeichner.</summary>
        public string Bezeichner { get; set; }
        /// <summary>Der Gruppencode (Baustoff); beim Aufbau leer.</summary>
        public string Gruppe { get; set; }
        /// <summary>λ, ρ, c (Baustoff).</summary>
        public double? Lambda { get; set; }
        /// <summary>Siehe <see cref="Lambda"/>.</summary>
        public double? Rho { get; set; }
        /// <summary>Siehe <see cref="Lambda"/>.</summary>
        public double? Cp { get; set; }
        /// <summary>Die Herkunft.</summary>
        public string Herkunft { get; set; }
        /// <summary>Die Quellkennung.</summary>
        public string Quellkennung { get; set; }
        /// <summary>Die Quelle.</summary>
        public string Quelle { get; set; }
    }

    /// <summary>Der Abzug des Katalogs, gegen den geplant wird.</summary>
    public sealed class BauteilkatalogStand
    {
        /// <summary>Die Baustoffe des Katalogs.</summary>
        public List<BauteilkatalogBestand> Baustoffe { get; } = new List<BauteilkatalogBestand>();
        /// <summary>Die Aufbauten des Katalogs.</summary>
        public List<BauteilkatalogBestand> Aufbauten { get; } = new List<BauteilkatalogBestand>();
    }

    /// <summary>
    /// <b>Bauteilkatalog einlesen</b> (BK-3): Regeln 1–12 des Entwurfs 2.3 auf den CSV-Export, Vorschau und
    /// Schreiben in den Katalog in einem Vorgang.
    /// </summary>
    public static class BauteilkatalogImport
    {
        /// <summary>Der Anfang der Quellangabe (Regel 11).</summary>
        public const string QUELLE_PRAEFIX = "Hottgenroth-Katalog";

        /// <summary>Toleranzen der Dublettenregel (relativ).</summary>
        public const double TOL_LAMBDA = 0.02, TOL_RHO = 0.05, TOL_CP = 0.10;

        /// <summary>c bis zu diesem Wert steht in kJ/(kg·K) und wird mit 1 000 multipliziert (Regel 2).</summary>
        public const double CP_KJ_GRENZE = 10.0;

        /// <summary>λ darüber nur bei Metallen (Regel 3, E134: erkannt an der Rohdichte).</summary>
        public const double LAMBDA_METALL_GRENZE = 10.0;

        /// <summary>Rohdichte [kg/m³], ab der ein Stoff mit λ &gt; 10 als Metall gilt (E134).</summary>
        public const double RHO_METALL_MIN = 2500.0;

        /// <summary>λ darüber kann bei einem Nichtmetall ein Skalenfehler der Quelle sein (Faktor 1 000, E134).</summary>
        public const double LAMBDA_SKALENFEHLER_MIN = 60.0;

        /// <summary>Der Faktor des Skalenfehlers.</summary>
        public const double LAMBDA_SKALENFAKTOR = 1000.0;

        /// <summary>Band, in dem λ/1 000 liegen muss, damit der Skalenfehler als erkannt gilt [W/(m·K)].</summary>
        public const double LAMBDA_SKALIERT_MIN = 0.02, LAMBDA_SKALIERT_MAX = 0.5;

        /// <summary>Ablehnungsgrund eines Baustoffs mit λ &gt; 10, der weder Metall noch erkannter Skalenfehler ist.</summary>
        public const string GRUND_LAMBDA = "λ > 10 ohne Metall (ρ < 2 500 kg/m³) und ohne erkennbaren Skalenfehler";

        /// <summary>Rsi und Rse, wenn die Quelle sie am Aufbau nicht nennt (DIN EN ISO 6946, horizontal).</summary>
        public const double RSI_VORGABE = 0.13, RSE_VORGABE = 0.04;

        /// <summary>
        /// <b>Stoffgruppencode der Quelle → Gruppe</b> (Regel 5, Code-Tafel nach E134): 1 Putze, Mörtel, Estriche, 2 Beton,
        /// 3 Dämmstoffe mit Bauplatten (Bauplatten), 4 Mauerwerk, 5 Dämmstoffe, 6 Holz, 7 Beläge und Abdichtungen, 8 Sonstige;
        /// 0 und leer gehören nicht zur Tafel — sie werden über <c>Basis_Auszug</c> (TcBasis) nachgetragen, sonst „Sonstige“.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, string> STOFFGRUPPE_AUS_CODE = new Dictionary<int, string>
        {
            [1] = "PUTZE", [2] = "BETON", [3] = "PLATTEN", [4] = "MAUERWERK",
            [5] = "DAEMMSTOFFE", [6] = "HOLZ", [7] = "ABDICHTUNGEN", [8] = "SONSTIGE"
        };

        /// <summary>
        /// <b>Nachbargruppen eines Stoffgruppencodes</b>: die Tafel fasst zusammen, was der Katalog trennt (Putz und Estrich,
        /// Dämmstoff und Bauplatte, Belag und Abdichtung, sonstige Stoffe mit Metallen und Erdreich). Trifft die Klasse aus dem
        /// Namen eine dieser Gruppen, gilt sie; sonst die Gruppe der Tafel.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, string[]> NACHBARGRUPPEN_AUS_CODE = new Dictionary<int, string[]>
        {
            [1] = new[] { "ESTRICHE" },
            [3] = new[] { "DAEMMSTOFFE" },
            [7] = new[] { "BODENBELAEGE" },
            [8] = new[] { "METALLE", "ERDREICH" }
        };

        /// <summary>Basis-Codes (<c>Basis_Auszug</c> GroupType) der Haustechnik-Werkstoffe und ihres Zubehörs (E134).</summary>
        public static readonly IReadOnlyCollection<int> HAUSTECHNIK_CODES = new HashSet<int> { 14, 19, 29, 30 };

        /// <summary>Basis-Code der Luftdurchlässe: an Aufbauten gekennzeichnet (E134).</summary>
        public const int CODE_LUFTDURCHLASS = 19;

        /// <summary>Die Klasse „Haustechnik“, sobald das Schema sie führt.</summary>
        public const string KLASSE_HAUSTECHNIK = "HAUSTECHNIK";

        /// <summary>
        /// Die Klasse eines Haustechnik-Satzes in der Gruppe „Sonstige“: <see cref="KLASSE_HAUSTECHNIK"/>, sobald die Liste des
        /// Schemas sie führt (Prüfklausel der Spalte <c>Klasse_Code</c>), bis dahin die sonstige Klasse der Gruppe.
        /// </summary>
        public static string KlasseHaustechnik(bool baustoff)
        {
            IReadOnlyList<(string Gruppe, string Klasse)> liste = baustoff ? BauteilkatalogSchema.BAUSTOFF_KLASSEN : BauteilkatalogSchema.AUFBAU_KLASSEN;
            if (liste.Any(k => k.Klasse == KLASSE_HAUSTECHNIK)) return KLASSE_HAUSTECHNIK;
            return baustoff ? "STOFF_SONSTIGE" : "SONSTIGES";
        }

        /// <summary>
        /// <b>Gruppe und Klasse eines Baustoffs aus Code und Name</b> (Regel 5, E134). Ein Haustechnik-Code → „Sonstige“ mit
        /// Klasse Haustechnik; ein Code der Tafel → seine Gruppe, die Klasse aus dem Namen auch in einer Nachbargruppe;
        /// leer, 0 oder unbekannt → „Sonstige“ (<paramref name="gedeutet"/> = <c>false</c> nur für einen unbekannten Code).
        /// </summary>
        public static (string Gruppe, string Klasse, bool Haustechnik) GliederungAusCode(int? code, string schluessel, out bool gedeutet)
        {
            gedeutet = true;
            if (code.HasValue && HAUSTECHNIK_CODES.Contains(code.Value))
                return (Bauteilkatalog.GRUPPE_SONSTIGE, KlasseHaustechnik(true), true);
            string gruppe = Bauteilkatalog.GRUPPE_SONSTIGE;
            if (code.HasValue && code.Value != 0)
            {
                if (STOFFGRUPPE_AUS_CODE.TryGetValue(code.Value, out string g)) gruppe = g;
                else gedeutet = false;
            }
            string klasse = KlasseAusName(schluessel, gruppe);
            if (klasse == null && code.HasValue && NACHBARGRUPPEN_AUS_CODE.TryGetValue(code.Value, out string[] nachbarn))
                foreach (string n in nachbarn)
                {
                    klasse = KlasseAusName(schluessel, n);
                    if (klasse != null) { gruppe = n; break; }
                }
            return (gruppe, klasse, false);
        }

        /// <summary>Befund der λ-Plausibilität (E134).</summary>
        public enum LambdaBefund { Gueltig, Metall, Skaliert, Abgelehnt }

        /// <summary>
        /// <b>λ-Plausibilität</b> (Regel 3, E134): λ ≤ 10 gilt; λ &gt; 10 nur bei ρ ≥ 2 500 kg/m³ (Metall); ein Nichtmetall mit
        /// λ &gt; 60, dessen λ/1 000 im Band 0,02–0,5 liegt, ist ein Skalenfehler der Quelle und wird durch 1 000 geteilt;
        /// jedes übrige λ &gt; 10 wird abgelehnt (Rückgabe <c>null</c>). λ ≤ 0 oder leer bleibt „kein Wert“ des Aufrufers.
        /// </summary>
        public static (double? Lambda, LambdaBefund Befund) LambdaPruefen(double? lambda, double? rho)
        {
            if (!(lambda > LAMBDA_METALL_GRENZE)) return (lambda, LambdaBefund.Gueltig);
            if (rho >= RHO_METALL_MIN) return (lambda, LambdaBefund.Metall);
            double skaliert = lambda.Value / LAMBDA_SKALENFAKTOR;
            if (lambda.Value > LAMBDA_SKALENFEHLER_MIN && skaliert >= LAMBDA_SKALIERT_MIN && skaliert <= LAMBDA_SKALIERT_MAX)
                return (skaliert, LambdaBefund.Skaliert);
            return (null, LambdaBefund.Abgelehnt);
        }

        /// <summary>
        /// <b>Bauteilart aus <c>Basis_Auszug</c> GroupType/SubType</b> (Regel 6, E134): 1 Wände (Untertyp 2 Innenwand,
        /// sonst Außenwand), 2 Türen, 3 Fenster, 4 Decken und Böden, 5 Dächer, 15|0 und 16|0 Wände (Außenwand); 17 Wärmebrücken
        /// und die Haustechnik-Codes 14, 19 (Luftdurchlässe), 29, 30 sind gedeutet, aber kein Aufbau der Hülle → Sonstiges;
        /// alles Übrige ist ungedeutet → Sonstiges mit Protokoll.
        /// </summary>
        public static (string Bauteilart, bool Gedeutet) BauteilartAusBasis(int? gruppe, int? untertyp)
        {
            switch (gruppe)
            {
                case 1: return (untertyp == 2 ? DbWerte.BAUTEILART_INNENWAND : DbWerte.BAUTEILART_AUSSENWAND, true);
                case 2: return (DbWerte.BAUTEILART_TUER, true);
                case 3: return (DbWerte.BAUTEILART_FENSTER, true);
                case 4: return (DbWerte.BAUTEILART_DECKE, true);
                case 5: return (DbWerte.BAUTEILART_DACH, true);
                case 15:
                case 16:
                    if (untertyp.GetValueOrDefault() == 0) return (DbWerte.BAUTEILART_AUSSENWAND, true);
                    return (DbWerte.BAUTEILART_SONSTIGES, false);
                case 14:
                case 17:
                case 19:
                case 29:
                case 30: return (DbWerte.BAUTEILART_SONSTIGES, true);
                default: return (DbWerte.BAUTEILART_SONSTIGES, false);
            }
        }

        /// <summary>
        /// <b>Klasse eines Baustoffs aus dem aufbereiteten Namen</b> (Fachbegriffe, keine Produktnamen); gilt
        /// nur, wenn die Klasse zur Gruppe gehört. Reihenfolge entscheidet (Stahlbeton vor Beton vor Stahl).
        /// </summary>
        public static readonly IReadOnlyList<(string Wort, string Klasse)> KLASSE_AUS_NAME = new[]
        {
            ("stahlbeton", "STAHLBETON"), ("porenbeton", "PORENBETON"), ("leichtbeton", "LEICHTBETON"),
            ("kalksand", "KALKSANDSTEIN"), ("ziegel", "ZIEGEL"), ("naturstein", "NATURSTEIN"), ("sandstein", "NATURSTEIN"),
            ("beton", "NORMALBETON"),
            ("zementestrich", "ZEMENTESTRICH"), ("anhydrit", "CALCIUMSULFATESTRICH"), ("calciumsulfat", "CALCIUMSULFATESTRICH"),
            ("gussasphalt", "GUSSASPHALT"), ("trockenestrich", "TROCKENESTRICH"),
            ("kalkputz", "KALKPUTZ"), ("kalkzementputz", "ZEMENTPUTZ"), ("zementputz", "ZEMENTPUTZ"), ("gipsputz", "GIPSPUTZ"),
            ("lehmputz", "LEHMPUTZ"), ("daemmputz", "DAEMMPUTZ"), ("moertel", "MOERTEL"),
            ("mineralwolle", "MINERALWOLLE"), ("steinwolle", "MINERALWOLLE"), ("glaswolle", "MINERALWOLLE"),
            ("polystyrol", "POLYSTYROL"), ("eps", "POLYSTYROL"), ("xps", "POLYSTYROL"),
            ("polyurethan", "POLYURETHAN"), ("pur", "POLYURETHAN"), ("pir", "POLYURETHAN"),
            ("holzfaser", "HOLZFASER"), ("hanf", "NATURFASER"), ("flachs", "NATURFASER"), ("zellulose", "NATURFASER"),
            ("schaumglas", "SCHAUMGLAS"), ("vakuum", "VAKUUMDAEMMUNG"),
            ("spanplatte", "HOLZWERKSTOFF"), ("osb", "HOLZWERKSTOFF"), ("sperrholz", "HOLZWERKSTOFF"), ("holzwerkstoff", "HOLZWERKSTOFF"),
            ("holz", "VOLLHOLZ"),
            ("gipskarton", "GIPSPLATTE"), ("gipsfaser", "GIPSPLATTE"), ("zementplatte", "ZEMENTPLATTE"),
            ("fliese", "KERAMIK"), ("keramik", "KERAMIK"), ("parkett", "PARKETT"), ("teppich", "TEXTILBELAG"),
            ("linoleum", "KUNSTSTOFFBELAG"), ("pvc", "KUNSTSTOFFBELAG"),
            ("bitumen", "BITUMEN"), ("dampfbremse", "DAMPFBREMSE"), ("dampfsperre", "DAMPFBREMSE"),
            ("stahl", "STAHL"), ("aluminium", "ALUMINIUM"), ("kupfer", "KUPFER"), ("glas", "GLAS"),
            ("kies", "NICHTBINDIG"), ("sand", "NICHTBINDIG"), ("lehm", "BINDIG"), ("ton", "BINDIG"), ("fels", "FELS"),
            ("luft", "LUFT")
        };

        // ---- Abbildung der Felder ---------------------------------------------------------

        /// <summary>Art eines Quellfelds.</summary>
        internal enum Art { Zahl, Ganz, Schalter, Text, Prozent }

        /// <summary>Zielspalte ← Quellspalte (Zusatzfelder; Einheiten offener Felder: wie Quelle).</summary>
        internal static readonly (string Ziel, string Quelle, Art Art)[] BAUSTOFF_FELDER =
        {
            ("Langtext", "LongDesc", Art.Text), ("Infotext", "InfoDesc", Art.Text),
            ("Lambda_Bemessung", "DesignValueThermalConductivity", Art.Zahl),
            ("Mue_Min", "LowerDiffusionResistanceCoefficient", Art.Zahl), ("Mue_Max", "UpperDiffusionResistanceCoefficient", Art.Zahl),
            ("Baustoffart_Code", "MaterialType", Art.Ganz), ("Oberflaechenart_Code", "MaterialSurfaceType", Art.Ganz),
            ("Dampfdicht", "IsVapourDiffusionTight", Art.Schalter), ("Waermereflektierend", "IsHeatReflective", Art.Schalter),
            ("Kapillaraktiv", "IsCapillary", Art.Schalter), ("Feuchtegehalt", "MoistureContent", Art.Zahl),
            ("Temperatur_Max", "TemperatureMax", Art.Zahl), ("Rauheit", "Roughness", Art.Zahl),
            ("Laengenausdehnung", "LinearExtensionCoefficient", Art.Zahl),
            ("OI3_AP", "OI3AP", Art.Zahl), ("OI3_PEI", "OI3PEI", Art.Zahl), ("OI3_GWP", "OI3GWP", Art.Zahl)
        };

        /// <summary>Siehe <see cref="BAUSTOFF_FELDER"/>.</summary>
        internal static readonly (string Ziel, string Quelle, Art Art)[] AUFBAU_FELDER =
        {
            ("Infotext", "InfoDesc", Art.Text), ("Dicke_Quelle", "Thickness", Art.Zahl), ("U_Quelle", "UValue", Art.Zahl),
            ("R_Quelle", "RValue", Art.Zahl), ("Rsi", "InternalCoefficientOfHeatTransfer", Art.Zahl),
            ("Rse", "ExternalCoefficientOfHeatTransfer", Art.Zahl), ("Hinterlueftet", "IsVentilated", Art.Schalter),
            ("Rahmenanteil", "FractionOfFrame", Art.Prozent), ("g_Wert", "GValue", Art.Zahl), ("Ug", "GlazingUValue", Art.Zahl),
            ("Uf", "FrameUValue", Art.Zahl), ("Rahmenbreite", "FrameWidth", Art.Zahl),
            ("Verschattungsfaktor", "ShadingCoefficient", Art.Zahl), ("Psi_Gesamt", "PsiValue", Art.Zahl),
            ("Psi_System", "PsiSystemValue", Art.Zahl), ("Psi_Geometrie", "PsiGeometryValue", Art.Zahl),
            ("OI3_Rahmen_AP", "FrameOI3AP", Art.Zahl), ("OI3_Rahmen_PEI", "FrameOI3PEI", Art.Zahl),
            ("OI3_Rahmen_GWP", "FrameOI3GWP", Art.Zahl)
        };

        /// <summary>Siehe <see cref="BAUSTOFF_FELDER"/>.</summary>
        internal static readonly (string Ziel, string Quelle, Art Art)[] SCHICHT_FELDER =
        {
            ("Langtext", "LongDesc", Art.Text), ("Schichtart_Code", "LayerType", Art.Ganz), ("Estrichart_Code", "ScreedType", Art.Ganz),
            ("Belueftungsart_Code", "VentilatedType", Art.Ganz), ("Dickenart_Code", "ThicknessType", Art.Ganz),
            ("R_Quelle", "RValue", Art.Zahl), ("Emissionsgrad", "Emissivity", Art.Zahl),
            ("Mue_Min", "LowerDiffusionResistanceCoefficient", Art.Zahl), ("Mue_Max", "UpperDiffusionResistanceCoefficient", Art.Zahl),
            ("Lambda_Bemessung", "DesignValueThermalConductivity", Art.Zahl), ("Rsi", "InternalCoefficientOfHeatTransfer", Art.Zahl),
            ("Rse", "ExternalCoefficientOfHeatTransfer", Art.Zahl), ("Rauheit", "Roughness", Art.Zahl),
            ("Elastizitaet", "ElasticConstant", Art.Zahl), ("Laengenausdehnung", "LinearExtensionCoefficient", Art.Zahl),
            ("Hinterlueftet", "IsVentilated", Art.Schalter), ("Kapillaraktiv", "IsCapillary", Art.Schalter),
            ("Dampfdicht", "IsVapourDiffusionTight", Art.Schalter), ("Feuchtegehalt", "MoistureContent", Art.Zahl),
            ("OI3_AP", "OI3AP", Art.Zahl), ("OI3_PEI", "OI3PEI", Art.Zahl), ("OI3_GWP", "OI3GWP", Art.Zahl),
            ("EN6946_Balkenbreite", "EN6946BeamWidth", Art.Zahl), ("EN6946_Gefachbreite", "EN6946GapWidth", Art.Zahl),
            ("EN6946_Versatz", "EN6946OffsetWidth", Art.Zahl), ("EN6946_Drehung", "EN6946Rotation", Art.Zahl)
        };

        // ---- Planung -----------------------------------------------------------------------

        /// <summary>Ein geplanter Satz (Baustoff, Aufbau, Abschnitt oder Schicht).</summary>
        internal sealed class Satz
        {
            internal string Kennung;
            internal string Schluessel;
            internal string Gruppe;
            internal int? Quellcode;
            internal bool Haustechnik;
            internal bool Luftdurchlass;
            internal Dictionary<string, object> Werte = new Dictionary<string, object>(StringComparer.Ordinal);
            internal string Aktion; // NEU, AKTUALISIEREN, ERGAENZEN, AUS (übergangen)
            internal long ZielId;
            internal Satz Vertreter;          // Schicht → Baustoff, Schicht/Abschnitt → Aufbau
            internal Satz Abschnitt;          // Schicht → Abschnitt
            internal BauteilkatalogBestand Bestand;
            internal List<Satz> Schichten = new List<Satz>();
            internal double? Lambda => W("Lambda");
            internal double? Rho => W("Rho");
            internal double? Cp => W("cp");
            internal double? W(string s) => Werte.TryGetValue(s, out object o) && o is double d ? d : (double?)null;
        }

        internal const string NEU = "NEU", AKTUALISIEREN = "AKTUALISIEREN", ERGAENZEN = "ERGAENZEN";

        /// <summary>Der Plan: Sätze in Schreibreihenfolge samt Vorschau.</summary>
        internal sealed class Plan
        {
            internal BauteilkatalogVorschau Vorschau = new BauteilkatalogVorschau();
            internal List<Satz> Baustoffe = new List<Satz>();
            internal List<Satz> Aufbauten = new List<Satz>();
            internal List<Satz> Abschnitte = new List<Satz>();
            internal List<Satz> Schichten = new List<Satz>();
        }

        /// <summary>Gleich nach der Dublettenregel: beide leer oder beide da und relativ höchstens <paramref name="tol"/> auseinander.</summary>
        public static bool Nah(double? a, double? b, double tol) =>
            (!a.HasValue && !b.HasValue) ||
            (a.HasValue && b.HasValue && Math.Abs(a.Value - b.Value) <= tol * Math.Max(Math.Abs(a.Value), Math.Abs(b.Value)) + 1e-12);

        /// <summary>λ, ρ, c nach der Dublettenregel gleich.</summary>
        public static bool WerteGleich(double? l1, double? r1, double? c1, double? l2, double? r2, double? c2) =>
            Nah(l1, l2, TOL_LAMBDA) && Nah(r1, r2, TOL_RHO) && Nah(c1, c2, TOL_CP);

        /// <summary>c nach Regel 2: 0 → kein Wert, ≤ 10 → kJ, × 1 000; sonst J.</summary>
        public static double? CpNormieren(double? c)
        {
            if (!c.HasValue || c.Value <= 0) return null;
            return c.Value <= CP_KJ_GRENZE ? c.Value * 1000.0 : c.Value;
        }

        /// <summary>Die Klasse eines Baustoffs aus seinem aufbereiteten Namen, passend zur Gruppe; sonst <c>null</c>.</summary>
        public static string KlasseAusName(string schluessel, string gruppe)
        {
            if (string.IsNullOrEmpty(schluessel)) return null;
            string[] woerter = schluessel.Split(' ');
            foreach ((string wort, string klasse) in KLASSE_AUS_NAME)
            {
                bool trifft = wort.Length <= 4 ? woerter.Contains(wort) : schluessel.Contains(wort);
                if (!trifft) continue;
                return Bauteilkatalog.GruppeDerBaustoffklasse(klasse) == gruppe ? klasse : null;
            }
            return null;
        }

        /// <summary>Die Quellangabe aus dem jüngsten Zeitstempel (Regel 11).</summary>
        internal static string QuelleAus(DateTime? stand) =>
            stand.HasValue ? QUELLE_PRAEFIX + ", Datenstand " + stand.Value.ToString("yyyy-MM", CultureInfo.InvariantCulture) : QUELLE_PRAEFIX;

        /// <summary>
        /// <b>Plant den Import</b> ohne Datenbank: Regeln 1–12, Zusammenführung gegen <paramref name="stand"/>,
        /// Kennzeichen, Stichprobe der Richtung. Die Vorschau des Plans trägt alle Zahlen.
        /// </summary>
        internal static Plan Planen(BauteilkatalogQuelle q, BauteilkatalogStand stand)
        {
            var p = new Plan();
            BauteilkatalogVorschau v = p.Vorschau;
            if (q.Abgelehnt != null) { v.Abgelehnt = q.Abgelehnt; return p; }
            v.Protokoll.AddRange(q.Hinweise);
            stand ??= new BauteilkatalogStand();

            BauteilkatalogTabelle tb = q.Tabelle(BauteilkatalogQuelle.DATEI_BAUSTOFFE);
            BauteilkatalogTabelle ta = q.Tabelle(BauteilkatalogQuelle.DATEI_AUFBAUTEN);
            BauteilkatalogTabelle ts = q.Tabelle(BauteilkatalogQuelle.DATEI_SCHICHTEN);
            BauteilkatalogTabelle tx = q.Tabelle(BauteilkatalogQuelle.DATEI_ABSCHNITTE);
            BauteilkatalogTabelle tf = q.Tabelle(BauteilkatalogQuelle.DATEI_FENSTER);
            BauteilkatalogTabelle tz = q.Tabelle(BauteilkatalogQuelle.DATEI_BASIS);

            DateTime? jung = null;
            foreach (BauteilkatalogTabelle t in new[] { tb, ta, ts })
                foreach (string[] z in t.Zeilen)
                {
                    DateTime? d = t.Datum(z, "StampEdit") ?? t.Datum(z, "StampCreate");
                    if (d.HasValue && d.Value.Year >= 1990 && (!jung.HasValue || d.Value > jung.Value)) jung = d;
                }
            v.Quelle = QuelleAus(jung);
            v.Protokoll.Add("Quelle: " + v.Quelle);
            var zaehler = new Dictionary<string, int>(StringComparer.Ordinal);
            void Zaehle(string text) => zaehler[text] = zaehler.TryGetValue(text, out int n) ? n + 1 : 1;

            // Basis: Baustoff → Stoffgruppencodes, Aufbau-Basiszeile → (GroupType, SubType).
            var basisAufbau = new Dictionary<string, (int?, int?)>(StringComparer.Ordinal);
            var basisStoff = new Dictionary<string, Dictionary<int, int>>(StringComparer.Ordinal);
            foreach (string[] z in tz.Zeilen)
            {
                string uid = tz.Kennung(z, "UId");
                int? g = tz.Ganz(z, "GroupType");
                string mat = tz.Kennung(z, "MaterialUId");
                if (uid != null) basisAufbau[uid] = (g, tz.Ganz(z, "SubType"));
                if (mat != null && g.HasValue)
                {
                    if (!basisStoff.TryGetValue(mat, out Dictionary<int, int> zahl)) basisStoff[mat] = zahl = new Dictionary<int, int>();
                    zahl[g.Value] = zahl.TryGetValue(g.Value, out int n) ? n + 1 : 1;
                }
            }

            // ---- Baustoffe ----
            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            var vertreter = new Dictionary<(string, string), List<Satz>>();
            int widerspruch = 0;
            foreach (string[] z in tb.Zeilen)
            {
                string k = tb.Kennung(z, "UId");
                if (k == null) { v.Baustoffe.Ablehnen("ohne Kennung"); continue; }
                if (!gesehen.Add(k)) { v.Baustoffe.Ablehnen("Kennung doppelt"); continue; }
                string name = tb.Text(z, "ShortDesc") ?? tb.Text(z, "LongDesc");
                if (name == null) { v.Baustoffe.Ablehnen("ohne Namen"); continue; }

                int? code = tb.Ganz(z, "MaterialGroupType");
                string wo = "Baustoffe.csv MaterialGroupType";
                // 0 und leer gehören nicht zur Tafel: über die Basiszeilen (TcBasis) nachtragen (E134).
                if (code.GetValueOrDefault() == 0 && basisStoff.TryGetValue(k, out Dictionary<int, int> codes))
                {
                    if (codes.Count > 1) widerspruch++;
                    code = codes.OrderByDescending(c => c.Value).ThenBy(c => c.Key).First().Key;
                    wo = "Basis_Auszug.csv GroupType";
                }
                string schluessel = Baustoffabgleich.Schluessel(name);
                (string gruppe, string klasse, bool haustechnik) = GliederungAusCode(code, schluessel, out bool gedeutet);
                if (!gedeutet) v.Code(wo, code.Value.ToString(CultureInfo.InvariantCulture));

                double? lambda = tb.Zahl(z, "HeatConductivity");
                if (lambda.HasValue && lambda.Value <= 0) { lambda = null; Zaehle("Baustoff: λ ≤ 0 als kein Wert gelesen"); }
                double? rho = tb.Zahl(z, "Density");
                if (rho <= 0) rho = null;
                (double? lambdaGeprueft, LambdaBefund befund) = LambdaPruefen(lambda, rho);
                if (befund == LambdaBefund.Abgelehnt)
                {
                    v.Baustoffe.Ablehnen(GRUND_LAMBDA);
                    continue;
                }
                if (befund == LambdaBefund.Skaliert)
                {
                    v.LambdaSkaliert++;
                    v.Protokoll.Add("λ als Skalenfehler ÷ 1 000 (" + Zahl3(lambda.Value) + " → " + Zahl3(lambdaGeprueft.Value) +
                                    " W/(m·K)): Baustoff " + k + " „" + Kuerzen(name, 80) + "“");
                }
                lambda = lambdaGeprueft;
                double? cRoh = tb.Zahl(z, "HeatCapacity");
                double? cp = CpNormieren(cRoh);
                if (cRoh > 0 && cRoh <= CP_KJ_GRENZE) Zaehle("Baustoff: c in kJ/(kg·K) gelesen, × 1 000");

                var s = new Satz { Kennung = k, Schluessel = schluessel, Gruppe = gruppe, Quellcode = code, Haustechnik = haustechnik };
                s.Werte["Bezeichner"] = Kuerzen(name, 80);
                s.Werte["Gruppe"] = Bauteilkatalog.StoffgruppeAusGruppe(gruppe);
                s.Werte["Lambda"] = lambda;
                s.Werte["Rho"] = rho;
                s.Werte["cp"] = cp;
                s.Werte[BauteilkatalogSchema.SPALTE_GRUPPE] = gruppe;
                s.Werte[BauteilkatalogSchema.SPALTE_KLASSE] = klasse;
                Felder(s, tb, z, BAUSTOFF_FELDER, BauteilkatalogSchema.TAB_BAUSTOFF_STAMM, Zaehle);
                s.Werte["Quelle"] = v.Quelle;
                s.Werte["Quellkennung"] = k;

                var schl = (schluessel, gruppe);
                if (!vertreter.TryGetValue(schl, out List<Satz> liste)) vertreter[schl] = liste = new List<Satz>();
                Satz gleich = liste.FirstOrDefault(x => WerteGleich(x.Lambda, x.Rho, x.Cp, lambda, rho, cp));
                if (gleich != null)
                {
                    foreach (KeyValuePair<string, object> w in s.Werte)
                        if (gleich.Werte.TryGetValue(w.Key, out object alt) && alt == null) gleich.Werte[w.Key] = w.Value;
                    v.Baustoffe.InQuelle++;
                    continue;
                }
                liste.Add(s);
                p.Baustoffe.Add(s);
            }
            if (widerspruch > 0)
                v.Protokoll.Add("Baustoffe mit widersprüchlicher Stoffgruppe in Basis_Auszug (häufigste gewählt): " + Zahl(widerspruch));

            // Gegen den Katalog: Quellkennung, dann Dublettenregel.
            Dictionary<string, BauteilkatalogBestand> nachKennung = NachKennung(stand.Baustoffe);
            var nachName = new Dictionary<(string, string), List<BauteilkatalogBestand>>();
            foreach (BauteilkatalogBestand b in stand.Baustoffe)
            {
                var schl = (Baustoffabgleich.Schluessel(b.Bezeichner ?? ""), b.Gruppe ?? Bauteilkatalog.GRUPPE_SONSTIGE);
                if (!nachName.TryGetValue(schl, out List<BauteilkatalogBestand> l)) nachName[schl] = l = new List<BauteilkatalogBestand>();
                l.Add(b);
            }
            foreach (Satz s in p.Baustoffe)
            {
                if (nachKennung.TryGetValue(s.Kennung, out BauteilkatalogBestand b))
                {
                    s.Bestand = b;
                    s.Aktion = b.Herkunft == BauteilkatalogSchema.HERKUNFT_IMPORT ? AKTUALISIEREN : ERGAENZEN;
                    v.Baustoffe.Wiedererkannt++;
                    continue;
                }
                if (nachName.TryGetValue((s.Schluessel, s.Gruppe), out List<BauteilkatalogBestand> kandidaten))
                {
                    b = kandidaten.FirstOrDefault(x => WerteGleich(x.Lambda, x.Rho, x.Cp, s.Lambda, s.Rho, s.Cp));
                    if (b != null)
                    {
                        s.Bestand = b;
                        s.Aktion = ERGAENZEN;
                        v.Baustoffe.MitKatalog++;
                        continue;
                    }
                }
                s.Aktion = NEU;
                v.Baustoffe.Neu++;
            }

            // ---- Aufbauten ----
            var fenster = new Dictionary<string, (double? Ug, double? G)>(StringComparer.Ordinal);
            if (tf != null)
                foreach (string[] z in tf.Zeilen)
                {
                    string d = tf.Kennung(z, "DimensionUId");
                    if (d == null) continue;
                    fenster.TryGetValue(d, out (double? Ug, double? G) alt);
                    fenster[d] = (alt.Ug ?? tf.Zahl(z, "GlazingUValue"), alt.G ?? tf.Zahl(z, "GlazingGValue"));
                }
            Dictionary<string, BauteilkatalogBestand> aufbauNachKennung = NachKennung(stand.Aufbauten);
            var aufbauten = new Dictionary<string, Satz>(StringComparer.Ordinal);
            gesehen.Clear();
            foreach (string[] z in ta.Zeilen)
            {
                string k = ta.Kennung(z, "UId");
                if (k == null) { v.Aufbauten.Ablehnen("ohne Kennung"); continue; }
                if (!gesehen.Add(k)) { v.Aufbauten.Ablehnen("Kennung doppelt"); continue; }
                string name = ta.Text(z, "ShortDesc") ?? ta.Text(z, "LongDesc");
                if (name == null) { v.Aufbauten.Ablehnen("ohne Namen"); continue; }
                string basis = ta.Kennung(z, "BasisUId");
                string art = DbWerte.BAUTEILART_SONSTIGES;
                bool haustechnik = false, luftdurchlass = false;
                if (basis != null && basisAufbau.TryGetValue(basis, out (int? G, int? U) bt))
                {
                    (string a, bool gedeutet) = BauteilartAusBasis(bt.G, bt.U);
                    art = a;
                    haustechnik = bt.G.HasValue && HAUSTECHNIK_CODES.Contains(bt.G.Value);
                    luftdurchlass = bt.G == CODE_LUFTDURCHLASS;
                    if (!gedeutet)
                        v.Code("Basis_Auszug.csv GroupType|SubType", (bt.G?.ToString(CultureInfo.InvariantCulture) ?? "") + "|" +
                                                                     (bt.U?.ToString(CultureInfo.InvariantCulture) ?? ""));
                }
                else Zaehle("Aufbau ohne Basiszeile, Bauteilart Sonstiges");

                var s = new Satz { Kennung = k, Haustechnik = haustechnik, Luftdurchlass = luftdurchlass };
                (string gruppe, string klasse) = haustechnik
                    ? (Bauteilkatalog.GRUPPE_SONSTIGE, KlasseHaustechnik(false))
                    : Bauteilkatalog.GliederungAusBauteilart(art, null);
                s.Werte["Bezeichner"] = Kuerzen(name, 80);
                s.Werte["Beschreibung"] = Kuerzen(ta.Text(z, "LongDesc"), 250);
                s.Werte["Bauteilart"] = art;
                s.Werte[BauteilkatalogSchema.SPALTE_GRUPPE] = gruppe;
                s.Werte[BauteilkatalogSchema.SPALTE_KLASSE] = klasse;
                Felder(s, ta, z, AUFBAU_FELDER, BauteilkatalogSchema.TAB_AUFBAU_STAMM, Zaehle);
                if (fenster.TryGetValue(k, out (double? Ug, double? G) fl))
                {
                    if (s.Werte["Ug"] == null) s.Werte["Ug"] = Bereinigen(BauteilkatalogSchema.TAB_AUFBAU_STAMM, "Ug", fl.Ug, Zaehle);
                    if (s.Werte["g_Wert"] == null) s.Werte["g_Wert"] = Bereinigen(BauteilkatalogSchema.TAB_AUFBAU_STAMM, "g_Wert", fl.G, Zaehle);
                }
                DateTime? von = ta.Datum(z, "ValidStartDate"), bis = ta.Datum(z, "ValidEndDate");
                s.Werte["Gueltigkeit"] = von.HasValue || bis.HasValue
                    ? (von?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "") + " bis " + (bis?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "")
                    : null;
                s.Werte["Quelle"] = v.Quelle;
                s.Werte["Quellkennung"] = k;
                if (aufbauNachKennung.TryGetValue(k, out BauteilkatalogBestand b))
                {
                    s.Bestand = b;
                    s.Aktion = b.Herkunft == BauteilkatalogSchema.HERKUNFT_IMPORT ? AKTUALISIEREN : ERGAENZEN;
                    v.Aufbauten.Wiedererkannt++;
                }
                else { s.Aktion = NEU; v.Aufbauten.Neu++; }
                aufbauten[k] = s;
                p.Aufbauten.Add(s);
            }

            // ---- Abschnitte ----
            var abschnitte = new Dictionary<string, Satz>(StringComparer.Ordinal);
            if (tx != null)
            {
                var nummern = new HashSet<(string, int)>();
                foreach (string[] z in tx.Zeilen)
                {
                    string k = tx.Kennung(z, "UId");
                    string a = tx.Kennung(z, "DimensionUId");
                    int? nr = tx.Ganz(z, "SectionNum");
                    if (k == null) { v.Abschnitte.Ablehnen("ohne Kennung"); continue; }
                    if (a == null || !aufbauten.TryGetValue(a, out Satz auf)) { v.Abschnitte.Ablehnen("Aufbau fehlt oder abgelehnt"); continue; }
                    if (!nr.HasValue || nr.Value < 1 || !nummern.Add((a, nr.Value))) { v.Abschnitte.Ablehnen("Abschnittsnummer fehlt oder doppelt"); continue; }
                    if (auf.Aktion == ERGAENZEN) { v.Abschnitte.Wiedererkannt++; continue; }
                    var s = new Satz { Kennung = k, Vertreter = auf };
                    double? anteil = tx.Zahl(z, "SectionShareValve");
                    s.Werte["Nummer"] = nr.Value;
                    s.Werte["Flaechenanteil"] = anteil.HasValue && anteil.Value >= 0 && anteil.Value <= 100 ? anteil.Value / 100.0 : (double?)null;
                    s.Werte["Text"] = Kuerzen(tx.Text(z, "LongDesc") ?? tx.Text(z, "ShortDesc"), 250);
                    s.Werte["Quellkennung"] = k;
                    if (auf.Aktion == AKTUALISIEREN) v.Abschnitte.Wiedererkannt++; else v.Abschnitte.Neu++;
                    abschnitte[k] = s;
                    p.Abschnitte.Add(s);
                }
            }

            // ---- Schichten ----
            var gruppen = new Dictionary<(string, string), List<(int Seq, int Pos, Satz S)>>();
            gesehen.Clear();
            int pos = 0;
            foreach (string[] z in ts.Zeilen)
            {
                pos++;
                string k = ts.Kennung(z, "UId");
                string a = ts.Kennung(z, "DimensionUId");
                if (k == null) { v.Schichten.Ablehnen("ohne Kennung"); continue; }
                if (!gesehen.Add(k)) { v.Schichten.Ablehnen("Kennung doppelt"); continue; }
                if (a == null || !aufbauten.TryGetValue(a, out Satz auf)) { v.Schichten.Ablehnen("Aufbau fehlt oder abgelehnt"); continue; }
                string ab = ts.Kennung(z, "LayerSectionUId");
                Satz abschnitt = null;
                if (ab != null && !abschnitte.TryGetValue(ab, out abschnitt) && auf.Aktion != ERGAENZEN)
                {
                    v.Schichten.Ablehnen("Abschnitt fehlt oder abgelehnt");
                    continue;
                }
                if (auf.Aktion == ERGAENZEN) { v.Schichten.Wiedererkannt++; continue; }

                string name = ts.Text(z, "ShortDesc") ?? ts.Text(z, "LongDesc") ?? "";
                int? code = ts.Ganz(z, "MaterialGroupType");
                string schluessel = Baustoffabgleich.Schluessel(name);
                (string gruppe, _, _) = GliederungAusCode(code, schluessel, out bool gedeutet);
                if (!gedeutet) v.Code("Schichten.csv MaterialGroupType", code.Value.ToString(CultureInfo.InvariantCulture));
                double? lambda = ts.Zahl(z, "ThermalConductivity");
                if (lambda <= 0) lambda = null;
                double? rho = ts.Zahl(z, "Density");
                if (rho <= 0) rho = null;
                (double? lambdaGeprueft, LambdaBefund befund) = LambdaPruefen(lambda, rho);
                if (befund == LambdaBefund.Abgelehnt) Zaehle("Schicht: λ > 10 ohne Metall und ohne Skalenfehler verworfen");
                if (befund == LambdaBefund.Skaliert)
                {
                    Zaehle("Schicht: λ als Skalenfehler ÷ 1 000");
                    v.Protokoll.Add("λ als Skalenfehler ÷ 1 000 (" + Zahl3(lambda.Value) + " → " + Zahl3(lambdaGeprueft.Value) +
                                    " W/(m·K)): Schicht " + k);
                }
                lambda = lambdaGeprueft;
                double? cRoh = ts.Zahl(z, "HeatCapacity");
                if (cRoh > 0 && cRoh <= CP_KJ_GRENZE) Zaehle("Schicht: c in kJ/(kg·K) gelesen, × 1 000");
                double? dicke = ts.Zahl(z, "Thickness");
                if (!dicke.HasValue || dicke.Value < 0) { dicke = 0; Zaehle("Schicht ohne Dicke (0 m, R der Quelle bleibt)"); }

                var s = new Satz { Kennung = k, Schluessel = schluessel, Gruppe = gruppe, Vertreter = auf, Abschnitt = abschnitt };
                s.Werte["Dicke"] = dicke.Value;
                s.Werte["IstLuftschicht"] = Baustoffabgleich.Luftschichtnamen.Contains(schluessel) ? 1 : 0;
                s.Werte["Lambda"] = lambda;
                s.Werte["Rho"] = rho;
                s.Werte["cp"] = CpNormieren(cRoh);
                s.Werte["Kurztext"] = Kuerzen(name, 80);
                s.Werte[BauteilkatalogSchema.SPALTE_GRUPPE] = gruppe;
                Felder(s, ts, z, SCHICHT_FELDER, BauteilkatalogSchema.TAB_SCHICHT_STAMM, Zaehle);
                s.Werte["Quellkennung"] = k;

                // Regel 9: Name trifft einen importierten Baustoff derselben Gruppe (N1–N3).
                if (vertreter.TryGetValue((schluessel, gruppe), out List<Satz> treffer) && treffer.Count > 0)
                {
                    Satz bs = treffer[0];
                    s.Abschnitt = abschnitt;
                    s.Werte["__Baustoff"] = bs;
                    v.SchichtenMitBaustoff++;
                    if (!lambda.HasValue && bs.Lambda.HasValue)
                    {
                        s.Werte["Lambda"] = bs.Lambda;
                        s.Werte["Rho"] = (double?)s.Werte["Rho"] ?? bs.Rho;
                        s.Werte["cp"] = (double?)s.Werte["cp"] ?? bs.Cp;
                        Zaehle("Schicht: λ über den Namensabgleich gefüllt");
                    }
                }
                var gk = (a, ab ?? "");
                if (!gruppen.TryGetValue(gk, out List<(int, int, Satz)> l)) gruppen[gk] = l = new List<(int, int, Satz)>();
                l.Add((ts.Ganz(z, "SeqNum") ?? int.MaxValue, pos, s));
                if (auf.Aktion == AKTUALISIEREN) v.Schichten.Wiedererkannt++; else v.Schichten.Neu++;
            }
            int luecken = 0;
            foreach (KeyValuePair<(string, string), List<(int Seq, int Pos, Satz S)>> g in gruppen)
            {
                List<(int Seq, int Pos, Satz S)> geordnet = g.Value.OrderBy(x => x.Seq).ThenBy(x => x.Pos).ToList();
                bool luecke = false;
                for (int i = 0; i < geordnet.Count; i++)
                {
                    if (geordnet[i].Seq != i + 1) luecke = true;
                    geordnet[i].S.Werte["Reihenfolge"] = i + 1;
                    geordnet[i].S.Vertreter.Schichten.Add(geordnet[i].S);
                    p.Schichten.Add(geordnet[i].S);
                }
                if (luecke)
                {
                    luecken++;
                    v.Protokoll.Add("Schichtfolge neu nummeriert (Lücke oder Doppel in SeqNum): Aufbau " + g.Key.Item1 +
                                    (g.Key.Item2.Length > 0 ? ", Abschnitt " + g.Key.Item2 : ""));
                }
            }
            if (luecken > 0) v.Protokoll.Add("Schichtgruppen neu nummeriert: " + Zahl(luecken));

            // ---- Kennzeichen (allein der Kern) ----
            foreach (Satz a in p.Aufbauten)
            {
                if (a.Aktion == ERGAENZEN) continue;
                var m = new BauteilaufbauModel { Bauteilart = (string)a.Werte["Bauteilart"] };
                m.Zusatz[BauteilkatalogSchema.SPALTE_U_QUELLE] = a.Werte[BauteilkatalogSchema.SPALTE_U_QUELLE];
                // BK-2b: mit Dicke, ρ und c_p - das Kennzeichen prüft auch die Bänder des Rechenwegs.
                foreach (Satz s in a.Schichten)
                    m.Schichten.Add(new BauteilschichtModel
                    {
                        IstLuftschicht = (int)s.Werte["IstLuftschicht"] == 1, Lambda = s.Lambda,
                        Dicke = s.W("Dicke") ?? 0.0, Rho = s.W("Rho"), Cp = s.W("cp")
                    });
                // E134: ein Luftdurchlass trägt keine Bauphysik der Hülle - gekennzeichnet auch mit Schichten.
                bool ungeeignet = Bauteilkatalog.MeistUngeeignet(m) || a.Luftdurchlass;
                a.Werte[BauteilkatalogSchema.SPALTE_UNGEEIGNET] = ungeeignet ? 1 : 0;
                if (ungeeignet) v.Gekennzeichnet++;
            }

            // ---- Gliederung der geplanten Sätze (Zählungen für die Vorschau) ----
            foreach (Satz b in p.Baustoffe)
            {
                Gliederung(v.BaustoffGliederung, b);
                if (b.Haustechnik) v.HaustechnikBaustoffe++;
            }
            foreach (Satz a in p.Aufbauten)
            {
                Gliederung(v.AufbauGliederung, a);
                if (a.Haustechnik) v.HaustechnikAufbauten++;
                if (a.Luftdurchlass) v.Luftdurchlaesse++;
            }

            Richtungsprobe(p);
            foreach (KeyValuePair<string, int> z in zaehler.OrderBy(x => x.Key, StringComparer.Ordinal))
                v.Protokoll.Add(z.Key + ": " + Zahl(z.Value));
            v.Protokoll.Add("Einheiten wie Quelle: Feuchtegehalt, Rauheit, Elastizität, Längenausdehnung, OI3 (unverändert übernommen)");
            return p;
        }

        private static void Gliederung(SortedDictionary<string, int> ziel, Satz s)
        {
            string schl = (s.Werte.TryGetValue(BauteilkatalogSchema.SPALTE_GRUPPE, out object g) ? g as string : null) + "/" +
                          ((s.Werte.TryGetValue(BauteilkatalogSchema.SPALTE_KLASSE, out object k) ? k as string : null) ?? "-");
            ziel[schl] = ziel.TryGetValue(schl, out int n) ? n + 1 : 1;
        }

        /// <summary>Regel 7: Dämmlage hinter tragender Schicht (Außenwände) und U aus Schichten gegen U der Quelle.</summary>
        private static void Richtungsprobe(Plan p)
        {
            BauteilkatalogRichtungsprobe r = p.Vorschau.Richtung;
            var verhaeltnisse = new List<double>();
            foreach (Satz a in p.Aufbauten)
            {
                if (a.Aktion == ERGAENZEN || a.Schichten.Count == 0) continue;
                List<Satz> folge = a.Schichten.OrderBy(s => (int)s.Werte["Reihenfolge"]).ToList();
                bool homogen = folge.All(s => s.Abschnitt == null);
                if ((string)a.Werte["Bauteilart"] == DbWerte.BAUTEILART_AUSSENWAND && homogen)
                {
                    Satz daemm = folge.Where(s => s.Gruppe == "DAEMMSTOFFE").OrderByDescending(s => (double)s.Werte["Dicke"]).FirstOrDefault();
                    Satz trag = folge.Where(s => s.Gruppe == "MAUERWERK" || s.Gruppe == "BETON").OrderByDescending(s => (double)s.Werte["Dicke"]).FirstOrDefault();
                    if (daemm != null && trag != null)
                    {
                        r.Paare++;
                        if (folge.IndexOf(daemm) > folge.IndexOf(trag)) r.DaemmungHinten++;
                    }
                }
                double? uQuelle = a.W(BauteilkatalogSchema.SPALTE_U_QUELLE);
                if (!homogen || !uQuelle.HasValue) continue;
                double summe = 0;
                bool vollstaendig = true;
                foreach (Satz s in folge)
                {
                    double d = (double)s.Werte["Dicke"];
                    double? rq = s.W("R_Quelle");
                    if (s.Lambda > 0 && d > 0) summe += d / s.Lambda.Value;
                    else if (rq.HasValue) summe += rq.Value;
                    else { vollstaendig = false; break; }
                }
                if (!vollstaendig) continue;
                double rsi = a.W("Rsi") ?? RSI_VORGABE, rse = a.W("Rse") ?? RSE_VORGABE;
                double u = 1.0 / (rsi + summe + rse);
                verhaeltnisse.Add(u / uQuelle.Value);
            }
            r.UGeprueft = verhaeltnisse.Count;
            if (verhaeltnisse.Count > 0)
            {
                verhaeltnisse.Sort();
                int n = verhaeltnisse.Count;
                r.MedianVerhaeltnis = n % 2 == 1 ? verhaeltnisse[n / 2] : 0.5 * (verhaeltnisse[n / 2 - 1] + verhaeltnisse[n / 2]);
                r.AnteilAusserhalb = verhaeltnisse.Count(x => Math.Abs(x - 1.0) > 0.10) / (double)n;
            }
        }

        private static Dictionary<string, BauteilkatalogBestand> NachKennung(IEnumerable<BauteilkatalogBestand> liste)
        {
            var d = new Dictionary<string, BauteilkatalogBestand>(StringComparer.Ordinal);
            foreach (BauteilkatalogBestand b in liste)
            {
                string k = BauteilkatalogQuelle.Kennung(b.Quellkennung);
                if (k != null && !d.ContainsKey(k)) d[k] = b;
            }
            return d;
        }

        /// <summary>Überträgt die Zusatzfelder einer Zeile; Werte außerhalb des Bereichs der Zielspalte fallen weg.</summary>
        private static void Felder(Satz s, BauteilkatalogTabelle t, string[] z, (string Ziel, string Quelle, Art Art)[] felder,
                                   string tabelle, Action<string> zaehle)
        {
            foreach ((string ziel, string quelle, Art art) in felder)
            {
                object w;
                switch (art)
                {
                    case Art.Text: w = t.Text(z, quelle); break;
                    case Art.Ganz: w = t.Ganz(z, quelle); break;
                    case Art.Schalter: w = t.Schalter(z, quelle); break;
                    case Art.Prozent: w = t.Zahl(z, quelle) / 100.0; break;
                    default: w = t.Zahl(z, quelle); break;
                }
                s.Werte[ziel] = Bereinigen(tabelle, ziel, w, zaehle);
            }
        }

        private static readonly Regex LAENGE = new Regex(@"length\(""[^""]+""\) <= (\d+)", RegexOptions.CultureInvariant);

        /// <summary>Hält einen Wert in den Prüfklauseln seiner Zielspalte (Bereich, Länge); sonst <c>null</c> mit Zählung.</summary>
        internal static object Bereinigen(string tabelle, string spalte, object w, Action<string> zaehle)
        {
            if (w == null) return null;
            BauteilkatalogSchema.Spalte sp = BauteilkatalogSchema.SpaltenVon(tabelle).FirstOrDefault(x => x.Name == spalte);
            if (sp == null) return w;
            string typ = sp.Typ;
            bool verworfen = false;
            switch (w)
            {
                case double d:
                    if (typ.Contains(">= 0 AND", StringComparison.Ordinal)) verworfen = d < 0 || d > 1;
                    else if (typ.Contains("> 0)", StringComparison.Ordinal)) verworfen = !(d > 0);
                    else if (typ.Contains(">= 0)", StringComparison.Ordinal)) verworfen = d < 0;
                    break;
                case int i:
                    if (sp.Art == BauteilkatalogSchema.Spaltenart.Ganz) verworfen = i < 0;
                    else if (sp.Art == BauteilkatalogSchema.Spaltenart.Schalter) verworfen = i != 0 && i != 1;
                    break;
                case string text:
                    Match m = LAENGE.Match(typ);
                    if (m.Success) return Kuerzen(text, int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture));
                    break;
            }
            if (!verworfen) return w;
            zaehle(spalte + " außerhalb des Bereichs verworfen");
            return null;
        }

        private static string Kuerzen(string s, int max) => s == null ? null : (s.Length <= max ? s : s.Substring(0, max).TrimEnd());

        private static string Zahl(int n) => n.ToString("N0", CultureInfo.InvariantCulture);

        private static string Zahl3(double d) => d.ToString("0.####", CultureInfo.InvariantCulture);

        // ---- Datenbank ---------------------------------------------------------------------

        /// <summary>Der Abzug des Katalogs im Vorgang <paramref name="v"/>.</summary>
        internal static BauteilkatalogStand StandLesen(DbVorgang v)
        {
            var st = new BauteilkatalogStand();
            DataTable b = v.Lese("SELECT \"ID\", \"Bezeichner\", \"Gruppe\", \"Gruppe_Code\", \"Lambda\", \"Rho\", \"cp\", \"Herkunft\", " +
                                 "\"Quellkennung\", \"Quelle\" FROM \"" + BauteilkatalogSchema.TAB_BAUSTOFF_STAMM + "\"");
            foreach (DataRow r in b.Rows)
                st.Baustoffe.Add(new BauteilkatalogBestand
                {
                    Id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture),
                    Bezeichner = r["Bezeichner"] as string,
                    Gruppe = r["Gruppe_Code"] as string ?? Bauteilkatalog.GruppeAusStoffgruppe(r["Gruppe"] as string) ?? Bauteilkatalog.GRUPPE_SONSTIGE,
                    Lambda = Dbl(r["Lambda"]), Rho = Dbl(r["Rho"]), Cp = Dbl(r["cp"]),
                    Herkunft = r["Herkunft"] as string, Quellkennung = r["Quellkennung"] as string, Quelle = r["Quelle"] as string
                });
            DataTable a = v.Lese("SELECT \"ID\", \"Bezeichner\", \"Herkunft\", \"Quellkennung\", \"Quelle\" FROM \"" +
                                 BauteilkatalogSchema.TAB_AUFBAU_STAMM + "\" WHERE \"Quellkennung\" IS NOT NULL");
            foreach (DataRow r in a.Rows)
                st.Aufbauten.Add(new BauteilkatalogBestand
                {
                    Id = Convert.ToInt64(r["ID"], CultureInfo.InvariantCulture),
                    Bezeichner = r["Bezeichner"] as string,
                    Herkunft = r["Herkunft"] as string, Quellkennung = r["Quellkennung"] as string, Quelle = r["Quelle"] as string
                });
            return st;
        }

        private static double? Dbl(object o) => o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        /// <summary>Für Tests: wird nach jeder Schreibstufe mit ihrem Namen gerufen (wirft er, rollt der Vorgang zurück).</summary>
        internal static Action<string> Schreibhaken = _ => { };

        /// <summary><b>Vorschau</b> des Ordners (oder der gewählten <c>Baustoffe.csv</c>) gegen den Katalog; schreibt nichts.</summary>
        public static BauteilkatalogVorschau Vorschau(string pfad)
        {
            var uhr = Stopwatch.StartNew();
            BauteilkatalogQuelle q = BauteilkatalogQuelle.Lesen(pfad);
            BauteilkatalogStand stand = null;
            if (q.Abgelehnt == null)
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    stand = StandLesen(v);
                    v.Rollback();
                }
            BauteilkatalogVorschau vs = Planen(q, stand).Vorschau;
            vs.DauerMs = uhr.ElapsedMilliseconds;
            return vs;
        }

        /// <summary>
        /// <b>Übernehmen</b>: liest, plant gegen den Katalog im selben Vorgang und schreibt alles in EINER
        /// Transaktion; bei jedem Fehler rollt der Vorgang zurück und <see cref="BauteilkatalogVorschau.Fehler"/> nennt ihn.
        /// </summary>
        public static BauteilkatalogVorschau Uebernehmen(string pfad)
        {
            var uhr = Stopwatch.StartNew();
            BauteilkatalogQuelle q = BauteilkatalogQuelle.Lesen(pfad);
            if (q.Abgelehnt != null) return Planen(q, null).Vorschau;
            BauteilkatalogVorschau vs;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                Plan p = Planen(q, StandLesen(v));
                vs = p.Vorschau;
                try
                {
                    Schreiben(v, p);
                    v.Commit();
                    vs.Geschrieben = true;
                }
                catch (Exception ex)
                {
                    v.Rollback();
                    vs.Fehler = ex.Message;
                }
            }
            vs.DauerMs = uhr.ElapsedMilliseconds;
            return vs;
        }

        private static readonly string[] BAUSTOFF_KOPF = { "Bezeichner", "Gruppe", "Lambda", "Rho", "cp", "Quelle", "Quellkennung" };
        private static readonly string[] AUFBAU_KOPF = { "Bezeichner", "Beschreibung", "Bauteilart", "Quelle", "Quellkennung" };
        private static readonly string[] SCHICHT_KOPF = { "ID_Aufbau", "Reihenfolge", "ID_Baustoff", "Dicke", "IstLuftschicht", "Lambda", "Rho", "cp" };
        private static readonly string[] ABSCHNITT_KOPF = { "ID_Aufbau", "Nummer", "Flaechenanteil", "Text", "Quellkennung" };

        /// <summary>Die Spalten eines Kopfsatzes in Schreibreihenfolge (ohne Herkunft, ReadOnly).</summary>
        private static List<string> Spalten(string[] kopf, string tabelle) =>
            kopf.Concat(BauteilkatalogSchema.SpaltenVon(tabelle).Where(s => s.Art != BauteilkatalogSchema.Spaltenart.Verweis &&
                                                                            !kopf.Contains(s.Name)).Select(s => s.Name)).ToList();

        /// <summary>Schreibt den Plan im Vorgang <paramref name="v"/> (vorbereitete Anweisungen, Parameter mit „?“).</summary>
        internal static void Schreiben(DbVorgang v, Plan p)
        {
            string tb = BauteilkatalogSchema.TAB_BAUSTOFF_STAMM, ta = BauteilkatalogSchema.TAB_AUFBAU_STAMM;
            string ts = BauteilkatalogSchema.TAB_SCHICHT_STAMM, tx = BauteilkatalogSchema.TAB_ABSCHNITT_STAMM;
            List<string> sb = Spalten(BAUSTOFF_KOPF, tb), sa = Spalten(AUFBAU_KOPF, ta);
            List<string> ss = Spalten(SCHICHT_KOPF, ts).Concat(new[] { BauteilkatalogSchema.SPALTE_ID_ABSCHNITT }).ToList();
            string quelle = p.Vorschau.Quelle;

            foreach (Satz s in p.Baustoffe) KopfSchreiben(v, tb, sb, s, quelle);
            Schreibhaken("Baustoffe");
            foreach (Satz s in p.Aufbauten)
            {
                KopfSchreiben(v, ta, sa, s, quelle);
                if (s.Aktion == AKTUALISIEREN)
                {
                    v.Ausfuehren("DELETE FROM \"" + ts + "\" WHERE \"ID_Aufbau\" = ?", new DbParam("@a", s.ZielId));
                    v.Ausfuehren("DELETE FROM \"" + tx + "\" WHERE \"ID_Aufbau\" = ?", new DbParam("@a", s.ZielId));
                }
            }
            Schreibhaken("Aufbauten");
            string einAbschnitt = Einfuegen(tx, ABSCHNITT_KOPF);
            foreach (Satz s in p.Abschnitte)
            {
                s.Werte["ID_Aufbau"] = s.Vertreter.ZielId;
                s.ZielId = v.EinfuegenUndId(einAbschnitt, Parameter(ABSCHNITT_KOPF, s.Werte));
            }
            Schreibhaken("Abschnitte");
            string einSchicht = Einfuegen(ts, ss);
            foreach (Satz s in p.Schichten)
            {
                s.Werte["ID_Aufbau"] = s.Vertreter.ZielId;
                s.Werte["ID_Baustoff"] = s.Werte.TryGetValue("__Baustoff", out object bs) && bs is Satz b ? b.ZielId : (object)null;
                s.Werte[BauteilkatalogSchema.SPALTE_ID_ABSCHNITT] = s.Abschnitt?.ZielId;
                v.Ausfuehren(einSchicht, Parameter(ss, s.Werte));
            }
            Schreibhaken("Schichten");
        }

        private static void KopfSchreiben(DbVorgang v, string tabelle, List<string> spalten, Satz s, string quelle)
        {
            if (s.Aktion == NEU)
            {
                var alle = spalten.Concat(new[] { "Herkunft", "ReadOnly" }).ToList();
                s.Werte["Herkunft"] = BauteilkatalogSchema.HERKUNFT_IMPORT;
                s.Werte["ReadOnly"] = 0;
                if (!s.Werte.ContainsKey(BauteilkatalogSchema.SPALTE_UNGEEIGNET) && tabelle == BauteilkatalogSchema.TAB_AUFBAU_STAMM)
                    s.Werte[BauteilkatalogSchema.SPALTE_UNGEEIGNET] = 0;
                s.ZielId = v.EinfuegenUndId(Einfuegen(tabelle, alle), Parameter(alle, s.Werte));
                return;
            }
            s.ZielId = s.Bestand.Id;
            if (s.Aktion == AKTUALISIEREN)
            {
                List<string> sp = spalten.Where(x => s.Werte.ContainsKey(x)).ToList();
                var p = Parameter(sp, s.Werte).ToList();
                p.Add(new DbParam("@id", s.ZielId));
                v.Ausfuehren("UPDATE \"" + tabelle + "\" SET " + string.Join(", ", sp.Select(x => "\"" + x + "\" = ?")) + " WHERE \"ID\" = ?", p.ToArray());
                return;
            }
            // ERGAENZEN: nur leere Felder füllen, Quelle anhängen; Bezeichner, Herkunft und Schloss bleiben.
            List<string> leer = spalten.Where(x => s.Werte.ContainsKey(x) && x != "Bezeichner" && x != "Quelle" &&
                                                   x != BauteilkatalogSchema.SPALTE_UNGEEIGNET).ToList();
            var q = Parameter(leer, s.Werte).ToList();
            q.Add(new DbParam("@q", QuelleAnhaengen(s.Bestand.Quelle, quelle)));
            q.Add(new DbParam("@id", s.ZielId));
            v.Ausfuehren("UPDATE \"" + tabelle + "\" SET " + string.Join(", ", leer.Select(x => "\"" + x + "\" = COALESCE(\"" + x + "\", ?)")) +
                         ", \"Quelle\" = ? WHERE \"ID\" = ?", q.ToArray());
        }

        /// <summary>Hängt die Importquelle an eine vorhandene Quelle an (einmal, höchstens 400 Zeichen).</summary>
        public static string QuelleAnhaengen(string alt, string neu)
        {
            if (string.IsNullOrWhiteSpace(alt)) return neu;
            if (alt.Contains(neu, StringComparison.Ordinal)) return alt;
            string s = alt.TrimEnd() + "; " + neu;
            return s.Length <= BauteilkatalogSchema.QUELLE_MAX_ZEICHEN ? s : alt;
        }

        private static string Einfuegen(string tabelle, IReadOnlyList<string> spalten) =>
            "INSERT INTO \"" + tabelle + "\" (" + string.Join(", ", spalten.Select(x => "\"" + x + "\"")) + ") VALUES (" +
            string.Join(", ", spalten.Select(_ => "?")) + ")";

        private static DbParam[] Parameter(IReadOnlyList<string> spalten, Dictionary<string, object> werte) =>
            spalten.Select(x => new DbParam("@" + x, werte.TryGetValue(x, out object w) && w != null ? w : DBNull.Value)).ToArray();
    }
}
