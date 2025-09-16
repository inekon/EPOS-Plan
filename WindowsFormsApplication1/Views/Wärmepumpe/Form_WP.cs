using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Data.Odbc;

namespace WindowsFormsApplication1
{
    public partial class Form_WP : Form
    {
        private WPModel item;
        public bool CloseWithOK = false;
        private WPCtrl ctrl = null;
        private bool neu = false;

        public Form_WP()
        {
            InitializeComponent();
            item = new WPModel();
            ctrl = new WPCtrl();
            ctrl.ReadAll();
            FillWPList();
        }

        public Form_WP(string wpname)
        {
            InitializeComponent();
            item = new WPModel();
            ctrl = new WPCtrl();
            ctrl.ReadAll("WPName='" + wpname + "'");
            FillWPList();
            textBox_Name.Enabled = false;
            btn_Neu.Enabled=false;
            btn_Loeschen.Enabled = false;
        }

        public void FillWPList()
        {
            listBox_WP.Items.Clear();
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_WP.Items.Add(ctrl.items[i].WPName);
            }
            listBox_WP.SetSelected(0,true);
        }

        public void SetControls()
        {
            textBox_Beschreibung.Text = item.Beschreibung;
            comboBox_Baujahr.Text = item.Baujahr.ToString();
            comboBox_Leistungsstufen.Text = item.Regelung;
            comboBox_Waermepumpentyp.Text = item.Typ;
            textBox_Hersteller.Text = item.Firma;
            textBox_Modulkosten.Text = item.Modulkosten.ToString();
            textBox_Nennleistung.Text = item.Nennleistung.ToString();
            textBox_Heizstab.Text = item.Heizung.ToString();
            comboBox_Aufstellung.Text = item.Aufstellung;

            comboBox_Waermepumpentyp.Items.Add("Sole-Wasser");
            comboBox_Waermepumpentyp.Items.Add("Wasser-Wasser");
            comboBox_Waermepumpentyp.Items.Add("Luft-Wasser");
            comboBox_Waermepumpentyp.Items.Add("Luft-Luft-Klimagerät");
            comboBox_Leistungsstufen.Items.Add("einstufig");
            comboBox_Leistungsstufen.Items.Add("zweistufig");
            comboBox_Leistungsstufen.Items.Add("stetig");
            comboBox_Aufstellung.Items.Add("Außenaufstellung");
            comboBox_Aufstellung.Items.Add("Innen- oder Außenaufstellung");
            comboBox_Aufstellung.Items.Add("Innenaufstellung");
            comboBox_Aufstellung.Items.Add("ohne Typenbeschreibung");
            comboBox_Baujahr.Items.Add("2025");
            comboBox_Baujahr.Items.Add("2024");
            comboBox_Baujahr.Items.Add("2023");
            comboBox_Baujahr.Items.Add("2024");
            comboBox_Baujahr.Items.Add("2021");
            comboBox_Baujahr.Items.Add("2020");
            comboBox_Baujahr.Items.Add("2019");
            comboBox_Baujahr.Items.Add("2018");
            comboBox_Baujahr.Items.Add("2017");
            comboBox_Baujahr.Items.Add("2016");

            InitChart();
        }

        private void InitChart()
        {
            // Erstellen eines Datasets und Füllen mit Daten
            OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Kenndaten where ID_WP = " + item.ID + " order by Temperatur ASC", Program.DBConnection);
            DataSet dataSet = new DataSet();
            adapter.Fill(dataSet, "Tab_Kenndaten");
            
            chart1.ChartAreas[0].AxisX.Title = "Temperatur";
            chart1.ChartAreas[0].AxisY.Title = "COP";
            chart1.Series.Clear();

            chart2.ChartAreas[0].AxisX.Title = "Temperatur";
            chart2.ChartAreas[0].AxisY.Title = "Leistung";
            chart2.Series.Clear();

            KenndatenCtrl ctrl = new KenndatenCtrl();
            ctrl.ReadVorlauf("SELECT Vorlauf, ID_WP FROM Tab_Kenndaten GROUP BY Vorlauf, ID_WP HAVING ID_WP=" + item.ID);

            for (int i = 0; i < ctrl.rows; i++)
            {
                chart1.Series.Add(ctrl.items[i].m_nVorlauf.ToString());
                chart1.Series[i].Name = ctrl.items[i].m_nVorlauf.ToString() + "°C";
                chart1.Series[i].BorderWidth = 3;
                chart1.Series[i].ChartType = SeriesChartType.Line; // Oder ein anderer Typ
                chart2.Series.Add(ctrl.items[i].m_nVorlauf.ToString());
                chart2.Series[i].Name = ctrl.items[i].m_nVorlauf.ToString() + "°C";
                chart2.Series[i].BorderWidth = 3;
                chart2.Series[i].ChartType = SeriesChartType.Line; // Oder ein anderer Typ

                chart1.Series[i].XValueMember = "Temperatur";
                chart2.Series[i].XValueMember = "Temperatur";
                chart1.Series[i].YValueMembers = "COP";
                chart2.Series[i].YValueMembers = "Leistung";

                chart1.Series[i].SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
                chart1.Series[i].SmartLabelStyle.IsMarkerOverlappingAllowed = false;
                chart1.Series[i].SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Bottom;
                chart1.Series[i].Points.DataBind(dataSet.Tables["Tab_Kenndaten"].Select("Vorlauf=" + ctrl.items[i].m_nVorlauf.ToString()), "Temperatur", "COP", "");

                chart2.Series[i].SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
                chart2.Series[i].SmartLabelStyle.IsMarkerOverlappingAllowed = false;
                chart2.Series[i].SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Bottom;
                chart2.Series[i].Points.DataBind(dataSet.Tables["Tab_Kenndaten"].Select("Vorlauf=" + ctrl.items[i].m_nVorlauf.ToString()), "Temperatur", "Ptherm", "");
            }
        }

