using System.Globalization;

namespace EPOS.UI.Dialoge.Berichte;

/// <summary>
/// Ein Eintrag des Platzhalterkatalogs (BV-E1, Konzept 5 und 9.1 D) in der Überlagerung
/// „Platzhalterkatalog" — ohne Fachklasse des Kerns: Die Hülle formt den Katalogeintrag in
/// diese Zeile um, mit fertigen Anzeigetexten.
/// </summary>
/// <param name="Schluessel">Der Schlüssel ohne Klammern („projekt.kunde").</param>
/// <param name="Art">Die Art als Anzeigetext („Text", „Zahl", „Tabelle", „Bild", „Kapitel").</param>
/// <param name="Kontext">Der Kontext als Anzeigetext („Bericht", „Stamm", „je Stand").</param>
/// <param name="Beschreibung">Die Beschreibung aus dem Katalog (<c>VF_*</c>).</param>
/// <param name="Beispiel">Eine Beispielausgabe samt Einheit („1.234 MWh/a"); leer = keine.</param>
public sealed record Katalogzeile(string Schluessel, string Art, string Kontext, string Beschreibung,
                                  string Beispiel = "");

/// <summary>
/// Suche und Schreibweise des Platzhalterkatalogs — als eigene Klasse, damit die Regel an
/// EINER Stelle steht und sich ohne Oberfläche prüfen lässt.
/// </summary>
public static class Platzhalterkatalogsuche
{
    /// <summary>
    /// Die Einträge, die zur Suche passen: Jedes Wort der Suche (durch Leerraum getrennt) steht
    /// im Schlüssel ODER in der Beschreibung, Groß- und Kleinschreibung gleich. Geschweifte
    /// Klammern um ein Wort zählen nicht — wer „{{projekt.kunde}}" aus einer Vorlage einfügt,
    /// findet den Eintrag. Eine leere Suche lässt alles stehen; die Reihenfolge bleibt.
    /// </summary>
    public static IReadOnlyList<Katalogzeile> Filtern(IReadOnlyList<Katalogzeile>? zeilen, string? suche)
    {
        IReadOnlyList<Katalogzeile> alle = zeilen ?? Array.Empty<Katalogzeile>();
        string[] woerter = Woerter(suche);
        if (woerter.Length == 0) return alle;

        var treffer = new List<Katalogzeile>();
        foreach (Katalogzeile z in alle)
        {
            if (z is null) continue;
            bool passt = true;
            foreach (string w in woerter)
                if (!Enthaelt(z.Schluessel, w) && !Enthaelt(z.Beschreibung, w)) { passt = false; break; }
            if (passt) treffer.Add(z);
        }
        return treffer;
    }

    /// <summary>Die Schreibweise eines Schlüssels in der Vorlage: <c>{{schluessel}}</c>.</summary>
    public static string Schreibweise(string? schluessel) => "{{" + (schluessel ?? "").Trim() + "}}";

    /// <summary>Die Wörter der Suche, ohne Leerraum und ohne geschweifte Klammern.</summary>
    private static string[] Woerter(string? suche)
        => (suche ?? "")
           .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
           .Select(w => w.Trim('{', '}'))
           .Where(w => w.Length > 0)
           .ToArray();

    private static bool Enthaelt(string? text, string wort)
        => !string.IsNullOrEmpty(text)
           && CultureInfo.InvariantCulture.CompareInfo.IndexOf(text, wort, CompareOptions.IgnoreCase) >= 0;
}
