using System.Globalization;
using System.Text;

namespace Formularkarte;

/// <summary>
/// Schreibt die Feldkarte als Markdown: Kopf mit den Kennzahlen der Maske,
/// dann je Abschnitt eine Tabelle. Die letzte Spalte ist die Abnahme-Checkbox -
/// die Karte ist die Checkliste, an der die Umstellung einer Maske (iU9)
/// abgenommen wird.
/// </summary>
public static class FeldkarteSchreiber
{
    public static string Schreiben(Maske maske)
    {
        var werkzeug = new StringBuilder();
        var abschnitte = Kartenbau.Abschnitte(maske);

        werkzeug.Append("# Feldkarte ").Append(maske.Bezeichner).Append("\n\n");
        Kopf(werkzeug, maske, abschnitte);

        foreach (var abschnitt in abschnitte)
        {
            if (abschnitt.Zeilen.Count == 0) continue;
            Abschnitt(werkzeug, abschnitt);
        }

        Fuss(werkzeug, maske);
        return werkzeug.ToString();
    }

    private static void Kopf(StringBuilder werkzeug, Maske maske, List<Abschnitt> abschnitte)
    {
        var groesse = maske.Fenstergroesse;
        var felder = abschnitte.Sum(a => a.Zeilen.Count);
        var ohneLabel = abschnitte.SelectMany(a => a.Zeilen)
            .Count(z => z.Element.Art == Art.Feld && string.IsNullOrWhiteSpace(z.TextDe));

        werkzeug.Append("| Angabe | Wert |\n|---|---|\n");
        Zeile(werkzeug, "Maske", maske.Bezeichner + (maske.Klasse == maske.Bezeichner ? "" : " (Klasse `" + maske.Klasse + "`)"));
        Zeile(werkzeug, "Datei", "`" + maske.Datei + "`");
        Zeile(werkzeug, "Titel de", maske.Titel);
        Zeile(werkzeug, "Titel en", maske.TitelEn ?? "");
        Zeile(werkzeug, "ClientSize", groesse is { } paar
            ? paar.X.ToString(CultureInfo.InvariantCulture) + " x " + paar.Y.ToString(CultureInfo.InvariantCulture)
            : "");
        Zeile(werkzeug, "Lokalisiert", maske.Lokalisiert ? "ja (ApplyResources)" : "nein");
        if (maske.Ressourcendateien.Count > 0)
        {
            Zeile(werkzeug, "Ressourcen", string.Join(", ", maske.Ressourcendateien.Select(d => "`" + Path.GetFileName(d) + "`")));
        }
        Zeile(werkzeug, "Zeilen der Karte", felder.ToString(CultureInfo.InvariantCulture));
        Zeile(werkzeug, "Steuerelemente", Kartenbau.Typzeile(maske));
        Zeile(werkzeug, "Felder ohne Beschriftung", ohneLabel.ToString(CultureInfo.InvariantCulture));
        Zeile(werkzeug, "MessageBox", maske.QuelltextGefunden
            ? maske.Meldungen.ToString(CultureInfo.InvariantCulture)
            : "keine Form_X.cs gefunden");
        Zeile(werkzeug, "Aufrufer (ShowDialog)", Aufrufer(maske));
        if (maske.Erreichbarkeit is { } befund)
        {
            // Die Frage, die die Karte bis iU8-12 nicht beantwortet hat: Ist der
            // Aufrufer selbst noch ueber Menue, Kachel oder Reiter zu erreichen?
            Zeile(werkzeug, "Öffner erreichbar", befund.Zusammenfassung);
        }
        if (maske.FormularEreignisse.Count > 0)
        {
            Zeile(werkzeug, "Fensterereignisse", string.Join(", ", maske.FormularEreignisse.Select(e => "`" + e + "`")));
        }
        werkzeug.Append('\n');
    }

