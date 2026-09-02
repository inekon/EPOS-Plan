using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Zugriff auf Tab_Kostenprofil - die 12 Monats- und 168 Wochenwerte eines
    // Kostenprofils (Fachkonzept Stromspeicher 4.1 b, angelegt von SchemaMigration
    // Schritt 12c).
    //
    // Die beiden Wertesaetze werden als ";"-Zeichenketten mit InvariantCulture
    // abgelegt - genau das Format, das Form_Quellprofil fuer WQ_Monatswerte und
    // WQ_Wochenwerte schon verwendet. Der Controller reicht die Zeichenketten
    // unveraendert durch; das Zerlegen macht die Oberflaeche (Eingabe) bzw.
    // StromPreisCtrl (Rechenweg).
    //
    // Durchgaengig ueber DataRepository mit ?-Parametern; IDs ueber MAX(ID)+1.
    // ---------------------------------------------------------------------------
    public class KostenprofilCtrl
    {
        public const string TABLE = "Tab_Kostenprofil";

        private readonly List<KostenprofilModel> _internalList = new List<KostenprofilModel>();

        public int rows => _internalList.Count;
        public List<KostenprofilModel> items => _internalList;

        // =====================================================================
        // Vorsorge
        // =====================================================================

        /// <summary>
        /// Legt die Tabelle an, falls die Migration noch nicht gelaufen ist - dieselbe
        /// tolerante Rueckfallebene wie <c>PreisreiheCtrl.StelleTabellenSicher</c>.
        /// </summary>
        public static void StelleTabelleSicher()
        {
            try
            {
                DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + TABLE + "]");
            }
            catch
            {
                try
                {
                    DataRepository.ExecuteSQL(SchemaMigration.SQL_CREATE_KOSTENPROFIL);
                    DataRepository.ExecuteSQL(SchemaMigration.SQL_INDEX_KOSTENPROFIL);
                }
                catch { /* der eigentliche Zugriff meldet den Fehler */ }
            }
        }

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>Alle Profile eines Projekts in Namensreihenfolge.</summary>
        public List<KostenprofilModel> ReadAllByProjekt(int idProjekt)
        {
            _internalList.Clear();
            if (idProjekt <= 0) return _internalList;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID_Projekt = ? ORDER BY Bezeichner",
                new DbParam("@proj", idProjekt));

            if (dt == null) return _internalList;

            foreach (DataRow r in dt.Rows) _internalList.Add(AusZeile(dt, r));
            return _internalList;
        }

        /// <summary>Ein Profil ueber seine ID; <c>null</c>, wenn es keines gibt.</summary>
        public KostenprofilModel ReadSingle(int id)
        {
            if (id <= 0) return null;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE ID = ?",
                new DbParam("@id", id));

            if (dt == null || dt.Rows.Count == 0) return null;
            return AusZeile(dt, dt.Rows[0]);
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>
        /// Legt ein Profil an (ID nach dem MAX(ID)+1-Hausmuster) und traegt die
        /// vergebene ID in das Modell zurueck. Rueckgabe -1 bei Fehler.
        /// </summary>
        public int Insert(KostenprofilModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));

            StelleTabelleSicher();

            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            bool ok = DataRepository.ExecuteSQL(
                "INSERT INTO [" + TABLE + "] (ID, ID_Projekt, Bezeichner, Monatswerte, Wochenwerte) " +
                "VALUES (?, ?, ?, ?, ?)",
                new DbParam("@id", DbParamTyp.Integer) { Wert = neueId },
                new DbParam("@proj", DbParamTyp.Integer) { Wert = m.ID_Projekt },
                new DbParam("@bez", DbParamTyp.VarWChar) { Wert = m.Bezeichner ?? "" },
                new DbParam("@mon", DbParamTyp.VarWChar) { Wert = m.Monatswerte ?? "" },
                new DbParam("@woch", DbParamTyp.LongVarWChar) { Wert = m.Wochenwerte ?? "" });

            if (!ok) return -1;

            m.ID = neueId;
            return neueId;
        }

        /// <summary>Schreibt ein vorhandenes Profil zurueck (zielgenaues UPDATE ueber die ID).</summary>
        public bool Update(KostenprofilModel m)
        {
            if (m == null) throw new ArgumentNullException(nameof(m));
            if (m.ID <= 0) return false;

            return DataRepository.ExecuteSQL(
                "UPDATE [" + TABLE + "] SET Bezeichner = ?, Monatswerte = ?, Wochenwerte = ? WHERE ID = ?",
                new DbParam("@bez", DbParamTyp.VarWChar) { Wert = m.Bezeichner ?? "" },
                new DbParam("@mon", DbParamTyp.VarWChar) { Wert = m.Monatswerte ?? "" },
                new DbParam("@woch", DbParamTyp.LongVarWChar) { Wert = m.Wochenwerte ?? "" },
                new DbParam("@id", DbParamTyp.Integer) { Wert = m.ID });
        }

        /// <summary>Loescht ein Profil.</summary>
        public bool Delete(int id)
        {
            if (id <= 0) return false;

            return DataRepository.ExecuteSQL(
                "DELETE FROM [" + TABLE + "] WHERE ID = ?",
                new DbParam("@id", id));
        }

        // =====================================================================
        // Kleinigkeiten
        // =====================================================================

        private static KostenprofilModel AusZeile(DataTable dt, DataRow r)
        {
            KostenprofilModel m = new KostenprofilModel();
            m.ID = Zahl(dt, r, "ID");
            m.ID_Projekt = Zahl(dt, r, "ID_Projekt");
            m.Bezeichner = Text(dt, r, "Bezeichner");
            m.Monatswerte = Text(dt, r, "Monatswerte");
            m.Wochenwerte = Text(dt, r, "Wochenwerte");
            return m;
        }

        private static int Zahl(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return 0;
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        private static string Text(DataTable dt, DataRow r, string spalte)
        {
            if (!dt.Columns.Contains(spalte)) return "";
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }
    }
}
