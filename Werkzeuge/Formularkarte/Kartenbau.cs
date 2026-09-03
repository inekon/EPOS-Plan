using System.Globalization;
using System.Text;

namespace Formularkarte;

/// <summary>Eine Zeile der Feldkarte - ein Steuerelement mit seinem Umstellungsziel.</summary>
public sealed class Zeile
{
    public required Steuerelement Element { get; init; }

    /// <summary>Beschriftung deutsch (Label links/darueber, bei Knoepfen die Aufschrift).</summary>
    public string TextDe { get; init; } = "";

    /// <summary>Beschriftung englisch aus <c>Form_X.en-US.resx</c>.</summary>
    public string TextEn { get; init; } = "";

    /// <summary>Feldtyp im Ziel: Zahl, Ganzzahl, Text, Auswahl, Schalter, Datum, Raster, ...</summary>
    public string Feldtyp { get; init; } = "";

    /// <summary>Komponente aus EPOS.UI, die das Steuerelement ersetzt.</summary>
    public string Komponente { get; init; } = "";

    /// <summary>Wertebereich, Optionen und Zustandsbesonderheiten.</summary>
    public string Bereich { get; init; } = "";
}

/// <summary>Ein Abschnitt der Maske - das Fenster selbst oder eine GroupBox / TabPage / Panel.</summary>
public sealed class Abschnitt
{
    /// <summary>Ueberschrift des Abschnitts.</summary>
    public required string Titel { get; init; }

    /// <summary>Das Steuerelement, das den Abschnitt aufmacht; <c>null</c> beim Fenster.</summary>
    public Steuerelement? Traeger { get; init; }

    /// <summary>Verschachtelungstiefe (0 = Fenster).</summary>
    public int Tiefe { get; init; }

    /// <summary>Die Zeilen dieses Abschnitts in TabIndex-Reihenfolge.</summary>
    public List<Zeile> Zeilen { get; } = new();
}

/// <summary>
/// Baut aus der gelesenen Maske die Abschnitte und Zeilen der Feldkarte.
/// Beide Ausgaben - Markdown und Razor - arbeiten auf diesem Ergebnis, damit
/// Karte und Skelett nie auseinanderlaufen.
/// </summary>
public static class Kartenbau
{
    /// <summary>Liest Designer, .resx und Form_X.cs und baut die Abschnitte.</summary>
    public static Maske Vollstaendig(string designerPfad, string? resxPfad = null, string? suchwurzel = null,
                                     bool erreichbarkeit = true)
    {
        var maske = DesignerLeser.Lesen(designerPfad);
        ResxLeser.Anwenden(maske, resxPfad);
        LabelRegel.Anwenden(maske);
        QuelltextLeser.Anwenden(maske, suchwurzel);
        if (erreichbarkeit) Erreichbarkeit.Anwenden(maske, suchwurzel);
        return maske;
    }

    /// <summary>Die Abschnitte der Maske, Fenster zuerst, dann die Behaelter von oben nach unten.</summary>
    public static List<Abschnitt> Abschnitte(Maske maske)
    {
        var ergebnis = new List<Abschnitt>();
        Sammeln(maske, null, "Fenster", 0, ergebnis);
        return ergebnis;
    }

    private static void Sammeln(Maske maske, Steuerelement? traeger, string titel, int tiefe, List<Abschnitt> ergebnis)
    {
        var name = traeger?.Name;
        var kinder = maske.Steuerelemente
            .Where(s => string.Equals(s.Elter, name, StringComparison.Ordinal))
            .ToList();

        var abschnitt = new Abschnitt { Titel = titel, Traeger = traeger, Tiefe = tiefe };
        foreach (var kind in kinder.Where(Zeilenwuerdig).OrderBy(Ordnung).ThenBy(s => s.Y).ThenBy(s => s.X))
        {
            abschnitt.Zeilen.Add(Bauen(kind));
        }
        if (abschnitt.Zeilen.Count > 0 || traeger is null) ergebnis.Add(abschnitt);

        foreach (var kind in kinder.Where(s => s.Art == Art.Sektion).OrderBy(s => s.Y).ThenBy(s => s.X))
        {
            var untertitel = string.IsNullOrWhiteSpace(kind.Text) ? kind.Name : kind.Text;
            Sammeln(maske, kind, untertitel, tiefe + 1, ergebnis);
        }
    }

