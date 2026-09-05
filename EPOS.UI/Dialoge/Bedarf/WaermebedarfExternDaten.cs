namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// EINE Zuordnung „externe Wärmebedarfsganglinie ↔ Projekt" (iU9-W9.4) — das
/// plattformfreie Abbild von <c>Z_ProjWaermebedarfModel</c>.
///
/// <para><b>Der KANAL gilt je Zuordnung, nicht je Ganglinie</b> (Migrationsschritt 48,
/// Entscheidung F18). Dieselbe Ganglinie darf einem Projekt mehrfach zugeordnet sein und
/// dabei einmal in den Heizbedarf und einmal in den Brauchwasserbedarf laufen — deshalb
/// hat jede Zeile ihr eigenes Modell und ihren eigenen Kanal.</para>
///
/// <para><b><see cref="Kanal"/> ist ein STEUERWERT</b>
/// (<c>DbWerte.KANAL_HEIZUNG</c>/<c>_BRAUCHWASSER</c>/<c>_PROZESS</c>), nie ein
/// Anzeigetext. Was der Anwender liest, steht in den Kanaleinträgen des Dialogs.</para>
/// </summary>
public sealed class WaermebedarfExternZeile
{
    /// <summary>Der Schlüssel der Zuordnung (<c>Z_ProjektWaermebedarf.ID_Z</c>).</summary>
    public int IdZ { get; set; }

    /// <summary>Die Stamm-Id der Ganglinie (<c>Tab_Waermebedarf.ID</c>).</summary>
    public int IdGanglinie { get; set; }

    /// <summary>Der Bezeichner der Ganglinie.</summary>
    public string Bezeichner { get; set; } = "";

    /// <summary>Der Bedarfskanal als Steuerwert.</summary>
    public string Kanal { get; set; } = "";
}
