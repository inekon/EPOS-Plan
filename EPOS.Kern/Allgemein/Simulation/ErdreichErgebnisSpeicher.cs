using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE ERDREICHPRÜFUNG IM GESPEICHERTEN ERGEBNIS - Entscheidungsvorlage Modellgrenzen EQ1.
    //
    // Die Prüfung nach VDI 4640 lag nur prozessweit im Speicher (ErdreichAuswertung, „letzter Lauf
    // gewinnt"); nach einem Neustart zeigte der Erdreich-Dialog „noch kein Simulationslauf". Jetzt
    // schreibt ErgebnisCtrl.Save sie mit dem übrigen Ergebnis nach Tab_ErgebnisErdreich - je Anlage
    // die Prüfzeilen:
    //
    //   ENTZUGSLEISTUNG   Ist maximale Entzugsleistung [W]; Hinweis = Text ANSTELLE der Prüfung
    //                     (Luft-Wasser, nicht je Modul trennbar), leer = Prüfung möglich
    //   JAHRESENTZUG      Ist Jahresentzugsarbeit [kWh/a]; Hinweis = Vorbehalt zur Prüfung
    //   VOLLLASTSTUNDEN   Ist Jahresvolllaststunden [h/a]
    //   FROST             Ist Froststunden [h], Grenzwert 5 % der Betriebsstunden; Hinweis = Frostmeldung
    //   VDI4640:<Zeile>   die Zeilen der Auslegungsprüfung des Laufs (Ist, Grenzwert, Einheit)
    //
    // Der Dialog liest sie, wenn kein frischer Lauf der Sitzung vorliegt, mit „Stand des Laufs vom …".
    // Kein Rechenweg ist betroffen; der Lauf schreibt nur, was er ohnehin ausgewertet hat.
    // ====================================================================================

    /// <summary>Speichern und Lesen der Erdreichprüfung (EQ1) — Regeln im Kopf der Datei.</summary>
    public static class ErdreichErgebnisSpeicher
    {
        /// <summary>Prüfzeile der maximalen Entzugsleistung.</summary>
        public const string ZEILE_ENTZUGSLEISTUNG = "ENTZUGSLEISTUNG";

        /// <summary>Prüfzeile der Jahresentzugsarbeit.</summary>
        public const string ZEILE_JAHRESENTZUG = "JAHRESENTZUG";

        /// <summary>Prüfzeile der Volllaststunden.</summary>
        public const string ZEILE_VOLLLASTSTUNDEN = "VOLLLASTSTUNDEN";

        /// <summary>Prüfzeile der Frostbedingung.</summary>
        public const string ZEILE_FROST = "FROST";

        /// <summary>Vorsatz der Zeilen der Auslegungsprüfung nach VDI 4640.</summary>
        public const string VORSATZ_VDI4640 = "VDI4640:";

        /// <summary>Das Format des Laufstempels (invariant, sortierbar).</summary>
        public const string STEMPELFORMAT = "yyyy-MM-dd HH:mm:ss";

        /// <summary>Eine gespeicherte Prüfzeile.</summary>
        public sealed record Zeile(int IdAnlage, string Pruefzeile, double? Istwert, double? Grenzwert,
                                   string Einheit, string Grundlage, string Hinweis, string Laufstempel);

        /// <summary>Ist die Tabelle angelegt (Schemaschritt der Katalogfassung)?</summary>
        public static bool Vorhanden() => DataRepository.TabelleVorhanden(KatalogfassungSchema.TAB_ERGEBNIS_ERDREICH);

        /// <summary>Die Prüfzeilen eines Laufs — ohne Datenbank.</summary>
        public static List<Zeile> Zeilen(IEnumerable<ErdreichAuswertung.AnlageErgebnis> ergebnisse, DateTime lauf)
        {
            string stempel = lauf.ToString(STEMPELFORMAT, CultureInfo.InvariantCulture);
            var liste = new List<Zeile>();
            if (ergebnisse == null) return liste;
            foreach (ErdreichAuswertung.AnlageErgebnis a in ergebnisse)
            {
                if (a == null || a.ID_Anlage <= 0) continue;
                ErdreichAuswertung.ErdreichLaufErgebnis l = ErdreichAuswertung.ErgebnisZuordnen(a);
                string grundlage = a.Pruefung?.Grundlage ?? "";
                liste.Add(new Zeile(a.ID_Anlage, ZEILE_ENTZUGSLEISTUNG, a.MaxEntzugW, null, "W", grundlage, l.HinweisErgebnis, stempel));
                liste.Add(new Zeile(a.ID_Anlage, ZEILE_JAHRESENTZUG, a.JahresentzugKWh, null, "kWh/a", grundlage, l.HinweisVorbehalt, stempel));
                liste.Add(new Zeile(a.ID_Anlage, ZEILE_VOLLLASTSTUNDEN, a.VolllastStunden, null, "h/a", grundlage, "", stempel));
                liste.Add(new Zeile(a.ID_Anlage, ZEILE_FROST, a.FrostStunden,
                                    ErdreichAuswertung.FROST_ANTEIL_MAX * a.BetriebsStunden, "h",
                                    ErdreichAuswertung.FROST_NORMBASIS, l.HinweisFrost, stempel));
                if (a.Pruefung != null && a.Pruefung.Moeglich)
                    foreach (VDI4640Pruefung.Pruefzeile z in a.Pruefung.Zeilen)
                    {
                        string name = VORSATZ_VDI4640 + (z.Bezeichnung ?? "").Trim();
                        if (name.Length > 60) name = name.Substring(0, 60);
                        liste.Add(new Zeile(a.ID_Anlage, name, z.Istwert, z.Grenzwert, z.Einheit ?? "", grundlage,
                                            (z.IstText ?? "") + (z.Ueberschritten ? " !" : ""), stempel));
                    }
            }
            return liste;
        }

        /// <summary>
        /// Schreibt die Prüfzeilen eines Projekts IM VORGANG des Ergebnisses: erst die alten Zeilen
        /// weg, dann die neuen. Ohne Tabelle (Datenbank vor dem Schemaschritt) geschieht nichts.
        /// </summary>
        internal static void Schreiben(DbVorgang v, int idProjekt, IEnumerable<Zeile> zeilen, bool tabelleVorhanden)
        {
            if (!tabelleVorhanden || idProjekt <= 0) return;
            v.Ausfuehren("DELETE FROM \"" + KatalogfassungSchema.TAB_ERGEBNIS_ERDREICH + "\" WHERE \"ID_Projekt\" = ?",
                         new DbParam("@p", idProjekt));
            if (zeilen == null) return;
            foreach (Zeile z in zeilen)
                v.Ausfuehren("INSERT INTO \"" + KatalogfassungSchema.TAB_ERGEBNIS_ERDREICH +
                             "\" (\"ID_Projekt\", \"ID_Anlage\", \"Pruefzeile\", \"Istwert\", \"Grenzwert\", \"Einheit\", " +
                             "\"Grundlage\", \"Hinweis\", \"Laufstempel\") " +
                             "SELECT ?, ?, ?, ?, ?, ?, ?, ?, ? WHERE EXISTS (SELECT 1 FROM \"Tab_Energieanlagen\" WHERE ID = ?)",
                             new DbParam("@p", idProjekt), new DbParam("@a", z.IdAnlage), new DbParam("@z", z.Pruefzeile),
                             new DbParam("@i", Endlich(z.Istwert)), new DbParam("@g", Endlich(z.Grenzwert)),
                             new DbParam("@e", (object)z.Einheit ?? DBNull.Value),
                             new DbParam("@gr", (object)z.Grundlage ?? DBNull.Value),
                             new DbParam("@h", (object)z.Hinweis ?? DBNull.Value),
                             new DbParam("@s", z.Laufstempel), new DbParam("@a2", z.IdAnlage));
        }

        private static object Endlich(double? w) =>
            w.HasValue && !double.IsNaN(w.Value) && !double.IsInfinity(w.Value) ? (object)w.Value : DBNull.Value;

        /// <summary>Die gespeicherten Prüfzeilen eines Projekts (leer ohne Tabelle oder ohne Lauf).</summary>
        public static List<Zeile> Lesen(int idProjekt)
        {
            var liste = new List<Zeile>();
            if (idProjekt <= 0 || !Vorhanden()) return liste;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT \"ID_Anlage\", \"Pruefzeile\", \"Istwert\", \"Grenzwert\", \"Einheit\", \"Grundlage\", \"Hinweis\", " +
                "\"Laufstempel\" FROM \"" + KatalogfassungSchema.TAB_ERGEBNIS_ERDREICH + "\" WHERE \"ID_Projekt\" = ? ORDER BY ID",
                new DbParam("@p", idProjekt));
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows)
                liste.Add(new Zeile(
                    Convert.ToInt32(r[0], CultureInfo.InvariantCulture),
                    Text(r[1]), Zahl(r[2]), Zahl(r[3]), Text(r[4]), Text(r[5]), Text(r[6]), Text(r[7])));
            return liste;
        }

        private static string Text(object o) => o == null || o == DBNull.Value ? "" : Convert.ToString(o, CultureInfo.InvariantCulture);

        private static double? Zahl(object o) =>
            o == null || o == DBNull.Value ? (double?)null : Convert.ToDouble(o, CultureInfo.InvariantCulture);

        /// <summary>
        /// Das gespeicherte Ergebnis einer Anlage in der Sprache des Dialogs — mit Laufstempel.
        /// <paramref name="idAnlage"/> ≤ 0: das einzige gespeicherte, wenn es genau eines gibt.
        /// <see cref="ErdreichAuswertung.ErdreichLaufErgebnis.Keines"/>, wenn nichts gespeichert ist.
        /// </summary>
        public static ErdreichAuswertung.ErdreichLaufErgebnis Gespeichert(int idProjekt, int idAnlage)
        {
            List<Zeile> alle = Lesen(idProjekt);
            if (alle.Count == 0) return ErdreichAuswertung.ErdreichLaufErgebnis.Keines;
            List<int> anlagen = alle.Select(z => z.IdAnlage).Distinct().ToList();
            int anlage = idAnlage > 0 && anlagen.Contains(idAnlage) ? idAnlage
                       : (idAnlage <= 0 && anlagen.Count == 1 ? anlagen[0] : 0);
            if (anlage == 0) return ErdreichAuswertung.ErdreichLaufErgebnis.Keines;

            List<Zeile> zeilen = alle.Where(z => z.IdAnlage == anlage).ToList();
            Zeile Holen(string name) => zeilen.FirstOrDefault(z => z.Pruefzeile == name);
            Zeile entzug = Holen(ZEILE_ENTZUGSLEISTUNG), jahr = Holen(ZEILE_JAHRESENTZUG),
                  voll = Holen(ZEILE_VOLLLASTSTUNDEN), frost = Holen(ZEILE_FROST);
            if (entzug == null) return ErdreichAuswertung.ErdreichLaufErgebnis.Keines;

            string hinweis = entzug.Hinweis ?? "";
            return new ErdreichAuswertung.ErdreichLaufErgebnis(
                true, hinweis.Length == 0,
                entzug.Istwert ?? 0, jahr?.Istwert ?? 0, voll?.Istwert ?? 0,
                hinweis, jahr?.Hinweis ?? "", frost?.Hinweis ?? "")
            {
                Laufstempel = entzug.Laufstempel
            };
        }
    }
}
