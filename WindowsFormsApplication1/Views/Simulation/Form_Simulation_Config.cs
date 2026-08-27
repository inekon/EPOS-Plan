using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    public partial class Form_Simulation_Config : BaseForm
    {
        public KonfigurationModel Konfiguration = new KonfigurationModel();
        public int m_ID_Projekt;
        private List<string> listErzeuger = new List<string>();

        // D1: Die vier programmatischen Steuerelemente der Alt-Rubrik "Pufferspeicher"
        // (zwei Dropdowns, zwei Checkboxen) samt Rückkopplungssperre sind entfallen -
        // siehe AltRubrikStilllegen(). Sie waren seit Paket 2 unsichtbar und wurden nur
        // noch befüllt.
        //
        // PAKET A1: Mit ihnen ist jetzt auch der DATENBESTAND der Alt-Zuordnung gegangen -
        // das Feld _zuordnungen (Erzeuger, Pufferspeicher, Vorlauf, Rücklauf aus
        // Z_ProjektPufferSp), sein Ladeweg ZuordnungenLaden, der Delete/Insert-Zyklus in
        // btn_Speichern_Click samt Schwellenrettung und Temperatur-Nachführung, der
        // Zelleditor der unsichtbaren listView1 und der Schwellendialog
        // SpeicherregelungBearbeiten. Migrationsschritt 51 hat die Betriebstemperaturen
        // einmalig an Tab_Pufferspeicher übergeben; dort pflegt sie Form_PufferSp_Projekt.

        private Timer statusTimer = new Timer();

        public Form_Simulation_Config()
        {
            // BaseForm setzt AutoScaleMode schon im Konstruktor auf Font. Bei diesem
            // lokalisierten Formular wendet ApplyResources($this) die
            // AutoScaleDimensions (7;17, resx) dann mit bereits aktivem Font-Modus an
            // und skaliert das Formular sofort um den Faktor 15/17 (gemessen:
            // ClientSize-Hoehe 502 statt 552, Schriften auf ~8 pt verkleinert).
            // Der Designer-Code setzt den Font-Modus selbst erst NACH
            // ApplyResources - diese Reihenfolge wird hier wiederhergestellt,
            // damit das Formular unskaliert bleibt wie vor der BaseForm-Ableitung.
            AutoScaleMode = AutoScaleMode.Inherit;

            InitializeComponent();

            // PAKET A1: Spalten, Zelleditor und Mouseover-Hinweise der unsichtbaren
            // listView1 sind entfallen - sie zeigten und bearbeiteten die Alt-Zuordnung
            // Z_ProjektPufferSp. Das Steuerelement selbst bleibt im Designer stehen
            // (zusammen mit groupBox_PufferSp, btn_Hinzu und btn_Loeschen); es wird seit
            // Etappe D1 nicht mehr befüllt und ist unsichtbar.

            // Das Array mit deinen 4 ComboBoxen (Namen anpassen)
            ComboBox[] myComboBoxes = { comboBox1, comboBox2, comboBox3, comboBox4 };

            // Über das Array iterieren
            foreach (var cb in myComboBoxes)
            {
                // WICHTIG: Erst Member setzen, dann DataSource
                cb.DisplayMember = "DisplayName";
                cb.ValueMember = "DbValue";

                // Je ComboBox eine eigene Liste, damit die Boxen unabhängig
                // voneinander selektieren.
                cb.DataSource = ErzeugerKatalog.Liste(ErzeugerKatalog.WAERMEERZEUGER);

                // Auswahl auf leer setzen
                cb.SelectedIndex = -1;
            }

            // Erst JETZT das Event abonnieren
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
            comboBox3.SelectedIndexChanged += comboBox3_SelectedIndexChanged;
            comboBox4.SelectedIndexChanged += comboBox4_SelectedIndexChanged;

            comboBox5.DisplayMember = "DisplayName";
            comboBox5.ValueMember = "DbValue";
            comboBox5.DataSource = ErzeugerKatalog.Liste(ErzeugerKatalog.STROMERZEUGER);
            comboBox5.SelectedIndex = -1;
            comboBox6.DisplayMember = "DisplayName";
            comboBox6.ValueMember = "DbValue";
            comboBox6.DataSource = ErzeugerKatalog.Liste(ErzeugerKatalog.ENERGIESPEICHER);
            comboBox6.SelectedIndex = -1;

            comboBox5.SelectedIndexChanged += comboBox5_SelectedIndexChanged;
            comboBox6.SelectedIndexChanged += comboBox6_SelectedIndexChanged;

            // Aufruf im Konstruktor:
            SetGroupBoxFontBold(groupBox_Tools);

            // Timer konfigurieren
            statusTimer.Interval = 3000; // 3 Sekunden Sichtbarkeit
            statusTimer.Tick += (s, e) => {
                lblStatus.Visible = false;
                statusTimer.Stop();
            };

            // D2/D3: Der gesamte Anzeigebereich - zwei Kartenspalten, Fußzeile,
            // Fenstergröße - entsteht in EINEM Schritt. Er löst AltRubrikStilllegen,
            // InitErzeugerUebersicht und InitPufferFusszeile ab; die vier
            // Selbstkorrekturen der Fenstergeometrie sind damit weg (siehe dort).
            KartenLayoutAufbauen();

            // Karten mit dem füllen, was ohne Projekt schon bekannt ist. Den echten
            // Inhalt bringt SetControls.
            AktualisiereErzeugerUebersicht();

            // Statuszeile MUSS nach KartenLayoutAufbauen ausgerichtet werden - dort
            // bekommt die Knopfzeile ihre endgültige Lage.
            StatuszeileAusrichten();

            // Bereich für den KI-Hilfe-Assistenten melden (nur Bedien-Kontext,
            // keine Projekt- oder Kundendaten)
            this.Activated += (s, e) =>
                HilfeKontext.SetzeBereich("Simulation Konfiguration (Erzeuger definieren, Pufferspeicher zuordnen)");
        }

        /// <summary>
        /// Blendet die Status-Anzeige erstmalig aus - und zwar erst NACH dem Laden.
        ///
        /// Hintergrund (Muster aus Wizard_WPItem, Commit d49075e): BaseForm staucht das
        /// Formular in ihrem OnLoad auf die Bildschirm-Arbeitsfläche; überzähliger
        /// Inhalt wandert in den AutoScroll-Bereich. Den Scroll-Versatz gibt WinForms
        /// nur an Controls weiter, die ein Fensterhandle besitzen - und ein Handle
        /// bekommt beim Aufbau des Formulars nur, wer SICHTBAR ist. Ein per resx
        /// unsichtbares lblStatus bekäme kein Handle, verpasste jeden Versatz und
        /// erschiene beim Speichern (ShowStatus) an der ungescrollten Position statt
        /// neben den Schaltflächen. Deshalb startet lblStatus sichtbar (der
        /// Visible=False-Eintrag in der .resx ist entfernt) und wird hier - noch vor
        /// dem ersten Zeichnen, also ohne Aufblitzen - ausgeblendet.
        ///
        /// Die dauerhaft ausgeblendeten Controls (checkBox_PufferSp und
        /// groupBox_PufferSp, siehe AltRubrikStilllegen) brauchen
        /// diese Behandlung nicht: sie werden zur Laufzeit nie wieder eingeblendet.
        /// Die Bearbeitungs-Dropdowns (comboBox, _wqCombo) sind ebenfalls unkritisch,
        /// weil ihre Bounds bei jedem Einblenden frisch aus Bildschirmkoordinaten
        /// berechnet werden.
        /// </summary>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            lblStatus.Visible = false;
        }

        // PAKET A1: listView1_MouseMove (Mouseover-Hinweise der Zuordnungstabelle),
        // SpeicherregelungBearbeiten (Hysterese-Schwellen der Alt-Zuordnung; seit Etappe
        // D1 ohne Aufrufer, weil sein Einstieg die entfallene Spalte „Zuordnung (alt)"
        // war) und ZugeordnetePufferSp (Leseauskunft auf _zuordnungen, ebenfalls seit D1
        // ohne Aufrufer) sind ERSATZLOS ENTFALLEN.
        //
        // Die Ein-/Abschaltschwellen sind längst am Puffer selbst gepflegt
        // (Form_PufferSp_Projekt), und nur dort liest die Engine sie seit Paket 4.

        // ETAPPE D2/D3: AltRubrikStilllegen ist ENTFALLEN.
        //
        // Die Methode legte die Alt-Rubrik still (das bleibt, jetzt in
        // Form_Simulation_Config.Karten.AltSteuerelementeStilllegen) und stellte
        // daneben die GEOMETRIE her, die der Dialog von ihr geerbt hatte: 105 px
        // Zusatzhöhe, ein Nachziehen der drei unverankerten Fußzeilenelemente und die
        // unsichtbare groupBox_PufferSp als Höhenanker der Übersicht. Alle drei Zwecke
        // sind mit dem Kartenlayout weggefallen - die Höhe kommt aus der Wunschgröße,
        // die Fußzeile ist verankert, und einen Höhenanker braucht ein
        // TableLayoutPanel nicht.

        // PAKET A1: RefreshZuordnungAnzeige, ComboBox_SelectedIndexChanged,
        // ListView_MouseDoubleClick, TemperaturPaarPruefen und IstLeerwert sind
        // ERSATZLOS ENTFALLEN.
        //
        // RefreshZuordnungAnzeige rief seit Etappe D1 nur noch AktualisiereErzeugerUebersicht
        // auf; ihr Name führte in die Irre und ihre Aufrufstellen rufen jetzt direkt.
        // Die übrigen vier bildeten den ZELLEDITOR der unsichtbaren Alt-Tabelle: Auswahl
        // des Speichers per Dropdown, Vorlauf/Rücklauf per Textfeld samt Paarprüfung
        // (B4-2/B4-3). Sie schrieben in _zuordnungen, und die gibt es nicht mehr - die
        // Betriebstemperaturen stehen an Tab_Pufferspeicher und werden in
        // Form_PufferSp_Projekt gepflegt.

        private void btn_OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        // D2: Die sechs Auswahlfelder und ihre Haken sind seit dem Kartenlayout
        // unsichtbar (Form_Simulation_Config.Karten.AltSteuerelementeStilllegen), aber
        // weiterhin das PERSISTENZMODELL von Tab_Einstellungen.Tool_1..6. Bedient werden
        // sie nur noch programmatisch aus KaskadeSchreiben - und genau dagegen schuetzt
        // die Sperre _kaskadeSetzen: Ohne sie riefe jedes einzelne Setzen mitten im
        // Umsortieren AddErzeuger und damit einen halbfertigen Kartenaufbau auf.
        // AddErzeuger laeuft stattdessen einmal am Ende von KaskadeSchreiben.

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_kaskadeSetzen) return;
            if (comboBox1.SelectedIndex != -1)
            {
                checkBox1.Checked = true;
                // listBox1.Items.Add(comboBox1.Text);
                AddErzeuger();
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_kaskadeSetzen) return;
            if (comboBox2.SelectedIndex != -1)
            {
                checkBox2.Checked = true;
                //  listBox1.Items.Add(comboBox2.Text);
                AddErzeuger();
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_kaskadeSetzen) return;
            if (comboBox3.SelectedIndex != -1)
            {
                checkBox3.Checked = true;
                // listBox1.Items.Add(comboBox3.Text);
                AddErzeuger();
            }
        }

        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_kaskadeSetzen) return;
            if (comboBox4.SelectedIndex != -1)
            {
                checkBox4.Checked = true;
                // listBox1.Items.Add(comboBox4.Text);
                AddErzeuger();
            }
        }

        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox5.SelectedIndex != -1) { checkBox5.Checked = true; }
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox6.SelectedIndex != -1) { checkBox6.Checked = true; }
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (_kaskadeSetzen) return;
            if (!checkBox1.Checked) { comboBox1.Text = ""; }
            AddErzeuger();
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (_kaskadeSetzen) return;
            if (!checkBox2.Checked) { comboBox2.Text = ""; }
            AddErzeuger();
        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            if (_kaskadeSetzen) return;
            if (!checkBox3.Checked) { comboBox3.Text = ""; }
            AddErzeuger();
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            if (_kaskadeSetzen) return;
            if (!checkBox4.Checked) { comboBox4.Text = ""; }
            AddErzeuger();
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox5.Checked) { comboBox5.Text = ""; comboBox5.SelectedIndex = -1; }
        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkBox6.Checked) { comboBox6.Text = ""; comboBox6.SelectedIndex = -1; }
        }

        /// <summary>
        /// Schaltet die Eingaben ab, wenn die Schema-Migration nicht durchkam
        /// (ADR-001, Aufgabe 6). Bewusst nur die Kindsteuerelemente und nicht das
        /// Formular selbst - sonst ließe sich das Fenster nicht mehr schließen.
        /// </summary>
        private void SimulationsbereichSperren()
        {
            foreach (Control c in this.Controls) c.Enabled = false;
        }

        public void SetControls(int ID_Projekt)
        {
            // Blockade bei nicht abgeschlossener Schema-Migration (ADR-001, Aufgabe 6):
            // auf halb migriertem Schema zu konfigurieren, führt zu stillen Datenfehlern.
            string sperrgrund;
            if (SchemaMigration.SimulationGesperrt(out sperrgrund))
            {
                MessageBox.Show(sperrgrund,
                                MyResource.Resource.SIM_TITEL_SIMULATION_NICHT_VERFUEGBAR,
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                SimulationsbereichSperren();
                return;
            }

            m_ID_Projekt = ID_Projekt;

            // Neue Spalten (Prioritaet, Wärmequelle) bei Bedarf anlegen
            WaermequelleClass.SchemaSicherstellen();

            comboBox1.SelectedValue = Konfiguration.m_Tool_1;
            comboBox2.SelectedValue = Konfiguration.m_Tool_2;
            comboBox3.SelectedValue = Konfiguration.m_Tool_3;
            comboBox4.SelectedValue = Konfiguration.m_Tool_4;
            comboBox5.SelectedValue = Konfiguration.m_Tool_5;
            comboBox6.SelectedValue = Konfiguration.m_Tool_6;

            // Ä15 (Nutzerabnahme 26.08.2026): Im Projekt ANGELEGTE Anlagen
            // erscheinen von selbst als gewählte Komponenten — auch ohne je
            // gespeicherte Konfiguration. SelectedValue setzt die Combo, deren
            // Bestands-Handler hakt die Checkbox und baut die Karten; eine
            // gespeicherte Auswahl bleibt unangetastet, ergänzt wird nur
            // Fehlendes (Wahrheit: TechnikPlanwertCtrl.Verbaut — dieselbe wie
            // Kostendialoge und Berichte).
            VerbauteAnlagenVorwaehlen();

            // PAKET A1: Hier standen ZuordnungenLaden (Alt-Zuordnung aus
            // Z_ProjektPufferSp) und das Befüllen der Speicher-Auswahlliste aus den
            // Stammdaten - Letztere belieferte den Zelleditor der unsichtbaren
            // Alt-Tabelle und den entfallenen Dialog Form_KonfigPufferspeicher. Beides
            // ist ohne Gegenstück.

            // Beide Kartenspalten mit den geladenen Daten aufbauen. D3: Der frühere
            // Zusatzaufruf AktualisierePufferFusszeile entfällt - die Speicherspalte
            // hängt an derselben Auffrischung.
            AktualisiereErzeugerUebersicht();

            // Einstellung Extrapolation_erlaubt vorbelegen (Paket 8 - Konzept 13.4)
            AktualisiereExtrapolationSchalter();
        }

        /// <summary>Ä15: verbaute Wärmeerzeuger in freie Auswahlplätze heben;
        /// PV und Stromspeicher auf ihre festen Plätze (Combo 5/6).</summary>
        private void VerbauteAnlagenVorwaehlen()
        {
            try
            {
                var paare = new[]
                {
                    new { Cb = checkBox1, Combo = comboBox1 },
                    new { Cb = checkBox2, Combo = comboBox2 },
                    new { Cb = checkBox3, Combo = comboBox3 },
                    new { Cb = checkBox4, Combo = comboBox4 }
                };
                foreach (string erzeuger in ErzeugerKatalog.WAERMEERZEUGER)
                {
                    if (!TechnikPlanwertCtrl.Verbaut(m_ID_Projekt, erzeuger)) continue;
                    bool schon = false;
                    foreach (var p in paare)
                        if (GetDbValue(p.Combo) == erzeuger) schon = true;
                    if (schon) continue;
                    foreach (var p in paare)
                        if (!p.Cb.Checked && string.IsNullOrEmpty(GetDbValue(p.Combo)))
                        { p.Combo.SelectedValue = erzeuger; break; }
                }

                if (TechnikPlanwertCtrl.Verbaut(m_ID_Projekt, DbWerte.ERZEUGER_PHOTOVOLTAIK) &&
                    GetDbValue(comboBox5) != DbWerte.ERZEUGER_PHOTOVOLTAIK)
                    comboBox5.SelectedValue = DbWerte.ERZEUGER_PHOTOVOLTAIK;
                if (TechnikPlanwertCtrl.Verbaut(m_ID_Projekt, DbWerte.ERZEUGER_STROMSPEICHER) &&
                    GetDbValue(comboBox6) != DbWerte.ERZEUGER_STROMSPEICHER)
                    comboBox6.SelectedValue = DbWerte.ERZEUGER_STROMSPEICHER;
            }
            catch { /* Vorwahl ist Komfort — sie darf das Öffnen nie verhindern */ }
        }

        // Hilfsmethode, um den DB-Wert sicher zu extrahieren
        private string GetDbValue(ComboBox cb)
        {
            // Variante A: Über SelectedValue (setzt voraus, dass ValueMember="DbValue" korrekt ist)
            // return cb.SelectedValue?.ToString() ?? "";

            // Variante B: Über das Objekt selbst (sicherste Methode)
            if (cb.SelectedItem is LanguageItem item)
            {
                return item.DbValue;
            }
            return ""; // Falls nichts ausgewählt ist
        }

        private void btn_Speichern_Click(object sender, EventArgs e)
        {
            KonfigurationCtrl ctrl = new KonfigurationCtrl();

            Konfiguration.m_Tool_1 = checkBox1.Checked ? GetDbValue(comboBox1) : "";
            Konfiguration.m_Tool_2 = checkBox2.Checked ? GetDbValue(comboBox2) : "";
            Konfiguration.m_Tool_3 = checkBox3.Checked ? GetDbValue(comboBox3) : "";
            Konfiguration.m_Tool_4 = checkBox4.Checked ? GetDbValue(comboBox4) : "";
            Konfiguration.m_Tool_5 = checkBox5.Checked ? GetDbValue(comboBox5) : "";
            Konfiguration.m_Tool_6 = checkBox6.Checked ? GetDbValue(comboBox6) : "";

            // FRAGE 23 (Nachtrag zu Abnahmebefund 3): Extrapolation_erlaubt steht nicht in
            // der Spaltenliste von KonfigurationCtrl.Insert (die Liste hängt an der
            // Ordinalkette von ReadSingle) - dort zieht ein stilles UPDATE die Vorbelegung
            // WAHR nach. Für NEUE Projekte ist das richtig (Paket 8); beim Wiederspeichern
            // eines bestehenden Projekts überschrieb es die bewusste Abwahl des Anwenders
            // (checkBox_Extrapolation schreibt sofort, die Datenbank ist die Wahrheit),
            // und der nächste Lauf extrapolierte wieder still.
            //
            // Der Lesezugriff VOR dem Delete unterscheidet beide Fälle: Bei fehlender
            // Zeile (neues Projekt), fehlender Spalte, NULL oder Schemastand < 7
            // liefert ExtrapolationErlaubtLesen die Vorbelegung WAHR - dann bleibt
            // unten alles beim Insert-Stand, und "NULL heißt Datenlücke" (Befund N8)
            // kippt nicht.
            //
            // PAKET A1: Dieselbe Rettung gab es für das Feature-Flag der zweikanaligen
            // Kaskade (Abnahmebefund 3). Sie ist mit dem Flag entfallen -
            // Migrationsschritt 51 setzt Kaskade_Zweikanalig in Bestandsdaten auf WAHR und
            // nimmt es aus der Weiche; die Spalte ist Lese-Altlast.
            bool extrapolationErlaubt = KonfigurationCtrl.ExtrapolationErlaubtLesen(m_ID_Projekt);

            ctrl.model = Konfiguration;
            if (!ctrl.Delete(m_ID_Projekt)) return;
            if (ctrl.Insert(m_ID_Projekt))
                ShowStatus(MyResource.Resource.SIM_STATUS_KONFIG_GESPEICHERT, Color.ForestGreen);

            // Spiegelbildlich nur die ABWAHL zurückschreiben - WAHR hat Insert gerade
            // selbst nachgezogen. Ein FALSE aus ExtrapolationErlaubtLesen setzt
            // Schemastand 7 voraus, das UPDATE trifft also nie eine Datenbank ohne
            // die Spalte.
            if (!extrapolationErlaubt)
                KonfigurationCtrl.ExtrapolationErlaubtSchreiben(m_ID_Projekt, false);

            // PAKET A1: Hier stand der Delete/Insert-Zyklus auf Z_ProjektPufferSp - der
            // komplette Alt-Zuordnungsbestand des Projekts, samt Rettung der
            // Hysterese-Schwellen über den Zyklus hinweg (B0-1), der Prioritätsvergabe in
            // Listenreihenfolge (B4-1) und der Nachführung der Betriebstemperaturen an den
            // Puffer über PufferSpCtrl.SetTemperaturen/TemperaturenLoeschen („führende
            // Ablage", Etappe 4).
            //
            // Alles davon ist gegenstandslos: Die Alt-Zuordnung ist mit Migrationsschritt
            // 51 stillgelegt, die Betriebstemperaturen stehen an Tab_Pufferspeicher und
            // werden dort in Form_PufferSp_Projekt gepflegt, die Schwellen ebenso. Die
            // Senken der Anlagen liegen in Z_AnlageSenke und werden im Senkendialog
            // geschrieben - dieser Knopf fasst sie nicht an.
            //
            // Ebenfalls entfallen: die Rückfrage KaskadeAutomatikBeimSpeichern (siehe
            // Form_Simulation_Config.Uebersicht).
        }


        private void AddErzeuger()
        {
            listErzeuger.Clear(); // Liste leeren, wir bauen sie neu auf

            // Wir erstellen ein Array von Paaren: Checkbox + zugehörige ComboBox
            // Das ist viel sauberer als 4 separate Abfragen.
            var controlPairs = new[]
            {
                new { CheckBox = checkBox1, ComboBox = comboBox1 },
                new { CheckBox = checkBox2, ComboBox = comboBox2 },
                new { CheckBox = checkBox3, ComboBox = comboBox3 },
                new { CheckBox = checkBox4, ComboBox = comboBox4 }
            };

            foreach (var pair in controlPairs)
            {
                // SCHRITT 1: Prüfen, ob die Checkbox überhaupt aktiv ist!
                if (pair.CheckBox.Checked)
                {
                    // SCHRITT 2: Wenn ja, prüfen wir die ComboBox
                    if (pair.ComboBox.SelectedItem is LanguageItem selectedItem)
                    {
                        string valueToSave = selectedItem.DbValue;

                        if (!string.IsNullOrEmpty(valueToSave) && !listErzeuger.Contains(valueToSave))
                        {
                            listErzeuger.Add(valueToSave);
                        }
                    }
                }
                // WENN die Checkbox nicht aktiv ist (Checked == false), 
                // wird ihr ComboBox-Inhalt einfach ignoriert und nicht zur Liste hinzugefügt.
                // Ein explizites "Löschen" ist nicht nötig, da wir listErzeuger.Clear() oben machen.
            }

            // "Gesamtsystem" immer hinzufügen (außer es ist schon drin)
            if (!listErzeuger.Contains(DbWerte.ERZEUGER_GESAMTSYSTEM))
            {
                listErzeuger.Add(DbWerte.ERZEUGER_GESAMTSYSTEM);
            }

            // Übersicht rechts an die geänderte Auswahl anpassen
            AktualisiereErzeugerUebersicht();
        }

        // PAKET A1: btn_Hinzu und btn_Loeschen legten Zeilen der Alt-Zuordnung an bzw.
        // entfernten sie; „Hinzufügen" öffnete dafür den Dialog Form_KonfigPufferspeicher,
        // der mit diesem Paket GELÖSCHT ist (Konzept 10). Beide Knöpfe sitzen in der seit
        // Etappe D1 unsichtbaren groupBox_PufferSp und sind über den Designer verdrahtet;
        // die Rümpfe bleiben deshalb als No-op stehen, damit die Designer-Datei
        // unangetastet bleibt (Projektregel: nicht von Hand editieren).
        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
        }

        private void checkBox_PufferSp_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox_PufferSp.Checked)
            {
                groupBox_PufferSp.Visible = true;
            }
            else
            {
                groupBox_PufferSp.Visible = false;
            }
        }

        private void listView1_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            // Hintergrund Hellblau
            using (SolidBrush pb = new SolidBrush(Color.LightBlue))
            {
                e.Graphics.FillRectangle(pb, e.Bounds);
            }

            // Text zeichnen
            TextRenderer.DrawText(e.Graphics, e.Header.Text, e.Font, e.Bounds, Color.Black,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);

            // Rahmen (Optional)
            e.Graphics.DrawRectangle(Pens.LightGray, e.Bounds.X, e.Bounds.Y, e.Bounds.Width, e.Bounds.Height);
        }

        private void listView1_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            e.DrawDefault = true; // Nutzt das Standard-Zeichnen für die Zeile
        }

        private void listView1_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            e.DrawDefault = true; // Nutzt das Standard-Zeichnen für die Zellen (inkl. Symbole)
        }

        private void SetGroupBoxFontBold(GroupBox gb)
        {
            // Titel der GroupBox fett machen
            gb.Font = new Font(gb.Font, FontStyle.Bold);

            // Alle Kinder in der GroupBox wieder auf normal setzen
            foreach (Control c in gb.Controls)
            {
                c.Font = new Font(c.Font, FontStyle.Regular);
            }
            gb.Invalidate(); 
        }

        /// <summary>
        /// Richtet die Statuszeile („✔ Konfiguration erfolgreich gespeichert") an der
        /// Knopfzeile aus und holt sie in den Vordergrund.
        ///
        /// <b>Zwei im Harness gemessene Befunde</b> (Paket 9, Etappe 2b):
        ///
        /// <list type="number">
        ///   <item><description><b>Unterkante abgeschnitten.</b> Die Entwurfsposition
        ///     (y = 390 bei 427 px Nutzhöhe) wandert über <c>Anchor = Bottom</c> mit,
        ///     während <c>AltRubrikStilllegen</c> (+105 px; hieß bis D1
        ///     <c>InitPufferspeicherRubrik</c>) und
        ///     <c>ExtrapolationSchalterPlatzieren</c> (+fehlt) die Nutzhöhe erhöhen und
        ///     die Zeile zusätzlich absolut verschieben. Gemessen lag die Unterkante bei
        ///     555 px, die Nutzfläche endete bei 552 - die letzten drei Pixel der
        ///     Meldung fehlten.</description></item>
        ///   <item><description><b>Verdeckung beim Verkleinern.</b> Der Dialog ist
        ///     <c>Sizable</c> und hat KEINEN Scrollbereich (siehe unten). Zieht der
        ///     Anwender ihn kleiner, zieht die untenverankerte Statuszeile nach oben
        ///     über <c>groupBox_Uebersicht</c> - und die stand mit Z-Index 4 VOR der
        ///     Zeile (Z-Index 5). Gemessen bei 380 px Nutzhöhe: Zeile bei y = 363,
        ///     vollständig hinter der Übersichtsgruppe (109…418).</description></item>
        /// </list>
        ///
        /// <b>Nicht die Ursache aus Commit d49075e.</b> Dort verpassten unsichtbar
        /// gestartete Steuerelemente den <c>AutoScroll</c>-Versatz der
        /// <see cref="BaseForm"/>. Dieses Formular erbt von <see cref="Form"/>, nicht von
        /// <c>BaseForm</c>; <c>AutoScroll</c> ist <c>false</c> und
        /// <c>AutoScrollPosition</c> in jeder gemessenen Fenstergröße <c>0,0</c>. Ein
        /// „sichtbar starten und in OnLoad ausblenden" wäre hier wirkungslos - die
        /// Fehlposition kommt aus der Verankerung, nicht aus einem verpassten Versatz.
        /// </summary>
        private void StatuszeileAusrichten()
        {
            if (lblStatus == null || btn_Speichern == null) return;

            // Senkrecht auf die Knopfzeile zentrieren: Die Knöpfe werden von
            // AltRubrikStilllegen und ExtrapolationSchalterPlatzieren ohnehin
            // nachgezogen und liegen damit garantiert in der Nutzfläche.
            int y = btn_Speichern.Top + (btn_Speichern.Height - lblStatus.Height) / 2;
            lblStatus.Location = new Point(lblStatus.Left, y);

            // Vor alle Geschwister holen: Beim Verkleinern des Fensters wandert die
            // untenverankerte Zeile über die Übersichtsgruppe.
            lblStatus.BringToFront();

            // MindestgroesseFestlegen() läuft NICHT hier, sondern in OnShown -
            // Begründung dort.
        }

        /// <summary>
        /// Setzt die Mindestgröße, sobald das Fenster steht.
        ///
        /// <b>Warum nicht im Konstruktor (Paket-9-Nacharbeit).</b>
        /// <see cref="MindestgroesseFestlegen"/> deckelt die Mindestgröße auf die
        /// Arbeitsfläche des Bildschirms und ruft dafür <c>Screen.FromControl(this)</c>
        /// auf. Im Konstruktor hat das zwei Nachteile: Der Aufruf erzwingt die
        /// Fensterhandle, bevor der Aufbau fertig ist, und er misst den falschen
        /// Bildschirm — das Formular steht dort noch an seiner Entwurfsposition,
        /// <c>StartPosition</c> (CenterParent/CenterScreen) wirkt erst beim Anzeigen.
        /// Auf einem Mehrschirmplatz kam so die Arbeitsfläche des Primärbildschirms
        /// heraus, auch wenn der Dialog auf dem zweiten Bildschirm aufging.
        ///
        /// In <c>OnShown</c> ist das Fenster positioniert; <c>Screen.FromControl</c>
        /// liefert den Bildschirm, auf dem der Dialog tatsächlich liegt. Die Ausrichtung
        /// der Statuszeile bleibt im Konstruktor — sie muss vor dem ersten Zeichnen
        /// stimmen und braucht keinen Bildschirmbezug.
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            MindestgroesseFestlegen();
        }

        /// <summary>
        /// Legt die Mindestgröße des Dialogs auf die Größe fest, die der fertig
        /// aufgebaute Inhalt braucht — begrenzt auf die Arbeitsfläche des Bildschirms.
        ///
        /// <b>Warum.</b> Der Dialog ist <c>Sizable</c>, hat aber im Gegensatz zur
        /// <see cref="BaseForm"/> <b>keinen Scrollbereich</b> (<c>AutoScroll = false</c>,
        /// im Harness gemessen). Wird er kleiner gezogen, verschwindet die Knopfzeile
        /// samt Statusmeldung nach unten aus der Nutzfläche, und die untenverankerte
        /// Statuszeile wandert über <c>groupBox_Uebersicht</c>. Beides ist gemessen:
        /// bei 380 px Nutzhöhe lag <c>lblStatus</c> vollständig hinter der
        /// Übersichtsgruppe.
        ///
        /// Dieselbe Vorgehensweise wie in <c>BaseForm.OnLoad</c> („automatische
        /// Mindestgröße"), nur ohne den dortigen Scrollbereich. Die Deckelung auf die
        /// Arbeitsfläche verhindert, dass der Dialog auf einem kleinen Bildschirm größer
        /// als dieser wird und sich dann nicht mehr verkleinern lässt.
        /// </summary>
        private void MindestgroesseFestlegen()
        {
            if (this.MinimumSize.Width != 0 || this.MinimumSize.Height != 0) return;

            Size noetig = this.Size;
            Rectangle flaeche = Screen.FromControl(this).WorkingArea;

            this.MinimumSize = new Size(Math.Min(noetig.Width, flaeche.Width),
                                        Math.Min(noetig.Height, flaeche.Height));
        }

        private void ShowStatus(string message, Color color)
        {
            lblStatus.Text = message;
            lblStatus.ForeColor = color;
            lblStatus.Visible = true;

            statusTimer.Stop(); // Falls er noch lief, zurücksetzen
            statusTimer.Start();
        }
    }
}
