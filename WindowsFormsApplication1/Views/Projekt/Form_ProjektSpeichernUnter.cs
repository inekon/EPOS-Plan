using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_ProjektSpeichernUnter : Form
    {
        public string m_szProjekt;
        public string m_szNeuerProjektName;
        public int m_ID_Klimaregion;
        public int m_ID_Projekt;
        public string m_szKlimaregion;
        public string m_szKunde;
        public string m_szBearbeiter;
        public DateTime m_Datum;

        public Form_ProjektSpeichernUnter()
        {
            InitializeComponent();
            m_szProjekt = "";
            m_szKlimaregion = "";
            m_ID_Klimaregion = 0;
            m_ID_Projekt = 0;

            listView_Projekt.View = View.Details;
            listView_Projekt.Columns.Add(MyResource.Resource.Text_Name, -2, HorizontalAlignment.Left);
            listView_Projekt.Columns.Add(MyResource.Resource.Text_Beschreibung, -2, HorizontalAlignment.Left);
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
            listView_Projekt.Select(); 
            if (listView_Projekt.Items.Count>0) listView_Projekt.Items[0].Selected = true;   
            listView_Projekt.Items[0].Selected = true;
            listView_Projekt.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Projekt.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            ctrl = null;
        }

        private void button_Open_Click(object sender, EventArgs e)
        {
            m_szNeuerProjektName = textBox_NeuerProjektName.Text;
            if (listView_Projekt.FindItemWithText(m_szNeuerProjektName) != null) { MessageBox.Show("Projektname bereits vorhanden!","Hinweis",MessageBoxButtons.OK); return; }
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
