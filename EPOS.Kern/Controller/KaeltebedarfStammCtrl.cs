using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;

namespace WindowsFormsApplication1
{
    // Controller fuer die Kaeltebedarf-STAMMDATEN (Tab_Kaeltebedarf_STAMM) samt Typkatalog (Tab_Kaeltetyp_STAMM),
    // Welle K1 nach dem Muster ProzesswaermeStammCtrl: Admin-Leseoperationen, Katalog-Schreiben (Kopf + Typ) und die
    // Kopierlogik STAMM -> Projekt (Kopf + Typprofil, Master-Detail). Anders als die Prozesswaerme traegt die Kopie
    // ihren Katalogverweis ID_Stamm (ON DELETE SET NULL) von Anfang an, und das Temperaturpaar ist eine Angabe ohne
    // Wirkung (E-K3, KaeltebedarfSchema.Paarpruefung: -40 ... 100 Grad C, Vorlauf <= Ruecklauf).
    // Das Modell ist ProzesswaermeModel - Kopf und Typ sind gleich geschnitten (Namensfeld m_szProzessname).
    // Jeder Weg ist spaltentolerant: vor dem Schritt KaeltebedarfSchema liest er leer und schreibt nichts.
    class KaeltebedarfStammCtrl : ProzesswaermeModel
    {
        public const string TABLE      = KaeltebedarfSchema.TAB_KOPF_STAMM;
        public const string TYP_STAMM  = KaeltebedarfSchema.TAB_TYP_STAMM;
        public const string TABLE_PROJ = KaeltebedarfSchema.TAB_KOPF;
        public const string TYP_PROJ   = KaeltebedarfSchema.TAB_TYP;

        private readonly List<ProzesswaermeModel> _internalList = new List<ProzesswaermeModel>();
        public int rows => _internalList.Count;
        public new List<ProzesswaermeModel> items => _internalList;

        /// <summary>Steht der Katalog (Schritt <see cref="KaeltebedarfSchema"/>)?</summary>
        private static bool Da => DataRepository.TabelleVorhanden(TABLE);

        #region --- READ (Katalog) ---

