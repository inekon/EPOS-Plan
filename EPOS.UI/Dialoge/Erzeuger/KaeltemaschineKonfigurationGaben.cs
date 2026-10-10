using System;
using System.Collections.Generic;

namespace EPOS.UI.Dialoge.Erzeuger;

/// <summary>
/// <b>Die Gaben der Komponente <see cref="KaeltemaschineKonfiguration"/></b> für EINE Anlage (Welle KB-B, Entwurf
/// Kältebereich 3.3): der gelesene Feldsatz samt Kennwerten der Projektkopie, die Stromträger des Projekts für die
/// Wahl des Kühlträgers und der Stromträger des Projekts selbst. Die Hülle baut sie aus
/// <c>KaeltemaschineAnlageHuelle</c> — dieselben Daten wie im Dialog „Kältemaschinen im Projekt“.
/// </summary>
public sealed class KaeltemaschineKonfigurationGaben
{
    /// <summary>Der gelesene Feldsatz der Anlage; die Seite bearbeitet eine Kopie davon.</summary>
    public KaeltemaschineAnlageDaten Daten = new();

    /// <summary>Die Stromträger des Projekts (Id, Anzeigetext).</summary>
    public IReadOnlyList<(int Id, string Text)> Stromtraeger = Array.Empty<(int, string)>();

    /// <summary>Der Stromträger des Projekts; 0 = keiner.</summary>
    public int ProjektStromtraeger;
}
