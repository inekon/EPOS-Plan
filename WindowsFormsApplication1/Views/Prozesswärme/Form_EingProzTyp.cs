using System;
using System.Data;
using System.Linq;
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
        private ChartManager _chartManager;

        public Form_EingProzTyp()
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
            _chartManager.AddSeries("Prozess", Color.FromArgb(100, Color.Blue), werte);

            // Numerische X-Achse auf die Wochenstunden begrenzen (ChartManager-Standard waere 8760 h).
            Axis xAchse = _chartManager._chart.ChartAreas[0].AxisX;
            xAchse.Minimum = 0;
            xAchse.Maximum = arr_seriell.Length;   // 168
            xAchse.Interval = 24;                  // Tagesgrenzen
            _chartManager._chart.Invalidate();
        }

        public void SetControls()
        {
            listBox_Typname.Items.Clear();

            string sql = "SELECT * FROM Tab_Prozesstyp_STAMM ORDER BY Bezeichner";
            DataTable dt = DataRepository.GetDataTable(sql, null);

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    listBox_Typname.Items.Add(row["Bezeichner"]?.ToString());
                }

                // Daten des ersten Elements temporär einlesen
                DatenEinlesenVonRow(dt.Rows[0]);
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
                    ctrl.Text = arr[Tag, stunde].ToString();
                }
            }
        }

        private void listBox_Typname_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Typname.Text)) return;

            string sql = "SELECT * FROM Tab_Prozesstyp_STAMM WHERE Bezeichner = ?";
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

            ChartAktualisieren();

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

            // Folgepaket zu ab5bf32: erst alle 24 Felder pruefen, dann die geprueften
            // Werte uebernehmen. Beim ersten ungueltigen Feld meldet der Helfer
            // sprechend, setzt den Fokus und es wird nichts uebernommen - ein leeres
            // Feld bleibt wie bisher unzulaessig. Kein double.Parse mehr auf dem
            // Feldtext, damit "12.5" und "12,5" identisch als 12,5 ankommen.
            TextBox[] felder = new TextBox[24];
            double[] werte = new double[24];
            for (int stunde = 0; stunde < 24; stunde++)
            {
                felder[stunde] = tabPage1.Controls["st" + (stunde + 1).ToString()] as TextBox;
                if (felder[stunde] == null) continue;
                if (!Program.ZahlPruefen(felder[stunde], "Stunde " + (stunde + 1).ToString(), out werte[stunde])) return;
            }

            for (int stunde = 0; stunde < 24; stunde++)
            {
                if (felder[stunde] == null) continue;
                arr[Tag, stunde] = werte[stunde];
                arr_seriell[Tag * 24 + stunde] = werte[stunde];
            }
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Typname.Text)) return;
            if (IsTypReadOnly(listBox_Typname.Text))
            {
                MessageBox.Show("Dieses Typ-Profil ist schreibgeschuetzt (ReadOnly) und kann nicht gespeichert werden.", "Schreibgeschuetzt");
                return;
            }

            try
            {
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    // 1. Alle Stundenwerte aktualisieren (Gebündelt in einer Transaktion für max. Performance)
                    for (int Tag = 0; Tag < 7; Tag++)
                    {
                        for (int stunde = 0; stunde < 24; stunde++)
                        {
                            string feldName = (Tag * 24 + stunde + 1).ToString();
                            if (!UpdateWert(v, listBox_Typname.Text, feldName, arr[Tag, stunde]))
                            {
                                v.Rollback();
                                return;
                            }
                        }
                    }

                    // 2. Beschreibung aktualisieren
                    if (!UpdateBeschreibung(v, textBox_Beschreibung.Text, listBox_Typname.Text))
                    {
                        v.Rollback();
                        return;
                    }

                    v.Commit();
                    MessageBox.Show("Daten erfolgreich gespeichert.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Massenspeichern: " + ex.Message);
                MessageBox.Show("Fehler beim Speichern der Daten!");
            }

            ChartAktualisieren();
        }

        private bool UpdateBeschreibung(DbVorgang v, string szBeschreibung, string szTyp)
        {
            string sql = "UPDATE Tab_Prozesstyp_STAMM SET Beschreibung = ? WHERE Bezeichner = ?";
            v.Ausfuehren(sql,
                new OleDbParameter("@bes", szBeschreibung ?? (object)DBNull.Value),
                new OleDbParameter("@typ", szTyp));
            return true;
        }

        private bool UpdateWert(DbVorgang v, string typ, string feld, double value)
        {
            // Feldnamen in eckige Klammern setzen, da reine Nummern (z.B. [1]) sonst SQL-Syntaxfehler erzeugen
            string sql = $"UPDATE Tab_Prozesstyp_STAMM SET [{feld}] = ? WHERE Bezeichner = ?";
            v.Ausfuehren(sql,
                new OleDbParameter("@val", value),
                new OleDbParameter("@typ", typ));
            return true;
        }

        private void btn_Schliessen_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(listBox_Typname.Text)) return;

            if (IsTypReadOnly(listBox_Typname.Text))
            {
                MessageBox.Show("Dieses Typ-Profil ist schreibgeschuetzt (ReadOnly) und kann nicht geloescht werden.", "Schreibgeschuetzt");
                return;
            }

            DialogResult dialogResult = MessageBox.Show("Soll " + listBox_Typname.Text + " wirklich gelöscht werden ?", "Löschen", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.No) return;

            string sql = "DELETE FROM Tab_Prozesstyp_STAMM WHERE Bezeichner = ?";
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
            string insertSql = "INSERT INTO Tab_Prozesstyp_STAMM ( Bezeichner, ReadOnly ) VALUES (?, ?)";
            OleDbParameter[] ps = { new OleDbParameter("@typ", frm.m_szName), new OleDbParameter("@ro", false) };

            if (DataRepository.ExecuteSQL(insertSql, ps))
            {
                // Alle Stundenwerte über den Transaktions-Speicherer initialisieren
                try
                {
                    using (DbVorgang v = DataRepository.Vorgang())
                    {
                        for (int Tag = 0; Tag < 7; Tag++)
                        {
                            for (int stunde = 0; stunde < 24; stunde++)
                            {
                                string feldName = (Tag * 24 + stunde + 1).ToString();
                                UpdateWert(v, frm.m_szName, feldName, 0);
                            }
                        }
                        UpdateBeschreibung(v, "", frm.m_szName);
                        v.Commit();
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

            string insertSql = "INSERT INTO Tab_Prozesstyp_STAMM ( Bezeichner, ReadOnly ) VALUES (?, ?)";
            OleDbParameter[] ps = { new OleDbParameter("@typ", frm.m_szName), new OleDbParameter("@ro", false) };

            if (DataRepository.ExecuteSQL(insertSql, ps))
            {
                try
                {
                    using (DbVorgang v = DataRepository.Vorgang())
                    {
                        for (int Tag = 0; Tag < 7; Tag++)
                        {
                            for (int stunde = 0; stunde < 24; stunde++)
                            {
                                string feldName = (Tag * 24 + stunde + 1).ToString();
                                UpdateWert(v, frm.m_szName, feldName, arr[Tag, stunde]);
                            }
                        }
                        UpdateBeschreibung(v, textBox_Beschreibung.Text, frm.m_szName);
                        v.Commit();
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

        // Liefert true, wenn das Typ-Profil (per Bezeichner) schreibgeschuetzt ist.
        private bool IsTypReadOnly(string szBezeichner)
        {
            object v = DataRepository.ExecuteScalar(
                "SELECT ReadOnly FROM Tab_Prozesstyp_STAMM WHERE Bezeichner = ?",
                new OleDbParameter("@bez", szBezeichner ?? ""));
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

    }
}
