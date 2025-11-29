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
using System.Threading.Tasks;
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
            tabControl_Wizard.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl_Wizard.DrawItem += tabControl_Wizard_DrawItem;
            tabControl_Wizard.TabPages[1].Enabled = false;  
        }

        private void tabControl_Wizard_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            Color tabTextColor = Color.FromArgb(0x000000);
            var color = Color.FromArgb(tabTextColor.R, tabTextColor.G, tabTextColor.B);

            if (e.Index == tabControl_Wizard.SelectedIndex)
            {
                TextRenderer.DrawText(e.Graphics, tabControl_Wizard.TabPages[e.Index].Text, e.Font, e.Bounds, Color.FromArgb(0xffffff));
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, tabControl_Wizard.TabPages[e.Index].Text, e.Font, e.Bounds, color);
            }
        }

        public void SetTextProjekt(string szProjekt)
        {
            textBox_ProjektOpen.Text = szProjekt;
            pBox_Detailansicht.Enabled = true; 
        }

        private void pBox_Prozess_Click(object sender, EventArgs e)
        {
            Form_Prozesswaerme frm = new Form_Prozesswaerme();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_pwmodel.Clear();
   //         frm.SetControls(m_szProjektname);

            string sql = "SELECT Z_Projekt_Prozesswaerme.ID, Z_Projekt_Prozesswaerme.ID_Projekt, " +
                "Z_Projekt_Prozesswaerme.ID_Prozesswaerme, Tab_Prozesswaerme.Prozessname, Z_Projekt_Prozesswaerme.Summe " +
                "FROM Z_Projekt_Prozesswaerme INNER JOIN Tab_Prozesswaerme ON " +
                "Z_Projekt_Prozesswaerme.ID_Prozesswaerme = Tab_Prozesswaerme.ID " +
                " where ID_Projekt=" + m_ID_Projekt;
          
            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektProzesswaermeModel item = new Z_ProjektProzesswaermeModel();
                item.ID_Z = (int)rs.Read("ID");
                item.ID_Projekt = m_ID_Projekt;
                item.ID_Prozesswaerme = (int)rs.Read("ID_Prozesswaerme");
                item.szProzessname = (string)rs.Read("Prozessname");
                item.Summe = (double)rs.Read("Summe");
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

            if (frm.list_pwmodel.Count > 0)
                status |= 32;
            else status &= ~32;
            pBox_Prozess.Invalidate();
        }

        private void pBox_WBedarfDaten_Click(object sender, EventArgs e)
        {
            Wizard_Waermebedarf frm = new Wizard_Waermebedarf();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();

            frm.list_wbmodel.Clear();

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
                frm.list_wbmodel.Add(item);
            }
            rs.Close();

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);

            frm.ShowDialog();

            if (frm.result == DialogResult.OK)
            {
                wizctrl.Del_WaermebedarfExtern(m_ID_Projekt);
                wizctrl.Add_WaermebedarfExtern(m_ID_Projekt, frm.list_wbmodel);
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();

                if (frm.list_wbmodel.Count > 0)
                    status |= 16;
                else status &= ~16;
                pBox_WBedarfDaten.Invalidate();
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
            //frm.SetControls(m_szProjektname);

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
                if (frm.list_gebmodel.Count > 0)
                    status |= 8;
                else status &= ~8;
                pBox_Gebaude.Invalidate();

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
            
            ApplikationCtrl ctrl_app = new ApplikationCtrl();
            ProjektCtrl ctrl_projekt = new ProjektCtrl();

            if (Program.wizardctrl.Projektname == "") return;
            ctrl_projekt.ReadSingle("Select * from Tab_Projekt where Projektname='" + Program.wizardctrl.Projektname + "'");

            ctrl_app.m_ID_Projekt = ctrl_projekt.m_ID;
            ctrl_app.m_szProjektname = ctrl_projekt.m_szProjektname;
            ctrl_app.Update();
            
            SetTextProjekt(Program.wizardctrl.Projektname);
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

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);
            if (werzctrl.rows > 0) status |= 4; else status &= ~4;

            Z_ProjGebCtrl gebCtrl = new Z_ProjGebCtrl();
            gebCtrl.ReadAll(" select * from Z_ProjektGebaeude where ID_Projekt=" + m_ID_Projekt.ToString());
            if (gebCtrl.rows > 0) status |= 8; else status &= ~8;

            Z_ProjektGebGanglinieCtrl gebgangctrl = new Z_ProjektGebGanglinieCtrl();
            gebgangctrl.ReadAll(" select * from Z_ProjektWaermebedarf where ID_Projekt=" + m_ID_Projekt.ToString());
            if (gebgangctrl.rows > 0) status |= 16; else status &= ~16;

            Z_ProjektProzesswaermeCtrl proctrl = new Z_ProjektProzesswaermeCtrl();
            proctrl.ReadAll("select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + m_ID_Projekt.ToString());
            if (proctrl.rows > 0) status |= 32; else status &= ~32;

            Z_ProjektStromverbraucherCtrl strvctrl = new Z_ProjektStromverbraucherCtrl();
            strvctrl.ReadAll("select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + m_ID_Projekt.ToString());
            if (strvctrl.rows > 0) status |= 64; else status &= ~64;

            Z_ProjektStromganglinieCtrl strgctrl = new Z_ProjektStromganglinieCtrl();
            strgctrl.ReadAll("select * from Z_ProjektStromganglinie where ID_Projekt=" + m_ID_Projekt.ToString());
            if (strgctrl.rows > 0) status |= 128; else status &= ~128;

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.BHKW_TYP);
            if (werzctrl.rows > 0) status |= 256; else status &= ~256;

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

            if (frm.list_sbmodel.Count > 0)
                status |= 64;
            else status &= ~64;
            pBox_StdLastProfil.Invalidate();
        }

        private void pBox_StromProfilEigenes_Click(object sender, EventArgs e)
        {
            Form_EingStromTyp frm = new Form_EingStromTyp();
            frm.SetControls();
            frm.ShowDialog();
        }

        private void pBox_StromMessdaten_Click(object sender, EventArgs e)
        {
            Form_Stromganglinie frm = new Form_Stromganglinie();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();

            frm.DateiListe.Clear();

            string sql = "SELECT Z_ProjektStromganglinie.ID_Z, Z_ProjektStromganglinie.ID_Projekt, " +
                  "Z_ProjektStromganglinie.ID_Ganglinie, Tab_Stromganglinie.Bezeichner " +
                  "FROM Z_ProjektStromganglinie INNER JOIN Tab_Stromganglinie ON " +
                  "Z_ProjektStromganglinie.ID_Ganglinie = Tab_Stromganglinie.ID " +
                  " where ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektStromganglinieCtrl item = new Z_ProjektStromganglinieCtrl();
                item.m_ID_Z = (int)rs.Read("ID_Z");
                item.m_ID_Projekt = m_ID_Projekt;
                item.m_ID_Stromganglinie = (int)rs.Read("ID_Ganglinie");
                item.m_szStromganglinie = (string)rs.Read("Bezeichner");//item.Text;
                frm.DateiListe.Add(item);
            }
            rs.Close();

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);

            frm.ShowDialog();

            if (frm.result == DialogResult.OK)
            {
                wizctrl.Del_Stromganglinie(m_ID_Projekt);
                wizctrl.Add_Stromganglinie(m_ID_Projekt, frm.DateiListe);
  
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }

            if (frm.DateiListe.Count > 0)
                status |= 128;
            else status &= ~128;
            pBox_StromMessdaten.Invalidate();
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
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
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
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
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
            Form_Stromspeicher frm = new Form_Stromspeicher();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_werzmodel.Clear();
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SP_TYP);
            id_type = WizardItemClass.SP_TYP;

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
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);
            }

            if (frm.list_werzmodel.Count > 0)
                status |= 4;
            else status &= ~4;

            pBox_Stromspeicher.Invalidate();
        }

        private void pBox_Heizkessel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 1) == 1)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);
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
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);
                }
            }
        }

        private void pBox_Stromspeicher_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 4) == 4)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);
                }
            }
        }

        private void pBox_ProjektZuletzt_Click(object sender, EventArgs e)
        {
            ApplikationCtrl ctrl = new ApplikationCtrl();

            ctrl.ReadSingle("Select * from Tab_Applikation where ID=1");

            // falls zuletzt geöffnetes Projekt gelöscht wurde
            if (ctrl.m_szProjektname != "")
            {
                m_szProjektname = ctrl.m_szProjektname;
                m_ID_Projekt = ctrl.m_ID_Projekt;
                SetTextProjekt(m_szProjektname);
            }
            
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                Graphics g = pBox_ProjektZuletzt.CreateGraphics();
                Rectangle rt = pBox_ProjektZuletzt.ClientRectangle;
                rt.Width = rt.Width - 20;
                rt.Height = rt.Height - 20;
                rt.Y = rt.Y + 10;
                rt.X = rt.X + 10;
                Program.FillRoundedRectangle(g, brush, rt, 10);
                var t = Task.Run(async delegate
                {
                    await Task.Delay(200);
                    return 0;
                });
                t.Wait();
                pBox_ProjektZuletzt.Invalidate(); 
            }

            //MessageBox.Show("Projekt " + m_szProjektname  + " geöffnet!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Form_Hinweis frm = new Form_Hinweis("Hinweis", "Projekt " + m_szProjektname  + " geöffnet!");
            frm.Location = this.PointToScreen(tabControl_Wizard.PointToScreen(pBox_ProjektZuletzt.Location));
            frm.ShowDialog();

        }

        private void pBox_Detailansicht_Click(object sender, EventArgs e)
        {
            if (textBox_ProjektOpen.Text == "bitte auswählen!")
            {
                //    MessageBox.Show("Bitte zuerst ein Projekt auswählen!\n (<Projekt öffnen> oder <zuletzt geöffnet>)!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Form_Hinweis frm = new Form_Hinweis("Hinweis", "\r\nBitte zuerst ein Projekt auswählen!\r\nProjekt öffnen oder zuletzt geöffnet");
                System.Drawing.Point p1 = pBox_Detailansicht.Location;
                p1 = this.PointToScreen(p1);
                frm.Location = p1;
                frm.ShowDialog();
                return;
            }

            MenueCtrl ctrl = new MenueCtrl();
            if (textBox_ProjektOpen.Text == "nicht geöffnet") return;
            ctrl.ProjektOeffnen(true);
        }

        private void pBox_Gebaude_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 8) == 8)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);
                }
            }
        }

        private void pBox_WBedarfDaten_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 16) == 16)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);
                }
            }
        }

        private void pBox_Prozess_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 32) == 32)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);
                }
            }
        }

        private void pBox_StdLastProfil_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 64) == 64)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);
                }
            }
        }

        private void pBox_StromMessdaten_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 128) == 128)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);
                }
            }
        }

        private void pBox_ProjektZuletzt_Paint(object sender, PaintEventArgs e)
        {

        }

        private void tabPage5_Enter(object sender, EventArgs e)
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_ProjektOpen.Text + "'");
            
            label_Name.Text = textBox_ProjektOpen.Text;
            SimulationStrombedarf simulationStrombedarf = new SimulationStrombedarf();
            simulationStrombedarf.Berechnung(ctrl.m_ID);
            label_Strom.Text = simulationStrombedarf.Strombedarf_gesamt.ToString("F2") + " MWh/a";

            SimulationWaermebedarf simulationWaermebedarf = new SimulationWaermebedarf();
            simulationWaermebedarf.Waermebedarf_berechnen(ctrl.m_ID, ctrl.m_ID_Klimaregion);
            label_WBedarf.Text = simulationWaermebedarf.Waermebedarf_Gesamt.ToString("F2") + " MWh/a";

            label_Name.Left = pictureBox_Zusammenfassung.Width - label_Name.Width - 20;  
            label_WBedarf.Left = label_Name.Left + label_Name.Width - label_WBedarf.Width;
            label_Strom.Left = label_Name.Left + label_Name.Width - label_Strom.Width;
            
            label_Komponenten.Text = "";
            if ((status & 1) == 1) label_Komponenten.Text += "Heizkessel";
            if ((status & 2) == 2) label_Komponenten.Text += ", Wärmepumpe";
            if ((status & 4) == 4) label_Komponenten.Text += ", Stromspeicher";
            if ((status & 256) == 256) label_Komponenten.Text += ", BHKW";

            if (label_Komponenten.Text.StartsWith(", ")) label_Komponenten.Text = label_Komponenten.Text.Substring(2);
            label_Komponenten.Left = label_Name.Left + label_Name.Width - label_Komponenten.Width;

        }

        private void pBox_BHKW_Click(object sender, EventArgs e)
        {
            Form_BHKWEing frm = new Form_BHKWEing();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_werzmodel.Clear();
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.BHKW_TYP);
            id_type = WizardItemClass.BHKW_TYP;

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
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);
            }

            if (frm.list_werzmodel.Count > 0)
                status |= 256;
            else status &= ~256;

            pBox_BHKW.Invalidate();
        }

        private void label_Strom_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox_Zusammenfassung_Click(object sender, EventArgs e)
        {

        }

        private void label_Name_Click(object sender, EventArgs e)
        {

        }

        private void label_WBedarf_Click(object sender, EventArgs e)
        {

        }

        private void label_Komponenten_Click(object sender, EventArgs e)
        {

        }

        private void pBox_BHKW_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 256) == 256)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);
                }
            }
        }

        private void tabControl_Wizard_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPageIndex < 0) return;

            if (e.TabPageIndex == 1 && textBox_ProjektOpen.Text == "bitte auswählen!")
            {
                e.Cancel = true;
                //MessageBox.Show("Bitte zuerst ein Projekt auswählen!\n (<Projekt öffnen> oder <zuletzt geöffnet>)!", "Hinweis", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                Form_Hinweis frm = new Form_Hinweis("Hinweis", "Bitte zuerst ein Projekt auswählen!\r\nProjekt öffnen oder zuletzt geöffnet");
                System.Drawing.Point p1 = pBox_Weiter.Location;
                p1 = this.PointToScreen(p1);
                p1.Y -= 200;
                frm.Location = p1;
                frm.ShowDialog();
            }

            //e.Cancel = !e.TabPage.Enabled;
        }
    }
}
