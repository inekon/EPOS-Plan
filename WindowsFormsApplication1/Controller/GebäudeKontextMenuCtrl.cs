using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class GebäudeKontextMenuCtrl : Form
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
               // if (item != null)
                {
                 //   if (listView_Gebäude.SelectedItems.Count > 0)
                    {
                        // Element auswählen
                     //   item.Selected = true;
                        // Kontextmenü anzeigen
                        contextMenuStrip1.Show(listView_Gebäude, e.Location);
                    }
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView_Gebäude.SelectedItems.Count <= 0)
            {
                // e.Cancel = true;
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = false;
                //contextMenuStrip1.Items[2].Enabled = false;
            }
            else
            {
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = true;
          //      contextMenuStrip1.Items[2].Enabled = true;
            }
        }

        private void ContextMenuItemBearbeiten_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Gebäude.SelectedIndices;

         //   if (indexes.Count > 0)
            {
            //    ListViewItem lvitem = listView_Gebäude.Items[indexes[0]];
                Z_ProjGebModel item;
                Form_Gebaeude frm = new Form_Gebaeude();
                RecordSet rs = new RecordSet();
                WizardCtrl wizctrl = new WizardCtrl();
                ProjektCtrl projctrl = new ProjektCtrl();

                frm.list_gebmodel.Clear();
                frm.SetControls(m_szProjektname);
                
                string sql = "SELECT Z_ProjektGebaeude.ID, Z_ProjektGebaeude.ID_Gebaeude, Z_ProjektGebaeude.[ID_Projekt], " +
                    "[DBGebaeude].Gebaeudename, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, " +
                    "dezWarmwasserbereitung, Gebaeudeart, Beschreibung  FROM [DBGebaeude] " +
                    "INNER JOIN Z_ProjektGebaeude ON [DBGebaeude].ID = Z_ProjektGebaeude.ID_Gebaeude" +
                    " where Z_ProjektGebaeude.ID_Projekt=" + m_ID_Projekt;

                rs.Open(sql);
                while(rs.Next())
                {
                    item = new Z_ProjGebModel();
                    item.ID_Z = (int)rs.Read("ID");
                    item.ID_Projekt = m_ID_Projekt;
                    item.ID_Gebaeude = (int)rs.Read("ID_Gebaeude");
                    item.Gebaeudename = (string)rs.Read("Gebaeudename");
                    item.Wohnflaeche = (double)rs.Read("Wohnflaeche_Waermebedarf");
                    item.Einheit = (string)rs.Read("Einheit_Waermebedarf_Wohnflaeche");
                    item.Jahresnutzungsgrad = (double)rs.Read("Jahresnutzungsgrad");
                    item.DezentralWarmwasser = (bool)rs.Read("dezWarmwasserbereitung");
                    item.Gebaeudeart = (string)rs.Read("Gebaeudeart");
                    item.Beschreibung = (string)rs.Read("Beschreibung");

                    frm.list_gebmodel.Add(item);
                }
                
                frm.m_ID_Projekt = m_ID_Projekt;
                frm.SetControls(m_szProjektname);
                frm.ShowDialog();

                if (frm.DialogResult == DialogResult.OK)
                {
                    for (int i = 0; i < frm.list_gebmodel.Count; i++)
                    {
                        wizctrl.Del_Projekt_ZuordungGebäude(m_ID_Projekt, frm.list_gebmodel[i].ID_Z);
                    }
                    wizctrl.Add_Projekt_ZuordungGebäude(m_ID_Projekt, frm.list_gebmodel);

                    projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                    projctrl.m_Aenderungsdatum = DateTime.Now;
                    projctrl.Update();
                    Program.mainfrm.SetGebaeudeControl(m_szProjektname);
                }
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
                
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
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

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetGebaeudeControl(m_szProjektname);
            }
        }

    }
}
