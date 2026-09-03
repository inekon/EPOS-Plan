using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// „Projekte löschen" (Nutzerauftrag 02.09.2026): dieselbe Projektliste wie der
    /// Öffnen-Dialog (<see cref="ProjektAuswahl"/>), hier im Häkchenmodus —
    /// Suche, Sortierung, Varianten unter ihrem Stamm. Ein angehakter Stamm nimmt
    /// seine Varianten mit. Der Dialog fragt vor dem Löschen mit der vollständigen
    /// Liste zurück und liefert dem Aufrufer (<see cref="MenueCtrl.ProjektDelete"/>)
    /// die zu löschenden Projekte; gelöscht wird dort über den bewährten Weg.
    /// </summary>
    public partial class Form_ProjektDelete : Form
    {
        /// <summary>Die vom Anwender bestätigten Projekte (Varianten vor Stämmen).</summary>
        public List<ProjektModel> ZuLoeschen { get; private set; } = new List<ProjektModel>();

        /// <summary>true, wenn vor dem Löschen eine Sicherungskopie der Datenbank gewünscht ist.</summary>
        public bool SicherungGewuenscht { get; private set; } = true;

        public Form_ProjektDelete()
        {
            InitializeComponent();
            TexteSetzen();
            CancelButton = btn_Abbrechen;
        }

        // Ressourcen-Helfer mit deutschem Fallback (Drei-Schichten-Regel; die
        // generierten Resource-Eigenschaften entstehen erst im VS-Designer).
        internal static string TPd(string key, string fallback)
        {
            try
            {
                string s = MyResource.Resource.ResourceManager.GetString(key);
                return string.IsNullOrEmpty(s) ? fallback : s;
            }
            catch { return fallback; }
        }

        private void TexteSetzen()
        {
            Text = TPd("PDLG_TITEL", "Projekte löschen");
            lblHinweis.Text = TPd("PDLG_HINWEIS",
                "Wählen Sie die zu löschenden Projekte per Häkchen. Ein Stammprojekt nimmt seine Varianten mit. " +
                "Das Löschen ist unwiderruflich.");
            lnkAlle.Text = TPd("PDLG_ALLE", "Alle sichtbaren auswählen");
            lnkKeine.Text = TPd("PDLG_KEINE", "Auswahl aufheben");
            chkSicherung.Text = TPd("PDLG_SICHERUNG", "Sicherungskopie der Datenbank vor dem Löschen anlegen");
            btn_Loeschen.Text = TPd("PDLG_LOESCHEN", "Löschen…");
            btn_Abbrechen.Text = TPd("PDLG_ABBRECHEN", "Abbrechen");
        }

        private void Form_ProjektDelete_Load(object sender, EventArgs e)
        {
            ucAuswahl.MehrfachAuswahl = true;
            ucAuswahl.AutomatischeVorauswahl = false;
            ucAuswahl.Laden();
            ucAuswahl.SuchfeldFokussieren();
            AuswahlNachziehen();
        }

        private void ucAuswahl_AuswahlGeaendert(object sender, EventArgs e) { AuswahlNachziehen(); }

        private void AuswahlNachziehen()
        {
            btn_Loeschen.Enabled = ucAuswahl.GewaehlteProjekte.Count > 0;
        }

        private void lnkAlle_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { ucAuswahl.AlleSichtbaren(true); }
        private void lnkKeine_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { ucAuswahl.AlleSichtbaren(false); }

        private void btn_Loeschen_Click(object sender, EventArgs e)
        {
            List<ProjektModel> liste = ucAuswahl.GewaehlteProjekte;
            if (liste.Count == 0) return;

            // Rückfrage mit der vollständigen Liste — gekürzt, wenn sie sehr lang wird.
            const int MAX_NAMEN = 12;
            var namen = new StringBuilder();
            for (int i = 0; i < liste.Count && i < MAX_NAMEN; i++)
            {
                namen.Append("  • ").Append(liste[i].m_szProjektname);
                if (ucAuswahl.IstVariante(liste[i].m_ID))
                    namen.Append("  (").Append(TPd("PDLG_VARIANTE", "Variante")).Append(")");
                namen.Append("\r\n");
            }
            if (liste.Count > MAX_NAMEN)
                namen.Append("  ").Append(string.Format(TPd("PDLG_WEITERE", "… und {0} weitere"), liste.Count - MAX_NAMEN));

            string frage = string.Format(TPd("PDLG_RUECKFRAGE",
                "{0} Projekt(e) werden mit allen zugehörigen Daten unwiderruflich gelöscht:\r\n\r\n{1}\r\nFortfahren?"),
                liste.Count, namen.ToString());
            if (MessageBox.Show(frage, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning,
                                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            ZuLoeschen = liste;
            SicherungGewuenscht = chkSicherung.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btn_Abbrechen_Click(object sender, EventArgs e)
        {
            ZuLoeschen = new List<ProjektModel>();
            DialogResult = DialogResult.Cancel;
            Close();
        }
    }
}
