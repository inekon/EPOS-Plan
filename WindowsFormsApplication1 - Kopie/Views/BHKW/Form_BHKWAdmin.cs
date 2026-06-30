using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_BHKWAdmin : Form
    {
        BHKWCtrl ctrl = new BHKWCtrl();
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.BHKW_TYP;
        public int m_ID_Projekt = 0;

        public Form_BHKWAdmin()
        {
            InitializeComponent();

            DataGridView dgv = dataGridView1;
            dgv.AutoGenerateColumns = false;
            dgv.RowHeadersVisible = false;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCells;
            dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10);

            dgv.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Name",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 50
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Eigenschaften",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 50
            });

            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Color.White;
 			
			// Grundfarbe für alle Zeilen
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            // Farbe für jede zweite Zeile (Zebra)
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(215, 230, 245);
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form_BHKWEing_Load(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();
            SetControls();
            SetFilter();
        }

        public void SetControls()
        {
            SetFilter();

            dataGridView1.Select();
            dataGridView1.ClearSelection();

            comboBox_Brennstoff.Items.Clear();
            comboBox_Leistung.Items.Clear();

            comboBox_Brennstoff.Items.Add("Alle");
            for (int i = 0; i <  ctrl.Brennstoffart_Gruppe.Count; i++)
            {
                comboBox_Brennstoff.Items.Add(ctrl.Brennstoffart_Gruppe[i]);
            }

            comboBox_Leistung.Items.Add("Alle");
            for (int i = 0; i < BHKWCtrl.LeistungText.Length; i++)
            {
                if (BHKWCtrl.LeistungText[i] != "")
                    comboBox_Leistung.Items.Add(BHKWCtrl.LeistungText[i]);
            }

            comboBox_Brennstoff.SelectedIndex = 0;
            comboBox_Leistung.SelectedIndex = 0;

            dataGridView1.Select(); 
            dataGridView1.Rows[0].Cells[0].Selected = true;
        }
        
        private void dataGridView1_Click(object sender, EventArgs e)
        {
            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_BHKW where Bezeichner='" + szName + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                object firma = rs.Read("Firma");
                textBox_Firma.Text = (firma == DBNull.Value) ? "" : (string)firma;
                object beschreibungValue = rs.Read("Beschreibung");
                textBox_Beschreibung.Text = (beschreibungValue == DBNull.Value) ? "" : (string)beschreibungValue;
                textBox_Leistung_th.Text = rs.Read("Ptherm").ToString();
                textBox_Leistung_el.Text = rs.Read("Pel").ToString();
                textBox__M_GrenzL.Text = rs.Read("Grenzleistung").ToString();
            }
            rs.Close();
        }

        private void SetFilter()
        {
            RecordSet rs = new RecordSet();
            string szFilter = "";
            string szFilterLeistung = "";
            string sql = "";

            szFilterLeistung = "";
            if (comboBox_Leistung.Text == "Alle") szFilterLeistung = "Ptherm Like '%'";
            else if (comboBox_Leistung.Text == "kleiner 20 kW") szFilterLeistung = "Ptherm <20";
            else if (comboBox_Leistung.Text == "20 bis 40 kW") szFilterLeistung = "Ptherm >=20 and Ptherm <40";
            else if (comboBox_Leistung.Text == "40 bis 80 kW") szFilterLeistung = "Ptherm >=40 and Ptherm <80";
            else if (comboBox_Leistung.Text == "80 bis 200 kW") szFilterLeistung = "Ptherm >=80 and Ptherm <200";
            else if (comboBox_Leistung.Text == "200 bis 500 kW") szFilterLeistung = "Ptherm >=200 and Ptherm <500";
            else if (comboBox_Leistung.Text == "500 bis 800 kW") szFilterLeistung = "Ptherm >=500 and Ptherm <800";
            else if (comboBox_Leistung.Text == "800 bis 1200 kW") szFilterLeistung = "Ptherm >=800 and Ptherm <1200";
            else if (comboBox_Leistung.Text == "über 1.200 kW") szFilterLeistung = "Ptherm >=1200";

            if (comboBox_Brennstoff.Text == "Gas") szFilter = "(Brennstoff >=1 and Brennstoff <=5) or Brennstoff=14";
            else if (comboBox_Brennstoff.Text == "Öl") szFilter = "(Brennstoff >=6 and Brennstoff <=9) or (Brennstoff >=18 and Brennstoff <=22)";
            else if (comboBox_Brennstoff.Text == "Koks") szFilter = "Brennstoff=10";
            else if (comboBox_Brennstoff.Text == "Kohle") szFilter = "Brennstoff=11";
            else if (comboBox_Brennstoff.Text == "Holz") szFilter = "Brennstoff=12";
            else if (comboBox_Brennstoff.Text == "Tierische Fette") szFilter = "Brennstoff=17";
            else if (comboBox_Brennstoff.Text == "Strom") szFilter = "Brennstoff=13";
            else if (comboBox_Brennstoff.Text == "Pellets") szFilter = "Brennstoff=15";
            else if (comboBox_Brennstoff.Text == "Rapsöl") szFilter = "Brennstoff=16";
            else if (comboBox_Brennstoff.Text == "Sonstige") szFilter = "Brennstoff=23";
            else if (comboBox_Brennstoff.Text == "Alle") szFilter = "Brennstoff Like '%'";

            if (szFilter == "")
                sql = "select * from [Tab_BHKW] where " + szFilterLeistung + " order by Bewzeichner";
            else
                sql = "select * from [Tab_BHKW] where (" + szFilter + ") and " + szFilterLeistung + " order by Bezeichner";

            rs.Open(sql);

            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            int i = 0;
            while (rs.Next())
            {
                dgv.Rows.Add((string)rs.Read("Bezeichner"), (string)rs.Read("Firma") + "\nBrennstoff: " + ctrl.Brennstoffart[(int)rs.Read("Brennstoff")-1] + "\nPtherm: " + rs.Read("Ptherm").ToString()  + " kW" + "\nPel: " + rs.Read("Pel").ToString()  + " kW");
                dgv.Rows[i++].DividerHeight = 5;
            }
            rs.Close();
        }

        private void comboBox_Brennstoff_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void btn_DBBHKW_Edit_Click(object sender, EventArgs e)
        {
            Form_DBBHKW frm = new Form_DBBHKW();
            frm.m_mode = Form_DBBHKW.MODE_EDIT;
            DataGridViewSelectedRowCollection sr =  dataGridView1.SelectedRows;
            if(sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte ein BHKW auswählen!"); return; }   

            frm.SetControls((string)dataGridView1.CurrentRow.Cells[0].Value); 
            frm.ShowDialog();
        }

        private void btn_DBBHKW_Neu_Click(object sender, EventArgs e)
        {
            Form_DBBHKW frm = new Form_DBBHKW();
            Form_Sp_ItemNeu frmLabel = new Form_Sp_ItemNeu();
    
            System.Drawing.Point p1 = btn_DBBHKW_Neu.Location;
            p1 = this.PointToScreen(p1);
            frmLabel.Location = p1;
            frmLabel.m_szName = "";
            frmLabel.SetControl();

            if (frmLabel.ShowDialog() == DialogResult.OK)
            {
                frm.m_mode = Form_DBBHKW.MODE_NEU;
                frm.SetControls(frmLabel.m_szName);
                frm.m_szName = frmLabel.m_szName;
                frm.ShowDialog();
            }
        }

        private void btn_DBBHKW_Löschen_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection sr = dataGridView1.SelectedRows;
            if (sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte ein BHKW auswählen!"); return; }

            DialogResult dialogResult = MessageBox.Show("Soll " + (string)dataGridView1.SelectedRows[0].Cells[0].Value + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;


            RecordSet rs = new RecordSet();
            rs.Open("Delete * from Tab_BHKW where Bezeichner='" + (string)dataGridView1.SelectedRows[0].Cells[0].Value + "'");
            rs.Close();

            dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_BHKW where Bezeichner='" + szName + "'");
            if (!rs.EOF())
            {
                textBox_Name.Text = (string)rs.Read("Bezeichner");
                object firma = rs.Read("Firma");
                textBox_Firma.Text = (firma == DBNull.Value) ? "" : (string)firma;
                object beschreibungValue = rs.Read("Beschreibung");
                textBox_Beschreibung.Text = (beschreibungValue == DBNull.Value) ? "" : (string)beschreibungValue;
                textBox_Leistung_th.Text = rs.Read("Ptherm").ToString();
                textBox_Leistung_el.Text = rs.Read("Pel").ToString();
                textBox__M_GrenzL.Text = rs.Read("Grenzleistung").ToString();
            }
            rs.Close();
        }
    }

}