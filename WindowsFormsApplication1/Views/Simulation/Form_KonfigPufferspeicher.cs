using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApplication1.Views.Simulation
{
    public partial class Form_KonfigPufferspeicher : Form
    {
        public Z_ProjektPufferSpModel model = new Z_ProjektPufferSpModel();
        public List<string> listErzeuger = new List<String>();
        public List<string> listPufferSp = new List<String>();
        public int m_ID_Projekt = 0;    

        public Form_KonfigPufferspeicher()
        {
            InitializeComponent();
        }

        public void SetControls()
        {
            comboBox_Erzeuger.Items.AddRange(listErzeuger.ToArray());
            comboBox_Puffer.Items.AddRange(listPufferSp.ToArray());
        }

        private void btn_Abbruch_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel; 
            Close();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            model.Erzeuger = comboBox_Erzeuger.Text;
            model.PufferSp = comboBox_Puffer.Text;
            model.Vorlauf = Int32.Parse(textBox_Vorlauf.Text);
            model.Ruecklauf = Int32.Parse(textBox_Ruecklauf.Text);
            this.DialogResult = DialogResult.OK;
        }

        private void comboBox_Puffer_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet(); 
            rs.Open("select * from Abfrage_Erzeuger_Vorlauftemperaturen where Typ='" + comboBox_Erzeuger.Text + "' and ID_Projekt=" + m_ID_Projekt);
            if (rs.Next())
            {
                textBox_Vorlauf.Text = rs.Read("Vorlauf").ToString();
            }
            rs.Close();
            rs.Open("select * from Abfrage_Erzeuger_Ruecklauftemperaturen where Typ='" + comboBox_Erzeuger.Text + "' and ID_Projekt=" + m_ID_Projekt);
            if (rs.Next())
            {
                textBox_Ruecklauf.Text = rs.Read("Ruecklauf").ToString();
            }
            rs.Close();
        }
    }
}