    /// <summary>Ohne TabIndex ans Ende - sonst stuenden alle Ungesetzten vorn.</summary>
    private static int Ordnung(Steuerelement element) => element.TabIndex ?? int.MaxValue;

    /// <summary>
    /// Kommt das Steuerelement als eigene Zeile in die Karte? Ein Label, das
    /// bereits als Beschriftung eines Feldes dient, nicht - es steht dort in
    /// der Spalte "Label/Text de".
    /// </summary>
    public static bool Zeilenwuerdig(Steuerelement element) => element.Art switch
    {
        Art.Feld => true,
        Art.Knopf => true,
        Art.Sonstig => true,
        Art.Beschriftung => !element.AlsBeschriftungVerbraucht,
        _ => false
    };

    private static Zeile Bauen(Steuerelement element)
    {
        var (feldtyp, komponente) = Ziel(element);
        return new Zeile
        {
            Element = element,
            TextDe = TextDe(element),
            TextEn = TextEn(element),
            Feldtyp = feldtyp,
            Komponente = komponente,
            Bereich = Bereich(element)
        };
    }

    /// <summary>Deutsche Beschriftung: Label links/darueber, sonst der eigene Text.</summary>
    public static string TextDe(Steuerelement element) =>
        element.Beschriftung is { } label && !string.IsNullOrWhiteSpace(label.Text)
            ? label.Text
            : element.Text;

    /// <summary>Englische Beschriftung aus der en-US-.resx, in derselben Reihenfolge.</summary>
    public static string TextEn(Steuerelement element)
    {
        if (element.Beschriftung is { } label && !string.IsNullOrWhiteSpace(label.TextEn)) return label.TextEn!;
        return element.TextEn ?? "";
    }

    /// <summary>
    /// Feldtyp und Zielkomponente. Die Tabelle steht in LIESMICH.md;
    /// alles, was der Leser nicht sicher zuordnen kann, wird "pruefen" -
    /// die Karte soll nicht raten.
    /// </summary>
    public static (string Feldtyp, string Komponente) Ziel(Steuerelement element)
    {
        switch (element.Typ)
        {
            case "TextBox":
            case "RichTextBox":
            case "MaskedTextBox":
                return element.ZahlArt switch
                {
                    "Zahl" => ("Zahl", "Zahlenfeld"),
                    "Ganzzahl" => ("Ganzzahl", "Ganzzahlfeld"),
                    _ => ("Text", "Textfeld")
                };
            case "NumericUpDown":
            case "DomainUpDown":
                return ("Zahl", "Zahlenfeld");
            case "ComboBox":
            case "ListBox":
                return ("Auswahl", "Auswahlfeld");
            case "RadioButton":
                return ("Auswahl", "Auswahlfeld (Gruppe pruefen)");
            case "CheckBox":
                return ("Schalter", "Schalter");
            case "DateTimePicker":
            case "MonthCalendar":
                return ("Datum", "Datumsfeld");
            case "DataGridView":
            case "ListView":
                return ("Raster", "Raster");
            case "Chart":
                return ("Diagramm", "ChartBild");
            case "GroupBox":
            case "TabPage":
                return ("Sektion", "Gruppenkopf");
            case "TabControl":
            case "Panel":
            case "FlowLayoutPanel":
            case "TableLayoutPanel":
            case "SplitContainer":
            case "SplitterPanel":
                return ("Sektion", "Aufteilung");
            case "Label":
            case "LinkLabel":
                return ("Text", "Text");
            case "Button":
                if (Typtabelle.IstHilfeknopf(element)) return ("Hilfe", "InfoKnopf");
                if (Typtabelle.IstSchliessknopf(element)) return ("Knopf", "SpeichernLeiste");
                return ("Knopf", "Knopf (pruefen)");
            default:
                return ("-", "pruefen");
        }
    }

