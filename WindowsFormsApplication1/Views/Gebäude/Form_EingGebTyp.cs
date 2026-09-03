using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
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
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            tt.Draw += new DrawToolTipEventHandler(this.tt_Draw);
            FensterEinpassung.Einhaengen(this);
        }

        public void SetControls()
        {
            TagVCtrl ctrl = new TagVCtrl();
            ctrl.ReadAll("select * from Tab_DBTagV_STAMM order by Bezeichner");
            for (int i = 0; i < ctrl.rows; i++)
            {
                listBox_Typename.Items.Add(ctrl.items[i].Name);  
            }
            if (listBox_Typename.Items.Count > 0) listBox_Typename.SelectedIndex = 0;
            listBox_Typename.Focus(); 
        }

        private void listBox_Typename_SelectedIndexChanged(object sender, EventArgs e)
        {
            RecordSet rs = new RecordSet();
            TagVCtrl ctrl = new TagVCtrl();

            if (listBox_Typename.Text == "") return;
            ctrl.ReadAll("select * from Tab_DBTagV_STAMM where Bezeichner='" + listBox_Typename.Text + "'");
            if (ctrl.rows == 0) return;
            textBox_Beschreibung.Text = ctrl.items[0].Beschreibung;
            textBox_Beschreibung.Select(0, 0); 

            string sql = "SELECT Count('Verteilung') AS Ausdr1 FROM Tab_DBTagVDaten_STAMM WHERE [ID_TagV]=" + ctrl.items[0].ID;
            rs.Open(sql);
            rs.Next();

            int anz = (int)rs.Read("Ausdr1");
            AnzalTagV = anz;
            ID_TagV = ctrl.items[0].ID;
            rs.Close();

            Verteilung = new double[anz, 24];

            rs.Open("select * from Tab_DBTagVDaten_STAMM where ID_TagV=" + ctrl.items[0].ID + " order by ID");
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

            if (listBox_Kurve.Items.Count > 0) listBox_Kurve.SelectedIndex = 0;
            _selected_index = 0;

            tt.OwnerDraw = true; 
            tt.BackColor = Color.LightYellow;
            tt.ForeColor = Color.Black;  
            if (ctrl.items[0].Veraenderbar && !ctrl.items[0].ReadOnly)
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
            // Der 5-Kurven-Typ (z.B. "Wohngebaeude VDI 2067") nutzt die kurze Namensliste,
            // alle anderen die lange. Entscheidung ueber die Kurvenanzahl (AnzalTagV/24),
            // NICHT ueber die Listenposition - die Liste ist alphabetisch sortiert.
            List<string> names = (AnzalTagV / 24 <= list_TagVName_0.Count) ? list_TagVName_0 : list_TagVName;
            if (index >= 0 && index < names.Count) return names[index];
            return "";
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

        // Stiller Weg (Aufrufer: Kurvenwechsel, Folgepaket zu ab5bf32): ein ungueltiges
        // oder leeres Feld laesst den bisherigen Wert stehen, statt beim Umschalten
        // eine FormatException zu werfen. Gemeldet wird erst am Speichern-Knopf.
        private void RefreshArrayValues()
        {
            for (int i = 0; i < 24; i++)
            {
                double wert;
                if (Program.ZahlParsen(groupBox1.Controls["st" + (i + 1).ToString()].Text, out wert))
                    Verteilung[_selected_index, i] = wert;
            }
        }

        /// <summary>
        /// Knopf-Variante von RefreshArrayValues: prueft erst alle 24 Stundenfelder und
        /// uebernimmt danach die geprueften Werte. Das erste ungueltige Feld meldet
        /// sprechend, bekommt den Fokus und liefert false - der Aufrufer kehrt zurueck
        /// und laesst den Dialog offen. Leer bleibt unzulaessig (frueher
        /// FormatException in RefreshArrayValues).
        /// </summary>
        private bool VerteilungUebernehmen()
        {
            double[] werte = new double[24];
            for (int i = 0; i < 24; i++)
            {
                TextBox tb = groupBox1.Controls["st" + (i + 1).ToString()] as TextBox;
                if (!Program.ZahlPruefen(tb, "Stunde " + (i + 1).ToString(), out werte[i])) return false;
            }

            for (int i = 0; i < 24; i++) Verteilung[_selected_index, i] = werte[i];
            return true;
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

            // Zahlen erst hier pruefen: bei ungueltiger Eingabe bleibt der Dialog
            // offen und es wird nichts geschrieben.
            if (!VerteilungUebernehmen()) return;
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
                // IDs der Verteilungszeilen in stabiler Reihenfolge laden und typisiert zurueckschreiben
                // (ersetzt den gegen Access nicht funktionierenden OdbcCommandBuilder).
                DataTable dt = DataRepository.GetDataTable(
                    "select ID from Tab_DBTagVDaten_STAMM where ID_TagV = ? order by ID",
                    new DbParam("@id", id_tagv));
                if (dt == null) return false;

                for (int n = 0; n < AnzalTagV / 24; n++)
                {
                    for (int i = 0; i < 24; i++)
                    {
                        int pos = n * 24 + i;
                        if (pos >= dt.Rows.Count) break;
                        int rowId = Convert.ToInt32(dt.Rows[pos]["ID"]);
                        DataRepository.ExecuteSQL(
                            "update Tab_DBTagVDaten_STAMM set Verteilung = ? where ID = ?",
                            new DbParam("@vv", DbParamTyp.Double) { Wert = Verteilung[n, i] },
                            new DbParam("@rid", DbParamTyp.Integer) { Wert = rowId });
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Speichern der Tagesverteilung: " + ex.Message);
                return false;
            }
        }

        private void btn_EingneuerTyp_Click(object sender, EventArgs e)
        {
            // iU9-W2.1: Bezeichner UND Beschreibung ueber NamensDialogHuelle;
            // Form_GebaeudetypNeu ist im selben Schritt geloescht (Regel M1).
            string szBeschreibung;
            string szName = NamensDialogHuelle.BezeichnerUndBeschreibung(this, out szBeschreibung);
            if (szName == null) return;

            if (listBox_Typename.FindString(szName) != -1)
            {
                MessageBox.Show("Name existiert bereits!");
                return;
            }

            try
            {
                // Kopf im Katalog Tab_DBTagV_STAMM anlegen (ID explizit, Veraenderbar=true, ReadOnly=false)
                int nID = DataRepository.GetMaxID("Tab_DBTagV_STAMM") + 1;
                string sqlInsertTyp = "INSERT INTO Tab_DBTagV_STAMM (ID, Bezeichner, Beschreibung, Veraenderbar, ReadOnly) VALUES (?, ?, ?, ?, ?)";
                DbParam[] paramsTyp = {
                    new DbParam("@nid", DbParamTyp.Integer) { Wert = nID },
                    new DbParam("@bez", DbParamTyp.VarWChar) { Wert = (object)szName },
                    new DbParam("@besch", DbParamTyp.VarWChar) { Wert = (object)szBeschreibung },
                    new DbParam("@ver", DbParamTyp.Boolean) { Wert = true },
                    new DbParam("@ro", DbParamTyp.Boolean) { Wert = false }
                };
                if (!DataRepository.ExecuteSQL(sqlInsertTyp, paramsTyp))
                {
                    MessageBox.Show("Speichern des Gebäudetyps fehlgeschlagen!");
                    return;
                }

                // 192 Verteilungs-Datensaetze im Katalog Tab_DBTagVDaten_STAMM anlegen (ID explizit)
                int nextDid = DataRepository.GetMaxID("Tab_DBTagVDaten_STAMM") + 1;
                string sqlInsertDaten = "INSERT INTO Tab_DBTagVDaten_STAMM (ID, ID_TagV, Verteilung, ReadOnly) VALUES (?, ?, ?, ?)";
                for (int i = 0; i < 192; i++)
                {
                    DbParam[] pd = {
                        new DbParam("@did", DbParamTyp.Integer) { Wert = nextDid++ },
                        new DbParam("@dtag", DbParamTyp.Integer) { Wert = nID },
                        new DbParam("@dv", DbParamTyp.Double) { Wert = 0.0 },
                        new DbParam("@dro", DbParamTyp.Boolean) { Wert = false }
                    };
                    if (!DataRepository.ExecuteSQL(sqlInsertDaten, pd))
                    {
                        MessageBox.Show($"Fehler beim Erstellen der Verteilungsdaten im Schritt {i + 1}!");
                        return;
                    }
                }

                listBox_Typename.Items.Add(szName);
                listBox_Typename.SelectedIndex = listBox_Typename.Items.Count - 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler beim Speichern: " + ex.Message);
                MessageBox.Show("Ein unerwarteter Fehler ist aufgetreten: " + ex.Message);
            }
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (listBox_Typename.SelectedIndex == -1) { MessageBox.Show("Gebäudetyp auswählen!"); return; }

            // Schutz: schreibgeschützte / vom Hersteller gelieferte Typen nicht löschen
            TagVCtrl chk = new TagVCtrl();
            chk.ReadAll("select * from Tab_DBTagV_STAMM where Bezeichner='" + listBox_Typename.Text + "'");
            if (chk.rows > 0 && (chk.items[0].ReadOnly || !chk.items[0].Veraenderbar))
            {
                MessageBox.Show("Dieser Gebäudetyp ist schreibgeschützt und kann nicht gelöscht werden.", "Hinweis");
                return;
            }

            // Detail vor Kopf löschen (typisiert statt fehlerhaftem DELETE-Statement)
            DataRepository.ExecuteSQL("DELETE FROM Tab_DBTagVDaten_STAMM WHERE ID_TagV = ?",
                new DbParam("@idt", ID_TagV));
            DataRepository.ExecuteSQL("DELETE FROM Tab_DBTagV_STAMM WHERE ID = ?",
                new DbParam("@idk", ID_TagV));

            listBox_Typename.Items.Remove(listBox_Typename.Text);
        }

        // Validating faerbt nur noch (Folgepaket zu ab5bf32, alle 24 Stundenfelder
        // haengen an diesem Handler): kein modales Melden und kein Undo() mehr,
        // geprueft wird am Speichern-Knopf.
        private void st1_Validating(object sender, CancelEventArgs e)
        {
            Program.ZahlFaerben(sender);
        }

    }
}
