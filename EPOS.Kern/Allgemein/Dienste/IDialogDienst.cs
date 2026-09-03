namespace WindowsFormsApplication1
{
    /// <summary>Antwort einer Dreifachwahl.</summary>
    public enum JaNeinAbbruch
    {
        /// <summary>Zustimmung.</summary>
        Ja,
        /// <summary>Ablehnung.</summary>
        Nein,
        /// <summary>Abbruch — der Vorgang unterbleibt ganz.</summary>
        Abbruch
    }

    /// <summary>
    /// Meldungen und Rückfragen. Vier Formen — mehr braucht der Kern nachweislich nicht
    /// (Vermessung iU5, Abschnitt A.2: 47 reine Meldungen, 3 Warnungen, 2 Fehler,
    /// 4 Rückfragen, keine einzige Dreifachwahl).
    ///
    /// <para><b>Verhältnis zu <see cref="Meldung"/>.</b> <c>Meldung</c> bleibt bestehen
    /// und ist seit iU5 die schmale Vorderseite dieser Schnittstelle: Seine vier Haken
    /// sind auf <c>Dienste.Dialog</c> vorbelegt. Wer neu schreibt, ruft
    /// <c>Dienste.Dialog</c>; der Bestand darf bei <c>Meldung</c> bleiben.</para>
    ///
    /// <para><b>Verhältnis zu <c>DataRepository.FehlerMelden</c>.</b> Unverändert:
    /// Datenbankfehler entscheiden dort zwischen Dialog und Protokoll. Diese
    /// Schnittstelle beantwortet nur, WIE ein Dialog erzeugt wird, nicht OB einer
    /// erscheinen soll.</para>
    /// </summary>
    public interface IDialogDienst
    {
        /// <summary>
        /// Schlichte Meldung. Ohne Titel entspricht sie <c>MessageBox.Show(text)</c>,
        /// mit Titel <c>MessageBox.Show(text, titel, OK, Information)</c>.
        /// </summary>
        void Meldung(string text, string titel = null);

        /// <summary>Warnung — <c>MessageBox.Show(text, titel, OK, Warning)</c>.</summary>
        void Warnung(string text, string titel = null);

        /// <summary>Fehler — <c>MessageBox.Show(text, titel, OK, Error)</c>.</summary>
        void Fehler(string text, string titel = null);

        /// <summary>
        /// Rückfrage mit zwei Antworten; <c>true</c> = Ja.
        ///
        /// <para>Rückgabe <c>bool</c> und nicht <c>DialogResult</c>: Im Kern wird das
        /// Ergebnis ausnahmslos gegen „Ja" geprüft.</para>
        /// </summary>
        /// <param name="text">Der Fragetext.</param>
        /// <param name="titel">Fenstertitel; <c>null</c> = ohne.</param>
        /// <param name="warnend">
        /// <c>true</c> = Warnsymbol statt Fragezeichen. Zwei der vier Rückfragen des
        /// Kerns tragen heute ein Warnsymbol (Löschen einer Speichervariante, Löschen
        /// eines Projekts); das Symbol ist dort eine Aussage und geht nicht verloren.
        /// </param>
        /// <param name="vorgabeNein">
        /// <c>true</c> = die Schaltfläche „Nein" hat den Fokus. Der Löschdialog des
        /// Projekts setzt das seit jeher (<c>MessageBoxDefaultButton.Button2</c>) —
        /// ein versehentliches Bestätigen mit der Eingabetaste soll unmöglich bleiben.
        /// </param>
        bool Frage(string text, string titel = null, bool warnend = false, bool vorgabeNein = false);

        /// <summary>
        /// Dreifachwahl Ja/Nein/Abbruch. Im Kern gibt es dafür heute keine Fundstelle;
        /// die Form steht hier, damit die Masken (iU9 — <c>Views/</c> hat eine
        /// <c>YesNoCancel</c>- und zwei <c>OKCancel</c>-Stellen) die Schnittstelle
        /// später nicht aufbrechen müssen.
        /// </summary>
        JaNeinAbbruch Wahl(string text, string titel = null);

        /// <summary>
        /// Wartekurve an (<c>true</c>) oder aus (<c>false</c>) — die Entsprechung von
        /// <c>Cursor.Current = Cursors.WaitCursor</c>. Ohne Oberfläche folgenlos.
        /// </summary>
        void Warten(bool an);
    }
}
