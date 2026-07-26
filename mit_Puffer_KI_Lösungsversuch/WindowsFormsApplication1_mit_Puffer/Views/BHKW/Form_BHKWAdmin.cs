using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_BHKWAdmin : Form
    {
        // Admin-Dialog bearbeitet jetzt die Stammdaten-Tabelle Tab_BHKW_STAMM.
        BHKWStammCtrl ctrl = new BHKWStammCtrl();
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
            for (int i = 0; i < BHKWStammCtrl.LeistungText.Length; i++)
            {
                if (BHKWStammCtrl.LeistungText[i] != "")
                    comboBox_Leistung.Items.Add(BHKWStammCtrl.LeistungText[i]);
            }

            comboBox_Brennstoff.SelectedIndex = 0;
            comboBox_Leistung.SelectedIndex = 0;

            dataGridView1.Select();
            if (dataGridView1.Rows.Count > 0)
                dataGridView1.Rows[0].Cells[0].Selected = true;
        }

        // Liest die Detail-Felder für den ausgewählten STAMM-Datensatz.
        private void FillDetails(string szName)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE Bezeichner = ?",
                new OleDbParameter("@name", szName));

            if (dt == null || dt.Rows.Count == 0) return;
            DataRow r = dt.Rows[0];
            textBox_Name.Text = r["Bezeichner"].ToString();
            textBox_Firma.Text = r["Firma"] == DBNull.Value ? "" : r["Firma"].ToString();
            textBox_Beschreibung.Text = r["Beschreibung"] == DBNull.Value ? "" : r["Beschreibung"].ToString();
            textBox_Leistung_th.Text = r["Ptherm"].ToString();
            textBox_Leistung_el.Text = r["Pel"].ToString();
            textBox_M_GrenzL.Text = r["Grenzleistung"].ToString();
            textBox_Vorlauf.Text = r["Vorlauf"].ToString();
            textBox_Ruecklauf.Text = r["Ruecklauf"].ToString();
        }

        private void dataGridView1_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;
            FillDetails((string)dataGridView1.CurrentRow.Cells[0].Value);
        }

        // Filterliste basiert jetzt auf den STAMM-Daten (Tab_BHKW_STAMM), gelesen über DataRepository.
        private void SetFilter()
        {
            string szFilter = "";
            string szFilterLeistung = "";
            string sql = "";

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

            if (szFilterLeistung == "") szFilterLeistung = "Ptherm Like '%'";

            if (szFilter == "")
                sql = "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE " + szFilterLeistung + " ORDER BY Bezeichner";
            else
                sql = "SELECT * FROM " + BHKWStammCtrl.TABLE + " WHERE (" + szFilter + ") and " + szFilterLeistung + " ORDER BY Bezeichner";

            DataTable dt = DataRepository.GetDataTable(sql);

            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            int i = 0;
            foreach (DataRow row in dt.Rows)
            {
                int brennIdx = row["Brennstoff"] != DBNull.Value ? Convert.ToInt32(row["Brennstoff"]) : 0;
                string brennText = (brennIdx >= 1 && brennIdx <= ctrl.Brennstoffart.Count) ? ctrl.Brennstoffart[brennIdx - 1] : "";
                bool ro = row.Table.Columns.Contains("ReadOnly") && row["ReadOnly"] != DBNull.Value && Convert.ToBoolean(row["ReadOnly"]);

                dgv.Rows.Add(
                    row["Bezeichner"].ToString(),
                    row["Firma"].ToString() + "\nBrennstoff: " + brennText +
                    "\nPtherm: " + row["Ptherm"].ToString() + " kW" +
                    "\nPel: " + row["Pel"].ToString() + " kW");
                // Schreibgeschützte (ReadOnly) Datensätze optisch grau kennzeichnen
                if (ro)
                    dgv.Rows[i].DefaultCellStyle.ForeColor = Color.Gray;
                dgv.Rows[i++].DividerHeight = 5;
            }
        }

        private void comboBox_Brennstoff_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            SetFilter();
        }

        // Liefert den Bezeichner der aktuell gewählten Zeile.
        private string SelectedBezeichner()
        {
            if (dataGridView1.CurrentRow == null) return "";
            return (string)dataGridView1.CurrentRow.Cells[0].Value;
        }

        private void btn_DBBHKW_Edit_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection sr =  dataGridView1.SelectedRows;
            if(sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte ein BHKW auswählen!"); return; }

            string szName = SelectedBezeichner();

            // Editor ist auch für schreibgeschützte (ReadOnly) Datensätze aufrufbar;
            // dort ist lediglich der "Überschreiben"-Button gesperrt.
            Form_DBBHKW frm = new Form_DBBHKW();
            frm.m_mode = Form_DBBHKW.MODE_EDIT;
            frm.SetControls(szName);
            frm.ShowDialog();
            SetFilter();
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
                SetFilter();
            }
        }

        private void btn_DBBHKW_Löschen_Click(object sender, EventArgs e)
        {
            DataGridViewSelectedRowCollection sr = dataGridView1.SelectedRows;
            if (sr.Count == 0) { System.Windows.Forms.MessageBox.Show("Bitte ein BHKW auswählen!"); return; }

            string szName = SelectedBezeichner();

            // ReadOnly-Schutz: schreibgeschützte Datensätze nicht löschbar
            if (ctrl.IsReadOnly(szName))
            {
                MessageBox.Show("Dieser Stammdatensatz ist schreibgeschützt (ReadOnly) und kann nicht gelöscht werden.",
                    "Schreibgeschützt", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Soll " + szName + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            // Löschen über den Stamm-Controller (nutzt DataRepository, prüft ReadOnly erneut)
            if (ctrl.Delete(szName))
            {
                dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
            }
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null) return;
            FillDetails(SelectedBezeichner());
        }
    }

}
