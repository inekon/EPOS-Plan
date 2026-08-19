using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zeigt die rechtlichen Informationen zu EPOS-Plan an (Menü "Hilfe &gt; Lizenz").
    ///
    /// Der Dialog gliedert sich in drei Registerkarten:
    ///   1. Lizenzvereinbarung  - der verbindliche Vertragstext aus "LIZENZ-INEKON.rtf"
    ///   2. Rechtliche Hinweise - Anbieter, Nutzungsumfang, Haftung, Datenverarbeitung
    ///   3. Komponenten         - verwendete Fremdkomponenten und Datenquellen
    ///
    /// Verbindlich ist ausschließlich die Lizenzvereinbarung auf der ersten
    /// Registerkarte; die übrigen Seiten fassen den Inhalt lesbar zusammen und
    /// verweisen darauf.
    ///
    /// Komplett programmatisch aufgebaut (kein Designer, keine .resx).
    /// </summary>
    public class Form_Lizenz : Form
    {
        /// <summary>Dateinamen, nach denen gesucht wird (in dieser Reihenfolge).</summary>
        private static readonly string[] DATEINAMEN =
        {
            "LIZENZ-INEKON.rtf",
            "LIZENZVEREINBARUNG UND ALLGEMEINE GESCHÄFTSBEDINGUNGEN- Wärmeplan.docx"
        };

        private const string REG_SCHLUESSEL = @"Software\wp-plan";
        private const string REG_ZUSTIMMUNG = "LizenzZugestimmt";

        /// <summary>Vom Anwender gewaehlter Pfad der Lizenzdatei (Registry).</summary>
        private const string REG_LIZENZDATEI = "LizenzDatei";

        /// <summary>Die jeweils geltende Fassung steht online; die App zeigt eine Kopie.</summary>
        private const string ONLINE_FASSUNG = "https://epos-plan.de/agb/";

        /// <summary>
        /// Dieselbe Seite über die WordPress-Schnittstelle - liefert den reinen
        /// Vertragstext ohne Menü und Fußbereich, dazu das Änderungsdatum.
        /// </summary>
        private const string ONLINE_QUELLE =
            "https://epos-plan.de/wp-json/wp/v2/pages?slug=agb&_fields=modified,content";

        private TabControl _register;
        private RichTextBox _text;
        private RichTextBox _hinweise;
        private RichTextBox _komponenten;
        private TextBox _suche;
        private Label _lblQuelle;
        private string _gefundeneDatei = "";
        private float _schriftgroesse = 9.5f;

        private PrintDocument _druck;
        private string _druckText = "";
        private int _druckPosition = 0;
        private int _druckSeite = 0;

        /// <summary>true, wenn der Dialog als Zustimmungsabfrage geöffnet wurde.</summary>
        private readonly bool _zustimmungAbfragen;

        public Form_Lizenz() : this(false) { }

        public Form_Lizenz(bool zustimmungAbfragen)
        {
            _zustimmungAbfragen = zustimmungAbfragen;
            BaueOberflaeche();
            LizenzLaden();
            RechtlicheHinweiseFuellen();
            KomponentenFuellen();
        }

        // ------------------------------------------------------------------
        // Oberfläche
        // ------------------------------------------------------------------

        private void BaueOberflaeche()
        {
            this.Text = _zustimmungAbfragen
                ? "EPOS-Plan - Lizenzvereinbarung"
                : "EPOS-Plan - Lizenz und rechtliche Hinweise";
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(920, 700);
            this.MinimumSize = new Size(660, 480);
            this.MinimizeBox = false;
            this.ShowIcon = false;

            // --- Kopfzeile ---
            Panel kopf = new Panel { Dock = DockStyle.Top, Height = 58, BackColor = Color.White };
            kopf.Paint += (s, e) =>
            {
                using (Pen stift = new Pen(Color.FromArgb(222, 227, 232)))
                    e.Graphics.DrawLine(stift, 0, kopf.Height - 1, kopf.Width, kopf.Height - 1);
            };

            Label titel = new Label
            {
                Text = "Lizenz und rechtliche Hinweise",
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 90, 160),
                Location = new Point(18, 10)
            };
            Label untertitel = new Label
            {
                Text = "EPOS-Plan - Energieplanungs-Software - INEKON, Intelligente Energiekonzepte",
                AutoSize = true,
                Font = new Font("Segoe UI", 8.25f),
                ForeColor = Color.FromArgb(112, 119, 126),
                Location = new Point(20, 34)
            };
            kopf.Controls.Add(titel);
            kopf.Controls.Add(untertitel);

            // --- Registerkarten ---
            _register = new TabControl { Dock = DockStyle.Fill, Padding = new Point(14, 5) };

            TabPage seiteVertrag = new TabPage("Lizenzvereinbarung") { BackColor = Color.White, Padding = new Padding(0) };
            TabPage seiteHinweise = new TabPage("Rechtliche Hinweise") { BackColor = Color.White, Padding = new Padding(0) };
            TabPage seiteKomponenten = new TabPage("Komponenten") { BackColor = Color.White, Padding = new Padding(0) };

            _text = NeueAnzeige();
            _text.LinkClicked += (s, e) => LinkOeffnen(e.LinkText);

            // Werkzeugleiste über dem Vertragstext: Suche und Schriftgröße
            Panel werkzeuge = new Panel { Dock = DockStyle.Top, Height = 38, BackColor = Color.FromArgb(250, 251, 252), Padding = new Padding(10, 6, 10, 6) };

            _suche = new TextBox { Width = 220, Dock = DockStyle.Left };
            _suche.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; Suchen(); } };

            Button btnSuchen = new Button { Text = "Suchen", Width = 90, Dock = DockStyle.Left, Margin = new Padding(6, 0, 0, 0) };
            btnSuchen.Click += (s, e) => Suchen();

            // Kein Dock: Kinder eines FlowLayoutPanel melden damit keine
            // Vorzugsgröße, das AutoSize-Panel bleibt 0 breit und die
            // Schaltflächen sind unsichtbar.
            Button btnGroesser = new Button { Text = "A+", Width = 44, Height = 26, Margin = new Padding(6, 0, 0, 0) };
            btnGroesser.Click += (s, e) => SchriftAendern(+1f);
            Button btnKleiner = new Button { Text = "A-", Width = 44, Height = 26, Margin = new Padding(6, 0, 0, 0) };
            btnKleiner.Click += (s, e) => SchriftAendern(-1f);

            FlowLayoutPanel links = new FlowLayoutPanel
            {
                Dock = DockStyle.Left,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };
            links.Controls.Add(new Label { Text = "Im Text suchen:", AutoSize = true, Margin = new Padding(0, 6, 8, 0), ForeColor = Color.DimGray });
            links.Controls.Add(_suche);
            links.Controls.Add(btnSuchen);

            FlowLayoutPanel rechts = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };
            Button btnWaehlen = new Button { Text = "Datei wählen...", Width = 130, Height = 26, Margin = new Padding(6, 0, 0, 0) };
            btnWaehlen.Click += (s, e) => LizenzDateiWaehlen();

            rechts.Controls.Add(btnGroesser);
            rechts.Controls.Add(btnKleiner);
            rechts.Controls.Add(btnWaehlen);

            werkzeuge.Controls.Add(links);
            werkzeuge.Controls.Add(rechts);

            seiteVertrag.Controls.Add(_text);        // Fill zuerst
            seiteVertrag.Controls.Add(werkzeuge);

            _hinweise = NeueAnzeige();
            _hinweise.LinkClicked += (s, e) => LinkOeffnen(e.LinkText);
            seiteHinweise.Controls.Add(_hinweise);

            _komponenten = NeueAnzeige();
            _komponenten.LinkClicked += (s, e) => LinkOeffnen(e.LinkText);
            seiteKomponenten.Controls.Add(_komponenten);

            _register.TabPages.Add(seiteVertrag);
            _register.TabPages.Add(seiteHinweise);
            _register.TabPages.Add(seiteKomponenten);

            // --- Fußzeile ---
            Panel unten = new Panel { Dock = DockStyle.Bottom, Height = 68, Padding = new Padding(12, 10, 12, 10) };

            _lblQuelle = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                AutoEllipsis = true,
                TextAlign = ContentAlignment.MiddleLeft
            };

            FlowLayoutPanel schalter = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = Color.Transparent
            };

            Button btnDrucken = new Button { Text = "Drucken...", Width = 110, Height = 30, Margin = new Padding(6, 0, 0, 0) };
            btnDrucken.Click += (s, e) => Drucken();

            Button btnSpeichern = new Button { Text = "Speichern unter...", Width = 140, Height = 30, Margin = new Padding(6, 0, 0, 0) };
            btnSpeichern.Click += (s, e) => SpeichernUnter();

            Button btnAktivieren = new Button { Text = "Lizenz aktivieren...", Width = 160, Height = 30, Margin = new Padding(6, 0, 0, 0) };
            btnAktivieren.Click += (s, e) => LizenzVerwaltungOeffnen();

            if (_zustimmungAbfragen)
            {
                Button btnAblehnen = new Button
                {
                    Text = "Ablehnen",
                    Width = 110,
                    Height = 30,
                    Margin = new Padding(6, 0, 0, 0),
                    DialogResult = DialogResult.Cancel
                };
                Button btnZustimmen = new Button
                {
                    Text = "Zustimmen",
                    Width = 130,
                    Height = 30,
                    Margin = new Padding(6, 0, 0, 0),
                    DialogResult = DialogResult.OK
                };
                btnZustimmen.Click += (s, e) => ZustimmungMerken();

                // RightToLeft: zuerst hinzugefügt = ganz rechts
                schalter.Controls.Add(btnZustimmen);
                schalter.Controls.Add(btnAblehnen);
                schalter.Controls.Add(btnDrucken);
                schalter.Controls.Add(btnSpeichern);
                schalter.Controls.Add(btnAktivieren);

                this.AcceptButton = btnZustimmen;
                this.CancelButton = btnAblehnen;
                _lblQuelle.Text = "Bitte lesen Sie die Vereinbarung und bestätigen Sie sie, um das Programm zu nutzen.";
            }
            else
            {
                Button btnSchliessen = new Button
                {
                    Text = "Schließen",
                    Width = 110,
                    Height = 30,
                    Margin = new Padding(6, 0, 0, 0),
                    DialogResult = DialogResult.OK
                };
                btnSchliessen.Click += (s, e) => this.Close();

                schalter.Controls.Add(btnSchliessen);
                schalter.Controls.Add(btnDrucken);
                schalter.Controls.Add(btnSpeichern);
                schalter.Controls.Add(btnAktivieren);

                this.AcceptButton = btnSchliessen;
                this.CancelButton = btnSchliessen;
            }

            unten.Controls.Add(_lblQuelle);   // Fill zuerst
            unten.Controls.Add(schalter);

            // Reihenfolge beachten: Fill zuerst, dann Top/Bottom
            this.Controls.Add(_register);
            this.Controls.Add(kopf);
            this.Controls.Add(unten);
        }

        /// <summary>Einheitlich formatierte Textanzeige für alle Registerkarten.</summary>
        private RichTextBox NeueAnzeige()
        {
            return new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.White,
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", _schriftgroesse),
                DetectUrls = true,
                ScrollBars = RichTextBoxScrollBars.Vertical
            };
        }

        // ------------------------------------------------------------------
        // Inhalte
        // ------------------------------------------------------------------

        /// <summary>Sucht die Lizenzdatei und lädt sie in die Anzeige.</summary>
        private void LizenzLaden()
        {
            // Eine ausdrücklich gewählte Datei hat Vorrang, sonst gilt die
            // Fassung von epos-plan.de.
            string treffer = DateiSuchen(null);

            if (treffer == null)
            {
                string stand;
                string zwischenspeicher = ZwischenspeicherLesen(out stand);

                if (!string.IsNullOrEmpty(zwischenspeicher))
                {
                    _text.Text = zwischenspeicher;
                    QuelleSetzen(ONLINE_FASSUNG, stand);
                }
                else
                {
                    _text.Text =
                        "Die Lizenzvereinbarung wird von epos-plan.de geladen..." + Environment.NewLine + Environment.NewLine +
                        "Besteht keine Verbindung, finden Sie die verbindliche Fassung unter" + Environment.NewLine +
                        "  " + ONLINE_FASSUNG;
                    QuelleSetzen(ONLINE_FASSUNG, null);
                }

                OnlineFassungHolen();
                return;
            }

            _gefundeneDatei = treffer;
            QuelleSetzen(treffer, null);

            try
            {
                if (treffer.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase))
                {
                    _text.LoadFile(treffer, RichTextBoxStreamType.RichText);
                }
                else
                {
                    _text.Text =
                        "Die Lizenzvereinbarung liegt als Word-Dokument vor:" + Environment.NewLine +
                        treffer + Environment.NewLine + Environment.NewLine +
                        "Über 'Speichern unter...' können Sie das Dokument ablegen und mit Word öffnen.";
                }
            }
            catch (Exception ex)
            {
                _text.Text = "Die Lizenzdatei konnte nicht gelesen werden:" + Environment.NewLine +
                             treffer + Environment.NewLine + Environment.NewLine + ex.Message;
            }
        }

        /// <summary>Füllt die Registerkarte "Rechtliche Hinweise".</summary>
        private void RechtlicheHinweiseFuellen()
        {
            SchreibeUeberschrift(_hinweise, "Anbieter");
            SchreibeAbsatz(_hinweise,
                "INEKON - Intelligente Energiekonzepte, Dr. Dirk Engelmann, Breitwiesenstraße 13, " +
                "70565 Stuttgart. Kontakt und vollständige Anbieterkennzeichnung: https://epos-plan.de/impressum/");

            SchreibeUeberschrift(_hinweise, "Verbindliche Grundlage");
            SchreibeAbsatz(_hinweise,
                "Für die Nutzung von EPOS-Plan gilt ausschließlich die Lizenzvereinbarung einschließlich der " +
                "Allgemeinen Geschäftsbedingungen auf der ersten Registerkarte dieses Fensters. Die folgenden " +
                "Abschnitte fassen wesentliche Punkte zusammen; im Zweifel gilt der Wortlaut der Vereinbarung.");

            SchreibeUeberschrift(_hinweise, "Nutzungsrecht");
            SchreibeAbsatz(_hinweise,
                "Der Anwender erhält ein nicht ausschließliches Recht zur Nutzung der Software im Umfang des " +
                "geschlossenen Lizenz- beziehungsweise Wartungsvertrags. Die Software ist urheberrechtlich " +
                "geschützt. Weitergabe, Vermietung, Dekompilierung und Veränderung sind nur in den gesetzlich " +
                "zwingend erlaubten Grenzen zulässig. Mitgelieferte Stammdaten, Kennfelder und Klimadatensätze " +
                "dürfen ausschließlich innerhalb der Software genutzt werden.");

            SchreibeUeberschrift(_hinweise, "Ergebnisse und Verantwortung des Anwenders");
            SchreibeAbsatz(_hinweise,
                "EPOS-Plan ist ein Planungswerkzeug. Die Berechnungen beruhen auf den eingegebenen Daten, auf " +
                "Herstellerangaben und auf modellhaften Annahmen; sie bilden das reale Anlagenverhalten " +
                "näherungsweise ab. Ergebnisse ersetzen weder die fachliche Prüfung durch eine qualifizierte " +
                "Planerin oder einen qualifizierten Planer noch eine Ausführungsplanung, eine Heizlastberechnung " +
                "nach den einschlägigen Normen oder behördlich geforderte Nachweise. Für die Plausibilität der " +
                "Eingangsdaten und für die Verwendung der Ergebnisse ist der Anwender verantwortlich.");

            SchreibeUeberschrift(_hinweise, "Gewährleistung und Haftung");
            SchreibeAbsatz(_hinweise,
                "Es gelten die Regelungen der Lizenzvereinbarung. Eine Haftung für Schäden, die auf fehlerhaften " +
                "Eingabedaten, unsachgemäßer Anwendung oder auf der Verwendung der Ergebnisse ohne fachliche " +
                "Prüfung beruhen, ist ausgeschlossen, soweit dies gesetzlich zulässig ist. Unberührt bleibt die " +
                "Haftung bei Vorsatz und grober Fahrlässigkeit, bei der Verletzung von Leben, Körper und " +
                "Gesundheit sowie nach dem Produkthaftungsgesetz.");

            SchreibeUeberschrift(_hinweise, "Datenverarbeitung");
            SchreibeAbsatz(_hinweise,
                "Projekt-, Kunden- und Simulationsdaten werden ausschließlich lokal auf diesem Rechner gespeichert " +
                "und nicht an INEKON übertragen. Eine Internetverbindung nutzt das Programm für den Bezug von " +
                "Klimadaten, für die Ortssuche und für den Aufruf der Online-Dokumentation. Wird der optionale " +
                "Hilfe-Assistent mit eigenem Zugangsschlüssel verwendet, werden ausschließlich die gestellte " +
                "Frage, der Name des Programmbereichs und die integrierten Hilfetexte an den Dienst des " +
                "jeweiligen Anbieters übertragen - keine Projekt- oder Kundendaten. Ohne hinterlegten Schlüssel " +
                "arbeitet die Hilfe rein lokal. Einzelheiten: https://epos-plan.de/datenschutz/");

            SchreibeUeberschrift(_hinweise, "Marken und Urheberrecht");
            SchreibeAbsatz(_hinweise,
                "EPOS-Plan sowie Programmoberfläche, Dokumentation und Datenbestände sind urheberrechtlich " +
                "geschützt. Genannte Produkt- und Firmennamen Dritter sind Marken ihrer jeweiligen Inhaber und " +
                "werden ausschließlich zur Bezeichnung der betreffenden Produkte verwendet.");

            SchreibeAbsatz(_hinweise,
                Environment.NewLine + "Stand: " + DateTime.Now.ToString("MMMM yyyy") +
                " - Programmversion " + VersionText() + ".");

            _hinweise.SelectionStart = 0;
            _hinweise.ScrollToCaret();
        }

        /// <summary>Füllt die Registerkarte "Komponenten" (Fremdkomponenten und Datenquellen).</summary>
        private void KomponentenFuellen()
        {
            SchreibeUeberschrift(_komponenten, "Verwendete Komponenten und Datenquellen");
            SchreibeAbsatz(_komponenten,
                "EPOS-Plan verwendet die nachfolgend genannten Komponenten und Daten Dritter. Deren jeweilige " +
                "Lizenz- und Nutzungsbedingungen gelten fort und werden durch die Lizenzvereinbarung zu " +
                "EPOS-Plan nicht berührt.");

            SchreibeUeberschrift(_komponenten, "Laufzeit und Bibliotheken");
            SchreibeAbsatz(_komponenten,
                "Microsoft .NET 8 mit Windows Forms (MIT-Lizenz, Microsoft Corporation) - Laufzeitumgebung und " +
                "Bedienoberfläche." + Environment.NewLine +
                "Microsoft Access Database Engine sowie OLE-DB- und ODBC-Treiber (Microsoft Corporation) - " +
                "Zugriff auf die Kenndaten-Datenbank; Installation und Nutzung nach den Bedingungen von Microsoft.");

            SchreibeUeberschrift(_komponenten, "Klima- und Geodaten");
            SchreibeAbsatz(_komponenten,
                "PVGIS - Photovoltaic Geographical Information System der Europäischen Kommission, Joint " +
                "Research Centre: Herkunft der typischen meteorologischen Jahresdatensätze für Temperatur und " +
                "Strahlung. Nutzung nach den Bedingungen der Europäischen Kommission, https://re.jrc.ec.europa.eu/" +
                Environment.NewLine +
                "OpenStreetMap / Nominatim: Ermittlung von Geokoordinaten zu Ortsnamen. Daten der " +
                "OpenStreetMap-Mitwirkenden, verfügbar unter der Open Database License (ODbL), " +
                "https://www.openstreetmap.org/copyright");

            SchreibeUeberschrift(_komponenten, "Produkt- und Herstellerdaten");
            SchreibeAbsatz(_komponenten,
                "Datensätze zu Wärmepumpen, Heizkesseln, Pufferspeichern und Solarkollektoren können nach " +
                "VDI 3805 aus Herstellerdatenbeständen eingelesen werden. Die Rechte an diesen Daten liegen bei " +
                "den jeweiligen Herstellern; für Richtigkeit und Aktualität der Herstellerangaben wird keine " +
                "Gewähr übernommen. Kennfelder und Kennwerte werden unverändert für die Berechnung verwendet.");

            SchreibeUeberschrift(_komponenten, "Optionaler Hilfe-Assistent");
            SchreibeAbsatz(_komponenten,
                "Der Hilfe-Assistent kann auf Wunsch einen externen Sprachmodell-Dienst nutzen. Dafür ist ein " +
                "eigener Zugangsschlüssel erforderlich, den der Anwender selbst hinterlegt; es gelten die " +
                "Nutzungsbedingungen des jeweiligen Anbieters. Ohne Schlüssel arbeitet die Hilfe ausschließlich " +
                "lokal und ohne Datenübertragung.");

            _komponenten.SelectionStart = 0;
            _komponenten.ScrollToCaret();
        }

        // ------------------------------------------------------------------
        // Hilfsfunktionen für die Textausgabe
        // ------------------------------------------------------------------

        private void SchreibeUeberschrift(RichTextBox ziel, string text)
        {
            ziel.SelectionStart = ziel.TextLength;
            ziel.SelectionLength = 0;
            ziel.SelectionFont = new Font("Segoe UI Semibold", _schriftgroesse + 1.5f, FontStyle.Bold);
            ziel.SelectionColor = Color.FromArgb(0, 90, 160);
            ziel.AppendText((ziel.TextLength > 0 ? Environment.NewLine : "") + text + Environment.NewLine);
        }

        private void SchreibeAbsatz(RichTextBox ziel, string text)
        {
            ziel.SelectionStart = ziel.TextLength;
            ziel.SelectionLength = 0;
            ziel.SelectionFont = new Font("Segoe UI", _schriftgroesse, FontStyle.Regular);
            ziel.SelectionColor = Color.FromArgb(40, 44, 48);
            ziel.AppendText(text + Environment.NewLine);
        }

        /// <summary>Sucht den eingegebenen Begriff im Vertragstext und markiert ihn.</summary>
        private void Suchen()
        {
            string begriff = (_suche.Text ?? "").Trim();
            if (begriff.Length == 0) return;

            int start = _text.SelectionStart + _text.SelectionLength;
            int treffer = _text.Find(begriff, start, RichTextBoxFinds.None);
            if (treffer < 0) treffer = _text.Find(begriff, 0, RichTextBoxFinds.None);

            if (treffer < 0)
            {
                MessageBox.Show("Der Begriff wurde nicht gefunden.", "Suchen",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _text.Select(treffer, begriff.Length);
            _text.ScrollToCaret();
            _text.Focus();
        }

        /// <summary>Vergrößert oder verkleinert die Schrift in allen Anzeigen.</summary>
        private void SchriftAendern(float schritt)
        {
            _schriftgroesse = Math.Max(7.5f, Math.Min(18f, _schriftgroesse + schritt));
            try
            {
                int start = _text.SelectionStart, laenge = _text.SelectionLength;
                _text.SelectAll();
                _text.SelectionFont = new Font(_text.Font.FontFamily, _schriftgroesse);
                _text.Select(start, laenge);

                _hinweise.Font = new Font("Segoe UI", _schriftgroesse);
                _komponenten.Font = new Font("Segoe UI", _schriftgroesse);
            }
            catch { }
        }

        // ------------------------------------------------------------------
        // Dateisuche, Speichern, Drucken
        // ------------------------------------------------------------------

        /// <summary>
        /// Durchsucht die üblichen Ablageorte nach der Lizenzdatei und
        /// protokolliert dabei die geprüften Verzeichnisse.
        /// </summary>
        /// <summary>
        /// Holt die geltende Fassung von epos-plan.de und legt sie örtlich ab.
        /// Läuft im Hintergrund; scheitert der Abruf, bleibt der zuletzt
        /// geholte Stand stehen - der Dialog soll auch ohne Netz etwas zeigen.
        /// Übertragen werden keine Projekt- oder Kundendaten, nur ein Seitenabruf.
        /// </summary>
        private async void OnlineFassungHolen()
        {
            try
            {
                string json;
                using (HttpClient http = new HttpClient())
                {
                    http.Timeout = TimeSpan.FromSeconds(20);
                    http.DefaultRequestHeaders.Add("User-Agent", "EPOS-Plan");
                    json = await http.GetStringAsync(ONLINE_QUELLE);
                }

                string text = "";
                string stand = null;
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    JsonElement wurzel = doc.RootElement;
                    if (wurzel.ValueKind != JsonValueKind.Array || wurzel.GetArrayLength() == 0) return;

                    JsonElement seite = wurzel[0];
                    if (seite.TryGetProperty("content", out JsonElement inhalt) &&
                        inhalt.TryGetProperty("rendered", out JsonElement gerendert))
                    {
                        text = HtmlZuText(gerendert.GetString());
                    }
                    if (seite.TryGetProperty("modified", out JsonElement geaendert))
                    {
                        stand = StandFormatieren(geaendert.GetString());
                    }
                }

                // Ein paar Zeilen wären kein Vertragstext - dann lieber den
                // vorhandenen Stand behalten als ihn durch Bruchstücke ersetzen.
                if (text.Length < 2000) return;

                ZwischenspeicherSchreiben(text, stand);

                if (!IsDisposed && string.IsNullOrEmpty(_gefundeneDatei))
                {
                    _text.Text = text;
                    QuelleSetzen(ONLINE_FASSUNG, stand);
                }
            }
            catch
            {
                // ohne Netz bleibt der Zwischenspeicher stehen
            }
        }

        /// <summary>HTML der Vertragsseite in lesbaren Fließtext umsetzen.</summary>
        private static string HtmlZuText(string html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            string s = html;
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?is)<(script|style)[^>]*>.*?</\1>", "");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?i)<br\s*/?>", "\n");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?i)<li[^>]*>", "  - ");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?i)<h[1-6][^>]*>", "\n");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"(?i)</(p|div|li|tr|h[1-6])>", "\n");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"<[^>]+>", "");
            s = System.Net.WebUtility.HtmlDecode(s);

            s = s.Replace("\r\n", "\n").Replace('\r', '\n');
            s = System.Text.RegularExpressions.Regex.Replace(s, @"[ \t]+\n", "\n");
            s = System.Text.RegularExpressions.Regex.Replace(s, @"\n{3,}", "\n\n");
            return s.Trim().Replace("\n", Environment.NewLine);
        }

        /// <summary>"2026-08-13T22:08:02" wird zu "13.08.2026".</summary>
        private static string StandFormatieren(string roh)
        {
            if (string.IsNullOrEmpty(roh)) return null;
            DateTime wert;
            return DateTime.TryParse(roh, out wert) ? wert.ToString("dd.MM.yyyy") : roh;
        }

        /// <summary>Ablage neben den übrigen Lizenzdaten in %AppData%\wp-plan.</summary>
        private static string ZwischenspeicherDatei(string name)
        {
            string pfad = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wp-plan");
            Directory.CreateDirectory(pfad);
            return Path.Combine(pfad, name);
        }

        private static string ZwischenspeicherLesen(out string stand)
        {
            stand = null;
            try
            {
                string textdatei = ZwischenspeicherDatei("lizenztext.txt");
                if (!File.Exists(textdatei)) return "";

                string standdatei = ZwischenspeicherDatei("lizenztext-stand.txt");
                if (File.Exists(standdatei)) stand = File.ReadAllText(standdatei).Trim();

                return File.ReadAllText(textdatei);
            }
            catch { return ""; }
        }

        private static void ZwischenspeicherSchreiben(string text, string stand)
        {
            try
            {
                File.WriteAllText(ZwischenspeicherDatei("lizenztext.txt"), text);
                File.WriteAllText(ZwischenspeicherDatei("lizenztext-stand.txt"), stand ?? "");
            }
            catch { }
        }

        /// <summary>Fußzeile: Lizenzstand dieses Arbeitsplatzes und Herkunft des Textes.</summary>
        private void QuelleSetzen(string quelle, string stand)
        {
            string zweite = "Quelle: " + quelle;
            if (!string.IsNullOrEmpty(stand)) zweite += "   ·   Stand " + stand;

            string status;
            try { status = LizenzManager.StatusText(); }
            catch { status = "Status nicht ermittelbar"; }

            _lblQuelle.Text = "Lizenz: " + status + Environment.NewLine + zweite;
        }

        /// <summary>
        /// Öffnet die Lizenzverwaltung (Schlüssel, .lic-Datei, Testversion) und
        /// zieht danach den Status nach. Die Aktivierung selbst liegt bewusst
        /// nur dort - zwei Eingabewege für denselben Vorgang wären eine Falle.
        /// </summary>
        private void LizenzVerwaltungOeffnen()
        {
            try
            {
                using (Form_LizenzVerwaltung frm = new Form_LizenzVerwaltung())
                    frm.ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Die Lizenzverwaltung konnte nicht geöffnet werden:" +
                    Environment.NewLine + ex.Message, this.Text,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            QuelleSetzen(string.IsNullOrEmpty(_gefundeneDatei) ? ONLINE_FASSUNG : _gefundeneDatei, null);
        }

        /// <summary>
        /// Lässt den Anwender die Lizenzdatei auswählen und merkt sich den Pfad.
        /// Nötig, weil die Datei nicht zwingend neben dem Programm liegt; ohne
        /// diese Auswahl bliebe die Registerkarte leer.
        /// </summary>
        private void LizenzDateiWaehlen()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Lizenzvereinbarung auswählen";
                dialog.Filter = "Lizenzvereinbarung (*.rtf;*.docx;*.pdf)|*.rtf;*.docx;*.pdf|Rich Text (*.rtf)|*.rtf|Alle Dateien (*.*)|*.*";
                dialog.CheckFileExists = true;

                try
                {
                    string bisher = GewaehltenPfadLesen();
                    if (!string.IsNullOrEmpty(bisher) && File.Exists(bisher))
                        dialog.InitialDirectory = Path.GetDirectoryName(bisher);
                }
                catch { }

                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                GewaehltenPfadSpeichern(dialog.FileName);
            }

            LizenzLaden();
        }

        /// <summary>Zuletzt gewählter Pfad der Lizenzdatei; leer, wenn keiner gemerkt ist.</summary>
        private static string GewaehltenPfadLesen()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                       Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REG_SCHLUESSEL))
                {
                    if (key == null) return "";
                    return key.GetValue(REG_LIZENZDATEI) as string ?? "";
                }
            }
            catch { return ""; }
        }

        /// <summary>Merkt den gewählten Pfad, damit die Datei beim nächsten Öffnen sofort da ist.</summary>
        private static void GewaehltenPfadSpeichern(string pfad)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                       Microsoft.Win32.Registry.CurrentUser.CreateSubKey(REG_SCHLUESSEL))
                {
                    if (key != null) key.SetValue(REG_LIZENZDATEI, pfad ?? "");
                }
            }
            catch { }
        }

        private string DateiSuchen(System.Collections.Generic.List<string> protokoll)
        {
            // Vorrang hat die vom Anwender ausgewaehlte Datei - sie kann irgendwo
            // liegen und wird sonst von keiner der Suchebenen unten gefunden.
            string gewaehlt = GewaehltenPfadLesen();
            if (!string.IsNullOrEmpty(gewaehlt))
            {
                try
                {
                    if (File.Exists(gewaehlt)) return gewaehlt;
                }
                catch { }

                if (protokoll != null)
                    protokoll.Add(gewaehlt + "   (ausgewählt, aber nicht mehr vorhanden)");
            }

            System.Collections.Generic.List<string> ordner = new System.Collections.Generic.List<string>();

            try
            {
                string basis = AppDomain.CurrentDomain.BaseDirectory;
                ordner.Add(basis);

                // Übergeordnete Ebenen mitnehmen: bin\x86\Debug\net8.0-windows -> Projektstamm
                DirectoryInfo di = new DirectoryInfo(basis);
                for (int i = 0; i < 6 && di.Parent != null; i++)
                {
                    di = di.Parent;
                    ordner.Add(di.FullName);
                }
            }
            catch { }

            try
            {
                if (!string.IsNullOrEmpty(Program.ApplicationPath_Common)) ordner.Add(Program.ApplicationPath_Common);
                if (!string.IsNullOrEmpty(Program.ApplicationPath_User)) ordner.Add(Program.ApplicationPath_User);
            }
            catch { }

            foreach (string o in ordner)
            {
                if (string.IsNullOrEmpty(o)) continue;
                if (protokoll != null && !protokoll.Contains(o)) protokoll.Add(o);

                foreach (string name in DATEINAMEN)
                {
                    try
                    {
                        string pfad = Path.Combine(o, name);
                        if (File.Exists(pfad)) return pfad;
                    }
                    catch { }
                }
            }

            return null;
        }

        /// <summary>Speichert den Inhalt der aktiven Registerkarte.</summary>
        private void SpeichernUnter()
        {
            bool istVertrag = _register.SelectedIndex == 0;

            if (istVertrag && !string.IsNullOrEmpty(_gefundeneDatei) && File.Exists(_gefundeneDatei))
            {
                SaveFileDialog dlgDatei = new SaveFileDialog();
                dlgDatei.Title = "Lizenzvereinbarung speichern";
                dlgDatei.FileName = Path.GetFileName(_gefundeneDatei);
                dlgDatei.Filter = "Alle Dateien (*.*)|*.*";
                dlgDatei.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                if (dlgDatei.ShowDialog(this) != DialogResult.OK) return;

                try
                {
                    File.Copy(_gefundeneDatei, dlgDatei.FileName, true);
                    MessageBox.Show("Die Lizenzvereinbarung wurde gespeichert:\n" + dlgDatei.FileName,
                        "Lizenz", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Die Datei konnte nicht gespeichert werden:\n" + ex.Message,
                        "Lizenz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            RichTextBox quelle = AktiveAnzeige();
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Title = "Text speichern";
            dlg.FileName = (istVertrag ? "EPOS-Plan_Lizenz" :
                            _register.SelectedIndex == 1 ? "EPOS-Plan_Rechtliche_Hinweise" : "EPOS-Plan_Komponenten") + ".rtf";
            dlg.Filter = "Rich Text (*.rtf)|*.rtf|Textdatei (*.txt)|*.txt";
            dlg.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                if (dlg.FileName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                    File.WriteAllText(dlg.FileName, quelle.Text);
                else
                    quelle.SaveFile(dlg.FileName, RichTextBoxStreamType.RichText);

                MessageBox.Show("Gespeichert:\n" + dlg.FileName, "Lizenz",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Die Datei konnte nicht gespeichert werden:\n" + ex.Message,
                    "Lizenz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private RichTextBox AktiveAnzeige()
        {
            if (_register.SelectedIndex == 1) return _hinweise;
            if (_register.SelectedIndex == 2) return _komponenten;
            return _text;
        }

        /// <summary>Druckt den Text der aktiven Registerkarte mit Kopf- und Fußzeile.</summary>
        private void Drucken()
        {
            try
            {
                _druckText = AktiveAnzeige().Text;
                _druckPosition = 0;
                _druckSeite = 0;

                _druck = new PrintDocument();
                _druck.DocumentName = "EPOS-Plan - Lizenz";
                _druck.PrintPage += Druck_PrintPage;

                PrintDialog dlg = new PrintDialog();
                dlg.Document = _druck;
                if (dlg.ShowDialog(this) == DialogResult.OK) _druck.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Der Druck konnte nicht gestartet werden:\n" + ex.Message,
                    "Lizenz", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void Druck_PrintPage(object sender, PrintPageEventArgs e)
        {
            _druckSeite++;

            using (Font f = new Font("Segoe UI", 9f))
            using (Font klein = new Font("Segoe UI", 7.5f))
            {
                RectangleF bereich = new RectangleF(
                    e.MarginBounds.Left, e.MarginBounds.Top + 22,
                    e.MarginBounds.Width, e.MarginBounds.Height - 44);

                // Kopfzeile
                e.Graphics.DrawString("EPOS-Plan - Lizenz und rechtliche Hinweise", klein, Brushes.Gray,
                    e.MarginBounds.Left, e.MarginBounds.Top);
                // Fußzeile mit Seitenzahl
                e.Graphics.DrawString("Seite " + _druckSeite + "   -   Stand " + DateTime.Now.ToString("dd.MM.yyyy"),
                    klein, Brushes.Gray, e.MarginBounds.Left, e.MarginBounds.Bottom - 12);

                string rest = _druckText.Substring(_druckPosition);

                int zeichen, zeilen;
                e.Graphics.MeasureString(rest, f, bereich.Size, StringFormat.GenericTypographic,
                    out zeichen, out zeilen);

                e.Graphics.DrawString(rest.Substring(0, zeichen), f, Brushes.Black, bereich,
                    StringFormat.GenericTypographic);

                _druckPosition += zeichen;
                e.HasMorePages = _druckPosition < _druckText.Length;
                if (!e.HasMorePages) { _druckPosition = 0; _druckSeite = 0; }
            }
        }

        private void LinkOeffnen(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url)
                {
                    UseShellExecute = true
                });
            }
            catch { }
        }

        private static string VersionText()
        {
            try
            {
                Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                return v == null ? "" : v.ToString();
            }
            catch { return ""; }
        }

        // ------------------------------------------------------------------
        // Zustimmung beim ersten Start (optional)
        // ------------------------------------------------------------------

        /// <summary>Merkt die erteilte Zustimmung samt Datum und Programmversion.</summary>
        private void ZustimmungMerken()
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                       Microsoft.Win32.Registry.CurrentUser.CreateSubKey(REG_SCHLUESSEL))
                {
                    if (key != null)
                        key.SetValue(REG_ZUSTIMMUNG,
                            VersionText() + " | " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                }
            }
            catch { }
        }

        /// <summary>
        /// Prüft, ob der Lizenzvereinbarung bereits zugestimmt wurde, und holt die
        /// Zustimmung andernfalls nach. Rückgabe false bedeutet: abgelehnt - das
        /// Programm sollte dann beendet werden.
        ///
        /// Aufruf beim Programmstart, zum Beispiel in Program.Main vor dem Öffnen
        /// des Hauptfensters:  if (!Form_Lizenz.ZustimmungSicherstellen()) return;
        /// </summary>
        public static bool ZustimmungSicherstellen(IWin32Window besitzer = null)
        {
            try
            {
                using (Microsoft.Win32.RegistryKey key =
                       Microsoft.Win32.Registry.CurrentUser.OpenSubKey(REG_SCHLUESSEL))
                {
                    object wert = key == null ? null : key.GetValue(REG_ZUSTIMMUNG);
                    if (wert != null && wert.ToString().Length > 0) return true;
                }
            }
            catch { return true; }   // im Zweifel den Start nicht blockieren

            Form_Lizenz frm = new Form_Lizenz(true);
            DialogResult ergebnis = besitzer != null ? frm.ShowDialog(besitzer) : frm.ShowDialog();
            return ergebnis == DialogResult.OK;
        }

        /// <summary>Bequemer Einstiegspunkt für den Menüaufruf.</summary>
        public static void Anzeigen(IWin32Window besitzer = null)
        {
            Form_Lizenz frm = new Form_Lizenz();
            if (besitzer != null) frm.ShowDialog(besitzer); else frm.ShowDialog();
        }
    }
}
