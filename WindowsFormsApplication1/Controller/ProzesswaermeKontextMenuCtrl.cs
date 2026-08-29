using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class ProzesswaermeKontextMenuCtrl
    {
        private ToolStripMenuItem ContextMenuItemNeu;
        private ToolStripMenuItem ContextMenuItemBearbeiten;
        private ToolStripMenuItem ContextMenuItemLoeschen;
        public ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();
        
        ListView listView_Prozesswaerme = null;
        int m_ID_Projekt = 0;
        string m_szProjektname = "";

        public void Init(ListView ctrl, int ID_Projekt, string szProjektname)
        {
            // Kontextmenü erstellen
            listView_Prozesswaerme = ctrl;
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
            listView_Prozesswaerme.ContextMenuStrip = contextMenuStrip1;

            // Ereignisbehandlung für MouseDown hinzufügen, um das Kontextmenü bei Rechtsklick zu öffnen
            listView_Prozesswaerme.MouseDown += new MouseEventHandler(listView_WP_MouseDown);

            contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip1_Opening);
        }

        private void listView_WP_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Überprüfen, ob ein Element unter dem Mauszeiger angeklickt wurde
                ListViewItem item = listView_Prozesswaerme.GetItemAt(e.X, e.Y);
                if (item != null) item.Selected = true;

                // DAS MENÜ ÖFFNET AUCH ÜBER DER LEEREN LISTE (Befund 27.08.2026, Lücke F).
                // Vorher stand hier "if (item != null) if (SelectedItems.Count > 0)": In einem
                // NEUEN Projekt ist die Liste leer, es gibt also kein Element unter dem
                // Mauszeiger - und damit war "Hinzufügen/Bearbeiten" der einzige Einstieg,
                // über den sich Prozesswärme überhaupt zuordnen lässt, genau dann
                // unerreichbar, wenn man ihn braucht. Der Eintrag "Hinzufügen" ist
                // zusätzlich seit jeher auskommentiert (Init).
                //
                // Dasselbe Muster wie GebäudeKontextMenuCtrl.listView_WP_MouseDown. Die
                // Ausgrauung der Einträge bleibt Sache von contextMenuStrip1_Opening: Ohne
                // Auswahl ist "Hinzufügen/Bearbeiten" aktiv und "Löschen" gesperrt.
                contextMenuStrip1.Show(listView_Prozesswaerme, e.Location);
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView_Prozesswaerme.SelectedItems.Count <= 0)
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
            Form_Prozesswaerme frm = new Form_Prozesswaerme();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_pwmodel.Clear();
            frm.SetControls(m_szProjektname);

            string sql = "SELECT Z_Projekt_Prozesswaerme.ID, Z_Projekt_Prozesswaerme.ID_Projekt, " +
                "Z_Projekt_Prozesswaerme.ID_Prozesswaerme, Tab_Prozesswaerme.Bezeichner, " +
                " Z_Projekt_Prozesswaerme.Summe " +
                "FROM Z_Projekt_Prozesswaerme INNER JOIN Tab_Prozesswaerme ON " +
                "Z_Projekt_Prozesswaerme.ID_Prozesswaerme = Tab_Prozesswaerme.ID " +
                " where Z_Projekt_Prozesswaerme.ID_Projekt=" + m_ID_Projekt;
                //+ " and Tab_Prozesswaerme.Prozessname='" + lvitem.Text +
                //"' and Z_Projekt_Prozesswaerme.ID=" + lvitem.SubItems[3].Text;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektProzesswaermeModel item = new Z_ProjektProzesswaermeModel();
                item.ID_Z = (int)rs.Read("ID");
                item.ID_Projekt = m_ID_Projekt;
                item.ID_Prozesswaerme = (int)rs.Read("ID_Prozesswaerme");
                item.szProzessname = (string)rs.Read("Bezeichner");
                item.Summe = (double)rs.Read("Summe");
                frm.list_pwmodel.Add(item);
            }
                
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_Prozess(m_ID_Projekt);
                wizctrl.Add_Projekt_Prozess(m_ID_Projekt, frm.list_pwmodel);
                    
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetProzesswaermeControl(m_ID_Projekt);
            }
        }

        private void ContextMenuItemLoeschen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Prozesswaerme.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            
            if (indexes.Count > 0)
            {
                ListViewItem item = listView_Prozesswaerme.Items[indexes[0]];
                listView_Prozesswaerme.Items[indexes[0]].Remove();
                wizctrl.Del_Projekt_Prozess(m_ID_Projekt, Int32.Parse(item.SubItems[3].Text));
                
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetProzesswaermeControl(m_ID_Projekt);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {
            Form_Prozesswaerme frm = new Form_Prozesswaerme();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_pwmodel.Clear();
            frm.SetControls(m_szProjektname);
            frm.m_ID_Projekt = m_ID_Projekt;
        
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Add_Projekt_Prozess(m_ID_Projekt, frm.list_pwmodel);

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetProzesswaermeControl(m_ID_Projekt);
            }
        }

    }


}
