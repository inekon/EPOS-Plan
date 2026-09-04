using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class StromganglinieKontextMenuCtrl
    {
        private ToolStripMenuItem ContextMenuItemNeu;
        private ToolStripMenuItem ContextMenuItemLoeschen;
        public ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();
        
        ListView listView_Stromganglinie = null;
        int m_ID_Projekt = 0;
        string m_szProjektname = "";

        public void Init(ListView ctrl, int ID_Projekt, string szProjektname)
        {
            // Kontextmenü erstellen
            listView_Stromganglinie = ctrl;
            m_ID_Projekt = ID_Projekt;
            m_szProjektname = szProjektname;

            // Menüelemente hinzufügen
            ContextMenuItemNeu = new ToolStripMenuItem();
            ContextMenuItemNeu.Text = "Hinzufügen/Bearbeiten";
            ContextMenuItemNeu.Click += new EventHandler(ContextMenuItemNeu_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemNeu);

            ContextMenuItemLoeschen = new ToolStripMenuItem();
            ContextMenuItemLoeschen.Text = "Löschen";
            ContextMenuItemLoeschen.Click += new EventHandler(ContextMenuItemLoeschen_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemLoeschen);

            // Kontextmenü dem ListView zuweisen
            listView_Stromganglinie.ContextMenuStrip = contextMenuStrip1;

            // Ereignisbehandlung für MouseDown hinzufügen, um das Kontextmenü bei Rechtsklick zu öffnen
            listView_Stromganglinie.MouseDown += new MouseEventHandler(listView_WP_MouseDown);

            contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip1_Opening);
        }

        private void listView_WP_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Überprüfen, ob ein Element unter dem Mauszeiger angeklickt wurde
                ListViewItem item = listView_Stromganglinie.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    if (listView_Stromganglinie.SelectedItems.Count > 0)
                    {
                        // Element auswählen
                        item.Selected = true;
                        // Kontextmenü anzeigen
                        contextMenuStrip1.Show(listView_Stromganglinie, e.Location);
                    }
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView_Stromganglinie.SelectedItems.Count <= 0)
            {
                // e.Cancel = true;
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = false;
            }
            else
            {
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = true;
            }
        }
 
        private void ContextMenuItemLoeschen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Stromganglinie.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            Form_Stromganglinie frm = new Form_Stromganglinie();
            
            if (indexes.Count > 0)
            {
                ListViewItem item = listView_Stromganglinie.Items[indexes[0]];
                List<Z_ProjektStromganglinieModel> waelist = new List<Z_ProjektStromganglinieModel>();

                listView_Stromganglinie.Items[indexes[0]].Remove();
                wizctrl.Del_Stromganglinie(m_ID_Projekt);
                
                frm.DateiListe.Clear();

                for (int i = 0; i < listView_Stromganglinie.Items.Count; i++)
                {
                    Z_ProjektStromganglinieModel model = new Z_ProjektStromganglinieModel();
                    model.m_szStromganglinie = listView_Stromganglinie.Items[i].Text;
                    item = listView_Stromganglinie.Items[i];
                    model.m_ID_Z =  Int32.Parse(item.SubItems[1].Text);
                    model.m_ID_Stromganglinie = Int32.Parse(item.SubItems[2].Text);
                    waelist.Add(model);
                }
                
                wizctrl.Add_Stromganglinie(m_ID_Projekt, waelist);
                
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Dienste.Navigation.OeffneGewerk(Gewerke.Gebaeude, m_ID_Projekt, m_szProjektname);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {
            Form_Stromganglinie frm = new Form_Stromganglinie();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            // iU9-W12.0g: derselbe JOIN wie in Form_Start - jetzt einmal im Kern.
            frm.DateiListe.Clear();
            frm.DateiListe.AddRange(Z_ProjektStromganglinieCtrl.LiesProjekt(m_ID_Projekt));

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);

            frm.ShowDialog();

            if (frm.result == DialogResult.OK)
            {
                wizctrl.Del_Stromganglinie(m_ID_Projekt);
                wizctrl.Add_Stromganglinie(m_ID_Projekt, frm.DateiListe);

                // BEFUND W12-B7, behoben (iU9-W12.0g): Hier stand ein zweiter
                // OeffneGewerk-Aufruf mit Gewerke.WaermebedarfExtern - im
                // Stromganglinien-Ablauf offensichtlich falsch. Er ist ersatzlos
                // gestrichen; die richtige Auffrischung steht am Ende des Blocks
                // (Gewerke.Stromganglinie) und lief ohnehin unmittelbar danach.
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Dienste.Navigation.OeffneGewerk(Gewerke.Stromganglinie, m_ID_Projekt, m_szProjektname);
            }
        }

    }
}
