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


    public partial class Form_Klimadaten : Form
    {
        private string m_szExcelBasName;
        double ChartSelBegin;
        double ChartSelEnd;

        public Form_Klimadaten()
        {
            InitializeComponent();
            m_szExcelBasName = "";
        }

        private void Form_Klimadaten_Load(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Normal;
            KlimaregionCtrl krclass = new KlimaregionCtrl();
            krclass.ReadAll();
            krclass.FillListBox(listBoxKlimreg);
            krclass = null;
            
            chart1.Series.Clear();
            var ca = chart1.ChartAreas[0];
            ca.CursorX.IsUserEnabled = false;
            ca.CursorX.IsUserSelectionEnabled = false;
            ca.CursorY.IsUserEnabled = false;
            ca.CursorY.IsUserSelectionEnabled = false;
            ca.AxisY.ScaleView.Zoomable = true;
            ca.AxisX.ScaleView.Zoomable = true;
            ca.CursorX.AutoScroll = true;
            ca.CursorX.IsUserSelectionEnabled = true;
        }

        private void butt_WP_Click(object sender, EventArgs e)
        {
            KlimaregionCtrl krclass = new KlimaregionCtrl();
            krclass.ReadAll();
            krclass.FillListBox(listBoxKlimreg);
            krclass = null;
        }

        private void listBoxWP_SelectedIndexChanged(object sender, EventArgs e)
        {
            m_szExcelBasName = listBoxKlimreg.Text;
            CreateChart();
        }

        private void CreateChart()
        {
            KlimaregionCtrl ctrlregion = new KlimaregionCtrl();
            ctrlregion.ReadSingle("Select * from Tab_Klimaregion where Name = '" + m_szExcelBasName + "'");
            int ID_Klimaregion = ctrlregion.m_ID_Klimaregion;

            KlimadatenCtrl ctrl = new KlimadatenCtrl();
            ctrl.ReadAll(ID_Klimaregion);

            var series = new Series("Jahrestemperatur");
            chart1.Series.Clear();

            List<int> xAxis = new List<int>();
            List<Double> yAxis = new List<Double>(); 

            yAxis = ctrl.list_Temperatur;
            xAxis = ctrl.list_Tag;  
            series.Points.DataBindXY(xAxis, yAxis);
            chart1.Series.Add(series);
            series.ChartType = SeriesChartType.Line;
            chart1.Series[0].SmartLabelStyle.AllowOutsidePlotArea = LabelOutsidePlotAreaStyle.Yes;
            chart1.Series[0].SmartLabelStyle.IsMarkerOverlappingAllowed = false;
            chart1.Series[0].SmartLabelStyle.MovingDirection = LabelAlignmentStyles.Bottom;
            chart1.Series[0].IsValueShownAsLabel = false;
            chart1.Series[0].BorderWidth = 2;  
            chart1.Update();
            series = null;
        }

        private void butt_Delete_Click(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            if (listBoxKlimreg.SelectedIndex != -1)
            {
                //ist diese Klimaregion einem Projekt zugeordnet?
                string sql = "SELECT Tab_Projekt.Projektname, Tab_Klimaregion.Name " +
                            "FROM Tab_Klimaregion INNER JOIN Tab_Projekt ON " +
                            "Tab_Klimaregion.ID_Klimaregion = Tab_Projekt.ID_Klimaregion where " +
                            "Tab_Klimaregion.Name ='" + listBoxKlimreg.Text + "'";

                rs.Open(sql);

                if (rs.Next())
                {
                    MessageBox.Show("Löschen nicht möglich!\nDiese Klimaregion ist dem Projekt " + rs.Read("Projektname")+ " zugeordnet!", "Hinweis");
                    return;
                }

                KlimaregionCtrl krclass = new KlimaregionCtrl();
                krclass.Delete(listBoxKlimreg.Text);
                krclass.ReadAll();
                krclass.FillListBox(listBoxKlimreg);
                krclass = null;
            }
            else
            {
                MessageBox.Show("Klimaregion auswählen", "Hinweis");
            }
        }

        private void btn_Import_Click(object sender, EventArgs e)
        {
            string file = textBox_Excel.Text;
            
            if (textBox_Excel.Text != "")
            {
                ToolsClass ctrl = new ToolsClass();

                if (!ctrl.Exist(m_szExcelBasName))
                {
                    this.Cursor = Cursors.WaitCursor;

                    ctrl.OpenExcel(textBox_Excel.Text, "Klima", 6, 370, 4, 11);

                    KlimaregionCtrl ctrlklimareg = new KlimaregionCtrl();
                    ctrlklimareg.Add(m_szExcelBasName);

                    KlimadatenCtrl ctrlklimadat = new KlimadatenCtrl();
                    ctrlklimadat.Insert(ctrlklimareg.m_ID_Klimaregion, ctrl.excelList);

                    ctrlklimareg.ReadAll();
                    ctrlklimareg.FillListBox(listBoxKlimreg);
                    listBoxKlimreg.SelectedIndex = listBoxKlimreg.FindString(m_szExcelBasName);   
                    ctrlklimareg = null;

                    this.Cursor = Cursors.Default;
                }
                else MessageBox.Show("Daten sind bereits importiert!");
            }
        }

        private void btn_Beenden_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_ExcelDatei_Click(object sender, EventArgs e)
        {
            FileDlgClass file = new FileDlgClass();
            file.default_folder = "Klima";
            file.Show();
            textBox_Excel.Text = file.filename;
            m_szExcelBasName = file.filebasename;
        }

        private void SelectionRangeBegin(object sender, CursorEventArgs e)
        {
            ChartSelBegin = chart1.ChartAreas[0].CursorX.SelectionStart;
            ChartSelEnd = chart1.ChartAreas[0].CursorX.SelectionEnd;
        }

        private void SelectionRangeEnd(object sender, CursorEventArgs e)
        {
            //MessageBox.Show("start:" + x1.ToString() + " end:" + x2.ToString() );
            if (!Program.HasValue(ChartSelBegin)) return;
            
            DateTime date = new DateTime(DateTime.Now.Year, 1, 1);
            date = date.AddDays(ChartSelBegin - 1);

            DateTime date2 = new DateTime(DateTime.Now.Year, 1, 1);
            date2 = date2.AddDays(ChartSelEnd - 1);
        
            TextAnnotation ta2 = new TextAnnotation();
            ta2.Text = date.ToString("dd.MMMM") + " bis " + date2.ToString("dd.MMMM");
            ta2.AnchorX = 18 ;  // % of chart width
            ta2.AnchorY = 98;  // % of chart height, from top
            if (chart1.Annotations.Count == 0)
            {
                chart1.Annotations.Add(ta2);
            }
            else
            {
                chart1.Annotations[0] = ta2;
            }
        }

        private void AxisScrollBarClicked(object sender, ScrollBarEventArgs e)
        {
            if (e.ButtonType == ScrollBarButtonType.ZoomReset)
            {
                chart1.Annotations.Clear();
            }
            else
            {
                ChartSelBegin = chart1.ChartAreas[0].AxisX.ScaleView.ViewMinimum;
                ChartSelEnd = chart1.ChartAreas[0].AxisX.ScaleView.ViewMaximum;
                DateTime date = new DateTime(2025, 1, 1);
                date = date.AddDays(ChartSelBegin - 1);

                DateTime date2 = new DateTime(2025, 1, 1);
                date2 = date2.AddDays(ChartSelEnd - 1);

                TextAnnotation ta2 = new TextAnnotation();
                ta2.Text = date.ToString("dd.MMMM") + " bis " + date2.ToString("dd.MMMM");
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
            }
        }

 
    }

}
