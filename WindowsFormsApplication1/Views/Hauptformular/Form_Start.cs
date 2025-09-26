using NReco.Csv;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;

namespace WindowsFormsApplication1
{
    public partial class Form_Start : Form
    {
        int index = 0;

        public Form_Start()
        {
            InitializeComponent();
        }

 

        private void Form_Start_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
        }
 
    }
}
