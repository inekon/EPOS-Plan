using System;
using System.Data;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form_EingStromTyp : Form
    {
        public double[,] arr = new double[7, 24];
        private double[] arr_seriell = new double[168];
        private ChartManager _chartManager;

        public Form_EingStromTyp()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt

            // Diagramm-Darstellung ueber den ChartManager (einmalige Grundkonfiguration).
            _chartManager = new ChartManager(chart1);
            _chartManager.XAxisAsNumber = true;    // X = Wochenstunden 1..168 (kein Datum, kein 8760)
            _chartManager.AreaLine = true;         // Flaechendiagramm wie zuvor
            _chartManager.MitLegende = false;      // nur eine Serie
            _chartManager.WheelZoomed = false;     // Mausrad-Zoom ist auf 8760 h ausgelegt -> hier aus
            _chartManager.MaxXVALUE = 168;
            _chartManager.YMinValue = 0;
            _chartManager.XAxisTitle = "Wochenstunde (1..168)";
            _chartManager.YAxisTitle = "Verteilung";
            _chartManager.ChartTitle = "";
            _chartManager.toolTipUnit = "";
        }

        // Baut das Diagramm ueber den ChartManager neu auf und passt die Y-Skalierung an die Werte an.
        private void ChartAktualisieren()
        {
            double max = (arr_seriell != null && arr_seriell.Length > 0) ? arr_seriell.Max() : 0;
            _chartManager.YMaxValue = (max > 0 ? max : 1) * 1.1;   // 0 -> ChartManager wuerde 100 annehmen
            _chartManager.Init();                          // Achsen/Stil neu setzen (leert die Serien)

            float[] werte = new float[arr_seriell.Length];
            for (int i = 0; i < arr_seriell.Length; i++) werte[i] = (float)arr_seriell[i];
            _chartManager.AddSeries("Stromverbrauch", Color.FromArgb(100, Color.Blue), werte);

            // Numerische X-Achse auf die Wochenstunden begrenzen (ChartManager-Standard waere 8760 h).
            Axis xAchse = _chartManager._chart.ChartAreas[0].AxisX;
            xAchse.Minimum = 0;
            xAchse.Maximum = arr_seriell.Length;   // 168
            xAchse.Interval = 24;                  // Tagesgrenzen
            _chartManager._chart.Invalidate();
        }

        public void SetControls()
        {
            // Abfrage über das DataRepository holen
            string sql = "SELECT * FROM Tab_Stromverbrauchertyp_STAMM ORDER BY Typname";
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
                listBox_Typname.SelectedIndex = 0; // loest SelectedIndexChanged -> ChartAktualisieren()
            }
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

            string sql = "SELECT * FROM Tab_Stromverbrauchertyp_STAMM WHERE Typname = ?";
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

            ChartAktualisieren();

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

            // Erst alle 24 Stundenwerte pruefen, dann uebernehmen (Folgepaket zu
            // ab5bf32): bei ungueltiger oder leerer Eingabe meldet der Helfer
            // sprechend, der Dialog bleibt offen und der Tag bleibt unveraendert.
            TextBox[] felder = new TextBox[24];
            double[] werte = new double[24];
            for (int stunde = 0; stunde < 24; stunde++)
            {
                felder[stunde] = tabPage1.Controls["st" + (stunde + 1).ToString()] as TextBox;
                if (felder[stunde] == null) continue;

                if (!Program.ZahlPruefen(felder[stunde], "Stundenwert " + (stunde + 1).ToString(), out werte[stunde])) return;
            }

            for (int stunde = 0; stunde < 24; stunde++)
            {
                if (felder[stunde] == null) continue;

                arr[Tag, stunde] = werte[stunde];
                arr_seriell[Tag * 24 + stunde] = werte[stunde];
            }
            pictureBox1.Visible = true;
            pictureBox1.Refresh();
            Thread.Sleep(500);
            pictureBox1.Visible = false;
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Typname.Text)) return;
            if (StromverbraucherStammCtrl.TypIsReadOnly(listBox_Typname.Text))
            {
                MessageBox.Show("Dieser Typ ist schreibgeschuetzt und kann nicht geaendert werden.", "Hinweis");
                return;
            }

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

            ChartAktualisieren();
        }

        private bool Update(string szBeschreibung, string szTyp)
        {
            string sql = "UPDATE Tab_Stromverbrauchertyp_STAMM SET Beschreibung = ? WHERE Typname = ?";
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
            string sql = $"UPDATE Tab_Stromverbrauchertyp_STAMM SET [{feld}] = ? WHERE Typname = ?";

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

            // TypDelete prueft selbst auf ReadOnly und meldet ggf.
            if (!StromverbraucherStammCtrl.TypDelete(listBox_Typname.Text)) return;

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

            if (StromverbraucherStammCtrl.TypNew(frm.m_szName) <= 0) return;

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

            if (StromverbraucherStammCtrl.TypNew(frm.m_szName) <= 0) return;

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

    }
}
