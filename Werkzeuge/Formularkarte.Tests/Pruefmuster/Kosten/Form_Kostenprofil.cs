using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Kostenprofil-Editor (Fachkonzept Stromspeicher 4.1 b, Arbeitspaket AP4):
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
    /// <b>Ä17 (26.08.2026): Designer-fähig umgestellt</b> (FK1/Ä6-Regel der
    /// Kostendialoge). Das statische Gerüst — Kopf, Reiter, Knöpfe, Diagramm —
    /// steht in <c>Form_Kostenprofil.Designer.cs</c> mit deutschen
    /// Vorgabetexten; der Konstruktor überschreibt aus <c>MyResource</c>.
    /// Nur die beiden WERTERASTER (12 Monats- und 24 Stundenfelder) entstehen
    /// wie beim Positionsraster der Kostenverwaltung zur Laufzeit — sie sind
    /// Schleifenware, keine Layoutarbeit.
    /// </para>
    /// <para>
    /// Die Vorschau rechnet über <c>SpeicherEngine.PreisModell</c> — der
    /// Anwender sieht GENAU die Reihe, mit der die Simulation später rechnet.
    /// Ablageformat: zwei <c>";"</c>-getrennte Zeichenketten mit
    /// <see cref="CultureInfo.InvariantCulture"/>; die EINGABE folgt der Kultur
    /// des Anwenders (<c>Program.ZahlParsen</c> nimmt Komma und Punkt).
    /// </para>
    /// </remarks>
    public partial class Form_Kostenprofil : Form
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

            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            TexteAnwenden();
            MonatsRasterBauen();
            StundenRasterBauen();
            ChartKonfigurieren();
            lbTag.Items.AddRange(Wochentagsnamen);

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
            tbBezeichner.Text = _modell.Bezeichner;
            for (int m = 0; m < 12; m++) _tbMonat[m].Text = Anzeige(_monat[m]);

            lbTag.SelectedIndex = 0;
            TagAnzeigen(0);
            ChartAktualisieren();
        }

        // ------------------------------------------------------------------
        // Oberfläche: Texte, dynamische Raster, Diagramm
        // ------------------------------------------------------------------

        /// <summary>Designer-Vorgaben (deutsch) durch MyResource-Texte ersetzen
        /// (Ä6-Regel 2 — Designer-Vorschau und Lokalisierung bleiben intakt).</summary>
        private void TexteAnwenden()
        {
            Text = MyResource.Resource.PREIS_PROFIL_TITEL;
            lblInfo.Text = MyResource.Resource.PREIS_PROFIL_INFO;
            lblName.Text = MyResource.Resource.PREIS_PROFIL_LABEL_BEZEICHNER;
            tpMonat.Text = MyResource.Resource.PREIS_PROFIL_TAB_MONATSWERTE;
            tpWoche.Text = MyResource.Resource.PREIS_PROFIL_TAB_WOCHENWERTE;
            tpGrafik.Text = MyResource.Resource.PREIS_PROFIL_TAB_GRAFIK;
            lblKopfMonat.Text = MyResource.Resource.PREIS_PROFIL_KOPF_MONAT;
            lblKopfWoche.Text = MyResource.Resource.PREIS_PROFIL_KOPF_WOCHE;
            lblWochentag.Text = MyResource.Resource.PREIS_PROFIL_LBL_WOCHENTAG;
            lblHinweisAbweichung.Text = MyResource.Resource.PREIS_PROFIL_HINWEIS_ABWEICHUNG;
            btnAlleMonate.Text = MyResource.Resource.PREIS_PROFIL_BTN_ALLE_MONATE;
            btnTagKopieren.Text = MyResource.Resource.PREIS_PROFIL_BTN_TAG_KOPIEREN;
            btnTagEinfuegen.Text = MyResource.Resource.PREIS_PROFIL_BTN_TAG_EINFUEGEN;
            btnAlleTage.Text = MyResource.Resource.PREIS_PROFIL_BTN_ALLE_TAGE;
            btnTagUebernehmen.Text = MyResource.Resource.PREIS_PROFIL_BTN_UEBERNEHMEN;
            btnOk.Text = MyResource.Resource.SIM_BTN_OK;
            btnAbbruch.Text = MyResource.Resource.SIM_BTN_ABBRECHEN;
        }

        /// <summary>Die 12 Monatszeilen (Label · Feld · Einheit) — Schleifenware
        /// zur Laufzeit, wie das Positionsraster der Kostenverwaltung (Ä6).</summary>
        private void MonatsRasterBauen()
        {
            string[] monate = Monatsnamen;
            for (int m = 0; m < 12; m++)
            {
                int spalte = m / 6;
                int zeile = m % 6;

                tpMonat.Controls.Add(new Label
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
                tpMonat.Controls.Add(_tbMonat[m]);

                tpMonat.Controls.Add(new Label
                {
                    Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH,
                    AutoSize = true,
                    Location = new Point(228 + spalte * 320, 56 + zeile * 42)
                });
            }
        }

        /// <summary>Die 24 Stundenzeilen des gewählten Wochentags — Schleifenware.</summary>
        private void StundenRasterBauen()
        {
            for (int h = 0; h < 24; h++)
            {
                int spalte = h / 8;
                int zeile = h % 8;

                tpWoche.Controls.Add(new Label
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
                tpWoche.Controls.Add(_tbStunde[h]);
            }
        }

        /// <summary>Diagrammbereich und Serie — die Chart-Feinkonfiguration bleibt
        /// im Code (die Designer-Serialisierung des Chart ist fehleranfällig).</summary>
        private void ChartKonfigurieren()
        {
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
            chart.ChartAreas.Add(ca);

            Series s = new Series("KOSTENPROFIL")
            {
                ChartType = SeriesChartType.Line,
                Color = Color.FromArgb(180, Color.DarkGreen),
                BorderWidth = 2,
                XValueType = ChartValueType.Double,
                LegendText = MyResource.Resource.PREIS_CHART_SERIE_KOSTENPROFIL
            };
            chart.Series.Add(s);
        }

        // ------------------------------------------------------------------
        // Ereignisse
        // ------------------------------------------------------------------

        private void tabs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabs.SelectedIndex == 2) ChartAktualisieren();
        }

        private void btnAlleMonate_Click(object sender, EventArgs e)
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
        }

        private void btnTagKopieren_Click(object sender, EventArgs e)
        {
            if (!TagUebernehmen(_aktuellerTag, true)) return;
            _tagKopie = new double[24];
            for (int h = 0; h < 24; h++) _tagKopie[h] = _woche[_aktuellerTag, h];
        }

        private void btnTagEinfuegen_Click(object sender, EventArgs e)
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
        }

        private void btnAlleTage_Click(object sender, EventArgs e)
        {
            if (!TagUebernehmen(_aktuellerTag, true)) return;
            for (int t = 0; t < 7; t++)
                for (int h = 0; h < 24; h++)
                    _woche[t, h] = _woche[_aktuellerTag, h];
            ChartAktualisieren();
            MessageBox.Show(MyResource.Resource.PREIS_PROFIL_MSG_ALLE_TAGE,
                MyResource.Resource.PREIS_PROFIL_TITEL,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnTagUebernehmen_Click(object sender, EventArgs e)
        {
            if (TagUebernehmen(_aktuellerTag, true)) ChartAktualisieren();
        }

        private void lbTag_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbTag.SelectedIndex < 0) return;

            TagUebernehmen(_aktuellerTag, false);
            _aktuellerTag = lbTag.SelectedIndex;
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
            _modell.Bezeichner = tbBezeichner.Text.Trim().Length > 0
                ? tbBezeichner.Text.Trim()
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
            if (chart == null || chart.Series.Count == 0) return;

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

            chart.Series[0].Points.Clear();
            for (int i = 0; i < RasterAdapter.StundenJahr; i++)
            {
                double x = i * 12.0 / RasterAdapter.StundenJahr;
                chart.Series[0].Points.AddXY(x, profil[i]);
            }
        }

        private static string Anzeige(double wert)
        {
            return wert.ToString("0.###", CultureInfo.CurrentCulture);
        }
    }
}
