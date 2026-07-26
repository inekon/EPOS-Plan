using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

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
        private Label _lblStatus;

        private readonly List<string> _verlauf = new List<string>();
        private string _kontext = "";

        public Form_KiChat()
        {
            BaueOberflaeche();
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

            // --- Kopfzeile: Kontext links, Dokumentationslink rechts ---
            Panel kopf = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(10, 6, 10, 0) };

            _lblKontext = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.FromArgb(0, 90, 160),
                TextAlign = ContentAlignment.MiddleLeft,
                AutoEllipsis = true,
                Text = "Kontext: (nicht erkannt)"
            };

            LinkLabel linkDoku = new LinkLabel
            {
                Text = "Dokumentation",
                Dock = DockStyle.Right,
                AutoSize = true,
                Padding = new Padding(12, 2, 0, 0)
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

            kopf.Controls.Add(_lblKontext);   // Fill zuerst
            kopf.Controls.Add(linkDoku);

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
            Panel unten = new Panel { Dock = DockStyle.Bottom, Height = 138 };

            // --- Eine einzige Schaltflächenzeile, rechtsbündig und gleich groß ---
            Panel leiste = new Panel { Dock = DockStyle.Bottom, Height = 48, Padding = new Padding(8, 6, 8, 8) };

            _btnSenden = new Button { Text = "Fragen", Width = 110, Height = 30, Margin = new Padding(6, 0, 0, 0) };
            _btnSenden.Click += async (s, e) => await FrageStellen(true);

            _btnSuchen = new Button { Text = "Nur suchen", Width = 110, Height = 30, Margin = new Padding(6, 0, 0, 0) };
            _btnSuchen.Click += async (s, e) => await FrageStellen(false);

            _btnEinstellungen = new Button { Text = "Einstellungen...", Width = 110, Height = 30, Margin = new Padding(6, 0, 0, 0) };
            _btnEinstellungen.Click += (s, e) => EinstellungenOeffnen();

            Button btnSchliessen = new Button
            {
                Text = "Schließen",
                Width = 110,
                Height = 30,
                Margin = new Padding(6, 0, 0, 0)
            };
            // Das Fenster wird nicht-modal geöffnet - DialogResult schließt es dann
            // nicht. Deshalb hier ausdrücklich Close() aufrufen.
            btnSchliessen.Click += (s, e) => this.Close();

            FlowLayoutPanel leisteRechts = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false
            };
            // RightToLeft: zuerst hinzugefügt = ganz rechts
            leisteRechts.Controls.Add(btnSchliessen);
            leisteRechts.Controls.Add(_btnEinstellungen);
            leisteRechts.Controls.Add(_btnSuchen);
            leisteRechts.Controls.Add(_btnSenden);

            _lblStatus = new Label
            {
                Dock = DockStyle.Fill,
                ForeColor = Color.DimGray,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(4, 0, 8, 0),
                AutoEllipsis = true,
                Text = ""
            };

            // Reihenfolge beachten: Fill zuerst, dann die andockenden Elemente
            leiste.Controls.Add(_lblStatus);
            leiste.Controls.Add(leisteRechts);

            // --- Eingabebereich (volle Breite, Schaltflächen liegen darunter) ---
            Panel eingabeBereich = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10, 8, 10, 0) };

            _eingabe = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                // Enter wird in Eingabe_KeyDown ausgewertet (Senden bzw. Shift+Enter
                // = neue Zeile). AcceptsReturn verhindert, dass eine Schaltfläche
                // die Eingabetaste vorher abfängt.
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Vertical,
                Font = new Font("Segoe UI", 9.5f)
            };
            _eingabe.KeyDown += Eingabe_KeyDown;

            eingabeBereich.Controls.Add(_eingabe);

            unten.Controls.Add(eingabeBereich);           // Fill zuerst
            unten.Controls.Add(leiste);

            // Reihenfolge beachten: Fill zuerst, dann Top/Bottom
            this.Controls.Add(_verlaufAnzeige);
            this.Controls.Add(kopf);
            this.Controls.Add(unten);

            // Kein AcceptButton setzen - sonst fängt er die Eingabetaste aus dem
            // Textfeld ab und die Frage würde doppelt gesendet.
            // Escape schließt das Fenster - CancelButton greift nur modal
            this.KeyPreview = true;
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape) { e.Handled = true; this.Close(); }
            };

            Begruessung();
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

            _btnSenden.Enabled = false;
            _btnSuchen.Enabled = false;
            _lblStatus.Text = "Der Assistent denkt nach...";
            Cursor.Current = Cursors.WaitCursor;

            try
            {
                KiAntwort antwort = await KiChatService.FrageAsync(frage, _kontext, _verlauf);

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
                _btnSenden.Enabled = true;
                _btnSuchen.Enabled = true;
                Cursor.Current = Cursors.Default;
                _eingabe.Focus();
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
            frm.ClientSize = new Size(500, 250);

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
                Text = "Tageslimit (Anfragen je Arbeitsplatz):",
                AutoSize = true,
                Location = new Point(14, 82)
            };
            NumericUpDown numLimit = new NumericUpDown
            {
                Location = new Point(300, 79),
                Width = 80,
                Minimum = 1,
                Maximum = 1000,
                Value = Math.Min(1000, Math.Max(1, KiChatService.Tageslimit))
            };

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

            Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(314, 212), Width = 80 };
            Button abbruch = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Location = new Point(400, 212), Width = 84 };

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
            frm.Controls.Add(l2); frm.Controls.Add(numLimit);
            frm.Controls.Add(btnModell);
            frm.Controls.Add(hinweis);
            frm.Controls.Add(ok); frm.Controls.Add(abbruch);
            frm.AcceptButton = ok;
            frm.CancelButton = abbruch;

            if (frm.ShowDialog(this) != DialogResult.OK) return;

            KiChatService.ApiKey = tbKey.Text.Trim();
            KiChatService.Tageslimit = (int)numLimit.Value;

            SchreibeZeile(KiChatService.IstEingerichtet
                ? "Einstellungen gespeichert - der Assistent ist einsatzbereit."
                : "Einstellungen gespeichert - ohne Schlüssel bleibt nur die lokale Suche aktiv.",
                Color.FromArgb(0, 120, 0), false);
            SchreibeZeile("", Color.Black, false);
        }

        // ------------------------------------------------------------------
        // Hilfsfunktionen
        // ------------------------------------------------------------------

        private void SchreibeZeile(string text, Color farbe, bool fett)
        {
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
        public static void Oeffnen(IWin32Window besitzer = null)
        {
            // Kontext ermitteln, SOLANGE das aufrufende Fenster noch aktiv ist
            string kontext = HilfeKontext.Beschreibung();

            // Bereits geöffnetes Fenster wiederverwenden, statt Fenster zu stapeln
            if (_offen != null && !_offen.IsDisposed)
            {
                _offen.SetzeKontext(kontext);
                if (_offen.WindowState == FormWindowState.Minimized)
                    _offen.WindowState = FormWindowState.Normal;
                _offen.BringToFront();
                _offen.Activate();
                return;
            }

            Form_KiChat frm = new Form_KiChat();
            frm.SetzeKontext(kontext);
            frm.FormClosed += (s, e) => { if (ReferenceEquals(_offen, frm)) _offen = null; };
            _offen = frm;

            if (besitzer != null) frm.Show(besitzer); else frm.Show();
        }

        /// <summary>Aktuell geöffnetes Chatfenster (nicht-modal, daher nur eines).</summary>
        private static Form_KiChat _offen;
    }
}
