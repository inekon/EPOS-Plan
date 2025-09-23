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
    public partial class Form_Gebaeude : Form
    {
        private List<string> list_geb = new List<string>() { "vor 1919", "1919 bis 1948", "1949 bis 1957", "1958 bis 1968", "1969 bis 1978", "1979 bis 1983", "1984 bis 1994", "1995 bis 2000", "Niedrigenergiebauweise", "Passivhaus", "EnEv 2007", "Eff. 70 (EnEV 2007)", "EnEV 2009", "Eff. 70 (EnEV 2009)", "Eff. 55 (EnEV 2009)", "EnEV 2014", "EnEV 2016", "Eff. 100 (EnEV 2016)", "Eff. 155 (EnEV 2016)", "BEG 55", "BEG 40" };
        private const string szFilterNichtWohngebäude = "Wohngebaeude_Nicht_Wohngebaeude='Nicht Wohngebaeude'";
        private const string szFilterWohngebäude = "Wohngebaeude_Nicht_Wohngebaeude='Wohngebaeude'";
        private string szFilterOption;
        private int startindex = 1000;
        private int zprojGeb_id = 0;

        public Form_Gebaeude()
        {
            InitializeComponent();
            GebaeudeCtrl ctrl = new GebaeudeCtrl();
            listBox_Gebaeude_DB.Items.Clear();
            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_Gebaeude_DB.Items.Add(ctrl.items[i].Gebaeudename);
            }
            listView_Gebaeude.View = View.Details;
            listView_Gebaeude.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Gebaeude.Columns.Add("ID", -2, HorizontalAlignment.Left);
            listView_Gebaeude.Columns[0].Width = listView_Gebaeude.ClientRectangle.Width; 
        }

        public void SetControls(string szProjekt)
        {
            WizardParent wizardparent = (WizardParent)getWizardPage();
            Z_ProjGebCtrl ctrl = new Z_ProjGebCtrl();
            GebaeudeCtrl ctrl_geb = new GebaeudeCtrl();
            RecordSet rs = new RecordSet();

            szFilterOption = szFilterWohngebäude;

            listView_Gebaeude.Items.Clear();
            for (int i = 0; i < wizardparent.list_gebmodel.Count; i++)
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = wizardparent.list_gebmodel[i].Gebaeudename;
                lvitem.SubItems.Add(wizardparent.list_gebmodel[i].ID_Z.ToString());
                listView_Gebaeude.Items.Add(lvitem);
            }
       //     listView_Gebaeude.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
       //     listView_Gebaeude.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            listBox_Gebaeude_DB.Items.Clear();
            ctrl_geb.ReadAll();
            for (int i = 0; i < ctrl_geb.rows; i++)
            {
                listBox_Gebaeude_DB.Items.Add(ctrl_geb.items[i].Gebaeudename);
            }
            if (listView_Gebaeude.Items.Count > 0) listView_Gebaeude.Items[0].Selected = true;

            comboBox_Baujahr.Items.Add("Alle");
            for (int i = 0; i < list_geb.Count; i++)
            {
                comboBox_Baujahr.Items.Add(list_geb[i]);
            }
            comboBox_Baujahr.SelectedIndex = 0;

            comboBox_Gebäudeart.Items.Add("Alle");
            rs.Open("SELECT * from Abfrage_Gebaeudearten where " + szFilterWohngebäude);
            while (!rs.EOF())
            {
                if (!rs.Next()) break;
                string szGebArt = (string)rs.Read("Gebaeudeart");
                comboBox_Gebäudeart.Items.Add(szGebArt);
                
            }
            rs.Close();
            comboBox_Gebäudeart.SelectedIndex = 0;
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

        private void radioBtn_Sonstige_CheckedChanged(object sender, EventArgs e)
        {
            GebaeudeCtrl ctrl_geb = new GebaeudeCtrl();
            RecordSet rs = new RecordSet();

            szFilterOption = szFilterNichtWohngebäude;

            listBox_Gebaeude_DB.Items.Clear();
            ctrl_geb.ReadAll(szFilterNichtWohngebäude);
            listBox_Gebaeude_DB.Items.Add("Alle");
            for (int i = 0; i < ctrl_geb.rows; i++)
            {
                listBox_Gebaeude_DB.Items.Add(ctrl_geb.items[i].Gebaeudename);
            }

            comboBox_Gebäudeart.Enabled = false;
            comboBox_Gebäudeart.Items.Clear();
            comboBox_Gebäudeart.Items.Add("Alle");

            rs.Open("SELECT * from Abfrage_Gebaeudearten where " + szFilterNichtWohngebäude);
            while (!rs.EOF())
            {
                if (!rs.Next()) break;
                string szGebArt = (string)rs.Read("Gebaeudeart");
                comboBox_Gebäudeart.Items.Add(szGebArt);
     
            }
            rs.Close();
            comboBox_Gebäudeart.Enabled = true;
            comboBox_Gebäudeart.SelectedIndex = 0;
        }

        private void radioBtn_Wohngebäude_CheckedChanged(object sender, EventArgs e)
        {
            GebaeudeCtrl ctrl_geb = new GebaeudeCtrl();
            RecordSet rs = new RecordSet();

            szFilterOption = szFilterWohngebäude;
            listBox_Gebaeude_DB.Items.Clear();
            ctrl_geb.ReadAll();

            listBox_Gebaeude_DB.Items.Add("Alle");
            for (int i = 0; i < ctrl_geb.rows; i++)
            {
                listBox_Gebaeude_DB.Items.Add(ctrl_geb.items[i].Gebaeudename);
            }

            comboBox_Gebäudeart.Enabled = false;
            comboBox_Gebäudeart.Items.Clear();
            comboBox_Gebäudeart.Items.Add("Alle");
            
            rs.Open("SELECT * from Abfrage_Gebaeudearten where " + szFilterWohngebäude);
            while (!rs.EOF())
            {
                if (!rs.Next()) break;
                string szGebArt = (string)rs.Read("Gebaeudeart");
                comboBox_Gebäudeart.Items.Add(szGebArt);
    
            }
            rs.Close();

            comboBox_Gebäudeart.Enabled = true;
            comboBox_Gebäudeart.SelectedIndex = 0;
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            WizardParent wizardparent = (WizardParent)getWizardPage();
            Z_ProjGebModel model = new Z_ProjGebModel();

            if (listBox_Gebaeude_DB.Text == "") return;

            string sql = "SELECT * from [DB-Gebaeude] where [DB-Gebaeude].Gebaeudename='" + listBox_Gebaeude_DB.Text + "'";
            rs.Open(sql);

            if (!rs.EOF())
            {
                rs.Next();
                model.ID_Z = startindex++; // noch nicht gespeichert, also noch unbekannt
                model.ID_Gebaeude = (int)rs.Read("ID");
                model.ID_Projekt = wizardparent.projektID;
                model.Wohnflaeche = (double)rs.Read("Wohnflaeche_gesamt");
                model.Einheit = "Wohnfläche [m²]";
                model.Jahresnutzungsgrad = 1;
                model.DezentralWarmwasser = false;
                model.Gebaeudename = listBox_Gebaeude_DB.Text;
  
                wizardparent.list_gebmodel.Add(model);

                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = listBox_Gebaeude_DB.Text;
                lvitem.SubItems.Add(model.ID_Z.ToString());
                listView_Gebaeude.Items.Add(lvitem);
          
                if (listView_Gebaeude.Items.Count > 0) listView_Gebaeude.Items[listView_Gebaeude.Items.Count-1].Selected=true;
            }
            rs.Close();
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listView_Gebaeude.Text == "") return;
            Z_ProjGebModel model = new Z_ProjGebModel();
            model.Gebaeudename = listView_Gebaeude.Text;

            WizardParent wizardparent = (WizardParent)getWizardPage();
            for (int i = 0; i < wizardparent.list_gebmodel.Count; i++)
            {
                if (wizardparent.list_gebmodel[i].ID_Gebaeude == Int32.Parse(textBox_ID_Gebaeude.Text))
                {
                    wizardparent.list_gebmodel.RemoveAt(i);

                    ListView.SelectedIndexCollection indexes = listView_Gebaeude.SelectedIndices;
                    if (indexes.Count > 0)
                    {
                        ListViewItem lvitem = listView_Gebaeude.Items[indexes[0]];
                        listView_Gebaeude.Items.Remove(lvitem);
                    }
                    break;
                }
            }
            if (listView_Gebaeude.Items.Count > 0) listView_Gebaeude.Items[0].Selected = true;
        }

        private void listBox_Geb_SelectedIndexChanged(object sender, EventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_Gebaeude.SelectedIndices;
            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_Gebaeude.Items[indexes[0]];
                textBox_Gebäudename.Text = lvitem.Text;
                zprojGeb_id = Int32.Parse(lvitem.SubItems[1].Text);  
            }
            else return;

            int id = GetIDGebaeude(textBox_Gebäudename.Text);
            textBox_ID_Gebaeude.Text = id.ToString() ;
            WizardParent wizardparent = (WizardParent)getWizardPage();
            for (int i = 0; i < wizardparent.list_gebmodel.Count; i++)
            {
                if (wizardparent.list_gebmodel[i].Gebaeudename == textBox_Gebäudename.Text &&
                    wizardparent.list_gebmodel[i].ID_Z == zprojGeb_id)
                {
                    textBox_Jahresnutzungsgrad.Text = wizardparent.list_gebmodel[i].Jahresnutzungsgrad.ToString();
                    textBox_TypEinheit.Text = wizardparent.list_gebmodel[i].Einheit;
                    textBox_Wohnflaeche.Text = wizardparent.list_gebmodel[i].Wohnflaeche.ToString();
                    checkBox_dezWarmwasser.Checked = wizardparent.list_gebmodel[i].DezentralWarmwasser;
                    textBox_Gebaeudeart.Text = wizardparent.list_gebmodel[i].Gebaeudeart;
                    textBox_Beschreibung.Text = wizardparent.list_gebmodel[i].Beschreibung;  
                    break;
                }
            }
        }

        private int GetIDGebaeude(string szGebaeude)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from [DB-Gebaeude] where Gebaeudename='" + szGebaeude + "'");
            rs.Next();
            int ret = (int)rs.Read("ID");
            rs.Close();
            return ret;
        }

        private void comboBox_Gebäudeart_SelectedIndexChanged(object sender, EventArgs e)
        {
            GebaeudeCtrl ctrl_geb = new GebaeudeCtrl();
            int index = 0;

            listBox_Gebaeude_DB.Items.Clear();

            if (comboBox_Gebäudeart.Text == "Alle" && comboBox_Baujahr.Text == "Alle")
            {
                ctrl_geb.ReadAll(szFilterOption);
            }
            else
            {
                if (comboBox_Gebäudeart.Text == "Alle")
                {
                    for (int i = 0; i < list_geb.Count; i++)
                    {
                        if (list_geb[i] == comboBox_Baujahr.Text) { index = i; break; }
                    }
                    ctrl_geb.ReadAll("Baualtersklasse='" + Convert.ToChar('A' + index) + "' and " + szFilterOption);
                }
                else
                {
                    for (int i = 0; i < list_geb.Count; i++)
                    {
                        if (list_geb[i] == comboBox_Baujahr.Text) { index = i; break; }
                    }
                    if (comboBox_Baujahr.Text == "Alle")
                    {
                        ctrl_geb.ReadAll("Gebaeudeart='" + comboBox_Gebäudeart.Text + "'");
                    }
                    else
                    {
                        ctrl_geb.ReadAll("Gebaeudeart='" + comboBox_Gebäudeart.Text + "' and Baualtersklasse='" + Convert.ToChar('A' + index) + "' and " + szFilterOption);
                    }
                }
            }
            for (int i = 0; i < ctrl_geb.rows; i++)
            {
                listBox_Gebaeude_DB.Items.Add(ctrl_geb.items[i].Gebaeudename);
            }
        }

        private void comboBox_Baujahr_SelectedIndexChanged(object sender, EventArgs e)
        {
            GebaeudeCtrl ctrl_geb = new GebaeudeCtrl();
            int index = 0;

            listBox_Gebaeude_DB.Items.Clear();

            if (comboBox_Gebäudeart.Text == "Alle" && comboBox_Baujahr.Text == "Alle")
            {
                ctrl_geb.ReadAll(szFilterOption);
            }
            else
            {
                if (comboBox_Baujahr.Text == "Alle")
                {
                    ctrl_geb.ReadAll("Gebaeudeart='" + comboBox_Gebäudeart.Text + "' and " + szFilterOption);
                }
                else
                {
                    for (int i = 0; i < list_geb.Count; i++)
                    {
                        if (list_geb[i] == comboBox_Baujahr.Text) { index = i; break; }
                    }

                    if (comboBox_Gebäudeart.Text == "Alle")
                    {
                        ctrl_geb.ReadAll("Baualtersklasse='" + Convert.ToChar('A' + index) + "' and " + szFilterOption);
                    }
                    else
                    {
                        ctrl_geb.ReadAll("Gebaeudeart='" + comboBox_Gebäudeart.Text + "' and Baualtersklasse='" + Convert.ToChar('A' + index) + "' and " + szFilterOption);
                    }
                }
            }

            for (int i = 0; i < ctrl_geb.rows; i++)
            {
                listBox_Gebaeude_DB.Items.Add(ctrl_geb.items[i].Gebaeudename);
            }


        }

        private void btn_Aendern_Click(object sender, EventArgs e)
        {
            if (textBox_Gebäudename.Text == "") return;

            Form_GebWohnflaeche frm = new Form_GebWohnflaeche();
            frm.ctrl.Gebaeudename = textBox_Gebäudename.Text;
            frm.ctrl.Beschreibung = textBox_Beschreibung.Text;
            frm.ctrl.Gebaeudeart = textBox_Gebaeudeart.Text;
            frm.ctrl.Baualtersklasse = textBox_Baujahr.Text;
            string Baualtersklasse = textBox_Baujahr.Text.Substring(0, 1);
            int index = (int)Baualtersklasse[0] -(int)'A';
            if (index < 0) index = 0;

            frm.ctrl.Baualtersklasse = list_geb[index];
            frm.ctrl.Wohnflaeche = double.Parse(textBox_Wohnflaeche.Text);
            frm.ctrl.Einheit = textBox_TypEinheit.Text;
            frm.ctrl.DezentralWarmwasser = checkBox_dezWarmwasser.Checked;
            frm.ctrl.Jahresnutzungsgrad = double.Parse(textBox_Jahresnutzungsgrad.Text);

            frm.SetControls();
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                WizardParent wizardparent = (WizardParent)getWizardPage();
                for (int i = 0; i < wizardparent.list_gebmodel.Count; i++)
                {
                    if (wizardparent.list_gebmodel[i].ID_Gebaeude == Int32.Parse(textBox_ID_Gebaeude.Text) &&
                    wizardparent.list_gebmodel[i].ID_Z == zprojGeb_id)
                    {
                        Z_ProjGebModel model;
                        model = wizardparent.list_gebmodel[i];
                        model.ID_Z = zprojGeb_id;
                        model.ID_Gebaeude = wizardparent.list_gebmodel[i].ID_Gebaeude;
                        model.ID_Projekt = wizardparent.projektID;
                        model.Wohnflaeche = frm.ctrl.Wohnflaeche;
                        model.Einheit = frm.ctrl.Einheit;
                        model.Jahresnutzungsgrad = frm.ctrl.Jahresnutzungsgrad;
                        model.DezentralWarmwasser = frm.ctrl.DezentralWarmwasser;
                        wizardparent.list_gebmodel[i] = model;

                        textBox_Jahresnutzungsgrad.Text = model.Jahresnutzungsgrad.ToString();
                        textBox_TypEinheit.Text = model.Einheit;
                        textBox_Wohnflaeche.Text = model.Wohnflaeche.ToString();
                        checkBox_dezWarmwasser.Checked = model.DezentralWarmwasser;
                        break;
                    }
                }
            }
        }

    }
}

