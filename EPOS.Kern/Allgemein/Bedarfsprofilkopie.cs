using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Monatswerte und Wochenprofil in der PROJEKTKOPIE eines Bedarfsprofils</b> (Konzept Kältebedarfsprofile 3.2, 3.3,
    /// 3.5; Entscheid OE-5, Welle KBP-5a) — derselbe Weg für Prozesswärme und Brauchwasser, den die Kälte mit
    /// <see cref="KaeltebedarfStammCtrl"/> geht: Die Monatsanteile stehen in <c>Monat_1…12</c> des Kopfs, das Wochenprofil
    /// in <c>"1"…"168"</c> der ersten Typzeile, die über ihre Kopf-ID auf genau diese Kopie zeigt. Die Kette
    /// <see cref="ProfilBedarf"/> liest beide unverändert; geschrieben wird nie in den Katalog.
    /// </summary>
    public static class Bedarfsprofilkopie
    {
        /// <summary>Kopf-, Typtabelle und die Spalte der Typtabelle, die auf den Kopf zeigt.</summary>
        public sealed record Tabellen(string Kopf, string Typ, string TypKopfId);

        /// <summary>Die Projektkopien der Prozesswärme.</summary>
        public static readonly Tabellen Prozesswaerme = new Tabellen("Tab_Prozesswaerme", "Tab_Prozesstyp", "ID_Prozesswaerme");

        /// <summary>Die Projektkopien des Brauchwassers.</summary>
        public static readonly Tabellen Brauchwasser = new Tabellen("Tab_Brauchwasser", "Tab_Brauchwassertyp", "ID_Brauchwasser");

        private static string Q(string s) => "\"" + s + "\"";

        private static double? Zahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte) || r[spalte] == DBNull.Value) return null;
            return Convert.ToDouble(r[spalte], CultureInfo.InvariantCulture);
        }

        /// <summary>Die Monatsanteile [%] der Kopie <paramref name="idKopie"/>; <c>null</c> ohne Kopie. Leer zählt 0.</summary>
        public static double[] Monate(Tabellen t, int idKopie)
        {
            if (t == null || idKopie <= 0 || !DataRepository.TabelleVorhanden(t.Kopf)) return null;
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + Q(t.Kopf) + " WHERE \"ID\" = ?", new DbParam("@id", idKopie));
            if (dt == null || dt.Rows.Count == 0) return null;
            var m = new double[Kaelteprofileingabe.MONATE];
            for (int i = 0; i < m.Length; i++)
                m[i] = Zahl(dt.Rows[0], "Monat_" + (i + 1).ToString(CultureInfo.InvariantCulture)) ?? 0.0;
            return m;
        }

        /// <summary>Das Wochenprofil (168 Werte) der ersten Typzeile der Kopie; <c>null</c> ohne Typzeile. Leer zählt 0.</summary>
        public static double[] Woche(Tabellen t, int idKopie)
        {
            if (t == null || idKopie <= 0 || !DataRepository.TabelleVorhanden(t.Typ)) return null;
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + Q(t.Typ) + " WHERE " + Q(t.TypKopfId) + " = ? ORDER BY \"ID\"",
                                                       new DbParam("@id", idKopie));
            if (dt == null || dt.Rows.Count == 0) return null;
            var w = new double[Kaelteprofileingabe.WOCHE];
            for (int h = 0; h < w.Length; h++)
                w[h] = Zahl(dt.Rows[0], (h + 1).ToString(CultureInfo.InvariantCulture)) ?? 0.0;
            return w;
        }

        /// <summary>
        /// Schreibt Monatsanteile und/oder Wochenprofil in die Kopie <paramref name="idKopie"/> — nie in den Katalog.
        /// <c>null</c> lässt die jeweilige Größe stehen. Läuft in der Vorgangsklammer des Aufrufers; ohne Typzeile wird
        /// keine angelegt (Rückgabe <c>false</c>).
        /// </summary>
        public static bool Schreiben(Tabellen t, int idKopie, double[] monate, double[] woche)
        {
            if (t == null || idKopie <= 0 || !DataRepository.TabelleVorhanden(t.Kopf)) return false;
            if (monate != null)
            {
                if (monate.Length != Kaelteprofileingabe.MONATE) return false;
                var satz = new StringBuilder();
                var p = new List<DbParam>();
                for (int i = 0; i < monate.Length; i++)
                {
                    if (i > 0) satz.Append(", ");
                    satz.Append(Q("Monat_" + (i + 1).ToString(CultureInfo.InvariantCulture))).Append(" = ?");
                    p.Add(new DbParam("@m" + (i + 1).ToString("D2", CultureInfo.InvariantCulture), monate[i]));
                }
                p.Add(new DbParam("@id", idKopie));
                if (!DataRepository.ExecuteSQL("UPDATE " + Q(t.Kopf) + " SET " + satz + " WHERE \"ID\" = ?", p.ToArray()))
                    return false;
            }
            if (woche != null)
            {
                if (woche.Length != Kaelteprofileingabe.WOCHE) return false;
                object typId = DataRepository.ExecuteScalar(
                    "SELECT \"ID\" FROM " + Q(t.Typ) + " WHERE " + Q(t.TypKopfId) + " = ? ORDER BY \"ID\" LIMIT 1",
                    new DbParam("@kopf", idKopie));
                if (typId == null || typId == DBNull.Value) return false;
                var satz = new StringBuilder();
                var p = new List<DbParam>();
                for (int h = 0; h < woche.Length; h++)
                {
                    if (h > 0) satz.Append(", ");
                    satz.Append('"').Append((h + 1).ToString(CultureInfo.InvariantCulture)).Append("\" = ?");
                    p.Add(new DbParam("@h" + (h + 1).ToString("D3", CultureInfo.InvariantCulture), woche[h]));
                }
                p.Add(new DbParam("@tid", typId));
                if (!DataRepository.ExecuteSQL("UPDATE " + Q(t.Typ) + " SET " + satz + " WHERE \"ID\" = ?", p.ToArray()))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// <b>Kopie bei Bearbeitung</b> (Konzept 3.5): legt von der Kopie <paramref name="idKopie"/> eine eigene Kopie
        /// desselben Projekts an — Kopf mit allen Spalten (gleicher Bezeichner, gleicher Ursprung) und alle Typzeilen — und
        /// gibt ihre ID zurück; 0 bei Fehler. Läuft in der Vorgangsklammer des Aufrufers.
        /// </summary>
        public static int Abspalten(Tabellen t, int idKopie, int idProjekt)
        {
            if (t == null || idKopie <= 0 || idProjekt <= 0 || !DataRepository.TabelleVorhanden(t.Kopf)) return 0;
            DataTable kopf = DataRepository.GetDataTable("SELECT * FROM " + Q(t.Kopf) + " WHERE \"ID\" = ? AND \"ID_Projekt\" = ?",
                                                         new DbParam("@id", idKopie), new DbParam("@proj", idProjekt));
            if (kopf == null || kopf.Rows.Count == 0) return 0;
            int neuId = DataRepository.GetMaxID(t.Kopf, "ID") + 1;
            if (!ZeileKopieren(t.Kopf, kopf.Rows[0], new Dictionary<string, object> { ["ID"] = neuId })) return 0;

            DataTable typen = DataRepository.GetDataTable("SELECT * FROM " + Q(t.Typ) + " WHERE " + Q(t.TypKopfId) + " = ? ORDER BY \"ID\"",
                                                          new DbParam("@kopf", idKopie));
            if (typen != null)
                foreach (DataRow r in typen.Rows)
                {
                    int neuTyp = DataRepository.GetMaxID(t.Typ, "ID") + 1;
                    if (!ZeileKopieren(t.Typ, r, new Dictionary<string, object> { ["ID"] = neuTyp, [t.TypKopfId] = neuId }))
                        return 0;
                }
            return neuId;
        }

        /// <summary>
        /// Braucht die bearbeitete Zeile an der Stelle <paramref name="eigene"/> eine eigene Kopie, weil eine andere Zeile
        /// auf dieselbe Kopie <paramref name="kopie"/> zeigt? Ja, sobald eine solche Zeile unbearbeitet ist oder als
        /// frühere bearbeitete Zeile die Kopie schon trägt; sind alle Teilhaber bearbeitet, behält die erste die Kopie —
        /// es bleibt keine Waise. <paramref name="kopieVon"/> nennt die Kopie der Zeile i (≤ 0 = noch keine).
        /// </summary>
        public static bool AbspaltenNoetig(int anzahl, int eigene, int kopie, Func<int, int> kopieVon, Func<int, bool> bearbeitet)
        {
            bool geteilt = false, alleBearbeitet = true, erster = true;
            for (int i = 0; i < anzahl; i++)
            {
                if (i == eigene || kopieVon(i) != kopie) continue;
                geteilt = true;
                if (!bearbeitet(i)) alleBearbeitet = false;
                else if (i < eigene) erster = false;
            }
            return geteilt && !(alleBearbeitet && erster);
        }

        /// <summary>Fügt die Zeile <paramref name="r"/> mit allen ihren Spalten in <paramref name="tabelle"/> ein, <paramref name="ersetzt"/> überschreibt einzelne Werte.</summary>
        private static bool ZeileKopieren(string tabelle, DataRow r, Dictionary<string, object> ersetzt)
        {
            var cols = new StringBuilder();
            var vals = new StringBuilder();
            var p = new List<DbParam>();
            int k = 0;
            foreach (DataColumn c in r.Table.Columns)
            {
                if (k > 0) { cols.Append(", "); vals.Append(", "); }
                cols.Append('"').Append(c.ColumnName).Append('"');
                vals.Append('?');
                object wert = ersetzt.TryGetValue(c.ColumnName, out object e) ? e : r[c];
                p.Add(new DbParam("@c" + (k++).ToString("D3", CultureInfo.InvariantCulture), wert ?? DBNull.Value));
            }
            return DataRepository.ExecuteSQL("INSERT INTO " + Q(tabelle) + " (" + cols + ") VALUES (" + vals + ")", p.ToArray());
        }
    }
}
