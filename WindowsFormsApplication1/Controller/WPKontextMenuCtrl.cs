using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class WPKontextMenuCtrl
    {
        private ToolStripMenuItem ContextMenuItemAnzeigen;
        private ToolStripMenuItem ContextMenuItemNeu;
        private ToolStripMenuItem ContextMenuItemBearbeiten;
        private ToolStripMenuItem ContextMenuItemLoeschen;
        public ContextMenuStrip contextMenuStrip1 = new ContextMenuStrip();
        
        ListView listView_WP;
        int m_ID_Projekt = 0;
        string m_szProjektname = "";

        public void Init(ListView ctrl, int ID_Projekt, string szProjektname)
        {
            // Kontextmenü erstellen
            listView_WP = ctrl;
            m_ID_Projekt = ID_Projekt;
            m_szProjektname = szProjektname;

            // Menüelemente hinzufügen
            ContextMenuItemNeu = new ToolStripMenuItem();
            ContextMenuItemNeu.Text = "Hinzufügen";
            ContextMenuItemNeu.Click += new EventHandler(ContextMenuItemNeu_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemNeu);

            ContextMenuItemAnzeigen = new ToolStripMenuItem();
            ContextMenuItemAnzeigen.Text = "Anzeigen";
            ContextMenuItemAnzeigen.Click += new EventHandler(ContextMenuItemAnzeigen_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemAnzeigen);

            ContextMenuItemBearbeiten = new ToolStripMenuItem();
            ContextMenuItemBearbeiten.Text = "Bearbeiten";
            ContextMenuItemBearbeiten.Click += new EventHandler(ContextMenuItemBearbeiten_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemBearbeiten);


            ContextMenuItemLoeschen = new ToolStripMenuItem();
            ContextMenuItemLoeschen.Text = "Löschen";
            ContextMenuItemLoeschen.Click += new EventHandler(ContextMenuItemLoeschen_Click);
            contextMenuStrip1.Items.Add(ContextMenuItemLoeschen);

            // Kontextmenü dem ListView zuweisen
            listView_WP.ContextMenuStrip = contextMenuStrip1;
 
            // Ereignisbehandlung für MouseDown hinzufügen, um das Kontextmenü bei Rechtsklick zu öffnen
            listView_WP.MouseDown += new MouseEventHandler(listView_WP_MouseDown);

            contextMenuStrip1.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStrip1_Opening);
        }

        private void listView_WP_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                // Überprüfen, ob ein Element unter dem Mauszeiger angeklickt wurde
                ListViewItem item = listView_WP.GetItemAt(e.X, e.Y);
                if (item != null)
                {
                    if (listView_WP.SelectedItems.Count > 0)
                    {
                        // Element auswählen
                        item.Selected = true;
                        // Kontextmenü anzeigen
                        contextMenuStrip1.Show(listView_WP, e.Location);
                    }
                }
            }
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (listView_WP.SelectedItems.Count <= 0)
            {
                // e.Cancel = true;
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = false;
                contextMenuStrip1.Items[2].Enabled = false;
                contextMenuStrip1.Items[3].Enabled = false;
            }
            else
            {
                contextMenuStrip1.Items[0].Enabled = true;
                contextMenuStrip1.Items[1].Enabled = true;
                contextMenuStrip1.Items[2].Enabled = true;
                contextMenuStrip1.Items[3].Enabled = true;
            }
        }

        private void ContextMenuItemAnzeigen_Click(object sender, EventArgs e)
        {

            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_WP.Items[indexes[0]];
                WErzeugerCtrl werzctrl = new WErzeugerCtrl();

                werzctrl.ReadAllFilter("Bezeichner='" + lvitem.Text + "' and ID_Projekt=" + m_ID_Projekt + " and ID=" + lvitem.SubItems[4].Text  + " and (ID_Type=" + WizardItemClass.WP_TYP + " Or ID_Type=" + WizardItemClass.REF_WP_TYP + ")");
                WaermepumpeGeraeteCtrl.GeraetedatenFuellen(werzctrl.items[0], werzctrl.items[0].ID_WP);

                // iU9-W7.4: Die Detailansicht ist die Razor-Komponente
                // WaermepumpeAnlageDialog; Wizard_WPItem ist im selben Schritt
                // GELOESCHT (Regel M1). Der Weg "Anzeigen" wertet das Ergebnis wie
                // bisher nicht aus.
                WaermepumpeAnlageHuelle.Oeffnen(null, werzctrl.items[0], m_ID_Projekt);
            }
        }

        private void ContextMenuItemLoeschen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            // iU9-W7.4: Das hier angelegte Form_Gebaeude wurde nie benutzt - eine
            // fremde Maske, die beim Loeschen einer Waermepumpe nichts zu suchen hat
            // (dieselbe Aufraeumung wie bei den fuenf Kontextmenues der Welle 6).

            if (indexes.Count > 0)
            {
                ListViewItem item = listView_WP.Items[indexes[0]];
                listView_WP.Items[indexes[0]].Remove();
                wizctrl.Del_Projekt_ID_Waermeerzeuger(m_ID_Projekt, Int32.Parse(item.SubItems[4].Text));

                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Dienste.Navigation.OeffneGewerk(Gewerke.Waermepumpe, m_ID_Projekt, m_szProjektname);
            }
        }

        private void ContextMenuItemBearbeiten_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_WP.Items[indexes[0]];
                WErzeugerCtrl werzctrl = new WErzeugerCtrl();
                List<WErzeugerModel> list_alle = new List<WErzeugerModel>();

               // werzctrl.ReadAllFilter("Bezeichner='" + lvitem.Text + "' and ID_Projekt=" + m_ID_Projekt + " and ID=" + lvitem.SubItems[4].Text + " and (ID_Type=" + WizardItemClass.WP_TYP + " Or ID_Type=" + WizardItemClass.REF_WP_TYP + ")");
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and (ID_Type=" + WizardItemClass.WP_TYP + " Or ID_Type=" + WizardItemClass.REF_WP_TYP + ")");
                int index=0;
                
                for (int i = 0; i < werzctrl.rows; i++)
                {
                    if (werzctrl.items[i].Bezeichner == lvitem.Text && werzctrl.items[i].ID == Int32.Parse(lvitem.SubItems[4].Text))
                    {
                        // WP Kenndaten lesen - zweistufig wie ueberall sonst (Ä22).
                        // ID_Type gehoert nicht zum Stammdaten-Merge: er unterscheidet Plan-
                        // (WP_TYP) und Referenzliste (REF_WP_TYP) und steht nach ReadAllFilter
                        // je Zeile korrekt. Der fruehere Angleich an items[0] kippte bei
                        // Mischbestand den Typ der bearbeiteten Zeile.
                        WaermepumpeGeraeteCtrl.GeraetedatenFuellen(werzctrl.items[i], werzctrl.items[i].ID_WP);
                        index = i;
                    }
                    list_alle.Add(werzctrl.items[i]);
                }

                // iU9-W7.4: Die Detailansicht ist die Razor-Komponente
                // WaermepumpeAnlageDialog; Wizard_WPItem ist im selben Schritt
                // GELOESCHT (Regel M1). Die Huelle bearbeitet die Zeile AN ORT UND
                // STELLE - es ist dieselbe Instanz, die in list_alle steht.
                if (!WaermepumpeAnlageHuelle.Oeffnen(null, list_alle[index], m_ID_Projekt)) return;

                // Der frueher hier stehende Rueckschreibblock ist ersatzlos entfallen:
                // Er kopierte 21 Felder von frm_wpitem.item nach list_alle[index] -
                // und das war DASSELBE Objekt (list[0] == werzctrl.items[index] ==
                // list_alle[index]). Zwanzig der Zuweisungen waren damit wirkungslos.
                // Die einundzwanzigste war es NICHT und richtete Schaden an
                // (Befund W7-O-4, Abweichung A-21):
                //     list_alle[index].Regelung = frm_wpitem.item.Leistungsstufen;
                // WPModel.Leistungsstufen wird im ganzen Bestand NIE geschrieben und
                // steht auf ""; jedes "Bearbeiten" aus diesem Kontextmenue loeschte
                // damit die Leistungsstufen der Anlage.

                WizardCtrl wizctrl = new WizardCtrl();

                // Nur die beiden WP-Typen loeschen (Plan- und Referenzliste dieses Menues):
                // list_alle fuehrt ausschliesslich WP-Zeilen, ein Loeschen ohne Typfilter
                // wuerde alle uebrigen Gewerke des Projekts unwiederbringlich mitnehmen.
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, WizardItemClass.WP_TYP);
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, WizardItemClass.REF_WP_TYP);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, list_alle);

                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle(m_szProjektname);
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                
                Dienste.Navigation.OeffneGewerk(Gewerke.Waermepumpe, m_ID_Projekt, m_szProjektname);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {

            List<WErzeugerModel> list = new List<WErzeugerModel>();
            WErzeugerModel werzmodel = new WErzeugerModel();
            list.Add(werzmodel);

            // iU9-W7.4: Die Detailansicht ist die Razor-Komponente
            // WaermepumpeAnlageDialog; Wizard_WPItem ist im selben Schritt GELOESCHT
            // (Regel M1). Die Huelle bearbeitet werzmodel an Ort und Stelle - der
            // frueher hier stehende Rueckschreibblock kopierte 19 Felder von
            // frm_wpitem.item nach list[0], und das war DASSELBE Objekt.
            if (!WaermepumpeAnlageHuelle.Oeffnen(null, werzmodel, m_ID_Projekt)) return;

            if (listView_WP.Name == "listView_WP_Ref") { list[0].ID_Type = WizardItemClass.REF_WP_TYP; }
            else { list[0].ID_Type = WizardItemClass.WP_TYP; }

            list[0].ID_Projekt = m_ID_Projekt;

            WizardCtrl wizctrl = new WizardCtrl();

            wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, list);

            ProjektCtrl projctrl = new ProjektCtrl();
            projctrl.ReadSingle(m_szProjektname);
            projctrl.m_Aenderungsdatum = DateTime.Now;
            projctrl.Update();

            Dienste.Navigation.OeffneGewerk(Gewerke.Waermepumpe, m_ID_Projekt, m_szProjektname);
        }
    }
}
