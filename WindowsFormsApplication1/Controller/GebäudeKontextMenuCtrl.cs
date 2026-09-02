using System;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    class GebäudeKontextMenuCtrl
    {
        private ToolStripMenuItem ContextMenuItemNeu;
        private ToolStripMenuItem ContextMenuItemBearbeiten;
        private ToolStripMenuItem ContextMenuItemLoeschen;
        public ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();
        
        ListView listView_Gebäude = null;
        int m_ID_Projekt = 0;
        string m_szProjektname = "";

        public void Init(ListView ctrl, int ID_Projekt, string szProjektname)
        {
            // Kontextmenü erstellen
            listView_Gebäude = ctrl;
            m_ID_Projekt = ID_Projekt;
            m_szProjektname = szProjektname;

            // Menüelemente hinzufügen
            ContextMenuItemNeu = new ToolStripMenuItem();
            ContextMenuItemNeu.Text = "Hinzufügen";
            ContextMenuItemNeu.Click += new EventHandler(ContextMenuItemNeu_Click);
            //contextMenuStrip1.Items.Add(ContextMenuItemNeu);

            ContextMenuItemBearbeiten = new ToolStripMenuItem();
            ContextMenuItemBearbeiten.Text = "Bearbeiten/Hinzufügen";
            ContextMenuItemBearbeiten.Click += new EventHandler(ContextMenuItemBearbeiten_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemBearbeiten);

            ContextMenuItemLoeschen = new ToolStripMenuItem();
            ContextMenuItemLoeschen.Text = "Löschen";
            ContextMenuItemLoeschen.Click += new EventHandler(ContextMenuItemLoeschen_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemLoeschen);

            // Kontextmenü dem ListView zuweisen
            listView_Gebäude.ContextMenuStrip = contextMenuStrip1;

            // Ereignisbehandlung für MouseDown hinzufügen, um das Kontextmenü bei Rechtsklick zu öffnen
            listView_Gebäude.MouseDown += new MouseEventHandler(listView_WP_MouseDown);

            contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip1_Opening);
        }

        private void listView_WP_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Überprüfen, ob ein Element unter dem Mauszeiger angeklickt wurde
                ListViewItem item = listView_Gebäude.GetItemAt(e.X, e.Y);
                {
                    contextMenuStrip1.Show(listView_Gebäude, e.Location);
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView_Gebäude.SelectedItems.Count <= 0)
            {
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = false;
            }
            else
            {
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = true;
            }
        }

        private void ContextMenuItemBearbeiten_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Gebäude.SelectedIndices;
            Z_ProjGebModel item;
            Form_Gebaeude frm = new Form_Gebaeude();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_gebmodel.Clear();
                
            string sql = @"SELECT
                             Z_ProjektGebaeude.ID, Z_ProjektGebaeude.[ID_Projekt], 
                             [Tab_Gebaeude].ID_ProjektGebaeude, [Tab_Gebaeude].Gebaeudename, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, Einheit_Waermebedarf_Wohnflaeche,
                             Jahresnutzungsgrad, dezWarmwasserbereitung, Gebaeudeart, Beschreibung, Baualtersklasse
                         FROM [Tab_Gebaeude]
                         INNER JOIN Z_ProjektGebaeude ON [Tab_Gebaeude].ID_ProjektGebaeude = Z_ProjektGebaeude.ID
                         WHERE Z_ProjektGebaeude.ID_Projekt=?";

            OleDbParameter[] p = { new OleDbParameter("@id",m_ID_Projekt) };
            DataTable dt = DataRepository.GetDataTable(sql, p);

            for(int i=0; i<dt.Rows.Count; i++)
            {
                DataRow dr = dt.Rows[i];
                item = new Z_ProjGebModel();
                // ARBEITSPAKET S5: harte Casts -> Convert (Typ-Vereinheitlichung).
                // Der Typ-Rueckweg D9 liefert bereits Int32/Boolean; Convert ist die
                // robuste Form, die auch bei Int64/0-1 aus SQLite traegt. Verhalten gleich.
                item.ID_Z = Convert.ToInt32(dr["ID"]);
                item.ID_Projekt = m_ID_Projekt;
                item.ID_Gebaeude = Convert.ToInt32(dr["ID_ProjektGebaeude"]);
                item.Gebaeudename = (string)dr["Gebaeudename"];
                item.Wohnflaeche = (double)dr["Wohnflaeche_Waermebedarf"];
                item.Einheit = (string)dr["Einheit_Waermebedarf_Wohnflaeche"];
                item.Jahresnutzungsgrad = (double)dr["Jahresnutzungsgrad"];
                item.DezentralWarmwasser = Convert.ToBoolean(dr["dezWarmwasserbereitung"]);
                item.Gebaeudeart = (string)dr["Gebaeudeart"];  
                item.Beschreibung = (string)dr["Beschreibung"];
                item.Baualtersklasse = (string)dr["Baualtersklasse"];
                frm.list_gebmodel.Add(item);
            }
                
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_ZuordungGebäude(m_ID_Projekt);
                wizctrl.Add_Projekt_ZuordungGebäude(m_ID_Projekt, frm.list_gebmodel);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetGebaeudeControl(m_szProjektname);
            }
        }

        private void ContextMenuItemLoeschen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Gebäude.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            Form_Gebaeude frm = new Form_Gebaeude();
            
            if (indexes.Count > 0)
            {
                ListViewItem item = listView_Gebäude.Items[indexes[0]];
                listView_Gebäude.Items[indexes[0]].Remove();
                wizctrl.Del_Projekt_ZuordungGebäude(m_ID_Projekt, Int32.Parse(item.SubItems[4].Text));
                
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetGebaeudeControl(m_szProjektname);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {
            Form_Gebaeude frm = new Form_Gebaeude();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_gebmodel.Clear();
            frm.SetControls(m_szProjektname);
            frm.m_ID_Projekt = m_ID_Projekt;
        
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Add_Projekt_ZuordungGebäude(m_ID_Projekt, frm.list_gebmodel);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetGebaeudeControl(m_szProjektname);
            }
        }

    }
}
