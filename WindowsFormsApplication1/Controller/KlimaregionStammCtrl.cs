using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Controller fuer die Klimaregion-STAMMDATEN (Tab_Klimaregion_STAMM).
    // Behaelt die alte Spaltenstruktur (ID_Klimaregion, Name) und kennt das neue Feld ReadOnly.
    // Enthaelt ausserdem die zentrale Kopierlogik STAMM -> Projekt fuer Klimaregion + Klimadaten + Solar.
    class KlimaregionStammCtrl : KlimaregionStammModel
    {
        public const string TAB_REGION_STAMM   = "Tab_Klimaregion_STAMM";
        public const string TAB_KLIMADATEN_STAMM = "Tab_Klimadaten_STAMM";
        public const string TAB_SOLAR_STAMM    = "Tab_Solar_STAMM";
        public const string TAB_REGION_PROJEKT = "Tab_Klimaregion";
        public const string TAB_KLIMADATEN_PROJEKT = "Tab_Klimadaten";
        public const string TAB_SOLAR_PROJEKT  = "Tab_Solar";

        private List<KlimaregionStammModel> _internalList = new List<KlimaregionStammModel>();
        public new int rows => _internalList.Count;
        public new List<KlimaregionStammModel> items => _internalList;

        public KlimaregionStammCtrl()
        {
            m_ID_Klimaregion = 0;
            m_szName = "";
            Longitude = 0;
            Latitude = 0;
            Details = "";
            m_bReadOnly = false;
        }

        #region --- READ OPERATIONS ---

        public void ReadAll()
        {
            ExecuteRead("SELECT * FROM " + TAB_REGION_STAMM + " ORDER BY Name");
        }

        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();
            if (dt.Rows.Count > 0)
            {
                MapRowToThis(dt.Rows[0]);
                _internalList.Add(this);
            }
        }

        private void ExecuteRead(string sql, params DbParam[] parameters)
        {
            DataTable dt = DataRepository.GetDataTable(sql, parameters);
            _internalList.Clear();
            foreach (DataRow row in dt.Rows)
            {
                KlimaregionStammModel item = new KlimaregionStammModel();
                item.m_ID_Klimaregion = row["ID_Klimaregion"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimaregion"]) : 0;
                item.m_szName = row["Name"] != DBNull.Value ? row["Name"].ToString() : "";
                item.Longitude = row["Longitude"] != DBNull.Value ? Convert.ToDouble(row["Longitude"]) : 0;
                item.Latitude = row["Latitude"] != DBNull.Value ? Convert.ToDouble(row["Latitude"]) : 0;
                item.Details = row["Details"] != DBNull.Value ? row["Details"].ToString() : "";
                item.m_bReadOnly = row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
                _internalList.Add(item);
            }
        }

        private void MapRowToThis(DataRow row)
        {
            this.m_ID_Klimaregion = row["ID_Klimaregion"] != DBNull.Value ? Convert.ToInt32(row["ID_Klimaregion"]) : 0;
            this.m_szName = row["Name"] != DBNull.Value ? row["Name"].ToString() : "";
            this.Longitude = row["Longitude"] != DBNull.Value ? Convert.ToDouble(row["Longitude"]) : 0;
            this.Latitude = row["Latitude"] != DBNull.Value ? Convert.ToDouble(row["Latitude"]) : 0;
            this.Details = row["Details"] != DBNull.Value ? row["Details"].ToString() : "";
            this.m_bReadOnly = row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
        }

        // Liefert true, wenn der Stammdatensatz (per Name) schreibgeschuetzt ist.
        public bool IsReadOnly(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + TAB_REGION_STAMM + " WHERE Name = ?",
                new DbParam("@name", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // Liefert die Stamm-ID_Klimaregion zu einem Namen (oder 0).
        public int GetStammId(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID_Klimaregion FROM " + TAB_REGION_STAMM + " WHERE Name = ?",
                new DbParam("@name", szName ?? ""));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        #endregion

        #region --- WRITE OPERATIONS (STAMM / Admin) ---

        // Legt eine neue Klimaregion in der STAMM-Tabelle an (Import im Admin-Dialog).
        // ID_Klimaregion ist AutoWert; ReadOnly wird mit false gesetzt (Feld ist NOT NULL).
        public bool Add(string szName, double Longitude, double Latitude, string Details, DbVorgang v)
        {
            string sql = "INSERT INTO " + TAB_REGION_STAMM + " (Name, Longitude, Latitude, Details, ReadOnly) VALUES (?, ?, ?, ?, ?)";
            DbParam[] ps = {
                new DbParam("?", string.IsNullOrEmpty(szName) ? (object)DBNull.Value : szName),
                new DbParam("?", Longitude),
                new DbParam("?", Latitude),
                new DbParam("?", string.IsNullOrEmpty(Details) ? (object)DBNull.Value : Details),
                new DbParam("?", false)
            };
            // ARBEITSPAKET S4e: Einfuegen und ID-Rueckgabe in EINEM Aufruf auf der
            // Verbindung des Vorgangs (frueher SELECT @@IDENTITY auf conn/trans).
            int neueId = v.EinfuegenUndId(sql, ps);
            if (neueId > 0) m_ID_Klimaregion = neueId;
            return true;
        }

        public bool Update()
        {
            if (IsReadOnly(m_szName))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            string sql = "UPDATE " + TAB_REGION_STAMM + " SET Name = ?, Longitude = ?, Latitude = ?, Details = ? WHERE ID_Klimaregion = ?";
            DbParam[] ps = {
                new DbParam("?", string.IsNullOrEmpty(m_szName) ? (object)DBNull.Value : m_szName),
                new DbParam("?", Longitude),
                new DbParam("?", Latitude),
                new DbParam("?", string.IsNullOrEmpty(Details) ? (object)DBNull.Value : Details),
                new DbParam("?", m_ID_Klimaregion)
            };
            return DataRepository.ExecuteSQL(sql, ps);
        }

        // Loescht eine Klimaregion aus der STAMM-Tabelle, sofern nicht schreibgeschuetzt.
        public bool Delete(string szName)
        {
            if (IsReadOnly(szName))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            string sql = "DELETE FROM " + TAB_REGION_STAMM + " WHERE Name = ?";
            return DataRepository.ExecuteSQL(sql, new DbParam("@name", szName ?? ""));
        }

        #endregion

        #region --- UI FILL ---

        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++) ctrl.Items.Add(items[i].m_szName);
        }

        public void FillListBox(ListBox ctrl)
        {
            ctrl.Items.Clear();
            for (int i = 0; i < rows; i++) ctrl.Items.Add(items[i].m_szName);
        }

        #endregion

        #region --- COPY STAMM -> PROJEKT ---

        // Liefert die Projekt-Region-ID (Tab_Klimaregion.ID) eines Stamm-Namens im Projekt, oder 0.
        public static int GetProjektRegionId(string szName, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + TAB_REGION_PROJEKT + " WHERE Bezeichner = ? AND ID_Projekt = ?",
                new DbParam("@name", szName ?? ""),
                new DbParam("@idProj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Zentrale Anwendung: kopiert die Stamm-Region (falls noetig) in das Projekt UND
        // setzt Tab_Projekt.ID_Klimaregion auf die neue Projekt-Region-ID.
        // Von allen Speicherpunkten (Wizard, Startformular, Hauptformular) zu verwenden.
        // Rueckgabe: die Projekt-Region-ID, 0 bei Fehler/leerer Auswahl.
        public static int ApplyRegionToProjekt(int stammRegionId, int idProjekt)
        {
            if (stammRegionId <= 0 || idProjekt <= 0) return 0;
            int neueRegionId = CopyRegionToProjekt(stammRegionId, idProjekt);
            if (neueRegionId > 0)
            {
                DataRepository.ExecuteSQL(
                    "UPDATE Tab_Projekt SET ID_Klimaregion = ? WHERE ID = ?",
                    new DbParam("@reg", neueRegionId),
                    new DbParam("@id", idProjekt));
            }
            return neueRegionId;
        }

        // Namensbasierte Anwendung: ermittelt die Stamm-Region-ID zum Namen und kopiert die
        // Klimadaten (falls noetig) ins Projekt; setzt Tab_Projekt.ID_Klimaregion auf die Kopie.
        // Rueckgabe: Projekt-Region-ID (Tab_Klimaregion.ID), 0 bei Fehler/unbekanntem Namen.
        public static int ApplyRegionByNameToProjekt(string szName, int idProjekt)
        {
            if (string.IsNullOrEmpty(szName) || idProjekt <= 0) return 0;
            object v = DataRepository.ExecuteScalar(
                "SELECT ID_Klimaregion FROM " + TAB_REGION_STAMM + " WHERE Name = ?",
                new DbParam("@name", szName));
            int stammRegionId = (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
            if (stammRegionId <= 0) return 0;
            return ApplyRegionToProjekt(stammRegionId, idProjekt);
        }

        // Bequeme Ueberladung: kopiert ueber eine eigene Transaktion.
        // Rueckgabe: die (neue oder bereits vorhandene) Projekt-Region-ID (Tab_Klimaregion.ID), 0 bei Fehler.
        public static int CopyRegionToProjekt(int stammRegionId, int idProjekt)
        {
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    int neu = CopyRegionToProjekt(stammRegionId, idProjekt, v);
                    if (neu > 0) v.Commit(); else v.Rollback();
                    return neu;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    MessageBox.Show("Fehler beim Kopieren der Klimaregion in das Projekt: " + ex.Message);
                    return 0;
                }
            }
        }

        // Kopiert eine Klimaregion samt Klimadaten und Solar aus den STAMM-Tabellen in die
        // Projekt-Tabellen, sofern fuer das Projekt noch nicht vorhanden. Setzt ID_Projekt und
        // bildet die Beziehung neu ab: die kopierten Klimadaten/Solar erhalten als ID_Klimaregion
        // die NEUE Projekt-Region-ID (Tab_Klimaregion.ID), nicht die Stamm-ID_Klimaregion.
        // Laeuft in der uebergebenen Transaktion. Rueckgabe: Projekt-Region-ID, 0 bei Fehler.
        public static int CopyRegionToProjekt(int stammRegionId, int idProjekt, DbVorgang v)
        {
            // 1. Stammdaten (Referenz) lesen – ausserhalb der Transaktion, nur lesend.
            DataTable dtRegion = DataRepository.GetDataTable(
                "SELECT * FROM " + TAB_REGION_STAMM + " WHERE ID_Klimaregion = ?",
                new DbParam("@id", stammRegionId));
            if (dtRegion == null || dtRegion.Rows.Count == 0) return 0;
            DataRow reg = dtRegion.Rows[0];
            string szName = reg["Name"].ToString();

            // 2. Bereits im Projekt vorhanden? -> vorhandene Projekt-Region-ID zurueckgeben.
            {
                List<DbParam> p = new List<DbParam>();
                p.Add(new DbParam("@name", szName));
                p.Add(new DbParam("@idProj", idProjekt));
                object ex = v.Skalar("SELECT ID FROM " + TAB_REGION_PROJEKT + " WHERE Bezeichner = ? AND ID_Projekt = ?", p.ToArray());
                if (ex != null && ex != DBNull.Value) return Convert.ToInt32(ex);
            }

            // 3. Region in Projekt-Tabelle anlegen (ID ist AutoWert), neue Region-ID holen.
            //    ARBEITSPAKET S4e: Einfuegen und ID-Rueckgabe in EINEM Aufruf auf der
            //    Verbindung des Vorgangs (frueher SELECT @@IDENTITY auf conn/trans).
            DbParam[] psRegion = {
                new DbParam("@idProj", idProjekt),
                new DbParam("@bez", szName),
                Val("@lon", reg["Longitude"]),
                Val("@lat", reg["Latitude"]),
                Val("@det", reg["Details"])
            };
            int neueRegionId = v.EinfuegenUndId(
                "INSERT INTO " + TAB_REGION_PROJEKT + " (ID_Projekt, Bezeichner, Longitude, Latitude, Details) VALUES (?, ?, ?, ?, ?)",
                psRegion);

            // 4. Klimadaten kopieren (FK ID_Klimaregion in STAMM -> neue Projekt-Region-ID).
            DataTable dtKlima = DataRepository.GetDataTable(
                "SELECT * FROM " + TAB_KLIMADATEN_STAMM + " WHERE ID_Klimaregion = ?",
                new DbParam("@id", stammRegionId));
            if (dtKlima != null)
            {
                string ins = "INSERT INTO " + TAB_KLIMADATEN_PROJEKT + " (ID_Projekt, ID_Klimaregion, Sol_Nord, Sol_Ost, Sol_Sued, Sol_West, Temperatur, WE, TagTyp_W, TagTyp_NW, Globalstrahlung, Direktstrahlung, Diffusstrahlung, Sonnenwinkel) VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?)";
                foreach (DataRow r in dtKlima.Rows)
                {
                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@idProj", idProjekt));
                        p.Add(new DbParam("@reg", neueRegionId));
                        p.Add(Val("@sn", r["Sol_Nord"]));
                        p.Add(Val("@so", r["Sol_Ost"]));
                        p.Add(Val("@ss", r["Sol_Sued"]));
                        p.Add(Val("@sw", r["Sol_West"]));
                        p.Add(Val("@temp", r["Temperatur"]));
                        p.Add(new DbParam("@we", (r["WE"] != DBNull.Value) && Convert.ToBoolean(r["WE"])));
                        p.Add(Val("@tw", r["TagTyp_W"]));
                        p.Add(Val("@tnw", r["TagTyp_NW"]));
                        p.Add(Val("@glob", ColOrNull(r, "Globalstrahlung")));
                        p.Add(Val("@dir", ColOrNull(r, "Direktstrahlung")));
                        p.Add(Val("@dif", ColOrNull(r, "Diffusstrahlung")));
                        p.Add(Val("@sw2", ColOrNull(r, "Sonnenwinkel")));
                        v.Ausfuehren(ins, p.ToArray());
                    }
                }
            }

            // 5. Solar kopieren. STAMM-FK heisst "ID_Klimaregion" (Long) = Stamm-ID_Klimaregion.
            //    Projekt-Tab_Solar.ID ist KEIN AutoWert -> explizite ID (MAX+1) vergeben.
            DataTable dtSolar = DataRepository.GetDataTable(
                "SELECT * FROM " + TAB_SOLAR_STAMM + " WHERE ID_Klimaregion = ?",
                new DbParam("@id", stammRegionId));
            if (dtSolar != null && dtSolar.Rows.Count > 0)
            {
                int nextId;
                {
                    object m = v.Skalar("SELECT MAX(ID) FROM " + TAB_SOLAR_PROJEKT);
                    nextId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                }
                string ins = "INSERT INTO " + TAB_SOLAR_PROJEKT + " (ID, ID_Projekt, ID_Klimaregion, Temperatur, Sol_Nord, Sol_Ost, Sol_Sued, Sol_West, Globalstrahlung, Direktstrahlung, Diffusstrahlung, Sonnenwinkel) VALUES (?,?,?,?,?,?,?,?,?,?,?,?)";
                foreach (DataRow r in dtSolar.Rows)
                {
                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@id", nextId++));
                        p.Add(new DbParam("@idProj", idProjekt));
                        p.Add(new DbParam("@reg", neueRegionId));
                        p.Add(Val("@temp", r["Temperatur"]));
                        p.Add(Val("@sn", r["Sol_Nord"]));
                        p.Add(Val("@so", r["Sol_Ost"]));
                        p.Add(Val("@ss", r["Sol_Sued"]));
                        p.Add(Val("@sw", r["Sol_West"]));
                        p.Add(Val("@glob", ColOrNull(r, "Globalstrahlung")));
                        p.Add(Val("@dir", ColOrNull(r, "Direktstrahlung")));
                        p.Add(Val("@dif", ColOrNull(r, "Diffusstrahlung")));
                        p.Add(Val("@sw2", ColOrNull(r, "Sonnenwinkel")));
                        v.Ausfuehren(ins, p.ToArray());
                    }
                }
            }

            return neueRegionId;
        }

        private static DbParam Val(string name, object value)
        {
            return new DbParam(name, value ?? DBNull.Value);
        }

        private static object ColOrNull(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) ? row[col] : DBNull.Value;
        }

        #endregion
    }
}
