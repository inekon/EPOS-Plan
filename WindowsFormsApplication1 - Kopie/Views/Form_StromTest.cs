using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_StromTest : Form
    {
        public int m_ID_Projekt;

        public Form_StromTest()
        {
            InitializeComponent();
        }

        public void SetControls(int ID_Projekt)
        {
            m_ID_Projekt = ID_Projekt;
            StromspeicherCtrl ctrl = new StromspeicherCtrl();
            ctrl.ReadAll();
            
            for (int i = 0; i < ctrl.rows; i++)
            {
                StromspeicherModel item = ctrl.items[i];
                comboBox_Stromspeicher.Items.Add(item.m_szBezeichner);
            }   

            // stromspeicher zum Projekt
            WErzeugerCtrl wErzeugerCtrl = new WErzeugerCtrl();
            wErzeugerCtrl.ReadSingle("select * from Tab_Energieanlagen where ID_Projekt=" + ID_Projekt + " and ID_TYPE=" + WizardItemClass.SP_TYP);
            int idsp = wErzeugerCtrl.ID_SP; 
            ctrl.ReadSingle(idsp);
            comboBox_Stromspeicher.Text = ctrl.m_szBezeichner;

            // Stromlastgang zum Projekt
            Z_ProjektStromganglinieCtrl z_ProjektStromganglinieCtrl = new Z_ProjektStromganglinieCtrl();
            z_ProjektStromganglinieCtrl.ReadAll("Select * from Z_ProjektStromganglinie where ID_Projekt=" + ID_Projekt);
            string szStromganglinie = z_ProjektStromganglinieCtrl.items[0].m_szStromganglinie; 
            comboBox_Lastgang.Text = szStromganglinie;

            // Stromverbraucher zum Projekt
            Z_ProjektStromverbraucherCtrl ctrl_profil = new Z_ProjektStromverbraucherCtrl();
            ctrl_profil.ReadAll("Select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + ID_Projekt);
            string szStromverbraucher = ctrl_profil.items[0].m_szVerbraucher;
            comboBox_Stromprofil.Text = szStromverbraucher;
        }

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            StromTestClass stromTestClass = new StromTestClass();
            stromTestClass.m_ID_Projekt = m_ID_Projekt;
            stromTestClass.m_szStromspeicher = comboBox_Stromspeicher.Text; 
            stromTestClass.MyTestProfil(comboBox_Stromprofil.Text);
        }

        private void btn_Simulation_Lastgang_Click(object sender, EventArgs e)
        {
            StromTestClass stromTestClass = new StromTestClass();
            stromTestClass.m_ID_Projekt = m_ID_Projekt;
            stromTestClass.m_szStromspeicher = comboBox_Stromspeicher.Text;
            stromTestClass.MyTestLastgang(comboBox_Lastgang.Text);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            StromTestClass stromTestClass = new StromTestClass();
            stromTestClass.KühlleistungDaten(textBox_Kuehlung.Text);
        }
    }
}
