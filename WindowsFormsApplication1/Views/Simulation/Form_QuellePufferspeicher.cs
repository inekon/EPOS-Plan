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

            double kapazitaet = p.Gesamtvolumen * 1.16 * spreizung / 1000.0;
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
        }

        private void tbTemperatur_TextChanged(object sender, EventArgs e)
        {
            BerechneKapazitaet();
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
