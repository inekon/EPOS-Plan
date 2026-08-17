using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Quellendialog "Erdreich" einer Wärmepumpe (Sole-Wasser / Wasser-Wasser),
    /// Aufbau nach Konzept-Mockup 4.5.
    ///
    /// Erdkollektor - Verlegetiefe und Fläche; die Quelltemperatur folgt dem
    /// gedämpften und phasenverschobenen Jahresgang nach Kusuda.
    /// Erdsonde - Länge je Sonde und Anzahl; die Quelltemperatur ist konstant,
    /// weil der Jahresgang ab der neutralen Zone abgeklungen ist.
    ///
    /// Der 8760er-Außentemperaturvektor wird vom Aufrufer übergeben und beim
    /// Öffnen einmal verwendet - die Vorschau rechnet bei Parameteränderungen nur
    /// noch aus dem gecachten Vektor, ohne erneuten Datenbankzugriff (Konzept 4.5).
    ///
    /// Die Auslegungsprüfung nach VDI 4640 Blatt 2 braucht Simulationsergebnisse.
    /// Sie kommen entweder vom Aufrufer (Ergebnisse eines früheren Laufs der Sitzung)
    /// oder aus einem Lauf, den der Anwender hier selbst anstößt - Schaltfläche
    /// „Simulation", siehe <see cref="btnSimulation_Click"/>. Liegt beides nicht vor,
    /// bleibt die Prüfung leer und sagt das an. Stehen die Eingaben nicht mehr auf dem
    /// Stand, mit dem gerechnet wurde, warnt eine Hinweiszeile
    /// (<see cref="AenderungshinweisAktualisieren"/>).
    ///
    /// Das Formular wird - wie Form_QuellePufferspeicher und Form_Quellprofil -
    /// komplett programmatisch aufgebaut (kein Designer, keine .resx). Die sichtbaren
    /// Texte kommen seit Paket 9 / L7 aus dem Ressourcenkatalog
    /// (<c>MyResource.Resource.SIMQ_ERDREICH_*</c>, Konzept 13.6); Bodentyp-Schlüssel
    /// und Quellsystem bleiben deutsche Persistenzwerte aus <see cref="DbWerte"/>
    /// (Drei-Schichten-Regel).
    /// </summary>
    public class Form_QuelleErdreich : Form
    {
        // ---- Übergabefelder (öffentlich, wie im Bestandsmuster) -----------

        /// <summary>Name der Wärmepumpe (nur für den Fenstertitel).</summary>
        public string WPName = "";

        /// <summary>
        /// Tab_Projekt.ID des Projekts, zu dem diese Wärmequelle gehört. Wird nur für
        /// die Schaltfläche „Simulation" gebraucht (Befund 3 vom 17.08.2026): Ohne
        /// Projektbezug gibt es keinen Lauf, den der Dialog anstoßen könnte.
        ///
        /// 0 = nicht gesetzt. Dann versucht <see cref="ProjektErmitteln"/> den Bezug
        /// über das besitzende Formular zu bestimmen; siehe dort, warum der Aufrufer
        /// dieses Feld derzeit nicht selbst belegt.
        /// </summary>
        public int ID_Projekt = 0;

        /// <summary>
        /// Tab_Energieanlagen.ID der Wärmepumpe (Muster <c>Form_Waermesenke.ID_Anlage</c>).
        /// Sie ordnet die Ergebnisse eines Laufs eindeutig dieser Anlage zu
        /// (<see cref="ErdreichAuswertung.AnlageErgebnis.ID_Anlage"/>).
        ///
        /// 0 = nicht gesetzt. Dann fällt <see cref="ErgebnisDesLaufs"/> auf den
        /// Modulnamen und, wenn das Projekt nur eine Erdreichquelle führt, auf deren
        /// einziges Ergebnis zurück.
        /// </summary>
        public int ID_Anlage = 0;

        /// <summary>Quellsystem: ErdreichTemperatur.QUELLSYSTEM_KOLLEKTOR | _SONDE.</summary>
        public string Quellsystem = ErdreichTemperatur.QUELLSYSTEM_KOLLEKTOR;

        /// <summary>Verlegetiefe des Kollektors bzw. Länge je Sonde [m].</summary>
        public double Tiefe = ErdreichTemperatur.TIEFE_DEFAULT;

        /// <summary>Kollektorfläche [m²].</summary>
        public double Flaeche = 0;

        /// <summary>Anzahl Sonden.</summary>
        public int Anzahl = 1;

        /// <summary>Katalogschlüssel des Bodentyps (VDI 4640 Blatt 1).</summary>
        public string Bodentyp = ErdreichTemperatur.BODENTYP_DEFAULT;

        /// <summary>Klimazone 1…15 nach DIN 4710; 0 = nicht zugeordnet.</summary>
        public int Klimazone = 0;

        /// <summary>
        /// Nutzbare Spreizung der Quelle [K] (WQ_Spreizung). Sie ist die Temperatur-
        /// differenz zwischen Quelleintritt und -austritt und geht in die zweite
        /// Warnbedingung aus Konzept 13.1 ein: gewarnt wird, wenn
        /// „Quelltemperatur − Spreizung" dauerhaft unter 0 °C liegt.
        ///
        /// Bis Paket 7 war der Wert nur über den Pufferspeicher-Quellendialog pflegbar -
        /// bei einer Erdreichquelle gab es gar keine Eingabemöglichkeit und die Prüfung
        /// rechnete immer mit der Vorgabe von 5 K.
        /// </summary>
        public double Spreizung = ErdreichAuswertung.SPREIZUNG_DEFAULT;

        /// <summary>
        /// Außentemperatur der Klimaregion (8760 Stundenwerte). Wird vom Aufrufer
        /// gesetzt; fehlt der Vektor, rechnet das Modell mit Ersatzwerten weiter.
        /// </summary>
        public float[] Aussentemperatur = null;

        // ---- Ergebnisse eines Simulationslaufs (Auslegungsprüfung) --------

        /// <summary>true, wenn Ergebnisse eines Simulationslaufs vorliegen.</summary>
        public bool ErgebnisseVorhanden = false;

        /// <summary>Maximale Entzugsleistung der Quelle [W].</summary>
        public double MaxEntzugW = 0;

        /// <summary>Jahresentzugsarbeit der Quelle [kWh/a].</summary>
        public double JahresentzugKWh = 0;

        /// <summary>Jahresvolllaststunden der Wärmepumpe [h/a].</summary>
        public double VolllastStunden = 0;

        /// <summary>
        /// Grund, aus dem die Prüfung nicht mit Ergebnissen versorgt werden konnte
        /// (Paket 7): entweder „noch kein Lauf" oder die Grenze der Zuordnung
        /// (mehrere Wärmepumpen mit unterschiedlichen Quellen). Leer = Vorgabetext.
        /// </summary>
        public string HinweisErgebnis = "";

        /// <summary>
        /// Vorbehalt zu belastbaren Ergebnissen (z. B. „Spitze anteilig aus der
        /// Summenganglinie geschätzt"). Wird unter die Prüfung geschrieben.
        /// </summary>
        public string HinweisVorbehalt = "";

        /// <summary>
        /// Meldung der zweiten Warnbedingung (Konzept 13.1) samt Normbasis. Steht
        /// bewusst getrennt vom Prüfergebnis: „Grenzwert eingehalten" und eine
        /// Frostmeldung schließen einander nicht aus, weil VDI 4640 Bl. 2 gegen
        /// −5 °C Soleaustritt bemisst.
        /// </summary>
        public string HinweisFrost = "";

        // ---- Steuerelemente -----------------------------------------------

        private RadioButton _rbKollektor;
        private RadioButton _rbSonde;
        private TextBox _tbTiefe;
        private TextBox _tbFlaeche;
        private TextBox _tbLaenge;
        private TextBox _tbAnzahl;
        private ComboBox _cbBoden;
        private ComboBox _cbZone;
        private TextBox _tbSpreizung;
        private Chart _chart;
        private Label _lblKennwerte;
        private Label _lblBoden;
        private Label _lblPruefung;
        private Label _lblAenderung;
        private Button _btnSimulation;

        private bool _uiAufbau = true;   // unterdrückt Ereignisse während SetControls

        /// <summary>
        /// Zustand der Eingabefelder, wie ihn <see cref="SetControls"/> vorgefunden hat -
        /// also der Stand, der in der Datenbank steht (Befund 4 vom 17.08.2026).
        ///
        /// Er ist die Bezugsgröße für zwei Aussagen, die der Dialog treffen muss:
        ///   • Weicht der aktuelle Stand ab, beruht die Auslegungsprüfung auf ANDEREN
        ///     Werten als den angezeigten - dann muss gewarnt werden.
        ///   • Ein Lauf, den die Schaltfläche „Simulation" anstößt, rechnet mit den
        ///     GESPEICHERTEN Werten (die Engine liest WQ_* aus der Datenbank, nicht aus
        ///     diesem Dialog). Auch das muss der Hinweis sagen können.
        ///
        /// Verglichen wird der Text der Steuerelemente, nicht der geparste Zahlenwert:
        /// Der Text ist genau das, was der Anwender sieht, und <see cref="SetControls"/>
        /// hat ihn aus den Datenbankwerten erzeugt - beide Seiten sind damit identisch
        /// formatiert. Stellt der Anwender einen geänderten Wert wieder auf den
        /// Ausgangswert zurück, verschwindet der Hinweis von selbst.
        /// </summary>
        private string _standGeladen = "";

        /// <summary>
        /// true, sobald in diesem Dialog ein Lauf über die Schaltfläche „Simulation"
        /// durchgelaufen ist. Nur dann darf der Änderungshinweis auf „der Lauf hat mit
        /// den gespeicherten Werten gerechnet" umschalten - vorher wäre die Aussage
        /// falsch, weil der letzte Lauf dann von woanders kommt.
        /// </summary>
        private bool _laufAusDialog = false;

        /// <summary>
        /// Warnfarbe der Hinweiszeile. Derselbe Bernsteinton, den
        /// <c>Form_GanglinieProtokoll</c> für <c>PruefStufe.Warnung</c> verwendet -
        /// bewusst NICHT das Firebrick der Grenzwertüberschreitung: Ein veralteter
        /// Prüfstand ist ein Bedienhinweis, keine überschrittene Norm.
        /// </summary>
        private static readonly Color FARBE_WARNUNG = Color.FromArgb(160, 96, 0);

        // --- Technische Serienschlüssel (Paket 9 / L7) --------------------------------
        // Schicht 2 der Drei-Schichten-Regel: sprachneutral, ASCII, unveränderlich.
        // Der Anzeigetext steht ausschließlich in Series.LegendText.
        private const string S_QUELLTEMPERATUR = "QUELLTEMPERATUR";
        private const string S_AUSSENTEMPERATUR = "AUSSENTEMPERATUR";

        public Form_QuelleErdreich()
        {
            BaueOberflaeche();

            // Bereich für den KI-Hilfe-Assistenten melden (nur Bedien-Kontext,
            // keine Projekt- oder Kundendaten). Muster und Platz wie am Ende des
            // Konstruktors von Form_Simulation_Config und Form_Simulation_Detail:
            // ein reiner Kontext-Setzer am Activated-Ereignis, keine eigene
            // Hilfe-Schaltfläche - die Hilfe wird im Hauptfenster geöffnet
            // (MDIMainForm -> Form_KiChat.Oeffnen) und holt sich den Bereich dort ab.
            //
            // Der Bereichsname ist bewusst ein deutsches Literal und KEIN
            // Ressourcenschlüssel: Er ist kein sichtbarer Text, sondern Eingabe an den
            // Assistenten (HilfeKontext.Beschreibung), und beide Bestandsaufrufe halten
            // es genauso. Genannt werden die drei Dinge, nach denen der Anwender in
            // dieser Maske fragen kann.
            this.Activated += (s, e) =>
                HilfeKontext.SetzeBereich("Wärmequelle Erdreich (Quellsystem, Bodentyp, Auslegungsprüfung VDI 4640)");
        }

        /// <summary>
        /// Zahlenwert als Feldvorbelegung — kulturneutral im Quelltext, formatiert wie
        /// alle übrigen Ausgaben dieses Dialogs (<c>ToString("0.##")</c>). Gelesen wird
        /// über <see cref="WaermequelleClass.ZahlParsen"/>, das Komma UND Punkt annimmt;
        /// <c>CurrentCulture</c> wird nicht gesetzt (Konzept 13.6). Bis Paket 9 stand
        /// hier die Zeichenkette „1,5" mit hartkodiertem Dezimalkomma. Muster aus L3
        /// (<see cref="Form_Quellprofil"/>).
        /// </summary>
        private static string Vorgabe(double wert)
        {
            return wert.ToString("0.##", CultureInfo.CurrentCulture);
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        private void BaueOberflaeche()
        {
            this.Text = MyResource.Resource.SIMQ_ERDREICH_TITEL;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            // Höhe um eine Zeile gewachsen: die nutzbare Spreizung braucht ein
            // Eingabefeld (Konzept 13.1) - siehe unten bei _tbSpreizung.
            //
            // BEFUNDE 1/3/4 vom 17.08.2026 - die Höhe wächst von 718 auf 748. Dazu
            // gekommen sind rund 56 Pixel:
            //   +14  die Bodenkennwerte brauchen zwei Zeilen (siehe _lblBoden),
            //   + 8  der Spreizungs-Hinweis bricht um (siehe lSH),
            //   +38  die Auslegungsprüfung bekommt Hinweiszeile und Schaltfläche.
            // Gegengerechnet sind 26 Pixel aus der Vorschau: Das Diagramm ist von 210
            // auf 184 Pixel Höhe verkleinert (siehe _chart). Das ist Absicht und der
            // Preis dafür, dass der Dialog NICHT über die Fensterhöhe hinauswächst, die
            // Windows auf einem 1366×768-Gerät noch zulässt (dort endet die zulässige
            // Fensterhöhe bei etwa 788 Pixeln; mit Titelzeile und Rahmen liegt dieser
            // Dialog bei 787). Ohne die Gegenrechnung wären OK und Abbrechen dort unter
            // dem unteren Bildschirmrand verschwunden - ein schlechterer Fehler als der,
            // der hier behoben wird.
            //
            // Die Breite bleibt bei 700; alle Beschriftungen passen jetzt hinein
            // (nachgemessen für Deutsch und Englisch, siehe _lblBoden und lSH).
            this.ClientSize = new Size(700, 748);

            // --- Quellsystem ------------------------------------------------
            GroupBox gbSystem = new GroupBox
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_GB_QUELLSYSTEM,
                Location = new Point(12, 10),
                Size = new Size(676, 120)
            };
            this.Controls.Add(gbSystem);

            _rbKollektor = new RadioButton
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_RB_KOLLEKTOR,
                AutoSize = true,
                Checked = true,
                Location = new Point(16, 26)
            };
            _rbSonde = new RadioButton
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_RB_SONDE,
                AutoSize = true,
                Location = new Point(16, 76)
            };
            _rbKollektor.CheckedChanged += (s, e) => { SystemUmschalten(); Aktualisieren(); };
            _rbSonde.CheckedChanged += (s, e) => { SystemUmschalten(); Aktualisieren(); };

            Label lT = new Label { Text = MyResource.Resource.SIMQ_ERDREICH_VERLEGETIEFE, AutoSize = true, Location = new Point(160, 28) };
            _tbTiefe = new TextBox { Location = new Point(285, 25), Width = 70, Text = Vorgabe(ErdreichTemperatur.TIEFE_DEFAULT) };
            Label lF = new Label { Text = MyResource.Resource.SIMQ_ERDREICH_FLAECHE, AutoSize = true, Location = new Point(390, 28) };
            _tbFlaeche = new TextBox { Location = new Point(490, 25), Width = 70, Text = "0" };

            Label lL = new Label { Text = MyResource.Resource.SIMQ_ERDREICH_LAENGE_SONDE, AutoSize = true, Location = new Point(160, 78) };
            _tbLaenge = new TextBox { Location = new Point(285, 75), Width = 70, Text = "90" };
            Label lA = new Label { Text = MyResource.Resource.SIMQ_ERDREICH_ANZAHL_SONDEN, AutoSize = true, Location = new Point(390, 78) };
            _tbAnzahl = new TextBox { Location = new Point(490, 75), Width = 70, Text = "1" };

            _tbTiefe.TextChanged += (s, e) => Aktualisieren();
            _tbFlaeche.TextChanged += (s, e) => Aktualisieren();
            _tbLaenge.TextChanged += (s, e) => Aktualisieren();
            _tbAnzahl.TextChanged += (s, e) => Aktualisieren();

            gbSystem.Controls.Add(_rbKollektor);
            gbSystem.Controls.Add(lT); gbSystem.Controls.Add(_tbTiefe);
            gbSystem.Controls.Add(lF); gbSystem.Controls.Add(_tbFlaeche);
            gbSystem.Controls.Add(_rbSonde);
            gbSystem.Controls.Add(lL); gbSystem.Controls.Add(_tbLaenge);
            gbSystem.Controls.Add(lA); gbSystem.Controls.Add(_tbAnzahl);

            // --- Bodentyp und Klimazone -------------------------------------
            Label lB = new Label { Text = MyResource.Resource.SIMQ_ERDREICH_BODENTYP, AutoSize = true, Location = new Point(28, 145) };
            _cbBoden = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 142),
                Width = 230
            };
            _cbBoden.Items.AddRange(ErdreichTemperatur.KatalogAnzeige());
            _cbBoden.SelectedIndexChanged += (s, e) => Aktualisieren();
            Label lBH = new Label
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_BODENTYP_HINWEIS,
                AutoSize = true,
                Location = new Point(392, 145)
            };

            // BEFUND 1 vom 17.08.2026 („Text nicht sichtbar"): Die Kennwertzeile stand in
            // einem 530 Pixel breiten Feld ab x=150 und wurde hart abgeschnitten -
            // gemessen brauchte sie 635 Pixel, sichtbar endete sie mitten in
            // „Bodenart nach Tabelle A1: …". Zwei Änderungen beheben das:
            //   • Sie beginnt jetzt am linken Rand (x=28) und nutzt die volle Breite
            //     von 660 Pixeln statt 530.
            //   • Sie bekommt Platz für ZWEI Zeilen (32 statt 18 Pixel). Nötig ist das
            //     für den längsten Fall: Mit der Bodenart „Sandiger Ton" statt „Sand"
            //     wächst der Text auf rund 683 Pixel und läuft damit auch über die
            //     volle Breite hinaus. Bei kurzen Bodenarten bleibt es optisch eine
            //     Zeile - AutoSize=false bricht nur um, wenn es sein muss.
            _lblBoden = new Label
            {
                AutoSize = false,
                Location = new Point(28, 170),
                Size = new Size(660, 32),
                ForeColor = SystemColors.GrayText
            };

            // Ab hier liegt jede Zeile 14 Pixel tiefer als vorher - genau die zweite
            // Zeile, die _lblBoden dazubekommen hat.
            Label lZ = new Label { Text = MyResource.Resource.SIMQ_ERDREICH_KLIMAZONE, AutoSize = true, Location = new Point(28, 212) };
            _cbZone = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 209),
                Width = 230
            };
            _cbZone.Items.Add(MyResource.Resource.SIMQ_ERDREICH_ZONE_NICHT_ZUGEORDNET);
            for (int z = 1; z <= VDI4640Pruefung.KLIMAZONEN; z++)
            {
                _cbZone.Items.Add(z.ToString(CultureInfo.CurrentCulture) + " — " +
                    VDI4640Pruefung.VolllaststundenZone(z).ToString("N0", CultureInfo.CurrentCulture) + " h/a");
            }
            _cbZone.SelectedIndexChanged += (s, e) => Aktualisieren();
            Label lZH = new Label
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_KLIMAZONE_HINWEIS,
                AutoSize = true,
                Location = new Point(392, 212)
            };

            // --- Nutzbare Spreizung ------------------------------------------
            // Eingangsgröße der zweiten Warnbedingung (Konzept 13.1). Ohne dieses Feld
            // war WQ_Spreizung bei einer Erdreichquelle nicht pflegbar und die Prüfung
            // rechnete immer mit 5 K.
            Label lS = new Label { Text = MyResource.Resource.SIMQ_ERDREICH_SPREIZUNG, AutoSize = true, Location = new Point(28, 242) };
            _tbSpreizung = new TextBox
            {
                Location = new Point(150, 239),
                Width = 70,
                Text = ErdreichAuswertung.SPREIZUNG_DEFAULT.ToString("0.##", CultureInfo.CurrentCulture)
            };
            // BEFUND 1 vom 17.08.2026 („Text nicht sichtbar"): Dieser Hinweis ist der
            // Hauptbefund. Er ist gemessen 564 Pixel breit, begann bei x=232 und endete
            // damit bei 796 - also 96 Pixel HINTER dem rechten Dialogrand (700). Sichtbar
            // brach er mitten in „…Quelltemperatur − Spreizung dauerh" ab.
            //
            // Er bleibt an seinem Platz hinter dem Eingabefeld (das ist die Zuordnung,
            // die der Anwender erwartet) und darf jetzt UMBRECHEN: MaximumSize begrenzt
            // die Breite auf die 456 Pixel, die bis zum rechten Rand frei sind, AutoSize
            // lässt ihn dafür in die Höhe wachsen. Das ist das MaximumSize/AutoSize-
            // Muster und die kleinstmögliche Änderung - Position und Reihenfolge der
            // Steuerelemente bleiben, wie sie waren.
            //
            // Deutsch und Englisch belegen damit zwei Zeilen (rund 30 Pixel). Bis zur
            // Vorschau-Gruppe (y=290) sind ab y=242 aber 48 Pixel frei, also Platz für
            // DREI Zeilen - Reserve für längere Übersetzungen.
            Label lSH = new Label
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_SPREIZUNG_HINWEIS,
                AutoSize = true,
                MaximumSize = new Size(456, 0),
                Location = new Point(232, 242),
                ForeColor = SystemColors.GrayText
            };
            _tbSpreizung.TextChanged += (s, e) => Aktualisieren();

            this.Controls.Add(lB); this.Controls.Add(_cbBoden); this.Controls.Add(lBH);
            this.Controls.Add(_lblBoden);
            this.Controls.Add(lZ); this.Controls.Add(_cbZone); this.Controls.Add(lZH);
            this.Controls.Add(lS); this.Controls.Add(_tbSpreizung); this.Controls.Add(lSH);

            // --- Vorschau ----------------------------------------------------
            GroupBox gbVorschau = new GroupBox
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_GB_VORSCHAU,
                Location = new Point(12, 280),
                Size = new Size(676, 244)
            };
            this.Controls.Add(gbVorschau);

            // 184 statt 210 Pixel hoch: die 26 Pixel gehen an die Zeilen, die Befund 1
            // und Befund 3/4 unten brauchen - siehe die Begründung bei ClientSize. Für
            // einen Jahresgang über zwölf Monate bleibt das Seitenverhältnis 652×184
            // gut lesbar; die Zoom-Bedienung des Diagramms ist unberührt.
            _chart = new Chart
            {
                Location = new Point(12, 20),
                Size = new Size(652, 184)
            };
            ChartArea ca = new ChartArea("Jahr");
            ca.AxisX.Title = MyResource.Resource.CHART_ACHSE_MONAT;
            ca.AxisY.Title = MyResource.Resource.CHART_ACHSE_QUELLTEMPERATUR;
            ca.AxisX.Minimum = 0;
            ca.AxisX.Maximum = 12;
            ca.AxisX.Interval = 1;
            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.AxisX.ScaleView.Zoomable = true;
            _chart.ChartAreas.Add(ca);

            // FastLine: 8760 Punkte je Neuzeichnung (Konzept 4.5).
            // Series.Name ist der technische Schlüssel (Schicht 2), der Anzeigetext steht
            // in LegendText — Muster wie NavigatorWaerme (Paket 9 / L6).
            Series sQuelle = new Series(S_QUELLTEMPERATUR)
            {
                ChartType = SeriesChartType.FastLine,
                Color = Color.FromArgb(200, Color.SaddleBrown),
                BorderWidth = 2,
                XValueType = ChartValueType.Double,
                LegendText = MyResource.Resource.CHART_SERIE_QUELLTEMPERATUR
            };
            _chart.Series.Add(sQuelle);

            Series sAussen = new Series(S_AUSSENTEMPERATUR)
            {
                ChartType = SeriesChartType.FastLine,
                Color = Color.FromArgb(90, Color.SteelBlue),
                BorderWidth = 1,
                XValueType = ChartValueType.Double,
                LegendText = MyResource.Resource.CHART_SERIE_AUSSENTEMPERATUR
            };
            _chart.Series.Add(sAussen);

            _chart.Legends.Add(new Legend("L") { Docking = Docking.Top, Alignment = StringAlignment.Center });
            sQuelle.Legend = "L";
            sAussen.Legend = "L";

            _lblKennwerte = new Label
            {
                AutoSize = false,
                Location = new Point(14, 210),
                Size = new Size(650, 20),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            gbVorschau.Controls.Add(_chart);
            gbVorschau.Controls.Add(_lblKennwerte);

            // --- Auslegungsprüfung -------------------------------------------
            // Die Gruppe ist von 130 auf 168 Pixel gewachsen: unter das Prüfergebnis
            // kommen die Hinweiszeile (Befund 4) und die Schaltfläche „Simulation"
            // (Befund 3) - beide gehören sachlich hierher und nirgends sonst hin.
            GroupBox gbPruefung = new GroupBox
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_GB_PRUEFUNG,
                Location = new Point(12, 532),
                Size = new Size(676, 168)
            };
            this.Controls.Add(gbPruefung);

            _lblPruefung = new Label
            {
                AutoSize = false,
                Location = new Point(14, 22),
                Size = new Size(650, 100),
                Font = new Font(FontFamily.GenericMonospace, 8.25f)
            };
            gbPruefung.Controls.Add(_lblPruefung);

            // BEFUND 4 vom 17.08.2026: Sobald der Anwender eine Quell-Einstellung ändert,
            // zeigt die Prüfung oben noch den Stand des LETZTEN Laufs. Ohne Hinweis liest
            // sich das wie eine Bewertung der neuen Eingaben - sie ist es aber nicht.
            // Die Zeile bleibt leer, solange Anzeige und Lauf zusammenpassen; welchen der
            // beiden Texte sie sonst zeigt, entscheidet AenderungshinweisAktualisieren.
            //
            // AutoSize=false mit zwei Zeilen Höhe: der Text bricht dann von selbst um und
            // schiebt die Schaltfläche daneben nicht weg (Lehre aus Befund 1).
            _lblAenderung = new Label
            {
                AutoSize = false,
                Location = new Point(14, 128),
                Size = new Size(500, 34),
                ForeColor = FARBE_WARNUNG
            };
            gbPruefung.Controls.Add(_lblAenderung);

            // BEFUND 3 vom 17.08.2026 („Simulation nur für diesen Bereich"): Die Prüfung
            // war bisher nur zu füllen, indem der Anwender den Dialog verließ und den
            // großen Simulationsweg ging. Der Knopf rechnet sie hier - was genau er tut
            // und was er bewusst NICHT tut, steht bei btnSimulation_Click.
            _btnSimulation = new Button
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_BTN_SIMULATION,
                Location = new Point(528, 126),
                Size = new Size(134, 28)
            };
            _btnSimulation.Click += btnSimulation_Click;
            gbPruefung.Controls.Add(_btnSimulation);

            // --- Schaltflächen ------------------------------------------------
            Button btnOk = new Button
            {
                Text = MyResource.Resource.SIM_BTN_OK,
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 190, 712),
                Width = 85
            };
            Button btnAbbruch = new Button
            {
                Text = MyResource.Resource.SIM_BTN_ABBRECHEN,
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 97, 712),
                Width = 85
            };
            btnOk.Click += btnOk_Click;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbruch);
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbruch;
        }

        // ------------------------------------------------------------------
        // Vorbelegung
        // ------------------------------------------------------------------

        /// <summary>
        /// Belegt die Steuerelemente aus den öffentlichen Feldern und zeichnet die
        /// Vorschau ein erstes Mal. Vor ShowDialog aufzurufen.
        /// </summary>
        public void SetControls()
        {
            _uiAufbau = true;

            if (!string.IsNullOrEmpty(WPName))
                this.Text = string.Format(MyResource.Resource.SIMQ_ERDREICH_TITEL_MIT_WP, WPName);

            bool sonde = string.Equals(Quellsystem, ErdreichTemperatur.QUELLSYSTEM_SONDE,
                                       StringComparison.OrdinalIgnoreCase);
            _rbSonde.Checked = sonde;
            _rbKollektor.Checked = !sonde;

            if (sonde)
            {
                _tbLaenge.Text = (Tiefe > 0 ? Tiefe : 90).ToString("0.##", CultureInfo.CurrentCulture);
                _tbTiefe.Text = ErdreichTemperatur.TIEFE_DEFAULT.ToString("0.##", CultureInfo.CurrentCulture);
            }
            else
            {
                _tbTiefe.Text = (Tiefe > 0 ? Tiefe : ErdreichTemperatur.TIEFE_DEFAULT)
                    .ToString("0.##", CultureInfo.CurrentCulture);
                _tbLaenge.Text = "90";
            }

            _tbFlaeche.Text = Flaeche.ToString("0.##", CultureInfo.CurrentCulture);
            _tbAnzahl.Text = (Anzahl > 0 ? Anzahl : 1).ToString(CultureInfo.CurrentCulture);

            int bi = ErdreichTemperatur.KatalogIndex(Bodentyp);
            _cbBoden.SelectedIndex = bi >= 0 ? bi : ErdreichTemperatur.KatalogIndex(ErdreichTemperatur.BODENTYP_DEFAULT);

            _cbZone.SelectedIndex = (Klimazone >= 0 && Klimazone <= VDI4640Pruefung.KLIMAZONEN) ? Klimazone : 0;

            _tbSpreizung.Text = (Spreizung > 0 ? Spreizung : ErdreichAuswertung.SPREIZUNG_DEFAULT)
                .ToString("0.##", CultureInfo.CurrentCulture);

            _uiAufbau = false;

            // Ausgangsstand festhalten, BEVOR der Anwender etwas ändern kann (Befund 4).
            // Er ist der Stand der Datenbank - alles darüber siehe _standGeladen.
            _standGeladen = Eingabestand();
            _laufAusDialog = false;

            SystemUmschalten();
            Aktualisieren();
        }

        /// <summary>Aktiviert die Eingabefelder des gewählten Quellsystems.</summary>
        private void SystemUmschalten()
        {
            bool kollektor = _rbKollektor.Checked;
            _tbTiefe.Enabled = kollektor;
            _tbFlaeche.Enabled = kollektor;
            _tbLaenge.Enabled = !kollektor;
            _tbAnzahl.Enabled = !kollektor;
        }

        // ------------------------------------------------------------------
        // Vorschau und Prüfung
        // ------------------------------------------------------------------

        /// <summary>
        /// Zeichnet Jahresgang, Kennwerte und Auslegungsprüfung neu. Rechnet
        /// ausschließlich aus dem gecachten Außentemperaturvektor.
        /// </summary>
        private void Aktualisieren()
        {
            if (_uiAufbau) return;

            string bodenSchluessel = AktuellerBodentyp();
            ErdreichTemperatur.Bodenkennwerte boden = ErdreichTemperatur.Bodentyp(bodenSchluessel);

            float tiefe, flaeche, laenge, anzahl;
            WaermequelleClass.ZahlParsen(_tbTiefe.Text, out tiefe);
            WaermequelleClass.ZahlParsen(_tbFlaeche.Text, out flaeche);
            WaermequelleClass.ZahlParsen(_tbLaenge.Text, out laenge);
            WaermequelleClass.ZahlParsen(_tbAnzahl.Text, out anzahl);

            float[] profil = _rbSonde.Checked
                ? ErdreichTemperatur.JahresprofilSonde(Aussentemperatur, laenge)
                : ErdreichTemperatur.JahresprofilKollektor(Aussentemperatur, tiefe, bodenSchluessel);

            // Kennwerte des Bodens
            // Die Formatangaben (0.0 / 0.00) kommen aus dem Quelltext; der Katalog führt
            // die Platzhalter normalisiert als {0}…{4} (Lesehinweis des Katalogs). Sie
            // werden deshalb hier auf die Werte angewandt, nicht auf die Formatzeichenkette.
            _lblBoden.Text = string.Format(CultureInfo.CurrentCulture,
                MyResource.Resource.SIMQ_ERDREICH_BODENKENNWERTE,
                boden.Lambda.ToString("0.0", CultureInfo.CurrentCulture),
                boden.RhoCp.ToString("0.00", CultureInfo.CurrentCulture),
                boden.A_mm2s.ToString("0.00", CultureInfo.CurrentCulture),
                boden.Daempfungstiefe.ToString("0.00", CultureInfo.CurrentCulture),
                VDI4640Pruefung.BodenartAusBodentyp(bodenSchluessel));

            // Chart
            _chart.Series[0].Points.Clear();
            _chart.Series[1].Points.Clear();
            for (int i = 0; i < profil.Length; i++)
            {
                double x = i * 12.0 / ErdreichTemperatur.STUNDEN_JAHR;
                _chart.Series[0].Points.AddXY(x, profil[i]);
            }
            if (Aussentemperatur != null && Aussentemperatur.Length >= ErdreichTemperatur.STUNDEN_JAHR)
            {
                for (int i = 0; i < ErdreichTemperatur.STUNDEN_JAHR; i++)
                {
                    double x = i * 12.0 / ErdreichTemperatur.STUNDEN_JAHR;
                    _chart.Series[1].Points.AddXY(x, Aussentemperatur[i]);
                }
            }

            // Kennwertzeile
            ErdreichTemperatur.Kennwerte k = ErdreichTemperatur.ProfilKennwerte(profil);
            ErdreichTemperatur.Jahresgang jg = ErdreichTemperatur.AnalysiereJahresgang(Aussentemperatur);
            _lblKennwerte.Text = k.Zeile() +
                (jg.AusKlimadaten ? "" : MyResource.Resource.SIMQ_ERDREICH_OHNE_KLIMADATEN);

            PruefungAktualisieren(bodenSchluessel, tiefe, flaeche, laenge, anzahl);

            // Befund 4: Der Hinweis hängt an DIESER Stelle und nicht an den einzelnen
            // Ereignishandlern - Aktualisieren() ist der gemeinsame Weg aller sechs
            // Eingaben (Quellsystem, Verlegetiefe/Fläche, Sondenlänge/-anzahl, Bodentyp,
            // Klimazone, Spreizung), und der Rücksprung bei _uiAufbau oben stellt
            // sicher, dass die Vorbelegung selbst nichts auslöst.
            AenderungshinweisAktualisieren();
        }

        /// <summary>Füllt den Bereich der Auslegungsprüfung (Konzept 4.5/13.1).</summary>
        private void PruefungAktualisieren(string bodenSchluessel, double tiefe, double flaeche,
                                           double laenge, double anzahl)
        {
            if (!ErgebnisseVorhanden)
            {
                // Die .resx legt Umbrüche als LF ab (XML-Normierung); der Bestand schrieb
                // hier CRLF. Deshalb vor der Anzeige zurückbiegen.
                _lblPruefung.Text = !string.IsNullOrEmpty(HinweisErgebnis)
                    ? HinweisErgebnis
                    : MyResource.Resource.SIMQ_ERDREICH_PRUEFUNG_KEIN_LAUF.Replace("\n", "\r\n");
                // Zurücksetzen ist neu nötig: Seit Befund 3 kann ErgebnisseVorhanden
                // während der Lebensdauer des Dialogs umschlagen. Ohne diese Zeile
                // behielte ein Hinweistext das Firebrick einer vorher angezeigten
                // Grenzwertüberschreitung.
                _lblPruefung.ForeColor = SystemColors.ControlText;
                return;
            }

            VDI4640Pruefung.Ergebnis erg;
            if (_rbSonde.Checked)
            {
                double meter = laenge * Math.Max(1, anzahl);
                double stunden = VolllastStunden > 0 ? VolllastStunden : VDI4640Pruefung.VolllaststundenZone(AktuelleZone());
                erg = VDI4640Pruefung.PruefeSonde(
                    ErdreichTemperatur.Bodentyp(bodenSchluessel).Lambda,
                    (int)Math.Max(1, anzahl), stunden, meter, MaxEntzugW, bodenSchluessel);
            }
            else
            {
                erg = VDI4640Pruefung.PruefeKollektor(
                    AktuelleZone(), VDI4640Pruefung.BodenartAusBodentyp(bodenSchluessel),
                    flaeche, MaxEntzugW, JahresentzugKWh, bodenSchluessel);
            }

            // Der Festgesteins-Vorbehalt steht jetzt als Flag im Ergebnis (für den
            // Ergebnisausweis in Paket 7); der Dialog macht ihn zusätzlich sichtbar.
            string text = erg.Anzeigetext();
            if (erg.Moeglich && erg.FestgesteinNaeherung)
                text += MyResource.Resource.SIMQ_ERDREICH_HINWEIS_FESTGESTEIN.Replace("\n", "\r\n");
            if (!string.IsNullOrEmpty(HinweisVorbehalt))
                text += string.Format(MyResource.Resource.SIMQ_ERDREICH_HINWEIS_VORBEHALT.Replace("\n", "\r\n"),
                                      HinweisVorbehalt);
            if (!string.IsNullOrEmpty(HinweisFrost))
                text += "\r\n  " + HinweisFrost;

            _lblPruefung.Text = text;
            _lblPruefung.ForeColor = (erg.Moeglich && erg.Warnung) ? Color.Firebrick : SystemColors.ControlText;
        }

        private string AktuellerBodentyp()
        {
            int i = _cbBoden.SelectedIndex;
            if (i < 0 || i >= ErdreichTemperatur.Katalog.Length) return ErdreichTemperatur.BODENTYP_DEFAULT;
            return ErdreichTemperatur.Katalog[i].Schluessel;
        }

        private int AktuelleZone()
        {
            return _cbZone.SelectedIndex < 0 ? 0 : _cbZone.SelectedIndex;
        }

        // ------------------------------------------------------------------
        // Hinweis auf geänderte Eingaben (Befund 4 vom 17.08.2026)
        // ------------------------------------------------------------------

        /// <summary>
        /// Kennzeichnung des aktuellen Eingabestands - Vergleichsgröße für
        /// <see cref="_standGeladen"/>. Enthält alle sechs Größen, die in die
        /// Auslegungsprüfung eingehen; das trennende Zeichen ist bewusst eines, das in
        /// keinem der Werte vorkommen kann.
        /// </summary>
        private string Eingabestand()
        {
            return (_rbSonde.Checked ? "S" : "K") + "\u0001" +
                   _tbTiefe.Text + "\u0001" + _tbFlaeche.Text + "\u0001" +
                   _tbLaenge.Text + "\u0001" + _tbAnzahl.Text + "\u0001" +
                   _cbBoden.SelectedIndex.ToString(CultureInfo.InvariantCulture) + "\u0001" +
                   _cbZone.SelectedIndex.ToString(CultureInfo.InvariantCulture) + "\u0001" +
                   _tbSpreizung.Text;
        }

        /// <summary>
        /// Schreibt die Hinweiszeile der Auslegungsprüfung (Befund 4). Drei Zustände:
        ///
        ///   • Es liegt kein Lauf vor ODER die Eingaben stehen noch auf dem geladenen
        ///     Stand → kein Hinweis. Im ersten Fall sagt die Prüfung selbst schon
        ///     „(noch kein Simulationslauf)", im zweiten passen Anzeige und Lauf zusammen.
        ///
        ///   • Eingaben geändert, kein Lauf aus diesem Dialog → die Prüfung zeigt den
        ///     Stand des letzten Laufs; der Anwender muss die Simulation neu starten.
        ///
        ///   • Eingaben geändert UND hier schon gerechnet → der Lauf hat mit den
        ///     GESPEICHERTEN Werten gerechnet, weil die Engine WQ_* aus der Datenbank
        ///     liest (ErdreichAuswertung/SimulationWaermepumpe) und nicht aus diesem
        ///     Dialog. Ein Neustart des Laufs würde daran nichts ändern - deshalb steht
        ///     hier ein anderer Satz, der auf „OK" verweist. Grenzwert und Sondenmeter
        ///     rechnet PruefungAktualisieren dagegen sehr wohl mit den neuen Eingaben;
        ///     genau diese Halbheit muss der Text benennen.
        /// </summary>
        private void AenderungshinweisAktualisieren()
        {
            bool geaendert = !string.Equals(Eingabestand(), _standGeladen, StringComparison.Ordinal);

            if (!ErgebnisseVorhanden || !geaendert)
            {
                _lblAenderung.Text = "";
                return;
            }

            _lblAenderung.Text = _laufAusDialog
                ? MyResource.Resource.SIMQ_ERDREICH_SIM_NUR_GESPEICHERT
                : MyResource.Resource.SIMQ_ERDREICH_AENDERUNG_HINWEIS;
        }

        // ------------------------------------------------------------------
        // Simulationslauf aus dem Dialog (Befund 3 vom 17.08.2026)
        // ------------------------------------------------------------------

        /// <summary>
        /// Rechnet das Projekt durch und füllt die Auslegungsprüfung mit den Größen, die
        /// nur ein Simulationslauf liefern kann: maximale Entzugsleistung,
        /// Jahresentzugsarbeit und Jahresvolllaststunden.
        ///
        /// WARUM EIN VOLLSTÄNDIGER LAUF. Ein kleinerer Rechenweg gibt es nicht. Die drei
        /// Größen entstehen in <see cref="ErdreichAuswertung"/> aus der Entzugsganglinie
        /// „Wärmeproduktion − Strombedarf" der gesamten Wärmepumpenkaskade; die
        /// Wärmeproduktion setzt den Wärmebedarf des Gebäudes und den vollständigen
        /// Kaskadenlauf mit Puffer voraus, und <c>ErdreichAuswertung.AusLauf</c> nimmt
        /// folgerichtig eine fertige <c>SimulationControl</c>. Ein Teilnachbau würde
        /// Enginelogik doppeln.
        ///
        /// WAS DER LAUF ÄNDERT - und was nicht:
        ///   • <see cref="SimulationRunner.Simuliere"/> RECHNET nur. Anders als
        ///     <c>SimuliereUndSpeichere</c> ruft es <c>ErgebnisCtrl.Save</c> NICHT auf,
        ///     es entstehen also keine Zeilen in den Tab_Ergebnis*-Tabellen. Der
        ///     Rechenpfad selbst schreibt nichts in die Datenbank (nachgesehen: keine
        ///     schreibenden Anweisungen in SimulationControl/SimulationWaermepumpe/
        ///     SimulationWaermebedarf/SimulationStrombedarf).
        ///   • Der Lauf setzt aber den prozessweiten Zwischenspeicher von
        ///     <see cref="ErdreichAuswertung"/> für dieses Projekt neu - das ist gewollt
        ///     (es IST ein echter Lauf) und wirkt sich auf die Ergebnisanzeigen der
        ///     Sitzung aus, so wie jeder andere Lauf auch.
        ///   • Er rechnet mit den GESPEICHERTEN WQ_*-Werten. Der Dialog schreibt seine
        ///     Eingaben erst nach „OK" (und zwar im Aufrufer), und daran ändert dieser
        ///     Knopf bewusst nichts: Ein „Abbrechen" muss die Eingaben verwerfen können.
        ///     Weicht die Anzeige ab, sagt die Hinweiszeile das an (Befund 4).
        ///
        /// Der Lauf läuft SYNCHRON im Oberflächenfaden mit Wartecursor - Muster wie
        /// <c>Form_SpeicherOptimierung</c>. Der Dialog ist modal, es kann also nichts
        /// dazwischenkommen; der Knopf sperrt sich zusätzlich selbst, damit ein zweiter
        /// Klick nicht in denselben Lauf hineinläuft.
        /// </summary>
        private void btnSimulation_Click(object sender, EventArgs e)
        {
            int idProjekt = ProjektErmitteln();
            if (idProjekt <= 0)
            {
                // Kein Meldung(): Das setzt DialogResult auf None und ist der
                // Prüfpfad von „OK". Hier ist nichts zu bestätigen.
                MessageBox.Show(MyResource.Resource.SIMQ_ERDREICH_MSG_SIM_OHNE_PROJEKT.Replace("\n", "\r\n"),
                                MyResource.Resource.SIMQ_ERDREICH_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fehler;
            bool ok;
            _btnSimulation.Enabled = false;
            // Wartecursor über this.Cursor (Muster FormMain) und nicht über
            // Cursor.Current: In einer von Form abgeleiteten Klasse verdeckt die
            // geerbte Eigenschaft Control.Cursor den gleichnamigen Typ, der statische
            // Zugriff wäre also nur voll qualifiziert möglich. Der Dialog ist modal -
            // seine eigene Fläche ist die einzige, die der Anwender währenddessen
            // bedienen könnte.
            this.Cursor = Cursors.WaitCursor;
            try
            {
                ok = new SimulationRunner().Simuliere(idProjekt, out fehler);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                _btnSimulation.Enabled = true;
            }

            if (!ok)
            {
                MessageBox.Show(string.Format(CultureInfo.CurrentCulture,
                                    MyResource.Resource.SIMQ_ERDREICH_MSG_SIM_FEHLER.Replace("\n", "\r\n"),
                                    fehler),
                                MyResource.Resource.SIMQ_ERDREICH_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ErdreichAuswertung.AnlageErgebnis erg = ErgebnisDesLaufs(idProjekt);
            if (erg == null)
            {
                MessageBox.Show(MyResource.Resource.SIMQ_ERDREICH_MSG_SIM_OHNE_ERGEBNIS.Replace("\n", "\r\n"),
                                MyResource.Resource.SIMQ_ERDREICH_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ErgebnisUebernehmen(erg);
            _laufAusDialog = true;
            Aktualisieren();
        }

        /// <summary>
        /// Projektbezug für den Lauf. Vorrang hat <see cref="ID_Projekt"/>; ist es nicht
        /// gesetzt, kommt der Bezug aus dem besitzenden Formular.
        ///
        /// Warum der Umweg: Der einzige Aufrufer ist
        /// <c>Form_Simulation_Config.Uebersicht.cs</c> (Zweig TYP_ERDREICH), und diese
        /// Datei wird derzeit an anderer Stelle umgebaut - sie durfte für diesen Befund
        /// nicht angefasst werden. <c>m_ID_Projekt</c> ist dort öffentlich, der Dialog
        /// wird mit <c>ShowDialog(this)</c> geöffnet und hat den Aufrufer damit als
        /// <c>Owner</c>. Sobald die Datei wieder frei ist, genügt dort
        /// <c>frmErde.ID_Projekt = m_ID_Projekt; frmErde.ID_Anlage = info.ID;</c> - dann
        /// greift der Vorrang und dieser Rückfallweg wird nicht mehr betreten.
        /// </summary>
        private int ProjektErmitteln()
        {
            if (ID_Projekt > 0) return ID_Projekt;

            Form_Simulation_Config aufrufer = this.Owner as Form_Simulation_Config;
            return (aufrufer != null && aufrufer.m_ID_Projekt > 0) ? aufrufer.m_ID_Projekt : 0;
        }

        /// <summary>
        /// Sucht das Erdreich-Ergebnis dieser Wärmepumpe aus dem eben gelaufenen
        /// Simulationslauf. Drei Stufen, absteigend nach Eindeutigkeit:
        ///
        ///   1. über <see cref="ID_Anlage"/> - eindeutig, sobald der Aufrufer sie setzt;
        ///   2. über den Modulnamen: <c>AnlageErgebnis.Modul</c> ist
        ///      <c>Tab_Energieanlagen.Bezeichner</c> (SimulationWaermepumpe:
        ///      <c>WP_Modul[i] = model.Bezeichner</c>), und genau den bekommt der Dialog
        ///      als <see cref="WPName"/> vom Aufrufer;
        ///   3. führt das Projekt nur EINE Erdreichquelle, ist auch deren einziges
        ///      Ergebnis eindeutig - dieser Fall trägt die Zuordnung, wenn der
        ///      Modulname leer war und durch einen Ersatznamen ersetzt wurde.
        ///
        /// null = der Lauf hat für diese Anlage nichts geliefert (Wärmepumpe nicht
        /// gerechnet, oder WQ_Typ steht in der Datenbank nicht auf Erdreich).
        /// </summary>
        private ErdreichAuswertung.AnlageErgebnis ErgebnisDesLaufs(int idProjekt)
        {
            ErdreichAuswertung.AnlageErgebnis einziges = null;
            int anzahl = 0;

            foreach (ErdreichAuswertung.AnlageErgebnis a in ErdreichAuswertung.FuerProjekt(idProjekt))
            {
                if (ID_Anlage > 0 && a.ID_Anlage == ID_Anlage) return a;
                if (!string.IsNullOrEmpty(WPName) &&
                    string.Equals(a.Modul, WPName, StringComparison.Ordinal)) return a;

                anzahl++;
                einziges = a;
            }

            return anzahl == 1 ? einziges : null;
        }

        /// <summary>
        /// Übernimmt die Ergebnisgrößen eines Laufs in die Felder der Auslegungsprüfung.
        ///
        /// Die Zuordnung ist Zeile für Zeile dieselbe, die der Aufrufer beim Öffnen des
        /// Dialogs vornimmt (Form_Simulation_Config.Uebersicht.cs, Zweig TYP_ERDREICH,
        /// Block „Ergebnisanbindung der Auslegungsprüfung"). Sie steht hier absichtlich
        /// noch einmal und nicht als gemeinsame Hilfsmethode: Die gemeinsame Methode
        /// gehörte in den Aufrufer oder in ErdreichAuswertung, und beide Wege hätten
        /// Dateien angefasst, die für diesen Befund gesperrt waren. Ändert sich die
        /// Zuordnung, sind beide Stellen zu pflegen - deshalb dieser Hinweis.
        /// </summary>
        private void ErgebnisUebernehmen(ErdreichAuswertung.AnlageErgebnis erg)
        {
            ErgebnisseVorhanden = erg.MaxEntzugBelastbar;
            MaxEntzugW = erg.MaxEntzugW;
            JahresentzugKWh = erg.JahresentzugKWh;
            VolllastStunden = erg.VolllastStunden;

            HinweisErgebnis = "";
            HinweisVorbehalt = "";
            HinweisFrost = "";

            if (erg.Unwirksam)
            {
                // Luft-Wasser: die Konfiguration wird gar nicht gerechnet.
                HinweisErgebnis = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMQ_ERDREICH_WIRKUNGSLOS.Replace("\n", Environment.NewLine),
                    erg.Grenze);
                return;
            }

            if (!erg.MaxEntzugBelastbar)
            {
                HinweisErgebnis = string.Format(CultureInfo.CurrentCulture,
                    MyResource.Resource.SIMQ_ERDREICH_KEINE_PRUEFUNG.Replace("\n", Environment.NewLine),
                    erg.Grenze);
                return;
            }

            if (erg.MaxEntzugGeschaetzt) HinweisVorbehalt = erg.Grenze;
            if (erg.InklSpeicherladung)
                HinweisVorbehalt = (HinweisVorbehalt.Length > 0 ? HinweisVorbehalt + " " : "") +
                                   MyResource.Resource.SIMQ_ERDREICH_SPEICHERLADUNG;
            if (erg.FrostWarnung) HinweisFrost = erg.Frosttext();
        }

        // ------------------------------------------------------------------
        // Übernahme
        // ------------------------------------------------------------------

        private void btnOk_Click(object sender, EventArgs e)
        {
            string titel = MyResource.Resource.SIMQ_ERDREICH_TITEL;

            float tiefe, flaeche, laenge, anzahl;

            if (_rbKollektor.Checked)
            {
                if (!WaermequelleClass.ZahlParsen(_tbTiefe.Text, out tiefe) ||
                    !WaermequelleClass.ZahlParsen(_tbFlaeche.Text, out flaeche))
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_ZAHL_KOLLEKTOR, titel);
                    return;
                }
                if (tiefe <= 0)
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_TIEFE_NULL, titel);
                    return;
                }
                if (tiefe > 10)
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_TIEFE_MAX, titel);
                    return;
                }
                if (flaeche <= 0)
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_FLAECHE, titel);
                    return;
                }

                Quellsystem = ErdreichTemperatur.QUELLSYSTEM_KOLLEKTOR;
                Tiefe = tiefe;
                Flaeche = flaeche;
                Anzahl = 0;
            }
            else
            {
                if (!WaermequelleClass.ZahlParsen(_tbLaenge.Text, out laenge) ||
                    !WaermequelleClass.ZahlParsen(_tbAnzahl.Text, out anzahl))
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_ZAHL_SONDE, titel);
                    return;
                }
                if (laenge <= 0)
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_LAENGE_NULL, titel);
                    return;
                }
                if (anzahl < 1)
                {
                    Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_ANZAHL_MIN, titel);
                    return;
                }

                Quellsystem = ErdreichTemperatur.QUELLSYSTEM_SONDE;
                Tiefe = laenge;
                Flaeche = 0;
                Anzahl = (int)Math.Round(anzahl);
            }

            float spreizung;
            if (!WaermequelleClass.ZahlParsen(_tbSpreizung.Text, out spreizung) || spreizung <= 0)
            {
                Meldung(MyResource.Resource.SIMQ_ERDREICH_MSG_SPREIZUNG, titel);
                return;
            }

            Spreizung = spreizung;
            Bodentyp = AktuellerBodentyp();
            Klimazone = AktuelleZone();
        }

        /// <summary>Hinweis anzeigen und den Dialog offen halten (Bestandsmuster).</summary>
        private void Meldung(string text, string titel)
        {
            MessageBox.Show(text, titel, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            this.DialogResult = DialogResult.None;
        }
    }
}
