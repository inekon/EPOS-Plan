namespace WindowsFormsApplication1
{
    /// <summary>
    /// Geräte-Identität für die Lizenzbindung. Eine Fundstelle:
    /// <c>Allgemein\Lizenz\GeraeteId.cs</c>.
    ///
    /// <para><b>Was <see cref="Kennung"/> liefern muss.</b> Die ROHEN Merkmale, aus
    /// denen <c>GeraeteId.Ermitteln</c> den SHA-256-Abdruck bildet — unter Windows
    /// <c>&lt;MachineGuid&gt;|&lt;Systemlaufwerk&gt;|&lt;Größe&gt;</c>, zeichengleich zum
    /// Bestand. Jede Änderung an dieser Zeichenkette ändert den Abdruck und macht
    /// vorhandene Lizenz-Token ungültig.</para>
    /// </summary>
    public interface IGeraeteId
    {
        /// <summary>
        /// Die rohen Gerätemerkmale, aus denen der Abdruck gebildet wird.
        /// <c>""</c>, wenn keines ermittelbar ist.
        /// </summary>
        string Kennung { get; }

        /// <summary>Anzeigename des Geräts, im Bestand <c>"RECHNER (benutzer)"</c>.</summary>
        string Anzeige { get; }
    }
}
