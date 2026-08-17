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
        private ComboBox comboBox;
        private int index = -1;
        private List<string> listErzeuger = new List<string>();
        private List<string> listPufferSp = new List<string>();

        // D1: Die vier programmatischen Steuerelemente der Alt-Rubrik "Pufferspeicher"
        // (zwei Dropdowns, zwei Checkboxen) samt Rückkopplungssperre sind entfallen -
        // siehe AltRubrikStilllegen(). Sie waren seit Paket 2 unsichtbar und wurden nur
        // noch befüllt.

        // Vollständiger Datenbestand der Pufferspeicher-Zuordnungen
        // (Erzeuger als DB-WERT, Pufferspeicher, Vorlauf, Rücklauf). listView1 zeigt
        // davon nur die per Checkbox ausgewählten Pufferspeicher an - gespeichert
        // wird immer der komplette Bestand.
        //
        // PAKET 9 / L4: Feld 0 trägt jetzt den DB-Wert (DbWerte.ERZEUGER_*) und nicht
        // mehr den lokalisierten Anzeigenamen. Damit ist die gesamte Steuerlogik
        // dieses Formulars sprachfrei; übersetzt wird ausschließlich beim Füllen der
        // ListView (ErzeugerKatalog.Anzeige) und beim Zurücklesen einer Eingabe
        // (ErzeugerKatalog.DbWert). Das ist B0-11 zu Ende gedacht: Vorher hing der
        // Umweg Anzeige→DB am Speicherzeitpunkt und griff nur, solange die Sprache
        // zwischen Anlegen und Speichern gleich blieb.
        private List<string[]> _zuordnungen = new List<string[]>();

        // Mouseover-Hinweise in der Pufferspeicher-Zuordnung
        private ToolTip _zuordnungTip = new ToolTip();
        private ListViewItem _tipItemZuordnung = null;
        private int _tipSpalteZuordnung = -1;
        private Timer statusTimer = new Timer();

        // LanguageItem liegt seit Paket 9 / L4 in ErzeugerKatalog.cs - dort steht die
        // EINE Zuordnung DB-Wert ↔ Anzeigename, die vorher viermal im Quelltext stand.
        private readonly List<LanguageItem> _waermeerzeugerItems;

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

            listView1.FullRowSelect = true;
            listView1.GridLines = true;
            listView1.View = View.Details;
            listView1.Columns.Add(MyResource.Resource.PSP_SPALTE_WAERMEERZEUGER, -2, HorizontalAlignment.Left);
            listView1.Columns.Add(MyResource.Resource.SIMQ_TYP_PUFFERSPEICHER, -2, HorizontalAlignment.Left);
            listView1.Columns.Add(MyResource.Resource.PSP_SPALTE_VORLAUF, -2, HorizontalAlignment.Left);
            listView1.Columns.Add(MyResource.Resource.PSP_SPALTE_RUECKLAUF, -2, HorizontalAlignment.Left);
            listView1.Columns.Add("", -2, HorizontalAlignment.Left);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            // Handle double-click for editing
            listView1.MouseDoubleClick += ListView_MouseDoubleClick;

            // Mouseover-Hinweise für die Zuordnungstabelle
            _zuordnungTip.AutoPopDelay = 15000;
            _zuordnungTip.InitialDelay = 400;
            _zuordnungTip.ReshowDelay = 100;
            listView1.MouseMove += listView1_MouseMove;
            listView1.MouseLeave += (s, e) => { _tipItemZuordnung = null; _tipSpalteZuordnung = -1; _zuordnungTip.Hide(listView1); };

            // Initialize ComboBox (hidden by default) für die Bearbeitung der "Pufferspeicher"-Spalte  
            comboBox = new ComboBox
            {
                Visible = false,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // EINE Quelle für alle Erzeugerlisten (Paket 9 / L4, ErzeugerKatalog).
            _waermeerzeugerItems = ErzeugerKatalog.Liste(ErzeugerKatalog.WAERMEERZEUGER);

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

        /// <summary>
        /// Mouseover-Hinweise in der Tabelle "Pufferspeicher Zuordnung":
        /// erklärt die per Doppelklick bearbeitbaren Spalten.
        /// </summary>
        private void listView1_MouseMove(object sender, MouseEventArgs e)
        {
            ListViewHitTestInfo hit = listView1.HitTest(e.Location);
            if (hit.Item == null)
            {
                if (_tipItemZuordnung != null)
                {
                    _tipItemZuordnung = null; _tipSpalteZuordnung = -1;
                    _zuordnungTip.Hide(listView1);
                }
                return;
            }

            int spalte = hit.SubItem != null ? hit.Item.SubItems.IndexOf(hit.SubItem) : -1;
            if (_tipItemZuordnung == hit.Item && _tipSpalteZuordnung == spalte) return;
            _tipItemZuordnung = hit.Item;
            _tipSpalteZuordnung = spalte;

            string text;
            switch (spalte)
            {
                case 0:
                    text = MyResource.Resource.PSP_TIP_ZUORDNUNG_ERZEUGER;
                    break;

                case 1:
                    text = MyResource.Resource.PSP_TIP_ZUORDNUNG_SPEICHER;
                    break;

                case 2:
                    text = MyResource.Resource.PSP_TIP_ZUORDNUNG_VORLAUF;
                    break;

                case 3:
                    text = MyResource.Resource.PSP_TIP_ZUORDNUNG_RUECKLAUF;
                    break;

                case 4:
                    text = MyResource.Resource.PSP_TIP_ZUORDNUNG_STAMMDATEN;
                    break;

                default:
                    text = MyResource.Resource.PSP_TIP_ZUORDNUNG_STANDARD;
                    break;
            }

            _zuordnungTip.Show(text, listView1, e.X + 16, e.Y + 18, 15000);
        }

        /// <summary>
        /// Einstellung der Speicherregelung (Hysterese) für den Pufferspeicher
        /// der Wärmepumpe: Ein- und Abschaltschwelle in Prozent der nutzbaren
        /// Kapazität. Gespeichert je Zuordnung in Z_ProjektPufferSp.
        ///
        /// <b>D1: derzeit ohne Aufrufer.</b> Der Einstieg war der Doppelklick auf die
        /// entfallene Spalte „Zuordnung (alt)". Der Dialog bleibt bewusst stehen
        /// (Konzeptvorgabe): Er ist der einzige Weg zu den Schwellen der ALT-Zuordnung,
        /// und die Brücke, die diese Zeilen schreibt, ist bis zur Abnahme unangetastet.
        /// Für den Anwender sind die Schwellen längst am Puffer selbst pflegbar
        /// (<c>Form_PufferSp_Projekt</c>) — dort gehören sie hin, und nur dort liest die
        /// Engine sie seit Paket 4.
        /// </summary>
        private void SpeicherregelungBearbeiten()
        {
            // Zuordnung der Wärmepumpe suchen (höchste Priorität)
            Z_ProjektPufferSpCtrl ctrlpsp = new Z_ProjektPufferSpCtrl();
            ctrlpsp.ReadAll("ID_Projekt=" + m_ID_Projekt +
                            " AND Erzeuger='" + DbWerte.ERZEUGER_WAERMEPUMPE + "'");
            if (ctrlpsp.rows == 0)
            {
                MessageBox.Show(MyResource.Resource.PSP_MSG_WP_OHNE_SPEICHER,
                    MyResource.Resource.PSP_TITEL_SPEICHERREGELUNG,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int idZuordnung = ctrlpsp.items[0].ID;
            string speicherName = ctrlpsp.items[0].PufferSp;

            // Gespeicherte Schwellen lesen (Vorgabe 10 % / 95 %)
            double vorgabeEin = 10, vorgabeAus = 95;
            object sEin = WaermequelleClass.WertLesenStill("Z_ProjektPufferSp", "Schwelle_Ein", idZuordnung);
            object sAus = WaermequelleClass.WertLesenStill("Z_ProjektPufferSp", "Schwelle_Aus", idZuordnung);
            if (sEin != null && Convert.ToDouble(sEin) > 0) vorgabeEin = Convert.ToDouble(sEin);
            if (sAus != null && Convert.ToDouble(sAus) > 0) vorgabeAus = Convert.ToDouble(sAus);

            Form frm = new Form();
            frm.Text = string.Format(MyResource.Resource.PSP_SPEICHERREGELUNG_FENSTERTITEL, speicherName);
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.MinimizeBox = false;
            frm.MaximizeBox = false;
            frm.ClientSize = new Size(430, 250);

            Label kopf = new Label
            {
                Text = MyResource.Resource.PSP_SPEICHERREGELUNG_KOPF,
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(14, 14)
            };

            Label l1 = new Label { Text = MyResource.Resource.PSP_SPEICHERREGELUNG_EINSCHALT, AutoSize = true, Location = new Point(24, 52) };
            Label l2 = new Label { Text = MyResource.Resource.PSP_SPEICHERREGELUNG_ABSCHALT, AutoSize = true, Location = new Point(24, 88) };

            // Feste Pixel-Geometrie (Konzept 13.6): Eingabespalte hinter die breitere
            // der beiden Beschriftungen. Auf Deutsch bleibt es bei den bisherigen 280 px.
            int xSchwelle = Math.Max(l1.Right, l2.Right) + 12;
            if (xSchwelle < 280) xSchwelle = 280;
            if (xSchwelle > 340) xSchwelle = 340;

            TextBox tbEin = new TextBox { Location = new Point(xSchwelle, 49), Width = 70, Text = vorgabeEin.ToString("0.#") };
            TextBox tbAus = new TextBox { Location = new Point(xSchwelle, 85), Width = 70, Text = vorgabeAus.ToString("0.#") };

            Label hinweis = new Label
            {
                AutoSize = false,
                Location = new Point(14, 124),
                Size = new Size(400, 80),
                Text = MyResource.Resource.PSP_SPEICHERREGELUNG_HINWEIS
            };

            Button ok = new Button { Text = MyResource.Resource.SIM_BTN_OK, DialogResult = DialogResult.OK, Location = new Point(242, 210), Width = 85 };
            Button abbruch = new Button { Text = MyResource.Resource.SIM_BTN_ABBRECHEN, DialogResult = DialogResult.Cancel, Location = new Point(333, 210), Width = 85 };

            frm.Controls.Add(kopf);
            frm.Controls.Add(l1);
            frm.Controls.Add(tbEin);
            frm.Controls.Add(l2);
            frm.Controls.Add(tbAus);
            frm.Controls.Add(hinweis);
            frm.Controls.Add(ok);
            frm.Controls.Add(abbruch);
            frm.AcceptButton = ok;
            frm.CancelButton = abbruch;

            while (frm.ShowDialog(this) == DialogResult.OK)
            {
                float ein, aus;
                if (!WaermequelleClass.ZahlParsen(tbEin.Text, out ein) ||
                    !WaermequelleClass.ZahlParsen(tbAus.Text, out aus))
                {
                    MessageBox.Show(MyResource.Resource.PSP_MSG_ZAHLENWERTE,
                        MyResource.Resource.PSP_TITEL_SPEICHERREGELUNG,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }
                if (ein < 0 || ein > 100 || aus <= 0 || aus > 100 || ein >= aus)
                {
                    MessageBox.Show(MyResource.Resource.PSP_MSG_SCHWELLEN_BEREICH,
                        MyResource.Resource.PSP_TITEL_SPEICHERREGELUNG,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    continue;
                }

                DataRepository.ExecuteSQL(
                    "UPDATE Z_ProjektPufferSp SET Schwelle_Ein=?, Schwelle_Aus=? WHERE ID=" + idZuordnung,
                    new System.Data.OleDb.OleDbParameter("@ein", (double)ein),
                    new System.Data.OleDb.OleDbParameter("@aus", (double)aus));

                ShowStatus(string.Format(MyResource.Resource.PSP_STATUS_SPEICHERREGELUNG_GESPEICHERT,
                                         ein.ToString("0.#"), aus.ToString("0.#")), Color.ForestGreen);
                return;
            }
        }

        /// <summary>
        /// Liefert die dem Erzeuger zugeordneten Pufferspeicher aus der
        /// Zuordnungstabelle (kommagetrennt) oder "-" ohne Zuordnung.
        ///
        /// Der Parameter ist seit Paket 9 / L4 der <b>DB-Wert</b> des Erzeugers
        /// (<see cref="DbWerte"/>) und nicht mehr sein Anzeigename — die Auswahl läuft
        /// damit sprachfrei.
        ///
        /// <b>D1: derzeit ohne Aufrufer</b> — die Spalte „Zuordnung (alt)" der Übersicht
        /// war die einzige Anzeige dieser Auskunft. Die Methode bleibt stehen, solange
        /// <c>_zuordnungen</c> und die Spiegel-Brücke bestehen; sie ist die
        /// Leseschnittstelle darauf, wenn für die Abnahme noch einmal jemand
        /// hineinschauen muss.
        /// </summary>
        private string ZugeordnetePufferSp(string erzeugerDbWert)
        {
            // Aus dem kompletten Datenbestand lesen.
            List<string> speicher = new List<string>();
            foreach (string[] z in _zuordnungen)
            {
                if (z[0] == erzeugerDbWert && !string.IsNullOrEmpty(z[1]) && !speicher.Contains(z[1]))
                    speicher.Add(z[1]);
            }
            return speicher.Count > 0 ? string.Join(", ", speicher) : "-";
        }

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

        /// <summary>
        /// Frischt die Anzeige nach einer Änderung am Zuordnungsbestand auf.
        ///
        /// <b>D1:</b> Die unsichtbare Alt-Tabelle <c>listView1</c> wird nicht mehr
        /// befüllt — sie war die Anzeige der stillgelegten Rubrik. <c>_zuordnungen</c>
        /// selbst bleibt vollständig erhalten: geladen wird über
        /// <see cref="ZuordnungenLaden"/>, geschrieben über
        /// <c>btn_Speichern_Click</c>, und die Spiegel-Brücke
        /// <c>WaermesenkeClass.WpSenkeSpiegeln</c> arbeitet unverändert weiter
        /// (Konzeptvorgabe: bis zur Abnahme unangetastet).
        ///
        /// Was bleibt, ist der eine Zweck, den die Methode für den sichtbaren Dialog
        /// hat: die Erzeuger-Übersicht neu aufbauen. Der Methodenname bleibt, weil ihn
        /// mehrere Aufrufstellen führen; D2 löst ihn mit der Alt-Tabelle zusammen ab.
        /// </summary>
        private void RefreshZuordnungAnzeige()
        {
            AktualisiereErzeugerUebersicht();
        }

        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            ListViewItem item = listView1.SelectedItems[0];
            item.SubItems[1].Text = comboBox.SelectedItem.ToString();
            comboBox.Visible = false;

            // Änderung in den Datenbestand übernehmen
            if (item.Tag is int idx && idx >= 0 && idx < _zuordnungen.Count)
                _zuordnungen[idx][1] = item.SubItems[1].Text;

            AktualisiereErzeugerUebersicht();
        }

        private void ListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            ListViewItem item = listView1.SelectedItems[0];
            ListViewHitTestInfo hit = listView1.HitTest(e.Location);
            int subItemIndex = hit.Item.SubItems.IndexOf(hit.SubItem);
            index = subItemIndex;

            // Die Bounds des SubItems relativ zum ListView holen
            Rectangle subItemBounds = hit.SubItem.Bounds;

            // Umrechnung der Position: Absolut zur Form, egal in welcher GroupBox das ListView liegt
            Point screenPoint = listView1.PointToScreen(subItemBounds.Location);
            Point formPoint = this.PointToClient(screenPoint);
            Rectangle displayBounds = new Rectangle(formPoint, subItemBounds.Size);

            if (subItemIndex == 1) // Spalte "Pufferspeicher"
            {
                // WICHTIG: Alte Events entfernen, um Mehrfach-Aufrufe zu verhindern
                comboBox.SelectedIndexChanged -= ComboBox_SelectedIndexChanged;
                comboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;

                // Sicherstellen, dass die ComboBox auf der Form liegt
                if (!this.Controls.Contains(comboBox)) this.Controls.Add(comboBox);

                comboBox.Bounds = displayBounds;
                comboBox.Text = item.SubItems[subItemIndex].Text;
                comboBox.Visible = true;
                comboBox.BringToFront();
                comboBox.Focus();
                comboBox.DroppedDown = true; // Öffnet die Liste sofort beim Doppelklick
            }
            else if (subItemIndex == 2 || subItemIndex == 3) // "Vorlauf" oder "Rücklauf"
            {
                string alterWert = item.SubItems[subItemIndex].Text;
                TextBox textBox = new TextBox { Bounds = displayBounds, Text = alterWert };

                // Verhindert die Eingabe von Buchstaben
                textBox.KeyPress += (s, ev) => {
                    if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar))
                    {
                        ev.Handled = true;
                    }
                };

                // Event beim Verlassen der TextBox
                textBox.LostFocus += (s, ev) =>
                {
                    string neuerText = textBox.Text;

                    // B4-2: Die Eingabe läuft jetzt über dieselbe Prüfung wie überall
                    // sonst (ProjektPuffer.TemperaturenPruefen, siehe
                    // Form_KonfigPufferspeicher und Wizard_WPItem). Geprüft wird das
                    // PAAR und nicht die einzelne Zelle - erst Vorlauf UND Rücklauf
                    // ergeben eine Spreizung. Die Gegenzelle steht schon im ListView.
                    int gegenSpalte = (subItemIndex == 2) ? 3 : 2;
                    string gegenText = item.SubItems[gegenSpalte].Text;
                    string vorlaufText = (subItemIndex == 2) ? neuerText : gegenText;
                    string ruecklaufText = (subItemIndex == 2) ? gegenText : neuerText;

                    string fehler;
                    if (!TemperaturPaarPruefen(vorlaufText, ruecklaufText, out fehler))
                    {
                        MessageBox.Show(fehler, MyResource.Resource.PSP_TITEL_TEMPERATUR_PRUEFEN,
                                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        neuerText = alterWert;   // Zelle auf den letzten gültigen Stand zurück
                    }

                    item.SubItems[subItemIndex].Text = neuerText;

                    // Änderung in den Datenbestand übernehmen (Spalte 2=Vorlauf, 3=Rücklauf)
                    if (item.Tag is int idxZ && idxZ >= 0 && idxZ < _zuordnungen.Count)
                        _zuordnungen[idxZ][subItemIndex] = neuerText;

                    textBox.Dispose();
                };

                // Enter-Taste zum Bestätigen unterstützen
                textBox.KeyDown += (s, ev) =>
                {
                    if (ev.KeyCode == Keys.Enter)
                    {
                        listView1.Focus(); // Löst LostFocus aus
                        ev.SuppressKeyPress = true;
                    }
                };

                this.Controls.Add(textBox);
                textBox.BringToFront();
                textBox.Focus();
            }
            else if (subItemIndex == 4) // Spalte mit dem "📂" Symbol
            {
                Form_PufferSp_Admin frm = new Form_PufferSp_Admin();
                frm.m_bReadOnly = true;
                frm.ShowDialog();
            }
        }

        /// <summary>
        /// Prüft das Temperaturpaar einer Zuordnungszeile beim Verlassen einer Zelle
        /// (B4-2). Grundlage ist <see cref="ProjektPuffer.TemperaturenPruefen"/> — eine
        /// Stelle für alle Temperatureingaben, ohne Untergrenze: 35/28 ist gültig.
        ///
        /// Zwei Zustände gelten ausdrücklich als in Ordnung, obwohl
        /// <c>TemperaturenPruefen</c> sie ablehnen würde:
        ///
        ///   - **beide Zellen leer oder 0** — das ist die RÜCKNAHME einer Vorgabe
        ///     (B4-3). Beim Speichern werden Vorlauf/Ruecklauf am Puffer dann auf NULL
        ///     gesetzt, und die Engine fällt geordnet zurück.
        ///   - **genau eine Zelle gefüllt** — der unvermeidliche Zwischenstand während
        ///     der Eingabe. Wer die erste von zwei Zellen füllt, darf dabei nicht mit
        ///     einer Meldung unterbrochen werden. Ein halbes Paar wird ohnehin nirgends
        ///     an den Puffer geschrieben.
        ///
        /// Abgefangen wird damit genau das, was schaden würde: ein VOLLSTÄNDIGES, aber
        /// unbrauchbares Paar (vertauscht, Spreizung 0, über 110 °C).
        /// </summary>
        private static bool TemperaturPaarPruefen(string vorlaufText, string ruecklaufText, out string fehler)
        {
            fehler = null;

            bool vorlaufLeer = IstLeerwert(vorlaufText);
            bool ruecklaufLeer = IstLeerwert(ruecklaufText);
            if (vorlaufLeer || ruecklaufLeer) return true;

            int vorlauf, ruecklauf;
            return ProjektPuffer.TemperaturenPruefen(vorlaufText, ruecklaufText,
                                                     out vorlauf, out ruecklauf, out fehler);
        }

        /// <summary>
        /// "Nicht gepflegt": leere Zelle oder die 0. Beides bedeutet in diesen Spalten
        /// dasselbe — der Zelleditor lässt kein Minus zu, und 0 °C Rücklauf ist auch in
        /// der Datenbank der Wert für "nichts eingetragen".
        /// </summary>
        private static bool IstLeerwert(string text)
        {
            text = (text ?? "").Trim();
            if (text.Length == 0) return true;

            int zahl;
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out zahl) && zahl == 0;
        }

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
            
            // Rückgabewert wird seit D1 nicht mehr gebraucht (er belieferte die
            // Vorbelegung der entfallenen Alt-Rubrik); _zuordnungen wird weiter gefüllt.
            ZuordnungenLaden();

            // Auswahl aus den STAMM-Daten füllen (eindeutige Bezeichner) - die
            // Projekt-Tabelle enthält Kopien aller Projekte und erzeugte Duplikate
            // in der Dropdown-Liste. Beim Speichern wird der gewählte Speicher bei
            // Bedarf automatisch aus den Stammdaten ins Projekt kopiert.
            RecordSet rsPsp = new RecordSet();
            rsPsp.Open("SELECT DISTINCT Bezeichner FROM " + PufferSpStammCtrl.TABLE + " ORDER BY Bezeichner");
            while (rsPsp.Next()) listPufferSp.Add(rsPsp.Read("Bezeichner").ToString());
            rsPsp.Close();
            comboBox.Items.AddRange(listPufferSp.ToArray());

            // D1: Das Befüllen und Vorbelegen der Alt-Rubrik-Dropdowns ist entfallen -
            // die Steuerelemente gibt es nicht mehr.

            // Beide Kartenspalten mit den geladenen Daten aufbauen. D3: Der frühere
            // Zusatzaufruf AktualisierePufferFusszeile entfällt - die Speicherspalte
            // hängt an derselben Auffrischung.
            RefreshZuordnungAnzeige();

            // Feature-Flag der zweikanaligen Kaskade aus der Datenbank vorbelegen
            // (Paket 4, Etappe 4a - Konzept Kapitel 9)
            AktualisiereKaskadeSchalter();

            // Einstellung Extrapolation_erlaubt vorbelegen (Paket 8 - Konzept 13.4)
            AktualisiereExtrapolationSchalter();
        }

        /// <summary>
        /// Lädt den kompletten Zuordnungsbestand des Projekts aus
        /// <c>Z_ProjektPufferSp</c> nach <c>_zuordnungen</c> (Anzeigename, Speicher,
        /// Vorlauf, Rücklauf).
        ///
        /// Aus <c>SetControls</c> herausgelöst, weil Paket 2 einen ZWEITEN Aufrufer hat:
        /// Nach jeder Senkenänderung an einer Wärmepumpe spiegelt
        /// <see cref="WaermesenkeClass.WpSenkeSpiegeln"/> das neue Modell auf die
        /// Alt-Zuordnung (Übergangsbrücke, Konzept 4.4/Etappe A). Ohne das erneute Laden
        /// stünde in <c>_zuordnungen</c> weiter der alte Stand - und das nächste
        /// "Speichern" (Delete/Insert-Zyklus) würde die gerade erzeugte Zeile wieder
        /// wegschreiben.
        /// </summary>
        private Z_ProjektPufferSpCtrl ZuordnungenLaden()
        {
            Z_ProjektPufferSpCtrl ctrlpsp = new Z_ProjektPufferSpCtrl();
            ctrlpsp.ReadAll("ID_Projekt= " + m_ID_Projekt);

            _zuordnungen.Clear();
            for (int i = 0; i < ctrlpsp.rows; i++)
            {
                // Paket 9 / L4: Der Erzeuger wird UNVERÄNDERT als DB-Wert übernommen.
                // Vorher wurde er hier in den Anzeigenamen übersetzt und beim Speichern
                // wieder zurück — ein Hin und Her, das nur auf deutscher Oberfläche
                // zuverlässig war (B0-11). Übersetzt wird jetzt erst beim Anzeigen.
                _zuordnungen.Add(new[] {
                    ctrlpsp.items[i].Erzeuger,
                    ctrlpsp.items[i].PufferSp,
                    ctrlpsp.items[i].Vorlauf.ToString(),
                    ctrlpsp.items[i].Ruecklauf.ToString() });
            }

            return ctrlpsp;
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
            Z_ProjektPufferSpCtrl ctrlpsp = new Z_ProjektPufferSpCtrl();

            Konfiguration.m_Tool_1 = checkBox1.Checked ? GetDbValue(comboBox1) : "";
            Konfiguration.m_Tool_2 = checkBox2.Checked ? GetDbValue(comboBox2) : "";
            Konfiguration.m_Tool_3 = checkBox3.Checked ? GetDbValue(comboBox3) : "";
            Konfiguration.m_Tool_4 = checkBox4.Checked ? GetDbValue(comboBox4) : "";
            Konfiguration.m_Tool_5 = checkBox5.Checked ? GetDbValue(comboBox5) : "";
            Konfiguration.m_Tool_6 = checkBox6.Checked ? GetDbValue(comboBox6) : "";

            // ABNAHMEBEFUND 3: Das Feature-Flag der zweikanaligen Kaskade steht NICHT in
            // der Spaltenliste von KonfigurationCtrl.Insert (Begründung dort und bei
            // KaskadeZweikanaligSchreiben: Die Liste hängt an der Ordinalkette von
            // ReadSingle). Der Delete/Insert-Zyklus unten löschte das Flag deshalb bei
            // JEDEM Speichern still weg - die neue Zeile bekam von Access die
            // YESNO-Vorbelegung False. Auffällig wurde es erst an der Folge:
            // KaskadeAutomatikBeimSpeichern las danach „aus" und fragte, ob eingeschaltet
            // werden soll, während der Haken der Fußzeile weiter gesetzt dastand.
            // Schwerer wog das Stille daran - der nächste Lauf rechnete wieder einkanalig.
            //
            // Gerettet wird der GESPEICHERTE Stand und nicht der des Hakens: Der Schalter
            // schreibt sofort (checkBox_KaskadeZweikanalig_CheckedChanged), beide Werte
            // sind also deckungsgleich; wo sie es nicht sind, ist die Datenbank die
            // Wahrheit. Dasselbe Muster, mit dem Insert seit Paket 8 die Einstellung
            // Extrapolation_erlaubt nachzieht.
            bool kaskadeZweikanalig = KonfigurationCtrl.KaskadeZweikanaligLesen(m_ID_Projekt);

            // FRAGE 23 (Nachtrag zu Abnahmebefund 3): Gleiche Fehlerklasse, UMGEKEHRTE
            // Vorbelegung. Auch Extrapolation_erlaubt steht nicht in der Spaltenliste
            // von Insert - dort zieht ein stilles UPDATE die Vorbelegung WAHR nach.
            // Für NEUE Projekte ist das richtig (Paket 8); beim Wiederspeichern eines
            // bestehenden Projekts überschrieb es die bewusste Abwahl des Anwenders
            // (checkBox_Extrapolation schreibt sofort, die Datenbank ist die Wahrheit),
            // und der nächste Lauf extrapolierte wieder still.
            //
            // Der Lesezugriff VOR dem Delete unterscheidet beide Fälle: Bei fehlender
            // Zeile (neues Projekt), fehlender Spalte, NULL oder Schemastand < 7
            // liefert ExtrapolationErlaubtLesen die Vorbelegung WAHR - dann bleibt
            // unten alles beim Insert-Stand, und "NULL heißt Datenlücke" (Befund N8)
            // kippt nicht.
            bool extrapolationErlaubt = KonfigurationCtrl.ExtrapolationErlaubtLesen(m_ID_Projekt);

            ctrl.model = Konfiguration;
            if (!ctrl.Delete(m_ID_Projekt)) return;
            if (ctrl.Insert(m_ID_Projekt))
                ShowStatus(MyResource.Resource.SIM_STATUS_KONFIG_GESPEICHERT, Color.ForestGreen);

            // Nur wenn er AN war: Ein blindes Schreiben von false wäre auf einer
            // Datenbank ohne Schemastand 6 eine Fehlermeldung ohne Anlass.
            if (kaskadeZweikanalig)
                KonfigurationCtrl.KaskadeZweikanaligSchreiben(m_ID_Projekt, true);

            // Spiegelbildlich nur die ABWAHL zurückschreiben - WAHR hat Insert gerade
            // selbst nachgezogen. Ein FALSE aus ExtrapolationErlaubtLesen setzt
            // Schemastand 7 voraus, das UPDATE trifft also nie eine Datenbank ohne
            // die Spalte.
            if (!extrapolationErlaubt)
                KonfigurationCtrl.ExtrapolationErlaubtSchreiben(m_ID_Projekt, false);

            int prioritaet = 1;

            ctrlpsp.ID_Projekt = m_ID_Projekt;

            // B0-1: Die Schwellen der Speicherregelung hängen an der Zuordnungszeile und
            // überleben den Delete/Insert-Zyklus nicht (stiller Rückfall auf 10/95 %).
            // Vor dem Löschen sichern; Schlüssel: Erzeuger (DB-Wert) + Pufferspeicher.
            var alteSchwellen = new Dictionary<string, double?[]>();
            ctrlpsp.ReadAll("ID_Projekt=" + m_ID_Projekt);
            for (int i = 0; i < ctrlpsp.rows; i++)
            {
                var alt = ctrlpsp.items[i];
                alteSchwellen[alt.Erzeuger + "|" + alt.PufferSp] =
                    new double?[] { alt.Schwelle_Ein, alt.Schwelle_Aus };
            }

            if (!ctrlpsp.Delete()) return;

            // WICHTIG: Gespeichert wird der komplette Datenbestand - nicht nur die
            // aktuell (per Pufferspeicher-Checkbox gefiltert) angezeigten Zeilen!
            int fehlgeschlagen = 0;

            // B4-1: An den PUFFER schreibt nur die eine Zeile, die die Engine auch
            // auswertet. SimulationControl.Do_Simulation überspringt jede Zuordnung mit
            // einem anderen Erzeuger (continue) und bricht nach dem ersten
            // Wärmepumpen-Treffer ab (break) - die Reihenfolge ist ORDER BY Prioritaet.
            // Genau diese Zeile wird hier bestimmt: die Priorität vergibt die Schleife
            // unten fortlaufend in Listenreihenfolge (prioritaet++), also gewinnt die
            // ERSTE Wärmepumpen-Zeile der Liste. Sie bekommt zugleich die kleinste ID,
            // womit auch der Gleichstandsfall der Migration (R1: ORDER BY Prioritaet, ID)
            // dieselbe Zeile wählt.
            //
            // Alles andere - BHKW-, Kessel-, Solarthermie- und Gesamtsystem-Zeilen sowie
            // jede weitere WP-Zeile - schreibt NICHT an den Puffer. Vorher tat es das:
            // die zuletzt gespeicherte Zeile überschrieb die Betriebstemperaturen des
            // Speichers, obwohl die Engine sie nie gelesen hat. Das hätte die
            // R2-Entscheidung der Migration ausgehebelt (wirkungslose Altzuordnungen
            // bleiben wirkungslos) und über den Vorrang der führenden Ablage sogar
            // ergebniswirksam werden können.
            bool pufferZeileGeschrieben = false;

            for (int i = 0; i < _zuordnungen.Count; i++)
            {
                string[] z = _zuordnungen[i];
                ctrlpsp.PufferSp = z[1];

                // B0-11, Paket 9 / L4: _zuordnungen führt den Erzeuger bereits als
                // DB-Wert. ErzeugerKatalog.DbWert bleibt trotzdem davorgeschaltet — es
                // ist die tolerante Rückabbildung (erst Anzeigename, dann DB-Wert) und
                // fängt Alt- oder Fremdwerte ab, ohne je einen lokalisierten Text in die
                // Datenbank zu lassen. Bei einem DB-Wert ist der Aufruf wirkungslos.
                ctrlpsp.Erzeuger = ErzeugerKatalog.DbWert(z[0]);

                // B0-1: gesicherte Schwellen der Zuordnung wieder mitgeben
                double?[] schwellen;
                if (alteSchwellen.TryGetValue(ctrlpsp.Erzeuger + "|" + ctrlpsp.PufferSp, out schwellen))
                {
                    ctrlpsp.Schwelle_Ein = schwellen[0];
                    ctrlpsp.Schwelle_Aus = schwellen[1];
                }
                else
                {
                    ctrlpsp.Schwelle_Ein = null;
                    ctrlpsp.Schwelle_Aus = null;
                }

                // Konzept 4.6: TryParse statt Int32.Parse. Ein leeres oder unlesbares
                // Feld warf hier bisher eine unbehandelte FormatException — und zwar
                // NACH dem Delete, also mitten im Datenverlust. Unlesbares wird zu 0;
                // die Engine fällt dann auf ihre Vorgabespreizung zurück.
                int vorlauf, ruecklauf;
                if (!Int32.TryParse(z[2], out vorlauf)) vorlauf = 0;
                if (!Int32.TryParse(z[3], out ruecklauf)) ruecklauf = 0;

                ctrlpsp.Vorlauf = vorlauf;
                ctrlpsp.Ruecklauf = ruecklauf;
                ctrlpsp.Prioritaet = prioritaet++;

                bool istWaermepumpe = string.Equals(ctrlpsp.Erzeuger,
                                                    ProjektPuffer.ERZEUGER_WAERMEPUMPE,
                                                    StringComparison.Ordinal);

                // B0-1: Rückgabewert auswerten — nach dem Delete ist ein stiller
                // Insert-Fehlschlag ein Datenverlust und muss sichtbar werden.
                if (!ctrlpsp.Insert()) fehlgeschlagen++;
                else if (istWaermepumpe && !pufferZeileGeschrieben)
                {
                    pufferZeileGeschrieben = true;

                    // Etappe 4: Die Puffer-Zeile ist die FÜHRENDE Ablage der
                    // Betriebstemperaturen (Konzept 5.1) — die Zuordnung wird nur noch
                    // mitgeschrieben, damit Alt-Datenbanken lesbar bleiben. Insert()
                    // hat ID_Pufferspeicher gerade frisch aufgelöst (und die
                    // Projektkopie bei Bedarf angelegt), der Wert zeigt also sicher auf
                    // Tab_Pufferspeicher.
                    if (ProjektPuffer.IstTemperaturpaar(vorlauf, ruecklauf))
                    {
                        // Niedrige Paare wie 35/28 laufen unverändert durch — hier wird
                        // nichts geklemmt.
                        PufferSpCtrl.SetTemperaturen(ctrlpsp.ID_Pufferspeicher, vorlauf, ruecklauf);
                    }
                    else if (vorlauf <= 0 && ruecklauf <= 0)
                    {
                        // B4-3, Rücknahme: Der Anwender hat beide Zellen geleert. Am
                        // Speicher darf dann kein alter Wert stehen bleiben — er wäre
                        // die führende Ablage und verdeckte die Zuordnung dauerhaft.
                        // Mit NULL fällt die Engine geordnet zurück (Zuordnung, sonst
                        // Vorgabe 10 K).
                        PufferSpCtrl.TemperaturenLoeschen(ctrlpsp.ID_Pufferspeicher);
                    }
                    // Halb gefülltes oder vertauschtes Paar: unverändert lassen. Die
                    // Eingabe wird bereits im Zelleditor über
                    // ProjektPuffer.TemperaturenPruefen abgefangen; kommt hier doch so
                    // etwas an (Altbestand in _zuordnungen), ist Nichtstun die sichere
                    // Wahl — es entsteht weder eine Scheinvorgabe noch ein Datenverlust.
                }
            }

            if (fehlgeschlagen > 0)
                ShowStatus(string.Format(MyResource.Resource.PSP_STATUS_ZUORDNUNG_FEHLGESCHLAGEN,
                                         fehlgeschlagen), Color.Firebrick);

            // ZULETZT: Die Notwendigkeitsregel liest Tool_1..Tool_4 (welche Erzeuger
            // überhaupt rechnen) und die Senken-/Quellenfelder. Beides steht erst nach
            // dem Insert oben in der Datenbank - vorher gälte noch die alte Kaskade.
            KaskadeAutomatikBeimSpeichern();
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

        private void btn_Hinzu_Click(object sender, EventArgs e)
        {
            Form_KonfigPufferspeicher frm = new Form_KonfigPufferspeicher();

            // Transformation in eine Liste von Strings (nur die Anzeigenamen)
            List<string> displayListe = listErzeuger
                .Select(dbVal => _waermeerzeugerItems.FirstOrDefault(refItem => refItem.DbValue == dbVal)?.DisplayName)
                .Where(name => name != null) // Falls ein DB-Wert nicht in der Referenzliste war
                .ToList();
            displayListe.Add(MyResource.Resource.KONFIG_GESAMTSYSTEM);

            frm.listErzeuger = displayListe;

            // D1: Der Filter „nur die in der Rubrik aktivierten Pufferspeicher" ist mit
            // der Rubrik entfallen - angeboten wird die komplette Stammdaten-Liste, also
            // genau das, was der Bestand ohne aktive Auswahl schon tat.
            frm.listPufferSp = listPufferSp;
            frm.m_ID_Projekt = m_ID_Projekt;
            frm.SetControls();

            DialogResult result = frm.ShowDialog();
            if (result == DialogResult.OK)
            {
                // Der Zuordnungsdialog arbeitet mit Anzeigenamen (er bekommt sie oben
                // als displayListe). Hier wird zurück auf den DB-Wert abgebildet -
                // _zuordnungen führt seit Paket 9 / L4 ausschließlich DB-Werte.
                _zuordnungen.Add(new[] { ErzeugerKatalog.DbWert(frm.model.Erzeuger), frm.model.PufferSp,
                    frm.model.Vorlauf.ToString(), frm.model.Ruecklauf.ToString() });
                RefreshZuordnungAnzeige();
            }
        }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;

            // Über Tag den Eintrag im Datenbestand entfernen (Anzeige kann gefiltert sein)
            if (listView1.SelectedItems[0].Tag is int idx && idx >= 0 && idx < _zuordnungen.Count)
                _zuordnungen.RemoveAt(idx);

            RefreshZuordnungAnzeige();
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
