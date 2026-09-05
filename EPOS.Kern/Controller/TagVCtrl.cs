using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    class TagVCtrl : TagVModel
    {
        // Das besprochene dynamische Listen-Schema
        private List<TagVModel> _internalList = new List<TagVModel>();
        public int rows => _internalList.Count;
        public new List<TagVModel> items => _internalList;

        public TagVCtrl()
        {
        }

        // Der Parametersatz ist mit iU9-W8.0d dazugekommen: Der Gebaeudetyp wird ueber
        // seinen Bezeichner gelesen, und der kam bisher als Zeichenkette in den
        // Anweisungstext. Bestandsaufrufe ohne Parameter bleiben unveraendert gueltig.
        public void ReadAll(string sql, params DbParam[] parameter)
        {
            // Daten abrufen über das zentrale DataRepository
            DataTable dt = DataRepository.GetDataTable(sql, parameter);

            // Interne Liste vor dem erneuten Laden leeren
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                // Korrektur: Hier wird nun korrekterweise das Model (statt des Controllers) erzeugt
                TagVModel item = new TagVModel();

                // Spaltenbasiertes, sicheres Auslesen über Spaltennamen statt numerischer Indizes
                if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value)
                    item.ID = Convert.ToInt32(row["ID"]);

                if (dt.Columns.Contains("Name") && row["Name"] != DBNull.Value)
                    item.Name = row["Name"].ToString();

                if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value)
                    item.Beschreibung = row["Beschreibung"].ToString();

                if (dt.Columns.Contains("Veraenderbar") && row["Veraenderbar"] != DBNull.Value)
                    item.Veraenderbar = Convert.ToBoolean(row["Veraenderbar"]);

                // Fallback, falls die Spalte in der Access-Tabelle "Veränderbar" (mit Umlaut) geschrieben ist
                else if (dt.Columns.Contains("Veränderbar") && row["Veränderbar"] != DBNull.Value)
                    item.Veraenderbar = Convert.ToBoolean(row["Veränderbar"]);

                // STAMM-Katalog: Namensfeld heißt "Bezeichner" (wird im Model als Name geführt)
                if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value)
                    item.Name = row["Bezeichner"].ToString();

                // Neues STAMM-Schutzfeld
                if (dt.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value)
                    item.ReadOnly = Convert.ToBoolean(row["ReadOnly"]);

                // Das fertige Element der dynamischen Liste hinzufügen
                _internalList.Add(item);
            }
        }

        // ===================================================== Gebaeudetyp-Verwaltung (iU9-W8.0d)

        /// <summary>Kopftabelle der Tagesverteilungen.</summary>
        public const string TABLE = "Tab_DBTagV_STAMM";

        /// <summary>Detailtabelle: eine Zeile je Stundenwert, 24 je Kurve.</summary>
        public const string TABLE_DATEN = "Tab_DBTagVDaten_STAMM";

        /// <summary>Stunden je Kurve — das feste Raster des Rechenkerns.</summary>
        public const int STUNDEN = 24;

        /// <summary>
        /// Kurven eines neu angelegten Gebaeudetyps: acht Kurven zu 24 Stunden = 192
        /// Datenzeilen. Woertlich aus <c>Form_EingGebTyp.btn_EingneuerTyp_Click</c>:253.
        /// </summary>
        public const int NEUE_ZEILEN = 192;

        /// <summary>
        /// Die Namensliste der Gebaeudetypen, sortiert wie im Vorlaeufer
        /// (<c>SetControls</c>:32: <c>order by Bezeichner</c>).
        /// </summary>
        public static List<string> Typen()
        {
            var liste = new List<string>();
            var ctrl = new TagVCtrl();
            ctrl.ReadAll("select * from " + TABLE + " order by Bezeichner");
            for (int i = 0; i < ctrl.rows; i++) liste.Add(ctrl.items[i].Name ?? "");
            return liste;
        }

        /// <summary>
        /// Kopf und Tagesverteilungen eines Gebaeudetyps; <c>null</c>, wenn es ihn nicht
        /// (mehr) gibt. <c>Verteilung</c> ist [Kurve, Stunde].
        ///
        /// <para>Woertlich aus <c>listBox_Typename_SelectedIndexChanged</c>:44 — erst der
        /// Kopf, dann <c>Count('Verteilung')</c> als KURVENZAHL x 24 und zuletzt die
        /// Datenzeilen <c>order by ID</c>. Die Reihenfolge der Zeilen IST die Zuordnung
        /// zur Kurve; eine Kurvennummer fuehrt die Tabelle nicht.</para>
        /// </summary>
        public static (TagVModel Kopf, double[,] Verteilung, int Kurven)? Lies(string bezeichner)
        {
            var ctrl = new TagVCtrl();
            ctrl.ReadAll("select * from " + TABLE + " where Bezeichner = ?",
                         new DbParam("@bez", bezeichner ?? ""));
            if (ctrl.rows == 0) return null;

            TagVModel kopf = ctrl.items[0];

            object anzahl = DataRepository.ExecuteScalar(
                "SELECT Count('Verteilung') AS Ausdr1 FROM " + TABLE_DATEN + " WHERE ID_TagV = ?",
                new DbParam("@id", kopf.ID));
            int zeilen = (anzahl == null || anzahl == DBNull.Value) ? 0 : Convert.ToInt32(anzahl);
            int kurven = zeilen / STUNDEN;
            if (kurven <= 0) return (kopf, new double[0, STUNDEN], 0);

            var verteilung = new double[kurven, STUNDEN];
            DataTable dt = DataRepository.GetDataTable(
                "select * from " + TABLE_DATEN + " where ID_TagV = ? order by ID",
                new DbParam("@id", kopf.ID));
            if (dt != null)
                for (int n = 0; n < kurven; n++)
                    for (int i = 0; i < STUNDEN; i++)
                    {
                        int pos = n * STUNDEN + i;
                        if (pos >= dt.Rows.Count) break;
                        object v = dt.Rows[pos]["Verteilung"];
                        verteilung[n, i] = (v == DBNull.Value) ? 0.0 : Convert.ToDouble(v);
                    }

            return (kopf, verteilung, kurven);
        }

        /// <summary>
        /// Schreibt die Tagesverteilungen zurueck — in EINER Transaktion.
        ///
        /// <para>Woertlich aus <c>Form_EingGebTyp.TagV_Speichern</c>:215: Die IDs der
        /// Datenzeilen werden in stabiler Reihenfolge geladen und je Zeile typisiert
        /// zurueckgeschrieben. Die Transaktion ist neu (A-8): Der Vorlaeufer schickte bis
        /// zu 192 Einzelanweisungen los, und <c>Tab_DBTagVDaten_STAMM</c> ist
        /// Simulationseingang — ein Fehler in der Mitte hinterliess einen halben Stand.</para>
        /// </summary>
        public static bool Speichern(int idTagV, double[,] verteilung)
        {
            if (verteilung == null) return false;
            int kurven = verteilung.GetLength(0);
            if (kurven <= 0 || verteilung.GetLength(1) < STUNDEN) return false;

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "select ID from " + TABLE_DATEN + " where ID_TagV = ? order by ID",
                    new DbParam("@id", idTagV));
                if (dt == null) return false;

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    for (int n = 0; n < kurven; n++)
                        for (int i = 0; i < STUNDEN; i++)
                        {
                            int pos = n * STUNDEN + i;
                            if (pos >= dt.Rows.Count) break;
                            v.Ausfuehren(
                                "update " + TABLE_DATEN + " set Verteilung = ? where ID = ?",
                                new DbParam("@vv", DbParamTyp.Double) { Wert = verteilung[n, i] },
                                new DbParam("@rid", DbParamTyp.Integer) { Wert = Convert.ToInt32(dt.Rows[pos]["ID"]) });
                        }
                    v.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern der Tagesverteilung: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Legt einen Gebaeudetyp an: Kopf (<c>Veraenderbar = true</c>,
        /// <c>ReadOnly = false</c>) und 192 Datenzeilen zu 0. Rueckgabe: die neue ID
        /// oder 0.
        ///
        /// <para>Woertlich aus <c>btn_EingneuerTyp_Click</c>:253, samt der ausdruecklichen
        /// ID-Vergabe ueber <c>GetMaxID + 1</c> — auch fuer die Datenzeilen, die fortlaufend
        /// weitergezaehlt werden. Neu ist die Transaktion: 193 Einzelschreibungen ohne
        /// Klammer konnten einen Kopf OHNE Verteilungen hinterlassen, und den haette die
        /// Maske danach mit null Kurven angezeigt (Befund R-W8-4 zur ID-Vergabe steht im
        /// Protokoll).</para>
        /// </summary>
        public static int Anlegen(string bezeichner, string beschreibung)
        {
            if (string.IsNullOrEmpty(bezeichner)) return 0;

            try
            {
                int nID = DataRepository.GetMaxID(TABLE) + 1;
                int nextDid = DataRepository.GetMaxID(TABLE_DATEN) + 1;

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    v.Ausfuehren(
                        "INSERT INTO " + TABLE + " (ID, Bezeichner, Beschreibung, Veraenderbar, ReadOnly) VALUES (?, ?, ?, ?, ?)",
                        new DbParam("@nid", DbParamTyp.Integer) { Wert = nID },
                        new DbParam("@bez", DbParamTyp.VarWChar) { Wert = (object)bezeichner },
                        new DbParam("@besch", DbParamTyp.VarWChar) { Wert = (object)(beschreibung ?? "") },
                        new DbParam("@ver", DbParamTyp.Boolean) { Wert = true },
                        new DbParam("@ro", DbParamTyp.Boolean) { Wert = false });

                    for (int i = 0; i < NEUE_ZEILEN; i++)
                        v.Ausfuehren(
                            "INSERT INTO " + TABLE_DATEN + " (ID, ID_TagV, Verteilung, ReadOnly) VALUES (?, ?, ?, ?)",
                            new DbParam("@did", DbParamTyp.Integer) { Wert = nextDid++ },
                            new DbParam("@dtag", DbParamTyp.Integer) { Wert = nID },
                            new DbParam("@dv", DbParamTyp.Double) { Wert = 0.0 },
                            new DbParam("@dro", DbParamTyp.Boolean) { Wert = false });

                    v.Commit();
                }
                return nID;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Anlegen des Gebaeudetyps: " + ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// Loescht einen Gebaeudetyp — DETAIL VOR KOPF, wie im Vorlaeufer
        /// (<c>btn_Loeschen_Click</c>:313). Die Sperre gegen Auslieferungsbestand prueft
        /// der Aufrufer vorher ueber den Kopf (<c>ReadOnly</c> bzw. <c>Veraenderbar</c>).
        /// </summary>
        public static bool Loeschen(int idTagV)
        {
            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    v.Ausfuehren("DELETE FROM " + TABLE_DATEN + " WHERE ID_TagV = ?",
                                 new DbParam("@idt", DbParamTyp.Integer) { Wert = idTagV });
                    v.Ausfuehren("DELETE FROM " + TABLE + " WHERE ID = ?",
                                 new DbParam("@idk", DbParamTyp.Integer) { Wert = idTagV });
                    v.Commit();
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Loeschen des Gebaeudetyps: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Die Namen der Tageskurven eines Gebaeudetyps — fuenf bei bis zu fuenf Kurven,
        /// sonst acht.
        ///
        /// <para><b>Entschieden wird ueber die KURVENZAHL, nicht ueber die Listenposition</b>
        /// (Kommentar <c>GetTagVName</c>:108): Die Typliste ist alphabetisch sortiert, und
        /// der 5-Kurven-Typ steht nicht immer vorn.</para>
        ///
        /// <para>Die Namen selbst sind ANZEIGETEXTE und stehen deshalb in
        /// <c>MyResource.Resource</c> (<c>GTYP_KURVE_*</c>); ohne Ressource gilt der
        /// deutsche Wortlaut des Vorlaeufers.</para>
        /// </summary>
        public static List<string> KurvenNamen(int kurven)
        {
            string[] kurz =
            {
                Text("GTYP_KURVE_K1", "Winter-heiter"), Text("GTYP_KURVE_K2", "Winter-trübe"),
                Text("GTYP_KURVE_K3", "Übergang-heiter"), Text("GTYP_KURVE_K4", "Übergang-trübe"),
                Text("GTYP_KURVE_K5", "Sommertag")
            };
            string[] lang =
            {
                Text("GTYP_KURVE_L1", "Winter-Wochentag"), Text("GTYP_KURVE_L2", "Winter-Wochenende"),
                Text("GTYP_KURVE_L3", "Übergang1-Wochentag"), Text("GTYP_KURVE_L4", "Übergang1-Wochenende"),
                Text("GTYP_KURVE_L5", "Sommer-Wochentag"), Text("GTYP_KURVE_L6", "Sommer-Wochenende"),
                Text("GTYP_KURVE_L7", "Übergang2-Wochentag"), Text("GTYP_KURVE_L8", "Übergang2-Wochenende")
            };

            string[] quelle = (kurven <= kurz.Length) ? kurz : lang;
            var namen = new List<string>();
            for (int i = 0; i < kurven; i++) namen.Add(i < quelle.Length ? quelle[i] : "");
            return namen;
        }

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