        private static void MapRow(DataRow row, ProzesswaermeModel item)
        {
            DataColumnCollection c = row.Table.Columns;
            if (c.Contains("ID") && row["ID"] != DBNull.Value) item.m_ID = Convert.ToInt32(row["ID"], CultureInfo.InvariantCulture);
            if (c.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.m_szProzessname = row["Bezeichner"].ToString();
            if (c.Contains("Typ") && row["Typ"] != DBNull.Value) item.m_szTyp = row["Typ"].ToString();
            if (c.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) item.m_szBeschreibung = row["Beschreibung"].ToString();
            if (c.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value) item.m_bReadOnly = Convert.ToBoolean(row["ReadOnly"], CultureInfo.InvariantCulture);
            for (int i = 0; i < 12; i++)
            {
                string col = "Monat_" + (i + 1).ToString(CultureInfo.InvariantCulture);
                if (c.Contains(col) && row[col] != DBNull.Value) item.m_Monat[i] = Convert.ToDouble(row[col], CultureInfo.InvariantCulture);
            }
            item.m_Vorlauf = Zahl(row, KaeltebedarfSchema.SPALTE_VORLAUF);
            item.m_Ruecklauf = Zahl(row, KaeltebedarfSchema.SPALTE_RUECKLAUF);
        }

        private static double? Zahl(DataRow row, string spalte)
        {
            if (row == null || !row.Table.Columns.Contains(spalte) || row[spalte] == DBNull.Value) return null;
            return Convert.ToDouble(row[spalte], CultureInfo.InvariantCulture);
        }

        /// <summary>Das Temperaturpaar (Angabe) eines KATALOGsatzes; <c>(null, null)</c> ohne Satz, Paar oder Schritt.</summary>
        public static (double? Vorlauf, double? Ruecklauf) Temperaturpaar(string szBezeichner)
        {
            if (!Da) return (null, null);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Vorlauf, Ruecklauf FROM " + TABLE + " WHERE Bezeichner = ?", new DbParam("@bez", szBezeichner ?? ""));
            if (dt == null || dt.Rows.Count == 0) return (null, null);
            return (Zahl(dt.Rows[0], KaeltebedarfSchema.SPALTE_VORLAUF), Zahl(dt.Rows[0], KaeltebedarfSchema.SPALTE_RUECKLAUF));
        }

        /// <summary>Das Temperaturpaar (Angabe) einer PROJEKTKOPIE (<c>Tab_Kaeltebedarf.ID</c>).</summary>
        public static (double? Vorlauf, double? Ruecklauf) ProjektTemperaturpaar(int idKopie)
        {
            if (idKopie <= 0 || !DataRepository.TabelleVorhanden(TABLE_PROJ)) return (null, null);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Vorlauf, Ruecklauf FROM " + TABLE_PROJ + " WHERE ID = ?", new DbParam("@id", idKopie));
            if (dt == null || dt.Rows.Count == 0) return (null, null);
            return (Zahl(dt.Rows[0], KaeltebedarfSchema.SPALTE_VORLAUF), Zahl(dt.Rows[0], KaeltebedarfSchema.SPALTE_RUECKLAUF));
        }

        /// <summary>Schreibt das Temperaturpaar einer PROJEKTKOPIE; ein unzulässiges Paar schreibt nichts.</summary>
        public static bool ProjektTemperaturSetzen(int idKopie, double? vorlauf, double? ruecklauf)
        {
            if (idKopie <= 0 || !DataRepository.TabelleVorhanden(TABLE_PROJ)) return false;
            if (KaeltebedarfSchema.Paarpruefung(vorlauf, ruecklauf) != null) return false;
            return DataRepository.ExecuteSQL(
                "UPDATE " + TABLE_PROJ + " SET Vorlauf = ?, Ruecklauf = ? WHERE ID = ?",
                new DbParam("@v", (object)vorlauf ?? DBNull.Value),
                new DbParam("@r", (object)ruecklauf ?? DBNull.Value),
                new DbParam("@id", idKopie));
        }

        public void ReadAll()
        {
            _internalList.Clear();
            if (!Da) return;
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + TABLE + " ORDER BY Bezeichner", null);
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                var item = new ProzesswaermeModel();
                MapRow(row, item);
                _internalList.Add(item);
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            _internalList.Clear();
            m_ID = 0; m_szProzessname = ""; m_szTyp = ""; m_szBeschreibung = ""; m_bReadOnly = false;
            m_Vorlauf = null; m_Ruecklauf = null;
            for (int i = 0; i < 12; i++) m_Monat[i] = 0.0;
            if (!Da) return;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + TABLE + " WHERE Bezeichner = ?", new DbParam("@bez", szBezeichner ?? (object)DBNull.Value));
            if (dt != null && dt.Rows.Count > 0)
            {
                MapRow(dt.Rows[0], this);
                _internalList.Add(this);
            }
        }

        public bool IsReadOnly(string szBezeichner)
        {
            if (!Da) return false;
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + TABLE + " WHERE Bezeichner = ?", new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Löscht einen Kopfsatz (per Bezeichner), sofern nicht schreibgeschützt. Die Projektkopien behalten ihren Stand,
        /// ihr Verweis <c>ID_Stamm</c> fällt auf NULL (ON DELETE SET NULL) — eine Zuordnung rechnet auf ihrer Kopie weiter.
        /// </summary>
        public bool Delete(string szBezeichner)
        {
            if (!Da) return false;
            if (IsReadOnly(szBezeichner))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt");
                return false;
            }
            return DataRepository.ExecuteSQL("DELETE FROM " + TABLE + " WHERE Bezeichner = ?", new DbParam("@bez", szBezeichner ?? ""));
        }

