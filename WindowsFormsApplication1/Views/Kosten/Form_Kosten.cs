using Humanizer;
using System;
using System.Collections.Generic;
using System.Data;
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

        private FlowLayoutPanel flp = null;
        private string kategorie = "";
        private int kategorieID = 0;

        // Variable für den Extender des aktuellen Formulars
        private HelpExtender _helpExtender;

        public Form_Kosten(int IDProjekt)
        {
            InitializeComponent(); // Lädt die Designer-Struktur

            // Den Extender erstellen und mit dem bereits geladenen globalen Katalog füttern
            _helpExtender = new HelpExtender(Program.HelpCatalog);

            m_ID_Projekt = IDProjekt;
            tabMain.SelectedIndex = 0;
            kategorieID = 1;
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

            // Double Buffered für ruckelfreiere UI
            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, flpContainer_Betriebskosten, new object[] { true });

            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, flpContainer, new object[] { true });

            typeof(FlowLayoutPanel).InvokeMember("DoubleBuffered",
                System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                null, flpContainer_Energiekosten, new object[] { true });

            FillCarrierComboBox();
            RenderEnergieTab();
        }

        private void Form_Kosten_Load(object sender, EventArgs e)
        {
            // Designer-Schutz (wichtig!)
            if (this.DesignMode) return;

            _helpExtender.RegisterForm(this);
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
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
            string sql = $"SELECT Komponente, Summe FROM Abfrage_KostenKomponenten WHERE ProjektID = ?";

            // Parameter vorbereiten
            OleDbParameter[] ps = {
                new OleDbParameter("@id", m_ID_Projekt),
            };

            // Repository nutzen, um die Daten zu holen
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Durch die Zeilen loopen (ersetzt den Reader)
            foreach (DataRow row in dt.Rows)
            {
                string komponente = row["Komponente"].ToString();
                decimal betrag = row["Summe"] != DBNull.Value ? Convert.ToDecimal(row["Summe"]) : 0;

                // Wenn es NICHT die aktuelle Komponente ist, zur Gesamtsumme addieren
           //     if (komponente != aktuelleSelektion)
                {
                     summeGesamt += betrag;
                }
            }

            // Jetzt die live berechnete Selektion zur Gesamtsumme addieren
            //summeGesamt += summeSelektion;

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
            foreach (Control c in flp.Controls)
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
                    DeleteGruppeAusDatenbank(gruppenName, m_ID_Projekt, kategorieID);

                    // UI: Nur die Zeilen entfernen, die keine MainComponent sind
                    flp.SuspendLayout();
                    //                    flpContainer.SuspendLayout();
                    //                    for (int i = flpContainer.Controls.Count - 1; i >= 0; i--)
                    for (int i = flp.Controls.Count - 1; i >= 0; i--)
                    {
                        Control c = flp.Controls[i];
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

                            flp.Controls.Remove(c);
                            c.Dispose();
                        }
                    }
                    flp.ResumeLayout();

                    Gesamtkosten(listBox_Erzeuger.Text);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Bereinigen der Gruppe: " + ex.Message);
                }
            }
        }

        private void DeleteGruppeAusDatenbank(string gruppenName, int projektID, int kategorieID)
        {
            try
            {
                // Löscht alle Faktoren dieser Gruppe aus dem aktuellen Projekt
                string sqlDeleteProjektWerte = "DELETE FROM Tab_ProjektWerte WHERE Gruppe = ? AND ProjektID = ? AND KategorieID=?";

                DataRepository.ExecuteSQL(sqlDeleteProjektWerte,
                    new OleDbParameter("@gName", gruppenName),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@pIDkat", kategorieID));

                // Cleanup Katalog: Lösche Gruppe nur, wenn sie nirgendwo mehr verwendet wird
                // Hinweis: Access braucht den Parameter hier 2x, weil 2 Fragezeichen im SQL sind
                string sqlCleanupKatalog = @"DELETE FROM Tab_KostenGruppenKatalog 
                                     WHERE GruppenName = ? 
                                     AND NOT EXISTS (SELECT 1 FROM Tab_ProjektWerte WHERE Gruppe = ?)";

                DataRepository.ExecuteSQL(sqlCleanupKatalog,
                    new OleDbParameter("@g1", gruppenName),
                    new OleDbParameter("@g2", gruppenName));

                // Optional: UI Logik zum Refresh danach aufrufen
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Löschen der Gruppe: " + ex.Message);
            }
        }

        private void Zeile_DeleteRequested(object sender, EventArgs e)
        {
            if (sender is ucKostenZeile zeile)
            {
                int stammID = zeile.Daten.StammID;
                int datensatzID = zeile.Daten.ID; // Falls du lieber über die Primär-ID löschst

                // UI-Aufräumarbeiten
                zeile.DeleteRequested -= Zeile_DeleteRequested;
                flp.Controls.Remove(zeile);
                zeile.Dispose();

                // Datenbank-Löschung
                string sql = "DELETE FROM Tab_ProjektWerte WHERE ID = ?";

                bool erfolg = DataRepository.ExecuteSQL(sql, new OleDbParameter("@id", datensatzID));

                if (erfolg)
                {
                    Gesamtkosten(listBox_Erzeuger.Text);
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

            string sql = @"
            SELECT ID, ProjektID, StammID, KategorieName, Komponente, Bezeichnung, 
                   Gruppe, EingegebenerWert, WorstCase, BestCase, Nutzungsdauer, 
                   WorstCase_Nutzungsdauer, BestCase_Nutzungsdauer, Einheit, IsMainComponent
            FROM Abfrage_Kostenfaktoren
            WHERE (KategorieName = ?) AND (Komponente = ?) AND (ProjektID = ?)";

            // Parameter vorbereiten
            OleDbParameter[] ps = {
                new OleDbParameter("@kat", kategorie),
                new OleDbParameter("@komp", komponente),
                new OleDbParameter("@pID", projektID)
            };

            // Repository nutzen, um die Daten zu holen
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            // Durch die Zeilen loopen (ersetzt den Reader)
            foreach (DataRow row in dt.Rows)
            {
                geladeneFaktoren.Add(new KostenPosition
                {
                    ID = Convert.ToInt32(row["ID"]),
                    Name = row["Bezeichnung"].ToString(),
                    Betrag = row["EingegebenerWert"] != DBNull.Value ? Convert.ToDecimal(row["EingegebenerWert"]) : 0,
                    Einheit = row["Einheit"].ToString(),
                    Nutzungsdauer = row["Nutzungsdauer"] != DBNull.Value ? Convert.ToDecimal(row["Nutzungsdauer"]) : 0,
                    IsMainComponent = Convert.ToBoolean(row["IsMainComponent"]),
                    Gruppenname = row["Gruppe"] != DBNull.Value ? row["Gruppe"].ToString() : "Allgemein",
                    StammID = Convert.ToInt32(row["StammID"]),
                    BestCase = row["BestCase"] != DBNull.Value ? Convert.ToDecimal(row["BestCase"]) : 0,
                    WorstCase = row["WorstCase"] != DBNull.Value ? Convert.ToDecimal(row["WorstCase"]) : 0,
                    BestCase_Nutzungsdauer = row["BestCase_Nutzungsdauer"] != DBNull.Value ? Convert.ToDecimal(row["BestCase_Nutzungsdauer"]) : 0,
                    WorstCase_Nutzungsdauer = row["WorstCase_Nutzungsdauer"] != DBNull.Value ? Convert.ToDecimal(row["WorstCase_Nutzungsdauer"]) : 0
                });
            }

            // UI aktualisieren
            UpdateDetailPanel(komponente, geladeneFaktoren);
        }

        private void EnsureMainComponentExists(int projektID, string komponente, decimal externeKosten)
        {
            try
            {
                // Stammdaten prüfen ---
                string sqlStamm = @"SELECT StammID FROM Abfrage_Kostenfaktoren 
                            WHERE Komponente = ? AND Bezeichnung = ? AND IsMainComponent = True";

                object resStamm = DataRepository.ExecuteScalar(sqlStamm,
                    new OleDbParameter("@k1", komponente),
                    new OleDbParameter("@k2", komponente));

                if (resStamm == null) return; // Nichts gefunden, Abbruch
                int stammID = Convert.ToInt32(resStamm);

                // Projektdaten prüfen ---
                string sqlCheckProjekt = "SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ProjektID = ? AND StammID = ?";

                int exists = Convert.ToInt32(DataRepository.ExecuteScalar(sqlCheckProjekt,
                    new OleDbParameter("@p1", projektID),
                    new OleDbParameter("@s1", stammID)));

                if (exists == 0)
                {
                    // --- Kosten ermitteln ---
                    decimal initialeKosten = externeKosten;
                    if (initialeKosten == 0)
                    {
                        initialeKosten = (decimal)GetModulKosten(projektID, komponente);
                    }

                    string sqlInsertWert = @"INSERT INTO Tab_ProjektWerte (ProjektID, StammID, KomponentenID, 
                                     KategorieID, EingegebenerWert, Nutzungsdauer, Einheit, Gruppe) 
                                     VALUES (?, ?, ?, ?, ?, 0, ?,?)";

                    DataRepository.ExecuteSQL(sqlInsertWert,
                        new OleDbParameter("@pID", projektID),
                        new OleDbParameter("@sID", stammID),
                        new OleDbParameter("@kID", GetKomponentenID(komponente)),
                        new OleDbParameter("@kat", tabMain.SelectedIndex + 1),
                        new OleDbParameter("@val", (double)initialeKosten), // Access mag Double oft lieber als Decimal
                        new OleDbParameter("@unit", "€"),
                        new OleDbParameter("gr", "Allgemein")
                    );

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
            // Eingabemaske öffnen (bleibt UI-Logik)
            Form_KostenfaktorItem frm = new Form_KostenfaktorItem();

            if (frm.ShowDialog() != DialogResult.OK) return;

            try
            {
                // 2. Werte aus dem Dialog abrufen
                int stammID = frm.gewählteID;
                double nutzungsdauer = Convert.ToDouble(frm.Nutzungsdauer);
                double betrag = Convert.ToDouble(frm.Wert);
                string einheit = frm.Einheit;
                string gewaehlteGruppe = string.IsNullOrWhiteSpace(frm.Gruppe) ? "Allgemein" : frm.Gruppe.Trim();

                // 3. Gruppe in den Katalog aufnehmen ("Lern-Funktion")
                // Wir nutzen den "Insert if not exists" Trick mit deiner neuen Methode
                string sqlKatalog = @"INSERT INTO Tab_KostenGruppenKatalog (GruppenName) 
                              SELECT ?
                              FROM (SELECT COUNT(*)
                              FROM Tab_KostenGruppenKatalog
                              WHERE GruppenName = ?) AS CheckTbl 
                              WHERE CheckTbl.[Expr1000] = 0";

                DataRepository.ExecuteSQL(sqlKatalog,
                    new OleDbParameter("@g1", gewaehlteGruppe),
                    new OleDbParameter("@g2", gewaehlteGruppe));

                // 4. INSERT in Tab_ProjektWerte
                string sqlInsert = @"INSERT INTO Tab_ProjektWerte
                                    (ProjektID, StammID, EingegebenerWert, Nutzungsdauer, Einheit, Gruppe, KomponentenID, KategorieID) 
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)";

                DataRepository.ExecuteSQL(sqlInsert,
                    new OleDbParameter("@pid", m_ID_Projekt),
                    new OleDbParameter("@sid", stammID),
                    new OleDbParameter("@val", betrag),
                    new OleDbParameter("@nd", nutzungsdauer),
                    new OleDbParameter("@ein", einheit),
                    new OleDbParameter("@grp", gewaehlteGruppe),
                    new OleDbParameter("@kid", GetKomponentenID(komponenete)),
                    new OleDbParameter("@kat", tabMain.SelectedIndex + 1)
                );

                // 5. UI aktualisieren
                LoadKostenFaktoren(m_ID_Projekt, komponenete);
                Gesamtkosten();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Verarbeiten der Daten: " + ex.Message);
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
            if (pos.ID <= 0) return;

            string sql = @"UPDATE Tab_ProjektWerte 
                   SET EingegebenerWert = ?, 
                       BestCase = ?, 
                       WorstCase = ?,
                       Nutzungsdauer = ?,
                       BestCase_Nutzungsdauer = ?, 
                       WorstCase_Nutzungsdauer = ?,
                       Gruppe = ?
                   WHERE ID = ?";

            // Aufruf der neuen zentralen Methode
            DataRepository.ExecuteSQL(sql,
                new OleDbParameter("@val", (double)pos.Betrag),
                new OleDbParameter("@best", (double)pos.BestCase),
                new OleDbParameter("@worst", (double)pos.WorstCase),
                new OleDbParameter("@nd", (double)pos.Nutzungsdauer),
                new OleDbParameter("@bestNd", (double)pos.BestCase_Nutzungsdauer),
                new OleDbParameter("@worstNd", (double)pos.WorstCase_Nutzungsdauer),
                new OleDbParameter("gn", (string)pos.Gruppenname),
                new OleDbParameter("@id", pos.ID)
            );
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
            flpContainer_Energiekosten.Visible = false;
            kategorie = tabMain.SelectedTab.Text;
            if (kategorie == "Investitionskosten")
            {
                flp = flpContainer;
                Gesamtkosten(listBox_Erzeuger.Text);
                kategorieID = 1;
            }
            else if (kategorie == "Betriebskosten")
            {
                flp = flpContainer_Betriebskosten;
                Gesamtkosten(listBox_Betriebskosten.Text);
                kategorieID = 2;
            }
            else if (kategorie == "Energiekosten")
            {
                flp = flpContainer_Energiekosten;
                flp.Visible = false;
                Gesamtkosten();
                kategorieID = 3;
            }

        }

        private void btnTest_KostenUebernahme_Click(string komponente)
        {
            // Wert aus dem Technik-Modul abrufen
            decimal technikKosten = (decimal)GetModulKosten(m_ID_Projekt, komponente);

            if (technikKosten == 0)
            {
                if (MessageBox.Show("Es wurden 0,00 € in der Technik gefunden. Trotzdem übernehmen?",
                    "Hinweis", MessageBoxButtons.YesNo) == DialogResult.No) return;
            }

            // Suche die "MainComponent" Zeile in der UI, um sie sofort zu aktualisieren
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

                // In die Datenbank schreiben
                UpdateSingleRowInDatabase(mainZeile.Daten);

                // Gesamtsummen im Formular neu berechnen
                Gesamtkosten(komponente);

                MessageBox.Show($"Der Wert für '{komponente}' wurde erfolgreich auf {technikKosten:N2} € aktualisiert.",
                    "Update erfolgreich", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private double GetModulKosten(int projektID, string komponente)
        {
            double Summe = 0;

            string sql = "SELECT Abfrage_ProjektKostenKomponenten.Gesamt,Abfrage_ProjektKostenKomponenten.ID_Projekt, Tab_Typ_Energieanlagen.Bezeichner " +
                         "FROM Abfrage_ProjektKostenKomponenten " +
                         "INNER JOIN Tab_Typ_Energieanlagen ON Abfrage_ProjektKostenKomponenten.ID_Type = Tab_Typ_Energieanlagen.ID " +
                         "WHERE Abfrage_ProjektKostenKomponenten.ID_Projekt=? and Tab_Typ_Energieanlagen.Bezeichner=?";

            OleDbParameter[] p = { new OleDbParameter("@id", (Int32)projektID), new OleDbParameter("@komp", (string)komponente) };

            object obj = DataRepository.ExecuteScalar(sql, p);
            Summe = (obj != null && obj != DBNull.Value) ? Convert.ToDouble(obj) : 0.0;
            return Summe;
        }

        private void RenderEnergieTab(string filterKategorie = "Alle Kategorien")
        {
            flpContainer_Energiekosten.Controls.Clear();
            flpContainer_Energiekosten.SuspendLayout();
        }

        private void listBox_Energieträger_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox_Energieträger.SelectedItem is EnergyCarrier selectedCarrier)
            {
                flpContainer_Energiekosten.Controls.Clear();
                flpContainer_Energiekosten.Visible = true;
                UserControl uc = null;

                switch (selectedCarrier.PricingModel)
                {
                    case "FUEL":
                    case "LIQUID_FUEL":
                    case "SOLID_FUEL":
                    case "ANIMAL_FAT":
                    case "HEAT":
                    case "GASEOUS_FUEL":
                    case "ELECTRICITY":
                        uc = new ucFuelSettings(m_ID_Projekt, selectedCarrier);
                        break;
                }

                if (uc != null)
                {
                    // WICHTIG: Gib der Instanz einen festen Namen, den wir in der txt-Datei ansprechen können!
                    uc.Name = "ucFuelSettings";

                    // Breite an den Container anpassen
                    uc.Width = flpContainer.ClientSize.Width - 10;
                    flpContainer_Energiekosten.Controls.Add(uc);

                    // JETZT ERST REGISTRIEREN, da das Control nun existiert und im Panel sitzt!
                    _helpExtender.RegisterControl(uc, "ucFuelSettings");
                }
            }
        }

        public static List<EnergyCarrier> GetAllCarriers(int ID_Projekt)
        {
            List<EnergyCarrier> carriers = new List<EnergyCarrier>();

            //string sql = "SELECT * FROM ENERGY_CARRIER WHERE is_active = true ORDER BY name ASC";

            string sql = @"SELECT
                            energy_project_settings.ID_Projekt,
                            ec.*, 
                            pm.has_hi, 
                            pm.has_hs, 
                            pm.has_powerprice
                        FROM
                            energy_project_settings
                            INNER JOIN (
                                energy_carrier AS ec
                                LEFT JOIN
                                pricing_model AS pm ON ec.pricing_model = pm.code
                            ) ON energy_project_settings.ID_Energieträger = ec.id
                        WHERE energy_project_settings.ID_Projekt=?";

            OleDbParameter[] ps = {
                new OleDbParameter("@p", ID_Projekt),
            };

            DataTable dt = DataRepository.GetDataTable(sql, ps);

            foreach (DataRow row in dt.Rows)
            {
                carriers.Add(new EnergyCarrier
                {
                    ID = Convert.ToInt32(row["id"]),
                    Code = row["code"].ToString(),
                    Name = row["name"].ToString(),
                    GroupCode = row["group_code"].ToString(),
                    PricingModel = row["pricing_model"].ToString(),
                    BillingUnit = row["billing_unit"].ToString(),
                    HiKwhPerUnit = row["hi_kwh_per_unit"] != DBNull.Value ? Convert.ToDouble(row["hi_kwh_per_unit"]) : 0,
                    HsKwhPerUnit = row["hs_kwh_per_unit"] != DBNull.Value ? Convert.ToDouble(row["hs_kwh_per_unit"]) : 0,
                    ID_Brennstoff = Convert.ToInt32(row["id_brennstoff"]),
                    price_base = row["price_base"] != DBNull.Value ? Convert.ToDouble(row["price_base"]) : 0,
                    price_work = row["price_work"] != DBNull.Value ? Convert.ToDouble(row["price_work"]) : 0,
                    CO2 = row["co2"] != DBNull.Value ? Convert.ToDouble(row["co2"]) : 0,
                    SO2 = row["so2"] != DBNull.Value ? Convert.ToDouble(row["so2"]) : 0,
                    NOx = row["nox"] != DBNull.Value ? Convert.ToDouble(row["nox"]) : 0,
                    HasHi = row["has_hi"] != DBNull.Value ? Convert.ToBoolean(row["has_hi"]) : false,
                    HasHs = row["has_hs"] != DBNull.Value ? Convert.ToBoolean(row["has_hs"]) : false,
                    HasPowerPrice = row["has_powerprice"] != DBNull.Value ? Convert.ToBoolean(row["has_powerprice"]) : false
                });
            }
            return carriers;
        }
        
        private void FillCarrierComboBox()
        {
            // Daten holen
            List<EnergyCarrier> allCarriers = GetAllCarriers(m_ID_Projekt);
            // ComboBox konfigurieren
            listBox_Energieträger.DataSource = allCarriers;
            // Darstellung
            listBox_Energieträger.DisplayMember = "Name";
            // Welcher Wert soll im Hintergrund identifizieren?
            listBox_Energieträger.ValueMember = "Id";
            listBox_Energieträger.SelectedIndex = -1; // Start ohne Auswahl 
        }

        private string CreateNewEnergyCarrier()
        {
            using (var dlg = new Form_Kosten_Auswahl())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    // Prüfen, ob der Code bereits existiert
                    string checkSql = "SELECT COUNT(*) FROM energy_carrier WHERE name = ?";
                    int count = (int)DataRepository.ExecuteScalar(checkSql, new OleDbParameter[] {
                        new OleDbParameter("@name", dlg.SelectedName)
                    });

                    if (count > 0)
                    {
                        MessageBox.Show($"Die Energieträgervariante '{dlg.SelectedName}' existiert bereits!");
                        return "";
                    }

                    Object result = DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Arbeitspreis", dlg.SelectedBrennstoffID);
                    double default_arbeitspreis = result != null ? Convert.ToDouble(result) : 0;
                    result = DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Grundpreis", dlg.SelectedBrennstoffID);
                    double default_grundpreis = result != null ? Convert.ToDouble(result) : 0;
                    result = DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Leistungspreis", dlg.SelectedBrennstoffID);
                    double default_leistungspreis = result != null ? Convert.ToDouble(result) : 0;

                    result = DataRepository.GetValueById("Tab_Brennstoff_Stamm", "CO2", dlg.SelectedBrennstoffID);
                    double default_co2 = result != null ? Convert.ToDouble(result) : 0;
                    result = DataRepository.GetValueById("Tab_Brennstoff_Stamm", "SO2", dlg.SelectedBrennstoffID);
                    double default_so2 = result != null ? Convert.ToDouble(result) : 0;
                    result = DataRepository.GetValueById("Tab_Brennstoff_Stamm", "NOx", dlg.SelectedBrennstoffID);
                    double default_nox = result != null ? Convert.ToDouble(result) : 0;


                    // In energy_carrier speichern
                    string insertSql = @"INSERT INTO energy_carrier 
                                 (ID_Brennstoff, code, name, group_code, pricing_model, billing_unit, hi_kwh_per_unit,
                                 hs_kwh_per_unit, price_work, price_base, co2, so2, nox, is_active) 
                                 VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                    OleDbParameter[] ps = {
                        new OleDbParameter("@idB", dlg.SelectedBrennstoffID),
                        new OleDbParameter("@code", dlg.SelectedCode),
                        new OleDbParameter("@name", dlg.SelectedName),
                        new OleDbParameter("@gc", dlg.SelectedGroupCode),
                        new OleDbParameter("@pm", dlg.SelectedBrennstoffCode),
                        new OleDbParameter("@unit", dlg.SelectedBillingUnit),
                        new OleDbParameter("@shi", dlg.SelectedHi),
                        new OleDbParameter("@shs", dlg.SelectedHs),
                        new OleDbParameter("defap", default_arbeitspreis),
                        new OleDbParameter("defgp", default_grundpreis),
                        new OleDbParameter("co2", default_co2),
                        new OleDbParameter("so2", default_so2),
                        new OleDbParameter("nox", default_nox),
                        new OleDbParameter("@active", OleDbType.Boolean) { Value = true}
                    };

                    try
                    {
                        int id = DataRepository.ExecuteInsertAndGetId(insertSql, ps);

                        // Historie nur bei Änderung ---
                        string sqlHistory = @"INSERT INTO energy_price 
                                (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit) 
                                VALUES (?, ?, ?, ?, ?, ?, ?)";

                        DataRepository.ExecuteSQL(sqlHistory, new OleDbParameter[] {
                            new OleDbParameter("@cid", id),
                            new OleDbParameter("@prid", m_ID_Projekt),
                            new OleDbParameter("@ap", Math.Round(default_arbeitspreis,4)),
                            new OleDbParameter("@hi", Math.Round(dlg.SelectedHi,4)),
                            new OleDbParameter("@gp", Math.Round(default_grundpreis,4)),
                            new OleDbParameter("@date", OleDbType.Date) { Value = DateTime.Now },
                            new OleDbParameter(@"au", dlg.SelectedBillingUnit)
                        });

                        string sqlInsert = @"INSERT INTO energy_Project_settings 
                                (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs, 
                                custom_price_base, ID_Umrechnung, co2, so2, nox) 
                                VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";

                        DataRepository.ExecuteSQL(sqlInsert, new OleDbParameter[] {
                        new OleDbParameter("@pid", m_ID_Projekt),
                        new OleDbParameter("@eid", id),
                        new OleDbParameter("@p",  Math.Round(default_arbeitspreis,4)),
                        new OleDbParameter("@pl", Math.Round(default_leistungspreis,4)),
                        new OleDbParameter("@h", Math.Round(dlg.SelectedHi,4)),
                        new OleDbParameter("@hs", Math.Round(dlg.SelectedHs,4)),
                        new OleDbParameter("@b", Math.Round(default_grundpreis,4)),
                        new OleDbParameter(@"cid",  dlg.SelectedConvID),
                        new OleDbParameter("@co2", default_co2),
                        new OleDbParameter("@so2", default_so2),
                        new OleDbParameter("@nox", default_nox)
                    });

                        MessageBox.Show("Energieträgervariante erfolgreich angelegt.");

                        return dlg.SelectedName;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Fehler beim Speichern: " + ex.Message);
                    }
                }
            }
            return "";
        }

        private void btn_Carrier_Click(object sender, EventArgs e)
        {
            string carrierName = CreateNewEnergyCarrier();
            FillCarrierComboBox();
            int index = listBox_Energieträger.FindStringExact(carrierName);

            if (index != ListBox.NoMatches)
            {
                listBox_Energieträger.SelectedIndex = index;
            }
        }

        private void btn_Delete_Click(object sender, EventArgs e)
        {
            if (listBox_Energieträger.SelectedItem is EnergyCarrier selectedCarrier)
            {
                DeleteEnergyCarrierWithSettings(selectedCarrier.Name, m_ID_Projekt);
            }
        }

        public bool DeleteEnergyCarrierWithSettings(string carrierName, int ID_Projekt)
        {
            // Erst die ID finden
            int id = DataRepository.GetIdByName("energy_carrier", "name", carrierName);
            if(id==0) return false;

            // 1. Details löschen (z.B. project_settings)
            var (conn, trans) = DataRepository.BeginTransaction();
            try
            {
                string sqlDetail = $"DELETE FROM energy_project_settings WHERE ID_Energieträger=? AND ID_Projekt=?";
                using (OleDbCommand cmd = new OleDbCommand(sqlDetail, conn, trans))
                {
                    cmd.Parameters.AddWithValue("?", id);
                    cmd.Parameters.AddWithValue("?", ID_Projekt);
                    cmd.ExecuteNonQuery();
                }

                sqlDetail = $"DELETE FROM energy_price WHERE carrier_id=? AND ID_Projekt=?";
                using (OleDbCommand cmd = new OleDbCommand(sqlDetail, conn, trans))
                {
                    cmd.Parameters.AddWithValue("?", id);
                    cmd.Parameters.AddWithValue("?", ID_Projekt);
                    cmd.ExecuteNonQuery();
                }

                trans.Commit();

                FillCarrierComboBox();

                return true;
            }
            catch (Exception ex)
            {
                trans.Rollback();
                MessageBox.Show($"Fehler beim Löschen in energy_project_settings: " + ex.Message);
                return false;
            }
            finally { conn.Close(); }
        }
 
    }

    public class EnergyCarrier
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string PricingModel { get; set; } // GAS, FUEL, GRID
        public string Code { get; set; }                                      // Das ist der Standard-Heizwert aus der Tabelle ENERGY_CARRIER
        public double HiKwhPerUnit { get; set; }
        public double HsKwhPerUnit { get; set; }
        public string GroupCode { get; set; }
        public string BillingUnit { get; set; }
        public int ID_Brennstoff { get; set; }
        public double price_work { get; set; }
        public double price_base { get; set; }
        public double price_power { get; set; }
        public double CO2 { get; set; }
        public double SO2 { get; set; }
        public double NOx { get; set; }
        public bool HasPowerPrice { get; set; }
        public bool HasHi { get; set; }
        public bool HasHs { get; set; }
    }

    public class EnergyConversion
    {
        public int IDBrennstoff { get; set; }
        public string FromUnit { get; set; }
        public string ToUnitCode { get; set; } // z.B. "kg", "L"
        public double Factor { get; set; }

        // Hilfseigenschaft für die ComboBox-Anzeige
        public string ToUnitLabel => $"{ToUnitCode} (Faktor: {Factor})";
    }

}