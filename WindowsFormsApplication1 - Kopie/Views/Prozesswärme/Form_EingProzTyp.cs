using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form_EingProzTyp : Form
    {
        public double[,] arr = new double[7, 24];
        private double[] arr_seriell = new double[168];

        public Form_EingProzTyp()
        {
            InitializeComponent();
            chart1.ChartAreas[0].AxisY.Minimum = 0;
            chart1.Series[0].BorderWidth = 2;
        }

        public void SetControls()
        {
            listBox_Typname.Items.Clear();

            string sql = "SELECT * FROM Tab_Prozesstyp ORDER BY Typname";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    listBox_Typname.Items.Add(row["Typname"]?.ToString());
                }

                // Daten des ersten Elements temporär einlesen
                DatenEinlesenVonRow(dt.Rows[0]);
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
                    ctrl.Text = arr[Tag, stunde].ToString();
                }
            }
        }

        private void listBox_Typname_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Typname.Text)) return;

            string sql = "SELECT * FROM Tab_Prozesstyp WHERE Typname = ?";
            OleDbParameter[] ps = { new OleDbParameter("@typ", listBox_Typname.Text) };
            DataTable dt = DataRepository.GetDataTable(sql, ps);

            if (dt != null && dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                DatenEinlesenVonRow(row);

                if (row["Beschreibung"] != DBNull.Value)
                    textBox_Beschreibung.Text = row["Beschreibung"].ToString();
                else
                    textBox_Beschreibung.Text = "";
            }

            chart1.Series[0].Points.DataBindY(arr_seriell);

            listBox_Tag.ClearSelected();
            listBox_Tag.SelectedIndex = 0;
        }

        private void listBox_Tag_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox_Tag.SelectedIndex == -1) return;
            Tagesdaten(listBox_Typname.Text, listBox_Tag.SelectedIndex);
        }

        private void DatenEinlesenVonRow(DataRow row)
        {
            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    // Index 3 entspricht der vierten Spalte (nach Typname, Beschreibung etc.)
                    int columnIndex = Tag * 24 + stunde + 3;

                    if (columnIndex < row.Table.Columns.Count && row[columnIndex] != DBNull.Value)
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
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Typname.Text)) return;

            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    using (OleDbTransaction trans = conn.BeginTransaction())
                    {
                        // 1. Alle Stundenwerte aktualisieren (Gebündelt in einer Transaktion für max. Performance)
                        for (int Tag = 0; Tag < 7; Tag++)
                        {
                            for (int stunde = 0; stunde < 24; stunde++)
                            {
                                string feldName = (Tag * 24 + stunde + 1).ToString();
                                if (!UpdateWert(conn, trans, listBox_Typname.Text, feldName, arr[Tag, stunde]))
                                {
                                    trans.Rollback();
                                    return;
                                }
                            }
                        }

                        // 2. Beschreibung aktualisieren
                        if (!UpdateBeschreibung(conn, trans, textBox_Beschreibung.Text, listBox_Typname.Text))
                        {
                            trans.Rollback();
                            return;
                        }

                        trans.Commit();
                        MessageBox.Show("Daten erfolgreich gespeichert.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Massenspeichern: " + ex.Message);
                MessageBox.Show("Fehler beim Speichern der Daten!");
            }

            chart1.Series[0].Points.DataBindY(arr_seriell);
        }

        private bool UpdateBeschreibung(OleDbConnection conn, OleDbTransaction trans, string szBeschreibung, string szTyp)
        {
            string sql = "UPDATE Tab_Prozesstyp SET Beschreibung = ? WHERE Typname = ?";
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = sql;
                cmd.Parameters.Add(new OleDbParameter("@bes", szBeschreibung ?? (object)DBNull.Value));
                cmd.Parameters.Add(new OleDbParameter("@typ", szTyp));
                cmd.ExecuteNonQuery();
            }
            return true;
        }

        private bool UpdateWert(OleDbConnection conn, OleDbTransaction trans, string typ, string feld, double value)
        {
            // Feldnamen in eckige Klammern setzen, da reine Nummern (z.B. [1]) sonst SQL-Syntaxfehler erzeugen
            string sql = $"UPDATE Tab_Prozesstyp SET [{feld}] = ? WHERE Typname = ?";
            using (OleDbCommand cmd = conn.CreateCommand())
            {
                cmd.Transaction = trans;
                cmd.CommandText = sql;
                cmd.Parameters.Add(new OleDbParameter("@val", value));
                cmd.Parameters.Add(new OleDbParameter("@typ", typ));
                cmd.ExecuteNonQuery();
            }
            return true;
        }

        private void btn_Schliessen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Typname.Text)) return;

            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_Typname.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            string sql = "DELETE FROM Tab_Prozesstyp WHERE Typname = ?";
            OleDbParameter[] ps = { new OleDbParameter("@typ", listBox_Typname.Text) };

            if (DataRepository.ExecuteSQL(sql, ps))
            {
                MessageBox.Show("Datensatz gelöscht.");
                SetControls();
            }
            else
            {
                MessageBox.Show("Löschen nicht möglich!");
            }
        }

        private void btn_Neu_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();
            Point p1 = btn_Neu.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;

            // BUGFIX: frm.ShowDialog() darf hier nur einmal ausgewertet werden
            if (frm.ShowDialog() == DialogResult.Cancel || string.IsNullOrEmpty(frm.m_szName)) return;

            // Arrays zurücksetzen
            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    arr[Tag, stunde] = 0;
                    arr_seriell[Tag * 24 + stunde] = 0;
                }
            }

            // Datensatz anlegen
            string insertSql = "INSERT INTO Tab_Prozesstyp ( Typname ) VALUES (?)";
            OleDbParameter[] ps = { new OleDbParameter("@typ", frm.m_szName) };

            if (DataRepository.ExecuteSQL(insertSql, ps))
            {
                // Alle Stundenwerte über den Transaktions-Speicherer initialisieren
                try
                {
                    using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                    {
                        conn.Open();
                        using (OleDbTransaction trans = conn.BeginTransaction())
                        {
                            for (int Tag = 0; Tag < 7; Tag++)
                            {
                                for (int stunde = 0; stunde < 24; stunde++)
                                {
                                    string feldName = (Tag * 24 + stunde + 1).ToString();
                                    UpdateWert(conn, trans, frm.m_szName, feldName, 0);
                                }
                            }
                            UpdateBeschreibung(conn, trans, "", frm.m_szName);
                            trans.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler beim Initialisieren des neuen Datensatzes: " + ex.Message);
                }

                SetControls();
                listBox_Typname.Text = frm.m_szName;
            }
        }

        private void btn_SpeichernUnter_Click(object sender, EventArgs e)
        {
            Form_Sp_ItemNeu frm = new Form_Sp_ItemNeu();
            Point p1 = btn_SpeichernUnter.Location;
            p1 = this.PointToScreen(p1);
            frm.Location = p1;

            // BUGFIX: Nur ein Aufruf von ShowDialog()
            if (frm.ShowDialog() == DialogResult.Cancel || string.IsNullOrEmpty(frm.m_szName)) return;

            string insertSql = "INSERT INTO Tab_Prozesstyp ( Typname ) VALUES (?)";
            OleDbParameter[] ps = { new OleDbParameter("@typ", frm.m_szName) };

            if (DataRepository.ExecuteSQL(insertSql, ps))
            {
                try
                {
                    using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                    {
                        conn.Open();
                        using (OleDbTransaction trans = conn.BeginTransaction())
                        {
                            for (int Tag = 0; Tag < 7; Tag++)
                            {
                                for (int stunde = 0; stunde < 24; stunde++)
                                {
                                    string feldName = (Tag * 24 + stunde + 1).ToString();
                                    UpdateWert(conn, trans, frm.m_szName, feldName, arr[Tag, stunde]);
                                }
                            }
                            UpdateBeschreibung(conn, trans, textBox_Beschreibung.Text, frm.m_szName);
                            trans.Commit();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fehler bei Speichern Unter: " + ex.Message);
                }

                SetControls();
                listBox_Typname.Text = frm.m_szName;
            }
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