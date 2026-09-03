using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    class SolardatenCtrl : SolardatenModel
    {
        // Dynamisches Listen-Schema zur Aufhebung des 1.000.000er Limits
        private List<SolardatenModel> _internalList = new List<SolardatenModel>();

        public int rows => _internalList.Count;
        public new List<SolardatenModel> items => _internalList;

        // Zusätzliche Analyse-Listen aus dem Altcode beibehalten
        public List<double> list_Temperatur = new List<double>();
        public List<double> list_Sonnenwinkel = new List<double>();
        public List<int> list_Tag = new List<int>();

        public string Klimazone { get; set; }

        public SolardatenCtrl()
        {
            Klimazone = "";
            m_ID_Klimaregion = 0;
        }

        private void MapDataRowToModel(DataRow row, SolardatenModel item, DataTable dt)
        {
            if (dt.Columns.Contains("ID") && row["ID"] != DBNull.Value) item.m_ID = Convert.ToInt32(row["ID"]);
            if (dt.Columns.Contains("ID_Klimaregion") && row["ID_Klimaregion"] != DBNull.Value) item.m_ID_Klimaregion = Convert.ToInt32(row["ID_Klimaregion"]);
            if (dt.Columns.Contains("Temperatur") && row["Temperatur"] != DBNull.Value) item.Außen_Temp = Convert.ToDouble(row["Temperatur"]);
            if (dt.Columns.Contains("Sol_Nord") && row["Sol_Nord"] != DBNull.Value) item.Sol_Nord = Convert.ToDouble(row["Sol_Nord"]);
            if (dt.Columns.Contains("Sol_Ost") && row["Sol_Ost"] != DBNull.Value) item.Sol_Ost = Convert.ToDouble(row["Sol_Ost"]);
            if (dt.Columns.Contains("Sol_Sued") && row["Sol_Sued"] != DBNull.Value) item.Sol_Sued = Convert.ToDouble(row["Sol_Sued"]);
            if (dt.Columns.Contains("Sol_West") && row["Sol_West"] != DBNull.Value) item.Sol_West = Convert.ToDouble(row["Sol_West"]);
            if (dt.Columns.Contains("Globalstrahlung") && row["Globalstrahlung"] != DBNull.Value) item.Globalstrahlung = Convert.ToDouble(row["Globalstrahlung"]);
            if (dt.Columns.Contains("Direktstrahlung") && row["Direktstrahlung"] != DBNull.Value) item.Direktstrahlung = Convert.ToDouble(row["Direktstrahlung"]);
            if (dt.Columns.Contains("Diffusstrahlung") && row["Diffusstrahlung"] != DBNull.Value) item.Diffusstrahlung = Convert.ToDouble(row["Diffusstrahlung"]);
            if (dt.Columns.Contains("Sonnenwinkel") && row["Sonnenwinkel"] != DBNull.Value) item.Sonnenwinkel = Convert.ToDouble(row["Sonnenwinkel"]);
        }

        public void ReadAll(string sql = "")
        {
            if (string.IsNullOrEmpty(sql))
            {
                sql = "SELECT * FROM Tab_Solar ORDER BY ID";
            }

            DataTable dt = DataRepository.GetDataTable(sql, null);
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                SolardatenModel item = new SolardatenModel();
                MapDataRowToModel(row, item, dt);
                _internalList.Add(item);
            }
        }

        public void ReadAll(int ID_Klimaregion)
        {
            string sql = "SELECT * FROM Tab_Solar WHERE ID_Klimaregion = ? ORDER BY ID";

            DbParam paramReg = new DbParam("@regId", DbParamTyp.Integer);
            paramReg.Wert = ID_Klimaregion;

            DataTable dt = DataRepository.GetDataTable(sql, new[] { paramReg });

            _internalList.Clear();
            list_Temperatur.Clear();
            list_Sonnenwinkel.Clear();
            list_Tag.Clear();

            if (dt == null) return;

            int currentIndex = 0;
            foreach (DataRow row in dt.Rows)
            {
                SolardatenModel item = new SolardatenModel();
                MapDataRowToModel(row, item, dt);

                list_Temperatur.Add(item.Außen_Temp);
                list_Sonnenwinkel.Add(item.Sonnenwinkel);
                list_Tag.Add(currentIndex + 1);

                _internalList.Add(item);
                currentIndex++;
            }
        }

        // Liest Solar-STAMMDATEN einer Region (FK-Spalte heisst dort "Bezeichner")
        // und fuellt die Hilfslisten – fuer die Admin-Anzeige (Form_Klimadaten).
        public void ReadAllStamm(int stammRegionId)
        {
            string sql = "SELECT * FROM Tab_Solar_STAMM WHERE ID_Klimaregion = " + stammRegionId + " ORDER BY ID";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            _internalList.Clear();
            list_Temperatur.Clear();
            list_Sonnenwinkel.Clear();
            list_Tag.Clear();
            if (dt == null) return;

            int idx = 0;
            foreach (DataRow row in dt.Rows)
            {
                SolardatenModel item = new SolardatenModel();
                MapDataRowToModel(row, item, dt);
                list_Temperatur.Add(item.Außen_Temp);
                list_Sonnenwinkel.Add(item.Sonnenwinkel);
                list_Tag.Add(++idx);
                _internalList.Add(item);
            }
        }

        // =================================================================================
        // ORTSZEIT-LESEPFAD (Befund B1, Paket A des PV-Ertragsmodell-Konzepts)
        // =================================================================================

        /// <summary>
        /// Liest die Solarreihe einer Klimaregion und liefert sie in ORTSZEIT
        /// (MEZ/MESZ) — der eine Lesepfad für alle stundenscharfen Verbraucher.
        ///
        /// <para><b>Warum.</b> <c>Tab_Solar(_STAMM)</c> steht im UTC-Raster (PVGIS
        /// <c>time(UTC)</c>, Ablage in Empfangsreihenfolge, keine Zeitspalte), Lastgänge
        /// und Bedarfsprofile stehen in Ortszeit. Ohne diese Verschiebung stand die
        /// Erzeugung dem Bedarf 1 h (Winter) bzw. 2 h (Sommer) zu früh gegenüber.
        /// Begründung, Regel und die beiden Umstellstunden: <see cref="SolarZeitbasis"/>.</para>
        ///
        /// <para><b>Ganze Zeilen.</b> <c>Sol_*</c>, <c>Globalstrahlung</c>,
        /// <c>Direktstrahlung</c>, <c>Diffusstrahlung</c>, <c>Sonnenwinkel</c> und
        /// <c>Temperatur</c> sind auf DERSELBEN UTC-Stunde gerechnet; verschoben wird
        /// deshalb die Zeile, nie eine einzelne Spalte. Die UTC-Herkunft bleibt an der
        /// Zeile (<see cref="SolardatenModel.TagUtc"/>,
        /// <see cref="SolardatenModel.StundeUtc"/>) — der Sonnenstand rechnet weiter auf
        /// UTC-Basis.</para>
        ///
        /// <para><b>Keine 8.760 Zeilen ⇒ KEINE Verschiebung.</b> Die Zuordnung
        /// Ortszeit → UTC ist auf das feste Jahresraster gebaut; auf einer Teilreihe
        /// verschöbe sie Zeilen an willkürliche Stellen. Der Lauf rechnet dann mit der
        /// rohen Reihe weiter und meldet das als Warnung — dieselbe Linie wie bei
        /// Befund B4 (bis dahin gab es überhaupt keine Zeilenzahlprüfung).</para>
        /// </summary>
        /// <param name="idKlimaregion">Die Klimaregion; Filter der Abfrage.</param>
        /// <param name="idProjekt">
        /// Für die Wahl des Referenzjahres (siehe <see cref="Referenzjahr"/>); 0 ist
        /// zulässig und führt auf die Konstante.
        /// </param>
        /// <param name="stamm">true liest <c>Tab_Solar_STAMM</c> statt <c>Tab_Solar</c>.</param>
        public void ReadOrtszeit(int idKlimaregion, int idProjekt, bool stamm = false)
        {
            string tabelle = stamm ? "Tab_Solar_STAMM" : "Tab_Solar";

            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM [" + tabelle + "] WHERE ID_Klimaregion = ? ORDER BY ID",
                new[] { new DbParam("@regId", DbParamTyp.Integer) { Wert = idKlimaregion } });

            Leeren();
            if (dt == null) return;

            // --- 1) Die rohe UTC-Reihe, jede Zeile mit ihrer UTC-Herkunft ---------------
            List<SolardatenModel> utc = new List<SolardatenModel>(dt.Rows.Count);
            int pos = 0;
            foreach (DataRow row in dt.Rows)
            {
                SolardatenModel item = new SolardatenModel();
                MapDataRowToModel(row, item, dt);
                item.TagUtc = pos / 24 + 1;      // 1-basiert, wie CalculateHourly es erwartet
                item.StundeUtc = pos % 24;
                utc.Add(item);
                pos++;
            }

            // --- 2) Zeilenzahl -----------------------------------------------------------
            if (utc.Count != SolarZeitbasis.STUNDEN_JAHR)
            {
                SimulationProtokoll.Aktuell.WarnungEinmal(
                    "solar-zeitraster-" + idKlimaregion,
                    "Klimadaten: Die Reihe der Klimaregion " + idKlimaregion + " fuehrt " +
                    utc.Count + " statt " + SolarZeitbasis.STUNDEN_JAHR + " Stunden. Die " +
                    "Umrechnung UTC -> Ortszeit (MEZ/MESZ) bleibt deshalb AUS - gerechnet " +
                    "wird mit der rohen Reihe, Erzeugung und Bedarf stehen sich damit 1 bis " +
                    "2 Stunden zu frueh gegenueber. Die Klimaregion ist neu zu importieren.");
                Uebernehmen(utc);
                return;
            }

            // --- 3) Umsortieren ----------------------------------------------------------
            int jahr = Referenzjahr(idProjekt);
            int[] zuordnung = SolarZeitbasis.Zuordnung(jahr);

            List<SolardatenModel> ortszeit = new List<SolardatenModel>(SolarZeitbasis.STUNDEN_JAHR);
            for (int l = 0; l < SolarZeitbasis.STUNDEN_JAHR; l++) ortszeit.Add(utc[zuordnung[l]]);

            SimulationProtokoll.Aktuell.HinweisEinmal(
                "solar-zeitbasis-" + idKlimaregion,
                "Zeitbasis Klimadaten: UTC -> MEZ/MESZ, Referenzjahr " +
                jahr.ToString(CultureInfo.InvariantCulture) + ", Umstellung " +
                SolarZeitbasis.UmstelltageText(jahr) + ".");

            Uebernehmen(ortszeit);
        }

        /// <summary>
        /// Das Referenzjahr der Zeitbasis — es bestimmt ausschliesslich die beiden
        /// Sommerzeit-Umstelltage.
        ///
        /// <para>Erste Wahl ist das Jahr der zum Projekt hinterlegten SPOTPREISREIHE
        /// (aktive Speichervariante → <c>ID_Preisreihe</c> → <c>Tab_Preisreihe.Jahr</c>).
        /// Dann liegen Erzeugung und Preisreihe auf denselben Umstelltagen — genau die
        /// Paarung, um die es bei Befund B1 geht. Ohne Reihe gilt
        /// <see cref="DbWerte.SOLAR_REFERENZJAHR_STANDARD"/>; ein <c>DateTime.Today</c>
        /// waere hier falsch, weil derselbe Referenzlauf am Jahreswechsel andere Zahlen
        /// lieferte.</para>
        /// </summary>
        public static int Referenzjahr(int idProjekt)
        {
            try
            {
                if (idProjekt > 0)
                {
                    StromspeicherVarianteModel v = new StromspeicherVarianteCtrl().ReadAktiveVariante(idProjekt);
                    if (v != null && v.ID_Preisreihe > 0)
                    {
                        PreisreiheModel kopf = new PreisreiheCtrl().ReadSingle(v.ID_Preisreihe);
                        if (kopf != null && kopf.Jahr > 0) return kopf.Jahr;
                    }
                }
            }
            catch (Exception ex)
            {
                // Still: Das Referenzjahr entscheidet nur ueber zwei Umstelltage. Ein
                // Lesefehler darf den Lauf nicht anhalten, der Rueckfall ist eindeutig.
                Console.WriteLine("Referenzjahr der Zeitbasis nicht lesbar: " + ex.Message);
            }
            return DbWerte.SOLAR_REFERENZJAHR_STANDARD;
        }

        /// <summary>Leert Liste und Hilfslisten - eine Stelle statt vier.</summary>
        private void Leeren()
        {
            _internalList.Clear();
            list_Temperatur.Clear();
            list_Sonnenwinkel.Clear();
            list_Tag.Clear();
        }

        /// <summary>Uebernimmt die fertige Reihe samt Hilfslisten.</summary>
        private void Uebernehmen(List<SolardatenModel> reihe)
        {
            int idx = 0;
            foreach (SolardatenModel item in reihe)
            {
                list_Temperatur.Add(item.Außen_Temp);
                list_Sonnenwinkel.Add(item.Sonnenwinkel);
                list_Tag.Add(++idx);
                _internalList.Add(item);
            }
        }

        public bool Insert(int ID_Klimaregion, List<SolardatenModel> list)
        {
            if (list == null || list.Count == 0) return true;

            try
            {
                string sqlCount = "SELECT COUNT(*) FROM Tab_Solar";
                object countResult = DataRepository.ExecuteScalar(sqlCount, null);
                int count = countResult != null ? Convert.ToInt32(countResult) : 0;

                int currentID = 1;
                if (count > 0)
                {
                    string sqlMax = "SELECT MAX(ID) FROM Tab_Solar";
                    object maxResult = DataRepository.ExecuteScalar(sqlMax, null);
                    currentID = (maxResult != null ? Convert.ToInt32(maxResult) : 0) + 1;
                }

                using (DbVorgang v = DataRepository.Vorgang())
                {
                    // SQL-Dialekt-Audit 03.09.2026: Die Spalte heisst im Schema
                    // Temperatur; "Außen_Temp" ist der Name der EIGENSCHAFT im Model
                    // (siehe MapDataRowToModel, das Temperatur nach Außen_Temp liest).
                    // Mit dem Modellnamen scheiterte der Satz an "table Tab_Solar has no
                    // column named Außen_Temp" - unter Access ebenso, nur ruft niemand
                    // diese Methode auf.
                    string sqlInsert = "INSERT INTO Tab_Solar (ID, ID_Klimaregion, Temperatur) VALUES (?, ?, ?)";

                    try
                    {
                        foreach (var item in list)
                        {
                            v.Ausfuehren(sqlInsert,
                                new DbParam("@id", DbParamTyp.Integer) { Wert = currentID },
                                new DbParam("@regId", DbParamTyp.Integer) { Wert = ID_Klimaregion },
                                new DbParam("@temp", DbParamTyp.Double) { Wert = item.Außen_Temp });

                            currentID++;
                        }

                        v.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        v.Rollback();
                        Console.WriteLine("Fehler beim Massen-Insert in der Schleife: " + ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public bool WriteDataTable(DataTable dt, string szName, DbVorgang v)
        {
            if (dt == null) return false;

            try
            {
                int nextID = 1;
                object maxRes = v.Skalar("SELECT MAX(ID) FROM Tab_Solar");
                nextID = (maxRes != DBNull.Value && maxRes != null ? Convert.ToInt32(maxRes) : 0) + 1;

                int refID = 0;
                // SQL-Dialekt-Audit 03.09.2026: Tab_Klimaregion fuehrt den Schluessel als
                // ID und den Namen als Bezeichner - ID_Klimaregion/Name gibt es nur in
                // Tab_Klimaregion_STAMM. Tab_Solar.ID_Klimaregion zeigt auf
                // Tab_Klimaregion.ID, also ist DAS der gesuchte Wert.
                string sqlRef = "SELECT ID FROM Tab_Klimaregion WHERE Bezeichner = ?";
                object refRes = v.Skalar(sqlRef,
                    new DbParam("@name", DbParamTyp.VarWChar) { Wert = szName ?? (object)DBNull.Value });
                if (refRes != null && refRes != DBNull.Value)
                {
                    refID = Convert.ToInt32(refRes);
                }

                // Zeilenweises Schreiben in der Transaktion des Vorgangs
                // Spaltenname wie im Schema (siehe Insert): Temperatur, nicht Außen_Temp.
                string sqlInsert = "INSERT INTO Tab_Solar (ID, ID_Klimaregion, Temperatur) VALUES (?, ?, ?)";
                foreach (DataRow row in dt.Rows)
                {
                    v.Ausfuehren(sqlInsert,
                        new DbParam("@id", DbParamTyp.Integer) { Wert = nextID++ },
                        new DbParam("@regId", DbParamTyp.Integer) { Wert = refID },
                        // Dynamische Typprüfung für die übergebene DataTable
                        new DbParam("@temp", DbParamTyp.Double)
                        { Wert = row[0] != DBNull.Value ? Convert.ToDouble(row[0]) : 0.0 });
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei WriteDataTable: " + ex.Message);
                DataRepository.FehlerMelden("Fehler beim Schreiben der Tabellendaten: " + ex.Message);
                return false;
            }
        }
    }
}