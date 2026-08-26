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

        public Form_VorlagenUebernahme()
        {
            InitializeComponent();
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

            // Projektmodus: Das Ziel IST das geöffnete Projekt — keine Umwahl.
            cmbZielProjekt.Enabled = zielProjektId <= 0;
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
            cmbQuellVorlage.Enabled = rbQuelleVorlage.Checked;
            VorschauAktualisieren();
        }

        /// <summary>Klartext-Vorschau (§ 8 Nr. 3) — nur Zählen, kein Schreiben.</summary>
        private void VorschauAktualisieren()
        {
            int ziel = ZielProjektId;
            if (ziel <= 0) { lblVorschau.Text = ""; return; }

            int vorhanden = KostenVorlagenUebernahmeCtrl.VorhandeneImProjekt(
                ziel, _komponentenId, _kategorieId);
            KostenVorlageKopf quellVorlage = QuellVorlage;
            int quelle = rbQuelleVorlage.Checked
                ? (quellVorlage != null ? KostenVorlagenCtrl.Positionen(quellVorlage.Id).Count : 0)
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
                ? KostenVorlagenUebernahmeCtrl.AusVorlage(ZielProjektId, QuellVorlage, _zielAnlageId)
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
