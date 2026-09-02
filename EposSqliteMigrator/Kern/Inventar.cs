using System.Text.Json;
using System.Text.Json.Serialization;

namespace EposSqliteMigrator.Kern;

/// <summary>
/// Abbild von sql\schema\inventar.json (Arbeitspaket S2). Das Inventar ist zugleich
/// die Whitelist der zu migrierenden Tabellen und die Typquelle fuer die Wandlung.
/// Unbekannte JSON-Eigenschaften werden von System.Text.Json still uebergangen -
/// die Datei enthaelt weitere Diagnoseabschnitte, die der Migrator nicht braucht.
/// </summary>
public sealed class Inventar
{
    public InventarQuelle? Quelle { get; set; }
    public string? Erzeugt { get; set; }
    public string? Generator { get; set; }
    public Dictionary<string, InventarTabelle> Tabellen { get; set; } = new();

    private static readonly JsonSerializerOptions Optionen = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static Inventar AusText(string json)
    {
        var inv = JsonSerializer.Deserialize<Inventar>(json, Optionen)
                  ?? throw new InvalidOperationException("inventar.json konnte nicht gelesen werden.");
        if (inv.Tabellen.Count == 0)
            throw new InvalidOperationException("inventar.json enthaelt keine Tabellen.");
        return inv;
    }

    /// <summary>Tabellennamen alphabetisch (ordinal) - die Ladereihenfolge.</summary>
    public List<string> TabellenNamenSortiert()
    {
        var namen = new List<string>(Tabellen.Keys);
        namen.Sort(StringComparer.OrdinalIgnoreCase);
        return namen;
    }
}

public sealed class InventarQuelle
{
    public string? Pfad { get; set; }
    public long Bytes { get; set; }
    public string? Geaendert { get; set; }
    public int SchemaVersion { get; set; }
}

public sealed class InventarTabelle
{
    public List<InventarSpalte> Spalten { get; set; } = new();
    public List<string>? PrimaerSchluessel { get; set; }

    /// <summary>Name der AUTOINCREMENT-Spalte oder null.</summary>
    public string? Autowert { get; set; }

    public List<InventarIndex> Indizes { get; set; } = new();
}

public sealed class InventarSpalte
{
    public string Name { get; set; } = string.Empty;
    public int Ordinal { get; set; }
    public int DaoTyp { get; set; }
    public string DaoTypName { get; set; } = string.Empty;
    public string SqliteTyp { get; set; } = string.Empty;
    public bool Autowert { get; set; }
    public bool Required { get; set; }
    public bool NotNull { get; set; }
    public int? Textlaenge { get; set; }
}

public sealed class InventarIndex
{
    public string Name { get; set; } = string.Empty;
    public string? AccessName { get; set; }
    public bool Unique { get; set; }
    public List<string> Spalten { get; set; } = new();
}
