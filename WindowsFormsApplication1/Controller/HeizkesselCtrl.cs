using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows;

namespace WindowsFormsApplication1
{
    public class HeizkesselCtrl : HeizkesselModel
    {
        // --- Kompatibilitäts-Layer für bestehenden UI-Code ---
        private List<HeizkesselModel> _internalList = new List<HeizkesselModel>();
        private bool _hasSingleData = false;

        // Simuliert die alte 'rows' Variable und das 'items' Array
        public int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);
        public List<HeizkesselModel> items => _internalList;

        // Stammdaten-Listen (Bleiben erhalten für Dropdowns)
        public List<string> Brennstoffart = new List<string>();
        public List<string> Brennstoffart_Gruppe = new List<string>();

        public HeizkesselCtrl()
        {
            LoadMetaData();
        }

        private void LoadMetaData()
        {
            DataTable dtG = DataRepository.GetDataTable("SELECT Gruppe FROM Tab_BrennstoffKategorien ORDER BY ID");
            Brennstoffart_Gruppe.Clear();
            foreach (DataRow r in dtG.Rows) Brennstoffart_Gruppe.Add(r["Gruppe"].ToString());

            DataTable dtS = DataRepository.GetDataTable("SELECT Bezeichner FROM Tab_Brennstoff_Stamm ORDER BY ID");
            Brennstoffart.Clear();
            foreach (DataRow r in dtS.Rows) Brennstoffart.Add(r["Bezeichner"].ToString());
        }

        // --- READ Methoden ---

        public void ReadAll(string filter = "")
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM [Tab_Heizkessel]";
            if (!string.IsNullOrEmpty(filter)) sql += " WHERE " + filter;

