using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    partial class Form_Prozesswaerme : Form
    {
        private ProzesswaermeModel model = new ProzesswaermeModel();
        private ProzesswaermeCtrl ctrl = new ProzesswaermeCtrl();
        public List<Z_ProjektProzesswaermeModel> list_pwmodel = new List<Z_ProjektProzesswaermeModel>();
        public int m_ID_Projekt = 0;
        private int startindex = 100000;
        private SimulationWaermebedarf simulation = new SimulationWaermebedarf();
        private string m_szProjekt;
        private int m_ListIndex = 0;
    
        public Form_Prozesswaerme()
        {
            InitializeComponent();
            dataGridView1.Rows.Clear();
            listView_Prozess_Auswahl.Items.Clear();
            listView_Prozess_Auswahl.View = View.Details;
            listView_Prozess_Auswahl.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Prozess_Auswahl.Columns[0].Width = listView_Prozess_Auswahl.ClientRectangle.Width;

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
            dgv.Columns[1].DefaultCellStyle.BackColor = Color.GreenYellow;
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(255, 215, 159, 57);
        }

        private void SetDBList()
        {
            ProzesswaermeCtrl ctrl_pw = new ProzesswaermeCtrl();
            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            ctrl_pw.ReadAll();
            for (int i = 0; i < ctrl_pw.rows; i++)
            {
                dgv.Rows.Add(ctrl_pw.items[i].m_szProzessname, ctrl_pw.items[i].m_szTyp);
                dgv.Rows[i].DividerHeight = 1;
            }
        }

        public void SetControls(string szProjekt, bool bWizard = false)
        {
            Z_ProjektProzesswaermeCtrl ctrl = new Z_ProjektProzesswaermeCtrl();
            ProzesswaermeCtrl ctrl_pw = new ProzesswaermeCtrl();
            Z_ProjektProzesswaermeModel model = new Z_ProjektProzesswaermeModel();

            if (bWizard)
            {
                btn_Abbrechen.Visible = false;
                btn_OK.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
            }

            m_szProjekt = szProjekt;

            SetDBList();
       
            listView_Prozess_Auswahl.Items.Clear(); 
            for (int i = 0; i < list_pwmodel.Count; i++)
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = list_pwmodel[i].szProzessname;
                lvitem.SubItems.Add(list_pwmodel[i].ID_Z.ToString());
                listView_Prozess_Auswahl.Items.Add(lvitem);
            }
            btn_ErgebnisseVerbrauch.Enabled = false;

            if (listView_Prozess_Auswahl.Items.Count > 0)
            {
                textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2");
            }

            dataGridView1.Select();
            dataGridView1.ClearSelection();
            listView_Prozess_Auswahl.Select();
            if(listView_Prozess_Auswahl.Items.Count > 0) listView_Prozess_Auswahl.Items[0].Selected = true;
        }

        private void listBox_Prozess_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;
            textBox_Jahres_Verbrauch.Text = Prozesssumme(szName).ToString();
            SetProzessInfo(szName);
        }

        private void listView_Prozess_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Prozess_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
                m_ListIndex = indexes[0];
                ListViewItem lvitem = listView_Prozess_Auswahl.Items[indexes[0]];
                textBox_Jahres_Verbrauch.Text = list_pwmodel[m_ListIndex].Summe.ToString("F2");
                textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2");
                SetProzessInfo(lvitem.Text);
            }
            dataGridView1.ClearSelection();
        }
        private void SetProzessInfo(string szName)
        {
            ProzesswaermeCtrl ctrl = new ProzesswaermeCtrl();
            ctrl.ReadSingle(szName);

            if (ctrl.rows > 0)
            {
                textBox_Prozess_Name.Text = szName;
                textBox_Beschreibung.Text = ctrl.m_szBeschreibung;
                textBox_Prozess_Type.Text = ctrl.m_szTyp;  
            }
        }

        private double Prozesssumme(string szName)
        {
            ProzesswaermeCtrl ctrl = new ProzesswaermeCtrl();
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

            string sql = "SELECT * from Tab_Prozesswaerme where Prozessname='" + (string)dataGridView1.CurrentRow.Cells[0].Value + "'";
            rs.Open(sql);

            if (rs.Next())
            {
                Z_ProjektProzesswaermeModel model = new Z_ProjektProzesswaermeModel();
                model.ID_Z = startindex++; // noch nicht gespeichert, also noch unbekannt
                model.ID_Prozesswaerme = (int)rs.Read("ID");
                model.ID_Projekt = m_ID_Projekt;
                model.szProzessname = (string)dataGridView1.CurrentRow.Cells[0].Value;
                model.Summe = Prozesssumme(model.szProzessname);
         
                list_pwmodel.Add(model);

                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = (string)dataGridView1.CurrentRow.Cells[0].Value;
                lvitem.SubItems.Add(model.ID_Z.ToString());
                listView_Prozess_Auswahl.Items.Add(lvitem);
                listView_Prozess_Auswahl.Select();
                listView_Prozess_Auswahl.SelectedItems.Clear();
                listView_Prozess_Auswahl.Items[listView_Prozess_Auswahl.Items.Count-1].Selected = true;  
            }
            rs.Close();

            textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2");
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Prozess_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
       
                ListViewItem lvitem = listView_Prozess_Auswahl.Items[indexes[0]];
    
                for (int i = 0; i < list_pwmodel.Count; i++)
                {
                    if (list_pwmodel[i].szProzessname == lvitem.Text && list_pwmodel[i].ID_Z.ToString()  == lvitem.SubItems[1].Text  )
                    {
                        list_pwmodel.RemoveAt(i);
                        m_ListIndex -= 1;
                        if(m_ListIndex < 0) m_ListIndex = 0;
                        listView_Prozess_Auswahl.Items[indexes[0]].Remove();
                        textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2");
                        break;
                    }
                }
                if (list_pwmodel.Count == 0)
                {
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
                    listView_Prozess_Auswahl.Select(); 
                    listView_Prozess_Auswahl.Items[0].Selected = true;
                }
            }
            
        }

        private double ProzesssummeGesamt()
        {
            double summe = 0;

            for (int i=0; i<listView_Prozess_Auswahl.Items.Count; i++)
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
  
            List<string> list;
            list = listView_Prozess_Auswahl.Items.Cast<ListViewItem>().Select(item => item.Text).ToList();

            simulation.Prozesswaerme_berechnen(list);
            simulation.Waermebedarf_Prozess = simulation.com.I_vector_summe(simulation.prozesswerte);
            simulation.com.I_monats_summe(simulation.prozesswerte, simulation.Waermebedarf_Prozess_Monat, simulation.mo_anfang, simulation.mo_ende);

            Form_ErgProzesswaerme frm = new Form_ErgProzesswaerme();
            frm.Init(simulation);
            frm.SetPage(1); 
            frm.ShowDialog();
            btn_ErgebnisseVerbrauch.Enabled = true;
        }

        private void btn_ErgebnisseVerbrauch_Click(object sender, EventArgs e)
        {
            Form_ErgProzesswaerme frm = new Form_ErgProzesswaerme();
            frm.Init(simulation);
            frm.SetPage(1);
            frm.ShowDialog(); 
        }

        private void btn_Prozess_DBedit_Click(object sender, EventArgs e)
        {
            Form_EingDBProzess frm = new Form_EingDBProzess();
            frm.m_szProzessname = textBox_Prozess_Name.Text;
            frm.m_szBeschreibung = textBox_Beschreibung.Text;
            frm.m_szProzesstyp = textBox_Prozess_Type.Text;
            frm.mode = "Bearbeiten";
            frm.SetControls();
            frm.ShowDialog();
            SetControls(m_szProjekt);
            SetDBList();
        }

        private void btn_Prozess_DBneu_Click(object sender, EventArgs e)
        {
            Form_EingDBProzess frm = new Form_EingDBProzess();
            Form_Sp_ItemNeu frm_item = new Form_Sp_ItemNeu();

            Point p1 = btn_Prozess_DBneu.Location;
            p1 = this.PointToScreen(p1);
            frm_item.Location = p1;
            frm_item.ShowDialog();

            if (frm_item.result == DialogResult.OK)
            {
                frm.m_szProzessname = frm_item.m_szName;
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

            OdbcCommand DBCommand = Program.DBConnection.CreateCommand();
            try
            {
                DBCommand.CommandText = "DELETE Prozessname FROM Tab_Prozesswaerme WHERE Prozessname='" + (string)dataGridView1.CurrentRow.Cells[0].Value + "'";
                DBCommand.ExecuteNonQuery();
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
            }
            dgv.Rows.RemoveAt(dgv.CurrentCell.RowIndex);
        }

        private void btn_ProzTypeDBedit_Click(object sender, EventArgs e)
        {
            Form_EingProzTyp frm = new Form_EingProzTyp();
            frm.SetControls();
            frm.ShowDialog(); 
        }

        private void btn_neuerWert_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Prozess_Auswahl.SelectedIndices;
            if (indexes.Count == 0) return;

            list_pwmodel[indexes[0]].Summe = double.Parse(textBox_Verbrauch.Text);
            textBox_Jahres_Verbrauch.Text = textBox_Verbrauch.Text;
            textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2") ; 
        }
 
        private void Form_Prozesswaerme_Load(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();

            if (listView_Prozess_Auswahl.Items.Count > 0)
            {
                listView_Prozess_Auswahl.Select();
                listView_Prozess_Auswahl.SelectedItems.Clear();
                listView_Prozess_Auswahl.Items[0].Selected = true;
            }

        }

        private void textBox_Verbrauch_TextChanged(object sender, EventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }
    }
}