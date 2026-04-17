using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
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

            // Alle NumericUpDowns an die Rechenmethode binden
            foreach (var num in _Inputs.Values)
            {
                num.ValueChanged += (s, e) => Gesamtkosten();
            }

            // Einmal initial aufrufen, damit beim Start 0 oder die Startwerte da stehen
            Gesamtkosten();

            if ((Program.startfrm.status & 0x2) == 0x2) listBox_Erzeuger.Items.Add("Wärmepumpe");
            if ((Program.startfrm.status & 0x1) == 0x1) listBox_Erzeuger.Items.Add("Heizkessel");
            if ((Program.startfrm.status & 0x4) == 0x4) listBox_Erzeuger.Items.Add("Stromspeicher");
            if ((Program.startfrm.status & 256) == 256) listBox_Erzeuger.Items.Add("BHKW");
            if ((Program.startfrm.status & 512) == 512) listBox_Erzeuger.Items.Add("Solarthermie");
            if ((Program.startfrm.status & 1024) == 1024) listBox_Erzeuger.Items.Add("Photovoltaik");
            if ((Program.startfrm.status & 2048) == 2048) listBox_Erzeuger.Items.Add("Pufferspeicher");
         }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
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
            decimal sum = 0;

            // 1. Statische Werte aus dem Dictionary (Zinssatz/Zuschuss ignorieren wir für die Summe)
            foreach (var entry in _Inputs)
            {
                if (!entry.Key.Contains("Zins") && !entry.Key.Contains("Zuschuss") && !entry.Key.Contains("Nutzungsdauer"))
                {
                    sum += entry.Value.Value;
                }
            }

            // 2. Dynamische Werte aus den UserControls im FlowLayoutPanel
            foreach (Control c in flpContainer.Controls)
            {
                if (c is ucKostenZeile zeile)
                {
                    sum += zeile.Daten.Betrag;
                }
            }

            label_Gesamt.Text = $"{sum:N2} €";
        }

        // Beispiel: Wenn links eine Komponente (z.B. BHKW) gewählt wird
        private void UpdateDetailPanel(string komponente, List<KostenPosition> faktoren)
        {
            flpContainer.Controls.Clear();
            flpContainer.SuspendLayout();

            // 1. Berechne die verfügbare Innenbreite exakt
            // ClientSize.Width zieht die Scrollbar bereits automatisch ab.
            int targetWidth = flpContainer.ClientSize.Width - flpContainer.Padding.Left - flpContainer.Padding.Right;

            // Falls du einen kleinen Sicherheitsabstand zum rechten Rand willst (z.B. 5 Pixel):
            targetWidth -= 5;

            string aktuelleGruppe = "";

            foreach (var f in faktoren)
            {
                if (f.Gruppenname != aktuelleGruppe)
                {
                    aktuelleGruppe = f.Gruppenname;

                    // Blaues Gruppen-Label
                    Label groupTitle = new Label
                    {
                        AutoSize = false,
                        Text = aktuelleGruppe.ToUpper(),
                        Font = new Font(this.Font, FontStyle.Bold),
                        BackColor = Color.FromArgb(20, 40, 80),
                        ForeColor = Color.White,
                        // WICHTIG: Nutze exakt targetWidth
                        Size = new Size(targetWidth, 30),
                        TextAlign = ContentAlignment.MiddleLeft,
                        Margin = new Padding(0, 10, 0, 0)
                        
                    };
                    flpContainer.Controls.Add(groupTitle);
                    // Spalten-Header
                    Panel columnHeader = CreateColumnHeader();
                    // WICHTIG: Auch hier exakt targetWidth
                    columnHeader.Width = targetWidth;
                    flpContainer.Controls.Add(columnHeader);
                }

                var zeile = new ucKostenZeile(f);
                zeile.Width = targetWidth;

                // Das Event abfangen
                zeile.ValueChanged += (s, e) =>
                {
                    // 1. Datenbank für genau diese StammID updaten
                    UpdateSingleRowInDatabase(zeile.Daten);

                    // 2. UI Summe aktualisieren
                    Gesamtkosten();
                };

                if (f.IsMainComponent)
                {
                    zeile.BackColor = Color.LightSteelBlue;
                    zeile.Font = new Font(zeile.Font, FontStyle.Bold);
                    zeile.Margin = new Padding(0, 1, 0, 5);
                }

                zeile.DeleteRequested += Zeile_DeleteRequested;
                flpContainer.Controls.Add(zeile);
            }

            flpContainer.ResumeLayout();
        }

        private void Zeile_DeleteRequested(object sender, EventArgs e)
        {
            int StammID = 0;

            if (sender is ucKostenZeile zeile)
            {
                StammID = zeile.Daten.StammID;
                // 1. Event-Handler abmelden (saubere Speicherverwaltung)
                zeile.DeleteRequested -= Zeile_DeleteRequested;

                // 2. Aus dem FlowLayoutPanel entfernen
                flpContainer.Controls.Remove(zeile);

                // 3. Das Control endgültig zerstören
                zeile.Dispose();

                // 4. Optional: Gesamtsumme neu berechnen
                //UpdateTotalSum();

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

                        // INSERT Logik
                        string insSql = @"DELETE * FROM Tab_ProjektWerte
                                          WHERE ProjektID = @pid and StammID=@stid";

                        using (OleDbCommand insCmd = new OleDbCommand(insSql, conn, trans)) // <--- Transaktion übergeben
                        {
                            insCmd.Parameters.AddWithValue("@pid", m_ID_Projekt);
                            insCmd.Parameters.AddWithValue("@stid", StammID);
             

                            insCmd.ExecuteNonQuery();
                        }


                        // 2. Wenn alles erfolgreich war: Bestätigen
                        trans.Commit();
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

                }
            }
        }

        private Panel CreateColumnHeader()
        {
            Panel p = new Panel
            {
                Size = new Size(flpContainer.Width - 25, 20),
                BackColor = Color.LightGray,
                Margin = new Padding(0, 0, 0, 5)
            };

            // Beispielhafte Labels (Breiten müssen denen im UserControl entsprechen!)
            p.Controls.Add(new Label { Text = "Komponente", Location = new Point(5, 2), Width = 150, Font = new Font(this.Font, FontStyle.Regular) });
            p.Controls.Add(new Label { Text = "Kosten [€]", Location = new Point(160, 2), Width = 80, Font = new Font(this.Font, FontStyle.Regular) });
            p.Controls.Add(new Label { Text = "Einheit", Location = new Point(250, 2), Width = 50, Font = new Font(this.Font, FontStyle.Regular) });
            p.Controls.Add(new Label { Text = "Nutzungsdauer [a]", Location = new Point(310, 2), Width = 100, Font = new Font(this.Font, FontStyle.Regular) });

            return p;
        }

        private void listBox_Erzeuger_SelectedIndexChanged(object sender, EventArgs e)
        {
            flpContainer.Visible = true;
            btn_Hinzu.Enabled = true;

            string komponente = listBox_Erzeuger.Text;
            string kategorie = tabMain.SelectedTab.Text;

            EnsureMainComponentExists(m_ID_Projekt, kategorie, komponente, 30);
            LoadKostenFaktoren(m_ID_Projekt, kategorie, komponente);
        }

        public void LoadKostenFaktoren(int projektID, string kategorie, string komponente)
        {
            List<KostenPosition> geladeneFaktoren = new List<KostenPosition>();

            string dbPath = GetDBPath();
            string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";

            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                try
                {
                    conn.Open();

                    // SQL: Wir nutzen konsequent Parameter (?) statt Variablen im String
                    // Das Feld 'Gruppe' muss in deiner 'Abfrage_Kostenfaktoren' 
                    // jetzt auf Tab_ProjektWerte.Gruppe verweisen!
                    string sql = @"
                SELECT  ID,
                        ProjektID,
                        StammID,
                        KategorieName,
                        Komponente,
                        Bezeichnung,
                        Gruppe, 
                        EingegebenerWert,
                        Nutzungsdauer,
                        Einheit,
                        IsMainComponent
                FROM Abfrage_Kostenfaktoren
                WHERE (KategorieName = ?) 
                  AND (Komponente = ?)
                  AND (ProjektID = ?)";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        // WICHTIG: Die Reihenfolge der Parameter muss exakt dem SQL entsprechen
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = kategorie;
                        cmd.Parameters.Add("?", OleDbType.VarWChar).Value = komponente;
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = projektID;

                        using (OleDbDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                geladeneFaktoren.Add(new KostenPosition
                                {
                                    ID = Convert.ToInt32(reader["ID"]),
                                    Name = reader["Bezeichnung"].ToString(),
                                    Betrag = reader["EingegebenerWert"] != DBNull.Value
                                             ? Convert.ToDecimal(reader["EingegebenerWert"])
                                             : 0,
                                    Einheit = reader["Einheit"].ToString(),
                                    Nutzungsdauer = Convert.ToInt32(reader["Nutzungsdauer"]),
                                    IsMainComponent = Convert.ToBoolean(reader["IsMainComponent"]),
                                    // Hier wird die projekt-spezifische Gruppe geladen:
                                    Gruppenname = reader["Gruppe"] != DBNull.Value ? reader["Gruppe"].ToString() : "Allgemein",
                                    StammID = Convert.ToInt32(reader["StammID"])
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Laden der Faktoren: " + ex.Message);
                }
            }

            // UI aktualisieren
            UpdateDetailPanel(komponente, geladeneFaktoren);
        }

        private void EnsureMainComponentExists(int projektID, string kategorie, string komponente, decimal externeKosten)
        {
            try
            {
                string dbPath = GetDBPath();
                string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";

                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    int stammID = 0;

                    // --- SCHRITT 1: Stammdaten prüfen/anlegen ---
                    // Suche StammID für den Namen und IsMainComponent
                    string sqlStamm = $@"SELECT StammID FROM Abfrage_Kostenfaktoren 
                             WHERE Komponente = '{komponente}' and Bezeichnung = '{komponente}' AND IsMainComponent = True";

                    using (OleDbCommand cmd = new OleDbCommand(sqlStamm, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            stammID = Convert.ToInt32(result);
                        }
                    }

                    // --- SCHRITT 2: Projektdaten prüfen/anlegen ---
                    string sqlCheckProjekt = $@"SELECT COUNT(*) FROM Tab_ProjektWerte 
                                   WHERE ProjektID = {projektID} AND StammID = {stammID}";

                    using (OleDbCommand cmd = new OleDbCommand(sqlCheckProjekt, conn))
                    {
                        int exists = (int)cmd.ExecuteScalar();

                        if (exists == 0)
                        {
                            // Punkt statt Komma für SQL
                            string betragSql = externeKosten.ToString(System.Globalization.CultureInfo.InvariantCulture);

                            string sqlInsertWert = $@"INSERT INTO Tab_ProjektWerte (ProjektID, StammID, EingegebenerWert, Nutzungsdauer) 
                                         VALUES ({projektID}, {stammID}, {betragSql}, 0)";

                            using (OleDbCommand cmdIns = new OleDbCommand(sqlInsertWert, conn))
                            {
                                cmdIns.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Initialisieren der Hauptkomponente: " + ex.Message);
            }
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            try
            {
                string dbPath = GetDBPath();
                string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";

                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();

                    // 1. Eingabemaske öffnen
                    Form_KostenfaktorItem frm = new Form_KostenfaktorItem();
                    if (frm.ShowDialog() != DialogResult.OK) return;

                    // 2. Werte aus dem Dialog abrufen
                    int stammID = frm.gewählteID;
                    int nutzungsdauer = Convert.ToInt32(frm.Nutzungsdauer);
                    double betrag = Convert.ToDouble(frm.Wert);
                    string einheit = frm.Einheit;
                    string gewaehlteGruppe = frm.Gruppe.Trim(); // Gruppe aus der ComboBox

                    if (string.IsNullOrEmpty(gewaehlteGruppe)) gewaehlteGruppe = "Allgemein";

                    // 3. Gruppe in den Katalog aufnehmen, falls sie neu ist ("Lern-Funktion")
                    string sqlKatalog = @"INSERT INTO Tab_GruppenKatalog (GruppenName) 
                                  SELECT ? FROM (SELECT COUNT(*) FROM Tab_GruppenKatalog WHERE GruppenName = ?) AS CheckTbl 
                                  WHERE CheckTbl.[Expr1000] = 0";
                    // Hinweis: Das obige SQL ist ein "Insert if not exists" Trick für Access. 
                    // Alternativ einfach ein Try-Catch um ein normales INSERT machen.

                    try
                    {
                        using (OleDbCommand cmdKat = new OleDbCommand("INSERT INTO Tab_GruppenKatalog (GruppenName) VALUES (?)", conn))
                        {
                            cmdKat.Parameters.Add("?", OleDbType.VarWChar).Value = gewaehlteGruppe;
                            cmdKat.ExecuteNonQuery();
                        }
                    }
                    catch { /* Ignorieren, wenn Gruppe schon existiert (Duplicate Key) */ }

                    // 4. INSERT in Tab_ProjektWerte (inklusive der projekt-spezifischen Gruppe)
                    string sqlInsert = @"INSERT INTO Tab_ProjektWerte (ProjektID, StammID, EingegebenerWert, Nutzungsdauer, Einheit, Gruppe) 
                                VALUES (?, ?, ?, ?, ?, ?)";

                    using (OleDbCommand cmdIns = new OleDbCommand(sqlInsert, conn))
                    {
                        cmdIns.Parameters.Add("?", OleDbType.Integer).Value = m_ID_Projekt;
                        cmdIns.Parameters.Add("?", OleDbType.Integer).Value = stammID;
                        cmdIns.Parameters.Add("?", OleDbType.Double).Value = betrag;
                        cmdIns.Parameters.Add("?", OleDbType.Integer).Value = nutzungsdauer;
                        cmdIns.Parameters.Add("?", OleDbType.VarWChar).Value = einheit;
                        cmdIns.Parameters.Add("?", OleDbType.VarWChar).Value = gewaehlteGruppe;

                        cmdIns.ExecuteNonQuery();
                    }

                    UpdateKomponentenIDInStammdaten(stammID, tabMain.SelectedIndex+1, listBox_Erzeuger.SelectedIndex+1);
                }

                // 5. UI aktualisieren
                LoadKostenFaktoren(m_ID_Projekt, tabMain.SelectedTab.Text, listBox_Erzeuger.Text);
                Gesamtkosten();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Hinzufügen: " + ex.Message);
            }
        }

        public bool UpdateKomponentenIDInStammdaten(int stammID, int kategorieID, int neueKomponentenID)
        {
            bool erfolgreich = false;
            string dbPath = GetDBPath(); // Deine Methode zum Pfad finden
            string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";

            // SQL: Suche den Eintrag basierend auf StammID UND KategorieName
            // und setze die neue KomponentenID
            string sql = @"UPDATE Tab_Kostenfaktor 
                   SET KomponentenID = ? 
                   WHERE StammID = ? AND KategorieID = ?";

            try
            {
                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        // Parameter in der Reihenfolge der Fragezeichen (?)
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = neueKomponentenID;
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = stammID;
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = kategorieID;

                        int zeilen = cmd.ExecuteNonQuery();

                        // Wenn mindestens eine Zeile geändert wurde, war es erfolgreich
                        erfolgreich = (zeilen > 0);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Aktualisieren der Stammdaten: " + ex.Message);
                erfolgreich = false;
            }

            return erfolgreich;
        }

        private void UpdateSingleRowInDatabase(KostenPosition pos)
        {
            // Sicherheitscheck: Ohne ID kein Update
            if (pos.ID <= 0) return;

            try
            {
                string dbPath = GetDBPath();
                string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};";

                using (OleDbConnection conn = new OleDbConnection(connString))
                {
                    conn.Open();
                    // filtern NUR noch nach der eindeutigen ID
                    string sql = @"UPDATE Tab_ProjektWerte 
                           SET EingegebenerWert = @wert, 
                               Nutzungsdauer = @dauer 
                           WHERE ID = @id";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        // Parameter-Reihenfolge einhalten:
                        cmd.Parameters.Add("@wert", OleDbType.Double).Value = (double)pos.Betrag;
                        cmd.Parameters.Add("@dauer", OleDbType.Integer).Value = pos.Nutzungsdauer;
                        cmd.Parameters.Add("@id", OleDbType.Integer).Value = pos.ID;

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern: " + ex.Message);
            }
        }
    }

}