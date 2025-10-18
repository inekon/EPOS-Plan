using MathNet.Numerics.LinearAlgebra.Factorization;
using Microsoft.Office.Interop.Excel;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Odbc;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Net.Mime.MediaTypeNames;
using System.Diagnostics;
using Series = System.Windows.Forms.DataVisualization.Charting.Series;
using WindowsFormsApplication1.Classes.Simulation;

namespace WindowsFormsApplication1
{

    public partial class FormMain : Form
    {

        private int m_ID_Projekt = 0;
        private string m_szProjektname = "";
        private int m_ID_Klimaregion = 0;
        private Control drag_control;

        SimulationWaermebedarf simulation_Waermebedarf = new SimulationWaermebedarf();
        SimulationStrombedarf simulation_strom = new SimulationStrombedarf();
        SimulationWaermepumpe simulation_wp = new SimulationWaermepumpe();

        ToolTip tt = new ToolTip();
        double ChartSelBegin;
        double ChartSelEnd;

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

        public void SetNetzverluste(int nVerluste, string Einheit)
        {
            textBox_Netzverluste.Text = nVerluste.ToString();
            comboBox_NetzvEinheit.Text = Einheit;
        }

        public FormMain()
        {
            InitializeComponent();
            FillKlimaList();
            SimulationWaermebedarf.Ergebnis result;
            result = simulation_Waermebedarf.SimulationErgebis_aus_DB();

            textBox_MaxWaermelast.Text = result.Waermebedarf_Max.ToString("F2");
            textBox_Gesamt_Waermebedarf.Text = result.Gesamt_Waermebedarf.ToString("F2");

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
            button_Beenden.Top = ClientSize.Height - button_Beenden.Height;
            button_Beenden.Left = ClientSize.Width - button_Beenden.Width;

            listView_WP.View = View.Details;
            listView_WP.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Vorlauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Rücklauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP.Columns.Add("Betriebsart", -2, HorizontalAlignment.Left);
            //listView_WP.Columns.Add("", 0, HorizontalAlignment.Left);
            listView_WP.Width = tabControl_Komponenten.ClientSize.Width;
            //listView_WP.Height = tabControl_Komponenten.ClientSize.Height;
            listView_WP.Top = -2;
            listView_WP.Left = -2;

            listView_WP_Ref.View = View.Details;
            listView_WP_Ref.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_WP_Ref.Columns.Add("Vorlauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP_Ref.Columns.Add("Rücklauf [°C]", -2, HorizontalAlignment.Left);
            listView_WP_Ref.Columns.Add("Betriebsart", -2, HorizontalAlignment.Left);
            listView_WP_Ref.Width = tabControl_Komponenten.ClientSize.Width;
            listView_WP_Ref.Left = -2;
            listView_WP_Ref.Height = tabPage_Komponenten.Height - listView_WP_Ref.Top + 2;
            //listView_WP_Ref.Columns.Add("", 0, HorizontalAlignment.Left);


            listView_SP.View = View.Details;
            listView_SP.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_SP.Columns.Add("Typ", -2, HorizontalAlignment.Left);
            listView_SP.Columns.Add("Leistung [kW]", -2, HorizontalAlignment.Left);
            listView_SP.Columns.Add("Energie", -2, HorizontalAlignment.Left);
            listView_SP.Columns.Add("Degradation", -2, HorizontalAlignment.Left);
            listView_SP.Columns.Add("Ladezustand", -2, HorizontalAlignment.Left);
            listView_SP.Width = tabControl_Komponenten.ClientSize.Width;
            //listView_SP.Height = tabControl_Komponenten.ClientSize.Height;
            listView_SP.Top = -2;
            listView_SP.Left = -2;

            listView_SP_REF.View = View.Details;
            listView_SP_REF.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_SP_REF.Columns.Add("Typ", -2, HorizontalAlignment.Left);
            listView_SP_REF.Columns.Add("Leistung [kW]", -2, HorizontalAlignment.Left);
            listView_SP_REF.Columns.Add("Energie", -2, HorizontalAlignment.Left);
            listView_SP_REF.Columns.Add("Degradation", -2, HorizontalAlignment.Left);
            listView_SP_REF.Columns.Add("Ladezustand", -2, HorizontalAlignment.Left);
            listView_SP_REF.Width = tabControl_Komponenten.ClientSize.Width;
            //listView_SP_REF.Height = tabControl_Komponenten.ClientSize.Height;
            listView_SP_REF.Left = -2;
            listView_SP_REF.Height = tabPage_Simulation_WP.Height - listView_SP_REF.Top + 2;

            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_WP_Ref.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WP_Ref.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            listView_Heizkessel.View = View.Details;
            listView_Heizkessel.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Heizkessel.Columns.Add("Typ", -2, HorizontalAlignment.Left);
            listView_Heizkessel.Columns.Add("Leistung [kW]", -2, HorizontalAlignment.Left);
            listView_Heizkessel.Columns.Add("Beschreibung", -2, HorizontalAlignment.Left);
            listView_Heizkessel.Width = tabControl_Komponenten.ClientSize.Width;
            //listView_Heizkessel.Height = tabControl_Komponenten.ClientSize.Height;
            listView_Heizkessel.Left = -2;
            listView_Heizkessel.Top = -2;
            listView_Heizkessel.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Heizkessel.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            listView_Heizkessel_REF.View = View.Details;
            listView_Heizkessel_REF.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_Heizkessel_REF.Columns.Add("Typ", -2, HorizontalAlignment.Left);
            listView_Heizkessel_REF.Columns.Add("Leistung [kW]", -2, HorizontalAlignment.Left);
            listView_Heizkessel_REF.Columns.Add("Beschreibung", -2, HorizontalAlignment.Left);
            listView_Heizkessel_REF.Width = tabControl_Komponenten.ClientSize.Width;
            listView_Heizkessel_REF.Left = -2;
            listView_Heizkessel_REF.Height = tabPage_Simulation_WP.Height - listView_Heizkessel_REF.Top + 2;
            listView_Heizkessel_REF.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Heizkessel_REF.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

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

            listView_SimSPK.View = View.Details;
            listView_SimSPK.Columns.Add("Kessel", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Gas/Biogas/Rapsöl/Holz... [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Öl [MWh/a]", -2, HorizontalAlignment.Left);
            listView_SimSPK.Columns.Add("Jahresnutzungsgrad [%]", -2, HorizontalAlignment.Left);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);


            init_Chart(chart1);
            init_Chart(chart2);

            comboBox_Bereitschaft.Items.Add("6000");
            comboBox_Bereitschaft.Items.Add("3000");

        }

        private void init_Chart(System.Windows.Forms.DataVisualization.Charting.Chart chart)
        {
            var ca = chart.ChartAreas[0];
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.CursorY.IsUserEnabled = true;
            ca.CursorY.IsUserSelectionEnabled = true;

            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisX.ScaleView.Zoomable = true;
            ca.CursorX.AutoScroll = true;
            ca.AxisX.ScrollBar.Enabled = true;

            chart.Series[0].BorderWidth = 2;
            chart.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].CursorX.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].CursorY.LineDashStyle = ChartDashStyle.Dot;
            chart.ChartAreas[0].CursorX.LineColor = Color.Red;
            chart.ChartAreas[0].CursorY.LineColor = Color.Red;
        }

