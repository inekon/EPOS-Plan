using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{

    public partial class FormMain : Form
    {
        private int m_ID_Projekt = 0;
        private string m_szProjektname = "";
        private int m_ID_Klimaregion = 0;
        private Control drag_control;

        // Lazy initialisiert – Field-Initializers wuerden im WinForms-Designer
        // die Simulation-Klassen instanziieren und am DB-/COM-Zugriff im
        // Konstruktor scheitern lassen.
        private SimulationWaermebedarf _simulation_Waermebedarf;
        SimulationWaermebedarf simulation_Waermebedarf
            => _simulation_Waermebedarf ??= new SimulationWaermebedarf();

        private SimulationStrombedarf _simulation_strom;
        SimulationStrombedarf simulation_strom
            => _simulation_strom ??= new SimulationStrombedarf();

        private SimulationWaermepumpe _simulation_wp;
        SimulationWaermepumpe simulation_wp
            => _simulation_wp ??= new SimulationWaermepumpe();

        ToolTip tt = new ToolTip();
  
        public bool Simulation_durchgeführt = false;

        public void SetProjekt(string szProjekt)
        {
            textBox_Projekt.Text = szProjekt;
            m_szProjektname = szProjekt;
        }

        public void SetIDProjekt(int IDProjekt)
        {
            m_ID_Projekt = IDProjekt;
        }

        public void SetKlima(string szKlima)
        {
            comboBox_Klima.Text = szKlima;
        }

        public void SetKunde(string szKunde)
        {
            textBox_Kunde.Text = szKunde;
        }

        public void SetBearbeiter(string szBearbeiter)
        {
            textBox_Bearbeiter.Text = szBearbeiter;
        }

        public void SetBeschreibung(string szBeschreibung)
        {
            textBox_Beschreibung.Text = szBeschreibung;
        }

        public void SetAenderungsdatum(DateTime datum)
        {
            textBox_Datum.Text = datum.ToString("d", CultureInfo.CreateSpecificCulture("de-DE"));
        }

        public FormMain()
        {
            InitializeComponent();
            FillKlimaList();

            tt.Draw += new DrawToolTipEventHandler(this.tt_Draw);
        }

        private void tt_Draw(object sender, DrawToolTipEventArgs e)
        {
            e.DrawBackground();
            e.DrawBorder();
            e.DrawText();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Maximized;
            Size clientsize = this.ClientSize;
   
            listView_WP.View = View.Details;
            listView_WP.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Vorlauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Rücklauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Betriebsart", -2, HorizontalAlignment.Left);
            //listView_WP.Columns.Add("", 0, HorizontalAlignment.Left);
            listView_WP.Width = tabControl_Komponenten.ClientSize.Width;
            listView_WP.Height = tabControl_Komponenten.ClientSize.Height;
            listView_WP.Top = -2;
            listView_WP.Left = -2;

            listView_SP.View = View.Details;
            listView_SP.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_SP.Columns.Add("Typ", -2, HorizontalAlignment.Left);
            listView_SP.Columns.Add("Leistung [kW]", -2, HorizontalAlignment.Left);
            listView_SP.Columns.Add("Energie", -2, HorizontalAlignment.Left);
            listView_SP.Columns.Add("Degradation", -2, HorizontalAlignment.Left);
            listView_SP.Columns.Add("Ladezustand", -2, HorizontalAlignment.Left);
            listView_SP.Width = tabControl_Komponenten.ClientSize.Width;
            listView_SP.Height = tabControl_Komponenten.ClientSize.Height;
            listView_SP.Top = -2;
            listView_SP.Left = -2;

            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            listView_Heizkessel.View = View.Details;
            listView_Heizkessel.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Heizkessel.Columns.Add("Typ", -2, HorizontalAlignment.Left);
            listView_Heizkessel.Columns.Add("Leistung [kW]", -2, HorizontalAlignment.Left);
            listView_Heizkessel.Columns.Add("Beschreibung", -2, HorizontalAlignment.Left);
            listView_Heizkessel.Width = tabControl_Komponenten.ClientSize.Width;
            listView_Heizkessel.Height = tabControl_Komponenten.ClientSize.Height;
            listView_Heizkessel.Left = -2;
            listView_Heizkessel.Top = -2;
            listView_Heizkessel.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Heizkessel.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            listView_Gebaeude.View = View.Details;
            listView_Gebaeude.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Gebaeude.Columns.Add("Größe", -2, HorizontalAlignment.Left);
            listView_Gebaeude.Columns.Add("Einheit", -2, HorizontalAlignment.Left);
            listView_Gebaeude.Columns.Add("Gebäudeart", -2, HorizontalAlignment.Left);
            listView_Gebaeude.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Gebaeude.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_Gebaeude.Width = tabControl_Komponenten.ClientSize.Width;
            listView_Gebaeude.Height = tabControl_Komponenten.ClientSize.Height;
            listView_Gebaeude.Top = -2;
            listView_Gebaeude.Left = -2;

            listView_WaermebedarfExtern.View = View.Details;
            listView_WaermebedarfExtern.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_WaermebedarfExtern.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WaermebedarfExtern.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_WaermebedarfExtern.Width = tabControl_Komponenten.ClientSize.Width;
            listView_WaermebedarfExtern.Height = tabControl_Komponenten.ClientSize.Height;
            listView_WaermebedarfExtern.Top = -2;
            listView_WaermebedarfExtern.Left = -2;

            listView_Prozesswaerme.View = View.Details;
            listView_Prozesswaerme.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Prozesswaerme.Columns.Add("Typ", -2, HorizontalAlignment.Left);
            listView_Prozesswaerme.Columns.Add("Beschreibung", -2, HorizontalAlignment.Left);
            listView_Prozesswaerme.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Prozesswaerme.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_Prozesswaerme.Width = tabControl_Komponenten.ClientSize.Width;
            listView_Prozesswaerme.Height = tabControl_Komponenten.ClientSize.Height;
            listView_Prozesswaerme.Top = -2;
            listView_Prozesswaerme.Left = -2;

            listView_Strombedarf.View = View.Details;
            listView_Strombedarf.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Strombedarf.Columns.Add("Typ", -2, HorizontalAlignment.Left);
            listView_Strombedarf.Columns.Add("Beschreibung", -2, HorizontalAlignment.Left);
            listView_Strombedarf.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Strombedarf.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_Strombedarf.Width = tabControl_Komponenten.ClientSize.Width;
            listView_Strombedarf.Height = tabControl_Komponenten.ClientSize.Height;
            listView_Strombedarf.Top = -2;
            listView_Strombedarf.Left = -2;

            listView_Stromganglinie.View = View.Details;
            listView_Stromganglinie.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Stromganglinie.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Stromganglinie.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_Stromganglinie.Width = tabControl_Komponenten.ClientSize.Width;
            listView_Stromganglinie.Height = tabControl_Komponenten.ClientSize.Height;
            listView_Stromganglinie.Top = -2;
            listView_Stromganglinie.Left = -2;

            listView_BHKW.View = View.Details;
            listView_BHKW.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_BHKW.Columns.Add("Firma", -2, HorizontalAlignment.Left);
            listView_BHKW.Columns.Add("Ptherm", -2, HorizontalAlignment.Left);
            listView_BHKW.Columns.Add("Pel", -2, HorizontalAlignment.Left);
            listView_BHKW.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_BHKW.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_BHKW.Width = tabControl_Komponenten.ClientSize.Width;
            listView_BHKW.Height = tabControl_Komponenten.ClientSize.Height;
            listView_BHKW.Top = -2;
            listView_BHKW.Left = -2;

            listView_Solar.View = View.Details;
            listView_Solar.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Solar.Columns.Add("Hersteller", -2, HorizontalAlignment.Left);
            listView_Solar.Columns.Add("Typ", -2, HorizontalAlignment.Left);
            listView_Solar.Columns.Add("Kollektofläche [m²]", -2, HorizontalAlignment.Left);
            listView_Solar.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Solar.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_Solar.Width = tabControl_Komponenten.ClientSize.Width;
            listView_Solar.Height = tabControl_Komponenten.ClientSize.Height;
            listView_Solar.Top = -2;
            listView_Solar.Left = -2;

            listView_PV.View = View.Details;
            listView_PV.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_PV.Columns.Add("Hersteller", -2, HorizontalAlignment.Left);
            listView_PV.Columns.Add("Leistung Anlage [W]", -2, HorizontalAlignment.Left);
            listView_PV.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_PV.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_PV.Width = tabControl_Komponenten.ClientSize.Width;
            listView_PV.Height = tabControl_Komponenten.ClientSize.Height;
            listView_PV.Top = -2;
            listView_PV.Left = -2;

            listView_Pufferspeicher.View = View.Details;
            listView_Pufferspeicher.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Pufferspeicher.Columns.Add("Hersteller", -2, HorizontalAlignment.Left);
            listView_Pufferspeicher.Columns.Add("Speichertyp", -2, HorizontalAlignment.Left);
            listView_Pufferspeicher.Columns.Add("Volumen", -2, HorizontalAlignment.Left);
            listView_Pufferspeicher.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Pufferspeicher.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_Pufferspeicher.Width = tabControl_Komponenten.ClientSize.Width;
            listView_Pufferspeicher.Height = tabControl_Komponenten.ClientSize.Height;
            listView_Pufferspeicher.Top = -2;
            listView_Pufferspeicher.Left = -2;
        }

        private void button_Beenden_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void SetWPControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            WPCtrl wpctrl = new WPCtrl();

            projctrl.ReadSingle(textBox_Projekt.Text);
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.WP_TYP + " or ID_Type=" + WizardItemClass.REF_WP_TYP + ")");

            listView_WP.Items.Clear();

            while (rs.Next())
            {
                ListViewItem lvitem = new ListViewItem();

                lvitem.Text = (string)rs.Read("Bezeichner");
                lvitem.SubItems.Add(rs.Read("Vorlauf").ToString());
                lvitem.SubItems.Add(rs.Read("Rücklauf").ToString());
                lvitem.SubItems.Add((string)rs.Read("Betriebsart"));
                lvitem.SubItems.Add(rs.Read("ID").ToString());
                if ((int)rs.Read("ID_Type") == WizardItemClass.WP_TYP)
                {
                    listView_WP.Items.Add(lvitem);
                }
            }
            rs.Close();

            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void SetSPControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            WPCtrl wpctrl = new WPCtrl();

            projctrl.ReadSingle(textBox_Projekt.Text);
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.SP_TYP + " or ID_Type=" + WizardItemClass.REF_SP_TYP + ")");
            listView_SP.Items.Clear();

            while (rs.Next())
            {
                ListViewItem lvitem = new ListViewItem();
                RecordSet rs_sp = new RecordSet();
                rs_sp.Open("select * from Tab_Stromspeicher where ID=" + rs.Read("ID_SP"));

                if (!rs_sp.EOF())
                {
                    lvitem.Text = (string)rs_sp.Read("Bezeichner");
                    lvitem.SubItems.Add(rs_sp.Read("Typ").ToString());
                    lvitem.SubItems.Add(rs_sp.Read("Leistung").ToString());
                    lvitem.SubItems.Add(rs_sp.Read("Energie").ToString());
                    lvitem.SubItems.Add(rs_sp.Read("Degradation").ToString());
                    lvitem.SubItems.Add(rs_sp.Read("Ladezustand").ToString());
                    lvitem.SubItems.Add(rs.Read("ID").ToString());
                    if ((int)rs.Read("ID_Type") == WizardItemClass.SP_TYP)
                    {
                        listView_SP.Items.Add(lvitem);
                    }
                }
                rs_sp.Close();
            }
            rs.Close();

            listView_SP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void listView_WP_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            ListView.SelectedIndexCollection indexes = listView_WP.SelectedIndices;

            if (indexes.Count > 0)
            {
                ListViewItem lvitem = listView_WP.Items[indexes[0]];
                Wizard_WPItem frm_wpitem = new Wizard_WPItem(lvitem.Text);
                WErzeugerCtrl ctrl = new WErzeugerCtrl();
                List<WErzeugerModel> list = new List<WErzeugerModel>();
                WPCtrl wpctrl = new WPCtrl();

                ctrl.ReadAllFilter("Bezeichner='" + lvitem.Text + "'");
                wpctrl.ReadAll("ID_WP=" + ctrl.items[0].ID_WP);
                ctrl.items[0].Regelung = wpctrl.items[0].Regelung;
                ctrl.items[0].Nennleistung = wpctrl.items[0].Nennleistung;
                ctrl.items[0].Modulkosten = wpctrl.items[0].Modulkosten;
                ctrl.items[0].Baujahr = wpctrl.items[0].Baujahr;
                ctrl.items[0].Beschreibung = wpctrl.items[0].Beschreibung;
                ctrl.items[0].Firma = wpctrl.items[0].Firma;
                ctrl.items[0].Typ = wpctrl.items[0].Typ;

                list.Add(ctrl.items[0]);
                frm_wpitem.m_werzitemlist = list;
                frm_wpitem.SetControls(0);
                frm_wpitem.ShowDialog();
            }
        }

        public void Add_WPKontext()
        {
            WPKontextMenuCtrl ctrl = new WPKontextMenuCtrl();
            ctrl.Init(listView_WP, m_ID_Projekt, m_szProjektname);
        }

        public void Add_BHKWKontext()
        {
            WPKontextMenuCtrl ctrl = new WPKontextMenuCtrl();
            ctrl.Init(listView_WP, m_ID_Projekt, m_szProjektname);
        }

        public void Add_GebäudeKontext()
        {
            GebäudeKontextMenuCtrl ctrl = new GebäudeKontextMenuCtrl();
            ctrl.Init(listView_Gebaeude, m_ID_Projekt, m_szProjektname);
        }

        public void Add_SpKontext()
        {
            SpKontextMenuCtrl ctrl = new SpKontextMenuCtrl();
            ctrl.Init(listView_SP, m_ID_Projekt, m_szProjektname);
        }

        public void Add_HeizkesselKontext()
        {
            HeizkesselKontextMenuCtrl ctrl = new HeizkesselKontextMenuCtrl();
            ctrl.Init(listView_Heizkessel, m_ID_Projekt, m_szProjektname);
        }

        public void Add_WaermebedarfExternKontext()
        {
            WaermebedarfExternKontextMenuCtrl ctrl = new WaermebedarfExternKontextMenuCtrl();
            ctrl.Init(listView_WaermebedarfExtern, m_ID_Projekt, m_szProjektname);
        }

        public void Add_ProzesswaermeKontext()
        {
            ProzesswaermeKontextMenuCtrl ctrl = new ProzesswaermeKontextMenuCtrl();
            ctrl.Init(listView_Prozesswaerme, m_ID_Projekt, m_szProjektname);
        }

        public void Add_StrombedarfKontext()
        {
            StrombedarfKontextMenuCtrl ctrl = new StrombedarfKontextMenuCtrl();
            ctrl.Init(listView_Strombedarf, m_ID_Projekt, m_szProjektname);
        }

        public void Add_StromganglinieKontext()
        {
            StromganglinieKontextMenuCtrl ctrl = new StromganglinieKontextMenuCtrl();
            ctrl.Init(listView_Stromganglinie, m_ID_Projekt, m_szProjektname);
        }

        public void Add_PVKontext()
        {
            PVKontextMenuCtrl ctrl = new PVKontextMenuCtrl();
            ctrl.Init(listView_PV, m_ID_Projekt, m_szProjektname);
        }

        public void Add_SolarKontext()
        {
            SolarKontextMenuCtrl ctrl = new SolarKontextMenuCtrl();
            ctrl.Init(listView_Solar, m_ID_Projekt, m_szProjektname);
        }

        public void SetHeizkesselControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();
            HeizkesselCtrl heizkesselctrl = new HeizkesselCtrl();

            projctrl.ReadSingle(textBox_Projekt.Text);
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.REF_KESSEL_TYP + " or ID_Type=" + WizardItemClass.KESSEL_TYP + ")");

            listView_Heizkessel.Items.Clear();

            while (rs.Next())
            {
                ListViewItem lvitem = new ListViewItem();
                RecordSet rs_hk = new RecordSet();

                rs_hk.Open("select * from [Tab_Heizkessel] where ID=" + rs.Read("ID_Kessel"));

                if (!rs_hk.EOF())
                {
                    lvitem.Text = (string)rs_hk.Read("Bezeichner");
                    lvitem.SubItems.Add(heizkesselctrl.Brennstoffart[(int)rs_hk.Read("Brennstoff")]);
                    double kl = (double)rs_hk.Read("Ptherm");
                    lvitem.SubItems.Add(kl.ToString("F2"));
                    lvitem.SubItems.Add(rs_hk.Read("Beschreibung").ToString());
                    lvitem.SubItems.Add(rs.Read("ID").ToString());
                    if ((int)rs.Read("ID_Type") == WizardItemClass.KESSEL_TYP)
                    {
                        listView_Heizkessel.Items.Add(lvitem);
                    }
                }
                rs_hk.Close();
            }
            rs.Close();
            listView_Heizkessel.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Heizkessel.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void SetGebaeudeControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();

            projctrl.ReadSingle(textBox_Projekt.Text);


            string sql = "SELECT Z_ProjektGebaeude.ID, Z_ProjektGebaeude.ID_Projekt, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, " +
             "[Tab_Gebaeude].Gebaeudename, Z_ProjektGebaeude.Einheit_Waermebedarf_Wohnflaeche, [Tab_Gebaeude].Gebaeudeart " +
             "FROM [Tab_Gebaeude] INNER JOIN Z_ProjektGebaeude ON [Tab_Gebaeude].ID_ProjektGebaeude = Z_ProjektGebaeude.ID" +
             " where Z_ProjektGebaeude.ID_Projekt=" + projctrl.m_ID;

            RecordSet rs = new RecordSet();
            rs.Open(sql);
            listView_Gebaeude.Items.Clear();

            while (rs.Next())
            {
                ListViewItem lvitem = new ListViewItem();
                {
                    double nWohnflaeche = (double)rs.Read("Wohnflaeche_Waermebedarf");
                    lvitem.Text = (string)rs.Read("Gebaeudename");
                    lvitem.SubItems.Add(nWohnflaeche.ToString("F2"));
                    lvitem.SubItems.Add(rs.Read("Einheit_Waermebedarf_Wohnflaeche").ToString());
                    lvitem.SubItems.Add(rs.Read("Gebaeudeart").ToString());
                    lvitem.SubItems.Add(rs.Read("ID").ToString());
                    listView_Gebaeude.Items.Add(lvitem);
                }
            }
            rs.Close();

            listView_Gebaeude.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Gebaeude.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void SetWaermebedarfExternControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();

            projctrl.ReadSingle(textBox_Projekt.Text);

            listView_WaermebedarfExtern.Items.Clear();

            string sql = "SELECT Z_ProjektWaermebedarf.ID_Z, Z_ProjektWaermebedarf.ID_Projekt, " +
               "Z_ProjektWaermebedarf.ID_Ganglinie, Tab_Waermebedarf.Bezeichner " +
               "FROM Z_ProjektWaermebedarf INNER JOIN Tab_Waermebedarf ON " +
               "Z_ProjektWaermebedarf.ID_Ganglinie = Tab_Waermebedarf.ID " +
               " where ID_Projekt=" + projctrl.m_ID;

            rs.Open(sql);
            while (rs.Next())
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = (string)rs.Read("Bezeichner");
                lvitem.SubItems.Add(rs.Read("ID_Z").ToString());
                lvitem.SubItems.Add(rs.Read("ID_Ganglinie").ToString());
                listView_WaermebedarfExtern.Items.Add(lvitem);
            }
            rs.Close();

            listView_WaermebedarfExtern.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WaermebedarfExtern.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void SetProzesswaermeControl(int m_ID_Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            Z_ProjektProzesswaermeCtrl ctrl = new Z_ProjektProzesswaermeCtrl();
            BrauchwasserCtrl pwctrl = new BrauchwasserCtrl();

            ctrl.ReadAll("select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + m_ID_Projekt);

            listView_Prozesswaerme.Items.Clear();

            for (int i = 0; i < ctrl.rows; i++)
            {
                pwctrl.ReadSingle(ctrl.items[i].ID_Prozesswaerme);
                for (int j = 0; j < pwctrl.rows; j++)
                {
                    ListViewItem lvitem = new ListViewItem();
                    lvitem.Text = pwctrl.m_szBezeichner;
                    lvitem.SubItems.Add(pwctrl.m_szTyp);
                    lvitem.SubItems.Add(pwctrl.m_szBeschreibung);
                    lvitem.SubItems.Add(ctrl.items[i].ID_Z.ToString());
                    listView_Prozesswaerme.Items.Add(lvitem);
                }
            }

            listView_Prozesswaerme.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Prozesswaerme.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void FillKlimaList()
        {
            KlimaregionCtrl ctrl = new KlimaregionCtrl();
            ctrl.ReadAll();
            // ctrl.FillListBox(listBox_Klima);

            comboBox_Klima.Items.Clear();
            for (int i = 0; i < ctrl.rows; i++)
            {
                comboBox_Klima.Items.Add(ctrl.items[i].m_szName);
            }
        }

        private void comboBox_Klima_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_ID_Klimaregion = GetIDKlimaregion();
        }

        private int GetIDKlimaregion()
        {
            int ID_Klimaregion = 0;
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Klimaregion where Name = '" + comboBox_Klima.Text + "'");
            if (rs.Next())
            {
                ID_Klimaregion = (int)rs.Read("ID_Klimaregion");
            }
            rs.Close();
            return ID_Klimaregion;
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

        private void listView_Gebaeude_DoubleClick(object sender, EventArgs e)
        {
            GebäudeKontextMenuCtrl ctrl = new GebäudeKontextMenuCtrl();
            ctrl.Init(listView_Gebaeude, m_ID_Projekt, m_szProjektname);
            ctrl.contextMenuStrip1.Items[0].PerformClick();
        }

        private void listView_WaermebedatfExtern_DoubleClick(object sender, EventArgs e)
        {
            WaermebedarfExternKontextMenuCtrl ctrl = new WaermebedarfExternKontextMenuCtrl();
            ctrl.Init(listView_WaermebedarfExtern, m_ID_Projekt, m_szProjektname);
            ctrl.contextMenuStrip1.Items[0].PerformClick();
        }

        private void listView_SP_DoubleClick(object sender, EventArgs e)
        {
            SpKontextMenuCtrl ctrl = new SpKontextMenuCtrl();
            ctrl.Init(listView_SP, m_ID_Projekt, m_szProjektname);
            ctrl.contextMenuStrip1.Items[0].PerformClick();
        }

        private void listView_Heizkessel_DoubleClick(object sender, EventArgs e)
        {
            HeizkesselKontextMenuCtrl ctrl = new HeizkesselKontextMenuCtrl();
            ctrl.Init(listView_Heizkessel, m_ID_Projekt, m_szProjektname);
            ctrl.contextMenuStrip1.Items[0].PerformClick();
        }

        private void listView_Prozesswaerme_DoubleClick(object sender, EventArgs e)
        {
            ProzesswaermeKontextMenuCtrl ctrl = new ProzesswaermeKontextMenuCtrl();
            ctrl.Init(listView_Prozesswaerme, m_ID_Projekt, m_szProjektname);
            ctrl.contextMenuStrip1.Items[0].PerformClick();
        }

        private void button1_DragDrop(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
            this.Cursor = Cursors.Default;

            if ((ListView)drag_control == listView_Gebaeude)
            {
                GebäudeKontextMenuCtrl ctrl = new GebäudeKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
            else if ((ListView)drag_control == listView_WaermebedarfExtern)
            {
                WaermebedarfExternKontextMenuCtrl ctrl = new WaermebedarfExternKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
            else if ((ListView)drag_control == listView_SP)
            {
                SpKontextMenuCtrl ctrl = new SpKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
            else if ((ListView)drag_control == listView_Heizkessel)
            {
                HeizkesselKontextMenuCtrl ctrl = new HeizkesselKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
            else if ((ListView)drag_control == listView_Prozesswaerme)
            {
                ProzesswaermeKontextMenuCtrl ctrl = new ProzesswaermeKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
            else if ((ListView)drag_control == listView_Strombedarf)
            {
                StrombedarfKontextMenuCtrl ctrl = new StrombedarfKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
            else if ((ListView)drag_control == listView_Stromganglinie)
            {
                StromganglinieKontextMenuCtrl ctrl = new StromganglinieKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
            else if ((ListView)drag_control == listView_WP)
            {
                WPKontextMenuCtrl ctrl = new WPKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[2].PerformClick();
            }
            else if ((ListView)drag_control == listView_BHKW)
            {
                BHKWKontextMenuCtrl ctrl = new BHKWKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
            else if ((ListView)drag_control == listView_PV)
            {
                PVKontextMenuCtrl ctrl = new PVKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
            else if ((ListView)drag_control == listView_Solar)
            {
                SolarKontextMenuCtrl ctrl = new SolarKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
            else if ((ListView)drag_control == listView_Pufferspeicher)
            {
                PufferSpKontextMenuCtrl ctrl = new PufferSpKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[0].PerformClick();
            }
        }

        private void button1_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void button1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void listView_Gebaeude_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_Gebaeude;
                ListViewItem lvi = listView_Gebaeude.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_Gebaeude.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_Gebaeude_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void listView_Gebaeude_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_Gebaeude.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_WaermebedarfExtern_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_WaermebedarfExtern;
                ListViewItem lvi = listView_WaermebedarfExtern.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_WaermebedarfExtern.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_WaermebedarfExtern_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void listView_WaermebedarfExtern_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_WaermebedarfExtern.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_SP_REF_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void listView_SP_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_SP;
                ListViewItem lvi = listView_SP.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_SP.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_SP_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void listView_SP_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_SP.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_Heizkessel_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_Heizkessel;
                ListViewItem lvi = listView_Heizkessel.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_Heizkessel.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_Heizkessel_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void listView_Heizkessel_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_Heizkessel.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_Prozesswaerme_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_Prozesswaerme;
                ListViewItem lvi = listView_Prozesswaerme.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_Prozesswaerme.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_Prozesswaerme_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;

        }

        private void listView_Prozesswaerme_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_Prozesswaerme.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void btn_DragDestination_MouseHover(object sender, EventArgs e)
        {
            tt.OwnerDraw = true;
            tt.BackColor = Color.LightYellow;
            tt.ForeColor = Color.Black;
            tt.Show("Drag&Drop aus Listen", btn_DragDestination, 0, 0, 1000);
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            ProjektCtrl ctrl = new ProjektCtrl();
            ctrl.m_Aenderungsdatum = DateTime.Now;
            ctrl.m_szBearbeiter = textBox_Bearbeiter.Text;
            ctrl.m_szBeschreibung = textBox_Beschreibung.Text;
            ctrl.m_szKunde = textBox_Kunde.Text;
            ctrl.m_szProjektname = m_szProjektname;
            ctrl.m_ID_Klimaregion = m_ID_Klimaregion;
            ctrl.Update();
        }

        private void btn_StromSimulSpeichern_Click(object sender, EventArgs e)
        {
            simulation_strom.SimulationErgebis_in_DB();
        }

        public void SetStrombedarfControl(int m_ID_Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            Z_ProjektStromverbraucherCtrl ctrl = new Z_ProjektStromverbraucherCtrl();
            StromverbraucherCtrl svctrl = new StromverbraucherCtrl();

            ctrl.ReadAll("select * from Z_Projekt_Stromverbraucher where ID_Projekt=" + m_ID_Projekt);

            listView_Strombedarf.Items.Clear();

            for (int i = 0; i < ctrl.rows; i++)
            {
                svctrl.ReadSingle(ctrl.items[i].m_ID_Stromverbraucher);
                for (int j = 0; j < svctrl.rows; j++)
                {
                    ListViewItem lvitem = new ListViewItem();
                    lvitem.Text = svctrl.m_szBezeichner;
                    lvitem.SubItems.Add(svctrl.m_szTyp);
                    lvitem.SubItems.Add(svctrl.m_szBeschreibung);
                    lvitem.SubItems.Add(ctrl.items[i].m_ID_Z.ToString());
                    listView_Strombedarf.Items.Add(lvitem);
                }
            }

            listView_Strombedarf.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Strombedarf.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void listView_Strombedarf_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_Strombedarf;
                ListViewItem lvi = listView_Strombedarf.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_Strombedarf.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_Strombedarf_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_Strombedarf.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_Strombedarf_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        public void SetStromganglinieControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();

            projctrl.ReadSingle(textBox_Projekt.Text);

            listView_Stromganglinie.Items.Clear();

            string sql = "SELECT Z_ProjektStromganglinie.ID_Z, Z_ProjektStromganglinie.ID_Projekt, " +
               "Z_ProjektStromganglinie.ID_Ganglinie, Tab_Stromganglinie.Bezeichner " +
               "FROM Z_ProjektStromganglinie INNER JOIN Tab_Stromganglinie ON " +
               "Z_ProjektStromganglinie.ID_Ganglinie = Tab_Stromganglinie.ID " +
               " where ID_Projekt=" + projctrl.m_ID;

            rs.Open(sql);
            while (rs.Next())
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = (string)rs.Read("Bezeichner");
                lvitem.SubItems.Add(rs.Read("ID_Z").ToString());
                lvitem.SubItems.Add(rs.Read("ID_Ganglinie").ToString());
                listView_Stromganglinie.Items.Add(lvitem);
            }
            rs.Close();

            listView_Stromganglinie.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Stromganglinie.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void listView_Stromganglinie_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_Stromganglinie;
                ListViewItem lvi = listView_Stromganglinie.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_Stromganglinie.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_Stromganglinie_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_Stromganglinie.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_Stromganglinie_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void listView_WP_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_WP;
                ListViewItem lvi = listView_WP.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_WP.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_WP_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_WP.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_WP_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form_StromTest frm = new Form_StromTest();
            frm.SetControls(m_ID_Projekt);
            frm.ShowDialog();
        }

        public void SetBHKWControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            BHKWCtrl bhkwctrl = new BHKWCtrl();

            projctrl.ReadSingle(textBox_Projekt.Text);
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.BHKW_TYP + ")");

            listView_BHKW.Items.Clear();

            while (rs.Next())
            {
                bhkwctrl.ReadSingle((int)rs.Read("ID_BHKW"));
                ListViewItem lvitem = new ListViewItem();

                lvitem.Text = (string)rs.Read("Bezeichner");
                lvitem.SubItems.Add(bhkwctrl.m_szFirma.ToString());
                lvitem.SubItems.Add(bhkwctrl.m_Ptherm.ToString());
                lvitem.SubItems.Add(bhkwctrl.m_Pel.ToString());
                lvitem.SubItems.Add(rs.Read("ID").ToString());
                listView_BHKW.Items.Add(lvitem);
            }
            rs.Close();

            listView_BHKW.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_BHKW.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void listView_BHKW_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_BHKW;
                ListViewItem lvi = listView_BHKW.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_BHKW.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_BHKW_MouseMove(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void listView_BHKW_MouseUp(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_BHKW.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        public void SetPVControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            PhotovoltaikCtrl ctrl = new PhotovoltaikCtrl();

            projctrl.ReadSingle(textBox_Projekt.Text);
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.PV_TYP + ")");

            listView_PV.Items.Clear();

            while (rs.Next())
            {
                ctrl.ReadSingle((int)rs.Read("ID_PV"));
                ListViewItem lvitem = new ListViewItem();

                lvitem.Text = (string)rs.Read("Bezeichner");
                lvitem.SubItems.Add(ctrl.m_szFirma.ToString());
                lvitem.SubItems.Add(((double)rs.Read("PV_Leistung")).ToString() );
                lvitem.SubItems.Add(rs.Read("ID").ToString());
                listView_PV.Items.Add(lvitem);
            }
            rs.Close();

            listView_PV.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_PV.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void SetPufferSpControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            PufferSpCtrl ctrl = new PufferSpCtrl(); 
                  
            projctrl.ReadSingle(textBox_Projekt.Text);
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.PUFFER_TYP + ")");

            listView_Pufferspeicher.Items.Clear();

            while (rs.Next())
            {
                ctrl.ReadAll("ID=" + (int)rs.Read("ID_PUFFER"));
                ListViewItem lvitem = new ListViewItem();

                if (ctrl.rows > 0)
                {
                    lvitem.Text = (string)rs.Read("Bezeichner");
                    lvitem.SubItems.Add(ctrl.items[0].Firma);
                    lvitem.SubItems.Add(ctrl.items[0].Speichertyp);
                    lvitem.SubItems.Add(ctrl.items[0].Gesamtvolumen.ToString());
                    lvitem.SubItems.Add(rs.Read("ID").ToString());
                    listView_Pufferspeicher.Items.Add(lvitem);
                }
            }
            rs.Close();

            listView_Pufferspeicher.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Pufferspeicher.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void SetSolarControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            SolarkollektorenCtrl ctrl = new SolarkollektorenCtrl();

            projctrl.ReadSingle(textBox_Projekt.Text);
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.SOLAR_TYP + ")");

            listView_Solar.Items.Clear();

            while (rs.Next())
            {
                ctrl.ReadSingle((int)rs.Read("ID_SOLAR"));
                ListViewItem lvitem = new ListViewItem();

                lvitem.Text = (string)rs.Read("Bezeichner");
                lvitem.SubItems.Add(ctrl.m_szFirma.ToString());
                lvitem.SubItems.Add(ctrl.m_szKollektortyp.ToString());
                lvitem.SubItems.Add((ctrl.m_Modulfläche * (int)rs.Read("Kollektormodulanzahl")).ToString());
                lvitem.SubItems.Add(rs.Read("ID").ToString());
                listView_Solar.Items.Add(lvitem);
            }
            rs.Close();

            listView_Solar.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Solar.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        private void listView_Solar_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_Solar;
                ListViewItem lvi = listView_Solar.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_Solar.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_Solar_MouseMove(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void listView_Solar_MouseUp(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_Solar.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_PV_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_PV;
                ListViewItem lvi = listView_PV.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_PV.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_PV_MouseMove(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void listView_PV_MouseUp(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_PV.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_Pufferspeicher_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_Pufferspeicher;
                ListViewItem lvi = listView_Pufferspeicher.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_Pufferspeicher.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_Pufferspeicher_MouseMove(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void listView_Pufferspeicher_MouseUp(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_Pufferspeicher.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }
    }
}