using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Kapitalwert-Vergleichsansicht „Wirtschaftlichkeit"
    /// (Konzept_Wirtschaftlichkeit.md, Kap. 6; Phase 6 = Ausbaustufe W1).
    ///
    /// Zeigt die Kapitalwert-Ergebnisse der Vergleichsgruppe (Stamm + gewählte
    /// Varianten) je Szenario. Vorbedingungen laut Vorgabe: Varianten müssen
    /// ausgewählt und berechnet sein — fehlende Simulationsergebnisse werden beim
    /// Berechnen automatisch headless nachgerechnet (BerichtsDatenSammler, gleiche
    /// Prüfkette wie der Bericht). Ergebnisse werden persistiert
    /// (Tab_ErgebnisWirtschaftlichkeit) — Seite, Word- und Excel-Bericht zeigen
    /// damit garantiert identische Zahlen.
    ///
    /// <para><b>Herkunft.</b> Der Inhalt stand bis zum Umbau „Berichte &amp; Kosten"
    /// direkt in <see cref="Form_Wirtschaftlichkeit"/>. Er ist unverändert in dieses
    /// UserControl gehoben worden, damit ihn die Seite „Wirtschaftlichkeit" des
    /// Reiters einbetten kann; <see cref="Form_Wirtschaftlichkeit"/> ist seither nur
    /// noch ein dünner Dialog-Wrapper um dieses Control. Geändert wurden allein die
    /// Wirtsbeziehungen: Formular-Eigenschaften (ClientSize/Text/StartPosition) sind
    /// zu Control-Eigenschaften geworden, <c>Load</c>/<c>FormClosing</c> zu
    /// <see cref="LadeDaten"/>/<see cref="Beschaeftigt"/>+<see cref="Abbrechen"/>,
    /// und die Dialogaufrufe nehmen das umgebende Formular als Besitzer.</para>
    ///
    /// Ist das übergebene Projekt eine Variante, wird automatisch ihr Stamm verwendet.
    /// </summary>
    public class UcWirtschaftlichkeit : UserControl
    {
        private readonly int _idStamm;
        private readonly string _stammName;

        private readonly WirtschaftlichkeitCtrl _ctrl = new WirtschaftlichkeitCtrl();
        private CancellationTokenSource _cts;
        private bool _initialisiere;
        private bool _geladen;

        private List<WirtschaftlichkeitErgebnis> _ergebnisse = new List<WirtschaftlichkeitErgebnis>();
        private readonly Dictionary<int, string> _namen = new Dictionary<int, string>();

        // W3: Emissionsbilanzen + Parameter werden je Datenstand EINMAL ermittelt
        // (Review Phase 8 — nicht bei jedem Szenariowechsel im UI-Thread rechnen).
        private WirtschaftlichkeitParameter _parameterCache;

        /// <summary>
        /// ETAPPE E7: Der Tarif entscheidet über die Beschriftung der Stromkostenzeile
        /// (Bezug im Zonenmodell, Reststrom im Rollenmodell). Er wird EINMAL je Sitzung
        /// gelesen — <see cref="ZeigeErgebnisse"/> läuft bei jedem Szenariowechsel, und
        /// eine Datenbankabfrage je Wechsel wäre Verschwendung.
        /// </summary>
        private TarifParameter _tarifCache;
        private readonly Dictionary<int, EmissionsBilanz> _bilanzen = new Dictionary<int, EmissionsBilanz>();

        // Steuerelemente
        private Label lblVarianten;
        private ListView lvVarianten;
        private ColumnHeader colArt, colBez, colName, colSim;
        private Label lblSzenario;
        private ComboBox cbSzenario;
        private DataGridView grid;
        private Label lblParameter;
        private Label lblStatus;
        private ProgressBar progress;
        private Button btnTarif, btnParameter, btnVerlauf, btnBerechnen, btnSchliessen;

        /// <summary>Stammprojekt-ID der angezeigten Vergleichsgruppe.</summary>
        public int IdStamm { get { return _idStamm; } }

        /// <summary>
        /// true = das Control sitzt im Dialog-Wrapper <see cref="Form_Wirtschaftlichkeit"/>;
        /// dann bleibt „Schließen" dauerhaft sichtbar. Eingebettet im Reiter erscheint
        /// der Knopf nur während eines Laufs — als „Abbrechen".
        /// </summary>
        public bool AlsDialog
        {
            get { return _alsDialog; }
            set { _alsDialog = value; if (btnSchliessen != null) btnSchliessen.Visible = value || Beschaeftigt; }
        }
        private bool _alsDialog;

        /// <summary>Der Anwender hat „Schließen" gedrückt (nur im Dialog-Wrapper belegt).</summary>
        public event EventHandler SchliessenAngefordert;

        public UcWirtschaftlichkeit(int idProjekt)
        {
            // Variante → Stamm auflösen (Muster Form_AlsVariante/Form_Bericht).
            int idStamm = idProjekt;
            try
            {
                int refId = new VariantenCtrl().StammRefDerVariante(idProjekt);
                if (refId > 0) idStamm = refId;
            }
            catch { }
            _idStamm = idStamm;

            var pc = new ProjektCtrl();
            pc.ReadSingle(_idStamm);
            _stammName = pc.rows > 0 ? pc.m_szProjektname : "";

            InitializeComponent();
        }

        /// <summary>Titelzeile für den Dialog-Wrapper bzw. die Seitenüberschrift.</summary>
        public string Titel
        {
            get { return "Wirtschaftlichkeit (Kapitalwertmethode DIN EN 17463) — Stamm: " + _stammName; }
        }

        // ------------------------------------------------------------- Aufbau

        private void InitializeComponent()
        {
            this.lblVarianten = new Label();
            this.lvVarianten = new ListView();
            this.colArt = new ColumnHeader();
            this.colBez = new ColumnHeader();
            this.colName = new ColumnHeader();
            this.colSim = new ColumnHeader();
            this.lblSzenario = new Label();
            this.cbSzenario = new ComboBox();
            this.grid = new DataGridView();
            this.lblParameter = new Label();
            this.lblStatus = new Label();
            this.progress = new ProgressBar();
            this.btnTarif = new Button();
            this.btnParameter = new Button();
            this.btnVerlauf = new Button();
            this.btnBerechnen = new Button();
            this.btnSchliessen = new Button();
            this.SuspendLayout();

            // Variantenliste (oben)
            this.lblVarianten.AutoSize = true;
            this.lblVarianten.Location = new Point(12, 12);
            this.lblVarianten.Text = "Vergleichsgruppe (Referenz: Stamm, fest gewählt):";

            this.lvVarianten.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            this.lvVarianten.CheckBoxes = true;
            this.lvVarianten.Columns.AddRange(new ColumnHeader[] { this.colArt, this.colBez, this.colName, this.colSim });
            this.lvVarianten.FullRowSelect = true;
            this.lvVarianten.HideSelection = false;
            this.lvVarianten.Location = new Point(12, 32);
            this.lvVarianten.MultiSelect = false;
            this.lvVarianten.Size = new Size(876, 120);
            this.lvVarianten.View = View.Details;
            this.lvVarianten.ItemCheck += new ItemCheckEventHandler(this.lvVarianten_ItemCheck);

            this.colArt.Text = "Art"; this.colArt.Width = 70;
            this.colBez.Text = "Bezeichner"; this.colBez.Width = 180;
            this.colName.Text = "Projektname"; this.colName.Width = 330;
            this.colSim.Text = "Simulation"; this.colSim.Width = 130;

            // Szenario-Umschalter
            this.lblSzenario.AutoSize = true;
            this.lblSzenario.Location = new Point(12, 164);
            this.lblSzenario.Text = "Szenario:";

            this.cbSzenario.DropDownStyle = ComboBoxStyle.DropDownList;
            this.cbSzenario.Location = new Point(78, 160);
            this.cbSzenario.Width = 140;
            this.cbSzenario.Items.AddRange(new object[]
            {
                WirtschaftlichkeitSzenario.ERWARTET,
                WirtschaftlichkeitSzenario.BEST,
                WirtschaftlichkeitSzenario.WORST
            });
            this.cbSzenario.SelectedIndex = 0;
            this.cbSzenario.SelectedIndexChanged += (s, e) => ZeigeErgebnisse();

            // Ergebnis-Tabelle
            this.grid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AllowUserToResizeRows = false;
            this.grid.ReadOnly = true;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            this.grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
            this.grid.Location = new Point(12, 192);
            this.grid.Size = new Size(876, 268);

            // Parameter-Nachweiszeile
            this.lblParameter.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            this.lblParameter.ForeColor = Color.DimGray;
            this.lblParameter.Location = new Point(12, 468);
            this.lblParameter.Size = new Size(876, 18);

            // Status + Fortschritt
            this.lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.lblStatus.ForeColor = Color.DimGray;
            this.lblStatus.Location = new Point(12, 490);
            this.lblStatus.Size = new Size(276, 18);

            this.progress.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            this.progress.Location = new Point(12, 512);
            this.progress.Size = new Size(276, 14);
            this.progress.Visible = false;

            // Schaltflächen
            this.btnTarif.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnTarif.Location = new Point(304, 494);
            this.btnTarif.Size = new Size(124, 30);
            this.btnTarif.Text = "Tarifstruktur…";
            this.btnTarif.Click += new EventHandler(this.btnTarif_Click);

            this.btnParameter.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnParameter.Location = new Point(434, 494);
            this.btnParameter.Size = new Size(110, 30);
            this.btnParameter.Text = "Parameter…";
            this.btnParameter.Click += new EventHandler(this.btnParameter_Click);

            this.btnVerlauf.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnVerlauf.Location = new Point(550, 494);
            this.btnVerlauf.Size = new Size(110, 30);
            this.btnVerlauf.Text = "Verlauf…";
            this.btnVerlauf.Click += new EventHandler(this.btnVerlauf_Click);

            this.btnBerechnen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnBerechnen.Location = new Point(666, 494);
            this.btnBerechnen.Size = new Size(110, 30);
            this.btnBerechnen.Text = "Berechnen";
            this.btnBerechnen.Click += new EventHandler(this.btnBerechnen_Click);

            this.btnSchliessen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            this.btnSchliessen.Location = new Point(782, 494);
            this.btnSchliessen.Size = new Size(106, 30);
            this.btnSchliessen.Text = "Schließen";
            this.btnSchliessen.Visible = false;   // im Reiter nur während eines Laufs (SetBusy)
            this.btnSchliessen.Click += new EventHandler(this.btnSchliessen_Click);

            // Control
            this.AutoScaleMode = AutoScaleMode.Font;
            this.Size = new Size(900, 536);
            this.MinimumSize = new Size(640, 380);   // darunter kollabiert die Kennzahlentabelle
            this.Font = new Font("Segoe UI", 9f);
            this.Name = "UcWirtschaftlichkeit";
            this.Controls.Add(this.lblVarianten);
            this.Controls.Add(this.lvVarianten);
            this.Controls.Add(this.lblSzenario);
            this.Controls.Add(this.cbSzenario);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.lblParameter);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.progress);
            this.Controls.Add(this.btnTarif);
            this.Controls.Add(this.btnParameter);
            this.Controls.Add(this.btnVerlauf);
            this.Controls.Add(this.btnBerechnen);
            this.Controls.Add(this.btnSchliessen);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        /// <summary>Umgebendes Formular als Dialog-Besitzer (im Reiter das Startformular).</summary>
        private IWin32Window Besitzer
        {
            get { Form f = this.FindForm(); return f != null ? (IWin32Window)f : this; }
        }

        // ------------------------------------------------------------- Laden

        /// <summary>
        /// Baut Liste und Kennzahlen auf. Wird vom Wrapper bzw. vom Reiter genau einmal
        /// gerufen; wiederholte Aufrufe sind wirkungslos (frühere Load-Ereigniskette).
        /// </summary>
        public void LadeDaten()
        {
            if (_geladen || this.DesignMode) return;
            _geladen = true;

            AktualisiereListe(false);
            ZeigeParameterzeile();

            // Persistierte Ergebnisse anzeigen, solange sie zum Simulationsstand passen.
            _ergebnisse = _ctrl.LadeErgebnisse(GewaehlteIds(true));
            bool veraltet = _ergebnisse.Count > 0 &&
                            _ergebnisse.Any(x => x.Fehlgrund == null && !_ctrl.ErgebnisAktuell(x));
            AktualisiereBilanzen();
            ZeigeErgebnisse();
            Melde(_ergebnisse.Count == 0
                ? "Noch keine Wirtschaftlichkeitsberechnung gespeichert — bitte „Berechnen“."
                : veraltet
                    ? "⚠ Gespeicherte Ergebnisse passen nicht mehr zum Simulationsstand — bitte „Berechnen“."
                    : "Gespeicherte Ergebnisse vom " +
                      _ergebnisse[0].Zeitstempel.ToString("dd.MM.yyyy HH:mm") + ".");
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            LadeDaten();
        }

        /// <summary>Variantenliste (neu) aufbauen; bewahreAuswahl = Häkchen erhalten.</summary>
        private void AktualisiereListe(bool bewahreAuswahl)
        {
            var abgewaehlt = new HashSet<int>();
            if (bewahreAuswahl)
                foreach (ListViewItem it in lvVarianten.Items)
                {
                    var alt = it.Tag as BerichtsDatenSammler.VariantenStatus;
                    if (alt != null && !it.Checked) abgewaehlt.Add(alt.IdProjekt);
                }

            _initialisiere = true;
            try
            {
                lvVarianten.Items.Clear();
                _namen.Clear();
                foreach (BerichtsDatenSammler.VariantenStatus st in
                         BerichtsDatenSammler.ErmittleStatus(_idStamm, _stammName))
                {
                    var it = new ListViewItem(new[]
                    {
                        st.IstStamm ? "Stamm" : "Variante",
                        st.IstStamm ? "(Stammprojekt)" : st.Variantenname,
                        st.Projektname,
                        st.SimStandText
                    });
                    it.Tag = st;
                    // Vorgabe: standardmäßig alle Varianten der Gruppe vergleichen.
                    it.Checked = st.IstStamm || !abgewaehlt.Contains(st.IdProjekt);
                    if (!st.SimStand.HasValue || st.Veraltet) it.ForeColor = Color.Firebrick;
                    lvVarianten.Items.Add(it);
                    _namen[st.IdProjekt] = st.IstStamm ? "Stamm"
                        : (string.IsNullOrEmpty(st.Variantenname) ? st.Projektname : st.Variantenname);
                }
            }
            finally { _initialisiere = false; }
        }

        private void ZeigeParameterzeile()
        {
            WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
            TarifParameter t = _ctrl.LadeTarif(_idStamm);
            lblParameter.Text = "Parameter: " + p.Nachweis(BerichtTexte.Kultur) +
                                " · Referenz: Stammprojekt · Restwert linear · " +
                                t.Nachweis(BerichtTexte.Kultur);
        }

        // ------------------------------------------------------------- Ereignisse

        private void lvVarianten_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (_initialisiere) return;
            var st = lvVarianten.Items[e.Index].Tag as BerichtsDatenSammler.VariantenStatus;
            if (st != null && st.IstStamm && e.NewValue != CheckState.Checked)
            {
                e.NewValue = CheckState.Checked;
                Melde("Das Stammprojekt ist die Referenz und immer enthalten.");
            }
        }

        private void btnTarif_Click(object sender, EventArgs e)
        {
            using (var dlg = new Form_Tarifstruktur(_idStamm))
            {
                dlg.ShowDialog(Besitzer);
                if (dlg.Gespeichert)
                {
                    _tarifCache = null;   // E7: Beschriftung der Stromkostenzeile neu holen
                    ZeigeParameterzeile();
                    Melde("Tarifstruktur gespeichert — bitte neu berechnen.");
                }
            }
        }

        private void btnParameter_Click(object sender, EventArgs e)
        {
            using (var dlg = new Form_WirtschaftlichkeitParameter(_idStamm))
            {
                dlg.ShowDialog(Besitzer);
                if (dlg.Gespeichert)
                {
                    ZeigeParameterzeile();
                    Melde("Parameter gespeichert — bitte neu berechnen.");
                }
            }
        }

        private void btnVerlauf_Click(object sender, EventArgs e)
        {
            // Kapitalwert-Verlauf (Phase 11): Zeitraum frei wählbar (auch > T).
            var variantenIds = new List<int>();
            foreach (ListViewItem it in lvVarianten.Items)
            {
                var st = it.Tag as BerichtsDatenSammler.VariantenStatus;
                if (st != null && !st.IstStamm && it.Checked) variantenIds.Add(st.IdProjekt);
            }
            using (var dlg = new Form_WirtschaftlichkeitVerlauf(_idStamm, _stammName, variantenIds))
            {
                dlg.ShowDialog(Besitzer);

                // Der Verlaufsdialog kann neu simuliert haben (Stundenreihen) — dann
                // passen die persistierten Ergebnisse nicht mehr zum Simulationsstand
                // (Review Phase 11): Anzeige auffrischen und offen darauf hinweisen.
                if (dlg.DatenNeuGesammelt)
                {
                    AktualisiereListe(true);
                    _ergebnisse = _ctrl.LadeErgebnisse(GewaehlteIds(true));
                    AktualisiereBilanzen();
                    ZeigeErgebnisse();
                    if (_ergebnisse.Any(x => x.Fehlgrund == null && !_ctrl.ErgebnisAktuell(x)))
                        Melde("⚠ Für den Verlauf wurde neu simuliert — gespeicherte Ergebnisse " +
                              "passen nicht mehr zum Simulationsstand, bitte „Berechnen“.");
                }
            }
        }

        private void btnSchliessen_Click(object sender, EventArgs e)
        {
            if (_cts != null) { _cts.Cancel(); return; }
            EventHandler h = SchliessenAngefordert;
            if (h != null) h(this, EventArgs.Empty);
        }

        /// <summary>true, solange ein Berechnungslauf aussteht (Wrapper darf dann nicht schließen).</summary>
        public bool Beschaeftigt { get { return _cts != null; } }

        /// <summary>Bricht einen laufenden Berechnungslauf ab (Wrapper beim Schließen).</summary>
        public void Abbrechen()
        {
            if (_cts != null) _cts.Cancel();
        }

        // ------------------------------------------------------------- Berechnen

        private List<int> GewaehlteIds(bool mitStamm)
        {
            var ids = new List<int>();
            foreach (ListViewItem it in lvVarianten.Items)
            {
                var st = it.Tag as BerichtsDatenSammler.VariantenStatus;
                if (st == null || !it.Checked) continue;
                if (st.IstStamm && !mitStamm) continue;
                ids.Add(st.IdProjekt);
            }
            return ids;
        }

        private async void btnBerechnen_Click(object sender, EventArgs e)
        {
            if (_cts != null) return;

            var variantenIds = new List<int>();
            foreach (ListViewItem it in lvVarianten.Items)
            {
                var st = it.Tag as BerichtsDatenSammler.VariantenStatus;
                if (st != null && !st.IstStamm && it.Checked) variantenIds.Add(st.IdProjekt);
            }

            _cts = new CancellationTokenSource();
            SetBusy(true);
            var melder = new Progress<BerichtsDatenSammler.Fortschritt>(f =>
            {
                if (f.Gesamt > 0)
                {
                    progress.Maximum = f.Gesamt;
                    progress.Value = Math.Min(f.Aktuell, f.Gesamt);
                }
                Melde(string.Format("({0}/{1}) {2}", f.Aktuell, f.Gesamt, f.Text));
            });

            try
            {
                CancellationToken ct = _cts.Token;
                WirtschaftlichkeitParameter p = _ctrl.LadeParameter(_idStamm);
                TarifParameter tarif = _ctrl.LadeTarif(_idStamm);

                // W3: Tarifmatrix und KWKG-Split brauchen Stundenreihen — dann wird
                // je Projekt frisch in-memory simuliert (wie beim Ganglinien-Bericht).
                bool mitZeitreihen = tarif.Aktiv || p.KwkgBonus > 0 || p.KwkgBonusEinspeisung > 0;

                // Prüfkette (Konzept Kap. 6, Punkt 2): fehlende/veraltete Simulations-
                // ergebnisse rechnet der Sammler automatisch headless nach.
                _ergebnisse = await Task.Run(() =>
                {
                    BerichtsDaten daten = new BerichtsDatenSammler().Sammle(
                        _idStamm, _stammName, variantenIds,
                        false, mitZeitreihen, melder, ct);
                    return _ctrl.Berechne(daten, p);
                }, ct);

                AktualisiereListe(true);      // Simulationsstände auffrischen, Auswahl erhalten
                ZeigeParameterzeile();
                AktualisiereBilanzen();
                ZeigeErgebnisse();            // frisch berechnete Ergebnisse anzeigen
                Melde("Berechnet am " + DateTime.Now.ToString("dd.MM.yyyy HH:mm") +
                      " — Ergebnisse gespeichert (Basis für den Berichts-Baustein Wirtschaftlichkeit).");
            }
            catch (OperationCanceledException) { Melde("Vorgang abgebrochen."); }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler bei der Wirtschaftlichkeitsberechnung: " + ex.Message,
                    "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _cts.Dispose();
                _cts = null;
                SetBusy(false);
            }
        }

        // ------------------------------------------------------------- Anzeige

        private void ZeigeErgebnisse()
        {
            string szenario = cbSzenario.SelectedItem as string ?? WirtschaftlichkeitSzenario.ERWARTET;
            var kultur = BerichtTexte.Kultur;

            grid.Columns.Clear();
            grid.Rows.Clear();

            List<WirtschaftlichkeitErgebnis> zeilen = _ergebnisse
                .Where(x => x.Szenario == szenario)
                .OrderByDescending(x => x.IstStamm)
                .ToList();
            if (zeilen.Count == 0) return;

            grid.Columns.Add("kennzahl", "Kennzahl");
            grid.Columns[0].FillWeight = 190;
            foreach (WirtschaftlichkeitErgebnis erg in zeilen)
            {
                string name = _namen.ContainsKey(erg.IdProjekt) ? _namen[erg.IdProjekt]
                            : (erg.IstStamm ? "Stamm" : erg.Anzeige);
                int idx = grid.Columns.Add("p" + erg.IdProjekt, name);
                grid.Columns[idx].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                grid.Columns[idx].FillWeight = 110;
            }

            // ETAPPE E7: EINE Zeilendefinition für Reiter, Word und Excel
            // (WirtschaftlichkeitZeilen). Bis dahin stand dieselbe Liste dreimal im
            // Code — hier, im Word-Baustein und im Excel-Generator.
            //
            // Die SICHTBARKEIT entscheidet sich über ALLE Ergebnisse der Gruppe (alle
            // Szenarien), nicht über das gerade angezeigte — sonst zeigten Reiter und
            // Bericht verschiedene Tabellen. Eine Zeile, die im angezeigten Szenario
            // nirgends einen Wert hat, fällt darunter trotzdem weg.
            if (_tarifCache == null)
            {
                try { _tarifCache = _ctrl.LadeTarif(_idStamm); }
                catch { _tarifCache = new TarifParameter(); }
            }
            foreach (WirtZeile z in WirtschaftlichkeitZeilen.Kennzahlen(_ergebnisse, _tarifCache))
            {
                bool hatWert = zeilen.Any(x => z.IstText
                    ? !string.IsNullOrEmpty(z.Text(x))
                    : (x.IstStamm && z.StammAnzeige != null) || (z.Wert != null && z.Wert(x).HasValue));
                if (!hatWert) continue;
                Zeile(z.Titel, zeilen, x => z.Anzeige(x, kultur));
            }

            // W3: CO₂-Vermeidung gegenüber getrennter Erzeugung (aus dem Cache;
            // nur für Projekte, deren Wirtschaftlichkeits-Ergebnis zum aktuellen
            // Simulationslauf passt — sonst „—", Review Phase 8).
            if (_bilanzen.Values.Any(x => x != null && x.CO2VermeidungT.HasValue))
                Zeile("CO₂-Vermeidung vs. getrennt [t/a]", zeilen, x =>
                {
                    EmissionsBilanz b = _bilanzen.ContainsKey(x.IdProjekt) ? _bilanzen[x.IdProjekt] : null;
                    return b == null ? "—" : W(b.CO2VermeidungT, "N1", kultur);
                });

            // Hinweiszeilen (nicht-fatal W3 / unvollständige Rechnungen).
            if (zeilen.Any(x => x.Hinweis != null))
                Zeile("Hinweis", zeilen, x => x.Hinweis != null ? "⚠ " + x.Hinweis : "");
            if (zeilen.Any(x => x.Fehlgrund != null))
                Zeile("Hinweis", zeilen, x => x.Fehlgrund != null ? "⚠ " + x.Fehlgrund : "");

            grid.ClearSelection();
        }

        /// <summary>Emissionsbilanz-Cache neu füllen (nur aktuelle Ergebnisse, W3).</summary>
        private void AktualisiereBilanzen()
        {
            _bilanzen.Clear();
            _parameterCache = _ctrl.LadeParameter(_idStamm);
            if (_parameterCache.IdKraftwerkspark <= 0) return;
            foreach (WirtschaftlichkeitErgebnis erg in _ergebnisse
                     .Where(x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET))
            {
                if (_bilanzen.ContainsKey(erg.IdProjekt)) continue;
                _bilanzen[erg.IdProjekt] = _ctrl.ErgebnisAktuell(erg)
                    ? EmissionsBilanzRechner.Berechne(erg.IdProjekt, _parameterCache)
                    : null;
            }
        }

        private void Zeile(string label, List<WirtschaftlichkeitErgebnis> zeilen,
                           Func<WirtschaftlichkeitErgebnis, string> wert)
        {
            var werte = new List<object> { label };
            foreach (WirtschaftlichkeitErgebnis erg in zeilen) werte.Add(wert(erg));
            int idx = grid.Rows.Add(werte.ToArray());
            grid.Rows[idx].Cells[0].Style.Font = new Font(grid.Font, FontStyle.Bold);
        }

        private static string W(double? v, string format, System.Globalization.CultureInfo kultur)
        { return v.HasValue ? v.Value.ToString(format, kultur) : "—"; }

        private void SetBusy(bool busy)
        {
            progress.Visible = busy;
            if (!busy) progress.Value = 0;
            lvVarianten.Enabled = !busy;
            cbSzenario.Enabled = !busy;
            btnTarif.Enabled = !busy;
            btnParameter.Enabled = !busy;
            btnVerlauf.Enabled = !busy;
            btnBerechnen.Enabled = !busy;
            // Eingebettet dient der Knopf allein dem Abbrechen; im Dialog bleibt er stehen.
            btnSchliessen.Visible = AlsDialog || busy;
            btnSchliessen.Text = busy ? "Abbrechen" : "Schließen";
            this.UseWaitCursor = busy;
        }

        private void Melde(string text) { lblStatus.Text = text ?? ""; }
    }
}
