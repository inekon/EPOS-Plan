using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE ANSCHLUSSLAENGEN DER GEBAEUDE-KATALOGSAETZE - Migrationsschritt
    // GebaeudeAnschlusslaengenReparatur.SCHRITT (Welle #493; Anwenderentscheid vom 24.09.2026
    // "Entscheide zu Satz 79 und Nebenbefund zu den Saetzen 80 bis 83, 37 und 77: Ersetzt
    // durch plausible Werte."; Konzept Administrationsdialoge 7.1 (a)).
    //
    // JE SATZ, SPALTE UND WERT - das Schadensbild ist Bezeichner UND der unplausible Wert
    // der Spalte (Toleranz 0,05; die Bestandswerte stehen teils in einfacher Genauigkeit,
    // z. B. 243,6999969 oder 1 392,78). Jede Spalte ist ein eigener Handgriff: Wer einen
    // Wert schon von Hand berichtigt hat, behaelt ihn.
    //
    //  1. "Krankenhaus_92-EnEV2016" (Satz 79, Abwandlung von "KrankenH_NE", Satz 78):
    //     a) Abmessung_Anschluß_Fenster_Wand 1 800 -> 4 812,0 m. Das Verhaeltnis Laibung je m2
    //        Fenster des Ausgangssatzes 78 ist 7 655,75 m / 3 016,3 m2 = 2,5381 m/m2; auf die
    //        Fensterflaeche von 79 uebertragen: 2,5381 x 1 895,9 m2 = 4 812,0 m. (1 800 m
    //        waeren 0,95 m/m2 - kleiner als jede Fensterform zulaesst, deren Laibungen die
    //        Flaeche umschliessen.)
    //     b) Flaeche_Außenwand 12 094 -> 13 214,4 m2. Die Huellflaeche (Wand + Fenster) der
    //        Geometrie ist die von 78: 12 094 + 3 016,3 = 15 110,3 m2 (Umfang 313,8 m).
    //        79 verkleinert Ost/West auf 400 m2 (Fenster gesamt 1 895,9 m2), wie in der
    //        EnEV-2016-Familie; die freigewordene Flaeche ist Wand:
    //        15 110,3 - 1 895,9 = 13 214,4 m2. Ost/West bleibt 400.
    //
    //  2. "KrankenH-F-S-136", "KrankenH-F-TS-236", "KrankenH-F-U-400",
    //     "KrankenH-F-U-400-Pinneberg" (80-83) und "gr_Hotel-G-134" (37) - alle auf der
    //     Geometrie von 78 (Grund- und Dachflaeche 1 469 m2, Sonstige 3 708,5 m2), mit
    //     Nutzflaeche 18 012 m2, Raumhoehe 2,55 m, Wand 10 094 m2, Fenster 3 062,3 m2 und
    //     DENSELBEN drei Laengen 243,7 / 7 879 / 1 392,8 m (eine Quelle):
    //     a) Abmessung_Anschluß_Fenster_Wand 243,7 -> 7 879,0 m. Die Vertauschung mit der
    //        Dachkante ist fuer diese Spalte die Loesung: 7 879 / 3 062,3 = 2,573 m/m2, gleich
    //        dem Ausgangssatz 78 (2,538 m/m2, +1,4 %). 243,7 m waeren 0,08 m/m2.
    //     b) Abmessung_Anschluß_Wand_Dach 7 879 -> 313,8 m = Umfang. 7 879 m bei 1 469 m2
    //        Dach ist das 51-Fache der Quadratkante (4 x Wurzel 1 469 = 153,3 m). Den Tausch
    //        (243,7 m) traegt die Geometrie NICHT: Huelle 13 156,3 m2 / 243,7 m = 54,0 m
    //        Gebaeudehoehe = 4,40 m je Geschoss (18 012 / 1 469 = 12,26 Geschosse) bei 2,55 m
    //        Raumhoehe. Die Grundflaeche ist die von 78, dessen Umfang 313,8 m ist
    //        (Wand-Dach = Aussenwand-Keller = 313,8): 13 156,3 / 313,8 = 41,9 m = 3,42 m je
    //        Geschoss (78: 3,54 m bei 3,0 m Raumhoehe). Die EnEV-Abwandlung derselben
    //        Geometrie ("gr_Hotel-80-EnEV2016") traegt gerundet 300 m.
    //     c) Abmessung_Anschluß_Außenwand_Kellerdecke 1 392,8 -> 313,8 m = Umfang, wie beim
    //        Ausgangssatz 78 (313,8 = 313,8). 1 392,8 m waeren das 4,4-Fache des Umfangs.
    //
    //  3. "Kaufhaus" (77) - Abwandlung der F-Saetze (Wand 10 093,99 m2, dieselben psi-Werte
    //     0,44 / 0,007 / 0,368, dieselben drei Laengen 243,7 / 7 879 / 1 392,78; Fenster je
    //     Richtung verkleinert: Sued -400, Ost/West -500, Nord +100 -> 2 262,36 m2;
    //     Grundflaeche 1 469 m2):
    //     a) Abmessung_Anschluß_Fenster_Wand 243,7 -> 5 820,8 m. Hier traegt der Tausch nicht
    //        (7 879 / 2 262,36 = 3,48 m/m2); das Verhaeltnis des Ausgangssatzes (F-Saetze nach
    //        der Berichtigung 7 879 / 3 062,3 = 2,5729 m/m2) x 2 262,36 m2 = 5 820,8 m.
    //     b) Abmessung_Anschluß_Wand_Dach 7 879 -> 313,8 m und
    //     c) Abmessung_Anschluß_Außenwand_Kellerdecke 1 392,78 -> 313,8 m: der Umfang der
    //        Grundflaeche 1 469 m2 wie bei 78 und den F-Saetzen.
    //
    // NUR DER KATALOG. Eine Projektkopie ist Rechengrundlage ihres Projekts und bleibt, wie
    // sie ist; es wird nichts geloescht. ReadOnly spielt keine Rolle. Kulturfrei: Die Werte
    // gehen als ?-Parameter hinein.
    //
    // EINFRIERREGEL. Keinen der sieben Saetze fuehrt ein Referenzprojekt (weder ueber
    // ID_Gebaeude_Stamm noch ueber den Namen; die dreizehn Referenzprojekte fuehren die Saetze
    // 125, 129, 142-146, 233, 56); der Referenzlauf bleibt byte-gleich.
    //
    // WIEDERHOLBAR. Nach dem Lauf traegt keine Spalte mehr ihr Bild; ein zweiter Lauf tut
    // nichts. Die Nachprobe (Offen) fragt dieselben Bedingungen.
    // ====================================================================================

    /// <summary>
    /// Die Berichtigung der Anschlusslängen (und der Außenwandfläche des Krankenhaussatzes)
    /// im Gebäudekatalog (Schemaschritt <see cref="SCHRITT"/>) — EINE Quelle für Migration,
    /// Werkzeug <c>Testdatenbankschema</c>, Nachzieh-Liste der Tests und Nachweis.
    /// </summary>
    public static class GebaeudeAnschlusslaengenReparatur
    {
        /// <summary>
        /// Die Nummer des Schemaschritts — die EINZIGE Stelle im Code, an der sie als Zahl
        /// steht (Migration, Werkzeug und Protokoll lesen sie von hier).
        /// </summary>
        public const int SCHRITT = 130;

        /// <summary>Der Katalog.</summary>
        public const string TABELLE = "Tab_Gebaeude_STAMM";

        /// <summary>Anschlusslänge Fenster–Wand (Laibung) [m].</summary>
        public const string SPALTE_FENSTER_WAND = "Abmessung_Anschluß_Fenster_Wand";

        /// <summary>Anschlusslänge Wand–Dach [m].</summary>
        public const string SPALTE_WAND_DACH = "Abmessung_Anschluß_Wand_Dach";

        /// <summary>Anschlusslänge Außenwand–Kellerdecke [m].</summary>
        public const string SPALTE_AUSSENWAND_KELLER = "Abmessung_Anschluß_Außenwand_Kellerdecke";

        /// <summary>Außenwandfläche [m²].</summary>
        public const string SPALTE_FLAECHE_AUSSENWAND = "Flaeche_Außenwand";

        /// <summary>Die Toleranz des Schadensbilds (einfache Genauigkeit des Bestands).</summary>
        public const double TOLERANZ = 0.05;

        /// <summary>Der Krankenhaussatz.</summary>
        public const string KRANKENHAUS = "Krankenhaus_92-EnEV2016";

        /// <summary>Das Kaufhaus.</summary>
        public const string KAUFHAUS = "Kaufhaus";

        /// <summary>Die fünf Sätze der F-Quelle (Krankenhaus F-Familie und großes Hotel).</summary>
        public static readonly string[] FQuelle =
        {
            "KrankenH-F-S-136", "KrankenH-F-TS-236", "KrankenH-F-U-400",
            "KrankenH-F-U-400-Pinneberg", "gr_Hotel-G-134",
        };

        /// <summary>Laibung je m² Fenster des Ausgangssatzes „KrankenH_NE": 7 655,75 / 3 016,3.</summary>
        public const double LAIBUNG_JE_M2_78 = 7655.75 / 3016.3;

        /// <summary>Laibung je m² Fenster der F-Quelle nach der Berichtigung: 7 879 / 3 062,3.</summary>
        public const double LAIBUNG_JE_M2_F = 7879.0 / 3062.3;

        /// <summary>Umfang der Grundfläche 1 469 m² (Ausgangssatz „KrankenH_NE") [m].</summary>
        public const double UMFANG = 313.8;

        /// <summary>Anschlusslänge Fenster–Wand des Krankenhaussatzes: 2,5381 × 1 895,9 m² [m].</summary>
        public const double KRANKENHAUS_FENSTER_WAND = 4812.0;

        /// <summary>Außenwand des Krankenhaussatzes: 15 110,3 − 1 895,9 m² [m²].</summary>
        public const double KRANKENHAUS_AUSSENWAND = 13214.4;

        /// <summary>Anschlusslänge Fenster–Wand der F-Quelle (Tausch mit der Dachkante) [m].</summary>
        public const double F_FENSTER_WAND = 7879.0;

        /// <summary>Anschlusslänge Fenster–Wand des Kaufhauses: 2,5729 × 2 262,36 m² [m].</summary>
        public const double KAUFHAUS_FENSTER_WAND = 5820.8;

        /// <summary>Die Berichtigungen, je Satz und Spalte mit Bild und neuem Wert.</summary>
        public static readonly Anschlusslaengenberichtigung[] Berichtigungen = Liste();

        private static Anschlusslaengenberichtigung[] Liste()
        {
            var l = new List<Anschlusslaengenberichtigung>
            {
                new Anschlusslaengenberichtigung(KRANKENHAUS, SPALTE_FENSTER_WAND, 1800.0, KRANKENHAUS_FENSTER_WAND),
                new Anschlusslaengenberichtigung(KRANKENHAUS, SPALTE_FLAECHE_AUSSENWAND, 12094.0, KRANKENHAUS_AUSSENWAND),
            };
            foreach (string name in FQuelle)
            {
                l.Add(new Anschlusslaengenberichtigung(name, SPALTE_FENSTER_WAND, 243.7, F_FENSTER_WAND));
                l.Add(new Anschlusslaengenberichtigung(name, SPALTE_WAND_DACH, 7879.0, UMFANG));
                l.Add(new Anschlusslaengenberichtigung(name, SPALTE_AUSSENWAND_KELLER, 1392.8, UMFANG));
            }
            l.Add(new Anschlusslaengenberichtigung(KAUFHAUS, SPALTE_FENSTER_WAND, 243.7, KAUFHAUS_FENSTER_WAND));
            l.Add(new Anschlusslaengenberichtigung(KAUFHAUS, SPALTE_WAND_DACH, 7879.0, UMFANG));
            l.Add(new Anschlusslaengenberichtigung(KAUFHAUS, SPALTE_AUSSENWAND_KELLER, 1392.8, UMFANG));
            return l.ToArray();
        }

        // =================================================================
        //  Die Anweisungen - je Spalte eine feste Anweisung, kein zusammengesetzter Text
        //  zur Laufzeit. Parameter der Berichtigung: neu, Bezeichner, Bild von, Bild bis;
        //  der Zaehlung: Bezeichner, Bild von, Bild bis.
        // =================================================================

        /// <summary>Berichtigung der Anschlusslänge Fenster–Wand.</summary>
        public const string SQL_FENSTER_WAND =
            "UPDATE \"" + TABELLE + "\" SET \"" + SPALTE_FENSTER_WAND + "\" = ? WHERE \"Bezeichner\" = ? " +
            "AND \"" + SPALTE_FENSTER_WAND + "\" > ? AND \"" + SPALTE_FENSTER_WAND + "\" < ?";

        /// <summary>Zählung des Bilds Fenster–Wand.</summary>
        public const string SQL_FENSTER_WAND_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"" + TABELLE + "\" WHERE \"Bezeichner\" = ? " +
            "AND \"" + SPALTE_FENSTER_WAND + "\" > ? AND \"" + SPALTE_FENSTER_WAND + "\" < ?";

        /// <summary>Berichtigung der Anschlusslänge Wand–Dach.</summary>
        public const string SQL_WAND_DACH =
            "UPDATE \"" + TABELLE + "\" SET \"" + SPALTE_WAND_DACH + "\" = ? WHERE \"Bezeichner\" = ? " +
            "AND \"" + SPALTE_WAND_DACH + "\" > ? AND \"" + SPALTE_WAND_DACH + "\" < ?";

        /// <summary>Zählung des Bilds Wand–Dach.</summary>
        public const string SQL_WAND_DACH_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"" + TABELLE + "\" WHERE \"Bezeichner\" = ? " +
            "AND \"" + SPALTE_WAND_DACH + "\" > ? AND \"" + SPALTE_WAND_DACH + "\" < ?";

        /// <summary>Berichtigung der Anschlusslänge Außenwand–Kellerdecke.</summary>
        public const string SQL_AUSSENWAND_KELLER =
            "UPDATE \"" + TABELLE + "\" SET \"" + SPALTE_AUSSENWAND_KELLER + "\" = ? WHERE \"Bezeichner\" = ? " +
            "AND \"" + SPALTE_AUSSENWAND_KELLER + "\" > ? AND \"" + SPALTE_AUSSENWAND_KELLER + "\" < ?";

        /// <summary>Zählung des Bilds Außenwand–Kellerdecke.</summary>
        public const string SQL_AUSSENWAND_KELLER_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"" + TABELLE + "\" WHERE \"Bezeichner\" = ? " +
            "AND \"" + SPALTE_AUSSENWAND_KELLER + "\" > ? AND \"" + SPALTE_AUSSENWAND_KELLER + "\" < ?";

        /// <summary>Berichtigung der Außenwandfläche.</summary>
        public const string SQL_FLAECHE_AUSSENWAND =
            "UPDATE \"" + TABELLE + "\" SET \"" + SPALTE_FLAECHE_AUSSENWAND + "\" = ? WHERE \"Bezeichner\" = ? " +
            "AND \"" + SPALTE_FLAECHE_AUSSENWAND + "\" > ? AND \"" + SPALTE_FLAECHE_AUSSENWAND + "\" < ?";

        /// <summary>Zählung des Bilds Außenwandfläche.</summary>
        public const string SQL_FLAECHE_AUSSENWAND_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"" + TABELLE + "\" WHERE \"Bezeichner\" = ? " +
            "AND \"" + SPALTE_FLAECHE_AUSSENWAND + "\" > ? AND \"" + SPALTE_FLAECHE_AUSSENWAND + "\" < ?";

        /// <summary>Die Berichtigungsanweisung einer Spalte.</summary>
        public static string SqlBerichtigung(string spalte)
        {
            switch (spalte)
            {
                case SPALTE_FENSTER_WAND: return SQL_FENSTER_WAND;
                case SPALTE_WAND_DACH: return SQL_WAND_DACH;
                case SPALTE_AUSSENWAND_KELLER: return SQL_AUSSENWAND_KELLER;
                case SPALTE_FLAECHE_AUSSENWAND: return SQL_FLAECHE_AUSSENWAND;
                default: throw new ArgumentException("Unbekannte Spalte: " + spalte, nameof(spalte));
            }
        }

        /// <summary>Die Zählanweisung einer Spalte.</summary>
        public static string SqlZaehlung(string spalte)
        {
            switch (spalte)
            {
                case SPALTE_FENSTER_WAND: return SQL_FENSTER_WAND_ZAEHLUNG;
                case SPALTE_WAND_DACH: return SQL_WAND_DACH_ZAEHLUNG;
                case SPALTE_AUSSENWAND_KELLER: return SQL_AUSSENWAND_KELLER_ZAEHLUNG;
                case SPALTE_FLAECHE_AUSSENWAND: return SQL_FLAECHE_AUSSENWAND_ZAEHLUNG;
                default: throw new ArgumentException("Unbekannte Spalte: " + spalte, nameof(spalte));
            }
        }

        // =================================================================
        //  Der Schritt
        // =================================================================

        /// <summary>Was ein Lauf von <see cref="Ausfuehren"/> getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Die Berichtigungen, je Eintrag „Satz: Spalte alt -> neu".</summary>
            public List<string> Berichtigt { get; } = new List<string>();

            /// <summary>Eine Zeile für Protokoll und Konsole.</summary>
            public string Text()
            {
                return "berichtigt " + Berichtigt.Count.ToString(CultureInfo.InvariantCulture) +
                       (Berichtigt.Count > 0 ? " (" + string.Join("; ", Berichtigt) + ")" : "");
            }
        }

        /// <summary>
        /// Der ganze Schritt: jede Berichtigung, deren Bild der Satz trägt. Jeder Handgriff
        /// trifft nur sein Bild und ist für sich wiederholbar.
        /// </summary>
        public static Bericht Ausfuehren() => Ausfuehren(Berichtigungen);

        /// <summary>
        /// Eine Liste von Berichtigungen ausführen — dieselben Anweisungen für jeden Schritt, der
        /// Katalogspalten nach Satz, Spalte und Schadensbild berichtigt (auch
        /// <see cref="GebaeudeAnschlusslaengenFolgereparatur"/>). Jeder Handgriff trifft nur sein
        /// Bild und ist für sich wiederholbar; die Liste läuft in ihrer Reihenfolge.
        /// </summary>
        public static Bericht Ausfuehren(IEnumerable<Anschlusslaengenberichtigung> berichtigungen)
        {
            var b = new Bericht();
            foreach (Anschlusslaengenberichtigung k in berichtigungen)
            {
                if (Zahl(SqlZaehlung(k.Spalte), P(k.Bezeichner), P(k.BildVon), P(k.BildBis)) == 0) continue;
                int n = DataRepository.ExecuteNonQuery(SqlBerichtigung(k.Spalte), P(k.Neu), P(k.Bezeichner),
                                                       P(k.BildVon), P(k.BildBis));
                if (n > 0)
                    b.Berichtigt.Add(k.Bezeichner + ": " + k.Spalte + " " + Text(k.Bild) + " -> " + Text(k.Neu));
            }
            return b;
        }

        /// <summary>
        /// Wie viele Spalten tragen noch ihr Schadensbild? 0 = der Schritt ist gelaufen (die
        /// Nachprobe der Migration).
        /// </summary>
        public static long Offen() => Offen(Berichtigungen);

        /// <summary>Wie viele Spalten einer Liste tragen noch ihr Schadensbild?</summary>
        public static long Offen(IEnumerable<Anschlusslaengenberichtigung> berichtigungen)
        {
            long offen = 0;
            foreach (Anschlusslaengenberichtigung k in berichtigungen)
                offen += Zahl(SqlZaehlung(k.Spalte), P(k.Bezeichner), P(k.BildVon), P(k.BildBis));
            return offen;
        }

        private static DbParam P(object wert) => new DbParam("?", wert);

        private static string Text(double wert) => wert.ToString(CultureInfo.InvariantCulture);

        private static long Zahl(string sql, params DbParam[] parameter)
        {
            object wert = DataRepository.ExecuteScalar(sql, parameter);
            if (wert == null || wert == DBNull.Value) return 0;
            return Convert.ToInt64(wert, CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Eine Berichtigung im Gebäudekatalog: Satz, Spalte, der unplausible Wert (das Bild,
    /// Toleranz <see cref="GebaeudeAnschlusslaengenReparatur.TOLERANZ"/>) und der neue Wert.
    /// </summary>
    public sealed class Anschlusslaengenberichtigung
    {
        /// <summary>Legt die Berichtigung an.</summary>
        public Anschlusslaengenberichtigung(string bezeichner, string spalte, double bild, double neu)
        {
            Bezeichner = bezeichner;
            Spalte = spalte;
            Bild = bild;
            Neu = neu;
        }

        /// <summary>Der Bezeichner des Katalogsatzes.</summary>
        public string Bezeichner { get; }

        /// <summary>Die Spalte (eine der Spaltenkonstanten von <see cref="GebaeudeAnschlusslaengenReparatur"/>).</summary>
        public string Spalte { get; }

        /// <summary>Der unplausible Wert des Schadensbilds.</summary>
        public double Bild { get; }

        /// <summary>Untere Grenze des Bilds (ausschließlich).</summary>
        public double BildVon => Bild - GebaeudeAnschlusslaengenReparatur.TOLERANZ;

        /// <summary>Obere Grenze des Bilds (ausschließlich).</summary>
        public double BildBis => Bild + GebaeudeAnschlusslaengenReparatur.TOLERANZ;

        /// <summary>Der Wert danach.</summary>
        public double Neu { get; }
    }
}
