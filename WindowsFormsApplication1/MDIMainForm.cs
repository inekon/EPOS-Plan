using Microsoft.Win32;
using System;
using System.Globalization;
using System.Reflection;
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
            Program.startfrm = (Form_Start)Program.menuectrl.OpenForm(typeof(Form_Start), true);
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

        private void MenuItem_WPBearbeiten_Click(object sender, EventArgs e)
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

        private void MenuItem_Stromganglinie_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Stromganglinie();
        }

        private void MenuItem_WP_VDI3805_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.WPImport();
        }

        private void MenuItem_BHKW_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.BHKW();
        }

        private void MenuItem_SolThermGanglinie_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Solarganglinie();
        }

        private void MenuItem_Update_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.DBUpdate();
        }

        private void MenuItem_Version_Click(object sender, EventArgs e)
        {
            Version currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
            MessageBox.Show("Version: " + currentVersion.ToString());
        }

        private void MenuItem_Lizenz_Click(object sender, EventArgs e)
        {

        }

        private void Deutsch_Click(object sender, EventArgs e)
        {
            var culture_de = new CultureInfo("de-DE");

            // Erzwingen der deutschen Sprache
            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\\wp-plan", true);
            var nLanguage = (int)key.GetValue("Language", 0);
            if (nLanguage == 0) return;
            key.SetValue("Language", 0, RegistryValueKind.DWord);
            Application.Restart();
        }

        private void Englisch_Click(object sender, EventArgs e)
        {
            var culture_de = new CultureInfo("en-US");

            RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\\wp-plan", true);
            var nLanguage = (int)key.GetValue("Language", 0);
            if (nLanguage == 1) return;
            key.SetValue("Language", 1, RegistryValueKind.DWord);
            Application.Restart();
        }

        private void MenuItem_SPKBearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Kessel();
        }

        private void MeniItem_PufferSp_VDI3805_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.PufferSPImport();
        }

        private void MenuItem_PufferSpBearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.PufferSp();
        }

        private void MenuItem_Brauchwasser_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Brauchwasser();
        }

        private void MenuItem_PV_Bearbeiten_Click(object sender, EventArgs e)
        {
            Form_AdminPV frm = new Form_AdminPV();
            frm.ShowDialog();
        }

        private void MenuItem_PV_Import_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.PVImport();
        }

        private void MenuItem_ST_Bearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Solarkollektoren();
        }

        private void MenuItem_ST_Import_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.SolarThermieImport();
        }
        
        private void MenuItem_Import_Heizkessel_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.SPKImport();
        }

        private void MenuItem_Kessel_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Kessel();
        }

        private void MenuItem_PufferSp_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.PufferSp();
        }

        private void MenuItem_WP_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.WP_Administration();
        }

        private void MenuItem_PV_Import_CEC_Click(object sender, EventArgs e)
        {
            Main_PV_Test frm = new Main_PV_Test();
            frm.ShowDialog();
        }

        private void MenuItem_PV_Import_PAN_Click(object sender, EventArgs e)
        {
            Main_PV_Test frm = new Main_PV_Test();
            frm.ShowDialog();
        }

        private void MenuItem_Kosten_Click(object sender, EventArgs e)
        {
            int id = Program.startfrm.m_ID_Projekt;
            if (id != 0)
            {
                using (var form = new Form_Kosten(id))
                {
                    form.ShowDialog(); // Öffnet das Fenster als modaler Dialog
                }
            }
            else MessageBox.Show("Projekt auswählen!");
        }
    }
}

