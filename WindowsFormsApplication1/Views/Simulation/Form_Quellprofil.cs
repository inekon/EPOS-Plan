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
        /// <summary>
        /// Monatsnamen der Oberflächensprache (Paket 9 / L3). Sie kommen aus
        /// <see cref="CultureInfo.CurrentUICulture"/> und NICHT mehr aus einem eigenen
        /// Array: Monats- und Wochentagsnamen sind in jedem .NET-Kulturdatensatz
        /// gepflegt, eine eigene Ressource dafür wäre eine zweite Wahrheit
        /// (Konzept 13.6, Teilpaket L3). Unter de-DE liefert das zeichengleich
        /// „Januar"…„Dezember".
        ///
        /// Bewusst eine Eigenschaft statt eines statischen Feldes: Ein statisches Feld
        /// würde beim ersten Typzugriff eingefroren; die Sprachumschaltung (und die
        /// Sprachgleichheitsprobe der Referenzlauf-Suite) sollen aber jederzeit greifen.
        /// </summary>
        private static string[] Monatsnamen
        {
            get
            {
                string[] namen = CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames;
                string[] zwoelf = new string[12];
                Array.Copy(namen, zwoelf, 12);   // MonthNames hat 13 Einträge (der 13. ist leer)
                return zwoelf;
            }
        }

        /// <summary>
        /// Wochentagsnamen, beginnend mit Montag — die Reihenfolge des Datenmodells
        /// (168 Wochenwerte ab Montag 0 Uhr). <c>DayNames</c> beginnt mit Sonntag,
        /// deshalb der Versatz.
        /// </summary>
        private static string[] Wochentagsnamen
        {
            get
            {
                string[] tage = CultureInfo.CurrentUICulture.DateTimeFormat.DayNames;
                string[] abMontag = new string[7];
                for (int t = 0; t < 7; t++) abMontag[t] = tage[(t + 1) % 7];
                return abMontag;
            }
        }

        /// <summary>
        /// Vorbelegung der Monatsfelder [°C] bzw. der Stundenfelder [K]. Bis Paket 9
        /// standen hier die Zeichenketten „10,0" und „0,0" mit hartkodiertem
        /// Dezimalkomma im Quelltext (Konzept 13.6). Jetzt wird der ZAHLENWERT über
        /// <see cref="Vorgabe"/> formatiert - dieselbe Schreibweise, die
        /// <c>SetControls</c>/<c>TagAnzeigen</c> unmittelbar danach erzeugen.
        /// </summary>
        private const double VORGABE_MONATSWERT = 10.0;
        private const double VORGABE_WOCHENWERT = 0.0;

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

        /// <summary>
        /// Zahlenwert als Feldvorbelegung — kulturneutral im Quelltext, formatiert wie
        /// alle übrigen Ausgaben dieses Dialogs (<c>ToString("F1")</c>). Gelesen wird
        /// über <see cref="WaermequelleClass.ZahlParsen"/>, das Komma UND Punkt
        /// annimmt; <c>CurrentCulture</c> wird nicht gesetzt (Konzept 13.6).
        /// </summary>
        private static string Vorgabe(double wert)
        {
            return wert.ToString("F1", CultureInfo.CurrentCulture);
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
                for (int m = 0; m < 12; m++) _monat[m] = VORGABE_MONATSWERT; // Vorgabe
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
            if (!string.IsNullOrEmpty(WPName))
                this.Text = string.Format(MyResource.Resource.SIMQ_QUELLPROFIL_TITEL_MIT_WP, WPName);

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
            this.Text = MyResource.Resource.SIMQ_QUELLPROFIL_TITEL;
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
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_INFO
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
                Text = MyResource.Resource.SIM_BTN_OK,
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 190, 500),
                Width = 85
            };
            Button btnAbbruch = new Button
            {
                Text = MyResource.Resource.SIM_BTN_ABBRECHEN,
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
            TabPage seite = new TabPage(MyResource.Resource.SIMQ_QUELLPROFIL_TAB_MONATSWERTE);

            Label kopf = new Label
            {
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_KOPF_MONAT,
                AutoSize = true,
                Location = new Point(20, 18),
                Font = new Font(this.Font, FontStyle.Bold)
            };
            seite.Controls.Add(kopf);

            // 12 Monate in zwei Spalten zu je sechs Zeilen
            string[] monate = Monatsnamen;
            for (int m = 0; m < 12; m++)
            {
                int spalte = m / 6;
                int zeile = m % 6;

                Label l = new Label
                {
                    Text = monate[m],
                    AutoSize = false,
                    // 80 px tragen den längsten Monatsnamen beider Sprachen
                    // („September"); das Eingabefeld beginnt erst bei x = 120.
                    Size = new Size(80, 22),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(30 + spalte * 320, 55 + zeile * 42)
                };
                _tbMonat[m] = new TextBox
                {
                    Location = new Point(120 + spalte * 320, 53 + zeile * 42),
                    Width = 100,
                    Text = Vorgabe(VORGABE_MONATSWERT)
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
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_BTN_ALLE_MONATE,
                Location = new Point(30, 330),
                Width = 250
            };
            btnAlle.Click += (s, e) =>
            {
                float w;
                if (!WaermequelleClass.ZahlParsen(_tbMonat[0].Text, out w))
                {
                    MessageBox.Show(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_JANUAR,
                        MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL,
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
            TabPage seite = new TabPage(MyResource.Resource.SIMQ_QUELLPROFIL_TAB_WOCHENWERTE);

            Label kopf = new Label
            {
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_KOPF_WOCHE,
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
                    Text = Vorgabe(VORGABE_WOCHENWERT)
                };

                seite.Controls.Add(nr);
                seite.Controls.Add(_tbStunde[h]);
            }

            Label lblTag = new Label
            {
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_LBL_WOCHENTAG,
                AutoSize = true,
                Location = new Point(490, 25)
            };
            _lbTag = new ListBox
            {
                Location = new Point(490, 48),
                Size = new Size(150, 130)
            };
            _lbTag.Items.AddRange(Wochentagsnamen);
            _lbTag.SelectedIndexChanged += lbTag_SelectedIndexChanged;

            Button btnKopieren = new Button { Text = MyResource.Resource.SIMQ_QUELLPROFIL_BTN_TAG_KOPIEREN, Location = new Point(490, 190), Width = 150 };
            Button btnEinfuegen = new Button { Text = MyResource.Resource.SIMQ_QUELLPROFIL_BTN_TAG_EINFUEGEN, Location = new Point(490, 222), Width = 150 };
            Button btnAlleTage = new Button { Text = MyResource.Resource.SIMQ_QUELLPROFIL_BTN_ALLE_TAGE, Location = new Point(490, 254), Width = 150 };
            Button btnUebernehmen = new Button { Text = MyResource.Resource.SIMQ_QUELLPROFIL_BTN_UEBERNEHMEN, Location = new Point(20, 330), Width = 430 };

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
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_HINWEIS_ABWEICHUNG,
                AutoSize = true,
                Location = new Point(20, 368)
            };
            seite.Controls.Add(hinweis);

            return seite;
        }

        private TabPage BaueGrafikSeite()
        {
            TabPage seite = new TabPage(MyResource.Resource.SIMQ_QUELLPROFIL_TAB_GRAFIK);

            _chart = new Chart
            {
                Location = new Point(10, 10),
                Size = new Size(648, 380)
            };
            // "Jahr" ist der technische Name des Diagrammbereichs (Zugriffsschlüssel,
            // Schicht 2 der Drei-Schichten-Regel) - nur die Achsentitel sind Anzeige.
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

            // Drei-Schichten-Regel: Der Serienname ist ein technischer Schlüssel
            // (sprachneutral, ASCII), der Anzeigetext steht in LegendText.
            Series s = new Series("QUELLTEMPERATUR")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.FromArgb(180, Color.Blue),
                BorderWidth = 2,
                XValueType = ChartValueType.Double,
                LegendText = MyResource.Resource.CHART_SERIE_QUELLTEMPERATUR
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
                        MessageBox.Show(
                            string.Format(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_STUNDE_UNGUELTIG,
                                          h + 1, _tbStunde[h].Text),
                            MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL,
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
                MessageBox.Show(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_ERST_KOPIEREN,
                    MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL,
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
            MessageBox.Show(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_ALLE_TAGE,
                MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            // Monatswerte prüfen und übernehmen
            string[] monate = Monatsnamen;
            for (int m = 0; m < 12; m++)
            {
                float w;
                if (!WaermequelleClass.ZahlParsen(_tbMonat[m].Text, out w))
                {
                    MessageBox.Show(
                        string.Format(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_MONAT_UNGUELTIG,
                                      monate[m], _tbMonat[m].Text),
                        MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
