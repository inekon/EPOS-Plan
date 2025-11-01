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
    public partial class Form_BHKWEing : Form
    {
        public Form_BHKWEing()
        {
            InitializeComponent();
        }

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void Form_BHKWEing_Load(object sender, EventArgs e)
        {
            BHKWCtrl ctrl = new BHKWCtrl();

            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_BHKW_DB.Items.Add(ctrl.items[i].m_szBezeichner); 
            }

        }
    }
}