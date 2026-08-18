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
        /// Zahl der Komponenten, deren erfasste Investitionsposition zu keinem
        /// Technik-Planwert passt — gefüllt in <see cref="LadeKomponenten"/>, gemeldet in
        /// der Statuszeile.
        /// </summary>
        private int _abweichungen = 0;

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

            this.btnVerwaltung.Dock = DockStyle.Fill;
            this.btnVerwaltung.Margin = new Padding(12, 0, 0, 0);
            this.btnVerwaltung.AutoSize = true;
            this.btnVerwaltung.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            this.btnVerwaltung.Text = MyResource.Resource.BK_KOSTEN_BTN_VERWALTUNG;
            this.btnVerwaltung.Click += new EventHandler(this.btnVerwaltung_Click);

            this.pnlKopf.Dock = DockStyle.Fill;
            this.pnlKopf.ColumnCount = 2;
            this.pnlKopf.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            this.pnlKopf.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            this.pnlKopf.RowCount = 1;
            this.pnlKopf.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            this.pnlKopf.Margin = new Padding(0, 0, 0, 8);
            this.pnlKopf.Controls.Add(this.lblProjekt, 0, 0);
            this.pnlKopf.Controls.Add(this.btnVerwaltung, 1, 0);

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

            this.lblTraeger.Dock = DockStyle.Fill;
            this.lblTraeger.Margin = new Padding(8, 0, 0, 0);
            this.lblTraeger.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            this.lblTraeger.Text = MyResource.Resource.BK_KOSTEN_LBL_TRAEGER;

            BereiteGridVor(this.gridTraeger);
            this.gridTraeger.Margin = new Padding(8, 0, 0, 0);

            this.pnlListen.Dock = DockStyle.Fill;
            this.pnlListen.ColumnCount = 2;
            this.pnlListen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42f));
            this.pnlListen.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58f));
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
                Melde("");
                return;
            }

            btnVerwaltung.Enabled = true;
            lblProjekt.Text = string.Format(MyResource.Resource.BK_KOSTEN_PROJEKT, _projektname);

            var kultur = BerichtTexte.Kultur;

            // --- Kategorie 1: Investition (gleiche Leselogik wie die Kapitalwertrechnung) ---
            double invest = 0;
            int investPositionen = 0;
            try
            {
                var positionen = WirtschaftlichkeitCtrl.LiesInvestitionen(
                    _idProjekt, WirtschaftlichkeitSzenario.ERWARTET);
                investPositionen = positionen.Count;
                foreach (KapitalwertRechner.InvestPosition p in positionen) invest += p.Betrag;
            }
            catch { }
            kInvest.Wert = invest.ToString("N2", kultur) + " " + MyResource.Resource.BK_KOSTEN_EINHEIT_EUR;

            // --- Kategorie 2: Betrieb ---
            double betrieb = 0;
            try
            {
                betrieb = WirtschaftlichkeitCtrl.LiesBetriebskosten(
                    _idProjekt, WirtschaftlichkeitSzenario.ERWARTET);
            }
            catch { }
            kBetrieb.Wert = betrieb.ToString("N2", kultur) + " " + MyResource.Resource.BK_KOSTEN_EINHEIT_EUR_A;

            // --- Energie: zuletzt gespeicherter Wert der Wirtschaftlichkeitsrechnung ---
            string energieHinweis = "";
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
                    energieHinweis = string.Format(MyResource.Resource.BK_KOSTEN_STAND,
                        erg.Zeitstempel.ToString("dd.MM.yyyy HH:mm"));
                }
            }
            catch { kEnergie.Wert = "—"; }

            LadeKomponenten(kultur);
            LadeTraeger(kultur);

            string status = string.Format(MyResource.Resource.BK_KOSTEN_STATUS,
                                          investPositionen, energieHinweis).Trim();
            if (_abweichungen > 0)
                status += "  ·  " + string.Format(MyResource.Resource.BK_KOSTEN_ABWEICHUNG, _abweichungen);
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
        private void LadeKomponenten(System.Globalization.CultureInfo kultur)
        {
            _abweichungen = 0;

            gridKomponenten.Columns.Add("komponente", MyResource.Resource.BK_KOSTEN_SP_KOMPONENTE);
            gridKomponenten.Columns.Add("summe", MyResource.Resource.BK_KOSTEN_SP_SUMME);
            gridKomponenten.Columns.Add("technik", MyResource.Resource.BK_KOSTEN_SP_TECHNIK);
            gridKomponenten.Columns[0].FillWeight = 120;
            gridKomponenten.Columns[1].FillWeight = 70;
            gridKomponenten.Columns[2].FillWeight = 70;
            gridKomponenten.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            gridKomponenten.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            try
            {
                DataTable dt = Form_Kosten.LiesKomponentenSummen(
                    _idProjekt, Form_Kosten.KATEGORIE_INVESTITION);
                if (dt == null) return;

                double summe = 0;
                foreach (DataRow r in dt.Rows)
                {
                    string komponente = S(r, "Komponente");
                    double? w = D(r, "Summe");
                    summe += w ?? 0;

                    // Technik-Planwert daneben, Abweichungen markiert — angezeigt, nie
                    // überschrieben (Nutzerentscheidung 4 vom 18.08.2026). Angeglichen
                    // wird ausschließlich in der Kostenverwaltung über
                    // „Planwert übernehmen…".
                    KostenPositionCtrl.Abweichung ab = Abweichung(komponente);

                    int idxZeile = gridKomponenten.Rows.Add(
                        komponente,
                        w.HasValue ? w.Value.ToString("N2", kultur) : "—",
                        (ab != null && ab.TechnikVorhanden) ? ab.Technik.ToString("N2", kultur) : "—");

                    if (ab != null && ab.Abweichend)
                    {
                        _abweichungen++;
                        gridKomponenten.Rows[idxZeile].DefaultCellStyle.BackColor =
                            Color.FromArgb(0xFF, 0xF4, 0xD9);
                        gridKomponenten.Rows[idxZeile].Cells[2].ToolTipText = ab.Text;
                    }
                }
                if (dt.Rows.Count > 0)
                {
                    int idx = gridKomponenten.Rows.Add(MyResource.Resource.BK_KOSTEN_SUMME,
                                                       summe.ToString("N2", kultur), "");
                    gridKomponenten.Rows[idx].DefaultCellStyle.Font =
                        new Font(gridKomponenten.Font, FontStyle.Bold);
                }
                gridKomponenten.ClearSelection();
            }
            catch { }
        }

        /// <summary>
        /// Abweichung einer Komponente zwischen erfasster Position und Technik-Planwert —
        /// dieselbe Prüfung, die auch die Kostenverwaltung anzeigt
        /// (<see cref="KostenPositionCtrl.Pruefe"/>). <c>null</c>, wenn die Komponente
        /// nicht zur Landkarte gehört.
        /// </summary>
        private KostenPositionCtrl.Abweichung Abweichung(string komponente)
        {
            try
            {
                int id = Convert.ToInt32(DataRepository.ExecuteScalar(
                    "SELECT MIN(ID) FROM Tab_KostenKomponente WHERE Komponente = ?",
                    new OleDbParameter("@k", komponente ?? "")));
                if (id <= 0) return null;

                return KostenPositionCtrl.Pruefe(_idProjekt, komponente,
                                                 Form_Kosten.KATEGORIE_INVESTITION, id);
            }
            catch { return null; }
        }

        // Energieträger des Projekts mit Abrechnungseinheit, effektivem Heizwert und
        // den wirksamen Preisen (Projektwert vor Katalogwert — Kette wie im
        // KostenEmissionRechner).
        private void LadeTraeger(System.Globalization.CultureInfo kultur)
        {
            gridTraeger.Columns.Add("traeger", MyResource.Resource.BK_KOSTEN_SP_TRAEGER);
            gridTraeger.Columns.Add("einheit", MyResource.Resource.BK_KOSTEN_SP_ABRECHNUNG);
            gridTraeger.Columns.Add("hi", MyResource.Resource.BK_KOSTEN_SP_HEIZWERT);
            gridTraeger.Columns.Add("arbeit", MyResource.Resource.BK_KOSTEN_SP_ARBEITSPREIS);
            gridTraeger.Columns.Add("grund", MyResource.Resource.BK_KOSTEN_SP_GRUNDPREIS);
            gridTraeger.Columns[0].FillWeight = 150;
            gridTraeger.Columns[1].FillWeight = 90;
            gridTraeger.Columns[2].FillWeight = 90;
            gridTraeger.Columns[3].FillWeight = 90;
            gridTraeger.Columns[4].FillWeight = 90;
            for (int i = 2; i <= 4; i++)
                gridTraeger.Columns[i].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT carrier_id, name, billing_unit, eff_hi " +
                    "FROM Abfrage_Energietraeger_Effektiv WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", _idProjekt));
                if (dt == null) return;

                foreach (DataRow r in dt.Rows)
                {
                    int carrier = (int)(D(r, "carrier_id") ?? 0);
                    double? preis, grund;
                    LiesPreise(carrier, out preis, out grund);
                    double? hi = D(r, "eff_hi");

                    gridTraeger.Rows.Add(
                        S(r, "name"),
                        S(r, "billing_unit"),
                        hi.HasValue ? hi.Value.ToString("N2", kultur) : "—",
                        preis.HasValue ? preis.Value.ToString("N4", kultur) : "—",
                        grund.HasValue ? grund.Value.ToString("N2", kultur) : "—");
                }
                gridTraeger.ClearSelection();
            }
            catch { }
        }

        // Vorrangkette des KostenEmissionRechners: Projektwert (energy_project_settings)
        // schlägt Katalogwert (energy_carrier).
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

        private void btnVerwaltung_Click(object sender, EventArgs e)
        {
            if (_idProjekt <= 0) return;
            Form f = this.FindForm();
            using (var dlg = new Form_Kosten(_idProjekt))
            {
                dlg.m_ID_Projekt = _idProjekt;
                if (f != null) dlg.ShowDialog(f); else dlg.ShowDialog();
            }
            Aktualisiere();   // Kompaktwerte nach der Pflege auffrischen
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

        // ------------------------------------------------------ Kategorie-Kachel

        /// <summary>
        /// Eine Kategorie-Kachel: Überschrift, großer Wert, kleine Herkunftszeile.
        /// Bewusst schlicht gehalten (Rahmen + Flächenfarbe des Hausstils).
        /// </summary>
        private class Kachel : TableLayoutPanel
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

            public string Wert
            {
                get { return _wert.Text; }
                set { _wert.Text = value ?? "—"; }
            }
        }
    }
}
