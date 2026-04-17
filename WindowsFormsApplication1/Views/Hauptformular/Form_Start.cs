using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using Rectangle = System.Drawing.Rectangle;

namespace WindowsFormsApplication1
{
    public partial class Form_Start : Form
    {
        public int m_ID_Projekt = 0;
        public string m_szProjektname = "";
        public int status = 0;
        private bool bUpdateWizardSymbole = false;

        private SimulationStrombedarf simulationStrombedarf = new SimulationStrombedarf();
        private SimulationWaermebedarf simulationWaermebedarf = new SimulationWaermebedarf();

        // Definition des Dictionarys
        // Key: Name des Controls oder ein Tag
        // Value: Die Methode, die aufgerufen werden soll
        private Dictionary<string, Action<object, EventArgs>> _clickEvents;

        public Form_Start()
        {
            InitializeComponent();
            textBox_ProjektOpen.Text = MyResource.Resource.Text_Select;
            InitEventDictionary();
        }

        private void Form_Start_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
            tabControl_Wizard.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl_Wizard.DrawItem += tabControl_Wizard_DrawItem;
            for (int i = 1; i < tabControl_Wizard.TabPages.Count; i++) tabControl_Wizard.TabPages[i].Enabled = false;
            btn_Weiter.MakeSmoothButton(6);
            btn_Zurueck.MakeSmoothButton(6);
            btn_Weiter.BackColor = Color.LightGray;
            btn_Zurueck.BackColor = Color.LightGray;
            btn_SimKonfig.MakeSmoothButton(6);
            btn_SimKonfig.BackColor = Color.LightGray;
  
            label_Haus.Text = "\uE80F";
            label_Haus.Parent = pictureBox2;
            label_Haus.BackColor = Color.Transparent;
            label_Haus.Location = new Point(30, (pictureBox2.Height -label_Haus.Height)/2); // Achtung: Location ist jetzt relativ zum Panel!

            // DropDownStyle auf DropDownList
            comboBox_Klimaregion.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_Klimaregion.FlatStyle = FlatStyle.Popup;
            // Hintergrundfarbe auf Weiß setzen
            comboBox_Klimaregion.BackColor = Color.White;
            // Textfarbe auf Schwarz
            comboBox_Klimaregion.ForeColor = Color.Black;
            ComboBox_Klimaregion();
            comboBox_Klimaregion.SetPlaceholder("Bitte zuerst ein Projekt auswählen.");
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
            pBox_ProjektDetails.Enabled = true; 
        }

        private void pBox_Prozess_Click(object sender, EventArgs e)
        {
            Form_Prozesswaerme frm = new Form_Prozesswaerme();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_pwmodel.Clear();

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
            Form_Waermebedarf frm = new Form_Waermebedarf();
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
                "[Tab_Gebaeude].Gebaeudename, [Tab_Gebaeude].Baualtersklasse, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, " +
                "dezWarmwasserbereitung, Gebaeudeart, Beschreibung  FROM [Tab_Gebaeude] " +
                "INNER JOIN Z_ProjektGebaeude ON [Tab_Gebaeude].ID = Z_ProjektGebaeude.ID_Gebaeude" +
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
                item.Baualtersklasse = (string)rs.Read("Baualtersklasse");

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

        public void SetKlima(string szKlima)
        {
            comboBox_Klimaregion.Text = szKlima;
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
                for (int i = 1; i < tabControl_Wizard.TabPages.Count; i++) tabControl_Wizard.TabPages[i].Enabled = true;
            }
            label_ProjektStatus.Text = "✔";
            label_ProjektStatus.ForeColor = Color.Green;
            comboBox_Klimaregion.Text = GetProjektKlimaregion(m_ID_Projekt);
        }

        public string GetKlimaregion(int ID_Klimaregion)
        {
            RecordSet rs = new RecordSet();
            string szKlimaregion = "";
            rs.Open("select * from Tab_Klimaregion where ID_Klimaregion = " + ID_Klimaregion);
            if (rs.Next())
            {
                szKlimaregion = (string)rs.Read("Name");
            }
            rs.Close();
            return szKlimaregion;
        }

