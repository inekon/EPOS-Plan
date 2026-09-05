using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // Controller fuer die Stromganglinie-STAMMDATEN
    // (Tab_Stromganglinie_STAMM + Tab_StromganglinieDaten_STAMM).
    // Kopf-Schluessel = ID, Name = Bezeichner; neues Feld ReadOnly. Enthaelt die Admin-Operationen
    // (Import/Loeschen) sowie die zentrale Kopierlogik STAMM -> Projekt (Ganglinie + Daten).
    class StromganglinieStammCtrl
    {
        public const string HEAD_STAMM = "Tab_Stromganglinie_STAMM";
        public const string DATA_STAMM = "Tab_StromganglinieDaten_STAMM";
        public const string HEAD_PROJ  = "Tab_Stromganglinie";
        public const string DATA_PROJ  = "Tab_StromganglinieDaten";

        private List<StromganglinieModel> _internalList = new List<StromganglinieModel>();
        public int rows => _internalList.Count;
        public List<StromganglinieModel> items => _internalList;

        // Liest alle Stamm-Ganglinien (Kopfdaten) in die Liste.
        public void ReadAll()
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + HEAD_STAMM + " ORDER BY Bezeichner", null);
            _internalList.Clear();
            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                StromganglinieModel item = new StromganglinieModel();
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                {
                    item.ID = Convert.ToInt32(row["ID"]);
                    item.m_ID_Ganglinie = item.ID;
                }
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.m_szBezeichner = row["Bezeichner"].ToString();
                if (dt.Columns.Contains("Zeitinterval") && row["Zeitinterval"] != DBNull.Value)
                    item.m_Zeitinterval = Convert.ToInt32(row["Zeitinterval"]);
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

        /// <summary>
        /// Der Katalogsatz zu einem Bezeichner — der Ersatz fuer das konkatenierte
        /// <c>"SELECT * from Tab_Stromganglinie_STAMM where Bezeichner='" + text + "'"</c>
        /// aus <c>Form_Stromganglinie.cs:72</c> (Befund W12-B4: ein Bezeichner mit
        /// Apostroph brach die Abfrage).
        /// </summary>
        /// <param name="szName">Bezeichner des Katalogeintrags.</param>
        /// <returns>Der Satz oder <c>null</c>, wenn es ihn nicht gibt.</returns>
        public static StromganglinieModel FindeStamm(string szName)
        {
            if (string.IsNullOrEmpty(szName)) return null;

            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Zeitinterval FROM " + HEAD_STAMM + " WHERE Bezeichner = ?",
                new DbParam("@bez", szName));
            if (dt == null || dt.Rows.Count == 0) return null;

            DataRow row = dt.Rows[0];
            StromganglinieModel model = new StromganglinieModel();
            if (row["ID"] != DBNull.Value)
            {
                model.ID = Convert.ToInt32(row["ID"]);
                model.m_ID_Ganglinie = model.ID;
            }
            if (row["Bezeichner"] != DBNull.Value) model.m_szBezeichner = row["Bezeichner"].ToString();
            if (row["Zeitinterval"] != DBNull.Value) model.m_Zeitinterval = Convert.ToInt32(row["Zeitinterval"]);
            return model;
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
                Meldung.Hinweis("Diese Stromganglinie ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.", "Hinweis");
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
        // Kopf-ID explizit (MAX+1), ReadOnly=false; Daten-ID ist AutoWert. Alles in einer Transaktion.
        //
        // AP5: Der Parameter ist die bereits geprueffte und normalisierte Zahlenreihe
        // (8.760 oder 35.040 Werte in kW) aus GanglinienPruefung statt der frueheren
        // rohen Zeilenliste. Das Parsen liegt jetzt in der Leseschicht
        // (Allgemein\Import\GanglinienDatei), das Transaktionsmuster ist unveraendert.
        public bool ImportGanglinie(string szBezeichner, int zeitinterval, IList<double> werte)
        {
            if (werte == null || werte.Count == 0) return false;

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
                        p.Add(new DbParam("@int", DbParamTyp.Integer) { Wert = zeitinterval });
                        p.Add(new DbParam("@ro", DbParamTyp.Boolean) { Wert = false });
                        v.Ausfuehren("INSERT INTO " + HEAD_STAMM + " (ID, Bezeichner, Zeitinterval, ReadOnly) VALUES (?, ?, ?, ?)", p.ToArray());
                    }

                    foreach (double w in werte)
                    {
                        v.Ausfuehren(
                            "INSERT INTO " + DATA_STAMM + " (ID_Ganglinie, Wert, ReadOnly) VALUES (?, ?, ?)",
                            new DbParam("@g", DbParamTyp.Integer) { Wert = neueId },
                            new DbParam("@w", DbParamTyp.Double) { Wert = w },
                            new DbParam("@r", DbParamTyp.Boolean) { Wert = false });
                    }

                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    Meldung.Zeigen("Fehler beim Speichern der Ganglinie (Stammdaten): " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// Ersetzt beim Import-Ueberschreiben die Werte einer vorhandenen Ganglinie:
        /// Kopfsatz und ID bleiben stehen, nur das Zeitinterval wird aktualisiert und
        /// die Datenzeilen werden in einer Transaktion getauscht (Dublettenkonzept 4.4).
        /// </summary>
        /// <remarks>
        /// Bewusst OHNE ReadOnly-Sperre: Das Ueberschreiben eines ReadOnly-Satzes ist
        /// erlaubt und wird vorher im Konfliktdialog bestaetigt (Entscheidung 9.2 -
        /// erlauben mit Hinweis). Transaktionsmuster wie <see cref="ImportGanglinie"/>.
        /// </remarks>
        public bool ErsetzeGanglinie(string szBezeichner, int zeitinterval, IList<double> werte)
        {
            if (werte == null || werte.Count == 0) return false;

            int id = GetStammId(szBezeichner);
            if (id <= 0) return false;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@int", DbParamTyp.Integer) { Wert = zeitinterval });
                        p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = id });
                        v.Ausfuehren("UPDATE " + HEAD_STAMM + " SET Zeitinterval = ? WHERE ID = ?", p.ToArray());
                    }

                    {
                        List<DbParam> p = new List<DbParam>();
                        p.Add(new DbParam("@id", DbParamTyp.Integer) { Wert = id });
                        v.Ausfuehren("DELETE FROM " + DATA_STAMM + " WHERE ID_Ganglinie = ?", p.ToArray());
                    }

                    foreach (double w in werte)
                    {
                        v.Ausfuehren(
                            "INSERT INTO " + DATA_STAMM + " (ID_Ganglinie, Wert, ReadOnly) VALUES (?, ?, ?)",
                            new DbParam("@g", DbParamTyp.Integer) { Wert = id },
                            new DbParam("@w", DbParamTyp.Double) { Wert = w },
                            new DbParam("@r", DbParamTyp.Boolean) { Wert = false });
                    }

                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    Meldung.Zeigen("Fehler beim Ersetzen der Ganglinie (Stammdaten): " + ex.Message);
                    return false;
                }
            }
        }

        // Projekt-Ganglinie-ID (Tab_Stromganglinie.ID) zu einem Bezeichner im Projekt, oder 0.
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
                    Meldung.Zeigen("Fehler beim Kopieren der Stromganglinie ins Projekt: " + ex.Message);
                    return 0;
                }
            }
        }

        // Kopiert eine Stamm-Ganglinie (per Bezeichner) samt Daten in die Projekt-Tabellen.
        // Kopf-ID im Projekt explizit (MAX+1); Daten-ID ist AutoWert; ID_Ganglinie = neue Kopf-ID.
        // Die Daten werden in Stamm-Reihenfolge (nach ID) kopiert, damit die Zeitreihe erhalten bleibt.
        private static int CopyGanglinieToProjekt(string szBezeichner, int idProjekt, DbVorgang v)
        {
            int stammId;
            int zeitinterval;
            {
                DataTable dtKopf = v.Lese(
                    "SELECT ID, Zeitinterval FROM " + HEAD_STAMM + " WHERE Bezeichner = ?",
                    new DbParam("@bez", DbParamTyp.VarWChar) { Wert = szBezeichner ?? (object)DBNull.Value });
                if (dtKopf.Rows.Count == 0) return 0;
                DataRow r = dtKopf.Rows[0];
                stammId = Convert.ToInt32(r["ID"]);
                zeitinterval = r["Zeitinterval"] != DBNull.Value ? Convert.ToInt32(r["Zeitinterval"]) : 0;
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
                p.Add(new DbParam("@int", DbParamTyp.Integer) { Wert = zeitinterval });
                v.Ausfuehren("INSERT INTO " + HEAD_PROJ + " (ID, ID_Projekt, Bezeichner, Zeitinterval) VALUES (?, ?, ?, ?)", p.ToArray());
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

            // ... und in die Projekt-Datentabelle schreiben (ID = AutoWert, Reihenfolge = Einfuegereihenfolge).
            foreach (double w in werte)
            {
                v.Ausfuehren(
                    "INSERT INTO " + DATA_PROJ + " (ID_Ganglinie, Wert) VALUES (?, ?)",
                    new DbParam("@g", DbParamTyp.Integer) { Wert = neueId },
                    new DbParam("@w", DbParamTyp.Double) { Wert = w });
            }

            return neueId;
        }
    }
}
