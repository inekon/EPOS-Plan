using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    public partial class FormMain : Form
    {
        private int m_ID_Projekt = 0;
        private string m_szProjektname;
        SimulationGebaeude simulation = new SimulationGebaeude();
        
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
            SimulationGebaeude.Ergebnis result;
            result = simulation.SimulationErgebis_aus_DB();

            textBox_MaxWaermelast.Text = result.Max_Waermebedarf.ToString("F2") ;
            textBox_Gesamt_Waermebedarf.Text = result.Gesamt_Waermebedarf.ToString("F2");
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
            listView_WP.Columns.Add("", 0, HorizontalAlignment.Left);
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
            listView_WP_Ref.Height = tabPage1.Height-listView_WP_Ref.Top+2;

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
            listView_SP_REF.Height = tabControl_Komponenten.ClientSize.Height;
            listView_SP_REF.Left = -2;
            listView_SP_REF.Height = tabPage2.Height - listView_SP_REF.Top + 2;

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

            listView_WaermebedatfExtern.View = View.Details;
            listView_WaermebedatfExtern.Columns.Add("Name", -2, HorizontalAlignment.Left);
            listView_WaermebedatfExtern.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WaermebedatfExtern.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
            listView_WaermebedatfExtern.Width = tabControl_Komponenten.ClientSize.Width;
            //listView_WaermebedatfExtern.Height = tabControl_Komponenten.ClientSize.Height;
            listView_WaermebedatfExtern.Top = -2;
            listView_WaermebedatfExtern.Left = -2;
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

            while(rs.Next())
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
                
                if(!rs_sp.EOF()) 
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
        }

        public void Add_WaermebedarfExternKontext()
        {
            WaermebedarfExternKontextMenuCtrl ctrl = new WaermebedarfExternKontextMenuCtrl();
            ctrl.Init(listView_WaermebedatfExtern, m_ID_Projekt, m_szProjektname);
        }

        public void SetHeizkesselControl(string Projekt)
        {
            ProjektCtrl projctrl = new ProjektCtrl();
            RecordSet rs = new RecordSet();
            
            projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Projekt.Text + "'");
            rs.Open("select * from Tab_Energieanlagen where ID_Projekt=" + projctrl.m_ID + " and (ID_Type=" + WizardItemClass.REF_KESSEL_TYP + ")");

            listView_Heizkessel.Items.Clear();

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
                    listView_Heizkessel.Items.Add(lvitem);
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
                    lvitem.Text = (string)rs.Read("Gebaeudename");
                    lvitem.SubItems.Add(rs.Read("Wohnflaeche_Waermebedarf").ToString());
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
            WaermebedarfExternCtrl waectrl = new WaermebedarfExternCtrl();

            projctrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Projekt.Text + "'");
            waectrl.ReadAll(projctrl.m_ID);

            listView_WaermebedatfExtern.Items.Clear();

            for(int i=0; i<waectrl.rows; i++)  
            {
                ListViewItem lvitem = new ListViewItem();
                lvitem.Text = waectrl.items[i].m_szDateiname;
                listView_WaermebedatfExtern.Items.Add(lvitem);
            }
     
            listView_WaermebedatfExtern.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView_WaermebedatfExtern.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);
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
            ProjektCtrl projektCtrl = new ProjektCtrl();
            projektCtrl.ReadSingle("select * from Tab_Projekt where Projektname='" + textBox_Projekt.Text + "'");

            projektCtrl.m_ID_Klimaregion = GetIDKlimaregion();
            projektCtrl.Update(); 
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

        private void button1_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();

            int netzverluste = Int32.Parse(textBox_Netzverluste.Text);

            if (comboBox_NetzvEinheit.Text == "%" && netzverluste > 100)
            {
                MessageBox.Show("die Netzverluste dürfen nicht größer als 100 % sein!");
                return;
            }

            simulation.Netzverluste = netzverluste;
            simulation.Netzverluste_Einheit = comboBox_NetzvEinheit.Text; 
            simulation.Berechnung(m_ID_Projekt, GetIDKlimaregion());

            textBox_MaxWaermelast.Text = simulation.Max_Waermebedarf.ToString("F2");

            chart1.Series[0].Points.DataBindY(simulation.Dauerlinie);
            chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Interval = 1000;
            chart1.ChartAreas[0].AxisY.Maximum = 100.2;
            chart1.Series[0].BorderWidth = 2;
       
            rs.Open("UPDATE Tab_Projekt SET Netzverluste=" + Int32.Parse(textBox_Netzverluste.Text) + ", Netzverluste_Einheit='" + comboBox_NetzvEinheit.Text + "' where ID=" + m_ID_Projekt );
            rs.Close();
        }

        private void btn_SimulSpeichern_Click(object sender, EventArgs e)
        {
            simulation.SimulationErgebis_in_DB();
        }

        private void textBox_Netzverluste_Validating(object sender, CancelEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkInt(tb, tb.Text)) { tb.Undo(); e.Cancel = true; }
        }

 
     }
}
