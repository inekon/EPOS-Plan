using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Auslegungsprüfung erdgekoppelter Wärmequellen nach VDI 4640 Blatt 2:2019-06
    /// (Weißdruck), Anhänge A und B - Stufe 1 nach Konzept 13.1.
    ///
    /// Stufe 1 deckt den Standardfall "Heizen ohne Trinkwarmwasser" ab:
    ///
    ///   Kollektoren - Anhang A, Tabelle A2 vollständig (15 Klimazonen nach
    ///   DIN 4710 x 4 Bodenarten, je Entzugsleistung [W/m²] und Entzugsenergie
    ///   [kWh/(m²·a)] sowie die Jahresvolllaststunden der Zone). Einzuhalten sind
    ///   BEIDE Werte.
    ///
    ///   Sonden - Anhang B, Tabelle B2 ("nur Heizen", Soleaustritt −5 °C bei
    ///   Spitzenlast), spezifische Entzugsleistung [W/m] über Wärmeleitfähigkeit
    ///   des Untergrunds, Sondenzahl (Reihe, Abstand 6 m) und Jahresvolllaststunden.
    ///
    /// WICHTIG: Tabelle B2 ist hier nur als AUSZUG kodiert - genau die Stützstellen,
    /// die Konzept 13.1 wiedergibt (1200/1800/2400 h/a, Sondenzahl 1/5 bzw. 1/4,
    /// λ = 1…4 W/(m·K)). Die Vervollständigung gegen den Normtext ist offen; die
    /// Zwischenwerte entstehen durch lineare Interpolation, außerhalb der
    /// Stützstellen wird geklemmt. Auf der Sondenzahl-Achse ist diese Klemmung
    /// NICHT konservativ (die zulässige Entzugsleistung sinkt mit der Sondenzahl);
    /// das Ergebnis trägt deshalb das Flag <see cref="Ergebnis.AusserhalbTabelle"/>
    /// und einen entsprechenden Hinweistext. Tabelle A2 ist dagegen vollständig.
    ///
    /// Stufe 2 (später, Konzept 13.1): Tabellen B3–B7 für die übrigen Betriebsfälle,
    /// Tabelle A3 für Kapillarrohrmatten, Rohrabstands-Empfehlung im Dialog.
    ///
    /// Randbedingungen der Tabellen, die hier NICHT geprüft werden: A2 gilt für
    /// PE-Rohr 32 x 3,0 mm bei turbulenter Durchströmung und Heizgrenztemperatur
    /// 12 °C; B2 für Doppel-U-Sonde 32 x 3,0, Verfüllung λ = 0,8 W/(m·K), Bohrloch
    /// 150 mm, turbulente Strömung. Bei laminarer Strömung sinkt die zulässige
    /// Entzugsleistung um rund 10 % (A2) bzw. auf Faktor 0,79…0,85 (B1).
    /// </summary>
    public static class VDI4640Pruefung
    {
        // ------------------------------------------------------------------
        // Bodenarten der Tabelle A1
        // ------------------------------------------------------------------

        public const string BODENART_SAND = "Sand";
        public const string BODENART_LEHM = "Lehm";
        public const string BODENART_SCHLUFF = "Schluff";
        public const string BODENART_SANDIGER_TON = "Sandiger Ton";

        /// <summary>Bodenarten in Spaltenreihenfolge der Tabelle A2.</summary>
        public static readonly string[] Bodenarten =
        { BODENART_SAND, BODENART_LEHM, BODENART_SCHLUFF, BODENART_SANDIGER_TON };

        /// <summary>Rechenwerte λ [W/(m·K)] der vier Bodenarten nach Tabelle A1.</summary>
        public static readonly double[] BodenartLambda = { 1.2, 1.5, 1.5, 1.8 };

        /// <summary>Wassergehalt [Vol.-%] der vier Bodenarten nach Tabelle A1 (Anzeige).</summary>
        public static readonly string[] BodenartWassergehalt = { "< 10", "25…31", "35…40", "35…40" };

        // ------------------------------------------------------------------
        // Tabelle A2 - Kollektoren
        // ------------------------------------------------------------------

        /// <summary>
        /// Maximale Entzugsleistung [W/m²] je Klimazone (Zeile 0 = Zone 1) und
        /// Bodenart (Spalte 0 = Sand). VDI 4640 Bl. 2:2019-06, Tabelle A2.
        /// </summary>
        private static readonly double[,] A2_LEISTUNG =
        {
            //  Sand  Lehm  Schluff  Sand.Ton     Zone
            {   28,   34,   36,      39 },     //  1
            {   21,   29,   29,      31 },     //  2
            {   25,   32,   35,      38 },     //  3
            {   23,   30,   33,      36 },     //  4
            {   29,   37,   38,      41 },     //  5
            {   16,   26,   28,      30 },     //  6
            {   25,   32,   33,      37 },     //  7
            {   12,   23,   25,      26 },     //  8
            {   17,   26,   29,      32 },     //  9
            {   13,   23,   26,      28 },     // 10
            {    5,    9,   12,      13 },     // 11
            {   30,   37,   39,      42 },     // 12
            {   16,   25,   27,      29 },     // 13
            {   14,   25,   27,      28 },     // 14
            {   14,   25,   26,      29 }      // 15
        };

        /// <summary>
        /// Maximale Entzugsenergie [kWh/(m²·a)], gleiche Anordnung wie A2_LEISTUNG.
        /// </summary>
        private static readonly double[,] A2_ENERGIE =
        {
            //  Sand  Lehm  Schluff  Sand.Ton     Zone
            {   46,   56,   59,      64 },     //  1
            {   37,   52,   52,      55 },     //  2
            {   41,   52,   57,      62 },     //  3
            {   34,   45,   49,      54 },     //  4
            {   49,   62,   64,      69 },     //  5
            {   31,   50,   54,      58 },     //  6
            {   40,   51,   52,      59 },     //  7
            {   24,   46,   50,      52 },     //  8
            {   29,   45,   50,      56 },     //  9
            {   23,   41,   46,      50 },     // 10
            {   12,   21,   28,      31 },     // 11
            {   40,   49,   52,      56 },     // 12
            {   28,   45,   48,      52 },     // 13
            {   25,   46,   49,      51 },     // 14
            {   24,   43,   45,      50 }      // 15
        };

        /// <summary>Jahresvolllaststunden je Klimazone [h/a], Tabelle A2.</summary>
        private static readonly double[] A2_VOLLLASTSTUNDEN =
        { 1650, 1800, 1650, 1500, 1700, 1950, 1600, 2000, 1750, 1800, 2400, 1350, 1800, 1850, 1750 };

        /// <summary>Anzahl der Klimazonen nach DIN 4710 (Bild A1 der Norm).</summary>
        public const int KLIMAZONEN = 15;

        // ------------------------------------------------------------------
        // Tabelle B2 - Sonden (AUSZUG, siehe Klassenkommentar)
        // ------------------------------------------------------------------

        /// <summary>λ-Stützstellen der Tabelle B2 [W/(m·K)].</summary>
        private static readonly double[] B2_LAMBDA = { 1.0, 2.0, 3.0, 4.0 };

        /// <summary>Volllaststunden-Stützstellen der Tabelle B2 [h/a].</summary>
        private static readonly double[] B2_STUNDEN = { 1200, 1800, 2400 };

        /// <summary>
        /// Sondenzahl-Stützstellen je Volllaststunden-Zeile. Der Konzeptauszug
        /// führt zu 1200 und 1800 h die Werte für 1 und 5 Sonden, zu 2400 h die
        /// für 1 und 4 Sonden.
        /// </summary>
        private static readonly double[][] B2_SONDEN =
        {
            new double[] { 1, 5 },
            new double[] { 1, 5 },
            new double[] { 1, 4 }
        };

        /// <summary>
        /// Spezifische Entzugsleistung [W/m]: [Volllaststunden-Zeile][Sondenzahl][λ].
        /// VDI 4640 Bl. 2:2019-06, Tabelle B2 ("nur Heizen", Austritt −5 °C).
        /// </summary>
        private static readonly double[][][] B2_LEISTUNG =
        {
            // 1200 h/a
            new double[][] { new double[] { 37.5, 52.0, 61.5, 68.3 },     // 1 Sonde
                             new double[] { 29.7, 43.4, 53.4, 60.8 } },   // 5 Sonden
            // 1800 h/a
            new double[][] { new double[] { 28.6, 43.0, 53.0, 60.4 },     // 1 Sonde
                             new double[] { 21.6, 33.9, 43.6, 51.3 } },   // 5 Sonden
            // 2400 h/a
            new double[][] { new double[] { 23.7, 37.4, 47.3, 55.0 },     // 1 Sonde
                             new double[] { 18.0, 29.5, 38.5, 46.0 } }    // 4 Sonden
        };

        // ------------------------------------------------------------------
        // Bodentyp (Blatt 1) -> Bodenart (Blatt 2, Tabelle A1)
        // ------------------------------------------------------------------

        /// <summary>
        /// Bildet die 13 Untergrundtypen des Blatt-1-Katalogs auf die vier
        /// Bodenarten der Tabelle A1 ab.
        ///
        /// Zuordnungsregel - Textur zuerst, λ als Feinabgleich:
        ///
        ///  - Sand und Kies (alle Feuchtezustände) → "Sand". Grobkörnige
        ///    Lockergesteine; A1 kennt keinen Kies, "Sand" ist die nächstgelegene
        ///    Kornklasse. "Sand" führt zugleich die niedrigsten Grenzwerte der
        ///    Tabelle A2, die Zuordnung ist also konservativ. Bestätigt durch das
        ///    Konzept-Mockup 4.5: dort ergibt "Sand, feucht" in Klimazone 6 die
        ///    Grenzwerte 16 W/m² und 31 kWh/(m²·a) - exakt Zone 6 / Spalte Sand.
        ///  - Ton/Schluff trocken → "Sand". Hier schlägt der λ-Feinabgleich die
        ///    Textur: mit λ = 0,5 W/(m·K) ist das der thermisch schlechteste Typ
        ///    des gesamten Blatt-1-Katalogs und liegt deutlich UNTER dem
        ///    kleinsten A1-Rechenwert (Sand, 1,2). Die Bodenarten der Tabelle A1
        ///    sind gerade über den Wassergehalt definiert (BodenartWassergehalt);
        ///    "Schluff" steht dort für 35…40 Vol.-% Wasser und λ = 1,5 und hätte
        ///    für einen trockenen Boden in Zone 6 einen um 75 % höheren Entzug
        ///    zugelassen als für feuchten Sand. "Sand" führt die niedrigsten
        ///    A2-Grenzwerte - die Zuordnung ist damit konservativ.
        ///  - Ton/Schluff wassergesättigt → "Sandiger Ton": bindig und λ = 1,8 =
        ///    exakt der A1-Rechenwert des sandigen Tons.
        ///  - Geschiebemergel/-lehm → "Lehm" (Textur).
        ///  - Festgesteine (Tonstein, Sandstein, Kalkstein, Granit, Gneis) →
        ///    "Sandiger Ton". Tabelle A2 gilt für Lockergestein; Fels liegt mit
        ///    λ = 2,2…3,2 W/(m·K) über allen A1-Klassen, weshalb die höchste
        ///    Klasse gewählt wird. Ein Flachkollektor im Fels ist ohnehin
        ///    untypisch - die Prüfung ist dort nur grobe Orientierung und wird
        ///    im Ergebnis über <see cref="Ergebnis.FestgesteinNaeherung"/>
        ///    gekennzeichnet (dafür ist den Prüfmethoden der Katalogschlüssel zu
        ///    übergeben).
        /// </summary>
        public static string BodenartAusBodentyp(string bodentypSchluessel)
        {
            switch ((bodentypSchluessel ?? "").ToUpperInvariant())
            {
                case "SAND_TROCKEN":
                case "SAND_FEUCHT":
                case "SAND_NASS":
                case "KIES_TROCKEN":
                case "KIES_NASS":
                    return BODENART_SAND;

                case "TON_TROCKEN":
                    // λ 0,5 < kleinster A1-Rechenwert 1,2 → konservativ "Sand"
                    return BODENART_SAND;

                case "TON_NASS":
                    return BODENART_SANDIGER_TON;

                case "MERGEL_LEHM":
                    return BODENART_LEHM;

                case "TONSTEIN":
                case "SANDSTEIN":
                case "KALKSTEIN":
                case "GRANIT":
                case "GNEIS":
                    return BODENART_SANDIGER_TON;

                default:
                    // unbekannt -> Vorgabetyp SAND_FEUCHT
                    return BODENART_SAND;
            }
        }

        /// <summary>true, wenn der Bodentyp ein Festgestein ist (Zuordnung nur näherungsweise).</summary>
        public static bool IstFestgestein(string bodentypSchluessel)
        {
            switch ((bodentypSchluessel ?? "").ToUpperInvariant())
            {
                case "TONSTEIN":
                case "SANDSTEIN":
                case "KALKSTEIN":
                case "GRANIT":
                case "GNEIS":
                    return true;
                default:
                    return false;
            }
        }

        private static int BodenartIndex(string bodenart)
        {
            for (int i = 0; i < Bodenarten.Length; i++)
                if (string.Equals(Bodenarten[i], bodenart, StringComparison.OrdinalIgnoreCase)) return i;
            return 0; // Sand
        }

        // ------------------------------------------------------------------
        // Ergebnisstruktur
        // ------------------------------------------------------------------

        /// <summary>Eine geprüfte Größe (Istwert gegen Grenzwert).</summary>
        public class Pruefzeile
        {
            public string Bezeichnung;
            public string IstText;      // z. B. "6 480 W / 250 m² = 25,9 W/m²"
            public double Istwert;
            public double Grenzwert;
            public string Einheit;
            public bool Ueberschritten;

            public string Text()
            {
                return string.Format(CultureInfo.CurrentCulture,
                    "{0,-18} {1}   Grenze {2:0.#} {3}{4}",
                    Bezeichnung, IstText, Grenzwert, Einheit, Ueberschritten ? "  !" : "");
            }
        }

        /// <summary>Ergebnis einer Auslegungsprüfung.</summary>
        public class Ergebnis
        {
            /// <summary>false = Prüfung nicht möglich (Hinweis erklärt warum).</summary>
            public bool Moeglich;

            /// <summary>true, wenn mindestens ein Grenzwert überschritten ist.</summary>
            public bool Warnung;

            /// <summary>
            /// true, wenn die Anfrage auf der Sondenzahl- oder der λ-Achse
            /// außerhalb der kodierten B2-Stützstellen lag und deshalb auf den
            /// Randwert geklemmt wurde. Auf der Sondenzahl-Achse ist das NICHT
            /// konservativ: die zulässige spezifische Entzugsleistung sinkt mit
            /// wachsender Sondenzahl, ein Feld mit 20 Sonden bekäme also den
            /// Grenzwert des 5-Sonden-Falls. Der Hinweistext führt das mit.
            /// </summary>
            public bool AusserhalbTabelle;

            /// <summary>
            /// true, wenn der Untergrund ein Festgestein ist und deshalb nur
            /// näherungsweise auf die höchste Bodenart der Tabelle A1 abgebildet
            /// werden konnte (Tabelle A2 gilt für Lockergestein). Das Ergebnis ist
            /// dann nur eine grobe Orientierung.
            /// </summary>
            public bool FestgesteinNaeherung;

            /// <summary>Kopfzeile, z. B. "Klimazone 6, Bodenart Sand".</summary>
            public string Grundlage = "";

            /// <summary>Erläuterung bzw. Grund, wenn die Prüfung nicht möglich ist.</summary>
            public string Hinweis = "";

            public List<Pruefzeile> Zeilen = new List<Pruefzeile>();

            /// <summary>Mehrzeilige Anzeige im Stil des Konzept-Mockups 4.5.</summary>
            public string Anzeigetext()
            {
                if (!Moeglich) return Hinweis;

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < Zeilen.Count; i++) sb.AppendLine(Zeilen[i].Text());
                if (!string.IsNullOrEmpty(Grundlage) || !string.IsNullOrEmpty(Hinweis))
                {
                    sb.Append("  ");
                    if (!string.IsNullOrEmpty(Grundlage)) sb.Append(Grundlage + ": ");
                    sb.Append(Hinweis);
                }
                return sb.ToString().TrimEnd();
            }
        }

        // ------------------------------------------------------------------
        // Kollektorprüfung (Tabelle A2)
        // ------------------------------------------------------------------

        /// <summary>
        /// Prüft einen Erdkollektor gegen Tabelle A2. Einzuhalten sind BEIDE
        /// Grenzwerte - spezifische Entzugsleistung und spezifische Jahresenergie.
        /// </summary>
        /// <param name="klimazone">Klimazone 1…15 nach DIN 4710; 0 = nicht zugeordnet</param>
        /// <param name="bodenart">Bodenart nach Tabelle A1 (siehe BodenartAusBodentyp)</param>
        /// <param name="flaecheM2">Kollektorfläche [m²]</param>
        /// <param name="maxEntzugW">maximale Entzugsleistung aus der Simulation [W]</param>
        /// <param name="jahresentzugKWh">Jahresentzugsarbeit aus der Simulation [kWh/a]</param>
        /// <param name="bodentypSchluessel">
        /// optionaler Blatt-1-Katalogschlüssel (z. B. "GNEIS"). Wird nur gebraucht,
        /// um <see cref="Ergebnis.FestgesteinNaeherung"/> zu setzen; die Prüfung
        /// selbst rechnet mit <paramref name="bodenart"/>.
        /// </param>
        public static Ergebnis PruefeKollektor(int klimazone, string bodenart, double flaecheM2,
                                               double maxEntzugW, double jahresentzugKWh,
                                               string bodentypSchluessel = null)
        {
            Ergebnis e = new Ergebnis();
            e.FestgesteinNaeherung = IstFestgestein(bodentypSchluessel);

            if (klimazone < 1 || klimazone > KLIMAZONEN)
            {
                e.Moeglich = false;
                e.Hinweis = "Klimazone nicht zugeordnet, Prüfung nicht möglich.";
                return e;
            }
            if (flaecheM2 <= 0)
            {
                e.Moeglich = false;
                e.Hinweis = "Keine Kollektorfläche angegeben, Prüfung nicht möglich.";
                return e;
            }

            int zi = klimazone - 1;
            int bi = BodenartIndex(bodenart);

            double grenzLeistung = A2_LEISTUNG[zi, bi];
            double grenzEnergie = A2_ENERGIE[zi, bi];

            double istLeistung = maxEntzugW / flaecheM2;
            double istEnergie = jahresentzugKWh / flaecheM2;

            CultureInfo ci = CultureInfo.CurrentCulture;

            Pruefzeile p1 = new Pruefzeile();
            p1.Bezeichnung = "Entzugsleistung";
            p1.Istwert = istLeistung;
            p1.Grenzwert = grenzLeistung;
            p1.Einheit = "W/m²";
            p1.Ueberschritten = istLeistung > grenzLeistung;
            p1.IstText = string.Format(ci, "{0:N0} W / {1:N0} m² = {2:0.0} W/m²", maxEntzugW, flaecheM2, istLeistung);
            e.Zeilen.Add(p1);

            Pruefzeile p2 = new Pruefzeile();
            p2.Bezeichnung = "Entzugsenergie";
            p2.Istwert = istEnergie;
            p2.Grenzwert = grenzEnergie;
            p2.Einheit = "kWh/(m²·a)";
            p2.Ueberschritten = istEnergie > grenzEnergie;
            p2.IstText = string.Format(ci, "{0:N0} kWh/a = {1:0.0} kWh/(m²·a)", jahresentzugKWh, istEnergie);
            e.Zeilen.Add(p2);

            e.Moeglich = true;
            e.Warnung = p1.Ueberschritten || p2.Ueberschritten;
            e.Grundlage = "Klimazone " + klimazone + ", Bodenart " + Bodenarten[bi];
            e.Hinweis = e.Warnung
                ? "Kollektor ist zu klein bemessen. Erforderlich sind mindestens "
                  + ErforderlicheFlaeche(maxEntzugW, jahresentzugKWh, grenzLeistung, grenzEnergie).ToString("N0", ci)
                  + " m² (Zonen-Volllaststunden " + A2_VOLLLASTSTUNDEN[zi].ToString("N0", ci) + " h/a)."
                : "Auslegung liegt innerhalb der Grenzwerte der Tabelle A2.";

            return e;
        }

        /// <summary>Kleinste Fläche, die beide Grenzwerte einhält [m²].</summary>
        private static double ErforderlicheFlaeche(double maxEntzugW, double jahresentzugKWh,
                                                   double grenzLeistung, double grenzEnergie)
        {
            double aus_p = grenzLeistung > 0 ? maxEntzugW / grenzLeistung : 0;
            double aus_e = grenzEnergie > 0 ? jahresentzugKWh / grenzEnergie : 0;
            return Math.Max(aus_p, aus_e);
        }

        /// <summary>Jahresvolllaststunden der Klimazone [h/a]; 0 wenn Zone unbekannt.</summary>
        public static double VolllaststundenZone(int klimazone)
        {
            if (klimazone < 1 || klimazone > KLIMAZONEN) return 0;
            return A2_VOLLLASTSTUNDEN[klimazone - 1];
        }

        // ------------------------------------------------------------------
        // Sondenprüfung (Tabelle B2, Auszug)
        // ------------------------------------------------------------------

        /// <summary>
        /// Prüft Erdsonden gegen Tabelle B2 (Auszug). Verglichen wird die
        /// spezifische Entzugsleistung [W/m] über die gesamte Sondenmeterzahl.
        /// </summary>
        /// <param name="lambda">Wärmeleitfähigkeit des Untergrunds [W/(m·K)]</param>
        /// <param name="sondenzahl">Anzahl Sonden (Reihe, Abstand 6 m)</param>
        /// <param name="volllastStunden">Jahresvolllaststunden der Wärmepumpe [h/a]</param>
        /// <param name="sondenmeterGesamt">Sondenlänge x Sondenzahl [m]</param>
        /// <param name="maxEntzugW">maximale Entzugsleistung aus der Simulation [W]</param>
        /// <param name="bodentypSchluessel">
        /// optionaler Blatt-1-Katalogschlüssel; setzt nur
        /// <see cref="Ergebnis.FestgesteinNaeherung"/>.
        /// </param>
        public static Ergebnis PruefeSonde(double lambda, int sondenzahl, double volllastStunden,
                                           double sondenmeterGesamt, double maxEntzugW,
                                           string bodentypSchluessel = null)
        {
            Ergebnis e = new Ergebnis();
            e.FestgesteinNaeherung = IstFestgestein(bodentypSchluessel);

            if (sondenmeterGesamt <= 0)
            {
                e.Moeglich = false;
                e.Hinweis = "Keine Sondenlänge angegeben, Prüfung nicht möglich.";
                return e;
            }
            if (volllastStunden <= 0)
            {
                e.Moeglich = false;
                e.Hinweis = "Keine Jahresvolllaststunden bekannt, Prüfung nicht möglich.";
                return e;
            }

            double grenzSpezifisch = B2Wert(lambda, sondenzahl, volllastStunden);
            double istSpezifisch = maxEntzugW / sondenmeterGesamt;
            e.AusserhalbTabelle = AusserhalbB2Bereich(lambda, sondenzahl, volllastStunden);

            CultureInfo ci = CultureInfo.CurrentCulture;

            Pruefzeile p = new Pruefzeile();
            p.Bezeichnung = "Entzugsleistung";
            p.Istwert = istSpezifisch;
            p.Grenzwert = grenzSpezifisch;
            p.Einheit = "W/m";
            p.Ueberschritten = istSpezifisch > grenzSpezifisch;
            p.IstText = string.Format(ci, "{0:N0} W / {1:N0} m = {2:0.0} W/m", maxEntzugW, sondenmeterGesamt, istSpezifisch);
            e.Zeilen.Add(p);

            e.Moeglich = true;
            e.Warnung = p.Ueberschritten;
            e.Grundlage = string.Format(ci, "λ = {0:0.0} W/(m·K), {1} Sonde(n), {2:N0} h/a",
                lambda, sondenzahl, volllastStunden);
            e.Hinweis = e.Warnung
                ? "Sondenfeld ist zu klein bemessen. Erforderlich sind mindestens "
                  + (maxEntzugW / grenzSpezifisch).ToString("N0", ci) + " Sondenmeter."
                : "Auslegung liegt innerhalb der Grenzwerte der Tabelle B2 (Auszug).";

            if (e.AusserhalbTabelle)
                e.Hinweis += " Achtung: Sondenzahl bzw. λ liegen außerhalb des kodierten Tabellenbereichs "
                           + "(B2-Auszug); der Grenzwert wurde auf die Randstützstelle geklemmt. Auf der "
                           + "Sondenzahl-Achse ist das nicht konservativ - größere Sondenfelder brauchen "
                           + "kleinere spezifische Entzugsleistungen, als der Randwert zulässt.";

            return e;
        }

        /// <summary>
        /// true, wenn <paramref name="lambda"/> oder <paramref name="sondenzahl"/>
        /// außerhalb der im B2-Auszug kodierten Stützstellen liegen und
        /// <see cref="B2Wert"/> deshalb auf den Randwert klemmt.
        ///
        /// Geprüft werden nur diese beiden Achsen: λ außerhalb 1…4 W/(m·K) und
        /// die Sondenzahl außerhalb der Stützstellen der beteiligten
        /// Volllaststunden-Zeile(n) - 1…5 bzw. 1…4 bei 2400 h/a. Die Klemmung auf
        /// der Volllaststunden-Achse wirkt dagegen konservativ (oberhalb 2400 h/a
        /// gilt der kleinste Tabellenwert) und wird nicht gemeldet.
        /// </summary>
        public static bool AusserhalbB2Bereich(double lambda, double sondenzahl, double volllastStunden)
        {
            if (lambda < B2_LAMBDA[0] || lambda > B2_LAMBDA[B2_LAMBDA.Length - 1]) return true;

            int z0, z1; double fz;
            Stuetzstellen(B2_STUNDEN, volllastStunden, out z0, out z1, out fz);

            for (int z = z0; z <= z1; z++)
            {
                double[] s = B2_SONDEN[z];
                if (sondenzahl < s[0] || sondenzahl > s[s.Length - 1]) return true;
            }
            return false;
        }

        /// <summary>
        /// Interpoliert die zulässige spezifische Entzugsleistung [W/m] aus dem
        /// B2-Auszug: linear über λ, dann über die Sondenzahl innerhalb der
        /// Volllaststunden-Zeile, dann zwischen den Zeilen. Außerhalb der
        /// Stützstellen wird auf den Randwert geklemmt.
        /// </summary>
        public static double B2Wert(double lambda, double sondenzahl, double volllastStunden)
        {
            // Zeilen (Volllaststunden) eingrenzen
            int z0, z1; double fz;
            Stuetzstellen(B2_STUNDEN, volllastStunden, out z0, out z1, out fz);

            double w0 = B2ZeilenWert(z0, lambda, sondenzahl);
            double w1 = B2ZeilenWert(z1, lambda, sondenzahl);
            return w0 + (w1 - w0) * fz;
        }

        private static double B2ZeilenWert(int zeile, double lambda, double sondenzahl)
        {
            double[] sonden = B2_SONDEN[zeile];

            int s0, s1; double fs;
            Stuetzstellen(sonden, sondenzahl, out s0, out s1, out fs);

            double v0 = B2LambdaWert(B2_LEISTUNG[zeile][s0], lambda);
            double v1 = B2LambdaWert(B2_LEISTUNG[zeile][s1], lambda);
            return v0 + (v1 - v0) * fs;
        }

        private static double B2LambdaWert(double[] werte, double lambda)
        {
            int l0, l1; double fl;
            Stuetzstellen(B2_LAMBDA, lambda, out l0, out l1, out fl);
            return werte[l0] + (werte[l1] - werte[l0]) * fl;
        }

        /// <summary>
        /// Ermittelt die beiden umschließenden Stützstellen und den Anteil dazwischen;
        /// außerhalb des Bereichs wird geklemmt (Anteil 0 bzw. 1 auf demselben Index).
        /// </summary>
        private static void Stuetzstellen(double[] achse, double wert, out int i0, out int i1, out double f)
        {
            if (wert <= achse[0]) { i0 = 0; i1 = 0; f = 0; return; }
            if (wert >= achse[achse.Length - 1]) { i0 = achse.Length - 1; i1 = i0; f = 0; return; }

            for (int i = 0; i < achse.Length - 1; i++)
            {
                if (wert <= achse[i + 1])
                {
                    i0 = i; i1 = i + 1;
                    double spanne = achse[i + 1] - achse[i];
                    f = spanne > 0 ? (wert - achse[i]) / spanne : 0;
                    return;
                }
            }

            i0 = achse.Length - 1; i1 = i0; f = 0;
        }

        // ------------------------------------------------------------------
        // Selbsttest - ausschließlich im Debug-Build (kein Testcode im Release)
        // ------------------------------------------------------------------
