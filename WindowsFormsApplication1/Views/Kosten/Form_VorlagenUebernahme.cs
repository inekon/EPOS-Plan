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
        private KostenVorlageKopf _vorlage;
        private IList<KeyValuePair<int, string>> _projekte;
        private bool _fuellt;

        public Form_VorlagenUebernahme()
        {
            InitializeComponent();
        }

        /// <summary>Kontext vor <c>ShowDialog</c> übergeben (der Aufrufer bestimmt
        /// Komponente, Kategorie und die aktuell angezeigte Vorlage).</summary>
        public void SetControls(int komponentenId, string komponentenName, int kategorieId,
                                KostenVorlageKopf vorlage)
        {
            _fuellt = true;
            _komponentenId = komponentenId;
            _kategorieId = kategorieId;
            _vorlage = vorlage;

            lblKontext.Text = komponentenName + " · " +
                (kategorieId == Form_Kosten.KATEGORIE_BETRIEB
                    ? Text_("KDLG_KAT_BETRIEB", "Betriebskosten")
                    : Text_("KDLG_KAT_INVEST", "Investitionskosten"));
            rbQuelleVorlage.Text = Text_("KDLG_UEB_QUELLE_VORLAGE", "Aus der aktuellen Vorlage/Variante") +
                (vorlage != null ? " („" + vorlage.Name + "\")" : "");
            rbQuelleVorlage.Enabled = vorlage != null;
            if (vorlage == null) rbQuelleProjekt.Checked = true;

            _projekte = KostenVorlagenUebernahmeCtrl.Projekte();
            cmbZielProjekt.Items.Clear();
            cmbQuellProjekt.Items.Clear();
            foreach (KeyValuePair<int, string> p in _projekte)
            {
                cmbZielProjekt.Items.Add(p.Value + "  [" + p.Key + "]");
                cmbQuellProjekt.Items.Add(p.Value + "  [" + p.Key + "]");
            }
            if (cmbZielProjekt.Items.Count > 0) cmbZielProjekt.SelectedIndex = 0;
            if (cmbQuellProjekt.Items.Count > 0) cmbQuellProjekt.SelectedIndex = 0;
            _fuellt = false;

            VorschauAktualisieren();
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
            VorschauAktualisieren();
        }

        /// <summary>Klartext-Vorschau (§ 8 Nr. 3) — nur Zählen, kein Schreiben.</summary>
        private void VorschauAktualisieren()
        {
            int ziel = ZielProjektId;
            if (ziel <= 0) { lblVorschau.Text = ""; return; }

            int vorhanden = KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                ziel, _komponentenId, _kategorieId);
            int quelle = rbQuelleVorlage.Checked
                ? (_vorlage != null ? KostenVorlagenCtrl.Positionen(_vorlage.Id).Count : 0)
                : KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                      QuellProjektId, _komponentenId, _kategorieId);

            lblVorschau.Text = string.Format(
                Text_("KDLG_UEB_VORSCHAU",
                    "Die Quelle enthält {0} Positionen. Das Zielprojekt führt für diese " +
                    "Komponente bereits {1} Positionen — vorhandene bleiben unberührt, nur " +
                    "fehlende werden angelegt. Die Herkunft wird je Position vermerkt."),
                quelle, vorhanden);

            btnUebernehmen.Enabled = quelle > 0 &&
                (rbQuelleVorlage.Checked || QuellProjektId != ziel);
        }

        private void btnUebernehmen_Click(object sender, EventArgs e)
        {
            UebernahmeErgebnis ergebnis = rbQuelleVorlage.Checked
                ? KostenVorlagenUebernahmeCtrl.AusVorlage(ZielProjektId, _vorlage)
                : KostenVorlagenUebernahmeCtrl.AusProjekt(ZielProjektId, QuellProjektId,
                                                          _komponentenId, _kategorieId);

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
