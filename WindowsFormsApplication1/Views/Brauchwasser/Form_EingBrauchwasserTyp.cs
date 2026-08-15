using System;
using System.Data.OleDb;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class Form_EingBrauchwasserTyp : Form
    {
        public double[,] arr = new double[7, 24];
        private double[] arr_seriell = new double[168];
        private ChartManager _chartManager;

        public Form_EingBrauchwasserTyp()
        {
            InitializeComponent();

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
            _chartManager.AddSeries("Brauchwasser", Color.FromArgb(100, Color.Blue), werte);

            // Numerische X-Achse auf die Wochenstunden begrenzen (ChartManager-Standard waere 8760 h).
            Axis xAchse = _chartManager._chart.ChartAreas[0].AxisX;
            xAchse.Minimum = 0;
            xAchse.Maximum = arr_seriell.Length;   // 168
            xAchse.Interval = 24;                  // Tagesgrenzen
            _chartManager._chart.Invalidate();
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
            listBox_Typname.SelectedIndex = 0; // loest listBox_Typname_SelectedIndexChanged -> ChartAktualisieren()
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

            if (rs.Next())
            {
                DatenEinlesen(rs);

                Object obj = rs.Read("Beschreibung");
                if (!DBNull.Value.Equals(obj))
                    textBox_Beschreibung.Text = (string)rs.Read("Beschreibung");
                else
                    textBox_Beschreibung.Text = "";
            }
            rs.Close();

            ChartAktualisieren();

            listBox_Tag.ClearSelected();
            listBox_Tag.SelectedIndex = 0;
        }

        private void listBox_Tag_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox_Tag.SelectedIndex == -1) return;
            Tagesdaten(listBox_Typname.Text, listBox_Tag.SelectedIndex);
        }

        private void DatenEinlesen(RecordSet rs)
        {
            for (int Tag = 0; Tag < 7; Tag++)
            {
                for (int stunde = 0; stunde < 24; stunde++)
                {
                    arr[Tag, stunde] = (double)rs.Read(Tag * 24 + stunde + 3);
                    arr_seriell[Tag * 24 + stunde] = arr[Tag, stunde];
                }
            }
        }

        private void btn_WocheUebernehmen_Click(object sender, EventArgs e)
        {
            int Tag = listBox_Tag.SelectedIndex;

            // Folgepaket zu ab5bf32: erst alle 24 Felder pruefen, dann die geprueften
            // Werte uebernehmen. Beim ersten ungueltigen Feld meldet der Helfer
            // sprechend, setzt den Fokus und es wird nichts uebernommen - ein leeres
            // Feld bleibt wie bisher unzulaessig. Kein double.Parse mehr auf dem
            // Feldtext, damit "12.5" und "12,5" identisch als 12,5 ankommen.
            double[] werte = new double[24];
            for (int stunde = 0; stunde < 24; stunde++)
            {
                TextBox tb = tabPage1.Controls["st" + (stunde + 1).ToString()] as TextBox;
                if (!Program.ZahlPruefen(tb, "Stunde " + (stunde + 1).ToString(), out werte[stunde])) return;
            }

            for (int stunde = 0; stunde < 24; stunde++)
            {
                arr[Tag, stunde] = werte[stunde];
                arr_seriell[Tag * 24 + stunde] = werte[stunde];
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
            update(textBox_Beschreibung.Text, listBox_Typname.Text);
            MessageBox.Show("Datensatz gespeichert!");

            ChartAktualisieren();
        }

        private bool update(string szBeschreibung, string szTyp)
        {
            // OleDb ueber DataRepository, parametrisiert (kein String-Concat -> kein Quote-/Injection-Problem).
            // DataRepository zeigt bei einem Fehler bereits eine Meldung und liefert -1 zurueck.
            int n = DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Brauchwassertyp_STAMM SET Beschreibung = ? WHERE Bezeichner = ?",
                new OleDbParameter("?", szBeschreibung ?? ""),
                new OleDbParameter("?", szTyp ?? ""));
            return n >= 0;
        }

        private bool update(string typ, string feld, double value)
        {
            // Spaltenname (feld) ist ein Bezeichner und kann NICHT parametrisiert werden -> in eckige Klammern.
            // Wert + Bezeichner als Parameter: der Provider setzt den Dezimalpunkt korrekt (keine Kultur-Formatierung noetig).
            int n = DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Brauchwassertyp_STAMM SET [" + feld + "] = ? WHERE Bezeichner = ?",
                new OleDbParameter("?", value),
                new OleDbParameter("?", typ ?? ""));
            return n >= 0;
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

    }
}
