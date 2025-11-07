using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class SpKontextMenuCtrl : Form
    {
        private ToolStripMenuItem ContextMenuItemNeu;
        private ToolStripMenuItem ContextMenuItemLoeschen;
        public ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();
        
        ListView listView_SP;
        int m_ID_Projekt = 0;
        string m_szProjektname = "";

        public void Init(ListView ctrl, int ID_Projekt, string szProjektname)
        {
            // Kontextmenü erstellen
            listView_SP = ctrl;
            m_ID_Projekt = ID_Projekt;
            m_szProjektname = szProjektname;

            // Menüelemente hinzufügen
            ContextMenuItemNeu = new ToolStripMenuItem();
            ContextMenuItemNeu.Text = "Hinzufügen";
            ContextMenuItemNeu.Click += new EventHandler(ContextMenuItemNeu_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemNeu);

            ContextMenuItemLoeschen = new ToolStripMenuItem();
            ContextMenuItemLoeschen.Text = "Löschen";
            ContextMenuItemLoeschen.Click += new EventHandler(ContextMenuItemLoeschen_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemLoeschen);

            ContextMenuItemNeu = new ToolStripMenuItem();
            ContextMenuItemNeu.Text = "Bearbeiten";
            ContextMenuItemNeu.Click += new EventHandler(ContextMenuItemBearbeiten_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemNeu);

            // Kontextmenü dem ListView zuweisen
            listView_SP.ContextMenuStrip = contextMenuStrip1;

            // Ereignisbehandlung für MouseDown hinzufügen, um das Kontextmenü bei Rechtsklick zu öffnen
            listView_SP.MouseDown += new MouseEventHandler(listView_SP_MouseDown);

            contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip1_Opening);
        }

        private void listView_SP_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Überprüfen, ob ein Element unter dem Mauszeiger angeklickt wurde
                ListViewItem item = listView_SP.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    if (listView_SP.SelectedItems.Count > 0)
                    {
                        // Element auswählen
                        item.Selected = true;
                        // Kontextmenü anzeigen
                        contextMenuStrip1.Show(listView_SP, e.Location);
                    }
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView_SP.SelectedItems.Count <= 0)
            {
                // e.Cancel = true;
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = false;
                contextMenuStrip1.Items[2].Enabled = false;
            }
            else
            {
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = true;
                contextMenuStrip1.Items[2].Enabled = true;
             }
        }

        private void ContextMenuItemLoeschen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_SP.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
 
            if (indexes.Count > 0)
            {
                ListViewItem item = listView_SP.Items[indexes[0]];
                listView_SP.Items[indexes[0]].Remove();
                wizctrl.Del_Projekt_ID_Waermeerzeuger(m_ID_Projekt, Int32.Parse(item.SubItems[6].Text));

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetSPControl(m_szProjektname);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {
            Form_Sp frm = new Form_Sp();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_spmodel.Clear();
            if (listView_SP.Name == "listView_SP_REF")
            {
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.REF_SP_TYP);
                id_type = WizardItemClass.REF_SP_TYP;
            }
            else
            {
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);
                id_type = WizardItemClass.SP_TYP;
            }
            
            for (int i = 0; i < werzctrl.rows; i++)
            {
                WErzeugerModel item = new WErzeugerModel();
                item.ID = werzctrl.items[i].ID;
                item.ID_SP = werzctrl.items[i].ID_SP;
                item.ID_Type = werzctrl.items[i].ID_Type;
                item.Bezeichner = werzctrl.items[i].Bezeichner;

                frm.list_spmodel.Add(item);
            }

            frm.SetControls(m_szProjektname);
            frm.m_nType = id_type;
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                // Datenbank aktualisieren
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_spmodel);

                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();

                Program.mainfrm.SetSPControl(m_szProjektname);
            }
        }

        private void ContextMenuItemBearbeiten_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_SP.SelectedIndices;

            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            Form_AdminStromspeicher frm = new Form_AdminStromspeicher();

            if (indexes.Count <= 0) return;

            frm.list_spmodel.Clear();
            ListViewItem item = listView_SP.Items[indexes[0]];
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID=" + Int32.Parse(item.SubItems[6].Text));

            WErzeugerModel model = new WErzeugerModel();
            model.ID = werzctrl.items[0].ID;
            model.ID_SP = werzctrl.items[0].ID_SP;
            model.ID_Type = werzctrl.items[0].ID_Type;
            model.Bezeichner = werzctrl.items[0].Bezeichner;
            frm.list_spmodel.Add(model);
            frm.m_bItemBearbeiten = true;
            int id_type = model.ID_Type;

            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                WizardCtrl wizctrl = new WizardCtrl();
                // Datenbank aktualisieren
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_spmodel);

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();

                Program.mainfrm.SetSPControl(m_szProjektname);
            }
        }
    }
}