        private void butt_Beenden_Click(object sender, EventArgs e)
        {
            CloseWithOK = true;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            CloseWithOK = false;
            Close();
        }

        private void listBox_WP_SelectedIndexChanged(object sender, EventArgs e)
        {
            int index = listBox_WP.SelectedIndex;
            if (index != -1)
            {
                item = ctrl.items[index];
                textBox_Name.Text = item.WPName;
                SetControls();
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            ctrl.ReadSingle("select * from Tab_WP where WPName='" + item.WPName + "'");
            ctrl.Modulkosten = Int32.Parse(textBox_Modulkosten.Text);
            ctrl.Nennleistung = Int32.Parse(textBox_Nennleistung.Text);
            ctrl.Beschreibung = textBox_Beschreibung.Text;
            ctrl.Baujahr = Int32.Parse(comboBox_Baujahr.Text);
            ctrl.Regelung = comboBox_Leistungsstufen.Text;
            ctrl.Typ = comboBox_Waermepumpentyp.Text;
            ctrl.Firma = textBox_Hersteller.Text;
            ctrl.Heizung = Int32.Parse(textBox_Heizstab.Text);
            ctrl.Aufstellung = comboBox_Aufstellung.Text;
            ctrl.WPName = textBox_Name.Text;
            
            bool result;
            if (!neu)
            {
                result = ctrl.Update();
            }
            else
            {
                result = ctrl.Insert();
            }
             
            if (result)
            {
                ctrl.ReadAll();
                FillWPList();
                listBox_WP.SelectedIndex = listBox_WP.FindString(ctrl.WPName);
                MessageBox.Show("Gespeichert");
            }
            else
            {
                listBox_WP.SelectedIndex = 0; 
                MessageBox.Show("Speicherung nicht möglich, Fehler aufgetreten!");
            }
            neu = false;
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("Wollen Sie wirklich die Wärmepumpe löschen?", "Löschen", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                RecordSet rs = new RecordSet();
                rs.Open("SELECT Tab_Projekt.ID, Tab_Projekt.Projektname FROM Tab_Projekt INNER JOIN " +
                    "Tab_Energieanlagen ON Tab_Projekt.ID = " +
                    "Tab_Energieanlagen.ID_Projekt where Tab_Energieanlagen.Bezeichner='" + item.WPName + "'");
                if (rs.Next())
                {
                    MessageBox.Show("Löschen nicht möglich!\nDiese Wärmepumpe ist dem Projekt " + rs.Read("Projektname") + " zugeordnet!", "Hinweis");
                    return;
                }

                ctrl.ReadSingle("select * from Tab_WP where WPName = '" + listBox_WP.Text + "'");
                ctrl.Delete();
                ctrl.ReadAll();
                FillWPList();
            }
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            WPModel itemneu = new WPModel();
            neu = true;
            listBox_WP.ClearSelected();
            textBox_Beschreibung.Text = itemneu.Beschreibung;
            comboBox_Baujahr.Text = itemneu.Baujahr.ToString();
            comboBox_Leistungsstufen.Text = itemneu.Regelung;
            comboBox_Waermepumpentyp.Text = itemneu.Typ;
            textBox_Hersteller.Text = itemneu.Firma;
            textBox_Modulkosten.Text = itemneu.Modulkosten.ToString();
            textBox_Nennleistung.Text = itemneu.Nennleistung.ToString();
            textBox_Heizstab.Text = itemneu.Heizung.ToString();
            comboBox_Aufstellung.Text = itemneu.Aufstellung;
            textBox_Name.Text = "";
        }

        private void btn_Kenndaten_Click(object sender, EventArgs e)
        {
            DataSet ds = new DataSet();
            OdbcDataAdapter adapter = new OdbcDataAdapter("select * from Tab_Kenndaten where ID_WP=" + item.ID, Program.DBConnection);
            adapter.Fill(ds);
            
            
            DataTable table = ds.Tables[0];
            Kenndaten frm = new Kenndaten(ref ds);

            frm.m_ID_WP = item.ID;
            DialogResult ret = frm.ShowDialog();
            if (ret == DialogResult.OK)
            {
                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(ds);

                InitChart();
            }
        }
  
     }
}
