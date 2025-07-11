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
    public partial class MDIMainForm : Form
    {
        public MDIMainForm()
        {
            InitializeComponent();
        }

        private void WP_Click(object sender, EventArgs e)
        {
            Program.menuectrl.OpenForm(typeof(FormTest),false); 
        }

        private void verwaltungToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.menuectrl.OpenForm(typeof(Form_Projekt),false); 
        }

        private void MDIMainForm_Load(object sender, EventArgs e)
        {
            // Hauptformular öffnen
            Program.mainfrm = (Form_Main)Program.menuectrl.OpenForm(typeof(Form_Main));
            Program.menuectrl.SetProjektname();
            Program.menuectrl.SetKlimaregion();
        }

        private void öffnenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.menuectrl.OpenForm(typeof(Form_ProjektOpen),false);
        }

        private void MDIMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
        }

        private void klimadatenEinlesenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Program.menuectrl.OpenForm(typeof(Form_Klimaregionen), false);
        }

        private void wärmepumpenToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Program.menuectrl.OpenForm(typeof(FormTest), false);
        }

        private void toolStripMenuItem_Stromspeicher_Click(object sender, EventArgs e)
        {
            Program.menuectrl.OpenForm(typeof(Form1), false);
        }
    }
}

