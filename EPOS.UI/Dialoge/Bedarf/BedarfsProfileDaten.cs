namespace EPOS.UI.Dialoge.Bedarf;

/// <summary>
/// EINE Zuordnung „Bedarfsprofil ↔ Projekt" (iU9-W9.5) — das plattformfreie Abbild von
/// <c>Z_ProjektProzesswaermeModel</c>, <c>Z_ProjektStromverbraucherModel</c> bzw.
/// <c>Z_ProjektBrauchwasserModel</c>. Die drei Modelle tragen dieselben fünf Felder unter
/// drei Namen; hier steht EIN Satz.
///
/// <para><b><see cref="IdZ"/> ist der Schlüssel.</b> Alle drei Vorläufer suchen die zu
/// entfernende Zeile über Name UND <c>ID_Z</c> — der Name allein ist nicht eindeutig,
/// dasselbe Profil darf einem Projekt mehrfach zugeordnet sein.</para>
/// </summary>
public sealed class BedarfsProfilZeile
{
    /// <summary>Der Schlüssel der Zuordnung.</summary>
    public int IdZ { get; set; }

    /// <summary>Die Stamm-Id des Profils.</summary>
    public int IdStamm { get; set; }

    /// <summary>Der Bezeichner des Profils.</summary>
    public string Name { get; set; } = "";

    /// <summary>Der Jahresverbrauch dieser Zuordnung.</summary>
    public double Summe { get; set; }
}

/// <summary>
/// EINE Zeile des Profilkatalogs (iU9-W9.5). Prozesswärme und Brauchwasser zeigen Name
/// UND Typ in einem Raster, der Stromverbraucher nur den Namen in einer Liste — der
/// Unterschied ist Bestand und bleibt.
/// </summary>
/// <param name="Name">Der Bezeichner.</param>
/// <param name="Typ">Der Profiltyp; beim Stromverbraucher nicht angezeigt.</param>
public sealed record BedarfsKatalogZeile(string Name, string Typ);

/// <summary>
/// Der Infoblock zu einem Profil (iU9-W9.5) — <c>SetProzessInfo</c> der drei Vorläufer.
/// </summary>
/// <param name="Name">Der Bezeichner.</param>
/// <param name="Beschreibung">Die Beschreibung aus dem Kopfsatz.</param>
/// <param name="Typ">Der Profiltyp aus dem Kopfsatz.</param>
public sealed record BedarfsProfilInfo(string Name, string Beschreibung, string Typ);
