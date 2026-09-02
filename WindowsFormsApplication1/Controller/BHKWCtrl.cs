using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public class BHKWCtrl : BHKWModel
    {
        public BHKWModel model;

        // --- Statische Texte (beibehalten) ---
        public static string[] BrennstoffartText = { "Öl", "Gas", "Biogas", "Rapsöl", "Holz/Pellet", "Sonstiges", "", "", "Flüssiggas", "", "", "Bioerdgas", "", "", "", "Strom" };
        public static string[] LeistungText = { "kleiner 20 kW", "20 bis 40 kW", "40 bis 80 kW", "80 bis 200 kW", "200 bis 500 kW", "500 bis 800 kW", "800 bis 1200 kW", "größer 1200 kW" };
        public static string[] LeistungFilterText = { "Ptherm LIKE '%'", "Ptherm<20", "Ptherm>=20 and Ptherm<40", "Ptherm>=40 and Ptherm<80", "Ptherm>=80 and Ptherm<200",
                                                      "Ptherm>=200 and Ptherm<500", "Ptherm>=500 and Ptherm<800", "Ptherm>=800 and Ptherm<1200", "Ptherm>=1200" };

        // --- Kompatibilitäts-Layer nach vereinbarter Schablone ---
        private List<BHKWModel> _internalList = new List<BHKWModel>();
        private bool _hasSingleData = false;

        // Simuliert die alte 'rows' Variable dynamisch (ohne 'new', da aus Model gelöscht)
        public int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);

        // Simuliert das alte 'items' Array als Liste (ohne 'new')
        public List<BHKWModel> items => _internalList;

        // HIER ERGÄNZT: Das OleDbCommand für transaktionale Aufrufe aus dem UI-Code
        public OleDbCommand DBCommand;

        /// <summary>
        /// ARBEITSPAKET S4e: Laeuft der Schreibvorgang innerhalb einer FREMDEN
        /// Transaktion (Form_DBBHKW), setzt der Aufrufer hier den Vorgang. Bis dahin
        /// wurden dafuer Verbindung und Transaktion am <c>DBCommand</c> gesetzt; das
        /// OleDbCommand bleibt reiner Datentraeger fuer CommandText und Parameter.
        /// <c>null</c> = eigenstaendiger Aufruf ueber die Zugriffsschicht.
        /// </summary>
        public DbVorgang Vorgang { get; set; }


        // Stammdaten-Listen (Bleiben erhalten für Dropdowns)
        public List<string> Brennstoffart = new List<string>();
        public List<string> Brennstoffart_Gruppe = new List<string>();

        public BHKWCtrl()
        {
            _hasSingleData = false;
            DBCommand = new OleDbCommand(); // Command im Konstruktor initialisieren
            model = new BHKWModel();
            LoadMetaData();
        }

        ~BHKWCtrl()
        {
            if (DBCommand != null)
            {
                DBCommand.Dispose();
            }
        }

        #region --- DATABASE OPERATIONS ---

        private void LoadMetaData()
        {
            DataTable dtG = DataRepository.GetDataTable("SELECT Gruppe FROM Tab_BrennstoffKategorien ORDER BY ID");
            Brennstoffart_Gruppe.Clear();
            foreach (DataRow r in dtG.Rows) Brennstoffart_Gruppe.Add(r["Gruppe"].ToString());

            DataTable dtS = DataRepository.GetDataTable("SELECT Bezeichner FROM Tab_Brennstoff_Stamm ORDER BY ID");
            Brennstoffart.Clear();
            foreach (DataRow r in dtS.Rows) Brennstoffart.Add(r["Bezeichner"].ToString());
        }

        // Liest alle Projekt-BHKW. Zusaetzlicher optionaler Projektfilter, damit die
        // "Ansicht der im Projekt ausgewaehlten Komponenten" nur die Datensaetze des
        // aktuellen Projekts anzeigt.
        public void ReadAll(string szFilter = "")
        {
            string sql = "SELECT * FROM Tab_BHKW";
            if (!string.IsNullOrEmpty(szFilter))
            {
                sql += " WHERE " + szFilter;
            }
            sql += " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();
            _hasSingleData = false;

            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    _internalList.Add(MapRowToModel(row));
                }
            }
        }

        // Komfort: alle Projekt-BHKW eines bestimmten Projekts lesen.
        public void ReadAllByProjekt(int idProjekt)
        {
            ReadAll("ID_Projekt = " + idProjekt);
        }

        public void ReadSingle(int ID)
        {
            string sql = "SELECT * FROM Tab_BHKW WHERE ID = ?";
            DbParam[] ps = { new DbParam("@id", ID) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            if (dt != null && dt.Rows.Count > 0)
            {
                MapThisToRow(dt.Rows[0]);
                _hasSingleData = true;
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            string sql = "SELECT * FROM Tab_BHKW WHERE Bezeichner = ?";
            DbParam[] ps = { new DbParam("@name", szBezeichner) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            if (dt != null && dt.Rows.Count > 0)
            {
                MapThisToRow(dt.Rows[0]);
                _hasSingleData = true;
            }
        }

        /// <summary>
        /// Schreibt das in <see cref="model"/> gehaltene Projekt-BHKW zurück. Schlüssel ist
        /// der Primärschlüssel <c>ID</c>; ohne gesetzte ID passiert nichts.
        /// </summary>
        /// <remarks>
        /// Befund D6 (18.08.2026): Der Schlüssel war <c>Bezeichner</c> <b>ohne</b>
        /// Projektbezug. <c>Tab_BHKW</c> hält die Projektkopien aller Projekte, und eine
        /// Projektkopie behält den Bezeichner ihrer Stammvorlage — ein Aufruf hätte also die
        /// gleichnamigen BHKW <b>aller</b> Projekte überschrieben. Umgestellt auf den
        /// Primärschlüssel statt auf einen zusätzlichen Projektfilter, weil ein Projekt zwei
        /// gleichnamige Geräte führen kann (Konvention CLAUDE.md: Beziehungen über IDs);
        /// Entfernen schied aus, weil es eine Bruchänderung an der öffentlichen Schnittstelle
        /// wäre, während parallele Zweige offen sind. Die Methode hat derzeit keinen Aufrufer.
        /// </remarks>
        public bool Update()
        {
            try
            {
                if (model == null || model.m_ID <= 0) return false;   // ohne Zeilenidentität nichts schreiben

                string sql = @"UPDATE Tab_BHKW SET
                               Beschreibung=?, Firma=?, Motortyp=?, Ptherm=?, Pel=?,
                               Brennstoff=?, Wirkungsgrad=?, Investition_kwel=?, Raumbedarf=?,
                               Wartungskosten_kwhel=?, Nutzungsdauer=?, NOx=?, SO2=?, CO=?,
                               CO2=?, Staub=?, Grenzleistung=?, Kosten_Modul=?, Kosten_Montage=?,
                               Kosten_Lieferung=?, Kosten_Schallschutzhaube=?, Kosten_Abgasreinigung=?
                               WHERE ID=?";

                // Die Einzelposten fuehren (Regel in BHKWKosten, Nutzerentscheid
                // 22.08.2026): der spezifische Wert wird hier aus den Posten und Pel
                // abgeleitet, damit auch dieser Schreibweg die beiden Groessen nicht
                // auseinanderlaufen lassen kann. CopyFromStamm rechnet bewusst NICHT
                // nach: es kopiert einen Stammsatz unveraendert ins Projekt.
                model.m_Investition_KWel = BHKWKosten.JeKWel(
                    BHKWKosten.Summe(model.m_Kosten_Modul, model.m_Kosten_Montage,
                                     model.m_Kosten_Lieferung, model.m_Kosten_Schallschutzhaube,
                                     model.m_Kosten_Abgasreinigung),
                    model.m_Pel);

                // Nutzt das instanziierte DBCommand als Datenträger für die Parameter
                DBCommand.CommandText = sql;
                // ARBEITSPAKET iU6: Die Parametersammlung des DBCommand war hier nur
                // ZWISCHENSPEICHER - unten wurde sie sofort in ein Array kopiert und an
                // die Zugriffsschicht gegeben. Ein OleDbParameter wuerde dafuer heute
                // nur noch auf Nicht-Windows scheitern, also sammelt eine Liste die
                // DbParam direkt. Reihenfolge, Namen und Werte unveraendert; gebunden
                // wird ohnehin nach Position.
                List<DbParam> werte = new List<DbParam>();
                DBCommand.Parameters.Clear();

                // Beachte: Wenn das Control über InitDatensatzUpdate() befüllt wurde,
                // müssen wir hier auf das zugewiesene 'model' Objekt zugreifen!
                werte.Add(new DbParam("@besch", model.m_szBeschreibung ?? ""));
                werte.Add(new DbParam("@firma", model.m_szFirma ?? ""));
                werte.Add(new DbParam("@motor", model.m_szMotortyp ?? ""));
                werte.Add(new DbParam("@ptherm", model.m_Ptherm));
                werte.Add(new DbParam("@pel", model.m_Pel));
                werte.Add(new DbParam("@brenn", model.m_Brennstoff));
                werte.Add(new DbParam("@wirk", model.m_Wirkungsgrad));
                werte.Add(new DbParam("@inv", model.m_Investition_KWel));
                werte.Add(new DbParam("@raum", model.m_Raumbedarf));
                werte.Add(new DbParam("@wart", model.m_Wartungskosten_kWhel));
                werte.Add(new DbParam("@nutz", model.m_Nutzungsdauer));
                werte.Add(new DbParam("@nox", model.m_NOx));
                werte.Add(new DbParam("@so2", model.m_SO2));
                werte.Add(new DbParam("@co", model.m_CO));
                werte.Add(new DbParam("@co2", model.m_CO2));
                werte.Add(new DbParam("@staub", model.m_Staub));
                werte.Add(new DbParam("@grenz", model.m_Grenzleistung));
                werte.Add(new DbParam("@modul", model.m_Kosten_Modul));
                werte.Add(new DbParam("@mont", model.m_Kosten_Montage));
                werte.Add(new DbParam("@lief", model.m_Kosten_Lieferung));
                werte.Add(new DbParam("@schall", model.m_Kosten_Schallschutzhaube));
                werte.Add(new DbParam("@abgas", model.m_Kosten_Abgasreinigung));
                werte.Add(new DbParam("@key", model.m_ID));

                // ARBEITSPAKET S4b/S4e: Ohne fremden Vorgang läuft der Schreibvorgang
                // über die Zugriffsschicht - die eigene Standalone-Verbindung entfällt.
                // MIT Vorgang (Transaktion aus der UI) läuft er auf DESSEN Verbindung
                // und in DESSEN Transaktion.

                if (Vorgang == null)
                {
                    // ExecuteNonQuery statt ExecuteSQL: Diese Methode meldet ihre Fehler
                    // selbst auf die Konsole (catch unten) und darf keinen Dialog zeigen.
                    if (StilleDb.NonQuery(sql, werte.ToArray()) < 0) return false;
                    return true;
                }

                Vorgang.Ausfuehren(sql, werte.ToArray());

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren des BHKW: " + ex.Message);
                return false;
            }
        }

        // Liefert die Projekt-ID (Tab_BHKW.ID) eines Bezeichners im Projekt, oder 0 wenn nicht vorhanden.
        public int GetProjektId(string szBezeichner, int idProjekt)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_BHKW WHERE Bezeichner = ? AND ID_Projekt = ?",
                new DbParam("@name", szBezeichner ?? ""),
                new DbParam("@idProj", idProjekt));
            return (v != null && v != DBNull.Value) ? Convert.ToInt32(v) : 0;
        }

        // Prueft, ob im angegebenen Projekt bereits ein BHKW mit diesem Bezeichner existiert.
        public bool ExistsInProjekt(string szBezeichner, int idProjekt)
        {
            return GetProjektId(szBezeichner, idProjekt) > 0;
        }

        // Kopiert einen Stammdatensatz (Tab_BHKW_STAMM) in die Projekt-Tabelle (Tab_BHKW),
        // sofern er fuer das Projekt noch nicht existiert. Setzt ID_Projekt und vergibt eine
        // neue Projekt-ID. Alle DB-Zugriffe laufen ueber das DataRepository.
        // Rueckgabe: Projekt-ID (Tab_BHKW.ID) des kopierten ODER bereits vorhandenen Datensatzes,
        //            -1 bei Fehler. Die zurueckgegebene ID ist der Wert, den WErzeugerModel.ID_BHKW
        //            tragen muss (Beziehungen verweisen auf die Projekt-Tabelle, nicht auf STAMM).
        public int CopyFromStamm(int stammId, int idProjekt)
        {
            try
            {
                // 1. Stammdatensatz lesen
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE ID = ?",
                    new DbParam("@id", stammId));

                if (dt == null || dt.Rows.Count == 0)
                {
                    // NACHARBEIT PAKET 8, BEFUND N10: über dieselbe Entscheidungsstelle
                    // wie PufferSpCtrl.CopyFromStamm - im Engine-Modus Protokolleintrag,
                    // sonst der Dialog mit unverändertem Wortlaut. BHKWCtrl wird aus
                    // SimulationBHKW heraus benutzt, der Pfad ist also erreichbar.
                    DataRepository.FehlerMelden("Der Stammdatensatz wurde nicht gefunden (ID " + stammId + ").");
                    return -1;
                }

                DataRow s = dt.Rows[0];
                string szBezeichner = s["Bezeichner"].ToString();

                // 2. Dublettenpruefung: bereits im Projekt vorhanden? -> vorhandene Projekt-ID zurueckgeben
                int vorhandeneId = GetProjektId(szBezeichner, idProjekt);
                if (vorhandeneId > 0)
                {
                    return vorhandeneId;
                }

                // 3. Neue Projekt-ID bestimmen (funktioniert fuer AutoWert- wie auch Long-Spalten)
                int neueId = DataRepository.GetMaxID("Tab_BHKW") + 1;

                // 4. Datensatz in die Projekt-Tabelle kopieren, ID_Projekt setzen.
                //    Spalten, die in beiden Tabellen existieren, werden 1:1 uebernommen
                //    (inkl. Vorlauf/Rücklauf). ReadOnly wird NICHT uebernommen.
                string sql = @"INSERT INTO Tab_BHKW
                    (ID, ID_Projekt, Bezeichner, Firma, Beschreibung, Ptherm, Pel, Brennstoff,
                     Wirkungsgrad, Investition_kwel, Raumbedarf, Wartungskosten_kwhel, Nutzungsdauer,
                     NOx, SO2, CO, CO2, Staub, Motortyp, Grenzleistung, Kosten_Modul, Kosten_Montage,
                     Kosten_Lieferung, Kosten_Schallschutzhaube, Kosten_Abgasreinigung, Vorlauf, Ruecklauf)
                    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
      
                DbParam[] ps = {
                    new DbParam("@id", neueId),
                    new DbParam("@idProj", idProjekt),
                    P("@bez", s["Bezeichner"]),
                    P("@firma", s["Firma"]),
                    P("@besch", s["Beschreibung"]),
                    P("@ptherm", s["Ptherm"]),
                    P("@pel", s["Pel"]),
                    P("@brenn", s["Brennstoff"]),
                    P("@wirk", s["Wirkungsgrad"]),
                    P("@inv", s["Investition_kwel"]),
                    P("@raum", s["Raumbedarf"]),
                    P("@wart", s["Wartungskosten_kwhel"]),
                    P("@nutz", s["Nutzungsdauer"]),
                    P("@nox", s["NOx"]),
                    P("@so2", s["SO2"]),
                    P("@co", s["CO"]),
                    P("@co2", s["CO2"]),
                    P("@staub", s["Staub"]),
                    P("@motor", s["Motortyp"]),
                    P("@grenz", s["Grenzleistung"]),
                    P("@modul", s["Kosten_Modul"]),
                    P("@mont", s["Kosten_Montage"]),
                    P("@lief", s["Kosten_Lieferung"]),
                    P("@schall", s["Kosten_Schallschutzhaube"]),
                    P("@abgas", s["Kosten_Abgasreinigung"]),
                    P("@vor", ColOrNull(s, "Vorlauf")),
                    P("@rue", ColOrNull(s, "Ruecklauf"))
                };

                bool ok = DataRepository.ExecuteSQL(sql, ps);
                return ok ? neueId : -1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Kopieren des BHKW aus den Stammdaten: " + ex.Message);
                return -1;
            }
        }

        // Kopiert per Bezeichner (Komfort-Ueberladung, z.B. wenn die Auswahlliste nur den Namen kennt).
        public int CopyFromStamm(string szBezeichner, int idProjekt)
        {
            int stammId = DataRepository.GetIdByName(BHKWStammCtrl.TABLE, "Bezeichner", szBezeichner);
            if (stammId <= 0) return -1;
            return CopyFromStamm(stammId, idProjekt);
        }

        // Loescht einen Projekt-BHKW eines Projekts (Ansicht der ausgewaehlten Komponenten).
        public bool DeleteFromProjekt(string szBezeichner, int idProjekt)
        {
            string sql = "DELETE FROM Tab_BHKW WHERE Bezeichner = ? AND ID_Projekt = ?";
            return DataRepository.ExecuteSQL(sql,
                new DbParam("@name", szBezeichner ?? ""),
                new DbParam("@idProj", idProjekt));
        }

        // Hilfsfunktion: DbParam mit DBNull-Behandlung
        private static DbParam P(string name, object value)
        {
            return new DbParam(name, value ?? DBNull.Value);
        }

        // Hilfsfunktion: Spaltenwert oder DBNull, falls die Spalte nicht existiert
        private static object ColOrNull(DataRow row, string col)
        {
            return row.Table.Columns.Contains(col) ? row[col] : DBNull.Value;
        }

        #endregion

        #region --- UI FILL METHODS ---

        public void FillComboBox(ComboBox ctrl)
        {
            ctrl.Items.Clear();
            foreach (var item in _internalList)
            {
                ctrl.Items.Add(item.m_szBezeichner);
            }
        }

        #endregion

        #region --- MAPPING HELPERS ---

        private BHKWModel MapRowToModel(DataRow row)
        {
            BHKWModel m = new BHKWModel();
            m.m_ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
            m.m_ID_Projekt = row.Table.Columns.Contains("ID_Projekt") && row["ID_Projekt"] != DBNull.Value ? Convert.ToInt32(row["ID_Projekt"]) : 0;
            m.m_szBezeichner = row["Bezeichner"].ToString();
            m.m_szFirma = row["Firma"].ToString();
            m.m_szBeschreibung = row["Beschreibung"].ToString();
            m.m_Ptherm = row["Ptherm"] != DBNull.Value ? Convert.ToDouble(row["Ptherm"]) : 0;
            m.m_Pel = row["Pel"] != DBNull.Value ? Convert.ToDouble(row["Pel"]) : 0;
            m.m_Brennstoff = row["Brennstoff"] != DBNull.Value ? Convert.ToInt32(row["Brennstoff"]) : 0;
            m.m_Wirkungsgrad = row["Wirkungsgrad"] != DBNull.Value ? Convert.ToDouble(row["Wirkungsgrad"]) : 0;
            m.m_Investition_KWel = row["Investition_kwel"] != DBNull.Value ? Convert.ToDouble(row["Investition_kwel"]) : 0;
            m.m_Raumbedarf = row["Raumbedarf"] != DBNull.Value ? Convert.ToDouble(row["Raumbedarf"]) : 0;
            m.m_Wartungskosten_kWhel = row["Wartungskosten_kwhel"] != DBNull.Value ? Convert.ToDouble(row["Wartungskosten_kwhel"]) : 0;
            m.m_Nutzungsdauer = row["Nutzungsdauer"] != DBNull.Value ? Convert.ToInt32(row["Nutzungsdauer"]) : 0;
            m.m_NOx = row["NOx"] != DBNull.Value ? Convert.ToInt32(row["NOx"]) : 0;
            m.m_SO2 = row["SO2"] != DBNull.Value ? Convert.ToInt32(row["SO2"]) : 0;
            m.m_CO = row["CO"] != DBNull.Value ? Convert.ToInt32(row["CO"]) : 0;
            m.m_CO2 = row["CO2"] != DBNull.Value ? Convert.ToInt32(row["CO2"]) : 0;
            m.m_Staub = row["Staub"] != DBNull.Value ? Convert.ToInt32(row["Staub"]) : 0;
            m.m_szMotortyp = row["Motortyp"].ToString();
            m.m_Grenzleistung = row["Grenzleistung"] != DBNull.Value ? Convert.ToDouble(row["Grenzleistung"]) : 0;
            m.m_Kosten_Modul = row["Kosten_Modul"] != DBNull.Value ? Convert.ToDouble(row["Kosten_Modul"]) : 0;
            m.m_Kosten_Montage = row["Kosten_Montage"] != DBNull.Value ? Convert.ToDouble(row["Kosten_Montage"]) : 0;
            m.m_Kosten_Lieferung = row["Kosten_Lieferung"] != DBNull.Value ? Convert.ToDouble(row["Kosten_Lieferung"]) : 0;
            m.m_Kosten_Schallschutzhaube = row["Kosten_Schallschutzhaube"] != DBNull.Value ? Convert.ToDouble(row["Kosten_Schallschutzhaube"]) : 0;
            m.m_Kosten_Abgasreinigung = row["Kosten_Abgasreinigung"] != DBNull.Value ? Convert.ToDouble(row["Kosten_Abgasreinigung"]) : 0;
            return m;
        }

        private void MapThisToRow(DataRow row)
        {
            BHKWModel m = MapRowToModel(row);
            this.m_ID = m.m_ID;
            this.m_ID_Projekt = m.m_ID_Projekt;
            this.m_szBezeichner = m.m_szBezeichner;
            this.m_szFirma = m.m_szFirma;
            this.m_szBeschreibung = m.m_szBeschreibung;
            this.m_Ptherm = m.m_Ptherm;
            this.m_Pel = m.m_Pel;
            this.m_Brennstoff = m.m_Brennstoff;
            this.m_Wirkungsgrad = m.m_Wirkungsgrad;
            this.m_Investition_KWel = m.m_Investition_KWel;
            this.m_Raumbedarf = m.m_Raumbedarf;
            this.m_Wartungskosten_kWhel = m.m_Wartungskosten_kWhel;
            this.m_Nutzungsdauer = m.m_Nutzungsdauer;
            this.m_NOx = m.m_NOx;
            this.m_SO2 = m.m_SO2;
            this.m_CO = m.m_CO;
            this.m_CO2 = m.m_CO2;
            this.m_Staub = m.m_Staub;
            this.m_szMotortyp = m.m_szMotortyp;
            this.m_Grenzleistung = m.m_Grenzleistung;
            this.m_Kosten_Modul = m.m_Kosten_Modul;
            this.m_Kosten_Montage = m.m_Kosten_Montage;
            this.m_Kosten_Lieferung = m.m_Kosten_Lieferung;
            this.m_Kosten_Schallschutzhaube = m.m_Kosten_Schallschutzhaube;
            this.m_Kosten_Abgasreinigung = m.m_Kosten_Abgasreinigung;
        }

        #endregion
    }
}
