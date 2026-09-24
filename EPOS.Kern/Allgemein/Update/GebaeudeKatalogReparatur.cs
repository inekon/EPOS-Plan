using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE REPARATUR DER GEBAEUDE-KATALOGSAETZE - Migrationsschritt GebaeudeKatalogReparatur.SCHRITT
    // (Welle #485; Anwenderentscheid "Empfehlung uebernehmen" zu den Katalogsaetzen, die
    // nach Schritt 121 dem Anwender vorgelegt waren; Konzept Administrationsdialoge 7.1 (a)).
    //
    // DREI SCHADENSBILDER, JE SATZ NACH BEZEICHNER UND WERTEN:
    //
    //  1. "Krankenhaus_92-EnEV2016": U-Wert Fenster 0,09 W/(m2K) - unter der Grenze
    //     0,1 ... 6,0 des Editors und des Stundenmodells - und eine Nordfensterflaeche
    //     von 10 000 m2 (gesamt 11 646 m2 bei 20 012 m2 Nutzflaeche). Der Satz ist eine
    //     Abwandlung von "KrankenH_NE" (gleiche Flaechen, 400,24 Bewohner): Die
    //     Schwestersaetze tragen Nord 250 bzw. 296 m2 und 3 016 bis 3 062 m2 gesamt. Neu:
    //     U-Wert Fenster 1,3, Nord 250 m2; die gesamte Fensterflaeche wird wie im Editor
    //     gebildet (Sued + Ost/West + Nord, GebaeudeArbeitsstand.Fenstergesamt), Sued und
    //     Ost/West bleiben. Beide Teilreparaturen haben ihr EIGENES Bild (U-Wert um 0,09,
    //     Nord genau 10 000): Wer eines schon berichtigt hat, behaelt es.
    //
    //  2. Vier Saetze ohne "Flaeche je Nutzer" (EFH-BZ2, KrankenH-F-U-400, KMEH-M-U-54,
    //     Z-EFH-A-S-126). Bei allen uebrigen Saetzen des Katalogs gilt
    //     Flaeche je Nutzer = Wohnflaeche / Bewohner (Abweichung hoechstens 0,01); die
    //     Werte folgen derselben Regel, auf zwei Nachkommastellen wie im Bestand.
    //     KrankenH-F-U-400 fuehrt 360 statt 360,24 Bewohner wie seine drei Geschwister
    //     (18 012 m2 / 50 m2 = 360,24) und bekommt deren Wert mit. Getroffen wird nur ein
    //     Satz mit leerer Flaeche je Nutzer UND genau der Wohnflaeche und Bewohnerzahl des
    //     Befunds.
    //
    //  3. Acht Testreste (sieben "Z2-EFH-A-S*" und "EFH-BZ2 XXX"), alle ohne Flaeche je
    //     Nutzer und damit im Editor nicht speicherbar. Geloescht wird ein Satz nur, wenn
    //     ihn KEINE Projektkopie fuehrt - weder ueber den Katalogverweis
    //     (Tab_Gebaeude.ID_Gebaeude_Stamm, Schritt 121) noch ueber den Namen (gross/klein
    //     egal, wie die Loeschsperre GebaeudeStammCtrl.Projektverwendung). Ein benutzter
    //     Satz bleibt stehen und wird im Protokoll mit seinen Projekten genannt.
    //
    // NUR DER KATALOG. Eine Projektkopie ist Rechengrundlage ihres Projekts und bleibt,
    // wie sie ist. ReadOnly spielt keine Rolle - ein Auslieferungssatz mit dem Bild ist
    // so falsch wie ein eigener. Kulturfrei: Die Werte gehen als ?-Parameter hinein.
    //
    // EINFRIERREGEL. Keiner der dreizehn Saetze ist einem Referenzprojekt zugeordnet
    // (Befund #473); der Referenzlauf bleibt byte-gleich.
    //
    // WIEDERHOLBAR. Nach dem Lauf traegt kein Satz mehr sein Bild; ein zweiter Lauf tut
    // nichts. Die Nachprobe (Offen) fragt dieselben Bedingungen.
    // ====================================================================================

    /// <summary>
    /// Die Reparatur der Gebäude-Katalogsätze (Schemaschritt <see cref="SCHRITT"/>) —
    /// EINE Quelle für Migration, Werkzeug <c>Testdatenbankschema</c>, Nachzieh-Liste der
    /// Tests und Nachweis.
    /// </summary>
    public static class GebaeudeKatalogReparatur
    {
        /// <summary>
        /// Die Nummer des Schemaschritts — die EINZIGE Stelle im Code, an der sie als Zahl
        /// steht (Migration, Werkzeug und Protokoll lesen sie von hier).
        /// </summary>
        public const int SCHRITT = 126;

        /// <summary>Der Katalog.</summary>
        public const string TABELLE = "Tab_Gebaeude_STAMM";

        /// <summary>Der Krankenhaussatz mit U-Wert Fenster 0,09 und 10 000 m² Nordfenster.</summary>
        public const string KRANKENHAUS = "Krankenhaus_92-EnEV2016";

        /// <summary>Der neue U-Wert Fenster des Krankenhaussatzes [W/(m²K)].</summary>
        public const double U_FENSTER_NEU = 1.3;

        /// <summary>Das Schadensbild des U-Werts: 0,09 in einfacher Genauigkeit.</summary>
        public const double U_FENSTER_BILD_VON = 0.0895, U_FENSTER_BILD_BIS = 0.0905;

        /// <summary>Die Nordfensterfläche des Schadensbilds [m²].</summary>
        public const double NORD_BILD = 10000.0;

        /// <summary>Die neue Nordfensterfläche [m²] — die des Ausgangssatzes „KrankenH_NE".</summary>
        public const double NORD_NEU = 250.0;

        /// <summary>
        /// Die vier Sätze ohne „Fläche je Nutzer": Bezeichner, Wohnfläche und Bewohner des
        /// Schadensbilds, dann Bewohner und Fläche je Nutzer danach.
        /// </summary>
        public static readonly FlaecheNutzerSatz[] FlaecheNutzerSaetze =
        {
            new FlaecheNutzerSatz("EFH-BZ2",            240.0,   6.0,   6.0,    40.0),
            new FlaecheNutzerSatz("KrankenH-F-U-400", 18012.0, 360.0, 360.24,   50.0),
            new FlaecheNutzerSatz("KMEH-M-U-54",        572.0,  18.0,  18.0,    31.78),
            new FlaecheNutzerSatz("Z-EFH-A-S-126",      201.0,   7.0,   7.0,    28.71),
        };

        /// <summary>Die acht Testreste — gelöscht nur, wenn unbenutzt.</summary>
        public static readonly string[] Testreste =
        {
            "Z2-EFH-A-S-126", "Z2-EFH-A-S-12", "Z2-EFH-A-S-1", "Z2-EFH-A-S-",
            "Z2-EFH-A-S", "Z2-EFH-A-S-test", "Z2-EFH-A-S-test2", "EFH-BZ2 XXX",
        };

        // =================================================================
        //  Die Anweisungen
        // =================================================================

        private const string WO_U_FENSTER =
            " WHERE \"Bezeichner\" = ? AND \"k_Wert_Fenster\" > ? AND \"k_Wert_Fenster\" < ?";

        /// <summary>U-Wert Fenster des Krankenhaussatzes. Parameter: neu, Bezeichner, Bild von, Bild bis.</summary>
        public const string SQL_U_FENSTER =
            "UPDATE \"" + TABELLE + "\" SET \"k_Wert_Fenster\" = ?" + WO_U_FENSTER;

        /// <summary>Zählung des U-Wert-Bilds. Parameter: Bezeichner, Bild von, Bild bis.</summary>
        public const string SQL_U_FENSTER_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"" + TABELLE + "\"" + WO_U_FENSTER;

        private const string WO_NORD =
            " WHERE \"Bezeichner\" = ? AND \"Fensterflaeche_Nord\" = ?";

        /// <summary>
        /// Nordfenster des Krankenhaussatzes und die gesamte Fensterfläche wie im Editor:
        /// Süd + (Ost + West, sonst Ost/West) + Nord. Parameter: Nord neu, Nord neu,
        /// Bezeichner, Nord des Bilds.
        /// </summary>
        public const string SQL_NORD =
            "UPDATE \"" + TABELLE + "\" SET \"Fensterflaeche_Nord\" = ?, " +
            "\"gesamte_Fensterflaeche\" = COALESCE(\"Fensterflaeche_Sued\", 0) + " +
            "(CASE WHEN \"Fensterflaeche_Ost\" IS NOT NULL AND \"Fensterflaeche_West\" IS NOT NULL " +
            "THEN \"Fensterflaeche_Ost\" + \"Fensterflaeche_West\" " +
            "ELSE COALESCE(\"Fensterflaeche_Ost_West\", 0) END) + ?" + WO_NORD;

        /// <summary>Zählung des Nordfenster-Bilds. Parameter: Bezeichner, Nord des Bilds.</summary>
        public const string SQL_NORD_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"" + TABELLE + "\"" + WO_NORD;

        private const string WO_FLAECHE_NUTZER =
            " WHERE \"Bezeichner\" = ? AND \"Flaeche_Nutzer\" IS NULL " +
            "AND \"Wohnflaeche_gesamt\" = ? AND \"Bewohner\" = ?";

        /// <summary>
        /// Fläche je Nutzer (und Bewohner) eines Satzes. Parameter: Fläche je Nutzer,
        /// Bewohner neu, Bezeichner, Wohnfläche, Bewohner des Bilds.
        /// </summary>
        public const string SQL_FLAECHE_NUTZER =
            "UPDATE \"" + TABELLE + "\" SET \"Flaeche_Nutzer\" = ?, \"Bewohner\" = ?" + WO_FLAECHE_NUTZER;

        /// <summary>Zählung des Bilds. Parameter: Bezeichner, Wohnfläche, Bewohner des Bilds.</summary>
        public const string SQL_FLAECHE_NUTZER_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"" + TABELLE + "\"" + WO_FLAECHE_NUTZER;

        // Die Nutzungsbedingung steht zweimal woertlich da - als eigener Satzteil bestuende
        // sie keine Pruefung des SqlDialektPruefers (wie in GebaeudeKatalogverweis).

        /// <summary>
        /// Löscht einen Testrest, den keine Projektkopie führt (Verweis oder Name).
        /// Parameter: Bezeichner.
        /// </summary>
        public const string SQL_TESTREST_LOESCHEN =
            "DELETE FROM \"" + TABELLE + "\" WHERE \"Bezeichner\" = ? AND \"Flaeche_Nutzer\" IS NULL " +
            "AND NOT EXISTS (SELECT 1 FROM \"Tab_Gebaeude\" g " +
            "WHERE g.\"ID_Gebaeude_Stamm\" = \"" + TABELLE + "\".\"ID\" " +
            "OR lower(trim(g.\"Gebaeudename\")) = lower(trim(\"" + TABELLE + "\".\"Bezeichner\")))";

        /// <summary>Zählung: Wie viele Sätze dieses Namens trägt der Katalog noch mit dem Bild? Parameter: Bezeichner.</summary>
        public const string SQL_TESTREST_ZAEHLUNG =
            "SELECT COUNT(*) FROM \"" + TABELLE + "\" WHERE \"Bezeichner\" = ? AND \"Flaeche_Nutzer\" IS NULL";

        /// <summary>
        /// Die Projekte, die einen Testrest führen — Projekt-Ids, durch Komma getrennt, leer
        /// = unbenutzt. Parameter: Bezeichner.
        /// </summary>
        public const string SQL_TESTREST_VERWENDUNG =
            "SELECT group_concat(DISTINCT g.\"ID_Projekt\") FROM \"Tab_Gebaeude\" g " +
            "INNER JOIN \"" + TABELLE + "\" s ON (g.\"ID_Gebaeude_Stamm\" = s.\"ID\" " +
            "OR lower(trim(g.\"Gebaeudename\")) = lower(trim(s.\"Bezeichner\"))) " +
            "WHERE s.\"Bezeichner\" = ? AND s.\"Flaeche_Nutzer\" IS NULL";

        // =================================================================
        //  Der Schritt
        // =================================================================

        /// <summary>Was ein Lauf von <see cref="Ausfuehren"/> getan hat.</summary>
        public sealed class Bericht
        {
            /// <summary>Die reparierten Befunde, je Eintrag „Satz: was".</summary>
            public List<string> Repariert { get; } = new List<string>();

            /// <summary>Die gelöschten Testreste.</summary>
            public List<string> Geloescht { get; } = new List<string>();

            /// <summary>Die Testreste, die stehen blieben, weil ein Projekt sie führt — „Name (Projekte a,b)".</summary>
            public List<string> Benutzt { get; } = new List<string>();

            /// <summary>Eine Zeile für Protokoll und Konsole.</summary>
            public string Text()
            {
                return "repariert " + Repariert.Count.ToString(CultureInfo.InvariantCulture) +
                       (Repariert.Count > 0 ? " (" + string.Join("; ", Repariert) + ")" : "") +
                       ", Testreste geloescht " + Geloescht.Count.ToString(CultureInfo.InvariantCulture) +
                       (Geloescht.Count > 0 ? " (" + string.Join(", ", Geloescht) + ")" : "") +
                       ", benutzt und stehen gelassen " + Benutzt.Count.ToString(CultureInfo.InvariantCulture) +
                       (Benutzt.Count > 0 ? " (" + string.Join("; ", Benutzt) + ")" : "");
            }
        }

        /// <summary>
        /// Der ganze Schritt: Krankenhaussatz (U-Wert, Nordfenster), die vier Sätze ohne
        /// Fläche je Nutzer, dann die unbenutzten Testreste. Jeder Handgriff trifft nur sein
        /// Bild und ist für sich wiederholbar.
        /// </summary>
        public static Bericht Ausfuehren()
        {
            var b = new Bericht();

            if (Zahl(SQL_U_FENSTER_ZAEHLUNG, P(KRANKENHAUS), P(U_FENSTER_BILD_VON), P(U_FENSTER_BILD_BIS)) > 0)
            {
                int n = DataRepository.ExecuteNonQuery(SQL_U_FENSTER, P(U_FENSTER_NEU), P(KRANKENHAUS),
                                                       P(U_FENSTER_BILD_VON), P(U_FENSTER_BILD_BIS));
                if (n > 0) b.Repariert.Add(KRANKENHAUS + ": U-Wert Fenster 0.09 -> " + Text(U_FENSTER_NEU));
            }

            if (Zahl(SQL_NORD_ZAEHLUNG, P(KRANKENHAUS), P(NORD_BILD)) > 0)
            {
                int n = DataRepository.ExecuteNonQuery(SQL_NORD, P(NORD_NEU), P(NORD_NEU),
                                                       P(KRANKENHAUS), P(NORD_BILD));
                if (n > 0) b.Repariert.Add(KRANKENHAUS + ": Fensterflaeche Nord " + Text(NORD_BILD) +
                                           " -> " + Text(NORD_NEU) + ", gesamte Fensterflaeche neu gebildet");
            }

            foreach (FlaecheNutzerSatz s in FlaecheNutzerSaetze)
            {
                if (Zahl(SQL_FLAECHE_NUTZER_ZAEHLUNG, P(s.Bezeichner), P(s.Wohnflaeche), P(s.BewohnerBild)) == 0)
                    continue;
                int n = DataRepository.ExecuteNonQuery(SQL_FLAECHE_NUTZER, P(s.FlaecheNutzer), P(s.BewohnerNeu),
                                                       P(s.Bezeichner), P(s.Wohnflaeche), P(s.BewohnerBild));
                if (n > 0)
                    b.Repariert.Add(s.Bezeichner + ": Flaeche je Nutzer leer -> " + Text(s.FlaecheNutzer) +
                                    (s.BewohnerNeu != s.BewohnerBild
                                         ? ", Bewohner " + Text(s.BewohnerBild) + " -> " + Text(s.BewohnerNeu)
                                         : ""));
            }

            foreach (string name in Testreste)
            {
                if (Zahl(SQL_TESTREST_ZAEHLUNG, P(name)) == 0) continue;
                string projekte = Verwendung(name);
                if (projekte.Length > 0)
                {
                    b.Benutzt.Add(name + " (Projekte " + projekte + ")");
                    continue;
                }
                int n = DataRepository.ExecuteNonQuery(SQL_TESTREST_LOESCHEN, P(name));
                if (n > 0) b.Geloescht.Add(name);
            }
            return b;
        }

        /// <summary>
        /// Wie viele Befunde stehen noch offen — ein Satz mit einem der Reparaturbilder oder
        /// ein UNBENUTZTER Testrest? 0 = der Schritt ist gelaufen (die Nachprobe der
        /// Migration). Ein benutzter Testrest zählt nicht: Er bleibt mit Absicht.
        /// </summary>
        public static long Offen()
        {
            long offen = Zahl(SQL_U_FENSTER_ZAEHLUNG, P(KRANKENHAUS), P(U_FENSTER_BILD_VON), P(U_FENSTER_BILD_BIS))
                       + Zahl(SQL_NORD_ZAEHLUNG, P(KRANKENHAUS), P(NORD_BILD));
            foreach (FlaecheNutzerSatz s in FlaecheNutzerSaetze)
                offen += Zahl(SQL_FLAECHE_NUTZER_ZAEHLUNG, P(s.Bezeichner), P(s.Wohnflaeche), P(s.BewohnerBild));
            foreach (string name in Testreste)
                if (Zahl(SQL_TESTREST_ZAEHLUNG, P(name)) > 0 && Verwendung(name).Length == 0) offen++;
            return offen;
        }

        /// <summary>Die Projekt-Ids, die einen Testrest führen; leer = unbenutzt.</summary>
        public static string Verwendung(string bezeichner)
        {
            object o = DataRepository.ExecuteScalar(SQL_TESTREST_VERWENDUNG, P(bezeichner));
            return (o == null || o == DBNull.Value) ? "" : Convert.ToString(o, CultureInfo.InvariantCulture) ?? "";
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
    /// Ein Katalogsatz ohne „Fläche je Nutzer": das Schadensbild (Bezeichner, Wohnfläche,
    /// Bewohner) und die Werte danach.
    /// </summary>
    public sealed class FlaecheNutzerSatz
    {
        /// <summary>Legt den Satz an.</summary>
        public FlaecheNutzerSatz(string bezeichner, double wohnflaeche, double bewohnerBild,
                                 double bewohnerNeu, double flaecheNutzer)
        {
            Bezeichner = bezeichner;
            Wohnflaeche = wohnflaeche;
            BewohnerBild = bewohnerBild;
            BewohnerNeu = bewohnerNeu;
            FlaecheNutzer = flaecheNutzer;
        }

        /// <summary>Der Bezeichner des Katalogsatzes.</summary>
        public string Bezeichner { get; }

        /// <summary>Die Wohnfläche des Schadensbilds [m²].</summary>
        public double Wohnflaeche { get; }

        /// <summary>Die Bewohnerzahl des Schadensbilds.</summary>
        public double BewohnerBild { get; }

        /// <summary>Die Bewohnerzahl danach.</summary>
        public double BewohnerNeu { get; }

        /// <summary>Die Fläche je Nutzer danach [m²] = Wohnfläche / Bewohner, zwei Nachkommastellen.</summary>
        public double FlaecheNutzer { get; }
    }
}
