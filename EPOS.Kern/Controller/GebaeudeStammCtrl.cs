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

        /// <summary>
        /// Liest die PROJEKTKOPIE eines Gebäudes (<c>Tab_Gebaeude</c>) nach ihrer Id in ein frisches
        /// Modell — derselbe namensbasierte Leser wie für den Katalog (Namensspalte
        /// <c>Gebaeudename</c>, die neuen Spalten NULL-erhaltend); <c>null</c>, wenn es die Zeile
        /// nicht gibt. Der Gebäudedialog bearbeitet diesen Satz in der Betriebsart Projekt
        /// (Stufe G3, Welle D2).
        /// </summary>
        public static GebaeudeModel LiesProjektkopie(int idGebaeude)
        {
            if (idGebaeude <= 0) return null;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + TABLE_PROJ + "] WHERE [ID] = ?",
                new DbParam("@id", idGebaeude));
            if (dt == null || dt.Rows.Count == 0) return null;
            var item = new GebaeudeModel();
            new GebaeudeStammCtrl().FillModel(item, dt.Rows[0]);
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
        /// Die Fläche je Nutzer [m²], mit der ein Gebäude ohne diese Angabe gespeichert wird — die
        /// Regel „Flaeche_Nutzer == 0 → 35" des Vorläufers (<c>GebaeudeKatalogHuelle.NachModell</c>).
        /// Der Gebäudeimport setzt sie als ausgewiesene Vorgabe, wenn die Datei keine vollständige
        /// Personenangabe trägt (<c>GebaeudeAggregation</c>). Die eine Stelle der Zahl.
        /// </summary>
        public const double FLAECHE_JE_NUTZER_VORGABE = 35.0;

        /// <summary>
        /// Die inneren Wärmegewinne je m² Nutzfläche [W/m²], die der Gebäudeimport als ausgewiesene,
        /// änderbare Vorgabe setzt (Entscheid E43: ein Wert für alle Gebäudearten) —
        /// <c>Interne_Waermegewinne = 5 W/m² × Nutzfläche</c>. Die eine Stelle der Zahl.
        /// </summary>
        public const double INNERE_GEWINNE_JE_M2_VORGABE = 5.0;

        /// <summary>Der Heizsollwert am Tag [°C], den der Gebäudeimport vorgibt, wenn die Datei keinen trägt (E43).</summary>
        public const double SOLLTEMPERATUR_TAG_VORGABE = 20.0;

        /// <summary>
        /// Der Heizsollwert der Nachtabsenkung [°C], den der Gebäudeimport vorgibt (E43) — höchstens
        /// der Tagsollwert. Die Nachtzeit dazu ist <see cref="Nachtzeit.Vorgabe"/> (22 bis 6 Uhr).
        /// </summary>
        public const double SOLLTEMPERATUR_NACHT_VORGABE = 18.0;

        /// <summary>
        /// Die 13 Baualtersklassen A bis M in ihrer festen Reihenfolge (Entscheid E47) — Index 0 ist
        /// <c>'A'</c> (bis 1859), Index 12 ist <c>'M'</c> (ab 2021). Liste, Texte und Regeln stehen bei
        /// <see cref="Gebaeudeklassen"/>; die Namen hier bleiben fuer die Bestandsaufrufer.
        /// </summary>
        public static IReadOnlyList<string> Baualtersklassen() => Gebaeudeklassen.Texte();

        /// <summary>Die deutschen Texte der 13 Klassen — der Rueckfall, solange ein Schluessel fehlt (<see cref="Gebaeudeklassen.TEXTE_DE"/>).</summary>
        public static readonly string[] BAUALTERSKLASSEN_DE = System.Linq.Enumerable.ToArray(Gebaeudeklassen.TEXTE_DE);

        /// <summary>Der Buchstabe zu einem Listenindex (<see cref="Gebaeudeklassen.Buchstabe"/>); ausserhalb der Liste <c>'A'</c>.</summary>
        public static char KlassenBuchstabe(int index) => Gebaeudeklassen.Buchstabe(index);

        /// <summary>Der Listenindex zu einer gespeicherten Baualtersklasse (<see cref="Gebaeudeklassen.Index"/>); leer und unbekannt 0.</summary>
        public static int KlassenIndex(string baualtersklasse) => Gebaeudeklassen.Index(baualtersklasse);

        /// <summary>Der Klartext einer gespeicherten Klasse; leer und unbekannt bleiben leer (<see cref="Gebaeudeklassen.Text"/>).</summary>
        public static string Klassentext(string baualtersklasse) => Gebaeudeklassen.Text(baualtersklasse);

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
                // AK-S1 (Schemaschritt 122): NULL-erhaltend, die zwei Schalter 0/1
                new DbParam("@b71", DbParamTyp.Boolean) { Wert = m.Heizkreis_Aktiv },
                new DbParam("@b72", DbParamTyp.VarWChar) { Wert = Wert(m.Uebergabe_Art) },
                new DbParam("@b73", DbParamTyp.Double) { Wert = Wert(m.Uebergabe_Exponent) },
                new DbParam("@b74", DbParamTyp.Double) { Wert = Wert(m.Uebergabe_Leistung_Nenn) },
                new DbParam("@b75", DbParamTyp.Double) { Wert = Wert(m.Auslegung_Vorlauf) },
                new DbParam("@b76", DbParamTyp.Double) { Wert = Wert(m.Auslegung_Ruecklauf) },
                new DbParam("@b77", DbParamTyp.Double) { Wert = Wert(m.Auslegung_Raumtemperatur) },
                new DbParam("@b78", DbParamTyp.Double) { Wert = Wert(m.Auslegung_Aussentemperatur) },
                new DbParam("@b79", DbParamTyp.Boolean) { Wert = m.Heizkurve_Aktiv },
                new DbParam("@b80", DbParamTyp.Double) { Wert = Wert(m.Heizkurve_Niveau) },
                new DbParam("@b81", DbParamTyp.Double) { Wert = Wert(m.Heizkurve_Steilheit) },
                new DbParam("@b82", DbParamTyp.Double) { Wert = Wert(m.Regler_Proportionalband) },
                new DbParam("@b83", DbParamTyp.LongVarWChar) { Wert = Wert(m.Sollwertprofil) },
                // KAK-S1 (E37): NULL-erhaltend, der Schalter 0/1
                new DbParam("@b84", DbParamTyp.Boolean) { Wert = m.Kuehluebergabe_Aktiv },
                new DbParam("@b85", DbParamTyp.VarWChar) { Wert = Wert(m.Kuehl_Uebergabe_Art) },
                new DbParam("@b86", DbParamTyp.Double) { Wert = Wert(m.Kuehl_Uebergabe_Exponent) },
                new DbParam("@b87", DbParamTyp.Double) { Wert = Wert(m.Kuehl_Uebergabe_Leistung_Nenn) },
                new DbParam("@b88", DbParamTyp.Double) { Wert = Wert(m.Kuehl_Auslegung_Vorlauf) },
                new DbParam("@b89", DbParamTyp.Double) { Wert = Wert(m.Kuehl_Auslegung_Ruecklauf) },
                new DbParam("@b90", DbParamTyp.Double) { Wert = Wert(m.Kuehl_Auslegung_Raumtemperatur) },
                new DbParam("@b91", DbParamTyp.Double) { Wert = Wert(m.Kuehl_Vorlaufgrenze) },
                // Das Baujahr (G4a, BaujahrSchema.SCHRITT): NULL-erhaltend, NULL = unbekannt
                new DbParam("@b92", DbParamTyp.Integer) { Wert = Wert(m.Baujahr) },
                // Die Nachtzeit (E43, NachtzeitSchema.SCHRITT): NULL-erhaltend, beide NULL = Vorgabe 22 bis 6 Uhr
                new DbParam("@b93", DbParamTyp.Integer) { Wert = Wert(m.Nachtabsenkung_Beginn) },
                new DbParam("@b94", DbParamTyp.Integer) { Wert = Wert(m.Nachtabsenkung_Ende) },
                // Der Energiestandard (E47, BaualtersklassenSchema.SCHRITT): der Code, leer = NULL (keiner)
                new DbParam("@b95", DbParamTyp.VarWChar) { Wert = Standardwert(m.Energiestandard) },
                // Der wirksame U-Wert der Bodenplatte (E65, ErdreichVorgabeSchema.SCHRITT): NULL-erhaltend,
                // NULL = Erdreichkorrektur nach DIN EN ISO 13370
                new DbParam("@b96", DbParamTyp.Double) { Wert = Wert(m.Erdreich_U_Wirksam) },
                // Der Raumeinfluss der Heizkurve (AK3, Ak3Schema.SCHRITT): NULL-erhaltend, NULL = aus
                new DbParam("@b97", DbParamTyp.Double) { Wert = Wert(m.Heizkurve_Raumeinfluss) },
                // Die Kuehlkurve (KK, KuehlkurveSchema.SCHRITT): der Schalter 0/1, die Werte NULL-erhaltend,
                // NULL = Vorgabe (Fusspunkt: Auslegungsruecklauf; Raumeinfluss: aus; Weg: tagesmittel)
                new DbParam("@b98", DbParamTyp.Boolean) { Wert = m.Kuehlkurve_Aktiv },
                new DbParam("@b99", DbParamTyp.Double) { Wert = Wert(m.Kuehlkurve_Fusspunkt) },
                new DbParam("@b100", DbParamTyp.Double) { Wert = Wert(m.Kuehlkurve_Raumeinfluss) },
                new DbParam("@b101", DbParamTyp.VarWChar) { Wert = Standardwert(m.Kuehlkurve_Auslegung_Weg) },
                new DbParam("@b102", DbParamTyp.Double) { Wert = Wert(m.Kuehlkurve_Auslegung_Aussen) },
            };
        }

        /// <summary>Ein nullbarer Wert als Parameterwert - NULL bleibt NULL (Vorgabe).</summary>
        private static object Wert(double? x) => x.HasValue ? (object)x.Value : DBNull.Value;

        /// <summary>Eine nullbare Ganzzahl als Parameterwert - NULL bleibt NULL (unbekannt).</summary>
        private static object Wert(int? x) => x.HasValue ? (object)x.Value : DBNull.Value;

        /// <summary>Ein Text als Parameterwert - NULL bleibt NULL (Vorgabe).</summary>
        private static object Wert(string x) => x != null ? (object)x : DBNull.Value;

        /// <summary>
        /// Der Energiestandard als Parameterwert (E47): der Code, leer oder <c>null</c> wird NULL ("keiner") -
        /// die Spalte haelt mit ihrem <c>CHECK</c> nur die elf Codes und NULL, nie einen Leertext.
        /// </summary>
        private static object Standardwert(string code) => string.IsNullOrEmpty(code) ? DBNull.Value : (object)code;

        // Legt einen neuen Gebaeude-Stammdatensatz an. ID explizit als MAX(ID)+1
        // (beim Kopieren einer Access-Tabelle wird die Autonummerierung zu einer normalen Long-Zahl).
        public bool Insert(GebaeudeModel m) => Insert(m, out _);

        /// <summary>
        /// Dieselbe Anlage, dazu die ID des neuen Satzes — „Speichern unter" braucht sie, um dem
        /// neuen Katalogbau seine Konditionierung mitzugeben (<see cref="SpeichernUnter"/>).
        /// <paramref name="neueId"/> ist 0, wenn nichts angelegt wurde.
        /// </summary>
        public bool Insert(GebaeudeModel m, out int neueId)
        {
            int newId = DataRepository.GetMaxID(TABLE) + 1;
            neueId = newId;
            string sql = "INSERT INTO [" + TABLE + "] ([ID], [Bezeichner], [Typ], [Beschreibung], [Wohnflaeche_gesamt], [Bewohner], [Flaeche_Nutzer], [Interne_Waermegewinne], [Bauweise], [Fensterflaeche_Sued], [Fensterflaeche_Ost_West], [Fensterflaeche_Nord], [Fensterdurchlassgrad], [Raumsolltemperatur_Nachtabsenkung], [Raumsolltemperatur_Tag], [Raumsolltemperatur_Wochenende], [Raumsolltemperatur_Ferien], [Maximaleraumtemperatur], [k_Wert_Außenwand], [k_Wert_Fenster], [k_Wert_Dachflaeche], [k_Wert_Grundflaeche], [k_Wert_Sonstiges], [Flaeche_Außenwand], [gesamte_Fensterflaeche], [Dachflaeche], [Grundflaeche], [Sonstige_Flaechen], [Nutzflaeche], [Raumhoehe], [WBVK_Anschluß_Fenster_Wand], [WBVK_Anschluß_Wand_Dach], [WBVK_Anschluß_Außenwand_Kellerdecke], [Abmessung_Anschluß_Fenster_Wand], [Abmessung_Anschluß_Wand_Dach], [Abmessung_Anschluß_Außenwand_Kellerdecke], [Luftwechselrate], [Wochenende], [Ferien], [Ferienbeginn_1], [Ferienende_1], [Ferienbeginn_2], [Ferienende_2], [Ferienbeginn_3], [Ferienende_3], [Ferienbeginn_4], [Ferienende_4], [WW_Bedarf], [spez_Waermeverbrauch], [Waermebedarf], [Baualtersklasse], [Gebaeudeart], [Wohngebaeude_Nicht_Wohngebaeude], [Gebaeude_Modell], [Fensterflaeche_Ost], [Fensterflaeche_West], [Rahmenanteil], [Verschattungsfaktor], [Grundflaeche_Randbedingung], [Kellertemperatur], [Masseanteil_Aussen], [Innenflaechenfaktor], [Heizung_Strahlungsanteil], [Heizleistung_Max], [Aussenbauteile_Strahlung], [Luftwechsel_Infiltration], [Luftwechsel_Nutzer], [Sommerlueftung], [Kuehl_Sollwert], [Kuehlleistung_Max], [Kuehlung_Aktiv], [Kuehl_Sollwert_Nacht], [Heizkreis_Aktiv], [Uebergabe_Art], [Uebergabe_Exponent], [Uebergabe_Leistung_Nenn], [Auslegung_Vorlauf], [Auslegung_Ruecklauf], [Auslegung_Raumtemperatur], [Auslegung_Aussentemperatur], [Heizkurve_Aktiv], [Heizkurve_Niveau], [Heizkurve_Steilheit], [Regler_Proportionalband], [Sollwertprofil], [Kuehluebergabe_Aktiv], [Kuehl_Uebergabe_Art], [Kuehl_Uebergabe_Exponent], [Kuehl_Uebergabe_Leistung_Nenn], [Kuehl_Auslegung_Vorlauf], [Kuehl_Auslegung_Ruecklauf], [Kuehl_Auslegung_Raumtemperatur], [Kuehl_Vorlaufgrenze], [Baujahr], [Nachtabsenkung_Beginn], [Nachtabsenkung_Ende], [Energiestandard], [Erdreich_U_Wirksam], [Heizkurve_Raumeinfluss], [Kuehlkurve_Aktiv], [Kuehlkurve_Fusspunkt], [Kuehlkurve_Raumeinfluss], [Kuehlkurve_Auslegung_Weg], [Kuehlkurve_Auslegung_Aussen], [ReadOnly]) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?,?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
            var ps = new List<DbParam>();
            ps.Add(new DbParam("@bid", DbParamTyp.Integer) { Wert = newId });
            ps.AddRange(BuildValueParams(m));
            ps.Add(new DbParam("@bro", DbParamTyp.Boolean) { Wert = false });
            bool ok = DataRepository.ExecuteSQL(sql, ps.ToArray());
            if (!ok) neueId = 0;
            return ok;
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
            string sql = "UPDATE [" + TABLE + "] SET " + SET_SPALTEN + " WHERE Bezeichner = ?";
            var ps = new List<DbParam>(BuildValueParams(m));
            ps.Add(new DbParam("@bkey", DbParamTyp.VarWChar) { Wert = (object)(m.Gebaeudename ?? "") });
            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        /// <summary>
        /// Die Spalten eines Gebäudesatzes in der Reihenfolge von <see cref="BuildValueParams"/> —
        /// die SET-Liste von <see cref="Overwrite"/> (Katalog, Namensspalte <c>Bezeichner</c>) und
        /// von <see cref="ProjektkopieUeberschreiben"/> (Projektkopie, Namensspalte <c>Gebaeudename</c>).
        /// </summary>
        private const string SET_SPALTEN = "[Bezeichner] = ?, [Typ] = ?, [Beschreibung] = ?, [Wohnflaeche_gesamt] = ?, [Bewohner] = ?, [Flaeche_Nutzer] = ?, [Interne_Waermegewinne] = ?, [Bauweise] = ?, [Fensterflaeche_Sued] = ?, [Fensterflaeche_Ost_West] = ?, [Fensterflaeche_Nord] = ?, [Fensterdurchlassgrad] = ?, [Raumsolltemperatur_Nachtabsenkung] = ?, [Raumsolltemperatur_Tag] = ?, [Raumsolltemperatur_Wochenende] = ?, [Raumsolltemperatur_Ferien] = ?, [Maximaleraumtemperatur] = ?, [k_Wert_Außenwand] = ?, [k_Wert_Fenster] = ?, [k_Wert_Dachflaeche] = ?, [k_Wert_Grundflaeche] = ?, [k_Wert_Sonstiges] = ?, [Flaeche_Außenwand] = ?, [gesamte_Fensterflaeche] = ?, [Dachflaeche] = ?, [Grundflaeche] = ?, [Sonstige_Flaechen] = ?, [Nutzflaeche] = ?, [Raumhoehe] = ?, [WBVK_Anschluß_Fenster_Wand] = ?, [WBVK_Anschluß_Wand_Dach] = ?, [WBVK_Anschluß_Außenwand_Kellerdecke] = ?, [Abmessung_Anschluß_Fenster_Wand] = ?, [Abmessung_Anschluß_Wand_Dach] = ?, [Abmessung_Anschluß_Außenwand_Kellerdecke] = ?, [Luftwechselrate] = ?, [Wochenende] = ?, [Ferien] = ?, [Ferienbeginn_1] = ?, [Ferienende_1] = ?, [Ferienbeginn_2] = ?, [Ferienende_2] = ?, [Ferienbeginn_3] = ?, [Ferienende_3] = ?, [Ferienbeginn_4] = ?, [Ferienende_4] = ?, [WW_Bedarf] = ?, [spez_Waermeverbrauch] = ?, [Waermebedarf] = ?, [Baualtersklasse] = ?, [Gebaeudeart] = ?, [Wohngebaeude_Nicht_Wohngebaeude] = ?, [Gebaeude_Modell] = ?, [Fensterflaeche_Ost] = ?, [Fensterflaeche_West] = ?, [Rahmenanteil] = ?, [Verschattungsfaktor] = ?, [Grundflaeche_Randbedingung] = ?, [Kellertemperatur] = ?, [Masseanteil_Aussen] = ?, [Innenflaechenfaktor] = ?, [Heizung_Strahlungsanteil] = ?, [Heizleistung_Max] = ?, [Aussenbauteile_Strahlung] = ?, [Luftwechsel_Infiltration] = ?, [Luftwechsel_Nutzer] = ?, [Sommerlueftung] = ?, [Kuehl_Sollwert] = ?, [Kuehlleistung_Max] = ?, [Kuehlung_Aktiv] = ?, [Kuehl_Sollwert_Nacht] = ?, [Heizkreis_Aktiv] = ?, [Uebergabe_Art] = ?, [Uebergabe_Exponent] = ?, [Uebergabe_Leistung_Nenn] = ?, [Auslegung_Vorlauf] = ?, [Auslegung_Ruecklauf] = ?, [Auslegung_Raumtemperatur] = ?, [Auslegung_Aussentemperatur] = ?, [Heizkurve_Aktiv] = ?, [Heizkurve_Niveau] = ?, [Heizkurve_Steilheit] = ?, [Regler_Proportionalband] = ?, [Sollwertprofil] = ?, [Kuehluebergabe_Aktiv] = ?, [Kuehl_Uebergabe_Art] = ?, [Kuehl_Uebergabe_Exponent] = ?, [Kuehl_Uebergabe_Leistung_Nenn] = ?, [Kuehl_Auslegung_Vorlauf] = ?, [Kuehl_Auslegung_Ruecklauf] = ?, [Kuehl_Auslegung_Raumtemperatur] = ?, [Kuehl_Vorlaufgrenze] = ?, [Baujahr] = ?, [Nachtabsenkung_Beginn] = ?, [Nachtabsenkung_Ende] = ?, [Energiestandard] = ?, [Erdreich_U_Wirksam] = ?, [Heizkurve_Raumeinfluss] = ?, [Kuehlkurve_Aktiv] = ?, [Kuehlkurve_Fusspunkt] = ?, [Kuehlkurve_Raumeinfluss] = ?, [Kuehlkurve_Auslegung_Weg] = ?, [Kuehlkurve_Auslegung_Aussen] = ?";

        /// <summary>
        /// <b>Überschreibt die PROJEKTKOPIE eines Gebäudes</b> (<c>Tab_Gebaeude</c>, Stufe G3,
        /// Welle D2) — die Gebäudewerte, die der Gebäudedialog in der Betriebsart Projekt bearbeitet,
        /// mit denselben Spalten und Werten wie das Überschreiben eines Katalogsatzes
        /// (<see cref="BuildValueParams"/>, NULL-erhaltend). Die Zeile wird GEÄNDERT, nie gelöscht
        /// und neu angelegt: Id, Projekt, Zuordnung, Katalogverweis, Herkunft und die Zonen der Kopie
        /// (Kaskade) bleiben. Getroffen wird genau die Zeile <paramref name="idGebaeude"/> des
        /// Projekts <paramref name="idProjekt"/>; <c>false</c>, wenn es sie nicht gibt oder das
        /// Schreiben scheitert. Die Veraltung des Projekts setzt der Aufrufer.
        /// </summary>
        public static bool ProjektkopieUeberschreiben(int idGebaeude, int idProjekt, GebaeudeModel m)
        {
            if (m == null || idGebaeude <= 0) return false;
            object da = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE_PROJ + "] WHERE [ID] = ? AND [ID_Projekt] = ?",
                new DbParam("@id", idGebaeude), new DbParam("@p", idProjekt));
            if (da == null || Convert.ToInt64(da, System.Globalization.CultureInfo.InvariantCulture) != 1) return false;

            string sql = "UPDATE [" + TABLE_PROJ + "] SET " + SET_SPALTEN.Replace("[Bezeichner] = ?", "[Gebaeudename] = ?") +
                         " WHERE [ID] = ? AND [ID_Projekt] = ?";
            var ps = new List<DbParam>(new GebaeudeStammCtrl().BuildValueParams(m));
            ps.Add(new DbParam("@gid", DbParamTyp.Integer) { Wert = idGebaeude });
            ps.Add(new DbParam("@pid", DbParamTyp.Integer) { Wert = idProjekt });
            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        #endregion

        #region --- Gebaeudespalten-Schritt M3 (NULL-erhaltend) ---

        /// <summary>
        /// Liest die fuenfzehn Spalten des Gebaeudespalten-Schritts M3 (Schemaschritt 101),
        /// die vier Kuehleingaben aus KU-S1 (Schemaschritt 108), die dreizehn Spalten der
        /// Waermeuebergabe aus AK-S1 (Schemaschritt 122), die acht der Kuehluebergabe aus
        /// KAK-S1 (E37) und das Baujahr (G4a) NULL-ERHALTEND in das Modell: NULL bleibt
        /// <c>null</c>, die sechs Schalter werden 0/1. Fehlt eine Spalte
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

            // AK-S1 (Schemaschritt 122, Anlagenkopplung 8.1): die dreizehn Spalten der
            // Waermeuebergabe, ebenso NULL-ERHALTEND; die zwei Schalter werden 0/1.
            item.Heizkreis_Aktiv = Schalter(row, GebaeudeSchema.SPALTE_HEIZKREIS_AKTIV);
            item.Uebergabe_Art = Text(row, GebaeudeSchema.SPALTE_UEBERGABE_ART);
            item.Uebergabe_Exponent = Zahl(row, GebaeudeSchema.SPALTE_UEBERGABE_EXPONENT);
            item.Uebergabe_Leistung_Nenn = Zahl(row, GebaeudeSchema.SPALTE_UEBERGABE_LEISTUNG_NENN);
            item.Auslegung_Vorlauf = Zahl(row, GebaeudeSchema.SPALTE_AUSLEGUNG_VORLAUF);
            item.Auslegung_Ruecklauf = Zahl(row, GebaeudeSchema.SPALTE_AUSLEGUNG_RUECKLAUF);
            item.Auslegung_Raumtemperatur = Zahl(row, GebaeudeSchema.SPALTE_AUSLEGUNG_RAUMTEMPERATUR);
            item.Auslegung_Aussentemperatur = Zahl(row, GebaeudeSchema.SPALTE_AUSLEGUNG_AUSSENTEMPERATUR);
            item.Heizkurve_Aktiv = Schalter(row, GebaeudeSchema.SPALTE_HEIZKURVE_AKTIV);
            item.Heizkurve_Niveau = Zahl(row, GebaeudeSchema.SPALTE_HEIZKURVE_NIVEAU);
            item.Heizkurve_Steilheit = Zahl(row, GebaeudeSchema.SPALTE_HEIZKURVE_STEILHEIT);
            item.Regler_Proportionalband = Zahl(row, GebaeudeSchema.SPALTE_REGLER_PROPORTIONALBAND);
            item.Sollwertprofil = Text(row, GebaeudeSchema.SPALTE_SOLLWERTPROFIL);

            // KAK-S1 (E37, Anlagenkopplung 8.1): die acht Spalten der Kuehluebergabe, ebenso
            // NULL-ERHALTEND; der Schalter wird 0/1 (A1).
            item.Kuehluebergabe_Aktiv = Schalter(row, GebaeudeSchema.SPALTE_KUEHLUEBERGABE_AKTIV);
            item.Kuehl_Uebergabe_Art = Text(row, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_ART);
            item.Kuehl_Uebergabe_Exponent = Zahl(row, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_EXPONENT);
            item.Kuehl_Uebergabe_Leistung_Nenn = Zahl(row, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_LEISTUNG_NENN);
            item.Kuehl_Auslegung_Vorlauf = Zahl(row, GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_VORLAUF);
            item.Kuehl_Auslegung_Ruecklauf = Zahl(row, GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_RUECKLAUF);
            item.Kuehl_Auslegung_Raumtemperatur = Zahl(row, GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_RAUMTEMPERATUR);
            item.Kuehl_Vorlaufgrenze = Zahl(row, GebaeudeSchema.SPALTE_KUEHL_VORLAUFGRENZE);

            // Das Baujahr (G4a, BaujahrSchema.SCHRITT): NULL-ERHALTEND, null = unbekannt.
            item.Baujahr = Ganzzahl(row, GebaeudeSchema.SPALTE_BAUJAHR);

            // Die Nachtzeit (E43, NachtzeitSchema.SCHRITT): NULL-ERHALTEND, beide null = Vorgabe.
            item.Nachtabsenkung_Beginn = Ganzzahl(row, GebaeudeSchema.SPALTE_NACHTABSENKUNG_BEGINN);
            item.Nachtabsenkung_Ende = Ganzzahl(row, GebaeudeSchema.SPALTE_NACHTABSENKUNG_ENDE);

            // Der Energiestandard (E47, BaualtersklassenSchema.SCHRITT): NULL-ERHALTEND, null = keiner;
            // ein Leertext gilt wie NULL.
            string standard = Text(row, GebaeudeSchema.SPALTE_ENERGIESTANDARD);
            item.Energiestandard = string.IsNullOrEmpty(standard) ? null : standard;

            // Der wirksame U-Wert der Bodenplatte (E65, ErdreichVorgabeSchema.SCHRITT): NULL-ERHALTEND,
            // null = Erdreichkorrektur nach DIN EN ISO 13370.
            item.Erdreich_U_Wirksam = Zahl(row, GebaeudeSchema.SPALTE_ERDREICH_U_WIRKSAM);

            // Der Raumeinfluss der Heizkurve (AK3, Ak3Schema.SCHRITT): NULL-ERHALTEND, null = aus.
            item.Heizkurve_Raumeinfluss = Zahl(row, GebaeudeSchema.SPALTE_HEIZKURVE_RAUMEINFLUSS);

            // Die Kuehlkurve (KK, KuehlkurveSchema.SCHRITT): NULL-ERHALTEND, der Schalter NULL = aus.
            item.Kuehlkurve_Aktiv = Schalter(row, GebaeudeSchema.SPALTE_KUEHLKURVE_AKTIV);
            item.Kuehlkurve_Fusspunkt = Zahl(row, GebaeudeSchema.SPALTE_KUEHLKURVE_FUSSPUNKT);
            item.Kuehlkurve_Raumeinfluss = Zahl(row, GebaeudeSchema.SPALTE_KUEHLKURVE_RAUMEINFLUSS);
            string weg = Text(row, GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_WEG);
            item.Kuehlkurve_Auslegung_Weg = string.IsNullOrEmpty(weg) ? null : weg;
            item.Kuehlkurve_Auslegung_Aussen = Zahl(row, GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_AUSSEN);
        }

        private static object Roh(DataRow row, string spalte)
            => row.Table.Columns.Contains(spalte) ? row[spalte] : DBNull.Value;

        private static int? Ganzzahl(DataRow row, string spalte)
        {
            object w = Roh(row, spalte);
            return w == DBNull.Value ? (int?)null : Convert.ToInt32(w, System.Globalization.CultureInfo.InvariantCulture);
        }

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
        /// <b>Der Katalogsatz, aus dem eine Projektkopie entsteht</b> — zuerst über seine Id, ohne Id
        /// oder ohne Satz dieser Id über den Namen; <c>null</c>, wenn es keinen gibt. EINE Suchregel
        /// für <see cref="CopyFromStamm(int?, string, int, int)"/> und den Arbeitsstand eines noch
        /// nicht gespeicherten Gebäudes (<see cref="GebaeudeBedarfCtrl.Arbeitsstandgebaeude"/>): Beide
        /// finden denselben Satz. Schreibt nichts.
        /// </summary>
        internal static DataRow Katalogzeile(int? idStamm, string szBezeichner)
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
            if (dt == null || dt.Rows.Count == 0) return null;
            return dt.Rows[0];
        }

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
            DataRow r = Katalogzeile(idStamm, szBezeichner);
            if (r == null) return 0;

            // Stufe KP1b (Konzept 5.5): Kopf, Tagesverteilung UND Konditionierung in DERSELBEN
            // Transaktion — scheitert ein Schritt, bleibt keine halbe Kopie zurück. Unter dem
            // Assistenten läuft schon eine Klammer; daraus wird ein Sicherungspunkt statt einer
            // zweiten Verbindung an der Schreibsperre (Vorgangsklammer).
            using (DbVorgang vorgang = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang))
            {
                try
                {
                    int id = Kopfkopie(r, idProjekt, idProjektGebaeude);
                    if (id == 0)
                    {
                        vorgang.Rollback();
                        return 0;
                    }

                    Konditionierungskopie.Befund befund = Konditionierungskopie.Kopieren(
                        vorgang,
                        KonditionierungCtrl.Eigner.Katalogbau(Convert.ToInt64(r["ID"])),
                        KonditionierungCtrl.Eigner.Gebaeude(id),
                        Konditionierungskopie.Auswahl.Alles);
                    if (!befund.Ok)
                    {
                        vorgang.Rollback();
                        return 0;
                    }

                    vorgang.Commit();
                    return id;
                }
                catch (Exception)
                {
                    vorgang.Rollback();
                    return 0;
                }
            }
        }

        /// <summary>
        /// Der Kopfsatz der Projektkopie samt Tagesverteilung — der bisherige Rumpf von
        /// <see cref="CopyFromStamm(int?, string, int, int)"/>, unverändert und ohne eigene
        /// Transaktion: Die Klammer hält der Aufrufer.
        /// </summary>
        private int Kopfkopie(DataRow r, int idProjekt, int idProjektGebaeude)
        {
            int newId = DataRepository.GetMaxID(TABLE_PROJ) + 1;

            string sql = "INSERT INTO [" + TABLE_PROJ + "] ([ID], [ID_ProjektGebaeude], [ID_Projekt], [Gebaeudename], [Typ], [Beschreibung], [Wohnflaeche_gesamt], [Bewohner], [Flaeche_Nutzer], [Interne_Waermegewinne], [Bauweise], [Fensterflaeche_Sued], [Fensterflaeche_Ost_West], [Fensterflaeche_Nord], [Fensterdurchlassgrad], [Raumsolltemperatur_Nachtabsenkung], [Raumsolltemperatur_Tag], [Raumsolltemperatur_Wochenende], [Raumsolltemperatur_Ferien], [Maximaleraumtemperatur], [k_Wert_Außenwand], [k_Wert_Fenster], [k_Wert_Dachflaeche], [k_Wert_Grundflaeche], [k_Wert_Sonstiges], [Flaeche_Außenwand], [gesamte_Fensterflaeche], [Dachflaeche], [Grundflaeche], [Sonstige_Flaechen], [Nutzflaeche], [Raumhoehe], [WBVK_Anschluß_Fenster_Wand], [WBVK_Anschluß_Wand_Dach], [WBVK_Anschluß_Außenwand_Kellerdecke], [Abmessung_Anschluß_Fenster_Wand], [Abmessung_Anschluß_Wand_Dach], [Abmessung_Anschluß_Außenwand_Kellerdecke], [Luftwechselrate], [Wochenende], [Ferien], [Ferienbeginn_1], [Ferienende_1], [Ferienbeginn_2], [Ferienende_2], [Ferienbeginn_3], [Ferienende_3], [Ferienbeginn_4], [Ferienende_4], [WW_Bedarf], [spez_Waermeverbrauch], [Waermebedarf], [Baualtersklasse], [Gebaeudeart], [Wohngebaeude_Nicht_Wohngebaeude], [Gebaeude_Modell], [Fensterflaeche_Ost], [Fensterflaeche_West], [Rahmenanteil], [Verschattungsfaktor], [Grundflaeche_Randbedingung], [Kellertemperatur], [Masseanteil_Aussen], [Innenflaechenfaktor], [Heizung_Strahlungsanteil], [Heizleistung_Max], [Aussenbauteile_Strahlung], [Luftwechsel_Infiltration], [Luftwechsel_Nutzer], [Sommerlueftung], [Kuehl_Sollwert], [Kuehlleistung_Max], [Kuehlung_Aktiv], [Kuehl_Sollwert_Nacht], [Heizkreis_Aktiv], [Uebergabe_Art], [Uebergabe_Exponent], [Uebergabe_Leistung_Nenn], [Auslegung_Vorlauf], [Auslegung_Ruecklauf], [Auslegung_Raumtemperatur], [Auslegung_Aussentemperatur], [Heizkurve_Aktiv], [Heizkurve_Niveau], [Heizkurve_Steilheit], [Regler_Proportionalband], [Sollwertprofil], [Kuehluebergabe_Aktiv], [Kuehl_Uebergabe_Art], [Kuehl_Uebergabe_Exponent], [Kuehl_Uebergabe_Leistung_Nenn], [Kuehl_Auslegung_Vorlauf], [Kuehl_Auslegung_Ruecklauf], [Kuehl_Auslegung_Raumtemperatur], [Kuehl_Vorlaufgrenze], [Baujahr], [Nachtabsenkung_Beginn], [Nachtabsenkung_Ende], [Energiestandard], [Erdreich_U_Wirksam], [Heizkurve_Raumeinfluss], [Kuehlkurve_Aktiv], [Kuehlkurve_Fusspunkt], [Kuehlkurve_Raumeinfluss], [Kuehlkurve_Auslegung_Weg], [Kuehlkurve_Auslegung_Aussen], [ID_Gebaeude_Stamm]) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?,?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
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
                // AK-S1 (Schemaschritt 122): dieselbe Regel - NULL bleibt NULL (ideal, Vorgabe
                // der Uebergabeart, hergeleitet, Bestandssollwerte); aus einem leeren Exponenten
                // wird kein 0,0. Die zwei Schalter gehen als 0/1 hinueber.
                new DbParam("@c74", DbParamTyp.Boolean) { Wert = Schalter(r, GebaeudeSchema.SPALTE_HEIZKREIS_AKTIV) },
                new DbParam("@c75", DbParamTyp.VarWChar) { Wert = Roh(r, GebaeudeSchema.SPALTE_UEBERGABE_ART) },
                new DbParam("@c76", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_UEBERGABE_EXPONENT) },
                new DbParam("@c77", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_UEBERGABE_LEISTUNG_NENN) },
                new DbParam("@c78", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_AUSLEGUNG_VORLAUF) },
                new DbParam("@c79", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_AUSLEGUNG_RUECKLAUF) },
                new DbParam("@c80", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_AUSLEGUNG_RAUMTEMPERATUR) },
                new DbParam("@c81", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_AUSLEGUNG_AUSSENTEMPERATUR) },
                new DbParam("@c82", DbParamTyp.Boolean) { Wert = Schalter(r, GebaeudeSchema.SPALTE_HEIZKURVE_AKTIV) },
                new DbParam("@c83", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_HEIZKURVE_NIVEAU) },
                new DbParam("@c84", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_HEIZKURVE_STEILHEIT) },
                new DbParam("@c85", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_REGLER_PROPORTIONALBAND) },
                new DbParam("@c86", DbParamTyp.LongVarWChar) { Wert = Roh(r, GebaeudeSchema.SPALTE_SOLLWERTPROFIL) },
                // KAK-S1 (E37): dieselbe Regel - NULL bleibt NULL (ideal, Vorgabe der Art,
                // Auslegungstag); der Schalter geht als 0/1 hinueber, die Art bleibt beim
                // Abschalten erhalten (A1).
                new DbParam("@c87", DbParamTyp.Boolean) { Wert = Schalter(r, GebaeudeSchema.SPALTE_KUEHLUEBERGABE_AKTIV) },
                new DbParam("@c88", DbParamTyp.VarWChar) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_ART) },
                new DbParam("@c89", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_EXPONENT) },
                new DbParam("@c90", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHL_UEBERGABE_LEISTUNG_NENN) },
                new DbParam("@c91", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_VORLAUF) },
                new DbParam("@c92", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_RUECKLAUF) },
                new DbParam("@c93", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHL_AUSLEGUNG_RAUMTEMPERATUR) },
                new DbParam("@c94", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHL_VORLAUFGRENZE) },
                // Das Baujahr (G4a, BaujahrSchema.SCHRITT): dieselbe Regel - NULL bleibt NULL
                // (unbekannt), eine Jahreszahl geht unveraendert hinueber.
                new DbParam("@c95", DbParamTyp.Integer) { Wert = Roh(r, GebaeudeSchema.SPALTE_BAUJAHR) },
                // Die Nachtzeit (E43, NachtzeitSchema.SCHRITT): dieselbe Regel - NULL bleibt NULL
                // (Vorgabe), eine Stunde geht unveraendert hinueber.
                new DbParam("@c96", DbParamTyp.Integer) { Wert = Roh(r, GebaeudeSchema.SPALTE_NACHTABSENKUNG_BEGINN) },
                new DbParam("@c97", DbParamTyp.Integer) { Wert = Roh(r, GebaeudeSchema.SPALTE_NACHTABSENKUNG_ENDE) },
                // Der Energiestandard (E47, BaualtersklassenSchema.SCHRITT): dieselbe Regel - NULL bleibt
                // NULL (keiner), ein Code geht unveraendert hinueber.
                new DbParam("@c98", DbParamTyp.VarWChar) { Wert = Roh(r, GebaeudeSchema.SPALTE_ENERGIESTANDARD) },
                // Der wirksame U-Wert der Bodenplatte (E65, ErdreichVorgabeSchema.SCHRITT): dieselbe Regel -
                // NULL bleibt NULL (Rechnung nach DIN EN ISO 13370), ein Wert geht unveraendert hinueber.
                new DbParam("@cerd", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_ERDREICH_U_WIRKSAM) },
                // Der Raumeinfluss der Heizkurve (AK3, Ak3Schema.SCHRITT): dieselbe Regel - NULL bleibt NULL (aus),
                // ein Wert geht unveraendert hinueber.
                new DbParam("@craum", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_HEIZKURVE_RAUMEINFLUSS) },
                // Die Kuehlkurve (KK, KuehlkurveSchema.SCHRITT): dieselbe Regel - NULL bleibt NULL (Vorgabe bzw. aus),
                // ein Wert geht unveraendert hinueber.
                new DbParam("@ckk1", DbParamTyp.Integer) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHLKURVE_AKTIV) },
                new DbParam("@ckk2", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHLKURVE_FUSSPUNKT) },
                new DbParam("@ckk3", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHLKURVE_RAUMEINFLUSS) },
                new DbParam("@ckk4", DbParamTyp.VarWChar) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_WEG) },
                new DbParam("@ckk5", DbParamTyp.Double) { Wert = Roh(r, GebaeudeSchema.SPALTE_KUEHLKURVE_AUSLEGUNG_AUSSEN) },
                // Schemaschritt 121 (Welle #468): Die Kopie merkt sich, aus welchem
                // Katalogsatz sie stammt - eine Umbenennung des Satzes zerreisst die Klammer
                // der Loeschsperre dann nicht mehr (GebaeudeKatalogverweis).
                new DbParam("@c99", DbParamTyp.Integer) { Wert = Convert.ToInt32(r["ID"]) },
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
        /// <b>Die Zeilen der Gebaeudekataloge</b> — der Verwaltung (Konzept
        /// Administrationsdialoge, V16) und des Projektdialogs (Stufe G3, Welle K): ALLE
        /// Katalogsaetze mit den fuenf Spalten aus
        /// <see cref="Katalogfilterprofil.FuerGebaeude"/>, in EINER Abfrage.
        ///
        /// <para><b>Gefiltert wird danach, nicht hier:</b> Die vier Vorfilter der frueheren
        /// eigenen Tabellen (Verwendung, Gebaeudeart, Baualtersklasse, Suche) sind Trichter und
        /// Suche der Katalogliste, und die filtert im Kern (<c>Katalogfilter.Anwenden</c>) auf dem
        /// ANGEZEIGTEN Wert. Deshalb stehen Verwendung und Baualtersklasse hier als Klartext, nicht
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
                     .MitText(Katalogfilterprofil.SpBaualtersklasse, Gebaeudeklassen.Text(buchstabe))
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
        ///
        /// <para><b>Stufe KP1b:</b> Matrix, Kalender und Perioden des Katalogbaus folgen ihm
        /// (Konzept 5.5) — in DERSELBEN Transaktion wie der Kopfsatz. <see cref="Katalogkopie"/>
        /// bleibt dafür unverändert; sie kennt nur EINE Kindebene und gibt keine
        /// Alt→Neu-Zuordnung heraus, die die Perioden brauchen.</para>
        /// </summary>
        public static Katalogkopie.Ergebnis Duplizieren(int id, string neuerName)
        {
            using (DbVorgang vorgang = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang))
            {
                try
                {
                    // Der innere Vorgang der Katalogkopie wird unter dieser Klammer ein
                    // Sicherungspunkt auf DERSELBEN Verbindung.
                    Katalogkopie.Ergebnis kopf = Katalogkopie.Duplizieren(TABLE, id, neuerName);
                    if (!kopf.Ok)
                    {
                        vorgang.Rollback();
                        return kopf;
                    }

                    Konditionierungskopie.Befund befund = Konditionierungskopie.Kopieren(
                        vorgang,
                        KonditionierungCtrl.Eigner.Katalogbau(id),
                        KonditionierungCtrl.Eigner.Katalogbau(kopf.Id),
                        Konditionierungskopie.Auswahl.Alles);
                    if (!befund.Ok)
                    {
                        vorgang.Rollback();
                        return new Katalogkopie.Ergebnis(false, 0, "", befund.Meldung);
                    }

                    vorgang.Commit();
                    return kopf;
                }
                catch (Exception ex)
                {
                    vorgang.Rollback();
                    return new Katalogkopie.Ergebnis(false, 0, "",
                        MyResource.Resource.ADM_MSG_KOPIE_FEHLER + " " + ex.Message);
                }
            }
        }

        // =================================================================
        //  Konditionierung: erneut übernehmen und „Speichern unter" (Stufe KP1b)
        // =================================================================

        /// <summary>
        /// <b>„Konditionierung erneut übernehmen"</b> (Konzept 5.5; Entwurf KP2, Festlegung 4, Befund
        /// B9): Die <b>ganze Gebäudeebene</b> wird die des Katalogbaus — Vorgabezeilen, Kalender samt
        /// Perioden, die neun Bestandszellen, Nachtzeiten, Merker, Ferienzeiträume und
        /// <c>Luftwechselrate</c> (<see cref="Konditionierungsarbeit.KatalogErneut"/>). Die Zeilen der
        /// ZONEN bleiben — sie sind eine andere Ebene; der Befund zählt sie. Eine dünne Hülle: Lesen →
        /// reiner Schritt → Schreiben, alles in EINEM Vorgang. Die Rückfrage entsteht vorher
        /// (<see cref="KonditionierungErneutUebernehmenRueckfrage"/>). Der Katalogbau wird allein über den
        /// Verweis der Kopie gesucht (<c>ID_Gebaeude_Stamm</c>, <see cref="KatalogbauDerKopie"/>); ohne
        /// Verweis gibt es keinen — ein gleichnamiger Satz wird nicht angebunden.
        /// </summary>
        public static Konditionierungskopie.Befund KonditionierungErneutUebernehmen(int idGebaeude)
        {
            long? idKatalog = KatalogbauDerKopie(idGebaeude);
            if (!idKatalog.HasValue)
                return Konditionierungskopie.Befund.Fehler(MyResource.Resource.ADM_MSG_KOPIE_FEHLT);
            if (!KonditionierungSchema.Lesbar()) return Konditionierungskopie.Befund.Nichts;

            using (DbVorgang vorgang = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang))
            {
                try
                {
                    var ctrl = new KonditionierungCtrl();
                    KonditionierungCtrl.Eigner gebaeude = KonditionierungCtrl.Eigner.Gebaeude(idGebaeude);
                    Konditionierungsstand projekt = ctrl.StandLesen(gebaeude, out _);
                    Konditionierungsstand katalog = ctrl.StandLesen(
                        KonditionierungCtrl.Eigner.Katalogbau(idKatalog.Value), out string meldung);
                    if (meldung != null)
                    {
                        vorgang.Rollback();
                        return Konditionierungskopie.Befund.Fehler(meldung);
                    }

                    Konditionierungsschritt s = Konditionierungsarbeit.KatalogErneut(
                        new Konditionierungsarbeitsstand(projekt, null), katalog);
                    if (!s.Ok)
                    {
                        vorgang.Rollback();
                        return Konditionierungskopie.Befund.Fehler(s.Meldung);
                    }
                    KonditionierungCtrl.Ergebnis e = ctrl.StandSchreiben(vorgang, gebaeude, s.Stand.Gebaeude,
                                                                          mitBestand: true, out _);
                    if (!e.Ok)
                    {
                        vorgang.Rollback();
                        return Konditionierungskopie.Befund.Fehler(e.Meldung);
                    }

                    int perioden = 0;
                    foreach (Konditionierungskalender k in katalog.Angelegt().Values) perioden += k.Perioden.Count;
                    int zonen = Konditionierungskopie.Zonenzeilen(vorgang, idGebaeude);
                    vorgang.Commit();
                    return new Konditionierungskopie.Befund(true, katalog.VorgabenAnzahl, katalog.KalenderAnzahl,
                                                            perioden, zonen, "");
                }
                catch (Exception ex)
                {
                    vorgang.Rollback();
                    return Konditionierungskopie.Befund.Fehler(ex.Message);
                }
            }
        }

        /// <summary>
        /// <b>Die Rückfrage VOR „erneut übernehmen"</b> (Festlegung 3, 4; Befund B9): was die erneute
        /// Übernahme am Gebäude ersetzt, was bleibt und welche Zonen ihre eigenen Werte behalten, mit
        /// Namen — gelesen, nicht geschrieben (<see cref="Konditionierungsarbeit.Rueckfrage"/>).
        /// <c>null</c>, wenn es keinen Katalogbau oder keine Konditionierungstabellen gibt.
        /// </summary>
        public static Konditionierungsbilanz KonditionierungErneutUebernehmenRueckfrage(int idGebaeude)
        {
            long? idKatalog = KatalogbauDerKopie(idGebaeude);
            if (!idKatalog.HasValue || !KonditionierungSchema.Lesbar()) return null;
            var ctrl = new KonditionierungCtrl();
            Konditionierungsarbeitsstand projekt = ctrl.ArbeitsstandLesen(idGebaeude, null, out _);
            Konditionierungsstand katalog = ctrl.StandLesen(KonditionierungCtrl.Eigner.Katalogbau(idKatalog.Value), out _);
            return Konditionierungsarbeit.Rueckfrage(projekt, null, Konditionierungshandlung.KatalogErneut, katalog);
        }

        /// <summary>
        /// <b>Die Konditionierung des Katalogbaus einer Projektkopie</b> — die Ebene, aus der „erneut
        /// übernehmen" liest (Stufe KP2, Festlegung 4); <c>null</c>, wenn es keinen Katalogbau oder keine
        /// Konditionierungstabellen gibt. Für die Hülle, die den Schritt am Arbeitsstand fährt.
        /// </summary>
        public static Konditionierungsstand KatalogebeneDerKopie(int idGebaeude, out string meldung)
        {
            meldung = null;
            long? idKatalog = KatalogbauDerKopie(idGebaeude);
            if (!idKatalog.HasValue || !KonditionierungSchema.Lesbar()) return null;
            return new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Katalogbau(idKatalog.Value), out meldung);
        }

        /// <summary>
        /// Der Katalogbau einer Projektkopie — allein über ihren Verweis <c>ID_Gebaeude_Stamm</c>;
        /// <c>null</c> = keiner. <b>Kein Namensrückfall:</b> Jede Kopie trägt ihren Verweis, seit die
        /// Übernahme ihn setzt und Schemaschritt 121 den Altbestand über eindeutige Namen nachgetragen
        /// hat. Leer ist er nur, wenn es den Satz nicht (mehr) gibt — gelöscht („Gebäude in DB
        /// löschen"), beim Nachtrag ohne Treffer, am Ziel eines Pakets ohne Treffer. Ein später
        /// angelegter Satz gleichen Namens ist ein FREMDER Satz und wird nicht angebunden.
        /// </summary>
        private static long? KatalogbauDerKopie(int idGebaeude)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT s.[ID] FROM [" + TABLE_PROJ + "] AS g " +
                "INNER JOIN [" + TABLE + "] AS s ON s.[ID] = g.[ID_Gebaeude_Stamm] WHERE g.[ID] = ?",
                new DbParam("@g", idGebaeude));
            return v == null || v == DBNull.Value
                ? (long?)null
                : Convert.ToInt64(v, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Was der OK-Weg eines Katalogbaus ergeben hat (<see cref="KatalogSchreiben"/>): die Id des
        /// Satzes und ob die Konditionierung Zeilen geschrieben hat.
        /// </summary>
        public sealed record Katalogschreibergebnis(bool Ok, int Id, bool KonditionierungGeschrieben, string Meldung)
        {
            /// <summary>Der benannte Fehlschlag — nichts ist geschrieben.</summary>
            public static Katalogschreibergebnis Fehler(string meldung) => new Katalogschreibergebnis(false, 0, false, meldung ?? "");
        }

        /// <summary>
        /// <b>Der OK-Weg eines Katalogbaus</b> (Stufe KP2, Welle K2; Entwurf KP2 Abschnitt 2 „eine
        /// Schreibstelle"): Kopf und Konditionierung in EINEM Vorgang. <paramref name="neu"/> legt den
        /// Satz an — sein Eigner entsteht im Vorgang —, sonst wird er unter <paramref name="ursprungsname"/>
        /// überschrieben (der Name steht im Modell). Die Konditionierung kommt aus dem Arbeitsstand
        /// (<paramref name="stand"/>; nur Geändertes, <see cref="KonditionierungCtrl.StandSchreiben"/>); ohne
        /// Stand bekommt ein neuer Satz die Kopie der <paramref name="quelle"/> („Speichern unter" wie
        /// Duplizieren), ohne beides nur den Kopf. Scheitert ein Schritt, fällt alles zurück. Das Schloss und
        /// den freien Namen prüft der Aufrufer (die Hülle meldet es mit eigenem Text).
        /// </summary>
        public static Katalogschreibergebnis KatalogSchreiben(GebaeudeModel modell, bool neu, string ursprungsname,
                                                              Konditionierungsstand stand,
                                                              KonditionierungCtrl.Eigner quelle = null)
        {
            if (modell == null) throw new ArgumentNullException(nameof(modell));
            if (!neu) modell.Gebaeudename = ursprungsname;

            using (DbVorgang vorgang = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang))
            {
                try
                {
                    var ctrl = new GebaeudeStammCtrl();
                    int id;
                    if (neu)
                    {
                        if (!ctrl.Insert(modell, out id) || id <= 0)
                        {
                            vorgang.Rollback();
                            return Katalogschreibergebnis.Fehler(MyResource.Resource.KOND_MSG_KOPF_NICHT_ANGELEGT);
                        }
                    }
                    else
                    {
                        if (!ctrl.Overwrite(modell))
                        {
                            vorgang.Rollback();
                            return Katalogschreibergebnis.Fehler("");
                        }
                        id = ctrl.Lies(modell.Gebaeudename)?.ID ?? 0;
                    }

                    bool geschrieben = false;
                    if (stand != null && id > 0)
                    {
                        KonditionierungCtrl.Ergebnis e = new KonditionierungCtrl().StandSchreiben(
                            vorgang, KonditionierungCtrl.Eigner.Katalogbau(id), stand.AlsArt(Kalendereigentuemer.Katalogbau),
                            mitBestand: false, out geschrieben);
                        if (!e.Ok)
                        {
                            vorgang.Rollback();
                            return Katalogschreibergebnis.Fehler(e.Meldung);
                        }
                    }
                    else if (neu && quelle != null)
                    {
                        Konditionierungskopie.Befund b = Konditionierungskopie.Kopieren(
                            vorgang, quelle, KonditionierungCtrl.Eigner.Katalogbau(id), Konditionierungskopie.Auswahl.Alles);
                        if (!b.Ok)
                        {
                            vorgang.Rollback();
                            return Katalogschreibergebnis.Fehler(b.Meldung);
                        }
                        geschrieben = b.Vorgaben + b.Kalender > 0;
                    }

                    vorgang.Commit();
                    return new Katalogschreibergebnis(true, id, geschrieben, "");
                }
                catch (Exception ex)
                {
                    vorgang.Rollback();
                    return Katalogschreibergebnis.Fehler(ex.Message);
                }
            }
        }

        /// <summary>
        /// <b>OK im Projekt, Schritt 1</b> (Stufe KP2, Welle K2): die Projektkopie
        /// (<see cref="ProjektkopieUeberschreiben"/>) samt ihrer Konditionierung aus dem Arbeitsstand in
        /// EINEM Vorgang — nur Geändertes; <paramref name="stand"/> <c>null</c> lässt die Tabellen stehen.
        /// </summary>
        public static KonditionierungCtrl.Ergebnis ProjektkopieSchreiben(int idGebaeude, int idProjekt, GebaeudeModel modell,
                                                                         Konditionierungsstand stand)
        {
            using (DbVorgang vorgang = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang))
            {
                try
                {
                    if (!ProjektkopieUeberschreiben(idGebaeude, idProjekt, modell))
                    {
                        vorgang.Rollback();
                        return KonditionierungCtrl.Ergebnis.Fehler(MyResource.Resource.GEBZ_MSG_GEBAEUDE);
                    }
                    if (stand != null)
                    {
                        KonditionierungCtrl.Ergebnis e = new KonditionierungCtrl().StandSchreiben(
                            vorgang, KonditionierungCtrl.Eigner.Gebaeude(idGebaeude), stand.AlsArt(Kalendereigentuemer.Gebaeude),
                            mitBestand: false, out _);
                        if (!e.Ok)
                        {
                            vorgang.Rollback();
                            return e;
                        }
                    }
                    vorgang.Commit();
                    return KonditionierungCtrl.Ergebnis.Gut;
                }
                catch (Exception ex)
                {
                    vorgang.Rollback();
                    return KonditionierungCtrl.Ergebnis.Fehler(ex.Message);
                }
            }
        }

        /// <summary>Was „Speichern unter" ergeben hat: der neue Katalogbau und sein Kopierbefund.</summary>
        public sealed record SpeichernUnterErgebnis(bool Ok, int Id,
                                                    Konditionierungskopie.Befund Befund, string Meldung);

        /// <summary>
        /// <b>„Speichern unter"</b> (Konzept 5.5, Festlegung 10): Der Satz
        /// <paramref name="modell"/> wird als NEUER Katalogbau angelegt, und die Konditionierung
        /// der <paramref name="quelle"/> — eines Projektgebäudes oder eines Katalogbaus — kommt in
        /// DERSELBEN Transaktion mit. Es reist nur die <b>Gebäudeebene</b>; die Zonenzeilen bleiben
        /// zurück und stehen im Befund für die Rückfrage (KP2).
        ///
        /// <para>Im Katalogmodus ist die Quelle der Ursprungs-Katalogbau — dann wirkt „Speichern
        /// unter" wie Duplizieren. <paramref name="quelle"/> <c>null</c> legt nur den Kopf an.</para>
        /// </summary>
        public static SpeichernUnterErgebnis SpeichernUnter(GebaeudeModel modell,
                                                            KonditionierungCtrl.Eigner quelle)
        {
            if (modell == null) throw new ArgumentNullException(nameof(modell));

            using (DbVorgang vorgang = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang))
            {
                try
                {
                    var ctrl = new GebaeudeStammCtrl();
                    if (!ctrl.Insert(modell, out int id) || id <= 0)
                    {
                        vorgang.Rollback();
                        return new SpeichernUnterErgebnis(false, 0, null,
                            MyResource.Resource.KOND_MSG_KOPF_NICHT_ANGELEGT);
                    }

                    Konditionierungskopie.Befund befund = quelle == null
                        ? Konditionierungskopie.Befund.Nichts
                        : Konditionierungskopie.Kopieren(vorgang, quelle,
                                                         KonditionierungCtrl.Eigner.Katalogbau(id),
                                                         Konditionierungskopie.Auswahl.Alles);
                    if (!befund.Ok)
                    {
                        vorgang.Rollback();
                        return new SpeichernUnterErgebnis(false, 0, befund, befund.Meldung);
                    }

                    vorgang.Commit();
                    return new SpeichernUnterErgebnis(true, id, befund, "");
                }
                catch (Exception ex)
                {
                    vorgang.Rollback();
                    return new SpeichernUnterErgebnis(false, 0, null, ex.Message);
                }
            }
        }

        // =================================================================
        //  „In DB übernehmen": ein Projektgebäude als neuer Katalogsatz
        // =================================================================

        /// <summary>Warum <see cref="AusProjektUebernehmen"/> nichts geschrieben hat.</summary>
        public enum Projektuebernahmeabsage
        {
            /// <summary>Keine Absage — der Satz steht im Katalog.</summary>
            Keine,

            /// <summary>Kein Projektgebäude gewählt, oder es hat (noch) keine Projektkopie.</summary>
            KeinGebaeude,

            /// <summary>Der Name ist leer.</summary>
            NameLeer,

            /// <summary>Ein Katalogsatz trägt den Namen schon.</summary>
            NameVergeben,

            /// <summary>Das Schreiben ist gescheitert; der Grund steht in der Meldung.</summary>
            Fehler,
        }

        /// <summary>
        /// Was „In DB übernehmen" ergeben hat: die Id und der Name des neuen Katalogsatzes, der
        /// Kopierbefund der Konditionierung und die Zahl der Zonen und Bauteile, die im Projekt
        /// bleiben (der Katalog führt keine Zonen) — oder die benannte Absage.
        /// </summary>
        public sealed record ProjektuebernahmeErgebnis(bool Ok, int Id, string Name, Projektuebernahmeabsage Absage,
                                                       string Meldung, Konditionierungskopie.Befund Befund,
                                                       int ZonenImProjekt, int BauteileImProjekt)
        {
            /// <summary>Steht die Absage am Namensfeld (leer oder vergeben)?</summary>
            public bool AmNamen => Absage == Projektuebernahmeabsage.NameLeer || Absage == Projektuebernahmeabsage.NameVergeben;

            /// <summary>Die benannte Absage — nichts ist geschrieben.</summary>
            public static ProjektuebernahmeErgebnis Abgelehnt(Projektuebernahmeabsage absage, string meldung)
                => new ProjektuebernahmeErgebnis(false, 0, "", absage, meldung ?? "", null, 0, 0);
        }

        /// <summary>
        /// Die 95 Fachspalten des Gebäudemodells, die Projektkopie (<c>Tab_Gebaeude</c>) und Katalog
        /// (<c>Tab_Gebaeude_STAMM</c>) gleich führen — alle Spalten des Katalogs außer <c>ID</c>,
        /// <c>Bezeichner</c>, <c>Beschreibung</c> und <c>ReadOnly</c>. Dieselbe Liste wie die Anlage
        /// <see cref="Insert(GebaeudeModel, out int)"/>, in derselben Reihenfolge.
        /// </summary>
        internal const string KOPFSPALTEN = "[Typ], [Wohnflaeche_gesamt], [Bewohner], [Flaeche_Nutzer], [Interne_Waermegewinne], [Bauweise], [Fensterflaeche_Sued], [Fensterflaeche_Ost_West], [Fensterflaeche_Nord], [Fensterdurchlassgrad], [Raumsolltemperatur_Nachtabsenkung], [Raumsolltemperatur_Tag], [Raumsolltemperatur_Wochenende], [Raumsolltemperatur_Ferien], [Maximaleraumtemperatur], [k_Wert_Außenwand], [k_Wert_Fenster], [k_Wert_Dachflaeche], [k_Wert_Grundflaeche], [k_Wert_Sonstiges], [Flaeche_Außenwand], [gesamte_Fensterflaeche], [Dachflaeche], [Grundflaeche], [Sonstige_Flaechen], [Nutzflaeche], [Raumhoehe], [WBVK_Anschluß_Fenster_Wand], [WBVK_Anschluß_Wand_Dach], [WBVK_Anschluß_Außenwand_Kellerdecke], [Abmessung_Anschluß_Fenster_Wand], [Abmessung_Anschluß_Wand_Dach], [Abmessung_Anschluß_Außenwand_Kellerdecke], [Luftwechselrate], [Wochenende], [Ferien], [Ferienbeginn_1], [Ferienende_1], [Ferienbeginn_2], [Ferienende_2], [Ferienbeginn_3], [Ferienende_3], [Ferienbeginn_4], [Ferienende_4], [WW_Bedarf], [spez_Waermeverbrauch], [Waermebedarf], [Baualtersklasse], [Gebaeudeart], [Wohngebaeude_Nicht_Wohngebaeude], [Gebaeude_Modell], [Fensterflaeche_Ost], [Fensterflaeche_West], [Rahmenanteil], [Verschattungsfaktor], [Grundflaeche_Randbedingung], [Kellertemperatur], [Masseanteil_Aussen], [Innenflaechenfaktor], [Heizung_Strahlungsanteil], [Heizleistung_Max], [Aussenbauteile_Strahlung], [Luftwechsel_Infiltration], [Luftwechsel_Nutzer], [Sommerlueftung], [Kuehl_Sollwert], [Kuehlleistung_Max], [Kuehlung_Aktiv], [Kuehl_Sollwert_Nacht], [Heizkreis_Aktiv], [Uebergabe_Art], [Uebergabe_Exponent], [Uebergabe_Leistung_Nenn], [Auslegung_Vorlauf], [Auslegung_Ruecklauf], [Auslegung_Raumtemperatur], [Auslegung_Aussentemperatur], [Heizkurve_Aktiv], [Heizkurve_Niveau], [Heizkurve_Steilheit], [Regler_Proportionalband], [Sollwertprofil], [Kuehluebergabe_Aktiv], [Kuehl_Uebergabe_Art], [Kuehl_Uebergabe_Exponent], [Kuehl_Uebergabe_Leistung_Nenn], [Kuehl_Auslegung_Vorlauf], [Kuehl_Auslegung_Ruecklauf], [Kuehl_Auslegung_Raumtemperatur], [Kuehl_Vorlaufgrenze], [Baujahr], [Nachtabsenkung_Beginn], [Nachtabsenkung_Ende], [Energiestandard], [Erdreich_U_Wirksam], [Heizkurve_Raumeinfluss], [Kuehlkurve_Aktiv], [Kuehlkurve_Fusspunkt], [Kuehlkurve_Raumeinfluss], [Kuehlkurve_Auslegung_Weg], [Kuehlkurve_Auslegung_Aussen]";

        /// <summary>
        /// Der Name, den „In DB übernehmen" für die Projektkopie des Projektgebäudes
        /// <paramref name="idProjektGebaeude"/> (<c>Z_ProjektGebaeude.ID</c>) vorschlägt: ihr Name, und ist
        /// er im Katalog vergeben, mit Zähler („… (2)", „… (3)" …). Leer, wenn es die Kopie nicht gibt.
        /// </summary>
        public static string NamensvorschlagAusProjekt(int idProjektGebaeude)
        {
            DataRow r = Projektkopiezeile(idProjektGebaeude);
            if (r == null) return "";
            string basis = Spaltentext(r, "Gebaeudename").Trim();
            if (basis.Length == 0) return "";
            if (!NameVergeben(basis)) return basis;
            for (int n = 2; n < 10000; n++)
            {
                string kandidat = basis + " (" + n.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
                if (!NameVergeben(kandidat)) return kandidat;
            }
            return basis;
        }

        /// <summary>Trägt ein Katalogsatz den Namen <paramref name="name"/> schon (eindeutiger Index auf <c>Bezeichner</c>)?</summary>
        public static bool NameVergeben(string name)
        {
            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM [" + TABLE + "] WHERE [Bezeichner] = ?",
                new DbParam("@bez", name ?? ""));
            return n != null && n != DBNull.Value && Convert.ToInt64(n, System.Globalization.CultureInfo.InvariantCulture) > 0;
        }

        /// <summary>
        /// <b>„In DB übernehmen"</b> — die Projektkopie des Projektgebäudes
        /// <paramref name="idProjektGebaeude"/> (<c>Z_ProjektGebaeude.ID</c>, die Zuordnung der Projektliste)
        /// als NEUER Anwendersatz im Katalog unter <paramref name="name"/>: der Spiegel von
        /// <see cref="CopyFromStamm(int?, string, int, int)"/>.
        ///
        /// <list type="bullet">
        /// <item><b>Kopf:</b> alle 96 Fachspalten (<see cref="KOPFSPALTEN"/>) wörtlich aus
        /// <c>Tab_Gebaeude</c> — NULL bleibt NULL, Schalter bleiben 0/1. Neue Id, <c>ReadOnly = 0</c>;
        /// Projekt, Zuordnung und Katalogverweis der Kopie gehören nicht zum Katalog.</item>
        /// <item><b>Beschreibung:</b> die der Kopie, darunter die Herkunft „aus Projekt …, Datum"
        /// (<c>GEB_TEXT_HERKUNFT_PROJEKT</c>).</item>
        /// <item><b>Konditionierung:</b> die Gebäudeebene (Vorgaben, Kalender samt Perioden) über
        /// <see cref="Konditionierungskopie"/> — wie „Speichern unter".</item>
        /// <item><b>Zonen und Bauteile</b> bleiben im Projekt: Der Katalog führt keine Zonen
        /// (<c>Tab_Zone.ID_Gebaeude</c> zeigt nur auf Projektkopien). Ihre Zahl steht im Ergebnis.</item>
        /// <item>Die Tagesverteilung hängt im Katalog am Gebäudetyp (<c>Typ</c>), der mitreist.</item>
        /// </list>
        ///
        /// <para>Alles in EINEM Vorgang; das Projekt bleibt unberührt. Benannte Absagen: kein Gebäude
        /// (oder keine Projektkopie), Name leer, Name vergeben.</para>
        /// </summary>
        public static ProjektuebernahmeErgebnis AusProjektUebernehmen(int idProjektGebaeude, string name,
                                                                      DateTime? stichtag = null)
        {
            DataRow r = idProjektGebaeude > 0 ? Projektkopiezeile(idProjektGebaeude) : null;
            if (r == null)
                return ProjektuebernahmeErgebnis.Abgelehnt(Projektuebernahmeabsage.KeinGebaeude,
                                                           MyResource.Resource.GEB_MSG_DB_UEBERNAHME_KEIN_GEBAEUDE);

            string neuerName = (name ?? "").Trim();
            if (neuerName.Length == 0)
                return ProjektuebernahmeErgebnis.Abgelehnt(Projektuebernahmeabsage.NameLeer,
                                                           MyResource.Resource.GEB_MSG_DB_UEBERNAHME_NAME_LEER);
            if (NameVergeben(neuerName))
                return ProjektuebernahmeErgebnis.Abgelehnt(Projektuebernahmeabsage.NameVergeben,
                                                           MyResource.Resource.GEBK_MSG_NAME_VERGEBEN);

            int idGebaeude = Convert.ToInt32(r["ID"], System.Globalization.CultureInfo.InvariantCulture);
            string beschreibung = Herkunftsbeschreibung(Spaltentext(r, "Beschreibung"),
                                                        Projektname(Ganzzahl(r, "ID_Projekt")),
                                                        stichtag ?? DateTime.Today);
            int zonen = Anzahl("SELECT COUNT(*) FROM [Tab_Zone] WHERE [ID_Gebaeude] = ?", idGebaeude);
            int bauteile = Anzahl("SELECT COUNT(*) FROM [Tab_Bauteil] WHERE [ID_Zone] IN " +
                                  "(SELECT [ID] FROM [Tab_Zone] WHERE [ID_Gebaeude] = ?)", idGebaeude);

            using (DbVorgang vorgang = DataRepository.Vorgang())
            using (Vorgangsklammer.Halter klammer = Vorgangsklammer.Setzen(vorgang))
            {
                try
                {
                    int id = DataRepository.GetMaxID(TABLE) + 1;
                    bool ok = DataRepository.ExecuteSQL(
                        "INSERT INTO [" + TABLE + "] ([ID], [Bezeichner], [Beschreibung], [ReadOnly], " + KOPFSPALTEN + ") " +
                        "SELECT ?, ?, ?, 0, " + KOPFSPALTEN + " FROM [" + TABLE_PROJ + "] WHERE [ID] = ?",
                        new DbParam("@nid", DbParamTyp.Integer) { Wert = id },
                        new DbParam("@nbez", DbParamTyp.VarWChar) { Wert = neuerName },
                        new DbParam("@nbes", DbParamTyp.VarWChar) { Wert = beschreibung },
                        new DbParam("@gid", DbParamTyp.Integer) { Wert = idGebaeude });
                    if (!ok || !NameVergeben(neuerName))
                    {
                        vorgang.Rollback();
                        return ProjektuebernahmeErgebnis.Abgelehnt(Projektuebernahmeabsage.Fehler,
                                                                   MyResource.Resource.KOND_MSG_KOPF_NICHT_ANGELEGT);
                    }

                    Konditionierungskopie.Befund befund = KonditionierungSchema.Lesbar()
                        ? Konditionierungskopie.Kopieren(vorgang, KonditionierungCtrl.Eigner.Gebaeude(idGebaeude),
                                                         KonditionierungCtrl.Eigner.Katalogbau(id),
                                                         Konditionierungskopie.Auswahl.Alles)
                        : Konditionierungskopie.Befund.Nichts;
                    if (!befund.Ok)
                    {
                        vorgang.Rollback();
                        return ProjektuebernahmeErgebnis.Abgelehnt(Projektuebernahmeabsage.Fehler, befund.Meldung);
                    }

                    vorgang.Commit();
                    return new ProjektuebernahmeErgebnis(true, id, neuerName, Projektuebernahmeabsage.Keine, "",
                                                         befund, zonen, bauteile);
                }
                catch (Exception ex)
                {
                    vorgang.Rollback();
                    return ProjektuebernahmeErgebnis.Abgelehnt(Projektuebernahmeabsage.Fehler, ex.Message);
                }
            }
        }

        /// <summary>
        /// Die Beschreibung des neuen Katalogsatzes: die der Projektkopie, darunter die Herkunft
        /// (<c>GEB_TEXT_HERKUNFT_PROJEKT</c>: {0} Projekt, {1} Datum in der Kultur der Oberfläche).
        /// </summary>
        internal static string Herkunftsbeschreibung(string beschreibung, string projekt, DateTime stichtag)
        {
            string herkunft = string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                            MyResource.Resource.GEB_TEXT_HERKUNFT_PROJEKT,
                                            projekt ?? "", stichtag.ToString("d", System.Globalization.CultureInfo.CurrentCulture));
            string vorher = (beschreibung ?? "").TrimEnd();
            return vorher.Length == 0 ? herkunft : vorher + "\n" + herkunft;
        }

        /// <summary>Die Projektkopie (<c>Tab_Gebaeude</c>) einer Zuordnung der Projektliste; <c>null</c> = keine.</summary>
        private static DataRow Projektkopiezeile(int idProjektGebaeude)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT [ID], [ID_Projekt], [Gebaeudename], [Beschreibung] FROM [" + TABLE_PROJ + "] " +
                "WHERE [ID_ProjektGebaeude] = ? ORDER BY [ID]",
                new DbParam("@zid", idProjektGebaeude));
            return dt == null || dt.Rows.Count == 0 ? null : dt.Rows[0];
        }

        /// <summary>Der Name eines Projekts; leer, wenn es ihn nicht gibt.</summary>
        private static string Projektname(int? idProjekt)
        {
            if (!idProjekt.HasValue) return "";
            object n = DataRepository.ExecuteScalar("SELECT [Projektname] FROM [Tab_Projekt] WHERE [ID] = ?",
                                                    new DbParam("@pid", idProjekt.Value));
            return n == null || n == DBNull.Value ? "" : Convert.ToString(n, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>Eine Zählung mit einem Id-Parameter; 0 ohne Ergebnis.</summary>
        private static int Anzahl(string sql, int id)
        {
            object n = DataRepository.ExecuteScalar(sql, new DbParam("@id", id));
            return n == null || n == DBNull.Value ? 0 : Convert.ToInt32(n, System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// <b>„Schloss setzen…" / „Schloss aufheben…"</b> (Entscheid AD-Q15): schaltet das
        /// Auslieferungskennzeichen der Sätze <paramref name="ids"/> — nur den Kopfsatz, kein
        /// Wert ändert sich. Die Regel steht einmal in
        /// <see cref="Auslieferungskennzeichen.SetzenInTabelle"/>; hier steht nur die Tabelle.
        /// </summary>
        public static Auslieferungskennzeichen.Ergebnis SchlossSetzen(IReadOnlyList<int> ids, bool gesperrt)
            => Auslieferungskennzeichen.SetzenInTabelle(TABLE, ids, gesperrt);

        /// <summary>
        /// <b>Warum ein Katalogsatz nicht gelöscht wird</b> — der benannte Sperrgrund für
        /// „Gebäude in DB löschen" des Projekt-Gebäudedialogs und für „Löschen" der
        /// Gebäudeverwaltung (dieselbe Regel in beiden, Anwenderentscheid 06.10.2026): allein ein Auslieferungssatz
        /// (<c>ReadOnly</c>). Ein Satz, den Projekte führen, ist löschbar (Anwenderentscheid
        /// 06.10.2026): Jedes Projekt trägt eine VOLLSTÄNDIGE Kopie (Kopf, Zonen, Bauteile,
        /// Luftströme, Konditionierung als eigene Zeilen), nichts davon hängt am Katalogsatz;
        /// beim Löschen leert die Beziehung (<c>ON DELETE SET NULL</c>) nur den Verweis
        /// <c>ID_Gebaeude_Stamm</c>. Welche Projekte ihre Kopie behalten, nennt
        /// <see cref="Loeschhinweis"/> in der Rückfrage. Leer, wenn der Satz gelöscht werden
        /// darf (oder kein Name gewählt ist).
        /// </summary>
        public static string Loeschsperrgrund(string bezeichner)
        {
            if (string.IsNullOrWhiteSpace(bezeichner)) return "";
            return new GebaeudeStammCtrl().IsReadOnly(bezeichner)
                ? MyResource.Resource.BADM_MSG_SCHREIBGESCHUETZT
                : "";
        }

        /// <summary>
        /// <b>Die Projekte, deren Kopie auf den Katalogsatz verweist</b> — genau die, deren Verweis
        /// <c>ID_Gebaeude_Stamm</c> das Löschen leert; nach Projektnamen geordnet, ohne Doppel.
        /// <b>Allein über den Verweis</b>, nicht über den Namen: Eine Kopie ohne Verweis gehört zu
        /// keinem Katalogsatz (ihr Satz ist gelöscht oder fand sich nie), auch nicht zu einem
        /// später angelegten gleichnamigen. Schreibt nichts.
        /// </summary>
        public static IReadOnlyList<string> Projektkopien(string bezeichner)
        {
            var projekte = new List<string>();
            if (string.IsNullOrWhiteSpace(bezeichner)) return projekte;
            DataTable dt = DataRepository.GetDataTable(
                "SELECT DISTINCT p.Projektname FROM [" + TABLE_PROJ + "] AS g " +
                "INNER JOIN Tab_Projekt AS p ON p.ID = g.ID_Projekt " +
                "INNER JOIN [" + TABLE + "] AS s ON s.ID = g.ID_Gebaeude_Stamm " +
                "WHERE s.Bezeichner = ? ORDER BY p.Projektname",
                new DbParam("@bez", bezeichner.Trim()));
            if (dt != null)
                foreach (DataRow r in dt.Rows)
                {
                    string name = Spaltentext(r, "Projektname");
                    if (!projekte.Contains(name)) projekte.Add(name);
                }
            return projekte;
        }

        /// <summary>
        /// <b>Der Zusatz der Rückfrage vor dem Löschen</b>: die Projekte, die eine Kopie des
        /// Katalogsatzes führen (<see cref="Projektkopien"/>), und dass ihre Kopien bleiben
        /// (<c>GEB_MSG_LOESCHHINWEIS_KOPIEN</c>). Leer, wenn kein Projekt den Satz führt.
        /// </summary>
        public static string Loeschhinweis(string bezeichner) => Loeschhinweis(bezeichner, null);

        /// <summary>
        /// <see cref="Loeschhinweis(string)"/> samt einem Projekt, dessen Kopie erst noch entsteht:
        /// Der Gebäudedialog speichert eine eben übernommene, noch ungespeicherte Projektzeile des
        /// Satzes vor dem Löschen still (Anwenderentscheid 06.10.2026) — dieses Projekt behält dann
        /// ebenfalls seine Kopie und steht mit in der Rückfrage. <c>null</c> oder leer: keines.
        /// </summary>
        public static string Loeschhinweis(string bezeichner, string weiteresProjekt)
        {
            var projekte = new List<string>(Projektkopien(bezeichner));
            if (!string.IsNullOrWhiteSpace(weiteresProjekt) && !projekte.Contains(weiteresProjekt))
            {
                projekte.Add(weiteresProjekt);
                projekte.Sort(StringComparer.Ordinal);
            }
            return projekte.Count == 0
                ? ""
                : string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                MyResource.Resource.GEB_MSG_LOESCHHINWEIS_KOPIEN,
                                string.Join(", ", projekte));
        }

        /// <summary>
        /// <b>Der Zusatz der Rückfrage für mehrere Sätze</b> — der Weg der Gebäudeverwaltung, die
        /// eine Mehrfachwahl löscht: die Projekte, die eine Kopie eines der Sätze führen, je einmal
        /// und nach Namen geordnet (<c>GEB_MSG_LOESCHHINWEIS_KOPIEN_MEHR</c>). Ein einzelner Satz
        /// bekommt den Text von <see cref="Loeschhinweis(string)"/>. Leer, wenn kein Projekt einen
        /// der Sätze führt.
        /// </summary>
        public static string Loeschhinweis(IReadOnlyList<string> bezeichner)
        {
            if (bezeichner == null || bezeichner.Count == 0) return "";
            if (bezeichner.Count == 1) return Loeschhinweis(bezeichner[0]);
            var projekte = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string name in bezeichner)
                foreach (string projekt in Projektkopien(name)) projekte.Add(projekt);
            return projekte.Count == 0
                ? ""
                : string.Format(System.Globalization.CultureInfo.CurrentCulture,
                                MyResource.Resource.GEB_MSG_LOESCHHINWEIS_KOPIEN_MEHR,
                                string.Join(", ", projekte));
        }

        /// <summary>
        /// Loescht einen Katalogsatz OHNE Rueckmeldung ueber einen Kasten — der Weg der
        /// Gebaeudeverwaltung (Stufe 5) und des Projekt-Gebaeudedialogs: Die Oberflaeche sperrt
        /// Auslieferungssaetze weich und fragt vorher zurueck; <see cref="Delete"/> meldete die
        /// Sperre ueber <c>Meldung.Hinweis</c>, und das waere in der WebView ein modaler Kasten.
        ///
        /// <para><b>Allein der Auslieferungssatz ist gesperrt</b> (Anwenderentscheid 06.10.2026,
        /// <see cref="Loeschsperrgrund"/>): Ein Satz, den Projekte fuehren, wird geloescht; ihre
        /// Kopien samt Zonen, Bauteilen, Luftstroemen und Konditionierung bleiben unberuehrt, die
        /// Beziehung leert nur ihren Verweis <c>ID_Gebaeude_Stamm</c> (<c>SET NULL</c>). Die
        /// Katalogkalender, Perioden und Vorgaben des Satzes fallen per <c>CASCADE</c> weg.</para>
        /// </summary>
        /// <returns><c>true</c>, wenn der Satz geloescht wurde.</returns>
        public static bool Loeschen(string bezeichner)
        {
            if (string.IsNullOrEmpty(bezeichner)) return false;
            if (new GebaeudeStammCtrl().IsReadOnly(bezeichner)) return false;
            return DataRepository.ExecuteSQL(
                "DELETE FROM [" + TABLE + "] WHERE Bezeichner = ? AND ReadOnly = 0",
                new DbParam("@bez", bezeichner));
        }

        #endregion
    }
}
