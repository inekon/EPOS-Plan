using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // Controller fuer die Waermebedarf-STAMMDATEN (Tab_Waermebedarf_STAMM + Tab_WaermebedarfDaten_STAMM).
    // Aufbau exakt wie StromganglinieStammCtrl: Kopf-Schluessel = ID, Name = Bezeichner, Feld ReadOnly;
    // die Daten sind ueber ID_Ganglinie = Kopf-ID gruppiert. Enthaelt Admin-Import/-Loeschen (mit
    // ReadOnly-Schutz) sowie die Kopierlogik STAMM -> Projekt (Ganglinie + 8760 Daten).
    class WaermebedarfStammCtrl
    {
        public const string HEAD_STAMM = "Tab_Waermebedarf_STAMM";
        public const string DATA_STAMM = "Tab_WaermebedarfDaten_STAMM";
        public const string HEAD_PROJ  = "Tab_Waermebedarf";
        public const string DATA_PROJ  = "Tab_WaermebedarfDaten";

        private List<WaermebedarfModel> _internalList = new List<WaermebedarfModel>();
        public int rows => _internalList.Count;
        public List<WaermebedarfModel> items => _internalList;

        // Liest alle Stamm-Ganglinien (Kopfdaten) in die Liste.
        public void ReadAll()
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + HEAD_STAMM + " ORDER BY Bezeichner", null);
            _internalList.Clear();
            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                WaermebedarfModel item = new WaermebedarfModel();
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                {
                    item.ID = Convert.ToInt32(row["ID"]);
                    item.m_ID_Ganglinie = item.ID;
                }
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();
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
        /// <summary>
        /// Gibt es zu dieser Ganglinie eine PROJEKTZUORDNUNG? (iU9-W9.0d) — die Sperre vor
        /// dem Loeschen aus <c>Form_Waermebedarf.btn_Loeschen_Click</c>:304.
        ///
        /// <para><b>Der Vorlaeufer las die ganze Zuordnungstabelle</b>
        /// (<c>Select * from Z_ProjektWaermebedarf where Bezeichner ='…'</c>) und zaehlte
        /// die Zeilen. Hier steht dieselbe Bedingung als <c>COUNT(*)</c> mit Parameter —
        /// ergebnisgleich, ohne Zeichenkettenverkettung.</para>
        /// </summary>
        public bool HatProjektzuordnung(string szName)
        {
            object anzahl = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Z_ProjektWaermebedarf WHERE Bezeichner = ?",
                new DbParam("@bez", szName ?? ""));
            return anzahl != null && anzahl != DBNull.Value && Convert.ToInt32(anzahl) > 0;
        }

        public bool Delete(string szName)
        {
            if (IsReadOnly(szName))
            {
                Meldung.Hinweis("Diese Waermebedarf-Ganglinie ist schreibgeschuetzt (ReadOnly) und kann nicht geloescht werden.", "Hinweis");
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
        // Kopf-ID und Daten-IDs explizit (MAX+1), ID_Ganglinie = Kopf-ID, ReadOnly=false. Alles in einer Transaktion.
        public bool ImportGanglinie(string szBezeichner, List<string> roheWerte)
        {
            if (roheWerte == null || roheWerte.Count == 0) return false;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    int neueId;
                    {
                        object m = v.Skalar("SELECT MAX(ID) FROM " + HEAD_STAMM);
                        neueId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                    }

                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = neueId });
                        p.Add(new DbParam("@bez", DbParamTyp.VarWChar) { Wert = szBezeichner ?? (object)DBNull.Value });
                        p.Add(new DbParam("@ro", DbParamTyp.Boolean) { Wert = false });
                        v.Ausfuehren("INSERT INTO " + HEAD_STAMM + " (ID, Bezeichner, ReadOnly) VALUES (?, ?, ?)", p.ToArray());
                    }

                    int neueDatenId;
                    {
                        object m = v.Skalar("SELECT MAX(ID) FROM " + DATA_STAMM);
                        neueDatenId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                    }

                    foreach (string s in roheWerte)
                    {
                        v.Ausfuehren(
                            "INSERT INTO " + DATA_STAMM + " (ID, ID_Ganglinie, Wert, ReadOnly) VALUES (?, ?, ?, ?)",
                            new DbParam("@did", DbParamTyp.Integer) { Wert = neueDatenId++ },
                            new DbParam("@dg", DbParamTyp.Integer) { Wert = neueId },
                            new DbParam("@dw", DbParamTyp.Double)
                            { Wert = double.Parse(s, System.Globalization.CultureInfo.InvariantCulture) },
                            new DbParam("@dr", DbParamTyp.Boolean) { Wert = false });
                    }

                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    Meldung.Zeigen("Fehler beim Speichern der Waermebedarf-Ganglinie (Stammdaten): " + ex.Message);
                    return false;
                }
            }
        }

        // Projekt-Ganglinie-ID (Tab_Waermebedarf.ID) zu einem Bezeichner im Projekt, oder 0.
        public static int GetProjektGanglinieId(string szName, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + HEAD_PROJ + " WHERE Bezeichner = ? AND ID_Projekt = ?",
                new DbParam("@bez", szName ?? ""),
                new DbParam("@proj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Liefert die Projekt-Ganglinie-ID; kopiert bei Bedarf die Stamm-Ganglinie (+ Daten) ins Projekt.
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
                    Meldung.Zeigen("Fehler beim Kopieren der Waermebedarf-Ganglinie ins Projekt: " + ex.Message);
                    return 0;
                }
            }
        }

        // Kopiert eine Stamm-Ganglinie (per Bezeichner) samt Daten in die Projekt-Tabellen.
        // Kopf-ID und Daten-IDs im Projekt explizit (MAX+1); ID_Ganglinie = neue Kopf-ID.
        private static int CopyGanglinieToProjekt(string szBezeichner, int idProjekt, DbVorgang v)
        {
            int stammId;
            {
                DataTable dtKopf = v.Lese(
                    "SELECT ID FROM " + HEAD_STAMM + " WHERE Bezeichner = ?",
                    new DbParam("@bez", DbParamTyp.VarWChar) { Wert = szBezeichner ?? (object)DBNull.Value });
                if (dtKopf.Rows.Count == 0) return 0;
                stammId = Convert.ToInt32(dtKopf.Rows[0]["ID"]);
            }

            int neueId;
            {
                object m = v.Skalar("SELECT MAX(ID) FROM " + HEAD_PROJ);
                neueId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
            }

            // Projekt-Kopf (Tab_Waermebedarf) ohne ReadOnly.
            {
                List<DbParam> p = new List<DbParam>();
                p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = neueId });
                p.Add(new DbParam("@proj", DbParamTyp.Integer) { Wert = idProjekt });
                p.Add(new DbParam("@bez", DbParamTyp.VarWChar) { Wert = szBezeichner ?? (object)DBNull.Value });
                v.Ausfuehren("INSERT INTO " + HEAD_PROJ + " (ID, ID_Projekt, Bezeichner) VALUES (?, ?, ?)", p.ToArray());
            }

            // Daten der Stamm-Ganglinie in Reihenfolge lesen ...
            List<double> werte = new List<double>();
            {
                DataTable dtWerte = v.Lese(
                    "SELECT Wert FROM " + DATA_STAMM + " WHERE ID_Ganglinie = ? ORDER BY ID",
                    new DbParam("@g", DbParamTyp.Integer) { Wert = stammId });
                foreach (DataRow r in dtWerte.Rows)
                    werte.Add(r["Wert"] != DBNull.Value ? Convert.ToDouble(r["Wert"]) : 0);
            }

            int neueDatenId;
            {
                object m = v.Skalar("SELECT MAX(ID) FROM " + DATA_PROJ);
                neueDatenId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
            }

            foreach (double w in werte)
            {
                v.Ausfuehren(
                    "INSERT INTO " + DATA_PROJ + " (ID, ID_Ganglinie, Wert) VALUES (?, ?, ?)",
                    new DbParam("@did", DbParamTyp.Integer) { Wert = neueDatenId++ },
                    new DbParam("@dg", DbParamTyp.Integer) { Wert = neueId },
                    new DbParam("@dw", DbParamTyp.Double) { Wert = w });
            }

            return neueId;
        }
    }
}
