using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting; // WICHTIG für Charting-Typen
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    public partial class DashboardForm : Form
    {
        public float[] pvProd;
        public float[] stProd;
        public float[] stromBedarf;
        public float[] waermeBedarf;
        public double speicherKWh = 0;

        // --- Präsenz der Komponenten -------------------------------------------------
        //
        // Das Formular ist kein UserControl mit SimulationControl, sondern wird von
        // TabNavigationManager mit fertigen Vektoren versorgt. Die Präsenz kommt deshalb
        // von dort - seit iU9-W11a als ErgebnisPraesenz-Objekt. Bis dahin standen hier
        // zwei bool-Felder (HatPV/HatSolarthermie), weil ErgebnisPraesenz internal war
        // und ein öffentliches Feld dieses Typs in einer öffentlichen Klasse nicht
        // übersetzbar gewesen wäre. Mit dem Umzug in den Kern ist die Klasse public.
        //
        // Vorbelegt mit Alles(), damit ein Aufrufer, der sie nicht setzt, alles sieht.
        public ErgebnisPraesenz Praesenz = ErgebnisPraesenz.Alles();

        private bool isUpdatingUI = false; // Event Sperre

        // --- Technische Serienschlüssel (Paket 9 / L7) --------------------------------
        //
        // Schicht 2 der Drei-Schichten-Regel: sprachneutral, ASCII, unveränderlich.
        // Sie sind der ZUGRIFFSSCHLÜSSEL auf die Chart-Serien; der angezeigte Text steht
        // ausschließlich in Series.LegendText und kommt aus dem Ressourcenkatalog.
        // Muster wie NavigatorWaerme (Paket 9 / L6).
        private const string S_DIREKTVERBRAUCH = "DIREKTVERBRAUCH";
        private const string S_SPEICHERNUTZUNG = "SPEICHERNUTZUNG";
        private const string S_NETZBEZUG = "NETZBEZUG";

        public DashboardForm()
        {
            InitializeComponent();

            // Entwurfsposition merken, BEVOR PraesenzAnwenden sie verschieben kann.
            _stLinksOriginal = groupST.Left;

            // D2 (28.08.2026): Entwurfsmaße einfrieren, solange sie noch unverfälscht
            // sind — LayoutEinpassen() rechnet gegen sie und bleibt damit bei jedem
            // erneuten Aufruf beim selben Ergebnis.
            _entwurfClient = this.ClientSize;
            _speicherInfoEntwurf = lblSpeicherInfo.Size;
            _nutzungsgradEntwurf = lblNutzungsgradST.Size;

            // D2-Beifang: lblCO2 war die einzige Arial-Beschriftung der ganzen Anwendung
            // (D-Check, Klasse e). Fett bleibt fett, die Familie folgt dem Formular.
            lblCO2.Font = new Font(this.Font, FontStyle.Bold);

            BeschriftungenSetzen();
            SetupChart(); // Ruft die neue Setup-Methode auf
            LayoutEinpassen();
        }

        /// <summary>Client-Maß des Entwurfs (Bezug für die Breitenbegrenzung).</summary>
        private Size _entwurfClient;

        /// <summary>Entwurfsmaß der Speicher-Beschriftung (Untergrenze der Breite).</summary>
        private Size _speicherInfoEntwurf;

        /// <summary>Entwurfsmaß der Nutzungsgradzeile (Untergrenze der Breite).</summary>
        private Size _nutzungsgradEntwurf;

        /// <summary>
        /// D-CHECK 28.08.2026, offener Befund Prio 1: die sechs Layoutfehler der
        /// Autarkie-Analyse, programmatisch geheilt.
        ///
        /// <para><b>Drei abgeschnittene Beschriftungen (Klasse c).</b>
        /// <c>lblSpeicherInfo</c> („Theoretischer Speicher (PV) (kWh):") brauchte 242 px
        /// und hatte 165, <c>lblNutzungsgradST</c> 198 gegen 159, <c>lblSTDeckung</c>
        /// („nicht benötigt") 102 gegen 100. Feste Breiten wären nur für EINE Sprache und
        /// EINE Schriftgröße richtig — gemessen wird deshalb am Steuerelement selbst, mit
        /// dessen aktueller Schrift, und zwar NACH dem Setzen der Texte (die Prozentwerte
        /// entstehen erst in <see cref="UpdateSimulationData"/>).</para>
        ///
        /// <para><b>Drei Überlappungen (Klasse a).</b> <c>lblTest</c> lag 120 × 2 px auf
        /// <c>numSpeicherKWh</c>, <c>pbPV</c> und <c>pbST</c> je 100 × 3 px auf ihrer
        /// Wertbeschriftung. Alle drei stammen aus derselben Ursache: „eine Zeile unter X"
        /// war im Entwurf mit festen Pixelwerten gemeint, und die Steuerelemente sind bei
        /// Segoe UI 9 höher als bei der Entwurfsschrift. Die Zeilen hängen jetzt an der
        /// Unterkante ihres Vorgängers statt an einer Zahl.</para>
        ///
        /// <para><b>Warum das an <see cref="Control.OnFontChanged"/> hängt.</b> Das
        /// Formular wird von <c>TabNavigationManager</c> als Kind in ein fremdes Panel
        /// gehängt; erbt es dort eine andere Schrift, verschieben sich alle gemessenen
        /// Maße. Dasselbe Muster wie bei der Bedarfsseite der Detailansicht (D8).</para>
        /// </summary>
        private void LayoutEinpassen()
        {
            if (lblSpeicherInfo == null || numSpeicherKWh == null) return;

            this.SuspendLayout();
            try
            {
                // --- Kacheln: Wert über dem Balken, Wertbreite = Balkenbreite ---------
                KachelOrdnen(lblPVAutarkie, pbPV);
                KachelOrdnen(lblSTDeckung, pbST);

                // --- Speicherblock rechts: Beschriftung, Eingabe, Nutzenzeile ---------
                BreiteMessen(lblSpeicherInfo, _speicherInfoEntwurf.Width);
                int hoehe = lblSpeicherInfo.GetPreferredSize(
                                new Size(lblSpeicherInfo.Width, 0)).Height;
                if (hoehe > lblSpeicherInfo.Height ||
                    lblSpeicherInfo.Height != _speicherInfoEntwurf.Height)
                    lblSpeicherInfo.Height = Math.Max(_speicherInfoEntwurf.Height, hoehe);

                numSpeicherKWh.Left = lblSpeicherInfo.Left;
                numSpeicherKWh.Top = lblSpeicherInfo.Bottom + 2;

                lblTest.Left = lblSpeicherInfo.Left;
                lblTest.Top = numSpeicherKWh.Bottom + 6;

                // --- Nutzungsgradzeile ------------------------------------------------
                BreiteMessen(lblNutzungsgradST, _nutzungsgradEntwurf.Width);

                // Ohne Photovoltaik rückt die Solarthermie-Kachel in die linke Spalte
                // (PraesenzAnwenden) - dort steht die CO2-Zeile. Die breitere
                // Nutzungsgradzeile weicht dann nach unten aus.
                if (lblCO2.Visible && lblNutzungsgradST.Visible &&
                    lblNutzungsgradST.Bounds.IntersectsWith(lblCO2.Bounds))
                    lblNutzungsgradST.Top = lblCO2.Bottom + 6;
            }
            finally
            {
                this.ResumeLayout(true);
            }
        }

        /// <summary>
        /// Ordnet eine Kachel: Die Wertbeschriftung bekommt die Höhe ihrer Schrift und
        /// die Breite des Balkens, der Balken rückt darunter. Damit passt auch der
        /// längste Wert („nicht benötigt") in die Zeile, und Balken und Beschriftung
        /// überlagern sich nicht mehr.
        /// </summary>
        private static void KachelOrdnen(Label wert, ProgressBar balken)
        {
            if (wert == null || balken == null) return;

            int hoehe = wert.Font.Height + 4;
            if (wert.Height != hoehe) wert.Height = hoehe;
            if (wert.Width != balken.Width) wert.Width = balken.Width;
            if (balken.Top != wert.Bottom + 2) balken.Top = wert.Bottom + 2;
        }

        /// <summary>
        /// Gibt einer Beschriftung die Breite, die ihr Text bei ihrer Schrift wirklich
        /// braucht — mindestens das Entwurfsmaß, höchstens der Platz bis zum rechten
        /// Rand. Die zwei Zusatzpixel sind der Rundungsabstand: ein Text, der exakt auf
        /// die Breite passt, wird von GDI+ je nach Schrift trotzdem gekürzt.
        /// </summary>
        private void BreiteMessen(Label lbl, int mindestbreite)
        {
            if (lbl == null) return;

            int noetig = lbl.GetPreferredSize(Size.Empty).Width + 2;
            int breite = Math.Max(mindestbreite, noetig);

            int platz = Math.Max(_entwurfClient.Width, this.ClientSize.Width) - lbl.Left - 12;
            if (platz > 40 && breite > platz) breite = platz;

            if (lbl.Width != breite) lbl.Width = breite;
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (lblCO2 != null) lblCO2.Font = new Font(this.Font, FontStyle.Bold);
            LayoutEinpassen();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LayoutEinpassen();
        }

        /// <summary>
        /// Setzt die im Designer angelegten Beschriftungen aus dem Ressourcenkatalog
        /// (Paket 9 / L7). Zur Begründung, warum das programmatisch und nicht über eine
        /// <c>Localizable</c>-Designer-Ressource geschieht, siehe <see cref="NavigatorStrom"/>.
        ///
        /// Nicht gesetzt werden die reinen Entwurfszeit-Vorbelegungen
        /// (<c>lblNutzungsgradST</c>, <c>lblCO2</c>, <c>lblTest</c>): sie werden von
        /// <see cref="UpdateSimulationData"/> vor der ersten Anzeige überschrieben.
        /// Ebenso wenig der Fenstertitel — das Formular wird von
        /// <c>TabNavigationManager</c> mit <c>TopLevel = false</c> und
        /// <c>FormBorderStyle.None</c> eingebettet, seine Titelzeile ist nie sichtbar.
        /// </summary>
        private void BeschriftungenSetzen()
        {
            groupPV.Text = MyResource.Resource.SIM_DASH_GRUPPE_PV;
            groupST.Text = MyResource.Resource.SIM_DASH_GRUPPE_ST;
            lblSpeicherInfo.Text = MyResource.Resource.SIM_DASH_SPEICHER_INFO;
        }

        public void Init()
        {
            isUpdatingUI = true; // Event SPERRE AKTIVIEREN
            if (speicherKWh > 0) numSpeicherKWh.Value = (decimal)speicherKWh;
            isUpdatingUI = false; // Event SPERRE DEAKTIVIEREN  
        }

        private void SetupChart()
        {
            // Alle alten Serien löschen, wir bauen sie neu auf
            chartSolar.Series.Clear();
            chartSolar.Legends.Clear();

            // Legende erstellen
            Legend legend1 = new Legend
            {
                Name = "DefaultLegend",
                Docking = Docking.Top,
                Alignment = StringAlignment.Center,
                Enabled = true
            };
            this.chartSolar.Legends.Add(legend1);

            // Serien für gestapelte Säulen definieren
    
            // Serie 1 (Ganz unten): Der Direktverbrauch (Sonne -> Haus)
            Series serDirekt = new Series(S_DIREKTVERBRAUCH)
            {
                ChartType = SeriesChartType.StackedColumn,
                Color = Color.Gold, // Gold/Sonnengelb
                LegendText = MyResource.Resource.CHART_LEGENDE_EIGENVERBRAUCH_DIREKT
            };
            chartSolar.Series.Add(serDirekt);

            // Serie 2 (Mitte): Was aus dem Speicher kommt
            Series serSpeicher = new Series(S_SPEICHERNUTZUNG)
            {
                ChartType = SeriesChartType.StackedColumn,
                Color = Color.LightGreen, // Grün für Speicherstrom
                LegendText = MyResource.Resource.CHART_LEGENDE_EIGENVERBRAUCH_SPEICHER
            };
            chartSolar.Series.Add(serSpeicher);

            // Serie 3 (Ganz oben, der Rest): Die Autarkie-Lücke (Netzbezug)
            Series serNetz = new Series(S_NETZBEZUG)
            {
                ChartType = SeriesChartType.StackedColumn,
                Color = Color.Red, // Rot für Kosten/Abhängigkeit
                LegendText = MyResource.Resource.CHART_LEGENDE_AUTARKIELUECKE
            };
            chartSolar.Series.Add(serNetz);

            // Achsen-Konfiguration
            chartSolar.ChartAreas[0].AxisY.Title = MyResource.Resource.CHART_ACHSE_ENERGIEBEDARF_DECKUNG;
            chartSolar.ChartAreas[0].AxisY.TitleFont = new Font("Arial", 10, FontStyle.Bold);

            // Y-Achse automatisch skalieren, damit der Winter-Bedarf gut sichtbar ist
            // und nicht durch Sommer-PV-Werte "erdrückt" wird.
            chartSolar.ChartAreas[0].AxisY.IsStartedFromZero = true;

            chartSolar.ChartAreas[0].AxisX.Interval = 1; // Jeden Monat anzeigen
            chartSolar.ChartAreas[0].AxisX.Title = MyResource.Resource.CHART_ACHSE_MONAT;
        }

        /// <summary>
        /// Blendet die Kacheln nicht vorhandener Komponenten aus und lässt die verbleibende
        /// nachrücken.
        ///
        /// „Photovoltaik Autarkie" samt Speicherfeld und „Solarthermie Deckung" samt
        /// Nutzungsgrad standen bisher immer da — in einem Projekt ohne PV bzw. ohne
        /// Kollektoren mit „0,0 %" bzw. „nicht benötigt". Die CO2-Zeile und das
        /// Monatsdiagramm bleiben: sie beschreiben das Projekt, nicht eine Komponente.
        ///
        /// Nachrücken heißt hier: fehlt die PV-Kachel, wandert die Solarthermie-Kachel auf
        /// deren Platz. Die Verschiebung erfolgt relativ, damit sie bei mehrfachem Aufruf
        /// nicht kumuliert.
        /// </summary>
        private void PraesenzAnwenden()
        {
            this.SuspendLayout();

            bool hatPV = Praesenz != null && Praesenz.Photovoltaik;
            bool hatSolarthermie = Praesenz != null && Praesenz.Solarthermie;

            groupPV.Visible = hatPV;
            lblSpeicherInfo.Visible = hatPV;
            numSpeicherKWh.Visible = hatPV;
            lblTest.Visible = hatPV;

            groupST.Visible = hatSolarthermie;
            lblNutzungsgradST.Visible = hatSolarthermie;

            // Zielspalte der Solarthermie-Kachel: eigener Platz, oder der der PV-Kachel,
            // wenn es die nicht gibt.
            int zielLinks = hatPV ? _stLinksOriginal : groupPV.Left;
            int versatz = zielLinks - groupST.Left;
            if (versatz != 0)
            {
                groupST.Left += versatz;
                lblNutzungsgradST.Left += versatz;
            }

            this.ResumeLayout();
        }

        /// <summary>Entwurfsposition der Solarthermie-Kachel (Bezugspunkt fürs Nachrücken).</summary>
        private int _stLinksOriginal;

        public void UpdateSimulationData()
        {
            PraesenzAnwenden();

            isUpdatingUI = true; // Event SPERRE AKTIVIEREN

            // Speicher-Parameter

            speicherKWh = (double)numSpeicherKWh.Value;

            // Speicherwirkung über die SpeicherEngine (AP2b). Bis dahin stand hier eine
            // zweite, unabhängige Speicherrechnung (stündlich, verlustfrei, ohne
            // SoC-Band und Leistungsgrenze), die neben der Simulation abweichende
            // Autarkiegrade anzeigte - Fachkonzept 8.2, Rudiment 3.
            SpeicherErgebnis speicher = RechneSpeicher();

            // Berechnungen
            double gesWaerme = waermeBedarf.Sum();
            double stPotenzialGesamt = stProd.Sum();

            // Last, Direktverbrauch und Speicherentnahme kommen aus derselben
            // Intervallzerlegung wie die Simulationskette (Vorverarbeitung 6).
            double gesStromBedarfBrutto = speicher.Kennzahlen.LastKwh; // Brutto-Bedarf
            double pvDirektSumme = speicher.Kennzahlen.DirektverbrauchKwh;
            double pvAusSpeicherSumme = speicher.EntladeenergieKwh;

            // Solarthermie (Genutzte Wärme) - unverändert stündlich
            double stGenutztSumme = 0;
            for (int i = 0; i < 8760; i++)
            {
                stGenutztSumme += Math.Min(stProd[i], waermeBedarf[i]);
            }

            // Kennzahlen
            // Autarkie: (Direktverbrauch + Speicherentnahme) / Gesamtbedarf. Der
            // Wechselrichterfaktor 0,95 steckt bereits in pvProd
            // (SimulationPV.pvPotentialGesamt_stuendlich) und wird hier NICHT erneut
            // angesetzt - bis AP2b tat das die Kachel, das Monatsdiagramm aber nicht.
            double autarkiePV = gesStromBedarfBrutto > 0 ? ((pvDirektSumme + pvAusSpeicherSumme) / gesStromBedarfBrutto) * 100 : 0;
            double deckungST = gesWaerme > 0 ? (stGenutztSumme / gesWaerme) * 100 : 0;

            double nutzungsgradST = stPotenzialGesamt > 0 ? (stGenutztSumme / stPotenzialGesamt) * 100 : 0;

            // CO2 Ersparnis (PV-Deckung + ST-Nutzung). iU9-W11a.5 (Befund W11-B31):
            // Die beiden Substitutionsfaktoren standen als Literale in dieser Zeile;
            // sie stehen jetzt in EmissionsVorgaben - woertlich, samt Formel.
            double co2Saved = EmissionsVorgaben.Co2ErsparnisKg(
                                  pvDirektSumme + pvAusSpeicherSumme, stGenutztSumme);

            // UI Updates
            lblPVAutarkie.Text = $"{autarkiePV:F1} %";  

            pbPV.Value = (int)Math.Min(100, autarkiePV);

            lblSTDeckung.Text = deckungST > 0 ? $"{deckungST:F1} %" : MyResource.Resource.SIM_ANZEIGE_NICHT_BENOETIGT;
            pbST.Value = (int)Math.Min(100, deckungST);

            // Formatangaben aus dem Bestand übernommen (F1 bzw. N0) - der Katalog
            // führt die Platzhalter normalisiert als {0} (Lesehinweis des Katalogs).
            lblNutzungsgradST.Text = string.Format(MyResource.Resource.SIM_ANZEIGE_THERM_NUTZUNGSGRAD,
                                                   nutzungsgradST.ToString("F1"));
            lblCO2.Text = string.Format(MyResource.Resource.SIM_ANZEIGE_CO2_ERSPARNIS,
                                        co2Saved.ToString("N0"));

            // Chart
            FillMonthlyChart(speicher);

            // Speichernutzen
            lblTest.Text = string.Format(MyResource.Resource.SIM_ANZEIGE_SPEICHERNUTZEN,
                                         pvAusSpeicherSumme.ToString("N0"));

            // D2 (28.08.2026): ZULETZT - erst jetzt stehen die Texte, deren Breite die
            // Einpassung misst (Prozentwerte, „nicht benötigt", CO2-Menge).
            LayoutEinpassen();

            isUpdatingUI = false; // Event SPERRE DEAKTIVIEREN
        }

        /// <summary>
        /// Rechnet die Speicherwirkung der eingestellten Kapazität über die
        /// <see cref="SpeicherEngine"/> — dasselbe Modell wie die Simulationskette.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Die Kachel bleibt eine Was-wäre-wenn-Betrachtung: Der Anwender verstellt die
        /// Kapazität und sieht ihre Wirkung sofort. Gerechnet wird sie seit AP2b aber
        /// mit derselben Strategie, demselben SoC-Band und demselben Verlustmodell wie
        /// der Lauf selbst — die Vorgabewerte stehen an genau einer Stelle
        /// (<see cref="StromspeicherSimCtrl.StandardParameter"/>). Die Leistungsgrenze
        /// ist 1 C, wie beim Kettenlauf ohne hinterlegte Leistung.
        /// </para>
        /// <para>
        /// Die Reihen kommen stündlich herein und werden per Wertwiederholung auf das
        /// Viertelstundenraster der Engine gebracht; die Monatssummen sind davon
        /// unberührt. BHKW bleibt außen vor — die Kachel beschreibt die PV-Autarkie.
        /// </para>
        /// </remarks>
        private SpeicherErgebnis RechneSpeicher()
        {
            _lastKw = RasterAdapter.ZuViertelstundenDouble(stromBedarf);
            _pvKw = RasterAdapter.ZuViertelstundenDouble(pvProd);

            SpeicherEingang eingang = new SpeicherEingang(
                _lastKw, _pvKw,
                SpeicherEingang.KonstanteReihe(StromspeicherSimCtrl.FIXPREIS_BEZUG_CT_KWH, _lastKw.Length));

            SpeicherParameter parameter = StromspeicherSimCtrl.StandardParameter(speicherKWh, speicherKWh);

            return new Dauernutzung(SpeicherModus.Energetisch).Berechne(eingang, parameter);
        }

        /// <summary>Lastgang [kW] des letzten Laufs im Viertelstundenraster (Quelle des Monatsdiagramms).</summary>
        private double[] _lastKw;

        /// <summary>PV-Erzeugung [kW] des letzten Laufs im Viertelstundenraster.</summary>
        private double[] _pvKw;

        private void FillMonthlyChart(SpeicherErgebnis speicher)
        {
            // Alle Datenpunkte leeren
            foreach (var series in chartSolar.Series)
            {
                series.Points.Clear();
            }

            for (int m = 0; m < 12; m++)
            {
                // Berechnung der 3 Kategorien für den gestapelten Balken
                double monatsDirekt = 0;   // Sonne -> Haus
                double monatsSpeicher = 0; // Speicher -> Haus
                double monatsLuecke = 0;    // Netz -> Haus (Die Lücke)

                // Indizierung basierend auf der 8760/12 Annahme (730 Stunden/Monat),
                // im Viertelstundenraster der Engine also 2.920 Intervalle je Monat.
                for (int v = 0; v < 2920; v++)
                {
                    int i = (m * 2920) + v;
                    if (i >= _lastKw.Length) break;

                    // Zerlegung des Intervalls wie in der Simulationskette
                    // (Fachkonzept 6): Direktdeckung und Residuallast.
                    IntervallEnergien e = Vorverarbeitung.Berechne(
                        _lastKw[i], _pvKw[i], 0.0, StromspeicherSimCtrl.INTERVALL_H, true, false);

                    monatsDirekt += e.EDirektKwh;

                    // WICHTIG: Speicherstrom ist genutzte Energie!
                    double entnahme = speicher.EntladungAcKwh[i];
                    monatsSpeicher += entnahme;

                    // Die Lücke (was Netz noch liefern muss)
                    monatsLuecke += e.EDefizitKwh - entnahme;
                }

                // Monatspunkt hinzufügen (gestapelt übereinander).
                // Monatsnamen kommen aus der Oberflächenkultur - wie in L3 festgelegt
                // (Form_Quellprofil): der Monatsname ist Anzeige und folgt der UI-Sprache,
                // nicht der Zahlenkultur. CurrentCulture bleibt unangetastet.
                string monthName = System.Globalization.CultureInfo.CurrentUICulture
                                       .DateTimeFormat.GetAbbreviatedMonthName(m + 1);

                // Reihenfolge von unten nach oben hinzufügen
                chartSolar.Series[S_DIREKTVERBRAUCH].Points.AddXY(monthName, monatsDirekt); // Gold unten
                chartSolar.Series[S_SPEICHERNUTZUNG].Points.AddXY(monthName, monatsSpeicher); // Grün mitte
                chartSolar.Series[S_NETZBEZUG].Points.AddXY(monthName, monatsLuecke); // Rot oben
            }
        }

        private void numSpeicherKWh_ValueChanged(object sender, EventArgs e)
        {
            if (isUpdatingUI) return;

            UpdateSimulationData();
        }
    }
}