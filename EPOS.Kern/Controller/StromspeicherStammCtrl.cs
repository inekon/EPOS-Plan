using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // Controller fuer die Stammdaten-Tabelle Tab_Stromspeicher_STAMM (globaler Katalog).
    // Analog zu HeizkesselStammCtrl / BHKWStammCtrl:
    //   - Tabelle = Tab_Stromspeicher_STAMM
    //   - liest/schreibt das Feld ReadOnly
    //   - Insert() vergibt eine explizite ID (MAX+1) und setzt ReadOnly = false
    //   - Update()/Delete() verweigern schreibgeschuetzte Datensaetze
    // Alle DB-Zugriffe laufen ueber DataRepository.
    public class StromspeicherStammCtrl : StromspeicherModel
    {
        public const string TABLE = "Tab_Stromspeicher_STAMM";

        private List<StromspeicherModel> _internalList = new List<StromspeicherModel>();
        public int rows => _internalList.Count;
        public List<StromspeicherModel> items => _internalList;

        // Zuletzt gelesener ReadOnly-Zustand
        public bool m_bReadOnly = false;

        public void ReadAll()
        {
            string sql = "SELECT * FROM [" + TABLE + "] ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            _internalList.Clear();
            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            string sql = "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@bez", szBezeichner ?? (object)DBNull.Value));

            Reset();
            _internalList.Clear();

            if (dt != null && dt.Rows.Count > 0)
            {
                FillFromRow(this, dt.Rows[0]);
                this.m_bReadOnly = ReadOnlyOf(dt.Rows[0]);
                _internalList.Add(this);
            }
        }

        public bool Exists(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToInt32(v) > 0;
        }

        public static bool IsReadOnlyStatic(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // Legt einen neuen Stammdatensatz an (explizite ID, ReadOnly = false).
        public bool Insert()
        {
            StromspeicherCtrl.StelleGeraetespaltenSicher();   // AP3-Spalten, bevor sie im INSERT stehen

            int neueId = DataRepository.GetMaxID(TABLE) + 1;

            string sql = @"INSERT INTO [" + TABLE + @"]
                            (ID, Bezeichner, Typ, Leistung, Energie, Degradation, Ladezustand, Modulkosten, ReadOnly,
                             Wirkungsgrad_RT, Zyklen_Zugesichert, Verschleisskosten, Leistungskosten, Investition_Fix, Standby_Verbrauch)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            DbParam[] ps = {
                new DbParam("@id", neueId),
                new DbParam("@bez", this.m_szBezeichner ?? ""),
                new DbParam("@typ", (object)(this.m_szTyp ?? "") ),
                new DbParam("@lei", this.m_Leistung),
                new DbParam("@ene", this.m_Energie),
                new DbParam("@deg", this.m_Degradation),
                new DbParam("@lad", this.m_Ladezustand),
                new DbParam("@mod", this.m_Modulkosten),
                new DbParam("@ro", false),
                new DbParam("@eta", this.m_WirkungsgradRT),
                new DbParam("@nzyk", this.m_ZyklenZugesichert),
                new DbParam("@cver", this.m_Verschleisskosten),
                new DbParam("@cpow", this.m_Leistungskosten),
                new DbParam("@ifix", this.m_InvestitionFix),
                new DbParam("@stby", this.m_StandbyVerbrauch)
            };

            bool ok = DataRepository.ExecuteSQL(sql, ps);
            if (ok) this.m_ID = neueId;
            return ok;
        }

        // Aktualisiert den Datensatz. szKey ist der urspruengliche Bezeichner (WHERE-Schluessel),
        // this.m_szBezeichner der (evtl. geaenderte) neue Bezeichner.
        public bool Update(string szKey)
        {
            if (IsReadOnlyStatic(szKey))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt");
                return false;
            }

            StromspeicherCtrl.StelleGeraetespaltenSicher();   // AP3-Spalten, bevor sie im UPDATE stehen

            string sql = @"UPDATE [" + TABLE + @"] SET
                            Bezeichner = ?, Typ = ?, Leistung = ?, Energie = ?,
                            Degradation = ?, Ladezustand = ?, Modulkosten = ?,
                            Wirkungsgrad_RT = ?, Zyklen_Zugesichert = ?, Verschleisskosten = ?,
                            Leistungskosten = ?, Investition_Fix = ?, Standby_Verbrauch = ?
                          WHERE Bezeichner = ?";

            DbParam[] ps = {
                new DbParam("@bez", this.m_szBezeichner ?? ""),
                new DbParam("@typ", (object)(this.m_szTyp ?? "") ),
                new DbParam("@lei", this.m_Leistung),
                new DbParam("@ene", this.m_Energie),
                new DbParam("@deg", this.m_Degradation),
                new DbParam("@lad", this.m_Ladezustand),
                new DbParam("@mod", this.m_Modulkosten),
                new DbParam("@eta", this.m_WirkungsgradRT),
                new DbParam("@nzyk", this.m_ZyklenZugesichert),
                new DbParam("@cver", this.m_Verschleisskosten),
                new DbParam("@cpow", this.m_Leistungskosten),
                new DbParam("@ifix", this.m_InvestitionFix),
                new DbParam("@stby", this.m_StandbyVerbrauch),
                new DbParam("@key", szKey ?? "")
            };

            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Delete(string szBezeichner)
        {
            if (IsReadOnlyStatic(szBezeichner))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt");
                return false;
            }

            string sql = "DELETE FROM [" + TABLE + "] WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new DbParam("@bez", szBezeichner ?? ""));
        }

        // --- MAPPING ---

        private void Reset()
        {
            m_ID = 0; m_szBezeichner = string.Empty; m_szTyp = string.Empty;
            m_Leistung = 0; m_Energie = 0; m_Degradation = 0; m_Ladezustand = 0; m_Modulkosten = 0;
            m_WirkungsgradRT = 0; m_ZyklenZugesichert = 0; m_Verschleisskosten = 0;
            m_Leistungskosten = 0; m_InvestitionFix = 0; m_StandbyVerbrauch = 0;
            m_bReadOnly = false;
        }

        private static bool ReadOnlyOf(DataRow row)
        {
            return row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
        }

        private static void FillFromRow(StromspeicherModel t, DataRow row)
        {
            if (row["ID"] != DBNull.Value) t.m_ID = Convert.ToInt32(row["ID"]);
            if (row["Bezeichner"] != DBNull.Value) t.m_szBezeichner = row["Bezeichner"].ToString();
            if (row.Table.Columns.Contains("Typ") && row["Typ"] != DBNull.Value) t.m_szTyp = row["Typ"].ToString();
            if (row.Table.Columns.Contains("Leistung") && row["Leistung"] != DBNull.Value) t.m_Leistung = Convert.ToDouble(row["Leistung"]);
            if (row.Table.Columns.Contains("Energie") && row["Energie"] != DBNull.Value) t.m_Energie = Convert.ToDouble(row["Energie"]);
            if (row.Table.Columns.Contains("Degradation") && row["Degradation"] != DBNull.Value) t.m_Degradation = Convert.ToDouble(row["Degradation"]);
            if (row.Table.Columns.Contains("Ladezustand") && row["Ladezustand"] != DBNull.Value) t.m_Ladezustand = Convert.ToDouble(row["Ladezustand"]);
            if (row.Table.Columns.Contains("Modulkosten") && row["Modulkosten"] != DBNull.Value) t.m_Modulkosten = Convert.ToDouble(row["Modulkosten"]);

            // AP3-Geraetetechnik (Fachkonzept 5.1) - dieselbe Columns.Contains-Wache wie
            // darueber: auf einer Datenbank vor Migrationsschritt 11 fehlen die Spalten,
            // die Felder behalten dann ihre 0.
            if (row.Table.Columns.Contains("Wirkungsgrad_RT") && row["Wirkungsgrad_RT"] != DBNull.Value) t.m_WirkungsgradRT = Convert.ToDouble(row["Wirkungsgrad_RT"]);
            if (row.Table.Columns.Contains("Zyklen_Zugesichert") && row["Zyklen_Zugesichert"] != DBNull.Value) t.m_ZyklenZugesichert = Convert.ToInt32(row["Zyklen_Zugesichert"]);
            if (row.Table.Columns.Contains("Verschleisskosten") && row["Verschleisskosten"] != DBNull.Value) t.m_Verschleisskosten = Convert.ToDouble(row["Verschleisskosten"]);
            if (row.Table.Columns.Contains("Leistungskosten") && row["Leistungskosten"] != DBNull.Value) t.m_Leistungskosten = Convert.ToDouble(row["Leistungskosten"]);
            if (row.Table.Columns.Contains("Investition_Fix") && row["Investition_Fix"] != DBNull.Value) t.m_InvestitionFix = Convert.ToDouble(row["Investition_Fix"]);
            if (row.Table.Columns.Contains("Standby_Verbrauch") && row["Standby_Verbrauch"] != DBNull.Value) t.m_StandbyVerbrauch = Convert.ToDouble(row["Standby_Verbrauch"]);
        }

        private StromspeicherModel MapRowToModel(DataRow row)
        {
            StromspeicherModel m = new StromspeicherModel();
            FillFromRow(m, row);
            return m;
        }

        /// <summary>
        /// Der RUECKFALL der Dashboard-Kachel, wenn das Projekt keinen Stromspeicher
        /// fuehrt: 5 kWh (iU9-W11a.2).
        ///
        /// <para>Woertlich uebernommen aus <c>TabNavigationManager</c> Z. 154
        /// (<c>if (speicherKWh == 0) dashForm.speicherKWh = 5;</c>). Der Wert ist eine
        /// ANZEIGEvorgabe fuer das Was-waere-wenn-Feld der Autarkiekachel und geht
        /// nirgends in die Datenbank (Befund W11-B32) — er darf deshalb hier stehen und
        /// nicht in <c>DbWerte</c>.</para>
        /// </summary>
        public const double KAPAZITAET_RUECKFALL_KWH = 5.0;

        /// <summary>
        /// Kapazitaet [kWh] und Lade-/Entladeleistung [kW] der Einheit, die auch
        /// GERECHNET wird (iU9-W11a.2; woertlich aus
        /// <c>Form_Simulation_Detail.SpGeraetedaten</c>, Z. 6446-6510).
        ///
        /// <para><b>Die „zwei Fassungen" der Vermessung sind Abfrage und Rueckfall</b>,
        /// nicht zwei Meinungen ueber dieselbe Frage — deshalb wandern BEIDE hierher und
        /// keine wird gestrichen. Die erste engt auf die Anlagenzeile der AKTIVEN
        /// Variante ein, die zweite summiert ueber alle Speicheranlagen des Projekts.
        /// Genau diese Reihenfolge nimmt auch
        /// <c>StromspeicherSimCtrl.LeseParameter(int)</c> (Fachkonzept 7.3).</para>
        ///
        /// <para><b>Warum die Einengung die richtige ist.</b> Seit AP9b rechnet die
        /// Simulation die Anlagenzeile der aktiven Variante, nicht deren Summe. Ohne die
        /// Einengung zeigte die Parameterseite bei mehreren Varianten eine Leistung, mit
        /// der nie jemand gerechnet hat (Abnahmebefund 1: Projekt 1011 der Produktiv-DB,
        /// vier Speicherzeilen, angezeigt wurden 43,9 kW statt der 11,04 kW der aktiven
        /// Variante). Der Rueckfall bleibt genau dort, wo ihn auch der Controller nimmt:
        /// wenn sich keine aktive Variantenzeile bestimmen laesst (Altprojekt vor
        /// Migrationsschritt 11d, oder eine Variante, die auf keine Speicheranlage dieses
        /// Projekts mehr zeigt).</para>
        ///
        /// <param name="idAnlageAktiveVariante">Die Anlagenzeile der aktiven Variante;
        /// 0 oder kleiner heisst „keine" und fuehrt sofort zur Aggregation.</param>
        /// </summary>
        public static (double Kwh, double Kw) KapazitaetUndLeistung(
            int idProjekt, int idAnlageAktiveVariante = 0)
        {
            if (idProjekt <= 0) return (0.0, 0.0);

            try
            {
                string sql =
                    "SELECT SUM(sp.Energie) AS C, SUM(sp.Leistung) AS P " +
                    "FROM Tab_Energieanlagen AS a " +
                    "INNER JOIN Tab_Stromspeicher AS sp ON a.ID_SP = sp.ID " +
                    "WHERE a.ID_Projekt = ? AND a.ID_Type = ?";

                List<DbParam> parameter = new List<DbParam>
                {
                    new DbParam("@proj", idProjekt),
                    new DbParam("@typ", WizardItemClass.SP_TYP)
                };

                // Die Anlage der aktiven Variante, sofern sie eine Speicheranlage dieses
                // Projekts ist - die WHERE-Bedingung oben prueft das gleich mit.
                if (idAnlageAktiveVariante > 0)
                {
                    sql += " AND a.ID = ?";
                    parameter.Add(new DbParam("@anlage", idAnlageAktiveVariante));
                }

                (double Kwh, double Kw)? treffer = Summenzeile(
                    DataRepository.GetDataTable(sql, parameter.ToArray()));
                if (treffer.HasValue) return treffer.Value;

                // Rueckfall: Die aktive Variante zeigt ins Leere - dann gilt wieder die
                // Aggregation ueber alle Speicheranlagen (Verhalten bis AP9a).
                if (parameter.Count > 2)
                {
                    treffer = Summenzeile(DataRepository.GetDataTable(
                        "SELECT SUM(sp.Energie) AS C, SUM(sp.Leistung) AS P " +
                        "FROM Tab_Energieanlagen AS a " +
                        "INNER JOIN Tab_Stromspeicher AS sp ON a.ID_SP = sp.ID " +
                        "WHERE a.ID_Projekt = ? AND a.ID_Type = ?",
                        parameter[0], parameter[1]));
                    if (treffer.HasValue) return treffer.Value;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Geraetedaten des Speichers konnten nicht gelesen werden: " + ex.Message);
            }

            return (0.0, 0.0);
        }

        /// <summary>Die Summenzeile einer der beiden Abfragen; <c>null</c>, wenn nichts steht.</summary>
        private static (double Kwh, double Kw)? Summenzeile(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0 || dt.Rows[0]["C"] == DBNull.Value) return null;

            double kwh = Convert.ToDouble(dt.Rows[0]["C"]);
            double kw = dt.Rows[0]["P"] != DBNull.Value ? Convert.ToDouble(dt.Rows[0]["P"]) : 0.0;
            return (kwh, kw);
        }

        /// <summary>
        /// Die SUMME der Speicherkapazitaeten eines Projekts [kWh], mit dem
        /// 5-kWh-Rueckfall der Autarkiekachel (iU9-W11a.2, Befund W11-B45).
        ///
        /// <para><b>Woher sie kommt.</b> Bis hierher stand sie in der
        /// NAVIGATIONSklasse <c>TabNavigationManager</c> (Z. 142-154) — mit einem
        /// <c>RecordSet</c> und string-konkateniertem SQL
        /// (<c>"select * from Tab_Stromspeicher where ID=" + id</c>), also mit dem
        /// Altbestandsweg, den <c>WindowsFormsApplication1/CLAUDE.md</c> ausdruecklich
        /// nicht mehr vorsieht.</para>
        ///
        /// <para><b>Der Weg bleibt derselbe:</b> ueber die Anlagenzeilen des Projekts vom
        /// Typ SP und deren <c>ID_SP</c> in <c>Tab_Stromspeicher</c>. Er nimmt bewusst
        /// ALLE Speicheranlagen (nicht die aktive Variante wie
        /// <see cref="KapazitaetUndLeistung"/>): Die Kachel fragt „was koennte ein
        /// Speicher bringen", nicht „was hat der gerechnete gebracht" (Befund W11-B32,
        /// reine Was-waere-wenn-Groesse ohne Rueckschreiben).</para>
        /// </summary>
        public static double KapazitaetJeProjekt(int idProjekt)
        {
            double summe = 0.0;
            if (idProjekt <= 0) return KAPAZITAET_RUECKFALL_KWH;

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT SUM(sp.Energie) AS C " +
                    "FROM Tab_Energieanlagen AS a " +
                    "INNER JOIN Tab_Stromspeicher AS sp ON a.ID_SP = sp.ID " +
                    "WHERE a.ID_Projekt = ? AND a.ID_Type = ?",
                    new DbParam("@proj", idProjekt),
                    new DbParam("@typ", WizardItemClass.SP_TYP));

                if (dt != null && dt.Rows.Count > 0 && dt.Rows[0]["C"] != DBNull.Value)
                    summe = Convert.ToDouble(dt.Rows[0]["C"]);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Die Speicherkapazitaet des Projekts konnte nicht gelesen werden: " + ex.Message);
            }

            return summe == 0.0 ? KAPAZITAET_RUECKFALL_KWH : summe;
        }

        // =================================================================================
        // W14a.0c - Katalogliste und Detailblock des Modulkatalogs
        // =================================================================================

        /// <summary>Eine Zeile der Katalogliste: Primaerschluessel und Bezeichner.</summary>
        public sealed record KatalogZeile(int Id, string Bezeichner);

        /// <summary>
        /// Die Bezeichner des Speicherkatalogs, nach Namen sortiert — die Liste des
        /// Modulkatalogs (<c>Form_AdminStromspeicher.Form_Stromspeicher_Load</c> Z. 71).
        /// </summary>
        /// <remarks>
        /// Der Vorlaeufer las <c>SELECT Bezeichner FROM Tab_Stromspeicher_STAMM</c> OHNE
        /// Sortierung; die Liste kam damit in Einfuegereihenfolge. <c>ORDER BY</c> steht
        /// jetzt da — dieselbe Angleichung wie in <c>PhotovoltaikStammCtrl.Filtern</c>
        /// (W6.0c).
        /// </remarks>
        public static IReadOnlyList<KatalogZeile> KatalogZeilen()
        {
            var liste = new List<KatalogZeile>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner FROM [" + TABLE + "] ORDER BY Bezeichner");
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                if (row["ID"] == null || row["ID"] == DBNull.Value) continue;
                liste.Add(new KatalogZeile(Convert.ToInt32(row["ID"]),
                                           row["Bezeichner"] == DBNull.Value ? "" : row["Bezeichner"].ToString()));
            }
            return liste;
        }

        // =================================================================================
        // W14a.0e - der EINE Schreibeinstieg des Modulkatalogs
        // =================================================================================

        /// <summary>
        /// Was ein Speicherversuch des Modulkatalogs ergeben hat — dieselbe Form wie
        /// <c>HeizkesselStammCtrl.SpeicherErgebnis</c> (W6.0).
        /// </summary>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, string Name);

        /// <summary>
        /// Schreibt den Speichersatz — der Weg des Knopfes „Speichern"
        /// (<c>Form_AdminStromspeicher.btn_Speichern_Click</c> Z. 97-194).
        /// </summary>
        /// <param name="daten">Die dreizehn Felder der Maske, Bestand und AP3.</param>
        /// <param name="neu"><c>true</c> nach „Neu…" (Bestandsfeld <c>m_Neu</c>).</param>
        /// <param name="schluessel">Der urspruengliche Bezeichner — WHERE-Schluessel des UPDATE.</param>
        /// <remarks>
        /// <para><b>Befund W14-B47 behoben.</b> Der Vorlaeufer kehrte bei einem
        /// fehlgeschlagenen <see cref="Update(string)"/> STILL zurueck (Z. 175) — kein
        /// Wort, kein Hinweis. Jetzt kommt der Grund zurueck.</para>
        /// <para><b>Der <c>Exists</c>-Vorabtest ist NEU</b> (Angleichung an Heizkessel,
        /// Pufferspeicher und Photovoltaik): Der Vorlaeufer legte ohne Vorabtest an und
        /// bekam erst von der Datenbank eine Absage — oder, schlimmer, eine zweite Zeile
        /// mit demselben Bezeichner.</para>
        /// </remarks>
        public static SpeicherErgebnis SpeichernAus(StromspeicherModel daten, bool neu, string schluessel)
        {
            if (daten == null || string.IsNullOrWhiteSpace(daten.m_szBezeichner))
                return new SpeicherErgebnis(false,
                    MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG, "");

            try
            {
                var ctrl = new StromspeicherStammCtrl();
                Uebernehmen(ctrl, daten);

                if (neu)
                {
                    if (ctrl.Exists(daten.m_szBezeichner))
                        return new SpeicherErgebnis(false,
                            MyResource.Resource.PSP_MELDUNG_NAME_EXISTIERT, "");

                    if (!ctrl.Insert())
                        return new SpeicherErgebnis(false,
                            MyResource.Resource.PSP_MELDUNG_SPEICHERN_FEHLER, "");

                    return new SpeicherErgebnis(true,
                        MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT, daten.m_szBezeichner);
                }

                string key = schluessel ?? daten.m_szBezeichner;

                // Der Schutz wird VOR dem Schreiben gefragt, damit der Grund als Text
                // zurueckkommt; Update() zeigt ihn sonst selbst ueber Meldung.Hinweis.
                if (IsReadOnlyStatic(key))
                    return new SpeicherErgebnis(false, Text("MODK_MSG_SCHUTZ",
                        "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden."), "");

                if (!ctrl.Update(key))
                    return new SpeicherErgebnis(false,
                        MyResource.Resource.PSP_MELDUNG_SPEICHERN_FEHLER, "");

                return new SpeicherErgebnis(true,
                    MyResource.Resource.PSP_MELDUNG_DATENSATZ_GESPEICHERT, daten.m_szBezeichner);
            }
            catch (Exception ex)
            {
                return new SpeicherErgebnis(false,
                    string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), "");
            }
        }

        /// <summary>
        /// Loescht einen Speichersatz und sagt, WARUM es nicht ging.
        /// </summary>
        /// <remarks>
        /// <b>Befund W14-B42 behoben.</b> Der Vorlaeufer
        /// (<c>Form_AdminStromspeicher.btn_Loeschen_Click</c> Z. 330-335) deutete JEDE
        /// Ausnahme als „Es besteht eine Projektzuordnung!" — auch eine gesperrte Datei,
        /// einen Tippfehler im SQL oder einen fehlenden Schreibzugriff. Jetzt kommt der
        /// wirkliche Grund zurueck; die Projektzuordnung ist einer davon und wird als
        /// solche benannt.
        /// </remarks>
        public static SpeicherErgebnis Loeschen(string szBezeichner)
        {
            if (string.IsNullOrWhiteSpace(szBezeichner))
                return new SpeicherErgebnis(false,
                    MyResource.Resource.PSP_MELDUNG_BEZEICHNER_UNGUELTIG, "");

            try
            {
                if (IsReadOnlyStatic(szBezeichner))
                    return new SpeicherErgebnis(false, Text("KBROW_MSG_SCHUTZ_LOESCHEN",
                        "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden."), "");

                var ctrl = new StromspeicherStammCtrl();
                if (!ctrl.Delete(szBezeichner))
                    return new SpeicherErgebnis(false, Text("KBROW_MSG_LOESCHEN_FEHLER",
                        "Der Datensatz konnte nicht gelöscht werden."), "");

                return new SpeicherErgebnis(true, "", szBezeichner);
            }
            catch (Exception ex)
            {
                return new SpeicherErgebnis(false,
                    string.Format(MyResource.Resource.PSP_MELDUNG_FEHLER_AUFGETRETEN, ex.Message), "");
            }
        }

        /// <summary>Uebernimmt die dreizehn Felder eines Modells in diesen Controller.</summary>
        private static void Uebernehmen(StromspeicherStammCtrl ziel, StromspeicherModel m)
        {
            ziel.m_szBezeichner = m.m_szBezeichner ?? "";
            ziel.m_szTyp = m.m_szTyp ?? "";
            ziel.m_Leistung = m.m_Leistung;
            ziel.m_Energie = m.m_Energie;
            ziel.m_Degradation = m.m_Degradation;
            ziel.m_Ladezustand = m.m_Ladezustand;
            ziel.m_Modulkosten = m.m_Modulkosten;
            ziel.m_WirkungsgradRT = m.m_WirkungsgradRT;
            ziel.m_ZyklenZugesichert = m.m_ZyklenZugesichert;
            ziel.m_Verschleisskosten = m.m_Verschleisskosten;
            ziel.m_Leistungskosten = m.m_Leistungskosten;
            ziel.m_InvestitionFix = m.m_InvestitionFix;
            ziel.m_StandbyVerbrauch = m.m_StandbyVerbrauch;
        }

        /// <summary>
        /// Die dreizehn Anzeigefelder eines Katalogsatzes, bereits als Text — der
        /// Detailblock von <c>Form_AdminStromspeicher
        /// .listBox_Stromspeicher_SelectedIndexChanged</c> (Z. 196-239) samt der sechs
        /// AP3-Gerätefelder (<c>GeraetefelderAnzeigen</c> Z. 469-477).
        /// </summary>
        /// <remarks>
        /// Die Werte kommen ROH wie im Bestand. Fehlende Spalten und <c>NULL</c> ergeben
        /// einen leeren Text: Auf einer Datenbank vor Migrationsschritt 11 gibt es die
        /// sechs AP3-Spalten nicht, und das Durchklicken des Katalogs darf daran nicht
        /// scheitern (Kommentar Z. 463-468).
        /// </remarks>
        public static IReadOnlyDictionary<string, string> KatalogsatzAnzeige(string szBezeichner)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@bez", szBezeichner ?? ""));
            if (dt == null || dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            var werte = new Dictionary<string, string>(StringComparer.Ordinal);

            werte[ModulKatalogProfil.FeldBezeichner] = Spaltentext(r, "Bezeichner");
            werte[ModulKatalogProfil.FeldTyp] = Spaltentext(r, "Typ");
            werte[ModulKatalogProfil.FeldEnergie] = Spaltentext(r, "Energie");
            werte[ModulKatalogProfil.FeldLeistung] = Spaltentext(r, "Leistung");
            werte[ModulKatalogProfil.FeldDegradation] = Spaltentext(r, "Degradation");
            werte[ModulKatalogProfil.FeldLadezustand] = Spaltentext(r, "Ladezustand");
            werte[ModulKatalogProfil.FeldModulkosten] = Spaltentext(r, "Modulkosten");

            werte[ModulKatalogProfil.FeldWirkungsgradRt] = Spaltentext(r, "Wirkungsgrad_RT");
            werte[ModulKatalogProfil.FeldZyklen] = Spaltentext(r, "Zyklen_Zugesichert");
            werte[ModulKatalogProfil.FeldVerschleisskosten] = Spaltentext(r, "Verschleisskosten");
            werte[ModulKatalogProfil.FeldLeistungskosten] = Spaltentext(r, "Leistungskosten");
            werte[ModulKatalogProfil.FeldInvestitionFix] = Spaltentext(r, "Investition_Fix");
            werte[ModulKatalogProfil.FeldStandby] = Spaltentext(r, "Standby_Verbrauch");

            return werte;
        }

        /// <summary>Feldwert als Text; fehlende Spalte und <c>NULL</c> ergeben „".</summary>
        private static string Spaltentext(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte)) return "";
            object v = row[spalte];
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
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
