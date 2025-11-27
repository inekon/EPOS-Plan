using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Stromspeicher : Form
    {
        private WErzeugerModel model = new WErzeugerModel();
        private WErzeugerCtrl ctrl = new WErzeugerCtrl();
        private StromspeicherCtrl spctrl = new StromspeicherCtrl();
        public List<WErzeugerModel> list_werzmodel = new List<WErzeugerModel>();
        public int m_nType = WizardItemClass.SP_TYP;
        public int m_ID_Projekt = 0;
        private string m_szProjekt;
        private bool m_bWizard = false;
        WizardParent wizardparent = null;

        public Form_Stromspeicher ()
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
                FillWeight = 25
            });
            dgv.Columns.Add(new DataGridViewTextBoxColumn()
            {
                HeaderText = "Eigenschaften",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                FillWeight = 75
            });

            dgv.BackgroundColor = Color.White;
            dgv.GridColor = Color.White;
            dgv.Columns[1].DefaultCellStyle.BackColor = Color.GreenYellow;
            dgv.DefaultCellStyle.BackColor = Color.FromArgb(255, 215, 159, 57);
            SetDBList();
        }

        private void Form_Stromspeicher_Load(object sender, EventArgs e)
        {
            dataGridView1.ClearSelection();

            if (listBox_SP.Items.Count > 0)
            {
                listBox_SP.Select();
                listBox_SP.SelectedItems.Clear();
                listBox_SP.SelectedIndex = 0;
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

        private void SetDBList()
        {
            DataGridView dgv = dataGridView1;
            dgv.Rows.Clear();
            spctrl.ReadAll();
            for (int i = 0; i < spctrl.rows; i++)
            {
                dgv.Rows.Add(spctrl.items[i].m_szBezeichner, spctrl.items[i].m_Leistung + " kW\n" + spctrl.items[i].m_szTyp);
                dgv.Rows[i].DividerHeight = 1;
            }
            //if (dgv.Rows.Count > 0) dgv.Rows[0].Selected = true;
            dgv.ClearSelection();
        }

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentCell.RowIndex == -1) return;
            model.Bezeichner = (string)dataGridView1.CurrentRow.Cells[0].Value;
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Stromspeicher where Bezeichner='" + model.Bezeichner + "'");
            if (!rs.EOF())
            {
                model.ID_SP = (int)rs.Read("ID");
                model.ID_Type = WizardItemClass.SP_TYP; 
            }
            rs.Close();
 
            list_werzmodel.Add(model);
            if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;

            listBox_SP.Items.Add(model.Bezeichner);
            if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = listBox_SP.Items.Count-1;
        }

        private void btn_Entfernen_Click(object sender, EventArgs e)
        {
            if (listBox_SP.Text == "") return;
            model.Bezeichner = listBox_SP.Text;
    
            for (int i = 0; i < list_werzmodel.Count; i++)
            {
                if (list_werzmodel[i].Bezeichner == listBox_SP.Text && list_werzmodel[i].ID_Type == WizardItemClass.SP_TYP)
                {
                    list_werzmodel.RemoveAt(i);
                    listBox_SP.Items.Remove(listBox_SP.Text);
                    break;
                }
            }
            if (m_bWizard) wizardparent.list_werzmodel = list_werzmodel;

            if (listBox_SP.Items.Count > 0)
            {
                listBox_SP.SelectedIndex = 0;
            }
            else
            {
                if (dataGridView1.Rows.Count > 0)
                {
                    dataGridView1.Rows[0].Selected = true;
                    dataGridView1.CurrentCell = dataGridView1.Rows[0].Cells[0];
                }
            }

            if (listBox_SP.Items.Count  == 0)
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
                listBox_SP.Select();
                listBox_SP.SelectedIndex = 0;
            }

        }

        private void listBox_SP_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            textBox_Name.Text = "";
            textBox_Typ.Text = "";
            textBox_Leistung.Text = "";
            textBox_Energie.Text = "";
            textBox_Degradation.Text = "";
            textBox_Ladezustand.Text = "";

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

            dataGridView1.ClearSelection();
        }

        public void SetControls(string szProjekt, bool bWizard=false)
        {
            if (bWizard)
            {
                btn_Abbrechen.Visible = false;
                btn_OK.Visible = false;
                this.FormBorderStyle = FormBorderStyle.None;
                this.BackColor = Color.White;
                m_bWizard = true;
                wizardparent = (WizardParent)getWizardPage();
                list_werzmodel = wizardparent.list_werzmodel;
            }

            m_szProjekt = szProjekt;


            listBox_SP.Items.Clear();

            for (int n = 0; n < list_werzmodel.Count; n++)
            {
                WErzeugerModel item = new WErzeugerModel();

                if (list_werzmodel[n].ID_Type == WizardItemClass.SP_TYP)
                {
                    item.Bezeichner = list_werzmodel[n].Bezeichner;
                    listBox_SP.Items.Add(item.Bezeichner);
                }
            }

            dataGridView1.Select();
            dataGridView1.ClearSelection();

            if (listBox_SP.Items.Count > 0) listBox_SP.SelectedIndex = 0;

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
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btn_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_AdminStromspeicher frm = new Form_AdminStromspeicher();
            frm.ShowDialog();
            if (frm.DialogResult == DialogResult.OK)
            {
                SetDBList();
            }
        }

        private void dataGridView1_Click(object sender, EventArgs e)
        {
            string szName = (string)dataGridView1.CurrentRow.Cells[0].Value;
            RecordSet rs = new RecordSet();

            rs.Open("select * from Tab_Stromspeicher where Bezeichner='" + szName + "'");
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
    }
}
