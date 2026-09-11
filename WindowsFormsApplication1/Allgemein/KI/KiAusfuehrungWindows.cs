// WAS VON DER AUSFUEHRUNGSSCHICHT IN DER WINDOWS-HUELLE BLEIBT (Auftrag #201).
//
// Bis #201 lag die ganze Schicht hier: KiAusfuehrer (995 Zeilen, statisch) samt Register,
// Einlaeufigkeit, Bestaetigungsriegel, Protokoll und Sitzungsgedaechtnis. Sie liegt jetzt
// im Kern (EPOS.Kern\Allgemein\KI\KiAusfuehrung.cs) - der Grund steht im Konzept "Der
// Hilfe-Assistent im Dialog", 3.4: iOS und die Razor-Dialoge bekommen damit DASSELBE
// Register. Vorher stand dort KeineAusfuehrung mit einem leeren.
//
// UEBRIG BLEIBT, WAS DIE PLATTFORM BEISTEUERT, und das ist wenig:
//
//   * Ist gerade ein MODALER Dialog offen? -> Form.ActiveForm.Modal
//   * Der Hilfetext zu einem Feld -> WikiHelpCatalog, der im Dokumentationsordner liegt
//
// Der dritte plattformgebundene Weg - "fuehre diese Arbeit auf dem Oberflaechenfaden aus"
// - steht NICHT hier, sondern dort, wo die Oberflaeche lebt: KiChatHuelle belegt
// KiAusfuehrung.AufOberflaeche mit ihrem eigenen BeginInvoke, solange das Chatfenster
// steht, und nimmt ihn beim Schliessen wieder heraus. Ein Weg auf ein Fenster, das es
// nicht mehr gibt, waere schlechter als gar keiner.
//
// DER FRUEHERE RUECKFALL UEBER Application.OpenForms IST ERSATZLOS ENTFALLEN. Er suchte
// das erste offene Formular, wenn kein Weg eingelegt war; unter Windows legt die Huelle
// ihn ohnehin ein, und auf jeder anderen Plattform half er nicht.

using System;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Windows-Seite der Ausfuehrungsschicht: der Halter der einen Instanz und die
    /// zwei Haken, die nur diese Plattform beantworten kann.
    /// </summary>
    internal static class KiAusfuehrungWindows
    {
        private static readonly object _sperre = new object();
        private static KiAusfuehrung _aktuell;

        /// <summary>
        /// Die Ausfuehrung dieser Anwendung. Sie entsteht beim ersten Zugriff und wird
        /// dabei in <see cref="KiAusfuehrungsweg"/> eingelegt.
        /// </summary>
        /// <remarks>
        /// <b>Warum lazy und nicht allein in <c>Program.Main</c>.</b> Der Chat greift auf
        /// Belegtzustand und Protokollpfad zu; ein Prueflauf ohne <c>Main</c> (und der
        /// Entwurfsmodus von Visual Studio) taete das sonst auf <c>null</c>.
        /// <see cref="Einlegen"/> bleibt trotzdem: Es setzt die zwei Haken, und das soll
        /// sichtbar im Programmstart stehen.
        /// </remarks>
        internal static KiAusfuehrung Aktuell
        {
            get
            {
                if (_aktuell != null) return _aktuell;
                lock (_sperre)
                {
                    if (_aktuell == null)
                    {
                        _aktuell = new KiAusfuehrung();
                        KiAusfuehrungsweg.Aktuell = _aktuell;
                    }
                }
                return _aktuell;
            }
        }

        /// <summary>
        /// Legt die Ausfuehrung samt ihren Windows-Haken ein (gerufen aus
        /// <c>Program.Main</c>).
        /// </summary>
        internal static void Einlegen()
        {
            KiAusfuehrung schicht = Aktuell;

            // Die Modalitaetsfrage (Fachkonzept 3.4, Pflicht 2). Der zweite Haken
            // - die Ueberlagerung einer Razor-Oberflaeche - kommt von der Chatkomponente
            // und wird hier bewusst NICHT belegt.
            schicht.ModalerDialog = ModalerDialogOffen;

            // Der Hilfetext zu einem Katalogfeld. Der Katalog wird erst spaeter in
            // Program.Main belegt; der Lieferant fragt ihn deshalb bei jedem Aufruf neu,
            // statt ihn hier festzuhalten.
            KiFeldhilfe.Lieferant = Hilfetext;
        }

        /// <summary>Ist gerade ein modaler Dialog offen? Im Zweifel nein.</summary>
        /// <remarks>
        /// Wortgleich aus <c>KiAusfuehrer.ModalerDialogOffen</c> uebernommen - die eine
        /// Zeile, die der Kern nicht haben darf.
        /// </remarks>
        internal static bool ModalerDialogOffen()
        {
            try
            {
                Form aktiv = Form.ActiveForm;
                return aktiv != null && aktiv.Modal;
            }
            catch { return false; }
        }

        /// <summary>Der Hilfeeintrag zum Slug; <c>null</c>, wenn es keinen gibt.</summary>
        /// <remarks>
        /// <b>Der Hilfekatalog darf fehlen.</b> <c>WikiHelpCatalog.Aktueller</c> wird erst
        /// in <c>Program.Main</c> belegt; im Aktionsharnisch und in Prueflaeufen gibt es
        /// ihn nicht. Ein fehlender Hilfetext ist ein Schoenheitsfehler und kein Grund,
        /// eine Erklaerung scheitern zu lassen.
        /// </remarks>
        private static KiFeldhilfeEintrag Hilfetext(string slug)
        {
            try
            {
                WikiHelpCatalog katalog = WikiHelpCatalog.Aktueller;
                HelpEntry eintrag = katalog == null ? null : katalog.Get(slug);
                return eintrag == null ? null : new KiFeldhilfeEintrag(eintrag.Tooltip, eintrag.Url);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
