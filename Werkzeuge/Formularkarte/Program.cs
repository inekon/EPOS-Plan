using System.Text;
using Formularkarte;

// Werkzeug "Formularkarte" - Aufruf siehe LIESMICH.md im selben Ordner.
//
//   dotnet run --project Werkzeuge/Formularkarte -- <Form_X.Designer.cs>
//        [--resx <pfad>] [--karte <ausgabe.md>] [--razor <ausgabe.razor>]
//   dotnet run --project Werkzeuge/Formularkarte -- --alle <Ordner> --ziel <Ordner>
//
// Ohne --karte/--razor geht die Feldkarte nach stdout.

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 0 || args[0] is "--hilfe" or "-h" or "--help")
{
    Hilfe();
    return 0;
}

string? quelle = null, resx = null, karte = null, razor = null, alle = null, ziel = null, wurzel = null;

for (var i = 0; i < args.Length; i++)
{
    switch (args[i])
    {
        case "--resx": resx = Naechstes(args, ref i); break;
        case "--karte": karte = Naechstes(args, ref i); break;
        case "--razor": razor = Naechstes(args, ref i); break;
        case "--alle": alle = Naechstes(args, ref i); break;
        case "--ziel": ziel = Naechstes(args, ref i); break;
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

var bom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

try
{
    if (alle is not null)
    {
        if (!Directory.Exists(alle))
        {
            Console.Error.WriteLine("Ordner nicht gefunden: " + alle);
            return 2;
        }

        var ergebnis = Stapel.Laufen(alle, ziel, wurzel);
        var uebersicht = Stapel.Uebersicht(ergebnis, alle);

        if (ziel is not null)
        {
            var pfad = Path.Combine(ziel, "UEBERSICHT.md");
            File.WriteAllText(pfad, uebersicht, bom);
            Console.WriteLine("Geschrieben nach " + ziel + " (" + ergebnis.Masken + " Masken, Uebersicht: " + pfad + ")");
        }
        Console.WriteLine(uebersicht);
        return ergebnis.Fehler.Count == 0 ? 0 : 1;
    }

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

    var maske = Kartenbau.Vollstaendig(quelle, resx, wurzel);

    if (karte is null && razor is null)
    {
        Console.WriteLine(FeldkarteSchreiber.Schreiben(maske));
        return 0;
    }
    if (karte is not null)
    {
        Verzeichnis(karte);
        File.WriteAllText(karte, FeldkarteSchreiber.Schreiben(maske), bom);
        Console.WriteLine("Feldkarte: " + karte);
    }
    if (razor is not null)
    {
        Verzeichnis(razor);
        File.WriteAllText(razor, RazorSchreiber.Schreiben(maske), bom);
        Console.WriteLine("Skelett:   " + razor);

        // Razor leitet den Komponentennamen aus dem Dateinamen ab und laesst
        // dabei keinen kleinen Anfangsbuchstaben zu (RZ10011).
        var stamm = Path.GetFileNameWithoutExtension(razor);
        if (stamm.Length > 0 && char.IsLower(stamm[0]))
        {
            Console.WriteLine("Hinweis:   '" + stamm + "' faengt klein an - Razor braucht einen grossen " +
                              "Anfangsbuchstaben (RZ10011). Vorschlag: " + RazorSchreiber.Dateiname(maske));
        }
    }
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

static void Verzeichnis(string pfad)
{
    var ordner = Path.GetDirectoryName(Path.GetFullPath(pfad));
    if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);
}

static void Hilfe()
{
    Console.WriteLine("""
        Formularkarte - Feldkarte und Razor-Skelett aus einer WinForms-Designer-Datei.

          Formularkarte <Form_X.Designer.cs> [--resx <pfad>]
                        [--karte <ausgabe.md>] [--razor <ausgabe.razor>]
          Formularkarte --alle <Ordner> [--ziel <Ordner>] [--wurzel <Projektordner>]

          --resx    abweichende neutrale .resx (sonst die neben dem Designer)
          --karte   Feldkarte als Markdown; ohne --karte/--razor geht sie nach stdout
          --razor   Razor-Skelett fuer EPOS.UI/Dialoge/
          --alle    Stapellauf ueber alle *.Designer.cs unterhalb des Ordners
          --ziel    Ausgabeordner des Stapellaufs (ohne: nur die Uebersicht)
          --wurzel  Projektordner fuer die Suche nach ShowDialog-Aufrufern
        """);
}
