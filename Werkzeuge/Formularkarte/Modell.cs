using System.Globalization;

namespace Formularkarte;

/// <summary>
/// Ein Punkt oder eine Groesse aus dem Designer bzw. der .resx - beides steht
/// dort als Zahlenpaar ("159, 26").
/// </summary>
public readonly record struct Paar(int X, int Y)
{
    public override string ToString() => X.ToString(CultureInfo.InvariantCulture) + ", " +
                                         Y.ToString(CultureInfo.InvariantCulture);
}

/// <summary>Ein am Steuerelement angemeldeter Ereignishandler.</summary>
/// <param name="Ereignis">Name des Ereignisses, z. B. <c>Click</c>.</param>
/// <param name="Handler">Name der Methode in der Form_X.cs.</param>
public sealed record Anmeldung(string Ereignis, string Handler)
{
    public override string ToString() => Ereignis + " -> " + Handler;
}

/// <summary>Grobe Einordnung eines Steuerelementtyps.</summary>
public enum Art
{
    /// <summary>Eingabefeld (TextBox, ComboBox, NumericUpDown, ...).</summary>
    Feld,
    /// <summary>Beschriftung (Label, LinkLabel).</summary>
    Beschriftung,
    /// <summary>Knopf.</summary>
    Knopf,
    /// <summary>Behaelter, der einen Abschnitt aufmacht (GroupBox, TabPage, Panel, ...).</summary>
    Sektion,
    /// <summary>Menue- und Leistenteile - in der Karte nur gezaehlt.</summary>
    Leiste,
    /// <summary>Kein sichtbares Steuerelement (Container, ComponentResourceManager, ChartArea, ...).</summary>
    Beiwerk,
    /// <summary>Typ dem Leser unbekannt - in der Karte als "sonstig" mit Typnamen.</summary>
    Sonstig
}

/// <summary>
/// Ein Steuerelement aus <c>InitializeComponent</c>. Die Eigenschaften stehen
/// normalisiert in <see cref="Eigenschaften"/>: Zeichenketten ohne
/// Anfuehrungszeichen, Punkte und Groessen als "x, y", Aufzaehlungen auf ihr
/// letztes Glied gekuerzt. Damit liefern der Designer-Leser und der
/// .resx-Leser dieselbe Form.
/// </summary>
public sealed class Steuerelement
{
    /// <summary>Feldname im Designer, z. B. <c>cmbBrennstoffArt</c>.</summary>
    public required string Name { get; init; }

    /// <summary>Einfacher Typname, z. B. <c>ComboBox</c>.</summary>
    public string Typ { get; set; } = "";

    /// <summary>Vollstaendiger Typname, wie er im Designer steht.</summary>
    public string VollerTyp { get; set; } = "";

    /// <summary>Einordnung des Typs.</summary>
    public Art Art { get; set; } = Art.Sonstig;

    /// <summary>Reihenfolge der Felddeklaration - die letzte Ruecklage beim Sortieren.</summary>
    public int Reihenfolge { get; set; }

    /// <summary>Normalisierte Eigenschaften aus Designer und .resx.</summary>
    public Dictionary<string, string> Eigenschaften { get; } = new(StringComparer.Ordinal);

    /// <summary>Texte aus <c>Form_X.en-US.resx</c> (nur Text-Eigenschaften).</summary>
    public string? TextEn { get; set; }

    /// <summary>Angemeldete Ereignishandler.</summary>
    public List<Anmeldung> Ereignisse { get; } = new();

    /// <summary>Eintraege aus <c>Items.Add</c> / <c>Items.AddRange</c>.</summary>
    public List<string> Eintraege { get; } = new();

    /// <summary>Name des Elternsteuerelements; <c>null</c> = liegt direkt im Fenster.</summary>
    public string? Elter { get; set; }

    /// <summary>
    /// Wurde das Steuerelement ueberhaupt in eine <c>Controls</c>-Sammlung
    /// gehaengt? Ein Nein heisst: Der Designer kennt es, das Fenster zeigt es
    /// nicht - ein Befund fuer die Karte.
    /// </summary>
    public bool Eingehaengt { get; set; }

    /// <summary>Wurde das Steuerelement ueber <c>resources.ApplyResources</c> versorgt?</summary>
    public bool AusRessourcen { get; set; }

