using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE SCAN-KANDIDATEN MIT VERTAUSCHTEN ANSCHLUSSLAENGEN UND DIE AUSSENWAND DES KAUFHAUSES
    // - Migrationsschritt GebaeudeAnschlusslaengenFolgereparatur.SCHRITT (Welle #496;
    // Anwenderauftrag vom 25.09.2026 "setze um: weiteren Scan-Kandidaten mit vertauschten
    // Anschlusslaengen, die Aussenwand des Kaufhauses"; Folgewelle zu #493, Konzept
    // Administrationsdialoge 7.1 (a)).
    //
    // DIESELBE BAUART WIE #493 (GebaeudeAnschlusslaengenReparatur): je Satz, Spalte und Wert -
    // das Schadensbild ist Bezeichner UND der unplausible Wert der Spalte (Toleranz 0,05), jede
    // Spalte ein eigener Handgriff, ?-Parameter, dieselben Anweisungen.
    //
    // DIE REGEL DER HERLEITUNG, in dieser Reihenfolge:
    //  (1) Fuehrt ein Satz GLEICHER Geometrie (Ausgangssatz, nicht die gerundeten
    //      EnEV-Abwandlungen) den Umfang, gilt dessen Umfang.
    //  (2) Sonst der TAUSCH von Laibung und Dachkante, wenn er fuer beide Spalten traegt:
    //      Laibung je m2 Fenster im Band des Katalogs (1,2 ... 3,4 m/m2; Median 2,57) und
    //      Dachkante nicht unter der Quadratkante 4 x Wurzel(Grundflaeche) und nahe dem Umfang
    //      aus der eigenen Geometrie U = (Aussenwand + Fenster) / (Nutzflaeche / Grundflaeche
    //      x Raumhoehe).
    //  (3) Sonst herleiten: Laibung = Verhaeltnis des Ausgangssatzes x Fensterflaeche (wie das
    //      Kaufhaus in #493), Dachkante = Umfang aus der eigenen Geometrie.
    //
    //  1. Alten-/Pflegeheime "AltenH-C-S-108", "AltenH-C-TS-169", "AltenH-C-U-252",
    //     "Pflegeheim-C-S-139", "Pflegeheim-C-TS-204", "Pflegeheim-C-U-284" (2, 4, 5, 7, 9, 10)
    //     und Schulen "Schule-C-U-202", "Schule-NE1", "Schule-NE-66" (92, 94, 96) - eine
    //     Geometrie: Aussenwand 2 132 m2, Fenster 545 m2, Dach = Grund 540 m2, Nutzflaeche
    //     3 020 m2, Laengen 185 / 985 / 0 m. Regel (2), TAUSCH:
    //     a) Abmessung_Anschluß_Fenster_Wand 185 -> 985 m (1,81 m/m2; 185 m waeren 0,34 m/m2).
    //     b) Abmessung_Anschluß_Wand_Dach 985 -> 185 m. Umfang aus der Geometrie
    //        (2 132 + 545) / (3 020 / 540 x 2,55 m) = 187,7 m (1,4 % neben 185); Quadratkante
    //        93,0 m. 985 m waeren das 10,6-Fache der Quadratkante. Die EnEV-Abwandlungen
    //        derselben Geometrie (3, 8) tragen gerundet 540 / 200 m.
    //     Kellerkante 0 m bleibt: Die EnEV-Abwandlungen fuehren sie mit 140 m, nicht als Umfang
    //     (Regel des Auftrags: nur setzen, wo der Ausgangssatz sie als Umfang fuehrt); die
    //     C-Saetze rechnen sie ohnehin mit psi = 0.
    //
    //  2. Hallenbaeder "Hallenbad-652", "Hallenbad-Sauna-750" (17, 20) - dieselben Laengen
    //     185 / 985 / 0 m samt psi 0,228 / 0,14 / 0 (von 1. uebernommen) auf eigener Geometrie
    //     (Aussenwand 1 200 m2, Fenster 420 m2, Dach = Grund 1 100 m2). Regel (2), TAUSCH:
    //     a) Fenster_Wand 185 -> 985 m (2,35 m/m2; 185 m waeren 0,44 m/m2).
    //     b) Wand_Dach 985 -> 185 m: zwischen Quadratkante 132,7 m und dem Umfang aus der
    //        Geometrie 206,3 m (Rechteck 78,5 x 14,0 m). 985 m waeren das 7,4-Fache der
    //        Quadratkante. Kellerkante 0 m bleibt (kein Ausgangssatz, psi = 0).
    //
    //  3. "Hotel-F-228", "ml_Hotel-F-228", "ml-Hotel-F-228" (54, 69, 71) - Laengen 515,2 /
    //     5 380,8 / 40 m. Die Laibung traegt (2,12 m/m2) und bleibt. Regel (1): "Kaufhalle_NE"
    //     (76) hat DIESELBE Geometrie (Aussenwand 834, Fenster 243,4, Dach 473,7, Grund 480,8,
    //     Nutzflaeche 1 138 m2) und fuehrt Wand-Dach = Aussenwand-Keller = 116,16 m:
    //     a) Wand_Dach 5 380,8 -> 116,16 m (bei 54 steht 5 380,75). 5 380,8 m waeren das
    //        61,8-Fache der Quadratkante (87,7 m).
    //     b) Abmessung_Anschluß_Außenwand_Kellerdecke 40 -> 116,16 m (weniger als die halbe
    //        Quadratkante; der Ausgangssatz fuehrt sie als Umfang).
    //     Probe: 116,16 m umschliessen 480,8 m2 als 48,08 x 10,0 m; Huelle 1 077,4 m2 / 116,16 m
    //     = 9,28 m = drei Geschosse zu 3,09 m (Nutzflaeche 1 138 > 2 x 480,8 m2 verlangt drei).
    //
    //  4. "Hotel_G_96", "ml-Hotel-G-096", "GMH-G-U-97" (42, 72, 134) - Laengen 86,6 / 295,5 /
    //     14,6 m bei Fenster 237,6 m2, Grundflaeche 431,2 m2. Regel (2), TAUSCH:
    //     a) Fenster_Wand 86,6 -> 295,5 m (1,24 m/m2 - am unteren Rand des Katalogs wie die
    //        "GMH KfW 55"-Saetze 174, 175 mit 1,20; 86,6 m waeren 0,36 m/m2).
    //     b) Wand_Dach 295,5 -> 86,6 m: Umfang aus der Geometrie (433 + 237,6) /
    //        (1 263 / 431,2 x 2,61 m) = 87,7 m; Quadratkante 83,1 m.
    //     Kellerkante 14,6 m bleibt (kein Ausgangssatz fuehrt sie als Umfang) - berichtet.
    //
    //  5. "GMH-BZ_T", "GMH-J-015" (84, 85) - DIESELBEN Laengen 86,6 / 295,5 / 14,6 m (von 4.
    //     uebernommen) auf eigener Geometrie (Aussenwand 633 m2, Fenster 307,6 m2, Grund
    //     485,2 m2, Nutzflaeche 1 430 m2). Der Tausch traegt nicht (0,96 m/m2; 86,6 m laegen
    //     unter der Quadratkante 88,1 m). Regel (3):
    //     a) Fenster_Wand 86,6 -> 382,6 m = Verhaeltnis von 4. nach dem Tausch
    //        (295,5 / 237,6 = 1,2437 m/m2) x 307,6 m2.
    //     b) Wand_Dach 295,5 -> 122,3 m = Umfang aus der Geometrie (633 + 307,6) /
    //        (1 430 / 485,2 x 2,61 m) = 122,28 m (Rechteck 51,8 x 9,4 m).
    //     Kellerkante 14,6 m bleibt - berichtet.
    //
    //  6. "Kaufhaus" (77) - Flaeche_Außenwand 10 093,99 -> 1 820,9 m2. Die 10 094 m2 stammen
    //     aus den F-Saetzen (18 012 m2 Nutzflaeche, zwoelf Geschosse); das Kaufhaus hat
    //     4 201 m2 Nutzflaeche auf 1 468,97 m2 Grundflaeche = 2,86 Geschosse zu 4,55 m =
    //     13,01 m Hoehe. Mit dem Umfang 313,8 m (Grundflaeche von "KrankenH_NE", aus #493)
    //     ist die Huelle 313,8 x 13,01 = 4 083,2 m2, abzueglich 2 262,36 m2 Fenster
    //     1 820,9 m2 Wand (Fensteranteil 55 %). Die 10 094 m2 ergaeben 949,6 m Umfang -
    //     eine 1 469-m2-Grundflaeche mit 3 m Tiefe. Der Umfang 313,8 m bleibt: Er ist der
    //     einzige belegte Umfang dieser Grundflaeche, und er traegt die Fensterflaeche
    //     (mindestens 2 262,36 / 13,01 = 173,9 m Fassadenlaenge).
    //
    // NUR DER KATALOG. Projektkopien bleiben (keine Kopie der zwanzig Saetze in der
    // Testdatenbank); nichts wird geloescht; ReadOnly spielt keine Rolle.
    //
    // EINFRIERREGEL. Keinen der zwanzig Saetze fuehrt ein Referenzprojekt (weder ueber
    // ID_Gebaeude_Stamm noch ueber den Namen; Referenzsaetze 125, 129, 142-146, 233, 56); der
    // Referenzlauf bleibt byte-gleich.
    //
    // WIEDERHOLBAR. Nach dem Lauf traegt keine Spalte mehr ihr Bild (beim Tausch steht in
    // jeder Spalte der Wert der anderen, der ausserhalb des eigenen Bilds liegt); ein zweiter
    // Lauf tut nichts. Die Nachprobe (Offen) fragt dieselben Bedingungen.
    // ====================================================================================

    /// <summary>
    /// Die Folgeberichtigung der Anschlusslängen (Scan-Kandidaten mit vertauschter Laibung und
    /// Dachkante) und der Außenwandfläche des Kaufhauses im Gebäudekatalog (Schemaschritt
    /// <see cref="SCHRITT"/>) — EINE Quelle für Migration, Werkzeug <c>Testdatenbankschema</c>,
    /// Nachzieh-Liste der Tests und Nachweis. Anweisungen und Ablauf teilt sie mit
    /// <see cref="GebaeudeAnschlusslaengenReparatur"/>.
    /// </summary>
    public static class GebaeudeAnschlusslaengenFolgereparatur
    {
        /// <summary>
        /// Die Nummer des Schemaschritts — die EINZIGE Stelle im Code, an der sie als Zahl
        /// steht (Migration, Werkzeug und Protokoll lesen sie von hier).
        /// </summary>
        public const int SCHRITT = 141;

        private const string FW = GebaeudeAnschlusslaengenReparatur.SPALTE_FENSTER_WAND;
        private const string WD = GebaeudeAnschlusslaengenReparatur.SPALTE_WAND_DACH;
        private const string KD = GebaeudeAnschlusslaengenReparatur.SPALTE_AUSSENWAND_KELLER;
        private const string AW = GebaeudeAnschlusslaengenReparatur.SPALTE_FLAECHE_AUSSENWAND;

        /// <summary>Alten-/Pflegeheime und Schulen mit 185 / 985 / 0 m (Tausch).</summary>
        public static readonly string[] HeimeUndSchulen =
        {
            "AltenH-C-S-108", "AltenH-C-TS-169", "AltenH-C-U-252",
            "Pflegeheim-C-S-139", "Pflegeheim-C-TS-204", "Pflegeheim-C-U-284",
            "Schule-C-U-202", "Schule-NE1", "Schule-NE-66",
        };

        /// <summary>Hallenbäder mit den Längen der Heime (Tausch).</summary>
        public static readonly string[] Hallenbaeder = { "Hallenbad-652", "Hallenbad-Sauna-750" };

        /// <summary>Der Hotelsatz mit 5 380,75 m Dachkante.</summary>
        public const string HOTEL_F_228 = "Hotel-F-228";

        /// <summary>Seine beiden Abwandlungen mit 5 380,8 m Dachkante.</summary>
        public static readonly string[] HotelF228Abwandlungen = { "ml_Hotel-F-228", "ml-Hotel-F-228" };

        /// <summary>Die Sätze mit 86,6 / 295,5 m auf ihrer eigenen Geometrie (Tausch).</summary>
        public static readonly string[] HotelG096 = { "Hotel_G_96", "ml-Hotel-G-096", "GMH-G-U-97" };

        /// <summary>Die Sätze mit denselben Längen auf fremder Geometrie (Herleitung).</summary>
        public static readonly string[] GmhBz = { "GMH-BZ_T", "GMH-J-015" };

        /// <summary>Das Kaufhaus.</summary>
        public const string KAUFHAUS = GebaeudeAnschlusslaengenReparatur.KAUFHAUS;

        /// <summary>Laibung der Heime im Bestand [m] (nach dem Tausch die Dachkante).</summary>
        public const double HEIME_KURZ = 185.0;

        /// <summary>Dachkante der Heime im Bestand [m] (nach dem Tausch die Laibung).</summary>
        public const double HEIME_LANG = 985.0;

        /// <summary>Dachkante von „Hotel-F-228" im Bestand [m].</summary>
        public const double HOTEL_F_228_DACHKANTE = 5380.75;

        /// <summary>Dachkante der beiden Abwandlungen im Bestand [m].</summary>
        public const double HOTEL_F_228_ABWANDLUNG_DACHKANTE = 5380.8;

        /// <summary>Kellerkante der drei Hotelsätze im Bestand [m].</summary>
        public const double HOTEL_F_228_KELLERKANTE = 40.0;

        /// <summary>Umfang des Ausgangssatzes gleicher Geometrie „Kaufhalle_NE" [m].</summary>
        public const double UMFANG_KAUFHALLE_NE = 116.16;

        /// <summary>Laibung der G-096-Sätze im Bestand [m] (nach dem Tausch die Dachkante).</summary>
        public const double G096_KURZ = 86.6;

        /// <summary>Dachkante der G-096-Sätze im Bestand [m] (nach dem Tausch die Laibung).</summary>
        public const double G096_LANG = 295.5;

        /// <summary>Laibung je m² Fenster der G-096-Sätze nach dem Tausch: 295,5 / 237,6.</summary>
        public const double LAIBUNG_JE_M2_G096 = 295.5 / 237.6;

        /// <summary>Anschlusslänge Fenster–Wand von 84, 85: 1,2437 × 307,6 m² [m].</summary>
        public const double GMH_BZ_FENSTER_WAND = 382.6;

        /// <summary>Umfang von 84, 85 aus der eigenen Geometrie [m].</summary>
        public const double GMH_BZ_UMFANG = 122.3;

        /// <summary>Außenwandfläche des Kaufhauses im Bestand [m²] (von den F-Sätzen).</summary>
        public const double KAUFHAUS_AUSSENWAND_ALT = 10094.0;

        /// <summary>Außenwandfläche des Kaufhauses: 313,8 × 4 201 / 1 468,97 × 4,55 − 2 262,36 [m²].</summary>
        public const double KAUFHAUS_AUSSENWAND = 1820.9;

        /// <summary>Die Berichtigungen, je Satz und Spalte mit Bild und neuem Wert.</summary>
        public static readonly Anschlusslaengenberichtigung[] Berichtigungen = Liste();

        private static Anschlusslaengenberichtigung[] Liste()
        {
            var l = new List<Anschlusslaengenberichtigung>();
            foreach (string name in HeimeUndSchulen)
            {
                l.Add(new Anschlusslaengenberichtigung(name, FW, HEIME_KURZ, HEIME_LANG));
                l.Add(new Anschlusslaengenberichtigung(name, WD, HEIME_LANG, HEIME_KURZ));
            }
            foreach (string name in Hallenbaeder)
            {
                l.Add(new Anschlusslaengenberichtigung(name, FW, HEIME_KURZ, HEIME_LANG));
                l.Add(new Anschlusslaengenberichtigung(name, WD, HEIME_LANG, HEIME_KURZ));
            }
            l.Add(new Anschlusslaengenberichtigung(HOTEL_F_228, WD, HOTEL_F_228_DACHKANTE, UMFANG_KAUFHALLE_NE));
            l.Add(new Anschlusslaengenberichtigung(HOTEL_F_228, KD, HOTEL_F_228_KELLERKANTE, UMFANG_KAUFHALLE_NE));
            foreach (string name in HotelF228Abwandlungen)
            {
                l.Add(new Anschlusslaengenberichtigung(name, WD, HOTEL_F_228_ABWANDLUNG_DACHKANTE, UMFANG_KAUFHALLE_NE));
                l.Add(new Anschlusslaengenberichtigung(name, KD, HOTEL_F_228_KELLERKANTE, UMFANG_KAUFHALLE_NE));
            }
            foreach (string name in HotelG096)
            {
                l.Add(new Anschlusslaengenberichtigung(name, FW, G096_KURZ, G096_LANG));
                l.Add(new Anschlusslaengenberichtigung(name, WD, G096_LANG, G096_KURZ));
            }
            foreach (string name in GmhBz)
            {
                l.Add(new Anschlusslaengenberichtigung(name, FW, G096_KURZ, GMH_BZ_FENSTER_WAND));
                l.Add(new Anschlusslaengenberichtigung(name, WD, G096_LANG, GMH_BZ_UMFANG));
            }
            l.Add(new Anschlusslaengenberichtigung(KAUFHAUS, AW, KAUFHAUS_AUSSENWAND_ALT, KAUFHAUS_AUSSENWAND));
            return l.ToArray();
        }

        /// <summary>
        /// Der ganze Schritt: jede Berichtigung, deren Bild der Satz trägt — über die Anweisungen
        /// von <see cref="GebaeudeAnschlusslaengenReparatur"/>.
        /// </summary>
        public static GebaeudeAnschlusslaengenReparatur.Bericht Ausfuehren()
            => GebaeudeAnschlusslaengenReparatur.Ausfuehren(Berichtigungen);

        /// <summary>
        /// Wie viele Spalten tragen noch ihr Schadensbild? 0 = der Schritt ist gelaufen (die
        /// Nachprobe der Migration).
        /// </summary>
        public static long Offen() => GebaeudeAnschlusslaengenReparatur.Offen(Berichtigungen);
    }
}
