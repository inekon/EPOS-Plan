using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE EINDEUTIG UNPLAUSIBLEN ANSCHLUSSLAENGEN NACH #496 - Migrationsschritt
    // GebaeudeAnschlusslaengenDritteReparatur.SCHRITT (Welle #505; Anwenderentscheid vom
    // 25.09.2026 "Empfehlung uebernommen - eindeutig unplausible Werte berichtigen, den Rest
    // lassen"; Berichtstabelle des Nachtrags #496 in Referenzlaeufe/LIESMICH.md; Konzept
    // Administrationsdialoge 7.1 (a)).
    //
    // DIESELBE BAUART WIE #493 UND #496: je Satz, Spalte und Wert - das Schadensbild ist
    // Bezeichner UND der unplausible Wert der Spalte (Toleranz 0,05), jede Spalte ein eigener
    // Handgriff, ?-Parameter, dieselben Anweisungen. NEU ist allein das Bild "leer" (die Zelle
    // ist NULL) fuer die Laibung dreier Saetze - eine feste Anweisung mehr bei
    // GebaeudeAnschlusslaengenReparatur (SQL_FENSTER_WAND_LEER), weil ein Wertebereich eine
    // leere Zelle nicht trifft.
    //
    // DIE REGEL DER HERLEITUNG (wie #496): (1) ein Satz gleicher Geometrie (Zwilling,
    // Ausgangssatz) gibt seinen Wert; (2) sonst Laibung = Laibung je m2 Fenster der Quelle x
    // eigene Fensterflaeche; (3) Kanten = Umfang aus der eigenen Geometrie
    // U = (Aussenwand + Fenster) / (Nutzflaeche / Grundflaeche x Raumhoehe), nicht unter der
    // Quadratkante 4 x Wurzel(Grundflaeche).
    //
    //  1. LAIBUNG 0 m ODER LEER (Abmessung_Anschluß_Fenster_Wand):
    //     a) "Pflegeheim-122-EnEV2016" (6): 0 -> 540 m - der Zwilling "Pflegeheim-C-S-140-EnEV2016"
    //        (8, dieselbe Geometrie: Wand 2 132, Fenster 545, Grund 540 m2) fuehrt 540 m
    //        (0,99 m/m2, wie auch "AltenH-C-S-110-EnEV2016", 3).
    //     b) "Industriehalle-320" (15): 0 -> 16 000 m - der Zwilling "Industrie_ne_81" (14,
    //        Wand 15 100, Fenster 6 400, Grund 36 587 m2) fuehrt 16 000 m = 2,5 m/m2 und ist
    //        in dieser Spalte selbst plausibel.
    //     c) "Hotel_H_BZ" (43), "kl_Hotel-H-086" (64): 0 -> 391,5 m und "Verw_H_75" (117):
    //        leer -> 391,5 m - die Zwillinge "kl_Hotel-I-080" (65), "ml-Hotel-NE-68" (73),
    //        "Verw_I_33" (118) fuehren auf derselben Geometrie (Wand 613,1, Fenster 156,6,
    //        Grund 254,9 m2) 391,5 m = 2,5 m/m2.
    //     d) "Büro1-F-U-89" (105), "Bürogebäude_F_72" (107): leer -> 1 462,1 m - Verwaltung F
    //        ("Verw_F_147", 115): 476,8 / 164,36 = 2,901 m/m2 x 504 m2.
    //     e) "Bürogebäude KfW 55" (106): 0 -> 2 875 m - Verhaeltnis der NE-/I-Saetze
    //        2,5 m/m2 x 1 150 m2 (kein Satz gleicher Geometrie).
    //     f) "KMH-G-U-120" (207): 0 -> 238,3 m - KMH G ("KMH-G-S-84", "KMH-G-U-150"; 206, 209):
    //        268,6 / 112,015 = 2,398 m/m2 x 99,37 m2.
    //     Die Kanten dieser Saetze (0 oder leer) bleiben: Wo sie 0 sind, ist auch psi 0; bei
    //     105, 107 ist psi leer (berichtet, Anwenderentscheid "den Rest lassen").
    //
    //  2. GERUNDETE EnEV-LAIBUNGEN (0,12 ... 0,25 m/m2):
    //     a) "Hallenbad-Umkl-140-EnEV2016" (23): 50 -> 865,1 m - Ausgangssatz
    //        "Hallenbad-Umkl-180" (24): 142 / 70,4 = 2,017 m/m2 x 428,9 m2.
    //     b) "gr_Hotel-80-EnEV2016" (34): 600 -> 6 164,4 m - Ausgangssatz gleicher Geometrie
    //        (Wand 10 094, Grund 1 469, Nutzflaeche 18 012 m2) sind die F-Saetze samt
    //        "gr_Hotel-G-134" (37) nach #493: 7 879 / 3 062,3 = 2,5729 m/m2 x 2 395,9 m2. Die
    //        Kanten 300 m (Umfang der Geometrie 313,8 m, gerundet) bleiben.
    //     c) "Bürogebäude_gross-30-EnEV2016" (108): 330 -> 4 460 m - kein Ausgangssatz gleicher
    //        Geometrie; Verhaeltnis der NE-/I-Saetze 2,5 m/m2 x 1 784 m2.
    //
    //  3. KELLERKANTEN 14,6 m (Abmessung_Anschluß_Außenwand_Kellerdecke):
    //     a) "Hotel_G_96", "ml-Hotel-G-096", "GMH-G-U-97" (42, 72, 134): 14,6 -> 86,6 m = der
    //        Umfang, den #496 als Dachkante gesetzt hat (Geometrie 87,7 m; Quadratkante 83,1 m).
    //     b) "GMH-BZ_T", "GMH-J-015" (84, 85): 14,6 -> 122,3 m = der Umfang aus der eigenen
    //        Geometrie (#496, Dachkante).
    //     14,6 m waeren weniger als ein Fuenftel der Quadratkante.
    //
    //  4. "Industrie_ne_81" (14): Dachkante UND Kellerkante 7 337,4 -> 2 362,1 m = Umfang aus
    //     der eigenen Geometrie (15 100 + 6 400) / (39 645 / 36 587 x 8,4 m) = 2 362,1 m;
    //     Quadratkante 765,1 m. 7 337,4 m waeren das 9,6-Fache der Quadratkante. Die Laibung
    //     16 000 m (2,5 m/m2) traegt und bleibt.
    //
    // NICHT GEAENDERT (Anwenderentscheid): Kellerkanten 0 m der Heime, Schulen und
    // Hallenbaeder (psi 0), die Saetze 46, 57, 120 (Laibung 0,32 ... 0,64 m/m2), die Gruppe
    // "nur Dachkante" (geneigte Daecher), die Referenzsaetze 145, 146.
    //
    // NUR DER KATALOG. Projektkopien bleiben (keine Kopie der achtzehn Saetze in der
    // Testdatenbank); nichts wird geloescht; ReadOnly spielt keine Rolle.
    //
    // EINFRIERREGEL. Keinen der achtzehn Saetze fuehrt ein Referenzprojekt (weder ueber
    // ID_Gebaeude_Stamm noch ueber den Namen; Referenzsaetze 125, 129, 142-146, 233, 56); der
    // Referenzlauf bleibt byte-gleich.
    //
    // WIEDERHOLBAR. Nach dem Lauf traegt keine Spalte mehr ihr Bild; ein zweiter Lauf tut
    // nichts. Die Nachprobe (Offen) fragt dieselben Bedingungen.
    // ====================================================================================

    /// <summary>
    /// Die dritte Berichtigung der Anschlusslängen im Gebäudekatalog (Schemaschritt
    /// <see cref="SCHRITT"/>): Laibungen 0 m oder leer, gerundete EnEV-Laibungen, Kellerkanten
    /// 14,6 m und die Kanten von „Industrie_ne_81" — EINE Quelle für Migration, Werkzeug
    /// <c>Testdatenbankschema</c>, Nachzieh-Liste der Tests und Nachweis. Anweisungen und Ablauf
    /// teilt sie mit <see cref="GebaeudeAnschlusslaengenReparatur"/>.
    /// </summary>
    public static class GebaeudeAnschlusslaengenDritteReparatur
    {
        /// <summary>
        /// Die Nummer des Schemaschritts — die EINZIGE Stelle im Code, an der sie als Zahl
        /// steht (Migration, Werkzeug und Protokoll lesen sie von hier).
        /// </summary>
        public const int SCHRITT = 142;

        private const string FW = GebaeudeAnschlusslaengenReparatur.SPALTE_FENSTER_WAND;
        private const string WD = GebaeudeAnschlusslaengenReparatur.SPALTE_WAND_DACH;
        private const string KD = GebaeudeAnschlusslaengenReparatur.SPALTE_AUSSENWAND_KELLER;

        /// <summary>Laibung je m² Fenster der NE-/I-Sätze [m/m²].</summary>
        public const double LAIBUNG_JE_M2_NE = 2.5;

        /// <summary>Laibung je m² Fenster von Verwaltung F: 476,8 / 164,36.</summary>
        public const double LAIBUNG_JE_M2_VERW_F = 476.8 / 164.36;

        /// <summary>Laibung je m² Fenster von KMH G: 268,6 / 112,015.</summary>
        public const double LAIBUNG_JE_M2_KMH_G = 268.6 / 112.015;

        /// <summary>Laibung je m² Fenster der Hallenbad-Umkleide „Hallenbad-Umkl-180": 142 / 70,4.</summary>
        public const double LAIBUNG_JE_M2_UMKLEIDE = 142.0 / 70.4;

        /// <summary>Die Laibungen 0 m, je Satz mit neuem Wert.</summary>
        public static readonly (string Name, double Neu)[] LaibungNull =
        {
            ("Pflegeheim-122-EnEV2016", 540.0),
            ("Industriehalle-320", 16000.0),
            ("Hotel_H_BZ", 391.5),
            ("kl_Hotel-H-086", 391.5),
            ("Bürogebäude KfW 55", 2875.0),
            ("KMH-G-U-120", 238.3),
        };

        /// <summary>Die leeren Laibungen, je Satz mit neuem Wert.</summary>
        public static readonly (string Name, double Neu)[] LaibungLeer =
        {
            ("Verw_H_75", 391.5),
            ("Büro1-F-U-89", 1462.1),
            ("Bürogebäude_F_72", 1462.1),
        };

        /// <summary>Die gerundeten EnEV-Laibungen, je Satz mit Bild und neuem Wert.</summary>
        public static readonly (string Name, double Bild, double Neu)[] LaibungGerundet =
        {
            ("Hallenbad-Umkl-140-EnEV2016", 50.0, 865.1),
            ("gr_Hotel-80-EnEV2016", 600.0, 6164.4),
            ("Bürogebäude_gross-30-EnEV2016", 330.0, 4460.0),
        };

        /// <summary>Kellerkante der G-096- und GMH-BZ-Sätze im Bestand [m].</summary>
        public const double KELLERKANTE_ALT = 14.6;

        /// <summary>Die Kellerkanten 14,6 m, je Satz mit dem Umfang aus #496.</summary>
        public static readonly (string Name, double Neu)[] Kellerkanten =
        {
            ("Hotel_G_96", GebaeudeAnschlusslaengenFolgereparatur.G096_KURZ),
            ("ml-Hotel-G-096", GebaeudeAnschlusslaengenFolgereparatur.G096_KURZ),
            ("GMH-G-U-97", GebaeudeAnschlusslaengenFolgereparatur.G096_KURZ),
            ("GMH-BZ_T", GebaeudeAnschlusslaengenFolgereparatur.GMH_BZ_UMFANG),
            ("GMH-J-015", GebaeudeAnschlusslaengenFolgereparatur.GMH_BZ_UMFANG),
        };

        /// <summary>Der Industriesatz.</summary>
        public const string INDUSTRIE = "Industrie_ne_81";

        /// <summary>Dach- und Kellerkante des Industriesatzes im Bestand [m].</summary>
        public const double INDUSTRIE_KANTE_ALT = 7337.4;

        /// <summary>Umfang des Industriesatzes aus der eigenen Geometrie [m].</summary>
        public const double INDUSTRIE_UMFANG = 2362.1;

        /// <summary>Die Berichtigungen, je Satz und Spalte mit Bild und neuem Wert.</summary>
        public static readonly Anschlusslaengenberichtigung[] Berichtigungen = Liste();

        private static Anschlusslaengenberichtigung[] Liste()
        {
            var l = new List<Anschlusslaengenberichtigung>();
            foreach ((string name, double neu) in LaibungNull)
                l.Add(new Anschlusslaengenberichtigung(name, FW, 0.0, neu));
            foreach ((string name, double neu) in LaibungLeer)
                l.Add(Anschlusslaengenberichtigung.AusLeer(name, FW, neu));
            foreach ((string name, double bild, double neu) in LaibungGerundet)
                l.Add(new Anschlusslaengenberichtigung(name, FW, bild, neu));
            foreach ((string name, double neu) in Kellerkanten)
                l.Add(new Anschlusslaengenberichtigung(name, KD, KELLERKANTE_ALT, neu));
            l.Add(new Anschlusslaengenberichtigung(INDUSTRIE, WD, INDUSTRIE_KANTE_ALT, INDUSTRIE_UMFANG));
            l.Add(new Anschlusslaengenberichtigung(INDUSTRIE, KD, INDUSTRIE_KANTE_ALT, INDUSTRIE_UMFANG));
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
