using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wärmesenke einer Wärmeerzeuger-Anlage (Konzept 4.2).
    ///
    /// Jede Anlage hat genau EINE Hauptsenke — Heizkreis (direkte Deckung des
    /// Momentanbedarfs), Pufferspeicher Heizung, Pufferspeicher Brauchwasser oder, seit
    /// Etappe D5a, Pufferspeicher Kombi (ein Vorrat für Heizung und Warmwasser) — und
    /// optional eine Zweitsenke, die ausschließlich Überschuss bzw. verbleibendes
    /// Ladepotenzial verwertet.
    ///
    /// Aufbau wie beim Bestandsmuster <see cref="Form_QuellePufferspeicher"/>: komplett
    /// programmatisch, kein Designer, keine .resx; Datenübergabe über öffentliche Felder;
    /// Validierung im OK-Klick mit <c>DialogResult.None</c>.
    ///
    /// Die Fachlogik (Lesen, Schreiben, Prüfen nach 4.6, Ladeordnung nach 3.4) steht in
    /// <see cref="WaermesenkeClass"/> und <see cref="Ladeordnung"/> — dieser Dialog ist
    /// reine Oberfläche darüber.
    ///
    /// Die sichtbaren Texte stehen seit Paket 9 / L7 (Konzept 13.6) im Ressourcenkatalog
    /// (<c>MyResource.Resource.SIM_*</c>). Steuerwerte — Ziel, Bedarfsart, Verwendung —
    /// bleiben davon unberührt: sie kommen aus <see cref="WaermesenkeClass"/> bzw.
    /// <see cref="WaermequelleClass"/> und sind deutsche Persistenzwerte
    /// (Drei-Schichten-Regel).
    /// </summary>
    public class Form_Waermesenke : Form
    {
        // --- Übergabe ----------------------------------------------------------------

        /// <summary>Projekt der Anlage.</summary>
        public int ID_Projekt;

        /// <summary>Tab_Energieanlagen.ID der Anlage.</summary>
        public int ID_Anlage;

        /// <summary>ID_Type der Anlage (1 WP, 2 Solarthermie, 10 Heizkessel, 11 BHKW).</summary>
        public int ID_Type;

        /// <summary>Bezeichner der Anlage (Fenstertitel).</summary>
        public string AnlagenName = "";

        /// <summary>Betriebsmodus der Anlage — die PV-Zeile erscheint nur bei „PV" (Konzept 3.5).</summary>
        public string BM_Typ = "";

        /// <summary>Die Senkeneinstellung: beim Öffnen Vorbelegung, nach OK das Ergebnis.</summary>
        public WaermesenkeClass.SenkeDaten Daten = new WaermesenkeClass.SenkeDaten();

        // --- Oberfläche ---------------------------------------------------------------

        private RadioButton _rbHeizkreis;
        private RadioButton _rbPufferHeizung;
        private RadioButton _rbPufferBrauchwasser;

        /// <summary>Vierte Option der Hauptsenke: Kombispeicher (Etappe D5a).</summary>
        private RadioButton _rbPufferKombi;

        private ComboBox _cbBedarfsart;
        private ComboBox _cbPufferHeizung;
        private ComboBox _cbPufferBrauchwasser;
        private ComboBox _cbPufferKombi;

        private GroupBox _gbLaden;
        private ComboBox _cbLadeprio;
        private Label _lblPosition;
        private CheckBox _chkLadegrenze;
        private TextBox _tbLadegrenze;
        private Label _lblLadegrenzeEinheit;
        private Label _lblPV;
        private ComboBox _cbLadeprioPV;

        private CheckBox _chkZweitsenke;
        private GroupBox _gbZweitsenke;
        private ComboBox _cbZiel2;
        private ComboBox _cbPuffer2;
        private ComboBox _cbLadeprio2;
        private CheckBox _chkLadegrenze2;
        private TextBox _tbLadegrenze2;

        private Label _lblHinweis;
        private Button _btnPufferAnlegen;

        private List<WaermesenkeClass.PufferInfo> _pufferHeizung =
            new List<WaermesenkeClass.PufferInfo>();
        private List<WaermesenkeClass.PufferInfo> _pufferBrauchwasser =
            new List<WaermesenkeClass.PufferInfo>();

        /// <summary>Projekt-Puffer mit Verwendung „Kombi" (Etappe D5a).</summary>
        private List<WaermesenkeClass.PufferInfo> _pufferKombi =
            new List<WaermesenkeClass.PufferInfo>();

        private bool _aktualisiert;   // verhindert Event-Rückkopplung beim Befüllen

        /// <summary>Eintrag der Ladeprioritäts-Dropdowns (0 = nach Vorgabe).</summary>
        private class PrioItem
        {
            public int Wert;
            public string Text = "";
            public override string ToString() { return Text; }
        }

        public Form_Waermesenke()
        {
            BaueOberflaeche();
        }

        private void BaueOberflaeche()
        {
            this.Text = MyResource.Resource.SIM_SENKE_TITEL;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            // D5a: Die vierte Option der Hauptsenke braucht eine Zeile mehr - Gruppe und
            // alles darunter rücken um genau diese Zeilenhöhe (26 px) nach unten.
            this.ClientSize = new Size(620, 618);

            // --- Hauptsenke ----------------------------------------------------------
            // Beschriftung, deshalb der gross geschriebene Schluessel: SIM_ROLLE_* liefert
            // die klein geschriebene Satzform ("main sink") fuer den Einsatz in Meldungen.
            GroupBox gbHaupt = new GroupBox
            {
                Text = MyResource.Resource.SIM_GRUPPE_HAUPTSENKE,
                Location = new Point(12, 10),
                Size = new Size(596, 158)
            };
            this.Controls.Add(gbHaupt);

            _rbHeizkreis = new RadioButton
            {
                Text = MyResource.Resource.SIM_RB_HEIZKREIS,
                AutoSize = true,
                Location = new Point(16, 24)
            };
            _rbHeizkreis.CheckedChanged += Auswahl_Geaendert;

            Label lblBedarf = new Label
            {
                Text = MyResource.Resource.SIM_LBL_BEDARFSART,
                AutoSize = true,
                Location = new Point(40, 50)
            };
            _cbBedarfsart = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 46),
                Width = 210
            };
            _cbBedarfsart.Items.AddRange(new object[]
            {
                MyResource.Resource.SIM_BEDARF_BEIDES, MyResource.Resource.SIM_BEDARF_WARMWASSER, MyResource.Resource.SIM_BEDARF_HEIZWAERME
            });

            Label lblBedarfHinweis = new Label
            {
                Text = MyResource.Resource.SIM_LBL_BEDARF_HINWEIS,
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Location = new Point(370, 50)
            };

            _rbPufferHeizung = new RadioButton
            {
                Text = MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_HEIZUNG,
                AutoSize = true,
                Location = new Point(16, 76)
            };
            _rbPufferHeizung.CheckedChanged += Auswahl_Geaendert;
            _cbPufferHeizung = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(240, 73),
                Width = 340
            };
            _cbPufferHeizung.SelectedIndexChanged += Auswahl_Geaendert;

            _rbPufferBrauchwasser = new RadioButton
            {
                Text = MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_BRAUCHWASSER,
                AutoSize = true,
                Location = new Point(16, 102)
            };
            _rbPufferBrauchwasser.CheckedChanged += Auswahl_Geaendert;
            _cbPufferBrauchwasser = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(240, 99),
                Width = 340
            };
            _cbPufferBrauchwasser.SelectedIndexChanged += Auswahl_Geaendert;

            // D5a: vierte Option — Kombispeicher (Konzept_KonfigUI_Hydraulik,
            // Anforderungen 4 und 7).
            _rbPufferKombi = new RadioButton
            {
                Text = MyResource.Resource.SIM_RB_PUFFER_KOMBI,
                AutoSize = true,
                Location = new Point(16, 128)
            };
            _rbPufferKombi.CheckedChanged += Auswahl_Geaendert;
            _cbPufferKombi = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(240, 125),
                Width = 340
            };
            _cbPufferKombi.SelectedIndexChanged += Auswahl_Geaendert;

            gbHaupt.Controls.Add(_rbHeizkreis);
            gbHaupt.Controls.Add(lblBedarf);
            gbHaupt.Controls.Add(_cbBedarfsart);
            gbHaupt.Controls.Add(lblBedarfHinweis);
            gbHaupt.Controls.Add(_rbPufferHeizung);
            gbHaupt.Controls.Add(_cbPufferHeizung);
            gbHaupt.Controls.Add(_rbPufferBrauchwasser);
            gbHaupt.Controls.Add(_cbPufferBrauchwasser);
            gbHaupt.Controls.Add(_rbPufferKombi);
            gbHaupt.Controls.Add(_cbPufferKombi);

            // --- Ladeverhalten der Hauptsenke ----------------------------------------
            _gbLaden = new GroupBox
            {
                Text = MyResource.Resource.SIM_GB_LADEVERHALTEN,
                Location = new Point(12, 176),
                Size = new Size(596, 140)
            };
            this.Controls.Add(_gbLaden);

            Label lblPrio = new Label { Text = MyResource.Resource.SIM_LBL_LADEPRIO, AutoSize = true, Location = new Point(16, 28) };
            _cbLadeprio = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 24),
                Width = 210
            };
            _cbLadeprio.SelectedIndexChanged += Auswahl_Geaendert;

            _lblPosition = new Label
            {
                AutoSize = false,
                Location = new Point(370, 28),
                Size = new Size(210, 32),
                Text = ""
            };

            _chkLadegrenze = new CheckBox
            {
                Text = MyResource.Resource.SIM_CHK_LADEGRENZE,
                AutoSize = true,
                Location = new Point(19, 66)
            };
            _chkLadegrenze.CheckedChanged += Auswahl_Geaendert;
            _tbLadegrenze = new TextBox { Location = new Point(196, 63), Width = 60, Text = "70" };
            _lblLadegrenzeEinheit = new Label
            {
                Text = MyResource.Resource.SIM_LBL_LADEGRENZE_EINHEIT,
                AutoSize = true,
                Location = new Point(262, 66)
            };

            _lblPV = new Label { Text = MyResource.Resource.SIM_LBL_PV_UEBERSCHUSS, AutoSize = true, Location = new Point(16, 102) };
            _cbLadeprioPV = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 98),
                Width = 210
            };
            _cbLadeprioPV.SelectedIndexChanged += Auswahl_Geaendert;

            _gbLaden.Controls.Add(lblPrio);
            _gbLaden.Controls.Add(_cbLadeprio);
            _gbLaden.Controls.Add(_lblPosition);
            _gbLaden.Controls.Add(_chkLadegrenze);
            _gbLaden.Controls.Add(_tbLadegrenze);
            _gbLaden.Controls.Add(_lblLadegrenzeEinheit);
            _gbLaden.Controls.Add(_lblPV);
            _gbLaden.Controls.Add(_cbLadeprioPV);

            // --- Zweitsenke -----------------------------------------------------------
            _chkZweitsenke = new CheckBox
            {
                Text = MyResource.Resource.SIM_CHK_ZWEITSENKE,
                AutoSize = true,
                Location = new Point(20, 326)
            };
            _chkZweitsenke.CheckedChanged += Auswahl_Geaendert;
            this.Controls.Add(_chkZweitsenke);

            _gbZweitsenke = new GroupBox
            {
                Text = "",
                Location = new Point(12, 346),
                Size = new Size(596, 132)
            };
            this.Controls.Add(_gbZweitsenke);

            Label lblZiel2 = new Label { Text = MyResource.Resource.SIM_LBL_ZIEL2, AutoSize = true, Location = new Point(16, 28) };
            _cbZiel2 = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 24),
                Width = 210
            };
            // D5a: dritter Eintrag — der Kombispeicher ist auch als ZWEITsenke zulässig
            // (Konzept Anforderung 4: „Alle Wärmeerzeuger haben als Senke die Optionen …").
            _cbZiel2.Items.AddRange(new object[] { MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_HEIZUNG,
                                                   MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_BRAUCHWASSER,
                                                   MyResource.Resource.SIM_ZIEL_PUFFERSPEICHER_KOMBI });
            _cbZiel2.SelectedIndexChanged += Auswahl_Geaendert;

            Label lblPuffer2 = new Label { Text = MyResource.Resource.PSP_RUBRIK_LABEL, AutoSize = true, Location = new Point(16, 60) };
            _cbPuffer2 = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 56),
                Width = 430
            };
            _cbPuffer2.SelectedIndexChanged += Auswahl_Geaendert;

            Label lblPrio2 = new Label { Text = MyResource.Resource.SIM_LBL_LADEPRIO, AutoSize = true, Location = new Point(16, 96) };
            _cbLadeprio2 = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(150, 92),
                Width = 210
            };
            // Wie alle übrigen Auswahlfelder an den gemeinsamen Handler: sonst bleibt
            // eine Änderung der Zweitsenken-Ladepriorität das einzige Bedienelement des
            // Dialogs, das die Anzeige nicht auffrischt.
            _cbLadeprio2.SelectedIndexChanged += Auswahl_Geaendert;

            _chkLadegrenze2 = new CheckBox
            {
                Text = MyResource.Resource.SIM_CHK_LADEGRENZE2,
                AutoSize = true,
                Location = new Point(378, 95)
            };
            _chkLadegrenze2.CheckedChanged += Auswahl_Geaendert;
            _tbLadegrenze2 = new TextBox { Location = new Point(500, 92), Width = 50, Text = "70" };
            Label lblProzent2 = new Label { Text = "%", AutoSize = true, Location = new Point(556, 95) };

            _gbZweitsenke.Controls.Add(lblZiel2);
            _gbZweitsenke.Controls.Add(_cbZiel2);
            _gbZweitsenke.Controls.Add(lblPuffer2);
            _gbZweitsenke.Controls.Add(_cbPuffer2);
            _gbZweitsenke.Controls.Add(lblPrio2);
            _gbZweitsenke.Controls.Add(_cbLadeprio2);
            _gbZweitsenke.Controls.Add(_chkLadegrenze2);
            _gbZweitsenke.Controls.Add(_tbLadegrenze2);
            _gbZweitsenke.Controls.Add(lblProzent2);

            // --- Hinweis und Absprung -------------------------------------------------
            //
            // NACHARBEIT I-K1-1 — DIE HÖHE WIRD GERECHNET, NICHT GESCHÄTZT.
            //
            // Der Hinweistext ist mit dem Kombi-Satz von rund 116 auf rund 271 Zeichen
            // gewachsen; die feste Fläche 390 × 56 px trug davon drei Zeilen, der Rest
            // wurde unten abgeschnitten — ausgerechnet die Knappheitsregel „Warmwasser
            // zuerst", für die der Satz da ist. Weil der Text zudem übersetzt wird und
            // die englische Fassung anders umbricht, ist jede feste Höhe die nächste
            // Fehlerquelle. TextRenderer.MeasureText misst den Umbruch mit DERSELBEN
            // Schrift und DERSELBEN Breite, mit der das Label ihn später zeichnet;
            // Trenner, Knöpfe und ClientSize hängen an dem Ergebnis.
            const int HINWEIS_LINKS = 14;
            const int HINWEIS_BREITE = 390;
            const int HINWEIS_OBEN = 488;
            const int HINWEIS_MIN = 56;     // nie kleiner als der Bestand
            const int HINWEIS_MAX = 160;    // Notbremse gegen eine entgleiste Übersetzung

            string hinweisText = MyResource.Resource.SIM_LBL_HINWEIS_PUFFER +
                                 Environment.NewLine + MyResource.Resource.SIM_LBL_HINWEIS_KOMBI;

            int hinweisHoehe = TextRenderer.MeasureText(
                hinweisText, this.Font, new Size(HINWEIS_BREITE, int.MaxValue),
                TextFormatFlags.WordBreak | TextFormatFlags.TextBoxControl).Height + 4;

            if (hinweisHoehe < HINWEIS_MIN) hinweisHoehe = HINWEIS_MIN;
            if (hinweisHoehe > HINWEIS_MAX) hinweisHoehe = HINWEIS_MAX;

            _lblHinweis = new Label
            {
                AutoSize = false,
                Location = new Point(HINWEIS_LINKS, HINWEIS_OBEN),
                Size = new Size(HINWEIS_BREITE, hinweisHoehe),
                // D5a: Der Bestandshinweis bleibt; der Kombi-Satz kommt dazu, weil die
                // Knappheitsregel (Warmwasser zuerst) sonst nirgends sichtbar wäre.
                Text = hinweisText
            };
            this.Controls.Add(_lblHinweis);

            _btnPufferAnlegen = new Button
            {
                Text = MyResource.Resource.PSP_BTN_PUFFER_ANLEGEN,
                Location = new Point(410, HINWEIS_OBEN + 4),
                Size = new Size(198, 28)
            };
            _btnPufferAnlegen.Click += btnPufferAnlegen_Click;
            this.Controls.Add(_btnPufferAnlegen);

            // Trenner und Fußzeile hängen am gemessenen Hinweis. Die Abstände sind die
            // bisherigen (10 px über dem Trenner, 18 px darunter, 23 px Knopfhöhe,
            // 23 px Rand) — bei einem 56-px-Hinweis kommt exakt das alte Raster heraus.
            int trennerOben = HINWEIS_OBEN + hinweisHoehe + 10;
            int knopfOben = trennerOben + 18;

            Label trenner = new Label
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(12, trennerOben),
                Size = new Size(596, 2)
            };
            this.Controls.Add(trenner);

            Button btnOk = new Button
            {
                Text = MyResource.Resource.SIM_BTN_OK,
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 190, knopfOben),
                Width = 85
            };
            Button btnAbbruch = new Button
            {
                Text = MyResource.Resource.SIM_BTN_ABBRECHEN,
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 97, knopfOben),
                Width = 85
            };
            btnOk.Click += btnOk_Click;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbruch);
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbruch;

            // Fensterhöhe zuletzt, aus der gemessenen Fußzeile. Der Absprungknopf reicht
            // bei einem kurzen Hinweis tiefer als das Label - beide gehen in die Rechnung.
            int unten = Math.Max(knopfOben + btnOk.Height,
                                 Math.Max(HINWEIS_OBEN + hinweisHoehe,
                                          _btnPufferAnlegen.Bottom));
            this.ClientSize = new Size(this.ClientSize.Width, unten + 23);
        }

        // --- Befüllen -----------------------------------------------------------------

        /// <summary>Füllt den Dialog aus <see cref="Daten"/> und den Projekt-Puffern.</summary>
        public void SetControls()
        {
            if (!string.IsNullOrEmpty(AnlagenName))
                this.Text = string.Format(MyResource.Resource.SIM_SENKE_TITEL_ANLAGE, AnlagenName);

            _aktualisiert = true;
            try
            {
                PufferListenLaden();
                PrioListeFuellen(_cbLadeprio, false);
                PrioListeFuellen(_cbLadeprio2, false);
                PrioListeFuellen(_cbLadeprioPV, true);

                WaermesenkeClass.Normalisieren(Daten);

                // Hauptsenke
                if (string.Equals(Daten.Ziel, WaermesenkeClass.ZIEL_PUFFER_HEIZUNG, StringComparison.Ordinal))
                    _rbPufferHeizung.Checked = true;
                else if (string.Equals(Daten.Ziel, WaermesenkeClass.ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                    _rbPufferBrauchwasser.Checked = true;
                else if (string.Equals(Daten.Ziel, WaermesenkeClass.ZIEL_PUFFER_KOMBI, StringComparison.Ordinal))
                    _rbPufferKombi.Checked = true;                 // D5a
                else
                    _rbHeizkreis.Checked = true;

                switch (Daten.Bedarfsart)
                {
                    case WaermequelleClass.SENKE_WARMWASSER: _cbBedarfsart.SelectedIndex = 1; break;
                    case WaermequelleClass.SENKE_HEIZUNG: _cbBedarfsart.SelectedIndex = 2; break;
                    default: _cbBedarfsart.SelectedIndex = 0; break;
                }

                PufferWaehlen(_cbPufferHeizung, _pufferHeizung, Daten.ID_Puffer);
                PufferWaehlen(_cbPufferBrauchwasser, _pufferBrauchwasser, Daten.ID_Puffer);
                PufferWaehlen(_cbPufferKombi, _pufferKombi, Daten.ID_Puffer);     // D5a

                PrioWaehlen(_cbLadeprio, Daten.Ladeprio);
                PrioWaehlen(_cbLadeprioPV, Daten.LadeprioPV);

                _chkLadegrenze.Checked = Daten.Ladegrenze > 0;
                if (Daten.Ladegrenze > 0) _tbLadegrenze.Text = Daten.Ladegrenze.ToString("0.#");

                // Zweitsenke
                _chkZweitsenke.Checked = Daten.HatZweitsenke;
                if (string.Equals(Daten.Ziel2, WaermesenkeClass.ZIEL_PUFFER_BRAUCHWASSER, StringComparison.Ordinal))
                    _cbZiel2.SelectedIndex = 1;
                else if (string.Equals(Daten.Ziel2, WaermesenkeClass.ZIEL_PUFFER_KOMBI, StringComparison.Ordinal))
                    _cbZiel2.SelectedIndex = 2;                   // D5a
                else
                    _cbZiel2.SelectedIndex = 0;
                Puffer2ListeFuellen();
                PufferWaehlen(_cbPuffer2, Zweitsenkenliste(), Daten.ID_Puffer2);
                PrioWaehlen(_cbLadeprio2, Daten.Ladeprio2);
                _chkLadegrenze2.Checked = Daten.Ladegrenze2 > 0;
                if (Daten.Ladegrenze2 > 0) _tbLadegrenze2.Text = Daten.Ladegrenze2.ToString("0.#");
            }
            finally
            {
                _aktualisiert = false;
            }

            AnzeigeAktualisieren();
        }

        private void PufferListenLaden()
        {
            // SENKENZIEL-Sicht, nicht Kanalsicht (WaermesenkeClass.ProjektPufferListe):
            // Ein Kombi-Ziel verlangt einen Kombi-Puffer, ein Heizungs-Ziel einen
            // Heizungs-Puffer (Konzept Abschnitt 7). Genau dasselbe prüft
            // WaermesenkeClass.Pruefen beim Speichern - Auswahl und Validierung dürfen
            // nicht auseinanderlaufen.
            _pufferHeizung = WaermesenkeClass.ProjektPufferListe(ID_Projekt, WaermesenkeClass.VERWENDUNG_HEIZUNG);
            _pufferBrauchwasser = WaermesenkeClass.ProjektPufferListe(ID_Projekt, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER);
            _pufferKombi = WaermesenkeClass.ProjektPufferListe(ID_Projekt, WaermesenkeClass.VERWENDUNG_KOMBI);

            FuelleCombo(_cbPufferHeizung, _pufferHeizung);
            FuelleCombo(_cbPufferBrauchwasser, _pufferBrauchwasser);
            FuelleCombo(_cbPufferKombi, _pufferKombi);
        }

        private static void FuelleCombo(ComboBox cb, List<WaermesenkeClass.PufferInfo> liste)
        {
            int alteId = AktuelleId(cb);
            cb.Items.Clear();
            foreach (WaermesenkeClass.PufferInfo p in liste) cb.Items.Add(p);
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
            if (alteId > 0) PufferWaehlen(cb, liste, alteId);
        }

        private static void PufferWaehlen(ComboBox cb, List<WaermesenkeClass.PufferInfo> liste, int idPuffer)
        {
            if (idPuffer <= 0) return;
            for (int i = 0; i < liste.Count; i++)
            {
                if (liste[i].ID == idPuffer) { cb.SelectedIndex = i; return; }
            }
        }

        private static int AktuelleId(ComboBox cb)
        {
            WaermesenkeClass.PufferInfo p = cb.SelectedItem as WaermesenkeClass.PufferInfo;
            return p != null ? p.ID : 0;
        }

        /// <summary>Füllt ein Prioritäts-Dropdown: „nach Vorgabe" plus die Werte 1…99.</summary>
        private void PrioListeFuellen(ComboBox cb, bool pvVariante)
        {
            cb.Items.Clear();

            int vorgabe = Ladeordnung.VorgabeLadeprio(ID_Type);
            cb.Items.Add(new PrioItem
            {
                Wert = 0,
                Text = pvVariante
                    ? MyResource.Resource.SIM_PRIO_UNVERAENDERT
                    : string.Format(MyResource.Resource.SIM_PRIO_VORGABE,
                                    vorgabe, Ladeordnung.ErzeugerName(ID_Type))
            });

            for (int p = Ladeordnung.PRIO_MIN; p <= Ladeordnung.PRIO_MAX; p++)
                cb.Items.Add(new PrioItem { Wert = p, Text = p.ToString() });

            cb.SelectedIndex = 0;
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

        private List<WaermesenkeClass.PufferInfo> Zweitsenkenliste()
        {
            if (_cbZiel2.SelectedIndex == 1) return _pufferBrauchwasser;
            if (_cbZiel2.SelectedIndex == 2) return _pufferKombi;    // D5a
            return _pufferHeizung;
        }

        /// <summary>Ziel-Persistenzwert der aktuell gewählten Zweitsenke (D5a).</summary>
        private string ZielWertZweitsenke()
        {
            if (_cbZiel2.SelectedIndex == 1) return WaermesenkeClass.ZIEL_PUFFER_BRAUCHWASSER;
            if (_cbZiel2.SelectedIndex == 2) return WaermesenkeClass.ZIEL_PUFFER_KOMBI;
            return WaermesenkeClass.ZIEL_PUFFER_HEIZUNG;
        }

        private void Puffer2ListeFuellen()
        {
            FuelleCombo(_cbPuffer2, Zweitsenkenliste());
        }

        // --- Ereignisse ---------------------------------------------------------------

        private void Auswahl_Geaendert(object sender, EventArgs e)
        {
            if (_aktualisiert) return;

            if (sender == _cbZiel2)
            {
                _aktualisiert = true;
                try { Puffer2ListeFuellen(); }
                finally { _aktualisiert = false; }
            }

            AnzeigeAktualisieren();
        }

        /// <summary>Blendet die Bereiche passend zur Auswahl ein und rechnet die Position neu.</summary>
        private void AnzeigeAktualisieren()
        {
            bool pufferSenke = _rbPufferHeizung.Checked || _rbPufferBrauchwasser.Checked ||
                               _rbPufferKombi.Checked;

            // Bedarfsart ist nur beim Heizkreis die Feinsteuerung (Konzept 3.1)
            _cbBedarfsart.Enabled = _rbHeizkreis.Checked;
            _cbPufferHeizung.Enabled = _rbPufferHeizung.Checked;
            _cbPufferBrauchwasser.Enabled = _rbPufferBrauchwasser.Checked;
            _cbPufferKombi.Enabled = _rbPufferKombi.Checked;

            _gbLaden.Enabled = pufferSenke;
            _tbLadegrenze.Enabled = pufferSenke && _chkLadegrenze.Checked;

            // Die PV-Sonderregel greift nur bei Betriebsmodus PV (Konzept 3.5)
            bool pvModus = string.Equals(BM_Typ, WaermequelleClass.MODUS_PV, StringComparison.Ordinal);
            _lblPV.Visible = pvModus;
            _cbLadeprioPV.Visible = pvModus;

            _gbZweitsenke.Enabled = _chkZweitsenke.Checked;
            _tbLadegrenze2.Enabled = _chkZweitsenke.Checked && _chkLadegrenze2.Checked;

            _lblPosition.Text = PositionsText();
        }

        /// <summary>„Lädt als n. von m" für die aktuell gewählte Priorität (Konzept 3.4/4.2).</summary>
        private string PositionsText()
        {
            int idPuffer = AktuellerHauptPuffer();
            if (idPuffer <= 0) return "";

            List<Ladeordnung.LadeEintrag> vorschau = Ladeordnung.LadereihenfolgeVorschau(
                ID_Projekt, idPuffer, ID_Anlage, ID_Type, false,
                GewaehltePrio(_cbLadeprio), LadegrenzeWert(_chkLadegrenze, _tbLadegrenze),
                GewaehltePrio(_cbLadeprioPV));

            int pos = Ladeordnung.Position(vorschau, ID_Anlage, false);
            if (pos <= 0) return "";

            // Formatangabe „0.#" der Obergrenze aus dem Bestand übernommen; der Katalog
            // führt den Platzhalter normalisiert als {0} (Lesehinweis des Katalogs).
            string text = string.Format(MyResource.Resource.SIM_POSITION_LAEDT_ALS, pos, vorschau.Count);
            if (vorschau.Count > 0 && pos <= vorschau.Count)
                text += Environment.NewLine + string.Format(MyResource.Resource.SIM_POSITION_BIS,
                                                            vorschau[pos - 1].Obergrenze.ToString("0.#"));
            return text;
        }

        private int AktuellerHauptPuffer()
        {
            if (_rbPufferHeizung.Checked) return AktuelleId(_cbPufferHeizung);
            if (_rbPufferBrauchwasser.Checked) return AktuelleId(_cbPufferBrauchwasser);
            if (_rbPufferKombi.Checked) return AktuelleId(_cbPufferKombi);      // D5a
            return 0;
        }

        private static double LadegrenzeWert(CheckBox chk, TextBox tb)
        {
            if (!chk.Checked) return 0;
            float wert;
            if (!WaermequelleClass.ZahlParsen(tb.Text, out wert)) return 0;
            return wert;
        }

        private void btnPufferAnlegen_Click(object sender, EventArgs e)
        {
            Form_PufferSp_Projekt frm = new Form_PufferSp_Projekt();
            frm.ID_Projekt = ID_Projekt;
            // Vorbelegung der Verwendung passend zur gerade gewählten Senke.
            // D5a: Die Puffer-VERWALTUNG kennt „Kombi" seit der Nacharbeit I-K2-4 als
            // reguläre dritte Option — die Vorbelegung kommt dort also an und wird beim
            // Übernehmen unverändert zurückgeschrieben.
            if (_rbPufferKombi.Checked || _cbZiel2.SelectedIndex == 2)
                frm.Verwendung = WaermesenkeClass.VERWENDUNG_KOMBI;
            else if (_rbPufferBrauchwasser.Checked || _cbZiel2.SelectedIndex == 1)
                frm.Verwendung = WaermesenkeClass.VERWENDUNG_BRAUCHWASSER;
            else
                frm.Verwendung = WaermesenkeClass.VERWENDUNG_HEIZUNG;
            frm.SetControls();
            frm.ShowDialog(this);

            // Die Verwaltung schreibt sofort in die Datenbank (siehe Klassenkommentar
            // dort) - die Dropdowns werden deshalb UNABHÄNGIG vom DialogResult neu
            // aufgebaut, sonst bliebe ein über das Fensterkreuz verlassener Neuanlage-
            // Vorgang unsichtbar.
            _aktualisiert = true;
            try
            {
                PufferListenLaden();
                Puffer2ListeFuellen();

                if (frm.ID_Puffer > 0)
                {
                    if (WaermesenkeClass.IstKombiVerwendung(frm.Verwendung))
                        PufferWaehlen(_cbPufferKombi, _pufferKombi, frm.ID_Puffer);   // D5a
                    else if (string.Equals(frm.Verwendung, WaermesenkeClass.VERWENDUNG_BRAUCHWASSER,
                                           StringComparison.OrdinalIgnoreCase))
                        PufferWaehlen(_cbPufferBrauchwasser, _pufferBrauchwasser, frm.ID_Puffer);
                    else
                        PufferWaehlen(_cbPufferHeizung, _pufferHeizung, frm.ID_Puffer);
                }
            }
            finally
            {
                _aktualisiert = false;
            }

            AnzeigeAktualisieren();
        }

        // --- Übernahme und Validierung (Konzept 4.6) ----------------------------------

        private void btnOk_Click(object sender, EventArgs e)
        {
            WaermesenkeClass.SenkeDaten neu = AusOberflaeche(out string eingabefehler);
            if (eingabefehler != null)
            {
                MessageBox.Show(eingabefehler, MyResource.Resource.SIM_SENKE_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            WaermesenkeClass.PruefErgebnis erg = WaermesenkeClass.Pruefen(ID_Projekt, ID_Anlage, neu);
            if (!erg.Ok)
            {
                if (erg.AbsprungPufferVerwaltung)
                {
                    // Konzept 4.6: Meldung MIT Absprung "Pufferspeicher anlegen..."
                    DialogResult wahl = MessageBox.Show(
                        string.Format(MyResource.Resource.SIM_MSG_PUFFER_ANLEGEN_FRAGE
                                          .Replace("\n", Environment.NewLine), erg.Fehler),
                        MyResource.Resource.SIM_TITEL_SENKE_PUFFER_FEHLT,
                        MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                    this.DialogResult = DialogResult.None;
                    if (wahl == DialogResult.Yes) btnPufferAnlegen_Click(sender, e);
                    return;
                }

                MessageBox.Show(erg.Fehler, MyResource.Resource.SIM_SENKE_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // Warnung ohne Blockerwirkung (Kanal ohne Bedarf) und der Übergangshinweis
            // zur Brauchwasser-Senke - zusammen in EINER Meldung, damit nicht zwei
            // Dialoge hintereinander bestätigt werden müssen.
            List<string> hinweise = new List<string>();
            if (!string.IsNullOrEmpty(erg.Warnung)) hinweise.Add(erg.Warnung);

            string uebergang = BrauchwasserUebergangsHinweis(neu);
            if (uebergang != null) hinweise.Add(uebergang);

            if (hinweise.Count > 0)
                MessageBox.Show(string.Join(Environment.NewLine + Environment.NewLine, hinweise),
                                MyResource.Resource.SIM_SENKE_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Information);

            Daten = neu;
        }

        /// <summary>
        /// Hinweis auf die REICHWEITE der Brauchwasser-/Kombi-Senke; <c>null</c>, wenn
        /// keine solche Senke im Spiel ist ODER die zweikanalige Kaskade für das Projekt
        /// bereits eingeschaltet ist.
        ///
        /// Der Engine-Umbau ist abgeschlossen; maßgeblich ist heute allein die
        /// Projekteinstellung <c>Tab_Einstellungen.Kaskade_Zweikanalig</c> — der Schalter
        /// „Zweikanalige Kaskade (Vorschau)" im Konfigurationsdialog. IST SIE GESETZT,
        /// verzweigt <c>SimulationControl.Do_Simulation</c> in die zweikanalige Kaskade
        /// und rechnet Heizung und Warmwasser getrennt: Die Senke wirkt, es gibt nichts
        /// zu melden.
        ///
        /// IST SIE NICHT GESETZT, läuft weiter der einkanalige Rechenweg. Der holt den
        /// Pufferspeicher aus <c>Z_ProjektPufferSp</c> und kennt dort ausschließlich den
        /// HEIZUNGS-Speicher der Wärmepumpe. Eine Brauchwasser-/Kombi-Senke wird deshalb
        /// gespeichert und angezeigt, rechnet aber noch nicht mit.
        ///
        /// Bei der WÄRMEPUMPE kommt dann hinzu: <c>WaermesenkeClass.WpSenkeSpiegeln</c>
        /// findet keine Heizungs-Puffersenke mehr und nimmt die bisherige Zuordnung
        /// zurück — die Simulation rechnet danach ganz ohne Speicher. Das darf nicht
        /// still passieren. Für Kessel, BHKW und Solarthermie fasst die Brücke die
        /// Zuordnung nicht an; dort entfällt dieser Satz.
        ///
        /// Der Flag-Stand kommt aus
        /// <see cref="KonfigurationCtrl.KaskadeZweikanaligLesen"/> — genau dem Lesepfad,
        /// über den auch der Konfigurationsdialog seinen Schalter vorbelegt. Keine
        /// zweite SQL-Wahrheit.
        /// </summary>
        private string BrauchwasserUebergangsHinweis(WaermesenkeClass.SenkeDaten d)
        {
            if (d == null) return null;

            // D5a: Der Kombispeicher bedient den Warmwasserkanal mit — für ihn gilt
            // dieselbe Reichweitenaussage.
            bool haupt = WaermesenkeClass.IstBrauchwasserseitig(d.Ziel);
            bool zweit = d.HatZweitsenke && WaermesenkeClass.IstBrauchwasserseitig(d.Ziel2);
            if (!haupt && !zweit) return null;

            // Zweikanalige Kaskade eingeschaltet: Die Senke geht in die Simulation ein —
            // der Hinweis wäre falsch.
            if (KonfigurationCtrl.KaskadeZweikanaligLesen(ID_Projekt)) return null;

            string text = MyResource.Resource.SIM_MSG_BRAUCHWASSER_UEBERGANG
                              .Replace("\n", Environment.NewLine);

            // Nur die Hauptsenke einer Wärmepumpe zieht die Alt-Zuordnung mit sich
            // (die Brücke wertet ausschließlich WS_Ziel der Wärmepumpen aus).
            if (haupt && ID_Type == ProjektPuffer.TYP_WP)
                text += Environment.NewLine + Environment.NewLine +
                        MyResource.Resource.SIM_MSG_BRAUCHWASSER_WP_ZUSATZ;

            return text;
        }

        /// <summary>Liest die Oberfläche aus; <paramref name="fehler"/> nur bei Eingabefehlern.</summary>
        private WaermesenkeClass.SenkeDaten AusOberflaeche(out string fehler)
        {
            fehler = null;
            WaermesenkeClass.SenkeDaten d = new WaermesenkeClass.SenkeDaten();

            if (_rbPufferHeizung.Checked)
            {
                d.Ziel = WaermesenkeClass.ZIEL_PUFFER_HEIZUNG;
                d.ID_Puffer = AktuelleId(_cbPufferHeizung);
            }
            else if (_rbPufferBrauchwasser.Checked)
            {
                d.Ziel = WaermesenkeClass.ZIEL_PUFFER_BRAUCHWASSER;
                d.ID_Puffer = AktuelleId(_cbPufferBrauchwasser);
            }
            else if (_rbPufferKombi.Checked)
            {
                d.Ziel = WaermesenkeClass.ZIEL_PUFFER_KOMBI;        // D5a
                d.ID_Puffer = AktuelleId(_cbPufferKombi);
            }
            else
            {
                d.Ziel = WaermesenkeClass.ZIEL_HEIZKREIS;
            }

            switch (_cbBedarfsart.SelectedIndex)
            {
                case 1: d.Bedarfsart = WaermequelleClass.SENKE_WARMWASSER; break;
                case 2: d.Bedarfsart = WaermequelleClass.SENKE_HEIZUNG; break;
                default: d.Bedarfsart = WaermequelleClass.SENKE_BEIDES; break;
            }

            d.Ladeprio = GewaehltePrio(_cbLadeprio);
            d.LadeprioPV = string.Equals(BM_Typ, WaermequelleClass.MODUS_PV, StringComparison.Ordinal)
                ? GewaehltePrio(_cbLadeprioPV) : 0;

            if (!LadegrenzeLesen(_chkLadegrenze, _tbLadegrenze,
                                 MyResource.Resource.SIM_ROLLE_HAUPTSENKE, out d.Ladegrenze, out fehler))
                return d;

            if (_chkZweitsenke.Checked)
            {
                d.Ziel2 = ZielWertZweitsenke();                     // D5a: inkl. Kombi
                d.ID_Puffer2 = AktuelleId(_cbPuffer2);
                d.Ladeprio2 = GewaehltePrio(_cbLadeprio2);

                if (!LadegrenzeLesen(_chkLadegrenze2, _tbLadegrenze2,
                                     MyResource.Resource.SIM_ROLLE_ZWEITSENKE, out d.Ladegrenze2, out fehler))
                    return d;
            }

            return d;
        }

        /// <summary>Liest eine Ladeobergrenze [%]; 0, wenn die Checkbox nicht gesetzt ist.</summary>
        private static bool LadegrenzeLesen(CheckBox chk, TextBox tb, string rolle,
                                            out double wert, out string fehler)
        {
            wert = 0;
            fehler = null;
            if (!chk.Checked) return true;

            float zahl;
            if (!WaermequelleClass.ZahlParsen(tb.Text, out zahl))
            {
                fehler = string.Format(MyResource.Resource.SIM_MSG_LADEGRENZE_ZAHL, rolle);
                return false;
            }

            if (zahl <= 0 || zahl > 100)
            {
                fehler = string.Format(MyResource.Resource.SIM_MSG_LADEGRENZE_BEREICH, rolle);
                return false;
            }

            wert = zahl;
            return true;
        }
    }
}
