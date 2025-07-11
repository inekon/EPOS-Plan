using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_ProjektOpen : Form
    {

        public Form_ProjektOpen()
        {
            InitializeComponent();
        }

        private void button_Abbrechen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form_ProjektOpen_Load(object sender, EventArgs e)
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadAll();
            ctrl.FillComboBox(comboBox_Projekte);
            ctrl = null;
        }

        private void button_Open_Click(object sender, EventArgs e)
        {
            ApplikationCtrl ctrl = new ApplikationCtrl();
            ProjektCtrl ctrl_projekt = new ProjektCtrl();
            ctrl_projekt.ReadSingle("Select * from Tab_Projekt where Projektname='" + comboBox_Projekte.Text + "'");
            ctrl.ID_Projekt = ctrl_projekt.ID;
            ctrl.Projektname = ctrl_projekt.Projektname;
            ctrl.Update();
            
            FormMain frm = (FormMain)Program.mainfrm;
            frm.SetProjekt(comboBox_Projekte.Text);
            Close();

        }

        private void button_Klima_Click(object sender, EventArgs e)
        {
            KlimaregionCtrl ctrl = new KlimaregionCtrl();
            ctrl.OpenExcel("Klima");
            textBox_Klima.Text = ctrl.Klimaregion;
            ctrl.WriteKlimaregion();
            ctrl.WriteKlimadaten();
        }
    }
}
