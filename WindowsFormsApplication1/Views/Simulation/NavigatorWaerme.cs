using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class NavigatorWaerme : UserControl, INavigatableContent
    {
        ChartManager _chartManager;
        SimulationControl sim;

        private float[] temp_profil;
        private float[] temp_wp;
        private float[] temp_hs;
        private float[] temp_hk;
        private float[] temp_st;
        private float[] temp_bhkw;
        private float[] temp_ges;

        // Alle Speicher des Laufs (Senken- und Quellspeicher) in stabiler Reihenfolge -
        // dieselbe Liste, die auch Ergebnis-Persistenz und Detailansicht speist
        // (Konzept 6.6/13.3, eine Quelle der Wahrheit).
        private List<SimulationPufferspeicher> speicherListe = new List<SimulationPufferspeicher>();

        // Technische Serienschlüssel (PUFFER_<ID> / QUELLE_<AnlagenID>) in derselben
        // Reihenfolge wie speicherListe. Der Anzeigetext steht ausschließlich in
        // Series.LegendText, damit die Umstellung nicht mit der Lokalisierung
        // kollidiert (Konzept 13.3; Lokalisierung des Bereichs = Paket 9).
        private List<string> speicherSchluessel = new List<string>();

        // Checkbox für den Speicherfüllstand (programmatisch, kein Designer nötig)
        private CheckBox checkBox_Puffer;

        // Auswahlliste der Speicher (13.3) - nur sichtbar, wenn es mehr als einen gibt.
        private ComboBox comboBox_Puffer;

        /// <summary>Farbfolge der Speicherserien (wiederholt sich bei vielen Speichern).</summary>
        private static readonly Color[] SPEICHER_FARBEN =
        {
            Color.MediumVioletRed, Color.DarkViolet, Color.Teal,
            Color.SaddleBrown, Color.DarkSlateGray, Color.Crimson
        };

        // --- Technische Serienschlüssel (Paket 9 / L6) --------------------------------
        //
        // Schicht 2 der Drei-Schichten-Regel: sprachneutral, ASCII, unveränderlich.
        // Sie sind der ZUGRIFFSSCHLÜSSEL auf die Chart-Serien; der angezeigte Text steht
        // ausschließlich in Series.LegendText und kommt aus dem Ressourcenkatalog.
        //
        // Vorher trugen die Serien ihre deutschen Anzeigenamen als Namen - und zwar
        // uneinheitlich: "Wärmebedarf" mit Umlaut, "Waermepumpe" ohne. Genau diese
        // Vermischung macht die Lokalisierung unmöglich, weil ein übersetzter Name
        // sämtliche ~30 Nachschlagestellen (Series["…"]) ins Leere laufen ließe.
        // Die Speicherserien tragen ihre technischen Schlüssel (PUFFER_<ID> /
        // QUELLE_<AnlagenID>) bereits seit Paket 7; hier wird das zu Ende geführt.
        private const string S_WAERMEBEDARF = "WAERMEBEDARF";
        private const string S_GESAMT = "GESAMT";
        private const string S_WAERMEPUMPE = "WAERMEPUMPE";
        private const string S_HEIZSTAB = "HEIZSTAB";
        private const string S_HEIZKESSEL = "HEIZKESSEL";
        private const string S_SOLARTHERMIE = "SOLARTHERMIE";
        private const string S_BHKW = "BHKW_WAERME";

        public NavigatorWaerme(SimulationControl simctrl)
        {
            InitializeComponent();
            BeschriftungenSetzen();
            InitPufferCheckBox();
            SetControl(sim = simctrl);
            InitCsvExportButton();
        }

        /// <summary>
        /// Setzt die im Designer angelegten Beschriftungen aus dem Ressourcenkatalog
        /// (Paket 9 / L7). Sie schließt den in Etappe 2 offen gebliebenen Punkt 3:
        /// die Serien-Checkboxen blieben bis dahin deutsch.
        ///
        /// <b>Bewusste Abweichung vom WinForms-Weg</b> (wie vom Auftraggeber entschieden):
        /// Eine <c>Localizable</c>-Ressource trüge je Kultur auch Position und Größe; ein
        /// Handumbau der Designer-.resx ohne den WinForms-Designer verschöbe
        /// Steuerelemente. Die Texte werden deshalb programmatisch gesetzt, die
        /// Designer-Fassung bleibt als deutsche Entwurfszeit-Vorbelegung stehen.
        ///
        /// <b>Reihenfolge beachten:</b> Der Aufruf steht VOR <see cref="InitPufferCheckBox"/>,
        /// weil dort <c>checkBox_BHKW.Right</c> und <c>checkBox_Waermebedarf.Right</c> die
        /// Position der programmatischen Steuerelemente bestimmen — die Breite hängt am
        /// Text.
        /// </summary>
        private void BeschriftungenSetzen()
        {
            checkBox_Gesamt.Text = MyResource.Resource.CHART_LEGENDE_GESAMT;
            checkBox_WP.Text = MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE;
            checkBox_Heizstab.Text = MyResource.Resource.CHART_SEGMENT_HEIZSTAB;
            checkBox_SPK.Text = MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL;
            checkBox_ST.Text = MyResource.Resource.SIM_ERZEUGERNAME_SOLARTHERMIE;
            checkBox_BHKW.Text = MyResource.Resource.SIM_ERZEUGERNAME_BHKW;
            checkBox_Waermebedarf.Text = MyResource.Resource.SIM_CHK_WAERMEBEDARF_EINBLENDEN;

            // Entwurfszeit-Titel des Charts. Er ist nur zu sehen, solange SetControl noch
            // nicht gelaufen ist - ChartManager.Init() ersetzt die Titelsammlung danach.
            if (chart_Waerme.Titles.Count > 0)
                chart_Waerme.Titles[0].Text = MyResource.Resource.CHART_TITEL_WAERMEPRODUKTION_JAHRESGANGLINIE;
        }

        /// <summary>
        /// Legt die Checkbox "Speicherfüllstand" und die Speicher-Auswahlliste neben den
        /// übrigen Serien-Checkboxen an (programmatisch, kein Designer nötig).
        /// Die Checkbox schaltet die Speicherserien gemeinsam ein und aus, die
        /// Auswahlliste schränkt bei mehreren Speichern auf einen einzelnen ein.
        /// </summary>
        private void InitPufferCheckBox()
        {
            checkBox_Puffer = new CheckBox();
            checkBox_Puffer.Name = "checkBox_Puffer";
            checkBox_Puffer.Text = MyResource.Resource.PSP_CHECKBOX_SPEICHERFUELLSTAND;
            checkBox_Puffer.AutoSize = true;
            checkBox_Puffer.Location = new Point(checkBox_BHKW.Right + 15, checkBox_BHKW.Top);
            checkBox_Puffer.CheckedChanged += checkBox_Puffer_CheckedChanged;
            this.Controls.Add(checkBox_Puffer);
            checkBox_Puffer.BringToFront();

            comboBox_Puffer = new ComboBox();
            comboBox_Puffer.Name = "comboBox_Puffer";
            comboBox_Puffer.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox_Puffer.Width = 220;
            // Zweite Checkbox-Zeile, rechts neben "Wärmebedarf einblenden" (dort ist
            // Platz frei). Die erste Zeile ist bis "BHKW" belegt; hinter der neuen
            // Checkbox "Speicherfüllstand" wären die 220 px der Liste über die rechte
            // Diagrammkante hinausgelaufen und die Auswahl damit unerreichbar gewesen.
            comboBox_Puffer.Location = new Point(checkBox_Waermebedarf.Right + 6,
                                                 checkBox_Waermebedarf.Top - 2);
            comboBox_Puffer.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            comboBox_Puffer.Visible = false;
            comboBox_Puffer.SelectedIndexChanged += comboBox_Puffer_SelectedIndexChanged;
            this.Controls.Add(comboBox_Puffer);
            comboBox_Puffer.BringToFront();
        }

        /// <summary>
        /// Legt den CSV-Export-Button rechts neben den Checkboxen an (programmatisch, kein Designer nötig).
        /// </summary>
        private void InitCsvExportButton()
        {
            Button btnExport = new Button();
            btnExport.Name = "btn_CsvExport";
            btnExport.Text = MyResource.Resource.SIM_BTN_CSV_EXPORT;
            btnExport.Size = new Size(110, 28);
            // Oberhalb des Diagramms rechtsbündig (rechte Kante = Diagrammkante),
            // damit die Checkbox-Zeile darunter frei bleibt.
            btnExport.Location = new Point(chart_Waerme.Right - btnExport.Width, 20);
            btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnExport.Click += btn_CsvExport_Click;
            this.Controls.Add(btnExport);
            btnExport.BringToFront();
        }

        /// <summary>
        /// Exportiert die aktuell per Checkbox selektierten Serien des Wärme-Charts als CSV
        /// (Zeitstempel, Außentemperatur, Werte — Stundenwerte).
        /// </summary>
        private void btn_CsvExport_Click(object sender, EventArgs e)
        {
            if (sim == null || sim.simulation_Waermebedarf == null || temp_ges == null)
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_KEINE_DATEN_SIMULATION,
                    MyResource.Resource.SIM_BTN_CSV_EXPORT,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // nur die aktuell selektierten (angezeigten) Serien exportieren
            List<CsvSpalte> spalten = new List<CsvSpalte>();
            if (checkBox_Gesamt.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_GESAMT, temp_ges));
            if (checkBox_WP.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMEPUMPE, temp_wp));
            if (checkBox_Heizstab.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_HEIZSTAB, temp_hs));
            if (checkBox_SPK.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_HEIZKESSEL, temp_hk));
            if (checkBox_ST.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_SOLARTHERMIE, temp_st));
            if (checkBox_BHKW.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_BHKW, temp_bhkw));

            // Je angezeigtem Speicher eine eigene Spalte, Bezeichner im Kopf (13.3).
            if (checkBox_Puffer != null && checkBox_Puffer.Checked)
                for (int i = 0; i < speicherListe.Count; i++)
                {
                    if (!SpeicherSichtbar(i)) continue;
                    spalten.Add(new CsvSpalte(
                        string.Format(MyResource.Resource.CHART_CSV_SPEICHERFUELLSTAND,
                                      SpeicherAnzeige(speicherListe[i])),
                        speicherListe[i].SOC_stuendlich));
                }

            CsvExportClass.Export(MyResource.Resource.CHART_DATEI_WAERMEPRODUKTION,
                sim.simulation_Waermebedarf.Stundentemperatur, spalten, false);
        }

        public void RefreshContent()
        {
            SetControl(this.sim);
            ApplyCheckboxStates();
        }

        public void SetControl(SimulationControl sim)
        {
            if (sim.simulation_Waermebedarf == null) return; // Sicherheitshalber prüfen 

            // Chart Strombedarf und Stromverbrauch Übersicht
            temp_profil = sim.simulation_Waermebedarf.Waermebedarf;
            temp_wp = sim.simulation_wp.WP_Waermeproduktion_stuendlich;
            temp_hs = sim.simulation_wp.Heizstab_stuendlich;
            temp_hk = sim.simulation_spk.Kesselleistung_stuendlich;
            temp_st = Array.ConvertAll<double, float>(sim.simulation_solarthermie.Waermeproduktion, x => (float)x);
            temp_bhkw = sim.simulation_bhkw.waermeproduktion;
            temp_ges = new float[8760];

            // Speicherfüllstände: eine Serie je vorhandenem Speicher (Senken-Puffer und
            // Quellspeicher), nicht mehr nur der eine puffer_wp (Konzept 13.3).
            speicherListe = sim.AlleSpeicher();
            speicherSchluessel.Clear();
            for (int i = 0; i < speicherListe.Count; i++)
            {
                // Series.Name muss eindeutig sein - Chart.Series.Add wirft sonst.
                // Der Schlüssel ist es von sich aus (verschiedene Präfixe, verschiedene
                // IDs); der Zähler ist nur die Absicherung gegen fehlende IDs.
                string s = speicherListe[i].Schluessel(i);
                while (speicherSchluessel.Contains(s)) s += "_" + i;
                speicherSchluessel.Add(s);
            }

            for (int i = 0; i < 8760; i++) temp_ges[i] = temp_wp[i] + temp_hs[i] + temp_hk[i] + temp_st[i] + temp_bhkw[i];

            _chartManager = new ChartManager(chart_Waerme);
            _chartManager.BackColor = Color.White;
            _chartManager._chart.BackColor = Color.LightGray;
            // Skalierung so wählen, dass auch die Speicherfüllstände vollständig sichtbar sind
            _chartManager.YMaxValue = Math.Max(temp_ges.Max(), SpeicherMax()) + 1;
            _chartManager.YMinValue = 0;
            _chartManager.XAxisAsNumber = false;
            _chartManager.XAxisTitle = MyResource.Resource.CHART_ACHSE_MONATE;
            _chartManager.YAxisTitle = MyResource.Resource.CHART_ACHSE_LEISTUNG_SPEICHERINHALT;
            _chartManager.toolTipUnit = "kW";
            _chartManager.ChartTitle = MyResource.Resource.CHART_TITEL_WAERMEPRODUKTION_JAHRESGANGLINIE;
            _chartManager.MitLegende = true;
            _chartManager.MaxXVALUE = 8760;
            _chartManager.MitViertelStunde = false;
            _chartManager.LegendMarkerBreite = 5;
            
            _chartManager.Init();
            SerieAnlegen(S_WAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF, Color.DarkCyan, temp_profil);
            SerieAnlegen(S_GESAMT, MyResource.Resource.CHART_LEGENDE_GESAMT, Color.Green, temp_ges);
            SerieAnlegen(S_WAERMEPUMPE, MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE, Color.Orange, temp_wp);
            SerieAnlegen(S_HEIZSTAB, MyResource.Resource.CHART_SEGMENT_HEIZSTAB, Color.Yellow, temp_hs);
            SerieAnlegen(S_HEIZKESSEL, MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL, Color.Blue, temp_hk);
            SerieAnlegen(S_SOLARTHERMIE, MyResource.Resource.SIM_ERZEUGERNAME_SOLARTHERMIE, Color.Brown, temp_st);
            SerieAnlegen(S_BHKW, MyResource.Resource.SIM_ERZEUGERNAME_BHKW, Color.Red, temp_bhkw);

            // Eine Serie je Speicher. Series.Name ist der technische Schlüssel,
            // der Anzeigetext geht in LegendText (Konzept 13.3).
            for (int i = 0; i < speicherListe.Count; i++)
            {
                _chartManager.AddSeries(speicherSchluessel[i],
                    SPEICHER_FARBEN[i % SPEICHER_FARBEN.Length],
                    speicherListe[i].SOC_stuendlich);
                Series s = _chartManager._chart.Series[speicherSchluessel[i]];
                s.LegendText = SpeicherAnzeige(speicherListe[i]);
                s.Enabled = false;
            }

            _chartManager._chart.Series[S_WAERMEBEDARF].BorderDashStyle = ChartDashStyle.Solid;
            _chartManager._chart.Series[S_WAERMEPUMPE].Enabled = false;
            _chartManager._chart.Series[S_HEIZSTAB].Enabled = false;
            _chartManager._chart.Series[S_HEIZKESSEL].Enabled = false;
            _chartManager._chart.Series[S_SOLARTHERMIE].Enabled = false;
            _chartManager._chart.Series[S_BHKW].Enabled = false;
            _chartManager._chart.Series[S_WAERMEBEDARF].Enabled = false;
            checkBox_Gesamt.Checked = true;

            // Checkbox nur anbieten, wenn der Lauf überhaupt einen Speicher hatte.
            if (checkBox_Puffer != null) checkBox_Puffer.Enabled = (speicherListe.Count > 0);
            AktualisiereSpeicherAuswahl();
        }

        /// <summary>
        /// Legt eine Chart-Serie an: <b>Name</b> = technischer Schlüssel (sprachneutral),
        /// <b>LegendText</b> = übersetzter Anzeigetext. Dieselbe Trennung, die die
        /// Speicherserien seit Paket 7 haben (Konzept 13.3).
        /// </summary>
        private void SerieAnlegen(string schluessel, string legende, Color farbe, float[] werte)
        {
            _chartManager.AddSeries(schluessel, farbe, werte);
            _chartManager._chart.Series[schluessel].LegendText = legende;
        }

        /// <summary>
        /// Füllt die Auswahlliste der Speicher. Sie erscheint erst ab zwei Speichern -
        /// bei genau einem bleibt es bei der reinen Checkbox wie bisher.
        /// </summary>
        private void AktualisiereSpeicherAuswahl()
        {
            if (comboBox_Puffer == null) return;

            comboBox_Puffer.SelectedIndexChanged -= comboBox_Puffer_SelectedIndexChanged;
            comboBox_Puffer.Items.Clear();
            comboBox_Puffer.Items.Add(MyResource.Resource.PSP_AUSWAHL_ALLE_SPEICHER);
            foreach (SimulationPufferspeicher sp in speicherListe)
                comboBox_Puffer.Items.Add(SpeicherAnzeige(sp));
            comboBox_Puffer.SelectedIndex = 0;
            comboBox_Puffer.SelectedIndexChanged += comboBox_Puffer_SelectedIndexChanged;

            comboBox_Puffer.Visible = AuswahlAktiv();
        }

        /// <summary>Anzeigetext eines Speichers: Bezeichner und Rolle (Konzept 13.3).</summary>
        private static string SpeicherAnzeige(SimulationPufferspeicher sp)
        {
            return sp.Anzeige();
        }

        /// <summary>Wird die Auswahlliste überhaupt benutzt? (Kriterium wie beim Anlegen)</summary>
        private bool AuswahlAktiv()
        {
            return speicherListe.Count > 1;
        }

        /// <summary>
        /// Soll der Speicher mit diesem Index angezeigt werden (Auswahlliste)?
        ///
        /// Das Kriterium ist bewusst die Speicherzahl und NICHT comboBox_Puffer.Visible:
        /// Control.Visible liefert false, solange das Steuerelement (oder eine seiner
        /// Elternebenen) noch nicht angezeigt wird. Wird der Navigator im Hintergrund
        /// aufgebaut oder der CSV-Export vor dem ersten Anzeigen ausgelöst, hätte die
        /// Prüfung "nicht sichtbar => alle Speicher" gegolten und die getroffene Auswahl
        /// wäre stillschweigend übergangen worden.
        /// </summary>
        private bool SpeicherSichtbar(int index)
        {
            if (comboBox_Puffer == null || !AuswahlAktiv()) return true;
            int sel = comboBox_Puffer.SelectedIndex;
            return sel <= 0 || sel - 1 == index;   // 0 = "Alle Speicher"
        }

        /// <summary>Größter Füllstand über alle Speicher (Y-Skalierung).</summary>
        private double SpeicherMax()
        {
            double max = 0;
            foreach (SimulationPufferspeicher sp in speicherListe)
                if (sp.SOC_stuendlich != null && sp.SOC_stuendlich.Length > 0)
                {
                    float m = sp.SOC_stuendlich.Max();
                    if (m > max) max = m;
                }
            return max;
        }

        /// <summary>Schaltet die Speicherserien gemäß Checkbox und Auswahlliste.</summary>
        private void SpeicherSerienAktualisieren()
        {
            if (_chartManager == null || _chartManager._chart == null) return;
            bool an = (checkBox_Puffer != null && checkBox_Puffer.Checked);

            for (int i = 0; i < speicherSchluessel.Count; i++)
            {
                if (_chartManager._chart.Series.IndexOf(speicherSchluessel[i]) < 0) continue;
                _chartManager._chart.Series[speicherSchluessel[i]].Enabled = an && SpeicherSichtbar(i);
            }
        }

        private void comboBox_Puffer_SelectedIndexChanged(object sender, EventArgs e)
        {
            SpeicherSerienAktualisieren();
        }

        private void ApplyCheckboxStates()
        {
            // Hier erzwingst du, dass das Chart genau das anzeigt, was die Checkbox sagt
            if (_chartManager != null && _chartManager._chart.Series.Count > 0)
            {
                _chartManager._chart.Series[S_GESAMT].Enabled = checkBox_Gesamt.Checked;
                _chartManager._chart.Series[S_WAERMEPUMPE].Enabled = checkBox_WP.Checked;
                _chartManager._chart.Series[S_HEIZSTAB].Enabled = checkBox_Heizstab.Checked;
                _chartManager._chart.Series[S_HEIZKESSEL].Enabled = checkBox_SPK.Checked;
                _chartManager._chart.Series[S_SOLARTHERMIE].Enabled = checkBox_ST.Checked;
                _chartManager._chart.Series[S_BHKW].Enabled = checkBox_BHKW.Checked;
                SpeicherSerienAktualisieren();
            }
        }

        private void checkBox_Puffer_CheckedChanged(object sender, EventArgs e)
        {
            SpeicherSerienAktualisieren();
        }

        private void checkBox_Gesamt_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Gesamt.Checked)
            {
                _chartManager._chart.Series[S_GESAMT].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series[S_GESAMT].Enabled = false;
            }
        }

        private void checkBox_WP_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_WP.Checked)
            {
                _chartManager._chart.Series[S_WAERMEPUMPE].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series[S_WAERMEPUMPE].Enabled = false;
            }
        }

        private void checkBox_Heizstab_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_Heizstab.Checked)
            {
                _chartManager._chart.Series[S_HEIZSTAB].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series[S_HEIZSTAB].Enabled = false;
            }
        }

        private void checkBox_SPK_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_SPK.Checked)
            {
                _chartManager._chart.Series[S_HEIZKESSEL].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series[S_HEIZKESSEL].Enabled = false;
            }
        }

        private void checkBox_ST_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_ST.Checked)
            {
                _chartManager._chart.Series[S_SOLARTHERMIE].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series[S_SOLARTHERMIE].Enabled = false;
            }
        }

        private void checkBox_BHKW_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_BHKW.Checked)
            {
                _chartManager._chart.Series[S_BHKW].Enabled = true;
            }
            else
            {
                _chartManager._chart.Series[S_BHKW].Enabled = false;
            }
        }

        private void checkBox_Waermebedarf_CheckedChanged(object sender, EventArgs e)
        {
            double neueMax = 0;

            _chartManager._chart.Series[S_WAERMEBEDARF].Enabled = checkBox_Waermebedarf.Checked;

            if (checkBox_Waermebedarf.Checked)
            {
                neueMax = temp_profil.Max() * 1.1;
                if (neueMax < 10) neueMax = 10; // Minimum setzen, damit die Achse nicht zu klein wird
            }
            else
                neueMax = Math.Max(temp_ges.Max(), SpeicherMax()) + 1;

            // Achsen-Maximum darf nie 0 oder negativ sein, sonst wirft RecalculateAxesScale
            // "Axis Object - Auto interval does not have proper value" (z. B. wenn noch keine
            // Bedarfsdaten vorliegen bzw. der Handler vor der Simulation feuert).
            if (neueMax < 10 || double.IsNaN(neueMax)) neueMax = 10;

            // Nur die Achse updaten ohne die Daten zu löschen:
            var ca = _chartManager._chart.ChartAreas[0];

            ca.AxisY.Maximum = neueMax; // Den oben berechneten Wert direkt setzen
            ca.AxisY.Interval = 0;      // Auf Auto stellen

            // 2. Prüfen, ob die Serie existiert
            if (_chartManager._chart.Series.IndexOf(S_WAERMEBEDARF) != -1)
            {
                var s = _chartManager._chart.Series[S_WAERMEBEDARF];
                bool anzeigen = checkBox_Waermebedarf.Checked;

                s.Enabled = anzeigen;

                if (anzeigen)
                {
                    // --- SPEZIALFALL: Y2-ACHSE AKTIVIEREN ---
                    s.YAxisType = AxisType.Secondary; // Serie nach rechts binden
                    ca.AxisY2.Enabled = AxisEnabled.True;

                    // Optik der rechten Achse
                    ca.AxisY2.Title = MyResource.Resource.CHART_ACHSE_WAERMEBEDARF_KWH;
                    ca.AxisY2.TitleForeColor = Color.Black;
                    ca.AxisY2.LabelStyle.ForeColor = Color.Black;
                    ca.AxisY2.MajorGrid.Enabled = false; // Gitter nur links lassen

                    // Skalierung berechnen (falls nicht automatisch gewünscht)
                    if (s.Points.Count > 0)
                    {
                        double maxVal = s.Points.Max(p => p.YValues[0]);
                        ca.AxisY2.Maximum = maxVal > 0 ? maxVal * 1.1 : 10;
                    }

                    // Den inneren Bereich schrumpfen, damit rechts Platz für die 2. Achse ist
                    ca.InnerPlotPosition.Auto = false;
                    ca.InnerPlotPosition.X = 10;      // Start links
                    ca.InnerPlotPosition.Width = 75;  // Vorher ca. 85, jetzt weniger für Y2-Platz
                    ca.InnerPlotPosition.Y = 8;
                    ca.InnerPlotPosition.Height = 75;

                    // Sicherstellen, dass die Achse nicht abgeschnitten wird
                    ca.AxisY2.LabelStyle.Enabled = true;

                }
                else
                {
                    // Y2-Achse wieder verstecken, wenn Speicher aus
                    ca.AxisY2.Enabled = AxisEnabled.False;
                }
            }
        }
    }
}
