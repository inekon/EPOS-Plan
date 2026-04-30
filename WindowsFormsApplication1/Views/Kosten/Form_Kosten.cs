using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
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

        private FlowLayoutPanel flp = null;
        private string kategorie = "";

        // Globale Liste, damit wir nicht bei jedem Filtern die DB abfragen müssen
        private List<ProjektBrennstoff> m_AlleBrennstoffDaten;

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

            m_AlleBrennstoffDaten = GetBrennstoffDaten(m_ID_Projekt); // Einmal aus DB laden
            FillFilterCombo(m_AlleBrennstoffDaten);                   // Combo füllen
            RenderEnergieTab();
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
            try
            {
                // Löscht alle Faktoren dieser Gruppe aus dem aktuellen Projekt
                string sqlDeleteProjektWerte = "DELETE FROM Tab_ProjektWerte WHERE Gruppe = ? AND ProjektID = ?";

                DataRepository.ExecuteSQL(sqlDeleteProjektWerte,
                    new OleDbParameter("@gName", gruppenName),
                    new OleDbParameter("@pID", projektID));

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
                                     KategorieID, EingegebenerWert, Nutzungsdauer, Einheit) 
                                     VALUES (?, ?, ?, ?, ?, 0, ?)";

                    DataRepository.ExecuteSQL(sqlInsertWert,
                        new OleDbParameter("@pID", projektID),
                        new OleDbParameter("@sID", stammID),
                        new OleDbParameter("@kID", GetKomponentenID(komponente)),
                        new OleDbParameter("@kat", tabMain.SelectedIndex + 1),
                        new OleDbParameter("@val", (double)initialeKosten), // Access mag Double oft lieber als Decimal
                        new OleDbParameter("@unit", "€")
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
                       WorstCase_Nutzungsdauer = ?
                   WHERE ID = ?";

            // Aufruf der neuen zentralen Methode
            DataRepository.ExecuteSQL(sql,
                new OleDbParameter("@val", (double)pos.Betrag),
                new OleDbParameter("@best", (double)pos.BestCase),
                new OleDbParameter("@worst", (double)pos.WorstCase),
                new OleDbParameter("@nd", (double)pos.Nutzungsdauer),
                new OleDbParameter("@bestNd", (double)pos.BestCase_Nutzungsdauer),
                new OleDbParameter("@worstNd", (double)pos.WorstCase_Nutzungsdauer),
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
            }
            else if (kategorie == "Betriebskosten")
            {
                flp = flpContainer_Betriebskosten;
                Gesamtkosten(listBox_Betriebskosten.Text);
            }
            else if (kategorie == "Energiekosten") 
            {
                flp = flpContainer_Energiekosten;
                RenderEnergieTab();
                flp.Visible = true;
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

            string sql = "SELECT Abfrage_ProjektKostenKomponenten.ID_Projekt, Abfrage_ProjektKostenKomponenten.Gesamt,Tab_Typ_Energieanlagen.Bezeichner " +
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

            // Nur filtern, wenn nicht "Alle" gewählt ist
            var gefilterteDaten = (filterKategorie == "Alle Kategorien")
                ? m_AlleBrennstoffDaten
                : m_AlleBrennstoffDaten.Where(b => b.Kategorie == filterKategorie).ToList();

            string aktuelleKat = "";

            foreach (var b in gefilterteDaten)
            {
                // Kategorie-Balken (Navy Blue) wenn Kategorie wechselt
                if (b.Kategorie != aktuelleKat)
                {
                    var header = new ucKategorieHeader(b.Kategorie);
                    header.Width = flpContainer_Energiekosten.ClientSize.Width - 25;
                    header.Height = 16; 
                    header.Margin = new Padding(0, 5, 0, 0); // Optional: Kleiner Abstand nach oben

                    flpContainer_Energiekosten.Controls.Add(header);
                    aktuelleKat = b.Kategorie;

                    // --- NEU: Spaltenüberschriften hinzufügen ---
                    Panel spaltenHeader = CreateBrennstoffColumnHeader(aktuelleKat.Trim());
                    spaltenHeader.Width = flpContainer_Energiekosten.ClientSize.Width - 25;
                    spaltenHeader.Height = 25;
                    spaltenHeader.Margin = new Padding(0, 0, 0, 2); // Kleiner Abstand nach unten
                    flpContainer_Energiekosten.Controls.Add(spaltenHeader);
                }

                // Zeile hinzufügen
                var zeile = new ucBrennstoffZeile(b);
                zeile.Width = flpContainer_Energiekosten.ClientSize.Width - 25;
                zeile.Height = 20;
                zeile.Margin = new Padding(0);
         
                // Event zum Speichern binden
                zeile.ValueChanged += (s, e) =>
                {
                    SaveBrennstoffToDb(zeile.Daten);
                };

                flpContainer_Energiekosten.Controls.Add(zeile);
            }

            flpContainer_Energiekosten.Controls[flpContainer_Energiekosten.Controls.Count - 1].Margin = new Padding(0, 0, 0, 10); 

            flpContainer_Energiekosten.ResumeLayout();

            SyncHeaderPositions();
        }

        private void SaveBrennstoffToDb(ProjektBrennstoff b)
        {
            string finalSql = "";

            try
            {
                // Dezimalzahlen für SQL formatieren (Punkt statt Komma)
                string gp = b.ProjektGrundpreis.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                string ap = b.ProjektArbeitspreis.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
                string lp = b.ProjektLeistungspreis.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                // Prüfen, ob für dieses Projekt und diesen Brennstoff-Stamm bereits ein Eintrag existiert
                string sqlCheck = "SELECT COUNT(*) FROM Tab_Brennstoff_Projekt WHERE ID_Projekt = ? AND ID_Stamm = ?";

                int exists = Convert.ToInt32(DataRepository.ExecuteScalar(sqlCheck,
                    new OleDbParameter("@p1", m_ID_Projekt),
                    new OleDbParameter("@s1", b.StammID)));

                if (exists > 0)
                {
                    // UPDATE: Datensatz existiert bereits
                    finalSql = $@"UPDATE Tab_Brennstoff_Projekt SET 
                          Grundpreis = {gp}, 
                          Arbeitspreis = {ap}, 
                          Leistungspreis = {lp}, 
                          [Bezug] = '{b.Bezug}' 
                          WHERE ID_Projekt = {m_ID_Projekt} AND ID_Stamm = {b.StammID}";
                }
                else
                {
                    // INSERT: Neuer Datensatz für dieses Projekt anlegen
                    finalSql = $@"INSERT INTO Tab_Brennstoff_Projekt (ID_Projekt, ID_Stamm, Grundpreis, Arbeitspreis, Leistungspreis, [Bezug]) 
                          VALUES ({m_ID_Projekt}, {b.StammID}, {gp}, {ap}, {lp}, '{b.Bezug}')";
                }
                DataRepository.ExecuteSQL(finalSql);
            }
            catch (Exception ex)
            {
                // Optional: Logging oder Fehlermeldung
                Console.WriteLine("Fehler beim Speichern: " + ex.Message);
            }
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
                    S.PreisEinheit,
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

            try
            {
                OleDbParameter[] p = { new OleDbParameter("id", projektID) };
                DataTable dt = DataRepository.GetDataTable(sql, p);

                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    var b = new ProjektBrennstoff();

                    // Stammdaten
                    DataRow dr = dt.Rows[i];
                    b.StammID = Convert.ToInt32(dr["ID"]);
                    b.Name = dr["Name"].ToString();
                    b.Einheit = dr["Einheit"].ToString();
                    b.PreisEinheit = dr["PreisEinheit"].ToString();
                    b.Hi = Convert.ToDouble(dr["Hi"] ?? 0);
                    b.Hs = Convert.ToDouble(dr["Hs"] ?? 0);
                    b.Kategorie = dr["KatName"].ToString();
                    b.DefaultArbeitspreis = Convert.ToDouble(dr["Standard_Arbeitspreis"] ?? 0);

                    // Projektspezifische Daten (können NULL sein wegen LEFT JOIN)
                    object pArbeit = dr["Arbeitspreis"];
                    object pGrund = dr["Grundpreis"];
                    object pLeist = dr["Leistungspreis"];
                    object pBezug = dr["Bezug"];

                    b.ProjektArbeitspreis = pArbeit != DBNull.Value ? Convert.ToDouble(pArbeit) : 0;
                    b.ProjektGrundpreis = pGrund != DBNull.Value ? Convert.ToDouble(pGrund) : 0;
                    b.ProjektLeistungspreis = pLeist != DBNull.Value ? Convert.ToDouble(pLeist) : 0;
                    b.Bezug = pBezug != DBNull.Value ? pBezug.ToString() : "Hi"; // Default Hi

                    liste.Add(b);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Laden der Brennstoffdaten: " + ex.Message);
            }

            return liste;
        }

        private Panel CreateBrennstoffColumnHeader(string gruppe)
        {
            Panel p = new Panel
            {
                Size = new Size(flpContainer_Energiekosten.ClientSize.Width - 25, 20),
                BackColor = Color.LightGray,
                Margin = new Padding(0, 0, 0, 5),
                Tag = gruppe,
                Name = "pnlSpaltenHeader"
            };
            p.Controls.Clear();
            p.SuspendLayout();
    
            // Wir erstellen eine temporäre Instanz zum Auslesen der Positionen
            using (ucBrennstoffZeile muster = new ucBrennstoffZeile(new ProjektBrennstoff()))
            {
                muster.Width = flpContainer_Energiekosten.ClientSize.Width - 25;

                // Lokale Hilfsfunktion für die absolute Positionierung
                void AddLbl(string text, string ctrlName, int width, int x)
                {
                    Control target = muster.Controls[ctrlName];
                    if (target == null) return;

                    Label lbl = new Label();
                    lbl.Name = ctrlName;
                    lbl.Text = text;
                    lbl.AutoSize = false;
                    lbl.Width = width;
                    lbl.Height = 26;
                    lbl.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular);
                    lbl.TextAlign = ContentAlignment.MiddleLeft;
                    lbl.Padding = new Padding(0, 0, 0, 8);
                    lbl.Tag = lbl.Name; 

                    // Der Versatz: NumericUpDowns brauchen +2 bis +3 Pixel 
                    // damit der Text über der Zahl steht, nicht über dem Rahmen.
                    int korrektur = (target is NumericUpDown) ? 3 : 0;
                    lbl.Location = new Point(x + korrektur, 5);
                    p.Controls.Add(lbl);
                }

                // Jetzt mappen wir die Bezeichner auf die Namen vom ucBrennstoffZeile.Designer.cs
                AddLbl("Brennstoff", "lblName", 100,12);
                AddLbl("Einheit", "lblEinheit", 60,170);
                AddLbl("Hi [kWh/Einh.]", "lblHi", 100,256);
                AddLbl("Grundpr. [€]", "numGrundpreis", 80,369);
                AddLbl("Arbeitsp. [€]", "numArbeitspreis", 80,477);
                AddLbl("Leist.pr. [€]", "numLeistungpreis", 80,592);
            }

            p.ResumeLayout();

            return p;
        }


        private void FillFilterCombo(List<ProjektBrennstoff> daten)
        {
            cmbFilterKategorie.Items.Clear();
            cmbFilterKategorie.Items.Add("Alle Kategorien");

            // Holt alle eindeutigen Kategorienamen aus der Liste
            var kategorien = daten.Select(b => b.Kategorie).Distinct().OrderBy(k => k);

            foreach (var kat in kategorien)
            {
                cmbFilterKategorie.Items.Add(kat);
            }

            cmbFilterKategorie.SelectedIndex = 0; // "Alle" vorselektieren
            cmbFilterKategorie.SelectedIndex = -1;
            cmbFilterKategorie.SetPlaceholder("🔍 Filter wählen...");
        }

        private void cmbFilterKategorie_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbFilterKategorie.SelectedIndex == -1) return;
            string selected = cmbFilterKategorie.SelectedItem.ToString();
            RenderEnergieTab(selected);
        }

        private void SyncHeaderPositions()
        {
            // Finde die erste Datenzeile im FlowLayoutPanel
            var ersteZeile = flpContainer_Energiekosten.Controls.OfType<ucBrennstoffZeile>().FirstOrDefault();

            // Finde den Spalten-Header (das Panel, das wir vorher eingefügt haben)
            // Das Panel suchenn, das die Header-Labels enthält
            var headerPanel = flpContainer_Energiekosten.Controls.OfType<Panel>()
                              .FirstOrDefault(p => p.Name == "pnlSpaltenHeader");

            if (ersteZeile != null && headerPanel != null)
            {
                // Wir gehen alle Controls im Header durch und suchen das Gegenstück in der Zeile
                foreach (Control hLabel in headerPanel.Controls)
                {
                    if (hLabel is Label && hLabel.Tag != null)
                    {
                        string targetName = hLabel.Tag.ToString();
                        Control zielCtrl = ersteZeile.Controls[targetName];

                        if (zielCtrl != null)
                        {
                            // X-Position abgleichen
                            // Bei NumericUpDown korrigieren wir 3 Pixel für die Optik
                            int offset = (zielCtrl is NumericUpDown) ? 3 : 0;
                            hLabel.Left = zielCtrl.Left + offset;
                        }
                    }
                }
            }
        }


    }
}