using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Der Komponenten-Kostendialog „Kostenverwaltung ‹Komponente›" (Etappe KD2,
    /// Konzept Kostendialoge Rev. 1.2, § 5) — <b>Admin-/Stammkontext</b>:
    /// Vorlagenpflege je Komponente und Kategorie (Investition/Betrieb) mit
    /// Varianten (KL2), Positionsraster nach den Vorlagen-Folien 8/19,
    /// Satz-🔗-Betrag-Kopplung (KL4), Nutzungsdauer-Spalte (FK4) und
    /// Netto-/Bruttosumme (KL5, Umsatzsteuersatz aus dem Gesetzeskatalog).
    ///
    /// <para><b>Designer-fähig (Ä6):</b> Layout in
    /// <c>Form_KostenKomponente.Designer.cs</c> mit deutschen Vorgabetexten; der
    /// Konstruktor überschreibt aus <c>MyResource</c> (de/en). Nur das
    /// Positionsraster wird zur Laufzeit in das im Designer platzierte
    /// <c>pnlZeilen</c> gefüllt — die Zeile selbst ist das Designer-fähige
    /// <see cref="ucVorlagenZeile"/>.</para>
    ///
    /// <para><b>Der Dialog rechnet und schreibt nicht selbst:</b> alle Zugriffe
    /// laufen über <see cref="KostenVorlagenCtrl"/>. Der Projektkontext folgt in
    /// Etappe KD3 (Übernahme-Mechanik, § 8).</para>
    /// </summary>
    public partial class Form_KostenKomponente : Form
    {
        private readonly List<ucVorlagenZeile> _zeilen = new List<ucVorlagenZeile>();
        private IList<KeyValuePair<int, string>> _komponenten;
        private IList<KostenVorlageKopf> _varianten;
        private bool _fuellt;

        /// <summary>ETAPPE KD5 (§ 6): Inhalt des Reiters „Ertrag/Bonus".</summary>
        private ucErtragBonus _ertrag;

        public Form_KostenKomponente()
        {
            InitializeComponent();
            TexteAnwenden();

            // KD5: Der Platzhalter weicht dem echten Reiterinhalt (BHKW: HF6-Anzeige;
            // PV: Vergütungsdialog-Einstieg; sonst blendet FK5 den Reiter aus).
            lblErtragHinweis.Visible = false;
            _ertrag = new ucErtragBonus { Dock = DockStyle.Fill };
            tpErtrag.Controls.Add(_ertrag);
            _ertrag.BringToFront();

            _fuellt = true;
            _komponenten = KostenVorlagenCtrl.Komponenten();
            cmbKomponente.Items.Clear();
            foreach (KeyValuePair<int, string> k in _komponenten)
                cmbKomponente.Items.Add(k.Value);
            if (cmbKomponente.Items.Count > 0) cmbKomponente.SelectedIndex = 0;
            _fuellt = false;

            Kontext_Geaendert(this, EventArgs.Empty);
        }

        /// <summary>Vorwahl einer Komponente (Aufruf aus dem Anlagendialog, KD6).</summary>
        public void SetControls(string komponente)
        {
            for (int i = 0; i < cmbKomponente.Items.Count; i++)
                if (string.Equals((string)cmbKomponente.Items[i], komponente, StringComparison.Ordinal))
                { cmbKomponente.SelectedIndex = i; break; }
        }

        /// <summary>Auf die Betriebskosten-Sicht schalten (Aufruf „Betriebskosten…"
        /// aus dem Anlagendialog, KD6).</summary>
        public void WaehleBetrieb()
        {
            rbBetrieb.Checked = true;
        }

        // ------------------------------------------------------------- Kontext ---

        private int KomponentenId
        {
            get
            {
                int i = cmbKomponente.SelectedIndex;
                return (i >= 0 && i < _komponenten.Count) ? _komponenten[i].Key : 0;
            }
        }

        private int KategorieId
        {
            get { return rbInvest.Checked ? Form_Kosten.KATEGORIE_INVESTITION : Form_Kosten.KATEGORIE_BETRIEB; }
        }

        private KostenVorlageKopf Variante
        {
            get
            {
                int i = cmbVariante.SelectedIndex;
                return (_varianten != null && i >= 0 && i < _varianten.Count) ? _varianten[i] : null;
            }
        }

        private void Kontext_Geaendert(object sender, EventArgs e)
        {
            if (_fuellt) return;
            KopfAnzeigen();
            VariantenLaden(null);
            ErtragReiterSteuern();
        }

        /// <summary>
        /// ETAPPE KD5 / FK5: Der Reiter „Ertrag/Bonus" existiert nur für BHKW und
        /// Photovoltaik — bei allen übrigen Komponenten wird die Reiterseite
        /// ENTFERNT (nicht nur geleert), damit sie gar nicht erst anwählbar ist.
        /// </summary>
        private void ErtragReiterSteuern()
        {
            string name = cmbKomponente.SelectedIndex >= 0
                ? (string)cmbKomponente.Items[cmbKomponente.SelectedIndex] : "";
            bool zeigen = ucErtragBonus.HatInhalt(name);
            bool drin = tabHaupt.TabPages.Contains(tpErtrag);

            if (zeigen && !drin) tabHaupt.TabPages.Add(tpErtrag);
            else if (!zeigen && drin)
            {
                if (ReferenceEquals(tabHaupt.SelectedTab, tpErtrag))
                    tabHaupt.SelectedTab = tpKosten;
                tabHaupt.TabPages.Remove(tpErtrag);
            }

            if (zeigen && _ertrag != null) _ertrag.Zeige(name);
        }

        private void cmbVariante_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_fuellt) return;
            RasterAufbauen();
        }

        private void KopfAnzeigen()
        {
            string name = cmbKomponente.SelectedIndex >= 0
                ? (string)cmbKomponente.Items[cmbKomponente.SelectedIndex] : "";
            lblTitel.Text = string.Format(Text_("KDLG_TITEL", "Kostenverwaltung {0}"), name);
            lblUntertitel.Text = rbInvest.Checked
                ? Text_("KDLG_UNTERTITEL_INVEST", "Investitionskosten nach VDI 2067")
                : Text_("KDLG_UNTERTITEL_BETRIEB", "Betriebskosten nach VDI 2067");
            lblSpBetrag.Text = rbInvest.Checked
                ? Text_("KDLG_SP_BETRAG", "Betrag netto [€]")
                : Text_("KDLG_SP_BETRAG_JAHR", "Betrag netto [€/a]");
            lblSpNutzung.Visible = rbInvest.Checked;
        }

        private void VariantenLaden(int? auswaehlenId)
        {
            _fuellt = true;
            _varianten = KostenVorlagenCtrl.Vorlagen(KomponentenId, KategorieId);
            cmbVariante.Items.Clear();
            int index = 0;
            for (int i = 0; i < _varianten.Count; i++)
            {
                cmbVariante.Items.Add(_varianten[i].Name);
                if (auswaehlenId.HasValue && _varianten[i].Id == auswaehlenId.Value) index = i;
            }
            if (cmbVariante.Items.Count > 0) cmbVariante.SelectedIndex = index;
            _fuellt = false;

            RasterAufbauen();
        }

        // -------------------------------------------------------------- Raster ---

        private void RasterAufbauen()
        {
            pnlZeilen.SuspendLayout();
            foreach (ucVorlagenZeile z in _zeilen) { pnlZeilen.Controls.Remove(z); z.Dispose(); }
            _zeilen.Clear();

            KostenVorlageKopf v = Variante;
            bool nurLesen = v == null || v.NurLesen;
            lblReadOnly.Visible = v != null && v.NurLesen;
            btnPositionNeu.Enabled = v != null && !v.NurLesen;
            btnVarianteLoeschen.Enabled = v != null && !v.NurLesen;

            int y = 2;
            if (v != null)
            {
                foreach (KostenVorlagenPosition p in KostenVorlagenCtrl.Positionen(v.Id))
                {
                    ucVorlagenZeile z = ZeileBauen(y);
                    z.Zeige(p, rbInvest.Checked, nurLesen);
                    y += 36;
                }

                // Abschlusszeile „+ Neue Position hinzufügen…" (FK2, Mockup-Muster).
                ucVorlagenZeile neu = ZeileBauen(y);
                neu.ZeigeNeu(v.Id, rbInvest.Checked, nurLesen);
            }
            pnlZeilen.ResumeLayout();
            SummenAnzeigen();
        }

        private ucVorlagenZeile ZeileBauen(int y)
        {
            var z = new ucVorlagenZeile();
            z.Location = new System.Drawing.Point(0, y);
            z.Width = Math.Max(pnlZeilen.ClientSize.Width - 4, 928);
            z.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            z.PositionGeaendert += delegate { SummenAnzeigen(); };
            z.LoeschenAngefordert += Zeile_LoeschenAngefordert;
            z.EditorAngefordert += Zeile_EditorAngefordert;
            z.NeuAngelegt += delegate { RasterAufbauen(); };
            pnlZeilen.Controls.Add(z);
            _zeilen.Add(z);
            return z;
        }

        private void Zeile_LoeschenAngefordert(object sender, KostenVorlagenPosition p)
        {
            if (MessageBox.Show(
                    string.Format(Text_("KDLG_MSG_POS_LOESCHEN", "Position „{0}\" löschen?"), p.Bezeichnung),
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            if (KostenVorlagenCtrl.PositionLoeschen(p.Id)) RasterAufbauen();
        }

        private void Zeile_EditorAngefordert(object sender, KostenVorlagenPosition p)
        {
            using (var dlg = new Form_VorlagenPosition())
            {
                dlg.SetControls(p);
                if (dlg.ShowDialog(this) == DialogResult.OK &&
                    KostenVorlagenCtrl.PositionSpeichern(p))
                    RasterAufbauen();
            }
        }

        private void btnPositionNeu_Click(object sender, EventArgs e)
        {
            KostenVorlageKopf v = Variante;
            if (v == null) return;
            if (v.NurLesen)
            {
                MessageBox.Show(Text_("KDLG_MSG_READONLY", "Auslieferungsvorlagen sind schreibgeschützt."),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string kostenart = rbInvest.Checked
                ? DbWerte.KOSTENART_KAPITALGEBUNDEN : DbWerte.KOSTENART_BETRIEBSGEBUNDEN;
            string bemessung = rbInvest.Checked
                ? DbWerte.BEMESSUNG_BETRAG : DbWerte.BEMESSUNG_JAHRESBETRAG;
            if (KostenVorlagenCtrl.PositionNeu(v.Id,
                    Text_("KDLG_POS_NEU_VORGABE", "Neue Position"), kostenart, bemessung) != 0)
                RasterAufbauen();
        }

        // -------------------------------------------------------------- Summen ---

        /// <summary>
        /// Stammkontext-Summe (§ 5.2): nur absolute Positionen tragen einen Betrag;
        /// Erlöse/Zuschüsse mit negativem Ausweis (L7). Brutto ist reine Anzeige
        /// (KL5), Satz aus dem Gesetzeskatalog (<c>UMSATZSTEUER_REGELSATZ</c>).
        /// </summary>
        private void SummenAnzeigen()
        {
            double netto = 0;
            foreach (ucVorlagenZeile z in _zeilen)
            {
                KostenVorlagenPosition p = z.Position;
                if (p == null) continue;
                double? wert = p.BetragNetto.HasValue ? p.BetragNetto : null;
                if (!wert.HasValue) continue;
                netto += p.IstErloes ? -wert.Value : wert.Value;
            }

            string nettoText = netto.ToString("#,##0.00", CultureInfo.CurrentCulture);
            lblSummeNetto.Text = string.Format(rbInvest.Checked
                    ? Text_("KDLG_SUMME_NETTO_INVEST", "Summe Investitionskosten netto: {0} €")
                    : Text_("KDLG_SUMME_NETTO_BETRIEB", "Summe Betriebskosten netto: {0} €/a"),
                nettoText);

            double? ust = KostenVorlagenCtrl.UstSatzProzent();
            if (ust.HasValue)
            {
                double brutto = netto * (1.0 + ust.Value / 100.0);
                string bruttoText = brutto.ToString("#,##0.00", CultureInfo.CurrentCulture) +
                                    (rbInvest.Checked ? " €" : " €/a");
                lblSummeBrutto.Text = string.Format(
                    Text_("KDLG_SUMME_BRUTTO", "Summe brutto: {0} (Umsatzsteuer {1} % aus dem Katalog)"),
                    bruttoText, ust.Value.ToString("0.#", CultureInfo.CurrentCulture));
                lblSummeBrutto.Visible = true;
            }
            else lblSummeBrutto.Visible = false;
        }

        // ---------------------------------------------------- Variantenpflege ---

        private void btnVarianteNeu_Click(object sender, EventArgs e)
        {
            NeueVariante(false);
        }

        private void btnSpeichernUnter_Click(object sender, EventArgs e)
        {
            NeueVariante(true);
        }

        /// <summary>FK9: Namensschema „‹Name› — Variante ‹n›" als Vorbelegung.</summary>
        private void NeueVariante(bool kopie)
        {
            KostenVorlageKopf quelle = Variante;
            if (kopie && quelle == null) return;

            string basis = cmbKomponente.SelectedIndex >= 0
                ? (string)cmbKomponente.Items[cmbKomponente.SelectedIndex] : "";
            string vorschlag = basis + " — Variante " + (_varianten.Count);

            using (var dlg = new Form_VariantenName())
            {
                dlg.SetControls(
                    kopie ? Text_("KDLG_MSG_KOPIE_TITEL", "Speichern unter")
                          : Text_("KDLG_MSG_NEU_TITEL", "Neue Variante"),
                    Text_("KDLG_MSG_NEU_NAME", "Name der neuen Variante:"),
                    vorschlag);
                if (dlg.ShowDialog(this) != DialogResult.OK) return;

                int neueId = kopie
                    ? KostenVorlagenCtrl.SpeichernUnter(quelle.Id, dlg.Ergebnis)
                    : KostenVorlagenCtrl.VorlageNeu(KomponentenId, KategorieId, dlg.Ergebnis);
                if (neueId == 0)
                {
                    MessageBox.Show(Text_("KDLG_MSG_NAME_BELEGT", "Der Name ist bereits vergeben oder leer."),
                        Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                VariantenLaden(neueId);
            }
        }

        private void btnVarianteLoeschen_Click(object sender, EventArgs e)
        {
            KostenVorlageKopf v = Variante;
            if (v == null) return;
            if (v.NurLesen)
            {
                MessageBox.Show(Text_("KDLG_MSG_READONLY", "Auslieferungsvorlagen sind schreibgeschützt."),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (MessageBox.Show(
                    string.Format(Text_("KDLG_MSG_LOESCHEN", "Variante „{0}\" wirklich löschen?"), v.Name),
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            if (KostenVorlagenCtrl.VorlageLoeschen(v.Id)) VariantenLaden(null);
        }

        // ----------------------------------------------------------- Übernahme ---

        /// <summary>KD3 (§ 8): Übernahme-Dialog — Zielprojekt und Quelle wählen,
        /// Klartext-Vorschau, Schreiben über <see cref="KostenVorlagenUebernahmeCtrl"/>.</summary>
        private void btnUebernahme_Click(object sender, EventArgs e)
        {
            string name = cmbKomponente.SelectedIndex >= 0
                ? (string)cmbKomponente.Items[cmbKomponente.SelectedIndex] : "";
            using (var dlg = new Form_VorlagenUebernahme())
            {
                dlg.SetControls(KomponentenId, name, KategorieId, Variante);
                dlg.ShowDialog(this);
            }
        }

        // -------------------------------------------------------------- Diverses ---

        private void btnBannerZu_Click(object sender, EventArgs e)
        {
            pnlBanner.Visible = false;
        }

        /// <summary>Designer-Vorgaben (deutsch) durch MyResource-Texte ersetzen
        /// (Ä6-Regel 2 — Designer-Vorschau und Lokalisierung bleiben beide intakt).</summary>
        private void TexteAnwenden()
        {
            tpKosten.Text = Text_("KDLG_TAB_KOSTEN", tpKosten.Text);
            tpErtrag.Text = Text_("KDLG_TAB_ERTRAG", tpErtrag.Text);
            lblErtragHinweis.Text = Text_("KDLG_TAB_ERTRAG_HINWEIS", lblErtragHinweis.Text);
            lblBanner.Text = Text_("KDLG_BANNER", lblBanner.Text);
            lblReadOnly.Text = Text_("KDLG_READONLY_HINWEIS", lblReadOnly.Text);
            lblKomponente.Text = Text_("KDLG_LBL_KOMPONENTE", lblKomponente.Text);
            rbInvest.Text = Text_("KDLG_KAT_INVEST", rbInvest.Text);
            rbBetrieb.Text = Text_("KDLG_KAT_BETRIEB", rbBetrieb.Text);
            lblVariante.Text = Text_("KDLG_LBL_VARIANTE", lblVariante.Text);
            btnVarianteNeu.Text = Text_("KDLG_BTN_NEU", btnVarianteNeu.Text);
            btnSpeichernUnter.Text = Text_("KDLG_BTN_SPEICHERN_UNTER", btnSpeichernUnter.Text);
            btnPositionNeu.Text = Text_("KDLG_BTN_POSITION", btnPositionNeu.Text);
            btnUebernahme.Text = Text_("KDLG_BTN_UEBERNAHME", btnUebernahme.Text);
            btnKatalog.Text = Text_("KDLG_BTN_KATALOG", btnKatalog.Text);
            lblSpAktionen.Text = Text_("KDLG_SP_AKTIONEN", lblSpAktionen.Text);
            lblSpPosition.Text = Text_("KDLG_SP_POSITION", lblSpPosition.Text);
            lblSpBemessung.Text = Text_("KDLG_SP_BEMESSUNG", lblSpBemessung.Text);
            lblSpSatz.Text = Text_("KDLG_SP_SATZ", lblSpSatz.Text);
            lblSpNutzung.Text = Text_("KDLG_SP_NUTZUNG", lblSpNutzung.Text);
            lblSpWorstBest.Text = Text_("KDLG_SP_WORSTBEST", lblSpWorstBest.Text);
        }

        /// <summary>§ 3.1/Ä7: Katalogpflege der Positionsbezeichnungen — als
        /// Unterfunktion der Kostenverwaltung statt eines eigenen Menüeintrags.</summary>
        private void btnKatalog_Click(object sender, EventArgs e)
        {
            using (var frm = new Form_KostenAdmin())
                frm.ShowDialog(this);
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
