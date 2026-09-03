using System.Globalization;
using System.Text;

namespace Formularkarte;

/// <summary>Was ein Stapellauf ueber eine Maske festhaelt.</summary>
public sealed class Stapelzeile
{
    public required string Datei { get; init; }
    public string Bezeichner { get; init; } = "";
    public int Zeilen { get; init; }
    public int OhneBeschriftung { get; init; }
    public int Abschnitte { get; init; }
    public bool Lokalisiert { get; init; }
    public bool Gelesen { get; init; }
    public string Bemerkung { get; init; } = "";
    public SortedDictionary<string, int> Typen { get; init; } = new(StringComparer.Ordinal);
    public SortedSet<string> Unbekannt { get; init; } = new(StringComparer.Ordinal);
}

/// <summary>Das Ergebnis eines Stapellaufs ueber einen ganzen Ordnerbaum.</summary>
public sealed class Stapelergebnis
{
    public List<Stapelzeile> Zeilen { get; } = new();

    /// <summary>Dateien, in denen kein <c>InitializeComponent</c> steht (Resource/Settings).</summary>
    public List<string> KeineMaske { get; } = new();

    /// <summary>Dateien, die der Leser nicht verarbeiten konnte - muss leer bleiben.</summary>
    public List<string> Fehler { get; } = new();

    public int Masken => Zeilen.Count;
    public int Dateien => Zeilen.Count + KeineMaske.Count + Fehler.Count;
    public int Lokalisierte => Zeilen.Count(z => z.Lokalisiert);
    public int Felder => Zeilen.Sum(z => z.Zeilen);
    public int OhneBeschriftung => Zeilen.Sum(z => z.OhneBeschriftung);

    /// <summary>Zaehlung ueber alle Masken je Steuerelementtyp.</summary>
    public SortedDictionary<string, int> Typen
    {
        get
        {
            var gesamt = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var zeile in Zeilen)
            {
                foreach (var (typ, anzahl) in zeile.Typen)
                {
                    gesamt.TryGetValue(typ, out var vorher);
                    gesamt[typ] = vorher + anzahl;
                }
            }
            return gesamt;
        }
    }

    /// <summary>Typen, die der Leser nicht kennt - je Typ die Zahl der Masken.</summary>
    public SortedDictionary<string, int> Unbekannt
    {
        get
        {
            var gesamt = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var typ in Zeilen.SelectMany(z => z.Unbekannt))
            {
                gesamt.TryGetValue(typ, out var vorher);
                gesamt[typ] = vorher + 1;
            }
            return gesamt;
        }
    }
}

