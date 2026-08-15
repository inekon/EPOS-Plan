using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eingabe des Quellprofils einer Wärmepumpe (Wärmequelle) - aufgebaut
    /// analog zu "Brauchwassertypen Stundenverteilung":
    ///
    /// - Reiter "Monatswerte":  12 Monats-Mitteltemperaturen der Quelle [°C]
    /// - Reiter "Wochenwerte":  Tagesgang je Wochentag als Abweichung [K]
    ///                          (24 Stundenwerte je Tag, Tag kopieren/einfügen)
    /// - Reiter "Grafik":       daraus konstruiertes Jahresprofil (8760 h)
    ///
    /// Jahresprofil: Quelltemperatur(h) = Monatswert(Monat) + Wochenwert(Wochentag, Stunde)
    ///
    /// Das Formular wird bewusst komplett programmatisch aufgebaut (kein Designer,
    /// keine .resx) - passend zum übrigen Umbau der Simulations-Konfiguration.
    /// </summary>
    public class Form_Quellprofil : Form
    {
        private static readonly string[] MONATE =
            { "Januar", "Februar", "März", "April", "Mai", "Juni",
              "Juli", "August", "September", "Oktober", "November", "Dezember" };

        private static readonly string[] WOCHENTAGE =
            { "Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag", "Sonntag" };

        /// <summary>Monats-Mitteltemperaturen der Wärmequelle [°C]</summary>
        private double[] _monat = new double[12];

        /// <summary>Tagesgang je Wochentag als Abweichung vom Monatswert [K]</summary>
        private double[,] _woche = new double[7, 24];

        /// <summary>Zwischenablage für "Tag kopieren" / "Tag einfügen"</summary>
        private double[] _tagKopie = null;

        private TextBox[] _tbMonat = new TextBox[12];
        private TextBox[] _tbStunde = new TextBox[24];
        private ListBox _lbTag;
        private Chart _chart;
        private Label _lblInfo;
        private int _aktuellerTag = 0;

        /// <summary>Name der Wärmepumpe (nur für den Fenstertitel).</summary>
        public string WPName = "";

        public Form_Quellprofil()
        {
            BaueOberflaeche();
        }

        // ------------------------------------------------------------------
        // Laden / Speichern der Werte als Zeichenkette (Datenbankspalten)
        // ------------------------------------------------------------------

        /// <summary>
        /// Monatswerte als "t1;...;t12" (Punkt als Dezimaltrennzeichen).
        /// </summary>
        public string Monatswerte
        {
            get
            {
                string[] werte = new string[12];
                for (int m = 0; m < 12; m++)
                    werte[m] = _monat[m].ToString(CultureInfo.InvariantCulture);
                return string.Join(";", werte);
            }
            set
            {
                for (int m = 0; m < 12; m++) _monat[m] = 10; // Vorgabe
                if (string.IsNullOrEmpty(value)) return;

                string[] teile = value.Split(';');
                for (int m = 0; m < 12 && m < teile.Length; m++)
                {
                    float w;
                    if (WaermequelleClass.ZahlParsen(teile[m], out w)) _monat[m] = w;
                }
            }
        }

        /// <summary>
        /// Wochenwerte als 168 Werte "w1;...;w168" (Montag 0 Uhr bis Sonntag 23 Uhr).
        /// </summary>
        public string Wochenwerte
        {
            get
            {
                string[] werte = new string[168];
                for (int t = 0; t < 7; t++)
                    for (int h = 0; h < 24; h++)
                        werte[t * 24 + h] = _woche[t, h].ToString(CultureInfo.InvariantCulture);
                return string.Join(";", werte);
            }
            set
            {
                Array.Clear(_woche, 0, _woche.Length); // Vorgabe: keine Abweichung
                if (string.IsNullOrEmpty(value)) return;

                string[] teile = value.Split(';');
                for (int i = 0; i < 168 && i < teile.Length; i++)
                {
                    float w;
                    if (WaermequelleClass.ZahlParsen(teile[i], out w)) _woche[i / 24, i % 24] = w;
                }
            }
        }

        /// <summary>Übernimmt die geladenen Werte in die Eingabefelder.</summary>
        public void SetControls()
        {
            if (!string.IsNullOrEmpty(WPName)) this.Text = "Quellprofil Wärmequelle - " + WPName;

            for (int m = 0; m < 12; m++)
                _tbMonat[m].Text = _monat[m].ToString("F1");

            _lbTag.SelectedIndex = 0;
            TagAnzeigen(0);
            ChartAktualisieren();
        }

        // ------------------------------------------------------------------
        // Oberfläche
        // ------------------------------------------------------------------

        private void BaueOberflaeche()
        {
            this.Text = "Quellprofil Wärmequelle";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(700, 540);

            _lblInfo = new Label
            {
                AutoSize = false,
                Location = new Point(12, 10),
                Size = new Size(676, 36),
                Text = "Quelltemperatur = Monatswert [°C] + Wochenwert [K].\n" +
                       "Die Monatswerte geben den Jahresgang vor, die Wochenwerte den Tages-/Wochengang."
            };
            this.Controls.Add(_lblInfo);

            TabControl tabs = new TabControl
            {
                Location = new Point(12, 52),
                Size = new Size(676, 440)
            };
            this.Controls.Add(tabs);

            tabs.TabPages.Add(BaueMonatsSeite());
            tabs.TabPages.Add(BaueWochenSeite());
            tabs.TabPages.Add(BaueGrafikSeite());
            tabs.SelectedIndexChanged += (s, e) => { if (tabs.SelectedIndex == 2) ChartAktualisieren(); };

            Button btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 190, 500),
                Width = 85
            };
            Button btnAbbruch = new Button
            {
                Text = "Abbrechen",
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 97, 500),
                Width = 85
            };
            btnOk.Click += btnOk_Click;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbruch);
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbruch;
        }

        private TabPage BaueMonatsSeite()
        {
            TabPage seite = new TabPage("Monatswerte");

            Label kopf = new Label
            {
                Text = "Monats-Mitteltemperatur der Wärmequelle [°C]",
                AutoSize = true,
                Location = new Point(20, 18),
                Font = new Font(this.Font, FontStyle.Bold)
            };
            seite.Controls.Add(kopf);

            // 12 Monate in zwei Spalten zu je sechs Zeilen
            for (int m = 0; m < 12; m++)
            {
                int spalte = m / 6;
                int zeile = m % 6;

                Label l = new Label
                {
                    Text = MONATE[m],
                    AutoSize = false,
                    Size = new Size(80, 22),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(30 + spalte * 320, 55 + zeile * 42)
                };
                _tbMonat[m] = new TextBox
                {
                    Location = new Point(120 + spalte * 320, 53 + zeile * 42),
                    Width = 100,
                    Text = "10,0"
                };
                Label einheit = new Label
                {
                    Text = "°C",
                    AutoSize = true,
                    Location = new Point(228 + spalte * 320, 56 + zeile * 42)
                };

                seite.Controls.Add(l);
                seite.Controls.Add(_tbMonat[m]);
                seite.Controls.Add(einheit);
            }

            Button btnAlle = new Button
            {
                Text = "Alle Monate auf Januarwert setzen",
                Location = new Point(30, 330),
                Width = 250
            };
            btnAlle.Click += (s, e) =>
            {
                float w;
                if (!WaermequelleClass.ZahlParsen(_tbMonat[0].Text, out w))
                {
                    MessageBox.Show("Bitte im Feld Januar eine gültige Zahl eintragen!", "Quellprofil",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                for (int m = 0; m < 12; m++) _tbMonat[m].Text = w.ToString("F1");
            };
            seite.Controls.Add(btnAlle);

            return seite;
        }

        private TabPage BaueWochenSeite()
        {
            TabPage seite = new TabPage("Wochenwerte");

            Label kopf = new Label
            {
                Text = "Abweichung vom Monatswert je Stunde [K]",
                AutoSize = true,
                Location = new Point(20, 15),
                Font = new Font(this.Font, FontStyle.Bold)
            };
            seite.Controls.Add(kopf);

            // 24 Stundenfelder in drei Spalten zu je acht Zeilen (wie Brauchwasser)
            for (int h = 0; h < 24; h++)
            {
                int spalte = h / 8;
                int zeile = h % 8;

                Label nr = new Label
                {
                    Text = (h + 1).ToString(),
                    AutoSize = false,
                    Size = new Size(22, 20),
                    TextAlign = ContentAlignment.MiddleRight,
                    Location = new Point(20 + spalte * 150, 48 + zeile * 34)
                };
                _tbStunde[h] = new TextBox
                {
                    Location = new Point(48 + spalte * 150, 45 + zeile * 34),
                    Width = 90,
                    Text = "0,0"
                };

                seite.Controls.Add(nr);
                seite.Controls.Add(_tbStunde[h]);
            }

            Label lblTag = new Label
            {
                Text = "Auswahl Wochentag",
                AutoSize = true,
                Location = new Point(490, 25)
            };
            _lbTag = new ListBox
            {
                Location = new Point(490, 48),
                Size = new Size(150, 130)
            };
            _lbTag.Items.AddRange(WOCHENTAGE);
            _lbTag.SelectedIndexChanged += lbTag_SelectedIndexChanged;

            Button btnKopieren = new Button { Text = "Tag kopieren", Location = new Point(490, 190), Width = 150 };
            Button btnEinfuegen = new Button { Text = "Tag einfügen", Location = new Point(490, 222), Width = 150 };
            Button btnAlleTage = new Button { Text = "auf alle Tage übertragen", Location = new Point(490, 254), Width = 150 };
            Button btnUebernehmen = new Button { Text = "Änderungen Übernehmen", Location = new Point(20, 330), Width = 430 };

            btnKopieren.Click += btnKopieren_Click;
            btnEinfuegen.Click += btnEinfuegen_Click;
            btnAlleTage.Click += btnAlleTage_Click;
            btnUebernehmen.Click += btnUebernehmen_Click;

            seite.Controls.Add(lblTag);
            seite.Controls.Add(_lbTag);
            seite.Controls.Add(btnKopieren);
            seite.Controls.Add(btnEinfuegen);
            seite.Controls.Add(btnAlleTage);
            seite.Controls.Add(btnUebernehmen);

            Label hinweis = new Label
            {
                Text = "Hinweis: 0 = keine Abweichung (Quelltemperatur entspricht dem Monatswert).",
                AutoSize = true,
                Location = new Point(20, 368)
            };
            seite.Controls.Add(hinweis);

            return seite;
        }

        private TabPage BaueGrafikSeite()
        {
            TabPage seite = new TabPage("Grafik");

            _chart = new Chart
            {
                Location = new Point(10, 10),
                Size = new Size(648, 380)
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

            Series s = new Series("Quelltemperatur")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.FromArgb(180, Color.Blue),
                BorderWidth = 2,
                XValueType = ChartValueType.Double
            };
            _chart.Series.Add(s);

            seite.Controls.Add(_chart);
            return seite;
        }

        // ------------------------------------------------------------------
        // Ereignisse
        // ------------------------------------------------------------------

        private void lbTag_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lbTag.SelectedIndex < 0) return;

            // Eingaben des bisherigen Tages sichern, dann neuen Tag anzeigen
            TagUebernehmen(_aktuellerTag, false);
            _aktuellerTag = _lbTag.SelectedIndex;
            TagAnzeigen(_aktuellerTag);
        }

        private void TagAnzeigen(int tag)
        {
            for (int h = 0; h < 24; h++)
                _tbStunde[h].Text = _woche[tag, h].ToString("F1");
        }

        /// <summary>
        /// Liest die 24 Stundenfelder in den Datenbestand des Tages.
        /// </summary>
        private bool TagUebernehmen(int tag, bool meldung)
        {
            double[] werte = new double[24];
            for (int h = 0; h < 24; h++)
            {
                float w;
                if (!WaermequelleClass.ZahlParsen(_tbStunde[h].Text, out w))
                {
                    if (meldung)
                        MessageBox.Show("Stunde " + (h + 1) + ": '" + _tbStunde[h].Text +
                            "' ist keine gültige Zahl!", "Quellprofil",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                werte[h] = w;
            }

            for (int h = 0; h < 24; h++) _woche[tag, h] = werte[h];
            return true;
        }

        private void btnUebernehmen_Click(object sender, EventArgs e)
        {
            if (TagUebernehmen(_aktuellerTag, true)) ChartAktualisieren();
        }

        private void btnKopieren_Click(object sender, EventArgs e)
        {
            if (!TagUebernehmen(_aktuellerTag, true)) return;

            _tagKopie = new double[24];
            for (int h = 0; h < 24; h++) _tagKopie[h] = _woche[_aktuellerTag, h];
        }

        private void btnEinfuegen_Click(object sender, EventArgs e)
        {
            if (_tagKopie == null)
            {
                MessageBox.Show("Bitte zuerst einen Tag kopieren!", "Quellprofil",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            for (int h = 0; h < 24; h++) _woche[_aktuellerTag, h] = _tagKopie[h];
            TagAnzeigen(_aktuellerTag);
            ChartAktualisieren();
        }

        private void btnAlleTage_Click(object sender, EventArgs e)
        {
            if (!TagUebernehmen(_aktuellerTag, true)) return;

            for (int t = 0; t < 7; t++)
                for (int h = 0; h < 24; h++)
                    _woche[t, h] = _woche[_aktuellerTag, h];

            ChartAktualisieren();
            MessageBox.Show("Der Tagesgang wurde auf alle Wochentage übertragen.", "Quellprofil",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            // Monatswerte prüfen und übernehmen
            for (int m = 0; m < 12; m++)
            {
                float w;
                if (!WaermequelleClass.ZahlParsen(_tbMonat[m].Text, out w))
                {
                    MessageBox.Show(MONATE[m] + ": '" + _tbMonat[m].Text + "' ist keine gültige Zahl!",
                        "Quellprofil", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                _monat[m] = w;
            }

            // Sichtbaren Wochentag ebenfalls übernehmen
            if (!TagUebernehmen(_aktuellerTag, true))
            {
                this.DialogResult = DialogResult.None;
                return;
            }
        }

        private void ChartAktualisieren()
        {
            if (_chart == null) return;

            // Monatswerte aus den Feldern lesen (ohne Meldung - Grafik ist nur Vorschau)
            for (int m = 0; m < 12; m++)
            {
                float w;
                if (WaermequelleClass.ZahlParsen(_tbMonat[m].Text, out w)) _monat[m] = w;
            }

            float[] profil = WaermequelleClass.ProfilAusMonatsUndWochenwerten(Monatswerte, Wochenwerte);
            _chart.Series[0].Points.Clear();
            if (profil == null) return;

            // Jede Stunde zeichnen, X-Achse in Monaten
            for (int i = 0; i < 8760; i++)
            {
                double x = (double)i * 12.0 / 8760.0;
                _chart.Series[0].Points.AddXY(x, profil[i]);
            }
        }
    }
}
