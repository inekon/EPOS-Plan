using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Dialog „BHKW-Wirtschaftlichkeit“ (Etappe B5, Konzept
    /// <c>Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md</c> § 6.1, Leitentscheidung BW9).
    ///
    /// <para><b>Er bündelt, was bisher an zwei Orten lag:</b> die Angaben je BHKW-Modul
    /// (bis B5 <see cref="Form_KwkgModule"/>) und die beiden BHKW-Gruppen des
    /// Parameterdialogs — „BHKW — KWKG 2025“ sowie „BHKW — Energie- und Stromsteuer“.
    /// <see cref="Form_WirtschaftlichkeitParameter"/> blendet sie seit B5 aus (BW9) und
    /// verweist hierher.</para>
    ///
    /// <para><b>Code-only, ohne Designer und ohne eigene <c>.resx</c></b> — Bauvorbild
    /// <c>ucBrennstoffBestandteile</c> (B2) und <see cref="Form_WirtschaftlichkeitParameter"/>,
    /// der ebenfalls vollständig im Code entsteht. Damit entfällt die Designer-Pflegefalle;
    /// alle sichtbaren Texte laufen über das Rückfallmuster <see cref="T"/> mit den
    /// Schlüsseln <c>BHW_*</c> (Konzept § 6.4).</para>
    ///
    /// <para><b>Der Dialog rechnet nichts.</b> Sätze und Herleitungen kommen aus
    /// <see cref="KwkgSatzRechner"/>, die Vorschauzahlen und die Steuerherkunft aus dem
    /// <b>zuletzt gebuchten</b> Ergebnis des einen Rechenwegs
    /// (<see cref="WirtschaftlichkeitCtrl.LadeErgebnisse"/>), die Mengenkette aus
    /// <see cref="KwkgModulNachweis"/> (B3b) und die Kohärenzzeilen aus
    /// <see cref="KohaerenzPruefung"/> bzw. aus den durchgereichten Hinweisen des letzten
    /// Laufs. Eine Zweitrechnung gibt es nicht — sie wäre genau die zweite Wahrheit, die
    /// BW8 verhindern soll.</para>
    ///
    /// <para><b>Nullsemantik durchgehend:</b> An der Anlage heißt leer bzw. 0 „kein
    /// eigener Wert — es gilt die Projektvorgabe“ (E6-/B3a-Muster); nur beim
    /// Hilfsenergieanteil heißt 0 „keine Hilfsenergie“ (BF4).</para>
    /// </summary>
    public class Form_BhkwWirtschaftlichkeit : Form
    {
        // ------------------------------------------------------------ Hausmaße (§ 5)

        private const int FENSTER_BREITE = 914;
        private const int RAND = 16;
        private const int INHALT_BREITE = FENSTER_BREITE - 2 * RAND;     // 882
        private const int SPALTE_RECHTS = 464;
        private const int GRUPPE_BREITE = 432;
        private const int GRUPPE_BREITE_R = 434;
        private const int ZEILE = 30;

        private static readonly Color FARBE_KOPF = ColorTranslator.FromHtml("#0F1F3D");
        private static readonly Color FARBE_VORSCHAU = ColorTranslator.FromHtml("#1A3261");
        private static readonly Color FARBE_LEISE = ColorTranslator.FromHtml("#5A5A5A");

        // ------------------------------------------------------------ Zustand

        private readonly int _idStamm;
        private readonly string _stammName;
        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();
        private readonly KwkgAnlagenCtrl _anlagenCtrl = new KwkgAnlagenCtrl();
        private readonly GesetzKatalog _katalog = new GesetzKatalog();

        private readonly WirtschaftlichkeitParameter _parameter;
        private readonly WirtschaftlichkeitCtrl.ErzeugerFlags _erzeuger;
        private readonly List<KwkgAnlagenAngabe> _anlagen;

        /// <summary>
        /// Die Ergebnisse des zuletzt gerechneten Laufs, vom Wirt durchgereicht.
        ///
        /// <para>Zwei ihrer Bestandteile sind <b>nicht persistiert</b> und deshalb aus
        /// der Datenbank nicht zu holen: die Kohärenzhinweise (B2-O4) und die
        /// KWKG-Modulnachweise mit der Mengenkette (E7/B3b). Wer den Dialog aus der
        /// Wirtschaftlichkeitsseite öffnet, hat sie bereits im Speicher — sie werden
        /// gereicht statt nachgerechnet. Leere Liste = noch nicht gerechnet; dann
        /// fallen Vorschau und Steuerherkunft auf den gebuchten Stand zurück
        /// (<see cref="WirtschaftlichkeitCtrl.LadeErgebnisse"/>), Mengenkette und
        /// Kohärenzzeilen bleiben bis auf die Doppelpflege-Prüfung leer.</para>
        /// </summary>
        private readonly List<WirtschaftlichkeitErgebnis> _ausLauf =
            new List<WirtschaftlichkeitErgebnis>();

        private int _aktuell = -1;
        private bool _stumm;      // true, solange der Dialog die Felder selbst füllt
        private bool _aufbau;     // true, solange InitializeComponent läuft
        private bool _geaendert;  // true, wenn seit dem letzten Speichern etwas geändert wurde

        /// <summary>true, wenn mindestens einmal gespeichert wurde (der Wirt rechnet dann neu).</summary>
        public bool Gespeichert { get; private set; }

        // ------------------------------------------------------------ Steuerelemente

        private Panel _pnlKopf, _pnlInhalt, _pnlFuss;
        private Label _lblVorschau;

        // Gruppe 1 — Anlagen
        private ListView _lvAnlagen;
        private Label _lblAnlagenWarn;
        private DateTimePicker _dtStichtagA, _dtIbnA;
        private ComboBox _cbArtA, _cbFallA, _cbEnergiesteuerA, _cbAufteilungA;
        private NumericUpDown _numSatzEinspA, _numSatzEigenA, _numKontingentA, _numDeckelA;
        private NumericUpDown _numHilfsAnteilA;
        private Label _lblHilfsAnteilA, _lblHilfsBasisA;

        // Gruppe 2 — KWK-Zuschlag (Projekt)
        private NumericUpDown _numBonusEigen, _numBonusEinsp, _numDeckelP, _numKontingentP,
                              _numAbschlagNeg, _numKostenanteil;
        private ComboBox _cbTatbestandP, _cbAnlagenartP;
        private CheckBox _chkPauschal;
        private DateTimePicker _dtStichtagP, _dtIbnP;
        private Label _lblHerleitung;
        private Button _btnVorschlag;

        // Gruppe 3 — Energiesteuer (Projekt)
        private ComboBox _cbEnergiesteuerP, _cbAufteilungP;
        private NumericUpDown _numNutzungsgrad;
        private Label _lblSteuerHerkunft;

        // Gruppe 4 — Stromsteuer (Projekt)
        private ComboBox _cbUnternehmensart, _cbBefreiungModus;
        private CheckBox _chkRaeumlich, _chkHocheffizienz;

        // Kohärenzprüfung (gemeinsamer Block, siehe KohaerenzZeigen)
        private Label _lblKohaerenz;

        // Gruppe 5 — Hilfsstrom
        private Label _lblMengenkette, _lblDoppelpflege, _lblHilfsHinweis;

        private Button _btnSchliessen;
        private SpeichernLeiste _speichern;

        // =====================================================================
        // Aufbau
        // =====================================================================

        public Form_BhkwWirtschaftlichkeit(int idStamm)
            : this(idStamm, null)
        {
        }

        /// <param name="idStamm">Stammprojekt der Vergleichsgruppe.</param>
        /// <param name="ergebnisseAusLauf">Die Ergebnisse des zuletzt gerechneten Laufs
        /// (<c>UcWirtschaftlichkeit</c> reicht sie durch — siehe <see cref="_ausLauf"/>).
        /// <c>null</c> ist zulässig.</param>
        public Form_BhkwWirtschaftlichkeit(int idStamm,
                                           List<WirtschaftlichkeitErgebnis> ergebnisseAusLauf)
        {
            _idStamm = idStamm;
            _parameter = _ctrl.LadeParameter(idStamm);
            _erzeuger = _ctrl.ErzeugerDerGruppe(idStamm);

            var pc = new ProjektCtrl();
            try { pc.ReadSingle(idStamm); } catch { }
            _stammName = pc.rows > 0 ? pc.m_szProjektname : "";
            _anlagen = _anlagenCtrl.LadeGruppe(idStamm, _stammName);

            if (ergebnisseAusLauf != null)
                foreach (WirtschaftlichkeitErgebnis e in ergebnisseAusLauf)
                    if (e != null && string.Equals(e.Szenario, WirtschaftlichkeitSzenario.ERWARTET,
                                                   StringComparison.Ordinal))
                        _ausLauf.Add(e);

            InitializeComponent();
            InfoKnopf.Anbringen(this, abstandRechts: 12, ziel: _pnlKopf);
            FensterEinpassung.Einhaengen(this);

            // Nicht serialisierbarer Nachlauf (Muster Form_KwkgModule): Listeninhalt,
            // Erstauswahl und alle abgeleiteten Anzeigen.
            ListeFuellen();
            ErsteZeileWaehlen();
            KohaerenzZeigen();
            SteuerherkunftZeigen();
            VorschauZeigen();
            SpeichernZustand();
        }

        private void InitializeComponent()
        {
            _aufbau = true;
            this.SuspendLayout();
            // AutoScaleMode.None wie bei den Nachbardialogen (Form_KwkgModule,
            // UcWirtschaftlichkeit): Die Anwendung läuft DpiUnaware, und None hält
            // dieses Verhalten fest, statt eine Skalierung erstmals scharf zu schalten.
            // Es ist zugleich der Schutz gegen die AutoScroll-Verdeckung, die aus dem
            // verzögerten Font-Skalieren entsteht.
            this.AutoScaleMode = AutoScaleMode.None;
            this.Font = new Font("Segoe UI", 9f);

            BaueKopf();
            BaueFuss();

            _pnlInhalt = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };
            this.Controls.Add(_pnlInhalt);
            // Das Fill-Control braucht den KLEINSTEN Z-Index: WinForms dockt vom
            // höchsten Index abwärts, sonst nähme Fill zuerst die ganze Fläche und
            // Kopf- und Fußband lägen darüber.
            this.Controls.SetChildIndex(_pnlInhalt, 0);

            int y = 12;
            BaueAnlagen(ref y);
            BaueAnlagenfelder(ref y);

            int yLinks = y, yRechts = y;
            BaueZuschlag(ref yLinks);
            BaueEnergiesteuer(ref yRechts);
            BaueStromsteuer(ref yRechts);
            BaueKohaerenz(ref yRechts);
            y = Math.Max(yLinks, yRechts);

            BaueHilfsstrom(ref y);
            BaueVorschau(ref y);

            // Höhe auf den Arbeitsbereich deckeln, damit AutoScroll wirklich greift
            // (Muster Form_WirtschaftlichkeitParameter).
            int inhalt = y + 12 + _pnlKopf.Height + _pnlFuss.Height;
            int maxHoehe = Screen.PrimaryScreen.WorkingArea.Height - 90;
            this.ClientSize = new Size(FENSTER_BREITE, Math.Min(inhalt, Math.Max(420, maxHoehe)));
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.CancelButton = _btnSchliessen;
            this.Name = "Form_BhkwWirtschaftlichkeit";
            this.Text = T("BHW_TITEL", "BHKW-Wirtschaftlichkeit");
            this.ResumeLayout(false);
            _aufbau = false;
        }

        /// <summary>Kopfband (§ 5): Dock.Top, 48 hoch, <c>#0F1F3D</c>, Titel weiß 12 pt fett.</summary>
        private void BaueKopf()
        {
            _pnlKopf = new Panel { Dock = DockStyle.Top, Height = 48, BackColor = FARBE_KOPF };
            var lblTitel = new Label
            {
                Location = new Point(RAND, 12),
                Size = new Size(700, 24),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                Text = T("BHW_TITEL", "BHKW-Wirtschaftlichkeit") +
                       (string.IsNullOrEmpty(_stammName) ? "" : " — " + _stammName)
            };
            _pnlKopf.Controls.Add(lblTitel);
            this.Controls.Add(_pnlKopf);
        }

        /// <summary>Fußleiste: nicht schließender Speichern-Knopf mit Statuszeile
        /// (<see cref="SpeichernLeiste"/>, B2-Muster) und „Schließen“.</summary>
        private void BaueFuss()
        {
            _pnlFuss = new Panel { Dock = DockStyle.Bottom, Height = 46 };
            _btnSchliessen = new Button
            {
                Size = new Size(110, 30),
                Location = new Point(FENSTER_BREITE - RAND - 110, 8),
                UseVisualStyleBackColor = true,
                Text = T("BHW_BTN_SCHLIESSEN", "Schließen"),
                DialogResult = DialogResult.Cancel,
                TabIndex = 90
            };
            _pnlFuss.Controls.Add(_btnSchliessen);
            this.Controls.Add(_pnlFuss);

            // Der Speichern-Knopf sitzt links neben „Schließen“; die Statuszeile nimmt
            // die freie Fläche davor.
            _speichern = new SpeichernLeiste(
                _pnlFuss, _btnSchliessen,
                new Rectangle(RAND, 8,
                              FENSTER_BREITE - 2 * RAND - 2 * 110 - 2 * SpeichernLeiste.ABSTAND, 30),
                Speichern_Klick);
        }

        // --------------------------------------------------------- Gruppe 1: Anlagen

        private void BaueAnlagen(ref int y)
        {
            GroupBox g = Gruppe(T("BHW_G1", "Anlagen"), RAND, y, INHALT_BREITE, 178);

            _lvAnlagen = new ListView
            {
                Location = new Point(10, 22),
                Size = new Size(INHALT_BREITE - 20, 108),
                View = View.Details,
                FullRowSelect = true,
                MultiSelect = false,
                HideSelection = false,
                GridLines = true,
                Font = new Font("Segoe UI", 9f)
            };
            _lvAnlagen.Columns.Add(T("BHW_SP_PROJEKT", "Projekt"), 150);
            _lvAnlagen.Columns.Add(T("BHW_SP_ANLAGE", "Anlage"), 185);
            _lvAnlagen.Columns.Add(T("BHW_SP_PEL", "P_el [kW]"), 75, HorizontalAlignment.Right);
            _lvAnlagen.Columns.Add(T("BHW_SP_BRENNSTOFF", "Brennstoff"), 130);
            _lvAnlagen.Columns.Add(T("BHW_SP_STICHTAG", "Stichtag"), 90);
            _lvAnlagen.Columns.Add(T("BHW_SP_IBN", "Inbetriebnahme"), 105);
            _lvAnlagen.Columns.Add(T("BHW_SP_ANLAGENART", "Anlagenart"), 110);
            _lvAnlagen.SelectedIndexChanged += Liste_Wechsel;
            g.Controls.Add(_lvAnlagen);

            _lblAnlagenWarn = new Label
            {
                Location = new Point(10, 134),
                Size = new Size(INHALT_BREITE - 20, 38),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Firebrick,
                Text = ""
            };
            g.Controls.Add(_lblAnlagenWarn);

            y += g.Height + 10;
        }

        /// <summary>Die elf Angaben der gewählten Anlage — acht aus E6 (Schritt 22) und
        /// drei aus B3a (Schritt 61). Zwei Halbspalten, damit die Gruppe nicht in die
        /// Höhe läuft.</summary>
        private void BaueAnlagenfelder(ref int y)
        {
            GroupBox g = Gruppe(
                T("BHW_G1B", "Angaben der gewählten Anlage — leer bzw. 0 = Projektvorgabe"),
                RAND, y, INHALT_BREITE, 220);

            int yl = 24, yr = 24;
            const int XL = 10, XR = 446;

            _dtStichtagA = DatumZeile(g, T("BHW_A_STICHTAG", "Stichtag (Bestellung/Genehmigung):"),
                                      XL, ref yl);
            _dtIbnA = DatumZeile(g, T("BHW_A_IBN", "Inbetriebnahme:"), XL, ref yl);
            _cbArtA = AuswahlZeile(g, T("BHW_A_ANLAGENART", "Anlagenart:"), XL, ref yl,
                                   AnlagenartWahlen(false));
            _cbFallA = AuswahlZeile(g, T("BHW_A_EIGENFALL", "Eigenstrom nach § 6 Abs. 3:"),
                                    XL, ref yl, EigenfallWahlen(false));
            _numSatzEinspA = Zahlzeile(g, T("BHW_A_SATZ_EINSP",
                                            "Satz Einspeisung [ct/kWh] (0 = Projektsatz):"),
                                       XL, ref yl, 0m, 30m, 2, 0.1m);
            _numSatzEigenA = Zahlzeile(g, T("BHW_A_SATZ_EIGEN",
                                            "Satz Eigenstrom [ct/kWh] (0 = Projektsatz):"),
                                       XL, ref yl, 0m, 30m, 2, 0.1m);

            _numKontingentA = Zahlzeile(g, T("BHW_A_KONTINGENT",
                                             "Vbh-Kontingent [h] (0 = Projektwert):"),
                                        XR, ref yr, 0m, 200000m, 0, 1000m);
            _numDeckelA = Zahlzeile(g, T("BHW_A_DECKEL", "Vbh-Jahresdeckel [h/a] (0 = Staffel):"),
                                    XR, ref yr, 0m, 8760m, 0, 100m);
            _cbEnergiesteuerA = AuswahlZeile(g,
                T("BHW_A_ENERGIESTEUER", "Energiesteuerentlastung (Anlage):"), XR, ref yr,
                EnergiesteuerWahlen(true));
            _cbAufteilungA = AuswahlZeile(g,
                T("BHW_A_AUFTEILUNG", "Brennstoff auf Strom/Wärme (Anlage):"), XR, ref yr,
                AufteilungWahlen(true));

            // K2/K6: Die Bemessungsbasis steht ausdrücklich in der Beschriftung —
            // gerechnet wird am ENDENERGIEbedarf (Brennstoff) der Anlage, nicht an den
            // Kosten; die Sätze der beiden Wege sind nicht austauschbar. Das Feld ist
            // nur für BHKW-Anlagen gedacht (siehe HilfsenergieSichtbarkeit).
            int yMerk = yr;
            _numHilfsAnteilA = Zahlzeile(g,
                T("BHW_A_HILFSANTEIL", "Hilfsenergieanteil [% des Endenergiebedarfs] (0 = keine):"),
                XR, ref yr, 0m, 100m, 1, 0.5m);
            _lblHilfsAnteilA = BeschriftungBei(g, XR, yMerk);
            _lblHilfsBasisA = new Label
            {
                Location = new Point(XR, yr + 2),
                Size = new Size(420, 34),
                Font = new Font("Segoe UI", 9f),
                ForeColor = FARBE_LEISE,
                Text = T("BHW_A_HILFS_BASIS",
                         "Vorschlag BHKW 2–4 %. Bemessen wird am Endenergiebedarf " +
                         "(Brennstoff) dieser Anlage — nicht an den Kosten.")
            };
            g.Controls.Add(_lblHilfsBasisA);

            y += g.Height + 10;
        }

        // ------------------------------------------------- Gruppe 2: KWK-Zuschlag

        private void BaueZuschlag(ref int y)
        {
            GroupBox g = Gruppe(T("BHW_G2", "KWK-Zuschlag (Projektvorgabe)"),
                                RAND, y, GRUPPE_BREITE, 466);
            int yy = 24;
            const int X = 10;

            _numBonusEigen = Zahlzeile(g, T("BHW_P_BONUS_EIGEN",
                                            "Bonus Eigenstrom [ct/kWh] (0 = aus):"),
                                       X, ref yy, 0m, 30m, 2, 0.1m);
            _numBonusEigen.Value = Geklemmt(_numBonusEigen, _parameter.KwkgBonus);
            _numBonusEinsp = Zahlzeile(g, T("BHW_P_BONUS_EINSP", "Bonus Einspeisung [ct/kWh]:"),
                                       X, ref yy, 0m, 30m, 2, 0.1m);
            _numBonusEinsp.Value = Geklemmt(_numBonusEinsp, _parameter.KwkgBonusEinspeisung);
            _numDeckelP = Zahlzeile(g, T("BHW_P_DECKEL", "Vbh-Deckel-Override [h/a]:"),
                                    X, ref yy, 0m, 8760m, 0, 100m);
            _numDeckelP.Value = Geklemmt(_numDeckelP, _parameter.KwkgVbhJahresdeckel);
            _numKontingentP = Zahlzeile(g, T("BHW_P_KONTINGENT",
                                             "Vbh-Kontingent gesamt [h] (0 = automatisch):"),
                                        X, ref yy, 0m, 200000m, 0, 1000m);
            _numKontingentP.Value = Geklemmt(_numKontingentP, _parameter.KwkgVbhKontingent);
            _numAbschlagNeg = Zahlzeile(g, T("BHW_P_ABSCHLAG", "Abschlag Negativstunden [%]:"),
                                        X, ref yy, 0m, 50m, 1, 0.5m);
            _numAbschlagNeg.Value = Geklemmt(_numAbschlagNeg, _parameter.KwkgAbschlagNegativ);

            _cbTatbestandP = AuswahlZeile(g, T("BHW_P_TATBESTAND",
                                               "Eigenstrom-Tatbestand (§ 6 Abs. 3):"),
                                          X, ref yy, EigenfallWahlen(true));
            Waehle(_cbTatbestandP, _parameter.KwkgTatbestand);
            _cbAnlagenartP = AuswahlZeile(g, T("BHW_P_ANLAGENART", "Anlagenart (§ 8):"),
                                          X, ref yy, AnlagenartWahlen(true));
            Waehle(_cbAnlagenartP, _parameter.KwkgAnlagenart);
            _numKostenanteil = Zahlzeile(g, T("BHW_P_KOSTENANTEIL",
                                              "Anteil Neuherstellungskosten [%]:"),
                                         X, ref yy, 0m, 100m, 1, 5m);
            _numKostenanteil.Value = Geklemmt(_numKostenanteil, _parameter.KwkgKostenanteil);

            _chkPauschal = Schalterzeile(g, T("BHW_P_PAUSCHAL",
                                              "Pauschale § 9 KWKG (nur bis 2 kWel, einmalig)"),
                                         X, ref yy, _parameter.KwkgPauschalmodus);

            _dtStichtagP = DatumZeile(g, T("BHW_P_STICHTAG", "Stichtag, Vorgabe je Anlage:"),
                                      X, ref yy);
            Datum(_dtStichtagP, _parameter.KwkgStichtag);
            _dtIbnP = DatumZeile(g, T("BHW_P_IBN", "Inbetriebnahme, Vorgabe je Anlage:"),
                                 X, ref yy);
            Datum(_dtIbnP, _parameter.KwkgInbetriebnahme);

            // Herleitungslabel je Anlage — Bestand KwkgSatzRechner.Vorschlag, bis B5
            // nur im Modulformular sichtbar (Konzept § 6.1, Gruppe 2).
            _lblHerleitung = new Label
            {
                Location = new Point(X, yy + 4),
                Size = new Size(GRUPPE_BREITE - 2 * X, 62),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.DimGray,
                Text = ""
            };
            g.Controls.Add(_lblHerleitung);
            yy += 68;

            _btnVorschlag = new Button
            {
                Location = new Point(X, yy),
                Size = new Size(240, 30),
                UseVisualStyleBackColor = true,
                Text = T("BHW_BTN_VORSCHLAG", "Vorschlag in die Satzfelder übernehmen")
            };
            _btnVorschlag.Click += Vorschlag_Klick;
            g.Controls.Add(_btnVorschlag);

            y += g.Height + 10;
        }

        // -------------------------------------------------- Gruppe 3: Energiesteuer

        private void BaueEnergiesteuer(ref int y)
        {
            GroupBox g = Gruppe(T("BHW_G3", "Energiesteuer (Projektvorgabe)"),
                                SPALTE_RECHTS, y, GRUPPE_BREITE_R, 168);
            int yy = 24;
            const int X = 10;

            _cbEnergiesteuerP = AuswahlZeile(g, T("BHW_E_WAHL", "Energiesteuerentlastung:"),
                                             X, ref yy, EnergiesteuerWahlen(false));
            Waehle(_cbEnergiesteuerP, _parameter.EnergiesteuerWahl);
            _cbAufteilungP = AuswahlZeile(g, T("BHW_E_AUFTEILUNG", "Brennstoff auf Strom/Wärme:"),
                                          X, ref yy, AufteilungWahlen(false));
            Waehle(_cbAufteilungP, _parameter.AufteilungMethode);
            // K5: Der Jahresnutzungsgrad bleibt eine PROJEKTgröße (B3a-Stand) — ein
            // anlagenscharfer Wert wäre eine eigene Datenfrage.
            _numNutzungsgrad = Zahlzeile(g, T("BHW_E_NUTZUNGSGRAD",
                                              "Jahresnutzungsgrad [%] (0 = nicht erfasst):"),
                                         X, ref yy, 0m, 100m, 1, 1m);
            _numNutzungsgrad.Value = Geklemmt(_numNutzungsgrad, _parameter.Jahresnutzungsgrad ?? 0);

            // Herleitung = die im LAUF ausgewiesene Satzherkunft (Fundstelle, Wert,
            // Einheit, Gültigkeitsjahr, Status). Kein zweiter Rechenweg.
            _lblSteuerHerkunft = new Label
            {
                Location = new Point(X, yy + 2),
                Size = new Size(GRUPPE_BREITE_R - 2 * X, 44),
                Font = new Font("Segoe UI", 9f),
                ForeColor = FARBE_LEISE,
                Text = ""
            };
            g.Controls.Add(_lblSteuerHerkunft);

            y += g.Height + 10;
        }

        // --------------------------------------------------- Gruppe 4: Stromsteuer

        private void BaueStromsteuer(ref int y)
        {
            GroupBox g = Gruppe(T("BHW_G4", "Stromsteuer (Projektvorgabe)"),
                                SPALTE_RECHTS, y, GRUPPE_BREITE_R, 208);
            int yy = 24;
            const int X = 10;

            // BW4: Die Unternehmensart ist das führende Feld der Stromsteuerseite.
            _cbUnternehmensart = AuswahlZeile(g, T("BHW_S_UNTERNEHMENSART", "Unternehmensart:"),
                                              X, ref yy, UnternehmensartWahlen());
            Waehle(_cbUnternehmensart, _parameter.Unternehmensart);
            _chkRaeumlich = Schalterzeile(g, T("BHW_S_RAEUMLICH",
                                               "Räumlicher Zusammenhang (4,5 km) gegeben"),
                                          X, ref yy, _parameter.RaeumlicherZusammenhang);
            _chkHocheffizienz = Schalterzeile(g, T("BHW_S_HOCHEFFIZIENZ",
                                                   "Hocheffizienz nachgewiesen"),
                                              X, ref yy, _parameter.HocheffizienzNachweis);

            // K3 = a (Anwenderentscheid 03.09.2026): Das Modusfeld des § 9 Abs. 1 Nr. 3
            // (BW3) wird GEZEIGT, aber ausgegraut — die Persistenzspalte
            // Stromst_Befreiung_Modus kommt erst mit M-3 (Etappe B6). B5 legt dafür
            // ausdrücklich KEINEN Schemaschritt an, und der Wert wird nirgends
            // gespeichert; die Vorgabe „Ausweis“ (BF1) ist der heutige Rechenstand.
            _cbBefreiungModus = AuswahlZeile(g, T("BHW_S_MODUS", "Modus § 9 Abs. 1 Nr. 3:"),
                                             X, ref yy, BefreiungsmodusWahlen());
            _cbBefreiungModus.SelectedIndex = 0;        // AUSWEIS
            _cbBefreiungModus.Enabled = false;
            var lblB6 = new Label
            {
                Location = new Point(X, yy),
                Size = new Size(GRUPPE_BREITE_R - 2 * X, 18),
                Font = new Font("Segoe UI", 9f),
                ForeColor = FARBE_LEISE,
                Text = T("BHW_S_MODUS_B6",
                         "ab B6 — bis dahin gilt fest „Ausweis“ (nicht im Kapitalwert).")
            };
            g.Controls.Add(lblB6);
            yy += 22;

            var btnStrom = new Button
            {
                Location = new Point(X, yy),
                Size = new Size(195, 30),
                UseVisualStyleBackColor = true,
                Text = T("BHW_BTN_STROMBEZUG", "Strombezug…")
            };
            btnStrom.Click += delegate { TarifOeffnen(TarifSicht.Strombezug); };
            g.Controls.Add(btnStrom);

            // Kein Funktionsverlust durch K8 = c: Der Knopf der Wirtschaftlichkeitsseite,
            // der bis B5 die BHKW-Sicht der Tarifstruktur öffnete, führt jetzt hierher —
            // die Sicht selbst bleibt über diesen Sprung erreichbar.
            var btnBhkwTarif = new Button
            {
                Location = new Point(X + 203, yy),
                Size = new Size(195, 30),
                UseVisualStyleBackColor = true,
                Text = T("BHW_BTN_BHKW_TARIF", "BHKW-Tarif…")
            };
            btnBhkwTarif.Click += delegate { TarifOeffnen(TarifSicht.Bhkw); };
            g.Controls.Add(btnBhkwTarif);

            y += g.Height + 10;
        }

        /// <summary>
        /// Die Kohärenzzeilen des letzten Laufs — <b>ein</b> Block für beide Steuerseiten.
        ///
        /// <para><b>Warum nicht je Gruppe getrennt</b> (Abweichung von der Feldkarte,
        /// begründet): <see cref="KohaerenzHinweis"/> trägt Schwere, Text und Betrag,
        /// aber kein Artmerkmal. Eine Aufteilung auf „Energiesteuer“ und „Stromsteuer“
        /// ginge nur über den Anzeigetext — also sprachabhängiges Textraten — oder über
        /// eine Änderung an <c>KohaerenzPruefung</c>, die zu B5 nicht gehört. Der
        /// gemeinsame Block zeigt dieselben Zeilen wie der Reiter, in derselben
        /// Reihenfolge und aus derselben Quelle.</para>
        /// </summary>
        private void BaueKohaerenz(ref int y)
        {
            GroupBox g = Gruppe(T("BHW_G_KOHAERENZ", "Kohärenzprüfung (Energie- und Stromsteuer)"),
                                SPALTE_RECHTS, y, GRUPPE_BREITE_R, 116);
            _lblKohaerenz = new Label
            {
                Location = new Point(10, 22),
                Size = new Size(GRUPPE_BREITE_R - 20, 86),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Firebrick,
                Text = ""
            };
            g.Controls.Add(_lblKohaerenz);
            y += g.Height + 10;
        }

        // ----------------------------------------------------- Gruppe 5: Hilfsstrom

        private void BaueHilfsstrom(ref int y)
        {
            GroupBox g = Gruppe(T("BHW_G5", "Hilfsstrom"), RAND, y, INHALT_BREITE, 176);
            const int X = 10;

            // K1: Es gibt bewusst KEIN Feld „Deckung je Modul“ — die Spalte ist mit
            // § 4.5 des Konzepts gestrichen (BW6: Die Befreiung nach § 9 Abs. 1 Nr. 3
            // ist bilanziell und folgt den Anlagenbedingungen).
            var lblBasis = new Label
            {
                Location = new Point(X, 24),
                Size = new Size(INHALT_BREITE - 2 * X, 32),
                Font = new Font("Segoe UI", 9f),
                ForeColor = FARBE_LEISE,
                Text = T("BHW_H_BASIS",
                         "Der Anteil wird je Anlage oben gepflegt und am ENDENERGIEBEDARF " +
                         "(Brennstoff) der Anlage bemessen — nicht an den Kosten. Die Menge " +
                         "mindert die zuschlagsfähige Nettostromerzeugung.")
            };
            g.Controls.Add(lblBasis);

            _lblMengenkette = new Label
            {
                Location = new Point(X, 60),
                Size = new Size(INHALT_BREITE - 2 * X, 40),
                Font = new Font("Segoe UI", 9f),
                Text = ""
            };
            g.Controls.Add(_lblMengenkette);

            _lblDoppelpflege = new Label
            {
                Location = new Point(X, 102),
                Size = new Size(INHALT_BREITE - 2 * X, 44),
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.Firebrick,
                Text = ""
            };
            g.Controls.Add(_lblDoppelpflege);

            // K6: Der Anteil gilt formal für jede Anlagenart, gelesen wird er in B3 aber
            // nur für BHKW und Kessel. Gepflegt wird er hier allein am BHKW; für Kessel
            // steht hier der Verweis, die Wärmepumpe kommt später.
            _lblHilfsHinweis = new Label
            {
                Location = new Point(X, 148),
                Size = new Size(INHALT_BREITE - 2 * X, 20),
                Font = new Font("Segoe UI", 9f),
                ForeColor = FARBE_LEISE,
                Text = T("BHW_H_KESSEL",
                         "Heizkessel der Gruppe: Der Hilfsenergieanteil wird für Kessel " +
                         "mitgerechnet, aber nicht hier gepflegt.")
            };
            g.Controls.Add(_lblHilfsHinweis);

            y += g.Height + 10;
        }

        // ------------------------------------------------------ Gruppe 6: Vorschau

        private void BaueVorschau(ref int y)
        {
            var pnlKopf = new Panel
            {
                Location = new Point(RAND, y),
                Size = new Size(INHALT_BREITE, 28),
                BackColor = FARBE_VORSCHAU
            };
            var lblTitel = new Label
            {
                Location = new Point(10, 5),
                Size = new Size(500, 18),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.75f, FontStyle.Bold),
                Text = T("BHW_G6", "Vorschau — zuletzt gebuchter Lauf")
            };
            pnlKopf.Controls.Add(lblTitel);
            _pnlInhalt.Controls.Add(pnlKopf);
            y += 30;

            _lblVorschau = new Label
            {
                Location = new Point(RAND + 10, y),
                Size = new Size(INHALT_BREITE - 20, 110),
                Font = new Font("Segoe UI", 9f),
                Text = "—"
            };
            _pnlInhalt.Controls.Add(_lblVorschau);
            y += 114;
        }

        // =====================================================================
        // Füllen und Anzeigen
        // =====================================================================

        private void ListeFuellen()
        {
            _lvAnlagen.BeginUpdate();
            try
            {
                foreach (KwkgAnlagenAngabe a in _anlagen)
                {
                    var it = new ListViewItem(a.Projektname);
                    it.SubItems.Add(a.Bezeichner);
                    it.SubItems.Add(a.PelKW.ToString("N0", BerichtTexte.Kultur));
                    it.SubItems.Add(a.Brennstoffname);
                    it.SubItems.Add(Kurzdatum(a.Stichtag));
                    it.SubItems.Add(Kurzdatum(a.Inbetriebnahme));
                    it.SubItems.Add(Anlagenarttext(a.Anlagenart));
                    _lvAnlagen.Items.Add(it);
                }
            }
            finally { _lvAnlagen.EndUpdate(); }
            AnlagenwarnungZeigen();
        }

        private void ErsteZeileWaehlen()
        {
            if (_lvAnlagen.Items.Count == 0)
            { FelderAktiv(false); HilfsenergieSichtbarkeit(); return; }

            _lvAnlagen.Items[0].Selected = true;
            // Der Zustand wird ausdrücklich hergestellt statt erwartet: Solange die
            // Liste kein Fensterhandle hat, feuert SelectedIndexChanged nicht — die
            // Felder blieben dann leer und aktiv, ohne dass eine Zeile gewählt wäre.
            // Der Aufruf ist idempotent; kommt das Ereignis später doch, läuft
            // dieselbe Füllung ein zweites Mal.
            if (_aktuell < 0) Liste_Wechsel(null, EventArgs.Empty);
        }

        private void FelderAktiv(bool an)
        {
            _dtStichtagA.Enabled = an; _dtIbnA.Enabled = an;
            _cbArtA.Enabled = an; _cbFallA.Enabled = an;
            _numSatzEinspA.Enabled = an; _numSatzEigenA.Enabled = an;
            _numKontingentA.Enabled = an; _numDeckelA.Enabled = an;
            _cbEnergiesteuerA.Enabled = an; _cbAufteilungA.Enabled = an;
            _numHilfsAnteilA.Enabled = an;
            _btnVorschlag.Enabled = an;
        }

        private void Liste_Wechsel(object sender, EventArgs e)
        {
            UebernimmFelder();                 // die zuvor gewählte Zeile sichern
            _aktuell = GewaehlteZeile();
            if (_aktuell < 0 || _aktuell >= _anlagen.Count)
            { FelderAktiv(false); HilfsenergieSichtbarkeit(); return; }

            KwkgAnlagenAngabe a = _anlagen[_aktuell];
            _stumm = true;
            try
            {
                FelderAktiv(true);
                Datum(_dtStichtagA, a.Stichtag);
                Datum(_dtIbnA, a.Inbetriebnahme);
                Waehle(_cbArtA, a.Anlagenart);
                Waehle(_cbFallA, a.Eigenfall);
                _numSatzEinspA.Value = Geklemmt(_numSatzEinspA, a.SatzEinspCt);
                _numSatzEigenA.Value = Geklemmt(_numSatzEigenA, a.SatzEigenCt);
                _numKontingentA.Value = Geklemmt(_numKontingentA, a.VbhKontingent);
                _numDeckelA.Value = Geklemmt(_numDeckelA, a.VbhDeckel);
                Waehle(_cbEnergiesteuerA, a.EnergiesteuerWahl);
                Waehle(_cbAufteilungA, a.AufteilungMethode);
                _numHilfsAnteilA.Value = Geklemmt(_numHilfsAnteilA, a.HilfsenergieAnteil);
            }
            finally { _stumm = false; }
            HilfsenergieSichtbarkeit();
            HerleitungZeigen();
            MengenketteZeigen();
        }

        /// <summary>Der Index der gewählten Anlagenzeile; −1 = keine. Ohne Fensterhandle
        /// führt die Liste ihre <c>SelectedIndices</c> nicht — dann zählt der gemerkte
        /// Zustand der Einträge selbst.</summary>
        private int GewaehlteZeile()
        {
            if (_lvAnlagen.SelectedIndices.Count > 0) return _lvAnlagen.SelectedIndices[0];
            for (int i = 0; i < _lvAnlagen.Items.Count; i++)
                if (_lvAnlagen.Items[i].Selected) return i;
            return -1;
        }

        /// <summary>
        /// K6 — der Hilfsenergieanteil ist nur bei BHKW-Anlagen pflegbar.
        ///
        /// <para>Die Spalte <c>Hilfsenergie_Anteil</c> gilt formal für jede Komponente
        /// (Konzept § 5.2), gelesen wird sie in B3 aber nur für BHKW und Heizkessel.
        /// Der Kessel bekommt in Gruppe 5 einen Hinweis statt eines Feldes — sein
        /// Hilfsstrom ist reiner Ausweis ohne Strommengenwirkung (B3b) —, die
        /// Wärmepumpe bleibt einer späteren Etappe vorbehalten.</para>
        /// </summary>
        internal static bool AnteilPflegbar(int idType)
        {
            return idType == WizardItemClass.BHKW_TYP;
        }

        /// <summary>Zeigt den Hinweis für Kessel bzw. blendet ihn aus; die Wärmepumpe
        /// bekommt weder Feld noch Hinweis.</summary>
        internal static bool AnteilHinweis(int idType)
        {
            return idType == WizardItemClass.KESSEL_TYP;
        }

        private void HilfsenergieSichtbarkeit()
        {
            // Der Dialog führt ausschließlich BHKW-Anlagenzeilen (LadeGruppe filtert auf
            // ID_Type = BHKW_TYP) — das Feld ist deshalb sichtbar, sobald eine Zeile
            // gewählt ist.
            bool sichtbar = _aktuell >= 0 && _aktuell < _anlagen.Count &&
                            AnteilPflegbar(WizardItemClass.BHKW_TYP);
            _numHilfsAnteilA.Visible = sichtbar;
            if (_lblHilfsAnteilA != null) _lblHilfsAnteilA.Visible = sichtbar;
            _lblHilfsBasisA.Visible = sichtbar;
            _lblHilfsHinweis.Visible = _erzeuger.Heizkessel && AnteilHinweis(WizardItemClass.KESSEL_TYP);
        }

        /// <summary>Ein Feld hat sich geändert: in die Liste zurückschreiben und die
        /// abhängigen Anzeigen erneuern. Geschrieben wird erst beim Speichern.</summary>
        private void Feld_Wechsel(object sender, EventArgs e)
        {
            if (_aufbau || _stumm) return;
            UebernimmFelder();
            _geaendert = true;
            SpeichernZustand();
            HerleitungZeigen();
            AnlagenwarnungZeigen();
            ListeZeileAuffrischen();
        }

        /// <summary>Die Bildschirmfelder in die Liste zurückschreiben (ohne Datenbank).</summary>
        private void UebernimmFelder()
        {
            if (_aufbau || _stumm || _aktuell < 0 || _aktuell >= _anlagen.Count) return;
            KwkgAnlagenAngabe a = _anlagen[_aktuell];
            a.Stichtag = _dtStichtagA.Checked ? (DateTime?)_dtStichtagA.Value.Date : null;
            a.Inbetriebnahme = _dtIbnA.Checked ? (DateTime?)_dtIbnA.Value.Date : null;
            a.Anlagenart = Gewaehlt(_cbArtA);
            a.Eigenfall = Gewaehlt(_cbFallA);
            // 0 heißt „kein eigener Wert": Ein Zuschlagssatz von null wäre fachlich kein
            // Satz und ein Kontingent von null keine Laufzeit — sonst käme niemand mehr
            // zum Projektwert zurück (E6-Muster).
            a.SatzEinspCt = _numSatzEinspA.Value > 0 ? (double?)_numSatzEinspA.Value : null;
            a.SatzEigenCt = _numSatzEigenA.Value > 0 ? (double?)_numSatzEigenA.Value : null;
            a.VbhKontingent = _numKontingentA.Value > 0 ? (double?)_numKontingentA.Value : null;
            a.VbhDeckel = _numDeckelA.Value > 0 ? (double?)_numDeckelA.Value : null;
            a.EnergiesteuerWahl = Gewaehlt(_cbEnergiesteuerA);
            a.AufteilungMethode = Gewaehlt(_cbAufteilungA);
            // Beim Anteil ist 0 ein GÜLTIGER Wert („keine Hilfsenergie", BF4) — er wird
            // deshalb als 0 geschrieben und nicht auf null abgebildet.
            a.HilfsenergieAnteil = (double)_numHilfsAnteilA.Value;
        }

        private void ListeZeileAuffrischen()
        {
            if (_aktuell < 0 || _aktuell >= _lvAnlagen.Items.Count) return;
            KwkgAnlagenAngabe a = _anlagen[_aktuell];
            ListViewItem it = _lvAnlagen.Items[_aktuell];
            it.SubItems[4].Text = Kurzdatum(a.Stichtag);
            it.SubItems[5].Text = Kurzdatum(a.Inbetriebnahme);
            it.SubItems[6].Text = Anlagenarttext(a.Anlagenart);
        }

        /// <summary>Herleitung des Katalogvorschlags für die gewählte Anlage
        /// (<see cref="KwkgSatzRechner"/> — dieselbe Quelle wie im Modulformular).</summary>
        private void HerleitungZeigen()
        {
            if (_aktuell < 0 || _aktuell >= _anlagen.Count) { _lblHerleitung.Text = ""; return; }
            KwkgSatzVorschlag v = Vorschlag(_anlagen[_aktuell]);
            CultureInfo k = BerichtTexte.Kultur;
            _lblHerleitung.Text =
                string.Format(T("BHW_HERLEITUNG_EINSP", "Einspeisung {0} ct/kWh — {1}"),
                              v.SatzEinspeisungCt.ToString("N2", k), v.HerleitungEinspeisung) +
                Environment.NewLine + Environment.NewLine +
                string.Format(T("BHW_HERLEITUNG_EIGEN", "Eigenstrom {0} ct/kWh — {1}"),
                              v.SatzEigenCt.ToString("N2", k), v.HerleitungEigen);
        }

        /// <summary>Der Vorschlag für eine Anlage — mit dem Inbetriebnahmejahr DIESER
        /// Anlage, ersatzweise dem der Projektvorgabe (Muster Form_KwkgModule).</summary>
        private KwkgSatzVorschlag Vorschlag(KwkgAnlagenAngabe a)
        {
            int jahr = a.Inbetriebnahme.HasValue
                ? a.Inbetriebnahme.Value.Year
                : (_dtIbnP.Checked ? _dtIbnP.Value.Year
                                   : (_parameter.KwkgInbetriebnahme.HasValue
                                      ? _parameter.KwkgInbetriebnahme.Value.Year
                                      : DateTime.Now.Year + 1));
            return KwkgSatzRechner.Vorschlag(a.PelKW, jahr, a.Anlagenart, a.Eigenfall,
                                             _katalog.WertMitHerkunft, BerichtTexte.Kultur);
        }

        private void Vorschlag_Klick(object sender, EventArgs e)
        {
            if (_aktuell < 0 || _aktuell >= _anlagen.Count) return;
            UebernimmFelder();
            KwkgSatzVorschlag v = Vorschlag(_anlagen[_aktuell]);
            _stumm = true;
            try
            {
                _numSatzEinspA.Value = Geklemmt(_numSatzEinspA, v.SatzEinspeisungCt);
                _numSatzEigenA.Value = Geklemmt(_numSatzEigenA, v.SatzEigenCt);
            }
            finally { _stumm = false; }
            UebernimmFelder();
            _geaendert = true;
            SpeichernZustand();
            HerleitungZeigen();
        }

        /// <summary>
        /// Die drei Warnzeilen der Gruppe 1 (Konzept § 6.1). <b>Reine Anzeige</b> — die
        /// Grenzwerte kommen aus demselben Gesetzeskatalog, den auch der Rechenweg liest
        /// (<c>KWKG_AUSSCHREIBUNG_GRENZE_KW</c>, <c>STROMST_GRENZE_BEFREIUNG_9_1_3_KW</c>),
        /// mit demselben Rückfall. Über den Ausschluss entscheidet der Rechenweg, nicht
        /// diese Zeile.
        /// </summary>
        private void AnlagenwarnungZeigen()
        {
            int jahr = _dtIbnP.Checked ? _dtIbnP.Value.Year
                     : (_parameter.KwkgInbetriebnahme.HasValue
                        ? _parameter.KwkgInbetriebnahme.Value.Year : DateTime.Now.Year);
            double grenzeAusschreibung = Katalogwert(DbWerte.GESETZ_KWKG_AUSSCHREIBUNG_GRENZE, jahr,
                                                     WirtschaftlichkeitCtrl.KWKG_MAX_LEISTUNG_KW);
            double grenzeStromsteuer = Katalogwert(DbWerte.GESETZ_STROMST_GRENZE_BEFREIUNG, jahr, 2000);

            var ausschreibung = new List<string>();
            var ueberStromsteuer = new List<string>();
            var heizoel = new List<string>();
            foreach (KwkgAnlagenAngabe a in _anlagen)
            {
                if (a.PelKW > grenzeAusschreibung) ausschreibung.Add(a.Bezeichner);
                if (a.PelKW > grenzeStromsteuer) ueberStromsteuer.Add(a.Bezeichner);
                int ibn = a.Inbetriebnahme.HasValue ? a.Inbetriebnahme.Value.Year : jahr;
                if (a.Heizoel && ibn >= 2025) heizoel.Add(a.Bezeichner);
            }

            var zeilen = new List<string>();
            CultureInfo k = BerichtTexte.Kultur;
            if (ausschreibung.Count > 0)
                zeilen.Add(string.Format(T("BHW_W_AUSSCHREIBUNG",
                    "Ausschreibung nach § 8a KWKG: {0} über {1} kW."),
                    string.Join(", ", ausschreibung.ToArray()),
                    grenzeAusschreibung.ToString("N0", k)));
            if (ueberStromsteuer.Count > 0)
                zeilen.Add(string.Format(T("BHW_W_STROMSTEUER",
                    "Stromsteuerbefreiung § 9 Abs. 1 Nr. 3 entfällt: {0} über {1} kW."),
                    string.Join(", ", ueberStromsteuer.ToArray()),
                    grenzeStromsteuer.ToString("N0", k)));
            if (heizoel.Count > 0)
                zeilen.Add(string.Format(T("BHW_W_HEIZOEL",
                    "Heizöl-Ausschluss ab Inbetriebnahme 2025: {0}."),
                    string.Join(", ", heizoel.ToArray())));
            _lblAnlagenWarn.Text = string.Join(Environment.NewLine, zeilen.ToArray());
        }

        private double Katalogwert(string schluessel, int jahr, double rueckfall)
        {
            try
            {
                double? w = _katalog.Wert(schluessel, jahr);
                if (w.HasValue && w.Value > 0) return w.Value;
            }
            catch { }
            return rueckfall;
        }

        /// <summary>
        /// Die Mengenkette der Gruppe 5 aus dem <b>Bestand</b>: <see cref="KwkgModulNachweis"/>
        /// führt seit B3b Brutto − Hilfsstrom = Netto → Eigen/Einspeisung je Anlage. Ohne
        /// gebuchten Lauf steht hier ein Hinweis — nachgerechnet wird nichts.
        /// </summary>
        private void MengenketteZeigen()
        {
            _lblMengenkette.Text = "";
            if (_aktuell < 0 || _aktuell >= _anlagen.Count) return;
            KwkgModulNachweis n = ModulNachweis(_anlagen[_aktuell]);
            if (n == null)
            {
                _lblMengenkette.Text = T("BHW_H_OHNE_LAUF",
                    "Mengenkette: noch kein gebuchtes Ergebnis — bitte in der " +
                    "Wirtschaftlichkeit „Berechnen“.");
                return;
            }
            CultureInfo k = BerichtTexte.Kultur;
            _lblMengenkette.Text =
                string.Format(T("BHW_H_KETTE1",
                    "Stromerzeugung brutto {0} MWh/a − Hilfsstrom {1} MWh/a = " +
                    "Nettostromerzeugung {2} MWh/a"),
                    n.StromBruttoMWh.ToString("N3", k), n.HilfsstromMWh.ToString("N3", k),
                    n.StromNettoMWh.ToString("N3", k)) +
                Environment.NewLine +
                string.Format(T("BHW_H_KETTE2",
                    "davon Eigenverbrauch {0} MWh/a, Einspeisung {1} MWh/a"),
                    n.EigenMWh.ToString("N3", k), n.EinspeisungMWh.ToString("N3", k));
        }

        /// <summary>Die Modulzeile des zuletzt gebuchten Laufs zu einer Anlage — über den
        /// Bezeichner, so wie die Rechnung selbst zuordnet.</summary>
        private KwkgModulNachweis ModulNachweis(KwkgAnlagenAngabe a)
        {
            try
            {
                foreach (WirtschaftlichkeitErgebnis e in Ergebnisse())
                {
                    if (e.IdProjekt != a.IdProjekt || e.KwkgModule == null) continue;
                    foreach (KwkgModulNachweis n in e.KwkgModule)
                        if (string.Equals(n.Bezeichner, a.Bezeichner, StringComparison.OrdinalIgnoreCase))
                            return n;
                }
            }
            catch { }
            return null;
        }

        private List<WirtschaftlichkeitErgebnis> _ergebnisCache;

        /// <summary>
        /// Die Ergebnisse der Gruppe im Szenario „Erwartet“ — EINMAL beschafft.
        ///
        /// <para><b>Der Lauf hat Vorrang vor der Datenbank.</b> Reicht der Wirt seine
        /// Liste durch, wird sie genommen: Nur sie trägt Mengenkette und Kohärenzzeilen,
        /// die beide nicht persistiert sind. Ohne durchgereichten Lauf bleibt der
        /// gebuchte Stand — er trägt die Vorschauzahlen und die Steuerherkunft.</para>
        /// </summary>
        private List<WirtschaftlichkeitErgebnis> Ergebnisse()
        {
            if (_ergebnisCache != null) return _ergebnisCache;
            if (_ausLauf.Count > 0) { _ergebnisCache = _ausLauf; return _ergebnisCache; }
            _ergebnisCache = new List<WirtschaftlichkeitErgebnis>();
            try
            {
                var ids = new List<int>();
                foreach (KwkgAnlagenAngabe a in _anlagen)
                    if (!ids.Contains(a.IdProjekt)) ids.Add(a.IdProjekt);
                if (!ids.Contains(_idStamm)) ids.Add(_idStamm);
                foreach (WirtschaftlichkeitErgebnis e in _ctrl.LadeErgebnisse(ids))
                    if (string.Equals(e.Szenario, WirtschaftlichkeitSzenario.ERWARTET,
                                      StringComparison.Ordinal))
                        _ergebnisCache.Add(e);
            }
            catch { }
            return _ergebnisCache;
        }

        /// <summary>
        /// Kohärenz- und Warnzeilen aus den <b>bestehenden</b> Prüfquellen: Die
        /// laufunabhängige Doppelpflege-Prüfung ruft der Dialog selbst auf
        /// (<c>KohaerenzPruefung.Pruefe(id, null)</c> — genau der Zweig, der keinen
        /// Steuerlauf braucht); die steuerlichen Zeilen kommen aus dem letzten Lauf und
        /// werden nur einsortiert. Eine Zweitlogik gibt es nicht.
        /// </summary>
        private void KohaerenzZeigen()
        {
            var doppel = new List<KohaerenzHinweis>();
            try { doppel.AddRange(KohaerenzPruefung.Pruefe(_idStamm, null)); }
            catch { }

            var doppelTexte = new List<string>();
            foreach (KohaerenzHinweis h in doppel)
                if (h != null && !string.IsNullOrEmpty(h.Text)) doppelTexte.Add(h.Text);

            var steuer = new List<string>();
            var hilfs = new List<string>();
            foreach (string s in doppelTexte) hilfs.Add(Marke(KohaerenzSchwere.WARNUNG) + s);

            var ausLauf = new List<KohaerenzHinweis>();
            foreach (WirtschaftlichkeitErgebnis e in Ergebnisse())
                if (e.KohaerenzHinweise != null) ausLauf.AddRange(e.KohaerenzHinweise);

            foreach (KohaerenzHinweis h in ausLauf)
            {
                if (h == null || string.IsNullOrEmpty(h.Text)) continue;
                string zeile = Marke(h.Schwere) + h.Text;
                // Die Doppelpflege steht in Gruppe 5 — sie hier ein zweites Mal zu
                // zeigen, wäre dieselbe Aussage an zwei Stellen.
                if (doppelTexte.Contains(h.Text))
                { if (!hilfs.Contains(zeile)) hilfs.Add(zeile); continue; }
                if (!steuer.Contains(zeile)) steuer.Add(zeile);
            }

            _lblKohaerenz.Text = steuer.Count > 0
                ? string.Join(Environment.NewLine, steuer.ToArray())
                : T("BHW_K_LEER", "Keine Auffälligkeit im zuletzt gebuchten Lauf.");
            _lblKohaerenz.ForeColor = steuer.Count > 0 ? Color.Firebrick : FARBE_LEISE;
            _lblDoppelpflege.Text = string.Join(Environment.NewLine, hilfs.ToArray());
        }

        private static string Marke(string schwere)
        {
            return string.Equals(schwere, KohaerenzSchwere.WARNUNG, StringComparison.Ordinal)
                 ? "⚠ " : "· ";
        }

        /// <summary>Die im Lauf ausgewiesene Herkunft der Steuersätze (Fundstelle, Wert,
        /// Einheit, Jahr, Status) — die Herleitungszeile der Gruppe 3.</summary>
        private void SteuerherkunftZeigen()
        {
            string herkunft = null;
            foreach (WirtschaftlichkeitErgebnis e in Ergebnisse())
                if (e.IdProjekt == _idStamm && !string.IsNullOrEmpty(e.SteuerHerkunft))
                { herkunft = e.SteuerHerkunft; break; }
            _lblSteuerHerkunft.Text = string.IsNullOrEmpty(herkunft)
                ? T("BHW_E_OHNE_HERKUNFT",
                    "Keine Gutschrift im zuletzt gebuchten Lauf — es wurde kein Satz verwendet.")
                : herkunft;
        }

        /// <summary>
        /// Vorschau (Gruppe 6) — die gebuchten Jahr-1-Werte des EINEN Rechenwegs. Der
        /// Dialog rechnet nichts nach: Ohne Lauf steht hier ein Hinweis statt einer Zahl.
        /// </summary>
        private void VorschauZeigen()
        {
            WirtschaftlichkeitErgebnis e = null;
            foreach (WirtschaftlichkeitErgebnis x in Ergebnisse())
                if (x.IdProjekt == _idStamm) { e = x; break; }
            if (e == null)
            {
                _lblVorschau.Text = T("BHW_V_OHNE_LAUF",
                    "Noch kein gebuchtes Ergebnis — die Vorschau erscheint nach " +
                    "„Berechnen“ in der Wirtschaftlichkeit.");
                return;
            }

            CultureInfo k = BerichtTexte.Kultur;
            var zeilen = new List<string>
            {
                Vorschauzeile(T("BHW_V_ZUSCHLAG", "KWK-Zuschlag p. a."), e.KwkgErloesJahr1, k),
                Vorschauzeile(T("BHW_V_ENERGIESTEUER", "Energiesteuer p. a."), e.EnergiesteuerJahr1, k),
                Vorschauzeile(T("BHW_V_STROMSTEUER", "Stromsteuer p. a."),
                              e.StromsteuerBefreiungJahr1 + e.StromsteuerEntlastungJahr1, k),
                Vorschauzeile(T("BHW_V_EINSPEISUNG", "Einspeiseerlös KWK p. a."),
                              e.EinspeiseerloesKwkJahr, k),
                Vorschauzeile(T("BHW_V_VERMIEDEN", "Vermiedene Stromkosten p. a. (Ausweis)"),
                              e.VermiedenGesamtJahr, k)
            };
            _lblVorschau.Text = string.Join(Environment.NewLine, zeilen.ToArray()) +
                Environment.NewLine +
                string.Format(T("BHW_V_STAND", "Stand: {0} — nach dem Speichern neu berechnen."),
                              e.Zeitstempel.ToString("g", k));
        }

        private static string Vorschauzeile(string titel, double wert, CultureInfo k)
        {
            return titel + ": " + wert.ToString("N0", k) + " €";
        }

        // =====================================================================
        // Speichern
        // =====================================================================

        private void SpeichernZustand()
        {
            if (_speichern != null) _speichern.Zustand(true, _geaendert);
        }

        /// <summary>
        /// Speichert die Anlagenzeilen (elf Spalten, K7) und die Projektvorgaben.
        /// <b>Schließt nicht</b> — SpeichernLeiste-Muster aus B2.
        /// </summary>
        private void Speichern_Klick(object sender, EventArgs e)
        {
            UebernimmFelder();

            int fehler = 0;
            foreach (KwkgAnlagenAngabe a in _anlagen)
                if (!_anlagenCtrl.Speichere(a, true)) fehler++;

            if (!ProjektwerteSpeichern()) fehler++;

            if (fehler > 0)
            {
                MessageBox.Show(
                    string.Format(T("BHW_MSG_FEHLER",
                                    "{0} Angabe(n) konnten nicht gespeichert werden."), fehler),
                    T("BHW_MSG_FEHLER_TITEL", "Fehler"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                _speichern.Fehler();
                return;
            }

            Gespeichert = true;
            _geaendert = false;
            SpeichernZustand();
            _speichern.Gespeichert();
            _ergebnisCache = null;    // der gebuchte Lauf ist überholt — neu einlesen
            SteuerherkunftZeigen();
            VorschauZeigen();
        }

        /// <summary>
        /// Die Projektvorgaben zurückschreiben. <b>Nur die Felder dieses Dialogs</b> —
        /// alles Übrige steht unverändert im geladenen Parametersatz und geht wertgleich
        /// in die Zeile zurück; das ist dieselbe Eigenschaft, mit der der Parameterdialog
        /// seine ausgeblendeten Gruppen unverändert lässt.
        /// </summary>
        private bool ProjektwerteSpeichern()
        {
            _parameter.KwkgBonus = (double)_numBonusEigen.Value;
            _parameter.KwkgBonusEinspeisung = (double)_numBonusEinsp.Value;
            _parameter.KwkgVbhJahresdeckel = (double)_numDeckelP.Value;
            _parameter.KwkgVbhKontingent = (double)_numKontingentP.Value;
            _parameter.KwkgAbschlagNegativ = (double)_numAbschlagNeg.Value;
            // Leer ist hier eine gültige Aussage („nicht angegeben") und darf nicht durch
            // einen Ersatzwert überschrieben werden (K6-Muster des Bestands).
            _parameter.KwkgTatbestand = Gewaehlt(_cbTatbestandP);
            _parameter.KwkgAnlagenart = Gewaehlt(_cbAnlagenartP);
            _parameter.KwkgKostenanteil = (double)_numKostenanteil.Value;
            _parameter.KwkgPauschalmodus = _chkPauschal.Checked;
            _parameter.KwkgStichtag = _dtStichtagP.Checked ? (DateTime?)_dtStichtagP.Value.Date : null;
            _parameter.KwkgInbetriebnahme = _dtIbnP.Checked
                                          ? (DateTime?)_dtIbnP.Value.Date : null;

            _parameter.Unternehmensart = GewaehltMitVorgabe(_cbUnternehmensart,
                                            DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE);
            _parameter.RaeumlicherZusammenhang = _chkRaeumlich.Checked;
            _parameter.HocheffizienzNachweis = _chkHocheffizienz.Checked;
            // 0 % Jahresnutzungsgrad heißt „nicht erfasst": Ein Nutzungsgrad von null ist
            // fachlich kein Wert, und die Begründung soll „nicht erfasst" von „erfasst
            // und zu niedrig" unterscheiden können (E4-Muster).
            _parameter.Jahresnutzungsgrad = _numNutzungsgrad.Value > 0
                                          ? (double?)_numNutzungsgrad.Value : null;
            _parameter.EnergiesteuerWahl = GewaehltMitVorgabe(_cbEnergiesteuerP,
                                            DbWerte.ENERGIESTEUER_WAHL_KEINE);
            _parameter.AufteilungMethode = GewaehltMitVorgabe(_cbAufteilungP,
                                            DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF);
            // K3 = a: Der Modus des § 9 Abs. 1 Nr. 3 wird NICHT gespeichert — es gibt
            // dafür bis B6 (M-3) keine Spalte.

            try { return _ctrl.SpeichereParameter(_parameter); }
            catch { return false; }
        }

        private void TarifOeffnen(TarifSicht sicht)
        {
            using (var dlg = new Form_Tarifstruktur(_idStamm, sicht))
                dlg.ShowDialog(this);
        }

        // =====================================================================
        // Auswahllisten (Steuerwerte aus DbWerte — Drei-Schichten-Regel)
        // =====================================================================

        /// <summary>Ein Eintrag einer Auswahlliste: sprachneutraler Steuerwert für die
        /// Datenbank, Anzeigetext für den Bildschirm.</summary>
        private sealed class Steuerwahl
        {
            public readonly string Wert;
            private readonly string _text;
            public Steuerwahl(string wert, string text) { Wert = wert; _text = text; }
            public override string ToString() { return _text; }
        }

        /// <param name="mitOffen">true = der erste Eintrag heißt „(nicht angegeben)“;
        /// der Steuerwert ist in beiden Fällen LEER — das ist der Zustand jeder
        /// Bestandszeile.</param>
        private Steuerwahl[] AnlagenartWahlen(bool mitOffen)
        {
            return new[]
            {
                new Steuerwahl("", mitOffen
                    ? T("BHW_W_OFFEN", "(nicht angegeben)")
                    : T("BHW_W_ART_LEER", "(nicht erfasst — gilt als Neuanlage)")),
                new Steuerwahl(DbWerte.KWKG_ANLAGENART_NEU,
                               T("BHW_W_ART_NEU", "neue Anlage (§ 8 Abs. 1)")),
                new Steuerwahl(DbWerte.KWKG_ANLAGENART_MODERNISIERT,
                               T("BHW_W_ART_MOD", "modernisiert (§ 8 Abs. 2)")),
                new Steuerwahl(DbWerte.KWKG_ANLAGENART_NACHGERUESTET,
                               T("BHW_W_ART_NACH", "nachgerüstet (§ 8 Abs. 3)"))
            };
        }

        private Steuerwahl[] EigenfallWahlen(bool mitOffen)
        {
            var l = new List<Steuerwahl>();
            if (mitOffen) l.Add(new Steuerwahl("", T("BHW_W_OFFEN", "(nicht angegeben)")));
            l.Add(new Steuerwahl(DbWerte.KWKG_EIGENFALL_KEINER,
                                 T("BHW_W_FALL_KEINER", "kein Tatbestand (kein Eigenstromzuschlag)")));
            l.Add(new Steuerwahl(DbWerte.KWKG_EIGENFALL_NR1,
                                 T("BHW_W_FALL_NR1", "Nr. 1 — Anlage bis 100 kW")));
            l.Add(new Steuerwahl(DbWerte.KWKG_EIGENFALL_NR2,
                                 T("BHW_W_FALL_NR2", "Nr. 2 — Kundenanlage / geschl. Netz")));
            l.Add(new Steuerwahl(DbWerte.KWKG_EIGENFALL_NR3,
                                 T("BHW_W_FALL_NR3", "Nr. 3 — stromkostenintensiv")));
            return l.ToArray();
        }

        /// <param name="jeAnlage">true = mit dem ersten Eintrag „(Projektwert)“; an der
        /// Anlage heißt leer „kein eigener Wert“ (B3a).</param>
        private Steuerwahl[] EnergiesteuerWahlen(bool jeAnlage)
        {
            var l = new List<Steuerwahl>();
            if (jeAnlage) l.Add(new Steuerwahl("", T("BHW_W_PROJEKTWERT", "(Projektwert)")));
            l.Add(new Steuerwahl(DbWerte.ENERGIESTEUER_WAHL_KEINE, T("BHW_W_ES_KEINE", "keine")));
            l.Add(new Steuerwahl(DbWerte.ENERGIESTEUER_WAHL_53,
                                 T("BHW_W_ES_53", "§ 53 EnergieStG (Formular 1131)")));
            l.Add(new Steuerwahl(DbWerte.ENERGIESTEUER_WAHL_53A,
                                 T("BHW_W_ES_53A", "§ 53a Abs. 5 EnergieStG (1135)")));
            l.Add(new Steuerwahl(DbWerte.ENERGIESTEUER_WAHL_54,
                                 T("BHW_W_ES_54", "§ 54 EnergieStG (Formular 1450)")));
            return l.ToArray();
        }

        private Steuerwahl[] AufteilungWahlen(bool jeAnlage)
        {
            var l = new List<Steuerwahl>();
            if (jeAnlage) l.Add(new Steuerwahl("", T("BHW_W_PROJEKTWERT", "(Projektwert)")));
            l.Add(new Steuerwahl(DbWerte.AUFTEILUNG_VOLLER_BRENNSTOFF,
                                 T("BHW_W_AUF_VOLL", "voller BHKW-Brennstoff (§ 53 Abs. 2)")));
            l.Add(new Steuerwahl(DbWerte.AUFTEILUNG_ENERGETISCH,
                                 T("BHW_W_AUF_ENERGETISCH", "energetisch (konservativ)")));
            return l.ToArray();
        }

        private Steuerwahl[] UnternehmensartWahlen()
        {
            return new[]
            {
                new Steuerwahl(DbWerte.UNTERNEHMENSART_KEIN_PROD_GEWERBE,
                               T("BHW_W_UA_KEIN", "kein produzierendes Gewerbe")),
                new Steuerwahl(DbWerte.UNTERNEHMENSART_PROD_GEWERBE,
                               T("BHW_W_UA_PROD", "produzierendes Gewerbe")),
                new Steuerwahl(DbWerte.UNTERNEHMENSART_LAND_FORST,
                               T("BHW_W_UA_LAND", "Land- und Forstwirtschaft"))
            };
        }

        /// <summary>K3 = a: die beiden Modi des § 9 Abs. 1 Nr. 3 — reine ANZEIGE ohne
        /// Persistenz. Die Steuerwerte stehen bewusst nicht in <c>DbWerte</c>: Es gibt
        /// bis B6 keine Spalte, in die sie geschrieben würden, und <c>DbWerte</c> sammelt
        /// ausschließlich Werte, die wirklich in der Datenbank stehen.</summary>
        private Steuerwahl[] BefreiungsmodusWahlen()
        {
            return new[]
            {
                new Steuerwahl("AUSWEIS", T("BHW_W_MODUS_AUSWEIS", "Ausweis (nicht im Kapitalwert)")),
                new Steuerwahl("ERLOES", T("BHW_W_MODUS_ERLOES", "Erlös (im Kapitalwert)"))
            };
        }

        // =====================================================================
        // Layout-Helfer
        // =====================================================================

        private GroupBox Gruppe(string titel, int x, int y, int breite, int hoehe)
        {
            var g = new GroupBox
            {
                Location = new Point(x, y),
                Size = new Size(breite, hoehe),
                Text = titel,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _pnlInhalt.Controls.Add(g);
            return g;
        }

        /// <summary>Die zuvor angelegte Beschriftung einer Zeile — für die Fälle, in
        /// denen der Aufrufer sie später noch braucht (K6-Sichtbarkeit).</summary>
        private static Label BeschriftungBei(Control eltern, int x, int y)
        {
            foreach (Control c in eltern.Controls)
            {
                var l = c as Label;
                if (l != null && l.Left == x && l.Top == y + 3) return l;
            }
            return null;
        }

        private NumericUpDown Zahlzeile(Control eltern, string beschriftung, int x, ref int y,
                                        decimal min, decimal max, int dez, decimal schritt)
        {
            var lbl = new Label
            {
                Location = new Point(x, y + 3),
                Size = new Size(250, 20),
                Font = new Font("Segoe UI", 9f),
                Text = beschriftung
            };
            var num = new NumericUpDown
            {
                Location = new Point(x + 256, y),
                Size = new Size(160, 23),
                Font = new Font("Segoe UI", 9f),
                Minimum = min,
                Maximum = max,
                DecimalPlaces = dez,
                Increment = schritt,
                TextAlign = HorizontalAlignment.Right
            };
            num.ValueChanged += Feld_Wechsel;
            eltern.Controls.Add(lbl);
            eltern.Controls.Add(num);
            y += ZEILE;
            return num;
        }

        private ComboBox AuswahlZeile(Control eltern, string beschriftung, int x, ref int y,
                                      Steuerwahl[] eintraege)
        {
            var lbl = new Label
            {
                Location = new Point(x, y + 3),
                Size = new Size(250, 20),
                Font = new Font("Segoe UI", 9f),
                Text = beschriftung
            };
            var cb = new ComboBox
            {
                Location = new Point(x + 256, y),
                Size = new Size(160, 23),
                Font = new Font("Segoe UI", 9f),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cb.Items.AddRange(eintraege);
            cb.SelectedIndex = 0;
            cb.SelectedIndexChanged += Feld_Wechsel;
            eltern.Controls.Add(lbl);
            eltern.Controls.Add(cb);
            y += ZEILE;
            return cb;
        }

        private DateTimePicker DatumZeile(Control eltern, string beschriftung, int x, ref int y)
        {
            var lbl = new Label
            {
                Location = new Point(x, y + 3),
                Size = new Size(250, 20),
                Font = new Font("Segoe UI", 9f),
                Text = beschriftung
            };
            var dt = new DateTimePicker
            {
                Location = new Point(x + 256, y),
                Size = new Size(160, 23),
                Font = new Font("Segoe UI", 9f),
                Format = DateTimePickerFormat.Short,
                ShowCheckBox = true,
                Checked = false
            };
            dt.ValueChanged += Feld_Wechsel;
            eltern.Controls.Add(lbl);
            eltern.Controls.Add(dt);
            y += ZEILE;
            return dt;
        }

        private CheckBox Schalterzeile(Control eltern, string beschriftung, int x, ref int y, bool wert)
        {
            var chk = new CheckBox
            {
                Location = new Point(x, y + 2),
                Size = new Size(408, 22),
                Font = new Font("Segoe UI", 9f),
                Text = beschriftung,
                Checked = wert
            };
            chk.CheckedChanged += Feld_Wechsel;
            eltern.Controls.Add(chk);
            y += 27;
            return chk;
        }

        // =====================================================================
        // Kleinkram
        // =====================================================================

        /// <summary>Anzeigetext mit deutschem Rückfall (Konzept § 6.4). Fehlt der
        /// Schlüssel im Katalog, steht der deutsche Wortlaut — der Sammelnachtrag der
        /// <c>.resx</c> holt ihn nach.</summary>
        private static string T(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        private static string Kurzdatum(DateTime? d)
        {
            return d.HasValue ? d.Value.ToString("d", BerichtTexte.Kultur) : "—";
        }

        private string Anlagenarttext(string steuerwert)
        {
            foreach (Steuerwahl w in AnlagenartWahlen(false))
                if (string.Equals(w.Wert, steuerwert ?? "", StringComparison.Ordinal))
                    return w.ToString();
            return "";
        }

        private static void Datum(DateTimePicker dt, DateTime? wert)
        {
            if (wert.HasValue && wert.Value >= dt.MinDate && wert.Value <= dt.MaxDate)
            {
                dt.Value = wert.Value;
                dt.Checked = true;
            }
            else dt.Checked = false;   // unplausibles DB-Datum: nicht übernehmen, nicht abstürzen
        }

        private static void Waehle(ComboBox cb, string wert)
        {
            for (int i = 0; i < cb.Items.Count; i++)
                if (string.Equals(((Steuerwahl)cb.Items[i]).Wert, wert ?? "", StringComparison.Ordinal))
                { cb.SelectedIndex = i; return; }
            cb.SelectedIndex = 0;
        }

        private static string Gewaehlt(ComboBox cb)
        {
            var w = cb != null ? cb.SelectedItem as Steuerwahl : null;
            return w != null ? w.Wert : "";
        }

        private static string GewaehltMitVorgabe(ComboBox cb, string vorgabe)
        {
            var w = cb != null ? cb.SelectedItem as Steuerwahl : null;
            return w != null ? w.Wert : vorgabe;
        }

        private static decimal Geklemmt(NumericUpDown num, double? wert)
        {
            return wert.HasValue ? Geklemmt(num, wert.Value) : num.Minimum;
        }

        private static decimal Geklemmt(NumericUpDown num, double wert)
        {
            decimal d;
            try { d = Convert.ToDecimal(wert); } catch { return num.Minimum; }
            return d < num.Minimum ? num.Minimum : (d > num.Maximum ? num.Maximum : d);
        }
    }
}
