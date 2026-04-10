using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Kosten : Form
    {
        private readonly Color Navy = Color.FromArgb(15, 31, 61);
        private readonly Color NavyMid = Color.FromArgb(26, 50, 97);
        private readonly Color Accent = Color.FromArgb(59, 130, 246);
        private readonly Color Surface = Color.FromArgb(248, 249, 252);
 
        public Dictionary<string, NumericUpDown> _Inputs = new Dictionary<string, NumericUpDown>();
        public int m_ID_Projekt = 0;

        public Form_Kosten(int IDProjekt)
        {
            InitializeComponent(); // Lädt die Designer-Struktur
            m_ID_Projekt = IDProjekt;

            // UI verfeinern
            this.BackColor = Surface;
            this.tabInvest.BackColor = Surface;

            AddErzeugerList();
            InitErzeugerCtrl();
            AddInfrastrukturList();
            InitInfrastrukturCtrl();
            AddZinsZuschuss();
            AddNahwaermenetzList();
            InitNahwaermenetzCtrl();
            AddSonstigeList();
            InitSonstigeCtrl();
            AddMassnahmenList();
            InitMassnahmenCtrl();

            LoadDatenAusDictionary();
            LoadModulKostenKomponenten();

            // Alle NumericUpDowns an die Rechenmethode binden
            foreach (var num in _Inputs.Values)
            {
                num.ValueChanged += (s, e) => Gesamtkosten();
            }

            // Einmal initial aufrufen, damit beim Start 0 oder die Startwerte da stehen
            Gesamtkosten();

            if ((Program.startfrm.status & 0x2) == 0x2) panel_WP.Visible = true; else panel_WP.Visible = false;
            if ((Program.startfrm.status & 0x1) == 0x1) panel_Heizkessel.Visible = true; else panel_Heizkessel.Visible = false;
            if ((Program.startfrm.status & 0x4) == 0x4) panel_Stromspeicher.Visible = true; else panel_Stromspeicher.Visible = false;
            if ((Program.startfrm.status & 256) == 256) panel_BHKW.Visible = true; else panel_BHKW.Visible = false;
            if ((Program.startfrm.status & 512) == 512) panel_Solarthermie.Visible = true; else panel_Solarthermie.Visible = false;
            if ((Program.startfrm.status & 1024) == 1024) panel_Photovoltaik.Visible = true; else panel_Photovoltaik.Visible = false;
            if ((Program.startfrm.status & 2048) == 2048) panel_Pufferspeicher.Visible = true; else panel_Pufferspeicher.Visible = false;

        }

        private void AddErzeugerList()
        {
            _Inputs.Add("BHKW", num1);
            _Inputs.Add("BHKW_Nutzungsdauer", num2);
            _Inputs.Add("BHKW_Zinsreduktion", num3);
            _Inputs.Add("Wärmepumpe", num4);
            _Inputs.Add("Wärmepumpe_Nutzungsdauer", num5);
            _Inputs.Add("Wärmepumpe_Zinsreduktion", num6);
            _Inputs.Add("Heizkessel", num7);
            _Inputs.Add("Heizkessel_Nutzungsdauer", num8);
            _Inputs.Add("Heizkessel_Zinsreduktion", num9);
            _Inputs.Add("Stromspeicher", num10);
            _Inputs.Add("Stromspeicher_Nutzungsdauer", num11);
            _Inputs.Add("Stromspeicher_Zinsreduktion", num12);
            _Inputs.Add("Photovoltaik", num13);
            _Inputs.Add("Photovoltaik_Nutzungsdauer", num14);
            _Inputs.Add("Photovoltaik_Zinsreduktion", num15);
            _Inputs.Add("Solarthermie", num16);
            _Inputs.Add("Solarthermie_Nutzungsdauer", num17);
            _Inputs.Add("Solarthermie_Zinsreduktion", num18);
            _Inputs.Add("Pufferspeicher", num19);
            _Inputs.Add("Pufferspeicher_Nutzungsdauer", num20);
            _Inputs.Add("Pufferspeicher_Zinsreduktion", num21);
        }

        private void AddNahwaermenetzList()
        {
            _Inputs.Add("NW_Verteilernetz", num37);
            _Inputs.Add("NW_Verteilernetz_Nutzungsdauer", num38);
            _Inputs.Add("NW_Hausanschluss", num39);
            _Inputs.Add("NW_Hausanschluss_Nutzungsdauer", num40);
            _Inputs.Add("NW_Hausstation", num41);
            _Inputs.Add("NW_Hausstation_Nutzungsdauer", num42);
            _Inputs.Add("Anzahl_Hausstationen", num43);
        }

        private void InitNahwaermenetzCtrl()
        {
            _Inputs["NW_Verteilernetz"].Maximum = 9999;
            _Inputs["NW_Verteilernetz"].ThousandsSeparator = true;
            _Inputs["NW_Hausanschluss"].Maximum = 9999;
            _Inputs["NW_Hausanschluss"].ThousandsSeparator = true;
            _Inputs["NW_Hausstation"].Maximum = 9999;
            _Inputs["NW_Hausstation"].ThousandsSeparator = true;
            _Inputs["Anzahl_Hausstationen"].Maximum = 99;
        }

        private void AddSonstigeList()
        {
            _Inputs.Add("Sonstige1", num44);
            _Inputs.Add("Sonstige1_Nutzungsdauer", num45);
            _Inputs.Add("Sonstige2", num46);
            _Inputs.Add("Sonstige2_Nutzungsdauer", num47);
            _Inputs.Add("Sonstige3", num48);
            _Inputs.Add("Sonstige3_Nutzungsdauer", num49);
        }

        private void InitSonstigeCtrl()
        {
            _Inputs["Sonstige1"].Maximum = 99999;
            _Inputs["Sonstige1"].ThousandsSeparator = true;
            _Inputs["Sonstige2"].Maximum = 99999;
            _Inputs["Sonstige2"].ThousandsSeparator = true;
            _Inputs["Sonstige3"].Maximum = 99999;
            _Inputs["Sonstige3"].ThousandsSeparator = true;
        }

        private void InitErzeugerCtrl()
        {
            _Inputs["BHKW"].Maximum = 99999;
            _Inputs["BHKW"].ThousandsSeparator = true;
            _Inputs["BHKW_Zinsreduktion"].DecimalPlaces = 1;
            _Inputs["BHKW_Zinsreduktion"].Increment = 0.1m;
            _Inputs["Wärmepumpe"].Maximum = 99999;
            _Inputs["Wärmepumpe"].ThousandsSeparator = true;
            _Inputs["Wärmepumpe_Zinsreduktion"].DecimalPlaces = 1;
            _Inputs["Wärmepumpe_Zinsreduktion"].Increment = 0.1m;
            _Inputs["Heizkessel"].Maximum = 99999;
            _Inputs["Heizkessel"].ThousandsSeparator = true;
            _Inputs["Heizkessel_Zinsreduktion"].DecimalPlaces = 1;
            _Inputs["Heizkessel_Zinsreduktion"].Increment = 0.1m;
            _Inputs["Stromspeicher"].Maximum = 99999;
            _Inputs["Stromspeicher"].ThousandsSeparator = true;
            _Inputs["Stromspeicher_Zinsreduktion"].DecimalPlaces = 1;
            _Inputs["Stromspeicher_Zinsreduktion"].Increment = 0.1m;
            _Inputs["Photovoltaik"].Maximum = 99999;
            _Inputs["Photovoltaik"].ThousandsSeparator = true;
            _Inputs["Photovoltaik_Zinsreduktion"].DecimalPlaces = 1;
            _Inputs["Photovoltaik_Zinsreduktion"].Increment = 0.1m;
            _Inputs["Solarthermie"].Maximum = 99999;
            _Inputs["Solarthermie"].ThousandsSeparator = true;
            _Inputs["Solarthermie_Zinsreduktion"].DecimalPlaces = 1;
            _Inputs["Solarthermie_Zinsreduktion"].Increment = 0.1m;
            _Inputs["Pufferspeicher"].Maximum = 99999;
            _Inputs["Pufferspeicher"].ThousandsSeparator = true;
            _Inputs["Pufferspeicher_Zinsreduktion"].DecimalPlaces = 1;
            _Inputs["Pufferspeicher_Zinsreduktion"].Increment = 0.1m;
        }

        private void AddInfrastrukturList()
        {
            _Inputs.Add("Heizraum", num22);
            _Inputs.Add("Heizraum_Nutzungsdauer", num23);
            _Inputs.Add("Heizungstechnik", num24);
            _Inputs.Add("Heizungstechnik_Nutzungsdauer", num25);
            _Inputs.Add("Schornstein", num26);
            _Inputs.Add("Schornstein_Nutzungsdauer", num27);
            _Inputs.Add("Abgasanlage", num28);
            _Inputs.Add("Abgasanlage_Nutzungsdauer", num29);
            _Inputs.Add("Heizöllagerung", num30);
            _Inputs.Add("Heizöllagerung_Nutzungsdauer", num31);
            _Inputs.Add("Erdgasanschluss", num32);
            _Inputs.Add("Erdgasanschluss_Nutzungsdauer", num33);
            _Inputs.Add("Stromeinspeisung", num34);
            _Inputs.Add("Stromeinspeisung_Nutzungsdauer", num35);
            _Inputs.Add("Raumbedarf", num36);
        }


        private void InitInfrastrukturCtrl()
        {
            _Inputs["Heizraum"].Maximum = 99999;
            _Inputs["Heizungstechnik"].Maximum = 99999;
            _Inputs["Schornstein"].Maximum = 99999;
            _Inputs["Abgasanlage"].Maximum = 99999;
            _Inputs["Heizöllagerung"].Maximum = 99999;
            _Inputs["Erdgasanschluss"].Maximum = 99999;
            _Inputs["Stromeinspeisung"].Maximum = 99999;
            _Inputs["Raumbedarf"].Maximum = 999;
        }

        private void AddMassnahmenList()
        {
            _Inputs.Add("Baumassnahmen", num50);
            _Inputs.Add("Baumassnahmen_Nutzungsdauer", num51);
            _Inputs.Add("Nebenkosten", num52);
            _Inputs.Add("Nebenkosten_Nutzungsdauer", num53);
            _Inputs.Add("Planungskosten", num54);
            _Inputs.Add("Planungskosten_Nutzungsdauer", num55);
        }

        private void InitMassnahmenCtrl()
        {
            _Inputs["Baumassnahmen"].Maximum = 99999;
            _Inputs["Baumassnahmen"].ThousandsSeparator = true;
            _Inputs["Nebenkosten"].Maximum = 99999;
            _Inputs["Nebenkosten"].ThousandsSeparator = true;
            _Inputs["Planungskosten"].Maximum = 99999;
            _Inputs["Planungskosten"].ThousandsSeparator = true;
        }

        private void AddZinsZuschuss()
        {
            _Inputs.Add("Zinssatz", num_Zinssatz);
            _Inputs.Add("Zuschuss", num_Zuschuss);
            _Inputs["Zinssatz"].DecimalPlaces = 1;
            _Inputs["Zinssatz"].Increment = 0.1m;
            _Inputs["Zuschuss"].DecimalPlaces = 1;
            _Inputs["Zuschuss"].Increment = 0.1m;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if(SaveDatenAusDictionary(m_ID_Projekt))
                MessageBox.Show("Daten gespeichert");
            else
                MessageBox.Show("Daten nicht gespeichert!", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;  
            Close();    
        }

        // DATEN LADEN
        public void LoadDatenAusDictionary()
        {
            string dbPath = GetDBPath();
            string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";

            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                conn.Open();

                // Wir holen die Werte über einen Join von Stamm (Bezeichnung) und Projektwerten
                string sql = @"SELECT
                                Tab_KostenStamm.Bezeichnung,
                                Tab_KostenStamm.Default,
                                Tab_ProjektWerte.EingegebenerWert
                            FROM
                                Tab_KostenStamm
                                LEFT JOIN Tab_ProjektWerte ON Tab_KostenStamm.StammID = Tab_ProjektWerte.StammID";

                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@pid", m_ID_Projekt);
                    using (OleDbDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string bez = reader["Bezeichnung"].ToString();
                            decimal wert = reader["EingegebenerWert"] == DBNull.Value ? Convert.ToDecimal(reader["Default"]) : Convert.ToDecimal(reader["EingegebenerWert"]);

                            // Prüfen, ob dieser Datenbank-Bezeichner in deinem Dictionary existiert
                            if (_Inputs.ContainsKey(bez))
                            {
                                _Inputs[bez].Value = wert;
                            }
                        }
                    }
                }
            }
        }

        // DATEN SPEICHERN
        public bool SaveDatenAusDictionary(int projektID)
        {
            string dbPath = GetDBPath();
            string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";

            // Transaktion außerhalb deklarieren, damit wir sie im catch-Block erreichen
            OleDbTransaction trans = null;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    // 1. Transaktion starten
                    trans = conn.BeginTransaction();

                    foreach (var entry in _Inputs)
                    {
                        string bez = entry.Key;
                        double wertFuerDB = Convert.ToDouble(entry.Value.Value);

                        // UPDATE Logik
                        string sql = @"UPDATE Tab_ProjektWerte 
                               INNER JOIN Tab_KostenStamm ON Tab_ProjektWerte.StammID = Tab_KostenStamm.StammID
                               SET Tab_ProjektWerte.EingegebenerWert = @wert
                               WHERE Tab_ProjektWerte.ProjektID = @pid 
                               AND Tab_KostenStamm.Bezeichnung = @bez";

                        using (OleDbCommand cmd = new OleDbCommand(sql, conn, trans)) // <--- Transaktion übergeben
                        {
                            cmd.Parameters.Add("@wert", OleDbType.Double).Value = wertFuerDB;
                            cmd.Parameters.AddWithValue("@pid", projektID);
                            cmd.Parameters.AddWithValue("@bez", bez);

                            if (cmd.ExecuteNonQuery() == 0)
                            {
                                // INSERT Logik
                                string insSql = @"INSERT INTO Tab_ProjektWerte (ProjektID, StammID, EingegebenerWert)
                                          SELECT @pid, StammID, @wert 
                                          FROM Tab_KostenStamm 
                                          WHERE Bezeichnung = @bez";

                                using (OleDbCommand insCmd = new OleDbCommand(insSql, conn, trans)) // <--- Transaktion übergeben
                                {
                                    insCmd.Parameters.AddWithValue("@pid", projektID);
                                    insCmd.Parameters.AddWithValue("@wert", wertFuerDB);
                                    insCmd.Parameters.AddWithValue("@bez", bez);

                                    insCmd.ExecuteNonQuery();
                                }
                            }
                        }
                    }

                    // 2. Wenn alles erfolgreich war: Bestätigen
                    trans.Commit();
                    return true;
                }
            }
            catch (Exception ex)
            {
                // 3. Im Fehlerfall: Alles rückgängig machen
                try
                {
                    if (trans != null) trans.Rollback();
                }
                catch { /* Ignorieren, falls Rollback selbst fehlschlägt */ }

                // Optional: Loggen oder Fehlermeldung
                // MessageBox.Show("Fehler beim Speichern. Die Änderungen wurden nicht übernommen: " + ex.Message);
                return false;
            }
        }
        
        private string GetDBPath()
        {
            string db = "";
            string userPath = $@"SOFTWARE\ODBC\ODBC.INI\TEST";
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(userPath))
            {
                if (key != null)
                {
                    db = key.GetValue("DBQ")?.ToString() ?? key.GetValue("Database")?.ToString();
                }
            }
            return db;
        }

        private void Gesamtkosten()
        {
            decimal sum = 0; // Nutze decimal für Geldwerte (Präzision!)

            foreach (var entry in _Inputs)
            {
                string key = entry.Key;

                // Prüfe, ob der Key NICHT "Nutzungsdauer" und NICHT "Zins" enthält
                // Wir rechnen nur die reinen Kosten-Keys zusammen
                if (!key.Contains("_Nutzungsdauer") && !key.Contains("_Zins") && !key.Contains("Zinssatz") && !key.Contains("Zuschuss"))
                {
                    sum += entry.Value.Value;
                }
            }

            // Anzeige mit Tausendertrenner und Euro-Zeichen
            label_Gesamt.Text = $"{sum:N2}";
        }

        private void LoadModulKostenKomponenten()
        {
            RecordSet rs = new RecordSet();
            rs.Open("Select * from Abfrage_Kosten_WP where ID_Projekt=" + m_ID_Projekt);
            if (rs.Next()) _Inputs["Wärmepumpe"].Value = Convert.ToDecimal(rs.Read("Gesamt"));
            rs.Close();
            rs.Open("Select * from Abfrage_Kosten_BHKW where ID_Projekt=" + m_ID_Projekt);
            if (rs.Next()) _Inputs["BHKW"].Value = Convert.ToDecimal(rs.Read("Gesamt"));
            rs.Close();
            rs.Open("Select * from Abfrage_Kosten_Heizkessel where ID_Projekt=" + m_ID_Projekt);
            if (rs.Next()) _Inputs["Heizkessel"].Value = Convert.ToDecimal(rs.Read("Gesamt"));
            rs.Close();
            rs.Open("Select * from Abfrage_Kosten_Pufferspeicher where ID_Projekt=" + m_ID_Projekt);
            if (rs.Next()) _Inputs["Pufferspeicher"].Value = Convert.ToDecimal(rs.Read("Gesamt"));
            rs.Close();
            rs.Open("Select * from Abfrage_Kosten_Stromspeicher where ID_Projekt=" + m_ID_Projekt);
            if (rs.Next()) _Inputs["Stromspeicher"].Value = Convert.ToDecimal(rs.Read("Gesamt"));
            rs.Close();
            rs.Open("Select * from Abfrage_Kosten_Photovoltaik where ID_Projekt=" + m_ID_Projekt);
            if (rs.Next()) _Inputs["Photovoltaik"].Value = Convert.ToDecimal(rs.Read("Gesamt"));
            rs.Close();
            rs.Open("Select * from Abfrage_Kosten_Solarthermie where ID_Projekt=" + m_ID_Projekt);
            if (rs.Next()) _Inputs["Solarthermie"].Value = Convert.ToDecimal(rs.Read("Gesamt"));
            rs.Close();

        }
    }
}