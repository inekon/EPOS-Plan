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

        // Umschalter Jahresganglinie <-> Jahresdauerlinie (programmatisch, kein Designer).
        private CheckBox checkBox_Sortiert;

        /// <summary>
        /// Welche Erzeuger gehören zu diesem Ergebnis? Vorbelegt mit „alles sichtbar",
        /// damit vor dem ersten <see cref="SetControl"/> nichts fehlt.
        /// </summary>
        private ErgebnisPraesenz _praesenz = ErgebnisPraesenz.Alles();

        /// <summary>Dauerlinien-Darstellung: jede Serie für sich absteigend sortiert.</summary>
        private bool _sortiert = false;

        /// <summary>
        /// Sperrt die Checkbox-Ereignisse, solange die Serien neu aufgebaut werden.
        /// Ohne sie würde jedes <c>Checked</c>-Setzen während des Umbaus auf Serien
        /// zugreifen, die es in diesem Moment noch nicht gibt.
        /// </summary>
        private bool _imAufbau = false;

        /// <summary>Abstand zwischen zwei Checkboxen beim Nachrücken.</summary>
        private const int CHK_ABSTAND = 20;

        /// <summary>
        /// Linienbreite der Konturlinie „Gesamt" im gestapelten (chronologischen) Bild.
        /// Die Linie liegt UNTER dem Stapel; sichtbar bleibt die halbe Breite über der
        /// Stapeloberkante. Begründung im Blockkommentar in <see cref="SerienAufbauen"/>.
        /// </summary>
        private const int GESAMT_KONTUR_BREITE = 4;

        /// <summary>Linienbreite von „Gesamt" in der Dauerlinie — schmaler als die
        /// Erzeugerlinien darunter, damit deckungsgleiche Kurven lesbar bleiben.</summary>
        private const int GESAMT_DAUERLINIE_BREITE = 2;

        /// <summary>Linienbreite der Erzeugerserien in der Dauerlinie.</summary>
        private const int ERZEUGER_DAUERLINIE_BREITE = 4;

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
            InitSortiertCheckBox();
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
        /// <b>Reihenfolge beachten:</b> Der Aufruf steht VOR den programmatischen
        /// Steuerelementen, weil deren Platzierung an den Breiten der Checkboxen hängt —
        /// und die Breite hängt am Text (<c>AutoSize</c>).
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
        /// Legt die Checkbox "Speicherfüllstand" und die Speicher-Auswahlliste an
        /// (programmatisch, kein Designer nötig). Die Checkbox schaltet die
        /// Speicherserien gemeinsam ein und aus, die Auswahlliste schränkt bei mehreren
        /// Speichern auf einen einzelnen ein.
        ///
        /// Die endgültige POSITION vergibt <see cref="CheckboxenAnordnen"/> — sie hängt
        /// davon ab, welche Erzeuger-Checkboxen davor überhaupt sichtbar bleiben.
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
        /// Legt die Checkbox „sortiert" an — den Umschalter zwischen Jahresganglinie
        /// (chronologisch, Monatsachse) und Jahresdauerlinie (jede Serie für sich
        /// absteigend sortiert, numerische Jahresstundenachse).
        ///
        /// Dasselbe Bedienmuster wie auf der Wärmepumpen-Seite der Detailansicht
        /// (<c>Form_Simulation_Detail.checkBox_WP_sortiert</c>, Diagramm „Wärmelast
        /// Jahresganglinie"): dort schaltet die Checkbox <c>XAxisAsNumber</c> um, setzt
        /// den ChartManager per <c>HardReset()/Init()</c> zurück und legt die Serien mit
        /// sortierten Kopien neu an. Genau dieser Ablauf steht in
        /// <see cref="SerienAufbauen"/>.
        /// </summary>
        private void InitSortiertCheckBox()
        {
            checkBox_Sortiert = new CheckBox();
            checkBox_Sortiert.Name = "checkBox_Sortiert";
            checkBox_Sortiert.Text = MyResource.Resource.SIM_CHK_SORTIERT;
            checkBox_Sortiert.AutoSize = true;
            checkBox_Sortiert.BackColor = Color.Transparent;
            checkBox_Sortiert.Font = checkBox_Waermebedarf.Font;
            checkBox_Sortiert.ForeColor = Color.Black;
            checkBox_Sortiert.Location = new Point(checkBox_Waermebedarf.Right + CHK_ABSTAND,
                                                   checkBox_Waermebedarf.Top);
            checkBox_Sortiert.CheckedChanged += checkBox_Sortiert_CheckedChanged;
            this.Controls.Add(checkBox_Sortiert);
            checkBox_Sortiert.BringToFront();
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
        ///
        /// Der Export bleibt IMMER chronologisch: „sortiert" ist eine Darstellungsform,
        /// keine andere Datenlage — eine sortierte Datei hätte zu den Zeitstempeln in
        /// Spalte 1 nicht mehr gepasst.
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
            if (sim == null || sim.simulation_Waermebedarf == null) return; // Sicherheitshalber prüfen

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

            // Welche Erzeuger gehören zu diesem Ergebnis? Alles Weitere - welche
            // Checkboxen erscheinen und welche Serien überhaupt entstehen - hängt daran.
            _praesenz = ErgebnisPraesenz.Ermitteln(sim);

            _chartManager = new ChartManager(chart_Waerme);
            SerienAufbauen();
            CheckboxenAnordnen();

            _imAufbau = true;
            checkBox_Gesamt.Checked = true;
            _imAufbau = false;
            ApplyCheckboxStates();
            // Auch die Bedarfsserie samt zweiter Y-Achse nachziehen: bei RefreshContent
            // kann der Haken von vorher noch gesetzt sein, die Serie aber frisch angelegt
            // und damit abgeschaltet.
            WaermebedarfAchseAktualisieren();

            AktualisiereSpeicherAuswahl();
        }

        /// <summary>
        /// Baut Diagrammkonfiguration und Serien auf — in der Darstellungsform, die
        /// <see cref="_sortiert"/> vorgibt, und nur für die Erzeuger, die zum Ergebnis
        /// gehören (<see cref="_praesenz"/>).
        ///
        /// Serien fehlender Erzeuger entstehen GAR NICHT: Ein bloßes <c>Enabled = false</c>
        /// ließe die Legende weiter mitwachsen und die Y-Skalierung mitrechnen.
        ///
        /// Der Umbau folgt dem Ablauf der Wärmepumpen-Seite
        /// (<c>Form_Simulation_Detail.checkBox_WP_sortiert_CheckedChanged</c>):
        /// <c>XAxisAsNumber</c> setzen, <c>HardReset()</c>, <c>Init()</c>, Serien neu.
        /// </summary>
        private void SerienAufbauen()
        {
            if (_chartManager == null) return;

            _chartManager.BackColor = Color.White;
            _chartManager._chart.BackColor = Color.LightGray;
            // Skalierung so wählen, dass auch die Speicherfüllstände vollständig sichtbar sind
            _chartManager.YMaxValue = Math.Max(temp_ges.Max(), SpeicherMax()) + 1;
            _chartManager.YMinValue = 0;
            _chartManager.XAxisAsNumber = _sortiert;
            _chartManager.XAxisTitle = _sortiert
                ? MyResource.Resource.CHART_ACHSE_JAHRESSTUNDEN
                : MyResource.Resource.CHART_ACHSE_MONATE;
            _chartManager.YAxisTitle = MyResource.Resource.CHART_ACHSE_LEISTUNG_SPEICHERINHALT;
            _chartManager.toolTipUnit = "kW";
            _chartManager.ChartTitle = MyResource.Resource.CHART_TITEL_WAERMEPRODUKTION_JAHRESGANGLINIE;
            _chartManager.MitLegende = true;
            _chartManager.MaxXVALUE = 8760;
            _chartManager.MitViertelStunde = false;
            _chartManager.LegendMarkerBreite = 5;

            _chartManager.HardReset();
            _chartManager.Init();

            // Linienbreiten der Dauerlinie: dort ist jede Serie eine eigenständige Kurve,
            // und in einem Projekt mit nur EINEM Erzeuger ist dessen Dauerlinie punktgleich
            // mit der von „Gesamt". Die untere (Erzeuger) wird deshalb breiter gezeichnet
            // als die obere (Gesamt) — dasselbe Mittel wie auf der Heizkessel-Seite der
            // Detailansicht; Strichelung scheidet aus, weil BorderDashStyle bei FastLine
            // wirkungslos ist.
            int erzeugerBreite = _sortiert ? ERZEUGER_DAUERLINIE_BREITE : 0;

            // -----------------------------------------------------------------------
            // 0. „Gesamt" im gestapelten Bild: die KONTUR DES STAPELS, UNTER ihm.
            //
            // <b>Befund vom 16.08.2026.</b> „Der Heizkessel ist im Hintergrund (blau) nicht
            // sichtbar." Die Ursache ist NICHT der Serientyp — „Gesamt" war längst eine
            // Linie (FastLine) — sondern die PUNKTDICHTE in Verbindung mit der obersten
            // Zeichenlage: 8760 Stundenwerte liegen auf rund 775 Bildpunkten Plotbreite,
            // also gut 11 Stunden je Bildspalte. Zwischen zwei benachbarten Stunden zieht
            // die Linie einen senkrechten Strich; über den Tagesgang schwankt die Summe
            // zwischen fast 0 und dem Tagesmaximum. In JEDER Bildspalte überstreicht die
            // „Linie" damit den gesamten Schwankungsbereich der Summe — sie ist optisch
            // eine gefüllte Fläche, und zwar genau über dem Bereich, in dem die oberen
            // Stapelanteile (Heizstab, Heizkessel) liegen. Gemessen am Referenzprojekt
            // 1023: 2.545 blaue Bildpunkte mit der Linie, 29.685 ohne sie — 91 % des
            // Kessels waren übermalt.
            //
            // <b>Warum sie trotzdem nicht einfach nach hinten „als FastLine" kann.</b>
            // MS-Chart zeichnet NICHT in der Reihenfolge der Series-Collection, sondern in
            // TYPGRUPPEN, und die Gruppen in der Reihenfolge ihres ersten Auftretens in der
            // Collection. Stünde „Gesamt" als FastLine an erster Stelle, rutschte die ganze
            // FastLine-Gruppe vor den Stapel — Speicherfüllstand und Wärmebedarf
            // verschwänden mit. Nachgemessen: Speicherfüllstand 0 Bildpunkte.
            //
            // <b>Die Lösung.</b> „Gesamt" bekommt mit <c>Line</c> einen EIGENEN Serientyp
            // und steht als erste Serie. Damit entstehen drei Gruppen in genau der
            // gewünschten Folge: Line (Gesamt) — StackedColumn (Erzeuger) — FastLine
            // (Speicherfüllstand, Wärmebedarf). Der Stapel steht vollständig in seinen
            // eigenen Farben; von „Gesamt" bleibt die halbe Linienbreite als grüne Kontur
            // über der Stapeloberkante stehen.
            //
            // <b>Warum das die Kontrollfunktion erhält und nicht aufgibt.</b> „Gesamt"
            // stammt aus einem eigenen Vektor (temp_ges = Summe der fünf Erzeuger, siehe
            // SetControl). Solange alle vorhandenen Erzeuger angehakt sind, liegt die
            // Kontur auf der Stapeloberkante — Deckung heißt: die Summe stimmt. Wird ein
            // Erzeuger abgewählt oder fehlt einer, schrumpft der Stapel, die grüne Linie
            // bleibt oben stehen und der fehlende Anteil ist als Abstand ablesbar. Genau
            // dafür ist sie da. Der frühere Befund „Gesamt verschwindet unter einer
            // Einzelserie" (Alternativbetrieb) kehrt damit NICHT zurück: Er entstand, als
            // sich Gesamt und eine Erzeugerserie als gleich hohe FLÄCHEN überlagerten;
            // gestapelte Säulen überlagern sich nicht, und die Kontur ragt über sie hinaus.
            //
            // In der DAUERLINIE gilt das alles nicht: Dort ist jede Serie für sich
            // sortiert, monoton fallend und damit eine echte dünne Linie. „Gesamt" bleibt
            // deshalb im sortierten Modus die oberste Serie (Abschnitt 3).
            if (!_sortiert)
            {
                SerieAnlegen(S_GESAMT, MyResource.Resource.CHART_LEGENDE_GESAMT, Color.Green,
                             temp_ges, SeriesChartType.Line, GESAMT_KONTUR_BREITE);
            }

            // -----------------------------------------------------------------------
            // 1. DER STAPEL: die Erzeuger.
            //
            // Wärmepumpe, Heizstab, Heizkessel, Solarthermie und BHKW addieren sich
            // physikalisch zur Gesamtproduktion — genau das zeigt ein Stapel und keine
            // Schar übereinandergelegter Linien. Die Reihenfolge folgt der Kaskadenlogik
            // (WP unten), damit der Stapel bei jedem Projekt gleich zu lesen ist.
            //
            // Fehlende Erzeuger entstehen gar nicht erst (Präsenzregel) und können den
            // Stapel deshalb auch nicht verschieben. Abgewählte Serien nimmt MS-Chart
            // über Enabled = false aus dem Stapel heraus — der Stapel zeigt dann genau
            // die angehakten Anteile.
            //
            // Im SORTIERTEN Modus wird NICHT gestapelt: dort ist jede Serie für sich
            // absteigend sortiert, die Stunde i der einen Serie hat mit der Stunde i der
            // anderen nichts mehr zu tun, und eine Summe daraus wäre frei erfunden.
            //
            // SÄULEN statt Flächen (Sichttest-Befund): Läuft die Anlage im
            // ALTERNATIVbetrieb — je Stunde entweder Wärmepumpe oder Kessel —, steht der
            // Kessel in den WP-Stunden auf 0. Eine Fläche verbindet ihre Stützstellen mit
            // einer Geraden und spannt sich dann zwischen der kumulierten WP-Oberkante und
            // den Nullstunden auf: blaue Dreiecke über dem orangen WP-Anteil, obwohl der
            // Kessel dort nichts produziert hat. Die Säule zeichnet je Stunde einen
            // eigenen Balken und interpoliert nicht. Regel und Begründung stehen in
            // GanglinienDarstellung.Stapeltyp — dieselbe Regel gilt für NavigatorStrom
            // und die Diagramme der Detailansicht.
            SeriesChartType erzeugerTyp = GanglinienDarstellung.Stapeltyp(_sortiert);

            if (_praesenz.Waermepumpe)
                SerieAnlegen(S_WAERMEPUMPE, MyResource.Resource.SIM_ERZEUGERNAME_WAERMEPUMPE, Color.Orange, temp_wp, erzeugerTyp, erzeugerBreite);
            if (_praesenz.Heizstab)
                SerieAnlegen(S_HEIZSTAB, MyResource.Resource.CHART_SEGMENT_HEIZSTAB, Color.Yellow, temp_hs, erzeugerTyp, erzeugerBreite);
            if (_praesenz.Heizkessel)
                SerieAnlegen(S_HEIZKESSEL, MyResource.Resource.SIM_ERZEUGERNAME_HEIZKESSEL, Color.Blue, temp_hk, erzeugerTyp, erzeugerBreite);
            if (_praesenz.Solarthermie)
                SerieAnlegen(S_SOLARTHERMIE, MyResource.Resource.SIM_ERZEUGERNAME_SOLARTHERMIE, Color.Brown, temp_st, erzeugerTyp, erzeugerBreite);
            if (_praesenz.BHKW)
                SerieAnlegen(S_BHKW, MyResource.Resource.SIM_ERZEUGERNAME_BHKW, Color.Red, temp_bhkw, erzeugerTyp, erzeugerBreite);

            // -----------------------------------------------------------------------
            // 2. DIE LINIEN darüber — zuletzt angelegt und damit oben gezeichnet.
            //
            // Der Speicherfüllstand bleibt eine eigenständige Linie: er ist ein
            // Energieinhalt, keine Erzeugung, und gehört nicht in die Summe.
            // Series.Name ist der technische Schlüssel, der Anzeigetext geht in
            // LegendText (Konzept 13.3).
            if (_praesenz.Speicher)
                for (int i = 0; i < speicherListe.Count; i++)
                {
                    _chartManager.AddSeries(speicherSchluessel[i],
                        SPEICHER_FARBEN[i % SPEICHER_FARBEN.Length],
                        Anzeigewerte(speicherListe[i].SOC_stuendlich));
                    Series s = _chartManager._chart.Series[speicherSchluessel[i]];
                    s.LegendText = SpeicherAnzeige(speicherListe[i]);
                    s.Enabled = false;
                }

            // Der Bedarf beschreibt das Projekt, nicht einen Erzeuger - er bleibt immer
            // und liegt als Linie über dem Stapel (zweite Y-Achse, siehe
            // WaermebedarfAchseAktualisieren).
            SerieAnlegen(S_WAERMEBEDARF, MyResource.Resource.CHART_LEGENDE_WAERMEBEDARF, Color.DarkCyan, temp_profil);

            // ---------------------------------------------------------------------
            // 3. „Gesamt" in der DAUERLINIE — dort zuletzt und damit ganz oben.
            //
            // Im sortierten Modus wird nicht gestapelt (jede Serie ist für sich sortiert,
            // eine Summe daraus wäre frei erfunden). „Gesamt" ist hier die einzige
            // Summendarstellung und muss deshalb sichtbar obenauf liegen. Das Problem der
            // chronologischen Ansicht besteht hier nicht: Eine Dauerlinie fällt monoton,
            // benachbarte Punkte liegen dicht beieinander, die Linie bleibt eine Linie.
            //
            // Sie wird SCHMALER gezeichnet als die Erzeugerlinien darunter (2 gegen 4) —
            // in einem Projekt mit nur einem Erzeuger sind beide Dauerlinien punktgleich,
            // und vom Breiteren bleibt links und rechts ein Rand stehen. Strichelung wäre
            // der übliche zweite Weg, ist aber bei FastLine wirkungslos.
            if (_sortiert)
            {
                SerieAnlegen(S_GESAMT, MyResource.Resource.CHART_LEGENDE_GESAMT, Color.Green,
                             temp_ges, SeriesChartType.FastLine, GESAMT_DAUERLINIE_BREITE);
            }

            // Ausgangszustand: nur "Gesamt" an. ApplyCheckboxStates stellt danach den
            // tatsächlichen Stand der Checkboxen wieder her.
            foreach (Series s in _chartManager._chart.Series)
                if (s.Name != S_GESAMT) s.Enabled = false;
        }

        /// <summary>
        /// Werte in der aktuellen Darstellungsform: chronologisch oder — für die
        /// Dauerlinie — absteigend sortiert. Die Regel selbst steht in
        /// <see cref="GanglinienDarstellung.Anzeigewerte"/>, damit Navigatoren und
        /// Detailansicht dieselbe verwenden.
        /// </summary>
        private float[] Anzeigewerte(float[] werte)
        {
            return GanglinienDarstellung.Anzeigewerte(werte, _sortiert);
        }

        /// <summary>
        /// Legt eine Chart-Serie an: <b>Name</b> = technischer Schlüssel (sprachneutral),
        /// <b>LegendText</b> = übersetzter Anzeigetext. Dieselbe Trennung, die die
        /// Speicherserien seit Paket 7 haben (Konzept 13.3).
        ///
        /// <paramref name="typ"/> überschreibt den Serientyp, den
        /// <see cref="ChartManager.AddSeries(string, Color, float[])"/> vergibt
        /// (<c>FastLine</c>). Das ist nötig, weil <c>FastLine</c> nicht stapeln kann —
        /// und weil der Serientyp zugleich über die ZEICHENLAGE entscheidet (MS-Chart
        /// zeichnet in Typgruppen, siehe Blockkommentar in <see cref="SerienAufbauen"/>).
        ///
        /// <paramref name="breite"/> = 0 belässt die Linienbreite, die
        /// <c>AddSeries</c> vergibt (2).
        /// </summary>
        private void SerieAnlegen(string schluessel, string legende, Color farbe, float[] werte,
                                  SeriesChartType typ = SeriesChartType.FastLine, int breite = 0)
        {
            _chartManager.AddSeries(schluessel, farbe, Anzeigewerte(werte));
            Series s = _chartManager._chart.Series[schluessel];
            s.LegendText = legende;
            GanglinienDarstellung.StapelEinstellen(s, typ, null);
            if (breite > 0) s.BorderWidth = breite;
        }

        // ====================================================================
        //  Checkbox-Zeile: nur vorhandene Komponenten
        // ====================================================================
        //
        // Unter dem Diagramm standen bisher IMMER alle Schalter — auch „Solarthermie" und
        // „BHKW" in einem Projekt aus Wärmepumpe, Kessel und Puffer. Sie waren dort ohne
        // Wirkung, weil die zugehörige Serie durchweg null ist.
        //
        // Die Schalter fehlender Komponenten werden deshalb AUSGEBLENDET (nicht gesperrt),
        // und die verbleibenden rücken in der Zeile nach links nach. Die Positionen
        // entstehen dabei aus den tatsächlichen Breiten (AutoSize) — dieselbe
        // programmatische Platzierung, mit der schon „Speicherfüllstand" und die
        // Speicherauswahl angelegt werden; Designer und .resx bleiben unangetastet.

        /// <summary>
        /// Blendet die Schalter fehlender Komponenten aus und ordnet die Zeile neu.
        /// </summary>
        private void CheckboxenAnordnen()
        {
            this.SuspendLayout();
            _imAufbau = true;

            // Erste Zeile: "Gesamt" ist der feste Anfang, alles Übrige rückt dahinter auf.
            // Die Präsenz wird HIER mitgeführt und nicht aus Control.Visible zurückgelesen:
            // dessen Getter liefert false, solange das Elternelement noch nicht angezeigt
            // wird - beim Aufbau im Hintergrund wäre die ganze Zeile "unsichtbar" und
            // niemand rückte nach.
            var zeile = new[]
            {
                new { Schalter = checkBox_Gesamt,   Da = true },                  // Summe, immer
                new { Schalter = checkBox_WP,       Da = _praesenz.Waermepumpe },
                new { Schalter = checkBox_Heizstab, Da = _praesenz.Heizstab },
                new { Schalter = checkBox_SPK,      Da = _praesenz.Heizkessel },
                new { Schalter = checkBox_ST,       Da = _praesenz.Solarthermie },
                new { Schalter = checkBox_BHKW,     Da = _praesenz.BHKW },
                new { Schalter = checkBox_Puffer,   Da = _praesenz.Speicher }
            };

            int links = checkBox_Gesamt.Left;
            int oben = checkBox_Gesamt.Top;
            foreach (var e in zeile)
            {
                if (e.Schalter == null) continue;

                // Ein ausgeblendeter Schalter wird zugleich abgewählt - sonst bliebe ein
                // unsichtbares Checked stehen und der CSV-Export nähme eine Spalte mit,
                // die niemand sieht.
                if (!e.Da) e.Schalter.Checked = false;
                e.Schalter.Visible = e.Da;
                if (!e.Da) continue;

                e.Schalter.Location = new Point(links, oben);
                links = e.Schalter.Right + CHK_ABSTAND;
            }

            // Zweite Zeile: "Wärmebedarf einblenden", "sortiert", Speicherauswahl.
            if (checkBox_Sortiert != null)
                checkBox_Sortiert.Location = new Point(checkBox_Waermebedarf.Right + CHK_ABSTAND,
                                                       checkBox_Waermebedarf.Top);
            if (comboBox_Puffer != null)
            {
                Control davor = (checkBox_Sortiert != null) ? (Control)checkBox_Sortiert : checkBox_Waermebedarf;
                comboBox_Puffer.Location = new Point(davor.Right + CHK_ABSTAND, checkBox_Waermebedarf.Top - 2);
            }

            _imAufbau = false;
            this.ResumeLayout();
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
            return _praesenz.Speicher && speicherListe.Count > 1;
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
                SerieSchalten(S_GESAMT, checkBox_Gesamt.Checked);
                SerieSchalten(S_WAERMEPUMPE, checkBox_WP.Checked);
                SerieSchalten(S_HEIZSTAB, checkBox_Heizstab.Checked);
                SerieSchalten(S_HEIZKESSEL, checkBox_SPK.Checked);
                SerieSchalten(S_SOLARTHERMIE, checkBox_ST.Checked);
                SerieSchalten(S_BHKW, checkBox_BHKW.Checked);
                SpeicherSerienAktualisieren();
            }
        }

        /// <summary>
        /// Schaltet eine Serie, sofern es sie gibt. Seit der Präsenzfilterung entstehen
        /// die Serien fehlender Erzeuger nicht mehr — ein ungeprüfter Zugriff über
        /// <c>Series["…"]</c> liefe dann ins Leere.
        /// </summary>
        private void SerieSchalten(string schluessel, bool an)
        {
            if (_imAufbau) return;
            if (_chartManager == null || _chartManager._chart == null) return;
            if (_chartManager._chart.Series.IndexOf(schluessel) < 0) return;
            _chartManager._chart.Series[schluessel].Enabled = an;
        }

        private void checkBox_Puffer_CheckedChanged(object sender, EventArgs e)
        {
            if (_imAufbau) return;
            SpeicherSerienAktualisieren();
        }

        private void checkBox_Gesamt_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_GESAMT, checkBox_Gesamt.Checked);
        }

        private void checkBox_WP_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_WAERMEPUMPE, checkBox_WP.Checked);
        }

        private void checkBox_Heizstab_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_HEIZSTAB, checkBox_Heizstab.Checked);
        }

        private void checkBox_SPK_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_HEIZKESSEL, checkBox_SPK.Checked);
        }

        private void checkBox_ST_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_SOLARTHERMIE, checkBox_ST.Checked);
        }

        private void checkBox_BHKW_CheckedChanged(object sender, EventArgs e)
        {
            SerieSchalten(S_BHKW, checkBox_BHKW.Checked);
        }

        /// <summary>
        /// Umschalter Jahresganglinie &lt;-&gt; Jahresdauerlinie. Baut die Serien in der
        /// neuen Darstellungsform auf und stellt danach den Stand der Checkboxen wieder
        /// her — die Auswahl des Anwenders überlebt das Umschalten.
        /// </summary>
        private void checkBox_Sortiert_CheckedChanged(object sender, EventArgs e)
        {
            if (_imAufbau || _chartManager == null || temp_ges == null) return;

            _sortiert = checkBox_Sortiert.Checked;
            SerienAufbauen();
            ApplyCheckboxStates();
            WaermebedarfAchseAktualisieren();
            _chartManager._chart.Invalidate();
        }

        private void checkBox_Waermebedarf_CheckedChanged(object sender, EventArgs e)
        {
            WaermebedarfAchseAktualisieren();
        }

        /// <summary>
        /// Schaltet die Bedarfsserie samt zweiter Y-Achse. Ausgelagert, weil die
        /// Einstellungen nach jedem <c>HardReset()/Init()</c> neu gesetzt werden müssen —
        /// also auch nach dem Umschalten auf die Dauerlinie.
        /// </summary>
        private void WaermebedarfAchseAktualisieren()
        {
            if (_imAufbau || _chartManager == null || _chartManager._chart == null) return;
            if (_chartManager._chart.ChartAreas.Count == 0) return;
            if (_chartManager._chart.Series.IndexOf(S_WAERMEBEDARF) < 0) return;

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
