using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;


namespace WindowsFormsApplication1
{
    public partial class Form_BHKWEing : Form
    {
        BHKWCtrl ctrl = new BHKWCtrl();               // Projekt-Operationen (Kopieren/Löschen)
        BHKWStammCtrl ctrlStamm = new BHKWStammCtrl(); // Stammdaten (Auswahlliste)
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.BHKW_TYP;
        public int m_ID_Projekt = 0;
        private WErzeugerModel model = new WErzeugerModel();
        private string m_szProjekt;
        private bool m_bWizard = false;
        private WizardParent wizardparent = null;
        private int startindex = 100000;

        public Form_BHKWEing()
        {
            InitializeComponent();

            DataGridView dgv = dataGridView1;
            dgv.AutoGenerateColumns = false;
            dgv.RowHeadersVisible = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10);

            dgv.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Name",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                //FillWeight = 50
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Eigenschaften",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                //FillWeight = 50
            });

            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Color.White;

            // Grundfarbe für alle Zeilen
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            // Farbe für jede zweite Zeile (Zebra)
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(215, 230, 245);

            dgv.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            // Erlaubt eigene Farben für den Header (sonst bleibt er Windows-Grau)
            dgv.EnableHeadersVisualStyles = false;

            // Hintergrundfarbe festlegen (ein kräftiges "BHKW-Blau")
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(51, 102, 153);

            // Schriftfarbe auf Weiß setzen (für den Kontrast zum dunklen Blau)
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

            // Der Text bestimmt die Breite (sehr genau, kann aber bei viel Text flackern)
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // ODER: Die Spalten teilen sich den verfügbaren Platz gleichmäßig auf (füllt das ganze Grid aus)
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            InitAuswahlListe();
        }

        // Konfiguriert die Auswahl-ListView (Details, Spalten Name + ID). Steuerungsname bleibt
        // "listBox_Auswahl" (jetzt ListView), damit die .resx-Eintraege weiter passen.
        private void InitAuswahlListe()
        {
            listBox_Auswahl.View = View.Details;
            listBox_Auswahl.FullRowSelect = true;
            listBox_Auswahl.HeaderStyle = ColumnHeaderStyle.None;   // keine Spaltenueberschrift
            listBox_Auswahl.MultiSelect = false;
            listBox_Auswahl.Scrollable = true;
            if (listBox_Auswahl.Columns.Count == 0)
            {
                // nur die Bezeichner-Spalte sichtbar (fuellt die Breite); die eindeutige
                // Zuordnung laeuft ueber ListViewItem.Tag, eine ID-Spalte ist nicht noetig.
                int w = listBox_Auswahl.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
                if (w < 50) w = 200;
                listBox_Auswahl.Columns.Add("", w);
            }
        }

        private WErzeugerModel GetSelectedBHKW()
        {
            if (listBox_Auswahl.SelectedItems.Count == 0) return null;
            return listBox_Auswahl.SelectedItems[0].Tag as WErzeugerModel;
        }

        private void AddAuswahlRow(WErzeugerModel m)
        {
            ListViewItem lvi = new ListViewItem(m.Bezeichner);
            lvi.Tag = m;
            listBox_Auswahl.Items.Add(lvi);
            FitColumn();
        }

        // Spalte auf den laengsten Bezeichner anpassen; bei langen Namen entsteht so eine
        // horizontale Scrollbar, bei kurzen fuellt die Spalte mindestens die Breite.
        private void FitColumn()
        {
            if (listBox_Auswahl.Columns.Count == 0) return;
            // Breite explizit aus der gemessenen Textbreite setzen (handle-unabhaengig, damit
            // die Spalte auch im Wizard vor dem Anzeigen breiter als der Client werden kann
            // -> horizontale Scrollbar bei langen Bezeichnern).
            int max = 0;
            foreach (ListViewItem it in listBox_Auswahl.Items)
            {
                int wItem = TextRenderer.MeasureText(it.Text, listBox_Auswahl.Font).Width;
                if (wItem > max) max = wItem;
            }
            int avail = listBox_Auswahl.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
            int w = max + 24;
            if (w < avail) w = avail;
            listBox_Auswahl.Columns[0].Width = w;
        }

        // Liest einen Integer-Spaltenwert; probiert mehrere Spaltennamen (z. B. Ruecklauf/Rücklauf).
        private static int IntCol(DataRow row, params string[] cols)
        {
            foreach (string c in cols)
                if (row.Table.Columns.Contains(c) && row[c] != DBNull.Value) return Convert.ToInt32(row[c]);
            return 0;
        }

        // Baut den WHERE-Filter (Brennstoff + Leistung) aus den ComboBoxen.
        private string BuildFilter()
        {
            string szFilter = "";
            string szFilterLeistung = "";

            if (comboBox_Leistung.Text == "Alle") szFilterLeistung = "Ptherm Like '%'";
            else if (comboBox_Leistung.Text == "kleiner 20 kW") szFilterLeistung = "Ptherm <20";
            else if (comboBox_Leistung.Text == "20 bis 40 kW") szFilterLeistung = "Ptherm >=20 and Ptherm <40";
            else if (comboBox_Leistung.Text == "40 bis 80 kW") szFilterLeistung = "Ptherm >=40 and Ptherm <80";
            else if (comboBox_Leistung.Text == "80 bis 200 kW") szFilterLeistung = "Ptherm >=80 and Ptherm <200";
            else if (comboBox_Leistung.Text == "200 bis 500 kW") szFilterLeistung = "Ptherm >=200 and Ptherm <500";
            else if (comboBox_Leistung.Text == "500 bis 800 kW") szFilterLeistung = "Ptherm >=500 and Ptherm <800";
            else if (comboBox_Leistung.Text == "800 bis 1200 kW") szFilterLeistung = "Ptherm >=800 and Ptherm <1200";
            else if (comboBox_Leistung.Text == "über 1.200 kW") szFilterLeistung = "Ptherm >=1200";

            if (comboBox_Brennstoff.Text == "Gas") szFilter = "(Brennstoff >=1 and Brennstoff <=5) or Brennstoff=14";
            else if (comboBox_Brennstoff.Text == "Öl") szFilter = "(Brennstoff >=6 and Brennstoff <=9) or (Brennstoff >=18 and Brennstoff <=22)";
            else if (comboBox_Brennstoff.Text == "Koks") szFilter = "Brennstoff=10";
            else if (comboBox_Brennstoff.Text == "Kohle") szFilter = "Brennstoff=11";
            else if (comboBox_Brennstoff.Text == "Holz") szFilter = "Brennstoff=12";
            else if (comboBox_Brennstoff.Text == "Tierische Fette") szFilter = "Brennstoff=17";
            else if (comboBox_Brennstoff.Text == "Strom") szFilter = "Brennstoff=13";
            else if (comboBox_Brennstoff.Text == "Pellets") szFilter = "Brennstoff=15";
            else if (comboBox_Brennstoff.Text == "Rapsöl") szFilter = "Brennstoff=16";
            else if (comboBox_Brennstoff.Text == "Sonstige") szFilter = "Brennstoff=23";
            else if (comboBox_Brennstoff.Text == "Alle") szFilter = "Brennstoff Like '%'";

            if (szFilterLeistung == "") szFilterLeistung = "Ptherm Like '%'";

            if (szFilter == "")
                return szFilterLeistung;
            return "(" + szFilter + ") and " + szFilterLeistung;
        }

        // Auswahlliste (Pick-Liste) speist sich jetzt aus den STAMM-Daten (Tab_BHKW_STAMM),
        // gelesen über das DataRepository.
        private void SetFilter()
        {
            string szWhere = BuildFilter();
            string sql = "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE " + szWhere + " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);

            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            int i = 0;
            foreach (DataRow row in dt.Rows)
            {
                int brennIdx = row["Brennstoff"] != DBNull.Value ? Convert.ToInt32(row["Brennstoff"]) : 0;
                string brennText = (brennIdx >= 1 && brennIdx <= ctrl.Brennstoffart.Count) ? ctrl.Brennstoffart[brennIdx - 1] : "";
                dgv.Rows.Add(
                    row["Bezeichner"].ToString(),
                    row["Firma"].ToString() + "\nBrennstoff: " + brennText +
                    "\nPtherm: " + row["Ptherm"].ToString() + " kW" +
                    "\nPel: " + row["Pel"].ToString() + " kW");
                dgv.Rows[i++].DividerHeight = 5;
            }
        }

        private Form getWizardPage()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form.Name == "WizardParent")
                {
                    return form;
                }
            }
            return null;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Form_BHKWEing_Load(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();

            if (listBox_Auswahl.Items.Count > 0)
            {
                listBox_Auswahl.Select();
                listBox_Auswahl.Items[0].Selected = true;
            }
            SetFilter();
        }

        public void SetControls(string szProjekt, bool bWizard = false)
        {
            if (bWizard)
            {
                btn_Abbrechen.Visible = false;
                btn_OK.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                wizardparent = (WizardParent)getWizardPage();
                list_werzmodel = wizardparent.list_werzmodel;
                m_bWizard = bWizard;
            }

            m_szProjekt = szProjekt;

            SetFilter();

            dataGridView1.Select();
            dataGridView1.ClearSelection();

            listBox_Auswahl.Items.Clear();
            for (int n = 0; n < list_werzmodel.Count; n++)
            {
                if (list_werzmodel[n].ID_Type == WizardItemClass.BHKW_TYP)
                {
                    AddAuswahlRow(list_werzmodel[n]);
                }
            }
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.Items[0].Selected = true;

            textBox_Summe_Leistung.Text = SummeLeistung().ToString();

            comboBox_Brennstoff.Items.Clear();
            comboBox_Leistung.Items.Clear();

            comboBox_Brennstoff.Items.Add("Alle");
            for (int i = 0; i < ctrl.Brennstoffart_Gruppe.Count; i++)
            {
                comboBox_Brennstoff.Items.Add(ctrl.Brennstoffart_Gruppe[i]);
            }

            comboBox_Leistung.Items.Add("Alle");
            for (int i = 0; i < BHKWCtrl.LeistungText.Length; i++)
            {
                if (BHKWCtrl.LeistungText[i] != "")
                    comboBox_Leistung.Items.Add(BHKWCtrl.LeistungText[i]);
            }

            comboBox_Brennstoff.SelectedIndex = 0;
            comboBox_Leistung.SelectedIndex = 0;
        }

        // Liest die Detail-Felder eines Bezeichners für die Anzeige.
        // Pick-Liste zeigt STAMM-Daten; bereits gewählte (Projekt) werden aus Tab_BHKW gelesen.
        private void FillDetailsFromStamm(string szName)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE Bezeichner = ?",
                new OleDbParameter("@name", szName));
            FillDetailControls(dt);
        }

        private void FillDetailsFromProjekt(string szName)
        {
            DataTable dt;
            if (m_ID_Projekt > 0)
            {
                dt = DataRepository.GetDataTable(
                    "SELECT * FROM Tab_BHKW WHERE Bezeichner = ? AND ID_Projekt = ?",
                    new OleDbParameter("@name", szName),
                    new OleDbParameter("@idProj", m_ID_Projekt));
            }
            else
            {
                // Wizard-Fall ohne persistiertes Projekt: Attribute stammen aus den Stammdaten.
                dt = DataRepository.GetDataTable(
                    "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE Bezeichner = ?",
                    new OleDbParameter("@name", szName));
            }
            FillDetailControls(dt);
        }

        private void FillDetailControls(DataTable dt)
        {
            if (dt == null || dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            textBox_Name.Text = r["Bezeichner"].ToString();
            textBox_Firma.Text = r["Firma"] == DBNull.Value ? "" : r["Firma"].ToString();
            textBox_Beschreibung.Text = r["Beschreibung"] == DBNull.Value ? "" : r["Beschreibung"].ToString();
            textBox_Leistung_th.Text = r["Ptherm"].ToString();
            textBox_Leistung_el.Text = r["Pel"].ToString();
        }

        private void listBox_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            ApplySelectedBHKW();
        }

        // Aktualisiert die Detailanzeige aus dem aktuell selektierten BHKW-Eintrag.
        private void ApplySelectedBHKW()
        {
            WErzeugerModel m = GetSelectedBHKW();
            if (m == null) return;

            FillDetailsFromProjekt(m.Bezeichner);

            textBox__M_GrenzL.Text = m.Grenzleistung.ToString();
            textBox_Vorlauf.Text = m.Vorlauf.ToString();
            textBox_Ruecklauf.Text = m.Ruecklauf.ToString();

            dataGridView1.ClearSelection();
        }

        private void btn_Hinzzu_Click(object sender, EventArgs e)
        {
            int nBrennstoff = 0;

            if (dataGridView1.CurrentCell == null || dataGridView1.CurrentCell.RowIndex == -1) return;
            model.Bezeichner = (string)dataGridView1.CurrentRow.Cells[0].Value;

            // Stamm-ID des ausgewählten Datensatzes ermitteln
            int stammId = DataRepository.GetIdByName(BHKWStammCtrl.TABLE, "Bezeichner", model.Bezeichner);
            if (stammId <= 0)
            {
                MessageBox.Show("Das ausgewählte BHKW wurde in den Stammdaten nicht gefunden.");
                return;
            }
            model.ID_Type = WizardItemClass.BHKW_TYP;
            model.ID = startindex++;

            // Vorlauf/Ruecklauf aus dem Stammdatensatz als Default uebernehmen; sie werden mit
            // in Tab_Energieanlagen geschrieben (siehe WizardCtrl.Add_WP_Waermeerzeuger).
            DataTable dtStamm = DataRepository.GetDataTable(
                "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE ID = ?",
                new OleDbParameter("@id", stammId));
            if (dtStamm != null && dtStamm.Rows.Count > 0)
            {
                DataRow sr = dtStamm.Rows[0];
                model.Vorlauf = IntCol(sr, "Vorlauf");
                model.Ruecklauf = IntCol(sr, "Ruecklauf", "Rücklauf");
                nBrennstoff = IntCol(sr, "Brennstoff"); 
            }

            // Datentechnisch: Datensatz aus der STAMM-Tabelle in die Projekt-Tabelle kopieren,
            // sofern für das Projekt noch nicht vorhanden. ID_Projekt wird dabei gesetzt.
            if (m_ID_Projekt > 0)
            {
                // CopyFromStamm liefert die (neue oder bereits vorhandene) Tab_BHKW-Projekt-ID.
                int projektId = ctrl.CopyFromStamm(stammId, m_ID_Projekt);
                if (projektId <= 0)
                {
                    MessageBox.Show("Der Datensatz konnte nicht in das Projekt übernommen werden.");
                    return;
                }
                // WICHTIG: ID_BHKW referenziert die Projekt-Tabelle (Tab_BHKW), NICHT die Stammdaten.
                model.ID_BHKW = projektId;
            }
            else
            {
                // Reiner Wizard-Vorschaumodus ohne Projekt: kein DB-Satz, Stamm-ID als Platzhalter.
                model.ID_BHKW = stammId;
            }

            int caarierID = 0;
            CreateNewEnergyCarrier(nBrennstoff, ref caarierID);
            model.ID_Carrier = caarierID;   

            list_werzmodel.Add(model);
            if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;

            AddAuswahlRow(model);
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.Items[listBox_Auswahl.Items.Count - 1].Selected = true;

            // Neues, leeres model-Objekt für den nächsten Hinzufügen-Vorgang
            model = new WErzeugerModel();

            textBox_Summe_Leistung.Text = SummeLeistung().ToString();
  
        }
        private static double ToDouble(object o)
        {
            return (o != null && o != DBNull.Value) ? Convert.ToDouble(o) : 0.0;
        }

        private string CreateNewEnergyCarrier(int nBrennstoff, ref int carrierId)
        {
            using (var dlg = new Form_Kosten_Auswahl())
            {
                string szBrennstoff = "";
                object br = DataRepository.ExecuteScalar(
                    "SELECT Bezeichner FROM Tab_Brennstoff_Stamm WHERE ID = ?",
                    new OleDbParameter[] { new OleDbParameter("@id", nBrennstoff) });
                if (br != null && br != DBNull.Value)
                    szBrennstoff = br.ToString();
                
                dlg.m_szBVrennstoff = szBrennstoff;
                dlg.bOhneVariante = false;

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
                    carrierId = -1;
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

                    // 1b) Ab hier PROJEKTGEBUNDENE Sätze - die gehen nur mit einem wirklich
                    // gespeicherten Projekt. SetControls belegt m_ID_Projekt NICHT (es bekommt
                    // nur den Projektnamen); die Nicht-Wizard-Aufrufer setzen das Feld vor
                    // ShowDialog selbst (BHKWKontextMenuCtrl, Form_Start), im Wizard bleibt es
                    // deshalb auf 0. energy_price und energy_Project_settings haben aber
                    // je eine erzwungene Beziehung auf Tab_Projekt.ID - beide INSERTs
                    // scheiterten damit (zwei "Datenbankfehler"-Meldungen), und das Projekt
                    // hatte anschließend einen Energieträger an der Anlage, aber KEINEN
                    // Preis-/Emissionssatz. Im Wizard bleibt es deshalb beim Katalogträger;
                    // die projektgebundenen Sätze trägt WizardCtrl.Add_Projekt_Energietraeger
                    // beim Speichern nach.
                    if (m_bWizard || m_ID_Projekt <= 0)
                    {
                        MessageBox.Show("Energieträgervariante vorgemerkt. Die Preis- und Emissionssätze " +
                                        "werden beim Speichern des Projekts angelegt.");
                        return dlg.SelectedName;
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
        private void btn_BHKW_Löschen_Click(object sender, EventArgs e)
        {
            if (listBox_Auswahl.SelectedItems.Count == 0) return;
            ListViewItem lvi = listBox_Auswahl.SelectedItems[0];
            WErzeugerModel m = lvi.Tag as WErzeugerModel;
            if (m == null) return;
            string szName = m.Bezeichner;

            list_werzmodel.Remove(m);
            listBox_Auswahl.Items.Remove(lvi);
            FitColumn();
            if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;

            // Projekt-Kopie nur entfernen, wenn keine weitere Auswahl mehr darauf verweist
            // (mehrere Instanzen desselben BHKW teilen sich eine Tab_BHKW-Kopie).
            bool nochReferenziert = false;
            foreach (WErzeugerModel it in list_werzmodel)
                if (it.ID_Type == WizardItemClass.BHKW_TYP && it.ID_BHKW == m.ID_BHKW) { nochReferenziert = true; break; }
            if (m_ID_Projekt > 0 && !nochReferenziert)
            {
                ctrl.DeleteFromProjekt(szName, m_ID_Projekt);
            }

            textBox_Summe_Leistung.Text = SummeLeistung().ToString();

            if (listBox_Auswahl.Items.Count > 0)
            {
                listBox_Auswahl.Items[0].Selected = true;
                listBox_Auswahl.Select();
            }
            else
            {
                textBox__M_GrenzL.Text = "0";
                if (dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.Rows[0].Selected = true;
                    dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
                }
            }
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private double SummeLeistung()
        {
            double summe = 0;

            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                // ID_BHKW verweist bei vorhandenem Projekt auf Tab_BHKW (Projekt),
                // im Wizard-Vorschaumodus auf die Stammdaten.
                if (m_ID_Projekt > 0)
                {
                    ctrl.ReadSingle(list_werzmodel[i].ID_BHKW);
                    summe += ctrl.m_Ptherm;
                }
                else
                {
                    ctrlStamm.ReadSingle(list_werzmodel[i].ID_BHKW);
                    summe += ctrlStamm.m_Ptherm;
                }
            }
            return summe;
        }

        private void textBox__M_GrenzL_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox__M_GrenzL, textBox__M_GrenzL.Text))
            {
                textBox__M_GrenzL.Undo();
                textBox__M_GrenzL.ClearUndo();
                return;
            }

            WErzeugerModel m = GetSelectedBHKW();
            if (m != null)
                m.Grenzleistung = double.Parse(textBox__M_GrenzL.Text);
        }

        private void dataGridView1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;
            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;
            // Pick-Liste -> Detailanzeige aus den Stammdaten
            FillDetailsFromStamm(szName);
        }

        private void comboBox_Brennstoff_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void btn_DBBHKW_Edit_Click(object sender, EventArgs e)
        {
            // Bearbeitet einen STAMM-Datensatz (Editor Form_DBBHKW muss auf STAMM zeigen).
            Form_DBBHKW frm = new Form_DBBHKW();
            frm.m_mode = Form_DBBHKW.MODE_EDIT;
            DataGridViewSelectedRowCollection sr = dataGridView1.SelectedRows;
            if (sr.Count == 0) { MessageBox.Show("Bitte ein BHKW auswählen!"); return; }

            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;

            // Editor ist auch für schreibgeschützte (ReadOnly) Datensätze aufrufbar;
            // dort ist lediglich der "Überschreiben"-Button gesperrt.
            frm.SetControls(szName);
            DialogResult result = frm.ShowDialog();
            if (result == DialogResult.OK) SetFilter();
        }

        private void btn_DBBHKW_Neu_Click(object sender, EventArgs e)
        {
            Form_DBBHKW frm = new Form_DBBHKW();
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();

            System.Drawing.Point p1 = btn_DBBHKW_Neu.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                frm.m_mode = Form_DBBHKW.MODE_NEU;
                frm.SetControls(frmLabel.m_szName);
                frm.m_szName = frmLabel.m_szName;
                frm.ShowDialog();
                SetFilter();
            }
        }

        private void btn_DBBHKW_Löschen_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection sr = dataGridView1.SelectedRows;
            if (sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte ein BHKW auswählen!"); return; }

            string szName = (string)dataGridView1.SelectedRows[0].Cells[0].Value;

            // ReadOnly-Stammdatensätze dürfen nicht gelöscht werden
            if (ctrlStamm.IsReadOnly(szName))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var result = MessageBox.Show("Wollen Sie wirklich das BHKW löschen?", "Löschen", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                // Löschen aus der STAMM-Tabelle über den Stamm-Controller (DataRepository)
                if (ctrlStamm.Delete(szName))
                {
                    dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
                }
            }
        }
        private void textBox_Ruecklauf_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Ruecklauf, textBox_Ruecklauf.Text)) { textBox_Ruecklauf.Undo(); }
            WErzeugerModel m = GetSelectedBHKW();
            if (m != null && m.ID_Type == WizardItemClass.BHKW_TYP)
                m.Ruecklauf = Int32.Parse(textBox_Ruecklauf.Text);
        }

        private void textBox_Vorlauf_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Vorlauf, textBox_Vorlauf.Text)) { textBox_Vorlauf.Undo(); }
            WErzeugerModel m = GetSelectedBHKW();
            if (m != null && m.ID_Type == WizardItemClass.BHKW_TYP)
                m.Vorlauf = Int32.Parse(textBox_Vorlauf.Text);
        }

        // ListView loest SelectedIndexChanged nicht aus, wenn sich der Index nicht aendert
        // (Klick auf das bereits selektierte bzw. einzige Item). Deshalb per Klick nachziehen.
        private void listBox_Auswahl_MouseClick(object sender, MouseEventArgs e)
        {
            ApplySelectedBHKW();
        }
    }
}
