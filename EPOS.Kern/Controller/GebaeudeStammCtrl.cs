using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApplication1
{
    // Controller fuer die Gebaeude-STAMMDATEN (Tab_Gebaeude_STAMM).
    // Katalog: Schluessel = ID, Namensfeld = Bezeichner (im Model als Gebaeudename gefuehrt).
    // Neues Feld ReadOnly. Enthaelt Katalog-Lesen und die Admin-Operationen (Loeschen mit Schutz).
    // Die Kopierlogik STAMM -> Projekt folgt in Etappe 2.
    class GebaeudeStammCtrl : GebaeudeModel
    {
        public const string TABLE      = "Tab_Gebaeude_STAMM";
        public const string TABLE_PROJ = "Tab_Gebaeude";

        private List<GebaeudeModel> _internalList = new List<GebaeudeModel>();
        public int rows => _internalList.Count;
        public List<GebaeudeModel> items => _internalList;

        public bool m_bReadOnly = false;

        public GebaeudeStammCtrl() { }

        #region --- READ (Katalog) ---

        public void ReadAll(string szFilter = "")
        {
            string sql = "SELECT * FROM [" + TABLE + "]";
            if (!string.IsNullOrEmpty(szFilter)) sql += " WHERE " + szFilter;
            sql += " ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql);
            _internalList.Clear();
            if (dt == null) return;
            foreach (DataRow row in dt.Rows)
            {
                GebaeudeModel item = new GebaeudeModel();
                FillModel(item, row);
                _internalList.Add(item);
            }
        }

        public void ReadSingle(string szBezeichner)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? (object)DBNull.Value));
            _internalList.Clear();
            if (dt != null && dt.Rows.Count > 0)
            {
                FillModel(this, dt.Rows[0]);
                _internalList.Add(this);
            }
        }

        /// <summary>
        /// Liest EINEN Katalogsatz nach seinem Bezeichner in ein frisches Modell;
        /// <c>null</c>, wenn es ihn nicht gibt (Schreibweg des Gebäudedialogs, Stufe G1).
        /// </summary>
        public GebaeudeModel Lies(string szBezeichner)
        {
            if (string.IsNullOrEmpty(szBezeichner)) return null;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner));
            if (dt == null || dt.Rows.Count == 0) return null;
            var item = new GebaeudeModel();
            FillModel(item, dt.Rows[0]);
            return item;
        }

        public bool IsReadOnly(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        // Loescht einen Gebaeude-Stammdatensatz (per Bezeichner), sofern nicht schreibgeschuetzt.
        public bool Delete(string szBezeichner)
        {
            if (IsReadOnly(szBezeichner))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt");
                return false;
            }
            return DataRepository.ExecuteSQL("DELETE FROM [" + TABLE + "] WHERE Bezeichner = ?",
                new DbParam("@bez", szBezeichner ?? ""));
        }

        #endregion

        #region --- W9.0b: Listen, Filter und Ableitungen der Gebaeudemasken ---

        // Die beiden Steuerwerte der Spalte Wohngebaeude_Nicht_Wohngebaeude. Sie stehen
        // woertlich so in Form_Gebaeude:15/16 und gehen in jeden Filter; sie sind
        // Datenbankinhalt und werden NIE uebersetzt.
        public const string FILTER_WOHNGEBAEUDE       = "Wohngebaeude_Nicht_Wohngebaeude='Wohngebaeude'";
        public const string FILTER_NICHT_WOHNGEBAEUDE = "Wohngebaeude_Nicht_Wohngebaeude='Nicht Wohngebaeude'";

        /// <summary>
        /// Die 21 Baualtersklassen in ihrer festen Reihenfolge — Index 0 ist <c>'A'</c>,
        /// Index 20 ist <c>'U'</c> (<c>Form_Gebaeude.list_geb</c>:14 und
        /// <c>Form_Gebaeude1.list_geb</c>:9, dort zweimal wortgleich).
        ///
        /// <para>Die TEXTE kommen aus dem Ressourcenkatalog (<c>GEB_BAK_A</c>…
        /// <c>GEB_BAK_U</c>), die ABBILDUNG auf den Buchstaben steht hier: Der Buchstabe
        /// geht in die Datenbank (<c>Baualtersklasse</c>), der Text nur auf den
        /// Bildschirm. Ginge die Abbildung ueber den Text, verschoebe eine Uebersetzung
        /// die gespeicherte Klasse.</para>
        /// </summary>
        public static IReadOnlyList<string> Baualtersklassen()
        {
            var liste = new List<string>(BAUALTERSKLASSEN_DE.Length);
            for (int i = 0; i < BAUALTERSKLASSEN_DE.Length; i++)
            {
                string schluessel = "GEB_BAK_" + (char)('A' + i);
                string text = null;
                try { text = MyResource.Resource.ResourceManager.GetString(schluessel); }
                catch { }
                liste.Add(string.IsNullOrEmpty(text) ? BAUALTERSKLASSEN_DE[i] : text);
            }
            return liste;
        }

        /// <summary>Die deutschen Bestandstexte — der Rueckfall, solange ein Schluessel fehlt.</summary>
        public static readonly string[] BAUALTERSKLASSEN_DE =
        {
            "vor 1919", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978",
            "1979 bis 1983", "1984 bis 1994", "1995 bis 2000", "Niedrigenergiebauweise",
            "Passivhaus", "EnEv 2007", "Eff. 70 (EnEV 2007)", "EnEV 2009",
            "Eff. 70 (EnEV 2009)", "Eff. 55 (EnEV 2009)", "EnEV 2014", "EnEV 2016",
            "Eff. 100 (EnEV 2016)", "Eff. 155 (EnEV 2016)", "BEG 55", "BEG 40"
        };

        /// <summary>
        /// Der Buchstabe zu einem Listenindex: <c>'A' + index</c>
        /// (<c>InitModelFromControls</c>:212). Ein Index ausserhalb der Liste faellt auf
        /// <c>'A'</c> zurueck — genauso wie der Rueckweg.
        /// </summary>
        public static char KlassenBuchstabe(int index)
        {
            if (index < 0 || index >= BAUALTERSKLASSEN_DE.Length) return 'A';
            return (char)('A' + index);
        }

        /// <summary>
        /// Der Listenindex zu einer gespeicherten Baualtersklasse — das erste Zeichen
        /// minus <c>'A'</c>, negativ wird 0 (<c>Form_Gebaeude1.SetControls</c>:102-104 und
        /// <c>Form_Gebaeude.btn_Aendern_Click</c>:430-432, dort wortgleich).
        /// </summary>
        public static int KlassenIndex(string baualtersklasse)
        {
            if (string.IsNullOrEmpty(baualtersklasse)) return 0;
            int index = baualtersklasse[0] - 'A';
            if (index < 0) return 0;
            if (index >= BAUALTERSKLASSEN_DE.Length) return 0;
            return index;
        }

        /// <summary>
        /// Die Gebaeudearten aus der Sicht <c>Abfrage_Gebaeudearten</c>.
        /// <paramref name="wohngebaeude"/>: <c>true</c> nur Wohngebaeude,
        /// <c>false</c> nur Nichtwohngebaeude, <c>null</c> alle (so laedt sie
        /// <c>Form_Gebaeude1.SetControls</c>:88 ohne Filter).
        /// </summary>
        public static IReadOnlyList<string> Gebaeudearten(bool? wohngebaeude)
        {
            string sql = "SELECT * from Abfrage_Gebaeudearten";
            if (wohngebaeude.HasValue)
                sql += " where " + (wohngebaeude.Value ? FILTER_WOHNGEBAEUDE : FILTER_NICHT_WOHNGEBAEUDE);

            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable(sql);
            if (dt == null) return liste;
            foreach (DataRow row in dt.Rows)
                if (row["Gebaeudeart"] != DBNull.Value) liste.Add(row["Gebaeudeart"].ToString());
            return liste;
        }

        /// <summary>
        /// Die Gebaeudetypen aus der Sicht <c>Abfrage_Gebaeudetypen</c> — die Klappliste
        /// „Gebaeudetyp" des Katalogeditors (<c>Form_Gebaeude1.SetControls</c>:70).
        /// </summary>
        public static IReadOnlyList<string> Gebaeudetypen()
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable("SELECT * from Abfrage_Gebaeudetypen");
            if (dt == null) return liste;
            foreach (DataRow row in dt.Rows)
                if (row["Typ"] != DBNull.Value) liste.Add(row["Typ"].ToString());
            return liste;
        }

        /// <summary>
        /// Die Namen ALLER Katalogsaetze — die Klappliste des Admin-Modus
        /// (<c>Form_Gebaeude1_Load</c>:34).
        /// </summary>
        public static IReadOnlyList<string> Katalognamen()
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable("SELECT * from " + TABLE);
            if (dt == null) return liste;
            foreach (DataRow row in dt.Rows)
                if (row["Bezeichner"] != DBNull.Value) liste.Add(row["Bezeichner"].ToString());
            return liste;
        }

        /// <summary>
        /// Der KATALOGFILTER der Gebaeudeverwaltung — die vier SQL-Zweige aus
        /// <c>Form_Gebaeude.comboBox_Gebaeudeart_SelectedIndexChanged</c>:329-374 und
        /// <c>comboBox_Baujahr_SelectedIndexChanged</c>:376-419.
        ///
        /// <para><b>Befund W9-B1 — die beiden Handler sind NICHT gleich.</b> Im Zweig
        /// „Gebaeudeart gewaehlt, Baujahr Alle" filtert der Gebaeudeart-Handler NUR nach
        /// <c>Gebaeudeart</c> (:359), der Baujahr-Handler zusaetzlich nach der Verwendung
        /// (:392). Welche Liste erscheint, haengt also davon ab, welche Klappliste der
        /// Anwender zuletzt angefasst hat. Das ist woertlich uebernommen (Regel F3) und
        /// steckt in <paramref name="ausBaujahrwahl"/>; der Anwender entscheidet, ob es so
        /// bleibt.</para>
        /// </summary>
        /// <param name="wohngebaeude"><c>true</c> = Wohngebaeude, <c>false</c> = Sonstige.</param>
        /// <param name="gebaeudeart">Gewaehlte Gebaeudeart; <c>null</c> oder leer = „Alle".</param>
        /// <param name="klassenIndex">Index der Baualtersklasse; <c>null</c> = „Alle".</param>
        /// <param name="ausBaujahrwahl">
        /// <c>true</c>, wenn die BAUJAHR-Klappliste die Auswahl ausgeloest hat.
        /// </param>
        public static string FilterAusdruck(bool wohngebaeude, string gebaeudeart,
                                            int? klassenIndex, bool ausBaujahrwahl)
        {
            string option = wohngebaeude ? FILTER_WOHNGEBAEUDE : FILTER_NICHT_WOHNGEBAEUDE;

            bool arteAlle = string.IsNullOrEmpty(gebaeudeart);
            bool jahrAlle = !klassenIndex.HasValue;

            if (arteAlle && jahrAlle) return option;
            if (arteAlle)
                return "Baualtersklasse='" + KlassenBuchstabe(klassenIndex.Value) + "' and " + option;
            if (jahrAlle)
                return ausBaujahrwahl
                    ? "Gebaeudeart='" + gebaeudeart + "' and " + option   // :392
                    : "Gebaeudeart='" + gebaeudeart + "'";                // :359  (Befund W9-B1)

            return "Gebaeudeart='" + gebaeudeart + "' and Baualtersklasse='" +
                   KlassenBuchstabe(klassenIndex.Value) + "' and " + option;
        }

        /// <summary>
        /// Liest den gefilterten Katalog. Derselbe Weg wie <c>ReadAll(filter)</c>, nur mit
        /// dem Ausdruck aus <see cref="FilterAusdruck"/>.
        /// </summary>
        public IReadOnlyList<GebaeudeModel> Filtern(bool wohngebaeude, string gebaeudeart,
                                                    int? klassenIndex, bool ausBaujahrwahl)
        {
            ReadAll(FilterAusdruck(wohngebaeude, gebaeudeart, klassenIndex, ausBaujahrwahl));
            return _internalList;
        }

        /// <summary>
        /// Die BAUART aus der gespeicherten Bauweise — <c>Form_Gebaeude1.SetControls</c>
        /// :107-110. 0 = leicht (&lt; 30), 1 = schwer, 2 = sehr schwer (&gt; 75).
        ///
        /// <para>Die Rechnung steht seit dem Entscheid W9-O-2 (04.09.2026) in der
        /// oeffentlichen Hilfsklasse <see cref="Gebaeudebauweise"/>: Der
        /// Katalogeditor in <c>EPOS.UI</c> braucht sie selbst, und dieser Controller
        /// ist <c>internal</c>. Hier bleibt der Name aus W9.0b stehen.</para>
        /// </summary>
        public static int BauartAusBauweise(double bauweise, double wohnflaeche)
            => Gebaeudebauweise.BauartAusBauweise(bauweise, wohnflaeche);

        /// <summary>
        /// Der Rueckweg — <c>InitModelFromControls</c>:188-191. Index 0/1/2 ergeben
        /// Wohnflaeche × 20 / 50 / 100, jeder andere Index ergibt 50.
        ///
        /// <para><b>Befund W9-B6, Entscheid W9-O-2 (Anwender, 04.09.2026).</b> Der
        /// Vorlaeufer nahm hier den Index der <b>Gebaeudeart</b>-Klappliste
        /// (<c>comboBox_Gebaeudeart.SelectedIndex</c>) und NICHT den der
        /// Bauart-Klappliste, obwohl die Bauart aus derselben Groesse abgeleitet
        /// angezeigt wurde. Der Anwender hat entschieden: Die BAUART bestimmt die
        /// Bauweise. <c>GebaeudeKatalogDialog</c> bildet sie deshalb aus der
        /// BAUART-Auswahl; <see cref="BauartAusBauweise"/> ist der Rueckweg beim
        /// Laden. Die Rechnung selbst ist unveraendert und steht jetzt in
        /// <see cref="Gebaeudebauweise"/>.</para>
        /// </summary>
        public static double BauweiseAusBauart(int index, double wohnflaeche)
            => Gebaeudebauweise.BauweiseAusBauart(index, wohnflaeche);

        #endregion

        #region --- MAPPING (namensbasiert) ---

        private void FillModel(GebaeudeModel item, DataRow row)
        {
            DataTable dt = row.Table;
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.ID = Convert.ToInt32(row["ID"]);
            if (dt.Columns.Contains("Bezeichner") && row["Bezeichner"] != DBNull.Value) item.Gebaeudename = row["Bezeichner"].ToString();
            else if (dt.Columns.Contains("Gebaeudename") && row["Gebaeudename"] != DBNull.Value) item.Gebaeudename = row["Gebaeudename"].ToString();
            if (dt.Columns.Contains("Typ") && row["Typ"] != DBNull.Value) item.Typ = row["Typ"].ToString();
            if (dt.Columns.Contains("Beschreibung") && row["Beschreibung"] != DBNull.Value) item.Beschreibung = row["Beschreibung"].ToString();
            if (dt.Columns.Contains("Wohnflaeche_gesamt") && row["Wohnflaeche_gesamt"] != DBNull.Value) item.Wohnflaeche_gesamt = Convert.ToDouble(row["Wohnflaeche_gesamt"]);
            if (dt.Columns.Contains("Bewohner") && row["Bewohner"] != DBNull.Value) item.Bewohner = Convert.ToDouble(row["Bewohner"]);
            if (dt.Columns.Contains("Flaeche_Nutzer") && row["Flaeche_Nutzer"] != DBNull.Value) item.Flaeche_Nutzer = Convert.ToDouble(row["Flaeche_Nutzer"]);
            if (dt.Columns.Contains("Interne_Waermegewinne") && row["Interne_Waermegewinne"] != DBNull.Value) item.Interne_Waermegewinne = Convert.ToDouble(row["Interne_Waermegewinne"]);
            if (dt.Columns.Contains("Bauweise") && row["Bauweise"] != DBNull.Value) item.Bauweise = Convert.ToDouble(row["Bauweise"]);
            if (dt.Columns.Contains("Fensterflaeche_Sued") && row["Fensterflaeche_Sued"] != DBNull.Value) item.Fensterflaeche_Sued = Convert.ToDouble(row["Fensterflaeche_Sued"]);
            if (dt.Columns.Contains("Fensterflaeche_Ost_West") && row["Fensterflaeche_Ost_West"] != DBNull.Value) item.Fensterflaeche_OstWest = Convert.ToDouble(row["Fensterflaeche_Ost_West"]);
            if (dt.Columns.Contains("Fensterflaeche_Nord") && row["Fensterflaeche_Nord"] != DBNull.Value) item.Fensterflaeche_Nord = Convert.ToDouble(row["Fensterflaeche_Nord"]);
            if (dt.Columns.Contains("Fensterdurchlassgrad") && row["Fensterdurchlassgrad"] != DBNull.Value) item.Fensterdurchlassgrad = Convert.ToDouble(row["Fensterdurchlassgrad"]);
            if (dt.Columns.Contains("Raumsolltemperatur_Nachtabsenkung") && row["Raumsolltemperatur_Nachtabsenkung"] != DBNull.Value) item.Raumsolltemperatur_Nachtabsenkung = Convert.ToDouble(row["Raumsolltemperatur_Nachtabsenkung"]);
            if (dt.Columns.Contains("Raumsolltemperatur_Tag") && row["Raumsolltemperatur_Tag"] != DBNull.Value) item.Raumsolltemperatur_Tag = Convert.ToDouble(row["Raumsolltemperatur_Tag"]);
            if (dt.Columns.Contains("Raumsolltemperatur_Wochenende") && row["Raumsolltemperatur_Wochenende"] != DBNull.Value) item.Raumsolltemperatur_Wochenende = Convert.ToDouble(row["Raumsolltemperatur_Wochenende"]);
            if (dt.Columns.Contains("Raumsolltemperatur_Ferien") && row["Raumsolltemperatur_Ferien"] != DBNull.Value) item.Raumsolltemperatur_Ferien = Convert.ToDouble(row["Raumsolltemperatur_Ferien"]);
            if (dt.Columns.Contains("Maximaleraumtemperatur") && row["Maximaleraumtemperatur"] != DBNull.Value) item.Maximaleraumtemperatur = Convert.ToDouble(row["Maximaleraumtemperatur"]);
            if (dt.Columns.Contains("k_Wert_Außenwand") && row["k_Wert_Außenwand"] != DBNull.Value) item.k_Wert_Außenwand = Convert.ToDouble(row["k_Wert_Außenwand"]);
            if (dt.Columns.Contains("k_Wert_Fenster") && row["k_Wert_Fenster"] != DBNull.Value) item.k_Wert_Fenster = Convert.ToDouble(row["k_Wert_Fenster"]);
            if (dt.Columns.Contains("k_Wert_Dachflaeche") && row["k_Wert_Dachflaeche"] != DBNull.Value) item.k_Wert_Dachflaeche = Convert.ToDouble(row["k_Wert_Dachflaeche"]);
            if (dt.Columns.Contains("k_Wert_Grundflaeche") && row["k_Wert_Grundflaeche"] != DBNull.Value) item.k_Wert_Grundflaeche = Convert.ToDouble(row["k_Wert_Grundflaeche"]);
            if (dt.Columns.Contains("k_Wert_Sonstiges") && row["k_Wert_Sonstiges"] != DBNull.Value) item.k_Wert_Sonstiges = Convert.ToDouble(row["k_Wert_Sonstiges"]);
            if (dt.Columns.Contains("Flaeche_Außenwand") && row["Flaeche_Außenwand"] != DBNull.Value) item.Flaeche_Außenwand = Convert.ToDouble(row["Flaeche_Außenwand"]);
            if (dt.Columns.Contains("gesamte_Fensterflaeche") && row["gesamte_Fensterflaeche"] != DBNull.Value) item.gesamte_Fensterflaeche = Convert.ToDouble(row["gesamte_Fensterflaeche"]);
            if (dt.Columns.Contains("Dachflaeche") && row["Dachflaeche"] != DBNull.Value) item.Dachflaeche = Convert.ToDouble(row["Dachflaeche"]);
            if (dt.Columns.Contains("Grundflaeche") && row["Grundflaeche"] != DBNull.Value) item.Grundflaeche = Convert.ToDouble(row["Grundflaeche"]);
            if (dt.Columns.Contains("Sonstige_Flaechen") && row["Sonstige_Flaechen"] != DBNull.Value) item.Sonstige_Flaechen = Convert.ToDouble(row["Sonstige_Flaechen"]);
            if (dt.Columns.Contains("Nutzflaeche") && row["Nutzflaeche"] != DBNull.Value) item.Nutzflaeche = Convert.ToDouble(row["Nutzflaeche"]);
            if (dt.Columns.Contains("Raumhoehe") && row["Raumhoehe"] != DBNull.Value) item.Raumhoehe = Convert.ToDouble(row["Raumhoehe"]);
            if (dt.Columns.Contains("WBVK_Anschluß_Fenster_Wand") && row["WBVK_Anschluß_Fenster_Wand"] != DBNull.Value) item.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand = Convert.ToDouble(row["WBVK_Anschluß_Fenster_Wand"]);
            if (dt.Columns.Contains("WBVK_Anschluß_Wand_Dach") && row["WBVK_Anschluß_Wand_Dach"] != DBNull.Value) item.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach = Convert.ToDouble(row["WBVK_Anschluß_Wand_Dach"]);
            if (dt.Columns.Contains("WBVK_Anschluß_Außenwand_Kellerdecke") && row["WBVK_Anschluß_Außenwand_Kellerdecke"] != DBNull.Value) item.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke = Convert.ToDouble(row["WBVK_Anschluß_Außenwand_Kellerdecke"]);
            if (dt.Columns.Contains("Abmessung_Anschluß_Fenster_Wand") && row["Abmessung_Anschluß_Fenster_Wand"] != DBNull.Value) item.Abmessung_Anschluß_Fenster_Wand = Convert.ToDouble(row["Abmessung_Anschluß_Fenster_Wand"]);
            if (dt.Columns.Contains("Abmessung_Anschluß_Wand_Dach") && row["Abmessung_Anschluß_Wand_Dach"] != DBNull.Value) item.Abmessung_Anschluß_Wand_Dach = Convert.ToDouble(row["Abmessung_Anschluß_Wand_Dach"]);
            if (dt.Columns.Contains("Abmessung_Anschluß_Außenwand_Kellerdecke") && row["Abmessung_Anschluß_Außenwand_Kellerdecke"] != DBNull.Value) item.Abmessung_Anschluß_Außenwand_Kellerdecke = Convert.ToDouble(row["Abmessung_Anschluß_Außenwand_Kellerdecke"]);
            if (dt.Columns.Contains("Luftwechselrate") && row["Luftwechselrate"] != DBNull.Value) item.Luftwechselrate = Convert.ToDouble(row["Luftwechselrate"]);
            if (dt.Columns.Contains("Wochenende") && row["Wochenende"] != DBNull.Value) item.Wochenende = Convert.ToDouble(row["Wochenende"]);
            if (dt.Columns.Contains("Ferien") && row["Ferien"] != DBNull.Value) item.Ferien = Convert.ToDouble(row["Ferien"]);
            if (dt.Columns.Contains("Ferienbeginn_1") && row["Ferienbeginn_1"] != DBNull.Value) item.Ferienbeginn_1 = Convert.ToDouble(row["Ferienbeginn_1"]);
            if (dt.Columns.Contains("Ferienende_1") && row["Ferienende_1"] != DBNull.Value) item.Ferienende_1 = Convert.ToDouble(row["Ferienende_1"]);
            if (dt.Columns.Contains("Ferienbeginn_2") && row["Ferienbeginn_2"] != DBNull.Value) item.Ferienbeginn_2 = Convert.ToDouble(row["Ferienbeginn_2"]);
            if (dt.Columns.Contains("Ferienende_2") && row["Ferienende_2"] != DBNull.Value) item.Ferienende_2 = Convert.ToDouble(row["Ferienende_2"]);
            if (dt.Columns.Contains("Ferienbeginn_3") && row["Ferienbeginn_3"] != DBNull.Value) item.Ferienbeginn_3 = Convert.ToDouble(row["Ferienbeginn_3"]);
            if (dt.Columns.Contains("Ferienende_3") && row["Ferienende_3"] != DBNull.Value) item.Ferienende_3 = Convert.ToDouble(row["Ferienende_3"]);
            if (dt.Columns.Contains("Ferienbeginn_4") && row["Ferienbeginn_4"] != DBNull.Value) item.Ferienbeginn_4 = Convert.ToDouble(row["Ferienbeginn_4"]);
            if (dt.Columns.Contains("Ferienende_4") && row["Ferienende_4"] != DBNull.Value) item.Ferienende_4 = Convert.ToDouble(row["Ferienende_4"]);
            if (dt.Columns.Contains("WW_Bedarf") && row["WW_Bedarf"] != DBNull.Value) item.WW_Bedarf = Convert.ToDouble(row["WW_Bedarf"]);
            if (dt.Columns.Contains("spez_Waermeverbrauch") && row["spez_Waermeverbrauch"] != DBNull.Value) item.spez_Waermeverbrauch = Convert.ToDouble(row["spez_Waermeverbrauch"]);
            if (dt.Columns.Contains("Waermebedarf") && row["Waermebedarf"] != DBNull.Value) item.Waermebedarf = Convert.ToDouble(row["Waermebedarf"]);
            if (dt.Columns.Contains("Baualtersklasse") && row["Baualtersklasse"] != DBNull.Value) item.Baualtersklasse = row["Baualtersklasse"].ToString();
            if (dt.Columns.Contains("Gebaeudeart") && row["Gebaeudeart"] != DBNull.Value) item.Gebaeudeart = row["Gebaeudeart"].ToString();
            if (dt.Columns.Contains("Wohngebaeude_Nicht_Wohngebaeude") && row["Wohngebaeude_Nicht_Wohngebaeude"] != DBNull.Value) item.Wohngebaeude_Nicht_Wohngebaeude = row["Wohngebaeude_Nicht_Wohngebaeude"].ToString();
            NeueSpaltenLesen(item, row);
            if (item == this && dt.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value)
                this.m_bReadOnly = Convert.ToBoolean(row["ReadOnly"]);
        }

        #endregion

        #region --- WRITE (Katalog: Insert / Overwrite) ---

        // Wert-Parameter in fester Spaltenreihenfolge (positionsgebunden fuer OleDb "?").
        // WICHTIG: eindeutige, praefixfreie Parameternamen (@b00..), damit der ACE-OLEDB-Provider
        // keine namensaehnlichen Parameter verwechselt (z.B. Wohnflaeche vs. Wohnflaeche_gesamt).
        private DbParam[] BuildValueParams(GebaeudeModel m)
        {
            return new DbParam[]
            {
                new DbParam("@b00", DbParamTyp.VarWChar) { Wert = (object)(m.Gebaeudename ?? "") },
                new DbParam("@b01", DbParamTyp.VarWChar) { Wert = (object)(m.Typ ?? "") },
                new DbParam("@b02", DbParamTyp.VarWChar) { Wert = (object)(m.Beschreibung ?? "") },
                new DbParam("@b03", DbParamTyp.Double) { Wert = m.Wohnflaeche_gesamt },
                new DbParam("@b04", DbParamTyp.Double) { Wert = m.Bewohner },
                new DbParam("@b05", DbParamTyp.Double) { Wert = m.Flaeche_Nutzer },
                new DbParam("@b06", DbParamTyp.Double) { Wert = m.Interne_Waermegewinne },
                new DbParam("@b07", DbParamTyp.Double) { Wert = m.Bauweise },
                new DbParam("@b08", DbParamTyp.Double) { Wert = m.Fensterflaeche_Sued },
                new DbParam("@b09", DbParamTyp.Double) { Wert = m.Fensterflaeche_OstWest },
                new DbParam("@b10", DbParamTyp.Double) { Wert = m.Fensterflaeche_Nord },
                new DbParam("@b11", DbParamTyp.Double) { Wert = m.Fensterdurchlassgrad },
                new DbParam("@b12", DbParamTyp.Double) { Wert = m.Raumsolltemperatur_Nachtabsenkung },
                new DbParam("@b13", DbParamTyp.Double) { Wert = m.Raumsolltemperatur_Tag },
                new DbParam("@b14", DbParamTyp.Double) { Wert = m.Raumsolltemperatur_Wochenende },
                new DbParam("@b15", DbParamTyp.Double) { Wert = m.Raumsolltemperatur_Ferien },
                new DbParam("@b16", DbParamTyp.Double) { Wert = m.Maximaleraumtemperatur },
                new DbParam("@b17", DbParamTyp.Double) { Wert = m.k_Wert_Außenwand },
                new DbParam("@b18", DbParamTyp.Double) { Wert = m.k_Wert_Fenster },
                new DbParam("@b19", DbParamTyp.Double) { Wert = m.k_Wert_Dachflaeche },
                new DbParam("@b20", DbParamTyp.Double) { Wert = m.k_Wert_Grundflaeche },
                new DbParam("@b21", DbParamTyp.Double) { Wert = m.k_Wert_Sonstiges },
                new DbParam("@b22", DbParamTyp.Double) { Wert = m.Flaeche_Außenwand },
                new DbParam("@b23", DbParamTyp.Double) { Wert = m.gesamte_Fensterflaeche },
                new DbParam("@b24", DbParamTyp.Double) { Wert = m.Dachflaeche },
                new DbParam("@b25", DbParamTyp.Double) { Wert = m.Grundflaeche },
                new DbParam("@b26", DbParamTyp.Double) { Wert = m.Sonstige_Flaechen },
                new DbParam("@b27", DbParamTyp.Double) { Wert = m.Nutzflaeche },
                new DbParam("@b28", DbParamTyp.Double) { Wert = m.Raumhoehe },
                new DbParam("@b29", DbParamTyp.Double) { Wert = m.Waermebrueckenverlustkoeffizient_Anschluß_Fenster_Wand },
                new DbParam("@b30", DbParamTyp.Double) { Wert = m.Waermebrueckenverlustkoeffizient_Anschluß_Wand_Dach },
                new DbParam("@b31", DbParamTyp.Double) { Wert = m.Waermebruckenverlustkoeffizient_Anschluß_Außenwand_Kellerdecke },
                new DbParam("@b32", DbParamTyp.Double) { Wert = m.Abmessung_Anschluß_Fenster_Wand },
                new DbParam("@b33", DbParamTyp.Double) { Wert = m.Abmessung_Anschluß_Wand_Dach },
                new DbParam("@b34", DbParamTyp.Double) { Wert = m.Abmessung_Anschluß_Außenwand_Kellerdecke },
                new DbParam("@b35", DbParamTyp.Double) { Wert = m.Luftwechselrate },
                new DbParam("@b36", DbParamTyp.Double) { Wert = m.Wochenende },
                new DbParam("@b37", DbParamTyp.Double) { Wert = m.Ferien },
                new DbParam("@b38", DbParamTyp.Double) { Wert = m.Ferienbeginn_1 },
                new DbParam("@b39", DbParamTyp.Double) { Wert = m.Ferienende_1 },
                new DbParam("@b40", DbParamTyp.Double) { Wert = m.Ferienbeginn_2 },
                new DbParam("@b41", DbParamTyp.Double) { Wert = m.Ferienende_2 },
                new DbParam("@b42", DbParamTyp.Double) { Wert = m.Ferienbeginn_3 },
                new DbParam("@b43", DbParamTyp.Double) { Wert = m.Ferienende_3 },
                new DbParam("@b44", DbParamTyp.Double) { Wert = m.Ferienbeginn_4 },
                new DbParam("@b45", DbParamTyp.Double) { Wert = m.Ferienende_4 },
                new DbParam("@b46", DbParamTyp.Double) { Wert = m.WW_Bedarf },
                new DbParam("@b47", DbParamTyp.Double) { Wert = m.spez_Waermeverbrauch },
                new DbParam("@b48", DbParamTyp.Double) { Wert = m.Waermebedarf },
                new DbParam("@b49", DbParamTyp.VarWChar) { Wert = (object)(m.Baualtersklasse ?? "") },
                new DbParam("@b50", DbParamTyp.VarWChar) { Wert = (object)(m.Gebaeudeart ?? "") },
                new DbParam("@b51", DbParamTyp.VarWChar) { Wert = (object)(m.Wohngebaeude_Nicht_Wohngebaeude ?? "") },
                // Gebaeudespalten-Schritt M3 (Schemaschritt 101): NULL-erhaltend
                new DbParam("@b52", DbParamTyp.VarWChar) { Wert = Wert(m.Gebaeude_Modell) },
                new DbParam("@b53", DbParamTyp.Double) { Wert = Wert(m.Fensterflaeche_Ost) },
                new DbParam("@b54", DbParamTyp.Double) { Wert = Wert(m.Fensterflaeche_West) },
                new DbParam("@b55", DbParamTyp.Double) { Wert = Wert(m.Rahmenanteil) },
                new DbParam("@b56", DbParamTyp.Double) { Wert = Wert(m.Verschattungsfaktor) },
                new DbParam("@b57", DbParamTyp.VarWChar) { Wert = Wert(m.Grundflaeche_Randbedingung) },
                new DbParam("@b58", DbParamTyp.Double) { Wert = Wert(m.Kellertemperatur) },
                new DbParam("@b59", DbParamTyp.Double) { Wert = Wert(m.Masseanteil_Aussen) },
                new DbParam("@b60", DbParamTyp.Double) { Wert = Wert(m.Innenflaechenfaktor) },
                new DbParam("@b61", DbParamTyp.Double) { Wert = Wert(m.Heizung_Strahlungsanteil) },
                new DbParam("@b62", DbParamTyp.Double) { Wert = Wert(m.Heizleistung_Max) },
                new DbParam("@b63", DbParamTyp.Boolean) { Wert = m.Aussenbauteile_Strahlung },
                new DbParam("@b64", DbParamTyp.Double) { Wert = Wert(m.Luftwechsel_Infiltration) },
                new DbParam("@b65", DbParamTyp.Double) { Wert = Wert(m.Luftwechsel_Nutzer) },
                new DbParam("@b66", DbParamTyp.Boolean) { Wert = m.Sommerlueftung },
                // KU-S1 (Schemaschritt 108): NULL-erhaltend, der Schalter 0/1
                new DbParam("@b67", DbParamTyp.Double) { Wert = Wert(m.Kuehl_Sollwert) },
                new DbParam("@b68", DbParamTyp.Double) { Wert = Wert(m.Kuehlleistung_Max) },
                new DbParam("@b69", DbParamTyp.Boolean) { Wert = m.Kuehlung_Aktiv },
                new DbParam("@b70", DbParamTyp.Double) { Wert = Wert(m.Kuehl_Sollwert_Nacht) },
            };
        }

        /// <summary>Ein nullbarer Wert als Parameterwert - NULL bleibt NULL (Vorgabe).</summary>
        private static object Wert(double? x) => x.HasValue ? (object)x.Value : DBNull.Value;

        /// <summary>Ein Text als Parameterwert - NULL bleibt NULL (Vorgabe).</summary>
        private static object Wert(string x) => x != null ? (object)x : DBNull.Value;

        // Legt einen neuen Gebaeude-Stammdatensatz an. ID explizit als MAX(ID)+1
        // (beim Kopieren einer Access-Tabelle wird die Autonummerierung zu einer normalen Long-Zahl).
        public bool Insert(GebaeudeModel m)
        {
            int newId = DataRepository.GetMaxID(TABLE) + 1;
            string sql = "INSERT INTO [" + TABLE + "] ([ID], [Bezeichner], [Typ], [Beschreibung], [Wohnflaeche_gesamt], [Bewohner], [Flaeche_Nutzer], [Interne_Waermegewinne], [Bauweise], [Fensterflaeche_Sued], [Fensterflaeche_Ost_West], [Fensterflaeche_Nord], [Fensterdurchlassgrad], [Raumsolltemperatur_Nachtabsenkung], [Raumsolltemperatur_Tag], [Raumsolltemperatur_Wochenende], [Raumsolltemperatur_Ferien], [Maximaleraumtemperatur], [k_Wert_Außenwand], [k_Wert_Fenster], [k_Wert_Dachflaeche], [k_Wert_Grundflaeche], [k_Wert_Sonstiges], [Flaeche_Außenwand], [gesamte_Fensterflaeche], [Dachflaeche], [Grundflaeche], [Sonstige_Flaechen], [Nutzflaeche], [Raumhoehe], [WBVK_Anschluß_Fenster_Wand], [WBVK_Anschluß_Wand_Dach], [WBVK_Anschluß_Außenwand_Kellerdecke], [Abmessung_Anschluß_Fenster_Wand], [Abmessung_Anschluß_Wand_Dach], [Abmessung_Anschluß_Außenwand_Kellerdecke], [Luftwechselrate], [Wochenende], [Ferien], [Ferienbeginn_1], [Ferienende_1], [Ferienbeginn_2], [Ferienende_2], [Ferienbeginn_3], [Ferienende_3], [Ferienbeginn_4], [Ferienende_4], [WW_Bedarf], [spez_Waermeverbrauch], [Waermebedarf], [Baualtersklasse], [Gebaeudeart], [Wohngebaeude_Nicht_Wohngebaeude], [Gebaeude_Modell], [Fensterflaeche_Ost], [Fensterflaeche_West], [Rahmenanteil], [Verschattungsfaktor], [Grundflaeche_Randbedingung], [Kellertemperatur], [Masseanteil_Aussen], [Innenflaechenfaktor], [Heizung_Strahlungsanteil], [Heizleistung_Max], [Aussenbauteile_Strahlung], [Luftwechsel_Infiltration], [Luftwechsel_Nutzer], [Sommerlueftung], [Kuehl_Sollwert], [Kuehlleistung_Max], [Kuehlung_Aktiv], [Kuehl_Sollwert_Nacht], [ReadOnly]) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            var ps = new List<DbParam>();
            ps.Add(new DbParam("@bid", DbParamTyp.Integer) { Wert = newId });
            ps.AddRange(BuildValueParams(m));
            ps.Add(new DbParam("@bro", DbParamTyp.Boolean) { Wert = false });
            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        // Ueberschreibt einen vorhandenen Stammdatensatz (Schluessel = Bezeichner), sofern nicht schreibgeschuetzt.
        public bool Overwrite(GebaeudeModel m)
        {
            if (IsReadOnly(m.Gebaeudename))
            {
                Meldung.Hinweis("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht überschrieben werden.",
                    "Schreibgeschützt");
                return false;
            }
            string sql = "UPDATE [" + TABLE + "] SET [Bezeichner] = ?, [Typ] = ?, [Beschreibung] = ?, [Wohnflaeche_gesamt] = ?, [Bewohner] = ?, [Flaeche_Nutzer] = ?, [Interne_Waermegewinne] = ?, [Bauweise] = ?, [Fensterflaeche_Sued] = ?, [Fensterflaeche_Ost_West] = ?, [Fensterflaeche_Nord] = ?, [Fensterdurchlassgrad] = ?, [Raumsolltemperatur_Nachtabsenkung] = ?, [Raumsolltemperatur_Tag] = ?, [Raumsolltemperatur_Wochenende] = ?, [Raumsolltemperatur_Ferien] = ?, [Maximaleraumtemperatur] = ?, [k_Wert_Außenwand] = ?, [k_Wert_Fenster] = ?, [k_Wert_Dachflaeche] = ?, [k_Wert_Grundflaeche] = ?, [k_Wert_Sonstiges] = ?, [Flaeche_Außenwand] = ?, [gesamte_Fensterflaeche] = ?, [Dachflaeche] = ?, [Grundflaeche] = ?, [Sonstige_Flaechen] = ?, [Nutzflaeche] = ?, [Raumhoehe] = ?, [WBVK_Anschluß_Fenster_Wand] = ?, [WBVK_Anschluß_Wand_Dach] = ?, [WBVK_Anschluß_Außenwand_Kellerdecke] = ?, [Abmessung_Anschluß_Fenster_Wand] = ?, [Abmessung_Anschluß_Wand_Dach] = ?, [Abmessung_Anschluß_Außenwand_Kellerdecke] = ?, [Luftwechselrate] = ?, [Wochenende] = ?, [Ferien] = ?, [Ferienbeginn_1] = ?, [Ferienende_1] = ?, [Ferienbeginn_2] = ?, [Ferienende_2] = ?, [Ferienbeginn_3] = ?, [Ferienende_3] = ?, [Ferienbeginn_4] = ?, [Ferienende_4] = ?, [WW_Bedarf] = ?, [spez_Waermeverbrauch] = ?, [Waermebedarf] = ?, [Baualtersklasse] = ?, [Gebaeudeart] = ?, [Wohngebaeude_Nicht_Wohngebaeude] = ?, [Gebaeude_Modell] = ?, [Fensterflaeche_Ost] = ?, [Fensterflaeche_West] = ?, [Rahmenanteil] = ?, [Verschattungsfaktor] = ?, [Grundflaeche_Randbedingung] = ?, [Kellertemperatur] = ?, [Masseanteil_Aussen] = ?, [Innenflaechenfaktor] = ?, [Heizung_Strahlungsanteil] = ?, [Heizleistung_Max] = ?, [Aussenbauteile_Strahlung] = ?, [Luftwechsel_Infiltration] = ?, [Luftwechsel_Nutzer] = ?, [Sommerlueftung] = ?, [Kuehl_Sollwert] = ?, [Kuehlleistung_Max] = ?, [Kuehlung_Aktiv] = ?, [Kuehl_Sollwert_Nacht] = ? WHERE Bezeichner = ?";
            var ps = new List<DbParam>(BuildValueParams(m));
            ps.Add(new DbParam("@bkey", DbParamTyp.VarWChar) { Wert = (object)(m.Gebaeudename ?? "") });
            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        #endregion

        #region --- Gebaeudespalten-Schritt M3 (NULL-erhaltend) ---

        /// <summary>
        /// Liest die fuenfzehn Spalten des Gebaeudespalten-Schritts M3 (Schemaschritt 101)
        /// und die vier Kuehleingaben aus KU-S1 (Schemaschritt 108) NULL-ERHALTEND in das
        /// Modell: NULL bleibt <c>null</c>, die drei Schalter werden 0/1. Fehlt eine Spalte
        /// (Datenbank vor dem Schritt), bleibt das Feld auf seiner Vorbelegung. Gerufen fuer
        /// Katalog (hier) und Projekt (<c>GebaeudeCtrl</c>).
        /// </summary>
        internal static void NeueSpaltenLesen(GebaeudeModel item, DataRow row)
        {
            item.Gebaeude_Modell = Text(row, GebaeudeSchema.SPALTE_GEBAEUDE_MODELL);
            item.Fensterflaeche_Ost = Zahl(row, GebaeudeSchema.SPALTE_FENSTERFLAECHE_OST);
            item.Fensterflaeche_West = Zahl(row, GebaeudeSchema.SPALTE_FENSTERFLAECHE_WEST);
            item.Rahmenanteil = Zahl(row, GebaeudeSchema.SPALTE_RAHMENANTEIL);
            item.Verschattungsfaktor = Zahl(row, GebaeudeSchema.SPALTE_VERSCHATTUNGSFAKTOR);
            item.Grundflaeche_Randbedingung = Text(row, GebaeudeSchema.SPALTE_GRUNDFLAECHE_RANDBEDINGUNG);
            item.Kellertemperatur = Zahl(row, GebaeudeSchema.SPALTE_KELLERTEMPERATUR);
            item.Masseanteil_Aussen = Zahl(row, GebaeudeSchema.SPALTE_MASSEANTEIL_AUSSEN);
            item.Innenflaechenfaktor = Zahl(row, GebaeudeSchema.SPALTE_INNENFLAECHENFAKTOR);
            item.Heizung_Strahlungsanteil = Zahl(row, GebaeudeSchema.SPALTE_HEIZUNG_STRAHLUNGSANTEIL);
            item.Heizleistung_Max = Zahl(row, GebaeudeSchema.SPALTE_HEIZLEISTUNG_MAX);
            item.Aussenbauteile_Strahlung = Schalter(row, GebaeudeSchema.SPALTE_AUSSENBAUTEILE_STRAHLUNG);
            item.Luftwechsel_Infiltration = Zahl(row, GebaeudeSchema.SPALTE_LUFTWECHSEL_INFILTRATION);
            item.Luftwechsel_Nutzer = Zahl(row, GebaeudeSchema.SPALTE_LUFTWECHSEL_NUTZER);
            item.Sommerlueftung = Schalter(row, GebaeudeSchema.SPALTE_SOMMERLUEFTUNG);

            // KU-S1 (Schemaschritt 108, Kuehlkonzept 7.1): die vier Kuehleingaben, ebenso
            // NULL-ERHALTEND; der Schalter wird 0/1.
            item.Kuehl_Sollwert = Zahl(row, GebaeudeSchema.SPALTE_KUEHL_SOLLWERT);
            item.Kuehlleistung_Max = Zahl(row, GebaeudeSchema.SPALTE_KUEHLLEISTUNG_MAX);
            item.Kuehlung_Aktiv = Schalter(row, GebaeudeSchema.SPALTE_KUEHLUNG_AKTIV);
            item.Kuehl_Sollwert_Nacht = Zahl(row, GebaeudeSchema.SPALTE_KUEHL_SOLLWERT_NACHT);
        }

        private static object Roh(DataRow row, string spalte)
            => row.Table.Columns.Contains(spalte) ? row[spalte] : DBNull.Value;

        private static string Text(DataRow row, string spalte)
        {
            object w = Roh(row, spalte);
            return w == DBNull.Value ? null : Convert.ToString(w, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static double? Zahl(DataRow row, string spalte)
        {
            object w = Roh(row, spalte);
            return w == DBNull.Value ? (double?)null : Convert.ToDouble(w, System.Globalization.CultureInfo.InvariantCulture);
        }

        private static bool Schalter(DataRow row, string spalte)
        {
            object w = Roh(row, spalte);
            return w != DBNull.Value && Convert.ToInt64(w, System.Globalization.CultureInfo.InvariantCulture) != 0;
        }

        #endregion

        #region --- COPY (STAMM -> Projekt) ---

        // Kopiert einen Gebaeude-Stammdatensatz (per Bezeichner) in die Projekt-Tabelle Tab_Gebaeude.
        // Setzt ID_Projekt, die Verknuepfung ID_ProjektGebaeude (-> Z_ProjektGebaeude.ID) und den
        // Katalogverweis ID_Gebaeude_Stamm (-> Tab_Gebaeude_STAMM.ID, Schemaschritt 121).
        // Der EINZIGE Weg Katalog -> Projekt: Assistent und Projekt-Gebaeudedialog gehen beide
        // ueber WizardCtrl.Add_Projekt_ZuordungGebäude hierher; Duplizieren und Varianten
        // kopieren die Projektzeile samt Verweis (ProjektDuplizierenCtrl).
        // Rueckgabe: neue Tab_Gebaeude.ID (>0) oder 0 bei Fehler / nicht gefunden.
        public int CopyFromStamm(string szBezeichner, int idProjekt, int idProjektGebaeude)
            => CopyFromStamm(null, szBezeichner, idProjekt, idProjektGebaeude);

        /// <summary>
        /// Dieselbe Kopie, aber der Katalogsatz wird ZUERST über seine Id gesucht
        /// (<c>Tab_Gebaeude.ID_Gebaeude_Stamm</c> der bisherigen Projektkopie, Schemaschritt
        /// 121) und erst ohne Id — oder wenn es den Satz dieser Id nicht mehr gibt — über den
        /// Namen. So übersteht das Neuschreiben der Gebäudeliste eines Projekts eine
        /// Umbenennung des Katalogsatzes; der Name ist nur noch der Rückfall für Altbestand
        /// ohne Verweis (Konzept Administrationsdialoge 7.1 (a)).
        /// </summary>
        public int CopyFromStamm(int? idStamm, string szBezeichner, int idProjekt, int idProjektGebaeude)
        {
            DataTable dt = null;
            if (idStamm.HasValue && idStamm.Value > 0)
                dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + TABLE + "] WHERE ID = ?",
                    new DbParam("@cid", idStamm.Value));
            if (dt == null || dt.Rows.Count == 0)
                dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + TABLE + "] WHERE Bezeichner = ?",
                    new DbParam("@cbez", szBezeichner ?? (object)DBNull.Value));
            if (dt == null || dt.Rows.Count == 0) return 0;
            DataRow r = dt.Rows[0];

            int newId = DataRepository.GetMaxID(TABLE_PROJ) + 1;

            string sql = "INSERT INTO [" + TABLE_PROJ + "] ([ID], [ID_ProjektGebaeude], [ID_Projekt], [Gebaeudename], [Typ], [Beschreibung], [Wohnflaeche_gesamt], [Bewohner], [Flaeche_Nutzer], [Interne_Waermegewinne], [Bauweise], [Fensterflaeche_Sued], [Fensterflaeche_Ost_West], [Fensterflaeche_Nord], [Fensterdurchlassgrad], [Raumsolltemperatur_Nachtabsenkung], [Raumsolltemperatur_Tag], [Raumsolltemperatur_Wochenende], [Raumsolltemperatur_Ferien], [Maximaleraumtemperatur], [k_Wert_Außenwand], [k_Wert_Fenster], [k_Wert_Dachflaeche], [k_Wert_Grundflaeche], [k_Wert_Sonstiges], [Flaeche_Außenwand], [gesamte_Fensterflaeche], [Dachflaeche], [Grundflaeche], [Sonstige_Flaechen], [Nutzflaeche], [Raumhoehe], [WBVK_Anschluß_Fenster_Wand], [WBVK_Anschluß_Wand_Dach], [WBVK_Anschluß_Außenwand_Kellerdecke], [Abmessung_Anschluß_Fenster_Wand], [Abmessung_Anschluß_Wand_Dach], [Abmessung_Anschluß_Außenwand_Kellerdecke], [Luftwechselrate], [Wochenende], [Ferien], [Ferienbeginn_1], [Ferienende_1], [Ferienbeginn_2], [Ferienende_2], [Ferienbeginn_3], [Ferienende_3], [Ferienbeginn_4], [Ferienende_4], [WW_Bedarf], [spez_Waermeverbrauch], [Waermebedarf], [Baualtersklasse], [Gebaeudeart], [Wohngebaeude_Nicht_Wohngebaeude], [Gebaeude_Modell], [Fensterflaeche_Ost], [Fensterflaeche_West], [Rahmenanteil], [Verschattungsfaktor], [Grundflaeche_Randbedingung], [Kellertemperatur], [Masseanteil_Aussen], [Innenflaechenfaktor], [Heizung_Strahlungsanteil], [Heizleistung_Max], [Aussenbauteile_Strahlung], [Luftwechsel_Infiltration], [Luftwechsel_Nutzer], [Sommerlueftung], [Kuehl_Sollwert], [Kuehlleistung_Max], [Kuehlung_Aktiv], [Kuehl_Sollwert_Nacht], [ID_Gebaeude_Stamm]) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            DbParam[] ps = new DbParam[]
            {
                new DbParam("@c00", DbParamTyp.Integer) { Wert = newId },
                new DbParam("@c01", DbParamTyp.Integer) { Wert = idProjektGebaeude },
                new DbParam("@c02", DbParamTyp.Integer) { Wert = idProjekt },
                new DbParam("@c03", DbParamTyp.VarWChar) { Wert = (object)(r["Bezeichner"] == DBNull.Value ? "" : r["Bezeichner"].ToString()) },
                new DbParam("@c04", DbParamTyp.VarWChar) { Wert = (object)(r["Typ"] == DBNull.Value ? "" : r["Typ"].ToString()) },
                new DbParam("@c05", DbParamTyp.VarWChar) { Wert = (object)(r["Beschreibung"] == DBNull.Value ? "" : r["Beschreibung"].ToString()) },
                new DbParam("@c06", DbParamTyp.Double) { Wert = (object)(r["Wohnflaeche_gesamt"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Wohnflaeche_gesamt"])) },
                new DbParam("@c07", DbParamTyp.Double) { Wert = (object)(r["Bewohner"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Bewohner"])) },
                new DbParam("@c08", DbParamTyp.Double) { Wert = (object)(r["Flaeche_Nutzer"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Flaeche_Nutzer"])) },
                new DbParam("@c09", DbParamTyp.Double) { Wert = (object)(r["Interne_Waermegewinne"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Interne_Waermegewinne"])) },
                new DbParam("@c10", DbParamTyp.Double) { Wert = (object)(r["Bauweise"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Bauweise"])) },
                new DbParam("@c11", DbParamTyp.Double) { Wert = (object)(r["Fensterflaeche_Sued"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Fensterflaeche_Sued"])) },
                new DbParam("@c12", DbParamTyp.Double) { Wert = (object)(r["Fensterflaeche_Ost_West"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Fensterflaeche_Ost_West"])) },
                new DbParam("@c13", DbParamTyp.Double) { Wert = (object)(r["Fensterflaeche_Nord"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Fensterflaeche_Nord"])) },
                new DbParam("@c14", DbParamTyp.Double) { Wert = (object)(r["Fensterdurchlassgrad"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Fensterdurchlassgrad"])) },
                new DbParam("@c15", DbParamTyp.Double) { Wert = (object)(r["Raumsolltemperatur_Nachtabsenkung"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Raumsolltemperatur_Nachtabsenkung"])) },
                new DbParam("@c16", DbParamTyp.Double) { Wert = (object)(r["Raumsolltemperatur_Tag"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Raumsolltemperatur_Tag"])) },
                new DbParam("@c17", DbParamTyp.Double) { Wert = (object)(r["Raumsolltemperatur_Wochenende"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Raumsolltemperatur_Wochenende"])) },
                new DbParam("@c18", DbParamTyp.Double) { Wert = (object)(r["Raumsolltemperatur_Ferien"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Raumsolltemperatur_Ferien"])) },
                new DbParam("@c19", DbParamTyp.Double) { Wert = (object)(r["Maximaleraumtemperatur"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Maximaleraumtemperatur"])) },
                new DbParam("@c20", DbParamTyp.Double) { Wert = (object)(r["k_Wert_Außenwand"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["k_Wert_Außenwand"])) },
                new DbParam("@c21", DbParamTyp.Double) { Wert = (object)(r["k_Wert_Fenster"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["k_Wert_Fenster"])) },
                new DbParam("@c22", DbParamTyp.Double) { Wert = (object)(r["k_Wert_Dachflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["k_Wert_Dachflaeche"])) },
                new DbParam("@c23", DbParamTyp.Double) { Wert = (object)(r["k_Wert_Grundflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["k_Wert_Grundflaeche"])) },
                new DbParam("@c24", DbParamTyp.Double) { Wert = (object)(r["k_Wert_Sonstiges"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["k_Wert_Sonstiges"])) },
                new DbParam("@c25", DbParamTyp.Double) { Wert = (object)(r["Flaeche_Außenwand"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Flaeche_Außenwand"])) },
                new DbParam("@c26", DbParamTyp.Double) { Wert = (object)(r["gesamte_Fensterflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["gesamte_Fensterflaeche"])) },
                new DbParam("@c27", DbParamTyp.Double) { Wert = (object)(r["Dachflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Dachflaeche"])) },
                new DbParam("@c28", DbParamTyp.Double) { Wert = (object)(r["Grundflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Grundflaeche"])) },
                new DbParam("@c29", DbParamTyp.Double) { Wert = (object)(r["Sonstige_Flaechen"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Sonstige_Flaechen"])) },
                new DbParam("@c30", DbParamTyp.Double) { Wert = (object)(r["Nutzflaeche"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Nutzflaeche"])) },
                new DbParam("@c31", DbParamTyp.Double) { Wert = (object)(r["Raumhoehe"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Raumhoehe"])) },
                new DbParam("@c32", DbParamTyp.Double) { Wert = (object)(r["WBVK_Anschluß_Fenster_Wand"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["WBVK_Anschluß_Fenster_Wand"])) },
                new DbParam("@c33", DbParamTyp.Double) { Wert = (object)(r["WBVK_Anschluß_Wand_Dach"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["WBVK_Anschluß_Wand_Dach"])) },
                new DbParam("@c34", DbParamTyp.Double) { Wert = (object)(r["WBVK_Anschluß_Außenwand_Kellerdecke"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["WBVK_Anschluß_Außenwand_Kellerdecke"])) },
                new DbParam("@c35", DbParamTyp.Double) { Wert = (object)(r["Abmessung_Anschluß_Fenster_Wand"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Abmessung_Anschluß_Fenster_Wand"])) },
                new DbParam("@c36", DbParamTyp.Double) { Wert = (object)(r["Abmessung_Anschluß_Wand_Dach"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Abmessung_Anschluß_Wand_Dach"])) },
                new DbParam("@c37", DbParamTyp.Double) { Wert = (object)(r["Abmessung_Anschluß_Außenwand_Kellerdecke"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Abmessung_Anschluß_Außenwand_Kellerdecke"])) },
                new DbParam("@c38", DbParamTyp.Double) { Wert = (object)(r["Luftwechselrate"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Luftwechselrate"])) },
                new DbParam("@c39", DbParamTyp.Double) { Wert = (object)(r["Wochenende"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Wochenende"])) },
                new DbParam("@c40", DbParamTyp.Double) { Wert = (object)(r["Ferien"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferien"])) },
                new DbParam("@c41", DbParamTyp.Double) { Wert = (object)(r["Ferienbeginn_1"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienbeginn_1"])) },
                new DbParam("@c42", DbParamTyp.Double) { Wert = (object)(r["Ferienende_1"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienende_1"])) },
                new DbParam("@c43", DbParamTyp.Double) { Wert = (object)(r["Ferienbeginn_2"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienbeginn_2"])) },
                new DbParam("@c44", DbParamTyp.Double) { Wert = (object)(r["Ferienende_2"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienende_2"])) },
                new DbParam("@c45", DbParamTyp.Double) { Wert = (object)(r["Ferienbeginn_3"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienbeginn_3"])) },
                new DbParam("@c46", DbParamTyp.Double) { Wert = (object)(r["Ferienende_3"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienende_3"])) },
                new DbParam("@c47", DbParamTyp.Double) { Wert = (object)(r["Ferienbeginn_4"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienbeginn_4"])) },
                new DbParam("@c48", DbParamTyp.Double) { Wert = (object)(r["Ferienende_4"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Ferienende_4"])) },
                new DbParam("@c49", DbParamTyp.Double) { Wert = (object)(r["WW_Bedarf"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["WW_Bedarf"])) },
                new DbParam("@c50", DbParamTyp.Double) { Wert = (object)(r["spez_Waermeverbrauch"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["spez_Waermeverbrauch"])) },
                new DbParam("@c51", DbParamTyp.Double) { Wert = (object)(r["Waermebedarf"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["Waermebedarf"])) },
                new DbParam("@c52", DbParamTyp.VarWChar) { Wert = (object)(r["Baualtersklasse"] == DBNull.Value ? "" : r["Baualtersklasse"].ToString()) },
                new DbParam("@c53", DbParamTyp.VarWChar) { Wert = (object)(r["Gebaeudeart"] == DBNull.Value ? "" : r["Gebaeudeart"].ToString()) },
                new DbParam("@c54", DbParamTyp.VarWChar) { Wert = (object)(r["Wohngebaeude_Nicht_Wohngebaeude"] == DBNull.Value ? "" : r["Wohngebaeude_Nicht_Wohngebaeude"].ToString()) },
                // Gebaeudespalten-Schritt M3 (Schemaschritt 101): NULL-ERHALTEND. Anders
                // als die Bestandsspalten oben wird NULL hier nicht zu 0 oder "" - NULL
                // heisst "Vorgabe", und fuer Rahmenanteil, Verschattungsfaktor & Co. ist 0
                // kein neutraler Wert (Umsetzungskonzept Gebaeudesimulation 1.6). Die zwei
                // Schalter kennen kein NULL und gehen als 0/1 hinueber.
                new DbParam("@c55", DbParamTyp.VarWChar) { Wert = Roh(r, GebaeudeSchema.SPALTE_GEBAEUDE_MODELL) },
                new DbParam("@c56", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_FENSTERFLAECHE_OST) },
                new DbParam("@c57", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_FENSTERFLAECHE_WEST) },
                new DbParam("@c58", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_RAHMENANTEIL) },
                new DbParam("@c59", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_VERSCHATTUNGSFAKTOR) },
                new DbParam("@c60", DbParamTyp.VarWChar) { Wert = Roh(r, GebaeudeSchema.SPALTE_GRUNDFLAECHE_RANDBEDINGUNG) },
                new DbParam("@c61", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KELLERTEMPERATUR) },
                new DbParam("@c62", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_MASSEANTEIL_AUSSEN) },
                new DbParam("@c63", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_INNENFLAECHENFAKTOR) },
                new DbParam("@c64", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_HEIZUNG_STRAHLUNGSANTEIL) },
                new DbParam("@c65", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_HEIZLEISTUNG_MAX) },
                new DbParam("@c66", DbParamTyp.Boolean) { Wert = Schalter(r, GebaeudeSchema.SPALTE_AUSSENBAUTEILE_STRAHLUNG) },
                new DbParam("@c67", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_LUFTWECHSEL_INFILTRATION) },
                new DbParam("@c68", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_LUFTWECHSEL_NUTZER) },
                new DbParam("@c69", DbParamTyp.Boolean) { Wert = Schalter(r, GebaeudeSchema.SPALTE_SOMMERLUEFTUNG) },
                // KU-S1 (Schemaschritt 108): dieselbe Regel - NULL bleibt NULL (Kuehlung aus,
                // unbegrenzt, wie der Tagwert), der Schalter geht als 0/1 hinueber.
                new DbParam("@c70", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHL_SOLLWERT) },
                new DbParam("@c71", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHLLEISTUNG_MAX) },
                new DbParam("@c72", DbParamTyp.Boolean) { Wert = Schalter(r, GebaeudeSchema.SPALTE_KUEHLUNG_AKTIV) },
                new DbParam("@c73", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHL_SOLLWERT_NACHT) },
                // Schemaschritt 121 (Welle #468): Die Kopie merkt sich, aus welchem
                // Katalogsatz sie stammt - eine Umbenennung des Satzes zerreisst die Klammer
                // der Loeschsperre dann nicht mehr (GebaeudeKatalogverweis).
                new DbParam("@c74", DbParamTyp.Integer) { Wert = Convert.ToInt32(r["ID"]) },
            };
            bool ok = DataRepository.ExecuteSQL(sql, ps);

            // Tagesverteilung des Gebaeudetyps (Tab_Gebaeude.Typ) mitkopieren.
            if (ok)
                CopyTagVForGebaeude(newId, r["Typ"] == DBNull.Value ? "" : r["Typ"].ToString());

            return ok ? newId : 0;
        }


        // Kopiert die Tagesverteilung (Tab_DBTagV_STAMM + Tab_DBTagVDaten_STAMM) des Gebaeudetyps
        // (Katalog-Bezeichner == Gebaeudetyp) in die Projekt-Tabellen und verknuepft ueber ID_Gebaeude.
        // Gibt es fuer den Typ keinen Katalogeintrag, wird sauber uebersprungen.
        private void CopyTagVForGebaeude(int idGebaeude, string szTyp)
        {
            if (string.IsNullOrEmpty(szTyp)) return;

            DataTable head = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Beschreibung FROM [Tab_DBTagV_STAMM] WHERE Bezeichner = ?",
                new DbParam("@t01", szTyp));
            if (head == null || head.Rows.Count == 0) return; // kein Tagesgang fuer diesen Typ

            int stammTagvId = Convert.ToInt32(head.Rows[0]["ID"]);
            string bez = head.Rows[0]["Bezeichner"] == DBNull.Value ? szTyp : head.Rows[0]["Bezeichner"].ToString();
            string beschr = head.Rows[0]["Beschreibung"] == DBNull.Value ? "" : head.Rows[0]["Beschreibung"].ToString();

            int newTagvId = DataRepository.GetMaxID("Tab_DBTagV") + 1;
            bool ok = DataRepository.ExecuteSQL(
                "INSERT INTO [Tab_DBTagV] ([ID], [ID_Gebaeude], [Bezeichner], [Beschreibung]) VALUES (?, ?, ?, ?)",
                new DbParam("@t02", DbParamTyp.Integer) { Wert = newTagvId },
                new DbParam("@t03", DbParamTyp.Integer) { Wert = idGebaeude },
                new DbParam("@t04", DbParamTyp.VarWChar) { Wert = (object)bez },
                new DbParam("@t05", DbParamTyp.VarWChar) { Wert = (object)beschr });
            if (!ok) return;

            DataTable daten = DataRepository.GetDataTable(
                "SELECT Verteilung FROM [Tab_DBTagVDaten_STAMM] WHERE ID_TagV = ? ORDER BY ID",
                new DbParam("@t06", stammTagvId));
            if (daten == null || daten.Rows.Count == 0) return;

            int nextId = DataRepository.GetMaxID("Tab_DBTagVDaten") + 1;
            using (DbVorgang v = DataRepository.Vorgang())
            {
                try
                {
                    foreach (DataRow dr in daten.Rows)
                    {
                        v.Ausfuehren(
                            "INSERT INTO [Tab_DBTagVDaten] ([ID], [ID_TagV], [Verteilung]) VALUES (?, ?, ?)",
                            new DbParam("@d01", DbParamTyp.Integer) { Wert = nextId++ },
                            new DbParam("@d02", DbParamTyp.Integer) { Wert = newTagvId },
                            new DbParam("@d03", DbParamTyp.Double)
                            { Wert = dr["Verteilung"] == DBNull.Value ? 0.0 : Convert.ToDouble(dr["Verteilung"]) });
                    }
                    v.Commit();
                }
                catch
                {
                    v.Rollback();
                }
            }
        }

        #endregion

        #region --- Stufe 5 der Neuordnung: die Verwaltung als Katalogliste (V16) ---

        /// <summary>
        /// <b>Die Zeilen der Gebaeudeverwaltung</b> (Konzept Administrationsdialoge, V16) —
        /// ALLE Katalogsaetze mit den fuenf Spalten aus
        /// <see cref="Katalogfilterprofil.FuerGebaeude"/>, in EINER Abfrage.
        ///
        /// <para><b>Gefiltert wird danach, nicht hier:</b> Die vier Vorfilter der eigenen
        /// Tabelle (Verwendung, Gebaeudeart, Baujahr, Suche) sind seit Stufe 5 Trichter und
        /// Suche der Katalogliste, und die filtert im Kern (<c>Katalogfilter.Anwenden</c>) auf
        /// dem ANGEZEIGTEN Wert. Deshalb stehen Verwendung und Baujahr hier als Klartext, nicht
        /// als Steuerwert bzw. Buchstabe.</para>
        ///
        /// <para>Ein Satz mit <c>ReadOnly</c> ist ein Auslieferungssatz und traegt das Schloss
        /// (<see cref="Katalogfilterzeile.Geschuetzt"/>).</para>
        /// </summary>
        public static IReadOnlyList<Katalogfilterzeile> Katalogfilterzeilen()
        {
            var liste = new List<Katalogfilterzeile>();
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Bezeichner, Gebaeudeart, Wohngebaeude_Nicht_Wohngebaeude, Baualtersklasse, " +
                "Wohnflaeche_gesamt, ReadOnly FROM [" + TABLE + "] ORDER BY Bezeichner");
            if (dt == null) return liste;

            IReadOnlyList<string> klassen = Baualtersklassen();
            foreach (DataRow r in dt.Rows)
            {
                string name = Spaltentext(r, "Bezeichner");
                string buchstabe = Spaltentext(r, "Baualtersklasse");
                object flaeche = r["Wohnflaeche_gesamt"];

                var zeile = new Katalogfilterzeile(Convert.ToInt32(r["ID"]), name)
                {
                    Geschuetzt = r["ReadOnly"] != DBNull.Value && Convert.ToInt32(r["ReadOnly"]) != 0
                };
                zeile.MitText(Katalogfilterprofil.SpBezeichner, name)
                     .MitText(Katalogfilterprofil.SpGebaeudeart, Spaltentext(r, "Gebaeudeart"))
                     .MitText(Katalogfilterprofil.SpVerwendung,
                              Verwendungstext(Spaltentext(r, "Wohngebaeude_Nicht_Wohngebaeude")))
                     .MitText(Katalogfilterprofil.SpBaujahr,
                              buchstabe.Length == 0 ? "" : klassen[KlassenIndex(buchstabe)])
                     .MitZahl(Katalogfilterprofil.SpFlaecheM2,
                              flaeche == DBNull.Value ? (double?)null : Convert.ToDouble(flaeche), 0);
                liste.Add(zeile);
            }
            return liste;
        }

        /// <summary>
        /// Der ANZEIGETEXT eines Steuerwerts der Spalte <c>Wohngebaeude_Nicht_Wohngebaeude</c>
        /// — die zwei Worte des frueheren Vorfilters „Verwendung" (<c>GEBK_VERWENDUNG_WOHN</c>,
        /// <c>GEB_TEXT_SONSTIGE</c>). Leer bleibt leer.
        ///
        /// <para><b>Nicht „Nicht Wohngebaeude"</b>: Der Trichter der Katalogliste filtert mit
        /// „enthaelt…", und „Wohngebaeude" traefe dann beide Werte. Die zwei Worte enthalten
        /// einander nicht.</para>
        /// </summary>
        public static string Verwendungstext(string steuerwert)
        {
            if (string.IsNullOrEmpty(steuerwert)) return "";
            bool wohn = string.Equals(steuerwert, FILTERWERT_WOHN, StringComparison.Ordinal);
            string schluessel = wohn ? "GEBK_VERWENDUNG_WOHN" : "GEB_TEXT_SONSTIGE";
            string text = null;
            try { text = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(text) ? (wohn ? "Wohngebäude" : "Gewerbe+Sonstige") : text;
        }

        /// <summary>Die zwei Steuerwerte der Spalte <c>Wohngebaeude_Nicht_Wohngebaeude</c> — nie uebersetzt.</summary>
        public const string FILTERWERT_WOHN = "Wohngebaeude";

        /// <summary>Der zweite Steuerwert: Gewerbe und Sonstige.</summary>
        public const string FILTERWERT_SONSTIGE = "Nicht Wohngebaeude";

        private static string Spaltentext(DataRow r, string spalte)
            => r.Table.Columns.Contains(spalte) && r[spalte] != DBNull.Value ? Convert.ToString(r[spalte]) ?? "" : "";

        /// <summary>
        /// <b>„Duplizieren…"</b> (Stufe 5; Entscheid AD-Q11) — der Gebaeudesatz als EIGENER
        /// Satz unter <paramref name="neuerName"/>, alle Spalten ausser ID, Bezeichner und
        /// ReadOnly. Die Regel steht einmal in <see cref="Katalogkopie.Duplizieren(string, int, string, Katalogkopie.Kindtabelle[])"/>.
        /// </summary>
        public static Katalogkopie.Ergebnis Duplizieren(int id, string neuerName)
            => Katalogkopie.Duplizieren(TABLE, id, neuerName);

        /// <summary>
        /// <b>„Schloss setzen…" / „Schloss aufheben…"</b> (Entscheid AD-Q15): schaltet das
        /// Auslieferungskennzeichen der Sätze <paramref name="ids"/> — nur den Kopfsatz, kein
        /// Wert ändert sich. Die Regel steht einmal in
        /// <see cref="Auslieferungskennzeichen.SetzenInTabelle"/>; hier steht nur die Tabelle.
        /// </summary>
        public static Auslieferungskennzeichen.Ergebnis SchlossSetzen(IReadOnlyList<int> ids, bool gesperrt)
            => Auslieferungskennzeichen.SetzenInTabelle(TABLE, ids, gesperrt);

        /// <summary>
        /// <b>Welche Projekte ein Gebaeude fuehren</b> (Stufe 5, V8: die weiche Loeschsperre
        /// nennt das Projekt) — je KATALOGNAME die Projektnamen, EINE Abfrage fuer die ganze
        /// Liste. Ein Projekt fuehrt eine KOPIE des Katalogsatzes (<c>Tab_Gebaeude</c>).
        ///
        /// <para><b>Zuerst die ID, dann der Name</b> (Schemaschritt 121, Welle #468; Konzept
        /// Administrationsdialoge 7.1 (a)). Traegt die Kopie ihren Katalogverweis
        /// (<c>ID_Gebaeude_Stamm</c>), kommt der Schluessel aus dem KATALOGSATZ selbst — er
        /// ueberlebt die Umbenennung des Satzes wie die der Kopie. Nur eine Kopie OHNE Verweis
        /// (Altbestand, deren Name beim Nachtrag keinen Katalogsatz traf, ein geloeschter
        /// Katalogsatz, ein Paket ohne Treffer am Ziel) sperrt weiter ueber ihren Namen —
        /// gross/klein egal, wie bisher.</para>
        /// </summary>
        public static IReadOnlyDictionary<string, IReadOnlyList<string>> Projektverwendung()
        {
            var sammlung = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            DataTable dt = DataRepository.GetDataTable(
                "SELECT COALESCE(s.Bezeichner, g.Gebaeudename) AS Katalogname, p.Projektname " +
                "FROM [" + TABLE_PROJ + "] AS g " +
                "INNER JOIN Tab_Projekt AS p ON p.ID = g.ID_Projekt " +
                "LEFT JOIN [" + TABLE + "] AS s ON s.ID = g.ID_Gebaeude_Stamm " +
                "ORDER BY p.Projektname");
            if (dt != null)
            {
                foreach (DataRow r in dt.Rows)
                {
                    string name = Spaltentext(r, "Katalogname").Trim();
                    string projekt = Spaltentext(r, "Projektname");
                    if (name.Length == 0) continue;
                    if (!sammlung.TryGetValue(name, out List<string> projekte))
                        sammlung[name] = projekte = new List<string>();
                    if (!projekte.Contains(projekt)) projekte.Add(projekt);
                }
            }

            var ergebnis = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, List<string>> p in sammlung) ergebnis[p.Key] = p.Value;
            return ergebnis;
        }

        /// <summary>
        /// <b>Die Loeschsperre eines Katalogsatzes</b>: die Projekte, die ihn fuehren — leer,
        /// wenn keines. Dieselbe Wahrheit wie <see cref="Projektverwendung"/> (zuerst der
        /// Katalogverweis, dann der Name); die Verwaltung nennt diese Projekte im Sperrgrund.
        /// </summary>
        public static IReadOnlyList<string> Loeschsperre(string bezeichner)
        {
            if (string.IsNullOrWhiteSpace(bezeichner)) return Array.Empty<string>();
            return Projektverwendung().TryGetValue(bezeichner.Trim(), out IReadOnlyList<string> projekte)
                ? projekte : Array.Empty<string>();
        }

        /// <summary>
        /// Loescht einen Katalogsatz OHNE Rueckmeldung ueber einen Kasten — der Weg der
        /// Gebaeudeverwaltung (Stufe 5): Die Oberflaeche sperrt Auslieferungssaetze und
        /// benutzte Gebaeude weich und fragt vorher zurueck; <see cref="Delete"/> meldete die
        /// Sperre ueber <c>Meldung.Hinweis</c>, und das waere in der WebView ein modaler Kasten.
        ///
        /// <para><b>Die Sperre haelt auch hier</b> (Welle #468): Ein Satz, den ein Projekt
        /// fuehrt (<see cref="Loeschsperre"/>), wird nicht geloescht — die Oberflaeche reicht
        /// ihn ohnehin nicht herein, aber der Kern verlaesst sich nicht darauf.</para>
        /// </summary>
        /// <returns><c>true</c>, wenn der Satz geloescht wurde.</returns>
        public static bool Loeschen(string bezeichner)
        {
            if (string.IsNullOrEmpty(bezeichner)) return false;
            if (new GebaeudeStammCtrl().IsReadOnly(bezeichner)) return false;
            if (Loeschsperre(bezeichner).Count > 0) return false;
            return DataRepository.ExecuteSQL(
                "DELETE FROM [" + TABLE + "] WHERE Bezeichner = ? AND ReadOnly = 0",
                new DbParam("@bez", bezeichner));
        }

        #endregion
    }
}