        private void button_Beenden_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        public void SetWPControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            WPCtrl wpctrl = new WPCtrl();

            projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Projekt.Text + "'");
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.WP_TYP + " or ID_Type=" + WizardItemClass.REF_WP_TYP + ")");

            listView_WP.Items.Clear();
            listView_WP_Ref.Items.Clear();

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
                else
                {
                    listView_WP_Ref.Items.Add(lvitem);
                }
            }
            rs.Close();

            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_WP_Ref.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WP_Ref.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void SetSPControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            WPCtrl wpctrl = new WPCtrl();

            projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Projekt.Text + "'");
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.SP_TYP + " or ID_Type=" + WizardItemClass.REF_SP_TYP + ")");
            listView_SP.Items.Clear();
            listView_SP_REF.Items.Clear();

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
                    else
                    {
                        listView_SP_REF.Items.Add(lvitem);
                    }
                }
                rs_sp.Close();
            }
            rs.Close();

            listView_SP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SP.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_SP_REF.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SP_REF.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
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
            WPKontextMenuCtrl refctrl = new WPKontextMenuCtrl();
            refctrl.Init(listView_WP_Ref, m_ID_Projekt, m_szProjektname);
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
            SpKontextMenuCtrl refctrl = new SpKontextMenuCtrl();
            refctrl.Init(listView_SP_REF, m_ID_Projekt, m_szProjektname);
        }

        public void Add_HeizkesselKontext()
        {
            HeizkesselKontextMenuCtrl ctrl = new HeizkesselKontextMenuCtrl();
            ctrl.Init(listView_Heizkessel, m_ID_Projekt, m_szProjektname);
            HeizkesselKontextMenuCtrl refctrl = new HeizkesselKontextMenuCtrl();
            refctrl.Init(listView_Heizkessel_REF, m_ID_Projekt, m_szProjektname);
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

        public void SetHeizkesselControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();

            projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Projekt.Text + "'");
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.REF_KESSEL_TYP + " or ID_Type=" + WizardItemClass.KESSEL_TYP + ")");

            listView_Heizkessel.Items.Clear();
            listView_Heizkessel_REF.Items.Clear();

            while (rs.Next())
            {
                ListViewItem lvitem = new ListViewItem();
                RecordSet rs_hk = new RecordSet();

                rs_hk.Open("select * from [DB-Heizung] where ID=" + rs.Read("ID_Kessel"));

                if (!rs_hk.EOF())
                {
                    lvitem.Text = (string)rs_hk.Read("Name");
                    lvitem.SubItems.Add(BrennstoffCtrl.Brennstoffart[(int)rs_hk.Read("Brennstoff")]);
                    double kl = (double)rs_hk.Read("Ptherm");
                    lvitem.SubItems.Add(kl.ToString("F2"));
                    lvitem.SubItems.Add(rs_hk.Read("Beschreibung").ToString());
                    lvitem.SubItems.Add(rs.Read("ID").ToString());
                    if ((int)rs.Read("ID_Type") == WizardItemClass.KESSEL_TYP)
                    {
                        listView_Heizkessel.Items.Add(lvitem);
                    }
                    else
                    {
                        listView_Heizkessel_REF.Items.Add(lvitem);
                    }
                }
                rs_hk.Close();
            }
            rs.Close();
            listView_Heizkessel.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Heizkessel.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_Heizkessel_REF.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_Heizkessel_REF.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void SetGebaeudeControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();

            projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Projekt.Text + "'");


            string sql = "SELECT Z_ProjektGebaeude.ID, Z_ProjektGebaeude.ID_Projekt, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, " +
             "[DBGebaeude].Gebaeudename, Z_ProjektGebaeude.Einheit_Waermebedarf_Wohnflaeche, [DBGebaeude].Gebaeudeart " +
             "FROM [DBGebaeude] INNER JOIN Z_ProjektGebaeude ON [DBGebaeude].ID = Z_ProjektGebaeude.ID_Gebaeude" +
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

            projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Projekt.Text + "'");

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
            ProzesswaermeCtrl pwctrl = new ProzesswaermeCtrl();

            ctrl.ReadAll("select * from Z_Projekt_Prozesswaerme where ID_Projekt=" + m_ID_Projekt);

            listView_Prozesswaerme.Items.Clear();

            for (int i = 0; i < ctrl.rows; i++)
            {
                pwctrl.ReadSingle(ctrl.items[i].ID_Prozesswaerme);
                for (int j = 0; j < pwctrl.rows; j++)
                {
                    ListViewItem lvitem = new ListViewItem();
                    lvitem.Text = pwctrl.m_szProzessname;
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
            simulation_Waermebedarf.DBGelesen = false;
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

        private void btn_SimulSpeichern_Click(object sender, EventArgs e)
        {
            simulation_Waermebedarf.SimulationErgebis_in_DB();
        }

        private void textBox_Netzverluste_Validating(object sender, CancelEventArgs e)
        {
            System.Windows.Forms.TextBox tb = sender as System.Windows.Forms.TextBox;
            if (!Program.checkInt(tb, tb.Text)) { tb.Undo(); e.Cancel = true; }
        }

        private void checkBox_Sortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Sortiert.Checked)
            {
                chart1.Series[0].Points.DataBindY(simulation_Waermebedarf.Dauerlinie);
            }
            else chart1.Series[0].Points.DataBindY(simulation_Waermebedarf.Dauerlinie_nicht_sortiert);
            chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Interval = 1000;
            chart1.ChartAreas[0].AxisY.Maximum = 100.2;
            chart1.Series[0].BorderWidth = 2;
        }

        private void btn_Details_Click(object sender, EventArgs e)
        {
            Form_ErgProzesswaerme frm = new Form_ErgProzesswaerme();
            frm.Init(simulation_Waermebedarf);
            frm.ShowDialog();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {

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

        private void listView_SP_REF_DoubleClick(object sender, EventArgs e)
        {
            SpKontextMenuCtrl ctrl = new SpKontextMenuCtrl();
            ctrl.Init(listView_SP_REF, m_ID_Projekt, m_szProjektname);
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
            else if ((ListView)drag_control == listView_SP_REF)
            {
                SpKontextMenuCtrl ctrl = new SpKontextMenuCtrl();
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
            else if ((ListView)drag_control == listView_Heizkessel_REF)
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
            else if ((ListView)drag_control == listView_WP_Ref)
            {
                WPKontextMenuCtrl ctrl = new WPKontextMenuCtrl();
                ctrl.Init((ListView)drag_control, m_ID_Projekt, m_szProjektname);
                ctrl.contextMenuStrip1.Items[2].PerformClick();
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

        private void listView_SP_REF_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_SP_REF;
                ListViewItem lvi = listView_SP_REF.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_SP_REF.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }

        }

        private void listView_SP_REF_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;

        }

        private void listView_SP_REF_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_SP_REF.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }

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

        private void btn_Start_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            int netzverluste = Int32.Parse(textBox_Netzverluste.Text);

            if (comboBox_NetzvEinheit.Text == "%" && netzverluste > 100)
            {
                MessageBox.Show("die Netzverluste dürfen nicht größer als 100 % sein!");
                Simulation_durchgeführt = false;
                return;
            }

            int nKlimaregion = GetIDKlimaregion();
            if (nKlimaregion == 0)
            {
                MessageBox.Show("Klimaregion auswählen!");
                Simulation_durchgeführt = false;
                return;
            }

            simulation_Waermebedarf.Netzverluste = netzverluste;
            simulation_Waermebedarf.Netzverluste_Einheit = comboBox_NetzvEinheit.Text;
            simulation_Waermebedarf.Waermebedarf_berechnen(m_ID_Projekt, nKlimaregion);

            textBox_MaxWaermelast.Text = simulation_Waermebedarf.Waermebedarf_Max.ToString("F2");
            textBox_Gesamt_Waermebedarf.Text = simulation_Waermebedarf.Waermebedarf_Gesamt.ToString("F2");

            chart1.Annotations.Clear();
            chart1.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chart1.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);

            if (checkBox_Sortiert.Checked)
            {
                chart1.Series[0].Points.DataBindY(simulation_Waermebedarf.Dauerlinie);
            }
            else chart1.Series[0].Points.DataBindY(simulation_Waermebedarf.Dauerlinie_nicht_sortiert);
            chart1.ChartAreas[0].AxisY.Maximum = 100.2;

            rs.Open("UPDATE Tab_Projekt SET Netzverluste=" + Int32.Parse(textBox_Netzverluste.Text) + ", Netzverluste_Einheit='" + comboBox_NetzvEinheit.Text + "' where ID=" + m_ID_Projekt);
            rs.Close();

            //   Simulation_durchgeführt = true;
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

        private void btn_Start_Strombedarf_Click(object sender, EventArgs e)
        {
            simulation_strom.Berechnung(m_ID_Projekt);

            textBox_MaxStrombedarf.Text = simulation_strom.Strombedarf_Max.ToString("F2");
            textBox_Gesamt_Strombedarf.Text = simulation_strom.Strombedarf_gesamt.ToString("F2");

            chart2.Annotations.Clear();
            chart2.ChartAreas[0].AxisX.ScaleView.ZoomReset(0);
            chart2.ChartAreas[0].AxisY.ScaleView.ZoomReset(0);
            if (checkBox_StromSortiert.Checked)
            {
                chart2.Series[0].Points.DataBindY(simulation_strom.Dauerlinie);
            }
            else chart2.Series[0].Points.DataBindY(simulation_strom.Dauerlinie_nicht_sortiert);
        }

        private void checkBox_StromSortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_StromSortiert.Checked)
            {
                chart2.Series[0].Points.DataBindY(simulation_strom.Dauerlinie);
            }
            else chart2.Series[0].Points.DataBindY(simulation_strom.Dauerlinie_nicht_sortiert);
            chart2.ChartAreas[0].AxisX.Minimum = 0;
            chart2.ChartAreas[0].AxisX.Interval = 1000;
            chart2.ChartAreas[0].AxisY.Maximum = 100.2;
            chart2.Series[0].BorderWidth = 2;
        }

        private void btn_StromDetails_Click(object sender, EventArgs e)
        {
            Form_ErgStromverbraucher frm = new Form_ErgStromverbraucher();
            frm.Init(simulation_strom);
            frm.ShowDialog();
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

        private void SelectionRangeBegin(object sender, CursorEventArgs e)
        {
            ChartSelBegin = chart1.ChartAreas[0].CursorX.SelectionStart;
            ChartSelEnd = chart1.ChartAreas[0].CursorX.SelectionEnd;
        }

        private void AxisScrollBarClicked(object sender, ScrollBarEventArgs e)
        {
            if (e.ButtonType == ScrollBarButtonType.ZoomReset)
            {
                chart1.Annotations.Clear();
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {
            ChartSelBegin = chart1.ChartAreas[0].CursorX.Position;
            if (!Program.HasValue(ChartSelBegin)) return;
            if (chart1.Series[0].Points.Count == 0) return;

            DateTime date = new DateTime(DateTime.Now.Year, 1, 1);
            date = new DateTime(DateTime.Now.Year, 1, 1).AddHours(ChartSelBegin - 1);
            string szText = date.ToString("dd.MMMM");

            TextAnnotation ta2 = new TextAnnotation();
            ta2.Text = szText;
            ta2.AnchorX = 18;  // % of chart width
            ta2.AnchorY = 98;  // % of chart height, from top
            if (chart1.Annotations.Count == 0)
            {
                chart1.Annotations.Add(ta2);
            }
            else
            {
                chart1.Annotations[0] = ta2;
            }
;
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

            projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Projekt.Text + "'");

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

        private void listView_WP_Ref_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_WP_Ref;
                ListViewItem lvi = listView_WP_Ref.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_WP_Ref.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_WP_Ref_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_WP_Ref.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_WP_Ref_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form_StromTest frm = new Form_StromTest();
            frm.SetControls(m_ID_Projekt);
            frm.ShowDialog();
        }

        private void btn_WP_Simulation_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
            watch.Start();

            textBox_WB_Deckung.Text = "";

            if (!Simulation_durchgeführt)
            {
                int netzverluste = Int32.Parse(textBox_Netzverluste.Text);

                if (comboBox_NetzvEinheit.Text == "%" && netzverluste > 100)
                {
                    MessageBox.Show("die Netzverluste dürfen nicht größer als 100 % sein!");
                    Simulation_durchgeführt = false;
                    return;
                }

                int nKlimaregion = GetIDKlimaregion();
                if (nKlimaregion == 0)
                {
                    MessageBox.Show("Klimaregion auswählen!");
                    Simulation_durchgeführt = false;
                    return;
                }

                simulation_Waermebedarf.Netzverluste = netzverluste;
                simulation_Waermebedarf.Netzverluste_Einheit = comboBox_NetzvEinheit.Text;
                simulation_Waermebedarf.Waermebedarf_berechnen(m_ID_Projekt, nKlimaregion);
            }

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.WP_TYP);

            simulation_wp.wp_list.Clear();

            while (rs.Next())
            {
                simulation_wp.wp_list.Add((int)rs.Read("ID"));
            }
            rs.Close();

            simulation_wp.Temperatur = simulation_Waermebedarf.Stundentemperatur;
            simulation_wp.Waermebedarf_stuendlich = simulation_Waermebedarf.Waermebedarf;
            simulation_wp.Mit_Heizstab = checkBox_Heizstab.Checked;

            // Simulation starten
            simulation_wp.Berechnung();

            double a = (double)simulation_Waermebedarf.Waermebedarf_Gesamt;
            double b = (double)simulation_wp.WP_Waermeproduktion_gesamt / 1000;
            double c = (double)simulation_wp.Heizstab_gesamt / 1000;

            if ((b / a * 100) > 100)
                textBox_WB_Deckung.Text = "100";
            else
                textBox_WB_Deckung.Text = (b / a * 100).ToString("F2");

            if (checkBox_Heizstab.Checked)
                textBox_WB_Deckung.Text = ((b + c) / a * 100).ToString("F2");
            else
                textBox_WB_Deckung.Text = (b / a * 100).ToString("F2");


            textBox_Laufzeit.Text = (simulation_wp.WP_Laufzeit / simulation_wp.wp_list.Count).ToString("F0");
            if (simulation_wp.Bivalenzpunkt != -100)
                textBox_Bivalenzpunkt.Text = simulation_wp.Bivalenzpunkt.ToString("F2");
            else
                textBox_Bivalenzpunkt.Text = "-";

            chart3.ChartAreas[0].AxisX.Minimum = 0;
            chart3.ChartAreas[0].AxisX.Interval = 1000;

            chart3.Series["Waermebedarf"].Points.DataBindY(simulation_wp.Waermebedarf_stuendlich);
            chart3.Series["Waermebedarf"].BorderWidth = 1;
            chart3.Series["Waermebedarf"].Color = System.Drawing.Color.Red;

            float[] temp = new float[8760];
            for (int i = 0; i < 8760; i++)
            {
                if (checkBox_Heizstab.Checked)
                {
                    temp[i] = simulation_wp.WP_Waermeproduktion_stuendlich[i] + simulation_wp.Heizstab_stuendlich[i];
                }
                else temp[i] = 0;
            }
            chart3.Series["Heizstab"].Points.DataBindY(temp);
            chart3.Series["Heizstab"].BorderWidth = 1;
            chart3.Series["Heizstab"].Color = System.Drawing.Color.Yellow;

            chart3.Series["Waermeproduktion"].Points.DataBindY(simulation_wp.WP_Waermeproduktion_stuendlich);
            chart3.Series["Waermeproduktion"].BorderWidth = 1;
            chart3.Series["Waermeproduktion"].Color = System.Drawing.Color.Blue;

            textBox_Waermebedarf.Text = simulation_Waermebedarf.Waermebedarf_Gesamt.ToString("F2");
            textBox_WPStromverbrauch.Text = (simulation_wp.WP_Strombedarf_gesamt / 1000).ToString("F2");
            textBox_HeizstabStromverbrauch.Text = (simulation_wp.Heizstab_gesamt / 1000).ToString("F2");
            textBox_Restwaerme.Text = (simulation_Waermebedarf.Waermebedarf_Gesamt - simulation_wp.WP_Waermeproduktion_gesamt / 1000 - simulation_wp.Heizstab_gesamt / 1000).ToString("F2");
            textBox_WPWaermeproduktion.Text = (simulation_wp.WP_Waermeproduktion_gesamt / 1000).ToString("F2");
            textBox_Pufferspeicher.Text = (simulation_wp.Volumen_Pufferspeicher * 1.16).ToString();
            textBox_WPVollbenutzungsstunden.Text = (simulation_wp.WP_Laufzeit / simulation_wp.wp_list.Count).ToString("F0");
            
            textBox_Waermerestbedarf.Text = (simulation_wp.waermerestbedarf_gesamt/1000).ToString("F2");
            tb_gesWaermebedarf.Text = (simulation_Waermebedarf.Waermebedarf_Gesamt).ToString("F2");

            double Max_Spk = 0;
            for (int i = 0; i < 8750; i++)
            {
                if (simulation_wp.waermerestbedarf_stuendlich[i] > Max_Spk) Max_Spk = simulation_wp.waermerestbedarf_stuendlich[i];
            }
            textBox_MinSPKLeistung.Text = Max_Spk.ToString("F2");

            for (int i = 1; i <= 5; i++)
            {
                tabPage_Simulation_WP.Controls["textBox" + i.ToString() + "_1"].Text = (simulation_wp.Modul_WP_Waermeproduktion[i - 1] / 1000).ToString("F2");
                tabPage_Simulation_WP.Controls["textBox" + i.ToString() + "_2"].Text = (simulation_wp.Modul_WP_Strombedarf[i - 1] / 1000).ToString("F2");
                tabPage_Simulation_WP.Controls["textBox" + i.ToString() + "_3"].Text = (simulation_wp.Modul_Heizstab[i - 1] / 1000).ToString("F2");
                tabPage_Simulation_WP.Controls["textBox" + i.ToString() + "_4"].Text = (simulation_wp.Modul_WP_Laufzeit[i - 1]).ToString("F2");
                tabPage_Simulation_WP.Controls["textBox" + i.ToString() + "_Modul"].Text = simulation_wp.WP_Modul[i - 1];
            }

            // chart Temperatur - Leistung  
            PointF[] ps_produktion_raw = new PointF[8760];
            PointF[] ps_bedarf_raw = new PointF[8760];
            PointF[] ps_heizstab_raw = new PointF[8760];

            List<double> werte_produktion = new List<double>();
            List<double> werte_bedarf = new List<double>();

            // nur 1 Leistungswert Wert pro gleicher Temperatur nehmen
            int index = 0;
            for (int n = 0; n < 8760; n++)
            {
                if (werte_produktion.Contains(simulation_wp.Temperatur[n])) continue;
                double d = Math.Round(simulation_wp.Temperatur[n], 1);
                ps_produktion_raw[index].X = (float)d;
                ps_produktion_raw[index].Y = simulation_wp.WP_Waermeproduktion_stuendlich[n];
                ps_bedarf_raw[index].X = ps_produktion_raw[index].X;
                ps_bedarf_raw[index].Y = simulation_wp.Waermebedarf_stuendlich[n];
                if (simulation_wp.Heizstab_stuendlich[n] > 0)
                    ps_heizstab_raw[index].Y = simulation_wp.WP_Waermeproduktion_stuendlich[n] + simulation_wp.Heizstab_stuendlich[n];
                else
                    ps_heizstab_raw[index].Y = 0;

                ps_heizstab_raw[index].X = ps_produktion_raw[index].X;
                werte_produktion.Add(simulation_wp.Temperatur[n]);
                werte_bedarf.Add(simulation_wp.Waermebedarf_stuendlich[n]);
                index++;
            }
;
            // Points Array nur mit der tatsächlichen Anzahl(mehrfache Werte gleicher Tempeatur filtern) füllen
            PointF[] ps_produktion = new PointF[index];
            PointF[] ps_bedarf = new PointF[index];
            PointF[] ps_heizstab = new PointF[index];

            for (int n = 0; n < index; n++)
            {
                ps_produktion.SetValue(ps_produktion_raw[n], n);
                ps_bedarf.SetValue(ps_bedarf_raw[n], n);
                ps_heizstab.SetValue(ps_heizstab_raw[n], n);
            }

            chart4.Series[0].ChartType = SeriesChartType.Area;
            chart4.Series[1].ChartType = SeriesChartType.Area;
            chart4.Series[2].ChartType = SeriesChartType.Area;
            chart4.Series["Waermeproduktion"].Color = Color.FromArgb(100, Color.Blue);
            chart4.Series["Waermebedarf"].Color = Color.FromArgb(140, Color.Red);
            chart4.Series["Heizstab"].Color = Color.FromArgb(240, Color.Yellow);

            chart4.Series["Heizstab"].Points.DataBindXY(ps_heizstab, "X", ps_heizstab, "Y");
            chart4.Series["Waermeproduktion"].Points.DataBindXY(ps_produktion, "X", ps_produktion, "Y");
            chart4.Series["Waermebedarf"].Points.DataBindXY(ps_bedarf, "X", ps_bedarf, "Y");

            chart4.Series[0].Sort(PointSortOrder.Ascending, "X");
            chart4.Series[1].Sort(PointSortOrder.Ascending, "X");
            chart4.Series[2].Sort(PointSortOrder.Ascending, "X");

            chart4.ChartAreas[0].AxisX.Interval = 4;
            chart4.ChartAreas[0].AxisX.LabelStyle.Format = "0.0";
            chart4.Update();

            watch.Stop();
            TimeSpan ts = watch.Elapsed;
            Console.WriteLine("Zeit, die benötigt wurde: " + watch.Elapsed + " ms:" + ts.Milliseconds);

        }

        private void checkBox_WP_sortiert_CheckedChanged(object sender, EventArgs e)
        {
            float[] temp = new float[8760];

            for (int i = 0; i < 8760; i++)
            {
                if (checkBox_Heizstab.Checked)
                {
                    temp[i] = simulation_wp.WP_Waermeproduktion_stuendlich[i] + simulation_wp.Heizstab_stuendlich[i];
                }
                else temp[i] = 0;
            }

            if (checkBox_WP_sortiert.Checked)
            {
                chart3.Series["Waermeproduktion"].Points.DataBindY(simulation_wp.WP_Waermeproduktion_stuendlich_sortiert);

                float[] sortedArray = new float[8760];
                Array.Copy(simulation_wp.Waermebedarf_stuendlich, sortedArray, 8760);
                Array.Sort(sortedArray);
                chart3.Series["Waermebedarf"].Points.DataBindY(sortedArray);
                float[] sortedArrayHeizstab = new float[8760];
                Array.Copy(temp, sortedArrayHeizstab, 8760);
                Array.Sort(sortedArrayHeizstab);
                chart3.Series["Heizstab"].Points.DataBindY(sortedArrayHeizstab);
            }
            else
            {
                chart3.Series["Waermeproduktion"].Points.DataBindY(simulation_wp.WP_Waermeproduktion_stuendlich);
                chart3.Series["Waermebedarf"].Points.DataBindY(simulation_wp.Waermebedarf_stuendlich);
                chart3.Series["Heizstab"].Points.DataBindY(temp);
            }
        }

        private void listView_Heizkessel_REF_MouseDown(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                drag_control = listView_Heizkessel_REF;
                ListViewItem lvi = listView_Heizkessel_REF.GetItemAt(e.X, e.Y);

                if (lvi != null)
                {
                    listView_Heizkessel_REF.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
                }
            }
        }

        private void listView_Heizkessel_REF_MouseMove(object sender, MouseEventArgs e)
        {
            if ((e.Button & MouseButtons.Left) == MouseButtons.Left)
            {
                listView_Heizkessel_REF.DoDragDrop(tabControl_Komponenten.SelectedIndex.ToString(), DragDropEffects.Link);
            }
        }

        private void listView_Heizkessel_REF_MouseUp(object sender, MouseEventArgs e)
        {
            this.Cursor = Cursors.Default;
        }

        private void btn_Simulation_Click(object sender, EventArgs e)
        {
            SimulationSPK sim = new SimulationSPK();
            RecordSet rs = new RecordSet();

            sim.spk_list.Clear();

            sim.Max_Waermebedarf = simulation_wp.waermerestbedarf_gesamt;
            sim.Waermebedarf = Array.ConvertAll(simulation_wp.waermerestbedarf_stuendlich, x => (double)x);

            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + m_ID_Projekt + " and ID_Type=" + WizardItemClass.KESSEL_TYP);
            while (rs.Next())
            {
                sim.spk_list.Add((string)rs.Read("Bezeichner"));
            }
            rs.Close();

            sim.Vorgabe_Betriebsbereitschaft = Int32.Parse(comboBox_Bereitschaft.Text);
            sim.Berechnung(m_ID_Projekt);


            tb_gesWaermebedarf.Text = (simulation_Waermebedarf.Waermebedarf_Gesamt).ToString("F2");
            tb_WaermeprSpk.Text = (sim.S_Waerme_spk).ToString("F2");
            tb_Gasverbrauch.Text = (sim.Gasverbrauch_SPK).ToString("F2");
            tb_Oelverbrauch.Text = (sim.Oelverbrauch_SPK).ToString("F2");
            tb_Biogasverbrauch.Text = (sim.Biogasverbrauch_SPK).ToString("F2");
            tb_Rapsoelverbrauch.Text = (sim.Rapsoelverbrauch_SPK).ToString("F2");
            tb_Holzverbrauch.Text = (sim.Holzverbrauch_SPK).ToString("F2");
            tb_Fluessiggasverbrauch.Text = (sim.Fluessiggasverbrauch_SPK).ToString("F2");
            tb_Stromverbrauch.Text = (sim.Stromverbrauch_Spk).ToString("F2");
            tb_Sonstigverbrauch.Text = (sim.Sonstigverbrauch_SPK).ToString("F2");


            tb_Max_Kesselleistung.Text = (sim.Maximale_Kesselleistung_Spk).ToString("F2");
            tb_Gasspitze.Text = sim.Gasspitze_Spk.ToString("F2");

            //reset your chart series and legends
            //chart5.Series.Clear();
            //chart5.Legends.Clear();

            //Add a new Legend(if needed) and do some formating
            //chart5.Legends.Add("MyLegend");
            chart5.Legends[0].LegendStyle = LegendStyle.Table;
            chart5.Legends[0].Docking = Docking.Right;
            chart5.Legends[0].Alignment = StringAlignment.Center;
            chart5.Legends[0].Title = "Wärmebedarfsdeckung";
            chart5.Legends[0].BorderColor = Color.Green;
            chart5.Series[0].IsValueShownAsLabel = false;
            chart5.Series[0]["PieLabelStyle"] = "Outside";
            //chart5.Series[0].Points.AddXY("Gesamtbbedarf", double.Parse(tb_gesWaermebedarf.Text));
            chart5.Series[0].Points.Clear();

            for (int i = 0; i < sim.spk_list.Count(); i++)
            {
                double valkessel = sim.s_waerme_Gas_Spk[i] + sim.s_waerme_Oel_Spk[i];
                if (valkessel > 0)
                    chart5.Series[0].Points.AddXY("Kessel" + (i + 1).ToString(), valkessel);
            }
       
            chart5.Series[0].Points.AddXY("WP", simulation_wp.WP_Waermeproduktion_gesamt/1000);
            if (simulation_wp.Heizstab_gesamt > 0)
                chart5.Series[0].Points.AddXY("Heizstab", simulation_wp.Heizstab_gesamt / 1000);   

            double rest = 0;
            for (int i = 0; i < sim.spk_list.Count() ; i++)
            {
                rest = rest + sim.s_waerme_Gas_Spk[i] + sim.s_waerme_Oel_Spk[i];
            }
            rest = simulation_Waermebedarf.Waermebedarf_Gesamt -
                                                        simulation_wp.WP_Waermeproduktion_gesamt / 1000 -
                                                        simulation_wp.Heizstab_gesamt / 1000 -
                                                        rest;

            textBox_SPKRestwermebedarf.Text = "0";
            if (rest >= 0.1)
            {
                textBox_SPKRestwermebedarf.Text = rest.ToString("F2");
                chart5.Series[0].Points.AddXY("Restbedarf", rest);
            }

            listView_SimSPK.Items.Clear();
            for (int i = 0; i < sim.spk_list.Count(); i++)
            {

                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = (i+1).ToString();
                lvitem.SubItems.Add(sim.spk_list[i]);
                lvitem.SubItems.Add((sim.s_waerme_Gas_Spk[i]).ToString("F2"));
                lvitem.SubItems.Add((sim.s_waerme_Oel_Spk[i]).ToString("F2"));
                lvitem.SubItems.Add((sim.Kessel_Wirk_Gas_Spk[i] * 100).ToString("F1"));

                listView_SimSPK.Items.Add(lvitem);
            }
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_SimSPK.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

        }
    }
}