using MathNet.Numerics.LinearAlgebra.Factorization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;


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
            
            SetDBList();

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
       
            RecordSet rs = new RecordSet();
            rs.Open("Delete * from Tab_BHKW where Bezeichner='" + (string)dataGridView1.SelectedRows[0].Cells[0].Value + "'");
            rs.Close();

            dataGridView1.Rows.RemoveAt(dataGridView1.SelectedRows[0].Index);
        }
    }

}