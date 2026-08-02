using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_EingDBBrauchwasser : Form
    {
        public string m_szBezeichner;
        public string m_szBeschreibung;
        public string m_szBrauchwassertyp;
        public string mode;
        
        public Form_EingDBBrauchwasser()
        {
            InitializeComponent();

            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Brauchwassertyp_STAMM order by Bezeichner");

            while(rs.Next())
            {
                comboBox_Brauchwassertyp.Items.Add(rs.Read("Bezeichner"));
            }
            rs.Close(); 
         }

        public void SetControls()
        {
            RecordSet rs = new RecordSet();

            textBox_Bezeichner.Text = m_szBezeichner;
            textBox_Beschreibung.Text = m_szBeschreibung;
            comboBox_Brauchwassertyp.Text = m_szBrauchwassertyp;
            rs.Open("select * from Tab_Brauchwasser_STAMM where Bezeichner='" + textBox_Bezeichner.Text + "'");

            if (rs.Next())
            {
                Wert1.Text = ((double)rs.Read("Monat_1")).ToString("F4");
                Wert2.Text = ((double)rs.Read("Monat_2")).ToString("F4");
                Wert3.Text = ((double)rs.Read("Monat_3")).ToString("F4");
                Wert4.Text = ((double)rs.Read("Monat_4")).ToString("F4");
                Wert5.Text = ((double)rs.Read("Monat_5")).ToString("F4");
                Wert6.Text = ((double)rs.Read("Monat_6")).ToString("F4");
                Wert7.Text = ((double)rs.Read("Monat_7")).ToString("F4");
                Wert8.Text = ((double)rs.Read("Monat_8")).ToString("F4");
                Wert9.Text = ((double)rs.Read("Monat_9")).ToString("F4");
                Wert10.Text = ((double)rs.Read("Monat_10")).ToString("F4");
                Wert11.Text = ((double)rs.Read("Monat_11")).ToString("F4");
                Wert12.Text = ((double)rs.Read("Monat_12")).ToString("F4");
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
            
            for (int i = 1; i <= 12; i++)
            {
                string val = this.Controls["Wert" + i.ToString()].Text;
                if (!Program.checkDouble(this.Controls["Wert" + i.ToString()], val)) return;
            }
            
            double[] monat = new double[12];
            for (int i = 1; i <= 12; i++)
                monat[i - 1] = double.Parse(this.Controls["Wert" + i.ToString()].Text);

            BrauchwasserStammCtrl ctrl = new BrauchwasserStammCtrl();
            // SaveHead prueft selbst auf ReadOnly und meldet ggf.
            if (ctrl.SaveHead(m_szBezeichner, comboBox_Brauchwassertyp.Text, textBox_Beschreibung.Text, monat, false))
                MessageBox.Show("Daten aktualisiert!");
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
             Close();
        }

        private void btn_Speichern_Unter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();
            
            frm.m_szName = textBox_Bezeichner.Text;
            frm.SetControl();
            Point p1 = btn_Speichern_Unter.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;

            if (frm.ShowDialog() == DialogResult.OK)
            {
                BrauchwasserStammCtrl ctrl = new BrauchwasserStammCtrl();
                if (ctrl.Exists(frm.m_szName)) { MessageBox.Show("Name existiert bereits!"); return; }
                textBox_Bezeichner.Text = frm.m_szName;

                double[] monat = new double[12];
                for (int i = 1; i <= 12; i++)
                    monat[i - 1] = double.Parse(this.Controls["Wert" + i.ToString()].Text);

                if (ctrl.SaveHead(textBox_Bezeichner.Text, comboBox_Brauchwassertyp.Text, textBox_Beschreibung.Text, monat, true))
                    MessageBox.Show("Daten gespeichert!");
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if(comboBox_Brauchwassertyp.Text == "" )
            {
                MessageBox.Show("Brauchwassertyp auswählen!");
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
            double result = 0;
            for (int i = 1; i <= 12; i++)
            {
                if(!double.TryParse(this.Controls["Wert" + i.ToString()].Text, out result))
                {
                    MessageBox.Show("Eingaben überprüfen!");
                    return;
                }
            }

            double[] monat = new double[12];
            for (int i = 1; i <= 12; i++)
                monat[i - 1] = double.Parse(this.Controls["Wert" + i.ToString()].Text);

            BrauchwasserStammCtrl ctrl = new BrauchwasserStammCtrl();
            if (ctrl.SaveHead(textBox_Bezeichner.Text, comboBox_Brauchwassertyp.Text, textBox_Beschreibung.Text, monat, true))
                MessageBox.Show("Daten gespeichert!");
        }

 
  
    }
}
