using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class StrombedarfKontextMenuCtrl
    {
        private ToolStripMenuItem ContextMenuItemNeu;
        private ToolStripMenuItem ContextMenuItemBearbeiten;
        private ToolStripMenuItem ContextMenuItemLoeschen;
        public ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();
        
        ListView listView_Strombedarf = null;
        int m_ID_Projekt = 0;
        string m_szProjektname = "";

        public void Init(ListView ctrl, int ID_Projekt, string szProjektname)
        {
            // Kontextmenü erstellen
            listView_Strombedarf = ctrl;
            m_ID_Projekt = ID_Projekt;
            m_szProjektname = szProjektname;

            // Menüelemente hinzufügen
            ContextMenuItemNeu = new ToolStripMenuItem();
            ContextMenuItemNeu.Text = "Hinzufügen";
            ContextMenuItemNeu.Click += new EventHandler(ContextMenuItemNeu_Click);
            //contextMenuStrip1.Items.Add(ContextMenuItemNeu);

            ContextMenuItemBearbeiten = new ToolStripMenuItem();
            ContextMenuItemBearbeiten.Text = "Hinzufügen/Bearbeiten";
            ContextMenuItemBearbeiten.Click += new EventHandler(ContextMenuItemBearbeiten_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemBearbeiten);


            ContextMenuItemLoeschen = new ToolStripMenuItem();
            ContextMenuItemLoeschen.Text = "Löschen";
            ContextMenuItemLoeschen.Click += new EventHandler(ContextMenuItemLoeschen_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemLoeschen);

            // Kontextmenü dem ListView zuweisen
            listView_Strombedarf.ContextMenuStrip = contextMenuStrip1;

            // Ereignisbehandlung für MouseDown hinzufügen, um das Kontextmenü bei Rechtsklick zu öffnen
            listView_Strombedarf.MouseDown += new MouseEventHandler(listView_WP_MouseDown);

            contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip1_Opening);
        }

        private void listView_WP_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Überprüfen, ob ein Element unter dem Mauszeiger angeklickt wurde
                ListViewItem item = listView_Strombedarf.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    if (listView_Strombedarf.SelectedItems.Count > 0)
                    {
                        // Element auswählen
                        item.Selected = true;
                        // Kontextmenü anzeigen
                        contextMenuStrip1.Show(listView_Strombedarf, e.Location);
                    }
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView_Strombedarf.SelectedItems.Count <= 0)
            {
                // e.Cancel = true;
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = false;
              //  contextMenuStrip1.Items[2].Enabled = false;
            }
            else
            {
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = true;
              //  contextMenuStrip1.Items[2].Enabled = true;
            }
        }

        private void ContextMenuItemBearbeiten_Click(object sender, EventArgs e)
        {

            Form_Stromverbraucher frm = new Form_Stromverbraucher();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_sbmodel.Clear();
            frm.SetControls(m_szProjektname);

            string sql = "SELECT Z_Projekt_Stromverbraucher.ID, Z_Projekt_Stromverbraucher.ID_Projekt, " +
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher, Tab_Stromverbraucher.Bezeichner " +
                "FROM Z_Projekt_Stromverbraucher INNER JOIN Tab_Stromverbraucher ON " +
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher = Tab_Stromverbraucher.ID " +
                " where ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektStromverbraucherModel item = new Z_ProjektStromverbraucherModel();
                item.m_ID_Z = (int)rs.Read("ID");
                item.m_ID_Projekt = m_ID_Projekt;
                item.m_ID_Stromverbraucher = (int)rs.Read("ID_Stromverbraucher");
                item.m_szVerbraucher = (string)rs.Read("Bezeichner");//item.Text;
                frm.list_sbmodel.Add(item);
            }
                
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_Stromverbraucher(m_ID_Projekt);
                wizctrl.Add_Projekt_Stromverbraucher(m_ID_Projekt, frm.list_sbmodel);
                    
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetStrombedarfControl(m_ID_Projekt);
            }
        }

        private void ContextMenuItemLoeschen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Strombedarf.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            
            if (indexes.Count > 0)
            {
                ListViewItem item = listView_Strombedarf.Items[indexes[0]];
                listView_Strombedarf.Items[indexes[0]].Remove();
                wizctrl.Del_Projekt_Stromverbraucher(m_ID_Projekt, Int32.Parse(item.SubItems[3].Text));
                
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetStrombedarfControl(m_ID_Projekt);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {
            Form_Stromverbraucher frm = new Form_Stromverbraucher();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_sbmodel.Clear();
            frm.SetControls(m_szProjektname);
            frm.m_ID_Projekt = m_ID_Projekt;
        
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Add_Projekt_Stromverbraucher(m_ID_Projekt, frm.list_sbmodel);

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetStrombedarfControl(m_ID_Projekt);
            }
        }

    }


}
