using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // ERZEUGT von Werkzeuge/Standardlastprofile/ableiten.py - NICHT VON HAND AENDERN.
    // Pruefen: py Werkzeuge/Standardlastprofile/ableiten.py (ohne Argument), schreiben: ... schreiben.
    //
    // DIE BDEW-STANDARDLASTPROFILE STROM 2025 als Katalogsaat der "Datenbank Strombedarf",
    // gesaet mit dem Schemaschritt StandardlastprofilSchema.SCHRITT.
    //
    // QUELLE. BDEW, Standardlastprofile Strom 2025, Veroeffentlichung vom 17.03.2025; Datei
    // Quellen/Standardlastprofile/BDEW_Repraesentative_Profile_H25_G25_L25_P25_S25.xlsx
    // (SHA-256 1803d4c612693563a784eb61001e7c58ffd6bd18a6bca3780f774f3c3459b845). Die Dateien tragen keine Lizenzangabe;
    // ausgeliefert werden allein die abgeleiteten Werte unten (Quellen/Standardlastprofile/LIESMICH.md).
    //
    // ABLEITUNG (Werkzeuge/Standardlastprofile/LIESMICH.md):
    //   Wochenwerte: 168 Stunden Mo 0 Uhr ... So 23 Uhr; Mo-Fr aus dem Werktag (WT), Sa aus dem Samstag
    //   (SA), So aus dem Sonn- und Feiertag (FT); Stunde = Summe der vier Viertelstundenenergien;
    //   Jahresmittel je Typtag, die Monate mit der Zahl ihrer Tage gewichtet; ohne Dynamisierung.
    //   Einheit kWh je Stunde bei 1.000 MWh/a - die Skala ist fuer die Rechnung gleichgueltig,
    //   BhkwPlan.StromWocheToJahr normiert je Monat auf den Monatswert.
    //   Monatswerte: Monatsenergie [MWh] der Abrollung ueber 365 Tage, gemittelt ueber die sieben
    //   Wochentage des 1. Januar (28-Jahre-Zyklus), ohne Feiertage; H25 mit der Dynamisierung
    //   F(t) je Tag des Jahres (PDF S. 4), G25 und L25 ohne; danach exakt auf 1.000 MWh skaliert.
    //   Feiertage legt ein Betriebskalender an der Zuordnung auf den Sonntag (BDEW: Feiertag = FT).
    // ====================================================================================

    /// <summary>
    /// Ein BDEW-Standardlastprofil der Saat — Kopfsatz für <c>Tab_Stromverbraucher_STAMM</c> und Typprofil
    /// für <c>Tab_Stromverbrauchertyp_STAMM</c>, verknüpft über den Namen (<c>Typ</c> = <c>Typname</c>).
    /// </summary>
    public sealed class StandardlastprofilSaat
    {
        internal StandardlastprofilSaat(string kuerzel, string bezeichner, string typname, string beschreibung,
                                        string typbeschreibung, double[] monatswerte, double[] wochenwerte)
        {
            Kuerzel = kuerzel;
            Bezeichner = bezeichner;
            Typname = typname;
            Beschreibung = beschreibung;
            Typbeschreibung = typbeschreibung;
            Monatswerte = Array.AsReadOnly(monatswerte);
            Wochenwerte = Array.AsReadOnly(wochenwerte);
        }

        /// <summary>Das Kürzel des BDEW (H25, G25, L25).</summary>
        public string Kuerzel { get; }

        /// <summary>Der Name des Katalogsatzes (<c>Tab_Stromverbraucher_STAMM.Bezeichner</c>).</summary>
        public string Bezeichner { get; }

        /// <summary>Der Name des Typprofils (<c>Tab_Stromverbrauchertyp_STAMM.Typname</c>), zugleich <c>Typ</c> des Kopfsatzes.</summary>
        public string Typname { get; }

        /// <summary>Die Beschreibung des Kopfsatzes.</summary>
        public string Beschreibung { get; }

        /// <summary>Die Beschreibung des Typprofils.</summary>
        public string Typbeschreibung { get; }

        /// <summary>Die zwölf Monatswerte [MWh], Januar zuerst; Summe 1.000 MWh.</summary>
        public IReadOnlyList<double> Monatswerte { get; }

        /// <summary>Die 168 Wochenstunden [kWh je Stunde bei 1.000 MWh/a], Montag 0 Uhr zuerst.</summary>
        public IReadOnlyList<double> Wochenwerte { get; }
    }

    /// <summary>
    /// <b>Die drei Sätze der Saat</b> (H25, G25, L25) mit Quelle und Prüfsumme der Excel, aus der sie
    /// abgeleitet sind.
    /// </summary>
    public static class StandardlastprofilSaattabelle
    {
        /// <summary>Die Quelle der Werte.</summary>
        public const string QUELLE = "BDEW, Standardlastprofile Strom 2025, Veröffentlichung vom 17.03.2025";

        /// <summary>Die Excel im Repositorium, repo-relativ.</summary>
        public const string QUELLDATEI = "Quellen/Standardlastprofile/BDEW_Repraesentative_Profile_H25_G25_L25_P25_S25.xlsx";

        /// <summary>SHA-256 der Excel, aus der die Werte abgeleitet sind.</summary>
        public const string QUELLDATEI_SHA256 = "1803d4c612693563a784eb61001e7c58ffd6bd18a6bca3780f774f3c3459b845";

        /// <summary>Die Jahressumme jedes Satzes [MWh] — die Normierung des BDEW (1 Mio. kWh).</summary>
        public const double JAHRESSUMME_MWH = 1000.0;

        /// <summary>Die Sätze in der Folge H25, G25, L25.</summary>
        public static IReadOnlyList<StandardlastprofilSaat> Alle { get; } = new[]
        {
            new StandardlastprofilSaat(
                "H25", "BDEW_H25_Haushalt", "BDEW_H25",
                "BDEW-Standardlastprofil 2025 H25 (Haushalt), normiert auf 1.000 MWh/a; im Projekt auf den Jahresverbrauch skalieren; Feiertage über den Betriebskalender",
                "BDEW-Standardlastprofil 2025 H25 (Haushalt): Wochenprofil aus dem Jahresmittel je Typtag (Mo–Fr Werktag, Sa Samstag, So Sonn- und Feiertag), kWh je Stunde bei 1.000 MWh/a",
                new[]
                {
                    // Januar bis Dezember [MWh]
                    101.047323, 88.184665, 88.292656, 81.527387, 77.137443, 71.520708, 74.370492, 73.546158, 73.162851, 83.530801, 88.298723, 99.380793,
                },
                new[]
                {
                    // Montag 0 bis 23 Uhr (Werktag)
                    80.559652, 69.676000, 65.720523, 65.263707, 68.503008, 78.721570, 99.279214, 106.132986, 100.514288, 98.124142, 98.648679, 107.740937,
                    112.380674, 110.129132, 106.866310, 109.317948, 120.492901, 142.882003, 163.715378, 169.843310, 161.772567, 147.764992, 129.767792, 103.072526,
                    // Dienstag 0 bis 23 Uhr (Werktag)
                    80.559652, 69.676000, 65.720523, 65.263707, 68.503008, 78.721570, 99.279214, 106.132986, 100.514288, 98.124142, 98.648679, 107.740937,
                    112.380674, 110.129132, 106.866310, 109.317948, 120.492901, 142.882003, 163.715378, 169.843310, 161.772567, 147.764992, 129.767792, 103.072526,
                    // Mittwoch 0 bis 23 Uhr (Werktag)
                    80.559652, 69.676000, 65.720523, 65.263707, 68.503008, 78.721570, 99.279214, 106.132986, 100.514288, 98.124142, 98.648679, 107.740937,
                    112.380674, 110.129132, 106.866310, 109.317948, 120.492901, 142.882003, 163.715378, 169.843310, 161.772567, 147.764992, 129.767792, 103.072526,
                    // Donnerstag 0 bis 23 Uhr (Werktag)
                    80.559652, 69.676000, 65.720523, 65.263707, 68.503008, 78.721570, 99.279214, 106.132986, 100.514288, 98.124142, 98.648679, 107.740937,
                    112.380674, 110.129132, 106.866310, 109.317948, 120.492901, 142.882003, 163.715378, 169.843310, 161.772567, 147.764992, 129.767792, 103.072526,
                    // Freitag 0 bis 23 Uhr (Werktag)
                    80.559652, 69.676000, 65.720523, 65.263707, 68.503008, 78.721570, 99.279214, 106.132986, 100.514288, 98.124142, 98.648679, 107.740937,
                    112.380674, 110.129132, 106.866310, 109.317948, 120.492901, 142.882003, 163.715378, 169.843310, 161.772567, 147.764992, 129.767792, 103.072526,
                    // Samstag 0 bis 23 Uhr (Samstag)
                    88.561666, 75.474397, 69.479690, 67.467819, 67.878636, 70.308038, 82.353740, 105.176748, 127.443367, 139.543301, 147.348205, 158.511932,
                    156.862123, 148.708630, 144.066570, 142.561795, 146.396285, 159.437633, 170.342858, 168.187381, 157.701011, 145.949452, 133.655227, 112.369016,
                    // Sonntag 0 bis 23 Uhr (Sonn- und Feiertag)
                    92.146890, 78.989203, 72.204033, 69.164208, 68.286863, 70.197252, 79.030773, 99.426227, 127.625296, 149.592036, 165.763178, 183.835277,
                    176.417430, 159.135342, 148.070066, 142.892940, 145.556033, 162.365293, 175.711611, 174.419249, 163.085151, 147.575992, 128.590438, 102.058164,
                }),
            new StandardlastprofilSaat(
                "G25", "BDEW_G25_Gewerbe", "BDEW_G25",
                "BDEW-Standardlastprofil 2025 G25 (Gewerbe allgemein), normiert auf 1.000 MWh/a; im Projekt auf den Jahresverbrauch skalieren; Feiertage über den Betriebskalender",
                "BDEW-Standardlastprofil 2025 G25 (Gewerbe allgemein): Wochenprofil aus dem Jahresmittel je Typtag (Mo–Fr Werktag, Sa Samstag, So Sonn- und Feiertag), kWh je Stunde bei 1.000 MWh/a",
                new[]
                {
                    // Januar bis Dezember [MWh]
                    93.642207, 83.681007, 90.014865, 81.551182, 79.761270, 77.307113, 75.673008, 77.063442, 76.754466, 82.065378, 90.076676, 92.409386,
                },
                new[]
                {
                    // Montag 0 bis 23 Uhr (Werktag)
                    55.105712, 53.787323, 53.323367, 54.445038, 58.712211, 71.515501, 101.397011, 157.303348, 208.915408, 229.864279, 242.516047, 241.403677,
                    222.589397, 207.757800, 205.041529, 195.733658, 178.744299, 157.571153, 124.638712, 97.999975, 83.183247, 73.308386, 64.606181, 59.116049,
                    // Dienstag 0 bis 23 Uhr (Werktag)
                    55.105712, 53.787323, 53.323367, 54.445038, 58.712211, 71.515501, 101.397011, 157.303348, 208.915408, 229.864279, 242.516047, 241.403677,
                    222.589397, 207.757800, 205.041529, 195.733658, 178.744299, 157.571153, 124.638712, 97.999975, 83.183247, 73.308386, 64.606181, 59.116049,
                    // Mittwoch 0 bis 23 Uhr (Werktag)
                    55.105712, 53.787323, 53.323367, 54.445038, 58.712211, 71.515501, 101.397011, 157.303348, 208.915408, 229.864279, 242.516047, 241.403677,
                    222.589397, 207.757800, 205.041529, 195.733658, 178.744299, 157.571153, 124.638712, 97.999975, 83.183247, 73.308386, 64.606181, 59.116049,
                    // Donnerstag 0 bis 23 Uhr (Werktag)
                    55.105712, 53.787323, 53.323367, 54.445038, 58.712211, 71.515501, 101.397011, 157.303348, 208.915408, 229.864279, 242.516047, 241.403677,
                    222.589397, 207.757800, 205.041529, 195.733658, 178.744299, 157.571153, 124.638712, 97.999975, 83.183247, 73.308386, 64.606181, 59.116049,
                    // Freitag 0 bis 23 Uhr (Werktag)
                    55.105712, 53.787323, 53.323367, 54.445038, 58.712211, 71.515501, 101.397011, 157.303348, 208.915408, 229.864279, 242.516047, 241.403677,
                    222.589397, 207.757800, 205.041529, 195.733658, 178.744299, 157.571153, 124.638712, 97.999975, 83.183247, 73.308386, 64.606181, 59.116049,
                    // Samstag 0 bis 23 Uhr (Samstag)
                    55.895668, 54.437090, 53.338137, 53.756885, 55.992984, 63.170584, 70.116852, 78.434189, 95.236463, 117.114211, 128.799622, 129.622134,
                    121.110978, 108.351093, 98.461293, 94.673510, 90.638658, 90.108805, 84.368764, 79.137658, 72.709403, 67.289619, 61.504118, 57.641030,
                    // Sonntag 0 bis 23 Uhr (Sonn- und Feiertag)
                    54.421701, 52.924230, 52.096033, 51.936238, 52.645677, 54.391159, 57.428997, 60.668099, 64.047537, 67.481356, 71.772079, 74.185978,
                    73.988838, 72.383882, 70.778241, 71.231986, 73.261833, 75.233551, 74.240312, 71.814353, 68.223025, 63.539353, 58.432356, 54.480778,
                }),
            new StandardlastprofilSaat(
                "L25", "BDEW_L25_Landwirtschaft", "BDEW_L25",
                "BDEW-Standardlastprofil 2025 L25 (Landwirtschaftsbetriebe), normiert auf 1.000 MWh/a; im Projekt auf den Jahresverbrauch skalieren; Feiertage über den Betriebskalender",
                "BDEW-Standardlastprofil 2025 L25 (Landwirtschaftsbetriebe): Wochenprofil aus dem Jahresmittel je Typtag (Mo–Fr Werktag, Sa Samstag, So Sonn- und Feiertag), kWh je Stunde bei 1.000 MWh/a",
                new[]
                {
                    // Januar bis Dezember [MWh]
                    92.021817, 83.116480, 89.509891, 81.760681, 80.823031, 74.670991, 77.160024, 77.160024, 78.215836, 84.486037, 89.053371, 92.021817,
                },
                new[]
                {
                    // Montag 0 bis 23 Uhr (Werktag)
                    65.567272, 58.589966, 56.052671, 54.222112, 53.552957, 58.962660, 80.258699, 134.547865, 192.446005, 182.068459, 143.889053, 145.218459,
                    130.219772, 106.334406, 102.476096, 102.250205, 105.743916, 130.343676, 184.964429, 205.584920, 161.662066, 118.288904, 97.205913, 79.517317,
                    // Dienstag 0 bis 23 Uhr (Werktag)
                    65.567272, 58.589966, 56.052671, 54.222112, 53.552957, 58.962660, 80.258699, 134.547865, 192.446005, 182.068459, 143.889053, 145.218459,
                    130.219772, 106.334406, 102.476096, 102.250205, 105.743916, 130.343676, 184.964429, 205.584920, 161.662066, 118.288904, 97.205913, 79.517317,
                    // Mittwoch 0 bis 23 Uhr (Werktag)
                    65.567272, 58.589966, 56.052671, 54.222112, 53.552957, 58.962660, 80.258699, 134.547865, 192.446005, 182.068459, 143.889053, 145.218459,
                    130.219772, 106.334406, 102.476096, 102.250205, 105.743916, 130.343676, 184.964429, 205.584920, 161.662066, 118.288904, 97.205913, 79.517317,
                    // Donnerstag 0 bis 23 Uhr (Werktag)
                    65.567272, 58.589966, 56.052671, 54.222112, 53.552957, 58.962660, 80.258699, 134.547865, 192.446005, 182.068459, 143.889053, 145.218459,
                    130.219772, 106.334406, 102.476096, 102.250205, 105.743916, 130.343676, 184.964429, 205.584920, 161.662066, 118.288904, 97.205913, 79.517317,
                    // Freitag 0 bis 23 Uhr (Werktag)
                    65.567272, 58.589966, 56.052671, 54.222112, 53.552957, 58.962660, 80.258699, 134.547865, 192.446005, 182.068459, 143.889053, 145.218459,
                    130.219772, 106.334406, 102.476096, 102.250205, 105.743916, 130.343676, 184.964429, 205.584920, 161.662066, 118.288904, 97.205913, 79.517317,
                    // Samstag 0 bis 23 Uhr (Samstag)
                    69.153333, 64.235628, 59.179772, 55.527660, 54.161062, 56.319977, 66.888116, 115.519441, 179.667489, 191.232808, 161.962363, 158.966587,
                    139.310091, 108.379840, 92.174087, 85.529635, 87.268493, 112.373459, 166.219018, 192.568653, 154.373276, 111.668231, 90.897146, 73.644772,
                    // Sonntag 0 bis 23 Uhr (Sonn- und Feiertag)
                    62.666884, 58.099737, 55.538128, 54.114361, 53.726678, 59.296484, 85.452432, 141.635890, 197.973904, 188.966336, 149.556427, 150.981689,
                    134.639110, 110.219600, 104.700662, 100.669349, 103.134098, 128.987078, 181.986438, 204.883710, 163.151621, 119.364132, 97.449680, 77.178162,
                })
        };
    }
}
