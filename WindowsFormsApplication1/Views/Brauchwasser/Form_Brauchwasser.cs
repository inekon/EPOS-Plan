using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Brauchwasser : Form
    {
        private BrauchwasserModel model = new BrauchwasserModel();
        public List<Z_ProjektBrauchwasserModel> list_pwmodel = new List<Z_ProjektBrauchwasserModel>();
        public int m_ID_Projekt = 0;
        private int startindex = 100000;
        private SimulationWaermebedarf simulation = new SimulationWaermebedarf();
        private string m_szProjekt;
        private int m_ListIndex = 0;
        private bool m_bWizard = false;

        public Form_Brauchwasser ()
        {
            InitializeComponent();
            dataGridView1.Rows.Clear();
            listView_Auswahl.Items.Clear();
            listView_Auswahl.View = View.Details;
            listView_Auswahl.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Auswahl.Columns[0].Width = listView_Auswahl.ClientRectangle.Width;

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
                HeaderText = "Typ",
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
        }

        private void SetDBList()
        {
            BrauchwasserStammCtrl ctrl_pw = new BrauchwasserStammCtrl();
            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            ctrl_pw.ReadAll();
            for (int i = 0; i < ctrl_pw.rows; i++)
            {
                dgv.Rows.Add(ctrl_pw.items[i].m_szBezeichner, ctrl_pw.items[i].m_szTyp);
                dgv.Rows[i].DividerHeight = 1;
            }
        }
        public void SetControls(string szProjekt, bool bWizard = false)
        {
            Z_ProjektBrauchwasserCtrl ctrl = new Z_ProjektBrauchwasserCtrl();
            BrauchwasserStammCtrl ctrl_pw = new BrauchwasserStammCtrl();
            Z_ProjektBrauchwasserModel model = new Z_ProjektBrauchwasserModel();

            if (bWizard)
            {
                btn_Abbrechen.Visible = false;
                btn_OK.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                m_bWizard = true;
            }

            m_szProjekt = szProjekt;

        
            listView_Auswahl.Items.Clear(); 
            for (int i = 0; i < list_pwmodel.Count; i++)
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = list_pwmodel[i].szBezeichner;
                lvitem.SubItems.Add(list_pwmodel[i].ID_Z.ToString());
                listView_Auswahl.Items.Add(lvitem);
            }
            btn_ErgebnisseVerbrauch.Enabled = false;

            if (listView_Auswahl.Items.Count > 0)
            {
                textBox_Summe.Text = BrauchwasserGesamt().ToString("F2");
            }

            dataGridView1.Select();
            dataGridView1.ClearSelection();
            listView_Auswahl.Select();
            if(listView_Auswahl.Items.Count > 0) listView_Auswahl.Items[0].Selected = true;
        }

        private void listBox_Prozess_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;
            textBox_Jahres_Verbrauch.Text = Prozesssumme(szName).ToString();
            SetProzessInfo(szName);
        }

        private void listView_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
                m_ListIndex = indexes[0];
                ListViewItem lvitem = listView_Auswahl.Items[indexes[0]];
                textBox_Jahres_Verbrauch.Text = list_pwmodel[m_ListIndex].Summe.ToString("F2");
                textBox_Summe.Text = BrauchwasserGesamt().ToString("F2");
                SetProzessInfo(lvitem.Text);
            }
            dataGridView1.ClearSelection();
        }
        private void SetProzessInfo(string szName)
        {
            BrauchwasserStammCtrl ctrl = new BrauchwasserStammCtrl();
            ctrl.ReadSingle(szName);

            if (ctrl.rows > 0)
            {
                textBox_Name.Text = szName;
                textBox_Beschreibung.Text = ctrl.m_szBeschreibung;
                textBox_Type.Text = ctrl.m_szTyp;  
            }
        }

        private double Prozesssumme(string szName)
        {
            BrauchwasserStammCtrl ctrl = new BrauchwasserStammCtrl();
            double summe = 0;
   
            ctrl.ReadSingle(szName);
            if (ctrl.rows > 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    summe += ctrl.m_Monat[i];
                }
            }
            return summe;  
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            if (dataGridView1.CurrentCell.RowIndex == -1) return;

            string sql = "SELECT * from Tab_Brauchwasser_STAMM where Bezeichner='" + (string)dataGridView1.CurrentRow.Cells[0].Value + "'";
            rs.Open(sql);

            if (rs.Next())
            {
                Z_ProjektBrauchwasserModel model = new Z_ProjektBrauchwasserModel();
                model.ID_Z = startindex++; // noch nicht gespeichert, also noch unbekannt
                model.ID_Brauchwasser = (int)rs.Read("ID");
                model.ID_Projekt = m_ID_Projekt;
                model.szBezeichner = (string)dataGridView1.CurrentRow.Cells[0].Value;
                model.Summe = Prozesssumme(model.szBezeichner);
         
                list_pwmodel.Add(model);

                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = (string)dataGridView1.CurrentRow.Cells[0].Value;
                lvitem.SubItems.Add(model.ID_Z.ToString());
                listView_Auswahl.Items.Add(lvitem);
                listView_Auswahl.Select();
                listView_Auswahl.SelectedItems.Clear();
                listView_Auswahl.Items[listView_Auswahl.Items.Count-1].Selected = true;  
            }
            rs.Close();

            textBox_Summe.Text = BrauchwasserGesamt().ToString("F2");
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
       
                ListViewItem lvitem = listView_Auswahl.Items[indexes[0]];
    
                for (int i = 0; i < list_pwmodel.Count; i++)
                {
                    if (list_pwmodel[i].szBezeichner == lvitem.Text && list_pwmodel[i].ID_Z.ToString()  == lvitem.SubItems[1].Text  )
                    {
                        list_pwmodel.RemoveAt(i);
                        m_ListIndex -= 1;
                        if(m_ListIndex < 0) m_ListIndex = 0;
                        listView_Auswahl.Items[indexes[0]].Remove();
                        textBox_Summe.Text = BrauchwasserGesamt().ToString("F2");
                        break;
                    }
                }
                if (list_pwmodel.Count == 0)
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
                    listBox_Prozess_DB_SelectedIndexChanged(this.dataGridView1, dgvme);
                }
                else
                {
                    listView_Auswahl.Select(); 
                    listView_Auswahl.Items[0].Selected = true;
                }
            }
            
        }

        private double BrauchwasserGesamt()
        {
            double summe = 0;

            for (int i=0; i<listView_Auswahl.Items.Count; i++)
            {
                summe += list_pwmodel[i].Summe;
            }
            return summe;
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

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            simulation.m_ID_Projekt = m_ID_Projekt;

            if (textBox_Name.Text == "")
            {
                MessageBox.Show("Bitte einen Eintrag aus der Liste auswählen!");
                return;
            }
    
            // ZahlParsen statt double.Parse: Seit die Pruefung nicht mehr im TextChanged
            // sitzt, kann im Feld auch ein ungueltiger Text stehen - double.Parse haette
            // hier eine FormatException geworfen. Ungueltig heisst jetzt schlicht: nicht
            // sichern, die Vorschau rechnet mit dem zuletzt gespeicherten Wert.
            double dSumme;
            if (m_bWizard && Program.ZahlParsen(textBox_Verbrauch.Text, out dSumme)) // nur im Wizard Wert sofort speichern, wegen Simulation
            {
                Z_ProjektBrauchwasserCtrl ctrl = new Z_ProjektBrauchwasserCtrl();
                ctrl.UpdateSumme(dSumme, textBox_Name.Text, m_ID_Projekt);
            }

            List<string> list = new List<string>();
            //list = listView_Auswahl.Items.Cast<ListViewItem>().Select(item => item.Text).ToList();
            list.Add(textBox_Name.Text);

            simulation.Brauchwasserwaerme_berechnen(list);
            //simulation.Waermebedarf_Brauchwasser = simulation.com.I_vector_summe(simulation.brauchwasserwerte);
            simulation.Waermebedarf_Brauchwasser = simulation.brauchwasserwerte.Sum();
           
            //simulation.com.I_monats_summe(simulation.brauchwasserwerte, simulation.Waermebedarf_Brauchwasser_Monat, simulation.mo_anfang, simulation.mo_ende);
            WPPlan.Core.BhkwPlan.MonatsSumme(simulation.brauchwasserwerte, simulation.Waermebedarf_Brauchwasser_Monat, simulation.mo_anfang, simulation.mo_ende);

            Form_ErgBrauchwasserwaerme frm = new Form_ErgBrauchwasserwaerme();
            frm.Text = frm.Text + " - " + textBox_Name.Text;   
            frm.Init(simulation);
            frm.SetPage(2); 
            frm.ShowDialog();
            btn_ErgebnisseVerbrauch.Enabled = true;
        }

        private void btn_ErgebnisseVerbrauch_Click(object sender, EventArgs e)
        {
            Form_ErgBrauchwasserwaerme frm = new Form_ErgBrauchwasserwaerme();
            frm.Init(simulation);
            frm.SetPage(2);
            frm.ShowDialog(); 
        }

        private void btn_Prozess_DBedit_Click(object sender, EventArgs e)
        {
            Form_EingDBBrauchwasser frm = new Form_EingDBBrauchwasser();
            frm.m_szBezeichner = textBox_Name.Text;
            frm.m_szBeschreibung = textBox_Beschreibung.Text;
            frm.m_szBrauchwassertyp = textBox_Type.Text;
            frm.mode = "Bearbeiten";
            frm.SetControls();
            frm.ShowDialog();
            SetControls(m_szProjekt);
            SetDBList();
        }

        private void btn_Prozess_DBneu_Click(object sender, EventArgs e)
        {
            Form_EingDBBrauchwasser frm = new Form_EingDBBrauchwasser();
            Form_Sp_ItemNeu frm_item = new Form_Sp_ItemNeu();

            Point p1 = btn_Prozess_DBneu.Location;
            p1 = this.PointToScreen(p1);
            frm_item.Location = p1;

            if (frm_item.ShowDialog() == DialogResult.OK)
            {
                frm.m_szBezeichner = frm_item.m_szName;
                frm.mode = "Neu";
                frm.SetControls();
                frm.Location = p1;
                frm.ShowDialog();
                SetControls(m_szProjekt);
                SetDBList();
            }
        }

        private void btn_Prozess_loeschen_Click(object sender, EventArgs e)
        {
            DataGridView dgv = dataGridView1;

            if (dgv.CurrentCell.RowIndex < 0)
            {
                MessageBox.Show("Prozesswärme auswählen!");
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Soll " + (string)dataGridView1.CurrentRow.Cells[0].Value + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            BrauchwasserStammCtrl ctrl_del = new BrauchwasserStammCtrl();
            // Delete prueft selbst auf ReadOnly und meldet ggf.
            if (!ctrl_del.Delete((string)dataGridView1.CurrentRow.Cells[0].Value)) return;
            dgv.Rows.RemoveAt(dgv.CurrentCell.RowIndex);
        }

        private void btn_ProzTypeDBedit_Click(object sender, EventArgs e)
        {
            Form_EingBrauchwasserTyp frm = new Form_EingBrauchwasserTyp();
            frm.SetControls();
            frm.ShowDialog(); 
        }

        private void btn_neuerWert_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Auswahl.SelectedIndices;
            if (indexes.Count == 0 || textBox_Verbrauch.Text.Trim() == "")
            {
                MessageBox.Show("Bitte einen Eintrag aus der Liste auswählen und einen Wert eingeben!");
                return;
            }

            // Pruefung beim Aktionsknopf statt im TextChanged (Muster
            // ProjektPuffer.TemperaturenPruefen): TryParse, sprechende Meldung, Feld
            // markieren, Dialog bleibt offen. double.Parse() stand hier ungesichert und
            // haette nach dem Wegfall des Undo() eine FormatException geworfen.
            double dVerbrauch;
            if (!Program.ZahlParsen(textBox_Verbrauch.Text, out dVerbrauch) || dVerbrauch < 0)
            {
                MessageBox.Show("Eingaben überprüfen: \"" + textBox_Verbrauch.Text + "\"" + Environment.NewLine +
                                "Bitte den Jahresverbrauch als Zahl in MWh eingeben, z. B. 12,5.",
                                "Ungültige Eingabe", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBox_Verbrauch.Focus();
                textBox_Verbrauch.SelectAll();
                return;
            }

            list_pwmodel[indexes[0]].Summe = dVerbrauch;
            textBox_Jahres_Verbrauch.Text = dVerbrauch.ToString("F2");
            textBox_Summe.Text = BrauchwasserGesamt().ToString("F2") ;
            pictureBox1.Visible = true;
            pictureBox1.Refresh();
            Thread.Sleep(500);
            pictureBox1.Visible = false;
        }
 
        private void Form_Brauchwasser_Load(object sender, EventArgs e)
        {
            SetDBList();
            dataGridView1.ClearSelection();

            if (listView_Auswahl.Items.Count > 0)
            {
                listView_Auswahl.Select();
                listView_Auswahl.SelectedItems.Clear();
                listView_Auswahl.Items[0].Selected = true;
            }
        }

        /// <summary>
        /// Stiller Hinweis statt fokushaltender Pruefung.
        ///
        /// Die alte Fassung meldete jede Zwischeneingabe modal und nahm sie mit
        /// tb.Undo() zurueck. Undo() loest TextChanged erneut aus und schaltet dabei
        /// zwischen Rueckgaengig und Wiederherstellen um: War das Feld vorher leer
        /// (also ebenfalls keine Zahl), pendelte der Text zwischen Fehleingabe und
        /// Leerstand, die Meldung kam nach jedem OK sofort zurueck und der Dialog war
        /// gefangen. Geprueft wird jetzt erst beim Uebernehmen (btn_neuerWert_Click);
        /// hier bleibt nur die Feldfarbe als Hinweis.
        /// </summary>
        private void textBox_Verbrauch_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb == null) return;

            double wert;
            bool bOk = tb.Text.Trim().Length == 0 || Program.ZahlParsen(tb.Text, out wert);
            tb.BackColor = bOk ? SystemColors.Window : Color.FromArgb(255, 235, 235);
        }
    }
}
