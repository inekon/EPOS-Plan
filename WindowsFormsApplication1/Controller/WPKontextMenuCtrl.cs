using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;

namespace WindowsFormsApplication1
{
    class WPKontextMenuCtrl : Form
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
                Wizard_WPItem frm_wpitem = new Wizard_WPItem(lvitem.Text);
                WErzeugerCtrl werzctrl = new WErzeugerCtrl();
                List<WErzeugerModel> list = new List<WErzeugerModel>();
                WPCtrl wpctrl = new WPCtrl();

                werzctrl.ReadAllFilter("Bezeichner='" + lvitem.Text + "' and ID_Projekt=" + m_ID_Projekt + " and ID=" + lvitem.SubItems[4].Text  + " and (ID_Type=" + WizardItemClass.WP_TYP + " Or ID_Type=" + WizardItemClass.REF_WP_TYP + ")");
                wpctrl.ReadAll("ID_WP=" + werzctrl.items[0].ID_WP);
                werzctrl.items[0].Regelung = wpctrl.items[0].Regelung;
                werzctrl.items[0].Nennleistung = wpctrl.items[0].Nennleistung;
                werzctrl.items[0].Modulkosten = wpctrl.items[0].Modulkosten;
                werzctrl.items[0].Baujahr = wpctrl.items[0].Baujahr;
                werzctrl.items[0].Beschreibung = wpctrl.items[0].Beschreibung;
                werzctrl.items[0].Firma = wpctrl.items[0].Firma;
                werzctrl.items[0].Typ = wpctrl.items[0].Typ;

