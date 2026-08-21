using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Farben, Maße und Zeichenhilfen der Karten aus Etappe D2/D3
    /// (Konzept_KonfigUI_Hydraulik, Abschnitt 3 und 3a).
    ///
    /// Die Werte stammen 1:1 aus dem Mockup
    /// <c>Entwurf_Hydraulikuebersicht_Konfiguration.html</c> — dort stehen sie als
    /// Hexzahlen im Stilattribut. Sie liegen hier an EINER Stelle, damit
    /// <see cref="ErzeugerKarte"/> und <see cref="SpeicherKarte"/> nicht zwei
    /// auseinanderlaufende Farbtabellen führen.
    ///
    /// Bewusst keine <c>SystemColors</c>: Die Karten sind eine gezeichnete Fläche mit
    /// festem Farbklang (blau = Quelle, koralle = Senke/Speicher, amber = Warnung), und
    /// genau diese Zuordnung trägt die Aussage. Ein Systemthema würde sie einebnen.
    /// </summary>
    internal static class KartenStil
    {
        /// <summary>Rahmen einer Erzeugerkarte (#b4b2a9).</summary>
        public static readonly Color RAHMEN = Color.FromArgb(180, 178, 169);

        /// <summary>Rahmen einer Speicherkarte (#D85A30) — koralle wie im Schema.</summary>
        public static readonly Color RAHMEN_SPEICHER = Color.FromArgb(216, 90, 48);

        /// <summary>Rahmen einer Karte ohne Inhalt (gestrichelt, Platzhalterzeile).</summary>
        public static readonly Color RAHMEN_LEISE = Color.FromArgb(217, 215, 207);

        public static readonly Color TEXT = Color.FromArgb(44, 44, 42);          // #2c2c2a
        public static readonly Color TEXT_LEISE = Color.FromArgb(95, 94, 90);    // #5f5e5a
        public static readonly Color TEXT_SEHR_LEISE = Color.FromArgb(136, 135, 128); // #888780

        public static readonly Color CHIP_RAHMEN = Color.FromArgb(217, 215, 207);
        public static readonly Color FLAECHE = Color.FromArgb(245, 244, 239);    // #f5f4ef

        public static readonly Color QUELLE_RAHMEN = Color.FromArgb(55, 138, 221);   // #378ADD
        public static readonly Color QUELLE_TEXT = Color.FromArgb(24, 95, 165);      // #185FA5

        public static readonly Color SENKE_RAHMEN = Color.FromArgb(216, 90, 48);     // #D85A30
        public static readonly Color SENKE_TEXT = Color.FromArgb(153, 60, 29);       // #993C1D

        public static readonly Color BADGE_FLAECHE = Color.FromArgb(250, 236, 231);  // #FAECE7
        public static readonly Color BADGE_TEXT = Color.FromArgb(113, 43, 19);       // #712B13

        public static readonly Color WARN_RAHMEN = Color.FromArgb(200, 138, 0);
        public static readonly Color WARN_FLAECHE = Color.FromArgb(255, 246, 224);
        public static readonly Color WARN_TEXT = Color.FromArgb(138, 91, 0);

        /// <summary>Innenabstand einer Karte [px].</summary>
        public const int RAND = 10;

        /// <summary>Eckenradius der Karten und Chips [px].</summary>
        public const int ECKE = 6;

        /// <summary>Kreisziffern ①…⑨ für die wirksame Ladepriorität (Konzept 3, „①②").</summary>
        public static string Kreisziffer(int n)
        {
            if (n < 1) return "";
            if (n > 9) return "(" + n + ")";
            return ((char)('①' + (n - 1))).ToString();
        }

        /// <summary>Rechteck mit abgerundeten Ecken — für Kartenrahmen und Chips.</summary>
        public static GraphicsPath Rundeck(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            if (radius <= 0 || r.Width <= 2 * radius || r.Height <= 2 * radius)
            {
                p.AddRectangle(r);
                return p;
            }

            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        /// <summary>Ein Label, dessen Schriftschnitt geändert wird, ohne die Familie zu verlieren.</summary>
        public static void Schnitt(Control c, FontStyle stil)
        {
            c.Font = new Font(c.Font, stil);
        }
    }

    /// <summary>
    /// Ein Chip: kurzer Text in einer abgerundeten Umrandung (Mockup Abschnitt 4).
    ///
    /// Erbt von <see cref="Label"/> und nicht von <see cref="Control"/>, weil damit
    /// <c>AutoSize</c>, <c>Padding</c> und die Textausgabe geschenkt sind; gezeichnet
    /// wird nur der Rahmen. Ein eigenes <c>Control</c> müsste die Textmessung nachbauen —
    /// und die ist genau das, was bei Sprachumschaltung stimmen muss.
    /// </summary>
    internal sealed class KartenChip : Label
    {
        /// <summary>Farbe der Umrandung; <see cref="OhneRand"/> unterdrückt sie ganz.</summary>
        public Color RandFarbe = KartenStil.CHIP_RAHMEN;

        /// <summary>true = gestrichelte Umrandung (Kaskaden-Quelle, Konzept 3).</summary>
        public bool Gestrichelt;

        /// <summary>true = nur Füllfläche, kein Rahmen (Temperaturpaar).</summary>
        public bool OhneRand;

        public KartenChip()
        {
            AutoSize = true;
            Padding = new Padding(8, 3, 8, 3);
            Margin = new Padding(0, 0, 6, 4);
            BackColor = Color.Transparent;
            ForeColor = KartenStil.TEXT;
            TextAlign = ContentAlignment.MiddleLeft;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);

            if (BackColor != Color.Transparent)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (GraphicsPath p = KartenStil.Rundeck(r, KartenStil.ECKE))
                using (SolidBrush b = new SolidBrush(BackColor))
                    e.Graphics.FillPath(b, p);
            }

            // Text erst NACH der Fläche: Label.OnPaint füllt den Hintergrund selbst,
            // würde also die abgerundete Fläche wieder eckig übermalen.
            base.OnPaint(e);

            if (OhneRand) return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (GraphicsPath p = KartenStil.Rundeck(r, KartenStil.ECKE))
            using (Pen stift = new Pen(RandFarbe))
            {
                if (Gestrichelt) stift.DashStyle = DashStyle.Dash;
                e.Graphics.DrawPath(stift, p);
            }
        }
    }

    /// <summary>
    /// ETAPPE D2 (Konzept_KonfigUI_Hydraulik, Abschnitt 3 und 6) — eine Karte je
    /// Wärmeerzeuger-Anlage in der Erzeugerspalte der Simulationskonfiguration.
    ///
    /// <b>Was sie ersetzt.</b> Die neunspaltige <c>listView_Uebersicht</c> und die vier
    /// ComboBoxen der Rubrik „Erzeuger &amp;&amp; Speicher". Die Liste zeigte Quelle, Senke
    /// und Modus in Spalten, deren Breite an den Kopftexten hing; die Kaskadenreihenfolge
    /// war nur an der Reihenfolge der ComboBoxen ablesbar, also an einem zweiten,
    /// getrennten Bedienelement. Die Karte führt beides zusammen: Kaskadenrang und
    /// Anlagenname in der Kopfzeile, alles Übrige als Chips darunter.
    ///
    /// <b>Reine Lesefläche.</b> Sie hält keinen Zustand und schreibt nichts. ▲▼ und ✎
    /// melden nur; was daraufhin geschieht, entscheidet <c>Form_Simulation_Config</c>
    /// (Konzept 3: „Doppelklick/✎ öffnet überall die bestehenden Dialoge — die neue Seite
    /// ist Lesefläche, keine Parallel-Editierwelt").
    ///
    /// <b>Kein Designer.</b> Wie die übrige Fußzeile des Dialogs rein programmatisch
    /// aufgebaut; alle sichtbaren Texte kommen aus <c>MyResource</c>.
    /// </summary>
    internal sealed class ErzeugerKarte : UserControl
    {
        /// <summary>Farbklang eines Chips — die Zuordnung aus Konzept 3.</summary>
        public enum ChipStil
        {
            /// <summary>Grauer Rahmen (Zweitsenke, Betriebsmodus).</summary>
            Neutral,

            /// <summary>Blau (Wärmequelle).</summary>
            Quelle,

            /// <summary>Blau gestrichelt (Quelle ist ein Pufferspeicher = Kaskade).</summary>
            QuelleKaskade,

            /// <summary>Koralle (Haupt- und Zweitsenke auf einen Puffer).</summary>
            Senke,

            /// <summary>Nur Füllfläche, kein Rahmen (Temperaturpaar).</summary>
            Flaeche,

            /// <summary>Amber (Temperatur-Warnregel, Konzept Abschnitt 5).</summary>
            Warnung
        }

        /// <summary>
        /// Editor, den ein Doppelklick auf DIESEN Chip öffnet — der Ersatz für den
        /// Spalten-Dispatcher der alten Übersicht (<c>SPALTEN_MIT_DIALOG</c>).
        ///
        /// Der frühere Dispatcher entschied über den Spaltenindex; die Karte hat keine
        /// Spalten mehr, also trägt jeder Chip sein Ziel selbst. Was hier
        /// <see cref="Keines"/> ist, öffnet den Standard-Editor der Karte — genauso wie
        /// eine Spalte, die nicht in der Whitelist stand, früher nichts tat.
        /// </summary>
        public enum ChipZiel
        {
            Keines,
            Quelle,
            Senke,
            Zweitsenke,
            Modus,
            Prioritaet
        }

        /// <summary>Ein Chip, so wie ihn die Konfigurationsseite beschreibt.</summary>
        public sealed class ChipDaten
        {
            public string Text = "";
            public ChipStil Stil = ChipStil.Neutral;

            /// <summary>Mouseover-Hinweis; null = kein Hinweis.</summary>
            public string Hinweis;

            /// <summary>Dialog, den ein Doppelklick auf diesen Chip öffnet.</summary>
            public ChipZiel Ziel = ChipZiel.Keines;
        }

        /// <summary>
        /// Nimmt die Komponente an der Simulation teil?
        ///
        /// Das ist die Auswahl, die bis D1 die vier Wärmeerzeuger-ComboBoxen samt ihren
        /// Checkboxen und die beiden Strom-Auswahlfelder trafen: Sie entschieden, WELCHE
        /// im Projekt vorhandene Technologie in <c>Tab_Einstellungen.Tool_1..6</c> landet
        /// und damit gerechnet wird. Die Karten bilden genau das ab — sie sind an dieser
        /// Stelle NICHT nur Anzeige.
        /// </summary>
        public enum Kartenzustand
        {
            /// <summary>In der Simulation (steht in Tool_1..6).</summary>
            Aufgenommen,

            /// <summary>Im Katalog wählbar, aber nicht aufgenommen — leerer Auswahlplatz.</summary>
            Verfuegbar
        }

        /// <summary>Alles, was eine Karte für ihren Aufbau braucht.</summary>
        public sealed class Aufbau
        {
            /// <summary>Kaskadenrang; leer = kein Rang (Strom- und Speicherseite).</summary>
            public string Rang = "";

            public string Titel = "";
            public List<ChipDaten> Chips = new List<ChipDaten>();

            public Kartenzustand Zustand = Kartenzustand.Aufgenommen;

            /// <summary>▲▼ anbieten (nur bei aufgenommenen Wärmeerzeugern).</summary>
            public bool Reihenfolge;
            public bool AufMoeglich;
            public bool AbMoeglich;

            /// <summary>+ bzw. × anbieten (Auswahl-Mechanik).</summary>
            public bool Umschaltbar;

            /// <summary>✎ anbieten (öffnet den Senkendialog).</summary>
            public bool Editierbar;

            /// <summary>
            /// Chips des AUFKLAPPBAREN Detailbereichs (Abnahmebefund 3).
            ///
            /// Leer = die Karte hat keinen Detailbereich und sieht aus wie bisher; sonst
            /// erscheint links in der Kopfzeile das Dreieck ▸/▾, und diese Chips stehen
            /// unter den <see cref="Chips"/> — nur solange die Karte aufgeklappt ist.
            /// </summary>
            public List<ChipDaten> Detailchips = new List<ChipDaten>();

            /// <summary>Zustand des Detailbereichs beim Aufbau (die Seite merkt ihn sich).</summary>
            public bool Aufgeklappt;
        }

        private readonly Label _lblPfeil = new Label();
        private readonly Label _lblRang = new Label();
        private readonly Label _lblTitel = new Label();
        private readonly Label _lnkAuf = new Label();
        private readonly Label _lnkAb = new Label();
        private readonly Label _lnkEdit = new Label();
        private readonly Label _lnkAufnehmen = new Label();
        private readonly Label _lnkEntfernen = new Label();
        private readonly FlowLayoutPanel _chips = new FlowLayoutPanel();
        private readonly FlowLayoutPanel _detail = new FlowLayoutPanel();
        private readonly ToolTip _tip = new ToolTip();

        private bool _aufbau;
        private Kartenzustand _zustand = Kartenzustand.Aufgenommen;
        private bool _hervorgehoben;
        private bool _aufgeklappt;

        /// <summary>
        /// ABNAHMEBEFUND 3 — Detailbereich sichtbar?
        ///
        /// Die Karte kennt ihre Nachbarn nicht; dass höchstens eine Karte je Gruppe offen
        /// ist, regelt <c>Form_Simulation_Config</c> — dieselbe Arbeitsteilung wie bei
        /// <see cref="SpeicherKarte"/>.
        /// </summary>
        public bool Aufgeklappt
        {
            get { return _aufgeklappt; }
            set
            {
                if (_aufgeklappt == value) return;
                _aufgeklappt = value;
                DetailZustandAnwenden();
            }
        }

        /// <summary>
        /// ETAPPE D4 — die Karte ist das im Schema markierte Element (oder umgekehrt).
        ///
        /// Reine Hervorhebung: kräftigerer Rahmen in der Quellfarbe. Sie ersetzt KEINE
        /// Auswahlmechanik der Karte (das „+/×" bleibt die Teilnahme an der Simulation);
        /// sie sagt nur „von diesem Element ist gerade die Rede" — das Gegenstück zur
        /// Auswahl in <c>SchemaAnsicht</c> (Konzept 3a: „die Auswahl ist mit der
        /// Schema-Ansicht synchronisiert").
        /// </summary>
        public bool Hervorgehoben
        {
            get { return _hervorgehoben; }
            set
            {
                if (_hervorgehoben == value) return;
                _hervorgehoben = value;
                Invalidate();
            }
        }

        /// <summary>Klick auf ▲ — die Anlage soll in der Kaskade nach vorn.</summary>
        public event EventHandler NachOben;

        /// <summary>Klick auf ▼ — die Anlage soll in der Kaskade nach hinten.</summary>
        public event EventHandler NachUnten;

        /// <summary>Klick auf ✎ oder Doppelklick auf eine Stelle ohne eigenen Editor.</summary>
        public event EventHandler Bearbeiten;

        /// <summary>Klick auf „+ aufnehmen" — die Komponente soll mitgerechnet werden.</summary>
        public event EventHandler Aufnehmen;

        /// <summary>Klick auf „×" — die Komponente soll nicht mehr mitgerechnet werden.</summary>
        public event EventHandler Entfernen;

        /// <summary>Doppelklick auf einen Chip mit eigenem Editor (<see cref="ChipZiel"/>).</summary>
        public event Action<ChipDaten> ChipBearbeiten;

        /// <summary>
        /// ABNAHMEBEFUND 3 — Klick auf ▸/▾: Der Detailbereich soll auf- bzw. zuklappen.
        /// Gemeldet wird nur; umgeschaltet wird über <see cref="Aufgeklappt"/> von der
        /// Seite aus, die auch die „höchstens eine offen"-Regel führt.
        /// </summary>
        public event EventHandler Umschalten;

        /// <summary>
        /// ETAPPE D4 — einfacher Klick auf die Karte (nicht auf ▲▼✎+×).
        ///
        /// Er ist die Auswahl, die die Schema-Ansicht mitführt. Bewusst UNMITTELBAR und
        /// nicht über <see cref="Melden"/>: Der Empfänger hebt nur etwas hervor und
        /// entsorgt keine Karte — die Begründung für die verzögerte Meldung greift hier
        /// nicht, und ein verzögerter Klick würde beim Doppelklick nach dem Editor
        /// eintreffen.
        /// </summary>
        public event EventHandler Ausgewaehlt;

        public ErzeugerKarte()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);

            BackColor = Color.White;
            Margin = new Padding(0, 0, 0, 8);
            Height = 60;

            _tip.AutoPopDelay = 15000;
            _tip.InitialDelay = 400;
            _tip.ReshowDelay = 100;

            // Aufklapp-Dreieck (Abnahmebefund 3) - Glyphe und Verhalten wie in
            // SpeicherKarte, damit beide Kartenarten dieselbe Sprache sprechen.
            _lblPfeil.Text = "▸";
            _lblPfeil.AutoSize = true;
            _lblPfeil.BackColor = Color.Transparent;
            _lblPfeil.ForeColor = KartenStil.TEXT_LEISE;
            _lblPfeil.Cursor = Cursors.Hand;
            _lblPfeil.Visible = false;
            _lblPfeil.Click += delegate { if (Umschalten != null) Umschalten(this, EventArgs.Empty); };
            _tip.SetToolTip(_lblPfeil, MyResource.Resource.SIM_KARTE_TIP_AUFKLAPPEN);

            _lblRang.AutoSize = true;
            _lblRang.ForeColor = KartenStil.TEXT_LEISE;
            _lblRang.BackColor = Color.Transparent;
            KartenStil.Schnitt(_lblRang, FontStyle.Bold);

            _lblTitel.AutoSize = false;
            _lblTitel.AutoEllipsis = true;
            _lblTitel.ForeColor = KartenStil.TEXT;
            _lblTitel.BackColor = Color.Transparent;
            _lblTitel.TextAlign = ContentAlignment.MiddleLeft;
            KartenStil.Schnitt(_lblTitel, FontStyle.Bold);

            SchalterAufbauen(_lnkAuf, "▲", MyResource.Resource.SIM_KARTE_TIP_HOCH,
                             delegate { Melden(NachOben); });
            SchalterAufbauen(_lnkAb, "▼", MyResource.Resource.SIM_KARTE_TIP_RUNTER,
                             delegate { Melden(NachUnten); });
            SchalterAufbauen(_lnkEdit, "✎", MyResource.Resource.SIM_KARTE_TIP_BEARBEITEN,
                             delegate { Melden(Bearbeiten); });
            SchalterAufbauen(_lnkEntfernen, "×", MyResource.Resource.SIM_KARTE_TIP_ENTFERNEN,
                             delegate { Melden(Entfernen); });
            SchalterAufbauen(_lnkAufnehmen, MyResource.Resource.SIM_KARTE_AUFNEHMEN,
                             MyResource.Resource.SIM_KARTE_TIP_AUFNEHMEN,
                             delegate { Melden(Aufnehmen); });
            _lnkAufnehmen.ForeColor = KartenStil.QUELLE_TEXT;

            _chips.AutoSize = true;
            _chips.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _chips.WrapContents = true;
            _chips.FlowDirection = FlowDirection.LeftToRight;
            _chips.Margin = Padding.Empty;
            _chips.Padding = Padding.Empty;
            _chips.BackColor = Color.Transparent;
            _chips.SizeChanged += delegate { HoeheNachfuehren(); };

            _detail.AutoSize = true;
            _detail.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _detail.WrapContents = true;
            _detail.FlowDirection = FlowDirection.LeftToRight;
            _detail.Margin = Padding.Empty;
            _detail.Padding = Padding.Empty;
            _detail.BackColor = Color.Transparent;
            _detail.Visible = false;
            _detail.SizeChanged += delegate { HoeheNachfuehren(); };

            Controls.Add(_lblPfeil);
            Controls.Add(_detail);
            Controls.Add(_lblRang);
            Controls.Add(_lblTitel);
            Controls.Add(_lnkAuf);
            Controls.Add(_lnkAb);
            Controls.Add(_lnkEdit);
            Controls.Add(_lnkEntfernen);
            Controls.Add(_lnkAufnehmen);
            Controls.Add(_chips);

            DoppelklickDurchreichen(this);
        }

        private void SchalterAufbauen(Label l, string glyphe, string hinweis, EventHandler klick)
        {
            Color ruhe = ReferenceEquals(l, _lnkAufnehmen)
                ? KartenStil.QUELLE_TEXT : KartenStil.TEXT_SEHR_LEISE;

            l.Text = glyphe;
            l.AutoSize = true;
            l.BackColor = Color.Transparent;
            l.ForeColor = ruhe;
            l.Cursor = Cursors.Hand;
            l.Click += klick;
            l.MouseEnter += delegate { if (l.Enabled) l.ForeColor = KartenStil.QUELLE_TEXT; };
            l.MouseLeave += delegate { l.ForeColor = l.Enabled ? ruhe : KartenStil.RAHMEN_LEISE; };
            _tip.SetToolTip(l, hinweis);
        }

        /// <summary>
        /// Doppelklick auf JEDES Kind der Karte öffnet den Editor.
        ///
        /// Ohne diesen Durchgriff wäre nur der schmale freie Streifen zwischen den
        /// Beschriftungen doppelklickbar — die Karte besteht fast vollständig aus
        /// Kindsteuerelementen, und die verschlucken das Ereignis.
        ///
        /// AUSGENOMMEN sind die drei Schalter ▲▼✎: Sie haben eigene Klick-Handler, und
        /// ein Doppelklick darauf würde sonst zusätzlich den Editor öffnen. Die Chips
        /// bekommen ihren Handler in <see cref="Setzen"/> — sie entstehen erst dort und
        /// führen ihr Ziel selbst.
        /// </summary>
        private void DoppelklickDurchreichen(Control c)
        {
            if (ReferenceEquals(c, _lnkAuf) || ReferenceEquals(c, _lnkAb) ||
                ReferenceEquals(c, _lnkEdit) || ReferenceEquals(c, _lnkAufnehmen) ||
                ReferenceEquals(c, _lnkEntfernen) || ReferenceEquals(c, _lblPfeil)) return;

            c.DoubleClick += KarteDoppelklick;
            c.Click += KarteGeklickt;                 // D4: Auswahl-Synchronisation
            foreach (Control k in c.Controls) DoppelklickDurchreichen(k);
        }

        private void KarteDoppelklick(object sender, EventArgs e)
        {
            Melden(Bearbeiten);
        }

        /// <summary>
        /// Einfacher Klick auf die Karte.
        ///
        /// <b>Abnahmebefund 2 — die ganze ZEILE schaltet um, nicht nur die Glyphe.</b>
        /// Bis hierher meldete ausschließlich <see cref="_lblPfeil"/> das Umschalten;
        /// das ist eine acht Pixel breite Trefferfläche in einer über 600 px breiten
        /// Karte, und jeder Klick daneben lief wirkungslos in die Auswahl. Genau das
        /// hat der Anwender gemeldet („trägt das Dreieck, klappt aber nichts auf").
        /// <see cref="SpeicherKarte"/> macht es seit jeher richtig: Dort reicht
        /// <c>KlickDurchreichen</c> den Klick JEDES Kindes an das Umschalten weiter.
        ///
        /// Karten ohne Detailbereich (alle Wärmeerzeuger) bleiben unberührt — dort ist
        /// <c>_detail</c> leer, und der Klick bedeutet weiterhin nur „Auswahl".
        /// </summary>
        private void KarteGeklickt(object sender, EventArgs e)
        {
            if (_detail.Controls.Count > 0 && Umschalten != null)
                Umschalten(this, EventArgs.Empty);

            if (Ausgewaehlt != null) Ausgewaehlt(this, EventArgs.Empty);
        }

        /// <summary>
        /// Meldet ein Ereignis — aber erst, NACHDEM die laufende Nachricht abgearbeitet
        /// ist.
        ///
        /// <b>Warum das nötig ist.</b> Jeder Empfänger dieser Ereignisse baut die
        /// Kartenspalte neu auf und entsorgt dabei GENAU DIESE Karte samt ihren
        /// Kindsteuerelementen (<c>SpalteLeeren</c>). Würde die Meldung unmittelbar aus
        /// dem <c>Click</c> heraus laufen, liefe die Nachrichtenverarbeitung danach in
        /// ein entsorgtes Steuerelement zurück. Im Harness ist genau das aufgeschlagen:
        /// <c>ObjectDisposedException</c> in <c>Control.CreateHandle</c>, ausgelöst aus
        /// <c>PointToScreen</c> einer Karte, die es nicht mehr gab.
        ///
        /// Vor dem ersten Anzeigen gibt es noch keine Fensterhandle und damit keine
        /// Nachrichtenschleife — dann wird direkt gemeldet. Das ist unkritisch, weil zu
        /// diesem Zeitpunkt niemand klicken kann.
        /// </summary>
        private void Melden(EventHandler ereignis)
        {
            if (ereignis == null) return;

            if (IsHandleCreated && !IsDisposed)
                BeginInvoke((MethodInvoker)delegate { ereignis(this, EventArgs.Empty); });
            else
                ereignis(this, EventArgs.Empty);
        }

        /// <summary>Setzt den Inhalt der Karte.</summary>
        public void Setzen(Aufbau a)
        {
            if (a == null) return;

            _aufbau = true;
            try
            {
                _zustand = a.Zustand;

                _lblRang.Text = a.Rang ?? "";
                _lblRang.Visible = _lblRang.Text.Length > 0;
                _lblTitel.Text = a.Titel ?? "";
                _tip.SetToolTip(_lblTitel, a.Titel ?? "");

                BackColor = _zustand == Kartenzustand.Aufgenommen
                    ? Color.White : KartenStil.FLAECHE;
                _lblTitel.ForeColor = _zustand == Kartenzustand.Aufgenommen
                    ? KartenStil.TEXT : KartenStil.TEXT_LEISE;

                // ▲▼ bleiben SICHTBAR, auch wenn sie nicht möglich sind (erster bzw.
                // letzter Rang) — nur ausgegraut. Sonst rutschte die Kopfzeile bei jedem
                // Verschieben um die Breite eines Schalters weiter. Ganz weg sind sie
                // dort, wo es keine Kaskade gibt: Strom- und Speicherseite, und bei jeder
                // noch nicht aufgenommenen Komponente.
                SchalterZustand(_lnkAuf, a.Reihenfolge, a.AufMoeglich);
                SchalterZustand(_lnkAb, a.Reihenfolge, a.AbMoeglich);

                _lnkEdit.Visible = a.Editierbar;
                _lnkEntfernen.Visible = a.Umschaltbar && _zustand == Kartenzustand.Aufgenommen;
                _lnkAufnehmen.Visible = a.Umschaltbar && _zustand == Kartenzustand.Verfuegbar;

                // Detailbereich (Abnahmebefund 3): Das Dreieck erscheint nur, wenn es
                // etwas aufzuklappen gibt - eine Karte ohne Detailchips sieht damit
                // unverändert aus.
                foreach (Control c in _detail.Controls) c.Dispose();
                _detail.Controls.Clear();

                if (a.Detailchips != null)
                {
                    foreach (ChipDaten d in a.Detailchips)
                    {
                        if (d == null || string.IsNullOrEmpty(d.Text)) continue;

                        KartenChip chip = ChipBauen(d);
                        chip.DoubleClick += KarteDoppelklick;
                        chip.Click += KarteGeklickt;
                        _detail.Controls.Add(chip);
                    }
                }

                _aufgeklappt = a.Aufgeklappt && _detail.Controls.Count > 0;
                _lblPfeil.Visible = _detail.Controls.Count > 0;
                _lblPfeil.Text = _aufgeklappt ? "▾" : "▸";
                _detail.Visible = _aufgeklappt;

                foreach (Control c in _chips.Controls) c.Dispose();
                _chips.Controls.Clear();

                IEnumerable<ChipDaten> chips = a.Chips;
                if (chips != null)
                {
                    foreach (ChipDaten d in chips)
                    {
                        if (d == null || string.IsNullOrEmpty(d.Text)) continue;

                        ChipDaten daten = d;   // eigene Bindung je Durchlauf
                        KartenChip chip = ChipBauen(daten);
                        _chips.Controls.Add(chip);

                        // D4: Ein Klick auf einen Chip ist ein Klick auf die Karte -
                        // sonst bliebe die Auswahl aus, sobald der Anwender eine Karte an
                        // ihren Chips trifft (sie füllen die untere Kartenhälfte).
                        chip.Click += KarteGeklickt;

                        if (daten.Ziel == ChipZiel.Keines)
                        {
                            chip.DoubleClick += KarteDoppelklick;
                        }
                        else
                        {
                            chip.Cursor = Cursors.Hand;
                            chip.DoubleClick += delegate
                            {
                                // Verzögert wie in Melden — der Editor baut die Spalte
                                // neu auf und entsorgt dabei diese Karte samt Chip.
                                Action<ChipDaten> ziel = ChipBearbeiten;
                                if (ziel == null) return;

                                if (IsHandleCreated && !IsDisposed)
                                    BeginInvoke((MethodInvoker)delegate { ziel(daten); });
                                else
                                    ziel(daten);
                            };
                        }
                    }
                }
            }
            finally { _aufbau = false; }

            Neuordnen();
        }

        private void SchalterZustand(Label l, bool sichtbar, bool moeglich)
        {
            l.Visible = sichtbar;
            l.Enabled = moeglich;
            l.ForeColor = moeglich ? KartenStil.TEXT_SEHR_LEISE : KartenStil.RAHMEN_LEISE;
        }

        private KartenChip ChipBauen(ChipDaten d)
        {
            KartenChip chip = new KartenChip();
            chip.Text = d.Text;

            switch (d.Stil)
            {
                case ChipStil.Quelle:
                    chip.RandFarbe = KartenStil.QUELLE_RAHMEN;
                    chip.ForeColor = KartenStil.QUELLE_TEXT;
                    break;

                case ChipStil.QuelleKaskade:
                    chip.RandFarbe = KartenStil.QUELLE_RAHMEN;
                    chip.ForeColor = KartenStil.QUELLE_TEXT;
                    chip.Gestrichelt = true;
                    break;

                case ChipStil.Senke:
                    chip.RandFarbe = KartenStil.SENKE_RAHMEN;
                    chip.ForeColor = KartenStil.SENKE_TEXT;
                    break;

                case ChipStil.Flaeche:
                    chip.OhneRand = true;
                    chip.BackColor = KartenStil.FLAECHE;
                    break;

                case ChipStil.Warnung:
                    chip.RandFarbe = KartenStil.WARN_RAHMEN;
                    chip.BackColor = KartenStil.WARN_FLAECHE;
                    chip.ForeColor = KartenStil.WARN_TEXT;
                    break;
            }

            if (!string.IsNullOrEmpty(d.Hinweis))
                _tip.SetToolTip(chip, Zeilenumbruch.Normalisieren(d.Hinweis));

            return chip;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (!_aufbau) Neuordnen();
        }

        /// <summary>
        /// ABNAHMEBEFUND 4 — beim Sichtbarwerden einmal nachordnen.
        ///
        /// <see cref="Neuordnen"/> fragt die Schalter über den Visible-GETTER ab, und
        /// der liefert den WIRKSAMEN Zustand — also false für ALLE Kinder, solange das
        /// übergeordnete Fenster noch nicht angezeigt wird (dieselbe Falle wie bei
        /// <see cref="HoeheNachfuehren"/> beschrieben). Die Karten entstehen aber in
        /// <c>SetControls</c>, also VOR dem ersten Anzeigen: Neuordnen platzierte dort
        /// keinen einzigen Schalter (▲▼✎× blieben auf Restkoordinaten außerhalb der
        /// Karte; im Harness gemessen: ✎ bei x = −172 auf einer 622 px breiten Karte)
        /// und schob den Titel auf die Rangposition, dessen Anfang das Rang-Label dann
        /// verdeckte. Blieb die Fenstergröße nach dem Anzeigen unverändert, kam kein
        /// OnResize mehr — die Kopfzeile stand dauerhaft falsch. Genau deshalb wirkte
        /// der Fehler sporadisch: Er heilte überall dort, wo BaseForm oder der Anwender
        /// das Fenster nach dem Anzeigen noch umformte.
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (Visible && !_aufbau && !IsDisposed) Neuordnen();
        }

        private void Neuordnen()
        {
            if (_aufbau) return;

            int y = KartenStil.RAND;
            int links = KartenStil.RAND;

            if (_lblPfeil.Visible)
            {
                _lblPfeil.Location = new Point(links, y);
                links = _lblPfeil.Right + 4;
            }

            _lblRang.Location = new Point(links, y);

            // Schalter von rechts nach links: ×/+ ganz außen (die Auswahl),
            // davor ✎, davor ▼ und ▲ (die Reihenfolge).
            int x = ClientSize.Width - KartenStil.RAND;
            Label[] schalter = { _lnkEntfernen, _lnkAufnehmen, _lnkEdit, _lnkAb, _lnkAuf };
            foreach (Label l in schalter)
            {
                if (!l.Visible) continue;
                x -= l.Width;
                l.Location = new Point(x, y);
                x -= 6;
            }

            int titelLinks = _lblRang.Visible ? _lblRang.Right + 8 : links;
            int titelBreite = Math.Max(40, x - 8 - titelLinks);
            _lblTitel.Bounds = new Rectangle(titelLinks, y, titelBreite, _lblRang.Height);

            int kopfUnten = _lblTitel.Bottom;
            foreach (Label l in schalter) if (l.Visible && l.Bottom > kopfUnten) kopfUnten = l.Bottom;

            int innen = Math.Max(60, ClientSize.Width - 2 * KartenStil.RAND);

            _chips.Location = new Point(KartenStil.RAND, kopfUnten + 6);
            Innenbreite(_chips, innen);

            // Der Detailbereich steht UNTER den Chips und in derselben Innenbreite.
            _detail.Location = new Point(KartenStil.RAND,
                                         (_chips.Controls.Count > 0 ? _chips.Bottom : kopfUnten) + 6);
            Innenbreite(_detail, innen);

            HoeheNachfuehren();
        }

        /// <summary>
        /// Zwingt einen Chipbereich auf die Innenbreite der Karte.
        ///
        /// <b>Abnahmebefund 2 — die Breite allein genügt nicht.</b> Ein
        /// <see cref="FlowLayoutPanel"/> mit <c>AutoSize</c> und
        /// <see cref="AutoSizeMode.GrowAndShrink"/> misst seine Wunschgröße OHNE
        /// Breitenvorgabe und legt deshalb alle Chips in EINE Zeile — die gesetzte
        /// Breite überschreibt die nächste Layoutrunde wieder, und
        /// <c>WrapContents</c> bleibt wirkungslos. Im Harness gemessen: 1213 px
        /// Detailbereich in einer 622 px breiten Karte (acht Gerätechips des
        /// Stromspeichers), also die Hälfte der Gerätedaten außerhalb des
        /// Kartenrands abgeschnitten. Erst <see cref="Control.MaximumSize"/>
        /// begrenzt die Messung, und der Umbruch greift.
        ///
        /// Dasselbe Mittel wie in <c>SpeicherKarte.Neuordnen</c>, wo die Detailzeilen
        /// über <c>MaximumSize</c> auf die Innenbreite der KARTE gestellt werden.
        /// </summary>
        private static void Innenbreite(FlowLayoutPanel bereich, int breite)
        {
            if (bereich.MaximumSize.Width != breite)
                bereich.MaximumSize = new Size(breite, 0);
            if (bereich.Width != breite) bereich.Width = breite;
        }

        /// <summary>
        /// Höhe der Karte an ihren Inhalt anpassen.
        ///
        /// Maßgeblich ist <see cref="_aufgeklappt"/> und NICHT <c>_detail.Visible</c> —
        /// dieselbe Begründung wie in <c>SpeicherKarte.HoeheNachfuehren</c>: Der
        /// Visible-GETTER liefert den WIRKSAMEN Zustand und damit <c>false</c>, solange
        /// das übergeordnete Fenster noch nicht angezeigt wird. Die Karten entstehen
        /// aber in <c>SetControls</c>, also VOR dem ersten Anzeigen; eine dort bereits
        /// aufgeklappte Karte (<c>_offeneStromgruppe</c>) bekäme sonst die zugeklappte
        /// Höhe und richtete sich erst bei der nächsten Größenänderung.
        /// </summary>
        private void HoeheNachfuehren()
        {
            int noetig = _chips.Bottom + KartenStil.RAND;
            if (_chips.Controls.Count == 0) noetig = _lblTitel.Bottom + KartenStil.RAND;
            if (_aufgeklappt && _detail.Controls.Count > 0) noetig = _detail.Bottom + KartenStil.RAND;
            if (Height != noetig) Height = noetig;
        }

        /// <summary>
        /// Übernimmt <see cref="Aufgeklappt"/> in Pfeil, Sichtbarkeit und Höhe
        /// (Abnahmebefund 3).
        /// </summary>
        private void DetailZustandAnwenden()
        {
            _lblPfeil.Text = _aufgeklappt ? "▾" : "▸";
            _detail.Visible = _aufgeklappt && _detail.Controls.Count > 0;
            Neuordnen();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent != null ? Parent.BackColor : SystemColors.Control);

            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (GraphicsPath p = KartenStil.Rundeck(r, KartenStil.ECKE))
            {
                using (SolidBrush b = new SolidBrush(BackColor)) e.Graphics.FillPath(b, p);

                // Nicht aufgenommene Komponenten stehen gestrichelt und in der
                // Flächenfarbe da — dieselbe Sprache wie beim Kaskaden-Quellchip:
                // gestrichelt heißt „gehört dazu, ist aber nicht der Normalfall".
                using (Pen stift = new Pen(_hervorgehoben
                                               ? KartenStil.QUELLE_RAHMEN
                                               : _zustand == Kartenzustand.Aufgenommen
                                                   ? KartenStil.RAHMEN : KartenStil.RAHMEN_LEISE,
                                           _hervorgehoben ? 2f : 1f))
                {
                    if (_zustand == Kartenzustand.Verfuegbar) stift.DashStyle = DashStyle.Dash;
                    e.Graphics.DrawPath(stift, p);
                }
            }

            base.OnPaint(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _tip.Dispose();
            base.Dispose(disposing);
        }
    }
}
