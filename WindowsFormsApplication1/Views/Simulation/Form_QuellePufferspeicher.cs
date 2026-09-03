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
    /// Die Oberfläche steht in <c>Form_QuellePufferspeicher.Designer.cs</c>, weiterhin ohne
    /// eigene <c>.resx</c>: Alle sichtbaren Texte kommen aus <c>MyResource</c> und werden in
    /// <see cref="TexteSetzen"/> gesetzt. Im Designer stehen seit der Design-Politur vom
    /// 21.08.2026 die DEUTSCHEN Fassungen derselben Ressourcen (vorher der Feldname als
    /// Platzhalter) — allein damit die Entwurfsfläche zeigt, was der Anwender sieht;
    /// maßgeblich bleibt <see cref="TexteSetzen"/>, das jeden dieser Texte beim Öffnen
    /// in der eingestellten Sprache überschreibt.
    /// Nicht serialisierbar und deshalb im Konstruktor-Nachlauf: die kulturabhängigen
    /// Vorgabewerte (<see cref="VorgabenSetzen"/>) und die gemessene Eingabespalte
    /// (<see cref="EingabespalteAusrichten"/>).
    /// </summary>
    public partial class Form_QuellePufferspeicher : Form
    {
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

        /// <summary>Name der Anlage (nur für den Fenstertitel).</summary>
        public string WPName = "";

        /// <summary>Projekt der Anlage — bestimmt die Auswahlliste (E0).</summary>
        public int ID_Projekt;

        /// <summary>
        /// <c>Tab_Energieanlagen.ID_Type</c> der Anlage (Etappe D5b). Vorbelegt mit der
        /// Wärmepumpe, damit alle Bestandsaufrufe unverändert bleiben; beim HEIZKESSEL
        /// (<see cref="ProjektPuffer.TYP_KESSEL"/>) beschreibt der Dialog die KASKADE und
        /// blendet die Verdampfer-Parameter aus.
        /// </summary>
        public int ID_Type = ProjektPuffer.TYP_WP;

        /// <summary>true = der Dialog läuft für einen Heizkessel (Kaskade statt Verdampfer).</summary>
        private bool IstKessel
        {
            get { return ID_Type == ProjektPuffer.TYP_KESSEL; }
        }

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

        /// <summary>
        /// PAKET Q1 (Konzept 8.2/8.4, Schema-Schritt 54): die QUELL-ENTNAHMEHÖHE
        /// <c>Tab_Energieanlagen.WQ_Anschlusshoehe</c>, 0…1 (1 = ganz oben);
        /// <c>null</c> = nicht gepflegt und damit „oben" — der Wert, mit dem Paket B1
        /// fest gerechnet hat.
        ///
        /// <para>Sie gilt für BEIDE Erzeugerarten (8.4 Gleichbehandlung) und steht
        /// deshalb außerhalb der Verdampfer-Rubrik, die beim Heizkessel ausgeblendet
        /// ist.</para>
        /// </summary>
        public double? Anschlusshoehe = null;

        // --- PAKET Q1: programmatisch angehängte Zeile „Quell-Entnahmehöhe" ----------
        //
        // NICHT im Designer: Die Designer-Dateien werden in diesem Projekt nicht von Hand
        // bearbeitet (Hauskonvention), und der Dialog ist ein FixedDialog mit fest
        // gerechneter Pixelgeometrie. Die Zeile entsteht deshalb im Konstruktor, und die
        // Fußknöpfe samt ClientSize rücken um genau ihre Höhe nach unten - dasselbe
        // Verfahren wie bei der Schichtungsgruppe aus Paket P1 (Form_PufferSp_Projekt).
        private Label _lblAnschlusshoehe;
        private TextBox _tbAnschlusshoehe;
        private Label _lblAnschlusshoeheHinweis;

        /// <summary>Höhe der neuen Zeile samt Abstand [px] — der Betrag, um den der Dialog wächst.</summary>
        private const int ANSCHLUSSHOEHE_ZEILE = 56;

        // --- PAKET B2: Temperaturbezug der Kessel-Kaskade (Nutzerauftrag 28.08.2026) ---
        //
        // NUR beim HEIZKESSEL. Bei der Wärmepumpe gibt es keinen Temperaturhub, gegen den
        // ein Quellanteil zu rechnen wäre — sie zieht Verdampferwärme, und das steuern die
        // Felder der Rubrik darüber. Eine Auswahl ohne Wirkung wäre eine Zusage ohne
        // Wirkung (dieselbe Regel wie bei den Verdampfer-Parametern, Etappe D5b).
        //
        // Wie in Paket Q1 programmatisch und nicht im Designer: Der Dialog ist ein
        // FixedDialog mit fest gerechneter Pixelgeometrie, und die Designer-Dateien werden
        // in diesem Projekt nicht von Hand bearbeitet (Hauskonvention).
        private Label _lblTempBezug;
        private RadioButton _rbBerechnet;
        private RadioButton _rbFest;
        private Label _lblTbVorlauf;
        private TextBox _tbTbVorlauf;
        private Label _lblTbRuecklauf;
        private TextBox _tbTbRuecklauf;
        private Label _lblTbHinweis;

        /// <summary>true, sobald <see cref="TemperaturbezugEinpassen"/> den Dialog vergrößert hat.</summary>
        private bool _tempBezugEingepasst = false;

        /// <summary>
        /// PAKET B2 — Vorschlag beim Umschalten auf „fest vorgegeben" mit leeren Feldern.
        /// 70/50 °C ist die Auslegung eines konventionellen Heizkesselsystems und
        /// dieselbe Zahl, die die Engine als letzten Rückfall des Berechnet-Wegs benutzt
        /// (<c>SimulationControl.KESSEL_VORLAUF_RUECKFALL</c>).
        ///
        /// <para>Ein VORSCHLAG, keine stille Festschreibung: Er erscheint nur, wenn beide
        /// Felder leer sind, und er steht danach sichtbar in der Maske — der Anwender
        /// bestätigt ihn mit OK oder überschreibt ihn (Muster der 55-°C-Vorbelegung aus
        /// Paket P1).</para>
        /// </summary>
        private const int VORSCHLAG_VORLAUF = 70;

        /// <summary>Vorschlagswert des Rücklaufs [°C]; siehe <see cref="VORSCHLAG_VORLAUF"/>.</summary>
        private const int VORSCHLAG_RUECKLAUF = 50;

        /// <summary>
        /// PAKET B2: <c>Tab_Energieanlagen.WQ_TemperaturModus</c> (Schema-Schritt 55) —
        /// <c>DbWerte.WQ_TEMPMODUS_BERECHNET</c> oder <c>…_FEST</c>. Beim Öffnen
        /// Vorbelegung, nach OK das Ergebnis.
        /// </summary>
        public string TemperaturModus = DbWerte.WQ_TEMPMODUS_BERECHNET;

        /// <summary>
        /// PAKET B2: die feste Vorgabe der ANLAGE, <c>Tab_Energieanlagen.Vorlauf</c> [°C];
        /// 0 = nicht gepflegt. Sie ist die erste Stufe der W3-Kette aus Paket B1 und wird
        /// nur im Modus <c>Fest</c> geschrieben.
        /// </summary>
        public int VorlaufAnlage;

        /// <summary>
        /// PAKET B2: <c>Tab_Energieanlagen.[Rücklauf]</c> [°C] — die Spalte trägt an der
        /// Datenbank den UMLAUT (siehe <c>ProjektPuffer.SQL_SYSTEM_RUECKLAUF</c>); 0 =
        /// nicht gepflegt.
        /// </summary>
        public int RuecklaufAnlage;

        public Form_QuellePufferspeicher()
        {
            // Der Designer setzt AutoScaleMode bewusst auf None und lässt
            // AutoScaleDimensions weg: Die Maske ist ein FixedDialog mit fest
            // gerechneten Pixelpositionen, und die Anwendung läuft DpiUnaware
            // (app.manifest, Program.SetHighDpiMode). Vor der Designer-Umstellung
            // wurde AutoScaleMode überhaupt nicht gesetzt, es fand also ebenfalls
            // keine Skalierung statt — None hält genau dieses Verhalten fest.
            InitializeComponent();
            TexteSetzen();
            // Erst NACH TexteSetzen(): die drei Beschriftungen haben bis dahin nur
            // die Platzhalter des Designers und damit die falsche Breite.
            EingabespalteAusrichten();
            AnschlusshoeheAnbauen();
            // PAKET B2: Die Steuerelemente entstehen hier, EINGEPASST wird der Block erst
            // in ArtAnwenden() — die Erzeugerart setzt der Aufrufer nach dem Konstruktor.
            TemperaturbezugAnbauen();
            // PAKET B3 (Nutzerauftrag 28.08.2026): Das Häkchen „unbegrenzt verfügbar"
            // schaltet bei gewähltem Puffer die Speicherkopplung ab
            // (WaermequelleClass.Quellspeicher) — dieser Konflikt soll IM Dialog sichtbar
            // sein, nicht erst im Laufprotokoll.
            _cbUnbegrenzt.CheckedChanged += cbUnbegrenzt_CheckedChanged;
            VorgabenSetzen();
            FensterEinpassung.Einhaengen(this);

            // D2 (28.08.2026): Der Dialog steht mit 110x30 bereits auf der Norm-Größe —
            // OK stand aber LINKS von Abbrechen und die Reihe unverankert auf Top/Left.
            FusszeilenNorm.Einhaengen(this, _btnOk, _btnAbbruch);
        }

        /// <summary>
        /// PAKET Q1: hängt die Zeile „Quell-Entnahmehöhe" unter die Rubrik und schiebt
        /// die Fußknöpfe samt <c>ClientSize</c> um genau ihre Höhe nach unten.
        ///
        /// <para><b>Warum unterhalb der Rubrik und nicht darin.</b> Die Rubrik
        /// <c>_gbParameter</c> trägt die VERDAMPFER-Parameter und ist beim Heizkessel
        /// ausgeblendet (Etappe D5b). Die Quell-Entnahmehöhe gilt aber für beide
        /// Erzeugerarten gleichermaßen (Konzept 8.4, „Gleichbehandlung in Dialog und
        /// Schema") — in der Rubrik verschwände sie am Kessel, und genau dort ist sie
        /// so wirksam wie an der Wärmepumpe.</para>
        ///
        /// <para>Läuft NACH <see cref="EingabespalteAusrichten"/>: Das Eingabefeld
        /// übernimmt dessen gemessene Spaltenposition, damit es mit den drei Feldern
        /// darüber fluchtet — auch auf Englisch, wo die Spalte um 17 px nach rechts
        /// rückt.</para>
        /// </summary>
        private void AnschlusshoeheAnbauen()
        {
            int oben = _gbParameter.Bottom + 10;

            _lblAnschlusshoehe = new Label
            {
                Text = MyResource.Resource.SIMQ_PUFFER_ANSCHLUSSHOEHE,
                AutoSize = false,
                Size = new Size(160, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Location = new Point(_gbParameter.Left + 16, oben + 3)
            };

            _tbAnschlusshoehe = new TextBox
            {
                Location = new Point(_gbParameter.Left + _tbTemperatur.Left, oben),
                Width = _tbTemperatur.Width
            };

            _lblAnschlusshoeheHinweis = new Label
            {
                Text = MyResource.Resource.SIMQ_PUFFER_ANSCHLUSSHOEHE_HINWEIS,
                AutoSize = false,
                Size = new Size(590, 28),
                Location = new Point(_gbParameter.Left, oben + 26)
            };

            this.Controls.Add(_lblAnschlusshoehe);
            this.Controls.Add(_tbAnschlusshoehe);
            this.Controls.Add(_lblAnschlusshoeheHinweis);

            // D-CHECK 28.08.2026: Die Hinweiszeile war auf EINE Textzeile ausgelegt; der
            // deutsche Text braucht bei 590 px Breite zwei (30 px statt 28), die zweite
            // wurde am Rahmen beschnitten. Erst NACH dem Einhängen messen - vorher trägt
            // das Label noch nicht die Schrift der Maske. Feste 30 px wären nur für EINE
            // Sprache richtig.
            int noetig = _lblAnschlusshoeheHinweis.GetPreferredSize(
                             new Size(_lblAnschlusshoeheHinweis.Width, 0)).Height;
            int zusatz = Math.Max(0, noetig - _lblAnschlusshoeheHinweis.Height);
            if (zusatz > 0) _lblAnschlusshoeheHinweis.Height = noetig;

            _btnOk.Top += ANSCHLUSSHOEHE_ZEILE + zusatz;
            _btnAbbruch.Top += ANSCHLUSSHOEHE_ZEILE + zusatz;
            this.ClientSize = new Size(this.ClientSize.Width,
                                       this.ClientSize.Height + ANSCHLUSSHOEHE_ZEILE + zusatz);
        }

        /// <summary>
        /// PAKET B2 — legt die Steuerelemente des TEMPERATURBEZUGS an (unsichtbar). Ob
        /// und wo sie erscheinen, entscheidet <see cref="TemperaturbezugEinpassen"/>,
        /// gerufen aus <see cref="ArtAnwenden"/>: Die Erzeugerart steht erst fest, wenn
        /// der Aufrufer <c>ID_Type</c> gesetzt hat, und das geschieht NACH dem
        /// Konstruktor.
        /// </summary>
        private void TemperaturbezugAnbauen()
        {
            _lblTempBezug = new Label
            {
                Text = MyResource.Resource.SIMQ_PUFFER_TEMPERATURBEZUG,
                AutoSize = false,
                Size = new Size(160, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };

            _rbBerechnet = new RadioButton
            {
                Text = MyResource.Resource.SIMQ_PUFFER_TB_BERECHNET,
                AutoSize = true,
                Checked = true,
                Visible = false
            };

            _rbFest = new RadioButton
            {
                Text = MyResource.Resource.SIMQ_PUFFER_TB_FEST,
                AutoSize = true,
                Visible = false
            };

            _lblTbVorlauf = new Label
            {
                Text = MyResource.Resource.SIMQ_PUFFER_TB_VORLAUF,
                AutoSize = false,
                Size = new Size(110, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            _tbTbVorlauf = new TextBox { Width = 60, Visible = false };

            _lblTbRuecklauf = new Label
            {
                Text = MyResource.Resource.SIMQ_PUFFER_TB_RUECKLAUF,
                AutoSize = false,
                Size = new Size(110, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Visible = false
            };
            _tbTbRuecklauf = new TextBox { Width = 60, Visible = false };

            _lblTbHinweis = new Label
            {
                Text = MyResource.Resource.SIMQ_PUFFER_TB_HINWEIS,
                AutoSize = false,
                Size = new Size(590, 30),
                Visible = false
            };

            _rbFest.CheckedChanged += rbTemperaturbezug_CheckedChanged;

            this.Controls.Add(_lblTempBezug);
            this.Controls.Add(_rbBerechnet);
            this.Controls.Add(_rbFest);
            this.Controls.Add(_lblTbVorlauf);
            this.Controls.Add(_tbTbVorlauf);
            this.Controls.Add(_lblTbRuecklauf);
            this.Controls.Add(_tbTbRuecklauf);
            this.Controls.Add(_lblTbHinweis);
        }

        /// <summary>
        /// PAKET B2 — belegt Auswahl und Eingabefelder aus den öffentlichen Feldern vor.
        /// Die beiden Zahlen erscheinen LEER, solange kein vollständiges Paar gepflegt
        /// ist: „0/0 °C" wäre eine Angabe, die es nicht gibt (dieselbe Regel wie in
        /// <see cref="SpeicherItem"/> und bei der Anschlusshöhe).
        /// </summary>
        private void TemperaturbezugSetzen()
        {
            if (_rbFest == null) return;

            bool paar = ProjektPuffer.IstTemperaturpaar(VorlaufAnlage, RuecklaufAnlage);
            _tbTbVorlauf.Text = paar ? VorlaufAnlage.ToString(CultureInfo.CurrentCulture) : "";
            _tbTbRuecklauf.Text = paar ? RuecklaufAnlage.ToString(CultureInfo.CurrentCulture) : "";

            bool fest = string.Equals(TemperaturModus, DbWerte.WQ_TEMPMODUS_FEST,
                                      StringComparison.Ordinal);
            _rbBerechnet.Checked = !fest;
            _rbFest.Checked = fest;
        }

        /// <summary>
        /// PAKET B2 — setzt den Temperaturbezug-Block unter die Quell-Entnahmehöhe und
        /// schiebt die Fußknöpfe samt <c>ClientSize</c> um genau seine Höhe nach unten.
        /// Dasselbe Verfahren wie <see cref="AnschlusshoeheAnbauen"/>; nur läuft es
        /// HÖCHSTENS EINMAL und nur beim Heizkessel.
        ///
        /// <para>Die Hinweiszeile misst sich nach (<c>GetPreferredSize</c>), statt eine
        /// feste Höhe zu behaupten: Der deutsche Text braucht bei 590 px mehr Zeilen als
        /// der englische, und feste Pixel wären nur für EINE Sprache richtig (D-Check
        /// 28.08.2026, derselbe Befund wie bei der Anschlusshöhe).</para>
        /// </summary>
        private void TemperaturbezugEinpassen()
        {
            if (_tempBezugEingepasst || _lblTempBezug == null) return;
            _tempBezugEingepasst = true;

            int links = _gbParameter.Left;
            int oben = _lblAnschlusshoeheHinweis.Bottom + 10;

            _lblTempBezug.Location = new Point(links + 16, oben + 2);
            _rbBerechnet.Location = new Point(links + 180, oben);
            _rbFest.Location = new Point(links + 180 + 190, oben);

            int zeile2 = oben + 26;
            _lblTbVorlauf.Location = new Point(links + 16, zeile2 + 3);
            _tbTbVorlauf.Location = new Point(links + 130, zeile2);
            _lblTbRuecklauf.Location = new Point(links + 220, zeile2 + 3);
            _tbTbRuecklauf.Location = new Point(links + 334, zeile2);

            _lblTbHinweis.Location = new Point(links, zeile2 + 28);

            foreach (Control c in new Control[] { _lblTempBezug, _rbBerechnet, _rbFest,
                                                  _lblTbHinweis })
                c.Visible = true;

            // Erst NACH dem Sichtbarmachen messen - vorher trägt das Label noch nicht die
            // Schrift der Maske.
            int noetig = _lblTbHinweis.GetPreferredSize(new Size(_lblTbHinweis.Width, 0)).Height;
            if (noetig > _lblTbHinweis.Height) _lblTbHinweis.Height = noetig;

            // DAS WACHSTUM WIRD GEMESSEN, NICHT BEHAUPTET.
            //
            // BEFUND des D-Checks vom 28.08.2026: Eine ausgeschriebene Blockhöhe (erster
            // Versuch: 68 px) war um 26 px zu klein - die Fußknöpfe rückten weniger weit
            // nach unten, als der Block hoch ist, und schnitten die Hinweiszeile an
            // (Überlappung 110x20 px, gemessen an _btnOk und _btnAbbruch). Genau dieselbe
            // Falle wie die vier Selbstkorrekturen des alten Layouts (Befund N13a).
            //
            // Der Abstand zwischen dem UNTEREN RAND des Blocks und dem des Vorgängers ist
            // die Strecke, um die alles darunter weichen muss - und er steht nach dem
            // Platzieren fest, statt aus Zeilenhöhen hochgerechnet zu werden.
            int wachstum = _lblTbHinweis.Bottom - _lblAnschlusshoeheHinweis.Bottom;
            if (wachstum < 0) wachstum = 0;

            _btnOk.Top += wachstum;
            _btnAbbruch.Top += wachstum;
            this.ClientSize = new Size(this.ClientSize.Width,
                                       this.ClientSize.Height + wachstum);
        }

        /// <summary>
        /// PAKET B2 — blendet die beiden Eingabefelder ein oder aus. Bei „berechnet" sind
        /// sie AUSGEBLENDET und nicht nur gesperrt: Ein Feld, das niemand liest, ist keine
        /// halbe Zusage, sondern gar keine (dieselbe Regel wie bei den
        /// Verdampfer-Parametern am Kessel).
        /// </summary>
        private void TemperaturfelderAnzeigen()
        {
            bool fest = _rbFest != null && _rbFest.Checked && IstKessel;

            foreach (Control c in new Control[] { _lblTbVorlauf, _tbTbVorlauf,
                                                  _lblTbRuecklauf, _tbTbRuecklauf })
                if (c != null) c.Visible = fest;
        }

        /// <summary>
        /// Beim Umschalten auf „fest vorgegeben" mit LEEREN Feldern erscheint der
        /// Vorschlag 70/50 °C (siehe <see cref="VORSCHLAG_VORLAUF"/>). Er wird
        /// GESCHRIEBEN, sobald der Anwender mit OK bestätigt — bis dahin steht er sichtbar
        /// in der Maske und lässt sich überschreiben.
        /// </summary>
        private void rbTemperaturbezug_CheckedChanged(object sender, EventArgs e)
        {
            TemperaturfelderAnzeigen();

            if (_rbFest == null || !_rbFest.Checked) return;

            if ((_tbTbVorlauf.Text ?? "").Trim().Length == 0 &&
                (_tbTbRuecklauf.Text ?? "").Trim().Length == 0)
            {
                _tbTbVorlauf.Text = VORSCHLAG_VORLAUF.ToString(CultureInfo.CurrentCulture);
                _tbTbRuecklauf.Text = VORSCHLAG_RUECKLAUF.ToString(CultureInfo.CurrentCulture);
            }
        }

        /// <summary>
        /// PAKET B2: liest den Temperaturbezug-Block nach <see cref="TemperaturModus"/>,
        /// <see cref="VorlaufAnlage"/> und <see cref="RuecklaufAnlage"/>.
        ///
        /// <para>Bei „berechnet" bleiben die beiden Zahlen unangetastet — der Aufrufer
        /// schreibt sie dann auch nicht. Das ist die Zusage des Nutzerauftrags: Wer
        /// „berechnet" wählt, muss nichts pflegen, und was er einmal gepflegt hat,
        /// verliert er nicht.</para>
        ///
        /// <para>Bei „fest" gilt dieselbe Regel wie überall im Schema: Nur ein
        /// VOLLSTÄNDIGES Paar (Rücklauf &gt; 0 °C, Vorlauf &gt; Rücklauf) ist eine
        /// Betriebsvorgabe.</para>
        /// </summary>
        /// <returns>false = Meldung gezeigt, der Dialog bleibt offen.</returns>
        private bool TemperaturbezugUebernehmen()
        {
            if (!IstKessel || _rbFest == null) return true;

            if (!_rbFest.Checked)
            {
                TemperaturModus = DbWerte.WQ_TEMPMODUS_BERECHNET;
                return true;
            }

            float v, r;
            if (!WaermequelleClass.ZahlParsen(_tbTbVorlauf.Text, out v) ||
                !WaermequelleClass.ZahlParsen(_tbTbRuecklauf.Text, out r) ||
                !ProjektPuffer.IstTemperaturpaar((int)Math.Round(v), (int)Math.Round(r)))
            {
                MessageBox.Show(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_PUFFER_MSG_TEMPERATURPAAR),
                    MyResource.Resource.SIMQ_PUFFER_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            TemperaturModus = DbWerte.WQ_TEMPMODUS_FEST;
            VorlaufAnlage = (int)Math.Round(v);
            RuecklaufAnlage = (int)Math.Round(r);
            return true;
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

        // ==================================================================
        // Oberfläche — gerettete Begründungen zur Geometrie
        // ==================================================================
        //
        // Die Steuerelemente stehen seit der Designer-Umstellung in
        // Form_QuellePufferspeicher.Designer.cs. Designer-Code trägt keine Kommentare;
        // die Pixelentscheidungen aus den Abnahmebefunden stehen deshalb hier.
        //
        // * ClientSize 620 x 470 (E0): 40 px höher als bisher — darunter liegt die Zeile
        //   mit dem Leer-Hinweis und dem Absprung „Pufferspeicher anlegen…" (Muster
        //   Form_Waermesenke). Die übrigen Maße bleiben unverändert.
        //   ÜBERHOLT durch die Design-Politur 21.08.2026, siehe unten: 620 x 508.
        // * _btnPufferAnlegen (330/244) und _lblLeer (14/240): Leer-Hinweis und Absprung
        //   in die Puffer-Verwaltung (E0). Ohne diesen Weg wäre ein Projekt ohne
        //   Pufferspeicher eine Sackgasse - vorher stand hier eine Meldung über die
        //   STAMM-Daten, die dem Anwender nicht sagte, was zu tun ist (Muster
        //   Form_Waermesenke, Konzept 4.2/4.3).
        // * _lblLeer, Größe 300 x 48 (D5b): 14 px höher und 2 px weiter oben als in E0
        //   (242/34 -> 240/48). Der Hinweis trägt jetzt zwei Fälle - „kein Projektpuffer"
        //   und den nicht aufgelösten Alt-Bezeichner aus E0 -, und der zweite braucht
        //   drei Zeilen. Die Liste darüber endet bei y = 238, die Rubrik darunter beginnt
        //   bei y = 288: Der Platz ist damit vollständig ausgenutzt und nicht
        //   überschritten (gemessen mit TextRenderer, beide Sprachen).
        // * _gbParameter trägt die Parameter der VERDAMPFERSEITE (Quelltemperatur,
        //   nutzbare Spreizung, Regeneration, „unbegrenzt verfügbar"). Sie gelten nur für
        //   die Wärmepumpe und sind beim Heizkessel ausgeblendet (Etappe D5b) — dort
        //   liefert der Puffer über seinen VORLAUF einen Teil des Temperaturhubs, und
        //   alles Übrige rechnet SimulationSPK aus dem Temperaturpaar des Kessels.
        // * _lblKaskade (D5b) steht beim Heizkessel an der Stelle der
        //   Verdampfer-Parameter. Beide liegen deckungsgleich auf (14, 288) mit
        //   590 x 130 — dasselbe Rechteck, damit die Fenstergeometrie unverändert bleibt
        //   (der Dialog ist FixedDialog). Getauscht wird über Visible in ArtAnwenden();
        //   Grundzustand im Designer ist die Wärmepumpe: Rubrik sichtbar,
        //   Kaskadentext unsichtbar.
        // * _lblHinweisArt (330/132) ist der Erklärtext rechts — je Erzeugerart
        //   Verdampferwärme oder Kaskade (D5b). 275 x 105 ist mit TextRenderer
        //   nachgemessen: Der längere der beiden Texte (Quellwärme) braucht bei 275 px
        //   Breite genau 105 px in sieben Zeilen, deutsch wie englisch — kein Spielraum,
        //   aber auch kein Überstand.
        // * _btnOk (430/432) und _btnAbbruch (523/432) standen im Bestand als
        //   ClientSize.Width - 190 bzw. - 97; bei ClientSize.Width = 620 sind das
        //   genau diese beiden Werte. ÜBERHOLT durch die Design-Politur, siehe unten.
        //
        // ==================================================================
        // DESIGN-POLITUR 21.08.2026
        // ==================================================================
        //
        // Anlass: Im Designer standen bis dahin die Feldnamen als Platzhalter, und mit
        // den ECHTEN Texten fiel ein Überstand auf, den kein Platzhalter zeigen konnte.
        // Alle Maße unten sind mit TextRenderer in beiden Sprachen nachgemessen.
        //
        // * _cbUnbegrenzt: 285/92 -> 16/122. DER EIGENTLICHE BEFUND. Die Beschriftung
        //   „Quelle unbegrenzt verfügbar (nur Temperatur maßgeblich)" ist als AutoSize-
        //   Kästchen 343 px breit (englisch 336). Ab x = 285 endete sie damit bei 628 —
        //   38 px HINTER dem rechten Rand der Rubrik (590). Eine GroupBox schneidet ihre
        //   Kinder an der eigenen Kante ab, sichtbar fehlte also „…maßgeblich)".
        //   In der rechten Spalte ist der Platz nicht zu beschaffen (dort stehen nur
        //   295 px zur Verfügung), im linken Feldraster ebenfalls nicht. Das Kästchen
        //   bekommt deshalb eine EIGENE, volle Zeile unter den drei Eingabefeldern:
        //   x = 16 wie die Beschriftungen darüber, y = 122 (die Felder enden bei 114,
        //   also 8 px Luft), Ende bei 359 — reichlich Reserve für Übersetzungen.
        // * _gbParameter und _lblKaskade: 14/288 -> 14/296, 590 x 130 -> 590 x 156.
        //   +26 px für die neue Zeile des Kästchens (Zeilenraster der Rubrik), +8 px
        //   Versatz nach unten, damit unter _lblLeer die 6 px Mindestabstand stehen:
        //   Der Leer-Hinweis endet bei y = 288 und stieß vorher unmittelbar an die
        //   Rubrik. Beide Steuerelemente bleiben deckungsgleich (D5b), der Kaskadentext
        //   braucht bei 590 px Breite 120 px und passt weiterhin.
        // * ClientSize 620 x 470 -> 620 x 508: +38 px. Davon sind 34 px die Verschiebung
        //   der Rubrik (26 px neue Zeile + 8 px Versatz), 4 px kommen von den 7 px, um
        //   die die Fußknöpfe höher geworden sind - die restlichen 3 px holt sich der
        //   untere Rand aus seinem vorher großzügigeren Maß (15 -> 12 px). Die Breite
        //   bleibt bei 620.
        // * _lblDaten: 275 x 90 -> 275 x 84. Die Datenzeile brauchte in keiner Sprache
        //   mehr als 60 px (vier Zeilen), stand aber nur 4 px über _lblHinweisArt.
        //   84 px lassen weiterhin fünf Zeilen zu und schaffen 10 px Abstand.
        // * _btnPufferAnlegen: Höhe 28 -> 30, einheitlich mit den Fußknöpfen. Die Breite
        //   bleibt bei 275 px: Der Text braucht 148 px (englisch 139), die 275 px sind
        //   die Spaltenbreite der rechten Seite (_lblDaten, _lblHinweisArt) — eine
        //   schmalere Schaltfläche würde diese Kante brechen.
        // * _btnOk 430/432 -> 378/466 und _btnAbbruch 523/432 -> 498/466, beide
        //   85 x 23 (WinForms-Vorgabe) -> 110 x 30. Die RECHTE KANTE der Knopfgruppe
        //   bleibt bei x = 608 und damit 12 px vor dem Fensterrand; zwischen den Knöpfen
        //   liegen 10 px. Nach unten bleiben 12 px, nach oben 14 px zur Rubrik. Die
        //   Herleitung „ClientSize.Width − 190 / − 97" trägt nicht mehr, weil die Knöpfe
        //   breiter geworden sind; maßgeblich ist jetzt die rechte Kante.
        // * NICHT geändert: EingabespalteAusrichten() und die Entwurfsposition x = 180
        //   der drei Eingabefelder. Die Methode misst weiter zur Laufzeit nach; dass im
        //   Designer nun der deutsche Text steht, ändert daran nichts — deutsch ergibt
        //   die Rechnung nach wie vor 180 (max. Right = 147), englisch 197. Ihr Hinweis
        //   „vorher tragen die Beschriftungen nur die Designer-Platzhalter" heißt seit
        //   der Politur: vorher tragen sie den DEUTSCHEN Entwurfstext — in jeder anderen
        //   Sprache also weiterhin die falsche Breite.

        /// <summary>
        /// Setzt alle sichtbaren Texte aus <c>MyResource</c>. Läuft direkt nach
        /// <c>InitializeComponent()</c> und ersetzt die dortigen Platzhalter.
        /// </summary>
        private void TexteSetzen()
        {
            this.Text = MyResource.Resource.SIMQ_PUFFER_TITEL;
            _lblKopf.Text = MyResource.Resource.SIMQ_PUFFER_KOPF;
            _btnPufferAnlegen.Text = MyResource.Resource.PSP_BTN_PUFFER_ANLEGEN;
            _gbParameter.Text = MyResource.Resource.SIMQ_PUFFER_GB_PARAMETER;
            _lblQuelltemperatur.Text = MyResource.Resource.SIMQ_PUFFER_QUELLTEMPERATUR;
            _lblSpreizung.Text = MyResource.Resource.SIMQ_PUFFER_SPREIZUNG;
            _lblRegeneration.Text = MyResource.Resource.SIMQ_PUFFER_REGENERATION;
            _cbUnbegrenzt.Text = MyResource.Resource.SIMQ_PUFFER_CB_UNBEGRENZT;
            _lblHinweisArt.Text = MyResource.Resource.SIMQ_PUFFER_HINWEIS_QUELLWAERME;
            _lblKaskade.Text = MyResource.Resource.SIMQ_PUFFER_HINWEIS_KASKADE;
            _btnOk.Text = MyResource.Resource.SIM_BTN_OK;
            _btnAbbruch.Text = MyResource.Resource.SIM_BTN_ABBRECHEN;
        }

        /// <summary>
        /// Feste Pixel-Geometrie (Konzept 13.6, Hauptrisiko der programmatischen
        /// Dialoge): Die englischen Beschriftungen sind länger als die deutschen.
        /// Die Eingabespalte beginnt deshalb erst hinter der breitesten Beschriftung -
        /// auf Deutsch bleibt es bei den bisherigen 180 px, weil dort keine
        /// Beschriftung so weit reicht. Nach oben gekappt, damit die Felder nicht in
        /// die Kapazitätsanzeige (x = 285) laufen.
        ///
        /// Die 180 px des deutschen Falls stehen als Entwurfswert im Designer; diese
        /// Methode rechnet sie zur Laufzeit nach. Sie MUSS deshalb nach
        /// <see cref="TexteSetzen"/> laufen — vorher tragen die drei Beschriftungen nur
        /// die Designer-Platzhalter und sind entsprechend falsch breit.
        ///
        /// BEFUND der Designer-Umstellung: Im Bestand wurde auf drei Labels gemessen, die
        /// zu diesem Zeitpunkt noch KEINEN Container hatten. Eine AutoSize-Beschriftung
        /// ohne Parent misst sich aber nicht nach — sie behält die Vorgabebreite von
        /// 100 px. Die Rechnung ergab deshalb immer 116 + 12 = 128 und damit über die
        /// Untergrenze stets 180, in beiden Sprachen; die englische Beschriftung
        /// „Usable temperature spread [K]:" (Right = 185) lief also in das Eingabefeld
        /// hinein. Jetzt sind die Labels bereits Kinder der Rubrik, die Messung greift:
        /// Deutsch bleibt bei 180 (gemessen: max. Right = 145), Englisch ergibt 197
        /// (max. Right = 185) — genau das, was Paket 9 / L3 beschrieben hatte
        /// („auf Englisch rückt die Spalte um wenige Pixel nach rechts").
        /// </summary>
        private void EingabespalteAusrichten()
        {
            int xEingabe = Math.Max(_lblQuelltemperatur.Right,
                           Math.Max(_lblSpreizung.Right, _lblRegeneration.Right)) + 12;
            if (xEingabe < 180) xEingabe = 180;
            if (xEingabe > 200) xEingabe = 200;

            _tbTemperatur.Left = xEingabe;
            _tbSpreizung.Left = xEingabe;
            _tbRegeneration.Left = xEingabe;
        }

        /// <summary>
        /// Vorbelegung der drei Eingabefelder. Steht bewusst nicht im Designer:
        /// <see cref="Vorgabe"/> formatiert kulturabhängig, und die Werte stammen aus
        /// den öffentlichen Feldern, die der Aufrufer erst nach dem Konstruktor setzt.
        /// <see cref="SetControls"/> überschreibt sie unmittelbar vor dem Anzeigen erneut.
        /// </summary>
        private void VorgabenSetzen()
        {
            _tbTemperatur.Text = Vorgabe(Quelltemperatur);
            _tbSpreizung.Text = Vorgabe(Spreizung);
            _tbRegeneration.Text = Vorgabe(Regeneration);
        }

        /// <summary>
        /// Füllt die Auswahlliste aus den PROJEKT-Pufferspeichern (E0) und belegt die
        /// Felder mit den gespeicherten Werten vor.
        /// </summary>
        public void SetControls()
        {
            if (!string.IsNullOrEmpty(WPName))
                this.Text = string.Format(MyResource.Resource.SIMQ_PUFFER_TITEL_MIT_WP, WPName);

            // PAKET B2: VOR ArtAnwenden - dort wird der Block eingepasst und sichtbar
            // gemacht, und die Auswahl soll dann schon stehen. Der CheckedChanged-Handler
            // des Vorschlags (70/50) läuft dabei nur, wenn wirklich auf „fest"
            // umgeschaltet wird UND die Felder leer sind; ein gepflegtes Paar steht
            // vorher drin und bleibt.
            TemperaturbezugSetzen();

            ArtAnwenden();
            PufferListeLaden();

            _tbTemperatur.Text = Quelltemperatur.ToString("F1");
            _tbSpreizung.Text = Spreizung.ToString("F1");
            _tbRegeneration.Text = Regeneration.ToString("F1");
            _cbUnbegrenzt.Checked = Unbegrenzt;

            // PAKET Q1: LEER heißt „oben" - genau die Aussage der Spalte
            // (WQ_Anschlusshoehe NULL). Eine ausgeschriebene 1,0 behauptete eine
            // Anwenderentscheidung, die es nicht gibt.
            double? hoehe = Anschlusshoehe;   // lokal wegen CS1690 (MarshalByRefObject)
            _tbAnschlusshoehe.Text = hoehe.HasValue ? hoehe.Value.ToString("0.##") : "";

            VorauswahlSetzen();

            ZeigeSpeicherDaten();
            BerechneKapazitaet();
            // PAKET B3: ausdrücklich, nicht nur über die Ereigniskette — bei leerer
            // Pufferliste feuert kein SelectedIndexChanged, und der Checked-Setter oben
            // schweigt, wenn der gespeicherte Wert der Vorgabe (false) entspricht.
            UnbegrenztKonfliktAnzeigen();
        }

        /// <summary>
        /// ETAPPE D5b — stellt den Dialog auf die Erzeugerart ein.
        ///
        /// Für die WÄRMEPUMPE bleibt alles, wie es war. Für den HEIZKESSEL entfallen die
        /// Verdampfer-Parameter: Quelltemperatur, nutzbare Spreizung, Regeneration und
        /// „unbegrenzt verfügbar" beschreiben die Entnahme über einen Verdampfer, und
        /// <c>SimulationSPK</c> liest keinen davon. Der Kessel bezieht seine
        /// Eintrittstemperatur aus dem VORLAUF des Quellpuffers und hebt von dort auf sein
        /// eigenes Temperaturpaar an (<c>SimulationControl.KesselTemperaturpaar</c>) —
        /// genau das erklärt der Text, der an ihrer Stelle steht. Eingabefelder, die
        /// niemand liest, wären eine Zusage ohne Wirkung (dieselbe Regel wie beim
        /// Betriebsmodus, Konzept 4.1).
        /// </summary>
        private void ArtAnwenden()
        {
            if (_gbParameter == null) return;

            _gbParameter.Visible = !IstKessel;
            _lblKaskade.Visible = IstKessel;

            _lblHinweisArt.Text = IstKessel
                ? MyResource.Resource.SIMQ_PUFFER_HINWEIS_KASKADE_KURZ
                : MyResource.Resource.SIMQ_PUFFER_HINWEIS_QUELLWAERME;

            // PAKET B2: Der Temperaturbezug gilt NUR für den Heizkessel (Begründung beim
            // Feld _lblTempBezug). Bei der Wärmepumpe bleibt der Dialog Pixel für Pixel
            // der von Paket Q1.
            if (IstKessel) TemperaturbezugEinpassen();
            TemperaturfelderAnzeigen();
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
            _lbSpeicher.Enabled = !leer;

            if (leer)
            {
                _lblLeer.Text = MyResource.Resource.SIMQ_PUFFER_HINWEIS_KEIN_PROJEKTPUFFER;
                return;
            }

            // ETAPPE E0 / D5b — NICHT AUFGELÖSTER ALTBESTAND.
            //
            // Schritt 9 der SchemaMigration (Regel R7) hat die Bezeichner-Referenz
            // WQ_Puffer in den Fremdschlüssel WQ_ID_Puffer überführt, aber nur bei
            // EINDEUTIGEN Treffern: Gibt es im Projekt keinen oder mehr als einen Puffer
            // dieses Namens, bleibt WQ_ID_Puffer leer und es gilt weiter der Text. Für
            // die Kaskade ist das keine gültige Identität - die Engine baut aus einem
            // reinen Bezeichner KEINEN Quellbezug auf (QuellbezuegeAufbauen verlangt
            // WQ_ID_Puffer > 0).
            //
            // Ohne diesen Hinweis passierte das Folgende STILL: Der Dialog wählt über
            // VorauswahlSetzen den namensgleichen Puffer aus (oder, wenn es keinen gibt,
            // schlicht den ersten der Liste), und beim Bestätigen wird DIESE ID
            // geschrieben. Das ist die richtige Auflösung - aber der Anwender muss sehen,
            // dass er sie gerade trifft.
            _lblLeer.Text = (ID_Puffer <= 0 && !string.IsNullOrEmpty(Pufferspeicher))
                ? string.Format(MyResource.Resource.SIMQ_PUFFER_HINWEIS_ALTBEZEICHNER, Pufferspeicher)
                : "";
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

            // iU9-W10a.0b (Befund W10-B12): die Formel steht im Kern, nicht mehr hier.
            double kapazitaet = ProjektPuffer.NutzbareKapazitaetKWh(p.Gesamtvolumen, spreizung);
            _lblKapazitaet.Text = string.Format(MyResource.Resource.SIMQ_PUFFER_KAPAZITAET,
                kapazitaet.ToString("F1"));
        }

        // ==================================================================
        // Ereignisse
        // ==================================================================

        private void lbSpeicher_SelectedIndexChanged(object sender, EventArgs e)
        {
            ZeigeSpeicherDaten();
            BerechneKapazitaet();
            UnbegrenztKonfliktAnzeigen();
        }

        private void tbTemperatur_TextChanged(object sender, EventArgs e)
        {
            BerechneKapazitaet();
            // PAKET B3: Der Konflikttext nennt die Temperatur, die dann gälte.
            UnbegrenztKonfliktAnzeigen();
        }

        // PAKET B3 (Nutzerauftrag 28.08.2026)
        private void cbUnbegrenzt_CheckedChanged(object sender, EventArgs e)
        {
            UnbegrenztKonfliktAnzeigen();
        }

        /// <summary>
        /// PAKET B3 (Nutzerauftrag 28.08.2026) — macht den KONFLIKT des Häkchens
        /// „unbegrenzt verfügbar" mit einem gewählten Pufferspeicher sichtbar.
        ///
        /// <para><b>Der Befund dahinter:</b> <c>WaermequelleClass.Quellspeicher</c>
        /// liefert bei <c>WQ_Unbegrenzt</c> KEINEN Speicher („nur die Temperatur wirkt,
        /// keine Bilanz") — die gesamte Speicherkopplung aus Paket B1/B2 ist damit
        /// abgeschaltet, obwohl in diesem Dialog ein Puffer gewählt ist. Genau so stand
        /// die Booster-Kette des Anwenderprojekts 1042 still auf konstant 45 °C. Der
        /// Dialog nahm die Kombination bis B3 kommentarlos an.</para>
        ///
        /// <para><b>Bewusst KEINE stille Korrektur:</b> Das Häkchen ist eine gespeicherte
        /// Anwenderangabe, und es gibt den legitimen Altfall „Puffer benannt, aber
        /// bewusst als unerschöpfliche Quelle gerechnet". Der Dialog verwirft nichts —
        /// er färbt die Beschriftung warnrot und nennt die Folge samt der Temperatur,
        /// die dann gälte. Dieselbe Aussage steht als Warnkriterium
        /// (<c>Warnkriterien.QUELLE_UNBEGRENZT</c>) an Karte und Laufstart.</para>
        ///
        /// <para>Beim HEIZKESSEL ist die Rubrik ausgeblendet (D5b) und der Kessel-Pfad
        /// liest das Flag gar nicht (<c>QuellbezuegeAufbauen</c>) — die Methode läuft
        /// dort ins Leere, ohne Schaden.</para>
        /// </summary>
        private void UnbegrenztKonfliktAnzeigen()
        {
            bool konflikt = _cbUnbegrenzt.Checked && AktuellerPuffer() != null;

            if (!konflikt)
            {
                _cbUnbegrenzt.ForeColor = SystemColors.ControlText;
                _cbUnbegrenzt.Text = MyResource.Resource.SIMQ_PUFFER_CB_UNBEGRENZT;
                return;
            }

            float temp;
            string tempText = WaermequelleClass.ZahlParsen(_tbTemperatur.Text, out temp)
                ? temp.ToString("0.#")
                : _tbTemperatur.Text.Trim();

            _cbUnbegrenzt.ForeColor = Color.Firebrick;
            _cbUnbegrenzt.Text = string.Format(
                MyResource.Resource.SIMQ_PUFFER_CB_UNBEGRENZT_KONFLIKT, tempText);
        }

        private void tbSpreizung_TextChanged(object sender, EventArgs e)
        {
            BerechneKapazitaet();
        }

        /// <summary>
        /// Absprung in die Puffer-Verwaltung (Konzept 4.3). Wie in
        /// <c>Form_Waermesenke</c> wird die Liste UNABHÄNGIG vom DialogResult neu
        /// aufgebaut: Die Verwaltung schreibt sofort in die Datenbank, ein über das
        /// Fensterkreuz verlassener Neuanlage-Vorgang bliebe sonst unsichtbar.
        /// </summary>
        private void btnPufferAnlegen_Click(object sender, EventArgs e)
        {
            // Die Verwaltung ist seit iU9-W10a.4 eine Razor-Komponente. In W10a.5 wird
            // sie eine UEBERLAGERUNG dieses Dialogs; bis dahin zeigt die Huelle sie als
            // zweites Fenster - derselbe Aufruf, dasselbe Ergebnis.
            //
            // Keine Verwendungsvorgabe: die Quellseite legt den Kanal nicht fest.
            int neu = PufferSpProjektHuelle.Oeffnen(this, ID_Projekt, null, 0);
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

            // PAKET Q1: die Quell-Entnahmehöhe gilt für BEIDE Erzeugerarten (Konzept 8.4)
            // und wird deshalb VOR der Kessel-Abkürzung geprüft.
            if (!AnschlusshoeheUebernehmen())
            {
                this.DialogResult = DialogResult.None;
                return;
            }

            // D5b: Beim Heizkessel gibt es die Verdampfer-Parameter nicht (ArtAnwenden hat
            // die Rubrik ausgeblendet). Ihre Prüfung würde die Vorbelegungen der
            // unsichtbaren Felder bewerten und im schlimmsten Fall eine Meldung über ein
            // Feld zeigen, das der Anwender nicht sieht.
            if (IstKessel)
            {
                // PAKET B2: Der Temperaturbezug ist die EINZIGE Eingabe, die der Kessel
                // in diesem Dialog macht - sie wird deshalb hier geprüft.
                if (!TemperaturbezugUebernehmen())
                {
                    this.DialogResult = DialogResult.None;
                    return;
                }

                ID_Puffer = gewaehlt.ID;
                Pufferspeicher = gewaehlt.Bezeichner;
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

        /// <summary>
        /// PAKET Q1: liest das Feld „Quell-Entnahmehöhe" nach <see cref="Anschlusshoehe"/>.
        ///
        /// <para><b>LEER ist gültig</b> und bedeutet „oben" (<c>null</c> → die Spalte
        /// bleibt NULL). Alles andere muss eine Zahl aus 0…1 sein; ein Wert außerhalb
        /// wird ABGEWIESEN statt geklemmt, damit der Anwender merkt, dass er eine
        /// Prozentangabe oder eine Höhe in Metern eingegeben hat.</para>
        /// </summary>
        /// <returns>false = Meldung gezeigt, der Dialog bleibt offen.</returns>
        private bool AnschlusshoeheUebernehmen()
        {
            string text = (_tbAnschlusshoehe.Text ?? "").Trim();
            if (text.Length == 0)
            {
                Anschlusshoehe = null;
                return true;
            }

            float h;
            if (!WaermequelleClass.ZahlParsen(text, out h) || h < 0 || h > 1)
            {
                MessageBox.Show(
                    Zeilenumbruch.Normalisieren(MyResource.Resource.SIMQ_PUFFER_MSG_ANSCHLUSSHOEHE),
                    MyResource.Resource.SIMQ_PUFFER_TITEL,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            Anschlusshoehe = h;
            return true;
        }
    }
}