        public int GetKlimaregion(string szKlimaregion)
        {
            RecordSet rs = new RecordSet();
            int IDKlimaregion = 0;
            rs.Open("select * from Tab_Klimaregion where Name = '" + szKlimaregion + "'");
            if (rs.Next())
            {
                IDKlimaregion = (int)rs.Read("ID_Klimaregion");
            }
            rs.Close();
            return IDKlimaregion;
        }
        public string GetProjektKlimaregion(int ID_Projekt)
        {
            RecordSet rs = new RecordSet();
  
            string szKlimaregion = "";
            rs.Open("select * from Tab_Projekt where ID = " + ID_Projekt);
            if (rs.Next())
            {
                int id = (int)rs.Read("ID_Klimaregion");
                rs.Close();
                rs.Open("select * from Tab_Klimaregion where ID_Klimaregion = " + id);
                if (rs.Next())
                {
                    szKlimaregion = (string)rs.Read("Name");
                }
            }
 
            rs.Close();
            return szKlimaregion;
        }

        private void pBox_Bearbeiten_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektBearbeiten();
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
                "Z_Projekt_Stromverbraucher.ID_Stromverbraucher, Z_Projekt_Stromverbraucher.Summe, Tab_Stromverbraucher.Bezeichner " +
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
                item.m_Summe = (double)rs.Read("Summe");
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
            Form_WPAuswahl frm = new Form_WPAuswahl();
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

