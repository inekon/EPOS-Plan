using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            // iU9-W9.0d: derselbe JOIN wie im Startbild - jetzt EINMAL im Kern.
            List<Z_ProjGebModel> liste = Z_ProjGebCtrl.LiesProjekt(m_ID_Projekt);

            // iU9-W9.2: Blazor-Huelle statt Form_Gebaeude.
            if (GebaeudeHuelle.Oeffnen(listView_Gebäude, m_ID_Projekt, m_szProjektname, liste))
            {
                wizctrl.Del_Projekt_ZuordungGebäude(m_ID_Projekt);
                wizctrl.Add_Projekt_ZuordungGebäude(m_ID_Projekt, liste);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Dienste.Navigation.OeffneGewerk(Gewerke.Gebaeude, m_ID_Projekt, m_szProjektname);
            }
        }

        private void ContextMenuItemLoeschen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Gebäude.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            // iU9-W9.2: Das frueher hier angelegte Form_Gebaeude war unbenutzt - es
            // wurde weder gefuellt noch gezeigt. Ersatzlos gestrichen.
            
            if (indexes.Count > 0)
            {
                ListViewItem item = listView_Gebäude.Items[indexes[0]];
                listView_Gebäude.Items[indexes[0]].Remove();
                wizctrl.Del_Projekt_ZuordungGebäude(m_ID_Projekt, Int32.Parse(item.SubItems[4].Text));
                
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Dienste.Navigation.OeffneGewerk(Gewerke.Gebaeude, m_ID_Projekt, m_szProjektname);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            // "Hinzufuegen" startet mit einer LEEREN Liste und legt nur an (kein Del_).
            List<Z_ProjGebModel> liste = new List<Z_ProjGebModel>();

            // iU9-W9.2: Blazor-Huelle statt Form_Gebaeude.
            if (GebaeudeHuelle.Oeffnen(listView_Gebäude, m_ID_Projekt, m_szProjektname, liste))
            {
                wizctrl.Add_Projekt_ZuordungGebäude(m_ID_Projekt, liste);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Dienste.Navigation.OeffneGewerk(Gewerke.Gebaeude, m_ID_Projekt, m_szProjektname);
            }
        }

    }
}
