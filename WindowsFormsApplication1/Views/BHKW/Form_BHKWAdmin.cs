using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using MessageBox = System.Windows.Forms.MessageBox;

namespace WindowsFormsApplication1
{
    public partial class Form_BHKWAdmin : Form
    {
        BHKWCtrl ctrl = new BHKWCtrl();
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.BHKW_TYP;
        public int m_ID_Projekt = 0;
        private WErzeugerModel model = new WErzeugerModel();
        private string m_szProjekt;

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
            dgv.Columns[1].DefaultCellStyle.BackColor = Color.GreenYellow;
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(255, 215, 159, 57);
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form_BHKWEing_Load(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();
            SetControls();
            SetDBList();
        }

        private void SetDBList(string szFilter="")
        {
            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            ctrl.ReadAll(szFilter);
            for (int i = 0; i < ctrl.rows; i++)
            {
                dgv.Rows.Add(ctrl.items[i].m_szBezeichner, ctrl.items[i].m_szFirma +  "\nBrennstoff: " + BHKWCtrl.BrennstoffartText[ctrl.items[i].m_Brennstoff] + "\nPtherm: " + ctrl.items[i].m_Ptherm + " kW" + "\nPel: " + ctrl.items[i].m_Pel + " kW");
                dgv.Rows[i].DividerHeight = 5;
            }
        }

        public void SetControls()
        {
            SetDBList();

            dataGridView1.Select();
            dataGridView1.ClearSelection();

            comboBox_Brennstoff.Items.Clear();
            comboBox_Leistung.Items.Clear();

            comboBox_Brennstoff.Items.Add("Alle");
            for (int i = 0; i < BHKWCtrl.BrennstoffartText.Length; i++)
            {
                if (BHKWCtrl.BrennstoffartText[i] != "")
                    comboBox_Brennstoff.Items.Add(BHKWCtrl.BrennstoffartText[i]);
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

        private void comboBox_Brennstoff_SelectedIndexChanged(object sender, EventArgs e)
        {
            string[] brennstofftext = new string[BHKWCtrl.BrennstoffartText.Length];
            brennstofftext = BHKWCtrl.BrennstoffartText;
            string[] leistungfiltertext = new string[BHKWCtrl.LeistungFilterText.Length];
            leistungfiltertext = BHKWCtrl.LeistungFilterText;

            if (comboBox_Brennstoff.Text == "Alle" && comboBox_Leistung.Text == "Alle")
            {
                SetDBList(""); 
            }
            else
            {
                if (comboBox_Leistung.Text == "Alle" || comboBox_Leistung.Text == "")
                {
                    SetDBList("Brennstoff=" + Array.FindIndex(brennstofftext, a => a.Equals(comboBox_Brennstoff.Text)));                }
                else
                {
                    if (comboBox_Brennstoff.Text == "Alle")
                    {
                        SetDBList("");
                    }
                    else
                    {
                        SetDBList("Brennstoff=" + Array.FindIndex(brennstofftext, a => a.Equals(comboBox_Brennstoff.Text)) +
                                  " and " + leistungfiltertext[comboBox_Leistung.SelectedIndex]);
                    }
                }
            }
        }

        private void comboBox_Leistung_SelectedIndexChanged(object sender, EventArgs e)
        {
            string[] brennstofftext = new string[BHKWCtrl.BrennstoffartText.Length];
            brennstofftext = BHKWCtrl.BrennstoffartText;
            string[] leistungfiltertext = new string[BHKWCtrl.LeistungFilterText.Length];
            leistungfiltertext = BHKWCtrl.LeistungFilterText;

            if (comboBox_Brennstoff.Text == "Alle" && comboBox_Leistung.Text == "Alle")
            {
                SetDBList("");
            }
            else
            {
                if (comboBox_Brennstoff.Text == "Alle" || comboBox_Brennstoff.Text == "")
                {
                    SetDBList(leistungfiltertext[comboBox_Leistung.SelectedIndex]);
                }
                else
                {
                    int brennstoffindex = Array.FindIndex(brennstofftext, a => a.Equals(comboBox_Brennstoff.Text));
                    int leistungsfilterindex = comboBox_Leistung.FindString(comboBox_Leistung.Text);

                    if (comboBox_Leistung.Text == "Alle")
                    {
                        SetDBList("Brennstoff=" + brennstoffindex + " and " + leistungfiltertext[leistungsfilterindex]);
                    }
                    else
                    {
                   
                        SetDBList("Brennstoff=" + brennstoffindex + " and " + leistungfiltertext[leistungsfilterindex]);
                    }
                }
            }
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
            frmLabel.ShowDialog();

            if (frmLabel.result == DialogResult.OK)
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