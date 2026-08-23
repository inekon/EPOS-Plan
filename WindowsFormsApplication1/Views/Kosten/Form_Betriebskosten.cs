using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Betriebskosten nach VDI 2067 (Etappe E3, Konzept <c>Konzept_BHKW_Kosten_Erloese.md</c>
    /// Abschnitt 5) — die zwölf Positionen der Altmaske <c>Dial_BetriebKost</c> in drei
    /// Spalten: <b>Satz</b> (Prozent bzw. €/h, €/kWh) · <b>Betrag netto</b> ·
    /// <b>Betrag brutto</b> (abgeleitet, gesperrt).
    ///
    /// <para>
    /// <b>Was aus der Altanwendung NICHT übernommen wird</b> (Analyse, Abschnitt 5):
    /// </para>
    /// <list type="bullet">
    ///   <item><description><b>Befund 5</b> — dort mischten die Prozentbasen netto und
    ///   brutto. Hier ist <b>jede</b> Bezugsgröße netto (L8), und der Umsatzsteuersatz
    ///   kommt aus <see cref="GesetzKatalog"/> statt aus einer hart codierten 1,19.</description></item>
    ///   <item><description><b>Befund 6</b> — dort überschrieben sich die drei
    ///   Wartungsfelder kommentarlos. Hier gilt genau EINE Bemessung, sichtbar ausgewählt,
    ///   die übrigen Felder sind gesperrt (L7).</description></item>
    ///   <item><description><b>Befund 7</b> — dort stand „oder Instandhaltung BHKW", und
    ///   der Betrag wurde trotzdem addiert. Hier sind es zwei eigene Zeilen, und der
    ///   Hinweistext sagt ausdrücklich, dass sie sich addieren.</description></item>
    ///   <item><description>Die Absolutfelder werden bei gepflegtem Satz <b>gesperrt und
    ///   gekennzeichnet</b>, nicht still geleert.</description></item>
    /// </list>
    ///
    /// <para>
    /// <b>Ohne Designer-Datei</b> — wie <see cref="Form_Gesetzesparameter"/>:
    /// Die Maske ist ein Raster aus zwölf
    /// gleichartigen Zeilen, und der WinForms-Designer brächte drei weitere Dateien ohne
    /// Gegenwert. Alle Anzeigetexte kommen aus <c>MyResource</c>, alle Steuer- und
    /// Datenbankwerte aus <see cref="DbWerte"/>.
    /// </para>
    /// </summary>
    internal class Form_Betriebskosten : Form
    {
        private readonly int _projektID;
        private readonly BetriebskostenCtrl.Bezugsgroessen _bezug;
        private readonly List<BetriebskostenCtrl.Zeile> _zeilen;

        /// <summary>Umsatzsteuersatz [%] aus dem Katalog; null = nicht gepflegt.</summary>
        private readonly double? _ustProzent;

        private readonly List<Steuerung> _steuerungen = new List<Steuerung>();
        private Label _lblSummeNetto;
        private Label _lblSummeBrutto;
        private Panel _liste;

        /// <summary>Zahl der beim Bestätigen geschriebenen Zeilen.</summary>
        internal int GeschriebeneZeilen { get; private set; }

        /// <summary>Die Steuerelemente einer Zeile, damit sie sich gegenseitig sperren können.</summary>
        private sealed class Steuerung
        {
            public BetriebskostenCtrl.Zeile Daten;
            public ComboBox Bemessung;       // null, wenn die Position nur eine Art kennt
            public Label BemessungFest;      // Anzeige, wenn es keine Auswahl gibt
            public NumericUpDown Satz;
            public Label SatzEinheit;
            public NumericUpDown Netto;
            public Label Brutto;
            public Label Bezug;
        }

        internal Form_Betriebskosten(int projektID)
        {
            _projektID = projektID;

            KostenPositionCtrl.StelleSpaltenSicher();
            _bezug = BetriebskostenCtrl.LiesBezugsgroessen(projektID);
            _zeilen = BetriebskostenCtrl.Lies(projektID, _bezug);
            _ustProzent = new GesetzKatalog().Wert(DbWerte.GESETZ_UMSATZSTEUER_REGELSATZ,
                                                   DateTime.Now.Year);

            Aufbauen();
            ZeilenAufbauen();
            AllesNachziehen();
        }

        // ------------------------------------------------------------------ Aufbau

        private const int SP_NAME = 12;
        private const int SP_BEMESSUNG = 250;
        private const int SP_SATZ = 420;
        private const int SP_EINHEIT = 500;
        private const int SP_NETTO = 560;
        private const int SP_BRUTTO = 680;
        private const int SP_BEZUG = 800;
        private const int ZEILE_H = 28;
        private const int BREITE = 1160;

        private void Aufbauen()
        {
            Text = MyResource.Resource.VDI_TITEL;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            MinimizeBox = false;
            MaximizeBox = false;
            ClientSize = new Size(BREITE, 620);
            MinimumSize = new Size(900, 480);
            Font = new Font("Segoe UI", 9f);
            Name = "Form_Betriebskosten";

            Label lblKopf = new Label
            {
                Name = "lblKopf",
                Text = MyResource.Resource.VDI_HINWEIS,
                Location = new Point(12, 8),
                Size = new Size(BREITE - 24, 32),
                AutoSize = false,
                ForeColor = Color.FromArgb(0, 90, 160),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(lblKopf);

            Panel kopfzeile = new Panel
            {
                Name = "pnlKopfzeile",
                Location = new Point(12, 46),
                Size = new Size(BREITE - 24, 24),
                BackColor = Color.FromArgb(20, 40, 80),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Spaltenkopf(kopfzeile, MyResource.Resource.VDI_SP_POSITION, SP_NAME, SP_BEMESSUNG - SP_NAME - 6);
            Spaltenkopf(kopfzeile, MyResource.Resource.VDI_SP_BEMESSUNG, SP_BEMESSUNG, SP_SATZ - SP_BEMESSUNG - 6);
            Spaltenkopf(kopfzeile, MyResource.Resource.VDI_SP_SATZ, SP_SATZ, SP_NETTO - SP_SATZ - 6);
            Spaltenkopf(kopfzeile, MyResource.Resource.VDI_SP_NETTO, SP_NETTO, SP_BRUTTO - SP_NETTO - 6);
            Spaltenkopf(kopfzeile, MyResource.Resource.VDI_SP_BRUTTO, SP_BRUTTO, SP_BEZUG - SP_BRUTTO - 6);
            Spaltenkopf(kopfzeile, MyResource.Resource.VDI_SP_BEZUG, SP_BEZUG, BREITE - SP_BEZUG - 40);
            Controls.Add(kopfzeile);

            _liste = new Panel
            {
                Name = "pnlListe",
                Location = new Point(12, 72),
                Size = new Size(BREITE - 24, 12 * ZEILE_H + 8),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_liste);

            int y = _liste.Bottom + 8;

            _lblSummeNetto = new Label
            {
                Name = "lblSummeNetto",
                Location = new Point(12, y),
                Size = new Size(BREITE - 24, 20),
                AutoSize = false,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_lblSummeNetto);

            _lblSummeBrutto = new Label
            {
                Name = "lblSummeBrutto",
                Location = new Point(12, y + 22),
                Size = new Size(BREITE - 24, 20),
                AutoSize = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_lblSummeBrutto);

            Label lblFuss = new Label
            {
                Name = "lblFussHinweise",
                Text = MyResource.Resource.VDI_HINWEIS_INSTANDHALTUNG + Environment.NewLine +
                       MyResource.Resource.VDI_VBH_NAEHERUNG,
                Location = new Point(12, y + 48),
                Size = new Size(BREITE - 24, 44),
                AutoSize = false,
                ForeColor = Color.FromArgb(0x8A, 0x4B, 0x00),
                BackColor = Color.FromArgb(0xFF, 0xF4, 0xD9),
                Padding = new Padding(6, 3, 6, 0),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(lblFuss);

            Button btnOk = new Button
            {
                Name = "btnOk",
                Text = MyResource.Resource.VDI_BTN_OK,
                Size = new Size(150, 28),
                Location = new Point(BREITE - 320, y + 100),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnOk.Click += btnOk_Click;
            Controls.Add(btnOk);

            Button btnAbbruch = new Button
            {
                Name = "btnAbbruch",
                Text = MyResource.Resource.VDI_BTN_ABBRUCH,
                Size = new Size(140, 28),
                Location = new Point(BREITE - 160, y + 100),
                DialogResult = DialogResult.Cancel,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            Controls.Add(btnAbbruch);

            AcceptButton = btnOk;
            CancelButton = btnAbbruch;
        }

        private static void Spaltenkopf(Panel wirt, string text, int x, int breite)
        {
            wirt.Controls.Add(new Label
            {
                Text = text,
                Location = new Point(x, 4),
                Size = new Size(breite, 18),
                AutoSize = false,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold)
            });
        }

        private void ZeilenAufbauen()
        {
            int y = 4;

            foreach (BetriebskostenCtrl.Zeile z in _zeilen)
            {
                var s = new Steuerung { Daten = z };

                _liste.Controls.Add(new Label
                {
                    Text = PositionName(z.Pos),
                    Location = new Point(SP_NAME, y + 4),
                    Size = new Size(SP_BEMESSUNG - SP_NAME - 6, 20),
                    AutoSize = false
                });

                // --- Bemessung: Auswahl nur, wo es wirklich etwas zu wählen gibt (L7) ---
                if (z.Pos.Bemessungen != null && z.Pos.Bemessungen.Length > 1)
                {
                    s.Bemessung = new ComboBox
                    {
                        Name = "cbBemessung_" + z.Pos.Bezeichnung,
                        DropDownStyle = ComboBoxStyle.DropDownList,
                        Location = new Point(SP_BEMESSUNG, y + 1),
                        Size = new Size(SP_SATZ - SP_BEMESSUNG - 10, 22)
                    };
                    foreach (string b in z.Pos.Bemessungen)
                        s.Bemessung.Items.Add(new BemessungItem(b, BemessungName(b)));
                    WaehleBemessung(s.Bemessung, z.Bemessung);
                    s.Bemessung.SelectedIndexChanged += (o, e) => BemessungGewechselt(s);
                    _liste.Controls.Add(s.Bemessung);
                }
                else
                {
                    s.BemessungFest = new Label
                    {
                        Text = BemessungName(z.Bemessung),
                        Location = new Point(SP_BEMESSUNG, y + 4),
                        Size = new Size(SP_SATZ - SP_BEMESSUNG - 10, 20),
                        AutoSize = false,
                        ForeColor = Color.DimGray
                    };
                    _liste.Controls.Add(s.BemessungFest);
                }

                // --- Satz -----------------------------------------------------------
                s.Satz = new NumericUpDown
                {
                    Name = "numSatz_" + z.Pos.Bezeichnung,
                    Location = new Point(SP_SATZ, y + 1),
                    Size = new Size(SP_EINHEIT - SP_SATZ - 8, 22),
                    DecimalPlaces = 4,
                    Increment = 0.1M,
                    Minimum = 0M,
                    Maximum = 1000000M,
                    TextAlign = HorizontalAlignment.Right
                };
                if (z.Satz.HasValue) s.Satz.Value = Klemme(z.Satz.Value, s.Satz);
                s.Satz.ValueChanged += (o, e) => ZeileNachziehen(s, true);
                _liste.Controls.Add(s.Satz);

                s.SatzEinheit = new Label
                {
                    Location = new Point(SP_EINHEIT, y + 4),
                    Size = new Size(SP_NETTO - SP_EINHEIT - 6, 20),
                    AutoSize = false,
                    ForeColor = Color.DimGray
                };
                _liste.Controls.Add(s.SatzEinheit);

                // --- Betrag netto ---------------------------------------------------
                s.Netto = new NumericUpDown
                {
                    Name = "numNetto_" + z.Pos.Bezeichnung,
                    Location = new Point(SP_NETTO, y + 1),
                    Size = new Size(SP_BRUTTO - SP_NETTO - 10, 22),
                    DecimalPlaces = 2,
                    Increment = 10M,
                    Minimum = 0M,
                    Maximum = 100000000M,
                    TextAlign = HorizontalAlignment.Right
                };
                // Erfassten Betrag anzeigen, BEVOR der Handler hängt: das Setzen von Value
                // feuert ValueChanged, und ZeileNachziehen greift auf Steuerelemente zu,
                // die erst weiter unten entstehen.
                s.Netto.Value = Klemme(z.Fest, s.Netto);
                s.Netto.ValueChanged += (o, e) => ZeileNachziehen(s, false);
                _liste.Controls.Add(s.Netto);

                // --- Betrag brutto (abgeleitet, gesperrt) ---------------------------
                s.Brutto = new Label
                {
                    Name = "lblBrutto_" + z.Pos.Bezeichnung,
                    Location = new Point(SP_BRUTTO, y + 4),
                    Size = new Size(SP_BEZUG - SP_BRUTTO - 10, 20),
                    AutoSize = false,
                    TextAlign = ContentAlignment.MiddleRight,
                    ForeColor = Color.DimGray
                };
                _liste.Controls.Add(s.Brutto);

                // --- Bezugsgröße und Empfehlung -------------------------------------
                s.Bezug = new Label
                {
                    Name = "lblBezug_" + z.Pos.Bezeichnung,
                    Location = new Point(SP_BEZUG, y + 4),
                    Size = new Size(BREITE - SP_BEZUG - 50, 20),
                    AutoSize = false,
                    ForeColor = Color.DimGray
                };
                _liste.Controls.Add(s.Bezug);

                _steuerungen.Add(s);
                y += ZEILE_H;
            }
        }

        // ------------------------------------------------------------------ Verhalten

        private void BemessungGewechselt(Steuerung s)
        {
            s.Daten.Bemessung = GewaehlteBemessung(s);
            s.Daten.Menge = _bezug.Wert(s.Daten.Pos.BezugZu(s.Daten.Bemessung));
            AllesNachziehen();
        }

        /// <summary>
        /// Zieht eine Zeile nach: Sperrzustand, Brutto, Bezugsanzeige.
        /// </summary>
        /// <param name="vomSatz">
        /// true = die Änderung kam aus dem Satzfeld. Nur dann wird der Nettobetrag
        /// überschrieben — sonst höbe das Nachziehen jede Eingabe im Absolutfeld sofort
        /// wieder auf.
        /// </param>
        private void ZeileNachziehen(Steuerung s, bool vomSatz)
        {
            if (_stille) return;
            _stille = true;
            try
            {
                BetriebskostenCtrl.Zeile z = s.Daten;
                z.Bemessung = GewaehlteBemessung(s);
                z.Satz = s.Satz.Value > 0 ? (double?)(double)s.Satz.Value : null;
                z.Menge = _bezug.Wert(z.Pos.BezugZu(z.Bemessung));

                bool festerBetrag = string.Equals(z.Bemessung, DbWerte.BEMESSUNG_BETRAG,
                                                  StringComparison.Ordinal);

                // Satzfeld nur dort, wo die Bemessung einen Satz kennt.
                s.Satz.Enabled = !festerBetrag;
                s.SatzEinheit.Text = festerBetrag ? "" : BetriebskostenCtrl.SatzEinheit(z.Bemessung);

                // VORRANG: gepflegter Satz schlägt die Absolutangabe. Das Absolutfeld wird
                // GESPERRT und GEKENNZEICHNET, nicht still geleert (Konzept 4.1).
                bool abgeleitet = !festerBetrag && z.Satz.HasValue;

                if (abgeleitet)
                {
                    if (vomSatz || s.Netto.ReadOnly == false)
                        s.Netto.Value = Klemme(z.Netto, s.Netto);
                    s.Netto.ReadOnly = true;
                    s.Netto.BackColor = SystemColors.Control;
                    s.Netto.Increment = 0M;
                    ToolTipSetzen(s.Netto, MyResource.Resource.VDI_ERSETZT);
                }
                else
                {
                    s.Netto.ReadOnly = false;
                    s.Netto.BackColor = SystemColors.Window;
                    s.Netto.Increment = 10M;
                    ToolTipSetzen(s.Netto, "");
                    z.Fest = (double)s.Netto.Value;
                }

                double netto = abgeleitet ? z.Netto : (double)s.Netto.Value;
                s.Brutto.Text = _ustProzent.HasValue
                    ? (netto * (1.0 + _ustProzent.Value / 100.0)).ToString("N2", BerichtTexte.Kultur)
                    : MyResource.Resource.VDI_UST_FEHLT;

                s.Bezug.Text = Bezugstext(z);
            }
            finally { _stille = false; }

            SummenNachziehen();
        }

        private bool _stille;

        private void AllesNachziehen()
        {
            foreach (Steuerung s in _steuerungen) ZeileNachziehen(s, true);
            SummenNachziehen();
        }

        private void SummenNachziehen()
        {
            double netto = 0;
            foreach (Steuerung s in _steuerungen)
            {
                bool festerBetrag = string.Equals(s.Daten.Bemessung, DbWerte.BEMESSUNG_BETRAG,
                                                  StringComparison.Ordinal);
                netto += (festerBetrag || !s.Daten.Satz.HasValue)
                    ? (double)s.Netto.Value : s.Daten.Netto;
            }

            _lblSummeNetto.Text = string.Format(MyResource.Resource.VDI_SUMME_NETTO,
                                                netto.ToString("N2", BerichtTexte.Kultur));
            _lblSummeBrutto.Text = _ustProzent.HasValue
                ? string.Format(MyResource.Resource.VDI_SUMME_BRUTTO,
                                (netto * (1.0 + _ustProzent.Value / 100.0)).ToString("N2", BerichtTexte.Kultur),
                                _ustProzent.Value.ToString("N1", BerichtTexte.Kultur))
                : MyResource.Resource.VDI_UST_FEHLT;
        }

        /// <summary>Bezugsgröße im Klartext plus Empfehlungsbereich der VDI 2067.</summary>
        private string Bezugstext(BetriebskostenCtrl.Zeile z)
        {
            string bezug = z.Pos.BezugZu(z.Bemessung);
            string text;

            if (string.Equals(bezug, BetriebskostenCtrl.BEZUG_KEINE, StringComparison.Ordinal))
                text = "";
            else if (!z.Menge.HasValue)
                text = BezugName(bezug) + ": " + MyResource.Resource.VDI_BEZUG_FEHLT;
            else
                text = BezugName(bezug) + ": " + z.Menge.Value.ToString("N2", BerichtTexte.Kultur) +
                       " " + BetriebskostenCtrl.MengenEinheit(z.Bemessung);

            if (z.Pos.EmpfehlungBis > 0)
            {
                string e = string.Format(MyResource.Resource.VDI_EMPFEHLUNG,
                                         z.Pos.EmpfehlungVon.ToString("N1", BerichtTexte.Kultur),
                                         z.Pos.EmpfehlungBis.ToString("N1", BerichtTexte.Kultur));
                text = string.IsNullOrEmpty(text) ? e : text + "   " + e;
            }
            return text;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            foreach (Steuerung s in _steuerungen)
            {
                s.Daten.Bemessung = GewaehlteBemessung(s);
                s.Daten.Satz = s.Satz.Value > 0 ? (double?)(double)s.Satz.Value : null;
                s.Daten.Menge = _bezug.Wert(s.Daten.Pos.BezugZu(s.Daten.Bemessung));
                if (!s.Netto.ReadOnly) s.Daten.Fest = (double)s.Netto.Value;
            }

            GeschriebeneZeilen = BetriebskostenCtrl.Speichere(_projektID, _zeilen);
            DialogResult = DialogResult.OK;
            Close();
        }

        // ------------------------------------------------------------------ Hilfsmittel

        /// <summary>
        /// Trägt den DB-Wert und zeigt den lokalisierten Namen — kein Anzeigetext ist je
        /// Steuerwert (Drei-Schichten-Regel). Muster <c>Form_Gesetzesparameter.KlasseItem</c>.
        /// </summary>
        internal sealed class BemessungItem
        {
            public BemessungItem(string wert, string anzeige) { Wert = wert; Anzeige = anzeige; }
            public string Wert { get; private set; }
            public string Anzeige { get; private set; }
            public override string ToString() { return Anzeige; }
        }

        private static void WaehleBemessung(ComboBox cb, string wert)
        {
            for (int i = 0; i < cb.Items.Count; i++)
            {
                BemessungItem it = cb.Items[i] as BemessungItem;
                if (it != null && string.Equals(it.Wert, wert, StringComparison.Ordinal))
                { cb.SelectedIndex = i; return; }
            }
            if (cb.Items.Count > 0) cb.SelectedIndex = 0;
        }

        private static string GewaehlteBemessung(Steuerung s)
        {
            if (s.Bemessung == null) return s.Daten.Bemessung;
            BemessungItem it = s.Bemessung.SelectedItem as BemessungItem;
            return it != null ? it.Wert : s.Daten.Bemessung;
        }

        private static decimal Klemme(double wert, NumericUpDown feld)
        {
            decimal d;
            try { d = (decimal)wert; } catch { return feld.Minimum; }
            if (d < feld.Minimum) return feld.Minimum;
            if (d > feld.Maximum) return feld.Maximum;
            return d;
        }

        private ToolTip _tip;
        private void ToolTipSetzen(Control c, string text)
        {
            if (_tip == null) _tip = new ToolTip();
            _tip.SetToolTip(c, text ?? "");
        }

        /// <summary>Anzeigename einer Position — Persistenzwert → Ressourcenschlüssel.</summary>
        private static string PositionName(BetriebskostenCtrl.Position p)
        {
            switch (p.Bezeichnung)
            {
                case DbWerte.VDI_POS_WARTUNG_BHKW: return MyResource.Resource.VDI_POS_ANZ_WARTUNG_BHKW;
                case DbWerte.VDI_POS_INSTANDHALTUNG_BHKW: return MyResource.Resource.VDI_POS_ANZ_INST_BHKW;
                case DbWerte.VDI_POS_INSTANDHALTUNG_KESSEL: return MyResource.Resource.VDI_POS_ANZ_INST_KESSEL;
                case DbWerte.VDI_POS_INSTANDHALTUNG_WAERMEZENTRALE: return MyResource.Resource.VDI_POS_ANZ_INST_ZENTRALE;
                case DbWerte.VDI_POS_INSTANDHALTUNG_BAULICH: return MyResource.Resource.VDI_POS_ANZ_INST_BAULICH;
                case DbWerte.VDI_POS_INSTANDHALTUNG_STROMEINSPEISUNG: return MyResource.Resource.VDI_POS_ANZ_INST_EINSPEISUNG;
                case DbWerte.VDI_POS_PERSONAL: return MyResource.Resource.VDI_POS_ANZ_PERSONAL;
                case DbWerte.VDI_POS_VERWALTUNG: return MyResource.Resource.VDI_POS_ANZ_VERWALTUNG;
                case DbWerte.VDI_POS_HILFSENERGIE: return MyResource.Resource.VDI_POS_ANZ_HILFSENERGIE;
                case DbWerte.VDI_POS_RESERVELEISTUNG: return MyResource.Resource.VDI_POS_ANZ_RESERVE;
                case DbWerte.VDI_POS_SONSTIGE: return MyResource.Resource.VDI_POS_ANZ_SONSTIGE;
                default: return p.Bezeichnung;
            }
        }

        /// <summary>Anzeigename einer Bemessungsart — Persistenzwert → Ressourcenschlüssel.</summary>
        private static string BemessungName(string bemessung)
        {
            switch (bemessung)
            {
                case DbWerte.BEMESSUNG_BETRAG: return MyResource.Resource.VDI_BEM_ANZ_BETRAG;
                case DbWerte.BEMESSUNG_PROZENT_INVESTITION: return MyResource.Resource.VDI_BEM_ANZ_PROZ_INVEST;
                case DbWerte.BEMESSUNG_EUR_PRO_H: return MyResource.Resource.VDI_BEM_ANZ_EUR_H;
                case DbWerte.BEMESSUNG_EUR_PRO_KWH: return MyResource.Resource.VDI_BEM_ANZ_EUR_KWH;
                case DbWerte.BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN: return MyResource.Resource.VDI_BEM_ANZ_PROZ_BRENNSTOFF;
                default: return bemessung ?? "";
            }
        }

        /// <summary>Anzeigename einer Bezugsgröße — Steuerwert → Ressourcenschlüssel.</summary>
        private static string BezugName(string bezug)
        {
            switch (bezug)
            {
                case BetriebskostenCtrl.BEZUG_INVEST_BHKW: return MyResource.Resource.VDI_BEZUG_INVEST_BHKW;
                case BetriebskostenCtrl.BEZUG_INVEST_KESSEL: return MyResource.Resource.VDI_BEZUG_INVEST_KESSEL;
                case BetriebskostenCtrl.BEZUG_INVEST_GESAMT: return MyResource.Resource.VDI_BEZUG_INVEST_GESAMT;
                case BetriebskostenCtrl.BEZUG_STROM_BHKW: return MyResource.Resource.VDI_BEZUG_STROM;
                case BetriebskostenCtrl.BEZUG_VBH_BHKW: return MyResource.Resource.VDI_BEZUG_VBH;
                case BetriebskostenCtrl.BEZUG_BRENNSTOFFKOSTEN: return MyResource.Resource.VDI_BEZUG_BRENNSTOFF;
                default: return "";
            }
        }
    }
}
