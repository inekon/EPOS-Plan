using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Auswahl des Pufferspeichers, der als Wärmequelle einer Wärmepumpe dient
    /// (Sole-Wasser / Wasser-Wasser).
    ///
    /// Der gewählte Speicher liefert in der Simulation die Quellwärme:
    /// Je Stunde entzieht die Wärmepumpe dem Speicher die Verdampferwärme
    /// (Wärmeproduktion - Stromaufnahme). Reicht der Speicherinhalt nicht aus,
    /// wird die Leistung der Wärmepumpe entsprechend begrenzt - der Wärmebedarf
    /// muss also tatsächlich aus dem Speicher gedeckt werden.
    ///
    /// ETAPPE E0 (Konzept_KonfigUI_Hydraulik, Abschnitt 4): Der Dialog listet die
    /// PROJEKT-Pufferspeicher (<see cref="WaermesenkeClass.ProjektPufferListe"/>) —
    /// dieselbe Quelle, aus der die Senkendialoge wählen — und liefert die
    /// Puffer-<b>ID</b> zurück (<see cref="ID_Puffer"/>, Spalte <c>WQ_ID_Puffer</c>).
    /// Der Bezeichner (<see cref="Pufferspeicher"/>, Spalte <c>WQ_Puffer</c>) wird
    /// weiterhin mitgeführt, ist aber nur noch Anzeige- und Alt-Kompatibilität; führend
    /// ist der Fremdschlüssel. Vorher standen hier die STAMM-Speicher, und der Bezeichner
    /// war die einzige Identität — mit den Dubletten der Projektkopien war damit nicht
    /// entscheidbar, welcher Speicher gemeint ist.
    ///
    /// Invariante S-1 (Konzept, Abschnitt 5): Puffer werden ausschließlich an
    /// ERZEUGER-Bezügen angeboten. Dieser Dialog hängt an der Wärmequelle einer Anlage
    /// und erfüllt das; eine Speicher-zu-Speicher-Verbindung entsteht hier nicht.
    ///
    /// Das Formular wird komplett programmatisch aufgebaut (kein Designer/.resx).
    /// </summary>
    public class Form_QuellePufferspeicher : Form
    {
        private ListBox _lbSpeicher;
        private TextBox _tbTemperatur;
        private TextBox _tbSpreizung;
        private TextBox _tbRegeneration;
        private CheckBox _cbUnbegrenzt;
        private Label _lblKapazitaet;
        private Label _lblDaten;
        private Label _lblLeer;
        private Button _btnPufferAnlegen;

        private List<WaermesenkeClass.PufferInfo> _puffer =
            new List<WaermesenkeClass.PufferInfo>();

        /// <summary>
        /// Listeneintrag: trägt den Projekt-Puffer und formatiert ihn für die Anzeige
        /// („{Bezeichner} — {Verwendung}, {Volumen} l, {Vorlauf}/{Rücklauf} °C").
        ///
        /// Eigene Klasse statt <see cref="WaermesenkeClass.PufferInfo.ToString"/>: dort
        /// steht die KURZform der Senkendialoge („Name (500 l)"), die in einer 300 px
        /// breiten Liste zu wenig sagt — Verwendung und Temperaturpaar sind hier die
        /// Entscheidungsgrundlage (kann der Speicher die Quelle sein?).
        /// </summary>
        private sealed class SpeicherItem
        {
            public readonly WaermesenkeClass.PufferInfo Puffer;

            public SpeicherItem(WaermesenkeClass.PufferInfo p) { Puffer = p; }

            public override string ToString()
            {
                string verwendung = WaermesenkeClass.VerwendungAnzeige(
                    WaermesenkeClass.WirksameVerwendung(Puffer));

                // Ohne gepflegtes Temperaturpaar bleibt die kurze Form - „0/0 °C" wäre
                // eine Angabe, die es nicht gibt.
                if (Puffer.Vorlauf <= 0 || Puffer.Ruecklauf <= 0)
                    return string.Format(MyResource.Resource.SIMQ_PUFFER_LISTE_OHNE_TEMP,
                                         Puffer.Bezeichner, verwendung, Puffer.Gesamtvolumen);

                return string.Format(MyResource.Resource.SIMQ_PUFFER_LISTE_EINTRAG,
                                     Puffer.Bezeichner, verwendung, Puffer.Gesamtvolumen,
                                     Puffer.Vorlauf, Puffer.Ruecklauf);
            }
        }

        /// <summary>Name der Wärmepumpe (nur für den Fenstertitel).</summary>
        public string WPName = "";

        /// <summary>Projekt der Anlage — bestimmt die Auswahlliste (E0).</summary>
        public int ID_Projekt;

        /// <summary>
        /// <c>Tab_Pufferspeicher.ID</c> des gewählten Projekt-Puffers — beim Öffnen
        /// Vorbelegung, nach OK das Ergebnis. 0 = keiner. Das ist seit E0 die FÜHRENDE
        /// Identität (Spalte <c>WQ_ID_Puffer</c>).
        /// </summary>
        public int ID_Puffer;

        /// <summary>
        /// Bezeichner des gewählten Pufferspeichers (Spalte <c>WQ_Puffer</c>) — wird
        /// weiter mitgeschrieben, ist aber seit E0 nur noch Anzeige-/Alt-Kompatibilität.
        /// </summary>
        public string Pufferspeicher = "";

        /// <summary>Quelltemperatur des Speichers [°C].</summary>
        public double Quelltemperatur = 10;

        /// <summary>Nutzbare Temperaturspreizung des Speichers [K].</summary>
        public double Spreizung = 5;

        /// <summary>Regeneration/Nachladung der Quelle [kW], 0 = keine.</summary>
        public double Regeneration = 0;

        /// <summary>true = Quelle immer verfügbar (nur die Temperatur wirkt).</summary>
        public bool Unbegrenzt = false;

        public Form_QuellePufferspeicher()
        {
            BaueOberflaeche();
        }

        /// <summary>
        /// Vorbelegung der Eingabefelder als Text. Paket 9 / L3: Statt der früher
        /// hartkodierten Dezimalkomma-Zeichenketten („10,0") wird der ZAHLENWERT
        /// formatiert — mit derselben Kultur, die <see cref="SetControls"/> unmittelbar
        /// danach benutzt (<c>ToString("F1")</c>). Damit steht in der Maske auf jedem
        /// System dieselbe Schreibweise wie in den nachgeladenen Werten; gelesen wird
        /// ohnehin kulturinvariant über <see cref="WaermequelleClass.ZahlParsen"/>
        /// (Komma ODER Punkt). <c>CurrentCulture</c> selbst wird nicht gesetzt
        /// (Konzept 13.6, „Nicht Teil dieses Pakets").
        /// </summary>
        private static string Vorgabe(double wert)
        {
            return wert.ToString("F1", CultureInfo.CurrentCulture);
        }

        private void BaueOberflaeche()
        {
            this.Text = MyResource.Resource.SIMQ_PUFFER_TITEL;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.MaximizeBox = false;
            // E0: 40 px höher als bisher — darunter liegt jetzt die Zeile mit dem
            // Leer-Hinweis und dem Absprung „Pufferspeicher anlegen…" (Muster
            // Form_Waermesenke). Die übrigen Maße bleiben unverändert.
            this.ClientSize = new Size(620, 470);

            Label kopf = new Label
            {
                Text = MyResource.Resource.SIMQ_PUFFER_KOPF,
                AutoSize = true,
                Font = new Font(this.Font, FontStyle.Bold),
                Location = new Point(14, 12)
            };
            this.Controls.Add(kopf);

            _lbSpeicher = new ListBox
            {
                Location = new Point(14, 38),
                Size = new Size(300, 200)
            };
            _lbSpeicher.SelectedIndexChanged += (s, e) => { ZeigeSpeicherDaten(); BerechneKapazitaet(); };
            this.Controls.Add(_lbSpeicher);

            _lblDaten = new Label
            {
                AutoSize = false,
                Location = new Point(330, 38),
                Size = new Size(275, 90),
                Text = ""
            };
            this.Controls.Add(_lblDaten);

            // E0: Leer-Hinweis und Absprung in die Puffer-Verwaltung. Ohne diesen Weg
            // wäre ein Projekt ohne Pufferspeicher eine Sackgasse - vorher stand hier
            // eine Meldung über die STAMM-Daten, die dem Anwender nicht sagte, was zu
            // tun ist (Muster Form_Waermesenke, Konzept 4.2/4.3).
            _lblLeer = new Label
            {
                AutoSize = false,
                Location = new Point(14, 242),
                Size = new Size(300, 34),
                ForeColor = SystemColors.GrayText,
                Text = ""
            };
            this.Controls.Add(_lblLeer);

            _btnPufferAnlegen = new Button
            {
                Text = MyResource.Resource.PSP_BTN_PUFFER_ANLEGEN,
                Location = new Point(330, 244),
                Size = new Size(275, 28)
            };
            _btnPufferAnlegen.Click += btnPufferAnlegen_Click;
            this.Controls.Add(_btnPufferAnlegen);

            // Parameter der Wärmequelle
            GroupBox gb = new GroupBox
            {
                Text = MyResource.Resource.SIMQ_PUFFER_GB_PARAMETER,
                Location = new Point(14, 288),
                Size = new Size(590, 130)
            };
            this.Controls.Add(gb);

            Label l1 = new Label { Text = MyResource.Resource.SIMQ_PUFFER_QUELLTEMPERATUR, AutoSize = true, Location = new Point(16, 30) };
            Label l2 = new Label { Text = MyResource.Resource.SIMQ_PUFFER_SPREIZUNG, AutoSize = true, Location = new Point(16, 62) };
            Label l3 = new Label { Text = MyResource.Resource.SIMQ_PUFFER_REGENERATION, AutoSize = true, Location = new Point(16, 94) };

            // Feste Pixel-Geometrie (Konzept 13.6, Hauptrisiko der programmatischen
            // Dialoge): Die englischen Beschriftungen sind länger als die deutschen.
            // Die Eingabespalte beginnt deshalb erst hinter der breitesten Beschriftung -
            // auf Deutsch bleibt es bei den bisherigen 180 px, weil dort keine
            // Beschriftung so weit reicht. Nach oben gekappt, damit die Felder nicht in
            // die Kapazitätsanzeige (x = 285) laufen.
            int xEingabe = Math.Max(l1.Right, Math.Max(l2.Right, l3.Right)) + 12;
            if (xEingabe < 180) xEingabe = 180;
            if (xEingabe > 200) xEingabe = 200;

            _tbTemperatur = new TextBox { Location = new Point(xEingabe, 27), Width = 80, Text = Vorgabe(Quelltemperatur) };
            _tbTemperatur.TextChanged += (s, e) => BerechneKapazitaet();

            _tbSpreizung = new TextBox { Location = new Point(xEingabe, 59), Width = 80, Text = Vorgabe(Spreizung) };
            _tbSpreizung.TextChanged += (s, e) => BerechneKapazitaet();

            _tbRegeneration = new TextBox { Location = new Point(xEingabe, 91), Width = 80, Text = Vorgabe(Regeneration) };

            _lblKapazitaet = new Label
            {
                AutoSize = false,
                Location = new Point(285, 28),
                Size = new Size(290, 40),
                Text = ""
            };

            _cbUnbegrenzt = new CheckBox
            {
                Text = MyResource.Resource.SIMQ_PUFFER_CB_UNBEGRENZT,
                AutoSize = true,
                Location = new Point(285, 92)
            };

            gb.Controls.Add(l1);
            gb.Controls.Add(_tbTemperatur);
            gb.Controls.Add(l2);
            gb.Controls.Add(_tbSpreizung);
            gb.Controls.Add(l3);
            gb.Controls.Add(_tbRegeneration);
            gb.Controls.Add(_lblKapazitaet);
            gb.Controls.Add(_cbUnbegrenzt);

            Label hinweis = new Label
            {
                AutoSize = false,
                Location = new Point(330, 132),
                Size = new Size(275, 105),
                Text = MyResource.Resource.SIMQ_PUFFER_HINWEIS_QUELLWAERME
            };
            this.Controls.Add(hinweis);

            Button btnOk = new Button
            {
                Text = MyResource.Resource.SIM_BTN_OK,
                DialogResult = DialogResult.OK,
                Location = new Point(this.ClientSize.Width - 190, 432),
                Width = 85
            };
            Button btnAbbruch = new Button
            {
                Text = MyResource.Resource.SIM_BTN_ABBRECHEN,
                DialogResult = DialogResult.Cancel,
                Location = new Point(this.ClientSize.Width - 97, 432),
                Width = 85
            };
            btnOk.Click += btnOk_Click;

            this.Controls.Add(btnOk);
            this.Controls.Add(btnAbbruch);
            this.AcceptButton = btnOk;
            this.CancelButton = btnAbbruch;
        }

        /// <summary>
        /// Füllt die Auswahlliste aus den PROJEKT-Pufferspeichern (E0) und belegt die
        /// Felder mit den gespeicherten Werten vor.
        /// </summary>
        public void SetControls()
        {
            if (!string.IsNullOrEmpty(WPName))
                this.Text = string.Format(MyResource.Resource.SIMQ_PUFFER_TITEL_MIT_WP, WPName);

            PufferListeLaden();

            _tbTemperatur.Text = Quelltemperatur.ToString("F1");
            _tbSpreizung.Text = Spreizung.ToString("F1");
            _tbRegeneration.Text = Regeneration.ToString("F1");
            _cbUnbegrenzt.Checked = Unbegrenzt;

            VorauswahlSetzen();

            ZeigeSpeicherDaten();
            BerechneKapazitaet();
        }

        /// <summary>
        /// Lädt die Projekt-Puffer (ohne Verwendungsfilter: als QUELLE taugt jeder
        /// Speicher des Projekts, die Verwendung steuert nur die Senkenseite) und
        /// schaltet den Leer-Hinweis.
        /// </summary>
        private void PufferListeLaden()
        {
            _puffer = WaermesenkeClass.ProjektPufferListe(ID_Projekt, null);

            _lbSpeicher.Items.Clear();
            foreach (WaermesenkeClass.PufferInfo p in _puffer)
                _lbSpeicher.Items.Add(new SpeicherItem(p));

            bool leer = _lbSpeicher.Items.Count == 0;
            _lblLeer.Text = leer ? MyResource.Resource.SIMQ_PUFFER_HINWEIS_KEIN_PROJEKTPUFFER : "";
            _lbSpeicher.Enabled = !leer;
        }

        /// <summary>
        /// Vorauswahl in dieser Reihenfolge: Fremdschlüssel (führend), sonst Bezeichner
        /// (Altweg — dieselbe Rückfallkette wie in <c>WaermequelleClass.Quellspeicher</c>),
        /// sonst der erste Eintrag.
        /// </summary>
        private void VorauswahlSetzen()
        {
            if (_lbSpeicher.Items.Count == 0) return;

            if (ID_Puffer > 0)
            {
                for (int i = 0; i < _puffer.Count; i++)
                    if (_puffer[i].ID == ID_Puffer) { _lbSpeicher.SelectedIndex = i; return; }
            }

            if (!string.IsNullOrEmpty(Pufferspeicher))
            {
                for (int i = 0; i < _puffer.Count; i++)
                    if (string.Equals(_puffer[i].Bezeichner, Pufferspeicher, StringComparison.OrdinalIgnoreCase))
                    { _lbSpeicher.SelectedIndex = i; return; }
            }

            _lbSpeicher.SelectedIndex = 0;
        }

        /// <summary>Zeigt die Daten des markierten Projekt-Puffers an.</summary>
        private void ZeigeSpeicherDaten()
        {
            WaermesenkeClass.PufferInfo p = AktuellerPuffer();
            if (p == null) { _lblDaten.Text = ""; return; }

            _lblDaten.Text = string.Format(MyResource.Resource.SIMQ_PUFFER_DATEN_PROJEKT,
                WaermesenkeClass.VerwendungAnzeige(WaermesenkeClass.WirksameVerwendung(p)),
                p.Gesamtvolumen,
                p.Bereitschaftsverluste.ToString("0.#"),
                // Einheit und "-" sind Symbole, keine Anzeigetexte (Katalogregel) -
                // "-" wie im Bestand die frühere Feld()-Ersatzausgabe.
                p.Vorlauf > 0 && p.Ruecklauf > 0
                    ? p.Vorlauf + "/" + p.Ruecklauf + " °C"
                    : "-");
        }

        private WaermesenkeClass.PufferInfo AktuellerPuffer()
        {
            SpeicherItem it = _lbSpeicher.SelectedItem as SpeicherItem;
            return it != null ? it.Puffer : null;
        }

        /// <summary>Zeigt die nutzbare Speicherkapazität aus Volumen und Spreizung.</summary>
        private void BerechneKapazitaet()
        {
            WaermesenkeClass.PufferInfo p = AktuellerPuffer();
            float spreizung;
            if (p == null || !WaermequelleClass.ZahlParsen(_tbSpreizung.Text, out spreizung))
            {
                _lblKapazitaet.Text = "";
                return;
            }

            double kapazitaet = p.Gesamtvolumen * 1.16 * spreizung / 1000.0;
            _lblKapazitaet.Text = string.Format(MyResource.Resource.SIMQ_PUFFER_KAPAZITAET,
                kapazitaet.ToString("F1"));
        }

        /// <summary>
        /// Absprung in die Puffer-Verwaltung (Konzept 4.3). Wie in
        /// <c>Form_Waermesenke</c> wird die Liste UNABHÄNGIG vom DialogResult neu
        /// aufgebaut: Die Verwaltung schreibt sofort in die Datenbank, ein über das
        /// Fensterkreuz verlassener Neuanlage-Vorgang bliebe sonst unsichtbar.
        /// </summary>
        private void btnPufferAnlegen_Click(object sender, EventArgs e)
        {
            Form_PufferSp_Projekt frm = new Form_PufferSp_Projekt();
            frm.ID_Projekt = ID_Projekt;
            // Keine Verwendungsvorgabe: die Quellseite legt den Kanal nicht fest.
            frm.Verwendung = null;
            frm.SetControls();
            frm.ShowDialog(this);

            int neu = frm.ID_Puffer;
            PufferListeLaden();
            if (neu > 0) ID_Puffer = neu;
            VorauswahlSetzen();

            ZeigeSpeicherDaten();
            BerechneKapazitaet();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            WaermesenkeClass.PufferInfo gewaehlt = AktuellerPuffer();
            if (gewaehlt == null)
            {
                // Zwei Fälle, zwei Meldungen: gar kein Projekt-Puffer vorhanden (dann
                // hilft nur der Absprung) oder einfach nichts markiert.
                MessageBox.Show(
                    _lbSpeicher.Items.Count == 0
                        ? MyResource.Resource.SIMQ_PUFFER_HINWEIS_KEIN_PROJEKTPUFFER
                        : MyResource.Resource.SIMQ_PUFFER_MSG_AUSWAHL,
                    MyResource.Resource.SIMQ_PUFFER_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            float temp, spreizung, regeneration;
            if (!WaermequelleClass.ZahlParsen(_tbTemperatur.Text, out temp) ||
                !WaermequelleClass.ZahlParsen(_tbSpreizung.Text, out spreizung) ||
                !WaermequelleClass.ZahlParsen(_tbRegeneration.Text, out regeneration))
            {
                MessageBox.Show(MyResource.Resource.PSP_MSG_ZAHLENWERTE,
                    MyResource.Resource.SIMQ_PUFFER_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            if (spreizung <= 0)
            {
                MessageBox.Show(MyResource.Resource.SIMQ_PUFFER_MSG_SPREIZUNG,
                    MyResource.Resource.SIMQ_PUFFER_TITEL, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.None;
                return;
            }

            // E0: Die ID ist das Ergebnis; der Bezeichner geht als Anzeige-/Altwert mit.
            ID_Puffer = gewaehlt.ID;
            Pufferspeicher = gewaehlt.Bezeichner;
            Quelltemperatur = temp;
            Spreizung = spreizung;
            Regeneration = regeneration;
            Unbegrenzt = _cbUnbegrenzt.Checked;
        }
    }
}
