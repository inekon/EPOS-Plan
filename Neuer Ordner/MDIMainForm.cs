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

        private void MenuItem_Neu_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektNeu();
        }

        private void MDIMainForm_Load(object sender, EventArgs e)
        {
            Program.menuectrl.OpenForm(typeof(Form_Start), true);
        }

        private void MenuItem_zuletztGeöffnet_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektOeffnen(true);
        }

        private void MenuItem_ProjektLöschen_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektDelete();
        }

        private void MenuItem_Klimadaten_Click(object sender, EventArgs e)
        {
            Program.menuectrl.OpenForm(typeof(Form_Klimadaten), false);
        }

        private void MenuItem_ProjektBearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektBearbeiten();    
        }

        private void MenuItem_ProjektOeffnen_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektOeffnen();
        }

        private void MenuItem_WPBearbeiten_Click_1(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.WP_Administration();
        }

        private void MenuItem_Stromspeicher_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.StromspeicherBearbeiten();
        }

        private void MenuItem_GebBearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.GebaeudeBearbeiten();
        }

        private void MenuItem_GebTypen_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.GebaeudetypenBearbeiten();
        }

        private void MenuItem_WaermebedarfExtern_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.WaermebedarfExtern(); 
        }

        private void MenuItem_Prozesswaerme_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Prozesswaerme(); 
        }

        private void MenuItem_Stromverbraucher_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Stromverbraucher(); 
        }

        private void testToolStripMenuItem_Click(object sender, EventArgs e)
        {
            StromTestClass st = new StromTestClass();
            st.MyTest(); 
        }

        private void MenuItem_Stromganglinie_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Stromganglinie();
        }

        private void MeniItem_VDI3805_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.WPImport();
        }
  
    }
}

