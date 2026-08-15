using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    public partial class NavigatorStrom : UserControl, INavigatableContent
    {
        private float[] temp_profil;
        private float[] temp_wp;
        private float[] temp_hs;
        private float[] temp_hk;
        private float[] temp_bhkw;
        private float[] temp_ges;

        ChartManager _chartManager;
        SimulationControl sim;

        // --- Technische Serienschlüssel (Paket 9 / L7) --------------------------------
        //
        // Schicht 2 der Drei-Schichten-Regel: sprachneutral, ASCII, unveränderlich.
        // Sie sind der ZUGRIFFSSCHLÜSSEL auf die Chart-Serien; der angezeigte Text steht
        // ausschließlich in Series.LegendText und kommt aus dem Ressourcenkatalog.
        // Muster wie NavigatorWaerme (Paket 9 / L6): dort trugen die Serien ihre deutschen
        // Anzeigenamen als Namen, hier ebenso - und ebenso uneinheitlich („Waermepumpe"
        // ohne Umlaut). Ein übersetzter Name ließe sämtliche Series["…"]-Nachschlagestellen
        // ins Leere laufen.
        private const string S_GESAMT = "GESAMT";
        private const string S_WAERMEPUMPE = "WAERMEPUMPE";
        private const string S_HEIZSTAB = "HEIZSTAB";
        private const string S_HEIZKESSEL = "HEIZKESSEL";
        private const string S_PROFIL_LASTGANG = "PROFIL_LASTGANG";
        private const string S_BHKW = "BHKW_STROM";

        // „PV" ist in beiden Sprachen dasselbe Kürzel (Lokalisierungskatalog, Abschnitt
        // „reine Einheiten und Symbole"). Schlüssel und Anzeigetext fallen hier zusammen;
        // ein eigener LegendText wäre eine Ressource mit zweimal demselben Wert.
        private const string S_PV = "PV";

        /// <summary>
        /// Welche Erzeuger gehören zu diesem Ergebnis? Vorbelegt mit „alles sichtbar",
        /// damit vor dem ersten <see cref="SetControl"/> nichts fehlt.
        /// </summary>
        private ErgebnisPraesenz _praesenz = ErgebnisPraesenz.Alles();

        /// <summary>Abstand zwischen zwei Checkboxen beim Nachrücken.</summary>
        private const int CHK_ABSTAND = 20;

        public NavigatorStrom(SimulationControl simctrl)
        {
            InitializeComponent();
            BeschriftungenSetzen();
            sim = simctrl;
            InitCsvExportButton();
        }

        /// <summary>
        /// Setzt die im Designer angelegten Beschriftungen aus dem Ressourcenkatalog.
        ///
        /// <b>Bewusste Abweichung vom WinForms-Weg</b> (Paket 9 / L7, wie vom Auftraggeber
        /// entschieden): Eine <c>Localizable</c>-Ressource trüge je Kultur auch Position und
        /// Größe; ein Handumbau der Designer-.resx ohne den WinForms-Designer verschöbe
        /// Steuerelemente. Die Texte werden deshalb programmatisch aus dem Katalog gesetzt,
        /// die Designer-Fassung bleibt als deutsche Entwurfszeit-Vorbelegung stehen.
        /// </summary>
        private void BeschriftungenSetzen()
        {
            checkBox_Gesamt.Text = MyResource.Resource.CHART_LEGENDE_GESAMT;
            checkBox_WP.Text = MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE;
            checkBox_Heizstab.Text = MyResource.Resource.CHART_SEGMENT_HEIZSTAB;
            checkBox_SPK.Text = MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL;
            checkBox_Profil_Lastgang.Text = MyResource.Resource.CHART_LEGENDE_PROFIL_LASTGANG;
            checkBox_PV.Text = MyResource.Resource.SIM_PHOTOVOLTAIK;
            checkBox_BHKW.Text = MyResource.Resource.SIM_ERZEUGERNAME_BHKW;

            // Entwurfszeit-Titel des Charts. Er ist nur zu sehen, solange SetControl noch
            // nicht gelaufen ist - ChartManager.Init() ersetzt die Titelsammlung danach.
            if (chart7.Titles.Count > 0)
                chart7.Titles[0].Text = MyResource.Resource.CHART_TITEL_STROMVERLAUF_JAHRESGANGLINIE;
        }

        /// <summary>
        /// Legt den CSV-Export-Button rechts neben den Checkboxen an (programmatisch, kein Designer nötig).
        /// </summary>
        private void InitCsvExportButton()
        {
            Button btnExport = new Button();
            btnExport.Name = "btn_CsvExport";
            btnExport.Text = MyResource.Resource.SIM_BTN_CSV_EXPORT;
            btnExport.Size = new Size(105, 28);
            btnExport.Location = new Point(875, 516); // rechts neben checkBox_BHKW (y=520)
            btnExport.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            btnExport.Click += btn_CsvExport_Click;
            this.Controls.Add(btnExport);
            btnExport.BringToFront();
        }

        /// <summary>
        /// Exportiert die aktuell per Checkbox selektierten Serien des Strom-Charts als CSV
        /// (Zeitstempel, Außentemperatur, Werte — Viertelstundenwerte).
        /// </summary>
        private void btn_CsvExport_Click(object sender, EventArgs e)
        {
            if (sim == null || sim.simulation_Strombedarf == null || temp_ges == null)
            {
                MessageBox.Show(MyResource.Resource.SIM_MSG_KEINE_DATEN_SIMULATION.Replace("\n", Environment.NewLine),
                    MyResource.Resource.SIM_BTN_CSV_EXPORT, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // nur die aktuell selektierten (angezeigten) Serien exportieren
            List<CsvSpalte> spalten = new List<CsvSpalte>();
            if (checkBox_Gesamt.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_GESAMT, temp_ges));
            if (checkBox_WP.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_WAERMEPUMPE, temp_wp));
            if (checkBox_Heizstab.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_HEIZSTAB, temp_hs));
            if (checkBox_SPK.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_HEIZKESSEL, temp_hk));
            if (checkBox_Profil_Lastgang.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_PROFIL_LASTGANG, temp_profil));
            if (checkBox_PV.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_PV, sim.simulation_pv != null ? sim.simulation_pv.Stromproduktion_viertelstunde : null));
            if (checkBox_BHKW.Checked) spalten.Add(new CsvSpalte(MyResource.Resource.CHART_CSV_BHKW, temp_bhkw));

            float[] temperatur = (sim.simulation_Waermebedarf != null)
                ? sim.simulation_Waermebedarf.Stundentemperatur
                : null;

            CsvExportClass.Export(MyResource.Resource.CHART_DATEI_STROMBEDARF, temperatur, spalten, true);
        }

        public void RefreshContent()
        {
            // Hier die Logik, um die Charts mit neuen Daten aus simctrl zu füttern
            SetControl(this.sim);
            ApplyCheckboxStates();
            this.Invalidate();
        }

        public void UpdateSimulationData()
        {
            SetControl(this.sim);
        }

        public void SetControl(SimulationControl sim)
        {
            if (sim == null) return;
            if (sim.simulation_Strombedarf == null) return; // Sicherheitshalber prüfen 
            _chartManager = new ChartManager(chart7);
            _chartManager.BackColor = Color.White;
            _chartManager._chart.BackColor = Color.LightGray;

            // Chart Strombedarf und Stromverbrauch Übersicht
            temp_profil = sim.simulation_Strombedarf.Strombedarf_viertelStundenwerte;
            temp_wp = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.WP_Strombedarf_stuendlich);
            temp_hs = sim.Stundenwerte_zu_viertelstunden(sim.simulation_wp.Heizstab_stuendlich);
            temp_hk = sim.Stundenwerte_zu_viertelstunden(sim.simulation_spk.Strombedarf_stuendlich);
            temp_bhkw = sim.Stundenwerte_zu_viertelstunden(sim.simulation_bhkw.stromproduktion);
            temp_ges = new float[8760 * 4];

            for (int i = 0; i < 8760 * 4; i++) temp_ges[i] = temp_wp[i] + temp_hs[i] + temp_hk[i] + temp_profil[i];

            _chartManager.YMaxValue = temp_ges.Max() + 1;
            _chartManager.YMinValue = 0;
            _chartManager.XAxisAsNumber = false;
            _chartManager.XAxisTitle = MyResource.Resource.CHART_ACHSE_MONATE;
            _chartManager.YAxisTitle = MyResource.Resource.CHART_ACHSE_LEISTUNG;
            _chartManager.toolTipUnit = "kW";
            _chartManager.ChartTitle = MyResource.Resource.CHART_TITEL_STROMBEDARF_STROMVERBRAUCH_JAHRESGANGLINIE;
            _chartManager.MitLegende = true;
            _chartManager.MaxXVALUE = 8760 * 4;
            _chartManager.MitViertelStunde = true;
            _chartManager.LegendMarkerBreite = 5;

            // Welche Erzeuger gehören zu diesem Ergebnis? Serien und Checkboxen der
            // übrigen entstehen gar nicht erst (siehe ErgebnisPraesenz und
            // CheckboxenAnordnen).
            _praesenz = ErgebnisPraesenz.Ermitteln(sim);

            _chartManager.Init();

            // -----------------------------------------------------------------------
            // DER STAPEL: die VERBRAUCHSSEITE.
            //
            // "Gesamt" ist hier die Summe aus Lastgangprofil, Wärmepumpe, Heizstab und
            // Kessel (siehe temp_ges oben) - genau diese vier addieren sich und werden
            // deshalb gestapelt. PV und BHKW gehören NICHT hinein: sie erzeugen Strom und
            // sind in der Summe nicht enthalten; sie bleiben Linien über dem Stapel.
            SeriesChartType verbrauchTyp = SeriesChartType.StackedArea;

            SerieAnlegen(S_PROFIL_LASTGANG, MyResource.Resource.CHART_LEGENDE_PROFIL_LASTGANG, Color.Brown, temp_profil, verbrauchTyp);
            if (_praesenz.Waermepumpe)
                SerieAnlegen(S_WAERMEPUMPE, MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE, Color.Orange, temp_wp, verbrauchTyp);
            if (_praesenz.Heizstab)
                SerieAnlegen(S_HEIZSTAB, MyResource.Resource.CHART_SEGMENT_HEIZSTAB, Color.Yellow, temp_hs, verbrauchTyp);
            if (_praesenz.Heizkessel)
                SerieAnlegen(S_HEIZKESSEL, MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL, Color.Blue, temp_hk, verbrauchTyp);

            // --- Erzeugung: Linien über dem Stapel ---------------------------------
            if (_praesenz.BHKW)
                SerieAnlegen(S_BHKW, MyResource.Resource.SIM_ERZEUGERNAME_BHKW, Color.Brown, temp_bhkw);

            // _chartManager[7].AddSeries("Rest", Color.Black, sim.Rest_Strombedarf_viertelstuendlich);
            if (_praesenz.Photovoltaik)
                _chartManager.AddSeries(S_PV, Color.BlueViolet, sim.simulation_pv.Stromproduktion_viertelstunde);
            // _chartManager[7].AddSeries("Überschuss", Color.Magenta, sim.simulation_pv.Ueberschuss_viertelstunde);

            // "Gesamt" ZULETZT und damit ganz oben - dieselbe Begründung wie in
            // NavigatorWaerme: MS-Chart zeichnet in Collection-Reihenfolge, und eine Summe
            // an zweiter Stelle verschwindet unter jeder danach angelegten Serie, sobald
            // sie punktweise mit ihr zusammenfällt. Über dem Stapel bleibt sie als
            // Kontrolllinie aus einem eigenen Vektor stehen.
            SerieAnlegen(S_GESAMT, MyResource.Resource.CHART_LEGENDE_GESAMT, Color.Green, temp_ges);
            _chartManager._chart.Series[S_GESAMT].BorderWidth = 2;

            // Ausgangszustand: nur "Gesamt" an.
            foreach (var s in _chartManager._chart.Series)
                if (s.Name != S_GESAMT) s.Enabled = false;

            CheckboxenAnordnen();
            checkBox_Gesamt.Checked = true;
        }

        // ====================================================================
        //  Checkbox-Zeile: nur vorhandene Komponenten
        // ====================================================================
        //
        // Wie in NavigatorWaerme: die Schalter fehlender Komponenten werden AUSGEBLENDET
        // (nicht gesperrt) und die verbleibenden rücken nach links nach. „Gesamt" und
        // „Profil/Lastgang" bleiben immer — sie beschreiben das Projekt, nicht einen
        // Erzeuger. Positionen entstehen aus den tatsächlichen Breiten (AutoSize);
        // Designer und .resx bleiben unangetastet.
        private void CheckboxenAnordnen()
        {
            this.SuspendLayout();

            // Die Präsenz wird hier mitgeführt und nicht aus Control.Visible zurückgelesen:
            // dessen Getter liefert false, solange das Elternelement noch nicht angezeigt
            // wird - beim Aufbau im Hintergrund rückte sonst nichts nach.
            var zeile = new[]
            {
                new { Schalter = checkBox_Gesamt,         Da = true },
                new { Schalter = checkBox_WP,             Da = _praesenz.Waermepumpe },
                new { Schalter = checkBox_Heizstab,       Da = _praesenz.Heizstab },
                new { Schalter = checkBox_SPK,            Da = _praesenz.Heizkessel },
                new { Schalter = checkBox_PV,             Da = _praesenz.Photovoltaik },
                new { Schalter = checkBox_Profil_Lastgang, Da = true },
                new { Schalter = checkBox_BHKW,           Da = _praesenz.BHKW }
            };

            int links = checkBox_Gesamt.Left;
            int oben = checkBox_Gesamt.Top;
            foreach (var e in zeile)
            {
                if (e.Schalter == null) continue;

                // Ausgeblendet heißt auch abgewählt - sonst bliebe ein unsichtbares
                // Checked stehen und der CSV-Export nähme eine Spalte mit, die niemand sieht.
                if (!e.Da) e.Schalter.Checked = false;
                e.Schalter.Visible = e.Da;
                if (!e.Da) continue;

                e.Schalter.Location = new Point(links, oben);
                links = e.Schalter.Right + CHK_ABSTAND;
            }

            this.ResumeLayout();
        }

        /// <summary>
        /// Schaltet eine Serie, sofern es sie gibt. Seit der Präsenzfilterung entstehen
        /// die Serien fehlender Erzeuger nicht mehr — ein ungeprüfter Zugriff über
        /// <c>Series["…"]</c> liefe dann ins Leere.
        /// </summary>
        private void SerieSchalten(string schluessel, bool an)
        {
            if (_chartManager == null || _chartManager._chart == null) return;
            if (_chartManager._chart.Series.IndexOf(schluessel) < 0) return;
            _chartManager._chart.Series[schluessel].Enabled = an;
        }

        /// <summary>
        /// Legt eine Serie unter ihrem technischen Schlüssel an und hängt den
        /// Anzeigetext an <c>LegendText</c> (Muster aus NavigatorWaerme, Paket 9 / L6).
        ///
        /// <paramref name="typ"/> überschreibt den Serientyp aus
        /// <c>ChartManager.AddSeries</c> (<c>FastLine</c>) — nötig, weil <c>FastLine</c>
        /// nicht stapeln kann.
        /// </summary>
        private void SerieAnlegen(string schluessel, string legende, Color farbe, float[] werte,
                                  SeriesChartType typ = SeriesChartType.FastLine)
        {
            _chartManager.AddSeries(schluessel, farbe, werte);
            var s = _chartManager._chart.Series[schluessel];
            s.LegendText = legende;
            s.ChartType = typ;
        }

        private void ApplyCheckboxStates()
        {
            // Hier erzwingst du, dass das Chart genau das anzeigt, was die Checkbox sagt
            if (_chartManager != null && _chartManager._chart.Series.Count > 0)
            {
                SerieSchalten(S_GESAMT, checkBox_Gesamt.Checked);
                SerieSchalten(S_WAERMEPUMPE, checkBox_WP.Checked);
                SerieSchalten(S_HEIZSTAB, checkBox_Heizstab.Checked);
                SerieSchalten(S_HEIZKESSEL, checkBox_SPK.Checked);
                SerieSchalten(S_PROFIL_LASTGANG, checkBox_Profil_Lastgang.Checked);
                SerieSchalten(S_PV, checkBox_PV.Checked);
                SerieSchalten(S_BHKW, checkBox_BHKW.Checked);
            }
        }

        private void checkBox_Gesamt_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_GESAMT, checkBox_Gesamt.Checked);
            OptimizeYAxisScale();
        }

        private void checkBox_WP_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_WAERMEPUMPE, checkBox_WP.Checked);
            OptimizeYAxisScale();
        }

        private void checkBox_Heizstab_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_HEIZSTAB, checkBox_Heizstab.Checked);
            OptimizeYAxisScale();
        }

        private void checkBox_SPK_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_HEIZKESSEL, checkBox_SPK.Checked);
            OptimizeYAxisScale();
        }

        private void checkBox_Profil_Lastgang_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_PROFIL_LASTGANG, checkBox_Profil_Lastgang.Checked);
            OptimizeYAxisScale();
        }


        private void checkBox_PV_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_PV, checkBox_PV.Checked);
            OptimizeYAxisScale();
        }

        private void OptimizeYAxisScale()
        {
            if (_chartManager == null || _chartManager._chart == null) return;

            var chart = _chartManager._chart;
            float maxVisibleValue = 0;
            bool anySeriesVisible = false;

            // 1. Finde den höchsten Wert aller sichtbaren Serien
            foreach (var series in chart.Series)
            {
                if (series.Enabled && series.Points.Count > 0)
                {
                    anySeriesVisible = true;
                    // Ermittle das Maximum der Punkte dieser Serie
                    double seriesMax = series.Points.Max(p => p.YValues[0]);
                    if (seriesMax > maxVisibleValue)
                    {
                        maxVisibleValue = (float)seriesMax;
                    }
                }
            }

            // 2. Skalierung + passendes Interval anwenden.
            //    WICHTIG: Bisher wurde nur das Maximum gesetzt. Das in Init() berechnete
            //    Interval passte danach nicht mehr zum neuen Maximum -> die Y-Achse hatte
            //    keine (oder unpassende) Labels. Deshalb hier das Interval passend zum
            //    neuen Maximum neu berechnen (gleiche Logik wie in ChartManager.Init()).
            var axisY = chart.ChartAreas[0].AxisY;
            if (anySeriesVisible && maxVisibleValue > 0)
            {
                double interval = _chartManager.CalculateNiceInterval(maxVisibleValue, 8);
                if (interval <= 0) interval = 1;
                double roundedMax = Math.Ceiling((maxVisibleValue * 1.1) / interval) * interval;

                axisY.Minimum = 0;
                axisY.Maximum = roundedMax;
                axisY.Interval = interval;
                axisY.IntervalOffset = 0;
                axisY.LabelStyle.Format = interval < 1.0 ? "N1" : "N0";
            }
            else
            {
                // Fallback, wenn nichts ausgewählt ist: alles automatisch
                axisY.Minimum = 0;
                axisY.Maximum = Double.NaN;
                axisY.Interval = 0; // 0 = automatische Intervallberechnung
            }

            chart.ChartAreas[0].RecalculateAxesScale();
            chart.Invalidate();
        }

        private void checkBox_BHKW_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_BHKW, checkBox_BHKW.Checked);
            OptimizeYAxisScale();
        }
    }
}
