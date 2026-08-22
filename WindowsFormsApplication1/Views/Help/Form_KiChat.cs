using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Chatfenster des KI-Hilfe-Assistenten (Prototyp).
    ///
    /// Der Assistent kennt den Bereich, in dem der Benutzer gerade arbeitet
    /// (siehe HilfeKontext), sucht dazu passende Hilfeabschnitte lokal heraus
    /// (HilfeWissen) und lässt daraus von Gemini 2.5 Flash-Lite eine Antwort
    /// formulieren (KiChatService). Ohne API-Schlüssel arbeitet das Fenster als
    /// reine Hilfe-Suche weiter - dann entstehen keinerlei Kosten.
    ///
    /// Komplett programmatisch aufgebaut (kein Designer, keine .resx).
    /// </summary>
    public class Form_KiChat : Form
    {
        private RichTextBox _verlaufAnzeige;
        private TextBox _eingabe;
        private Button _btnSenden;
        private Button _btnSuchen;
        private Button _btnEinstellungen;
        private Label _lblKontext;
        private LinkLabel _linkVorschau;
        private LinkLabel _linkProtokoll;
        private LinkLabel _linkHinweis;
        private Label _lblStatus;
        private Label _lblFeldsicherung;
        private CheckBox _chkAktionen;
        private Button _btnWerkzeuge;

        // Die beiden Zeilen, die im Hilfe-Betrieb ganz verschwinden (F5). Sie werden
        // als Felder gehalten, weil das ANDOCKENDE Panel verschwinden muss: Ein
        // unsichtbares Dock-Panel gibt seinen Platz frei, einzeln versteckte Inhalte
        // hinterlassen dagegen einen leeren Streifen.
        private Panel _schalterZeile;
        private Panel _hinweisZeile;

        private System.Windows.Forms.Timer _sperrUhr;

        // --- Bestätigungsschicht (Etappe 3, Fachkonzept 3.5) ---
        private Panel _bestaetigungBereich;
        private RichTextBox _bestaetigungText;
        private Label _bestaetigungVerfall;
        private Button _btnAusfuehren;
        private Button _btnBestaetigungAbbrechen;
        private System.Windows.Forms.Timer _verfallUhr;

        /// <summary>Die Freigabe, auf deren Entscheidung dieses Fenster gerade wartet.</summary>
        private KiFreigabe _offeneFreigabe;

        /// <summary>Die Aufgabe, die mit der Entscheidung des Anwenders erfüllt wird.</summary>
        private System.Threading.Tasks.TaskCompletionSource<KiEntscheidung> _offeneAntwort;

        /// <summary>
        /// Der Bestätigungsweg DIESES Fensters — einmal erzeugt und gemerkt.
        /// </summary>
        /// <remarks>
        /// Muss ein Feld sein: Aus einer Methodengruppe entsteht bei jedem Zugriff ein
        /// NEUER Delegat. Ein Vergleich per Verweis beim Schließen ginge damit immer
        /// schief, und der Dienst behielte den Weg eines längst geschlossenen Fensters.
        /// </remarks>
        private readonly KiBestaetigungsfrage _bestaetigungsweg;

        private readonly List<string> _verlauf = new List<string>();
        private string _kontext = "";

        /// <summary>
        /// Bezeichner-Platzhalter DIESER Sitzung (Fachkonzept 4.2). Nach außen geht
        /// „Name 1“, im Chat steht der Klarname; die Tabelle wird nirgends abgelegt.
        /// </summary>
        private readonly KiPlatzhalter _platzhalter = new KiPlatzhalter();

        /// <summary>true, solange dieses Fenster auf eine Antwort wartet.</summary>
        private bool _beschaeftigt;

        /// <summary>Merker, damit die Sperranzeige den übrigen Status nicht überschreibt.</summary>
        private bool _sperreGezeigt;

        /// <summary>
        /// Merker gegen Rückläufer: das Zurücksetzen des Aktionsschalters nach einer
        /// verweigerten Einwilligung löst CheckedChanged erneut aus.
        /// </summary>
        private bool _schalterLaeuft;

        /// <summary>
        /// Hilfe-Betrieb: Die KI ist für diese Installation abgeschaltet, das Fenster
        /// arbeitet als reine Hilfesuche (Fachkonzept 11.9, Umsetzungspaket F5).
        /// Gesetzt wird der Merker ausschließlich in <see cref="HilfeBetriebAnwenden"/>.
        /// </summary>
        private bool _hilfeBetrieb;

        /// <summary>
        /// Beschriftung der lokalen Suche im Regelbetrieb - dort grenzt „Nur suchen“
        /// die kostenlose Suche gegen „Fragen“ ab. Im Hilfe-Betrieb gibt es nichts
        /// mehr abzugrenzen, dann heißt der Knopf schlicht „Suchen“
        /// (KI_HILFEBETRIEB_SUCHEN_BTN).
        /// </summary>
        private const string TEXT_SUCHEN_REGEL = "Nur suchen";

        public Form_KiChat()
        {
            BaueOberflaeche();

            // Der Ausfuehrer marshallt jeden Datenbankzugriff ueber dieses Steuerelement
            // auf den UI-Thread (Fachkonzept 3.4). Ohne Anker suchte er sich das erste
            // offene Formular - das kann waehrend eines Wizards ein anderes sein.
            KiAusfuehrer.Anker = this;

            // Der Weg zur Bestätigung (Fachkonzept 3.5, Punkt 3). Solange dieses Fenster
            // offen ist, kann der Assistent fragen; ist es zu, läuft keine Schreibaktion,
            // weil KiChatService.Bestaetigungsweg dann wieder leer ist.
            _bestaetigungsweg = BestaetigungFragen;
            KiChatService.Bestaetigungsweg = _bestaetigungsweg;
        }

        /// <inheritdoc/>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (ReferenceEquals(KiAusfuehrer.Anker, this)) KiAusfuehrer.Anker = null;
            if (_sperrUhr != null) _sperrUhr.Stop();

            // Eine offene Vorschau darf das Fenster nicht überleben: Wer das Fenster
            // schließt, hat nicht bestätigt.
            Entscheiden(KiEntscheidung.Abgebrochen);
            if (_verfallUhr != null) _verfallUhr.Stop();
            if (ReferenceEquals(KiChatService.Bestaetigungsweg, _bestaetigungsweg))
                KiChatService.Bestaetigungsweg = null;
            base.OnFormClosed(e);
        }

        /// <summary>
        /// Übernimmt den aktuellen Bedienkontext. Wird beim Öffnen aufgerufen -
        /// bewusst vorher, solange das aufrufende Fenster noch aktiv ist.
        /// </summary>
        public void SetzeKontext(string kontext)
        {
            _kontext = kontext ?? "";
            _lblKontext.Text = string.IsNullOrEmpty(_kontext)
                ? "Kontext: (nicht erkannt)"
                : "Kontext: " + _kontext;
        }

        private void BaueOberflaeche()
        {
            this.Text = "Hilfe-Assistent";
            // Der Formularname ist das Praefix in help_mapping.txt; ohne ihn
            // findet die Hilfeautomatik dieses Fenster nicht (HilfeAutomatik, F5).
            this.Name = "Form_KiChat";
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;
            this.ClientSize = new Size(720, 580);
            this.MinimumSize = new Size(620, 460);

            _lblKontext = new Label
            {
                Dock = DockStyle.Top,
                Height = 26,
                Padding = new Padding(10, 6, 10, 0),
                ForeColor = Color.FromArgb(0, 90, 160),
                Text = "Kontext: (nicht erkannt)"
            };

            // Dauerhafter Hinweis, solange die Feldsicherung abgeschaltet ist (Paket F4).
            // Sichtbarkeit und Text setzt FeldsicherungAnwenden(); bei aktiver Sicherung
            // bleibt das Label verborgen und ein angedocktes unsichtbares Label nimmt
            // keinen Platz weg. Gestaltung wie der Bestätigungsblock — gedämpftes Gelb
            // mit dunkler Schrift —, damit sofort erkennbar ist, dass hier etwas vom
            // Regelbetrieb abweicht, ohne dass es wie eine Fehlermeldung aussieht.
            _lblFeldsicherung = new Label
            {
                Dock = DockStyle.Top,
                Height = 24,
                Visible = false,
                Padding = new Padding(10, 4, 10, 0),
                AutoEllipsis = true,
                BackColor = Color.FromArgb(255, 249, 226),
                ForeColor = Color.FromArgb(150, 90, 0),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Text = ""
            };

            _verlaufAnzeige = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9.5f)
            };

            // Unterer Bereich: konsequent über Docking aufgebaut, damit alle
            // Schaltflächen unabhängig von Fenstergröße und Schriftskalierung
            // sichtbar bleiben (keine absoluten Koordinaten).
            Panel unten = new Panel { Dock = DockStyle.Bottom, Height = 182 };

            // --- untere Schaltflächenleiste ---
            Panel leiste = new Panel { Dock = DockStyle.Bottom, Height = 44, Padding = new Padding(8, 8, 8, 8) };

            _btnEinstellungen = new Button { Text = "Einstellungen...", Width = 120, Height = 28, Margin = new Padding(6, 0, 0, 0) };
            _btnEinstellungen.Click += (s, e) => EinstellungenOeffnen();

            Button btnSchliessen = new Button
            {
                Text = "Schließen",
                Width = 100,
                Height = 28,
                Margin = new Padding(6, 0, 0, 0),
                DialogResult = DialogResult.Cancel
            };
            btnSchliessen.Click += (s, e) => Close();

            FlowLayoutPanel leisteRechts = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };
            leisteRechts.Controls.Add(btnSchliessen);        // ganz rechts
            leisteRechts.Controls.Add(_btnEinstellungen);    // links daneben

            // Infobutton (H4): der Weg zur geschriebenen Hilfe, unabhaengig vom
            // Assistenten. Zuordnung in help_mapping.txt; fehlt sie, bleibt der
            // Knopf grau statt wirkungslos anklickbar (F3).
            Button btn_Help = new Button
            {
                Name = "btn_Help",
                Size = new Size(28, 28),
                Margin = new Padding(6, 0, 0, 0),
                BackColor = Color.Transparent,
                BackgroundImage = Properties.Resources.help_icon,
                BackgroundImageLayout = ImageLayout.Zoom,
                Cursor = Cursors.Hand,
                FlatStyle = FlatStyle.Flat,
                TabStop = false,
                UseVisualStyleBackColor = false
            };
            btn_Help.FlatAppearance.BorderSize = 0;
            leisteRechts.Controls.Add(btn_Help);               // links vom Einstellungsknopf

            LinkLabel linkDoku = new LinkLabel
            {
                Text = "Online-Dokumentation öffnen",
                AutoSize = true,
                Padding = new Padding(2, 6, 0, 0)
            };
            linkDoku.LinkClicked += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                        MDIMainForm.DOKU_URL) { UseShellExecute = true });
                }
                catch { }
            };

            // Selbstprüfung (A5): zeigt den vollständigen Text, der übertragen würde.
            _linkVorschau = new LinkLabel
            {
                Text = MyResource.Resource.KI_VORSCHAU_LINK,
                AutoSize = true,
                Padding = new Padding(14, 6, 0, 0)
            };
            _linkVorschau.LinkClicked += (s, e) => VorschauZeigen();

            FlowLayoutPanel leisteLinks = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };
            // Die Protokollzeile jeder Aktion ist einsehbar - die Datei liegt neben der
            // Datenbank (Fachkonzept 3.6).
            _linkProtokoll = new LinkLabel
            {
                Text = MyResource.Resource.KI_AKT_PROTOKOLL_LINK,
                AutoSize = true,
                Padding = new Padding(14, 6, 0, 0)
            };
            _linkProtokoll.LinkClicked += (s, e) => ProtokollZeigen();

            leisteLinks.Controls.Add(linkDoku);
            leisteLinks.Controls.Add(_linkVorschau);
            leisteLinks.Controls.Add(_linkProtokoll);

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                AutoEllipsis = true,
                Text = ""
            };

            // Reihenfolge beachten: Fill zuerst, dann die andockenden Elemente
            leiste.Controls.Add(_lblStatus);
            leiste.Controls.Add(leisteLinks);
            leiste.Controls.Add(leisteRechts);

            // --- Dauerhafter Kurzhinweis, eine Zeile, direkt über der Linkleiste ---
            // Er sagt in einem Satz, was der eigentliche Punkt ist (die Frage geht im
            // Wortlaut hinaus) und führt auf den vollständigen Rechtshinweis.
            _hinweisZeile = new Panel { Dock = DockStyle.Bottom, Height = 24, Padding = new Padding(8, 2, 8, 0) };

            string hinweisVorn = MyResource.Resource.KI_HINWEIS_ZEILE ?? "";
            string hinweisLink = MyResource.Resource.KI_HINWEIS_ZEILE_LINK ?? "";

            _linkHinweis = new LinkLabel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8.5f),
                Text = hinweisVorn + hinweisLink,
                LinkArea = new LinkArea(hinweisVorn.Length, hinweisLink.Length)
            };
            _linkHinweis.LinkClicked += (s, e) => Form_KiHinweis.Anzeigen(this);
            _hinweisZeile.Controls.Add(_linkHinweis);

            // --- Eingabebereich ---
            Panel eingabeBereich = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 4) };

            _btnSenden = new Button { Text = "Fragen", Width = 110, Height = 30, Margin = new Padding(0, 0, 0, 6) };
            _btnSenden.Click += async (s, e) => await FrageStellen(true);

            _btnSuchen = new Button { Text = TEXT_SUCHEN_REGEL, Width = 110, Height = 30 };
            _btnSuchen.Click += async (s, e) => await FrageStellen(false);

            FlowLayoutPanel eingabeRechts = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Padding = new Padding(8, 0, 0, 0)
            };
            eingabeRechts.Controls.Add(_btnSenden);
            eingabeRechts.Controls.Add(_btnSuchen);

            _eingabe = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                Font = new Font("Segoe UI", 9.5f)
            };
            _eingabe.KeyDown += Eingabe_KeyDown;

            eingabeBereich.Controls.Add(_eingabe);        // Fill zuerst
            eingabeBereich.Controls.Add(eingabeRechts);

            // --- Schalterzeile: Aktionsbetrieb und Werkzeugliste ---
            _schalterZeile = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(8, 4, 8, 0) };

            _chkAktionen = new CheckBox
            {
                Text = MyResource.Resource.KI_AKT_SCHALTER,
                AutoSize = true,
                Dock = DockStyle.Left,
                Checked = false
            };

            // Der Datenschutzsatz der Begruessung gilt nur fuer den reinen Hilfefall.
            // Sobald Aktionen zugelassen sind, gehen ERGEBNISSE zurueck - deshalb wird
            // beim Umschalten gesagt, was sich genau aendert (Fachkonzept 4.2).
            //
            // Der Aktionsbetrieb ueberträgt mehr (Werkzeugkatalog, Ergebnisse) und
            // verlangt deshalb die Einwilligung schon beim Einschalten - nicht erst bei
            // der ersten Frage. Wer ablehnt, bekommt den Schalter zurueckgestellt.
            _chkAktionen.CheckedChanged += (s, e) => AktionsschalterGeaendert();

            _btnWerkzeuge = new Button
            {
                Text = MyResource.Resource.KI_AKT_WERKZEUGE_BTN,
                Width = 130,
                Height = 24,
                Dock = DockStyle.Right
            };
            _btnWerkzeuge.Click += (s, e) => WerkzeugeOeffnen();

            _schalterZeile.Controls.Add(_chkAktionen);
            _schalterZeile.Controls.Add(_btnWerkzeuge);

            unten.Controls.Add(eingabeBereich);           // Fill zuerst
            unten.Controls.Add(leiste);
            unten.Controls.Add(_hinweisZeile);           // dockt oberhalb der Leiste an
            unten.Controls.Add(_schalterZeile);

            BaueBestaetigungsblock();

            this.Controls.Add(_verlaufAnzeige);
            this.Controls.Add(_bestaetigungBereich);
            this.Controls.Add(_lblKontext);

            // Nach _lblKontext eingehängt und deshalb ÜBER ihm: Angedockt wird in
            // umgekehrter Reihenfolge des Einfügens (dieselbe Regel, nach der weiter oben
            // das Fill-Element zuerst eingehängt wird). Der Hinweis steht damit als
            // oberste Zeile des Fensters — er gilt für alles, was darunter passiert.
            this.Controls.Add(_lblFeldsicherung);
            this.Controls.Add(unten);
            this.CancelButton = btnSchliessen;

            // Sperranzeige: solange eine Assistentenaktion laeuft, bleiben Fragen und
            // Werkzeugliste gesperrt. Die Aktion kann auch von woanders angestossen
            // worden sein - deshalb wird KiAusfuehrer.Belegt zyklisch abgefragt und nicht
            // nur beim eigenen Aufruf gesetzt.
            _sperrUhr = new System.Windows.Forms.Timer { Interval = 400 };
            _sperrUhr.Tick += (s, e) => SperreAktualisieren();
            _sperrUhr.Start();

            // Vor der Begrüßung: Sie fällt im Hilfe-Betrieb anders aus und braucht den
            // Merker bereits gesetzt.
            HilfeBetriebAnwenden();
            FeldsicherungAnwenden();

            Begruessung();
        }

        // ------------------------------------------------------------------
        // Hilfe-Betrieb (Fachkonzept 11.9, Umsetzungspaket F5)
        // ------------------------------------------------------------------

        /// <summary>
        /// Legt fest, welche Bedienelemente dieses Fenster zeigt: im Regelbetrieb alle,
        /// im Hilfe-Betrieb nur Eingabefeld, Suche und Hilfeabschnitte.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Was der Hilfe-Betrieb ist.</b> Ist die KI für diese Installation
        /// abgeschaltet (<see cref="KiEinwilligung.Abgeschaltet"/> - benutzerbezogen unter
        /// HKCU, maschinenweit unter HKLM und dann aus der Anwendung heraus nicht lösbar),
        /// bleibt vom Fenster die reine Hilfesuche übrig: keine KI-Beschriftung, keine
        /// Werkzeugliste, kein „Was wird gesendet?“, keine Aufgabensteuerung. Das Fenster
        /// geht trotzdem auf - die Hilfe liegt lokal vor und braucht den Dienst nicht
        /// (Fachkonzept 1.3).
        /// </para>
        /// <para>
        /// <b>Warum Verstecken und kein zweiter Fensteraufbau.</b> Eine zweite
        /// Aufbauroutine wäre eine zweite Pflegestelle für dieselben Bedienelemente und
        /// liefe unweigerlich auseinander. Verborgene Steuerelemente bleiben dagegen
        /// gültige Verweise: <c>SperreAktualisieren</c> und <c>VorschauZeigen</c> greifen
        /// unverändert auf sie zu, ohne Sonderfall und ohne Prüfung auf <c>null</c>.
        /// </para>
        /// <para>
        /// <b>Warum in beide Richtungen geschaltet wird.</b> Die Methode setzt jede
        /// Sichtbarkeit ausdrücklich - im Regelbetrieb auf sichtbar, im Hilfe-Betrieb auf
        /// verborgen. So ändert sie bei nicht gesetztem Schalter nachweislich nichts
        /// (Abgrenzung F5) und bleibt bei mehrfachem Aufruf gutmütig.
        /// </para>
        /// <para>
        /// <b>Warum bei jedem Öffnen und nicht einmal beim Programmstart.</b> Die
        /// Verwaltung kann den Schalter im laufenden Programm umlegen
        /// (<c>Form_AdminSettings</c>). Der Aufruf steht deshalb im Aufbau des Fensters,
        /// und <see cref="Oeffnen"/> legt jedes Mal ein neues an - jedes Öffnen liest den
        /// Schalter also neu, genau wie der Menüeintrag beim Aufklappen.
        /// </para>
        /// <para>
        /// <b>Keine Schutzwirkung.</b> Dass nichts an den Dienst hinausgeht, trägt
        /// <c>KiEinwilligung.Sicherstellen</c> und der Einwilligungsriegel in
        /// <c>KiChatService</c> - nicht diese Methode. Das Ausblenden ist eine reine
        /// Darstellungsfrage und ersetzt keinen Schutz.
        /// </para>
        /// </remarks>
        private void HilfeBetriebAnwenden()
        {
            _hilfeBetrieb = KiEinwilligung.Abgeschaltet;
            bool mitKi = !_hilfeBetrieb;

            // „Fragen“ ist der einzige Weg zum Dienst - im Hilfe-Betrieb bleibt die
            // Suche daneben als einziger Knopf stehen und heißt dann nur noch „Suchen“.
            _btnSenden.Visible = mitKi;
            _btnSuchen.Text = mitKi ? TEXT_SUCHEN_REGEL
                                    : MyResource.Resource.KI_HILFEBETRIEB_SUCHEN_BTN;

            // Aktionsbetrieb und Werkzeugliste (Aufgabensteuerung) sowie der
            // Übertragungshinweis samt „Was wird gesendet?“ und Aktionsprotokoll: Sie
            // beschreiben allesamt den Verkehr mit dem Dienst, den es hier nicht gibt.
            _schalterZeile.Visible = mitKi;
            _hinweisZeile.Visible = mitKi;
            _linkVorschau.Visible = mitKi;
            _linkProtokoll.Visible = mitKi;

            // „Einstellungen...“ führt einzig zum API-Schlüssel und zum Modell.
            _btnEinstellungen.Visible = mitKi;

            // Der Bestätigungsblock ist ohnehin verborgen, bis eine Aktion ihn füllt;
            // im Hilfe-Betrieb kann keine entstehen. Er wird hier nicht angefasst.
        }

        // ------------------------------------------------------------------
        // Feldsicherung (Fachkonzept 11.5, Umsetzungspaket F4)
        // ------------------------------------------------------------------

        /// <summary>
        /// Zeigt dauerhaft an, dass die Feldsicherung für diesen Programmlauf abgeschaltet
        /// ist — und verbirgt die Zeile, solange sie an ist.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Warum der Hinweis dauerhaft steht und nicht einmal gemeldet wird.</b> Er
        /// beschreibt keinen Vorgang, sondern einen Zustand: Solange er sichtbar ist,
        /// werden Felder ohne gesonderte Bestätigung gesetzt. Eine Meldung beim Öffnen
        /// wäre nach der dritten Frage vergessen; die Zeile bleibt stehen, solange der
        /// Zustand gilt (Fachkonzept 11.5: „das Chatfenster zeigt dauerhaft
        /// ‚Feldsicherung AUS'").
        /// </para>
        /// <para>
        /// <b>Warum der Text aus dem Kern kommt.</b> <c>KiFeldsicherung.Chathinweis()</c>
        /// liefert ihn fertig — dieselbe Quelle, aus der auch der Protokollvermerk stammt.
        /// Ein hier formulierter zweiter Wortlaut könnte vom Protokoll abweichen, und der
        /// Satz sagt bewusst auch, was WEITER gilt: Die Bestätigung datenverändernder
        /// Aktionen bleibt bestehen.
        /// </para>
        /// <para>
        /// <b>Warum die Auswertung bei jedem Öffnen genügt.</b> Der Schalter ist ein
        /// Startzustand und lässt sich zur Laufzeit nicht mehr ändern
        /// (<c>KiFeldsicherung.Abschalten</c> wirkt genau einmal, gerufen aus
        /// <c>Program.Main</c>). Anders als beim Hilfe-Betrieb kann sich hier während des
        /// Betriebs also nichts verstellen — die einmalige Auswertung im Fensteraufbau ist
        /// deshalb keine Vereinfachung, sondern die vollständige Antwort.
        /// </para>
        /// <para>
        /// <b>Im Hilfe-Betrieb ohne Wirkung.</b> Dort läuft keine Aktion und damit auch
        /// keine Feldsetzung; ein Hinweis auf die Feldsicherung ginge ins Leere. Die Zeile
        /// bleibt deshalb verborgen — geprüft über den Merker, den
        /// <see cref="HilfeBetriebAnwenden"/> unmittelbar davor gesetzt hat.
        /// </para>
        /// </remarks>
        private void FeldsicherungAnwenden()
        {
            string hinweis = _hilfeBetrieb ? "" : KiFeldsicherung.Chathinweis();

            _lblFeldsicherung.Text = hinweis;
            _lblFeldsicherung.Visible = hinweis.Length > 0;
        }

        // ------------------------------------------------------------------
        // Bestätigungsschicht (Etappe 3, Fachkonzept 3.5)
        // ------------------------------------------------------------------

        /// <summary>
        /// Baut den Vorschaublock: Klartext, „Ausführen", „Abbrechen", Verfallsanzeige.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Schaltflächen, kein getipptes „ja".</b> Ein getipptes Einverständnis wäre
        /// wieder Modellinterpretation, ein Klick ist es nicht (Fachkonzept 3.5, Punkt 3).
        /// </para>
        /// <para>
        /// <b>Ein Block im Fenster, kein modaler Dialog.</b> Ein MessageBox-Dialog würde
        /// die Modalitätsprüfung des Ausführers selbst auslösen und den Verlauf verdecken —
        /// der Anwender soll die Vorschau neben dem lesen können, was zu ihr geführt hat.
        /// </para>
        /// </remarks>
        private void BaueBestaetigungsblock()
        {
            _bestaetigungBereich = new Panel
            {
                Dock = DockStyle.Top,
                Height = 172,
                Visible = false,
                Padding = new Padding(10, 6, 10, 6),
                BackColor = Color.FromArgb(255, 249, 226)
            };

            _bestaetigungText = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                Font = new Font("Segoe UI", 9f)
            };

            Panel fuss = new Panel { Dock = DockStyle.Bottom, Height = 36 };

            _btnAusfuehren = new Button
            {
                Text = MyResource.Resource.KI_AKT_BESTAETIGUNG_AUSFUEHREN,
                Width = 120,
                Height = 28,
                Dock = DockStyle.Left
            };
            _btnAusfuehren.Click += (s, e) => Entscheiden(KiEntscheidung.Erteilt);

            _btnBestaetigungAbbrechen = new Button
            {
                Text = MyResource.Resource.KI_AKT_BESTAETIGUNG_ABBRECHEN,
                Width = 120,
                Height = 28,
                Dock = DockStyle.Left
            };
            _btnBestaetigungAbbrechen.Click += (s, e) => Entscheiden(KiEntscheidung.Abgelehnt);

            _bestaetigungVerfall = new Label
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(14, 7, 0, 0),
                ForeColor = Color.FromArgb(160, 80, 0)
            };

            // Reihenfolge: das Fill-Element zuerst, dann die Schaltflächen nach links -
            // sonst nimmt das Fill-Element den Platz der Knöpfe ein.
            fuss.Controls.Add(_bestaetigungVerfall);
            fuss.Controls.Add(_btnBestaetigungAbbrechen);
            fuss.Controls.Add(_btnAusfuehren);

            Label kopf = new Label
            {
                Dock = DockStyle.Top,
                Height = 20,
                Text = MyResource.Resource.KI_AKT_BESTAETIGUNG_TITEL,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(150, 90, 0)
            };

            _bestaetigungBereich.Controls.Add(_bestaetigungText);
            _bestaetigungBereich.Controls.Add(fuss);
            _bestaetigungBereich.Controls.Add(kopf);

            _verfallUhr = new System.Windows.Forms.Timer { Interval = 500 };
            _verfallUhr.Tick += (s, e) => VerfallAktualisieren();
        }

        /// <summary>
        /// Der Weg, über den <c>KiChatService</c> die Bestätigung einholt.
        /// </summary>
        /// <remarks>
        /// Liefert eine Aufgabe, die erst mit dem Klick des Anwenders erfüllt wird — das
        /// ist die Stelle, an der die Runden-Schleife des Dienstes stehen bleibt, ohne
        /// einen Thread zu belegen. Ist das Fenster nicht (mehr) da, kommt sofort eine
        /// Ablehnung zurück; ein wartender Dienst darf nicht auf ein geschlossenes Fenster
        /// hoffen.
        /// </remarks>
        private System.Threading.Tasks.Task<KiEntscheidung> BestaetigungFragen(
            KiFreigabe freigabe, System.Threading.CancellationToken abbruch)
        {
            var quelle = new System.Threading.Tasks.TaskCompletionSource<KiEntscheidung>();

            if (freigabe == null || this.IsDisposed || !this.IsHandleCreated)
            {
                quelle.TrySetResult(KiEntscheidung.Abgelehnt);
                return quelle.Task;
            }

            MethodInvoker zeigen = delegate { BestaetigungZeigen(freigabe, quelle, abbruch); };
            if (this.InvokeRequired) this.BeginInvoke(zeigen); else zeigen();

            return quelle.Task;
        }

        /// <summary>Zeigt den Vorschaublock und wartet auf die Entscheidung.</summary>
        private void BestaetigungZeigen(
            KiFreigabe freigabe,
            System.Threading.Tasks.TaskCompletionSource<KiEntscheidung> quelle,
            System.Threading.CancellationToken abbruch)
        {
            // Es kann immer nur EINE Vorschau offen sein; eine zweite wäre eine
            // Sammelbestätigung durch die Hintertür (Fachkonzept 3.5, Punkt 4).
            if (_offeneAntwort != null)
            {
                quelle.TrySetResult(KiEntscheidung.Abgelehnt);
                return;
            }

            _offeneFreigabe = freigabe;
            _offeneAntwort = quelle;

            // Der Text steht AUCH im Verlauf: Was bestätigt wurde, soll nachlesbar
            // bleiben, nachdem der Block wieder verschwunden ist.
            SchreibeZeile(MyResource.Resource.KI_AKT_BESTAETIGUNG_TITEL,
                          Color.FromArgb(150, 90, 0), true);
            SchreibeZeile(freigabe.Text.TrimEnd(), Color.Black, false);

            _bestaetigungText.Text = freigabe.Text.Replace("\n", Environment.NewLine);
            _bestaetigungBereich.Visible = true;
            _btnAusfuehren.Enabled = true;
            _btnBestaetigungAbbrechen.Enabled = true;
            _btnAusfuehren.Focus();

            VerfallAktualisieren();
            _verfallUhr.Start();

            if (abbruch.CanBeCanceled)
                abbruch.Register(delegate
                {
                    MethodInvoker weg = delegate { Entscheiden(KiEntscheidung.Abgebrochen); };
                    try
                    {
                        if (this.IsDisposed || !this.IsHandleCreated) return;
                        if (this.InvokeRequired) this.BeginInvoke(weg); else weg();
                    }
                    catch (ObjectDisposedException) { }
                    catch (InvalidOperationException) { }
                });
        }

        /// <summary>Zählt die Frist herunter und beendet die Vorschau beim Verfall.</summary>
        private void VerfallAktualisieren()
        {
            KiFreigabe f = _offeneFreigabe;
            if (f == null) { _verfallUhr.Stop(); return; }

            TimeSpan rest = f.Restzeit();
            if (rest <= TimeSpan.Zero)
            {
                Entscheiden(KiEntscheidung.Verfallen);
                return;
            }

            _bestaetigungVerfall.Text = string.Format(
                MyResource.Resource.KI_AKT_BESTAETIGUNG_VERFALL,
                (int)Math.Ceiling(rest.TotalSeconds));
        }

        /// <summary>
        /// Beendet die offene Vorschau mit dem Ausgang <paramref name="entscheidung"/>.
        /// Mehrfachaufruf ist unschädlich — der erste gewinnt.
        /// </summary>
        private void Entscheiden(KiEntscheidung entscheidung)
        {
            System.Threading.Tasks.TaskCompletionSource<KiEntscheidung> quelle = _offeneAntwort;
            if (quelle == null) return;

            _offeneAntwort = null;
            _offeneFreigabe = null;

            if (_verfallUhr != null) _verfallUhr.Stop();
            if (_bestaetigungBereich != null && !_bestaetigungBereich.IsDisposed)
                _bestaetigungBereich.Visible = false;

            if (!this.IsDisposed)
            {
                switch (entscheidung)
                {
                    case KiEntscheidung.Erteilt:
                        SchreibeZeile(MyResource.Resource.KI_AKT_BESTAETIGUNG_ERTEILT,
                                      Color.FromArgb(0, 120, 0), false);
                        break;
                    case KiEntscheidung.Verfallen:
                        SchreibeZeile(MyResource.Resource.KI_AKT_BESTAETIGUNG_VERFALLEN,
                                      Color.FromArgb(160, 80, 0), false);
                        break;
                    default:
                        SchreibeZeile(MyResource.Resource.KI_AKT_BESTAETIGUNG_ABGELEHNT,
                                      Color.FromArgb(170, 0, 0), false);
                        break;
                }
            }

            quelle.TrySetResult(entscheidung);
        }

        /// <summary>
        /// Der Aktionsschalter wurde umgelegt. Beim EINschalten wird die Einwilligung in
        /// den Rechtshinweis sichergestellt; ohne sie bleibt der Schalter aus.
        /// </summary>
        private void AktionsschalterGeaendert()
        {
            if (_schalterLaeuft) return;

            if (_chkAktionen.Checked && !KiEinwilligung.Sicherstellen())
            {
                _schalterLaeuft = true;
                try { _chkAktionen.Checked = false; }
                finally { _schalterLaeuft = false; }

                SchreibeZeile(KiEinwilligung.Abgeschaltet
                                  ? MyResource.Resource.KI_ABSCHALTER_MELDUNG
                                  : MyResource.Resource.KI_HINWEIS_ABGELEHNT,
                              Color.FromArgb(170, 0, 0), false);
                return;
            }

            SchreibeZeile(_chkAktionen.Checked
                              ? MyResource.Resource.KI_AKT_DATENSCHUTZ_EIN
                              : MyResource.Resource.KI_AKT_DATENSCHUTZ_AUS,
                          Color.FromArgb(0, 90, 160), false);
        }

        /// <summary>Schaltet Eingaben ab, solange etwas laeuft.</summary>
        private void SperreAktualisieren()
        {
            bool laeuft = KiAusfuehrer.Belegt;
            bool belegt = _beschaeftigt || laeuft;

            _btnSenden.Enabled = !belegt;
            _btnSuchen.Enabled = !belegt;
            _btnWerkzeuge.Enabled = !belegt;

            if (laeuft)
            {
                _lblStatus.Text = MyResource.Resource.KI_AKT_LAEUFT;
                _sperreGezeigt = true;
            }
            else if (_sperreGezeigt)
            {
                _sperreGezeigt = false;
                _lblStatus.Text = "";
            }
        }

        private void Begruessung()
        {
            SchreibeZeile("Hilfe-Assistent", Color.FromArgb(0, 90, 160), true);

            // Im Hilfe-Betrieb gibt es weder Schlüssel noch Tageskontingent, über die zu
            // berichten wäre - nur die lokale Suche. Der Satz sagt zugleich, dass dabei
            // nichts diesen Rechner verlässt.
            if (_hilfeBetrieb)
            {
                SchreibeZeile(MyResource.Resource.KI_HILFEBETRIEB_BEGRUESSUNG,
                    Color.Black, false);
                SchreibeZeile("", Color.Black, false);
                return;
            }

            if (KiChatService.IstEingerichtet)
            {
                SchreibeZeile("Stellen Sie Ihre Frage zur Bedienung oder zur Rechenlogik. " +
                    "Der Assistent kennt den Bereich, in dem Sie gerade arbeiten, und antwortet " +
                    "auf Basis der Hilfe-Dokumentation.", Color.Black, false);
                SchreibeZeile("Es werden nur Hilfetexte, Ihre Frage und der Bereichsname übertragen - " +
                    "keine Projekt- oder Kundendaten.", Color.DimGray, false);
                SchreibeZeile("Heute genutzt: " + KiChatService.AnfragenHeute + " von " +
                    KiChatService.Tageslimit + " Anfragen.", Color.DimGray, false);
                SchreibeZeile(MyResource.Resource.KI_AKT_STUFE1_HINWEIS, Color.DimGray, false);
            }
            else
            {
                SchreibeZeile("Es ist noch kein API-Schlüssel hinterlegt. " +
                    "Die Schaltfläche 'Nur suchen' funktioniert bereits - sie durchsucht die " +
                    "Hilfe lokal und kostenlos. Für die KI-Antworten bitte über " +
                    "'Einstellungen...' einen Google-AI-Studio-Schlüssel eintragen.",
                    Color.FromArgb(160, 80, 0), false);
            }

            SchreibeZeile("", Color.Black, false);
        }

        private async void Eingabe_KeyDown(object sender, KeyEventArgs e)
        {
            // Enter sendet, Shift+Enter erzeugt eine neue Zeile.
            // Im Hilfe-Betrieb führt Enter auf die lokale Suche: Der Weg zum Dienst ist
            // dort weder sichtbar (die Schaltfläche „Fragen“ ist verborgen) noch gangbar
            // (der Einwilligungsriegel weist ihn ab) - Enter darf ihn nicht heimlich
            // wieder öffnen und dem Anwender nur eine Fehlermeldung eintragen.
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await FrageStellen(!_hilfeBetrieb);
            }
        }

        /// <summary>
        /// Beantwortet die Frage. mitKi=false führt nur die lokale Suche aus
        /// (immer kostenlos), mitKi=true formuliert zusätzlich eine Antwort.
        /// </summary>
        private async System.Threading.Tasks.Task FrageStellen(bool mitKi)
        {
            string frage = _eingabe.Text.Trim();
            if (frage.Length == 0) return;

            SchreibeZeile("Sie: " + frage, Color.FromArgb(0, 100, 0), true);
            _eingabe.Clear();
            _verlauf.Add("Benutzer: " + frage);

            // Immer zuerst die lokale Suche zeigen - sie kostet nichts
            List<WissensAbschnitt> treffer = HilfeWissen.Suchen(frage, _kontext, 4);

            if (!mitKi)
            {
                if (treffer.Count == 0)
                {
                    SchreibeZeile("Hilfe: Keine passenden Abschnitte gefunden. " +
                        "Versuchen Sie es mit anderen Stichworten oder stellen Sie die Frage " +
                        "über die Schaltfläche 'Fragen'.", Color.DimGray, false);
                }
                else
                {
                    SchreibeZeile("Gefundene Hilfeabschnitte:", Color.FromArgb(0, 90, 160), true);
                    foreach (WissensAbschnitt a in treffer)
                    {
                        SchreibeZeile("• " + a.Titel + " (" + a.Bereich + ")", Color.Black, false);
                        SchreibeZeile("   " + Kuerzen(a.Inhalt, 220), Color.DimGray, false);
                    }
                }
                SchreibeZeile("", Color.Black, false);
                return;
            }

            _beschaeftigt = true;
            _btnSenden.Enabled = false;
            _btnSuchen.Enabled = false;
            _btnWerkzeuge.Enabled = false;
            _lblStatus.Text = "Der Assistent denkt nach...";
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                // Mit eingeschaltetem Aktionsbetrieb laeuft die Werkzeugrunde; sonst
                // bleibt es beim reinen Hilfefall mit Antwort-Cache.
                KiAntwort antwort = _chkAktionen.Checked
                    ? await KiChatService.FrageMitAktionenAsync(frage, _kontext, _verlauf, _platzhalter)
                    : await KiChatService.FrageAsync(frage, _kontext, _verlauf);

                if (antwort.Schritte.Count > 0 || antwort.Hinweise.Count > 0) SchritteZeigen(antwort);

                if (antwort.Erfolg)
                {
                    SchreibeZeile("Assistent:", Color.FromArgb(0, 90, 160), true);
                    SchreibeZeile(antwort.Text, Color.Black, false);

                    if (antwort.Quellen.Count > 0)
                        SchreibeZeile("Quellen: " + string.Join(", ", antwort.Quellen), Color.DimGray, false);

                    if (antwort.AusCache)
                        SchreibeZeile("(aus dem lokalen Zwischenspeicher - ohne erneute Anfrage)",
                            Color.DimGray, false);

                    _verlauf.Add("Assistent: " + Kuerzen(antwort.Text, 400));

                    _lblStatus.Text = "Heute genutzt: " + KiChatService.AnfragenHeute +
                                      " von " + KiChatService.Tageslimit;
                }
                else
                {
                    SchreibeZeile("Hinweis: " + antwort.Fehler, Color.FromArgb(170, 0, 0), false);

                    // Ersatzweise die lokalen Treffer anbieten
                    if (treffer.Count > 0)
                    {
                        SchreibeZeile("Passende Hilfeabschnitte:", Color.FromArgb(0, 90, 160), true);
                        foreach (WissensAbschnitt a in treffer)
                            SchreibeZeile("• " + a.Titel + ": " + Kuerzen(a.Inhalt, 200), Color.DimGray, false);
                    }
                    _lblStatus.Text = "";
                }
            }
            finally
            {
                SchreibeZeile("", Color.Black, false);
                _beschaeftigt = false;
                SperreAktualisieren();
                Cursor.Current = Cursors.Default;
                _eingabe.Focus();
            }
        }

        // ------------------------------------------------------------------
        // Aktionsbetrieb (Etappe 2)
        // ------------------------------------------------------------------

        /// <summary>
        /// Zeigt, was in den Runden geschehen ist: Hinweise zum gewaehlten Weg, jede
        /// Aktion mit Angaben und die zugehoerige Protokollzeile.
        /// </summary>
        private void SchritteZeigen(KiAntwort antwort)
        {
            foreach (string hinweis in antwort.Hinweise)
                SchreibeZeile(hinweis, Color.FromArgb(160, 80, 0), false);

            foreach (KiSchritt schritt in antwort.Schritte)
            {
                string bezeichnung = schritt.Kurzfassung.Length > 0 ? schritt.Kurzfassung : schritt.Aktion;

                if (schritt.Ausgefuehrt)
                {
                    SchreibeZeile(string.Format(MyResource.Resource.KI_AKT_AUSGEFUEHRT, bezeichnung),
                                  Color.FromArgb(0, 120, 0), false);

                    if (schritt.Ergebnis != null && schritt.Ergebnis.Zeilen.Count > 0)
                        SchreibeZeile(string.Format(MyResource.Resource.KI_AKT_ERGEBNISZEILEN,
                                                    schritt.Ergebnis.Zeilen.Count), Color.DimGray, false);
                }
                else
                {
                    SchreibeZeile(string.Format(MyResource.Resource.KI_AKT_NICHT_AUSGEFUEHRT,
                                                bezeichnung, schritt.Grund),
                                  Color.FromArgb(170, 0, 0), false);
                }

                if (schritt.Ergebnis != null)
                    foreach (string meldung in schritt.Ergebnis.Meldungen)
                        SchreibeZeile("   " + meldung, Color.DimGray, false);

                // Der Sicherungspunkt gehört sichtbar in den Verlauf, nicht nur ins
                // Protokoll (Fachkonzept 4.4, Punkt 1).
                if (schritt.Sicherungspunkt.Length > 0)
                    SchreibeZeile("   " + KiTexte.FeldSicherung + ": " + schritt.Sicherungspunkt,
                                  Color.DimGray, false);

                if (schritt.Protokollzeile.Length > 0)
                    SchreibeZeile(string.Format(MyResource.Resource.KI_AKT_PROTOKOLLZEILE,
                                                schritt.Protokollzeile), Color.DimGray, false);
            }

            if (antwort.Runden > 0)
            {
                string weg = antwort.WegB ? MyResource.Resource.KI_AKT_WEG_B
                                          : MyResource.Resource.KI_AKT_WEG_A;
                SchreibeZeile(string.Format(MyResource.Resource.KI_AKT_RUNDEN, antwort.Runden, weg),
                              Color.DimGray, false);
            }
        }

        /// <summary>
        /// Die Werkzeugliste zur Auswahl VON HAND (Fachkonzept 8, Etappe 1): Aktion
        /// waehlen, Angaben eintragen, ausfuehren. Ohne Modell und ohne Kosten.
        /// </summary>
        private void WerkzeugeOeffnen()
        {
            KiRegister register = KiAusfuehrer.Register;
            KiAktion gewaehlt = null;
            IReadOnlyDictionary<string, object> werte = null;

            using (Form frm = new Form())
            {
                frm.Text = MyResource.Resource.KI_AKT_WERKZEUGE_TITEL;
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.MinimizeBox = false;
                frm.MaximizeBox = false;
                frm.ShowInTaskbar = false;
                frm.ClientSize = new Size(780, 520);
                frm.MinimumSize = new Size(640, 420);

                ListBox liste = new ListBox { Dock = DockStyle.Left, Width = 240, IntegralHeight = false };
                foreach (KiAktion a in register.Alle) liste.Items.Add(a.Name);

                TextBox beschreibung = new TextBox
                {
                    Dock = DockStyle.Top,
                    Height = 150,
                    Multiline = true,
                    ReadOnly = true,
                    ScrollBars = ScrollBars.Vertical,
                    BackColor = Color.White,
                    Font = new Font("Segoe UI", 9f)
                };

                Panel felder = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(8) };
                Panel rechts = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 0, 0, 0) };
                rechts.Controls.Add(felder);          // Fill zuerst
                rechts.Controls.Add(beschreibung);

                Dictionary<string, Control> eingaben = new Dictionary<string, Control>(StringComparer.Ordinal);

                liste.SelectedIndexChanged += (s, e) =>
                {
                    gewaehlt = liste.SelectedIndex >= 0 ? register.Alle[liste.SelectedIndex] : null;
                    beschreibung.Text = gewaehlt != null
                        ? KiBestaetigung.Beschreibe(gewaehlt).Replace("\n", Environment.NewLine)
                        : "";
                    FelderBauen(felder, gewaehlt, eingaben);
                };

                Button ausfuehren = new Button
                {
                    Text = MyResource.Resource.KI_AKT_AUSFUEHREN,
                    DialogResult = DialogResult.OK,
                    Width = 110,
                    Height = 28
                };
                Button schliessen = new Button
                {
                    Text = MyResource.Resource.KI_VORSCHAU_SCHLIESSEN,
                    DialogResult = DialogResult.Cancel,
                    Width = 110,
                    Height = 28
                };

                ausfuehren.Click += (s, e) =>
                {
                    if (gewaehlt == null)
                    {
                        MessageBox.Show(frm, MyResource.Resource.KI_AKT_AKTION_WAEHLEN, frm.Text,
                                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                        frm.DialogResult = DialogResult.None;
                        return;
                    }
                    werte = WerteSammeln(gewaehlt, eingaben);
                };

                FlowLayoutPanel fuss = new FlowLayoutPanel
                {
                    Dock = DockStyle.Bottom,
                    FlowDirection = FlowDirection.RightToLeft,
                    AutoSize = true,
                    AutoSizeMode = AutoSizeMode.GrowAndShrink,
                    WrapContents = false,
                    Padding = new Padding(8)
                };
                fuss.Controls.Add(schliessen);
                fuss.Controls.Add(ausfuehren);

                frm.Controls.Add(rechts);             // Fill zuerst
                frm.Controls.Add(liste);
                frm.Controls.Add(fuss);
                frm.CancelButton = schliessen;

                if (liste.Items.Count > 0) liste.SelectedIndex = 0;

                if (frm.ShowDialog(this) != DialogResult.OK) return;
            }

            if (gewaehlt == null || werte == null) return;

            // Erst NACH dem Schliessen des Dialogs ausfuehren: der Ausfuehrer weist
            // Aktionen ab, solange ein modales Fenster offen ist (Fachkonzept 3.4).
            AktionVonHandAusfuehren(gewaehlt.Name, werte);
        }

        /// <summary>Baut die Eingabefelder der gewaehlten Aktion.</summary>
        private static void FelderBauen(Panel ziel, KiAktion aktion, Dictionary<string, Control> eingaben)
        {
            ziel.Controls.Clear();
            eingaben.Clear();
            if (aktion == null) return;

            TableLayoutPanel tabelle = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 2,
                RowCount = aktion.Parameter.Count
            };
            tabelle.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
            tabelle.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

            foreach (KiParameter p in aktion.Parameter)
            {
                Label lbl = new Label
                {
                    Text = p.Anzeigename + (p.Pflicht ? " *" : ""),
                    AutoSize = true,
                    Anchor = AnchorStyles.Left,
                    Padding = new Padding(0, 6, 6, 0)
                };

                Control feld;
                if (p.Typ == KiParameterTyp.Aufzaehlung && p.Werte.Count > 0)
                {
                    ComboBox box = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
                    if (!p.Pflicht) box.Items.Add("");
                    foreach (string w in p.Werte) box.Items.Add(w);
                    if (box.Items.Count > 0) box.SelectedIndex = 0;
                    feld = box;
                }
                else if (p.Typ == KiParameterTyp.Wahrheitswert)
                {
                    ComboBox box = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
                    box.Items.Add("");
                    box.Items.Add("true");
                    box.Items.Add("false");
                    box.SelectedIndex = 0;
                    feld = box;
                }
                else
                {
                    feld = new TextBox { Dock = DockStyle.Fill };
                }

                feld.Tag = p;
                eingaben[p.Name] = feld;

                tabelle.Controls.Add(lbl);
                tabelle.Controls.Add(feld);
            }

            ziel.Controls.Add(tabelle);
        }

        /// <summary>
        /// Sammelt die Rohwerte. Zahlen werden hier - und nur hier - von der Anzeige- in
        /// die invariante Schreibweise gebracht (Kulturregel, Fachkonzept 3.2).
        /// </summary>
        private static IReadOnlyDictionary<string, object> WerteSammeln(KiAktion aktion,
                                                                        Dictionary<string, Control> eingaben)
        {
            Dictionary<string, object> werte = new Dictionary<string, object>(StringComparer.Ordinal);

            foreach (KiParameter p in aktion.Parameter)
            {
                Control feld;
                if (!eingaben.TryGetValue(p.Name, out feld)) continue;

                string text = (feld.Text ?? "").Trim();
                if (text.Length == 0) continue;          // Leeres Feld = nicht angegeben

                if (p.Typ == KiParameterTyp.GanzzahlListe)
                {
                    werte[p.Name] = text.Split(new[] { ',', ';', ' ', '\t' },
                                               StringSplitOptions.RemoveEmptyEntries);
                }
                else if (p.Typ == KiParameterTyp.Zahl || p.Typ == KiParameterTyp.Ganzzahl)
                {
                    werte[p.Name] = text.Replace(",", ".");
                }
                else
                {
                    werte[p.Name] = text;
                }
            }
            return werte;
        }

        /// <summary>Fuehrt eine von Hand gewaehlte Aktion aus und schreibt das Ergebnis in den Chat.</summary>
        private async void AktionVonHandAusfuehren(string aktion, IReadOnlyDictionary<string, object> werte)
        {
            SchreibeZeile("Sie: " + aktion, Color.FromArgb(0, 100, 0), true);

            _beschaeftigt = true;
            SperreAktualisieren();
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                KiErgebnis ergebnis = await KiAusfuehrer.AusfuehrenAsync(aktion, werte);

                KiSchritt schritt = new KiSchritt
                {
                    Aktion = aktion,
                    Kurzfassung = aktion,
                    Ausgefuehrt = ergebnis.Erfolg,
                    Grund = ergebnis.Erfolg ? "" : ergebnis.Text,
                    Ergebnis = ergebnis,
                    Protokollzeile = KiAusfuehrer.LetzteProtokollzeile
                };

                KiAntwort anzeige = new KiAntwort();
                anzeige.Schritte.Add(schritt);
                SchritteZeigen(anzeige);

                if (ergebnis.Erfolg && ergebnis.Text.Length > 0)
                    SchreibeZeile(ergebnis.Text, Color.Black, false);
            }
            finally
            {
                SchreibeZeile("", Color.Black, false);
                _beschaeftigt = false;
                SperreAktualisieren();
                Cursor.Current = Cursors.Default;
            }
        }

        /// <summary>
        /// Zeigt das Aktionsprotokoll. Es liegt neben der Datenbank, damit Protokoll und
        /// Datenstand zusammen gesichert werden (Fachkonzept 3.6).
        /// </summary>
        private void ProtokollZeigen()
        {
            string pfad = KiAusfuehrer.ProtokollPfad();
            string inhalt;

            if (string.IsNullOrEmpty(pfad) || !File.Exists(pfad))
            {
                inhalt = string.Format(MyResource.Resource.KI_AKT_PROTOKOLL_FEHLT, pfad ?? "?");
            }
            else
            {
                try
                {
                    string[] zeilen = File.ReadAllLines(pfad, Encoding.UTF8);
                    int ab = Math.Max(0, zeilen.Length - 400);
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(pfad);
                    sb.AppendLine();
                    for (int i = ab; i < zeilen.Length; i++) sb.AppendLine(zeilen[i]);
                    inhalt = sb.ToString();
                }
                catch (Exception ex)
                {
                    inhalt = ex.Message;
                }
            }

            // Die Maske selbst steht in Form_TextAnzeige - sie war wortgleich mit der
            // Vorschau (VorschauZeigen). Maße und Maximieren-Schaltfläche werden
            // mitgegeben, damit das Protokollfenster genau so groß bleibt wie bisher.
            using (Form_TextAnzeige frm = new Form_TextAnzeige(
                       MyResource.Resource.KI_AKT_PROTOKOLL_TITEL, inhalt, null,
                       new Size(900, 480), new Size(520, 320), true))
            {
                frm.ShowDialog(this);
            }
        }

        /// <summary>Dialog für API-Schlüssel und Tageslimit.</summary>
        /// <remarks>
        /// Der Fensteraufbau steht seit der Designer-Umstellung in
        /// <see cref="Form_KiEinstellungen"/>; hier bleibt genau das, was schon
        /// vorher hier stand: das Speichern nach OK und die Rückmeldung im
        /// Verlauf. Neu ist allein das <c>using</c> — das frühere
        /// <c>new Form()</c> wurde nie entsorgt.
        /// </remarks>
        private void EinstellungenOeffnen()
        {
            using (Form_KiEinstellungen frm = new Form_KiEinstellungen())
            {
                if (frm.ShowDialog(this) != DialogResult.OK) return;

                KiChatService.ApiKey = frm.ApiSchluessel;
                KiChatService.WegBErzwingen = frm.WegBErzwingen;

                SchreibeZeile(KiChatService.IstEingerichtet
                    ? MyResource.Resource.KI_EINST_MSG_GESPEICHERT
                    : MyResource.Resource.KI_EINST_MSG_GESPEICHERT_OHNE_SCHLUESSEL,
                    Color.FromArgb(0, 120, 0), false);
                SchreibeZeile("", Color.Black, false);
            }
        }

        /// <summary>
        /// Selbstprüfung (A5): zeigt genau den Text, den die nächste Frage an den
        /// Anbieter senden würde. Es wird dabei nichts gesendet und nichts gezählt.
        /// </summary>
        private void VorschauZeigen()
        {
            string text;
            try
            {
                // Mit eingeschaltetem Aktionsbetrieb geht der WERKZEUGKATALOG mit -
                // die Selbstpruefung muss ihn deshalb ebenfalls zeigen.
                text = KiChatService.SendeVorschau(_eingabe.Text, _kontext, _verlauf,
                                                   _chkAktionen.Checked);
            }
            catch (Exception ex)
            {
                text = ex.Message;
            }

            string kopf = string.Format(MyResource.Resource.KI_VORSCHAU_HINWEIS,
                                        KiChatService.MODELL, KiChatService.Endpunkt());

            // Dieselbe Maske wie das Aktionsprotokoll (ProtokollZeigen), nur mit
            // Kopfzeile, kleinerem Fenster und ohne Maximieren-Schaltfläche.
            using (Form_TextAnzeige frm = new Form_TextAnzeige(
                       MyResource.Resource.KI_VORSCHAU_TITEL, text, kopf,
                       new Size(720, 520), new Size(520, 360), false))
            {
                frm.ShowDialog(this);
            }
        }

        // ------------------------------------------------------------------
        // Hilfsfunktionen
        // ------------------------------------------------------------------

        private void SchreibeZeile(string text, Color farbe, bool fett)
        {
            // Beim Schließen des Fensters wird die offene Vorschau noch beantwortet -
            // dann kann die Anzeige bereits verworfen sein.
            if (_verlaufAnzeige == null || _verlaufAnzeige.IsDisposed) return;

            _verlaufAnzeige.SelectionStart = _verlaufAnzeige.TextLength;
            _verlaufAnzeige.SelectionLength = 0;
            _verlaufAnzeige.SelectionColor = farbe;
            _verlaufAnzeige.SelectionFont = new Font(_verlaufAnzeige.Font,
                fett ? FontStyle.Bold : FontStyle.Regular);
            _verlaufAnzeige.AppendText(text + Environment.NewLine);
            _verlaufAnzeige.SelectionColor = _verlaufAnzeige.ForeColor;
            _verlaufAnzeige.ScrollToCaret();
        }

        private static string Kuerzen(string text, int laenge)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= laenge) return text ?? "";
            return text.Substring(0, laenge) + "...";
        }

        /// <summary>
        /// Öffnet den Assistenten mit dem aktuell erkannten Bedienkontext.
        /// Bequemer Einstiegspunkt für Menü, Schaltflächen oder Tastenkürzel.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Auch bei abgeschalteter KI öffnet das Fenster.</b> Bis Paket F5 endete
        /// dieser Weg mit einer Hinweismeldung, der Einstieg war also ganz zu. Seither
        /// gilt der Hilfe-Betrieb (Fachkonzept 11.9): Das Fenster geht auf und arbeitet
        /// als reine Hilfesuche - die Hilfe liegt lokal vor, kostet nichts und ist
        /// gerade dann nützlich, wenn der Dienst nicht zur Verfügung steht. Über seinen
        /// Betrieb entscheidet das Fenster selbst (<see cref="HilfeBetriebAnwenden"/>).
        /// </para>
        /// <para>
        /// <b>Warum jeder Aufruf ein neues Fenster anlegt.</b> Das ist Bestand und bleibt
        /// so; der Aufrufknopf der Masken (<c>KiAufrufKnopf</c>) holt ein bereits offenes
        /// Fenster auf seiner Seite nach vorn. Für den Hilfe-Betrieb ist das erwünscht:
        /// Jedes Öffnen liest den Schalter neu.
        /// </para>
        /// <para>
        /// <b>Keine Schutzwirkung geht verloren.</b> Dass ohne Einwilligung und bei
        /// gesetztem Abschalter nichts hinausgeht, trägt
        /// <c>KiEinwilligung.Sicherstellen</c> und der Einwilligungsriegel in
        /// <c>KiChatService</c> - nicht das geschlossene Fenster.
        /// </para>
        /// </remarks>
        public static void Oeffnen(IWin32Window besitzer = null)
        {
            // Kontext ermitteln, SOLANGE das aufrufende Fenster noch aktiv ist
            string kontext = HilfeKontext.Beschreibung();

            Form_KiChat frm = new Form_KiChat();
            frm.SetzeKontext(kontext);

            if (besitzer != null) frm.Show(besitzer); else frm.Show();
        }
    }
}
