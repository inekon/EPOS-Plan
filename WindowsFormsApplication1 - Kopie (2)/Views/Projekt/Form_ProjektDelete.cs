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
    public partial class Form_ProjektDelete : Form
    {
        public string szProjekt;
        public string szklima;
        public int ID_Klima;
        public int ID_Projekt;

        public Form_ProjektDelete()
        {
            InitializeComponent();
        }

        private void Form_ProjektDelete_Load(object sender, EventArgs e)
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadAll();
            ctrl.FillComboBox(comboBox_Projekte);
            ctrl = null;
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            szProjekt = comboBox_Projekte.Text;
            this.DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
           this.DialogResult = DialogResult.Cancel;
           Close();
        }

        private void comboBox_Projekte_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            rs.Open("Select * from Tab_Projekt where projektname='" + comboBox_Projekte.Text + "'");
            rs.Next();
            ID_Projekt = (int)rs.Read("ID");
            rs.Close();
        }
 
     }
}
