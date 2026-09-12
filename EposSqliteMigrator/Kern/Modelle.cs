namespace EposSqliteMigrator.Kern;

/// <summary>Umgang mit Fremdschluesselverletzungen (Waisen) im Ziel.</summary>
public enum OrphanPolicy
{
    /// <summary>Verletzung gefunden -> Zieldatei loeschen, Abbruch mit Exit 3.</summary>
    Abbruch,

    /// <summary>Verletzung bleibt in der Datei; der Constraint bleibt bestehen,
    /// die Verletzung wird ausgehalten und steht namentlich im Bericht.</summary>
    AlsProtokollAussetzen,
}

/// <summary>Rueckgabewerte des Werkzeugs.</summary>
public static class ExitCode
{
    public const int Erfolg = 0;
    public const int Fehler = 1;             // Argumente, Version, Ziel existiert, Wandlung, sonstiges
    public const int SitzungOffen = 2;       // .laccdb neben der Quelle
    public const int Waisen = 3;             // foreign_key_check + orphanPolicy Abbruch
    public const int BeweisFehlgeschlagen = 4; // Zeilenzahl oder Pruefsumme abweichend
}

public sealed class MigrationsOptionen
{
    public const string QuelleStandard = @"C:\ProgramData\EPOS_PLAN\Kenndaten.accdb";

    public string Quelle { get; set; } = QuelleStandard;
    public string Ziel { get; set; } = string.Empty;
    public OrphanPolicy OrphanPolicy { get; set; } = OrphanPolicy.Abbruch;
    public string? Bericht { get; set; }

    /// <summary>Berichtspfad; ohne Angabe neben dem Ziel.</summary>
    public string BerichtPfad(DateTime start)
    {
        if (!string.IsNullOrWhiteSpace(Bericht)) return Path.GetFullPath(Bericht!);
        var ordner = Path.GetDirectoryName(Path.GetFullPath(Ziel));
        if (string.IsNullOrEmpty(ordner)) ordner = Directory.GetCurrentDirectory();
        var quellname = Path.GetFileNameWithoutExtension(Quelle);
        return Path.Combine(ordner, $"Migrationsbericht_{quellname}_{start:yyyyMMdd_HHmmss}.md");
    }
}

/// <summary>Geordneter Abbruch mit vorgesehenem Rueckgabewert.</summary>
public sealed class MigrationsAbbruch : Exception
{
    public int Code { get; }

    public MigrationsAbbruch(int code, string meldung) : base(meldung) => Code = code;
}

public sealed class TabellenErgebnis
{
    public string Name { get; init; } = string.Empty;
    public long QuelleCount { get; set; } = -1;
    public long QuelleGelesen { get; set; } = -1;
    public long ZielCount { get; set; } = -1;
    public string QuellePruefsumme { get; set; } = string.Empty;
    public string ZielPruefsumme { get; set; } = string.Empty;
    public double Sekunden { get; set; }

    public bool ZeilenGleich => QuelleCount == QuelleGelesen && QuelleGelesen == ZielCount;
    public bool PruefsummeGleich => QuellePruefsumme.Length > 0 && QuellePruefsumme == ZielPruefsumme;
    public bool Ok => ZeilenGleich && PruefsummeGleich;
}

public sealed record FkVerletzung(string Tabelle, string RowId, string Elterntabelle, string FkId);

public sealed record SeqBefund(string Tabelle, string Spalte, long MaxId, long SeqVorher, long SeqNachher, string Vermerk);

public sealed record CaseDrift(string Tabelle, string Spalte, string Kleinform, IReadOnlyList<string> Werte);

public sealed class MigrationsErgebnis
{
    public int Code { get; set; } = ExitCode.Erfolg;
    public string? Fehlermeldung { get; set; }

    public string Quelle { get; set; } = string.Empty;
    public long QuelleBytes { get; set; }
    public DateTime QuelleGeaendert { get; set; }
    public int SchemaVersion { get; set; }
    public string Ziel { get; set; } = string.Empty;
    public long ZielBytes { get; set; }
    public string BerichtPfad { get; set; } = string.Empty;
    public string Werkzeugversion { get; set; } = string.Empty;
    public DateTime Start { get; set; }
    public TimeSpan Dauer { get; set; }
    public OrphanPolicy OrphanPolicy { get; set; }

    public bool NurLesendGeoeffnet { get; set; }
    public string OeffnungsVermerk { get; set; } = string.Empty;

    public List<TabellenErgebnis> Tabellen { get; } = new();
    public List<string> NichtMigriert { get; } = new();

    /// <summary>Anzahl der Quellobjekte je TABLE_TYPE des OleDb-Schema-Rowsets.</summary>
    public Dictionary<string, int> QuellobjektArten { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gespeicherte Access-Abfragen ohne View im Ziel (S2-Entscheidung, kein Datenverlust).</summary>
    public List<string> AbfragenOhneView { get; } = new();
    public List<FkVerletzung> FkVerletzungen { get; } = new();
    public string IntegrityCheck { get; set; } = "(nicht ausgefuehrt)";
    public List<SeqBefund> SeqBefunde { get; } = new();
    public List<CaseDrift> CaseDrifts { get; } = new();
    public List<string> CaseDriftGeprueft { get; } = new();
    public List<string> Warnungen { get; } = new();

    public long ZeilenGesamt => Tabellen.Sum(t => Math.Max(0, t.ZielCount));
    public bool BeweisVollstaendig => Tabellen.Count > 0 && Tabellen.All(t => t.Ok);
}
