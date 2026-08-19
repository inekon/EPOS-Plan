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
        private CheckBox _chkAktionen;
        private Button _btnWerkzeuge;
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
            Panel hinweisZeile = new Panel { Dock = DockStyle.Bottom, Height = 24, Padding = new Padding(8, 2, 8, 0) };

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
            hinweisZeile.Controls.Add(_linkHinweis);

            // --- Eingabebereich ---
            Panel eingabeBereich = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8, 8, 8, 4) };

            _btnSenden = new Button { Text = "Fragen", Width = 110, Height = 30, Margin = new Padding(0, 0, 0, 6) };
            _btnSenden.Click += async (s, e) => await FrageStellen(true);

            _btnSuchen = new Button { Text = "Nur suchen", Width = 110, Height = 30 };
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
            Panel schalter = new Panel { Dock = DockStyle.Top, Height = 32, Padding = new Padding(8, 4, 8, 0) };

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

            schalter.Controls.Add(_chkAktionen);
            schalter.Controls.Add(_btnWerkzeuge);

            unten.Controls.Add(eingabeBereich);           // Fill zuerst
            unten.Controls.Add(leiste);
            unten.Controls.Add(hinweisZeile);            // dockt oberhalb der Leiste an
            unten.Controls.Add(schalter);

            BaueBestaetigungsblock();

            this.Controls.Add(_verlaufAnzeige);
            this.Controls.Add(_bestaetigungBereich);
            this.Controls.Add(_lblKontext);
            this.Controls.Add(unten);
            this.CancelButton = btnSchliessen;

            // Sperranzeige: solange eine Assistentenaktion laeuft, bleiben Fragen und
            // Werkzeugliste gesperrt. Die Aktion kann auch von woanders angestossen
            // worden sein - deshalb wird KiAusfuehrer.Belegt zyklisch abgefragt und nicht
            // nur beim eigenen Aufruf gesetzt.
            _sperrUhr = new System.Windows.Forms.Timer { Interval = 400 };
            _sperrUhr.Tick += (s, e) => SperreAktualisieren();
            _sperrUhr.Start();

            Begruessung();
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
            // Enter sendet, Shift+Enter erzeugt eine neue Zeile
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                await FrageStellen(true);
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

            using (Form frm = new Form())
            {
                frm.Text = MyResource.Resource.KI_AKT_PROTOKOLL_TITEL;
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.MinimizeBox = false;
                frm.ShowInTaskbar = false;
                frm.ClientSize = new Size(900, 480);
                frm.MinimumSize = new Size(520, 320);

                TextBox anzeige = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    WordWrap = false,
                    ScrollBars = ScrollBars.Both,
                    BackColor = Color.White,
                    Font = new Font("Consolas", 9f),
                    Text = inhalt
                };

                Button schliessen = new Button
                {
                    Text = MyResource.Resource.KI_VORSCHAU_SCHLIESSEN,
                    DialogResult = DialogResult.OK,
                    Width = 100,
                    Height = 28
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

                frm.Controls.Add(anzeige);            // Fill zuerst
                frm.Controls.Add(fuss);
                frm.AcceptButton = schliessen;
                frm.CancelButton = schliessen;

                frm.ShowDialog(this);
            }
        }

        /// <summary>Dialog für API-Schlüssel und Tageslimit.</summary>
        private void EinstellungenOeffnen()
        {
            Form frm = new Form();
            frm.Text = "KI-Assistent - Einstellungen";
            frm.FormBorderStyle = FormBorderStyle.FixedDialog;
            frm.StartPosition = FormStartPosition.CenterParent;
            frm.MinimizeBox = false;
            frm.MaximizeBox = false;
            frm.ClientSize = new Size(500, 276);

            Label l1 = new Label
            {
                Text = "API-Schlüssel (Google AI Studio):",
                AutoSize = true,
                Location = new Point(14, 18)
            };
            TextBox tbKey = new TextBox
            {
                Location = new Point(14, 42),
                Width = 470,
                UseSystemPasswordChar = true,
                Text = KiChatService.ApiKey
            };

            Label l2 = new Label
            {
                Text = "Tageslimit je Arbeitsplatz:",
                AutoSize = true,
                Location = new Point(14, 82)
            };
            // Nur Anzeige: Das Limit wird maschinenweit vorgegeben und soll
            // vom Anwender nicht angehoben werden koennen.
            Label lblLimit = new Label
            {
                Text = KiChatService.Tageslimit + " (fest vorgegeben)",
                AutoSize = true,
                Location = new Point(200, 82),
                ForeColor = Color.DimGray
            };
            new ToolTip().SetToolTip(lblLimit,
                "Fest im Programm hinterlegt und nicht änderbar - weder hier noch über " +
                "eine Einstellung. Eine Änderung erfordert einen neuen Programmstand.");

            Button btnModell = new Button
            {
                Text = "Modell neu erkennen",
                Location = new Point(390, 78),
                Size = new Size(94, 24)
            };

            Label hinweis = new Label
            {
                AutoSize = false,
                Location = new Point(14, 118),
                Size = new Size(470, 88),
                ForeColor = Color.DimGray,
                Text = "Modell: " + KiChatService.MODELL + " (kostengünstige Klasse).\n\n" +
                       "Es werden ausschließlich Hilfetexte, Ihre Frage und der Bereichsname " +
                       "übertragen - keine Projekt-, Kunden- oder Simulationsdaten.\n\n" +
                       "Hinweis: Im kostenlosen Kontingent verwendet der Anbieter die Inhalte zur " +
                       "Produktverbesserung. Für den produktiven Einsatz einen kostenpflichtigen " +
                       "Zugang nutzen."
            };

            CheckBox chkWegB = new CheckBox
            {
                Text = MyResource.Resource.KI_AKT_WEGB_EINSTELLUNG,
                AutoSize = true,
                Location = new Point(14, 208),
                Checked = KiChatService.WegBErzwingen
            };

            Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(314, 236), Width = 80 };
            Button abbruch = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Location = new Point(400, 236), Width = 84 };

            btnModell.Click += (s, e) =>
            {
                // Schlüssel zuerst übernehmen, damit die Abfrage funktioniert
                KiChatService.ApiKey = tbKey.Text.Trim();
                KiChatService.ModellZuruecksetzen();
                hinweis.Text = "Modell: " + KiChatService.MODELL + " (Vorgabe - wird beim nächsten " +
                    "Aufruf automatisch geprüft und bei Bedarf durch ein verfügbares ersetzt)." +
                    hinweis.Text.Substring(hinweis.Text.IndexOf("\n\n"));
            };

            frm.Controls.Add(l1); frm.Controls.Add(tbKey);
            frm.Controls.Add(l2); frm.Controls.Add(lblLimit);
            frm.Controls.Add(btnModell);
            frm.Controls.Add(hinweis);
            frm.Controls.Add(chkWegB);
            frm.Controls.Add(ok); frm.Controls.Add(abbruch);
            frm.AcceptButton = ok;
            frm.CancelButton = abbruch;

            if (frm.ShowDialog(this) != DialogResult.OK) return;

            KiChatService.ApiKey = tbKey.Text.Trim();
            KiChatService.WegBErzwingen = chkWegB.Checked;

            SchreibeZeile(KiChatService.IstEingerichtet
                ? "Einstellungen gespeichert - der Assistent ist einsatzbereit."
                : "Einstellungen gespeichert - ohne Schlüssel bleibt nur die lokale Suche aktiv.",
                Color.FromArgb(0, 120, 0), false);
            SchreibeZeile("", Color.Black, false);
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

            using (Form frm = new Form())
            {
                frm.Text = MyResource.Resource.KI_VORSCHAU_TITEL;
                frm.StartPosition = FormStartPosition.CenterParent;
                frm.MinimizeBox = false;
                frm.MaximizeBox = false;
                frm.ShowInTaskbar = false;
                frm.ClientSize = new Size(720, 520);
                frm.MinimumSize = new Size(520, 360);

                TextBox anzeige = new TextBox
                {
                    Dock = DockStyle.Fill,
                    Multiline = true,
                    ReadOnly = true,
                    WordWrap = false,
                    ScrollBars = ScrollBars.Both,
                    BackColor = Color.White,
                    Font = new Font("Consolas", 9f),
                    Text = text
                };

                Label kopf = new Label
                {
                    Dock = DockStyle.Top,
                    Height = 66,
                    Padding = new Padding(10, 8, 10, 4),
                    ForeColor = Color.DimGray,
                    Text = string.Format(MyResource.Resource.KI_VORSCHAU_HINWEIS,
                                         KiChatService.MODELL, KiChatService.Endpunkt())
                };

                Button schliessen = new Button
                {
                    Text = MyResource.Resource.KI_VORSCHAU_SCHLIESSEN,
                    DialogResult = DialogResult.OK,
                    Width = 100,
                    Height = 28
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

                // Reihenfolge beachten: Fill zuerst, dann die andockenden Elemente
                frm.Controls.Add(anzeige);
                frm.Controls.Add(kopf);
                frm.Controls.Add(fuss);
                frm.AcceptButton = schliessen;
                frm.CancelButton = schliessen;

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
        /// Ist der Assistent für diese Installation abgeschaltet, öffnet sich kein
        /// Fenster, sondern es kommt eine klare Meldung. Der Menüeintrag wird dann
        /// ohnehin ausgeblendet - die Meldung fängt die übrigen Wege ab (F1,
        /// Schaltflächen, spätere Umschaltung im laufenden Programm).
        /// </remarks>
        public static void Oeffnen(IWin32Window besitzer = null)
        {
            if (KiEinwilligung.Abgeschaltet)
            {
                MessageBox.Show(besitzer ?? (IWin32Window)Form.ActiveForm,
                                MyResource.Resource.KI_ABSCHALTER_MELDUNG,
                                MyResource.Resource.KI_ABSCHALTER_TITEL,
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Kontext ermitteln, SOLANGE das aufrufende Fenster noch aktiv ist
            string kontext = HilfeKontext.Beschreibung();

            Form_KiChat frm = new Form_KiChat();
            frm.SetzeKontext(kontext);

            if (besitzer != null) frm.Show(besitzer); else frm.Show();
        }
    }
}
