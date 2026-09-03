using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eingabe des QUELLPROFILS einer Wärmequelle.
    ///
    /// <para><b>PAKET Q1 (Konzept 8.1 Punkt 2/3, Kapitel 10).</b> Der Dialog pflegt
    /// nicht mehr zwei delimitierte Zeichenketten an der Anlage, sondern einen eigenen
    /// Gegenstand: eine Zeile in <c>Tab_Quellprofil</c> mit ihrem Wertesatz in
    /// <c>Tab_QuellprofilDaten</c> (Migrationsschritt 54). Die Anlage verweist über
    /// <c>WQ_ID_Quellprofil</c> darauf — Schlüssel- statt Indexkopplung.</para>
    ///
    /// <para><b>Drei Betriebsarten</b> (<c>DbWerte.WQ_PROFIL_BETRIEBSART_*</c>):
    /// <list type="bullet">
    ///   <item><b>Monat</b> — 12 Eingabefelder, wie bisher.</item>
    ///   <item><b>Tag</b> — 365 Werte. Bewusst KEIN Formular mit 365 Feldern, sondern
    ///   eine Tabelle mit CSV-Import: Ein Tageswertsatz kommt aus einer Messreihe, nicht
    ///   aus 365 Tastatureingaben. <b>Kalenderunabhängig</b> — Tag <c>i</c> gilt für die
    ///   24 Stunden des Tages <c>i</c>, ohne Wochentagsbezug.</item>
    ///   <item><b>Stunde</b> — 8760 Werte, ebenfalls über den Import. Damit kommt das
    ///   Stundenprofil in die DATENBANK statt als Dateipfad an der Anlage zu stehen
    ///   (Konzept 8.1 Punkt 3); die Bemessung gegen die 2-GB-Grenze steht bei
    ///   <c>SchemaKatalog.TAB_QUELLPROFILDATEN</c>.</item>
    /// </list></para>
    ///
    /// <para><b>Der additive Wochengang (168 Werte) ist Altweg</b> und wird hier nur
    /// noch ANGEZEIGT, wenn die Anlage einen trägt. Er lebt in
    /// <c>WQ_Wochenwerte</c> weiter und wird von der Engine gerechnet, solange kein
    /// Quellprofil gewählt ist (<c>WaermequelleClass.Quelltemperatur</c>, Stufe 2). Ein
    /// gespeichertes Quellprofil setzt ihn außer Kraft — deshalb steht das auf der Seite
    /// und deshalb ist sie nicht bearbeitbar: Eine Eingabe ohne Wirkung wäre schlimmer
    /// als keine. Die Tagesvariante ist sein Nachfolger und braucht keinen Kalender.</para>
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

        /// <summary>Steuerwert der ComboBox-Zeile „&lt;neues Profil&gt;" (Schicht 2, ASCII).</summary>
        private const int PROFIL_NEU = 0;

        /// <summary>Monats-Mitteltemperaturen der Wärmequelle [°C]</summary>
        private double[] _monat = new double[12];

        /// <summary>Tagesgang je Wochentag als Abweichung vom Monatswert [K] — ALTWEG, nur Anzeige</summary>
        private double[,] _woche = new double[7, 24];

        /// <summary>true = die Anlage trägt einen Wochengang aus dem Altweg (mindestens ein Wert ≠ 0).</summary>
        private bool _wochengangVorhanden;

        /// <summary>
        /// Werte der Betriebsarten Tag (365) und Stunde (8760). Länge folgt der
        /// Betriebsart; <c>null</c>, solange Monat gewählt ist.
        /// </summary>
        private double[] _werte;

        private TextBox[] _tbMonat = new TextBox[12];
        private TextBox[] _tbStunde = new TextBox[24];
        private ListBox _lbTag;
        private Chart _chart;
        private Label _lblInfo;
        private int _aktuellerTag = 0;

        private TabControl _tabs;
        private TabPage _seiteMonat, _seiteWoche, _seiteWerte, _seiteGrafik;
        private ComboBox _cbProfil, _cbBetriebsart;
        private TextBox _tbBezeichner, _tbBeschreibung;
        private DataGridView _grid;
        private DataTable _gridTabelle;
        private Label _lblWerteInfo, _lblWerteHinweis;
        private bool _aufbau;

        /// <summary>Die Profile des Projekts, in der Reihenfolge der Auswahlliste.</summary>
        private List<QuellprofilCtrl.Kopf> _profile = new List<QuellprofilCtrl.Kopf>();

        /// <summary>Name der Wärmepumpe (nur für den Fenstertitel).</summary>
        public string WPName = "";

        /// <summary>Projekt, zu dem ein neu angelegtes Profil gehört (Q1).</summary>
        public int ID_Projekt;

        /// <summary>
        /// EIN- UND AUSGABE (Q1): das gewählte Quellprofil
        /// (<c>Tab_Energieanlagen.WQ_ID_Quellprofil</c>); 0 = keines. Nach
        /// <c>DialogResult.OK</c> steht hier die ID des gespeicherten Profils.
        /// </summary>
        public int ID_Quellprofil;

        public Form_Quellprofil()
        {
            BaueOberflaeche();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
            FensterEinpassung.Einhaengen(this);
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
        // Laden / Speichern der Werte als Zeichenkette (Altweg-Spalten)
        // ------------------------------------------------------------------

        /// <summary>
        /// Monatswerte als "t1;...;t12" (Punkt als Dezimaltrennzeichen).
        ///
        /// <para><b>Q1:</b> Nur noch EINGANG — die Vorbelegung aus dem Altweg
        /// <c>WQ_Monatswerte</c>, wenn die Anlage noch kein Quellprofil hat. Der
        /// Rückgabeweg bleibt für die Diagrammvorschau bestehen; geschrieben wird die
        /// Spalte vom Aufrufer nicht mehr.</para>
        /// </summary>
        /// <remarks>
        /// iU9-W10a.0b (Befund W10-B21): Der Parser und sein Gegenstueck stehen jetzt in
        /// QuellprofilCtrl (MonatswerteParsen/MonatswerteText); hier bleibt die
        /// Eigenschaft als Uebergabefeld des Aufrufers.
        /// </remarks>
        public string Monatswerte
        {
            get { return QuellprofilCtrl.MonatswerteText(_monat); }
            set { _monat = QuellprofilCtrl.MonatswerteParsen(value); }
        }

        /// <summary>
        /// Wochenwerte als 168 Werte "w1;...;w168" (Montag 0 Uhr bis Sonntag 23 Uhr).
        ///
        /// <para><b>Q1: ALTWEG, nur Anzeige.</b> Der Wochengang ist nicht Teil des
        /// Quellprofil-Modells; er bleibt in <c>WQ_Wochenwerte</c> stehen und wird von
        /// der Engine gerechnet, solange die Anlage kein Quellprofil führt.</para>
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
                // iU9-W10a.0b (Befund W10-B21): Der Parser steht jetzt in
                // QuellprofilCtrl.WochenwerteParsen; null heisst dort wie hier
                // "kein Wochengang" (alle Werte 0 zaehlen nicht).
                Array.Clear(_woche, 0, _woche.Length); // Vorgabe: keine Abweichung
                double[] gelesen = QuellprofilCtrl.WochenwerteParsen(value);
                _wochengangVorhanden = gelesen != null;
                if (gelesen == null) return;

                for (int i = 0; i < 168; i++) _woche[i / 24, i % 24] = gelesen[i];
            }
        }

        /// <summary>Die gerade gewählte Betriebsart (Steuerwert aus <c>DbWerte</c>).</summary>
        private string Betriebsart
        {
            get
            {
                object tag = (_cbBetriebsart != null && _cbBetriebsart.SelectedItem is SchluesselEintrag)
                    ? ((SchluesselEintrag)_cbBetriebsart.SelectedItem).Wert : null;
                return (tag as string) ?? DbWerte.WQ_PROFIL_BETRIEBSART_MONAT;
            }
        }

        /// <summary>Übernimmt die geladenen Werte in die Eingabefelder.</summary>
        public void SetControls()
        {
            if (!string.IsNullOrEmpty(WPName))
                this.Text = string.Format(MyResource.Resource.SIMQ_QUELLPROFIL_TITEL_MIT_WP, WPName);

            _aufbau = true;
            try
            {
                ProfillisteLaden();

                // Ein bereits gewähltes Profil überschreibt die Altweg-Vorbelegung.
                if (ID_Quellprofil > 0) ProfilUebernehmen(ID_Quellprofil);
                else BetriebsartSetzen(DbWerte.WQ_PROFIL_BETRIEBSART_MONAT);

                for (int m = 0; m < 12; m++) _tbMonat[m].Text = _monat[m].ToString("F1");

                _lbTag.SelectedIndex = 0;
                TagAnzeigen(0);
            }
            finally { _aufbau = false; }

            SeitenAnpassen();
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
            this.ClientSize = new Size(700, 612);

            _lblInfo = new Label
            {
                AutoSize = false,
                Location = new Point(12, 10),
                Size = new Size(676, 32),
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_INFO
            };
            this.Controls.Add(_lblInfo);

            // --- Kopfzeilen: Profilauswahl, Bezeichnung, Betriebsart, Beschreibung ----
            // Feste Spaltenmaße: 108 px Beschriftung tragen beide Sprachen („Betriebsart:"
            // 70 px, „Operating mode:" 95 px; „Beschreibung:" 85 px, „Description:" 70 px).
            this.Controls.Add(Beschriftung(MyResource.Resource.SIMQ_QUELLPROFIL_LBL_PROFIL, 12, 50));
            _cbProfil = new ComboBox
            {
                Location = new Point(124, 47),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbProfil.SelectedIndexChanged += cbProfil_SelectedIndexChanged;
            this.Controls.Add(_cbProfil);

            this.Controls.Add(Beschriftung(MyResource.Resource.SIMQ_QUELLPROFIL_LBL_BETRIEBSART, 356, 50));
            _cbBetriebsart = new ComboBox
            {
                Location = new Point(468, 47),
                Width = 220,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            _cbBetriebsart.Items.Add(new SchluesselEintrag(DbWerte.WQ_PROFIL_BETRIEBSART_MONAT,
                                                           MyResource.Resource.SIMQ_QUELLPROFIL_BA_MONAT));
            _cbBetriebsart.Items.Add(new SchluesselEintrag(DbWerte.WQ_PROFIL_BETRIEBSART_TAG,
                                                           MyResource.Resource.SIMQ_QUELLPROFIL_BA_TAG));
            _cbBetriebsart.Items.Add(new SchluesselEintrag(DbWerte.WQ_PROFIL_BETRIEBSART_STUNDE,
                                                           MyResource.Resource.SIMQ_QUELLPROFIL_BA_STUNDE));
            _cbBetriebsart.SelectedIndex = 0;
            _cbBetriebsart.SelectedIndexChanged += cbBetriebsart_SelectedIndexChanged;
            this.Controls.Add(_cbBetriebsart);

            this.Controls.Add(Beschriftung(MyResource.Resource.SIMQ_QUELLPROFIL_LBL_BEZEICHNER, 12, 80));
            _tbBezeichner = new TextBox { Location = new Point(124, 77), Width = 220 };
            this.Controls.Add(_tbBezeichner);

            this.Controls.Add(Beschriftung(MyResource.Resource.SIMQ_QUELLPROFIL_LBL_BESCHREIBUNG, 356, 80));
            _tbBeschreibung = new TextBox { Location = new Point(468, 77), Width = 220 };
            this.Controls.Add(_tbBeschreibung);

            _tabs = new TabControl
            {
                Location = new Point(12, 112),
                Size = new Size(676, 440)
            };
            this.Controls.Add(_tabs);

            _seiteMonat = BaueMonatsSeite();
            _seiteWoche = BaueWochenSeite();
            _seiteWerte = BaueWerteSeite();
            _seiteGrafik = BaueGrafikSeite();

            _tabs.SelectedIndexChanged += delegate
            {
                if (_tabs.SelectedTab == _seiteGrafik) ChartAktualisieren();
            };

            Button btnOk = new Button
            {
                Text = MyResource.Resource.SIM_BTN_OK,
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 190, 572),
                Size = new Size(FusszeilenNorm.BREITE, FusszeilenNorm.HOEHE)
            };
            Button btnAbbruch = new Button
            {
                Text = MyResource.Resource.SIM_BTN_ABBRECHEN,
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 97, 572),
                Size = new Size(FusszeilenNorm.BREITE, FusszeilenNorm.HOEHE)
            };
            btnOk.Click += btnOk_Click;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbruch);
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbruch;

            // HinweiszeileEinpassen() vergrößert das Fenster, wenn die Hinweiszeile mehr
            // Zeilen braucht — die Fußzeile muss danach ausgerichtet werden.
            HinweiszeileEinpassen();

            // D2 (28.08.2026): OK links neben Abbrechen, 85×23, Top/Left -> Norm.
            FusszeilenNorm.Einhaengen(this, btnOk, btnAbbruch);
            FusszeilenNorm.Anwenden(this, btnOk, btnAbbruch);
        }

        /// <summary>
        /// Gibt der Hinweiszeile die Höhe, die ihr Text bei der vorhandenen Breite
        /// wirklich braucht, und lässt alles darunter nachrücken.
        /// </summary>
        /// <remarks>
        /// D-CHECK 28.08.2026: Die Zeile war mit 32 px auf zwei Textzeilen ausgelegt; der
        /// deutsche Text braucht bei 676 px Breite drei (45 px), die dritte war
        /// abgeschnitten. Fest verdrahtete 45 px wären nur für EINE Sprache richtig —
        /// die englische Fassung ist anders lang. Deshalb gemessen, und die Maske wächst
        /// um denselben Betrag (dasselbe „Platz schaffen" wie in
        /// <c>Form_PufferSp_Projekt.KlassenSetAufbauen</c>).
        /// </remarks>
        private void HinweiszeileEinpassen()
        {
            if (_lblInfo == null) return;

            int noetig = _lblInfo.GetPreferredSize(new Size(_lblInfo.Width, 0)).Height;
            int delta = noetig - _lblInfo.Height;
            if (delta <= 0) return;

            foreach (Control c in this.Controls)
                if (!ReferenceEquals(c, _lblInfo) && c.Top > _lblInfo.Top) c.Top += delta;

            _lblInfo.Height = noetig;
            this.ClientSize = new Size(this.ClientSize.Width, this.ClientSize.Height + delta);
        }

        /// <summary>Beschriftung fester Breite — die englischen Texte sind länger als die deutschen.</summary>
        private static Label Beschriftung(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                AutoSize = false,
                Size = new Size(108, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(x, y)
            };
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
            btnAlle.Click += delegate
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

        /// <summary>
        /// Die ALTWEG-Seite: der additive Wochengang aus <c>WQ_Wochenwerte</c>.
        ///
        /// <para>Sie erscheint nur, wenn die Anlage einen Wochengang trägt, und ist
        /// NICHT bearbeitbar — mit einem gespeicherten Quellprofil rechnet die Engine
        /// ihn nicht mehr (Konzept 8.1 Punkt 2: die Tagesvariante ist sein Nachfolger).
        /// Eingabefelder, die niemand liest, wären eine Zusage ohne Wirkung; ihn ganz
        /// wegzulassen hieße, gepflegte Daten stillschweigend verschwinden zu lassen.</para>
        /// </summary>
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
                    Text = Vorgabe(VORGABE_WOCHENWERT),
                    ReadOnly = true
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

            seite.Controls.Add(lblTag);
            seite.Controls.Add(_lbTag);

            Label hinweis = new Label
            {
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_HINWEIS_ALTWEG,
                AutoSize = false,
                Size = new Size(620, 64),
                Location = new Point(20, 320)
            };
            seite.Controls.Add(hinweis);

            return seite;
        }

        /// <summary>
        /// Die WERTE-Seite der Betriebsarten Tag (365) und Stunde (8760) —
        /// Tabelle statt Formular.
        ///
        /// <para><b>Warum eine Tabelle mit Import und keine 365 Eingabefelder</b>
        /// (Konzept 10, Auftrag Q1): Ein Tages- oder Stundenwertsatz stammt aus einer
        /// Messreihe oder einer Norm-Auswertung, nicht aus Tastatureingaben. Der Import
        /// liest ANSI-kodiert (<c>WaermequelleClass.WerteAusCsv</c>) — deutsche
        /// Zählerexporte sind fast nie UTF-8.</para>
        ///
        /// <para>Das Raster hängt an einer <see cref="DataTable"/> statt an
        /// <c>Rows.Add</c>: 8760 einzeln angelegte Zeilen brauchen mehrere Sekunden,
        /// eine Bindung zeigt sie sofort.</para>
        /// </summary>
        private TabPage BaueWerteSeite()
        {
            TabPage seite = new TabPage(MyResource.Resource.SIMQ_QUELLPROFIL_TAB_TAGESWERTE);

            _lblWerteHinweis = new Label
            {
                AutoSize = false,
                Size = new Size(640, 48),
                Location = new Point(14, 10),
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_HINWEIS_TAG
            };
            seite.Controls.Add(_lblWerteHinweis);

            _gridTabelle = new DataTable();
            _gridTabelle.Columns.Add("Nr", typeof(int));
            _gridTabelle.Columns.Add("Wert", typeof(double));

            _grid = new DataGridView
            {
                Location = new Point(14, 64),
                Size = new Size(400, 300),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                AutoGenerateColumns = true,
                DataSource = _gridTabelle
            };
            _grid.DataBindingComplete += delegate
            {
                if (_grid.Columns.Count < 2) return;
                _grid.Columns[0].HeaderText = MyResource.Resource.SIMQ_QUELLPROFIL_SPALTE_NR;
                _grid.Columns[0].ReadOnly = true;
                _grid.Columns[0].Width = 80;
                _grid.Columns[1].HeaderText = MyResource.Resource.SIMQ_QUELLPROFIL_SPALTE_WERT;
                _grid.Columns[1].Width = 260;
                _grid.Columns[1].DefaultCellStyle.Format = "F1";
            };
            _grid.CellValueChanged += delegate { WerteInfoAktualisieren(); };
            seite.Controls.Add(_grid);

            Button btnCsv = new Button
            {
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_BTN_CSV,
                Location = new Point(430, 64),
                Width = 210
            };
            btnCsv.Click += btnCsv_Click;
            seite.Controls.Add(btnCsv);

            Button btnAlle = new Button
            {
                Text = MyResource.Resource.SIMQ_QUELLPROFIL_BTN_ALLE_WERTE,
                Location = new Point(430, 98),
                Width = 210
            };
            btnAlle.Click += btnAlleWerte_Click;
            seite.Controls.Add(btnAlle);

            _lblWerteInfo = new Label
            {
                AutoSize = false,
                Size = new Size(210, 90),
                Location = new Point(430, 140)
            };
            seite.Controls.Add(_lblWerteInfo);

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
        // Profilliste und Betriebsart
        // ------------------------------------------------------------------

        private void ProfillisteLaden()
        {
            _profile = QuellprofilCtrl.LesenJeProjekt(ID_Projekt);

            _cbProfil.Items.Clear();
            _cbProfil.Items.Add(new SchluesselEintrag(PROFIL_NEU,
                                                      MyResource.Resource.SIMQ_QUELLPROFIL_NEU));
            foreach (QuellprofilCtrl.Kopf k in _profile)
                _cbProfil.Items.Add(new SchluesselEintrag(k.ID, k.ToString()));

            _cbProfil.SelectedIndex = 0;
            for (int i = 1; i < _cbProfil.Items.Count; i++)
            {
                if ((int)((SchluesselEintrag)_cbProfil.Items[i]).Wert != ID_Quellprofil) continue;
                _cbProfil.SelectedIndex = i;
                break;
            }
        }

        private void cbProfil_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_aufbau || _cbProfil.SelectedItem == null) return;

            int id = (int)((SchluesselEintrag)_cbProfil.SelectedItem).Wert;

            _aufbau = true;
            try
            {
                if (id == PROFIL_NEU)
                {
                    _tbBezeichner.Text = "";
                    _tbBeschreibung.Text = "";
                }
                else ProfilUebernehmen(id);
            }
            finally { _aufbau = false; }

            SeitenAnpassen();
            ChartAktualisieren();
        }

        /// <summary>Lädt Kopf und Werte eines gespeicherten Profils in die Oberfläche.</summary>
        private void ProfilUebernehmen(int idProfil)
        {
            QuellprofilCtrl.Kopf k = QuellprofilCtrl.Lesen(idProfil);
            if (k == null) return;

            _tbBezeichner.Text = k.Bezeichner;
            _tbBeschreibung.Text = k.Beschreibung;
            BetriebsartSetzen(k.Betriebsart);

            double[] werte = QuellprofilCtrl.WerteLesen(idProfil);
            int soll = DbWerte.QuellprofilWerteanzahl(k.Betriebsart);
            if (werte == null || soll <= 0) return;

            if (k.Betriebsart == DbWerte.WQ_PROFIL_BETRIEBSART_MONAT)
            {
                for (int m = 0; m < 12 && m < werte.Length; m++)
                {
                    _monat[m] = werte[m];
                    _tbMonat[m].Text = _monat[m].ToString("F1");
                }
                return;
            }

            _werte = new double[soll];
            Array.Copy(werte, _werte, Math.Min(soll, werte.Length));
            GridFuellen();
        }

        private void BetriebsartSetzen(string betriebsart)
        {
            for (int i = 0; i < _cbBetriebsart.Items.Count; i++)
            {
                if ((string)((SchluesselEintrag)_cbBetriebsart.Items[i]).Wert != betriebsart) continue;
                _cbBetriebsart.SelectedIndex = i;
                return;
            }
            _cbBetriebsart.SelectedIndex = 0;   // unbekannter Altwert -> Monat
        }

        private void cbBetriebsart_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_aufbau) return;

            int soll = DbWerte.QuellprofilWerteanzahl(Betriebsart);
            if (soll > 0 && Betriebsart != DbWerte.WQ_PROFIL_BETRIEBSART_MONAT)
            {
                // Längenwechsel Tag <-> Stunde: Was passt, bleibt; der Rest bekommt die
                // Vorgabe. Ein stilles Abschneiden wäre ein Datenverlust ohne Ansage,
                // ein Verwerfen der ganzen Eingabe eine Überreaktion.
                double[] neu = new double[soll];
                for (int i = 0; i < soll; i++)
                    neu[i] = (_werte != null && i < _werte.Length) ? _werte[i] : VORGABE_MONATSWERT;
                _werte = neu;
                GridFuellen();
            }

            SeitenAnpassen();
            ChartAktualisieren();
        }

        /// <summary>
        /// Blendet die Seiten je Betriebsart um: Monat zeigt die zwölf Felder, Tag und
        /// Stunde die Werteseite. Die Altweg-Seite erscheint nur, wenn die Anlage einen
        /// Wochengang trägt.
        /// </summary>
        private void SeitenAnpassen()
        {
            string ba = Betriebsart;
            bool monat = ba == DbWerte.WQ_PROFIL_BETRIEBSART_MONAT;

            _seiteWerte.Text = (ba == DbWerte.WQ_PROFIL_BETRIEBSART_STUNDE)
                ? MyResource.Resource.SIMQ_QUELLPROFIL_TAB_STUNDENWERTE
                : MyResource.Resource.SIMQ_QUELLPROFIL_TAB_TAGESWERTE;

            _lblWerteHinweis.Text = (ba == DbWerte.WQ_PROFIL_BETRIEBSART_STUNDE)
                ? MyResource.Resource.SIMQ_QUELLPROFIL_HINWEIS_STUNDE
                : MyResource.Resource.SIMQ_QUELLPROFIL_HINWEIS_TAG;

            _tabs.TabPages.Clear();
            if (monat)
            {
                _tabs.TabPages.Add(_seiteMonat);
                if (_wochengangVorhanden) _tabs.TabPages.Add(_seiteWoche);
            }
            else _tabs.TabPages.Add(_seiteWerte);

            _tabs.TabPages.Add(_seiteGrafik);

            WerteInfoAktualisieren();
        }

        // ------------------------------------------------------------------
        // Werteraster
        // ------------------------------------------------------------------

        private void GridFuellen()
        {
            if (_gridTabelle == null) return;

            _gridTabelle.BeginLoadData();
            _gridTabelle.Rows.Clear();
            if (_werte != null)
                for (int i = 0; i < _werte.Length; i++) _gridTabelle.Rows.Add(i + 1, _werte[i]);
            _gridTabelle.EndLoadData();

            WerteInfoAktualisieren();
        }

        /// <summary>Liest das Raster zurück in <see cref="_werte"/>.</summary>
        private void GridUebernehmen()
        {
            if (_gridTabelle == null || _werte == null) return;

            for (int i = 0; i < _gridTabelle.Rows.Count && i < _werte.Length; i++)
            {
                object v = _gridTabelle.Rows[i]["Wert"];
                if (v == null || v == DBNull.Value) continue;
                try { _werte[i] = Convert.ToDouble(v); }
                catch { /* die Zelle bleibt beim alten Wert */ }
            }
        }

        private void WerteInfoAktualisieren()
        {
            if (_lblWerteInfo == null) return;

            GridUebernehmen();

            if (_werte == null || _werte.Length == 0)
            {
                _lblWerteInfo.Text = "";
                return;
            }

            double min = _werte[0], max = _werte[0], summe = 0;
            for (int i = 0; i < _werte.Length; i++)
            {
                if (_werte[i] < min) min = _werte[i];
                if (_werte[i] > max) max = _werte[i];
                summe += _werte[i];
            }

            _lblWerteInfo.Text = string.Format(MyResource.Resource.SIMQ_QUELLPROFIL_INFO_WERTE,
                                               _werte.Length, min.ToString("F1"),
                                               max.ToString("F1"),
                                               (summe / _werte.Length).ToString("F1"));
        }

        private void btnCsv_Click(object sender, EventArgs e)
        {
            int soll = DbWerte.QuellprofilWerteanzahl(Betriebsart);
            if (soll <= 0) return;

            if (MessageBox.Show(
                    Zeilenumbruch.Normalisieren(
                        string.Format(MyResource.Resource.SIMQ_QUELLPROFIL_CSV_HINWEIS, soll)),
                    MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL,
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return;

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = MyResource.Resource.SIMQ_CSV_DATEIDIALOG_TITEL;
            dlg.Filter = MyResource.Resource.SIMQ_CSV_DATEIFILTER;
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            double[] gelesen = WaermequelleClass.WerteAusCsv(dlg.FileName, soll);
            if (gelesen == null)
            {
                MessageBox.Show(
                    Zeilenumbruch.Normalisieren(
                        string.Format(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_CSV_FEHLER, soll)),
                    MyResource.Resource.SIMQ_CSV_FEHLER_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _werte = gelesen;
            GridFuellen();
            ChartAktualisieren();
        }

        private void btnAlleWerte_Click(object sender, EventArgs e)
        {
            int soll = DbWerte.QuellprofilWerteanzahl(Betriebsart);
            if (soll <= 0) return;

            string eingabe = Eingabefrage.Fragen(
                this,
                MyResource.Resource.SIMQ_QUELLPROFIL_BTN_ALLE_WERTE,
                MyResource.Resource.SIMQ_QUELLPROFIL_ALLE_WERTE_TEXT,
                Vorgabe(VORGABE_MONATSWERT));

            float w;
            if (eingabe == null || !WaermequelleClass.ZahlParsen(eingabe, out w)) return;

            _werte = new double[soll];
            for (int i = 0; i < soll; i++) _werte[i] = w;
            GridFuellen();
            ChartAktualisieren();
        }

        // ------------------------------------------------------------------
        // Ereignisse
        // ------------------------------------------------------------------

        private void lbTag_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lbTag.SelectedIndex < 0) return;

            _aktuellerTag = _lbTag.SelectedIndex;
            TagAnzeigen(_aktuellerTag);
        }

        private void TagAnzeigen(int tag)
        {
            for (int h = 0; h < 24; h++)
                _tbStunde[h].Text = _woche[tag, h].ToString("F1");
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            string betriebsart = Betriebsart;
            int soll = DbWerte.QuellprofilWerteanzahl(betriebsart);
            double[] werte;

            if (betriebsart == DbWerte.WQ_PROFIL_BETRIEBSART_MONAT)
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

                werte = new double[12];
                Array.Copy(_monat, werte, 12);
            }
            else
            {
                GridUebernehmen();
                if (_werte == null || _werte.Length != soll)
                {
                    MessageBox.Show(
                        Zeilenumbruch.Normalisieren(
                            string.Format(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_WERTE_FEHLEN, soll)),
                        MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.DialogResult = DialogResult.None;
                    return;
                }
                werte = _werte;
            }

            QuellprofilCtrl.Kopf kopf = new QuellprofilCtrl.Kopf
            {
                ID = (_cbProfil.SelectedItem != null)
                    ? (int)((SchluesselEintrag)_cbProfil.SelectedItem).Wert : PROFIL_NEU,
                ID_Projekt = ID_Projekt,
                Bezeichner = _tbBezeichner.Text.Trim(),
                Betriebsart = betriebsart,
                Einheit = QuellprofilCtrl.EINHEIT_GRAD_CELSIUS,
                Beschreibung = _tbBeschreibung.Text.Trim()
            };

            if (kopf.Bezeichner.Length == 0)
            {
                MessageBox.Show(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_BEZEICHNER,
                    MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            int id = QuellprofilCtrl.Speichern(kopf, werte);
            if (id <= 0)
            {
                MessageBox.Show(MyResource.Resource.SIMQ_QUELLPROFIL_MSG_SPEICHERN,
                    MyResource.Resource.SIMQ_QUELLE_QUELLPROFIL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            ID_Quellprofil = id;
        }

        private void ChartAktualisieren()
        {
            if (_chart == null) return;

            string betriebsart = Betriebsart;
            double[] werte;

            if (betriebsart == DbWerte.WQ_PROFIL_BETRIEBSART_MONAT)
            {
                // Monatswerte aus den Feldern lesen (ohne Meldung - Grafik ist nur Vorschau)
                for (int m = 0; m < 12; m++)
                {
                    float w;
                    if (WaermequelleClass.ZahlParsen(_tbMonat[m].Text, out w)) _monat[m] = w;
                }

                // ALTWEG-VORSCHAU: Trägt die Anlage noch einen Wochengang und ist noch
                // kein Profil gespeichert, zeigt die Grafik das, was die Engine heute
                // rechnet - Monatswert plus Wochengang.
                if (_wochengangVorhanden)
                {
                    float[] altweg = WaermequelleClass.ProfilAusMonatsUndWochenwerten(
                        Monatswerte, Wochenwerte);
                    if (altweg != null) { ChartZeichnen(altweg); return; }
                }

                werte = new double[12];
                Array.Copy(_monat, werte, 12);
            }
            else
            {
                GridUebernehmen();
                werte = _werte;
            }

            ChartZeichnen(QuellprofilCtrl.Jahresprofil(betriebsart, werte));
        }

        private void ChartZeichnen(float[] profil)
        {
            _chart.Series[0].Points.Clear();
            if (profil == null) return;

            // Jede Stunde zeichnen, X-Achse in Monaten
            for (int i = 0; i < profil.Length; i++)
            {
                double x = (double)i * 12.0 / 8760.0;
                _chart.Series[0].Points.AddXY(x, profil[i]);
            }
        }
    }
}
