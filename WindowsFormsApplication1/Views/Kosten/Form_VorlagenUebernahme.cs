using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Übernahme-Dialog Stamm → Projekt (Etappe KD3, Konzept Kostendialoge Rev. 1.2,
    /// § 8): Zielprojekt wählen, Quelle wählen (aktuelle Vorlage/Variante oder anderes
    /// Projekt), Klartext-Vorschau, Übernehmen.
    ///
    /// <para><b>Der Dialog rechnet und schreibt nicht selbst</b> (Hausmuster
    /// <c>Form_BkUebernahme</c>): Zählen und Schreiben erledigt
    /// <see cref="KostenVorlagenUebernahmeCtrl"/>. Vorhandene Projektzeilen bleiben
    /// grundsätzlich unberührt. Designer-fähig, App-Design (§ 12).</para>
    /// </summary>
    public partial class Form_VorlagenUebernahme : Form
    {
        private int _komponentenId;
        private int _kategorieId;
        private IList<KostenVorlageKopf> _vorlagen;
        private IList<KeyValuePair<int, string>> _projekte;
        private bool _fuellt;

        /// <summary>Ä20: Ziel-Anlage der Übernahme (0 = ohne Anlagenbezug).</summary>
        private int _zielAnlageId;

        /// <summary>Ä21: Komponentenname (für die Quell-Anlagenliste) und die
        /// Einträge der Quell-Anlagen-Klappliste (AnlageId; 0 = ohne Zuordnung).</summary>
        private string _komponentenName = "";
        private readonly List<KeyValuePair<int, string>> _quellAnlagen =
            new List<KeyValuePair<int, string>>();

        public Form_VorlagenUebernahme()
        {
            InitializeComponent();
            // H7: Infoknopf in das Kopfband (pnlKopf, Dock Top, 40 hoch).
            InfoKnopf.Anbringen(this, ziel: pnlKopf);
        }

        /// <summary>Kontext vor <c>ShowDialog</c> übergeben. Die Quelle „Vorlage“
        /// bietet ALLE Vorlagen/Varianten des Admin-Katalogs dieser Komponente und
        /// Kategorie an (Ä11 — Nutzerauftrag 26.08.2026); <paramref name="vorlage"/>
        /// ist nur die VORAUSWAHL (null = Standard). Mit
        /// <paramref name="zielProjektId"/> &gt; 0 steht das Zielprojekt fest
        /// (Aufruf aus dem Projektmodus der Kostenverwaltung).</summary>
        public void SetControls(int komponentenId, string komponentenName, int kategorieId,
                                KostenVorlageKopf vorlage, int zielProjektId = 0, int zielAnlageId = 0)
        {
            _zielAnlageId = zielAnlageId;   // Ä20: Ziel-Anlage der Übernahme
            _fuellt = true;
            _komponentenId = komponentenId;
            _komponentenName = komponentenName ?? "";
            _kategorieId = kategorieId;

            lblKontext.Text = komponentenName + " · " +
                (kategorieId == Form_Kosten.KATEGORIE_BETRIEB
                    ? Text_("KDLG_KAT_BETRIEB", "Betriebskosten")
                    : Text_("KDLG_KAT_INVEST", "Investitionskosten"));

            // Ä11: Vorlagenliste des Admin-Katalogs als wählbare Quelle.
            _vorlagen = KostenVorlagenCtrl.Vorlagen(komponentenId, kategorieId);
            cmbQuellVorlage.Items.Clear();
            int vorwahl = 0;
            for (int i = 0; i < _vorlagen.Count; i++)
            {
                cmbQuellVorlage.Items.Add(_vorlagen[i].Name);
                if (vorlage != null && _vorlagen[i].Id == vorlage.Id) vorwahl = i;
            }
            if (cmbQuellVorlage.Items.Count > 0) cmbQuellVorlage.SelectedIndex = vorwahl;
            rbQuelleVorlage.Text = Text_("KDLG_UEB_QUELLE_VORLAGE", "Aus Vorlage/Variante:");
            rbQuelleVorlage.Enabled = _vorlagen.Count > 0;
            if (_vorlagen.Count == 0) rbQuelleProjekt.Checked = true;

            _projekte = KostenVorlagenUebernahmeCtrl.Projekte();
            cmbZielProjekt.Items.Clear();
            cmbQuellProjekt.Items.Clear();
            int zielIndex = 0;
            for (int i = 0; i < _projekte.Count; i++)
            {
                cmbZielProjekt.Items.Add(_projekte[i].Value + "  [" + _projekte[i].Key + "]");
                cmbQuellProjekt.Items.Add(_projekte[i].Value + "  [" + _projekte[i].Key + "]");
                if (zielProjektId > 0 && _projekte[i].Key == zielProjektId) zielIndex = i;
            }
            if (cmbZielProjekt.Items.Count > 0) cmbZielProjekt.SelectedIndex = zielIndex;
            if (cmbQuellProjekt.Items.Count > 0) cmbQuellProjekt.SelectedIndex = 0;

            // Ä21: „Aus anderem Projekt“ heißt jetzt Projekt UND Anlage — damit
            // eine weitere Wärmepumpe die Kosten der bereits vorhandenen übernehmen
            // kann, ist auch das EIGENE Projekt eine gültige Quelle.
            rbQuelleProjekt.Text = Text_("KDLG_UEB_QUELLE_PROJEKT", "Aus Projekt/Anlage:");

            // Projektmodus: Das Ziel IST das geöffnete Projekt — keine Umwahl;
            // die Quelle startet dann sinnvollerweise beim eigenen Projekt.
            cmbZielProjekt.Enabled = zielProjektId <= 0;
            if (zielProjektId > 0)
                for (int i = 0; i < _projekte.Count; i++)
                    if (_projekte[i].Key == zielProjektId) { cmbQuellProjekt.SelectedIndex = i; break; }
            QuellAnlagenFuellen();
            _fuellt = false;

            VorschauAktualisieren();
        }

        /// <summary>Die in der Klappliste gewählte Quellvorlage (null = keine).</summary>
        private KostenVorlageKopf QuellVorlage
        {
            get
            {
                int i = cmbQuellVorlage.SelectedIndex;
                return (_vorlagen != null && i >= 0 && i < _vorlagen.Count) ? _vorlagen[i] : null;
            }
        }

        private int ZielProjektId
        {
            get
            {
                int i = cmbZielProjekt.SelectedIndex;
                return (i >= 0 && i < _projekte.Count) ? _projekte[i].Key : 0;
            }
        }

        private int QuellProjektId
        {
            get
            {
                int i = cmbQuellProjekt.SelectedIndex;
                return (i >= 0 && i < _projekte.Count) ? _projekte[i].Key : 0;
            }
        }

        private void Auswahl_Geaendert(object sender, EventArgs e)
        {
            if (_fuellt) return;
            cmbQuellProjekt.Enabled = rbQuelleProjekt.Checked;
            cmbQuellAnlage.Enabled = rbQuelleProjekt.Checked;
            cmbQuellVorlage.Enabled = rbQuelleVorlage.Checked;
            VorschauAktualisieren();
        }

        /// <summary>Ä21: Projektwechsel der Quelle — Anlagenliste nachziehen.</summary>
        private void QuellProjekt_Geaendert(object sender, EventArgs e)
        {
            if (_fuellt) return;
            _fuellt = true;
            QuellAnlagenFuellen();
            _fuellt = false;
            Auswahl_Geaendert(sender, e);
        }

        /// <summary>Die Anlagen der Komponente im gewählten Quellprojekt — plus
        /// „(ohne Anlagenzuordnung)“, wenn dort lose Positionen liegen.</summary>
        private void QuellAnlagenFuellen()
        {
            _quellAnlagen.Clear();
            cmbQuellAnlage.Items.Clear();
            int projekt = QuellProjektId;
            if (projekt <= 0) return;

            foreach (ProjektEnergietraegerCtrl.AnlagenEintrag a in
                     ProjektEnergietraegerCtrl.AnlagenMitTraeger(projekt))
            {
                if (!string.Equals(a.Komponente, _komponentenName, StringComparison.Ordinal))
                    continue;
                string text = string.IsNullOrEmpty(a.Bezeichner)
                    ? a.Komponente : a.Komponente + " — " + a.Bezeichner;
                _quellAnlagen.Add(new KeyValuePair<int, string>(a.AnlageId, text));
            }

            int lose = KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                           projekt, _komponentenId, _kategorieId, 0) +
                       KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                           projekt, _komponentenId,
                           _kategorieId == Form_Kosten.KATEGORIE_BETRIEB
                               ? Form_Kosten.KATEGORIE_INVESTITION
                               : Form_Kosten.KATEGORIE_BETRIEB, 0);
            if (lose > 0 || _quellAnlagen.Count == 0)
                _quellAnlagen.Add(new KeyValuePair<int, string>(0,
                    Text_("KDLG_UEB_QUELLE_LOSE", "(ohne Anlagenzuordnung)")));

            foreach (KeyValuePair<int, string> q in _quellAnlagen)
                cmbQuellAnlage.Items.Add(q.Value);
            if (cmbQuellAnlage.Items.Count > 0) cmbQuellAnlage.SelectedIndex = 0;
        }

        /// <summary>Ä21: gewählte Quell-Anlage (0 = ohne Zuordnung).</summary>
        private int QuellAnlageId
        {
            get
            {
                int i = cmbQuellAnlage.SelectedIndex;
                return (i >= 0 && i < _quellAnlagen.Count) ? _quellAnlagen[i].Key : 0;
            }
        }

        /// <summary>Klartext-Vorschau (§ 8 Nr. 3) — nur Zählen, kein Schreiben.</summary>
        private void VorschauAktualisieren()
        {
            int ziel = ZielProjektId;
            if (ziel <= 0) { lblVorschau.Text = ""; return; }

            // Ä21: Ziel und Quelle zählen ANLAGENBEZOGEN — sonst behauptete die
            // Vorschau bei einer leeren zweiten Anlage „führt bereits 7 Positionen“.
            int vorhanden = KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                ziel, _komponentenId, _kategorieId, _zielAnlageId > 0 ? _zielAnlageId : -1);
            KostenVorlageKopf quellVorlage = QuellVorlage;
            int quelle = rbQuelleVorlage.Checked
                ? (quellVorlage != null ? KostenVorlagenCtrl.Positionen(quellVorlage.Id).Count : 0)
                : KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                      QuellProjektId, _komponentenId, _kategorieId, QuellAnlageId);

            lblVorschau.Text = string.Format(
                Text_("KDLG_UEB_VORSCHAU",
                    "Die Quelle enthält {0} Positionen. Das Zielprojekt führt für diese " +
                    "Komponente bereits {1} Positionen — vorhandene bleiben unberührt, nur " +
                    "fehlende werden angelegt. Die Herkunft wird je Position vermerkt."),
                quelle, vorhanden);

            btnUebernehmen.Enabled = quelle > 0 &&
                (rbQuelleVorlage.Checked || QuellProjektId != ziel ||
                 (QuellAnlageId != _zielAnlageId &&
                  (QuellAnlageId > 0 || _zielAnlageId > 0)));
        }

        private void btnUebernehmen_Click(object sender, EventArgs e)
        {
            UebernahmeErgebnis ergebnis = rbQuelleVorlage.Checked
                ? KostenVorlagenUebernahmeCtrl.AusVorlage(ZielProjektId, QuellVorlage, _zielAnlageId)
                : KostenVorlagenUebernahmeCtrl.AusProjekt(ZielProjektId, QuellProjektId,
                                                          _komponentenId, _kategorieId,
                                                          QuellAnlageId, _zielAnlageId);

            MessageBox.Show(string.Join(Environment.NewLine, ergebnis.Meldungen), Text,
                MessageBoxButtons.OK,
                ergebnis.Fehler ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

            if (!ergebnis.Fehler)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
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
