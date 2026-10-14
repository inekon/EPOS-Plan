using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Betriebskalender der Bedarfsprofile</b> (<c>Tab_Betriebskalender</c>,
    /// <see cref="BedarfNetzKalenderSchema"/>; Entscheidungsvorlage Modellgrenzen PW2, BW2) —
    /// Lesen, Speichern, Löschen und die Auskunft, wie viele Zuordnungen einen Kalender nutzen.
    /// Die Tabelle ist projektübergreifend wie ein Katalog; eine Zuordnungszeile zeigt per
    /// <c>ID_Betriebskalender</c> auf eine Zeile (<c>ON DELETE SET NULL</c>).
    ///
    /// <para><b>Ohne Tabelle</b> (Datenbank vor dem Schemaschritt) liefern die Leser leer und die
    /// Schreiber <c>false</c> — der Rechenweg rechnet dann ohne Kalender wie zuvor.</para>
    /// </summary>
    public static class BetriebskalenderCtrl
    {
        private const string TAB = BedarfNetzKalenderSchema.TAB_KALENDER;

        /// <summary>Gibt es die Tabelle?</summary>
        public static bool TabelleVorhanden() => DataRepository.TabelleVorhanden(TAB);

        /// <summary>Alle Kalender, nach Bezeichner sortiert.</summary>
        public static List<Betriebskalender> Liste()
        {
            var liste = new List<Betriebskalender>();
            if (!TabelleVorhanden()) return liste;
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + TAB + " ORDER BY Bezeichner, ID");
            if (dt == null) return liste;
            foreach (DataRow r in dt.Rows) liste.Add(AusZeile(r));
            return liste;
        }

        /// <summary>Ein Kalender über seine ID; <c>null</c> = keiner.</summary>
        public static Betriebskalender Lies(int id)
        {
            if (id <= 0 || !TabelleVorhanden()) return null;
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + TAB + " WHERE ID = ?", new DbParam("?", id));
            return dt != null && dt.Rows.Count > 0 ? AusZeile(dt.Rows[0]) : null;
        }

        /// <summary>
        /// Speichert einen Kalender — neu (ID 0) oder über seine ID — und liefert die ID; 0, wenn die
        /// Prüfung (<see cref="Betriebskalender.Pruefen"/>) oder das Schreiben scheitert.
        /// </summary>
        public static int Speichern(Betriebskalender k)
        {
            if (k == null || k.Pruefen() != null || !TabelleVorhanden()) return 0;

            var spalten = new List<string> { BedarfNetzKalenderSchema.SPALTE_BEZEICHNER, BedarfNetzKalenderSchema.SPALTE_BUNDESLAND };
            var werte = new List<DbParam>
            {
                new DbParam("?", k.Bezeichner.Trim()),
                new DbParam("?", (object)k.Bundesland ?? DBNull.Value)
            };
            for (int i = 1; i <= BedarfNetzKalenderSchema.FERIEN_ANZAHL; i++)
            {
                Ferienzeitraum f = k.Ferien != null && k.Ferien.Count >= i ? k.Ferien[i - 1] : null;
                spalten.Add(BedarfNetzKalenderSchema.SpalteFerienVon(i));
                spalten.Add(BedarfNetzKalenderSchema.SpalteFerienBis(i));
                werte.Add(new DbParam("?", f != null ? (object)f.Von : DBNull.Value));
                werte.Add(new DbParam("?", f != null ? (object)f.Bis : DBNull.Value));
            }
            spalten.Add(BedarfNetzKalenderSchema.SPALTE_FERIENFAKTOR);
            spalten.Add(BedarfNetzKalenderSchema.SPALTE_FEIERTAG_WIE_SONNTAG);
            spalten.Add(BedarfNetzKalenderSchema.SPALTE_FERIEN_KUERZEN);
            werte.Add(new DbParam("?", k.Ferienfaktor));
            werte.Add(new DbParam("?", k.FeiertagWieSonntag ? 1 : 0));
            werte.Add(new DbParam("?", k.FerienKuerzen ? 1 : 0));

            if (k.ID > 0)
            {
                werte.Add(new DbParam("?", k.ID));
                string sql = "UPDATE " + TAB + " SET " + string.Join(" = ?, ", spalten) + " = ? WHERE ID = ?";
                return DataRepository.ExecuteNonQuery(sql, werte.ToArray()) > 0 ? k.ID : 0;
            }

            string einfuegen = "INSERT INTO " + TAB + " (" + string.Join(", ", spalten) + ") VALUES (" +
                               string.Join(", ", spalten.ConvertAll(_ => "?")) + ")";
            int neu = DataRepository.ExecuteInsertAndGetId(einfuegen, werte.ToArray());
            if (neu > 0) k.ID = neu;
            return neu > 0 ? neu : 0;
        }

        /// <summary>
        /// Löscht einen Kalender. Die Zuordnungen, die ihn nutzen, stehen danach ohne Kalender
        /// (<c>ON DELETE SET NULL</c>; ohne scharfe Fremdschlüssel setzt der Weg sie selbst leer).
        /// </summary>
        public static bool Loeschen(int id)
        {
            if (id <= 0 || !TabelleVorhanden()) return false;
            if (BedarfNetzKalenderSchema.KalenderspaltenVorhanden())
                foreach (string z in BedarfNetzKalenderSchema.ZUORDNUNGEN)
                    DataRepository.ExecuteNonQuery(
                        "UPDATE " + z + " SET " + BedarfNetzKalenderSchema.SPALTE_ID_KALENDER +
                        " = NULL WHERE " + BedarfNetzKalenderSchema.SPALTE_ID_KALENDER + " = ?",
                        new DbParam("?", id));
            return DataRepository.ExecuteNonQuery("DELETE FROM " + TAB + " WHERE ID = ?", new DbParam("?", id)) > 0;
        }

        /// <summary>Wie viele Zuordnungszeilen (aller drei Bedarfsarten, aller Projekte) nutzen den Kalender?</summary>
        public static int Verwendungen(int id)
        {
            if (id <= 0 || !BedarfNetzKalenderSchema.KalenderspaltenVorhanden()) return 0;
            int n = 0;
            foreach (string z in BedarfNetzKalenderSchema.ZUORDNUNGEN)
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM " + z + " WHERE " + BedarfNetzKalenderSchema.SPALTE_ID_KALENDER + " = ?",
                    new DbParam("?", id));
                if (o != null && o != DBNull.Value) n += Convert.ToInt32(o, CultureInfo.InvariantCulture);
            }
            return n;
        }

        /// <summary>
        /// Der Betriebskalender je Zuordnungszeile eines Projekts (Schlüssel = ID der Zeile) in einer
        /// der drei Zuordnungstabellen; leer, wenn die Spalte fehlt. Gelesen von den
        /// <c>LiesProjekt</c>-Wegen der drei Zuordnungscontroller.
        /// </summary>
        public static Dictionary<int, int?> KalenderDerZuordnungen(string zuordnungTabelle, int idProjekt)
        {
            var karte = new Dictionary<int, int?>();
            if (!IstZuordnungstabelle(zuordnungTabelle) ||
                !DataRepository.SpalteVorhanden(zuordnungTabelle, BedarfNetzKalenderSchema.SPALTE_ID_KALENDER))
                return karte;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, " + BedarfNetzKalenderSchema.SPALTE_ID_KALENDER + " FROM " + zuordnungTabelle +
                " WHERE ID_Projekt = ?", new DbParam("?", idProjekt));
            if (dt == null) return karte;
            foreach (DataRow r in dt.Rows)
                karte[Convert.ToInt32(r[0], CultureInfo.InvariantCulture)] =
                    r[1] == DBNull.Value ? (int?)null : Convert.ToInt32(r[1], CultureInfo.InvariantCulture);
            return karte;
        }

        /// <summary>
        /// Setzt den Betriebskalender EINER Zuordnungszeile nach dem Anlegen (WizardCtrl.Add_*):
        /// <c>null</c> schreibt nichts (die neue Zeile ist leer); ohne Spalte ist ein gesetzter
        /// Kalender nicht zu speichern und liefert <c>false</c>.
        /// </summary>
        public static bool ZuordnungKalenderSetzen(string zuordnungTabelle, int idZeile, int? kalenderId)
        {
            if (!kalenderId.HasValue || kalenderId.Value <= 0) return true;
            if (!IstZuordnungstabelle(zuordnungTabelle) ||
                !DataRepository.SpalteVorhanden(zuordnungTabelle, BedarfNetzKalenderSchema.SPALTE_ID_KALENDER))
                return false;
            return DataRepository.ExecuteSQL(
                "UPDATE " + zuordnungTabelle + " SET " + BedarfNetzKalenderSchema.SPALTE_ID_KALENDER + " = ? WHERE ID = ?",
                new DbParam("?", kalenderId.Value), new DbParam("?", idZeile));
        }

        /// <summary>Ist der Name eine der drei Zuordnungstabellen? (Der Name wird Teil des SQL-Texts.)</summary>
        private static bool IstZuordnungstabelle(string name)
        {
            foreach (string z in BedarfNetzKalenderSchema.ZUORDNUNGEN)
                if (string.Equals(z, name, StringComparison.Ordinal)) return true;
            return false;
        }

        /// <summary>Ein Kalender aus einer Tabellenzeile.</summary>
        internal static Betriebskalender AusZeile(DataRow r)
        {
            var k = new Betriebskalender
            {
                ID = Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture),
                Bezeichner = r[BedarfNetzKalenderSchema.SPALTE_BEZEICHNER] == DBNull.Value
                             ? "" : Convert.ToString(r[BedarfNetzKalenderSchema.SPALTE_BEZEICHNER], CultureInfo.InvariantCulture),
                Bundesland = r[BedarfNetzKalenderSchema.SPALTE_BUNDESLAND] == DBNull.Value
                             ? null : Convert.ToString(r[BedarfNetzKalenderSchema.SPALTE_BUNDESLAND], CultureInfo.InvariantCulture),
                Ferienfaktor = r[BedarfNetzKalenderSchema.SPALTE_FERIENFAKTOR] == DBNull.Value
                               ? 0.0 : Convert.ToDouble(r[BedarfNetzKalenderSchema.SPALTE_FERIENFAKTOR], CultureInfo.InvariantCulture),
                FeiertagWieSonntag = r[BedarfNetzKalenderSchema.SPALTE_FEIERTAG_WIE_SONNTAG] == DBNull.Value ||
                                     Convert.ToInt32(r[BedarfNetzKalenderSchema.SPALTE_FEIERTAG_WIE_SONNTAG], CultureInfo.InvariantCulture) != 0,
                FerienKuerzen = r[BedarfNetzKalenderSchema.SPALTE_FERIEN_KUERZEN] != DBNull.Value &&
                                Convert.ToInt32(r[BedarfNetzKalenderSchema.SPALTE_FERIEN_KUERZEN], CultureInfo.InvariantCulture) != 0
            };
            for (int i = 1; i <= BedarfNetzKalenderSchema.FERIEN_ANZAHL; i++)
            {
                object von = r[BedarfNetzKalenderSchema.SpalteFerienVon(i)];
                object bis = r[BedarfNetzKalenderSchema.SpalteFerienBis(i)];
                if (von == DBNull.Value || bis == DBNull.Value) continue;
                k.Ferien.Add(new Ferienzeitraum(Convert.ToInt32(von, CultureInfo.InvariantCulture),
                                                Convert.ToInt32(bis, CultureInfo.InvariantCulture)));
            }
            return k;
        }
    }
}
