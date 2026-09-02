using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class WErzeugerCtrl : WErzeugerModel
    {
        private List<WErzeugerModel> _internalList = new List<WErzeugerModel>();
        public int rows => _internalList.Count;
        public new List<WErzeugerModel> items => _internalList;

        public WErzeugerCtrl()
        {
        }

        /// <summary>
        /// Ändert die Grunddaten einer Anlagenzeile.
        ///
        /// <para>
        /// Bewusst NICHT auf den vollen Spaltensatz erweitert: Ein UPDATE lässt die
        /// nicht genannten Spalten unverändert stehen - es ist also nicht verlustbehaftet
        /// und damit keine zweite Wahrheit über die Quellen-/Senken-Konfiguration. Die
        /// pflegen <c>WaermesenkeClass.Schreiben</c> und
        /// <c>WaermequelleClass.WertSchreiben</c> gezielt je Anlage. Verlustbehaftet ist
        /// allein der Weg Löschen + Neuanlegen - der geht über
        /// <see cref="Insert"/> bzw. <see cref="WizardCtrl.SQL_ANLAGE_INSERT"/>.
        /// </para>
        /// </summary>
        public bool Update()
        {
            try
            {
                string sql = @"UPDATE Tab_Energieanlagen 
                               SET ID_Projekt = ?, Bezeichner = ?, ID_Type = ?, ID_WP = ?, Betriebsart = ?, 
                                   Sperrung = ?, Sperrzeit_von = ?, Sperrzeit_bis = ?, Vorlauf = ?, Rücklauf = ?,
                                   Bivalenter_Betrieb = ?, Abschaltpunkt = ?, Nutzungszeit = ?, ID_SP = ?, ID_PV = ?, ID_Solar = ?
                               WHERE ID = ?";

                OleDbParameter[] ps = {
                    new OleDbParameter("@idProj", ID_Projekt),
                    new OleDbParameter("@bez", Bezeichner ?? (object)DBNull.Value),
                    new OleDbParameter("@idType", ID_Type),
                    new OleDbParameter("@idWp", ID_WP),
                    new OleDbParameter("@betr", Betriebsart ?? (object)DBNull.Value),
                    new OleDbParameter("@sperr", Sperrung),
                    new OleDbParameter("@von", Sperrzeit_von),
                    new OleDbParameter("@bis", Sperrzeit_bis),
                    new OleDbParameter("@vor", Vorlauf),
                    new OleDbParameter("@rue", Ruecklauf),
                    new OleDbParameter("@biv", Bivalenter_Betrieb),
                    new OleDbParameter("@absch", Abschaltpunkt),
                    new OleDbParameter("@nutz", Nutzungszeit),
                    new OleDbParameter("@idSp", ID_SP),
                    new OleDbParameter("@idPv", ID_PV),
                    new OleDbParameter("@idSol", ID_Solar),
                    new OleDbParameter("@id", ID) // Die ID am Ende bestimmt die WHERE-Klausel
                };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Update: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Entfernt ALLE Anlagenzeilen eines Projekts - und seit dem 22.08.2026 auch die
        /// Gerätezeilen, auf die danach nichts mehr zeigt.
        ///
        /// <para>
        /// DIESE METHODE IST DER PROJEKT-LÖSCHWEG, nicht der Speicherweg. Ihre beiden
        /// Aufrufer sind <c>MenueCtrl.ProjektDelete</c> und
        /// <c>VariantenCtrl.LoescheVariante</c>; gespeichert wird über
        /// <see cref="WizardCtrl.Del_Projekt_Waermeerzeuger"/> +
        /// <see cref="WizardCtrl.Add_WP_Waermeerzeuger"/>. Weil hier alle Anlagenzeilen
        /// fallen, ist danach JEDE Gerätezeile des Projekts verwaist.
        /// </para>
        ///
        /// <para>
        /// WARUM DAS NÖTIG IST. Von den sieben Gerätetabellen hängt nur
        /// <c>Tab_Pufferspeicher</c> mit Löschweitergabe an <c>Tab_Projekt</c>. Die
        /// übrigen sechs behielten ihre Zeilen: Auf der Arbeitskopie standen am
        /// 22.08.2026 Gerätezeilen zu sieben Projekt-IDs, die es in <c>Tab_Projekt</c>
        /// längst nicht mehr gibt. Sie waren über keine Oberfläche mehr erreichbar und
        /// wuchsen mit jedem gelöschten Projekt weiter.
        /// </para>
        ///
        /// <para>
        /// DER AUFRÄUMLAUF DARF DAS LÖSCHEN NICHT SCHEITERN LASSEN. Er läuft NACH dem
        /// erfolgreichen DELETE und sein Ergebnis geht nicht in den Rückgabewert ein:
        /// Was er nicht wegräumt, ist Altbestand wie bisher - der Migrationsschritt holt
        /// ihn beim nächsten Programmstart nach.
        /// </para>
        /// </summary>
        public bool Delete()
        {
            try
            {
                // Korrektur: DELETE * FROM bzw. DELETE FROM statt der alten fehlerhaften Syntax "DELETE ID_Projekt FROM..."
                string sql = "DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ?";
                OleDbParameter[] ps = { new OleDbParameter("@idProj", ID_Projekt) };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;

                GeraeteWaisen.Aufraeumen(ID_Projekt);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Delete: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Legt eine Anlagenzeile an - über DIESELBE Anweisung wie der Wizard-Weg
        /// (<see cref="WizardCtrl.SQL_ANLAGE_INSERT"/>).
        ///
        /// <para>
        /// Die frühere eigene Fassung führte 21 der 57 Spalten und schrieb
        /// <c>WS_Ladeprio*</c>/<c>WS_Ladegrenze*</c> mit hartkodierter 0. Das war die
        /// zweite Halbwahrheit über denselben Datensatz: zwei Einfügewege mit
        /// unterschiedlichem Spaltensatz. Die 0 („nach Vorgabe" bzw. „nicht gesetzt",
        /// Konzept 3.4, Paket-4-Review Punkt 9) steckt jetzt in der Vorbelegung von
        /// <see cref="WErzeugerModel"/> und gilt damit für JEDEN Einfügeweg; die
        /// Fremdschlüsselspalten (<c>WS_ID_Puffer</c> &amp; Co.) bleiben dort NULL.
        /// </para>
        /// </summary>
        public bool Insert()
        {
            try
            {
                return DataRepository.ExecuteSQL(WizardCtrl.SQL_ANLAGE_INSERT,
                                                 WizardCtrl.AnlagenParameter(ID_Projekt, this));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Insert: " + ex.Message);
                return false;
            }
        }

        public void ReadAllFilter(string filter = "")
        {
            string sql;
            if (string.IsNullOrEmpty(filter))
            {
                sql = "SELECT * FROM Tab_Energieanlagen ORDER BY Bezeichner";
            }
            else
            {
                sql = "SELECT * FROM Tab_Energieanlagen WHERE " + filter;
            }

            DataTable dt = DataRepository.GetDataTable(sql, null);
            _internalList.Clear();

            if (dt == null) return;

            foreach (DataRow row in dt.Rows)
            {
                WErzeugerModel item = new WErzeugerModel();
                AusZeile(dt, row, item);
                _internalList.Add(item);
            }
        }

        public void ReadSingle(string sql)
        {
            DataTable dt = DataRepository.GetDataTable(sql, null);

            // "rows" Variable spiegelt im Single-Modus die Existenz wider (0 oder 1)
            if (dt != null && dt.Rows.Count > 0)
            {
                AusZeile(dt, dt.Rows[0], this);
            }
        }

        /// <summary>
        /// Überträgt eine Zeile aus <c>Tab_Energieanlagen</c> in ein Modell - die EINE
        /// Leseabbildung für <see cref="ReadAllFilter"/> und <see cref="ReadSingle"/>.
        ///
        /// <para>
        /// Beide Methoden hatten dieselbe Spaltenliste zweimal ausgeschrieben und waren
        /// bereits auseinandergelaufen (in <c>ReadAllFilter</c> stand bei <c>Azimut</c>
        /// ein nicht kurzschließendes <c>&amp;</c>, das bei fehlender Spalte geworfen
        /// hätte). Eine Abbildung bedeutet: Was gelesen wird, wird auch geschrieben -
        /// die Symmetrie zu <see cref="WizardCtrl.SQL_ANLAGE_INSERT"/> ist an einer
        /// einzigen Stelle prüfbar.
        /// </para>
        ///
        /// <para>
        /// ZWEI ZUWEISUNGSMUSTER, mit Absicht. Die 30 Bestandsspalten behalten das
        /// bisherige Muster „vorhanden und nicht NULL ⇒ übernehmen", sonst bleibt die
        /// Vorbelegung stehen. Die 27 Spalten der Quellen-/Senken-Konfiguration werden
        /// dagegen AUSDRÜCKLICH zugewiesen, auch mit <c>null</c>: Ihre Vorbelegung ist
        /// bei den Ladeprioritäten die 0, und ein NULL aus der Datenbank würde sonst
        /// beim Zurückschreiben zur 0 - der Roundtrip muss beide Zustände
        /// unterscheiden (Konzept 3.4).
        /// </para>
        /// </summary>
        private static void AusZeile(DataTable dt, DataRow row, WErzeugerModel item)
        {
            // --- Bestandsspalten ------------------------------------------------------
            if (Belegt(dt, row, "ID")) item.ID = Convert.ToInt32(row["ID"]);
            if (Belegt(dt, row, "ID_Projekt")) item.ID_Projekt = Convert.ToInt32(row["ID_Projekt"]);
            if (Belegt(dt, row, "Bezeichner")) item.Bezeichner = row["Bezeichner"].ToString();
            if (Belegt(dt, row, "ID_Type")) item.ID_Type = Convert.ToInt32(row["ID_Type"]);
            if (Belegt(dt, row, "ID_WP")) item.ID_WP = Convert.ToInt32(row["ID_WP"]);
            if (Belegt(dt, row, "Betriebsart")) item.Betriebsart = row["Betriebsart"].ToString();
            if (Belegt(dt, row, "Sperrung")) item.Sperrung = Convert.ToBoolean(row["Sperrung"]);
            if (Belegt(dt, row, "Sperrzeit_von")) item.Sperrzeit_von = Convert.ToInt32(row["Sperrzeit_von"]);
            if (Belegt(dt, row, "Sperrzeit_bis")) item.Sperrzeit_bis = Convert.ToInt32(row["Sperrzeit_bis"]);
            if (Belegt(dt, row, "Vorlauf")) item.Vorlauf = Convert.ToInt32(row["Vorlauf"]);
            if (Belegt(dt, row, "Rücklauf")) item.Ruecklauf = Convert.ToInt32(row["Rücklauf"]);
            if (Belegt(dt, row, "Bivalenter_Betrieb")) item.Bivalenter_Betrieb = Convert.ToBoolean(row["Bivalenter_Betrieb"]);
            if (Belegt(dt, row, "Abschaltpunkt")) item.Abschaltpunkt = Convert.ToDouble(row["Abschaltpunkt"]);
            if (Belegt(dt, row, "Nutzungszeit")) item.Nutzungszeit = Convert.ToInt32(row["Nutzungszeit"]);
            if (Belegt(dt, row, "ID_SP")) item.ID_SP = Convert.ToInt32(row["ID_SP"]);
            if (Belegt(dt, row, "ID_PV")) item.ID_PV = Convert.ToInt32(row["ID_PV"]);
            if (Belegt(dt, row, "ID_Solar")) item.ID_Solar = Convert.ToInt32(row["ID_Solar"]);
            if (Belegt(dt, row, "Heizstab")) item.Heizstab = Convert.ToBoolean(row["Heizstab"]);
            if (Belegt(dt, row, "Volumen")) item.Volumen = Convert.ToDouble(row["Volumen"]);
            if (Belegt(dt, row, "rendeMix")) item.rendeMix = Convert.ToBoolean(row["rendeMix"]);
            if (Belegt(dt, row, "Solaranteil")) item.Solaranteil = Convert.ToInt32(row["Solaranteil"]);
            if (Belegt(dt, row, "ID_Kessel")) item.ID_Kessel = Convert.ToInt32(row["ID_Kessel"]);
            if (Belegt(dt, row, "ID_BHKW")) item.ID_BHKW = Convert.ToInt32(row["ID_BHKW"]);
            if (Belegt(dt, row, "Grenzleistung")) item.Grenzleistung = Convert.ToDouble(row["Grenzleistung"]);
            if (Belegt(dt, row, "Kollektormodulanzahl")) item.Kollektormodulanzahl = Convert.ToInt32(row["Kollektormodulanzahl"]);
            if (Belegt(dt, row, "PV_Leistung")) item.PV_Leistung = Convert.ToDouble(row["PV_Leistung"]);
            if (Belegt(dt, row, "Neigung")) item.m_Neigung = Convert.ToInt32(row["Neigung"]);
            if (Belegt(dt, row, "Azimut")) item.m_Azimut = Convert.ToInt32(row["Azimut"]);
            if (Belegt(dt, row, "ID_PUFFER")) item.ID_PUFFER = Convert.ToInt32(row["ID_PUFFER"]);
            // Ohne diese Zeile verliert der Bearbeiten-Zweig des Wizards den Energietraeger:
            // WizardParent.LoadWEFromDB uebernimmt ID_Carrier aus diesen Modellen, und
            // Del_Projekt_Waermeerzeuger + Add_WP_Waermeerzeuger schrieben die Anlagen
            // anschliessend mit ID_Carrier = 0 zurueck.
            // Über den ROHWERT, damit NULL nicht zur 0 wird (siehe WErzeugerModel).
            item.ID_CarrierRoh = Zahl(dt, row, "ID_Carrier");

            // --- Quellen-/Senken-Konfiguration (ausdrücklich, auch mit null) -----------
            item.Prioritaet = Zahl(dt, row, "Prioritaet");
            item.BM_Typ = Text(dt, row, "BM_Typ");

            item.WQ_Typ = Text(dt, row, "WQ_Typ");
            item.WQ_Temp = Kommazahl(dt, row, "WQ_Temp");
            item.WQ_Monatswerte = Text(dt, row, "WQ_Monatswerte");
            item.WQ_Wochenwerte = Text(dt, row, "WQ_Wochenwerte");
            item.WQ_CSV = Text(dt, row, "WQ_CSV");
            item.WQ_Puffer = Text(dt, row, "WQ_Puffer");
            item.WQ_ID_Puffer = Zahl(dt, row, "WQ_ID_Puffer");
            item.WQ_Spreizung = Kommazahl(dt, row, "WQ_Spreizung");
            item.WQ_Regeneration = Kommazahl(dt, row, "WQ_Regeneration");
            item.WQ_Unbegrenzt = JaNein(dt, row, "WQ_Unbegrenzt");
            item.WQ_Tiefe = Kommazahl(dt, row, "WQ_Tiefe");
            item.WQ_Flaeche = Kommazahl(dt, row, "WQ_Flaeche");
            item.WQ_Anzahl = Zahl(dt, row, "WQ_Anzahl");
            item.WQ_Bodentyp = Text(dt, row, "WQ_Bodentyp");
            item.WQ_Quellsystem = Text(dt, row, "WQ_Quellsystem");

            item.WS_Typ = Text(dt, row, "WS_Typ");
            item.WS_Ziel = Text(dt, row, "WS_Ziel");
            item.WS_ID_Puffer = Zahl(dt, row, "WS_ID_Puffer");
            item.WS_Ladeprio = Zahl(dt, row, "WS_Ladeprio");
            item.WS_Ladegrenze = Kommazahl(dt, row, "WS_Ladegrenze");
            item.WS_Ladeprio_PV = Zahl(dt, row, "WS_Ladeprio_PV");
            item.WS_Ziel2 = Text(dt, row, "WS_Ziel2");
            item.WS_ID_Puffer2 = Zahl(dt, row, "WS_ID_Puffer2");
            item.WS_Ladeprio2 = Zahl(dt, row, "WS_Ladeprio2");
            item.WS_Ladegrenze2 = Kommazahl(dt, row, "WS_Ladegrenze2");

            // --- PV-Anlagenparameter (Paket A, Stufe E1.3) ----------------------------
            // Ausdruecklich auch mit null - NULL heisst hier "nie gepflegt, es gilt der
            // Vorgabewert" (0,95 bzw. 0 %) und darf beim Zurueckschreiben nicht zur 0
            // werden. Belegt() behandelt eine FEHLENDE Spalte wie NULL; eine Datenbank
            // vor Migrationsschritt 62 laeuft damit unveraendert weiter.
            item.PV_WrWirkungsgrad = Kommazahl(dt, row, "PV_WrWirkungsgrad");
            item.PV_Systemverluste = Kommazahl(dt, row, "PV_Systemverluste");
        }

        /// <summary>Spalte vorhanden UND nicht NULL - eine fehlende Spalte gilt wie NULL.</summary>
        private static bool Belegt(DataTable dt, DataRow row, string spalte)
        {
            return dt.Columns.Contains(spalte) && row[spalte] != DBNull.Value;
        }

        /// <summary>Text oder <c>null</c>; der Unterschied NULL/Leerstring bleibt erhalten.</summary>
        private static string Text(DataTable dt, DataRow row, string spalte)
        {
            return Belegt(dt, row, spalte) ? row[spalte].ToString() : null;
        }

        private static int? Zahl(DataTable dt, DataRow row, string spalte)
        {
            return Belegt(dt, row, spalte) ? (int?)Convert.ToInt32(row[spalte]) : null;
        }

        private static double? Kommazahl(DataTable dt, DataRow row, string spalte)
        {
            return Belegt(dt, row, spalte) ? (double?)Convert.ToDouble(row[spalte]) : null;
        }

        /// <summary>Ja/Nein-Feld; Access kennt dort kein NULL, fehlende Spalte = false.</summary>
        private static bool JaNein(DataTable dt, DataRow row, string spalte)
        {
            return Belegt(dt, row, spalte) && Convert.ToBoolean(row[spalte]);
        }
    }
}