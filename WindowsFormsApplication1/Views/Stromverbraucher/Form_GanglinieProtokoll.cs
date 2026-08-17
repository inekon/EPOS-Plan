using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Uebersetzt die sprachneutralen Protokollschluessel der Engine
    /// (<see cref="PruefMeldung"/>) in Anzeigetexte. Drei-Schichten-Regel: die
    /// Engine liefert Schluessel und Werte, der Text kommt ausschliesslich aus
    /// <c>MyResource</c>.
    /// </summary>
    public static class GanglinienProtokollText
    {
        /// <summary>
        /// Anzeigetext einer Meldung. Fehlt der Schluessel im Katalog, wird die
        /// sprachneutrale Kurzfassung angezeigt - besser als ein leeres Feld.
        /// </summary>
        /// <param name="m">Meldung.</param>
        public static string Text(PruefMeldung m)
        {
            if (m == null) return "";

            string vorlage = null;
            try
            {
                vorlage = MyResource.Resource.ResourceManager.GetString(m.Schluessel, MyResource.Resource.Culture);
            }
            catch (Exception) { }

            if (string.IsNullOrEmpty(vorlage)) return m.ToString();
            if (m.Werte.Length == 0) return vorlage;

            try
            {
                return string.Format(CultureInfo.CurrentCulture, vorlage, m.Werte);
            }
            catch (FormatException)
            {
                return vorlage + " (" + string.Join("; ", m.Werte) + ")";
            }
        }

        /// <summary>Anzeigetext einer Pruefstufe.</summary>
        /// <param name="stufe">Stufe.</param>
        public static string StufeText(PruefStufe stufe)
        {
            switch (stufe)
            {
                case PruefStufe.Fehler: return MyResource.Resource.IMPORT_STUFE_FEHLER;
                case PruefStufe.Warnung: return MyResource.Resource.IMPORT_STUFE_WARNUNG;
                default: return MyResource.Resource.IMPORT_STUFE_INFO;
            }
        }

        /// <summary>Farbe einer Pruefstufe in der Protokollliste.</summary>
        /// <param name="stufe">Stufe.</param>
        public static Color StufeFarbe(PruefStufe stufe)
        {
            switch (stufe)
            {
                case PruefStufe.Fehler: return Color.FromArgb(176, 0, 32);
                case PruefStufe.Warnung: return Color.FromArgb(160, 96, 0);
                default: return SystemColors.WindowText;
            }
        }
    }

    /// <summary>
    /// Anzeige des Validierungsprotokolls (AP5). Ersetzt die frueheren
    /// Abbruch-MessageBoxen des Ganglinienimports: Fehler blockieren den Import,
    /// Warnungen und Eingriffe (Schaltjahr, Sommerzeit, Minutenmittelung) werden
    /// zur Bestaetigung vorgelegt, ein sauberer Lauf laeuft ohne Nachfrage durch.
    /// </summary>
    /// <remarks>
    /// Bewusst vollstaendig im Quelltext aufgebaut - keine <c>.Designer.cs</c> und
    /// keine <c>.resx</c>, damit die Projekt-CLAUDE.md-Regel "Designer- und
    /// resx-Dateien nicht von Hand editieren" eingehalten bleibt. Alle Texte
    /// kommen aus <c>MyResource</c> und sind damit zweisprachig.
    /// </remarks>
    public class Form_GanglinieProtokoll : Form
    {
        private ListView listView_Protokoll;
        private Label lbl_Kopf;
        private Button btn_OK;
        private Button btn_Abbrechen;

        /// <summary>
        /// Baut den Dialog auf.
        /// </summary>
        /// <param name="meldungen">Anzuzeigende Meldungen.</param>
        /// <param name="importMoeglich">Kein Fehler - die Schaltflaeche "Uebernehmen" ist aktiv.</param>
        /// <param name="bestaetigungNoetig">An der Reihe wurde etwas veraendert; der Anwender muss bestaetigen.</param>
        public Form_GanglinieProtokoll(IList<PruefMeldung> meldungen, bool importMoeglich, bool bestaetigungNoetig)
        {
            Text = MyResource.Resource.IMPORT_TITEL_PROTOKOLL;
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition = FormStartPosition.CenterParent;
            MinimizeBox = false;
            MaximizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(760, 420);
            MinimumSize = new Size(520, 300);

            lbl_Kopf = new Label();
            lbl_Kopf.SetBounds(12, 10, 736, 34);
            lbl_Kopf.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lbl_Kopf.Text = !importMoeglich
                ? MyResource.Resource.IMPORT_KOPF_FEHLER
                : (bestaetigungNoetig ? MyResource.Resource.IMPORT_KOPF_EINGRIFF
                                      : MyResource.Resource.IMPORT_KOPF_OK);
            Controls.Add(lbl_Kopf);

            listView_Protokoll = new ListView();
            listView_Protokoll.SetBounds(12, 50, 736, 320);
            listView_Protokoll.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listView_Protokoll.View = View.Details;
            listView_Protokoll.FullRowSelect = true;
            listView_Protokoll.GridLines = true;
            listView_Protokoll.MultiSelect = false;
            listView_Protokoll.HideSelection = false;
            listView_Protokoll.Columns.Add(MyResource.Resource.IMPORT_SPALTE_STUFE, 90);
            listView_Protokoll.Columns.Add(MyResource.Resource.IMPORT_SPALTE_MELDUNG, 620);
            Controls.Add(listView_Protokoll);

            if (meldungen != null)
            {
                foreach (PruefMeldung m in meldungen)
                {
                    ListViewItem item = new ListViewItem(GanglinienProtokollText.StufeText(m.Stufe));
                    item.SubItems.Add(GanglinienProtokollText.Text(m));
                    item.ForeColor = GanglinienProtokollText.StufeFarbe(m.Stufe);
                    listView_Protokoll.Items.Add(item);
                }
            }

            btn_OK = new Button();
            btn_OK.SetBounds(556, 382, 90, 26);
            btn_OK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_OK.Text = MyResource.Resource.IMPORT_BTN_UEBERNEHMEN;
            btn_OK.DialogResult = DialogResult.OK;
            btn_OK.Enabled = importMoeglich;
            Controls.Add(btn_OK);

            btn_Abbrechen = new Button();
            btn_Abbrechen.SetBounds(654, 382, 94, 26);
            btn_Abbrechen.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btn_Abbrechen.Text = importMoeglich
                ? MyResource.Resource.IMPORT_BTN_ABBRECHEN
                : MyResource.Resource.IMPORT_BTN_SCHLIESSEN;
            btn_Abbrechen.DialogResult = DialogResult.Cancel;
            Controls.Add(btn_Abbrechen);

            AcceptButton = importMoeglich ? btn_OK : btn_Abbrechen;
            CancelButton = btn_Abbrechen;
        }

        /// <summary>
        /// Zeigt das Protokoll und liefert <c>true</c>, wenn der Import fortgesetzt
        /// werden soll. Ein fehlerfreier Lauf ohne Eingriffe wird gar nicht erst
        /// angezeigt.
        /// </summary>
        /// <param name="eltern">Elternfenster.</param>
        /// <param name="meldungen">Meldungen.</param>
        /// <param name="importMoeglich">Kein Fehler im Protokoll.</param>
        /// <param name="bestaetigungNoetig">Warnung oder Eingriff an der Reihe.</param>
        public static bool Zeigen(IWin32Window eltern, IList<PruefMeldung> meldungen,
                                  bool importMoeglich, bool bestaetigungNoetig)
        {
            if (importMoeglich && !bestaetigungNoetig) return true;

            using (Form_GanglinieProtokoll dlg =
                   new Form_GanglinieProtokoll(meldungen, importMoeglich, bestaetigungNoetig))
            {
                return dlg.ShowDialog(eltern) == DialogResult.OK && importMoeglich;
            }
        }
    }
}