    /// <summary>Wertebereich, Auswahlliste und Zustandsbesonderheiten in einer Spalte.</summary>
    public static string Bereich(Steuerelement element)
    {
        var teile = new List<string>();

        Zahl(teile, element, "Minimum", "Min");
        Zahl(teile, element, "Maximum", "Max");
        Zahl(teile, element, "DecimalPlaces", "Nachkomma");
        Zahl(teile, element, "Increment", "Schritt");
        Zahl(teile, element, "MaxLength", "Hoechstlaenge");

        if (element.Wert("DropDownStyle") is { } stil) teile.Add("Stil=" + stil);
        if (element.Wert("Format") is { } format && element.Typ == "DateTimePicker") teile.Add("Format=" + format);
        if (Wahr(element, "Multiline")) teile.Add("mehrzeilig");
        if (Wahr(element, "Checked")) teile.Add("vorbelegt an");
        if (Falsch(element, "Enabled")) teile.Add("gesperrt");
        if (Falsch(element, "Visible")) teile.Add("verborgen");
        if (Wahr(element, "ReadOnly")) teile.Add("nur lesen");
        if (element.Wert("Dock") is { } dock) teile.Add("Dock=" + dock);
        if (!element.Eingehaengt && element.Art != Art.Beiwerk) teile.Add("nicht eingehaengt");

        if (element.Eintraege.Count > 0)
        {
            var liste = element.Eintraege.Take(6).Select(e => "\"" + e + "\"");
            teile.Add("Eintraege: " + string.Join(", ", liste) +
                      (element.Eintraege.Count > 6 ? " ... (" + element.Eintraege.Count + ")" : ""));
        }

        return string.Join("; ", teile);
    }

    private static void Zahl(List<string> teile, Steuerelement element, string eigenschaft, string beschriftung)
    {
        var wert = element.Wert(eigenschaft);
        if (string.IsNullOrWhiteSpace(wert)) return;
        teile.Add(beschriftung + "=" + wert);
    }

    private static bool Wahr(Steuerelement element, string eigenschaft) =>
        string.Equals(element.Wert(eigenschaft), "true", StringComparison.OrdinalIgnoreCase);

    private static bool Falsch(Steuerelement element, string eigenschaft) =>
        string.Equals(element.Wert(eigenschaft), "false", StringComparison.OrdinalIgnoreCase);

    /// <summary>Zaehlt die Steuerelemente je Typ - fuer den Kopf der Karte und den Stapelbericht.</summary>
    public static SortedDictionary<string, int> Typzaehlung(Maske maske)
    {
        var zaehlung = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var element in maske.Steuerelemente)
        {
            if (element.Art == Art.Beiwerk) continue;
            zaehlung.TryGetValue(element.Typ, out var anzahl);
            zaehlung[element.Typ] = anzahl + 1;
        }
        return zaehlung;
    }

    /// <summary>Fasst die Typzaehlung als "ComboBox 1, TextBox 1, Button 2" zusammen.</summary>
    public static string Typzeile(Maske maske)
    {
        var zaehlung = Typzaehlung(maske);
        if (zaehlung.Count == 0) return "keine";
        var werkzeug = new StringBuilder();
        foreach (var (typ, anzahl) in zaehlung.OrderByDescending(p => p.Value).ThenBy(p => p.Key, StringComparer.Ordinal))
        {
            if (werkzeug.Length > 0) werkzeug.Append(", ");
            werkzeug.Append(typ).Append(' ').Append(anzahl.ToString(CultureInfo.InvariantCulture));
        }
        return werkzeug.ToString();
    }
}
