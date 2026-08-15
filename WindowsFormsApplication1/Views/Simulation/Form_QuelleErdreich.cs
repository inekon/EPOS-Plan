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
    /// Die Auslegungsprüfung nach VDI 4640 Blatt 2 braucht Simulationsergebnisse
    /// und bleibt leer, solange kein Lauf vorliegt.
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

        private bool _uiAufbau = true;   // unterdrückt Ereignisse während SetControls

        // --- Technische Serienschlüssel (Paket 9 / L7) --------------------------------
        // Schicht 2 der Drei-Schichten-Regel: sprachneutral, ASCII, unveränderlich.
        // Der Anzeigetext steht ausschließlich in Series.LegendText.
        private const string S_QUELLTEMPERATUR = "QUELLTEMPERATUR";
        private const string S_AUSSENTEMPERATUR = "AUSSENTEMPERATUR";

        public Form_QuelleErdreich()
        {
            BaueOberflaeche();
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
            this.ClientSize = new Size(700, 718);

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

            _lblBoden = new Label
            {
                AutoSize = false,
                Location = new Point(150, 170),
                Size = new Size(530, 18),
                ForeColor = SystemColors.GrayText
            };

            Label lZ = new Label { Text = MyResource.Resource.SIMQ_ERDREICH_KLIMAZONE, AutoSize = true, Location = new Point(28, 198) };
            _cbZone = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 195),
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
                Location = new Point(392, 198)
            };

            // --- Nutzbare Spreizung ------------------------------------------
            // Eingangsgröße der zweiten Warnbedingung (Konzept 13.1). Ohne dieses Feld
            // war WQ_Spreizung bei einer Erdreichquelle nicht pflegbar und die Prüfung
            // rechnete immer mit 5 K.
            Label lS = new Label { Text = MyResource.Resource.SIMQ_ERDREICH_SPREIZUNG, AutoSize = true, Location = new Point(28, 228) };
            _tbSpreizung = new TextBox
            {
                Location = new Point(150, 225),
                Width = 70,
                Text = ErdreichAuswertung.SPREIZUNG_DEFAULT.ToString("0.##", CultureInfo.CurrentCulture)
            };
            Label lSH = new Label
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_SPREIZUNG_HINWEIS,
                AutoSize = true,
                Location = new Point(232, 228),
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
                Location = new Point(12, 256),
                Size = new Size(676, 270)
            };
            this.Controls.Add(gbVorschau);

            _chart = new Chart
            {
                Location = new Point(12, 20),
                Size = new Size(652, 210)
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
                Location = new Point(14, 236),
                Size = new Size(650, 20),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            gbVorschau.Controls.Add(_chart);
            gbVorschau.Controls.Add(_lblKennwerte);

            // --- Auslegungsprüfung -------------------------------------------
            GroupBox gbPruefung = new GroupBox
            {
                Text = MyResource.Resource.SIMQ_ERDREICH_GB_PRUEFUNG,
                Location = new Point(12, 534),
                Size = new Size(676, 130)
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

            // --- Schaltflächen ------------------------------------------------
            Button btnOk = new Button
            {
                Text = MyResource.Resource.SIM_BTN_OK,
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 190, 676),
                Width = 85
            };
            Button btnAbbruch = new Button
            {
                Text = MyResource.Resource.SIM_BTN_ABBRECHEN,
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 97, 676),
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
