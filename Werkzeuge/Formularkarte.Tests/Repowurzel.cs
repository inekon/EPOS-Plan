namespace Formularkarte.Tests;

/// <summary>
/// Findet die Repowurzel vom Testverzeichnis aus - erkennbar an
/// <c>WP-Plan.sln</c>. Die Tests lesen die echten Designer-Dateien des
/// Bestands; ein fest verdrahteter relativer Pfad wuerde beim ersten
/// Ordnerumbau brechen.
/// </summary>
public static class Repowurzel
{
    private static string? _pfad;

    public static string Pfad => _pfad ??= Suchen();

    /// <summary>Ein Pfad unterhalb der Repowurzel, mit / als Trenner geschrieben.</summary>
    public static string Datei(string relativ) =>
        Path.Combine(Pfad, relativ.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Der Designer einer Maske unter <c>WindowsFormsApplication1/Views/</c>.</summary>
    public static string Designer(string relativ) =>
        Datei("WindowsFormsApplication1/Views/" + relativ);

    private static string Suchen()
    {
        var ordner = new DirectoryInfo(AppContext.BaseDirectory);
        while (ordner is not null)
        {
            if (File.Exists(Path.Combine(ordner.FullName, "WP-Plan.sln"))) return ordner.FullName;
            ordner = ordner.Parent;
        }
        throw new InvalidOperationException(
            "Repowurzel nicht gefunden (gesucht wurde WP-Plan.sln oberhalb von " + AppContext.BaseDirectory + ").");
    }
}
