using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class SolarKontextMenuCtrl
    {
        private ToolStripMenuItem ContextMenuItemNeu;
        private ToolStripMenuItem ContextMenuItemLoeschen;
        public ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();


        ListView listView_Solar;
        int m_ID_Projekt = 0;
        string m_szProjektname = "";

        public void Init(ListView ctrl, int ID_Projekt, string szProjektname)
        {
            // Kontextmenü erstellen
            listView_Solar = ctrl;
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
            listView_Solar.ContextMenuStrip = contextMenuStrip1;

            // Ereignisbehandlung für MouseDown hinzufügen, um das Kontextmenü bei Rechtsklick zu öffnen
            listView_Solar.MouseDown += new MouseEventHandler(listView_Solar_MouseDown);

            contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip1_Opening);
        }

        private void listView_Solar_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Überprüfen, ob ein Element unter dem Mauszeiger angeklickt wurde
                ListViewItem item = listView_Solar.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    if (listView_Solar.SelectedItems.Count > 0)
                    {
                        // Element auswählen
                        item.Selected = true;
                        // Kontextmenü anzeigen
                        contextMenuStrip1.Show(listView_Solar, e.Location);
                    }
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView_Solar.SelectedItems.Count <= 0)
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
            ListView.SelectedIndexCollection indexes = listView_Solar.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            // iU9-W7.7: Das hier angelegte Form_SolarKollektoren wurde nie benutzt -
            // dieselbe Aufraeumung wie bei den fuenf Kontextmenues der Welle 6 und
            // beim WP-Loeschweg (W7.4).

            if (indexes.Count > 0)
            {
                ListViewItem item = listView_Solar.Items[indexes[0]];
                listView_Solar.Items[indexes[0]].Remove();
                wizctrl.Del_Projekt_ID_Waermeerzeuger(m_ID_Projekt, Int32.Parse(item.SubItems[4].Text));

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Dienste.Navigation.OeffneGewerk(Gewerke.Solarthermie, m_ID_Projekt, m_szProjektname);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            int id_type = WizardItemClass.SOLAR_TYP;

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);

            // Vollstaendig gelesene Modelle durchreichen - wie im Karten-Weg
            // (Form_Start.pBox_Solarthermie_Click, Zweig Kollektorprofil). Die Teilkopie hat
            // beim Speichern alle nicht kopierten Anlagenfelder verloren, weil WizardCtrl
            // unten die Anlagen des Typs loescht und ueber Add_WP_Waermeerzeuger komplett neu
            // schreibt - genullt wurden dabei ID_Carrier, Vorlauf/Ruecklauf, Grenzleistung,
            // Betriebsart, Sperrung/Sperrzeiten, Bivalenter_Betrieb, Abschaltpunkt und
            // Nutzungszeit.
            List<WErzeugerModel> liste = new List<WErzeugerModel>();
            for (int i = 0; i < werzctrl.rows; i++)
            {
                liste.Add(werzctrl.items[i]);
            }

            // iU9-W7.7: Der Kollektordialog ist die Razor-Komponente
            // SolarkollektorenDialog; Form_SolarKollektoren ist im selben Schritt
            // GELOESCHT (Regel M1).
            if (SolarkollektorHuelle.Oeffnen(null, m_ID_Projekt, liste))
            {
                // Datenbank aktualisieren
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, liste);

                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();

                Dienste.Navigation.OeffneGewerk(Gewerke.Solarthermie, m_ID_Projekt, m_szProjektname);
            }

        }
    }
}
