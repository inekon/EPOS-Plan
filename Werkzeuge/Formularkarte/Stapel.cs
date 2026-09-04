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

    /// <summary>Der Befund des Erreichbarkeitsgraphen; <c>null</c>, wenn nicht gerechnet wurde.</summary>
    public Maskenknoten? Erreichbarkeit { get; init; }
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

    /// <summary>Wurde die Erreichbarkeit mitgerechnet?</summary>
    public bool MitErreichbarkeit => Zeilen.Any(z => z.Erreichbarkeit is not null);

    /// <summary>Zahl der Masken in einem Erreichbarkeitszustand.</summary>
    public int Erreichbar(Erreichbar status) => Zeilen.Count(z => z.Erreichbarkeit?.Status == status);

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
    /// <summary>
    /// Ordner, die nie zum Bestand zaehlen - Bauordner, die eingefrorenen Pruefmuster und die
    /// Git-Nebenbaeume (<c>.claude/worktrees</c>): Dort liegen vollstaendige Kopien des Repos auf
    /// einem anderen Stand, und ein Stapellauf ueber die Repowurzel zaehlte deren Masken mit
    /// (04.09.2026: der Typzeuge fand NumericUpDown nur noch in einem Worktree vor W13).
    /// </summary>
    private static readonly string[] Uebergangen = { "obj", "bin", "Pruefmuster", ".claude", ".git" };

    /// <summary>
    /// Alle Designer-Dateien unterhalb eines Ordners, Gross-/Kleinschreibung egal.
    ///
    /// <para>
    /// <c>Pruefmuster/</c> bleibt aussen vor: Dort liegen eingefrorene Kopien
    /// von Masken, die es im Bestand nicht mehr gibt (Werkzeuge/Formularkarte.Tests).
    /// Sie sind Lesevorlagen fuer die Tests - wuerde der Stapellauf sie mitzaehlen,
    /// meldete das Vollstaendigkeitsnetz mehr Masken, als das Programm hat.
    /// </para>
    ///
    /// <para><b>Gemessen wird ab der SUCHWURZEL, nicht am ganzen Pfad</b> (iU9-W14b): Laeuft
    /// der Stapellauf in einem Git-Nebenbaum, liegt dessen Wurzel SELBST unterhalb von
    /// <c>.claude/worktrees/</c> - ein Vergleich ueber den absoluten Pfad wirft dann den
    /// gesamten Bestand hinaus und meldet null Masken. Uebergangen wird also, was
    /// UNTERHALB der Suchwurzel in einem dieser Ordner liegt.</para>
    /// </summary>
    public static List<string> Dateien(string ordner) =>
        Directory.EnumerateFiles(ordner, "*.cs", SearchOption.AllDirectories)
            .Where(d => d.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .Where(d => !Uebergeht(ordner, d))
            .OrderBy(d => d, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// Liegt <paramref name="datei"/> unterhalb von <paramref name="wurzel"/> in einem der
    /// <see cref="Uebergangen"/>-Ordner? Gemessen wird der RELATIVE Pfad.
    /// </summary>
    private static bool Uebergeht(string wurzel, string datei)
    {
        string relativ = Path.GetRelativePath(wurzel, datei);
        string[] teile = relativ.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Der letzte Teil ist der Dateiname selbst - er zaehlt nicht als Ordner.
        for (int i = 0; i < teile.Length - 1; i++)
            if (Uebergangen.Contains(teile[i], StringComparer.Ordinal)) return true;
        return false;
    }

    /// <summary>Laeuft ueber alle Designer-Dateien; <paramref name="ziel"/> null = nur zaehlen.</summary>
    /// <param name="erreichbarkeit">
    /// Den Erreichbarkeitsgraphen mitrechnen (Vorgabe). Er liest den ganzen
    /// Projektbaum ein zweites Mal mit Roslyn - wer nur zaehlen will, spart ihn.
    /// </param>
    public static Stapelergebnis Laufen(string ordner, string? ziel, string? suchwurzel = null,
                                        bool erreichbarkeit = true)
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
                if (erreichbarkeit) Erreichbarkeit.Anwenden(maske, suchwurzel);

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
                    Typen = Kartenbau.Typzaehlung(maske),
                    Erreichbarkeit = maske.Erreichbarkeit
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
        werkzeug.Append("| Felder ohne Beschriftung | ").Append(ergebnis.OhneBeschriftung).Append(" |\n");
        if (ergebnis.MitErreichbarkeit)
        {
            werkzeug.Append("| Öffner erreichbar | ").Append(ergebnis.Erreichbar(Formularkarte.Erreichbar.Ja)).Append(" |\n");
            werkzeug.Append("| Öffner unerreichbar | ").Append(ergebnis.Erreichbar(Formularkarte.Erreichbar.Nein)).Append(" |\n");
            werkzeug.Append("| verwaist (kein Öffner) | ").Append(ergebnis.Erreichbar(Formularkarte.Erreichbar.Verwaist)).Append(" |\n");
            werkzeug.Append("| unklar | ").Append(ergebnis.Erreichbar(Formularkarte.Erreichbar.Unklar)).Append(" |\n");
        }
        werkzeug.Append('\n');

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

        werkzeug.Append("## Masken\n\n| Maske | Zeilen | Abschnitte | ohne Beschriftung | lokalisiert | ")
                .Append("Öffner erreichbar | Datei |\n");
        werkzeug.Append("|---|---|---|---|---|---|---|\n");
        foreach (var zeile in ergebnis.Zeilen.OrderBy(z => z.Bezeichner, StringComparer.Ordinal))
        {
            werkzeug.Append("| ").Append(zeile.Bezeichner)
                    .Append(" | ").Append(zeile.Zeilen)
                    .Append(" | ").Append(zeile.Abschnitte)
                    .Append(" | ").Append(zeile.OhneBeschriftung)
                    .Append(" | ").Append(zeile.Lokalisiert ? "ja" : "-")
                    .Append(" | ").Append(zeile.Erreichbarkeit?.StatusText ?? "-")
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

    /// <summary>
    /// Die Befundliste "Öffner erreichbar" als Markdown - jede Maske mit Zustand
    /// und Weg bzw. Öffner, die unerreichbaren, verwaisten und unklaren zuerst.
    /// Das ist die Stilllegungsliste K6 fuer iU9.
    /// </summary>
    public static string Erreichbarkeitsbefund(Stapelergebnis ergebnis, string ordner)
    {
        var werkzeug = new StringBuilder();
        werkzeug.Append("# Öffner erreichbar — Befund aller Masken\n\n");
        werkzeug.Append("Gelesener Baum: `").Append(ordner.Replace('\\', '/')).Append("`  \n");
        werkzeug.Append("Datum: ").Append(DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)).Append("  \n");
        werkzeug.Append("Erzeugt von `Werkzeuge/Formularkarte` (`--alle … --erreichbarkeit`).\n\n");

        werkzeug.Append("| Zustand | Masken | Bedeutung |\n|---|---|---|\n");
        werkzeug.Append("| ja | ").Append(ergebnis.Erreichbar(Formularkarte.Erreichbar.Ja))
                .Append(" | Weg von MDIMainForm bzw. Form_Start vorhanden |\n");
        werkzeug.Append("| nein | ").Append(ergebnis.Erreichbar(Formularkarte.Erreichbar.Nein))
                .Append(" | Öffner steht im Quelltext, ist selbst aber nicht zu erreichen |\n");
        werkzeug.Append("| verwaist | ").Append(ergebnis.Erreichbar(Formularkarte.Erreichbar.Verwaist))
                .Append(" | die Maske wird nirgends erzeugt |\n");
        werkzeug.Append("| unklar | ").Append(ergebnis.Erreichbar(Formularkarte.Erreichbar.Unklar))
                .Append(" | nur über einen zweifelhaften Weg (verborgener oder gesperrter Knopf) |\n");
        werkzeug.Append("| gesamt | ").Append(ergebnis.Masken).Append(" | |\n\n");

        werkzeug.Append("| Maske | Öffner erreichbar | Pfad bzw. Öffner | Datei |\n|---|---|---|---|\n");
        foreach (var zeile in ergebnis.Zeilen.OrderBy(z => Rang(z.Erreichbarkeit?.Status))
                                             .ThenBy(z => z.Bezeichner, StringComparer.Ordinal))
        {
            var befund = zeile.Erreichbarkeit;
            var erklaerung = befund is null
                ? ""
                : befund.Wurzel ? "Wurzel (Einstieg der Anwendung)" : Erklaerung(befund);

            werkzeug.Append("| ").Append(zeile.Bezeichner)
                    .Append(" | ").Append(befund?.StatusText ?? "-")
                    .Append(" | ").Append(Zelle(erklaerung))
                    .Append(" | `").Append(zeile.Datei).Append("` |\n");
        }
        werkzeug.Append('\n');
        return werkzeug.ToString();
    }

    /// <summary>Unerreichbar zuerst - die Liste wird von oben abgearbeitet.</summary>
    private static int Rang(Erreichbar? status) => status switch
    {
        Formularkarte.Erreichbar.Nein => 0,
        Formularkarte.Erreichbar.Verwaist => 1,
        Formularkarte.Erreichbar.Unklar => 2,
        Formularkarte.Erreichbar.Ja => 3,
        _ => 4
    };

    /// <summary>Pfad, Öffner und Hinweise in einer Zelle.</summary>
    private static string Erklaerung(Maskenknoten befund)
    {
        var teile = new List<string>();
        if (!string.IsNullOrEmpty(befund.Pfad)) teile.Add(befund.Pfad);
        if (befund.Status is Formularkarte.Erreichbar.Nein or Formularkarte.Erreichbar.Unklar && befund.Oeffner.Count > 0)
        {
            teile.Add("Öffner: " + string.Join("; ", befund.Oeffner.Take(3)) +
                      (befund.Oeffner.Count > 3 ? " … (" + befund.Oeffner.Count + ")" : ""));
        }
        teile.AddRange(befund.Hinweise);
        return teile.Count == 0 ? "" : string.Join(" — ", teile);
    }

    /// <summary>Zellinhalt entschaerfen: Senkrechte und Umbrueche brechen die Tabelle.</summary>
    private static string Zelle(string text) =>
        text.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ").Trim();
}