        public bool Exists(string szBezeichner)
        {
            if (!Da) return false;
            object v = DataRepository.ExecuteScalar("SELECT ID FROM " + TABLE + " WHERE Bezeichner = ?", new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value;
        }

        #endregion

        #region --- STAMM -> PROJEKT KOPIE (Master-Detail) ---

        /// <summary>Projektkopie (<c>Tab_Kaeltebedarf.ID</c>) zu einem Bezeichner im Projekt, oder 0.</summary>
        public static int GetProjektId(string szBezeichner, int idProjekt)
        {
            if (!DataRepository.TabelleVorhanden(TABLE_PROJ)) return 0;
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + TABLE_PROJ + " WHERE Bezeichner = ? AND ID_Projekt = ? ORDER BY ID",
                new DbParam("@bez", szBezeichner ?? ""), new DbParam("@proj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : 0;
        }

        /// <summary>Die Projektkopie <paramref name="idKopie"/>, wenn sie zu DIESEM Projekt gehört und den Namen trägt; sonst 0.</summary>
        public static int GetProjektIdUeberId(int idKopie, string szBezeichner, int idProjekt)
        {
            if (idKopie <= 0 || idProjekt <= 0 || string.IsNullOrEmpty(szBezeichner) ||
                !DataRepository.TabelleVorhanden(TABLE_PROJ)) return 0;
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + TABLE_PROJ + " WHERE ID = ? AND ID_Projekt = ? AND Bezeichner = ?",
                new DbParam("@id", idKopie), new DbParam("@proj", idProjekt), new DbParam("@bez", szBezeichner));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v, CultureInfo.InvariantCulture) : 0;
        }

        /// <summary>
        /// Kopiert einen Katalogsatz samt Typprofil ins Projekt, falls noch nicht vorhanden; die Kopie trägt
        /// <c>ID_Stamm</c> = Katalog-ID und <c>ReadOnly = 0</c>. Rückgabe: ID der Projektkopie, −1 bei Fehler.
        /// </summary>
        public static int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            if (string.IsNullOrEmpty(szBezeichner) || idProjekt <= 0 || !Da) return -1;

            int vorhanden = GetProjektId(szBezeichner, idProjekt);
            if (vorhanden > 0) return vorhanden;

            DataTable head = DataRepository.GetDataTable("SELECT * FROM " + TABLE + " WHERE Bezeichner = ?", new DbParam("@bez", szBezeichner));
            if (head == null || head.Rows.Count == 0) return -1;
            DataRow h = head.Rows[0];
            string typName = h["Typ"] != DBNull.Value ? h["Typ"].ToString() : "";
            DataTable dtTyp = string.IsNullOrEmpty(typName) ? null
                : DataRepository.GetDataTable("SELECT * FROM " + TYP_STAMM + " WHERE Bezeichner = ?", new DbParam("@bez", typName));

            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    object m = v.Skalar("SELECT Max(ID) FROM " + TABLE_PROJ);
                    int neuId = ((m != null && m != DBNull.Value) ? Convert.ToInt32(m, CultureInfo.InvariantCulture) : 0) + 1;

                    var cols = new StringBuilder("ID, ID_Projekt, Bezeichner, Typ, Beschreibung");
                    var vals = new StringBuilder("?, ?, ?, ?, ?");
                    var p = new List<DbParam>
                    {
                        new DbParam("@hid", neuId), new DbParam("@hproj", idProjekt), new DbParam("@hbez", szBezeichner),
                        new DbParam("@htyp", (object)typName ?? DBNull.Value), new DbParam("@hbeschr", ColOrNull(h, "Beschreibung"))
                    };
                    for (int i = 1; i <= 12; i++)
                    {
                        cols.Append(", Monat_" + i.ToString(CultureInfo.InvariantCulture)); vals.Append(", ?");
                        p.Add(new DbParam("@hmon" + i.ToString("D2", CultureInfo.InvariantCulture), ColOrNull(h, "Monat_" + i.ToString(CultureInfo.InvariantCulture))));
                    }
                    cols.Append(", ReadOnly, Vorlauf, Ruecklauf, ID_Stamm"); vals.Append(", 0, ?, ?, ?");
                    p.Add(new DbParam("@hvl", ColOrNull(h, KaeltebedarfSchema.SPALTE_VORLAUF)));
                    p.Add(new DbParam("@hrl", ColOrNull(h, KaeltebedarfSchema.SPALTE_RUECKLAUF)));
                    p.Add(new DbParam("@hstamm", Convert.ToInt64(h["ID"], CultureInfo.InvariantCulture)));
                    v.Ausfuehren("INSERT INTO " + TABLE_PROJ + " (" + cols + ") VALUES (" + vals + ")", p.ToArray());

