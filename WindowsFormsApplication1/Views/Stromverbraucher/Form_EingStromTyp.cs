using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form_EingStromTyp : Form
    {
        public double[,] arr = new double[7, 24];
        private double[] arr_seriell = new double[168];

        public Form_EingStromTyp()
        {
            InitializeComponent();
            chart1.ChartAreas[0].AxisY.Minimum = 0;
            chart1.Series[0].BorderWidth = 2;
        }

        public void SetControls()
        {
            // Abfrage über das DataRepository holen
            string sql = "SELECT * FROM Tab_Stromverbrauchertyp ORDER BY Typname";
            DataTable dt = DataRepository.GetDataTable(sql);

            listBox_Typname.Items.Clear();

            if (dt != null)
            {
                foreach (DataRow row in dt.Rows)
                {
                    if (row["Typname"] != DBNull.Value)
                    {
                        listBox_Typname.Items.Add(row["Typname"].ToString());
                    }
                }
            }

            if (listBox_Typname.Items.Count > 0)
            {
                listBox_Typname.SelectedIndex = 0;
            }

            init_Chart(chart1);
        }

        private void Tagesdaten(string szTyp, int Tag)
        {
            for (int stunde = 0; stunde < 24; stunde++)
            {
                string ctrl_name = "st" + (stunde + 1).ToString();
                Control ctrl = tabPage1.Controls[ctrl_name];
                if (ctrl != null)
                {
                    ctrl.Text = arr[Tag, stunde].ToString("F2");
                }
            }
        }

        private void listBox_Typname_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Typname.Text)) return;

            string sql = "SELECT * FROM Tab_Stromverbrauchertyp WHERE Typname = ?";
            OleDbParameter parameter = new OleDbParameter("?", listBox_Typname.Text);
            DataTable dt = DataRepository.GetDataTable(sql, parameter);

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                DatenEinlesen(row);

                if (row["Beschreibung"] != DBNull.Value)
                    textBox_Beschreibung.Text = row["Beschreibung"].ToString();
                else
                    textBox_Beschreibung.Text = "";
            }

            chart1.Series[0].Points.DataBindY(arr_seriell);

            listBox_Tag.ClearSelected();
            if (listBox_Tag.Items.Count > 0)
            {
                listBox_Tag.SelectedIndex = 0;
            }
        }

        private void listBox_Tag_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox_Tag.SelectedIndex == -1) return;
            Tagesdaten(listBox_Typname.Text, listBox_Tag.SelectedIndex);
        }

        private void DatenEinlesen(DataRow row)
        {
            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    // Index 3 entspricht der vierten Spalte (Überspringen von ID, Typname, Beschreibung)
                    int columnIndex = Tag * 24 + stunde + 3;
                    if (row[columnIndex] != DBNull.Value)
                    {
                        arr[Tag, stunde] = Convert.ToDouble(row[columnIndex]);
                    }
                    else
                    {
                        arr[Tag, stunde] = 0;
                    }
                    arr_seriell[Tag * 24 + stunde] = arr[Tag, stunde];
                }
            }
        }

        private void btn_WocheUebernehmen_Click(object sender, EventArgs e)
        {
            int Tag = listBox_Tag.SelectedIndex;
            if (Tag == -1) return;

            for (int stunde = 0; stunde < 24; stunde++)
            {
                string ctrlName = "st" + (stunde + 1).ToString();
                Control ctrl = tabPage1.Controls[ctrlName];
                if (ctrl == null) continue;

                string szval = ctrl.Text;
                if (!Program.checkDouble(ctrl, szval)) return;

                double dval = double.Parse(szval);
                arr[Tag, stunde] = dval;
                arr_seriell[Tag * 24 + stunde] = dval;
            }
            pictureBox1.Visible = true;
            pictureBox1.Refresh();
            Thread.Sleep(500);
            pictureBox1.Visible = false;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Typname.Text)) return;

            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    string feldName = "_" + (Tag * 24 + stunde + 1).ToString(); // Access mag Spaltennamen, die nur aus Zahlen bestehen, oft nicht ohne Präfix/Klammern (z.B. [1]). Wenn deine Spalten in der DB exakt "1", "2" heißen, belasse es bei (Tag * 24 + stunde + 1).ToString()
                    string feld = (Tag * 24 + stunde + 1).ToString();

                    if (!Update(listBox_Typname.Text, feld, arr[Tag, stunde])) return;
                }
            }
            Update(textBox_Beschreibung.Text, listBox_Typname.Text);
            MessageBox.Show("Datensatz gespeichert!");
            chart1.Series[0].Points.DataBindY(arr_seriell);
        }

        private bool Update(string szBeschreibung, string szTyp)
        {
            string sql = "UPDATE Tab_Stromverbrauchertyp SET Beschreibung = ? WHERE Typname = ?";
            OleDbParameter[] parameters = new OleDbParameter[]
            {
                new OleDbParameter("?", szBeschreibung ?? (object)DBNull.Value),
                new OleDbParameter("?", szTyp)
            };

            try
            {
                DataRepository.ExecuteNonQuery(sql, parameters);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Aktualisieren der Beschreibung nicht möglich!");
                Console.WriteLine("Fehler beim Aktualisieren der Beschreibung: " + ex.Message);
                return false;
            }
            return true;
        }

        private bool Update(string typ, string feld, double value)
        {
            // Da Spaltennamen nicht parametrisiert werden können, betten wir den validierten Feldnamen direkt ein.
            // Die eckigen Klammern [ ] schützen rein numerische Spaltennamen (z.B. [1], [2]) unter Access/OleDb.
            string sql = $"UPDATE Tab_Stromverbrauchertyp SET [{feld}] = ? WHERE Typname = ?";

            OleDbParameter[] parameters = new OleDbParameter[]
            {
                new OleDbParameter("?", value),
                new OleDbParameter("?", typ)
            };

            try
            {
                DataRepository.ExecuteNonQuery(sql, parameters);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Aktualisieren des Stundenwerts nicht möglich!");
                Console.WriteLine($"Fehler beim Aktualisieren von Feld {feld}: " + ex.Message);
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
            if (string.IsNullOrEmpty(listBox_Typname.Text))
            {
                MessageBox.Show("Bitte wählen Sie zuerst einen Typnamen aus!");
                return;
            }

            DialogResult dialogResult = MessageBox.Show(
                $"Soll {listBox_Typname.Text} wirklich gelöscht werden ?",
                "Löschen",
                MessageBoxButtons.YesNo
            );

            if (dialogResult == DialogResult.No) return;

            try
            {
                string sql = "DELETE FROM Tab_Stromverbrauchertyp WHERE Typname = ?";
                OleDbParameter parameter = new OleDbParameter("?", listBox_Typname.Text);
                DataRepository.ExecuteNonQuery(sql, parameter);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Löschen nicht möglich!");
                Console.WriteLine("Fehler beim Löschen des Verbrauchertyps: " + ex.Message);
                return;
            }

            SetControls();
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();

            Point p1 = btn_Neu.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;

            // Fehler korrigiert: Im Original wurde ShowDialog() zweimal aufgerufen!
            if (frm.ShowDialog() == DialogResult.Cancel) return;

            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    arr[Tag, stunde] = 0;
                    arr_seriell[Tag * 24 + stunde] = 0;
                }
            }

            // Sicherer parametrisierter Insert-Befehl
            string sql = "INSERT INTO Tab_Stromverbrauchertyp ( Typname ) VALUES (?)";
            OleDbParameter parameter = new OleDbParameter("?", frm.m_szName);

            try
            {
                DataRepository.ExecuteNonQuery(sql, parameter);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Anlegen des Typs nicht möglich!");
                Console.WriteLine("Fehler beim Einfügen: " + ex.Message);
                return;
            }

            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    if (!Update(frm.m_szName, (Tag * 24 + stunde + 1).ToString(), arr[Tag, stunde])) return;
                }
            }

            Update("", frm.m_szName);
            SetControls();
            listBox_Typname.Text = frm.m_szName;
        }

        private void btn_SpeichernUnter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();

            Point p1 = btn_Neu.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;

            if (frm.ShowDialog() == DialogResult.Cancel) return;

            string sql = "INSERT INTO Tab_Stromverbrauchertyp ( Typname ) VALUES (?)";
            OleDbParameter parameter = new OleDbParameter("?", frm.m_szName);

            try
            {
                DataRepository.ExecuteNonQuery(sql, parameter);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Speichern Unter nicht möglich!");
                Console.WriteLine("Fehler beim Einfügen: " + ex.Message);
                return;
            }

            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    Update(frm.m_szName, (Tag * 24 + stunde + 1).ToString(), arr[Tag, stunde]);
                }
            }

            Update(textBox_Beschreibung.Text, frm.m_szName);
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