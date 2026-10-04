// Bauplan des Referenzprojekts 1054 "Referenzprojekt Zonen mit Heizkreis" (Welle AK1z, E63, Teil E):
// EINE Quelle der gesaeten Zellen, gezogen vom Saatskript referenzprojekt_1054_zonen_heizkreis.cs und von
// der Wache EPOS.Kern.Tests/ZonenHeizkreisReferenzprojektWacheTests.
//
// HERKUNFT. 1054 ist eine Kopie des Zonenprojekts 1052 auf dem Kopierweg des Programms
// (ProjektDuplizierenCtrl): dieselben drei Zonen, Bauteile, Trennflaechen, der Luftstrom, die Zonenkalender
// und die Aufheizvorgabe. 1052 selbst bleibt unveraendert - es traegt den Nachweis der Aufheizoptimierung mit
// Zonen; gekoppelt stuenden seine Zonen GEKOPPELT.
//
// GESAETE ZELLEN (alles andere wie kopiert):
//   Tab_Projekt        Beschreibung, Aenderungsdatum, Erstelldatum; Kosten_Geaendert leer
//   Tab_Einstellungen  Anlagenkopplung = 'AK1' (KonfigurationCtrl.AnlagenkopplungSetzen)
//   Tab_Gebaeude       Heizkreis_Aktiv = 1, Uebergabe_Art = 'RADIATOR'; die uebrigen Uebergabe-, Auslegungs-
//                      und Heizkurvenspalten leer, Heizkurve_Aktiv = 0 (fester Vorlauf aus der Anlage)
//   Tab_Zone           "Gastronomie und Verwaltung": Uebergabe_Art = 'KONVEKTOR', Auslegung_Vorlauf = 70,
//                      Auslegung_Ruecklauf = 50, Regler_Proportionalband = 2.0; "Gaestezimmer" und "Keller":
//                      alle sieben Uebergabespalten leer (Wert des Gebaeudes)
// Die Zonen rechnen damit gekoppelt (Aufheizzustand GEKOPPELT) - das ist gewollt.

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;

namespace EPOS.Referenzlaeufe.Skripte
{
    public static class Zonenprojekt1054
    {
        public const int VORLAGE = Zonenprojekt1052.NEU;
        public const string VORLAGE_NAME = Zonenprojekt1052.NAME;
        public const int NEU = 1054;
        public const string NAME = "Referenzprojekt Zonen mit Heizkreis";
        public const string BESCHREIBUNG =
            "Referenzprojekt der Wärmeübergabe je Zone: Kopie von Projekt 1052 (Hotel in drei Zonen) mit " +
            "Anlagenkopplung AK1 — das Gebäude mit Heizkreis und Radiatoren, die Zone Gastronomie und Verwaltung " +
            "mit eigener Übergabe (Konvektor, Auslegung 70/50 °C, Proportionalband 2 K), Gästezimmer und Keller " +
            "mit den Werten des Gebäudes. Hält Vorlauf, Rücklauf und begrenzte Stunden je Zone im Regressionsnetz.";
        public const string DATUM = "2026-10-04 00:00:00";

        public const string GEB_UEBERGABE_ART = DbWerte.UEBERGABE_RADIATOR;
        public const string GASTRO_UEBERGABE_ART = DbWerte.UEBERGABE_KONVEKTOR;
        public const double GASTRO_VORLAUF = 70.0;
        public const double GASTRO_RUECKLAUF = 50.0;
        public const double GASTRO_PROPORTIONALBAND = 2.0;

        /// <summary>Die sieben Uebergabespalten einer Zone (Schritt ZonenUebergabeSchema).</summary>
        public static readonly string[] ZONENSPALTEN =
        {
            "Uebergabe_Art", "Uebergabe_Exponent", "Uebergabe_Leistung_Nenn", "Auslegung_Vorlauf",
            "Auslegung_Ruecklauf", "Auslegung_Raumtemperatur", "Regler_Proportionalband",
        };

        /// <summary>Die Uebergabe-, Auslegungs- und Heizkurvenspalten des Gebaeudes, die leer bleiben.</summary>
        public static readonly string[] GEBAEUDE_LEER =
        {
            "Uebergabe_Exponent", "Uebergabe_Leistung_Nenn", "Auslegung_Vorlauf", "Auslegung_Ruecklauf",
            "Auslegung_Raumtemperatur", "Auslegung_Aussentemperatur", "Heizkurve_Niveau", "Heizkurve_Steilheit",
            "Regler_Proportionalband", "Sollwertprofil",
            "Kuehl_Uebergabe_Art", "Kuehl_Uebergabe_Exponent", "Kuehl_Uebergabe_Leistung_Nenn",
            "Kuehl_Auslegung_Vorlauf", "Kuehl_Auslegung_Ruecklauf", "Kuehl_Auslegung_Raumtemperatur",
        };

