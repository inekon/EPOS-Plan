using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Kostenprofil-Editor (Fachkonzept Stromspeicher 4.1 b, Arbeitspaket AP4) —
    /// aufgebaut nach dem Muster <c>Views\Simulation\Form_Quellprofil.cs</c>:
    ///
    /// - Reiter „Monatswerte":  12 Monats-Preisniveaus [ct/kWh]
    /// - Reiter „Wochenwerte":  Tagesgang je Wochentag als Abweichung [ct/kWh]
    ///                          (24 Stundenwerte je Tag, Tag kopieren/einfügen)
    /// - Reiter „Grafik":       daraus konstruiertes Jahresprofil (8760 h)
    ///
    /// Jahresprofil: Preis(h) = Monatswert(Monat) + Wochenwert(Wochentag, Stunde)
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Was gegenüber der Vorlage anders ist — und warum.</b> Drei Punkte:
    /// (1) Die Einheit ist ct/kWh statt °C/K; ein HT/NT-Profil ist damit der
    /// Sonderfall „alle sieben Tage gleich".
    /// (2) Die Vorschau rechnet über <c>SpeicherEngine.PreisModell</c> statt über
    /// <c>WaermequelleClass.ProfilAusMonatsUndWochenwerten</c> — der Anwender sieht
    /// GENAU die Reihe, mit der die Simulation später rechnet, samt der festen
    /// Kalenderausrichtung des Rechenkerns. Die Vorlage leitet den Wochentag aus dem
    /// Systemdatum ab und liefert je nach Tag ein anderes Profil; bei einem Preis wäre
    /// das ein nicht reproduzierbares Ergebnis.
    /// (3) Die Persistenz ist eine eigene Tabelle (<c>Tab_Kostenprofil</c>) statt zweier
    /// Spalten an der Anlage — ein Kostenprofil gehört zum Projekt, nicht zu einem
    /// Wärmeerzeuger.
    /// </para>
    /// <para>
    /// Ablageformat unverändert von der Vorlage: zwei <c>";"</c>-getrennte
    /// Zeichenketten mit <see cref="CultureInfo.InvariantCulture"/>. Die EINGABE folgt
    /// dagegen der Kultur des Anwenders (<c>Program.ZahlParsen</c> nimmt Komma und
    /// Punkt).
    /// </para>
    /// <para>
    /// Vollständig programmatisch, ohne Designer und ohne eigene <c>.resx</c> — wie die
    /// Vorlage.
    /// </para>
    /// </remarks>
    public class Form_Kostenprofil : Form
    {
        /// <summary>Vorbelegung eines Monatswerts [ct/kWh] — der Regelfall-Aufschlag plus 20 ct Energie.</summary>
        private const double VORGABE_MONATSWERT = 25.0;

        /// <summary>Vorbelegung eines Wochenwerts [ct/kWh]: keine Abweichung.</summary>
        private const double VORGABE_WOCHENWERT = 0.0;

        private static string[] Monatsnamen
        {
            get
            {
                string[] namen = CultureInfo.CurrentUICulture.DateTimeFormat.MonthNames;
                string[] zwoelf = new string[12];
                Array.Copy(namen, zwoelf, 12);
                return zwoelf;
            }
        }

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

        private readonly double[] _monat = new double[12];
        private readonly double[,] _woche = new double[7, 24];
        private double[] _tagKopie;

        private readonly TextBox[] _tbMonat = new TextBox[12];
        private readonly TextBox[] _tbStunde = new TextBox[24];
        private TextBox _tbBezeichner;
        private ListBox _lbTag;
        private Chart _chart;
        private int _aktuellerTag;

        private readonly KostenprofilModel _modell;
        private readonly int _idProjekt;

        /// <summary>
        /// Öffnet den Editor für ein bestehendes oder ein neues Profil.
        /// </summary>
        /// <param name="idProjekt">Projekt, dem das Profil gehört.</param>
        /// <param name="idProfil">Vorhandenes Profil, oder 0 für ein neues.</param>
        public Form_Kostenprofil(int idProjekt, int idProfil = 0)
        {
            _idProjekt = idProjekt;

            KostenprofilCtrl.StelleTabelleSicher();

            _modell = idProfil > 0 ? new KostenprofilCtrl().ReadSingle(idProfil) : null;
            if (_modell == null)
            {
                _modell = new KostenprofilModel { ID_Projekt = idProjekt };
                _modell.Bezeichner = MyResource.Resource.PREIS_PROFIL_NEU;
            }

            BaueOberflaeche();

            Monatswerte = _modell.Monatswerte;
            Wochenwerte = _modell.Wochenwerte;
            SetControls();
        }

        /// <summary>Die ID des gespeicherten Profils; 0, wenn nicht gespeichert wurde.</summary>
        public int ProfilId
        {
            get { return _modell.ID; }
        }

        // ------------------------------------------------------------------
        // Serialisierung (Format wie Form_Quellprofil)
        // ------------------------------------------------------------------

        /// <summary>Monatswerte als "m1;...;m12" mit InvariantCulture.</summary>
        public string Monatswerte
        {
            get
            {
                string[] werte = new string[12];
                for (int m = 0; m < 12; m++) werte[m] = _monat[m].ToString(CultureInfo.InvariantCulture);
                return string.Join(";", werte);
            }
            set
            {
                for (int m = 0; m < 12; m++) _monat[m] = VORGABE_MONATSWERT;
                if (string.IsNullOrEmpty(value)) return;

                string[] teile = value.Split(';');
                for (int m = 0; m < 12 && m < teile.Length; m++)
                {
                    double w;
                    if (double.TryParse(teile[m], NumberStyles.Float, CultureInfo.InvariantCulture, out w))
                        _monat[m] = w;
                }
            }
        }

        /// <summary>Wochenwerte als "w1;...;w168" (Montag 0 Uhr bis Sonntag 23 Uhr).</summary>
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
                Array.Clear(_woche, 0, _woche.Length);
                if (string.IsNullOrEmpty(value)) return;

                string[] teile = value.Split(';');
                for (int i = 0; i < 168 && i < teile.Length; i++)
                {
                    double w;
                    if (double.TryParse(teile[i], NumberStyles.Float, CultureInfo.InvariantCulture, out w))
                        _woche[i / 24, i % 24] = w;
                }
            }
        }

        private void SetControls()
        {
            _tbBezeichner.Text = _modell.Bezeichner;
            for (int m = 0; m < 12; m++) _tbMonat[m].Text = Anzeige(_monat[m]);

            _lbTag.SelectedIndex = 0;
            TagAnzeigen(0);
            ChartAktualisieren();
        }

        // ------------------------------------------------------------------
        // Oberfläche
        // ------------------------------------------------------------------

        private void BaueOberflaeche()
        {
            this.Text = MyResource.Resource.PREIS_PROFIL_TITEL;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(700, 580);

            Label lblInfo = new Label
            {
                AutoSize = false,
                Location = new Point(12, 10),
                Size = new Size(676, 34),
                Text = MyResource.Resource.PREIS_PROFIL_INFO
            };
            this.Controls.Add(lblInfo);

            Label lblName = new Label
            {
                Text = MyResource.Resource.PREIS_PROFIL_LABEL_BEZEICHNER,
                Location = new Point(12, 50),
                AutoSize = true
            };
            _tbBezeichner = new TextBox
            {
                Location = new Point(120, 47),
                Width = 400
            };
            this.Controls.Add(lblName);
            this.Controls.Add(_tbBezeichner);

            TabControl tabs = new TabControl
            {
                Location = new Point(12, 80),
                Size = new Size(676, 450)
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
                Location = new Point(this.ClientSize.Width - 190, 540),
                Width = 85
            };
            Button btnAbbruch = new Button
            {
                Text = MyResource.Resource.SIM_BTN_ABBRECHEN,
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 97, 540),
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
            TabPage seite = new TabPage(MyResource.Resource.PREIS_PROFIL_TAB_MONATSWERTE);

            seite.Controls.Add(new Label
            {
                Text = MyResource.Resource.PREIS_PROFIL_KOPF_MONAT,
                AutoSize = true,
                Location = new Point(20, 18),
                Font = new Font(this.Font, FontStyle.Bold)
            });

            string[] monate = Monatsnamen;
            for (int m = 0; m < 12; m++)
            {
                int spalte = m / 6;
                int zeile = m % 6;

                seite.Controls.Add(new Label
                {
                    Text = monate[m],
                    AutoSize = false,
                    Size = new Size(80, 22),
                    TextAlign = ContentAlignment.MiddleLeft,
                    Location = new Point(30 + spalte * 320, 55 + zeile * 42)
                });

                _tbMonat[m] = new TextBox
                {
                    Location = new Point(120 + spalte * 320, 53 + zeile * 42),
                    Width = 100,
                    TextAlign = HorizontalAlignment.Right,
                    Text = Anzeige(VORGABE_MONATSWERT)
                };
                _tbMonat[m].TextChanged += (s, e) => Program.ZahlFaerben(s);
                seite.Controls.Add(_tbMonat[m]);

                seite.Controls.Add(new Label
                {
                    Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH,
                    AutoSize = true,
                    Location = new Point(228 + spalte * 320, 56 + zeile * 42)
                });
            }

            Button btnAlle = new Button
            {
                Text = MyResource.Resource.PREIS_PROFIL_BTN_ALLE_MONATE,
                Location = new Point(30, 340),
                Width = 250
            };
            btnAlle.Click += (s, e) =>
            {
                double w;
                if (!Program.ZahlParsen(_tbMonat[0].Text, out w))
                {
                    MessageBox.Show(MyResource.Resource.PREIS_PROFIL_MSG_JANUAR,
                        MyResource.Resource.PREIS_PROFIL_TITEL,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                for (int m = 0; m < 12; m++) _tbMonat[m].Text = Anzeige(w);
            };
            seite.Controls.Add(btnAlle);

            return seite;
        }

        private TabPage BaueWochenSeite()
        {
            TabPage seite = new TabPage(MyResource.Resource.PREIS_PROFIL_TAB_WOCHENWERTE);

            seite.Controls.Add(new Label
            {
                Text = MyResource.Resource.PREIS_PROFIL_KOPF_WOCHE,
                AutoSize = true,
                Location = new Point(20, 15),
                Font = new Font(this.Font, FontStyle.Bold)
            });

            for (int h = 0; h < 24; h++)
            {
                int spalte = h / 8;
                int zeile = h % 8;

                seite.Controls.Add(new Label
                {
                    Text = (h + 1).ToString(CultureInfo.CurrentCulture),
                    AutoSize = false,
                    Size = new Size(22, 20),
                    TextAlign = ContentAlignment.MiddleRight,
                    Location = new Point(20 + spalte * 150, 48 + zeile * 34)
                });

                _tbStunde[h] = new TextBox
                {
                    Location = new Point(48 + spalte * 150, 45 + zeile * 34),
                    Width = 90,
                    TextAlign = HorizontalAlignment.Right,
                    Text = Anzeige(VORGABE_WOCHENWERT)
                };
                _tbStunde[h].TextChanged += (s, e) => Program.ZahlFaerben(s);
                seite.Controls.Add(_tbStunde[h]);
            }

            seite.Controls.Add(new Label
            {
                Text = MyResource.Resource.PREIS_PROFIL_LBL_WOCHENTAG,
                AutoSize = true,
                Location = new Point(490, 25)
            });

            _lbTag = new ListBox
            {
                Location = new Point(490, 48),
                Size = new Size(150, 130)
            };
            _lbTag.Items.AddRange(Wochentagsnamen);
            _lbTag.SelectedIndexChanged += lbTag_SelectedIndexChanged;
            seite.Controls.Add(_lbTag);

            Button btnKopieren = new Button { Text = MyResource.Resource.PREIS_PROFIL_BTN_TAG_KOPIEREN, Location = new Point(490, 190), Width = 150 };
            Button btnEinfuegen = new Button { Text = MyResource.Resource.PREIS_PROFIL_BTN_TAG_EINFUEGEN, Location = new Point(490, 222), Width = 150 };
            Button btnAlleTage = new Button { Text = MyResource.Resource.PREIS_PROFIL_BTN_ALLE_TAGE, Location = new Point(490, 254), Width = 150 };
            Button btnUebernehmen = new Button { Text = MyResource.Resource.PREIS_PROFIL_BTN_UEBERNEHMEN, Location = new Point(20, 340), Width = 430 };

            btnKopieren.Click += (s, e) =>
            {
                if (!TagUebernehmen(_aktuellerTag, true)) return;
                _tagKopie = new double[24];
                for (int h = 0; h < 24; h++) _tagKopie[h] = _woche[_aktuellerTag, h];
            };

            btnEinfuegen.Click += (s, e) =>
            {
                if (_tagKopie == null)
                {
                    MessageBox.Show(MyResource.Resource.PREIS_PROFIL_MSG_ERST_KOPIEREN,
                        MyResource.Resource.PREIS_PROFIL_TITEL,
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                for (int h = 0; h < 24; h++) _woche[_aktuellerTag, h] = _tagKopie[h];
                TagAnzeigen(_aktuellerTag);
                ChartAktualisieren();
            };

            btnAlleTage.Click += (s, e) =>
            {
                if (!TagUebernehmen(_aktuellerTag, true)) return;
                for (int t = 0; t < 7; t++)
                    for (int h = 0; h < 24; h++)
                        _woche[t, h] = _woche[_aktuellerTag, h];
                ChartAktualisieren();
                MessageBox.Show(MyResource.Resource.PREIS_PROFIL_MSG_ALLE_TAGE,
                    MyResource.Resource.PREIS_PROFIL_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            btnUebernehmen.Click += (s, e) => { if (TagUebernehmen(_aktuellerTag, true)) ChartAktualisieren(); };

            seite.Controls.Add(btnKopieren);
            seite.Controls.Add(btnEinfuegen);
            seite.Controls.Add(btnAlleTage);
            seite.Controls.Add(btnUebernehmen);

            seite.Controls.Add(new Label
            {
                Text = MyResource.Resource.PREIS_PROFIL_HINWEIS_ABWEICHUNG,
                AutoSize = false,
                Size = new Size(430, 34),
                Location = new Point(20, 378)
            });

            return seite;
        }

        private TabPage BaueGrafikSeite()
        {
            TabPage seite = new TabPage(MyResource.Resource.PREIS_PROFIL_TAB_GRAFIK);

            _chart = new Chart
            {
                Location = new Point(10, 10),
                Size = new Size(648, 390)
            };

            // "Jahr" ist der technische Name des Diagrammbereichs (Zugriffsschlüssel,
            // Schicht 2 der Drei-Schichten-Regel) - nur die Achsentitel sind Anzeige.
            ChartArea ca = new ChartArea("Jahr");
            ca.AxisX.Title = MyResource.Resource.CHART_ACHSE_MONAT;
            ca.AxisY.Title = MyResource.Resource.PREIS_CHART_ACHSE_PREIS;
            ca.AxisX.Minimum = 0;
            ca.AxisX.Maximum = 12;
            ca.AxisX.Interval = 1;
            ca.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
            ca.CursorX.IsUserEnabled = true;
            ca.CursorX.IsUserSelectionEnabled = true;
            ca.AxisX.ScaleView.Zoomable = true;
            _chart.ChartAreas.Add(ca);

            Series s = new Series("KOSTENPROFIL")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.FromArgb(180, Color.DarkGreen),
                BorderWidth = 2,
                XValueType = ChartValueType.Double,
                LegendText = MyResource.Resource.PREIS_CHART_SERIE_KOSTENPROFIL
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

            TagUebernehmen(_aktuellerTag, false);
            _aktuellerTag = _lbTag.SelectedIndex;
            TagAnzeigen(_aktuellerTag);
        }

        private void TagAnzeigen(int tag)
        {
            for (int h = 0; h < 24; h++) _tbStunde[h].Text = Anzeige(_woche[tag, h]);
        }

        private bool TagUebernehmen(int tag, bool meldung)
        {
            double[] werte = new double[24];
            for (int h = 0; h < 24; h++)
            {
                double w;
                if (!Program.ZahlParsen(_tbStunde[h].Text, out w))
                {
                    if (meldung)
                        MessageBox.Show(
                            string.Format(MyResource.Resource.PREIS_PROFIL_MSG_STUNDE_UNGUELTIG,
                                          h + 1, _tbStunde[h].Text),
                            MyResource.Resource.PREIS_PROFIL_TITEL,
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
                werte[h] = w;
            }

            for (int h = 0; h < 24; h++) _woche[tag, h] = werte[h];
            return true;
        }

        /// <summary>
        /// Prüft die Eingaben, übernimmt sie und SPEICHERT das Profil. Erst danach
        /// schließt der Dialog mit <c>DialogResult.OK</c>.
        /// </summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            string[] monate = Monatsnamen;
            for (int m = 0; m < 12; m++)
            {
                double w;
                if (!Program.ZahlParsen(_tbMonat[m].Text, out w))
                {
                    MessageBox.Show(
                        string.Format(MyResource.Resource.PREIS_PROFIL_MSG_MONAT_UNGUELTIG,
                                      monate[m], _tbMonat[m].Text),
                        MyResource.Resource.PREIS_PROFIL_TITEL,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                _monat[m] = w;
            }

            if (!TagUebernehmen(_aktuellerTag, true))
            {
                this.DialogResult = DialogResult.None;
                return;
            }

            _modell.ID_Projekt = _idProjekt;
            _modell.Bezeichner = _tbBezeichner.Text.Trim().Length > 0
                ? _tbBezeichner.Text.Trim()
                : MyResource.Resource.PREIS_PROFIL_NEU;
            _modell.Monatswerte = Monatswerte;
            _modell.Wochenwerte = Wochenwerte;

            KostenprofilCtrl ctrl = new KostenprofilCtrl();
            bool ok = _modell.ID > 0 ? ctrl.Update(_modell) : ctrl.Insert(_modell) > 0;

            if (!ok)
            {
                MessageBox.Show(MyResource.Resource.PREIS_PROFIL_MSG_NICHT_GESPEICHERT,
                    MyResource.Resource.PREIS_PROFIL_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
            }
        }

        /// <summary>
        /// Zeichnet die Vorschau - über dieselbe Engine-Methode, die auch die
        /// Simulation verwendet.
        /// </summary>
        private void ChartAktualisieren()
        {
            if (_chart == null) return;

            for (int m = 0; m < 12; m++)
            {
                double w;
                if (Program.ZahlParsen(_tbMonat[m].Text, out w)) _monat[m] = w;
            }

            double[] woche = new double[168];
            for (int t = 0; t < 7; t++)
                for (int h = 0; h < 24; h++)
                    woche[t * 24 + h] = _woche[t, h];

            double[] profil = PreisModell.AusMonatsUndWochenwerten(_monat, woche);

            _chart.Series[0].Points.Clear();
            for (int i = 0; i < RasterAdapter.StundenJahr; i++)
            {
                double x = i * 12.0 / RasterAdapter.StundenJahr;
                _chart.Series[0].Points.AddXY(x, profil[i]);
            }
        }

        private static string Anzeige(double wert)
        {
            return wert.ToString("0.###", CultureInfo.CurrentCulture);
        }
    }
}
