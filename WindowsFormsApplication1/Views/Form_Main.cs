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
    public partial class Form_Main : Form
    {
        public void SetProjekt(string szProjekt)
        {
            textBox_Projekt.Text = szProjekt;
        }
        public void SetKlimaregion(string szKlimaregion)
        {
            textBox_Klimaregion.Text = szKlimaregion;
        }

        public Form_Main()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized; 
        }


    }
}
