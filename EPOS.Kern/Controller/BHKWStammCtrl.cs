using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // Controller fuer die Stammdaten-Tabelle Tab_BHKW_STAMM.
    // Analog zu BHKWCtrl, aber:
    //   - Tabelle = Tab_BHKW_STAMM
    //   - liest/schreibt das Feld ReadOnly
    //   - Update() und Delete() verweigern die Aenderung schreibgeschuetzter Datensaetze
    // Alle DB-Zugriffe laufen ueber DataRepository.
    public class BHKWStammCtrl : BHKWStammModel
    {
        public const string TABLE = "Tab_BHKW_STAMM";

        public BHKWStammModel model;

        // --- Statische Texte (aus BHKWCtrl uebernommen) ---
        public static string[] BrennstoffartText = { "Öl", "Gas", "Biogas", "Rapsöl", "Holz/Pellet", "Sonstiges", "", "", "Flüssiggas", "", "", "Bioerdgas", "", "", "", "Strom" };
        public static string[] LeistungText = { "kleiner 20 kW", "20 bis 40 kW", "40 bis 80 kW", "80 bis 200 kW", "200 bis 500 kW", "500 bis 800 kW", "800 bis 1200 kW", "größer 1200 kW" };
        public static string[] LeistungFilterText = { "Ptherm LIKE '%'", "Ptherm<20", "Ptherm>=20 and Ptherm<40", "Ptherm>=40 and Ptherm<80", "Ptherm>=80 and Ptherm<200",
                                                      "Ptherm>=200 and Ptherm<500", "Ptherm>=500 and Ptherm<800", "Ptherm>=800 and Ptherm<1200", "Ptherm>=1200" };

        // --- Kompatibilitaets-Layer nach vereinbarter Schablone ---
        private List<BHKWStammModel> _internalList = new List<BHKWStammModel>();
        private bool _hasSingleData = false;

        public int rows => _internalList.Count > 0 ? _internalList.Count : (_hasSingleData ? 1 : 0);
        public List<BHKWStammModel> items => _internalList;

        // iU3 SCHRITT 3: Das OleDbCommand ist entfallen. Es war seit 3a28e4c nur noch
        // Rest - CommandText setzen und Parameters.Clear() -, extern von niemandem
        // benutzt, und sein Konstruktor wirft auf net10.0 PlatformNotSupportedException.

        /// <summary>
        /// ARBEITSPAKET S4e: Laeuft der Schreibvorgang innerhalb einer FREMDEN
        /// Transaktion (Form_DBBHKW), setzt der Aufrufer hier den Vorgang. Bis dahin
        /// wurden dafuer Verbindung und Transaktion an einem <c>OleDbCommand</c>
        /// gesetzt; dieses Feld ist mit iU3 ersatzlos entfallen.
        /// <c>null</c> = eigenstaendiger Aufruf ueber die Zugriffsschicht.
        /// </summary>
        public DbVorgang Vorgang { get; set; }


        // Stammdaten-Listen (Dropdowns)
        public List<string> Brennstoffart = new List<string>();
        public List<string> Brennstoffart_Gruppe = new List<string>();

        public BHKWStammCtrl()
        {
            _hasSingleData = false;
            model = new BHKWStammModel();
            LoadMetaData();
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

        public void ReadAll(string szFilter = "")
        {
            string sql = "SELECT * FROM " + TABLE;
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

        public void ReadSingle(int ID)
        {
            string sql = "SELECT * FROM " + TABLE + " WHERE ID = ?";
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
            string sql = "SELECT * FROM " + TABLE + " WHERE Bezeichner = ?";
            DbParam[] ps = { new DbParam("@name", szBezeichner) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            if (dt != null && dt.Rows.Count > 0)
            {
                MapThisToRow(dt.Rows[0]);
                _hasSingleData = true;
            }
        }

        /// <summary>
        /// Liest einen Stammdatensatz per Bezeichner und liefert ihn als eigenes
        /// Model. Gedacht fuer Masken, die nur einen TEIL der Felder anzeigen und
        /// den Rest unveraendert zurueckschreiben muessen (Form_BHKWAdmin) - so
        /// bleibt MapRowToModel die einzige Abbildungsstelle und Update() bekommt
        /// nie ein halb gefuelltes Model. Liefert null, wenn es den Bezeichner
        /// nicht gibt.
        /// </summary>
        public BHKWStammModel ReadModel(string szBezeichner)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + TABLE + " WHERE Bezeichner = ?",
                new DbParam("@name", szBezeichner ?? ""));
            if (dt == null || dt.Rows.Count == 0) return null;
            return MapRowToModel(dt.Rows[0]);
        }

        // Liefert true, wenn der Datensatz (per ID) schreibgeschuetzt ist.
        public bool IsReadOnly(int ID)
        {
            object val = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + TABLE + " WHERE ID = ?",
                new DbParam("@id", ID));
            return val != null && val != DBNull.Value && Convert.ToBoolean(val);
        }

        // Liefert true, wenn der Datensatz (per Bezeichner) schreibgeschuetzt ist.
        public bool IsReadOnly(string szBezeichner)
        {
            object val = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + TABLE + " WHERE Bezeichner = ?",
                new DbParam("@name", szBezeichner));
            return val != null && val != DBNull.Value && Convert.ToBoolean(val);
        }

        /// <summary>
        /// Hebt den ReadOnly-Schutz fuer genau den naechsten <see cref="Update"/>-Aufruf auf.
        /// Nur setzen, wenn der Anwender das Ueberschreiben eines Katalogsatzes ausdruecklich
        /// bestaetigt hat (siehe Form_DBBHKW); wird danach selbsttaetig zurueckgesetzt.
        /// </summary>
        public bool SchreibschutzUebergehen = false;

        public bool Update()
        {
            // ReadOnly-Schutz: schreibgeschuetzte Stammdatensaetze duerfen nicht geaendert werden.
            // Nur bei Standalone-Aufruf pruefen (kein externer Transaktions-Connection gesetzt),
            // um Sperrkonflikte mit einer bereits laufenden Transaktion zu vermeiden. Bei
            // transaktionalen Neuanlagen ist der Datensatz ohnehin frisch (ReadOnly = false).
            if (!SchreibschutzUebergehen && Vorgang == null && IsReadOnly(model.m_szBezeichner))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden.",
                    "Schreibgeschützt");
                return false;
            }

            // Die Freigabe gilt nur fuer diesen einen Aufruf.
            SchreibschutzUebergehen = false;

            try
            {
                string sql = @"UPDATE " + TABLE + @" SET
                               Beschreibung=?, Firma=?, Motortyp=?, Ptherm=?, Pel=?,
                               Brennstoff=?, Wirkungsgrad=?, Investition_kwel=?, Raumbedarf=?,
                               Wartungskosten_kwhel=?, Nutzungsdauer=?, NOx=?, SO2=?, CO=?,
                               CO2=?, Staub=?, Grenzleistung=?, Kosten_Modul=?, Kosten_Montage=?,
                               Kosten_Lieferung=?, Kosten_Schallschutzhaube=?, Kosten_Abgasreinigung=?,
                               Vorlauf=?, Ruecklauf=?
                               WHERE Bezeichner=?";

                // Die Einzelposten fuehren (Regel in BHKWKosten, Nutzerentscheid
                // 22.08.2026): der spezifische Wert wird hier aus den Posten und Pel
                // abgeleitet. Damit kann kein Schreibweg die beiden Groessen
                // auseinanderlaufen lassen - auch Form_BHKWAdmin nicht, das nur Pel
                // aendert und den vollstaendig gelesenen Satz sonst unveraendert
                // zurueckschreibt. Der Bestand wird dadurch erst beim Speichern
                // angeglichen, nicht schon beim Lesen oder Kopieren.
                model.m_Investition_KWel = BHKWKosten.JeKWel(
                    BHKWKosten.Summe(model.m_Kosten_Modul, model.m_Kosten_Montage,
                                     model.m_Kosten_Lieferung, model.m_Kosten_Schallschutzhaube,
                                     model.m_Kosten_Abgasreinigung),
                    model.m_Pel);

                // ARBEITSPAKET iU6: Die Parametersammlung des DBCommand war hier nur
                // ZWISCHENSPEICHER - unten wurde sie sofort in ein Array kopiert und an
                // die Zugriffsschicht gegeben. Ein OleDbParameter wuerde dafuer heute
                // nur noch auf Nicht-Windows scheitern, also sammelt eine Liste die
                // DbParam direkt. Reihenfolge, Namen und Werte unveraendert; gebunden
                // wird ohnehin nach Position.
                List<DbParam> werte = new List<DbParam>();

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
                werte.Add(new DbParam("@vl", model.m_Vorlauf));
                werte.Add(new DbParam("@rl", model.m_Ruecklauf));
                werte.Add(new DbParam("@key", model.m_szBezeichner ?? ""));

                // ARBEITSPAKET S4b/S4e: Ohne fremden Vorgang laeuft der Schreibvorgang
                // ueber die Zugriffsschicht - die eigene Standalone-Verbindung entfaellt.
                // MIT Vorgang (Transaktion aus Form_DBBHKW) laeuft er auf DESSEN
                // Verbindung und in DESSEN Transaktion.

                if (Vorgang == null)
                {
                    // StilleDb statt DataRepository: Diese Methode meldet ihre Fehler
                    // selbst auf die Konsole (catch unten) und darf keinen Dialog zeigen.
                    if (StilleDb.NonQuery(sql, werte.ToArray()) < 0) return false;
                    return true;
                }

                Vorgang.Ausfuehren(sql, werte.ToArray());

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren des BHKW-Stammsatzes: " + ex.Message);
                return false;
            }
        }

        // Loescht einen Stammdatensatz per Bezeichner, sofern nicht schreibgeschuetzt.
        public bool Delete(string szBezeichner)
        {
            if (IsReadOnly(szBezeichner))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt");
                return false;
            }

            string sql = "DELETE FROM " + TABLE + " WHERE Bezeichner = ?";
            return DataRepository.ExecuteSQL(sql, new DbParam("@name", szBezeichner ?? ""));
        }

        #endregion

        #region --- UI FILL METHODS ---


        #endregion

        #region --- MAPPING HELPERS ---

        private BHKWStammModel MapRowToModel(DataRow row)
        {
            BHKWStammModel m = new BHKWStammModel();
            m.m_ID = row["ID"] != DBNull.Value ? Convert.ToInt32(row["ID"]) : 0;
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
            m.m_bReadOnly = row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);
            m.m_Vorlauf = row["Vorlauf"] != DBNull.Value ? Convert.ToInt32(row["Vorlauf"]) : 0;
            m.m_Ruecklauf = row["Ruecklauf"] != DBNull.Value ? Convert.ToInt32(row["Ruecklauf"]) : 0;

            return m;
        }

        private void MapThisToRow(DataRow row)
        {
            BHKWStammModel m = MapRowToModel(row);
            this.m_ID = m.m_ID;
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
            this.m_Vorlauf = m.m_Vorlauf;
            this.m_Ruecklauf = m.m_Ruecklauf;
            this.m_bReadOnly = m.m_bReadOnly;
        }

        #endregion

        // =================================================================================
        // W6.0c - der KATALOGFILTER des Projektdialogs
        // =================================================================================

        /// <summary>
        /// Eine Zeile der Katalogliste. Der Vorlaeufer <c>Form_BHKWEing</c> zeigte sie in
        /// einem <c>DataGridView</c> mit zwei Spalten: „Name" und ein Mehrzeiler
        /// „Eigenschaften" aus Firma, Brennstoff, Ptherm und Pel.
        /// </summary>
        /// <param name="Id">Primaerschluessel - er ersetzt die Namenssuche des Vorlaeufers.</param>
        /// <param name="Bezeichner">Spalte „Name".</param>
        /// <param name="Firma">Erste Zeile der Spalte „Eigenschaften".</param>
        /// <param name="Brennstoff">
        /// Anzeigename des Brennstoffs aus <see cref="Brennstoffart"/>; leer, wenn die
        /// gespeicherte Nummer ausserhalb der Liste liegt - Bestandsverhalten.
        /// </param>
        /// <param name="Ptherm">Thermische Leistung [kW].</param>
        /// <param name="Pel">Elektrische Leistung [kW].</param>
        public sealed record KatalogZeile(int Id, string Bezeichner, string Firma,
                                          string Brennstoff, double Ptherm, double Pel);

        /// <summary>
        /// Die Katalogliste des Projektdialogs, eingeengt auf Brennstoffgruppe und
        /// Leistungsstufe.
        /// </summary>
        /// <param name="gruppe">
        /// Eintrag aus <see cref="Brennstoffart_Gruppe"/>. Leer, <c>null</c>, „Alle" und
        /// jeder unbekannte Wert heben die Einengung auf - Bestandsverhalten.
        /// </param>
        /// <param name="leistungsstufe">
        /// Index in <see cref="LeistungFilterText"/>: 0 = „Alle", 1..8 die acht Stufen aus
        /// <see cref="LeistungText"/>. Alles ausserhalb gilt als 0.
        /// </param>
        /// <remarks>
        /// <para>
        /// Die Gruppenkette ist WORTGLEICH aus <c>Form_BHKWEing.BuildFilter</c> (Z. 168-186)
        /// uebernommen, samt der doppelten Zeile fuer „Tierische Fette" (die zweite war
        /// schon dort unerreichbar). Anders als die Heizkesselkette bildet sie alle zwoelf
        /// Gruppen von <c>Tab_BrennstoffKategorien</c> ab; der Unterschied zwischen beiden
        /// ist Befund W6-O-1 des Protokolls.
        /// </para>
        /// <para>
        /// <b>Abweichung mit Grund (A-6).</b> Die Leistungsstufe kommt jetzt ueber den
        /// INDEX aus <see cref="LeistungFilterText"/> statt ueber einen Textvergleich. Im
        /// Bestand fuellte <c>SetControls</c> die Liste aus <see cref="LeistungText"/> -
        /// letzter Eintrag „größer 1200 kW" -, waehrend <c>BuildFilter</c> gegen
        /// „über 1.200 kW" verglich: Die achte Stufe traf NIE und zeigte still alle
        /// Leistungen. Ueber den Index ist sie erreichbar. Dieselbe Umstellung wie in
        /// Paket 9 fuer den Pufferspeicher (B0-10).
        /// </para>
        /// </remarks>
        public IReadOnlyList<KatalogZeile> Filtern(string gruppe, int leistungsstufe)
        {
            if (leistungsstufe < 0 || leistungsstufe >= LeistungFilterText.Length) leistungsstufe = 0;
            string szFilterLeistung = LeistungFilterText[leistungsstufe];

            string szFilter = "";
            string g = gruppe ?? "";
            if (g == "Gas") szFilter = "(Brennstoff >=1 and Brennstoff <=5) or Brennstoff=14";
            else if (g == "Öl") szFilter = "(Brennstoff >=6 and Brennstoff <=9) or (Brennstoff >=18 and Brennstoff <=22)";
            else if (g == "Koks") szFilter = "Brennstoff=10";
            else if (g == "Kohle") szFilter = "Brennstoff=11";
            else if (g == "Holz") szFilter = "Brennstoff=12";
            else if (g == "Tierische Fette") szFilter = "Brennstoff=17";
            else if (g == "Strom") szFilter = "Brennstoff=13";
            else if (g == "Pellets") szFilter = "Brennstoff=15";
            else if (g == "Rapsöl") szFilter = "Brennstoff=16";
            else if (g == "Fernwärme") szFilter = "Brennstoff=23";
            else if (g == "Sonstige Energieträger") szFilter = "Brennstoff=24";
            else if (g == "Wasserstoff") szFilter = "Brennstoff=25";
            else if (g == "Alle") szFilter = "Brennstoff Like '%'";

            string szWhere = szFilter == "" ? szFilterLeistung
                                            : "(" + szFilter + ") and " + szFilterLeistung;
            string sql = "SELECT * FROM " + TABLE + " WHERE " + szWhere + " ORDER BY Bezeichner";

            var liste = new List<KatalogZeile>();
            DataTable dt = DataRepository.GetDataTable(sql);
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
            {
                if (row["ID"] == null || row["ID"] == DBNull.Value) continue;

                int brennIdx = row["Brennstoff"] != DBNull.Value ? Convert.ToInt32(row["Brennstoff"]) : 0;
                string brennText = (brennIdx >= 1 && brennIdx <= Brennstoffart.Count)
                                 ? Brennstoffart[brennIdx - 1] : "";

                liste.Add(new KatalogZeile(
                    Convert.ToInt32(row["ID"]),
                    row["Bezeichner"] == DBNull.Value ? "" : row["Bezeichner"].ToString(),
                    row["Firma"] == DBNull.Value ? "" : row["Firma"].ToString(),
                    brennText,
                    row["Ptherm"] == DBNull.Value ? 0 : Convert.ToDouble(row["Ptherm"]),
                    row["Pel"] == DBNull.Value ? 0 : Convert.ToDouble(row["Pel"])));
            }
            return liste;
        }

        /// <summary>
        /// Der Primaerschluessel zum Bezeichner, 0 wenn es keinen gibt - Ersatz fuer
        /// <c>DataRepository.GetIdByName</c> in den Aufrufern.
        /// </summary>
        public static int IdZu(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ID FROM " + TABLE + " WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@nam", szBezeichner ?? ""));
            return (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
        }

        // =================================================================================
        // W6.2 - die beiden Schreibeinstiege des Katalogeditors
        // =================================================================================

        /// <summary>
        /// Was ein Speicherversuch des Katalogeditors ergeben hat - Gegenstueck zu
        /// <c>HeizkesselStammCtrl.SpeicherErgebnis</c>.
        /// </summary>
        /// <param name="Ok">Wurde geschrieben?</param>
        /// <param name="Meldung">Der Grund im Klartext, bereits lokalisiert.</param>
        /// <param name="Name">Der Bezeichner, unter dem der Satz jetzt steht.</param>
        public sealed record SpeicherErgebnis(bool Ok, string Meldung, string Name);

        /// <summary>
        /// Schreibt den geladenen Katalogsatz zurueck - der Weg des Knopfes
        /// „Überschreiben" (<c>Form_DBBHKW.btn_Überschreiben_Click</c>, Z. 255).
        /// </summary>
        /// <param name="daten">Der Feldsatz aus der Maske.</param>
        /// <param name="schreibschutzUebergehen">
        /// <c>true</c> hebt den ReadOnly-Schutz fuer GENAU diesen Schreibvorgang auf. Der
        /// Vorlaeufer setzte das nach einer ausdruecklichen Ja/Nein-Rueckfrage; die
        /// Rueckfrage selbst steht jetzt in der Komponente (<c>Rueckfrage</c>-Baustein),
        /// die Antwort kommt hier an.
        /// </param>
        public static SpeicherErgebnis Ueberschreiben(BHKWStammModel daten, bool schreibschutzUebergehen)
        {
            if (daten == null)
                return new SpeicherErgebnis(false, Text("BHKWK_MSG_FEHLER",
                    "Fehler beim Überschreiben des Datensatzes!"), "");

            try
            {
                var ctrl = new BHKWStammCtrl { model = daten, SchreibschutzUebergehen = schreibschutzUebergehen };

                // Ohne diese Freigabe prueft Update() selbst erneut auf ReadOnly. Es
                // meldet den Grund ueber Meldung.* - hier zaehlt nur, ob geschrieben
                // wurde; die Oberflaeche sagt es danach.
                if (!ctrl.Update())
                    return new SpeicherErgebnis(false, Text("BHKWK_MSG_NICHT_GESCHRIEBEN",
                        "Der Datensatz konnte nicht überschrieben werden."), "");

                return new SpeicherErgebnis(true,
                    Text("BHKWK_MSG_GESPEICHERT", "Datensatz gespeichert"), daten.m_szBezeichner);
            }
            catch
            {
                return new SpeicherErgebnis(false, Text("BHKWK_MSG_FEHLER",
                    "Fehler beim Überschreiben des Datensatzes!"), "");
            }
        }

        /// <summary>
        /// Legt einen neuen Katalogsatz an - der Weg der Knoepfe „Speichern" (Modus NEU)
        /// und „Speichern unter".
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Eine Transaktion, ein Ort.</b> Der Vorlaeufer trug die Anlage ZWEIMAL:
        /// <c>btn_Speichern_Unter_Click</c> (Z. 403, Existenzpruefung ueber ein
        /// zusammengesetztes <c>RecordSet</c>-SQL) und <c>btn_Speichern_Click</c>
        /// (Z. 483, dieselbe Pruefung als parametrisiertes <c>COUNT(*)</c>). Beide
        /// legten den Satz mit <c>INSERT INTO Tab_BHKW_STAMM (Bezeichner, ReadOnly)</c>
        /// an und fuellten ihn dann ueber <see cref="Update"/> im selben
        /// <see cref="DbVorgang"/>. Hier steht das einmal - mit der parametrisierten
        /// Pruefung, die auch einen Namen mit Hochkomma vertraegt.
        /// </para>
        /// <para>
        /// <c>ReadOnly = false</c> ist Pflicht: Die Spalte ist NOT NULL, und ein neu
        /// angelegter Satz gehoert nie zur Auslieferung.
        /// </para>
        /// </remarks>
        public static SpeicherErgebnis Anlegen(BHKWStammModel daten, string name)
        {
            if (daten == null || string.IsNullOrWhiteSpace(name))
                return new SpeicherErgebnis(false, Text("BHKWK_MSG_NAME_FEHLT",
                    "Bitte einen gültigen Namen eingeben!"), "");

            string bezeichner = name.Trim();

            try
            {
                // 1./2. ARBEITSPAKET S4e: Verbindung UND Transaktion sind EIN
                // Datenbankvorgang. Ohne Commit rollt sein Dispose beim Verlassen zurueck.
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    // Existenzpruefung IM Vorgang, damit sie die noch nicht
                    // festgeschriebenen Zeilen sieht.
                    int vorhanden = Convert.ToInt32(v.Skalar(
                        "SELECT COUNT(*) FROM " + TABLE + " WHERE Bezeichner = ?",
                        new DbParam("@nam", bezeichner)));
                    if (vorhanden > 0)
                    {
                        v.Rollback();
                        return new SpeicherErgebnis(false, Text("BHKWK_MSG_NAME_BELEGT",
                            "Name existiert bereits!"), "");
                    }

                    // INSERT inkl. ReadOnly = false (Feld ist NOT NULL).
                    v.Ausfuehren("INSERT INTO " + TABLE + " (Bezeichner, ReadOnly) VALUES (?, ?)",
                                 new DbParam("@nam", bezeichner),
                                 new DbParam("@ro", false));

                    daten.m_szBezeichner = bezeichner;
                    var ctrl = new BHKWStammCtrl { model = daten, Vorgang = v };

                    if (!ctrl.Update())
                    {
                        v.Rollback();
                        return new SpeicherErgebnis(false, Text("BHKWK_MSG_FEHLER_ANLEGEN",
                            "Fehler beim Speichern des Datensatzes!"), "");
                    }

                    v.Commit();
                    return new SpeicherErgebnis(true,
                        Text("BHKWK_MSG_GESPEICHERT", "Datensatz gespeichert"), bezeichner);
                }
            }
            catch
            {
                // Zurueckgerollt hat bereits DbVorgang.Dispose beim Verlassen des using.
                return new SpeicherErgebnis(false, Text("BHKWK_MSG_FEHLER_ANLEGEN",
                    "Fehler beim Speichern des Datensatzes!"), "");
            }
        }

        /// <summary>
        /// Alle Katalognamen in Anzeigereihenfolge - die Auswahlliste
        /// <c>comboBox_Name</c> des Editors (<c>FillComboBox</c>).
        /// </summary>
        public static IReadOnlyList<string> Namen()
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT Bezeichner FROM " + TABLE + " ORDER BY Bezeichner");
            if (dt == null) return liste;

            foreach (DataRow row in dt.Rows)
                liste.Add(row["Bezeichner"] == DBNull.Value ? "" : row["Bezeichner"].ToString());
            return liste;
        }

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        // =================================================================================
        // W14a.0c - der Detailblock des Katalogbrowsers
        // =================================================================================

        /// <summary>
        /// Die acht Anzeigefelder eines Katalogsatzes, bereits als Text — der Detailblock
        /// von <c>Form_BHKWAdmin.FillDetails</c> (Z. 111-138). <c>null</c>, wenn es den
        /// Bezeichner nicht gibt.
        /// </summary>
        /// <remarks>
        /// Der Vorlaeufer war als einziger der vier Browser bereits parametrisiert
        /// (<c>DbParam</c>, Z. 113-115); der Wortlaut ist unveraendert uebernommen,
        /// einschliesslich der ROHEN Zahlenanzeige ohne Format (Z. 126-130) — anders als
        /// beim Heizkessel, der <c>F2</c> nimmt. Die Schluessel sind die Feldschluessel
        /// aus <see cref="KatalogBrowserProfil"/>.
        /// </remarks>
        public static IReadOnlyDictionary<string, string> KatalogsatzAnzeige(string szName)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + TABLE + " WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@name", szName ?? ""));
            if (dt == null || dt.Rows.Count == 0) return null;

            DataRow r = dt.Rows[0];
            var werte = new Dictionary<string, string>(StringComparer.Ordinal);

            werte[KatalogBrowserProfil.FeldBezeichner] = Feld(r, "Bezeichner");
            werte[KatalogBrowserProfil.FeldFirma] = Feld(r, "Firma");
            werte[KatalogBrowserProfil.FeldBeschreibung] = Feld(r, "Beschreibung");
            werte[KatalogBrowserProfil.FeldPtherm] = Feld(r, "Ptherm");
            werte[KatalogBrowserProfil.FeldPel] = Feld(r, "Pel");
            werte[KatalogBrowserProfil.FeldGrenzleistung] = Feld(r, "Grenzleistung");
            werte[KatalogBrowserProfil.FeldVorlauf] = Feld(r, "Vorlauf");
            werte[KatalogBrowserProfil.FeldRuecklauf] = Feld(r, "Ruecklauf");

            return werte;
        }

        /// <summary>Feldwert als Text; fehlende Spalte und <c>NULL</c> ergeben „".</summary>
        private static string Feld(DataRow row, string spalte)
        {
            if (!row.Table.Columns.Contains(spalte)) return "";
            object v = row[spalte];
            return (v == null || v == DBNull.Value) ? "" : v.ToString();
        }

        /// <summary>
        /// Traegt der Katalogsatz den Schreibschutz der Auslieferung? Der Katalogbrowser
        /// fragt danach, bevor er ueberschreibt — in der Auslieferungsdatenbank sind ALLE
        /// Saetze von <c>Tab_BHKW_STAMM</c> geschuetzt, die Rueckfrage ist dort also der
        /// Regelfall (<c>Form_BHKWAdmin.cs:413-417</c>).
        /// </summary>
        public static bool IstSchreibgeschuetzt(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM " + TABLE + " WHERE Bezeichner = ? ORDER BY ID",
                new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // =================================================================================
        // W14a.0c - der Speicherweg des Katalogbrowsers
        // =================================================================================

        /// <summary>
        /// Die SECHS Felder, die der Katalogbrowser zurueckschreibt
        /// (<c>Form_BHKWAdmin.Speicherfelder</c> Z. 338-345).
        /// </summary>
        public sealed record AnzeigefelderBhkw(string Firma, double Ptherm, double Pel,
                                               double Grenzleistung, int Vorlauf, int Ruecklauf);

        /// <summary>
        /// Schreibt die sechs Anzeigefelder in den Katalogsatz zurueck — der Weg des
        /// Knopfes „Speichern" im Browser.
        /// </summary>
        /// <remarks>
        /// <para>Woertlich aus <c>Form_BHKWAdmin.SpeichereStammsatz</c> (Z. 385-438): Der
        /// Satz wird VOLLSTAENDIG gelesen und nur in den angezeigten Feldern geaendert,
        /// weil <see cref="Update"/> alle Spalten schreibt und ein halb gefuelltes Modell
        /// Kosten, Emissionen und Wirkungsgrad nullen wuerde.</para>
        /// <para><paramref name="schreibschutzUebergehen"/> ist die Antwort auf die
        /// Rueckfrage <c>ADM_SCHUTZ_FRAGE</c>, die der Aufrufer stellt, wenn
        /// <see cref="IstSchreibgeschuetzt"/> zutrifft. Der Schutz wird nur fuer genau
        /// diesen Schreibvorgang aufgehoben — dieselbe Regel wie beim Knopf
        /// „Überschreiben" des Katalogeditors.</para>
        /// </remarks>
        public static SpeicherErgebnis AnzeigefelderSchreiben(string bezeichner,
                                                              AnzeigefelderBhkw felder,
                                                              bool schreibschutzUebergehen)
        {
            if (string.IsNullOrEmpty(bezeichner) || felder == null)
                return new SpeicherErgebnis(false, Text("BHKWK_MSG_FEHLER",
                    "Fehler beim Überschreiben des Datensatzes!"), "");

            try
            {
                var leser = new BHKWStammCtrl();
                BHKWStammModel m = leser.ReadModel(bezeichner);
                if (m == null)
                    return new SpeicherErgebnis(false, Text("BHKWK_MSG_FEHLER",
                        "Fehler beim Überschreiben des Datensatzes!"), "");

                m.m_szFirma = felder.Firma ?? "";
                m.m_Ptherm = felder.Ptherm;
                m.m_Pel = felder.Pel;
                m.m_Grenzleistung = felder.Grenzleistung;
                m.m_Vorlauf = felder.Vorlauf;
                m.m_Ruecklauf = felder.Ruecklauf;

                var schreiber = new BHKWStammCtrl { model = m };
                if (m.m_bReadOnly)
                {
                    if (!schreibschutzUebergehen)
                        return new SpeicherErgebnis(false, Text("BHKWK_MSG_SCHUTZ",
                            "Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gespeichert werden."), "");
                    schreiber.SchreibschutzUebergehen = true;
                }

                if (!schreiber.Update())
                    return new SpeicherErgebnis(false, Text("BHKWK_MSG_FEHLER",
                        "Fehler beim Überschreiben des Datensatzes!"), "");

                return new SpeicherErgebnis(true,
                    Text("BHKWK_MSG_GESPEICHERT", "Datensatz gespeichert"), bezeichner);
            }
            catch
            {
                return new SpeicherErgebnis(false, Text("BHKWK_MSG_FEHLER",
                    "Fehler beim Überschreiben des Datensatzes!"), "");
            }
        }
    }
}
