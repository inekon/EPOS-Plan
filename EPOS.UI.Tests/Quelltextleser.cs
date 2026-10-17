using Xunit;

namespace EPOS.UI.Tests;

/// <summary>
/// Liest Textquellen des Baums — das Hausblatt <c>EPOS.UI/wwwroot/epos-ui.css</c>, weitere
/// Stilblätter, Razor-Seiten, Module — mit einheitlichen Zeilenenden <c>\n</c>.
///
/// <para>Grund: Im Repository und auf Linux stehen die Dateien mit LF, im Windows-Klon mit
/// <c>core.autocrlf=true</c> (und <c>text=auto</c>) mit CRLF. Tests, die nach Mustern mit
/// <c>\n</c> suchen oder Zeilen zählen, sollen auf beiden Klonen dasselbe sehen; deshalb liest
/// jeder solche Test über diesen Helfer statt über <see cref="File.ReadAllText(string)"/>.</para>
/// </summary>
internal static class Quelltextleser
{
    /// <summary>Wandelt <c>\r\n</c> und einzelne <c>\r</c> in <c>\n</c>.</summary>
    internal static string Normalisiert(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    /// <summary>Der Inhalt der Datei <paramref name="pfad"/> mit Zeilenenden <c>\n</c>.</summary>
    internal static string Lesen(string pfad) => Normalisiert(File.ReadAllText(pfad));

    /// <summary>
    /// Die Repowurzel: vom Ausgabeverzeichnis so lange aufwärts, bis das Hausblatt dasteht.
    /// </summary>
    internal static string Repowurzel()
    {
        DirectoryInfo? d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "EPOS.UI", "wwwroot", "epos-ui.css")))
            d = d.Parent;

        Assert.NotNull(d);   // das Stilblatt muss im Baum stehen
        return d!.FullName;
    }

    /// <summary><c>EPOS.UI/wwwroot</c> im Baum.</summary>
    internal static string Wwwroot() => Path.Combine(Repowurzel(), "EPOS.UI", "wwwroot");

    /// <summary>
    /// Ein Stilblatt unter <c>EPOS.UI/wwwroot</c> (Vorgabe: das Hausblatt) mit Zeilenenden <c>\n</c>.
    /// </summary>
    internal static string Stilblatt(string datei = "epos-ui.css") => Lesen(Path.Combine(Wwwroot(), datei));
}