        /// <summary>Sollwerte der sieben Zonenspalten je Zone (in der Reihenfolge von <see cref="ZONENSPALTEN"/>).</summary>
        public static object[] ZonenSoll(string zone)
            => zone == Zonenprojekt1052.ZONE_GASTRO
                ? new object[] { GASTRO_UEBERGABE_ART, null, null, GASTRO_VORLAUF, GASTRO_RUECKLAUF, null, GASTRO_PROPORTIONALBAND }
                : new object[ZONENSPALTEN.Length];

        private static DbParam P(object w) => new DbParam("?", w ?? DBNull.Value);

        /// <summary>Saet die Zellen an der frischen Kopie; Rueckgabe eine Meldung oder <c>null</c>.</summary>
        public static string Bauen(int projekt)
        {
            DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Beschreibung = ?, Aenderungsdatum = ?, Erstelldatum = ? WHERE ID = ?",
                                           P(BESCHREIBUNG), P(DATUM), P(DATUM), P(projekt));
            if (!KonfigurationCtrl.AnlagenkopplungSetzen(projekt, DbWerte.ANLAGENKOPPLUNG_AK1))
                return "Die Kopplungsstufe ist nicht geschrieben.";
            int geb = Zonenprojekt1052.Gebaeude(projekt);
            if (geb <= 0) return "Die Projektkopie des Gebäudes fehlt.";
            if (DataRepository.ExecuteNonQuery("UPDATE Tab_Gebaeude SET Heizkreis_Aktiv = 1, Uebergabe_Art = ? WHERE ID = ?",
                                               P(GEB_UEBERGABE_ART), P(geb)) != 1)
                return "Die Gebäudezeile ist nicht geschrieben.";
            if (DataRepository.ExecuteNonQuery(
                    "UPDATE Tab_Zone SET Uebergabe_Art = ?, Auslegung_Vorlauf = ?, Auslegung_Ruecklauf = ?, Regler_Proportionalband = ? " +
                    "WHERE ID_Gebaeude = ? AND Bezeichner = ?",
                    P(GASTRO_UEBERGABE_ART), P(GASTRO_VORLAUF), P(GASTRO_RUECKLAUF), P(GASTRO_PROPORTIONALBAND),
                    P(geb), P(Zonenprojekt1052.ZONE_GASTRO)) != 1)
                return "Die Zone '" + Zonenprojekt1052.ZONE_GASTRO + "' ist nicht geschrieben.";
            // Zuletzt: Die Kostenstempel-Trigger stempeln die kopierten Kostenzeilen; die Testdatenbank fuehrt
            // leere Kostenstempel (wie 1052 und 1053).
            DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Kosten_Geaendert = NULL WHERE ID = ?", P(projekt));
            return null;
        }

        /// <summary>Die Zaehlung der Zonendaten eines Projekts: Zonen, Bauteile, Trennflaechen, Luftstroeme,
        /// Kalender, Perioden, Vorgaben.</summary>
        public static string Zaehlung(int projekt)
        {
            const string GEB = "SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?";
            const string ZON = "SELECT ID FROM Tab_Zone WHERE ID_Gebaeude IN (" + GEB + ")";
            long Z(string sql) => Zonenprojekt1052.Zahl(sql, projekt);
            return "Zonen " + Z("SELECT COUNT(*) FROM Tab_Zone WHERE ID_Gebaeude IN (" + GEB + ")") +
                   ", Bauteile " + Z("SELECT COUNT(*) FROM Tab_Bauteil WHERE ID_Zone IN (" + ZON + ")") +
                   ", Trennflächen " + Z("SELECT COUNT(*) FROM Tab_Bauteil WHERE ID_Nachbarzone IS NOT NULL AND ID_Zone IN (" + ZON + ")") +
                   ", Luftströme " + Z("SELECT COUNT(*) FROM Tab_Zonenluftstrom WHERE ID_ZoneA IN (" + ZON + ")") +
                   ", Kalender " + Z("SELECT COUNT(*) FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IN (" + GEB + ")") +
                   ", Perioden " + Z("SELECT COUNT(*) FROM Tab_Konditionierungsperiode WHERE ID_Kalender IN " +
                                     "(SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude IN (" + GEB + "))") +
                   ", Vorgaben " + Z("SELECT COUNT(*) FROM Tab_Konditionierungsvorgabe WHERE ID_Gebaeude IN (" + GEB + ")");
        }

