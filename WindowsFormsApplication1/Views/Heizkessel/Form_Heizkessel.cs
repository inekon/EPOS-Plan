using MathNet.Numerics.Optimization;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Heizkessel : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private HeizkesselStammCtrl heizkesselctrl = new HeizkesselStammCtrl();
        public List<WErzeugerModel> list_heizkesselmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.KESSEL_TYP;
        public int m_ID_Projekt = 0;
        int startindex = 100000;
        private bool m_bWizard = false;
        private WizardParent wizardparent = null;

        public Form_Heizkessel()
        {
            InitializeComponent();
            InitKesselListe();
            listBox_Kessel_DB.Items.Clear();
            listBox_Kessel.Items.Clear();
        }

        // Konfiguriert die Auswahl-ListView (Details, Spalten Name + ID). Der Steuerungsname
        // bleibt "listBox_Kessel" (jetzt ListView), damit die .resx-Eintraege weiter passen.
        private void InitKesselListe()
        {
            listBox_Kessel.View = View.Details;
            listBox_Kessel.FullRowSelect = true;
            listBox_Kessel.HeaderStyle = ColumnHeaderStyle.None;   // keine Spaltenueberschrift
            listBox_Kessel.MultiSelect = false;
            listBox_Kessel.Scrollable = true;
            if (listBox_Kessel.Columns.Count == 0)
            {
                // nur die Bezeichner-Spalte sichtbar (fuellt die Breite); die eindeutige
                // Zuordnung laeuft ueber ListViewItem.Tag, eine ID-Spalte ist nicht noetig.
                int w = listBox_Kessel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
                if (w < 50) w = 200;
                listBox_Kessel.Columns.Add("", w);
            }
        }

        // Liefert das zur markierten Zeile gehoerende Modell (oder null).
        private WErzeugerModel GetSelectedKessel()
        {
            if (listBox_Kessel.SelectedItems.Count == 0) return null;
            return listBox_Kessel.SelectedItems[0].Tag as WErzeugerModel;
        }

        // Fuegt eine Zeile fuer ein Modell hinzu (Tag = Modell, Spalte ID = eindeutige Instanz-ID).
        private void AddKesselRow(WErzeugerModel m)
        {
            ListViewItem lvi = new ListViewItem(m.Bezeichner);
            lvi.Tag = m;
            listBox_Kessel.Items.Add(lvi);
            FitColumn();
        }

        // Spalte auf den laengsten Bezeichner anpassen; bei langen Namen entsteht so eine
        // horizontale Scrollbar, bei kurzen fuellt die Spalte mindestens die Breite.
        private void FitColumn()
        {
            if (listBox_Kessel.Columns.Count == 0) return;
            // Breite explizit aus der gemessenen Textbreite setzen (handle-unabhaengig, damit
            // die Spalte auch im Wizard vor dem Anzeigen breiter als der Client werden kann
            // -> horizontale Scrollbar bei langen Bezeichnern).
            int max = 0;
            foreach (ListViewItem it in listBox_Kessel.Items)
            {
                int wItem = TextRenderer.MeasureText(it.Text, listBox_Kessel.Font).Width;
                if (wItem > max) max = wItem;
            }
            int avail = listBox_Kessel.ClientSize.Width - SystemInformation.VerticalScrollBarWidth;
            int w = max + 24;
            if (w < avail) w = avail;
            listBox_Kessel.Columns[0].Width = w;
        }

        // Liest einen ganzzahligen Spaltenwert; probiert mehrere Spaltennamen
        // (z.B. "Ruecklauf" ASCII bzw. "Rücklauf" mit Umlaut).
        private static int IntCol(DataRow row, params string[] cols)
        {
            foreach (string c in cols)
                if (row.Table.Columns.Contains(c) && row[c] != DBNull.Value) return Convert.ToInt32(row[c]);
            return 0;
        }

        public void SetControls(int IDProjekt, bool bWizard = false)
        {
            m_ID_Projekt = IDProjekt;
            if (bWizard)
            {
                m_bWizard = true;
                btn_OK.Visible = false;
                btn_Abbrechen.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                wizardparent = (WizardParent)getWizardPage();
                list_heizkesselmodel = wizardparent.list_werzmodel;
            }
            listBox_Kessel.Items.Clear();
            for (int i = 0; i < list_heizkesselmodel.Count; i++)
            {
                if (list_heizkesselmodel[i].ID_Type == WizardItemClass.KESSEL_TYP)
                {
                    AddKesselRow(list_heizkesselmodel[i]);
                }
            }
            if (listBox_Kessel.Items.Count > 0) listBox_Kessel.Items[0].Selected = true;
        }

        private void Form_Heizkessel_Load(object sender, EventArgs e)
        {
            heizkesselctrl.ReadAll();
            for (int i = 0; i < heizkesselctrl.rows; i++)
            {
                listBox_Kessel_DB.Items.Add(heizkesselctrl.items[i].Name);

            }

            comboBox_Brennstoffart.Items.Add("Alle");
            for (int i = 0; i < heizkesselctrl.Brennstoffart_Gruppe.Count; i++)
            {
                comboBox_Brennstoffart.Items.Add(heizkesselctrl.Brennstoffart_Gruppe[i]);
            }

            comboBox_Leistung.Items.Add("Alle");
            comboBox_Leistung.Items.Add("bis 50 kW");
            comboBox_Leistung.Items.Add(">50 bis 200 kW");
            comboBox_Leistung.Items.Add(">200 bis 500 kW");
            comboBox_Leistung.Items.Add(">500 bis 1.000 kW");
            comboBox_Leistung.Items.Add("über 1.000 kW");
            comboBox_Leistung.Text = "Alle";
            comboBox_Brennstoffart.Text = "Alle";
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

        private void btn_Kessel_Hinzu_Click(object sender, EventArgs e)
        {
            int nBrennstoff = 0;

            WizardParent wizardparent = (WizardParent)getWizardPage();

            if (listBox_Kessel_DB.Text == "") return;

            // Stamm-ID des ausgewaehlten Heizkessels ermitteln.
            int stammId = DataRepository.GetIdByName(HeizkesselStammCtrl.TABLE, "Bezeichner", listBox_Kessel_DB.Text);
            if (stammId <= 0)
            {
                MessageBox.Show("Der ausgewählte Heizkessel wurde in den Stammdaten nicht gefunden.");
                return;
            }

            WErzeugerModel model = new WErzeugerModel();
            model.ID = startindex++;
            model.ID_Projekt = m_ID_Projekt;
            model.ID_Type = m_nType;
            model.Bezeichner = listBox_Kessel_DB.Text;

            // Vorlauf/Ruecklauf aus dem Stamm-Datensatz vorbelegen -> fliessen als
            // Default in Tab_Energieanlagen (Vorlauf, Ruecklauf) beim Speichern.
            DataTable dtStamm = DataRepository.GetDataTable(
                "SELECT * FROM " + HeizkesselStammCtrl.TABLE + " WHERE ID = ?",
                new OleDbParameter("@id", stammId));
            if (dtStamm != null && dtStamm.Rows.Count > 0)
            {
                DataRow sr = dtStamm.Rows[0];
                model.Vorlauf = IntCol(sr, "Vorlauf");
                model.Ruecklauf = IntCol(sr, "Ruecklauf", "Rücklauf");
                nBrennstoff = IntCol(sr, "Brennstoff");
            }

            // Analog zu BHKW: im direkten Projektmodus den Stammdatensatz sofort in die Projekt-Tabelle
            // kopieren (idempotent) und die PROJEKT-ID referenzieren. Im Wizard-Vorschaumodus nur die
            // Stamm-ID als Platzhalter; die eigentliche Kopie macht WizardCtrl.Add_WP_Waermeerzeuger beim Speichern.
            if (!m_bWizard && m_ID_Projekt > 0)
            {
                int projektId = new HeizkesselCtrl().CopyFromStamm(stammId, m_ID_Projekt);
                if (projektId <= 0)
                {
                    MessageBox.Show("Der Datensatz konnte nicht in das Projekt übernommen werden.");
                    return;
                }
                // WICHTIG: ID_Kessel referenziert die Projekt-Tabelle (Tab_Heizkessel), NICHT die Stammdaten.
                model.ID_Kessel = projektId;
            }
            else
            {
                model.ID_Kessel = stammId;
            }

            list_heizkesselmodel.Add(model);
            AddKesselRow(model);
            if (m_bWizard) wizardparent.list_werzmodel = list_heizkesselmodel;

            CreateNewEnergyCarrier(nBrennstoff);
        }

        private static double ToDouble(object o)
        {
            return (o != null && o != DBNull.Value) ? Convert.ToDouble(o) : 0.0;
        }

        private string CreateNewEnergyCarrier(int nBrennstoff)
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
                dlg.bOhneVariante = true;

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

        private void btn_Kessel_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Kessel.SelectedItems.Count == 0) return;
            ListViewItem lvi = listBox_Kessel.SelectedItems[0];
            WErzeugerModel m = lvi.Tag as WErzeugerModel;
            if (m == null) return;
            string szName = m.Bezeichner;

            list_heizkesselmodel.Remove(m);
            listBox_Kessel.Items.Remove(lvi);
            FitColumn();
            if (m_bWizard) wizardparent.list_werzmodel = list_heizkesselmodel;

            // Projekt-Kopie nur entfernen, wenn keine weitere Auswahl mehr darauf verweist
            // (mehrere Instanzen desselben Kessels teilen sich eine Tab_Heizkessel-Kopie).
            bool nochReferenziert = false;
            foreach (WErzeugerModel it in list_heizkesselmodel)
                if (it.ID_Type == WizardItemClass.KESSEL_TYP && it.ID_Kessel == m.ID_Kessel) { nochReferenziert = true; break; }
            if (!m_bWizard && m_ID_Projekt > 0 && !nochReferenziert)
            {
                new HeizkesselCtrl().DeleteFromProjekt(szName, m_ID_Projekt);
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void listBox_Kessel_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            ApplySelectedKessel();
        }

        // Aktualisiert die Detailanzeige aus dem aktuell selektierten Kessel-Eintrag.
        private void ApplySelectedKessel()
        {
            WErzeugerModel m = GetSelectedKessel();
            if (m == null) return;

            textBox_Vorlauf.Text = m.Vorlauf.ToString();
            textBox_Ruecklauf.Text = m.Ruecklauf.ToString();

            RecordSet rs = new RecordSet();
            rs.Open("select * from [Tab_Heizkessel] where ID=" + m.ID_Kessel);
            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Bezeichner");
                textBox_Kesselbeschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Kesseltyp.Text = heizkesselctrl.Brennstoffart[(int)rs.Read("Brennstoff") - 1].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
                textBox_Investitionskosten.Text = ((double)rs.Read("Investitionskosten")).ToString("F2");
                checkBox_Brennwert.Checked = (bool)rs.Read("Brennwert");
            }
            rs.Close();
        }

        private void listBox_Kessel_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from [Tab_Heizkessel_STAMM] where Bezeichner='" + listBox_Kessel_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Bezeichner");
                textBox_Kesselbeschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Kesseltyp.Text = heizkesselctrl.Brennstoffart[(int)rs.Read("Brennstoff") - 1].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
                textBox_Investitionskosten.Text = ((double)rs.Read("Investitionskosten")).ToString("F2");
                checkBox_Brennwert.Checked = (bool)rs.Read("Brennwert");
            }
            rs.Close();
        }

        private void comboBox_Brennstoffart_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string szFilter = "";
            string szFilterLeistung = "";
            string sql = "";

            szFilterLeistung = "";
            if (comboBox_Leistung.Text == "Alle") szFilterLeistung = "Ptherm Like '%'";
            else if (comboBox_Leistung.Text == "bis 50 kW") szFilterLeistung = "Ptherm <50";
            else if (comboBox_Leistung.Text == ">50 bis 200 kW") szFilterLeistung = "Ptherm >=50 and Ptherm <200";
            else if (comboBox_Leistung.Text == ">200 bis 500 kW") szFilterLeistung = "Ptherm >=200 and Ptherm <500";
            else if (comboBox_Leistung.Text == ">500 bis 1.000 kW") szFilterLeistung = "Ptherm >=500 and Ptherm <1000";
            else if (comboBox_Leistung.Text == "über 1.000 kW") szFilterLeistung = "Ptherm >=1000";


            if (comboBox_Brennstoffart.Text == "Gas") szFilter = "(Brennstoff >=1 and Brennstoff <=5) or Brennstoff=14";
            else if (comboBox_Brennstoffart.Text == "Öl") szFilter = "(Brennstoff >=6 and Brennstoff <=9) or (Brennstoff >=18 and Brennstoff <=22)";
            else if (comboBox_Brennstoffart.Text == "Koks") szFilter = "Brennstoff=10";
            else if (comboBox_Brennstoffart.Text == "Kohle") szFilter = "Brennstoff=11";
            else if (comboBox_Brennstoffart.Text == "Holz") szFilter = "Brennstoff=12";
            else if (comboBox_Brennstoffart.Text == "Tierische Fette") szFilter = "Brennstoff=17";
            else if (comboBox_Brennstoffart.Text == "Strom") szFilter = "Brennstoff=13";
            else if (comboBox_Brennstoffart.Text == "Pellets") szFilter = "Brennstoff=15";
            else if (comboBox_Brennstoffart.Text == "Rapsöl") szFilter = "Brennstoff=16";
            else if (comboBox_Brennstoffart.Text == "Sonstige") szFilter = "Brennstoff=23";
            else if (comboBox_Brennstoffart.Text == "Alle") szFilter = "Brennstoff Like '%'";

            listBox_Kessel_DB.Items.Clear();
            if (szFilter == "")
                sql = "select * from [Tab_Heizkessel_STAMM] where " + szFilterLeistung + " order by Bezeichner";
            else
                sql = "select * from [Tab_Heizkessel_STAMM] where " + szFilter + " and " + szFilterLeistung + " order by Bezeichner";

            rs.Open(sql);

            while (rs.Next())
            {
                listBox_Kessel_DB.Items.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_Heizkessel_Bearbeiten frm = new Form_Heizkessel_Bearbeiten(Form_Heizkessel_Bearbeiten.MODE_EDIT);

            if (listBox_Kessel_DB.Text == "") return;
            int index = listBox_Kessel_DB.SelectedIndex;
            frm.SetControls(listBox_Kessel_DB.Text, textBox_Kesselbeschreibung.Text);
            DialogResult ret = frm.ShowDialog();

            if (ret == DialogResult.OK)
            {
                string szKessel = frm.m_szKessel;
                listBox_Kessel.SelectedItems.Clear();
                listBox_Kessel_DB.SelectedItems.Clear();
                heizkesselctrl.ReadAll();

                for (int i = 0; i < heizkesselctrl.rows; i++)
                {
                    listBox_Kessel_DB.Items.Add(heizkesselctrl.items[i].Name);
                }
                listBox_Kessel_DB.SelectedIndex = -1;
                listBox_Kessel_DB.SelectedIndex = index;
            }
        }

        private void btn_Löschen_Click(object sender, EventArgs e)
        {
            if (listBox_Kessel_DB.SelectedIndex == -1) { MessageBox.Show("Bitte ein Modul auswählen!"); return; }

            if (!heizkesselctrl.Delete(listBox_Kessel_DB.Text)) return;

            listBox_Kessel_DB.Items.RemoveAt(listBox_Kessel_DB.SelectedIndex);
        }

        private void btn_Admin_Click(object sender, EventArgs e)
        {
            Form_Heizkessel_Admin frm = new Form_Heizkessel_Admin();
            frm.ShowDialog();
            heizkesselctrl.ReadAll();
            listBox_Kessel_DB.Items.Clear();
            for (int i = 0; i < heizkesselctrl.rows; i++)
            {
                listBox_Kessel_DB.Items.Add(heizkesselctrl.items[i].Name);

            }
        }

        private void textBox_Ruecklauf_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Ruecklauf, textBox_Ruecklauf.Text)) { textBox_Ruecklauf.Undo(); }
            WErzeugerModel m = GetSelectedKessel();
            if (m != null && m.ID_Type == WizardItemClass.KESSEL_TYP)
                m.Ruecklauf = Int32.Parse(textBox_Ruecklauf.Text);
        }

        private void textBox_Vorlauf_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Vorlauf, textBox_Vorlauf.Text)) { textBox_Vorlauf.Undo(); }
            WErzeugerModel m = GetSelectedKessel();
            if (m != null && m.ID_Type == WizardItemClass.KESSEL_TYP)
                m.Vorlauf = Int32.Parse(textBox_Vorlauf.Text);
        }

        // ListView loest SelectedIndexChanged nicht aus, wenn sich der Index nicht aendert
        // (Klick auf das bereits selektierte bzw. einzige Item). Deshalb per Klick nachziehen.
        private void listBox_Kessel_MouseClick(object sender, MouseEventArgs e)
        {
            ApplySelectedKessel();
        }
    }
}
