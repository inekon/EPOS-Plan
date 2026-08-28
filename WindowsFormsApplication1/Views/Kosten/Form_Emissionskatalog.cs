using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EMISSIONSFAKTOR-KATALOG (Etappe E4,
    /// Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md § 4.2) — ein Dialog,
    /// zwei Aufgaben: links die Emissionsarten (Auswahl, Anlegen, Ändern,
    /// Löschen), rechts die Werte der markierten Art für den übergebenen Träger
    /// samt den trägerunabhängigen Vorlagen.
    ///
    /// <para><b>Zwei Aufrufwege</b> (<see cref="SetControls"/>):</para>
    /// <list type="bullet">
    ///   <item><description><b>Aus dem Emissions-Tab</b> („Katalog…" einer Zeile):
    ///     vorgefiltert auf Art und Träger, Übernahme wird ZURÜCKGEREICHT
    ///     (<see cref="Uebernommen"/>) statt geschrieben — sonst bräche die
    ///     deferred-Semantik des Trägerdialogs (Ä12/Ä14).</description></item>
    ///   <item><description><b>Verwaltungsmodus</b> („Emissionsarten &amp; Katalog
    ///     verwalten…", auch ohne Träger): Übernahme schreibt sofort; ohne
    ///     Trägerkontext bleiben nur Artenverwaltung und trägerunabhängige
    ///     Vorlagen sichtbar.</description></item>
    /// </list>
    ///
    /// <para>Die Regeln stehen NICHT hier, sondern in
    /// <see cref="EmissionskatalogCtrl"/> — der Dialog kennt keine einzige
    /// SQL-Zeile (Hausmuster Ä9).</para>
    /// </summary>
    public partial class Form_Emissionskatalog : Form
    {
        private int _carrierId;
        private string _carrierName = "";
        private bool _rueckgabemodus;
        private bool _wirdGefuellt;

        private List<EmissionsartModel> _arten = new List<EmissionsartModel>();
        private List<EmissionswertModel> _werte = new List<EmissionswertModel>();

        /// <summary>Der im Rückgabemodus markierte und übernommene Katalogwert;
        /// <c>null</c>, wenn nichts übernommen wurde (Konzept F8).</summary>
        public EmissionswertModel Uebernommen { get; private set; }

        /// <summary>true, sobald Arten angelegt, geändert, gelöscht oder ab-/
        /// angewählt wurden — der Emissions-Tab lädt dann seine Feldliste neu
        /// (F5), ohne den Bearbeitungsstand zu verlieren.</summary>
        public bool ArtenGeaendert { get; private set; }

        /// <summary>true, sobald im Verwaltungsmodus ein Trägerwert geschrieben
        /// wurde — der Aufrufer liest dann neu.</summary>
        public bool WerteGeaendert { get; private set; }

        public Form_Emissionskatalog()
        {
            InitializeComponent();

            Text = T("EMK_TITEL", "Emissionsfaktor-Katalog");
            lblKopfTitel.Text = Text;
            grpArten.Text = T("EMK_GRP_ARTEN", "Emissionsarten");
            lblModus.Text = T("EMK_MODUS", "CO₂-Berechnung:");
            rbModusCo2.Text = T("EMK_MODUS_CO2", "CO₂");
            rbModusCo2e.Text = T("EMK_MODUS_CO2E", "CO₂-Äquivalent (GWP₁₀₀)");
            lblModusOrt.Text = T("EMK_MODUS_ORT", "[globale Vorgabe]");
            btnArtNeu.Text = T("EMK_ART_NEU", "Neu…");
            btnArtBearbeiten.Text = T("EMK_ART_BEARBEITEN", "Bearbeiten…");
            btnArtLoeschen.Text = T("EMK_ART_LOESCHEN", "Löschen");
            btnUebernehmen.Text = T("EMK_UEBERNEHMEN", "Übernehmen");
            btnWertNeu.Text = T("EMK_WERT_NEU", "Neu…");
            btnWertBearbeiten.Text = T("EMK_WERT_BEARBEITEN", "Bearbeiten…");
            btnWertLoeschen.Text = T("EMK_WERT_LOESCHEN", "Löschen");
            btnOk.Text = T("KDLG_BTN_OK", "OK");
            btnAbbrechen.Text = T("PVW_ABBRECHEN", "Abbrechen");

            BaueArtenspalten();
            BaueWertespalten();

            dgvArten.SelectionChanged += (s, e) => { if (!_wirdGefuellt) ZeigeWerte(); };
            dgvArten.CurrentCellDirtyStateChanged += DgvArten_CurrentCellDirtyStateChanged;
            dgvArten.CellValueChanged += DgvArten_CellValueChanged;

            btnArtNeu.Click += (s, e) => ArtNeu();
            btnArtBearbeiten.Click += (s, e) => ArtBearbeiten();
            btnArtLoeschen.Click += (s, e) => ArtLoeschen();

            btnUebernehmen.Click += (s, e) => Uebernehmen();
            btnWertNeu.Click += (s, e) => WertNeu();
            btnWertBearbeiten.Click += (s, e) => WertBearbeiten();
            btnWertLoeschen.Click += (s, e) => WertLoeschen();

            btnOk.Click += (s, e) => Beenden();
        }

        /// <summary>
        /// Kontext setzen — vor <c>ShowDialog</c>.
        /// </summary>
        /// <param name="carrierId">Träger; 0 = Verwaltungsmodus ohne Träger
        /// (nur Arten und trägerunabhängige Vorlagen).</param>
        /// <param name="carrierName">Anzeigename für die Kopfzeile.</param>
        /// <param name="artVorwahl">Kürzel der Art, auf die vorgefiltert wird;
        /// leer = erste Art.</param>
        /// <param name="rueckgabemodus">true, wenn „Übernehmen" den Wert
        /// zurückreichen statt schreiben soll (Aufruf aus dem Emissions-Tab).</param>
        public void SetControls(int carrierId, string carrierName, string artVorwahl,
                                bool rueckgabemodus)
        {
            _carrierId = carrierId > 0 ? carrierId : 0;
            _carrierName = carrierName ?? "";
            _rueckgabemodus = rueckgabemodus;

            lblKontext.Text = _carrierId > 0
                ? string.Format(CultureInfo.CurrentCulture,
                    T("EMK_KONTEXT_TRAEGER", "Träger: {0}"), _carrierName)
                : T("EMK_KONTEXT_VERWALTUNG",
                    "Verwaltungsmodus — Arten und trägerunabhängige Vorlagen");

            // Der Modus-Schalter im Katalog wirkt IMMER auf die globale Vorgabe
            // (Konzept F7); der Projekt-Override sitzt im Emissions-Tab.
            _wirdGefuellt = true;
            try
            {
                string modus = EmissionenCtrl.VorgabeLesen();
                rbModusCo2e.Checked = string.Equals(modus, DbWerte.EMISSION_MODUS_CO2E,
                                                    StringComparison.Ordinal);
                rbModusCo2.Checked = !rbModusCo2e.Checked;
            }
            finally { _wirdGefuellt = false; }

            btnUebernehmen.Visible = _carrierId > 0;
            lblHinweis.Text = _carrierId > 0
                ? T("EMK_HINWEIS_TRAEGER",
                    "„Übernehmen“ kopiert den markierten Wert als geltenden Trägerwert und " +
                    "vermerkt die Herkunft. Eine spätere Katalogänderung wirkt NICHT zurück. " +
                    "Werte ohne Träger sind Vorlagen für alle Träger.")
                : T("EMK_HINWEIS_VERWALTUNG",
                    "Ohne Trägerkontext zeigt der Katalog die Arten und die " +
                    "trägerunabhängigen Vorlagen. Ausgelieferte Einträge sind " +
                    "unveränderlich — abwählen statt löschen.");

            ZeigeArten(artVorwahl);
        }

        // =====================================================================
        // Emissionsarten
        // =====================================================================

        private void BaueArtenspalten()
        {
            dgvArten.Columns.Clear();
            dgvArten.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Auswahl",
                HeaderText = "",
                Width = 30,
                ToolTipText = T("EMK_SP_AUSWAHL",
                    "Ausgewählte Arten erscheinen als Feld im Emissions-Tab und gehen in die CO₂e-Summe ein.")
            });
            dgvArten.Columns.Add(NurLesenSpalte("Kuerzel", T("EMK_SP_KUERZEL", "Kürzel"), 104));
            dgvArten.Columns.Add(NurLesenSpalte("Name", T("EMK_SP_NAME", "Name"), 128));
            dgvArten.Columns.Add(NurLesenSpalte("Einheit", T("EMK_SP_EINHEIT", "Einheit"), 62));
            dgvArten.Columns.Add(NurLesenSpalte("GWP", T("EMK_SP_GWP", "GWP₁₀₀"), 56));
        }

        private static DataGridViewTextBoxColumn NurLesenSpalte(string name, string kopf, int breite)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = kopf,
                Width = breite,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
        }

        private void ZeigeArten(string vorwahlKuerzel)
        {
            _arten = EmissionskatalogCtrl.Arten(false);

            _wirdGefuellt = true;
            try
            {
                dgvArten.Rows.Clear();
                foreach (EmissionsartModel a in _arten)
                {
                    int i = dgvArten.Rows.Add(a.Ausgewaehlt, a.Kuerzel, a.Name, a.Einheit,
                        a.Co2Aequivalent.ToString("0.###", CultureInfo.CurrentCulture));

                    // CO₂ ist Pflicht: Häkchen gesetzt und gesperrt (Konzept F1).
                    if (a.IstPflicht)
                    {
                        dgvArten.Rows[i].Cells["Auswahl"].ReadOnly = true;
                        dgvArten.Rows[i].Cells["Auswahl"].Style.ForeColor = SystemColors.GrayText;
                        dgvArten.Rows[i].Cells["Kuerzel"].ToolTipText =
                            T("EMK_TIP_PFLICHT", "Pflichtart — nicht abwählbar, nicht löschbar.");
                    }
                    else if (a.IstAuslieferung)
                    {
                        dgvArten.Rows[i].Cells["Kuerzel"].ToolTipText =
                            T("EMK_TIP_AUSLIEFERUNG",
                              "Ausgelieferte Art — abwählbar, aber nicht löschbar.");
                    }
                }

                if (dgvArten.Rows.Count > 0)
                {
                    int ziel = 0;
                    if (!string.IsNullOrEmpty(vorwahlKuerzel))
                        for (int i = 0; i < _arten.Count; i++)
                            if (string.Equals(_arten[i].Kuerzel, vorwahlKuerzel,
                                              StringComparison.OrdinalIgnoreCase))
                            { ziel = i; break; }
                    dgvArten.ClearSelection();
                    dgvArten.Rows[ziel].Selected = true;
                    dgvArten.CurrentCell = dgvArten.Rows[ziel].Cells["Kuerzel"];
                }
            }
            finally { _wirdGefuellt = false; }

            ZeigeWerte();
        }

        /// <summary>Ein Häkchen soll sofort wirken, nicht erst beim Zellwechsel
        /// (dasselbe Muster wie im Umrechnungsblock des Trägerdialogs).</summary>
        private void DgvArten_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (_wirdGefuellt) return;
            if (dgvArten.IsCurrentCellDirty && dgvArten.CurrentCell is DataGridViewCheckBoxCell)
                dgvArten.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        private void DgvArten_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (_wirdGefuellt || e.RowIndex < 0 || e.RowIndex >= _arten.Count) return;
            if (dgvArten.Columns[e.ColumnIndex].Name != "Auswahl") return;

            EmissionsartModel a = _arten[e.RowIndex];
            bool neu = Convert.ToBoolean(dgvArten.Rows[e.RowIndex].Cells["Auswahl"].Value ?? false);

            string grund;
            if (!EmissionskatalogCtrl.AuswahlSetzen(a.ID, neu, out grund))
            {
                Sagen(grund);
                _wirdGefuellt = true;
                dgvArten.Rows[e.RowIndex].Cells["Auswahl"].Value = a.Ausgewaehlt;
                _wirdGefuellt = false;
                return;
            }
            a.Ausgewaehlt = neu;
            ArtenGeaendert = true;
        }

        private EmissionsartModel GewaehlteArt()
        {
            if (dgvArten.CurrentRow == null) return null;
            int i = dgvArten.CurrentRow.Index;
            return (i >= 0 && i < _arten.Count) ? _arten[i] : null;
        }

        private void ArtNeu()
        {
            var a = new EmissionsartModel
            {
                Einheit = DbWerte.EMISSION_EINHEIT_MG_KWH,
                Sortierung = _arten.Count > 0 ? _arten[_arten.Count - 1].Sortierung + 10 : 10
            };
            if (!ArtBearbeitenDialog(a, true)) return;

            string grund;
            if (EmissionskatalogCtrl.ArtAnlegen(a, out grund) <= 0) { Sagen(grund); return; }
            ArtenGeaendert = true;
            ZeigeArten(a.Kuerzel);
        }

        private void ArtBearbeiten()
        {
            EmissionsartModel a = GewaehlteArt();
            if (a == null) return;

            var kopie = new EmissionsartModel
            {
                ID = a.ID,
                Kuerzel = a.Kuerzel,
                Name = a.Name,
                Einheit = a.Einheit,
                Co2Aequivalent = a.Co2Aequivalent,
                AequivalentQuelle = a.AequivalentQuelle,
                IstPflicht = a.IstPflicht,
                IstAuslieferung = a.IstAuslieferung,
                Ausgewaehlt = a.Ausgewaehlt,
                Sortierung = a.Sortierung
            };
            if (!ArtBearbeitenDialog(kopie, false)) return;

            string grund;
            if (!EmissionskatalogCtrl.ArtAendern(kopie, out grund)) { Sagen(grund); return; }
            ArtenGeaendert = true;
            ZeigeArten(kopie.Kuerzel);
        }

        private void ArtLoeschen()
        {
            EmissionsartModel a = GewaehlteArt();
            if (a == null) return;

            string grund;
            if (a.IstPflicht || a.IstAuslieferung)
            {
                EmissionskatalogCtrl.ArtLoeschen(a.ID, out grund);
                AbwaehlenAnbieten(a, grund);
                return;
            }

            if (MessageBox.Show(string.Format(CultureInfo.CurrentCulture,
                        T("EMK_ART_LOESCHEN_FRAGE", "Emissionsart „{0}“ löschen?"), a.Name),
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            if (!EmissionskatalogCtrl.ArtLoeschen(a.ID, out grund))
            {
                AbwaehlenAnbieten(a, grund);
                return;
            }
            ArtenGeaendert = true;
            ZeigeArten(null);
        }

        /// <summary>„Abwählen statt löschen" (§ 4.2): Der Grund wird genannt UND
        /// der gangbare Weg gleich angeboten — bei der Pflichtart CO₂ gibt es ihn
        /// nicht, dort bleibt es beim Hinweis.</summary>
        private void AbwaehlenAnbieten(EmissionsartModel a, string grund)
        {
            if (a.IstPflicht || !a.Ausgewaehlt) { Sagen(grund); return; }

            if (MessageBox.Show(grund + Environment.NewLine + Environment.NewLine +
                        T("EMK_ART_ABWAEHLEN_FRAGE",
                          "Die Art stattdessen abwählen? Sie verschwindet dann aus den " +
                          "Emissionsfeldern und aus der CO₂e-Summe, ihre Werte bleiben erhalten."),
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Information) != DialogResult.Yes)
                return;

            string fehler;
            if (!EmissionskatalogCtrl.AuswahlSetzen(a.ID, false, out fehler)) { Sagen(fehler); return; }
            a.Ausgewaehlt = false;
            ArtenGeaendert = true;
            ZeigeArten(a.Kuerzel);
        }

        /// <summary>
        /// Kleiner Editor einer Art (Name, Einheit, GWP + Quelle). Bei CO₂ ist der
        /// GWP-Faktor gesperrt und bleibt 1 (F1/F2); das Kürzel einer
        /// ausgelieferten Art ist unveränderlich.
        /// </summary>
        private bool ArtBearbeitenDialog(EmissionsartModel a, bool neu)
        {
            using (var dlg = new Form())
            {
                dlg.Text = neu ? T("EMK_ART_DLG_NEU", "Neue Emissionsart")
                               : T("EMK_ART_DLG_BEARB", "Emissionsart bearbeiten");
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MinimizeBox = false; dlg.MaximizeBox = false; dlg.ShowInTaskbar = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(430, 214);

                var txtKuerzel = new TextBox { Location = new Point(150, 12), Width = 260, Text = a.Kuerzel };
                var txtName = new TextBox { Location = new Point(150, 42), Width = 260, Text = a.Name };
                var cmbEinheit = new ComboBox
                {
                    Location = new Point(150, 72), Width = 120,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbEinheit.Items.Add(DbWerte.EMISSION_EINHEIT_G_KWH);
                cmbEinheit.Items.Add(DbWerte.EMISSION_EINHEIT_MG_KWH);
                cmbEinheit.SelectedIndex = string.Equals(a.Einheit, DbWerte.EMISSION_EINHEIT_G_KWH,
                    StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                var txtGwp = new TextBox
                {
                    Location = new Point(150, 102), Width = 120,
                    Text = a.Co2Aequivalent.ToString("0.###", CultureInfo.CurrentCulture)
                };
                var txtQuelle = new TextBox
                {
                    Location = new Point(150, 132), Width = 260, Text = a.AequivalentQuelle
                };

                txtKuerzel.Enabled = neu || !a.IstAuslieferung;
                txtGwp.Enabled = !a.IstPflicht;
                txtQuelle.Enabled = !a.IstPflicht;

                dlg.Controls.Add(Beschriftung(T("EMK_ART_F_KUERZEL", "Kürzel:"), 12, 15));
                dlg.Controls.Add(Beschriftung(T("EMK_ART_F_NAME", "Name:"), 12, 45));
                dlg.Controls.Add(Beschriftung(T("EMK_ART_F_EINHEIT", "Einheit:"), 12, 75));
                dlg.Controls.Add(Beschriftung(T("EMK_ART_F_GWP", "CO₂-Äquivalent (GWP₁₀₀):"), 12, 105));
                dlg.Controls.Add(Beschriftung(T("EMK_ART_F_QUELLE", "Quelle des Faktors:"), 12, 135));
                dlg.Controls.Add(txtKuerzel); dlg.Controls.Add(txtName);
                dlg.Controls.Add(cmbEinheit); dlg.Controls.Add(txtGwp); dlg.Controls.Add(txtQuelle);

                if (a.IstPflicht)
                    dlg.Controls.Add(new Label
                    {
                        Location = new Point(12, 158), Size = new Size(400, 16),
                        ForeColor = Color.FromArgb(90, 90, 90),
                        Text = T("EMK_ART_PFLICHT_HINWEIS",
                                 "CO₂ ist die Pflichtart: Der Äquivalenzfaktor bleibt 1.")
                    });

                var ok = new Button
                {
                    Text = T("KDLG_BTN_OK", "OK"), DialogResult = DialogResult.OK,
                    Location = new Point(224, 178), Size = new Size(95, 27)
                };
                var abbruch = new Button
                {
                    Text = T("PVW_ABBRECHEN", "Abbrechen"), DialogResult = DialogResult.Cancel,
                    Location = new Point(325, 178), Size = new Size(95, 27)
                };
                dlg.Controls.Add(ok); dlg.Controls.Add(abbruch);
                dlg.AcceptButton = ok; dlg.CancelButton = abbruch;

                while (true)
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return false;

                    double gwp;
                    if (!a.IstPflicht && !Program.ZahlParsen(txtGwp.Text, out gwp))
                    {
                        Sagen(T("EMK_ART_GWP_UNGUELTIG",
                                "Der Äquivalenzfaktor muss eine Zahl sein (Komma oder Punkt)."));
                        continue;
                    }
                    else if (a.IstPflicht) gwp = 1.0;
                    else Program.ZahlParsen(txtGwp.Text, out gwp);

                    if (string.IsNullOrWhiteSpace(txtKuerzel.Text))
                    {
                        Sagen(T("EMK_ART_KUERZEL_LEER", "Das Kürzel darf nicht leer sein."));
                        continue;
                    }

                    a.Kuerzel = txtKuerzel.Text.Trim();
                    a.Name = string.IsNullOrWhiteSpace(txtName.Text) ? a.Kuerzel : txtName.Text.Trim();
                    a.Einheit = Convert.ToString(cmbEinheit.SelectedItem);
                    a.Co2Aequivalent = gwp;
                    a.AequivalentQuelle = txtQuelle.Text.Trim();
                    return true;
                }
            }
        }

        // =====================================================================
        // Werte der markierten Art
        // =====================================================================

        private void BaueWertespalten()
        {
            dgvWerte.Columns.Clear();
            dgvWerte.Columns.Add(NurLesenSpalte("Quelle", T("EMK_SP_QUELLE", "Quelle"), 214));
            dgvWerte.Columns.Add(NurLesenSpalte("Wert", T("EMK_SP_WERT", "Wert"), 78));
            dgvWerte.Columns.Add(NurLesenSpalte("Co2e", T("EMK_SP_CO2E", "bereits CO₂e?"), 94));
            dgvWerte.Columns.Add(NurLesenSpalte("Aktiv", T("EMK_SP_AKTIV", "aktiv"), 76));
            dgvWerte.Columns["Wert"].DefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleRight;
        }

        private void ZeigeWerte()
        {
            EmissionsartModel a = GewaehlteArt();
            _werte = a == null
                ? new List<EmissionswertModel>()
                : EmissionskatalogCtrl.Werte(a.ID, _carrierId);

            grpWerte.Text = a == null
                ? T("EMK_GRP_WERTE", "Werte")
                : string.Format(CultureInfo.CurrentCulture,
                    T("EMK_GRP_WERTE_ART", "Werte: {0}{1}"), a.Name,
                    _carrierId > 0 ? " — " + _carrierName : "");

            dgvWerte.Rows.Clear();
            foreach (EmissionswertModel w in _werte)
            {
                int i = dgvWerte.Rows.Add(
                    w.Herkunftstext,
                    w.Wert.HasValue ? w.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture) : "",
                    w.IstCo2e ? T("EMK_JA", "ja") : T("EMK_NEIN", "nein"),
                    w.IstAktiv ? T("EMK_AKTIV", "◆ geltend")
                               : (w.CarrierId.HasValue ? T("EMK_TRAEGER", "Träger")
                                                       : T("EMK_VORLAGE", "Vorlage")));
                if (w.IstAktiv)
                    dgvWerte.Rows[i].DefaultCellStyle.Font =
                        new Font(dgvWerte.Font, FontStyle.Bold);
                if (w.IstAuslieferung || !string.Equals(w.Quelle,
                        DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT, StringComparison.OrdinalIgnoreCase))
                    dgvWerte.Rows[i].Cells["Quelle"].ToolTipText =
                        T("EMK_TIP_UNVERAENDERLICH",
                          "Ausgelieferter Katalogwert — unveränderlich. Übernehmen ist möglich.");
            }

            bool eigen = GewaehlterWert() != null && DarfAendern(GewaehlterWert());
            btnWertBearbeiten.Enabled = eigen;
            btnWertLoeschen.Enabled = eigen;
            btnWertNeu.Enabled = a != null;
            btnUebernehmen.Enabled = _carrierId > 0 && GewaehlterWert() != null;
            dgvWerte.SelectionChanged -= DgvWerte_SelectionChanged;
            dgvWerte.SelectionChanged += DgvWerte_SelectionChanged;
        }

        private void DgvWerte_SelectionChanged(object sender, EventArgs e)
        {
            EmissionswertModel w = GewaehlterWert();
            bool eigen = w != null && DarfAendern(w);
            btnWertBearbeiten.Enabled = eigen;
            btnWertLoeschen.Enabled = eigen;
            btnUebernehmen.Enabled = _carrierId > 0 && w != null;
        }

        private static bool DarfAendern(EmissionswertModel w)
        {
            return w != null && !w.IstAuslieferung && string.Equals(
                w.Quelle, DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT,
                StringComparison.OrdinalIgnoreCase);
        }

        private EmissionswertModel GewaehlterWert()
        {
            if (dgvWerte.CurrentRow == null) return null;
            int i = dgvWerte.CurrentRow.Index;
            return (i >= 0 && i < _werte.Count) ? _werte[i] : null;
        }

        /// <summary>
        /// ÜBERNEHMEN (F8). Im Rückgabemodus wird der Wert nur zurückgereicht —
        /// der Emissions-Tab trägt ihn in seine Zeile ein und speichert ihn mit
        /// seinem eigenen „Speichern" (Ä12/Ä14). Im Verwaltungsmodus schreibt
        /// <see cref="EmissionskatalogCtrl.Uebernehmen"/> sofort.
        /// </summary>
        private void Uebernehmen()
        {
            EmissionswertModel w = GewaehlterWert();
            if (w == null || _carrierId <= 0) return;
            if (!w.Wert.HasValue)
            {
                Sagen(T("EMK_UEBERNAHME_LEER", "Der gewählte Eintrag trägt keinen Zahlenwert."));
                return;
            }

            if (_rueckgabemodus)
            {
                Uebernommen = w;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            string grund;
            if (!EmissionskatalogCtrl.Uebernehmen(_carrierId, w, out grund)) { Sagen(grund); return; }
            WerteGeaendert = true;
            ZeigeWerte();
        }

        private void WertNeu()
        {
            EmissionsartModel a = GewaehlteArt();
            if (a == null) return;

            var w = new EmissionswertModel
            {
                EmissionsartId = a.ID,
                CarrierId = _carrierId > 0 ? (int?)_carrierId : null,
                Quelle = DbWerte.EMISSIONSWERT_QUELLE_EIGENER_WERT,
                QuelleText = DbWerte.EMISSIONSWERT_TEXT_EIGENER_WERT,
                GueltigAb = DateTime.Today
            };
            if (!WertBearbeitenDialog(w, a, true)) return;

            string grund;
            if (EmissionskatalogCtrl.WertAnlegen(w, out grund) <= 0) { Sagen(grund); return; }
            WerteGeaendert = true;
            ZeigeWerte();
        }

        private void WertBearbeiten()
        {
            EmissionsartModel a = GewaehlteArt();
            EmissionswertModel w = GewaehlterWert();
            if (a == null || w == null) return;
            if (!DarfAendern(w))
            {
                Sagen(T("EMK_WERT_UNVERAENDERLICH",
                        "Ausgelieferte Katalogwerte sind unveränderlich — sie werden über neue " +
                        "Jahreszeilen der gesetzlichen Parameter fortgeschrieben. Legen Sie " +
                        "einen eigenen Wert an."));
                return;
            }

            var kopie = new EmissionswertModel
            {
                ID = w.ID,
                EmissionsartId = w.EmissionsartId,
                CarrierId = w.CarrierId,
                Quelle = w.Quelle,
                QuelleText = w.QuelleText,
                Wert = w.Wert,
                IstCo2e = w.IstCo2e,
                IstAktiv = w.IstAktiv,
                HerkunftId = w.HerkunftId,
                GueltigAb = w.GueltigAb
            };
            if (!WertBearbeitenDialog(kopie, a, false)) return;

            string grund;
            if (!EmissionskatalogCtrl.WertAendern(kopie, out grund)) { Sagen(grund); return; }
            WerteGeaendert = true;
            ZeigeWerte();
        }

        private void WertLoeschen()
        {
            EmissionswertModel w = GewaehlterWert();
            if (w == null) return;
            if (!DarfAendern(w))
            {
                Sagen(T("EMK_WERT_UNVERAENDERLICH",
                        "Ausgelieferte Katalogwerte sind unveränderlich — sie werden über neue " +
                        "Jahreszeilen der gesetzlichen Parameter fortgeschrieben. Legen Sie " +
                        "einen eigenen Wert an."));
                return;
            }

            if (MessageBox.Show(string.Format(CultureInfo.CurrentCulture,
                        T("EMK_WERT_LOESCHEN_FRAGE", "Wert „{0}“ löschen?"), w.Herkunftstext),
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            string grund;
            if (!EmissionskatalogCtrl.WertLoeschen(w.ID, out grund)) { Sagen(grund); return; }
            WerteGeaendert = true;
            ZeigeWerte();
        }

        /// <summary>Kleiner Editor eines EIGENEN Wertes (Bezeichnung, Zahl,
        /// „bereits CO₂e?", Geltungsbereich Träger oder Vorlage).</summary>
        private bool WertBearbeitenDialog(EmissionswertModel w, EmissionsartModel a, bool neu)
        {
            using (var dlg = new Form())
            {
                dlg.Text = neu ? T("EMK_WERT_DLG_NEU", "Neuer eigener Wert")
                               : T("EMK_WERT_DLG_BEARB", "Eigenen Wert bearbeiten");
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.MinimizeBox = false; dlg.MaximizeBox = false; dlg.ShowInTaskbar = false;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.ClientSize = new Size(440, 190);

                var txtText = new TextBox
                {
                    Location = new Point(160, 12), Width = 264,
                    Text = string.IsNullOrEmpty(w.QuelleText)
                           ? DbWerte.EMISSIONSWERT_TEXT_EIGENER_WERT : w.QuelleText
                };
                var txtWert = new TextBox
                {
                    Location = new Point(160, 42), Width = 120,
                    Text = w.Wert.HasValue
                           ? w.Wert.Value.ToString("0.####", CultureInfo.CurrentCulture) : ""
                };
                var lblEinheit = new Label
                {
                    Location = new Point(288, 45), AutoSize = true, Text = a.Einheit
                };
                var chkCo2e = new CheckBox
                {
                    Location = new Point(160, 72), AutoSize = true, Checked = w.IstCo2e,
                    Text = T("EMK_WERT_CO2E",
                             "Wert ist bereits ein CO₂-Äquivalent (nicht weiter aufsummieren)")
                };
                var chkVorlage = new CheckBox
                {
                    Location = new Point(160, 98), AutoSize = true,
                    Checked = !w.CarrierId.HasValue,
                    Enabled = neu && _carrierId > 0,
                    Text = T("EMK_WERT_VORLAGE", "Vorlage für ALLE Träger (ohne Trägerbindung)")
                };

                dlg.Controls.Add(Beschriftung(T("EMK_WERT_F_TEXT", "Bezeichnung/Quelle:"), 12, 15));
                dlg.Controls.Add(Beschriftung(T("EMK_WERT_F_WERT", "Wert:"), 12, 45));
                dlg.Controls.Add(txtText); dlg.Controls.Add(txtWert); dlg.Controls.Add(lblEinheit);
                dlg.Controls.Add(chkCo2e); dlg.Controls.Add(chkVorlage);

                var ok = new Button
                {
                    Text = T("KDLG_BTN_OK", "OK"), DialogResult = DialogResult.OK,
                    Location = new Point(234, 152), Size = new Size(95, 27)
                };
                var abbruch = new Button
                {
                    Text = T("PVW_ABBRECHEN", "Abbrechen"), DialogResult = DialogResult.Cancel,
                    Location = new Point(335, 152), Size = new Size(95, 27)
                };
                dlg.Controls.Add(ok); dlg.Controls.Add(abbruch);
                dlg.AcceptButton = ok; dlg.CancelButton = abbruch;

                while (true)
                {
                    if (dlg.ShowDialog(this) != DialogResult.OK) return false;

                    double zahl;
                    if (!Program.ZahlParsen(txtWert.Text, out zahl) || zahl < 0)
                    {
                        Sagen(T("EMK_WERT_UNGUELTIG",
                                "Der Wert muss eine Zahl ≥ 0 sein (Komma oder Punkt)."));
                        continue;
                    }

                    w.QuelleText = string.IsNullOrWhiteSpace(txtText.Text)
                        ? DbWerte.EMISSIONSWERT_TEXT_EIGENER_WERT : txtText.Text.Trim();
                    w.Wert = zahl;
                    w.IstCo2e = chkCo2e.Checked;
                    if (neu) w.CarrierId = chkVorlage.Checked ? (int?)null
                                                             : (_carrierId > 0 ? (int?)_carrierId : null);
                    return true;
                }
            }
        }

        // =====================================================================
        // Abschluss
        // =====================================================================

        /// <summary>OK: Der Modus-Schalter schreibt hier die GLOBALE VORGABE
        /// (F7) — anders als im Emissions-Tab, wo er im Projektkontext das
        /// Projektfeld trifft.</summary>
        private void Beenden()
        {
            string modus = rbModusCo2e.Checked
                ? DbWerte.EMISSION_MODUS_CO2E : DbWerte.EMISSION_MODUS_CO2;
            if (!string.Equals(modus, EmissionenCtrl.VorgabeLesen(), StringComparison.Ordinal))
                EmissionenCtrl.VorgabeSchreiben(modus);

            DialogResult = DialogResult.OK;
            Close();
        }

        private static Label Beschriftung(string text, int x, int y)
        {
            return new Label { Text = text, Location = new Point(x, y), AutoSize = true };
        }

        private void Sagen(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            MessageBox.Show(text, Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>MyResource mit deutschem Rückfall (Drei-Schichten-Regel).</summary>
        private static string T(string schluessel, string rueckfall)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(schluessel);
                return string.IsNullOrEmpty(s) ? rueckfall : s;
            }
            catch { return rueckfall; }
        }
    }
}
