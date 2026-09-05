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

        /// <summary>
        /// Gibt es diesen Bezeichner schon im Katalog? (iU9-W9-E-3.)
        ///
        /// <para>Die Dublettenpruefung des Weges „Speichern unter" — sie laeuft VOR dem
        /// Einfuegen, damit der Anwender ein Warnbanner sieht und nicht den
        /// UNIQUE-Fehler von SQLite. Zwilling von
        /// <c>StromganglinieStammCtrl.Exists</c> (W12-E-1) und
        /// <c>SolarganglinieStammCtrl.Exists</c> (W14b.0d); geprueft wird der GANZE
        /// Name und nicht sein Anfang — das war Befund W14-B70.</para>
        /// </summary>
        public bool Exists(string szName)
        {
            if (string.IsNullOrEmpty(szName)) return false;
            return GetStammId(szName) > 0;
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

        /// <summary>
        /// Import einer neuen Ganglinie in die STAMM-Tabellen. Kopf-ID und Daten-IDs
        /// explizit (MAX+1), <c>ID_Ganglinie</c> = Kopf-ID, <c>ReadOnly = false</c>;
        /// alles in EINER Transaktion.
        ///
        /// <para><b>Der Parameter ist die bereits geprueffte und normalisierte
        /// Zahlenreihe</b> (8 760 oder 35 040 Werte in kW) aus
        /// <c>GanglinienPruefung</c> — seit dem Anwenderwunsch <b>W9‑E‑3</b>
        /// (05.09.2026) laeuft der Waermebedarf durch dieselbe Kette wie der
        /// Stromlastgang (<c>GanglinienImportAblauf</c> mit
        /// <c>GanglinienZiel.Waermebedarf</c>). Bis dahin nahm diese Methode eine
        /// rohe Zeilenliste entgegen und parste sie selbst; das Parsen liegt jetzt
        /// in der Leseschicht (<c>Allgemein\Import\GanglinienDatei</c>), das
        /// Transaktionsmuster ist unveraendert.</para>
        ///
        /// <para><b>Ein Zeitinterval traegt der Waermebedarf nicht</b> —
        /// <c>Tab_Waermebedarf_STAMM</c> hat die Spalte gar nicht, und
        /// <c>SimulationWaermebedarf</c> leitet das Raster seit jeher aus der
        /// WERTZAHL ab (8 760 oder 35 040). Deshalb fehlt der Parameter hier, und
        /// deshalb ist keine Schemaaenderung noetig.</para>
        /// </summary>
        public bool ImportGanglinie(string szBezeichner, IList<double> werte)
        {
            if (werte == null || werte.Count == 0) return false;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    EinfuegenStamm(v, szBezeichner, werte);
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

        /// <summary>
        /// Der EINE Schreibweg eines neuen Katalogsatzes: Kopf mit gerechneter Id
        /// (<c>MAX(ID)+1</c>) und <c>ReadOnly = false</c>, danach die Werte in
        /// Reihenfolge. Laeuft IM Vorgang des Aufrufers — er entscheidet ueber
        /// Commit und Rollback.
        /// </summary>
        /// <remarks>
        /// Herausgezogen mit iU9-W9-E-3, damit „CSV-Datei importieren…" und
        /// „Speichern unter…" denselben Satz Anweisungen benutzen; Zwilling von
        /// <c>StromganglinieStammCtrl.EinfuegenStamm</c>. Zwei Fassungen dieses
        /// INSERT liefen beim ersten Schemawechsel auseinander.
        /// </remarks>
        /// <returns>Die vergebene Kopf-Id.</returns>
        private static int EinfuegenStamm(DbVorgang v, string szBezeichner, IList<double> werte)
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

            foreach (double w in werte)
            {
                v.Ausfuehren(
                    "INSERT INTO " + DATA_STAMM + " (ID, ID_Ganglinie, Wert, ReadOnly) VALUES (?, ?, ?, ?)",
                    new DbParam("@did", DbParamTyp.Integer) { Wert = neueDatenId++ },
                    new DbParam("@dg", DbParamTyp.Integer) { Wert = neueId },
                    new DbParam("@dw", DbParamTyp.Double) { Wert = w },
                    new DbParam("@dr", DbParamTyp.Boolean) { Wert = false });
            }

            return neueId;
        }

        /// <summary>
        /// Ersetzt beim Import-Ueberschreiben die Werte einer vorhandenen Ganglinie:
        /// Kopfsatz und ID bleiben stehen, die Datenzeilen werden in einer
        /// Transaktion getauscht (Dublettenkonzept 4.4).
        /// </summary>
        /// <remarks>
        /// Bewusst OHNE ReadOnly-Sperre: Das Ueberschreiben eines ReadOnly-Satzes ist
        /// erlaubt und wird vorher im Konfliktdialog bestaetigt (Entscheidung 9.2 —
        /// erlauben mit Hinweis). Bis W9-E-3 loeschte die Waermebedarfsverwaltung an
        /// dieser Stelle den ganzen Satz und legte ihn neu an — die Kopf-Id wechselte
        /// dabei, und eine Projektkopie verlor ihren Bezug. Transaktionsmuster wie
        /// <see cref="ImportGanglinie"/>.
        /// </remarks>
        public bool ErsetzeGanglinie(string szBezeichner, IList<double> werte)
        {
            if (werte == null || werte.Count == 0) return false;

            int id = GetStammId(szBezeichner);
            if (id <= 0) return false;

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    v.Ausfuehren("DELETE FROM " + DATA_STAMM + " WHERE ID_Ganglinie = ?",
                        new DbParam("@id", DbParamTyp.Integer) { Wert = id });

                    int neueDatenId;
                    {
                        object m = v.Skalar("SELECT MAX(ID) FROM " + DATA_STAMM);
                        neueDatenId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m) : 0) + 1;
                    }

                    foreach (double w in werte)
                    {
                        v.Ausfuehren(
                            "INSERT INTO " + DATA_STAMM + " (ID, ID_Ganglinie, Wert, ReadOnly) VALUES (?, ?, ?, ?)",
                            new DbParam("@did", DbParamTyp.Integer) { Wert = neueDatenId++ },
                            new DbParam("@dg", DbParamTyp.Integer) { Wert = id },
                            new DbParam("@dw", DbParamTyp.Double) { Wert = w },
                            new DbParam("@dr", DbParamTyp.Boolean) { Wert = false });
                    }

                    v.Commit();
                    return true;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    Meldung.Zeigen("Fehler beim Ersetzen der Waermebedarf-Ganglinie (Stammdaten): " + ex.Message);
                    return false;
                }
            }
        }

        /// <summary>
        /// <b>„Speichern unter" — eine Katalogganglinie unter neuem Namen</b>
        /// (iU9-W9-E-3, Anwenderwunsch der Windows-Abnahme vom 05.09.2026).
        ///
        /// <para>Kopf UND Werte werden kopiert; die Kopie traegt immer
        /// <c>ReadOnly = false</c>, auch wenn die Quelle zur Auslieferung gehoert —
        /// eine Kopie ist Anwenderbestand. Die Werte gehen in Stamm-Reihenfolge
        /// (<c>ORDER BY ID</c>) hinueber, damit die Zeitreihe erhalten bleibt;
        /// dieselbe Regel wie in <see cref="CopyGanglinieToProjekt"/>.</para>
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
                    {
                        DataTable dtKopf = v.Lese(
                            "SELECT ID FROM " + HEAD_STAMM + " WHERE Bezeichner = ?",
                            new DbParam("@bez", DbParamTyp.VarWChar) { Wert = szQuelle });
                        if (dtKopf.Rows.Count == 0) { v.Rollback(); return 0; }
                        quellId = Convert.ToInt32(dtKopf.Rows[0]["ID"]);
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

                    int neueId = EinfuegenStamm(v, ziel, werte);
                    v.Commit();
                    return neueId;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    Meldung.Zeigen("Fehler beim Kopieren der Waermebedarf-Ganglinie: " + ex.Message);
                    return 0;
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
