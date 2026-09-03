using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Was eine Assistentenseite können muss, die EINE GETEILTE LISTE bearbeitet
    /// (iU9-W9.0a) — die Verallgemeinerung von <see cref="IAssistentErzeugerSeite"/>.
    ///
    /// <para><b>Wozu.</b> Welle 6 hat die vier Erzeugerseiten über eine Schnittstelle
    /// eingehängt, weil ihre je zwei Zeilen in <c>WizardParent.LoadNewForm</c> wortgleich
    /// waren: Liste hineinreichen, dann <c>SetControls(…, true)</c>. Die vier
    /// BEDARFSSEITEN der Welle 9 (Gebäude, Wärmebedarf extern, Prozesswärme,
    /// Stromverbraucher) machen dasselbe — nur mit vier ANDEREN Listentypen. Eine
    /// zweite Schnittstelle je Typ wäre viermal derselbe Text; also trägt die
    /// Schnittstelle den Listentyp als Typparameter, und die Erzeugerfassung ist ihr
    /// Spezialfall <c>IAssistentListenSeite&lt;WErzeugerModel&gt;</c>.</para>
    ///
    /// <para><b>Die Liste wird GETEILT, nicht kopiert.</b> Wie bei den Erzeugern: Der
    /// Assistent hält je Gewerk genau eine Liste, die Seite bearbeitet sie an Ort und
    /// Stelle. Deshalb hat die Eigenschaft einen Setter und keine Kopie — und deshalb
    /// braucht <c>SpeichernAusfuehren</c> die Listen nicht mehr aus den Seiten
    /// zurückzulesen.</para>
    /// </summary>
    /// <typeparam name="T">Der Zeilentyp der geteilten Liste.</typeparam>
    internal interface IAssistentListenSeite<T>
    {
        /// <summary>
        /// Die geteilte Liste des Assistentenlaufs. Wird VOR <see cref="Bestuecken"/>
        /// gesetzt.
        /// </summary>
        List<T> Modelle { get; set; }

        /// <summary>
        /// Baut die Seite mit dem laufenden Projekt auf — das Gegenstück zu
        /// <c>SetControls(…, bWizard: true)</c> der WinForms-Fassungen.
        /// </summary>
        /// <param name="projektId">
        /// Id des Projekts. Im Assistenten kann sie eine geratene <c>MAX(ID)+1</c> sein
        /// (neues Projekt, noch nicht gespeichert); die Seite legt dann keine
        /// Projektkopien an.
        /// </param>
        /// <param name="projektName">Name des Projekts für die Kopfzeile.</param>
        void Bestuecken(int projektId, string projektName);
    }
}
