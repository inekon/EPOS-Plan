using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class HeizkesselKontextMenuCtrl
    {
        private ToolStripMenuItem ContextMenuItemNeu;
        private ToolStripMenuItem ContextMenuItemLoeschen;
        public ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();
        
        
        ListView listView_Heizkessel;
        int m_ID_Projekt = 0;
        string m_szProjektname = "";

        public void Init(ListView ctrl, int ID_Projekt, string szProjektname)
        {
            // Kontextmenü erstellen
            listView_Heizkessel = ctrl;
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
            listView_Heizkessel.ContextMenuStrip = contextMenuStrip1;

            // Ereignisbehandlung für MouseDown hinzufügen, um das Kontextmenü bei Rechtsklick zu öffnen
            listView_Heizkessel.MouseDown += new MouseEventHandler(listView_Heizkessel_MouseDown);

            contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip1_Opening);
        }

        private void listView_Heizkessel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Überprüfen, ob ein Element unter dem Mauszeiger angeklickt wurde
                ListViewItem item = listView_Heizkessel.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    if (listView_Heizkessel.SelectedItems.Count > 0)
                    {
                        // Element auswählen
                        item.Selected = true;
                        // Kontextmenü anzeigen
                        contextMenuStrip1.Show(listView_Heizkessel, e.Location);
                    }
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView_Heizkessel.SelectedItems.Count <= 0)
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

        private void ContextMenuItemLoeschen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Heizkessel.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            if (indexes.Count > 0)
            {
                ListViewItem item = listView_Heizkessel.Items[indexes[0]];
                listView_Heizkessel.Items[indexes[0]].Remove();
                wizctrl.Del_Projekt_ID_Waermeerzeuger(m_ID_Projekt, Int32.Parse(item.SubItems[4].Text));

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Dienste.Navigation.OeffneGewerk(Gewerke.Heizkessel, m_ID_Projekt, m_szProjektname);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {
            // iU9-W6.3: Der Dialog ist die Razor-Komponente HeizkesselDialog; die
            // WinForms-Fassung Form_Heizkessel ist im selben Schritt GELOESCHT (Regel M1).
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            int id_type;
            List<WErzeugerModel> liste = new List<WErzeugerModel>();

            if (listView_Heizkessel.Name == "listView_Heizkessel_REF")
            {
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.REF_KESSEL_TYP);
                id_type = WizardItemClass.REF_KESSEL_TYP;
            }
            else
            {
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.KESSEL_TYP);
                id_type = WizardItemClass.KESSEL_TYP;
            }

            // Vollstaendig gelesene Modelle durchreichen (siehe BHKWKontextMenuCtrl). Die
            // Teilkopie aus ID/ID_Kessel/ID_Type/Bezeichner hat beim Speichern alle uebrigen
            // Anlagenfelder verloren, weil WizardCtrl unten die Anlagen des Typs loescht und
            // ueber Add_WP_Waermeerzeuger komplett neu schreibt - genullt wurden dabei
            // ID_Carrier, Vorlauf/Ruecklauf, Grenzleistung, Betriebsart, Sperrung/Sperrzeiten,
            // Bivalenter_Betrieb, Abschaltpunkt und Nutzungszeit.
            for (int i = 0; i < werzctrl.rows; i++)
            {
                liste.Add(werzctrl.items[i]);
            }

            if (HeizkesselHuelle.Oeffnen(null, m_ID_Projekt, id_type, liste))
            {
                // Datenbank aktualisieren
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, liste);

                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();

                Dienste.Navigation.OeffneGewerk(Gewerke.Heizkessel, m_ID_Projekt, m_szProjektname);
            }
        }
    }
}
