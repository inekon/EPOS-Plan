using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Gebaeude : Form
    {
        private List<string> list_geb = new List<string>() { "vor 1919", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978", "1979 bis 1983", "1984 bis 1994", "1995 bis 2000", "Niedrigenergiebauweise", "Passivhaus", "EnEv 2007", "Eff. 70 (EnEV 2007)", "EnEV 2009", "Eff. 70 (EnEV 2009)", "Eff. 55 (EnEV 2009)", "EnEV 2014", "EnEV 2016", "Eff. 100 (EnEV 2016)", "Eff. 155 (EnEV 2016)", "BEG 55", "BEG 40" };
        private const string szFilterNichtWohngebäude = "Wohngebaeude_Nicht_Wohngebaeude='Nicht Wohngebaeude'";
        private const string szFilterWohngebäude = "Wohngebaeude_Nicht_Wohngebaeude='Wohngebaeude'";
        private string szFilterOption;
        private int startindex = 100000;
        private int zprojGeb_id = 0;
        public int m_ID_Projekt = 0;
        public List<Z_ProjGebModel> list_gebmodel = new List<Z_ProjGebModel>();
        public bool m_bAdmin = false;
        
        public Form_Gebaeude()
        {
            InitializeComponent();
            
            listView_Gebaeude.View = View.Details;
            listView_Gebaeude.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Gebaeude.Columns[0].Width = listView_Gebaeude.ClientRectangle.Width;

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
                FillWeight = 60
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Typ/Wohnfläche",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 40
            });

            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Color.White;
            //dgv.Columns[1].DefaultCellStyle.BackColor = Color.GreenYellow;
            //dgv.DefaultCellStyle.BackColor = Color.FromArgb(255, 215, 159, 57);

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

            SetGebaeudeDB();

            szFilterOption = szFilterWohngebäude;

            comboBox_Baujahr.Items.Clear();
            comboBox_Baujahr.Items.Clear();

            comboBox_Baujahr.Items.Add("Alle");
            for (int i = 0; i < list_geb.Count; i++)
            {
                comboBox_Baujahr.Items.Add(list_geb[i]);
            }
            comboBox_Baujahr.SelectedIndex = 0;
            comboBox_Gebäudeart.Items.Add("Alle");

            RecordSet rs = new RecordSet();
            rs.Open("SELECT * from Abfrage_Gebaeudearten where " + szFilterWohngebäude);
            while (rs.Next())
            {
                string szGebArt = (string)rs.Read("Gebaeudeart");
                comboBox_Gebäudeart.Items.Add(szGebArt);
            }
            rs.Close();

            comboBox_Gebäudeart.SelectedIndex = 0;
            dataGridView1.ClearSelection();

            textBox_Suche.PlaceholderText = "Suche, z. B. Haus*_1990*";
            textBox_Suche.TextChanged += textBox_Suche_TextChanged;
        }

        private void SetGebaeudeDB()
        {
            GebaeudeStammCtrl ctrl = new GebaeudeStammCtrl();
            DataGridView dgv = dataGridView1;

            dgv.Rows.Clear();

            ctrl.ReadAll(szFilterWohngebäude);
            for (int i = 0; i < ctrl.rows; i++)
            {
                dgv.Rows.Add(ctrl.items[i].Gebaeudename, ctrl.items[i].Gebaeudeart + "\n" + ctrl.items[i].Wohnflaeche_gesamt + " [m²]");
                dgv.Rows[i].DividerHeight = 1;
            }

            ApplyGridFilter();
        }

        public void SetControls(string szProjekt, bool bWizard=false)
        {
            Z_ProjGebCtrl ctrl = new Z_ProjGebCtrl();
            GebaeudeStammCtrl ctrl_geb = new GebaeudeStammCtrl();
            RecordSet rs = new RecordSet();

            if (bWizard)
            {
                btn_Abbrechen.Visible = false;
                btn_OK.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
            }

            listView_Gebaeude.Items.Clear();
            for (int i = 0; i < list_gebmodel.Count; i++)
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = list_gebmodel[i].Gebaeudename;
                lvitem.SubItems.Add(list_gebmodel[i].ID_Z.ToString());
                listView_Gebaeude.Items.Add(lvitem);
            }
             
            listView_Gebaeude.Select();
            if (listView_Gebaeude.Items.Count > 0) listView_Gebaeude.Items[0].Selected = true;
        }

        private Form getWizardPage()
        {
            // P4: typisierte Erkennung ueber WizardParent.Aktiver. Die frueheren elf
            // Kopien suchten den Rahmen als Zeichenkette "WizardParent" in
            // Application.OpenForms; der Rahmen meldet sich jetzt selbst an.
            return WizardParent.Aktiver as Form;
        }

        private void radioBtn_Sonstige_CheckedChanged(object sender, EventArgs e)
        {
            GebaeudeStammCtrl ctrl_geb = new GebaeudeStammCtrl();
            RecordSet rs = new RecordSet();
            DataGridView dgv = dataGridView1;

            szFilterOption = szFilterNichtWohngebäude;

            dgv.Rows.Clear();
            ctrl_geb.ReadAll(szFilterNichtWohngebäude);
    
            for (int i = 0; i < ctrl_geb.rows; i++)
            {
                dgv.Rows.Add(ctrl_geb.items[i].Gebaeudename, ctrl_geb.items[i].Gebaeudeart + "\n" + ctrl_geb.items[i].Wohnflaeche_gesamt.ToString("F2") + " [m²]");
                dgv.Rows[i].DividerHeight = 1;
            }

            comboBox_Gebäudeart.Enabled = false;
            comboBox_Gebäudeart.Items.Clear();
            comboBox_Gebäudeart.Items.Add("Alle");

            rs.Open("SELECT * from Abfrage_Gebaeudearten where " + szFilterNichtWohngebäude);
            
            while (rs.Next())
            {
                string szGebArt = (string)rs.Read("Gebaeudeart");
                comboBox_Gebäudeart.Items.Add(szGebArt);
            }
            rs.Close();

            comboBox_Gebäudeart.Enabled = true;
            comboBox_Gebäudeart.SelectedIndex = 0;
        }

        private void radioBtn_Wohngebäude_CheckedChanged(object sender, EventArgs e)
        {
            GebaeudeStammCtrl ctrl_geb = new GebaeudeStammCtrl();
            RecordSet rs = new RecordSet();
            DataGridView dgv = dataGridView1;

            szFilterOption = szFilterWohngebäude;
            dgv.Rows.Clear();
            ctrl_geb.ReadAll(szFilterWohngebäude);

    //        listBox_Gebaeude_DB.Items.Add("Alle");
            for (int i = 0; i < ctrl_geb.rows; i++)
            {
                dgv.Rows.Add(ctrl_geb.items[i].Gebaeudename, ctrl_geb.items[i].Gebaeudeart + "\n" + ctrl_geb.items[i].Wohnflaeche_gesamt.ToString("F2") + " [m²]");
                dgv.Rows[i].DividerHeight = 1;
            }

            comboBox_Gebäudeart.Enabled = false;
            comboBox_Gebäudeart.Items.Clear();
            comboBox_Gebäudeart.Items.Add("Alle");
            
            rs.Open("SELECT * from Abfrage_Gebaeudearten where " + szFilterWohngebäude);
            
            while (rs.Next())
            {
                string szGebArt = (string)rs.Read("Gebaeudeart");
                comboBox_Gebäudeart.Items.Add(szGebArt);
            }
            rs.Close();

            comboBox_Gebäudeart.Enabled = true;
            comboBox_Gebäudeart.SelectedIndex = 0;
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            Z_ProjGebModel model = new Z_ProjGebModel();
            DataGridView dgv = dataGridView1;

            if (dataGridView1.CurrentCell.RowIndex == -1) return;

            string sql = "SELECT * from [Tab_Gebaeude_STAMM] where [Tab_Gebaeude_STAMM].Bezeichner='" + (string)dataGridView1.CurrentRow.Cells[0].Value + "'";
            rs.Open(sql);

            if (rs.Next())
            {
                model.ID_Z = startindex++; // noch nicht gespeichert, also noch unbekannt
                model.ID_Gebaeude = (int)rs.Read("ID");
                model.ID_Projekt = m_ID_Projekt;
                model.Wohnflaeche = (double)rs.Read("Wohnflaeche_gesamt");
                model.Einheit = "Wohnfläche [m²]";
                model.Jahresnutzungsgrad = 1;
                model.DezentralWarmwasser = false;
                model.Gebaeudename = (string)dataGridView1.CurrentRow.Cells[0].Value;
                model.Gebaeudeart = (string)rs.Read("Typ");
                model.Beschreibung = (string)rs.Read("Beschreibung");
                model.Baualtersklasse = (string)rs.Read("Baualtersklasse");

                list_gebmodel.Add(model);

                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = (string)dataGridView1.CurrentRow.Cells[0].Value;
                lvitem.SubItems.Add(model.ID_Z.ToString());
                listView_Gebaeude.Items.Add(lvitem);


                listView_Gebaeude.Select();
                if (listView_Gebaeude.Items.Count > 0)
                {
                    listView_Gebaeude.SelectedItems.Clear();
                    listView_Gebaeude.Items[listView_Gebaeude.Items.Count - 1].Selected = true;
                }
            }
            rs.Close();
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Gebaeude.SelectedIndices;

            if (indexes.Count == 0) return;

            ListViewItem lvitem = listView_Gebaeude.Items[indexes[0]];

            // Eindeutige Zuordnung ueber ID_Z (im ListView-Item als SubItem gespeichert), NICHT ueber
            // die Stammdaten-ID: ID_Gebaeude ist pro Gebaeudetyp gleich und daher nicht eindeutig,
            // (die Stammdaten-ID war zudem nicht immer korrekt ermittelbar) -> Entfernen schlug fehl.
            int idZ;
            if (!Int32.TryParse(lvitem.SubItems.Count > 1 ? lvitem.SubItems[1].Text : "", out idZ)) return;

            for (int i = 0; i < list_gebmodel.Count; i++)
            {
                if (list_gebmodel[i].ID_Z == idZ)
                {
                    list_gebmodel.RemoveAt(i);
                    listView_Gebaeude.Items.Remove(lvitem);
                    break;
                }
            }

            dataGridView1.ClearSelection();
            if (list_gebmodel.Count == 0)
            {
                // Nur bei gefuelltem Katalog - Rows[0] warf bei leerem Grid.
                if (dataGridView1.Rows.Count == 0) return;

                dataGridView1.Rows[0].Selected = true;
                dataGridView1.Rows[0].Cells[0].Selected = true;

                // Definiere die Zeile und Spalte des zu klickenden Cells
                int rowIndex = 0;
                int columnIndex = 0;

                // Erstelle ein MouseEventArgs Objekt
                MouseEventArgs me = new MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 1, 100, 100, 0);

                // Erstelle ein DataGridViewCellMouseEventArgs Objekt
                DataGridViewCellMouseEventArgs dgvme = new DataGridViewCellMouseEventArgs(columnIndex, rowIndex, 100, 100, me);

                // Rufe den CellMouseClick-Ereignis-Handler auf
                // Ersetzen Sie dataGridView1_CellMouseClick durch den Namen Ihres tatsächlichen Event-Handlers
                listBox_Gebaeude_DB_SelectedIndexChanged(this.dataGridView1, dgvme);
            }
            else
            {
                listView_Gebaeude.Select();
                listView_Gebaeude.Items[0].Selected = true;
            }
        }

        private void comboBox_Gebäudeart_SelectedIndexChanged(object sender, EventArgs e)
        {
            GebaeudeStammCtrl ctrl_geb = new GebaeudeStammCtrl();
            DataGridView dgv = dataGridView1;
            int index = 0;

            dgv.Rows.Clear();

            if (comboBox_Gebäudeart.Text == "Alle" && comboBox_Baujahr.Text == "Alle")
            {
                ctrl_geb.ReadAll(szFilterOption);
            }
            else
            {
                if (comboBox_Gebäudeart.Text == "Alle")
                {
                    for (int i = 0; i < list_geb.Count; i++)
                    {
                        if (list_geb[i] == comboBox_Baujahr.Text) { index = i; break; }
                    }
                    ctrl_geb.ReadAll("Baualtersklasse='" + Convert.ToChar('A' + index) + "' and " + szFilterOption);
                }
                else
                {
                    for (int i = 0; i < list_geb.Count; i++)
                    {
                        if (list_geb[i] == comboBox_Baujahr.Text) { index = i; break; }
                    }
                    if (comboBox_Baujahr.Text == "Alle")
                    {
                        ctrl_geb.ReadAll("Gebaeudeart='" + comboBox_Gebäudeart.Text + "'");
                    }
                    else
                    {
                        ctrl_geb.ReadAll("Gebaeudeart='" + comboBox_Gebäudeart.Text + "' and Baualtersklasse='" + Convert.ToChar('A' + index) + "' and " + szFilterOption);
                    }
                }
            }
            for (int i = 0; i < ctrl_geb.rows; i++)
            {
                dgv.Rows.Add(ctrl_geb.items[i].Gebaeudename, ctrl_geb.items[i].Gebaeudeart + "\n" + ctrl_geb.items[i].Wohnflaeche_gesamt.ToString("F2")  + " [m²]");
                dgv.Rows[i].DividerHeight = 1;
            }

            ApplyGridFilter();
        }

        private void comboBox_Baujahr_SelectedIndexChanged(object sender, EventArgs e)
        {
            GebaeudeStammCtrl ctrl_geb = new GebaeudeStammCtrl();
            DataGridView dgv = dataGridView1;
            int index = 0;

            dgv.Rows.Clear();

            if (comboBox_Gebäudeart.Text == "Alle" && comboBox_Baujahr.Text == "Alle")
            {
                ctrl_geb.ReadAll(szFilterOption);
            }
            else
            {
                if (comboBox_Baujahr.Text == "Alle")
                {
                    ctrl_geb.ReadAll("Gebaeudeart='" + comboBox_Gebäudeart.Text + "' and " + szFilterOption);
                }
                else
                {
                    for (int i = 0; i < list_geb.Count; i++)
                    {
                        if (list_geb[i] == comboBox_Baujahr.Text) { index = i; break; }
                    }

                    if (comboBox_Gebäudeart.Text == "Alle")
                    {
                        ctrl_geb.ReadAll("Baualtersklasse='" + Convert.ToChar('A' + index) + "' and " + szFilterOption);
                    }
                    else
                    {
                        ctrl_geb.ReadAll("Gebaeudeart='" + comboBox_Gebäudeart.Text + "' and Baualtersklasse='" + Convert.ToChar('A' + index) + "' and " + szFilterOption);
                    }
                }
            }

            for (int i = 0; i < ctrl_geb.rows; i++)
            {
                dgv.Rows.Add(ctrl_geb.items[i].Gebaeudename, ctrl_geb.items[i].Gebaeudeart + "\n" + ctrl_geb.items[i].Wohnflaeche_gesamt.ToString("F2") + " [m²]");
                dgv.Rows[i].DividerHeight = 1;
            }

            ApplyGridFilter();
        }

        private void btn_Aendern_Click(object sender, EventArgs e)
        {
            if (textBox_Gebäudename.Text == "") return;

            Form_GebWohnflaeche frm = new Form_GebWohnflaeche();
            frm.ctrl.Gebaeudename = textBox_Gebäudename.Text;
            frm.ctrl.Beschreibung = textBox_Beschreibung.Text;
            frm.ctrl.Gebaeudeart = textBox_Gebaeudeart.Text;
            frm.ctrl.Baualtersklasse = textBox_Baujahr.Text;
            string Baualtersklasse = textBox_Baujahr.Text.Substring(0, 1);
            int index = (int)Baualtersklasse[0] -(int)'A';
            if (index < 0) index = 0;

            frm.ctrl.Baualtersklasse = list_geb[index];
            if (textBox_Wohnflaeche.Text != "")
            frm.ctrl.Wohnflaeche = double.Parse(textBox_Wohnflaeche.Text);
            frm.ctrl.Einheit = textBox_TypEinheit.Text;
            frm.ctrl.DezentralWarmwasser = checkBox_dezWarmwasser.Checked;
            if(textBox_Jahresnutzungsgrad.Text != "")
            frm.ctrl.Jahresnutzungsgrad = double.Parse(textBox_Jahresnutzungsgrad.Text);

            frm.SetControls();
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                for (int i = 0; i < list_gebmodel.Count; i++)
                {
                    if (list_gebmodel[i].ID_Z == zprojGeb_id)
                    {
                        Z_ProjGebModel model;
                        model = list_gebmodel[i];
                        model.ID_Z = zprojGeb_id;
                        model.ID_Gebaeude = list_gebmodel[i].ID_Gebaeude;
                        model.ID_Projekt = m_ID_Projekt;
                        model.Wohnflaeche = frm.ctrl.Wohnflaeche;
                        model.Einheit = frm.ctrl.Einheit;
                        model.Jahresnutzungsgrad = frm.ctrl.Jahresnutzungsgrad;
                        model.DezentralWarmwasser = frm.ctrl.DezentralWarmwasser;
                        list_gebmodel[i] = model;

                        textBox_Jahresnutzungsgrad.Text = model.Jahresnutzungsgrad.ToString();
                        textBox_TypEinheit.Text = model.Einheit;
                        textBox_Wohnflaeche.Text = model.Wohnflaeche.ToString();
                        checkBox_dezWarmwasser.Checked = model.DezentralWarmwasser;
                        break;
                    }
                }
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

        private void listView_Gebaeude_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Gebaeude.SelectedIndices;

            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_Gebaeude.Items[indexes[0]];
                textBox_Gebäudename.Text = lvitem.Text;
                zprojGeb_id = Int32.Parse(lvitem.SubItems[1].Text);
            }
            else
            {
                return;
            }

            WizardParent wizardparent = (WizardParent)getWizardPage();
            for (int i = 0; i < list_gebmodel.Count; i++)
            {
                if (list_gebmodel[i].Gebaeudename == textBox_Gebäudename.Text &&
                    list_gebmodel[i].ID_Z == zprojGeb_id)
                {
                    textBox_Jahresnutzungsgrad.Text = list_gebmodel[i].Jahresnutzungsgrad.ToString();
                    textBox_TypEinheit.Text = list_gebmodel[i].Einheit;
                    textBox_Wohnflaeche.Text = list_gebmodel[i].Wohnflaeche.ToString("F2");
                    checkBox_dezWarmwasser.Checked = list_gebmodel[i].DezentralWarmwasser;
                    textBox_Gebaeudeart.Text = list_gebmodel[i].Gebaeudeart;
                    textBox_Beschreibung.Text = list_gebmodel[i].Beschreibung;
                    textBox_Baujahr.Text = list_gebmodel[i].Baualtersklasse;    
                    break;
                }
            }
            
        }

        private void btn_GebAendern_DB_Click(object sender, EventArgs e)
        {
            using var frm = new Form_Gebaeude1();
            var dgv = dataGridView1;

            // Robust prüfen: CurrentRow statt CurrentCell benutzen und auf null prüfen
            if (dgv.CurrentRow == null || dgv.CurrentRow.Cells.Count == 0 || dgv.CurrentRow.Cells[0].Value == null)
            {
                MessageBox.Show("Gebäude in DB auswählen!");
                return;
            }

            frm.m_bNeu = false;
            frm.model.Gebaeudename = dgv.CurrentRow.Cells[0].Value as string ?? "";
            frm.SetControls();
            frm.ShowDialog();
            SetGebaeudeDB();
        }

        private void btn_Geb_Neu_DB_Click(object sender, EventArgs e)
        {
            Form_Gebaeude1 frm = new Form_Gebaeude1();

            frm.m_bNeu = true;
            frm.model.Gebaeudename = "";
            frm.SetControls();
            frm.ShowDialog();
            SetGebaeudeDB();
        }

        private void btn_GebLoeschen_DB_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            DataGridView dgv = dataGridView1;

            if (dgv.CurrentCell.RowIndex < 0)
            {
                MessageBox.Show("Gebäude auswählen!"); 
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Soll " + (string)dataGridView1.CurrentRow.Cells[0].Value + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            GebaeudeStammCtrl ctrl_del = new GebaeudeStammCtrl();
            if (!ctrl_del.Delete((string)dataGridView1.CurrentRow.Cells[0].Value)) return;
            dgv.Rows.RemoveAt(dgv.CurrentCell.RowIndex);
            MessageBox.Show("Gebäude gelöscht!"); 
        }

        private void btn_GebTypAendern_DB_Click(object sender, EventArgs e)
        {
            // iU9-W8.4: Blazor-Huelle statt Form_EingGebTyp.
            GebaeudetypHuelle.Oeffnen(this);
        }

        private void listBox_Gebaeude_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            GebaeudeStammCtrl ctrl = new GebaeudeStammCtrl();
            DataGridView dgv = dataGridView1;

            if (dgv.RowCount == 0) return;

            try
            {
                ctrl.ReadAll("Bezeichner='" + (string)dataGridView1.CurrentRow.Cells[0].Value + "'");
            
                textBox_Gebäudename.Text = (string)dataGridView1.CurrentRow.Cells[0].Value; 
                textBox_Jahresnutzungsgrad.Text = "";
                checkBox_dezWarmwasser.Checked = false;
           
                textBox_TypEinheit.Text = "Wohnfläche [m²]";
                textBox_Wohnflaeche.Text = ctrl.items[0].Wohnflaeche_gesamt.ToString();
                textBox_Gebaeudeart.Text = ctrl.items[0].Gebaeudeart;
                textBox_Beschreibung.Text = ctrl.items[0].Beschreibung;
            }
            catch (SystemException ex)
            {
                textBox_Wohnflaeche.Text = "";
                textBox_Gebaeudeart.Text = "";
                textBox_Beschreibung.Text = "";
                Console.Write(ex.Message); 
                return;
            }
        }

        private void Form_Gebaeude_Load(object sender, EventArgs e)
        {
            DataGridView dgv = dataGridView1;

            if (m_bAdmin)
            {
                listView_Gebaeude.Visible = false;
                btn_Entfernen.Visible = false;
                btn_Hinzu.Visible = false;
                label_ListProjektGebaeude.Visible = false;

                btn_Aendern.Visible = false;
                dgv.Left = groupBox1.Left;
                label_ListGebaeudeDB.Left = groupBox1.Left;
                pictureBox1.Visible = true;
                if (dgv.Rows.Count > 0) dgv.Rows[0].Selected = true;
            }
            else
            {
                dataGridView1.ClearSelection();

                if (listView_Gebaeude.Items.Count > 0)
                {
                    listView_Gebaeude.Select();
                    listView_Gebaeude.SelectedItems.Clear();
                    listView_Gebaeude.Items[0].Selected = true;
                }
            }
        }

        // Filtert dataGridView1 anhand des Suchfeldes. Platzhalter: * (beliebig viele Zeichen), ? (ein Zeichen).
        // Beispiel: "Haus*_1990*". Ohne Platzhalter wird als Teilstring-Suche interpretiert.
        private void ApplyGridFilter()
        {
            if (dataGridView1 == null) return;

            string muster = (textBox_Suche != null ? textBox_Suche.Text : "").Trim();

            // Aktuelle Zelle loesen, sonst kann die markierte Zeile nicht ausgeblendet werden.
            try { dataGridView1.CurrentCell = null; } catch { }

            if (muster.Length == 0)
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                    row.Visible = true;
                return;
            }

            // Wildcard-Muster in einen regulaeren Ausdruck uebersetzen.
            string pattern;
            if (muster.IndexOf('*') < 0 && muster.IndexOf('?') < 0)
                pattern = ".*" + System.Text.RegularExpressions.Regex.Escape(muster) + ".*";
            else
                pattern = "^" + System.Text.RegularExpressions.Regex.Escape(muster).Replace("\\*", ".*").Replace("\\?", ".") + "$";

            System.Text.RegularExpressions.Regex regex;
            try
            {
                regex = new System.Text.RegularExpressions.Regex(
                    pattern,
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Singleline);
            }
            catch
            {
                return; // ungueltiges Muster -> Anzeige unveraendert lassen
            }

            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                bool sichtbar = false;
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value == null) continue;
                    string zelltext = cell.Value.ToString().Replace("\r", " ").Replace("\n", " ");
                    if (regex.IsMatch(zelltext)) { sichtbar = true; break; }
                }
                row.Visible = sichtbar;
            }
        }

        private void textBox_Suche_TextChanged(object sender, EventArgs e)
        {
            ApplyGridFilter();
        }

        private void dataGridView1_Leave(object sender, EventArgs e)
        {
            //dataGridView1.ClearSelection();
        }
    }
}