#if DEBUG

        /// <summary>
        /// Prüft die Kataloge und die Interpolation gegen die Stützstellen aus
        /// Konzept 13.1 (kein automatischer Aufruf; Zahlen im Umsetzungsprotokoll).
        ///
        /// ZUGESICHERT wird (jede Verletzung setzt das Gesamtergebnis auf
        /// FEHLGESCHLAGEN):
        ///   1. Bandbreite der Tabelle A2: 5…42 W/m²
        ///   2. Stichproben aus A2 gegen Konzept 13.1: Zone 6/Sand = 16 W/m² und
        ///      31 kWh/(m²·a), Zone 12/Sandiger Ton = 42 W/m² und 56 kWh/(m²·a)
        ///   3. Monotonie über alle vier Bodenarten je Zone: Sand ≤ Lehm ≤
        ///      Schluff ≤ Sandiger Ton, für Leistung UND Energie
        ///   4. Konsistenz Leistung × Volllaststunden / 1000 ≈ Energie (±1 kWh/m²)
        ///   5. Mockup 4.5: Zone 6 / Sand / 250 m² / 6480 W / 8900 kWh ergibt
        ///      25,9 W/m² gegen 16 und 35,6 kWh/(m²·a) gegen 31, Warnung gesetzt,
        ///      erforderliche Fläche 405 m²
        ///   6. Klimazone 0 ⇒ Prüfung nicht möglich
        ///   7. alle sechs kodierten B2-Stützstellen exakt, Interpolation monoton,
        ///      Klemmung auf den Randwert
        ///   8. Bereichsmeldung AusserhalbTabelle bei > 5 bzw. > 4 Sonden und λ > 4,
        ///      einschließlich Hinweistext im Ergebnis
        ///   9. Bodentyp→Bodenart-Mapping der 13 Katalogtypen, insbesondere
        ///      TON_TROCKEN → Sand (λ 0,5 unter dem kleinsten A1-Wert)
        ///  10. Festgestein-Kennzeichnung im Ergebnisobjekt
        /// </summary>
        public static string Selbsttest()
        {
            StringBuilder sb = new StringBuilder();
            CultureInfo ci = CultureInfo.InvariantCulture;
            bool ok = true;

            sb.AppendLine("Selbsttest VDI4640Pruefung (VDI 4640 Bl. 2:2019-06, Anhaenge A und B)");
            sb.AppendLine();

            // A2: Bandbreite laut Konzept 5 W/m2 (Zone 11 Sand) bis 42 W/m2 (Zone 12 sand. Ton)
            double min = double.MaxValue, max = double.MinValue;
            for (int z = 0; z < KLIMAZONEN; z++)
                for (int b = 0; b < 4; b++)
                {
                    if (A2_LEISTUNG[z, b] < min) min = A2_LEISTUNG[z, b];
                    if (A2_LEISTUNG[z, b] > max) max = A2_LEISTUNG[z, b];
                }
            sb.AppendLine(string.Format(ci, "1. Tabelle A2: {0} Zonen x 4 Bodenarten, Leistung {1:F0}…{2:F0} W/m2 (Konzept: 5…42)",
                KLIMAZONEN, min, max));
            if (min != 5 || max != 42) { sb.AppendLine("   FEHLER: Bandbreite weicht ab"); ok = false; }

            // Zellweise Stichproben gegen Konzept 13.1 (Befund der Review: die
            // Bandbreitenpruefung allein wuerde einen Zahlendreher nicht bemerken)
            ok &= A2Probe(sb, ci, 6, 0, 16, 31);      // Zone  6 / Sand
            ok &= A2Probe(sb, ci, 12, 3, 42, 56);     // Zone 12 / Sandiger Ton

            // Monotonie ueber ALLE vier Bodenarten je Zone (Sand am schwaechsten).
            // Die Kette lief frueher 0 <= 1 <= 3 und uebersprang Schluff.
            for (int z = 0; z < KLIMAZONEN; z++)
            {
                if (!(A2_LEISTUNG[z, 0] <= A2_LEISTUNG[z, 1] && A2_LEISTUNG[z, 1] <= A2_LEISTUNG[z, 2]
                                                            && A2_LEISTUNG[z, 2] <= A2_LEISTUNG[z, 3]) ||
                    !(A2_ENERGIE[z, 0] <= A2_ENERGIE[z, 1] && A2_ENERGIE[z, 1] <= A2_ENERGIE[z, 2]
                                                           && A2_ENERGIE[z, 2] <= A2_ENERGIE[z, 3]))
                {
                    sb.AppendLine("   FEHLER: Bodenart-Reihenfolge in Zone " + (z + 1));
                    ok = false;
                }
            }

            // Konsistenz der drei Tabellen: Leistung x Volllaststunden / 1000 = Energie
            for (int z = 0; z < KLIMAZONEN; z++)
                for (int b = 0; b < 4; b++)
                {
                    double erwartet = A2_LEISTUNG[z, b] * A2_VOLLLASTSTUNDEN[z] / 1000.0;
                    if (Math.Abs(erwartet - A2_ENERGIE[z, b]) > 1.0)
                    {
                        sb.AppendLine(string.Format(ci,
                            "   FEHLER: Zone {0}, {1}: {2:F0} W/m2 x {3:F0} h/a = {4:F1}, Tabelle {5:F0} kWh/(m2 a)",
                            z + 1, Bodenarten[b], A2_LEISTUNG[z, b], A2_VOLLLASTSTUNDEN[z], erwartet, A2_ENERGIE[z, b]));
                        ok = false;
                    }
                }

            // Mockup 4.5: Zone 6, Sand -> 16 W/m2 und 31 kWh/(m2 a)
            Ergebnis mock = PruefeKollektor(6, BodenartAusBodentyp("SAND_FEUCHT"), 250, 6480, 8900, "SAND_FEUCHT");
            sb.AppendLine(string.Format(ci, "2. Mockup 4.5 (Zone 6, Sand feucht, 250 m2, 6480 W, 8900 kWh/a):"));
            sb.AppendLine("   " + mock.Anzeigetext().Replace("\r\n", "\r\n   "));
            if (Math.Abs(mock.Zeilen[0].Grenzwert - 16) > 0.001 || Math.Abs(mock.Zeilen[1].Grenzwert - 31) > 0.001)
            { sb.AppendLine("   FEHLER: Grenzwerte stimmen nicht mit dem Mockup ueberein"); ok = false; }
            if (Math.Abs(mock.Zeilen[0].Istwert - 25.92) > 0.01 || Math.Abs(mock.Zeilen[1].Istwert - 35.6) > 0.01)
            { sb.AppendLine("   FEHLER: Istwerte stimmen nicht mit dem Mockup ueberein"); ok = false; }
            if (!mock.Warnung) { sb.AppendLine("   FEHLER: Warnung fehlt"); ok = false; }
            if (mock.Hinweis.IndexOf("405", StringComparison.Ordinal) < 0)
            { sb.AppendLine("   FEHLER: erforderliche Flaeche 405 m2 fehlt im Hinweis"); ok = false; }

            // Zone 0
            Ergebnis z0 = PruefeKollektor(0, BODENART_SAND, 250, 6480, 8900);
            sb.AppendLine("3. Klimazone 0: " + z0.Anzeigetext());
            if (z0.Moeglich) { sb.AppendLine("   FEHLER: Zone 0 muesste unmoeglich sein"); ok = false; }

            // B2-Stuetzstellen exakt treffen
            sb.AppendLine("4. Tabelle B2 (Auszug), Stuetzstellen:");
            double[][] soll =
            {
                new double[] { 1200, 1, 1.0, 37.5 }, new double[] { 1200, 5, 4.0, 60.8 },
                new double[] { 1800, 1, 2.0, 43.0 }, new double[] { 1800, 5, 3.0, 43.6 },
                new double[] { 2400, 1, 4.0, 55.0 }, new double[] { 2400, 4, 1.0, 18.0 }
            };
            for (int i = 0; i < soll.Length; i++)
            {
                double w = B2Wert(soll[i][2], soll[i][1], soll[i][0]);
                sb.AppendLine(string.Format(ci, "   {0,4:F0} h/a, {1:F0} Sonde(n), lambda {2:F1} -> {3,5:F1} W/m (soll {4:F1})",
                    soll[i][0], soll[i][1], soll[i][2], w, soll[i][3]));
                if (Math.Abs(w - soll[i][3]) > 0.001) { sb.AppendLine("   FEHLER"); ok = false; }
            }

            // Interpolation und Klemmen
            double mitte = B2Wert(1.5, 1, 1500);
            double geklemmt = B2Wert(0.2, 1, 600);
            sb.AppendLine(string.Format(ci, "   Interpolation 1500 h/a, 1 Sonde, lambda 1,5 -> {0:F2} W/m", mitte));
            sb.AppendLine(string.Format(ci, "   Klemmung       600 h/a, 1 Sonde, lambda 0,2 -> {0:F2} W/m (= Stuetzstelle 1200 h/lambda 1,0 = 37,5)", geklemmt));
            if (Math.Abs(geklemmt - 37.5) > 0.001) { sb.AppendLine("   FEHLER: Klemmung"); ok = false; }
            if (mitte <= B2Wert(1.0, 1, 1500) || mitte >= B2Wert(2.0, 1, 1500)) { sb.AppendLine("   FEHLER: Interpolation nicht monoton"); ok = false; }

            // Bereichsmeldung ausserhalb der kodierten Stuetzstellen
            sb.AppendLine("5. Bereichsmeldung (Klemmung auf Sondenzahl- und lambda-Achse):");
            ok &= BereichsProbe(sb, ci, 2.0, 20, 1800, true);    // 20 Sonden, Zeile endet bei 5
            ok &= BereichsProbe(sb, ci, 2.0, 5, 2400, true);     // 2400-h-Zeile endet bei 4
            ok &= BereichsProbe(sb, ci, 4.5, 1, 1800, true);     // lambda > 4
            ok &= BereichsProbe(sb, ci, 2.0, 5, 1800, false);    // exakt auf der Stuetzstelle
            ok &= BereichsProbe(sb, ci, 1.5, 3, 1500, false);    // innerhalb beider Achsen

            Ergebnis gross = PruefeSonde(2.0, 20, 1800, 2000, 40000);
            if (!gross.AusserhalbTabelle ||
                gross.Hinweis.IndexOf("außerhalb des kodierten Tabellenbereichs (B2-Auszug)", StringComparison.Ordinal) < 0)
            { sb.AppendLine("   FEHLER: Hinweistext zur Klemmung fehlt im Ergebnis"); ok = false; }
            sb.AppendLine("   20 Sonden, 1800 h/a: " + gross.Hinweis);

            // Bodenart-Mapping
            sb.AppendLine("6. Bodentyp -> Bodenart:");
            string[] keys = { "TON_TROCKEN","TON_NASS","SAND_TROCKEN","SAND_FEUCHT","SAND_NASS","KIES_TROCKEN",
                              "KIES_NASS","MERGEL_LEHM","TONSTEIN","SANDSTEIN","KALKSTEIN","GRANIT","GNEIS" };
            string[] sollBodenart = { BODENART_SAND, BODENART_SANDIGER_TON, BODENART_SAND, BODENART_SAND,
                                      BODENART_SAND, BODENART_SAND, BODENART_SAND, BODENART_LEHM,
                                      BODENART_SANDIGER_TON, BODENART_SANDIGER_TON, BODENART_SANDIGER_TON,
                                      BODENART_SANDIGER_TON, BODENART_SANDIGER_TON };
            for (int i = 0; i < keys.Length; i++)
            {
                string ist = BodenartAusBodentyp(keys[i]);
                bool trifft = ist == sollBodenart[i];
                sb.AppendLine(string.Format("   {0,-14} -> {1}{2}{3}", keys[i], ist,
                    IstFestgestein(keys[i]) ? "  (Festgestein, nur Orientierung)" : "",
                    trifft ? "" : "   FEHLER, erwartet " + sollBodenart[i]));
                if (!trifft) ok = false;
            }

            // Festgestein-Kennzeichnung im Ergebnisobjekt
            Ergebnis fels = PruefeKollektor(6, BodenartAusBodentyp("GNEIS"), 250, 6480, 8900, "GNEIS");
            if (!fels.FestgesteinNaeherung) { sb.AppendLine("   FEHLER: FestgesteinNaeherung nicht gesetzt"); ok = false; }
            if (mock.FestgesteinNaeherung) { sb.AppendLine("   FEHLER: FestgesteinNaeherung faelschlich gesetzt"); ok = false; }
            sb.AppendLine("   Ergebnis-Flag FestgesteinNaeherung: GNEIS = " + fels.FestgesteinNaeherung +
                          ", SAND_FEUCHT = " + mock.FestgesteinNaeherung);

            sb.AppendLine();
            sb.AppendLine(ok ? "ERGEBNIS: alle Pruefungen bestanden." : "ERGEBNIS: mindestens eine Pruefung FEHLGESCHLAGEN.");
            return sb.ToString();
        }

        /// <summary>Stichprobe einer A2-Zelle (Zone 1-basiert) gegen Konzept 13.1.</summary>
        private static bool A2Probe(StringBuilder sb, CultureInfo ci, int klimazone, int bodenartIndex,
                                    double sollLeistung, double sollEnergie)
        {
            double l = A2_LEISTUNG[klimazone - 1, bodenartIndex];
            double en = A2_ENERGIE[klimazone - 1, bodenartIndex];
            bool ok = l == sollLeistung && en == sollEnergie;
            sb.AppendLine(string.Format(ci, "   Probe Zone {0,2} / {1,-13} {2:F0} W/m2 (soll {3:F0}), {4:F0} kWh/(m2 a) (soll {5:F0}){6}",
                klimazone, Bodenarten[bodenartIndex], l, sollLeistung, en, sollEnergie, ok ? "" : "   FEHLER"));
            return ok;
        }

        /// <summary>Prüft die Bereichsmeldung <see cref="AusserhalbB2Bereich"/>.</summary>
        private static bool BereichsProbe(StringBuilder sb, CultureInfo ci, double lambda, double sondenzahl,
                                          double stunden, bool erwartet)
        {
            bool ist = AusserhalbB2Bereich(lambda, sondenzahl, stunden);
            bool ok = ist == erwartet;
            sb.AppendLine(string.Format(ci, "   lambda {0:F1}, {1:F0} Sonde(n), {2:F0} h/a -> ausserhalb = {3,-5} (erwartet {4}){5}",
                lambda, sondenzahl, stunden, ist, erwartet, ok ? "" : "   FEHLER"));
            return ok;
        }

#endif
    }
}
