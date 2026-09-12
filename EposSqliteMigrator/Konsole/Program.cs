using System.Globalization;
using System.Text;
using EposSqliteMigrator.Kern;

namespace EposSqliteMigrator.Konsole;

/// <summary>
/// Konsolenhuelle: parst die Argumente und ruft den Kern. Es steckt hier bewusst
/// keine Migrationslogik - die Anwendung bindet spaeter denselben Kern ein.
/// </summary>
public static class Program
{
    public static int Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { /* umgeleitete Ausgabe */ }

        MigrationsOptionen opt;
        try
        {
            if (args.Any(a => a is "--hilfe" or "--help" or "-h" or "/?"))
            {
                Hilfe();
                return ExitCode.Erfolg;
            }
            opt = Argumente(args);
        }
        catch (ArgumentException ex)
        {
            Console.Error.WriteLine("Fehler in den Argumenten: " + ex.Message);
            Console.Error.WriteLine();
            Hilfe();
            return ExitCode.Fehler;
        }

        Console.WriteLine($"EposSqliteMigrator {Migrator.Version}");
        Console.WriteLine($"  Quelle       : {Path.GetFullPath(opt.Quelle)}");
        Console.WriteLine($"  Ziel         : {Path.GetFullPath(opt.Ziel)}");
        Console.WriteLine($"  orphanPolicy : {opt.OrphanPolicy}");
        Console.WriteLine();

        var migrator = new Migrator(Console.WriteLine);
        var erg = migrator.Ausfuehren(opt);

        Console.WriteLine();
        if (erg.Fehlermeldung is not null)
        {
            Console.Error.WriteLine("ABBRUCH: " + erg.Fehlermeldung);
            Console.WriteLine();
        }
        Console.WriteLine($"Tabellen        : {erg.Tabellen.Count}");
        Console.WriteLine($"Zeilen gesamt   : {erg.ZeilenGesamt.ToString("N0", CultureInfo.GetCultureInfo("de-DE"))}");
        Console.WriteLine($"Dauer           : {erg.Dauer.TotalSeconds:F2} s");
        Console.WriteLine($"Datenbeweis     : {(erg.BeweisVollstaendig ? "alle Pruefsummen gleich" : "ABWEICHUNGEN")}");
        Console.WriteLine($"integrity_check : {erg.IntegrityCheck}");
        Console.WriteLine($"foreign_key_chk : {(erg.FkVerletzungen.Count == 0 ? "keine Verletzung" : erg.FkVerletzungen.Count + " Verletzung(en)")}");
        Console.WriteLine($"Case-Drift      : {erg.CaseDrifts.Count} Befund(e) in {erg.CaseDriftGeprueft.Count} Textschluesseln");
        Console.WriteLine($"nicht migriert  : {erg.NichtMigriert.Count} Quelltabelle(n)");
        Console.WriteLine($"Bericht         : {erg.BerichtPfad}");
        Console.WriteLine($"Exit-Code       : {erg.Code}");

        return erg.Code;
    }

    private static MigrationsOptionen Argumente(string[] args)
    {
        var opt = new MigrationsOptionen();

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a.ToLowerInvariant())
            {
                case "--quelle":
                    opt.Quelle = Wert(args, ref i, "--quelle");
                    break;
                case "--ziel":
                    opt.Ziel = Wert(args, ref i, "--ziel");
                    break;
                case "--bericht":
                    opt.Bericht = Wert(args, ref i, "--bericht");
                    break;
                case "--orphanpolicy":
                    var w = Wert(args, ref i, "--orphanPolicy");
                    opt.OrphanPolicy = Enum.TryParse<OrphanPolicy>(w, ignoreCase: true, out var p)
                        ? p
                        : throw new ArgumentException(
                            $"--orphanPolicy kennt nur 'Abbruch' oder 'AlsProtokollAussetzen', nicht '{w}'.");
                    break;
                default:
                    throw new ArgumentException($"Unbekanntes Argument: {a}");
            }
        }

        if (string.IsNullOrWhiteSpace(opt.Ziel))
            throw new ArgumentException("--ziel fehlt (Pfad der zu erzeugenden .sqlite-Datei).");

        return opt;
    }

    private static string Wert(string[] args, ref int i, string name)
    {
        if (i + 1 >= args.Length) throw new ArgumentException($"{name} erwartet einen Wert.");
        return args[++i];
    }

    private static void Hilfe()
    {
        Console.WriteLine("EposSqliteMigrator - migriert einen EPOS-Plan-Access-Bestand nach SQLite.");
        Console.WriteLine();
        Console.WriteLine("  EposSqliteMigrator --ziel <pfad.sqlite>");
        Console.WriteLine("                     [--quelle <pfad.accdb>]");
        Console.WriteLine("                     [--orphanPolicy Abbruch|AlsProtokollAussetzen]");
        Console.WriteLine("                     [--bericht <pfad.md>]");
        Console.WriteLine();
        Console.WriteLine($"  --quelle        Standard: {MigrationsOptionen.QuelleStandard}");
        Console.WriteLine("  --orphanPolicy  Standard: Abbruch");
        Console.WriteLine("  --bericht       Standard: neben dem Ziel, Migrationsbericht_<quelle>_<zeit>.md");
        Console.WriteLine();
        Console.WriteLine("Exit-Codes: 0 Erfolg | 1 Fehler | 2 Quelle geoeffnet (.laccdb)");
        Console.WriteLine("            3 Fremdschluesselverletzungen (orphanPolicy=Abbruch)");
        Console.WriteLine("            4 Datenbeweis fehlgeschlagen");
        Console.WriteLine();
        Console.WriteLine("Die Quelldatenbank wird ausschliesslich gelesen. Bei jedem Fehler wird die");
        Console.WriteLine("Zieldatei geloescht - die .accdb ist das Rollback.");
    }
}