        private static string Text(object o)
            => o == null || o == DBNull.Value ? "∅" : o is double d ? d.ToString("R", CultureInfo.InvariantCulture)
               : Convert.ToString(o, CultureInfo.InvariantCulture);

        /// <summary>Jede gesaete Zelle samt Herkunft; leere Liste = alles wie geplant.</summary>
        public static List<string> Pruefen(int projekt)
        {
            var f = new List<string>();
            void Soll(string was, object ist, object soll)
            {
                if (Text(ist) != Text(soll)) f.Add(was + " = " + Text(ist) + " statt " + Text(soll));
            }
            DataTable p = Zonenprojekt1052.Tabelle("SELECT Projektname, Beschreibung, Aenderungsdatum, Erstelldatum, Kosten_Geaendert FROM Tab_Projekt WHERE ID = ?", projekt);
            if (p.Rows.Count != 1) { f.Add("Projekt " + projekt + " fehlt"); return f; }
            Soll("Projektname", p.Rows[0][0], NAME);
            Soll("Beschreibung", p.Rows[0][1], BESCHREIBUNG);
            foreach (int i in new[] { 2, 3 })
            {
                object w = p.Rows[0][i];
                string d = w is DateTime t ? t.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) : Text(w);
                Soll(p.Columns[i].ColumnName, d, DATUM);
            }
            Soll("Kosten_Geaendert", p.Rows[0][4], null);

            // Projekteinstellungen: Kopplungsstufe gesaet, Kuehlbetrieb, Aufheizvorgabe und Kaskade wie die Vorlage.
            Soll("Anlagenkopplung", KonfigurationCtrl.AnlagenkopplungLesen(projekt), DbWerte.ANLAGENKOPPLUNG_AK1);
            const string EIN = "SELECT Kuehlbetrieb, Aufheizoptimierung, Aufheiz_Bemessung, Aufheiz_Abzug_K, Aufheiz_Reserve, Aufheiz_Art, " +
                               "Aufheiz_Aufschlag_H, Aufheiz_Aufschlag_Prozent, Tool_1, Tool_2, Tool_3, Tool_4, Tool_5, Tool_6 " +
                               "FROM Tab_Einstellungen WHERE ID_Projekt = ?";
            DataTable e = Zonenprojekt1052.Tabelle(EIN, projekt), ev = Zonenprojekt1052.Tabelle(EIN, VORLAGE);
            if (e.Rows.Count != 1 || ev.Rows.Count != 1) f.Add("Tab_Einstellungen: nicht genau eine Zeile");
            else
                foreach (DataColumn c in e.Columns) Soll("Tab_Einstellungen." + c.ColumnName, e.Rows[0][c], ev.Rows[0][c.ColumnName]);

            // Gebaeude.
            int geb = Zonenprojekt1052.Gebaeude(projekt);
            if (geb <= 0) { f.Add("Gebäude fehlt"); return f; }
            DataTable g = Zonenprojekt1052.Tabelle("SELECT * FROM Tab_Gebaeude WHERE ID = ?", geb);
            Soll("Heizkreis_Aktiv", g.Rows[0]["Heizkreis_Aktiv"], 1L);
            Soll("Gebäude.Uebergabe_Art", g.Rows[0]["Uebergabe_Art"], GEB_UEBERGABE_ART);
            Soll("Heizkurve_Aktiv", g.Rows[0]["Heizkurve_Aktiv"], 0L);
            foreach (string s in GEBAEUDE_LEER) Soll("Gebäude." + s, g.Rows[0][s], null);

            // Zonen: dieselben wie in der Vorlage, die sieben Uebergabespalten wie geplant.
            DataTable z = Zonenprojekt1052.Tabelle("SELECT Bezeichner, Rang, IstBeheizt, " + string.Join(", ", ZONENSPALTEN) +
                                                   " FROM Tab_Zone WHERE ID_Gebaeude = ? ORDER BY Rang", geb);
            string[] namen = { Zonenprojekt1052.ZONE_GAESTE, Zonenprojekt1052.ZONE_GASTRO, Zonenprojekt1052.ZONE_KELLER };
            Soll("Zonen", string.Join(", ", z.Rows.Cast<DataRow>().Select(r => Text(r[0]))), string.Join(", ", namen));
            foreach (DataRow r in z.Rows)
            {
                object[] soll = ZonenSoll(Text(r[0]));
                for (int i = 0; i < ZONENSPALTEN.Length; i++) Soll(Text(r[0]) + "." + ZONENSPALTEN[i], r[3 + i], soll[i]);
            }

            // Kopie von 1052: dieselbe Zaehlung der Zonendaten.
            Soll("Zonendaten", Zaehlung(projekt), Zaehlung(VORLAGE));
            return f;
        }
    }
}
