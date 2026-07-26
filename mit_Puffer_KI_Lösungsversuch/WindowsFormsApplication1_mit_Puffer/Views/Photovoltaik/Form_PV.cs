using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_PV : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private PhotovoltaikStammCtrl pvctrl = new PhotovoltaikStammCtrl();
        public List<WErzeugerModel> list_pvmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.PV_TYP;
        public int m_ID_Projekt = 0;
        private bool m_bWizard = false;
        private WizardParent wizardparent = null;
        int startindex = 100000;

        public Form_PV ()
        {
            InitializeComponent();
            listBox_DB.Items.Clear();
            listBox_Auswahl.Items.Clear();
        }

        public void SetControls(string projekt, bool bWizard = false)
        {
            if (bWizard)
            {
                btn_OK.Visible = false;
                btn_Abbrechen.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                wizardparent = (WizardParent)getWizardPage();
                list_pvmodel = wizardparent.list_werzmodel;
            }

            listBox_Auswahl.Items.Clear();
            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                if (list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                {
                    listBox_Auswahl.Items.Add(list_pvmodel[i].Bezeichner);
                }
            }
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;

            textBox_Gesamtleistung.Text = UpdateGesamtleistung().ToString();
        }

        private void Form_PV_Load(object sender, EventArgs e)
        {
            pvctrl.ReadAll();
            for (int i = 0; i < pvctrl.rows; i++)
            {
                listBox_DB.Items.Add(pvctrl.items[i].m_szName);
            }

            comboBox_Hersteller.Items.Add("Alle");
            
            RecordSet rs = new RecordSet(); 
            rs.Open("SELECT Firma FROM Tab_PV_STAMM GROUP BY Firma ORDER BY Firma");
            while (rs.Next())
            {
                comboBox_Hersteller.Items.Add((string)rs.Read("Firma"));
            }
            rs.Close();

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

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            WizardParent wizardparent = (WizardParent)getWizardPage();
           
            if (listBox_DB.Text == "") return;

            rs.Open("select * from Tab_PV_STAMM where Bezeichner='" + listBox_DB.Text + "'");
            if (rs.Next())
            {
                WErzeugerModel model = new WErzeugerModel();
                model.ID = startindex++;
                model.ID_Projekt = m_ID_Projekt;
                model.ID_PV = (int)rs.Read("ID");
                model.ID_Type = m_nType;
                model.Bezeichner = listBox_DB.Text;

                list_pvmodel.Add(model);
                if (m_bWizard) wizardparent.list_werzmodel = list_pvmodel;
            }
            rs.Close();

            listBox_Auswahl.Items.Add(listBox_DB.Text);
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = listBox_Auswahl.Items.Count - 1;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_Auswahl.SelectedIndex == -1) return;
            list_pvmodel.RemoveAt(listBox_Auswahl.SelectedIndex);
            listBox_Auswahl.Items.RemoveAt(listBox_Auswahl.SelectedIndex);
            if (m_bWizard) wizardparent.list_werzmodel = list_pvmodel;
        }

        private void UpdateProerties()
        {
            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                if (list_pvmodel[i].Bezeichner == listBox_Auswahl.Text && list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                {
                    list_pvmodel[i].m_Neigung = textBox_Neigung.Text == "" ? 0 : Int32.Parse(textBox_Neigung.Text);
                    list_pvmodel[i].m_Azimut = textBox_Azimut.Text == "" ? 0 : Int32.Parse(textBox_Azimut.Text);
                    list_pvmodel[i].PV_Leistung = textBox_AnlagenLeistung.Text == "" ? 0 : double.Parse(textBox_AnlagenLeistung.Text);
                    break;
                }
            }
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

        private void listBox_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_PV_STAMM where Bezeichner='" + listBox_Auswahl.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Hersteller.Text = (string)rs.Read("Firma");
                double kl = (double)rs.Read("Leistung");
                textBox_Leistung.Text = kl.ToString("F2");
            }
            rs.Close();

            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                if (list_pvmodel[i].Bezeichner == listBox_Auswahl.Text && list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                {
                    textBox_Neigung.Text = list_pvmodel[i].m_Neigung.ToString();
                    textBox_Azimut.Text = list_pvmodel[i].m_Azimut.ToString();
                    textBox_AnlagenLeistung.Text = list_pvmodel[i].PV_Leistung.ToString();
                    panel1.Visible = true;
                }
            }
            panel1.Visible = true;
        }

        private void listBox_DB_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_PV_STAMM where Bezeichner='" + listBox_DB.Text + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                textBox_Hersteller.Text = (string)rs.Read("Firma");
                double kl = (double)rs.Read("Leistung");
                textBox_Leistung.Text = kl.ToString("F2");
                panel1.Visible = false; 

            }
            rs.Close();
            panel1.Visible = false;
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string szFilter = "";
            string sql = "";

            if (comboBox_Hersteller.Text == "Alle") szFilter = "Bezeichner Like '%'";
            else szFilter = "Firma='" + comboBox_Hersteller.Text + "'"; 

            listBox_DB.Items.Clear();
            sql = "select * from Tab_PV_STAMM where " + szFilter;
            rs.Open(sql);

            while (rs.Next())
            {
                listBox_DB.Items.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl menuectrl = new MenueCtrl();

            int index = listBox_DB.SelectedIndex;
            listBox_Auswahl.SelectedItems.Clear();
            listBox_DB.SelectedItems.Clear();
            menuectrl.PV();
            listBox_DB.Items.Clear();
            pvctrl.ReadAll();
            
            for (int i = 0; i < pvctrl.rows; i++)
            {
                listBox_DB.Items.Add(pvctrl.items[i].m_szName);
            }
        }

        private void btn_Löschen_Click(object sender, EventArgs e)
        {
            if (listBox_DB.SelectedIndex == -1) { MessageBox.Show("Bitte ein Modul auswählen!"); return; }

            var result = MessageBox.Show("Wollen Sie wirklich das Modul löschen?", "Löschen", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                if (!pvctrl.Delete(listBox_DB.Text)) return;
                listBox_DB.Items.RemoveAt(listBox_DB.SelectedIndex);
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                if (list_pvmodel[i].Bezeichner == listBox_Auswahl.Text && list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                {
                    list_pvmodel[i].m_Neigung = textBox_Neigung.Text == "" ? 0 : Int32.Parse(textBox_Neigung.Text);
                    list_pvmodel[i].m_Azimut = textBox_Azimut.Text == "" ? 0 : Int32.Parse(textBox_Azimut.Text);
                    list_pvmodel[i].PV_Leistung = textBox_AnlagenLeistung.Text == "" ? 0 : double.Parse(textBox_AnlagenLeistung.Text);
                    break;
                }
            }
        }

        private void textBox_Neigung_TextChanged(object sender, EventArgs e)
        {
            if (textBox_Neigung.Text == "") { textBox_Neigung.Text = "0"; return; }
            if (!Program.checkInt(textBox_Neigung, textBox_Neigung.Text))
            {
                textBox_Neigung.Undo();
                textBox_Neigung.ClearUndo();
                return;
            }
        }

        private void textBox_Azimut_TextChanged(object sender, EventArgs e)
        {
            if (textBox_Azimut.Text == "") { textBox_Azimut.Text = "0"; return; }
            if (!Program.checkInt(textBox_Azimut, textBox_Azimut.Text))
            {
                textBox_Azimut.Undo();
                textBox_Azimut.ClearUndo();
                return;
            }
        }

        private void textBox_AnlagenLeistung_TextChanged(object sender, EventArgs e)
        {
            if (textBox_AnlagenLeistung.Text == "") { textBox_AnlagenLeistung.Text = "0"; return; }
            if (!Program.checkInt(textBox_AnlagenLeistung, textBox_AnlagenLeistung.Text))
            {
                textBox_AnlagenLeistung.Undo();
                textBox_AnlagenLeistung.ClearUndo();
                return;
            }
            textBox_Gesamtleistung.Text = UpdateGesamtleistung().ToString();
        }

        private void Form_PV_Paint(object sender, PaintEventArgs e)
        {
            float[] dashValues = { 5, 2 };
            Pen blackPen = new Pen(Color.Gray, 1);
            blackPen.DashPattern = dashValues;

            int a, b, c, d;
            a = panel1.Left;
            b = panel1.Top;
            c = panel1.Width;
            d = panel1.Height;

            e.Graphics.DrawLine(blackPen, new Point(a + 10, b + 10), new Point(a + c - 10, b + 10));
            e.Graphics.DrawLine(blackPen, new Point(a + 10, b + d - 10), new Point(a + c - 10, b + d - 10));
            e.Graphics.DrawLine(blackPen, new Point(a + 10, b + 10), new Point(a + 10, b + d - 10));
            e.Graphics.DrawLine(blackPen, new Point(a + c - 10, b + 10), new Point(a + c - 10, b + d - 10));
        }

        private void panel1_Leave(object sender, EventArgs e)
        {
            UpdateProerties();
            textBox_Gesamtleistung.Text = UpdateGesamtleistung().ToString(); 
        }

        private double UpdateGesamtleistung()
        {
            RecordSet rs = new RecordSet();
            double gesamtleistung = 0;
            double modulleistung = 0;

            for (int i = 0; i < list_pvmodel.Count; i++)
            {
                if (list_pvmodel[i].ID_Type == WizardItemClass.PV_TYP)
                {
                    rs.Open("select * from Tab_PV_STAMM where Bezeichner='" + list_pvmodel[i].Bezeichner + "'");
                    if (!rs.EOF())
                    {
                        modulleistung = (double)rs.Read("Leistung");
                    }
                    rs.Close();

                    gesamtleistung += (double)(list_pvmodel[i].PV_Leistung * modulleistung);
                }
            }
            return gesamtleistung;
        }
    }

}
