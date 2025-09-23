using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace WindowsFormsApplication1
{
    partial class Form_Waermebedarf : Form
    {
        public int m_ID_Projekt = 0;
        public string m_szProjekt = "";
        public DialogResult result = DialogResult.Cancel;
        public List<Z_ProjWaermebedarfModel> DateiListe = new List<Z_ProjWaermebedarfModel>();
        public bool m_bAdmin = false;
        int startindex = 100000;

        public Form_Waermebedarf()
        {
            InitializeComponent();
 
            WaermebedarfCtrl ctrl = new WaermebedarfCtrl();
            ctrl.ReadAll(); 
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_Extern.Items.Add(ctrl.items[i].m_szBezeichner);
            }

        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            result = DialogResult.OK;  
            Close();
        }

        public void SetControls(string szProjekt)
        {
            Z_ProjektGebGanglinieCtrl ctrl = new Z_ProjektGebGanglinieCtrl();
            ctrl.ReadAll("select * from Z_ProjektWaermebedarf where ID_Projekt=" + m_ID_Projekt);

            listBox_Auswahl.Items.Clear();
 
            for (int i = 0; i < DateiListe.Count; i++)
            {
                Z_ProjWaermebedarfModel model = new Z_ProjWaermebedarfModel();

                model.m_ID_Projekt = DateiListe[i].m_ID_Projekt;
                model.m_ID_Z = DateiListe[i].m_ID_Z;
                model.m_szBezeichner = DateiListe[i].m_szBezeichner;
                model.m_ID_Ganglinie = DateiListe[i].m_ID_Ganglinie;
                listBox_Auswahl.Items.Add(model.m_szBezeichner);
            }

            if (m_bAdmin)
            {
                listBox_Auswahl.Enabled = false;
                btn_Hinzufuegen.Enabled = false;
                btn_Entfernen.Enabled = false;
            }
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel;
            Close();
        }

        private void btn_Hinzufuegen_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            
            if (listBox_Extern.Text == "") return;

            string sql = "SELECT * from Tab_Waermebedarf where Bezeichner='" + listBox_Extern.Text + "'";
            rs.Open(sql);

            if (rs.Next())
            {
                Z_ProjWaermebedarfModel model = new Z_ProjWaermebedarfModel();
                model.m_ID_Z = startindex++; // noch nicht gespeichert, also noch unbekannt
                model.m_ID_Ganglinie = (int)rs.Read("ID");
                model.m_ID_Projekt = m_ID_Projekt;
                model.m_szBezeichner = listBox_Extern.Text;
         
                DateiListe.Add(model);
                listBox_Auswahl.Items.Add(listBox_Extern.Text);
            }
            rs.Close();
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            WaermebedarfModel model = new WaermebedarfModel();

            if (listBox_Auswahl.SelectedIndex >= 0)
            {
                for (int i = 0; i < DateiListe.Count; i++)
                {
                    if (DateiListe[i].m_szBezeichner == listBox_Auswahl.Text)
                    {
                        DateiListe.RemoveAt(i);
                        listBox_Auswahl.Items.RemoveAt(listBox_Auswahl.SelectedIndex);
                        break;
                    }
                }
            }
        }

        private void listBox_Extern_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox_Name.Text = listBox_Extern.Text;
         }

        private void listBox_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            textBox_Name.Text = listBox_Auswahl.Text;
        }

    }
}