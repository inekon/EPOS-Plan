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
    /// komplett programmatisch aufgebaut (kein Designer, keine .resx). Die
    /// sichtbaren Texte sind deutsch hartkodiert; die durchgängige Lokalisierung
    /// des Simulationsbereichs ist Gegenstand von Paket 9 (Konzept 13.6).
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

        // ---- Steuerelemente -----------------------------------------------

        private RadioButton _rbKollektor;
        private RadioButton _rbSonde;
        private TextBox _tbTiefe;
        private TextBox _tbFlaeche;
        private TextBox _tbLaenge;
        private TextBox _tbAnzahl;
        private ComboBox _cbBoden;
        private ComboBox _cbZone;
        private Chart _chart;
        private Label _lblKennwerte;
        private Label _lblBoden;
        private Label _lblPruefung;

        private bool _uiAufbau = true;   // unterdrückt Ereignisse während SetControls

        public Form_QuelleErdreich()
        {
            BaueOberflaeche();
        }

        // ------------------------------------------------------------------
        // Aufbau
        // ------------------------------------------------------------------

        private void BaueOberflaeche()
        {
            this.Text = "Wärmequelle Erdreich";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(700, 690);

            // --- Quellsystem ------------------------------------------------
            GroupBox gbSystem = new GroupBox
            {
                Text = "Quellsystem",
                Location = new Point(12, 10),
                Size = new Size(676, 120)
            };
            this.Controls.Add(gbSystem);

            _rbKollektor = new RadioButton
            {
                Text = "Erdkollektor",
                AutoSize = true,
                Checked = true,
                Location = new Point(16, 26)
            };
            _rbSonde = new RadioButton
            {
                Text = "Erdsonde",
                AutoSize = true,
                Location = new Point(16, 76)
            };
            _rbKollektor.CheckedChanged += (s, e) => { SystemUmschalten(); Aktualisieren(); };
            _rbSonde.CheckedChanged += (s, e) => { SystemUmschalten(); Aktualisieren(); };

            Label lT = new Label { Text = "Verlegetiefe [m]:", AutoSize = true, Location = new Point(160, 28) };
            _tbTiefe = new TextBox { Location = new Point(285, 25), Width = 70, Text = "1,5" };
            Label lF = new Label { Text = "Fläche [m²]:", AutoSize = true, Location = new Point(390, 28) };
            _tbFlaeche = new TextBox { Location = new Point(490, 25), Width = 70, Text = "0" };

            Label lL = new Label { Text = "Länge je Sonde [m]:", AutoSize = true, Location = new Point(160, 78) };
            _tbLaenge = new TextBox { Location = new Point(285, 75), Width = 70, Text = "90" };
            Label lA = new Label { Text = "Anzahl Sonden:", AutoSize = true, Location = new Point(390, 78) };
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
            Label lB = new Label { Text = "Bodentyp:", AutoSize = true, Location = new Point(28, 145) };
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
                Text = "(Katalog VDI 4640 Blatt 1, Entwurf 2021-12)",
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

            Label lZ = new Label { Text = "Klimazone:", AutoSize = true, Location = new Point(28, 198) };
            _cbZone = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 195),
                Width = 230
            };
            _cbZone.Items.Add("0 — nicht zugeordnet");
            for (int z = 1; z <= VDI4640Pruefung.KLIMAZONEN; z++)
            {
                _cbZone.Items.Add(z.ToString(CultureInfo.CurrentCulture) + " — " +
                    VDI4640Pruefung.VolllaststundenZone(z).ToString("N0", CultureInfo.CurrentCulture) + " h/a");
            }
            _cbZone.SelectedIndexChanged += (s, e) => Aktualisieren();
            Label lZH = new Label
            {
                Text = "(DIN 4710, Vorbelegung aus der Klimaregion)",
                AutoSize = true,
                Location = new Point(392, 198)
            };

            this.Controls.Add(lB); this.Controls.Add(_cbBoden); this.Controls.Add(lBH);
            this.Controls.Add(_lblBoden);
            this.Controls.Add(lZ); this.Controls.Add(_cbZone); this.Controls.Add(lZH);

            // --- Vorschau ----------------------------------------------------
            GroupBox gbVorschau = new GroupBox
            {
                Text = "Vorschau: Jahresgang der Quelltemperatur",
                Location = new Point(12, 228),
                Size = new Size(676, 270)
            };
            this.Controls.Add(gbVorschau);

            _chart = new Chart
            {
                Location = new Point(12, 20),
                Size = new Size(652, 210)
            };
            ChartArea ca = new ChartArea("Jahr");
            ca.AxisX.Title = "Monat";
            ca.AxisY.Title = "Quelltemperatur [°C]";
            ca.AxisX.Minimum = 0;
            ca.AxisX.Maximum = 12;
            ca.AxisX.Interval = 1;
            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.AxisX.ScaleView.Zoomable = true;
            _chart.ChartAreas.Add(ca);

            // FastLine: 8760 Punkte je Neuzeichnung (Konzept 4.5)
            Series sQuelle = new Series("Quelltemperatur")
            {
                ChartType = SeriesChartType.FastLine,
                Color = Color.FromArgb(200, Color.SaddleBrown),
                BorderWidth = 2,
                XValueType = ChartValueType.Double
            };
            _chart.Series.Add(sQuelle);

            Series sAussen = new Series("Außentemperatur")
            {
                ChartType = SeriesChartType.FastLine,
                Color = Color.FromArgb(90, Color.SteelBlue),
                BorderWidth = 1,
                XValueType = ChartValueType.Double
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
                Text = "Auslegungsprüfung nach VDI 4640 Blatt 2 (nach der Simulation)",
                Location = new Point(12, 506),
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
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 190, 648),
                Width = 85
            };
            Button btnAbbruch = new Button
            {
                Text = "Abbrechen",
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 97, 648),
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

            if (!string.IsNullOrEmpty(WPName)) this.Text = "Wärmequelle Erdreich — " + WPName;

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
            _lblBoden.Text = string.Format(CultureInfo.CurrentCulture,
                "λ = {0:0.0} W/(m·K)   ρ·c_p = {1:0.00} MJ/(m³·K)   a = {2:0.00} mm²/s   " +
                "Dämpfungstiefe d = {3:0.00} m   Bodenart nach Tabelle A1: {4}",
                boden.Lambda, boden.RhoCp, boden.A_mm2s, boden.Daempfungstiefe,
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
                (jg.AusKlimadaten ? "" : "   (ohne Klimadaten — Ersatzwerte 9,5 °C / 8,5 K)");

            PruefungAktualisieren(bodenSchluessel, tiefe, flaeche, laenge, anzahl);
        }

        /// <summary>Füllt den Bereich der Auslegungsprüfung (Konzept 4.5/13.1).</summary>
        private void PruefungAktualisieren(string bodenSchluessel, double tiefe, double flaeche,
                                           double laenge, double anzahl)
        {
            if (!ErgebnisseVorhanden)
            {
                _lblPruefung.Text = "(noch kein Simulationslauf)\r\n\r\n" +
                    "Die Prüfung braucht maximale Entzugsleistung, Jahresentzugsarbeit und\r\n" +
                    "Jahresvolllaststunden aus einem Simulationslauf.";
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
                text += "\r\n  Hinweis: Festgestein wird auf die höchste Bodenart der Tabelle A1 abgebildet — nur Orientierung.";

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
            const string titel = "Wärmequelle Erdreich";

            float tiefe, flaeche, laenge, anzahl;

            if (_rbKollektor.Checked)
            {
                if (!WaermequelleClass.ZahlParsen(_tbTiefe.Text, out tiefe) ||
                    !WaermequelleClass.ZahlParsen(_tbFlaeche.Text, out flaeche))
                {
                    Meldung("Bitte gültige Zahlenwerte für Verlegetiefe und Fläche eintragen!", titel);
                    return;
                }
                if (tiefe <= 0)
                {
                    Meldung("Die Verlegetiefe muss größer als 0 m sein!", titel);
                    return;
                }
                if (tiefe > 10)
                {
                    Meldung("Ein Erdkollektor wird nicht tiefer als 10 m verlegt.\n" +
                            "Für größere Tiefen das Quellsystem 'Erdsonde' wählen.", titel);
                    return;
                }
                if (flaeche <= 0)
                {
                    Meldung("Bitte die Kollektorfläche eintragen — sie ist Eingangsgröße\n" +
                            "der Auslegungsprüfung nach VDI 4640 Blatt 2.", titel);
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
                    Meldung("Bitte gültige Zahlenwerte für Sondenlänge und Anzahl eintragen!", titel);
                    return;
                }
                if (laenge <= 0)
                {
                    Meldung("Die Sondenlänge muss größer als 0 m sein!", titel);
                    return;
                }
                if (anzahl < 1)
                {
                    Meldung("Es muss mindestens eine Sonde vorhanden sein!", titel);
                    return;
                }

                Quellsystem = ErdreichTemperatur.QUELLSYSTEM_SONDE;
                Tiefe = laenge;
                Flaeche = 0;
                Anzahl = (int)Math.Round(anzahl);
            }

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
