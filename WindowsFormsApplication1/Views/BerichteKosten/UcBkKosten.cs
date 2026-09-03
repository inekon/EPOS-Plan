using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Seite „Kosten" des Reiters „Berichte &amp; Kosten".
    ///
    /// Kompaktanzeige der Projektkosten des in der Übersicht markierten Projekts
    /// (Stamm oder Variante) je Kategorie — **ohne eigene Rechenwelt**:
    /// <list type="bullet">
    ///   <item><b>Investition</b> und <b>Betrieb</b> kommen aus derselben Leselogik,
    ///   die auch die Kapitalwertrechnung verwendet
    ///   (<see cref="WirtschaftlichkeitCtrl.LiesInvestitionen"/> bzw.
    ///   <see cref="WirtschaftlichkeitCtrl.LiesBetriebskosten"/> über
    ///   <c>Tab_ProjektWerte</c>, Kategorie 1 und 2, Szenario „Erwartet").</item>
    ///   <item><b>Energie</b> ist der zuletzt <i>gespeicherte</i> Wert der
    ///   Wirtschaftlichkeitsrechnung (<c>Tab_ErgebnisWirtschaftlichkeit</c> über
    ///   <see cref="WirtschaftlichkeitCtrl.LadeErgebnisse"/>). Die Energiekosten
    ///   entstehen erst aus einem Simulationslauf (KostenEmissionRechner: Verbrauch je
    ///   Energieträger × Preis) — hier wird deshalb angezeigt, was zuletzt berechnet
    ///   wurde, statt still nachzurechnen.</item>
    /// </list>
    /// Darunter die beiden Detaillisten:
    /// Investitionssummen je Komponente
    /// (<see cref="KostenSummenCtrl.LiesKomponentenSummen"/>, Kategorie 1 — die
    /// gemeinsame Leselogik aller Kostenanzeigen)
    /// und die Energieträger des Projekts
    /// (<c>Abfrage_Energietraeger_Effektiv</c> + <c>energy_project_settings</c> /
    /// <c>energy_carrier</c> — dieselbe Vorrangkette wie im
    /// <see cref="KostenEmissionRechner"/>: Projektwert vor Katalogwert).
    ///
    /// „Kostenverwaltung öffnen…" ruft das bestehende Kostenformular als Dialog und
    /// frischt danach die Kompaktwerte auf.
    /// </summary>
    public class UcBkKosten : UserControl
    {
        private int _idProjekt = -1;
        private string _projektname = "";

        /// <summary>
        /// Gewerke, die im Projekt VERBAUT sind, aber keine Investitionsposition führen —
        /// gefüllt in <see cref="LadeKomponenten"/>, gemeldet in der Statuszeile. Ohne diese
        /// Liste zeigte die Seite stumm 0,00 €: Kachel, Tabelle und Fußzeile schwiegen
        /// darüber, DASS ein Gewerk des Projekts überhaupt fehlt.
        /// </summary>
        private readonly List<string> _ohnePosition = new List<string>();

        /// <summary>Ä19: Komponenten mit Kostenpositionen, aber ohne verbaute Anlage.</summary>
        private readonly List<string> _nichtVerbaut = new List<string>();

        /// <summary>
        /// Energieträger des Projekts mit Arbeitspreis 0 — gefüllt in
        /// <see cref="LadeTraeger"/>. Solange hier etwas steht, können die Energiekosten
        /// gar nicht anders als 0 ausfallen; genau das sagt die Statuszeile dann auch.
        /// </summary>
        private readonly List<string> _traegerOhnePreis = new List<string>();

        /// <summary>
        /// Energieträger, die das Projekt VERWENDET, die ihm aber nicht zugeordnet sind
        /// (keine Zeile in <c>energy_project_settings</c>) — gefüllt in
        /// <see cref="LadeTraeger"/>, gemeldet in der Statuszeile. Für sie führt
        /// <c>Abfrage_Energietraeger_Effektiv</c> nichts, also gibt es weder Preis noch
        /// Heizwert; sie fehlen in der Tabelle und sollen deshalb wenigstens dastehen.
        /// </summary>
        private readonly List<string> _traegerNichtZugeordnet = new List<string>();

        /// <summary>
        /// Energieträger, deren Arbeitspreis sich NICHT auf kWh umrechnen lässt, weil der
        /// effektive Heizwert fehlt oder ≤ 0 ist — gefüllt in <see cref="LadeTraeger"/>,
        /// gemeldet in der Statuszeile. Die Spalte „Arbeitspreis [€/kWh]" zeigt für sie
        /// „—"; ohne diese Liste stünde dort ein Strich ohne Begründung.
        /// </summary>
        private readonly List<string> _traegerOhneHeizwert = new List<string>();

        private readonly WirtschaftlichkeitCtrl _wirt = new WirtschaftlichkeitCtrl();

        // Steuerelemente
        private TableLayoutPanel tl;
        private TableLayoutPanel pnlKopf;
        private Label lblProjekt;
        private Button btnVerwaltung;
        private TableLayoutPanel pnlKacheln;
        private Kachel kInvest, kBetrieb, kEnergie;
        private TableLayoutPanel pnlListen;
        private Label lblKomponenten;
        private DataGridView gridKomponenten;
        private Label lblTraeger;
        private DataGridView gridTraeger;
        private Label lblStatus;

        public UcBkKosten()
        {
            InitializeComponent();
            InfoKnopf.Anbringen(this);   // H7: Infoknopf oben rechts -> help_mapping.txt
        }

        // ------------------------------------------------------------- Aufbau

        private void InitializeComponent()
        {
            this.tl = new TableLayoutPanel();
            this.pnlKopf = new TableLayoutPanel();
            this.lblProjekt = new Label();
            this.btnVerwaltung = new Button();
            this.pnlKacheln = new TableLayoutPanel();
            this.kInvest = new Kachel();
            this.kBetrieb = new Kachel();
            this.kEnergie = new Kachel();
            this.pnlListen = new TableLayoutPanel();
            this.lblKomponenten = new Label();
            this.gridKomponenten = new DataGridView();
            this.lblTraeger = new Label();
            this.gridTraeger = new DataGridView();
            this.lblStatus = new Label();
            this.SuspendLayout();

            // --- Kopfzeile: Projektname + Einstieg in die Kostenverwaltung ---
            this.lblProjekt.Dock = DockStyle.Fill;
            this.lblProjekt.Margin = new Padding(0);
            this.lblProjekt.Font = new Font("Segoe UI", 9.75f, FontStyle.Bold);
            this.lblProjekt.TextAlign = ContentAlignment.MiddleLeft;
            this.lblProjekt.Text = MyResource.Resource.BK_KOSTEN_KEIN_PROJEKT;

            this.btnTraeger.Dock = DockStyle.Fill;
            this.btnTraeger.Margin = new Padding(12, 0, 0, 0);
            this.btnTraeger.AutoSize = true;
            this.btnTraeger.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.btnTraeger.Text = Text_("BK_KOSTEN_BTN_TRAEGER", "Energieträgerverwaltung…");
            this.btnTraeger.Click += new EventHandler(this.btnTraeger_Click);

            this.btnVerwaltung.Dock = DockStyle.Fill;
            this.btnVerwaltung.Margin = new Padding(12, 0, 0, 0);
            this.btnVerwaltung.AutoSize = true;
            this.btnVerwaltung.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.btnVerwaltung.Text = MyResource.Resource.BK_KOSTEN_BTN_VERWALTUNG;
            this.btnVerwaltung.Click += new EventHandler(this.btnVerwaltung_Click);

            this.pnlKopf.Dock = DockStyle.Fill;
            this.pnlKopf.ColumnCount = 3;
            this.pnlKopf.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this.pnlKopf.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            this.pnlKopf.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            this.pnlKopf.RowCount = 1;
            this.pnlKopf.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this.pnlKopf.Margin = new Padding(0, 0, 0, 8);
            this.pnlKopf.Controls.Add(this.lblProjekt, 0, 0);
            this.pnlKopf.Controls.Add(this.btnTraeger, 1, 0);
            this.pnlKopf.Controls.Add(this.btnVerwaltung, 2, 0);

            // --- Drei Kategorie-Kacheln ---
            this.kInvest.Setze(MyResource.Resource.BK_KOSTEN_INVEST,
                               MyResource.Resource.BK_KOSTEN_INVEST_HINT);
            this.kBetrieb.Setze(MyResource.Resource.BK_KOSTEN_BETRIEB,
                                MyResource.Resource.BK_KOSTEN_BETRIEB_HINT);
            this.kEnergie.Setze(MyResource.Resource.BK_KOSTEN_ENERGIE,
                                MyResource.Resource.BK_KOSTEN_ENERGIE_HINT);

            this.pnlKacheln.Dock = DockStyle.Fill;
            this.pnlKacheln.ColumnCount = 3;
            for (int i = 0; i < 3; i++)
                this.pnlKacheln.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3f));
            this.pnlKacheln.RowCount = 1;
            this.pnlKacheln.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this.pnlKacheln.Margin = new Padding(0, 0, 0, 10);
            this.pnlKacheln.Controls.Add(this.kInvest, 0, 0);
            this.pnlKacheln.Controls.Add(this.kBetrieb, 1, 0);
            this.pnlKacheln.Controls.Add(this.kEnergie, 2, 0);

            // --- Detaillisten ---
            this.lblKomponenten.Dock = DockStyle.Fill;
            this.lblKomponenten.Margin = new Padding(0);
            this.lblKomponenten.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            this.lblKomponenten.Text = MyResource.Resource.BK_KOSTEN_LBL_KOMPONENTEN;

            BereiteGridVor(this.gridKomponenten);
            this.gridKomponenten.Margin = new Padding(0, 0, 8, 0);
            // Ä19: Die Auswahl einer Anlage kennzeichnet ihren Energieträger rechts.
            this.gridKomponenten.SelectionChanged += new EventHandler(this.gridKomponenten_SelectionChanged);
            // Ä21: Doppelklick löscht die Positionen einer gelben Zeile (Rückfrage).
            this.gridKomponenten.CellDoubleClick += new DataGridViewCellEventHandler(this.gridKomponenten_CellDoubleClick);

            this.lblTraeger.Dock = DockStyle.Fill;
            this.lblTraeger.Margin = new Padding(8, 0, 0, 0);
            this.lblTraeger.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            this.lblTraeger.Text = MyResource.Resource.BK_KOSTEN_LBL_TRAEGER;

            BereiteGridVor(this.gridTraeger);
            this.gridTraeger.Margin = new Padding(8, 0, 0, 0);

            this.pnlListen.Dock = DockStyle.Fill;
            this.pnlListen.ColumnCount = 2;
            // 38/62 statt 42/58 seit 23.08.2026: Die Trägertabelle führt seit dem
            // kWh-bezogenen Arbeitspreis SECHS Spalten, die Komponententabelle nach dem
            // Wegfall des Technik-Planwerts nur noch zwei. Die zusätzlichen vier Prozent
            // gehen dorthin, wo sie gebraucht werden.
            // 30/70 seit 30.08.2026: Die Trägertabelle führt jetzt ZEHN Spalten
            // (Preise, Leistungspreis, drei Emissionsfaktoren), die Komponententabelle
            // unverändert drei. Begründung und Spaltengewichte in LadeTraeger.
            this.pnlListen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            this.pnlListen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            this.pnlListen.RowCount = 2;
            this.pnlListen.RowStyles.Add(new RowStyle(SizeType.Absolute, 22f));
            this.pnlListen.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this.pnlListen.Margin = new Padding(0);
            this.pnlListen.Controls.Add(this.lblKomponenten, 0, 0);
            this.pnlListen.Controls.Add(this.lblTraeger, 1, 0);
            this.pnlListen.Controls.Add(this.gridKomponenten, 0, 1);
            this.pnlListen.Controls.Add(this.gridTraeger, 1, 1);

            // --- Statuszeile ---
            this.lblStatus.Dock = DockStyle.Fill;
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Margin = new Padding(0, 4, 0, 0);

            // --- Raster ---
            this.tl.Dock = DockStyle.Fill;
            this.tl.ColumnCount = 1;
            this.tl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this.tl.RowCount = 4;
            this.tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));    // Kopf
            this.tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 92f));    // Kacheln
            this.tl.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));    // Listen
            this.tl.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));    // Status
            this.tl.Padding = new Padding(10, 8, 10, 6);
            this.tl.Controls.Add(this.pnlKopf, 0, 0);
            this.tl.Controls.Add(this.pnlKacheln, 0, 1);
            this.tl.Controls.Add(this.pnlListen, 0, 2);
            this.tl.Controls.Add(this.lblStatus, 0, 3);

            this.Controls.Add(this.tl);
            this.Font = new Font("Segoe UI", 9f);
            this.Name = "UcBkKosten";
            this.Size = new Size(1040, 520);
            this.ResumeLayout(false);
        }

        private static void BereiteGridVor(DataGridView g)
        {
            g.Dock = DockStyle.Fill;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.ReadOnly = true;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        }

        // ------------------------------------------------------------- Daten

        /// <summary>
        /// Setzt das anzuzeigende Projekt (Stamm oder Variante) und liest die
        /// Kompaktwerte neu. idProjekt &lt;= 0 leert die Seite.
        /// </summary>
        public void SetzeProjekt(int idProjekt, string projektname)
        {
            _idProjekt = idProjekt;
            _projektname = projektname ?? "";
            Aktualisiere();
        }

        /// <summary>Liest alle angezeigten Werte neu (nach dem Kostendialog, nach Simulation).</summary>
        public void Aktualisiere()
        {
            if (this.DesignMode) return;

            gridKomponenten.Rows.Clear(); gridKomponenten.Columns.Clear();
            gridTraeger.Rows.Clear(); gridTraeger.Columns.Clear();

            if (_idProjekt <= 0)
            {
                lblProjekt.Text = MyResource.Resource.BK_KOSTEN_KEIN_PROJEKT;
                kInvest.Wert = "—"; kBetrieb.Wert = "—"; kEnergie.Wert = "—";
                btnVerwaltung.Enabled = false;
                btnTraeger.Enabled = false;
                Melde("");
                return;
            }

            btnVerwaltung.Enabled = true;
            btnTraeger.Enabled = true;
            lblProjekt.Text = string.Format(MyResource.Resource.BK_KOSTEN_PROJEKT, _projektname);

            var kultur = BerichtTexte.Kultur;

            // --- Kategorie 1: Investition (gleiche Leselogik wie die Kapitalwertrechnung) ---
            double invest = 0;
            double zuschuss = 0;
            int investPositionen = 0;
            try
            {
                // ETAPPE K5: dieselbe Leseüberladung wie der Rechenkern — die
                // Zuschusszeilen kommen getrennt heraus und zählen deshalb weder als
                // Investitionsposition noch in die Summe.
                var positionen = WirtschaftlichkeitCtrl.LiesInvestitionen(
                    _idProjekt, WirtschaftlichkeitSzenario.ERWARTET, out zuschuss);
                investPositionen = positionen.Count;
                foreach (KapitalwertRechner.InvestPosition p in positionen) invest += p.Betrag;
            }
            catch { }
            // Keine einzige Investitionsposition heißt NICHT „das Projekt kostet nichts".
            // 0,00 € wäre an dieser Stelle eine Aussage, die niemand getroffen hat — dieselbe
            // Unterscheidung wie auf der Stromspeicher-Ergebnisseite („—" statt 0,0 %).
            kInvest.Wert = (investPositionen > 0)
                ? invest.ToString("N2", kultur) + " " + MyResource.Resource.BK_KOSTEN_EINHEIT_EUR
                : "—";

            // ETAPPE K5 — der Zuschuss als eigene, negativ ausgewiesene Zeile unter dem
            // Investitionsbetrag. Der große Wert bleibt die BRUTTO-Investitionssumme:
            // Sie ist die Zahl, die der Anwender in der Kostenmaske erfasst hat und dort
            // wiederfinden muss. Ohne Zuschuss bleibt die gewohnte Herkunftszeile stehen.
            kInvest.Quelle = (zuschuss > 0)
                ? string.Format(MyResource.Resource.BK_KOSTEN_ZUSCHUSS,
                                zuschuss.ToString("N2", kultur))
                : MyResource.Resource.BK_KOSTEN_INVEST_HINT;

            // --- Kategorie 2: Betrieb ---
            double betrieb = 0;
            int betriebPositionen = 0;
            try
            {
                betrieb = WirtschaftlichkeitCtrl.LiesBetriebskosten(
                    _idProjekt, WirtschaftlichkeitSzenario.ERWARTET);

                // Nur zum Unterscheiden von „nichts erfasst" und „erfasst, aber 0" —
                // gerechnet wird weiterhin ausschließlich mit LiesBetriebskosten.
                DataTable bt = KostenSummenCtrl.LiesKomponentenSummen(
                    _idProjekt, KostenSummenCtrl.KATEGORIE_BETRIEB);
                betriebPositionen = (bt != null) ? bt.Rows.Count : 0;
            }
            catch { }
            kBetrieb.Wert = (betriebPositionen > 0)
                ? betrieb.ToString("N2", kultur) + " " + MyResource.Resource.BK_KOSTEN_EINHEIT_EUR_A
                : "—";

            // --- Energie: zuletzt gespeicherter Wert der Wirtschaftlichkeitsrechnung ---
            string energieHinweis = "";
            bool energieNull = true;      // nichts berechnet ODER 0,00 €/a
            try
            {
                WirtschaftlichkeitErgebnis erg = _wirt
                    .LadeErgebnisse(new List<int> { _idProjekt })
                    .FirstOrDefault(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET);
                if (erg == null || !erg.EnergiekostenJahr.HasValue)
                {
                    kEnergie.Wert = "—";
                    energieHinweis = MyResource.Resource.BK_KOSTEN_ENERGIE_FEHLT;
                }
                else
                {
                    kEnergie.Wert = erg.EnergiekostenJahr.Value.ToString("N2", kultur) + " " +
                                    MyResource.Resource.BK_KOSTEN_EINHEIT_EUR_A;
                    energieNull = Math.Abs(erg.EnergiekostenJahr.Value) < 0.005;
                    energieHinweis = string.Format(MyResource.Resource.BK_KOSTEN_STAND,
                        erg.Zeitstempel.ToString("dd.MM.yyyy HH:mm"));
                }
            }
            catch { kEnergie.Wert = "—"; }

            LadeKomponenten(kultur);
            LadeTraeger(kultur);

            string status = string.Format(MyResource.Resource.BK_KOSTEN_STATUS,
                                          investPositionen, energieHinweis).Trim();

            // Ein verbautes Gewerk ohne Kostenposition ist kein Rechenfehler, sondern eine
            // FEHLENDE EINGABE — und muss als solche dastehen, statt sich hinter einer 0,00 €
            // zu verstecken (dieselbe Haltung wie auf der Stromspeicher-Ergebnisseite, wo
            // statt irreführender 0,0 % ein „—" mit Warnzeile steht).
            if (_ohnePosition.Count > 0)
                status += "  ·  " + string.Format(MyResource.Resource.BK_KOSTEN_OHNE_POSITION,
                                                  string.Join(", ", _ohnePosition.ToArray()));

            // Ä19: Kostenpositionen ohne verbaute Anlage (siehe LadeKomponenten).
            if (_nichtVerbaut.Count > 0)
                status += "  ·  " + string.Format(
                    Text_("BK_KOSTEN_STATUS_NICHT_VERBAUT",
                          "Kostenpositionen ohne verbaute Anlage: {0}"),
                    string.Join(", ", _nichtVerbaut.ToArray()));

            // 0,00 €/a Energiekosten bei ungepflegtem Arbeitspreis: die Zahl ist rechnerisch
            // richtig und trotzdem wertlos, solange niemand sagt, warum sie null ist.
            if (energieNull && _traegerOhnePreis.Count > 0)
                status += "  ·  " + string.Format(MyResource.Resource.BK_KOSTEN_ENERGIE_PREIS0,
                                                  string.Join(", ", _traegerOhnePreis.ToArray()));

            // Ein Träger, den das Projekt FÄHRT, dem aber keine Projekteinstellung
            // gegenübersteht, hat in Abfrage_Energietraeger_Effektiv keine Zeile — er
            // fehlte in der Tabelle schon vor der Filterung, nur sagte es niemand. Jetzt
            // steht er wenigstens in der Fußzeile, statt still zu verschwinden.
            if (_traegerNichtZugeordnet.Count > 0)
                status += "  ·  " + string.Format(MyResource.Resource.BK_KOSTEN_TRAEGER_FEHLT,
                                                  string.Join(", ", _traegerNichtZugeordnet.ToArray()));

            // Ohne Heizwert bleibt die Spalte „Arbeitspreis [€/kWh]" leer — ein „—" ohne
            // Begründung ist aber genauso stumm wie die 0,00 €, die es ersetzt. Die
            // Fußzeile nennt deshalb die Träger, denen der Heizwert fehlt.
            if (_traegerOhneHeizwert.Count > 0)
                status += "  ·  " + string.Format(MyResource.Resource.BK_KOSTEN_TRAEGER_HI0,
                                                  string.Join(", ", _traegerOhneHeizwert.ToArray()));

            Melde(status);
        }

        // Investitionssummen je Komponente — die gemeinsame Leselogik aller
        // Kostenanzeigen (KostenSummenCtrl.LiesKomponentenSummen).
        //
        // Befund D1 (18.08.2026): Hier lief zuvor die gespeicherte Abfrage
        // Abfrage_KostenKomponenten, die NICHT nach KategorieID filtert. Die Tabelle mischte
        // dadurch Investitions- und Betriebspositionen und widersprach der Kachel darüber:
        // Projekt 1024 zeigte in der Kachel „Investition" 12.001,00 €, in der Zeile „Gesamt"
        // der Tabelle aber 12.100,00 € (99 € Betriebskosten der Wärmepumpe mitgezählt).
        // Die Tabelle gehört fachlich zur Investitions-Kachel und liest deshalb Kategorie 1;
        // Spaltenkopf und Überschrift sagen das jetzt auch (BK_KOSTEN_SP_SUMME,
        // BK_KOSTEN_LBL_KOMPONENTEN).
        //
        // Nutzerentscheid 23.08.2026: Die zweite Spalte „Technik-Planwert" ist entfallen.
        // Sie stellte neben die Summe ALLER Investitionspositionen einer Komponente den
        // Planwert der HAUPTposition allein — zwei verschiedene Größen unter zwei
        // Spaltenköpfen, die dasselbe zu meinen schienen. Verglichen wird der Planwert mit
        // der Hauptposition weiterhin, aber dort, wo beide Zahlen gepflegt werden
        // (KostenPositionCtrl.Pruefe, KomponentenUebernahmeCtrl, KI-Auskunft). Diese
        // Tabelle zeigt jetzt nur noch die erfassten Kosten je Komponente.
        private void LadeKomponenten(System.Globalization.CultureInfo kultur)
        {
            _ohnePosition.Clear();
            _nichtVerbaut.Clear();

            // Ä19 (Nutzerauftrag 26.08.2026): Die Liste zeigt ANLAGEN, nicht
            // Komponentengruppen — zwei Wärmepumpen sind zwei Zeilen, und die
            // Auswahl einer Zeile kennzeichnet rechts den Träger der Anlage.
            // GEPFLEGT werden die Kosten weiterhin je KOMPONENTE (FK2): Die
            // Summen stehen an der ERSTEN Anlage ihrer Komponente, Folgeanlagen
            // zeigen „—“ mit Tooltip — so bleibt die Gesamtzeile die
            // Projektsumme, ohne doppelt zu zählen.
            gridKomponenten.Columns.Add("komponente", Text_("BK_KOSTEN_SP_ANLAGE", "Anlage / Komponente"));
            gridKomponenten.Columns.Add("summe", MyResource.Resource.BK_KOSTEN_SP_SUMME);
            // ETAPPE KD6 (§ 10): Betriebskosten als dritte Spalte im selben Raster.
            gridKomponenten.Columns.Add("betrieb", Text_("BK_KOSTEN_SP_BETRIEB", "Betrieb [€/a]"));
            gridKomponenten.Columns[0].FillWeight = 140;
            gridKomponenten.Columns[1].FillWeight = 60;
            gridKomponenten.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridKomponenten.Columns[2].FillWeight = 60;
            gridKomponenten.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Ä21: Selbstheilung VOR dem Lesen — der Anlagen-Wizard vergibt beim
            // Neuaufbau neue Anlagen-IDs; verwaiste Zuordnungen kommen über den
            // Geräteanker zurück an ihre Anlage.
            try { KostenProjektPositionenCtrl.ZuordnungReparieren(_idProjekt); } catch { }

            try
            {
                // Ä20: Summen je ANLAGENZEILE (Migrationsschritt 45). „Lose“ heißt:
                // ID_Anlage NULL oder Verweis auf eine gelöschte Anlage — beides
                // erscheint als gelbe Zeile je Komponente und zählt in die Summe
                // (Kachel = Tabelle). Fällt die Spalte auf einer Alt-Datenbank aus
                // (LiesAnlagenSummen = null), bleiben die Anlagenzeilen ohne Betrag.
                List<ProjektEnergietraegerCtrl.AnlagenEintrag> anlagen =
                    ProjektEnergietraegerCtrl.AnlagenMitTraeger(_idProjekt);
                var anlagenIds = new HashSet<int>();
                foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in anlagen)
                    anlagenIds.Add(a.AnlageId);

                var investAnlage = new Dictionary<int, double>();
                var betriebAnlage = new Dictionary<int, double>();
                var investLose = new Dictionary<string, double>(StringComparer.Ordinal);
                var betriebLose = new Dictionary<string, double>(StringComparer.Ordinal);
                var komponentenMitPositionen = new HashSet<string>(StringComparer.Ordinal);
                AnlagenSummenLesen(KostenSummenCtrl.KATEGORIE_INVESTITION, anlagen, anlagenIds,
                                   investAnlage, investLose, komponentenMitPositionen);
                AnlagenSummenLesen(KostenSummenCtrl.KATEGORIE_BETRIEB, anlagen, anlagenIds,
                                   betriebAnlage, betriebLose, komponentenMitPositionen);

                double summe = 0, summeBetrieb = 0;
                var rotGemeldet = new HashSet<string>(StringComparer.Ordinal);
                foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in anlagen)
                {
                    double invest, bWert;
                    bool hatI = investAnlage.TryGetValue(a.AnlageId, out invest);
                    bool hatB = betriebAnlage.TryGetValue(a.AnlageId, out bWert);
                    if (hatI) summe += invest;
                    if (hatB) summeBetrieb += bWert;

                    string name = string.IsNullOrEmpty(a.Bezeichner)
                        ? a.Komponente
                        : a.Komponente + " — " + a.Bezeichner;

                    int idx = gridKomponenten.Rows.Add(
                        name,
                        hatI ? invest.ToString("N2", kultur) : "—",
                        hatB ? bWert.ToString("N2", kultur) : "—");
                    gridKomponenten.Rows[idx].Tag = a;

                    if (!komponentenMitPositionen.Contains(a.Komponente))
                    {
                        // Die KOMPONENTE hat nirgends eine Position: FEHLENDE
                        // EINGABE, kein Nullbetrag (Nutzerentscheidung 4, 18.08.2026).
                        gridKomponenten.Rows[idx].DefaultCellStyle.BackColor =
                            Color.FromArgb(0xFF, 0xE6, 0xE6);
                        string hinweis = string.Format(
                            MyResource.Resource.BK_KOSTEN_OHNE_POSITION_HINT, a.Komponente);
                        foreach (DataGridViewCell c in gridKomponenten.Rows[idx].Cells)
                            c.ToolTipText = hinweis;
                        if (!rotGemeldet.Contains(a.Komponente))
                        {
                            rotGemeldet.Add(a.Komponente);
                            _ohnePosition.Add(a.Komponente);
                        }
                    }
                    else if (!hatI && !hatB)
                    {
                        string hinweis = Text_("BK_KOSTEN_ANLAGE_OHNE_POSITIONEN",
                            "Diese Anlage führt keine eigenen Positionen — „Kosten bearbeiten…“ im Anlagendialog oder die Kostenverwaltung pflegt sie je Anlage.");
                        foreach (DataGridViewCell c in gridKomponenten.Rows[idx].Cells)
                            c.ToolTipText = hinweis;
                    }
                }

                // Positionen ohne (gültigen) Anlagenbezug, in zwei Klassen:
                // Ä24 — die Erfassungsgruppen der KD1-Saat (Wärmezentrale,
                // Bauliche Anlagen, Stromeinspeisung; nicht anlagenfähig im Sinne
                // von Ä7) KÖNNEN keiner Anlage zugeordnet sein — sie erscheinen
                // als reguläre Komponentenzeile. GELB bleiben nur anlagenfähige
                // Komponenten: Variantenreste, gelöschte/getauschte Anlagen.
                var reste = new List<string>();
                foreach (string k in investLose.Keys) reste.Add(k);
                foreach (string k in betriebLose.Keys) if (!reste.Contains(k)) reste.Add(k);
                var resteGelb = new List<string>();
                foreach (string k in reste)
                {
                    double invest, bWert;
                    bool hatI = investLose.TryGetValue(k, out invest);
                    bool hatB = betriebLose.TryGetValue(k, out bWert);
                    if (KostenVorlagenCtrl.IstWaehlbar(k)) { resteGelb.Add(k); continue; }
                    if (hatI) summe += invest;
                    if (hatB) summeBetrieb += bWert;
                    int idxR = gridKomponenten.Rows.Add(
                        k,
                        hatI ? invest.ToString("N2", kultur) : "—",
                        hatB ? bWert.ToString("N2", kultur) : "—");
                    var gruppe = new ProjektEnergietraegerCtrl.AnlagenEintrag();
                    gruppe.Komponente = k;   // AnlageId 0: Verwaltung öffnet die Komponente
                    gridKomponenten.Rows[idxR].Tag = gruppe;
                }
                foreach (string k in resteGelb)
                {
                    double invest, bWert;
                    bool hatI = investLose.TryGetValue(k, out invest);
                    bool hatB = betriebLose.TryGetValue(k, out bWert);
                    if (hatI) summe += invest;
                    if (hatB) summeBetrieb += bWert;

                    int idx = gridKomponenten.Rows.Add(
                        string.Format(Text_("BK_KOSTEN_NICHT_VERBAUT", "{0} — ohne Anlagenzuordnung"), k),
                        hatI ? invest.ToString("N2", kultur) : "—",
                        hatB ? bWert.ToString("N2", kultur) : "—");
                    gridKomponenten.Rows[idx].DefaultCellStyle.BackColor =
                        Color.FromArgb(0xFF, 0xF4, 0xCC);
                    // Ä21: Der Tag trägt den Komponentennamen — Doppelklick löscht
                    // die losen Positionen nach Rückfrage (gridKomponenten_DoubleClick).
                    gridKomponenten.Rows[idx].Tag = k;
                    string hinweis = Text_("BK_KOSTEN_NICHT_VERBAUT_HINT",
                        "Kostenpositionen ohne (gültige) Anlagenzuordnung — sie rechnen in der Wirtschaftlichkeit mit. Doppelklick löscht sie nach Rückfrage; bearbeiten: Kostenverwaltung, Eintrag „(ohne Anlagenzuordnung)“.");
                    foreach (DataGridViewCell c in gridKomponenten.Rows[idx].Cells)
                        c.ToolTipText = hinweis;
                    _nichtVerbaut.Add(k);
                }

                if (gridKomponenten.Rows.Count > 0)
                {
                    int idx = gridKomponenten.Rows.Add(MyResource.Resource.BK_KOSTEN_SUMME,
                                                       summe.ToString("N2", kultur),
                                                       summeBetrieb.ToString("N2", kultur));
                    gridKomponenten.Rows[idx].DefaultCellStyle.Font =
                        new Font(gridKomponenten.Font, FontStyle.Bold);
                }
                gridKomponenten.ClearSelection();
            }
            catch { }
        }

        /// <summary>Ä20: Summen einer Kategorie je Anlage einlesen; „lose“ Zeilen
        /// (NULL oder gelöschte Anlage) laufen je Komponente auf. Rückfall ohne
        /// Spalte: Komponentensummen als lose Zeilen, damit nichts verschwindet.</summary>
        private void AnlagenSummenLesen(int kategorie,
            List<ProjektEnergietraegerCtrl.AnlagenEintrag> anlagen, HashSet<int> anlagenIds,
            Dictionary<int, double> jeAnlage, Dictionary<string, double> jeLose,
            HashSet<string> komponentenMitPositionen)
        {
            try
            {
                DataTable t = KostenSummenCtrl.LiesAnlagenSummen(_idProjekt, kategorie);
                if (t == null)
                {
                    DataTable alt = KostenSummenCtrl.LiesKomponentenSummen(_idProjekt, kategorie);
                    if (alt != null)
                        foreach (DataRow r in alt.Rows)
                        {
                            double? w = D(r, "Summe");
                            if (!w.HasValue) continue;
                            string k = S(r, "Komponente");
                            jeLose[k] = w.Value;
                            komponentenMitPositionen.Add(k);
                        }
                    return;
                }
                foreach (DataRow r in t.Rows)
                {
                    double? w = D(r, "Summe");
                    if (!w.HasValue) continue;
                    string k = S(r, "Komponente");
                    komponentenMitPositionen.Add(k);
                    bool lose = r["ID_Anlage"] == DBNull.Value ||
                                !anlagenIds.Contains(Convert.ToInt32(r["ID_Anlage"]));
                    if (lose)
                    {
                        double alt2;
                        jeLose.TryGetValue(k, out alt2);
                        jeLose[k] = alt2 + w.Value;
                    }
                    else
                        jeAnlage[Convert.ToInt32(r["ID_Anlage"])] = w.Value;
                }
            }
            catch { }
        }

        /// <summary>Ä21: Doppelklick auf eine gelbe Zeile löscht die Positionen
        /// ohne (gültige) Anlagenzuordnung dieser Komponente — mit Rückfrage samt
        /// Anzahl; der Weg läuft über die Einzellöschung des Controllers.</summary>
        private void gridKomponenten_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            string komponente = gridKomponenten.Rows[e.RowIndex].Tag as string;
            if (string.IsNullOrEmpty(komponente)) return;

            object kid = null;
            try
            {
                kid = DataRepository.ExecuteScalar(
                    "SELECT ID FROM Tab_KostenKomponente WHERE Komponente = ?",
                    new DbParam("@k", komponente));
            }
            catch { }
            if (kid == null || kid == DBNull.Value) return;

            if (MessageBox.Show(
                    string.Format(Text_("BK_KOSTEN_LOSE_LOESCHEN",
                        "Alle Kostenpositionen ohne Anlagenzuordnung der Komponente „{0}“ " +
                        "löschen?\n\nSie stammen z. B. aus einer Variantenkopie ohne dieses " +
                        "Gewerk und rechnen bis dahin in der Wirtschaftlichkeit mit."),
                        komponente),
                    Text_("BK_KOSTEN_LOSE_TITEL", "Positionen ohne Anlagenzuordnung"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            int n = KostenProjektPositionenCtrl.LoseLoeschen(_idProjekt, Convert.ToInt32(kid));
            Melde(string.Format(Text_("BK_KOSTEN_LOSE_GELOESCHT",
                "{0} Position(en) der Komponente „{1}“ gelöscht."), n, komponente));
            Aktualisiere();
        }

        /// <summary>Ä19: kennzeichnet rechts den Energieträger der gewählten Anlage.</summary>
        private void gridKomponenten_SelectionChanged(object sender, EventArgs e)
        {
            if (gridKomponenten.CurrentRow == null) return;
            var a = gridKomponenten.CurrentRow.Tag as ProjektEnergietraegerCtrl.AnlagenEintrag;
            gridTraeger.ClearSelection();
            if (a == null || a.CarrierId <= 0) return;
            foreach (DataGridViewRow r in gridTraeger.Rows)
                if (r.Tag is int && (int)r.Tag == a.CarrierId) { r.Selected = true; break; }
        }


        /// <summary>
        /// Die im Projekt VERWENDETEN Energieträger mit Abrechnungseinheit, effektivem
        /// Heizwert und den wirksamen Preisen (Projektwert vor Katalogwert — Kette wie im
        /// <see cref="KostenEmissionRechner"/>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Befund vom 22.08.2026: die Liste zeigte Träger, die das Projekt gar nicht
        /// fährt.</b> Angezeigt wurde schlicht der Inhalt von
        /// <c>Abfrage_Energietraeger_Effektiv</c> — und der stammt aus
        /// <c>energy_project_settings</c>. Das sind EINSTELLUNGEN je Träger (Preis,
        /// Heizwert, CO₂-Faktor), keine Verwendungsliste. Die Variante „Wöhler - Test2"
        /// (Projekt 1024) führt dort acht Träger und zeigte alle acht, obwohl im Projekt
        /// nur ein BHKW, ein elektrischer Heizkessel, eine Wärmepumpe und zwei
        /// Pufferspeicher stehen. Sechs der acht konnten nie eine Kilowattstunde liefern.
        /// </para>
        /// <para>
        /// <b>Gefiltert wird im Code, nicht in der Abfrage.</b>
        /// <c>Abfrage_Energietraeger_Effektiv</c> liegt außerhalb des Repos und wird von
        /// vier Stellen gelesen — die drei anderen schlagen gezielt EINEN Träger nach
        /// (<see cref="KostenEmissionRechner"/>, <c>WirtschaftlichkeitCtrl.Traeger</c>,
        /// <see cref="EnergieMengen.Menge"/>) und sind von diesem Befund nicht betroffen.
        /// Die Verwendungsmenge kommt aus
        /// <see cref="ProjektEnergietraegerCtrl.Verwendete"/>; dort steht auch, welches
        /// Gewerk welchen Träger beiträgt und wann <c>ID_Carrier</c> gilt und wann das
        /// ältere <c>Brennstoff</c>-Feld der Gerätetabelle.
        /// </para>
        /// <para>
        /// <b>Die Filterung kann nur wegnehmen.</b> Ein verwendeter Träger OHNE Zuordnung
        /// in <c>energy_project_settings</c> hat in der gespeicherten Abfrage ohnehin
        /// keine Zeile — er stand also auch vorher nicht in der Tabelle. Neu ist, dass er
        /// nicht mehr stillschweigend fehlt: Die Fußzeile nennt ihn. Genau dieser Fall
        /// steht im Bestand — Projekt 1023 fährt einen Erdgas-E-Kessel, dem Projekt ist
        /// aber nur „Elektrische Energie" zugeordnet.
        /// </para>
        /// <para>
        /// <b>Nutzerwunsch 23.08.2026: der Arbeitspreis auch je Kilowattstunde.</b> Von den
        /// 21 Katalogträgern rechnen 17 NICHT in kWh ab (7× <c>L</c>, 8× <c>Nm³</c>,
        /// 2× <c>kg</c>); nur Strom und Fernwärme tun es. Die Spalte
        /// „Arbeitspreis [€/Einheit]" stellte damit Preise nebeneinander, die sich nicht
        /// vergleichen lassen — 0,98 €/L und 0,35 €/kWh sagen nichts übereinander. Die
        /// sechste Spalte rechnet deshalb <c>Arbeitspreis ÷ eff_hi</c> und macht die Zeilen
        /// vergleichbar. Drei Festlegungen dazu:
        /// </para>
        /// <list type="number">
        ///   <item><description><b>Nur der Arbeitspreis.</b> Der Grundpreis steht in €/a und
        ///     ist mengenunabhängig — ihn durch einen Heizwert zu teilen wäre ein stiller
        ///     Rechenfehler, keine Umrechnung.</description></item>
        ///   <item><description><b>Bezugsgröße ist <c>eff_hi</c></b>, nicht der
        ///     Katalogheizwert: Heizöl EL trägt im Katalog 10,00 kWh/L, Projekt 1024
        ///     übersteuert über <c>energy_project_settings.custom_hi</c> auf 11,20 — und
        ///     11,20 steht auch in der Nachbarspalte.</description></item>
        ///   <item><description><b>Träger, die schon in kWh abrechnen, zeigen den Wert
        ///     trotzdem</b> (er ist dann gleich dem Arbeitspreis, <c>eff_hi</c> = 1,00).
        ///     Eine Spalte, die zeilenweise leer bliebe, taugt nicht zum Vergleichen —
        ///     und genau darum geht es.</description></item>
        /// </list>
        /// <para>
        /// <b>„—" statt einer Zahl</b>, wenn der Heizwert fehlt oder ≤ 0 ist (dann ist die
        /// Umrechnung unmöglich; die Fußzeile nennt den Träger über
        /// <c>BK_KOSTEN_TRAEGER_HI0</c>) und wenn kein Arbeitspreis gepflegt ist. Ein
        /// Arbeitspreis von 0 ist dabei KEIN Preis, sondern eine fehlende Eingabe — dieselbe
        /// Lesart, die <see cref="_traegerOhnePreis"/> schon anwendet. „0,0000 €/kWh" würde
        /// behaupten, die Kilowattstunde sei umsonst; dieselbe Haltung wie beim Betrag „—"
        /// der Gewerke ohne Kostenposition.
        /// </para>
        /// </remarks>
        private void LadeTraeger(System.Globalization.CultureInfo kultur)
        {
            _traegerOhnePreis.Clear();
            _traegerNichtZugeordnet.Clear();
            _traegerOhneHeizwert.Clear();

            gridTraeger.Columns.Add("traeger", MyResource.Resource.BK_KOSTEN_SP_TRAEGER);
            gridTraeger.Columns.Add("einheit", MyResource.Resource.BK_KOSTEN_SP_ABRECHNUNG);
            gridTraeger.Columns.Add("hi", MyResource.Resource.BK_KOSTEN_SP_HEIZWERT);
            gridTraeger.Columns.Add("arbeit", MyResource.Resource.BK_KOSTEN_SP_ARBEITSPREIS);
            gridTraeger.Columns.Add("arbeitkwh", MyResource.Resource.BK_KOSTEN_SP_ARBEITSPREIS_KWH);
            gridTraeger.Columns.Add("grund", MyResource.Resource.BK_KOSTEN_SP_GRUNDPREIS);
            // ETAPPE KD6 (§ 10, § 7.1): der effektive Leistungspreis als Jahreswert —
            // Monatssätze × 12, dieselbe Vorrangkette wie im KostenEmissionRechner
            // (Projekt vor Katalog, 0 = nicht gepflegt); Strom zeigt „—“ (Tarifwelt).
            gridTraeger.Columns.Add("leistung", Text_("BK_KOSTEN_SP_LEISTUNGSPREIS", "Leistungspreis [€/(kW·a)]"));
            // 30.08.2026 (Anwenderentscheid): die drei Emissionsfaktoren, mit denen die
            // Kennzahlen dieses Projekts tatsächlich rechnen — gelesen über die EINE
            // Kette EmissionsFaktorLader (Projektwert → aktive emissionswert-Zeile →
            // Tab_Brennstoff_Stamm → energy_carrier). Der Tooltip nennt die Ebene, aus
            // der der CO₂-Wert stammt; ohne sie wäre eine Zahl ohne Herkunft
            // ununterscheidbar von einer Katalog-Vorgabe.
            gridTraeger.Columns.Add("co2", Text_("BK_KOSTEN_SP_CO2", "CO₂ [g/kWh]"));
            gridTraeger.Columns.Add("so2", Text_("BK_KOSTEN_SP_SO2", "SO₂ [mg/kWh]"));
            gridTraeger.Columns.Add("nox", Text_("BK_KOSTEN_SP_NOX", "NOx [mg/kWh]"));

            // SECHS KÖPFE PASSEN EINZEILIG NICHT MEHR NEBENEINANDER. Gemessen bei 1040 px
            // Seitenbreite: 622 px stehen der Tabelle zur Verfügung, die sechs Köpfe
            // brauchen einzeilig zusammen 698 px. Ohne Umbruch kürzte der DataGridView
            // „Arbeitspreis [€/Einheit]" und „Arbeitspreis [€/kWh]" beide auf
            // „Arbeitspreis [€/…" — zwei Nachbarspalten mit demselben sichtbaren Kopf,
            // und die Vergleichsspalte wäre damit unlesbar geworden, um die es hier
            // gerade geht. Der Kopf bricht deshalb um und die Kopfzeile wächst mit.
            // (Die Werte selbst sind schmal: die breiteste Zelle misst 105 px.)
            gridTraeger.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.True;
            gridTraeger.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;

            // DIE GEWICHTE SIND MINDESTBREITEN, KEINE WUNSCHBREITEN. Sie stammen aus der
            // Messung bei 1040 px Seitenbreite: Träger 135 (breiteste Zelle
            // „Elektrische Energie“ = 105 px), Abrechnungseinheit 125 — EIN Wort, das nicht
            // umbrechen kann, also zählt die volle Kopfbreite von 119 px —, die vier
            // Preis-/Heizwertspalten 90/95/92/85 gegen ihre UMBROCHENEN Köpfe
            // (84/77/77/72 px) und ihre Zellen (höchstens 40 px).
            //
            // NEU AUSTARIERT AM 30.08.2026. Die Tabelle führt jetzt ZEHN Spalten: die
            // sechs von 2026-08-23, den Leistungspreis (KD6 § 10 — er lief bisher ohne
            // eigenes Gewicht und zog als Vorgabe 100 mehr Platz, als sein umbrochener
            // Kopf braucht) und die drei Emissionsspalten. Deren Köpfe sind kurz und
            // brechen sauber („CO₂“ / „[g/kWh]“), ihre Zellen tragen höchstens sechs
            // Zeichen — 62/70/70 genügen. Der Leistungspreis bekommt seine gemessenen
            // 105 statt der stillen 100.
            //
            // Summe 929 statt bisher 722. Bei 1040 px Seitenbreite ist das MEHR, als die
            // Tabelle hat; im Fill-Modus schrumpfen deshalb alle Spalten gleichmäßig, und
            // die Köpfe brechen in die zweite Zeile (ColumnHeadersHeightSizeMode =
            // AutoSize, s. o.). Das ist die bewusste Entscheidung: Zehn Spalten passen bei
            // dieser Fensterbreite nicht mehr in ihre Mindestbreiten, aber sie behalten
            // untereinander das RICHTIGE Verhältnis — und wer das Fenster größer zieht,
            // bekommt sofort die gemessenen Breiten. Kompensiert wird zusätzlich über die
            // Spaltenaufteilung der beiden Listen (pnlListen 30/70 statt 38/62): Die
            // Komponententabelle braucht für ihre drei Spalten 260 px, die Trägertabelle
            // für zehn ein Vielfaches davon.
            gridTraeger.Columns[0].FillWeight = 135;   // Träger
            gridTraeger.Columns[1].FillWeight = 125;   // Abrechnungseinheit
            gridTraeger.Columns[2].FillWeight = 90;    // Heizwert
            gridTraeger.Columns[3].FillWeight = 95;    // Arbeitspreis je Einheit
            gridTraeger.Columns[4].FillWeight = 92;    // Arbeitspreis je kWh
            gridTraeger.Columns[5].FillWeight = 85;    // Grundpreis
            gridTraeger.Columns[6].FillWeight = 105;   // Leistungspreis
            gridTraeger.Columns[7].FillWeight = 62;    // CO₂
            gridTraeger.Columns[8].FillWeight = 70;    // SO₂
            gridTraeger.Columns[9].FillWeight = 70;    // NOx
            for (int i = 2; i < gridTraeger.Columns.Count; i++)
                gridTraeger.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            // Die VERWENDUNGSMENGE — die eine Frage, die die gespeicherte Abfrage nicht
            // beantworten kann. Scheitert sie, bleibt die Menge leer; die Tabelle sagt
            // dann „kein Energieträger" statt eine ungefilterte Liste vorzuzeigen.
            var verwendet = new Dictionary<int, ProjektEnergietraegerCtrl.Verwendung>();
            try
            {
                foreach (ProjektEnergietraegerCtrl.Verwendung v in
                         ProjektEnergietraegerCtrl.Verwendete(_idProjekt))
                {
                    verwendet[v.CarrierId] = v;

                    // Verwendet, aber dem Projekt nicht zugeordnet: In
                    // Abfrage_Energietraeger_Effektiv gibt es für ihn keine Zeile, also
                    // auch keinen Preis und keinen Heizwert. Das gehört gesagt.
                    if (!v.Zugeordnet)
                        _traegerNichtZugeordnet.Add(v.Name.Length > 0
                            ? v.Name : "#" + v.CarrierId.ToString(kultur));
                }
            }
            catch { verwendet.Clear(); }

            // Der Berechnungsmodus des Projekts (F7) gilt für die CO₂-Spalte: Angezeigt
            // wird der Faktor, mit dem die Kennzahlen dieses Projekts WIRKLICH rechnen.
            string emissionsModus;
            try { emissionsModus = EmissionenCtrl.ModusFuerRechenlauf(_idProjekt); }
            catch { emissionsModus = DbWerte.EMISSION_MODUS_CO2; }

            // Die Träger, für die eine echte Zeile entstanden ist — die übrigen
            // verwendeten bekommen darunter ihre rote Fehlzeile.
            var angezeigt = new HashSet<int>();

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT carrier_id, name, billing_unit, eff_hi " +
                    "FROM Abfrage_Energietraeger_Effektiv WHERE ID_Projekt = ?",
                    new DbParam("@p", _idProjekt));

                foreach (DataRow r in (dt != null ? dt.Rows.Cast<DataRow>()
                                                  : Enumerable.Empty<DataRow>()))
                {
                    int carrier = (int)(D(r, "carrier_id") ?? 0);

                    // DER FILTER. Alles Weitere gilt nur für Träger, die im Projekt auch
                    // wirklich ein Gewerk fährt.
                    ProjektEnergietraegerCtrl.Verwendung v;
                    if (!verwendet.TryGetValue(carrier, out v)) continue;
                    angezeigt.Add(carrier);

                    double? preis, grund;
                    LiesPreise(carrier, out preis, out grund);
                    double? hi = D(r, "eff_hi");

                    // Weder Projekt- noch Katalogpreis gepflegt: der Träger kann keine
                    // Energiekosten erzeugen. Der Grund gehört in die Statuszeile, nicht nur
                    // die 0,0000 in die Tabelle. Ä-BK3: LiesPreise liefert dafür jetzt selbst
                    // null (Rechnerkette), die Preisspalte zeigt also „—" statt „0,0000";
                    // die Betragsprüfung bleibt als zweite Sicherung stehen.
                    bool ohnePreis = !preis.HasValue || Math.Abs(preis.Value) < 1e-9;
                    if (ohnePreis) _traegerOhnePreis.Add(S(r, "name"));

                    // Kein Heizwert = keine Umrechnung. Nur melden, wenn ein Preis da ist,
                    // den man überhaupt umrechnen wollte — sonst stünde derselbe Träger
                    // zweimal in der Fußzeile, einmal je fehlender Angabe.
                    bool ohneHeizwert = !hi.HasValue || hi.Value <= 0;
                    if (ohneHeizwert && !ohnePreis) _traegerOhneHeizwert.Add(S(r, "name"));

                    EmissionsFaktorSatz faktoren = EmissionsFaktoren(carrier);

                    int idx = gridTraeger.Rows.Add(
                        S(r, "name"),
                        S(r, "billing_unit"),
                        hi.HasValue ? hi.Value.ToString("N2", kultur) : "—",
                        preis.HasValue ? preis.Value.ToString("N4", kultur) : "—",
                        (ohnePreis || ohneHeizwert)
                            ? "—"
                            : (preis.Value / hi.Value).ToString("N4", kultur),
                        grund.HasValue ? grund.Value.ToString("N2", kultur) : "—",
                        LeistungspreisText(carrier, kultur),
                        Faktor(faktoren.Wirksam(emissionsModus), kultur),
                        Faktor(faktoren.So2, kultur),
                        Faktor(faktoren.Nox, kultur));
                    // Ä19: Schlüssel für die Anlagen-Auswahl (Träger kennzeichnen).
                    gridTraeger.Rows[idx].Tag = carrier;

                    // Warum steht diese Zeile hier? Der Filter bleibt nur dann
                    // nachvollziehbar, wenn er seine Begründung mitliefert.
                    gridTraeger.Rows[idx].Cells[0].ToolTipText = string.Format(
                        MyResource.Resource.BK_KOSTEN_TRAEGER_HINT, v.BeitraegerText);

                    // Die Herkunftsebene gehört an die Zahl: 240 g/kWh aus der
                    // Projektübersteuerung ist eine andere Aussage als 240 g/kWh aus
                    // dem Katalog — und nur die Ebene sagt, wo man sie ändert.
                    string herkunft = string.Format(
                        Text_("BK_KOSTEN_EMISSION_HINT",
                              "Emissionsfaktoren — CO₂ aus Ebene „{0}“, Berechnungsmodus {1}. " +
                              "Lesekette: Projektwert → aktiver Emissionswert → " +
                              "Brennstoff-Stamm → Trägerkatalog."),
                        faktoren.Co2Ebene, emissionsModus);
                    for (int c = 7; c <= 9; c++)
                        gridTraeger.Rows[idx].Cells[c].ToolTipText = herkunft;
                }
                gridTraeger.ClearSelection();
            }
            catch { }

            ZeigeFehlendeTraeger(verwendet, angezeigt, kultur);

            if (gridTraeger.Rows.Count == 0) ZeigeKeineTraeger(verwendet.Values);
        }

        /// <summary>
        /// <b>Die rote Fehlzeile je verwendetem, aber nicht angezeigtem Energieträger</b>
        /// (Anwenderentscheid 30.08.2026) — dasselbe Muster wie bei den Gewerken ohne
        /// Kostenposition in <see cref="LadeKomponenten"/>.
        ///
        /// <para><b>Warum eine ZEILE und nicht nur die Fußzeile.</b> Ein Träger, den das
        /// Projekt fährt, dem aber keine Projekteinstellung gegenübersteht, hat in
        /// <c>Abfrage_Energietraeger_Effektiv</c> keine Zeile — er fehlte in der Tabelle
        /// vollständig. Die graue Sammelfußzeile
        /// (<c>BK_KOSTEN_TRAEGER_FEHLT</c>) nannte ihn zwar, aber sie ist eine Zeile
        /// unter fünf anderen Hinweisen; in der Tabelle, in der man ihn sucht, war
        /// weiterhin nichts zu sehen. Die rote Zeile steht dort, wo der Träger fehlt,
        /// nennt die verursachenden Erzeuger im Tooltip und verweist auf den Knopf
        /// „Energieträgerverwaltung…“, über den die Zuordnung entsteht.</para>
        ///
        /// <para><b>Kriterium ist „nicht angezeigt“, nicht „nicht zugeordnet“.</b> Das
        /// ist der Obermenge-Fall: Auch ein zugeordneter Träger, zu dem die gespeicherte
        /// Abfrage nichts liefert, verschwände sonst wortlos. Die Fußzeile behält ihr
        /// eigenes, engeres Kriterium (<see cref="_traegerNichtZugeordnet"/>) — sie sagt
        /// etwas anderes, nämlich WARUM Preis und Heizwert fehlen.</para>
        /// </summary>
        private void ZeigeFehlendeTraeger(
            Dictionary<int, ProjektEnergietraegerCtrl.Verwendung> verwendet,
            HashSet<int> angezeigt, System.Globalization.CultureInfo kultur)
        {
            var fehlend = new List<ProjektEnergietraegerCtrl.Verwendung>();
            foreach (ProjektEnergietraegerCtrl.Verwendung v in verwendet.Values)
                if (!angezeigt.Contains(v.CarrierId)) fehlend.Add(v);
            if (fehlend.Count == 0) return;

            fehlend.Sort(delegate (ProjektEnergietraegerCtrl.Verwendung a,
                                   ProjektEnergietraegerCtrl.Verwendung b)
                         { return a.CarrierId.CompareTo(b.CarrierId); });

            foreach (ProjektEnergietraegerCtrl.Verwendung v in fehlend)
            {
                string name = v.Name.Length > 0 ? v.Name : "#" + v.CarrierId.ToString(kultur);
                int idx = gridTraeger.Rows.Add(
                    string.Format(Text_("BK_KOSTEN_TRAEGER_FEHLZEILE", "{0} — nicht zugeordnet"),
                                  name),
                    "—", "—", "—", "—", "—", "—", "—", "—", "—");
                gridTraeger.Rows[idx].DefaultCellStyle.BackColor =
                    Color.FromArgb(0xFF, 0xE6, 0xE6);
                gridTraeger.Rows[idx].Tag = v.CarrierId;

                string hinweis = string.Format(
                    Text_("BK_KOSTEN_TRAEGER_FEHLZEILE_HINT",
                          "„{0}“ wird von {1} verwendet, ist dem Projekt aber nicht " +
                          "zugeordnet — ohne Zuordnung gibt es weder Preis noch Heizwert " +
                          "noch Emissionsfaktoren, und die Energiekosten bleiben „—“. " +
                          "Zuordnen über den Knopf „{2}“ oben rechts."),
                    name, v.BeitraegerText,
                    Text_("BK_KOSTEN_BTN_TRAEGER", "Energieträgerverwaltung…"));
                foreach (DataGridViewCell c in gridTraeger.Rows[idx].Cells)
                    c.ToolTipText = hinweis;
            }
            gridTraeger.ClearSelection();
        }

        /// <summary>
        /// Der Faktorsatz eines Trägers über die EINE Lesekette
        /// (<see cref="EmissionsFaktorLader"/>) — dieselbe Quelle, aus der
        /// <see cref="KostenEmissionRechner"/> und <see cref="EmissionsBilanzRechner"/>
        /// rechnen. Eine eigene Abfrage auf <c>energy_project_settings.co2</c> wäre eine
        /// zweite Wahrheit: Sie sähe weder die aktive <c>emissionswert</c>-Zeile noch den
        /// Brennstoff-Stamm und zeigte damit Zahlen, mit denen niemand rechnet.
        /// </summary>
        private EmissionsFaktorSatz EmissionsFaktoren(int carrierId)
        {
            try { return EmissionsFaktorLader.Lade(_idProjekt, carrierId); }
            catch { return new EmissionsFaktorSatz(); }
        }

        /// <summary>Emissionsfaktor als Zellentext; „—“ = nicht gepflegt (dieselbe
        /// Lesart wie beim Arbeitspreis: eine 0 wäre eine Aussage, die niemand
        /// getroffen hat).</summary>
        private static string Faktor(double? wert, System.Globalization.CultureInfo kultur)
        {
            return wert.HasValue ? wert.Value.ToString("N2", kultur) : "—";
        }

        /// <summary>
        /// Eine erklärende Zeile statt eines leeren Rasters. Ein leeres Raster sagt nicht,
        /// OB gefiltert wurde — dieselbe Haltung wie bei den Gewerken ohne Kostenposition.
        /// </summary>
        /// <param name="verwendet">
        /// Die Träger, die das Projekt fährt. Leer = das Projekt bezieht überhaupt keine
        /// Energie, weil es keinen entsprechenden Erzeuger führt. Nicht leer und trotzdem
        /// keine Zeile = zu keinem dieser Träger liefert
        /// <c>Abfrage_Energietraeger_Effektiv</c> etwas; dann fehlen Preis und Heizwert,
        /// und die Zeile nennt die Träger, um die es geht.
        /// </param>
        private void ZeigeKeineTraeger(ICollection<ProjektEnergietraegerCtrl.Verwendung> verwendet)
        {
            var namen = new List<string>();
            foreach (ProjektEnergietraegerCtrl.Verwendung v in verwendet)
                namen.Add(v.Name.Length > 0 ? v.Name : "#" + v.CarrierId);

            string text = namen.Count > 0
                ? string.Format(MyResource.Resource.BK_KOSTEN_TRAEGER_UNGEPFLEGT,
                                string.Join(", ", namen.ToArray()))
                : MyResource.Resource.BK_KOSTEN_TRAEGER_KEINE;

            int idx = gridTraeger.Rows.Add(text, "", "", "", "", "", "", "", "", "");
            gridTraeger.Rows[idx].DefaultCellStyle.ForeColor = Color.DimGray;
            gridTraeger.Rows[idx].DefaultCellStyle.Font =
                new Font(gridTraeger.Font, FontStyle.Italic);
            gridTraeger.Rows[idx].Cells[0].ToolTipText = text;
            gridTraeger.ClearSelection();
        }

        // Vorrangkette des KostenEmissionRechners: Projektwert (energy_project_settings)
        // schlägt Katalogwert (energy_carrier).
        /// <summary>
        /// KD6 (§ 10): der effektive Leistungspreis eines Trägers als JAHRESWERT
        /// [€/(kW·a)] — custom_price_power vor price_power (0 = nicht gepflegt,
        /// Befund-D5-Regel), Monatsmodus × 12. Dieselbe Vorrangkette wie
        /// KostenEmissionRechner.LadeTraeger. Ä18: Der frühere Strom-Kurzschluss
        /// („—", Tarifwelt) ist entfallen — der Stromträger pflegt seinen
        /// Flat-Leistungspreis seit Migrationsschritt 44 wie jeder andere Träger,
        /// und die Spalte zeigt ihn; die Tarifstruktur bleibt das Detailmodell
        /// und ersetzt die Flat-Preise nur, wenn sie aktiv ist.
        /// </summary>
        private string LeistungspreisText(int carrierId, System.Globalization.CultureInfo kultur)
        {
            try
            {
                DataTable k = DataRepository.GetDataTable(
                    "SELECT price_power, price_power_modus, pricing_model " +
                    "FROM energy_carrier WHERE id = ?",
                    new DbParam("@c", carrierId));
                if (k == null || k.Rows.Count == 0) return "—";

                double? satz = null;
                DataTable s = DataRepository.GetDataTable(
                    "SELECT custom_price_power FROM energy_project_settings " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new DbParam("@p", _idProjekt), new DbParam("@c", carrierId));
                if (s != null && s.Rows.Count > 0)
                {
                    double? cw = D(s.Rows[0], "custom_price_power");
                    if (cw.HasValue && cw.Value > 0) satz = cw;
                }
                if (!satz.HasValue)
                {
                    double? kw = D(k.Rows[0], "price_power");
                    if (kw.HasValue && kw.Value > 0) satz = kw;
                }
                if (!satz.HasValue) return "—";

                bool monat = string.Equals(S(k.Rows[0], "price_power_modus"),
                    DbWerte.LEISTUNGSPREIS_MODUS_MONAT, StringComparison.Ordinal);
                return (monat ? satz.Value * 12.0 : satz.Value).ToString("N2", kultur);
            }
            catch { return "—"; }
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            try
            {
                string t = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(t) ? rueckfall : t;
            }
            catch { return rueckfall; }
        }

        /// <summary>
        /// Arbeits- und Grundpreis eines Trägers — <b>dieselbe Vorrangkette wie
        /// <c>KostenEmissionRechner.LadeTraeger</c></b>, damit Tabelle, Rechner und
        /// Fußzeile denselben Träger gleich beurteilen (Ä-BK3).
        ///
        /// <para><b>Arbeitspreis: 0 zählt als NICHT GEPFLEGT</b> (Befund D5) —
        /// Projektwert nur, wenn &gt; 0, sonst Katalogwert nur, wenn &gt; 0, sonst
        /// gar keiner. Vorher kam die 0 als gültiger Preis durch und stand als
        /// „0,0000" in der Spalte, während Rechner und Fußzeile denselben Träger
        /// bereits als preislos führten; die Zelle zeigt jetzt „—".</para>
        ///
        /// <para><b>Der Grundpreis bleibt bei „Projektwert vor Katalogwert"</b>,
        /// die 0 ausdrücklich eingeschlossen: 0 €/a ist dort ein üblicher und
        /// gültiger Vertragswert (Abgrenzung wie im Rechner).</para>
        /// </summary>
        private void LiesPreise(int carrierId, out double? arbeit, out double? grund)
        {
            arbeit = null; grund = null;
            if (carrierId <= 0) return;

            double? sArbeit = null, sGrund = null;
            try
            {
                DataTable s = DataRepository.GetDataTable(
                    "SELECT custom_price_work, custom_price_base FROM energy_project_settings " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new DbParam("@p", _idProjekt), new DbParam("@c", carrierId));
                if (s != null && s.Rows.Count > 0)
                {
                    sArbeit = D(s.Rows[0], "custom_price_work");
                    sGrund = D(s.Rows[0], "custom_price_base");
                }
            }
            catch { }

            double? kArbeit = null, kGrund = null;
            try
            {
                DataTable k = DataRepository.GetDataTable(
                    "SELECT price_work, price_base FROM energy_carrier WHERE id = ?",
                    new DbParam("@c", carrierId));
                if (k != null && k.Rows.Count > 0)
                {
                    kArbeit = D(k.Rows[0], "price_work");
                    kGrund = D(k.Rows[0], "price_base");
                }
            }
            catch { }

            arbeit = (sArbeit.HasValue && sArbeit.Value > 0) ? sArbeit
                   : ((kArbeit.HasValue && kArbeit.Value > 0) ? kArbeit : null);
            grund = sGrund ?? kGrund;
        }

        // ------------------------------------------------------------- Aktionen

        private readonly Button btnTraeger = new Button();

        private void btnVerwaltung_Click(object sender, EventArgs e)
        {
            if (_idProjekt <= 0) return;
            Form f = this.FindForm();
            // KD6a (§ 3.2): Der Einstieg führt in den NEUEN Kostendialog im
            // Projektmodus — der alte Kosteneditor ist mit iU9-W0 entfallen.
            // Ä19: vorgewählt wird die Komponente der GEWÄHLTEN Anlagenzeile —
            // in einer Heizkessel-Variante öffnet der Dialog damit den Kessel,
            // nicht mehr die erste Komponente der Katalogreihenfolge.
            var aw = gridKomponenten.CurrentRow != null
                ? gridKomponenten.CurrentRow.Tag as ProjektEnergietraegerCtrl.AnlagenEintrag
                : null;
            KostenKomponenteHuelle.OeffnenProjekt(f, _idProjekt, _projektname,
                                                  aw != null ? aw.Komponente : null,
                                                  false, aw != null ? aw.AnlageId : 0);
            Aktualisiere();   // Kompaktwerte nach der Pflege auffrischen
        }

        /// <summary>KD6a: direkter Einstieg in die Energieträgerverwaltung im
        /// Projektkontext — dieselbe Pflege wie über Administration, vorgefiltert
        /// auf das Projekt.</summary>
        private void btnTraeger_Click(object sender, EventArgs e)
        {
            if (_idProjekt <= 0) return;
            Form f = this.FindForm();
            using (var dlg = new Form_Energietraeger())
            {
                dlg.SetControls(_idProjekt);
                if (f != null) dlg.ShowDialog(f); else dlg.ShowDialog();
            }
            Aktualisiere();
        }

        // -------------------------------------------------------------- Helfer

        private static string S(DataRow r, string col)
        { return (r.Table.Columns.Contains(col) && r[col] != DBNull.Value) ? r[col].ToString() : ""; }

        private static double? D(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col) || r[col] == DBNull.Value) return null;
            try { return Convert.ToDouble(r[col]); } catch { return null; }
        }

        private void Melde(string text)
        { if (lblStatus != null) lblStatus.Text = text ?? ""; }

        /// <summary>Angezeigte Kompaktwerte (Prüfhilfen für den Headless-Harnisch).</summary>
        public string WertInvestition { get { return kInvest.Wert; } }
        public string WertBetrieb { get { return kBetrieb.Wert; } }
        public string WertEnergie { get { return kEnergie.Wert; } }
        public int KomponentenZeilen { get { return gridKomponenten.Rows.Count; } }
        public int TraegerZeilen { get { return gridTraeger.Rows.Count; } }
        public string StatusText { get { return lblStatus != null ? lblStatus.Text : ""; } }
        public int GewerkeOhnePosition { get { return _ohnePosition.Count; } }
        public int TraegerOhnePreis { get { return _traegerOhnePreis.Count; } }
        public int TraegerNichtZugeordnet { get { return _traegerNichtZugeordnet.Count; } }
        public int TraegerOhneHeizwert { get { return _traegerOhneHeizwert.Count; } }

        /// <summary>Prüfhilfe: eine Trägerzeile als „Zelle | Zelle | …“ samt Kennzeichnung
        /// der roten Fehlzeile — der Headless-Harnisch kann die Tabelle nicht ansehen,
        /// aber ihren Inhalt lesen.</summary>
        public string TraegerZeile(int zeile)
        {
            if (zeile < 0 || zeile >= gridTraeger.Rows.Count) return "";
            var teile = new List<string>();
            foreach (DataGridViewCell c in gridTraeger.Rows[zeile].Cells)
                teile.Add(c.Value == null ? "" : c.Value.ToString());
            bool rot = gridTraeger.Rows[zeile].DefaultCellStyle.BackColor ==
                       Color.FromArgb(0xFF, 0xE6, 0xE6);
            return (rot ? "[ROT] " : "[   ] ") + string.Join(" | ", teile.ToArray());
        }

        /// <summary>Prüfhilfe: der Tooltip einer Trägerzelle (Herkunft, Verursacher).</summary>
        public string TraegerTooltip(int zeile, int spalte)
        {
            if (zeile < 0 || zeile >= gridTraeger.Rows.Count) return "";
            if (spalte < 0 || spalte >= gridTraeger.Columns.Count) return "";
            return gridTraeger.Rows[zeile].Cells[spalte].ToolTipText ?? "";
        }

        /// <summary>Prüfhilfe: Anzahl der Spalten der Trägertabelle.</summary>
        public int TraegerSpalten { get { return gridTraeger.Columns.Count; } }

        // ------------------------------------------------------ Kategorie-Kachel

        /// <summary>
        /// Eine Kategorie-Kachel: Überschrift, großer Wert, kleine Herkunftszeile.
        /// Bewusst schlicht gehalten (Rahmen + Flächenfarbe des Hausstils).
        /// </summary>
        // KD6a: internal — die Wirtschaftlichkeitsseite nutzt DIESELBE Karte
        // (eine Gestaltungs-Wahrheit statt einer Kopie).
        internal class Kachel : TableLayoutPanel
        {
            private readonly Label _titel = new Label();
            private readonly Label _wert = new Label();
            private readonly Label _quelle = new Label();

            public Kachel()
            {
                Dock = DockStyle.Fill;
                Margin = new Padding(0, 0, 8, 0);
                Padding = new Padding(10, 6, 10, 6);
                BackColor = Color.FromArgb(0xF4, 0xF6, 0xFA);
                BorderStyle = BorderStyle.FixedSingle;
                ColumnCount = 1;
                ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
                RowCount = 3;
                RowStyles.Add(new RowStyle(SizeType.Absolute, 18f));
                RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
                RowStyles.Add(new RowStyle(SizeType.Absolute, 28f));

                _titel.Dock = DockStyle.Fill;
                _titel.Margin = new Padding(0);
                _titel.ForeColor = Color.FromArgb(0x33, 0x33, 0x33);
                _titel.Font = new Font("Segoe UI", 9f, FontStyle.Bold);

                _wert.Dock = DockStyle.Fill;
                _wert.Margin = new Padding(0);
                _wert.Font = new Font("Segoe UI", 14f, FontStyle.Regular);
                _wert.ForeColor = Color.FromArgb(0x00, 0x73, 0xAA);
                _wert.TextAlign = ContentAlignment.MiddleLeft;
                _wert.Text = "—";

                _quelle.Dock = DockStyle.Fill;
                _quelle.Margin = new Padding(0);
                _quelle.ForeColor = Color.DimGray;

                Controls.Add(_titel, 0, 0);
                Controls.Add(_wert, 0, 1);
                Controls.Add(_quelle, 0, 2);
            }

            public void Setze(string titel, string quelle)
            { _titel.Text = titel; _quelle.Text = quelle; }

            /// <summary>
            /// Die Herkunftszeile, nachträglich änderbar (ETAPPE K5). Sie trägt bei der
            /// Investitionskachel den Zuschussausweis — eine vierte Kachel wäre die
            /// falsche Form: Der Zuschuss ist keine eigene Kostenkategorie, sondern die
            /// Minderung genau dieser einen. Das Kachelraster ist zudem auf drei Spalten
            /// festgelegt (<c>pnlKacheln.ColumnCount</c>).
            /// </summary>
            public string Quelle
            {
                get { return _quelle.Text; }
                set { _quelle.Text = value ?? ""; }
            }

            public string Wert
            {
                get { return _wert.Text; }
                set { _wert.Text = value ?? "—"; }
            }
        }
    }
}