    /// <summary>Zugeordnete Beschriftung (Label links daneben oder darueber).</summary>
    public Steuerelement? Beschriftung { get; set; }

    /// <summary>Wurde dieses Label bereits als Beschriftung verbraucht?</summary>
    public bool AlsBeschriftungVerbraucht { get; set; }

    /// <summary>Feldtyp aus der Form_X.cs: "Zahl", "Ganzzahl" oder <c>null</c>.</summary>
    public string? ZahlArt { get; set; }

    public string? Wert(string eigenschaft) =>
        Eigenschaften.TryGetValue(eigenschaft, out var wert) ? wert : null;

    public string Text => Wert("Text") ?? "";

    public Paar? Ort => PaarLesen(Wert("Location"));

    public Paar? Groesse => PaarLesen(Wert("Size"));

    public int? TabIndex => GanzzahlLesen(Wert("TabIndex"));

    /// <summary>x-Wert des Steuerelements; ohne Location 0 (dann sortiert die Reihenfolge).</summary>
    public int X => Ort?.X ?? 0;

    /// <summary>y-Wert des Steuerelements; ohne Location 0.</summary>
    public int Y => Ort?.Y ?? 0;

    public static Paar? PaarLesen(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var teile = text.Split(',');
        if (teile.Length != 2) return null;
        if (!int.TryParse(teile[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var x)) return null;
        if (!int.TryParse(teile[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var y)) return null;
        return new Paar(x, y);
    }

    public static int? GanzzahlLesen(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        return int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var wert)
            ? wert
            : null;
    }
}

/// <summary>Eine gelesene Maske - alles, was Karte und Skelett brauchen.</summary>
public sealed class Maske
{
    /// <summary>Dateiname ohne <c>.Designer.cs</c>, z. B. <c>Form_Kosten_Auswahl</c>.</summary>
    public required string Bezeichner { get; init; }

    /// <summary>Name der partiellen Klasse - weicht vereinzelt vom Dateinamen ab.</summary>
    public string Klasse { get; set; } = "";

    /// <summary>Pfad der Designer-Datei, relativ zur Repowurzel wenn moeglich.</summary>
    public string Datei { get; set; } = "";

    /// <summary>Ordnername der Designer-Datei, z. B. <c>Kosten</c>.</summary>
    public string Ordner { get; set; } = "";

    /// <summary>Alle gelesenen Steuerelemente in Deklarationsreihenfolge.</summary>
    public List<Steuerelement> Steuerelemente { get; } = new();

    /// <summary>Eigenschaften des Formulars selbst (Text, ClientSize, Font, ...).</summary>
    public Dictionary<string, string> Formular { get; } = new(StringComparer.Ordinal);

    /// <summary>Am Formular angemeldete Ereignisse (Load, FormClosing, ...).</summary>
    public List<Anmeldung> FormularEreignisse { get; } = new();

    /// <summary>Fenstertitel englisch (aus <c>$this.Text</c> der en-US-.resx).</summary>
    public string? TitelEn { get; set; }

    /// <summary>Nutzt die Maske <c>resources.ApplyResources</c>?</summary>
    public bool Lokalisiert { get; set; }

    /// <summary>Gelesene .resx-Dateien (zur Nachvollziehbarkeit in der Karte).</summary>
    public List<string> Ressourcendateien { get; } = new();

    /// <summary>Anzahl <c>MessageBox.Show</c> in der Form_X.cs.</summary>
    public int Meldungen { get; set; }

    /// <summary>Fundstellen, an denen die Maske mit <c>ShowDialog</c> geoeffnet wird.</summary>
    public List<string> Aufrufer { get; } = new();

    /// <summary>Zeile und Umfang der Ereignishandler in der Form_X.cs.</summary>
    public Dictionary<string, (int Zeile, int Zeilen)> Handler { get; } = new(StringComparer.Ordinal);

    /// <summary>Gab es eine Form_X.cs neben dem Designer?</summary>
    public bool QuelltextGefunden { get; set; }

    public string Titel => Formular.TryGetValue("Text", out var text) ? text : "";

    public Paar? Fenstergroesse => Steuerelement.PaarLesen(
        Formular.TryGetValue("ClientSize", out var wert) ? wert : null);

    /// <summary>Steuerelement zu einem Namen; <c>null</c>, wenn unbekannt.</summary>
    public Steuerelement? Finden(string name) =>
        Steuerelemente.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.Ordinal));
}
