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
                // iU9-W12-E-1: Das Auslieferungskennzeichen kommt mit SELECT * ohnehin
                // mit; bis hierher fiel es weg und jede Huelle fragte es einzeln nach.
                if (dt.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value)
                    item.m_bReadOnly = Convert.ToBoolean(row["ReadOnly"]);
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

        /// <summary>
        /// Gibt es diesen Bezeichner schon im Katalog? (iU9-W12-E-1.)
        ///
        /// <para>Die Dublettenpruefung des Weges „Speichern unter" — sie laeuft VOR dem
        /// Einfuegen, damit der Anwender ein Warnbanner sieht und nicht den
        /// UNIQUE-Fehler von SQLite. Zwilling von
        /// <c>SolarganglinieStammCtrl.Exists</c> (W14b.0d).</para>
        /// </summary>
        public bool Exists(string szName)
        {
            if (string.IsNullOrEmpty(szName)) return false;
            return GetStammId(szName) > 0;
        }

        /// <summary>
        /// Gibt es zu dieser Ganglinie eine PROJEKTZUORDNUNG? (iU9-W12-E-1) — die Sperre
        /// vor dem Loeschen, Zwilling von <c>SolarganglinieStammCtrl.HatProjektzuordnung</c>
        /// (W14b.0d) und <c>WaermebedarfStammCtrl</c> (W9.0d).
        /// </summary>
        /// <remarks>
        /// <para><b>Warum ueber den Bezeichner und nicht ueber eine Id.</b>
        /// <c>Z_ProjektStromganglinie.ID_Ganglinie</c> zeigt auf die PROJEKTKOPIE
        /// (<c>Tab_Stromganglinie.ID</c>), nicht auf den Katalogsatz; einen Rueckweg
        /// Kopie → Katalogsatz fuehrt das Schema nicht. Die Zuordnungstabelle traegt
        /// aber den Bezeichner selbst mit, und genau ihn haelt diese Zaehlung dagegen —
        /// dieselbe Bedingung, die die Solarfassung seit W14b prueft. Eine NEUE
        /// Beziehung entsteht hier nicht; sie wird gelesen.</para>
        /// </remarks>
        public bool HatProjektzuordnung(string szName)
        {
            object anzahl = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Z_ProjektStromganglinie WHERE Bezeichner = ?",
                new DbParam("@bez", szName ?? ""));
            return anzahl != null && anzahl != DBNull.Value && Convert.ToInt32(anzahl) > 0;
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
                    EinfuegenStamm(v, szBezeichner, zeitinterval, werte);
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
        /// Der EINE Schreibweg eines neuen Katalogsatzes: Kopf mit gerechneter Id
        /// (<c>MAX(ID)+1</c>) und <c>ReadOnly = false</c>, danach die Werte in
        /// Reihenfolge. Laeuft IM Vorgang des Aufrufers — er entscheidet ueber
        /// Commit und Rollback.
        /// </summary>
        /// <remarks>
        /// Herausgezogen mit iU9-W12-E-1, damit „Datei einlesen" und
        /// „Speichern unter" denselben Satz Anweisungen benutzen. Der Rumpf ist
        /// woertlich der von <see cref="ImportGanglinie"/>; zwei Fassungen dieses
        /// INSERT liefen beim ersten Schemawechsel auseinander.
        /// </remarks>
        /// <returns>Die vergebene Kopf-Id.</returns>
        private static int EinfuegenStamm(DbVorgang v, string szBezeichner, int zeitinterval,
                                          IList<double> werte)
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

            return neueId;
        }

        /// <summary>
        /// <b>„Speichern unter" — eine Katalogganglinie unter neuem Namen</b>
        /// (iU9-W12-E-1, Anwenderwunsch der Windows-Abnahme vom 05.09.2026).
        ///
        /// <para>Kopf UND Werte werden kopiert; die Kopie traegt immer
        /// <c>ReadOnly = false</c>, auch wenn die Quelle zur Auslieferung gehoert —
        /// eine Kopie ist Anwenderbestand. Die Werte gehen in Stamm-Reihenfolge
        /// (<c>ORDER BY ID</c>) hinueber, damit die Zeitreihe erhalten bleibt;
        /// dieselbe Regel wie in <c>CopyGanglinieToProjekt</c>.</para>
        ///
        /// <para><b>Die Dublettenpruefung steht VOR dem Einfuegen:</b> Ein Name, den es
        /// schon gibt, ergibt <c>0</c> und keine Zeile — nicht einen UNIQUE-Fehler,
        /// den der Anwender als Ausnahmetext saehe.</para>
        /// </summary>
        /// <param name="szQuelle">Bezeichner des zu kopierenden Katalogsatzes.</param>
        /// <param name="szZiel">Name der Kopie; wird getrimmt.</param>
        /// <returns>Die Kopf-Id der Kopie, oder <c>0</c>: Quelle fehlt, Name leer,
        /// Name vergeben, Quelle ohne Werte oder Schreibfehler.</returns>
        public int KopiereStamm(string szQuelle, string szZiel)
        {
            if (string.IsNullOrEmpty(szQuelle) || string.IsNullOrWhiteSpace(szZiel)) return 0;

            string ziel = szZiel.Trim();
            if (Exists(ziel)) return 0;      // Dublette: der Aufrufer meldet, wir werfen nicht

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    int quellId;
                    int zeitinterval;
                    {
                        DataTable dtKopf = v.Lese(
                            "SELECT ID, Zeitinterval FROM " + HEAD_STAMM + " WHERE Bezeichner = ?",
                            new DbParam("@bez", DbParamTyp.VarWChar) { Wert = szQuelle });
                        if (dtKopf.Rows.Count == 0) { v.Rollback(); return 0; }
                        DataRow r = dtKopf.Rows[0];
                        quellId = Convert.ToInt32(r["ID"]);
                        zeitinterval = r["Zeitinterval"] != DBNull.Value ? Convert.ToInt32(r["Zeitinterval"]) : 0;
                    }

                    List<double> werte = new List<double>();
                    {
                        DataTable dtWerte = v.Lese(
                            "SELECT Wert FROM " + DATA_STAMM + " WHERE ID_Ganglinie = ? ORDER BY ID",
                            new DbParam("@g", DbParamTyp.Integer) { Wert = quellId });
                        foreach (DataRow r in dtWerte.Rows)
                            werte.Add(r["Wert"] != DBNull.Value ? Convert.ToDouble(r["Wert"]) : 0);
                    }

                    if (werte.Count == 0) { v.Rollback(); return 0; }

                    int neueId = EinfuegenStamm(v, ziel, zeitinterval, werte);
                    v.Commit();
                    return neueId;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    Meldung.Zeigen("Fehler beim Kopieren der Stromganglinie: " + ex.Message);
                    return 0;
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
