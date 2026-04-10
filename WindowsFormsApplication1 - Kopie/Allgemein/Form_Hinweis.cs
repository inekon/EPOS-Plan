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
    public partial class Form_Hinweis : Form
    {
        public Form_Hinweis(string szTitle, string szMessage)
        {
            InitializeComponent();
            this.Text = szTitle;
            label_Hinweis.Text = szMessage;  
        }

        private async void Form_Hinweis_Load(object sender, EventArgs e)
        {
            await Task.Delay(3000);
            Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Close();    
        }
    }
}
