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
    partial class Form_Prozesswaerme : Form
    {
        private ProzesswaermeModel model = new ProzesswaermeModel();
        private ProzesswaermeCtrl ctrl = new ProzesswaermeCtrl();
        public List<Z_ProjektProzesswaermeModel> list_pwmodel = new List<Z_ProjektProzesswaermeModel>();
        public int m_ID_Projekt = 0;
        private int startindex = 100000;
        private SimulationGebaeude simulation = new SimulationGebaeude();
    
        public Form_Prozesswaerme()
        {
            InitializeComponent();
            listView_Prozess_Auswahl.View = View.Details;
            listView_Prozess_Auswahl.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Prozess_Auswahl.Columns.Add("ID", -2, HorizontalAlignment.Left);
            listView_Prozess_Auswahl.Columns[0].Width = listView_Prozess_Auswahl.ClientRectangle.Width;
        }

        public void SetControls(string szProjekt)
        {
            Z_ProjektProzesswaermeCtrl ctrl = new Z_ProjektProzesswaermeCtrl();
            ProzesswaermeCtrl ctrl_pw = new ProzesswaermeCtrl();
            Z_ProjektProzesswaermeModel model = new Z_ProjektProzesswaermeModel();

            listView_Prozess_Auswahl.Items.Clear();
            for (int i = 0; i < list_pwmodel.Count; i++)
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = list_pwmodel[i].szProzessname;
                lvitem.SubItems.Add(list_pwmodel[i].ID_Z.ToString());
                listView_Prozess_Auswahl.Items.Add(lvitem);
            }
            btn_ErgebnisseVerbrauch.Enabled = false;

            listBox_Prozess_DB.Items.Clear();
            ctrl_pw.ReadAll();
            for (int i = 0; i < ctrl_pw.rows; i++)
            {
                listBox_Prozess_DB.Items.Add(ctrl_pw.items[i].m_szProzessname);
            }
            if (listBox_Prozess_DB.Items.Count > 0) listBox_Prozess_DB.SelectedIndex = 0;

            textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2");
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
            ListView.SelectedIndexCollection indexes = listView_Prozess_Auswahl.SelectedIndices;

            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_Prozess_Auswahl.Items[indexes[0]];
                textBox_Jahres_Verbrauch.Text = Prozesssumme(lvitem.Text).ToString("F2");
                SetProzessInfo(lvitem.Text);
            }                                                  
        }

        private void btn__Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            
            if (listBox_Prozess_DB.Text == "") return;

            string sql = "SELECT * from Tab_Prozesswaerme where Prozessname='" + listBox_Prozess_DB.Text + "'";
            rs.Open(sql);

            if (rs.Next())
            {
                Z_ProjektProzesswaermeModel model = new Z_ProjektProzesswaermeModel();
                model.ID_Z = startindex++; // noch nicht gespeichert, also noch unbekannt
                model.ID_Prozesswaerme = (int)rs.Read("ID");
                model.ID_Projekt = m_ID_Projekt;
                model.szProzessname = listBox_Prozess_DB.Text;
         
                list_pwmodel.Add(model);

                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = listBox_Prozess_DB.Text;
                lvitem.SubItems.Add(model.ID_Z.ToString());
                listView_Prozess_Auswahl.Items.Add(lvitem);
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
                        listView_Prozess_Auswahl.Items[indexes[0]].Remove();
                        textBox_SummeProzesswaerme.Text = ProzesssummeGesamt().ToString("F2");
                        break;
                    }
                }
            }
            
        }

        private double ProzesssummeGesamt()
        {
            double summe = 0;

            for (int i = 0; i < listView_Prozess_Auswahl.Items.Count; i++)
            {
                summe += Prozesssumme(listView_Prozess_Auswahl.Items[i].Text);
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
            simulation.Prozesswaerme_berechnen();
            simulation.Waermebedarf_Prozess = simulation.com.I_vector_summe(simulation.prozesswerte);
            simulation.com.I_monats_summe(simulation.prozesswerte, simulation.Waermebedarf_monat, simulation.mo_anfang, simulation.mo_ende);

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

  
    }
}