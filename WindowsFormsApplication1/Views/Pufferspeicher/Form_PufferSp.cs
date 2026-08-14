using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PufferSp : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private PufferSpStammCtrl pufferspctrl = new PufferSpStammCtrl();
        public List<WErzeugerModel> list_pufferspmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.PUFFER_TYP;
        public int m_ID_Projekt = 0;
        int startindex = 100000;
        private bool m_bWizard = false;
        private WizardParent wizardparent = null;

        public Form_PufferSp ()
        {
            InitializeComponent();
            listBox_Pufferspeicher_DB.Items.Clear();
            listBox_Pufferspeicher.Items.Clear();
        }

        public void SetControls(int IDProjekt, bool bWizard = false)
        {
            m_ID_Projekt = IDProjekt;
            if (bWizard)
            {
                m_bWizard = true;
                btn_OK.Visible = false;
                btn_Abbrechen.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                wizardparent = (WizardParent)getWizardPage();
                list_pufferspmodel = wizardparent.list_werzmodel;
            }
            listBox_Pufferspeicher.Items.Clear();
            for (int i = 0; i < list_pufferspmodel.Count; i++)
            {
                if (list_pufferspmodel[i].ID_Type == WizardItemClass.PUFFER_TYP)
                {
                    listBox_Pufferspeicher.Items.Add(list_pufferspmodel[i].Bezeichner);
                }
            }
            if (listBox_Pufferspeicher.Items.Count > 0) listBox_Pufferspeicher.SelectedIndex = 0;
        }

        private void Form_PufferSp_Load(object sender, EventArgs e)
        {
            pufferspctrl.ReadAll();
            for (int i = 0; i < pufferspctrl.rows; i++)
            {
                listBox_Pufferspeicher_DB.Items.Add(pufferspctrl.items[i].Name);
            }

            pufferspctrl.ReadAll();
            for (int i = 0; i < pufferspctrl.rows; i++)
            {
                if (comboBox_Hersteller.FindStringExact(pufferspctrl.items[i].Firma) == -1) comboBox_Hersteller.Items.Add(pufferspctrl.items[i].Firma);
            }

            comboBox_Volumen.Items.Add("Alle");
            comboBox_Volumen.Items.Add("bis 100 l");
            comboBox_Volumen.Items.Add(">100 bis 200 l");
            comboBox_Volumen.Items.Add(">200 bis 500 l");
            comboBox_Volumen.Items.Add(">500 bis 1.000 l");
            comboBox_Volumen.Items.Add("über 1.000 l");
            comboBox_Volumen.Text = "Alle";
            comboBox_Hersteller.Text = "Alle";
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

        private void btn_PufferSp_Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            WizardParent wizardparent = (WizardParent)getWizardPage();
           
            if (listBox_Pufferspeicher_DB.Text == "") return;

            rs.Open("select * from Tab_Pufferspeicher_STAMM where Bezeichner='" + listBox_Pufferspeicher_DB.Text + "'");
            if (rs.Next())
            {
                WErzeugerModel model = new WErzeugerModel();
                model.ID = startindex++;
                model.ID_Projekt = m_ID_Projekt;
                model.ID_PUFFER = (int)rs.Read("ID");
                model.ID_Type = m_nType;
                model.Bezeichner = listBox_Pufferspeicher_DB.Text;

                list_pufferspmodel.Add(model);
                listBox_Pufferspeicher.Items.Add(listBox_Pufferspeicher_DB.Text);
                if (m_bWizard) wizardparent.list_werzmodel = list_pufferspmodel;
            }
            rs.Close();
        }

        private void btn_PufferSp_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Pufferspeicher.SelectedIndex == -1) return;
            list_pufferspmodel.RemoveAt(listBox_Pufferspeicher.SelectedIndex);
            listBox_Pufferspeicher.Items.Remove(listBox_Pufferspeicher.Text);
            if (m_bWizard) wizardparent.list_werzmodel = list_pufferspmodel;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void listBox_PufferSp_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Pufferspeicher_STAMM where Bezeichner='" + listBox_Pufferspeicher.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Hersteller.Text = rs.GetString("Hersteller");
                textBox_Typ.Text = (string)rs.Read("Speichertyp");
                textBox_Versluste.Text = rs.Read("Bereitschaftsverluste").ToString();
                textBox_Volumen.Text = rs.Read("Gesamtvolumen").ToString();
                textBox_Investitionskosten.Text = rs.Read("Investitionskosten").ToString();
            }
            rs.Close();
        }

        private void listBox_PufferSp_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Pufferspeicher_STAMM where Bezeichner='" + listBox_Pufferspeicher_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Hersteller.Text = rs.GetString("Hersteller");
                textBox_Typ.Text = (string)rs.Read("Speichertyp");
                textBox_Versluste.Text = rs.Read("Bereitschaftsverluste").ToString();
                textBox_Volumen.Text = rs.Read("Gesamtvolumen").ToString();
                textBox_Investitionskosten.Text = rs.Read("Investitionskosten").ToString();
            }
            rs.Close();
        }

        private void comboBox_Hersteller_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Volumen_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string szFilter = "";
            string szFilterVolumen = "";
            string sql = "";

            // B0-10: Vorbelegung "alle Volumina" — ohne Treffer in der Literalkette blieb
            // der Volumenteil sonst leer und das SQL endete in "... and  order by ...".
            // Auslöser ist Freitext in der editierbaren ComboBox; das Symptom war eine
            // stumme Leerliste (RecordSet fängt den Syntaxfehler ab).
            szFilterVolumen = "Gesamtvolumen Like '%'";
            if (comboBox_Volumen.Text == "Alle" || comboBox_Volumen.Text == "") szFilterVolumen = "Gesamtvolumen Like '%'";
            else if (comboBox_Volumen.Text == "bis 100 l") szFilterVolumen = "Gesamtvolumen <100";
            else if (comboBox_Volumen.Text == ">100 bis 200 l") szFilterVolumen = "Gesamtvolumen >=100 and Gesamtvolumen <200";
            else if (comboBox_Volumen.Text == ">200 bis 500 l") szFilterVolumen = "Gesamtvolumen >=200 and Gesamtvolumen <500";
            else if (comboBox_Volumen.Text == ">500 bis 1.000 l") szFilterVolumen = "Gesamtvolumen >=500 and Gesamtvolumen <1000";
            else if (comboBox_Volumen.Text == "über 1.000 l") szFilterVolumen = "Gesamtvolumen >=1000";

            if (comboBox_Hersteller.Text == "Alle" || comboBox_Hersteller.Text == "") szFilter = "Hersteller Like '%'";
            else szFilter = "Hersteller='" + comboBox_Hersteller.Text + "'";

            listBox_Pufferspeicher_DB.Items.Clear();
            if (szFilter == "")
                sql = "select * from Tab_Pufferspeicher_STAMM where " + szFilterVolumen + " order by Bezeichner";
            else
                sql = "select * from Tab_Pufferspeicher_STAMM where " + szFilter + " and " + szFilterVolumen + " order by Bezeichner";

            rs.Open(sql);

            while (rs.Next())
            {
                listBox_Pufferspeicher_DB.Items.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();

            int index = listBox_Pufferspeicher_DB.SelectedIndex;
            listBox_Pufferspeicher.SelectedItems.Clear();
            listBox_Pufferspeicher_DB.SelectedItems.Clear();
            ctrl.PufferSp();
            listBox_Pufferspeicher_DB.Items.Clear();
            pufferspctrl.ReadAll();
            for (int i = 0; i < pufferspctrl.rows; i++)
            {
                listBox_Pufferspeicher_DB.Items.Add(pufferspctrl.items[i].Name);
            }
        }

        private void btn_Löschen_Click(object sender, EventArgs e)
        {
            if (listBox_Pufferspeicher_DB.SelectedIndex == -1) { MessageBox.Show("Bitte ein Modul auswählen!"); return; }

            // B0-8: Der Button löscht aus dem KATALOG (Tab_Pufferspeicher_STAMM), nicht
            // aus dem Projekt — bisher ohne Rückfrage und damit global wirksam.
            // Explizite Bestätigung, damit kein Katalogdatensatz versehentlich verschwindet.
            if (MessageBox.Show(
                    "Der Pufferspeicher '" + listBox_Pufferspeicher_DB.Text + "' wird aus dem Katalog\n" +
                    "(Stammdaten) gelöscht und steht danach in keinem Projekt mehr zur Auswahl.\n\n" +
                    "Wirklich aus den Stammdaten löschen?",
                    "Katalog-Löschung",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes) return;

            if (!pufferspctrl.Delete(listBox_Pufferspeicher_DB.Text)) return;

            listBox_Pufferspeicher_DB.Items.RemoveAt(listBox_Pufferspeicher_DB.SelectedIndex);
        }

 
    }
}
