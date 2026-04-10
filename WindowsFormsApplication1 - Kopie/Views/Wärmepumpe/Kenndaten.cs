using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Globalization;

namespace WindowsFormsApplication1
{
    public partial class Kenndaten : Form
    {
        private DataTable dt;
        private KenndatenModel model = new KenndatenModel();
        public int m_ID_WP = 0;

        public Kenndaten(ref DataSet ds)
        {
            InitializeComponent();
              
            dataGridView1.DataSource = ds.Tables[0];
            dt = ds.Tables[0];
            
            dataGridView1.RowHeadersVisible = true;
            dataGridView1.AllowUserToAddRows = false;
    
            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.ColumnHeader);// .AllCellsExceptHeader);
            dataGridView1.Columns["ID"].Visible = false;
            dataGridView1.Columns["ID_WP"].Visible = false;
            dataGridView1.Columns["Vorlauf"].Visible = false;

            if (dt.Rows.Count > 0)
            {
                FillVorlaufCombo();
                DataRow dr = dt.Rows[0];
                ds.Tables[0].DefaultView.RowFilter = string.Format("Convert([{0}], 'System.String') LIKE '%{1}%'", "Vorlauf", dr[2].ToString());
                listBox1.SelectedIndex = 0;
            }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            dt.DefaultView.RowFilter = string.Format("Convert([{0}], 'System.String') LIKE '{1}'", "Vorlauf", listBox1.Text);
            dataGridView1.Columns["ID"].Visible = false;
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            int index = e.RowIndex;
            //textBox2.Text = dataGridView1.Rows[index].Cells[3].Value.ToString();
            //textBox3.Text = dataGridView1.Rows[index].Cells[4].Value.ToString();
            //textBox4.Text = dataGridView1.Rows[index].Cells[5].Value.ToString();
        }

        private void btn_ItemNeu_Click(object sender, EventArgs e)
        {
            if (listBox1.Text == "") { MessageBox.Show("Vorlauftemperatur selektieren!"); return; }
            if (!Program.checkInt(textBox_Temperatur, textBox_Temperatur.Text)) return;
            if (!Program.checkDouble(textBox_COP, textBox_COP.Text)) return;
            if (!Program.checkDouble(textBox_Ptherm, textBox_Ptherm.Text)) return;
  
            DataRow newRow = dt.NewRow();
            newRow["ID_WP"] = m_ID_WP;
            newRow["Vorlauf"] = Int32.Parse(listBox1.Text);
            newRow["Temperatur"] = Int32.Parse(textBox_Temperatur.Text);
            newRow["COP"] = Double.Parse(textBox_COP.Text);
            newRow["Ptherm"] = Double.Parse(textBox_Ptherm.Text);
            dt.Rows.Add(newRow);
            textBox_Temperatur.Text = "";
            textBox_COP.Text = "";
            textBox_Ptherm.Text = "";
        }

        private void btn_NeuVorlauf_Click(object sender, EventArgs e)
        {
            if (!Program.checkInt(textBox_NeuVorlauf, textBox_NeuVorlauf.Text)) return;
   
            DataRow newRow = dt.NewRow();
            newRow["ID_WP"] = m_ID_WP;
            newRow["Vorlauf"] = Int32.Parse(textBox_NeuVorlauf.Text);
            newRow["Temperatur"] = 0;
            newRow["COP"] = 0;
            newRow["Ptherm"] = 0;
       //     dt.Rows.Add(newRow);
            if (listBox1.FindString(textBox_NeuVorlauf.Text) == ListBox.NoMatches) listBox1.Items.Add(textBox_NeuVorlauf.Text);
 
        }

        public void FillVorlaufCombo()
        {
            DataRow dr = dt.Rows[0];
            model.m_ID_WP = (int)dr[1];
            KenndatenCtrl ctrl = new KenndatenCtrl();
            ctrl.ReadVorlauf("SELECT Vorlauf, ID_WP FROM Tab_Kenndaten GROUP BY Vorlauf, ID_WP HAVING ID_WP=" + model.m_ID_WP);
            listBox1.Items.Clear();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox1.Items.Add(ctrl.items[i].m_nVorlauf);
            }
        }

        private void btn_Abbruch_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void checkValue(object sender, DataGridViewCellValidatingEventArgs e)
        {
            var dgv = (DataGridView)sender;
            int rowIdx = e.RowIndex;
            int colIdx = e.ColumnIndex;
            if (colIdx == 4 || colIdx == 5)
            {
                Control c = dgv.Controls[0];
                if (!Program.checkDouble(c, e.FormattedValue.ToString()))
                {
                    dataGridView1.CancelEdit();
                }
            }
            if (colIdx == 3)
            {
                Control c = dgv.Controls[0];
                if (!Program.checkInt(c, e.FormattedValue.ToString()))
                {
                    dataGridView1.CancelEdit();
                }
            }
        }
  
    }
}
