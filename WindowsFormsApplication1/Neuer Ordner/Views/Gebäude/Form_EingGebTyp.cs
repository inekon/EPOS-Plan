using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Data.Odbc;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form_EingGebTyp : Form
    {
        double[,] Verteilung;
        int _selected_index = 0;
        int AnzalTagV = 0;
        int ID_TagV = 0; 

        private List<string> list_TagVName_0 = new List<string>() { "Winter-heiter", "Winter-trübe", "Übergang-heiter", "Übergang-trübe", "Sommertag" };
        private List<string> list_TagVName = new List<string>() { "Winter-Wochentag", "Winter-Wochenende", "Übergang1-Wochentag", 
                                          "Übergang1-Wochenende", "Sommer-Wochentag","Sommer-Wochenende",
                                          "Übergang2-Wochentag", "Übergang2-Wochenende"};
        ToolTip tt = new ToolTip();

        public Form_EingGebTyp()
        {
            InitializeComponent();
            tt.Draw += new DrawToolTipEventHandler(this.tt_Draw);
        }

        public void SetControls()
        {
            TagVCtrl ctrl = new TagVCtrl();
            ctrl.ReadAll("select * from DBTagV");
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_Typename.Items.Add(ctrl.items[i].Name);  
            }
            listBox_Typename.SelectedIndex = 0;
            listBox_Typename.Focus(); 
        }

        private void listBox_Typename_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            TagVCtrl ctrl = new TagVCtrl();

            if (listBox_Typename.Text == "") return;
            ctrl.ReadAll("select * from DBTagV where Name='" + listBox_Typename.Text + "'");
            textBox_Beschreibung.Text = ctrl.items[0].Beschreibung;
            textBox_Beschreibung.Select(0, 0); 

            string sql = "SELECT Count('Verteilung') AS Ausdr1 FROM DBTagVDaten WHERE [ID_TagV]=" + ctrl.items[0].ID;
            rs.Open(sql);
            rs.Next();

            int anz = (int)rs.Read("Ausdr1");
            AnzalTagV = anz;
            ID_TagV = ctrl.items[0].ID;
            rs.Close();

            Verteilung = new double[anz, 24];

            rs.Open("select * from DBTagVDaten where ID_TagV=" + ctrl.items[0].ID);
            rs.Next();

            listBox_Kurve.Items.Clear();
            for (int n = 0; n < anz/24; n++)
            {
                for (int i = 0; i < 24; i++)
                {
                    double value = (double)rs.Read("Verteilung");
                //    groupBox1.Controls["st" + (i+1).ToString()].Text = value.ToString("F2");
                    Verteilung[n,i] = value;
                    rs.Next();
                }
                listBox_Kurve.Items.Add(GetTagVName(n));
            }
            rs.Close();

            listBox_Kurve.SelectedIndex = 0;
            _selected_index = 0;

            tt.OwnerDraw = true; 
            tt.BackColor = Color.LightYellow;
            tt.ForeColor = Color.Black;  
            if (ctrl.items[0].Veraenderbar)
            {
                btn_Speichern.Enabled = true;
                tt.Hide(listBox_Typename); 
            }
            else
            {
                btn_Speichern.Enabled = false;
                tt.Show("Die vom Softwarehersteller gelieferten Gebäudetypen können nicht geändert werden", listBox_Typename, 0, 0, 1000);
            }
        }

        private void tt_Draw(object sender, DrawToolTipEventArgs e)
        {
            e.DrawBackground();
            e.DrawBorder();
            e.DrawText();
        }
        private string GetTagVName(int index)
        {
            if(listBox_Typename.SelectedIndex==0)
            {
                return list_TagVName_0[index];
            }
            else
            {
                return list_TagVName[index];
            }
        }

        private void listBox_Kurve_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<int> xAxis = new List<int>();
            List<Double> yAxis = new List<Double>(); 

            int n = listBox_Kurve.SelectedIndex;

            if(n !=_selected_index) RefreshArrayValues();

            for (int i = 0; i < 24; i++)
            {
                groupBox1.Controls["st" + (i + 1).ToString()].Text = Verteilung[n, i].ToString("F4");
                xAxis.Add(i);
                yAxis.Add(Verteilung[n, i]);  
            }

            init_Chart(xAxis,yAxis);  
            _selected_index = n;
        }

        private void RefreshArrayValues()
        {
            for (int i = 0; i < 24; i++)
            {
                 Verteilung[_selected_index, i] = double.Parse(groupBox1.Controls["st" + (i + 1).ToString()].Text);
            }
        }

        private void init_Chart(List<int> xAxis,List<double> yAxis)
        {
            chart1.Series.Clear();
            var series = new Series("Tagesverteilung");   
  
            series.Points.DataBindXY(xAxis, yAxis);
            chart1.Series.Add(series);
            series.ChartType = SeriesChartType.Line;
            chart1.ChartAreas[0].AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chart1.ChartAreas[0].AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            chart1.Series[0].SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
            chart1.Series[0].SmartLabelStyle.IsMarkerOverlappingAllowed = false;
            chart1.Series[0].SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Bottom;
            chart1.Series[0].IsValueShownAsLabel = false;
            chart1.ChartAreas[0].AxisX.Minimum = 0;
            chart1.ChartAreas[0].AxisX.Maximum = 24;
            chart1.ChartAreas[0].AxisX.Interval = 2;
            chart1.Series[0].IsValueShownAsLabel = false;
            chart1.Series[0].BorderWidth = 2;
            chart1.Update();

            series = null;
        }

        private void btn_Schliessen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            List<int> xAxis = new List<int>();
            List<Double> yAxis = new List<Double>();

            RefreshArrayValues();
            TagV_Speichern(ID_TagV);

            int n = listBox_Kurve.SelectedIndex;
            for (int i = 0; i < 24; i++)
            {
                groupBox1.Controls["st" + (i + 1).ToString()].Text = Verteilung[n, i].ToString("F4");
                xAxis.Add(i);
                yAxis.Add(Verteilung[n, i]);
            }
            init_Chart(xAxis, yAxis); 
        }

        bool TagV_Speichern(int id_tagv)
        {
            try
            {
                OdbcDataAdapter adapter = new OdbcDataAdapter("select * from DBTagVDaten where ID_TagV=" + id_tagv, Program.DBConnection);
                DataSet dataSet = new DataSet();

                adapter.Fill(dataSet, "DBTagVDaten");

                for (int n = 0; n < AnzalTagV / 24; n++)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        DataRow row = dataSet.Tables["DBTagVDaten"].Rows[n * 24 + i];
                        row["ID_TagV"] = id_tagv;
                        row["Verteilung"] = Verteilung[n, i];
                    }
                }

                OdbcCommandBuilder commandBuilder = new OdbcCommandBuilder(adapter);
                adapter.Update(dataSet, "DBTagVDaten");
   
                return true;
            }
            catch (OdbcException sqlEx)
            {
                // Fehler beim Datenbankzugriff abfangen
                Console.WriteLine("SQL Fehler: " + sqlEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                // Allgemeine Fehler abfangen
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
                return false;
            }
        }

        private void btn_EingneuerTyp_Click(object sender, EventArgs e)
        {
            Form_GebaeudetypNeu frm = new Form_GebaeudetypNeu();
            
            frm.ShowDialog();
            if (frm.result == DialogResult.OK)
            {
                if (listBox_Typename.FindString(frm.m_szName) != -1) { MessageBox.Show("Name existiert bereits!"); return; }    

                OdbcCommand DBCommand;
                DBCommand = Program.DBConnection.CreateCommand();
                DBCommand.CommandText = "INSERT INTO DBTagV (Name, Beschreibung, Veraenderbar) SELECT '" + frm.m_szName + "' AS Ausdr1, '" + frm.m_szBeschreibung + "' AS Ausdr2, true as Ausdr3";
                
                try
                {
                    DBCommand.ExecuteNonQuery();
                }
                catch (SystemException ex)
                {
                    Console.WriteLine("Fehler beim Speichern: " + ex.Message);
                    MessageBox.Show("Speichern nicht möglich!");  
                    return;
                }
                
                RecordSet rs = new RecordSet();
                rs.Open("select ID from DBTagV where Name='" + frm.m_szName + "'");
                rs.Next();
                int nID = (int)rs.Read("ID");

                for (int i = 0; i < 192; i++)
                {
                    DBCommand.CommandText = "INSERT INTO DBTagVDaten (ID_TagV, Verteilung) SELECT " + nID + " AS Ausdr1,0 AS Ausdr2";
                    try
                    {
                        DBCommand.ExecuteNonQuery();
                    }
                    catch(SystemException ex)
                    {
                        MessageBox.Show("Speichern nicht möglich!" + ex.ToString());
                        return;
                    }
                }

                listBox_Typename.Items.Add(frm.m_szName);
                listBox_Typename.SelectedIndex = listBox_Typename.Items.Count - 1;     
            }
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (listBox_Typename.SelectedIndex == -1) { MessageBox.Show("Gebäudetyp auswählen!"); return; }

            OdbcCommand DBCommand;
            DBCommand = Program.DBConnection.CreateCommand();
            DBCommand.CommandText = "DELETE DBTagV.ID FROM DBTagV WHERE DBTagV.ID=" + ID_TagV;

            try
            {
                DBCommand.ExecuteNonQuery();
            }
            catch (SystemException ex)
            {
                Console.WriteLine("Fehler beim Löschen: " + ex.Message);
                MessageBox.Show("Löschen nicht möglich!");
                return;
            }
            listBox_Typename.Items.Remove(listBox_Typename.Text);   
        }

        private void st1_Validating(object sender, CancelEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (!Program.checkDouble(tb, tb.Text)) tb.Undo();
        }

    }
}