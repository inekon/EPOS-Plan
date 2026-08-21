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
            dataGridView1.CellParsing += dataGridView1_CellParsing;
    
            dataGridView1.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.ColumnHeader);// .AllCellsExceptHeader);
            dataGridView1.Columns["ID"].Visible = false;
            dataGridView1.Columns["ID_WP"].Visible = false;
            dataGridView1.Columns["Vorlauf"].Visible = false;

            if (dt.Rows.Count > 0)
            {
                FillVorlaufCombo();
                DataRow dr = dt.Rows[0];
                ds.Tables[0].DefaultView.RowFilter = string.Format("Convert([{0}], 'System.String') LIKE '%{1}%'", "Vorlauf", dr[2].ToString());
                //listBox1.SelectedIndex = 0;
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
            // Folgepaket zu ab5bf32: geprueft wird am Knopf, die geprueften Werte
            // wandern direkt in die neue Zeile - kein zweites Parse auf denselben
            // Text. Typwahl nach dem Speichertyp in Tab_Kenndaten_STAMM: Vorlauf und
            // Temperatur sind Ganzzahl-, COP und Ptherm Double-Spalten. Leere Felder
            // meldeten schon bisher (checkInt/checkDouble), daher leerErlaubt:false.
            int nVorlauf;
            if (!Program.GanzzahlParsen(listBox1.Text, out nVorlauf)) { MessageBox.Show("Vorlauftemperatur selektieren!"); return; }

            int nTemperatur; double dCOP, dPtherm;
            if (!Program.GanzzahlPruefen(textBox_Temperatur, "Temperatur [°C]", out nTemperatur, leerErlaubt: false)) return;
            if (!Program.ZahlPruefen(textBox_COP, "COP", out dCOP, leerErlaubt: false)) return;
            if (!Program.ZahlPruefen(textBox_Ptherm, "Ptherm [kW]", out dPtherm, leerErlaubt: false)) return;

            DataRow newRow = dt.NewRow();
            newRow["ID_WP"] = m_ID_WP;
            newRow["Vorlauf"] = nVorlauf;
            newRow["Temperatur"] = nTemperatur;
            newRow["COP"] = dCOP;
            newRow["Ptherm"] = dPtherm;
            dt.Rows.Add(newRow);
            textBox_Temperatur.Text = "";
            textBox_COP.Text = "";
            textBox_Ptherm.Text = "";
        }

        private void btn_NeuVorlauf_Click(object sender, EventArgs e)
        {
            // Ganzzahl, weil Vorlauf als Integer gespeichert wird; leer meldete schon
            // bisher (checkInt) und meldet weiter.
            int nVorlauf;
            if (!Program.GanzzahlPruefen(textBox_NeuVorlauf, "Vorlauftemperatur [°C]", out nVorlauf, leerErlaubt: false)) return;

            DataRow newRow = dt.NewRow();
            newRow["ID_WP"] = m_ID_WP;
            newRow["Vorlauf"] = nVorlauf;
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
            ctrl.ReadVorlauf("SELECT Vorlauf, ID_WP FROM Tab_Kenndaten_STAMM GROUP BY Vorlauf, ID_WP HAVING ID_WP=" + model.m_ID_WP);
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

        /// <summary>
        /// Zellpruefung des Rasters: still absichern statt melden (Folgepaket zu
        /// ab5bf32). Bei unlesbarem Text wird die Bearbeitung wie bisher verworfen -
        /// die Zelle faellt sichtbar auf den alten Wert zurueck. Spalte 3 ist
        /// Temperatur (Ganzzahl), 4 und 5 sind COP und Ptherm (Double).
        /// </summary>
        private void checkValue(object sender, DataGridViewCellValidatingEventArgs e)
        {
            int colIdx = e.ColumnIndex;
            if (colIdx == 4 || colIdx == 5)
            {
                double dWert;
                if (!Program.ZahlParsen(e.FormattedValue.ToString(), out dWert))
                {
                    dataGridView1.CancelEdit();
                }
            }
            if (colIdx == 3)
            {
                int nWert;
                if (!Program.GanzzahlParsen(e.FormattedValue.ToString(), out nWert))
                {
                    dataGridView1.CancelEdit();
                }
            }
        }

        /// <summary>
        /// Übernimmt den Zelltext selbst in den Spaltentyp: Ohne diesen Schritt
        /// konvertiert der DataGridView mit CurrentCulture, und "3.5" wird unter
        /// de-DE still zu 35 (Punkt = Tausendertrennzeichen). Geparst wird invariant
        /// mit Komma ODER Punkt als Dezimalzeichen. Spalte 3 ist Temperatur
        /// (Ganzzahl), 4 und 5 sind COP und Ptherm (Double). Unlesbaren Text nicht
        /// anfassen - den verwirft schon checkValue (CellValidating läuft vorher).
        /// </summary>
        private void dataGridView1_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (e.Value == null) return;
            if (e.ColumnIndex == 4 || e.ColumnIndex == 5)
            {
                double dWert;
                if (Program.ZahlParsen(e.Value.ToString(), out dWert))
                {
                    e.Value = dWert;
                    e.ParsingApplied = true;
                }
            }
            if (e.ColumnIndex == 3)
            {
                int nWert;
                if (Program.GanzzahlParsen(e.Value.ToString(), out nWert))
                {
                    e.Value = nWert;
                    e.ParsingApplied = true;
                }
            }
        }

    }
}
