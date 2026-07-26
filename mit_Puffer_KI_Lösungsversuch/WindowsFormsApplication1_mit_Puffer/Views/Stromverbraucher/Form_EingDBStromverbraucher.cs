using System;
using System.Data;
using System.Data.OleDb;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_EingDBStromverbraucher : Form
    {
        public string m_szStromname;
        public string m_szBeschreibung;
        public string m_szStromtyp;
        public string mode;

        public Form_EingDBStromverbraucher()
        {
            InitializeComponent();

            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Stromverbrauchertyp_STAMM order by Typname");

            while (rs.Next())
            {
                comboBox_Stromtyp.Items.Add(rs.Read("Typname"));
            }
            rs.Close();

        }

        public void SetControls()
        {
            RecordSet rs = new RecordSet();

            textBox_Stromname.Text = m_szStromname;
            textBox_Beschreibung.Text = m_szBeschreibung;
            comboBox_Stromtyp.Text = m_szStromtyp;
            rs.Open("select * from Tab_Stromverbraucher_STAMM where Bezeichner ='" + textBox_Stromname.Text + "'");

            if (rs.Next())
            {
                Wert1.Text = Convert.ToDouble(rs.Read("Monat_1")).ToString("F4");
                Wert2.Text = Convert.ToDouble(rs.Read("Monat_2")).ToString("F4");
                Wert3.Text = Convert.ToDouble(rs.Read("Monat_3")).ToString("F4");
                Wert4.Text = Convert.ToDouble(rs.Read("Monat_4")).ToString("F4");
                Wert5.Text = Convert.ToDouble(rs.Read("Monat_5")).ToString("F4");
                Wert6.Text = Convert.ToDouble(rs.Read("Monat_6")).ToString("F4");
                Wert7.Text = Convert.ToDouble(rs.Read("Monat_7")).ToString("F4");
                Wert8.Text = Convert.ToDouble(rs.Read("Monat_8")).ToString("F4");
                Wert9.Text = Convert.ToDouble(rs.Read("Monat_9")).ToString("F4");
                Wert10.Text = Convert.ToDouble(rs.Read("Monat_10")).ToString("F4");
                Wert11.Text = Convert.ToDouble(rs.Read("Monat_11")).ToString("F4");
                Wert12.Text = Convert.ToDouble(rs.Read("Monat_12")).ToString("F4");
            }
            rs.Close();

            if (mode == "Bearbeiten") btn_Speichern.Enabled = false;
            if (mode == "Neu")
            {
                btn_Speichern.Enabled = true;
                btn_Speichern_Unter.Enabled = false;
                btn_Ueberschreiben.Enabled = false;
            }
        }

        private void btn_Ueberschreiben_Click(object sender, EventArgs e)
        {
            double[] monat = new double[12];
            for (int i = 1; i <= 12; i++)
            {
                string val = this.Controls["Wert" + i.ToString()].Text;
                if (!Program.checkDouble(this.Controls["Wert" + i.ToString()], val)) return;
                monat[i - 1] = double.Parse(val);
            }

            StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
            // SaveHead prueft selbst auf ReadOnly und meldet ggf.
            if (ctrl.SaveHead(m_szStromname, comboBox_Stromtyp.Text, textBox_Beschreibung.Text, monat, false))
                MessageBox.Show("Daten aktualisiert!");
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();

            frm.m_szName = textBox_Stromname.Text;
            frm.SetControl();

            if (frm.ShowDialog() == DialogResult.OK)
            {
                StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
                if (ctrl.Exists(frm.m_szName)) { MessageBox.Show("Name existiert bereits!"); return; }

                textBox_Stromname.Text = frm.m_szName;

                double[] monat = new double[12];
                for (int i = 1; i <= 12; i++)
                    monat[i - 1] = double.Parse(this.Controls["Wert" + i.ToString()].Text);

                if (ctrl.SaveHead(textBox_Stromname.Text, comboBox_Stromtyp.Text, textBox_Beschreibung.Text, monat, true))
                    MessageBox.Show("Daten gespeichert!");
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (comboBox_Stromtyp.Text == "")
            {
                MessageBox.Show("Verbrauchertyp auswählen!");
                return;
            }
            for (int i = 1; i <= 12; i++)
            {
                if (this.Controls["Wert" + i.ToString()].Text == "")
                {
                    MessageBox.Show("Eingaben überprüfen!");
                    return;
                }
            }

            double[] monat = new double[12];
            for (int i = 1; i <= 12; i++)
                monat[i - 1] = double.Parse(this.Controls["Wert" + i.ToString()].Text);

            StromverbraucherStammCtrl ctrl = new StromverbraucherStammCtrl();
            if (ctrl.SaveHead(textBox_Stromname.Text, comboBox_Stromtyp.Text, textBox_Beschreibung.Text, monat, true))
                MessageBox.Show("Daten gespeichert!");
        }
    }
}
