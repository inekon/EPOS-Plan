using System.Text;
using Formularkarte;

// Werkzeug "Formularkarte" - Aufruf siehe LIESMICH.md im selben Ordner.
//
//   dotnet run --project Werkzeuge/Formularkarte -- <Form_X.Designer.cs> [--karte <ausgabe.md>]
//
// Ohne --karte geht die Feldkarte nach stdout.

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 0 || args[0] is "--hilfe" or "-h" or "--help")
{
    Hilfe();
    return 0;
}

string? quelle = null, karte = null, wurzel = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--karte": karte = Naechstes(args, ref i); break;
        case "--wurzel": wurzel = Naechstes(args, ref i); break;
        default:
            if (args[i].StartsWith("--", StringComparison.Ordinal))
            {
                Console.Error.WriteLine("Unbekannte Angabe: " + args[i]);
                Hilfe();
                return 2;
            }
            quelle ??= args[i];
            break;
    }
}

try
{
    if (quelle is null)
    {
        Console.Error.WriteLine("Keine Designer-Datei angegeben.");
        Hilfe();
        return 2;
    }
    if (!File.Exists(quelle))
    {
        Console.Error.WriteLine("Datei nicht gefunden: " + quelle);
        return 2;
    }

    var maske = Kartenbau.Vollstaendig(quelle, wurzel);
    var inhalt = FeldkarteSchreiber.Schreiben(maske);

    if (karte is null)
    {
        Console.WriteLine(inhalt);
        return 0;
    }

    var ordner = Path.GetDirectoryName(Path.GetFullPath(karte));
    if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);
    File.WriteAllText(karte, inhalt, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    Console.WriteLine("Feldkarte: " + karte);
    return 0;
}
catch (Exception fehler)
{
    Console.Error.WriteLine(fehler.GetType().Name + ": " + fehler.Message);
    return 1;
}

static string Naechstes(string[] args, ref int i)
{
    if (i + 1 >= args.Length) throw new ArgumentException("Zu '" + args[i] + "' fehlt der Wert.");
    return args[++i];
}

static void Hilfe()
{
    Console.WriteLine("""
        Formularkarte - Feldkarte aus einer WinForms-Designer-Datei.

          Formularkarte <Form_X.Designer.cs> [--karte <ausgabe.md>] [--wurzel <Projektordner>]

          --karte   Feldkarte als Markdown; ohne --karte geht sie nach stdout
          --wurzel  Projektordner fuer die Suche nach ShowDialog-Aufrufern
        """);
}