                    if (dtTyp != null && dtTyp.Rows.Count > 0)
                    {
                        object mt = v.Skalar("SELECT Max(ID) FROM " + TYP_PROJ);
                        int neuTypId = ((mt != null && mt != DBNull.Value) ? Convert.ToInt32(mt, CultureInfo.InvariantCulture) : 0) + 1;
                        var profil = new List<string>();
                        foreach (DataColumn dc in dtTyp.Columns)
                            if (int.TryParse(dc.ColumnName, out _)) profil.Add(dc.ColumnName);

                        foreach (DataRow tr in dtTyp.Rows)
                        {
                            var tc = new StringBuilder("ID, ID_Kaeltebedarf, ID_Projekt, Typname, Beschreibung, ReadOnly");
                            var tv = new StringBuilder("?, ?, ?, ?, ?, 0");
                            var tp = new List<DbParam>
                            {
                                new DbParam("@tid", neuTypId++), new DbParam("@tkb", neuId), new DbParam("@tproj", idProjekt),
                                new DbParam("@ttypn", typName), new DbParam("@tbeschr", ColOrNull(tr, "Beschreibung"))
                            };
                            int k = 0;
                            foreach (string col in profil)
                            {
                                tc.Append(", \"" + col + "\""); tv.Append(", ?");
                                tp.Add(new DbParam("@cp" + (k++).ToString("D3", CultureInfo.InvariantCulture), ColOrNull(tr, col)));
                            }
                            v.Ausfuehren("INSERT INTO " + TYP_PROJ + " (" + tc + ") VALUES (" + tv + ")", tp.ToArray());
                        }
                    }