                list.Add(werzctrl.items[0]);
                frm_wpitem.m_werzitemlist = list;
                frm_wpitem.SetControls(0);
                frm_wpitem.ShowDialog();
            }
        }

        private void ContextMenuItemLoeschen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            Form_Gebaeude frm = new Form_Gebaeude();

            if (indexes.Count > 0)
            {
                ListViewItem item = listView_WP.Items[indexes[0]];
                listView_WP.Items[indexes[0]].Remove();
                wizctrl.Del_Projekt_ID_Waermeerzeuger(m_ID_Projekt, Int32.Parse(item.SubItems[4].Text));

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                Program.mainfrm.SetWPControl(m_szProjektname);
            }
        }

        private void ContextMenuItemBearbeiten_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_WP.Items[indexes[0]];
                Wizard_WPItem frm_wpitem = new Wizard_WPItem(lvitem.Text);
                WErzeugerCtrl werzctrl = new WErzeugerCtrl();
                List<WErzeugerModel> list = new List<WErzeugerModel>();
                List<WErzeugerModel> list_alle = new List<WErzeugerModel>();
                WPCtrl wpctrl = new WPCtrl();

               // werzctrl.ReadAllFilter("Bezeichner='" + lvitem.Text + "' and ID_Projekt=" + m_ID_Projekt + " and ID=" + lvitem.SubItems[4].Text + " and (ID_Type=" + WizardItemClass.WP_TYP + " Or ID_Type=" + WizardItemClass.REF_WP_TYP + ")");
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and (ID_Type=" + WizardItemClass.WP_TYP + " Or ID_Type=" + WizardItemClass.REF_WP_TYP + ")");
                int index=0;
                
                for (int i = 0; i < werzctrl.rows; i++)
                {
                    if (werzctrl.items[i].Bezeichner == lvitem.Text && werzctrl.items[i].ID == Int32.Parse(lvitem.SubItems[4].Text))
                    {
                        // WP Kenndaten lesen
                        wpctrl.ReadAll("ID_WP=" + werzctrl.items[i].ID_WP);
                        werzctrl.items[i].Regelung = wpctrl.items[0].Regelung;
                        werzctrl.items[i].Nennleistung = wpctrl.items[0].Nennleistung;
                        werzctrl.items[i].Modulkosten = wpctrl.items[0].Modulkosten;
                        werzctrl.items[i].Baujahr = wpctrl.items[0].Baujahr;
                        werzctrl.items[i].Beschreibung = wpctrl.items[0].Beschreibung;
                        werzctrl.items[i].Firma = wpctrl.items[0].Firma;
                        werzctrl.items[i].Typ = wpctrl.items[0].Typ;
                        werzctrl.items[i].ID_Type = werzctrl.items[0].ID_Type;
                        werzctrl.items[i].Heizung = wpctrl.items[0].Heizung;
                        list.Add(werzctrl.items[i]);
                        index = i;
                    }
                    list_alle.Add(werzctrl.items[i]);
                }
                frm_wpitem.m_werzitemlist = list;

                frm_wpitem.SetControls(0);
                frm_wpitem.ShowDialog();
                if (!frm_wpitem.CloseWithOK) return;

                // Datenbank aktualisieren
                list_alle[index].Bezeichner = frm_wpitem.item.Bezeichner; //(string)rs.Read("WPName");
                list_alle[index].Abschaltpunkt = (double)frm_wpitem.item.Abschaltpunkt;
                list_alle[index].Betriebsart = (string)frm_wpitem.item.Betriebsart;
                list_alle[index].Bivalenter_Betrieb = frm_wpitem.item.Bivalenter_Betrieb;
                list_alle[index].Nutzungszeit = frm_wpitem.item.Nutzungszeit;
                list_alle[index].Ruecklauf = frm_wpitem.item.Ruecklauf;
                list_alle[index].Sperrung = frm_wpitem.item.Sperrung;
                list_alle[index].Sperrzeit_bis = frm_wpitem.item.Sperrzeit_bis;
                list_alle[index].Sperrzeit_von = frm_wpitem.item.Sperrzeit_von;
                list_alle[index].Vorlauf = frm_wpitem.item.Vorlauf;
                list_alle[index].Heizstab = frm_wpitem.item.Heizstab;
                list_alle[index].Volumen = frm_wpitem.item.Volumen;
                list_alle[index].rendeMix = frm_wpitem.item.rendeMix;
                list_alle[index].Solaranteil = frm_wpitem.item.Solaranteil;
                list_alle[index].Baujahr = frm_wpitem.item.Baujahr;
                list_alle[index].Regelung = frm_wpitem.item.Leistungsstufen;
                list_alle[index].Typ = frm_wpitem.item.Typ;
                list_alle[index].Firma = frm_wpitem.item.Firma;
                list_alle[index].Modulkosten = frm_wpitem.item.Modulkosten;
                list_alle[index].Nennleistung = frm_wpitem.item.Nennleistung;
                list_alle[index].ID_Type = frm_wpitem.item.ID_Type;

               // frm_wpitem.m_werzitemlist = list;

                WizardCtrl wizctrl = new WizardCtrl();

                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, list_alle);

                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
                
                Program.mainfrm.SetWPControl(m_szProjektname);
            }
        }

        private void ContextMenuItemNeu_Click(object sender, EventArgs e)
        {

            Wizard_WPItem frm_wpitem = new Wizard_WPItem();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            List<WErzeugerModel> list = new List<WErzeugerModel>();
            WErzeugerModel werzmodel = new WErzeugerModel();

            list.Add(werzmodel);
            frm_wpitem.m_werzitemlist = list;
            frm_wpitem.SetControls(0);
            frm_wpitem.ShowDialog();
            if (!frm_wpitem.CloseWithOK) return;

            if (listView_WP.Name == "listView_WP_Ref") { list[0].ID_Type = WizardItemClass.REF_WP_TYP; }
            else { list[0].ID_Type = WizardItemClass.WP_TYP; }
            
            list[0].ID = frm_wpitem.item.ID;
            list[0].ID_WP = frm_wpitem.item.ID_WP;
            list[0].ID_Projekt = m_ID_Projekt;
            list[0].Bezeichner = frm_wpitem.item.Bezeichner; //(string)rs.Read("WPName");
            list[0].Abschaltpunkt = (double)frm_wpitem.item.Abschaltpunkt;
            list[0].Betriebsart = (string)frm_wpitem.item.Betriebsart;
            list[0].Bivalenter_Betrieb = frm_wpitem.item.Bivalenter_Betrieb;
            list[0].Nutzungszeit = frm_wpitem.item.Nutzungszeit;
            list[0].Ruecklauf = frm_wpitem.item.Ruecklauf;
            list[0].Sperrung = frm_wpitem.item.Sperrung;
            list[0].Sperrzeit_bis = frm_wpitem.item.Sperrzeit_bis;
            list[0].Sperrzeit_von = frm_wpitem.item.Sperrzeit_von;
            list[0].Vorlauf = frm_wpitem.item.Vorlauf;
            list[0].Heizstab = frm_wpitem.item.Heizstab;
            list[0].Volumen = frm_wpitem.item.Volumen;
            list[0].rendeMix = frm_wpitem.item.rendeMix;
            list[0].Solaranteil = frm_wpitem.item.Solaranteil;

            frm_wpitem.m_werzitemlist = list;

            WizardCtrl wizctrl = new WizardCtrl();

            wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, list);

            ProjektCtrl projctrl = new ProjektCtrl();
            projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
            projctrl.m_Aenderungsdatum = DateTime.Now;
            projctrl.Update();

            Program.mainfrm.SetWPControl(m_szProjektname);
        }
    }
}
