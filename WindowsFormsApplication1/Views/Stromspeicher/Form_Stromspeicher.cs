using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Sp : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private StromspeicherCtrl spctrl = new StromspeicherCtrl();
        public List<WErzeugerModel> list_spmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.SP_TYP;
        public int m_ID_Projekt = 0;
        private string m_szProjekt;
        int startindex = 100000;

        public Form_Sp()
        {
            InitializeComponent();
            listBox_SP_DB.Items.Clear();
            listBox_SP.Items.Clear();
        }

        public void SetControls(string szprojekt)
        {
            m_szProjekt = szprojekt;    
            listBox_SP.Items.Clear();
            for (int i = 0; i < list_spmodel.Count; i++)
            {
                listBox_SP.Items.Add(list_spmodel[i].Bezeichner);
            }
            if(listBox_SP.Items .Count > 0) listBox_SP.SelectedIndex = 0;
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

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_SP.SelectedIndex == -1) return;
            list_spmodel.RemoveAt(listBox_SP.SelectedIndex);
            listBox_SP.Items.RemoveAt(listBox_SP.SelectedIndex); 
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            WizardParent wizardparent = (WizardParent)getWizardPage();
            WErzeugerModel model = new WErzeugerModel();

            if (listBox_SP_DB.Text == "") return;

            rs.Open("select * from Tab_Stromspeicher where Bezeichner='" + listBox_SP_DB.Text + "'");
            if (!rs.EOF())
            {
                model.ID = startindex++;
                model.ID_Projekt = m_ID_Projekt; 
                model.ID_SP = (int)rs.Read("ID");
                model.ID_Type = m_nType;
                model.Bezeichner = listBox_SP_DB.Text;
            }
            rs.Close();

            list_spmodel.Add(model);

            listBox_SP.Items.Add(listBox_SP_DB.Text);
            if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = listBox_SP.Items.Count - 1;
        }

        private void Form_Sp_Load(object sender, EventArgs e)
        {
            spctrl.ReadAll();
            for (int i = 0; i < spctrl.rows; i++)
            {
                listBox_SP_DB.Items.Add(spctrl.items[i].m_szBezeichner);
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

        private void listBox_SP_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Stromspeicher where Bezeichner='" + listBox_SP_DB.Text + "'");
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

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_Stromverbraucher_Admin frm = new Form_Stromverbraucher_Admin();
            frm.ShowDialog();
            SetControls(m_szProjekt);
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            int b;
            int a;
            int d;
            int x;
        }
    }
}

 