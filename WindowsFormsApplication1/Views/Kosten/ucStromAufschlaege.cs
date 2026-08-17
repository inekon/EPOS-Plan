using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Aufschlagsblock und Vergütungssätze des Strom-Energieträgers
    /// (Fachkonzept Stromspeicher 4.2/4.3, Arbeitspaket AP4).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein eigenes Steuerelement statt weiterer Felder in
    /// <c>ucFuelSettings</c>.</b> <c>ucFuelSettings</c> gilt für JEDEN Energieträger;
    /// Netzentgelt, Umlagen und Stromsteuer gibt es nur beim Strom. Als eingebetteter
    /// Block bleibt der Bestandsdialog unangetastet — <c>ucFuelSettings</c> hängt ihn
    /// nur an, wenn <c>pricing_model = ELECTRICITY</c> ist, und wächst dafür um seine
    /// Höhe. Zudem bleibt <c>ucFuelSettings.Designer.cs</c> unberührt (CLAUDE.md:
    /// Designer-Dateien nicht von Hand editieren).
    /// </para>
    /// <para>
    /// <b>Vollständig programmatisch</b>, ohne Designer und ohne eigene <c>.resx</c> —
    /// dasselbe Vorgehen wie bei der Speicher-Parameterseite aus AP3b. Alle sichtbaren
    /// Texte kommen aus <c>MyResource.Resource.PREIS_*</c> und sind zweisprachig.
    /// </para>
    /// <para>
    /// <b>Kulturregel:</b> Eingabe und Anzeige über <c>Program.ZahlParsen</c> /
    /// <c>CurrentCulture</c> (der deutsche Anwender tippt „6,44"); in die Datenbank geht
    /// ausschließlich der <c>double</c>, nie eine Zeichenkette.
    /// </para>
    /// </remarks>
    public class ucStromAufschlaege : UserControl
    {
        /// <summary>Breite des Blocks — passend zu <c>panel1</c>/<c>dgvHistory</c> in ucFuelSettings.</summary>
        public const int BREITE = 548;

        /// <summary>Gesamthöhe des Blocks.</summary>
        public const int HOEHE = 300;

        private const int SPALTE_SCHALTER = 14;
        private const int SPALTE_FELD = 250;
        private const int SPALTE_EINHEIT = 350;
        private const int ZEILE_HOEHE = 27;

        private readonly StromAufschlagModel _modell;

        private readonly TextBox[] _felder = new TextBox[5];
        private readonly CheckBox[] _schalter = new CheckBox[5];

        private RadioButton _rbAufgeschluesselt;
        private RadioButton _rbGesamtwert;
        private TextBox _tbOverride;
        private Label _lblSumme;
        private Label _lblRest;
        private TextBox _tbVerguetungPv;
        private TextBox _tbVerguetungBhkw;

        /// <summary>Sperrt das Zurückschreiben, solange die Felder programmatisch gefüllt werden.</summary>
        private bool _laden;

        /// <summary>
        /// Erzeugt den Block für eine (Projekt, Energieträger)-Zeile und liest ihren
        /// Stand aus der Datenbank.
        /// </summary>
        public ucStromAufschlaege(int idProjekt, int idEnergietraeger)
        {
            StromAufschlagCtrl.StelleSpaltenSicher();
            _modell = new StromAufschlagCtrl().Read(idProjekt, idEnergietraeger);

            BaueOberflaeche();
            ZeigeModell();
        }

        /// <summary>Der aktuelle Stand — nach <see cref="Uebernehmen"/> der gespeicherte.</summary>
        public StromAufschlagModel Modell
        {
            get { return _modell; }
        }

        // ==================================================================
        // Oberfläche
        // ==================================================================

        private void BaueOberflaeche()
        {
            this.Size = new Size(BREITE, HOEHE);
            this.Font = new Font("Segoe UI", 9f, FontStyle.Regular);

            GroupBox gbAufschlag = new GroupBox
            {
                Text = MyResource.Resource.PREIS_GRUPPE_AUFSCHLAG,
                Location = new Point(0, 0),
                Size = new Size(BREITE, 232),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            this.Controls.Add(gbAufschlag);

            // --- Modusumschalter (Fachkonzept 4.2) ---
            _rbAufgeschluesselt = new RadioButton
            {
                Text = MyResource.Resource.PREIS_MODUS_AUFGESCHLUESSELT,
                Location = new Point(SPALTE_SCHALTER, 22),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Checked = true
            };
            _rbGesamtwert = new RadioButton
            {
                Text = MyResource.Resource.PREIS_MODUS_GESAMTWERT,
                Location = new Point(SPALTE_SCHALTER + 210, 22),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            _rbAufgeschluesselt.CheckedChanged += (s, e) => ModusGewechselt();
            gbAufschlag.Controls.Add(_rbAufgeschluesselt);
            gbAufschlag.Controls.Add(_rbGesamtwert);

            // --- Die fünf Komponenten ---
            string[] beschriftung =
            {
                MyResource.Resource.PREIS_KOMP_NETZENTGELT,
                MyResource.Resource.PREIS_KOMP_UMLAGEN,
                MyResource.Resource.PREIS_KOMP_STROMSTEUER,
                MyResource.Resource.PREIS_KOMP_KONZESSION,
                MyResource.Resource.PREIS_KOMP_VERTRIEB
            };

            int y = 48;
            for (int i = 0; i < 5; i++)
            {
                _schalter[i] = new CheckBox
                {
                    Text = beschriftung[i],
                    Location = new Point(SPALTE_SCHALTER, y + 2),
                    Size = new Size(SPALTE_FELD - SPALTE_SCHALTER - 8, 21),
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                    Checked = true
                };
                _schalter[i].CheckedChanged += (s, e) => SummeAktualisieren();
                gbAufschlag.Controls.Add(_schalter[i]);

                _felder[i] = new TextBox
                {
                    Location = new Point(SPALTE_FELD, y),
                    Size = new Size(92, 23),
                    TextAlign = HorizontalAlignment.Right,
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular)
                };
                _felder[i].TextChanged += (s, e) => { Program.ZahlFaerben(s); SummeAktualisieren(); };
                gbAufschlag.Controls.Add(_felder[i]);

                gbAufschlag.Controls.Add(new Label
                {
                    Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH,
                    Location = new Point(SPALTE_EINHEIT, y + 3),
                    AutoSize = true,
                    Font = new Font("Segoe UI", 9f, FontStyle.Regular)
                });

                y += ZEILE_HOEHE;
            }

            // --- Stromsteuer-Schnellwahl (Fachkonzept 4.2) ---
            int yStromsteuer = 48 + 2 * ZEILE_HOEHE;
            gbAufschlag.Controls.Add(SchnellwahlKnopf(
                StromAufschlagModel.STROMSTEUER_REGELFALL,
                MyResource.Resource.PREIS_STROMSTEUER_REGELFALL, 402, yStromsteuer - 1));
            gbAufschlag.Controls.Add(SchnellwahlKnopf(
                StromAufschlagModel.STROMSTEUER_REDUZIERT,
                MyResource.Resource.PREIS_STROMSTEUER_REDUZIERT, 468, yStromsteuer - 1));

            // --- Live-Summe ---
            _lblSumme = new Label
            {
                Location = new Point(SPALTE_SCHALTER, y + 6),
                Size = new Size(BREITE - 2 * SPALTE_SCHALTER, 20),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            gbAufschlag.Controls.Add(_lblSumme);

            // --- Override-Gesamtwert und der nicht aufgeschlüsselte Rest ---
            y += 30;
            gbAufschlag.Controls.Add(new Label
            {
                Text = MyResource.Resource.PREIS_LABEL_GESAMTAUFSCHLAG,
                Location = new Point(SPALTE_SCHALTER, y + 3),
                Size = new Size(SPALTE_FELD - SPALTE_SCHALTER - 8, 21),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            });

            _tbOverride = new TextBox
            {
                Location = new Point(SPALTE_FELD, y),
                Size = new Size(92, 23),
                TextAlign = HorizontalAlignment.Right,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            _tbOverride.TextChanged += (s, e) => { Program.ZahlFaerben(s); SummeAktualisieren(); };
            gbAufschlag.Controls.Add(_tbOverride);

            gbAufschlag.Controls.Add(new Label
            {
                Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH,
                Location = new Point(SPALTE_EINHEIT, y + 3),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            });

            _lblRest = new Label
            {
                Location = new Point(SPALTE_SCHALTER, y + 28),
                Size = new Size(BREITE - 2 * SPALTE_SCHALTER, 20),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 100, 100)
            };
            gbAufschlag.Controls.Add(_lblRest);

            // --- Vergütung (Fachkonzept 4.3) ---
            GroupBox gbVerguetung = new GroupBox
            {
                Text = MyResource.Resource.PREIS_GRUPPE_VERGUETUNG,
                Location = new Point(0, 238),
                Size = new Size(BREITE, 58),
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            this.Controls.Add(gbVerguetung);

            _tbVerguetungPv = VerguetungsFeld(gbVerguetung, MyResource.Resource.PREIS_LABEL_VERGUETUNG_PV,
                                              SPALTE_SCHALTER, 22);
            _tbVerguetungBhkw = VerguetungsFeld(gbVerguetung, MyResource.Resource.PREIS_LABEL_VERGUETUNG_BHKW,
                                                290, 22);
        }

        private Button SchnellwahlKnopf(double wert, string beschriftung, int x, int y)
        {
            Button b = new Button
            {
                Text = beschriftung,
                Location = new Point(x, y),
                Size = new Size(62, 24),
                Font = new Font("Segoe UI", 8f, FontStyle.Regular)
            };
            b.Click += (s, e) =>
            {
                _felder[2].Text = wert.ToString("0.###", CultureInfo.CurrentCulture);
                _schalter[2].Checked = true;
            };
            return b;
        }

        private TextBox VerguetungsFeld(GroupBox eltern, string beschriftung, int x, int y)
        {
            eltern.Controls.Add(new Label
            {
                Text = beschriftung,
                Location = new Point(x, y + 3),
                Size = new Size(150, 21),
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            });

            TextBox tb = new TextBox
            {
                Location = new Point(x + 152, y),
                Size = new Size(70, 23),
                TextAlign = HorizontalAlignment.Right,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
            tb.TextChanged += (s, e) => Program.ZahlFaerben(s);
            eltern.Controls.Add(tb);

            eltern.Controls.Add(new Label
            {
                Text = DbWerte.PREISREIHE_EINHEIT_CT_KWH,
                Location = new Point(x + 226, y + 3),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            });

            return tb;
        }

        // ==================================================================
        // Modell <-> Oberfläche
        // ==================================================================

        private void ZeigeModell()
        {
            _laden = true;
            try
            {
                _felder[0].Text = Anzeige(_modell.Netzentgelt);
                _felder[1].Text = Anzeige(_modell.Umlagen);
                _felder[2].Text = Anzeige(_modell.Stromsteuer);
                _felder[3].Text = Anzeige(_modell.Konzession);
                _felder[4].Text = Anzeige(_modell.Vertrieb);

                _schalter[0].Checked = _modell.Netzentgelt_Aktiv;
                _schalter[1].Checked = _modell.Umlagen_Aktiv;
                _schalter[2].Checked = _modell.Stromsteuer_Aktiv;
                _schalter[3].Checked = _modell.Konzession_Aktiv;
                _schalter[4].Checked = _modell.Vertrieb_Aktiv;

                _tbOverride.Text = Anzeige(_modell.Override);
                _tbVerguetungPv.Text = Anzeige(_modell.Verguetung_PV);
                _tbVerguetungBhkw.Text = Anzeige(_modell.Verguetung_BHKW);

                bool gesamtwert = _modell.Modus == DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT;
                _rbGesamtwert.Checked = gesamtwert;
                _rbAufgeschluesselt.Checked = !gesamtwert;
            }
            finally
            {
                _laden = false;
            }

            ModusGewechselt();
        }

        /// <summary>
        /// Liest die Felder in das Modell zurück. Unlesbare Felder behalten den
        /// bisherigen Wert — die Rückmeldung gibt die Einfärbung
        /// (<c>Program.ZahlFaerben</c>), nicht eine modale Meldung.
        /// </summary>
        public void InsModell()
        {
            _modell.Netzentgelt = Zahl(_felder[0], _modell.Netzentgelt);
            _modell.Umlagen = Zahl(_felder[1], _modell.Umlagen);
            _modell.Stromsteuer = Zahl(_felder[2], _modell.Stromsteuer);
            _modell.Konzession = Zahl(_felder[3], _modell.Konzession);
            _modell.Vertrieb = Zahl(_felder[4], _modell.Vertrieb);

            _modell.Netzentgelt_Aktiv = _schalter[0].Checked;
            _modell.Umlagen_Aktiv = _schalter[1].Checked;
            _modell.Stromsteuer_Aktiv = _schalter[2].Checked;
            _modell.Konzession_Aktiv = _schalter[3].Checked;
            _modell.Vertrieb_Aktiv = _schalter[4].Checked;

            _modell.Override = Zahl(_tbOverride, _modell.Override);
            _modell.Verguetung_PV = Zahl(_tbVerguetungPv, _modell.Verguetung_PV);
            _modell.Verguetung_BHKW = Zahl(_tbVerguetungBhkw, _modell.Verguetung_BHKW);

            _modell.Modus = _rbGesamtwert.Checked
                ? DbWerte.SP_AUFSCHLAG_MODUS_GESAMTWERT
                : DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT;
        }

        /// <summary>
        /// Übernimmt die Eingaben und schreibt sie zurück. Rückgabe false, wenn es
        /// keine Zeile in <c>energy_project_settings</c> gibt — dann ist der
        /// Energieträger dem Projekt nicht zugeordnet.
        /// </summary>
        public bool Uebernehmen()
        {
            InsModell();
            return new StromAufschlagCtrl().Update(_modell);
        }

        // ==================================================================
        // Live-Rechnung (die Formeln stehen in der Engine)
        // ==================================================================

        private void ModusGewechselt()
        {
            bool aufgeschluesselt = _rbAufgeschluesselt.Checked;

            // Im Override-Modus bleiben die Komponenten SICHTBAR und lesbar
            // (Fachkonzept 4.2: "die Komponentenliste bleibt sichtbar und informativ"),
            // aber sie steuern den Rechenweg nicht mehr.
            for (int i = 0; i < 5; i++)
            {
                _felder[i].ReadOnly = !aufgeschluesselt;
                _felder[i].BackColor = aufgeschluesselt ? SystemColors.Window : SystemColors.Control;
                _schalter[i].Enabled = aufgeschluesselt;
            }

            _tbOverride.Enabled = !aufgeschluesselt;
            SummeAktualisieren();
        }

        private void SummeAktualisieren()
        {
            if (_laden) return;

            InsModell();
            SpeicherEngine.Aufschlagssatz satz = StromAufschlagCtrl.AlsAufschlagssatz(_modell);

            _lblSumme.Text = string.Format(MyResource.Resource.PREIS_SUMME_AKTIV,
                                           Anzeige(satz.SummeAktivCtKwh),
                                           Anzeige(satz.WirksamCtKwh));

            _lblRest.Text = _rbGesamtwert.Checked
                ? string.Format(MyResource.Resource.PREIS_REST_NICHT_AUFGESCHLUESSELT,
                                Anzeige(satz.NichtAufgeschluesselterRestCtKwh))
                : MyResource.Resource.PREIS_REST_HINWEIS_MODUS;

            _lblRest.ForeColor = satz.NichtAufgeschluesselterRestCtKwh < 0.0
                ? Color.Firebrick
                : Color.FromArgb(100, 100, 100);
        }

        private static double Zahl(TextBox feld, double vorgabe)
        {
            double w;
            return Program.ZahlParsen(feld.Text, out w) ? w : vorgabe;
        }

        private static string Anzeige(double wert)
        {
            return wert.ToString("0.###", CultureInfo.CurrentCulture);
        }
    }
}
