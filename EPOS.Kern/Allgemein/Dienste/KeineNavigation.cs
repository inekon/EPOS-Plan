namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorbelegung von <see cref="Dienste.Navigation"/>: Es gibt keine Masken, also
    /// geht keine auf.
    ///
    /// <para><see cref="OeffneMaske"/> liefert <c>false</c> — „nicht mit OK beendet".
    /// Die drei Fundstellen, die ein Dialogergebnis auswerten, brechen damit ab und
    /// ändern nichts. Das ist genau der Ablauf, den ein Anwender mit „Abbrechen"
    /// auslöst, und deshalb ein bereits erprobter Weg durch den Bestand.</para>
    /// </summary>
    public sealed class KeineNavigation : INavigation
    {
        /// <inheritdoc/>
        public bool OeffneMaske(string maske, params object[] argumente)
        {
            return false;
        }

        /// <inheritdoc/>
        public void MenueAktualisieren()
        {
        }

        /// <inheritdoc/>
        public void AnsichtAktualisieren(string bereich)
        {
        }
    }
}
