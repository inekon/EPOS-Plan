using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    partial class Form_Stromverbraucher : Form
    {
        private StromverbraucherModel model = new StromverbraucherModel();
        private StromverbraucherCtrl ctrl = new StromverbraucherCtrl();
        public List<Z_ProjektStromverbraucherModel> list_sbmodel = new List<Z_ProjektStromverbraucherModel>();
        public int m_ID_Projekt = 0;
        private int startindex = 100000;
        private SimulationStrombedarf simulation = new SimulationStrombedarf();
        private string m_szProjekt;
    
        public Form_Stromverbraucher()
        {
            InitializeComponent();
            listView_Strom_Auswahl.View = View.Details;
            listView_Strom_Auswahl.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Strom_Auswahl.Columns.Add("ID", -2, HorizontalAlignment.Left);
            listView_Strom_Auswahl.Columns[0].Width = listView_Strom_Auswahl.ClientRectangle.Width;
        }

        public void SetControls(string szProjekt)
        {
            Z_ProjektStromverbraucherCtrl ctrl = new Z_ProjektStromverbraucherCtrl();
            StromverbraucherCtrl ctrl_pw = new StromverbraucherCtrl();
            Z_ProjektStromverbraucherModel model = new Z_ProjektStromverbraucherModel();

            m_szProjekt = szProjekt;
            listView_Strom_Auswahl.Items.Clear();
            for (int i = 0; i < list_sbmodel.Count; i++)
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = list_sbmodel[i].m_szVerbraucher;
                lvitem.SubItems.Add(list_sbmodel[i].m_ID_Z.ToString());
                listView_Strom_Auswahl.Items.Add(lvitem);
            }
            btn_ErgebnisseVerbrauch.Enabled = false;

            listBox_Strom_DB.Items.Clear();
            ctrl_pw.ReadAll();
            for (int i = 0; i < ctrl_pw.rows; i++)
            {
                listBox_Strom_DB.Items.Add(ctrl_pw.items[i].m_szBezeichner);
            }
            if (listBox_Strom_DB.Items.Count > 0) listBox_Strom_DB.SelectedIndex = 0;

            textBox_StromSumme.Text = ProzesssummeGesamt().ToString("F2");
        }

        private void listBox_Prozess_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListBox list = (ListBox)sender;
            string szName = list.Text;
            textBox_Jahres_Verbrauch.Text = Prozesssumme(szName).ToString();
            SetProzessInfo(szName);
        }

        private void SetProzessInfo(string szName)
        {
            StromverbraucherCtrl ctrl = new StromverbraucherCtrl();
            ctrl.ReadSingle(szName);

            if (ctrl.rows > 0)
            {
                textBox_Stromname.Text = szName;
                textBox_Beschreibung.Text = ctrl.m_szBeschreibung;
                textBox_Stromtyp.Text = ctrl.m_szTyp;  
            }
        }

        private double Prozesssumme(string szName)
        {
            StromverbraucherCtrl ctrl = new StromverbraucherCtrl();
            ctrl.ReadSingle(szName);

            double summe = 0;
            if (ctrl.rows > 0)
            {
                for (int i = 0; i < 12; i++)
                {
                    summe += ctrl.m_Monat[i];
                }
            }
            return summe;  
        }

        private void listView_Prozess_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Strom_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_Strom_Auswahl.Items[indexes[0]];
                textBox_Jahres_Verbrauch.Text = Prozesssumme(lvitem.Text).ToString("F2");
                SetProzessInfo(lvitem.Text);
            }                                                  
        }

        private void btn__Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            
            if (listBox_Strom_DB.Text == "") return;

            string sql = "SELECT * from Tab_Stromverbraucher where Bezeichner='" + listBox_Strom_DB.Text + "'";
            rs.Open(sql);

            if (rs.Next())
            {
                Z_ProjektStromverbraucherModel model = new Z_ProjektStromverbraucherModel();
                model.m_ID_Z = startindex++; // noch nicht gespeichert, also noch unbekannt
                model.m_ID_Stromverbraucher = (int)rs.Read("ID");
                model.m_ID_Projekt = m_ID_Projekt;
                model.m_szVerbraucher = listBox_Strom_DB.Text;
         
                list_sbmodel.Add(model);

                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = listBox_Strom_DB.Text;
                lvitem.SubItems.Add(model.m_ID_Z.ToString());
                listView_Strom_Auswahl.Items.Add(lvitem);
            }
            rs.Close();

            textBox_StromSumme.Text = ProzesssummeGesamt().ToString("F2");
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Strom_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
       
                ListViewItem lvitem = listView_Strom_Auswahl.Items[indexes[0]];
    
                for (int i = 0; i < list_sbmodel.Count; i++)
                {
                    if (list_sbmodel[i].m_szVerbraucher == lvitem.Text && list_sbmodel[i].m_ID_Z.ToString()  == lvitem.SubItems[1].Text  )
                    {
                        list_sbmodel.RemoveAt(i);
                        listView_Strom_Auswahl.Items[indexes[0]].Remove();
                        textBox_StromSumme.Text = ProzesssummeGesamt().ToString("F2");
                        break;
                    }
                }
            }
            
        }

        private double ProzesssummeGesamt()
        {
            double summe = 0;

            for (int i = 0; i < listView_Strom_Auswahl.Items.Count; i++)
            {
                summe += Prozesssumme(listView_Strom_Auswahl.Items[i].Text);
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
            list = listView_Strom_Auswahl.Items.Cast<ListViewItem>().Select(item => item.Text).ToList();
            
            simulation.Strombedarf_berechnen(list);
            simulation.Strombedarf_gesamt = simulation.com.I_vector_summe(simulation.prozesswerte);
            simulation.com.I_monats_summe(simulation.prozesswerte, simulation.Strombedarf_monat, simulation.mo_anfang, simulation.mo_ende);

            Form_ErgStromverbraucher frm = new Form_ErgStromverbraucher();
            frm.Init(simulation);
            frm.SetPage(1); 
            frm.ShowDialog();
            btn_ErgebnisseVerbrauch.Enabled = true;
        }

        private void btn_ErgebnisseVerbrauch_Click(object sender, EventArgs e)
        {
            Form_ErgStromverbraucher frm = new Form_ErgStromverbraucher();
            frm.Init(simulation);
            frm.SetPage(1);
            frm.ShowDialog(); 
        }

        private void btn_Strom_DBedit_Click(object sender, EventArgs e)
        {
            Form_EingDBStromverbraucher frm = new Form_EingDBStromverbraucher();
            frm.m_szStromname = textBox_Stromname.Text;
            frm.m_szBeschreibung = textBox_Beschreibung.Text;
            frm.m_szStromtyp = textBox_Stromtyp.Text;
            frm.mode = "Bearbeiten";
            frm.SetControls();
            frm.ShowDialog();
            SetControls(m_szProjekt); 
        }

        private void btn_Strom_DBneu_Click(object sender, EventArgs e)
        {
            Form_EingDBStromverbraucher frm = new Form_EingDBStromverbraucher();
            Form_Sp_ItemNeu frm_item = new Form_Sp_ItemNeu();
            Point p1 = btn_Strom_DBneu.Location;
            p1 = this.PointToScreen(p1);
            frm_item.Location = p1;
            frm_item.ShowDialog();

            if (frm_item.result == DialogResult.OK)
            {
                frm.m_szStromname = frm_item.m_szName;
                frm.mode = "Neu";
                frm.SetControls();
                frm.ShowDialog();
                SetControls(m_szProjekt);
            }
        }

        private void btn_Strom_loeschen_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_Strom_DB.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            OdbcCommand DBCommand = Program.DBConnection.CreateCommand();
            try
            {
                DBCommand.CommandText = "DELETE * FROM Tab_Stromverbraucher WHERE Bezeichner='" + listBox_Strom_DB.Text + "'";
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
            listBox_Strom_DB.Items.Remove(listBox_Strom_DB.Text);    
        }

        private void btn_StromtypDBedit_Click(object sender, EventArgs e)
        {
            Form_EingStromTyp frm = new Form_EingStromTyp();
            frm.SetControls();
            frm.ShowDialog(); 
        }

        private void btn_neuerWert_Click(object sender, EventArgs e)
        {

        }
    }
}