using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Pufferspeicher-Verwaltung auf PROJEKTEBENE (Konzept 4.3).
    ///
    /// Ein Speicher, der als Wärmequelle oder -senke dienen soll, muss zuvor als
    /// Projekt-Pufferspeicher angelegt sein (Konzept 3.3). Dieser Dialog ist der
    /// ausdrückliche Weg dorthin: Katalogübernahme aus <c>Tab_Pufferspeicher_STAMM</c>
    /// oder freie Eingabe, Pflichtfeld Verwendung, Betriebsparameter, Schwellen und
    /// Entladepriorität — dazu die beiden Kontrollanzeigen „Ladereihenfolge dieses
    /// Speichers" und „Wird als n. von m … entladen".
    ///
    /// NEUBAU, kein Feldzusatz an <see cref="Form_PufferSp_Bearbeiten"/>: jene Maske
    /// arbeitet ausschließlich gegen die STAMM-Tabelle und liest positionsbasiert
    /// <c>row[2]…row[6]</c> (Konzept 4.3, letzter Absatz). Hier wird durchgehend über
    /// Spaltennamen gelesen.
    ///
    /// Aufbau programmatisch nach dem Bestandsmuster <see cref="Form_QuellePufferspeicher"/>
    /// (kein Designer, keine .resx). Texte deutsch hartkodiert bis Paket 9 (Konzept 13.6).
    ///
    /// WICHTIG: Anlegen, Ändern und Entfernen wirken SOFORT auf die Datenbank — der
    /// Dialog ist eine Verwaltung, kein Formular mit Abbruch. Deshalb schließt er nur mit
    /// „Schließen" (DialogResult.OK); ein „Abbrechen", das nichts zurücknähme, wäre eine
    /// Zusage, die der Dialog nicht halten kann.
    /// </summary>
    public class Form_PufferSp_Projekt : Form
    {
        // --- Übergabe ----------------------------------------------------------------

        /// <summary>Projekt, dessen Pufferspeicher verwaltet werden.</summary>
        public int ID_Projekt;

        /// <summary>Vorbelegung bzw. zuletzt bearbeitete Verwendung (Heizung|Brauchwasser).</summary>
        public string Verwendung = WaermesenkeClass.VERWENDUNG_HEIZUNG;

        /// <summary>ID des zuletzt angelegten oder ausgewählten Puffers; 0 = keiner.</summary>
        public int ID_Puffer;

        // --- Oberfläche ---------------------------------------------------------------

        private ListBox _lbProjekt;
        private Button _btnNeu;
        private Button _btnEntfernen;
        private Button _btnKatalog;

        private ComboBox _cbKatalog;
        private TextBox _tbBezeichner;
        private TextBox _tbVolumen;
        private TextBox _tbVerluste;
        private ComboBox _cbVerwendung;
        private TextBox _tbVorlauf;
        private TextBox _tbRuecklauf;
        private Label _lblQmax;
        private TextBox _tbSchwelleEin;
        private TextBox _tbSchwelleAus;
        private TextBox _tbSchwelleNachrang;

        private ListView _lvLaden;
        private ComboBox _cbEntladeprio;
        private Label _lblEntladeInfo;
        private Button _btnUebernehmen;
        private Label _lblStatus;

        private List<WaermesenkeClass.PufferInfo> _projektPuffer =
            new List<WaermesenkeClass.PufferInfo>();
        private DataTable _katalog;

        /// <summary>0 = Neuanlage, sonst die ID des gerade bearbeiteten Puffers.</summary>
        private int _bearbeiteteId;

        private bool _aktualisiert;

        /// <summary>Eintrag des Entladeprioritäts-Dropdowns (0 = automatisch).</summary>
        private class PrioItem
        {
            public int Wert;
            public string Text = "";
            public override string ToString() { return Text; }
        }

        public Form_PufferSp_Projekt()
        {
            BaueOberflaeche();
        }

        private void BaueOberflaeche()
        {
            this.Text = "Pufferspeicher im Projekt";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(700, 616);

            // --- Bestand --------------------------------------------------------------
            GroupBox gbListe = new GroupBox
            {
                Text = "Pufferspeicher im Projekt",
                Location = new Point(12, 8),
                Size = new Size(676, 122)
            };
            this.Controls.Add(gbListe);

            _lbProjekt = new ListBox { Location = new Point(14, 22), Size = new Size(420, 88) };
            _lbProjekt.SelectedIndexChanged += lbProjekt_SelectedIndexChanged;
            gbListe.Controls.Add(_lbProjekt);

            _btnNeu = new Button { Text = "Neuer Pufferspeicher", Location = new Point(446, 22), Size = new Size(214, 26) };
            _btnNeu.Click += btnNeu_Click;
            gbListe.Controls.Add(_btnNeu);

            _btnEntfernen = new Button { Text = "Entfernen", Location = new Point(446, 54), Size = new Size(214, 26) };
            _btnEntfernen.Click += btnEntfernen_Click;
            gbListe.Controls.Add(_btnEntfernen);

            _btnKatalog = new Button { Text = "Katalog ansehen…", Location = new Point(446, 86), Size = new Size(214, 26) };
            _btnKatalog.Click += btnKatalog_Click;
            gbListe.Controls.Add(_btnKatalog);

            // --- Eigenschaften --------------------------------------------------------
            GroupBox gbDaten = new GroupBox
            {
                Text = "Eigenschaften",
                Location = new Point(12, 136),
                Size = new Size(676, 200)
            };
            this.Controls.Add(gbDaten);

            gbDaten.Controls.Add(Beschriftung("Aus Katalog:", 16, 26));
            _cbKatalog = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(180, 22),
                Width = 300
            };
            _cbKatalog.SelectedIndexChanged += cbKatalog_SelectedIndexChanged;
            gbDaten.Controls.Add(_cbKatalog);

            gbDaten.Controls.Add(Beschriftung("Bezeichner:", 16, 58));
            _tbBezeichner = new TextBox { Location = new Point(180, 55), Width = 300 };
            gbDaten.Controls.Add(_tbBezeichner);

            gbDaten.Controls.Add(Beschriftung("Verwendung:", 16, 90));
            _cbVerwendung = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(180, 86),
                Width = 180
            };
            _cbVerwendung.Items.AddRange(new object[]
            {
                WaermesenkeClass.VERWENDUNG_HEIZUNG, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER
            });
            _cbVerwendung.SelectedIndexChanged += Daten_Geaendert;
            gbDaten.Controls.Add(_cbVerwendung);

            gbDaten.Controls.Add(Beschriftung("Gesamtvolumen [l]:", 380, 58));
            _tbVolumen = new TextBox { Location = new Point(540, 55), Width = 110 };
            _tbVolumen.TextChanged += Kapazitaet_Geaendert;
            gbDaten.Controls.Add(_tbVolumen);

            gbDaten.Controls.Add(Beschriftung("Bereitschaftsverl. [kWh/24h]:", 380, 90));
            _tbVerluste = new TextBox { Location = new Point(540, 87), Width = 110 };
            gbDaten.Controls.Add(_tbVerluste);

            gbDaten.Controls.Add(Beschriftung("Vorlauf [°C]:", 16, 124));
            _tbVorlauf = new TextBox { Location = new Point(180, 121), Width = 60 };
            _tbVorlauf.TextChanged += Kapazitaet_Geaendert;
            gbDaten.Controls.Add(_tbVorlauf);

            gbDaten.Controls.Add(Beschriftung("Rücklauf [°C]:", 260, 124));
            _tbRuecklauf = new TextBox { Location = new Point(360, 121), Width = 60 };
            _tbRuecklauf.TextChanged += Kapazitaet_Geaendert;
            gbDaten.Controls.Add(_tbRuecklauf);

            _lblQmax = new Label
            {
                AutoSize = false,
                Location = new Point(436, 124),
                Size = new Size(220, 18),
                Text = ""
            };
            gbDaten.Controls.Add(_lblQmax);

            gbDaten.Controls.Add(Beschriftung("Einschaltschwelle [%]:", 16, 160));
            _tbSchwelleEin = new TextBox { Location = new Point(180, 157), Width = 60 };
            gbDaten.Controls.Add(_tbSchwelleEin);

            gbDaten.Controls.Add(Beschriftung("Abschaltschwelle [%]:", 260, 160));
            _tbSchwelleAus = new TextBox { Location = new Point(400, 157), Width = 60 };
            gbDaten.Controls.Add(_tbSchwelleAus);

            gbDaten.Controls.Add(Beschriftung("… nachrangig [%]:", 480, 160));
            _tbSchwelleNachrang = new TextBox { Location = new Point(600, 157), Width = 56 };
            gbDaten.Controls.Add(_tbSchwelleNachrang);

            // --- Ladereihenfolge ------------------------------------------------------
            GroupBox gbLaden = new GroupBox
            {
                Text = "Ladereihenfolge dieses Speichers (aus den Erzeugerzuordnungen)",
                Location = new Point(12, 342),
                Size = new Size(676, 152)
            };
            this.Controls.Add(gbLaden);

            _lvLaden = new ListView
            {
                Location = new Point(14, 22),
                Size = new Size(646, 118),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            _lvLaden.Columns.Add("#", 30, HorizontalAlignment.Left);
            _lvLaden.Columns.Add("Anlage", 220, HorizontalAlignment.Left);
            _lvLaden.Columns.Add("Erzeuger", 120, HorizontalAlignment.Left);
            _lvLaden.Columns.Add("Senke", 90, HorizontalAlignment.Left);
            _lvLaden.Columns.Add("Ladeprio", 80, HorizontalAlignment.Left);
            _lvLaden.Columns.Add("lädt bis", 90, HorizontalAlignment.Left);
            gbLaden.Controls.Add(_lvLaden);

            // --- Entladepriorität -----------------------------------------------------
            Label lblEntlade = new Label
            {
                Text = "Entladepriorität:",
                AutoSize = true,
                Location = new Point(16, 506)
            };
            this.Controls.Add(lblEntlade);

            _cbEntladeprio = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(180, 502),
                Width = 210
            };
            this.Controls.Add(_cbEntladeprio);

            _lblEntladeInfo = new Label
            {
                AutoSize = false,
                Location = new Point(400, 506),
                Size = new Size(288, 32),
                Text = ""
            };
            this.Controls.Add(_lblEntladeInfo);

            _lblStatus = new Label
            {
                AutoSize = false,
                Location = new Point(14, 546),
                Size = new Size(430, 32),
                Text = ""
            };
            this.Controls.Add(_lblStatus);

            _btnUebernehmen = new Button
            {
                Text = "Übernehmen",
                Location = new Point(this.ClientSize.Width - 300, 578),
                Width = 130,
                Height = 28
            };
            _btnUebernehmen.Click += btnUebernehmen_Click;
            this.Controls.Add(_btnUebernehmen);

            Button btnSchliessen = new Button
            {
                Text = "Schließen",
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 150, 578),
                Width = 130,
                Height = 28
            };
            this.Controls.Add(btnSchliessen);
            this.AcceptButton = _btnUebernehmen;
            this.CancelButton = btnSchliessen;
        }

        private static Label Beschriftung(string text, int x, int y)
        {
            return new Label { Text = text, AutoSize = true, Location = new Point(x, y) };
        }

        // --- Befüllen -----------------------------------------------------------------

        /// <summary>Lädt Katalog und Projektbestand; danach ist der Dialog bereit.</summary>
        public void SetControls()
        {
            _aktualisiert = true;
            try
            {
                KatalogLaden();
                EntladeprioListeFuellen();
                ProjektlisteLaden();
            }
            finally
            {
                _aktualisiert = false;
            }

            // Der Absprung aus dem Senkendialog (Konzept 4.2, "Pufferspeicher
            // anlegen...") gibt die gesuchte VERWENDUNG mit. Der Dialog stellt sich
            // darauf ein:
            //   - passender Speicher im Bestand -> der erste davon ist ausgewählt
            //   - keiner                        -> direkt in die Neuanlage, mit der
            //                                      Verwendung schon vorbelegt
            // Vorher sprang der Dialog immer auf den ersten Speicher der Gesamtliste:
            // Wer aus einer Brauchwasser-Senke kam und noch keinen Brauchwasserspeicher
            // hatte, landete im Heizungsspeicher und musste erst "Neuer Pufferspeicher"
            // drücken - genau der Schritt, den der Absprung ersparen soll.
            // Leere Vorgabe = kein Wunsch (Einstieg über die Fußzeile der Übersicht):
            // dann wie bisher der erste Speicher des Bestands.
            int auswahl = string.IsNullOrEmpty(Verwendung)
                ? (_projektPuffer.Count > 0 ? 0 : -1)
                : ErsterMitVerwendung(Verwendung);

            if (auswahl >= 0) _lbProjekt.SelectedIndex = auswahl;
            else NeuVorbereiten();
        }

        /// <summary>
        /// Index des ersten Projekt-Puffers mit der gesuchten Verwendung; -1, wenn es
        /// keinen gibt. Leere <c>Verwendung</c> am Puffer zählt als „Heizung"
        /// (<see cref="WaermesenkeClass.WirksameVerwendung"/>) - dieselbe Regel wie in
        /// den Auswahllisten des Senkendialogs.
        /// </summary>
        private int ErsterMitVerwendung(string verwendung)
        {
            string gesucht = string.IsNullOrEmpty(verwendung)
                ? WaermesenkeClass.VERWENDUNG_HEIZUNG : verwendung;

            for (int i = 0; i < _projektPuffer.Count; i++)
            {
                if (string.Equals(WaermesenkeClass.WirksameVerwendung(_projektPuffer[i]),
                                  gesucht, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        private void KatalogLaden()
        {
            _katalog = StilleDb.Tabelle(
                "SELECT ID, Bezeichner, Hersteller, Speichertyp, Gesamtvolumen, Bereitschaftsverluste, " +
                "Investitionskosten FROM [" + PufferSpStammCtrl.TABLE + "] ORDER BY Bezeichner");

            _cbKatalog.Items.Clear();
            _cbKatalog.Items.Add("(freie Eingabe)");
            if (_katalog != null)
                foreach (DataRow r in _katalog.Rows)
                    _cbKatalog.Items.Add(StilleDb.Text(StilleDb.Feld(r, "Bezeichner")));
            _cbKatalog.SelectedIndex = 0;
        }

        private void ProjektlisteLaden()
        {
            _projektPuffer = WaermesenkeClass.ProjektPufferListe(ID_Projekt, null);

            _lbProjekt.Items.Clear();
            foreach (WaermesenkeClass.PufferInfo p in _projektPuffer)
            {
                _lbProjekt.Items.Add(p.Bezeichner + "  -  " + WaermesenkeClass.WirksameVerwendung(p) +
                                     ", " + p.Gesamtvolumen + " l" +
                                     (p.VerwendungFehlt ? "  (Verwendung nicht gepflegt)" : ""));
            }
        }

        private void EntladeprioListeFuellen()
        {
            _cbEntladeprio.Items.Clear();
            _cbEntladeprio.Items.Add(new PrioItem { Wert = 0, Text = "automatisch" });
            for (int p = Ladeordnung.PRIO_MIN; p <= Ladeordnung.PRIO_MAX; p++)
                _cbEntladeprio.Items.Add(new PrioItem { Wert = p, Text = p.ToString() });
            _cbEntladeprio.SelectedIndex = 0;
        }

        /// <summary>Setzt die Maske auf „neuer Speicher".</summary>
        private void NeuVorbereiten()
        {
            _aktualisiert = true;
            try
            {
                _bearbeiteteId = 0;
                _lbProjekt.ClearSelected();

                _cbKatalog.SelectedIndex = 0;
                _tbBezeichner.Text = "";
                _tbVolumen.Text = "";
                _tbVerluste.Text = "0";

                _cbVerwendung.SelectedItem =
                    string.Equals(Verwendung, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                  StringComparison.OrdinalIgnoreCase)
                        ? WaermesenkeClass.VERWENDUNG_BRAUCHWASSER
                        : WaermesenkeClass.VERWENDUNG_HEIZUNG;

                // Vorbelegung aus den SYSTEMVORGABEN des Projekts (Konzept 4.3, Punkt 3):
                // kleinster Vorlauf und größter Rücklauf über die Erzeuger. Fehlen sie,
                // bleiben die Felder leer - eine erfundene Vorbelegung wäre bei einem
                // Niedertemperatursystem falsch (ProjektPuffer.PufferParameter).
                int? vorlauf = PufferSpCtrl.SystemVorlauf(ID_Projekt);
                int? ruecklauf = PufferSpCtrl.SystemRuecklauf(ID_Projekt);
                _tbVorlauf.Text = vorlauf.HasValue ? vorlauf.Value.ToString(CultureInfo.InvariantCulture) : "";
                _tbRuecklauf.Text = ruecklauf.HasValue ? ruecklauf.Value.ToString(CultureInfo.InvariantCulture) : "";

                _tbSchwelleEin.Text = ProjektPuffer.SCHWELLE_EIN_DEFAULT.ToString("0.#");
                _tbSchwelleAus.Text = ProjektPuffer.SCHWELLE_AUS_DEFAULT.ToString("0.#");
                _tbSchwelleNachrang.Text = ProjektPuffer.SCHWELLE_AUS_DEFAULT.ToString("0.#");
                _cbEntladeprio.SelectedIndex = 0;

                _btnUebernehmen.Text = "Anlegen";
                _btnEntfernen.Enabled = false;
                _cbKatalog.Enabled = true;
            }
            finally
            {
                _aktualisiert = false;
            }

            AnzeigenAktualisieren();
        }

        /// <summary>Lädt einen vorhandenen Projekt-Puffer in die Maske.</summary>
        private void PufferAnzeigen(WaermesenkeClass.PufferInfo p)
        {
            if (p == null) return;

            _aktualisiert = true;
            try
            {
                _bearbeiteteId = p.ID;
                ID_Puffer = p.ID;

                _cbKatalog.SelectedIndex = 0;
                _cbKatalog.Enabled = false;   // ein vorhandener Speicher wird nicht neu übernommen

                _tbBezeichner.Text = p.Bezeichner;
                _tbVolumen.Text = p.Gesamtvolumen.ToString(CultureInfo.InvariantCulture);
                _tbVerluste.Text = p.Bereitschaftsverluste.ToString("0.###");
                _cbVerwendung.SelectedItem = WaermesenkeClass.WirksameVerwendung(p);

                _tbVorlauf.Text = p.Vorlauf > 0 ? p.Vorlauf.ToString(CultureInfo.InvariantCulture) : "";
                _tbRuecklauf.Text = p.Ruecklauf > 0 ? p.Ruecklauf.ToString(CultureInfo.InvariantCulture) : "";

                _tbSchwelleEin.Text = p.SchwelleEin.ToString("0.#");
                _tbSchwelleAus.Text = p.SchwelleAus.ToString("0.#");
                _tbSchwelleNachrang.Text = p.SchwelleAusNachrang.ToString("0.#");

                PrioWaehlen(_cbEntladeprio, p.Entladeprio);

                _btnUebernehmen.Text = "Übernehmen";
                _btnEntfernen.Enabled = true;
            }
            finally
            {
                _aktualisiert = false;
            }

            AnzeigenAktualisieren();
        }

        private static void PrioWaehlen(ComboBox cb, int wert)
        {
            foreach (object o in cb.Items)
            {
                PrioItem it = o as PrioItem;
                if (it != null && it.Wert == wert) { cb.SelectedItem = o; return; }
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private static int GewaehltePrio(ComboBox cb)
        {
            PrioItem it = cb.SelectedItem as PrioItem;
            return it != null ? it.Wert : 0;
        }

        // --- Anzeigen (Q_max, Ladereihenfolge, Entladung) -----------------------------

        /// <summary>
        /// Tippen in Volumen/Vorlauf/Rücklauf rechnet nur Q_max neu. Die beiden
        /// Reihenfolge-Anzeigen fragen die Datenbank ab und dürfen nicht an jedem
        /// Tastendruck hängen; sie werden bei Auswahlwechseln aufgefrischt.
        /// </summary>
        private void Kapazitaet_Geaendert(object sender, EventArgs e)
        {
            if (_aktualisiert) return;
            QmaxAnzeigen();
        }

        private void Daten_Geaendert(object sender, EventArgs e)
        {
            if (_aktualisiert) return;
            AnzeigenAktualisieren();
        }

        private void AnzeigenAktualisieren()
        {
            QmaxAnzeigen();
            LadereihenfolgeAnzeigen();
            EntladungAnzeigen();
        }

        /// <summary>Nutzbare Kapazität aus Volumen und Spreizung (dieselbe Formel wie die Engine).</summary>
        private void QmaxAnzeigen()
        {
            int volumen, vorlauf, ruecklauf;
            if (!int.TryParse(_tbVolumen.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out volumen) ||
                !int.TryParse(_tbVorlauf.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out vorlauf) ||
                !int.TryParse(_tbRuecklauf.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out ruecklauf) ||
                volumen <= 0 || vorlauf <= ruecklauf)
            {
                _lblQmax.Text = "";
                return;
            }

            double qmax = volumen * 1.16 * (vorlauf - ruecklauf) / 1000.0;
            _lblQmax.Text = "→  Q_max " + qmax.ToString("0.0") + " kWh";
        }

        private void LadereihenfolgeAnzeigen()
        {
            _lvLaden.Items.Clear();
            if (_bearbeiteteId <= 0)
            {
                _lvLaden.Items.Add(new ListViewItem(new[]
                    { "", "(der Speicher ist noch nicht angelegt)", "", "", "", "" }));
                return;
            }

            List<Ladeordnung.LadeEintrag> liste = Ladeordnung.Ladereihenfolge(ID_Projekt, _bearbeiteteId);
            if (liste.Count == 0)
            {
                _lvLaden.Items.Add(new ListViewItem(new[]
                    { "", "(keine Anlage lädt diesen Speicher)", "", "", "", "" }));
                return;
            }

            for (int i = 0; i < liste.Count; i++)
            {
                Ladeordnung.LadeEintrag e = liste[i];
                _lvLaden.Items.Add(new ListViewItem(new[]
                {
                    (i + 1) + ".",
                    e.Bezeichner,
                    e.Erzeuger,
                    e.Zweitsenke ? "Zweitsenke" : "Hauptsenke",
                    e.Ladeprio + (e.PrioManuell ? " (manuell)" : ""),
                    e.Obergrenze.ToString("0.#") + " %" + (e.ObergrenzeEigen ? " (eigene)" : "")
                }));
            }
        }

        private void EntladungAnzeigen()
        {
            if (_bearbeiteteId <= 0)
            {
                _lblEntladeInfo.Text = "";
                AutomatikTextSetzen(Ladeordnung.PRIO_SONSTIGE);
                return;
            }

            int automatik = Ladeordnung.EntladeprioAutomatik(ID_Projekt, _bearbeiteteId);
            AutomatikTextSetzen(automatik);

            string verwendung = _cbVerwendung.SelectedItem != null
                ? _cbVerwendung.SelectedItem.ToString()
                : WaermesenkeClass.VERWENDUNG_HEIZUNG;

            List<Ladeordnung.EntladeEintrag> reihe = Ladeordnung.Entladereihenfolge(ID_Projekt, verwendung);
            int pos = Ladeordnung.Position(reihe, _bearbeiteteId);

            _lblEntladeInfo.Text = pos > 0
                ? "Wird als " + pos + ". von " + reihe.Count + " " +
                  KanalSpeicherWort(verwendung, reihe.Count) + " entladen."
                : "";
        }

        /// <summary>
        /// Der Kanalname in der grammatisch richtigen Form: „Heizungsspeicher" bzw.
        /// „Brauchwasserspeicher", im Plural mit „n".
        ///
        /// Vorher wurde die Verwendung kleingeschrieben und ein „s-Speicher(n)"
        /// angehängt - das ergab „von 2 heizungs-Speicher(n) entladen".
        /// </summary>
        private static string KanalSpeicherWort(string verwendung, int anzahl)
        {
            string basis = string.Equals(verwendung, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                         StringComparison.OrdinalIgnoreCase)
                ? "Brauchwasserspeicher"
                : "Heizungsspeicher";

            return anzahl == 1 ? basis : basis + "n";
        }

        /// <summary>Beschriftet den Automatik-Eintrag mit dem errechneten Wert.</summary>
        private void AutomatikTextSetzen(int automatik)
        {
            if (_cbEntladeprio.Items.Count == 0) return;

            PrioItem it = _cbEntladeprio.Items[0] as PrioItem;
            if (it == null) return;

            it.Text = "automatisch (" + automatik + ")";

            // ComboBox neu zeichnen lassen, ohne die Auswahl zu verlieren
            int auswahl = _cbEntladeprio.SelectedIndex;
            _aktualisiert = true;
            try
            {
                _cbEntladeprio.Items[0] = it;
                _cbEntladeprio.SelectedIndex = auswahl >= 0 ? auswahl : 0;
            }
            finally
            {
                _aktualisiert = false;
            }
        }

        // --- Ereignisse ---------------------------------------------------------------

        private void lbProjekt_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_aktualisiert) return;
            int i = _lbProjekt.SelectedIndex;
            if (i < 0 || i >= _projektPuffer.Count) return;
            PufferAnzeigen(_projektPuffer[i]);
        }

        private void btnNeu_Click(object sender, EventArgs e)
        {
            NeuVorbereiten();
            _tbBezeichner.Focus();
        }

        private void cbKatalog_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_aktualisiert) return;
            if (_cbKatalog.SelectedIndex <= 0) return;   // (freie Eingabe)
            if (_katalog == null) return;

            int zeile = _cbKatalog.SelectedIndex - 1;
            if (zeile < 0 || zeile >= _katalog.Rows.Count) return;

            DataRow r = _katalog.Rows[zeile];
            _aktualisiert = true;
            try
            {
                _tbBezeichner.Text = StilleDb.Text(StilleDb.Feld(r, "Bezeichner"));
                _tbVolumen.Text = StilleDb.Zahl(StilleDb.Feld(r, "Gesamtvolumen"))
                                          .ToString(CultureInfo.InvariantCulture);
                _tbVerluste.Text = StilleDb.Kommazahl(StilleDb.Feld(r, "Bereitschaftsverluste"))
                                           .ToString("0.###");
            }
            finally
            {
                _aktualisiert = false;
            }

            AnzeigenAktualisieren();
        }

        private void btnKatalog_Click(object sender, EventArgs e)
        {
            // Katalogbrowser wie im Bestand (Konzept 4.3): nur Ansicht.
            Form_PufferSp_Admin frm = new Form_PufferSp_Admin();
            frm.m_bReadOnly = true;
            frm.ShowDialog(this);

            _aktualisiert = true;
            try { KatalogLaden(); }
            finally { _aktualisiert = false; }
        }

        private void btnUebernehmen_Click(object sender, EventArgs e)
        {
            string bezeichner, verwendung, fehler;
            int volumen, entladeprio;
            double verluste, schwelleEin, schwelleAus, schwelleNachrang;
            int? vorlauf, ruecklauf;

            if (!EingabenLesen(out bezeichner, out verwendung, out volumen, out verluste,
                               out vorlauf, out ruecklauf, out schwelleEin, out schwelleAus,
                               out schwelleNachrang, out entladeprio, out fehler))
            {
                MessageBox.Show(fehler, "Pufferspeicher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string hersteller = "", speichertyp = ProjektPuffer.SPEICHERTYP_PUFFER;
            double investition = 0;
            KatalogfelderLesen(ref hersteller, ref speichertyp, ref investition);

            if (_bearbeiteteId <= 0)
            {
                // Konzept 5.2 / E7: die EXPLIZITE Anlage legt immer eine neue Zeile an -
                // Mehrfachanlage desselben Katalogtyps ist ausdrücklich zulässig.
                int neueId = PufferSpCtrl.ProjektPufferAnlegen(
                    ID_Projekt, bezeichner, hersteller, speichertyp, volumen, verluste,
                    investition, verwendung, vorlauf, ruecklauf,
                    schwelleEin, schwelleAus, schwelleNachrang, entladeprio);

                if (neueId <= 0)
                {
                    MessageBox.Show("Der Pufferspeicher konnte nicht angelegt werden.",
                                    "Pufferspeicher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ID_Puffer = neueId;
                _bearbeiteteId = neueId;
                Verwendung = verwendung;
                Status("Pufferspeicher angelegt.");
            }
            else
            {
                // Konzept 5.2, Konsistenzregel: Ein Verwendungswechsel an einem bereits
                // zugeordneten Speicher darf nicht still durchgehen.
                if (!VerwendungswechselBestaetigt(verwendung)) return;

                if (!PufferSpCtrl.ProjektPufferAendern(
                        _bearbeiteteId, ID_Projekt, bezeichner, hersteller, speichertyp, volumen,
                        verluste, investition, verwendung, vorlauf, ruecklauf,
                        schwelleEin, schwelleAus, schwelleNachrang, entladeprio))
                {
                    MessageBox.Show("Der Pufferspeicher konnte nicht geändert werden.",
                                    "Pufferspeicher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ID_Puffer = _bearbeiteteId;
                Verwendung = verwendung;
                Status("Änderungen übernommen.");
            }

            BestandNeuLaden(ID_Puffer);
        }

        /// <summary>
        /// Rückfrage vor dem Wechsel der Verwendung eines bereits REFERENZIERTEN
        /// Speichers; <c>true</c> = weitermachen.
        ///
        /// Die Verwendung entscheidet, welche Senke den Speicher überhaupt wählen darf
        /// (<c>WaermesenkeClass.PufferPasst</c>). Wird sie an einem Speicher umgestellt,
        /// den eine Anlage schon als Haupt- oder Zweitsenke führt, passt diese Zuordnung
        /// hinterher nicht mehr: Der Senkendialog blockiert beim nächsten Öffnen mit
        /// „falsche Verwendung", und bis dahin steht in der Anlage eine Senke, die die
        /// Prüfung nach 4.6 nicht mehr bestehen würde. Deshalb die Rückfrage MIT der
        /// Liste der betroffenen Anlagen.
        ///
        /// Die Rückfrage sitzt hier im Dialog und nicht in
        /// <c>PufferSpCtrl.ProjektPufferAendern</c>: die Ctrl-Bausteine aus Paket 2 sind
        /// durchgehend dialogfrei (Konzept 13.4), damit die headless laufenden Proben und
        /// der Referenzlauf sie benutzen können. Eine MessageBox dort brächte den
        /// nächsten Lauf zum Stehen.
        /// </summary>
        private bool VerwendungswechselBestaetigt(string verwendungNeu)
        {
            if (_bearbeiteteId <= 0) return true;

            WaermesenkeClass.PufferInfo alt = WaermesenkeClass.PufferLesen(_bearbeiteteId);
            if (alt == null) return true;

            string verwendungAlt = WaermesenkeClass.WirksameVerwendung(alt);
            if (string.Equals(verwendungAlt, verwendungNeu, StringComparison.OrdinalIgnoreCase))
                return true;

            List<string> referenzen = PufferSpCtrl.ReferenzenAufPuffer(_bearbeiteteId);
            if (referenzen.Count == 0) return true;

            return MessageBox.Show(
                "Die Verwendung des Pufferspeichers „" + alt.Bezeichner + "\" wird von „" +
                verwendungAlt + "\" auf „" + verwendungNeu + "\" umgestellt." +
                Environment.NewLine + Environment.NewLine +
                "Der Speicher ist zugeordnet:" + Environment.NewLine +
                "  • " + string.Join(Environment.NewLine + "  • ", referenzen) +
                Environment.NewLine + Environment.NewLine +
                "Diese Zuordnungen passen danach nicht mehr zur Verwendung und müssen im " +
                "Wärmesenken-Dialog neu gesetzt werden." + Environment.NewLine +
                "Verwendung trotzdem ändern?",
                "Verwendung ändern", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning) == DialogResult.Yes;
        }

        private void btnEntfernen_Click(object sender, EventArgs e)
        {
            if (_bearbeiteteId <= 0) return;

            // Konzept 5.2: blockieren, solange eine Anlage den Puffer referenziert
            List<string> referenzen = PufferSpCtrl.ReferenzenAufPuffer(_bearbeiteteId);
            if (referenzen.Count > 0)
            {
                MessageBox.Show(
                    "Der Pufferspeicher „" + _tbBezeichner.Text + "\" kann nicht entfernt werden - " +
                    "er ist noch zugeordnet:" + Environment.NewLine + Environment.NewLine +
                    "  • " + string.Join(Environment.NewLine + "  • ", referenzen) +
                    Environment.NewLine + Environment.NewLine +
                    "Bitte zuerst die Wärmequelle bzw. Wärmesenke dieser Anlagen ändern.",
                    "Pufferspeicher entfernen", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                    "Den Pufferspeicher „" + _tbBezeichner.Text + "\" aus dem Projekt entfernen?" +
                    Environment.NewLine +
                    "Die Anlagenzeile im Projektbaum wird mit entfernt.",
                    "Pufferspeicher entfernen", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;

            if (!PufferSpCtrl.ProjektPufferEntfernen(_bearbeiteteId, ID_Projekt))
            {
                MessageBox.Show("Der Pufferspeicher konnte nicht entfernt werden.",
                                "Pufferspeicher", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ID_Puffer == _bearbeiteteId) ID_Puffer = 0;
            Status("Pufferspeicher entfernt.");
            BestandNeuLaden(0);
        }

        private void BestandNeuLaden(int auswahlId)
        {
            _aktualisiert = true;
            try { ProjektlisteLaden(); }
            finally { _aktualisiert = false; }

            for (int i = 0; i < _projektPuffer.Count; i++)
            {
                if (_projektPuffer[i].ID == auswahlId)
                {
                    _lbProjekt.SelectedIndex = i;   // löst PufferAnzeigen aus
                    return;
                }
            }

            NeuVorbereiten();
        }

        private void Status(string text)
        {
            _lblStatus.ForeColor = Color.ForestGreen;
            _lblStatus.Text = "✔ " + text;
        }

        /// <summary>Hersteller/Speichertyp/Investition — aus dem Katalog oder aus dem Bestand.</summary>
        private void KatalogfelderLesen(ref string hersteller, ref string speichertyp,
                                        ref double investition)
        {
            if (_cbKatalog.Enabled && _cbKatalog.SelectedIndex > 0 && _katalog != null)
            {
                int zeile = _cbKatalog.SelectedIndex - 1;
                if (zeile >= 0 && zeile < _katalog.Rows.Count)
                {
                    DataRow r = _katalog.Rows[zeile];
                    hersteller = StilleDb.Text(StilleDb.Feld(r, "Hersteller"));
                    string typ = StilleDb.Text(StilleDb.Feld(r, "Speichertyp"));
                    if (typ.Length > 0) speichertyp = typ;
                    investition = StilleDb.Kommazahl(StilleDb.Feld(r, "Investitionskosten"));
                    return;
                }
            }

            if (_bearbeiteteId <= 0) return;

            DataTable dt = StilleDb.Tabelle(
                "SELECT Hersteller, Speichertyp, Investitionskosten FROM Tab_Pufferspeicher WHERE ID = ?",
                StilleDb.Par("@id", System.Data.OleDb.OleDbType.Integer, _bearbeiteteId));
            if (dt == null || dt.Rows.Count == 0) return;

            hersteller = StilleDb.Text(StilleDb.Feld(dt.Rows[0], "Hersteller"));
            string typBestand = StilleDb.Text(StilleDb.Feld(dt.Rows[0], "Speichertyp"));
            if (typBestand.Length > 0) speichertyp = typBestand;
            investition = StilleDb.Kommazahl(StilleDb.Feld(dt.Rows[0], "Investitionskosten"));
        }

        // --- Validierung ---------------------------------------------------------------

        private bool EingabenLesen(out string bezeichner, out string verwendung, out int volumen,
                                   out double verluste, out int? vorlauf, out int? ruecklauf,
                                   out double schwelleEin, out double schwelleAus,
                                   out double schwelleNachrang, out int entladeprio,
                                   out string fehler)
        {
            bezeichner = (_tbBezeichner.Text ?? "").Trim();
            verwendung = _cbVerwendung.SelectedItem != null ? _cbVerwendung.SelectedItem.ToString() : "";
            volumen = 0;
            verluste = 0;
            vorlauf = null;
            ruecklauf = null;
            schwelleEin = ProjektPuffer.SCHWELLE_EIN_DEFAULT;
            schwelleAus = ProjektPuffer.SCHWELLE_AUS_DEFAULT;
            schwelleNachrang = ProjektPuffer.SCHWELLE_AUS_DEFAULT;
            entladeprio = GewaehltePrio(_cbEntladeprio);
            fehler = null;

            if (bezeichner.Length == 0)
            {
                fehler = "Bitte einen Bezeichner eintragen oder einen Katalogeintrag wählen.";
                return false;
            }

            if (verwendung.Length == 0)
            {
                fehler = "Die Verwendung ist ein Pflichtfeld: Heizung oder Brauchwasser (Konzept 5.1).";
                return false;
            }

            if (!int.TryParse(_tbVolumen.Text.Trim(), NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out volumen) || volumen <= 0)
            {
                fehler = "Bitte ein Gesamtvolumen in Litern eintragen (ganze Zahl größer 0).";
                return false;
            }

            float f;
            if (_tbVerluste.Text.Trim().Length > 0)
            {
                if (!WaermequelleClass.ZahlParsen(_tbVerluste.Text, out f) || f < 0)
                {
                    fehler = "Die Bereitschaftsverluste müssen eine Zahl ≥ 0 sein [kWh/24h].";
                    return false;
                }
                verluste = f;
            }

            // Temperaturen: leeres PAAR ist erlaubt (dann greift der Engine-Rückfall),
            // ein vollständiges Paar läuft durch die gemeinsame Prüfung.
            string vorText = _tbVorlauf.Text.Trim();
            string rueText = _tbRuecklauf.Text.Trim();
            if (vorText.Length > 0 || rueText.Length > 0)
            {
                int v, r;
                if (!ProjektPuffer.TemperaturenPruefen(vorText, rueText, out v, out r, out fehler))
                    return false;
                vorlauf = v;
                ruecklauf = r;
            }

            if (!SchwelleLesen(_tbSchwelleEin, "Einschaltschwelle", out schwelleEin, out fehler)) return false;
            if (!SchwelleLesen(_tbSchwelleAus, "Abschaltschwelle", out schwelleAus, out fehler)) return false;
            if (!SchwelleLesen(_tbSchwelleNachrang, "Abschaltschwelle für nachrangige Erzeuger",
                               out schwelleNachrang, out fehler)) return false;

            if (schwelleEin >= schwelleAus)
            {
                fehler = "Die Einschaltschwelle muss kleiner als die Abschaltschwelle sein.";
                return false;
            }

            if (schwelleNachrang > schwelleAus)
            {
                fehler = "Die Abschaltschwelle für nachrangige Erzeuger darf die Abschaltschwelle " +
                         "nicht überschreiten - sie ist die Reservezone für den Vorrang (Konzept 3.4).";
                return false;
            }

            if (schwelleNachrang <= schwelleEin)
            {
                fehler = "Die Abschaltschwelle für nachrangige Erzeuger muss über der " +
                         "Einschaltschwelle liegen.";
                return false;
            }

            return true;
        }

        private static bool SchwelleLesen(TextBox tb, string name, out double wert, out string fehler)
        {
            wert = 0;
            fehler = null;

            float f;
            if (!WaermequelleClass.ZahlParsen(tb.Text, out f))
            {
                fehler = "Die " + name + " muss eine Zahl sein [%].";
                return false;
            }

            if (f <= 0 || f > 100)
            {
                fehler = "Die " + name + " muss zwischen 0 und 100 % liegen.";
                return false;
            }

            wert = f;
            return true;
        }
    }
}
