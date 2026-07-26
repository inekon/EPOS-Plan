using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ermittelt, in welchem Bereich der Anwendung sich der Benutzer gerade
    /// befindet. Der KI-Assistent bekommt diesen Kontext mitgeliefert und kann
    /// dadurch gezielt zur aktuellen Maske antworten.
    ///
    /// Die Erkennung läuft automatisch über das aktive Fenster und dessen
    /// TabControls - es müssen also keine Formulare angepasst werden. Zusätzlich
    /// kann jede Maske über SetzeBereich() einen sprechenden Namen hinterlegen.
    /// </summary>
    public static class HilfeKontext
    {
        /// <summary>Optional gesetzter Bereichsname (überschreibt die Automatik).</summary>
        private static string _bereich = "";

        /// <summary>Zusätzliche Angaben der Maske (z. B. "Wärmepumpe: CS7800iLW 12").</summary>
        private static readonly List<string> _details = new List<string>();

        /// <summary>
        /// Setzt den Bereichsnamen, in dem sich der Benutzer befindet.
        /// Aufruf z. B. im Konstruktor oder beim Aktivieren einer Maske.
        /// </summary>
        public static void SetzeBereich(string bereich)
        {
            _bereich = bereich ?? "";
            _details.Clear();
        }

        /// <summary>Ergänzt eine Detailangabe zum aktuellen Bereich.</summary>
        public static void ErgaenzeDetail(string detail)
        {
            if (string.IsNullOrWhiteSpace(detail)) return;
            if (!_details.Contains(detail)) _details.Add(detail);
        }

        /// <summary>Löscht den gesetzten Kontext (z. B. beim Schließen einer Maske).</summary>
        public static void Zuruecksetzen()
        {
            _bereich = "";
            _details.Clear();
        }

        /// <summary>
        /// Liefert eine kurze Beschreibung des aktuellen Kontexts für den
        /// KI-Assistenten - bewusst knapp, da jedes Token Kosten verursacht.
        /// Es werden ausschließlich Bedien-Informationen übertragen,
        /// keine Projekt- oder Kundendaten.
        /// </summary>
        public static string Beschreibung()
        {
            StringBuilder sb = new StringBuilder();

            string bereich = !string.IsNullOrEmpty(_bereich) ? _bereich : AktivesFenster();
            if (!string.IsNullOrEmpty(bereich)) sb.Append("Bereich: ").Append(bereich);

            string tabs = AktiveRegisterkarten();
            if (!string.IsNullOrEmpty(tabs))
            {
                if (sb.Length > 0) sb.Append(" | ");
                sb.Append("Registerkarte: ").Append(tabs);
            }

            foreach (string d in _details)
            {
                if (sb.Length > 0) sb.Append(" | ");
                sb.Append(d);
            }

            return sb.ToString();
        }

        /// <summary>Titel bzw. Name des aktiven Fensters.</summary>
        private static string AktivesFenster()
        {
            try
            {
                Form frm = Form.ActiveForm;
                if (frm == null) return "";
                return !string.IsNullOrEmpty(frm.Text) ? frm.Text : frm.Name;
            }
            catch { return ""; }
        }

        /// <summary>
        /// Sammelt die Beschriftungen der aktuell gewählten Registerkarten
        /// (auch verschachtelt), z. B. "Simulation > Wärmepumpe".
        /// </summary>
        private static string AktiveRegisterkarten()
        {
            try
            {
                Form frm = Form.ActiveForm;
                if (frm == null) return "";

                List<string> namen = new List<string>();
                SucheTabs(frm, namen);
                return string.Join(" > ", namen);
            }
            catch { return ""; }
        }

        private static void SucheTabs(Control parent, List<string> namen)
        {
            foreach (Control c in parent.Controls)
            {
                TabControl tc = c as TabControl;
                if (tc != null && tc.SelectedTab != null)
                {
                    string text = tc.SelectedTab.Text;
                    if (!string.IsNullOrWhiteSpace(text) && !namen.Contains(text)) namen.Add(text);
                    SucheTabs(tc.SelectedTab, namen);   // verschachtelte TabControls
                    continue;
                }

                if (c.Controls.Count > 0) SucheTabs(c, namen);
            }
        }
    }
}
