using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Inhalt des Reiters „Ertrag/Bonus" im Komponenten-Kostendialog (Etappe KD5,
    /// Konzept Kostendialoge § 6) — reine ANZEIGE vorhandener Wahrheiten, keine
    /// Zweitpflege:
    ///
    /// <para><b>BHKW (§ 6.1):</b> KWKG-Zuschlagstabelle (Folie 10) und
    /// Steuervergünstigungen aus dem Gesetzeskatalog — es sind exakt die
    /// Katalogschlüssel, mit denen der <see cref="KwkgSatzRechner"/> rechnet
    /// (Abnahmekriterium KD5); Förderdauer samt Jahresdeckel-Reihe; FK7-Vermerk:
    /// der Strompreis-Teil der Einspeisung bleibt Tarifstruktur, die
    /// Projekt-Parameter (Tatbestand, Anlagenart, Kontingent) bleiben in der
    /// Wirtschaftlichkeits-/Anlagenpflege. Sprungknopf auf
    /// <c>Form_Gesetzesparameter</c>.</para>
    ///
    /// <para><b>Photovoltaik (§ 6.2):</b> öffnet DASSELBE
    /// <see cref="Form_PhotovoltaikVerguetung"/> wie der Knopf der
    /// Wirtschaftlichkeit — stammprojektbezogen; im Admin-Kontext wählt eine
    /// Klappliste das Stammprojekt (eine Vergütungswahrheit, V4/F7).</para>
    ///
    /// <para><b>Übrige Komponenten (§ 6.3, FK5):</b> der Reiter wird vom
    /// Eigner ausgeblendet (<see cref="HatInhalt"/>).</para>
    /// </summary>
    public partial class ucErtragBonus : UserControl
    {
        private IList<KeyValuePair<int, string>> _projekte;

        public ucErtragBonus()
        {
            InitializeComponent();
            TexteAnwenden();
        }

        /// <summary>FK5: nur BHKW und Photovoltaik führen den Reiter.</summary>
        public static bool HatInhalt(string komponente)
        {
            return string.Equals(komponente, DbWerte.KOSTEN_KOMPONENTE_BHKW, StringComparison.Ordinal) ||
                   string.Equals(komponente, DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK, StringComparison.Ordinal);
        }

        /// <summary>Inhalt für die gewählte Komponente aufbauen.</summary>
        public void Zeige(string komponente)
        {
            bool bhkw = string.Equals(komponente, DbWerte.KOSTEN_KOMPONENTE_BHKW, StringComparison.Ordinal);
            bool pv = string.Equals(komponente, DbWerte.KOSTEN_KOMPONENTE_PHOTOVOLTAIK, StringComparison.Ordinal);

            grpKwkg.Visible = grpDauer.Visible = grpSteuern.Visible = grpVerweise.Visible = bhkw;
            grpPv.Visible = pv;
            lblLeer.Visible = !bhkw && !pv;

            if (bhkw) BhkwFuellen();
            if (pv) PvFuellen();
        }

        // ================================================================ BHKW ---

        /// <summary>
        /// Werte aus DENSELBEN Katalogschlüsseln, die der KwkgSatzRechner liest —
        /// die Anzeige kann dem Rechner nicht davonlaufen (Abnahme KD5).
        /// </summary>
        private void BhkwFuellen()
        {
            GesetzKatalog.StelleKatalogSicher();
            GesetzKatalog k = new GesetzKatalog();
            int jahr = DateTime.Now.Year;
            CultureInfo ci = CultureInfo.CurrentCulture;

            Func<string, string> w = schluessel =>
            {
                double? wert = k.Wert(schluessel, jahr);
                return wert.HasValue ? wert.Value.ToString("0.0#", ci) : "—";
            };

            lblEinspeisung.Text =
                Z("KDLG_ERTRAG_T50", "bis 50 kW", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS50KW)) +
                Z("KDLG_ERTRAG_T100", "über 50 bis 100 kW", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS100KW)) +
                Z("KDLG_ERTRAG_T250", "über 100 bis 250 kW", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS250KW)) +
                Z("KDLG_ERTRAG_T2MW", "über 250 kW bis 2 MW", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_BIS2MW)) +
                Z("KDLG_ERTRAG_UE2MW", "über 2 MW (neu/modernisiert)", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_UEBER2MW)) +
                Z("KDLG_ERTRAG_UE2MWN", "über 2 MW (nachgerüstet)", w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EINSP_UEBER2MW_NACHGER));

            lblSonderregel.Text = string.Format(ci,
                T("KDLG_ERTRAG_SONDERREGEL",
                  "Sonderregel neue Anlagen ≤ 50 kWel (§ 7 Abs. 3a): eingespeist {0} · " +
                  "nicht eingespeist {1} ct/kWh — geht Abs. 1 und 2 vor."),
                w(DbWerte.GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EINSP),
                w(DbWerte.GESETZ_KWKG_ZUSCHLAG_NEU_BIS50KW_EIGEN));

            lblEigen.Text = string.Format(ci,
                T("KDLG_ERTRAG_EIGEN",
                  "Selbst genutzter KWK-Strom (§ 7 Abs. 2, nur in den Tatbeständen des " +
                  "§ 6 Abs. 3 — z. B. Anlage ≤ 100 kW): bis 50 kW {0} · 50–100 kW {1} ct/kWh. " +
                  "Tatbestand und Anlagenart werden JE ANLAGE in der Wirtschaftlichkeit " +
                  "gepflegt (KWKG-Module) — hier keine Zweitpflege."),
                w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS50KW),
                w(DbWerte.GESETZ_KWKG_ZUSCHLAG_EIGEN_N1_BIS100KW));

            // Dauer: Vollbenutzungsstunden-Kontingent + Jahresdeckel-Reihe.
            StringBuilder deckel = new StringBuilder();
            foreach (KeyValuePair<int, double> p in k.Reihe(DbWerte.GESETZ_KWKG_VBH_JAHRESDECKEL))
                deckel.AppendFormat(ci, "{0}: {1:N0} · ", p.Key, p.Value);
            double? vbh = k.Wert(DbWerte.GESETZ_KWKG_VBH_NEUANLAGE, jahr);
            lblDauer.Text = string.Format(ci,
                T("KDLG_ERTRAG_DAUER",
                  "Neue Anlagen: {0} Vollbenutzungsstunden Förderkontingent. " +
                  "Jahresdeckel [Vbh/a]: {1}(Kontingent-Override je Anlage in der Wirtschaftlichkeit)."),
                vbh.HasValue ? vbh.Value.ToString("N0", ci) : "—", deckel.ToString());

            lblSteuern.Text =
                string.Format(ci, T("KDLG_ERTRAG_ST_BEFREIUNG",
                    "Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3 StromStG: hocheffiziente Anlagen " +
                    "≤ 2 MW im räumlichen Zusammenhang (4,5 km); ab 2026 CO₂-Kriterium.")) + "\n\n" +
                string.Format(ci, T("KDLG_ERTRAG_ST_53A",
                    "Energiesteuer-Entlastung § 53a Abs. 5 EnergieStG: Erdgas {0} €/MWh · " +
                    "Heizöl {1} €/1.000 l · Flüssiggas {2} €/1.000 kg (Mindestnutzungsgrad {3} %)."),
                    w(DbWerte.GESETZ_ENERGIEST_53A5_ERDGAS),
                    w(DbWerte.GESETZ_ENERGIEST_53A5_HEIZOEL_EL),
                    w(DbWerte.GESETZ_ENERGIEST_53A5_FLUESSIGGAS),
                    w(DbWerte.GESETZ_ENERGIEST_53A_NUTZUNGSGRAD)) + "\n\n" +
                string.Format(ci, T("KDLG_ERTRAG_ST_9B",
                    "Stromsteuer-Entlastung § 9b StromStG: {0} €/MWh, Sockelbetrag {1} €/a."),
                    w(DbWerte.GESETZ_STROMST_ENTLASTUNG_9B),
                    w(DbWerte.GESETZ_STROMST_SOCKELBETRAG_9B));

            lblFk7.Text = T("KDLG_ERTRAG_FK7",
                "FK7: Der STROMPREIS-Teil der BHKW-Einspeisevergütung bleibt in der " +
                "Tarifstruktur des Projekts (Einsp_* ist rein KWK) — dieser Reiter zeigt " +
                "die gesetzlichen KWKG-/Steuergrößen an; gerechnet wird ausschließlich vom " +
                "KwkgSatzRechner und den Steuer-Gutschriftrechnern der Wirtschaftlichkeit. " +
                "Projektbezogene Schalter (Tatbestand, Anlagenart, Pauschalmodus § 9, " +
                "Kontingent-Override) werden dort je Anlage gepflegt.");
        }

        private static string Z(string schluessel, string rueckfall, string wert)
        {
            string beschriftung = T(schluessel, rueckfall);
            return (beschriftung + ":").PadRight(34) + wert.PadLeft(6) + " ct/kWh\n";
        }

        private void btnGesetze_Click(object sender, EventArgs e)
        {
            using (var dlg = new Form_Gesetzesparameter())
                dlg.ShowDialog(FindForm());
            BhkwFuellen();   // Katalog kann sich geändert haben
        }

        // =========================================================== Photovoltaik ---

        private void PvFuellen()
        {
            lblPvErklaerung.Text = T("KDLG_ERTRAG_PV",
                "Die PV-Vergütung wird STAMMPROJEKTBEZOGEN im Vergütungsdialog gepflegt — " +
                "demselben Formular, das auch der Knopf „Photovoltaik…“ im " +
                "Wirtschaftlichkeits-Reiter öffnet (eine Vergütungswahrheit, Befund V4). " +
                "Anzulegender Wert, Vermarktungsform, § 51/§ 51a und 60-%-Begrenzung " +
                "wirken über die PV-Erlösreihe direkt in der Kapitalwertrechnung.");

            if (_projekte == null)
            {
                _projekte = KostenVorlagenUebernahmeCtrl.Projekte();
                cmbPvProjekt.Items.Clear();
                foreach (KeyValuePair<int, string> p in _projekte)
                    cmbPvProjekt.Items.Add(p.Value);
                if (cmbPvProjekt.Items.Count > 0) cmbPvProjekt.SelectedIndex = 0;
            }
            btnPvOeffnen.Enabled = cmbPvProjekt.Items.Count > 0;
        }

        private void btnPvOeffnen_Click(object sender, EventArgs e)
        {
            int i = cmbPvProjekt.SelectedIndex;
            if (_projekte == null || i < 0 || i >= _projekte.Count) return;
            using (var dlg = new Form_PhotovoltaikVerguetung())
            {
                dlg.SetControls(_projekte[i].Key);
                dlg.ShowDialog(FindForm());
            }
        }

        // ================================================================ Texte ---

        private void TexteAnwenden()
        {
            grpKwkg.Text = T("KDLG_ERTRAG_G_KWKG",
                "KWKG-Zuschlag (§ 7 KWKG 2025) — Anzeige aus dem Gesetzeskatalog");
            lblKwkgTitel.Text = T("KDLG_ERTRAG_EINSP_TITEL", "Eingespeister KWK-Strom (Tranchen):");
            grpDauer.Text = T("KDLG_ERTRAG_G_DAUER", "Förderdauer und Jahresdeckel");
            grpSteuern.Text = T("KDLG_ERTRAG_G_STEUERN",
                "Steuervergünstigungen (HF6, Sätze aus dem Gesetzeskatalog)");
            grpVerweise.Text = T("KDLG_ERTRAG_G_VERWEISE", "Pflegeorte (eine Wahrheit je Größe)");
            btnGesetze.Text = T("KDLG_ERTRAG_BTN_GESETZE", "Gesetzesparameter…");
            grpPv.Text = T("KDLG_ERTRAG_G_PV", "PV-Vergütung (EEG) — eine Vergütungswahrheit (V4/F7)");
            lblPvProjekt.Text = T("KDLG_ERTRAG_PV_PROJEKT", "Stammprojekt:");
            btnPvOeffnen.Text = T("KDLG_ERTRAG_PV_OEFFNEN", "PV-Vergütungsdialog öffnen…");
            lblLeer.Text = T("KDLG_ERTRAG_LEER",
                "Diese Komponente führt keine laufenden Erträge — Förderungen/Zuschüsse " +
                "laufen als Zuschuss-Position in den Investitionskosten (FK5).");
        }

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
