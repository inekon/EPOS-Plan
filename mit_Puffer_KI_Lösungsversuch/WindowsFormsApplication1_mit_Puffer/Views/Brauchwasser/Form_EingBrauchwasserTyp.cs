using System;
using System.Data.Odbc;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form_EingBrauchwasserTyp : Form
    {
        public double[,] arr = new double[7, 24];
        private double[] arr_seriell = new double[168];
        OdbcCommand DBCommand = Program.DBConnection.CreateCommand();

        public Form_EingBrauchwasserTyp()
        {
            InitializeComponent();
            chart1.ChartAreas[0].AxisY.Minimum = 0;
            chart1.Series[0].BorderWidth = 2;
        }

        public void SetControls()
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Brauchwassertyp_STAMM order by Bezeichner");
            listBox_Typname.Items.Clear();  
            while (rs.Next())
            {
                listBox_Typname.Items.Add(rs.Read("Bezeichner"));
                DatenEinlesen(rs);
            }
            rs.Close();
            listBox_Typname.SelectedIndex = 0;

            init_Chart(chart1);
        }

        private void Tagesdaten(string szTyp, int Tag)
        {
            for (int stunde = 0; stunde < 24; stunde++)
            {
                string ctrl_name = "st" + (stunde + 1).ToString();
                Control ctrl = tabPage1.Controls[ctrl_name];
                ctrl.Text = arr[Tag, stunde].ToString("F4");
            }
        }

        private void listBox_Typname_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            rs.Open("select * from Tab_Brauchwassertyp_STAMM where Bezeichner='" + listBox_Typname.Text + "'");
            
            if(rs.Next())
            {
                DatenEinlesen(rs);

                Object obj = rs.Read("Beschreibung");
                if (!DBNull.Value.Equals(obj))
                    textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                else
                    textBox_Beschreibung.Text = "";
            }
            rs.Close();

            chart1.Series[0].Points.DataBindY(arr_seriell); 

            listBox_Tag.ClearSelected(); 
            listBox_Tag.SelectedIndex = 0;   
        }

        private void listBox_Tag_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(listBox_Tag.SelectedIndex == -1) return;
             Tagesdaten(listBox_Typname.Text, listBox_Tag.SelectedIndex); 
        }

        private void DatenEinlesen(RecordSet rs)
        {
            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    arr[Tag, stunde] = (double)rs.Read(Tag * 24 + stunde + 3);
                    arr_seriell[Tag*24+stunde] = arr[Tag, stunde];
                }
            }
        }

        private void btn_WocheUebernehmen_Click(object sender, EventArgs e)
        {
            int Tag = listBox_Tag.SelectedIndex;  

            for (int stunde = 0; stunde < 24; stunde++)
            {
                string szval = tabPage1.Controls["st" + (stunde + 1).ToString()].Text;
                Control ctrl = tabPage1.Controls["st" + (stunde + 1).ToString()];
                if(!Program.checkDouble(ctrl, szval)) return;
                double dval = double.Parse(szval);
                arr[Tag, stunde] = dval;
                arr_seriell[Tag * 24 + stunde] = dval;
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (BrauchwasserStammCtrl.TypIsReadOnly(listBox_Typname.Text))
            {
                MessageBox.Show("Dieser Typ ist schreibgeschuetzt und kann nicht geaendert werden.", "Hinweis");
                return;
            }
            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    if (!update(listBox_Typname.Text, (Tag * 24 + stunde + 1).ToString(), arr[Tag, stunde])) return;  
                }
            }
            update(textBox_Beschreibung.Text,listBox_Typname.Text);
            MessageBox.Show("Datensatz gespeichert!");
            chart1.Series[0].Points.DataBindY(arr_seriell); 
        }

        private bool update(string szBeschreibung, string szTyp)
        {
            DBCommand.CommandText = "UPDATE Tab_Brauchwassertyp_STAMM SET Beschreibung='" + szBeschreibung + "' WHERE Bezeichner='" + szTyp + "'";
            try
            {
                DBCommand.ExecuteNonQuery();
            }
            catch
            {
                MessageBox.Show("Aktualisieren nicht möglich!");
                return false;
            }
            return true;
        }

        private bool update(string typ, string feld, double value)
        {
            DBCommand.CommandText = "UPDATE Tab_Brauchwassertyp_STAMM SET [" + feld + "]=" + value.ToString(CultureInfo.CreateSpecificCulture("en-US")) + " WHERE Bezeichner='" + typ + "'";
            try
            {
                DBCommand.ExecuteNonQuery();
            }
            catch
            {
                MessageBox.Show("Aktualisieren nicht möglich!");
                return false;
            }
            return true;
        }

        private void btn_Schliessen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_Typname.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;
            // TypDelete prueft selbst auf ReadOnly und meldet ggf.
            if (!BrauchwasserStammCtrl.TypDelete(listBox_Typname.Text)) return;
            SetControls();

        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();
            Point p1 = btn_Neu.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;
            frm.ShowDialog();

            if (frm.ShowDialog() == DialogResult.Cancel) return;

            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    arr[Tag, stunde] = 0;
                    arr_seriell[Tag * 24 + stunde] = 0; 
                }
            }
            if (BrauchwasserStammCtrl.TypNew(frm.m_szName) <= 0) return;

            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    if (!update(frm.m_szName, (Tag * 24 + stunde + 1).ToString(), arr[Tag, stunde])) return;
                }
            }
          
            update("", frm.m_szName);
            SetControls();
            listBox_Typname.Text = frm.m_szName;  
        }

        private void btn_SpeichernUnter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();

            Point p1 = btn_SpeichernUnter.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;
            if (frm.ShowDialog() == DialogResult.Cancel) return;

            if (BrauchwasserStammCtrl.TypNew(frm.m_szName) <= 0) return;

            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    update(frm.m_szName, (Tag * 24 + stunde + 1).ToString(), arr[Tag, stunde]);
                }
            }

            update(textBox_Beschreibung.Text, frm.m_szName);
            SetControls();
            listBox_Typname.Text = frm.m_szName;  
        }

        private void init_Chart(Chart chart)
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

            chart.Series[0].ChartType = SeriesChartType.Area;
            chart.Series[0].Color = Color.FromArgb(100, Color.Blue);

        }


    }
}
