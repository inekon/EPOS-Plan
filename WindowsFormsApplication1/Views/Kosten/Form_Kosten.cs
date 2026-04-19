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

            // Einmal initial aufrufen, damit beim Start 0 oder die Startwerte da stehen
            Gesamtkosten();

            if ((Program.startfrm.status & 0x2) == 0x2) listBox_Erzeuger.Items.Add("Wärmepumpe");
            if ((Program.startfrm.status & 0x1) == 0x1) listBox_Erzeuger.Items.Add("Heizkessel");
            if ((Program.startfrm.status & 1024) == 1024) listBox_Erzeuger.Items.Add("Photovoltaik");
            if ((Program.startfrm.status & 512) == 512) listBox_Erzeuger.Items.Add("Solarthermie");
            if ((Program.startfrm.status & 0x4) == 0x4) listBox_Erzeuger.Items.Add("Stromspeicher");
            if ((Program.startfrm.status & 2048) == 2048) listBox_Erzeuger.Items.Add("Pufferspeicher");
            if ((Program.startfrm.status & 256) == 256) listBox_Erzeuger.Items.Add("BHKW");
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
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

        private void Gesamtkosten(string aktuelleSelektion = "")
        {
            decimal summeGesamt = 0;
            decimal summeSelektion = 0;

            // Die Summe der AKTUELLEN Selektion direkt aus den Controls lesen (Live-Werte)
            foreach (Control c in flpContainer.Controls)
            {
                if (c is ucKostenZeile zeile)
                {
                    summeSelektion += zeile.Daten.Betrag;
                }
            }

            // Die Gesamtsumme berechnen:
            // Summe aller ANDEREN Komponenten aus der Datenbank
            // und live berechnete summeSelektion dazu addieren.
            RecordSet rs = new RecordSet();
            rs.Open($"SELECT Komponente, Summe FROM Abfrage_KostenKomponenten WHERE ProjektID = {m_ID_Projekt}");

            while (rs.Next())
            {
                string komponente = rs.Read("Komponente").ToString();
                decimal betrag = rs.Read("Summe") != DBNull.Value ? Convert.ToDecimal(rs.Read("Summe")) : 0;

                // Wenn es NICHT die aktuelle Komponente ist, zur Gesamtsumme addieren
                if (komponente != aktuelleSelektion)
                {
                    summeGesamt += betrag;
                }
            }

            // Jetzt die live berechnete Selektion zur Gesamtsumme addieren
            summeGesamt += summeSelektion;

            // Anzeige aktualisieren
            label_ErzeugerGesamt.Text = $"Kosten {aktuelleSelektion}: {summeSelektion:N2} €";
            label_Gesamt.Text = $"PROJEKT GESAMT: {summeGesamt:N2} €";

            label_ErzeugerGesamt.Refresh();
            label_Gesamt.Refresh();
        }

        // Beispiel: Wenn links eine Komponente (z.B. BHKW) gewählt wird
        private void UpdateDetailPanel(string komponente, List<KostenPosition> faktoren)
        {
            flpContainer.Controls.Clear();
            flpContainer.SuspendLayout();

            // Berechnung verfügbare Innenbreite
            // ClientSize.Width zieht die Scrollbar bereits automatisch ab.
            int targetWidth = flpContainer.ClientSize.Width - flpContainer.Padding.Left - flpContainer.Padding.Right;

            // Falls ein kleiner Sicherheitsabstand zum rechten Rand sein soll (z.B. 5 Pixel):
            targetWidth -= 5;

            string aktuelleGruppe = "";

            foreach (var f in faktoren)
            {
                if (f.Gruppenname != aktuelleGruppe)
                {
                    aktuelleGruppe = f.Gruppenname;

                    // Wir erstellen ein Panel als Container für den Header
                    Panel headerPanel = new Panel
                    {
                        Size = new Size(targetWidth, 30),
                        BackColor = Color.FromArgb(20, 40, 80),
                        Margin = new Padding(0, 10, 0, 0),
                        Tag = aktuelleGruppe // Wichtig für die Lösch-Identifizierung
                    };

                    // Das Label für den Text
                    Label groupTitle = new Label
                    {
                        Text = aktuelleGruppe.ToUpper(),
                        Font = new Font(this.Font, FontStyle.Bold),
                        ForeColor = Color.White,
                        AutoSize = false,
                        Dock = DockStyle.Fill, // Nimmt den restlichen Platz ein
                        TextAlign = ContentAlignment.MiddleLeft,
                        Padding = new Padding(5, 0, 0, 0)
                    };

                    // Der Lösch-Button (-)
                    Button btnDeleteGroup = new Button
                    {
                        Text = "-",
                        Size = new Size(25, 25),
                        Dock = DockStyle.Right, // Ganz nach rechts im Panel
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.White,
                        BackColor = Color.Firebrick, // Dezentes Rot
                        Cursor = Cursors.Hand,
                        Tag = aktuelleGruppe, // Speichert den Gruppennamen für das Event
                        Font = new Font("Arial", 10, FontStyle.Bold)
                    };
                    btnDeleteGroup.FlatAppearance.BorderSize = 0;
                    btnDeleteGroup.Click += btnDeleteGroup_Click; // Event verknüpfen

                    // Controls zum Header-Panel hinzufügen
                    headerPanel.Controls.Add(groupTitle);
                    headerPanel.Controls.Add(btnDeleteGroup);
                    Panel columnHeader = CreateColumnHeader(aktuelleGruppe);
                    // WICHTIG: Auch hier exakt targetWidth
                    columnHeader.Width = targetWidth;
                   
                    flpContainer.Controls.Add(headerPanel);
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
                    Gesamtkosten(listBox_Erzeuger.Text);
                };

                zeile.Tag = aktuelleGruppe;
                if (f.IsMainComponent)
                {
                    zeile.BackColor = Color.LightSteelBlue;
                    zeile.Font = new Font(zeile.Font, FontStyle.Bold);
                    zeile.Margin = new Padding(0, 1, 0, 5);
                }
                zeile.DeleteRequested += Zeile_DeleteRequested;
                zeile.Daten.Komponente = listBox_Erzeuger.Text; 
                flpContainer.Controls.Add(zeile);
            }

            flpContainer.ResumeLayout();
        }

        private void btnDeleteGroup_Click(object sender, EventArgs e)
        {
            Button btn = (Button)sender;
            string gruppenName = btn.Tag.ToString();

            List<ucKostenZeile> gruppenZeilen = new List<ucKostenZeile>();
            bool enthältMainComponent = false;

            // alle Zeilen dieser Gruppe im Container
            foreach (Control c in flpContainer.Controls)
            {
                if (c is ucKostenZeile zeile && c.Tag?.ToString() == gruppenName)
                {
                    gruppenZeilen.Add(zeile);
                    if (zeile.Daten.IsMainComponent)
                    {
                        enthältMainComponent = true;
                    }
                }
            }

            // --- LOGIK-SPERRE ---
            // Wenn die Gruppe eine MainComponent enthält UND dies das einzige Element ist, 
            // oder wenn die Gruppe NUR aus der MainComponent besteht: Nichts tun.
            if (enthältMainComponent && gruppenZeilen.Count <= 1)
            {
                return; // Einfach abbrechen, keine MessageBox, keine Aktion.
            }

            // MessageBox nur zeigen, wenn löschbare (nicht-Main) Komponenten existieren
            string meldung = $"Möchten Sie die Gruppe '{gruppenName}' mit allen Kostenfaktoren löschen? (Die Hauptkomponente bleibt erhalten)";

            var confirm = MessageBox.Show(meldung, "Gruppe leeren", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    // Datenbank: Nur die Faktoren löschen, die KEINE MainComponent sind
                    DeleteGruppeAusDatenbank(gruppenName, m_ID_Projekt);

                    // UI: Nur die Zeilen entfernen, die keine MainComponent sind
                    flpContainer.SuspendLayout();
                    for (int i = flpContainer.Controls.Count - 1; i >= 0; i--)
                    {
                        Control c = flpContainer.Controls[i];
                        if (c.Tag?.ToString() == gruppenName)
                        {
                            // Falls es eine Zeile ist, prüfen wir IsMainComponent
                            if (c is ucKostenZeile zeile)
                            {
                                if (zeile.Daten.IsMainComponent) continue; // MainComponent überspringen
                            }

                            // ColumnHeader und normale Zeilen löschen
                            // (Das Header-Panel mit dem Namen lassen wir evtl. auch stehen?)
                            if (c is Panel && c.Height > 25) continue; // Header stehen lassen

                            flpContainer.Controls.Remove(c);
                            c.Dispose();
                        }
                    }
                    flpContainer.ResumeLayout();

                    Gesamtkosten(listBox_Erzeuger.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Bereinigen der Gruppe: " + ex.Message);
                }
            }
        }

        private void DeleteGruppeAusDatenbank(string gruppenName, int projektID)
        {
            string dbPath = GetDBPath();
            string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};";

            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                conn.Open();
                // Löscht alle Faktoren dieser Gruppe aus dem aktuellen Projekt
                string sql = "DELETE FROM Tab_ProjektWerte WHERE Gruppe = ? AND ProjektID = ?";
                using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                {
                    cmd.Parameters.Add("?", OleDbType.VarWChar).Value = gruppenName;
                    cmd.Parameters.Add("?", OleDbType.Integer).Value = projektID;
                    cmd.ExecuteNonQuery();
                }
            }

            using (OleDbConnection conn = new OleDbConnection(connString))
            {
                conn.Open();

                // SQL: Lösche die Gruppe aus dem Katalog NUR DANN, 
                // wenn kein einziger Eintrag in Tab_ProjektWerte diesen Namen mehr benutzt.
                string sqlCleanupKatalog = @"DELETE FROM Tab_KostenGruppenKatalog 
                                WHERE GruppenName = ? 
                                AND NOT EXISTS (SELECT 1 FROM Tab_ProjektWerte WHERE Gruppe = ?)";

                using (OleDbCommand cmdCleanup = new OleDbCommand(sqlCleanupKatalog, conn))
                {
                    // Wir brauchen den Parameter zweimal (einmal für das WHERE, einmal für das NOT EXISTS)
                    cmdCleanup.Parameters.Add("?", OleDbType.VarWChar).Value = gruppenName;
                    cmdCleanup.Parameters.Add("?", OleDbType.VarWChar).Value = gruppenName;

                    int gelöscht = cmdCleanup.ExecuteNonQuery();

                    // Optional: Debugging-Hinweis
                    if (gelöscht > 0)
                    {
                        Console.WriteLine($"Gruppe '{gruppenName}' wurde auch aus dem Katalog entfernt.");
                    }
                }
            }
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

                Gesamtkosten(listBox_Erzeuger.Text);

                string dbPath = GetDBPath();
                string connString = $@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={dbPath};Persist Security Info=False;";

                // Transaktion außerhalb deklarieren, damit wir sie im catch-Block erreichen
                OleDbTransaction trans = null;

                try
                {
                    using (OleDbConnection conn = new OleDbConnection(connString))
                    {
                        conn.Open();
                        // Transaktion starten
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

                        // Wenn alles erfolgreich war: Bestätigen
                        trans.Commit();
                    }

 
                }
                catch (Exception ex)
                {
                    // Im Fehlerfall: Alles rückgängig machen
                    try
                    {
                        if (trans != null) trans.Rollback();
                    }
                    catch { /* Ignorieren, falls Rollback selbst fehlschlägt */ }

                }
            }
        }

        private Panel CreateColumnHeader(string gruppe)
        {
            Panel p = new Panel
            {
                Size = new Size(flpContainer.Width - 25, 20),
                BackColor = Color.LightGray,
                Margin = new Padding(0, 0, 0, 5),
                Tag = gruppe
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
            Gesamtkosten(listBox_Erzeuger.Text);
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
                                    Nutzungsdauer = reader["EingegebenerWert"] != DBNull.Value
                                             ? Convert.ToDecimal(reader["Nutzuingsdauer"])
                                             : 0,
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

                    // Eingabemaske öffnen
                    Form_KostenfaktorItem frm = new Form_KostenfaktorItem();
                    if (frm.ShowDialog() != DialogResult.OK) return;

                    // Werte aus dem Dialog abrufen
                    int stammID = frm.gewählteID;
                    double nutzungsdauer = Convert.ToDouble(frm.Nutzungsdauer);
                    double betrag = Convert.ToDouble(frm.Wert);
                    string einheit = frm.Einheit;
                    string gewaehlteGruppe = frm.Gruppe.Trim(); // Gruppe aus der ComboBox

                    if (string.IsNullOrEmpty(gewaehlteGruppe)) gewaehlteGruppe = "Allgemein";

                    // Gruppe in den Katalog aufnehmen, falls sie neu ist ("Lern-Funktion")
                    string sqlKatalog = @"INSERT INTO Tab_KostenGruppenKatalog (GruppenName) 
                                  SELECT ? FROM (SELECT COUNT(*) FROM Tab_KostenGruppenKatalog WHERE GruppenName = ?) AS CheckTbl 
                                  WHERE CheckTbl.[Expr1000] = 0";
                    // Hinweis: Das obige SQL ist ein "Insert if not exists" Trick für Access. 
                    // Alternativ einfach ein Try-Catch um ein normales INSERT machen.

                    try
                    {
                        using (OleDbCommand cmdKat = new OleDbCommand(sqlKatalog, conn))
                        {
                            cmdKat.Parameters.Add("?", OleDbType.VarWChar).Value = gewaehlteGruppe;
                            cmdKat.Parameters.Add("?", OleDbType.VarWChar).Value = gewaehlteGruppe;
                            cmdKat.ExecuteNonQuery();
                        }
                    }
                    catch { /* Ignorieren, wenn Gruppe schon existiert (Duplicate Key) */ }

                    // INSERT in Tab_ProjektWerte (inklusive der projekt-spezifischen Gruppe)
                    string sqlInsert = @"INSERT INTO Tab_ProjektWerte (ProjektID, StammID, EingegebenerWert, Nutzungsdauer, Einheit, Gruppe, KomponentenID, KategorieID) 
                                VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                    using (OleDbCommand cmdIns = new OleDbCommand(sqlInsert, conn))
                    {
                        cmdIns.Parameters.Add("?", OleDbType.Integer).Value = m_ID_Projekt;
                        cmdIns.Parameters.Add("?", OleDbType.Integer).Value = stammID;
                        cmdIns.Parameters.Add("?", OleDbType.Double).Value = betrag;
                        cmdIns.Parameters.Add("?", OleDbType.Double).Value = nutzungsdauer;
                        cmdIns.Parameters.Add("?", OleDbType.VarWChar).Value = einheit;
                        cmdIns.Parameters.Add("?", OleDbType.VarWChar).Value = gewaehlteGruppe;
                        cmdIns.Parameters.Add("?", OleDbType.Integer).Value = GetKomponentenID(listBox_Erzeuger.Text);
                        cmdIns.Parameters.Add("?", OleDbType.Integer).Value = tabMain.SelectedIndex+1;
                        cmdIns.ExecuteNonQuery();
                    }
                }

                // UI aktualisieren
                LoadKostenFaktoren(m_ID_Projekt, tabMain.SelectedTab.Text, listBox_Erzeuger.Text);
                Gesamtkosten();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Hinzufügen: " + ex.Message);
            }
        }

        private int GetKomponentenID(string Erzeuger)
        { 
            switch (Erzeuger)
            {
                case "Wärmepumpe": return 1;
                case "Heizkessel": return 2;
                case "Photovoltaik": return 3;
                case "Solarthermie": return 4;
                case "Stromspeicher": return 5;
                case "Pufferspeicher": return 6;
                case "BHKW": return 7;
                default: return 0; // Oder eine andere Standard-ID für "Unbekannt"
            }
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
                        cmd.Parameters.Add("@dauer", OleDbType.Double).Value = (double)pos.Nutzungsdauer;
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