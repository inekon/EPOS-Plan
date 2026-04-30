using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Navigation;

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

        private FlowLayoutPanel flp = null;
        private string kategorie = "";

        public Form_Kosten(int IDProjekt)
        {
            InitializeComponent(); // Lädt die Designer-Struktur

            m_ID_Projekt = IDProjekt;
            tabMain.SelectedIndex = 0;
            kategorie = tabMain.TabPages[0].Text;
            flp = flpContainer;

            // UI verfeinern
            this.BackColor = Surface;
            this.tabInvest.BackColor = Surface;

            // Einmal initial aufrufen, damit beim Start 0 oder die Startwerte da stehen
            Gesamtkosten();

            if ((Program.startfrm.status & 0x2) == 0x2) { listBox_Erzeuger.Items.Add("Wärmepumpe"); listBox_Betriebskosten.Items.Add("Wärmepumpe"); }
            if ((Program.startfrm.status & 0x1) == 0x1) { listBox_Erzeuger.Items.Add("Heizkessel"); listBox_Betriebskosten.Items.Add("Heizkessel"); }
            if ((Program.startfrm.status & 1024) == 1024) { listBox_Erzeuger.Items.Add("Photovoltaik"); listBox_Betriebskosten.Items.Add("Photovoltaik"); }
            if ((Program.startfrm.status & 512) == 512) { listBox_Erzeuger.Items.Add("Solarthermie"); listBox_Betriebskosten.Items.Add("Solarthermie"); }
            if ((Program.startfrm.status & 0x4) == 0x4) { listBox_Erzeuger.Items.Add("Stromspeicher"); listBox_Betriebskosten.Items.Add("Stromspeicher"); }
            if ((Program.startfrm.status & 2048) == 2048) { listBox_Erzeuger.Items.Add("Pufferspeicher"); listBox_Betriebskosten.Items.Add("Pufferspeicher"); }
            if ((Program.startfrm.status & 256) == 256) { listBox_Erzeuger.Items.Add("BHKW"); listBox_Betriebskosten.Items.Add("BHKW"); }

            // Im Konstruktor deines Hauptformulars:
            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, flp, new object[] { true });
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
            foreach (Control c in flp.Controls)
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
            string sql = $"SELECT Komponente, Summe FROM Abfrage_KostenKomponenten WHERE ProjektID = {m_ID_Projekt}";

            rs.Open(sql);

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
            if (aktuelleSelektion != "")
                label_ErzeugerGesamt.Text = $"{kategorie} ({aktuelleSelektion}): {summeSelektion:N2} €";
            else
                label_ErzeugerGesamt.Text = "-";

            label_Gesamt.Text = $"PROJEKT GESAMT: {summeGesamt:N2} €";

            label_ErzeugerGesamt.Refresh();
            label_Gesamt.Refresh();
        }

        // Beispiel: Wenn links eine Komponente (z.B. BHKW) gewählt wird
        private void UpdateDetailPanel(string komponente, List<KostenPosition> faktoren)
        {
            flp.Controls.Clear();
            flp.SuspendLayout();

            // Berechnung verfügbare Innenbreite
            // ClientSize.Width zieht die Scrollbar bereits automatisch ab.
            int targetWidth = flp.ClientSize.Width - flp.Padding.Left - flp.Padding.Right;

            // Falls ein kleiner Sicherheitsabstand zum rechten Rand sein soll (z.B. 5 Pixel):
            targetWidth -= 5;

            string aktuelleGruppe = "";

            foreach (var f in faktoren)
            {
                if (f.Gruppenname.Trim() != aktuelleGruppe.Trim())
                {
                    aktuelleGruppe = f.Gruppenname.Trim();

                    // Wir erstellen ein Panel als Container für den Header
                    Panel headerPanel = new Panel
                    {
                        Size = new Size(targetWidth, 30),
                        BackColor = Color.FromArgb(20, 40, 80),
                        Margin = new Padding(0, 10, 0, 0),
                        Tag = aktuelleGruppe.Trim() // Wichtig für die Lösch-Identifizierung
                    };

                    // Das Label für den Text
                    Label groupTitle = new Label
                    {
                        Text = aktuelleGruppe.ToUpper().Trim(),
                        Font = new Font(this.Font, FontStyle.Bold),
                        ForeColor = Color.White,
                        AutoSize = true, // Wichtig: Passt sich dem Text an
                        Location = new Point(5, 7), // Ein bisschen Padding von oben/links
                        TextAlign = ContentAlignment.MiddleLeft

                    };

                    Button btnTest = null;
                    // Der Button erscheint nur in der Hauptgruppe (z.B. "Wärmepumpe")
                    if (f.IsMainComponent)
                    {
                        btnTest = new Button
                        {
                            Text = "🔄 Planwert übernehmen...",
                            Height = 20,
                            Width = 160,
                            AutoSize = false,
                            FlatStyle = FlatStyle.Flat,
                            ForeColor = Color.White,
                            BackColor = Color.FromArgb(0, 120, 215), // Blau für "Aktion"
                            Cursor = Cursors.Hand,
                            Font = new Font("Segoe UI", 8, FontStyle.Bold),
                            // Positionierung rechts vom Text:
                            Location = new Point(groupTitle.PreferredWidth + 20, 5)
                        };
                        btnTest.FlatAppearance.BorderSize = 0;

                        // Den EventHandler anhängen (Logik siehe unten)
                        btnTest.Click += (s, e) => btnTest_KostenUebernahme_Click(komponente);
                    }

                    // Der Lösch-Button (-)
                    Button btnDeleteGroup = new Button
                    {
                        Text = "-",
                        Size = new Size(25, 25),
                        AutoSize = false,
                        //Anchor = AnchorStyles.Right, // Er bleibt rechts, behält aber seine Größe
                        FlatStyle = FlatStyle.Flat,
                        ForeColor = Color.White,
                        BackColor = Color.Firebrick, // Dezentes Rot
                        Cursor = Cursors.Hand,
                        Tag = aktuelleGruppe.Trim(), // Speichert den Gruppennamen für das Event
                        Font = new Font("Segoe UI", 8, FontStyle.Bold),
                        // Manuelle Positionierung:
                        //X = Panelbreite - Buttonbreite - kleiner Abstand (z.B. 2px)
                        // Y = (Panelhöhe 30 - Buttonhöhe 25) / 2 = 2 oder 3
                        Location = new Point(targetWidth - 27, 2)
                    };

                    btnDeleteGroup.FlatAppearance.BorderSize = 0;
                    btnDeleteGroup.Click += btnDeleteGroup_Click; // Event verknüpfen
                    btnDeleteGroup.MinimumSize = new Size(25, 25);
                    btnDeleteGroup.MaximumSize = new Size(25, 25);

                    // Controls zum Header-Panel hinzufügen
                    headerPanel.Controls.Add(groupTitle);
                    if (btnTest != null)
                    {
                        headerPanel.Controls.Add(btnTest);
                    }
                    headerPanel.Controls.Add(btnDeleteGroup);
                    Panel columnHeader = CreateColumnHeader(aktuelleGruppe.Trim());
                    // WICHTIG: Auch hier exakt targetWidth
                    columnHeader.Width = targetWidth;

                    flp.Controls.Add(headerPanel);
                    flp.Controls.Add(columnHeader);
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

                zeile.Tag = aktuelleGruppe.Trim();
                if (f.IsMainComponent)
                {
                    zeile.BackColor = Color.LightSteelBlue;
                    //zeile.Font = new Font(zeile.Font, FontStyle.Bold);
                    zeile.Margin = new Padding(0, 1, 0, 1);
                }
                zeile.DeleteRequested += Zeile_DeleteRequested;
                zeile.Daten.Komponente = komponente; //listBox_Erzeuger.Text; 
                zeile.Height = 25;

                flp.Controls.Add(zeile);
            }
            flp.ResumeLayout();
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
                flp.Controls.Remove(zeile);

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
            p.Controls.Add(new Label { Text = "Worst/Best", Location = new Point(420, 2), Width = 100, Font = new Font(this.Font, FontStyle.Regular) });

            return p;
        }

        private void listBox_Erzeuger_SelectedIndexChanged(object sender, EventArgs e)
        {
            flpContainer.Visible = true;
            btn_Hinzu.Enabled = true;

            string komponente = listBox_Erzeuger.Text;
            //string kategorie = tabMain.SelectedTab.Text;

            EnsureMainComponentExists(m_ID_Projekt, komponente, 0);
            LoadKostenFaktoren(m_ID_Projekt, komponente);
            Gesamtkosten(listBox_Erzeuger.Text);
        }

        public void LoadKostenFaktoren(int projektID, string komponente)
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
                        WorstCase,
                        BestCase,
                        Nutzungsdauer,
                        WorstCase_Nutzungsdauer,
                        BestCase_Nutzungsdauer,
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

                                    Nutzungsdauer = reader["Nutzungsdauer"] != DBNull.Value
                                             ? Convert.ToDecimal(reader["Nutzungsdauer"])
                                             : 0,

                                    IsMainComponent = Convert.ToBoolean(reader["IsMainComponent"]),

                                    // Hier wird die projekt-spezifische Gruppe geladen:
                                    Gruppenname = reader["Gruppe"] != DBNull.Value ? reader["Gruppe"].ToString() : "Allgemein",

                                    StammID = Convert.ToInt32(reader["StammID"]),

                                    // BestCase & WorstCase
                                    BestCase = reader["BestCase"] != DBNull.Value ? Convert.ToDecimal(reader["BestCase"]) : 0,
                                    WorstCase = reader["WorstCase"] != DBNull.Value ? Convert.ToDecimal(reader["WorstCase"]) : 0,
                                    BestCase_Nutzungsdauer = reader["BestCase_Nutzungsdauer"] != DBNull.Value ? Convert.ToDecimal(reader["BestCase_Nutzungsdauer"]) : 0,
                                    WorstCase_Nutzungsdauer = reader["WorstCase_Nutzungsdauer"] != DBNull.Value ? Convert.ToDecimal(reader["WorstCase_Nutzungsdauer"]) : 0
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

        private void EnsureMainComponentExists(int projektID, string komponente, decimal externeKosten)
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

                            // --- NEU: Kosten aus dem Technik-Modul holen, falls externeKosten 0 sind ---
                            decimal initialeKosten = externeKosten;
                            if (initialeKosten == 0)
                            {
                                initialeKosten = (decimal)GetModulKosten(projektID, komponente);
                            }

                            // Punkt statt Komma für SQL
                            string betragSql = initialeKosten.ToString(System.Globalization.CultureInfo.InvariantCulture);

                            string sqlInsertWert = @"INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, 
                                                    KategorieID, EingegebenerWert, Nutzungsdauer, Einheit) 
                                                    VALUES (?, ?, ?, ?, ?, 0, ?)";

                            using (OleDbCommand cmdInsert = new OleDbCommand(sqlInsertWert, conn))
                            {
                                // Die Reihenfolge der Parameter MUSS exakt wie im SQL oben sein!
                                cmdInsert.Parameters.AddWithValue("@p1", projektID);
                                cmdInsert.Parameters.AddWithValue("@p2", stammID);
                                cmdInsert.Parameters.AddWithValue("@p3", GetKomponentenID(komponente));
                                cmdInsert.Parameters.AddWithValue("@p4", tabMain.SelectedIndex + 1);
                                cmdInsert.Parameters.AddWithValue("@p5", betragSql); // Hier das Dezimal-Objekt übergeben, kein String!
                                cmdInsert.Parameters.AddWithValue("@p6", "€");    // Einheit als Text

                                cmdInsert.ExecuteNonQuery();
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
            AddKostenItem(listBox_Erzeuger.Text);
        }

        private void AddKostenItem(string komponenete)
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
                        cmdIns.Parameters.Add("?", OleDbType.Integer).Value = GetKomponentenID(komponenete);
                        cmdIns.Parameters.Add("?", OleDbType.Integer).Value = tabMain.SelectedIndex + 1;
                        cmdIns.ExecuteNonQuery();
                    }
                }

                // UI aktualisieren
                LoadKostenFaktoren(m_ID_Projekt, komponenete);
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
                           SET EingegebenerWert = ?, 
                               BestCase = ?, 
                               WorstCase = ?,
                               Nutzungsdauer = ?,
                               BestCase_Nutzungsdauer = ?, 
                               WorstCase_Nutzungsdauer = ?
                           WHERE ID = ?";

                    using (OleDbCommand cmd = new OleDbCommand(sql, conn))
                    {
                        // Parameter-Reihenfolge einhalten:
                        // EingegebenerWert
                        cmd.Parameters.Add("?", OleDbType.Double).Value = (double)pos.Betrag;
                        // BestCase (Wichtig: Hier war dein Fehler!)
                        cmd.Parameters.Add("?", OleDbType.Double).Value = (double)pos.BestCase;
                        // WorstCase
                        cmd.Parameters.Add("?", OleDbType.Double).Value = (double)pos.WorstCase;
                        // Nutzungsdauer
                        cmd.Parameters.Add("?", OleDbType.Double).Value = (double)pos.Nutzungsdauer;

                        cmd.Parameters.Add("?", OleDbType.Double).Value = (double)pos.BestCase_Nutzungsdauer;
                        cmd.Parameters.Add("?", OleDbType.Double).Value = (double)pos.WorstCase_Nutzungsdauer;

                        // WHERE ID (Der letzte Parameter im SQL muss auch als letztes hinzugefügt werden)
                        cmd.Parameters.Add("?", OleDbType.Integer).Value = pos.ID;
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Speichern: " + ex.Message);
            }
        }

        private void listBox_Betriebskosten_SelectedIndexChanged(object sender, EventArgs e)
        {
            flpContainer_Betriebskosten.Visible = true;
            btn_Hinzu_Betriebskosten.Enabled = true;

            string komponente = listBox_Betriebskosten.Text;

            EnsureMainComponentExists(m_ID_Projekt, komponente, 0);
            LoadKostenFaktoren(m_ID_Projekt, komponente);
            Gesamtkosten(listBox_Betriebskosten.Text);
        }

        private void btn_Hinzu_Betriebskosten_Click(object sender, EventArgs e)
        {
            AddKostenItem(listBox_Betriebskosten.Text);
        }

        private void tabMain_SelectedIndexChanged(object sender, EventArgs e)
        {
            kategorie = tabMain.SelectedTab.Text;
            if (kategorie == "Investitionskosten")
            {
                flp = flpContainer;
                Gesamtkosten(listBox_Erzeuger.Text);
            }
            else if (kategorie == "Betriebskosten")
            {
                flp = flpContainer_Betriebskosten;
                Gesamtkosten(listBox_Betriebskosten.Text);
            }
            else if (kategorie == "Energiekosten") // Falls dein Tab so heißt
            {
                flp = flpContainer_Energiekosten;
                RenderEnergieTab();
                flp.Visible = true;
            }
        }

        private void btnTest_KostenUebernahme_Click(string komponente)
        {
            // 1. Wert aus dem Technik-Modul abrufen
            decimal technikKosten = (decimal)GetModulKosten(m_ID_Projekt, komponente);

            if (technikKosten == 0)
            {
                if (MessageBox.Show("Es wurden 0,00 € in der Technik gefunden. Trotzdem übernehmen?",
                    "Hinweis", MessageBoxButtons.YesNo) == DialogResult.No) return;
            }

            // 2. Suche die "MainComponent" Zeile in der UI, um sie sofort zu aktualisieren
            ucKostenZeile mainZeile = null;
            foreach (Control c in flp.Controls)
            {
                if (c is ucKostenZeile zeile && zeile.Daten.IsMainComponent)
                {
                    mainZeile = zeile;
                    break;
                }
            }

            if (mainZeile != null)
            {
                // Wert im UserControl setzen (das löst dort intern das UI-Update aus)
                mainZeile.Daten.Betrag = technikKosten;

                mainZeile.SetBerechnetenWert(technikKosten);
                // Manuelles UI-Refresh des UserControls (falls nötig)
                //mainZeile.UpdateDisplay(); 

                // 3. In die Datenbank schreiben
                UpdateSingleRowInDatabase(mainZeile.Daten);

                // 4. Gesamtsummen im Formular neu berechnen
                Gesamtkosten(komponente);

                MessageBox.Show($"Der Wert für '{komponente}' wurde erfolgreich auf {technikKosten:N2} € aktualisiert.",
                    "Update erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private double GetModulKosten(int projektID, string komponente)
        {
            double Summe = 0;
            RecordSet rs = new RecordSet();
            string sql = "SELECT Abfrage_ProjektKostenKomponenten.ID_Projekt, Abfrage_ProjektKostenKomponenten.Gesamt,Tab_Typ_Energieanlagen.Bezeichner" +
                " FROM Abfrage_ProjektKostenKomponenten INNER JOIN Tab_Typ_Energieanlagen ON Abfrage_ProjektKostenKomponenten.ID_Type = Tab_Typ_Energieanlagen.ID" +
                " where Abfrage_ProjektKostenKomponenten.ID_Projekt=" + projektID + " and Tab_Typ_Energieanlagen.Bezeichner='" + komponente + "'";
            rs.Open(sql);
            if (rs.Next())
                Summe = (double)rs.Read("Gesamt");
            rs.Close();

            return Summe;
        }

        private void RenderEnergieTab()
        {
            flpContainer_Energiekosten.Controls.Clear();
            flpContainer_Energiekosten.SuspendLayout();

            var daten = GetBrennstoffDaten(m_ID_Projekt);
            string aktuelleKat = "";

            foreach (var b in daten)
            {
                // 1. Kategorie-Balken (Navy Blue) wenn Kategorie wechselt
                if (b.Kategorie != aktuelleKat)
                {
                    var header = new ucKategorieHeader(b.Kategorie);
                    header.Width = flpContainer_Energiekosten.ClientSize.Width - 25;
                    header.Height = 20; // <-- Setze hier eine feste, kleine Höhe (z.B. 30)
                    header.Margin = new Padding(0, 5, 0, 0); // Optional: Kleiner Abstand nach oben

                    flpContainer_Energiekosten.Controls.Add(header);
                    aktuelleKat = b.Kategorie;



                    // --- NEU: Spaltenüberschriften hinzufügen ---
                    Panel spaltenHeader = CreateBrennstoffColumnHeader(aktuelleKat.Trim());

                    //var spaltenHeader = new ucBrennstoffHeader();
                    spaltenHeader.Width = flpContainer_Energiekosten.ClientSize.Width - 25;
                    spaltenHeader.Height = 25;
                    spaltenHeader.Margin = new Padding(0, 0, 0, 2); // Kleiner Abstand nach unten
                    flpContainer_Energiekosten.Controls.Add(spaltenHeader);

                }

                // 2. Zeile hinzufügen
                var zeile = new ucBrennstoffZeile(b);
                zeile.Width = flpContainer_Energiekosten.ClientSize.Width - 25;
                zeile.Height = 25;
                zeile.Margin = new Padding(0);

                // Event zum Speichern binden
                zeile.ValueChanged += (s, e) =>
                {
                    SaveBrennstoffToDb(zeile.Daten);
                };

                flpContainer_Energiekosten.Controls.Add(zeile);
            }

            flpContainer_Energiekosten.ResumeLayout();
        }

        private void SaveBrennstoffToDb(ProjektBrennstoff b)
        {
            // Logik: 
            // IF EXISTS (SELECT 1 FROM Tab_Projekt_Brennstoffe WHERE ID_Projekt = ... AND ID_Stamm = ...)
            //    UPDATE ...
            // ELSE
            //    INSERT INTO Tab_Projekt_Brennstoffe ...
        }

        private List<ProjektBrennstoff> GetBrennstoffDaten(int projektID)
        {
            List<ProjektBrennstoff> liste = new List<ProjektBrennstoff>();

            // Dein SQL-Statement (Access-kompatibel mit Klammern für den JOIN)
            string sql = $@"
            SELECT 
                S.ID, 
                K.Gruppe AS KatName, 
                S.[Name], 
                S.Einheit, 
                S.Hi, 
                S.Hs, 
                S.Standard_Grundpreis, 
                S.Standard_Arbeitspreis,
                S.Standard_Leistungspreis,
               
                P.Grundpreis,
                P.Arbeitspreis, 
                P.Leistungspreis, 
                P.[Bezug]
            FROM (Tab_Brennstoff_Stamm AS S 
            INNER JOIN Tab_BrennstoffKategorien AS K ON S.ID_Kategorie = K.ID) 
            LEFT JOIN Tab_Brennstoff_Projekt AS P ON (S.ID = P.ID_Stamm AND P.ID_Projekt = {projektID})
            ORDER BY K.Gruppe, S.[Name];";

            RecordSet rs = new RecordSet();
            try
            {
                rs.Open(sql);
                while (rs.Next())
                {
                    var b = new ProjektBrennstoff();

                    // Stammdaten
                    b.StammID = Convert.ToInt32(rs.Read("ID"));
                    b.Name = rs.Read("Name").ToString();
                    b.Einheit = rs.Read("Einheit").ToString();
                    b.Hi = Convert.ToDouble(rs.Read("Hi") ?? 0);
                    b.Hs = Convert.ToDouble(rs.Read("Hs") ?? 0);
                    b.Kategorie = rs.Read("KatName").ToString();
                    b.DefaultArbeitspreis = Convert.ToDouble(rs.Read("Standard_Arbeitspreis") ?? 0);

                    // Projektspezifische Daten (können NULL sein wegen LEFT JOIN)
                    object pArbeit = rs.Read("Arbeitspreis");
                    object pGrund = rs.Read("Grundpreis");
                    //object pAktiv = rs.Read("Aktiv");
                    object pBezug = rs.Read("Bezug");

                    b.ProjektArbeitspreis = pArbeit != DBNull.Value ? Convert.ToDouble(pArbeit) : 0;
                    b.ProjektGrundpreis = pGrund != DBNull.Value ? Convert.ToDouble(pGrund) : 0;
                    //b.Aktiv = pAktiv != DBNull.Value ? Convert.ToBoolean(pAktiv) : false;
                    b.Bezug = pBezug != DBNull.Value ? pBezug.ToString() : "Hi"; // Default Hi

                    liste.Add(b);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden der Brennstoffdaten: " + ex.Message);
            }
            // RecordSet schließt sich normalerweise automatisch beim Dispose oder du hast eine .Close() Methode

            return liste;
        }

        private Panel CreateBrennstoffColumnHeader(string gruppe)
        {
            Panel p = new Panel
            {
                Size = new Size(flpContainer.Width - 25, 20),
                BackColor = Color.LightGray,
                Margin = new Padding(0, 0, 0, 5),
                Tag = gruppe
            };


            p.Controls.Clear();
            p.SuspendLayout();
            p.BackColor = Color.FromArgb(230, 230, 230); // Heller Grauton

            // Wir erstellen eine temporäre Instanz zum Auslesen der Positionen
            using (ucBrennstoffZeile muster = new ucBrennstoffZeile(new ProjektBrennstoff()))
            {
                // Lokale Hilfsfunktion für die absolute Positionierung
                void AddLbl(string text, string ctrlName, int width)
                {
                    Control target = muster.Controls[ctrlName];
                    if (target == null) return;

                    Label lbl = new Label();
                    lbl.Text = text;
                    lbl.AutoSize = false;
                    lbl.Width = width;
                    lbl.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
                    lbl.TextAlign = ContentAlignment.MiddleLeft;

                    // Der magische Versatz: NumericUpDowns brauchen +2 bis +3 Pixel 
                    // damit der Text über der Zahl steht, nicht über dem Rahmen.
                    int korrektur = (target is NumericUpDown) ? 3 : 0;

                    lbl.Location = new Point(target.Left + korrektur, 5);
                    p.Controls.Add(lbl);
                }

                // Jetzt mappen wir die Bezeichner auf die Namen in deinem ucBrennstoffZeile.Designer.cs
                AddLbl("Brennstoff", "lblName", 100);
                AddLbl("Einheit", "lblEinheit", 60);
                AddLbl("Hi", "lblHi", 50);
                AddLbl("Grundpr. [€]", "numGrundpreis", 80);
                AddLbl("Arbeitsp. [€]", "numArbeitspreis", 80);
                AddLbl("Leist.pr. [€]", "numLeistungpreis", 80);
            }

            p.ResumeLayout();

            return p;
        }

 

    }
}