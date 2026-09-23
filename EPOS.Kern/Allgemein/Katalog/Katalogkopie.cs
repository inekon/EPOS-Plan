using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>„Duplizieren…" — ein Katalogsatz als eigener Satz</b> (Konzept
    /// Administrationsdialoge, Entscheid <b>AD-Q11</b> vom 23.09.2026: „Auslieferungssätze
    /// werden nie überschrieben").
    ///
    /// <para><b>Wozu.</b> Ein Satz mit <c>ReadOnly</c> gehört zur Auslieferung und bleibt
    /// Bezug; die Verwaltung sperrt sein „Speichern". Wer ihn ändern will, legt eine Kopie
    /// an — sie trägt den neuen Namen, ist <c>ReadOnly = 0</c> und damit ein eigener Satz,
    /// den „Speichern" schreiben darf. Beim BHKW (79 von 79 Sätzen der Testdatenbank
    /// ausgeliefert) beginnt damit jede Änderung.</para>
    ///
    /// <para><b>EINE Regel für alle Stammtabellen.</b> Die acht Gerätekataloge haben
    /// denselben Kopf — <c>ID INTEGER PRIMARY KEY AUTOINCREMENT</c>, <c>Bezeichner</c>,
    /// <c>ReadOnly</c> mit <c>CHECK (0,1)</c> — und verschiedene Fachspalten. Kopiert wird
    /// deshalb über die Spaltenliste der Tabelle (<c>pragma_table_info</c>): JEDE Spalte
    /// außer <c>ID</c>, <c>Bezeichner</c> und <c>ReadOnly</c> kommt unverändert mit, auch
    /// eine, die ein späterer Schemaschritt anlegt. Eine Liste im Quelltext liefe beim
    /// ersten neuen Feld auseinander.</para>
    ///
    /// <para><b>Kindtabellen</b> (die Kennlinien der Wärmepumpe) kommen im SELBEN Vorgang
    /// mit: jede Zeile mit dem Fremdschlüssel des Originals als Zeile der Kopie, mit neuer
    /// ID und — wo die Tabelle es führt — <c>ReadOnly = 0</c>.</para>
    ///
    /// <para><b>Der Kern prüft, die Oberfläche schlägt vor.</b> Den Namen „… (Kopie)"
    /// bildet <see cref="Namensvorschlag"/> ohne Datenbank; ob er frei ist, entscheidet
    /// <see cref="Duplizieren"/> in derselben Transaktion, die schreibt.</para>
    /// </summary>
    public static class Katalogkopie
    {
        /// <summary>
        /// Der Ausgang von <see cref="Duplizieren"/>: bei Erfolg die ID und der Name der
        /// Kopie, sonst der Grund in der Oberflächensprache.
        /// </summary>
        public sealed record Ergebnis(bool Ok, int Id, string Name, string Meldung);

        /// <summary>
        /// Eine Tabelle, deren Zeilen zum Satz gehören — z. B. die Kennlinien der
        /// Wärmepumpe (<c>Tab_Kenndaten_STAMM.ID_WP</c>).
        /// </summary>
        public sealed record Kindtabelle(string Tabelle, string Fremdschluessel);

        /// <summary>Die Spalten, die eine Kopie NICHT vom Original übernimmt.</summary>
        private static readonly HashSet<string> KOPFSPALTEN =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "ID", "Bezeichner", "ReadOnly" };

        /// <summary>
        /// <b>Der Name der Kopie</b>: „Name (Kopie)", ist der belegt, „Name (Kopie 2)",
        /// „Name (Kopie 3)" … — der erste freie. Ohne Datenbank: <paramref name="belegt"/>
        /// fragt die Liste, die der Dialog ohnehin hält.
        /// </summary>
        public static string Namensvorschlag(string name, Func<string, bool> belegt)
        {
            string basis = (name ?? "").Trim();
            Func<string, bool> frei = n => belegt == null || !belegt(n);

            string erster = string.Format(CultureInfo.CurrentCulture,
                                          MyResource.Resource.ADM_KOPIE_NAME, basis);
            if (frei(erster)) return erster;

            for (int i = 2; i < 1000; i++)
            {
                string kandidat = string.Format(CultureInfo.CurrentCulture,
                                                MyResource.Resource.ADM_KOPIE_NAME_N, basis, i);
                if (frei(kandidat)) return kandidat;
            }
            return erster;
        }

        /// <summary>
        /// <b>Kopiert den Satz <paramref name="id"/> der Stammtabelle als eigenen Satz</b>
        /// unter <paramref name="neuerName"/> — mit <c>ReadOnly = 0</c>, allen übrigen
        /// Spalten des Originals und den Zeilen der <paramref name="kinder"/>. Alles in
        /// EINER Transaktion: Scheitert ein Schritt, bleibt der Katalog, wie er war.
        /// </summary>
        /// <remarks>
        /// <para><b>Die ID vergibt der Vorgang selbst</b> (<c>MAX(ID) + 1</c>) — dasselbe
        /// Muster wie <c>…StammCtrl.Insert</c> und <c>ImportUebernehmen</c>; die Kindzeilen
        /// bekommen ihre ID von <c>AUTOINCREMENT</c>.</para>
        /// <para><b>Abgelehnt wird</b> ein leerer Name, ein Name, den es in der Tabelle
        /// schon gibt (der Bezeichner hat keinen eindeutigen Index — die Prüfung ist die
        /// einzige Wache gegen eine neue Dublette), und eine ID, die es nicht mehr gibt.</para>
        /// </remarks>
        public static Ergebnis Duplizieren(string tabelle, int id, string neuerName,
                                           params Kindtabelle[] kinder)
        {
            string name = (neuerName ?? "").Trim();
            if (name.Length == 0)
                return Abgelehnt(MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG);

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    if (Anzahl(v.Skalar("SELECT COUNT(*) FROM [" + tabelle + "] WHERE ID = ?",
                                        new DbParam("@id", id))) == 0)
                    {
                        v.Rollback();
                        return Abgelehnt(MyResource.Resource.ADM_MSG_KOPIE_FEHLT);
                    }

                    if (Anzahl(v.Skalar("SELECT COUNT(*) FROM [" + tabelle + "] WHERE Bezeichner = ?",
                                        new DbParam("@nam", name))) > 0)
                    {
                        v.Rollback();
                        return Abgelehnt(MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT);
                    }

                    object mx = v.Skalar("SELECT MAX(ID) FROM [" + tabelle + "]");
                    int neueId = (mx == null || mx == DBNull.Value) ? 1 : Convert.ToInt32(mx) + 1;

                    List<string> spalten = Spalten(v, tabelle, KOPFSPALTEN);
                    string liste = Liste(spalten);
                    string zusatz = liste.Length > 0 ? ", " + liste : "";

                    v.Ausfuehren(
                        "INSERT INTO [" + tabelle + "] (ID, Bezeichner" + zusatz + ", ReadOnly) " +
                        "SELECT ?, ?" + zusatz + ", 0 FROM [" + tabelle + "] WHERE ID = ?",
                        new DbParam("@neu", neueId),
                        new DbParam("@nam", name),
                        new DbParam("@alt", id));

                    foreach (Kindtabelle kind in kinder ?? Array.Empty<Kindtabelle>())
                        KinderKopieren(v, kind, id, neueId);

                    v.Commit();
                    return new Ergebnis(true, neueId, name, "");
                }
            }
            catch (Exception ex)
            {
                // DbVorgang.Dispose rollt zurueck, wenn kein Commit gesehen wurde.
                return Abgelehnt(MyResource.Resource.ADM_MSG_KOPIE_FEHLER + " " + ex.Message);
            }
        }

        /// <summary>
        /// Die Zeilen einer Kindtabelle als Zeilen der Kopie — jede Spalte außer
        /// <c>ID</c>, dem Fremdschlüssel und <c>ReadOnly</c> unverändert, in der
        /// Reihenfolge der Original-IDs.
        /// </summary>
        private static void KinderKopieren(DbVorgang v, Kindtabelle kind, int alteId, int neueId)
        {
            var ohne = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ID", kind.Fremdschluessel, "ReadOnly"
            };
            List<string> spalten = Spalten(v, kind.Tabelle, ohne);
            bool mitSchutz = Spalten(v, kind.Tabelle, new HashSet<string>())
                                 .Exists(s => string.Equals(s, "ReadOnly", StringComparison.OrdinalIgnoreCase));

            string liste = Liste(spalten);
            string zusatz = liste.Length > 0 ? ", " + liste : "";

            v.Ausfuehren(
                "INSERT INTO [" + kind.Tabelle + "] ([" + kind.Fremdschluessel + "]" + zusatz +
                (mitSchutz ? ", ReadOnly" : "") + ") " +
                "SELECT ?" + zusatz + (mitSchutz ? ", 0" : "") +
                " FROM [" + kind.Tabelle + "] WHERE [" + kind.Fremdschluessel + "] = ? ORDER BY ID",
                new DbParam("@neu", neueId),
                new DbParam("@alt", alteId));
        }

        /// <summary>Die Spalten der Tabelle in ihrer Reihenfolge, ohne die genannten.</summary>
        private static List<string> Spalten(DbVorgang v, string tabelle, HashSet<string> ohne)
        {
            var liste = new List<string>();
            DataTable dt = v.Lese("SELECT name FROM pragma_table_info(?) ORDER BY cid",
                                  new DbParam("@t", tabelle));
            foreach (DataRow r in dt.Rows)
            {
                string spalte = Convert.ToString(r["name"], CultureInfo.InvariantCulture) ?? "";
                if (spalte.Length == 0 || ohne.Contains(spalte)) continue;
                liste.Add(spalte);
            }
            return liste;
        }

        /// <summary>Die Spaltennamen in eckigen Klammern — Umlaute und Schlüsselwörter bleiben so lesbar.</summary>
        private static string Liste(List<string> spalten)
        {
            var teile = new List<string>(spalten.Count);
            foreach (string s in spalten) teile.Add("[" + s + "]");
            return string.Join(", ", teile);
        }

        private static int Anzahl(object wert)
            => wert == null || wert == DBNull.Value ? 0 : Convert.ToInt32(wert, CultureInfo.InvariantCulture);

        private static Ergebnis Abgelehnt(string meldung) => new Ergebnis(false, 0, "", meldung ?? "");
    }
}