            DataTable dt = DataRepository.GetDataTable(sql);
            foreach (DataRow row in dt.Rows)
            {
                _internalList.Add(MapRowToModel(row));
            }
        }

        public void ReadSingle(string name)
        {
            _internalList.Clear();
            _hasSingleData = false;

            string sql = "SELECT * FROM [Tab_Heizkessel] WHERE Bezeichner = ?";
            DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("@nam", name));

            ProcessSingleResult(dt);
        }

        private void ProcessSingleResult(DataTable dt)
        {
            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                FillModelFromRow(this, row); // Füllt Felder des Controllers
                _internalList.Add(MapRowToModel(row)); // Füllt items[0]
                _hasSingleData = true;
            }
        }

        // --- SAVE Methoden ---

        public bool Save()
        {
            // Da Tab_Heizkessel oft den 'Name' als Key nutzt, prüfen wir hier auf ID oder Name
            if (this.ID <= 0)
                return Insert();
            else
                return Update();
        }

        private bool Insert()
        {
            string sql = @"INSERT INTO [Tab_Heizkessel] (Bezeichner, Beschreibung, Firma, Ptherm, Brennstoff,
                            Wirkungsgrad_Gas, Wirkungsgrad_Öl, Investitionskosten, Raumbedarf,
                            Wartungskosten, Wartungskosten_Einheit, Nutzungsdauer, CO2, SO2, NOx, CO, Staub, Betriebsbereitschaftverlust, Brennwert)
                           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

            bool success = DataRepository.ExecuteSQL(sql, CreateParameters(false));
            if (success)
            {
                DataTable dt = DataRepository.GetDataTable("SELECT @@IDENTITY");
                if (dt.Rows.Count > 0) this.ID = Convert.ToInt32(dt.Rows[0][0]);
            }
            return success;
        }

        /// <summary>
        /// Schreibt den geladenen Projekt-Heizkessel zurück. Schlüssel ist der
        /// Primärschlüssel <c>ID</c>; ohne gesetzte ID passiert nichts.
        /// </summary>
        /// <remarks>
        /// Befund D6 (18.08.2026): Der Schlüssel war <c>Bezeichner</c> <b>ohne</b>
        /// Projektbezug. <c>Tab_Heizkessel</c> hält die Projektkopien aller Projekte, und
        /// eine Projektkopie behält den Bezeichner ihrer Stammvorlage — ein Aufruf hätte
        /// also die gleichnamigen Kessel <b>aller</b> Projekte überschrieben.
        /// <para>
        /// Statt die Methode zu entfernen (sie hat derzeit keinen Aufrufer) auf den
        /// Primärschlüssel umgestellt: <see cref="Save"/> entscheidet bereits anhand von
        /// <c>ID</c> zwischen Einfügen und Aktualisieren, die ID ist damit die Zeilenidentität,
        /// die die Klasse selbst voraussetzt — und ein Projektfilter allein bliebe mehrdeutig,
        /// sobald ein Projekt zwei gleichnamige Kessel führt (Konvention CLAUDE.md:
        /// Beziehungen über IDs). Ein Entfernen wäre zudem eine Bruchänderung an der
        /// öffentlichen Schnittstelle, während parallele Zweige offen sind.
        /// </para>
        /// </remarks>
        public bool Update()
        {
            if (this.ID <= 0) return false;   // ohne Zeilenidentität wird nichts geschrieben

            string sql = @"UPDATE [Tab_Heizkessel] SET
                            Beschreibung = ?, Firma = ?, Ptherm = ?, Brennstoff = ?,
                            Wirkungsgrad_Gas = ?, Wirkungsgrad_Öl = ?, Investitionskosten = ?,
                            Raumbedarf = ?, Wartungskosten = ?, Wartungskosten_Einheit = ?, Nutzungsdauer = ?,
                            CO2 = ?, SO2 = ?, NOx = ?, CO = ?, Staub = ?,
                            Betriebsbereitschaftverlust = ?, Brennwert = ?
                          WHERE ID = ?";

            return DataRepository.ExecuteSQL(sql, CreateParameters(true));
        }

        public bool Delete(string name)
        {
            string sql = "DELETE FROM [Tab_Heizkessel] WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new OleDbParameter("@nam", name));
        }

        // --- STAMM -> PROJEKT KOPIE (analog BHKWCtrl) ---

        // Liefert die Projekt-ID (Tab_Heizkessel.ID) eines Bezeichners im Projekt, oder 0 wenn nicht vorhanden.
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM [Tab_Heizkessel] WHERE Bezeichner = ? AND ID_Projekt = ?",
                new OleDbParameter("@nam", szBezeichner ?? ""),
                new OleDbParameter("@idProj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        public bool ExistsInProjekt(string szBezeichner, int idProjekt)
        {
            return GetProjektId(szBezeichner, idProjekt) > 0;
        }

        // Kopiert einen Stammdatensatz (Tab_Heizkessel_STAMM) in die Projekt-Tabelle (Tab_Heizkessel),
        // sofern er fuer das Projekt noch nicht existiert. Setzt ID_Projekt und vergibt eine neue
        // Projekt-ID. Rueckgabe: Projekt-ID (Tab_Heizkessel.ID) des kopierten ODER bereits vorhandenen
        // Datensatzes, -1 bei Fehler. Die zurueckgegebene ID ist der Wert, den WErzeugerModel.ID_Kessel
        // tragen muss (Beziehungen verweisen auf die Projekt-Tabelle, nicht auf STAMM).
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + HeizkesselStammCtrl.TABLE + "] WHERE ID = ?",
                    new OleDbParameter("@id", stammId));

                if (dt == null || dt.Rows.Count == 0)
                {
                    // NACHARBEIT PAKET 8, BEFUND N10: über dieselbe Entscheidungsstelle
                    // wie PufferSpCtrl.CopyFromStamm - im Engine-Modus Protokolleintrag,
                    // sonst der Dialog mit unverändertem Wortlaut. HeizkesselCtrl wird
                    // aus SimulationSPK heraus benutzt, der Pfad ist also erreichbar.
                    DataRepository.FehlerMelden("Der Heizkessel-Stammdatensatz wurde nicht gefunden (ID " + stammId + ").");
                    return -1;
                }

                DataRow s = dt.Rows[0];
                string szBezeichner = s["Bezeichner"].ToString();

                int vorhandeneId = GetProjektId(szBezeichner, idProjekt);
                if (vorhandeneId > 0) return vorhandeneId;

                int neueId = DataRepository.GetMaxID("Tab_Heizkessel") + 1;

                // ReadOnly wird NICHT uebernommen (existiert in der Projekt-Tabelle nicht).
                string sql = @"INSERT INTO [Tab_Heizkessel]
                    (ID, ID_Projekt, Bezeichner, Firma, Beschreibung, Ptherm, Brennstoff,
                     Wirkungsgrad_Gas, Wirkungsgrad_Öl, Investitionskosten, Raumbedarf, Wartungskosten,
                     Wartungskosten_Einheit,
                     Nutzungsdauer, CO2, SO2, NOx, CO, Staub, Betriebsbereitschaftverlust, Brennwert,
                     Vorlauf, Ruecklauf)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", neueId),
                    new OleDbParameter("@idProj", idProjekt),
                    P("@bez", s["Bezeichner"]),
                    P("@fir", s["Firma"]),
                    P("@bes", s["Beschreibung"]),
                    P("@pth", s["Ptherm"]),
                    P("@bre", s["Brennstoff"]),
                    P("@wgg", s["Wirkungsgrad_Gas"]),
                    P("@wgo", s["Wirkungsgrad_Öl"]),
                    P("@inv", s["Investitionskosten"]),
                    P("@rau", s["Raumbedarf"]),
                    P("@war", s["Wartungskosten"]),
                    // Die Bezugsgroesse muss mitkopiert werden, sonst haette die
                    // Projektkopie einen Betrag ohne Einheit (Migrationsschritt 15).
                    // ColOrNull haelt den Fall offen, dass die Spalte auf einer nicht
                    // migrierten Datenbank im STAMM noch fehlt.
                    P("@wae", Einheit(ColOrNull(s, SchemaKatalog.SPALTE_KESSEL_WARTUNG_EINHEIT) as string)),
                    P("@nut", s["Nutzungsdauer"]),
                    P("@co2", s["CO2"]),
                    P("@so2", s["SO2"]),
                    P("@nox", s["NOx"]),
                    P("@co", s["CO"]),
                    P("@sta", s["Staub"]),
                    P("@bbv", s["Betriebsbereitschaftverlust"]),
                    P("@brn", s["Brennwert"]),
                    P("@vor", ColOrNull(s, "Vorlauf")),
                    P("@rue", ColOrNull(s, "Ruecklauf"))
                };

                bool ok = DataRepository.ExecuteSQL(sql, ps);
                return ok ? neueId : -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Kopieren des Heizkessels aus den Stammdaten: " + ex.Message);
                return -1;
            }
        }

        // Komfort-Ueberladung: kopiert per Bezeichner (Stamm-Lookup ueber Bezeichner).
        public int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            int stammId = DataRepository.GetIdByName(HeizkesselStammCtrl.TABLE, "Bezeichner", szBezeichner);
            if (stammId <= 0) return -1;
            return CopyFromStamm(stammId, idProjekt);
        }

        // Loescht einen Projekt-Heizkessel eines Projekts.
        public bool DeleteFromProjekt(string szBezeichner, int idProjekt)
        {
            string sql = "DELETE FROM [Tab_Heizkessel] WHERE Bezeichner = ? AND ID_Projekt = ?";
            return DataRepository.ExecuteSQL(sql,
                new OleDbParameter("@nam", szBezeichner ?? ""),
                new OleDbParameter("@idProj", idProjekt));
        }

        // Hilfsfunktion: OleDbParameter mit DBNull-Behandlung
        private static OleDbParameter P(string name, object value)
        {
            return new OleDbParameter(name, value ?? DBNull.Value);
        }

        // Hilfsfunktion: Spaltenwert oder DBNull, falls die Spalte nicht existiert
        private static object ColOrNull(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) ? row[col] : DBNull.Value;
        }

        /// <summary>
        /// Bezugsgröße der Wartungskosten mit Rückfall auf den festen Jahresbetrag.
        /// </summary>
        /// <remarks>
        /// Leer bedeutet „nicht gesetzt" und tritt nur auf, solange Migrationsschritt 15
        /// nicht gelaufen ist. Die Rückfallebene ist dieselbe Wahl wie dort
        /// (<see cref="DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR"/>) — Lese- und Schreibseite
        /// dürfen sich hier nicht unterscheiden, sonst wechselte die Bedeutung einer Zahl
        /// mit dem Migrationsstand.
        /// </remarks>
        internal static string Einheit(string wert)
        {
            return string.IsNullOrWhiteSpace(wert) ? DbWerte.KESSEL_WARTUNG_EINHEIT_JAHR : wert.Trim();
        }

        // --- MAPPING & PARAMETER ---

        private OleDbParameter[] CreateParameters(bool isUpdate)
        {
            List<OleDbParameter> p = new List<OleDbParameter>();

            // Bei Insert muss der Name am Anfang stehen (gemäß SQL String)
            if (!isUpdate) p.Add(new OleDbParameter("@nam", this.Name ?? ""));

            p.Add(new OleDbParameter("@bes", this.Beschreibung ?? ""));
            p.Add(new OleDbParameter("@fir", this.Firma ?? ""));
            p.Add(new OleDbParameter("@pth", this.Ptherm));
            p.Add(new OleDbParameter("@bre", this.Brennstoff));
            p.Add(new OleDbParameter("@wgg", this.Wirkungsgrad_Gas));
            p.Add(new OleDbParameter("@wgo", this.Wirkungsgrad_Oel));
            p.Add(new OleDbParameter("@inv", this.Investitionskosten));
            p.Add(new OleDbParameter("@rau", this.Raumbedarf));
            p.Add(new OleDbParameter("@war", this.Wartungskosten));
            p.Add(new OleDbParameter("@wae", Einheit(this.Wartungskosten_Einheit)));
            p.Add(new OleDbParameter("@nut", this.Nutzungsdauer));
            p.Add(new OleDbParameter("@co2", this.CO2));
            p.Add(new OleDbParameter("@so2", this.SO2));
            p.Add(new OleDbParameter("@nox", this.NOx));
            p.Add(new OleDbParameter("@co", this.CO));
            p.Add(new OleDbParameter("@sta", this.Staub));
            p.Add(new OleDbParameter("@bbv", this.Betriebsbereitschaftverlust));
            p.Add(new OleDbParameter("@brn", this.Brennwert));

            // Bei Update steht der Schlüssel im WHERE-Teil (am Ende) — seit Befund D6
            // der Primärschlüssel ID statt des projektübergreifend mehrdeutigen Bezeichners.
            if (isUpdate) p.Add(new OleDbParameter("@id", this.ID));

            return p.ToArray();
        }

        private void FillModelFromRow(HeizkesselModel target, DataRow row)
        {
            target.ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            target.Name = row["Bezeichner"]?.ToString() ?? "";
            target.Firma = row["Firma"]?.ToString() ?? "";
            target.Beschreibung = row["Beschreibung"]?.ToString() ?? "";
            target.Ptherm = row["Ptherm"] != DBNull.Value ? Convert.ToDouble(row["Ptherm"]) : 0.0;
            target.Brennstoff = row["Brennstoff"] != DBNull.Value ? Convert.ToInt32(row["Brennstoff"]) : 0;
            target.Wirkungsgrad_Gas = row["Wirkungsgrad_Gas"] != DBNull.Value ? Convert.ToDouble(row["Wirkungsgrad_Gas"]) : 0.0;
            target.Wirkungsgrad_Oel = row["Wirkungsgrad_Öl"] != DBNull.Value ? Convert.ToDouble(row["Wirkungsgrad_Öl"]) : 0.0;
            target.Investitionskosten = row["Investitionskosten"] != DBNull.Value ? Convert.ToDouble(row["Investitionskosten"]) : 0.0;
            target.Raumbedarf = row["Raumbedarf"] != DBNull.Value ? Convert.ToDouble(row["Raumbedarf"]) : 0.0;
            target.Wartungskosten = row["Wartungskosten"] != DBNull.Value ? Convert.ToDouble(row["Wartungskosten"]) : 0.0;
            // Spaltenprüfung, weil eine nicht migrierte Datenbank die Spalte noch nicht führt.
            target.Wartungskosten_Einheit = Einheit(ColOrNull(row, SchemaKatalog.SPALTE_KESSEL_WARTUNG_EINHEIT) as string);
            target.Nutzungsdauer = row["Nutzungsdauer"] != DBNull.Value ? Convert.ToDouble(row["Nutzungsdauer"]) : 0.0;
            target.CO2 = row["CO2"] != DBNull.Value ? Convert.ToDouble(row["CO2"]) : 0.0;
            target.SO2 = row["SO2"] != DBNull.Value ? Convert.ToDouble(row["SO2"]) : 0.0;
            target.NOx = row["NOx"] != DBNull.Value ? Convert.ToDouble(row["NOx"]) : 0.0;
            target.CO = row["CO"] != DBNull.Value ? Convert.ToDouble(row["CO"]) : 0.0;
            target.Staub = row["Staub"] != DBNull.Value ? Convert.ToDouble(row["Staub"]) : 0.0;
            target.Betriebsbereitschaftverlust = row["Betriebsbereitschaftverlust"] != DBNull.Value ? Convert.ToDouble(row["Betriebsbereitschaftverlust"]) : 0.0;
            target.Brennwert = row["Brennwert"] != DBNull.Value ? Convert.ToBoolean(row["Brennwert"]) : false;  
        }

        private HeizkesselModel MapRowToModel(DataRow row)
        {
            HeizkesselModel m = new HeizkesselModel();
            FillModelFromRow(m, row);
            return m;
        }
    }
}