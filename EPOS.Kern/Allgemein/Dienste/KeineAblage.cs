namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vorbelegung von <see cref="Dienste.Lizenzablage"/>: Es gibt keine
    /// Geheimnisablage.
    ///
    /// <para><b>Warum sie nichts merkt.</b> Ein Lizenztoken oder ein API-Schlüssel
    /// UNVERSCHLÜSSELT abzulegen wäre schlimmer, als ihn zu verlieren. Ohne
    /// plattformeigenen Schutz — DPAPI unter Windows, Schlüsselbund unter iOS — wird
    /// deshalb nichts geschrieben und nichts gelesen. Der Lizenzstand ist damit
    /// „nicht aktiviert", was der Bestand als gültigen Zustand kennt; ein fehlender
    /// KI-Schlüssel bedeutet „keine KI".</para>
    /// </summary>
    public sealed class KeineAblage : ILizenzAblage
    {
        /// <inheritdoc/>
        public byte[] Lesen(string name, bool nurDiesesGeraet)
        {
            return null;
        }

        /// <inheritdoc/>
        public void Schreiben(string name, byte[] inhalt, bool nurDiesesGeraet)
        {
        }

        /// <inheritdoc/>
        public void Loeschen(string name)
        {
        }

        /// <inheritdoc/>
        public bool Vorhanden(string name)
        {
            return false;
        }

        /// <inheritdoc/>
        public string Ablageort(string name)
        {
            return Dienste.Pfade.Verbinde(Dienste.Pfade.Anwendungsdaten, name);
        }
    }
}