/// <summary>
/// Der Stapellauf: liest alle Designer-Dateien eines Ordnerbaums, schreibt
/// Karte und Skelett je Maske und fasst die Zaehlung in einer Uebersicht
/// zusammen. Das ist das Vollstaendigkeitsnetz fuer iU9 - jede Maske, die der
/// Leser nicht schafft, wird hier sichtbar.
/// </summary>
public static class Stapel
{
    /// <summary>Alle Designer-Dateien unterhalb eines Ordners, Gross-/Kleinschreibung egal.</summary>
    public static List<string> Dateien(string ordner) =>
        Directory.EnumerateFiles(ordner, "*.cs", SearchOption.AllDirectories)
            .Where(d => d.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Where(d => !d.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .Where(d => !d.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            .OrderBy(d => d, StringComparer.Ordinal)
            .ToList();

    /// <summary>Laeuft ueber alle Designer-Dateien; <paramref name="ziel"/> null = nur zaehlen.</summary>
    public static Stapelergebnis Laufen(string ordner, string? ziel, string? suchwurzel = null)
    {
        var ergebnis = new Stapelergebnis();
        if (ziel is not null) Directory.CreateDirectory(ziel);

        foreach (var datei in Dateien(ordner))
        {
            try
            {
                var maske = DesignerLeser.Versuchen(datei);
                if (maske is null)
                {
                    ergebnis.KeineMaske.Add(datei);
                    continue;
                }

                ResxLeser.Anwenden(maske, null);
                LabelRegel.Anwenden(maske);
                QuelltextLeser.Anwenden(maske, suchwurzel);

                var abschnitte = Kartenbau.Abschnitte(maske);
                var zeilen = abschnitte.SelectMany(a => a.Zeilen).ToList();

                var stapelzeile = new Stapelzeile
                {
                    Datei = maske.Datei,
                    Bezeichner = maske.Bezeichner,
                    Zeilen = zeilen.Count,
                    OhneBeschriftung = zeilen.Count(z => z.Element.Art == Art.Feld && string.IsNullOrWhiteSpace(z.TextDe)),
                    Abschnitte = abschnitte.Count(a => a.Traeger is not null),
                    Lokalisiert = maske.Lokalisiert,
                    Gelesen = true,
                    Typen = Kartenbau.Typzaehlung(maske)
                };
                foreach (var element in maske.Steuerelemente.Where(s => s.Art == Art.Sonstig))
                {
                    stapelzeile.Unbekannt.Add(element.Typ);
                }
                ergebnis.Zeilen.Add(stapelzeile);

                if (ziel is not null)
                {
                    var kopf = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
                    File.WriteAllText(Path.Combine(ziel, maske.Bezeichner + ".karte.md"),
                                      FeldkarteSchreiber.Schreiben(maske), kopf);
                    File.WriteAllText(Path.Combine(ziel, RazorSchreiber.Dateiname(maske)),
                                      RazorSchreiber.Schreiben(maske), kopf);
                }
            }
            catch (Exception fehler)
            {
                ergebnis.Fehler.Add(datei + ": " + fehler.GetType().Name + " - " + fehler.Message);
            }
        }
        return ergebnis;
    }

    /// <summary>
    /// Die Zielspalte der Typtabelle. Zwei Typen haengen nicht am Typ allein:
    /// Ein Knopf wird je nach Namen SpeichernLeiste, InfoKnopf oder bleibt ein
    /// eigener Knopf; ein Label ist meist die Beschriftung eines Feldes und
    /// wird nur ohne Feld zu einer eigenen Textzeile.
    /// </summary>
    private static string Zielspalte(string typ) => typ switch
    {
        "Button" => "SpeichernLeiste / InfoKnopf / Knopf (pruefen)",
        "Label" or "LinkLabel" => "Beschriftung eines Feldes, sonst Text",
        _ => Kartenbau.Ziel(new Steuerelement { Name = "x", Typ = typ, Art = Typtabelle.Einordnen(typ) }).Komponente
    };

    /// <summary>Die Uebersicht als Markdown - Kopfzahlen, Typtabelle, Maskentabelle.</summary>
    public static string Uebersicht(Stapelergebnis ergebnis, string ordner)
    {
        var werkzeug = new StringBuilder();
        werkzeug.Append("# Stapellauf Formularkarte\n\n");
        werkzeug.Append("Gelesener Baum: `").Append(ordner.Replace('\\', '/')).Append("`  \n");
        werkzeug.Append("Datum: ").Append(DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)).Append("\n\n");

        werkzeug.Append("| Kennzahl | Wert |\n|---|---|\n");
        werkzeug.Append("| Designer-Dateien | ").Append(ergebnis.Dateien).Append(" |\n");
        werkzeug.Append("| davon Masken (mit InitializeComponent) | ").Append(ergebnis.Masken).Append(" |\n");
        werkzeug.Append("| davon ohne InitializeComponent | ").Append(ergebnis.KeineMaske.Count).Append(" |\n");
        werkzeug.Append("| nicht lesbar | ").Append(ergebnis.Fehler.Count).Append(" |\n");
        werkzeug.Append("| lokalisiert (ApplyResources) | ").Append(ergebnis.Lokalisierte).Append(" |\n");
        werkzeug.Append("| Kartenzeilen gesamt | ").Append(ergebnis.Felder).Append(" |\n");
        werkzeug.Append("| Felder ohne Beschriftung | ").Append(ergebnis.OhneBeschriftung).Append(" |\n\n");

        werkzeug.Append("## Steuerelemente je Typ\n\n| Typ | Anzahl | Ziel |\n|---|---|---|\n");
        foreach (var (typ, anzahl) in ergebnis.Typen.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
        {
            werkzeug.Append("| ").Append(typ).Append(" | ").Append(anzahl).Append(" | ")
                    .Append(Zielspalte(typ)).Append(" |\n");
        }
        werkzeug.Append('\n');

        var unbekannt = ergebnis.Unbekannt;
        werkzeug.Append("## Unbekannte Typen\n\n");
        if (unbekannt.Count == 0)
        {
            werkzeug.Append("Keine.\n\n");
        }
        else
        {
            werkzeug.Append("| Typ | Masken |\n|---|---|\n");
            foreach (var (typ, anzahl) in unbekannt.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
            {
                werkzeug.Append("| ").Append(typ).Append(" | ").Append(anzahl).Append(" |\n");
            }
            werkzeug.Append('\n');
        }

        werkzeug.Append("## Masken\n\n| Maske | Zeilen | Abschnitte | ohne Beschriftung | lokalisiert | Datei |\n");
        werkzeug.Append("|---|---|---|---|---|---|\n");
        foreach (var zeile in ergebnis.Zeilen.OrderBy(z => z.Bezeichner, StringComparer.Ordinal))
        {
            werkzeug.Append("| ").Append(zeile.Bezeichner)
                    .Append(" | ").Append(zeile.Zeilen)
                    .Append(" | ").Append(zeile.Abschnitte)
                    .Append(" | ").Append(zeile.OhneBeschriftung)
                    .Append(" | ").Append(zeile.Lokalisiert ? "ja" : "-")
                    .Append(" | `").Append(zeile.Datei).Append("` |\n");
        }
        werkzeug.Append('\n');

        if (ergebnis.KeineMaske.Count > 0)
        {
            werkzeug.Append("## Ohne InitializeComponent (keine Maske)\n\n");
            foreach (var datei in ergebnis.KeineMaske) werkzeug.Append("- `").Append(datei.Replace('\\', '/')).Append("`\n");
            werkzeug.Append('\n');
        }
        if (ergebnis.Fehler.Count > 0)
        {
            werkzeug.Append("## Nicht lesbar\n\n");
            foreach (var fehler in ergebnis.Fehler) werkzeug.Append("- ").Append(fehler).Append('\n');
            werkzeug.Append('\n');
        }
        return werkzeug.ToString();
    }
}
