namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorbelegung von <see cref="Dienste.GeraeteId"/>: keine Gerätemerkmale.
    ///
    /// <para>Ohne Plattformfassung gibt es nichts, was ein Gerät stabil ausweist. Ein
    /// ERFUNDENES Merkmal wäre schlimmer als keines — es würde einen Lizenzabdruck
    /// erzeugen, der beim nächsten Start ein anderer sein kann. <c>""</c> führt
    /// stattdessen zu einem stets gleichen, offensichtlich unbrauchbaren Abdruck, den
    /// der Lizenzserver nicht bindet.</para>
    /// </summary>
    public sealed class KeineGeraeteId : IGeraeteId
    {
        /// <inheritdoc/>
        public string Kennung
        {
            get { return ""; }
        }

        /// <inheritdoc/>
        public string Anzeige
        {
            get { return ""; }
        }
    }
}
