using WindowsFormsApplication1;

namespace Rasterprobe.Wirt.Seiten;

/// <summary>
/// <b>Der Dateiwähler der Gebäudeimport-Sichtprobe</b> (Seite <c>/gebaeudeimport</c>) — steht als
/// <see cref="Dienste.Datei"/> im Wirt. Die Hülle des Gebäudeimports wählt über
/// <c>Dienste.Datei.DateiOeffnenAsync</c>, prüft danach Dateiart und Größe und lehnt benannt ab;
/// dieser Wähler ersetzt allein das Fenster der Plattform, der Rest ist der Weg der Anwendung.
///
/// <para><b>Die Antwort gilt je Aufruf, nicht je Prozess:</b> Die Seite legt sie in
/// <see cref="Antwort"/> (ein <see cref="AsyncLocal{T}"/>) unmittelbar um den Delegaten der Hülle —
/// zwei Browserfenster mit verschiedenen Proben stören sich nicht. Ohne Vormerkung antwortet er
/// leer wie die Vorgabe <c>KeineDateiwahl</c> (= abgebrochen).</para>
/// </summary>
public sealed class Probenwaehler : IDateiDienst
{
    private static readonly AsyncLocal<string?> _antwort = new();

    /// <summary>Der Pfad, den der Wähler im laufenden Aufruf liefert; <c>null</c> = keiner.</summary>
    public static string? Antwort
    {
        get => _antwort.Value;
        set => _antwort.Value = value;
    }

    /// <inheritdoc />
    public string DateiOeffnen(string titel, string filter, string startOrdner) => _antwort.Value ?? "";

    /// <inheritdoc />
    public string DateiSpeichern(string titel, string filter, string vorschlag) => "";

    /// <inheritdoc />
    public string OrdnerWaehlen(string titel, string startOrdner) => "";

    /// <inheritdoc />
    public bool MitSystemOeffnen(string pfad) => false;

    // =====================================================================================
    //  Die Proben unter Referenzlaeufe/Importproben
    // =====================================================================================

    /// <summary>
    /// Der Ordner <c>Referenzlaeufe/Importproben</c> — vom Programmordner aufwärts gesucht wie in den
    /// Tests (<c>GbxmlImportTests.Probe</c>); <c>null</c>, wenn er nicht zu finden ist.
    /// </summary>
    public static string? Probenordner()
    {
        DirectoryInfo? d = new(AppContext.BaseDirectory);
        for (int i = 0; i < 10 && d is not null; i++, d = d.Parent)
        {
            string ordner = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben");
            if (Directory.Exists(ordner)) return ordner;
        }
        return null;
    }

    /// <summary>
    /// Der absolute Pfad einer Probe; <c>null</c>, wenn der Name kein nackter Dateiname ist oder die
    /// Datei fehlt. Jede Datei des Ordners ist erlaubt — auch eine fremde Art, deren Ablehnung die
    /// Hülle benennt.
    /// </summary>
    public static string? Probe(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name)) return null;
        string? ordner = Probenordner();
        if (ordner is null) return null;
        string pfad = Path.Combine(ordner, name);
        return File.Exists(pfad) ? pfad : null;
    }

    /// <summary>Die Gebäudeproben des Ordners (gbXML und IFC), nach Namen geordnet.</summary>
    public static IReadOnlyList<string> Gebaeudeproben()
    {
        string? ordner = Probenordner();
        if (ordner is null) return Array.Empty<string>();
        return Directory.EnumerateFiles(ordner)
            .Select(Path.GetFileName)
            .OfType<string>()
            .Where(n => n.StartsWith("gbxml_", StringComparison.OrdinalIgnoreCase)
                     || n.StartsWith("ifc", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
