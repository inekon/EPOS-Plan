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
    public partial class Form_Sp_ItemNeu : Form
    {
        public StromspeicherModel model = new StromspeicherModel();
        public DialogResult result;
        public string m_szName = "";
 
        public Form_Sp_ItemNeu()
        {
            InitializeComponent();
            textBox_Bezeichner.Text = "";
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            m_szName = textBox_Bezeichner.Text;
            if (m_szName == "") { MessageBox.Show("Bezeichnung eingeben!"); return; } 
            result = DialogResult.OK; 
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            result = DialogResult.Cancel; 
            Close();
        }
    }
}