    /// <summary>
    /// Die Fundstellen, an denen die Maske geoeffnet wird. Sammeldialoge wie
    /// "Bezeichner eingeben" haben Dutzende davon - die Karte nennt die Zahl
    /// und die ersten acht Stellen, alles Weitere findet eine Volltextsuche.
    /// </summary>
    private static string Aufrufer(Maske maske)
    {
        if (maske.Aufrufer.Count == 0) return "keiner gefunden";

        var anzahl = maske.Aufrufer.Count;
        var gezeigt = maske.Aufrufer.Take(8).Select(a => "`" + a + "`");
        var text = anzahl.ToString(CultureInfo.InvariantCulture) + ": " + string.Join(", ", gezeigt);
        return anzahl > 8 ? text + " ..." : text;
    }

    private static void Zeile(StringBuilder werkzeug, string name, string wert)
    {
        werkzeug.Append("| ").Append(name).Append(" | ").Append(Zelle(wert)).Append(" |\n");
    }

    private static void Abschnitt(StringBuilder werkzeug, Abschnitt abschnitt)
    {
        var stufe = new string('#', Math.Min(6, abschnitt.Tiefe + 2));
        werkzeug.Append(stufe).Append(' ').Append(abschnitt.Titel);
        if (abschnitt.Traeger is { } traeger)
        {
            werkzeug.Append(" (`").Append(traeger.Name).Append("`, ").Append(traeger.Typ).Append(')');
        }
        werkzeug.Append("\n\n");

        werkzeug.Append("| # | Steuerelement | Typ | Label/Text de | Text en | Feldtyp Ziel | ")
                .Append("Bereich/Optionen | TabIndex | Ereignisse | Ziel-Komponente | ☐ |\n");
        werkzeug.Append("|---|---|---|---|---|---|---|---|---|---|---|\n");

        var nummer = 0;
        foreach (var zeile in abschnitt.Zeilen)
        {
            nummer++;
            var element = zeile.Element;
            werkzeug.Append("| ").Append(nummer.ToString(CultureInfo.InvariantCulture))
                    .Append(" | `").Append(element.Name).Append('`')
                    .Append(" | ").Append(Typ(element))
                    .Append(" | ").Append(Zelle(zeile.TextDe))
                    .Append(" | ").Append(Zelle(zeile.TextEn))
                    .Append(" | ").Append(zeile.Feldtyp)
                    .Append(" | ").Append(Zelle(zeile.Bereich))
                    .Append(" | ").Append(element.TabIndex?.ToString(CultureInfo.InvariantCulture) ?? "")
                    .Append(" | ").Append(Zelle(string.Join(", ", element.Ereignisse.Select(e => e.ToString()))))
                    .Append(" | ").Append(Zelle(zeile.Komponente))
                    .Append(" | ☐ |\n");
        }
        werkzeug.Append('\n');
    }

    private static string Typ(Steuerelement element) =>
        element.Art == Art.Sonstig ? "sonstig (" + element.Typ + ")" : element.Typ;

    private static void Fuss(StringBuilder werkzeug, Maske maske)
    {
        if (maske.Handler.Count == 0) return;

        werkzeug.Append("## Ereignishandler in `")
                .Append(maske.Bezeichner).Append(".cs`\n\n");
        werkzeug.Append("| Handler | Zeile | Umfang |\n|---|---|---|\n");
        foreach (var (name, stelle) in maske.Handler.OrderBy(p => p.Value.Zeile))
        {
            werkzeug.Append("| `").Append(name).Append("` | ")
                    .Append(stelle.Zeile.ToString(CultureInfo.InvariantCulture)).Append(" | ")
                    .Append(stelle.Zeilen.ToString(CultureInfo.InvariantCulture)).Append(" Zeilen |\n");
        }
        werkzeug.Append('\n');
    }

    /// <summary>Zellinhalt entschaerfen: Senkrechte und Zeilenumbrueche brechen die Tabelle.</summary>
    private static string Zelle(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text.Replace("|", "\\|")
                   .Replace("\r\n", " ")
                   .Replace("\n", " ")
                   .Replace("\r", " ")
                   .Trim();
    }
}
