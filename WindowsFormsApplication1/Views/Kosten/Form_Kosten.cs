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

        // Kostenkategorien wie in Tab_KostenKategorie. Die Reiter des Formulars stehen in
        // derselben Reihenfolge, deshalb gilt durchgehend KategorieID = tabMain.SelectedIndex + 1.
        internal const int KATEGORIE_INVESTITION = 1;
        internal const int KATEGORIE_BETRIEB = 2;
        internal const int KATEGORIE_ENERGIE = 3;

        public Dictionary<string, NumericUpDown> _Inputs = new Dictionary<string, NumericUpDown>();
        public int m_ID_Projekt = 0;

        private FlowLayoutPanel flp = null;
        private string kategorie = "";
        private int kategorieID = 0;

        /// <summary>
        /// Grund, warum die Betriebskosten einer Komponente NICHT vorbelegt werden konnten
        /// (bzw. Herleitung, wenn sie es wurden) — gefüllt beim ersten Anwählen, angezeigt
        /// als Hinweiszeile über der Gruppe. Schlüssel ist der Komponentenname.
        /// </summary>
        private readonly Dictionary<string, string> _betriebsHinweis =
            new Dictionary<string, string>(StringComparer.Ordinal);

        /// <summary>
        /// Sperrt den Aufbau des Energieträger-Blocks, solange
        /// <see cref="FillCarrierComboBox"/> die Liste an die Daten bindet.
        /// </summary>
        private bool _traegerlisteWirdGefuellt;

        // Variable für den Extender des aktuellen Formulars
        private HelpExtender _helpExtender;

        public Form_Kosten(int IDProjekt)
        {
            InitializeComponent(); // Lädt die Designer-Struktur

            // Den Extender erstellen und mit dem bereits geladenen globalen Katalog füttern
            _helpExtender = new HelpExtender(Program.HelpCatalog);

            m_ID_Projekt = IDProjekt;
            tabMain.SelectedIndex = 0;
            kategorieID = KATEGORIE_INVESTITION;
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
            BauePreisreihenEinstieg();
        }

        /// <summary>
        /// Einstieg in Spotpreisimport und Kostenprofil-Editor (AP4, Fachkonzept 4.1) —
        /// zwei Knöpfe unter der Energieträgerliste im Reiter „Energiekosten".
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum hier und nicht als eigener Menüpunkt.</b> Beides sind PREISDATEN des
        /// Strom-Energieträgers und gehören damit dorthin, wo der Strompreis ohnehin
        /// gepflegt wird: Arbeitspreis, Preishistorie und der Aufschlagsblock aus 4.2
        /// stehen alle im Reiter „Energiekosten" dieses Formulars. Ein eigener
        /// Navigationseintrag hätte die Preispflege auf zwei Orte verteilt, und der
        /// Anwender müsste wissen, dass „Spotpreise" und „Arbeitspreis" dasselbe Feld
        /// im Rechenweg füttern. Der Kostenbereich hat außerdem bereits seine
        /// Verwaltungsknöpfe an genau dieser Stelle (Hinzufügen/Löschen des Trägers).
        /// </para>
        /// <para>
        /// Programmatisch angehängt, damit <c>Form_Kosten.Designer.cs</c> unberührt
        /// bleibt (CLAUDE.md: Designer-Dateien nicht von Hand editieren).
        /// </para>
        /// </remarks>
        private void BauePreisreihenEinstieg()
        {
            try
            {
                Panel leiste = new Panel
                {
                    Location = new Point(17, 625),
                    Size = new Size(355, 66),
                    BackColor = Color.LightGray
                };

                Button btnSpot = new Button
                {
                    Text = MyResource.Resource.PREIS_BTN_SPOTIMPORT,
                    Location = new Point(6, 4),
                    Size = new Size(342, 28),
                    Font = new Font("Segoe UI", 9.75f)
                };
                btnSpot.Click += (s, e) =>
                {
                    using (Form_SpotpreisImport dlg = new Form_SpotpreisImport(m_ID_Projekt))
                        dlg.ShowDialog(this);
                };

                Button btnProfil = new Button
                {
                    Text = MyResource.Resource.PREIS_BTN_KOSTENPROFIL,
                    Location = new Point(6, 34),
                    Size = new Size(342, 28),
                    Font = new Font("Segoe UI", 9.75f)
                };
                btnProfil.Click += (s, e) => KostenprofilBearbeiten();

                leiste.Controls.Add(btnSpot);
                leiste.Controls.Add(btnProfil);
                tabEnergie.Controls.Add(leiste);
                leiste.BringToFront();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Der Preisreihen-Einstieg konnte nicht aufgebaut werden: " + ex.Message);
            }
        }

        /// <summary>
        /// Öffnet den Kostenprofil-Editor: das erste Profil des Projekts, oder ein neues,
        /// wenn noch keines existiert.
        /// </summary>
        /// <remarks>
        /// Bewusst keine eigene Auswahlmaske: Ein Projekt führt in aller Regel EIN
        /// Kostenprofil. Mehrere Profile bleiben über die Variantenauswahl auf der
        /// Speicher-Parameterseite erreichbar; eine dritte Liste hier wäre Beiwerk.
        /// </remarks>
        private void KostenprofilBearbeiten()
        {
            KostenprofilCtrl ctrl = new KostenprofilCtrl();
            var vorhandene = ctrl.ReadAllByProjekt(m_ID_Projekt);
            int id = vorhandene.Count > 0 ? vorhandene[0].ID : 0;

            using (Form_Kostenprofil dlg = new Form_Kostenprofil(m_ID_Projekt, id))
                dlg.ShowDialog(this);
        }

        private void Form_Kosten_Load(object sender, EventArgs e)
        {
            // Designer-Schutz (wichtig!)
            if (this.DesignMode) return;

            _helpExtender.RegisterForm(this);

            // Fenster an die aktuelle Bildschirmauflösung anpassen, damit auf
            // kleineren Bildschirmen nichts abgeschnitten wird (Scrollbars in den
            // Tabs übernehmen den Rest).
            FensterAnBildschirmAnpassen();
        }

        /// <summary>
        /// Klemmt die Fenstergröße auf den nutzbaren Bildschirmbereich (ohne
        /// Taskleiste) und zentriert das Fenster. Passt das Formular in seiner
        /// vollen Größe (1015 × 839 zzgl. Rahmen) nicht auf den Bildschirm, wird
        /// es verkleinert; die AutoScroll-Tabs (tabInvest/tabWartung/tabEnergie)
        /// zeigen dann bei Bedarf Scrollleisten. Kopf- (pnlHeader) und Fußzeile
        /// (pnlFooter) bleiben dank Dock=Top/Bottom fixiert.
        /// </summary>
        private void FensterAnBildschirmAnpassen()
        {
            Rectangle wa = Screen.FromControl(this).WorkingArea;

            int w = Math.Min(this.Width, wa.Width);
            int h = Math.Min(this.Height, wa.Height);
            this.Size = new Size(w, h);

            this.StartPosition = FormStartPosition.Manual;
            this.Location = new Point(
                wa.Left + Math.Max(0, (wa.Width - w) / 2),
                wa.Top + Math.Max(0, (wa.Height - h) / 2));
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        /// <summary>
        /// Befund B6 (11.08.2026): Energiepreise wurden nur über den Speichern-Button
        /// des ucFuelSettings-Controls persistiert — beim Schließen des Formulars
        /// gingen offene Eingaben verloren. Jetzt speichert das Schließen den
        /// aktuell geöffneten Energieträger mit (gleiche Logik wie der Button).
        /// </summary>
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            try
            {
                foreach (Control c in flpContainer_Energiekosten.Controls)
                {
                    ucFuelSettings uc = c as ucFuelSettings;
                    if (uc == null) continue;

                    // Nur speichern, wenn der Träger dem Projekt noch zugeordnet ist —
                    // sonst würde ein zuvor gelöschter Träger wieder angelegt.
                    int zugeordnet = Convert.ToInt32(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM energy_project_settings " +
                        "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                        new OleDbParameter("@p", m_ID_Projekt),
                        new OleDbParameter("@c", uc.CarrierId)));
                    if (zugeordnet > 0) uc.SaveProjectAndHistory();
                }
            }
            catch { /* Schließen nie am Speichern scheitern lassen */ }
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Summen je Komponente aus <c>Tab_ProjektWerte</c> — <b>getrennt nach Kategorie</b>
        /// (1 Investition, 2 Betrieb, 3 Energie). Spalten der Rückgabe: <c>Komponente</c>,
        /// <c>Summe</c>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Befund D1 (18.08.2026): Beide Aufrufer lasen zuvor die gespeicherte Abfrage
        /// <c>Abfrage_KostenKomponenten</c>. Die summiert <c>EingegebenerWert</c> nur über
        /// ProjektID und Komponente und filtert <b>nicht</b> nach <c>KategorieID</c> —
        /// Investitions-, Betriebs- und Energiepositionen derselben Komponente landeten in
        /// einer Zahl. Nachweis Projekt 1024: Wärmepumpe 6.100 € = 6.001 € (Investition) +
        /// 99 € (Betrieb), während die Investitions-Kachel der Kostenseite korrekt
        /// 12.001,00 € zeigte und die Tabelle darunter 12.100,00 €.
        /// </para>
        /// <para>
        /// Bewusst als eigenes parametrisiertes SQL statt einer Korrektur der gespeicherten
        /// Abfrage: Die Datenbank liegt außerhalb des Repos, eine Abfrageänderung erreicht
        /// Bestandsinstallationen nur über einen Migrationsschritt.
        /// </para>
        /// <para>
        /// <c>internal</c>, damit die Kompaktanzeige der Seite „Kosten"
        /// (<see cref="UcBkKosten"/>) dieselbe Leselogik verwendet und keine zweite entsteht —
        /// gleiche Begründung wie bei <see cref="WirtschaftlichkeitCtrl.LiesInvestitionen"/>.
        /// </para>
        /// </remarks>
        internal static DataTable LiesKomponentenSummen(int projektID, int kategorieID)
        {
            string sql = @"SELECT k.Komponente, Sum(w.EingegebenerWert) AS Summe
                           FROM Tab_KostenKomponente AS k
                                INNER JOIN Tab_ProjektWerte AS w ON k.ID = w.KomponentenID
                           WHERE w.ProjektID = ? AND w.KategorieID = ?
                           GROUP BY k.Komponente";

            return DataRepository.GetDataTable(sql,
                new OleDbParameter("@pid", projektID),
                new OleDbParameter("@kat", kategorieID));
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

            // Die Gesamtsumme der GERADE ANGEZEIGTEN Kategorie aus der Datenbank.
            DataTable dt = LiesKomponentenSummen(m_ID_Projekt, kategorieID);

            // Durch die Zeilen loopen (ersetzt den Reader)
            foreach (DataRow row in dt.Rows)
            {
                decimal betrag = row["Summe"] != DBNull.Value ? Convert.ToDecimal(row["Summe"]) : 0;
                summeGesamt += betrag;
            }

            // Anzeige aktualisieren
            if (aktuelleSelektion != "")
                label_ErzeugerGesamt.Text = $"{kategorie} ({aktuelleSelektion}): {summeSelektion:N2} €";
            else
                label_ErzeugerGesamt.Text = "-";

            // Die Kategorie steht mit im Text: Investitions-, Betriebs- und Energiekosten
            // haben verschiedene Bezugsgrößen (€ gegenüber €/a) und dürfen nicht als eine
            // Zahl gelesen werden.
            label_Gesamt.Text = string.Format(MyResource.Resource.KOSTEN_LBL_PROJEKT_GESAMT,
                                              kategorie, summeGesamt.ToString("N2"));

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

            // Hinweiszeile über der Liste: Abweichung zum Technik-Planwert (Investition)
            // bzw. Grund/Herleitung der Betriebskosten-Vorbelegung. Beides ist eine
            // Mitteilung, kein Eingabefeld — deshalb steht sie vor der ersten Gruppe.
            HinweiszeileAnlegen(komponente, targetWidth);

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
                            Text = MyResource.Resource.KOSTEN_BTN_PLANWERT,
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

        /// <summary>
        /// Legt die Hauptposition einer Komponente an, sofern sie im Projekt <b>und in der
        /// gerade geöffneten Kategorie</b> noch fehlt.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Befund D3 (18.08.2026): Die Existenzprüfung lief über
        /// <c>ProjektID</c> + <c>StammID</c> <b>ohne</c> <c>KategorieID</c>. Sobald die
        /// Investitions-Hauptposition einer Komponente existierte, galt sie auch für den
        /// Reiter „Betriebskosten" als vorhanden — eine Betriebskosten-Hauptposition konnte
        /// deshalb nie entstehen.
        /// </para>
        /// <para>
        /// Befund D4 (18.08.2026): Die <c>StammID</c> kam aus <c>Abfrage_Kostenfaktoren</c>.
        /// Diese gespeicherte Abfrage ist ein INNER JOIN <b>über <c>Tab_ProjektWerte</c></b>
        /// und liefert ohne bereits erfasste Projektwerte nichts — in einer frisch
        /// ausgelieferten Datenbank unterblieb die automatische Übernahme des Technik-Planwerts
        /// deshalb vollständig. Jetzt kommt die <c>StammID</c> aus der projektfreien
        /// Katalogtabelle <c>Tab_Kostenfaktor</c>.
        /// </para>
        /// <para>
        /// Die Existenzprüfung fragt bewusst über <c>Tab_Kostenfaktor.Bezeichnung</c> statt
        /// über eine feste <c>StammID</c>: Der Katalog führt für „Solarthermie" zwei
        /// Hauptpositions-Zeilen (StammID 82 und 84), und Bestandsprojekte verwenden beide.
        /// Ein Vergleich gegen nur eine der beiden würde für die andere Hälfte der Projekte
        /// eine zweite Hauptposition anlegen.
        /// </para>
        /// <para>
        /// <b>Vorbelegung je Kategorie.</b> Investitionskosten kommen aus
        /// <see cref="GetModulKosten"/> (eindeutige Technikwerte; mehrdeutige Anlagen
        /// tragen 0 bei und werden über die Abweichungsanzeige gemeldet). Betriebskosten
        /// entstehen seit dem 18.08.2026 aus den Wartungsangaben mal der tatsächlich
        /// gerechneten Jahresmenge — <see cref="TechnikPlanwertCtrl.LiesBetriebsplanwert"/>;
        /// liegt kein Simulationsergebnis vor, bleibt die Position bei 0 und der Grund steht
        /// als Hinweiszeile über der Gruppe (Nutzerentscheidung 3). Energiekosten haben ihre
        /// eigene Maske und werden hier nicht vorbelegt.
        /// </para>
        /// <para>
        /// <b>Nebenkosten entstehen als eigene Zeilen</b> (Nutzerentscheidung 2), nicht als
        /// Aufschlag auf die Hauptposition — siehe
        /// <see cref="KostenPositionCtrl.SchreibeNebenkosten"/>. Sie werden bei jedem
        /// Anwählen nur ANGELEGT, wenn sie fehlen; vorhandene Zeilen bleiben unberührt,
        /// damit ein zweites Öffnen weder Dubletten erzeugt noch Anwenderwerte überschreibt.
        /// </para>
        /// </remarks>
        private void EnsureMainComponentExists(int projektID, string komponente, decimal externeKosten)
        {
            try
            {
                int kategorieIDNeu = tabMain.SelectedIndex + 1;
                int komponentenID = GetKomponentenID(komponente);
                if (komponentenID <= 0) return;

                // --- Nebenkosten: fehlende Zeilen anlegen, vorhandene NICHT anfassen -----
                if (kategorieIDNeu == KATEGORIE_INVESTITION)
                    NebenkostenAnlegen(projektID, komponente, komponentenID);

                // Hauptposition dieser Komponente in DIESER Kategorie bereits vorhanden?
                int vorhanden = KostenPositionCtrl.FindeHauptposition(projektID, kategorieIDNeu,
                                                                      komponentenID, komponente);

                decimal initialeKosten = 0;

                if (kategorieIDNeu == KATEGORIE_BETRIEB)
                {
                    // komponentenID wird für die Kessel-Einheit „%/a" gebraucht: ihre
                    // Bezugsgröße ist die erfasste Investitionsposition dieser Komponente.
                    TechnikPlanwertCtrl.Betriebsplanwert bp =
                        TechnikPlanwertCtrl.LiesBetriebsplanwert(projektID, komponente, komponentenID);
                    _betriebsHinweis[komponente ?? ""] = bp.Hinweis ?? "";

                    if (vorhanden > 0)
                    {
                        // BETRAG 0 GILT ALS UNGEPFLEGT — dieselbe Hausregel, mit der seit
                        // dem 18.08.2026 auch ein Arbeitspreis 0 behandelt wird. Ohne sie
                        // liefe die Vorbelegung an allen Bestandsprojekten vorbei: deren
                        // Betriebskosten-Hauptposition existiert längst und steht auf 0,
                        // weil sie vor dem ersten Simulationslauf angelegt wurde. Ein
                        // gepflegter Wert wird NIE angefasst (Nutzerentscheidung 4).
                        if (bp.Betrag.HasValue &&
                            Math.Abs(KostenPositionCtrl.LiesBetrag(vorhanden)) < 0.005)
                            KostenPositionCtrl.SetzeBetragNachId(vorhanden, bp.Betrag.Value);
                        return;
                    }

                    if (bp.Betrag.HasValue) initialeKosten = (decimal)bp.Betrag.Value;
                }
                else
                {
                    if (vorhanden > 0) return;

                    if (kategorieIDNeu == KATEGORIE_INVESTITION)
                    {
                        initialeKosten = externeKosten;
                        if (initialeKosten == 0)
                            initialeKosten = (decimal)GetModulKosten(projektID, komponente);
                    }
                }

                // Stammdaten prüfen — projektfreie Quelle (D4).
                int stammID = KostenPositionCtrl.StammIdHaupt(komponente);
                if (stammID <= 0) return;                       // Nichts gefunden, Abbruch

                KostenPositionCtrl.SetzeBetrag(projektID, kategorieIDNeu, komponentenID, stammID,
                                               (double)initialeKosten,
                                               DbWerte.KOSTEN_GRUPPE_ALLGEMEIN, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler beim Initialisieren der Hauptkomponente: " + ex.Message);
            }
        }

        /// <summary>
        /// Legt für jeden Nebenkostenposten der Technik mit Wert &gt; 0 eine eigene
        /// Investitionszeile an, sofern sie noch fehlt (Nutzerentscheidung 2).
        /// </summary>
        private void NebenkostenAnlegen(int projektID, string komponente, int komponentenID)
        {
            var posten = TechnikPlanwertCtrl.Nebensummen(
                TechnikPlanwertCtrl.LiesAnlagen(projektID, komponente));
            if (posten.Count == 0) return;

            KostenPositionCtrl.SchreibeNebenkosten(projektID, KATEGORIE_INVESTITION, komponentenID,
                                                   posten, DbWerte.KOSTEN_GRUPPE_ALLGEMEIN,
                                                   KostenPositionCtrl.Nebenmodus.NurAnlegen);
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

            // Reihenfolge beachten: kategorieID muss VOR Gesamtkosten() stehen — die
            // Gesamtsumme wird seit Befund D1 nach Kategorie gefiltert und hätte sonst
            // noch die des zuvor gewählten Reiters verwendet.
            if (kategorie == "Investitionskosten")
            {
                kategorieID = KATEGORIE_INVESTITION;
                flp = flpContainer;
                Gesamtkosten(listBox_Erzeuger.Text);
            }
            else if (kategorie == "Betriebskosten")
            {
                kategorieID = KATEGORIE_BETRIEB;
                flp = flpContainer_Betriebskosten;
                Gesamtkosten(listBox_Betriebskosten.Text);
            }
            else if (kategorie == "Energiekosten")
            {
                kategorieID = KATEGORIE_ENERGIE;
                flp = flpContainer_Energiekosten;
                flp.Visible = false;
                Gesamtkosten();
            }

        }

        /// <summary>
        /// Knopf „Planwert übernehmen…": stellt die Technik-Planwerte <b>je Anlage</b> zur
        /// Wahl (<see cref="Form_PlanwertUebernahme"/>) und schreibt danach Hauptposition
        /// und Nebenkostenzeilen.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Bis 18.08.2026 nahm der Knopf ohne Rückfrage den Inhalt genau eines Feldes je
        /// Gewerk. Das war beim BHKW die kleinere von zwei gepflegten Zahlen
        /// (<c>Kosten_Modul</c> statt <c>Investition_kwel × Pel</c>) und ignorierte die vier
        /// Nebenkostenfelder vollständig. Jetzt entscheidet der Anwender, und die
        /// Nebenkosten entstehen als eigene, einzeln änderbare Zeilen
        /// (Nutzerentscheidungen 1 und 2).
        /// </para>
        /// <para>
        /// Übernommen wird nur auf ausdrückliche Bestätigung — <b>nie automatisch</b>
        /// (Nutzerentscheidung 4). Bricht der Anwender ab, bleibt jeder erfasste Wert stehen.
        /// </para>
        /// </remarks>
        private void btnTest_KostenUebernahme_Click(string komponente)
        {
            // Nur die Investitionskosten haben einen Technik-Planwert; auf den anderen
            // Reitern wäre der Knopf sinnlos (Betriebskosten sind €/a, Energiekosten
            // haben ihre eigene Maske).
            if (kategorieID != KATEGORIE_INVESTITION) return;

            var anlagen = TechnikPlanwertCtrl.LiesAnlagen(m_ID_Projekt, komponente);
            if (anlagen.Count == 0)
            {
                MessageBox.Show(string.Format(MyResource.Resource.KOSTEN_PLANWERT_LEER, komponente),
                                this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            double summe;
            List<TechnikPlanwertCtrl.Nebenposten> nebenkosten;
            using (var dlg = new Form_PlanwertUebernahme(komponente, anlagen))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                summe = dlg.Hauptsumme;
                nebenkosten = dlg.Nebenkosten;
            }

            int komponentenID = GetKomponentenID(komponente);
            int hauptID = KostenPositionCtrl.FindeHauptposition(m_ID_Projekt, KATEGORIE_INVESTITION,
                                                                komponentenID, komponente);
            if (hauptID > 0) KostenPositionCtrl.SetzeBetragNachId(hauptID, summe);

            int nZeilen = KostenPositionCtrl.SchreibeNebenkosten(
                m_ID_Projekt, KATEGORIE_INVESTITION, komponentenID, nebenkosten,
                DbWerte.KOSTEN_GRUPPE_ALLGEMEIN, KostenPositionCtrl.Nebenmodus.Abgleichen);

            // Neu einlesen statt die Zeilen von Hand nachzuziehen: die Nebenkosten können
            // gerade erst entstanden sein, und die Abweichungsanzeige muss ohnehin neu.
            LoadKostenFaktoren(m_ID_Projekt, komponente);
            Gesamtkosten(komponente);

            MessageBox.Show(string.Format(MyResource.Resource.KOSTEN_PLANWERT_UEBERNOMMEN,
                                          komponente, summe.ToString("N2", BerichtTexte.Kultur), nZeilen),
                            this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Hinweiszeile über der Positionsliste: Abweichung zum Technik-Planwert
        /// (Investitionskosten) bzw. Herleitung oder Grund der ausgebliebenen Vorbelegung
        /// (Betriebskosten). Ohne Mitteilung entsteht keine Zeile.
        /// </summary>
        private void HinweiszeileAnlegen(string komponente, int breite)
        {
            string text = "";
            Color farbe = Color.FromArgb(0x33, 0x33, 0x33);
            Color flaeche = Color.FromArgb(0xF4, 0xF6, 0xFA);

            if (kategorieID == KATEGORIE_INVESTITION)
            {
                KostenPositionCtrl.Abweichung ab = KostenPositionCtrl.Pruefe(
                    m_ID_Projekt, komponente, KATEGORIE_INVESTITION, GetKomponentenID(komponente));
                if (ab.Abweichend)
                {
                    text = ab.Text;
                    farbe = Color.FromArgb(0x8A, 0x4B, 0x00);
                    flaeche = Color.FromArgb(0xFF, 0xF4, 0xD9);
                }
            }
            else if (kategorieID == KATEGORIE_BETRIEB)
            {
                string h;
                if (!_betriebsHinweis.TryGetValue(komponente ?? "", out h))
                {
                    // Beim erneuten Öffnen ist die Position längst vorhanden; der Grund
                    // wird deshalb hier frisch ermittelt statt gemerkt.
                    h = TechnikPlanwertCtrl.LiesBetriebsplanwert(
                            m_ID_Projekt, komponente, GetKomponentenID(komponente)).Hinweis;
                    _betriebsHinweis[komponente ?? ""] = h ?? "";
                }
                text = h ?? "";
            }

            if (string.IsNullOrEmpty(text)) return;

            Label lbl = new Label
            {
                Text = text,
                AutoSize = false,
                Size = new Size(Math.Max(200, breite), 34),
                Margin = new Padding(0, 6, 0, 0),
                Padding = new Padding(6, 4, 6, 0),
                BackColor = flaeche,
                ForeColor = farbe
            };
            flp.Controls.Add(lbl);
        }

        /// <summary>
        /// Investitionswert der im Projekt verbauten Technik einer Komponente, soweit er
        /// <b>eindeutig</b> ist — Vorbelegung der Hauptposition beim ersten Anwählen.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Die Ermittlung selbst steht seit dem 18.08.2026 in
        /// <see cref="TechnikPlanwertCtrl"/>; von dort kommt auch der Entdoppelungsschutz
        /// des Befundes D2 (mehrere Anlagenzeilen auf dasselbe Gerät).
        /// </para>
        /// <para>
        /// <b>Mehrdeutige Anlagen tragen hier 0 bei.</b> Beim BHKW konkurrieren
        /// <c>Kosten_Modul</c> und <c>Investition_kwel × Pel</c> — für dieselbe Anlage
        /// zwei gültige, weit auseinanderliegende Zahlen (Beispielmodul „2G 250kw.el Gas":
        /// 16.666 € gegen 163.400 €). Welche gilt, entscheidet der Anwender im Dialog
        /// <see cref="Form_PlanwertUebernahme"/>; still eine davon einzutragen wäre geraten.
        /// Die Abweichungsanzeige weist genau darauf hin, solange nichts gewählt ist.
        /// </para>
        /// </remarks>
        private double GetModulKosten(int projektID, string komponente)
        {
            return TechnikPlanwertCtrl.Hauptsumme(
                TechnikPlanwertCtrl.LiesAnlagen(projektID, komponente), null);
        }

        private void RenderEnergieTab(string filterKategorie = "Alle Kategorien")
        {
            flpContainer_Energiekosten.Controls.Clear();
            flpContainer_Energiekosten.SuspendLayout();
        }

        private void listBox_Energieträger_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Während des Befüllens ist jede Auswahl eine Nebenwirkung der Bindung und
            // keine Entscheidung des Anwenders — Begründung bei FillCarrierComboBox().
            if (_traegerlisteWirdGefuellt) return;

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

        /// <summary>
        /// Füllt die Energieträgerliste des Projekts — <b>ohne</b> Auswahl und damit
        /// ohne Energieträger-Block im Panel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum die Sperre.</b> Die Bindung meldet unterwegs Auswahlen, die keine
        /// sind: <c>DataSource=</c> setzt die ListBox auf Zeile 0 (ein
        /// <c>SelectedIndexChanged</c>), <c>DisplayMember=</c> baut die Anzeige neu und
        /// meldet dabei zweimal erneut Zeile 0, erst <c>SelectedIndex = -1</c> nimmt die
        /// Auswahl zurück. Der Behandler baute daraus <b>dreimal</b> ein
        /// <c>ucFuelSettings</c> samt <c>ucStromAufschlaege</c> — jedes mit eigenen
        /// Lesezugriffen auf die Datenbank — von denen keines übrig bleiben sollte.
        /// Nachgewiesen am 18.08.2026 für die Projekte 1017 und 1023: drei Aufrufe von
        /// <c>StromAufschlagCtrl.StelleSpaltenSicher</c> je <c>new Form_Kosten(id)</c>.
        /// Seit Commit 87483b4 (Fehlerdialoge beseitigt) fiel das nicht mehr auf, die
        /// dreifache Arbeit blieb.
        /// </para>
        /// <para>
        /// Gleiches Mittel wie in <c>ucStromAufschlaege</c> (<c>_laden</c>): eine Sperre,
        /// die nur das programmatische Befüllen stummschaltet. Die echte Anwenderauswahl
        /// läuft unverändert durch den Behandler — auch die Zuweisung aus
        /// <see cref="btn_Carrier_Click"/> nach dem Anlegen eines Trägers, die erst
        /// <b>nach</b> dem Befüllen erfolgt.
        /// </para>
        /// </remarks>
        private void FillCarrierComboBox()
        {
            // Daten holen
            List<EnergyCarrier> allCarriers = GetAllCarriers(m_ID_Projekt);

            _traegerlisteWirdGefuellt = true;
            try
            {
                // ComboBox konfigurieren
                listBox_Energieträger.DataSource = allCarriers;
                // Darstellung
                listBox_Energieträger.DisplayMember = "Name";
                // Welcher Wert soll im Hintergrund identifizieren?
                listBox_Energieträger.ValueMember = "Id";
                listBox_Energieträger.SelectedIndex = -1; // Start ohne Auswahl
            }
            finally
            {
                _traegerlisteWirdGefuellt = false;
            }

            // Keine Auswahl, also auch kein Block: Bisher blieb der zuletzt während der
            // Bindung gebaute Block im Panel stehen, obwohl in der Liste nichts markiert
            // war. Im Konstruktor räumte ihn RenderEnergieTab() zufällig weg, nach
            // „Hinzufügen" ohne Treffer blieb er sichtbar — und wurde beim Schließen
            // (OnFormClosing) sogar gespeichert.
            flpContainer_Energiekosten.Controls.Clear();
        }

        private string CreateNewEnergyCarrier()
        {
            using (var dlg = new Form_Kosten_Auswahl())
            {
                if (dlg.ShowDialog() != DialogResult.OK) return "";

                try
                {
                    // Default-Werte aus dem Brennstoff-Stamm (Preise/Emissionen)
                    double default_arbeitspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Arbeitspreis", dlg.SelectedBrennstoffID));
                    double default_grundpreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Grundpreis", dlg.SelectedBrennstoffID));
                    double default_leistungspreis = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "Standard_Leistungspreis", dlg.SelectedBrennstoffID));
                    double default_co2 = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "CO2", dlg.SelectedBrennstoffID));
                    double default_so2 = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "SO2", dlg.SelectedBrennstoffID));
                    double default_nox = ToDouble(DataRepository.GetValueById("Tab_Brennstoff_Stamm", "NOx", dlg.SelectedBrennstoffID));

                    // 1) Katalog-Träger suchen; existiert er, wird er wiederverwendet
                    int carrierId = -1;
                    object existing = DataRepository.ExecuteScalar(
                        "SELECT id FROM energy_carrier WHERE name = ?",
                        new OleDbParameter[] { new OleDbParameter("@name", dlg.SelectedName) });
                    if (existing != null && existing != DBNull.Value)
                        carrierId = Convert.ToInt32(existing);

                    if (carrierId < 0)
                    {
                        // Katalog-Datensatz nur anlegen, wenn wirklich neu
                        string insertSql = @"INSERT INTO energy_carrier
                             (ID_Brennstoff, code, name, group_code, pricing_model, billing_unit, hi_kwh_per_unit,
                              hs_kwh_per_unit, price_work, price_base, co2, so2, nox, is_active)
                             VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                        OleDbParameter[] ps = {
                            new OleDbParameter("@idB",   dlg.SelectedBrennstoffID),
                            new OleDbParameter("@code",  dlg.SelectedCode),
                            new OleDbParameter("@name",  dlg.SelectedName),
                            new OleDbParameter("@gc",    dlg.SelectedGroupCode),
                            new OleDbParameter("@pm",    dlg.SelectedBrennstoffCode),
                            new OleDbParameter("@unit",  dlg.SelectedBillingUnit),
                            new OleDbParameter("@shi",   dlg.SelectedHi),
                            new OleDbParameter("@shs",   dlg.SelectedHs),
                            new OleDbParameter("@defap", default_arbeitspreis),
                            new OleDbParameter("@defgp", default_grundpreis),
                            new OleDbParameter("@co2",   default_co2),
                            new OleDbParameter("@so2",   default_so2),
                            new OleDbParameter("@nox",   default_nox),
                            new OleDbParameter("@active", OleDbType.Boolean) { Value = true }
                        };
                        carrierId = DataRepository.ExecuteInsertAndGetId(insertSql, ps);
                    }

                    // 2) Ist der Träger diesem Projekt schon zugeordnet? -> nicht doppeln
                    int vorhanden = Convert.ToInt32(DataRepository.ExecuteScalar(
                        "SELECT COUNT(*) FROM energy_Project_settings WHERE ID_Projekt = ? AND ID_Energieträger = ?",
                        new OleDbParameter[] {
                    new OleDbParameter("@pid", m_ID_Projekt),
                    new OleDbParameter("@eid", carrierId)
                        }));
                    if (vorhanden > 0)
                    {
                        MessageBox.Show($"Die Energieträgervariante '{dlg.SelectedName}' ist diesem Projekt bereits zugeordnet.");
                        return dlg.SelectedName;
                    }

                    // 3) Projektbezogene Sätze anlegen (Preis-Historie + Projekt-Einstellungen)
                    // Befund B5 (11.08.2026): der Ersteintrag ließ leistungspreis leer,
                    // obwohl der Standardwert aus Tab_Brennstoff_Stamm ermittelt wurde.
                    string sqlHistory = @"INSERT INTO energy_price
                         (carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis)
                         VALUES (?, ?, ?, ?, ?, ?, ?, ?)";
                    DataRepository.ExecuteSQL(sqlHistory, new OleDbParameter[] {
                        new OleDbParameter("@cid",  carrierId),
                        new OleDbParameter("@prid", m_ID_Projekt),
                        new OleDbParameter("@ap",   Math.Round(default_arbeitspreis, 4)),
                        new OleDbParameter("@hi",   Math.Round(dlg.SelectedHi, 4)),
                        new OleDbParameter("@gp",   Math.Round(default_grundpreis, 4)),
                        new OleDbParameter("@date", OleDbType.Date) { Value = DateTime.Now },
                        new OleDbParameter("@au",   dlg.SelectedBillingUnit),
                        new OleDbParameter("@lp",   Math.Round(default_leistungspreis, 4))
                    });

                    string sqlInsert = @"INSERT INTO energy_Project_settings
                         (ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs,
                          custom_price_base, ID_Umrechnung, co2, so2, nox)
                         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    DataRepository.ExecuteSQL(sqlInsert, new OleDbParameter[] {
                        new OleDbParameter("@pid",    m_ID_Projekt),
                        new OleDbParameter("@eid",    carrierId),
                        new OleDbParameter("@p",      Math.Round(default_arbeitspreis, 4)),
                        new OleDbParameter("@pl",     Math.Round(default_leistungspreis, 4)),
                        new OleDbParameter("@h",      Math.Round(dlg.SelectedHi, 4)),
                        new OleDbParameter("@hs",     Math.Round(dlg.SelectedHs, 4)),
                        new OleDbParameter("@b",      Math.Round(default_grundpreis, 4)),
                        new OleDbParameter("@convid", dlg.SelectedConvID),
                        new OleDbParameter("@co2",    default_co2),
                        new OleDbParameter("@so2",    default_so2),
                        new OleDbParameter("@nox",    default_nox)
                    });

                    MessageBox.Show("Energieträgervariante erfolgreich angelegt.");
                    return dlg.SelectedName;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Fehler beim Speichern: " + ex.Message);
                }
            }
            return "";
        }

        // kleiner Helfer gegen null/DBNull
        private static double ToDouble(object o)
        {
            return (o != null && o != DBNull.Value) ? Convert.ToDouble(o) : 0.0;
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
            if (id == 0) return false;

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

                // Review-Befund (Phase 7): das offene ucFuelSettings des gelöschten
                // Trägers muss aus dem Panel, sonst legt das Speichern beim
                // Schließen (B6) die Projektzuordnung wieder an.
                flpContainer_Energiekosten.Controls.Clear();
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