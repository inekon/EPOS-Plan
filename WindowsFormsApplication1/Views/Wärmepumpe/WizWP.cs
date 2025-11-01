using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class WizWP : Form
    {
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        
        public WizWP()
        {
            InitializeComponent();
 
            listView_WP.View = View.Details;
            listView_WP.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Vorlauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Rücklauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Betriebsart", -2, HorizontalAlignment.Left);
        }

        public void SetControls(string projekt)
        {
                
                listView_WP.Items.Clear();
   
                for (int n = 0; n < list_werzmodel.Count; n++)
                {
                    WErzeugerModel item = new WErzeugerModel();
                    ListViewItem lvitem = new ListViewItem();

                    if (list_werzmodel[n].ID_Type == WizardItemClass.WP_TYP)
                    {
                        item.Bezeichner = list_werzmodel[n].Bezeichner;
                        item.Abschaltpunkt = (double)list_werzmodel[n].Abschaltpunkt;
                        item.Betriebsart = (string)list_werzmodel[n].Betriebsart;
                        item.Bivalenter_Betrieb = list_werzmodel[n].Bivalenter_Betrieb;
                        item.Nutzungszeit = list_werzmodel[n].Nutzungszeit;
                        item.Ruecklauf = list_werzmodel[n].Ruecklauf;
                        item.Sperrung = list_werzmodel[n].Sperrung;
                        item.Sperrzeit_bis = list_werzmodel[n].Sperrzeit_bis;
                        item.Sperrzeit_von = list_werzmodel[n].Sperrzeit_von;
                        item.Vorlauf = list_werzmodel[n].Vorlauf;
                        item.Heizstab = list_werzmodel[n].Heizstab;
                        item.Volumen = list_werzmodel[n].Volumen;
                        item.rendeMix = list_werzmodel[n].rendeMix;
                        item.Solaranteil = list_werzmodel[n].Solaranteil;
                        item.ID_WP = list_werzmodel[n].ID_WP;


                        WPCtrl wpctrl = new WPCtrl();
                        wpctrl.ReadAll("ID_WP=" + item.ID_WP);
                        item.Regelung = wpctrl.items[0].Regelung;
                        item.Nennleistung = wpctrl.items[0].Nennleistung;
                        item.Modulkosten = wpctrl.items[0].Modulkosten;
                        item.Baujahr = wpctrl.items[0].Baujahr;
                        item.Beschreibung = wpctrl.items[0].Beschreibung;
                        item.Firma = wpctrl.items[0].Firma;
                        item.Typ = wpctrl.items[0].Typ;
                        item.Heizung = wpctrl.items[0].Heizung;

                        lvitem.Text = item.Bezeichner;
                        lvitem.SubItems.Add(item.Vorlauf.ToString());
                        lvitem.SubItems.Add(item.Ruecklauf.ToString());
                        lvitem.SubItems.Add(item.Betriebsart);

                        listView_WP.Items.Add(lvitem);
                    }
                }
                listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void listView_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_WP.Items[indexes[0]];
                textBox_WP.Text = lvitem.Text;
            }
        }

        private void btn_Löschen_Click(object sender, EventArgs e)
        {
            string wpname;

            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;

            if (indexes.Count > 0)
            {
                int n = indexes[0];
                wpname = listView_WP.Items[n].SubItems[0].Text;
                listView_WP.Items[indexes[0]].Remove();
                list_werzmodel.RemoveAt(n);
            }
        }

        private void btn_Ansicht_Click(object sender, EventArgs e)
        {
            WErzeugerModel item = new WErzeugerModel();
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;

            if (indexes.Count > 0)
            {
                int n = indexes[0];
                Wizard_WPItem frm = new Wizard_WPItem(textBox_WP.Text);
                frm.m_werzitemlist = list_werzmodel;
                
                int idwp = 0;
                int index = 0;
                
                for (index = 0; index < frm.m_werzitemlist.Count; index++)
                {
                    if (frm.m_werzitemlist[index].Bezeichner == textBox_WP.Text && frm.m_werzitemlist[index].ID_Type == 1)
                    {
                        idwp = frm.m_werzitemlist[index].ID_WP;
                        break;
                    }
                }

                if (idwp > 0)
                {
                    WPCtrl wpctrl = new WPCtrl();
                    wpctrl.ReadAll("ID_WP=" + idwp);
                    frm.m_werzitemlist[index].Regelung = wpctrl.items[0].Regelung;
                    frm.m_werzitemlist[index].Nennleistung = wpctrl.items[0].Nennleistung;
                    frm.m_werzitemlist[index].Modulkosten = wpctrl.items[0].Modulkosten;
                    frm.m_werzitemlist[index].Baujahr = wpctrl.items[0].Baujahr;
                    frm.m_werzitemlist[index].Beschreibung = wpctrl.items[0].Beschreibung;
                    frm.m_werzitemlist[index].Firma = wpctrl.items[0].Firma;
                    frm.m_werzitemlist[index].Typ = wpctrl.items[0].Typ;
                    frm.m_werzitemlist[index].Heizung = wpctrl.items[0].Heizung;

                    frm.SetControls(index);
                    frm.ShowDialog();
                }
             }
        }

        private void btn_Uebernehmen_Click(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            if (indexes.Count > 0)
            {
                int n = indexes[0];
                Wizard_WPItem frm = new Wizard_WPItem(textBox_WP.Text);
                frm.m_werzitemlist = list_werzmodel;

                int idwp = 0;
                int index = 0;
                
                for (index = 0; index < frm.m_werzitemlist.Count; index++)
                {
                    if (frm.m_werzitemlist[index].Bezeichner == textBox_WP.Text && frm.m_werzitemlist[index].ID_Type == 1)
                    {
                        idwp = frm.m_werzitemlist[index].ID_WP;
                        break;
                    }
                }

                WPCtrl wpctrl = new WPCtrl();
                wpctrl.ReadAll("ID_WP=" + frm.m_werzitemlist[index].ID_WP);
                frm.m_werzitemlist[index].Regelung = wpctrl.items[0].Regelung;
                frm.m_werzitemlist[index].Nennleistung = wpctrl.items[0].Nennleistung;
                frm.m_werzitemlist[index].Modulkosten = wpctrl.items[0].Modulkosten;
                frm.m_werzitemlist[index].Baujahr = wpctrl.items[0].Baujahr;
                frm.m_werzitemlist[index].Beschreibung = wpctrl.items[0].Beschreibung;
                frm.m_werzitemlist[index].Firma = wpctrl.items[0].Firma;
                frm.m_werzitemlist[index].Typ = wpctrl.items[0].Typ;
                frm.m_werzitemlist[index].Heizung = wpctrl.items[0].Heizung;
                frm.SetControls(index);
                frm.ShowDialog();
                if (!frm.CloseWithOK) return;
                
                list_werzmodel[index] = frm.m_werzitemlist[index];

                ListViewItem lvitem;
                lvitem = listView_WP.Items[n];
                lvitem.Text = frm.item.Bezeichner;
                lvitem.SubItems[1].Text = frm.item.Vorlauf.ToString();
                lvitem.SubItems[2].Text = frm.item.Ruecklauf.ToString();
                lvitem.SubItems[3].Text = frm.item.Betriebsart;
            }
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Wizard_WPItem frm = new Wizard_WPItem();
            frm.ShowDialog();
            if (!frm.CloseWithOK) return;
            frm.item.ID_Type = 1; 

            list_werzmodel.Add(frm.item);

            ListViewItem lvitem = new ListViewItem();
      
            lvitem.Text = frm.item.Bezeichner;
            lvitem.SubItems.Add(frm.item.Vorlauf.ToString());
            lvitem.SubItems.Add(frm.item.Ruecklauf.ToString());
            lvitem.SubItems.Add(frm.item.Betriebsart);
            lvitem = listView_WP.Items.Add(lvitem);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void listView_WP_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_WP.Items[indexes[0]];
                Wizard_WPItem frm_wpitem = new Wizard_WPItem(lvitem.Text);
                WErzeugerCtrl ctrl = new WErzeugerCtrl();
                List<WErzeugerModel> list = new List<WErzeugerModel>();
                WPCtrl wpctrl = new WPCtrl();

                ctrl.ReadAllFilter("Bezeichner='" + lvitem.Text + "'");
                wpctrl.ReadAll("ID_WP=" + ctrl.items[0].ID_WP);
                ctrl.items[0].Regelung = wpctrl.items[0].Regelung;
                ctrl.items[0].Nennleistung = wpctrl.items[0].Nennleistung;
                ctrl.items[0].Modulkosten = wpctrl.items[0].Modulkosten;
                ctrl.items[0].Baujahr = wpctrl.items[0].Baujahr;
                ctrl.items[0].Beschreibung = wpctrl.items[0].Beschreibung;
                ctrl.items[0].Firma = wpctrl.items[0].Firma;
                ctrl.items[0].Typ = wpctrl.items[0].Typ;

                list.Add(ctrl.items[0]);
                frm_wpitem.m_werzitemlist = list;
                frm_wpitem.SetControls(0);
                frm_wpitem.ShowDialog();
            }
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