                    v.Commit();
                    return neuId;
                }
                catch (Exception ex)
                {
                    try { v.Rollback(); } catch { }
                    Console.WriteLine("Fehler beim Kopieren des Kaeltebedarfs aus den Stammdaten: " + ex.Message);
                    return -1;
                }
            }
        }

        private static object ColOrNull(DataRow row, string col)
            => (row.Table.Columns.Contains(col) && row[col] != DBNull.Value) ? row[col] : (object)DBNull.Value;

        #endregion

        #region --- KATALOG-SCHREIBEN (Kopf + Typ) ---

        /// <summary>Schreibt einen Katalog-Kopf ohne Temperaturpaar (Paar bleibt bzw. ist leer).</summary>
        public bool SaveHead(string bez, string typ, string beschr, double[] monat, bool isNew)
            => SaveHead(bez, typ, beschr, monat, isNew, null, null, false);

        /// <summary>
        /// Schreibt einen Katalog-Kopf: <paramref name="isNew"/> INSERT (ID von der Datenbank, <c>ReadOnly = 0</c>), sonst
        /// UPDATE per Bezeichner mit ReadOnly-Schutz; mit <paramref name="mitTemperatur"/> dazu das Paar (Angabe). Ein
        /// unzulässiges Paar (<see cref="KaeltebedarfSchema.Paarpruefung"/>) schreibt nichts.
        /// </summary>
        public bool SaveHead(string bez, string typ, string beschr, double[] monat, bool isNew,
                             double? vorlauf, double? ruecklauf, bool mitTemperatur)
        {
            if (monat == null || monat.Length < 12 || !Da) return false;
            if (mitTemperatur && KaeltebedarfSchema.Paarpruefung(vorlauf, ruecklauf) != null) return false;

            if (isNew)
            {
                var cols = new StringBuilder("Bezeichner, Typ, Beschreibung");
                var vals = new StringBuilder("?, ?, ?");
                var ps = new List<DbParam>
                {
                    new DbParam("@hbez", bez ?? ""), new DbParam("@htyp", typ ?? ""), new DbParam("@hbeschr", beschr ?? "")
                };
                for (int i = 0; i < 12; i++)
                {
                    cols.Append(", Monat_" + (i + 1).ToString(CultureInfo.InvariantCulture)); vals.Append(", ?");
                    ps.Add(new DbParam("@mon" + (i + 1).ToString("D2", CultureInfo.InvariantCulture), monat[i]));
                }
                cols.Append(", ReadOnly"); vals.Append(", 0");
                if (mitTemperatur)
                {
                    cols.Append(", Vorlauf, Ruecklauf"); vals.Append(", ?, ?");
                    ps.Add(new DbParam("@hvl", (object)vorlauf ?? DBNull.Value));
                    ps.Add(new DbParam("@hrl", (object)ruecklauf ?? DBNull.Value));
                }
                return DataRepository.ExecuteSQL("INSERT INTO " + TABLE + " (" + cols + ") VALUES (" + vals + ")", ps.ToArray());
            }

            if (IsReadOnly(bez))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschuetzt (ReadOnly) und kann nicht ueberschrieben werden.",
                    "Schreibgeschuetzt");
                return false;
            }

            var set = new StringBuilder("Typ = ?, Beschreibung = ?");
            var pu = new List<DbParam> { new DbParam("@utyp", typ ?? ""), new DbParam("@ubeschr", beschr ?? "") };
            for (int i = 0; i < 12; i++)
            {
                set.Append(", Monat_" + (i + 1).ToString(CultureInfo.InvariantCulture) + " = ?");
                pu.Add(new DbParam("@umon" + (i + 1).ToString("D2", CultureInfo.InvariantCulture), monat[i]));
            }
            if (mitTemperatur)
            {
                set.Append(", Vorlauf = ?, Ruecklauf = ?");
                pu.Add(new DbParam("@uvl", (object)vorlauf ?? DBNull.Value));
                pu.Add(new DbParam("@url", (object)ruecklauf ?? DBNull.Value));
            }
            pu.Add(new DbParam("@ukey", bez ?? ""));
            return DataRepository.ExecuteSQL("UPDATE " + TABLE + " SET " + set + " WHERE Bezeichner = ?", pu.ToArray());
        }

        /// <summary>Ist der Typ Auslieferungsbestand (<c>ReadOnly</c>)?</summary>
        public static bool TypIsReadOnly(string bez)
        {
            if (!DataRepository.TabelleVorhanden(TYP_STAMM)) return false;
            object v = DataRepository.ExecuteScalar("SELECT ReadOnly FROM " + TYP_STAMM + " WHERE Bezeichner = ?", new DbParam("@bez", bez ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v, CultureInfo.InvariantCulture);
        }

        /// <summary>Legt einen neuen Typ an (nur Kopf, Profil leer); Rückgabe: neue ID oder 0.</summary>
        public static int TypNew(string bez)
        {
            if (!DataRepository.TabelleVorhanden(TYP_STAMM)) return 0;
            bool ok = DataRepository.ExecuteSQL("INSERT INTO " + TYP_STAMM + " (Bezeichner, ReadOnly) VALUES (?, 0)", new DbParam("@tbez", bez ?? ""));
            if (!ok) return 0;
            object v = DataRepository.ExecuteScalar("SELECT ID FROM " + TYP_STAMM + " WHERE Bezeichner = ?", new DbParam("@bez", bez ?? ""));
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v, CultureInfo.InvariantCulture);
        }

        /// <summary>Löscht einen Typ mit ReadOnly-Schutz.</summary>
        public static bool TypDelete(string bez)
        {
            if (!DataRepository.TabelleVorhanden(TYP_STAMM)) return false;
            if (TypIsReadOnly(bez))
            {
                Meldung.Hinweis("Dieser Typ ist schreibgeschuetzt (ReadOnly) und kann nicht geloescht werden.", "Schreibgeschuetzt");
                return false;
            }
            return DataRepository.ExecuteSQL("DELETE FROM " + TYP_STAMM + " WHERE Bezeichner = ?", new DbParam("@bez", bez ?? ""));
        }

        #endregion
    }
}