            frm.SetControls(m_ID_Projekt);
            frm.m_nType = id_type;
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
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 1) == 1)
                {
                    // --- DEIN BESTEHENDER CODE FÜR DAS RECHTECK ---
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;

                    // 1. Grüne Fläche zeichnen (wie gewohnt)
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }

                    // --- DEIN BESTEHENDER CODE FÜR DIE LABELS ---
                    label2_pBox_Heizkessel.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_Heizkessel.BackColor = label2_pBox_Heizkessel.BackColor;
                }
                else
                {
                    label2_pBox_Heizkessel.BackColor = Color.Transparent;
                    label_pBox_Heizkessel.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_WP_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

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

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }

                    label46.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label47.BackColor = label46.BackColor;
                }
                else
                {
                    label46.BackColor = Color.Transparent;
                    label47.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_Stromspeicher_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 162, 232)))
            {
                if ((status & 4) == 4)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label54.BackColor = Color.FromArgb(90, 0, 162, 232);
                    label55.BackColor = label54.BackColor;
                }
                else
                {
                    label54.BackColor = Color.Transparent;
                    label55.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_ProjektZuletzt_Click(object sender, EventArgs e)
        {
            ApplikationCtrl ctrl = new ApplikationCtrl();

            ctrl.ReadSingle("Select * from Tab_Applikation where ID=1");

            // falls zuletzt geöffnetes Projekt nicht gelöscht wurde
            if (ctrl.m_szProjektname != "")
            {
                m_szProjektname = ctrl.m_szProjektname;
                m_ID_Projekt = ctrl.m_ID_Projekt;
                SetTextProjekt(m_szProjektname);
                for (int i = 1; i < tabControl_Wizard.TabPages.Count; i++) tabControl_Wizard.TabPages[i].Enabled = true;
                label_ProjektStatus.Text = "✔";
                label_ProjektStatus.ForeColor = Color.Green;
                comboBox_Klimaregion.Text = GetProjektKlimaregion(m_ID_Projekt);
                UpdateWizardSymbole();
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

                Color bg = pBox_ProjektZuletzt.BackColor;
                label_pBox_ProjektZuletzt.BackColor = Color.FromArgb(90, 0, 255, 0);
                label2_pBox_ProjektZuletzt.BackColor = label_pBox_ProjektZuletzt.BackColor;
                label_pBox_ProjektZuletzt.Refresh();
                label2_pBox_ProjektZuletzt.Refresh();

                var t = Task.Run(async delegate
                {
                    await Task.Delay(200);
                    return 0;
                });
                t.Wait();
                pBox_ProjektZuletzt.Invalidate();
                label_pBox_ProjektZuletzt.BackColor = bg;
                label2_pBox_ProjektZuletzt.BackColor = label_pBox_ProjektZuletzt.BackColor;

            }

            Form_Hinweis frm = new Form_Hinweis(MyResource.Resource.Text_Hinweis, MyResource.Resource.Text_Projekt + " " + m_szProjektname + " " + MyResource.Resource.Text_Geoeffnet + "!");
            frm.Location = this.PointToScreen(tabControl_Wizard.PointToScreen(pBox_ProjektZuletzt.Location));
            frm.ShowDialog();
        }


        private void pBox_Gebaude_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

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

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_Gebaude.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_Gebaude.BackColor = label2_pBox_Gebaude.BackColor;
                }
                else
                {
                    label2_pBox_Gebaude.BackColor = Color.Transparent;
                    label_pBox_Gebaude.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_WBedarfDaten_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

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

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_WBedarfDaten.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_WBedarfDaten.BackColor = label2_pBox_WBedarfDaten.BackColor;
                }
                else
                {
                    label2_pBox_WBedarfDaten.BackColor = Color.Transparent;
                    label_pBox_WBedarfDaten.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_Prozess_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

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

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_Prozess.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_Prozess.BackColor = label2_pBox_Prozess.BackColor;
                }
                else
                {
                    label2_pBox_Prozess.BackColor = Color.Transparent;
                    label_pBox_Prozess.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_StdLastProfil_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

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

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_StdLastProfil.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_StdLastProfil.BackColor = label2_pBox_StdLastProfil.BackColor;
                }
                else
                {
                    label2_pBox_StdLastProfil.BackColor = Color.Transparent;
                    label_pBox_StdLastProfil.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_StromMessdaten_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken
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

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_StromMessdaten.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_StromMessdaten.BackColor = label2_pBox_StromMessdaten.BackColor;
                }
                else
                {
                    label2_pBox_StromMessdaten.BackColor = Color.Transparent;
                    label_pBox_StromMessdaten.BackColor = Color.Transparent;
                }
            }
        }

        private void tabPage5_Enter(object sender, EventArgs e)
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_ProjektOpen.Text + "'");
            
            label_Name.Text = textBox_ProjektOpen.Text;
            simulationStrombedarf.Berechnung(ctrl.m_ID);
            label_Strom.Text = simulationStrombedarf.Strombedarf_gesamt.ToString("F2") + " MWh/a";

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

        private void pBox_BHKW_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

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

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label52.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label53.BackColor = label52.BackColor;
                }
                else
                {
                    label52.BackColor = Color.Transparent;
                    label53.BackColor = Color.Transparent;
                }
            }
        }

        private void tabControl_Wizard_Selecting(object sender, TabControlCancelEventArgs e)
        {
            if (e.TabPageIndex < 0) return;

            if (e.TabPageIndex >= 1 && textBox_ProjektOpen.Text == MyResource.Resource.Text_Select)
            {
                e.Cancel = true;
                Form_Hinweis frm = new Form_Hinweis(MyResource.Resource.Text_Hinweis, 
                    "\r\n" + MyResource.Resource.Text_Form_Start_MessageBox1 + "\r\n" + MyResource.Resource.Text_Form_Start_MessageBox2);
                
                System.Drawing.Point p1 = tabControl_Wizard.Location;
                p1.X += tabControl_Wizard.Width / 2 - frm.Width / 2;
                frm.Location = p1;
                frm.ShowDialog();
            }
            else
            {
                //if(e.TabPageIndex >= 1 && e.TabPageIndex <= 3)
                if (!bUpdateWizardSymbole) { UpdateWizardSymbole(); bUpdateWizardSymbole = true; }
            }
        }

        private void pBox_Delete_Click(object sender, EventArgs e)
        {
            MenueCtrl menu = new MenueCtrl();
            menu.ProjektDelete();
        }

        private void pBoxSchnellSim_Click(object sender, EventArgs e)
        {
            Form_Simulation_Kurz frm = new Form_Simulation_Kurz(m_ID_Projekt);
            frm.SetControls();
            frm.ShowDialog();
        }

        private void btn_SimKonfig_Click(object sender, EventArgs e)
        {
            Form_Simulation_Config frm = new Form_Simulation_Config();
            KonfigurationCtrl ctrl = new KonfigurationCtrl();

            ctrl.ReadSingle("select * from Tab_Einstellungen where ID_Projekt=" + m_ID_Projekt);
            frm.Konfiguration = ctrl.model;
            frm.SetControls(m_ID_Projekt);
            System.Drawing.Point p1 = btn_SimKonfig.Location;
            p1 = tabControl_Wizard.PointToScreen(p1);
            p1.Y /= 2;
            p1.X /= 2;
            frm.Location = p1;
            frm.ShowDialog();
        }

        private void pBox_DetailSim_Click(object sender, EventArgs e)
        {
            Form_Simulation_Detail frm = new Form_Simulation_Detail(m_ID_Projekt);
            frm.simulation_Strombedarf = simulationStrombedarf;
            frm.simulation_Waermebedarf = simulationWaermebedarf;
  
            frm.SetControls();
            frm.ShowDialog();
        }

        private void pBox_Solarthermie_Click(object sender, EventArgs e)
        {
            Form_SolarKollektoren frm = new Form_SolarKollektoren();
            Form_Solarganglinie frm2 = new Form_Solarganglinie();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();
            WPCtrl wpctrl = new WPCtrl();
            Z_ProjektSolarganglinieCtrl solgctrl = new Z_ProjektSolarganglinieCtrl();
            RecordSet rs = new RecordSet();

            int id_type;

            System.Drawing.Point p1 = pBox_Solarthermie.Location;
            p1 = tabControl_Wizard.PointToScreen(p1);
            p1.Y /= 2;
            p1.X /= 2;
            
            if (radioButton_KollektorProfil.Checked)
            {
                frm.list_werzmodel.Clear();
                werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);
                id_type = WizardItemClass.SOLAR_TYP;

                WErzeugerModel item = new WErzeugerModel();
                for (int i = 0; i < werzctrl.rows; i++)
                {
                    frm.list_werzmodel.Add(werzctrl.items[i]);
                }

                frm.SetControls(m_ID_Projekt);
                frm.Location = p1;
                DialogResult result = frm.ShowDialog();

                if (result == DialogResult.OK)
                {
                    wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                    wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_werzmodel);

                    projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                    projctrl.m_Aenderungsdatum = DateTime.Now;
                    projctrl.Update();
                }
            }
            else
            {
                frm2.DateiListe.Clear();

                string sql = "SELECT Z_ProjektSolarganglinie.ID_Z, Z_ProjektSolarganglinie.ID_Projekt, " +
                      "Z_ProjektSolarganglinie.ID_Ganglinie, Tab_Solarganglinie.Bezeichner " +
                      "FROM Z_ProjektSolarganglinie INNER JOIN Tab_Solarganglinie ON " +
                      "Z_ProjektSolarganglinie.ID_Ganglinie = Tab_Solarganglinie.ID " +
                      " where ID_Projekt=" + m_ID_Projekt;

                rs.Open(sql);
                while (rs.Next())
                {
                    Z_ProjektSolarganglinieCtrl item = new Z_ProjektSolarganglinieCtrl();
                    item.m_ID_Z = (int)rs.Read("ID_Z");
                    item.m_ID_Projekt = m_ID_Projekt;
                    item.m_ID_Solarganglinie = (int)rs.Read("ID_Ganglinie");
                    item.m_szSolarganglinie = (string)rs.Read("Bezeichner");
                    frm2.DateiListe.Add(item);
                }
                rs.Close();

                frm2.m_ID_Projekt = m_ID_Projekt;
                frm2.SetControls(m_szProjektname);
                frm2.Location = p1;
                frm2.ShowDialog();

                if (frm2.result == DialogResult.OK)
                {
                    wizctrl.Del_Solarganglinie(m_ID_Projekt);
                    wizctrl.Add_Solarganglinie(m_ID_Projekt, frm2.DateiListe);

                    projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                    projctrl.m_Aenderungsdatum = DateTime.Now;
                    projctrl.Update();
                }
            }

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);
            solgctrl.ReadAll("select * from Z_ProjektSolarganglinie where ID_Projekt=" + m_ID_Projekt.ToString());

            if (werzctrl.rows > 0 || solgctrl.rows > 0) 
            {
                radioButton_Ganglinie.BackColor = Color.FromArgb(90, 0, 255, 0);
                radioButton_KollektorProfil.BackColor = Color.FromArgb(90, 0, 255, 0);
                status |= 512;
            }
            else
            {
                radioButton_Ganglinie.BackColor = Color.Transparent;
                radioButton_KollektorProfil.BackColor = Color.Transparent;
                status &= ~512;
            }
    
            pBox_Solarthermie.Invalidate();
        }

        private void pBox_Solarthermie_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 512) == 512)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label50.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label51.BackColor = label50.BackColor;
                }
                else
                {
                    label50.BackColor = Color.Transparent;
                    label51.BackColor = Color.Transparent;
                }
            }
        }

        private void pBox_PV_Click(object sender, EventArgs e)
        {
            Form_PV frm = new Form_PV();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_pvmodel.Clear();

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PV_TYP);
            id_type = WizardItemClass.PV_TYP;

            for (int i = 0; i < werzctrl.rows; i++)
            {
                WErzeugerModel item = new WErzeugerModel();
                item.ID = werzctrl.items[i].ID;
                item.ID_PV = werzctrl.items[i].ID_PV;
                item.ID_Type = werzctrl.items[i].ID_Type;
                item.Bezeichner = werzctrl.items[i].Bezeichner;
                item.PV_Leistung = werzctrl.items[i].PV_Leistung;
                item.m_Azimut = werzctrl.items[i].m_Azimut;
                item.m_Neigung = werzctrl.items[i].m_Neigung;
                frm.list_pvmodel.Add(item);
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
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_pvmodel);
                if (frm.list_pvmodel.Count > 0)
                    status |= 1024;
                else status &= ~1024;
                pBox_PV.Invalidate();

                ProjektCtrl projctrl = new ProjektCtrl();
                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }
        }

        private void pBox_PV_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 1024) == 1024)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label56.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label57.BackColor = label56.BackColor;
                }
                else
                {
                    label56.BackColor = Color.Transparent;
                    label57.BackColor = Color.Transparent;
                }

            }
        }

        private void pBox_ProjektDetails_Click(object sender, EventArgs e)
        {
            if (textBox_ProjektOpen.Text == MyResource.Resource.Text_Select)
            {
                Form_Hinweis frm = new Form_Hinweis(MyResource.Resource.Text_Hinweis, "\r\n" + MyResource.Resource.Text_Form_Start_MessageBox1 + "\r\n" + MyResource.Resource.Text_Form_Start_MessageBox2);
                System.Drawing.Point p1 = pBox_ProjektDetails.Location;
                p1 = this.PointToScreen(p1);
                frm.Location = p1;
                frm.ShowDialog();
                return;
            }

            MenueCtrl ctrl = new MenueCtrl();
            ctrl.ProjektOeffnen(true);
        }

        private void label47_Click(object sender, EventArgs e)
        {
            pBox_WP_Click(sender, e);
        }

        private void label46_Click(object sender, EventArgs e)
        {
            pBox_WP_Click(sender, e);
        }

        private void label51_Click(object sender, EventArgs e)
        {
            pBox_Solarthermie_Click(sender, e);
        }

        private void label50_Click(object sender, EventArgs e)
        {
            pBox_Solarthermie_Click(sender, e);
        }

        private void label61_Click(object sender, EventArgs e)
        {
            pBoxSchnellSim_Click(sender, e);
        }

        private void label60_Click(object sender, EventArgs e)
        {
            pBoxSchnellSim_Click(sender, e);
        }

        private void label63_Click(object sender, EventArgs e)
        {
            pBox_DetailSim_Click(sender, e);
        }

        private void label65_Click(object sender, EventArgs e)
        {
            pBox_Optimierung_Click(sender, e);
        }

        private void label64_Click(object sender, EventArgs e)
        {
            pBox_Optimierung_Click(sender, e);
        }

        private void pBox_Optimierung_Click(object sender, EventArgs e)
        {

        }

        private void label62_Click(object sender, EventArgs e)
        {
            pBox_DetailSim_Click(sender, e);
        }

        private void btn_Weiter_Click(object sender, EventArgs e)
        {
            if (tabControl_Wizard.SelectedIndex >= tabControl_Wizard.TabCount - 1) return;

            UpdateWizardSymbole();
            bUpdateWizardSymbole = true;

            tabControl_Wizard.SelectedIndex = tabControl_Wizard.SelectedIndex + 1;
        }

        private void UpdateWizardSymbole()
        {
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

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.SOLAR_TYP);
            Z_ProjektSolarganglinieCtrl solgctrl = new Z_ProjektSolarganglinieCtrl();
            solgctrl.ReadAll("select * from Z_ProjektSolarganglinie where ID_Projekt=" + m_ID_Projekt.ToString());

            if (werzctrl.rows > 0 || solgctrl.rows > 0)
            {
                status |= 512;
                radioButton_KollektorProfil.BackColor = Color.FromArgb(90, 0, 255, 0);
                radioButton_Ganglinie.BackColor = Color.FromArgb(90, 0, 255, 0);
            }
            else
            {
                status &= ~512;
                radioButton_KollektorProfil.BackColor = Color.Transparent;
                radioButton_Ganglinie.BackColor = Color.Transparent;
            }

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PV_TYP);
            if (werzctrl.rows > 0) status |= 1024; else status &= ~1024;

            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PUFFER_TYP);
            if (werzctrl.rows > 0) status |= 2048; else status &= ~2048;

            Z_ProjektBrauchwasserCtrl brauchwctrl = new Z_ProjektBrauchwasserCtrl();
            brauchwctrl.ReadAll("select * from Z_Projekt_Brauchwasser where ID_Projekt=" + m_ID_Projekt.ToString());
            if (brauchwctrl.rows > 0) status |= 4096; else status &= ~4096;
        }

        private void btn_Zurueck_Click(object sender, EventArgs e)
        {
            if (tabControl_Wizard.SelectedIndex <= 0) return;
            tabControl_Wizard.SelectedIndex = tabControl_Wizard.SelectedIndex - 1;
        }

        private void pBox_Pufferspeicher_Click(object sender, EventArgs e)
        {
            Form_PufferSp frm = new Form_PufferSp();
            WErzeugerCtrl werzctrl = new WErzeugerCtrl();
            WPCtrl wpctrl = new WPCtrl();
            int id_type;

            frm.list_pufferspmodel.Clear();
            werzctrl.ReadAllFilter("ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.PUFFER_TYP);
            id_type = WizardItemClass.PUFFER_TYP;

            WErzeugerModel item = new WErzeugerModel();
            for (int i = 0; i < werzctrl.rows; i++)
            {
                frm.list_pufferspmodel.Add(werzctrl.items[i]);
            }

            frm.SetControls(m_ID_Projekt);
            DialogResult result = frm.ShowDialog();

            if (result == DialogResult.OK)
            {
                WizardCtrl wizctrl = new WizardCtrl();
                wizctrl.Del_Projekt_Waermeerzeuger(m_ID_Projekt, id_type);
                wizctrl.Add_WP_Waermeerzeuger(m_ID_Projekt, frm.list_pufferspmodel);
            }

            if (frm.list_pufferspmodel.Count > 0)
                status |= 2048;
            else status &= ~2048;

            pBox_Pufferspeicher.Invalidate();
        }

        private void label55_Click(object sender, EventArgs e)
        {
            pBox_Stromspeicher_Click(sender, e);
        }

        private void label54_Click(object sender, EventArgs e)
        {
            pBox_Stromspeicher_Click(sender, e);
        }

        private void label72_Click(object sender, EventArgs e)
        {
            pBox_Pufferspeicher_Click(sender, e);
        }

        private void label71_Click(object sender, EventArgs e)
        {
            pBox_Pufferspeicher_Click(sender, e);
        }

        private void label57_Click(object sender, EventArgs e)
        {
            pBox_PV_Click(sender, e);
        }

        private void label56_Click(object sender, EventArgs e)
        {
            pBox_PV_Click(sender, e);
        }

        private void pBox_Pufferspeicher_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 162, 232)))
            {
                if ((status & 2048) == 2048)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label71.BackColor = Color.FromArgb(90, 0, 162, 232);
                    label72.BackColor = label71.BackColor;
                }
                else
                {
                    label71.BackColor = Color.Transparent;
                    label72.BackColor = Color.Transparent;
                }
            }

        }

        private void pBox_Brauchwasser_Click(object sender, EventArgs e)
        {
            Form_Brauchwasser frm = new Form_Brauchwasser();
            RecordSet rs = new RecordSet();
            WizardCtrl wizctrl = new WizardCtrl();
            ProjektCtrl projctrl = new ProjektCtrl();

            frm.list_pwmodel.Clear();

            string sql = "SELECT Z_Projekt_Brauchwasser.ID, Z_Projekt_Brauchwasser.ID_Projekt, " +
                "Z_Projekt_Brauchwasser.ID_Brauchwasser, Tab_Brauchwasser.Bezeichner, Z_Projekt_Brauchwasser.Summe " +
                "FROM Z_Projekt_Brauchwasser INNER JOIN Tab_Brauchwasser ON " +
                "Z_Projekt_Brauchwasser.ID_Brauchwasser = Tab_Brauchwasser.ID " +
                " where ID_Projekt=" + m_ID_Projekt;

            rs.Open(sql);
            while (rs.Next())
            {
                Z_ProjektBrauchwasserModel item = new Z_ProjektBrauchwasserModel();
                item.ID_Z = (int)rs.Read("ID");
                item.ID_Projekt = m_ID_Projekt;
                item.ID_Brauchwasser = (int)rs.Read("ID_Brauchwasser");
                item.szBezeichner = (string)rs.Read("Bezeichner");
                item.Summe = (double)rs.Read("Summe");
                frm.list_pwmodel.Add(item);
            }

            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls(m_szProjektname);
            frm.ShowDialog();

            if (frm.DialogResult == DialogResult.OK)
            {
                wizctrl.Del_Projekt_Brauchwasser(m_ID_Projekt);
                wizctrl.Add_Projekt_Brauchwasser(m_ID_Projekt, frm.list_pwmodel);

                projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
                projctrl.m_Aenderungsdatum = DateTime.Now;
                projctrl.Update();
            }

            if (frm.list_pwmodel.Count > 0)
                status |= 4096;
            else status &= ~4096;
            pBox_Brauchwasser.Invalidate();
        }

        private void label74_Click(object sender, EventArgs e)
        {
            pBox_Brauchwasser_Click(sender, e);
        }

        private void label73_Click(object sender, EventArgs e)
        {
            pBox_Brauchwasser_Click(sender, e);
        }

        private void pBox_Brauchwasser_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias; // Sorgt für glatte Kurven am Balken

            using (Brush brush = new SolidBrush(Color.FromArgb(90, 0, 255, 0)))
            {
                if ((status & 4096) == 4096)
                {
                    Rectangle rt = e.ClipRectangle;
                    rt.Width = rt.Width - 20;
                    rt.Height = rt.Height - 20;
                    rt.Y = rt.Y + 10;
                    rt.X = rt.X + 10;
                    Program.FillRoundedRectangle(e.Graphics, brush, rt, 10);

                    // --- NEU: DER BLAUE BALKEN LINKS ---
                    int barWidth = 7; // Breite des Balkens
                    int radius = 10;   // Gleicher Radius wie bei deiner FillRoundedRectangle

                    using (GraphicsPath path = new GraphicsPath())
                    {
                        // Wir definieren die Form des Balkens links bündig im Rechteck 'rt'
                        path.AddArc(rt.X, rt.Y, radius * 2, radius * 2, 180, 90); // Oben links
                        path.AddLine(rt.X + radius, rt.Y, rt.X + barWidth, rt.Y); // Kante oben
                        path.AddLine(rt.X + barWidth, rt.Y, rt.X + barWidth, rt.Bottom); // Gerade Kante rechts
                        path.AddLine(rt.X + barWidth, rt.Bottom, rt.X + radius, rt.Bottom); // Kante unten
                        path.AddArc(rt.X, rt.Bottom - radius * 2, radius * 2, radius * 2, 90, 90); // Unten links
                        path.CloseFigure();

                        using (Brush blueBrush = new SolidBrush(Color.FromArgb(255, 0, 150, 230)))
                        {
                            g.FillPath(blueBrush, path);
                        }
                    }
                    label2_pBox_Brauchwasser.BackColor = Color.FromArgb(90, 0, 255, 0);
                    label_pBox_Brauchwasser.BackColor = label2_pBox_Brauchwasser.BackColor;
                }
                else
                {
                    label2_pBox_Brauchwasser.BackColor = Color.Transparent;
                    label_pBox_Brauchwasser.BackColor = Color.Transparent;
                }
            }
        }

        private void ComboBox_Klimaregion()
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            if (m_szProjektname != "")
            {
                comboBox_Klimaregion.Text = m_szProjektname;
            }
            KlimaregionCtrl ctrl = new KlimaregionCtrl();
            ctrl.ReadAll();
            for (int i = 0; i < ctrl.rows; i++)
            {
                comboBox_Klimaregion.Items.Add(ctrl.items[i].m_szName);
            }
        }

        private void comboBox_Klimaregion_SelectedIndexChanged(object sender, EventArgs e)
        {
            ProjektCtrl ctrl_projekt = new ProjektCtrl();
            KlimaregionCtrl ctrl_klimaregion = new KlimaregionCtrl();

            this.ActiveControl = null;
            if (string.IsNullOrEmpty(m_szProjektname) || string.IsNullOrEmpty(comboBox_Klimaregion.Text)) return;
            
            ctrl_projekt.ReadSingle("Select * from Tab_Projekt where Projektname='" + m_szProjektname + "'");
            ctrl_projekt.m_ID_Klimaregion = GetKlimaregion(comboBox_Klimaregion.Text);
            ctrl_projekt.Update(); 
        }

        private void btn_Kosten_Click(object sender, EventArgs e)
        {
            using (var form = new Form_Kosten(m_ID_Projekt))
            {
                form.m_ID_Projekt = m_ID_Projekt;
                form.ShowDialog(); // Öffnet das Fenster als modaler Dialog
            }
        }

        private void InitEventDictionary()
        {
            _clickEvents = new Dictionary<string, Action<object, EventArgs>>
            {
                { "pBox_ProjektNeu", pBox_ProjektNeu_Click },
                { "label_pBox_ProjektNeu", pBox_ProjektNeu_Click },
                { "label2_pBox_ProjektNeu", pBox_ProjektNeu_Click },
                { "pBox_ProjektOeffnen", pBox_ProjektOeffnen_Click },
                { "label_pBox_ProjektOeffnen", pBox_ProjektOeffnen_Click },
                { "label2_pBox_ProjektOeffnen", pBox_ProjektOeffnen_Click },
                { "pBox_ProjektZuletzt", pBox_ProjektZuletzt_Click },
                { "label_pBox_ProjektZuletzt", pBox_ProjektZuletzt_Click },
                { "label2_pBox_ProjektZuletzt", pBox_ProjektZuletzt_Click },
                { "pBox_Bearbeiten", pBox_Bearbeiten_Click },
                { "label_pBox_Bearbeiten", pBox_Bearbeiten_Click },
                { "label2_pBox_Bearbeiten", pBox_Bearbeiten_Click },
                { "pBox_Delete", pBox_Delete_Click },
                { "label_pBox_Delete", pBox_Delete_Click },
                { "label2_pBox_Delete", pBox_Delete_Click },
                { "pBox_ProjektDetails", pBox_ProjektDetails_Click },
                { "label_pBox_ProjektDetails", pBox_ProjektDetails_Click },
                { "label2_pBox_ProjektDetails", pBox_ProjektDetails_Click },
                { "pBox_Gebaude", pBox_Gebaude_Click },
                { "label_pBox_Gebaude", pBox_Gebaude_Click },
                { "label2_pBox_Gebaude", pBox_Gebaude_Click },
                { "pBox_WBedarfDaten", pBox_WBedarfDaten_Click },
                { "label_pBox_WBedarfDaten", pBox_WBedarfDaten_Click },
                { "label2_pBox_WBedarfDaten", pBox_WBedarfDaten_Click },
                { "pBox_Prozess", pBox_Prozess_Click },
                { "label_pBox_Prozess", pBox_Prozess_Click },
                { "label2_pBox_Prozess", pBox_Prozess_Click },
                { "pBox_Brauchwasser", pBox_Brauchwasser_Click },
                { "label_pBox_Brauchwasser", pBox_Brauchwasser_Click },
                { "label2_pBox_Brauchwasser", pBox_Brauchwasser_Click },
                { "pBox_StdLastProfil", pBox_StdLastProfil_Click },
                { "label_pBox_StdLastProfil", pBox_StdLastProfil_Click },
                { "label2_pBox_StdLastProfil", pBox_StdLastProfil_Click },
                { "pBox_StromProfilEigenes", pBox_StromProfilEigenes_Click },
                { "label_pBox_StromProfilEigenes", pBox_StromProfilEigenes_Click },
                { "label2_pBox_StromProfilEigenes", pBox_StromProfilEigenes_Click },
                { "pBox_StromMessdaten", pBox_StromMessdaten_Click },
                { "label_pBox_StromMessdaten", pBox_StromMessdaten_Click },
                { "label2_pBox_StromMessdaten", pBox_StromMessdaten_Click },
                { "pBox_Heizkessel", pBox_Heizkessel_Click },
                { "label_pBox_Heizkessel", pBox_Heizkessel_Click },
                { "label2_pBox_Heizkessel", pBox_Heizkessel_Click },
            };
        }

        private void CentralControl_Click(object sender, EventArgs e)
        {
            Control ctrl = sender as Control;
            if (ctrl != null && _clickEvents.ContainsKey(ctrl.Name))
            {
                // Hier wird die im Dictionary hinterlegte Funktion direkt ausgeführt
                _clickEvents[ctrl.Name](sender, e);
            }
        }

        private void comboBox_Klimaregion_DropDownClosed(object sender, EventArgs e)
        {
            // Schiebt den Fokus auf das Parent-Element (z.B. das Panel oder die Form)
            this.ActiveControl = null;
        }
    }
}
