using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was eine Assistentenseite können muss, die eine ERZEUGERLISTE bearbeitet
    /// (iU9-W6.0e).
    ///
    /// <para><b>Wozu.</b> <see cref="WizardParent"/> hängte seine dreizehn Seiten bis
    /// Welle 6 über eine Kette von <c>if (top == …_ITEM) ((Form_X)page).…</c> ein — je
    /// Seite zwei Zeilen mit hartem Typumbruch. Für die vier Erzeugerseiten
    /// (Photovoltaik, Stromspeicher, Heizkessel, BHKW) sind diese zwei Zeilen
    /// wortgleich: Liste hineinreichen, dann <c>SetControls(…, true)</c>. Sobald die
    /// Seite eine <c>BlazorAssistentSeite&lt;T&gt;</c> ist, trifft der Typumbruch
    /// ohnehin nicht mehr — deshalb hier eine Schnittstelle statt vier Typen.</para>
    ///
    /// <para><b>Die Liste wird GETEILT, nicht kopiert.</b> Der Assistent hält genau
    /// eine <c>List&lt;WErzeugerModel&gt;</c> über alle Erzeugertypen hinweg
    /// (<c>WizardParent.list_werzmodel</c>); jede Seite bearbeitet dieselbe Liste an
    /// Ort und Stelle und filtert beim Anzeigen auf ihren eigenen
    /// <c>ID_Type</c>. Genau so arbeiteten die vier WinForms-Masken
    /// (<c>list_heizkesselmodel = wizardparent.list_werzmodel</c>) — und genau deshalb
    /// hat die Schnittstelle einen Setter und keine Kopie.</para>
    ///
    /// <para><b><c>WizardParent.Aktiver</c> entfällt.</b> Die vier Masken suchten sich
    /// den Rahmen bisher selbst (<c>getWizardPage()</c> über den statischen Halter) und
    /// lasen ihm die Liste ab. Jetzt reicht der Rahmen sie herein — die Richtung stimmt
    /// damit wieder: Der Wirt kennt seine Seiten, nicht umgekehrt.</para>
    /// </summary>
    internal interface IAssistentErzeugerSeite
    {
        /// <summary>
        /// Die geteilte Erzeugerliste des Assistentenlaufs. Wird VOR
        /// <see cref="Bestuecken"/> gesetzt.
        /// </summary>
        List<WErzeugerModel> Modelle { get; set; }

        /// <summary>
        /// Baut die Seite mit dem laufenden Projekt auf — das Gegenstück zu
        /// <c>SetControls(…, bWizard: true)</c> der WinForms-Fassungen.
        /// </summary>
        /// <param name="projektId">
        /// Id des Projekts. Im Assistenten kann sie 0 sein (neues Projekt, noch nicht
        /// gespeichert); die Seite legt dann keine Projektkopien an.
        /// </param>
        /// <param name="projektName">Name des Projekts für die Kopfzeile.</param>
        void Bestuecken(int projektId, string projektName);
    }
}
