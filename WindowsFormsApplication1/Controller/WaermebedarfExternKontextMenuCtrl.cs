using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class WaermebedarfExternKontextMenuCtrl
    {
        private ToolStripMenuItem ContextMenuItemNeu;
        private ToolStripMenuItem ContextMenuItemLoeschen;
        public ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();
        
        ListView listView_WaermebedarfExtern = null;
        int m_ID_Projekt = 0;
        string m_szProjektname = "";

        public void Init(ListView ctrl, int ID_Projekt, string szProjektname)
        {
            // Kontextmenü erstellen
            listView_WaermebedarfExtern = ctrl;
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
            listView_WaermebedarfExtern.ContextMenuStrip = contextMenuStrip1;

            // Ereignisbehandlung für MouseDown hinzufügen, um das Kontextmenü bei Rechtsklick zu öffnen
            listView_WaermebedarfExtern.MouseDown += new MouseEventHandler(listView_WP_MouseDown);

            contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip1_Opening);
        }

        private void listView_WP_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Überprüfen, ob ein Element unter dem Mauszeiger angeklickt wurde
                ListViewItem item = listView_WaermebedarfExtern.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    if (listView_WaermebedarfExtern.SelectedItems.Count > 0)
                    {
                        // Element auswählen
                        item.Selected = true;
                        // Kontextmenü anzeigen
                        contextMenuStrip1.Show(listView_WaermebedarfExtern, e.Location);
                    }
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView_WaermebedarfExtern.SelectedItems.Count <= 0)
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
            ListView.SelectedIndexCollection indexes = listView_WaermebedarfExtern.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            if (indexes.Count > 0)
            {
                ListViewItem item = listView_WaermebedarfExtern.Items[indexes[0]];
                List<Z_ProjWaermebedarfModel> waelist = new List<Z_ProjWaermebedarfModel>();

                // Migrationsschritt 48 (F18): Die Kanäle VOR dem Löschen sichern.
                // Die ListView führt nur Bezeichner, ID_Z und ID_Ganglinie; die Liste
                // unten entsteht allein aus ihr, und der Speicherweg ist Löschen +
                // Neuanlegen - ohne diese Karte verlören die verbleibenden Ganglinien
                // ihren Kanal beim Entfernen einer einzigen Zeile.
                Dictionary<int, string> kanaele =
                    Z_ProjektGebGanglinieCtrl.KanaeleLesen(m_ID_Projekt);

                listView_WaermebedarfExtern.Items[indexes[0]].Remove();
                wizctrl.Del_WaermebedarfExtern(m_ID_Projekt);

                for (int i = 0; i < listView_WaermebedarfExtern.Items.Count; i++)
                {
                    Z_ProjWaermebedarfModel model = new Z_ProjWaermebedarfModel();
                    model.m_szBezeichner = listView_WaermebedarfExtern.Items[i].Text;
                    item = listView_WaermebedarfExtern.Items[i];
                    model.m_ID_Z =  Int32.Parse(item.SubItems[1].Text);
                    model.m_ID_Ganglinie = Int32.Parse(item.SubItems[2].Text);

                    string kanal;
                    if (kanaele.TryGetValue(model.m_ID_Z, out kanal)) model.Kanal = kanal;

                    waelist.Add(model);
                }

                wizctrl.Add_WaermebedarfExtern(m_ID_Projekt, waelist);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Dienste.Navigation.OeffneGewerk(Gewerke.WaermebedarfExtern, m_ID_Projekt, m_szProjektname);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            // iU9-W9.0d: der JOIN samt Kanal steht im Kern.
            List<Z_ProjWaermebedarfModel> liste =
                Z_ProjektGebGanglinieCtrl.LiesProjekt(m_ID_Projekt);

            // iU9-W9.4: Blazor-Huelle statt Form_Waermebedarf.
            if (WaermebedarfExternHuelle.Oeffnen(listView_WaermebedarfExtern, m_ID_Projekt,
                                                 m_szProjektname, liste))
            {
                wizctrl.Del_WaermebedarfExtern(m_ID_Projekt);
                wizctrl.Add_WaermebedarfExtern(m_ID_Projekt, liste);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Dienste.Navigation.OeffneGewerk(Gewerke.WaermebedarfExtern, m_ID_Projekt, m_szProjektname);
            }
        }

    }
}
