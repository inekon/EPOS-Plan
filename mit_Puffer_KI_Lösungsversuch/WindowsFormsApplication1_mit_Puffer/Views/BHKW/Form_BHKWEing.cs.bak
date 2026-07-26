using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;


namespace WindowsFormsApplication1
{
    public partial class Form_BHKWEing : Form
    {
        BHKWCtrl ctrl = new BHKWCtrl();
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.BHKW_TYP;
        public int m_ID_Projekt = 0;
        private WErzeugerModel model = new WErzeugerModel();
        private string m_szProjekt;
        private bool m_bWizard = false;
        private WizardParent wizardparent = null;

        public Form_BHKWEing()
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
                //FillWeight = 50
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Eigenschaften",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                //FillWeight = 50
            });

            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Color.White;
 			
			// Grundfarbe für alle Zeilen
            dgv.RowsDefaultCellStyle.BackColor = Color.White;
            // Farbe für jede zweite Zeile (Zebra)
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(215, 230, 245);

            dgv.Columns[1].HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
            // Erlaubt eigene Farben für den Header (sonst bleibt er Windows-Grau)
            dgv.EnableHeadersVisualStyles = false;

            // Hintergrundfarbe festlegen (ein kräftiges "BHKW-Blau")
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(51, 102, 153);

            // Schriftfarbe auf Weiß setzen (für den Kontrast zum dunklen Blau)
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;

            // Der Text bestimmt die Breite (sehr genau, kann aber bei viel Text flackern)
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

            // ODER: Die Spalten teilen sich den verfügbaren Platz gleichmäßig auf (füllt das ganze Grid aus)
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
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
                dgv.Rows.Add((string)rs.Read("Bezeichner"), (string)rs.Read("Firma") + "\nBrennstoff: " + ctrl.Brennstoffart[(int)rs.Read("Brennstoff") - 1] + "\nPtherm: " + rs.Read("Ptherm").ToString() + " kW" + "\nPel: " + rs.Read("Pel").ToString() + " kW");
                dgv.Rows[i++].DividerHeight = 5;
            }
            rs.Close();
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

        private void btn_OK_Click(object sender, EventArgs e)
        {
            WErzeugerModel item;

            for (int n = 0; n < list_werzmodel.Count; n++)
            {
                if (list_werzmodel[n].ID_Type == WizardItemClass.BHKW_TYP)
                {
                    item = list_werzmodel[n];
                    if (textBox_Volumen_Pendelsp.Text == "") textBox_Volumen_Pendelsp.Text = "0";
                    item.Volumen = double.Parse(textBox_Volumen_Pendelsp.Text);
                    item.rendeMix = checkBox_Rendemix.Checked;
                }
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void Form_BHKWEing_Load(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();

            if (listBox_Auswahl.Items.Count > 0)
            {
                listBox_Auswahl.Select();
                listBox_Auswahl.SelectedItems.Clear();
                listBox_Auswahl.SelectedIndex = 0;
            }
            SetFilter();
        }

        public void SetControls(string szProjekt, bool bWizard = false)
        {
            if (bWizard)
            {
                btn_Abbrechen.Visible = false;
                btn_OK.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                wizardparent = (WizardParent)getWizardPage();
                list_werzmodel = wizardparent.list_werzmodel;   
            }

            m_szProjekt = szProjekt;
            
            SetFilter();

            dataGridView1.Select();
            dataGridView1.ClearSelection();

            listBox_Auswahl.Items.Clear();
            for (int n = 0; n < list_werzmodel.Count; n++)
            {
                WErzeugerModel item = new WErzeugerModel();

                if (list_werzmodel[n].ID_Type == WizardItemClass.BHKW_TYP)
                {
                    item.Bezeichner = list_werzmodel[n].Bezeichner;
                    listBox_Auswahl.Items.Add(item.Bezeichner);
                    textBox_Volumen_Pendelsp.Text = list_werzmodel[n].Volumen.ToString();
                    checkBox_Rendemix.Checked = list_werzmodel[n].rendeMix;
                }
            }
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = 0;

            textBox__Summe_Leistung.Text = SummeLeistung().ToString();

            comboBox_Brennstoff.Items.Clear();
            comboBox_Leistung.Items.Clear();

            comboBox_Brennstoff.Items.Add("Alle");
            for (int i = 0; i < ctrl.Brennstoffart_Gruppe.Count; i++)
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
        }

        private void listBox_Auswahl_SelectedIndexChanged(object sender, EventArgs e)
        {
            string szName = listBox_Auswahl.Text;
            RecordSet rs = new RecordSet();

            if (listBox_Auswahl.SelectedIndex == -1) return;

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
            }
            rs.Close();
            textBox__M_GrenzL.Text = list_werzmodel[listBox_Auswahl.SelectedIndex].Grenzleistung.ToString();

            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                if (list_werzmodel[i].Bezeichner == listBox_Auswahl.Text)
                {
                    textBox__M_GrenzL.Text = list_werzmodel[i].Grenzleistung.ToString();
                    break;
                }
            }

            dataGridView1.ClearSelection();
        }

        private void btn_Hinzzu_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell.RowIndex == -1) return;
            model.Bezeichner = (string)dataGridView1.CurrentRow.Cells[0].Value;
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_BHKW where Bezeichner='" + model.Bezeichner + "'");
            if (!rs.EOF())
            {
                model.ID_BHKW = (int)rs.Read("ID");
                model.ID_Type = WizardItemClass. BHKW_TYP;
            }
            rs.Close();

            list_werzmodel.Add(model);
            if(m_bWizard) wizardparent.list_werzmodel = list_werzmodel;   

            listBox_Auswahl.Items.Add(model.Bezeichner);
            if (listBox_Auswahl.Items.Count > 0) listBox_Auswahl.SelectedIndex = listBox_Auswahl.Items.Count - 1;

            textBox__Summe_Leistung.Text = SummeLeistung().ToString();
        }

        private void btn_BHKW_Löschen_Click(object sender, EventArgs e)
        {
            if (listBox_Auswahl.Text == "") return;
            model.Bezeichner = listBox_Auswahl.Text;

            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                if (list_werzmodel[i].Bezeichner == listBox_Auswahl.Text && list_werzmodel[i].ID_Type == WizardItemClass.BHKW_TYP)
                {
                    listBox_Auswahl.Items.Remove(listBox_Auswahl.Text);
                    list_werzmodel.RemoveAt(i);
                    break;
                }
            }
            if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;

            if (listBox_Auswahl.Items.Count > 0)
            {
                textBox__Summe_Leistung.Text = SummeLeistung().ToString();
                listBox_Auswahl.SelectedIndex = 0;
            }
            else
            {
                textBox__Summe_Leistung.Text = "0"; ;
                textBox__M_GrenzL.Text = "0";
                if (dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.Rows[0].Selected = true;
                    dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
                }
            }

            if (listBox_Auswahl.Items.Count == 0)
            {
                dataGridView1.Rows[0].Selected = true;
                dataGridView1.Rows[0].Cells[0].Selected = true;

                // Definiere die Zeile und Spalte des zu klickenden Cells
                int rowIndex = 0;
                int columnIndex = 0;

                // Erstelle ein MouseEventArgs Objekt
                MouseEventArgs me = new MouseEventArgs(System.Windows.Forms.MouseButtons.Left, 1, 100, 100, 0);

                // Erstelle ein DataGridViewCellMouseEventArgs Objekt
                DataGridViewCellMouseEventArgs dgvme = new DataGridViewCellMouseEventArgs(columnIndex, rowIndex, 100, 100, me);

                // Rufe den CellMouseClick-Ereignis-Handler auf
                // Ersetzen Sie dataGridView1_CellMouseClick durch den Namen Ihres tatsächlichen Event-Handlers
                dataGridView1_Click(this.dataGridView1, dgvme);

            }
            else
            {
                listBox_Auswahl.Select();
                listBox_Auswahl.SelectedIndex = 0;
            }
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private double SummeLeistung()
        {
            double summe = 0;

            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                ctrl.ReadSingle(list_werzmodel[i].ID_BHKW);
                summe += ctrl.m_Ptherm; 
            }
            return summe;
        }

        private void textBox__M_GrenzL_Validating(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!Program.checkDouble(textBox__M_GrenzL, textBox__M_GrenzL.Text))
            {
                textBox__M_GrenzL.Undo();
                textBox__M_GrenzL.ClearUndo();
                return;
            }
            
            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                if (list_werzmodel[i].Bezeichner == listBox_Auswahl.Text)
                {
                    list_werzmodel[i].Grenzleistung = double.Parse(textBox__M_GrenzL.Text);
                    break;
                }
            }
        }

        private void textBox_Volumen_Pendelsp_TextChanged(object sender, EventArgs e)
        {
            if (!Program.checkDouble(textBox_Volumen_Pendelsp, textBox_Volumen_Pendelsp.Text))
            {
                textBox_Volumen_Pendelsp.Undo();
                textBox_Volumen_Pendelsp.ClearUndo();
                return;
            }

            SetKapPendelspeicher();
        }

        private void checkBox_Rendemix_CheckedChanged(object sender, EventArgs e)
        {
            SetKapPendelspeicher();
        }

        private void SetKapPendelspeicher()
        {
            double InhaltPendelspeicher = double.Parse(textBox_Volumen_Pendelsp.Text);
            double KapazitaetPendelspeicher = 0;

            if (checkBox_Rendemix.Checked)
                KapazitaetPendelspeicher = InhaltPendelspeicher * 35 * 1.163;
            else
                KapazitaetPendelspeicher = InhaltPendelspeicher * 20 * 1.163;

            textBox_Größe_Pendelsp.Text = KapazitaetPendelspeicher.ToString();
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
            if(sr.Count == 0) { MessageBox.Show("Bitte ein BHKW auswählen!"); return; }   

            frm.SetControls((string)dataGridView1.CurrentRow.Cells[0].Value); 
            DialogResult result = frm.ShowDialog();
            if(result == DialogResult.OK) SetFilter();
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

            var result = MessageBox.Show("Wollen Sie wirklich das BHKW löschen?", "Löschen", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                RecordSet rs = new RecordSet();
                rs.Open("Delete * from Tab_BHKW where Bezeichner='" + (string)dataGridView1.SelectedRows[0].Cells[0].Value + "'");
                rs.Close();

                dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
            }
        }
   
    }
}