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
    public partial class Wizard_Referenz : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private StromspeicherCtrl spctrl = new StromspeicherCtrl();
        private BrennstoffModel brstmodel = new BrennstoffModel();
        private BrennstoffCtrl brstctrl = new BrennstoffCtrl();

        public Wizard_Referenz()
        {
            InitializeComponent();
            listBox_SP_DB.Items.Clear();
            listBox_SP.Items.Clear();
            listView_WP.View = View.Details;
            listView_WP.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Vorlauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Rücklauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Betriebsart", -2, HorizontalAlignment.Left);
        }

        private void Wizard_Referenz_Load(object sender, EventArgs e)
        {
            spctrl.ReadAll();
            for (int i = 0; i < spctrl.rows; i++)
            {
                listBox_SP_DB.Items.Add(spctrl.items[i].m_szBezeichner);
            }
            brstctrl.ReadAll("");
            for (int i = 0; i < brstctrl.rows; i++)
            {
                listBox_Kessel_DB.Items.Add(brstctrl.items[i].Name);
            }
        }

        public void SetControls(string projekt)
        {
            WizardParent wizardparent = (WizardParent)getWizardPage();

            if ((wizardparent.wizardmode == WizardParent.WIZARD_MODE_BEARBEITEN) && projekt != "")
            {
                listBox_SP.Items.Clear();
                listBox_Kessel.Items.Clear();

                for (int n = 0; n < wizardparent.list_werzmodel.Count; n++)
                {
                    WErzeugerModel item = new WErzeugerModel();

                    if (wizardparent.list_werzmodel[n].ID_Type == WizardItemClass.REF_SP_TYP)
                    {
                        item.Bezeichner = wizardparent.list_werzmodel[n].Bezeichner;
                        listBox_SP.Items.Add(item.Bezeichner);
                    }
                    if (wizardparent.list_werzmodel[n].ID_Type == WizardItemClass.REF_KESSEL_TYP)
                    {
                        item.Bezeichner = wizardparent.list_werzmodel[n].Bezeichner;
                        listBox_Kessel.Items.Add(item.Bezeichner);
                    }

                }
                if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = 0;
                if (listBox_Kessel.Items.Count > 0) listBox_Kessel.SelectedIndex = 0;
            }
        }

        private Form getWizardPage()
        {
            foreach (Form form in Application.OpenForms)
            {
                if (form.Name == "WizardParent")
                {
                    return form;
                }
            }
            return null;
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            if (listBox_SP_DB.Text == "") return;

            WErzeugerModel model = new WErzeugerModel();
            model.Bezeichner = listBox_SP_DB.Text;
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Stromspeicher where Bezeichner='" + listBox_SP_DB.Text + "'");
            if (!rs.EOF())
            {
                model.ID_SP = (int)rs.Read("ID");
                model.ID_Type = WizardItemClass.REF_SP_TYP; 
            }
            rs.Close();
            
            WizardParent wizardparent = (WizardParent)getWizardPage();
            wizardparent.list_werzmodel.Add(model);
            listBox_SP.Items.Add(listBox_SP_DB.Text);
            if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = listBox_SP.Items.Count-1;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_SP.Text == "") return;
            
            WErzeugerModel model = new WErzeugerModel();
            model.Bezeichner = listBox_SP.Text;
            WizardParent wizardparent = (WizardParent)getWizardPage();
            for (int i = 0; i < wizardparent.list_werzmodel.Count; i++)
            {
                if (wizardparent.list_werzmodel[i].Bezeichner == listBox_SP.Text)
                {
                    wizardparent.list_werzmodel.RemoveAt(i);
                    listBox_SP.Items.Remove(listBox_SP.Text);
                }
            }
            if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = 0;
        }

        private void listBox_SP_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Stromspeicher where Bezeichner='" + listBox_SP.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Typ.Text = (string)rs.Read("Typ");
                textBox_Leistung.Text = rs.Read("Leistung").ToString();
                textBox_Energie.Text = rs.Read("Energie").ToString();
                textBox_Degradation.Text = rs.Read("Degradation").ToString(); ;
                textBox_Ladezustand.Text = rs.Read("Ladezustand").ToString(); ;
            }
            rs.Close();
        }

        private void listBox_Kessel_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from [DB-Heizung] where Name='" + listBox_Kessel_DB.Text + "'");

            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Name");
                textBox_Kesseltyp.Text = BrennstoffCtrl.Brennstoffart[(int)rs.Read("Brennstoff")].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
                textBox_Kesselbeschreibung.Text = (string)rs.Read("Beschreibung");
            }
            rs.Close();
        }

        private void listBox_Kessel_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from [DB-Heizung] where Name='" + listBox_Kessel.Text + "'");

            if (!rs.EOF())
            {
                textBox_Kesselname.Text = (string)rs.Read("Name");
                textBox_Kesseltyp.Text = BrennstoffCtrl.Brennstoffart[(int)rs.Read("Brennstoff")].ToString();
                double kl = (double)rs.Read("Ptherm");
                textBox_Kesselleistung.Text = kl.ToString("F2");
                textBox_Kesselbeschreibung.Text = (string)rs.Read("Beschreibung");
            }
            rs.Close();
        }

        private void btn_Kessel_Hinzu_Click(object sender, EventArgs e)
        {
            if (listBox_Kessel_DB.Text == "") return;
            
            WErzeugerModel model = new WErzeugerModel();
            model.Bezeichner = listBox_Kessel_DB.Text;
            RecordSet rs = new RecordSet();

            rs.Open("select * from [DB-Heizung] where Name='" + listBox_Kessel_DB.Text + "'");
            if (!rs.EOF())
            {
                model.Bezeichner = listBox_Kessel_DB.Text;
                model.ID_Kessel = (int)rs.Read("ID");
                model.ID_Type = WizardItemClass.REF_KESSEL_TYP; 
            }
            rs.Close();
   
            WizardParent wizardparent = (WizardParent)getWizardPage();
            wizardparent.list_werzmodel.Add(model);
            listBox_Kessel.Items.Add(listBox_Kessel_DB.Text);
            if (listBox_Kessel.Items.Count > 0) listBox_Kessel.SelectedIndex = listBox_Kessel.Items.Count-1;
        }

        private void textBox_Kesselleistung_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_Kesselleistung, textBox_Kesselleistung.Text)) { textBox_Kesselleistung.Undo(); }
        }

        private void textBox_Leistung_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Leistung, textBox_Leistung.Text)) { textBox_Leistung.Undo(); }
        }

        private void textBox_Energie_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Energie, textBox_Energie.Text)) { textBox_Energie.Undo(); }

        }

        private void textBox_Degradation_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox_Degradation, textBox_Degradation.Text)) { textBox_Degradation.Undo(); }

        }

        private void textBox_Ladezustand_Validating(object sender, CancelEventArgs e)
        {
            if (!Program.checkInt(textBox_Ladezustand, textBox_Ladezustand.Text)) { textBox_Ladezustand.Undo(); }

        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Wizard_WPItem frm = new Wizard_WPItem();
            frm.ShowDialog();
            if (!frm.CloseWithOK) return;

            WizardParent wizardparent = (WizardParent)getWizardPage();
            frm.item.ID_Type = WizardItemClass.REF_WP_TYP;
            wizardparent.list_werzmodel.Add(frm.item);

            ListViewItem lvitem = new ListViewItem();
      
            lvitem.Text = frm.item.Bezeichner;
            lvitem.SubItems.Add(frm.item.Vorlauf.ToString());
            lvitem.SubItems.Add(frm.item.Ruecklauf.ToString());
            lvitem.SubItems.Add(frm.item.Betriebsart);
            lvitem = listView_WP.Items.Add(lvitem);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void SetWPControls(string projekt)
        {
            WizardParent wizardparent = (WizardParent)getWizardPage();

            if ((wizardparent.wizardmode == WizardParent.WIZARD_MODE_BEARBEITEN) && projekt != "")
            {

                listView_WP.Items.Clear();

                for (int n = 0; n < wizardparent.list_werzmodel.Count; n++)
                {
                    WErzeugerModel item = new WErzeugerModel();
                    ListViewItem lvitem = new ListViewItem();

                    if (wizardparent.list_werzmodel[n].ID_Type == WizardItemClass.REF_WP_TYP)
                    {
                        item.Bezeichner = wizardparent.list_werzmodel[n].Bezeichner;
                        item.Abschaltpunkt = (double)wizardparent.list_werzmodel[n].Abschaltpunkt;
                        item.Betriebsart = (string)wizardparent.list_werzmodel[n].Betriebsart;
                        item.Bivalenter_Betrieb = wizardparent.list_werzmodel[n].Bivalenter_Betrieb;
                        item.Nutzungszeit = wizardparent.list_werzmodel[n].Nutzungszeit;
                        item.Ruecklauf = wizardparent.list_werzmodel[n].Ruecklauf;
                        item.Sperrung = wizardparent.list_werzmodel[n].Sperrung;
                        item.Sperrzeit_bis = wizardparent.list_werzmodel[n].Sperrzeit_bis;
                        item.Sperrzeit_von = wizardparent.list_werzmodel[n].Sperrzeit_von;
                        item.Vorlauf = wizardparent.list_werzmodel[n].Vorlauf;
                        item.Heizstab = wizardparent.list_werzmodel[n].Heizstab;
                        item.Volumen = wizardparent.list_werzmodel[n].Volumen;
                        item.rendeMix = wizardparent.list_werzmodel[n].rendeMix;
                        item.Solaranteil = wizardparent.list_werzmodel[n].Solaranteil;
                        item.ID_WP = wizardparent.list_werzmodel[n].ID_WP;


                        WPCtrl wpctrl = new WPCtrl();
                        wpctrl.ReadAll("ID_WP=" + item.ID_WP);
                        item.Regelung = wpctrl.items[0].Regelung;
                        item.Nennleistung = wpctrl.items[0].Nennleistung;
                        item.Modulkosten = wpctrl.items[0].Modulkosten;
                        item.Baujahr = wpctrl.items[0].Baujahr;
                        item.Beschreibung = wpctrl.items[0].Beschreibung;
                        item.Firma = wpctrl.items[0].Firma;
                        item.Typ = wpctrl.items[0].Typ;

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
                WizardParent wizardparent = (WizardParent)getWizardPage();
                wizardparent.list_werzmodel.RemoveAt(n);
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
                frm.m_werzitemlist = Program.wizardctrl.parentform.list_werzmodel;

                int idwp = 0;
                int index = 0;

                for (index = 0; index < frm.m_werzitemlist.Count; index++)
                {
                    if (frm.m_werzitemlist[index].Bezeichner == textBox_WP.Text)
                    {
                        idwp = frm.m_werzitemlist[index].ID_WP;
                        break;
                    }
                }

                if (idwp > 0)
                {
                    WPCtrl wpctrl = new WPCtrl();
                    wpctrl.ReadAll("ID_WP=" + idwp);
                    frm.m_werzitemlist[n].Regelung = wpctrl.items[0].Regelung;
                    frm.m_werzitemlist[n].Nennleistung = wpctrl.items[0].Nennleistung;
                    frm.m_werzitemlist[n].Modulkosten = wpctrl.items[0].Modulkosten;
                    frm.m_werzitemlist[n].Baujahr = wpctrl.items[0].Baujahr;
                    frm.m_werzitemlist[n].Beschreibung = wpctrl.items[0].Beschreibung;
                    frm.m_werzitemlist[n].Firma = wpctrl.items[0].Firma;
                    frm.m_werzitemlist[n].Typ = wpctrl.items[0].Typ;

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
                frm.m_werzitemlist = Program.wizardctrl.parentform.list_werzmodel;

                int idwp = 0;
                int index = 0;
                
                for (index = 0; index < frm.m_werzitemlist.Count; index++)
                {
                    if (frm.m_werzitemlist[index].Bezeichner == textBox_WP.Text)
                    {
                        idwp = frm.m_werzitemlist[index].ID_WP;
                        break;
                    }
                }

                WPCtrl wpctrl = new WPCtrl();
                wpctrl.ReadAll("ID_WP=" + frm.m_werzitemlist[index].ID_WP);
                frm.m_werzitemlist[n].Regelung = wpctrl.items[0].Regelung;
                frm.m_werzitemlist[n].Nennleistung = wpctrl.items[0].Nennleistung;
                frm.m_werzitemlist[n].Modulkosten = wpctrl.items[0].Modulkosten;
                frm.m_werzitemlist[n].Baujahr = wpctrl.items[0].Baujahr;
                frm.m_werzitemlist[n].Beschreibung = wpctrl.items[0].Beschreibung;
                frm.m_werzitemlist[n].Firma = wpctrl.items[0].Firma;
                frm.m_werzitemlist[n].Typ = wpctrl.items[0].Typ;
                frm.SetControls(index);
                frm.ShowDialog();
                if (!frm.CloseWithOK) return;

                WizardParent wizardparent = (WizardParent)getWizardPage();
                wizardparent.list_werzmodel[n] = frm.m_werzitemlist[n];

                ListViewItem lvitem;
                lvitem = listView_WP.Items[n];
                lvitem.Text = frm.item.Bezeichner;
                lvitem.SubItems[1].Text = frm.item.Vorlauf.ToString();
                lvitem.SubItems[2].Text = frm.item.Ruecklauf.ToString();
                lvitem.SubItems[3].Text = frm.item.Betriebsart;
            }
        }

    }
}
