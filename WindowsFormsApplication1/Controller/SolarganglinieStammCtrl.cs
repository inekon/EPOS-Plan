using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    // Controller fuer die Solarganglinie-STAMMDATEN
    // (Tab_Solarganglinie_STAMM + Tab_SolarganglinieDaten_STAMM).
    // Kopf-Schluessel = ID, Name = Bezeichner, zusaetzlich Beschreibung; Feld ReadOnly.
    // Enthaelt die Admin-Operationen (Import/Loeschen) sowie die zentrale Kopierlogik
    // STAMM -> Projekt (Ganglinie-Kopf + Daten). Analog zu StromganglinieStammCtrl.
    class SolarganglinieStammCtrl
    {
        public const string HEAD_STAMM = "Tab_Solarganglinie_STAMM";
        public const string DATA_STAMM = "Tab_SolarganglinieDaten_STAMM";
        public const string HEAD_PROJ  = "Tab_Solarganglinie";
        public const string DATA_PROJ  = "Tab_SolarganglinieDaten";

        private List<SolarganglinieModel> _internalList = new List<SolarganglinieModel>();
        public int rows => _internalList.Count;
        public List<SolarganglinieModel> items => _internalList;

        // Liest alle Stamm-Ganglinien (Kopfdaten) in die Liste.
        public void ReadAll()
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + HEAD_STAMM + " ORDER BY Bezeichner", null);
            _internalList.Clear();
            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                SolarganglinieModel item = new SolarganglinieModel();
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                {
                    item.ID = Convert.ToInt32(row["ID"]);
                    item.m_ID_Ganglinie = item.ID;
                }
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();
                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                    item.m_szBeschreibung = row["Beschreibung"].ToString();
                _internalList.Add(item);
            }
        }

        public bool IsReadOnly(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + HEAD_STAMM + " WHERE Bezeichner = ?",
                new DbParam("@bez", szName ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        public int GetStammId(string szName)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + HEAD_STAMM + " WHERE Bezeichner = ?",
                new DbParam("@bez", szName ?? ""));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Loescht eine Stamm-Ganglinie samt Daten, sofern nicht schreibgeschuetzt.
        public bool Delete(string szName)
        {
            if (IsReadOnly(szName))
            {
                MessageBox.Show("Diese Solarganglinie ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.", "Hinweis");
                return false;
            }
            int id = GetStammId(szName);
            if (id <= 0) return false;

            DataRepository.ExecuteSQL("DELETE FROM " + DATA_STAMM + " WHERE ID_Ganglinie = ?",
                new DbParam("@id", id));
            return DataRepository.ExecuteSQL("DELETE FROM " + HEAD_STAMM + " WHERE ID = ?",
                new DbParam("@id", id));
        }

        // Import einer neuen Ganglinie in die STAMM-Tabellen (Admin-Dialog "Einlesen").
        // Kopf-ID und Daten-IDs explizit (MAX+1), ReadOnly = false. Alles in einer Transaktion.
        public bool ImportGanglinie(string szBezeichner, string szBeschreibung, List<string> roheWerte)
        {
            if (roheWerte == null || roheWerte.Count == 0) return false;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    int neueId = 1;
                    {
                        object m = v.Skalar("SELECT MAX(ID) FROM " + HEAD_STAMM);
                        neueId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                    }

                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = neueId });
                        p.Add(new DbParam("@bez", DbParamTyp.VarWChar) { Wert = szBezeichner ?? (object)DBNull.Value });
                        p.Add(new DbParam("@beschr", DbParamTyp.VarWChar) { Wert = szBeschreibung ?? (object)DBNull.Value });
                        p.Add(new DbParam("@ro", DbParamTyp.Boolean) { Wert = false });
                        v.Ausfuehren("INSERT INTO " + HEAD_STAMM + " (ID, Bezeichner, Beschreibung, ReadOnly) VALUES (?, ?, ?, ?)", p.ToArray());
                    }

                    int datenId = 1;
                    {
                        object m = v.Skalar("SELECT MAX(ID) FROM " + DATA_STAMM);
                        datenId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                    }

                    foreach (string s in roheWerte)
                    {
                        v.Ausfuehren(
                            "INSERT INTO " + DATA_STAMM + " (ID, ID_Ganglinie, Wert, ReadOnly) VALUES (?, ?, ?, ?)",
                            new DbParam("@id", DbParamTyp.Integer) { Wert = datenId++ },
                            new DbParam("@g", DbParamTyp.Integer) { Wert = neueId },
                            new DbParam("@w", DbParamTyp.Double)
                            { Wert = double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) },
                            new DbParam("@r", DbParamTyp.Boolean) { Wert = false });
                    }

                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    MessageBox.Show("Fehler beim Speichern der Ganglinie (Stammdaten): " + ex.Message);
                    return false;
                }
            }
        }

        // Projekt-Ganglinie-ID (Tab_Solarganglinie.ID) zu einem Bezeichner im Projekt, oder 0.
        public static int GetProjektGanglinieId(string szName, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + HEAD_PROJ + " WHERE Bezeichner = ? AND ID_Projekt = ?",
                new DbParam("@bez", szName ?? ""),
                new DbParam("@proj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Zentrale Anwendung (per Bezeichner): liefert die Projekt-Ganglinie-ID; kopiert bei Bedarf die
        // Stamm-Ganglinie (+ Daten) ins Projekt. Rueckgabe: Projekt-Ganglinie-ID, 0 bei Fehler.
        public static int ApplyGanglinieToProjekt(string szBezeichner, int idProjekt)
        {
            if (string.IsNullOrEmpty(szBezeichner) || idProjekt <= 0) return 0;

            int existing = GetProjektGanglinieId(szBezeichner, idProjekt);
            if (existing > 0) return existing;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    int neu = CopyGanglinieToProjekt(szBezeichner, idProjekt, v);
                    if (neu > 0) v.Commit(); else v.Rollback();
                    return neu;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    MessageBox.Show("Fehler beim Kopieren der Solarganglinie ins Projekt: " + ex.Message);
                    return 0;
                }
            }
        }

        // Kopiert eine Stamm-Ganglinie (per Bezeichner) samt Daten in die Projekt-Tabellen.
        // Kopf-ID und Daten-IDs im Projekt explizit (MAX+1); ID_Ganglinie = neue Kopf-ID.
        // Die Daten werden in Stamm-Reihenfolge (nach ID) kopiert, damit die Zeitreihe erhalten bleibt.
        private static int CopyGanglinieToProjekt(string szBezeichner, int idProjekt, DbVorgang v)
        {
            int stammId;
            string beschreibung;
            {
                DataTable dtKopf = v.Lese(
                    "SELECT ID, Beschreibung FROM " + HEAD_STAMM + " WHERE Bezeichner = ?",
                    new DbParam("@bez", DbParamTyp.VarWChar) { Wert = szBezeichner ?? (object)DBNull.Value });
                if (dtKopf.Rows.Count == 0) return 0;
                DataRow r = dtKopf.Rows[0];
                stammId = Convert.ToInt32(r["ID"]);
                beschreibung = r["Beschreibung"] != DBNull.Value ? r["Beschreibung"].ToString() : "";
            }

            // Neue Projekt-Kopf-ID
            int neueId;
            {
                object m = v.Skalar("SELECT MAX(ID) FROM " + HEAD_PROJ);
                neueId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
            }

            {
                List<DbParam> p = new List<DbParam>();
                p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = neueId });
                p.Add(new DbParam("@proj", DbParamTyp.Integer) { Wert = idProjekt });
                p.Add(new DbParam("@bez", DbParamTyp.VarWChar) { Wert = szBezeichner ?? (object)DBNull.Value });
                p.Add(new DbParam("@beschr", DbParamTyp.VarWChar) { Wert = (object)(beschreibung ?? "") });
                v.Ausfuehren("INSERT INTO " + HEAD_PROJ + " (ID, ID_Projekt, Bezeichner, Beschreibung) VALUES (?, ?, ?, ?)", p.ToArray());
            }

            // Daten der Stamm-Ganglinie einlesen (in Reihenfolge) ...
            List<double> werte = new List<double>();
            {
                DataTable dtWerte = v.Lese(
                    "SELECT Wert FROM " + DATA_STAMM + " WHERE ID_Ganglinie = ? ORDER BY ID",
                    new DbParam("@g", DbParamTyp.Integer) { Wert = stammId });
                foreach (DataRow r in dtWerte.Rows)
                    werte.Add(r["Wert"] != DBNull.Value ? Convert.ToDouble(r["Wert"]) : 0);
            }

            // Naechste freie Daten-ID im Projekt
            int datenId;
            {
                object m = v.Skalar("SELECT MAX(ID) FROM " + DATA_PROJ);
                datenId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
            }

            // ... und in die Projekt-Datentabelle schreiben (explizite IDs, Reihenfolge = Einfuegereihenfolge).
            foreach (double w in werte)
            {
                v.Ausfuehren(
                    "INSERT INTO " + DATA_PROJ + " (ID, ID_Ganglinie, Wert) VALUES (?, ?, ?)",
                    new DbParam("@id", DbParamTyp.Integer) { Wert = datenId++ },
                    new DbParam("@g", DbParamTyp.Integer) { Wert = neueId },
                    new DbParam("@w", DbParamTyp.Double) { Wert = w });
            }

            return neueId;
        }
    }
}
