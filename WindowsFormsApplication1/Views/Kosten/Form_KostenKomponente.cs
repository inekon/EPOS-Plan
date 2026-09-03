using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Forms;
using EPOS.UI.Dialoge.Kosten;

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

        // ---- PROJEKTMODUS (KD6a, § 3.2/§ 5 dritter Kontext) ------------------
        private int _idProjekt;
        private string _projektname = "";
        private List<KostenProjektPositionenCtrl.Zeile> _projektZeilen;
        private bool ProjektModus { get { return _idProjekt > 0; } }

        /// <summary>ETAPPE KD5 (§ 6): Inhalt des Reiters „Ertrag/Bonus".</summary>
        private ucErtragBonus _ertrag;

        public Form_KostenKomponente()
        {
            InitializeComponent();
            // H7: Infoknopf in das Kopfband (pnlKopf, Dock Top) - dessen rechtes Ende
            // ist frei, die Beschriftungen stehen links.
            InfoKnopf.Anbringen(this, ziel: pnlKopf);

            // Ä19: OK/Speichern/Abbrechen auch im ADMIN-Kontext (Katalogpflege) —
            // dieselbe Fußleiste wie im Projektmodus; der Designer hält sie
            // ausgeblendet, damit Alt-Aufrufer ohne Kontext nichts Falsches zeigen.
            btnOk.Visible = true;
            btnSpeichern.Visible = true;
            btnAbbrechen.Visible = true;
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
            {
                object it = cmbKomponente.Items[i];
                var w = it as AnlagenWahl;   // Ä20: Projektmodus listet Anlagen
                string name = w != null ? w.Komponente : it as string;
                if (string.Equals(name, komponente, StringComparison.Ordinal))
                { cmbKomponente.SelectedIndex = i; break; }
            }
        }

        /// <summary>Auf die Betriebskosten-Sicht schalten (Aufruf „Betriebskosten…"
        /// aus dem Anlagendialog, KD6).</summary>
        public void WaehleBetrieb()
        {
            rbBetrieb.Checked = true;
        }

        /// <summary>
        /// PROJEKTMODUS (KD6a): Der Dialog pflegt die Tab_ProjektWerte-Positionen
        /// des Projekts — gleiche Optik und Bedienung wie der Stammkontext, aber
        /// ohne Variantenzeile; „Übernehmen“ holt Vorlagen INS Projekt (§ 8),
        /// ± pflegt Worst/Best und Startjahr je Position (§ 11).
        /// </summary>
        public void SetProjekt(int idProjekt, string projektname,
                               string komponente = null, bool betrieb = false,
                               int idAnlage = 0)
        {
            _idProjekt = idProjekt;
            _projektname = projektname ?? "";

            // Ä20 (Nutzerauftrag 26.08.2026): Im Projektmodus wählt die Klappliste
            // ANLAGEN — je Anlagenzeile ein Eintrag „Komponente — Bezeichner“, die
            // Positionen hängen an der Anlage (Tab_ProjektWerte.ID_Anlage,
            // Migrationsschritt 45). Der Ä10-Grundsatz bleibt: Komponenten OHNE
            // Anlage stehen als „(keine Anlage im Projekt)“ bereit, damit die
            // §-8-Übernahme in leere Komponenten möglich bleibt; Positionen ohne
            // (gültige) Zuordnung bekommen den Pflege-Eintrag
            // „(ohne Anlagenzuordnung)“.
            _fuellt = true;
            _komponenten = KostenVorlagenCtrl.Komponenten();   // Name→Id-Landkarte (Ä7)
            AnlagenlisteFuellen(idAnlage, komponente);
            _fuellt = false;

            // Variantenpflege ist Stammsache — im Projekt verschwindet die Zeile.
            lblVariante.Visible = false;
            cmbVariante.Visible = false;
            btnVarianteNeu.Visible = false;
            btnSpeichernUnter.Visible = false;
            btnVarianteLoeschen.Visible = false;
            lblReadOnly.Visible = false;
            btnUebernahme.Text = Text_("KDLG_BTN_UEBERNAHME_PROJEKT",
                                       "Aus Vorlage übernehmen…");

            // Ä12/Ä13: explizite Knöpfe — seit Ä19 in beiden Kontexten sichtbar
            // (der Konstruktor zeigt sie bereits; hier bleibt die Zusicherung).
            btnOk.Visible = true;
            btnSpeichern.Visible = true;
            btnAbbrechen.Visible = true;

            if (betrieb) rbBetrieb.Checked = true;

            Kontext_Geaendert(this, EventArgs.Empty);
        }

        /// <summary>Ä20: ein Klapplisten-Eintrag des Projektmodus — eine Anlage
        /// (AnlageId &gt; 0) oder der Sammel-/Leereintrag einer Komponente.</summary>
        private sealed class AnlagenWahl
        {
            public readonly string Komponente;
            public readonly int AnlageId;
            private readonly string _text;
            public AnlagenWahl(string komponente, int anlageId, string text)
            { Komponente = komponente; AnlageId = anlageId; _text = text; }
            public override string ToString() { return _text; }
        }

        private List<ProjektEnergietraegerCtrl.AnlagenEintrag> _anlagenListe;

        private void AnlagenlisteFuellen(int vorwahlAnlage, string vorwahlKomponente)
        {
            // Ä21: verwaiste Zuordnungen zuerst heilen (Wizard-Neuaufbau).
            try { KostenProjektPositionenCtrl.ZuordnungReparieren(_idProjekt); } catch { }
            _anlagenListe = ProjektEnergietraegerCtrl.AnlagenMitTraeger(_idProjekt);
            HashSet<string> lose = LoseKomponenten();

            cmbKomponente.Items.Clear();
            var mitAnlage = new HashSet<string>(StringComparer.Ordinal);
            int vorwahl = -1;

            foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in _anlagenListe)
            {
                if (!KostenVorlagenCtrl.IstWaehlbar(a.Komponente)) continue;   // Ä7
                mitAnlage.Add(a.Komponente);
                string text = string.IsNullOrEmpty(a.Bezeichner)
                    ? a.Komponente : a.Komponente + " — " + a.Bezeichner;
                int idx = cmbKomponente.Items.Add(new AnlagenWahl(a.Komponente, a.AnlageId, text));
                if (vorwahl < 0 &&
                    ((vorwahlAnlage > 0 && a.AnlageId == vorwahlAnlage) ||
                     (vorwahlAnlage <= 0 && vorwahlKomponente != null &&
                      string.Equals(a.Komponente, vorwahlKomponente, StringComparison.Ordinal))))
                    vorwahl = idx;
            }

            foreach (KeyValuePair<int, string> k in _komponenten)
            {
                bool hatAnlagen = mitAnlage.Contains(k.Value);
                bool hatLose = lose.Contains(k.Value);
                if (hatAnlagen && !hatLose) continue;
                string text = hatLose
                    ? string.Format(Text_("KDLG_ANLAGE_LOSE", "{0} (ohne Anlagenzuordnung)"), k.Value)
                    : string.Format(Text_("KDLG_ANLAGE_KEINE", "{0} (keine Anlage im Projekt)"), k.Value);
                int idx = cmbKomponente.Items.Add(new AnlagenWahl(k.Value, 0, text));
                if (vorwahl < 0 && vorwahlAnlage <= 0 && vorwahlKomponente != null &&
                    string.Equals(k.Value, vorwahlKomponente, StringComparison.Ordinal))
                    vorwahl = idx;
            }

            if (cmbKomponente.Items.Count > 0)
                cmbKomponente.SelectedIndex = vorwahl >= 0 ? vorwahl : 0;
        }

        /// <summary>Komponenten, die Positionen ohne (gültige) Anlagenzuordnung
        /// führen — sie brauchen den Pflege-Eintrag.</summary>
        private HashSet<string> LoseKomponenten()
        {
            var s = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                var ids = new HashSet<int>();
                foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in _anlagenListe)
                    ids.Add(a.AnlageId);
                foreach (int kat in new[] { Form_Kosten.KATEGORIE_INVESTITION,
                                            Form_Kosten.KATEGORIE_BETRIEB })
                {
                    System.Data.DataTable t = Form_Kosten.LiesAnlagenSummen(_idProjekt, kat);
                    if (t == null) continue;
                    foreach (System.Data.DataRow r in t.Rows)
                    {
                        bool loseZeile = r["ID_Anlage"] == DBNull.Value ||
                                         !ids.Contains(Convert.ToInt32(r["ID_Anlage"]));
                        if (loseZeile) s.Add(Convert.ToString(r["Komponente"]));
                    }
                }
            }
            catch { }
            return s;
        }

        /// <summary>Komponente des gewählten Eintrags — im Projektmodus aus der
        /// AnlagenWahl, im Stammkontext der Listentext (Ä20).</summary>
        private string AktuelleKomponente
        {
            get
            {
                object it = cmbKomponente.SelectedIndex >= 0
                    ? cmbKomponente.Items[cmbKomponente.SelectedIndex] : null;
                var w = it as AnlagenWahl;
                return w != null ? w.Komponente : (it as string ?? "");
            }
        }

        /// <summary>Ä20: Anlagenzeile des gewählten Eintrags (0 = ohne Anlage).</summary>
        private int AnlagenId
        {
            get
            {
                object it = cmbKomponente.SelectedIndex >= 0
                    ? cmbKomponente.Items[cmbKomponente.SelectedIndex] : null;
                var w = it as AnlagenWahl;
                return w != null ? w.AnlageId : 0;
            }
        }

        // ------------------------------------------------------------- Kontext ---

        private int KomponentenId
        {
            get
            {
                if (ProjektModus)
                {
                    // Ä20: Die Klappliste führt Anlagen — die Komponenten-Id kommt
                    // aus der Namenslandkarte.
                    string name = AktuelleKomponente;
                    foreach (KeyValuePair<int, string> k in _komponenten)
                        if (string.Equals(k.Value, name, StringComparison.Ordinal)) return k.Key;
                    return 0;
                }
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
            if (ProjektModus) RasterAufbauen();
            else VariantenLaden(null);
            ErtragReiterSteuern();
        }

        /// <summary>
        /// ETAPPE KD5 / FK5: Der Reiter „Ertrag/Bonus" existiert nur für BHKW und
        /// Photovoltaik — bei allen übrigen Komponenten wird die Reiterseite
        /// ENTFERNT (nicht nur geleert), damit sie gar nicht erst anwählbar ist.
        /// </summary>
        private void ErtragReiterSteuern()
        {
            string name = AktuelleKomponente;
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
            string name = AktuelleKomponente;
            // Ä20: Der Projektmodus trägt die ANLAGE im Titel („Kostenverwaltung
            // Wärmepumpe — CS5800i … — Projekt“).
            string anzeige = cmbKomponente.SelectedIndex >= 0
                ? cmbKomponente.Items[cmbKomponente.SelectedIndex].ToString() : name;
            lblTitel.Text = ProjektModus
                ? string.Format(Text_("KDLG_TITEL_PROJEKT", "Kostenverwaltung {0} — {1}"),
                                anzeige, _projektname)
                : string.Format(Text_("KDLG_TITEL", "Kostenverwaltung {0}"), name);
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

            if (ProjektModus) { ProjektRasterAufbauen(); return; }

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

        /// <summary>Projektzweig des Rasters (KD6a): Zeilen aus
        /// <see cref="KostenProjektPositionenCtrl"/>, Schreibwege injiziert.</summary>
        private void ProjektRasterAufbauen()
        {
            btnPositionNeu.Enabled = KomponentenId > 0;
            // Ä20: nur die Positionen der gewählten Anlage (0 = ohne Zuordnung).
            _projektZeilen = KomponentenId > 0
                ? KostenProjektPositionenCtrl.Lies(_idProjekt, KomponentenId, KategorieId, AnlagenId)
                : new List<KostenProjektPositionenCtrl.Zeile>();

            int y = 2;
            foreach (KostenProjektPositionenCtrl.Zeile pz in _projektZeilen)
            {
                ucVorlagenZeile z = ZeileBauen(y);
                ProjektWegeSetzen(z);
                z.Zeige(pz.Raster, rbInvest.Checked, false);
                y += 36;
            }
            if (KomponentenId > 0)
            {
                ucVorlagenZeile neu = ZeileBauen(y);
                ProjektWegeSetzen(neu);
                neu.ZeigeNeu(0, rbInvest.Checked, false);
            }
            pnlZeilen.ResumeLayout();
            SummenAnzeigen();
        }

        private void ProjektWegeSetzen(ucVorlagenZeile z)
        {
            z.ProjektModus = true;
            z.NurExplizitSpeichern = true;   // Ä12: erst „Speichern“ schreibt
            z.SpeichernWeg = ProjektZeileSichern;
            z.NeuWeg = (name, kostenart, bemessung) =>
                KostenProjektPositionenCtrl.Neu(_idProjekt, KomponentenId, KategorieId,
                                                name, kostenart, bemessung, AnlagenId);
            z.WorstBestAngefordert += Zeile_WorstBestAngefordert;
        }

        private bool ProjektZeileSichern(KostenVorlagenPosition raster)
        {
            KostenProjektPositionenCtrl.Zeile pz = ProjektZeileZu(raster);
            return pz != null && KostenProjektPositionenCtrl.Speichern(pz);
        }

        private KostenProjektPositionenCtrl.Zeile ProjektZeileZu(KostenVorlagenPosition raster)
        {
            if (_projektZeilen == null || raster == null) return null;
            foreach (KostenProjektPositionenCtrl.Zeile pz in _projektZeilen)
                if (ReferenceEquals(pz.Raster, raster)) return pz;
            return null;
        }

        /// <summary>± im Projektmodus: Worst/Best + Startjahr über den KD6-Dialog
        /// (<see cref="Form_CaseEingabe"/>) — dieselbe Eingabe wie der Kosteneditor.</summary>
        private void Zeile_WorstBestAngefordert(object sender, KostenVorlagenPosition raster)
        {
            KostenProjektPositionenCtrl.Zeile pz = ProjektZeileZu(raster);
            if (pz == null) return;

            // iU9-W1.3: der KD6-Dialog als Razor-Komponente; Form_CaseEingabe ist
            // im selben Schritt gelöscht (Regel M1).
            //
            // OHNE Zuschuss-Schalter (Befund iU9-W1.3, A-6): Die Maske zeigte ihn
            // hier zwar — der Konstruktor bekam eine frische KostenPosition mit
            // leerer Kostenart, und eine leere Kostenart zählt als Investition —,
            // aber dieser Aufrufer las `daten.IstZuschuss` danach nie zurück; er
            // schreibt über `pz` (KostenProjektPositionenCtrl.Zeile), das die
            // Größe gar nicht führt. Der Haken war also folgenlos. Der einzige
            // Aufrufer, der ihn auswertet, ist ucKostenItem.
            CaseEingabeErgebnis ergebnis = null;
            BlazorDialogForm<CaseEingabeDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Betrag"] = raster.BetragNetto ?? 0,
                ["BestCase"] = pz.Best,
                ["WorstCase"] = pz.Worst,
                ["BestNutzungsdauer"] = pz.BestNutzung,
                ["WorstNutzungsdauer"] = pz.WorstNutzung,
                ["StartJahr"] = pz.StartJahr,
                ["IstZuschuss"] = false,
                ["ZuschussMoeglich"] = false,
                ["IstErloes"] = raster.IstErloes,

                ["TitelText"] = Text_("KCASE_TITEL", "Eingabe Worst/Best Case"),
                ["LabelAbsolut"] = Text_("KOSTEN_CASE_ABSOLUT", "Eingabe absolut [€]"),
                ["LabelProzent"] = Text_("KOSTEN_CASE_PROZENT", "Eingabe in % vom Erwartungswert"),
                ["VorlageUmrechnung"] = Text_("KOSTEN_CASE_UMRECHNUNG", "ergibt: Best {0:N2} € · Worst {1:N2} €"),
                ["LabelKosten"] = Text_("KCASE_G_KOSTEN", "Kosten:"),
                ["LabelNutzungsdauer"] = Text_("KCASE_G_NUTZUNGSDAUER", "Nutzungsdauer:"),
                ["LabelBestKosten"] = Text_("KCASE_BEST_EUR", "Best Case [€]:"),
                ["LabelWorstKosten"] = Text_("KCASE_WORST_EUR", "Worst Case [€]:"),
                ["LabelBestNutzung"] = Text_("KCASE_BEST_A", "Best Case [a]:"),
                ["LabelWorstNutzung"] = Text_("KCASE_WORST_A", "Worst Case [a]:"),
                ["LabelStartJahr"] = Text_("KOSTEN_CASE_STARTJAHR",
                    "Startjahr (0 = sofort; Jahr X: Zahlung/Betrieb ab X):"),
                ["LabelZuschuss"] = MyResource.Resource.KOSTEN_CHK_ZUSCHUSS,
                ["HinweisZuschuss"] = MyResource.Resource.KOSTEN_CHK_ZUSCHUSS_HINT,
                ["HinweisErloes"] = Text_("KCASE_ERLOES_HINWEIS",
                    "Erlösposition: Die Werte werden als Betrag eingegeben; das negative Vorzeichen setzt die Rechnung."),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,

                ["Geschlossen"] = Microsoft.AspNetCore.Components.EventCallback.Factory
                    .Create<CaseEingabeErgebnis>(this, e =>
                    {
                        ergebnis = e;
                        if (dlg != null) dlg.Schliessen(e != null);
                    })
            };

            dlg = new BlazorDialogForm<CaseEingabeDialog>(
                Text_("KCASE_TITEL", "Eingabe Worst/Best Case"),
                new System.Drawing.Size(560, 620), werte);

            using (dlg)
            {
                if (dlg.ShowDialog(this) != DialogResult.OK || ergebnis == null) return;
                pz.Best = ergebnis.BestCase;
                pz.Worst = ergebnis.WorstCase;
                pz.BestNutzung = ergebnis.BestNutzungsdauer;
                pz.WorstNutzung = ergebnis.WorstNutzungsdauer;
                pz.StartJahr = ergebnis.StartJahr;
                KostenProjektPositionenCtrl.CaseSichern(pz);
            }
        }

        private ucVorlagenZeile ZeileBauen(int y)
        {
            var z = new ucVorlagenZeile();
            // Ä19 (Nutzerauftrag 26.08.2026): Die Ä12-Semantik gilt jetzt in BEIDEN
            // Kontexten — Feldänderungen leben bis „Speichern“/„OK“ nur im Objekt,
            // „Abbrechen“ und Kontextwechsel verwerfen sie. Anlegen/Löschen/Editor/±
            // schreiben weiter sofort (eigene Bestätigungen).
            z.NurExplizitSpeichern = true;
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
            // ETAPPE H3 (H1-2): Pflichtpositionen (Schritt 59) sind nicht löschbar —
            // der Ausweg ist der Satz bzw. Betrag 0 (Entscheidung P1). Die zweite
            // Schicht sitzt in KostenProjektPositionenCtrl.Loeschen, dasselbe Doppel
            // wie beim ReadOnly-Schutz der Kataloge.
            if (ProjektModus && KostenProjektPositionenCtrl.IstPflicht(p.Id))
            {
                MessageBox.Show(
                    string.Format(Text_("KDLG_MSG_PFLICHT_LOESCHEN",
                        "„{0}\" ist eine Pflichtposition dieser Komponente und kann nicht gelöscht werden.\r\nZum Deaktivieren den Satz bzw. Betrag auf 0 setzen."),
                        p.Bezeichnung),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(
                    string.Format(Text_("KDLG_MSG_POS_LOESCHEN", "Position „{0}\" löschen?"), p.Bezeichnung),
                    Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            bool geloescht = ProjektModus
                ? KostenProjektPositionenCtrl.Loeschen(p.Id)
                : KostenVorlagenCtrl.PositionLoeschen(p.Id);
            if (geloescht) RasterAufbauen();
        }

        /// <summary>
        /// Die Kostenarten nach VDI 2067 in der Reihenfolge der Klappliste —
        /// wortgleich aus der gelöschten Maske <c>Form_VorlagenPosition</c>
        /// übernommen (iU9-W1.1). Der Index in diesem Feld IST die
        /// <c>KostenartId</c>, die der Blazor-Dialog zurückgibt.
        /// </summary>
        private static readonly string[] KOSTENARTEN =
        {
            DbWerte.KOSTENART_KAPITALGEBUNDEN,
            DbWerte.KOSTENART_BEDARFSGEBUNDEN,
            DbWerte.KOSTENART_BETRIEBSGEBUNDEN,
            DbWerte.KOSTENART_SONSTIGE,
            DbWerte.KOSTENART_ZUSCHUSS,
        };

        /// <summary>Anzeigetext einer Kostenart — wortgleich aus
        /// <c>Form_VorlagenPosition.KostenartAnzeige</c> (iU9-W1.1).</summary>
        private static string KostenartAnzeige(string persistenz)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString("KOSTENART_" + persistenz); }
            catch { }
            if (!string.IsNullOrEmpty(t)) return t;
            switch (persistenz)
            {
                case "KAPITALGEBUNDEN": return "kapitalgebunden";
                case "BEDARFSGEBUNDEN": return "bedarfsgebunden";
                case "BETRIEBSGEBUNDEN": return "betriebsgebunden";
                case "ZUSCHUSS": return "Zuschuss";
                default: return "sonstige";
            }
        }

        /// <summary>Die Kostenarten als Einträge des Auswahlfeldes (Id = Index).</summary>
        private static List<Tuple<int, string>> KostenartEintraege()
        {
            var liste = new List<Tuple<int, string>>();
            for (int i = 0; i < KOSTENARTEN.Length; i++)
                liste.Add(Tuple.Create(i, KostenartAnzeige(KOSTENARTEN[i])));
            return liste;
        }

        /// <summary>
        /// ✏️ Zeileneditor — seit iU9-W1.1 die Razor-Komponente
        /// <see cref="VorlagenPositionDialog"/> in der Hülle
        /// <see cref="BlazorDialogForm{T}"/>; die WinForms-Maske
        /// <c>Form_VorlagenPosition</c> ist im selben Schritt gelöscht (Regel M1).
        ///
        /// <para>Der Dialog bleibt datenbankfrei: Er bekommt die Kostenartenliste
        /// fertig und gibt reine Werte zurück. Das Eintragen in die Position und
        /// das Speichern stehen weiter hier — genau wie vorher in
        /// <c>btnOk_Click</c> der Maske.</para>
        /// </summary>
        private void Zeile_EditorAngefordert(object sender, KostenVorlagenPosition p)
        {
            var eintraege = new List<ValueTuple<int, string>>();
            foreach (Tuple<int, string> e in KostenartEintraege())
                eintraege.Add(new ValueTuple<int, string>(e.Item1, e.Item2));

            int vorwahl = Array.IndexOf(KOSTENARTEN, p.Kostenart ?? "");

            VorlagenPositionErgebnis ergebnis = null;
            BlazorDialogForm<VorlagenPositionDialog> dlg = null;

            var werte = new Dictionary<string, object>
            {
                ["Kostenarten"] = (IReadOnlyList<ValueTuple<int, string>>)eintraege,
                ["Bezeichnung"] = p.Bezeichnung ?? "",
                ["KostenartId"] = vorwahl >= 0 ? (int?)vorwahl : null,
                ["IstErloes"] = p.IstErloes,
                ["EmpfehlungVon"] = p.EmpfehlungVon,
                ["EmpfehlungBis"] = p.EmpfehlungBis,

                ["TitelText"] = Text_("VPOS_TITEL", "Position bearbeiten"),
                ["LabelBezeichnung"] = Text_("VPOS_LBL_BEZEICHNUNG", "Bezeichnung:"),
                ["LabelKostenart"] = Text_("VPOS_LBL_KOSTENART", "Kostenart:"),
                ["LabelErloes"] = Text_("VPOS_CHK_ERLOES", "Erlös/Zuschuss (negativer Ausweis)"),
                ["LabelEmpfehlungVon"] = Text_("VPOS_LBL_EMPFEHLUNG", "Empfehlung von/bis:"),
                ["LabelEmpfehlungBis"] = Text_("VPOS_LBL_BIS", "bis"),
                ["MeldungNameFehlt"] = Text_("VPOS_MSG_NAME_FEHLT", "Bitte eine Bezeichnung eingeben."),
                ["OkText"] = MyResource.Resource.ALLG_BTN_OK,
                ["AbbrechenText"] = MyResource.Resource.ALLG_BTN_ABBRECHEN,

                ["Geschlossen"] = Microsoft.AspNetCore.Components.EventCallback.Factory
                    .Create<VorlagenPositionErgebnis>(this, e =>
                    {
                        ergebnis = e;
                        if (dlg != null) dlg.Schliessen(e != null);
                    })
            };

            dlg = new BlazorDialogForm<VorlagenPositionDialog>(
                Text_("VPOS_TITEL", "Position bearbeiten"),
                new System.Drawing.Size(560, 440), werte);

            using (dlg)
            {
                if (dlg.ShowDialog(this) != DialogResult.OK || ergebnis == null) return;

                // Wortgleich zu btnOk_Click der gelöschten Maske.
                p.Bezeichnung = ergebnis.Bezeichnung;
                p.Kostenart = KOSTENARTEN[Math.Max(0, Math.Min(KOSTENARTEN.Length - 1, ergebnis.KostenartId))];
                p.IstErloes = ergebnis.IstErloes;
                p.EmpfehlungVon = ergebnis.EmpfehlungVon;
                p.EmpfehlungBis = ergebnis.EmpfehlungBis;

                // Ä12: Der Editor hat sein eigenes OK — er schreibt SOFORT,
                // im Projektmodus über den Projekt-Schreibweg.
                bool ok = ProjektModus
                    ? ProjektZeileSichern(p)
                    : KostenVorlagenCtrl.PositionSpeichern(p);
                if (ok) RasterAufbauen();
            }
        }

        /// <summary>Ä12/Ä19: übernimmt alle Zeilenfelder und schreibt sie in das
        /// AKTUELLE Ziel — Projektpositionen im Projektmodus, die Katalogvorlage im
        /// Adminkontext (seit Ä19 gilt die deferred-Semantik in beiden Kontexten).
        /// Die Fußsumme bestätigt mit Uhrzeit.</summary>
        /// <summary>Ä13: OK = speichern und verlassen.</summary>
        private void btnOk_Click(object sender, EventArgs e)
        {
            foreach (ucVorlagenZeile z in _zeilen) z.JetztSpeichern();
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnSpeichern_Click(object sender, EventArgs e)
        {
            foreach (ucVorlagenZeile z in _zeilen) z.JetztSpeichern();
            RasterAufbauen();
            lblSummeNetto.Text += "   — " + string.Format(
                Text_("KDLG_GESPEICHERT", "gespeichert {0:HH:mm} Uhr"), DateTime.Now);
        }

        /// <summary>Ä12: schließen OHNE Datenübernahme — ungespeicherte
        /// Feldänderungen verfallen (Anlegen/Löschen/Editor/± haben eigene
        /// Bestätigungen und sind bereits geschrieben).</summary>
        private void btnAbbrechen_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void btnPositionNeu_Click(object sender, EventArgs e)
        {
            if (ProjektModus)
            {
                string kostenartP = rbInvest.Checked
                    ? DbWerte.KOSTENART_KAPITALGEBUNDEN : DbWerte.KOSTENART_BETRIEBSGEBUNDEN;
                string bemessungP = rbInvest.Checked
                    ? DbWerte.BEMESSUNG_BETRAG : DbWerte.BEMESSUNG_JAHRESBETRAG;
                if (KostenProjektPositionenCtrl.Neu(_idProjekt, KomponentenId, KategorieId,
                        Text_("KDLG_POS_NEU_VORGABE", "Neue Position"), kostenartP, bemessungP) != 0)
                    RasterAufbauen();
                return;
            }

            KostenVorlageKopf v = Variante;
            if (v == null) return;
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

            // iU9-W1.2: Die Namensabfrage ist die Razor-Komponente NamensDialog;
            // Form_VariantenName ist im selben Schritt gelöscht (Regel M1).
            string name = NamensDialogHuelle.Fragen(this,
                kopie ? Text_("KDLG_MSG_KOPIE_TITEL", "Speichern unter")
                      : Text_("KDLG_MSG_NEU_TITEL", "Neue Variante"),
                Text_("KDLG_MSG_NEU_NAME", "Name der neuen Variante:"),
                vorschlag,
                Text_("NAMD_MSG_LEER", "Bitte einen Namen eingeben."));
            if (name == null) return;

            int neueId = kopie
                ? KostenVorlagenCtrl.SpeichernUnter(quelle.Id, name)
                : KostenVorlagenCtrl.VorlageNeu(KomponentenId, KategorieId, name);
            if (neueId == 0)
            {
                MessageBox.Show(Text_("KDLG_MSG_NAME_BELEGT", "Der Name ist bereits vergeben oder leer."),
                    Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            VariantenLaden(neueId);
        }

        private void btnVarianteLoeschen_Click(object sender, EventArgs e)
        {
            KostenVorlageKopf v = Variante;
            if (v == null) return;
            if (v.IstStandard)
            {
                // Ä8-Restschutz: Die Standardvorlage ist Quelle von „Speichern
                // unter…" und Übernahme — Varianten sind löschbar, sie nicht.
                MessageBox.Show(Text_("KDLG_MSG_STANDARD_LOESCHEN",
                        "Die Standardvorlage kann nicht gelöscht werden — Varianten schon."),
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
            // iU9-W1.4: der Übernahme-Dialog als Razor-Komponente über
            // VorlagenUebernahmeHuelle; Form_VorlagenUebernahme ist im selben
            // Schritt gelöscht (Regel M1).
            //
            // Ä11: Im Projektmodus steht das Ziel fest; die Quellvorlage
            // (Standard oder Variante des Admin-Katalogs) wählt der Dialog.
            // Ä20: übernommen wird in die GEWÄHLTE Anlage.
            VorlagenUebernahmeHuelle.Oeffnen(this, KomponentenId, AktuelleKomponente, KategorieId,
                                             ProjektModus ? null : Variante,
                                             ProjektModus ? _idProjekt : 0,
                                             ProjektModus ? AnlagenId : 0);

            // Projektmodus: Übernommene Positionen sofort zeigen (§ 8-Fluss).
            if (ProjektModus) RasterAufbauen();
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
            btnSpeichern.Text = Text_("KDLG_BTN_SPEICHERN", btnSpeichern.Text);
            btnAbbrechen.Text = Text_("KDLG_BTN_ABBRECHEN", btnAbbrechen.Text);
            btnOk.Text = Text_("KDLG_BTN_OK", btnOk.Text);
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
