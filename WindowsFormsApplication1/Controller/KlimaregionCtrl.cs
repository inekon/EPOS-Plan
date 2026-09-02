using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Controller für die Projekt-Klimaregionen (Tab_Klimaregion).
    ///
    /// Reale Spalten der Tabelle (am Schema verifiziert, 14.08.2026):
    ///   ID (AutoWert) · ID_Projekt · Bezeichner · Longitude · Latitude · Details
    ///   · Klimazone_DIN4710 (über WaermequelleClass.SchemaSicherstellen ergänzt)
    ///
    /// Die Schreibmethoden trafen bis Paket 3 nicht durchgängig das reale Schema und
    /// wären zur Laufzeit gescheitert - genauer:
    ///   Add():    <c>Longitude</c>, <c>Latitude</c> und <c>Details</c> waren richtig,
    ///             falsch war nur <c>Name</c> (heißt hier <c>Bezeichner</c>); zusätzlich
    ///             fehlte die Pflichtspalte <c>ID_Projekt</c>.
    ///   Update(): alle fünf Bezeichner falsch (Name, Längengrad, Breitengrad,
    ///             Beschreibung, WHERE ID_Klimaregion).
    ///   Delete(): <c>WHERE Name = ?</c> statt <c>Bezeichner</c>.
    /// Die Leseseite (ReadAll/ReadSingle) war dagegen immer korrekt. Alle drei sind
    /// jetzt auf das reale Schema gezogen.
    /// </summary>
    class KlimaregionCtrl : KlimaregionModel
    {
        // --- Kompatibilitäts-Layer ---
        private List<KlimaregionModel> _internalList = new List<KlimaregionModel>();
        public new int rows => _internalList.Count;
        public new List<KlimaregionModel> items => _internalList;

        public KlimaregionModel klimaregionmodel = new KlimaregionModel();

        public KlimaregionCtrl()
        {
            m_ID_Klimaregion = 0;
            m_szName = "";
            Longitude = 0;
            Latitude = 0;
            Details = "";
            Klimazone_DIN4710 = 0;
        }

        #region --- READ OPERATIONS ---

        public void ReadAll()
        {
            string sql = "SELECT * FROM Tab_Klimaregion ORDER BY ID";
            ExecuteRead(sql);
        }

        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();

            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                MapRowToThis(row);
                _internalList.Add(this);
            }
        }

        private void ExecuteRead(string sql, params OleDbParameter[] parameters)
        {
            DataTable dt = DataRepository.GetDataTable(sql, parameters);
            _internalList.Clear();

            foreach (DataRow row in dt.Rows)
            {
                KlimaregionModel item = new KlimaregionModel();

                // Zuweisung über Spaltennamen – passend zu deinem KlimaregionModel aufgebaut:
                item.m_ID_Klimaregion = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
                item.m_szName = row["Bezeichner"] != DBNull.Value ? row["Bezeichner"].ToString() : "";
                item.Longitude = row["Longitude"] != DBNull.Value ? Convert.ToDouble(row["Longitude"]) : 0;
                item.Latitude = row["Latitude"] != DBNull.Value ? Convert.ToDouble(row["Latitude"]) : 0;
                item.Details = row["Details"] != DBNull.Value ? row["Details"].ToString() : "";
                item.Klimazone_DIN4710 = KlimazoneAusZeile(row);

                _internalList.Add(item);
            }
        }

        private void MapRowToThis(DataRow row)
        {
            // Zuweisung an die "this"-Instanz über Spaltennamen:
            this.m_ID_Klimaregion = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            this.m_szName = row["Bezeichner"] != DBNull.Value ? row["Bezeichner"].ToString() : "";
            this.Longitude = row["Longitude"] != DBNull.Value ? Convert.ToDouble(row["Longitude"]) : 0;
            this.Latitude = row["Latitude"] != DBNull.Value ? Convert.ToDouble(row["Latitude"]) : 0;
            this.Details = row["Details"] != DBNull.Value ? row["Details"].ToString() : "";
            this.Klimazone_DIN4710 = KlimazoneAusZeile(row);
        }

        /// <summary>
        /// Liest Klimazone_DIN4710 tolerant: In Alt-Datenbanken fehlt die Spalte
        /// ganz, in bestehenden Zeilen ist sie nach dem ADD COLUMN NULL. Beides
        /// bedeutet "nicht zugeordnet" und wird als 0 gelesen.
        /// </summary>
        private static int KlimazoneAusZeile(DataRow row)
        {
            if (!row.Table.Columns.Contains("Klimazone_DIN4710")) return 0;
            if (row["Klimazone_DIN4710"] == DBNull.Value) return 0;
            return Convert.ToInt32(row["Klimazone_DIN4710"]);
        }

        /// <summary>
        /// Liefert die DIN-4710-Klimazone der Projekt-Klimaregion (0 = nicht
        /// zugeordnet). Wird vom Erdreichdialog zur Vorbelegung genutzt.
        /// </summary>
        public static int GetKlimazone(int idKlimaregion)
        {
            if (idKlimaregion <= 0) return 0;
            object v = WaermequelleClass.WertLesenStill("Tab_Klimaregion", "Klimazone_DIN4710", idKlimaregion);
            if (v == null) return 0;
            try { return Convert.ToInt32(v); } catch { return 0; }
        }

        /// <summary>Schreibt die DIN-4710-Klimazone an die Projekt-Klimaregion.</summary>
        public static bool SetKlimazone(int idKlimaregion, int zone)
        {
            if (idKlimaregion <= 0) return false;
            return DataRepository.ExecuteSQL(
                "UPDATE Tab_Klimaregion SET Klimazone_DIN4710 = ? WHERE ID = ?",
                new OleDbParameter("@zone", zone),
                new OleDbParameter("@id", idKlimaregion));
        }

        #endregion

        #region --- WRITE OPERATIONS ---

        /// <summary>
        /// Legt eine Klimaregion im Projekt an. Der Name steht in der Spalte
        /// <c>Bezeichner</c> (nicht <c>Name</c> - das ist die STAMM-Tabelle).
        ///
        /// <c>Bezeichner</c> ist NOT NULL: ein leerer Name wird als "" geschrieben,
        /// nicht als NULL (ein NULL lehnt Access mit "Sie müssen einen Wert in das
        /// Feld 'Tab_Klimaregion.Bezeichner' eingeben" ab).
        /// </summary>
        /// <param name="idProjekt">
        /// PFLICHTANGABE. Tab_Klimaregion ist eine Projekttabelle: <c>ID_Projekt</c>
        /// ist NOT NULL und über die erzwungene Beziehung "Tab_ProjektTab_Klimaregion"
        /// an Tab_Projekt gebunden. Eine projektlose Zeile lässt das Schema NICHT zu -
        /// ein INSERT ohne ID_Projekt scheitert mit "Der Datensatz kann nicht
        /// hinzugefügt oder geändert werden …". Deshalb bricht Add() bei
        /// idProjekt &lt;= 0 mit false ab, statt eine Ausnahme laufen zu lassen.
        /// </param>
        /// <returns>false, wenn kein gültiges Projekt angegeben wurde.</returns>
        public bool Add(string szName, double Longitude, double Latitude, string Details,
                        DbVorgang v, int idProjekt = 0)
        {
            if (idProjekt <= 0)
            {
                // Kein Fallback möglich: die Pflicht-FK auf Tab_Projekt lässt keine
                // projektlose Klimaregion zu (Schema am Original verifiziert).
                Console.WriteLine("KlimaregionCtrl.Add: ohne gültiges idProjekt (> 0) nicht möglich - " +
                                  "Tab_Klimaregion.ID_Projekt ist Pflichtfeld mit erzwungener Beziehung " +
                                  "auf Tab_Projekt. Kein Datensatz angelegt.");
                return false;
            }

            // Klimazone_DIN4710 entsteht erst über SchemaSicherstellen(); das läuft
            // bisher nur beim Öffnen der Simulationskonfiguration bzw. beim
            // Simulationsstart. Damit Add() davon unabhängig bleibt, wird die Spalte
            // hier abgesichert (still, legt sie nur an, wenn sie fehlt).
            WaermequelleClass.SpalteSicherstellen("Tab_Klimaregion", "Klimazone_DIN4710", "LONG");

            // 1. Das SQL ohne das ID-Feld, da Access dieses als Autowert selbst befüllt.
            string sql = "INSERT INTO Tab_Klimaregion (ID_Projekt, Bezeichner, Longitude, Latitude, Details, Klimazone_DIN4710) " +
                         "VALUES (?, ?, ?, ?, ?, ?)";

            // WICHTIG: Die Reihenfolge der Parameter MUSS exakt mit dem SQL übereinstimmen!
            OleDbParameter[] ps = {
                new OleDbParameter("?", idProjekt),
                new OleDbParameter("?", szName ?? ""),   // NOT NULL
                new OleDbParameter("?", Longitude),
                new OleDbParameter("?", Latitude),
                new OleDbParameter("?", string.IsNullOrEmpty(Details) ? (object)DBNull.Value : Details),
                new OleDbParameter("?", Klimazone_DIN4710)
            };

            // 2. Da die ID ein AutoWert ist, liefert der Vorgang sie im selben Aufruf
            // zurueck - auf DERSELBEN Verbindung (frueher SELECT @@IDENTITY).
            int neueId = v.EinfuegenUndId(sql, ps);
            if (neueId > 0)
            {
                m_ID_Klimaregion = neueId;
            }

            return true;
        }

        /// <summary>
        /// Schreibt die geladene Region zurück. Schlüssel ist Tab_Klimaregion.ID -
        /// dort steht auch, was die Leseseite in m_ID_Klimaregion ablegt.
        ///
        /// <c>Bezeichner</c> ist NOT NULL und wird deshalb bei leerem Namen als ""
        /// geschrieben, nicht als NULL. <c>ID_Projekt</c> bleibt unangetastet - die
        /// Projektzugehörigkeit einer bestehenden Zeile ändert sich hier nicht.
        /// </summary>
        public bool Update()
        {
            // wie in Add(): Spalte absichern, damit Update() nicht davon abhängt,
            // dass die Simulationskonfiguration schon einmal offen war
            WaermequelleClass.SpalteSicherstellen("Tab_Klimaregion", "Klimazone_DIN4710", "LONG");

            string sql = "UPDATE Tab_Klimaregion SET Bezeichner = ?, Longitude = ?, Latitude = ?, " +
                         "Details = ?, Klimazone_DIN4710 = ? WHERE ID = ?";

            OleDbParameter[] parameters = {
                new OleDbParameter("?", m_szName ?? ""),   // NOT NULL
                new OleDbParameter("?", Longitude),
                new OleDbParameter("?", Latitude),
                new OleDbParameter("?", string.IsNullOrEmpty(Details) ? (object)DBNull.Value : Details),
                new OleDbParameter("?", Klimazone_DIN4710),
                new OleDbParameter("?", m_ID_Klimaregion) // WHERE-Bedingung
            };

            return DataRepository.ExecuteSQL(sql, parameters);
        }

        /// <summary>
        /// Löscht Regionen über den Bezeichner. Ohne Projekteinschränkung trifft
        /// das alle Projekte mit gleichnamiger Region - deshalb idProjekt angeben,
        /// wo die ID bekannt ist.
        /// </summary>
        public bool Delete(string szName, int idProjekt = 0)
        {
            if (idProjekt > 0)
            {
                return DataRepository.ExecuteSQL(
                    "DELETE FROM Tab_Klimaregion WHERE Bezeichner = ? AND ID_Projekt = ?",
                    new OleDbParameter("?", szName ?? ""),
                    new OleDbParameter("?", idProjekt));
            }

            return DataRepository.ExecuteSQL(
                "DELETE FROM Tab_Klimaregion WHERE Bezeichner = ?",
                new OleDbParameter("?", szName ?? ""));
        }

        #endregion

        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(items[i].m_szName);
            }
        }

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++)
            {
                ctrl.Items.Add(items[i].m_szName);
            }
        }
    }
}