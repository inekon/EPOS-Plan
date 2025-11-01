using Microsoft.Office.Interop.Excel;
using NReco.Csv;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using static System.Net.WebRequestMethods;
using Rectangle = System.Drawing.Rectangle;

namespace WindowsFormsApplication1
{
    public partial class Form_Start : Form
    {
        public int m_ID_Projekt = 0;
        public string m_szProjektname = "";
        public int status = 0;

        public Form_Start()
        {
            InitializeComponent();
        }

        private void Form_Start_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
        }

        public void SetTextProjekt(string szProjekt)
        {
            textBox_ProjektOpen.Text = szProjekt;
        }

        private void pBox_Prozess_Click(object sender, EventArgs e)
        {
            Form_Prozesswaerme frm = new Form_Prozesswaerme();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_pwmodel.Clear();
            frm.SetControls(m_szProjektname);

            string sql = "SELECT Z_Projekt_Prozesswaerme.ID, Z_Projekt_Prozesswaerme.ID_Projekt, " +
                "Z_Projekt_Prozesswaerme.ID_Prozesswaerme, Tab_Prozesswaerme.Prozessname " +
                "FROM Z_Projekt_Prozesswaerme INNER JOIN Tab_Prozesswaerme ON " +
                "Z_Projekt_Prozesswaerme.ID_Prozesswaerme = Tab_Prozesswaerme.ID " +
                " where ID_Projekt=" + m_ID_Projekt;
            //+ " and Tab_Prozesswaerme.Prozessname='" + lvitem.Text +
            //"' and Z_Projekt_Prozesswaerme.ID=" + lvitem.SubItems[3].Text;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektProzesswaermeModel item = new Z_ProjektProzesswaermeModel();
                item.ID_Z = (int)rs.Read("ID");
                item.ID_Projekt = m_ID_Projekt;
                item.ID_Prozesswaerme = (int)rs.Read("ID_Prozesswaerme");
                item.szProzessname = (string)rs.Read("Prozessname");//item.Text;
                frm.list_pwmodel.Add(item);
            }

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_Prozess(m_ID_Projekt);
                wizctrl.Add_Projekt_Prozess(m_ID_Projekt, frm.list_pwmodel);

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void pBox_WBedarfDaten_Click(object sender, EventArgs e)
        {
            Form_Waermebedarf frm = new Form_Waermebedarf();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();

            frm.DateiListe.Clear();

            string sql = "SELECT Z_ProjektWaermebedarf.ID_Z, Z_ProjektWaermebedarf.ID_Projekt, " +
                  "Z_ProjektWaermebedarf.ID_Ganglinie, Tab_Waermebedarf.Bezeichner " +
                  "FROM Z_ProjektWaermebedarf INNER JOIN Tab_Waermebedarf ON " +
                  "Z_ProjektWaermebedarf.ID_Ganglinie = Tab_Waermebedarf.ID " +
                  " where ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektGebGanglinieCtrl item = new Z_ProjektGebGanglinieCtrl();
                item.m_ID_Z = (int)rs.Read("ID_Z");
                item.m_ID_Projekt = m_ID_Projekt;
                item.m_ID_Ganglinie = (int)rs.Read("ID_Ganglinie");
                item.m_szBezeichner = (string)rs.Read("Bezeichner");//item.Text;
                frm.DateiListe.Add(item);
            }
            rs.Close();

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);

            frm.ShowDialog();

            if (frm.result == DialogResult.OK)
            {
                wizctrl.Del_WaermebedarfExtern(m_ID_Projekt);
                wizctrl.Add_WaermebedarfExtern(m_ID_Projekt, frm.DateiListe);
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void pBox_Gebaude_Click(object sender, EventArgs e)
        {
            Z_ProjGebModel item;
            Form_Gebaeude frm = new Form_Gebaeude();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            
            frm.list_gebmodel.Clear();
            frm.SetControls(m_szProjektname);

            string sql = "SELECT Z_ProjektGebaeude.ID, Z_ProjektGebaeude.ID_Gebaeude, Z_ProjektGebaeude.[ID_Projekt], " +
                "[DBGebaeude].Gebaeudename, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, " +
                "dezWarmwasserbereitung, Gebaeudeart, Beschreibung  FROM [DBGebaeude] " +
                "INNER JOIN Z_ProjektGebaeude ON [DBGebaeude].ID = Z_ProjektGebaeude.ID_Gebaeude" +
                " where Z_ProjektGebaeude.ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                item = new Z_ProjGebModel();
                item.ID_Z = (int)rs.Read("ID");
                item.ID_Projekt = m_ID_Projekt;
                item.ID_Gebaeude = (int)rs.Read("ID_Gebaeude");
                item.Gebaeudename = (string)rs.Read("Gebaeudename");
                item.Wohnflaeche = (double)rs.Read("Wohnflaeche_Waermebedarf");
                item.Einheit = (string)rs.Read("Einheit_Waermebedarf_Wohnflaeche");
                item.Jahresnutzungsgrad = (double)rs.Read("Jahresnutzungsgrad");
                item.DezentralWarmwasser = (bool)rs.Read("dezWarmwasserbereitung");
                item.Gebaeudeart = (string)rs.Read("Gebaeudeart");
                item.Beschreibung = (string)rs.Read("Beschreibung");

                frm.list_gebmodel.Add(item);
            }

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_ZuordungGebäude(m_ID_Projekt);
                wizctrl.Add_Projekt_ZuordungGebäude(m_ID_Projekt, frm.list_gebmodel);

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void pBox_ProjektNeu_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektNeu();
        }

        private void pBox_ProjektOeffnen_Click(object sender, EventArgs e)
        {
            Form_ProjektOpen frm = new Form_ProjektOpen();
            ApplikationCtrl ctrl = new ApplikationCtrl();

            ctrl.ReadSingle("Select * from Tab_Applikation where ID=1");

            DialogResult ret = frm.ShowDialog();
            if (ret == DialogResult.OK)
            {
                m_szProjektname = frm.m_szProjekt;
                m_ID_Projekt = frm.m_ID_Projekt;
                SetTextProjekt(frm.m_szProjekt);
            }
        }

        private void pBox_ProjektZuletzt_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektOeffnen(true);
        }

        private void pBox_Bearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektBearbeiten();
        }

        private void pBox_Weiter_Click(object sender, EventArgs e)
        {
            if (tabControl_Wizard.SelectedIndex >= tabControl_Wizard.TabCount-1) return;
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.KESSEL_TYP);
            if (werzctrl.rows > 0) status |= 1; else status &= ~1;
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.WP_TYP);
            if (werzctrl.rows > 0) status |= 2; else status &= ~2;


            tabControl_Wizard.SelectedIndex = tabControl_Wizard.SelectedIndex + 1;
        }

        private void pBox_Zurueck_Click(object sender, EventArgs e)
        {
            if (tabControl_Wizard.SelectedIndex <= 0) return;
            tabControl_Wizard.SelectedIndex = tabControl_Wizard.SelectedIndex -1;
        }

        private void pBox_StdLastProfil_Click(object sender, EventArgs e)
        {
            Form_Stromverbraucher frm = new Form_Stromverbraucher();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_sbmodel.Clear();
            frm.SetControls(m_szProjektname);

            string sql = "SELECT Z_Projekt_Stromverbraucher.ID, Z_Projekt_Stromverbraucher.ID_Projekt, " +
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher, Tab_Stromverbraucher.Bezeichner " +
                "FROM Z_Projekt_Stromverbraucher INNER JOIN Tab_Stromverbraucher ON " +
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher = Tab_Stromverbraucher.ID " +
                " where ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektStromverbraucherModel item = new Z_ProjektStromverbraucherModel();
                item.m_ID_Z = (int)rs.Read("ID");
                item.m_ID_Projekt = m_ID_Projekt;
                item.m_ID_Stromverbraucher = (int)rs.Read("ID_Stromverbraucher");
                item.m_szVerbraucher = (string)rs.Read("Bezeichner");//item.Text;
                frm.list_sbmodel.Add(item);
            }

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_Stromverbraucher(m_ID_Projekt);
                wizctrl.Add_Projekt_Stromverbraucher(m_ID_Projekt, frm.list_sbmodel);

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void pBox_StromProfilEigenes_Click(object sender, EventArgs e)
        {

        }

        private void pBox_StromMessdaten_Click(object sender, EventArgs e)
        {
            MenueCtrl ctrl = new MenueCtrl();
            ctrl.Stromganglinie();
        }

        private void textBox_ProjektOpen_TextChanged(object sender, EventArgs e)
        {

        }

        private void pBox_WP_Click(object sender, EventArgs e)
        {
            WizWP frm = new WizWP();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_werzmodel.Clear();
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.WP_TYP);
            id_type = WizardItemClass.WP_TYP;

            WErzeugerModel item = new WErzeugerModel();
            for (int i = 0; i < werzctrl.rows; i++)
            {
                frm.list_werzmodel.Add(werzctrl.items[i]);
            }

            frm.SetControls(m_szProjektname);
            DialogResult result = frm.ShowDialog();

            if (result == DialogResult.OK)
            {
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, WizardItemClass.WP_TYP);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);
            }

            if (frm.list_werzmodel.Count > 0)
                status |= 2;
            else status &= ~2;
            pBox_WP.Invalidate();
        }

        private void pBox_Heizkessel_Click(object sender, EventArgs e)
        {
            Form_Heizkessel frm = new Form_Heizkessel();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_heizkesselmodel.Clear();

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.KESSEL_TYP);
            id_type = WizardItemClass.KESSEL_TYP;
            
            for (int i = 0; i < werzctrl.rows; i++)
            {
                WErzeugerModel item = new WErzeugerModel();
                item.ID = werzctrl.items[i].ID;
                item.ID_Kessel = werzctrl.items[i].ID_Kessel;
                item.ID_Type = werzctrl.items[i].ID_Type;
                item.Bezeichner = werzctrl.items[i].Bezeichner;

                frm.list_heizkesselmodel.Add(item);
            }

            frm.SetControls(m_szProjektname);
            frm.m_nType = id_type;
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                // Datenbank aktualisieren
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, WizardItemClass.KESSEL_TYP);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_heizkesselmodel);
                if (frm.list_heizkesselmodel.Count > 0)
                    status |= 1;
                else status &= ~1;
                pBox_Heizkessel.Invalidate();

                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void pBox_Stromspeicher_Click(object sender, EventArgs e)
        {

        }

        private void pBox_Heizkessel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 1) == 1)
                {
                    Program.FillRoundedRectangle(e.Graphics, brush, e.ClipRectangle, 10);
                }
            }
        }

        private void pBox_WP_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 2) == 2)
                {
                    Program.FillRoundedRectangle(e.Graphics, brush, e.ClipRectangle, 10);
                }
            }
        }
    }
}
