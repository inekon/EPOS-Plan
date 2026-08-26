using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EINE Zeile des Vorlagen-Positionsrasters (Etappe KD2, Konzept Kostendialoge
    /// Rev. 1.2, § 5.2): Aktionen · Position · Bemessung · Satz 🔗 Betrag ·
    /// Nutzungsdauer · Worst/Best.
    ///
    /// <para><b>Designer-fähig (Ä6):</b> Layout und Controls stehen in
    /// <c>ucVorlagenZeile.Designer.cs</c>; hier liegen nur Daten und Verhalten.
    /// Texte kommen aus <c>MyResource</c> (deutscher Designer-Text ist der Rückfall).</para>
    ///
    /// <para><b>Kopplungsregel (KL4/§ 5.4):</b> Bei absoluter Bemessung sind Satz und
    /// Betrag EIN Wert (das Betragsfeld spiegelt gesperrt); bei bezugsgrößen-abhängiger
    /// Bemessung bleibt der Betrag im Stammkontext leer und gesperrt („—"), die
    /// Bezugsgröße entsteht erst im Projekt.</para>
    ///
    /// <para><b>Neu-Modus (FK2):</b> Die gestrichelte Abschlusszeile der Mockups —
    /// gleiche Optik, legt beim Verlassen mit gefülltem Namen eine Position an;
    /// gleichwertig zum Knopf „+ Position hinzufügen".</para>
    /// </summary>
    public partial class ucVorlagenZeile : UserControl
    {
        private KostenVorlagenPosition _pos;
        private bool _istInvest;
        private bool _nurLesen;
        private bool _neuModus;
        private int _vorlageIdNeu;
        private bool _fuellt;

        /// <summary>Nach jedem erfolgreichen Speichern (Fußsummen nachziehen).</summary>
        public event EventHandler PositionGeaendert;

        /// <summary>Papierkorb gedrückt; der Aufrufer fragt nach und löscht.</summary>
        public event EventHandler<KostenVorlagenPosition> LoeschenAngefordert;

        /// <summary>Stift gedrückt; der Aufrufer öffnet den Zeileneditor (§ 5.2).</summary>
        public event EventHandler<KostenVorlagenPosition> EditorAngefordert;

        /// <summary>Neu-Modus: eine Position wurde angelegt (Raster neu aufbauen).</summary>
        public event EventHandler NeuAngelegt;

        // ---- PROJEKTMODUS (KD6a, § 3.2/§ 5 dritter Kontext) --------------------
        //
        // Die Zeile kennt ihre Persistenz nicht mehr fest: Ohne gesetzte Wege
        // gilt der Vorlagen-Bestand (KostenVorlagenCtrl); der Projektmodus der
        // Kostenverwaltung hängt hier seine Projekt-Schreibwege ein.

        /// <summary>Sichern-Weg; null = Vorlagen-Bestand.</summary>
        public Func<KostenVorlagenPosition, bool> SpeichernWeg;

        /// <summary>Anlegen-Weg (Name, Kostenart, Bemessung) → neue Id; null = Vorlage.</summary>
        public Func<string, string, string, int> NeuWeg;

        /// <summary>true = Projektmodus: satzbasierte Beträge werden BERECHNET
        /// angezeigt (statt „—“), ± öffnet die Worst/Best-Eingabe.</summary>
        public bool ProjektModus;

        /// <summary>± gedrückt (nur Projektmodus); der Aufrufer öffnet Form_CaseEingabe.</summary>
        public event EventHandler<KostenVorlagenPosition> WorstBestAngefordert;

        public ucVorlagenZeile()
        {
            InitializeComponent();
            lblKette.Text = "🔗";
            tip.SetToolTip(lblKette, Text_("KDLG_TT_KETTE",
                "Satz und Betrag netto sind verknüpft und werden bei Eingabe umgerechnet."));
            tip.SetToolTip(btnWorstBest, Text_("KDLG_TT_WORSTBEST",
                "Worst/Best wird je Projektposition gepflegt, nicht in der Stammvorlage."));
        }

        /// <summary>Die dargestellte Position (NULL im Neu-Modus).</summary>
        public KostenVorlagenPosition Position { get { return _pos; } }

        // ------------------------------------------------------------------ Füllen ---

        /// <summary>Bestehende Position anzeigen.</summary>
        public void Zeige(KostenVorlagenPosition p, bool istInvest, bool nurLesen)
        {
            _fuellt = true;
            _pos = p;
            _istInvest = istInvest;
            _nurLesen = nurLesen;
            _neuModus = false;

            BemessungenFuellen(istInvest, p.Bemessung);
            txtBezeichnung.Text = p.Bezeichnung;
            txtSatz.Text = ZahlText(p.Satz);
            txtBetrag.Text = ZahlText(p.BetragNetto);
            txtNutzung.Text = ZahlText(p.Nutzungsdauer);
            txtNutzung.Visible = istInvest;

            bool schreibbar = !nurLesen;
            txtBezeichnung.Enabled = schreibbar;
            cmbBemessung.Enabled = schreibbar;
            txtSatz.Enabled = schreibbar;
            txtNutzung.Enabled = schreibbar;
            btnEditor.Enabled = schreibbar;
            btnLoeschen.Enabled = schreibbar;

            btnWorstBest.Enabled = ProjektModus && schreibbar;
            btnEditor.Visible = !ProjektModus;   // der Zeileneditor gehört zur Vorlage

            KopplungAnwenden();
            EmpfehlungAnzeigen();
            _fuellt = false;
        }

        private void btnWorstBest_Click(object sender, EventArgs e)
        {
            if (ProjektModus && _pos != null && WorstBestAngefordert != null)
                WorstBestAngefordert(this, _pos);
        }

        /// <summary>Abschlusszeile „+ Neue Position hinzufügen…" (FK2).</summary>
        public void ZeigeNeu(int vorlageId, bool istInvest, bool nurLesen)
        {
            _fuellt = true;
            _pos = null;
            _istInvest = istInvest;
            _nurLesen = nurLesen;
            _neuModus = true;
            _vorlageIdNeu = vorlageId;

            BemessungenFuellen(istInvest, null);
            txtBezeichnung.Text = "";
            txtBezeichnung.PlaceholderText = "+ " + Text_("KDLG_POS_NEU_VORGABE", "Neue Position") + "…";
            txtSatz.Text = "";
            txtBetrag.Text = "";
            txtNutzung.Text = "";
            txtNutzung.Visible = istInvest;

            btnEditor.Visible = false;
            btnLoeschen.Visible = false;
            btnWorstBest.Visible = false;
            lblKette.Visible = false;
            txtSatz.Enabled = false;
            txtBetrag.Enabled = false;
            txtNutzung.Enabled = false;
            txtBezeichnung.Enabled = !nurLesen;
            cmbBemessung.Enabled = !nurLesen;
            _fuellt = false;
        }

        // ------------------------------------------------------------- Verhalten ---

        private void btnEditor_Click(object sender, EventArgs e)
        {
            if (_pos != null && EditorAngefordert != null) EditorAngefordert(this, _pos);
        }

        private void btnLoeschen_Click(object sender, EventArgs e)
        {
            if (_pos != null && LoeschenAngefordert != null) LoeschenAngefordert(this, _pos);
        }

        private void Zahl_TextChanged(object sender, EventArgs e)
        {
            if (!_fuellt) Program.ZahlFaerben(sender);
        }

        private void cmbBemessung_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_fuellt) return;
            BemessungKatalog.Info info = GewaehlteBemessung();
            if (info != null) lblEinheit.Text = info.Einheit;
            KopplungAnwenden();
            if (!_neuModus) Speichern();
        }

        private void Feld_Leave(object sender, EventArgs e)
        {
            if (_fuellt) return;
            if (_neuModus) { NeuAnlegenVersuchen(); return; }
            Speichern();
        }

        /// <summary>Felder → Position → Datenbank; danach Kopplung nachziehen.</summary>
        private void Speichern()
        {
            if (_pos == null || _nurLesen) return;

            string name = txtBezeichnung.Text.Trim();
            if (name.Length > 0) _pos.Bezeichnung = name;
            else txtBezeichnung.Text = _pos.Bezeichnung;   // leerer Name: zurücksetzen

            BemessungKatalog.Info info = GewaehlteBemessung();
            if (info != null) _pos.Bemessung = info.Persistenz;

            _pos.Satz = ZahlWert(txtSatz);
            _pos.Nutzungsdauer = _istInvest ? ZahlWert(txtNutzung) : _pos.Nutzungsdauer;

            // KL4/§ 5.4: absolut ⇒ Satz und Betrag sind EIN Wert; sonst bleibt der
            // Betrag im Stammkontext leer (Bezugsgröße erst im Projekt).
            if (info != null && info.Absolut)
                _pos.BetragNetto = _pos.Satz;
            else
                _pos.BetragNetto = null;

            bool gesichert = SpeichernWeg != null
                ? SpeichernWeg(_pos)
                : KostenVorlagenCtrl.PositionSpeichern(_pos);
            if (gesichert)
            {
                _fuellt = true;
                KopplungAnwenden();
                _fuellt = false;
                if (PositionGeaendert != null) PositionGeaendert(this, EventArgs.Empty);
            }
        }

        private void NeuAnlegenVersuchen()
        {
            string name = txtBezeichnung.Text.Trim();
            if (name.Length == 0 || _nurLesen) return;

            BemessungKatalog.Info info = GewaehlteBemessung();
            string bemessung = info != null ? info.Persistenz
                : (_istInvest ? DbWerte.BEMESSUNG_BETRAG : DbWerte.BEMESSUNG_JAHRESBETRAG);
            string kostenart = _istInvest ? DbWerte.KOSTENART_KAPITALGEBUNDEN
                                          : DbWerte.KOSTENART_BETRIEBSGEBUNDEN;

            int neuId = NeuWeg != null
                ? NeuWeg(name, kostenart, bemessung)
                : KostenVorlagenCtrl.PositionNeu(_vorlageIdNeu, name, kostenart, bemessung);
            if (neuId != 0)
            {
                txtBezeichnung.Text = "";
                if (NeuAngelegt != null) NeuAngelegt(this, EventArgs.Empty);
            }
        }

        // --------------------------------------------------------------- Helfer ---

        private void BemessungenFuellen(bool istInvest, string aktuelle)
        {
            cmbBemessung.Items.Clear();
            var eintraege = new List<BemessungKatalog.Info>();
            foreach (BemessungKatalog.Info i in BemessungKatalog.Alle)
                if ((istInvest && i.FuerInvest) || (!istInvest && i.FuerBetrieb) ||
                    string.Equals(i.Persistenz, aktuelle, StringComparison.Ordinal))
                    eintraege.Add(i);

            foreach (BemessungKatalog.Info i in eintraege)
                cmbBemessung.Items.Add(new BemessungEintrag(i));

            if (aktuelle != null)
                for (int n = 0; n < cmbBemessung.Items.Count; n++)
                    if (string.Equals(((BemessungEintrag)cmbBemessung.Items[n]).Info.Persistenz,
                                      aktuelle, StringComparison.Ordinal))
                    { cmbBemessung.SelectedIndex = n; break; }

            BemessungKatalog.Info akt = GewaehlteBemessung();
            lblEinheit.Text = akt != null ? akt.Einheit : "";
        }

        private BemessungKatalog.Info GewaehlteBemessung()
        {
            var e = cmbBemessung.SelectedItem as BemessungEintrag;
            return e == null ? null : e.Info;
        }

        /// <summary>Betragsfeld nach Kopplungsregel sperren/spiegeln.</summary>
        private void KopplungAnwenden()
        {
            BemessungKatalog.Info info = GewaehlteBemessung();
            bool absolut = info != null && info.Absolut;

            txtBetrag.Enabled = false;   // im Stammkontext nie direkt editierbar
            if (absolut)
            {
                txtBetrag.Text = txtSatz.Text;
                lblKette.Visible = true;
                tip.SetToolTip(txtBetrag, Text_("KDLG_TT_KETTE",
                    "Satz und Betrag netto sind verknüpft und werden bei Eingabe umgerechnet."));
            }
            else if (ProjektModus)
            {
                // Projektkontext: Die Bezugsgröße existiert — der BERECHNETE
                // Betrag wird angezeigt (derselbe Rechenweg wie der Rechenkern).
                txtBetrag.Text = _pos != null ? ZahlText(_pos.BetragNetto) : "";
                lblKette.Visible = false;
                tip.SetToolTip(txtBetrag, Text_("KDLG_TT_BETRAG_PROJEKT",
                    "Aus Satz und Bezugsgröße des Projekts berechnet."));
            }
            else
            {
                txtBetrag.Text = "—";
                lblKette.Visible = false;
                tip.SetToolTip(txtBetrag, Text_("KDLG_TT_BETRAG_ADMIN",
                    "Bezugsgröße erst im Projekt bekannt — der Betrag entsteht bei der Übernahme."));
            }
        }

        /// <summary>Empfehlungsbereich als Hinweistext neben dem Satzfeld
        /// (bestehendes Muster <c>Form_Betriebskosten</c>).</summary>
        private void EmpfehlungAnzeigen()
        {
            if (_pos != null && (_pos.EmpfehlungVon.HasValue || _pos.EmpfehlungBis.HasValue))
                tip.SetToolTip(txtSatz, "Empfehlung: " +
                    ZahlText(_pos.EmpfehlungVon) + " – " + ZahlText(_pos.EmpfehlungBis) + " " +
                    lblEinheit.Text);
            else
                tip.SetToolTip(txtSatz, "");
        }

        private static string ZahlText(double? wert)
        {
            return wert.HasValue ? wert.Value.ToString("0.##", CultureInfo.CurrentCulture) : "";
        }

        private static double? ZahlWert(TextBox feld)
        {
            double w;
            if (string.IsNullOrWhiteSpace(feld.Text)) return null;
            return Program.ZahlParsen(feld.Text, out w) ? (double?)w : null;
        }

        private static string Text_(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }

        /// <summary>Listeneintrag der Bemessungs-Klappliste (Anzeigetext aus MyResource).</summary>
        private sealed class BemessungEintrag
        {
            public readonly BemessungKatalog.Info Info;
            public BemessungEintrag(BemessungKatalog.Info info) { Info = info; }
            public override string ToString() { return BemessungKatalog.Anzeige(Info.Persistenz); }
        }
    }
}
