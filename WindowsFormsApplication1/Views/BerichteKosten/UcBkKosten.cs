using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
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
    /// Darunter die beiden Detaillisten, die auch die Kostenverwaltung speisen:
    /// Investitionssummen je Komponente (<see cref="Form_Kosten.LiesKomponentenSummen"/>,
    /// Kategorie 1 — dieselbe Leselogik wie in <see cref="Form_Kosten"/>)
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
            this.pnlListen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38f));
            this.pnlListen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62f));
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
                DataTable bt = Form_Kosten.LiesKomponentenSummen(
                    _idProjekt, Form_Kosten.KATEGORIE_BETRIEB);
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

        // Investitionssummen je Komponente — dieselbe Leselogik, die auch die
        // Kostenverwaltung für ihre Gesamtsumme verwendet (Form_Kosten.LiesKomponentenSummen).
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

            try
            {
                // Summen je Komponente — dieselbe Leselogik wie bisher (KD6 § 10).
                var investJe = new Dictionary<string, double>(StringComparer.Ordinal);
                var betriebJe = new Dictionary<string, double>(StringComparer.Ordinal);
                try
                {
                    DataTable it = Form_Kosten.LiesKomponentenSummen(
                        _idProjekt, Form_Kosten.KATEGORIE_INVESTITION);
                    if (it != null)
                        foreach (DataRow r2 in it.Rows)
                        {
                            double? w2 = D(r2, "Summe");
                            if (w2.HasValue) investJe[S(r2, "Komponente")] = w2.Value;
                        }
                    DataTable bt2 = Form_Kosten.LiesKomponentenSummen(
                        _idProjekt, Form_Kosten.KATEGORIE_BETRIEB);
                    if (bt2 != null)
                        foreach (DataRow r2 in bt2.Rows)
                        {
                            double? w2 = D(r2, "Summe");
                            if (w2.HasValue) betriebJe[S(r2, "Komponente")] = w2.Value;
                        }
                }
                catch { }

                List<ProjektEnergietraegerCtrl.AnlagenEintrag> anlagen =
                    ProjektEnergietraegerCtrl.AnlagenMitTraeger(_idProjekt);

                double summe = 0, summeBetrieb = 0;
                var ausgewiesen = new HashSet<string>(StringComparer.Ordinal);
                foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in anlagen)
                {
                    bool erste = !ausgewiesen.Contains(a.Komponente);
                    double invest, bWert;
                    bool hatI = investJe.TryGetValue(a.Komponente, out invest);
                    bool hatB = betriebJe.TryGetValue(a.Komponente, out bWert);

                    string name = string.IsNullOrEmpty(a.Bezeichner)
                        ? a.Komponente
                        : a.Komponente + " — " + a.Bezeichner;

                    int idx = gridKomponenten.Rows.Add(
                        name,
                        (erste && hatI) ? invest.ToString("N2", kultur) : "—",
                        (erste && hatB) ? bWert.ToString("N2", kultur) : "—");
                    gridKomponenten.Rows[idx].Tag = a;

                    if (erste)
                    {
                        ausgewiesen.Add(a.Komponente);
                        if (hatI) summe += invest;
                        if (hatB) summeBetrieb += bWert;
                        if (!hatI && !hatB)
                        {
                            // Verbaut ohne Kostenposition: FEHLENDE EINGABE, kein
                            // Nullbetrag (Nutzerentscheidung 4 vom 18.08.2026).
                            gridKomponenten.Rows[idx].DefaultCellStyle.BackColor =
                                Color.FromArgb(0xFF, 0xE6, 0xE6);
                            string hinweis = string.Format(
                                MyResource.Resource.BK_KOSTEN_OHNE_POSITION_HINT, a.Komponente);
                            foreach (DataGridViewCell c in gridKomponenten.Rows[idx].Cells)
                                c.ToolTipText = hinweis;
                            if (!_ohnePosition.Contains(a.Komponente))
                                _ohnePosition.Add(a.Komponente);
                        }
                    }
                    else
                    {
                        string hinweis = Text_("BK_KOSTEN_JE_KOMPONENTE",
                            "Kosten werden je Komponente gepflegt — die Summe steht an der ersten Anlage der Komponente.");
                        foreach (DataGridViewCell c in gridKomponenten.Rows[idx].Cells)
                            c.ToolTipText = hinweis;
                    }
                }

                // Ä19: Kostenpositionen OHNE verbaute Anlage (z. B. aus der
                // Variantenkopie eines anderen Gewerks). Sie RECHNEN in Kachel und
                // Wirtschaftlichkeit mit — sie zu verschweigen wäre eine stille
                // Falschausweisung (Kachel ≠ Tabelle). Gelbe Warnzeile statt
                // normaler Zeile; die Fußzeile nennt sie zusätzlich.
                var reste = new List<string>();
                foreach (string k in investJe.Keys) if (!ausgewiesen.Contains(k)) reste.Add(k);
                foreach (string k in betriebJe.Keys)
                    if (!ausgewiesen.Contains(k) && !reste.Contains(k)) reste.Add(k);
                foreach (string k in reste)
                {
                    ausgewiesen.Add(k);
                    double invest, bWert;
                    bool hatI = investJe.TryGetValue(k, out invest);
                    bool hatB = betriebJe.TryGetValue(k, out bWert);
                    if (hatI) summe += invest;
                    if (hatB) summeBetrieb += bWert;

                    int idx = gridKomponenten.Rows.Add(
                        string.Format(Text_("BK_KOSTEN_NICHT_VERBAUT", "{0} — nicht verbaut"), k),
                        hatI ? invest.ToString("N2", kultur) : "—",
                        hatB ? bWert.ToString("N2", kultur) : "—");
                    gridKomponenten.Rows[idx].DefaultCellStyle.BackColor =
                        Color.FromArgb(0xFF, 0xF4, 0xCC);
                    string hinweis = Text_("BK_KOSTEN_NICHT_VERBAUT_HINT",
                        "Kostenpositionen ohne verbaute Anlage — sie rechnen in der Wirtschaftlichkeit mit; bitte prüfen oder in der Kostenverwaltung löschen.");
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

            // Die Gewichte sind die bei 1040 px GEMESSENEN Mindestbreiten, nicht geraten:
            // Träger 135 (breiteste Zelle „Elektrische Energie“ = 105 px),
            // Abrechnungseinheit 125 — EIN Wort, das nicht umbrechen kann, also zählt hier
            // die volle Kopfbreite von 119 px; mit den bisherigen 103 px stand sie schon
            // vor dieser Änderung gekürzt da,
            // die vier Zahlenspalten 90/95/92/85 gegen ihre UMBROCHENEN Köpfe (84/77/77/72 px)
            // und ihre Zellen (höchstens 40 px). Danach ist kein Kopf mehr gekürzt.
            gridTraeger.Columns[0].FillWeight = 135;
            gridTraeger.Columns[1].FillWeight = 125;
            gridTraeger.Columns[2].FillWeight = 90;
            gridTraeger.Columns[3].FillWeight = 95;
            gridTraeger.Columns[4].FillWeight = 92;
            gridTraeger.Columns[5].FillWeight = 85;
            for (int i = 2; i <= 5; i++)
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

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT carrier_id, name, billing_unit, eff_hi " +
                    "FROM Abfrage_Energietraeger_Effektiv WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", _idProjekt));

                foreach (DataRow r in (dt != null ? dt.Rows.Cast<DataRow>()
                                                  : Enumerable.Empty<DataRow>()))
                {
                    int carrier = (int)(D(r, "carrier_id") ?? 0);

                    // DER FILTER. Alles Weitere gilt nur für Träger, die im Projekt auch
                    // wirklich ein Gewerk fährt.
                    ProjektEnergietraegerCtrl.Verwendung v;
                    if (!verwendet.TryGetValue(carrier, out v)) continue;

                    double? preis, grund;
                    LiesPreise(carrier, out preis, out grund);
                    double? hi = D(r, "eff_hi");

                    // Weder Projekt- noch Katalogpreis gepflegt: der Träger kann keine
                    // Energiekosten erzeugen. Der Grund gehört in die Statuszeile, nicht nur
                    // die 0,0000 in die Tabelle.
                    bool ohnePreis = !preis.HasValue || Math.Abs(preis.Value) < 1e-9;
                    if (ohnePreis) _traegerOhnePreis.Add(S(r, "name"));

                    // Kein Heizwert = keine Umrechnung. Nur melden, wenn ein Preis da ist,
                    // den man überhaupt umrechnen wollte — sonst stünde derselbe Träger
                    // zweimal in der Fußzeile, einmal je fehlender Angabe.
                    bool ohneHeizwert = !hi.HasValue || hi.Value <= 0;
                    if (ohneHeizwert && !ohnePreis) _traegerOhneHeizwert.Add(S(r, "name"));

                    int idx = gridTraeger.Rows.Add(
                        S(r, "name"),
                        S(r, "billing_unit"),
                        hi.HasValue ? hi.Value.ToString("N2", kultur) : "—",
                        preis.HasValue ? preis.Value.ToString("N4", kultur) : "—",
                        (ohnePreis || ohneHeizwert)
                            ? "—"
                            : (preis.Value / hi.Value).ToString("N4", kultur),
                        grund.HasValue ? grund.Value.ToString("N2", kultur) : "—",
                        LeistungspreisText(carrier, kultur));
                    // Ä19: Schlüssel für die Anlagen-Auswahl (Träger kennzeichnen).
                    gridTraeger.Rows[idx].Tag = carrier;

                    // Warum steht diese Zeile hier? Der Filter bleibt nur dann
                    // nachvollziehbar, wenn er seine Begründung mitliefert.
                    gridTraeger.Rows[idx].Cells[0].ToolTipText = string.Format(
                        MyResource.Resource.BK_KOSTEN_TRAEGER_HINT, v.BeitraegerText);
                }
                gridTraeger.ClearSelection();
            }
            catch { }

            if (gridTraeger.Rows.Count == 0) ZeigeKeineTraeger(verwendet.Values);
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

            int idx = gridTraeger.Rows.Add(text, "", "", "", "", "");
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
                    new OleDbParameter("@c", carrierId));
                if (k == null || k.Rows.Count == 0) return "—";

                double? satz = null;
                DataTable s = DataRepository.GetDataTable(
                    "SELECT custom_price_power FROM energy_project_settings " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new OleDbParameter("@p", _idProjekt), new OleDbParameter("@c", carrierId));
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

        private void LiesPreise(int carrierId, out double? arbeit, out double? grund)
        {
            arbeit = null; grund = null;
            if (carrierId <= 0) return;

            try
            {
                DataTable s = DataRepository.GetDataTable(
                    "SELECT custom_price_work, custom_price_base FROM energy_project_settings " +
                    "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                    new OleDbParameter("@p", _idProjekt), new OleDbParameter("@c", carrierId));
                if (s != null && s.Rows.Count > 0)
                {
                    arbeit = D(s.Rows[0], "custom_price_work");
                    grund = D(s.Rows[0], "custom_price_base");
                }
            }
            catch { }

            if (arbeit.HasValue && grund.HasValue) return;

            try
            {
                DataTable k = DataRepository.GetDataTable(
                    "SELECT price_work, price_base FROM energy_carrier WHERE id = ?",
                    new OleDbParameter("@c", carrierId));
                if (k != null && k.Rows.Count > 0)
                {
                    if (!arbeit.HasValue) arbeit = D(k.Rows[0], "price_work");
                    if (!grund.HasValue) grund = D(k.Rows[0], "price_base");
                }
            }
            catch { }
        }

        // ------------------------------------------------------------- Aktionen

        private readonly Button btnTraeger = new Button();

        private void btnVerwaltung_Click(object sender, EventArgs e)
        {
            if (_idProjekt <= 0) return;
            Form f = this.FindForm();
            // KD6a (§ 3.2): Der Einstieg führt in den NEUEN Kostendialog im
            // Projektmodus — der alte Editor Form_Kosten ist kein Einstieg mehr.
            using (var dlg = new Form_KostenKomponente())
            {
                // Ä19: vorgewählt wird die Komponente der GEWÄHLTEN Anlagenzeile —
                // in einer Heizkessel-Variante öffnet der Dialog damit den Kessel,
                // nicht mehr die erste Komponente der Katalogreihenfolge.
                var aw = gridKomponenten.CurrentRow != null
                    ? gridKomponenten.CurrentRow.Tag as ProjektEnergietraegerCtrl.AnlagenEintrag
                    : null;
                dlg.SetProjekt(_idProjekt, _projektname, aw != null ? aw.Komponente : null);
                if (f != null) dlg.ShowDialog(f); else dlg.ShowDialog();
            }
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
