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
            KlimaregionCtrl ctrlklima = new KlimaregionCtrl();
            ctrl.ReadAll();
            ctrl.FillComboBox(comboBox_Projekte);

            ctrlklima.ReadAllKlimaregion();
            ctrlklima.FillComboBox(comboBox_Klimaregion);

            ctrl = null;
            ctrlklima = null;
        }

        private void button_Open_Click(object sender, EventArgs e)
        {
            ApplikationCtrl ctrl = new ApplikationCtrl();
            ProjektCtrl ctrl_projekt = new ProjektCtrl();
            KlimaregionCtrl ctrl_klimaregion = new KlimaregionCtrl();
            
            ctrl_projekt.ReadSingle("Select * from Tab_Projekt where Projektname='" + comboBox_Projekte.Text + "'");
            ctrl_klimaregion.ReadSingleKlimaregion("Select * from Tab_Klimaregion where Name='" + comboBox_Klimaregion.Text + "'");

            ctrl.ID_Projekt = ctrl_projekt.ID;
            ctrl.Projektname = ctrl_projekt.Projektname;
            ctrl.ID_Klimaregion = ctrl_klimaregion.ID_Klimaregion;
            ctrl.Update();
            
            Form_Main frm = (Form_Main)Program.mainfrm;
            frm.SetProjekt(comboBox_Projekte.Text);
            frm.SetKlimaregion(comboBox_Klimaregion.Text);
            Close();

        }

        private void button_Klima_Click(object sender, EventArgs e)
        {
            KlimaregionCtrl ctrl = new KlimaregionCtrl();
            ctrl.OpenExcel("Klima");
            ctrl.WriteKlimaregion();
            ctrl.WriteKlimadaten();
        }

        private void comboBox_Klimaregion_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
