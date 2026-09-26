namespace EPOS.UI.Dienste;

/// <summary>
/// <b>Die Zwischenablage der Plattform</b> (Konzept Berichtsvorlagen 9.6) — die Naht, über die eine
/// Platzhaltermarke ihren Text kopiert. Unter Windows schreibt der Adapter der Schale
/// (<c>WindowsFormsApplication1/Allgemein/Blazor/WindowsZwischenablage.cs</c>) in die Zwischenablage
/// von Windows, auf iOS <c>EPOS.iOS/Dienste/IosZwischenablage.cs</c> über
/// <c>Clipboard.Default.SetTextAsync</c>.
///
/// <para><b>Ohne Adapter</b> (ein Prüfstand, eine Schale, die keinen einträgt) zeigt die
/// Aufklappung den Text in einem nur lesbaren Feld, markiert — benannt statt still: Der Anwender
/// kopiert dann selbst. Die Marke meldet nur; sie kennt keine Plattform.</para>
/// </summary>
public interface IZwischenablage
{
    /// <summary>Legt <paramref name="text"/> in die Zwischenablage.</summary>
    /// <returns><c>false</c>, wenn es nicht gelang — die Marke zeigt den Text dann markiert.</returns>
    Task<bool> TextSetzenAsync(string text);
}
