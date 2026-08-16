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

        /// <summary>
        /// Eintrag des Verwendungs-Dropdowns (Behebung Befund L0-2).
        ///
        /// <para>
        /// Vorher standen die DB-Werte <c>„Heizung"</c> und <c>„Brauchwasser"</c>
        /// UNMITTELBAR als ComboBox-Einträge in der Liste, und
        /// <c>SelectedItem.ToString()</c> las sie als Steuerwert zurück. Der angezeigte
        /// Text war damit nicht lokalisierbar, ohne zugleich den Persistenzwert zu
        /// verändern — genau die Verwechslung, die die Drei-Schichten-Regel verbietet.
        /// </para>
        ///
        /// Jetzt trägt der Eintrag beides getrennt: <see cref="DbWert"/> geht in die
        /// Datenbank und in jeden Vergleich, <see cref="ToString"/> liefert den
        /// übersetzten Anzeigetext.
        /// </summary>
        private class VerwendungItem
        {
            public string DbWert = "";
            public string Anzeige = "";
            public override string ToString() { return Anzeige; }
        }

        public Form_PufferSp_Projekt()
        {
            BaueOberflaeche();
        }

        private void BaueOberflaeche()
        {
            this.Text = MyResource.Resource.PSP_PROJEKT_FENSTERTITEL;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            this.ClientSize = new Size(700, 616);

            // --- Bestand --------------------------------------------------------------
            GroupBox gbListe = new GroupBox
            {
                Text = MyResource.Resource.PSP_PROJEKT_FENSTERTITEL,
                Location = new Point(12, 8),
                Size = new Size(676, 122)
            };
            this.Controls.Add(gbListe);

            _lbProjekt = new ListBox { Location = new Point(14, 22), Size = new Size(420, 88) };
            _lbProjekt.SelectedIndexChanged += lbProjekt_SelectedIndexChanged;
            gbListe.Controls.Add(_lbProjekt);

            _btnNeu = new Button { Text = MyResource.Resource.PSP_BTN_NEUER_PUFFERSPEICHER, Location = new Point(446, 22), Size = new Size(214, 26) };
            _btnNeu.Click += btnNeu_Click;
            gbListe.Controls.Add(_btnNeu);

            _btnEntfernen = new Button { Text = MyResource.Resource.PSP_BTN_ENTFERNEN, Location = new Point(446, 54), Size = new Size(214, 26) };
            _btnEntfernen.Click += btnEntfernen_Click;
            gbListe.Controls.Add(_btnEntfernen);

            _btnKatalog = new Button { Text = MyResource.Resource.PSP_BTN_KATALOG_ANSEHEN, Location = new Point(446, 86), Size = new Size(214, 26) };
            _btnKatalog.Click += btnKatalog_Click;
            gbListe.Controls.Add(_btnKatalog);

            // --- Eigenschaften --------------------------------------------------------
            GroupBox gbDaten = new GroupBox
            {
                Text = MyResource.Resource.PSP_GRUPPE_EIGENSCHAFTEN,
                Location = new Point(12, 136),
                Size = new Size(676, 200)
            };
            this.Controls.Add(gbDaten);

            gbDaten.Controls.Add(Beschriftung(MyResource.Resource.PSP_LABEL_AUS_KATALOG, 16, 26));
            _cbKatalog = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(180, 22),
                Width = 300
            };
            _cbKatalog.SelectedIndexChanged += cbKatalog_SelectedIndexChanged;
            gbDaten.Controls.Add(_cbKatalog);

            gbDaten.Controls.Add(Beschriftung(MyResource.Resource.PSP_LABEL_BEZEICHNER, 16, 58));
            _tbBezeichner = new TextBox { Location = new Point(180, 55), Width = 300 };
            gbDaten.Controls.Add(_tbBezeichner);

            gbDaten.Controls.Add(Beschriftung(MyResource.Resource.PSP_LABEL_VERWENDUNG, 16, 90));
            _cbVerwendung = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(180, 86),
                Width = 180
            };
            // Befund L0-2: DB-Wert und Anzeigetext getrennt (VerwendungItem).
            //
            // ETAPPE D5b, VORGEZOGEN (Nacharbeit I-K2-4): Der KOMBISPEICHER als dritte,
            // reguläre Option. Ohne sie zeigte die Bearbeitungsmaske für einen per
            // Datenbank angelegten Kombi-Puffer „Heizung" (kein Treffer in
            // VerwendungWaehlen -> Rückfall auf Index 0) und schrieb ihn beim nächsten
            // „Übernehmen" still auf Verwendung = 'Heizung' zurück - stiller Datenverlust
            // an genau der Konfiguration, die D5a einführt. Die Rückfrage
            // VerwendungswechselBestaetigt greift dabei nicht, weil sie nur bei bereits
            // REFERENZIERTEN Speichern anschlägt.
            _cbVerwendung.Items.AddRange(new object[]
            {
                new VerwendungItem
                {
                    DbWert = WaermesenkeClass.VERWENDUNG_HEIZUNG,
                    Anzeige = MyResource.Resource.PSP_VERWENDUNG_HEIZUNG_ANZEIGE
                },
                new VerwendungItem
                {
                    DbWert = WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                    Anzeige = MyResource.Resource.PSP_VERWENDUNG_BRAUCHWASSER_ANZEIGE
                },
                new VerwendungItem
                {
                    DbWert = WaermesenkeClass.VERWENDUNG_KOMBI,
                    Anzeige = MyResource.Resource.PSP_VERWENDUNG_KOMBI_ANZEIGE
                }
            });
            _cbVerwendung.SelectedIndexChanged += Daten_Geaendert;
            gbDaten.Controls.Add(_cbVerwendung);

            gbDaten.Controls.Add(Beschriftung(MyResource.Resource.PSP_LABEL_GESAMTVOLUMEN, 380, 58));
            _tbVolumen = new TextBox { Location = new Point(540, 55), Width = 110 };
            _tbVolumen.TextChanged += Kapazitaet_Geaendert;
            gbDaten.Controls.Add(_tbVolumen);

            gbDaten.Controls.Add(Beschriftung(MyResource.Resource.PSP_LABEL_BEREITSCHAFTSVERLUSTE, 380, 90));
            _tbVerluste = new TextBox { Location = new Point(540, 87), Width = 110 };
            gbDaten.Controls.Add(_tbVerluste);

            gbDaten.Controls.Add(Beschriftung(MyResource.Resource.PSP_LABEL_VORLAUF, 16, 124));
            _tbVorlauf = new TextBox { Location = new Point(180, 121), Width = 60 };
            _tbVorlauf.TextChanged += Kapazitaet_Geaendert;
            gbDaten.Controls.Add(_tbVorlauf);

            gbDaten.Controls.Add(Beschriftung(MyResource.Resource.PSP_LABEL_RUECKLAUF, 260, 124));
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

            gbDaten.Controls.Add(Beschriftung(MyResource.Resource.PSP_LABEL_EINSCHALTSCHWELLE, 16, 160));
            _tbSchwelleEin = new TextBox { Location = new Point(180, 157), Width = 60 };
            gbDaten.Controls.Add(_tbSchwelleEin);

            gbDaten.Controls.Add(Beschriftung(MyResource.Resource.PSP_LABEL_ABSCHALTSCHWELLE, 260, 160));
            _tbSchwelleAus = new TextBox { Location = new Point(400, 157), Width = 60 };
            gbDaten.Controls.Add(_tbSchwelleAus);

            gbDaten.Controls.Add(Beschriftung(MyResource.Resource.PSP_LABEL_SCHWELLE_NACHRANGIG, 480, 160));
            _tbSchwelleNachrang = new TextBox { Location = new Point(600, 157), Width = 56 };
            gbDaten.Controls.Add(_tbSchwelleNachrang);

            // --- Ladereihenfolge ------------------------------------------------------
            GroupBox gbLaden = new GroupBox
            {
                Text = MyResource.Resource.PSP_GRUPPE_LADEREIHENFOLGE,
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
            _lvLaden.Columns.Add(MyResource.Resource.SIM_SPALTE_ANLAGE, 220, HorizontalAlignment.Left);
            _lvLaden.Columns.Add(MyResource.Resource.SIM_ERZEUGERNAME_ALLGEMEIN, 120, HorizontalAlignment.Left);
            _lvLaden.Columns.Add(MyResource.Resource.SIM_SPALTE_SENKE, 90, HorizontalAlignment.Left);
            _lvLaden.Columns.Add(MyResource.Resource.PSP_SPALTE_LADEPRIO, 80, HorizontalAlignment.Left);
            _lvLaden.Columns.Add(MyResource.Resource.PSP_SPALTE_LAEDT_BIS, 90, HorizontalAlignment.Left);
            gbLaden.Controls.Add(_lvLaden);

            // --- Entladepriorität -----------------------------------------------------
            Label lblEntlade = new Label
            {
                Text = MyResource.Resource.PSP_LABEL_ENTLADEPRIORITAET,
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
                Text = MyResource.Resource.PSP_BTN_UEBERNEHMEN,
                Location = new Point(this.ClientSize.Width - 300, 578),
                Width = 130,
                Height = 28
            };
            _btnUebernehmen.Click += btnUebernehmen_Click;
            this.Controls.Add(_btnUebernehmen);

            Button btnSchliessen = new Button
            {
                Text = MyResource.Resource.PSP_BTN_SCHLIESSEN,
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

            // ETAPPE D3 (Konzept_KonfigUI_Hydraulik 3a): Das ✎ einer Speicherkarte meint
            // GENAU DIESEN Speicher und gibt seine ID mit. Ohne die Vorwahl landete der
            // Anwender im ersten Speicher der Liste - bei zwei Heizungsspeichern also
            // regelmäßig im falschen. Die ID hat Vorrang vor der Verwendungsregel
            // darüber; ist sie unbekannt (0, oder der Speicher gehört nicht zum
            // Projekt), bleibt es bei der bisherigen Wahl.
            int nachId = IndexVonPuffer(ID_Puffer);
            if (nachId >= 0) auswahl = nachId;

            if (auswahl >= 0) _lbProjekt.SelectedIndex = auswahl;
            else NeuVorbereiten();
        }

        /// <summary>Listenplatz eines Projekt-Puffers; -1, wenn er nicht dabei ist.</summary>
        private int IndexVonPuffer(int idPuffer)
        {
            if (idPuffer <= 0) return -1;

            for (int i = 0; i < _projektPuffer.Count; i++)
                if (_projektPuffer[i].ID == idPuffer) return i;

            return -1;
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
            _cbKatalog.Items.Add(MyResource.Resource.PSP_KATALOG_FREIE_EINGABE);
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
                // Befund L0-2: Der DB-Wert der Verwendung wird für die Anzeige übersetzt.
                _lbProjekt.Items.Add(
                    string.Format(MyResource.Resource.PSP_LISTE_EINTRAG,
                                  p.Bezeichner,
                                  WaermesenkeClass.VerwendungAnzeige(WaermesenkeClass.WirksameVerwendung(p)),
                                  p.Gesamtvolumen) +
                    (p.VerwendungFehlt ? MyResource.Resource.PSP_LISTE_VERWENDUNG_FEHLT : ""));
            }
        }

        private void EntladeprioListeFuellen()
        {
            _cbEntladeprio.Items.Clear();
            _cbEntladeprio.Items.Add(new PrioItem { Wert = 0, Text = MyResource.Resource.PSP_PRIO_AUTOMATISCH });
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

                // D5a/D5b: Die Vorbelegung aus dem Senken-Dialog kann jetzt auch „Kombi"
                // sein - der Absprung „Pufferspeicher anlegen…" gibt sie vor.
                VerwendungWaehlen(
                    WaermesenkeClass.IstKombiVerwendung(Verwendung)
                        ? WaermesenkeClass.VERWENDUNG_KOMBI
                        : string.Equals(Verwendung, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                        StringComparison.OrdinalIgnoreCase)
                            ? WaermesenkeClass.VERWENDUNG_BRAUCHWASSER
                            : WaermesenkeClass.VERWENDUNG_HEIZUNG);

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

                _btnUebernehmen.Text = MyResource.Resource.PSP_BTN_ANLEGEN;
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
                VerwendungWaehlen(WaermesenkeClass.WirksameVerwendung(p));

                _tbVorlauf.Text = p.Vorlauf > 0 ? p.Vorlauf.ToString(CultureInfo.InvariantCulture) : "";
                _tbRuecklauf.Text = p.Ruecklauf > 0 ? p.Ruecklauf.ToString(CultureInfo.InvariantCulture) : "";

                _tbSchwelleEin.Text = p.SchwelleEin.ToString("0.#");
                _tbSchwelleAus.Text = p.SchwelleAus.ToString("0.#");
                _tbSchwelleNachrang.Text = p.SchwelleAusNachrang.ToString("0.#");

                PrioWaehlen(_cbEntladeprio, p.Entladeprio);

                _btnUebernehmen.Text = MyResource.Resource.PSP_BTN_UEBERNEHMEN;
                _btnEntfernen.Enabled = true;
            }
            finally
            {
                _aktualisiert = false;
            }

            AnzeigenAktualisieren();
        }

        /// <summary>
        /// Wählt den Verwendungseintrag zu einem DB-Wert (Befund L0-2). Ohne Treffer
        /// bleibt es beim ersten Eintrag („Heizung") — dieselbe Wirkung wie bisher,
        /// wenn <c>SelectedItem</c> auf einen unbekannten Wert gesetzt wurde.
        /// </summary>
        private void VerwendungWaehlen(string dbWert)
        {
            foreach (object o in _cbVerwendung.Items)
            {
                VerwendungItem it = o as VerwendungItem;
                if (it != null && string.Equals(it.DbWert, dbWert, StringComparison.OrdinalIgnoreCase))
                {
                    _cbVerwendung.SelectedItem = o;
                    return;
                }
            }
            if (_cbVerwendung.Items.Count > 0) _cbVerwendung.SelectedIndex = 0;
        }

        /// <summary>
        /// Der DB-Wert der gewählten Verwendung — der Steuerwert, der in die Datenbank
        /// geht und gegen den geprüft wird (Befund L0-2). Leer, solange nichts gewählt ist.
        /// </summary>
        private string GewaehlteVerwendung()
        {
            VerwendungItem it = _cbVerwendung.SelectedItem as VerwendungItem;
            return it != null ? it.DbWert : "";
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
            _lblQmax.Text = string.Format(MyResource.Resource.PSP_ANZEIGE_QMAX, qmax.ToString("0.0"));
        }

        private void LadereihenfolgeAnzeigen()
        {
            _lvLaden.Items.Clear();
            if (_bearbeiteteId <= 0)
            {
                _lvLaden.Items.Add(new ListViewItem(new[]
                    { "", MyResource.Resource.PSP_LADEN_NOCH_NICHT_ANGELEGT, "", "", "", "" }));
                return;
            }

            List<Ladeordnung.LadeEintrag> liste = Ladeordnung.Ladereihenfolge(ID_Projekt, _bearbeiteteId);
            if (liste.Count == 0)
            {
                _lvLaden.Items.Add(new ListViewItem(new[]
                    { "", MyResource.Resource.PSP_LADEN_KEINE_ANLAGE, "", "", "", "" }));
                return;
            }

            for (int i = 0; i < liste.Count; i++)
            {
                Ladeordnung.LadeEintrag e = liste[i];

                // e.Erzeuger kommt aus Ladeordnung.ErzeugerName und ist bereits der
                // lokalisierte ANZEIGEname (nicht der Persistenzwert - der steht in
                // Ladeordnung.KaskadenLiteral).
                string ladeprio = e.PrioManuell
                    ? string.Format(MyResource.Resource.PSP_LADEPRIO_MANUELL, e.Ladeprio)
                    : e.Ladeprio.ToString();
                string obergrenze = e.ObergrenzeEigen
                    ? string.Format(MyResource.Resource.PSP_OBERGRENZE_EIGEN, e.Obergrenze.ToString("0.#"))
                    : e.Obergrenze.ToString("0.#") + " %";

                _lvLaden.Items.Add(new ListViewItem(new[]
                {
                    (i + 1) + ".",
                    e.Bezeichner,
                    e.Erzeuger,
                    // Zelleninhalt einer Tabelle = Beschriftung, deshalb die gross
                    // geschriebenen Schluessel (SIM_ROLLE_* ist die Satzform).
                    e.Zweitsenke ? MyResource.Resource.SIM_SPALTE_ZWEITSENKE
                                 : MyResource.Resource.SIM_GRUPPE_HAUPTSENKE,
                    ladeprio,
                    obergrenze
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

            string verwendung = GewaehlteVerwendung();
            if (verwendung.Length == 0) verwendung = WaermesenkeClass.VERWENDUNG_HEIZUNG;

            // ETAPPE D5b: Ein KOMBISPEICHER steht in BEIDEN Entladereihenfolgen — je
            // Kanal an der Stelle seiner Entladepriorität (D5a, Konzept Abschnitt 5).
            // D5a zeigte nur die Position im Heizkanal, weil „Kombi" selbst kein Kanal
            // ist; die Warmwasserposition fehlte und war als Restpunkt vermerkt. Jetzt
            // stehen BEIDE da, jede mit ihrem Kanalnamen — ohne den Kanalnamen wären zwei
            // Zahlen nebeneinander nicht zuzuordnen.
            if (WaermesenkeClass.IstKombiVerwendung(verwendung))
            {
                _lblEntladeInfo.Text = KombiPositionstext();
                return;
            }

            List<Ladeordnung.EntladeEintrag> reihe = Ladeordnung.Entladereihenfolge(ID_Projekt, verwendung);
            int pos = Ladeordnung.Position(reihe, _bearbeiteteId);

            _lblEntladeInfo.Text = pos > 0
                ? string.Format(MyResource.Resource.PSP_ENTLADE_POSITION,
                                pos, reihe.Count, KanalSpeicherWort(verwendung, reihe.Count))
                : "";
        }

        /// <summary>
        /// Die Positionen eines KOMBISPEICHERS in beiden Kanälen, zeilenweise
        /// untereinander (Etappe D5b).
        ///
        /// Die Zeilen sind bewusst kürzer gefasst als der Satz für den einkanaligen Fall
        /// (<c>PSP_ENTLADE_POSITION</c>): In das Feld passen zwei Zeilen, und der
        /// Kanalname trägt dort die Aussage, die im einkanaligen Fall im Speicherwort
        /// steckt („von 2 Heizungsspeichern"). Ein Kanal, in dem der Speicher nicht
        /// auftaucht — das kann nur passieren, während der Verwendungswechsel noch nicht
        /// übernommen ist —, bleibt weg statt eine „0. von 0" zu zeigen.
        /// </summary>
        private string KombiPositionstext()
        {
            List<string> zeilen = new List<string>();

            zeilen.Add(KanalPositionstext(WaermesenkeClass.VERWENDUNG_HEIZUNG,
                                          MyResource.Resource.PSP_ENTLADE_POSITION_KANAL_HEIZUNG));
            zeilen.Add(KanalPositionstext(WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                          MyResource.Resource.PSP_ENTLADE_POSITION_KANAL_WARMWASSER));

            zeilen.RemoveAll(delegate (string z) { return z.Length == 0; });
            return string.Join(Environment.NewLine, zeilen.ToArray());
        }

        /// <summary>Eine Kanalzeile für <see cref="KombiPositionstext"/>; "" = nicht enthalten.</summary>
        private string KanalPositionstext(string verwendung, string muster)
        {
            List<Ladeordnung.EntladeEintrag> reihe =
                Ladeordnung.Entladereihenfolge(ID_Projekt, verwendung);
            int pos = Ladeordnung.Position(reihe, _bearbeiteteId);

            return pos > 0 ? string.Format(muster, pos, reihe.Count) : "";
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
            bool brauchwasser = string.Equals(verwendung, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                              StringComparison.OrdinalIgnoreCase);

            // Singular und Plural sind je EIGENE Ressourcen. Das frühere „basis + \"n\""
            // war eine deutsche Beugungsregel im Quelltext und im Englischen falsch
            // (dort trägt der Plural ein „s" an anderer Stelle).
            if (brauchwasser)
                return anzahl == 1
                    ? MyResource.Resource.PSP_KANALWORT_BRAUCHWASSERSPEICHER
                    : MyResource.Resource.PSP_KANALWORT_BRAUCHWASSERSPEICHER_PLURAL;

            return anzahl == 1
                ? MyResource.Resource.PSP_KANALWORT_HEIZUNGSSPEICHER
                : MyResource.Resource.PSP_KANALWORT_HEIZUNGSSPEICHER_PLURAL;
        }

        /// <summary>Beschriftet den Automatik-Eintrag mit dem errechneten Wert.</summary>
        private void AutomatikTextSetzen(int automatik)
        {
            if (_cbEntladeprio.Items.Count == 0) return;

            PrioItem it = _cbEntladeprio.Items[0] as PrioItem;
            if (it == null) return;

            it.Text = string.Format(MyResource.Resource.PSP_PRIO_AUTOMATISCH_WERT, automatik);

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
                MessageBox.Show(fehler, MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_ANLEGEN_FEHLGESCHLAGEN,
                                    MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ID_Puffer = neueId;
                _bearbeiteteId = neueId;
                Verwendung = verwendung;
                Status(MyResource.Resource.PSP_STATUS_ANGELEGT);
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
                    MessageBox.Show(MyResource.Resource.PSP_MELDUNG_AENDERN_FEHLGESCHLAGEN,
                                    MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                ID_Puffer = _bearbeiteteId;
                Verwendung = verwendung;
                Status(MyResource.Resource.PSP_STATUS_AENDERUNGEN_UEBERNOMMEN);
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

            // Die beiden Verwendungen sind DB-Werte und werden für die Meldung übersetzt
            // (Befund L0-2) - sonst mischte die englische Meldung die Sprachen.
            return MessageBox.Show(
                string.Format(
                    // Umbrüche der Ressource (LF) auf die Plattformform bringen - und
                    // zwar VOR dem Einsetzen, sonst würden die bereits mit
                    // Environment.NewLine verketteten Referenzen doppelt umgebrochen.
                    MyResource.Resource.PSP_MELDUNG_VERWENDUNGSWECHSEL.Replace("\n", Environment.NewLine),
                    alt.Bezeichner,
                    WaermesenkeClass.VerwendungAnzeige(verwendungAlt),
                    WaermesenkeClass.VerwendungAnzeige(verwendungNeu),
                    string.Join(Environment.NewLine + "  • ", referenzen)),
                MyResource.Resource.PSP_TITEL_VERWENDUNG_AENDERN, MessageBoxButtons.YesNo,
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
                    string.Format(
                        MyResource.Resource.PSP_MELDUNG_ENTFERNEN_BLOCKIERT.Replace("\n", Environment.NewLine),
                        _tbBezeichner.Text,
                        string.Join(Environment.NewLine + "  • ", referenzen)),
                    MyResource.Resource.PSP_TITEL_PUFFER_ENTFERNEN,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (MessageBox.Show(
                    string.Format(
                        MyResource.Resource.PSP_MELDUNG_ENTFERNEN_BESTAETIGEN.Replace("\n", Environment.NewLine),
                        _tbBezeichner.Text),
                    MyResource.Resource.PSP_TITEL_PUFFER_ENTFERNEN, MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes) return;

            if (!PufferSpCtrl.ProjektPufferEntfernen(_bearbeiteteId, ID_Projekt))
            {
                MessageBox.Show(MyResource.Resource.PSP_MELDUNG_ENTFERNEN_FEHLGESCHLAGEN,
                                MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (ID_Puffer == _bearbeiteteId) ID_Puffer = 0;
            Status(MyResource.Resource.PSP_STATUS_ENTFERNT);
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
            verwendung = GewaehlteVerwendung();   // DB-Wert, nicht der Anzeigetext (L0-2)
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
                fehler = MyResource.Resource.PSP_FEHLER_BEZEICHNER_FEHLT;
                return false;
            }

            if (verwendung.Length == 0)
            {
                fehler = MyResource.Resource.PSP_FEHLER_VERWENDUNG_PFLICHT;
                return false;
            }

            if (!int.TryParse(_tbVolumen.Text.Trim(), NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out volumen) || volumen <= 0)
            {
                fehler = MyResource.Resource.PSP_FEHLER_VOLUMEN;
                return false;
            }

            float f;
            if (_tbVerluste.Text.Trim().Length > 0)
            {
                if (!WaermequelleClass.ZahlParsen(_tbVerluste.Text, out f) || f < 0)
                {
                    fehler = MyResource.Resource.PSP_FEHLER_VERLUSTE;
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

            if (!SchwelleLesen(_tbSchwelleEin, MyResource.Resource.PSP_NAME_EINSCHALTSCHWELLE,
                               out schwelleEin, out fehler)) return false;
            if (!SchwelleLesen(_tbSchwelleAus, MyResource.Resource.PSP_NAME_ABSCHALTSCHWELLE,
                               out schwelleAus, out fehler)) return false;
            if (!SchwelleLesen(_tbSchwelleNachrang, MyResource.Resource.PSP_NAME_ABSCHALTSCHWELLE_NACHRANG,
                               out schwelleNachrang, out fehler)) return false;

            if (schwelleEin >= schwelleAus)
            {
                fehler = MyResource.Resource.PSP_FEHLER_EIN_KLEINER_AUS;
                return false;
            }

            if (schwelleNachrang > schwelleAus)
            {
                fehler = MyResource.Resource.PSP_FEHLER_NACHRANG_UEBER_AUS;
                return false;
            }

            if (schwelleNachrang <= schwelleEin)
            {
                fehler = MyResource.Resource.PSP_FEHLER_NACHRANG_UNTER_EIN;
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
                fehler = string.Format(MyResource.Resource.PSP_FEHLER_SCHWELLE_ZAHL, name);
                return false;
            }

            if (f <= 0 || f > 100)
            {
                fehler = string.Format(MyResource.Resource.PSP_FEHLER_SCHWELLE_BEREICH, name);
                return false;
            }

            wert = f;
            return true;
        }
    }
}
