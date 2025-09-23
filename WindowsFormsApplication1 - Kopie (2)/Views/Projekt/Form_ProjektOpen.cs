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
    public partial class Form_ProjektOpen : Form
    {
        public string m_szProjekt;
        public int ID_Klimaregion;
        public int m_ID_Projekt;
        public string m_szKlimaregion;
        public string m_szKunde;
        public string m_szBearbeiter;
        public DateTime m_Datum;

        public Form_ProjektOpen()
        {
            InitializeComponent();
            m_szProjekt = "";
            m_szKlimaregion = "";
            ID_Klimaregion = 0;
            m_ID_Projekt = 0;

            listView_Projekt.View = View.Details;
            listView_Projekt.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Projekt.Columns.Add("Beschreibung", -2, HorizontalAlignment.Left);
            listView_Projekt.Columns[0].Width = listView_Projekt.ClientRectangle.Width;
        }

        private void button_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void Form_ProjektOpen_Load(object sender, EventArgs e)
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadAll();
     
            for (int i = 0; i < ctrl.rows; i++)
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = ctrl.items[i].m_szProjektname;
                lvitem.SubItems.Add(ctrl.items[i].m_szBeschreibung);
                listView_Projekt.Items.Add(lvitem);
            }
            listView_Projekt.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Projekt.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            ctrl = null;
        }

        private void button_Open_Click(object sender, EventArgs e)
        {
            ApplikationCtrl ctrl_app = new ApplikationCtrl();
            ProjektCtrl ctrl_projekt = new ProjektCtrl();
            KlimaregionCtrl ctrl_klimaregion = new KlimaregionCtrl();

            if (m_szProjekt == "") return;
            ctrl_projekt.ReadSingle("Select * from Tab_Projekt where Projektname='" + m_szProjekt + "'");

            m_szProjekt = ctrl_projekt.m_szProjektname;
            m_ID_Projekt = ctrl_projekt.m_ID;
            m_Datum = ctrl_projekt.m_Aenderungsdatum;
            m_szBearbeiter = ctrl_projekt.m_szBearbeiter;
            m_szKunde = ctrl_projekt.m_szKunde;
            
            ctrl_app.m_ID_Projekt = ctrl_projekt.m_ID;
            ctrl_app.m_szProjektname = ctrl_projekt.m_szProjektname;
            ctrl_app.Update();

            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void listView_Projekt_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Projekt.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_Projekt.Items[indexes[0]];
                m_szProjekt = lvitem.Text;
            }
        }

        private void listView_Projekt_DoubleClick(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Projekt.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_Projekt.Items[indexes[0]];
                m_szProjekt = lvitem.Text;
                button_Open.PerformClick(); 
            }
   
        }
 
    }
}
